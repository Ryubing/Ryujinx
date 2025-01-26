using System;
using System.Runtime.InteropServices;

namespace Ryujinx.Horizon.Sdk.Ngc.Detail
{
    class CompressedArray
    {
        private const int MaxUncompressedEntries = 64;
        private const int CompressedEntriesPerBlock = 64;
        private const int BitsPerWord = Set.BitsPerWord;

        private readonly struct BitfieldRange
        {
            private readonly uint _range;
            private readonly int _baseValue;

            public int BitfieldIndex => (int)(_range & 0x7ffffff);
            public int BitfieldLength => (int)(_range >> 27) + 1;
            public int BaseValue => _baseValue;

            public BitfieldRange(uint range, int baseValue)
            {
                _range = range;
                _baseValue = baseValue;
            }
        }

        private uint[] _bitfieldRanges;
        private uint[] _bitfields;
        private int[] _uncompressedArray;

        public int Length => (_bitfieldRanges.Length / 2) * CompressedEntriesPerBlock + _uncompressedArray.Length;

        public int this[int index]
        {
            get
            {
                ReadOnlySpan<BitfieldRange> ranges = GetBitfieldRanges();

                int rangeBlockIndex = index / CompressedEntriesPerBlock;

                if (rangeBlockIndex < ranges.Length)
                {
                    BitfieldRange range = ranges[rangeBlockIndex];

                    int bitfieldLength = range.BitfieldLength;
                    int bitfieldOffset = (index % CompressedEntriesPerBlock) * bitfieldLength;
                    int bitfieldIndex = range.BitfieldIndex + (bitfieldOffset / BitsPerWord);
                    int bitOffset = bitfieldOffset % BitsPerWord;

                    ulong bitfieldValue = _bitfields[bitfieldIndex];

                    // If the bit fields crosses the word boundary, let's load the next one to ensure we
                    // have access to the full value.
                    if (bitOffset + bitfieldLength > BitsPerWord)
                    {
                        bitfieldValue |= (ulong)_bitfields[bitfieldIndex + 1] << 32;
                    }

                    int value = (int)(bitfieldValue >> bitOffset) & ((1 << bitfieldLength) - 1);

                    // Sign-extend.
                    int remainderBits = BitsPerWord - bitfieldLength;
                    value <<= remainderBits;
                    value >>= remainderBits;

                    return value + range.BaseValue;
                }
                else if (rangeBlockIndex < _uncompressedArray.Length + _bitfieldRanges.Length * BitsPerWord)
                {
                    return _uncompressedArray[index % MaxUncompressedEntries];
                }

                return 0;
            }
        }

        private ReadOnlySpan<BitfieldRange> GetBitfieldRanges()
        {
            return MemoryMarshal.Cast<uint, BitfieldRange>(_bitfieldRanges);
        }

        public bool Import(ref BinaryReader reader)
        {
            if (!reader.Read(out int bitfieldRangesCount) ||
                reader.AllocateAndReadArray(ref _bitfieldRanges, bitfieldRangesCount) != bitfieldRangesCount)
            {
                return false;
            }

            if (!reader.Read(out int bitfieldsCount) || reader.AllocateAndReadArray(ref _bitfields, bitfieldsCount) != bitfieldsCount)
            {
                return false;
            }

            return reader.Read(out byte uncompressedArrayLength) &&
                reader.AllocateAndReadArray(ref _uncompressedArray, uncompressedArrayLength, MaxUncompressedEntries) == uncompressedArrayLength;
        }
    }
}
