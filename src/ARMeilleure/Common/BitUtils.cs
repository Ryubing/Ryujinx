using System;
using System.Numerics;

namespace ARMeilleure.Common
{
    static class BitUtils
    {
        private static ReadOnlySpan<sbyte> HbsNibbleLut => [-1, 0, 1, 1, 2, 2, 2, 2, 3, 3, 3, 3, 3, 3, 3, 3];

        public static long FillWithOnes(int bits)
        {
            return bits == 64 ? -1L : (1L << bits) - 1;
        }

        public static int HighestBitSet(int value)
        {
            return 31 - BitOperations.LeadingZeroCount((uint)value);
        }

        public static int HighestBitSetNibble(int value)
        {
            return HbsNibbleLut[value];
        }

        public static long Replicate(long bits, int size)
        {
            long output = 0;

            for (int bit = 0; bit < 64; bit += size)
            {
                output |= bits << bit;
            }

            return output;
        }

        public static int RotateRight(int bits, int shift, int size)
        {
            return (int)RotateRight((uint)bits, shift, size);
        }

        public static uint RotateRight(uint bits, int shift, int size)
        {
            return (bits >> shift) | (bits << (size - shift));
        }

        public static long RotateRight(long bits, int shift, int size)
        {
            return (long)RotateRight((ulong)bits, shift, size);
        }

        public static ulong RotateRight(ulong bits, int shift, int size)
        {
            return (bits >> shift) | (bits << (size - shift));
        }
    }
}
