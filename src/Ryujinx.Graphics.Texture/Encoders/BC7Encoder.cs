using Ryujinx.Graphics.Texture.Utils;
using System;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Threading.Tasks;

namespace Ryujinx.Graphics.Texture.Encoders
{
    static class BC7Encoder
    {
        private const int MinColorVarianceForModeChange = 160;

        public static void Encode(Memory<byte> outputStorage, ReadOnlyMemory<byte> data, int width, int height, EncodeMode mode)
        {
            int widthInBlocks = (width + 3) / 4;
            int heightInBlocks = (height + 3) / 4;

            bool fastMode = (mode & EncodeMode.ModeMask) == EncodeMode.Fast;

            if (mode.HasFlag(EncodeMode.Multithreaded))
            {
                Parallel.For(0, heightInBlocks, (yInBlocks) =>
                {
                    Span<ulong> output = MemoryMarshal.Cast<byte, ulong>(outputStorage.Span);
                    int y = yInBlocks * 4;

                    for (int xInBlocks = 0; xInBlocks < widthInBlocks; xInBlocks++)
                    {
                        int x = xInBlocks * 4;
                        Block block = CompressBlock(data.Span, x, y, width, height, fastMode);

                        int offset = (yInBlocks * widthInBlocks + xInBlocks) * 2;
                        output[offset] = block.Low;
                        output[offset + 1] = block.High;
                    }
                });
            }
            else
            {
                Span<ulong> output = MemoryMarshal.Cast<byte, ulong>(outputStorage.Span);
                int offset = 0;

                for (int y = 0; y < height; y += 4)
                {
                    for (int x = 0; x < width; x += 4)
                    {
                        Block block = CompressBlock(data.Span, x, y, width, height, fastMode);

                        output[offset++] = block.Low;
                        output[offset++] = block.High;
                    }
                }
            }
        }

        private static readonly int[] _mostFrequentPartitions =
        [
            0, 13, 2, 1, 15, 14, 10, 23
        ];

        private static Block CompressBlock(ReadOnlySpan<byte> data, int x, int y, int width, int height, bool fastMode)
        {
            int w = Math.Min(4, width - x);
            int h = Math.Min(4, height - y);

            ReadOnlySpan<uint> dataUint = MemoryMarshal.Cast<byte, uint>(data);

            int baseOffset = y * width + x;

            Span<uint> tile = stackalloc uint[w * h];

            for (int ty = 0; ty < h; ty++)
            {
                int rowOffset = baseOffset + ty * width;

                for (int tx = 0; tx < w; tx++)
                {
                    tile[ty * w + tx] = dataUint[rowOffset + tx];
                }
            }

            return fastMode ? EncodeFast(tile, w, h) : EncodeExhaustive(tile, w, h);
        }

        private static Block EncodeFast(ReadOnlySpan<uint> tile, int w, int h)
        {
            (RgbaColor8 minColor, RgbaColor8 maxColor) = BC67Utils.GetMinMaxColors(tile, w, h);

            bool alphaNotOne = minColor.A != 255 || maxColor.A != 255;
            int variance = BC67Utils.SquaredDifference(minColor.GetColor32(), maxColor.GetColor32());
            int selectedMode;
            int indexMode = 0;

            if (alphaNotOne)
            {
                bool constantAlpha = minColor.A == maxColor.A;
                if (constantAlpha)
                {
                    selectedMode = variance > MinColorVarianceForModeChange ? 7 : 6;
                }
                else
                {
                    if (variance > MinColorVarianceForModeChange)
                    {
                        Span<uint> uniqueRGB = stackalloc uint[16];
                        Span<uint> uniqueAlpha = stackalloc uint[16];

                        int uniqueRGBCount = 0;
                        int uniqueAlphaCount = 0;

                        uint rgbMask = new RgbaColor8(255, 255, 255, 0).ToUInt32();
                        uint alphaMask = new RgbaColor8(0, 0, 0, 255).ToUInt32();

                        for (int i = 0; i < tile.Length; i++)
                        {
                            uint c = tile[i];

                            if (!uniqueRGB[..uniqueRGBCount].Contains(c & rgbMask))
                            {
                                uniqueRGB[uniqueRGBCount++] = c & rgbMask;
                            }

                            if (!uniqueAlpha[..uniqueAlphaCount].Contains(c & alphaMask))
                            {
                                uniqueAlpha[uniqueAlphaCount++] = c & alphaMask;
                            }
                        }

                        selectedMode = 4;
                        indexMode = uniqueRGBCount > uniqueAlphaCount ? 1 : 0;
                    }
                    else
                    {
                        selectedMode = 5;
                    }
                }
            }
            else
            {
                if (variance > MinColorVarianceForModeChange)
                {
                    selectedMode = 1;
                }
                else
                {
                    selectedMode = 6;
                }
            }

            int selectedPartition = 0;

            if (selectedMode == 1 || selectedMode == 7)
            {
                int partitionSelectionLowestError = int.MaxValue;

                for (int i = 0; i < _mostFrequentPartitions.Length; i++)
                {
                    int p = _mostFrequentPartitions[i];
                    int error = GetEndPointSelectionErrorFast(tile, 2, p, w, h, partitionSelectionLowestError);
                    if (error < partitionSelectionLowestError)
                    {
                        partitionSelectionLowestError = error;
                        selectedPartition = p;
                    }
                }
            }

            return Encode(selectedMode, selectedPartition, 0, indexMode, fastMode: true, tile, w, h, out _);
        }

