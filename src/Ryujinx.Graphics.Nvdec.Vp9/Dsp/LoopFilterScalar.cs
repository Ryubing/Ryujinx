using Ryujinx.Common.Memory;
using Ryujinx.Graphics.Nvdec.Vp9.Common;
using System;

namespace Ryujinx.Graphics.Nvdec.Vp9.Dsp
{
    internal static class LoopFilterScalar
    {
        private static sbyte ClampSbyte(int t)
        {
            return (sbyte)Math.Clamp(t, -128, 127);
        }

        private static short ClampSbyteHigh(int t, int bd)
        {
            return bd switch
            {
                10 => (short)Math.Clamp(t, -128 * 4, (128 * 4) - 1),
                12 => (short)Math.Clamp(t, -128 * 16, (128 * 16) - 1),
                _ => (short)Math.Clamp(t, -128, 128 - 1)
            };
        }

        // Should we apply any filter at all: 11111111 yes, 00000000 no
        private static sbyte FilterMask(
            byte limit,
            byte blimit,
            byte p3,
            byte p2,
            byte p1,
            byte p0,
            byte q0,
            byte q1,
            byte q2,
            byte q3)
        {
            int mask = 0;
            mask |= Math.Abs(p3 - p2) > limit ? -1 : 0;
            mask |= Math.Abs(p2 - p1) > limit ? -1 : 0;
            mask |= Math.Abs(p1 - p0) > limit ? -1 : 0;
            mask |= Math.Abs(q1 - q0) > limit ? -1 : 0;
            mask |= Math.Abs(q2 - q1) > limit ? -1 : 0;
            mask |= Math.Abs(q3 - q2) > limit ? -1 : 0;
            mask |= (Math.Abs(p0 - q0) * 2) + (Math.Abs(p1 - q1) / 2) > blimit ? -1 : 0;
            return (sbyte)~mask;
        }

        private static sbyte FlatMask4(
            byte thresh,
            byte p3,
            byte p2,
            byte p1,
            byte p0,
            byte q0,
            byte q1,
            byte q2,
            byte q3)
        {
            int mask = 0;
            mask |= Math.Abs(p1 - p0) > thresh ? -1 : 0;
            mask |= Math.Abs(q1 - q0) > thresh ? -1 : 0;
            mask |= Math.Abs(p2 - p0) > thresh ? -1 : 0;
            mask |= Math.Abs(q2 - q0) > thresh ? -1 : 0;
            mask |= Math.Abs(p3 - p0) > thresh ? -1 : 0;
            mask |= Math.Abs(q3 - q0) > thresh ? -1 : 0;
            return (sbyte)~mask;
        }

        private static sbyte FlatMask5(
            byte thresh,
            byte p4,
            byte p3,
            byte p2,
            byte p1,
            byte p0,
            byte q0,
            byte q1,
            byte q2,
            byte q3,
            byte q4)
        {
            int mask = ~FlatMask4(thresh, p3, p2, p1, p0, q0, q1, q2, q3);
            mask |= Math.Abs(p4 - p0) > thresh ? -1 : 0;
            mask |= Math.Abs(q4 - q0) > thresh ? -1 : 0;
            return (sbyte)~mask;
        }

        // Is there high edge variance internal edge: 11111111 yes, 00000000 no
        private static sbyte HevMask(
            byte thresh,
            byte p1,
            byte p0,
            byte q0,
            byte q1)
        {
            int hev = 0;
            hev |= Math.Abs(p1 - p0) > thresh ? -1 : 0;
            hev |= Math.Abs(q1 - q0) > thresh ? -1 : 0;
            return (sbyte)hev;
        }

        private static void Filter4(
            sbyte mask,
            byte thresh,
            ref byte op1,
            ref byte op0,
            ref byte oq0,
            ref byte oq1)
        {
            sbyte filter1, filter2;

            sbyte ps1 = (sbyte)(op1 ^ 0x80);
            sbyte ps0 = (sbyte)(op0 ^ 0x80);
            sbyte qs0 = (sbyte)(oq0 ^ 0x80);
            sbyte qs1 = (sbyte)(oq1 ^ 0x80);
            sbyte hev = HevMask(thresh, op1, op0, oq0, oq1);

            // add outer taps if we have high edge variance
            sbyte filter = (sbyte)(ClampSbyte(ps1 - qs1) & hev);

            // inner taps
            filter = (sbyte)(ClampSbyte(filter + (3 * (qs0 - ps0))) & mask);

            // save bottom 3 bits so that we round one side +4 and the other +3
            // if it equals 4 we'll set it to adjust by -1 to account for the fact
            // we'd round it by 3 the other way
            filter1 = (sbyte)(ClampSbyte(filter + 4) >> 3);
            filter2 = (sbyte)(ClampSbyte(filter + 3) >> 3);

            oq0 = (byte)(ClampSbyte(qs0 - filter1) ^ 0x80);
            op0 = (byte)(ClampSbyte(ps0 + filter2) ^ 0x80);

            // outer tap adjustments
            filter = (sbyte)(BitUtils.RoundPowerOfTwo(filter1, 1) & ~hev);

            oq1 = (byte)(ClampSbyte(qs1 - filter) ^ 0x80);
            op1 = (byte)(ClampSbyte(ps1 + filter) ^ 0x80);
        }

