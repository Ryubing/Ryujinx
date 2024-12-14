using Ryujinx.Common.Logging;

namespace Ryujinx.HLE.HOS.Services.Ldn.Lp2p
{
    class ISfService : IpcService
    {
        public ISfService(ServiceCtx context) { }

        [CommandCmif(0)]
        // Initialize()
        public ResultCode Initialize(ServiceCtx context)
        {
            context.ResponseData.Write(0);

            return ResultCode.Success;
        }

        [CommandCmif(768)]
        // CreateGroup(buffer<nn::lp2p::GroupInfo, 0x31)
        public ResultCode CreateGroup(ServiceCtx context)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceLdn);

            return ResultCode.Success;
        }

        [CommandCmif(776)]
        // DestroyGroup()
        public ResultCode DestroyGroup(ServiceCtx context)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceLdn);

            return ResultCode.Success;
        }

        [CommandCmif(1536)]
        // SendToOtherGroup(nn::lp2p::MacAddress, nn::lp2p::GroupId, s16, s16, u32, buffer<unknown, 0x21>)
        public ResultCode SendToOtherGroup(ServiceCtx context)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceLdn);

            return ResultCode.Success;
        }

        [CommandCmif(1544)]
        // RecvFromOtherGroup(u32, buffer<unknown, 0x22>) -> (nn::lp2p::MacAddress, u16, s16, u32, s32)
        public ResultCode RecvFromOtherGroup(ServiceCtx context)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceLdn);

            return ResultCode.Success;
        }
    }
}
