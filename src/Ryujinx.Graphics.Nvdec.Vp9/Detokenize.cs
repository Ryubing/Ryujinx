using Ryujinx.Common.Memory;
using Ryujinx.Graphics.Nvdec.Vp9.Dsp;
using Ryujinx.Graphics.Nvdec.Vp9.Types;
using Ryujinx.Graphics.Video;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using static Ryujinx.Graphics.Nvdec.Vp9.Dsp.InvTxfm;

namespace Ryujinx.Graphics.Nvdec.Vp9
{
    internal static class Detokenize
    {
        private const int EobContextNode = 0;
        private const int ZeroContextNode = 1;
        private const int OneContextNode = 2;

        private static int GetCoefContext(ReadOnlySpan<short> neighbors, ReadOnlySpan<byte> tokenCache, int c)
        {
            const int MaxNeighbors = 2;

            return (1 + tokenCache[neighbors[(MaxNeighbors * c) + 0]] +
                    tokenCache[neighbors[(MaxNeighbors * c) + 1]]) >> 1;
        }

        private static int DecodeCoefs(
            ref MacroBlockD xd,
            PlaneType type,
            Span<int> dqcoeff,
            TxSize txSize,
            ref Array2<short> dq,
            int ctx,
            ReadOnlySpan<short> scan,
            ReadOnlySpan<short> nb,
            ref Reader r)
        {
            ref Vp9BackwardUpdates counts = ref xd.Counts.Value;
            int maxEob = 16 << ((int)txSize << 1);
            ref Vp9EntropyProbs fc = ref xd.Fc.Value;
            int refr = xd.Mi[0].Value.IsInterBlock() ? 1 : 0;
            int band, c = 0;
            ref Array6<Array6<Array3<byte>>> coefProbs = ref fc.CoefProbs[(int)txSize][(int)type][refr];
            Span<byte> tokenCache = stackalloc byte[32 * 32];
            ReadOnlySpan<byte> bandTranslate = Luts.GetBandTranslate(txSize);
            int dqShift = txSize == TxSize.Tx32X32 ? 1 : 0;
            int v;
            short dqv = dq[0];
            ReadOnlySpan<byte> cat6Prob = xd.Bd == 12
                ? Luts.Cat6ProbHigh12
                : xd.Bd == 10
                    ? Luts.Cat6ProbHigh12.Slice(2)
                    : Luts.Cat6Prob;
            int cat6Bits = xd.Bd == 12 ? 18 : xd.Bd == 10 ? 16 : 14;
            // Keep value, range, and count as locals.  The compiler produces better
            // results with the locals than using r directly.
            ulong value = r.Value;
            uint range = r.Range;
            int count = r.Count;

            while (c < maxEob)
            {
                int val = -1;
                band = bandTranslate[0];
                bandTranslate = bandTranslate.Slice(1);
                ref Array3<byte> prob = ref coefProbs[band][ctx];
                if (!xd.Counts.IsNull)
                {
                    ++counts.EobBranch[(int)txSize][(int)type][refr][band][ctx];
                }

                if (r.ReadBool(prob[EobContextNode], ref value, ref count, ref range) == 0)
                {
                    if (!xd.Counts.IsNull)
                    {
                        ++counts.Coef[(int)txSize][(int)type][refr][band][ctx][Constants.EobModelToken];
                    }

                    break;
                }

                while (r.ReadBool(prob[ZeroContextNode], ref value, ref count, ref range) == 0)
                {
                    if (!xd.Counts.IsNull)
                    {
                        ++counts.Coef[(int)txSize][(int)type][refr][band][ctx][Constants.ZeroToken];
                    }

                    dqv = dq[1];
                    tokenCache[scan[c]] = 0;
                    ++c;
                    if (c >= maxEob)
                    {
                        r.Value = value;
                        r.Range = range;
                        r.Count = count;
                        return c; // Zero tokens at the end (no eob token)
                    }

                    ctx = GetCoefContext(nb, tokenCache, c);
                    band = bandTranslate[0];
                    bandTranslate = bandTranslate.Slice(1);
                    prob = ref coefProbs[band][ctx];
                }

                if (r.ReadBool(prob[OneContextNode], ref value, ref count, ref range) != 0)
                {
                    ReadOnlySpan<byte> p = Luts.Pareto8Full[prob[Constants.PivotNode] - 1];
                    if (!xd.Counts.IsNull)
                    {
                        ++counts.Coef[(int)txSize][(int)type][refr][band][ctx][Constants.TwoToken];
                    }

                    if (r.ReadBool(p[0], ref value, ref count, ref range) != 0)
                    {
                        if (r.ReadBool(p[3], ref value, ref count, ref range) != 0)
                        {
                            tokenCache[scan[c]] = 5;
                            if (r.ReadBool(p[5], ref value, ref count, ref range) != 0)
                            {
                                if (r.ReadBool(p[7], ref value, ref count, ref range) != 0)
                                {
                                    val = Constants.Cat6MinVal + r.ReadCoeff(cat6Prob, cat6Bits, ref value,
                                        ref count, ref range);
                                }
                                else
                                {
                                    val = Constants.Cat5MinVal + r.ReadCoeff(Luts.Cat5Prob, 5, ref value,
                                        ref count, ref range);
                                }
                            }
                            else if (r.ReadBool(p[6], ref value, ref count, ref range) != 0)
                            {
                                val = Constants.Cat4MinVal + r.ReadCoeff(Luts.Cat4Prob, 4, ref value, ref count,
                                    ref range);
                            }
                            else
                            {
                                val = Constants.Cat3MinVal + r.ReadCoeff(Luts.Cat3Prob, 3, ref value, ref count,
                                    ref range);
                            }
                        }
                        else
                        {
                            tokenCache[scan[c]] = 4;
                            if (r.ReadBool(p[4], ref value, ref count, ref range) != 0)
                            {
                                val = Constants.Cat2MinVal + r.ReadCoeff(Luts.Cat2Prob, 2, ref value, ref count,
                                    ref range);
                            }
                            else
                            {
                                val = Constants.Cat1MinVal + r.ReadCoeff(Luts.Cat1Prob, 1, ref value, ref count,
                                    ref range);
                            }
                        }

                        // Val may use 18-bits
                        v = (int)(((long)val * dqv) >> dqShift);
                    }
                    else
                    {
                        if (r.ReadBool(p[1], ref value, ref count, ref range) != 0)
                        {
                            tokenCache[scan[c]] = 3;
                            v = ((3 + r.ReadBool(p[2], ref value, ref count, ref range)) * dqv) >> dqShift;
                        }
                        else
                        {
                            tokenCache[scan[c]] = 2;
                            v = (2 * dqv) >> dqShift;
                        }
                    }
                }
                else
                {
                    if (!xd.Counts.IsNull)
                    {
                        ++counts.Coef[(int)txSize][(int)type][refr][band][ctx][Constants.OneToken];
                    }

                    tokenCache[scan[c]] = 1;
                    v = dqv >> dqShift;
                }

                dqcoeff[scan[c]] = (int)HighbdCheckRange(r.ReadBool(128, ref value, ref count, ref range) != 0 ? -v : v,
                    xd.Bd);
                ++c;
                ctx = GetCoefContext(nb, tokenCache, c);
                dqv = dq[1];
            }

            r.Value = value;
            r.Range = range;
            r.Count = count;
            return c;
        }

