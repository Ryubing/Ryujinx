using Ryujinx.Common;
using Ryujinx.Common.Memory;
using System;
using System.Buffers.Binary;
using System.Runtime.InteropServices;

namespace Ryujinx.Graphics.Texture
{
    public static class ETC2Decoder
    {
        private const uint AlphaMask = 0xff000000u;

        private const int BlockWidth = 4;
        private const int BlockHeight = 4;

        private static readonly int[][] _etc1Lut =
        [
            [2, 8, -2, -8],
            [5, 17, -5, -17],
            [9, 29, -9, -29],
            [13, 42, -13, -42],
            [18, 60, -18, -60],
            [24, 80, -24, -80],
            [33, 106, -33, -106],
            [47, 183, -47, -183]
        ];

        private static readonly int[] _etc2Lut =
        [
            3, 6, 11, 16, 23, 32, 41, 64
        ];

        private static readonly int[][] _etc2AlphaLut =
        [
            [-3, -6, -9, -15, 2, 5, 8, 14],
            [-3, -7, -10, -13, 2, 6, 9, 12],
            [-2, -5, -8, -13, 1, 4, 7, 12],
            [-2, -4, -6, -13, 1, 3, 5, 12],
            [-3, -6, -8, -12, 2, 5, 7, 11],
            [-3, -7, -9, -11, 2, 6, 8, 10],
            [-4, -7, -8, -11, 3, 6, 7, 10],
            [-3, -5, -8, -11, 2, 4, 7, 10],
            [-2, -6, -8, -10, 1, 5, 7, 9],
            [-2, -5, -8, -10, 1, 4, 7, 9],
            [-2, -4, -8, -10, 1, 3, 7, 9],
            [-2, -5, -7, -10, 1, 4, 6, 9],
            [-3, -4, -7, -10, 2, 3, 6, 9],
            [-1, -2, -3, -10, 0, 1, 2, 9],
            [-4, -6, -8, -9, 3, 5, 7, 8],
            [-3, -5, -7, -9, 2, 4, 6, 8]
        ];

        public static MemoryOwner<byte> DecodeRgb(ReadOnlySpan<byte> data, int width, int height, int depth, int levels, int layers)
        {
            ReadOnlySpan<ulong> dataUlong = MemoryMarshal.Cast<byte, ulong>(data);

            int inputOffset = 0;

            MemoryOwner<byte> output = MemoryOwner<byte>.Rent(CalculateOutputSize(width, height, depth, levels, layers));

            Span<uint> outputUint = MemoryMarshal.Cast<byte, uint>(output.Span);
            Span<uint> tile = stackalloc uint[BlockWidth * BlockHeight];

            int imageBaseOOffs = 0;

            for (int l = 0; l < levels; l++)
            {
                int wInBlocks = BitUtils.DivRoundUp(width, BlockWidth);
                int hInBlocks = BitUtils.DivRoundUp(height, BlockHeight);

                for (int l2 = 0; l2 < layers; l2++)
                {
                    for (int z = 0; z < depth; z++)
                    {
                        for (int y = 0; y < hInBlocks; y++)
                        {
                            int ty = y * BlockHeight;
                            int bh = Math.Min(BlockHeight, height - ty);

                            for (int x = 0; x < wInBlocks; x++)
                            {
                                int tx = x * BlockWidth;
                                int bw = Math.Min(BlockWidth, width - tx);

                                ulong colorBlock = dataUlong[inputOffset++];

                                DecodeBlock(tile, colorBlock);

                                for (int py = 0; py < bh; py++)
                                {
                                    int oOffsBase = imageBaseOOffs + ((ty + py) * width) + tx;

                                    for (int px = 0; px < bw; px++)
                                    {
                                        int oOffs = oOffsBase + px;

                                        outputUint[oOffs] = tile[py * BlockWidth + px] | AlphaMask;
                                    }
                                }
                            }
                        }

                        imageBaseOOffs += width * height;
                    }
                }

                width = Math.Max(1, width >> 1);
                height = Math.Max(1, height >> 1);
                depth = Math.Max(1, depth >> 1);
            }

            return output;
        }

