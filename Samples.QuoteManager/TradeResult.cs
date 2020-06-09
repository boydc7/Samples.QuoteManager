using System;

namespace Samples.QuoteManager
{
    internal interface ITradeResult
    {
        Guid Id { get; set; }
        string Symbol { get; set; }
        double VolumeWeightedAveragePrice { get; set; }
        uint VolumeRequested { get; set; }
        uint VolumeExecuted { get; set; }
    }

    internal class TradeResult : ITradeResult
    {
        public Guid Id { get; set; }
        public string Symbol { get; set; }
        public double VolumeWeightedAveragePrice { get; set; }
        public uint VolumeRequested { get; set; }
        public uint VolumeExecuted { get; set; }
    }
}
