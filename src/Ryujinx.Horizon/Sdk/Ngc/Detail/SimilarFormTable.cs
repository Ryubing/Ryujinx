using System;

namespace Ryujinx.Horizon.Sdk.Ngc.Detail
{
    class SimilarFormTable
    {
        private int _similarTableStringLength;
        private int _canonicalTableStringLength;
        private int _count;
        private byte[][] _similarTable;
        private byte[][] _canonicalTable;

        public bool Import(ref BinaryReader reader)
        {
            if (!reader.Read(out _similarTableStringLength) ||
                !reader.Read(out _canonicalTableStringLength) ||
                !reader.Read(out _count))
            {
                return false;
            }

            _similarTable = new byte[_count][];
            _canonicalTable = new byte[_count][];

            if (_count < 1)
            {
                return true;
            }

            for (int tableIndex = 0; tableIndex < _count; tableIndex++)
            {
                if (reader.AllocateAndReadArray(ref _similarTable[tableIndex], _similarTableStringLength) != _similarTableStringLength ||
                    reader.AllocateAndReadArray(ref _canonicalTable[tableIndex], _canonicalTableStringLength) != _canonicalTableStringLength)
                {
                    return false;
                }
            }

            return true;
        }

        public ReadOnlySpan<byte> FindCanonicalString(ReadOnlySpan<byte> similarFormString)
        {
            int lowerBound = 0;
            int upperBound = _count;

            for (int charIndex = 0; charIndex < similarFormString.Length; charIndex++)
            {
                byte character = similarFormString[charIndex];

                int newLowerBound = GetLowerBound(character, charIndex, lowerBound - 1, upperBound - 1);
                if (newLowerBound < 0 || _similarTable[newLowerBound][charIndex] != character)
                {
                    return [];
                }

                int newUpperBound = GetUpperBound(character, charIndex, lowerBound - 1, upperBound - 1);
                if (newUpperBound < 0)
                {
                    newUpperBound = upperBound;
                }

                lowerBound = newLowerBound;
                upperBound = newUpperBound;
            }

            return _canonicalTable[lowerBound];
        }

        private int GetLowerBound(byte character, int charIndex, int left, int right)
        {
            while (right - left > 1)
            {
                int range = right + left;

                if (range < 0)
                {
                    range++;
                }

                int middle = range / 2;

                if (character <= _similarTable[middle][charIndex])
                {
                    right = middle;
                }
                else
                {
                    left = middle;
                }
            }

            if (_similarTable[right][charIndex] < character)
            {
                return -1;
            }

            return right;
        }

        private int GetUpperBound(byte character, int charIndex, int left, int right)
        {
            while (right - left > 1)
            {
                int range = right + left;

                if (range < 0)
                {
                    range++;
                }

                int middle = range / 2;

                if (_similarTable[middle][charIndex] <= character)
                {
                    left = middle;
                }
                else
                {
                    right = middle;
                }
            }

            if (_similarTable[right][charIndex] <= character)
            {
                return -1;
            }

            return right;
        }
    }
}
