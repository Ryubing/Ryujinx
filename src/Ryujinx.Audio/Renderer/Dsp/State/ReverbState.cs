using Ryujinx.Audio.Renderer.Common;
using Ryujinx.Audio.Renderer.Dsp.Effect;
using Ryujinx.Audio.Renderer.Parameter.Effect;
using System;

namespace Ryujinx.Audio.Renderer.Dsp.State
{
    public struct ReverbState
    {
        private static readonly float[] _fdnDelayTimes =
        [
            // Room
            53.953247f, 79.192566f, 116.238770f, 130.615295f,
            // Hall
            53.953247f, 79.192566f, 116.238770f, 170.615295f,
            // Plate
            5f, 10f, 5f, 10f,
            // Cathedral
            47.03f, 71f, 103f, 170f,
            // Max delay (Hall is the one with the highest values so identical to Hall)
            53.953247f, 79.192566f, 116.238770f, 170.615295f
        ];

        private static readonly float[] _decayDelayTimes =
        [
            // Room
            7f, 9f, 13f, 17f,
            // Hall
            7f, 9f, 13f, 17f,
            // Plate (no decay)
            1f, 1f, 1f, 1f,
            // Cathedral
            7f, 7f, 13f, 9f,
            // Max delay (Hall is the one with the highest values so identical to Hall)
            7f, 9f, 13f, 17f
        ];

        private static readonly float[] _earlyDelayTimes =
        [
            // Room
            0.0f, 3.5f, 2.8f, 3.9f, 2.7f, 13.4f, 7.9f, 8.4f, 9.9f, 12.0f,
            // Chamber
            0.0f, 11.8f, 5.5f, 11.2f, 10.4f, 38.1f, 22.2f, 29.6f, 21.2f, 24.8f,
            // Hall
            0.0f, 41.5f, 20.5f, 41.3f, 0.0f, 29.5f, 33.8f, 45.2f, 46.8f, 0.0f,
            // Cathedral
            33.1f, 43.3f, 22.8f, 37.9f, 14.9f, 35.3f, 17.9f, 34.2f, 0.0f, 43.3f,
            // Disabled
            0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f
        ];

        private static readonly float[] _earlyGainBase =
        [
            // Room
            0.70f, 0.68f, 0.70f, 0.68f, 0.70f, 0.68f, 0.70f, 0.68f, 0.68f, 0.68f,
            // Chamber
            0.70f, 0.68f, 0.70f, 0.68f, 0.70f, 0.68f, 0.68f, 0.68f, 0.68f, 0.68f,
            // Hall
            0.50f, 0.70f, 0.70f, 0.68f, 0.50f, 0.68f, 0.68f, 0.70f, 0.68f, 0.00f,
            // Cathedral
            0.93f, 0.92f, 0.87f, 0.86f, 0.94f, 0.81f, 0.80f, 0.77f, 0.76f, 0.65f,
            // Disabled
            0.00f, 0.00f, 0.00f, 0.00f, 0.00f, 0.00f, 0.00f, 0.00f, 0.00f, 0.00f
        ];

        private static readonly float[] _preDelayTimes =
        [
            // Room
            12.5f,
            // Chamber
            40.0f,
            // Hall
            50.0f,
            // Cathedral
            50.0f,
            // Disabled
            0.0f
        ];

        public DelayLine[] FdnDelayLines { get; }
        public DecayDelay[] DecayDelays { get; }
        public DelayLine PreDelayLine { get; }
        public DelayLine FrontCenterDelayLine { get; }
        public uint[] EarlyDelayTime { get; }
        public float[] EarlyGain { get; }
        public uint PreDelayLineDelayTime { get; private set; }

        public float[] HighFrequencyDecayDirectGain { get; }
        public float[] HighFrequencyDecayPreviousGain { get; }
        public float[] PreviousFeedbackOutput { get; }

        public const int EarlyModeCount = 10;

        private const int FixedPointPrecision = 14;

        private static ReadOnlySpan<float> GetFdnDelayTimesByLateMode(ReverbLateMode lateMode)
        {
            return _fdnDelayTimes.AsSpan((int)lateMode * 4, 4);
        }

        private static ReadOnlySpan<float> GetDecayDelayTimesByLateMode(ReverbLateMode lateMode)
        {
            return _decayDelayTimes.AsSpan((int)lateMode * 4, 4);
        }

