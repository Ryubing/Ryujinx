using Ryujinx.Common.Memory;
using System.Runtime.CompilerServices;
using static Ryujinx.Graphics.Nvdec.Vp9.Dsp.Convolve;
using static Ryujinx.Graphics.Nvdec.Vp9.Dsp.Filter;

namespace Ryujinx.Graphics.Nvdec.Vp9.Types
{
    internal struct ScaleFactors
    {
        private const int RefScaleShift = 14;
        private const int RefNoScale = 1 << RefScaleShift;
        private const int RefInvalidScale = -1;

        private unsafe delegate void ConvolveFn(
            byte* src,
            int srcStride,
            byte* dst,
            int dstStride,
            Array8<short>[] filter,
            int x0Q4,
            int xStepQ4,
            int y0Q4,
            int yStepQ4,
            int w,
            int h);

        private unsafe delegate void HighbdConvolveFn(
            ushort* src,
            int srcStride,
            ushort* dst,
            int dstStride,
            Array8<short>[] filter,
            int x0Q4,
            int xStepQ4,
            int y0Q4,
            int yStepQ4,
            int w,
            int h,
            int bd);

        private static readonly unsafe ConvolveFn[][][] _predictX16Y16 =
        [
            [
                [ConvolveCopy, ConvolveAvg],
                [Convolve8Vert, Convolve8AvgVert]
            ],
            [
                [Convolve8Horiz, Convolve8AvgHoriz],
                [Convolve8, Convolve8Avg]
            ]
        ];

        private static readonly unsafe ConvolveFn[][][] _predictX16 =
        [
            [
                [ScaledVert, ScaledAvgVert], [ScaledVert, ScaledAvgVert]
            ],
            [[Scaled2D, ScaledAvg2D], [Scaled2D, ScaledAvg2D]]
        ];

        private static readonly unsafe ConvolveFn[][][] _predictY16 =
        [
            [[ScaledHoriz, ScaledAvgHoriz], [Scaled2D, ScaledAvg2D]],
            [[ScaledHoriz, ScaledAvgHoriz], [Scaled2D, ScaledAvg2D]]
        ];

        private static readonly unsafe ConvolveFn[][][] _predict =
        [
            [[Scaled2D, ScaledAvg2D], [Scaled2D, ScaledAvg2D]],
            [[Scaled2D, ScaledAvg2D], [Scaled2D, ScaledAvg2D]]
        ];

        private static readonly unsafe HighbdConvolveFn[][][] _highbdPredictX16Y16 =
        [
            [
                [HighbdConvolveCopy, HighbdConvolveAvg],
                [HighbdConvolve8Vert, HighbdConvolve8AvgVert]
            ],
            [
                [HighbdConvolve8Horiz, HighbdConvolve8AvgHoriz],
                [HighbdConvolve8, HighbdConvolve8Avg]
            ]
        ];

        private static readonly unsafe HighbdConvolveFn[][][] _highbdPredictX16 =
        [
            [
                [HighbdConvolve8Vert, HighbdConvolve8AvgVert],
                [HighbdConvolve8Vert, HighbdConvolve8AvgVert]
            ],
            [
                [HighbdConvolve8, HighbdConvolve8Avg],
                [HighbdConvolve8, HighbdConvolve8Avg]
            ]
        ];

        private static readonly unsafe HighbdConvolveFn[][][] _highbdPredictY16 =
        [
            [
                [HighbdConvolve8Horiz, HighbdConvolve8AvgHoriz],
                [HighbdConvolve8, HighbdConvolve8Avg]
            ],
            [
                [HighbdConvolve8Horiz, HighbdConvolve8AvgHoriz],
                [HighbdConvolve8, HighbdConvolve8Avg]
            ]
        ];

        private static readonly unsafe HighbdConvolveFn[][][] _highbdPredict =
        [
            [
                [HighbdConvolve8, HighbdConvolve8Avg],
                [HighbdConvolve8, HighbdConvolve8Avg]
            ],
            [
                [HighbdConvolve8, HighbdConvolve8Avg],
                [HighbdConvolve8, HighbdConvolve8Avg]
            ]
        ];

        public int XScaleFp; // Horizontal fixed point scale factor
        public int YScaleFp; // Vertical fixed point scale factor
        public int XStepQ4;
        public int YStepQ4;

        public int ScaleValueX(int val)
        {
            return IsScaled() ? ScaledX(val) : val;
        }

