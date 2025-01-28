using Ryujinx.Common.Logging;
using Ryujinx.HLE.HOS.Ipc;
using Ryujinx.HLE.HOS.Kernel.Threading;
using Ryujinx.Horizon.Common;
using System;
using System.Collections.Generic;

namespace Ryujinx.HLE.HOS.Services.Ns.Aoc
{
    [Service("aoc:u")]
    class IAddOnContentManager : IpcService
    {
        private readonly KEvent _addOnContentListChangedEvent;
        private int _addOnContentListChangedEventHandle;

        private ulong _addOnContentBaseId;

        private readonly List<ulong> _mountedAocTitleIds = [];

        public IAddOnContentManager(ServiceCtx context)
        {
            _addOnContentListChangedEvent = new KEvent(context.Device.System.KernelContext);
        }

        [CommandCmif(0)] // 1.0.0-6.2.0
        // CountAddOnContentByApplicationId(u64 title_id) -> u32
        public ResultCode CountAddOnContentByApplicationId(ServiceCtx context)
        {
            ulong titleId = context.RequestData.ReadUInt64();

            return CountAddOnContentImpl(context, titleId);
        }

        [CommandCmif(1)] // 1.0.0-6.2.0
        // ListAddOnContentByApplicationId(u64 title_id, u32 start_index, u32 buffer_size) -> (u32 count, buffer<u32>)
        public ResultCode ListAddOnContentByApplicationId(ServiceCtx context)
        {
            ulong titleId = context.RequestData.ReadUInt64();

            return ListAddContentImpl(context, titleId);
        }

        [CommandCmif(2)]
        // CountAddOnContent(pid) -> u32
        public ResultCode CountAddOnContent(ServiceCtx context)
        {
#pragma warning disable IDE0059 // Remove unnecessary value assignment
            ulong pid = context.Request.HandleDesc.PId;
#pragma warning restore IDE0059

            // NOTE: Service call arp:r GetApplicationLaunchProperty to get TitleId using the PId.

            return CountAddOnContentImpl(context, context.Device.Processes.ActiveApplication.ProgramId);
        }

        [CommandCmif(3)]
        // ListAddOnContent(u32 start_index, u32 buffer_size, pid) -> (u32 count, buffer<u32>)
        public ResultCode ListAddOnContent(ServiceCtx context)
        {
#pragma warning disable IDE0059 // Remove unnecessary value assignment
            ulong pid = context.Request.HandleDesc.PId;
#pragma warning restore IDE0059

            // NOTE: Service call arp:r GetApplicationLaunchProperty to get TitleId using the PId.

            return ListAddContentImpl(context, context.Device.Processes.ActiveApplication.ProgramId);
        }

        [CommandCmif(4)] // 1.0.0-6.2.0
        // GetAddOnContentBaseIdByApplicationId(u64 title_id) -> u64
        public ResultCode GetAddOnContentBaseIdByApplicationId(ServiceCtx context)
        {
            ulong titleId = context.RequestData.ReadUInt64();

            return GetAddOnContentBaseIdImpl(context, titleId);
        }

        [CommandCmif(5)]
        // GetAddOnContentBaseId(pid) -> u64
        public ResultCode GetAddOnContentBaseId(ServiceCtx context)
        {
#pragma warning disable IDE0059 // Remove unnecessary value assignment
            ulong pid = context.Request.HandleDesc.PId;
#pragma warning restore IDE0059

            // NOTE: Service call arp:r GetApplicationLaunchProperty to get TitleId using the PId.

            return GetAddOnContentBaseIdImpl(context, context.Device.Processes.ActiveApplication.ProgramId);
        }

        [CommandCmif(6)] // 1.0.0-6.2.0
        // PrepareAddOnContentByApplicationId(u64 title_id, u32 index)
        public ResultCode PrepareAddOnContentByApplicationId(ServiceCtx context)
        {
            ulong titleId = context.RequestData.ReadUInt64();

            return PrepareAddOnContentImpl(context, titleId);
        }

        [CommandCmif(7)]
        // PrepareAddOnContent(u32 index, pid)
        public ResultCode PrepareAddOnContent(ServiceCtx context)
        {
#pragma warning disable IDE0059 // Remove unnecessary value assignment
            ulong pid = context.Request.HandleDesc.PId;
#pragma warning restore IDE0059

            // NOTE: Service call arp:r GetApplicationLaunchProperty to get TitleId using the PId.

            return PrepareAddOnContentImpl(context, context.Device.Processes.ActiveApplication.ProgramId);
        }