        public static void LpfHorizontal4(
            ArrayPtr<byte> s,
            int pitch,
            byte blimit,
            byte limit,
            byte thresh)
        {
            // loop filter designed to work using chars so that we can make maximum use
            // of 8 bit simd instructions.
            for (int i = 0; i < 8; ++i)
            {
                byte p3 = s[-4 * pitch], p2 = s[-3 * pitch], p1 = s[-2 * pitch], p0 = s[-pitch];
                byte q0 = s[0 * pitch], q1 = s[1 * pitch], q2 = s[2 * pitch], q3 = s[3 * pitch];
                sbyte mask = FilterMask(limit, blimit, p3, p2, p1, p0, q0, q1, q2, q3);
                Filter4(mask, thresh, ref s[-2 * pitch], ref s[-1 * pitch], ref s[0], ref s[1 * pitch]);
                s = s.Slice(1);
            }
        }

        public static void LpfHorizontal4Dual(
            ArrayPtr<byte> s,
            int pitch,
            byte blimit0,
            byte limit0,
            byte thresh0,
            byte blimit1,
            byte limit1,
            byte thresh1)
        {
            LpfHorizontal4(s, pitch, blimit0, limit0, thresh0);
            LpfHorizontal4(s.Slice(8), pitch, blimit1, limit1, thresh1);
        }

        public static void LpfVertical4(
            ArrayPtr<byte> s,
            int pitch,
            byte blimit,
            byte limit,
            byte thresh)
        {
            // loop filter designed to work using chars so that we can make maximum use
            // of 8 bit simd instructions.
            for (int i = 0; i < 8; ++i)
            {
                byte p3 = s[-4], p2 = s[-3], p1 = s[-2], p0 = s[-1];
                byte q0 = s[0], q1 = s[1], q2 = s[2], q3 = s[3];
                sbyte mask = FilterMask(limit, blimit, p3, p2, p1, p0, q0, q1, q2, q3);
                Filter4(mask, thresh, ref s[-2], ref s[-1], ref s[0], ref s[1]);
                s = s.Slice(pitch);
            }
        }

        public static void LpfVertical4Dual(
            ArrayPtr<byte> s,
            int pitch,
            byte blimit0,
            byte limit0,
            byte thresh0,
            byte blimit1,
            byte limit1,
            byte thresh1)
        {
            LpfVertical4(s, pitch, blimit0, limit0, thresh0);
            LpfVertical4(s.Slice(8 * pitch), pitch, blimit1, limit1, thresh1);
        }

        private static void Filter8(
            sbyte mask,
            byte thresh,
            bool flat,
            ref byte op3,
            ref byte op2,
            ref byte op1,
            ref byte op0,
            ref byte oq0,
            ref byte oq1,
            ref byte oq2,
            ref byte oq3)
        {
            if (flat && mask != 0)
            {
                byte p3 = op3, p2 = op2, p1 = op1, p0 = op0;
                byte q0 = oq0, q1 = oq1, q2 = oq2, q3 = oq3;

                // 7-tap filter [1, 1, 1, 2, 1, 1, 1]
                op2 = (byte)BitUtils.RoundPowerOfTwo(p3 + p3 + p3 + (2 * p2) + p1 + p0 + q0, 3);
                op1 = (byte)BitUtils.RoundPowerOfTwo(p3 + p3 + p2 + (2 * p1) + p0 + q0 + q1, 3);
                op0 = (byte)BitUtils.RoundPowerOfTwo(p3 + p2 + p1 + (2 * p0) + q0 + q1 + q2, 3);
                oq0 = (byte)BitUtils.RoundPowerOfTwo(p2 + p1 + p0 + (2 * q0) + q1 + q2 + q3, 3);
                oq1 = (byte)BitUtils.RoundPowerOfTwo(p1 + p0 + q0 + (2 * q1) + q2 + q3 + q3, 3);
                oq2 = (byte)BitUtils.RoundPowerOfTwo(p0 + q0 + q1 + (2 * q2) + q3 + q3 + q3, 3);
            }
            else
            {
                Filter4(mask, thresh, ref op1, ref op0, ref oq0, ref oq1);
            }
        }

        public static void LpfHorizontal8(
            ArrayPtr<byte> s,
            int pitch,
            byte blimit,
            byte limit,
            byte thresh)
        {
            // loop filter designed to work using chars so that we can make maximum use
            // of 8 bit simd instructions.
            for (int i = 0; i < 8; ++i)
            {
                byte p3 = s[-4 * pitch], p2 = s[-3 * pitch], p1 = s[-2 * pitch], p0 = s[-pitch];
                byte q0 = s[0 * pitch], q1 = s[1 * pitch], q2 = s[2 * pitch], q3 = s[3 * pitch];

                sbyte mask = FilterMask(limit, blimit, p3, p2, p1, p0, q0, q1, q2, q3);
                sbyte flat = FlatMask4(1, p3, p2, p1, p0, q0, q1, q2, q3);
                Filter8(
                    mask,
                    thresh,
                    flat != 0,
                    ref s[-4 * pitch],
                    ref s[-3 * pitch],
                    ref s[-2 * pitch],
                    ref s[-1 * pitch],
                    ref s[0],
                    ref s[1 * pitch],
                    ref s[2 * pitch],
                    ref s[3 * pitch]);
                s = s.Slice(1);
            }
        }

        public static void LpfHorizontal8Dual(
            ArrayPtr<byte> s,
            int pitch,
            byte blimit0,
            byte limit0,
            byte thresh0,
            byte blimit1,
            byte limit1,
            byte thresh1)
        {
            LpfHorizontal8(s, pitch, blimit0, limit0, thresh0);
            LpfHorizontal8(s.Slice(8), pitch, blimit1, limit1, thresh1);
        }

