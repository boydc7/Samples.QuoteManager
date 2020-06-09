using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Samples.QuoteManager.Recommended
{
    internal class ReaderWriterLockManager : ILockManager
    {
        private readonly ConcurrentDictionary<string, ReaderWriterLockSlim> _locks =
            new ConcurrentDictionary<string, ReaderWriterLockSlim>(StringComparer.OrdinalIgnoreCase);

        public void EnterRead(string key)
        {
            var lockSlim = _locks.GetOrAdd(key, valueFactory: k => new ReaderWriterLockSlim());

            lockSlim.EnterReadLock();
        }

        public void ExitRead(string key)
        {
            if (_locks.TryGetValue(key, out var lockSlim))
            {
                lockSlim.ExitReadLock();
            }
        }

        public void EnterIntentWrite(string key)
        {
            var lockSlim = _locks.GetOrAdd(key, valueFactory: k => new ReaderWriterLockSlim());

            lockSlim.EnterUpgradeableReadLock();
        }

        public void ExitIntentWrite(string key)
        {
            if (_locks.TryGetValue(key, out var lockSlim))
            {
                lockSlim.ExitUpgradeableReadLock();
            }
        }

        public void EnterWrite(string key)
        {
            var lockSlim = _locks.GetOrAdd(key, valueFactory: k => new ReaderWriterLockSlim());

            lockSlim.EnterWriteLock();
        }

        public void ExitWrite(string key)
        {
            if (_locks.TryGetValue(key, out var lockSlim))
            {
                lockSlim.ExitWriteLock();
            }
        }
    }
}
