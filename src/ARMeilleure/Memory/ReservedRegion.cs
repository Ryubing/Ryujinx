using System;

namespace ARMeilleure.Memory
{
    public class ReservedRegion
    {
        public const int DefaultGranularity = 65536; // Mapping granularity in Windows.

        public IJitMemoryBlock Block { get; }
        public IJitMemoryAllocator Allocator { get; }

        public nint Pointer => Block.Pointer;

        private readonly ulong _maxSize;
        private readonly ulong _sizeGranularity;
        private ulong _currentSize;

        public ReservedRegion(IJitMemoryAllocator allocator, ulong maxSize, ulong granularity = 0)
        {
            if (granularity == 0)
            {
                granularity = DefaultGranularity;
            }

            Allocator = allocator;
            Block = allocator.Reserve(maxSize);
            _maxSize = maxSize;
            _sizeGranularity = granularity;
            _currentSize = 0;
        }

        public void ExpandIfNeeded(ulong desiredSize)
        {
            if (desiredSize > _maxSize)
            {
                throw new OutOfMemoryException();
            }

            if (desiredSize > _currentSize)
            {
                // Lock, and then check again. We only want to commit once.
                lock (this)
                {
                    if (desiredSize >= _currentSize)
                    {
                        ulong overflowBytes = desiredSize - _currentSize;
                        ulong moreToCommit = (((_sizeGranularity - 1) + overflowBytes) / _sizeGranularity) * _sizeGranularity; // Round up.
                        Block.Commit(_currentSize, moreToCommit);
                        _currentSize += moreToCommit;
                    }
                }
            }
        }

        public void Dispose()
        {
            Block.Dispose();
        }
    }
}
