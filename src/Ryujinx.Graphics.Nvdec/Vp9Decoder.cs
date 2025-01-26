using Ryujinx.Common;
using Ryujinx.Graphics.Device;
using Ryujinx.Graphics.Nvdec.Image;
using Ryujinx.Graphics.Nvdec.Types.Vp9;
using Ryujinx.Graphics.Nvdec.Vp9;
using Ryujinx.Graphics.Video;
using Ryujinx.Memory;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Ryujinx.Graphics.Nvdec.MemoryExtensions;

namespace Ryujinx.Graphics.Nvdec
{
    static class Vp9Decoder
    {
        private static readonly Decoder _decoder = new();

        public unsafe static void Decode(ResourceManager rm, ref NvdecRegisters state)
        {
            PictureInfo pictureInfo = rm.MemoryManager.DeviceRead<PictureInfo>(state.SetDrvPicSetupOffset);
            EntropyProbs entropy = rm.MemoryManager.DeviceRead<EntropyProbs>(state.Vp9SetProbTabBufOffset);

            ISurface Rent(uint lumaOffset, uint chromaOffset, FrameSize size)
            {
                return rm.Cache.Get(_decoder, lumaOffset, chromaOffset, size.Width, size.Height);
            }

            ISurface lastSurface = Rent(state.SetPictureLumaOffset[0], state.SetPictureChromaOffset[0], pictureInfo.LastFrameSize);
            ISurface goldenSurface = Rent(state.SetPictureLumaOffset[1], state.SetPictureChromaOffset[1], pictureInfo.GoldenFrameSize);
            ISurface altSurface = Rent(state.SetPictureLumaOffset[2], state.SetPictureChromaOffset[2], pictureInfo.AltFrameSize);
            ISurface currentSurface = Rent(state.SetPictureLumaOffset[3], state.SetPictureChromaOffset[3], pictureInfo.CurrentFrameSize);

            Vp9PictureInfo info = pictureInfo.Convert();

            info.LastReference = lastSurface;
            info.GoldenReference = goldenSurface;
            info.AltReference = altSurface;

            entropy.Convert(ref info.Entropy);

            ReadOnlySpan<byte> bitstream = rm.MemoryManager.DeviceGetSpan(state.SetInBufBaseOffset, (int)pictureInfo.BitstreamSize);

            ReadOnlySpan<Vp9MvRef> mvsIn = ReadOnlySpan<Vp9MvRef>.Empty;

            if (info.UsePrevInFindMvRefs)
            {
                mvsIn = GetMvsInput(rm.MemoryManager, pictureInfo.CurrentFrameSize, state.Vp9SetColMvReadBufOffset);
            }

            int miCols = BitUtils.DivRoundUp(pictureInfo.CurrentFrameSize.Width, 8);
            int miRows = BitUtils.DivRoundUp(pictureInfo.CurrentFrameSize.Height, 8);

            using WritableRegion mvsRegion = rm.MemoryManager.GetWritableRegion(ExtendOffset(state.Vp9SetColMvWriteBufOffset), miRows * miCols * 16);

            Span<Vp9MvRef> mvsOut = MemoryMarshal.Cast<byte, Vp9MvRef>(mvsRegion.Memory.Span);

            uint lumaOffset = state.SetPictureLumaOffset[3];
            uint chromaOffset = state.SetPictureChromaOffset[3];

            if (_decoder.Decode(ref info, currentSurface, bitstream, mvsIn, mvsOut))
            {
                SurfaceWriter.Write(rm.MemoryManager, currentSurface, lumaOffset, chromaOffset);
            }

            WriteBackwardUpdates(rm.MemoryManager, state.Vp9SetCtxCounterBufOffset, ref info.BackwardUpdateCounts);

            rm.Cache.Put(lastSurface);
            rm.Cache.Put(goldenSurface);
            rm.Cache.Put(altSurface);
            rm.Cache.Put(currentSurface);
        }

        private static ReadOnlySpan<Vp9MvRef> GetMvsInput(DeviceMemoryManager mm, FrameSize size, uint offset)
        {
            int miCols = BitUtils.DivRoundUp(size.Width, 8);
            int miRows = BitUtils.DivRoundUp(size.Height, 8);

            return MemoryMarshal.Cast<byte, Vp9MvRef>(mm.DeviceGetSpan(offset, miRows * miCols * 16));
        }

        private static void WriteBackwardUpdates(DeviceMemoryManager mm, uint offset, ref Vp9BackwardUpdates counts)
        {
            using WritableRegion backwardUpdatesRegion = mm.GetWritableRegion(ExtendOffset(offset), Unsafe.SizeOf<BackwardUpdates>());

            ref BackwardUpdates backwardUpdates = ref MemoryMarshal.Cast<byte, BackwardUpdates>(backwardUpdatesRegion.Memory.Span)[0];

            backwardUpdates = new BackwardUpdates(ref counts);
        }
    }
}