        private static Block EncodeExhaustive(ReadOnlySpan<uint> tile, int w, int h)
        {
            Block bestBlock = default;
            int lowestError = int.MaxValue;
            int lowestErrorSubsets = int.MaxValue;

            for (int m = 0; m < 8; m++)
            {
                for (int r = 0; r < (m == 4 || m == 5 ? 4 : 1); r++)
                {
                    for (int im = 0; im < (m == 4 ? 2 : 1); im++)
                    {
                        for (int p = 0; p < 1 << BC67Tables.BC7ModeInfos[m].PartitionBitCount; p++)
                        {
                            Block block = Encode(m, p, r, im, fastMode: false, tile, w, h, out int maxError);
                            if (maxError < lowestError || (maxError == lowestError && BC67Tables.BC7ModeInfos[m].SubsetCount < lowestErrorSubsets))
                            {
                                lowestError = maxError;
                                lowestErrorSubsets = BC67Tables.BC7ModeInfos[m].SubsetCount;
                                bestBlock = block;
                            }
                        }
                    }
                }
            }

            return bestBlock;
        }

        private static Block Encode(
            int mode,
            int partition,
            int rotation,
            int indexMode,
            bool fastMode,
            ReadOnlySpan<uint> tile,
            int w,
            int h,
            out int errorSum)
        {
            BC7ModeInfo modeInfo = BC67Tables.BC7ModeInfos[mode];
            int subsetCount = modeInfo.SubsetCount;
            int partitionBitCount = modeInfo.PartitionBitCount;
            int rotationBitCount = modeInfo.RotationBitCount;
            int indexModeBitCount = modeInfo.IndexModeBitCount;
            int colorDepth = modeInfo.ColorDepth;
            int alphaDepth = modeInfo.AlphaDepth;
            int pBits = modeInfo.PBits;
            int colorIndexBitCount = modeInfo.ColorIndexBitCount;
            int alphaIndexBitCount = modeInfo.AlphaIndexBitCount;
            bool separateAlphaIndices = alphaIndexBitCount != 0;

            uint alphaMask;

            if (separateAlphaIndices)
            {
                alphaMask = rotation switch
                {
                    1 => new RgbaColor8(255, 0, 0, 0).ToUInt32(),
                    2 => new RgbaColor8(0, 255, 0, 0).ToUInt32(),
                    3 => new RgbaColor8(0, 0, 255, 0).ToUInt32(),
                    _ => new RgbaColor8(0, 0, 0, 255).ToUInt32(),
                };
            }
            else
            {
                alphaMask = new RgbaColor8(0, 0, 0, 0).ToUInt32();
            }

            if (indexMode != 0)
            {
                alphaMask = ~alphaMask;
            }

            //
            // Select color palette.
            //

            Span<uint> endPoints0 = stackalloc uint[subsetCount];
            Span<uint> endPoints1 = stackalloc uint[subsetCount];

            SelectEndPoints(
                tile,
                w,
                h,
                endPoints0,
                endPoints1,
                subsetCount,
                partition,
                colorIndexBitCount,
                colorDepth,
                alphaDepth,
                ~alphaMask,
                fastMode);

            if (separateAlphaIndices)
            {
                SelectEndPoints(
                    tile,
                    w,
                    h,
                    endPoints0,
                    endPoints1,
                    subsetCount,
                    partition,
                    alphaIndexBitCount,
                    colorDepth,
                    alphaDepth,
                    alphaMask,
                    fastMode);
            }

            Span<int> pBitValues = stackalloc int[pBits];

            for (int i = 0; i < pBits; i++)
            {
                int pBit;

                if (pBits == subsetCount)
                {
                    pBit = GetPBit(endPoints0[i], endPoints1[i], colorDepth, alphaDepth);
                }
                else
                {
                    int subset = i >> 1;
                    uint color = (i & 1) == 0 ? endPoints0[subset] : endPoints1[subset];
                    pBit = GetPBit(color, colorDepth, alphaDepth);
                }

                pBitValues[i] = pBit;
            }

            int colorIndexCount = 1 << colorIndexBitCount;
            int alphaIndexCount = 1 << alphaIndexBitCount;

            Span<byte> colorIndices = stackalloc byte[16];
            Span<byte> alphaIndices = stackalloc byte[16];

            errorSum = BC67Utils.SelectIndices(
                tile,
                w,
                h,
                endPoints0,
                endPoints1,
                pBitValues,
                colorIndices,
                subsetCount,
                partition,
                colorIndexBitCount,
                colorIndexCount,
                colorDepth,
                alphaDepth,
                pBits,
                alphaMask);

            if (separateAlphaIndices)
            {
                errorSum += BC67Utils.SelectIndices(
                    tile,
                    w,
                    h,
                    endPoints0,
                    endPoints1,
                    pBitValues,
                    alphaIndices,
                    subsetCount,
                    partition,
                    alphaIndexBitCount,
                    alphaIndexCount,
                    colorDepth,
                    alphaDepth,
                    pBits,
                    ~alphaMask);
            }

            Span<bool> colorSwapSubset = stackalloc bool[3];

            for (int i = 0; i < 3; i++)
            {
                colorSwapSubset[i] = colorIndices[BC67Tables.FixUpIndices[subsetCount - 1][partition][i]] >= (colorIndexCount >> 1);
            }

            bool alphaSwapSubset = alphaIndices[0] >= (alphaIndexCount >> 1);

            Block block = new();

            int offset = 0;

            block.Encode(1UL << mode, ref offset, mode + 1);
            block.Encode((ulong)partition, ref offset, partitionBitCount);
            block.Encode((ulong)rotation, ref offset, rotationBitCount);
            block.Encode((ulong)indexMode, ref offset, indexModeBitCount);

            for (int comp = 0; comp < 3; comp++)
            {
                int rotatedComp = comp;

                if (((comp + 1) & 3) == rotation)
                {
                    rotatedComp = 3;
                }

                for (int subset = 0; subset < subsetCount; subset++)
                {
                    RgbaColor8 color0 = RgbaColor8.FromUInt32(endPoints0[subset]);
                    RgbaColor8 color1 = RgbaColor8.FromUInt32(endPoints1[subset]);

                    int pBit0 = -1, pBit1 = -1;

                    if (pBits == subsetCount)
                    {
                        pBit0 = pBit1 = pBitValues[subset];
                    }
                    else if (pBits != 0)
                    {
                        pBit0 = pBitValues[subset * 2];
                        pBit1 = pBitValues[subset * 2 + 1];
                    }

                    if (indexMode == 0 ? colorSwapSubset[subset] : alphaSwapSubset)
                    {
                        block.Encode(BC67Utils.QuantizeComponent(color1.GetComponent(rotatedComp), colorDepth, pBit1), ref offset, colorDepth);
                        block.Encode(BC67Utils.QuantizeComponent(color0.GetComponent(rotatedComp), colorDepth, pBit0), ref offset, colorDepth);
                    }
                    else
                    {
                        block.Encode(BC67Utils.QuantizeComponent(color0.GetComponent(rotatedComp), colorDepth, pBit0), ref offset, colorDepth);
                        block.Encode(BC67Utils.QuantizeComponent(color1.GetComponent(rotatedComp), colorDepth, pBit1), ref offset, colorDepth);
                    }
                }
            }

            if (alphaDepth != 0)
            {
                int rotatedComp = (rotation - 1) & 3;

                for (int subset = 0; subset < subsetCount; subset++)
                {
                    RgbaColor8 color0 = RgbaColor8.FromUInt32(endPoints0[subset]);
                    RgbaColor8 color1 = RgbaColor8.FromUInt32(endPoints1[subset]);

                    int pBit0 = -1, pBit1 = -1;

                    if (pBits == subsetCount)
                    {
                        pBit0 = pBit1 = pBitValues[subset];
                    }
                    else if (pBits != 0)
                    {
                        pBit0 = pBitValues[subset * 2];
                        pBit1 = pBitValues[subset * 2 + 1];
                    }

                    if (separateAlphaIndices && indexMode == 0 ? alphaSwapSubset : colorSwapSubset[subset])
                    {
                        block.Encode(BC67Utils.QuantizeComponent(color1.GetComponent(rotatedComp), alphaDepth, pBit1), ref offset, alphaDepth);
                        block.Encode(BC67Utils.QuantizeComponent(color0.GetComponent(rotatedComp), alphaDepth, pBit0), ref offset, alphaDepth);
                    }
                    else
                    {
                        block.Encode(BC67Utils.QuantizeComponent(color0.GetComponent(rotatedComp), alphaDepth, pBit0), ref offset, alphaDepth);
                        block.Encode(BC67Utils.QuantizeComponent(color1.GetComponent(rotatedComp), alphaDepth, pBit1), ref offset, alphaDepth);
                    }
                }
            }

            for (int i = 0; i < pBits; i++)
            {
                block.Encode((ulong)pBitValues[i], ref offset, 1);
            }

            byte[] fixUpTable = BC67Tables.FixUpIndices[subsetCount - 1][partition];

            for (int i = 0; i < 16; i++)
            {
                int subset = BC67Tables.PartitionTable[subsetCount - 1][partition][i];
                byte index = colorIndices[i];

                if (colorSwapSubset[subset])
                {
                    index = (byte)(index ^ (colorIndexCount - 1));
                }

                int finalIndexBitCount = i == fixUpTable[subset] ? colorIndexBitCount - 1 : colorIndexBitCount;

                Debug.Assert(index < (1 << finalIndexBitCount));

                block.Encode(index, ref offset, finalIndexBitCount);
            }

            if (separateAlphaIndices)
            {
                for (int i = 0; i < 16; i++)
                {
                    byte index = alphaIndices[i];

                    if (alphaSwapSubset)
                    {
                        index = (byte)(index ^ (alphaIndexCount - 1));
                    }

                    int finalIndexBitCount = i == 0 ? alphaIndexBitCount - 1 : alphaIndexBitCount;

                    Debug.Assert(index < (1 << finalIndexBitCount));

                    block.Encode(index, ref offset, finalIndexBitCount);
                }
            }

            return block;
        }

