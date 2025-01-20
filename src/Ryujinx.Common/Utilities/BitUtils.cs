using System.Numerics;

namespace Ryujinx.Common
{
    public static class BitUtils
    {
        public static T AlignUp<T>(T value, T size) where T : IBinaryInteger<T>
            => (value + (size - T.One)) & -size;

        public static T AlignDown<T>(T value, T size) where T : IBinaryInteger<T>
            => value & -size;

        public static T DivRoundUp<T>(T value, T dividend) where T : IBinaryInteger<T>
            => (value + (dividend - T.One)) / dividend;

        public static int Pow2RoundDown(int value) => BitOperations.IsPow2(value) ? value : Pow2RoundUp(value) >> 1;

        public static long ReverseBits64(long value) => (long)ReverseBits64((ulong)value);

        public static int Pow2RoundUp(int value)
        {
            value--;

            value |= (value >> 1);
            value |= (value >> 2);
            value |= (value >> 4);
            value |= (value >> 8);
            value |= (value >> 16);

            return ++value;
        }

        private static ulong ReverseBits64(ulong value)
        {
            value = ((value & 0xaaaaaaaaaaaaaaaa) >> 1) | ((value & 0x5555555555555555) << 1);
            value = ((value & 0xcccccccccccccccc) >> 2) | ((value & 0x3333333333333333) << 2);
            value = ((value & 0xf0f0f0f0f0f0f0f0) >> 4) | ((value & 0x0f0f0f0f0f0f0f0f) << 4);
            value = ((value & 0xff00ff00ff00ff00) >> 8) | ((value & 0x00ff00ff00ff00ff) << 8);
            value = ((value & 0xffff0000ffff0000) >> 16) | ((value & 0x0000ffff0000ffff) << 16);

            return (value >> 32) | (value << 32);
        }
        
        // Never actually written bit packing logic before, so I looked it up.
        // This code is from https://gist.github.com/Alan-FGR/04938e93e2bffdf5802ceb218a37c195
        
        public static ulong PackBitFields(this uint[] values, byte[] bitFields)
        {
            ulong retVal = values[0]; //we set the first value right away
            for (int f = 1; f < values.Length; f++)
            {
                retVal <<= bitFields[f]; // we shift the previous value
                retVal += values[f];// and add our current value
            }
            return retVal;
        }

        public static uint[] UnpackBitFields(this ulong packed, byte[] bitFields)
        {
            int fields = bitFields.Length - 1; // number of fields to unpack
            uint[] retArr = new uint[fields + 1]; // init return array
            int curPos = 0; // current field bit position (start)
            int lastEnd; // position where last field ended
            for (int f = fields; f >= 0; f--) // loop from last
            {
                lastEnd = curPos; // we store where the last value ended
                curPos += bitFields[f]; // we get where the current value starts
                int leftShift = 64 - curPos; // we figure how much left shift we gotta apply for the other numbers to overflow into oblivion
                retArr[f] = (uint)((packed << leftShift) >> leftShift + lastEnd); // we do magic
            }
            return retArr;
        }
    }
}