        public static MemoryOwner<byte> DecodePta(ReadOnlySpan<byte> data, int width, int height, int depth, int levels, int layers)
        {
            ReadOnlySpan<ulong> dataUlong = MemoryMarshal.Cast<byte, ulong>(data);

            int inputOffset = 0;

            MemoryOwner<byte> output = MemoryOwner<byte>.Rent(CalculateOutputSize(width, height, depth, levels, layers));

            Span<uint> outputUint = MemoryMarshal.Cast<byte, uint>(output.Span);
            Span<uint> tile = stackalloc uint[BlockWidth * BlockHeight];

            int imageBaseOOffs = 0;

            for (int l = 0; l < levels; l++)
            {
                int wInBlocks = BitUtils.DivRoundUp(width, BlockWidth);
                int hInBlocks = BitUtils.DivRoundUp(height, BlockHeight);

                for (int l2 = 0; l2 < layers; l2++)
                {
                    for (int z = 0; z < depth; z++)
                    {
                        for (int y = 0; y < hInBlocks; y++)
                        {
                            int ty = y * BlockHeight;
                            int bh = Math.Min(BlockHeight, height - ty);

                            for (int x = 0; x < wInBlocks; x++)
                            {
                                int tx = x * BlockWidth;
                                int bw = Math.Min(BlockWidth, width - tx);

                                ulong colorBlock = dataUlong[inputOffset++];

                                DecodeBlockPta(tile, colorBlock);

                                for (int py = 0; py < bh; py++)
                                {
                                    int oOffsBase = imageBaseOOffs + ((ty + py) * width) + tx;

                                    tile.Slice(py * BlockWidth, bw).CopyTo(outputUint.Slice(oOffsBase, bw));
                                }
                            }
                        }

                        imageBaseOOffs += width * height;
                    }
                }

                width = Math.Max(1, width >> 1);
                height = Math.Max(1, height >> 1);
                depth = Math.Max(1, depth >> 1);
            }

            return output;
        }

        public static MemoryOwner<byte> DecodeRgba(ReadOnlySpan<byte> data, int width, int height, int depth, int levels, int layers)
        {
            ReadOnlySpan<ulong> dataUlong = MemoryMarshal.Cast<byte, ulong>(data);

            int inputOffset = 0;

            MemoryOwner<byte> output = MemoryOwner<byte>.Rent(CalculateOutputSize(width, height, depth, levels, layers));

            Span<uint> outputUint = MemoryMarshal.Cast<byte, uint>(output.Span);
            Span<uint> tile = stackalloc uint[BlockWidth * BlockHeight];

            int imageBaseOOffs = 0;

            for (int l = 0; l < levels; l++)
            {
                int wInBlocks = BitUtils.DivRoundUp(width, BlockWidth);
                int hInBlocks = BitUtils.DivRoundUp(height, BlockHeight);

                for (int l2 = 0; l2 < layers; l2++)
                {
                    for (int z = 0; z < depth; z++)
                    {
                        for (int y = 0; y < hInBlocks; y++)
                        {
                            int ty = y * BlockHeight;
                            int bh = Math.Min(BlockHeight, height - ty);

                            for (int x = 0; x < wInBlocks; x++)
                            {
                                int tx = x * BlockWidth;
                                int bw = Math.Min(BlockWidth, width - tx);

                                ulong alphaBlock = dataUlong[inputOffset];
                                ulong colorBlock = dataUlong[inputOffset + 1];

                                inputOffset += 2;

                                DecodeBlock(tile, colorBlock);

                                byte alphaBase = (byte)alphaBlock;
                                int[] alphaTable = _etc2AlphaLut[(alphaBlock >> 8) & 0xf];
                                int alphaMultiplier = (int)(alphaBlock >> 12) & 0xf;
                                ulong alphaIndices = BinaryPrimitives.ReverseEndianness(alphaBlock);

                                if (alphaMultiplier != 0)
                                {
                                    for (int py = 0; py < bh; py++)
                                    {
                                        int oOffsBase = imageBaseOOffs + ((ty + py) * width) + tx;

                                        for (int px = 0; px < bw; px++)
                                        {
                                            int oOffs = oOffsBase + px;
                                            int alphaIndex = (int)((alphaIndices >> (((px * BlockHeight + py) ^ 0xf) * 3)) & 7);

                                            byte a = Saturate(alphaBase + alphaTable[alphaIndex] * alphaMultiplier);

                                            outputUint[oOffs] = tile[py * BlockWidth + px] | ((uint)a << 24);
                                        }
                                    }
                                }
                                else
                                {
                                    uint a = (uint)alphaBase << 24;

                                    for (int py = 0; py < bh; py++)
                                    {
                                        int oOffsBase = imageBaseOOffs + ((ty + py) * width) + tx;

                                        for (int px = 0; px < bw; px++)
                                        {
                                            int oOffs = oOffsBase + px;

                                            outputUint[oOffs] = tile[py * BlockWidth + px] | a;
                                        }
                                    }
                                }
                            }
                        }

                        imageBaseOOffs += width * height;
                    }
                }

                width = Math.Max(1, width >> 1);
                height = Math.Max(1, height >> 1);
                depth = Math.Max(1, depth >> 1);
            }

            return output;
        }

