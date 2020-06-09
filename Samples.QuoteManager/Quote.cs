using System;
using Samples.QuoteManager.Recommended;

namespace Samples.QuoteManager
{
    internal interface IQuote
    {
        Guid Id { get; set; }
        string Symbol { get; set; }
        double Price { get; set; }
        uint AvailableVolume { get; set; }
        DateTime ExpirationDate { get; set; }
    }

    internal class Quote : IQuote
    {
        public Guid Id { get; set; }
        public string Symbol { get; set; }
        public double Price { get; set; }
        public uint AvailableVolume { get; set; }
        public DateTime ExpirationDate { get; set; }
    }

    internal interface IQuoteExtended : IQuote, ICloneable
    {
        long ExpirationTimestamp { get; set; }
        bool IsDeleted { get; set; }
    }

    internal class QuoteExtended : Quote, IQuoteExtended
    {
        internal QuoteExtended() { }

        internal QuoteExtended(IQuote quote)
        {
            Id = quote.Id;
            Symbol = quote.Symbol;
            Price = quote.Price;
            AvailableVolume = quote.AvailableVolume;
            ExpirationDate = quote.ExpirationDate;
            ExpirationTimestamp = ExpirationDate.ToUnixTimestamp();
        }

        public long ExpirationTimestamp { get; set; }
        public bool IsDeleted { get; set; }

        public object Clone()
            => new QuoteExtended(this)
               {
                   IsDeleted = IsDeleted
               };
    }
}
