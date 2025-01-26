using System;
using System.Numerics;

namespace Ryujinx.Graphics.Texture.Astc
{
    internal struct IntegerEncoded(IntegerEncoded.EIntegerEncoding encoding, int numBits)
    {
        internal const int StructSize = 8;
        private static readonly IntegerEncoded[] _encodings;

        public enum EIntegerEncoding : byte
        {
            JustBits,
            Quint,
            Trit,
        }

        readonly EIntegerEncoding _encoding = encoding;
        public byte NumberBits { get; } = (byte)numBits;
        public byte TritValue { get; private set; } = 0;
        public byte QuintValue { get; private set; } = 0;
        public int BitValue { get; private set; } = 0;

        static IntegerEncoded()
        {
            _encodings = new IntegerEncoded[0x100];

            for (int i = 0; i < _encodings.Length; i++)
            {
                _encodings[i] = CreateEncodingCalc(i);
            }
        }

        public readonly bool MatchesEncoding(IntegerEncoded other)
        {
            return _encoding == other._encoding && NumberBits == other.NumberBits;
        }

        public readonly EIntegerEncoding GetEncoding()
        {
            return _encoding;
        }

        public readonly int GetBitLength(int numberVals)
        {
            int totalBits = NumberBits * numberVals;
            if (_encoding == EIntegerEncoding.Trit)
            {
                totalBits += (numberVals * 8 + 4) / 5;
            }
            else if (_encoding == EIntegerEncoding.Quint)
            {
                totalBits += (numberVals * 7 + 2) / 3;
            }
            return totalBits;
        }

        public static IntegerEncoded CreateEncoding(int maxVal)
        {
            return _encodings[maxVal];
        }

        private static IntegerEncoded CreateEncodingCalc(int maxVal)
        {
            while (maxVal > 0)
            {
                int check = maxVal + 1;

                // Is maxVal a power of two?
                if ((check & (check - 1)) == 0)
                {
                    return new IntegerEncoded(EIntegerEncoding.JustBits, BitOperations.PopCount((uint)maxVal));
                }

                // Is maxVal of the type 3*2^n - 1?
                if ((check % 3 == 0) && ((check / 3) & ((check / 3) - 1)) == 0)
                {
                    return new IntegerEncoded(EIntegerEncoding.Trit, BitOperations.PopCount((uint)(check / 3 - 1)));
                }

                // Is maxVal of the type 5*2^n - 1?
                if ((check % 5 == 0) && ((check / 5) & ((check / 5) - 1)) == 0)
                {
                    return new IntegerEncoded(EIntegerEncoding.Quint, BitOperations.PopCount((uint)(check / 5 - 1)));
                }

                // Apparently it can't be represented with a bounded integer sequence...
                // just iterate.
                maxVal--;
            }

            return new IntegerEncoded(EIntegerEncoding.JustBits, 0);
        }

        public static void DecodeTritBlock(
            ref BitStream128 bitStream,
            ref IntegerSequence listIntegerEncoded,
            int numberBitsPerValue)
        {
            // Implement the algorithm in section C.2.12
            Span<int> m = stackalloc int[5];

            m[0] = bitStream.ReadBits(numberBitsPerValue);
            int encoded = bitStream.ReadBits(2);
            m[1] = bitStream.ReadBits(numberBitsPerValue);
            encoded |= bitStream.ReadBits(2) << 2;
            m[2] = bitStream.ReadBits(numberBitsPerValue);
            encoded |= bitStream.ReadBits(1) << 4;
            m[3] = bitStream.ReadBits(numberBitsPerValue);
            encoded |= bitStream.ReadBits(2) << 5;
            m[4] = bitStream.ReadBits(numberBitsPerValue);
            encoded |= bitStream.ReadBits(1) << 7;

            ReadOnlySpan<byte> encodings = GetTritEncoding(encoded);

            IntegerEncoded intEncoded = new(EIntegerEncoding.Trit, numberBitsPerValue);

            for (int i = 0; i < 5; i++)
            {
                intEncoded.BitValue = m[i];
                intEncoded.TritValue = encodings[i];

                listIntegerEncoded.Add(ref intEncoded);
            }
        }

