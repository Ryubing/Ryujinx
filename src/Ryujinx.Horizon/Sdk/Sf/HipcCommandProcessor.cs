using Ryujinx.Common;
using Ryujinx.Horizon.Common;
using Ryujinx.Horizon.Sdk.Sf.Cmif;
using Ryujinx.Horizon.Sdk.Sf.Hipc;
using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Ryujinx.Horizon.Sdk.Sf
{
    class HipcCommandProcessor : ServerMessageProcessor
    {
        private readonly CommandArg[] _args;

        private readonly int[] _inOffsets;
        private readonly int[] _outOffsets;
        private readonly PointerAndSize[] _bufferRanges;

        private readonly bool _hasInProcessIdHolder;
        private readonly int _inObjectsCount;
        private readonly int _outObjectsCount;
        private readonly int _inMapAliasBuffersCount;
        private readonly int _outMapAliasBuffersCount;
        private readonly int _inPointerBuffersCount;
        private readonly int _outPointerBuffersCount;
        private readonly int _outFixedSizePointerBuffersCount;
        private readonly int _inMoveHandlesCount;
        private readonly int _inCopyHandlesCount;
        private readonly int _outMoveHandlesCount;
        private readonly int _outCopyHandlesCount;

        public int FunctionArgumentsCount => _args.Length;

        public int InRawDataSize => BitUtils.AlignUp(_inOffsets[^1], sizeof(ushort));
        public int OutRawDataSize => BitUtils.AlignUp(_outOffsets[^1], sizeof(uint));

        private int OutUnfixedSizePointerBuffersCount => _outPointerBuffersCount - _outFixedSizePointerBuffersCount;

        public HipcCommandProcessor(CommandArg[] args)
        {
            _args = args;

            foreach (CommandArg argInfo in args)
            {
                switch (argInfo.Type)
                {
                    case CommandArgType.Buffer:
                        HipcBufferFlags flags = argInfo.BufferFlags;

                        if (flags.HasFlag(HipcBufferFlags.In))
                        {
                            if (flags.HasFlag(HipcBufferFlags.AutoSelect))
                            {
                                _inMapAliasBuffersCount++;
                                _inPointerBuffersCount++;
                            }
                            else if (flags.HasFlag(HipcBufferFlags.MapAlias))
                            {
                                _inMapAliasBuffersCount++;
                            }
                            else if (flags.HasFlag(HipcBufferFlags.Pointer))
                            {
                                _inPointerBuffersCount++;
                            }
                        }
                        else
                        {
                            bool autoSelect = flags.HasFlag(HipcBufferFlags.AutoSelect);
                            if (autoSelect || flags.HasFlag(HipcBufferFlags.Pointer))
                            {
                                _outPointerBuffersCount++;

                                if (flags.HasFlag(HipcBufferFlags.FixedSize))
                                {
                                    _outFixedSizePointerBuffersCount++;
                                }
                            }

                            if (autoSelect || flags.HasFlag(HipcBufferFlags.MapAlias))
                            {
                                _outMapAliasBuffersCount++;
                            }
                        }
                        break;
                    case CommandArgType.InCopyHandle:
                        _inCopyHandlesCount++;
                        break;
                    case CommandArgType.InMoveHandle:
                        _inMoveHandlesCount++;
                        break;
                    case CommandArgType.InObject:
                        _inObjectsCount++;
                        break;
                    case CommandArgType.ProcessId:
                        _hasInProcessIdHolder = true;
                        break;
                    case CommandArgType.OutCopyHandle:
                        _outCopyHandlesCount++;
                        break;
                    case CommandArgType.OutMoveHandle:
                        _outMoveHandlesCount++;
                        break;
                    case CommandArgType.OutObject:
                        _outObjectsCount++;
                        break;
                }
            }

            _inOffsets = RawDataOffsetCalculator.Calculate(args.Where(x => x.Type == CommandArgType.InArgument).ToArray());
            _outOffsets = RawDataOffsetCalculator.Calculate(args.Where(x => x.Type == CommandArgType.OutArgument).ToArray());
            _bufferRanges = new PointerAndSize[args.Length];
        }

        public int GetInArgOffset(int argIndex)
        {
            return _inOffsets[argIndex];
        }

        public int GetOutArgOffset(int argIndex)
        {
            return _outOffsets[argIndex];
        }

        public PointerAndSize GetBufferRange(int argIndex)
        {
            return _bufferRanges[argIndex];
        }

        public Result ProcessBuffers(ref ServiceDispatchContext context, scoped Span<bool> isBufferMapAlias, ServerMessageRuntimeMetadata runtimeMetadata)
        {
            bool mapAliasBuffersValid = true;

            ulong pointerBufferTail = context.PointerBuffer.Address;
            ulong pointerBufferHead = pointerBufferTail + context.PointerBuffer.Size;

            int sendMapAliasIndex = 0;
            int recvMapAliasIndex = 0;
            int sendPointerIndex = 0;
            int unfixedRecvPointerIndex = 0;

            for (int i = 0; i < _args.Length; i++)
            {
                if (_args[i].Type != CommandArgType.Buffer)
                {
                    continue;
                }

                HipcBufferFlags flags = _args[i].BufferFlags;
                bool isMapAlias;

                if (flags.HasFlag(HipcBufferFlags.MapAlias))
                {
                    isMapAlias = true;
                }
                else if (flags.HasFlag(HipcBufferFlags.Pointer))
                {
                    isMapAlias = false;
                }
                else /* if (flags.HasFlag(HipcBufferFlags.HipcAutoSelect)) */
                {
                    HipcBufferDescriptor descriptor = flags.HasFlag(HipcBufferFlags.In)
                        ? context.Request.Data.SendBuffers[sendMapAliasIndex]
                        : context.Request.Data.ReceiveBuffers[recvMapAliasIndex];

                    isMapAlias = descriptor.Address != 0UL;
                }

                isBufferMapAlias[i] = isMapAlias;

                if (isMapAlias)
                {
                    HipcBufferDescriptor descriptor = flags.HasFlag(HipcBufferFlags.In)
                        ? context.Request.Data.SendBuffers[sendMapAliasIndex++]
                        : context.Request.Data.ReceiveBuffers[recvMapAliasIndex++];

                    _bufferRanges[i] = new PointerAndSize(descriptor.Address, descriptor.Size);

                    if (!IsMapTransferModeValid(flags, descriptor.Mode))
                    {
                        mapAliasBuffersValid = false;
                    }
                }
                else
                {
                    if (flags.HasFlag(HipcBufferFlags.In))
                    {
                        HipcStaticDescriptor descriptor = context.Request.Data.SendStatics[sendPointerIndex++];
                        ulong address = descriptor.Address;
                        ulong size = descriptor.Size;

                        _bufferRanges[i] = new PointerAndSize(address, size);

                        if (size != 0)
                        {
                            pointerBufferTail = Math.Max(pointerBufferTail, address + size);
                        }
                    }
                    else /* if (flags.HasFlag(HipcBufferFlags.Out)) */
                    {
                        ulong size;

                        if (flags.HasFlag(HipcBufferFlags.FixedSize))
                        {
                            size = _args[i].BufferFixedSize;
                        }
                        else
                        {
                            Span<byte> data = MemoryMarshal.Cast<uint, byte>(context.Request.Data.DataWordsPadded);
                            Span<ushort> recvPointerSizes = MemoryMarshal.Cast<byte, ushort>(data[runtimeMetadata.UnfixedOutPointerSizeOffset..]);

                            size = recvPointerSizes[unfixedRecvPointerIndex++];
                        }

                        pointerBufferHead = BitUtils.AlignDown(pointerBufferHead - size, 0x10UL);
                        _bufferRanges[i] = new PointerAndSize(pointerBufferHead, size);
                    }
                }
            }

            if (!mapAliasBuffersValid)
            {
                return HipcResult.InvalidCmifRequest;
            }

            if (_outPointerBuffersCount != 0 && pointerBufferTail > pointerBufferHead)
            {
                return HipcResult.PointerBufferTooSmall;
            }

            return Result.Success;
        }

        private static bool IsMapTransferModeValid(HipcBufferFlags flags, HipcBufferMode mode)
        {
            if (flags.HasFlag(HipcBufferFlags.MapTransferAllowsNonSecure))
            {
                return mode == HipcBufferMode.NonSecure;
            }

            if (flags.HasFlag(HipcBufferFlags.MapTransferAllowsNonDevice))
            {
                return mode == HipcBufferMode.NonDevice;
            }

            return mode == HipcBufferMode.Normal;
        }

        public void SetOutBuffers(HipcMessageData response, ReadOnlySpan<bool> isBufferMapAlias)
        {
            int recvPointerIndex = 0;

            for (int i = 0; i < _args.Length; i++)
            {
                if (_args[i].Type != CommandArgType.Buffer)
                {
                    continue;
                }

                HipcBufferFlags flags = _args[i].BufferFlags;
                if (!flags.HasFlag(HipcBufferFlags.Out))
                {
                    continue;
                }

                PointerAndSize buffer = _bufferRanges[i];

                if (flags.HasFlag(HipcBufferFlags.Pointer))
                {
                    response.SendStatics[recvPointerIndex] = new HipcStaticDescriptor(buffer.Address, (ushort)buffer.Size, recvPointerIndex);
                }
                else if (flags.HasFlag(HipcBufferFlags.AutoSelect))
                {
                    if (!isBufferMapAlias[i])
                    {
                        response.SendStatics[recvPointerIndex] = new HipcStaticDescriptor(buffer.Address, (ushort)buffer.Size, recvPointerIndex);
                    }
                    else
                    {
                        response.SendStatics[recvPointerIndex] = new HipcStaticDescriptor(0UL, 0, recvPointerIndex);
                    }
                }

                recvPointerIndex++;
            }
        }

        public override void SetImplementationProcessor(ServerMessageProcessor impl)
        {
            // We don't need to do anything here as this should be always the last processor to be called.
        }

        public override ServerMessageRuntimeMetadata GetRuntimeMetadata()
        {
            return new ServerMessageRuntimeMetadata(
                (ushort)InRawDataSize,
                (ushort)OutRawDataSize,
                (byte)Unsafe.SizeOf<CmifInHeader>(),
                (byte)Unsafe.SizeOf<CmifOutHeader>(),
                (byte)_inObjectsCount,
                (byte)_outObjectsCount);
        }

        public override Result PrepareForProcess(ref ServiceDispatchContext context, ServerMessageRuntimeMetadata runtimeMetadata)
        {
            ref HipcMetadata meta = ref context.Request.Meta;
            bool requestValid = true;
            requestValid &= meta.SendPid == _hasInProcessIdHolder;
            requestValid &= meta.SendStaticsCount == _inPointerBuffersCount;
            requestValid &= meta.SendBuffersCount == _inMapAliasBuffersCount;
            requestValid &= meta.ReceiveBuffersCount == _outMapAliasBuffersCount;
            requestValid &= meta.ExchangeBuffersCount == 0;
            requestValid &= meta.CopyHandlesCount == _inCopyHandlesCount;
            requestValid &= meta.MoveHandlesCount == _inMoveHandlesCount;

            int rawSizeInBytes = meta.DataWordsCount * sizeof(uint);
            int commandRawSize = BitUtils.AlignUp(runtimeMetadata.UnfixedOutPointerSizeOffset + (OutUnfixedSizePointerBuffersCount * sizeof(ushort)), sizeof(uint));

            requestValid &= rawSizeInBytes >= commandRawSize;

            return requestValid ? Result.Success : HipcResult.InvalidCmifRequest;
        }

        public Result GetInObjects(ServerMessageProcessor processor, Span<IServiceObject> objects)
        {
            if (objects.Length == 0)
            {
                return Result.Success;
            }

            ServiceObjectHolder[] inObjects = new ServiceObjectHolder[objects.Length];
            Result result = processor.GetInObjects(inObjects);

            if (result.IsFailure)
            {
                return result;
            }

            int inObjectIndex = 0;

            foreach (CommandArg t in _args)
            {
                if (t.Type != CommandArgType.InObject)
                {
                    continue;
                }

                int index = inObjectIndex++;
                ServiceObjectHolder inObject = inObjects[index];

                objects[index] = inObject?.ServiceObject;
            }

            return Result.Success;
        }

        public override Result GetInObjects(Span<ServiceObjectHolder> inObjects)
        {
            return SfResult.NotSupported;
        }

        public override HipcMessageData PrepareForReply(scoped ref ServiceDispatchContext context, out Span<byte> outRawData, ServerMessageRuntimeMetadata runtimeMetadata)
        {
            int rawDataSize = OutRawDataSize + runtimeMetadata.OutHeadersSize;
            HipcMessageData response = HipcMessage.WriteResponse(
                context.OutMessageBuffer,
                _outPointerBuffersCount,
                (BitUtils.AlignUp(rawDataSize, 4) + 0x10) / sizeof(uint),
                _outCopyHandlesCount,
                _outMoveHandlesCount + runtimeMetadata.OutObjectsCount);
            outRawData = MemoryMarshal.Cast<uint, byte>(response.DataWords);

            return response;
        }

        public override void PrepareForErrorReply(scoped ref ServiceDispatchContext context, out Span<byte> outRawData, ServerMessageRuntimeMetadata runtimeMetadata)
        {
            int rawDataSize = runtimeMetadata.OutHeadersSize;
            HipcMessageData response = HipcMessage.WriteResponse(
                context.OutMessageBuffer,
                0,
                (BitUtils.AlignUp(rawDataSize, 4) + 0x10) / sizeof(uint),
                0,
                0);

            outRawData = MemoryMarshal.Cast<uint, byte>(response.DataWords);
        }

#pragma warning disable CA1822 // Mark member as static
        public void SetOutObjects(ref ServiceDispatchContext context, HipcMessageData response, Span<IServiceObject> objects)
#pragma warning restore CA1822
        {
            if (objects.Length == 0)
            {
                return;
            }

            ServiceObjectHolder[] outObjects = new ServiceObjectHolder[objects.Length];

            for (int i = 0; i < objects.Length; i++)
            {
                outObjects[i] = objects[i] != null ? new ServiceObjectHolder(objects[i]) : null;
            }

            context.Processor.SetOutObjects(ref context, response, outObjects);
        }

        public override void SetOutObjects(scoped ref ServiceDispatchContext context, HipcMessageData response, Span<ServiceObjectHolder> outObjects)
        {
            for (int index = 0; index < _outObjectsCount; index++)
            {
                SetOutObjectImpl(index, response, context.Manager, outObjects[index]);
            }
        }

        private static void SetOutObjectImpl(int index, HipcMessageData response, ServerSessionManager manager, ServiceObjectHolder obj)
        {
            if (obj == null)
            {
                response.MoveHandles[index] = 0;

                return;
            }

            Api.CreateSession(out int serverHandle, out int clientHandle).AbortOnFailure();
            manager.RegisterSession(serverHandle, obj).AbortOnFailure();
            response.MoveHandles[index] = clientHandle;
        }
    }
}