        private static unsafe int GetEndPointSelectionErrorFast(ReadOnlySpan<uint> tile, int subsetCount, int partition, int w, int h, int maxError)
        {
            byte[] partitionTable = BC67Tables.PartitionTable[subsetCount - 1][partition];

            Span<RgbaColor8> minColors = stackalloc RgbaColor8[subsetCount];
            Span<RgbaColor8> maxColors = stackalloc RgbaColor8[subsetCount];

            BC67Utils.GetMinMaxColors(partitionTable, tile, w, h, minColors, maxColors, subsetCount);

            Span<uint> endPoints0 = stackalloc uint[subsetCount];
            Span<uint> endPoints1 = stackalloc uint[subsetCount];

            SelectEndPointsFast(partitionTable, tile, w, h, subsetCount, minColors, maxColors, endPoints0, endPoints1, uint.MaxValue);

            Span<RgbaColor32> palette = stackalloc RgbaColor32[8];

            int errorSum = 0;

            for (int subset = 0; subset < subsetCount; subset++)
            {
                RgbaColor32 blockDir = maxColors[subset].GetColor32() - minColors[subset].GetColor32();
                int sum = blockDir.R + blockDir.G + blockDir.B + blockDir.A;
                if (sum != 0)
                {
                    blockDir = (blockDir << 6) / new RgbaColor32(sum);
                }

                uint c0 = endPoints0[subset];
                uint c1 = endPoints1[subset];

                int pBit0 = GetPBit(c0, 6, 0);
                int pBit1 = GetPBit(c1, 6, 0);

                c0 = BC67Utils.Quantize(RgbaColor8.FromUInt32(c0), 6, 0, pBit0).ToUInt32();
                c1 = BC67Utils.Quantize(RgbaColor8.FromUInt32(c1), 6, 0, pBit1).ToUInt32();

                if (Sse41.IsSupported)
                {
                    Vector128<byte> c0Rep = Vector128.Create(c0).AsByte();
                    Vector128<byte> c1Rep = Vector128.Create(c1).AsByte();

                    Vector128<byte> c0c1 = Sse2.UnpackLow(c0Rep, c1Rep);

                    Vector128<byte> rWeights;
                    Vector128<byte> lWeights;

                    fixed (byte* pWeights = BC67Tables.Weights[1], pInvWeights = BC67Tables.InverseWeights[1])
                    {
                        rWeights = Sse2.LoadScalarVector128((ulong*)pWeights).AsByte();
                        lWeights = Sse2.LoadScalarVector128((ulong*)pInvWeights).AsByte();
                    }

                    Vector128<byte> iWeights = Sse2.UnpackLow(rWeights, lWeights);
                    Vector128<byte> iWeights01 = Sse2.UnpackLow(iWeights.AsInt16(), iWeights.AsInt16()).AsByte();
                    Vector128<byte> iWeights23 = Sse2.UnpackHigh(iWeights.AsInt16(), iWeights.AsInt16()).AsByte();
                    Vector128<byte> iWeights0 = Sse2.UnpackLow(iWeights01.AsInt16(), iWeights01.AsInt16()).AsByte();
                    Vector128<byte> iWeights1 = Sse2.UnpackHigh(iWeights01.AsInt16(), iWeights01.AsInt16()).AsByte();
                    Vector128<byte> iWeights2 = Sse2.UnpackLow(iWeights23.AsInt16(), iWeights23.AsInt16()).AsByte();
                    Vector128<byte> iWeights3 = Sse2.UnpackHigh(iWeights23.AsInt16(), iWeights23.AsInt16()).AsByte();

                    static Vector128<short> ShiftRoundToNearest(Vector128<short> x)
                    {
                        return Sse2.ShiftRightLogical(Sse2.Add(x, Vector128.Create((short)32)), 6);
                    }

                    Vector128<short> pal0 = ShiftRoundToNearest(Ssse3.MultiplyAddAdjacent(c0c1, iWeights0.AsSByte()));
                    Vector128<short> pal1 = ShiftRoundToNearest(Ssse3.MultiplyAddAdjacent(c0c1, iWeights1.AsSByte()));
                    Vector128<short> pal2 = ShiftRoundToNearest(Ssse3.MultiplyAddAdjacent(c0c1, iWeights2.AsSByte()));
                    Vector128<short> pal3 = ShiftRoundToNearest(Ssse3.MultiplyAddAdjacent(c0c1, iWeights3.AsSByte()));

                    for (int i = 0; i < tile.Length; i++)
                    {
                        if (partitionTable[i] != subset)
                        {
                            continue;
                        }

                        uint c = tile[i];

                        Vector128<short> color = Sse41.ConvertToVector128Int16(Vector128.Create(c).AsByte());

                        Vector128<short> delta0 = Sse2.Subtract(color, pal0);
                        Vector128<short> delta1 = Sse2.Subtract(color, pal1);
                        Vector128<short> delta2 = Sse2.Subtract(color, pal2);
                        Vector128<short> delta3 = Sse2.Subtract(color, pal3);

                        Vector128<int> deltaSum0 = Sse2.MultiplyAddAdjacent(delta0, delta0);
                        Vector128<int> deltaSum1 = Sse2.MultiplyAddAdjacent(delta1, delta1);
                        Vector128<int> deltaSum2 = Sse2.MultiplyAddAdjacent(delta2, delta2);
                        Vector128<int> deltaSum3 = Sse2.MultiplyAddAdjacent(delta3, delta3);

                        Vector128<int> deltaSum01 = Ssse3.HorizontalAdd(deltaSum0, deltaSum1);
                        Vector128<int> deltaSum23 = Ssse3.HorizontalAdd(deltaSum2, deltaSum3);

                        Vector128<ushort> delta = Sse41.PackUnsignedSaturate(deltaSum01, deltaSum23);

                        Vector128<ushort> min = Sse41.MinHorizontal(delta);

                        errorSum += min.GetElement(0);
                    }
                }
                else
                {
                    RgbaColor32 e032 = RgbaColor8.FromUInt32(c0).GetColor32();
                    RgbaColor32 e132 = RgbaColor8.FromUInt32(c1).GetColor32();

                    palette[0] = e032;
                    palette[^1] = e132;

                    for (int i = 1; i < palette.Length - 1; i++)
                    {
                        palette[i] = BC67Utils.Interpolate(e032, e132, i, 3);
                    }

                    for (int i = 0; i < tile.Length; i++)
                    {
                        if (partitionTable[i] != subset)
                        {
                            continue;
                        }

                        uint c = tile[i];
                        RgbaColor32 color = Unsafe.As<uint, RgbaColor8>(ref c).GetColor32();

                        int bestMatchScore = int.MaxValue;

                        for (int j = 0; j < palette.Length; j++)
                        {
                            int score = BC67Utils.SquaredDifference(color, palette[j]);

                            if (score < bestMatchScore)
                            {
                                bestMatchScore = score;
                            }
                        }

                        errorSum += bestMatchScore;
                    }
                }

                // No point in continuing if we are already above maximum.
                if (errorSum >= maxError)
                {
                    return int.MaxValue;
                }
            }

            return errorSum;
        }

