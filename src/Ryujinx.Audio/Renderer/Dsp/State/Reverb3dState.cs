using Ryujinx.Audio.Renderer.Dsp.Effect;
using Ryujinx.Audio.Renderer.Parameter.Effect;
using System;

namespace Ryujinx.Audio.Renderer.Dsp.State
{
    public struct Reverb3dState
    {
        private readonly float[] _fdnDelayMinTimes = [5.0f, 6.0f, 13.0f, 14.0f];
        private readonly float[] _fdnDelayMaxTimes = [45.704f, 82.782f, 149.94f, 271.58f];
        private readonly float[] _decayDelayMaxTimes1 = [17.0f, 13.0f, 9.0f, 7.0f];
        private readonly float[] _decayDelayMaxTimes2 = [19.0f, 11.0f, 10.0f, 6.0f];
        private readonly float[] _earlyDelayTimes = [0.017136f, 0.059154f, 0.16173f, 0.39019f, 0.42526f, 0.45541f, 0.68974f, 0.74591f, 0.83384f, 0.8595f, 0.0f, 0.075024f, 0.16879f, 0.2999f, 0.33744f, 0.3719f, 0.59901f, 0.71674f, 0.81786f, 0.85166f
        ];
        public readonly float[] EarlyGain = [0.67096f, 0.61027f, 1.0f, 0.35680f, 0.68361f, 0.65978f, 0.51939f, 0.24712f, 0.45945f, 0.45021f, 0.64196f, 0.54879f, 0.92925f, 0.38270f, 0.72867f, 0.69794f, 0.5464f, 0.24563f, 0.45214f, 0.44042f
        ];

        public IDelayLine[] FdnDelayLines { get; }
        public DecayDelay[] DecayDelays1 { get; }
        public DecayDelay[] DecayDelays2 { get; }
        public IDelayLine PreDelayLine { get; }
        public IDelayLine FrontCenterDelayLine { get; }
        public float DryGain { get; private set; }
        public uint[] EarlyDelayTime { get; private set; }
        public float PreviousPreDelayValue { get; set; }
        public float PreviousPreDelayGain { get; private set; }
        public float TargetPreDelayGain { get; private set; }
        public float EarlyReflectionsGain { get; private set; }
        public float LateReverbGain { get; private set; }
        public uint ReflectionDelayTime { get; private set; }
        public float EchoLateReverbDecay { get; private set; }
        public float[] DecayDirectFdnGain { get; private set; }
        public float[] DecayCurrentFdnGain { get; private set; }
        public float[] DecayCurrentOutputGain { get; private set; }
        public float[] PreviousFeedbackOutputDecayed { get; private set; }

        public Reverb3dState(ref Reverb3dParameter parameter, ulong workBuffer)
        {
            FdnDelayLines = new IDelayLine[4];
            DecayDelays1 = new DecayDelay[4];
            DecayDelays2 = new DecayDelay[4];
            DecayDirectFdnGain = new float[4];
            DecayCurrentFdnGain = new float[4];
            DecayCurrentOutputGain = new float[4];
            PreviousFeedbackOutputDecayed = new float[4];

            uint sampleRate = parameter.SampleRate / 1000;

            for (int i = 0; i < 4; i++)
            {
                FdnDelayLines[i] = new DelayLine3d(sampleRate, _fdnDelayMaxTimes[i]);
                DecayDelays1[i] = new DecayDelay(new DelayLine3d(sampleRate, _decayDelayMaxTimes1[i]));
                DecayDelays2[i] = new DecayDelay(new DelayLine3d(sampleRate, _decayDelayMaxTimes2[i]));
            }

            PreDelayLine = new DelayLine3d(sampleRate, 400);
            FrontCenterDelayLine = new DelayLine3d(sampleRate, 5);

            UpdateParameter(ref parameter);
        }

