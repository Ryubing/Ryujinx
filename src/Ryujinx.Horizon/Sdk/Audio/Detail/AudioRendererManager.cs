using Ryujinx.Audio.Renderer.Device;
using Ryujinx.Audio.Renderer.Server;
using Ryujinx.Common.Logging;
using Ryujinx.Horizon.Common;
using Ryujinx.Horizon.Sdk.Applet;
using Ryujinx.Horizon.Sdk.Sf;
using Ryujinx.Memory;

namespace Ryujinx.Horizon.Sdk.Audio.Detail
{
    partial class AudioRendererManager : IAudioRendererManager
    {
        private const uint InitialRevision = ('R' << 0) | ('E' << 8) | ('V' << 16) | ('1' << 24);

        private readonly Ryujinx.Audio.Renderer.Server.AudioRendererManager _impl;
        private readonly VirtualDeviceSessionRegistry _registry;

        public AudioRendererManager(Ryujinx.Audio.Renderer.Server.AudioRendererManager impl, VirtualDeviceSessionRegistry registry)
        {
            _impl = impl;
            _registry = registry;
        }

        [CmifCommand(0)]
        public Result OpenAudioRenderer(
            out IAudioRenderer renderer,
            AudioRendererParameterInternal parameter,
            [CopyHandle] int workBufferHandle,
            [CopyHandle] int processHandle,
            ulong workBufferSize,
            AppletResourceUserId appletResourceId,
            [ClientProcessId] ulong pid)
        {
            IVirtualMemoryManager clientMemoryManager = HorizonStatic.Syscall.GetMemoryManagerByProcessHandle(processHandle);
            ulong workBufferAddress = HorizonStatic.Syscall.GetTransferMemoryAddress(workBufferHandle);

            Result result = new((int)_impl.OpenAudioRenderer(
                out AudioRenderSystem renderSystem,
                clientMemoryManager,
                ref parameter.Configuration,
                appletResourceId.Id,
                workBufferAddress,
                workBufferSize,
                (uint)processHandle));

            if (result.IsSuccess)
            {
                renderer = new AudioRenderer(renderSystem, workBufferHandle, processHandle);
            }
            else
            {
                renderer = null;

                HorizonStatic.Syscall.CloseHandle(workBufferHandle);
                HorizonStatic.Syscall.CloseHandle(processHandle);
            }

            return result;
        }

        [CmifCommand(1)]
        public Result GetWorkBufferSize(out long workBufferSize, AudioRendererParameterInternal parameter)
        {
            if (BehaviourContext.CheckValidRevision(parameter.Configuration.Revision))
            {
                workBufferSize = (long)Ryujinx.Audio.Renderer.Server.AudioRendererManager.GetWorkBufferSize(ref parameter.Configuration);

                Logger.Debug?.Print(LogClass.ServiceAudio, $"WorkBufferSize is 0x{workBufferSize:x16}.");

                return Result.Success;
            }
            else
            {
                workBufferSize = 0;

                Logger.Warning?.Print(LogClass.ServiceAudio, $"Library Revision REV{BehaviourContext.GetRevisionNumber(parameter.Configuration.Revision)} is not supported!");

                return AudioResult.UnsupportedRevision;
            }
        }

        [CmifCommand(2)]
        public Result GetAudioDeviceService(out IAudioDevice audioDevice, AppletResourceUserId appletResourceId)
        {
            audioDevice = new AudioDevice(_registry, appletResourceId, InitialRevision);

            return Result.Success;
        }

        [CmifCommand(3)] // 3.0.0+
        public Result OpenAudioRendererForManualExecution(
            out IAudioRenderer renderer,
            AudioRendererParameterInternal parameter,
            ulong workBufferAddress,
            [CopyHandle] int processHandle,
            ulong workBufferSize,
            AppletResourceUserId appletResourceId,
            [ClientProcessId] ulong pid)
        {
            IVirtualMemoryManager clientMemoryManager = HorizonStatic.Syscall.GetMemoryManagerByProcessHandle(processHandle);

            Result result = new((int)_impl.OpenAudioRenderer(
                out AudioRenderSystem renderSystem,
                clientMemoryManager,
                ref parameter.Configuration,
                appletResourceId.Id,
                workBufferAddress,
                workBufferSize,
                (uint)processHandle));

            if (result.IsSuccess)
            {
                renderer = new AudioRenderer(renderSystem, 0, processHandle);
            }
            else
            {
                renderer = null;

                HorizonStatic.Syscall.CloseHandle(processHandle);
            }

            return result;
        }

        [CmifCommand(4)] // 4.0.0+
        public Result GetAudioDeviceServiceWithRevisionInfo(out IAudioDevice audioDevice, AppletResourceUserId appletResourceId, uint revision)
        {
            audioDevice = new AudioDevice(_registry, appletResourceId, revision);

            return Result.Success;
        }
    }
}
