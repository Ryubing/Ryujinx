using Ryujinx.Audio.Renderer.Dsp.State;
using Ryujinx.Audio.Renderer.Parameter.Effect;
using Ryujinx.Audio.Renderer.Server.Effect;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Ryujinx.Audio.Renderer.Dsp.Command
{
    public class ReverbCommand : ICommand
    {
        private static readonly int[] _outputEarlyIndicesTableMono = [0, 0, 0, 0, 0, 0, 0, 0, 0, 0];
        private static readonly int[] _targetEarlyDelayLineIndicesTableMono = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9];
        private static readonly int[] _outputIndicesTableMono = [0, 0, 0, 0];
        private static readonly int[] _targetOutputFeedbackIndicesTableMono = [0, 1, 2, 3];

        private static readonly int[] _outputEarlyIndicesTableStereo = [0, 0, 1, 1, 0, 1, 0, 0, 1, 1];
        private static readonly int[] _targetEarlyDelayLineIndicesTableStereo = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9];
        private static readonly int[] _outputIndicesTableStereo = [0, 0, 1, 1];
        private static readonly int[] _targetOutputFeedbackIndicesTableStereo = [2, 0, 3, 1];

        private static readonly int[] _outputEarlyIndicesTableQuadraphonic = [0, 0, 1, 1, 0, 1, 2, 2, 3, 3];
        private static readonly int[] _targetEarlyDelayLineIndicesTableQuadraphonic = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9];
        private static readonly int[] _outputIndicesTableQuadraphonic = [0, 1, 2, 3];
        private static readonly int[] _targetOutputFeedbackIndicesTableQuadraphonic = [0, 1, 2, 3];

        private static readonly int[] _outputEarlyIndicesTableSurround = [0, 5, 0, 5, 1, 5, 1, 5, 4, 5, 4, 5, 2, 5, 2, 5, 3, 5, 3, 5
        ];
        private static readonly int[] _targetEarlyDelayLineIndicesTableSurround = [0, 0, 1, 1, 2, 2, 3, 3, 4, 4, 5, 5, 6, 6, 7, 7, 8, 8, 9, 9
        ];
        private static readonly int[] _outputIndicesTableSurround = [0, 1, 2, 3, 4, 5];
        private static readonly int[] _targetOutputFeedbackIndicesTableSurround = [0, 1, 2, 3, -1, 3];

        public bool Enabled { get; set; }

        public int NodeId { get; }

        public CommandType CommandType => CommandType.Reverb;

        public uint EstimatedProcessingTime { get; set; }

        public ReverbParameter Parameter => _parameter;
        public Memory<ReverbState> State { get; }
        public ulong WorkBuffer { get; }
        public ushort[] OutputBufferIndices { get; }
        public ushort[] InputBufferIndices { get; }
        public bool IsLongSizePreDelaySupported { get; }

        public bool IsEffectEnabled { get; }

        private ReverbParameter _parameter;

        private const int FixedPointPrecision = 14;

        public ReverbCommand(uint bufferOffset, ReverbParameter parameter, Memory<ReverbState> state, bool isEnabled, ulong workBuffer, int nodeId, bool isLongSizePreDelaySupported, bool newEffectChannelMappingSupported)
        {
            Enabled = true;
            IsEffectEnabled = isEnabled;
            NodeId = nodeId;
            _parameter = parameter;
            State = state;
            WorkBuffer = workBuffer;

            InputBufferIndices = new ushort[Constants.VoiceChannelCountMax];
            OutputBufferIndices = new ushort[Constants.VoiceChannelCountMax];

            for (int i = 0; i < Parameter.ChannelCount; i++)
            {
                InputBufferIndices[i] = (ushort)(bufferOffset + Parameter.Input[i]);
                OutputBufferIndices[i] = (ushort)(bufferOffset + Parameter.Output[i]);
            }

            IsLongSizePreDelaySupported = isLongSizePreDelaySupported;

            // NOTE: We do the opposite as Nintendo here for now to restore previous behaviour
            // TODO: Update reverb processing and remove this to use RemapLegacyChannelEffectMappingToChannelResourceMapping.
            DataSourceHelper.RemapChannelResourceMappingToLegacy(newEffectChannelMappingSupported, InputBufferIndices, Parameter.ChannelCount);
            DataSourceHelper.RemapChannelResourceMappingToLegacy(newEffectChannelMappingSupported, OutputBufferIndices, Parameter.ChannelCount);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ProcessReverbMono(ref ReverbState state, ReadOnlySpan<nint> outputBuffers, ReadOnlySpan<nint> inputBuffers, uint sampleCount)
        {
            ProcessReverbGeneric(
                ref state,
                outputBuffers,
                inputBuffers,
                sampleCount,
                _outputEarlyIndicesTableMono,
                _targetEarlyDelayLineIndicesTableMono,
                _targetOutputFeedbackIndicesTableMono,
                _outputIndicesTableMono);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ProcessReverbStereo(ref ReverbState state, ReadOnlySpan<nint> outputBuffers, ReadOnlySpan<nint> inputBuffers, uint sampleCount)
        {
            ProcessReverbGeneric(
                ref state,
                outputBuffers,
                inputBuffers,
                sampleCount,
                _outputEarlyIndicesTableStereo,
                _targetEarlyDelayLineIndicesTableStereo,
                _targetOutputFeedbackIndicesTableStereo,
                _outputIndicesTableStereo);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ProcessReverbQuadraphonic(ref ReverbState state, ReadOnlySpan<nint> outputBuffers, ReadOnlySpan<nint> inputBuffers, uint sampleCount)
        {
            ProcessReverbGeneric(
                ref state,
                outputBuffers,
                inputBuffers,
                sampleCount,
                _outputEarlyIndicesTableQuadraphonic,
                _targetEarlyDelayLineIndicesTableQuadraphonic,
                _targetOutputFeedbackIndicesTableQuadraphonic,
                _outputIndicesTableQuadraphonic);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ProcessReverbSurround(ref ReverbState state, ReadOnlySpan<nint> outputBuffers, ReadOnlySpan<nint> inputBuffers, uint sampleCount)
        {
            ProcessReverbGeneric(
                ref state,
                outputBuffers,
                inputBuffers,
                sampleCount,
                _outputEarlyIndicesTableSurround,
                _targetEarlyDelayLineIndicesTableSurround,
                _targetOutputFeedbackIndicesTableSurround,
                _outputIndicesTableSurround);
        }

        private unsafe void ProcessReverbGeneric(ref ReverbState state, ReadOnlySpan<nint> outputBuffers, ReadOnlySpan<nint> inputBuffers, uint sampleCount, ReadOnlySpan<int> outputEarlyIndicesTable, ReadOnlySpan<int> targetEarlyDelayLineIndicesTable, ReadOnlySpan<int> targetOutputFeedbackIndicesTable, ReadOnlySpan<int> outputIndicesTable)
        {
            bool isSurround = Parameter.ChannelCount == 6;

            float reverbGain = FixedPointHelper.ToFloat(Parameter.ReverbGain, FixedPointPrecision);
            float lateGain = FixedPointHelper.ToFloat(Parameter.LateGain, FixedPointPrecision);
            float outGain = FixedPointHelper.ToFloat(Parameter.OutGain, FixedPointPrecision);
            float dryGain = FixedPointHelper.ToFloat(Parameter.DryGain, FixedPointPrecision);

            Span<float> outputValues = stackalloc float[Constants.ChannelCountMax];
            Span<float> feedbackValues = stackalloc float[4];
            Span<float> feedbackOutputValues = stackalloc float[4];
            Span<float> channelInput = stackalloc float[Parameter.ChannelCount];

            for (int sampleIndex = 0; sampleIndex < sampleCount; sampleIndex++)
            {
                outputValues.Clear();

                for (int i = 0; i < targetEarlyDelayLineIndicesTable.Length; i++)
                {
                    int earlyDelayIndex = targetEarlyDelayLineIndicesTable[i];
                    int outputIndex = outputEarlyIndicesTable[i];

                    float tapOutput = state.PreDelayLine.TapUnsafe(state.EarlyDelayTime[earlyDelayIndex], 0);

                    outputValues[outputIndex] += tapOutput * state.EarlyGain[earlyDelayIndex];
                }

                if (isSurround)
                {
                    outputValues[5] *= 0.2f;
                }

                float targetPreDelayValue = 0;

                for (int channelIndex = 0; channelIndex < Parameter.ChannelCount; channelIndex++)
                {
                    channelInput[channelIndex] = *((float*)inputBuffers[channelIndex] + sampleIndex) * 64;
                    targetPreDelayValue += channelInput[channelIndex] * reverbGain;
                }

                state.PreDelayLine.Update(targetPreDelayValue);

                float lateValue = state.PreDelayLine.Tap(state.PreDelayLineDelayTime) * lateGain;

                for (int i = 0; i < state.FdnDelayLines.Length; i++)
                {
                    feedbackOutputValues[i] = state.FdnDelayLines[i].Read() * state.HighFrequencyDecayDirectGain[i] + state.PreviousFeedbackOutput[i] * state.HighFrequencyDecayPreviousGain[i];
                    state.PreviousFeedbackOutput[i] = feedbackOutputValues[i];
                }

                feedbackValues[0] = feedbackOutputValues[2] + feedbackOutputValues[1];
                feedbackValues[1] = -feedbackOutputValues[0] - feedbackOutputValues[3];
                feedbackValues[2] = feedbackOutputValues[0] - feedbackOutputValues[3];
                feedbackValues[3] = feedbackOutputValues[1] - feedbackOutputValues[2];

                for (int i = 0; i < state.FdnDelayLines.Length; i++)
                {
                    feedbackOutputValues[i] = state.DecayDelays[i].Update(feedbackValues[i] + lateValue);
                    state.FdnDelayLines[i].Update(feedbackOutputValues[i]);
                }

                for (int i = 0; i < targetOutputFeedbackIndicesTable.Length; i++)
                {
                    int targetOutputFeedbackIndex = targetOutputFeedbackIndicesTable[i];
                    int outputIndex = outputIndicesTable[i];

                    if (targetOutputFeedbackIndex >= 0)
                    {
                        outputValues[outputIndex] += feedbackOutputValues[targetOutputFeedbackIndex];
                    }
                }

                if (isSurround)
                {
                    outputValues[4] += state.FrontCenterDelayLine.Update((feedbackOutputValues[2] - feedbackOutputValues[3]) * 0.5f);
                }

                for (int channelIndex = 0; channelIndex < Parameter.ChannelCount; channelIndex++)
                {
                    *((float*)outputBuffers[channelIndex] + sampleIndex) = (outputValues[channelIndex] * outGain + channelInput[channelIndex] * dryGain) / 64;
                }
            }
        }

        private void ProcessReverb(CommandList context, ref ReverbState state)
        {
            Debug.Assert(Parameter.IsChannelCountValid());

            if (IsEffectEnabled && Parameter.IsChannelCountValid())
            {
                Span<nint> inputBuffers = stackalloc nint[Parameter.ChannelCount];
                Span<nint> outputBuffers = stackalloc nint[Parameter.ChannelCount];

                for (int i = 0; i < Parameter.ChannelCount; i++)
                {
                    inputBuffers[i] = context.GetBufferPointer(InputBufferIndices[i]);
                    outputBuffers[i] = context.GetBufferPointer(OutputBufferIndices[i]);
                }

                switch (Parameter.ChannelCount)
                {
                    case 1:
                        ProcessReverbMono(ref state, outputBuffers, inputBuffers, context.SampleCount);
                        break;
                    case 2:
                        ProcessReverbStereo(ref state, outputBuffers, inputBuffers, context.SampleCount);
                        break;
                    case 4:
                        ProcessReverbQuadraphonic(ref state, outputBuffers, inputBuffers, context.SampleCount);
                        break;
                    case 6:
                        ProcessReverbSurround(ref state, outputBuffers, inputBuffers, context.SampleCount);
                        break;
                    default:
                        throw new NotImplementedException(Parameter.ChannelCount.ToString());
                }
            }
            else
            {
                for (int i = 0; i < Parameter.ChannelCount; i++)
                {
                    if (InputBufferIndices[i] != OutputBufferIndices[i])
                    {
                        context.CopyBuffer(OutputBufferIndices[i], InputBufferIndices[i]);
                    }
                }
            }
        }

        public void Process(CommandList context)
        {
            ref ReverbState state = ref State.Span[0];

            if (IsEffectEnabled)
            {
                if (Parameter.Status == UsageState.Invalid)
                {
                    state = new ReverbState(ref _parameter, WorkBuffer, IsLongSizePreDelaySupported);
                }
                else if (Parameter.Status == UsageState.New)
                {
                    state.UpdateParameter(ref _parameter);
                }
            }

            ProcessReverb(context, ref state);
        }
    }
}
