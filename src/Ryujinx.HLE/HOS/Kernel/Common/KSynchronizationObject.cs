using Ryujinx.HLE.HOS.Kernel.Threading;
using System.Collections.Generic;

namespace Ryujinx.HLE.HOS.Kernel.Common
{
    class KSynchronizationObject : KAutoObject
    {
        public LinkedList<KThread> WaitingThreads { get; }

        public KSynchronizationObject(KernelContext context) : base(context)
        {
            WaitingThreads = [];
        }

        public LinkedListNode<KThread> AddWaitingThread(KThread thread)
        {
            return WaitingThreads.AddLast(thread);
        }

        public void RemoveWaitingThread(LinkedListNode<KThread> node)
        {
            WaitingThreads.Remove(node);
        }

        public virtual void Signal()
        {
            KernelContext.Synchronization.SignalObject(this);
        }

        public virtual bool IsSignaled()
        {
            return false;
        }
    }
}
