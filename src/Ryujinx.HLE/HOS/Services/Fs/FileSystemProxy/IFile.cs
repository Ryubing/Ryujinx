using LibHac;
using LibHac.Common;
using LibHac.Fs;
using LibHac.Sf;
using Ryujinx.Common;
using Ryujinx.Memory;

namespace Ryujinx.HLE.HOS.Services.Fs.FileSystemProxy
{
    class IFile : DisposableIpcService
    {
        private SharedRef<LibHac.FsSrv.Sf.IFile> _baseFile;

        public IFile(ref SharedRef<LibHac.FsSrv.Sf.IFile> baseFile)
        {
            _baseFile = SharedRef<LibHac.FsSrv.Sf.IFile>.CreateMove(ref baseFile);
        }

        [CommandCmif(0)]
        // Read(u32 readOption, u64 offset, u64 size) -> (u64 out_size, buffer<u8, 0x46, 0> out_buf)
        public ResultCode Read(ServiceCtx context)
        {
            ulong bufferAddress = context.Request.ReceiveBuff[0].Position;
            ulong bufferLen = context.Request.ReceiveBuff[0].Size;

            ReadOption readOption = context.RequestData.ReadStruct<ReadOption>();
            context.RequestData.BaseStream.Position += 4;

            long offset = context.RequestData.ReadInt64();
            long size = context.RequestData.ReadInt64();

            using WritableRegion region = context.Memory.GetWritableRegion(bufferAddress, (int)bufferLen, true);
            Result result = _baseFile.Get.Read(out long bytesRead, offset, new OutBuffer(region.Memory.Span), size, readOption);

            context.ResponseData.Write(bytesRead);

            return (ResultCode)result.Value;
        }

        [CommandCmif(1)]
        // Write(u32 writeOption, u64 offset, u64 size, buffer<u8, 0x45, 0>)
        public ResultCode Write(ServiceCtx context)
        {
            ulong position = context.Request.SendBuff[0].Position;

            WriteOption writeOption = context.RequestData.ReadStruct<WriteOption>();
            context.RequestData.BaseStream.Position += 4;

            long offset = context.RequestData.ReadInt64();
            long size = context.RequestData.ReadInt64();

            byte[] data = new byte[context.Request.SendBuff[0].Size];

            context.Memory.Read(position, data);

            return (ResultCode)_baseFile.Get.Write(offset, new InBuffer(data), size, writeOption).Value;
        }

        [CommandCmif(2)]
        // Flush()
        public ResultCode Flush(ServiceCtx context)
        {
            return (ResultCode)_baseFile.Get.Flush().Value;
        }

        [CommandCmif(3)]
        // SetSize(u64 size)
        public ResultCode SetSize(ServiceCtx context)
        {
            long size = context.RequestData.ReadInt64();

            return (ResultCode)_baseFile.Get.SetSize(size).Value;
        }

        [CommandCmif(4)]
        // GetSize() -> u64 fileSize
        public ResultCode GetSize(ServiceCtx context)
        {
            Result result = _baseFile.Get.GetSize(out long size);

            context.ResponseData.Write(size);

            return (ResultCode)result.Value;
        }

        protected override void Dispose(bool isDisposing)
        {
            if (isDisposing)
            {
                _baseFile.Destroy();
            }
        }
    }
}