        private static void DecodeBlock(Span<uint> tile, ulong block)
        {
            uint blockLow = (uint)(block >> 0);
            uint blockHigh = (uint)(block >> 32);

            uint r1, g1, b1;
            uint r2, g2, b2;

            bool differentialMode = (blockLow & 0x2000000) != 0;

            if (differentialMode)
            {
                (r1, g1, b1, r2, g2, b2) = UnpackRgb555DiffEndPoints(blockLow);

                if (r2 > 31)
                {
                    DecodeBlock59T(tile, blockLow, blockHigh);
                }
                else if (g2 > 31)
                {
                    DecodeBlock58H(tile, blockLow, blockHigh);
                }
                else if (b2 > 31)
                {
                    DecodeBlock57P(tile, block);
                }
                else
                {
                    r1 |= r1 >> 5;
                    g1 |= g1 >> 5;
                    b1 |= b1 >> 5;

                    r2 = (r2 << 3) | (r2 >> 2);
                    g2 = (g2 << 3) | (g2 >> 2);
                    b2 = (b2 << 3) | (b2 >> 2);

                    DecodeBlockETC1(tile, blockLow, blockHigh, r1, g1, b1, r2, g2, b2);
                }
            }
            else
            {
                r1 = (blockLow & 0x0000f0) >> 0;
                g1 = (blockLow & 0x00f000) >> 8;
                b1 = (blockLow & 0xf00000) >> 16;

                r2 = (blockLow & 0x00000f) << 4;
                g2 = (blockLow & 0x000f00) >> 4;
                b2 = (blockLow & 0x0f0000) >> 12;

                r1 |= r1 >> 4;
                g1 |= g1 >> 4;
                b1 |= b1 >> 4;

                r2 |= r2 >> 4;
                g2 |= g2 >> 4;
                b2 |= b2 >> 4;

                DecodeBlockETC1(tile, blockLow, blockHigh, r1, g1, b1, r2, g2, b2);
            }
        }

