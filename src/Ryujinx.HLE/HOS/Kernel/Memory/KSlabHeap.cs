using System.Collections.Generic;

namespace Ryujinx.HLE.HOS.Kernel.Memory
{
    class KSlabHeap
    {
        private readonly LinkedList<ulong> _items;

        public KSlabHeap(ulong pa, ulong itemSize, ulong size)
        {
            _items = [];

            int itemsCount = (int)(size / itemSize);

            for (int index = 0; index < itemsCount; index++)
            {
                _items.AddLast(pa);

                pa += itemSize;
            }
        }

        public bool TryGetItem(out ulong pa)
        {
            lock (_items)
            {
                if (_items.First != null)
                {
                    pa = _items.First.Value;

                    _items.RemoveFirst();

                    return true;
                }
            }

            pa = 0;

            return false;
        }

        public void Free(ulong pa)
        {
            lock (_items)
            {
                _items.AddFirst(pa);
            }
        }
    }
}
