using Ryujinx.Common.Memory;
using Ryujinx.Graphics.Nvdec.Vp9.Types;
using System;
using System.Buffers.Binary;

namespace Ryujinx.Graphics.Nvdec.Vp9.Dsp
{
    internal struct Reader
    {
        private static readonly byte[] _norm =
        [
            0, 7, 6, 6, 5, 5, 5, 5, 4, 4, 4, 4, 4, 4, 4, 4, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 2, 2, 2,
            2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 1, 1, 1, 1, 1, 1,
            1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
            1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0
        ];

        private const int BdValueSize = sizeof(ulong) * 8;

        // This is meant to be a large, positive constant that can still be efficiently
        // loaded as an immediate (on platforms like ARM, for example).
        // Even relatively modest values like 100 would work fine.
        private const int LotsOfBits = 0x40000000;

        public ulong Value;
        public uint Range;
        public int Count;
        private ArrayPtr<byte> _buffer;

        public bool Init(ArrayPtr<byte> buffer, int size)
        {
            if (size != 0 && buffer.IsNull)
            {
                return true;
            }

            _buffer = new ArrayPtr<byte>(ref buffer[0], size);
            Value = 0;
            Count = -8;
            Range = 255;
            Fill();
            return ReadBit() != 0; // Marker bit
        }

        private void Fill()
        {
            ReadOnlySpan<byte> buffer = _buffer.AsSpan();
            ReadOnlySpan<byte> bufferStart = buffer;
            ulong value = Value;
            int count = Count;
            ulong bytesLeft = (ulong)buffer.Length;
            ulong bitsLeft = bytesLeft * 8;
            int shift = BdValueSize - 8 - (count + 8);

            if (bitsLeft > BdValueSize)
            {
                int bits = (shift & unchecked((int)0xfffffff8)) + 8;
                ulong nv;
                ulong bigEndianValues = BinaryPrimitives.ReadUInt64BigEndian(buffer);
                nv = bigEndianValues >> (BdValueSize - bits);
                count += bits;
                buffer = buffer.Slice(bits >> 3);
                value = Value | (nv << (shift & 0x7));
            }
            else
            {
                int bitsOver = shift + 8 - (int)bitsLeft;
                int loopEnd = 0;
                if (bitsOver >= 0)
                {
                    count += LotsOfBits;
                    loopEnd = bitsOver;
                }

                if (bitsOver < 0 || bitsLeft != 0)
                {
                    while (shift >= loopEnd)
                    {
                        count += 8;
                        value |= (ulong)buffer[0] << shift;
                        buffer = buffer.Slice(1);
                        shift -= 8;
                    }
                }
            }

            // NOTE: Variable 'buffer' may not relate to '_buffer' after decryption,
            // so we increase '_buffer' by the amount that 'buffer' moved, rather than
            // assign 'buffer' to '_buffer'.
            _buffer = _buffer.Slice(bufferStart.Length - buffer.Length);
            Value = value;
            Count = count;
        }

        public bool HasError()
        {
            // Check if we have reached the end of the buffer.
            //
            // Variable 'count' stores the number of bits in the 'value' buffer, minus
            // 8. The top byte is part of the algorithm, and the remainder is buffered
            // to be shifted into it. So if count == 8, the top 16 bits of 'value' are
            // occupied, 8 for the algorithm and 8 in the buffer.
            //
            // When reading a byte from the user's buffer, count is filled with 8 and
            // one byte is filled into the value buffer. When we reach the end of the
            // data, count is additionally filled with LotsOfBits. So when
            // count == LotsOfBits - 1, the user's data has been exhausted.
            //
            // 1 if we have tried to decode bits after the end of stream was encountered.
            // 0 No error.
            return Count > BdValueSize && Count < LotsOfBits;
        }

