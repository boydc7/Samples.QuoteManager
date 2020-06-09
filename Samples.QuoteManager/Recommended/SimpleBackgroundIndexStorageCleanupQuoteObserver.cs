using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Samples.QuoteManager.Recommended
{
    // A simple background-thread-per-operation implementation to background cleanup stuff on a best-try basis
    // No error checking, restarting, etc.
    // No thread pooling, publisher/consumer model, nothing...
    // In the real world this would likely be best implemented in some sort of publisher/consumer pattern...
    internal class SimpleBackgroundIndexStorageCleanupQuoteObserver<T> : IQuoteObserver<T>
        where T : class, IQuoteExtended
    {
        private readonly IQuoteStorageService<T> _storageService;
        private readonly IQuoteSymbolIndexService<T> _indexService;
        private readonly object _lockObject = new object();

        private bool _inCleanup;

        internal SimpleBackgroundIndexStorageCleanupQuoteObserver(IQuoteStorageService<T> storageService,
                                                                  IQuoteSymbolIndexService<T> indexService)
        {
            _storageService = storageService;
            _indexService = indexService;
        }

        public void OnAddOrUpdate(T quote)
        {
            // Nothing to do here
        }

        public void OnRemove(T quote)
        {
            // Nothing to do here
        }

        public void OnRemoveAll(string symbol, IList<T> quotes)
        {
            if (_inCleanup || quotes == null || quotes.Count == 0)
            {
                return;
            }

            lock(_lockObject)
            {
                if (_inCleanup)
                {
                    return;
                }

                _inCleanup = true;
            }

            try
            {
                Task.Run(() => DoCleanup(quotes));
            }
            catch when(ResetInCleanupFlagAndReturnFalse())
            {
                // Unreachable...
                throw;
            }
        }

        public void OnCandidateQuoteMiss(T quote)
        {
            Task.Run(() => DoInspectCandidateMiss(quote));
        }

        public void OnExecuteTrade(ITradeResult result)
        {
            // Could be something here that enqueued quotes that were emptied as a result of the trade, but leaving that alone for now
        }

        private void DoCleanup(IList<T> quotes)
        {
            try
            {
                Console.WriteLine($"  Background index cleanup starting for [{quotes.Count}] quotes");

                foreach (var quote in quotes)
                {
                    _indexService.Remove(quote);
                }
            }
            finally
            {
                _inCleanup = false;

                Console.WriteLine($"  Background index cleanup finished for [{quotes.Count}] quotes");
            }
        }

        private void DoInspectCandidateMiss(T quote)
        {
            if (quote == null)
            {
                return;
            }

            if (quote.IsDeleted || quote.ExpirationTimestamp <= UnixTime.NowSeconds || quote.AvailableVolume <= 0)
            {
                _storageService.Remove(quote);
            }
        }

        private bool ResetInCleanupFlagAndReturnFalse()
        {
            _inCleanup = false;

            return false;
        }
    }
}