        public ReverbState(ref ReverbParameter parameter, ulong workBuffer, bool isLongSizePreDelaySupported)
        {
            FdnDelayLines = new DelayLine[4];
            DecayDelays = new DecayDelay[4];
            EarlyDelayTime = new uint[EarlyModeCount];
            EarlyGain = new float[EarlyModeCount];
            HighFrequencyDecayDirectGain = new float[4];
            HighFrequencyDecayPreviousGain = new float[4];
            PreviousFeedbackOutput = new float[4];

            ReadOnlySpan<float> fdnDelayTimes = GetFdnDelayTimesByLateMode(ReverbLateMode.Limit);
            ReadOnlySpan<float> decayDelayTimes = GetDecayDelayTimesByLateMode(ReverbLateMode.Limit);

            uint sampleRate = (uint)FixedPointHelper.ToFloat((uint)parameter.SampleRate, FixedPointPrecision);

            for (int i = 0; i < 4; i++)
            {
                FdnDelayLines[i] = new DelayLine(sampleRate, fdnDelayTimes[i]);
                DecayDelays[i] = new DecayDelay(new DelayLine(sampleRate, decayDelayTimes[i]));
            }

            float preDelayTimeMax = 150.0f;

            if (isLongSizePreDelaySupported)
            {
                preDelayTimeMax = 350.0f;
            }

            PreDelayLine = new DelayLine(sampleRate, preDelayTimeMax);
            FrontCenterDelayLine = new DelayLine(sampleRate, 5.0f);

            UpdateParameter(ref parameter);
        }

        public void UpdateParameter(ref ReverbParameter parameter)
        {
            uint sampleRate = (uint)FixedPointHelper.ToFloat((uint)parameter.SampleRate, FixedPointPrecision);

            float preDelayTimeInMilliseconds = FixedPointHelper.ToFloat(parameter.PreDelayTime, FixedPointPrecision);
            float earlyGain = FixedPointHelper.ToFloat(parameter.EarlyGain, FixedPointPrecision);
            float coloration = FixedPointHelper.ToFloat(parameter.Coloration, FixedPointPrecision);
            float decayTime = FixedPointHelper.ToFloat(parameter.DecayTime, FixedPointPrecision);

            for (int i = 0; i < 10; i++)
            {
                EarlyDelayTime[i] = Math.Min(IDelayLine.GetSampleCount(sampleRate, _earlyDelayTimes[i] + preDelayTimeInMilliseconds), PreDelayLine.SampleCountMax) + 1;
                EarlyGain[i] = _earlyGainBase[i] * earlyGain;
            }

            if (parameter.ChannelCount == 2)
            {
                EarlyGain[4] = EarlyGain[4] * 0.5f;
                EarlyGain[5] = EarlyGain[5] * 0.5f;
            }

            PreDelayLineDelayTime = Math.Min(IDelayLine.GetSampleCount(sampleRate, _preDelayTimes[(int)parameter.EarlyMode] + preDelayTimeInMilliseconds), PreDelayLine.SampleCountMax);

            ReadOnlySpan<float> fdnDelayTimes = GetFdnDelayTimesByLateMode(parameter.LateMode);
            ReadOnlySpan<float> decayDelayTimes = GetDecayDelayTimesByLateMode(parameter.LateMode);

            float highFrequencyDecayRatio = FixedPointHelper.ToFloat(parameter.HighFrequencyDecayRatio, FixedPointPrecision);
            float highFrequencyUnknownValue = FloatingPointHelper.Cos(1280.0f / sampleRate);

            for (int i = 0; i < 4; i++)
            {
                FdnDelayLines[i].SetDelay(fdnDelayTimes[i]);
                DecayDelays[i].SetDelay(decayDelayTimes[i]);

                float tempA = -3 * (DecayDelays[i].CurrentSampleCount + FdnDelayLines[i].CurrentSampleCount);
                float tempB = tempA / (decayTime * sampleRate);
                float tempC;
                float tempD;

                if (highFrequencyDecayRatio < 0.995f)
                {
                    float tempE = FloatingPointHelper.Pow10((((1.0f / highFrequencyDecayRatio) - 1.0f) * 2) / 100 * (tempB / 10));
                    float tempF = 1.0f - tempE;
                    float tempG = 2.0f - (tempE * 2 * highFrequencyUnknownValue);
                    float tempH = MathF.Sqrt((tempG * tempG) - (tempF * tempF * 4));

                    tempC = (tempG - tempH) / (tempF * 2);
                    tempD = 1.0f - tempC;
                }
                else
                {
                    // no high frequency decay ratio
                    tempC = 0.0f;
                    tempD = 1.0f;
                }

                HighFrequencyDecayDirectGain[i] = FloatingPointHelper.Pow10(tempB / 1000) * tempD * 0.7071f;
                HighFrequencyDecayPreviousGain[i] = tempC;
                PreviousFeedbackOutput[i] = 0.0f;

                DecayDelays[i].SetDecayRate(0.6f * (1.0f - coloration));
            }
        }
    }
}
