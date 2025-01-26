namespace ARMeilleure.Common
{
    public static class AddressTablePresets
    {
        private static readonly AddressTableLevel[] _levels64Bit =
        [
            new(31, 17),
                new(23,  8),
                new(15,  8),
                new( 7,  8),
                new( 2,  5)
        ];

        private static readonly AddressTableLevel[] _levels32Bit =
        [
            new(31, 17),
                new(23,  8),
                new(15,  8),
                new( 7,  8),
                new( 1,  6)
        ];

        private static readonly AddressTableLevel[] _levels64BitSparseTiny =
        [
            new( 11, 28),
                new( 2, 9)
        ];

        private static readonly AddressTableLevel[] _levels32BitSparseTiny =
        [
            new( 10, 22),
                new( 1, 9)
        ];

        private static readonly AddressTableLevel[] _levels64BitSparseGiant =
        [
            new( 38, 1),
                new( 2, 36)
        ];

        private static readonly AddressTableLevel[] _levels32BitSparseGiant =
        [
            new( 31, 1),
                new( 1, 30)
        ];

        //high power will run worse on DDR3 systems and some DDR4 systems due to the higher ram utilization
        //low power will never run worse than non-sparse, but for most systems it won't be necessary
        //high power is always used, but I've left low power in here for future reference
        public static AddressTableLevel[] GetArmPreset(bool for64Bits, bool sparse, bool lowPower = false)
        {
            if (sparse)
            {
                if (lowPower)
                {
                    return for64Bits ? _levels64BitSparseTiny : _levels32BitSparseTiny;
                }
                else
                {
                    return for64Bits ? _levels64BitSparseGiant : _levels32BitSparseGiant;
                }
            }
            else
            {
                return for64Bits ? _levels64Bit : _levels32Bit;
            }
        }
    }
}
