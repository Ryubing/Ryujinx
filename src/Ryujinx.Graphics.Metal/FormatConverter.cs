using System;
using System.Runtime.InteropServices;

namespace Ryujinx.Graphics.Metal
{
    class FormatConverter
    {
        public static void ConvertD24S8ToD32FS8(Span<byte> output, ReadOnlySpan<byte> input)
        {
            const float UnormToFloat = 1f / 0xffffff;

            Span<uint> outputUint = MemoryMarshal.Cast<byte, uint>(output);
            ReadOnlySpan<uint> inputUint = MemoryMarshal.Cast<byte, uint>(input);

            int i = 0;

            for (; i < inputUint.Length; i++)
            {
                uint depthStencil = inputUint[i];
                uint depth = depthStencil >> 8;
                uint stencil = depthStencil & 0xff;

                int j = i * 2;

                outputUint[j] = (uint)BitConverter.SingleToInt32Bits(depth * UnormToFloat);
                outputUint[j + 1] = stencil;
            }
        }

        public static void ConvertD32FS8ToD24S8(Span<byte> output, ReadOnlySpan<byte> input)
        {
            Span<uint> outputUint = MemoryMarshal.Cast<byte, uint>(output);
            ReadOnlySpan<uint> inputUint = MemoryMarshal.Cast<byte, uint>(input);

            int i = 0;

            for (; i < inputUint.Length; i += 2)
            {
                float depth = BitConverter.Int32BitsToSingle((int)inputUint[i]);
                uint stencil = inputUint[i + 1];
                uint depthStencil = (Math.Clamp((uint)(depth * 0xffffff), 0, 0xffffff) << 8) | (stencil & 0xff);

                int j = i >> 1;

                outputUint[j] = depthStencil;
            }
        }
    }
}