        private static void DecodeBlockPta(Span<uint> tile, ulong block)
        {
            uint blockLow = (uint)(block >> 0);
            uint blockHigh = (uint)(block >> 32);

            (uint r1, uint g1, uint b1, uint r2, uint g2, uint b2) = UnpackRgb555DiffEndPoints(blockLow);

            bool fullyOpaque = (blockLow & 0x2000000) != 0;

            if (fullyOpaque)
            {
                if (r2 > 31)
                {
                    DecodeBlock59T(tile, blockLow, blockHigh);
                }
                else if (g2 > 31)
                {
                    DecodeBlock58H(tile, blockLow, blockHigh);
                }
                else if (b2 > 31)
                {
                    DecodeBlock57P(tile, block);
                }
                else
                {
                    r1 |= r1 >> 5;
                    g1 |= g1 >> 5;
                    b1 |= b1 >> 5;

                    r2 = (r2 << 3) | (r2 >> 2);
                    g2 = (g2 << 3) | (g2 >> 2);
                    b2 = (b2 << 3) | (b2 >> 2);

                    DecodeBlockETC1(tile, blockLow, blockHigh, r1, g1, b1, r2, g2, b2);
                }

                for (int i = 0; i < tile.Length; i++)
                {
                    tile[i] |= AlphaMask;
                }
            }
            else
            {
                if (r2 > 31)
                {
                    DecodeBlock59T(tile, blockLow, blockHigh, AlphaMask);
                }
                else if (g2 > 31)
                {
                    DecodeBlock58H(tile, blockLow, blockHigh, AlphaMask);
                }
                else if (b2 > 31)
                {
                    DecodeBlock57P(tile, block);

                    for (int i = 0; i < tile.Length; i++)
                    {
                        tile[i] |= AlphaMask;
                    }
                }
                else
                {
                    r1 |= r1 >> 5;
                    g1 |= g1 >> 5;
                    b1 |= b1 >> 5;

                    r2 = (r2 << 3) | (r2 >> 2);
                    g2 = (g2 << 3) | (g2 >> 2);
                    b2 = (b2 << 3) | (b2 >> 2);

                    DecodeBlockETC1(tile, blockLow, blockHigh, r1, g1, b1, r2, g2, b2, AlphaMask);
                }
            }
        }

        private static (uint, uint, uint, uint, uint, uint) UnpackRgb555DiffEndPoints(uint blockLow)
        {
            uint r1 = (blockLow & 0x0000f8) >> 0;
            uint g1 = (blockLow & 0x00f800) >> 8;
            uint b1 = (blockLow & 0xf80000) >> 16;

            uint r2 = (uint)((sbyte)(r1 >> 3) + ((sbyte)((blockLow & 0x000007) << 5) >> 5));
            uint g2 = (uint)((sbyte)(g1 >> 3) + ((sbyte)((blockLow & 0x000700) >> 3) >> 5));
            uint b2 = (uint)((sbyte)(b1 >> 3) + ((sbyte)((blockLow & 0x070000) >> 11) >> 5));

            return (r1, g1, b1, r2, g2, b2);
        }

        private static void DecodeBlock59T(Span<uint> tile, uint blockLow, uint blockHigh, uint alphaMask = 0)
        {
            uint r1 = (blockLow & 3) | ((blockLow >> 1) & 0xc);
            uint g1 = (blockLow >> 12) & 0xf;
            uint b1 = (blockLow >> 8) & 0xf;

            uint r2 = (blockLow >> 20) & 0xf;
            uint g2 = (blockLow >> 16) & 0xf;
            uint b2 = (blockLow >> 28) & 0xf;

            r1 |= r1 << 4;
            g1 |= g1 << 4;
            b1 |= b1 << 4;

            r2 |= r2 << 4;
            g2 |= g2 << 4;
            b2 |= b2 << 4;

            int dist = _etc2Lut[((blockLow >> 24) & 1) | ((blockLow >> 25) & 6)];

            Span<uint> palette = stackalloc uint[4];

            palette[0] = Pack(r1, g1, b1);
            palette[1] = Pack(r2, g2, b2, dist);
            palette[2] = Pack(r2, g2, b2);
            palette[3] = Pack(r2, g2, b2, -dist);

            blockHigh = BinaryPrimitives.ReverseEndianness(blockHigh);

            for (int y = 0; y < BlockHeight; y++)
            {
                for (int x = 0; x < BlockWidth; x++)
                {
                    int offset = (y * 4) + x;
                    int index = (x * 4) + y;

                    int paletteIndex = (int)((blockHigh >> index) & 1) | (int)((blockHigh >> (index + 15)) & 2);

                    tile[offset] = palette[paletteIndex];

                    if (alphaMask != 0)
                    {
                        if (paletteIndex == 2)
                        {
                            tile[offset] = 0;
                        }
                        else
                        {
                            tile[offset] |= alphaMask;
                        }
                    }
                }
            }
        }