        public static void LpfVertical8(
            ArrayPtr<byte> s,
            int pitch,
            byte blimit,
            byte limit,
            byte thresh)
        {
            for (int i = 0; i < 8; ++i)
            {
                byte p3 = s[-4], p2 = s[-3], p1 = s[-2], p0 = s[-1];
                byte q0 = s[0], q1 = s[1], q2 = s[2], q3 = s[3];
                sbyte mask = FilterMask(limit, blimit, p3, p2, p1, p0, q0, q1, q2, q3);
                sbyte flat = FlatMask4(1, p3, p2, p1, p0, q0, q1, q2, q3);
                Filter8(
                    mask,
                    thresh,
                    flat != 0,
                    ref s[-4],
                    ref s[-3],
                    ref s[-2],
                    ref s[-1],
                    ref s[0],
                    ref s[1],
                    ref s[2],
                    ref s[3]);
                s = s.Slice(pitch);
            }
        }

        public static void LpfVertical8Dual(
            ArrayPtr<byte> s,
            int pitch,
            byte blimit0,
            byte limit0,
            byte thresh0,
            byte blimit1,
            byte limit1,
            byte thresh1)
        {
            LpfVertical8(s, pitch, blimit0, limit0, thresh0);
            LpfVertical8(s.Slice(8 * pitch), pitch, blimit1, limit1, thresh1);
        }

        private static void Filter16(
            sbyte mask,
            byte thresh,
            bool flat,
            bool flat2,
            ref byte op7,
            ref byte op6,
            ref byte op5,
            ref byte op4,
            ref byte op3,
            ref byte op2,
            ref byte op1,
            ref byte op0,
            ref byte oq0,
            ref byte oq1,
            ref byte oq2,
            ref byte oq3,
            ref byte oq4,
            ref byte oq5,
            ref byte oq6,
            ref byte oq7)
        {
            if (flat2 && flat && mask != 0)
            {
                byte p7 = op7, p6 = op6, p5 = op5, p4 = op4, p3 = op3, p2 = op2, p1 = op1, p0 = op0;
                byte q0 = oq0, q1 = oq1, q2 = oq2, q3 = oq3, q4 = oq4, q5 = oq5, q6 = oq6, q7 = oq7;

                // 15-tap filter [1, 1, 1, 1, 1, 1, 1, 2, 1, 1, 1, 1, 1, 1, 1]
                op6 = (byte)BitUtils.RoundPowerOfTwo(
                    (p7 * 7) + (p6 * 2) + p5 + p4 + p3 + p2 + p1 + p0 + q0, 4);
                op5 = (byte)BitUtils.RoundPowerOfTwo(
                    (p7 * 6) + p6 + (p5 * 2) + p4 + p3 + p2 + p1 + p0 + q0 + q1, 4);
                op4 = (byte)BitUtils.RoundPowerOfTwo(
                    (p7 * 5) + p6 + p5 + (p4 * 2) + p3 + p2 + p1 + p0 + q0 + q1 + q2, 4);
                op3 = (byte)BitUtils.RoundPowerOfTwo(
                    (p7 * 4) + p6 + p5 + p4 + (p3 * 2) + p2 + p1 + p0 + q0 + q1 + q2 + q3, 4);
                op2 = (byte)BitUtils.RoundPowerOfTwo(
                    (p7 * 3) + p6 + p5 + p4 + p3 + (p2 * 2) + p1 + p0 + q0 + q1 + q2 + q3 + q4, 4);
                op1 = (byte)BitUtils.RoundPowerOfTwo(
                    (p7 * 2) + p6 + p5 + p4 + p3 + p2 + (p1 * 2) + p0 + q0 + q1 + q2 + q3 + q4 + q5, 4);
                op0 = (byte)BitUtils.RoundPowerOfTwo(
                    p7 + p6 + p5 + p4 + p3 + p2 + p1 + (p0 * 2) + q0 + q1 + q2 + q3 + q4 + q5 + q6, 4);
                oq0 = (byte)BitUtils.RoundPowerOfTwo(
                    p6 + p5 + p4 + p3 + p2 + p1 + p0 + (q0 * 2) + q1 + q2 + q3 + q4 + q5 + q6 + q7, 4);
                oq1 = (byte)BitUtils.RoundPowerOfTwo(
                    p5 + p4 + p3 + p2 + p1 + p0 + q0 + (q1 * 2) + q2 + q3 + q4 + q5 + q6 + (q7 * 2), 4);
                oq2 = (byte)BitUtils.RoundPowerOfTwo(
                    p4 + p3 + p2 + p1 + p0 + q0 + q1 + (q2 * 2) + q3 + q4 + q5 + q6 + (q7 * 3), 4);
                oq3 = (byte)BitUtils.RoundPowerOfTwo(
                    p3 + p2 + p1 + p0 + q0 + q1 + q2 + (q3 * 2) + q4 + q5 + q6 + (q7 * 4), 4);
                oq4 = (byte)BitUtils.RoundPowerOfTwo(
                    p2 + p1 + p0 + q0 + q1 + q2 + q3 + (q4 * 2) + q5 + q6 + (q7 * 5), 4);
                oq5 = (byte)BitUtils.RoundPowerOfTwo(
                    p1 + p0 + q0 + q1 + q2 + q3 + q4 + (q5 * 2) + q6 + (q7 * 6), 4);
                oq6 = (byte)BitUtils.RoundPowerOfTwo(
                    p0 + q0 + q1 + q2 + q3 + q4 + q5 + (q6 * 2) + (q7 * 7), 4);
            }
            else
            {
                Filter8(mask, thresh, flat, ref op3, ref op2, ref op1, ref op0, ref oq0, ref oq1, ref oq2, ref oq3);
            }
        }

