using Ryujinx.Graphics.Nvdec.Vp9.Common;
using Ryujinx.Graphics.Nvdec.Vp9.Types;
using System;

namespace Ryujinx.Graphics.Nvdec.Vp9
{
    public ref struct ReadBitBuffer
    {
        public ReadOnlySpan<byte> BitBuffer;
        public ulong BitOffset;
        public object ErrorHandlerData;

        public int DecodeUnsignedMax(int max)
        {
            int data = ReadLiteral(BitUtils.GetUnsignedBits((uint)max));
            return data > max ? max : data;
        }

        public ulong BytesRead()
        {
            return (BitOffset + 7) >> 3;
        }

        public int ReadBit()
        {
            ulong off = BitOffset;
            ulong p = off >> 3;
            int q = 7 - (int)(off & 0x7);
            if (p < (ulong)BitBuffer.Length)
            {
                int bit = (BitBuffer[(int)p] >> q) & 1;
                BitOffset = off + 1;
                return bit;
            }

            return 0;
        }

        public int ReadLiteral(int bits)
        {
            int value = 0, bit;
            for (bit = bits - 1; bit >= 0; bit--)
            {
                value |= ReadBit() << bit;
            }

            return value;
        }

        public int ReadSignedLiteral(int bits)
        {
            int value = ReadLiteral(bits);
            return ReadBit() != 0 ? -value : value;
        }

        public int ReadInvSignedLiteral(int bits)
        {
            return ReadSignedLiteral(bits);
        }

        public int ReadDeltaQ()
        {
            return ReadBit() != 0 ? ReadSignedLiteral(4) : 0;
        }

        public void ReadFrameSize(out int width, out int height)
        {
            width = ReadLiteral(16) + 1;
            height = ReadLiteral(16) + 1;
        }

        public BitstreamProfile ReadProfile()
        {
            int profile = ReadBit();
            profile |= ReadBit() << 1;
            if (profile > 2)
            {
                profile += ReadBit();
            }

            return (BitstreamProfile)profile;
        }
    }
}