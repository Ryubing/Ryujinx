using LibHac;
using LibHac.Common;
using LibHac.Sf;
using Ryujinx.Memory;

namespace Ryujinx.HLE.HOS.Services.Fs.FileSystemProxy
{
    class IDirectory : DisposableIpcService
    {
        private SharedRef<LibHac.FsSrv.Sf.IDirectory> _baseDirectory;

        public IDirectory(ref SharedRef<LibHac.FsSrv.Sf.IDirectory> directory)
        {
            _baseDirectory = SharedRef<LibHac.FsSrv.Sf.IDirectory>.CreateMove(ref directory);
        }

        [CommandCmif(0)]
        // Read() -> (u64 count, buffer<nn::fssrv::sf::IDirectoryEntry, 6, 0> entries)
        public ResultCode Read(ServiceCtx context)
        {
            ulong bufferAddress = context.Request.ReceiveBuff[0].Position;
            ulong bufferLen = context.Request.ReceiveBuff[0].Size;

            using WritableRegion region = context.Memory.GetWritableRegion(bufferAddress, (int)bufferLen, true);
            Result result = _baseDirectory.Get.Read(out long entriesRead, new OutBuffer(region.Memory.Span));

            context.ResponseData.Write(entriesRead);

            return (ResultCode)result.Value;
        }

        [CommandCmif(1)]
        // GetEntryCount() -> u64
        public ResultCode GetEntryCount(ServiceCtx context)
        {
            Result result = _baseDirectory.Get.GetEntryCount(out long entryCount);

            context.ResponseData.Write(entryCount);

            return (ResultCode)result.Value;
        }

        protected override void Dispose(bool isDisposing)
        {
            if (isDisposing)
            {
                _baseDirectory.Destroy();
            }
        }
    }
}
