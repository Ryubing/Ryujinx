using Ryujinx.Horizon.Common;
using Ryujinx.Horizon.Sdk.Sf.Cmif;
using Ryujinx.Horizon.Sdk.Sf.Hipc;
using Ryujinx.Memory;
using System;

namespace Ryujinx.Horizon.Sdk
{
    static class ServiceUtil
    {
        public static Result SendRequest(out CmifResponse response, int sessionHandle, uint requestId, bool sendPid, scoped ReadOnlySpan<byte> data)
        {
            ulong tlsAddress = HorizonStatic.ThreadContext.TlsAddress;
            int tlsSize = Api.TlsMessageBufferSize;

            using (WritableRegion tlsRegion = HorizonStatic.AddressSpace.GetWritableRegion(tlsAddress, tlsSize))
            {
                CmifRequest request = CmifMessage.CreateRequest(tlsRegion.Memory.Span, new CmifRequestFormat
                {
                    DataSize = data.Length,
                    RequestId = requestId,
                    SendPid = sendPid,
                });

                data.CopyTo(request.Data);
            }

            Result result = HorizonStatic.Syscall.SendSyncRequest(sessionHandle);

            if (result.IsFailure)
            {
                response = default;

                return result;
            }

            return CmifMessage.ParseResponse(out response, HorizonStatic.AddressSpace.GetWritableRegion(tlsAddress, tlsSize).Memory.Span, false, 0);
        }

        public static Result SendRequest(
            out CmifResponse response,
            int sessionHandle,
            uint requestId,
            bool sendPid,
            scoped ReadOnlySpan<byte> data,
            ReadOnlySpan<HipcBufferFlags> bufferFlags,
            ReadOnlySpan<PointerAndSize> buffers)
        {
            ulong tlsAddress = HorizonStatic.ThreadContext.TlsAddress;
            int tlsSize = Api.TlsMessageBufferSize;

            using (WritableRegion tlsRegion = HorizonStatic.AddressSpace.GetWritableRegion(tlsAddress, tlsSize))
            {
                CmifRequestFormat format = new()
                {
                    DataSize = data.Length,
                    RequestId = requestId,
                    SendPid = sendPid,
                };

                for (int index = 0; index < bufferFlags.Length; index++)
                {
                    FormatProcessBuffer(ref format, bufferFlags[index]);
                }

                CmifRequest request = CmifMessage.CreateRequest(tlsRegion.Memory.Span, format);

                for (int index = 0; index < buffers.Length; index++)
                {
                    RequestProcessBuffer(ref request, buffers[index], bufferFlags[index]);
                }

                data.CopyTo(request.Data);
            }

            Result result = HorizonStatic.Syscall.SendSyncRequest(sessionHandle);

            if (result.IsFailure)
            {
                response = default;

                return result;
            }

            return CmifMessage.ParseResponse(out response, HorizonStatic.AddressSpace.GetWritableRegion(tlsAddress, tlsSize).Memory.Span, false, 0);
        }

        private static void FormatProcessBuffer(ref CmifRequestFormat format, HipcBufferFlags flags)
        {
            if (flags == 0)
            {
                return;
            }

            bool isIn = flags.HasFlag(HipcBufferFlags.In);
            bool isOut = flags.HasFlag(HipcBufferFlags.Out);

            if (flags.HasFlag(HipcBufferFlags.AutoSelect))
            {
                if (isIn)
                {
                    format.InAutoBuffersCount++;
                }

                if (isOut)
                {
                    format.OutAutoBuffersCount++;
                }
            }
            else if (flags.HasFlag(HipcBufferFlags.Pointer))
            {
                if (isIn)
                {
                    format.InPointersCount++;
                }

                if (isOut)
                {
                    if (flags.HasFlag(HipcBufferFlags.FixedSize))
                    {
                        format.OutFixedPointersCount++;
                    }
                    else
                    {
                        format.OutPointersCount++;
                    }
                }
            }
            else if (flags.HasFlag(HipcBufferFlags.MapAlias))
            {
                if (isIn && isOut)
                {
                    format.InOutBuffersCount++;
                }
                else if (isIn)
                {
                    format.InBuffersCount++;
                }
                else
                {
                    format.OutBuffersCount++;
                }
            }
        }

