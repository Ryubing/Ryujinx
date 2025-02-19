using Ryujinx.Common.Memory;
using Ryujinx.Graphics.Nvdec.Vp9.Common;
using Ryujinx.Graphics.Video;
using System.Diagnostics;

namespace Ryujinx.Graphics.Nvdec.Vp9.Types
{
    internal struct Vp9Decoder
    {
        public Vp9Common Common;

        public int ReadyForNewData;

        public int RefreshFrameFlags;

        public int NeedResync; // Wait for key/intra-only frame.
        public int HoldRefBuf; // Hold the reference buffer.

        private static void DecreaseRefCount(int idx, ref Array12<RefCntBuffer> frameBufs, ref BufferPool pool)
        {
            if (idx >= 0 && frameBufs[idx].RefCount > 0)
            {
                --frameBufs[idx].RefCount;
                // A worker may only get a free framebuffer index when calling GetFreeFb.
                // But the private buffer is not set up until finish decoding header.
                // So any error happens during decoding header, the frame_bufs will not
                // have valid priv buffer.
                if (frameBufs[idx].Released == 0 && frameBufs[idx].RefCount == 0 &&
                    !frameBufs[idx].RawFrameBuffer.Priv.IsNull)
                {
                    FrameBuffers.ReleaseFrameBuffer(pool.CbPriv, ref frameBufs[idx].RawFrameBuffer);
                    frameBufs[idx].Released = 1;
                }
            }
        }

        public void Create(MemoryAllocator allocator, ref BufferPool pool)
        {
            ref Vp9Common cm = ref Common;

            cm.CheckMemError(ref cm.Fc,
                new Ptr<Vp9EntropyProbs>(ref allocator.Allocate<Vp9EntropyProbs>(1)[0]));
            cm.CheckMemError(ref cm.FrameContexts,
                allocator.Allocate<Vp9EntropyProbs>(Constants.FrameContexts));

            for (int i = 0; i < EntropyMode.KfYModeProb.Length; i++)
            {
                for (int j = 0; j < EntropyMode.KfYModeProb[i].Length; j++)
                {
                    for (int k = 0; k < EntropyMode.KfYModeProb[i][j].Length; k++)
                    {
                        cm.Fc.Value.KfYModeProb[i][j][k] = EntropyMode.KfYModeProb[i][j][k];
                    }
                }
            }

            for (int i = 0; i < EntropyMode.KfUvModeProb.Length; i++)
            {
                for (int j = 0; j < EntropyMode.KfUvModeProb[i].Length; j++)
                {
                    cm.Fc.Value.KfUvModeProb[i][j] = EntropyMode.KfUvModeProb[i][j];
                }
            }

            byte[][] kfPartitionProbs =
            [
                // 8x8 . 4x4
                [158, 97, 94], // a/l both not split
                [93, 24, 99], // a split, l not split
                [85, 119, 44], // l split, a not split
                [62, 59, 67], // a/l both split

                // 16x16 . 8x8
                [149, 53, 53], // a/l both not split
                [94, 20, 48], // a split, l not split
                [83, 53, 24], // l split, a not split
                [52, 18, 18], // a/l both split

                // 32x32 . 16x16
                [150, 40, 39], // a/l both not split
                [78, 12, 26], // a split, l not split
                [67, 33, 11], // l split, a not split
                [24, 7, 5], // a/l both split

                // 64x64 . 32x32
                [174, 35, 49], // a/l both not split
                [68, 11, 27], // a split, l not split
                [57, 15, 9], // l split, a not split
                [12, 3, 3] // a/l both split
            ];

            for (int i = 0; i < kfPartitionProbs.Length; i++)
            {
                for (int j = 0; j < kfPartitionProbs[i].Length; j++)
                {
                    cm.Fc.Value.KfPartitionProb[i][j] = kfPartitionProbs[i][j];
                }
            }

            cm.Counts = new Ptr<Vp9BackwardUpdates>(ref allocator.Allocate<Vp9BackwardUpdates>(1)[0]);

            NeedResync = 1;

            // Initialize the references to not point to any frame buffers.
            for (int i = 0; i < 8; i++)
            {
                cm.RefFrameMap[i] = -1;
                cm.NextRefFrameMap[i] = -1;
            }

            cm.CurrentVideoFrame = 0;
            ReadyForNewData = 1;
            Common.BufferPool = new Ptr<BufferPool>(ref pool);

            cm.BitDepth = BitDepth.Bits8;
            cm.DequantBitDepth = BitDepth.Bits8;

            // vp9_loop_filter_init(ref cm);
        }

