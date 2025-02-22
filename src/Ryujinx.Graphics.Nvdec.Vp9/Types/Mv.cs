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
            0, 0, 1, 1, 2, 2, 2, 2, 3, 3, 3, 3, 3, 3, 3, 3, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 5, 5, 5,
            5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 6, 6, 6, 6, 6, 6,
            6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6,
            6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7,
            7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7,
            7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7,
            7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7,
            7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8,
            8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8,
            8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8,
            8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8,
            8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8,
            8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8,
            8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8,
            8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9,
            9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9,
            9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9,
            9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9,
            9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9,
            9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9,
            9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9,
            9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9,
            9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9,
            9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9,
            9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9,
            9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9,
            9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9,
            9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9,
            9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9,
            9, 9, 9, 9, 9, 9, 9, 9, 9, 10
        ];

        public bool UseHp()
        {
            const int KMvRefThresh = 64; // Threshold for use of high-precision 1/8 mv
            return Math.Abs(Row) < KMvRefThresh && Math.Abs(Col) < KMvRefThresh;
        }

        public static bool JointVertical(MvJointType type)
        {
            return type == MvJointType.Hzvnz || type == MvJointType.Hnzvnz;
        }

        public static bool JointHorizontal(MvJointType type)
        {
            return type == MvJointType.Hnzvz || type == MvJointType.Hnzvnz;
        }

        private static int ClassBase(MvClassType c)
        {
            return c != 0 ? Constants.Class0Size << ((int)c + 2) : 0;
        }

        private static MvClassType GetClass(int z, Ptr<int> offset)
        {
            MvClassType c = z >= Constants.Class0Size * 4096 ? MvClassType.Class10 : (MvClassType)LogInBase2[z >> 3];
            if (!offset.IsNull)
            {
                offset.Value = z - ClassBase(c);
            }

            return c;
        }

        private static void IncComponent(int v, ref Vp9BackwardUpdates counts, int comp, int incr, int usehp)
        {
            int o = 0;
            Debug.Assert(v != 0); /* Should not be zero */
            int s = v < 0 ? 1 : 0;
            counts.Sign[comp][s] += (uint)incr;
            int z = (s != 0 ? -v : v) - 1 /* Magnitude - 1 */;

            int c = (int)GetClass(z, new Ptr<int>(ref o));
            counts.Classes[comp][c] += (uint)incr;

            int d = o >> 3 /* Int mv data */;
            int f = (o >> 1) & 3 /* Fractional pel mv data */;
            int e = o & 1 /* High precision mv data */;

            if (c == (int)MvClassType.Class0)
            {
                counts.Class0[comp][d] += (uint)incr;
                counts.Class0Fp[comp][d][f] += (uint)incr;
                counts.Class0Hp[comp][e] += (uint)(usehp * incr);
            }
            else
            {
                int b = c + Constants.Class0Bits - 1; // Number of bits
                for (int i = 0; i < b; ++i)
                {
                    counts.Bits[comp][i][(d >> i) & 1] += (uint)incr;
                }

                counts.Fp[comp][f] += (uint)incr;
                counts.Hp[comp][e] += (uint)(usehp * incr);
            }
        }

        public MvJointType GetJoint()
        {
            if (Row == 0)
            {
                return Col == 0 ? MvJointType.Zero : MvJointType.Hnzvz;
            }

            return Col == 0 ? MvJointType.Hzvnz : MvJointType.Hnzvnz;
        }

        internal void Inc(Ptr<Vp9BackwardUpdates> counts)
        {
            if (!counts.IsNull)
            {
                MvJointType j = GetJoint();
                ++counts.Value.Joints[(int)j];

                if (JointVertical(j))
                {
                    IncComponent(Row, ref counts.Value, 0, 1, 1);
                }

                if (JointHorizontal(j))
                {
                    IncComponent(Col, ref counts.Value, 1, 1, 1);
                }
            }
        }

        public void Clamp(int minCol, int maxCol, int minRow, int maxRow)
        {
            Col = (short)Math.Clamp(Col, minCol, maxCol);
            Row = (short)Math.Clamp(Row, minRow, maxRow);
        }

        private const int Border = 16 << 3; // Allow 16 pels in 1/8th pel units

        public void ClampRef(ref MacroBlockD xd)
        {
            Clamp(
                xd.MbToLeftEdge - Border,
                xd.MbToRightEdge + Border,
                xd.MbToTopEdge - Border,
                xd.MbToBottomEdge + Border);
        }

        public void LowerPrecision(bool allowHp)
        {
            bool useHp = allowHp && UseHp();
            if (!useHp)
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

        public bool IsValid()
        {
            return Row is > Constants.MvLow and < Constants.MvUpp &&
                   Col is > Constants.MvLow and < Constants.MvUpp;
        }
    }
}