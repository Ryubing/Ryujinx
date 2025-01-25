namespace Ryujinx.Graphics.Nvdec.Vp9.Dsp
{
    internal static class Filter
    {
        public const int FilterBits = 7;

        public const int SubpelBits = 4;
        public const int SubpelMask = (1 << SubpelBits) - 1;
        public const int SubpelShifts = 1 << SubpelBits;
        public const int SubpelTaps = 8;
    }
}