        private static void DecodeBlock58H(Span<uint> tile, uint blockLow, uint blockHigh, uint alphaMask = 0)
        {
            uint r1 = (blockLow >> 3) & 0xf;
            uint g1 = ((blockLow << 1) & 0xe) | ((blockLow >> 12) & 1);
            uint b1 = ((blockLow >> 23) & 1) | ((blockLow >> 7) & 6) | ((blockLow >> 8) & 8);

            uint r2 = (blockLow >> 19) & 0xf;
            uint g2 = ((blockLow >> 31) & 1) | ((blockLow >> 15) & 0xe);
            uint b2 = (blockLow >> 27) & 0xf;

            uint rgb1 = Pack4Be(r1, g1, b1);
            uint rgb2 = Pack4Be(r2, g2, b2);

            r1 |= r1 << 4;
            g1 |= g1 << 4;
            b1 |= b1 << 4;

            r2 |= r2 << 4;
            g2 |= g2 << 4;
            b2 |= b2 << 4;

            int dist = _etc2Lut[(rgb1 >= rgb2 ? 1u : 0u) | ((blockLow >> 23) & 2) | ((blockLow >> 24) & 4)];

            Span<uint> palette = stackalloc uint[4];

            palette[0] = Pack(r1, g1, b1, dist);
            palette[1] = Pack(r1, g1, b1, -dist);
            palette[2] = Pack(r2, g2, b2, dist);
            palette[3] = Pack(r2, g2, b2, -dist);

            blockHigh = BinaryPrimitives.ReverseEndianness(blockHigh);

            for (int y = 0; y < BlockHeight; y++)
            {
                for (int x = 0; x < BlockWidth; x++)
                {
                    int offset = (y * 4) + x;
                    int index = (x * 4) + y;

                    int paletteIndex = (int)((blockHigh >> index) & 1) | (int)((blockHigh >> (index + 15)) & 2);

                    tile[offset] = palette[paletteIndex];

                    if (alphaMask != 0)
                    {
                        if (paletteIndex == 2)
                        {
                            tile[offset] = 0;
                        }
                        else
                        {
                            tile[offset] |= alphaMask;
                        }
                    }
                }
            }
        }

        private static void DecodeBlock57P(Span<uint> tile, ulong block)
        {
            int r0 = (int)((block >> 1) & 0x3f);
            int g0 = (int)(((block >> 9) & 0x3f) | ((block & 1) << 6));
            int b0 = (int)(((block >> 31) & 1) | ((block >> 15) & 6) | ((block >> 16) & 0x18) | ((block >> 3) & 0x20));

            int rh = (int)(((block >> 24) & 1) | ((block >> 25) & 0x3e));
            int gh = (int)((block >> 33) & 0x7f);
            int bh = (int)(((block >> 43) & 0x1f) | ((block >> 27) & 0x20));

            int rv = (int)(((block >> 53) & 7) | ((block >> 37) & 0x38));
            int gv = (int)(((block >> 62) & 3) | ((block >> 46) & 0x7c));
            int bv = (int)((block >> 56) & 0x3f);

            r0 = (r0 << 2) | (r0 >> 4);
            g0 = (g0 << 1) | (g0 >> 6);
            b0 = (b0 << 2) | (b0 >> 4);

            rh = (rh << 2) | (rh >> 4);
            gh = (gh << 1) | (gh >> 6);
            bh = (bh << 2) | (bh >> 4);

            rv = (rv << 2) | (rv >> 4);
            gv = (gv << 1) | (gv >> 6);
            bv = (bv << 2) | (bv >> 4);

            for (int y = 0; y < BlockHeight; y++)
            {
                for (int x = 0; x < BlockWidth; x++)
                {
                    int offset = y * BlockWidth + x;

                    byte r = Saturate(((x * (rh - r0)) + (y * (rv - r0)) + (r0 * 4) + 2) >> 2);
                    byte g = Saturate(((x * (gh - g0)) + (y * (gv - g0)) + (g0 * 4) + 2) >> 2);
                    byte b = Saturate(((x * (bh - b0)) + (y * (bv - b0)) + (b0 * 4) + 2) >> 2);

                    tile[offset] = Pack(r, g, b);
                }
            }
        }

