using Ryujinx.Common.Memory;

namespace Ryujinx.Graphics.Video
{
    public ref struct Vp9PictureInfo
    {
        public ISurface LastReference;
        public ISurface GoldenReference;
        public ISurface AltReference;
        public bool IsKeyFrame;
        public bool IntraOnly;
        public Array4<sbyte> RefFrameSignBias;
        public int LoopFilterLevel;
        public int LoopFilterSharpnessLevel;
        public int BaseQIndex;
        public int YDcDeltaQ;
        public int UvDcDeltaQ;
        public int UvAcDeltaQ;
        public bool Lossless;
        public int TransformMode;
        public bool AllowHighPrecisionMv;
        public int InterpFilter;
        public int ReferenceMode;
        public sbyte CompFixedRef;
        public Array2<sbyte> CompVarRef;
        public int Log2TileCols;
        public int Log2TileRows;
        public bool SegmentEnabled;
        public bool SegmentMapUpdate;
        public bool SegmentMapTemporalUpdate;
        public int SegmentAbsDelta;
        public Array8<uint> SegmentFeatureEnable;
        public Array8<Array4<short>> SegmentFeatureData;
        public bool ModeRefDeltaEnabled;
        public bool UsePrevInFindMvRefs;
        public Array4<sbyte> RefDeltas;
        public Array2<sbyte> ModeDeltas;
        public Vp9EntropyProbs Entropy;
        public Vp9BackwardUpdates BackwardUpdateCounts;
    }
}