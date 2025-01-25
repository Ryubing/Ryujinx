using Ryujinx.Common.Memory;

namespace Ryujinx.Graphics.Nvdec.Vp9.Types
{
    internal struct LoopFilter
    {
        public int FilterLevel;
        public int LastFiltLevel;

        public int SharpnessLevel;
        public int LastSharpnessLevel;

        public bool ModeRefDeltaEnabled;
        public bool ModeRefDeltaUpdate;

        // 0 = Intra, Last, GF, ARF
        public Array4<sbyte> RefDeltas;
        public Array4<sbyte> LastRefDeltas;

        // 0 = ZERO_MV, MV
        public Array2<sbyte> ModeDeltas;
        public Array2<sbyte> LastModeDeltas;

        public ArrayPtr<LoopFilterMask> Lfm;
        public int LfmStride;

        public void SetDefaultLfDeltas()
        {
            ModeRefDeltaEnabled = true;
            ModeRefDeltaUpdate = true;

            RefDeltas[Constants.IntraFrame] = 1;
            RefDeltas[Constants.LastFrame] = 0;
            RefDeltas[Constants.GoldenFrame] = -1;
            RefDeltas[Constants.AltRefFrame] = -1;
            ModeDeltas[0] = 0;
            ModeDeltas[1] = 0;
        }
    }
}