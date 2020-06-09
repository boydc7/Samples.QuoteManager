using System;
using System.Collections.Generic;
using System.Linq;

namespace Samples.QuoteManager.Recommended
{
    // Relatively course-grain locked executer service. Takes a single-writer only lock for basically the duration of a trade execution
    internal class CourseLockedTradeExecuter<T> : ITradeExecuter<T>
        where T : class, IQuoteExtended
    {
        private readonly List<Func<T, bool>> _quoteValidationPredicates;

        internal CourseLockedTradeExecuter(IEnumerable<Func<T, bool>> quoteValidationPredicates = null)
        {
            _quoteValidationPredicates = (quoteValidationPredicates ?? QuoteValidators.DefaultQuoteValidators<T>()).ToList();
        }

        public ITradeResult ExecuteTrade(string symbol, uint volumeRequested, ILockManager lockManager, IQuoteStorageService<T> quoteStorageService)
        {
            var response = new TradeResult
                           {
                               Id = Guid.NewGuid(),
                               Symbol = symbol,
                               VolumeRequested = volumeRequested
                           };

            double weightedSum = 0;

            var originalQuotes = new List<T>();

            // Intent write is essentially a single writer/many readers lock mode - only one thread can enter intent-write at a time, but readers
            // are not blocked at all (which is the functionality we're looking for here). Note that we never actually enter a write-lock mode, which
            // would allow a single writer and nothing else to occur...
            lockManager.EnterIntentWrite(symbol);

            try
            {
                var quotes = quoteStorageService.GetQuotesByPrice(symbol);

                if (quotes == null || quotes.Count == 0)
                {
                    return response;
                }

                foreach (var quote in quotes)
                { // Candidate quote, has to pass validation predicates to be considered
                    if (!_quoteValidationPredicates.All(p => p(quote)))
                    {
                        continue;
                    }

                    var volumeTaken = quote.AvailableVolume >= (response.VolumeRequested - response.VolumeExecuted)
                                          ? (response.VolumeRequested - response.VolumeExecuted)
                                          : quote.AvailableVolume;

                    originalQuotes.Add((T)quote.Clone());

                    quote.AvailableVolume -= volumeTaken;

                    // Mostly here to mimic what would be needed if a non-reference based storage service were to be used...
                    quoteStorageService.Update(quote);

                    weightedSum += volumeTaken * quote.Price;

                    response.VolumeExecuted += volumeTaken;

                    if (response.VolumeExecuted >= response.VolumeRequested)
                    {
                        break;
                    }
                }
            }
            catch when(RevertQuotesAndReturnFalse(originalQuotes, quoteStorageService))
            { // Unreachable throw - the above when filter will revert any modified quotes and return false, just rethrowing like an unhandled exception
                // without unwinding the call stack
                throw;
            }
            finally
            {
                lockManager.ExitIntentWrite(symbol);
            }

            if (response.VolumeExecuted > 0)
            {
                response.VolumeWeightedAveragePrice = weightedSum / response.VolumeExecuted;
            }

            return response;
        }

        private bool RevertQuotesAndReturnFalse(IReadOnlyCollection<T> originalQuotes, IQuoteStorageService<T> quoteStorageService)
        {
            if (originalQuotes == null || originalQuotes.Count == 0)
            {
                return false;
            }

            foreach (var originalQuote in originalQuotes)
            {
                quoteStorageService.AddOrUpdate(originalQuote);
            }

            return false;
        }
    }
}