        private static void MbLpfHorizontalEdgeW(
            ArrayPtr<byte> s,
            int pitch,
            byte blimit,
            byte limit,
            byte thresh,
            int count)
        {
            // loop filter designed to work using chars so that we can make maximum use
            // of 8 bit simd instructions.
            for (int i = 0; i < 8 * count; ++i)
            {
                byte p3 = s[-4 * pitch], p2 = s[-3 * pitch], p1 = s[-2 * pitch], p0 = s[-pitch];
                byte q0 = s[0 * pitch], q1 = s[1 * pitch], q2 = s[2 * pitch], q3 = s[3 * pitch];
                sbyte mask = FilterMask(limit, blimit, p3, p2, p1, p0, q0, q1, q2, q3);
                sbyte flat = FlatMask4(1, p3, p2, p1, p0, q0, q1, q2, q3);
                sbyte flat2 = FlatMask5(
                    1,
                    s[-8 * pitch],
                    s[-7 * pitch],
                    s[-6 * pitch],
                    s[-5 * pitch],
                    p0,
                    q0,
                    s[4 * pitch],
                    s[5 * pitch],
                    s[6 * pitch],
                    s[7 * pitch]);

                Filter16(
                    mask,
                    thresh,
                    flat != 0,
                    flat2 != 0,
                    ref s[-8 * pitch],
                    ref s[-7 * pitch],
                    ref s[-6 * pitch],
                    ref s[-5 * pitch],
                    ref s[-4 * pitch],
                    ref s[-3 * pitch],
                    ref s[-2 * pitch],
                    ref s[-1 * pitch],
                    ref s[0],
                    ref s[1 * pitch],
                    ref s[2 * pitch],
                    ref s[3 * pitch],
                    ref s[4 * pitch],
                    ref s[5 * pitch],
                    ref s[6 * pitch],
                    ref s[7 * pitch]);
                s = s.Slice(1);
            }
        }

        public static void LpfHorizontal16(
            ArrayPtr<byte> s,
            int pitch,
            byte blimit,
            byte limit,
            byte thresh)
        {
            MbLpfHorizontalEdgeW(s, pitch, blimit, limit, thresh, 1);
        }

        public static void LpfHorizontal16Dual(
            ArrayPtr<byte> s,
            int pitch,
            byte blimit,
            byte limit,
            byte thresh)
        {
            MbLpfHorizontalEdgeW(s, pitch, blimit, limit, thresh, 2);
        }

        private static void MbLpfVerticalEdgeW(
            ArrayPtr<byte> s,
            int pitch,
            byte blimit,
            byte limit,
            byte thresh,
            int count)
        {
            for (int i = 0; i < count; ++i)
            {
                byte p3 = s[-4], p2 = s[-3], p1 = s[-2], p0 = s[-1];
                byte q0 = s[0], q1 = s[1], q2 = s[2], q3 = s[3];
                sbyte mask = FilterMask(limit, blimit, p3, p2, p1, p0, q0, q1, q2, q3);
                sbyte flat = FlatMask4(1, p3, p2, p1, p0, q0, q1, q2, q3);
                sbyte flat2 = FlatMask5(1, s[-8], s[-7], s[-6], s[-5], p0, q0, s[4], s[5], s[6], s[7]);

                Filter16(
                    mask,
                    thresh,
                    flat != 0,
                    flat2 != 0,
                    ref s[-8],
                    ref s[-7],
                    ref s[-6],
                    ref s[-5],
                    ref s[-4],
                    ref s[-3],
                    ref s[-2],
                    ref s[-1],
                    ref s[0],
                    ref s[1],
                    ref s[2],
                    ref s[3],
                    ref s[4],
                    ref s[5],
                    ref s[6],
                    ref s[7]);
                s = s.Slice(pitch);
            }
        }

        public static void LpfVertical16(
            ArrayPtr<byte> s,
            int pitch,
            byte blimit,
            byte limit,
            byte thresh)
        {
            MbLpfVerticalEdgeW(s, pitch, blimit, limit, thresh, 8);
        }

        public static void LpfVertical16Dual(
            ArrayPtr<byte> s,
            int pitch,
            byte blimit,
            byte limit,
            byte thresh)
        {
            MbLpfVerticalEdgeW(s, pitch, blimit, limit, thresh, 16);
        }

        // Should we apply any filter at all: 11111111 yes, 00000000 no ?
        private static sbyte HighBdFilterMask(
            byte limit,
            byte blimit,
            ushort p3,
            ushort p2,
            ushort p1,
            ushort p0,
            ushort q0,
            ushort q1,
            ushort q2,
            ushort q3,
            int bd)
        {
            int mask = 0;
            short limit16 = (short)(limit << (bd - 8));
            short blimit16 = (short)(blimit << (bd - 8));
            mask |= Math.Abs(p3 - p2) > limit16 ? -1 : 0;
            mask |= Math.Abs(p2 - p1) > limit16 ? -1 : 0;
            mask |= Math.Abs(p1 - p0) > limit16 ? -1 : 0;
            mask |= Math.Abs(q1 - q0) > limit16 ? -1 : 0;
            mask |= Math.Abs(q2 - q1) > limit16 ? -1 : 0;
            mask |= Math.Abs(q3 - q2) > limit16 ? -1 : 0;
            mask |= (Math.Abs(p0 - q0) * 2) + (Math.Abs(p1 - q1) / 2) > blimit16 ? -1 : 0;
            return (sbyte)~mask;
        }