        private static void GetCtxShift(ref MacroBlockD xd, ref int ctxShiftA, ref int ctxShiftL, int x, int y,
            uint txSizeInBlocks)
        {
            if (xd.MaxBlocksWide != 0)
            {
                if (txSizeInBlocks + x > xd.MaxBlocksWide)
                {
                    ctxShiftA = (int)(txSizeInBlocks - (xd.MaxBlocksWide - x)) * 8;
                }
            }

            if (xd.MaxBlocksHigh != 0)
            {
                if (txSizeInBlocks + y > xd.MaxBlocksHigh)
                {
                    ctxShiftL = (int)(txSizeInBlocks - (xd.MaxBlocksHigh - y)) * 8;
                }
            }
        }

        private static PlaneType GetPlaneType(int plane)
        {
            return (PlaneType)(plane > 0 ? 1 : 0);
        }

        public static int DecodeBlockTokens(
            ref TileWorkerData twd,
            int plane,
            Luts.ScanOrder sc,
            int x,
            int y,
            TxSize txSize,
            int segId)
        {
            ref Reader r = ref twd.BitReader;
            ref MacroBlockD xd = ref twd.Xd;
            ref MacroBlockDPlane pd = ref xd.Plane[plane];
            ref Array2<short> dequant = ref pd.SegDequant[segId];
            int eob;
            Span<sbyte> a = pd.AboveContext.AsSpan().Slice(x);
            Span<sbyte> l = pd.LeftContext.AsSpan().Slice(y);
            int ctx;
            int ctxShiftA = 0;
            int ctxShiftL = 0;

            switch (txSize)
            {
                case TxSize.Tx4X4:
                    ctx = a[0] != 0 ? 1 : 0;
                    ctx += l[0] != 0 ? 1 : 0;
                    eob = DecodeCoefs(
                        ref xd,
                        GetPlaneType(plane),
                        pd.DqCoeff.AsSpan(),
                        txSize,
                        ref dequant,
                        ctx,
                        sc.Scan,
                        sc.Neighbors,
                        ref r);
                    a[0] = l[0] = (sbyte)(eob > 0 ? 1 : 0);
                    break;
                case TxSize.Tx8X8:
                    GetCtxShift(ref xd, ref ctxShiftA, ref ctxShiftL, x, y, 1 << (int)TxSize.Tx8X8);
                    ctx = MemoryMarshal.Cast<sbyte, ushort>(a)[0] != 0 ? 1 : 0;
                    ctx += MemoryMarshal.Cast<sbyte, ushort>(l)[0] != 0 ? 1 : 0;
                    eob = DecodeCoefs(
                        ref xd,
                        GetPlaneType(plane),
                        pd.DqCoeff.AsSpan(),
                        txSize,
                        ref dequant,
                        ctx,
                        sc.Scan,
                        sc.Neighbors,
                        ref r);
                    MemoryMarshal.Cast<sbyte, ushort>(a)[0] = (ushort)((eob > 0 ? 0x0101 : 0) >> ctxShiftA);
                    MemoryMarshal.Cast<sbyte, ushort>(l)[0] = (ushort)((eob > 0 ? 0x0101 : 0) >> ctxShiftL);
                    break;
                case TxSize.Tx16X16:
                    GetCtxShift(ref xd, ref ctxShiftA, ref ctxShiftL, x, y, 1 << (int)TxSize.Tx16X16);
                    ctx = MemoryMarshal.Cast<sbyte, uint>(a)[0] != 0 ? 1 : 0;
                    ctx += MemoryMarshal.Cast<sbyte, uint>(l)[0] != 0 ? 1 : 0;
                    eob = DecodeCoefs(
                        ref xd,
                        GetPlaneType(plane),
                        pd.DqCoeff.AsSpan(),
                        txSize,
                        ref dequant,
                        ctx,
                        sc.Scan,
                        sc.Neighbors,
                        ref r);
                    MemoryMarshal.Cast<sbyte, uint>(a)[0] = (uint)((eob > 0 ? 0x01010101 : 0) >> ctxShiftA);
                    MemoryMarshal.Cast<sbyte, uint>(l)[0] = (uint)((eob > 0 ? 0x01010101 : 0) >> ctxShiftL);
                    break;
                case TxSize.Tx32X32:
                    GetCtxShift(ref xd, ref ctxShiftA, ref ctxShiftL, x, y, 1 << (int)TxSize.Tx32X32);
                    // NOTE: Casting to ulong here is safe because the default memory
                    // alignment is at least 8 bytes and the Tx32x32 is aligned on 8 byte
                    // boundaries.
                    ctx = MemoryMarshal.Cast<sbyte, ulong>(a)[0] != 0 ? 1 : 0;
                    ctx += MemoryMarshal.Cast<sbyte, ulong>(l)[0] != 0 ? 1 : 0;
                    eob = DecodeCoefs(
                        ref xd,
                        GetPlaneType(plane),
                        pd.DqCoeff.AsSpan(),
                        txSize,
                        ref dequant,
                        ctx,
                        sc.Scan,
                        sc.Neighbors,
                        ref r);
                    MemoryMarshal.Cast<sbyte, ulong>(a)[0] = (eob > 0 ? 0x0101010101010101UL : 0) >> ctxShiftA;
                    MemoryMarshal.Cast<sbyte, ulong>(l)[0] = (eob > 0 ? 0x0101010101010101UL : 0) >> ctxShiftL;
                    break;
                default:
                    Debug.Assert(false, "Invalid transform size.");
                    eob = 0;
                    break;
            }

            return eob;
        }
    }
}