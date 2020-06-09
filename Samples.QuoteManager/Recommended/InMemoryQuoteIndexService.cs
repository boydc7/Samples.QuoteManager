using System;
using System.Collections.Generic;

namespace Samples.QuoteManager.Recommended
{
    internal class InMemoryQuoteIndexService<T> : IQuoteSymbolIndexService<T>
        where T : IQuote
    {
        private readonly Dictionary<Guid, string> _quoteIdSymbolMap = new Dictionary<Guid, string>();

        public void AddOrUpdate(T quote)
        {
            _quoteIdSymbolMap[quote.Id] = quote.Symbol;
        }

        public void Remove(T quote)
        {
            _quoteIdSymbolMap.Remove(quote.Id);
        }

        public string GetSymbolForId(Guid id)
            => _quoteIdSymbolMap.ContainsKey(id)
                   ? _quoteIdSymbolMap[id]
                   : null;
    }
}
