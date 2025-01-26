using Ryujinx.Audio;
using Ryujinx.Audio.Common;
using Ryujinx.Audio.Output;
using Ryujinx.Horizon.Common;
using Ryujinx.Horizon.Sdk.Applet;
using Ryujinx.Horizon.Sdk.Sf;
using Ryujinx.Horizon.Sdk.Sf.Hipc;
using Ryujinx.Memory;
using System;

namespace Ryujinx.Horizon.Sdk.Audio.Detail
{
    partial class AudioOutManager : IAudioOutManager
    {
        private readonly AudioOutputManager _impl;

        public AudioOutManager(AudioOutputManager impl)
        {
            _impl = impl;
        }

        [CmifCommand(0)]
        public Result ListAudioOuts(out int count, [Buffer(HipcBufferFlags.Out | HipcBufferFlags.MapAlias)] Span<DeviceName> names)
        {
            string[] deviceNames = _impl.ListAudioOuts();

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
        public Result OpenAudioOut(
            out AudioOutputConfiguration outputConfig,
            out IAudioOut audioOut,
            [Buffer(HipcBufferFlags.Out | HipcBufferFlags.MapAlias)] Span<DeviceName> outName,
            AudioInputConfiguration parameter,
            AppletResourceUserId appletResourceId,
            [CopyHandle] int processHandle,
            [Buffer(HipcBufferFlags.In | HipcBufferFlags.MapAlias)] ReadOnlySpan<DeviceName> name,
            [ClientProcessId] ulong pid)
        {
            IVirtualMemoryManager clientMemoryManager = HorizonStatic.Syscall.GetMemoryManagerByProcessHandle(processHandle);

            ResultCode rc = _impl.OpenAudioOut(
                out string outputDeviceName,
                out outputConfig,
                out AudioOutputSystem outSystem,
                clientMemoryManager,
                name.Length > 0 ? name[0].ToString() : string.Empty,
                SampleFormat.PcmInt16,
                ref parameter);

            if (rc == ResultCode.Success && outName.Length > 0)
            {
                outName[0] = new DeviceName(outputDeviceName);
            }

            audioOut = new AudioOut(outSystem, processHandle);

            return new Result((int)rc);
        }

        [CmifCommand(2)] // 3.0.0+
        public Result ListAudioOutsAuto(out int count, [Buffer(HipcBufferFlags.Out | HipcBufferFlags.AutoSelect)] Span<DeviceName> names)
        {
            return ListAudioOuts(out count, names);
        }

        [CmifCommand(3)] // 3.0.0+
        public Result OpenAudioOutAuto(
            out AudioOutputConfiguration outputConfig,
            out IAudioOut audioOut,
            [Buffer(HipcBufferFlags.Out | HipcBufferFlags.AutoSelect)] Span<DeviceName> outName,
            AudioInputConfiguration parameter,
            AppletResourceUserId appletResourceId,
            [CopyHandle] int processHandle,
            [Buffer(HipcBufferFlags.In | HipcBufferFlags.AutoSelect)] ReadOnlySpan<DeviceName> name,
            [ClientProcessId] ulong pid)
        {
            return OpenAudioOut(out outputConfig, out audioOut, outName, parameter, appletResourceId, processHandle, name, pid);
        }
    }
}
