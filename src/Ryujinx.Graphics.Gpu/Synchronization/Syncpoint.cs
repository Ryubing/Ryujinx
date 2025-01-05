using System;
using System.Collections.Generic;
using System.Threading;

namespace Ryujinx.Graphics.Gpu.Synchronization
{
    /// <summary>
    /// Represents GPU hardware syncpoint.
    /// </summary>
    class Syncpoint
    {
        private int _storedValue;

        public readonly uint Id;

        /// <summary>
        /// The value of the syncpoint.
        /// </summary>
        public uint Value => (uint)_storedValue;

        // TODO: switch to something handling concurrency?
        private readonly List<SyncpointWaiterHandle> _waiters;

        public Syncpoint(uint id)
        {
            Id = id;
            _waiters = [];
        }

        /// <summary>
        /// Register a new callback for a target threshold.
        /// The callback will be called once the threshold is reached and will automatically be unregistered.
        /// </summary>
        /// <param name="threshold">The target threshold</param>
        /// <param name="callback">The callback to call when the threshold is reached</param>
        /// <returns>The created SyncpointWaiterHandle object or null if already past threshold</returns>
        public SyncpointWaiterHandle RegisterCallback(uint threshold, Action<SyncpointWaiterHandle> callback)
        {
            lock (_waiters)
            {
                if (Value >= threshold)
                {
                    callback(null);

                    return null;
                }
                else
                {
                    SyncpointWaiterHandle waiterInformation = new()
                    {
                        Threshold = threshold,
                        Callback = callback,
                    };

                    _waiters.Add(waiterInformation);

                    return waiterInformation;
                }
            }
        }

        public void UnregisterCallback(SyncpointWaiterHandle waiterInformation)
        {
            lock (_waiters)
            {
                _waiters.Remove(waiterInformation);
            }
        }

        /// <summary>
        /// Increment the syncpoint
        /// </summary>
        /// <returns>The incremented value of the syncpoint</returns>
        public uint Increment()
        {
            uint currentValue = (uint)Interlocked.Increment(ref _storedValue);

            SyncpointWaiterHandle expired = null;
            List<SyncpointWaiterHandle> expiredList = null;

            lock (_waiters)
            {
                _waiters.RemoveAll(item =>
                {
                    bool isPastThreshold = currentValue >= item.Threshold;

                    if (isPastThreshold)
                    {
                        if (expired == null)
                        {
                            expired = item;
                        }
                        else
                        {
                            expiredList ??= [];

                            expiredList.Add(item);
                        }
                    }

                    return isPastThreshold;
                });
            }

            // Call the callbacks as a separate step.
            // As we don't know what the callback will be doing,
            // and it could block execution for a indefinite amount of time,
            // we can't call it inside the lock.
            if (expired != null)
            {
                expired.Callback(expired);

                if (expiredList != null)
                {
                    for (int i = 0; i < expiredList.Count; i++)
                    {
                        expiredList[i].Callback(expiredList[i]);
                    }
                }
            }

            return currentValue;
        }
    }
}
