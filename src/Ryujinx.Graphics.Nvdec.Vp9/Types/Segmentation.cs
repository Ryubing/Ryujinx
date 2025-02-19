using Ryujinx.Common.Memory;
using Ryujinx.Graphics.Video;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Ryujinx.Graphics.Nvdec.Vp9.Types
{
    internal struct Segmentation
    {
        public const int SegmentDeltadata = 0;
        public const int SegmentAbsdata = 1;

        public const int MaxSegments = 8;
        public const int SegTreeProbs = MaxSegments - 1;

        public const int PredictionProbs = 3;

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

        public int GetQIndex(int segmentId, int baseQIndex)
        {
            if (IsSegFeatureActive(segmentId, SegLvlFeatures.AltQ) != 0)
            {
                int data = GetSegData(segmentId, SegLvlFeatures.AltQ);
                int segQIndex = AbsDelta == Constants.SegmentAbsData ? data : baseQIndex + data;
                return Math.Clamp(segQIndex, 0, QuantCommon.MaxQ);
            }

            return baseQIndex;
        }

        public void SetupSegmentation(ref Vp9EntropyProbs fc, ref ReadBitBuffer rb)
        {
            UpdateMap = false;
            UpdateData = 0;

            Enabled = rb.ReadBit() != 0;
            if (!Enabled)
            {
                return;
            }

            // Segmentation map update
            UpdateMap = rb.ReadBit() != 0;
            if (UpdateMap)
            {
                for (int i = 0; i < SegTreeProbs; i++)
                {
                    fc.SegTreeProb[i] = rb.ReadBit() != 0
                        ? (byte)rb.ReadLiteral(8)
                        : (byte)Prob.MaxProb;
                }

                TemporalUpdate = rb.ReadBit() != 0;
                if (TemporalUpdate)
                {
                    for (int i = 0; i < PredictionProbs; i++)
                    {
                        fc.SegPredProb[i] = rb.ReadBit() != 0
                            ? (byte)rb.ReadLiteral(8)
                            : (byte)Prob.MaxProb;
                    }
                }
                else
                {
                    for (int i = 0; i < PredictionProbs; i++)
                    {
                        fc.SegPredProb[i] = Prob.MaxProb;
                    }
                }
            }

            // Segmentation data update
            UpdateData = (byte)rb.ReadBit();
            if (UpdateData != 0)
            {
                AbsDelta = (byte)rb.ReadBit();

                ClearAllSegFeatures();

                for (int i = 0; i < Constants.MaxSegments; i++)
                {
                    for (int j = 0; j < (int)SegLvlFeatures.Max; j++)
                    {
                        int data = 0;
                        int featureEnabled = rb.ReadBit();
                        if (featureEnabled != 0)
                        {
                            EnableSegFeature(i, (SegLvlFeatures)j);
                            data = rb.DecodeUnsignedMax(FeatureDataMax((SegLvlFeatures)j));
                            if (IsSegFeatureSigned((SegLvlFeatures)j) != 0)
                            {
                                data = rb.ReadBit() != 0 ? -data : data;
                            }
                        }

                        SetSegData(i, (SegLvlFeatures)j, data);
                    }
                }
            }
        }
    }
}