        private static sbyte HighBdFlatMask4(
            byte thresh,
            ushort p3,
            ushort p2,
            ushort p1,
            ushort p0,
            ushort q0,
            ushort q1,
            ushort q2,
            ushort q3,
            int bd)
        {
            int mask = 0;
            short thresh16 = (short)(thresh << (bd - 8));
            mask |= Math.Abs(p1 - p0) > thresh16 ? -1 : 0;
            mask |= Math.Abs(q1 - q0) > thresh16 ? -1 : 0;
            mask |= Math.Abs(p2 - p0) > thresh16 ? -1 : 0;
            mask |= Math.Abs(q2 - q0) > thresh16 ? -1 : 0;
            mask |= Math.Abs(p3 - p0) > thresh16 ? -1 : 0;
            mask |= Math.Abs(q3 - q0) > thresh16 ? -1 : 0;
            return (sbyte)~mask;
        }

        private static sbyte HighBdFlatMask5(
            byte thresh,
            ushort p4,
            ushort p3,
            ushort p2,
            ushort p1,
            ushort p0,
            ushort q0,
            ushort q1,
            ushort q2,
            ushort q3,
            ushort q4,
            int bd)
        {
            int mask = ~HighBdFlatMask4(thresh, p3, p2, p1, p0, q0, q1, q2, q3, bd);
            short thresh16 = (short)(thresh << (bd - 8));
            mask |= Math.Abs(p4 - p0) > thresh16 ? -1 : 0;
            mask |= Math.Abs(q4 - q0) > thresh16 ? -1 : 0;
            return (sbyte)~mask;
        }

        // Is there high edge variance internal edge:
        // 11111111_11111111 yes, 00000000_00000000 no ?
        private static short HighBdHevMask(
            byte thresh,
            ushort p1,
            ushort p0,
            ushort q0,
            ushort q1,
            int bd)
        {
            int hev = 0;
            short thresh16 = (short)(thresh << (bd - 8));
            hev |= Math.Abs(p1 - p0) > thresh16 ? -1 : 0;
            hev |= Math.Abs(q1 - q0) > thresh16 ? -1 : 0;
            return (short)hev;
        }

        private static void HighBdFilter4(
            sbyte mask,
            byte thresh,
            ref ushort op1,
            ref ushort op0,
            ref ushort oq0,
            ref ushort oq1,
            int bd)
        {
            short filter1, filter2;
            // ^0x80 equivalent to subtracting 0x80 from the values to turn them
            // into -128 to +127 instead of 0 to 255.
            int shift = bd - 8;
            short ps1 = (short)((short)op1 - (0x80 << shift));
            short ps0 = (short)((short)op0 - (0x80 << shift));
            short qs0 = (short)((short)oq0 - (0x80 << shift));
            short qs1 = (short)((short)oq1 - (0x80 << shift));
            short hev = HighBdHevMask(thresh, op1, op0, oq0, oq1, bd);

            // Add outer taps if we have high edge variance.
            short filter = (short)(ClampSbyteHigh(ps1 - qs1, bd) & hev);

            // Inner taps.
            filter = (short)(ClampSbyteHigh(filter + (3 * (qs0 - ps0)), bd) & mask);

            // Save bottom 3 bits so that we round one side +4 and the other +3
            // if it equals 4 we'll set it to adjust by -1 to account for the fact
            // we'd round it by 3 the other way.
            filter1 = (short)(ClampSbyteHigh(filter + 4, bd) >> 3);
            filter2 = (short)(ClampSbyteHigh(filter + 3, bd) >> 3);

            oq0 = (ushort)(ClampSbyteHigh(qs0 - filter1, bd) + (0x80 << shift));
            op0 = (ushort)(ClampSbyteHigh(ps0 + filter2, bd) + (0x80 << shift));

            // Outer tap adjustments.
            filter = (short)(BitUtils.RoundPowerOfTwo(filter1, 1) & ~hev);

            oq1 = (ushort)(ClampSbyteHigh(qs1 - filter, bd) + (0x80 << shift));
            op1 = (ushort)(ClampSbyteHigh(ps1 + filter, bd) + (0x80 << shift));
        }

        public static void HighBdLpfHorizontal4(
            ArrayPtr<ushort> s,
            int pitch,
            byte blimit,
            byte limit,
            byte thresh,
            int bd)
        {
            // loop filter designed to work using chars so that we can make maximum use
            // of 8 bit simd instructions.
            for (int i = 0; i < 8; ++i)
            {
                ushort p3 = s[-4 * pitch];
                ushort p2 = s[-3 * pitch];
                ushort p1 = s[-2 * pitch];
                ushort p0 = s[-pitch];
                ushort q0 = s[0 * pitch];
                ushort q1 = s[1 * pitch];
                ushort q2 = s[2 * pitch];
                ushort q3 = s[3 * pitch];
                sbyte mask = HighBdFilterMask(limit, blimit, p3, p2, p1, p0, q0, q1, q2, q3, bd);
                HighBdFilter4(mask, thresh, ref s[-2 * pitch], ref s[-1 * pitch], ref s[0], ref s[1 * pitch], bd);
                s = s.Slice(1);
            }
        }

