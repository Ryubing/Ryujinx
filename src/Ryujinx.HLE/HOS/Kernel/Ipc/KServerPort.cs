using Ryujinx.HLE.HOS.Kernel.Common;
using System.Collections.Generic;

namespace Ryujinx.HLE.HOS.Kernel.Ipc
{
    class KServerPort : KSynchronizationObject
    {
        private readonly LinkedList<KServerSession> _incomingConnections;
        private readonly LinkedList<KLightServerSession> _lightIncomingConnections;

        private readonly KPort _parent;

        public bool IsLight => _parent.IsLight;

        public KServerPort(KernelContext context, KPort parent) : base(context)
        {
            _parent = parent;

            _incomingConnections = [];
            _lightIncomingConnections = [];
        }

        public void EnqueueIncomingSession(KServerSession session)
        {
            AcceptIncomingConnection(_incomingConnections, session);
        }

        public void EnqueueIncomingLightSession(KLightServerSession session)
        {
            AcceptIncomingConnection(_lightIncomingConnections, session);
        }

        private void AcceptIncomingConnection<T>(LinkedList<T> list, T session)
        {
            KernelContext.CriticalSection.Enter();

            list.AddLast(session);

            if (list.Count == 1)
            {
                Signal();
            }

            KernelContext.CriticalSection.Leave();
        }

        public KServerSession AcceptIncomingConnection()
        {
            return AcceptIncomingConnection(_incomingConnections);
        }

        public KLightServerSession AcceptIncomingLightConnection()
        {
            return AcceptIncomingConnection(_lightIncomingConnections);
        }

        private T AcceptIncomingConnection<T>(LinkedList<T> list)
        {
            T session = default;

            KernelContext.CriticalSection.Enter();

            if (list.Count != 0)
            {
                session = list.First.Value;

                list.RemoveFirst();
            }

            KernelContext.CriticalSection.Leave();

            return session;
        }

        public override bool IsSignaled()
        {
            if (_parent.IsLight)
            {
                return _lightIncomingConnections.Count != 0;
            }
            else
            {
                return _incomingConnections.Count != 0;
            }
        }
    }
}
