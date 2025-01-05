using Microsoft.IO;
using Ryujinx.Common;
using Ryujinx.Common.Memory;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Ryujinx.HLE.HOS.Ipc
{
    class IpcMessage
    {
        public IpcMessageType Type { get; set; }

        public IpcHandleDesc HandleDesc { get; set; }

        public List<IpcPtrBuffDesc> PtrBuff { get; private set; }
        public List<IpcBuffDesc> SendBuff { get; private set; }
        public List<IpcBuffDesc> ReceiveBuff { get; private set; }
        public List<IpcBuffDesc> ExchangeBuff { get; private set; }
        public List<IpcRecvListBuffDesc> RecvListBuff { get; private set; }

        public List<int> ObjectIds { get; private set; }

        public byte[] RawData { get; set; }

        public IpcMessage()
        {
            PtrBuff = [];
            SendBuff = [];
            ReceiveBuff = [];
            ExchangeBuff = [];
            RecvListBuff = [];

            ObjectIds = [];
        }

        public IpcMessage(ReadOnlySpan<byte> data, long cmdPtr)
        {
            using RecyclableMemoryStream ms = MemoryStreamManager.Shared.GetStream(data);

            BinaryReader reader = new(ms);

            int word0 = reader.ReadInt32();
            int word1 = reader.ReadInt32();

            Type = (IpcMessageType)(word0 & 0xffff);

            int ptrBuffCount = (word0 >> 16) & 0xf;
            int sendBuffCount = (word0 >> 20) & 0xf;
            int recvBuffCount = (word0 >> 24) & 0xf;
            int xchgBuffCount = (word0 >> 28) & 0xf;

            int rawDataSize = (word1 >> 0) & 0x3ff;
            int recvListFlags = (word1 >> 10) & 0xf;
            bool hndDescEnable = ((word1 >> 31) & 0x1) != 0;

            if (hndDescEnable)
            {
                HandleDesc = new IpcHandleDesc(reader);
            }

            PtrBuff = new List<IpcPtrBuffDesc>(ptrBuffCount);

            for (int index = 0; index < ptrBuffCount; index++)
            {
                PtrBuff.Add(new IpcPtrBuffDesc(reader));
            }

            static List<IpcBuffDesc> ReadBuff(BinaryReader reader, int count)
            {
                List<IpcBuffDesc> buff = new(count);

                for (int index = 0; index < count; index++)
                {
                    buff.Add(new IpcBuffDesc(reader));
                }

                return buff;
            }

            SendBuff = ReadBuff(reader, sendBuffCount);
            ReceiveBuff = ReadBuff(reader, recvBuffCount);
            ExchangeBuff = ReadBuff(reader, xchgBuffCount);

            rawDataSize *= 4;

            long recvListPos = reader.BaseStream.Position + rawDataSize;

            // Only CMIF has the padding requirements.
            if (Type < IpcMessageType.TipcCloseSession)
            {
                long pad0 = GetPadSize16(reader.BaseStream.Position + cmdPtr);

                if (rawDataSize != 0)
                {
                    rawDataSize -= (int)pad0;
                }

                reader.BaseStream.Seek(pad0, SeekOrigin.Current);
            }

            int recvListCount = recvListFlags - 2;

            if (recvListCount == 0)
            {
                recvListCount = 1;
            }
            else if (recvListCount < 0)
            {
                recvListCount = 0;
            }

            RawData = reader.ReadBytes(rawDataSize);

            reader.BaseStream.Seek(recvListPos, SeekOrigin.Begin);

            RecvListBuff = new List<IpcRecvListBuffDesc>(recvListCount);

            for (int index = 0; index < recvListCount; index++)
            {
                RecvListBuff.Add(new IpcRecvListBuffDesc(reader.ReadUInt64()));
            }

            ObjectIds = [];
        }

        public RecyclableMemoryStream GetStream(long cmdPtr, ulong recvListAddr)
        {
            RecyclableMemoryStream ms = MemoryStreamManager.Shared.GetStream();

            int word0;
            int word1;

            word0 = (int)Type;
            word0 |= (PtrBuff.Count & 0xf) << 16;
            word0 |= (SendBuff.Count & 0xf) << 20;
            word0 |= (ReceiveBuff.Count & 0xf) << 24;
            word0 |= (ExchangeBuff.Count & 0xf) << 28;

            using RecyclableMemoryStream handleDataStream = HandleDesc?.GetStream();

            int dataLength = RawData?.Length ?? 0;

            dataLength = (dataLength + 3) & ~3;

            int rawLength = dataLength;

            int pad0 = (int)GetPadSize16(cmdPtr + 8 + (handleDataStream?.Length ?? 0) + PtrBuff.Count * 8);

            // Apparently, padding after Raw Data is 16 bytes, however when there is
            // padding before Raw Data too, we need to subtract the size of this padding.
            // This is the weirdest padding I've seen so far...
            int pad1 = 0x10 - pad0;

            dataLength = (dataLength + pad0 + pad1) / 4;

            word1 = (dataLength & 0x3ff) | (2 << 10);

            if (HandleDesc != null)
            {
                word1 |= 1 << 31;
            }

            ms.Write(word0);
            ms.Write(word1);

            if (handleDataStream != null)
            {
                ms.Write(handleDataStream);
            }

            foreach (IpcPtrBuffDesc ptrBuffDesc in PtrBuff)
            {
                ms.Write(ptrBuffDesc.GetWord0());
                ms.Write(ptrBuffDesc.GetWord1());
            }

            ms.WriteByte(0, pad0);

            if (RawData != null)
            {
                ms.Write(RawData);
                ms.WriteByte(0, rawLength - RawData.Length);
            }

            ms.WriteByte(0, pad1);

            ms.Write(recvListAddr);

            ms.Position = 0;

            return ms;
        }

        public RecyclableMemoryStream GetStreamTipc()
        {
            Debug.Assert(PtrBuff.Count == 0);

            RecyclableMemoryStream ms = MemoryStreamManager.Shared.GetStream();

            int word0;
            int word1;

            word0 = (int)Type;
            word0 |= (SendBuff.Count & 0xf) << 20;
            word0 |= (ReceiveBuff.Count & 0xf) << 24;
            word0 |= (ExchangeBuff.Count & 0xf) << 28;

            using RecyclableMemoryStream handleDataStream = HandleDesc?.GetStream();

            int dataLength = RawData?.Length ?? 0;

            dataLength = ((dataLength + 3) & ~3) / 4;

            word1 = (dataLength & 0x3ff);

            if (HandleDesc != null)
            {
                word1 |= 1 << 31;
            }

            ms.Write(word0);
            ms.Write(word1);

            if (handleDataStream != null)
            {
                ms.Write(handleDataStream);
            }

            if (RawData != null)
            {
                ms.Write(RawData);
            }

            return ms;
        }

        private static long GetPadSize16(long position)
        {
            if ((position & 0xf) != 0)
            {
                return 0x10 - (position & 0xf);
            }

            return 0;
        }

        // ReSharper disable once InconsistentNaming
        public (ulong Position, ulong Size) GetBufferType0x21(int index = 0)
        {
            if (PtrBuff.Count > index && PtrBuff[index].Position != 0)
            {
                return (PtrBuff[index].Position, PtrBuff[index].Size);
            }

            if (SendBuff.Count > index)
            {
                return (SendBuff[index].Position, SendBuff[index].Size);
            }

            return (0, 0);
        }

        // ReSharper disable once InconsistentNaming
        public (ulong Position, ulong Size) GetBufferType0x22(int index = 0)
        {
            if (RecvListBuff.Count > index && RecvListBuff[index].Position != 0)
            {
                return (RecvListBuff[index].Position, RecvListBuff[index].Size);
            }

            if (ReceiveBuff.Count > index)
            {
                return (ReceiveBuff[index].Position, ReceiveBuff[index].Size);
            }

            return (0, 0);
        }
    }
}
