using ARMeilleure.Common;
using System;

namespace ARMeilleure.Decoders
{
    static class DecoderHelper
    {
        static DecoderHelper()
        {
            Imm8ToFP32Table = BuildImm8ToFP32Table();
            Imm8ToFP64Table = BuildImm8ToFP64Table();
        }

        public static readonly uint[] Imm8ToFP32Table;
        public static readonly ulong[] Imm8ToFP64Table;

        private static uint[] BuildImm8ToFP32Table()
        {
            uint[] tbl = new uint[256];

            for (int idx = 0; idx < tbl.Length; idx++)
            {
                tbl[idx] = ExpandImm8ToFP32((uint)idx);
            }

            return tbl;
        }

        private static ulong[] BuildImm8ToFP64Table()
        {
            ulong[] tbl = new ulong[256];

            for (int idx = 0; idx < tbl.Length; idx++)
            {
                tbl[idx] = ExpandImm8ToFP64((ulong)idx);
            }

            return tbl;
        }

        // abcdefgh -> aBbbbbbc defgh000 00000000 00000000 (B = ~b)
        private static uint ExpandImm8ToFP32(uint imm)
        {
            static uint MoveBit(uint bits, int from, int to)
            {
                return ((bits >> from) & 1U) << to;
            }

            return MoveBit(imm, 7, 31) | MoveBit(~imm, 6, 30) |
                   MoveBit(imm, 6, 29) | MoveBit(imm, 6, 28) |
                   MoveBit(imm, 6, 27) | MoveBit(imm, 6, 26) |
                   MoveBit(imm, 6, 25) | MoveBit(imm, 5, 24) |
                   MoveBit(imm, 4, 23) | MoveBit(imm, 3, 22) |
                   MoveBit(imm, 2, 21) | MoveBit(imm, 1, 20) |
                   MoveBit(imm, 0, 19);
        }

        // abcdefgh -> aBbbbbbb bbcdefgh 00000000 00000000 00000000 00000000 00000000 00000000 (B = ~b)
        private static ulong ExpandImm8ToFP64(ulong imm)
        {
            static ulong MoveBit(ulong bits, int from, int to)
            {
                return ((bits >> from) & 1UL) << to;
            }

            return MoveBit(imm, 7, 63) | MoveBit(~imm, 6, 62) |
                   MoveBit(imm, 6, 61) | MoveBit(imm, 6, 60) |
                   MoveBit(imm, 6, 59) | MoveBit(imm, 6, 58) |
                   MoveBit(imm, 6, 57) | MoveBit(imm, 6, 56) |
                   MoveBit(imm, 6, 55) | MoveBit(imm, 6, 54) |
                   MoveBit(imm, 5, 53) | MoveBit(imm, 4, 52) |
                   MoveBit(imm, 3, 51) | MoveBit(imm, 2, 50) |
                   MoveBit(imm, 1, 49) | MoveBit(imm, 0, 48);
        }

        public struct BitMask
        {
            public long WMask;
            public long TMask;
            public int Pos;
            public int Shift;
            public bool IsUndefined;

            public static BitMask Invalid => new() { IsUndefined = true };
        }

        public static BitMask DecodeBitMask(int opCode, bool immediate)
        {
            int immS = (opCode >> 10) & 0x3f;
            int immR = (opCode >> 16) & 0x3f;

            int n = (opCode >> 22) & 1;
            int sf = (opCode >> 31) & 1;

            int length = BitUtils.HighestBitSet((~immS & 0x3f) | (n << 6));

            if (length < 1 || (sf == 0 && n != 0))
            {
                return BitMask.Invalid;
            }

            int size = 1 << length;

            int levels = size - 1;

            int s = immS & levels;
            int r = immR & levels;

            if (immediate && s == levels)
            {
                return BitMask.Invalid;
            }

            long wMask = BitUtils.FillWithOnes(s + 1);
            long tMask = BitUtils.FillWithOnes(((s - r) & levels) + 1);

            if (r > 0)
            {
                wMask = BitUtils.RotateRight(wMask, r, size);
                wMask &= BitUtils.FillWithOnes(size);
            }

            return new BitMask()
            {
                WMask = BitUtils.Replicate(wMask, size),
                TMask = BitUtils.Replicate(tMask, size),

                Pos = immS,
                Shift = immR,
            };
        }

        public static long DecodeImm24_2(int opCode)
        {
            return ((long)opCode << 40) >> 38;
        }

        public static long DecodeImm26_2(int opCode)
        {
            return ((long)opCode << 38) >> 36;
        }

        public static long DecodeImmS19_2(int opCode)
        {
            return (((long)opCode << 40) >> 43) & ~3;
        }

        public static long DecodeImmS14_2(int opCode)
        {
            return (((long)opCode << 45) >> 48) & ~3;
        }

        public static bool VectorArgumentsInvalid(bool q, params ReadOnlySpan<int> args)
        {
            if (q)
            {
                for (int i = 0; i < args.Length; i++)
                {
                    if ((args[i] & 1) == 1)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
