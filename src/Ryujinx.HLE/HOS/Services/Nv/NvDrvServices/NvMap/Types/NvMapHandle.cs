using System.Threading;

namespace Ryujinx.HLE.HOS.Services.Nv.NvDrvServices.NvMap
{
    class NvMapHandle
    {
#pragma warning disable CS0649 // Field is never assigned to
        public int Handle;
        public int Id;
#pragma warning restore CS0649
        public uint Size;
        public int Align;
        public int Kind;
        public ulong Address;
        public bool Allocated;
        public ulong DmaMapAddress;
        public ulong OwnerPid;
        
        private long _dupes;

        public NvMapHandle()
        {
            _dupes = 1;
        }

        public NvMapHandle(uint size) : this()
        {
            Size = size;
        }

        public void IncrementRefCount()
        {
            Interlocked.Increment(ref _dupes);
        }

        public long DecrementRefCount()
        {
            return Interlocked.Decrement(ref _dupes);
        }
    }
}
