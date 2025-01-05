using Ryujinx.Common.Memory;
using Ryujinx.Common.Utilities;
using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Ryujinx.Graphics.Texture.Astc
{
    // https://github.com/GammaUNC/FasTC/blob/master/ASTCEncoder/src/Decompressor.cpp
    public class AstcDecoder
    {
        private ReadOnlyMemory<byte> InputBuffer { get; }
        private Memory<byte> OutputBuffer { get; }

        private int BlockSizeX { get; }
        private int BlockSizeY { get; }

        private AstcLevel[] Levels { get; }

        private bool Success { get; set; }

        public int TotalBlockCount { get; }

        public AstcDecoder(
            ReadOnlyMemory<byte> inputBuffer,
            Memory<byte> outputBuffer,
            int blockWidth,
            int blockHeight,
            int width,
            int height,
            int depth,
            int levels,
            int layers)
        {
            if ((uint)blockWidth > 12)
            {
                throw new ArgumentOutOfRangeException(nameof(blockWidth));
            }

            if ((uint)blockHeight > 12)
            {
                throw new ArgumentOutOfRangeException(nameof(blockHeight));
            }

            InputBuffer = inputBuffer;
            OutputBuffer = outputBuffer;

            BlockSizeX = blockWidth;
            BlockSizeY = blockHeight;

            Levels = new AstcLevel[levels * layers];

            Success = true;

            TotalBlockCount = 0;

            int currentInputBlock = 0;
            int currentOutputOffset = 0;

            for (int i = 0; i < levels; i++)
            {
                for (int j = 0; j < layers; j++)
                {
                    ref AstcLevel level = ref Levels[i * layers + j];

                    level.ImageSizeX = Math.Max(1, width >> i);
                    level.ImageSizeY = Math.Max(1, height >> i);
                    level.ImageSizeZ = Math.Max(1, depth >> i);

                    level.BlockCountX = (level.ImageSizeX + blockWidth - 1) / blockWidth;
                    level.BlockCountY = (level.ImageSizeY + blockHeight - 1) / blockHeight;

                    level.StartBlock = currentInputBlock;
                    level.OutputByteOffset = currentOutputOffset;

                    currentInputBlock += level.TotalBlockCount;
                    currentOutputOffset += level.PixelCount * 4;
                }
            }

            TotalBlockCount = currentInputBlock;
        }

        private struct AstcLevel
        {
            public int ImageSizeX { get; set; }
            public int ImageSizeY { get; set; }
            public int ImageSizeZ { get; set; }

            public int BlockCountX { get; set; }
            public int BlockCountY { get; set; }

            public int StartBlock { get; set; }
            public int OutputByteOffset { get; set; }

            public readonly int TotalBlockCount => BlockCountX * BlockCountY * ImageSizeZ;
            public readonly int PixelCount => ImageSizeX * ImageSizeY * ImageSizeZ;
        }

        public static int QueryDecompressedSize(int sizeX, int sizeY, int sizeZ, int levelCount, int layerCount)
        {
            int size = 0;

            for (int i = 0; i < levelCount; i++)
            {
                int levelSizeX = Math.Max(1, sizeX >> i);
                int levelSizeY = Math.Max(1, sizeY >> i);
                int levelSizeZ = Math.Max(1, sizeZ >> i);

                size += levelSizeX * levelSizeY * levelSizeZ * layerCount;
            }

            return size * 4;
        }

        public void ProcessBlock(int index)
        {
            Buffer16 inputBlock = MemoryMarshal.Cast<byte, Buffer16>(InputBuffer.Span)[index];

            Span<int> decompressedData = stackalloc int[144];

            try
            {
                DecompressBlock(inputBlock, decompressedData, BlockSizeX, BlockSizeY);
            }
            catch (Exception)
            {
                Success = false;
            }

            Span<byte> decompressedBytes = MemoryMarshal.Cast<int, byte>(decompressedData);

            AstcLevel levelInfo = GetLevelInfo(index);

            WriteDecompressedBlock(decompressedBytes, OutputBuffer.Span[levelInfo.OutputByteOffset..],
                index - levelInfo.StartBlock, levelInfo);
        }

        private AstcLevel GetLevelInfo(int blockIndex)
        {
            foreach (AstcLevel levelInfo in Levels)
            {
                if (blockIndex < levelInfo.StartBlock + levelInfo.TotalBlockCount)
                {
                    return levelInfo;
                }
            }

            throw new AstcDecoderException("Invalid block index.");
        }

        private void WriteDecompressedBlock(ReadOnlySpan<byte> block, Span<byte> outputBuffer, int blockIndex, AstcLevel level)
        {
            int stride = level.ImageSizeX * 4;

            int blockCordX = blockIndex % level.BlockCountX;
            int blockCordY = blockIndex / level.BlockCountX;

            int pixelCordX = blockCordX * BlockSizeX;
            int pixelCordY = blockCordY * BlockSizeY;

            int outputPixelsX = Math.Min(pixelCordX + BlockSizeX, level.ImageSizeX) - pixelCordX;
            int outputPixelsY = Math.Min(pixelCordY + BlockSizeY, level.ImageSizeY * level.ImageSizeZ) - pixelCordY;

            int outputStart = pixelCordX * 4 + pixelCordY * stride;
            int outputOffset = outputStart;

            int inputOffset = 0;

            for (int i = 0; i < outputPixelsY; i++)
            {
                ReadOnlySpan<byte> blockRow = block.Slice(inputOffset, outputPixelsX * 4);
                Span<byte> outputRow = outputBuffer[outputOffset..];
                blockRow.CopyTo(outputRow);

                inputOffset += BlockSizeX * 4;
                outputOffset += stride;
            }
        }

        struct TexelWeightParams
        {
            public int Width;
            public int Height;
            public int MaxWeight;
            public bool DualPlane;
            public bool Error;
            public bool VoidExtentLdr;
            public bool VoidExtentHdr;

            public readonly int GetPackedBitSize()
            {
                // How many indices do we have?
                int indices = Height * Width;

                if (DualPlane)
                {
                    indices *= 2;
                }

                IntegerEncoded intEncoded = IntegerEncoded.CreateEncoding(MaxWeight);

                return intEncoded.GetBitLength(indices);
            }

            public readonly int GetNumWeightValues()
            {
                int ret = Width * Height;

                if (DualPlane)
                {
                    ret *= 2;
                }

                return ret;
            }
        }

        public static bool TryDecodeToRgba8(
            ReadOnlyMemory<byte> data,
            int blockWidth,
            int blockHeight,
            int width,
            int height,
            int depth,
            int levels,
            int layers,
            out Span<byte> decoded)
        {
            byte[] output = new byte[QueryDecompressedSize(width, height, depth, levels, layers)];

            AstcDecoder decoder = new(data, output, blockWidth, blockHeight, width, height, depth, levels, layers);

            for (int i = 0; i < decoder.TotalBlockCount; i++)
            {
                decoder.ProcessBlock(i);
            }

            decoded = output;

            return decoder.Success;
        }

        public static bool TryDecodeToRgba8(
            ReadOnlyMemory<byte> data,
            Memory<byte> outputBuffer,
            int blockWidth,
            int blockHeight,
            int width,
            int height,
            int depth,
            int levels,
            int layers)
        {
            AstcDecoder decoder = new(data, outputBuffer, blockWidth, blockHeight, width, height, depth, levels, layers);

            for (int i = 0; i < decoder.TotalBlockCount; i++)
            {
                decoder.ProcessBlock(i);
            }

            return decoder.Success;
        }

        public static bool TryDecodeToRgba8P(
            ReadOnlyMemory<byte> data,
            Memory<byte> outputBuffer,
            int blockWidth,
            int blockHeight,
            int width,
            int height,
            int depth,
            int levels,
            int layers)
        {
            AstcDecoder decoder = new(data, outputBuffer, blockWidth, blockHeight, width, height, depth, levels, layers);

            // Lazy parallelism
            Enumerable.Range(0, decoder.TotalBlockCount).AsParallel().ForAll(x => decoder.ProcessBlock(x));

            return decoder.Success;
        }

        public static bool TryDecodeToRgba8P(
            ReadOnlyMemory<byte> data,
            int blockWidth,
            int blockHeight,
            int width,
            int height,
            int depth,
            int levels,
            int layers,
            out MemoryOwner<byte> decoded)
        {
            decoded = MemoryOwner<byte>.Rent(QueryDecompressedSize(width, height, depth, levels, layers));

            AstcDecoder decoder = new(data, decoded.Memory, blockWidth, blockHeight, width, height, depth, levels, layers);

            Enumerable.Range(0, decoder.TotalBlockCount).AsParallel().ForAll(x => decoder.ProcessBlock(x));

            return decoder.Success;
        }

        public static bool DecompressBlock(
            Buffer16 inputBlock,
            Span<int> outputBuffer,
            int blockWidth,
            int blockHeight)
        {
            BitStream128 bitStream = new(inputBlock);

            DecodeBlockInfo(ref bitStream, out TexelWeightParams texelParams);

            if (texelParams.Error)
            {
                throw new AstcDecoderException("Invalid block mode");
            }

            if (texelParams.VoidExtentLdr)
            {
                FillVoidExtentLdr(ref bitStream, outputBuffer, blockWidth, blockHeight);

                return true;
            }

            if (texelParams.VoidExtentHdr)
            {
                throw new AstcDecoderException("HDR void extent blocks are not supported.");
            }

            if (texelParams.Width > blockWidth)
            {
                throw new AstcDecoderException("Texel weight grid width should be smaller than block width.");
            }

            if (texelParams.Height > blockHeight)
            {
                throw new AstcDecoderException("Texel weight grid height should be smaller than block height.");
            }

            // Read num partitions
            int numberPartitions = bitStream.ReadBits(2) + 1;
            Debug.Assert(numberPartitions <= 4);

            if (numberPartitions == 4 && texelParams.DualPlane)
            {
                throw new AstcDecoderException("Dual plane mode is incompatible with four partition blocks.");
            }

            // Based on the number of partitions, read the color endpoint mode for
            // each partition.

            // Determine partitions, partition index, and color endpoint modes
            int planeIndices;
            int partitionIndex;

            Span<uint> colorEndpointMode = stackalloc uint[4];

            BitStream128 colorEndpointStream = new();

            // Read extra config data...
            uint baseColorEndpointMode = 0;

            if (numberPartitions == 1)
            {
                colorEndpointMode[0] = (uint)bitStream.ReadBits(4);
                partitionIndex = 0;
            }
            else
            {
                partitionIndex = bitStream.ReadBits(10);
                baseColorEndpointMode = (uint)bitStream.ReadBits(6);
            }

            uint baseMode = (baseColorEndpointMode & 3);

            // Remaining bits are color endpoint data...
            int numberWeightBits = texelParams.GetPackedBitSize();
            int remainingBits = bitStream.BitsLeft - numberWeightBits;

            // Consider extra bits prior to texel data...
            uint extraColorEndpointModeBits = 0;

            if (baseMode != 0)
            {
                switch (numberPartitions)
                {
                    case 2:
                        extraColorEndpointModeBits += 2;
                        break;
                    case 3:
                        extraColorEndpointModeBits += 5;
                        break;
                    case 4:
                        extraColorEndpointModeBits += 8;
                        break;
                    default:
                        Debug.Assert(false);
                        break;
                }
            }

            remainingBits -= (int)extraColorEndpointModeBits;

            // Do we have a dual plane situation?
            int planeSelectorBits = 0;

            if (texelParams.DualPlane)
            {
                planeSelectorBits = 2;
            }

            remainingBits -= planeSelectorBits;

            // Read color data...
            int colorDataBits = remainingBits;

            while (remainingBits > 0)
            {
                int numberBits = Math.Min(remainingBits, 8);
                int bits = bitStream.ReadBits(numberBits);
                colorEndpointStream.WriteBits(bits, numberBits);
                remainingBits -= 8;
            }

            // Read the plane selection bits
            planeIndices = bitStream.ReadBits(planeSelectorBits);

            // Read the rest of the CEM
            if (baseMode != 0)
            {
                uint extraColorEndpointMode = (uint)bitStream.ReadBits((int)extraColorEndpointModeBits);
                uint tempColorEndpointMode = (extraColorEndpointMode << 6) | baseColorEndpointMode;
                tempColorEndpointMode >>= 2;

                Span<bool> c = stackalloc bool[4];

                for (int i = 0; i < numberPartitions; i++)
                {
                    c[i] = (tempColorEndpointMode & 1) != 0;
                    tempColorEndpointMode >>= 1;
                }

                Span<byte> m = stackalloc byte[4];

                for (int i = 0; i < numberPartitions; i++)
                {
                    m[i] = (byte)(tempColorEndpointMode & 3);
                    tempColorEndpointMode >>= 2;
                    Debug.Assert(m[i] <= 3);
                }

                for (int i = 0; i < numberPartitions; i++)
                {
                    colorEndpointMode[i] = baseMode;

                    if (!(c[i]))
                    {
                        colorEndpointMode[i] -= 1;
                    }

                    colorEndpointMode[i] <<= 2;
                    colorEndpointMode[i] |= m[i];
                }
            }
            else if (numberPartitions > 1)
            {
                uint tempColorEndpointMode = baseColorEndpointMode >> 2;

                for (int i = 0; i < numberPartitions; i++)
                {
                    colorEndpointMode[i] = tempColorEndpointMode;
                }
            }

            // Make sure everything up till here is sane.
            for (int i = 0; i < numberPartitions; i++)
            {
                Debug.Assert(colorEndpointMode[i] < 16);
            }
            Debug.Assert(bitStream.BitsLeft == texelParams.GetPackedBitSize());

            // Decode both color data and texel weight data
            Span<int> colorValues = stackalloc int[32]; // Four values * two endpoints * four maximum partitions
            DecodeColorValues(colorValues, ref colorEndpointStream, colorEndpointMode, numberPartitions, colorDataBits);

            EndPointSet endPoints;

            unsafe
            {
                // Skip struct initialization
                _ = &endPoints;
            }

            int colorValuesPosition = 0;

            for (int i = 0; i < numberPartitions; i++)
            {
                ComputeEndpoints(endPoints.Get(i), colorValues, colorEndpointMode[i], ref colorValuesPosition);
            }

            // Read the texel weight data.
            Buffer16 texelWeightData = inputBlock;

            // Reverse everything
            for (int i = 0; i < 8; i++)
            {
                byte a = ReverseByte(texelWeightData[i]);
                byte b = ReverseByte(texelWeightData[15 - i]);

                texelWeightData[i] = b;
                texelWeightData[15 - i] = a;
            }

            // Make sure that higher non-texel bits are set to zero
            int clearByteStart = (texelParams.GetPackedBitSize() >> 3) + 1;
            texelWeightData[clearByteStart - 1] &= (byte)((1 << (texelParams.GetPackedBitSize() % 8)) - 1);

            int cLen = 16 - clearByteStart;
            for (int i = clearByteStart; i < clearByteStart + cLen; i++)
            {
                texelWeightData[i] = 0;
            }

            IntegerSequence texelWeightValues;

            unsafe
            {
                // Skip struct initialization
                _ = &texelWeightValues;
            }

            texelWeightValues.Reset();

            BitStream128 weightBitStream = new(texelWeightData);

            IntegerEncoded.DecodeIntegerSequence(ref texelWeightValues, ref weightBitStream, texelParams.MaxWeight, texelParams.GetNumWeightValues());

            // Blocks can be at most 12x12, so we can have as many as 144 weights
            Weights weights;

            unsafe
            {
                // Skip struct initialization
                _ = &weights;
            }

            UnquantizeTexelWeights(ref weights, ref texelWeightValues, ref texelParams, blockWidth, blockHeight);

            ushort[] table = Bits.Replicate8_16Table;

            // Now that we have endpoints and weights, we can interpolate and generate
            // the proper decoding...
            for (int j = 0; j < blockHeight; j++)
            {
                for (int i = 0; i < blockWidth; i++)
                {
                    int partition = Select2dPartition(partitionIndex, i, j, numberPartitions, ((blockHeight * blockWidth) < 32));
                    Debug.Assert(partition < numberPartitions);

                    AstcPixel pixel = new();
                    for (int component = 0; component < 4; component++)
                    {
                        int component0 = endPoints.Get(partition)[0].GetComponent(component);
                        component0 = table[component0];
                        int component1 = endPoints.Get(partition)[1].GetComponent(component);
                        component1 = table[component1];

                        int plane = 0;

                        if (texelParams.DualPlane && (((planeIndices + 1) & 3) == component))
                        {
                            plane = 1;
                        }

                        int weight = weights.Get(plane)[j * blockWidth + i];
                        int finalComponent = (component0 * (64 - weight) + component1 * weight + 32) / 64;

                        if (finalComponent == 65535)
                        {
                            pixel.SetComponent(component, 255);
                        }
                        else
                        {
                            double finalComponentFloat = finalComponent;
                            pixel.SetComponent(component, (int)(255.0 * (finalComponentFloat / 65536.0) + 0.5));
                        }
                    }

                    outputBuffer[j * blockWidth + i] = pixel.Pack();
                }
            }

            return true;
        }

        // Blocks can be at most 12x12, so we can have as many as 144 weights
        [StructLayout(LayoutKind.Sequential, Size = 144 * sizeof(int) * Count)]
        private struct Weights
        {
            private int _start;

            public const int Count = 2;

            public Span<int> this[int index]
            {
                get
                {
                    if ((uint)index >= Count)
                    {
                        throw new ArgumentOutOfRangeException(nameof(index), index, null);
                    }

                    ref int start = ref Unsafe.Add(ref _start, index * 144);

                    return MemoryMarshal.CreateSpan(ref start, 144);
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Span<int> Get(int index)
            {
                ref int start = ref Unsafe.Add(ref _start, index * 144);

                return MemoryMarshal.CreateSpan(ref start, 144);
            }
        }

        private static int Select2dPartition(int seed, int x, int y, int partitionCount, bool isSmallBlock)
        {
            return SelectPartition(seed, x, y, 0, partitionCount, isSmallBlock);
        }

        private static int SelectPartition(int seed, int x, int y, int z, int partitionCount, bool isSmallBlock)
        {
            if (partitionCount == 1)
            {
                return 0;
            }

            if (isSmallBlock)
            {
                x <<= 1;
                y <<= 1;
                z <<= 1;
            }

            seed += (partitionCount - 1) * 1024;

            int rightNum = Hash52((uint)seed);
            byte seed01 = (byte)(rightNum & 0xF);
            byte seed02 = (byte)((rightNum >> 4) & 0xF);
            byte seed03 = (byte)((rightNum >> 8) & 0xF);
            byte seed04 = (byte)((rightNum >> 12) & 0xF);
            byte seed05 = (byte)((rightNum >> 16) & 0xF);
            byte seed06 = (byte)((rightNum >> 20) & 0xF);
            byte seed07 = (byte)((rightNum >> 24) & 0xF);
            byte seed08 = (byte)((rightNum >> 28) & 0xF);
            byte seed09 = (byte)((rightNum >> 18) & 0xF);
            byte seed10 = (byte)((rightNum >> 22) & 0xF);
            byte seed11 = (byte)((rightNum >> 26) & 0xF);
            byte seed12 = (byte)(((rightNum >> 30) | (rightNum << 2)) & 0xF);

            seed01 *= seed01;
            seed02 *= seed02;
            seed03 *= seed03;
            seed04 *= seed04;
            seed05 *= seed05;
            seed06 *= seed06;
            seed07 *= seed07;
            seed08 *= seed08;
            seed09 *= seed09;
            seed10 *= seed10;
            seed11 *= seed11;
            seed12 *= seed12;

            int seedHash1, seedHash2, seedHash3;

            if ((seed & 1) != 0)
            {
                seedHash1 = (seed & 2) != 0 ? 4 : 5;
                seedHash2 = (partitionCount == 3) ? 6 : 5;
            }
            else
            {
                seedHash1 = (partitionCount == 3) ? 6 : 5;
                seedHash2 = (seed & 2) != 0 ? 4 : 5;
            }

            seedHash3 = (seed & 0x10) != 0 ? seedHash1 : seedHash2;

            seed01 >>= seedHash1;
            seed02 >>= seedHash2;
            seed03 >>= seedHash1;
            seed04 >>= seedHash2;
            seed05 >>= seedHash1;
            seed06 >>= seedHash2;
            seed07 >>= seedHash1;
            seed08 >>= seedHash2;
            seed09 >>= seedHash3;
            seed10 >>= seedHash3;
            seed11 >>= seedHash3;
            seed12 >>= seedHash3;

            int a = seed01 * x + seed02 * y + seed11 * z + (rightNum >> 14);
            int b = seed03 * x + seed04 * y + seed12 * z + (rightNum >> 10);
            int c = seed05 * x + seed06 * y + seed09 * z + (rightNum >> 6);
            int d = seed07 * x + seed08 * y + seed10 * z + (rightNum >> 2);

            a &= 0x3F;
            b &= 0x3F;
            c &= 0x3F;
            d &= 0x3F;

            if (partitionCount < 4)
            {
                d = 0;
            }

            if (partitionCount < 3)
            {
                c = 0;
            }

            if (a >= b && a >= c && a >= d)
            {
                return 0;
            }
            else if (b >= c && b >= d)
            {
                return 1;
            }
            else if (c >= d)
            {
                return 2;
            }

            return 3;
        }

        static int Hash52(uint val)
        {
            val ^= val >> 15;
            val -= val << 17;
            val += val << 7;
            val += val << 4;
            val ^= val >> 5;
            val += val << 16;
            val ^= val >> 7;
            val ^= val >> 3;
            val ^= val << 6;
            val ^= val >> 17;

            return (int)val;
        }

        static void UnquantizeTexelWeights(
            ref Weights outputBuffer,
            ref IntegerSequence weights,
            ref TexelWeightParams texelParams,
            int blockWidth,
            int blockHeight)
        {
            int weightIndices = 0;
            Weights unquantized;

            unsafe
            {
                // Skip struct initialization
                _ = &unquantized;
            }

            Span<IntegerEncoded> weightsList = weights.List;
            Span<int> unquantized0 = unquantized[0];
            Span<int> unquantized1 = unquantized[1];

            for (int i = 0; i < weightsList.Length; i++)
            {
                unquantized0[weightIndices] = UnquantizeTexelWeight(weightsList[i]);

                if (texelParams.DualPlane)
                {
                    i++;
                    unquantized1[weightIndices] = UnquantizeTexelWeight(weightsList[i]);

                    if (i == weightsList.Length)
                    {
                        break;
                    }
                }

                if (++weightIndices >= texelParams.Width * texelParams.Height)
                {
                    break;
                }
            }

            // Do infill if necessary (Section C.2.18) ...
            int ds = (1024 + blockWidth / 2) / (blockWidth - 1);
            int dt = (1024 + blockHeight / 2) / (blockHeight - 1);

            int planeScale = texelParams.DualPlane ? 2 : 1;

            for (int plane = 0; plane < planeScale; plane++)
            {
                Span<int> unquantizedSpan = unquantized.Get(plane);
                Span<int> outputSpan = outputBuffer.Get(plane);

                for (int t = 0; t < blockHeight; t++)
                {
                    for (int s = 0; s < blockWidth; s++)
                    {
                        int cs = ds * s;
                        int ct = dt * t;

                        int gs = (cs * (texelParams.Width - 1) + 32) >> 6;
                        int gt = (ct * (texelParams.Height - 1) + 32) >> 6;

                        int js = gs >> 4;
                        int fs = gs & 0xF;

                        int jt = gt >> 4;
                        int ft = gt & 0x0F;

                        int w11 = (fs * ft + 8) >> 4;

                        int v0 = js + jt * texelParams.Width;

                        int weight = 8;

                        int wxh = texelParams.Width * texelParams.Height;

                        if (v0 < wxh)
                        {
                            weight += unquantizedSpan[v0] * (16 - fs - ft + w11);

                            if (v0 + 1 < wxh)
                            {
                                weight += unquantizedSpan[v0 + 1] * (fs - w11);
                            }
                        }

                        if (v0 + texelParams.Width < wxh)
                        {
                            weight += unquantizedSpan[v0 + texelParams.Width] * (ft - w11);

                            if (v0 + texelParams.Width + 1 < wxh)
                            {
                                weight += unquantizedSpan[v0 + texelParams.Width + 1] * w11;
                            }
                        }

                        outputSpan[t * blockWidth + s] = weight >> 4;
                    }
                }
            }
        }

        static int UnquantizeTexelWeight(IntegerEncoded intEncoded)
        {
            int bitValue = intEncoded.BitValue;
            int bitLength = intEncoded.NumberBits;

            int a = Bits.Replicate1_7(bitValue & 1);
            int b = 0, c = 0, d = 0;

            int result = 0;

            switch (intEncoded.GetEncoding())
            {
                case IntegerEncoded.EIntegerEncoding.JustBits:
                    result = Bits.Replicate(bitValue, bitLength, 6);
                    break;

                case IntegerEncoded.EIntegerEncoding.Trit:
                    {
                        d = intEncoded.TritValue;
                        Debug.Assert(d < 3);

                        switch (bitLength)
                        {
                            case 0:
                                {
                                    result = d switch
                                    {
                                        0 => 0,
                                        1 => 32,
                                        2 => 63,
                                        _ => 0,
                                    };

                                    break;
                                }

                            case 1:
                                {
                                    c = 50;
                                    break;
                                }

                            case 2:
                                {
                                    c = 23;
                                    int b2 = (bitValue >> 1) & 1;
                                    b = (b2 << 6) | (b2 << 2) | b2;

                                    break;
                                }

                            case 3:
                                {
                                    c = 11;
                                    int cb = (bitValue >> 1) & 3;
                                    b = (cb << 5) | cb;

                                    break;
                                }

                            default:
                                throw new AstcDecoderException("Invalid trit encoding for texel weight.");
                        }

                        break;
                    }

                case IntegerEncoded.EIntegerEncoding.Quint:
                    {
                        d = intEncoded.QuintValue;
                        Debug.Assert(d < 5);

                        switch (bitLength)
                        {
                            case 0:
                                {
                                    result = d switch
                                    {
                                        0 => 0,
                                        1 => 16,
                                        2 => 32,
                                        3 => 47,
                                        4 => 63,
                                        _ => 0,
                                    };

                                    break;
                                }

                            case 1:
                                {
                                    c = 28;

                                    break;
                                }

                            case 2:
                                {
                                    c = 13;
                                    int b2 = (bitValue >> 1) & 1;
                                    b = (b2 << 6) | (b2 << 1);

                                    break;
                                }

                            default:
                                throw new AstcDecoderException("Invalid quint encoding for texel weight.");
                        }

                        break;
                    }
            }

            if (intEncoded.GetEncoding() != IntegerEncoded.EIntegerEncoding.JustBits && bitLength > 0)
            {
                // Decode the value...
                result = d * c + b;
                result ^= a;
                result = (a & 0x20) | (result >> 2);
            }

            Debug.Assert(result < 64);

            // Change from [0,63] to [0,64]
            if (result > 32)
            {
                result += 1;
            }

            return result;
        }

        static byte ReverseByte(byte b)
        {
            // Taken from http://graphics.stanford.edu/~seander/bithacks.html#ReverseByteWith64Bits
            return (byte)((((b) * 0x80200802L) & 0x0884422110L) * 0x0101010101L >> 32);
        }

        static Span<uint> ReadUintColorValues(int number, Span<int> colorValues, ref int colorValuesPosition)
        {
            Span<int> ret = colorValues.Slice(colorValuesPosition, number);

            colorValuesPosition += number;

            return MemoryMarshal.Cast<int, uint>(ret);
        }

        static Span<int> ReadIntColorValues(int number, Span<int> colorValues, ref int colorValuesPosition)
        {
            Span<int> ret = colorValues.Slice(colorValuesPosition, number);

            colorValuesPosition += number;

            return ret;
        }

        static void ComputeEndpoints(
            Span<AstcPixel> endPoints,
            Span<int> colorValues,
            uint colorEndpointMode,
            ref int colorValuesPosition)
        {
            switch (colorEndpointMode)
            {
                case 0:
                    {
                        Span<uint> val = ReadUintColorValues(2, colorValues, ref colorValuesPosition);

                        endPoints[0] = new AstcPixel(0xFF, (short)val[0], (short)val[0], (short)val[0]);
                        endPoints[1] = new AstcPixel(0xFF, (short)val[1], (short)val[1], (short)val[1]);

                        break;
                    }


                case 1:
                    {
                        Span<uint> val = ReadUintColorValues(2, colorValues, ref colorValuesPosition);
                        int l0 = (int)((val[0] >> 2) | (val[1] & 0xC0));
                        int l1 = (int)Math.Min(l0 + (val[1] & 0x3F), 0xFFU);

                        endPoints[0] = new AstcPixel(0xFF, (short)l0, (short)l0, (short)l0);
                        endPoints[1] = new AstcPixel(0xFF, (short)l1, (short)l1, (short)l1);

                        break;
                    }

                case 4:
                    {
                        Span<uint> val = ReadUintColorValues(4, colorValues, ref colorValuesPosition);

                        endPoints[0] = new AstcPixel((short)val[2], (short)val[0], (short)val[0], (short)val[0]);
                        endPoints[1] = new AstcPixel((short)val[3], (short)val[1], (short)val[1], (short)val[1]);

                        break;
                    }

                case 5:
                    {
                        Span<int> val = ReadIntColorValues(4, colorValues, ref colorValuesPosition);

                        Bits.BitTransferSigned(ref val[1], ref val[0]);
                        Bits.BitTransferSigned(ref val[3], ref val[2]);

                        endPoints[0] = new AstcPixel((short)val[2], (short)val[0], (short)val[0], (short)val[0]);
                        endPoints[1] = new AstcPixel((short)(val[2] + val[3]), (short)(val[0] + val[1]), (short)(val[0] + val[1]), (short)(val[0] + val[1]));

                        endPoints[0].ClampByte();
                        endPoints[1].ClampByte();

                        break;
                    }

                case 6:
                    {
                        Span<uint> val = ReadUintColorValues(4, colorValues, ref colorValuesPosition);

                        endPoints[0] = new AstcPixel(0xFF, (short)(val[0] * val[3] >> 8), (short)(val[1] * val[3] >> 8), (short)(val[2] * val[3] >> 8));
                        endPoints[1] = new AstcPixel(0xFF, (short)val[0], (short)val[1], (short)val[2]);

                        break;
                    }

                case 8:
                    {
                        Span<uint> val = ReadUintColorValues(6, colorValues, ref colorValuesPosition);

                        if (val[1] + val[3] + val[5] >= val[0] + val[2] + val[4])
                        {
                            endPoints[0] = new AstcPixel(0xFF, (short)val[0], (short)val[2], (short)val[4]);
                            endPoints[1] = new AstcPixel(0xFF, (short)val[1], (short)val[3], (short)val[5]);
                        }
                        else
                        {
                            endPoints[0] = AstcPixel.BlueContract(0xFF, (short)val[1], (short)val[3], (short)val[5]);
                            endPoints[1] = AstcPixel.BlueContract(0xFF, (short)val[0], (short)val[2], (short)val[4]);
                        }

                        break;
                    }

                case 9:
                    {
                        Span<int> val = ReadIntColorValues(6, colorValues, ref colorValuesPosition);

                        Bits.BitTransferSigned(ref val[1], ref val[0]);
                        Bits.BitTransferSigned(ref val[3], ref val[2]);
                        Bits.BitTransferSigned(ref val[5], ref val[4]);

                        if (val[1] + val[3] + val[5] >= 0)
                        {
                            endPoints[0] = new AstcPixel(0xFF, (short)val[0], (short)val[2], (short)val[4]);
                            endPoints[1] = new AstcPixel(0xFF, (short)(val[0] + val[1]), (short)(val[2] + val[3]), (short)(val[4] + val[5]));
                        }
                        else
                        {
                            endPoints[0] = AstcPixel.BlueContract(0xFF, val[0] + val[1], val[2] + val[3], val[4] + val[5]);
                            endPoints[1] = AstcPixel.BlueContract(0xFF, val[0], val[2], val[4]);
                        }

                        endPoints[0].ClampByte();
                        endPoints[1].ClampByte();

                        break;
                    }

                case 10:
                    {
                        Span<uint> val = ReadUintColorValues(6, colorValues, ref colorValuesPosition);

                        endPoints[0] = new AstcPixel((short)val[4], (short)(val[0] * val[3] >> 8), (short)(val[1] * val[3] >> 8), (short)(val[2] * val[3] >> 8));
                        endPoints[1] = new AstcPixel((short)val[5], (short)val[0], (short)val[1], (short)val[2]);

                        break;
                    }

                case 12:
                    {
                        Span<uint> val = ReadUintColorValues(8, colorValues, ref colorValuesPosition);

                        if (val[1] + val[3] + val[5] >= val[0] + val[2] + val[4])
                        {
                            endPoints[0] = new AstcPixel((short)val[6], (short)val[0], (short)val[2], (short)val[4]);
                            endPoints[1] = new AstcPixel((short)val[7], (short)val[1], (short)val[3], (short)val[5]);
                        }
                        else
                        {
                            endPoints[0] = AstcPixel.BlueContract((short)val[7], (short)val[1], (short)val[3], (short)val[5]);
                            endPoints[1] = AstcPixel.BlueContract((short)val[6], (short)val[0], (short)val[2], (short)val[4]);
                        }

                        break;
                    }

                case 13:
                    {
                        Span<int> val = ReadIntColorValues(8, colorValues, ref colorValuesPosition);

                        Bits.BitTransferSigned(ref val[1], ref val[0]);
                        Bits.BitTransferSigned(ref val[3], ref val[2]);
                        Bits.BitTransferSigned(ref val[5], ref val[4]);
                        Bits.BitTransferSigned(ref val[7], ref val[6]);

                        if (val[1] + val[3] + val[5] >= 0)
                        {
                            endPoints[0] = new AstcPixel((short)val[6], (short)val[0], (short)val[2], (short)val[4]);
                            endPoints[1] = new AstcPixel((short)(val[7] + val[6]), (short)(val[0] + val[1]), (short)(val[2] + val[3]), (short)(val[4] + val[5]));
                        }
                        else
                        {
                            endPoints[0] = AstcPixel.BlueContract(val[6] + val[7], val[0] + val[1], val[2] + val[3], val[4] + val[5]);
                            endPoints[1] = AstcPixel.BlueContract(val[6], val[0], val[2], val[4]);
                        }

                        endPoints[0].ClampByte();
                        endPoints[1].ClampByte();

                        break;
                    }

                default:
                    throw new AstcDecoderException("Unsupported color endpoint mode (is it HDR?)");
            }
        }

        static void DecodeColorValues(
            Span<int> outputValues,
            ref BitStream128 colorBitStream,
            Span<uint> modes,
            int numberPartitions,
            int numberBitsForColorData)
        {
            // First figure out how many color values we have
            int numberValues = 0;

            for (int i = 0; i < numberPartitions; i++)
            {
                numberValues += (int)((modes[i] >> 2) + 1) << 1;
            }

            // Then based on the number of values and the remaining number of bits,
            // figure out the max value for each of them...
            int range = 256;

            while (--range > 0)
            {
                IntegerEncoded intEncoded = IntegerEncoded.CreateEncoding(range);
                int bitLength = intEncoded.GetBitLength(numberValues);

                if (bitLength <= numberBitsForColorData)
                {
                    // Find the smallest possible range that matches the given encoding
                    while (--range > 0)
                    {
                        IntegerEncoded newIntEncoded = IntegerEncoded.CreateEncoding(range);
                        if (!newIntEncoded.MatchesEncoding(intEncoded))
                        {
                            break;
                        }
                    }

                    // Return to last matching range.
                    range++;
                    break;
                }
            }

            // We now have enough to decode our integer sequence.
            IntegerSequence integerEncodedSequence;

            unsafe
            {
                // Skip struct initialization
                _ = &integerEncodedSequence;
            }

            integerEncodedSequence.Reset();

            IntegerEncoded.DecodeIntegerSequence(ref integerEncodedSequence, ref colorBitStream, range, numberValues);

            // Once we have the decoded values, we need to dequantize them to the 0-255 range
            // This procedure is outlined in ASTC spec C.2.13
            int outputIndices = 0;

            foreach (ref IntegerEncoded intEncoded in integerEncodedSequence.List)
            {
                int bitLength = intEncoded.NumberBits;
                int bitValue = intEncoded.BitValue;

                Debug.Assert(bitLength >= 1);

                int b = 0, c = 0, d = 0;
                // A is just the lsb replicated 9 times.
                int a = Bits.Replicate(bitValue & 1, 1, 9);

                switch (intEncoded.GetEncoding())
                {
                    case IntegerEncoded.EIntegerEncoding.JustBits:
                        {
                            outputValues[outputIndices++] = Bits.Replicate(bitValue, bitLength, 8);

                            break;
                        }

                    case IntegerEncoded.EIntegerEncoding.Trit:
                        {
                            d = intEncoded.TritValue;

                            switch (bitLength)
                            {
                                case 1:
                                    {
                                        c = 204;

                                        break;
                                    }

                                case 2:
                                    {
                                        c = 93;
                                        // B = b000b0bb0
                                        int b2 = (bitValue >> 1) & 1;
                                        b = (b2 << 8) | (b2 << 4) | (b2 << 2) | (b2 << 1);

                                        break;
                                    }

                                case 3:
                                    {
                                        c = 44;
                                        // B = cb000cbcb
                                        int cb = (bitValue >> 1) & 3;
                                        b = (cb << 7) | (cb << 2) | cb;

                                        break;
                                    }


                                case 4:
                                    {
                                        c = 22;
                                        // B = dcb000dcb
                                        int dcb = (bitValue >> 1) & 7;
                                        b = (dcb << 6) | dcb;

                                        break;
                                    }

                                case 5:
                                    {
                                        c = 11;
                                        // B = edcb000ed
                                        int edcb = (bitValue >> 1) & 0xF;
                                        b = (edcb << 5) | (edcb >> 2);

                                        break;
                                    }

                                case 6:
                                    {
                                        c = 5;
                                        // B = fedcb000f
                                        int fedcb = (bitValue >> 1) & 0x1F;
                                        b = (fedcb << 4) | (fedcb >> 4);

                                        break;
                                    }

                                default:
                                    throw new AstcDecoderException("Unsupported trit encoding for color values.");
                            }

                            break;
                        }

                    case IntegerEncoded.EIntegerEncoding.Quint:
                        {
                            d = intEncoded.QuintValue;

                            switch (bitLength)
                            {
                                case 1:
                                    {
                                        c = 113;

                                        break;
                                    }

                                case 2:
                                    {
                                        c = 54;
                                        // B = b0000bb00
                                        int b2 = (bitValue >> 1) & 1;
                                        b = (b2 << 8) | (b2 << 3) | (b2 << 2);

                                        break;
                                    }

                                case 3:
                                    {
                                        c = 26;
                                        // B = cb0000cbc
                                        int cb = (bitValue >> 1) & 3;
                                        b = (cb << 7) | (cb << 1) | (cb >> 1);

                                        break;
                                    }

                                case 4:
                                    {
                                        c = 13;
                                        // B = dcb0000dc
                                        int dcb = (bitValue >> 1) & 7;
                                        b = (dcb << 6) | (dcb >> 1);

                                        break;
                                    }

                                case 5:
                                    {
                                        c = 6;
                                        // B = edcb0000e
                                        int edcb = (bitValue >> 1) & 0xF;
                                        b = (edcb << 5) | (edcb >> 3);

                                        break;
                                    }

                                default:
                                    throw new AstcDecoderException("Unsupported quint encoding for color values.");
                            }
                            break;
                        }
                }

                if (intEncoded.GetEncoding() != IntegerEncoded.EIntegerEncoding.JustBits)
                {
                    int T = d * c + b;
                    T ^= a;
                    T = (a & 0x80) | (T >> 2);

                    outputValues[outputIndices++] = T;
                }
            }

            // Make sure that each of our values is in the proper range...
            for (int i = 0; i < numberValues; i++)
            {
                Debug.Assert(outputValues[i] <= 255);
            }
        }

        static void FillVoidExtentLdr(ref BitStream128 bitStream, Span<int> outputBuffer, int blockWidth, int blockHeight)
        {
            // Don't actually care about the void extent, just read the bits...
            for (int i = 0; i < 4; ++i)
            {
                bitStream.ReadBits(13);
            }

            // Decode the RGBA components and renormalize them to the range [0, 255]
            ushort r = (ushort)bitStream.ReadBits(16);
            ushort g = (ushort)bitStream.ReadBits(16);
            ushort b = (ushort)bitStream.ReadBits(16);
            ushort a = (ushort)bitStream.ReadBits(16);

            int rgba = (r >> 8) | (g & 0xFF00) | ((b) & 0xFF00) << 8 | ((a) & 0xFF00) << 16;

            for (int j = 0; j < blockHeight; j++)
            {
                for (int i = 0; i < blockWidth; i++)
                {
                    outputBuffer[j * blockWidth + i] = rgba;
                }
            }
        }

        static void DecodeBlockInfo(ref BitStream128 bitStream, out TexelWeightParams texelParams)
        {
            texelParams = new TexelWeightParams();

            // Read the entire block mode all at once
            ushort modeBits = (ushort)bitStream.ReadBits(11);

            // Does this match the void extent block mode?
            if ((modeBits & 0x01FF) == 0x1FC)
            {
                if ((modeBits & 0x200) != 0)
                {
                    texelParams.VoidExtentHdr = true;
                }
                else
                {
                    texelParams.VoidExtentLdr = true;
                }

                // Next two bits must be one.
                if ((modeBits & 0x400) == 0 || bitStream.ReadBits(1) == 0)
                {
                    texelParams.Error = true;
                }

                return;
            }

            // First check if the last four bits are zero
            if ((modeBits & 0xF) == 0)
            {
                texelParams.Error = true;

                return;
            }

            // If the last two bits are zero, then if bits
            // [6-8] are all ones, this is also reserved.
            if ((modeBits & 0x3) == 0 && (modeBits & 0x1C0) == 0x1C0)
            {
                texelParams.Error = true;

                return;
            }

            // Otherwise, there is no error... Figure out the layout
            // of the block mode. Layout is determined by a number
            // between 0 and 9 corresponding to table C.2.8 of the
            // ASTC spec.
            int layout;

            if ((modeBits & 0x1) != 0 || (modeBits & 0x2) != 0)
            {
                // layout is in [0-4]
                if ((modeBits & 0x8) != 0)
                {
                    // layout is in [2-4]
                    if ((modeBits & 0x4) != 0)
                    {
                        // layout is in [3-4]
                        if ((modeBits & 0x100) != 0)
                        {
                            layout = 4;
                        }
                        else
                        {
                            layout = 3;
                        }
                    }
                    else
                    {
                        layout = 2;
                    }
                }
                else
                {
                    // layout is in [0-1]
                    if ((modeBits & 0x4) != 0)
                    {
                        layout = 1;
                    }
                    else
                    {
                        layout = 0;
                    }
                }
            }
            else
            {
                // layout is in [5-9]
                if ((modeBits & 0x100) != 0)
                {
                    // layout is in [7-9]
                    if ((modeBits & 0x80) != 0)
                    {
                        // layout is in [7-8]
                        Debug.Assert((modeBits & 0x40) == 0);

                        if ((modeBits & 0x20) != 0)
                        {
                            layout = 8;
                        }
                        else
                        {
                            layout = 7;
                        }
                    }
                    else
                    {
                        layout = 9;
                    }
                }
                else
                {
                    // layout is in [5-6]
                    if ((modeBits & 0x80) != 0)
                    {
                        layout = 6;
                    }
                    else
                    {
                        layout = 5;
                    }
                }
            }

            Debug.Assert(layout < 10);

            // Determine R
            int r = (modeBits >> 4) & 1;
            if (layout < 5)
            {
                r |= (modeBits & 0x3) << 1;
            }
            else
            {
                r |= (modeBits & 0xC) >> 1;
            }

            Debug.Assert(2 <= r && r <= 7);

            // Determine width & height
            switch (layout)
            {
                case 0:
                    {
                        int a = (modeBits >> 5) & 0x3;
                        int b = (modeBits >> 7) & 0x3;

                        texelParams.Width = b + 4;
                        texelParams.Height = a + 2;

                        break;
                    }

                case 1:
                    {
                        int a = (modeBits >> 5) & 0x3;
                        int b = (modeBits >> 7) & 0x3;

                        texelParams.Width = b + 8;
                        texelParams.Height = a + 2;

                        break;
                    }

                case 2:
                    {
                        int a = (modeBits >> 5) & 0x3;
                        int b = (modeBits >> 7) & 0x3;

                        texelParams.Width = a + 2;
                        texelParams.Height = b + 8;

                        break;
                    }

                case 3:
                    {
                        int a = (modeBits >> 5) & 0x3;
                        int b = (modeBits >> 7) & 0x1;

                        texelParams.Width = a + 2;
                        texelParams.Height = b + 6;

                        break;
                    }

                case 4:
                    {
                        int a = (modeBits >> 5) & 0x3;
                        int b = (modeBits >> 7) & 0x1;

                        texelParams.Width = b + 2;
                        texelParams.Height = a + 2;

                        break;
                    }

                case 5:
                    {
                        int a = (modeBits >> 5) & 0x3;

                        texelParams.Width = 12;
                        texelParams.Height = a + 2;

                        break;
                    }

                case 6:
                    {
                        int a = (modeBits >> 5) & 0x3;

                        texelParams.Width = a + 2;
                        texelParams.Height = 12;

                        break;
                    }

                case 7:
                    {
                        texelParams.Width = 6;
                        texelParams.Height = 10;

                        break;
                    }

                case 8:
                    {
                        texelParams.Width = 10;
                        texelParams.Height = 6;
                        break;
                    }

                case 9:
                    {
                        int a = (modeBits >> 5) & 0x3;
                        int b = (modeBits >> 9) & 0x3;

                        texelParams.Width = a + 6;
                        texelParams.Height = b + 6;

                        break;
                    }

                default:
                    // Don't know this layout...
                    texelParams.Error = true;
                    break;
            }

            // Determine whether or not we're using dual planes
            // and/or high precision layouts.
            bool d = ((layout != 9) && ((modeBits & 0x400) != 0));
            bool h = (layout != 9) && ((modeBits & 0x200) != 0);

            if (h)
            {
                ReadOnlySpan<byte> maxWeights = [9, 11, 15, 19, 23, 31];
                texelParams.MaxWeight = maxWeights[r - 2];
            }
            else
            {
                ReadOnlySpan<byte> maxWeights = [1, 2, 3, 4, 5, 7];
                texelParams.MaxWeight = maxWeights[r - 2];
            }

            texelParams.DualPlane = d;
        }
    }
}
