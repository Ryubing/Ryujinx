using Ryujinx.Common.Memory;
using Ryujinx.Graphics.Nvdec.Vp9.Common;
using Ryujinx.Graphics.Nvdec.Vp9.Types;
using Ryujinx.Graphics.Video;
using System;

namespace Ryujinx.Graphics.Nvdec.Vp9
{
    internal class EntropyMode
    {
        public const int BlockSizeGroups = 4;

        public const int TxSizeContexts = 2;

        public static readonly byte[][][] KfYModeProb =
        [
            [
                // above = dc
                [137, 30, 42, 148, 151, 207, 70, 52, 91], // left = dc
                [92, 45, 102, 136, 116, 180, 74, 90, 100], // left = v
                [73, 32, 19, 187, 222, 215, 46, 34, 100], // left = h
                [91, 30, 32, 116, 121, 186, 93, 86, 94], // left = d45
                [72, 35, 36, 149, 68, 206, 68, 63, 105], // left = d135
                [73, 31, 28, 138, 57, 124, 55, 122, 151], // left = d117
                [67, 23, 21, 140, 126, 197, 40, 37, 171], // left = d153
                [86, 27, 28, 128, 154, 212, 45, 43, 53], // left = d207
                [74, 32, 27, 107, 86, 160, 63, 134, 102], // left = d63
                [59, 67, 44, 140, 161, 202, 78, 67, 119] // left = tm
            ],
            [
                // above = v
                [63, 36, 126, 146, 123, 158, 60, 90, 96], // left = dc
                [43, 46, 168, 134, 107, 128, 69, 142, 92], // left = v
                [44, 29, 68, 159, 201, 177, 50, 57, 77], // left = h
                [58, 38, 76, 114, 97, 172, 78, 133, 92], // left = d45
                [46, 41, 76, 140, 63, 184, 69, 112, 57], // left = d135
                [38, 32, 85, 140, 46, 112, 54, 151, 133], // left = d117
                [39, 27, 61, 131, 110, 175, 44, 75, 136], // left = d153
                [52, 30, 74, 113, 130, 175, 51, 64, 58], // left = d207
                [47, 35, 80, 100, 74, 143, 64, 163, 74], // left = d63
                [36, 61, 116, 114, 128, 162, 80, 125, 82] // left = tm
            ],
            [
                // above = h
                [82, 26, 26, 171, 208, 204, 44, 32, 105], // left = dc
                [55, 44, 68, 166, 179, 192, 57, 57, 108], // left = v
                [42, 26, 11, 199, 241, 228, 23, 15, 85], // left = h
                [68, 42, 19, 131, 160, 199, 55, 52, 83], // left = d45
                [58, 50, 25, 139, 115, 232, 39, 52, 118], // left = d135
                [50, 35, 33, 153, 104, 162, 64, 59, 131], // left = d117
                [44, 24, 16, 150, 177, 202, 33, 19, 156], // left = d153
                [55, 27, 12, 153, 203, 218, 26, 27, 49], // left = d207
                [53, 49, 21, 110, 116, 168, 59, 80, 76], // left = d63
                [38, 72, 19, 168, 203, 212, 50, 50, 107] // left = tm
            ],
            [
                // above = d45
                [103, 26, 36, 129, 132, 201, 83, 80, 93], // left = dc
                [59, 38, 83, 112, 103, 162, 98, 136, 90], // left = v
                [62, 30, 23, 158, 200, 207, 59, 57, 50], // left = h
                [67, 30, 29, 84, 86, 191, 102, 91, 59], // left = d45
                [60, 32, 33, 112, 71, 220, 64, 89, 104], // left = d135
                [53, 26, 34, 130, 56, 149, 84, 120, 103], // left = d117
                [53, 21, 23, 133, 109, 210, 56, 77, 172], // left = d153
                [77, 19, 29, 112, 142, 228, 55, 66, 36], // left = d207
                [61, 29, 29, 93, 97, 165, 83, 175, 162], // left = d63
                [47, 47, 43, 114, 137, 181, 100, 99, 95] // left = tm
            ],
            [
                // above = d135
                [69, 23, 29, 128, 83, 199, 46, 44, 101], // left = dc
                [53, 40, 55, 139, 69, 183, 61, 80, 110], // left = v
                [40, 29, 19, 161, 180, 207, 43, 24, 91], // left = h
                [60, 34, 19, 105, 61, 198, 53, 64, 89], // left = d45
                [52, 31, 22, 158, 40, 209, 58, 62, 89], // left = d135
                [44, 31, 29, 147, 46, 158, 56, 102, 198], // left = d117
                [35, 19, 12, 135, 87, 209, 41, 45, 167], // left = d153
                [55, 25, 21, 118, 95, 215, 38, 39, 66], // left = d207
                [51, 38, 25, 113, 58, 164, 70, 93, 97], // left = d63
                [47, 54, 34, 146, 108, 203, 72, 103, 151] // left = tm
            ],
            [
                // above = d117
                [64, 19, 37, 156, 66, 138, 49, 95, 133], // left = dc
                [46, 27, 80, 150, 55, 124, 55, 121, 135], // left = v
                [36, 23, 27, 165, 149, 166, 54, 64, 118], // left = h
                [53, 21, 36, 131, 63, 163, 60, 109, 81], // left = d45
                [40, 26, 35, 154, 40, 185, 51, 97, 123], // left = d135
                [35, 19, 34, 179, 19, 97, 48, 129, 124], // left = d117
                [36, 20, 26, 136, 62, 164, 33, 77, 154], // left = d153
                [45, 18, 32, 130, 90, 157, 40, 79, 91], // left = d207
                [45, 26, 28, 129, 45, 129, 49, 147, 123], // left = d63
                [38, 44, 51, 136, 74, 162, 57, 97, 121] // left = tm
            ],
            [
                // above = d153
                [75, 17, 22, 136, 138, 185, 32, 34, 166], // left = dc
                [56, 39, 58, 133, 117, 173, 48, 53, 187], // left = v
                [35, 21, 12, 161, 212, 207, 20, 23, 145], // left = h
                [56, 29, 19, 117, 109, 181, 55, 68, 112], // left = d45
                [47, 29, 17, 153, 64, 220, 59, 51, 114], // left = d135
                [46, 16, 24, 136, 76, 147, 41, 64, 172], // left = d117
                [34, 17, 11, 108, 152, 187, 13, 15, 209], // left = d153
                [51, 24, 14, 115, 133, 209, 32, 26, 104], // left = d207
                [55, 30, 18, 122, 79, 179, 44, 88, 116], // left = d63
                [37, 49, 25, 129, 168, 164, 41, 54, 148] // left = tm
            ],
            [
                // above = d207
                [82, 22, 32, 127, 143, 213, 39, 41, 70], // left = dc
                [62, 44, 61, 123, 105, 189, 48, 57, 64], // left = v
                [47, 25, 17, 175, 222, 220, 24, 30, 86], // left = h
                [68, 36, 17, 106, 102, 206, 59, 74, 74], // left = d45
                [57, 39, 23, 151, 68, 216, 55, 63, 58], // left = d135
                [49, 30, 35, 141, 70, 168, 82, 40, 115], // left = d117
                [51, 25, 15, 136, 129, 202, 38, 35, 139], // left = d153
                [68, 26, 16, 111, 141, 215, 29, 28, 28], // left = d207
                [59, 39, 19, 114, 75, 180, 77, 104, 42], // left = d63
                [40, 61, 26, 126, 152, 206, 61, 59, 93] // left = tm
            ],
            [
                // above = d63
                [78, 23, 39, 111, 117, 170, 74, 124, 94], // left = dc
                [48, 34, 86, 101, 92, 146, 78, 179, 134], // left = v
                [47, 22, 24, 138, 187, 178, 68, 69, 59], // left = h
                [56, 25, 33, 105, 112, 187, 95, 177, 129], // left = d45
                [48, 31, 27, 114, 63, 183, 82, 116, 56], // left = d135
                [43, 28, 37, 121, 63, 123, 61, 192, 169], // left = d117
                [42, 17, 24, 109, 97, 177, 56, 76, 122], // left = d153
                [58, 18, 28, 105, 139, 182, 70, 92, 63], // left = d207
                [46, 23, 32, 74, 86, 150, 67, 183, 88], // left = d63
                [36, 38, 48, 92, 122, 165, 88, 137, 91] // left = tm
            ],
            [
                // above = tm
                [65, 70, 60, 155, 159, 199, 61, 60, 81], // left = dc
                [44, 78, 115, 132, 119, 173, 71, 112, 93], // left = v
                [39, 38, 21, 184, 227, 206, 42, 32, 64], // left = h
                [58, 47, 36, 124, 137, 193, 80, 82, 78], // left = d45
                [49, 50, 35, 144, 95, 205, 63, 78, 59], // left = d135
                [41, 53, 52, 148, 71, 142, 65, 128, 51], // left = d117
                [40, 36, 28, 143, 143, 202, 40, 55, 137], // left = d153
                [52, 34, 29, 129, 183, 227, 42, 35, 43], // left = d207
                [42, 44, 44, 104, 105, 164, 64, 130, 80], // left = d63
                [43, 81, 53, 140, 169, 204, 68, 84, 72] // left = tm
            ]
        ];

