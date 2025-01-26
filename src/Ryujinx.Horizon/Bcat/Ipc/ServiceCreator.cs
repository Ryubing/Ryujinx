using LibHac.Common;
using Ryujinx.Horizon.Bcat.Types;
using Ryujinx.Horizon.Common;
using Ryujinx.Horizon.Sdk.Bcat;
using Ryujinx.Horizon.Sdk.Sf;
using System;
using System.Threading;
using ApplicationId = Ryujinx.Horizon.Sdk.Ncm.ApplicationId;

namespace Ryujinx.Horizon.Bcat.Ipc
{
    partial class ServiceCreator : IServiceCreator, IDisposable
    {
        private readonly BcatServicePermissionLevel _permissionLevel;
        private SharedRef<LibHac.Bcat.Impl.Ipc.IServiceCreator> _libHacService;

        private int _disposalState;

        public ServiceCreator(string serviceName, BcatServicePermissionLevel permissionLevel)
        {
            HorizonStatic.Options.BcatClient.Sm.GetService(ref _libHacService, serviceName).ThrowIfFailure();
            _permissionLevel = permissionLevel;
        }

        [CmifCommand(0)]
        public Result CreateBcatService(out IBcatService bcatService, [ClientProcessId] ulong pid)
        {
            // TODO: Call arp:r GetApplicationLaunchProperty with the pid to get the TitleId.
            //       Add an instance of nn::bcat::detail::service::core::PassphraseManager.
            //       Add an instance of nn::bcat::detail::service::ServiceMemoryManager.
            //       Add an instance of nn::bcat::detail::service::core::TaskManager who loads "bcat-sys:/" system save data and opens "dc/task.bin". 
            //       If the file don't exist, create a new one (with a size of 0x800 bytes) and write 2 empty structs with a size of 0x400 bytes.

            bcatService = new BcatService(_permissionLevel);

            return Result.Success;
        }

        [CmifCommand(1)]
        public Result CreateDeliveryCacheStorageService(out IDeliveryCacheStorageService service, [ClientProcessId] ulong pid)
        {
            using SharedRef<LibHac.Bcat.Impl.Ipc.IDeliveryCacheStorageService> libHacService = new();

            LibHac.Result resultCode = _libHacService.Get.CreateDeliveryCacheStorageService(ref libHacService.Ref, pid);

            if (resultCode.IsSuccess())
            {
                service = new DeliveryCacheStorageService(ref libHacService.Ref);
            }
            else
            {
                service = null;
            }

            return resultCode.ToHorizonResult();
        }

        [CmifCommand(2)]
        public Result CreateDeliveryCacheStorageServiceWithApplicationId(out IDeliveryCacheStorageService service, ApplicationId applicationId)
        {
            using SharedRef<LibHac.Bcat.Impl.Ipc.IDeliveryCacheStorageService> libHacService = new();

            LibHac.Result resultCode = _libHacService.Get.CreateDeliveryCacheStorageServiceWithApplicationId(ref libHacService.Ref, new LibHac.ApplicationId(applicationId.Id));

            if (resultCode.IsSuccess())
            {
                service = new DeliveryCacheStorageService(ref libHacService.Ref);
            }
            else
            {
                service = null;
            }

            return resultCode.ToHorizonResult();
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposalState, 1) == 0)
            {
                _libHacService.Destroy();
            }
        }
    }
}
