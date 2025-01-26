using Ryujinx.Horizon.Common;
using Ryujinx.Horizon.Sdk.Sf.Cmif;
using System;

namespace Ryujinx.Horizon.Sdk.Sf.Hipc
{
    partial class HipcManager : IServiceObject
    {
        private readonly ServerDomainSessionManager _manager;
        private readonly ServerSession _session;

        public HipcManager(ServerDomainSessionManager manager, ServerSession session)
        {
            _manager = manager;
            _session = session;
        }

        [CmifCommand(0)]
        public Result ConvertCurrentObjectToDomain(out int objectId)
        {
            objectId = 0;

            DomainServiceObject domain = _manager.Domain.AllocateDomainServiceObject();
            if (domain == null)
            {
                return HipcResult.OutOfDomains;
            }

            bool succeeded = false;

            try
            {
                Span<int> objectIds = stackalloc int[1];

                Result result = domain.ReserveIds(objectIds);

                if (result.IsFailure)
                {
                    return result;
                }

                objectId = objectIds[0];
                succeeded = true;
            }
            finally
            {
                if (!succeeded)
                {
                    ServerDomainManager.DestroyDomainServiceObject(domain);
                }
            }

            domain.RegisterObject(objectId, _session.ServiceObjectHolder);
            _session.ServiceObjectHolder = new ServiceObjectHolder(domain);

            return Result.Success;
        }

        [CmifCommand(1)]
        public Result CopyFromCurrentDomain([MoveHandle] out int clientHandle, int objectId)
        {
            clientHandle = 0;

            if (_session.ServiceObjectHolder.ServiceObject is not DomainServiceObject domain)
            {
                return HipcResult.TargetNotDomain;
            }

            ServiceObjectHolder obj = domain.GetObject(objectId);
            if (obj == null)
            {
                return HipcResult.DomainObjectNotFound;
            }

            Api.CreateSession(out int serverHandle, out clientHandle).AbortOnFailure();
            _manager.RegisterSession(serverHandle, obj).AbortOnFailure();

            return Result.Success;
        }

        [CmifCommand(2)]
        public Result CloneCurrentObject([MoveHandle] out int clientHandle)
        {
            return CloneCurrentObjectImpl(out clientHandle, _manager);
        }

        [CmifCommand(3)]
        public void QueryPointerBufferSize(out ushort size)
        {
            size = (ushort)_session.PointerBuffer.Size;
        }

        [CmifCommand(4)]
        public Result CloneCurrentObjectEx([MoveHandle] out int clientHandle, uint tag)
        {
            return CloneCurrentObjectImpl(out clientHandle, _manager.GetSessionManagerByTag(tag));
        }

        private Result CloneCurrentObjectImpl(out int clientHandle, ServerSessionManager manager)
        {
            clientHandle = 0;

            ServiceObjectHolder clone = _session.ServiceObjectHolder.Clone();
            if (clone == null)
            {
                return HipcResult.DomainObjectNotFound;
            }

            Api.CreateSession(out int serverHandle, out clientHandle).AbortOnFailure();
            manager.RegisterSession(serverHandle, clone).AbortOnFailure();

            return Result.Success;
        }
    }
}
