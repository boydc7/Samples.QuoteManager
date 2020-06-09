using System;

namespace Samples.QuoteManager.Recommended
{
    public static class UnixTime
    {
        private static readonly long _anchorTickCount;
        private static readonly long _anchorUtcTicks;
        private static readonly long _utcEpochTickOffset;

        static UnixTime()
        {
            var zeroUtc = new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);

            _utcEpochTickOffset = ((long)(DateTime.UnixEpoch - zeroUtc).TotalSeconds) * TimeSpan.TicksPerSecond;

            var loopCount = 0;
            long environmentTickBase;
            DateTime environmentTickBaseUtc;

            do
            {
                environmentTickBase = Environment.TickCount64;
                environmentTickBaseUtc = DateTime.UtcNow;
                var secondTick = Environment.TickCount64;

                if ((secondTick - environmentTickBase) < TimeSpan.TicksPerMillisecond)
                {
                    break;
                }

                loopCount++;
            } while (loopCount <= 100);

            _anchorTickCount = environmentTickBase;
            _anchorUtcTicks = environmentTickBaseUtc.Ticks;
        }

        private static long NowTicks => _anchorUtcTicks + (Environment.TickCount64 - _anchorTickCount);

        public static long NowMilliseconds => (NowTicks - _utcEpochTickOffset) / TimeSpan.TicksPerMillisecond;
        public static long NowSeconds => (NowTicks - _utcEpochTickOffset) / TimeSpan.TicksPerSecond;

        public static DateTime AsUtc(this DateTime dateTime)
        {
            if (dateTime.Kind == DateTimeKind.Utc)
            {
                return dateTime;
            }

            return new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, dateTime.Hour, dateTime.Minute,
                                dateTime.Second, dateTime.Millisecond, DateTimeKind.Utc);
        }

        public static long ToUnixTimestamp(this DateTime dateTime)
            => (long)dateTime.ToUniversalTime()
                             .Subtract(DateTime.UnixEpoch)
                             .TotalSeconds;
    }
}