        public static void DecodeQuintBlock(
            ref BitStream128 bitStream,
            ref IntegerSequence listIntegerEncoded,
            int numberBitsPerValue)
        {
            ReadOnlySpan<byte> interleavedBits = [3, 2, 2];

            // Implement the algorithm in section C.2.12
            Span<int> m = stackalloc int[3];
            ulong encoded = 0;
            int encodedBitsRead = 0;

            for (int i = 0; i < m.Length; i++)
            {
                m[i] = bitStream.ReadBits(numberBitsPerValue);

                uint encodedBits = (uint)bitStream.ReadBits(interleavedBits[i]);

                encoded |= encodedBits << encodedBitsRead;
                encodedBitsRead += interleavedBits[i];
            }

            ReadOnlySpan<byte> encodings = GetQuintEncoding((int)encoded);

            for (int i = 0; i < 3; i++)
            {
                IntegerEncoded intEncoded = new(EIntegerEncoding.Quint, numberBitsPerValue)
                {
                    BitValue = m[i],
                    QuintValue = encodings[i],
                };

                listIntegerEncoded.Add(ref intEncoded);
            }
        }

        public static void DecodeIntegerSequence(
            ref IntegerSequence decodeIntegerSequence,
            ref BitStream128 bitStream,
            int maxRange,
            int numberValues)
        {
            // Determine encoding parameters
            IntegerEncoded intEncoded = CreateEncoding(maxRange);

            // Start decoding
            int numberValuesDecoded = 0;
            while (numberValuesDecoded < numberValues)
            {
                switch (intEncoded.GetEncoding())
                {
                    case EIntegerEncoding.Quint:
                        {
                            DecodeQuintBlock(ref bitStream, ref decodeIntegerSequence, intEncoded.NumberBits);
                            numberValuesDecoded += 3;

                            break;
                        }

                    case EIntegerEncoding.Trit:
                        {
                            DecodeTritBlock(ref bitStream, ref decodeIntegerSequence, intEncoded.NumberBits);
                            numberValuesDecoded += 5;

                            break;
                        }

                    case EIntegerEncoding.JustBits:
                        {
                            intEncoded.BitValue = bitStream.ReadBits(intEncoded.NumberBits);
                            decodeIntegerSequence.Add(ref intEncoded);
                            numberValuesDecoded++;

                            break;
                        }
                }
            }
        }

        private static ReadOnlySpan<byte> GetTritEncoding(int index)
        {
            return TritEncodings.Slice(index * 5, 5);
        }

        private static ReadOnlySpan<byte> GetQuintEncoding(int index)
        {
            return QuintEncodings.Slice(index * 3, 3);
        }

