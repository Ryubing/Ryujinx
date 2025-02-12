using System;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Ryujinx.Graphics.Nvdec.Vp9.Common
{
    internal static class BitUtils
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte ClipPixel(int val)
        {
            return (byte)(val > 255 ? 255 : val < 0 ? 0 : val);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort ClipPixelHighbd(int val, int bd)
        {
            return bd switch
            {
                10 => (ushort)Math.Clamp(val, 0, 1023),
                12 => (ushort)Math.Clamp(val, 0, 4095),
                _ => (ushort)Math.Clamp(val, 0, 255),
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int RoundPowerOfTwo(int value, int n)
        {
            return (value + (1 << (n - 1))) >> n;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long RoundPowerOfTwo(long value, int n)
        {
            return (value + (1L << (n - 1))) >> n;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int AlignPowerOfTwo(int value, int n)
        {
            return (value + ((1 << n) - 1)) & ~((1 << n) - 1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetMsb(uint n)
        {
            Debug.Assert(n != 0);

            return 31 ^ BitOperations.LeadingZeroCount(n);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetUnsignedBits(uint numValues)
        {
            return numValues > 0 ? GetMsb(numValues) + 1 : 0;
        }
    }
}