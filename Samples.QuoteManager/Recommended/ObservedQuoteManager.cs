using System;
using System.Collections.Generic;

namespace Samples.QuoteManager.Recommended
{
    internal interface IQuoteObserver<T>
        where T : IQuoteExtended
    {
        void OnAddOrUpdate(T quote);
        void OnRemove(T quote);
        void OnRemoveAll(string symbol, IList<T> quotes);
        void OnExecuteTrade(ITradeResult result);
        void OnCandidateQuoteMiss(T quote);
    }

    internal class ObservedQuoteManager<T> : IQuoteObserver<T>
        where T : class, IQuoteExtended
    {
        private readonly List<IQuoteObserver<T>> _observers = new List<IQuoteObserver<T>>();

        private ObservedQuoteManager() { }

        internal static (ObservedQuoteManager<T> Observer, IQuoteManagerExtended<T> QuoteManager) Create(Func<IQuoteObserver<T>, IQuoteManagerExtended<T>> managerFactory)
        {
            var observableManager = new ObservedQuoteManager<T>();

            var quoteManager = managerFactory(observableManager);

            return (observableManager, quoteManager);
        }

        public IDisposable Subscribe(IQuoteObserver<T> observer)
        {
            if (!_observers.Contains(observer))
            {
                _observers.Add(observer);
            }

            return new ObservableUnsubscriber(this, observer);
        }

        private void Unsubscribe(IQuoteObserver<T> observer)
        {
            _observers.Remove(observer);
        }

        private class ObservableUnsubscriber : IDisposable
        {
            private readonly ObservedQuoteManager<T> _manager;
            private readonly IQuoteObserver<T> _observer;

            public ObservableUnsubscriber(ObservedQuoteManager<T> manager, IQuoteObserver<T> observer)
            {
                _manager = manager;
                _observer = observer;
            }

            public void Dispose()
            {
                _manager?.Unsubscribe(_observer);
            }
        }

        public void OnAddOrUpdate(T quote)
        {
            foreach (var observer in _observers)
            {
                observer.OnAddOrUpdate(quote);
            }
        }

        public void OnRemove(T quote)
        {
            foreach (var observer in _observers)
            {
                observer.OnRemove(quote);
            }
        }

        public void OnRemoveAll(string symbol, IList<T> quotes)
        {
            foreach (var observer in _observers)
            {
                observer.OnRemoveAll(symbol, quotes);
            }
        }

        public void OnExecuteTrade(ITradeResult result)
        {
            foreach (var observer in _observers)
            {
                observer.OnExecuteTrade(result);
            }
        }

        public void OnCandidateQuoteMiss(T quote)
        {
            foreach (var observer in _observers)
            {
                observer.OnCandidateQuoteMiss(quote);
            }
        }
    }
}
