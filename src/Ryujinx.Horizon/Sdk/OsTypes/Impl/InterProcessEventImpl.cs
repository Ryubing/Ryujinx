using Ryujinx.Horizon.Common;
using System;

namespace Ryujinx.Horizon.Sdk.OsTypes.Impl
{
    static class InterProcessEventImpl
    {
        public static Result Create(out int writableHandle, out int readableHandle)
        {
            Result result = HorizonStatic.Syscall.CreateEvent(out writableHandle, out readableHandle);

            if (result == KernelResult.OutOfResource)
            {
                return OsResult.OutOfResource;
            }

            result.AbortOnFailure();

            return Result.Success;
        }

        public static void Close(int handle)
        {
            if (handle != 0)
            {
                HorizonStatic.Syscall.CloseHandle(handle).AbortOnFailure();
            }
        }

        public static void Signal(int handle)
        {
            HorizonStatic.Syscall.SignalEvent(handle).AbortOnFailure();
        }

        public static void Clear(int handle)
        {
            HorizonStatic.Syscall.ClearEvent(handle).AbortOnFailure();
        }

        public static void Wait(int handle, bool autoClear)
        {
            Span<int> handles = [handle];

            while (true)
            {
                Result result = HorizonStatic.Syscall.WaitSynchronization(out _, handles, -1L);

                if (result == Result.Success)
                {
                    if (autoClear)
                    {
                        result = HorizonStatic.Syscall.ResetSignal(handle);

                        if (result == KernelResult.InvalidState)
                        {
                            continue;
                        }

                        result.AbortOnFailure();
                    }

                    return;
                }

                result.AbortUnless(KernelResult.Cancelled);
            }
        }

        public static bool TryWait(int handle, bool autoClear)
        {
            if (autoClear)
            {
                return HorizonStatic.Syscall.ResetSignal(handle) == Result.Success;
            }

            Span<int> handles = [handle];

            while (true)
            {
                Result result = HorizonStatic.Syscall.WaitSynchronization(out _, handles, 0);

                if (result == Result.Success)
                {
                    return true;
                }
                else if (result == KernelResult.TimedOut)
                {
                    return false;
                }

                result.AbortUnless(KernelResult.Cancelled);
            }
        }

        public static bool TimedWait(int handle, bool autoClear, TimeSpan timeout)
        {
            Span<int> handles = [handle];

            long timeoutNs = timeout.Milliseconds * 1000000L;

            while (true)
            {
                Result result = HorizonStatic.Syscall.WaitSynchronization(out _, handles, timeoutNs);

                if (result == Result.Success)
                {
                    if (autoClear)
                    {
                        result = HorizonStatic.Syscall.ResetSignal(handle);

                        if (result == KernelResult.InvalidState)
                        {
                            continue;
                        }

                        result.AbortOnFailure();
                    }

                    return true;
                }
                else if (result == KernelResult.TimedOut)
                {
                    return false;
                }

                result.AbortUnless(KernelResult.Cancelled);
            }
        }
    }
}
