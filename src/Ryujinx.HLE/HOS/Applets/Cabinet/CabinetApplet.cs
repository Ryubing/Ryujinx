using Ryujinx.Common.Logging;
using Ryujinx.Common.Memory;
using Ryujinx.HLE.HOS.Services.Am.AppletAE;
using Ryujinx.HLE.HOS.Services.Hid.HidServer;
using Ryujinx.HLE.HOS.Services.Hid;
using Ryujinx.HLE.HOS.Services.Nfc.Nfp;
using Ryujinx.HLE.HOS.Services.Nfc.Nfp.NfpManager;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Ryujinx.HLE.HOS.Applets.Cabinet
{
    internal unsafe class CabinetApplet : IApplet
    {
        private readonly Horizon _system;
        private AppletSession _normalSession;

        public event EventHandler AppletStateChanged;

        public CabinetApplet(Horizon system)
        {
            _system = system;
        }

        public ResultCode Start(AppletSession normalSession, AppletSession interactiveSession)
        {
            _normalSession = normalSession;

            byte[] launchParams = _normalSession.Pop();
            byte[] startParamBytes = _normalSession.Pop();

            StartParamForAmiiboSettings startParam = IApplet.ReadStruct<StartParamForAmiiboSettings>(startParamBytes);

            Logger.Stub?.PrintStub(LogClass.ServiceAm, $"CabinetApplet Start Type: {startParam.Type}");

            switch (startParam.Type)
            {
                case 0:
                    StartNicknameAndOwnerSettings(ref startParam);
                    break;
                case 1:
                case 3:
                    StartFormatter(ref startParam);
                    break;
                default:
                    Logger.Error?.Print(LogClass.ServiceAm, $"Unknown AmiiboSettings type: {startParam.Type}");
                    break;
            }

            // Prepare the response
            ReturnValueForAmiiboSettings returnValue = new()
            {
                AmiiboSettingsReturnFlag = (byte)AmiiboSettingsReturnFlag.HasRegisterInfo,
                DeviceHandle = new DeviceHandle
                {
                    Handle = 0 // Dummy device handle
                },
                RegisterInfo = startParam.RegisterInfo
            };

            // Push the response
            _normalSession.Push(BuildResponse(returnValue));
            AppletStateChanged?.Invoke(this, null);

            _system.ReturnFocus();

            return ResultCode.Success;
        }

        public ResultCode GetResult()
        {
            _system.Device.System.NfpDevices.RemoveAt(0);
            return ResultCode.Success;
        }

        private void StartFormatter(ref StartParamForAmiiboSettings startParam)
        {
            // Initialize RegisterInfo
            startParam.RegisterInfo = new RegisterInfo();
        }

        private void StartNicknameAndOwnerSettings(ref StartParamForAmiiboSettings startParam)
        {
            _system.Device.UIHandler.DisplayCabinetDialog(out string newName);
            byte[] nameBytes = Encoding.UTF8.GetBytes(newName);
            Array41<byte> nickName = new();
            nameBytes.CopyTo(nickName.AsSpan());
            startParam.RegisterInfo.Nickname = nickName;
            NfpDevice devicePlayer1 = new()
            {
                NpadIdType = NpadIdType.Player1,
                Handle = HidUtils.GetIndexFromNpadIdType(NpadIdType.Player1),
                State = NfpDeviceState.SearchingForTag,
            };
            _system.Device.System.NfpDevices.Add(devicePlayer1);
            _system.Device.UIHandler.DisplayCabinetMessageDialog();
            string amiiboId = string.Empty;
            bool scanned = false;
            while (!scanned)
            {
                for (int i = 0; i < _system.Device.System.NfpDevices.Count; i++)
                {
                    if (_system.Device.System.NfpDevices[i].State == NfpDeviceState.TagFound)
                    {
                        amiiboId = _system.Device.System.NfpDevices[i].AmiiboId;
                        scanned = true;
                    }
                }
            }
            VirtualAmiibo.UpdateNickName(amiiboId, newName);
        }

        private static byte[] BuildResponse(ReturnValueForAmiiboSettings returnValue)
        {
            int size = Unsafe.SizeOf<ReturnValueForAmiiboSettings>();
            byte[] bytes = new byte[size];

            fixed (byte* bytesPtr = bytes)
            {
                Unsafe.Write(bytesPtr, returnValue);
            }

            return bytes;
        }

        #region Structs

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public unsafe struct TagInfo
        {
            public fixed byte Data[0x58];
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public unsafe struct StartParamForAmiiboSettings
        {
            public byte ZeroValue; // Left at zero by sdknso
            public byte Type;
            public byte Flags;
            public byte AmiiboSettingsStartParamOffset28;
            public ulong AmiiboSettingsStartParam0;

            public TagInfo TagInfo; // Only enabled when flags bit 1 is set
            public RegisterInfo RegisterInfo; // Only enabled when flags bit 2 is set

            public fixed byte StartParamExtraData[0x20];

            public fixed byte Reserved[0x24];
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public unsafe struct ReturnValueForAmiiboSettings
        {
            public byte AmiiboSettingsReturnFlag;
            private byte Padding1;
            private byte Padding2;
            private byte Padding3;
            public DeviceHandle DeviceHandle;
            public TagInfo TagInfo;
            public RegisterInfo RegisterInfo;
            public fixed byte IgnoredBySdknso[0x24];
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct DeviceHandle
        {
            public ulong Handle;
        }

        public enum AmiiboSettingsReturnFlag : byte
        {
            Cancel = 0,
            HasTagInfo = 2,
            HasRegisterInfo = 4,
            HasTagInfoAndRegisterInfo = 6
        }

        #endregion
    }
}
