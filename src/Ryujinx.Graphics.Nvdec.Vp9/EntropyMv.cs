using Ryujinx.Common.Memory;
using Ryujinx.Graphics.Nvdec.Vp9.Types;
using Ryujinx.Graphics.Video;
using System.Diagnostics;

namespace Ryujinx.Graphics.Nvdec.Vp9
{
    internal static class EntropyMv
    {
        public const int UpdateProb = 252;

        /* Symbols for coding which components are zero jointly */
        public const int Joints = 4;


        public static readonly sbyte[] JointTree =
        [
            -(sbyte)MvJointType.Zero, 2, -(sbyte)MvJointType.Hnzvz, 4,
            -(sbyte)MvJointType.Hzvnz, -(sbyte)MvJointType.Hnzvnz
        ];

        public static readonly sbyte[] ClassTree =
        [
            -(sbyte)MvClassType.Class0, 2, -(sbyte)MvClassType.Class1, 4, 6, 8, -(sbyte)MvClassType.Class2,
            -(sbyte)MvClassType.Class3, 10, 12, -(sbyte)MvClassType.Class4, -(sbyte)MvClassType.Class5,
            -(sbyte)MvClassType.Class6, 14, 16, 18, -(sbyte)MvClassType.Class7, -(sbyte)MvClassType.Class8,
            -(sbyte)MvClassType.Class9, -(sbyte)MvClassType.Class10
        ];

        public static readonly sbyte[] Class0Tree = [-0, -1];

        public static readonly sbyte[] FpTree = [-0, 2, -1, 4, -2, -3];

        private static bool JointVertical(MvJointType type)
        {
            return type == MvJointType.Hzvnz || type == MvJointType.Hnzvnz;
        }

        private static bool JointHorizontal(MvJointType type)
        {
            return type == MvJointType.Hnzvz || type == MvJointType.Hnzvnz;
        }

        private static readonly byte[] _logInBase2 =
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

        private static int ClassBase(MvClassType c)
        {
            return c != 0 ? Class0Size << ((int)c + 2) : 0;
        }

        private static MvClassType GetClass(int z, Ptr<int> offset)
        {
            MvClassType c = z >= Class0Size * 4096
                ? MvClassType.Class10
                : (MvClassType)_logInBase2[z >> 3];
            if (!offset.IsNull)
            {
                offset.Value = z - ClassBase(c);
            }

            return c;
        }

        private static void IncComponent(int v, ref Vp9BackwardUpdates compCounts, int compIndex, int incr, int usehp)
        {
            int o = 0;
            Debug.Assert(v != 0); /* should not be zero */
            int s = v < 0 ? 1 : 0;
            compCounts.Sign[compIndex][s] += (uint)incr;
            int z = (s != 0 ? -v : v) - 1 /* magnitude - 1 */;

            int c = (int)GetClass(z, new Ptr<int>(ref o));
            compCounts.Classes[compIndex][c] += (uint)incr;

            int d = o >> 3 /* int mv data */;
            int f = (o >> 1) & 3 /* fractional pel mv data */;
            int e = o & 1 /* high precision mv data */;

            if (c == (int)MvClassType.Class0)
            {
                compCounts.Class0[compIndex][d] += (uint)incr;
                compCounts.Class0Fp[compIndex][d][f] += (uint)incr;
                compCounts.Class0Hp[compIndex][e] += (uint)(usehp * incr);
            }
            else
            {
                int b = c + Class0Bits - 1; // number of bits
                for (int i = 0; i < b; ++i)
                {
                    compCounts.Bits[compIndex][i][(d >> i) & 1] += (uint)incr;
                }

                compCounts.Fp[compIndex][f] += (uint)incr;
                compCounts.Hp[compIndex][e] += (uint)(usehp * incr);
            }
        }

        public static void Inc(ref Mv mv, Ptr<Vp9BackwardUpdates> counts)
        {
            if (!counts.IsNull)
            {
                MvJointType j = mv.GetJoint();
                ++counts.Value.Joints[(int)j];

                if (JointVertical(j))
                {
                    IncComponent(mv.Row, ref counts.Value, 0, 1, 1);
                }

                if (JointHorizontal(j))
                {
                    IncComponent(mv.Col, ref counts.Value, 1, 1, 1);
                }
            }
        }

        /* Symbols for coding magnitude class of nonzero components */
        public const int Classes = 11;

        public const int Class0Bits = 1; /* bits at integer precision for class 0 */
        public const int Class0Size = 1 << Class0Bits;
        public const int OffsetBits = Classes + Class0Bits - 2;
        public const int FpSize = 4;

        public const int MaxBits = Classes + Class0Bits + 2;
        public const int Max = (1 << MaxBits) - 1;
        public const int Vals = (Max << 1) + 1;

        public const int InUseBits = 14;
        public const int Upp = (1 << InUseBits) - 1;
        public const int Low = -(1 << InUseBits);
    }
}
