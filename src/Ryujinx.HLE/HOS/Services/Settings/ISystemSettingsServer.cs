using LibHac;
using LibHac.Common;
using LibHac.Fs;
using LibHac.Fs.Fsa;
using LibHac.FsSystem;
using LibHac.Ncm;
using LibHac.Tools.FsSystem.NcaUtils;
using Ryujinx.Common;
using Ryujinx.Common.Logging;
using Ryujinx.HLE.HOS.SystemState;
using System;
using System.IO;
using System.Text;

namespace Ryujinx.HLE.HOS.Services.Settings
{
    [Service("set:sys")]
    class ISystemSettingsServer : IpcService
    {
        public ISystemSettingsServer(ServiceCtx context) { }

        [CommandCmif(3)]
        // GetFirmwareVersion() -> buffer<nn::settings::system::FirmwareVersion, 0x1a, 0x100>
        public ResultCode GetFirmwareVersion(ServiceCtx context)
        {
            return GetFirmwareVersion2(context);
        }

        [CommandCmif(4)]
        // GetFirmwareVersion2() -> buffer<nn::settings::system::FirmwareVersion, 0x1a, 0x100>
        public ResultCode GetFirmwareVersion2(ServiceCtx context)
        {
            ulong replyPos = context.Request.RecvListBuff[0].Position;

            context.Response.PtrBuff[0] = context.Response.PtrBuff[0].WithSize(0x100L);

            byte[] firmwareData = GetFirmwareData(context.Device);

            if (firmwareData != null)
            {
                context.Memory.Write(replyPos, firmwareData);

                return ResultCode.Success;
            }

            const byte MajorFwVersion = 0x03;
            const byte MinorFwVersion = 0x00;
            const byte MicroFwVersion = 0x00;
            const byte Unknown = 0x00; //Build?

            const int RevisionNumber = 0x0A;

            const string Platform = "NX";
            const string UnknownHex = "7fbde2b0bba4d14107bf836e4643043d9f6c8e47";
            const string Version = "3.0.0";
            const string Build = "NintendoSDK Firmware for NX 3.0.0-10.0";

            // http://switchbrew.org/index.php?title=System_Version_Title
            using MemoryStream ms = new(0x100);

            BinaryWriter writer = new(ms);

            writer.Write(MajorFwVersion);
            writer.Write(MinorFwVersion);
            writer.Write(MicroFwVersion);
            writer.Write(Unknown);

            writer.Write(RevisionNumber);

            writer.Write(Encoding.ASCII.GetBytes(Platform));

            ms.Seek(0x28, SeekOrigin.Begin);

            writer.Write(Encoding.ASCII.GetBytes(UnknownHex));

            ms.Seek(0x68, SeekOrigin.Begin);

            writer.Write(Encoding.ASCII.GetBytes(Version));

            ms.Seek(0x80, SeekOrigin.Begin);

            writer.Write(Encoding.ASCII.GetBytes(Build));

            context.Memory.Write(replyPos, ms.ToArray());

            return ResultCode.Success;
        }

        [CommandCmif(23)]
        // GetColorSetId() -> i32
        public ResultCode GetColorSetId(ServiceCtx context)
        {
            context.ResponseData.Write((int)context.Device.System.State.ThemeColor);

            return ResultCode.Success;
        }

        [CommandCmif(24)]
        // GetColorSetId() -> i32
        public ResultCode SetColorSetId(ServiceCtx context)
        {
            int colorSetId = context.RequestData.ReadInt32();

            context.Device.System.State.ThemeColor = (ColorSet)colorSetId;

            return ResultCode.Success;
        }

