using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Ryujinx.Audio.Backends.CompatLayer
{
    public static class Downmixing
    {
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct Channel51FormatPCM16
        {
            public short FrontLeft;
            public short FrontRight;
            public short FrontCenter;
            public short LowFrequency;
            public short BackLeft;
            public short BackRight;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct ChannelStereoFormatPCM16
        {
            public short Left;
            public short Right;
        }

        private const int Q15Bits = 16;
        private const int RawQ15One = 1 << Q15Bits;
        private const int RawQ15HalfOne = (int)(0.5f * RawQ15One);
        private const int Minus3dBInQ15 = (int)(0.707f * RawQ15One);
        private const int Minus6dBInQ15 = (int)(0.501f * RawQ15One);
        private const int Minus12dBInQ15 = (int)(0.251f * RawQ15One);

        private static readonly long[] _defaultSurroundToStereoCoefficients =
        [
            RawQ15One,
            Minus3dBInQ15,
            Minus12dBInQ15,
            Minus3dBInQ15
        ];

        private static readonly long[] _defaultStereoToMonoCoefficients =
        [
            Minus6dBInQ15,
            Minus6dBInQ15
        ];

        private const int SurroundChannelCount = 6;
        private const int StereoChannelCount = 2;
        private const int MonoChannelCount = 1;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ReadOnlySpan<Channel51FormatPCM16> GetSurroundBuffer(ReadOnlySpan<short> data)
        {
            return MemoryMarshal.Cast<short, Channel51FormatPCM16>(data);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ReadOnlySpan<ChannelStereoFormatPCM16> GetStereoBuffer(ReadOnlySpan<short> data)
        {
            return MemoryMarshal.Cast<short, ChannelStereoFormatPCM16>(data);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static short DownMixStereoToMono(ReadOnlySpan<long> coefficients, short left, short right)
        {
            return (short)Math.Clamp((left * coefficients[0] + right * coefficients[1]) >> Q15Bits, short.MinValue, short.MaxValue);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static short DownMixSurroundToStereo(ReadOnlySpan<long> coefficients, short back, short lfe, short center, short front)
        {
            return (short)Math.Clamp(
                (coefficients[3] * back +
                coefficients[2] * lfe +
                coefficients[1] * center +
                coefficients[0] * front + RawQ15HalfOne) >> Q15Bits, short.MinValue, short.MaxValue);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static short[] DownMixSurroundToStereo(ReadOnlySpan<long> coefficients, ReadOnlySpan<short> data)
        {
            int samplePerChannelCount = data.Length / SurroundChannelCount;

            short[] downmixedBuffer = new short[samplePerChannelCount * StereoChannelCount];

            ReadOnlySpan<Channel51FormatPCM16> channels = GetSurroundBuffer(data);

            for (int i = 0; i < samplePerChannelCount; i++)
            {
                Channel51FormatPCM16 channel = channels[i];

                downmixedBuffer[i * 2] = DownMixSurroundToStereo(coefficients, channel.BackLeft, channel.LowFrequency, channel.FrontCenter, channel.FrontLeft);
                downmixedBuffer[i * 2 + 1] = DownMixSurroundToStereo(coefficients, channel.BackRight, channel.LowFrequency, channel.FrontCenter, channel.FrontRight);
            }

            return downmixedBuffer;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static short[] DownMixStereoToMono(ReadOnlySpan<long> coefficients, ReadOnlySpan<short> data)
        {
            int samplePerChannelCount = data.Length / StereoChannelCount;

            short[] downmixedBuffer = new short[samplePerChannelCount * MonoChannelCount];

            ReadOnlySpan<ChannelStereoFormatPCM16> channels = GetStereoBuffer(data);

            for (int i = 0; i < samplePerChannelCount; i++)
            {
                ChannelStereoFormatPCM16 channel = channels[i];

                downmixedBuffer[i] = DownMixStereoToMono(coefficients, channel.Left, channel.Right);
            }

            return downmixedBuffer;
        }

        public static short[] DownMixStereoToMono(ReadOnlySpan<short> data)
        {
            return DownMixStereoToMono(_defaultStereoToMonoCoefficients, data);
        }

        public static short[] DownMixSurroundToStereo(ReadOnlySpan<short> data)
        {
            return DownMixSurroundToStereo(_defaultSurroundToStereoCoefficients, data);
        }
    }
}
