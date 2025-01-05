using Ryujinx.Common.Memory;
using Ryujinx.Graphics.Video;
using System;
using System.Diagnostics;

namespace Ryujinx.Graphics.Nvdec.Vp9.Types
{
    internal struct Mv
    {
        public short Row;
        public short Col;

        private static ReadOnlySpan<byte> LogInBase2 =>
        [
            0, 0, 1, 1, 2, 2, 2, 2, 3, 3, 3, 3, 3, 3, 3, 3, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4,
            4, 4, 4, 4, 4, 4, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5,
            5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6,
            6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6,
            6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 7, 7,
            7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7,
            7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7,
            7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7,
            7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7,
            7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 8, 8, 8, 8,
            8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8,
            8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8,
            8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8,
            8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8,
            8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8,
            8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8,
            8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8,
            8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8,
            8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8,
            8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 9, 9, 9, 9, 9, 9, 9, 9,
            9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9,
            9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9,
            9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9,
            9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9,
            9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9,
            9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9,
            9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9,
            9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9,
            9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9,
            9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9,
            9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9,
            9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9,
            9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9,
            9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9,
            9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9,
            9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9,
            9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9,
            9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9,
            9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9,
            9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 10,
        ];

        public readonly bool UseMvHp()
        {
            const int KMvRefThresh = 64; // Threshold for use of high-precision 1/8 mv
            return Math.Abs(Row) < KMvRefThresh && Math.Abs(Col) < KMvRefThresh;
        }

        public static bool MvJointVertical(MvJointType type)
        {
            return type == MvJointType.MvJointHzvnz || type == MvJointType.MvJointHnzvnz;
        }

        public static bool MvJointHorizontal(MvJointType type)
        {
            return type == MvJointType.MvJointHnzvz || type == MvJointType.MvJointHnzvnz;
        }

        private static int MvClassBase(MvClassType c)
        {
            return c != 0 ? Constants.Class0Size << ((int)c + 2) : 0;
        }

        private static MvClassType GetMvClass(int z, Ptr<int> offset)
        {
            MvClassType c = (z >= Constants.Class0Size * 4096) ? MvClassType.MvClass10 : (MvClassType)LogInBase2[z >> 3];
            if (!offset.IsNull)
            {
                offset.Value = z - MvClassBase(c);
            }

            return c;
        }

        private static void IncMvComponent(int v, ref Vp9BackwardUpdates counts, int comp, int incr, int usehp)
        {
            int s, z, c, o = 0, d, e, f;
            Debug.Assert(v != 0); /* Should not be zero */
            s = v < 0 ? 1 : 0;
            counts.Sign[comp][s] += (uint)incr;
            z = (s != 0 ? -v : v) - 1; /* Magnitude - 1 */

            c = (int)GetMvClass(z, new Ptr<int>(ref o));
            counts.Classes[comp][c] += (uint)incr;

            d = (o >> 3);     /* Int mv data */
            f = (o >> 1) & 3; /* Fractional pel mv data */
            e = (o & 1);      /* High precision mv data */

            if (c == (int)MvClassType.MvClass0)
            {
                counts.Class0[comp][d] += (uint)incr;
                counts.Class0Fp[comp][d][f] += (uint)incr;
                counts.Class0Hp[comp][e] += (uint)(usehp * incr);
            }
            else
            {
                int i;
                int b = c + Constants.Class0Bits - 1; // Number of bits
                for (i = 0; i < b; ++i)
                {
                    counts.Bits[comp][i][((d >> i) & 1)] += (uint)incr;
                }

                counts.Fp[comp][f] += (uint)incr;
                counts.Hp[comp][e] += (uint)(usehp * incr);
            }
        }

        private readonly MvJointType GetMvJoint()
        {
            if (Row == 0)
            {
                return Col == 0 ? MvJointType.MvJointZero : MvJointType.MvJointHnzvz;
            }

            return Col == 0 ? MvJointType.MvJointHzvnz : MvJointType.MvJointHnzvnz;
        }

        internal readonly void IncMv(Ptr<Vp9BackwardUpdates> counts)
        {
            if (!counts.IsNull)
            {
                MvJointType j = GetMvJoint();
                ++counts.Value.Joints[(int)j];

                if (MvJointVertical(j))
                {
                    IncMvComponent(Row, ref counts.Value, 0, 1, 1);
                }

                if (MvJointHorizontal(j))
                {
                    IncMvComponent(Col, ref counts.Value, 1, 1, 1);
                }
            }
        }

        public void ClampMv(int minCol, int maxCol, int minRow, int maxRow)
        {
            Col = (short)Math.Clamp(Col, minCol, maxCol);
            Row = (short)Math.Clamp(Row, minRow, maxRow);
        }

        private const int MvBorder = (16 << 3); // Allow 16 pels in 1/8th pel units

        public void ClampMvRef(ref MacroBlockD xd)
        {
            ClampMv(
                xd.MbToLeftEdge - MvBorder,
                xd.MbToRightEdge + MvBorder,
                xd.MbToTopEdge - MvBorder,
                xd.MbToBottomEdge + MvBorder);
        }

        public void LowerMvPrecision(bool allowHP)
        {
            bool useHP = allowHP && UseMvHp();
            if (!useHP)
            {
                if ((Row & 1) != 0)
                {
                    Row += (short)(Row > 0 ? -1 : 1);
                }

                if ((Col & 1) != 0)
                {
                    Col += (short)(Col > 0 ? -1 : 1);
                }
            }
        }
    }
}
