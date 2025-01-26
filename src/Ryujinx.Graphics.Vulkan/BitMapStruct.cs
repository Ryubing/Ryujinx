using Ryujinx.Common.Memory;
using System;
using System.Numerics;

namespace Ryujinx.Graphics.Vulkan
{
    interface IBitMapListener
    {
        void BitMapSignal(int index, int count);
    }

    struct BitMapStruct<T> where T : IArray<long>
    {
        public const int IntSize = 64;

        private const int IntShift = 6;
        private const int IntMask = IntSize - 1;

        private T _masks;

        public BitMapStruct()
        {
            _masks = default;
        }

        public bool BecomesUnsetFrom(in BitMapStruct<T> from, ref BitMapStruct<T> into)
        {
            bool result = false;

            int masks = _masks.Length;
            for (int i = 0; i < masks; i++)
            {
                long fromMask = from._masks[i];
                long unsetMask = (~fromMask) & (fromMask ^ _masks[i]);
                into._masks[i] = unsetMask;

                result |= unsetMask != 0;
            }

            return result;
        }

        public void SetAndSignalUnset<T2>(in BitMapStruct<T> from, ref T2 listener) where T2 : struct, IBitMapListener
        {
            BitMapStruct<T> result = new();

            if (BecomesUnsetFrom(from, ref result))
            {
                // Iterate the set bits in the result, and signal them.

                int offset = 0;
                int masks = _masks.Length;
                ref T resultMasks = ref result._masks;
                for (int i = 0; i < masks; i++)
                {
                    long value = resultMasks[i];
                    while (value != 0)
                    {
                        int bit = BitOperations.TrailingZeroCount((ulong)value);

                        listener.BitMapSignal(offset + bit, 1);

                        value &= ~(1L << bit);
                    }

                    offset += IntSize;
                }
            }

            _masks = from._masks;
        }

        public void SignalSet(Action<int, int> action)
        {
            // Iterate the set bits in the result, and signal them.

            int offset = 0;
            int masks = _masks.Length;
            for (int i = 0; i < masks; i++)
            {
                long value = _masks[i];
                while (value != 0)
                {
                    int bit = BitOperations.TrailingZeroCount((ulong)value);

                    action(offset + bit, 1);

                    value &= ~(1L << bit);
                }

                offset += IntSize;
            }
        }

        public bool AnySet()
        {
            for (int i = 0; i < _masks.Length; i++)
            {
                if (_masks[i] != 0)
                {
                    return true;
                }
            }

            return false;
        }

        public bool IsSet(int bit)
        {
            int wordIndex = bit >> IntShift;
            int wordBit = bit & IntMask;

            long wordMask = 1L << wordBit;

            return (_masks[wordIndex] & wordMask) != 0;
        }

        public bool IsSet(int start, int end)
        {
            if (start == end)
            {
                return IsSet(start);
            }

            int startIndex = start >> IntShift;
            int startBit = start & IntMask;
            long startMask = -1L << startBit;

            int endIndex = end >> IntShift;
            int endBit = end & IntMask;
            long endMask = (long)(ulong.MaxValue >> (IntMask - endBit));

            if (startIndex == endIndex)
            {
                return (_masks[startIndex] & startMask & endMask) != 0;
            }

            if ((_masks[startIndex] & startMask) != 0)
            {
                return true;
            }

            for (int i = startIndex + 1; i < endIndex; i++)
            {
                if (_masks[i] != 0)
                {
                    return true;
                }
            }

            if ((_masks[endIndex] & endMask) != 0)
            {
                return true;
            }

            return false;
        }

        public bool Set(int bit)
        {
            int wordIndex = bit >> IntShift;
            int wordBit = bit & IntMask;

            long wordMask = 1L << wordBit;

            if ((_masks[wordIndex] & wordMask) != 0)
            {
                return false;
            }

            _masks[wordIndex] |= wordMask;

            return true;
        }

        public void Set(int bit, bool value)
        {
            if (value)
            {
                Set(bit);
            }
            else
            {
                Clear(bit);
            }
        }

        public void SetRange(int start, int end)
        {
            if (start == end)
            {
                Set(start);
                return;
            }

            int startIndex = start >> IntShift;
            int startBit = start & IntMask;
            long startMask = -1L << startBit;

            int endIndex = end >> IntShift;
            int endBit = end & IntMask;
            long endMask = (long)(ulong.MaxValue >> (IntMask - endBit));

            if (startIndex == endIndex)
            {
                _masks[startIndex] |= startMask & endMask;
            }
            else
            {
                _masks[startIndex] |= startMask;

                for (int i = startIndex + 1; i < endIndex; i++)
                {
                    _masks[i] |= -1L;
                }

                _masks[endIndex] |= endMask;
            }
        }

        public BitMapStruct<T> Union(BitMapStruct<T> other)
        {
            BitMapStruct<T> result = new();

            ref T masks = ref _masks;
            ref T otherMasks = ref other._masks;
            ref T newMasks = ref result._masks;

            for (int i = 0; i < masks.Length; i++)
            {
                newMasks[i] = masks[i] | otherMasks[i];
            }

            return result;
        }

        public void Clear(int bit)
        {
            int wordIndex = bit >> IntShift;
            int wordBit = bit & IntMask;

            long wordMask = 1L << wordBit;

            _masks[wordIndex] &= ~wordMask;
        }

        public void Clear()
        {
            for (int i = 0; i < _masks.Length; i++)
            {
                _masks[i] = 0;
            }
        }

        public void ClearInt(int start, int end)
        {
            for (int i = start; i <= end; i++)
            {
                _masks[i] = 0;
            }
        }
    }
}
