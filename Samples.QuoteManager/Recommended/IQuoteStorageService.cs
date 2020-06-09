using System;
using System.Collections.Generic;

namespace Samples.QuoteManager.Recommended
{
    internal interface IQuoteStorageService<T>
        where T : class, IQuote
    {
        IEnumerable<T> GetAllQuotes();
        T GetQuoteById(Guid id);
        T GetQuoteBySymbolId(string symbol, Guid id);
        IList<T> GetQuotesByPrice(string symbol);
        void AddOrUpdate(T quote);
        void Remove(T quote);
        IList<T> RemoveAll(string symbol);
        void Update(T quote);
    }
}
