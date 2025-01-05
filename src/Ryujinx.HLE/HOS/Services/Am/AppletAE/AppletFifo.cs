using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Ryujinx.HLE.HOS.Services.Am.AppletAE
{
    internal class AppletFifo<T> : IAppletFifo<T>
    {
        private readonly ConcurrentQueue<T> _dataQueue;

        public event EventHandler DataAvailable;

        public bool IsSynchronized
        {
            get { return ((ICollection)_dataQueue).IsSynchronized; }
        }

        public object SyncRoot
        {
            get { return ((ICollection)_dataQueue).SyncRoot; }
        }

        public int Count
        {
            get { return _dataQueue.Count; }
        }

        public AppletFifo()
        {
            _dataQueue = new ConcurrentQueue<T>();
        }

        public void Push(T item)
        {
            _dataQueue.Enqueue(item);

            DataAvailable?.Invoke(this, null);
        }

        public bool TryAdd(T item)
        {
            try
            {
                this.Push(item);

                return true;
            }
            catch
            {
                return false;
            }
        }

        public T Pop()
        {
            if (_dataQueue.TryDequeue(out T result))
            {
                return result;
            }

            throw new InvalidOperationException("FIFO empty.");
        }

        public bool TryPop(out T result)
        {
            return _dataQueue.TryDequeue(out result);
        }

        public bool TryTake(out T item)
        {
            return this.TryPop(out item);
        }

        public T Peek()
        {
            if (_dataQueue.TryPeek(out T result))
            {
                return result;
            }

            throw new InvalidOperationException("FIFO empty.");
        }

        public bool TryPeek(out T result)
        {
            return _dataQueue.TryPeek(out result);
        }

        public void Clear()
        {
            _dataQueue.Clear();
        }

        public T[] ToArray()
        {
            return [.. _dataQueue];
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            _dataQueue.CopyTo(array, arrayIndex);
        }

        public void CopyTo(Array array, int index)
        {
            this.CopyTo((T[])array, index);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _dataQueue.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _dataQueue.GetEnumerator();
        }
    }
}