        public void UpdateParameter(ref Reverb3dParameter parameter)
        {
            uint sampleRate = parameter.SampleRate / 1000;

            EarlyDelayTime = new uint[20];
            DryGain = parameter.DryGain;
            PreviousFeedbackOutputDecayed.AsSpan().Clear();
            PreviousPreDelayValue = 0;

            EarlyReflectionsGain = FloatingPointHelper.Pow10(Math.Min(parameter.RoomGain + parameter.ReflectionsGain, 5000.0f) / 2000.0f);
            LateReverbGain = FloatingPointHelper.Pow10(Math.Min(parameter.RoomGain + parameter.ReverbGain, 5000.0f) / 2000.0f);

            float highFrequencyRoomGain = FloatingPointHelper.Pow10(parameter.RoomHf / 2000.0f);

            if (highFrequencyRoomGain < 1.0f)
            {
                float tempA = 1.0f - highFrequencyRoomGain;
                float tempB = 2.0f - ((2.0f * highFrequencyRoomGain) * FloatingPointHelper.Cos(256.0f * parameter.HfReference / parameter.SampleRate));
                float tempC = MathF.Sqrt(MathF.Pow(tempB, 2) - (4.0f * (1.0f - highFrequencyRoomGain) * (1.0f - highFrequencyRoomGain)));

                PreviousPreDelayGain = (tempB - tempC) / (2.0f * tempA);
                TargetPreDelayGain = 1.0f - PreviousPreDelayGain;
            }
            else
            {
                PreviousPreDelayGain = 0.0f;
                TargetPreDelayGain = 1.0f;
            }

            ReflectionDelayTime = IDelayLine.GetSampleCount(sampleRate, 1000.0f * (parameter.ReflectionDelay + parameter.ReverbDelayTime));
            EchoLateReverbDecay = 0.6f * parameter.Diffusion * 0.01f;

            for (int i = 0; i < FdnDelayLines.Length; i++)
            {
                FdnDelayLines[i].SetDelay(_fdnDelayMinTimes[i] + (parameter.Density / 100 * (_fdnDelayMaxTimes[i] - _fdnDelayMinTimes[i])));

                uint tempSampleCount = FdnDelayLines[i].CurrentSampleCount + DecayDelays1[i].CurrentSampleCount + DecayDelays2[i].CurrentSampleCount;

                float tempA = (-60.0f * tempSampleCount) / (parameter.DecayTime * parameter.SampleRate);
                float tempB = tempA / parameter.HfDecayRatio;
                float tempC = FloatingPointHelper.Cos(128.0f * 0.5f * parameter.HfReference / parameter.SampleRate) / FloatingPointHelper.Sin(128.0f * 0.5f * parameter.HfReference / parameter.SampleRate);
                float tempD = FloatingPointHelper.Pow10((tempB - tempA) / 40.0f);
                float tempE = FloatingPointHelper.Pow10((tempB + tempA) / 40.0f) * 0.7071f;

                DecayDirectFdnGain[i] = tempE * ((tempD * tempC) + 1.0f) / (tempC + tempD);
                DecayCurrentFdnGain[i] = tempE * (1.0f - (tempD * tempC)) / (tempC + tempD);
                DecayCurrentOutputGain[i] = (tempC - tempD) / (tempC + tempD);

                DecayDelays1[i].SetDecayRate(EchoLateReverbDecay);
                DecayDelays2[i].SetDecayRate(EchoLateReverbDecay * -0.9f);
            }

            for (int i = 0; i < EarlyDelayTime.Length; i++)
            {
                uint sampleCount = Math.Min(IDelayLine.GetSampleCount(sampleRate, (parameter.ReflectionDelay * 1000.0f) + (_earlyDelayTimes[i] * 1000.0f * ((parameter.ReverbDelayTime * 0.9998f) + 0.02f))), PreDelayLine.SampleCountMax);
                EarlyDelayTime[i] = sampleCount;
            }
        }
    }
}