        private static void SelectEndPoints(
            ReadOnlySpan<uint> tile,
            int w,
            int h,
            Span<uint> endPoints0,
            Span<uint> endPoints1,
            int subsetCount,
            int partition,
            int indexBitCount,
            int colorDepth,
            int alphaDepth,
            uint writeMask,
            bool fastMode)
        {
            byte[] partitionTable = BC67Tables.PartitionTable[subsetCount - 1][partition];

            Span<RgbaColor8> minColors = stackalloc RgbaColor8[subsetCount];
            Span<RgbaColor8> maxColors = stackalloc RgbaColor8[subsetCount];

            BC67Utils.GetMinMaxColors(partitionTable, tile, w, h, minColors, maxColors, subsetCount);

            uint inverseMask = ~writeMask;

            for (int i = 0; i < subsetCount; i++)
            {
                Unsafe.As<RgbaColor8, uint>(ref minColors[i]) |= inverseMask;
                Unsafe.As<RgbaColor8, uint>(ref maxColors[i]) |= inverseMask;
            }

            if (fastMode)
            {
                SelectEndPointsFast(partitionTable, tile, w, h, subsetCount, minColors, maxColors, endPoints0, endPoints1, writeMask);
            }
            else
            {
                Span<RgbaColor8> colors = stackalloc RgbaColor8[subsetCount * 16];
                Span<byte> counts = stackalloc byte[subsetCount];

                int i = 0;
                for (int ty = 0; ty < h; ty++)
                {
                    for (int tx = 0; tx < w; tx++)
                    {
                        int subset = partitionTable[ty * 4 + tx];
                        RgbaColor8 color = RgbaColor8.FromUInt32(tile[i++] | inverseMask);

                        static void AddIfNew(Span<RgbaColor8> values, RgbaColor8 value, int subset, ref byte count)
                        {
                            for (int i = 0; i < count; i++)
                            {
                                if (values[subset * 16 + i] == value)
                                {
                                    return;
                                }
                            }

                            values[subset * 16 + count++] = value;
                        }

                        AddIfNew(colors, color, subset, ref counts[subset]);
                    }
                }

                for (int subset = 0; subset < subsetCount; subset++)
                {
                    int offset = subset * 16;

                    RgbaColor8 minColor = minColors[subset];
                    RgbaColor8 maxColor = maxColors[subset];

                    ReadOnlySpan<RgbaColor8> subsetColors = colors.Slice(offset, counts[subset]);

                    (RgbaColor8 e0, RgbaColor8 e1) = SelectEndPoints(subsetColors, minColor, maxColor, indexBitCount, colorDepth, alphaDepth, inverseMask);

                    endPoints0[subset] = (endPoints0[subset] & inverseMask) | (e0.ToUInt32() & writeMask);
                    endPoints1[subset] = (endPoints1[subset] & inverseMask) | (e1.ToUInt32() & writeMask);
                }
            }
        }

