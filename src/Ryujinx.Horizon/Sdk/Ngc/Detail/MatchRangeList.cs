using System;

namespace Ryujinx.Horizon.Sdk.Ngc.Detail
{
    readonly struct MatchRange
    {
        public readonly int StartOffset;
        public readonly int EndOffset;

        public MatchRange(int startOffset, int endOffset)
        {
            StartOffset = startOffset;
            EndOffset = endOffset;
        }
    }

    struct MatchRangeList
    {
        private int _capacity;
        private int _count;
        private MatchRange[] _ranges;

        public readonly int Count => _count;

        public readonly MatchRange this[int index] => _ranges[index];

        public MatchRangeList()
        {
            _capacity = 0;
            _count = 0;
            _ranges = [];
        }

        public void Add(int startOffset, int endOffset)
        {
            if (_count == _capacity)
            {
                int newCapacity = _count * 2;

                if (newCapacity == 0)
                {
                    newCapacity = 1;
                }

                Array.Resize(ref _ranges, newCapacity);

                _capacity = newCapacity;
            }

            _ranges[_count++] = new(startOffset, endOffset);
        }

        public readonly MatchRangeList Deduplicate()
        {
            MatchRangeList output = new();

            if (_count != 0)
            {
                int prevStartOffset = _ranges[0].StartOffset;
                int prevEndOffset = _ranges[0].EndOffset;

                for (int index = 1; index < _count; index++)
                {
                    int currStartOffset = _ranges[index].StartOffset;
                    int currEndOffset = _ranges[index].EndOffset;

                    if (prevStartOffset == currStartOffset)
                    {
                        if (prevEndOffset <= currEndOffset)
                        {
                            prevEndOffset = currEndOffset;
                        }
                    }
                    else if (prevEndOffset <= currStartOffset)
                    {
                        output.Add(prevStartOffset, prevEndOffset);

                        prevStartOffset = currStartOffset;
                        prevEndOffset = currEndOffset;
                    }
                }

                output.Add(prevStartOffset, prevEndOffset);
            }

            return output;
        }

        public readonly int Find(int startOffset, int endOffset)
        {
            int baseIndex = 0;
            int range = _count;

            while (range != 0)
            {
                MatchRange currRange = _ranges[baseIndex + (range / 2)];

                if (currRange.StartOffset < startOffset || (currRange.StartOffset == startOffset && currRange.EndOffset < endOffset))
                {
                    int nextHalf = (range / 2) + 1;
                    baseIndex += nextHalf;
                    range -= nextHalf;
                }
                else
                {
                    range /= 2;
                }
            }

            return baseIndex;
        }
    }
}
