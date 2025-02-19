using Ryujinx.Audio.Renderer.Dsp.State;
using Ryujinx.Audio.Renderer.Parameter.Effect;
using Ryujinx.Audio.Renderer.Server.Effect;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Ryujinx.Audio.Renderer.Dsp.Command
{
    public class Reverb3dCommand : ICommand
    {
        private static readonly int[] _outputEarlyIndicesTableMono = [0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0
        ];
        private static readonly int[] _targetEarlyDelayLineIndicesTableMono = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19
        ];
        private static readonly int[] _targetOutputFeedbackIndicesTableMono = [0];

        private static readonly int[] _outputEarlyIndicesTableStereo = [0, 0, 0, 1, 1, 1, 1, 0, 0, 0, 1, 1, 1, 0, 0, 0, 0, 1, 1, 1
        ];
        private static readonly int[] _targetEarlyDelayLineIndicesTableStereo = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19
        ];
        private static readonly int[] _targetOutputFeedbackIndicesTableStereo = [0, 1];

        private static readonly int[] _outputEarlyIndicesTableQuadraphonic = [0, 0, 0, 1, 1, 1, 1, 2, 2, 2, 1, 1, 1, 0, 0, 0, 0, 3, 3, 3
        ];
        private static readonly int[] _targetEarlyDelayLineIndicesTableQuadraphonic = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19
        ];
        private static readonly int[] _targetOutputFeedbackIndicesTableQuadraphonic = [0, 1, 2, 3];

        private static readonly int[] _outputEarlyIndicesTableSurround = [4, 5, 0, 5, 0, 5, 1, 5, 1, 5, 1, 5, 1, 5, 2, 5, 2, 5, 2, 5, 1, 5, 1, 5, 1, 5, 0, 5, 0, 5, 0, 5, 0, 5, 3, 5, 3, 5, 3, 5
        ];
        private static readonly int[] _targetEarlyDelayLineIndicesTableSurround = [0, 0, 1, 1, 2, 2, 3, 3, 4, 4, 5, 5, 6, 6, 7, 7, 8, 8, 9, 9, 10, 10, 11, 11, 12, 12, 13, 13, 14, 14, 15, 15, 16, 16, 17, 17, 18, 18, 19, 19
        ];
        private static readonly int[] _targetOutputFeedbackIndicesTableSurround = [0, 1, 2, 3, -1, 3];

        public bool Enabled { get; set; }

        public int NodeId { get; }

        public CommandType CommandType => CommandType.Reverb3d;

        public uint EstimatedProcessingTime { get; set; }

        public ushort InputBufferIndex { get; }
        public ushort OutputBufferIndex { get; }

        public Reverb3dParameter Parameter => _parameter;
        public Memory<Reverb3dState> State { get; }
        public ulong WorkBuffer { get; }
        public ushort[] OutputBufferIndices { get; }
        public ushort[] InputBufferIndices { get; }

        public bool IsEffectEnabled { get; }

        private Reverb3dParameter _parameter;

        public Reverb3dCommand(uint bufferOffset, Reverb3dParameter parameter, Memory<Reverb3dState> state, bool isEnabled, ulong workBuffer, int nodeId, bool newEffectChannelMappingSupported)
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

            // NOTE: We do the opposite as Nintendo here for now to restore previous behaviour
            // TODO: Update reverb 3d processing and remove this to use RemapLegacyChannelEffectMappingToChannelResourceMapping.
            DataSourceHelper.RemapChannelResourceMappingToLegacy(newEffectChannelMappingSupported, InputBufferIndices, Parameter.ChannelCount);
            DataSourceHelper.RemapChannelResourceMappingToLegacy(newEffectChannelMappingSupported, OutputBufferIndices, Parameter.ChannelCount);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ProcessReverb3dMono(ref Reverb3dState state, ReadOnlySpan<nint> outputBuffers, ReadOnlySpan<nint> inputBuffers, uint sampleCount)
        {
            ProcessReverb3dGeneric(ref state, outputBuffers, inputBuffers, sampleCount, _outputEarlyIndicesTableMono, _targetEarlyDelayLineIndicesTableMono, _targetOutputFeedbackIndicesTableMono);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ProcessReverb3dStereo(ref Reverb3dState state, ReadOnlySpan<nint> outputBuffers, ReadOnlySpan<nint> inputBuffers, uint sampleCount)
        {
            ProcessReverb3dGeneric(ref state, outputBuffers, inputBuffers, sampleCount, _outputEarlyIndicesTableStereo, _targetEarlyDelayLineIndicesTableStereo, _targetOutputFeedbackIndicesTableStereo);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ProcessReverb3dQuadraphonic(ref Reverb3dState state, ReadOnlySpan<nint> outputBuffers, ReadOnlySpan<nint> inputBuffers, uint sampleCount)
        {
            ProcessReverb3dGeneric(ref state, outputBuffers, inputBuffers, sampleCount, _outputEarlyIndicesTableQuadraphonic, _targetEarlyDelayLineIndicesTableQuadraphonic, _targetOutputFeedbackIndicesTableQuadraphonic);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ProcessReverb3dSurround(ref Reverb3dState state, ReadOnlySpan<nint> outputBuffers, ReadOnlySpan<nint> inputBuffers, uint sampleCount)
        {
            ProcessReverb3dGeneric(ref state, outputBuffers, inputBuffers, sampleCount, _outputEarlyIndicesTableSurround, _targetEarlyDelayLineIndicesTableSurround, _targetOutputFeedbackIndicesTableSurround);
        }

        private unsafe void ProcessReverb3dGeneric(ref Reverb3dState state, ReadOnlySpan<nint> outputBuffers, ReadOnlySpan<nint> inputBuffers, uint sampleCount, ReadOnlySpan<int> outputEarlyIndicesTable, ReadOnlySpan<int> targetEarlyDelayLineIndicesTable, ReadOnlySpan<int> targetOutputFeedbackIndicesTable)
        {
            const int DelayLineSampleIndexOffset = 1;

            bool isMono = Parameter.ChannelCount == 1;
            bool isSurround = Parameter.ChannelCount == 6;

            Span<float> outputValues = stackalloc float[Constants.ChannelCountMax];
            Span<float> channelInput = stackalloc float[Parameter.ChannelCount];
            Span<float> feedbackValues = stackalloc float[4];
            Span<float> feedbackOutputValues = stackalloc float[4];
            Span<float> values = stackalloc float[4];

            for (int sampleIndex = 0; sampleIndex < sampleCount; sampleIndex++)
            {
                outputValues.Clear();

                float tapOut = state.PreDelayLine.TapUnsafe(state.ReflectionDelayTime, DelayLineSampleIndexOffset);

                for (int i = 0; i < targetEarlyDelayLineIndicesTable.Length; i++)
                {
                    int earlyDelayIndex = targetEarlyDelayLineIndicesTable[i];
                    int outputIndex = outputEarlyIndicesTable[earlyDelayIndex];

                    float tempTapOut = state.PreDelayLine.TapUnsafe(state.EarlyDelayTime[earlyDelayIndex], DelayLineSampleIndexOffset);

                    outputValues[outputIndex] += tempTapOut * state.EarlyGain[earlyDelayIndex];
                }

                float targetPreDelayValue = 0;

                for (int channelIndex = 0; channelIndex < Parameter.ChannelCount; channelIndex++)
                {
                    channelInput[channelIndex] = *((float*)inputBuffers[channelIndex] + sampleIndex);
                    targetPreDelayValue += channelInput[channelIndex];
                }

                for (int i = 0; i < Parameter.ChannelCount; i++)
                {
                    outputValues[i] *= state.EarlyReflectionsGain;
                }

                state.PreviousPreDelayValue = (targetPreDelayValue * state.TargetPreDelayGain) + (state.PreviousPreDelayValue * state.PreviousPreDelayGain);

                state.PreDelayLine.Update(state.PreviousPreDelayValue);

                for (int i = 0; i < state.FdnDelayLines.Length; i++)
                {
                    float fdnValue = state.FdnDelayLines[i].Read();

                    float feedbackOutputValue = fdnValue * state.DecayDirectFdnGain[i] + state.PreviousFeedbackOutputDecayed[i];

                    state.PreviousFeedbackOutputDecayed[i] = (fdnValue * state.DecayCurrentFdnGain[i]) + (feedbackOutputValue * state.DecayCurrentOutputGain[i]);

                    feedbackOutputValues[i] = feedbackOutputValue;
                }

                feedbackValues[0] = feedbackOutputValues[2] + feedbackOutputValues[1];
                feedbackValues[1] = -feedbackOutputValues[0] - feedbackOutputValues[3];
                feedbackValues[2] = feedbackOutputValues[0] - feedbackOutputValues[3];
                feedbackValues[3] = feedbackOutputValues[1] - feedbackOutputValues[2];

                for (int i = 0; i < state.DecayDelays1.Length; i++)
                {
                    float temp = state.DecayDelays1[i].Update(tapOut * state.LateReverbGain + feedbackValues[i]);

                    values[i] = state.DecayDelays2[i].Update(temp);

                    state.FdnDelayLines[i].Update(values[i]);
                }

                for (int channelIndex = 0; channelIndex < targetOutputFeedbackIndicesTable.Length; channelIndex++)
                {
                    int targetOutputFeedbackIndex = targetOutputFeedbackIndicesTable[channelIndex];

                    if (targetOutputFeedbackIndex >= 0)
                    {
                        *((float*)outputBuffers[channelIndex] + sampleIndex) = (outputValues[channelIndex] + values[targetOutputFeedbackIndex] + channelInput[channelIndex] * state.DryGain);
                    }
                }

                if (isMono)
                {
                    *((float*)outputBuffers[0] + sampleIndex) += values[1];
                }

                if (isSurround)
                {
                    *((float*)outputBuffers[4] + sampleIndex) += (outputValues[4] + state.FrontCenterDelayLine.Update((values[2] - values[3]) * 0.5f) + channelInput[4] * state.DryGain);
                }
            }
        }

        public void ProcessReverb3d(CommandList context, ref Reverb3dState state)
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
                        ProcessReverb3dMono(ref state, outputBuffers, inputBuffers, context.SampleCount);
                        break;
                    case 2:
                        ProcessReverb3dStereo(ref state, outputBuffers, inputBuffers, context.SampleCount);
                        break;
                    case 4:
                        ProcessReverb3dQuadraphonic(ref state, outputBuffers, inputBuffers, context.SampleCount);
                        break;
                    case 6:
                        ProcessReverb3dSurround(ref state, outputBuffers, inputBuffers, context.SampleCount);
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
            ref Reverb3dState state = ref State.Span[0];

            if (IsEffectEnabled)
            {
                if (Parameter.ParameterStatus == UsageState.Invalid)
                {
                    state = new Reverb3dState(ref _parameter, WorkBuffer);
                }
                else if (Parameter.ParameterStatus == UsageState.New)
                {
                    state.UpdateParameter(ref _parameter);
                }
            }

            ProcessReverb3d(context, ref state);
        }
    }
}