        private static unsafe void SelectEndPointsFast(
            ReadOnlySpan<byte> partitionTable,
            ReadOnlySpan<uint> tile,
            int w,
            int h,
            int subsetCount,
            ReadOnlySpan<RgbaColor8> minColors,
            ReadOnlySpan<RgbaColor8> maxColors,
            Span<uint> endPoints0,
            Span<uint> endPoints1,
            uint writeMask)
        {
            uint inverseMask = ~writeMask;

            if (Sse41.IsSupported && w == 4 && h == 4)
            {
                Vector128<byte> row0, row1, row2, row3;
                Vector128<short> ones = Vector128<short>.AllBitsSet;

                fixed (uint* pTile = tile)
                {
                    row0 = Sse2.LoadVector128(pTile).AsByte();
                    row1 = Sse2.LoadVector128(pTile + 4).AsByte();
                    row2 = Sse2.LoadVector128(pTile + 8).AsByte();
                    row3 = Sse2.LoadVector128(pTile + 12).AsByte();
                }

                Vector128<byte> partitionMask;

                fixed (byte* pPartitionTable = partitionTable)
                {
                    partitionMask = Sse2.LoadVector128(pPartitionTable);
                }

                for (int subset = 0; subset < subsetCount; subset++)
                {
                    RgbaColor32 blockDir = maxColors[subset].GetColor32() - minColors[subset].GetColor32();
                    int sum = blockDir.R + blockDir.G + blockDir.B + blockDir.A;
                    if (sum != 0)
                    {
                        blockDir = (blockDir << 6) / new RgbaColor32(sum);
                    }

                    Vector128<byte> bd = Vector128.Create(blockDir.GetColor8().ToUInt32()).AsByte();

                    Vector128<short> delta0 = Ssse3.MultiplyAddAdjacent(row0, bd.AsSByte());
                    Vector128<short> delta1 = Ssse3.MultiplyAddAdjacent(row1, bd.AsSByte());
                    Vector128<short> delta2 = Ssse3.MultiplyAddAdjacent(row2, bd.AsSByte());
                    Vector128<short> delta3 = Ssse3.MultiplyAddAdjacent(row3, bd.AsSByte());

                    Vector128<short> delta01 = Ssse3.HorizontalAdd(delta0, delta1);
                    Vector128<short> delta23 = Ssse3.HorizontalAdd(delta2, delta3);

                    Vector128<byte> subsetMask = Sse2.Xor(Sse2.CompareEqual(partitionMask, Vector128.Create((byte)subset)), ones.AsByte());

                    Vector128<short> subsetMask01 = Sse2.UnpackLow(subsetMask, subsetMask).AsInt16();
                    Vector128<short> subsetMask23 = Sse2.UnpackHigh(subsetMask, subsetMask).AsInt16();

                    Vector128<ushort> min01 = Sse41.MinHorizontal(Sse2.Or(delta01, subsetMask01).AsUInt16());
                    Vector128<ushort> min23 = Sse41.MinHorizontal(Sse2.Or(delta23, subsetMask23).AsUInt16());
                    Vector128<ushort> max01 = Sse41.MinHorizontal(Sse2.Xor(Sse2.AndNot(subsetMask01, delta01), ones).AsUInt16());
                    Vector128<ushort> max23 = Sse41.MinHorizontal(Sse2.Xor(Sse2.AndNot(subsetMask23, delta23), ones).AsUInt16());

                    uint minPos01 = min01.AsUInt32().GetElement(0);
                    uint minPos23 = min23.AsUInt32().GetElement(0);
                    uint maxPos01 = max01.AsUInt32().GetElement(0);
                    uint maxPos23 = max23.AsUInt32().GetElement(0);

                    uint minDistColor = (ushort)minPos23 < (ushort)minPos01
                        ? tile[(int)(minPos23 >> 16) + 8]
                        : tile[(int)(minPos01 >> 16)];

                    // Note that we calculate the maximum as the minimum of the inverse, so less here is actually greater.
                    uint maxDistColor = (ushort)maxPos23 < (ushort)maxPos01
                        ? tile[(int)(maxPos23 >> 16) + 8]
                        : tile[(int)(maxPos01 >> 16)];

                    endPoints0[subset] = (endPoints0[subset] & inverseMask) | (minDistColor & writeMask);
                    endPoints1[subset] = (endPoints1[subset] & inverseMask) | (maxDistColor & writeMask);
                }
            }
            else
            {
                for (int subset = 0; subset < subsetCount; subset++)
                {
                    RgbaColor32 blockDir = maxColors[subset].GetColor32() - minColors[subset].GetColor32();
                    blockDir = RgbaColor32.DivideGuarded(blockDir << 6, new RgbaColor32(blockDir.R + blockDir.G + blockDir.B + blockDir.A), 0);

                    int minDist = int.MaxValue;
                    int maxDist = int.MinValue;

                    RgbaColor8 minDistColor = default;
                    RgbaColor8 maxDistColor = default;

                    int i = 0;
                    for (int ty = 0; ty < h; ty++)
                    {
                        for (int tx = 0; tx < w; tx++, i++)
                        {
                            if (partitionTable[ty * 4 + tx] != subset)
                            {
                                continue;
                            }

                            RgbaColor8 color = RgbaColor8.FromUInt32(tile[i]);
                            int dist = RgbaColor32.Dot(color.GetColor32(), blockDir);

                            if (minDist > dist)
                            {
                                minDist = dist;
                                minDistColor = color;
                            }

                            if (maxDist < dist)
                            {
                                maxDist = dist;
                                maxDistColor = color;
                            }
                        }
                    }

                    endPoints0[subset] = (endPoints0[subset] & inverseMask) | (minDistColor.ToUInt32() & writeMask);
                    endPoints1[subset] = (endPoints1[subset] & inverseMask) | (maxDistColor.ToUInt32() & writeMask);
                }
            }
        }

