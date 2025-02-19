using Ryujinx.Common.Memory;
using Ryujinx.Graphics.Nvdec.Vp9.Common;
using System;
using System.Diagnostics;

namespace Ryujinx.Graphics.Nvdec.Vp9
{
    public static class Prob
    {
        public const int MaxProb = 255;

        private static byte GetProb(uint num, uint den)
        {
            Debug.Assert(den != 0);
            {
                int p = (int)((((ulong)num * 256) + (den >> 1)) / den);
                // (p > 255) ? 255 : (p < 1) ? 1 : p;
                int clippedProb = p | ((255 - p) >> 23) | (p == 0 ? 1 : 0);
                return (byte)clippedProb;
            }
        }

        private static byte GetBinaryProb(uint n0, uint n1)
        {
            uint den = n0 + n1;
            if (den == 0)
            {
                return 128;
            }

            return GetProb(n0, den);
        }

        /* This function assumes prob1 and prob2 are already within [1,255] range. */
        public static byte WeightedProb(int prob1, int prob2, int factor)
        {
            return (byte)BitUtils.RoundPowerOfTwo((prob1 * (256 - factor)) + (prob2 * factor), 8);
        }

        public static byte MergeProbs(byte preProb, ref Array2<uint> ct, uint countSat, uint maxUpdateFactor)
        {
            byte prob = GetBinaryProb(ct[0], ct[1]);
            uint count = Math.Min(ct[0] + ct[1], countSat);
            uint factor = maxUpdateFactor * count / countSat;
            return WeightedProb(preProb, prob, (int)factor);
        }

        // MODE_MV_MAX_UPDATE_FACTOR (128) * count / MODE_MV_COUNT_SAT;
        private static readonly uint[] _countToUpdateFactor =
        [
            0, 6, 12, 19, 25, 32, 38, 44, 51, 57, 64, 70, 76, 83, 89, 96, 102, 108, 115, 121, 128
        ];

        private const int ModeMvCountSat = 20;

        public static byte ModeMvMergeProbs(byte preProb, ref Array2<uint> ct)
        {
            uint den = ct[0] + ct[1];
            if (den == 0)
            {
                return preProb;
            }

            uint count = Math.Min(den, ModeMvCountSat);
            uint factor = _countToUpdateFactor[(int)count];
            byte prob = GetProb(ct[0], den);
            return WeightedProb(preProb, prob, (int)factor);
        }

        private static uint TreeMergeProbsImpl(
            uint i,
            sbyte[] tree,
            ReadOnlySpan<byte> preProbs,
            ReadOnlySpan<uint> counts,
            Span<byte> probs)
        {
            int l = tree[i];
            uint leftCount = l <= 0 ? counts[-l] : TreeMergeProbsImpl((uint)l, tree, preProbs, counts, probs);
            int r = tree[i + 1];
            uint rightCount = r <= 0 ? counts[-r] : TreeMergeProbsImpl((uint)r, tree, preProbs, counts, probs);
            Array2<uint> ct = new();
            ct[0] = leftCount;
            ct[1] = rightCount;
            probs[(int)(i >> 1)] = ModeMvMergeProbs(preProbs[(int)(i >> 1)], ref ct);
            return leftCount + rightCount;
        }

        public static void VpxTreeMergeProbs(sbyte[] tree, ReadOnlySpan<byte> preProbs, ReadOnlySpan<uint> counts,
            Span<byte> probs)
        {
            TreeMergeProbsImpl(0, tree, preProbs, counts, probs);
        }
    }
}