        private static void DecodeBlockETC1(
            Span<uint> tile,
            uint blockLow,
            uint blockHigh,
            uint r1,
            uint g1,
            uint b1,
            uint r2,
            uint g2,
            uint b2,
            uint alphaMask = 0)
        {
            int[] table1 = _etc1Lut[(blockLow >> 29) & 7];
            int[] table2 = _etc1Lut[(blockLow >> 26) & 7];

            bool flip = (blockLow & 0x1000000) != 0;

            if (!flip)
            {
                for (int y = 0; y < BlockHeight; y++)
                {
                    for (int x = 0; x < BlockWidth / 2; x++)
                    {
                        uint color1 = CalculatePixel(r1, g1, b1, x + 0, y, blockHigh, table1, alphaMask);
                        uint color2 = CalculatePixel(r2, g2, b2, x + 2, y, blockHigh, table2, alphaMask);

                        int offset1 = y * BlockWidth + x;
                        int offset2 = y * BlockWidth + x + 2;

                        tile[offset1] = color1;
                        tile[offset2] = color2;
                    }
                }
            }
            else
            {
                for (int y = 0; y < BlockHeight / 2; y++)
                {
                    for (int x = 0; x < BlockWidth; x++)
                    {
                        uint color1 = CalculatePixel(r1, g1, b1, x, y + 0, blockHigh, table1, alphaMask);
                        uint color2 = CalculatePixel(r2, g2, b2, x, y + 2, blockHigh, table2, alphaMask);

                        int offset1 = (y * BlockWidth) + x;
                        int offset2 = ((y + 2) * BlockWidth) + x;

                        tile[offset1] = color1;
                        tile[offset2] = color2;
                    }
                }
            }
        }

        private static uint CalculatePixel(uint r, uint g, uint b, int x, int y, uint block, int[] table, uint alphaMask)
        {
            int index = x * BlockHeight + y;
            uint msb = block << 1;
            uint tableIndex = index < 8
                ? ((block >> (index + 24)) & 1) + ((msb >> (index + 8)) & 2)
                : ((block >> (index + 8)) & 1) + ((msb >> (index - 8)) & 2);

            if (alphaMask != 0)
            {
                if (tableIndex == 0)
                {
                    return Pack(r, g, b) | alphaMask;
                }
                else if (tableIndex == 2)
                {
                    return 0;
                }
                else
                {
                    return Pack(r, g, b, table[tableIndex]) | alphaMask;
                }
            }

            return Pack(r, g, b, table[tableIndex]);
        }

        private static uint Pack(uint r, uint g, uint b, int offset)
        {
            r = Saturate((int)(r + offset));
            g = Saturate((int)(g + offset));
            b = Saturate((int)(b + offset));

            return Pack(r, g, b);
        }

        private static uint Pack(uint r, uint g, uint b)
        {
            return r | (g << 8) | (b << 16);
        }

        private static uint Pack4Be(uint r, uint g, uint b)
        {
            return (r << 8) | (g << 4) | b;
        }

        private static byte Saturate(int value)
        {
            return value > byte.MaxValue ? byte.MaxValue : value < byte.MinValue ? byte.MinValue : (byte)value;
        }

        private static int CalculateOutputSize(int width, int height, int depth, int levels, int layers)
        {
            int size = 0;

            for (int l = 0; l < levels; l++)
            {
                size += Math.Max(1, width >> l) * Math.Max(1, height >> l) * Math.Max(1, depth >> l) * layers * 4;
            }

            return size;
        }
    }
}