        private static (RgbaColor8, RgbaColor8) SelectEndPoints(
            ReadOnlySpan<RgbaColor8> values,
            RgbaColor8 minValue,
            RgbaColor8 maxValue,
            int indexBitCount,
            int colorDepth,
            int alphaDepth,
            uint alphaMask)
        {
            int n = values.Length;
            int numInterpolatedColors = 1 << indexBitCount;
            int numInterpolatedColorsMinus1 = numInterpolatedColors - 1;

            if (n == 0)
            {
                return (default, default);
            }

            minValue = BC67Utils.Quantize(minValue, colorDepth, alphaDepth);
            maxValue = BC67Utils.Quantize(maxValue, colorDepth, alphaDepth);

            RgbaColor32 blockDir = maxValue.GetColor32() - minValue.GetColor32();
            blockDir = RgbaColor32.DivideGuarded(blockDir << 6, new RgbaColor32(blockDir.R + blockDir.G + blockDir.B + blockDir.A), 0);

            int minDist = int.MaxValue;
            int maxDist = 0;

            for (int i = 0; i < values.Length; i++)
            {
                RgbaColor8 color = values[i];
                int dist = RgbaColor32.Dot(BC67Utils.Quantize(color, colorDepth, alphaDepth).GetColor32(), blockDir);

                if (minDist >= dist)
                {
                    minDist = dist;
                }

                if (maxDist <= dist)
                {
                    maxDist = dist;
                }
            }

            Span<RgbaColor8> palette = stackalloc RgbaColor8[numInterpolatedColors];

            int distRange = Math.Max(1, maxDist - minDist);

            RgbaColor32 nV = new(n);

            int bestErrorSum = int.MaxValue;
            RgbaColor8 bestE0 = default;
            RgbaColor8 bestE1 = default;

            Span<int> indices = stackalloc int[n];
            Span<RgbaColor32> colors = stackalloc RgbaColor32[n];

            for (int maxIndex = numInterpolatedColorsMinus1; maxIndex >= 1; maxIndex--)
            {
                int sumX = 0;
                int sumXX = 0;
                int sumXXIncrement = 0;

                for (int i = 0; i < values.Length; i++)
                {
                    RgbaColor32 color = values[i].GetColor32();

                    int dist = RgbaColor32.Dot(color, blockDir);

                    int normalizedValue = ((dist - minDist) << 6) / distRange;
                    int texelIndex = (normalizedValue * maxIndex + 32) >> 6;

                    indices[i] = texelIndex;
                    colors[i] = color;

                    sumX += texelIndex;
                    sumXX += texelIndex * texelIndex;
                    sumXXIncrement += 1 + texelIndex * 2;
                }

                for (int start = 0; start < numInterpolatedColors - maxIndex; start++)
                {
                    RgbaColor32 sumY = new(0);
                    RgbaColor32 sumXY = new(0);

                    for (int i = 0; i < indices.Length; i++)
                    {
                        RgbaColor32 y = colors[i];

                        sumY += y;
                        sumXY += new RgbaColor32(start + indices[i]) * y;
                    }

                    RgbaColor32 sumXV = new(sumX);
                    RgbaColor32 sumXXV = new(sumXX);
                    RgbaColor32 m = RgbaColor32.DivideGuarded((nV * sumXY - sumXV * sumY) << 6, nV * sumXXV - sumXV * sumXV, 0);
                    RgbaColor32 b = ((sumY << 6) - m * sumXV) / nV;

                    RgbaColor8 candidateE0 = (b >> 6).GetColor8();
                    RgbaColor8 candidateE1 = ((b + m * new RgbaColor32(numInterpolatedColorsMinus1)) >> 6).GetColor8();

                    int pBit0 = GetPBit(candidateE0.ToUInt32(), colorDepth, alphaDepth);
                    int pBit1 = GetPBit(candidateE1.ToUInt32(), colorDepth, alphaDepth);

                    int errorSum = BC67Utils.SelectIndices(
                        MemoryMarshal.Cast<RgbaColor8, uint>(values),
                        candidateE0.ToUInt32(),
                        candidateE1.ToUInt32(),
                        pBit0,
                        pBit1,
                        indexBitCount,
                        numInterpolatedColors,
                        colorDepth,
                        alphaDepth,
                        alphaMask);

                    if (errorSum <= bestErrorSum)
                    {
                        bestErrorSum = errorSum;
                        bestE0 = candidateE0;
                        bestE1 = candidateE1;
                    }

                    sumX += n;
                    sumXX += sumXXIncrement;
                    sumXXIncrement += 2 * n;
                }
            }

            return (bestE0, bestE1);
        }

        private static int GetPBit(uint color, int colorDepth, int alphaDepth)
        {
            uint mask = 0x808080u >> colorDepth;

            if (alphaDepth != 0)
            {
                // If alpha is 0, let's assume the color information is not too important and prefer
                // to preserve alpha instead.
                if ((color >> 24) == 0)
                {
                    return 0;
                }

                mask |= 0x80000000u >> alphaDepth;
            }

            color &= 0x7f7f7f7fu;
            color += mask >> 1;

            int onesCount = BitOperations.PopCount(color & mask);
            return onesCount >= 2 ? 1 : 0;
        }

        private static int GetPBit(uint c0, uint c1, int colorDepth, int alphaDepth)
        {
            // Giving preference to the first endpoint yields better results,
            // might be a side effect of the endpoint selection algorithm?
            return GetPBit(c0, colorDepth, alphaDepth);
        }
    }
}
