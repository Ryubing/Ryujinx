using Ryujinx.Horizon.Common;
using System;

namespace Ryujinx.Horizon.Sdk.Sf.Hipc
{
    static class Api
    {
        public const int TlsMessageBufferSize = 0x100;

        public static Result Receive(out ReceiveResult recvResult, int sessionHandle, Span<byte> messageBuffer)
        {
            Result result = ReceiveImpl(sessionHandle, messageBuffer);

            if (result == KernelResult.PortRemoteClosed)
            {
                recvResult = ReceiveResult.Closed;

                return Result.Success;
            }
            else if (result == KernelResult.ReceiveListBroken)
            {
                recvResult = ReceiveResult.NeedsRetry;

                return Result.Success;
            }

            recvResult = ReceiveResult.Success;

            return result;
        }

        private static Result ReceiveImpl(int sessionHandle, Span<byte> messageBuffer)
        {
            Span<int> handles = stackalloc int[1];

            handles[0] = sessionHandle;

            ReadOnlySpan<byte> tlsSpan = HorizonStatic.AddressSpace.GetSpan(HorizonStatic.ThreadContext.TlsAddress, TlsMessageBufferSize);

            if (messageBuffer == tlsSpan)
            {
                return HorizonStatic.Syscall.ReplyAndReceive(out _, handles, 0, -1L);
            }

            throw new NotImplementedException();
        }

        public static Result Reply(int sessionHandle, ReadOnlySpan<byte> messageBuffer)
        {
            Result result = ReplyImpl(sessionHandle, messageBuffer);

            result.AbortUnless(KernelResult.TimedOut, KernelResult.PortRemoteClosed);

            return Result.Success;
        }

        private static Result ReplyImpl(int sessionHandle, ReadOnlySpan<byte> messageBuffer)
        {
            ReadOnlySpan<byte> tlsSpan = HorizonStatic.AddressSpace.GetSpan(HorizonStatic.ThreadContext.TlsAddress, TlsMessageBufferSize);

            if (messageBuffer == tlsSpan)
            {
                return HorizonStatic.Syscall.ReplyAndReceive(out _, ReadOnlySpan<int>.Empty, sessionHandle, 0);
            }

            throw new NotImplementedException();
        }

        public static Result CreateSession(out int serverHandle, out int clientHandle)
        {
            Result result = HorizonStatic.Syscall.CreateSession(out serverHandle, out clientHandle, false, null);

            if (result == KernelResult.OutOfResource)
            {
                return HipcResult.OutOfSessions;
            }

            return result;
        }
    }
}
