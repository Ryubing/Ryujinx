using Ryujinx.Horizon.Common;
using Ryujinx.Horizon.Sdk.OsTypes.Impl;
using System;

namespace Ryujinx.Horizon.Sdk.OsTypes
{
    static partial class Os
    {
        public static Result CreateSystemEvent(out SystemEventType sysEvent, EventClearMode clearMode, bool interProcess)
        {
            sysEvent = new SystemEventType();

            if (interProcess)
            {
                Result result = InterProcessEvent.Create(ref sysEvent.InterProcessEvent, clearMode);

                if (result != Result.Success)
                {
                    return result;
                }

                sysEvent.State = SystemEventType.InitializationState.InitializedAsInterProcess;
            }
            else
            {
                throw new NotImplementedException();
            }

            return Result.Success;
        }

        public static void DestroySystemEvent(ref SystemEventType sysEvent)
        {
            SystemEventType.InitializationState oldState = sysEvent.State;
            sysEvent.State = SystemEventType.InitializationState.NotInitialized;

            switch (oldState)
            {
                case SystemEventType.InitializationState.InitializedAsInterProcess:
                    InterProcessEvent.Destroy(ref sysEvent.InterProcessEvent);
                    break;
            }
        }

        public static int DetachReadableHandleOfSystemEvent(ref SystemEventType sysEvent)
        {
            return InterProcessEvent.DetachReadableHandle(ref sysEvent.InterProcessEvent);
        }

        public static int DetachWritableHandleOfSystemEvent(ref SystemEventType sysEvent)
        {
            return InterProcessEvent.DetachWritableHandle(ref sysEvent.InterProcessEvent);
        }

        public static int GetReadableHandleOfSystemEvent(ref SystemEventType sysEvent)
        {
            return InterProcessEvent.GetReadableHandle(ref sysEvent.InterProcessEvent);
        }

        public static int GetWritableHandleOfSystemEvent(ref SystemEventType sysEvent)
        {
            return InterProcessEvent.GetWritableHandle(ref sysEvent.InterProcessEvent);
        }

        public static void SignalSystemEvent(ref SystemEventType sysEvent)
        {
            switch (sysEvent.State)
            {
                case SystemEventType.InitializationState.InitializedAsInterProcess:
                    InterProcessEvent.Signal(ref sysEvent.InterProcessEvent);
                    break;
            }
        }

        public static void ClearSystemEvent(ref SystemEventType sysEvent)
        {
            switch (sysEvent.State)
            {
                case SystemEventType.InitializationState.InitializedAsInterProcess:
                    InterProcessEvent.Clear(ref sysEvent.InterProcessEvent);
                    break;
            }
        }
    }
}