        public static void HighBdLpfHorizontal4Dual(
            ArrayPtr<ushort> s,
            int pitch,
            byte blimit0,
            byte limit0,
            byte thresh0,
            byte blimit1,
            byte limit1,
            byte thresh1,
            int bd)
        {
            HighBdLpfHorizontal4(s, pitch, blimit0, limit0, thresh0, bd);
            HighBdLpfHorizontal4(s.Slice(8), pitch, blimit1, limit1, thresh1, bd);
        }

        public static void HighBdLpfVertical4(
            ArrayPtr<ushort> s,
            int pitch,
            byte blimit,
            byte limit,
            byte thresh,
            int bd)
        {
            // loop filter designed to work using chars so that we can make maximum use
            // of 8 bit simd instructions.
            for (int i = 0; i < 8; ++i)
            {
                ushort p3 = s[-4], p2 = s[-3], p1 = s[-2], p0 = s[-1];
                ushort q0 = s[0], q1 = s[1], q2 = s[2], q3 = s[3];
                sbyte mask = HighBdFilterMask(limit, blimit, p3, p2, p1, p0, q0, q1, q2, q3, bd);
                HighBdFilter4(mask, thresh, ref s[-2], ref s[-1], ref s[0], ref s[1], bd);
                s = s.Slice(pitch);
            }
        }

        public static void HighBdLpfVertical4Dual(
            ArrayPtr<ushort> s,
            int pitch,
            byte blimit0,
            byte limit0,
            byte thresh0,
            byte blimit1,
            byte limit1,
            byte thresh1,
            int bd)
        {
            HighBdLpfVertical4(s, pitch, blimit0, limit0, thresh0, bd);
            HighBdLpfVertical4(s.Slice(8 * pitch), pitch, blimit1, limit1, thresh1, bd);
        }

        private static void HighBdFilter8(
            sbyte mask,
            byte thresh,
            bool flat,
            ref ushort op3,
            ref ushort op2,
            ref ushort op1,
            ref ushort op0,
            ref ushort oq0,
            ref ushort oq1,
            ref ushort oq2,
            ref ushort oq3,
            int bd)
        {
            if (flat && mask != 0)
            {
                ushort p3 = op3, p2 = op2, p1 = op1, p0 = op0;
                ushort q0 = oq0, q1 = oq1, q2 = oq2, q3 = oq3;

                // 7-tap filter [1, 1, 1, 2, 1, 1, 1]
                op2 = (ushort)BitUtils.RoundPowerOfTwo(p3 + p3 + p3 + (2 * p2) + p1 + p0 + q0, 3);
                op1 = (ushort)BitUtils.RoundPowerOfTwo(p3 + p3 + p2 + (2 * p1) + p0 + q0 + q1, 3);
                op0 = (ushort)BitUtils.RoundPowerOfTwo(p3 + p2 + p1 + (2 * p0) + q0 + q1 + q2, 3);
                oq0 = (ushort)BitUtils.RoundPowerOfTwo(p2 + p1 + p0 + (2 * q0) + q1 + q2 + q3, 3);
                oq1 = (ushort)BitUtils.RoundPowerOfTwo(p1 + p0 + q0 + (2 * q1) + q2 + q3 + q3, 3);
                oq2 = (ushort)BitUtils.RoundPowerOfTwo(p0 + q0 + q1 + (2 * q2) + q3 + q3 + q3, 3);
            }
            else
            {
                HighBdFilter4(mask, thresh, ref op1, ref op0, ref oq0, ref oq1, bd);
            }
        }

        public static void HighBdLpfHorizontal8(
            ArrayPtr<ushort> s,
            int pitch,
            byte blimit,
            byte limit,
            byte thresh,
            int bd)
        {
            // loop filter designed to work using chars so that we can make maximum use
            // of 8 bit simd instructions.
            for (int i = 0; i < 8; ++i)
            {
                ushort p3 = s[-4 * pitch], p2 = s[-3 * pitch], p1 = s[-2 * pitch], p0 = s[-pitch];
                ushort q0 = s[0 * pitch], q1 = s[1 * pitch], q2 = s[2 * pitch], q3 = s[3 * pitch];

                sbyte mask = HighBdFilterMask(limit, blimit, p3, p2, p1, p0, q0, q1, q2, q3, bd);
                sbyte flat = HighBdFlatMask4(1, p3, p2, p1, p0, q0, q1, q2, q3, bd);
                HighBdFilter8(
                    mask,
                    thresh,
                    flat != 0,
                    ref s[-4 * pitch],
                    ref s[-3 * pitch],
                    ref s[-2 * pitch],
                    ref s[-1 * pitch],
                    ref s[0],
                    ref s[1 * pitch],
                    ref s[2 * pitch],
                    ref s[3 * pitch],
                    bd);
                s = s.Slice(1);
            }
        }

        public static void HighBdLpfHorizontal8Dual(
            ArrayPtr<ushort> s,
            int pitch,
            byte blimit0,
            byte limit0,
            byte thresh0,
            byte blimit1,
            byte limit1,
            byte thresh1,
            int bd)
        {
            HighBdLpfHorizontal8(s, pitch, blimit0, limit0, thresh0, bd);
            HighBdLpfHorizontal8(s.Slice(8), pitch, blimit1, limit1, thresh1, bd);
        }

