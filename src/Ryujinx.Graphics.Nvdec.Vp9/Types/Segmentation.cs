using Ryujinx.Common.Memory;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Ryujinx.Graphics.Nvdec.Vp9.Types
{
    internal struct Segmentation
    {
        private static readonly int[] _segFeatureDataSigned = [1, 1, 0, 0];
        private static readonly int[] _segFeatureDataMax = [QuantCommon.MaxQ, Vp9.LoopFilter.MaxLoopFilter, 3, 0];

        public bool Enabled;
        public bool UpdateMap;
        public byte UpdateData;
        public byte AbsDelta;
        public bool TemporalUpdate;

        public Array8<Array4<short>> FeatureData;
        public Array8<uint> FeatureMask;
        public int AqAvOffset;

        public static byte GetPredProbSegId(ref Array3<byte> segPredProbs, ref MacroBlockD xd)
        {
            return segPredProbs[xd.GetPredContextSegId()];
        }

        public void ClearAllSegFeatures()
        {
            MemoryMarshal.CreateSpan(ref FeatureData[0][0], 8 * 4).Clear();
            MemoryMarshal.CreateSpan(ref FeatureMask[0], 8).Clear();
            AqAvOffset = 0;
        }

        internal void EnableSegFeature(int segmentId, SegLvlFeatures featureId)
        {
            FeatureMask[segmentId] |= 1u << (int)featureId;
        }

        internal static int FeatureDataMax(SegLvlFeatures featureId)
        {
            return _segFeatureDataMax[(int)featureId];
        }

        internal static int IsSegFeatureSigned(SegLvlFeatures featureId)
        {
            return _segFeatureDataSigned[(int)featureId];
        }

        internal void SetSegData(int segmentId, SegLvlFeatures featureId, int segData)
        {
            Debug.Assert(segData <= _segFeatureDataMax[(int)featureId]);
            if (segData < 0)
            {
                Debug.Assert(_segFeatureDataSigned[(int)featureId] != 0);
                Debug.Assert(-segData <= _segFeatureDataMax[(int)featureId]);
            }

            FeatureData[segmentId][(int)featureId] = (short)segData;
        }

        internal int IsSegFeatureActive(int segmentId, SegLvlFeatures featureId)
        {
            return Enabled && (FeatureMask[segmentId] & (1 << (int)featureId)) != 0 ? 1 : 0;
        }

        internal short GetSegData(int segmentId, SegLvlFeatures featureId)
        {
            return FeatureData[segmentId][(int)featureId];
        }
    }
}