        [CommandCmif(37)]
        // GetSettingsItemValueSize(buffer<nn::settings::SettingsName, 0x19>, buffer<nn::settings::SettingsItemKey, 0x19>) -> u64
        public ResultCode GetSettingsItemValueSize(ServiceCtx context)
        {
            ulong classPos = context.Request.PtrBuff[0].Position;
            ulong classSize = context.Request.PtrBuff[0].Size;

            ulong namePos = context.Request.PtrBuff[1].Position;
            ulong nameSize = context.Request.PtrBuff[1].Size;

            byte[] classBuffer = new byte[classSize];

            context.Memory.Read(classPos, classBuffer);

            byte[] nameBuffer = new byte[nameSize];

            context.Memory.Read(namePos, nameBuffer);

            string askedSetting = Encoding.ASCII.GetString(classBuffer).Trim('\0') + "!" + Encoding.ASCII.GetString(nameBuffer).Trim('\0');

            NxSettings.Settings.TryGetValue(askedSetting, out object nxSetting);

            if (nxSetting != null)
            {
                ulong settingSize;

                if (nxSetting is string stringValue)
                {
                    settingSize = (ulong)stringValue.Length + 1;
                }
                else if (nxSetting is int)
                {
                    settingSize = sizeof(int);
                }
                else if (nxSetting is bool)
                {
                    settingSize = 1;
                }
                else
                {
                    throw new NotImplementedException(nxSetting.GetType().Name);
                }

                context.ResponseData.Write(settingSize);
            }

            return ResultCode.Success;
        }

        [CommandCmif(38)]
        // GetSettingsItemValue(buffer<nn::settings::SettingsName, 0x19, 0x48>, buffer<nn::settings::SettingsItemKey, 0x19, 0x48>) -> (u64, buffer<unknown, 6, 0>)
        public ResultCode GetSettingsItemValue(ServiceCtx context)
        {
            ulong classPos = context.Request.PtrBuff[0].Position;
            ulong classSize = context.Request.PtrBuff[0].Size;

            ulong namePos = context.Request.PtrBuff[1].Position;
            ulong nameSize = context.Request.PtrBuff[1].Size;

            ulong replyPos = context.Request.ReceiveBuff[0].Position;
            ulong replySize = context.Request.ReceiveBuff[0].Size;

            byte[] classBuffer = new byte[classSize];

            context.Memory.Read(classPos, classBuffer);

            byte[] nameBuffer = new byte[nameSize];

            context.Memory.Read(namePos, nameBuffer);

            string askedSetting = Encoding.ASCII.GetString(classBuffer).Trim('\0') + "!" + Encoding.ASCII.GetString(nameBuffer).Trim('\0');

            NxSettings.Settings.TryGetValue(askedSetting, out object nxSetting);

            if (nxSetting != null)
            {
                byte[] settingBuffer = new byte[replySize];

                if (nxSetting is string stringValue)
                {
                    if ((ulong)(stringValue.Length + 1) > replySize)
                    {
                        Logger.Error?.Print(LogClass.ServiceSet, $"{askedSetting} String value size is too big!");
                    }
                    else
                    {
                        settingBuffer = Encoding.ASCII.GetBytes(stringValue + "\0");
                    }
                }

                if (nxSetting is int intValue)
                {
                    settingBuffer = BitConverter.GetBytes(intValue);
                }
                else if (nxSetting is bool boolValue)
                {
                    settingBuffer[0] = boolValue ? (byte)1 : (byte)0;
                }
                else
                {
                    throw new NotImplementedException(nxSetting.GetType().Name);
                }

                context.Memory.Write(replyPos, settingBuffer);

                Logger.Debug?.Print(LogClass.ServiceSet, $"{askedSetting} set value: {nxSetting} as {nxSetting.GetType()}");
            }
            else
            {
                Logger.Error?.Print(LogClass.ServiceSet, $"{askedSetting} not found!");
            }

            return ResultCode.Success;
        }

        [CommandCmif(60)]
        // IsUserSystemClockAutomaticCorrectionEnabled() -> bool
        public ResultCode IsUserSystemClockAutomaticCorrectionEnabled(ServiceCtx context)
        {
            // NOTE: When set to true, is automatically synced with the internet.
            context.ResponseData.Write(true);

            Logger.Stub?.PrintStub(LogClass.ServiceSet);

            return ResultCode.Success;
        }

