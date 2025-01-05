using Ryujinx.Horizon.Common;
using Ryujinx.Horizon.Sdk.Sf.Hipc;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Ryujinx.Horizon.Sdk.Sf.Cmif
{
    static class CmifMessage
    {
        public const uint CmifInHeaderMagic = 0x49434653; // SFCI
        public const uint CmifOutHeaderMagic = 0x4f434653; // SFCO

        public static CmifRequest CreateRequest(Span<byte> output, CmifRequestFormat format)
        {
            int totalSize = 16;

            if (format.ObjectId != 0)
            {
                totalSize += Unsafe.SizeOf<CmifDomainInHeader>() + format.ObjectsCount * sizeof(int);
            }

            totalSize += Unsafe.SizeOf<CmifInHeader>() + format.DataSize;
            totalSize = (totalSize + 1) & ~1;

            int outPointerSizeTableOffset = totalSize;
            int outPointerSizeTableSize = format.OutAutoBuffersCount + format.OutPointersCount;

            totalSize += sizeof(ushort) * outPointerSizeTableSize;

            int rawDataSizeInWords = (totalSize + sizeof(uint) - 1) / sizeof(uint);

            CmifRequest request = new()
            {
                Hipc = HipcMessage.WriteMessage(output, new HipcMetadata
                {
                    Type = format.Context != 0 ? (int)CommandType.RequestWithContext : (int)CommandType.Request,
                    SendStaticsCount = format.InAutoBuffersCount + format.InPointersCount,
                    SendBuffersCount = format.InAutoBuffersCount + format.InBuffersCount,
                    ReceiveBuffersCount = format.OutAutoBuffersCount + format.OutBuffersCount,
                    ExchangeBuffersCount = format.InOutBuffersCount,
                    DataWordsCount = rawDataSizeInWords,
                    ReceiveStaticsCount = outPointerSizeTableSize + format.OutFixedPointersCount,
                    SendPid = format.SendPid,
                    CopyHandlesCount = format.HandlesCount,
                    MoveHandlesCount = 0,
                }),
            };

            Span<uint> data = request.Hipc.DataWords;

            if (format.ObjectId != 0)
            {
                ref CmifDomainInHeader domainHeader = ref MemoryMarshal.Cast<uint, CmifDomainInHeader>(data)[0];

                int payloadSize = Unsafe.SizeOf<CmifInHeader>() + format.DataSize;

                domainHeader = new CmifDomainInHeader
                {
                    Type = CmifDomainRequestType.SendMessage,
                    ObjectsCount = (byte)format.ObjectsCount,
                    DataSize = (ushort)payloadSize,
                    ObjectId = format.ObjectId,
                    Padding = 0,
                    Token = format.Context,
                };

                data = data[(Unsafe.SizeOf<CmifDomainInHeader>() / sizeof(uint))..];

                request.Objects = data[((payloadSize + sizeof(uint) - 1) / sizeof(uint))..];
            }

            ref CmifInHeader header = ref MemoryMarshal.Cast<uint, CmifInHeader>(data)[0];

            header = new CmifInHeader
            {
                Magic = CmifInHeaderMagic,
                Version = format.Context != 0 ? 1u : 0u,
                CommandId = format.RequestId,
                Token = format.ObjectId != 0 ? 0u : format.Context,
            };

            request.Data = MemoryMarshal.Cast<uint, byte>(data)[Unsafe.SizeOf<CmifInHeader>()..];

            int paddingSizeBefore = (rawDataSizeInWords - request.Hipc.DataWords.Length) * sizeof(uint);

            Span<byte> outPointerTable = MemoryMarshal.Cast<uint, byte>(request.Hipc.DataWords)[(outPointerSizeTableOffset - paddingSizeBefore)..];

            request.OutPointerSizes = MemoryMarshal.Cast<byte, ushort>(outPointerTable);
            request.ServerPointerSize = format.ServerPointerSize;

            return request;
        }

        public static Result ParseResponse(out CmifResponse response, Span<byte> input, bool isDomain, int size)
        {
            HipcMessage responseMessage = new(input);

            Span<byte> data = MemoryMarshal.Cast<uint, byte>(responseMessage.Data.DataWords);
            Span<uint> objects = [];

            if (isDomain)
            {
                data = data[Unsafe.SizeOf<CmifDomainOutHeader>()..];
                objects = MemoryMarshal.Cast<byte, uint>(data[(Unsafe.SizeOf<CmifOutHeader>() + size)..]);
            }

            CmifOutHeader header = MemoryMarshal.Cast<byte, CmifOutHeader>(data)[0];

            if (header.Magic != CmifOutHeaderMagic)
            {
                response = default;

                return SfResult.InvalidOutHeader;
            }

            if (header.Result.IsFailure)
            {
                response = default;

                return header.Result;
            }

            response = new CmifResponse
            {
                Data = data[Unsafe.SizeOf<CmifOutHeader>()..],
                Objects = objects,
                CopyHandles = responseMessage.Data.CopyHandles,
                MoveHandles = responseMessage.Data.MoveHandles,
            };

            return Result.Success;
        }
    }
}
