using Ryujinx.Common.Memory;
using Ryujinx.Graphics.Video;

namespace Ryujinx.Graphics.Nvdec.Types.Vp9
{
    struct PictureInfo
    {
#pragma warning disable CS0649 // Field is never assigned to
        public Array12<uint> Unknown0;
        public uint BitstreamSize;
        public uint IsEncrypted;
        public uint Unknown38;
        public uint Reserved3C;
        public uint BlockLayout; // Not supported on T210
        public uint WorkBufferSizeShr8;
        public FrameSize LastFrameSize;
        public FrameSize GoldenFrameSize;
        public FrameSize AltFrameSize;
        public FrameSize CurrentFrameSize;
        public FrameFlags Flags;
        public Array4<sbyte> RefFrameSignBias;
        public byte FirstLevel;
        public byte SharpnessLevel;
        public byte BaseQIndex;
        public byte YDcDeltaQ;
        public byte UvAcDeltaQ;
        public byte UvDcDeltaQ;
        public byte Lossless;
        public byte TxMode;
        public byte AllowHighPrecisionMv;
        public byte InterpFilter;
        public byte ReferenceMode;
        public sbyte CompFixedRef;
        public Array2<sbyte> CompVarRef;
        public byte Log2TileCols;
        public byte Log2TileRows;
        public Segmentation Seg;
        public LoopFilter Lf;
        public byte PaddingEB;
        public uint WorkBufferSizeShr8New; // Not supported on T210
        public uint SurfaceParams; // Not supported on T210
        public uint UnknownF4;
        public uint UnknownF8;
        public uint UnknownFC;
#pragma warning restore CS0649

        public readonly uint BitDepth => (SurfaceParams >> 1) & 0xf;

        public Vp9PictureInfo Convert()
        {
            return new Vp9PictureInfo()
            {
                IsKeyFrame = Flags.HasFlag(FrameFlags.IsKeyFrame),
                IntraOnly = Flags.HasFlag(FrameFlags.IntraOnly),
                UsePrevInFindMvRefs =
                    !Flags.HasFlag(FrameFlags.ErrorResilientMode) &&
                    !Flags.HasFlag(FrameFlags.FrameSizeChanged) &&
                    !Flags.HasFlag(FrameFlags.IntraOnly) &&
                    Flags.HasFlag(FrameFlags.LastShowFrame) &&
                    !Flags.HasFlag(FrameFlags.LastFrameIsKeyFrame),
                RefFrameSignBias = RefFrameSignBias,
                LoopFilterLevel = FirstLevel,
                LoopFilterSharpnessLevel = SharpnessLevel,
                BaseQIndex = BaseQIndex,
                YDcDeltaQ = YDcDeltaQ,
                UvDcDeltaQ = UvDcDeltaQ,
                UvAcDeltaQ = UvAcDeltaQ,
                Lossless = Lossless != 0,
                TransformMode = TxMode,
                AllowHighPrecisionMv = AllowHighPrecisionMv != 0,
                InterpFilter = InterpFilter,
                ReferenceMode = ReferenceMode,
                CompFixedRef = CompFixedRef,
                CompVarRef = CompVarRef,
                Log2TileCols = Log2TileCols,
                Log2TileRows = Log2TileRows,
                SegmentEnabled = Seg.Enabled != 0,
                SegmentMapUpdate = Seg.UpdateMap != 0,
                SegmentMapTemporalUpdate = Seg.TemporalUpdate != 0,
                SegmentAbsDelta = Seg.AbsDelta,
                SegmentFeatureEnable = Seg.FeatureMask,
                SegmentFeatureData = Seg.FeatureData,
                ModeRefDeltaEnabled = Lf.ModeRefDeltaEnabled != 0,
                RefDeltas = Lf.RefDeltas,
                ModeDeltas = Lf.ModeDeltas,
            };
        }
    }
}