        public static void HighBdLpfVertical8(
            ArrayPtr<ushort> s,
            int pitch,
            byte blimit,
            byte limit,
            byte thresh,
            int bd)
        {
            for (int i = 0; i < 8; ++i)
            {
                ushort p3 = s[-4], p2 = s[-3], p1 = s[-2], p0 = s[-1];
                ushort q0 = s[0], q1 = s[1], q2 = s[2], q3 = s[3];
                sbyte mask = HighBdFilterMask(limit, blimit, p3, p2, p1, p0, q0, q1, q2, q3, bd);
                sbyte flat = HighBdFlatMask4(1, p3, p2, p1, p0, q0, q1, q2, q3, bd);
                HighBdFilter8(
                    mask,
                    thresh,
                    flat != 0,
                    ref s[-4],
                    ref s[-3],
                    ref s[-2],
                    ref s[-1],
                    ref s[0],
                    ref s[1],
                    ref s[2],
                    ref s[3],
                    bd);
                s = s.Slice(pitch);
            }
        }

        public static void HighBdLpfVertical8Dual(
            ArrayPtr<ushort> s,
            int pitch,
            byte blimit0,
            byte limit0,
            byte thresh0,
            byte blimit1,
            byte limit1,
            byte thresh1,
            int bd)
        {
            HighBdLpfVertical8(s, pitch, blimit0, limit0, thresh0, bd);
            HighBdLpfVertical8(s.Slice(8 * pitch), pitch, blimit1, limit1, thresh1, bd);
        }

        private static void HighBdFilter16(
            sbyte mask,
            byte thresh,
            bool flat,
            bool flat2,
            ref ushort op7,
            ref ushort op6,
            ref ushort op5,
            ref ushort op4,
            ref ushort op3,
            ref ushort op2,
            ref ushort op1,
            ref ushort op0,
            ref ushort oq0,
            ref ushort oq1,
            ref ushort oq2,
            ref ushort oq3,
            ref ushort oq4,
            ref ushort oq5,
            ref ushort oq6,
            ref ushort oq7,
            int bd)
        {
            if (flat2 && flat && mask != 0)
            {
                ushort p7 = op7;
                ushort p6 = op6;
                ushort p5 = op5;
                ushort p4 = op4;
                ushort p3 = op3;
                ushort p2 = op2;
                ushort p1 = op1;
                ushort p0 = op0;
                ushort q0 = oq0;
                ushort q1 = oq1;
                ushort q2 = oq2;
                ushort q3 = oq3;
                ushort q4 = oq4;
                ushort q5 = oq5;
                ushort q6 = oq6;
                ushort q7 = oq7;

                // 15-tap filter [1, 1, 1, 1, 1, 1, 1, 2, 1, 1, 1, 1, 1, 1, 1]
                op6 = (ushort)BitUtils.RoundPowerOfTwo(
                    (p7 * 7) + (p6 * 2) + p5 + p4 + p3 + p2 + p1 + p0 + q0, 4);
                op5 = (ushort)BitUtils.RoundPowerOfTwo(
                    (p7 * 6) + p6 + (p5 * 2) + p4 + p3 + p2 + p1 + p0 + q0 + q1, 4);
                op4 = (ushort)BitUtils.RoundPowerOfTwo(
                    (p7 * 5) + p6 + p5 + (p4 * 2) + p3 + p2 + p1 + p0 + q0 + q1 + q2, 4);
                op3 = (ushort)BitUtils.RoundPowerOfTwo(
                    (p7 * 4) + p6 + p5 + p4 + (p3 * 2) + p2 + p1 + p0 + q0 + q1 + q2 + q3, 4);
                op2 = (ushort)BitUtils.RoundPowerOfTwo(
                    (p7 * 3) + p6 + p5 + p4 + p3 + (p2 * 2) + p1 + p0 + q0 + q1 + q2 + q3 + q4, 4);
                op1 = (ushort)BitUtils.RoundPowerOfTwo(
                    (p7 * 2) + p6 + p5 + p4 + p3 + p2 + (p1 * 2) + p0 + q0 + q1 + q2 + q3 + q4 + q5, 4);
                op0 = (ushort)BitUtils.RoundPowerOfTwo(
                    p7 + p6 + p5 + p4 + p3 + p2 + p1 + (p0 * 2) + q0 + q1 + q2 + q3 + q4 + q5 + q6, 4);
                oq0 = (ushort)BitUtils.RoundPowerOfTwo(
                    p6 + p5 + p4 + p3 + p2 + p1 + p0 + (q0 * 2) + q1 + q2 + q3 + q4 + q5 + q6 + q7, 4);
                oq1 = (ushort)BitUtils.RoundPowerOfTwo(
                    p5 + p4 + p3 + p2 + p1 + p0 + q0 + (q1 * 2) + q2 + q3 + q4 + q5 + q6 + (q7 * 2), 4);
                oq2 = (ushort)BitUtils.RoundPowerOfTwo(
                    p4 + p3 + p2 + p1 + p0 + q0 + q1 + (q2 * 2) + q3 + q4 + q5 + q6 + (q7 * 3), 4);
                oq3 = (ushort)BitUtils.RoundPowerOfTwo(
                    p3 + p2 + p1 + p0 + q0 + q1 + q2 + (q3 * 2) + q4 + q5 + q6 + (q7 * 4), 4);
                oq4 = (ushort)BitUtils.RoundPowerOfTwo(
                    p2 + p1 + p0 + q0 + q1 + q2 + q3 + (q4 * 2) + q5 + q6 + (q7 * 5), 4);
                oq5 = (ushort)BitUtils.RoundPowerOfTwo(
                    p1 + p0 + q0 + q1 + q2 + q3 + q4 + (q5 * 2) + q6 + (q7 * 6), 4);
                oq6 = (ushort)BitUtils.RoundPowerOfTwo(
                    p0 + q0 + q1 + q2 + q3 + q4 + q5 + (q6 * 2) + (q7 * 7), 4);
            }
            else
            {
                HighBdFilter8(mask, thresh, flat, ref op3, ref op2, ref op1, ref op0, ref oq0, ref oq1, ref oq2,
                    ref oq3, bd);
            }
        }