        public int ScaleValueY(int val)
        {
            return IsScaled() ? ScaledY(val) : val;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void InterPredict(
            int horiz,
            int vert,
            int avg,
            byte* src,
            int srcStride,
            byte* dst,
            int dstStride,
            int subpelX,
            int subpelY,
            int w,
            int h,
            Array8<short>[] kernel,
            int xs,
            int ys)
        {
            if (XStepQ4 == 16)
            {
                if (YStepQ4 == 16)
                {
                    // No scaling in either direction.
                    _predictX16Y16[horiz][vert][avg](src, srcStride, dst, dstStride, kernel, subpelX, xs, subpelY, ys, w,
                        h);
                }
                else
                {
                    // No scaling in x direction. Must always scale in the y direction.
                    _predictX16[horiz][vert][avg](src, srcStride, dst, dstStride, kernel, subpelX, xs, subpelY, ys, w,
                        h);
                }
            }
            else
            {
                if (YStepQ4 == 16)
                {
                    // No scaling in the y direction. Must always scale in the x direction.
                    _predictY16[horiz][vert][avg](src, srcStride, dst, dstStride, kernel, subpelX, xs, subpelY, ys, w,
                        h);
                }
                else
                {
                    // Must always scale in both directions.
                    _predict[horiz][vert][avg](src, srcStride, dst, dstStride, kernel, subpelX, xs, subpelY, ys, w, h);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void HighbdInterPredict(
            int horiz,
            int vert,
            int avg,
            ushort* src,
            int srcStride,
            ushort* dst,
            int dstStride,
            int subpelX,
            int subpelY,
            int w,
            int h,
            Array8<short>[] kernel,
            int xs,
            int ys,
            int bd)
        {
            if (XStepQ4 == 16)
            {
                if (YStepQ4 == 16)
                {
                    // No scaling in either direction.
                    _highbdPredictX16Y16[horiz][vert][avg](src, srcStride, dst, dstStride, kernel, subpelX, xs, subpelY,
                        ys, w, h, bd);
                }
                else
                {
                    // No scaling in x direction. Must always scale in the y direction.
                    _highbdPredictX16[horiz][vert][avg](src, srcStride, dst, dstStride, kernel, subpelX, xs, subpelY, ys,
                        w, h, bd);
                }
            }
            else
            {
                if (YStepQ4 == 16)
                {
                    // No scaling in the y direction. Must always scale in the x direction.
                    _highbdPredictY16[horiz][vert][avg](src, srcStride, dst, dstStride, kernel, subpelX, xs, subpelY, ys,
                        w, h, bd);
                }
                else
                {
                    // Must always scale in both directions.
                    _highbdPredict[horiz][vert][avg](src, srcStride, dst, dstStride, kernel, subpelX, xs, subpelY, ys, w,
                        h, bd);
                }
            }
        }

        private int ScaledX(int val)
        {
            return (int)(((long)val * XScaleFp) >> RefScaleShift);
        }

        private int ScaledY(int val)
        {
            return (int)(((long)val * YScaleFp) >> RefScaleShift);
        }

        private static int GetFixedPointScaleFactor(int otherSize, int thisSize)
        {
            // Calculate scaling factor once for each reference frame
            // and use fixed point scaling factors in decoding and encoding routines.
            // Hardware implementations can calculate scale factor in device driver
            // and use multiplication and shifting on hardware instead of division.
            return (otherSize << RefScaleShift) / thisSize;
        }

        public Mv32 ScaleMv(ref Mv mv, int x, int y)
        {
            int xOffQ4 = ScaledX(x << SubpelBits) & SubpelMask;
            int yOffQ4 = ScaledY(y << SubpelBits) & SubpelMask;
            Mv32 res = new() { Row = ScaledY(mv.Row) + yOffQ4, Col = ScaledX(mv.Col) + xOffQ4 };
            return res;
        }

        public bool IsValidScale()
        {
            return XScaleFp != RefInvalidScale && YScaleFp != RefInvalidScale;
        }

        public bool IsScaled()
        {
            return IsValidScale() && (XScaleFp != RefNoScale || YScaleFp != RefNoScale);
        }

        public static bool ValidRefFrameSize(int refWidth, int refHeight, int thisWidth, int thisHeight)
        {
            return 2 * thisWidth >= refWidth &&
                   2 * thisHeight >= refHeight &&
                   thisWidth <= 16 * refWidth &&
                   thisHeight <= 16 * refHeight;
        }

        public void SetupScaleFactorsForFrame(int otherW, int otherH, int thisW, int thisH)
        {
            if (!ValidRefFrameSize(otherW, otherH, thisW, thisH))
            {
                XScaleFp = RefInvalidScale;
                YScaleFp = RefInvalidScale;
                return;
            }

            XScaleFp = GetFixedPointScaleFactor(otherW, thisW);
            YScaleFp = GetFixedPointScaleFactor(otherH, thisH);
            XStepQ4 = ScaledX(16);
            YStepQ4 = ScaledY(16);
        }
    }
}