using Ryujinx.Horizon.Common;
using Ryujinx.Horizon.Sdk.OsTypes;
using Ryujinx.Horizon.Sdk.Sf;
using Ryujinx.Horizon.Sdk.Sm;

namespace Ryujinx.Horizon.Sm.Impl
{
    class ServiceManager
    {
        private const int MaxServicesCount = 256;

        private readonly ServiceInfo[] _services;

        public ServiceManager()
        {
            _services = new ServiceInfo[MaxServicesCount];
        }

        public Result GetService(out int handle, ulong processId, ServiceName name)
        {
            handle = 0;
            Result result = ValidateServiceName(name);

            if (result.IsFailure)
            {
                return result;
            }

            // TODO: Validation with GetProcessInfo etc.

            int serviceIndex = GetServiceInfo(name);

            if (serviceIndex < 0)
            {
                return SfResult.RequestDeferredByUser;
            }

            result = GetServiceImpl(out handle, ref _services[serviceIndex]);

            return result == KernelResult.SessionCountExceeded ? SmResult.OutOfSessions : result;
        }

        private static Result GetServiceImpl(out int handle, ref ServiceInfo serviceInfo)
        {
            return HorizonStatic.Syscall.ConnectToPort(out handle, serviceInfo.PortHandle);
        }

        public Result RegisterService(out int handle, ulong processId, ServiceName name, int maxSessions, bool isLight)
        {
            handle = 0;
            Result result = ValidateServiceName(name);

            if (result.IsFailure)
            {
                return result;
            }

            // TODO: Validation with GetProcessInfo etc.
            return HasServiceInfo(name) ? SmResult.AlreadyRegistered : RegisterServiceImpl(out handle, processId, name, maxSessions, isLight);
        }

        public Result RegisterServiceForSelf(out int handle, ServiceName name, int maxSessions)
        {
            return RegisterServiceImpl(out handle, Os.GetCurrentProcessId(), name, maxSessions, false);
        }

        private Result RegisterServiceImpl(out int handle, ulong processId, ServiceName name, int maxSessions, bool isLight)
        {
            handle = 0;

            Result result = ValidateServiceName(name);

            if (!result.IsSuccess)
            {
                return result;
            }

            if (HasServiceInfo(name))
            {
                return SmResult.AlreadyRegistered;
            }

            int freeServiceIndex = GetFreeService();

            if (freeServiceIndex < 0)
            {
                return SmResult.OutOfServices;
            }

            ref ServiceInfo freeService = ref _services[freeServiceIndex];

            result = HorizonStatic.Syscall.CreatePort(out handle, out int clientPort, maxSessions, isLight, null);

            if (!result.IsSuccess)
            {
                return result;
            }

            freeService.PortHandle = clientPort;
            freeService.Name = name;
            freeService.OwnerProcessId = processId;

            return Result.Success;
        }

        public Result UnregisterService(ulong processId, ServiceName name)
        {
            Result result = ValidateServiceName(name);

            if (result.IsFailure)
            {
                return result;
            }

            // TODO: Validation with GetProcessInfo etc.

            int serviceIndex = GetServiceInfo(name);
            if (serviceIndex < 0)
            {
                return SmResult.NotRegistered;
            }

            ref ServiceInfo serviceInfo = ref _services[serviceIndex];
            if (serviceInfo.OwnerProcessId != processId)
            {
                return SmResult.NotAllowed;
            }

            serviceInfo.Free();

            return Result.Success;
        }

        private static Result ValidateServiceName(ServiceName name)
        {
            if (name[0] == 0)
            {
                return SmResult.InvalidServiceName;
            }

            int nameLength = 1;

            for (; nameLength < ServiceName.Length; nameLength++)
            {
                if (name[nameLength] == 0)
                {
                    break;
                }
            }

            while (nameLength < ServiceName.Length)
            {
                if (name[nameLength++] != 0)
                {
                    return SmResult.InvalidServiceName;
                }
            }

            return Result.Success;
        }

        private bool HasServiceInfo(ServiceName name)
        {
            return GetServiceInfo(name) != -1;
        }

        private int GetFreeService()
        {
            return GetServiceInfo(ServiceName.Invalid);
        }

        private int GetServiceInfo(ServiceName name)
        {
            for (int index = 0; index < MaxServicesCount; index++)
            {
                if (_services[index].Name == name)
                {
                    return index;
                }
            }

            return -1;
        }
    }
}
