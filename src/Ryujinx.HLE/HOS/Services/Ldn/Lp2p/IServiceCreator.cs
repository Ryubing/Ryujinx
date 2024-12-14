namespace Ryujinx.HLE.HOS.Services.Ldn.Lp2p
{
    [Service("lp2p:app")] // 9.0.0+
    [Service("lp2p:sys")] // 9.0.0+
    class IServiceCreator : IpcService
    {
        public IServiceCreator(ServiceCtx context) { }

        [CommandCmif(0)]
        // CreateNetworkService(pid, u64, u32) -> object<nn::ldn::detail::ISfService>
        public ResultCode CreateNetworkService(ServiceCtx context)
        {
            MakeObject(context, new ISfService(context));

            return ResultCode.Success;
        }

        [CommandCmif(8)]
        // CreateNetworkServiceMonitor(pid, u64) -> object<nn::ldn::detail::ISfServiceMonitor>
        public ResultCode CreateNetworkServiceMonitor(ServiceCtx context)
        {
            MakeObject(context, new ISfServiceMonitor(context));

            return ResultCode.Success;
        }
    }
}
