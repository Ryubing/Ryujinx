using Ryujinx.Common.Memory;
using System;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace Ryujinx.Graphics.OpenGL.Image
{
    static class FormatConverter
    {
        public unsafe static MemoryOwner<byte> ConvertS8D24ToD24S8(ReadOnlySpan<byte> data)
        {
            MemoryOwner<byte> outputMemory = MemoryOwner<byte>.Rent(data.Length);

            Span<byte> output = outputMemory.Span;

            int start = 0;

            if (Avx2.IsSupported)
            {
                Vector256<byte> mask = Vector256.Create(
                    (byte)3, (byte)0, (byte)1, (byte)2,
                    (byte)7, (byte)4, (byte)5, (byte)6,
                    (byte)11, (byte)8, (byte)9, (byte)10,
                    (byte)15, (byte)12, (byte)13, (byte)14,
                    (byte)19, (byte)16, (byte)17, (byte)18,
                    (byte)23, (byte)20, (byte)21, (byte)22,
                    (byte)27, (byte)24, (byte)25, (byte)26,
                    (byte)31, (byte)28, (byte)29, (byte)30);

                int sizeAligned = data.Length & ~31;

                fixed (byte* pInput = data, pOutput = output)
                {
                    for (uint i = 0; i < sizeAligned; i += 32)
                    {
                        Vector256<byte> dataVec = Avx.LoadVector256(pInput + i);

                        dataVec = Avx2.Shuffle(dataVec, mask);

                        Avx.Store(pOutput + i, dataVec);
                    }
                }

                start = sizeAligned;
            }
            else if (Ssse3.IsSupported)
            {
                Vector128<byte> mask = Vector128.Create(
                    (byte)3, (byte)0, (byte)1, (byte)2,
                    (byte)7, (byte)4, (byte)5, (byte)6,
                    (byte)11, (byte)8, (byte)9, (byte)10,
                    (byte)15, (byte)12, (byte)13, (byte)14);

                int sizeAligned = data.Length & ~15;

                fixed (byte* pInput = data, pOutput = output)
                {
                    for (uint i = 0; i < sizeAligned; i += 16)
                    {
                        Vector128<byte> dataVec = Sse2.LoadVector128(pInput + i);

                        dataVec = Ssse3.Shuffle(dataVec, mask);

                        Sse2.Store(pOutput + i, dataVec);
                    }
                }

                start = sizeAligned;
            }

            Span<uint> outSpan = MemoryMarshal.Cast<byte, uint>(output);
            ReadOnlySpan<uint> dataSpan = MemoryMarshal.Cast<byte, uint>(data);
            for (int i = start / sizeof(uint); i < dataSpan.Length; i++)
            {
                outSpan[i] = BitOperations.RotateLeft(dataSpan[i], 8);
            }

            return outputMemory;
        }

        public unsafe static byte[] ConvertD24S8ToS8D24(ReadOnlySpan<byte> data)
        {
            byte[] output = new byte[data.Length];

            int start = 0;

            if (Avx2.IsSupported)
            {
                Vector256<byte> mask = Vector256.Create(
                    (byte)1, (byte)2, (byte)3, (byte)0,
                    (byte)5, (byte)6, (byte)7, (byte)4,
                    (byte)9, (byte)10, (byte)11, (byte)8,
                    (byte)13, (byte)14, (byte)15, (byte)12,
                    (byte)17, (byte)18, (byte)19, (byte)16,
                    (byte)21, (byte)22, (byte)23, (byte)20,
                    (byte)25, (byte)26, (byte)27, (byte)24,
                    (byte)29, (byte)30, (byte)31, (byte)28);

                int sizeAligned = data.Length & ~31;

                fixed (byte* pInput = data, pOutput = output)
                {
                    for (uint i = 0; i < sizeAligned; i += 32)
                    {
                        Vector256<byte> dataVec = Avx.LoadVector256(pInput + i);

                        dataVec = Avx2.Shuffle(dataVec, mask);

                        Avx.Store(pOutput + i, dataVec);
                    }
                }

                start = sizeAligned;
            }
            else if (Ssse3.IsSupported)
            {
                Vector128<byte> mask = Vector128.Create(
                    (byte)1, (byte)2, (byte)3, (byte)0,
                    (byte)5, (byte)6, (byte)7, (byte)4,
                    (byte)9, (byte)10, (byte)11, (byte)8,
                    (byte)13, (byte)14, (byte)15, (byte)12);

                int sizeAligned = data.Length & ~15;

                fixed (byte* pInput = data, pOutput = output)
                {
                    for (uint i = 0; i < sizeAligned; i += 16)
                    {
                        Vector128<byte> dataVec = Sse2.LoadVector128(pInput + i);

                        dataVec = Ssse3.Shuffle(dataVec, mask);

                        Sse2.Store(pOutput + i, dataVec);
                    }
                }

                start = sizeAligned;
            }

            Span<uint> outSpan = MemoryMarshal.Cast<byte, uint>(output);
            ReadOnlySpan<uint> dataSpan = MemoryMarshal.Cast<byte, uint>(data);
            for (int i = start / sizeof(uint); i < dataSpan.Length; i++)
            {
                outSpan[i] = BitOperations.RotateRight(dataSpan[i], 8);
            }

            return output;
        }
    }
}
