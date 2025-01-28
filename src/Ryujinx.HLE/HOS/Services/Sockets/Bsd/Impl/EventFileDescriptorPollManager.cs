using Ryujinx.Common.Logging;
using Ryujinx.HLE.HOS.Services.Sockets.Bsd.Types;
using System.Collections.Generic;
using System.Threading;

namespace Ryujinx.HLE.HOS.Services.Sockets.Bsd.Impl
{
    class EventFileDescriptorPollManager : IPollManager
    {
        private static EventFileDescriptorPollManager _instance;

        public static EventFileDescriptorPollManager Instance
        {
            get
            {
                _instance ??= new EventFileDescriptorPollManager();

                return _instance;
            }
        }

        public bool IsCompatible(PollEvent evnt)
        {
            return evnt.FileDescriptor is EventFileDescriptor;
        }

        public LinuxError Poll(List<PollEvent> events, int timeoutMilliseconds, out int updatedCount)
        {
            updatedCount = 0;

            List<ManualResetEvent> waiters = [];

            for (int i = 0; i < events.Count; i++)
            {
                PollEvent evnt = events[i];

                EventFileDescriptor socket = (EventFileDescriptor)evnt.FileDescriptor;

                bool isValidEvent = false;

                if (evnt.Data.InputEvents.HasFlag(PollEventTypeMask.Input) ||
                    evnt.Data.InputEvents.HasFlag(PollEventTypeMask.UrgentInput))
                {
                    waiters.Add(socket.ReadEvent);

                    isValidEvent = true;
                }
                if (evnt.Data.InputEvents.HasFlag(PollEventTypeMask.Output))
                {
                    waiters.Add(socket.WriteEvent);

                    isValidEvent = true;
                }

                if (!isValidEvent)
                {
                    Logger.Warning?.Print(LogClass.ServiceBsd, $"Unsupported Poll input event type: {evnt.Data.InputEvents}");

                    return LinuxError.EINVAL;
                }
            }

            int index = WaitHandle.WaitAny(waiters.ToArray(), timeoutMilliseconds);

            if (index != WaitHandle.WaitTimeout)
            {
                for (int i = 0; i < events.Count; i++)
                {
                    PollEventTypeMask outputEvents = 0;

                    PollEvent evnt = events[i];

                    EventFileDescriptor socket = (EventFileDescriptor)evnt.FileDescriptor;

                    if (socket.ReadEvent.WaitOne(0))
                    {
                        if (evnt.Data.InputEvents.HasFlag(PollEventTypeMask.Input))
                        {
                            outputEvents |= PollEventTypeMask.Input;
                        }

                        if (evnt.Data.InputEvents.HasFlag(PollEventTypeMask.UrgentInput))
                        {
                            outputEvents |= PollEventTypeMask.UrgentInput;
                        }
                    }

                    if ((evnt.Data.InputEvents.HasFlag(PollEventTypeMask.Output))
                        && socket.WriteEvent.WaitOne(0))
                    {
                        outputEvents |= PollEventTypeMask.Output;
                    }


                    if (outputEvents != 0)
                    {
                        evnt.Data.OutputEvents = outputEvents;

                        updatedCount++;
                    }
                }
            }
            else
            {
                return LinuxError.ETIMEDOUT;
            }

            return LinuxError.SUCCESS;
        }

        public LinuxError Select(List<PollEvent> events, int timeout, out int updatedCount)
        {
            // TODO: Implement Select for event file descriptors
            updatedCount = 0;

            return LinuxError.EOPNOTSUPP;
        }
    }
}