        private static void RequestProcessBuffer(ref CmifRequest request, PointerAndSize buffer, HipcBufferFlags flags)
        {
            if (flags == 0)
            {
                return;
            }

            bool isIn = flags.HasFlag(HipcBufferFlags.In);
            bool isOut = flags.HasFlag(HipcBufferFlags.Out);

            if (flags.HasFlag(HipcBufferFlags.AutoSelect))
            {
                HipcBufferMode mode = HipcBufferMode.Normal;

                if (flags.HasFlag(HipcBufferFlags.MapTransferAllowsNonSecure))
                {
                    mode = HipcBufferMode.NonSecure;
                }

                if (flags.HasFlag(HipcBufferFlags.MapTransferAllowsNonDevice))
                {
                    mode = HipcBufferMode.NonDevice;
                }

                if (isIn)
                {
                    RequestInAutoBuffer(ref request, buffer.Address, buffer.Size, mode);
                }

                if (isOut)
                {
                    RequestOutAutoBuffer(ref request, buffer.Address, buffer.Size, mode);
                }
            }
            else if (flags.HasFlag(HipcBufferFlags.Pointer))
            {
                if (isIn)
                {
                    RequestInPointer(ref request, buffer.Address, buffer.Size);
                }

                if (isOut)
                {
                    if (flags.HasFlag(HipcBufferFlags.FixedSize))
                    {
                        RequestOutFixedPointer(ref request, buffer.Address, buffer.Size);
                    }
                    else
                    {
                        RequestOutPointer(ref request, buffer.Address, buffer.Size);
                    }
                }
            }
            else if (flags.HasFlag(HipcBufferFlags.MapAlias))
            {
                HipcBufferMode mode = HipcBufferMode.Normal;

                if (flags.HasFlag(HipcBufferFlags.MapTransferAllowsNonSecure))
                {
                    mode = HipcBufferMode.NonSecure;
                }

                if (flags.HasFlag(HipcBufferFlags.MapTransferAllowsNonDevice))
                {
                    mode = HipcBufferMode.NonDevice;
                }

                if (isIn && isOut)
                {
                    RequestInOutBuffer(ref request, buffer.Address, buffer.Size, mode);
                }
                else if (isIn)
                {
                    RequestInBuffer(ref request, buffer.Address, buffer.Size, mode);
                }
                else
                {
                    RequestOutBuffer(ref request, buffer.Address, buffer.Size, mode);
                }
            }
        }

        private static void RequestInAutoBuffer(ref CmifRequest request, ulong bufferAddress, ulong bufferSize, HipcBufferMode mode)
        {
            if (request.ServerPointerSize != 0 && bufferSize <= (ulong)request.ServerPointerSize)
            {
                RequestInPointer(ref request, bufferAddress, bufferSize);
                RequestInBuffer(ref request, 0UL, 0UL, mode);
            }
            else
            {
                RequestInPointer(ref request, 0UL, 0UL);
                RequestInBuffer(ref request, bufferAddress, bufferSize, mode);
            }
        }

        private static void RequestOutAutoBuffer(ref CmifRequest request, ulong bufferAddress, ulong bufferSize, HipcBufferMode mode)
        {
            if (request.ServerPointerSize != 0 && bufferSize <= (ulong)request.ServerPointerSize)
            {
                RequestOutPointer(ref request, bufferAddress, bufferSize);
                RequestOutBuffer(ref request, 0UL, 0UL, mode);
            }
            else
            {
                RequestOutPointer(ref request, 0UL, 0UL);
                RequestOutBuffer(ref request, bufferAddress, bufferSize, mode);
            }
        }

        private static void RequestInBuffer(ref CmifRequest request, ulong bufferAddress, ulong bufferSize, HipcBufferMode mode)
        {
            request.Hipc.SendBuffers[request.SendBufferIndex++] = new HipcBufferDescriptor(bufferAddress, bufferSize, mode);
        }

        private static void RequestOutBuffer(ref CmifRequest request, ulong bufferAddress, ulong bufferSize, HipcBufferMode mode)
        {
            request.Hipc.ReceiveBuffers[request.RecvBufferIndex++] = new HipcBufferDescriptor(bufferAddress, bufferSize, mode);
        }

        private static void RequestInOutBuffer(ref CmifRequest request, ulong bufferAddress, ulong bufferSize, HipcBufferMode mode)
        {
            request.Hipc.ExchangeBuffers[request.ExchBufferIndex++] = new HipcBufferDescriptor(bufferAddress, bufferSize, mode);
        }

        private static void RequestInPointer(ref CmifRequest request, ulong bufferAddress, ulong bufferSize)
        {
            request.Hipc.SendStatics[request.SendStaticIndex++] = new HipcStaticDescriptor(bufferAddress, (ushort)bufferSize, request.CurrentInPointerId++);
            request.ServerPointerSize -= (int)bufferSize;
        }

        private static void RequestOutFixedPointer(ref CmifRequest request, ulong bufferAddress, ulong bufferSize)
        {
            request.Hipc.ReceiveList[request.RecvListIndex++] = new HipcReceiveListEntry(bufferAddress, (ushort)bufferSize);
            request.ServerPointerSize -= (int)bufferSize;
        }

        private static void RequestOutPointer(ref CmifRequest request, ulong bufferAddress, ulong bufferSize)
        {
            RequestOutFixedPointer(ref request, bufferAddress, bufferSize);
            request.OutPointerSizes[request.OutPointerSizeIndex++] = (ushort)bufferSize;
        }
    }
}