        [CommandCmif(62)]
        // GetDebugModeFlag() -> bool
        public ResultCode GetDebugModeFlag(ServiceCtx context)
        {
            context.ResponseData.Write(false);

            Logger.Stub?.PrintStub(LogClass.ServiceSet);

            return ResultCode.Success;
        }

        [CommandCmif(68)]
        // GetSerialNumber() -> buffer<nn::settings::system::SerialNumber, 0x16>
        public ResultCode GetSerialNumber(ServiceCtx context)
        {
            context.ResponseData.Write(Encoding.ASCII.GetBytes("RYU00000000000"));

            return ResultCode.Success;
        }

        [CommandCmif(77)]
        // GetDeviceNickName() -> buffer<nn::settings::system::DeviceNickName, 0x16>
        public ResultCode GetDeviceNickName(ServiceCtx context)
        {
            ulong deviceNickNameBufferPosition = context.Request.ReceiveBuff[0].Position;
            ulong deviceNickNameBufferSize = context.Request.ReceiveBuff[0].Size;

            if (deviceNickNameBufferPosition == 0)
            {
                return ResultCode.NullDeviceNicknameBuffer;
            }

            if (deviceNickNameBufferSize != 0x80)
            {
                Logger.Warning?.Print(LogClass.ServiceSet, "Wrong buffer size");
            }

            context.Memory.Write(deviceNickNameBufferPosition, Encoding.ASCII.GetBytes(context.Device.System.State.DeviceNickName + '\0'));

            return ResultCode.Success;
        }

        [CommandCmif(78)]
        // SetDeviceNickName(buffer<nn::settings::system::DeviceNickName, 0x15>)
        public ResultCode SetDeviceNickName(ServiceCtx context)
        {
            ulong deviceNickNameBufferPosition = context.Request.SendBuff[0].Position;
            ulong deviceNickNameBufferSize = context.Request.SendBuff[0].Size;

            byte[] deviceNickNameBuffer = new byte[deviceNickNameBufferSize];

            context.Memory.Read(deviceNickNameBufferPosition, deviceNickNameBuffer);

            context.Device.System.State.DeviceNickName = Encoding.ASCII.GetString(deviceNickNameBuffer);

            return ResultCode.Success;
        }

        [CommandCmif(90)]
        // GetMiiAuthorId() -> nn::util::Uuid
        public ResultCode GetMiiAuthorId(ServiceCtx context)
        {
            // NOTE: If miiAuthorId is null ResultCode.NullMiiAuthorIdBuffer is returned.
            //       Doesn't occur in our case.

            context.ResponseData.Write(Mii.Helper.GetDeviceId());

            return ResultCode.Success;
        }

        public byte[] GetFirmwareData(Switch device)
        {
            const ulong SystemVersionTitleId = 0x0100000000000809;

            string contentPath = device.System.ContentManager.GetInstalledContentPath(SystemVersionTitleId, StorageId.BuiltInSystem, NcaContentType.Data);

            if (string.IsNullOrWhiteSpace(contentPath))
            {
                return null;
            }

            string firmwareTitlePath = FileSystem.VirtualFileSystem.SwitchPathToSystemPath(contentPath);

            using IStorage firmwareStorage = new LocalStorage(firmwareTitlePath, FileAccess.Read);
            Nca firmwareContent = new(device.System.KeySet, firmwareStorage);

            if (!firmwareContent.CanOpenSection(NcaSectionType.Data))
            {
                return null;
            }

            IFileSystem firmwareRomFs = firmwareContent.OpenFileSystem(NcaSectionType.Data, device.System.FsIntegrityCheckLevel);

            using var firmwareFile = new UniqueRef<IFile>();

            Result result = firmwareRomFs.OpenFile(ref firmwareFile.Ref, "/file".ToU8Span(), OpenMode.Read);
            if (result.IsFailure())
            {
                return null;
            }

            result = firmwareFile.Get.GetSize(out long fileSize);
            if (result.IsFailure())
            {
                return null;
            }

            byte[] data = new byte[fileSize];

            result = firmwareFile.Get.Read(out _, 0, data);
            if (result.IsFailure())
            {
                return null;
            }

            return data;
        }
    }
}
