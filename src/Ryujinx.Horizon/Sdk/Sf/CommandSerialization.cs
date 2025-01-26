using Ryujinx.Horizon.Sdk.Sf.Cmif;
using Ryujinx.Horizon.Sdk.Sf.Hipc;
using Ryujinx.Memory;
using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Ryujinx.Horizon.Sdk.Sf
{
    static class CommandSerialization
    {
        public static ReadOnlySequence<byte> GetReadOnlySequence(PointerAndSize bufferRange)
        {
            return HorizonStatic.AddressSpace.GetReadOnlySequence(bufferRange.Address, checked((int)bufferRange.Size));
        }

        public static ReadOnlySpan<byte> GetReadOnlySpan(PointerAndSize bufferRange)
        {
            return HorizonStatic.AddressSpace.GetSpan(bufferRange.Address, checked((int)bufferRange.Size));
        }

        public static WritableRegion GetWritableRegion(PointerAndSize bufferRange)
        {
            return HorizonStatic.AddressSpace.GetWritableRegion(bufferRange.Address, checked((int)bufferRange.Size));
        }

        public static ref T GetRef<T>(PointerAndSize bufferRange) where T : unmanaged
        {
            WritableRegion writableRegion = GetWritableRegion(bufferRange);

            return ref MemoryMarshal.Cast<byte, T>(writableRegion.Memory.Span)[0];
        }

        public static object DeserializeArg<T>(ref ServiceDispatchContext context, ReadOnlySpan<byte> inRawData, int offset) where T : unmanaged
        {
            return MemoryMarshal.Cast<byte, T>(inRawData.Slice(offset, Unsafe.SizeOf<T>()))[0];
        }

        public static T DeserializeArg<T>(ReadOnlySpan<byte> inRawData, int offset) where T : unmanaged
        {
            return MemoryMarshal.Cast<byte, T>(inRawData.Slice(offset, Unsafe.SizeOf<T>()))[0];
        }

        public static ulong DeserializeClientProcessId(ref ServiceDispatchContext context)
        {
            return context.Request.Pid;
        }

        public static int DeserializeCopyHandle(ref ServiceDispatchContext context, int index)
        {
            return context.Request.Data.CopyHandles[index];
        }

        public static int DeserializeMoveHandle(ref ServiceDispatchContext context, int index)
        {
            return context.Request.Data.MoveHandles[index];
        }

        public static void SerializeArg<T>(Span<byte> outRawData, int offset, T value) where T : unmanaged
        {
            MemoryMarshal.Cast<byte, T>(outRawData.Slice(offset, Unsafe.SizeOf<T>()))[0] = value;
        }

        public static void SerializeCopyHandle(HipcMessageData response, int index, int value)
        {
            response.CopyHandles[index] = value;
        }

        public static void SerializeMoveHandle(HipcMessageData response, int index, int value)
        {
            response.MoveHandles[index] = value;
        }
    }
}