        public int Read(int prob)
        {
            uint bit = 0;
            ulong value;
            ulong bigsplit;
            int count;
            uint range;
            uint split = ((Range * (uint)prob) + (256 - (uint)prob)) >> 8;

            if (Count < 0)
            {
                Fill();
            }

            value = Value;
            count = Count;

            bigsplit = (ulong)split << (BdValueSize - 8);

            range = split;

            if (value >= bigsplit)
            {
                range = Range - split;
                value -= bigsplit;
                bit = 1;
            }

            {
                int shift = _norm[range];
                range <<= shift;
                value <<= shift;
                count -= shift;
            }
            Value = value;
            Count = count;
            Range = range;

            return (int)bit;
        }

        public int ReadBit()
        {
            return Read(128); // vpx_prob_half
        }

        public int ReadLiteral(int bits)
        {
            int literal = 0, bit;

            for (bit = bits - 1; bit >= 0; bit--)
            {
                literal |= ReadBit() << bit;
            }

            return literal;
        }

        public int ReadTree(ReadOnlySpan<sbyte> tree, ReadOnlySpan<byte> probs)
        {
            sbyte i = 0;

            while ((i = tree[i + Read(probs[i >> 1])]) > 0)
            {
            }

            return -i;
        }

        public int ReadBool(int prob, ref ulong value, ref int count, ref uint range)
        {
            uint split = ((range * (uint)prob) + (256 - (uint)prob)) >> 8;
            ulong bigsplit = (ulong)split << (BdValueSize - 8);

            if (count < 0)
            {
                Value = value;
                Count = count;
                Fill();
                value = Value;
                count = Count;
            }

            if (value >= bigsplit)
            {
                range = range - split;
                value = value - bigsplit;
                {
                    int shift = _norm[range];
                    range <<= shift;
                    value <<= shift;
                    count -= shift;
                }
                return 1;
            }

            range = split;
            {
                int shift = _norm[range];
                range <<= shift;
                value <<= shift;
                count -= shift;
            }
            return 0;
        }

        public ArrayPtr<byte> FindEnd()
        {
            // Find the end of the coded buffer
            while (Count > 8 && Count < BdValueSize)
            {
                Count -= 8;
                _buffer = _buffer.Slice(-1);
            }

            return _buffer;
        }

        private int DecodeUniform()
        {
            const int L = 8;
            const int M = (1 << L) - 191;
            int v = ReadLiteral(L - 1);
            return v < M ? v : (v << 1) - M + ReadBit();
        }

        public int DecodeTermSubexp()
        {
            if (ReadBit() == 0)
            {
                return ReadLiteral(4);
            }

            if (ReadBit() == 0)
            {
                return ReadLiteral(4) + 16;
            }

            if (ReadBit() == 0)
            {
                return ReadLiteral(5) + 32;
            }

            return DecodeUniform() + 64;
        }

        public TxMode ReadTxMode()
        {
            TxMode txMode = (TxMode)ReadLiteral(2);
            if (txMode == TxMode.Allow32X32)
            {
                txMode += ReadBit();
            }

            return txMode;
        }

        public int ReadCoeff(
            ReadOnlySpan<byte> probs,
            int n,
            ref ulong value,
            ref int count,
            ref uint range)
        {
            int val = 0;
            for (int i = 0; i < n; ++i)
            {
                val = (val << 1) | ReadBool(probs[i], ref value, ref count, ref range);
            }

            return val;
        }

        public void DiffUpdateProb(ref byte p)
        {
            if (Read(Entropy.DiffUpdateProb) != 0)
            {
                p = (byte)DSubExp.InvRemapProb(DecodeTermSubexp(), p);
            }
        }

        public void UpdateMvProbs(Span<byte> p, int n)
        {
            for (int i = 0; i < n; ++i)
            {
                if (Read(EntropyMv.UpdateProb) != 0)
                {
                    p[i] = (byte)((ReadLiteral(7) << 1) | 1);
                }
            }
        }
    }
}