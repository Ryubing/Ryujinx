using System;
using System.Collections.Generic;

namespace Ryujinx.Graphics.Vulkan
{
    /// <summary>
    /// A structure tracking pending upload ranges for buffers.
    /// Where a range is present, pending data exists that can either be used to build mirrors
    /// or upload directly to the buffer.
    /// </summary>
    struct BufferMirrorRangeList
    {
        internal readonly struct Range
        {
            public int Offset { get; }
            public int Size { get; }

            public int End => Offset + Size;

            public Range(int offset, int size)
            {
                Offset = offset;
                Size = size;
            }

            public bool OverlapsWith(int offset, int size)
            {
                return Offset < offset + size && offset < Offset + Size;
            }
        }

        private List<Range> _ranges;

        public readonly IEnumerable<Range> All()
        {
            return _ranges;
        }

        public readonly bool Remove(int offset, int size)
        {
            var list = _ranges;
            bool removedAny = false;
            if (list != null)
            {
                int overlapIndex = BinarySearch(list, offset, size);

                if (overlapIndex >= 0)
                {
                    // Overlaps with a range. Search back to find the first one it doesn't overlap with.

                    while (overlapIndex > 0 && list[overlapIndex - 1].OverlapsWith(offset, size))
                    {
                        overlapIndex--;
                    }

                    int endOffset = offset + size;
                    int startIndex = overlapIndex;

                    var currentOverlap = list[overlapIndex];

                    // Orphan the start of the overlap.
                    if (currentOverlap.Offset < offset)
                    {
                        list[overlapIndex] = new Range(currentOverlap.Offset, offset - currentOverlap.Offset);
                        currentOverlap = new Range(offset, currentOverlap.End - offset);
                        list.Insert(++overlapIndex, currentOverlap);
                        startIndex++;

                        removedAny = true;
                    }

                    // Remove any middle overlaps.
                    while (currentOverlap.Offset < endOffset)
                    {
                        if (currentOverlap.End > endOffset)
                        {
                            // Update the end overlap instead of removing it, if it spans beyond the removed range.
                            list[overlapIndex] = new Range(endOffset, currentOverlap.End - endOffset);

                            removedAny = true;
                            break;
                        }

                        if (++overlapIndex >= list.Count)
                        {
                            break;
                        }

                        currentOverlap = list[overlapIndex];
                    }

                    int count = overlapIndex - startIndex;

                    list.RemoveRange(startIndex, count);

                    removedAny |= count > 0;
                }
            }

            return removedAny;
        }

        public void Add(int offset, int size)
        {
            var list = _ranges;
            if (list != null)
            {
                int overlapIndex = BinarySearch(list, offset, size);
                if (overlapIndex >= 0)
                {
                    while (overlapIndex > 0 && list[overlapIndex - 1].OverlapsWith(offset, size))
                    {
                        overlapIndex--;
                    }

                    int endOffset = offset + size;
                    int startIndex = overlapIndex;

                    while (overlapIndex < list.Count && list[overlapIndex].OverlapsWith(offset, size))
                    {
                        var currentOverlap = list[overlapIndex];
                        var currentOverlapEndOffset = currentOverlap.Offset + currentOverlap.Size;

                        if (offset > currentOverlap.Offset)
                        {
                            offset = currentOverlap.Offset;
                        }

                        if (endOffset < currentOverlapEndOffset)
                        {
                            endOffset = currentOverlapEndOffset;
                        }

                        overlapIndex++;
                        size = endOffset - offset;
                    }

                    int count = overlapIndex - startIndex;

                    list.RemoveRange(startIndex, count);

                    overlapIndex = startIndex;
                }
                else
                {
                    overlapIndex = ~overlapIndex;
                }

                list.Insert(overlapIndex, new Range(offset, size));
            }
            else
            {
                _ranges = new List<Range>
                {
                    new Range(offset, size)
                };
            }
        }

        public readonly bool OverlapsWith(int offset, int size)
        {
            var list = _ranges;
            if (list == null)
            {
                return false;
            }

            return BinarySearch(list, offset, size) >= 0;
        }

        public readonly List<Range> FindOverlaps(int offset, int size)
        {
            var list = _ranges;
            if (list == null)
            {
                return null;
            }
            
            List<Range> result = null;

            int index = BinarySearch(list, offset, size);

            if (index >= 0)
            {
                while (index > 0 && list[index - 1].OverlapsWith(offset, size))
                {
                    index--;
                }

                do
                {
                    (result ??= []).Add(list[index++]);
                }
                while (index < list.Count && list[index].OverlapsWith(offset, size));
            }

            return result;
        }

        private static int BinarySearch(List<Range> list, int offset, int size)
        {
            int left = 0;
            int right = list.Count - 1;

            while (left <= right)
            {
                int range = right - left;

                int middle = left + (range >> 1);

                var item = list[middle];

                if (item.OverlapsWith(offset, size))
                {
                    return middle;
                }

                if (offset < item.Offset)
                {
                    right = middle - 1;
                }
                else
                {
                    left = middle + 1;
                }
            }

            return ~left;
        }

        public readonly void FillData(Span<byte> baseData, Span<byte> modData, int offset, Span<byte> result)
        {
            int size = baseData.Length;
            int endOffset = offset + size;

            var list = _ranges;
            if (list == null)
            {
                baseData.CopyTo(result);
            }

            int srcOffset = offset;
            int dstOffset = 0;
            bool activeRange = false;

            for (int i = 0; i < list.Count; i++)
            {
                var range = list[i];

                int rangeEnd = range.Offset + range.Size;

                if (activeRange)
                {
                    if (range.Offset >= endOffset)
                    {
                        break;
                    }
                }
                else
                {
                    if (rangeEnd <= offset)
                    {
                        continue;
                    }

                    activeRange = true;
                }

                int baseSize = range.Offset - srcOffset;

                if (baseSize > 0)
                {
                    baseData.Slice(dstOffset, baseSize).CopyTo(result.Slice(dstOffset, baseSize));
                    srcOffset += baseSize;
                    dstOffset += baseSize;
                }

                int modSize = Math.Min(rangeEnd - srcOffset, endOffset - srcOffset);
                if (modSize != 0)
                {
                    modData.Slice(dstOffset, modSize).CopyTo(result.Slice(dstOffset, modSize));
                    srcOffset += modSize;
                    dstOffset += modSize;
                }
            }

            int baseSizeEnd = endOffset - srcOffset;

            if (baseSizeEnd > 0)
            {
                baseData.Slice(dstOffset, baseSizeEnd).CopyTo(result.Slice(dstOffset, baseSizeEnd));
            }
        }

        public readonly int Count()
        {
            return _ranges?.Count ?? 0;
        }

        public void Clear()
        {
            _ranges = null;
        }
    }
}
