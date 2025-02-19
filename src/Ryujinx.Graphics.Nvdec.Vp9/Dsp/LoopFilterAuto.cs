using Ryujinx.Common.Memory;
using System;
using System.Runtime.Intrinsics.X86;

namespace Ryujinx.Graphics.Nvdec.Vp9.Dsp
{
    internal class LoopFilterAuto
    {
        public static void LpfHorizontal4(
            ArrayPtr<byte> s,
            int pitch,
            ReadOnlySpan<byte> blimit,
            ReadOnlySpan<byte> limit,
            ReadOnlySpan<byte> thresh)
        {
            if (Sse2.IsSupported)
            {
                LoopFilterSse2.LpfHorizontal4(s, pitch, blimit, limit, thresh);
            }
            else
            {
                LoopFilterScalar.LpfHorizontal4(s, pitch, blimit[0], limit[0], thresh[0]);
            }
        }

        public static void LpfHorizontal4Dual(
            ArrayPtr<byte> s,
            int pitch,
            ReadOnlySpan<byte> blimit0,
            ReadOnlySpan<byte> limit0,
            ReadOnlySpan<byte> thresh0,
            ReadOnlySpan<byte> blimit1,
            ReadOnlySpan<byte> limit1,
            ReadOnlySpan<byte> thresh1)
        {
            if (Sse2.IsSupported)
            {
                LoopFilterSse2.LpfHorizontal4Dual(s, pitch, blimit0, limit0, thresh0, blimit1, limit1, thresh1);
            }
            else
            {
                LoopFilterScalar.LpfHorizontal4Dual(s, pitch, blimit0[0], limit0[0], thresh0[0], blimit1[0], limit1[0],
                    thresh1[0]);
            }
        }

        public static void LpfHorizontal8(
            ArrayPtr<byte> s,
            int pitch,
            ReadOnlySpan<byte> blimit,
            ReadOnlySpan<byte> limit,
            ReadOnlySpan<byte> thresh)
        {
            if (Sse2.IsSupported)
            {
                LoopFilterSse2.LpfHorizontal8(s, pitch, blimit, limit, thresh);
            }
            else
            {
                LoopFilterScalar.LpfHorizontal8(s, pitch, blimit[0], limit[0], thresh[0]);
            }
        }

        public static void LpfHorizontal8Dual(
            ArrayPtr<byte> s,
            int pitch,
            ReadOnlySpan<byte> blimit0,
            ReadOnlySpan<byte> limit0,
            ReadOnlySpan<byte> thresh0,
            ReadOnlySpan<byte> blimit1,
            ReadOnlySpan<byte> limit1,
            ReadOnlySpan<byte> thresh1)
        {
            if (Sse2.IsSupported)
            {
                LoopFilterSse2.LpfHorizontal8Dual(s, pitch, blimit0, limit0, thresh0, blimit1, limit1, thresh1);
            }
            else
            {
                LoopFilterScalar.LpfHorizontal8Dual(s, pitch, blimit0[0], limit0[0], thresh0[0], blimit1[0], limit1[0],
                    thresh1[0]);
            }
        }

        public static void LpfHorizontal16(
            ArrayPtr<byte> s,
            int pitch,
            ReadOnlySpan<byte> blimit,
            ReadOnlySpan<byte> limit,
            ReadOnlySpan<byte> thresh)
        {
            if (Sse2.IsSupported)
            {
                LoopFilterSse2.LpfHorizontal16(s, pitch, blimit, limit, thresh);
            }
            else
            {
                LoopFilterScalar.LpfHorizontal16(s, pitch, blimit[0], limit[0], thresh[0]);
            }
        }

        public static void LpfHorizontal16Dual(
            ArrayPtr<byte> s,
            int pitch,
            ReadOnlySpan<byte> blimit,
            ReadOnlySpan<byte> limit,
            ReadOnlySpan<byte> thresh)
        {
            if (Sse2.IsSupported)
            {
                LoopFilterSse2.LpfHorizontal16Dual(s, pitch, blimit, limit, thresh);
            }
            else
            {
                LoopFilterScalar.LpfHorizontal16Dual(s, pitch, blimit[0], limit[0], thresh[0]);
            }
        }

