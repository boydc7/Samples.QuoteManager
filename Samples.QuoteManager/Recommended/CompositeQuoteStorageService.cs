using System;
using System.Collections.Generic;

namespace Samples.QuoteManager.Recommended
{
    internal class CompositeQuoteStorageService<T> : IQuoteStorageService<T>
        where T : class, IQuote
    {
        private readonly IQuoteStorageService<T> _quoteStorageService;
        private readonly IQuoteSymbolIndexService<T> _quoteIndexService;

        public CompositeQuoteStorageService(IQuoteStorageService<T> quoteStorageService, IQuoteSymbolIndexService<T> quoteIndexService)
        {
            _quoteStorageService = quoteStorageService;
            _quoteIndexService = quoteIndexService;
        }

        public IEnumerable<T> GetAllQuotes()
            => _quoteStorageService.GetAllQuotes();

        public T GetQuoteById(Guid id)
        {
            var symbol = _quoteIndexService.GetSymbolForId(id);

            return GetQuoteBySymbolId(symbol, id);
        }

        public IList<T> GetQuotesByPrice(string symbol)
            => _quoteStorageService.GetQuotesByPrice(symbol);

        public void AddOrUpdate(T quote)
        {
            // Index first, and if successful send to primary storage - taking this approach vs. the inverted as the indexes are simply pointers to the
            // primary storage and do not require thread syncronization, so reverting those is relatively painless and side-effect free compared to the possibility
            // of something existing in primary storage for a while, getting read possibly and used, then having to revert because something went wrong with
            // indexing.  An alternate approach here could be to lock things for the duration of the add/update, but that would be either fairly broad (i.e. a
            // lock on an entire symbol), or fairly complex (locking portions of a symbol, i.e. a symbol and quote combination), which...why?

            _quoteIndexService.AddOrUpdate(quote);

            try
            {
                _quoteStorageService.AddOrUpdate(quote);
            }
            catch when(RemoveFromIndexAndReturnFalse(quote))
            { // Unreachable throw - the above when filter will remove from the index and return false, which allows us to ensure index removal witout
                // unwinding the call stack just to throw an exception - as is, it'll just bubble out of here like an unhandled exception
                throw;
            }
        }

        public void Remove(T quote)
        {
            if (quote == null)
            {
                return;
            }

            // Purposely not adding try/catch semantics here - if the first call fails, we do not want to remove from the index. If the first call succeeds but the index
            // removal fails, we dont bother trying to re-add the primary back, as the index is just a pointer to the primary...
            _quoteStorageService.Remove(quote);
            _quoteIndexService.Remove(quote);
        }

        public IList<T> RemoveAll(string symbol)
            => _quoteStorageService.RemoveAll(symbol);

        public void Update(T quote)
            => _quoteStorageService.Update(quote);

        public T GetQuoteBySymbolId(string symbol, Guid id)
            => string.IsNullOrEmpty(symbol)
                   ? null
                   : _quoteStorageService.GetQuoteBySymbolId(symbol, id);

        private bool RemoveFromIndexAndReturnFalse(T quote)
        {
            _quoteIndexService.Remove(quote);

            return false;
        }
    }
}
