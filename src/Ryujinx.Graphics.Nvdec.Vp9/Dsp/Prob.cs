using Ryujinx.Graphics.Nvdec.Vp9.Common;
using System;
using System.Diagnostics;

namespace Ryujinx.Graphics.Nvdec.Vp9.Dsp
{
    internal static class Prob
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

        /* This function assumes prob1 and prob2 are already within [1,255] range. */
        public static byte WeightedProb(int prob1, int prob2, int factor)
        {
            return (byte)BitUtils.RoundPowerOfTwo((prob1 * (256 - factor)) + (prob2 * factor), 8);
        }

        // MODE_MV_MAX_UPDATE_FACTOR (128) * count / MODE_MV_COUNT_SAT;
        private static readonly uint[] _countToUpdateFactor =
        [
            0, 6, 12, 19, 25, 32, 38, 44, 51, 57, 64, 70, 76, 83, 89, 96, 102, 108, 115, 121, 128
        ];

        private const int ModeMvCountSat = 20;

        public static byte ModeMvMergeProbs(byte preProb, uint ct0, uint ct1)
        {
            uint den = ct0 + ct1;
            if (den == 0)
            {
                return preProb;
            }

            uint count = Math.Min(den, ModeMvCountSat);
            uint factor = _countToUpdateFactor[(int)count];
            byte prob = GetProb(ct0, den);
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
            probs[(int)(i >> 1)] = ModeMvMergeProbs(preProbs[(int)(i >> 1)], leftCount, rightCount);
            return leftCount + rightCount;
        }

        public static void TreeMergeProbs(sbyte[] tree, ReadOnlySpan<byte> preProbs, ReadOnlySpan<uint> counts,
            Span<byte> probs)
        {
            TreeMergeProbsImpl(0, tree, preProbs, counts, probs);
        }
    }
}