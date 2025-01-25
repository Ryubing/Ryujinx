namespace Ryujinx.Graphics.Nvdec.Vp9.Types
{
    internal enum MotionVectorContext
    {
        BothZero = 0,
        ZeroPlusPredicted = 1,
        BothPredicted = 2,
        NewPlusNonIntra = 3,
        BothNew = 4,
        IntraPlusNonIntra = 5,
        BothIntra = 6,
        InvalidCase = 9,
    }
}