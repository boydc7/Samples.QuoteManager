using System;

namespace Samples.QuoteManager
{
    internal class NullQuoteManager : IQuoteManager
    {
        private NullQuoteManager() { }

        public static NullQuoteManager Instance { get; } = new NullQuoteManager();

        public void AddOrUpdateQuote(IQuote quote) { }

        public void RemoveQuote(Guid id) { }

        public void RemoveAllQuotes(string symbol) { }

        public IQuote GetBestQuoteWithAvailableVolume(string symbol)
            => null;

        public ITradeResult ExecuteTrade(string symbol, uint volumeRequested)
            => null;
    }
}