        public static void LpfVertical4(
            ArrayPtr<byte> s,
            int pitch,
            ReadOnlySpan<byte> blimit,
            ReadOnlySpan<byte> limit,
            ReadOnlySpan<byte> thresh)
        {
            if (Sse2.IsSupported)
            {
                LoopFilterSse2.LpfVertical4(s, pitch, blimit, limit, thresh);
            }
            else
            {
                LoopFilterScalar.LpfVertical4(s, pitch, blimit[0], limit[0], thresh[0]);
            }
        }

        public static void LpfVertical4Dual(
            ArrayPtr<byte> s,
            int pitch,
            ReadOnlySpan<byte> blimit0,
            ReadOnlySpan<byte> limit0,
            ReadOnlySpan<byte> thresh0,
            ReadOnlySpan<byte> blimit1,
            ReadOnlySpan<byte> limit1,
            ReadOnlySpan<byte> thresh1)
        {
            if (Sse2.IsSupported)
            {
                LoopFilterSse2.LpfVertical4Dual(s, pitch, blimit0, limit0, thresh0, blimit1, limit1, thresh1);
            }
            else
            {
                LoopFilterScalar.LpfVertical4Dual(s, pitch, blimit0[0], limit0[0], thresh0[0], blimit1[0], limit1[0],
                    thresh1[0]);
            }
        }

        public static void LpfVertical8(
            ArrayPtr<byte> s,
            int pitch,
            ReadOnlySpan<byte> blimit,
            ReadOnlySpan<byte> limit,
            ReadOnlySpan<byte> thresh)
        {
            if (Sse2.IsSupported)
            {
                LoopFilterSse2.LpfVertical8(s, pitch, blimit, limit, thresh);
            }
            else
            {
                LoopFilterScalar.LpfVertical8(s, pitch, blimit[0], limit[0], thresh[0]);
            }
        }

        public static void LpfVertical8Dual(
            ArrayPtr<byte> s,
            int pitch,
            ReadOnlySpan<byte> blimit0,
            ReadOnlySpan<byte> limit0,
            ReadOnlySpan<byte> thresh0,
            ReadOnlySpan<byte> blimit1,
            ReadOnlySpan<byte> limit1,
            ReadOnlySpan<byte> thresh1)
        {
            if (Sse2.IsSupported)
            {
                LoopFilterSse2.LpfVertical8Dual(s, pitch, blimit0, limit0, thresh0, blimit1, limit1, thresh1);
            }
            else
            {
                LoopFilterScalar.LpfVertical8Dual(s, pitch, blimit0[0], limit0[0], thresh0[0], blimit1[0], limit1[0],
                    thresh1[0]);
            }
        }

        public static void LpfVertical16(
            ArrayPtr<byte> s,
            int pitch,
            ReadOnlySpan<byte> blimit,
            ReadOnlySpan<byte> limit,
            ReadOnlySpan<byte> thresh)
        {
            if (Sse2.IsSupported)
            {
                LoopFilterSse2.LpfVertical16(s, pitch, blimit, limit, thresh);
            }
            else
            {
                LoopFilterScalar.LpfVertical16(s, pitch, blimit[0], limit[0], thresh[0]);
            }
        }

        public static void LpfVertical16Dual(
            ArrayPtr<byte> s,
            int pitch,
            ReadOnlySpan<byte> blimit,
            ReadOnlySpan<byte> limit,
            ReadOnlySpan<byte> thresh)
        {
            if (Sse2.IsSupported)
            {
                LoopFilterSse2.LpfVertical16Dual(s, pitch, blimit, limit, thresh);
            }
            else
            {
                LoopFilterScalar.LpfVertical16Dual(s, pitch, blimit[0], limit[0], thresh[0]);
            }
        }
    }
}