        private static void HighBdMbLpfHorizontalEdgeW(
            ArrayPtr<ushort> s,
            int pitch,
            byte blimit,
            byte limit,
            byte thresh,
            int count,
            int bd)
        {
            // loop filter designed to work using chars so that we can make maximum use
            // of 8 bit simd instructions.
            for (int i = 0; i < 8 * count; ++i)
            {
                ushort p3 = s[-4 * pitch];
                ushort p2 = s[-3 * pitch];
                ushort p1 = s[-2 * pitch];
                ushort p0 = s[-pitch];
                ushort q0 = s[0 * pitch];
                ushort q1 = s[1 * pitch];
                ushort q2 = s[2 * pitch];
                ushort q3 = s[3 * pitch];
                sbyte mask = HighBdFilterMask(limit, blimit, p3, p2, p1, p0, q0, q1, q2, q3, bd);
                sbyte flat = HighBdFlatMask4(1, p3, p2, p1, p0, q0, q1, q2, q3, bd);
                sbyte flat2 = HighBdFlatMask5(
                    1,
                    s[-8 * pitch],
                    s[-7 * pitch],
                    s[-6 * pitch],
                    s[-5 * pitch],
                    p0,
                    q0,
                    s[4 * pitch],
                    s[5 * pitch],
                    s[6 * pitch],
                    s[7 * pitch],
                    bd);

                HighBdFilter16(
                    mask,
                    thresh,
                    flat != 0,
                    flat2 != 0,
                    ref s[-8 * pitch],
                    ref s[-7 * pitch],
                    ref s[-6 * pitch],
                    ref s[-5 * pitch],
                    ref s[-4 * pitch],
                    ref s[-3 * pitch],
                    ref s[-2 * pitch],
                    ref s[-1 * pitch],
                    ref s[0],
                    ref s[1 * pitch],
                    ref s[2 * pitch],
                    ref s[3 * pitch],
                    ref s[4 * pitch],
                    ref s[5 * pitch],
                    ref s[6 * pitch],
                    ref s[7 * pitch],
                    bd);
                s = s.Slice(1);
            }
        }

        public static void HighBdLpfHorizontal16(
            ArrayPtr<ushort> s,
            int pitch,
            byte blimit,
            byte limit,
            byte thresh,
            int bd)
        {
            HighBdMbLpfHorizontalEdgeW(s, pitch, blimit, limit, thresh, 1, bd);
        }

        public static void HighBdLpfHorizontal16Dual(
            ArrayPtr<ushort> s,
            int pitch,
            byte blimit,
            byte limit,
            byte thresh,
            int bd)
        {
            HighBdMbLpfHorizontalEdgeW(s, pitch, blimit, limit, thresh, 2, bd);
        }

        private static void HighBdMbLpfVerticalEdgeW(
            ArrayPtr<ushort> s,
            int pitch,
            byte blimit,
            byte limit,
            byte thresh,
            int count,
            int bd)
        {
            for (int i = 0; i < count; ++i)
            {
                ushort p3 = s[-4];
                ushort p2 = s[-3];
                ushort p1 = s[-2];
                ushort p0 = s[-1];
                ushort q0 = s[0];
                ushort q1 = s[1];
                ushort q2 = s[2];
                ushort q3 = s[3];
                sbyte mask = HighBdFilterMask(limit, blimit, p3, p2, p1, p0, q0, q1, q2, q3, bd);
                sbyte flat = HighBdFlatMask4(1, p3, p2, p1, p0, q0, q1, q2, q3, bd);
                sbyte flat2 = HighBdFlatMask5(1, s[-8], s[-7], s[-6], s[-5], p0, q0, s[4], s[5], s[6], s[7], bd);

                HighBdFilter16(
                    mask,
                    thresh,
                    flat != 0,
                    flat2 != 0,
                    ref s[-8],
                    ref s[-7],
                    ref s[-6],
                    ref s[-5],
                    ref s[-4],
                    ref s[-3],
                    ref s[-2],
                    ref s[-1],
                    ref s[0],
                    ref s[1],
                    ref s[2],
                    ref s[3],
                    ref s[4],
                    ref s[5],
                    ref s[6],
                    ref s[7],
                    bd);
                s = s.Slice(pitch);
            }
        }

        public static void HighBdLpfVertical16(
            ArrayPtr<ushort> s,
            int pitch,
            byte blimit,
            byte limit,
            byte thresh,
            int bd)
        {
            HighBdMbLpfVerticalEdgeW(s, pitch, blimit, limit, thresh, 8, bd);
        }

        public static void HighBdLpfVertical16Dual(
            ArrayPtr<ushort> s,
            int pitch,
            byte blimit,
            byte limit,
            byte thresh,
            int bd)
        {
            HighBdMbLpfVerticalEdgeW(s, pitch, blimit, limit, thresh, 16, bd);
        }
    }
}