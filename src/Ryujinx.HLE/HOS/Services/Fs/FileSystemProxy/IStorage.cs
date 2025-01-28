using LibHac;
using LibHac.Common;
using LibHac.Sf;
using Ryujinx.Common;
using Ryujinx.Common.Configuration;
using Ryujinx.Memory;
using System.Threading;

namespace Ryujinx.HLE.HOS.Services.Fs.FileSystemProxy
{
    class IStorage : DisposableIpcService
    {
        private SharedRef<LibHac.FsSrv.Sf.IStorage> _baseStorage;

        public IStorage(ref SharedRef<LibHac.FsSrv.Sf.IStorage> baseStorage)
        {
            _baseStorage = SharedRef<LibHac.FsSrv.Sf.IStorage>.CreateMove(ref baseStorage);
        }
        
        private const string Xc2JpTitleId = "0100f3400332c000";
        private const string Xc2GlobalTitleId = "0100e95004038000";
        private static bool IsXc2 => TitleIDs.CurrentApplication.Value.OrDefault() is Xc2GlobalTitleId or Xc2JpTitleId;

        [CommandCmif(0)]
        // Read(u64 offset, u64 length) -> buffer<u8, 0x46, 0> buffer
        public ResultCode Read(ServiceCtx context)
        {
            ulong offset = context.RequestData.ReadUInt64();
            ulong size = context.RequestData.ReadUInt64();

            if (context.Request.ReceiveBuff.Count > 0)
            {
                ulong bufferAddress = context.Request.ReceiveBuff[0].Position;
                ulong bufferLen = context.Request.ReceiveBuff[0].Size;

                // Use smaller length to avoid overflows.
                if (size > bufferLen)
                {
                    size = bufferLen;
                }

                using WritableRegion region = context.Memory.GetWritableRegion(bufferAddress, (int)bufferLen, true);
                Result result = _baseStorage.Get.Read((long)offset, new OutBuffer(region.Memory.Span), (long)size);
                
                if (context.Device.DirtyHacks.IsEnabled(DirtyHack.Xc2MenuSoftlockFix) && IsXc2)
                {
                    // Add a load-bearing sleep to avoid XC2 softlock
                    // https://web.archive.org/web/20240728045136/https://github.com/Ryujinx/Ryujinx/issues/2357
                    Thread.Sleep(2);
                }

                return (ResultCode)result.Value;
            }

            return ResultCode.Success;
        }

        [CommandCmif(4)]
        // GetSize() -> u64 size
        public ResultCode GetSize(ServiceCtx context)
        {
            Result result = _baseStorage.Get.GetSize(out long size);

            context.ResponseData.Write(size);

            return (ResultCode)result.Value;
        }

        protected override void Dispose(bool isDisposing)
        {
            if (isDisposing)
            {
                _baseStorage.Destroy();
            }
        }
    }
}