        /* If any buffer updating is signaled it should be done here. */
        private void SwapFrameBuffers()
        {
            int refIndex = 0, mask;
            ref Vp9Common cm = ref Common;
            ref BufferPool pool = ref cm.BufferPool.Value;
            ref Array12<RefCntBuffer> frameBufs = ref cm.BufferPool.Value.FrameBufs;

            for (mask = RefreshFrameFlags; mask != 0; mask >>= 1)
            {
                int oldIdx = cm.RefFrameMap[refIndex];
                // Current thread releases the holding of reference frame.
                DecreaseRefCount(oldIdx, ref frameBufs, ref pool);

                // Release the reference frame in reference map.
                if ((mask & 1) != 0)
                {
                    DecreaseRefCount(oldIdx, ref frameBufs, ref pool);
                }

                cm.RefFrameMap[refIndex] = cm.NextRefFrameMap[refIndex];
                ++refIndex;
            }

            // Current thread releases the holding of reference frame.
            for (; refIndex < Constants.RefFrames && cm.ShowExistingFrame == 0; ++refIndex)
            {
                int oldIdx = cm.RefFrameMap[refIndex];
                DecreaseRefCount(oldIdx, ref frameBufs, ref pool);
                cm.RefFrameMap[refIndex] = cm.NextRefFrameMap[refIndex];
            }

            HoldRefBuf = 0;
            cm.FrameToShow = new Ptr<Surface>(ref cm.GetFrameNewBuffer());

            --frameBufs[cm.NewFbIdx].RefCount;

            // Invalidate these references until the next frame starts.
            for (refIndex = 0; refIndex < 3; refIndex++)
            {
                cm.FrameRefs[refIndex].Idx = RefBuffer.InvalidIdx;
            }
        }

        public CodecErr ReceiveCompressedData(MemoryAllocator allocator, ulong size, ref ArrayPtr<byte> psource)
        {
            ref Vp9Common cm = ref Common;
            ref BufferPool pool = ref cm.BufferPool.Value;
            ref Array12<RefCntBuffer> frameBufs = ref cm.BufferPool.Value.FrameBufs;
            ArrayPtr<byte> source = psource;
            CodecErr retcode = 0;
            cm.Error.ErrorCode = CodecErr.Ok;

            if (size == 0)
            {
                // This is used to signal that we are missing frames.
                // We do not know if the missing frame(s) was supposed to update
                // any of the reference buffers, but we act conservative and
                // mark only the last buffer as corrupted.

                if (cm.FrameRefs[0].Idx > 0)
                {
                    cm.FrameRefs[0].Buf.Corrupted = 1;
                }
            }

            ReadyForNewData = 0;

            // Check if the previous frame was a frame without any references to it.
            if (cm.NewFbIdx >= 0 && frameBufs[cm.NewFbIdx].RefCount == 0 &&
                frameBufs[cm.NewFbIdx].Released == 0)
            {
                FrameBuffers.ReleaseFrameBuffer(pool.CbPriv, ref frameBufs[cm.NewFbIdx].RawFrameBuffer);
                frameBufs[cm.NewFbIdx].Released = 1;
            }

            // Find a free frame buffer. Return error if can not find any.
            cm.NewFbIdx = cm.GetFreeFb();
            if (cm.NewFbIdx == RefBuffer.InvalidIdx)
            {
                ReadyForNewData = 1;
                cm.Error.InternalError(CodecErr.MemError, "Unable to find free frame buffer");

                return cm.Error.ErrorCode;
            }

            // Assign a MV array to the frame buffer.
            cm.CurFrame = new Ptr<RefCntBuffer>(ref pool.FrameBufs[cm.NewFbIdx]);

            HoldRefBuf = 0;

            DecodeFrame.Decode(allocator, ref this, new ArrayPtr<byte>(ref source[0], (int)size), out psource);

            SwapFrameBuffers();

            // vpx_clear_system_state();

            if (cm.ShowExistingFrame == 0)
            {
                cm.LastShowFrame = cm.ShowFrame;
                cm.PrevFrame = cm.CurFrame;

                if (cm.PrevFrameMvs.IsNull || cm.PrevFrameMvs.Length != cm.CurFrameMvs.Length)
                {
                    allocator.Free(cm.PrevFrameMvs);
                    cm.PrevFrameMvs = allocator.Allocate<MvRef>(cm.CurFrameMvs.Length);
                }

                cm.CurFrameMvs.AsSpan().CopyTo(cm.PrevFrameMvs.AsSpan());
                if (cm.Seg.Enabled)
                {
                    cm.SwapCurrentAndLastSegMap();
                }
            }

            if (cm.ShowFrame != 0)
            {
                cm.CurShowFrameFbIdx = cm.NewFbIdx;
            }

            // Update progress in frame parallel decode.
            cm.LastWidth = cm.Width;
            cm.LastHeight = cm.Height;
            if (cm.ShowFrame != 0)
            {
                cm.CurrentVideoFrame++;
            }

            return retcode;
        }

