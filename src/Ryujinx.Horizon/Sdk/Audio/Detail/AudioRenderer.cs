using Ryujinx.Audio;
using Ryujinx.Audio.Integration;
using Ryujinx.Audio.Renderer.Server;
using Ryujinx.Horizon.Common;
using Ryujinx.Horizon.Sdk.Sf;
using Ryujinx.Horizon.Sdk.Sf.Hipc;
using System;
using System.Buffers;

namespace Ryujinx.Horizon.Sdk.Audio.Detail
{
    partial class AudioRenderer : IAudioRenderer, IDisposable
    {
        private readonly AudioRenderSystem _renderSystem;
        private int _workBufferHandle;
        private int _processHandle;

        public AudioRenderer(AudioRenderSystem renderSystem, int workBufferHandle, int processHandle)
        {
            _renderSystem = renderSystem;
            _workBufferHandle = workBufferHandle;
            _processHandle = processHandle;
        }

        [CmifCommand(0)]
        public Result GetSampleRate(out int sampleRate)
        {
            sampleRate = (int)_renderSystem.GetSampleRate();

            return Result.Success;
        }

        [CmifCommand(1)]
        public Result GetSampleCount(out int sampleCount)
        {
            sampleCount = (int)_renderSystem.GetSampleCount();

            return Result.Success;
        }

        [CmifCommand(2)]
        public Result GetMixBufferCount(out int mixBufferCount)
        {
            mixBufferCount = (int)_renderSystem.GetMixBufferCount();

            return Result.Success;
        }

        [CmifCommand(3)]
        public Result GetState(out int state)
        {
            state = _renderSystem.IsActive() ? 0 : 1;

            return Result.Success;
        }

        [CmifCommand(4)]
        public Result RequestUpdate(
            [Buffer(HipcBufferFlags.Out | HipcBufferFlags.MapAlias)] Memory<byte> output,
            [Buffer(HipcBufferFlags.Out | HipcBufferFlags.MapAlias)] Memory<byte> performanceOutput,
            [Buffer(HipcBufferFlags.In | HipcBufferFlags.MapAlias)] ReadOnlySequence<byte> input)
        {
            using MemoryHandle outputHandle = output.Pin();
            using MemoryHandle performanceOutputHandle = performanceOutput.Pin();

            Result result = new((int)_renderSystem.Update(output, performanceOutput, input));

            return result;
        }

        [CmifCommand(5)]
        public Result Start()
        {
            _renderSystem.Start();

            return Result.Success;
        }

        [CmifCommand(6)]
        public Result Stop()
        {
            _renderSystem.Stop();

            return Result.Success;
        }

        [CmifCommand(7)]
        public Result QuerySystemEvent([CopyHandle] out int eventHandle)
        {
            ResultCode rc = _renderSystem.QuerySystemEvent(out IWritableEvent systemEvent);

            eventHandle = 0;

            if (rc == ResultCode.Success && systemEvent is AudioEvent audioEvent)
            {
                eventHandle = audioEvent.GetReadableHandle();
            }

            return new Result((int)rc);
        }

        [CmifCommand(8)]
        public Result SetRenderingTimeLimit(int percent)
        {
            _renderSystem.SetRenderingTimeLimitPercent((uint)percent);

            return Result.Success;
        }

        [CmifCommand(9)]
        public Result GetRenderingTimeLimit(out int percent)
        {
            percent = (int)_renderSystem.GetRenderingTimeLimit();

            return Result.Success;
        }

        [CmifCommand(10)] // 3.0.0+
        public Result RequestUpdateAuto(
            [Buffer(HipcBufferFlags.Out | HipcBufferFlags.AutoSelect)] Memory<byte> output,
            [Buffer(HipcBufferFlags.Out | HipcBufferFlags.AutoSelect)] Memory<byte> performanceOutput,
            [Buffer(HipcBufferFlags.In | HipcBufferFlags.AutoSelect)] ReadOnlySequence<byte> input)
        {
            return RequestUpdate(output, performanceOutput, input);
        }

        [CmifCommand(11)] // 3.0.0+
        public Result ExecuteAudioRendererRendering()
        {
            return new Result((int)_renderSystem.ExecuteAudioRendererRendering());
        }

        [CmifCommand(12)] // 15.0.0+
        public Result SetVoiceDropParameter(float voiceDropParameter)
        {
            _renderSystem.SetVoiceDropParameter(voiceDropParameter);

            return Result.Success;
        }

        [CmifCommand(13)] // 15.0.0+
        public Result GetVoiceDropParameter(out float voiceDropParameter)
        {
            voiceDropParameter = _renderSystem.GetVoiceDropParameter();

            return Result.Success;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _renderSystem.Dispose();

                if (_workBufferHandle != 0)
                {
                    HorizonStatic.Syscall.CloseHandle(_workBufferHandle);

                    _workBufferHandle = 0;
                }

                if (_processHandle != 0)
                {
                    HorizonStatic.Syscall.CloseHandle(_processHandle);

                    _processHandle = 0;
                }
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
