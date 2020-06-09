using System;
using System.Collections.Generic;
using System.Text.Json;

namespace Samples.QuoteManager.Recommended
{
    internal class ConsoleLoggingQuoteObserver<T> : IQuoteObserver<T>
        where T : IQuoteExtended
    {
        public void OnAddOrUpdate(T quote)
        {
            Console.WriteLine($"  Quote added/updated: [{JsonSerializer.Serialize(quote)}]");
        }

        public void OnRemove(T quote)
        {
            Console.WriteLine($"  Quote removed: [{JsonSerializer.Serialize(quote)}]");
        }

        public void OnRemoveAll(string symbol, IList<T> quotes)
        {
            Console.WriteLine($"  All [{quotes.Count}] quotes removed for symbol [{symbol}]");
        }

        public void OnExecuteTrade(ITradeResult result)
        {
            Console.WriteLine($"  Trade executed: [{JsonSerializer.Serialize(result)}]");
        }

        public void OnCandidateQuoteMiss(T quote)
        {
            Console.WriteLine($"  Candidate quote miss: [{JsonSerializer.Serialize(quote)}]");
        }
    }
}
