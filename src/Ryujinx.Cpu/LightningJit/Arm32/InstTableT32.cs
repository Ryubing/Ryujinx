using Ryujinx.Cpu.LightningJit.Table;
using System.Collections.Generic;

namespace Ryujinx.Cpu.LightningJit.Arm32
{
    static class InstTableT32<T> where T : IInstEmit
    {
        private static readonly InstTableLevel<InstInfoForTable> _table;

        static InstTableT32()
        {
            InstEncoding[] rnRdsConstraints =
            [
                new(0x000D0000, 0x000F0000),
                new(0x00100F00, 0x00100F00)
            ];

            InstEncoding[] rnRnConstraints =
            [
                new(0x000D0000, 0x000F0000),
                new(0x000F0000, 0x000F0000)
            ];

            InstEncoding[] rdsConstraints =
            [
                new(0x00100F00, 0x00100F00)
            ];

            InstEncoding[] vdVmConstraints =
            [
                new(0x00001000, 0x00001000),
                new(0x00000001, 0x00000001)
            ];

            InstEncoding[] condCondCondConstraints =
            [
                new(0x03800000, 0x03C00000),
                new(0x03C00000, 0x03C00000),
                new(0x03800000, 0x03800000)
            ];

            InstEncoding[] rnConstraints =
            [
                new(0x000F0000, 0x000F0000)
            ];

            InstEncoding[] hConstraints =
            [
                new(0x00000001, 0x00000001)
            ];

            InstEncoding[] imodmConstraints =
            [
                new(0x00000000, 0x00000700)
            ];

            InstEncoding[] optionConstraints =
            [
                new(0x00000000, 0x0000000F)
            ];

            InstEncoding[] puwPwPuwPuwConstraints =
            [
                new(0x00000000, 0x01A00000),
                new(0x01000000, 0x01200000),
                new(0x00200000, 0x01A00000),
                new(0x01A00000, 0x01A00000)
            ];

            InstEncoding[] rnPuwConstraints =
            [
                new(0x000F0000, 0x000F0000),
                new(0x00000000, 0x01A00000)
            ];

            InstEncoding[] puwConstraints =
            [
                new(0x00000000, 0x01A00000)
            ];

            InstEncoding[] rnRtConstraints =
            [
                new(0x000F0000, 0x000F0000),
                new(0x0000F000, 0x0000F000)
            ];

            InstEncoding[] rnRtpuwPuwPwConstraints =
            [
                new(0x000F0000, 0x000F0000),
                new(0x0000F400, 0x0000F700),
                new(0x00000600, 0x00000700),
                new(0x00000000, 0x00000500)
            ];

            InstEncoding[] rtConstraints =
            [
                new(0x0000F000, 0x0000F000)
            ];

            InstEncoding[] rnPwConstraints =
            [
                new(0x000F0000, 0x000F0000),
                new(0x00000000, 0x01200000)
            ];

            InstEncoding[] pwConstraints =
            [
                new(0x00000000, 0x01200000)
            ];

            InstEncoding[] rnPuwPwConstraints =
            [
                new(0x000F0000, 0x000F0000),
                new(0x00000600, 0x00000700),
                new(0x00000000, 0x00000500)
            ];

            InstEncoding[] raConstraints =
            [
                new(0x0000F000, 0x0000F000)
            ];

            InstEncoding[] sTConstraints =
            [
                new(0x00100000, 0x00100000),
                new(0x00000010, 0x00000010)
            ];

            InstEncoding[] vdVnVmConstraints =
            [
                new(0x00001000, 0x00001000),
                new(0x00010000, 0x00010000),
                new(0x00000001, 0x00000001)
            ];

            InstEncoding[] shimm2imm3Constraints =
            [
                new(0x00200000, 0x002070C0)
            ];

            InstEncoding[] rnimm8Constraints =
            [
                new(0x000E0000, 0x000F00FF)
            ];

            InstEncoding[] sizeQvdQvnQvmConstraints =
            [
                new(0x00300000, 0x00300000),
                new(0x00001040, 0x00001040),
                new(0x00010040, 0x00010040),
                new(0x00000041, 0x00000041)
            ];

            InstEncoding[] sizeVdConstraints =
            [
                new(0x00300000, 0x00300000),
                new(0x00001000, 0x00001000)
            ];

            InstEncoding[] qvdQvnQvmConstraints =
            [
                new(0x00001040, 0x00001040),
                new(0x00010040, 0x00010040),
                new(0x00000041, 0x00000041)
            ];

            InstEncoding[] sizeQvdQvmConstraints =
            [
                new(0x000C0000, 0x000C0000),
                new(0x00001040, 0x00001040),
                new(0x00000041, 0x00000041)
            ];

            InstEncoding[] sizeVnVmConstraints =
            [
                new(0x00300000, 0x00300000),
                new(0x00010000, 0x00010000),
                new(0x00000001, 0x00000001)
            ];

            InstEncoding[] sizeVdOpvnConstraints =
            [
                new(0x00300000, 0x00300000),
                new(0x00001000, 0x00001000),
                new(0x00010100, 0x00010100)
            ];

            InstEncoding[] cmodeCmodeQvdConstraints =
            [
                new(0x00000000, 0x00000100),
                new(0x00000C00, 0x00000C00),
                new(0x00001040, 0x00001040)
            ];

            InstEncoding[] qvdQvnQvmOpConstraints =
            [
                new(0x00001040, 0x00001040),
                new(0x00010040, 0x00010040),
                new(0x00000041, 0x00000041),
                new(0x00000000, 0x00300000)
            ];

            InstEncoding[] qvdQvnQvmSizeConstraints =
            [
                new(0x00001040, 0x00001040),
                new(0x00010040, 0x00010040),
                new(0x00000041, 0x00000041),
                new(0x00300000, 0x00300000)
            ];

            InstEncoding[] qvdQvnConstraints =
            [
                new(0x00001040, 0x00001040),
                new(0x00010040, 0x00010040)
            ];

            InstEncoding[] qvdQvmConstraints =
            [
                new(0x00001040, 0x00001040),
                new(0x00000041, 0x00000041)
            ];

            InstEncoding[] sizeConstraints =
            [
                new(0x00000000, 0x00000300)
            ];

            InstEncoding[] vmConstraints =
            [
                new(0x00000001, 0x00000001)
            ];

            InstEncoding[] opvdOpvmConstraints =
            [
                new(0x00001100, 0x00001100),
                new(0x00000001, 0x00000101)
            ];

            InstEncoding[] imm6Opimm6Imm6QvdQvmConstraints =
            [
                new(0x00000000, 0x00380000),
                new(0x00200000, 0x00300200),
                new(0x00000000, 0x00200000),
                new(0x00001040, 0x00001040),
                new(0x00000041, 0x00000041)
            ];

            InstEncoding[] qvdEbConstraints =
            [
                new(0x00210000, 0x00210000),
                new(0x00400020, 0x00400020)
            ];

            InstEncoding[] imm4QvdConstraints =
            [
                new(0x00000000, 0x00070000),
                new(0x00001040, 0x00001040)
            ];

            InstEncoding[] qvdQvnQvmQimm4Constraints =
            [
                new(0x00001040, 0x00001040),
                new(0x00010040, 0x00010040),
                new(0x00000041, 0x00000041),
                new(0x00000800, 0x00000840)
            ];

            InstEncoding[] qvdConstraints =
            [
                new(0x00001040, 0x00001040)
            ];

            InstEncoding[] vdVnConstraints =
            [
                new(0x00001000, 0x00001000),
                new(0x00010000, 0x00010000)
            ];

            InstEncoding[] sizeConstraints2 =
            [
                new(0x00000C00, 0x00000C00)
            ];

            InstEncoding[] sizeIndexAlignIndexAlignConstraints =
            [
                new(0x00000C00, 0x00000C00),
                new(0x00000010, 0x00000030),
                new(0x00000020, 0x00000030)
            ];

            InstEncoding[] sizeSizeaConstraints =
            [
                new(0x000000C0, 0x000000C0),
                new(0x00000010, 0x000000D0)
            ];

            InstEncoding[] alignConstraints =
            [
                new(0x00000020, 0x00000020)
            ];

            InstEncoding[] alignConstraints2 =
            [
                new(0x00000030, 0x00000030)
            ];

            InstEncoding[] sizeConstraints3 =
            [
                new(0x000000C0, 0x000000C0)
            ];

            InstEncoding[] alignSizeConstraints =
            [
                new(0x00000030, 0x00000030),
                new(0x000000C0, 0x000000C0)
            ];

            InstEncoding[] sizeAConstraints =
            [
                new(0x000000C0, 0x000000C0),
                new(0x00000010, 0x00000010)
            ];

            InstEncoding[] sizeAlignConstraints =
            [
                new(0x000000C0, 0x000000C0),
                new(0x00000020, 0x00000020)
            ];

            InstEncoding[] sizeIndexAlignConstraints =
            [
                new(0x00000C00, 0x00000C00),
                new(0x00000030, 0x00000030)
            ];

            InstEncoding[] sizeaConstraints =
            [
                new(0x000000C0, 0x000000D0)
            ];

            InstEncoding[] sizeSizeVdConstraints =
            [
                new(0x00300000, 0x00300000),
                new(0x00000000, 0x00300000),
                new(0x00001000, 0x00001000)
            ];

            InstEncoding[] sizeQvdQvnConstraints =
            [
                new(0x00300000, 0x00300000),
                new(0x10001000, 0x10001000),
                new(0x10010000, 0x10010000)
            ];

            InstEncoding[] imm3hImm3hImm3hImm3hImm3hVdConstraints =
            [
                new(0x00000000, 0x00380000),
                new(0x00180000, 0x00380000),
                new(0x00280000, 0x00380000),
                new(0x00300000, 0x00380000),
                new(0x00380000, 0x00380000),
                new(0x00001000, 0x00001000)
            ];

            InstEncoding[] sizeVmConstraints =
            [
                new(0x000C0000, 0x000C0000),
                new(0x00000001, 0x00000001)
            ];

            InstEncoding[] opc1opc2Constraints =
            [
                new(0x00000040, 0x00400060)
            ];

            InstEncoding[] uopc1opc2Uopc1opc2Constraints =
            [
                new(0x00800000, 0x00C00060),
                new(0x00000040, 0x00400060)
            ];

            InstEncoding[] sizeOpuOpsizeVdConstraints =
            [
                new(0x00300000, 0x00300000),
                new(0x10000200, 0x10000200),
                new(0x00100200, 0x00300200),
                new(0x00001000, 0x00001000)
            ];

            InstEncoding[] sizeOpsizeOpsizeQvdQvnQvmConstraints =
            [
                new(0x00300000, 0x00300000),
                new(0x10100000, 0x10300000),
                new(0x10200000, 0x10300000),
                new(0x00001040, 0x00001040),
                new(0x00010040, 0x00010040),
                new(0x00000041, 0x00000041)
            ];

            InstEncoding[] cmodeQvdConstraints =
            [
                new(0x00000E00, 0x00000E00),
                new(0x00001040, 0x00001040)
            ];

            InstEncoding[] qConstraints =
            [
                new(0x00000040, 0x00000040)
            ];

            InstEncoding[] sizeQConstraints =
            [
                new(0x00300000, 0x00300000),
                new(0x00000040, 0x00000040)
            ];

            InstEncoding[] sizeConstraints4 =
            [
                new(0x00300000, 0x00300000)
            ];

            InstEncoding[] qvdQvnQvmSizeSizeConstraints =
            [
                new(0x00001040, 0x00001040),
                new(0x00010040, 0x00010040),
                new(0x00000041, 0x00000041),
                new(0x00000000, 0x00300000),
                new(0x00300000, 0x00300000)
            ];

            InstEncoding[] sizeSizeQvdQvnConstraints =
            [
                new(0x00300000, 0x00300000),
                new(0x00000000, 0x00300000),
                new(0x10001000, 0x10001000),
                new(0x10010000, 0x10010000)
            ];

            InstEncoding[] opSizeVmConstraints =
            [
                new(0x00000000, 0x000000C0),
                new(0x000C0000, 0x000C0000),
                new(0x00000001, 0x00000001)
            ];

            InstEncoding[] qvdQvmQvnConstraints =
            [
                new(0x00001040, 0x00001040),
                new(0x00000041, 0x00000041),
                new(0x00010040, 0x00010040)
            ];

            InstEncoding[] imm6UopVmConstraints =
            [
                new(0x00000000, 0x00380000),
                new(0x00000000, 0x10000100),
                new(0x00000001, 0x00000001)
            ];

            InstEncoding[] imm6lUopQvdQvmConstraints =
            [
                new(0x00000000, 0x00380080),
                new(0x00000000, 0x10000100),
                new(0x00001040, 0x00001040),
                new(0x00000041, 0x00000041)
            ];

            InstEncoding[] qvdQvmSizeSizeConstraints =
            [
                new(0x00001040, 0x00001040),
                new(0x00000041, 0x00000041),
                new(0x00000000, 0x000C0000),
                new(0x000C0000, 0x000C0000)
            ];

            InstEncoding[] sizeSizeSizeQvdQvmConstraints =
            [
                new(0x00040000, 0x000C0000),
                new(0x00080000, 0x000C0000),
                new(0x000C0000, 0x000C0000),
                new(0x00001040, 0x00001040),
                new(0x00000041, 0x00000041)
            ];

            InstEncoding[] sizeSizeQvdQvmConstraints =
            [
                new(0x00080000, 0x000C0000),
                new(0x000C0000, 0x000C0000),
                new(0x00001040, 0x00001040),
                new(0x00000041, 0x00000041)
            ];

            InstEncoding[] imm6lQvdQvmConstraints =
            [
                new(0x00000000, 0x00380080),
                new(0x00001040, 0x00001040),
                new(0x00000041, 0x00000041)
            ];

            InstEncoding[] imm6VmConstraints =
            [
                new(0x00000000, 0x00380000),
                new(0x00000001, 0x00000001)
            ];

            InstEncoding[] imm6VdImm6Imm6Imm6Constraints =
            [
                new(0x00000000, 0x00380000),
                new(0x00001000, 0x00001000),
                new(0x00080000, 0x003F0000),
                new(0x00100000, 0x003F0000),
                new(0x00200000, 0x003F0000)
            ];

            InstEncoding[] sizeVdConstraints2 =
            [
                new(0x000C0000, 0x000C0000),
                new(0x00001000, 0x00001000)
            ];

            InstEncoding[] sizeQsizeQvdQvmConstraints =
            [
                new(0x000C0000, 0x000C0000),
                new(0x00080000, 0x000C0040),
                new(0x00001040, 0x00001040),
                new(0x00000041, 0x00000041)
            ];

            List<InstInfoForTable> insts = 
            [
                new(0xF1400000, 0xFBE08000, InstName.AdcI, T.AdcIT1, IsaVersion.v80, InstFlags.Rd),
                new(0xEB400000, 0xFFE08000, InstName.AdcR, T.AdcRT2, IsaVersion.v80, InstFlags.Rd),
                new(0xF1000000, 0xFBE08000, rnRdsConstraints, InstName.AddI, T.AddIT3, IsaVersion.v80, InstFlags.Rd),
                new(0xF2000000, 0xFBF08000, rnRnConstraints, InstName.AddI, T.AddIT4, IsaVersion.v80, InstFlags.Rd),
                new(0xEB000000, 0xFFE08000, rnRdsConstraints, InstName.AddR, T.AddRT3, IsaVersion.v80, InstFlags.Rd),
                new(0xF10D0000, 0xFBEF8000, rdsConstraints, InstName.AddSpI, T.AddSpIT3, IsaVersion.v80, InstFlags.Rd),
                new(0xF20D0000, 0xFBFF8000, InstName.AddSpI, T.AddSpIT4, IsaVersion.v80, InstFlags.Rd),
                new(0xEB0D0000, 0xFFEF8000, rdsConstraints, InstName.AddSpR, T.AddSpRT3, IsaVersion.v80, InstFlags.Rd),
                new(0xF2AF0000, 0xFBFF8000, InstName.Adr, T.AdrT2, IsaVersion.v80, InstFlags.Rd),
                new(0xF20F0000, 0xFBFF8000, InstName.Adr, T.AdrT3, IsaVersion.v80, InstFlags.Rd),
                new(0xFFB00340, 0xFFBF0FD0, vdVmConstraints, InstName.Aesd, T.AesdT1, IsaVersion.v80, IsaFeature.FeatAes, InstFlags.None),
                new(0xFFB00300, 0xFFBF0FD0, vdVmConstraints, InstName.Aese, T.AeseT1, IsaVersion.v80, IsaFeature.FeatAes, InstFlags.None),
                new(0xFFB003C0, 0xFFBF0FD0, vdVmConstraints, InstName.Aesimc, T.AesimcT1, IsaVersion.v80, IsaFeature.FeatAes, InstFlags.None),
                new(0xFFB00380, 0xFFBF0FD0, vdVmConstraints, InstName.Aesmc, T.AesmcT1, IsaVersion.v80, IsaFeature.FeatAes, InstFlags.None),
                new(0xF0000000, 0xFBE08000, rdsConstraints, InstName.AndI, T.AndIT1, IsaVersion.v80, InstFlags.Rd),
                new(0xEA000000, 0xFFE08000, rdsConstraints, InstName.AndR, T.AndRT2, IsaVersion.v80, InstFlags.Rd),
                new(0xF0008000, 0xF800D000, condCondCondConstraints, InstName.B, T.BT3, IsaVersion.v80, InstFlags.Cond),
                new(0xF0009000, 0xF800D000, InstName.B, T.BT4, IsaVersion.v80, InstFlags.None),
                new(0xF36F0000, 0xFFFF8020, InstName.Bfc, T.BfcT1, IsaVersion.v80, InstFlags.Rd),
                new(0xF3600000, 0xFFF08020, rnConstraints, InstName.Bfi, T.BfiT1, IsaVersion.v80, InstFlags.Rd),
                new(0xF0200000, 0xFBE08000, InstName.BicI, T.BicIT1, IsaVersion.v80, InstFlags.Rd),
                new(0xEA200000, 0xFFE08000, InstName.BicR, T.BicRT2, IsaVersion.v80, InstFlags.Rd),
                new(0xF000D000, 0xF800D000, InstName.BlI, T.BlIT1, IsaVersion.v80, InstFlags.None),
                new(0xF000C000, 0xF800D000, hConstraints, InstName.BlI, T.BlIT2, IsaVersion.v80, InstFlags.None),
                new(0xF3C08F00, 0xFFF0FFFF, InstName.Bxj, T.BxjT1, IsaVersion.v80, InstFlags.None),
                new(0xF3AF8016, 0xFFFFFFFF, InstName.Clrbhb, T.ClrbhbT1, IsaVersion.v89, IsaFeature.FeatClrbhb, InstFlags.None),
                new(0xF3BF8F2F, 0xFFFFFFFF, InstName.Clrex, T.ClrexT1, IsaVersion.v80, InstFlags.None),
                new(0xFAB0F080, 0xFFF0F0F0, InstName.Clz, T.ClzT1, IsaVersion.v80, InstFlags.Rd),
                new(0xF1100F00, 0xFBF08F00, InstName.CmnI, T.CmnIT1, IsaVersion.v80, InstFlags.None),
                new(0xEB100F00, 0xFFF08F00, InstName.CmnR, T.CmnRT2, IsaVersion.v80, InstFlags.None),
                new(0xF1B00F00, 0xFBF08F00, InstName.CmpI, T.CmpIT2, IsaVersion.v80, InstFlags.None),
                new(0xEBB00F00, 0xFFF08F00, InstName.CmpR, T.CmpRT3, IsaVersion.v80, InstFlags.None),
                new(0xF3AF8000, 0xFFFFF800, imodmConstraints, InstName.Cps, T.CpsT2, IsaVersion.v80, InstFlags.None),
                new(0xFAC0F080, 0xFFF0F0C0, InstName.Crc32, T.Crc32T1, IsaVersion.v80, IsaFeature.FeatCrc32, InstFlags.Rd),
                new(0xFAD0F080, 0xFFF0F0C0, InstName.Crc32c, T.Crc32cT1, IsaVersion.v80, IsaFeature.FeatCrc32, InstFlags.Rd),
                new(0xF3AF8014, 0xFFFFFFFF, InstName.Csdb, T.CsdbT1, IsaVersion.v80, InstFlags.None),
                new(0xF3AF80F0, 0xFFFFFFF0, InstName.Dbg, T.DbgT1, IsaVersion.v80, InstFlags.None),
                new(0xF78F8001, 0xFFFFFFFF, InstName.Dcps1, T.Dcps1T1, IsaVersion.v80, InstFlags.None),
                new(0xF78F8002, 0xFFFFFFFF, InstName.Dcps2, T.Dcps2T1, IsaVersion.v80, InstFlags.None),
                new(0xF78F8003, 0xFFFFFFFF, InstName.Dcps3, T.Dcps3T1, IsaVersion.v80, InstFlags.None),
                new(0xF3BF8F50, 0xFFFFFFF0, InstName.Dmb, T.DmbT1, IsaVersion.v80, InstFlags.None),
                new(0xF3BF8F40, 0xFFFFFFF0, optionConstraints, InstName.Dsb, T.DsbT1, IsaVersion.v80, InstFlags.None),
                new(0xF0800000, 0xFBE08000, rdsConstraints, InstName.EorI, T.EorIT1, IsaVersion.v80, InstFlags.Rd),
                new(0xEA800000, 0xFFE08000, rdsConstraints, InstName.EorR, T.EorRT2, IsaVersion.v80, InstFlags.Rd),
                new(0xF3DE8F00, 0xFFFFFFFF, InstName.Eret, T.EretT1, IsaVersion.v80, InstFlags.None),
                new(0xF3AF8010, 0xFFFFFFFF, InstName.Esb, T.EsbT1, IsaVersion.v82, IsaFeature.FeatRas, InstFlags.None),
                new(0xEC100B01, 0xFE100F01, puwPwPuwPuwConstraints, InstName.Fldmx, T.FldmxT1, IsaVersion.v80, InstFlags.WBack),
                new(0xEC000B01, 0xFE100F01, puwPwPuwPuwConstraints, InstName.Fstmx, T.FstmxT1, IsaVersion.v80, InstFlags.WBack),
                new(0xF7E08000, 0xFFF0F000, InstName.Hvc, T.HvcT1, IsaVersion.v80, InstFlags.None),
                new(0xF3BF8F60, 0xFFFFFFF0, InstName.Isb, T.IsbT1, IsaVersion.v80, InstFlags.None),
                new(0xE8D00FAF, 0xFFF00FFF, InstName.Lda, T.LdaT1, IsaVersion.v80, InstFlags.Rt),
                new(0xE8D00F8F, 0xFFF00FFF, InstName.Ldab, T.LdabT1, IsaVersion.v80, InstFlags.Rt),
                new(0xE8D00FEF, 0xFFF00FFF, InstName.Ldaex, T.LdaexT1, IsaVersion.v80, InstFlags.Rt),
                new(0xE8D00FCF, 0xFFF00FFF, InstName.Ldaexb, T.LdaexbT1, IsaVersion.v80, InstFlags.Rt),
                new(0xE8D000FF, 0xFFF000FF, InstName.Ldaexd, T.LdaexdT1, IsaVersion.v80, InstFlags.RtRt2),
                new(0xE8D00FDF, 0xFFF00FFF, InstName.Ldaexh, T.LdaexhT1, IsaVersion.v80, InstFlags.Rt),
                new(0xE8D00F9F, 0xFFF00FFF, InstName.Ldah, T.LdahT1, IsaVersion.v80, InstFlags.Rt),
                new(0xEC105E00, 0xFE50FF00, rnPuwConstraints, InstName.LdcI, T.LdcIT1, IsaVersion.v80, InstFlags.WBack),
                new(0xEC1F5E00, 0xFE5FFF00, puwConstraints, InstName.LdcL, T.LdcLT1, IsaVersion.v80, InstFlags.WBack),
                new(0xE8900000, 0xFFD00000, InstName.Ldm, T.LdmT2, IsaVersion.v80, InstFlags.RlistWBack),
                new(0xE9100000, 0xFFD00000, InstName.Ldmdb, T.LdmdbT1, IsaVersion.v80, InstFlags.RlistWBack),
                new(0xF8100E00, 0xFFF00F00, rnConstraints, InstName.Ldrbt, T.LdrbtT1, IsaVersion.v80, InstFlags.Rt),
                new(0xF8900000, 0xFFF00000, rnRtConstraints, InstName.LdrbI, T.LdrbIT2, IsaVersion.v80, InstFlags.Rt),
                new(0xF8100800, 0xFFF00800, rnRtpuwPuwPwConstraints, InstName.LdrbI, T.LdrbIT3, IsaVersion.v80, InstFlags.RtWBack),
                new(0xF81F0000, 0xFF7F0000, rtConstraints, InstName.LdrbL, T.LdrbLT1, IsaVersion.v80, InstFlags.Rt),
                new(0xF8100000, 0xFFF00FC0, rnRtConstraints, InstName.LdrbR, T.LdrbRT2, IsaVersion.v80, InstFlags.Rt),
                new(0xE8500000, 0xFE500000, rnPwConstraints, InstName.LdrdI, T.LdrdIT1, IsaVersion.v80, InstFlags.Rt2WBack),
                new(0xE85F0000, 0xFE5F0000, pwConstraints, InstName.LdrdL, T.LdrdLT1, IsaVersion.v80, InstFlags.Rt2WBack),
                new(0xE8500F00, 0xFFF00F00, InstName.Ldrex, T.LdrexT1, IsaVersion.v80, InstFlags.Rt),
                new(0xE8D00F4F, 0xFFF00FFF, InstName.Ldrexb, T.LdrexbT1, IsaVersion.v80, InstFlags.Rt),
                new(0xE8D0007F, 0xFFF000FF, InstName.Ldrexd, T.LdrexdT1, IsaVersion.v80, InstFlags.RtRt2),
                new(0xE8D00F5F, 0xFFF00FFF, InstName.Ldrexh, T.LdrexhT1, IsaVersion.v80, InstFlags.Rt),
                new(0xF8300E00, 0xFFF00F00, rnConstraints, InstName.Ldrht, T.LdrhtT1, IsaVersion.v80, InstFlags.Rt),
                new(0xF8B00000, 0xFFF00000, rnRtConstraints, InstName.LdrhI, T.LdrhIT2, IsaVersion.v80, InstFlags.Rt),
                new(0xF8300800, 0xFFF00800, rnRtpuwPuwPwConstraints, InstName.LdrhI, T.LdrhIT3, IsaVersion.v80, InstFlags.RtWBack),
                new(0xF83F0000, 0xFF7F0000, rtConstraints, InstName.LdrhL, T.LdrhLT1, IsaVersion.v80, InstFlags.Rt),
                new(0xF8300000, 0xFFF00FC0, rnRtConstraints, InstName.LdrhR, T.LdrhRT2, IsaVersion.v80, InstFlags.Rt),
                new(0xF9100E00, 0xFFF00F00, rnConstraints, InstName.Ldrsbt, T.LdrsbtT1, IsaVersion.v80, InstFlags.Rt),
                new(0xF9900000, 0xFFF00000, rnRtConstraints, InstName.LdrsbI, T.LdrsbIT1, IsaVersion.v80, InstFlags.Rt),
                new(0xF9100800, 0xFFF00800, rnRtpuwPuwPwConstraints, InstName.LdrsbI, T.LdrsbIT2, IsaVersion.v80, InstFlags.RtWBack),
                new(0xF91F0000, 0xFF7F0000, rtConstraints, InstName.LdrsbL, T.LdrsbLT1, IsaVersion.v80, InstFlags.Rt),
                new(0xF9100000, 0xFFF00FC0, rnRtConstraints, InstName.LdrsbR, T.LdrsbRT2, IsaVersion.v80, InstFlags.Rt),
                new(0xF9300E00, 0xFFF00F00, rnConstraints, InstName.Ldrsht, T.LdrshtT1, IsaVersion.v80, InstFlags.Rt),
                new(0xF9B00000, 0xFFF00000, rnRtConstraints, InstName.LdrshI, T.LdrshIT1, IsaVersion.v80, InstFlags.Rt),
                new(0xF9300800, 0xFFF00800, rnRtpuwPuwPwConstraints, InstName.LdrshI, T.LdrshIT2, IsaVersion.v80, InstFlags.RtWBack),
                new(0xF93F0000, 0xFF7F0000, rtConstraints, InstName.LdrshL, T.LdrshLT1, IsaVersion.v80, InstFlags.Rt),
                new(0xF9300000, 0xFFF00FC0, rnRtConstraints, InstName.LdrshR, T.LdrshRT2, IsaVersion.v80, InstFlags.Rt),
                new(0xF8500E00, 0xFFF00F00, rnConstraints, InstName.Ldrt, T.LdrtT1, IsaVersion.v80, InstFlags.Rt),
                new(0xF8D00000, 0xFFF00000, rnConstraints, InstName.LdrI, T.LdrIT3, IsaVersion.v80, InstFlags.Rt),
                new(0xF8500800, 0xFFF00800, rnPuwPwConstraints, InstName.LdrI, T.LdrIT4, IsaVersion.v80, InstFlags.RtWBack),
                new(0xF85F0000, 0xFF7F0000, InstName.LdrL, T.LdrLT2, IsaVersion.v80, InstFlags.Rt),
                new(0xF8500000, 0xFFF00FC0, rnConstraints, InstName.LdrR, T.LdrRT2, IsaVersion.v80, InstFlags.Rt),
                new(0xEE000E10, 0xFF100E10, InstName.Mcr, T.McrT1, IsaVersion.v80, InstFlags.Rt),
                new(0xEC400E00, 0xFFF00E00, InstName.Mcrr, T.McrrT1, IsaVersion.v80, InstFlags.Rt),
                new(0xFB000000, 0xFFF000F0, raConstraints, InstName.Mla, T.MlaT1, IsaVersion.v80, InstFlags.Rd),
                new(0xFB000010, 0xFFF000F0, InstName.Mls, T.MlsT1, IsaVersion.v80, InstFlags.Rd),
                new(0xF2C00000, 0xFBF08000, InstName.Movt, T.MovtT1, IsaVersion.v80, InstFlags.Rd),
                new(0xF04F0000, 0xFBEF8000, InstName.MovI, T.MovIT2, IsaVersion.v80, InstFlags.Rd),
                new(0xF2400000, 0xFBF08000, InstName.MovI, T.MovIT3, IsaVersion.v80, InstFlags.Rd),
                new(0xEA4F0000, 0xFFEF8000, InstName.MovR, T.MovRT3, IsaVersion.v80, InstFlags.Rd),
                new(0xFA00F000, 0xFF80F0F0, InstName.MovRr, T.MovRrT2, IsaVersion.v80, InstFlags.Rd),
                new(0xEE100E10, 0xFF100E10, InstName.Mrc, T.MrcT1, IsaVersion.v80, InstFlags.Rt),
                new(0xEC500E00, 0xFFF00E00, InstName.Mrrc, T.MrrcT1, IsaVersion.v80, InstFlags.Rt),
                new(0xF3EF8000, 0xFFEFF0FF, InstName.Mrs, T.MrsT1, IsaVersion.v80, InstFlags.Rd),
                new(0xF3E08020, 0xFFE0F0EF, InstName.MrsBr, T.MrsBrT1, IsaVersion.v80, InstFlags.Rd),
                new(0xF3808020, 0xFFE0F0EF, InstName.MsrBr, T.MsrBrT1, IsaVersion.v80, InstFlags.None),
                new(0xF3808000, 0xFFE0F0FF, InstName.MsrR, T.MsrRT1, IsaVersion.v80, InstFlags.None),
                new(0xFB00F000, 0xFFF0F0F0, InstName.Mul, T.MulT2, IsaVersion.v80, InstFlags.Rd),
                new(0xF06F0000, 0xFBEF8000, InstName.MvnI, T.MvnIT1, IsaVersion.v80, InstFlags.Rd),
                new(0xEA6F0000, 0xFFEF8000, InstName.MvnR, T.MvnRT2, IsaVersion.v80, InstFlags.Rd),
                new(0xF3AF8000, 0xFFFFFFFF, InstName.Nop, T.NopT2, IsaVersion.v80, InstFlags.None),
                new(0xF0600000, 0xFBE08000, rnConstraints, InstName.OrnI, T.OrnIT1, IsaVersion.v80, InstFlags.Rd),
                new(0xEA600000, 0xFFE08000, rnConstraints, InstName.OrnR, T.OrnRT1, IsaVersion.v80, InstFlags.Rd),
                new(0xF0400000, 0xFBE08000, rnConstraints, InstName.OrrI, T.OrrIT1, IsaVersion.v80, InstFlags.Rd),
                new(0xEA400000, 0xFFE08000, rnConstraints, InstName.OrrR, T.OrrRT2, IsaVersion.v80, InstFlags.Rd),
                new(0xEAC00000, 0xFFF08010, sTConstraints, InstName.Pkh, T.PkhT1, IsaVersion.v80, InstFlags.Rd),
                new(0xF890F000, 0xFFD0F000, rnConstraints, InstName.PldI, T.PldIT1, IsaVersion.v80, InstFlags.WBack),
                new(0xF810FC00, 0xFFD0FF00, rnConstraints, InstName.PldI, T.PldIT2, IsaVersion.v80, InstFlags.WBack),
                new(0xF81FF000, 0xFF7FF000, InstName.PldL, T.PldLT1, IsaVersion.v80, InstFlags.None),
                new(0xF810F000, 0xFFD0FFC0, rnConstraints, InstName.PldR, T.PldRT1, IsaVersion.v80, InstFlags.WBack),
                new(0xF990F000, 0xFFF0F000, rnConstraints, InstName.PliI, T.PliIT1, IsaVersion.v80, InstFlags.None),
                new(0xF910FC00, 0xFFF0FF00, rnConstraints, InstName.PliI, T.PliIT2, IsaVersion.v80, InstFlags.None),
                new(0xF91FF000, 0xFF7FF000, InstName.PliI, T.PliIT3, IsaVersion.v80, InstFlags.None),
                new(0xF910F000, 0xFFF0FFC0, rnConstraints, InstName.PliR, T.PliRT1, IsaVersion.v80, InstFlags.None),
                new(0xF3BF8F44, 0xFFFFFFFF, InstName.Pssbb, T.PssbbT1, IsaVersion.v80, InstFlags.None),
                new(0xFA80F080, 0xFFF0F0F0, InstName.Qadd, T.QaddT1, IsaVersion.v80, InstFlags.Rd),
                new(0xFA90F010, 0xFFF0F0F0, InstName.Qadd16, T.Qadd16T1, IsaVersion.v80, InstFlags.Rd),
                new(0xFA80F010, 0xFFF0F0F0, InstName.Qadd8, T.Qadd8T1, IsaVersion.v80, InstFlags.Rd),
                new(0xFAA0F010, 0xFFF0F0F0, InstName.Qasx, T.QasxT1, IsaVersion.v80, InstFlags.Rd),
                new(0xFA80F090, 0xFFF0F0F0, InstName.Qdadd, T.QdaddT1, IsaVersion.v80, InstFlags.Rd),
                new(0xFA80F0B0, 0xFFF0F0F0, InstName.Qdsub, T.QdsubT1, IsaVersion.v80, InstFlags.Rd),
                new(0xFAE0F010, 0xFFF0F0F0, InstName.Qsax, T.QsaxT1, IsaVersion.v80, InstFlags.Rd),
                new(0xFA80F0A0, 0xFFF0F0F0, InstName.Qsub, T.QsubT1, IsaVersion.v80, InstFlags.Rd),
                new(0xFAD0F010, 0xFFF0F0F0, InstName.Qsub16, T.Qsub16T1, IsaVersion.v80, InstFlags.Rd),
                new(0xFAC0F010, 0xFFF0F0F0, InstName.Qsub8, T.Qsub8T1, IsaVersion.v80, InstFlags.Rd),
                new(0xFA90F0A0, 0xFFF0F0F0, InstName.Rbit, T.RbitT1, IsaVersion.v80, InstFlags.Rd),
                new(0xFA90F080, 0xFFF0F0F0, InstName.Rev, T.RevT2, IsaVersion.v80, InstFlags.Rd),
                new(0xFA90F090, 0xFFF0F0F0, InstName.Rev16, T.Rev16T2, IsaVersion.v80, InstFlags.Rd),
                new(0xFA90F0B0, 0xFFF0F0F0, InstName.Revsh, T.RevshT2, IsaVersion.v80, InstFlags.Rd),
                new(0xE810C000, 0xFFD0FFFF, InstName.Rfe, T.RfeT1, IsaVersion.v80, InstFlags.WBack),
                new(0xE990C000, 0xFFD0FFFF, InstName.Rfe, T.RfeT2, IsaVersion.v80, InstFlags.WBack),
                new(0xF1C00000, 0xFBE08000, InstName.RsbI, T.RsbIT2, IsaVersion.v80, InstFlags.Rd),
                new(0xEBC00000, 0xFFE08000, InstName.RsbR, T.RsbRT1, IsaVersion.v80, InstFlags.Rd),
                new(0xFA90F000, 0xFFF0F0F0, InstName.Sadd16, T.Sadd16T1, IsaVersion.v80, InstFlags.Rd),
                new(0xFA80F000, 0xFFF0F0F0, InstName.Sadd8, T.Sadd8T1, IsaVersion.v80, InstFlags.Rd),
                new(0xFAA0F000, 0xFFF0F0F0, InstName.Sasx, T.SasxT1, IsaVersion.v80, InstFlags.Rd),
                new(0xF3BF8F70, 0xFFFFFFFF, InstName.Sb, T.SbT1, IsaVersion.v85, IsaFeature.FeatSb, InstFlags.None),
                new(0xF1600000, 0xFBE08000, InstName.SbcI, T.SbcIT1, IsaVersion.v80, InstFlags.Rd),
                new(0xEB600000, 0xFFE08000, InstName.SbcR, T.SbcRT2, IsaVersion.v80, InstFlags.Rd),
                new(0xF3400000, 0xFFF08020, InstName.Sbfx, T.SbfxT1, IsaVersion.v80, InstFlags.Rd),
                new(0xFB90F0F0, 0xFFF0F0F0, InstName.Sdiv, T.SdivT1, IsaVersion.v80, InstFlags.Rd),
                new(0xFAA0F080, 0xFFF0F0F0, InstName.Sel, T.SelT1, IsaVersion.v80, InstFlags.Rd),
                new(0xF3AF8004, 0xFFFFFFFF, InstName.Sev, T.SevT2, IsaVersion.v80, InstFlags.None),
                new(0xF3AF8005, 0xFFFFFFFF, InstName.Sevl, T.SevlT2, IsaVersion.v80, InstFlags.None),
                new(0xEF000C40, 0xFFB00F50, vdVnVmConstraints, InstName.Sha1c, T.Sha1cT1, IsaVersion.v80, IsaFeature.FeatSha1, InstFlags.None),
                new(0xFFB902C0, 0xFFBF0FD0, vdVmConstraints, InstName.Sha1h, T.Sha1hT1, IsaVersion.v80, IsaFeature.FeatSha1, InstFlags.None),
                new(0xEF200C40, 0xFFB00F50, vdVnVmConstraints, InstName.Sha1m, T.Sha1mT1, IsaVersion.v80, IsaFeature.FeatSha1, InstFlags.None),
                new(0xEF100C40, 0xFFB00F50, vdVnVmConstraints, InstName.Sha1p, T.Sha1pT1, IsaVersion.v80, IsaFeature.FeatSha1, InstFlags.None),
                new(0xEF300C40, 0xFFB00F50, vdVnVmConstraints, InstName.Sha1su0, T.Sha1su0T1, IsaVersion.v80, IsaFeature.FeatSha1, InstFlags.None),
                new(0xFFBA0380, 0xFFBF0FD0, vdVmConstraints, InstName.Sha1su1, T.Sha1su1T1, IsaVersion.v80, IsaFeature.FeatSha1, InstFlags.None),
                new(0xFF000C40, 0xFFB00F50, vdVnVmConstraints, InstName.Sha256h, T.Sha256hT1, IsaVersion.v80, IsaFeature.FeatSha256, InstFlags.None),
                new(0xFF100C40, 0xFFB00F50, vdVnVmConstraints, InstName.Sha256h2, T.Sha256h2T1, IsaVersion.v80, IsaFeature.FeatSha256, InstFlags.None),
                new(0xFFBA03C0, 0xFFBF0FD0, vdVmConstraints, InstName.Sha256su0, T.Sha256su0T1, IsaVersion.v80, IsaFeature.FeatSha256, InstFlags.None),
                new(0xFF200C40, 0xFFB00F50, vdVnVmConstraints, InstName.Sha256su1, T.Sha256su1T1, IsaVersion.v80, IsaFeature.FeatSha256, InstFlags.None),
                new(0xFA90F020, 0xFFF0F0F0, InstName.Shadd16, T.Shadd16T1, IsaVersion.v80, InstFlags.Rd),
                new(0xFA80F020, 0xFFF0F0F0, InstName.Shadd8, T.Shadd8T1, IsaVersion.v80, InstFlags.Rd),
                new(0xFAA0F020, 0xFFF0F0F0, InstName.Shasx, T.ShasxT1, IsaVersion.v80, InstFlags.Rd),
                new(0xFAE0F020, 0xFFF0F0F0, InstName.Shsax, T.ShsaxT1, IsaVersion.v80, InstFlags.Rd),
                new(0xFAD0F020, 0xFFF0F0F0, InstName.Shsub16, T.Shsub16T1, IsaVersion.v80, InstFlags.Rd),
                new(0xFAC0F020, 0xFFF0F0F0, InstName.Shsub8, T.Shsub8T1, IsaVersion.v80, InstFlags.Rd),
                new(0xF7F08000, 0xFFF0FFFF, InstName.Smc, T.SmcT1, IsaVersion.v80, InstFlags.None),
                new(0xFB100000, 0xFFF000C0, raConstraints, InstName.Smlabb, T.SmlabbT1, IsaVersion.v80, InstFlags.Rd),
                new(0xFB200000, 0xFFF000E0, raConstraints, InstName.Smlad, T.SmladT1, IsaVersion.v80, InstFlags.Rd),
                new(0xFBC00000, 0xFFF000F0, InstName.Smlal, T.SmlalT1, IsaVersion.v80, InstFlags.RdLoRdHi),
                new(0xFBC00080, 0xFFF000C0, InstName.Smlalbb, T.SmlalbbT1, IsaVersion.v80, InstFlags.RdLoRdHi),
                new(0xFBC000C0, 0xFFF000E0, InstName.Smlald, T.SmlaldT1, IsaVersion.v80, InstFlags.RdLoRdHi),
                new(0xFB300000, 0xFFF000E0, raConstraints, InstName.Smlawb, T.SmlawbT1, IsaVersion.v80, InstFlags.Rd),
                new(0xFB400000, 0xFFF000E0, raConstraints, InstName.Smlsd, T.SmlsdT1, IsaVersion.v80, InstFlags.Rd),
                new(0xFBD000C0, 0xFFF000E0, InstName.Smlsld, T.SmlsldT1, IsaVersion.v80, InstFlags.RdLoRdHi),
                new(0xFB500000, 0xFFF000E0, raConstraints, InstName.Smmla, T.SmmlaT1, IsaVersion.v80, InstFlags.Rd),
                new(0xFB600000, 0xFFF000E0, InstName.Smmls, T.SmmlsT1, IsaVersion.v80, InstFlags.Rd),
                new(0xFB50F000, 0xFFF0F0E0, InstName.Smmul, T.SmmulT1, IsaVersion.v80, InstFlags.Rd),
                new(0xFB20F000, 0xFFF0F0E0, InstName.Smuad, T.SmuadT1, IsaVersion.v80, InstFlags.Rd),
                new(0xFB10F000, 0xFFF0F0C0, InstName.Smulbb, T.SmulbbT1, IsaVersion.v80, InstFlags.Rd),
                new(0xFB800000, 0xFFF000F0, InstName.Smull, T.SmullT1, IsaVersion.v80, InstFlags.RdLoRdHi),
                new(0xFB30F000, 0xFFF0F0E0, InstName.Smulwb, T.SmulwbT1, IsaVersion.v80, InstFlags.Rd),
                new(0xFB40F000, 0xFFF0F0E0, InstName.Smusd, T.SmusdT1, IsaVersion.v80, InstFlags.Rd),
                new(0xE80DC000, 0xFFDFFFE0, InstName.Srs, T.SrsT1, IsaVersion.v80, InstFlags.WBack),
                new(0xE98DC000, 0xFFDFFFE0, InstName.Srs, T.SrsT2, IsaVersion.v80, InstFlags.WBack),
                new(0xF3000000, 0xFFD08020, shimm2imm3Constraints, InstName.Ssat, T.SsatT1, IsaVersion.v80, InstFlags.Rd),
                new(0xF3200000, 0xFFF0F0F0, InstName.Ssat16, T.Ssat16T1, IsaVersion.v80, InstFlags.Rd),
                new(0xFAE0F000, 0xFFF0F0F0, InstName.Ssax, T.SsaxT1, IsaVersion.v80, InstFlags.Rd),
                new(0xF3BF8F40, 0xFFFFFFFF, InstName.Ssbb, T.SsbbT1, IsaVersion.v80, InstFlags.None),
                new(0xFAD0F000, 0xFFF0F0F0, InstName.Ssub16, T.Ssub16T1, IsaVersion.v80, InstFlags.Rd),
                new(0xFAC0F000, 0xFFF0F0F0, InstName.Ssub8, T.Ssub8T1, IsaVersion.v80, InstFlags.Rd),
                new(0xEC005E00, 0xFE50FF00, puwConstraints, InstName.Stc, T.StcT1, IsaVersion.v80, InstFlags.WBack),
                new(0xE8C00FAF, 0xFFF00FFF, InstName.Stl, T.StlT1, IsaVersion.v80, InstFlags.RtRead),
                new(0xE8C00F8F, 0xFFF00FFF, InstName.Stlb, T.StlbT1, IsaVersion.v80, InstFlags.RtRead),
                new(0xE8C00FE0, 0xFFF00FF0, InstName.Stlex, T.StlexT1, IsaVersion.v80, InstFlags.RdRtReadRd16),
                new(0xE8C00FC0, 0xFFF00FF0, InstName.Stlexb, T.StlexbT1, IsaVersion.v80, InstFlags.RdRtReadRd16),
                new(0xE8C000F0, 0xFFF000F0, InstName.Stlexd, T.StlexdT1, IsaVersion.v80, InstFlags.RdRt2ReadRd16),
                new(0xE8C00FD0, 0xFFF00FF0, InstName.Stlexh, T.StlexhT1, IsaVersion.v80, InstFlags.RdRtReadRd16),
                new(0xE8C00F9F, 0xFFF00FFF, InstName.Stlh, T.StlhT1, IsaVersion.v80, InstFlags.RtRead),
                new(0xE8800000, 0xFFD08000, InstName.Stm, T.StmT2, IsaVersion.v80, InstFlags.RlistReadWBack),
                new(0xE9000000, 0xFFD08000, InstName.Stmdb, T.StmdbT1, IsaVersion.v80, InstFlags.RlistReadWBack),
                new(0xF8000E00, 0xFFF00F00, rnConstraints, InstName.Strbt, T.StrbtT1, IsaVersion.v80, InstFlags.RtRead),
                new(0xF8800000, 0xFFF00000, rnConstraints, InstName.StrbI, T.StrbIT2, IsaVersion.v80, InstFlags.RtRead),
                new(0xF8000800, 0xFFF00800, rnPuwPwConstraints, InstName.StrbI, T.StrbIT3, IsaVersion.v80, InstFlags.RtReadWBack),
                new(0xF8000000, 0xFFF00FC0, rnConstraints, InstName.StrbR, T.StrbRT2, IsaVersion.v80, InstFlags.RtRead),
                new(0xE8400000, 0xFE500000, rnPwConstraints, InstName.StrdI, T.StrdIT1, IsaVersion.v80, InstFlags.Rt2ReadWBack),
                new(0xE8400000, 0xFFF00000, InstName.Strex, T.StrexT1, IsaVersion.v80, InstFlags.RdRtRead),
                new(0xE8C00F40, 0xFFF00FF0, InstName.Strexb, T.StrexbT1, IsaVersion.v80, InstFlags.RdRtReadRd16),
                new(0xE8C00070, 0xFFF000F0, InstName.Strexd, T.StrexdT1, IsaVersion.v80, InstFlags.RdRt2ReadRd16),
                new(0xE8C00F50, 0xFFF00FF0, InstName.Strexh, T.StrexhT1, IsaVersion.v80, InstFlags.RdRtReadRd16),
                new(0xF8200E00, 0xFFF00F00, rnConstraints, InstName.Strht, T.StrhtT1, IsaVersion.v80, InstFlags.RtRead),
                new(0xF8A00000, 0xFFF00000, rnConstraints, InstName.StrhI, T.StrhIT2, IsaVersion.v80, InstFlags.RtRead),
                new(0xF8200800, 0xFFF00800, rnPuwPwConstraints, InstName.StrhI, T.StrhIT3, IsaVersion.v80, InstFlags.RtReadWBack),
                new(0xF8200000, 0xFFF00FC0, rnConstraints, InstName.StrhR, T.StrhRT2, IsaVersion.v80, InstFlags.RtRead),
                new(0xF8400E00, 0xFFF00F00, rnConstraints, InstName.Strt, T.StrtT1, IsaVersion.v80, InstFlags.RtRead),
                new(0xF8C00000, 0xFFF00000, rnConstraints, InstName.StrI, T.StrIT3, IsaVersion.v80, InstFlags.RtRead),
                new(0xF8400800, 0xFFF00800, rnPuwPwConstraints, InstName.StrI, T.StrIT4, IsaVersion.v80, InstFlags.RtReadWBack),
                new(0xF8400000, 0xFFF00FC0, rnConstraints, InstName.StrR, T.StrRT2, IsaVersion.v80, InstFlags.RtRead),
                new(0xF1A00000, 0xFBE08000, rnRdsConstraints, InstName.SubI, T.SubIT3, IsaVersion.v80, InstFlags.Rd),
                new(0xF2A00000, 0xFBF08000, rnRnConstraints, InstName.SubI, T.SubIT4, IsaVersion.v80, InstFlags.Rd),
                new(0xF3D08F00, 0xFFF0FF00, rnimm8Constraints, InstName.SubI, T.SubIT5, IsaVersion.v80, InstFlags.None),
                new(0xEBA00000, 0xFFE08000, rnRdsConstraints, InstName.SubR, T.SubRT2, IsaVersion.v80, InstFlags.Rd),
                new(0xF1AD0000, 0xFBEF8000, rdsConstraints, InstName.SubSpI, T.SubSpIT2, IsaVersion.v80, InstFlags.Rd),
                new(0xF2AD0000, 0xFBFF8000, InstName.SubSpI, T.SubSpIT3, IsaVersion.v80, InstFlags.Rd),
                new(0xEBAD0000, 0xFFEF8000, rdsConstraints, InstName.SubSpR, T.SubSpRT1, IsaVersion.v80, InstFlags.Rd),
                new(0xFA40F080, 0xFFF0F0C0, rnConstraints, InstName.Sxtab, T.SxtabT1, IsaVersion.v80, InstFlags.Rd),
                new(0xFA20F080, 0xFFF0F0C0, rnConstraints, InstName.Sxtab16, T.Sxtab16T1, IsaVersion.v80, InstFlags.Rd),
                new(0xFA00F080, 0xFFF0F0C0, rnConstraints, InstName.Sxtah, T.SxtahT1, IsaVersion.v80, InstFlags.Rd),
                new(0xFA4FF080, 0xFFFFF0C0, InstName.Sxtb, T.SxtbT2, IsaVersion.v80, InstFlags.Rd),
                new(0xFA2FF080, 0xFFFFF0C0, InstName.Sxtb16, T.Sxtb16T1, IsaVersion.v80, InstFlags.Rd),
                new(0xFA0FF080, 0xFFFFF0C0, InstName.Sxth, T.SxthT2, IsaVersion.v80, InstFlags.Rd),
                new(0xE8D0F000, 0xFFF0FFE0, InstName.Tbb, T.TbbT1, IsaVersion.v80, InstFlags.None),
                new(0xF0900F00, 0xFBF08F00, InstName.TeqI, T.TeqIT1, IsaVersion.v80, InstFlags.None),
                new(0xEA900F00, 0xFFF08F00, InstName.TeqR, T.TeqRT1, IsaVersion.v80, InstFlags.None),
                new(0xF3AF8012, 0xFFFFFFFF, InstName.Tsb, T.TsbT1, IsaVersion.v84, IsaFeature.FeatTrf, InstFlags.None),
                new(0xF0100F00, 0xFBF08F00, InstName.TstI, T.TstIT1, IsaVersion.v80, InstFlags.None),
                new(0xEA100F00, 0xFFF08F00, InstName.TstR, T.TstRT2, IsaVersion.v80, InstFlags.None),
                new(0xFA90F040, 0xFFF0F0F0, InstName.Uadd16, T.Uadd16T1, IsaVersion.v80, InstFlags.Rd),
                new(0xFA80F040, 0xFFF0F0F0, InstName.Uadd8, T.Uadd8T1, IsaVersion.v80, InstFlags.Rd),
                new(0xFAA0F040, 0xFFF0F0F0, InstName.Uasx, T.UasxT1, IsaVersion.v80, InstFlags.Rd),
                new(0xF3C00000, 0xFFF08020, InstName.Ubfx, T.UbfxT1, IsaVersion.v80, InstFlags.Rd),
                new(0xF7F0A000, 0xFFF0F000, InstName.Udf, T.UdfT2, IsaVersion.v80, InstFlags.None),
                new(0xFBB0F0F0, 0xFFF0F0F0, InstName.Udiv, T.UdivT1, IsaVersion.v80, InstFlags.Rd),
                new(0xFA90F060, 0xFFF0F0F0, InstName.Uhadd16, T.Uhadd16T1, IsaVersion.v80, InstFlags.Rd),
                new(0xFA80F060, 0xFFF0F0F0, InstName.Uhadd8, T.Uhadd8T1, IsaVersion.v80, InstFlags.Rd),
                new(0xFAA0F060, 0xFFF0F0F0, InstName.Uhasx, T.UhasxT1, IsaVersion.v80, InstFlags.Rd),
                new(0xFAE0F060, 0xFFF0F0F0, InstName.Uhsax, T.UhsaxT1, IsaVersion.v80, InstFlags.Rd),
                new(0xFAD0F060, 0xFFF0F0F0, InstName.Uhsub16, T.Uhsub16T1, IsaVersion.v80, InstFlags.Rd),
                new(0xFAC0F060, 0xFFF0F0F0, InstName.Uhsub8, T.Uhsub8T1, IsaVersion.v80, InstFlags.Rd),
                new(0xFBE00060, 0xFFF000F0, InstName.Umaal, T.UmaalT1, IsaVersion.v80, InstFlags.RdLoRdHi),
                new(0xFBE00000, 0xFFF000F0, InstName.Umlal, T.UmlalT1, IsaVersion.v80, InstFlags.RdLoRdHi),
                new(0xFBA00000, 0xFFF000F0, InstName.Umull, T.UmullT1, IsaVersion.v80, InstFlags.RdLoRdHi),
                new(0xFA90F050, 0xFFF0F0F0, InstName.Uqadd16, T.Uqadd16T1, IsaVersion.v80, InstFlags.Rd),
                new(0xFA80F050, 0xFFF0F0F0, InstName.Uqadd8, T.Uqadd8T1, IsaVersion.v80, InstFlags.Rd),
                new(0xFAA0F050, 0xFFF0F0F0, InstName.Uqasx, T.UqasxT1, IsaVersion.v80, InstFlags.Rd),
                new(0xFAE0F050, 0xFFF0F0F0, InstName.Uqsax, T.UqsaxT1, IsaVersion.v80, InstFlags.Rd),
                new(0xFAD0F050, 0xFFF0F0F0, InstName.Uqsub16, T.Uqsub16T1, IsaVersion.v80, InstFlags.Rd),
                new(0xFAC0F050, 0xFFF0F0F0, InstName.Uqsub8, T.Uqsub8T1, IsaVersion.v80, InstFlags.Rd),
                new(0xFB70F000, 0xFFF0F0F0, InstName.Usad8, T.Usad8T1, IsaVersion.v80, InstFlags.Rd),
                new(0xFB700000, 0xFFF000F0, raConstraints, InstName.Usada8, T.Usada8T1, IsaVersion.v80, InstFlags.Rd),
                new(0xF3800000, 0xFFD08020, shimm2imm3Constraints, InstName.Usat, T.UsatT1, IsaVersion.v80, InstFlags.Rd),
                new(0xF3A00000, 0xFFF0F0F0, InstName.Usat16, T.Usat16T1, IsaVersion.v80, InstFlags.Rd),
                new(0xFAE0F040, 0xFFF0F0F0, InstName.Usax, T.UsaxT1, IsaVersion.v80, InstFlags.Rd),
                new(0xFAD0F040, 0xFFF0F0F0, InstName.Usub16, T.Usub16T1, IsaVersion.v80, InstFlags.Rd),
                new(0xFAC0F040, 0xFFF0F0F0, InstName.Usub8, T.Usub8T1, IsaVersion.v80, InstFlags.Rd),
                new(0xFA50F080, 0xFFF0F0C0, rnConstraints, InstName.Uxtab, T.UxtabT1, IsaVersion.v80, InstFlags.Rd),
                new(0xFA30F080, 0xFFF0F0C0, rnConstraints, InstName.Uxtab16, T.Uxtab16T1, IsaVersion.v80, InstFlags.Rd),
                new(0xFA10F080, 0xFFF0F0C0, rnConstraints, InstName.Uxtah, T.UxtahT1, IsaVersion.v80, InstFlags.Rd),
                new(0xFA5FF080, 0xFFFFF0C0, InstName.Uxtb, T.UxtbT2, IsaVersion.v80, InstFlags.Rd),
                new(0xFA3FF080, 0xFFFFF0C0, InstName.Uxtb16, T.Uxtb16T1, IsaVersion.v80, InstFlags.Rd),
                new(0xFA1FF080, 0xFFFFF0C0, InstName.Uxth, T.UxthT2, IsaVersion.v80, InstFlags.Rd),
                new(0xEF000710, 0xEF800F10, sizeQvdQvnQvmConstraints, InstName.Vaba, T.VabaT1, IsaVersion.v80, InstFlags.None),
                new(0xEF800500, 0xEF800F50, sizeVdConstraints, InstName.Vabal, T.VabalT1, IsaVersion.v80, InstFlags.None),
                new(0xEF800700, 0xEF800F50, sizeVdConstraints, InstName.VabdlI, T.VabdlIT1, IsaVersion.v80, InstFlags.None),
                new(0xFF200D00, 0xFFB00F10, qvdQvnQvmConstraints, InstName.VabdF, T.VabdFT1, IsaVersion.v80, InstFlags.None),
                new(0xFF300D00, 0xFFB00F10, qvdQvnQvmConstraints, InstName.VabdF, T.VabdFT1, IsaVersion.v80, IsaFeature.FeatFp16, InstFlags.None),
                new(0xEF000700, 0xEF800F10, sizeQvdQvnQvmConstraints, InstName.VabdI, T.VabdIT1, IsaVersion.v80, InstFlags.None),
                new(0xFFB10300, 0xFFB30F90, sizeQvdQvmConstraints, InstName.Vabs, T.VabsT1, IsaVersion.v80, InstFlags.None),
                new(0xFFB90700, 0xFFBB0F90, sizeQvdQvmConstraints, InstName.Vabs, T.VabsT1, IsaVersion.v80, InstFlags.None),
                new(0xFFB50700, 0xFFBF0F90, sizeQvdQvmConstraints, InstName.Vabs, T.VabsT1, IsaVersion.v80, IsaFeature.FeatFp16, InstFlags.None),
                new(0xEEB00AC0, 0xFFBF0ED0, InstName.Vabs, T.VabsT2, IsaVersion.v80, InstFlags.None),
                new(0xEEB009C0, 0xFFBF0FD0, InstName.Vabs, T.VabsT2, IsaVersion.v80, IsaFeature.FeatFp16, InstFlags.None),
                new(0xFF000E10, 0xFFB00F10, qvdQvnQvmConstraints, InstName.Vacge, T.VacgeT1, IsaVersion.v80, InstFlags.None),
                new(0xFF100E10, 0xFFB00F10, qvdQvnQvmConstraints, InstName.Vacge, T.VacgeT1, IsaVersion.v80, IsaFeature.FeatFp16, InstFlags.None),
                new(0xFF200E10, 0xFFB00F10, qvdQvnQvmConstraints, InstName.Vacgt, T.VacgtT1, IsaVersion.v80, InstFlags.None),
                new(0xFF300E10, 0xFFB00F10, qvdQvnQvmConstraints, InstName.Vacgt, T.VacgtT1, IsaVersion.v80, IsaFeature.FeatFp16, InstFlags.None),
                new(0xEF800400, 0xFF800F50, sizeVnVmConstraints, InstName.Vaddhn, T.VaddhnT1, IsaVersion.v80, InstFlags.None),
                new(0xEF800000, 0xEF800F50, sizeVdOpvnConstraints, InstName.Vaddl, T.VaddlT1, IsaVersion.v80, InstFlags.None),
                new(0xEF800100, 0xEF800F50, sizeVdOpvnConstraints, InstName.Vaddw, T.VaddwT1, IsaVersion.v80, InstFlags.None),
                new(0xEF000D00, 0xFFB00F10, qvdQvnQvmConstraints, InstName.VaddF, T.VaddFT1, IsaVersion.v80, InstFlags.None),
                new(0xEF100D00, 0xFFB00F10, qvdQvnQvmConstraints, InstName.VaddF, T.VaddFT1, IsaVersion.v80, IsaFeature.FeatFp16, InstFlags.None),
                new(0xEE300A00, 0xFFB00E50, InstName.VaddF, T.VaddFT2, IsaVersion.v80, InstFlags.None),
                new(0xEE300900, 0xFFB00F50, InstName.VaddF, T.VaddFT2, IsaVersion.v80, IsaFeature.FeatFp16, InstFlags.None),
                new(0xEF000800, 0xFF800F10, qvdQvnQvmConstraints, InstName.VaddI, T.VaddIT1, IsaVersion.v80, InstFlags.None),
                new(0xEF000110, 0xFFB00F10, qvdQvnQvmConstraints, InstName.VandR, T.VandRT1, IsaVersion.v80, InstFlags.None),
                new(0xEF800130, 0xEFB809B0, cmodeCmodeQvdConstraints, InstName.VbicI, T.VbicIT1, IsaVersion.v80, InstFlags.None),
                new(0xEF800930, 0xEFB80DB0, cmodeCmodeQvdConstraints, InstName.VbicI, T.VbicIT2, IsaVersion.v80, InstFlags.None),
                new(0xEF100110, 0xFFB00F10, qvdQvnQvmConstraints, InstName.VbicR, T.VbicRT1, IsaVersion.v80, InstFlags.None),
                new(0xFF300110, 0xFFB00F10, qvdQvnQvmOpConstraints, InstName.Vbif, T.VbifT1, IsaVersion.v80, InstFlags.None),
                new(0xFF200110, 0xFFB00F10, qvdQvnQvmOpConstraints, InstName.Vbit, T.VbitT1, IsaVersion.v80, InstFlags.None),
                new(0xFF100110, 0xFFB00F10, qvdQvnQvmOpConstraints, InstName.Vbsl, T.VbslT1, IsaVersion.v80, InstFlags.None),
                new(0xFC900800, 0xFEB00F10, qvdQvnQvmConstraints, InstName.Vcadd, T.VcaddT1, IsaVersion.v83, IsaFeature.FeatFcma, InstFlags.None),
                new(0xFC800800, 0xFEB00F10, qvdQvnQvmConstraints, InstName.Vcadd, T.VcaddT1, IsaVersion.v83, IsaFeature.FeatFcma | IsaFeature.FeatFp16, InstFlags.None),
                new(0xFFB10100, 0xFFB30F90, sizeQvdQvmConstraints, InstName.VceqI, T.VceqIT1, IsaVersion.v80, InstFlags.None),
                new(0xFFB90500, 0xFFBB0F90, sizeQvdQvmConstraints, InstName.VceqI, T.VceqIT1, IsaVersion.v80, InstFlags.None),
                new(0xFFB50500, 0xFFBF0F90, sizeQvdQvmConstraints, InstName.VceqI, T.VceqIT1, IsaVersion.v80, IsaFeature.FeatFp16, InstFlags.None),
                new(0xFF000810, 0xFF800F10, qvdQvnQvmSizeConstraints, InstName.VceqR, T.VceqRT1, IsaVersion.v80, InstFlags.None),
                new(0xEF000E00, 0xFFB00F10, qvdQvnQvmConstraints, InstName.VceqR, T.VceqRT2, IsaVersion.v80, InstFlags.None),
                new(0xEF100E00, 0xFFB00F10, qvdQvnQvmConstraints, InstName.VceqR, T.VceqRT2, IsaVersion.v80, IsaFeature.FeatFp16, InstFlags.None),
                new(0xFFB10080, 0xFFB30F90, sizeQvdQvmConstraints, InstName.VcgeI, T.VcgeIT1, IsaVersion.v80, InstFlags.None),
                new(0xFFB90480, 0xFFBB0F90, sizeQvdQvmConstraints, InstName.VcgeI, T.VcgeIT1, IsaVersion.v80, InstFlags.None),
                new(0xFFB50480, 0xFFBF0F90, sizeQvdQvmConstraints, InstName.VcgeI, T.VcgeIT1, IsaVersion.v80, IsaFeature.FeatFp16, InstFlags.None),
                new(0xEF000310, 0xEF800F10, qvdQvnQvmSizeConstraints, InstName.VcgeR, T.VcgeRT1, IsaVersion.v80, InstFlags.None),
                new(0xFF000E00, 0xFFB00F10, qvdQvnQvmConstraints, InstName.VcgeR, T.VcgeRT2, IsaVersion.v80, InstFlags.None),
                new(0xFF100E00, 0xFFB00F10, qvdQvnQvmConstraints, InstName.VcgeR, T.VcgeRT2, IsaVersion.v80, IsaFeature.FeatFp16, InstFlags.None),
                new(0xFFB10000, 0xFFB30F90, sizeQvdQvmConstraints, InstName.VcgtI, T.VcgtIT1, IsaVersion.v80, InstFlags.None),
                new(0xFFB90400, 0xFFBB0F90, sizeQvdQvmConstraints, InstName.VcgtI, T.VcgtIT1, IsaVersion.v80, InstFlags.None),
                new(0xFFB50400, 0xFFBF0F90, sizeQvdQvmConstraints, InstName.VcgtI, T.VcgtIT1, IsaVersion.v80, IsaFeature.FeatFp16, InstFlags.None),
                new(0xEF000300, 0xEF800F10, qvdQvnQvmSizeConstraints, InstName.VcgtR, T.VcgtRT1, IsaVersion.v80, InstFlags.None),
                new(0xFF200E00, 0xFFB00F10, qvdQvnQvmConstraints, InstName.VcgtR, T.VcgtRT2, IsaVersion.v80, InstFlags.None),
                new(0xFF300E00, 0xFFB00F10, qvdQvnQvmConstraints, InstName.VcgtR, T.VcgtRT2, IsaVersion.v80, IsaFeature.FeatFp16, InstFlags.None),
                new(0xFFB10180, 0xFFB30F90, sizeQvdQvmConstraints, InstName.VcleI, T.VcleIT1, IsaVersion.v80, InstFlags.None),
                new(0xFFB90580, 0xFFBB0F90, sizeQvdQvmConstraints, InstName.VcleI, T.VcleIT1, IsaVersion.v80, InstFlags.None),
                new(0xFFB50580, 0xFFBF0F90, sizeQvdQvmConstraints, InstName.VcleI, T.VcleIT1, IsaVersion.v80, IsaFeature.FeatFp16, InstFlags.None),
                new(0xFFB00400, 0xFFB30F90, sizeQvdQvmConstraints, InstName.Vcls, T.VclsT1, IsaVersion.v80, InstFlags.None),
                new(0xFFB10200, 0xFFB30F90, sizeQvdQvmConstraints, InstName.VcltI, T.VcltIT1, IsaVersion.v80, InstFlags.None),
                new(0xFFB90600, 0xFFBB0F90, sizeQvdQvmConstraints, InstName.VcltI, T.VcltIT1, IsaVersion.v80, InstFlags.None),
                new(0xFFB50600, 0xFFBF0F90, sizeQvdQvmConstraints, InstName.VcltI, T.VcltIT1, IsaVersion.v80, IsaFeature.FeatFp16, InstFlags.None),
                new(0xFFB00480, 0xFFB30F90, sizeQvdQvmConstraints, InstName.Vclz, T.VclzT1, IsaVersion.v80, InstFlags.None),
                new(0xFC200800, 0xFE200F10, qvdQvnQvmConstraints, InstName.Vcmla, T.VcmlaT1, IsaVersion.v83, IsaFeature.FeatFcma, InstFlags.None),
                new(0xFE000800, 0xFF000F10, qvdQvnConstraints, InstName.VcmlaS, T.VcmlaST1, IsaVersion.v83, IsaFeature.FeatFcma, InstFlags.None),
                new(0xEEB40A40, 0xFFBF0ED0, InstName.Vcmp, T.VcmpT1, IsaVersion.v80, InstFlags.None),
                new(0xEEB40940, 0xFFBF0FD0, InstName.Vcmp, T.VcmpT1, IsaVersion.v80, IsaFeature.FeatFp16, InstFlags.None),
                new(0xEEB50A40, 0xFFBF0EFF, InstName.Vcmp, T.VcmpT2, IsaVersion.v80, InstFlags.None),
                new(0xEEB50940, 0xFFBF0FFF, InstName.Vcmp, T.VcmpT2, IsaVersion.v80, IsaFeature.FeatFp16, InstFlags.None),
                new(0xEEB40AC0, 0xFFBF0ED0, InstName.Vcmpe, T.VcmpeT1, IsaVersion.v80, InstFlags.None),
                new(0xEEB409C0, 0xFFBF0FD0, InstName.Vcmpe, T.VcmpeT1, IsaVersion.v80, IsaFeature.FeatFp16, InstFlags.None),
                new(0xEEB50AC0, 0xFFBF0EFF, InstName.Vcmpe, T.VcmpeT2, IsaVersion.v80, InstFlags.None),
                new(0xEEB509C0, 0xFFBF0FFF, InstName.Vcmpe, T.VcmpeT2, IsaVersion.v80, IsaFeature.FeatFp16, InstFlags.None),
                new(0xFFB00500, 0xFFBF0F90, qvdQvmConstraints, InstName.Vcnt, T.VcntT1, IsaVersion.v80, InstFlags.None),
                new(0xFFBB0000, 0xFFBF0F10, qvdQvmConstraints, InstName.VcvtaAsimd, T.VcvtaAsimdT1, IsaVersion.v80, InstFlags.None),
                new(0xFFB70000, 0xFFBF0F10, qvdQvmConstraints, InstName.VcvtaAsimd, T.VcvtaAsimdT1, IsaVersion.v80, IsaFeature.FeatFp16, InstFlags.None),
                new(0xFEBC0A40, 0xFFBF0E50, sizeConstraints, InstName.VcvtaVfp, T.VcvtaVfpT1, IsaVersion.v80, InstFlags.None),
                new(0xFEBC0940, 0xFFBF0F50, sizeConstraints, InstName.VcvtaVfp, T.VcvtaVfpT1, IsaVersion.v80, IsaFeature.FeatFp16, InstFlags.None),
                new(0xEEB20A40, 0xFFBE0ED0, InstName.Vcvtb, T.VcvtbT1, IsaVersion.v80, InstFlags.None),
                new(0xEEB30940, 0xFFBF0FD0, InstName.VcvtbBfs, T.VcvtbBfsT1, IsaVersion.v86, IsaFeature.FeatAa32bf16, InstFlags.None),
                new(0xFFBB0300, 0xFFBF0F10, qvdQvmConstraints, InstName.VcvtmAsimd, T.VcvtmAsimdT1, IsaVersion.v80, InstFlags.None),
                new(0xFFB70300, 0xFFBF0F10, qvdQvmConstraints, InstName.VcvtmAsimd, T.VcvtmAsimdT1, IsaVersion.v80, IsaFeature.FeatFp16, InstFlags.None),
                new(0xFEBF0A40, 0xFFBF0E50, sizeConstraints, InstName.VcvtmVfp, T.VcvtmVfpT1, IsaVersion.v80, InstFlags.None),
                new(0xFEBF0940, 0xFFBF0F50, sizeConstraints, InstName.VcvtmVfp, T.VcvtmVfpT1, IsaVersion.v80, IsaFeature.FeatFp16, InstFlags.None),
                new(0xFFBB0100, 0xFFBF0F10, qvdQvmConstraints, InstName.VcvtnAsimd, T.VcvtnAsimdT1, IsaVersion.v80, InstFlags.None),
                new(0xFFB70100, 0xFFBF0F10, qvdQvmConstraints, InstName.VcvtnAsimd, T.VcvtnAsimdT1, IsaVersion.v80, IsaFeature.FeatFp16, InstFlags.None),
                new(0xFEBD0A40, 0xFFBF0E50, sizeConstraints, InstName.VcvtnVfp, T.VcvtnVfpT1, IsaVersion.v80, InstFlags.None),
                new(0xFEBD0940, 0xFFBF0F50, sizeConstraints, InstName.VcvtnVfp, T.VcvtnVfpT1, IsaVersion.v80, IsaFeature.FeatFp16, InstFlags.None),
                new(0xFFBB0200, 0xFFBF0F10, qvdQvmConstraints, InstName.VcvtpAsimd, T.VcvtpAsimdT1, IsaVersion.v80, InstFlags.None),
                new(0xFFB70200, 0xFFBF0F10, qvdQvmConstraints, InstName.VcvtpAsimd, T.VcvtpAsimdT1, IsaVersion.v80, IsaFeature.FeatFp16, InstFlags.None),
                new(0xFEBE0A40, 0xFFBF0E50, sizeConstraints, InstName.VcvtpVfp, T.VcvtpVfpT1, IsaVersion.v80, InstFlags.None),
                new(0xFEBE0940, 0xFFBF0F50, sizeConstraints, InstName.VcvtpVfp, T.VcvtpVfpT1, IsaVersion.v80, IsaFeature.FeatFp16, InstFlags.None),
                new(0xEEBC0A40, 0xFFBE0ED0, InstName.VcvtrIv, T.VcvtrIvT1, IsaVersion.v80, InstFlags.None),
                new(0xEEBC0940, 0xFFBE0FD0, InstName.VcvtrIv, T.VcvtrIvT1, IsaVersion.v80, IsaFeature.FeatFp16, InstFlags.None),
                new(0xEEB20AC0, 0xFFBE0ED0, InstName.Vcvtt, T.VcvttT1, IsaVersion.v80, InstFlags.None),
                new(0xEEB309C0, 0xFFBF0FD0, InstName.VcvttBfs, T.VcvttBfsT1, IsaVersion.v86, IsaFeature.FeatAa32bf16, InstFlags.None),
                new(0xFFB60640, 0xFFBF0FD0, vmConstraints, InstName.VcvtBfs, T.VcvtBfsT1, IsaVersion.v86, IsaFeature.FeatAa32bf16, InstFlags.None),
                new(0xEEB70AC0, 0xFFBF0ED0, InstName.VcvtDs, T.VcvtDsT1, IsaVersion.v80, InstFlags.None),
                new(0xFFB60600, 0xFFBF0ED0, opvdOpvmConstraints, InstName.VcvtHs, T.VcvtHsT1, IsaVersion.v80, InstFlags.None),
                new(0xFFBB0600, 0xFFBF0E10, qvdQvmConstraints, InstName.VcvtIs, T.VcvtIsT1, IsaVersion.v80, InstFlags.None),
                new(0xFFB70600, 0xFFBF0E10, qvdQvmConstraints, InstName.VcvtIs, T.VcvtIsT1, IsaVersion.v80, IsaFeature.FeatFp16, InstFlags.None),
                new(0xEEBC0AC0, 0xFFBE0ED0, InstName.VcvtIv, T.VcvtIvT1, IsaVersion.v80, InstFlags.None),
                new(0xEEBC09C0, 0xFFBE0FD0, InstName.VcvtIv, T.VcvtIvT1, IsaVersion.v80, IsaFeature.FeatFp16, InstFlags.None),
                new(0xEEB80A40, 0xFFBF0E50, InstName.VcvtVi, T.VcvtViT1, IsaVersion.v80, InstFlags.None),
                new(0xEEB80940, 0xFFBF0F50, InstName.VcvtVi, T.VcvtViT1, IsaVersion.v80, IsaFeature.FeatFp16, InstFlags.None),
                new(0xEF800E10, 0xEF800E90, imm6Opimm6Imm6QvdQvmConstraints, InstName.VcvtXs, T.VcvtXsT1, IsaVersion.v80, InstFlags.None),
                new(0xEF800C10, 0xEF800E90, imm6Opimm6Imm6QvdQvmConstraints, InstName.VcvtXs, T.VcvtXsT1, IsaVersion.v80, IsaFeature.FeatFp16, InstFlags.None),
                new(0xEEBA0A40, 0xFFBA0E50, InstName.VcvtXv, T.VcvtXvT1, IsaVersion.v80, InstFlags.None),
                new(0xEEBA0940, 0xFFBA0F50, InstName.VcvtXv, T.VcvtXvT1, IsaVersion.v80, IsaFeature.FeatFp16, InstFlags.None),
                new(0xEE800A00, 0xFFB00E50, InstName.Vdiv, T.VdivT1, IsaVersion.v80, InstFlags.None),
                new(0xEE800900, 0xFFB00F50, InstName.Vdiv, T.VdivT1, IsaVersion.v80, IsaFeature.FeatFp16, InstFlags.None),
                new(0xFC000D00, 0xFFB00F10, qvdQvnQvmConstraints, InstName.Vdot, T.VdotT1, IsaVersion.v86, IsaFeature.FeatAa32bf16, InstFlags.None),
                new(0xFE000D00, 0xFFB00F10, qvdQvnConstraints, InstName.VdotS, T.VdotST1, IsaVersion.v86, IsaFeature.FeatAa32bf16, InstFlags.None),
                new(0xEE800B10, 0xFF900F5F, qvdEbConstraints, InstName.VdupR, T.VdupRT1, IsaVersion.v80, InstFlags.RtRead),
                new(0xFFB00C00, 0xFFB00F90, imm4QvdConstraints, InstName.VdupS, T.VdupST1, IsaVersion.v80, InstFlags.None),
                new(0xFF000110, 0xFFB00F10, qvdQvnQvmConstraints, InstName.Veor, T.VeorT1, IsaVersion.v80, InstFlags.None),
                new(0xEFB00000, 0xFFB00010, qvdQvnQvmQimm4Constraints, InstName.Vext, T.VextT1, IsaVersion.v80, InstFlags.None),
                new(0xEF000C10, 0xFFB00F10, qvdQvnQvmConstraints, InstName.Vfma, T.VfmaT1, IsaVersion.v80, InstFlags.None),
                new(0xEF100C10, 0xFFB00F10, qvdQvnQvmConstraints, InstName.Vfma, T.VfmaT1, IsaVersion.v80, IsaFeature.FeatFp16, InstFlags.None),
                new(0xEEA00A00, 0xFFB00E50, InstName.Vfma, T.VfmaT2, IsaVersion.v80, InstFlags.None),
                new(0xEEA00900, 0xFFB00F50, InstName.Vfma, T.VfmaT2, IsaVersion.v80, IsaFeature.FeatFp16, InstFlags.None),
                new(0xFC200810, 0xFFB00F10, qvdConstraints, InstName.Vfmal, T.VfmalT1, IsaVersion.v82, IsaFeature.FeatFhm, InstFlags.None),
                new(0xFE000810, 0xFFB00F10, qvdConstraints, InstName.VfmalS, T.VfmalST1, IsaVersion.v82, IsaFeature.FeatFhm, InstFlags.None),
                new(0xFC300810, 0xFFB00F10, vdVnVmConstraints, InstName.VfmaBf, T.VfmaBfT1, IsaVersion.v86, IsaFeature.FeatAa32bf16, InstFlags.None),
                new(0xFE300810, 0xFFB00F10, vdVnConstraints, InstName.VfmaBfs, T.VfmaBfsT1, IsaVersion.v86, IsaFeature.FeatAa32bf16, InstFlags.None),
                new(0xEF200C10, 0xFFB00F10, qvdQvnQvmConstraints, InstName.Vfms, T.VfmsT1, IsaVersion.v80, InstFlags.None),
                new(0xEF300C10, 0xFFB00F10, qvdQvnQvmConstraints, InstName.Vfms, T.VfmsT1, IsaVersion.v80, IsaFeature.FeatFp16, InstFlags.None),
                new(0xEEA00A40, 0xFFB00E50, InstName.Vfms, T.VfmsT2, IsaVersion.v80, InstFlags.None),
                new(0xEEA00940, 0xFFB00F50, InstName.Vfms, T.VfmsT2, IsaVersion.v80, IsaFeature.FeatFp16, InstFlags.None),
                new(0xFCA00810, 0xFFB00F10, qvdConstraints, InstName.Vfmsl, T.VfmslT1, IsaVersion.v82, IsaFeature.FeatFhm, InstFlags.None),
                new(0xFE100810, 0xFFB00F10, qvdConstraints, InstName.VfmslS, T.VfmslST1, IsaVersion.v82, IsaFeature.FeatFhm, InstFlags.None),
                new(0xEE900A40, 0xFFB00E50, InstName.Vfnma, T.VfnmaT1, IsaVersion.v80, InstFlags.None),
                new(0xEE900940, 0xFFB00F50, InstName.Vfnma, T.VfnmaT1, IsaVersion.v80, IsaFeature.FeatFp16, InstFlags.None),
                new(0xEE900A00, 0xFFB00E50, InstName.Vfnms, T.VfnmsT1, IsaVersion.v80, InstFlags.None),
                new(0xEE900900, 0xFFB00F50, InstName.Vfnms, T.VfnmsT1, IsaVersion.v80, IsaFeature.FeatFp16, InstFlags.None),
                new(0xEF000000, 0xEF800F10, qvdQvnQvmSizeConstraints, InstName.Vhadd, T.VhaddT1, IsaVersion.v80, InstFlags.None),
                new(0xEF000200, 0xEF800F10, qvdQvnQvmSizeConstraints, InstName.Vhsub, T.VhsubT1, IsaVersion.v80, InstFlags.None),
                new(0xFEB00AC0, 0xFFBF0FD0, InstName.Vins, T.VinsT1, IsaVersion.v82, IsaFeature.FeatFp16, InstFlags.None),
                new(0xEEB90BC0, 0xFFBF0FD0, InstName.Vjcvt, T.VjcvtT1, IsaVersion.v83, IsaFeature.FeatJscvt, InstFlags.None),
                new(0xF9A00000, 0xFFB00F10, sizeConstraints2, InstName.Vld11, T.Vld11T1, IsaVersion.v80, InstFlags.None),
                new(0xF9A00400, 0xFFB00F20, sizeConstraints2, InstName.Vld11, T.Vld11T2, IsaVersion.v80, InstFlags.None),
                new(0xF9A00800, 0xFFB00F40, sizeIndexAlignIndexAlignConstraints, InstName.Vld11, T.Vld11T3, IsaVersion.v80, InstFlags.None),
                new(0xF9A00C00, 0xFFB00F00, sizeSizeaConstraints, InstName.Vld1A, T.Vld1AT1, IsaVersion.v80, InstFlags.None),
                new(0xF9200700, 0xFFB00F00, alignConstraints, InstName.Vld1M, T.Vld1MT1, IsaVersion.v80, InstFlags.None),
                new(0xF9200A00, 0xFFB00F00, alignConstraints2, InstName.Vld1M, T.Vld1MT2, IsaVersion.v80, InstFlags.None),
                new(0xF9200600, 0xFFB00F00, alignConstraints, InstName.Vld1M, T.Vld1MT3, IsaVersion.v80, InstFlags.None),
                new(0xF9200200, 0xFFB00F00, InstName.Vld1M, T.Vld1MT4, IsaVersion.v80, InstFlags.None),
                new(0xF9A00100, 0xFFB00F00, sizeConstraints2, InstName.Vld21, T.Vld21T1, IsaVersion.v80, InstFlags.None),
                new(0xF9A00500, 0xFFB00F00, sizeConstraints2, InstName.Vld21, T.Vld21T2, IsaVersion.v80, InstFlags.None),
                new(0xF9A00900, 0xFFB00F20, sizeConstraints2, InstName.Vld21, T.Vld21T3, IsaVersion.v80, InstFlags.None),
                new(0xF9A00D00, 0xFFB00F00, sizeConstraints3, InstName.Vld2A, T.Vld2AT1, IsaVersion.v80, InstFlags.None),
                new(0xF9200800, 0xFFB00E00, alignSizeConstraints, InstName.Vld2M, T.Vld2MT1, IsaVersion.v80, InstFlags.None),
                new(0xF9200300, 0xFFB00F00, sizeConstraints3, InstName.Vld2M, T.Vld2MT2, IsaVersion.v80, InstFlags.None),
                new(0xF9A00200, 0xFFB00F10, sizeConstraints2, InstName.Vld31, T.Vld31T1, IsaVersion.v80, InstFlags.None),
                new(0xF9A00600, 0xFFB00F10, sizeConstraints2, InstName.Vld31, T.Vld31T2, IsaVersion.v80, InstFlags.None),
                new(0xF9A00A00, 0xFFB00F30, sizeConstraints2, InstName.Vld31, T.Vld31T3, IsaVersion.v80, InstFlags.None),
                new(0xF9A00E00, 0xFFB00F10, sizeAConstraints, InstName.Vld3A, T.Vld3AT1, IsaVersion.v80, InstFlags.None),
                new(0xF9200400, 0xFFB00E00, sizeAlignConstraints, InstName.Vld3M, T.Vld3MT1, IsaVersion.v80, InstFlags.None),
                new(0xF9A00300, 0xFFB00F00, sizeConstraints2, InstName.Vld41, T.Vld41T1, IsaVersion.v80, InstFlags.None),
                new(0xF9A00700, 0xFFB00F00, sizeConstraints2, InstName.Vld41, T.Vld41T2, IsaVersion.v80, InstFlags.None),
                new(0xF9A00B00, 0xFFB00F00, sizeIndexAlignConstraints, InstName.Vld41, T.Vld41T3, IsaVersion.v80, InstFlags.None),
                new(0xF9A00F00, 0xFFB00F00, sizeaConstraints, InstName.Vld4A, T.Vld4AT1, IsaVersion.v80, InstFlags.None),
                new(0xF9200000, 0xFFB00E00, sizeConstraints3, InstName.Vld4M, T.Vld4MT1, IsaVersion.v80, InstFlags.None),
                new(0xEC100B00, 0xFE100F01, puwPwPuwPuwConstraints, InstName.Vldm, T.VldmT1, IsaVersion.v80, InstFlags.WBack),
                new(0xEC100A00, 0xFE100F00, puwPwPuwPuwConstraints, InstName.Vldm, T.VldmT2, IsaVersion.v80, InstFlags.WBack),
                new(0xED100A00, 0xFF300E00, rnConstraints, InstName.VldrI, T.VldrIT1, IsaVersion.v80, InstFlags.None),
                new(0xED100900, 0xFF300F00, rnConstraints, InstName.VldrI, T.VldrIT1, IsaVersion.v80, IsaFeature.FeatFp16, InstFlags.None),
                new(0xED1F0A00, 0xFF3F0E00, InstName.VldrL, T.VldrLT1, IsaVersion.v80, InstFlags.None),
                new(0xED1F0900, 0xFF3F0F00, InstName.VldrL, T.VldrLT1, IsaVersion.v80, IsaFeature.FeatFp16, InstFlags.None),
                new(0xFF000F10, 0xFFB00F10, qvdQvnQvmConstraints, InstName.Vmaxnm, T.VmaxnmT1, IsaVersion.v80, InstFlags.None),
                new(0xFF100F10, 0xFFB00F10, qvdQvnQvmConstraints, InstName.Vmaxnm, T.VmaxnmT1, IsaVersion.v80, IsaFeature.FeatFp16, InstFlags.None),
                new(0xFE800A00, 0xFFB00E50, sizeConstraints, InstName.Vmaxnm, T.VmaxnmT2, IsaVersion.v80, InstFlags.None),
                new(0xFE800900, 0xFFB00F50, sizeConstraints, InstName.Vmaxnm, T.VmaxnmT2, IsaVersion.v80, IsaFeature.FeatFp16, InstFlags.None),
                new(0xEF000F00, 0xFFB00F10, qvdQvnQvmConstraints, InstName.VmaxF, T.VmaxFT1, IsaVersion.v80, InstFlags.None),
                new(0xEF100F00, 0xFFB00F10, qvdQvnQvmConstraints, InstName.VmaxF, T.VmaxFT1, IsaVersion.v80, IsaFeature.FeatFp16, InstFlags.None),
                new(0xEF000600, 0xEF800F10, qvdQvnQvmSizeConstraints, InstName.VmaxI, T.VmaxIT1, IsaVersion.v80, InstFlags.None),
                new(0xFF200F10, 0xFFB00F10, qvdQvnQvmConstraints, InstName.Vminnm, T.VminnmT1, IsaVersion.v80, InstFlags.None),
                new(0xFF300F10, 0xFFB00F10, qvdQvnQvmConstraints, InstName.Vminnm, T.VminnmT1, IsaVersion.v80, IsaFeature.FeatFp16, InstFlags.None),
                new(0xFE800A40, 0xFFB00E50, sizeConstraints, InstName.Vminnm, T.VminnmT2, IsaVersion.v80, InstFlags.None),
                new(0xFE800940, 0xFFB00F50, sizeConstraints, InstName.Vminnm, T.VminnmT2, IsaVersion.v80, IsaFeature.FeatFp16, InstFlags.None),
                new(0xEF200F00, 0xFFB00F10, qvdQvnQvmConstraints, InstName.VminF, T.VminFT1, IsaVersion.v80, InstFlags.None),
                new(0xEF300F00, 0xFFB00F10, qvdQvnQvmConstraints, InstName.VminF, T.VminFT1, IsaVersion.v80, IsaFeature.FeatFp16, InstFlags.None),
                new(0xEF000610, 0xEF800F10, qvdQvnQvmSizeConstraints, InstName.VminI, T.VminIT1, IsaVersion.v80, InstFlags.None),
                new(0xEF800800, 0xEF800F50, sizeVdConstraints, InstName.VmlalI, T.VmlalIT1, IsaVersion.v80, InstFlags.None),
                new(0xEF800240, 0xEF800F50, sizeSizeVdConstraints, InstName.VmlalS, T.VmlalST1, IsaVersion.v80, InstFlags.None),
                new(0xEF000D10, 0xFFB00F10, qvdQvnQvmConstraints, InstName.VmlaF, T.VmlaFT1, IsaVersion.v80, InstFlags.None),
                new(0xEF100D10, 0xFFB00F10, qvdQvnQvmConstraints, InstName.VmlaF, T.VmlaFT1, IsaVersion.v80, IsaFeature.FeatFp16, InstFlags.None),
                new(0xEE000A00, 0xFFB00E50, InstName.VmlaF, T.VmlaFT2, IsaVersion.v80, InstFlags.None),
                new(0xEE000900, 0xFFB00F50, InstName.VmlaF, T.VmlaFT2, IsaVersion.v80, IsaFeature.FeatFp16, InstFlags.None),
                new(0xEF000900, 0xFF800F10, sizeQvdQvnQvmConstraints, InstName.VmlaI, T.VmlaIT1, IsaVersion.v80, InstFlags.None),
                new(0xEFA00040, 0xEFA00E50, sizeQvdQvnConstraints, InstName.VmlaS, T.VmlaST1, IsaVersion.v80, InstFlags.None),
                new(0xEF900040, 0xEFB00F50, sizeQvdQvnConstraints, InstName.VmlaS, T.VmlaST1, IsaVersion.v80, InstFlags.None),
                new(0xEF900140, 0xEFB00F50, sizeQvdQvnConstraints, InstName.VmlaS, T.VmlaST1, IsaVersion.v80, IsaFeature.FeatFp16, InstFlags.None),
                new(0xEF800A00, 0xEF800F50, sizeVdConstraints, InstName.VmlslI, T.VmlslIT1, IsaVersion.v80, InstFlags.None),
                new(0xEF800640, 0xEF800F50, sizeSizeVdConstraints, InstName.VmlslS, T.VmlslST1, IsaVersion.v80, InstFlags.None),
                new(0xEF200D10, 0xFFB00F10, qvdQvnQvmConstraints, InstName.VmlsF, T.VmlsFT1, IsaVersion.v80, InstFlags.None),
                new(0xEF300D10, 0xFFB00F10, qvdQvnQvmConstraints, InstName.VmlsF, T.VmlsFT1, IsaVersion.v80, IsaFeature.FeatFp16, InstFlags.None),
                new(0xEE000A40, 0xFFB00E50, InstName.VmlsF, T.VmlsFT2, IsaVersion.v80, InstFlags.None),
                new(0xEE000940, 0xFFB00F50, InstName.VmlsF, T.VmlsFT2, IsaVersion.v80, IsaFeature.FeatFp16, InstFlags.None),
                new(0xFF000900, 0xFF800F10, sizeQvdQvnQvmConstraints, InstName.VmlsI, T.VmlsIT1, IsaVersion.v80, InstFlags.None),
                new(0xEFA00440, 0xEFA00E50, sizeQvdQvnConstraints, InstName.VmlsS, T.VmlsST1, IsaVersion.v80, InstFlags.None),
                new(0xEF900440, 0xEFB00F50, sizeQvdQvnConstraints, InstName.VmlsS, T.VmlsST1, IsaVersion.v80, InstFlags.None),
                new(0xEF900540, 0xEFB00F50, sizeQvdQvnConstraints, InstName.VmlsS, T.VmlsST1, IsaVersion.v80, IsaFeature.FeatFp16, InstFlags.None),
                new(0xFC000C40, 0xFFB00F50, vdVnVmConstraints, InstName.Vmmla, T.VmmlaT1, IsaVersion.v86, IsaFeature.FeatAa32bf16, InstFlags.None),
                new(0xEF800A10, 0xEF870FD0, imm3hImm3hImm3hImm3hImm3hVdConstraints, InstName.Vmovl, T.VmovlT1, IsaVersion.v80, InstFlags.None),
                new(0xFFB20200, 0xFFB30FD0, sizeVmConstraints, InstName.Vmovn, T.VmovnT1, IsaVersion.v80, InstFlags.None),
                new(0xFEB00A40, 0xFFBF0FD0, InstName.Vmovx, T.VmovxT1, IsaVersion.v82, IsaFeature.FeatFp16, InstFlags.None),
                new(0xEC400B10, 0xFFE00FD0, InstName.VmovD, T.VmovDT1, IsaVersion.v80, InstFlags.Rt2Read),
                new(0xEE000910, 0xFFE00F7F, InstName.VmovH, T.VmovHT1, IsaVersion.v82, IsaFeature.FeatFp16, InstFlags.Rt),
                new(0xEF800010, 0xEFB809B0, qvdConstraints, InstName.VmovI, T.VmovIT1, IsaVersion.v80, InstFlags.None),
                new(0xEEB00A00, 0xFFB00EF0, InstName.VmovI, T.VmovIT2, IsaVersion.v80, InstFlags.None),
                new(0xEEB00900, 0xFFB00FF0, InstName.VmovI, T.VmovIT2, IsaVersion.v80, IsaFeature.FeatFp16, InstFlags.None),
                new(0xEF800810, 0xEFB80DB0, qvdConstraints, InstName.VmovI, T.VmovIT3, IsaVersion.v80, InstFlags.None),
                new(0xEF800C10, 0xEFB80CB0, qvdConstraints, InstName.VmovI, T.VmovIT4, IsaVersion.v80, InstFlags.None),
                new(0xEF800E30, 0xEFB80FB0, qvdConstraints, InstName.VmovI, T.VmovIT5, IsaVersion.v80, InstFlags.None),
                new(0xEEB00A40, 0xFFBF0ED0, InstName.VmovR, T.VmovRT2, IsaVersion.v80, InstFlags.None),
                new(0xEE000B10, 0xFF900F1F, opc1opc2Constraints, InstName.VmovRs, T.VmovRsT1, IsaVersion.v80, InstFlags.RtRead),
                new(0xEE000A10, 0xFFE00F7F, InstName.VmovS, T.VmovST1, IsaVersion.v80, InstFlags.RtRead),
                new(0xEE100B10, 0xFF100F1F, uopc1opc2Uopc1opc2Constraints, InstName.VmovSr, T.VmovSrT1, IsaVersion.v80, InstFlags.Rt),
                new(0xEC400A10, 0xFFE00FD0, InstName.VmovSs, T.VmovSsT1, IsaVersion.v80, InstFlags.Rt2Read),
                new(0xEEF00A10, 0xFFF00FFF, InstName.Vmrs, T.VmrsT1, IsaVersion.v80, InstFlags.Rt),
                new(0xEEE00A10, 0xFFF00FFF, InstName.Vmsr, T.VmsrT1, IsaVersion.v80, InstFlags.RtRead),
                new(0xEF800C00, 0xEF800D50, sizeOpuOpsizeVdConstraints, InstName.VmullI, T.VmullIT1, IsaVersion.v80, InstFlags.None),
                new(0xEF800A40, 0xEF800F50, sizeSizeVdConstraints, InstName.VmullS, T.VmullST1, IsaVersion.v80, InstFlags.None),
                new(0xFF000D10, 0xFFB00F10, qvdQvnQvmConstraints, InstName.VmulF, T.VmulFT1, IsaVersion.v80, InstFlags.None),
                new(0xFF100D10, 0xFFB00F10, qvdQvnQvmConstraints, InstName.VmulF, T.VmulFT1, IsaVersion.v80, IsaFeature.FeatFp16, InstFlags.None),
                new(0xEE200A00, 0xFFB00E50, InstName.VmulF, T.VmulFT2, IsaVersion.v80, InstFlags.None),
                new(0xEE200900, 0xFFB00F50, InstName.VmulF, T.VmulFT2, IsaVersion.v80, IsaFeature.FeatFp16, InstFlags.None),
                new(0xEF000910, 0xEF800F10, sizeOpsizeOpsizeQvdQvnQvmConstraints, InstName.VmulI, T.VmulIT1, IsaVersion.v80, InstFlags.None),
                new(0xEFA00840, 0xEFA00E50, sizeQvdQvnConstraints, InstName.VmulS, T.VmulST1, IsaVersion.v80, InstFlags.None),
                new(0xEF900840, 0xEFB00F50, sizeQvdQvnConstraints, InstName.VmulS, T.VmulST1, IsaVersion.v80, InstFlags.None),
                new(0xEF900940, 0xEFB00F50, sizeQvdQvnConstraints, InstName.VmulS, T.VmulST1, IsaVersion.v80, IsaFeature.FeatFp16, InstFlags.None),
                new(0xEF800030, 0xEFB809B0, cmodeQvdConstraints, InstName.VmvnI, T.VmvnIT1, IsaVersion.v80, InstFlags.None),
                new(0xEF800830, 0xEFB80DB0, cmodeQvdConstraints, InstName.VmvnI, T.VmvnIT2, IsaVersion.v80, InstFlags.None),
                new(0xEF800C30, 0xEFB80EB0, cmodeQvdConstraints, InstName.VmvnI, T.VmvnIT3, IsaVersion.v80, InstFlags.None),
                new(0xFFB00580, 0xFFBF0F90, qvdQvmConstraints, InstName.VmvnR, T.VmvnRT1, IsaVersion.v80, InstFlags.None),
                new(0xFFB10380, 0xFFB30F90, sizeQvdQvmConstraints, InstName.Vneg, T.VnegT1, IsaVersion.v80, InstFlags.None),
                new(0xFFB90780, 0xFFBB0F90, sizeQvdQvmConstraints, InstName.Vneg, T.VnegT1, IsaVersion.v80, InstFlags.None),
                new(0xFFB50780, 0xFFBF0F90, sizeQvdQvmConstraints, InstName.Vneg, T.VnegT1, IsaVersion.v80, IsaFeature.FeatFp16, InstFlags.None),
                new(0xEEB10A40, 0xFFBF0ED0, InstName.Vneg, T.VnegT2, IsaVersion.v80, InstFlags.None),
                new(0xEEB10940, 0xFFBF0FD0, InstName.Vneg, T.VnegT2, IsaVersion.v80, IsaFeature.FeatFp16, InstFlags.None),
                new(0xEE100A40, 0xFFB00E50, InstName.Vnmla, T.VnmlaT1, IsaVersion.v80, InstFlags.None),
                new(0xEE100940, 0xFFB00F50, InstName.Vnmla, T.VnmlaT1, IsaVersion.v80, IsaFeature.FeatFp16, InstFlags.None),
                new(0xEE100A00, 0xFFB00E50, InstName.Vnmls, T.VnmlsT1, IsaVersion.v80, InstFlags.None),
                new(0xEE100900, 0xFFB00F50, InstName.Vnmls, T.VnmlsT1, IsaVersion.v80, IsaFeature.FeatFp16, InstFlags.None),
                new(0xEE200A40, 0xFFB00E50, InstName.Vnmul, T.VnmulT1, IsaVersion.v80, InstFlags.None),
                new(0xEE200940, 0xFFB00F50, InstName.Vnmul, T.VnmulT1, IsaVersion.v80, IsaFeature.FeatFp16, InstFlags.None),
                new(0xEF300110, 0xFFB00F10, qvdQvnQvmConstraints, InstName.VornR, T.VornRT1, IsaVersion.v80, InstFlags.None),
                new(0xEF800110, 0xEFB809B0, cmodeCmodeQvdConstraints, InstName.VorrI, T.VorrIT1, IsaVersion.v80, InstFlags.None),
                new(0xEF800910, 0xEFB80DB0, cmodeCmodeQvdConstraints, InstName.VorrI, T.VorrIT2, IsaVersion.v80, InstFlags.None),
                new(0xEF200110, 0xFFB00F10, qvdQvnQvmConstraints, InstName.VorrR, T.VorrRT1, IsaVersion.v80, InstFlags.None),
                new(0xFFB00600, 0xFFB30F10, sizeQvdQvmConstraints, InstName.Vpadal, T.VpadalT1, IsaVersion.v80, InstFlags.None),
                new(0xFFB00200, 0xFFB30F10, sizeQvdQvmConstraints, InstName.Vpaddl, T.VpaddlT1, IsaVersion.v80, InstFlags.None),
                new(0xFF000D00, 0xFFB00F10, qConstraints, InstName.VpaddF, T.VpaddFT1, IsaVersion.v80, InstFlags.None),
                new(0xFF100D00, 0xFFB00F10, qConstraints, InstName.VpaddF, T.VpaddFT1, IsaVersion.v80, IsaFeature.FeatFp16, InstFlags.None),
                new(0xEF000B10, 0xFF800F10, sizeQConstraints, InstName.VpaddI, T.VpaddIT1, IsaVersion.v80, InstFlags.None),
                new(0xFF000F00, 0xFFB00F50, InstName.VpmaxF, T.VpmaxFT1, IsaVersion.v80, InstFlags.None),
                new(0xFF100F00, 0xFFB00F50, InstName.VpmaxF, T.VpmaxFT1, IsaVersion.v80, IsaFeature.FeatFp16, InstFlags.None),
                new(0xEF000A00, 0xEF800F50, sizeConstraints4, InstName.VpmaxI, T.VpmaxIT1, IsaVersion.v80, InstFlags.None),
                new(0xFF200F00, 0xFFB00F50, InstName.VpminF, T.VpminFT1, IsaVersion.v80, InstFlags.None),
                new(0xFF300F00, 0xFFB00F50, InstName.VpminF, T.VpminFT1, IsaVersion.v80, IsaFeature.FeatFp16, InstFlags.None),
                new(0xEF000A10, 0xEF800F50, sizeConstraints4, InstName.VpminI, T.VpminIT1, IsaVersion.v80, InstFlags.None),
                new(0xFFB00700, 0xFFB30F90, sizeQvdQvmConstraints, InstName.Vqabs, T.VqabsT1, IsaVersion.v80, InstFlags.None),
                new(0xEF000010, 0xEF800F10, qvdQvnQvmConstraints, InstName.Vqadd, T.VqaddT1, IsaVersion.v80, InstFlags.None),
                new(0xEF800900, 0xFF800F50, sizeSizeVdConstraints, InstName.Vqdmlal, T.VqdmlalT1, IsaVersion.v80, InstFlags.None),
                new(0xEF800340, 0xFF800F50, sizeSizeVdConstraints, InstName.Vqdmlal, T.VqdmlalT2, IsaVersion.v80, InstFlags.None),
                new(0xEF800B00, 0xFF800F50, sizeSizeVdConstraints, InstName.Vqdmlsl, T.VqdmlslT1, IsaVersion.v80, InstFlags.None),
                new(0xEF800740, 0xFF800F50, sizeSizeVdConstraints, InstName.Vqdmlsl, T.VqdmlslT2, IsaVersion.v80, InstFlags.None),
                new(0xEF000B00, 0xFF800F10, qvdQvnQvmSizeSizeConstraints, InstName.Vqdmulh, T.VqdmulhT1, IsaVersion.v80, InstFlags.None),
                new(0xEF800C40, 0xEF800F50, sizeSizeQvdQvnConstraints, InstName.Vqdmulh, T.VqdmulhT2, IsaVersion.v80, InstFlags.None),
                new(0xEF800D00, 0xFF800F50, sizeSizeVdConstraints, InstName.Vqdmull, T.VqdmullT1, IsaVersion.v80, InstFlags.None),
                new(0xEF800B40, 0xFF800F50, sizeSizeVdConstraints, InstName.Vqdmull, T.VqdmullT2, IsaVersion.v80, InstFlags.None),
                new(0xFFB20200, 0xFFB30F10, opSizeVmConstraints, InstName.Vqmovn, T.VqmovnT1, IsaVersion.v80, InstFlags.None),
                new(0xFFB00780, 0xFFB30F90, sizeQvdQvmConstraints, InstName.Vqneg, T.VqnegT1, IsaVersion.v80, InstFlags.None),
                new(0xFF000B10, 0xFF800F10, qvdQvnQvmSizeSizeConstraints, InstName.Vqrdmlah, T.VqrdmlahT1, IsaVersion.v81, IsaFeature.FeatRdm, InstFlags.None),
                new(0xEF800E40, 0xEF800F50, sizeSizeQvdQvnConstraints, InstName.Vqrdmlah, T.VqrdmlahT2, IsaVersion.v81, IsaFeature.FeatRdm, InstFlags.None),
                new(0xFF000C10, 0xFF800F10, qvdQvnQvmSizeSizeConstraints, InstName.Vqrdmlsh, T.VqrdmlshT1, IsaVersion.v81, IsaFeature.FeatRdm, InstFlags.None),
                new(0xEF800F40, 0xEF800F50, sizeSizeQvdQvnConstraints, InstName.Vqrdmlsh, T.VqrdmlshT2, IsaVersion.v81, IsaFeature.FeatRdm, InstFlags.None),
                new(0xFF000B00, 0xFF800F10, qvdQvnQvmSizeSizeConstraints, InstName.Vqrdmulh, T.VqrdmulhT1, IsaVersion.v80, InstFlags.None),
                new(0xEF800D40, 0xEF800F50, sizeSizeQvdQvnConstraints, InstName.Vqrdmulh, T.VqrdmulhT2, IsaVersion.v80, InstFlags.None),
                new(0xEF000510, 0xEF800F10, qvdQvmQvnConstraints, InstName.Vqrshl, T.VqrshlT1, IsaVersion.v80, InstFlags.None),
                new(0xEF800850, 0xEF800ED0, imm6UopVmConstraints, InstName.Vqrshrn, T.VqrshrnT1, IsaVersion.v80, InstFlags.None),
                new(0xEF800610, 0xEF800E10, imm6lUopQvdQvmConstraints, InstName.VqshlI, T.VqshlIT1, IsaVersion.v80, InstFlags.None),
                new(0xEF000410, 0xEF800F10, qvdQvmQvnConstraints, InstName.VqshlR, T.VqshlRT1, IsaVersion.v80, InstFlags.None),
                new(0xEF800810, 0xEF800ED0, imm6UopVmConstraints, InstName.Vqshrn, T.VqshrnT1, IsaVersion.v80, InstFlags.None),
                new(0xEF000210, 0xEF800F10, qvdQvnQvmConstraints, InstName.Vqsub, T.VqsubT1, IsaVersion.v80, InstFlags.None),
                new(0xFF800400, 0xFF800F50, sizeVnVmConstraints, InstName.Vraddhn, T.VraddhnT1, IsaVersion.v80, InstFlags.None),
                new(0xFFB30400, 0xFFB30E90, qvdQvmSizeSizeConstraints, InstName.Vrecpe, T.VrecpeT1, IsaVersion.v80, InstFlags.None),
                new(0xEF000F10, 0xFFB00F10, qvdQvnQvmConstraints, InstName.Vrecps, T.VrecpsT1, IsaVersion.v80, InstFlags.None),
                new(0xEF100F10, 0xFFB00F10, qvdQvnQvmConstraints, InstName.Vrecps, T.VrecpsT1, IsaVersion.v80, IsaFeature.FeatFp16, InstFlags.None),
                new(0xFFB00100, 0xFFB30F90, sizeSizeSizeQvdQvmConstraints, InstName.Vrev16, T.Vrev16T1, IsaVersion.v80, InstFlags.None),
                new(0xFFB00080, 0xFFB30F90, sizeSizeQvdQvmConstraints, InstName.Vrev32, T.Vrev32T1, IsaVersion.v80, InstFlags.None),
                new(0xFFB00000, 0xFFB30F90, sizeQvdQvmConstraints, InstName.Vrev64, T.Vrev64T1, IsaVersion.v80, InstFlags.None),
                new(0xEF000100, 0xEF800F10, qvdQvnQvmSizeConstraints, InstName.Vrhadd, T.VrhaddT1, IsaVersion.v80, InstFlags.None),
                new(0xFFBA0500, 0xFFBF0F90, qvdQvmConstraints, InstName.VrintaAsimd, T.VrintaAsimdT1, IsaVersion.v80, InstFlags.None),
                new(0xFFB60500, 0xFFBF0F90, qvdQvmConstraints, InstName.VrintaAsimd, T.VrintaAsimdT1, IsaVersion.v80, IsaFeature.FeatFp16, InstFlags.None),
                new(0xFEB80A40, 0xFFBF0ED0, sizeConstraints, InstName.VrintaVfp, T.VrintaVfpT1, IsaVersion.v80, InstFlags.None),
                new(0xFEB80940, 0xFFBF0FD0, sizeConstraints, InstName.VrintaVfp, T.VrintaVfpT1, IsaVersion.v80, IsaFeature.FeatFp16, InstFlags.None),
                new(0xFFBA0680, 0xFFBF0F90, qvdQvmConstraints, InstName.VrintmAsimd, T.VrintmAsimdT1, IsaVersion.v80, InstFlags.None),
                new(0xFFB60680, 0xFFBF0F90, qvdQvmConstraints, InstName.VrintmAsimd, T.VrintmAsimdT1, IsaVersion.v80, IsaFeature.FeatFp16, InstFlags.None),
                new(0xFEBB0A40, 0xFFBF0ED0, sizeConstraints, InstName.VrintmVfp, T.VrintmVfpT1, IsaVersion.v80, InstFlags.None),
                new(0xFEBB0940, 0xFFBF0FD0, sizeConstraints, InstName.VrintmVfp, T.VrintmVfpT1, IsaVersion.v80, IsaFeature.FeatFp16, InstFlags.None),
                new(0xFFBA0400, 0xFFBF0F90, qvdQvmConstraints, InstName.VrintnAsimd, T.VrintnAsimdT1, IsaVersion.v80, InstFlags.None),
                new(0xFFB60400, 0xFFBF0F90, qvdQvmConstraints, InstName.VrintnAsimd, T.VrintnAsimdT1, IsaVersion.v80, IsaFeature.FeatFp16, InstFlags.None),
                new(0xFEB90A40, 0xFFBF0ED0, sizeConstraints, InstName.VrintnVfp, T.VrintnVfpT1, IsaVersion.v80, InstFlags.None),
                new(0xFEB90940, 0xFFBF0FD0, sizeConstraints, InstName.VrintnVfp, T.VrintnVfpT1, IsaVersion.v80, IsaFeature.FeatFp16, InstFlags.None),
                new(0xFFBA0780, 0xFFBF0F90, qvdQvmConstraints, InstName.VrintpAsimd, T.VrintpAsimdT1, IsaVersion.v80, InstFlags.None),
                new(0xFFB60780, 0xFFBF0F90, qvdQvmConstraints, InstName.VrintpAsimd, T.VrintpAsimdT1, IsaVersion.v80, IsaFeature.FeatFp16, InstFlags.None),
                new(0xFEBA0A40, 0xFFBF0ED0, sizeConstraints, InstName.VrintpVfp, T.VrintpVfpT1, IsaVersion.v80, InstFlags.None),
                new(0xFEBA0940, 0xFFBF0FD0, sizeConstraints, InstName.VrintpVfp, T.VrintpVfpT1, IsaVersion.v80, IsaFeature.FeatFp16, InstFlags.None),
                new(0xEEB60A40, 0xFFBF0ED0, InstName.VrintrVfp, T.VrintrVfpT1, IsaVersion.v80, InstFlags.None),
                new(0xEEB60940, 0xFFBF0FD0, InstName.VrintrVfp, T.VrintrVfpT1, IsaVersion.v80, IsaFeature.FeatFp16, InstFlags.None),
                new(0xFFBA0480, 0xFFBF0F90, qvdQvmConstraints, InstName.VrintxAsimd, T.VrintxAsimdT1, IsaVersion.v80, InstFlags.None),
                new(0xFFB60480, 0xFFBF0F90, qvdQvmConstraints, InstName.VrintxAsimd, T.VrintxAsimdT1, IsaVersion.v80, IsaFeature.FeatFp16, InstFlags.None),
                new(0xEEB70A40, 0xFFBF0ED0, InstName.VrintxVfp, T.VrintxVfpT1, IsaVersion.v80, InstFlags.None),
                new(0xEEB70940, 0xFFBF0FD0, InstName.VrintxVfp, T.VrintxVfpT1, IsaVersion.v80, IsaFeature.FeatFp16, InstFlags.None),
                new(0xFFBA0580, 0xFFBF0F90, qvdQvmConstraints, InstName.VrintzAsimd, T.VrintzAsimdT1, IsaVersion.v80, InstFlags.None),
                new(0xFFB60580, 0xFFBF0F90, qvdQvmConstraints, InstName.VrintzAsimd, T.VrintzAsimdT1, IsaVersion.v80, IsaFeature.FeatFp16, InstFlags.None),
                new(0xEEB60AC0, 0xFFBF0ED0, InstName.VrintzVfp, T.VrintzVfpT1, IsaVersion.v80, InstFlags.None),
                new(0xEEB609C0, 0xFFBF0FD0, InstName.VrintzVfp, T.VrintzVfpT1, IsaVersion.v80, IsaFeature.FeatFp16, InstFlags.None),
                new(0xEF000500, 0xEF800F10, qvdQvmQvnConstraints, InstName.Vrshl, T.VrshlT1, IsaVersion.v80, InstFlags.None),
                new(0xEF800210, 0xEF800F10, imm6lQvdQvmConstraints, InstName.Vrshr, T.VrshrT1, IsaVersion.v80, InstFlags.None),
                new(0xEF800850, 0xFF800FD0, imm6VmConstraints, InstName.Vrshrn, T.VrshrnT1, IsaVersion.v80, InstFlags.None),
                new(0xFFB30480, 0xFFB30E90, qvdQvmSizeSizeConstraints, InstName.Vrsqrte, T.VrsqrteT1, IsaVersion.v80, InstFlags.None),
                new(0xEF200F10, 0xFFB00F10, qvdQvnQvmConstraints, InstName.Vrsqrts, T.VrsqrtsT1, IsaVersion.v80, InstFlags.None),
                new(0xEF300F10, 0xFFB00F10, qvdQvnQvmConstraints, InstName.Vrsqrts, T.VrsqrtsT1, IsaVersion.v80, IsaFeature.FeatFp16, InstFlags.None),
                new(0xEF800310, 0xEF800F10, imm6lQvdQvmConstraints, InstName.Vrsra, T.VrsraT1, IsaVersion.v80, InstFlags.None),
                new(0xFF800600, 0xFF800F50, sizeVnVmConstraints, InstName.Vrsubhn, T.VrsubhnT1, IsaVersion.v80, InstFlags.None),
                new(0xFC200D00, 0xFFB00F10, qvdQvnQvmConstraints, InstName.Vsdot, T.VsdotT1, IsaVersion.v82, IsaFeature.FeatDotprod, InstFlags.None),
                new(0xFE200D00, 0xFFB00F10, qvdQvnConstraints, InstName.VsdotS, T.VsdotST1, IsaVersion.v82, IsaFeature.FeatDotprod, InstFlags.None),
                new(0xFE000A00, 0xFF800E50, sizeConstraints, InstName.Vsel, T.VselT1, IsaVersion.v80, InstFlags.None),
                new(0xFE000900, 0xFF800F50, sizeConstraints, InstName.Vsel, T.VselT1, IsaVersion.v80, IsaFeature.FeatFp16, InstFlags.None),
                new(0xEF800A10, 0xEF800FD0, imm6VdImm6Imm6Imm6Constraints, InstName.Vshll, T.VshllT1, IsaVersion.v80, InstFlags.None),
                new(0xFFB20300, 0xFFB30FD0, sizeVdConstraints2, InstName.Vshll, T.VshllT2, IsaVersion.v80, InstFlags.None),
                new(0xEF800510, 0xFF800F10, imm6lQvdQvmConstraints, InstName.VshlI, T.VshlIT1, IsaVersion.v80, InstFlags.None),
                new(0xEF000400, 0xEF800F10, qvdQvmQvnConstraints, InstName.VshlR, T.VshlRT1, IsaVersion.v80, InstFlags.None),
                new(0xEF800010, 0xEF800F10, imm6lQvdQvmConstraints, InstName.Vshr, T.VshrT1, IsaVersion.v80, InstFlags.None),
                new(0xEF800810, 0xFF800FD0, imm6VmConstraints, InstName.Vshrn, T.VshrnT1, IsaVersion.v80, InstFlags.None),
                new(0xFF800510, 0xFF800F10, imm6lQvdQvmConstraints, InstName.Vsli, T.VsliT1, IsaVersion.v80, InstFlags.None),
                new(0xFC200C40, 0xFFB00F50, vdVnVmConstraints, InstName.Vsmmla, T.VsmmlaT1, IsaVersion.v82, IsaFeature.FeatAa32i8mm, InstFlags.None),
                new(0xEEB10AC0, 0xFFBF0ED0, InstName.Vsqrt, T.VsqrtT1, IsaVersion.v80, InstFlags.None),
                new(0xEEB109C0, 0xFFBF0FD0, InstName.Vsqrt, T.VsqrtT1, IsaVersion.v80, IsaFeature.FeatFp16, InstFlags.None),
                new(0xEF800110, 0xEF800F10, imm6lQvdQvmConstraints, InstName.Vsra, T.VsraT1, IsaVersion.v80, InstFlags.None),
                new(0xFF800410, 0xFF800F10, imm6lQvdQvmConstraints, InstName.Vsri, T.VsriT1, IsaVersion.v80, InstFlags.None),
                new(0xF9800000, 0xFFB00F10, sizeConstraints2, InstName.Vst11, T.Vst11T1, IsaVersion.v80, InstFlags.None),
                new(0xF9800400, 0xFFB00F20, sizeConstraints2, InstName.Vst11, T.Vst11T2, IsaVersion.v80, InstFlags.None),
                new(0xF9800800, 0xFFB00F40, sizeIndexAlignIndexAlignConstraints, InstName.Vst11, T.Vst11T3, IsaVersion.v80, InstFlags.None),
                new(0xF9000700, 0xFFB00F00, alignConstraints, InstName.Vst1M, T.Vst1MT1, IsaVersion.v80, InstFlags.None),
                new(0xF9000A00, 0xFFB00F00, alignConstraints2, InstName.Vst1M, T.Vst1MT2, IsaVersion.v80, InstFlags.None),
                new(0xF9000600, 0xFFB00F00, alignConstraints, InstName.Vst1M, T.Vst1MT3, IsaVersion.v80, InstFlags.None),
                new(0xF9000200, 0xFFB00F00, InstName.Vst1M, T.Vst1MT4, IsaVersion.v80, InstFlags.None),
                new(0xF9800100, 0xFFB00F00, sizeConstraints2, InstName.Vst21, T.Vst21T1, IsaVersion.v80, InstFlags.None),
                new(0xF9800500, 0xFFB00F00, sizeConstraints2, InstName.Vst21, T.Vst21T2, IsaVersion.v80, InstFlags.None),
                new(0xF9800900, 0xFFB00F20, sizeConstraints2, InstName.Vst21, T.Vst21T3, IsaVersion.v80, InstFlags.None),
                new(0xF9000800, 0xFFB00E00, alignSizeConstraints, InstName.Vst2M, T.Vst2MT1, IsaVersion.v80, InstFlags.None),
                new(0xF9000300, 0xFFB00F00, sizeConstraints3, InstName.Vst2M, T.Vst2MT2, IsaVersion.v80, InstFlags.None),
                new(0xF9800200, 0xFFB00F10, sizeConstraints2, InstName.Vst31, T.Vst31T1, IsaVersion.v80, InstFlags.None),
                new(0xF9800600, 0xFFB00F10, sizeConstraints2, InstName.Vst31, T.Vst31T2, IsaVersion.v80, InstFlags.None),
                new(0xF9800A00, 0xFFB00F30, sizeConstraints2, InstName.Vst31, T.Vst31T3, IsaVersion.v80, InstFlags.None),
                new(0xF9000400, 0xFFB00E00, sizeAlignConstraints, InstName.Vst3M, T.Vst3MT1, IsaVersion.v80, InstFlags.None),
                new(0xF9800300, 0xFFB00F00, sizeConstraints2, InstName.Vst41, T.Vst41T1, IsaVersion.v80, InstFlags.None),
                new(0xF9800700, 0xFFB00F00, sizeConstraints2, InstName.Vst41, T.Vst41T2, IsaVersion.v80, InstFlags.None),
                new(0xF9800B00, 0xFFB00F00, sizeIndexAlignConstraints, InstName.Vst41, T.Vst41T3, IsaVersion.v80, InstFlags.None),
                new(0xF9000000, 0xFFB00E00, sizeConstraints3, InstName.Vst4M, T.Vst4MT1, IsaVersion.v80, InstFlags.None),
                new(0xEC000B00, 0xFE100F01, puwPwPuwPuwConstraints, InstName.Vstm, T.VstmT1, IsaVersion.v80, InstFlags.WBack),
                new(0xEC000A00, 0xFE100F00, puwPwPuwPuwConstraints, InstName.Vstm, T.VstmT2, IsaVersion.v80, InstFlags.WBack),
                new(0xED000A00, 0xFF300E00, InstName.Vstr, T.VstrT1, IsaVersion.v80, InstFlags.None),
                new(0xED000900, 0xFF300F00, InstName.Vstr, T.VstrT1, IsaVersion.v80, IsaFeature.FeatFp16, InstFlags.None),
                new(0xEF800600, 0xFF800F50, sizeVnVmConstraints, InstName.Vsubhn, T.VsubhnT1, IsaVersion.v80, InstFlags.None),
                new(0xEF800200, 0xEF800F50, sizeVdOpvnConstraints, InstName.Vsubl, T.VsublT1, IsaVersion.v80, InstFlags.None),
                new(0xEF800300, 0xEF800F50, sizeVdOpvnConstraints, InstName.Vsubw, T.VsubwT1, IsaVersion.v80, InstFlags.None),
                new(0xEF200D00, 0xFFB00F10, qvdQvnQvmConstraints, InstName.VsubF, T.VsubFT1, IsaVersion.v80, InstFlags.None),
                new(0xEF300D00, 0xFFB00F10, qvdQvnQvmConstraints, InstName.VsubF, T.VsubFT1, IsaVersion.v80, IsaFeature.FeatFp16, InstFlags.None),
                new(0xEE300A40, 0xFFB00E50, InstName.VsubF, T.VsubFT2, IsaVersion.v80, InstFlags.None),
                new(0xEE300940, 0xFFB00F50, InstName.VsubF, T.VsubFT2, IsaVersion.v80, IsaFeature.FeatFp16, InstFlags.None),
                new(0xFF000800, 0xFF800F10, qvdQvnQvmConstraints, InstName.VsubI, T.VsubIT1, IsaVersion.v80, InstFlags.None),
                new(0xFE800D10, 0xFFB00F10, qvdQvnConstraints, InstName.VsudotS, T.VsudotST1, IsaVersion.v82, IsaFeature.FeatAa32i8mm, InstFlags.None),
                new(0xFFB20000, 0xFFBF0F90, qvdQvmConstraints, InstName.Vswp, T.VswpT1, IsaVersion.v80, InstFlags.None),
                new(0xFFB00800, 0xFFB00C10, InstName.Vtbl, T.VtblT1, IsaVersion.v80, InstFlags.None),
                new(0xFFB20080, 0xFFB30F90, sizeQvdQvmConstraints, InstName.Vtrn, T.VtrnT1, IsaVersion.v80, InstFlags.None),
                new(0xEF000810, 0xFF800F10, qvdQvnQvmSizeConstraints, InstName.Vtst, T.VtstT1, IsaVersion.v80, InstFlags.None),
                new(0xFC200D10, 0xFFB00F10, qvdQvnQvmConstraints, InstName.Vudot, T.VudotT1, IsaVersion.v82, IsaFeature.FeatDotprod, InstFlags.None),
                new(0xFE200D10, 0xFFB00F10, qvdQvnConstraints, InstName.VudotS, T.VudotST1, IsaVersion.v82, IsaFeature.FeatDotprod, InstFlags.None),
                new(0xFC200C50, 0xFFB00F50, vdVnVmConstraints, InstName.Vummla, T.VummlaT1, IsaVersion.v82, IsaFeature.FeatAa32i8mm, InstFlags.None),
                new(0xFCA00D00, 0xFFB00F10, qvdQvnQvmConstraints, InstName.Vusdot, T.VusdotT1, IsaVersion.v82, IsaFeature.FeatAa32i8mm, InstFlags.None),
                new(0xFE800D00, 0xFFB00F10, qvdQvnConstraints, InstName.VusdotS, T.VusdotST1, IsaVersion.v82, IsaFeature.FeatAa32i8mm, InstFlags.None),
                new(0xFCA00C40, 0xFFB00F50, vdVnVmConstraints, InstName.Vusmmla, T.VusmmlaT1, IsaVersion.v82, IsaFeature.FeatAa32i8mm, InstFlags.None),
                new(0xFFB20100, 0xFFB30F90, sizeQsizeQvdQvmConstraints, InstName.Vuzp, T.VuzpT1, IsaVersion.v80, InstFlags.None),
                new(0xFFB20180, 0xFFB30F90, sizeQsizeQvdQvmConstraints, InstName.Vzip, T.VzipT1, IsaVersion.v80, InstFlags.None),
                new(0xF3AF8002, 0xFFFFFFFF, InstName.Wfe, T.WfeT2, IsaVersion.v80, InstFlags.None),
                new(0xF3AF8003, 0xFFFFFFFF, InstName.Wfi, T.WfiT2, IsaVersion.v80, InstFlags.None),
                new(0xF3AF8001, 0xFFFFFFFF, InstName.Yield, T.YieldT2, IsaVersion.v80, InstFlags.None),
            ];

            _table = new(insts);
        }

        public static bool TryGetMeta(uint encoding, IsaVersion version, IsaFeature features, out InstMeta meta)
        {
            if (_table.TryFind(encoding, version, features, out InstInfoForTable info))
            {
                meta = info.Meta;

                return true;
            }

            meta = new(InstName.Udf, T.UdfA1, IsaVersion.v80, IsaFeature.None, InstFlags.None);

            return false;
        }
    }
}