        public static readonly byte[][] KfUvModeProb =
        [
            [144, 11, 54, 157, 195, 130, 46, 58, 108], // y = dc
            [118, 15, 123, 148, 131, 101, 44, 93, 131], // y = v
            [113, 12, 23, 188, 226, 142, 26, 32, 125], // y = h
            [120, 11, 50, 123, 163, 135, 64, 77, 103], // y = d45
            [113, 9, 36, 155, 111, 157, 32, 44, 161], // y = d135
            [116, 9, 55, 176, 76, 96, 37, 61, 149], // y = d117
            [115, 9, 28, 141, 161, 167, 21, 25, 193], // y = d153
            [120, 12, 32, 145, 195, 142, 32, 38, 86], // y = d207
            [116, 12, 64, 120, 140, 125, 49, 115, 121], // y = d63
            [102, 19, 66, 162, 182, 122, 35, 59, 128] // y = tm
        ];

        private static readonly byte[] _defaultIfYProbs =
        [
            65, 32, 18, 144, 162, 194, 41, 51, 98, // block_size < 8x8
            132, 68, 18, 165, 217, 196, 45, 40, 78, // block_size < 16x16
            173, 80, 19, 176, 240, 193, 64, 35, 46, // block_size < 32x32
            221, 135, 38, 194, 248, 121, 96, 85, 29 // block_size >= 32x32
        ];