        [CommandCmif(8)] // 4.0.0+
        // GetAddOnContentListChangedEvent() -> handle<copy>
        public ResultCode GetAddOnContentListChangedEvent(ServiceCtx context)
        {
            return GetAddOnContentListChangedEventImpl(context);
        }

        [CommandCmif(9)] // 10.0.0+
        // GetAddOnContentLostErrorCode() -> u64
        public ResultCode GetAddOnContentLostErrorCode(ServiceCtx context)
        {
            // NOTE: 0x7D0A4 -> 2164-1000
            context.ResponseData.Write(GetAddOnContentLostErrorCodeImpl(0x7D0A4));

            return ResultCode.Success;
        }

        [CommandCmif(10)] // 11.0.0+
        // GetAddOnContentListChangedEventWithProcessId(pid) -> handle<copy>
        public ResultCode GetAddOnContentListChangedEventWithProcessId(ServiceCtx context)
        {
#pragma warning disable IDE0059 // Remove unnecessary value assignment
            ulong pid = context.Request.HandleDesc.PId;
#pragma warning restore IDE0059

            // NOTE: Service call arp:r GetApplicationLaunchProperty to get TitleId using the PId.

            // TODO: Found where stored value is used.
            ResultCode resultCode = GetAddOnContentBaseIdFromTitleId(context, context.Device.Processes.ActiveApplication.ProgramId);

            if (resultCode != ResultCode.Success)
            {
                return resultCode;
            }

            return GetAddOnContentListChangedEventImpl(context);
        }

        [CommandCmif(11)] // 13.0.0+
        // NotifyMountAddOnContent(pid, u64 title_id)
        public ResultCode NotifyMountAddOnContent(ServiceCtx context)
        {
#pragma warning disable IDE0059 // Remove unnecessary value assignment
            ulong pid = context.Request.HandleDesc.PId;
#pragma warning restore IDE0059

            // NOTE: Service call arp:r GetApplicationLaunchProperty to get TitleId using the PId.

            ulong aocTitleId = context.RequestData.ReadUInt64();

            if (_mountedAocTitleIds.Count <= 0x7F)
            {
                _mountedAocTitleIds.Add(aocTitleId);
            }

            return ResultCode.Success;
        }

        [CommandCmif(12)] // 13.0.0+
        // NotifyUnmountAddOnContent(pid, u64 title_id)
        public ResultCode NotifyUnmountAddOnContent(ServiceCtx context)
        {
#pragma warning disable IDE0059 // Remove unnecessary value assignment
            ulong pid = context.Request.HandleDesc.PId;
#pragma warning restore IDE0059

            // NOTE: Service call arp:r GetApplicationLaunchProperty to get TitleId using the PId.

            ulong aocTitleId = context.RequestData.ReadUInt64();

            _mountedAocTitleIds.Remove(aocTitleId);

            return ResultCode.Success;
        }

        [CommandCmif(50)] // 13.0.0+
        // CheckAddOnContentMountStatus(pid)
        public ResultCode CheckAddOnContentMountStatus(ServiceCtx context)
        {
#pragma warning disable IDE0059 // Remove unnecessary value assignment
            ulong pid = context.Request.HandleDesc.PId;
#pragma warning restore IDE0059

            // NOTE: Service call arp:r GetApplicationLaunchProperty to get TitleId using the PId.
            //       Then it does some internal checks and returns InvalidBufferSize if they fail.

            Logger.Stub?.PrintStub(LogClass.ServiceNs);

            return ResultCode.Success;
        }

        [CommandCmif(100)] // 7.0.0+
        // CreateEcPurchasedEventManager() -> object<nn::ec::IPurchaseEventManager>
        public ResultCode CreateEcPurchasedEventManager(ServiceCtx context)
        {
            MakeObject(context, new IPurchaseEventManager(context.Device.System));

            return ResultCode.Success;
        }

        [CommandCmif(101)] // 9.0.0+
        // CreatePermanentEcPurchasedEventManager() -> object<nn::ec::IPurchaseEventManager>
        public ResultCode CreatePermanentEcPurchasedEventManager(ServiceCtx context)
        {
            // NOTE: Service call arp:r to get the TitleId, do some extra checks and pass it to returned interface.

            MakeObject(context, new IPurchaseEventManager(context.Device.System));

            return ResultCode.Success;
        }

        [CommandCmif(110)] // 12.0.0+
        // CreateContentsServiceManager() -> object<nn::ec::IContentsServiceManager>
        public ResultCode CreateContentsServiceManager(ServiceCtx context)
        {
            MakeObject(context, new IContentsServiceManager());

            return ResultCode.Success;
        }

