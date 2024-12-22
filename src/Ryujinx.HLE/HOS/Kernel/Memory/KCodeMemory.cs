using Ryujinx.Common;
using Ryujinx.HLE.HOS.Kernel.Common;
using Ryujinx.HLE.HOS.Kernel.Process;
using Ryujinx.Horizon.Common;
using System;
using System.Diagnostics;
using System.Threading;

namespace Ryujinx.HLE.HOS.Kernel.Memory
{
    class KCodeMemory : KAutoObject
    {
        public KProcess Owner { get; private set; }
        private readonly KPageList _pageList;
        private readonly Lock _lock = new();
        private ulong _address;
        private bool _isOwnerMapped;
        private bool _isMapped;

        public KCodeMemory(KernelContext context) : base(context)
        {
            _pageList = new KPageList();
        }

        public Result Initialize(ulong address, ulong size)
        {
            Owner = KernelStatic.GetCurrentProcess();

            Result result = Owner.MemoryManager.BorrowCodeMemory(_pageList, address, size);

            if (result != Result.Success)
            {
                return result;
            }

            Owner.CpuMemory.Fill(address, size, 0xff);
            Owner.IncrementReferenceCount();

            _address = address;
            _isMapped = false;
            _isOwnerMapped = false;

            return Result.Success;
        }

        public Result Map(ulong address, ulong size, KMemoryPermission perm)
        {
            if (_pageList.GetPagesCount() != BitUtils.DivRoundUp<ulong>(size, (ulong)KPageTableBase.PageSize))
            {
                return KernelResult.InvalidSize;
            }

            lock (_lock)
            {
                if (_isMapped)
                {
                    return KernelResult.InvalidState;
                }

                KProcess process = KernelStatic.GetCurrentProcess();

                Result result = process.MemoryManager.MapPages(address, _pageList, MemoryState.CodeWritable, KMemoryPermission.ReadAndWrite);

                if (result != Result.Success)
                {
                    return result;
                }

                _isMapped = true;
            }

            return Result.Success;
        }

        public Result MapToOwner(ulong address, ulong size, KMemoryPermission permission)
        {
            if (_pageList.GetPagesCount() != BitUtils.DivRoundUp<ulong>(size, (ulong)KPageTableBase.PageSize))
            {
                return KernelResult.InvalidSize;
            }

            lock (_lock)
            {
                if (_isOwnerMapped)
                {
                    return KernelResult.InvalidState;
                }

                Debug.Assert(permission == KMemoryPermission.Read || permission == KMemoryPermission.ReadAndExecute);

                Result result = Owner.MemoryManager.MapPages(address, _pageList, MemoryState.CodeReadOnly, permission);

                if (result != Result.Success)
                {
                    return result;
                }

                _isOwnerMapped = true;
            }

            return Result.Success;
        }

        public Result Unmap(ulong address, ulong size)
        {
            if (_pageList.GetPagesCount() != BitUtils.DivRoundUp<ulong>(size, (ulong)KPageTableBase.PageSize))
            {
                return KernelResult.InvalidSize;
            }

            lock (_lock)
            {
                KProcess process = KernelStatic.GetCurrentProcess();

                Result result = process.MemoryManager.UnmapPages(address, _pageList, MemoryState.CodeWritable);

                if (result != Result.Success)
                {
                    return result;
                }

                Debug.Assert(_isMapped);

                _isMapped = false;
            }

            return Result.Success;
        }

        public Result UnmapFromOwner(ulong address, ulong size)
        {
            if (_pageList.GetPagesCount() != BitUtils.DivRoundUp<ulong>(size, KPageTableBase.PageSize))
            {
                return KernelResult.InvalidSize;
            }

            lock (_lock)
            {
                Result result = Owner.MemoryManager.UnmapPages(address, _pageList, MemoryState.CodeReadOnly);

                if (result != Result.Success)
                {
                    return result;
                }

                Debug.Assert(_isOwnerMapped);

                _isOwnerMapped = false;
            }

            return Result.Success;
        }

        protected override void Destroy()
        {
            if (!_isMapped && !_isOwnerMapped)
            {
                ulong size = _pageList.GetPagesCount() * KPageTableBase.PageSize;

                if (Owner.MemoryManager.UnborrowCodeMemory(_address, size, _pageList) != Result.Success)
                {
                    throw new InvalidOperationException("Unexpected failure restoring transfer memory attributes.");
                }
            }

            Owner.DecrementReferenceCount();
        }
    }
}