        public int GetRawFrame(ref Surface sd)
        {
            ref Vp9Common cm = ref Common;
            int ret = -1;

            if (ReadyForNewData == 1)
            {
                return ret;
            }

            ReadyForNewData = 1;

            if (cm.ShowFrame == 0)
            {
                return ret;
            }

            ReadyForNewData = 1;

            sd = cm.FrameToShow.Value;
            ret = 0;

            return ret;
        }

        public CodecErr Decode(MemoryAllocator allocator, ArrayPtr<byte> data)
        {
            ArrayPtr<byte> dataStart = data;
            CodecErr res;
            Array8<uint> frameSizes = new();
            int frameCount = 0;

            res = Decoder.ParseSuperframeIndex(data, (ulong)data.Length, ref frameSizes, out frameCount);
            if (res != CodecErr.Ok)
            {
                return res;
            }

            // Decode in serial mode.
            if (frameCount > 0)
            {
                for (int i = 0; i < frameCount; ++i)
                {
                    ArrayPtr<byte> dataStartCopy = dataStart;
                    uint frameSize = frameSizes[i];
                    if (frameSize > (uint)dataStart.Length)
                    {
                        return CodecErr.CorruptFrame;
                    }

                    res = ReceiveCompressedData(allocator, frameSize, ref dataStartCopy);
                    if (res != CodecErr.Ok)
                    {
                        return res;
                    }

                    dataStart = dataStart.Slice((int)frameSize);
                }
            }
            else
            {
                while (dataStart.Length != 0)
                {
                    uint frameSize = (uint)dataStart.Length;
                    res = ReceiveCompressedData(allocator, frameSize, ref dataStart);
                    if (res != CodecErr.Ok)
                    {
                        return res;
                    }

                    // Account for suboptimal termination by the encoder.
                    while (dataStart.Length != 0)
                    {
                        byte marker = Decoder.ReadMarker(dataStart);
                        if (marker != 0)
                        {
                            break;
                        }

                        dataStart = dataStart.Slice(1);
                    }
                }
            }

            return res;
        }
    }

    internal static class Decoder
    {
        public static byte ReadMarker(ArrayPtr<byte> data)
        {
            return data[0];
        }

        public static CodecErr ParseSuperframeIndex(ArrayPtr<byte> data, ulong dataSz, ref Array8<uint> sizes, out int count)
        {
            // A chunk ending with a byte matching 0xc0 is an invalid chunk unless
            // it is a super frame index. If the last byte of real video compression
            // data is 0xc0 the encoder must add a 0 byte. If we have the marker but
            // not the associated matching marker byte at the front of the index we have
            // an invalid bitstream and need to return an error.

            byte marker;

            Debug.Assert(dataSz != 0);
            marker = ReadMarker(data.Slice((int)dataSz - 1));
            count = 0;

            if ((marker & 0xe0) == 0xc0)
            {
                uint frames = (uint)(marker & 0x7) + 1;
                uint mag = (uint)((marker >> 3) & 0x3) + 1;
                ulong indexSz = 2 + (mag * frames);

                // This chunk is marked as having a superframe index but doesn't have
                // enough data for it, thus it's an invalid superframe index.
                if (dataSz < indexSz)
                {
                    return CodecErr.CorruptFrame;
                }

                {
                    byte marker2 = ReadMarker(data.Slice((int)(dataSz - indexSz)));

                    // This chunk is marked as having a superframe index but doesn't have
                    // the matching marker byte at the front of the index therefore it's an
                    // invalid chunk.
                    if (marker != marker2)
                    {
                        return CodecErr.CorruptFrame;
                    }
                }

                {
                    // Found a valid superframe index.
                    ArrayPtr<byte> x = data.Slice((int)(dataSz - indexSz + 1));

                    for (int i = 0; i < frames; ++i)
                    {
                        uint thisSz = 0;

                        for (int j = 0; j < mag; ++j)
                        {
                            thisSz |= (uint)x[0] << j * 8;
                            x = x.Slice(1);
                        }

                        sizes[i] = thisSz;
                    }

                    count = (int)frames;
                }
            }

            return CodecErr.Ok;
        }
    }
}