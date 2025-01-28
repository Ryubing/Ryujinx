using System;

namespace ARMeilleure.CodeGen.RegisterAllocators
{
    unsafe struct UseList
    {
        private int* _items;
        private int _capacity;

        public int Count { get; private set; }

        public readonly int FirstUse => Count > 0 ? _items[Count - 1] : LiveInterval.NotFound;
        public readonly Span<int> Span => new(_items, Count);

        public void Add(int position)
        {
            if (Count + 1 > _capacity)
            {
                Span<int> oldSpan = Span;

                _capacity = Math.Max(4, _capacity * 2);
                _items = Allocators.Default.Allocate<int>((uint)_capacity);

                Span<int> newSpan = Span;

                oldSpan.CopyTo(newSpan);
            }

            // Use positions are usually inserted in descending order, so inserting in descending order is faster,
            // since the number of half exchanges is reduced.
            int i = Count - 1;

            while (i >= 0 && _items[i] < position)
            {
                _items[i + 1] = _items[i--];
            }

            _items[i + 1] = position;
            Count++;
        }

        public readonly int NextUse(int position)
        {
            int index = NextUseIndex(position);

            return index != LiveInterval.NotFound ? _items[index] : LiveInterval.NotFound;
        }

        public readonly int NextUseIndex(int position)
        {
            int i = Count - 1;

            if (i == -1 || position > _items[0])
            {
                return LiveInterval.NotFound;
            }

            while (i >= 0 && _items[i] < position)
            {
                i--;
            }

            return i;
        }

        public UseList Split(int position)
        {
            int index = NextUseIndex(position);

            // Since the list is in descending order, the new split list takes the front of the list and the current
            // list takes the back of the list.
            UseList result = new()
            {
                Count = index + 1,
            };
            result._capacity = result.Count;
            result._items = _items;

            Count -= result.Count;
            _capacity = Count;
            _items += result.Count;

            return result;
        }
    }
}