        private static readonly byte[] _defaultIfUvProbs =
        [
            120, 7, 76, 176, 208, 126, 28, 54, 103, // y = dc
            48, 12, 154, 155, 139, 90, 34, 117, 119, // y = v
            67, 6, 25, 204, 243, 158, 13, 21, 96, // y = h
            97, 5, 44, 131, 176, 139, 48, 68, 97, // y = d45
            83, 5, 42, 156, 111, 152, 26, 49, 152, // y = d135
            80, 5, 58, 178, 74, 83, 33, 62, 145, // y = d117
            86, 5, 32, 154, 192, 168, 14, 22, 163, // y = d153
            85, 5, 32, 156, 216, 148, 19, 29, 73, // y = d207
            77, 7, 64, 116, 132, 122, 37, 126, 120, // y = d63
            101, 21, 107, 181, 192, 103, 19, 67, 125 // y = tm
        ];

        private static readonly byte[] _defaultPartitionProbs =
        [
            // 8x8 . 4x4
            199, 122, 141, // a/l both not split
            147, 63, 159, // a split, l not split
            148, 133, 118, // l split, a not split
            121, 104, 114, // a/l both split
            // 16x16 . 8x8
            174, 73, 87, // a/l both not split
            92, 41, 83, // a split, l not split
            82, 99, 50, // l split, a not split
            53, 39, 39, // a/l both split
            // 32x32 . 16x16
            177, 58, 59, // a/l both not split
            68, 26, 63, // a split, l not split
            52, 79, 25, // l split, a not split
            17, 14, 12, // a/l both split
            // 64x64 . 32x32
            222, 34, 30, // a/l both not split
            72, 16, 44, // a split, l not split
            58, 32, 12, // l split, a not split
            10, 7, 6 // a/l both split
        ];

        private static readonly byte[] _defaultInterModeProbs =
        [
            2, 173, 34, // 0 = both zero mv
            7, 145, 85, // 1 = one zero mv + one a predicted mv
            7, 166, 63, // 2 = two predicted mvs
            7, 94, 66, // 3 = one predicted/zero and one new mv
            8, 64, 46, // 4 = two new mvs
            17, 81, 31, // 5 = one intra neighbour + x
            25, 29, 30 // 6 = two intra neighbours
        ];