        private static ReadOnlySpan<byte> TritEncodings =>
        [
            0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 2, 0, 0, 0, 0,
            0, 0, 2, 0, 0, 0, 1, 0, 0, 0, 1, 1, 0, 0, 0,
            2, 1, 0, 0, 0, 1, 0, 2, 0, 0, 0, 2, 0, 0, 0,
            1, 2, 0, 0, 0, 2, 2, 0, 0, 0, 2, 0, 2, 0, 0,
            0, 2, 2, 0, 0, 1, 2, 2, 0, 0, 2, 2, 2, 0, 0,
            2, 0, 2, 0, 0, 0, 0, 1, 0, 0, 1, 0, 1, 0, 0,
            2, 0, 1, 0, 0, 0, 1, 2, 0, 0, 0, 1, 1, 0, 0,
            1, 1, 1, 0, 0, 2, 1, 1, 0, 0, 1, 1, 2, 0, 0,
            0, 2, 1, 0, 0, 1, 2, 1, 0, 0, 2, 2, 1, 0, 0,
            2, 1, 2, 0, 0, 0, 0, 0, 2, 2, 1, 0, 0, 2, 2,
            2, 0, 0, 2, 2, 0, 0, 2, 2, 2, 0, 0, 0, 1, 0,
            1, 0, 0, 1, 0, 2, 0, 0, 1, 0, 0, 0, 2, 1, 0,
            0, 1, 0, 1, 0, 1, 1, 0, 1, 0, 2, 1, 0, 1, 0,
            1, 0, 2, 1, 0, 0, 2, 0, 1, 0, 1, 2, 0, 1, 0,
            2, 2, 0, 1, 0, 2, 0, 2, 1, 0, 0, 2, 2, 1, 0,
            1, 2, 2, 1, 0, 2, 2, 2, 1, 0, 2, 0, 2, 1, 0,
            0, 0, 1, 1, 0, 1, 0, 1, 1, 0, 2, 0, 1, 1, 0,
            0, 1, 2, 1, 0, 0, 1, 1, 1, 0, 1, 1, 1, 1, 0,
            2, 1, 1, 1, 0, 1, 1, 2, 1, 0, 0, 2, 1, 1, 0,
            1, 2, 1, 1, 0, 2, 2, 1, 1, 0, 2, 1, 2, 1, 0,
            0, 1, 0, 2, 2, 1, 1, 0, 2, 2, 2, 1, 0, 2, 2,
            1, 0, 2, 2, 2, 0, 0, 0, 2, 0, 1, 0, 0, 2, 0,
            2, 0, 0, 2, 0, 0, 0, 2, 2, 0, 0, 1, 0, 2, 0,
            1, 1, 0, 2, 0, 2, 1, 0, 2, 0, 1, 0, 2, 2, 0,
            0, 2, 0, 2, 0, 1, 2, 0, 2, 0, 2, 2, 0, 2, 0,
            2, 0, 2, 2, 0, 0, 2, 2, 2, 0, 1, 2, 2, 2, 0,
            2, 2, 2, 2, 0, 2, 0, 2, 2, 0, 0, 0, 1, 2, 0,
            1, 0, 1, 2, 0, 2, 0, 1, 2, 0, 0, 1, 2, 2, 0,
            0, 1, 1, 2, 0, 1, 1, 1, 2, 0, 2, 1, 1, 2, 0,
            1, 1, 2, 2, 0, 0, 2, 1, 2, 0, 1, 2, 1, 2, 0,
            2, 2, 1, 2, 0, 2, 1, 2, 2, 0, 0, 2, 0, 2, 2,
            1, 2, 0, 2, 2, 2, 2, 0, 2, 2, 2, 0, 2, 2, 2,
            0, 0, 0, 0, 2, 1, 0, 0, 0, 2, 2, 0, 0, 0, 2,
            0, 0, 2, 0, 2, 0, 1, 0, 0, 2, 1, 1, 0, 0, 2,
            2, 1, 0, 0, 2, 1, 0, 2, 0, 2, 0, 2, 0, 0, 2,
            1, 2, 0, 0, 2, 2, 2, 0, 0, 2, 2, 0, 2, 0, 2,
            0, 2, 2, 0, 2, 1, 2, 2, 0, 2, 2, 2, 2, 0, 2,
            2, 0, 2, 0, 2, 0, 0, 1, 0, 2, 1, 0, 1, 0, 2,
            2, 0, 1, 0, 2, 0, 1, 2, 0, 2, 0, 1, 1, 0, 2,
            1, 1, 1, 0, 2, 2, 1, 1, 0, 2, 1, 1, 2, 0, 2,
            0, 2, 1, 0, 2, 1, 2, 1, 0, 2, 2, 2, 1, 0, 2,
            2, 1, 2, 0, 2, 0, 2, 2, 2, 2, 1, 2, 2, 2, 2,
            2, 2, 2, 2, 2, 2, 0, 2, 2, 2, 0, 0, 0, 0, 1,
            1, 0, 0, 0, 1, 2, 0, 0, 0, 1, 0, 0, 2, 0, 1,
            0, 1, 0, 0, 1, 1, 1, 0, 0, 1, 2, 1, 0, 0, 1,
            1, 0, 2, 0, 1, 0, 2, 0, 0, 1, 1, 2, 0, 0, 1,
            2, 2, 0, 0, 1, 2, 0, 2, 0, 1, 0, 2, 2, 0, 1,
            1, 2, 2, 0, 1, 2, 2, 2, 0, 1, 2, 0, 2, 0, 1,
            0, 0, 1, 0, 1, 1, 0, 1, 0, 1, 2, 0, 1, 0, 1,
            0, 1, 2, 0, 1, 0, 1, 1, 0, 1, 1, 1, 1, 0, 1,
            2, 1, 1, 0, 1, 1, 1, 2, 0, 1, 0, 2, 1, 0, 1,
            1, 2, 1, 0, 1, 2, 2, 1, 0, 1, 2, 1, 2, 0, 1,
            0, 0, 1, 2, 2, 1, 0, 1, 2, 2, 2, 0, 1, 2, 2,
            0, 1, 2, 2, 2, 0, 0, 0, 1, 1, 1, 0, 0, 1, 1,
            2, 0, 0, 1, 1, 0, 0, 2, 1, 1, 0, 1, 0, 1, 1,
            1, 1, 0, 1, 1, 2, 1, 0, 1, 1, 1, 0, 2, 1, 1,
            0, 2, 0, 1, 1, 1, 2, 0, 1, 1, 2, 2, 0, 1, 1,
            2, 0, 2, 1, 1, 0, 2, 2, 1, 1, 1, 2, 2, 1, 1,
            2, 2, 2, 1, 1, 2, 0, 2, 1, 1, 0, 0, 1, 1, 1,
            1, 0, 1, 1, 1, 2, 0, 1, 1, 1, 0, 1, 2, 1, 1,
            0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2, 1, 1, 1, 1,
            1, 1, 2, 1, 1, 0, 2, 1, 1, 1, 1, 2, 1, 1, 1,
            2, 2, 1, 1, 1, 2, 1, 2, 1, 1, 0, 1, 1, 2, 2,
            1, 1, 1, 2, 2, 2, 1, 1, 2, 2, 1, 1, 2, 2, 2,
            0, 0, 0, 2, 1, 1, 0, 0, 2, 1, 2, 0, 0, 2, 1,
            0, 0, 2, 2, 1, 0, 1, 0, 2, 1, 1, 1, 0, 2, 1,
            2, 1, 0, 2, 1, 1, 0, 2, 2, 1, 0, 2, 0, 2, 1,
            1, 2, 0, 2, 1, 2, 2, 0, 2, 1, 2, 0, 2, 2, 1,
            0, 2, 2, 2, 1, 1, 2, 2, 2, 1, 2, 2, 2, 2, 1,
            2, 0, 2, 2, 1, 0, 0, 1, 2, 1, 1, 0, 1, 2, 1,
            2, 0, 1, 2, 1, 0, 1, 2, 2, 1, 0, 1, 1, 2, 1,
            1, 1, 1, 2, 1, 2, 1, 1, 2, 1, 1, 1, 2, 2, 1,
            0, 2, 1, 2, 1, 1, 2, 1, 2, 1, 2, 2, 1, 2, 1,
            2, 1, 2, 2, 1, 0, 2, 1, 2, 2, 1, 2, 1, 2, 2,
            2, 2, 1, 2, 2, 2, 1, 2, 2, 2, 0, 0, 0, 1, 2,
            1, 0, 0, 1, 2, 2, 0, 0, 1, 2, 0, 0, 2, 1, 2,
            0, 1, 0, 1, 2, 1, 1, 0, 1, 2, 2, 1, 0, 1, 2,
            1, 0, 2, 1, 2, 0, 2, 0, 1, 2, 1, 2, 0, 1, 2,
            2, 2, 0, 1, 2, 2, 0, 2, 1, 2, 0, 2, 2, 1, 2,
            1, 2, 2, 1, 2, 2, 2, 2, 1, 2, 2, 0, 2, 1, 2,
            0, 0, 1, 1, 2, 1, 0, 1, 1, 2, 2, 0, 1, 1, 2,
            0, 1, 2, 1, 2, 0, 1, 1, 1, 2, 1, 1, 1, 1, 2,
            2, 1, 1, 1, 2, 1, 1, 2, 1, 2, 0, 2, 1, 1, 2,
            1, 2, 1, 1, 2, 2, 2, 1, 1, 2, 2, 1, 2, 1, 2,
            0, 2, 2, 2, 2, 1, 2, 2, 2, 2, 2, 2, 2, 2, 2,
            2, 1, 2, 2, 2
        ];

