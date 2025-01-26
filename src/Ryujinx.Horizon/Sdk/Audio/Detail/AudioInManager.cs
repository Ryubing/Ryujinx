using Ryujinx.Audio;
using Ryujinx.Audio.Common;
using Ryujinx.Audio.Input;
using Ryujinx.Horizon.Common;
using Ryujinx.Horizon.Sdk.Applet;
using Ryujinx.Horizon.Sdk.Sf;
using Ryujinx.Horizon.Sdk.Sf.Hipc;
using Ryujinx.Memory;
using System;

namespace Ryujinx.Horizon.Sdk.Audio.Detail
{
    partial class AudioInManager : IAudioInManager
    {
        private readonly AudioInputManager _impl;

        public AudioInManager(AudioInputManager impl)
        {
            _impl = impl;
        }

        [CmifCommand(0)]
        public Result ListAudioIns(out int count, [Buffer(HipcBufferFlags.Out | HipcBufferFlags.MapAlias)] Span<DeviceName> names)
        {
            string[] deviceNames = _impl.ListAudioIns(filtered: false);

            count = 0;

            foreach (string deviceName in deviceNames)
            {
                if (count >= names.Length)
                {
                    break;
                }

                names[count++] = new DeviceName(deviceName);
            }

            return Result.Success;
        }

        [CmifCommand(1)]
        public Result OpenAudioIn(
            out AudioOutputConfiguration outputConfiguration,
            out IAudioIn audioIn,
            [Buffer(HipcBufferFlags.Out | HipcBufferFlags.MapAlias)] Span<DeviceName> outName,
            AudioInputConfiguration parameter,
            AppletResourceUserId appletResourceId,
            [CopyHandle] int processHandle,
            [Buffer(HipcBufferFlags.In | HipcBufferFlags.MapAlias)] ReadOnlySpan<DeviceName> name,
            [ClientProcessId] ulong pid)
        {
            IVirtualMemoryManager clientMemoryManager = HorizonStatic.Syscall.GetMemoryManagerByProcessHandle(processHandle);

            ResultCode rc = _impl.OpenAudioIn(
                out string outputDeviceName,
                out outputConfiguration,
                out AudioInputSystem inSystem,
                clientMemoryManager,
                name.Length > 0 ? name[0].ToString() : string.Empty,
                SampleFormat.PcmInt16,
                ref parameter);

            if (rc == ResultCode.Success && outName.Length > 0)
            {
                outName[0] = new DeviceName(outputDeviceName);
            }

            audioIn = new AudioIn(inSystem, processHandle);

            return new Result((int)rc);
        }

        [CmifCommand(2)] // 3.0.0+
        public Result ListAudioInsAuto(out int count, [Buffer(HipcBufferFlags.Out | HipcBufferFlags.AutoSelect)] Span<DeviceName> names)
        {
            return ListAudioIns(out count, names);
        }

        [CmifCommand(3)] // 3.0.0+
        public Result OpenAudioInAuto(
            out AudioOutputConfiguration outputConfig,
            out IAudioIn audioIn,
            [Buffer(HipcBufferFlags.Out | HipcBufferFlags.AutoSelect)] Span<DeviceName> outName,
            AudioInputConfiguration parameter,
            AppletResourceUserId appletResourceId,
            [CopyHandle] int processHandle,
            [Buffer(HipcBufferFlags.In | HipcBufferFlags.AutoSelect)] ReadOnlySpan<DeviceName> name,
            [ClientProcessId] ulong pid)
        {
            return OpenAudioIn(out outputConfig, out audioIn, outName, parameter, appletResourceId, processHandle, name, pid);
        }

        [CmifCommand(4)] // 3.0.0+
        public Result ListAudioInsAutoFiltered(out int count, [Buffer(HipcBufferFlags.Out | HipcBufferFlags.AutoSelect)] Span<DeviceName> names)
        {
            string[] deviceNames = _impl.ListAudioIns(filtered: true);

            count = 0;

            foreach (string deviceName in deviceNames)
            {
                if (count >= names.Length)
                {
                    break;
                }

                names[count++] = new DeviceName(deviceName);
            }

            return Result.Success;
        }

        [CmifCommand(5)] // 5.0.0+
        public Result OpenAudioInProtocolSpecified(
            out AudioOutputConfiguration outputConfig,
            out IAudioIn audioIn,
            [Buffer(HipcBufferFlags.Out | HipcBufferFlags.MapAlias)] Span<DeviceName> outName,
            AudioInProtocol protocol,
            AudioInputConfiguration parameter,
            AppletResourceUserId appletResourceId,
            [CopyHandle] int processHandle,
            [Buffer(HipcBufferFlags.In | HipcBufferFlags.MapAlias)] ReadOnlySpan<DeviceName> name,
            [ClientProcessId] ulong pid)
        {
            // NOTE: We always assume that only the default device will be plugged (we never report any USB Audio Class type devices).

            return OpenAudioIn(out outputConfig, out audioIn, outName, parameter, appletResourceId, processHandle, name, pid);
        }
    }
}
