using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Samples.QuoteManager.Recommended
{
    internal class InMemorySortedQuoteStorageService<T> : IQuoteStorageService<T>
        where T : class, IQuoteExtended
    {
        private const double _priceSortIncrement = 0.0000000001;
        private const double _priceFidelityDiff = 0.00000001;

        private readonly ConcurrentDictionary<string, SortedList<double, T>> _quotesBySymbol = new ConcurrentDictionary<string, SortedList<double, T>>(StringComparer.OrdinalIgnoreCase);

        public IEnumerable<T> GetAllQuotes()
            => _quotesBySymbol.Values.SelectMany(v => v.Values);

        public T GetQuoteById(Guid id)
        {   // If we use this storage to get the quote by id, it's gonna be a long loop...
            foreach (var symbolQuotes in _quotesBySymbol)
            {
                var quote = GetQuoteByIdFromList(symbolQuotes.Value, id);

                if (quote != null)
                {
                    return quote;
                }
            }

            return null;
        }

        public T GetQuoteBySymbolId(string symbol, Guid id)
            => string.IsNullOrEmpty(symbol)
                   ? null
                   : _quotesBySymbol.TryGetValue(symbol, out var quotes)
                       ? GetQuoteByIdFromList(quotes, id)
                       : null;

        public void Update(T quote)
        {
            // Purposely a no-op on this in-memory implementation, as updates to the quote object itself will reflect in the storage already (it's a ref)...
        }

        public void AddOrUpdate(T quote)
        {
            if (string.IsNullOrEmpty(quote?.Symbol))
            {
                return;
            }

            _quotesBySymbol.AddOrUpdate(quote.Symbol,
                                        new SortedList<double, T>
                                        {
                                            {
                                                quote.Price, quote
                                            }
                                        },
                                        (symbolKey, list) =>
                                        {
                                            // For purposes of this implementation, simply assuming that the tie breaker on quotes with the same price is the
                                            // time they were entered (earlier quotes being the earlier to be fulfilled)
                                            var sortKey = quote.Price;

                                            do
                                            {
                                                if (!list.ContainsKey(sortKey))
                                                {
                                                    list.Add(sortKey, quote);

                                                    return list;
                                                }

                                                // Increment the sort key by the tinyest of amounts
                                                sortKey += _priceSortIncrement;
                                            } while (true);
                                        });
        }

        public IList<T> RemoveAll(string symbol)
        {
            if (string.IsNullOrEmpty(symbol))
            {
                return null;
            }

            return _quotesBySymbol.TryRemove(symbol, out var removed)
                       ? removed.Values
                       : null;
        }

        public void Remove(T quote)
        {
            if (string.IsNullOrEmpty(quote.Symbol))
            {
                return;
            }

            // Use addOrUpdate vs. update as this has factory access on update, wheras update only allows replacing the entire value...
            _quotesBySymbol.AddOrUpdate(quote.Symbol,
                                        new SortedList<double, T>(),
                                        (symbolKey, list) =>
                                        {
                                            var quoteIndex = list.IndexOfKey(quote.Price);

                                            while (quoteIndex >= 0)
                                            {
                                                var existingQuote = list.Values[quoteIndex];

                                                if (Math.Abs(existingQuote.Price - quote.Price) > _priceFidelityDiff)
                                                {
                                                    // Different priced quote...all done
                                                    return list;
                                                }

                                                if (quote.Id == existingQuote.Id)
                                                {
                                                    existingQuote.IsDeleted = true;

                                                    return list;
                                                }

                                                // Wasn't the one, continue to the next index in the list for comparison
                                                quoteIndex++;
                                            }

                                            return list;
                                        });
        }

        public IList<T> GetQuotesByPrice(string symbol)
        {
            if (string.IsNullOrEmpty(symbol))
            {
                return null;
            }

            return _quotesBySymbol.TryGetValue(symbol, out var list)
                       ? list.Values
                       : null;
        }

        private T GetQuoteByIdFromList(SortedList<double, T> list, Guid id)
        {
            if (list == null || list.Count == 0)
            {
                return null;
            }

            var currentQuotesLength = list.Count;

            for (var quoteIndex = 0; quoteIndex < currentQuotesLength; quoteIndex++)
            {
                var quote = list.Values.Count > quoteIndex
                                ? list.Values[quoteIndex]
                                : null;

                if (quote != null && quote.Id == id)
                {
                    return quote;
                }
            }

            return null;
        }

    }
}
