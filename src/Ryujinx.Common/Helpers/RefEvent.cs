using Gommon;
using System.Collections.Generic;
using System.Threading;

namespace Ryujinx.Common.Helper
{
    public class RefEvent<T>
    {
        public delegate void Handler(ref T arg);
        
        private readonly Lock _subLock = new();
        private readonly List<Handler> _subscriptions = [];
    
        public bool HasSubscribers
        {
            get
            {
                lock (_subLock)
                    return _subscriptions.Count != 0;
            }
        }

        public IReadOnlyList<Handler> Subscriptions
        {
            get
            {
                lock (_subLock)
                    return _subscriptions;
            }
        }

        public void Add(Handler subscriber)
        {
            Guard.Require(subscriber, nameof(subscriber));
            lock (_subLock)
                _subscriptions.Add(subscriber);
        }

        public void Remove(Handler subscriber)
        {
            Guard.Require(subscriber, nameof(subscriber));
            lock (_subLock)
                _subscriptions.Remove(subscriber);
        }

        public void Clear()
        {
            lock (_subLock)
                _subscriptions.Clear();
        }

        public void Call(ref T arg)
        {
            foreach (Handler subscription in Subscriptions)
                subscription(ref arg);
        }
    }
}
