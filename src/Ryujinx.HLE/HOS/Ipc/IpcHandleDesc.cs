using Microsoft.IO;
using Ryujinx.Common;
using Ryujinx.Common.Memory;
using System;
using System.IO;

namespace Ryujinx.HLE.HOS.Ipc
{
    class IpcHandleDesc
    {
        public bool HasPId { get; private set; }

        public ulong PId { get; private set; }

        public int[] ToCopy { get; private set; }
        public int[] ToMove { get; private set; }

        public IpcHandleDesc(BinaryReader reader)
        {
            int word = reader.ReadInt32();

            HasPId = (word & 1) != 0;

            PId = HasPId ? reader.ReadUInt64() : 0;

            int toCopySize = (word >> 1) & 0xf;
            int[] toCopy = toCopySize == 0 ? [] : new int[toCopySize];

            for (int index = 0; index < toCopy.Length; index++)
            {
                toCopy[index] = reader.ReadInt32();
            }

            ToCopy = toCopy;

            int toMoveSize = (word >> 5) & 0xf;
            int[] toMove = toMoveSize == 0 ? [] : new int[toMoveSize];

            for (int index = 0; index < toMove.Length; index++)
            {
                toMove[index] = reader.ReadInt32();
            }

            ToMove = toMove;
        }

        public IpcHandleDesc(int[] copy, int[] move)
        {
            ToCopy = copy ?? throw new ArgumentNullException(nameof(copy));
            ToMove = move ?? throw new ArgumentNullException(nameof(move));
        }

        public IpcHandleDesc(int[] copy, int[] move, ulong pId) : this(copy, move)
        {
            PId = pId;

            HasPId = true;
        }

        public static IpcHandleDesc MakeCopy(params int[] handles)
        {
            return new IpcHandleDesc(handles, []);
        }

        public static IpcHandleDesc MakeMove(params int[] handles)
        {
            return new IpcHandleDesc([], handles);
        }

        public RecyclableMemoryStream GetStream()
        {
            RecyclableMemoryStream ms = MemoryStreamManager.Shared.GetStream();

            int word = HasPId ? 1 : 0;

            word |= (ToCopy.Length & 0xf) << 1;
            word |= (ToMove.Length & 0xf) << 5;

            ms.Write(word);

            if (HasPId)
            {
                ms.Write(PId);
            }

            ms.Write(ToCopy);
            ms.Write(ToMove);

            ms.Position = 0;
            return ms;
        }
    }
}