        private ResultCode CountAddOnContentImpl(ServiceCtx context, ulong titleId)
        {
            // NOTE: Service call sys:set GetQuestFlag and store it internally.
            //       If QuestFlag is true, counts some extra titles.

            ResultCode resultCode = GetAddOnContentBaseIdFromTitleId(context, titleId);

            if (resultCode != ResultCode.Success)
            {
                return resultCode;
            }

            // TODO: This should use _addOnContentBaseId;
            uint aocCount = (uint)context.Device.System.ContentManager.GetAocCount();

            context.ResponseData.Write(aocCount);

            return ResultCode.Success;
        }

        private ResultCode ListAddContentImpl(ServiceCtx context, ulong titleId)
        {
            // NOTE: Service call sys:set GetQuestFlag and store it internally.
            //       If QuestFlag is true, counts some extra titles.

            uint startIndex = context.RequestData.ReadUInt32();
            uint indexNumber = context.RequestData.ReadUInt32();
            ulong bufferPosition = context.Request.ReceiveBuff[0].Position;
            ulong bufferSize = context.Request.ReceiveBuff[0].Size;

            // TODO: This should use _addOnContentBaseId;
            uint aocTotalCount = (uint)context.Device.System.ContentManager.GetAocCount();

            if (indexNumber > bufferSize / sizeof(uint))
            {
                return ResultCode.InvalidBufferSize;
            }

            if (aocTotalCount <= startIndex)
            {
                context.ResponseData.Write(0);

                return ResultCode.Success;
            }

            IList<ulong> aocTitleIds = context.Device.System.ContentManager.GetAocTitleIds();

            GetAddOnContentBaseIdFromTitleId(context, titleId);

            uint indexCounter = 0;

            for (int i = 0; i < indexNumber; i++)
            {
                if (i + (int)startIndex < aocTitleIds.Count)
                {
                    context.Memory.Write(bufferPosition + (ulong)i * sizeof(uint), (uint)(aocTitleIds[i + (int)startIndex] - _addOnContentBaseId));

                    indexCounter++;
                }
            }

            context.ResponseData.Write(indexCounter);

            return ResultCode.Success;
        }

        private ResultCode GetAddOnContentBaseIdImpl(ServiceCtx context, ulong titleId)
        {
            ResultCode resultCode = GetAddOnContentBaseIdFromTitleId(context, titleId);

            context.ResponseData.Write(_addOnContentBaseId);

            return resultCode;
        }

        private ResultCode GetAddOnContentBaseIdFromTitleId(ServiceCtx context, ulong titleId)
        {
            // NOTE: Service calls arp:r GetApplicationControlProperty to get AddOnContentBaseId using TitleId,
            //       If the call fails, it returns ResultCode.InvalidPid.

            _addOnContentBaseId = context.Device.Processes.ActiveApplication.ApplicationControlProperties.AddOnContentBaseId;

            if (_addOnContentBaseId == 0)
            {
                _addOnContentBaseId = titleId + 0x1000;
            }

            return ResultCode.Success;
        }

        private ResultCode PrepareAddOnContentImpl(ServiceCtx context, ulong titleId)
        {
            uint index = context.RequestData.ReadUInt32();

            ResultCode resultCode = GetAddOnContentBaseIdFromTitleId(context, context.Device.Processes.ActiveApplication.ProgramId);

            if (resultCode != ResultCode.Success)
            {
                return resultCode;
            }

            // TODO: Service calls ns:am RegisterContentsExternalKey?, GetOwnedApplicationContentMetaStatus? etc...
            //       Ideally, this should probably initialize the AocData values for the specified index

            Logger.Stub?.PrintStub(LogClass.ServiceNs, new { index });

            return ResultCode.Success;
        }

        private ResultCode GetAddOnContentListChangedEventImpl(ServiceCtx context)
        {
            if (_addOnContentListChangedEventHandle == 0)
            {
                if (context.Process.HandleTable.GenerateHandle(_addOnContentListChangedEvent.ReadableEvent, out _addOnContentListChangedEventHandle) != Result.Success)
                {
                    throw new InvalidOperationException("Out of handles!");
                }
            }

            context.Response.HandleDesc = IpcHandleDesc.MakeCopy(_addOnContentListChangedEventHandle);

            return ResultCode.Success;
        }

        private static ulong GetAddOnContentLostErrorCodeImpl(int errorCode)
        {
            return ((ulong)errorCode & 0x1FF | ((((ulong)errorCode >> 9) & 0x1FFF) << 32)) + 2000;
        }
    }
}
