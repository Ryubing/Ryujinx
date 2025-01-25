using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Ryujinx.Common
{
    public sealed class AsyncWorkQueue<T> : IDisposable
    {
        private readonly Thread _workerThread;
        private readonly CancellationTokenSource _cts;
        private readonly Action<T> _workerAction;
        private readonly BlockingCollection<T> _queue;

        public bool IsCancellationRequested => _cts.IsCancellationRequested;

        public AsyncWorkQueue(Action<T> callback, string name = null) : this(callback, name, new BlockingCollection<T>())
        {
        }

        public AsyncWorkQueue(Action<T> callback, string name, BlockingCollection<T> collection)
        {
            _cts = new CancellationTokenSource();
            _queue = collection;
            _workerAction = callback;
            _workerThread = new Thread(DoWork)
            {
                Name = name,
                IsBackground = true,
            };
            _workerThread.Start();
        }

        private void DoWork()
        {
            try
            {
                foreach (T item in _queue.GetConsumingEnumerable(_cts.Token))
                {
                    _workerAction(item);
                }
            }
            catch (OperationCanceledException)
            {
            }
        }

        public void Cancel()
        {
            _cts.Cancel();
        }

        public void CancelAfter(int millisecondsDelay)
        {
            _cts.CancelAfter(millisecondsDelay);
        }

        public void CancelAfter(TimeSpan delay)
        {
            _cts.CancelAfter(delay);
        }

        public void Add(T workItem)
        {
            _queue.Add(workItem);
        }

        public void Add(T workItem, CancellationToken cancellationToken)
        {
            _queue.Add(workItem, cancellationToken);
        }

        public bool TryAdd(T workItem)
        {
            return _queue.TryAdd(workItem);
        }

        public bool TryAdd(T workItem, int millisecondsDelay)
        {
            return _queue.TryAdd(workItem, millisecondsDelay);
        }

        public bool TryAdd(T workItem, int millisecondsDelay, CancellationToken cancellationToken)
        {
            return _queue.TryAdd(workItem, millisecondsDelay, cancellationToken);
        }

        public bool TryAdd(T workItem, TimeSpan timeout)
        {
            return _queue.TryAdd(workItem, timeout);
        }

        public void Dispose()
        {
            _queue.CompleteAdding();
            _cts.Cancel();
            _workerThread.Join();

            _queue.Dispose();
            _cts.Dispose();
        }
    }
}