        private static ReadOnlySpan<byte> QuintEncodings =>
        [
            0, 0, 0, 1, 0, 0, 2, 0, 0, 3, 0, 0, 4, 0, 0,
            0, 4, 0, 4, 4, 0, 4, 4, 4, 0, 1, 0, 1, 1, 0,
            2, 1, 0, 3, 1, 0, 4, 1, 0, 1, 4, 0, 4, 4, 1,
            4, 4, 4, 0, 2, 0, 1, 2, 0, 2, 2, 0, 3, 2, 0,
            4, 2, 0, 2, 4, 0, 4, 4, 2, 4, 4, 4, 0, 3, 0,
            1, 3, 0, 2, 3, 0, 3, 3, 0, 4, 3, 0, 3, 4, 0,
            4, 4, 3, 4, 4, 4, 0, 0, 1, 1, 0, 1, 2, 0, 1,
            3, 0, 1, 4, 0, 1, 0, 4, 1, 4, 0, 4, 0, 4, 4,
            0, 1, 1, 1, 1, 1, 2, 1, 1, 3, 1, 1, 4, 1, 1,
            1, 4, 1, 4, 1, 4, 1, 4, 4, 0, 2, 1, 1, 2, 1,
            2, 2, 1, 3, 2, 1, 4, 2, 1, 2, 4, 1, 4, 2, 4,
            2, 4, 4, 0, 3, 1, 1, 3, 1, 2, 3, 1, 3, 3, 1,
            4, 3, 1, 3, 4, 1, 4, 3, 4, 3, 4, 4, 0, 0, 2,
            1, 0, 2, 2, 0, 2, 3, 0, 2, 4, 0, 2, 0, 4, 2,
            2, 0, 4, 3, 0, 4, 0, 1, 2, 1, 1, 2, 2, 1, 2,
            3, 1, 2, 4, 1, 2, 1, 4, 2, 2, 1, 4, 3, 1, 4,
            0, 2, 2, 1, 2, 2, 2, 2, 2, 3, 2, 2, 4, 2, 2,
            2, 4, 2, 2, 2, 4, 3, 2, 4, 0, 3, 2, 1, 3, 2,
            2, 3, 2, 3, 3, 2, 4, 3, 2, 3, 4, 2, 2, 3, 4,
            3, 3, 4, 0, 0, 3, 1, 0, 3, 2, 0, 3, 3, 0, 3,
            4, 0, 3, 0, 4, 3, 0, 0, 4, 1, 0, 4, 0, 1, 3,
            1, 1, 3, 2, 1, 3, 3, 1, 3, 4, 1, 3, 1, 4, 3,
            0, 1, 4, 1, 1, 4, 0, 2, 3, 1, 2, 3, 2, 2, 3,
            3, 2, 3, 4, 2, 3, 2, 4, 3, 0, 2, 4, 1, 2, 4,
            0, 3, 3, 1, 3, 3, 2, 3, 3, 3, 3, 3, 4, 3, 3,
            3, 4, 3, 0, 3, 4, 1, 3, 4
        ];
    }
}
