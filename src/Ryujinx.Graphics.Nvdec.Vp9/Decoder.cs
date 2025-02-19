using Ryujinx.Common.Memory;
using Ryujinx.Graphics.Nvdec.Vp9.Common;
using Ryujinx.Graphics.Nvdec.Vp9.Types;
using Ryujinx.Graphics.Video;
using System;

namespace Ryujinx.Graphics.Nvdec.Vp9
{
    public sealed class Decoder : IVp9Decoder
    {
        public bool IsHardwareAccelerated => false;

        private readonly MemoryAllocator _allocator = new();

        public ISurface CreateSurface(int width, int height)
        {
            return new Surface(width, height);
        }

        private static ReadOnlySpan<byte> LiteralToFilter =>
        [
            Constants.EightTapSmooth, Constants.EightTap, Constants.EightTapSharp, Constants.Bilinear
        ];

        public unsafe bool Decode(
            ref Vp9PictureInfo pictureInfo,
            ISurface output,
            ReadOnlySpan<byte> bitstream,
            ReadOnlySpan<Vp9MvRef> mvsIn,
            Span<Vp9MvRef> mvsOut)
        {
            Vp9Common cm = new();

            cm.FrameType = pictureInfo.IsKeyFrame ? FrameType.KeyFrame : FrameType.InterFrame;
            cm.IntraOnly = pictureInfo.IntraOnly;

            cm.Width = output.Width;
            cm.Height = output.Height;
            cm.SubsamplingX = 1;
            cm.SubsamplingY = 1;

            cm.UsePrevFrameMvs = pictureInfo.UsePrevInFindMvRefs;

            cm.RefFrameSignBias = pictureInfo.RefFrameSignBias;

            cm.BaseQindex = pictureInfo.BaseQIndex;
            cm.YDcDeltaQ = pictureInfo.YDcDeltaQ;
            cm.UvAcDeltaQ = pictureInfo.UvAcDeltaQ;
            cm.UvDcDeltaQ = pictureInfo.UvDcDeltaQ;

            cm.Mb.Lossless = pictureInfo.Lossless;
            cm.Mb.Bd = 8;

            cm.TxMode = (TxMode)pictureInfo.TransformMode;

            cm.AllowHighPrecisionMv = pictureInfo.AllowHighPrecisionMv;

            cm.InterpFilter = (byte)pictureInfo.InterpFilter;

            if (cm.InterpFilter != Constants.Switchable)
            {
                cm.InterpFilter = LiteralToFilter[cm.InterpFilter];
            }

            cm.ReferenceMode = (ReferenceMode)pictureInfo.ReferenceMode;

            cm.CompFixedRef = pictureInfo.CompFixedRef;
            cm.CompVarRef = pictureInfo.CompVarRef;

            cm.BitDepth = BitDepth.Bits8;

            cm.Log2TileCols = pictureInfo.Log2TileCols;
            cm.Log2TileRows = pictureInfo.Log2TileRows;

            cm.Seg.Enabled = pictureInfo.SegmentEnabled;
            cm.Seg.UpdateMap = pictureInfo.SegmentMapUpdate;
            cm.Seg.TemporalUpdate = pictureInfo.SegmentMapTemporalUpdate;
            cm.Seg.AbsDelta = (byte)pictureInfo.SegmentAbsDelta;
            cm.Seg.FeatureMask = pictureInfo.SegmentFeatureEnable;
            cm.Seg.FeatureData = pictureInfo.SegmentFeatureData;

            cm.Lf.FilterLevel = pictureInfo.LoopFilterLevel;
            cm.Lf.SharpnessLevel = pictureInfo.LoopFilterSharpnessLevel;
            cm.Lf.ModeRefDeltaEnabled = pictureInfo.ModeRefDeltaEnabled;
            cm.Lf.RefDeltas = pictureInfo.RefDeltas;
            cm.Lf.ModeDeltas = pictureInfo.ModeDeltas;

            cm.Fc = new Ptr<Vp9EntropyProbs>(ref pictureInfo.Entropy);
            cm.Counts = new Ptr<Vp9BackwardUpdates>(ref pictureInfo.BackwardUpdateCounts);

            cm.FrameRefs[0].Buf = (Surface)pictureInfo.LastReference;
            cm.FrameRefs[1].Buf = (Surface)pictureInfo.GoldenReference;
            cm.FrameRefs[2].Buf = (Surface)pictureInfo.AltReference;
            cm.Mb.CurBuf = (Surface)output;

            cm.Mb.SetupBlockPlanes(1, 1);

            int tileCols = 1 << pictureInfo.Log2TileCols;
            int tileRows = 1 << pictureInfo.Log2TileRows;

            // Video usually have only 4 columns, so more threads won't make a difference for those.
            // Try to not take all CPU cores for video decoding.
            int maxThreads = Math.Min(4, Environment.ProcessorCount / 2);

            cm.AllocTileWorkerData(_allocator, tileCols, tileRows, maxThreads);
            cm.AllocContextBuffers(_allocator, output.Width, output.Height);
            cm.InitContextBuffers();
            cm.SetupSegmentationDequant();
            cm.SetupScaleFactors();

            cm.SetMvs(mvsIn);

            if (cm.Lf.FilterLevel != 0 && cm.SkipLoopFilter == 0)
            {
                LoopFilter.LoopFilterFrameInit(ref cm, cm.Lf.FilterLevel);
            }

            fixed (byte* dataPtr = bitstream)
            {
                try
                {
                    if (maxThreads > 1 && tileRows == 1 && tileCols > 1)
                    {
                        DecodeFrame.DecodeTilesMt(ref cm, new ArrayPtr<byte>(dataPtr, bitstream.Length), maxThreads);

                        LoopFilter.LoopFilterFrameMt(
                            ref cm.Mb.CurBuf,
                            ref cm,
                            ref cm.Mb,
                            cm.Lf.FilterLevel,
                            false,
                            false,
                            maxThreads);
                    }
                    else
                    {
                        DecodeFrame.DecodeTiles(ref cm, new ArrayPtr<byte>(dataPtr, bitstream.Length));

                        LoopFilter.LoopFilterFrame(
                            ref cm.Mb.CurBuf,
                            ref cm,
                            ref cm.Mb,
                            cm.Lf.FilterLevel,
                            false,
                            false);
                    }
                }
                catch (InternalErrorException)
                {
                    return false;
                }
            }

            cm.GetMvs(mvsOut);

            cm.FreeTileWorkerData(_allocator);
            cm.FreeContextBuffers(_allocator);

            return true;
        }

        public void Dispose()
        {
            _allocator.Dispose();
        }
    }
}