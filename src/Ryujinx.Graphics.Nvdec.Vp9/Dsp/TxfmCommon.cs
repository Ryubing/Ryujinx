namespace Ryujinx.Graphics.Nvdec.Vp9.Dsp
{
    internal static class TxfmCommon
    {
        // Constants used by all idct/dct functions
        public const int DctConstBits = 14;
        public const int DctConstRounding = 1 << (DctConstBits - 1);

        public const int UnitQuantShift = 2;
        public const int UnitQuantFactor = 1 << UnitQuantShift;

        // Constants:
        //  for (int i = 1; i < 32; ++i)
        //    Console.WriteLine("public const short CosPi{0}_64 = {1};", i, MathF.Round(16384 * MathF.Cos(i * MathF.PI / 64)));
        // Note: sin(k * Pi / 64) = cos((32 - k) * Pi / 64)
        public const short CosPi164 = 16364;
        public const short CosPi264 = 16305;
        public const short CosPi364 = 16207;
        public const short CosPi464 = 16069;
        public const short CosPi564 = 15893;
        public const short CosPi664 = 15679;
        public const short CosPi764 = 15426;
        public const short CosPi864 = 15137;
        public const short CosPi964 = 14811;
        public const short CosPi1064 = 14449;
        public const short CosPi1164 = 14053;
        public const short CosPi1264 = 13623;
        public const short CosPi1364 = 13160;
        public const short CosPi1464 = 12665;
        public const short CosPi1564 = 12140;
        public const short CosPi1664 = 11585;
        public const short CosPi1764 = 11003;
        public const short CosPi1864 = 10394;
        public const short CosPi1964 = 9760;
        public const short CosPi2064 = 9102;
        public const short CosPi2164 = 8423;
        public const short CosPi2264 = 7723;
        public const short CosPi2364 = 7005;
        public const short CosPi2464 = 6270;
        public const short CosPi2564 = 5520;
        public const short CosPi2664 = 4756;
        public const short CosPi2764 = 3981;
        public const short CosPi2864 = 3196;
        public const short CosPi2964 = 2404;
        public const short CosPi3064 = 1606;
        public const short CosPi3164 = 804;

        //  16384 * sqrt(2) * sin(kPi / 9) * 2 / 3
        public const short SinPi19 = 5283;
        public const short SinPi29 = 9929;
        public const short SinPi39 = 13377;
        public const short SinPi49 = 15212;
    }
}