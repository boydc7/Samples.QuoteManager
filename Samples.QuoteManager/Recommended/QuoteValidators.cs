using System;
using System.Collections.Generic;

namespace Samples.QuoteManager.Recommended
{
    internal static class QuoteValidators
    {
        internal static IEnumerable<Func<T, bool>> DefaultQuoteValidators<T>()
            where T : IQuoteExtended
        {
            yield return QuoteIsNotNull;
            yield return QuoteIsNotDeleted;
            yield return QuoteIsNotExpired;
            yield return QuoteHasVolume;
        }

        private static bool QuoteIsNotNull<T>(T quote)
            where T : IQuoteExtended
            => quote != null;

        private static bool QuoteIsNotDeleted<T>(T quote)
            where T : IQuoteExtended
            => !quote.IsDeleted;

        private static bool QuoteIsNotExpired<T>(T quote)
            where T : IQuoteExtended
            => quote.ExpirationTimestamp > UnixTime.NowSeconds;

        private static bool QuoteHasVolume<T>(T quote)
            where T : IQuoteExtended
            => quote.AvailableVolume > 0;
    }
}
