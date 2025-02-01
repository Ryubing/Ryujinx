using Ryujinx.Common;
using Ryujinx.HLE.HOS.Kernel.Threading;
using Ryujinx.Horizon.Common;
using System.Collections.Generic;

namespace Ryujinx.HLE.HOS.Kernel.Common
{
    class KResourceLimit : KAutoObject
    {
        private const int DefaultTimeoutMs = 10000; // 10s

        private readonly long[] _current;
        private readonly long[] _limit;
        private readonly long[] _current2;
        private readonly long[] _peak;

        // type is not Lock due to Monitor class usage
        private readonly object _lock = new();

        private readonly LinkedList<KThread> _waitingThreads;

        private int _waitingThreadsCount;

        public KResourceLimit(KernelContext context) : base(context)
        {
            _current = new long[(int)LimitableResource.Count];
            _limit = new long[(int)LimitableResource.Count];
            _current2 = new long[(int)LimitableResource.Count];
            _peak = new long[(int)LimitableResource.Count];

            _waitingThreads = [];
        }

        public bool Reserve(LimitableResource resource, ulong amount)
        {
            return Reserve(resource, (long)amount);
        }

        public bool Reserve(LimitableResource resource, long amount)
        {
            return Reserve(resource, amount, KTimeManager.ConvertMillisecondsToNanoseconds(DefaultTimeoutMs));
        }

        public bool Reserve(LimitableResource resource, long amount, long timeout)
        {
            long endTimePoint = KTimeManager.ConvertNanosecondsToMilliseconds(timeout);

            endTimePoint += PerformanceCounter.ElapsedMilliseconds;

            bool success = false;

            int index = GetIndex(resource);

            lock (_lock)
            {
                if (_current2[index] >= _limit[index])
                {
                    return false;
                }

                long newCurrent = _current[index] + amount;

                while (newCurrent > _limit[index] && _current2[index] + amount <= _limit[index])
                {
                    _waitingThreadsCount++;

                    KConditionVariable.Wait(KernelContext, _waitingThreads, _lock, timeout);

                    _waitingThreadsCount--;

                    newCurrent = _current[index] + amount;

                    if (timeout >= 0 && PerformanceCounter.ElapsedMilliseconds > endTimePoint)
                    {
                        break;
                    }
                }

                if (newCurrent <= _limit[index])
                {
                    _current[index] = newCurrent;
                    _current2[index] += amount;

                    if (_current[index] > _peak[index])
                    {
                        _peak[index] = _current[index];
                    }

                    success = true;
                }
            }

            return success;
        }

        public void Release(LimitableResource resource, ulong amount)
        {
            Release(resource, (long)amount);
        }

        public void Release(LimitableResource resource, long amount)
        {
            Release(resource, amount, amount);
        }

        public void Release(LimitableResource resource, long amount, long amount2)
        {
            int index = GetIndex(resource);

            lock (_lock)
            {
                _current[index] -= amount;
                _current2[index] -= amount2;

                if (_waitingThreadsCount > 0)
                {
                    KConditionVariable.NotifyAll(KernelContext, _waitingThreads);
                }
            }
        }

        public long GetRemainingValue(LimitableResource resource)
        {
            int index = GetIndex(resource);

            lock (_lock)
            {
                return _limit[index] - _current[index];
            }
        }

        public long GetCurrentValue(LimitableResource resource)
        {
            int index = GetIndex(resource);

            lock (_lock)
            {
                return _current[index];
            }
        }

        public long GetLimitValue(LimitableResource resource)
        {
            int index = GetIndex(resource);

            lock (_lock)
            {
                return _limit[index];
            }
        }

        public long GetPeakValue(LimitableResource resource)
        {
            int index = GetIndex(resource);

            lock (_lock)
            {
                return _peak[index];
            }
        }

        public Result SetLimitValue(LimitableResource resource, long limit)
        {
            int index = GetIndex(resource);

            lock (_lock)
            {
                if (_current[index] <= limit)
                {
                    _limit[index] = limit;
                    _peak[index] = _current[index];

                    return Result.Success;
                }
                else
                {
                    return KernelResult.InvalidState;
                }
            }
        }

        private static int GetIndex(LimitableResource resource)
        {
            return (int)resource;
        }
    }
}
