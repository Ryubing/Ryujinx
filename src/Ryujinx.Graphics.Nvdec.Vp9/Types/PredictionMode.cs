namespace Ryujinx.Graphics.Nvdec.Vp9.Types
{
    internal enum PredictionMode
    {
        DcPred, // Average of above and left pixels
        VPred, // Vertical
        HPred, // Horizontal
        D45Pred, // Directional 45  deg = round(arctan(1 / 1) * 180 / pi)
        D135Pred, // Directional 135 deg = 180 - 45
        D117Pred, // Directional 117 deg = 180 - 63
        D153Pred, // Directional 153 deg = 180 - 27
        D207Pred, // Directional 207 deg = 180 + 27
        D63Pred, // Directional 63  deg = round(arctan(2 / 1) * 180 / pi)
        TmPred, // True-motion
        NearestMv,
        NearMv,
        ZeroMv,
        NewMv,
        MbModeCount
    }
}