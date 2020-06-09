using System;
using System.Collections.Generic;
using System.Linq;

namespace Samples.QuoteManager.Recommended
{
    internal class RecommendedQuoteManager<T> : IQuoteManagerExtended<T>
        where T : class, IQuoteExtended
    {
        private readonly ILockManager _lockManager;
        private readonly ITradeExecuter<T> _tradeExecuter;
        private readonly List<Func<T, bool>> _quoteValidationPredicates;
        private readonly IQuoteObserver<T> _observer;

        internal RecommendedQuoteManager(IQuoteObserver<T> observer,
                                         IQuoteStorageService<T> quoteStorageService,
                                         ILockManager lockManager,
                                         ITradeExecuter<T> tradeExecuter,
                                         IEnumerable<Func<T, bool>> quoteValidationPredicates = null)
        {
            _observer = observer ?? throw new ArgumentNullException(nameof(observer));
            StorageService = quoteStorageService ?? throw new ArgumentNullException(nameof(quoteStorageService));
            _lockManager = lockManager;
            _tradeExecuter = tradeExecuter;
            _quoteValidationPredicates = (quoteValidationPredicates ?? QuoteValidators.DefaultQuoteValidators<T>()).ToList();
        }

        public IQuoteStorageService<T> StorageService { get; }

        public void AddOrUpdateQuote(T quote)
        {
            // Typically this would be in a mapper/transformer/etc. likely, but for now simply adding some formating here
            quote.Price = Math.Round(quote.Price, 8);
            quote.Symbol = quote.Symbol.Trim();

            if (quote.ExpirationTimestamp <= 0)
            {
                quote.ExpirationTimestamp = quote.ExpirationDate.ToUnixTimestamp();
            }

            // Intent write is essentially a single writer/many readers lock mode - only one thread can enter intent-write at a time, but readers
            // are not blocked at all (which is the functionality we're looking for here). Note that we never actually enter a write-lock mode, which
            // would allow a single writer and nothing else to occur...
            _lockManager.EnterIntentWrite(quote.Symbol);

            try
            {
                StorageService.AddOrUpdate(quote);
            }
            finally
            {
                _lockManager.ExitIntentWrite(quote.Symbol);
            }

            _observer.OnAddOrUpdate(quote);
        }

        public void AddOrUpdateQuote(IQuote quote)
        {
            if (!(quote is T quoteExtended))
            {
                throw new ArgumentOutOfRangeException(nameof(quote));
            }

            AddOrUpdateQuote(quoteExtended);
        }

        public void RemoveQuote(Guid id)
        {
            var quote = StorageService.GetQuoteById(id);

            _lockManager.EnterIntentWrite(quote.Symbol);

            try
            {
                StorageService.Remove(quote);
            }
            finally
            {
                _lockManager.ExitIntentWrite(quote.Symbol);
            }

            _observer.OnRemove(quote);
        }

        public void RemoveAllQuotes(string symbol)
        {
            IList<T> removed = null;

            _lockManager.EnterIntentWrite(symbol);

            try
            {
                removed = StorageService.RemoveAll(symbol);
            }
            finally
            {
                _lockManager.ExitIntentWrite(symbol);
            }

            _observer.OnRemoveAll(symbol, removed);
        }

        public IQuote GetBestQuoteWithAvailableVolume(string symbol)
        {
            IList<T> quotes = null;

            _lockManager.EnterRead(symbol);

            try
            {
                quotes = StorageService.GetQuotesByPrice(symbol);
            }
            finally
            {
                _lockManager.ExitRead(symbol);
            }

            if (quotes == null || quotes.Count == 0)
            {
                return null;
            }

            foreach (var quote in quotes)
            {   // Candidate quote, is it better than the existing one (if there is an existing one)
                if (!_quoteValidationPredicates.All(p => p(quote)))
                {
                    _observer.OnCandidateQuoteMiss(quote);
                    continue;
                }

                // Quotes are already sorted from storage...first valid one we get is it
                // Technically still a race condition here in that this quote might at this point be invalid/missing/changed...but I'm taking that as an ok for the implementation
                // as the actual trade execution still ensures proper availability on a trade attempt, and the locking/checking/looping here is likely not worth it, since
                // as soon as anything is returned to a consumer, it's out of date anyhow....
                return quote;
            }

            return null;
        }

        public ITradeResult ExecuteTrade(string symbol, uint volumeRequested)
        {
            var tradeResult = _tradeExecuter.ExecuteTrade(symbol, volumeRequested, _lockManager, StorageService);

            _observer.OnExecuteTrade(tradeResult);

            return tradeResult;
        }
    }
}
