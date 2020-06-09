namespace Samples.QuoteManager.Recommended
{
    internal interface ITradeExecuter<T>
        where T : class, IQuoteExtended
    {
        ITradeResult ExecuteTrade(string symbol, uint volumeRequested, ILockManager lockManager, IQuoteStorageService<T> quoteStorageService);
    }
}
