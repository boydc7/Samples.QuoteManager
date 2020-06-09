using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Samples.QuoteManager
{
    // A simple, naive, inflexible, working implementation of the IQuoteManager - simple and functional, likely would work well for tests, local environments, low quote volumes, etc.
    // This implementation does nothing to:
    //    - Protect/validate/normalize input (i.e. DateTime values on the quote, etc.)
    //    - Ensure values for comparison are normalized in the comparisions (i.e. again DateTime types as an example - could be comparing Unspecified typee vs. Local or UTC)
    //    - Thread safety is minimally protected against - adding/updating quotes will be fine, getting/executing quotes has a bevy of race conditions in the search for candidate quotes (getting values from the concurrent dictionary returns a concrete list copy of the values at the time requested)

    internal class SimpleQuoteManager : IQuoteManager
    {
        private readonly ConcurrentDictionary<Guid, IQuote> _quotes = new ConcurrentDictionary<Guid, IQuote>();

        public void AddOrUpdateQuote(IQuote quote)
            => _quotes.AddOrUpdate(quote.Id,
                                   quote,
                                   // NOTE: The instructions mentioned that if it exists already to "update it to match the given price, volume, and symbol",
                                   // but says nothing about the existing ExpirationDate...normally I'd probably ask for clarification, but for simplicity here
                                   // I'm going to follow the instructions to the letter...if we simply wanted to replace the existing quote entirely with the
                                   // new quote, well that'd simply be and updateValueFactory of:
                                   //         updateValueFactory: (id, existingQuote) => quote);
                                   (id, existingQuote) =>
                                   {
                                       existingQuote.Symbol = quote.Symbol;
                                       existingQuote.Price = quote.Price;
                                       existingQuote.AvailableVolume = quote.AvailableVolume;

                                       return existingQuote;
                                   });

        public void RemoveQuote(Guid id)
            => _quotes.TryRemove(id, out _);

        public void RemoveAllQuotes(string symbol)
        {
            foreach (var quote in _quotes.Values)
            {
                if (!string.Equals(quote.Symbol, symbol, StringComparison.Ordinal))
                {
                    continue;
                }

                RemoveQuote(quote.Id);
            }
        }

        public IQuote GetBestQuoteWithAvailableVolume(string symbol)
        {
            IQuote bestQuote = null;

            foreach (var quote in GetCandidateQuotesForSymbol(symbol))
            {
                // Candidate quote, is it better than the existing one (if there is an existing one)
                if (bestQuote == null || bestQuote.Price > quote.Price)
                {
                    bestQuote = quote;
                }
            }

            return bestQuote;
        }

        public ITradeResult ExecuteTrade(string symbol, uint volumeRequested)
        {
            var quotesByPrice = GetCandidateQuotesForSymbol(symbol).OrderBy(q => q.Price).ToList();

            var response = new TradeResult
                           {
                               Id = Guid.NewGuid(),
                               Symbol = symbol,
                               VolumeRequested = volumeRequested
                           };

            double weightedSum = 0;

            // Thread safety issues/race conditions throughout here...again, taking this as a given in this implementation...
            foreach (var quote in quotesByPrice)
            {
                var volumeTaken = quote.AvailableVolume >= (response.VolumeRequested - response.VolumeExecuted)
                                      ? (response.VolumeRequested - response.VolumeExecuted)
                                      : quote.AvailableVolume;

                quote.AvailableVolume -= volumeTaken;

                weightedSum += volumeTaken * quote.Price;

                response.VolumeExecuted += volumeTaken;

                if (response.VolumeExecuted >= response.VolumeRequested)
                {
                    break;
                }
            }

            if (response.VolumeExecuted > 0)
            {
                response.VolumeWeightedAveragePrice = weightedSum / response.VolumeExecuted;
            }

            return response;
        }

        private IEnumerable<IQuote> GetCandidateQuotesForSymbol(string symbol)
        {
            var now = DateTime.UtcNow; // NOTE: assuming UTC times for naive implementation, also assuming that the drift between right now and the
                                       // instant this is compared against in the loop is acceptable for expiration checking (i.e. if this were a very large
                                       // collection and multiple seconds passed between now and then, the quote being compared could be expired by then...

            foreach (var quote in _quotes.Values)
            {
                if (quote.AvailableVolume <= 0 ||
                    quote.ExpirationDate <= now || // Assuming <= is correct here...as by the time anything actually was executed, it'd be expired...
                    !string.Equals(quote.Symbol, symbol, StringComparison.Ordinal))
                {
                    continue;
                }

                yield return quote; // Race condition here...might not be available as a quote any longer at this point, but, taking that as a given in this concrete implementation
            }
        }
    }
}
