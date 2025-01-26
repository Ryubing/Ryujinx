using Ryujinx.Common.Memory;
using Ryujinx.Graphics.Video;
using System;

namespace Ryujinx.Graphics.Nvdec.FFmpeg.H264
{
    static class SpsAndPpsReconstruction
    {
        public static Span<byte> Reconstruct(ref H264PictureInfo pictureInfo, byte[] workBuffer)
        {
            H264BitStreamWriter writer = new(workBuffer);

            // Sequence Parameter Set.
            writer.WriteU(1, 24);
            writer.WriteU(0, 1);
            writer.WriteU(3, 2);
            writer.WriteU(7, 5);
            writer.WriteU(100, 8); // Profile idc
            writer.WriteU(0, 8); // Reserved
            writer.WriteU(31, 8); // Level idc
            writer.WriteUe(0); // Seq parameter set id
            writer.WriteUe(pictureInfo.ChromaFormatIdc);

            if (pictureInfo.ChromaFormatIdc == 3)
            {
                writer.WriteBit(false); // Separate colour plane flag
            }

            writer.WriteUe(0); // Bit depth luma minus 8
            writer.WriteUe(0); // Bit depth chroma minus 8
            writer.WriteBit(pictureInfo.QpprimeYZeroTransformBypassFlag);
            writer.WriteBit(false); // Scaling matrix present flag

            writer.WriteUe(pictureInfo.Log2MaxFrameNumMinus4);
            writer.WriteUe(pictureInfo.PicOrderCntType);

            if (pictureInfo.PicOrderCntType == 0)
            {
                writer.WriteUe(pictureInfo.Log2MaxPicOrderCntLsbMinus4);
            }
            else if (pictureInfo.PicOrderCntType == 1)
            {
                writer.WriteBit(pictureInfo.DeltaPicOrderAlwaysZeroFlag);

                writer.WriteSe(0); // Offset for non-ref pic
                writer.WriteSe(0); // Offset for top to bottom field
                writer.WriteUe(0); // Num ref frames in pic order cnt cycle
            }

            writer.WriteUe(16); // Max num ref frames
            writer.WriteBit(false); // Gaps in frame num value allowed flag
            writer.WriteUe(pictureInfo.PicWidthInMbsMinus1);
            writer.WriteUe(pictureInfo.PicHeightInMapUnitsMinus1);
            writer.WriteBit(pictureInfo.FrameMbsOnlyFlag);

            if (!pictureInfo.FrameMbsOnlyFlag)
            {
                writer.WriteBit(pictureInfo.MbAdaptiveFrameFieldFlag);
            }

            writer.WriteBit(pictureInfo.Direct8x8InferenceFlag);
            writer.WriteBit(false); // Frame cropping flag
            writer.WriteBit(false); // VUI parameter present flag

            writer.End();

            // Picture Parameter Set.
            writer.WriteU(1, 24);
            writer.WriteU(0, 1);
            writer.WriteU(3, 2);
            writer.WriteU(8, 5);

            writer.WriteUe(0); // Pic parameter set id
            writer.WriteUe(0); // Seq parameter set id

            writer.WriteBit(pictureInfo.EntropyCodingModeFlag);
            writer.WriteBit(pictureInfo.PicOrderPresentFlag);
            writer.WriteUe(0); // Num slice groups minus 1
            writer.WriteUe(pictureInfo.NumRefIdxL0ActiveMinus1);
            writer.WriteUe(pictureInfo.NumRefIdxL1ActiveMinus1);
            writer.WriteBit(pictureInfo.WeightedPredFlag);
            writer.WriteU(pictureInfo.WeightedBipredIdc, 2);
            writer.WriteSe(pictureInfo.PicInitQpMinus26);
            writer.WriteSe(0); // Pic init qs minus 26
            writer.WriteSe(pictureInfo.ChromaQpIndexOffset);
            writer.WriteBit(pictureInfo.DeblockingFilterControlPresentFlag);
            writer.WriteBit(pictureInfo.ConstrainedIntraPredFlag);
            writer.WriteBit(pictureInfo.RedundantPicCntPresentFlag);
            writer.WriteBit(pictureInfo.Transform8x8ModeFlag);

            writer.WriteBit(pictureInfo.ScalingMatrixPresent);

            if (pictureInfo.ScalingMatrixPresent)
            {
                for (int index = 0; index < 6; index++)
                {
                    writer.WriteBit(true);

                    WriteScalingList(ref writer, pictureInfo.ScalingLists4x4[index]);
                }

                if (pictureInfo.Transform8x8ModeFlag)
                {
                    for (int index = 0; index < 2; index++)
                    {
                        writer.WriteBit(true);

                        WriteScalingList(ref writer, pictureInfo.ScalingLists8x8[index]);
                    }
                }
            }

            writer.WriteSe(pictureInfo.SecondChromaQpIndexOffset);

            writer.End();

            return writer.AsSpan();
        }

        // ZigZag LUTs from libavcodec.
        private static ReadOnlySpan<byte> ZigZagDirect =>
        [
            0,   1,  8, 16,  9,  2,  3, 10,
            17, 24, 32, 25, 18, 11,  4,  5,
            12, 19, 26, 33, 40, 48, 41, 34,
            27, 20, 13,  6,  7, 14, 21, 28,
            35, 42, 49, 56, 57, 50, 43, 36,
            29, 22, 15, 23, 30, 37, 44, 51,
            58, 59, 52, 45, 38, 31, 39, 46,
            53, 60, 61, 54, 47, 55, 62, 63
        ];

        private static ReadOnlySpan<byte> ZigZagScan =>
        [
            0 + 0 * 4, 1 + 0 * 4, 0 + 1 * 4, 0 + 2 * 4,
            1 + 1 * 4, 2 + 0 * 4, 3 + 0 * 4, 2 + 1 * 4,
            1 + 2 * 4, 0 + 3 * 4, 1 + 3 * 4, 2 + 2 * 4,
            3 + 1 * 4, 3 + 2 * 4, 2 + 3 * 4, 3 + 3 * 4
        ];

        private static void WriteScalingList(ref H264BitStreamWriter writer, IArray<byte> list)
        {
            ReadOnlySpan<byte> scan = list.Length == 16 ? ZigZagScan : ZigZagDirect;

            int lastScale = 8;

            for (int index = 0; index < list.Length; index++)
            {
                byte value = list[scan[index]];

                int deltaScale = value - lastScale;

                writer.WriteSe(deltaScale);

                lastScale = value;
            }
        }
    }
}