        /* Array indices are identical to previously-existing INTRAMODECONTEXTNODES. */
        public static readonly sbyte[] IntraModeTree =
        [
            -(int)PredictionMode.DcPred, 2, /* 0 = DC_NODE */ -(int)PredictionMode.TmPred, 4, /* 1 = TM_NODE */
            -(int)PredictionMode.VPred, 6, /* 2 = V_NODE */ 8, 12, /* 3 = COM_NODE */ -(int)PredictionMode.HPred,
            10, /* 4 = H_NODE */ -(int)PredictionMode.D135Pred, -(int)PredictionMode.D117Pred, /* 5 = D135_NODE */
            -(int)PredictionMode.D45Pred, 14, /* 6 = D45_NODE */ -(int)PredictionMode.D63Pred,
            16, /* 7 = D63_NODE */ -(int)PredictionMode.D153Pred, -(int)PredictionMode.D207Pred /* 8 = D153_NODE */
        ];

        public static readonly sbyte[] InterModeTree =
        [
            -((int)PredictionMode.ZeroMv - (int)PredictionMode.NearestMv), 2,
            -((int)PredictionMode.NearestMv - (int)PredictionMode.NearestMv), 4,
            -((int)PredictionMode.NearMv - (int)PredictionMode.NearestMv),
            -((int)PredictionMode.NewMv - (int)PredictionMode.NearestMv)
        ];

        public static readonly sbyte[] PartitionTree =
        [
            -(sbyte)PartitionType.PartitionNone, 2, -(sbyte)PartitionType.PartitionHorz, 4,
            -(sbyte)PartitionType.PartitionVert, -(sbyte)PartitionType.PartitionSplit
        ];

        public static readonly sbyte[] SwitchableInterpTree =
        [
            -Constants.EightTap, 2, -Constants.EightTapSmooth, -Constants.EightTapSharp
        ];

        private static readonly byte[] _defaultIntraInterP = [9, 102, 187, 225];
        private static readonly byte[] _defaultCompInterP = [239, 183, 119, 96, 41];
        private static readonly byte[] _defaultCompRefP = [50, 126, 123, 221, 226];
        private static readonly byte[] _defaultSingleRefP = [33, 16, 77, 74, 142, 142, 172, 170, 238, 247];
        private static readonly byte[] _defaultTxProbs = [3, 136, 37, 5, 52, 13, 20, 152, 15, 101, 100, 66];

        static EntropyMode()
        {
            byte[][] kfPartitionProbs =
            [
                // 8x8 . 4x4
                [158, 97, 94], // a/l both not split
                [93, 24, 99], // a split, l not split
                [85, 119, 44], // l split, a not split
                [62, 59, 67], // a/l both split

                // 16x16 . 8x8
                [149, 53, 53], // a/l both not split
                [94, 20, 48], // a split, l not split
                [83, 53, 24], // l split, a not split
                [52, 18, 18], // a/l both split

                // 32x32 . 16x16
                [150, 40, 39], // a/l both not split
                [78, 12, 26], // a split, l not split
                [67, 33, 11], // l split, a not split
                [24, 7, 5], // a/l both split

                // 64x64 . 32x32
                [174, 35, 49], // a/l both not split
                [68, 11, 27], // a split, l not split
                [57, 15, 9], // l split, a not split
                [12, 3, 3] // a/l both split
            ];
        }

        private static readonly byte[] _defaultSkipProbs = [192, 128, 64];

        private static readonly byte[] _defaultSwitchableInterpProb = [235, 162, 36, 255, 34, 3, 149, 144];

        private static void InitModeProbs(ref Vp9EntropyProbs fc)
        {
            Entropy.CopyProbs(ref fc.UvModeProb, _defaultIfUvProbs);
            Entropy.CopyProbs(ref fc.YModeProb, _defaultIfYProbs);
            Entropy.CopyProbs(ref fc.SwitchableInterpProb, _defaultSwitchableInterpProb);
            Entropy.CopyProbs(ref fc.PartitionProb, _defaultPartitionProbs);
            Entropy.CopyProbs(ref fc.IntraInterProb, _defaultIntraInterP);
            Entropy.CopyProbs(ref fc.CompInterProb, _defaultCompInterP);
            Entropy.CopyProbs(ref fc.CompRefProb, _defaultCompRefP);
            Entropy.CopyProbs(ref fc.SingleRefProb, _defaultSingleRefP);
            Entropy.CopyProbs(ref fc.Tx32x32Prob, _defaultTxProbs.AsSpan().Slice(0, 6));
            Entropy.CopyProbs(ref fc.Tx16x16Prob, _defaultTxProbs.AsSpan().Slice(6, 4));
            Entropy.CopyProbs(ref fc.Tx8x8Prob, _defaultTxProbs.AsSpan().Slice(10, 2));
            Entropy.CopyProbs(ref fc.SkipProb, _defaultSkipProbs);
            Entropy.CopyProbs(ref fc.InterModeProb, _defaultInterModeProbs);
        }

