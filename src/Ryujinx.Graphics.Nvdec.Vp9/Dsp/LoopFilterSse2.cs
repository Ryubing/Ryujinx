using Ryujinx.Common.Memory;
using System;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace Ryujinx.Graphics.Nvdec.Vp9.Dsp
{
    internal static class LoopFilterSse2
    {
        private static Vector128<byte> AbsDiff(Vector128<byte> a, Vector128<byte> b)
        {
            return Sse2.Or(Sse2.SubtractSaturate(a, b), Sse2.SubtractSaturate(b, a));
        }

        private static void FilterHevMask(
            Vector128<byte> q1P1,
            Vector128<byte> q0P0,
            Vector128<byte> p3P2,
            Vector128<byte> p2P1,
            Vector128<byte> p1P0,
            Vector128<byte> q3Q2,
            Vector128<byte> q2Q1,
            Vector128<byte> q1Q0,
            Vector128<byte> limitV,
            Vector128<byte> threshV,
            out Vector128<byte> hev,
            out Vector128<byte> mask)
        {
            /* (abs(q1 - q0), abs(p1 - p0) */
            Vector128<byte> flat = AbsDiff(q1P1, q0P0);
            /* abs(p1 - q1), abs(p0 - q0) */
            Vector128<byte> absP1Q1P0Q0 = AbsDiff(p1P0, q1Q0);
            Vector128<byte> absP0Q0, absP1Q1, work;

            /* const uint8_t hev = hev_mask(thresh, *op1, *op0, *oq0, *oq1); */
            hev = Sse2.UnpackLow(Sse2.Max(flat, Sse2.ShiftRightLogical128BitLane(flat, 8)), Vector128<byte>.Zero);
            hev = Sse2.CompareGreaterThan(hev.AsInt16(), threshV.AsInt16()).AsByte();
            hev = Sse2.PackSignedSaturate(hev.AsInt16(), hev.AsInt16()).AsByte();

            /* const int8_t mask = filter_mask(*limit, *blimit, p3, p2, p1, p0, q0, q1, q2, q3); */
            absP0Q0 = Sse2.AddSaturate(absP1Q1P0Q0, absP1Q1P0Q0); /* abs(p0 - q0) * 2 */
            absP1Q1 = Sse2.UnpackHigh(absP1Q1P0Q0, absP1Q1P0Q0); /* abs(p1 - q1) */
            absP1Q1 = Sse2.ShiftRightLogical(absP1Q1.AsInt16(), 9).AsByte();
            absP1Q1 = Sse2.PackSignedSaturate(absP1Q1.AsInt16(), absP1Q1.AsInt16()).AsByte(); /* abs(p1 - q1) / 2 */
            /* abs(p0 - q0) * 2 + abs(p1 - q1) / 2 */
            mask = Sse2.AddSaturate(absP0Q0, absP1Q1);
            /* abs(p3 - p2), abs(p2 - p1) */
            work = AbsDiff(p3P2, p2P1);
            flat = Sse2.Max(work, flat);
            /* abs(q3 - q2), abs(q2 - q1) */
            work = AbsDiff(q3Q2, q2Q1);
            flat = Sse2.Max(work, flat);
            flat = Sse2.Max(flat, Sse2.ShiftRightLogical128BitLane(flat, 8));
            mask = Sse2.UnpackLow(mask.AsInt64(), flat.AsInt64()).AsByte();
            mask = Sse2.SubtractSaturate(mask, limitV);
            mask = Sse2.CompareEqual(mask, Vector128<byte>.Zero);
            mask = Sse2.And(mask, Sse2.ShiftRightLogical128BitLane(mask, 8));
        }

        private static void Filter4(
            Vector128<byte> p1P0,
            Vector128<byte> q1Q0,
            Vector128<byte> hev,
            Vector128<byte> mask,
            Vector128<byte> ff,
            out Vector128<byte> ps1Ps0,
            out Vector128<byte> qs1Qs0)
        {
            Vector128<byte> t3T4 = Vector128.Create(
                4, 4, 4, 4,
                4, 4, 4, 4,
                3, 3, 3, 3,
                3, 3, 3, (byte)3);
            Vector128<byte> t80 = Vector128.Create((byte)0x80);
            Vector128<byte> filter, filter2Filter1, work;

            ps1Ps0 = Sse2.Xor(p1P0, t80); /* ^ 0x80 */
            qs1Qs0 = Sse2.Xor(q1Q0, t80);

            /* int8_t filter = signed_char_clamp(ps1 - qs1) & hev; */
            work = Sse2.SubtractSaturate(ps1Ps0.AsSByte(), qs1Qs0.AsSByte()).AsByte();
            filter = Sse2.And(Sse2.ShiftRightLogical128BitLane(work, 8), hev);
            /* filter = signed_char_clamp(filter + 3 * (qs0 - ps0)) & mask; */
            filter = Sse2.SubtractSaturate(filter.AsSByte(), work.AsSByte()).AsByte();
            filter = Sse2.SubtractSaturate(filter.AsSByte(), work.AsSByte()).AsByte();
            filter = Sse2.SubtractSaturate(filter.AsSByte(), work.AsSByte()).AsByte(); /* + 3 * (qs0 - ps0) */
            filter = Sse2.And(filter, mask); /* & mask */
            filter = Sse2.UnpackLow(filter.AsInt64(), filter.AsInt64()).AsByte();

            /* filter1 = signed_char_clamp(filter + 4) >> 3; */
            /* filter2 = signed_char_clamp(filter + 3) >> 3; */
            filter2Filter1 = Sse2.AddSaturate(filter.AsSByte(), t3T4.AsSByte()).AsByte(); /* signed_char_clamp */
            filter = Sse2.UnpackHigh(filter2Filter1, filter2Filter1);
            filter2Filter1 = Sse2.UnpackLow(filter2Filter1, filter2Filter1);
            filter2Filter1 = Sse2.ShiftRightArithmetic(filter2Filter1.AsInt16(), 11).AsByte(); /* >> 3 */
            filter = Sse2.ShiftRightArithmetic(filter.AsInt16(), 11).AsByte(); /* >> 3 */
            filter2Filter1 = Sse2.PackSignedSaturate(filter2Filter1.AsInt16(), filter.AsInt16()).AsByte();

            /* filter = ROUND_POWER_OF_TWO(filter1, 1) & ~hev; */
            filter = Sse2.SubtractSaturate(filter2Filter1.AsSByte(), ff.AsSByte()).AsByte(); /* + 1 */
            filter = Sse2.UnpackLow(filter, filter);
            filter = Sse2.ShiftRightArithmetic(filter.AsInt16(), 9).AsByte(); /* round */
            filter = Sse2.PackSignedSaturate(filter.AsInt16(), filter.AsInt16()).AsByte();
            filter = Sse2.AndNot(hev, filter);

            hev = Sse2.UnpackHigh(filter2Filter1.AsInt64(), filter.AsInt64()).AsByte();
            filter2Filter1 = Sse2.UnpackLow(filter2Filter1.AsInt64(), filter.AsInt64()).AsByte();

            /* signed_char_clamp(qs1 - filter), signed_char_clamp(qs0 - filter1) */
            qs1Qs0 = Sse2.SubtractSaturate(qs1Qs0.AsSByte(), filter2Filter1.AsSByte()).AsByte();
            /* signed_char_clamp(ps1 + filter), signed_char_clamp(ps0 + filter2) */
            ps1Ps0 = Sse2.AddSaturate(ps1Ps0.AsSByte(), hev.AsSByte()).AsByte();
            qs1Qs0 = Sse2.Xor(qs1Qs0, t80); /* ^ 0x80 */
            ps1Ps0 = Sse2.Xor(ps1Ps0, t80); /* ^ 0x80 */
        }

        public static unsafe void LpfHorizontal4(
            ArrayPtr<byte> s,
            int pitch,
            ReadOnlySpan<byte> blimit,
            ReadOnlySpan<byte> limit,
            ReadOnlySpan<byte> thresh)
        {
            Vector128<byte> zero = Vector128<byte>.Zero;
            Vector128<byte> limitV, threshV;

            fixed (byte* pBLimit = blimit, pLimit = limit, pThresh = thresh)
            {
                limitV = Sse2.UnpackLow(
                    Sse2.LoadScalarVector128((long*)pBLimit),
                    Sse2.LoadScalarVector128((long*)pLimit)).AsByte();
                threshV = Sse2.UnpackLow(Sse2.LoadScalarVector128((long*)pThresh).AsByte(), zero);
            }

            Vector128<byte> ff = Sse2.CompareEqual(zero, zero);
            Vector128<byte> q1P1, q0P0, p3P2, p2P1, p1P0, q3Q2, q2Q1, q1Q0, ps1Ps0, qs1Qs0;
            Vector128<byte> mask, hev;

            p3P2 = Sse2.UnpackLow(Sse2.LoadScalarVector128((long*)(s.ToPointer() - (3 * pitch))),
                Sse2.LoadScalarVector128((long*)(s.ToPointer() - (4 * pitch)))).AsByte();
            q1P1 = Sse2.UnpackLow(Sse2.LoadScalarVector128((long*)(s.ToPointer() - (2 * pitch))),
                Sse2.LoadScalarVector128((long*)(s.ToPointer() + (1 * pitch)))).AsByte();
            q0P0 = Sse2.UnpackLow(Sse2.LoadScalarVector128((long*)(s.ToPointer() - (1 * pitch))),
                Sse2.LoadScalarVector128((long*)(s.ToPointer() + (0 * pitch)))).AsByte();
            q3Q2 = Sse2.UnpackLow(Sse2.LoadScalarVector128((long*)(s.ToPointer() + (2 * pitch))),
                Sse2.LoadScalarVector128((long*)(s.ToPointer() + (3 * pitch)))).AsByte();
            p1P0 = Sse2.UnpackLow(q0P0.AsInt64(), q1P1.AsInt64()).AsByte();
            p2P1 = Sse2.UnpackLow(q1P1.AsInt64(), p3P2.AsInt64()).AsByte();
            q1Q0 = Sse2.UnpackHigh(q0P0.AsInt64(), q1P1.AsInt64()).AsByte();
            q2Q1 = Sse2.UnpackLow(Sse2.ShiftRightLogical128BitLane(q1P1, 8).AsInt64(), q3Q2.AsInt64()).AsByte();

            FilterHevMask(q1P1, q0P0, p3P2, p2P1, p1P0, q3Q2, q2Q1, q1Q0, limitV, threshV, out hev, out mask);
            Filter4(p1P0, q1Q0, hev, mask, ff, out ps1Ps0, out qs1Qs0);

            Sse.StoreHigh((float*)(s.ToPointer() - (2 * pitch)), ps1Ps0.AsSingle()); // *op1
            Sse2.StoreScalar((long*)(s.ToPointer() - (1 * pitch)), ps1Ps0.AsInt64()); // *op0
            Sse2.StoreScalar((long*)(s.ToPointer() + (0 * pitch)), qs1Qs0.AsInt64()); // *oq0
            Sse.StoreHigh((float*)(s.ToPointer() + (1 * pitch)), qs1Qs0.AsSingle()); // *oq1
        }

        public static unsafe void LpfVertical4(
            ArrayPtr<byte> s,
            int pitch,
            ReadOnlySpan<byte> blimit,
            ReadOnlySpan<byte> limit,
            ReadOnlySpan<byte> thresh)
        {
            Vector128<byte> zero = Vector128<byte>.Zero;
            Vector128<byte> limitV, threshV;

            fixed (byte* pBLimit = blimit, pLimit = limit, pThresh = thresh)
            {
                limitV = Sse2.UnpackLow(
                    Sse2.LoadScalarVector128((long*)pBLimit).AsInt64(),
                    Sse2.LoadScalarVector128((long*)pLimit).AsInt64()).AsByte();
                threshV = Sse2.UnpackLow(Sse2.LoadScalarVector128((long*)pThresh).AsByte(), zero);
            }

            Vector128<byte> ff = Sse2.CompareEqual(zero, zero);
            Vector128<byte> x0, x1, x2, x3;
            Vector128<byte> q1P1, q0P0, p3P2, p2P1, p1P0, q3Q2, q2Q1, q1Q0, ps1Ps0, qs1Qs0;
            Vector128<byte> mask, hev;

            // 00 10 01 11 02 12 03 13 04 14 05 15 06 16 07 17
            q1Q0 = Sse2.UnpackLow(
                Sse2.LoadScalarVector128((long*)(s.ToPointer() + (0 * pitch) - 4)).AsByte(),
                Sse2.LoadScalarVector128((long*)(s.ToPointer() + (1 * pitch) - 4)).AsByte());

            // 20 30 21 31 22 32 23 33 24 34 25 35 26 36 27 37
            x1 = Sse2.UnpackLow(
                Sse2.LoadScalarVector128((long*)(s.ToPointer() + (2 * pitch) - 4)).AsByte(),
                Sse2.LoadScalarVector128((long*)(s.ToPointer() + (3 * pitch) - 4)).AsByte());

            // 40 50 41 51 42 52 43 53 44 54 45 55 46 56 47 57
            x2 = Sse2.UnpackLow(
                Sse2.LoadScalarVector128((long*)(s.ToPointer() + (4 * pitch) - 4)).AsByte(),
                Sse2.LoadScalarVector128((long*)(s.ToPointer() + (5 * pitch) - 4)).AsByte());

            // 60 70 61 71 62 72 63 73 64 74 65 75 66 76 67 77
            x3 = Sse2.UnpackLow(
                Sse2.LoadScalarVector128((long*)(s.ToPointer() + (6 * pitch) - 4)).AsByte(),
                Sse2.LoadScalarVector128((long*)(s.ToPointer() + (7 * pitch) - 4)).AsByte());

            // Transpose 8x8
            // 00 10 20 30 01 11 21 31  02 12 22 32 03 13 23 33
            p1P0 = Sse2.UnpackLow(q1Q0.AsInt16(), x1.AsInt16()).AsByte();
            // 40 50 60 70 41 51 61 71  42 52 62 72 43 53 63 73
            x0 = Sse2.UnpackLow(x2.AsInt16(), x3.AsInt16()).AsByte();
            // 00 10 20 30 40 50 60 70  01 11 21 31 41 51 61 71
            p3P2 = Sse2.UnpackLow(p1P0.AsInt32(), x0.AsInt32()).AsByte();
            // 02 12 22 32 42 52 62 72  03 13 23 33 43 53 63 73
            p1P0 = Sse2.UnpackHigh(p1P0.AsInt32(), x0.AsInt32()).AsByte();
            p3P2 = Sse2.UnpackHigh(p3P2.AsInt64(), Sse2.ShiftLeftLogical128BitLane(p3P2, 8).AsInt64())
                .AsByte(); // swap lo and high
            p1P0 = Sse2.UnpackHigh(p1P0.AsInt64(), Sse2.ShiftLeftLogical128BitLane(p1P0, 8).AsInt64())
                .AsByte(); // swap lo and high

            // 04 14 24 34 05 15 25 35  06 16 26 36 07 17 27 37
            q1Q0 = Sse2.UnpackHigh(q1Q0.AsInt16(), x1.AsInt16()).AsByte();
            // 44 54 64 74 45 55 65 75  46 56 66 76 47 57 67 77
            x2 = Sse2.UnpackHigh(x2.AsInt16(), x3.AsInt16()).AsByte();
            // 06 16 26 36 46 56 66 76  07 17 27 37 47 57 67 77
            q3Q2 = Sse2.UnpackHigh(q1Q0.AsInt32(), x2.AsInt32()).AsByte();
            // 04 14 24 34 44 54 64 74  05 15 25 35 45 55 65 75
            q1Q0 = Sse2.UnpackLow(q1Q0.AsInt32(), x2.AsInt32()).AsByte();

            q0P0 = Sse2.UnpackLow(p1P0.AsInt64(), q1Q0.AsInt64()).AsByte();
            q1P1 = Sse2.UnpackHigh(p1P0.AsInt64(), q1Q0.AsInt64()).AsByte();
            p1P0 = Sse2.UnpackLow(q0P0.AsInt64(), q1P1.AsInt64()).AsByte();
            p2P1 = Sse2.UnpackLow(q1P1.AsInt64(), p3P2.AsInt64()).AsByte();
            q2Q1 = Sse2.UnpackLow(Sse2.ShiftRightLogical128BitLane(q1P1, 8).AsInt64(), q3Q2.AsInt64()).AsByte();

            FilterHevMask(q1P1, q0P0, p3P2, p2P1, p1P0, q3Q2, q2Q1, q1Q0, limitV, threshV, out hev, out mask);
            Filter4(p1P0, q1Q0, hev, mask, ff, out ps1Ps0, out qs1Qs0);

            // Transpose 8x4 to 4x8
            // qs1qs0: 20 21 22 23 24 25 26 27  30 31 32 33 34 34 36 37
            // ps1ps0: 10 11 12 13 14 15 16 17  00 01 02 03 04 05 06 07
            // 00 01 02 03 04 05 06 07  10 11 12 13 14 15 16 17
            ps1Ps0 = Sse2.UnpackHigh(ps1Ps0.AsInt64(), Sse2.ShiftLeftLogical128BitLane(ps1Ps0, 8).AsInt64()).AsByte();
            // 10 30 11 31 12 32 13 33  14 34 15 35 16 36 17 37
            x0 = Sse2.UnpackHigh(ps1Ps0, qs1Qs0);
            // 00 20 01 21 02 22 03 23  04 24 05 25 06 26 07 27
            ps1Ps0 = Sse2.UnpackLow(ps1Ps0, qs1Qs0);
            // 04 14 24 34 05 15 25 35  06 16 26 36 07 17 27 37
            qs1Qs0 = Sse2.UnpackHigh(ps1Ps0, x0);
            // 00 10 20 30 01 11 21 31  02 12 22 32 03 13 23 33
            ps1Ps0 = Sse2.UnpackLow(ps1Ps0, x0);

            *(int*)(s.ToPointer() + (0 * pitch) - 2) = ps1Ps0.AsInt32().GetElement(0);
            ps1Ps0 = Sse2.ShiftRightLogical128BitLane(ps1Ps0, 4);
            *(int*)(s.ToPointer() + (1 * pitch) - 2) = ps1Ps0.AsInt32().GetElement(0);
            ps1Ps0 = Sse2.ShiftRightLogical128BitLane(ps1Ps0, 4);
            *(int*)(s.ToPointer() + (2 * pitch) - 2) = ps1Ps0.AsInt32().GetElement(0);
            ps1Ps0 = Sse2.ShiftRightLogical128BitLane(ps1Ps0, 4);
            *(int*)(s.ToPointer() + (3 * pitch) - 2) = ps1Ps0.AsInt32().GetElement(0);

            *(int*)(s.ToPointer() + (4 * pitch) - 2) = qs1Qs0.AsInt32().GetElement(0);
            qs1Qs0 = Sse2.ShiftRightLogical128BitLane(qs1Qs0, 4);
            *(int*)(s.ToPointer() + (5 * pitch) - 2) = qs1Qs0.AsInt32().GetElement(0);
            qs1Qs0 = Sse2.ShiftRightLogical128BitLane(qs1Qs0, 4);
            *(int*)(s.ToPointer() + (6 * pitch) - 2) = qs1Qs0.AsInt32().GetElement(0);
            qs1Qs0 = Sse2.ShiftRightLogical128BitLane(qs1Qs0, 4);
            *(int*)(s.ToPointer() + (7 * pitch) - 2) = qs1Qs0.AsInt32().GetElement(0);
        }

        public static unsafe void LpfHorizontal16(
            ArrayPtr<byte> s,
            int pitch,
            ReadOnlySpan<byte> blimit,
            ReadOnlySpan<byte> limit,
            ReadOnlySpan<byte> thresh)
        {
            Vector128<byte> zero = Vector128<byte>.Zero;
            Vector128<byte> one = Vector128.Create((byte)1);
            Vector128<byte> blimitV, limitV, threshV;

            fixed (byte* pBLimit = blimit, pLimit = limit, pThresh = thresh)
            {
                blimitV = Sse2.LoadVector128(pBLimit);
                limitV = Sse2.LoadVector128(pLimit);
                threshV = Sse2.LoadVector128(pThresh);
            }

            Vector128<byte> mask, hev, flat, flat2;
            Vector128<byte> q7P7, q6P6, q5P5, q4P4, q3P3, q2P2, q1P1, q0P0, p0Q0, p1Q1;
            Vector128<byte> absP1P0;

            q4P4 = Sse2.LoadScalarVector128((long*)(s.ToPointer() - (5 * pitch))).AsByte();
            q4P4 = Sse.LoadHigh(q4P4.AsSingle(), (float*)(s.ToPointer() + (4 * pitch))).AsByte();
            q3P3 = Sse2.LoadScalarVector128((long*)(s.ToPointer() - (4 * pitch))).AsByte();
            q3P3 = Sse.LoadHigh(q3P3.AsSingle(), (float*)(s.ToPointer() + (3 * pitch))).AsByte();
            q2P2 = Sse2.LoadScalarVector128((long*)(s.ToPointer() - (3 * pitch))).AsByte();
            q2P2 = Sse.LoadHigh(q2P2.AsSingle(), (float*)(s.ToPointer() + (2 * pitch))).AsByte();
            q1P1 = Sse2.LoadScalarVector128((long*)(s.ToPointer() - (2 * pitch))).AsByte();
            q1P1 = Sse.LoadHigh(q1P1.AsSingle(), (float*)(s.ToPointer() + (1 * pitch))).AsByte();
            p1Q1 = Sse2.Shuffle(q1P1.AsUInt32(), 78).AsByte();
            q0P0 = Sse2.LoadScalarVector128((long*)(s.ToPointer() - (1 * pitch))).AsByte();
            q0P0 = Sse.LoadHigh(q0P0.AsSingle(), (float*)(s.ToPointer() - (0 * pitch))).AsByte();
            p0Q0 = Sse2.Shuffle(q0P0.AsUInt32(), 78).AsByte();

            {
                Vector128<byte> absP1Q1, absP0Q0, absQ1Q0, fe, ff, work;
                absP1P0 = AbsDiff(q1P1, q0P0);
                absQ1Q0 = Sse2.ShiftRightLogical128BitLane(absP1P0, 8);
                fe = Vector128.Create((byte)0xfe);
                ff = Sse2.CompareEqual(absP1P0, absP1P0);
                absP0Q0 = AbsDiff(q0P0, p0Q0);
                absP1Q1 = AbsDiff(q1P1, p1Q1);
                flat = Sse2.Max(absP1P0, absQ1Q0);
                hev = Sse2.SubtractSaturate(flat, threshV);
                hev = Sse2.Xor(Sse2.CompareEqual(hev, zero), ff);

                absP0Q0 = Sse2.AddSaturate(absP0Q0, absP0Q0);
                absP1Q1 = Sse2.ShiftRightLogical(Sse2.And(absP1Q1, fe).AsInt16(), 1).AsByte();
                mask = Sse2.SubtractSaturate(Sse2.AddSaturate(absP0Q0, absP1Q1), blimitV);
                mask = Sse2.Xor(Sse2.CompareEqual(mask, zero), ff);
                // mask |= (abs(p0 - q0) * 2 + abs(p1 - q1) / 2  > blimit) * -1;
                mask = Sse2.Max(absP1P0, mask);
                // mask |= (abs(p1 - p0) > limit) * -1;
                // mask |= (abs(q1 - q0) > limit) * -1;

                work = Sse2.Max(AbsDiff(q2P2, q1P1), AbsDiff(q3P3, q2P2));
                mask = Sse2.Max(work, mask);
                mask = Sse2.Max(mask, Sse2.ShiftRightLogical128BitLane(mask, 8));
                mask = Sse2.SubtractSaturate(mask, limitV);
                mask = Sse2.CompareEqual(mask, zero);
            }

            // lp filter
            {
                Vector128<byte> t4 = Vector128.Create((byte)4);
                Vector128<byte> t3 = Vector128.Create((byte)3);
                Vector128<byte> t80 = Vector128.Create((byte)0x80);
                Vector128<ushort> t1 = Vector128.Create((ushort)0x1);
                Vector128<byte> qs1Ps1 = Sse2.Xor(q1P1, t80);
                Vector128<byte> qs0Ps0 = Sse2.Xor(q0P0, t80);
                Vector128<byte> qs0 = Sse2.Xor(p0Q0, t80);
                Vector128<byte> qs1 = Sse2.Xor(p1Q1, t80);
                Vector128<byte> filt;
                Vector128<byte> workA;
                Vector128<byte> filter1, filter2;
                Vector128<byte> flat2Q6P6, flat2Q5P5, flat2Q4P4, flat2Q3P3, flat2Q2P2;
                Vector128<byte> flat2Q1P1, flat2Q0P0, flatQ2P2, flatQ1P1, flatQ0P0;

                filt = Sse2.And(Sse2.SubtractSaturate(qs1Ps1.AsSByte(), qs1.AsSByte()).AsByte(), hev);
                workA = Sse2.SubtractSaturate(qs0.AsSByte(), qs0Ps0.AsSByte()).AsByte();
                filt = Sse2.AddSaturate(filt.AsSByte(), workA.AsSByte()).AsByte();
                filt = Sse2.AddSaturate(filt.AsSByte(), workA.AsSByte()).AsByte();
                filt = Sse2.AddSaturate(filt.AsSByte(), workA.AsSByte()).AsByte();
                // (vpx_filter + 3 * (qs0 - ps0)) & mask
                filt = Sse2.And(filt, mask);

                filter1 = Sse2.AddSaturate(filt.AsSByte(), t4.AsSByte()).AsByte();
                filter2 = Sse2.AddSaturate(filt.AsSByte(), t3.AsSByte()).AsByte();

                filter1 = Sse2.UnpackLow(zero, filter1);
                filter1 = Sse2.ShiftRightArithmetic(filter1.AsInt16(), 0xB).AsByte();
                filter2 = Sse2.UnpackLow(zero, filter2);
                filter2 = Sse2.ShiftRightArithmetic(filter2.AsInt16(), 0xB).AsByte();

                // Filter1 >> 3
                filt = Sse2.PackSignedSaturate(filter2.AsInt16(),
                    Sse2.SubtractSaturate(zero.AsInt16(), filter1.AsInt16())).AsByte();
                qs0Ps0 = Sse2.Xor(Sse2.AddSaturate(qs0Ps0.AsSByte(), filt.AsSByte()).AsByte(), t80);

                // filt >> 1
                filt = Sse2.AddSaturate(filter1.AsInt16(), t1.AsInt16()).AsByte();
                filt = Sse2.ShiftRightArithmetic(filt.AsInt16(), 1).AsByte();
                filt = Sse2.AndNot(Sse2.ShiftRightArithmetic(Sse2.UnpackLow(zero, hev).AsInt16(), 0x8), filt.AsInt16())
                    .AsByte();
                filt = Sse2.PackSignedSaturate(filt.AsInt16(), Sse2.SubtractSaturate(zero.AsInt16(), filt.AsInt16()))
                    .AsByte();
                qs1Ps1 = Sse2.Xor(Sse2.AddSaturate(qs1Ps1.AsSByte(), filt.AsSByte()).AsByte(), t80);
                // loopfilter done

                {
                    Vector128<byte> work;
                    flat = Sse2.Max(AbsDiff(q2P2, q0P0), AbsDiff(q3P3, q0P0));
                    flat = Sse2.Max(absP1P0, flat);
                    flat = Sse2.Max(flat, Sse2.ShiftRightLogical128BitLane(flat, 8));
                    flat = Sse2.SubtractSaturate(flat, one);
                    flat = Sse2.CompareEqual(flat, zero);
                    flat = Sse2.And(flat, mask);

                    q5P5 = Sse2.LoadScalarVector128((long*)(s.ToPointer() - (6 * pitch))).AsByte();
                    q5P5 = Sse.LoadHigh(q5P5.AsSingle(), (float*)(s.ToPointer() + (5 * pitch))).AsByte();

                    q6P6 = Sse2.LoadScalarVector128((long*)(s.ToPointer() - (7 * pitch))).AsByte();
                    q6P6 = Sse.LoadHigh(q6P6.AsSingle(), (float*)(s.ToPointer() + (6 * pitch))).AsByte();
                    flat2 = Sse2.Max(AbsDiff(q4P4, q0P0), AbsDiff(q5P5, q0P0));

                    q7P7 = Sse2.LoadScalarVector128((long*)(s.ToPointer() - (8 * pitch))).AsByte();
                    q7P7 = Sse.LoadHigh(q7P7.AsSingle(), (float*)(s.ToPointer() + (7 * pitch))).AsByte();
                    work = Sse2.Max(AbsDiff(q6P6, q0P0), AbsDiff(q7P7, q0P0));
                    flat2 = Sse2.Max(work, flat2);
                    flat2 = Sse2.Max(flat2, Sse2.ShiftRightLogical128BitLane(flat2, 8));
                    flat2 = Sse2.SubtractSaturate(flat2, one);
                    flat2 = Sse2.CompareEqual(flat2, zero);
                    flat2 = Sse2.And(flat2, flat); // flat2 & flat & mask
                }

                // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
                // flat and wide flat calculations
                {
                    Vector128<short> eight = Vector128.Create((short)8);
                    Vector128<short> four = Vector128.Create((short)4);
                    Vector128<short> p716, p616, p516, p416, p316, p216, p116, p016;
                    Vector128<short> q716, q616, q516, q416, q316, q216, q116, q016;
                    Vector128<short> pixelFilterP, pixelFilterQ;
                    Vector128<short> pixetFilterP2P1P0, pixetFilterQ2Q1Q0;
                    Vector128<short> sumP7, sumQ7, sumP3, sumQ3, resP, resQ;

                    p716 = Sse2.UnpackLow(q7P7, zero).AsInt16();
                    p616 = Sse2.UnpackLow(q6P6, zero).AsInt16();
                    p516 = Sse2.UnpackLow(q5P5, zero).AsInt16();
                    p416 = Sse2.UnpackLow(q4P4, zero).AsInt16();
                    p316 = Sse2.UnpackLow(q3P3, zero).AsInt16();
                    p216 = Sse2.UnpackLow(q2P2, zero).AsInt16();
                    p116 = Sse2.UnpackLow(q1P1, zero).AsInt16();
                    p016 = Sse2.UnpackLow(q0P0, zero).AsInt16();
                    q016 = Sse2.UnpackHigh(q0P0, zero).AsInt16();
                    q116 = Sse2.UnpackHigh(q1P1, zero).AsInt16();
                    q216 = Sse2.UnpackHigh(q2P2, zero).AsInt16();
                    q316 = Sse2.UnpackHigh(q3P3, zero).AsInt16();
                    q416 = Sse2.UnpackHigh(q4P4, zero).AsInt16();
                    q516 = Sse2.UnpackHigh(q5P5, zero).AsInt16();
                    q616 = Sse2.UnpackHigh(q6P6, zero).AsInt16();
                    q716 = Sse2.UnpackHigh(q7P7, zero).AsInt16();

                    pixelFilterP = Sse2.Add(Sse2.Add(p616, p516), Sse2.Add(p416, p316));
                    pixelFilterQ = Sse2.Add(Sse2.Add(q616, q516), Sse2.Add(q416, q316));

                    pixetFilterP2P1P0 = Sse2.Add(p016, Sse2.Add(p216, p116));
                    pixelFilterP = Sse2.Add(pixelFilterP, pixetFilterP2P1P0);

                    pixetFilterQ2Q1Q0 = Sse2.Add(q016, Sse2.Add(q216, q116));
                    pixelFilterQ = Sse2.Add(pixelFilterQ, pixetFilterQ2Q1Q0);
                    pixelFilterP = Sse2.Add(eight, Sse2.Add(pixelFilterP, pixelFilterQ));
                    pixetFilterP2P1P0 = Sse2.Add(four, Sse2.Add(pixetFilterP2P1P0, pixetFilterQ2Q1Q0));
                    resP = Sse2.ShiftRightLogical(Sse2.Add(pixelFilterP, Sse2.Add(p716, p016)), 4);
                    resQ = Sse2.ShiftRightLogical(Sse2.Add(pixelFilterP, Sse2.Add(q716, q016)), 4);
                    flat2Q0P0 = Sse2.PackUnsignedSaturate(resP, resQ);
                    resP = Sse2.ShiftRightLogical(Sse2.Add(pixetFilterP2P1P0, Sse2.Add(p316, p016)), 3);
                    resQ = Sse2.ShiftRightLogical(Sse2.Add(pixetFilterP2P1P0, Sse2.Add(q316, q016)), 3);

                    flatQ0P0 = Sse2.PackUnsignedSaturate(resP, resQ);

                    sumP7 = Sse2.Add(p716, p716);
                    sumQ7 = Sse2.Add(q716, q716);
                    sumP3 = Sse2.Add(p316, p316);
                    sumQ3 = Sse2.Add(q316, q316);

                    pixelFilterQ = Sse2.Subtract(pixelFilterP, p616);
                    pixelFilterP = Sse2.Subtract(pixelFilterP, q616);
                    resP = Sse2.ShiftRightLogical(Sse2.Add(pixelFilterP, Sse2.Add(sumP7, p116)), 4);
                    resQ = Sse2.ShiftRightLogical(Sse2.Add(pixelFilterQ, Sse2.Add(sumQ7, q116)), 4);
                    flat2Q1P1 = Sse2.PackUnsignedSaturate(resP, resQ);

                    pixetFilterQ2Q1Q0 = Sse2.Subtract(pixetFilterP2P1P0, p216);
                    pixetFilterP2P1P0 = Sse2.Subtract(pixetFilterP2P1P0, q216);
                    resP = Sse2.ShiftRightLogical(Sse2.Add(pixetFilterP2P1P0, Sse2.Add(sumP3, p116)), 3);
                    resQ = Sse2.ShiftRightLogical(Sse2.Add(pixetFilterQ2Q1Q0, Sse2.Add(sumQ3, q116)), 3);
                    flatQ1P1 = Sse2.PackUnsignedSaturate(resP, resQ);

                    sumP7 = Sse2.Add(sumP7, p716);
                    sumQ7 = Sse2.Add(sumQ7, q716);
                    sumP3 = Sse2.Add(sumP3, p316);
                    sumQ3 = Sse2.Add(sumQ3, q316);

                    pixelFilterP = Sse2.Subtract(pixelFilterP, q516);
                    pixelFilterQ = Sse2.Subtract(pixelFilterQ, p516);
                    resP = Sse2.ShiftRightLogical(Sse2.Add(pixelFilterP, Sse2.Add(sumP7, p216)), 4);
                    resQ = Sse2.ShiftRightLogical(Sse2.Add(pixelFilterQ, Sse2.Add(sumQ7, q216)), 4);
                    flat2Q2P2 = Sse2.PackUnsignedSaturate(resP, resQ);

                    pixetFilterP2P1P0 = Sse2.Subtract(pixetFilterP2P1P0, q116);
                    pixetFilterQ2Q1Q0 = Sse2.Subtract(pixetFilterQ2Q1Q0, p116);

                    resP = Sse2.ShiftRightLogical(Sse2.Add(pixetFilterP2P1P0, Sse2.Add(sumP3, p216)), 3);
                    resQ = Sse2.ShiftRightLogical(Sse2.Add(pixetFilterQ2Q1Q0, Sse2.Add(sumQ3, q216)), 3);
                    flatQ2P2 = Sse2.PackUnsignedSaturate(resP, resQ);

                    sumP7 = Sse2.Add(sumP7, p716);
                    sumQ7 = Sse2.Add(sumQ7, q716);
                    pixelFilterP = Sse2.Subtract(pixelFilterP, q416);
                    pixelFilterQ = Sse2.Subtract(pixelFilterQ, p416);
                    resP = Sse2.ShiftRightLogical(Sse2.Add(pixelFilterP, Sse2.Add(sumP7, p316)), 4);
                    resQ = Sse2.ShiftRightLogical(Sse2.Add(pixelFilterQ, Sse2.Add(sumQ7, q316)), 4);
                    flat2Q3P3 = Sse2.PackUnsignedSaturate(resP, resQ);

                    sumP7 = Sse2.Add(sumP7, p716);
                    sumQ7 = Sse2.Add(sumQ7, q716);
                    pixelFilterP = Sse2.Subtract(pixelFilterP, q316);
                    pixelFilterQ = Sse2.Subtract(pixelFilterQ, p316);
                    resP = Sse2.ShiftRightLogical(Sse2.Add(pixelFilterP, Sse2.Add(sumP7, p416)), 4);
                    resQ = Sse2.ShiftRightLogical(Sse2.Add(pixelFilterQ, Sse2.Add(sumQ7, q416)), 4);
                    flat2Q4P4 = Sse2.PackUnsignedSaturate(resP, resQ);

                    sumP7 = Sse2.Add(sumP7, p716);
                    sumQ7 = Sse2.Add(sumQ7, q716);
                    pixelFilterP = Sse2.Subtract(pixelFilterP, q216);
                    pixelFilterQ = Sse2.Subtract(pixelFilterQ, p216);
                    resP = Sse2.ShiftRightLogical(Sse2.Add(pixelFilterP, Sse2.Add(sumP7, p516)), 4);
                    resQ = Sse2.ShiftRightLogical(Sse2.Add(pixelFilterQ, Sse2.Add(sumQ7, q516)), 4);
                    flat2Q5P5 = Sse2.PackUnsignedSaturate(resP, resQ);

                    sumP7 = Sse2.Add(sumP7, p716);
                    sumQ7 = Sse2.Add(sumQ7, q716);
                    pixelFilterP = Sse2.Subtract(pixelFilterP, q116);
                    pixelFilterQ = Sse2.Subtract(pixelFilterQ, p116);
                    resP = Sse2.ShiftRightLogical(Sse2.Add(pixelFilterP, Sse2.Add(sumP7, p616)), 4);
                    resQ = Sse2.ShiftRightLogical(Sse2.Add(pixelFilterQ, Sse2.Add(sumQ7, q616)), 4);
                    flat2Q6P6 = Sse2.PackUnsignedSaturate(resP, resQ);
                }
                // wide flat
                // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

                flat = Sse2.Shuffle(flat.AsInt32(), 68).AsByte();
                flat2 = Sse2.Shuffle(flat2.AsInt32(), 68).AsByte();

                q2P2 = Sse2.AndNot(flat, q2P2);
                flatQ2P2 = Sse2.And(flat, flatQ2P2);
                q2P2 = Sse2.Or(q2P2, flatQ2P2);

                qs1Ps1 = Sse2.AndNot(flat, qs1Ps1);
                flatQ1P1 = Sse2.And(flat, flatQ1P1);
                q1P1 = Sse2.Or(qs1Ps1, flatQ1P1);

                qs0Ps0 = Sse2.AndNot(flat, qs0Ps0);
                flatQ0P0 = Sse2.And(flat, flatQ0P0);
                q0P0 = Sse2.Or(qs0Ps0, flatQ0P0);

                q6P6 = Sse2.AndNot(flat2, q6P6);
                flat2Q6P6 = Sse2.And(flat2, flat2Q6P6);
                q6P6 = Sse2.Or(q6P6, flat2Q6P6);
                Sse2.StoreScalar((long*)(s.ToPointer() - (7 * pitch)), q6P6.AsInt64());
                Sse.StoreHigh((float*)(s.ToPointer() + (6 * pitch)), q6P6.AsSingle());

                q5P5 = Sse2.AndNot(flat2, q5P5);
                flat2Q5P5 = Sse2.And(flat2, flat2Q5P5);
                q5P5 = Sse2.Or(q5P5, flat2Q5P5);
                Sse2.StoreScalar((long*)(s.ToPointer() - (6 * pitch)), q5P5.AsInt64());
                Sse.StoreHigh((float*)(s.ToPointer() + (5 * pitch)), q5P5.AsSingle());

                q4P4 = Sse2.AndNot(flat2, q4P4);
                flat2Q4P4 = Sse2.And(flat2, flat2Q4P4);
                q4P4 = Sse2.Or(q4P4, flat2Q4P4);
                Sse2.StoreScalar((long*)(s.ToPointer() - (5 * pitch)), q4P4.AsInt64());
                Sse.StoreHigh((float*)(s.ToPointer() + (4 * pitch)), q4P4.AsSingle());

                q3P3 = Sse2.AndNot(flat2, q3P3);
                flat2Q3P3 = Sse2.And(flat2, flat2Q3P3);
                q3P3 = Sse2.Or(q3P3, flat2Q3P3);
                Sse2.StoreScalar((long*)(s.ToPointer() - (4 * pitch)), q3P3.AsInt64());
                Sse.StoreHigh((float*)(s.ToPointer() + (3 * pitch)), q3P3.AsSingle());

                q2P2 = Sse2.AndNot(flat2, q2P2);
                flat2Q2P2 = Sse2.And(flat2, flat2Q2P2);
                q2P2 = Sse2.Or(q2P2, flat2Q2P2);
                Sse2.StoreScalar((long*)(s.ToPointer() - (3 * pitch)), q2P2.AsInt64());
                Sse.StoreHigh((float*)(s.ToPointer() + (2 * pitch)), q2P2.AsSingle());

                q1P1 = Sse2.AndNot(flat2, q1P1);
                flat2Q1P1 = Sse2.And(flat2, flat2Q1P1);
                q1P1 = Sse2.Or(q1P1, flat2Q1P1);
                Sse2.StoreScalar((long*)(s.ToPointer() - (2 * pitch)), q1P1.AsInt64());
                Sse.StoreHigh((float*)(s.ToPointer() + (1 * pitch)), q1P1.AsSingle());

                q0P0 = Sse2.AndNot(flat2, q0P0);
                flat2Q0P0 = Sse2.And(flat2, flat2Q0P0);
                q0P0 = Sse2.Or(q0P0, flat2Q0P0);
                Sse2.StoreScalar((long*)(s.ToPointer() - (1 * pitch)), q0P0.AsInt64());
                Sse.StoreHigh((float*)(s.ToPointer() - (0 * pitch)), q0P0.AsSingle());
            }
        }

        private static Vector128<short> FilterAdd2Sub2(
            Vector128<short> total,
            Vector128<short> a1,
            Vector128<short> a2,
            Vector128<short> s1,
            Vector128<short> s2)
        {
            Vector128<short> x = Sse2.Add(a1, total);
            x = Sse2.Add(Sse2.Subtract(x, Sse2.Add(s1, s2)), a2);
            return x;
        }

        private static Vector128<byte> Filter8Mask(
            Vector128<byte> flat,
            Vector128<byte> otherFilt,
            Vector128<short> f8Lo,
            Vector128<short> f8Hi)
        {
            Vector128<byte> f8 =
                Sse2.PackUnsignedSaturate(Sse2.ShiftRightLogical(f8Lo, 3), Sse2.ShiftRightLogical(f8Hi, 3));
            Vector128<byte> result = Sse2.And(flat, f8);
            return Sse2.Or(Sse2.AndNot(flat, otherFilt), result);
        }

        private static Vector128<byte> Filter16Mask(
            Vector128<byte> flat,
            Vector128<byte> otherFilt,
            Vector128<short> fLo,
            Vector128<short> fHi)
        {
            Vector128<byte> f =
                Sse2.PackUnsignedSaturate(Sse2.ShiftRightLogical(fLo, 4), Sse2.ShiftRightLogical(fHi, 4));
            Vector128<byte> result = Sse2.And(flat, f);
            return Sse2.Or(Sse2.AndNot(flat, otherFilt), result);
        }

        public static unsafe void LpfHorizontal16Dual(
            ArrayPtr<byte> s,
            int pitch,
            ReadOnlySpan<byte> blimit,
            ReadOnlySpan<byte> limit,
            ReadOnlySpan<byte> thresh)
        {
            Vector128<byte> zero = Vector128<byte>.Zero;
            Vector128<byte> one = Vector128.Create((byte)1);
            Vector128<byte> blimitV, limitV, threshV;

            fixed (byte* pBLimit = blimit, pLimit = limit, pThresh = thresh)
            {
                blimitV = Sse2.LoadVector128(pBLimit);
                limitV = Sse2.LoadVector128(pLimit);
                threshV = Sse2.LoadVector128(pThresh);
            }

            Vector128<byte> mask, hev, flat, flat2;
            Vector128<byte> p7, p6, p5;
            Vector128<byte> p4, p3, p2, p1, p0, q0, q1, q2, q3, q4;
            Vector128<byte> q5, q6, q7;

            Vector128<byte> op2, op1, op0, oq0, oq1, oq2;

            Vector128<byte> maxAbsP1P0Q1Q0;

            p7 = Sse2.LoadVector128(s.ToPointer() - (8 * pitch));
            p6 = Sse2.LoadVector128(s.ToPointer() - (7 * pitch));
            p5 = Sse2.LoadVector128(s.ToPointer() - (6 * pitch));
            p4 = Sse2.LoadVector128(s.ToPointer() - (5 * pitch));
            p3 = Sse2.LoadVector128(s.ToPointer() - (4 * pitch));
            p2 = Sse2.LoadVector128(s.ToPointer() - (3 * pitch));
            p1 = Sse2.LoadVector128(s.ToPointer() - (2 * pitch));
            p0 = Sse2.LoadVector128(s.ToPointer() - (1 * pitch));
            q0 = Sse2.LoadVector128(s.ToPointer() - (0 * pitch));
            q1 = Sse2.LoadVector128(s.ToPointer() + (1 * pitch));
            q2 = Sse2.LoadVector128(s.ToPointer() + (2 * pitch));
            q3 = Sse2.LoadVector128(s.ToPointer() + (3 * pitch));
            q4 = Sse2.LoadVector128(s.ToPointer() + (4 * pitch));
            q5 = Sse2.LoadVector128(s.ToPointer() + (5 * pitch));
            q6 = Sse2.LoadVector128(s.ToPointer() + (6 * pitch));
            q7 = Sse2.LoadVector128(s.ToPointer() + (7 * pitch));

            {
                Vector128<byte> absP1P0 = AbsDiff(p1, p0);
                Vector128<byte> absQ1Q0 = AbsDiff(q1, q0);
                Vector128<byte> fe = Vector128.Create((byte)0xfe);
                Vector128<byte> ff = Sse2.CompareEqual(zero, zero);
                Vector128<byte> absP0Q0 = AbsDiff(p0, q0);
                Vector128<byte> absP1Q1 = AbsDiff(p1, q1);
                Vector128<byte> work;
                maxAbsP1P0Q1Q0 = Sse2.Max(absP1P0, absQ1Q0);

                absP0Q0 = Sse2.AddSaturate(absP0Q0, absP0Q0);
                absP1Q1 = Sse2.ShiftRightLogical(Sse2.And(absP1Q1, fe).AsInt16(), 1).AsByte();
                mask = Sse2.SubtractSaturate(Sse2.AddSaturate(absP0Q0, absP1Q1), blimitV);
                mask = Sse2.Xor(Sse2.CompareEqual(mask, zero), ff);
                // mask |= (abs(p0 - q0) * 2 + abs(p1 - q1) / 2  > blimit) * -1;
                mask = Sse2.Max(maxAbsP1P0Q1Q0, mask);
                // mask |= (abs(p1 - p0) > limit) * -1;
                // mask |= (abs(q1 - q0) > limit) * -1;
                work = Sse2.Max(AbsDiff(p2, p1), AbsDiff(p3, p2));
                mask = Sse2.Max(work, mask);
                work = Sse2.Max(AbsDiff(q2, q1), AbsDiff(q3, q2));
                mask = Sse2.Max(work, mask);
                mask = Sse2.SubtractSaturate(mask, limitV);
                mask = Sse2.CompareEqual(mask, zero);
            }

            {
                Vector128<byte> work;
                work = Sse2.Max(AbsDiff(p2, p0), AbsDiff(q2, q0));
                flat = Sse2.Max(work, maxAbsP1P0Q1Q0);
                work = Sse2.Max(AbsDiff(p3, p0), AbsDiff(q3, q0));
                flat = Sse2.Max(work, flat);
                work = Sse2.Max(AbsDiff(p4, p0), AbsDiff(q4, q0));
                flat = Sse2.SubtractSaturate(flat, one);
                flat = Sse2.CompareEqual(flat, zero);
                flat = Sse2.And(flat, mask);
                flat2 = Sse2.Max(AbsDiff(p5, p0), AbsDiff(q5, q0));
                flat2 = Sse2.Max(work, flat2);
                work = Sse2.Max(AbsDiff(p6, p0), AbsDiff(q6, q0));
                flat2 = Sse2.Max(work, flat2);
                work = Sse2.Max(AbsDiff(p7, p0), AbsDiff(q7, q0));
                flat2 = Sse2.Max(work, flat2);
                flat2 = Sse2.SubtractSaturate(flat2, one);
                flat2 = Sse2.CompareEqual(flat2, zero);
                flat2 = Sse2.And(flat2, flat); // flat2 & flat & mask
            }

            // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            // filter4
            {
                Vector128<byte> t4 = Vector128.Create((byte)4);
                Vector128<byte> t3 = Vector128.Create((byte)3);
                Vector128<byte> t80 = Vector128.Create((byte)0x80);
                Vector128<byte> te0 = Vector128.Create((byte)0xe0);
                Vector128<byte> t1F = Vector128.Create((byte)0x1f);
                Vector128<byte> t1 = Vector128.Create((byte)0x1);
                Vector128<byte> t7F = Vector128.Create((byte)0x7f);
                Vector128<byte> ff = Sse2.CompareEqual(t4, t4);

                Vector128<byte> filt;
                Vector128<byte> workA;
                Vector128<byte> filter1, filter2;

                op1 = Sse2.Xor(p1, t80);
                op0 = Sse2.Xor(p0, t80);
                oq0 = Sse2.Xor(q0, t80);
                oq1 = Sse2.Xor(q1, t80);

                hev = Sse2.SubtractSaturate(maxAbsP1P0Q1Q0, threshV);
                hev = Sse2.Xor(Sse2.CompareEqual(hev, zero), ff);
                filt = Sse2.And(Sse2.SubtractSaturate(op1.AsSByte(), oq1.AsSByte()).AsByte(), hev);

                workA = Sse2.SubtractSaturate(oq0.AsSByte(), op0.AsSByte()).AsByte();
                filt = Sse2.AddSaturate(filt.AsSByte(), workA.AsSByte()).AsByte();
                filt = Sse2.AddSaturate(filt.AsSByte(), workA.AsSByte()).AsByte();
                filt = Sse2.AddSaturate(filt.AsSByte(), workA.AsSByte()).AsByte();
                // (vpx_filter + 3 * (qs0 - ps0)) & mask
                filt = Sse2.And(filt, mask);
                filter1 = Sse2.AddSaturate(filt.AsSByte(), t4.AsSByte()).AsByte();
                filter2 = Sse2.AddSaturate(filt.AsSByte(), t3.AsSByte()).AsByte();

                // Filter1 >> 3
                workA = Sse2.CompareGreaterThan(zero.AsSByte(), filter1.AsSByte()).AsByte();
                filter1 = Sse2.ShiftRightLogical(filter1.AsInt16(), 3).AsByte();
                workA = Sse2.And(workA, te0);
                filter1 = Sse2.And(filter1, t1F);
                filter1 = Sse2.Or(filter1, workA);
                oq0 = Sse2.Xor(Sse2.SubtractSaturate(oq0.AsSByte(), filter1.AsSByte()).AsByte(), t80);

                // Filter2 >> 3
                workA = Sse2.CompareGreaterThan(zero.AsSByte(), filter2.AsSByte()).AsByte();
                filter2 = Sse2.ShiftRightLogical(filter2.AsInt16(), 3).AsByte();
                workA = Sse2.And(workA, te0);
                filter2 = Sse2.And(filter2, t1F);
                filter2 = Sse2.Or(filter2, workA);
                op0 = Sse2.Xor(Sse2.AddSaturate(op0.AsSByte(), filter2.AsSByte()).AsByte(), t80);

                // filt >> 1
                filt = Sse2.AddSaturate(filter1.AsSByte(), t1.AsSByte()).AsByte();
                workA = Sse2.CompareGreaterThan(zero.AsSByte(), filt.AsSByte()).AsByte();
                filt = Sse2.ShiftRightLogical(filt.AsInt16(), 1).AsByte();
                workA = Sse2.And(workA, t80);
                filt = Sse2.And(filt, t7F);
                filt = Sse2.Or(filt, workA);
                filt = Sse2.AndNot(hev, filt);
                op1 = Sse2.Xor(Sse2.AddSaturate(op1.AsSByte(), filt.AsSByte()).AsByte(), t80);
                oq1 = Sse2.Xor(Sse2.SubtractSaturate(oq1.AsSByte(), filt.AsSByte()).AsByte(), t80);
                // loopfilter done

                // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
                // filter8
                {
                    Vector128<short> four = Vector128.Create((short)4);
                    Vector128<short> p3Lo = Sse2.UnpackLow(p3, zero).AsInt16();
                    Vector128<short> p2Lo = Sse2.UnpackLow(p2, zero).AsInt16();
                    Vector128<short> p1Lo = Sse2.UnpackLow(p1, zero).AsInt16();
                    Vector128<short> p0Lo = Sse2.UnpackLow(p0, zero).AsInt16();
                    Vector128<short> q0Lo = Sse2.UnpackLow(q0, zero).AsInt16();
                    Vector128<short> q1Lo = Sse2.UnpackLow(q1, zero).AsInt16();
                    Vector128<short> q2Lo = Sse2.UnpackLow(q2, zero).AsInt16();
                    Vector128<short> q3Lo = Sse2.UnpackLow(q3, zero).AsInt16();

                    Vector128<short> p3Hi = Sse2.UnpackHigh(p3, zero).AsInt16();
                    Vector128<short> p2Hi = Sse2.UnpackHigh(p2, zero).AsInt16();
                    Vector128<short> p1Hi = Sse2.UnpackHigh(p1, zero).AsInt16();
                    Vector128<short> p0Hi = Sse2.UnpackHigh(p0, zero).AsInt16();
                    Vector128<short> q0Hi = Sse2.UnpackHigh(q0, zero).AsInt16();
                    Vector128<short> q1Hi = Sse2.UnpackHigh(q1, zero).AsInt16();
                    Vector128<short> q2Hi = Sse2.UnpackHigh(q2, zero).AsInt16();
                    Vector128<short> q3Hi = Sse2.UnpackHigh(q3, zero).AsInt16();
                    Vector128<short> f8Lo, f8Hi;

                    f8Lo = Sse2.Add(Sse2.Add(p3Lo, four), Sse2.Add(p3Lo, p2Lo));
                    f8Lo = Sse2.Add(Sse2.Add(p3Lo, f8Lo), Sse2.Add(p2Lo, p1Lo));
                    f8Lo = Sse2.Add(Sse2.Add(p0Lo, q0Lo), f8Lo);

                    f8Hi = Sse2.Add(Sse2.Add(p3Hi, four), Sse2.Add(p3Hi, p2Hi));
                    f8Hi = Sse2.Add(Sse2.Add(p3Hi, f8Hi), Sse2.Add(p2Hi, p1Hi));
                    f8Hi = Sse2.Add(Sse2.Add(p0Hi, q0Hi), f8Hi);

                    op2 = Filter8Mask(flat, p2, f8Lo, f8Hi);

                    f8Lo = FilterAdd2Sub2(f8Lo, q1Lo, p1Lo, p2Lo, p3Lo);
                    f8Hi = FilterAdd2Sub2(f8Hi, q1Hi, p1Hi, p2Hi, p3Hi);
                    op1 = Filter8Mask(flat, op1, f8Lo, f8Hi);

                    f8Lo = FilterAdd2Sub2(f8Lo, q2Lo, p0Lo, p1Lo, p3Lo);
                    f8Hi = FilterAdd2Sub2(f8Hi, q2Hi, p0Hi, p1Hi, p3Hi);
                    op0 = Filter8Mask(flat, op0, f8Lo, f8Hi);

                    f8Lo = FilterAdd2Sub2(f8Lo, q3Lo, q0Lo, p0Lo, p3Lo);
                    f8Hi = FilterAdd2Sub2(f8Hi, q3Hi, q0Hi, p0Hi, p3Hi);
                    oq0 = Filter8Mask(flat, oq0, f8Lo, f8Hi);

                    f8Lo = FilterAdd2Sub2(f8Lo, q3Lo, q1Lo, q0Lo, p2Lo);
                    f8Hi = FilterAdd2Sub2(f8Hi, q3Hi, q1Hi, q0Hi, p2Hi);
                    oq1 = Filter8Mask(flat, oq1, f8Lo, f8Hi);

                    f8Lo = FilterAdd2Sub2(f8Lo, q3Lo, q2Lo, q1Lo, p1Lo);
                    f8Hi = FilterAdd2Sub2(f8Hi, q3Hi, q2Hi, q1Hi, p1Hi);
                    oq2 = Filter8Mask(flat, q2, f8Lo, f8Hi);
                }

                // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
                // wide flat calculations
                {
                    Vector128<short> eight = Vector128.Create((short)8);
                    Vector128<short> p7Lo = Sse2.UnpackLow(p7, zero).AsInt16();
                    Vector128<short> p6Lo = Sse2.UnpackLow(p6, zero).AsInt16();
                    Vector128<short> p5Lo = Sse2.UnpackLow(p5, zero).AsInt16();
                    Vector128<short> p4Lo = Sse2.UnpackLow(p4, zero).AsInt16();
                    Vector128<short> p3Lo = Sse2.UnpackLow(p3, zero).AsInt16();
                    Vector128<short> p2Lo = Sse2.UnpackLow(p2, zero).AsInt16();
                    Vector128<short> p1Lo = Sse2.UnpackLow(p1, zero).AsInt16();
                    Vector128<short> p0Lo = Sse2.UnpackLow(p0, zero).AsInt16();
                    Vector128<short> q0Lo = Sse2.UnpackLow(q0, zero).AsInt16();
                    Vector128<short> q1Lo = Sse2.UnpackLow(q1, zero).AsInt16();
                    Vector128<short> q2Lo = Sse2.UnpackLow(q2, zero).AsInt16();
                    Vector128<short> q3Lo = Sse2.UnpackLow(q3, zero).AsInt16();
                    Vector128<short> q4Lo = Sse2.UnpackLow(q4, zero).AsInt16();
                    Vector128<short> q5Lo = Sse2.UnpackLow(q5, zero).AsInt16();
                    Vector128<short> q6Lo = Sse2.UnpackLow(q6, zero).AsInt16();
                    Vector128<short> q7Lo = Sse2.UnpackLow(q7, zero).AsInt16();

                    Vector128<short> p7Hi = Sse2.UnpackHigh(p7, zero).AsInt16();
                    Vector128<short> p6Hi = Sse2.UnpackHigh(p6, zero).AsInt16();
                    Vector128<short> p5Hi = Sse2.UnpackHigh(p5, zero).AsInt16();
                    Vector128<short> p4Hi = Sse2.UnpackHigh(p4, zero).AsInt16();
                    Vector128<short> p3Hi = Sse2.UnpackHigh(p3, zero).AsInt16();
                    Vector128<short> p2Hi = Sse2.UnpackHigh(p2, zero).AsInt16();
                    Vector128<short> p1Hi = Sse2.UnpackHigh(p1, zero).AsInt16();
                    Vector128<short> p0Hi = Sse2.UnpackHigh(p0, zero).AsInt16();
                    Vector128<short> q0Hi = Sse2.UnpackHigh(q0, zero).AsInt16();
                    Vector128<short> q1Hi = Sse2.UnpackHigh(q1, zero).AsInt16();
                    Vector128<short> q2Hi = Sse2.UnpackHigh(q2, zero).AsInt16();
                    Vector128<short> q3Hi = Sse2.UnpackHigh(q3, zero).AsInt16();
                    Vector128<short> q4Hi = Sse2.UnpackHigh(q4, zero).AsInt16();
                    Vector128<short> q5Hi = Sse2.UnpackHigh(q5, zero).AsInt16();
                    Vector128<short> q6Hi = Sse2.UnpackHigh(q6, zero).AsInt16();
                    Vector128<short> q7Hi = Sse2.UnpackHigh(q7, zero).AsInt16();

                    Vector128<short> fLo;
                    Vector128<short> fHi;

                    fLo = Sse2.Subtract(Sse2.ShiftLeftLogical(p7Lo, 3), p7Lo); // p7 * 7
                    fLo = Sse2.Add(Sse2.ShiftLeftLogical(p6Lo, 1), Sse2.Add(p4Lo, fLo));
                    fLo = Sse2.Add(Sse2.Add(p3Lo, fLo), Sse2.Add(p2Lo, p1Lo));
                    fLo = Sse2.Add(Sse2.Add(p0Lo, q0Lo), fLo);
                    fLo = Sse2.Add(Sse2.Add(p5Lo, eight), fLo);

                    fHi = Sse2.Subtract(Sse2.ShiftLeftLogical(p7Hi, 3), p7Hi); // p7 * 7
                    fHi = Sse2.Add(Sse2.ShiftLeftLogical(p6Hi, 1), Sse2.Add(p4Hi, fHi));
                    fHi = Sse2.Add(Sse2.Add(p3Hi, fHi), Sse2.Add(p2Hi, p1Hi));
                    fHi = Sse2.Add(Sse2.Add(p0Hi, q0Hi), fHi);
                    fHi = Sse2.Add(Sse2.Add(p5Hi, eight), fHi);

                    p6 = Filter16Mask(flat2, p6, fLo, fHi);
                    Sse2.Store(s.ToPointer() - (7 * pitch), p6);

                    fLo = FilterAdd2Sub2(fLo, q1Lo, p5Lo, p6Lo, p7Lo);
                    fHi = FilterAdd2Sub2(fHi, q1Hi, p5Hi, p6Hi, p7Hi);
                    p5 = Filter16Mask(flat2, p5, fLo, fHi);
                    Sse2.Store(s.ToPointer() - (6 * pitch), p5);

                    fLo = FilterAdd2Sub2(fLo, q2Lo, p4Lo, p5Lo, p7Lo);
                    fHi = FilterAdd2Sub2(fHi, q2Hi, p4Hi, p5Hi, p7Hi);
                    p4 = Filter16Mask(flat2, p4, fLo, fHi);
                    Sse2.Store(s.ToPointer() - (5 * pitch), p4);

                    fLo = FilterAdd2Sub2(fLo, q3Lo, p3Lo, p4Lo, p7Lo);
                    fHi = FilterAdd2Sub2(fHi, q3Hi, p3Hi, p4Hi, p7Hi);
                    p3 = Filter16Mask(flat2, p3, fLo, fHi);
                    Sse2.Store(s.ToPointer() - (4 * pitch), p3);

                    fLo = FilterAdd2Sub2(fLo, q4Lo, p2Lo, p3Lo, p7Lo);
                    fHi = FilterAdd2Sub2(fHi, q4Hi, p2Hi, p3Hi, p7Hi);
                    op2 = Filter16Mask(flat2, op2, fLo, fHi);
                    Sse2.Store(s.ToPointer() - (3 * pitch), op2);

                    fLo = FilterAdd2Sub2(fLo, q5Lo, p1Lo, p2Lo, p7Lo);
                    fHi = FilterAdd2Sub2(fHi, q5Hi, p1Hi, p2Hi, p7Hi);
                    op1 = Filter16Mask(flat2, op1, fLo, fHi);
                    Sse2.Store(s.ToPointer() - (2 * pitch), op1);

                    fLo = FilterAdd2Sub2(fLo, q6Lo, p0Lo, p1Lo, p7Lo);
                    fHi = FilterAdd2Sub2(fHi, q6Hi, p0Hi, p1Hi, p7Hi);
                    op0 = Filter16Mask(flat2, op0, fLo, fHi);
                    Sse2.Store(s.ToPointer() - (1 * pitch), op0);

                    fLo = FilterAdd2Sub2(fLo, q7Lo, q0Lo, p0Lo, p7Lo);
                    fHi = FilterAdd2Sub2(fHi, q7Hi, q0Hi, p0Hi, p7Hi);
                    oq0 = Filter16Mask(flat2, oq0, fLo, fHi);
                    Sse2.Store(s.ToPointer() - (0 * pitch), oq0);

                    fLo = FilterAdd2Sub2(fLo, q7Lo, q1Lo, p6Lo, q0Lo);
                    fHi = FilterAdd2Sub2(fHi, q7Hi, q1Hi, p6Hi, q0Hi);
                    oq1 = Filter16Mask(flat2, oq1, fLo, fHi);
                    Sse2.Store(s.ToPointer() + (1 * pitch), oq1);

                    fLo = FilterAdd2Sub2(fLo, q7Lo, q2Lo, p5Lo, q1Lo);
                    fHi = FilterAdd2Sub2(fHi, q7Hi, q2Hi, p5Hi, q1Hi);
                    oq2 = Filter16Mask(flat2, oq2, fLo, fHi);
                    Sse2.Store(s.ToPointer() + (2 * pitch), oq2);

                    fLo = FilterAdd2Sub2(fLo, q7Lo, q3Lo, p4Lo, q2Lo);
                    fHi = FilterAdd2Sub2(fHi, q7Hi, q3Hi, p4Hi, q2Hi);
                    q3 = Filter16Mask(flat2, q3, fLo, fHi);
                    Sse2.Store(s.ToPointer() + (3 * pitch), q3);

                    fLo = FilterAdd2Sub2(fLo, q7Lo, q4Lo, p3Lo, q3Lo);
                    fHi = FilterAdd2Sub2(fHi, q7Hi, q4Hi, p3Hi, q3Hi);
                    q4 = Filter16Mask(flat2, q4, fLo, fHi);
                    Sse2.Store(s.ToPointer() + (4 * pitch), q4);

                    fLo = FilterAdd2Sub2(fLo, q7Lo, q5Lo, p2Lo, q4Lo);
                    fHi = FilterAdd2Sub2(fHi, q7Hi, q5Hi, p2Hi, q4Hi);
                    q5 = Filter16Mask(flat2, q5, fLo, fHi);
                    Sse2.Store(s.ToPointer() + (5 * pitch), q5);

                    fLo = FilterAdd2Sub2(fLo, q7Lo, q6Lo, p1Lo, q5Lo);
                    fHi = FilterAdd2Sub2(fHi, q7Hi, q6Hi, p1Hi, q5Hi);
                    q6 = Filter16Mask(flat2, q6, fLo, fHi);
                    Sse2.Store(s.ToPointer() + (6 * pitch), q6);
                }
                // wide flat
                // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            }
        }

        public static unsafe void LpfHorizontal8(
            ArrayPtr<byte> s,
            int pitch,
            ReadOnlySpan<byte> blimit,
            ReadOnlySpan<byte> limit,
            ReadOnlySpan<byte> thresh)
        {
            Vector128<byte> flatOp2;
            Vector128<byte> flatOp1;
            Vector128<byte> flatOp0;
            Vector128<byte> flatOq2;
            Vector128<byte> flatOq1;
            Vector128<byte> flatOq0;
            Vector128<byte> zero = Vector128<byte>.Zero;
            Vector128<byte> blimitV, limitV, threshV;

            fixed (byte* pBLimit = blimit, pLimit = limit, pThresh = thresh)
            {
                blimitV = Sse2.LoadVector128(pBLimit);
                limitV = Sse2.LoadVector128(pLimit);
                threshV = Sse2.LoadVector128(pThresh);
            }

            Vector128<byte> mask, hev, flat;
            Vector128<byte> p3, p2, p1, p0, q0, q1, q2, q3;
            Vector128<byte> q3P3, q2P2, q1P1, q0P0, p1Q1, p0Q0;

            q3P3 = Sse2.UnpackLow(
                Sse2.LoadScalarVector128((long*)(s.ToPointer() - (4 * pitch))),
                Sse2.LoadScalarVector128((long*)(s.ToPointer() + (3 * pitch)))).AsByte();
            q2P2 = Sse2.UnpackLow(
                Sse2.LoadScalarVector128((long*)(s.ToPointer() - (3 * pitch))),
                Sse2.LoadScalarVector128((long*)(s.ToPointer() + (2 * pitch)))).AsByte();
            q1P1 = Sse2.UnpackLow(
                Sse2.LoadScalarVector128((long*)(s.ToPointer() - (2 * pitch))),
                Sse2.LoadScalarVector128((long*)(s.ToPointer() + (1 * pitch)))).AsByte();
            q0P0 = Sse2.UnpackLow(
                Sse2.LoadScalarVector128((long*)(s.ToPointer() - (1 * pitch))),
                Sse2.LoadScalarVector128((long*)(s.ToPointer() - (0 * pitch)))).AsByte();
            p1Q1 = Sse2.Shuffle(q1P1.AsInt32(), 78).AsByte();
            p0Q0 = Sse2.Shuffle(q0P0.AsInt32(), 78).AsByte();

            {
                // filter_mask and hev_mask
                Vector128<byte> one = Vector128.Create((byte)1);
                Vector128<byte> fe = Vector128.Create((byte)0xfe);
                Vector128<byte> ff = Sse2.CompareEqual(fe, fe);
                Vector128<byte> absP1Q1, absP0Q0, absQ1Q0, absP1P0, work;
                absP1P0 = AbsDiff(q1P1, q0P0);
                absQ1Q0 = Sse2.ShiftRightLogical128BitLane(absP1P0, 8);

                absP0Q0 = AbsDiff(q0P0, p0Q0);
                absP1Q1 = AbsDiff(q1P1, p1Q1);
                flat = Sse2.Max(absP1P0, absQ1Q0);
                hev = Sse2.SubtractSaturate(flat, threshV);
                hev = Sse2.Xor(Sse2.CompareEqual(hev, zero), ff);

                absP0Q0 = Sse2.AddSaturate(absP0Q0, absP0Q0);
                absP1Q1 = Sse2.ShiftRightLogical(Sse2.And(absP1Q1, fe).AsInt16(), 1).AsByte();
                mask = Sse2.SubtractSaturate(Sse2.AddSaturate(absP0Q0, absP1Q1), blimitV);
                mask = Sse2.Xor(Sse2.CompareEqual(mask, zero), ff);
                // mask |= (abs(p0 - q0) * 2 + abs(p1 - q1) / 2  > blimit) * -1;
                mask = Sse2.Max(absP1P0, mask);
                // mask |= (abs(p1 - p0) > limit) * -1;
                // mask |= (abs(q1 - q0) > limit) * -1;

                work = Sse2.Max(AbsDiff(q2P2, q1P1), AbsDiff(q3P3, q2P2));
                mask = Sse2.Max(work, mask);
                mask = Sse2.Max(mask, Sse2.ShiftRightLogical128BitLane(mask, 8));
                mask = Sse2.SubtractSaturate(mask, limitV);
                mask = Sse2.CompareEqual(mask, zero);

                // flat_mask4

                flat = Sse2.Max(AbsDiff(q2P2, q0P0), AbsDiff(q3P3, q0P0));
                flat = Sse2.Max(absP1P0, flat);
                flat = Sse2.Max(flat, Sse2.ShiftRightLogical128BitLane(flat, 8));
                flat = Sse2.SubtractSaturate(flat, one);
                flat = Sse2.CompareEqual(flat, zero);
                flat = Sse2.And(flat, mask);
            }

            {
                Vector128<short> four = Vector128.Create((short)4);
                {
                    Vector128<short> workpA, workpB, workpShft;
                    p3 = Sse2.UnpackLow(
                        Sse2.LoadScalarVector128((long*)(s.ToPointer() - (4 * pitch))).AsByte(), zero);
                    p2 = Sse2.UnpackLow(
                        Sse2.LoadScalarVector128((long*)(s.ToPointer() - (3 * pitch))).AsByte(), zero);
                    p1 = Sse2.UnpackLow(
                        Sse2.LoadScalarVector128((long*)(s.ToPointer() - (2 * pitch))).AsByte(), zero);
                    p0 = Sse2.UnpackLow(
                        Sse2.LoadScalarVector128((long*)(s.ToPointer() - (1 * pitch))).AsByte(), zero);
                    q0 = Sse2.UnpackLow(
                        Sse2.LoadScalarVector128((long*)(s.ToPointer() - (0 * pitch))).AsByte(), zero);
                    q1 = Sse2.UnpackLow(
                        Sse2.LoadScalarVector128((long*)(s.ToPointer() + (1 * pitch))).AsByte(), zero);
                    q2 = Sse2.UnpackLow(
                        Sse2.LoadScalarVector128((long*)(s.ToPointer() + (2 * pitch))).AsByte(), zero);
                    q3 = Sse2.UnpackLow(
                        Sse2.LoadScalarVector128((long*)(s.ToPointer() + (3 * pitch))).AsByte(), zero);

                    workpA = Sse2.Add(Sse2.Add(p3.AsInt16(), p3.AsInt16()), Sse2.Add(p2.AsInt16(), p1.AsInt16()));
                    workpA = Sse2.Add(Sse2.Add(workpA, four), p0.AsInt16());
                    workpB = Sse2.Add(Sse2.Add(q0.AsInt16(), p2.AsInt16()), p3.AsInt16());
                    workpShft = Sse2.ShiftRightLogical(Sse2.Add(workpA, workpB), 3);
                    Sse2.StoreScalar((long*)&flatOp2, Sse2.PackUnsignedSaturate(workpShft, workpShft).AsInt64());

                    workpB = Sse2.Add(Sse2.Add(q0.AsInt16(), q1.AsInt16()), p1.AsInt16());
                    workpShft = Sse2.ShiftRightLogical(Sse2.Add(workpA, workpB), 3);
                    Sse2.StoreScalar((long*)&flatOp1, Sse2.PackUnsignedSaturate(workpShft, workpShft).AsInt64());

                    workpA = Sse2.Add(Sse2.Subtract(workpA, p3.AsInt16()), q2.AsInt16());
                    workpB = Sse2.Add(Sse2.Subtract(workpB, p1.AsInt16()), p0.AsInt16());
                    workpShft = Sse2.ShiftRightLogical(Sse2.Add(workpA, workpB), 3);
                    Sse2.StoreScalar((long*)&flatOp0, Sse2.PackUnsignedSaturate(workpShft, workpShft).AsInt64());

                    workpA = Sse2.Add(Sse2.Subtract(workpA, p3.AsInt16()), q3.AsInt16());
                    workpB = Sse2.Add(Sse2.Subtract(workpB, p0.AsInt16()), q0.AsInt16());
                    workpShft = Sse2.ShiftRightLogical(Sse2.Add(workpA, workpB), 3);
                    Sse2.StoreScalar((long*)&flatOq0, Sse2.PackUnsignedSaturate(workpShft, workpShft).AsInt64());

                    workpA = Sse2.Add(Sse2.Subtract(workpA, p2.AsInt16()), q3.AsInt16());
                    workpB = Sse2.Add(Sse2.Subtract(workpB, q0.AsInt16()), q1.AsInt16());
                    workpShft = Sse2.ShiftRightLogical(Sse2.Add(workpA, workpB), 3);
                    Sse2.StoreScalar((long*)&flatOq1, Sse2.PackUnsignedSaturate(workpShft, workpShft).AsInt64());

                    workpA = Sse2.Add(Sse2.Subtract(workpA, p1.AsInt16()), q3.AsInt16());
                    workpB = Sse2.Add(Sse2.Subtract(workpB, q1.AsInt16()), q2.AsInt16());
                    workpShft = Sse2.ShiftRightLogical(Sse2.Add(workpA, workpB), 3);
                    Sse2.StoreScalar((long*)&flatOq2, Sse2.PackUnsignedSaturate(workpShft, workpShft).AsInt64());
                }
            }
            // lp filter
            {
                Vector128<byte> t4 = Vector128.Create((byte)4);
                Vector128<byte> t3 = Vector128.Create((byte)3);
                Vector128<byte> t80 = Vector128.Create((byte)0x80);
                Vector128<byte> t1 = Vector128.Create((byte)0x1);
                Vector128<byte> ps1 =
                    Sse2.Xor(Sse2.LoadScalarVector128((long*)(s.ToPointer() - (2 * pitch))).AsByte(),
                        t80);
                Vector128<byte> ps0 =
                    Sse2.Xor(Sse2.LoadScalarVector128((long*)(s.ToPointer() - (1 * pitch))).AsByte(),
                        t80);
                Vector128<byte> qs0 =
                    Sse2.Xor(Sse2.LoadScalarVector128((long*)(s.ToPointer() + (0 * pitch))).AsByte(),
                        t80);
                Vector128<byte> qs1 =
                    Sse2.Xor(Sse2.LoadScalarVector128((long*)(s.ToPointer() + (1 * pitch))).AsByte(),
                        t80);
                Vector128<byte> filt;
                Vector128<byte> workA;
                Vector128<byte> filter1, filter2;

                filt = Sse2.And(Sse2.SubtractSaturate(ps1.AsSByte(), qs1.AsSByte()).AsByte(), hev);
                workA = Sse2.SubtractSaturate(qs0.AsSByte(), ps0.AsSByte()).AsByte();
                filt = Sse2.AddSaturate(filt.AsSByte(), workA.AsSByte()).AsByte();
                filt = Sse2.AddSaturate(filt.AsSByte(), workA.AsSByte()).AsByte();
                filt = Sse2.AddSaturate(filt.AsSByte(), workA.AsSByte()).AsByte();
                // (vpx_filter + 3 * (qs0 - ps0)) & mask
                filt = Sse2.And(filt, mask);

                filter1 = Sse2.AddSaturate(filt.AsSByte(), t4.AsSByte()).AsByte();
                filter2 = Sse2.AddSaturate(filt.AsSByte(), t3.AsSByte()).AsByte();

                // Filter1 >> 3
                filter1 = Sse2.UnpackLow(zero, filter1);
                filter1 = Sse2.ShiftRightArithmetic(filter1.AsInt16(), 11).AsByte();
                filter1 = Sse2.PackSignedSaturate(filter1.AsInt16(), filter1.AsInt16()).AsByte();

                // Filter2 >> 3
                filter2 = Sse2.UnpackLow(zero, filter2);
                filter2 = Sse2.ShiftRightArithmetic(filter2.AsInt16(), 11).AsByte();
                filter2 = Sse2.PackSignedSaturate(filter2.AsInt16(), zero.AsInt16()).AsByte();

                // filt >> 1
                filt = Sse2.AddSaturate(filter1.AsSByte(), t1.AsSByte()).AsByte();
                filt = Sse2.UnpackLow(zero, filt);
                filt = Sse2.ShiftRightArithmetic(filt.AsInt16(), 9).AsByte();
                filt = Sse2.PackSignedSaturate(filt.AsInt16(), zero.AsInt16()).AsByte();

                filt = Sse2.AndNot(hev, filt);

                workA = Sse2.Xor(Sse2.SubtractSaturate(qs0.AsSByte(), filter1.AsSByte()).AsByte(), t80);
                q0 = Sse2.LoadScalarVector128((long*)&flatOq0).AsByte();
                workA = Sse2.AndNot(flat, workA);
                q0 = Sse2.And(flat, q0);
                q0 = Sse2.Or(workA, q0);

                workA = Sse2.Xor(Sse2.SubtractSaturate(qs1.AsSByte(), filt.AsSByte()).AsByte(), t80);
                q1 = Sse2.LoadScalarVector128((long*)&flatOq1).AsByte();
                workA = Sse2.AndNot(flat, workA);
                q1 = Sse2.And(flat, q1);
                q1 = Sse2.Or(workA, q1);

                workA = Sse2.LoadVector128(s.ToPointer() + (2 * pitch));
                q2 = Sse2.LoadScalarVector128((long*)&flatOq2).AsByte();
                workA = Sse2.AndNot(flat, workA);
                q2 = Sse2.And(flat, q2);
                q2 = Sse2.Or(workA, q2);

                workA = Sse2.Xor(Sse2.AddSaturate(ps0.AsSByte(), filter2.AsSByte()).AsByte(), t80);
                p0 = Sse2.LoadScalarVector128((long*)&flatOp0).AsByte();
                workA = Sse2.AndNot(flat, workA);
                p0 = Sse2.And(flat, p0);
                p0 = Sse2.Or(workA, p0);

                workA = Sse2.Xor(Sse2.AddSaturate(ps1.AsSByte(), filt.AsSByte()).AsByte(), t80);
                p1 = Sse2.LoadScalarVector128((long*)&flatOp1).AsByte();
                workA = Sse2.AndNot(flat, workA);
                p1 = Sse2.And(flat, p1);
                p1 = Sse2.Or(workA, p1);

                workA = Sse2.LoadVector128(s.ToPointer() - (3 * pitch));
                p2 = Sse2.LoadScalarVector128((long*)&flatOp2).AsByte();
                workA = Sse2.AndNot(flat, workA);
                p2 = Sse2.And(flat, p2);
                p2 = Sse2.Or(workA, p2);

                Sse2.StoreScalar((long*)(s.ToPointer() - (3 * pitch)), p2.AsInt64());
                Sse2.StoreScalar((long*)(s.ToPointer() - (2 * pitch)), p1.AsInt64());
                Sse2.StoreScalar((long*)(s.ToPointer() - (1 * pitch)), p0.AsInt64());
                Sse2.StoreScalar((long*)(s.ToPointer() + (0 * pitch)), q0.AsInt64());
                Sse2.StoreScalar((long*)(s.ToPointer() + (1 * pitch)), q1.AsInt64());
                Sse2.StoreScalar((long*)(s.ToPointer() + (2 * pitch)), q2.AsInt64());
            }
        }

        public static unsafe void LpfHorizontal8Dual(
            ArrayPtr<byte> s,
            int pitch,
            ReadOnlySpan<byte> blimit0,
            ReadOnlySpan<byte> limit0,
            ReadOnlySpan<byte> thresh0,
            ReadOnlySpan<byte> blimit1,
            ReadOnlySpan<byte> limit1,
            ReadOnlySpan<byte> thresh1)
        {
            Vector128<byte> flatOp2;
            Vector128<byte> flatOp1;
            Vector128<byte> flatOp0;
            Vector128<byte> flatOq2;
            Vector128<byte> flatOq1;
            Vector128<byte> flatOq0;
            Vector128<byte> zero = Vector128<byte>.Zero;
            Vector128<byte> blimit, limit, thresh;

            fixed (byte* pBLimit0 = blimit0, pLimit0 = limit0, pThresh0 = thresh0,
                   pBLimit1 = blimit1, pLimit1 = limit1, pThresh1 = thresh1)
            {
                blimit = Sse2.UnpackLow(Sse2.LoadVector128(pBLimit0).AsInt64(), Sse2.LoadVector128(pBLimit1).AsInt64())
                    .AsByte();
                limit = Sse2.UnpackLow(Sse2.LoadVector128(pLimit0).AsInt64(), Sse2.LoadVector128(pLimit1).AsInt64())
                    .AsByte();
                thresh = Sse2.UnpackLow(Sse2.LoadVector128(pThresh0).AsInt64(), Sse2.LoadVector128(pThresh1).AsInt64())
                    .AsByte();
            }

            Vector128<byte> mask, hev, flat;
            Vector128<byte> p3, p2, p1, p0, q0, q1, q2, q3;

            p3 = Sse2.LoadVector128(s.ToPointer() - (4 * pitch));
            p2 = Sse2.LoadVector128(s.ToPointer() - (3 * pitch));
            p1 = Sse2.LoadVector128(s.ToPointer() - (2 * pitch));
            p0 = Sse2.LoadVector128(s.ToPointer() - (1 * pitch));
            q0 = Sse2.LoadVector128(s.ToPointer() - (0 * pitch));
            q1 = Sse2.LoadVector128(s.ToPointer() + (1 * pitch));
            q2 = Sse2.LoadVector128(s.ToPointer() + (2 * pitch));
            q3 = Sse2.LoadVector128(s.ToPointer() + (3 * pitch));
            {
                Vector128<byte> absP1P0 = Sse2.Or(Sse2.SubtractSaturate(p1, p0), Sse2.SubtractSaturate(p0, p1));
                Vector128<byte> absQ1Q0 = Sse2.Or(Sse2.SubtractSaturate(q1, q0), Sse2.SubtractSaturate(q0, q1));
                Vector128<byte> one = Vector128.Create((byte)1);
                Vector128<byte> fe = Vector128.Create((byte)0xfe);
                Vector128<byte> ff = Sse2.CompareEqual(absP1P0, absP1P0);
                Vector128<byte> absP0Q0 = Sse2.Or(Sse2.SubtractSaturate(p0, q0), Sse2.SubtractSaturate(q0, p0));
                Vector128<byte> absP1Q1 = Sse2.Or(Sse2.SubtractSaturate(p1, q1), Sse2.SubtractSaturate(q1, p1));
                Vector128<byte> work;

                // filter_mask and hev_mask
                flat = Sse2.Max(absP1P0, absQ1Q0);
                hev = Sse2.SubtractSaturate(flat, thresh);
                hev = Sse2.Xor(Sse2.CompareEqual(hev, zero), ff);

                absP0Q0 = Sse2.AddSaturate(absP0Q0, absP0Q0);
                absP1Q1 = Sse2.ShiftRightLogical(Sse2.And(absP1Q1, fe).AsInt16(), 1).AsByte();
                mask = Sse2.SubtractSaturate(Sse2.AddSaturate(absP0Q0, absP1Q1), blimit);
                mask = Sse2.Xor(Sse2.CompareEqual(mask, zero), ff);
                // mask |= (abs(p0 - q0) * 2 + abs(p1 - q1) / 2  > blimit) * -1;
                mask = Sse2.Max(flat, mask);
                // mask |= (abs(p1 - p0) > limit) * -1;
                // mask |= (abs(q1 - q0) > limit) * -1;
                work = Sse2.Max(
                    Sse2.Or(Sse2.SubtractSaturate(p2, p1), Sse2.SubtractSaturate(p1, p2)),
                    Sse2.Or(Sse2.SubtractSaturate(p3, p2), Sse2.SubtractSaturate(p2, p3)));
                mask = Sse2.Max(work, mask);
                work = Sse2.Max(
                    Sse2.Or(Sse2.SubtractSaturate(q2, q1), Sse2.SubtractSaturate(q1, q2)),
                    Sse2.Or(Sse2.SubtractSaturate(q3, q2), Sse2.SubtractSaturate(q2, q3)));
                mask = Sse2.Max(work, mask);
                mask = Sse2.SubtractSaturate(mask, limit);
                mask = Sse2.CompareEqual(mask, zero);

                // flat_mask4
                work = Sse2.Max(
                    Sse2.Or(Sse2.SubtractSaturate(p2, p0), Sse2.SubtractSaturate(p0, p2)),
                    Sse2.Or(Sse2.SubtractSaturate(q2, q0), Sse2.SubtractSaturate(q0, q2)));
                flat = Sse2.Max(work, flat);
                work = Sse2.Max(
                    Sse2.Or(Sse2.SubtractSaturate(p3, p0), Sse2.SubtractSaturate(p0, p3)),
                    Sse2.Or(Sse2.SubtractSaturate(q3, q0), Sse2.SubtractSaturate(q0, q3)));
                flat = Sse2.Max(work, flat);
                flat = Sse2.SubtractSaturate(flat, one);
                flat = Sse2.CompareEqual(flat, zero);
                flat = Sse2.And(flat, mask);
            }
            {
                Vector128<short> four = Vector128.Create((short)4);
                ArrayPtr<byte> src = s;
                int i = 0;

                do
                {
                    Vector128<short> workpA, workpB, workpShft;
                    p3 = Sse2.UnpackLow(
                        Sse2.LoadScalarVector128((long*)(src.ToPointer() - (4 * pitch))).AsByte(), zero);
                    p2 = Sse2.UnpackLow(
                        Sse2.LoadScalarVector128((long*)(src.ToPointer() - (3 * pitch))).AsByte(), zero);
                    p1 = Sse2.UnpackLow(
                        Sse2.LoadScalarVector128((long*)(src.ToPointer() - (2 * pitch))).AsByte(), zero);
                    p0 = Sse2.UnpackLow(
                        Sse2.LoadScalarVector128((long*)(src.ToPointer() - (1 * pitch))).AsByte(), zero);
                    q0 = Sse2.UnpackLow(
                        Sse2.LoadScalarVector128((long*)(src.ToPointer() - (0 * pitch))).AsByte(), zero);
                    q1 = Sse2.UnpackLow(
                        Sse2.LoadScalarVector128((long*)(src.ToPointer() + (1 * pitch))).AsByte(), zero);
                    q2 = Sse2.UnpackLow(
                        Sse2.LoadScalarVector128((long*)(src.ToPointer() + (2 * pitch))).AsByte(), zero);
                    q3 = Sse2.UnpackLow(
                        Sse2.LoadScalarVector128((long*)(src.ToPointer() + (3 * pitch))).AsByte(), zero);

                    workpA = Sse2.Add(Sse2.Add(p3.AsInt16(), p3.AsInt16()), Sse2.Add(p2.AsInt16(), p1.AsInt16()));
                    workpA = Sse2.Add(Sse2.Add(workpA, four), p0.AsInt16());
                    workpB = Sse2.Add(Sse2.Add(q0.AsInt16(), p2.AsInt16()), p3.AsInt16());
                    workpShft = Sse2.ShiftRightLogical(Sse2.Add(workpA, workpB), 3);
                    Sse2.StoreScalar((long*)&flatOp2 + i, Sse2.PackUnsignedSaturate(workpShft, workpShft).AsInt64());

                    workpB = Sse2.Add(Sse2.Add(q0.AsInt16(), q1.AsInt16()), p1.AsInt16());
                    workpShft = Sse2.ShiftRightLogical(Sse2.Add(workpA, workpB), 3);
                    Sse2.StoreScalar((long*)&flatOp1 + i, Sse2.PackUnsignedSaturate(workpShft, workpShft).AsInt64());

                    workpA = Sse2.Add(Sse2.Subtract(workpA, p3.AsInt16()), q2.AsInt16());
                    workpB = Sse2.Add(Sse2.Subtract(workpB, p1.AsInt16()), p0.AsInt16());
                    workpShft = Sse2.ShiftRightLogical(Sse2.Add(workpA, workpB), 3);
                    Sse2.StoreScalar((long*)&flatOp0 + i, Sse2.PackUnsignedSaturate(workpShft, workpShft).AsInt64());

                    workpA = Sse2.Add(Sse2.Subtract(workpA, p3.AsInt16()), q3.AsInt16());
                    workpB = Sse2.Add(Sse2.Subtract(workpB, p0.AsInt16()), q0.AsInt16());
                    workpShft = Sse2.ShiftRightLogical(Sse2.Add(workpA, workpB), 3);
                    Sse2.StoreScalar((long*)&flatOq0 + i, Sse2.PackUnsignedSaturate(workpShft, workpShft).AsInt64());

                    workpA = Sse2.Add(Sse2.Subtract(workpA, p2.AsInt16()), q3.AsInt16());
                    workpB = Sse2.Add(Sse2.Subtract(workpB, q0.AsInt16()), q1.AsInt16());
                    workpShft = Sse2.ShiftRightLogical(Sse2.Add(workpA, workpB), 3);
                    Sse2.StoreScalar((long*)&flatOq1 + i, Sse2.PackUnsignedSaturate(workpShft, workpShft).AsInt64());

                    workpA = Sse2.Add(Sse2.Subtract(workpA, p1.AsInt16()), q3.AsInt16());
                    workpB = Sse2.Add(Sse2.Subtract(workpB, q1.AsInt16()), q2.AsInt16());
                    workpShft = Sse2.ShiftRightLogical(Sse2.Add(workpA, workpB), 3);
                    Sse2.StoreScalar((long*)&flatOq2 + i, Sse2.PackUnsignedSaturate(workpShft, workpShft).AsInt64());

                    src = src.Slice(8);
                } while (++i < 2);
            }
            // lp filter
            {
                Vector128<byte> t4 = Vector128.Create((byte)4);
                Vector128<byte> t3 = Vector128.Create((byte)3);
                Vector128<byte> t80 = Vector128.Create((byte)0x80);
                Vector128<byte> te0 = Vector128.Create((byte)0xe0);
                Vector128<byte> t1F = Vector128.Create((byte)0x1f);
                Vector128<byte> t1 = Vector128.Create((byte)0x1);
                Vector128<byte> t7F = Vector128.Create((byte)0x7f);

                Vector128<byte> ps1 = Sse2.Xor(Sse2.LoadVector128(s.ToPointer() - (2 * pitch)), t80);
                Vector128<byte> ps0 = Sse2.Xor(Sse2.LoadVector128(s.ToPointer() - (1 * pitch)), t80);
                Vector128<byte> qs0 = Sse2.Xor(Sse2.LoadVector128(s.ToPointer() + (0 * pitch)), t80);
                Vector128<byte> qs1 = Sse2.Xor(Sse2.LoadVector128(s.ToPointer() + (1 * pitch)), t80);
                Vector128<byte> filt;
                Vector128<byte> workA;
                Vector128<byte> filter1, filter2;

                filt = Sse2.And(Sse2.SubtractSaturate(ps1.AsSByte(), qs1.AsSByte()).AsByte(), hev);
                workA = Sse2.SubtractSaturate(qs0.AsSByte(), ps0.AsSByte()).AsByte();
                filt = Sse2.AddSaturate(filt.AsSByte(), workA.AsSByte()).AsByte();
                filt = Sse2.AddSaturate(filt.AsSByte(), workA.AsSByte()).AsByte();
                filt = Sse2.AddSaturate(filt.AsSByte(), workA.AsSByte()).AsByte();
                // (vpx_filter + 3 * (qs0 - ps0)) & mask
                filt = Sse2.And(filt, mask);

                filter1 = Sse2.AddSaturate(filt.AsSByte(), t4.AsSByte()).AsByte();
                filter2 = Sse2.AddSaturate(filt.AsSByte(), t3.AsSByte()).AsByte();

                // Filter1 >> 3
                workA = Sse2.CompareGreaterThan(zero.AsSByte(), filter1.AsSByte()).AsByte();
                filter1 = Sse2.ShiftRightLogical(filter1.AsInt16(), 3).AsByte();
                workA = Sse2.And(workA, te0);
                filter1 = Sse2.And(filter1, t1F);
                filter1 = Sse2.Or(filter1, workA);

                // Filter2 >> 3
                workA = Sse2.CompareGreaterThan(zero.AsSByte(), filter2.AsSByte()).AsByte();
                filter2 = Sse2.ShiftRightLogical(filter2.AsInt16(), 3).AsByte();
                workA = Sse2.And(workA, te0);
                filter2 = Sse2.And(filter2, t1F);
                filter2 = Sse2.Or(filter2, workA);

                // filt >> 1
                filt = Sse2.AddSaturate(filter1.AsSByte(), t1.AsSByte()).AsByte();
                workA = Sse2.CompareGreaterThan(zero.AsSByte(), filt.AsSByte()).AsByte();
                filt = Sse2.ShiftRightLogical(filt.AsInt16(), 1).AsByte();
                workA = Sse2.And(workA, t80);
                filt = Sse2.And(filt, t7F);
                filt = Sse2.Or(filt, workA);

                filt = Sse2.AndNot(hev, filt);

                workA = Sse2.Xor(Sse2.SubtractSaturate(qs0.AsSByte(), filter1.AsSByte()).AsByte(), t80);
                q0 = Sse2.LoadVector128((byte*)&flatOq0);
                workA = Sse2.AndNot(flat, workA);
                q0 = Sse2.And(flat, q0);
                q0 = Sse2.Or(workA, q0);

                workA = Sse2.Xor(Sse2.SubtractSaturate(qs1.AsSByte(), filt.AsSByte()).AsByte(), t80);
                q1 = Sse2.LoadVector128((byte*)&flatOq1);
                workA = Sse2.AndNot(flat, workA);
                q1 = Sse2.And(flat, q1);
                q1 = Sse2.Or(workA, q1);

                workA = Sse2.LoadVector128(s.ToPointer() + (2 * pitch));
                q2 = Sse2.LoadVector128((byte*)&flatOq2);
                workA = Sse2.AndNot(flat, workA);
                q2 = Sse2.And(flat, q2);
                q2 = Sse2.Or(workA, q2);

                workA = Sse2.Xor(Sse2.AddSaturate(ps0.AsSByte(), filter2.AsSByte()).AsByte(), t80);
                p0 = Sse2.LoadVector128((byte*)&flatOp0);
                workA = Sse2.AndNot(flat, workA);
                p0 = Sse2.And(flat, p0);
                p0 = Sse2.Or(workA, p0);

                workA = Sse2.Xor(Sse2.AddSaturate(ps1.AsSByte(), filt.AsSByte()).AsByte(), t80);
                p1 = Sse2.LoadVector128((byte*)&flatOp1);
                workA = Sse2.AndNot(flat, workA);
                p1 = Sse2.And(flat, p1);
                p1 = Sse2.Or(workA, p1);

                workA = Sse2.LoadVector128(s.ToPointer() - (3 * pitch));
                p2 = Sse2.LoadVector128((byte*)&flatOp2);
                workA = Sse2.AndNot(flat, workA);
                p2 = Sse2.And(flat, p2);
                p2 = Sse2.Or(workA, p2);

                Sse2.Store(s.ToPointer() - (3 * pitch), p2);
                Sse2.Store(s.ToPointer() - (2 * pitch), p1);
                Sse2.Store(s.ToPointer() - (1 * pitch), p0);
                Sse2.Store(s.ToPointer() + (0 * pitch), q0);
                Sse2.Store(s.ToPointer() + (1 * pitch), q1);
                Sse2.Store(s.ToPointer() + (2 * pitch), q2);
            }
        }

        public static unsafe void LpfHorizontal4Dual(
            ArrayPtr<byte> s,
            int pitch,
            ReadOnlySpan<byte> blimit0,
            ReadOnlySpan<byte> limit0,
            ReadOnlySpan<byte> thresh0,
            ReadOnlySpan<byte> blimit1,
            ReadOnlySpan<byte> limit1,
            ReadOnlySpan<byte> thresh1)
        {
            Vector128<byte> blimit, limit, thresh;

            fixed (byte* pBLimit0 = blimit0, pLimit0 = limit0, pThresh0 = thresh0,
                   pBLimit1 = blimit1, pLimit1 = limit1, pThresh1 = thresh1)
            {
                blimit = Sse2.UnpackLow(Sse2.LoadVector128(pBLimit0).AsInt64(), Sse2.LoadVector128(pBLimit1).AsInt64())
                    .AsByte();
                limit = Sse2.UnpackLow(Sse2.LoadVector128(pLimit0).AsInt64(), Sse2.LoadVector128(pLimit1).AsInt64())
                    .AsByte();
                thresh = Sse2.UnpackLow(Sse2.LoadVector128(pThresh0).AsInt64(), Sse2.LoadVector128(pThresh1).AsInt64())
                    .AsByte();
            }

            Vector128<byte> zero = Vector128<byte>.Zero;
            Vector128<byte> p3, p2, p1, p0, q0, q1, q2, q3;
            Vector128<byte> mask, hev, flat;

            p3 = Sse2.LoadVector128(s.ToPointer() - (4 * pitch));
            p2 = Sse2.LoadVector128(s.ToPointer() - (3 * pitch));
            p1 = Sse2.LoadVector128(s.ToPointer() - (2 * pitch));
            p0 = Sse2.LoadVector128(s.ToPointer() - (1 * pitch));
            q0 = Sse2.LoadVector128(s.ToPointer() - (0 * pitch));
            q1 = Sse2.LoadVector128(s.ToPointer() + (1 * pitch));
            q2 = Sse2.LoadVector128(s.ToPointer() + (2 * pitch));
            q3 = Sse2.LoadVector128(s.ToPointer() + (3 * pitch));

            // filter_mask and hev_mask
            {
                Vector128<byte> absP1P0 = Sse2.Or(Sse2.SubtractSaturate(p1, p0), Sse2.SubtractSaturate(p0, p1));
                Vector128<byte> absQ1Q0 = Sse2.Or(Sse2.SubtractSaturate(q1, q0), Sse2.SubtractSaturate(q0, q1));
                Vector128<byte> fe = Vector128.Create((byte)0xfe);
                Vector128<byte> ff = Sse2.CompareEqual(absP1P0, absP1P0);
                Vector128<byte> absP0Q0 = Sse2.Or(Sse2.SubtractSaturate(p0, q0), Sse2.SubtractSaturate(q0, p0));
                Vector128<byte> absP1Q1 = Sse2.Or(Sse2.SubtractSaturate(p1, q1), Sse2.SubtractSaturate(q1, p1));
                Vector128<byte> work;

                flat = Sse2.Max(absP1P0, absQ1Q0);
                hev = Sse2.SubtractSaturate(flat, thresh);
                hev = Sse2.Xor(Sse2.CompareEqual(hev, zero), ff);

                absP0Q0 = Sse2.AddSaturate(absP0Q0, absP0Q0);
                absP1Q1 = Sse2.ShiftRightLogical(Sse2.And(absP1Q1, fe).AsInt16(), 1).AsByte();
                mask = Sse2.SubtractSaturate(Sse2.AddSaturate(absP0Q0, absP1Q1), blimit);
                mask = Sse2.Xor(Sse2.CompareEqual(mask, zero), ff);
                // mask |= (abs(p0 - q0) * 2 + abs(p1 - q1) / 2  > blimit) * -1;
                mask = Sse2.Max(flat, mask);
                // mask |= (abs(p1 - p0) > limit) * -1;
                // mask |= (abs(q1 - q0) > limit) * -1;
                work = Sse2.Max(
                    Sse2.Or(Sse2.SubtractSaturate(p2, p1), Sse2.SubtractSaturate(p1, p2)),
                    Sse2.Or(Sse2.SubtractSaturate(p3, p2), Sse2.SubtractSaturate(p2, p3)));
                mask = Sse2.Max(work, mask);
                work = Sse2.Max(
                    Sse2.Or(Sse2.SubtractSaturate(q2, q1), Sse2.SubtractSaturate(q1, q2)),
                    Sse2.Or(Sse2.SubtractSaturate(q3, q2), Sse2.SubtractSaturate(q2, q3)));
                mask = Sse2.Max(work, mask);
                mask = Sse2.SubtractSaturate(mask, limit);
                mask = Sse2.CompareEqual(mask, zero);
            }

            // filter4
            {
                Vector128<byte> t4 = Vector128.Create((byte)4);
                Vector128<byte> t3 = Vector128.Create((byte)3);
                Vector128<byte> t80 = Vector128.Create((byte)0x80);
                Vector128<byte> te0 = Vector128.Create((byte)0xe0);
                Vector128<byte> t1F = Vector128.Create((byte)0x1f);
                Vector128<byte> t1 = Vector128.Create((byte)0x1);
                Vector128<byte> t7F = Vector128.Create((byte)0x7f);

                Vector128<byte> ps1 = Sse2.Xor(Sse2.LoadVector128(s.ToPointer() - (2 * pitch)), t80);
                Vector128<byte> ps0 = Sse2.Xor(Sse2.LoadVector128(s.ToPointer() - (1 * pitch)), t80);
                Vector128<byte> qs0 = Sse2.Xor(Sse2.LoadVector128(s.ToPointer() + (0 * pitch)), t80);
                Vector128<byte> qs1 = Sse2.Xor(Sse2.LoadVector128(s.ToPointer() + (1 * pitch)), t80);
                Vector128<byte> filt;
                Vector128<byte> workA;
                Vector128<byte> filter1, filter2;

                filt = Sse2.And(Sse2.SubtractSaturate(ps1.AsSByte(), qs1.AsSByte()).AsByte(), hev);
                workA = Sse2.SubtractSaturate(qs0.AsSByte(), ps0.AsSByte()).AsByte();
                filt = Sse2.AddSaturate(filt.AsSByte(), workA.AsSByte()).AsByte();
                filt = Sse2.AddSaturate(filt.AsSByte(), workA.AsSByte()).AsByte();
                filt = Sse2.AddSaturate(filt.AsSByte(), workA.AsSByte()).AsByte();
                // (vpx_filter + 3 * (qs0 - ps0)) & mask
                filt = Sse2.And(filt, mask);

                filter1 = Sse2.AddSaturate(filt.AsSByte(), t4.AsSByte()).AsByte();
                filter2 = Sse2.AddSaturate(filt.AsSByte(), t3.AsSByte()).AsByte();

                // Filter1 >> 3
                workA = Sse2.CompareGreaterThan(zero.AsSByte(), filter1.AsSByte()).AsByte();
                filter1 = Sse2.ShiftRightLogical(filter1.AsInt16(), 3).AsByte();
                workA = Sse2.And(workA, te0);
                filter1 = Sse2.And(filter1, t1F);
                filter1 = Sse2.Or(filter1, workA);

                // Filter2 >> 3
                workA = Sse2.CompareGreaterThan(zero.AsSByte(), filter2.AsSByte()).AsByte();
                filter2 = Sse2.ShiftRightLogical(filter2.AsInt16(), 3).AsByte();
                workA = Sse2.And(workA, te0);
                filter2 = Sse2.And(filter2, t1F);
                filter2 = Sse2.Or(filter2, workA);

                // filt >> 1
                filt = Sse2.AddSaturate(filter1.AsSByte(), t1.AsSByte()).AsByte();
                workA = Sse2.CompareGreaterThan(zero.AsSByte(), filt.AsSByte()).AsByte();
                filt = Sse2.ShiftRightLogical(filt.AsInt16(), 1).AsByte();
                workA = Sse2.And(workA, t80);
                filt = Sse2.And(filt, t7F);
                filt = Sse2.Or(filt, workA);

                filt = Sse2.AndNot(hev, filt);

                q0 = Sse2.Xor(Sse2.SubtractSaturate(qs0.AsSByte(), filter1.AsSByte()).AsByte(), t80);
                q1 = Sse2.Xor(Sse2.SubtractSaturate(qs1.AsSByte(), filt.AsSByte()).AsByte(), t80);
                p0 = Sse2.Xor(Sse2.AddSaturate(ps0.AsSByte(), filter2.AsSByte()).AsByte(), t80);
                p1 = Sse2.Xor(Sse2.AddSaturate(ps1.AsSByte(), filt.AsSByte()).AsByte(), t80);

                Sse2.Store(s.ToPointer() - (2 * pitch), p1);
                Sse2.Store(s.ToPointer() - (1 * pitch), p0);
                Sse2.Store(s.ToPointer() + (0 * pitch), q0);
                Sse2.Store(s.ToPointer() + (1 * pitch), q1);
            }
        }

        private static unsafe void Transpose8X16(
            ArrayPtr<byte> in0,
            ArrayPtr<byte> in1,
            int inP,
            ArrayPtr<byte> output,
            int outP)
        {
            Vector128<byte> x0, x1, x2, x3, x4, x5, x6, x7;
            Vector128<byte> x8, x9, x10, x11, x12, x13, x14, x15;

            // 2-way interleave w/hoisting of unpacks
            x0 = Sse2.LoadScalarVector128((long*)in0.ToPointer()).AsByte(); // 1
            x1 = Sse2.LoadScalarVector128((long*)(in0.ToPointer() + inP)).AsByte(); // 3
            x0 = Sse2.UnpackLow(x0, x1); // 1

            x2 = Sse2.LoadScalarVector128((long*)(in0.ToPointer() + (2 * inP))).AsByte(); // 5
            x3 = Sse2.LoadScalarVector128((long*)(in0.ToPointer() + (3 * inP))).AsByte(); // 7
            x1 = Sse2.UnpackLow(x2, x3); // 2

            x4 = Sse2.LoadScalarVector128((long*)(in0.ToPointer() + (4 * inP))).AsByte(); // 9
            x5 = Sse2.LoadScalarVector128((long*)(in0.ToPointer() + (5 * inP))).AsByte(); // 11
            x2 = Sse2.UnpackLow(x4, x5); // 3

            x6 = Sse2.LoadScalarVector128((long*)(in0.ToPointer() + (6 * inP))).AsByte(); // 13
            x7 = Sse2.LoadScalarVector128((long*)(in0.ToPointer() + (7 * inP))).AsByte(); // 15
            x3 = Sse2.UnpackLow(x6, x7); // 4
            x4 = Sse2.UnpackLow(x0.AsInt16(), x1.AsInt16()).AsByte(); // 9

            x8 = Sse2.LoadScalarVector128((long*)in1.ToPointer()).AsByte(); // 2
            x9 = Sse2.LoadScalarVector128((long*)(in1.ToPointer() + inP)).AsByte(); // 4
            x8 = Sse2.UnpackLow(x8, x9); // 5
            x5 = Sse2.UnpackLow(x2.AsInt16(), x3.AsInt16()).AsByte(); // 10

            x10 = Sse2.LoadScalarVector128((long*)(in1.ToPointer() + (2 * inP))).AsByte(); // 6
            x11 = Sse2.LoadScalarVector128((long*)(in1.ToPointer() + (3 * inP))).AsByte(); // 8
            x9 = Sse2.UnpackLow(x10, x11); // 6

            x12 = Sse2.LoadScalarVector128((long*)(in1.ToPointer() + (4 * inP))).AsByte(); // 10
            x13 = Sse2.LoadScalarVector128((long*)(in1.ToPointer() + (5 * inP))).AsByte(); // 12
            x10 = Sse2.UnpackLow(x12, x13); // 7
            x12 = Sse2.UnpackLow(x8.AsInt16(), x9.AsInt16()).AsByte(); // 11

            x14 = Sse2.LoadScalarVector128((long*)(in1.ToPointer() + (6 * inP))).AsByte(); // 14
            x15 = Sse2.LoadScalarVector128((long*)(in1.ToPointer() + (7 * inP))).AsByte(); // 16
            x11 = Sse2.UnpackLow(x14, x15); // 8
            x13 = Sse2.UnpackLow(x10.AsInt16(), x11.AsInt16()).AsByte(); // 12

            x6 = Sse2.UnpackLow(x4.AsInt32(), x5.AsInt32()).AsByte(); // 13
            x7 = Sse2.UnpackHigh(x4.AsInt32(), x5.AsInt32()).AsByte(); // 14
            x14 = Sse2.UnpackLow(x12.AsInt32(), x13.AsInt32()).AsByte(); // 15
            x15 = Sse2.UnpackHigh(x12.AsInt32(), x13.AsInt32()).AsByte(); // 16

            // Store first 4-line result
            Sse2.Store(output.ToPointer(), Sse2.UnpackLow(x6.AsInt64(), x14.AsInt64()).AsByte());
            Sse2.Store(output.ToPointer() + outP, Sse2.UnpackHigh(x6.AsInt64(), x14.AsInt64()).AsByte());
            Sse2.Store(output.ToPointer() + (2 * outP), Sse2.UnpackLow(x7.AsInt64(), x15.AsInt64()).AsByte());
            Sse2.Store(output.ToPointer() + (3 * outP), Sse2.UnpackHigh(x7.AsInt64(), x15.AsInt64()).AsByte());

            x4 = Sse2.UnpackHigh(x0.AsInt16(), x1.AsInt16()).AsByte();
            x5 = Sse2.UnpackHigh(x2.AsInt16(), x3.AsInt16()).AsByte();
            x12 = Sse2.UnpackHigh(x8.AsInt16(), x9.AsInt16()).AsByte();
            x13 = Sse2.UnpackHigh(x10.AsInt16(), x11.AsInt16()).AsByte();

            x6 = Sse2.UnpackLow(x4.AsInt32(), x5.AsInt32()).AsByte();
            x7 = Sse2.UnpackHigh(x4.AsInt32(), x5.AsInt32()).AsByte();
            x14 = Sse2.UnpackLow(x12.AsInt32(), x13.AsInt32()).AsByte();
            x15 = Sse2.UnpackHigh(x12.AsInt32(), x13.AsInt32()).AsByte();

            // Store second 4-line result
            Sse2.Store(output.ToPointer() + (4 * outP), Sse2.UnpackLow(x6.AsInt64(), x14.AsInt64()).AsByte());
            Sse2.Store(output.ToPointer() + (5 * outP), Sse2.UnpackHigh(x6.AsInt64(), x14.AsInt64()).AsByte());
            Sse2.Store(output.ToPointer() + (6 * outP), Sse2.UnpackLow(x7.AsInt64(), x15.AsInt64()).AsByte());
            Sse2.Store(output.ToPointer() + (7 * outP), Sse2.UnpackHigh(x7.AsInt64(), x15.AsInt64()).AsByte());
        }

        private static unsafe void Transpose(
            ReadOnlySpan<ArrayPtr<byte>> src,
            int inP,
            ReadOnlySpan<ArrayPtr<byte>> dst,
            int outP,
            int num8X8ToTranspose)
        {
            int idx8X8 = 0;
            Vector128<byte> x0, x1, x2, x3, x4, x5, x6, x7;

            do
            {
                ArrayPtr<byte> input = src[idx8X8];
                ArrayPtr<byte> output = dst[idx8X8];

                x0 = Sse2.LoadScalarVector128((long*)(input.ToPointer() + (0 * inP)))
                    .AsByte(); // 00 01 02 03 04 05 06 07
                x1 = Sse2.LoadScalarVector128((long*)(input.ToPointer() + (1 * inP)))
                    .AsByte(); // 10 11 12 13 14 15 16 17
                // 00 10 01 11 02 12 03 13 04 14 05 15 06 16 07 17
                x0 = Sse2.UnpackLow(x0, x1);

                x2 = Sse2.LoadScalarVector128((long*)(input.ToPointer() + (2 * inP)))
                    .AsByte(); // 20 21 22 23 24 25 26 27
                x3 = Sse2.LoadScalarVector128((long*)(input.ToPointer() + (3 * inP)))
                    .AsByte(); // 30 31 32 33 34 35 36 37
                // 20 30 21 31 22 32 23 33 24 34 25 35 26 36 27 37
                x1 = Sse2.UnpackLow(x2, x3);

                x4 = Sse2.LoadScalarVector128((long*)(input.ToPointer() + (4 * inP)))
                    .AsByte(); // 40 41 42 43 44 45 46 47
                x5 = Sse2.LoadScalarVector128((long*)(input.ToPointer() + (5 * inP)))
                    .AsByte(); // 50 51 52 53 54 55 56 57
                // 40 50 41 51 42 52 43 53 44 54 45 55 46 56 47 57
                x2 = Sse2.UnpackLow(x4, x5);

                x6 = Sse2.LoadScalarVector128((long*)(input.ToPointer() + (6 * inP)))
                    .AsByte(); // 60 61 62 63 64 65 66 67
                x7 = Sse2.LoadScalarVector128((long*)(input.ToPointer() + (7 * inP)))
                    .AsByte(); // 70 71 72 73 74 75 76 77
                // 60 70 61 71 62 72 63 73 64 74 65 75 66 76 67 77
                x3 = Sse2.UnpackLow(x6, x7);

                // 00 10 20 30 01 11 21 31 02 12 22 32 03 13 23 33
                x4 = Sse2.UnpackLow(x0.AsInt16(), x1.AsInt16()).AsByte();
                // 40 50 60 70 41 51 61 71 42 52 62 72 43 53 63 73
                x5 = Sse2.UnpackLow(x2.AsInt16(), x3.AsInt16()).AsByte();
                // 00 10 20 30 40 50 60 70 01 11 21 31 41 51 61 71
                x6 = Sse2.UnpackLow(x4.AsInt32(), x5.AsInt32()).AsByte();
                Sse2.StoreScalar((long*)(output.ToPointer() + (0 * outP)), x6.AsInt64()); // 00 10 20 30 40 50 60 70
                Sse2.StoreHigh((double*)(output.ToPointer() + (1 * outP)), x6.AsDouble()); // 01 11 21 31 41 51 61 71
                // 02 12 22 32 42 52 62 72 03 13 23 33 43 53 63 73
                x7 = Sse2.UnpackHigh(x4.AsInt32(), x5.AsInt32()).AsByte();
                Sse2.StoreScalar((long*)(output.ToPointer() + (2 * outP)), x7.AsInt64()); // 02 12 22 32 42 52 62 72
                Sse2.StoreHigh((double*)(output.ToPointer() + (3 * outP)), x7.AsDouble()); // 03 13 23 33 43 53 63 73

                // 04 14 24 34 05 15 25 35 06 16 26 36 07 17 27 37
                x4 = Sse2.UnpackHigh(x0.AsInt16(), x1.AsInt16()).AsByte();
                // 44 54 64 74 45 55 65 75 46 56 66 76 47 57 67 77
                x5 = Sse2.UnpackHigh(x2.AsInt16(), x3.AsInt16()).AsByte();
                // 04 14 24 34 44 54 64 74 05 15 25 35 45 55 65 75
                x6 = Sse2.UnpackLow(x4.AsInt32(), x5.AsInt32()).AsByte();
                Sse2.StoreScalar((long*)(output.ToPointer() + (4 * outP)), x6.AsInt64()); // 04 14 24 34 44 54 64 74
                Sse2.StoreHigh((double*)(output.ToPointer() + (5 * outP)), x6.AsDouble()); // 05 15 25 35 45 55 65 75
                // 06 16 26 36 46 56 66 76 07 17 27 37 47 57 67 77
                x7 = Sse2.UnpackHigh(x4.AsInt32(), x5.AsInt32()).AsByte();

                Sse2.StoreScalar((long*)(output.ToPointer() + (6 * outP)), x7.AsInt64()); // 06 16 26 36 46 56 66 76
                Sse2.StoreHigh((double*)(output.ToPointer() + (7 * outP)), x7.AsDouble()); // 07 17 27 37 47 57 67 77
            } while (++idx8X8 < num8X8ToTranspose);
        }

        public static unsafe void LpfVertical4Dual(
            ArrayPtr<byte> s,
            int pitch,
            ReadOnlySpan<byte> blimit0,
            ReadOnlySpan<byte> limit0,
            ReadOnlySpan<byte> thresh0,
            ReadOnlySpan<byte> blimit1,
            ReadOnlySpan<byte> limit1,
            ReadOnlySpan<byte> thresh1)
        {
            ulong* tDstStorage = stackalloc ulong[16];
            ArrayPtr<byte> tDst = new((byte*)tDstStorage, 16 * 8);
            Span<ArrayPtr<byte>> src = stackalloc ArrayPtr<byte>[2];
            Span<ArrayPtr<byte>> dst = stackalloc ArrayPtr<byte>[2];

            // Transpose 8x16
            Transpose8X16(s.Slice(-4), s.Slice(-4 + (pitch * 8)), pitch, tDst, 16);

            // Loop filtering
            LpfHorizontal4Dual(tDst.Slice(4 * 16), 16, blimit0, limit0, thresh0, blimit1, limit1, thresh1);
            src[0] = tDst;
            src[1] = tDst.Slice(8);
            dst[0] = s.Slice(-4);
            dst[1] = s.Slice(-4 + (pitch * 8));

            // Transpose back
            Transpose(src, 16, dst, pitch, 2);
        }

        public static unsafe void LpfVertical8(
            ArrayPtr<byte> s,
            int pitch,
            ReadOnlySpan<byte> blimit,
            ReadOnlySpan<byte> limit,
            ReadOnlySpan<byte> thresh)
        {
            ulong* tDstStorage = stackalloc ulong[8];
            ArrayPtr<byte> tDst = new((byte*)tDstStorage, 8 * 8);
            Span<ArrayPtr<byte>> src = stackalloc ArrayPtr<byte>[1];
            Span<ArrayPtr<byte>> dst = stackalloc ArrayPtr<byte>[1];

            // Transpose 8x8
            src[0] = s.Slice(-4);
            dst[0] = tDst;

            Transpose(src, pitch, dst, 8, 1);

            // Loop filtering
            LpfHorizontal8(tDst.Slice(4 * 8), 8, blimit, limit, thresh);

            // Transpose back
            Transpose(dst, 8, src, pitch, 1);
        }

        public static unsafe void LpfVertical8Dual(
            ArrayPtr<byte> s,
            int pitch,
            ReadOnlySpan<byte> blimit0,
            ReadOnlySpan<byte> limit0,
            ReadOnlySpan<byte> thresh0,
            ReadOnlySpan<byte> blimit1,
            ReadOnlySpan<byte> limit1,
            ReadOnlySpan<byte> thresh1)
        {
            ulong* tDstStorage = stackalloc ulong[16];
            ArrayPtr<byte> tDst = new((byte*)tDstStorage, 16 * 8);
            Span<ArrayPtr<byte>> src = stackalloc ArrayPtr<byte>[2];
            Span<ArrayPtr<byte>> dst = stackalloc ArrayPtr<byte>[2];

            // Transpose 8x16
            Transpose8X16(s.Slice(-4), s.Slice(-4 + (pitch * 8)), pitch, tDst, 16);

            // Loop filtering
            LpfHorizontal8Dual(tDst.Slice(4 * 16), 16, blimit0, limit0, thresh0, blimit1, limit1, thresh1);

            src[0] = tDst;
            src[1] = tDst.Slice(8);

            dst[0] = s.Slice(-4);
            dst[1] = s.Slice(-4 + (pitch * 8));

            // Transpose back
            Transpose(src, 16, dst, pitch, 2);
        }

        public static unsafe void LpfVertical16(
            ArrayPtr<byte> s,
            int pitch,
            ReadOnlySpan<byte> blimit,
            ReadOnlySpan<byte> limit,
            ReadOnlySpan<byte> thresh)
        {
            ulong* tDstStorage = stackalloc ulong[16];
            ArrayPtr<byte> tDst = new((byte*)tDstStorage, 16 * 8);
            Span<ArrayPtr<byte>> src = stackalloc ArrayPtr<byte>[2];
            Span<ArrayPtr<byte>> dst = stackalloc ArrayPtr<byte>[2];

            src[0] = s.Slice(-8);
            src[1] = s;
            dst[0] = tDst;
            dst[1] = tDst.Slice(8 * 8);

            // Transpose 16x8
            Transpose(src, pitch, dst, 8, 2);

            // Loop filtering
            LpfHorizontal16(tDst.Slice(8 * 8), 8, blimit, limit, thresh);

            // Transpose back
            Transpose(dst, 8, src, pitch, 2);
        }

        public static unsafe void LpfVertical16Dual(
            ArrayPtr<byte> s,
            int pitch,
            ReadOnlySpan<byte> blimit,
            ReadOnlySpan<byte> limit,
            ReadOnlySpan<byte> thresh)
        {
            Vector128<byte>* tDstStorage = stackalloc Vector128<byte>[16];
            ArrayPtr<byte> tDst = new((byte*)tDstStorage, 256);

            // Transpose 16x16
            Transpose8X16(s.Slice(-8), s.Slice(-8 + (8 * pitch)), pitch, tDst, 16);
            Transpose8X16(s, s.Slice(8 * pitch), pitch, tDst.Slice(8 * 16), 16);

            // Loop filtering
            LpfHorizontal16Dual(tDst.Slice(8 * 16), 16, blimit, limit, thresh);

            // Transpose back
            Transpose8X16(tDst, tDst.Slice(8 * 16), 16, s.Slice(-8), pitch);
            Transpose8X16(tDst.Slice(8), tDst.Slice(8 + (8 * 16)), 16, s.Slice(-8 + (8 * pitch)), pitch);
        }
    }
}