using System;

namespace Samples.QuoteManager.Recommended
{
    internal interface IQuoteSymbolIndexService<in T>
        where T : IQuote
    {
        void AddOrUpdate(T quote);
        void Remove(T quote);
        string GetSymbolForId(Guid id);
    }
}
