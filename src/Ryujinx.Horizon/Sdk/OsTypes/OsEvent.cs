using System;
using System.Threading;

namespace Ryujinx.Horizon.Sdk.OsTypes
{
    static partial class Os
    {
        public static void InitializeEvent(out EventType evnt, bool signaled, EventClearMode clearMode)
        {
            evnt = new EventType
            {
                MultiWaitHolders = [],
                Signaled = signaled,
                InitiallySignaled = signaled,
                ClearMode = clearMode,
                State = InitializationState.Initialized,
                Lock = new object(),
            };
        }

        public static void FinalizeEvent(ref EventType evnt)
        {
            evnt.State = InitializationState.NotInitialized;
        }

        public static void WaitEvent(ref EventType evnt)
        {
            lock (evnt.Lock)
            {
                ulong currentCounter = evnt.BroadcastCounter;

                while (!evnt.Signaled)
                {
                    if (currentCounter != evnt.BroadcastCounter)
                    {
                        break;
                    }

                    Monitor.Wait(evnt.Lock);
                }

                if (evnt.ClearMode == EventClearMode.AutoClear)
                {
                    evnt.Signaled = false;
                }
            }
        }

        public static bool TryWaitEvent(ref EventType evnt)
        {
            lock (evnt.Lock)
            {
                bool signaled = evnt.Signaled;

                if (evnt.ClearMode == EventClearMode.AutoClear)
                {
                    evnt.Signaled = false;
                }

                return signaled;
            }
        }

        public static bool TimedWaitEvent(ref EventType evnt, TimeSpan timeout)
        {
            lock (evnt.Lock)
            {
                ulong currentCounter = evnt.BroadcastCounter;

                while (!evnt.Signaled)
                {
                    if (currentCounter != evnt.BroadcastCounter)
                    {
                        break;
                    }

                    bool wasSignaledInTime = Monitor.Wait(evnt.Lock, timeout);
                    if (!wasSignaledInTime)
                    {
                        return false;
                    }
                }

                if (evnt.ClearMode == EventClearMode.AutoClear)
                {
                    evnt.Signaled = false;
                }
            }

            return true;
        }

        public static void SignalEvent(ref EventType evnt)
        {
            lock (evnt.Lock)
            {
                if (evnt.Signaled)
                {
                    return;
                }

                evnt.Signaled = true;

                if (evnt.ClearMode == EventClearMode.ManualClear)
                {
                    evnt.BroadcastCounter++;
                    Monitor.PulseAll(evnt.Lock);
                }
                else
                {
                    Monitor.Pulse(evnt.Lock);
                }

                foreach (MultiWaitHolderBase holder in evnt.MultiWaitHolders)
                {
                    holder.GetMultiWait().NotifyAndWakeUpThread(holder);
                }
            }
        }

        public static void ClearEvent(ref EventType evnt)
        {
            lock (evnt.Lock)
            {
                evnt.Signaled = false;
            }
        }
    }
}