        internal static void TxCountsToBranchCounts32X32(ReadOnlySpan<uint> txCount32X32P,
            ref Array3<Array2<uint>> ct32X32P)
        {
            ct32X32P[0][0] = txCount32X32P[(int)TxSize.Tx4X4];
            ct32X32P[0][1] = txCount32X32P[(int)TxSize.Tx8X8] + txCount32X32P[(int)TxSize.Tx16X16] +
                             txCount32X32P[(int)TxSize.Tx32X32];
            ct32X32P[1][0] = txCount32X32P[(int)TxSize.Tx8X8];
            ct32X32P[1][1] = txCount32X32P[(int)TxSize.Tx16X16] + txCount32X32P[(int)TxSize.Tx32X32];
            ct32X32P[2][0] = txCount32X32P[(int)TxSize.Tx16X16];
            ct32X32P[2][1] = txCount32X32P[(int)TxSize.Tx32X32];
        }

        internal static void TxCountsToBranchCounts16X16(ReadOnlySpan<uint> txCount16X16P,
            ref Array2<Array2<uint>> ct16X16P)
        {
            ct16X16P[0][0] = txCount16X16P[(int)TxSize.Tx4X4];
            ct16X16P[0][1] = txCount16X16P[(int)TxSize.Tx8X8] + txCount16X16P[(int)TxSize.Tx16X16];
            ct16X16P[1][0] = txCount16X16P[(int)TxSize.Tx8X8];
            ct16X16P[1][1] = txCount16X16P[(int)TxSize.Tx16X16];
        }

        internal static void TxCountsToBranchCounts8X8(ReadOnlySpan<uint> txCount8X8P,
            ref Array1<Array2<uint>> ct8X8P)
        {
            ct8X8P[0][0] = txCount8X8P[(int)TxSize.Tx4X4];
            ct8X8P[0][1] = txCount8X8P[(int)TxSize.Tx8X8];
        }

        public static unsafe void SetupPastIndependence(ref Vp9Common cm)
        {
            // Reset the segment feature data to the default stats:
            // Features disabled, 0, with delta coding (Default state).
            ref Types.LoopFilter lf = ref cm.Lf;

            cm.Seg.ClearAllSegFeatures();
            cm.Seg.AbsDelta = Segmentation.SegmentDeltadata;

            if (!cm.LastFrameSegMap.IsNull)
            {
                MemoryUtil.Fill(cm.LastFrameSegMap.ToPointer(), (byte)0, cm.MiRows * cm.MiCols);
            }

            if (!cm.CurrentFrameSegMap.IsNull)
            {
                MemoryUtil.Fill(cm.CurrentFrameSegMap.ToPointer(), (byte)0, cm.MiRows * cm.MiCols);
            }

            // Reset the mode ref deltas for loop filter
            lf.LastRefDeltas = new Array4<sbyte>();
            lf.LastModeDeltas = new Array2<sbyte>();
            lf.SetDefaultLfDeltas();

            // To force update of the sharpness
            lf.LastSharpnessLevel = -1;

            cm.DefaultCoefProbs();
            InitModeProbs(ref cm.Fc.Value);
            cm.InitMvProbs();

            if (cm.FrameType == FrameType.KeyFrame || cm.ErrorResilientMode != 0 || cm.ResetFrameContext == 3)
            {
                // Reset all frame contexts.
                for (int i = 0; i < Constants.FrameContexts; ++i)
                {
                    cm.FrameContexts[i] = cm.Fc.Value;
                }
            }
            else if (cm.ResetFrameContext == 2)
            {
                // Reset only the frame context specified in the frame header.
                cm.FrameContexts[(int)cm.FrameContextIdx] = cm.Fc.Value;
            }

            // prev_mip will only be allocated in encoder.
            if (cm.FrameIsIntraOnly() && !cm.PrevMip.IsNull)
            {
                cm.PrevMi.Value = new ModeInfo();
            }

            cm.RefFrameSignBias = new Array4<sbyte>();

            cm.FrameContextIdx = 0;
        }
    }
}