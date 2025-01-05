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

        public ISurface CreateSurface(int width, int height) => new Surface(width, height);

        private static ReadOnlySpan<byte> LiteralToFilter =>
        [
            Constants.EightTapSmooth,
            Constants.EightTap,
            Constants.EightTapSharp,
            Constants.Bilinear,
        ];

        public unsafe bool Decode(
            ref Vp9PictureInfo pictureInfo,
            ISurface output,
            ReadOnlySpan<byte> bitstream,
            ReadOnlySpan<Vp9MvRef> mvsIn,
            Span<Vp9MvRef> mvsOut)
        {
            Vp9Common cm = new()
            {
                FrameType = pictureInfo.IsKeyFrame ? FrameType.KeyFrame : FrameType.InterFrame,
                IntraOnly = pictureInfo.IntraOnly,

                Width = output.Width,
                Height = output.Height,
                SubsamplingX = 1,
                SubsamplingY = 1,

                UsePrevFrameMvs = pictureInfo.UsePrevInFindMvRefs,

                RefFrameSignBias = pictureInfo.RefFrameSignBias,

                BaseQindex = pictureInfo.BaseQIndex,
                YDcDeltaQ = pictureInfo.YDcDeltaQ,
                UvAcDeltaQ = pictureInfo.UvAcDeltaQ,
                UvDcDeltaQ = pictureInfo.UvDcDeltaQ,
            };

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

            cm.Log2TileCols = pictureInfo.Log2TileCols;
            cm.Log2TileRows = pictureInfo.Log2TileRows;

            cm.Seg.Enabled = pictureInfo.SegmentEnabled;
            cm.Seg.UpdateMap = pictureInfo.SegmentMapUpdate;
            cm.Seg.TemporalUpdate = pictureInfo.SegmentMapTemporalUpdate;
            cm.Seg.AbsDelta = (byte)pictureInfo.SegmentAbsDelta;
            cm.Seg.FeatureMask = pictureInfo.SegmentFeatureEnable;
            cm.Seg.FeatureData = pictureInfo.SegmentFeatureData;

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

            SetMvs(ref cm, mvsIn);

            fixed (byte* dataPtr = bitstream)
            {
                try
                {
                    if (maxThreads > 1 && tileRows == 1 && tileCols > 1)
                    {
                        DecodeFrame.DecodeTilesMt(ref cm, new ArrayPtr<byte>(dataPtr, bitstream.Length), maxThreads);
                    }
                    else
                    {
                        DecodeFrame.DecodeTiles(ref cm, new ArrayPtr<byte>(dataPtr, bitstream.Length));
                    }
                }
                catch (InternalErrorException)
                {
                    return false;
                }
            }

            GetMvs(ref cm, mvsOut);

            cm.FreeTileWorkerData(_allocator);
            cm.FreeContextBuffers(_allocator);

            return true;
        }

        private static void SetMvs(ref Vp9Common cm, ReadOnlySpan<Vp9MvRef> mvs)
        {
            if (mvs.Length > cm.PrevFrameMvs.Length)
            {
                throw new ArgumentException($"Size mismatch, expected: {cm.PrevFrameMvs.Length}, but got: {mvs.Length}.");
            }

            for (int i = 0; i < mvs.Length; i++)
            {
                ref var mv = ref cm.PrevFrameMvs[i];

                mv.Mv[0].Row = mvs[i].Mvs[0].Row;
                mv.Mv[0].Col = mvs[i].Mvs[0].Col;
                mv.Mv[1].Row = mvs[i].Mvs[1].Row;
                mv.Mv[1].Col = mvs[i].Mvs[1].Col;

                mv.RefFrame[0] = (sbyte)mvs[i].RefFrames[0];
                mv.RefFrame[1] = (sbyte)mvs[i].RefFrames[1];
            }
        }

        private static void GetMvs(ref Vp9Common cm, Span<Vp9MvRef> mvs)
        {
            if (mvs.Length > cm.CurFrameMvs.Length)
            {
                throw new ArgumentException($"Size mismatch, expected: {cm.CurFrameMvs.Length}, but got: {mvs.Length}.");
            }

            for (int i = 0; i < mvs.Length; i++)
            {
                ref var mv = ref cm.CurFrameMvs[i];

                mvs[i].Mvs[0].Row = mv.Mv[0].Row;
                mvs[i].Mvs[0].Col = mv.Mv[0].Col;
                mvs[i].Mvs[1].Row = mv.Mv[1].Row;
                mvs[i].Mvs[1].Col = mv.Mv[1].Col;

                mvs[i].RefFrames[0] = mv.RefFrame[0];
                mvs[i].RefFrames[1] = mv.RefFrame[1];
            }
        }

        public void Dispose() => _allocator.Dispose();
    }
}
