using Ryujinx.Common.Memory;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Ryujinx.Graphics.Nvdec.Vp9.Common
{
    internal class MemoryAllocator : IDisposable
    {
        private const int PoolEntries = 10;

        private struct PoolItem
        {
            public nint Pointer;
            public int Length;
            public bool InUse;
        }

        private readonly PoolItem[] _pool = new PoolItem[PoolEntries];

        public ArrayPtr<T> Allocate<T>(int length) where T : unmanaged
        {
            int lengthInBytes = Unsafe.SizeOf<T>() * length;

            nint ptr = nint.Zero;

            for (int i = 0; i < PoolEntries; i++)
            {
                ref PoolItem item = ref _pool[i];

                if (!item.InUse && item.Length == lengthInBytes)
                {
                    item.InUse = true;
                    ptr = item.Pointer;
                    break;
                }
            }

            if (ptr == nint.Zero)
            {
                ptr = Marshal.AllocHGlobal(lengthInBytes);

                for (int i = 0; i < PoolEntries; i++)
                {
                    ref PoolItem item = ref _pool[i];

                    if (!item.InUse)
                    {
                        item.InUse = true;
                        if (item.Pointer != nint.Zero)
                        {
                            Marshal.FreeHGlobal(item.Pointer);
                        }

                        item.Pointer = ptr;
                        item.Length = lengthInBytes;
                        break;
                    }
                }
            }

            ArrayPtr<T> allocation = new(ptr, length);

            allocation.AsSpan().Fill(default);

            return allocation;
        }

        public unsafe void Free<T>(ArrayPtr<T> arr) where T : unmanaged
        {
            nint ptr = (nint)arr.ToPointer();

            for (int i = 0; i < PoolEntries; i++)
            {
                ref PoolItem item = ref _pool[i];

                if (item.Pointer == ptr)
                {
                    item.InUse = false;
                    break;
                }
            }
        }

        public void Dispose()
        {
            for (int i = 0; i < PoolEntries; i++)
            {
                ref PoolItem item = ref _pool[i];

                if (item.Pointer != nint.Zero)
                {
                    Marshal.FreeHGlobal(item.Pointer);
                    item.Pointer = nint.Zero;
                }
            }
        }
    }
}
