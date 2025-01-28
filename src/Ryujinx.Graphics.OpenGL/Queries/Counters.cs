using Ryujinx.Graphics.GAL;
using System;

namespace Ryujinx.Graphics.OpenGL.Queries
{
    class Counters : IDisposable
    {
        private readonly CounterQueue[] _counterQueues;

        public Counters()
        {
            int count = Enum.GetNames<CounterType>().Length;

            _counterQueues = new CounterQueue[count];
        }

        public void Initialize()
        {
            for (int index = 0; index < _counterQueues.Length; index++)
            {
                CounterType type = (CounterType)index;
                _counterQueues[index] = new CounterQueue(type);
            }
        }

        public CounterQueueEvent QueueReport(CounterType type, EventHandler<ulong> resultHandler, float divisor, ulong lastDrawIndex, bool hostReserved)
        {
            return _counterQueues[(int)type].QueueReport(resultHandler, divisor, lastDrawIndex, hostReserved);
        }

        public void QueueReset(CounterType type)
        {
            _counterQueues[(int)type].QueueReset();
        }

        public void Update()
        {
            foreach (CounterQueue queue in _counterQueues)
            {
                queue.Flush(false);
            }
        }

        public void Flush(CounterType type)
        {
            _counterQueues[(int)type].Flush(true);
        }

        public void Dispose()
        {
            foreach (CounterQueue queue in _counterQueues)
            {
                queue.Dispose();
            }
        }
    }
}
