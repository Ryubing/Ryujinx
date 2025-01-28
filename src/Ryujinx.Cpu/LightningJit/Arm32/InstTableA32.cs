using Ryujinx.Cpu.LightningJit.Table;
using System.Collections.Generic;

namespace Ryujinx.Cpu.LightningJit.Arm32
{
    static class InstTableA32<T> where T : IInstEmit
    {
        private static readonly InstTableLevel<InstInfoForTable> _table;

        static InstTableA32()
        {
            InstEncoding[] condConstraints =
            [
                new(0xF0000000, 0xF0000000)
            ];

            InstEncoding[] condRnsRnConstraints =
            [
                new(0xF0000000, 0xF0000000),
                new(0x000F0000, 0x001F0000),
                new(0x000D0000, 0x000F0000)
            ];

            InstEncoding[] condRnConstraints =
            [
                new(0xF0000000, 0xF0000000),
                new(0x000D0000, 0x000F0000)
            ];

            InstEncoding[] vdVmConstraints =
            [
                new(0x00001000, 0x00001000),
                new(0x00000001, 0x00000001)
            ];

            InstEncoding[] condRnConstraints2 =
            [
                new(0xF0000000, 0xF0000000),
                new(0x0000000F, 0x0000000F)
            ];

            InstEncoding[] optionConstraints =
            [
                new(0x00000000, 0x0000000F)
            ];

            InstEncoding[] condPuwPwPuwPuwConstraints =
            [
                new(0xF0000000, 0xF0000000),
                new(0x00000000, 0x01A00000),
                new(0x01000000, 0x01200000),
                new(0x00200000, 0x01A00000),
                new(0x01A00000, 0x01A00000)
            ];

            InstEncoding[] condRnPuwConstraints =
            [
                new(0xF0000000, 0xF0000000),
                new(0x000F0000, 0x000F0000),
                new(0x00000000, 0x01A00000)
            ];

            InstEncoding[] condPuwConstraints =
            [
                new(0xF0000000, 0xF0000000),
                new(0x00000000, 0x01A00000)
            ];

            InstEncoding[] condRnPwConstraints =
            [
                new(0xF0000000, 0xF0000000),
                new(0x000F0000, 0x000F0000),
                new(0x00200000, 0x01200000)
            ];

            InstEncoding[] condPwConstraints =
            [
                new(0xF0000000, 0xF0000000),
                new(0x00200000, 0x01200000)
            ];

            InstEncoding[] condRnConstraints3 =
            [
                new(0xF0000000, 0xF0000000),
                new(0x000F0000, 0x000F0000)
            ];

            InstEncoding[] condMaskrConstraints =
            [
                new(0xF0000000, 0xF0000000),
                new(0x00000000, 0x004F0000)
            ];

            InstEncoding[] rnConstraints =
            [
                new(0x000F0000, 0x000F0000)
            ];

            InstEncoding[] vdVnVmConstraints =
            [
                new(0x00001000, 0x00001000),
                new(0x00010000, 0x00010000),
                new(0x00000001, 0x00000001)
            ];

            InstEncoding[] condRaConstraints =
            [
                new(0xF0000000, 0xF0000000),
                new(0x0000F000, 0x0000F000)
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

            InstEncoding[] condQvdEbConstraints =
            [
                new(0xF0000000, 0xF0000000),
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
                new(0x01001000, 0x01001000),
                new(0x01010000, 0x01010000)
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

            InstEncoding[] condOpc1opc2Constraints =
            [
                new(0xF0000000, 0xF0000000),
                new(0x00000040, 0x00400060)
            ];

            InstEncoding[] condUopc1opc2Uopc1opc2Constraints =
            [
                new(0xF0000000, 0xF0000000),
                new(0x00800000, 0x00C00060),
                new(0x00000040, 0x00400060)
            ];

            InstEncoding[] sizeOpuOpsizeVdConstraints =
            [
                new(0x00300000, 0x00300000),
                new(0x01000200, 0x01000200),
                new(0x00100200, 0x00300200),
                new(0x00001000, 0x00001000)
            ];

            InstEncoding[] sizeOpsizeOpsizeQvdQvnQvmConstraints =
            [
                new(0x00300000, 0x00300000),
                new(0x01100000, 0x01300000),
                new(0x01200000, 0x01300000),
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
                new(0x01001000, 0x01001000),
                new(0x01010000, 0x01010000)
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
                new(0x00000000, 0x01000100),
                new(0x00000001, 0x00000001)
            ];

            InstEncoding[] imm6lUopQvdQvmConstraints =
            [
                new(0x00000000, 0x00380080),
                new(0x00000000, 0x01000100),
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
                new(0x02A00000, 0x0FE00000, condConstraints, InstName.AdcI, T.AdcIA1, IsaVersion.v80, InstFlags.CondRd),
                new(0x00A00000, 0x0FE00010, condConstraints, InstName.AdcR, T.AdcRA1, IsaVersion.v80, InstFlags.CondRd),
                new(0x00A00010, 0x0FE00090, condConstraints, InstName.AdcRr, T.AdcRrA1, IsaVersion.v80, InstFlags.CondRd),
                new(0x02800000, 0x0FE00000, condRnsRnConstraints, InstName.AddI, T.AddIA1, IsaVersion.v80, InstFlags.CondRd),
                new(0x00800000, 0x0FE00010, condRnConstraints, InstName.AddR, T.AddRA1, IsaVersion.v80, InstFlags.CondRd),
                new(0x00800010, 0x0FE00090, condConstraints, InstName.AddRr, T.AddRrA1, IsaVersion.v80, InstFlags.CondRd),
                new(0x028D0000, 0x0FEF0000, condConstraints, InstName.AddSpI, T.AddSpIA1, IsaVersion.v80, InstFlags.CondRd),
                new(0x008D0000, 0x0FEF0010, condConstraints, InstName.AddSpR, T.AddSpRA1, IsaVersion.v80, InstFlags.CondRd),
                new(0x028F0000, 0x0FFF0000, condConstraints, InstName.Adr, T.AdrA1, IsaVersion.v80, InstFlags.CondRd),
                new(0x024F0000, 0x0FFF0000, condConstraints, InstName.Adr, T.AdrA2, IsaVersion.v80, InstFlags.CondRd),
                new(0xF3B00340, 0xFFBF0FD0, vdVmConstraints, InstName.Aesd, T.AesdA1, IsaVersion.v80, IsaFeature.FeatAes, InstFlags.None),
                new(0xF3B00300, 0xFFBF0FD0, vdVmConstraints, InstName.Aese, T.AeseA1, IsaVersion.v80, IsaFeature.FeatAes, InstFlags.None),
                new(0xF3B003C0, 0xFFBF0FD0, vdVmConstraints, InstName.Aesimc, T.AesimcA1, IsaVersion.v80, IsaFeature.FeatAes, InstFlags.None),
                new(0xF3B00380, 0xFFBF0FD0, vdVmConstraints, InstName.Aesmc, T.AesmcA1, IsaVersion.v80, IsaFeature.FeatAes, InstFlags.None),
                new(0x02000000, 0x0FE00000, condConstraints, InstName.AndI, T.AndIA1, IsaVersion.v80, InstFlags.CondRd),
                new(0x00000000, 0x0FE00010, condConstraints, InstName.AndR, T.AndRA1, IsaVersion.v80, InstFlags.CondRd),
                new(0x00000010, 0x0FE00090, condConstraints, InstName.AndRr, T.AndRrA1, IsaVersion.v80, InstFlags.CondRd),
                new(0x0A000000, 0x0F000000, condConstraints, InstName.B, T.BA1, IsaVersion.v80, InstFlags.Cond),
                new(0x07C0001F, 0x0FE0007F, condConstraints, InstName.Bfc, T.BfcA1, IsaVersion.v80, InstFlags.CondRd),
                new(0x07C00010, 0x0FE00070, condRnConstraints2, InstName.Bfi, T.BfiA1, IsaVersion.v80, InstFlags.CondRd),
                new(0x03C00000, 0x0FE00000, condConstraints, InstName.BicI, T.BicIA1, IsaVersion.v80, InstFlags.CondRd),
                new(0x01C00000, 0x0FE00010, condConstraints, InstName.BicR, T.BicRA1, IsaVersion.v80, InstFlags.CondRd),
                new(0x01C00010, 0x0FE00090, condConstraints, InstName.BicRr, T.BicRrA1, IsaVersion.v80, InstFlags.CondRd),
                new(0x01200070, 0x0FF000F0, condConstraints, InstName.Bkpt, T.BkptA1, IsaVersion.v80, InstFlags.Cond),
                new(0x012FFF30, 0x0FFFFFF0, condConstraints, InstName.BlxR, T.BlxRA1, IsaVersion.v80, InstFlags.Cond),
                new(0x0B000000, 0x0F000000, condConstraints, InstName.BlI, T.BlIA1, IsaVersion.v80, InstFlags.Cond),
                new(0xFA000000, 0xFE000000, InstName.BlI, T.BlIA2, IsaVersion.v80, InstFlags.None),
                new(0x012FFF10, 0x0FFFFFF0, condConstraints, InstName.Bx, T.BxA1, IsaVersion.v80, InstFlags.Cond),
                new(0x012FFF20, 0x0FFFFFF0, condConstraints, InstName.Bxj, T.BxjA1, IsaVersion.v80, InstFlags.Cond),
                new(0x0320F016, 0x0FFFFFFF, condConstraints, InstName.Clrbhb, T.ClrbhbA1, IsaVersion.v89, IsaFeature.FeatClrbhb, InstFlags.Cond),
                new(0xF57FF01F, 0xFFFFFFFF, InstName.Clrex, T.ClrexA1, IsaVersion.v80, InstFlags.None),
                new(0x016F0F10, 0x0FFF0FF0, condConstraints, InstName.Clz, T.ClzA1, IsaVersion.v80, InstFlags.CondRd),
                new(0x03700000, 0x0FF0F000, condConstraints, InstName.CmnI, T.CmnIA1, IsaVersion.v80, InstFlags.Cond),
                new(0x01700000, 0x0FF0F010, condConstraints, InstName.CmnR, T.CmnRA1, IsaVersion.v80, InstFlags.Cond),
                new(0x01700010, 0x0FF0F090, condConstraints, InstName.CmnRr, T.CmnRrA1, IsaVersion.v80, InstFlags.Cond),
                new(0x03500000, 0x0FF0F000, condConstraints, InstName.CmpI, T.CmpIA1, IsaVersion.v80, InstFlags.Cond),
                new(0x01500000, 0x0FF0F010, condConstraints, InstName.CmpR, T.CmpRA1, IsaVersion.v80, InstFlags.Cond),
                new(0x01500010, 0x0FF0F090, condConstraints, InstName.CmpRr, T.CmpRrA1, IsaVersion.v80, InstFlags.Cond),
                new(0xF1000000, 0xFFF1FE20, InstName.Cps, T.CpsA1, IsaVersion.v80, InstFlags.None),
                new(0x01000040, 0x0F900FF0, condConstraints, InstName.Crc32, T.Crc32A1, IsaVersion.v80, IsaFeature.FeatCrc32, InstFlags.CondRd),
                new(0x01000240, 0x0F900FF0, condConstraints, InstName.Crc32c, T.Crc32cA1, IsaVersion.v80, IsaFeature.FeatCrc32, InstFlags.CondRd),
                new(0x0320F014, 0x0FFFFFFF, condConstraints, InstName.Csdb, T.CsdbA1, IsaVersion.v80, InstFlags.Cond),
                new(0x0320F0F0, 0x0FFFFFF0, condConstraints, InstName.Dbg, T.DbgA1, IsaVersion.v80, InstFlags.Cond),
                new(0xF57FF050, 0xFFFFFFF0, InstName.Dmb, T.DmbA1, IsaVersion.v80, InstFlags.None),
                new(0xF57FF040, 0xFFFFFFF0, optionConstraints, InstName.Dsb, T.DsbA1, IsaVersion.v80, InstFlags.None),
                new(0x02200000, 0x0FE00000, condConstraints, InstName.EorI, T.EorIA1, IsaVersion.v80, InstFlags.CondRd),
                new(0x00200000, 0x0FE00010, condConstraints, InstName.EorR, T.EorRA1, IsaVersion.v80, InstFlags.CondRd),
                new(0x00200010, 0x0FE00090, condConstraints, InstName.EorRr, T.EorRrA1, IsaVersion.v80, InstFlags.CondRd),
                new(0x0160006E, 0x0FFFFFFF, condConstraints, InstName.Eret, T.EretA1, IsaVersion.v80, InstFlags.Cond),
                new(0x0320F010, 0x0FFFFFFF, condConstraints, InstName.Esb, T.EsbA1, IsaVersion.v82, IsaFeature.FeatRas, InstFlags.Cond),
                new(0x0C100B01, 0x0E100F01, condPuwPwPuwPuwConstraints, InstName.Fldmx, T.FldmxA1, IsaVersion.v80, InstFlags.CondWBack),
                new(0x0C000B01, 0x0E100F01, condPuwPwPuwPuwConstraints, InstName.Fstmx, T.FstmxA1, IsaVersion.v80, InstFlags.CondWBack),
                new(0x01000070, 0x0FF000F0, condConstraints, InstName.Hlt, T.HltA1, IsaVersion.v80, InstFlags.Cond),
                new(0x01400070, 0x0FF000F0, condConstraints, InstName.Hvc, T.HvcA1, IsaVersion.v80, InstFlags.Cond),
                new(0xF57FF060, 0xFFFFFFF0, InstName.Isb, T.IsbA1, IsaVersion.v80, InstFlags.None),
                new(0x01900C9F, 0x0FF00FFF, condConstraints, InstName.Lda, T.LdaA1, IsaVersion.v80, InstFlags.CondRt),
                new(0x01D00C9F, 0x0FF00FFF, condConstraints, InstName.Ldab, T.LdabA1, IsaVersion.v80, InstFlags.CondRt),
                new(0x01900E9F, 0x0FF00FFF, condConstraints, InstName.Ldaex, T.LdaexA1, IsaVersion.v80, InstFlags.CondRt),
                new(0x01D00E9F, 0x0FF00FFF, condConstraints, InstName.Ldaexb, T.LdaexbA1, IsaVersion.v80, InstFlags.CondRt),
                new(0x01B00E9F, 0x0FF00FFF, condConstraints, InstName.Ldaexd, T.LdaexdA1, IsaVersion.v80, InstFlags.CondRt2),
                new(0x01F00E9F, 0x0FF00FFF, condConstraints, InstName.Ldaexh, T.LdaexhA1, IsaVersion.v80, InstFlags.CondRt),
                new(0x01F00C9F, 0x0FF00FFF, condConstraints, InstName.Ldah, T.LdahA1, IsaVersion.v80, InstFlags.CondRt),
                new(0x0C105E00, 0x0E50FF00, condRnPuwConstraints, InstName.LdcI, T.LdcIA1, IsaVersion.v80, InstFlags.CondWBack),
                new(0x0C1F5E00, 0x0E5FFF00, condPuwConstraints, InstName.LdcL, T.LdcLA1, IsaVersion.v80, InstFlags.CondWBack),
                new(0x08900000, 0x0FD00000, condConstraints, InstName.Ldm, T.LdmA1, IsaVersion.v80, InstFlags.CondRlistWBack),
                new(0x08100000, 0x0FD00000, condConstraints, InstName.Ldmda, T.LdmdaA1, IsaVersion.v80, InstFlags.CondRlistWBack),
                new(0x09100000, 0x0FD00000, condConstraints, InstName.Ldmdb, T.LdmdbA1, IsaVersion.v80, InstFlags.CondRlistWBack),
                new(0x09900000, 0x0FD00000, condConstraints, InstName.Ldmib, T.LdmibA1, IsaVersion.v80, InstFlags.CondRlistWBack),
                new(0x08508000, 0x0E508000, condConstraints, InstName.LdmE, T.LdmEA1, IsaVersion.v80, InstFlags.CondRlistWBack),
                new(0x08500000, 0x0E708000, condConstraints, InstName.LdmU, T.LdmUA1, IsaVersion.v80, InstFlags.CondRlist),
                new(0x04700000, 0x0F700000, condConstraints, InstName.Ldrbt, T.LdrbtA1, IsaVersion.v80, InstFlags.CondRt),
                new(0x06700000, 0x0F700010, condConstraints, InstName.Ldrbt, T.LdrbtA2, IsaVersion.v80, InstFlags.CondRt),
                new(0x04500000, 0x0E500000, condRnPwConstraints, InstName.LdrbI, T.LdrbIA1, IsaVersion.v80, InstFlags.CondRtWBack),
                new(0x045F0000, 0x0E5F0000, condPwConstraints, InstName.LdrbL, T.LdrbLA1, IsaVersion.v80, InstFlags.CondRtWBack),
                new(0x06500000, 0x0E500010, condPwConstraints, InstName.LdrbR, T.LdrbRA1, IsaVersion.v80, InstFlags.CondRtWBack),
                new(0x004000D0, 0x0E5000F0, condRnConstraints3, InstName.LdrdI, T.LdrdIA1, IsaVersion.v80, InstFlags.CondRt2WBack),
                new(0x014F00D0, 0x0F7F00F0, condConstraints, InstName.LdrdL, T.LdrdLA1, IsaVersion.v80, InstFlags.CondRt2),
                new(0x000000D0, 0x0E500FF0, condConstraints, InstName.LdrdR, T.LdrdRA1, IsaVersion.v80, InstFlags.CondRt2WBack),
                new(0x01900F9F, 0x0FF00FFF, condConstraints, InstName.Ldrex, T.LdrexA1, IsaVersion.v80, InstFlags.CondRt),
                new(0x01D00F9F, 0x0FF00FFF, condConstraints, InstName.Ldrexb, T.LdrexbA1, IsaVersion.v80, InstFlags.CondRt),
                new(0x01B00F9F, 0x0FF00FFF, condConstraints, InstName.Ldrexd, T.LdrexdA1, IsaVersion.v80, InstFlags.CondRt2),
                new(0x01F00F9F, 0x0FF00FFF, condConstraints, InstName.Ldrexh, T.LdrexhA1, IsaVersion.v80, InstFlags.CondRt),
                new(0x007000B0, 0x0F7000F0, condConstraints, InstName.Ldrht, T.LdrhtA1, IsaVersion.v80, InstFlags.CondRt),
                new(0x003000B0, 0x0F700FF0, condConstraints, InstName.Ldrht, T.LdrhtA2, IsaVersion.v80, InstFlags.CondRt),
                new(0x005000B0, 0x0E5000F0, condRnPwConstraints, InstName.LdrhI, T.LdrhIA1, IsaVersion.v80, InstFlags.CondRtWBack),
                new(0x005F00B0, 0x0E5F00F0, condPwConstraints, InstName.LdrhL, T.LdrhLA1, IsaVersion.v80, InstFlags.CondRtWBack),
                new(0x001000B0, 0x0E500FF0, condPwConstraints, InstName.LdrhR, T.LdrhRA1, IsaVersion.v80, InstFlags.CondRtWBack),
                new(0x007000D0, 0x0F7000F0, condConstraints, InstName.Ldrsbt, T.LdrsbtA1, IsaVersion.v80, InstFlags.CondRt),
                new(0x003000D0, 0x0F700FF0, condConstraints, InstName.Ldrsbt, T.LdrsbtA2, IsaVersion.v80, InstFlags.CondRt),
                new(0x005000D0, 0x0E5000F0, condRnPwConstraints, InstName.LdrsbI, T.LdrsbIA1, IsaVersion.v80, InstFlags.CondRtWBack),
                new(0x005F00D0, 0x0E5F00F0, condPwConstraints, InstName.LdrsbL, T.LdrsbLA1, IsaVersion.v80, InstFlags.CondRtWBack),
                new(0x001000D0, 0x0E500FF0, condPwConstraints, InstName.LdrsbR, T.LdrsbRA1, IsaVersion.v80, InstFlags.CondRtWBack),
                new(0x007000F0, 0x0F7000F0, condConstraints, InstName.Ldrsht, T.LdrshtA1, IsaVersion.v80, InstFlags.CondRt),
                new(0x003000F0, 0x0F700FF0, condConstraints, InstName.Ldrsht, T.LdrshtA2, IsaVersion.v80, InstFlags.CondRt),
                new(0x005000F0, 0x0E5000F0, condRnPwConstraints, InstName.LdrshI, T.LdrshIA1, IsaVersion.v80, InstFlags.CondRtWBack),
                new(0x005F00F0, 0x0E5F00F0, condPwConstraints, InstName.LdrshL, T.LdrshLA1, IsaVersion.v80, InstFlags.CondRtWBack),
                new(0x001000F0, 0x0E500FF0, condPwConstraints, InstName.LdrshR, T.LdrshRA1, IsaVersion.v80, InstFlags.CondRtWBack),
                new(0x04300000, 0x0F700000, condConstraints, InstName.Ldrt, T.LdrtA1, IsaVersion.v80, InstFlags.CondRt),
                new(0x06300000, 0x0F700010, condConstraints, InstName.Ldrt, T.LdrtA2, IsaVersion.v80, InstFlags.CondRt),
                new(0x04100000, 0x0E500000, condRnPwConstraints, InstName.LdrI, T.LdrIA1, IsaVersion.v80, InstFlags.CondRtWBack),
                new(0x041F0000, 0x0E5F0000, condPwConstraints, InstName.LdrL, T.LdrLA1, IsaVersion.v80, InstFlags.CondRtWBack),
                new(0x06100000, 0x0E500010, condPwConstraints, InstName.LdrR, T.LdrRA1, IsaVersion.v80, InstFlags.CondRtWBack),
                new(0x0E000E10, 0x0F100E10, condConstraints, InstName.Mcr, T.McrA1, IsaVersion.v80, InstFlags.CondRt),
                new(0x0C400E00, 0x0FF00E00, condConstraints, InstName.Mcrr, T.McrrA1, IsaVersion.v80, InstFlags.CondRt),
                new(0x00200090, 0x0FE000F0, condConstraints, InstName.Mla, T.MlaA1, IsaVersion.v80, InstFlags.CondRd16),
                new(0x00600090, 0x0FF000F0, condConstraints, InstName.Mls, T.MlsA1, IsaVersion.v80, InstFlags.CondRd16),
                new(0x03400000, 0x0FF00000, condConstraints, InstName.Movt, T.MovtA1, IsaVersion.v80, InstFlags.CondRd),
                new(0x03A00000, 0x0FEF0000, condConstraints, InstName.MovI, T.MovIA1, IsaVersion.v80, InstFlags.CondRd),
                new(0x03000000, 0x0FF00000, condConstraints, InstName.MovI, T.MovIA2, IsaVersion.v80, InstFlags.CondRd),
                new(0x01A00000, 0x0FEF0010, condConstraints, InstName.MovR, T.MovRA1, IsaVersion.v80, InstFlags.CondRd),
                new(0x01A00010, 0x0FEF0090, condConstraints, InstName.MovRr, T.MovRrA1, IsaVersion.v80, InstFlags.CondRd),
                new(0x0E100E10, 0x0F100E10, condConstraints, InstName.Mrc, T.MrcA1, IsaVersion.v80, InstFlags.CondRt),
                new(0x0C500E00, 0x0FF00E00, condConstraints, InstName.Mrrc, T.MrrcA1, IsaVersion.v80, InstFlags.CondRt),
                new(0x010F0000, 0x0FBF0FFF, condConstraints, InstName.Mrs, T.MrsA1, IsaVersion.v80, InstFlags.CondRd),
                new(0x01000200, 0x0FB00EFF, condConstraints, InstName.MrsBr, T.MrsBrA1, IsaVersion.v80, InstFlags.CondRd),
                new(0x0120F200, 0x0FB0FEF0, condConstraints, InstName.MsrBr, T.MsrBrA1, IsaVersion.v80, InstFlags.Cond),
                new(0x0320F000, 0x0FB0F000, condMaskrConstraints, InstName.MsrI, T.MsrIA1, IsaVersion.v80, InstFlags.Cond),
                new(0x0120F000, 0x0FB0FFF0, condConstraints, InstName.MsrR, T.MsrRA1, IsaVersion.v80, InstFlags.Cond),
                new(0x00000090, 0x0FE0F0F0, condConstraints, InstName.Mul, T.MulA1, IsaVersion.v80, InstFlags.CondRd16),
                new(0x03E00000, 0x0FEF0000, condConstraints, InstName.MvnI, T.MvnIA1, IsaVersion.v80, InstFlags.CondRd),
                new(0x01E00000, 0x0FEF0010, condConstraints, InstName.MvnR, T.MvnRA1, IsaVersion.v80, InstFlags.CondRd),
                new(0x01E00010, 0x0FEF0090, condConstraints, InstName.MvnRr, T.MvnRrA1, IsaVersion.v80, InstFlags.CondRd),
                new(0x0320F000, 0x0FFFFFFF, condConstraints, InstName.Nop, T.NopA1, IsaVersion.v80, InstFlags.Cond),
                new(0x03800000, 0x0FE00000, condConstraints, InstName.OrrI, T.OrrIA1, IsaVersion.v80, InstFlags.CondRd),
                new(0x01800000, 0x0FE00010, condConstraints, InstName.OrrR, T.OrrRA1, IsaVersion.v80, InstFlags.CondRd),
                new(0x01800010, 0x0FE00090, condConstraints, InstName.OrrRr, T.OrrRrA1, IsaVersion.v80, InstFlags.CondRd),
                new(0x06800010, 0x0FF00030, condConstraints, InstName.Pkh, T.PkhA1, IsaVersion.v80, InstFlags.CondRd),
                new(0xF510F000, 0xFF30F000, rnConstraints, InstName.PldI, T.PldIA1, IsaVersion.v80, InstFlags.None),
                new(0xF55FF000, 0xFF7FF000, InstName.PldL, T.PldLA1, IsaVersion.v80, InstFlags.None),
                new(0xF710F000, 0xFF30F010, InstName.PldR, T.PldRA1, IsaVersion.v80, InstFlags.None),
                new(0xF450F000, 0xFF70F000, InstName.PliI, T.PliIA1, IsaVersion.v80, InstFlags.None),
                new(0xF650F000, 0xFF70F010, InstName.PliR, T.PliRA1, IsaVersion.v80, InstFlags.None),
                new(0xF57FF044, 0xFFFFFFFF, InstName.Pssbb, T.PssbbA1, IsaVersion.v80, InstFlags.None),
                new(0x01000050, 0x0FF00FF0, condConstraints, InstName.Qadd, T.QaddA1, IsaVersion.v80, InstFlags.CondRd),
                new(0x06200F10, 0x0FF00FF0, condConstraints, InstName.Qadd16, T.Qadd16A1, IsaVersion.v80, InstFlags.CondRd),
                new(0x06200F90, 0x0FF00FF0, condConstraints, InstName.Qadd8, T.Qadd8A1, IsaVersion.v80, InstFlags.CondRd),
                new(0x06200F30, 0x0FF00FF0, condConstraints, InstName.Qasx, T.QasxA1, IsaVersion.v80, InstFlags.CondRd),
                new(0x01400050, 0x0FF00FF0, condConstraints, InstName.Qdadd, T.QdaddA1, IsaVersion.v80, InstFlags.CondRd),
                new(0x01600050, 0x0FF00FF0, condConstraints, InstName.Qdsub, T.QdsubA1, IsaVersion.v80, InstFlags.CondRd),
                new(0x06200F50, 0x0FF00FF0, condConstraints, InstName.Qsax, T.QsaxA1, IsaVersion.v80, InstFlags.CondRd),
                new(0x01200050, 0x0FF00FF0, condConstraints, InstName.Qsub, T.QsubA1, IsaVersion.v80, InstFlags.CondRd),
                new(0x06200F70, 0x0FF00FF0, condConstraints, InstName.Qsub16, T.Qsub16A1, IsaVersion.v80, InstFlags.CondRd),
                new(0x06200FF0, 0x0FF00FF0, condConstraints, InstName.Qsub8, T.Qsub8A1, IsaVersion.v80, InstFlags.CondRd),
                new(0x06FF0F30, 0x0FFF0FF0, condConstraints, InstName.Rbit, T.RbitA1, IsaVersion.v80, InstFlags.CondRd),
                new(0x06BF0F30, 0x0FFF0FF0, condConstraints, InstName.Rev, T.RevA1, IsaVersion.v80, InstFlags.CondRd),
                new(0x06BF0FB0, 0x0FFF0FF0, condConstraints, InstName.Rev16, T.Rev16A1, IsaVersion.v80, InstFlags.CondRd),
                new(0x06FF0FB0, 0x0FFF0FF0, condConstraints, InstName.Revsh, T.RevshA1, IsaVersion.v80, InstFlags.CondRd),
                new(0xF8100A00, 0xFE50FFFF, InstName.Rfe, T.RfeA1, IsaVersion.v80, InstFlags.WBack),
                new(0x02600000, 0x0FE00000, condConstraints, InstName.RsbI, T.RsbIA1, IsaVersion.v80, InstFlags.CondRd),
                new(0x00600000, 0x0FE00010, condConstraints, InstName.RsbR, T.RsbRA1, IsaVersion.v80, InstFlags.CondRd),
                new(0x00600010, 0x0FE00090, condConstraints, InstName.RsbRr, T.RsbRrA1, IsaVersion.v80, InstFlags.CondRd),
                new(0x02E00000, 0x0FE00000, condConstraints, InstName.RscI, T.RscIA1, IsaVersion.v80, InstFlags.CondRd),
                new(0x00E00000, 0x0FE00010, condConstraints, InstName.RscR, T.RscRA1, IsaVersion.v80, InstFlags.CondRd),
                new(0x00E00010, 0x0FE00090, condConstraints, InstName.RscRr, T.RscRrA1, IsaVersion.v80, InstFlags.CondRd),
                new(0x06100F10, 0x0FF00FF0, condConstraints, InstName.Sadd16, T.Sadd16A1, IsaVersion.v80, InstFlags.CondRd),
                new(0x06100F90, 0x0FF00FF0, condConstraints, InstName.Sadd8, T.Sadd8A1, IsaVersion.v80, InstFlags.CondRd),
                new(0x06100F30, 0x0FF00FF0, condConstraints, InstName.Sasx, T.SasxA1, IsaVersion.v80, InstFlags.CondRd),
                new(0xF57FF070, 0xFFFFFFFF, InstName.Sb, T.SbA1, IsaVersion.v85, IsaFeature.FeatSb, InstFlags.None),
                new(0x02C00000, 0x0FE00000, condConstraints, InstName.SbcI, T.SbcIA1, IsaVersion.v80, InstFlags.CondRd),
                new(0x00C00000, 0x0FE00010, condConstraints, InstName.SbcR, T.SbcRA1, IsaVersion.v80, InstFlags.CondRd),
                new(0x00C00010, 0x0FE00090, condConstraints, InstName.SbcRr, T.SbcRrA1, IsaVersion.v80, InstFlags.CondRd),
                new(0x07A00050, 0x0FE00070, condConstraints, InstName.Sbfx, T.SbfxA1, IsaVersion.v80, InstFlags.CondRd),
                new(0x0710F010, 0x0FF0F0F0, condConstraints, InstName.Sdiv, T.SdivA1, IsaVersion.v80, InstFlags.CondRd16),
                new(0x06800FB0, 0x0FF00FF0, condConstraints, InstName.Sel, T.SelA1, IsaVersion.v80, InstFlags.CondRd),
                new(0xF1010000, 0xFFFFFDFF, InstName.Setend, T.SetendA1, IsaVersion.v80, InstFlags.None),
                new(0xF1100000, 0xFFFFFDFF, InstName.Setpan, T.SetpanA1, IsaVersion.v81, IsaFeature.FeatPan, InstFlags.None),
                new(0x0320F004, 0x0FFFFFFF, condConstraints, InstName.Sev, T.SevA1, IsaVersion.v80, InstFlags.Cond),
                new(0x0320F005, 0x0FFFFFFF, condConstraints, InstName.Sevl, T.SevlA1, IsaVersion.v80, InstFlags.Cond),
                new(0xF2000C40, 0xFFB00F50, vdVnVmConstraints, InstName.Sha1c, T.Sha1cA1, IsaVersion.v80, IsaFeature.FeatSha1, InstFlags.None),
                new(0xF3B902C0, 0xFFBF0FD0, vdVmConstraints, InstName.Sha1h, T.Sha1hA1, IsaVersion.v80, IsaFeature.FeatSha1, InstFlags.None),
                new(0xF2200C40, 0xFFB00F50, vdVnVmConstraints, InstName.Sha1m, T.Sha1mA1, IsaVersion.v80, IsaFeature.FeatSha1, InstFlags.None),
                new(0xF2100C40, 0xFFB00F50, vdVnVmConstraints, InstName.Sha1p, T.Sha1pA1, IsaVersion.v80, IsaFeature.FeatSha1, InstFlags.None),
                new(0xF2300C40, 0xFFB00F50, vdVnVmConstraints, InstName.Sha1su0, T.Sha1su0A1, IsaVersion.v80, IsaFeature.FeatSha1, InstFlags.None),
                new(0xF3BA0380, 0xFFBF0FD0, vdVmConstraints, InstName.Sha1su1, T.Sha1su1A1, IsaVersion.v80, IsaFeature.FeatSha1, InstFlags.None),
                new(0xF3000C40, 0xFFB00F50, vdVnVmConstraints, InstName.Sha256h, T.Sha256hA1, IsaVersion.v80, IsaFeature.FeatSha256, InstFlags.None),
                new(0xF3100C40, 0xFFB00F50, vdVnVmConstraints, InstName.Sha256h2, T.Sha256h2A1, IsaVersion.v80, IsaFeature.FeatSha256, InstFlags.None),
                new(0xF3BA03C0, 0xFFBF0FD0, vdVmConstraints, InstName.Sha256su0, T.Sha256su0A1, IsaVersion.v80, IsaFeature.FeatSha256, InstFlags.None),
                new(0xF3200C40, 0xFFB00F50, vdVnVmConstraints, InstName.Sha256su1, T.Sha256su1A1, IsaVersion.v80, IsaFeature.FeatSha256, InstFlags.None),
                new(0x06300F10, 0x0FF00FF0, condConstraints, InstName.Shadd16, T.Shadd16A1, IsaVersion.v80, InstFlags.CondRd),
                new(0x06300F90, 0x0FF00FF0, condConstraints, InstName.Shadd8, T.Shadd8A1, IsaVersion.v80, InstFlags.CondRd),
                new(0x06300F30, 0x0FF00FF0, condConstraints, InstName.Shasx, T.ShasxA1, IsaVersion.v80, InstFlags.CondRd),
                new(0x06300F50, 0x0FF00FF0, condConstraints, InstName.Shsax, T.ShsaxA1, IsaVersion.v80, InstFlags.CondRd),
                new(0x06300F70, 0x0FF00FF0, condConstraints, InstName.Shsub16, T.Shsub16A1, IsaVersion.v80, InstFlags.CondRd),
                new(0x06300FF0, 0x0FF00FF0, condConstraints, InstName.Shsub8, T.Shsub8A1, IsaVersion.v80, InstFlags.CondRd),
                new(0x01600070, 0x0FFFFFF0, condConstraints, InstName.Smc, T.SmcA1, IsaVersion.v80, InstFlags.Cond),
                new(0x01000080, 0x0FF00090, condConstraints, InstName.Smlabb, T.SmlabbA1, IsaVersion.v80, InstFlags.CondRd16),
                new(0x07000010, 0x0FF000D0, condRaConstraints, InstName.Smlad, T.SmladA1, IsaVersion.v80, InstFlags.CondRd16),
                new(0x00E00090, 0x0FE000F0, condConstraints, InstName.Smlal, T.SmlalA1, IsaVersion.v80, InstFlags.CondRdLoHi),
                new(0x01400080, 0x0FF00090, condConstraints, InstName.Smlalbb, T.SmlalbbA1, IsaVersion.v80, InstFlags.CondRdLoHi),
                new(0x07400010, 0x0FF000D0, condConstraints, InstName.Smlald, T.SmlaldA1, IsaVersion.v80, InstFlags.CondRdLoHi),
                new(0x01200080, 0x0FF000B0, condConstraints, InstName.Smlawb, T.SmlawbA1, IsaVersion.v80, InstFlags.CondRd16),
                new(0x07000050, 0x0FF000D0, condRaConstraints, InstName.Smlsd, T.SmlsdA1, IsaVersion.v80, InstFlags.CondRd16),
                new(0x07400050, 0x0FF000D0, condConstraints, InstName.Smlsld, T.SmlsldA1, IsaVersion.v80, InstFlags.CondRdLoHi),
                new(0x07500010, 0x0FF000D0, condRaConstraints, InstName.Smmla, T.SmmlaA1, IsaVersion.v80, InstFlags.CondRd16),
                new(0x075000D0, 0x0FF000D0, condConstraints, InstName.Smmls, T.SmmlsA1, IsaVersion.v80, InstFlags.CondRd16),
                new(0x0750F010, 0x0FF0F0D0, condConstraints, InstName.Smmul, T.SmmulA1, IsaVersion.v80, InstFlags.CondRd16),
                new(0x0700F010, 0x0FF0F0D0, condConstraints, InstName.Smuad, T.SmuadA1, IsaVersion.v80, InstFlags.CondRd16),
                new(0x01600080, 0x0FF0F090, condConstraints, InstName.Smulbb, T.SmulbbA1, IsaVersion.v80, InstFlags.CondRd16),
                new(0x00C00090, 0x0FE000F0, condConstraints, InstName.Smull, T.SmullA1, IsaVersion.v80, InstFlags.CondRdLoHi),
                new(0x012000A0, 0x0FF0F0B0, condConstraints, InstName.Smulwb, T.SmulwbA1, IsaVersion.v80, InstFlags.CondRd16),
                new(0x0700F050, 0x0FF0F0D0, condConstraints, InstName.Smusd, T.SmusdA1, IsaVersion.v80, InstFlags.CondRd16),
                new(0xF84D0500, 0xFE5FFFE0, InstName.Srs, T.SrsA1, IsaVersion.v80, InstFlags.WBack),
                new(0x06A00010, 0x0FE00030, condConstraints, InstName.Ssat, T.SsatA1, IsaVersion.v80, InstFlags.CondRd),
                new(0x06A00F30, 0x0FF00FF0, condConstraints, InstName.Ssat16, T.Ssat16A1, IsaVersion.v80, InstFlags.CondRd),
                new(0x06100F50, 0x0FF00FF0, condConstraints, InstName.Ssax, T.SsaxA1, IsaVersion.v80, InstFlags.CondRd),
                new(0xF57FF040, 0xFFFFFFFF, InstName.Ssbb, T.SsbbA1, IsaVersion.v80, InstFlags.None),
                new(0x06100F70, 0x0FF00FF0, condConstraints, InstName.Ssub16, T.Ssub16A1, IsaVersion.v80, InstFlags.CondRd),
                new(0x06100FF0, 0x0FF00FF0, condConstraints, InstName.Ssub8, T.Ssub8A1, IsaVersion.v80, InstFlags.CondRd),
                new(0x0C005E00, 0x0E50FF00, condPuwConstraints, InstName.Stc, T.StcA1, IsaVersion.v80, InstFlags.CondWBack),
                new(0x0180FC90, 0x0FF0FFF0, condConstraints, InstName.Stl, T.StlA1, IsaVersion.v80, InstFlags.CondRtRead),
                new(0x01C0FC90, 0x0FF0FFF0, condConstraints, InstName.Stlb, T.StlbA1, IsaVersion.v80, InstFlags.CondRtRead),
                new(0x01800E90, 0x0FF00FF0, condConstraints, InstName.Stlex, T.StlexA1, IsaVersion.v80, InstFlags.CondRdRtRead),
                new(0x01C00E90, 0x0FF00FF0, condConstraints, InstName.Stlexb, T.StlexbA1, IsaVersion.v80, InstFlags.CondRdRtRead),
                new(0x01A00E90, 0x0FF00FF0, condConstraints, InstName.Stlexd, T.StlexdA1, IsaVersion.v80, InstFlags.CondRdRt2Read),
                new(0x01E00E90, 0x0FF00FF0, condConstraints, InstName.Stlexh, T.StlexhA1, IsaVersion.v80, InstFlags.CondRdRtRead),
                new(0x01E0FC90, 0x0FF0FFF0, condConstraints, InstName.Stlh, T.StlhA1, IsaVersion.v80, InstFlags.CondRtRead),
                new(0x08800000, 0x0FD00000, condConstraints, InstName.Stm, T.StmA1, IsaVersion.v80, InstFlags.CondRlistReadWBack),
                new(0x08000000, 0x0FD00000, condConstraints, InstName.Stmda, T.StmdaA1, IsaVersion.v80, InstFlags.CondRlistReadWBack),
                new(0x09000000, 0x0FD00000, condConstraints, InstName.Stmdb, T.StmdbA1, IsaVersion.v80, InstFlags.CondRlistReadWBack),
                new(0x09800000, 0x0FD00000, condConstraints, InstName.Stmib, T.StmibA1, IsaVersion.v80, InstFlags.CondRlistReadWBack),
                new(0x08400000, 0x0E700000, condConstraints, InstName.StmU, T.StmUA1, IsaVersion.v80, InstFlags.CondRlistRead),
                new(0x04600000, 0x0F700000, condConstraints, InstName.Strbt, T.StrbtA1, IsaVersion.v80, InstFlags.CondRtRead),
                new(0x06600000, 0x0F700010, condConstraints, InstName.Strbt, T.StrbtA2, IsaVersion.v80, InstFlags.CondRtRead),
                new(0x04400000, 0x0E500000, condPwConstraints, InstName.StrbI, T.StrbIA1, IsaVersion.v80, InstFlags.CondRtReadWBack),
                new(0x06400000, 0x0E500010, condPwConstraints, InstName.StrbR, T.StrbRA1, IsaVersion.v80, InstFlags.CondRtReadWBack),
                new(0x004000F0, 0x0E5000F0, condConstraints, InstName.StrdI, T.StrdIA1, IsaVersion.v80, InstFlags.CondRt2ReadWBack),
                new(0x000000F0, 0x0E500FF0, condConstraints, InstName.StrdR, T.StrdRA1, IsaVersion.v80, InstFlags.CondRt2ReadWBack),
                new(0x01800F90, 0x0FF00FF0, condConstraints, InstName.Strex, T.StrexA1, IsaVersion.v80, InstFlags.CondRdRtRead),
                new(0x01C00F90, 0x0FF00FF0, condConstraints, InstName.Strexb, T.StrexbA1, IsaVersion.v80, InstFlags.CondRdRtRead),
                new(0x01A00F90, 0x0FF00FF0, condConstraints, InstName.Strexd, T.StrexdA1, IsaVersion.v80, InstFlags.CondRdRt2Read),
                new(0x01E00F90, 0x0FF00FF0, condConstraints, InstName.Strexh, T.StrexhA1, IsaVersion.v80, InstFlags.CondRdRtRead),
                new(0x006000B0, 0x0F7000F0, condConstraints, InstName.Strht, T.StrhtA1, IsaVersion.v80, InstFlags.CondRtRead),
                new(0x002000B0, 0x0F700FF0, condConstraints, InstName.Strht, T.StrhtA2, IsaVersion.v80, InstFlags.CondRtRead),
                new(0x004000B0, 0x0E5000F0, condPwConstraints, InstName.StrhI, T.StrhIA1, IsaVersion.v80, InstFlags.CondRtReadWBack),
                new(0x000000B0, 0x0E500FF0, condPwConstraints, InstName.StrhR, T.StrhRA1, IsaVersion.v80, InstFlags.CondRtReadWBack),
                new(0x04200000, 0x0F700000, condConstraints, InstName.Strt, T.StrtA1, IsaVersion.v80, InstFlags.CondRtRead),
                new(0x06200000, 0x0F700010, condConstraints, InstName.Strt, T.StrtA2, IsaVersion.v80, InstFlags.CondRtRead),
                new(0x04000000, 0x0E500000, condPwConstraints, InstName.StrI, T.StrIA1, IsaVersion.v80, InstFlags.CondRtReadWBack),
                new(0x06000000, 0x0E500010, condPwConstraints, InstName.StrR, T.StrRA1, IsaVersion.v80, InstFlags.CondRtReadWBack),
                new(0x02400000, 0x0FE00000, condRnsRnConstraints, InstName.SubI, T.SubIA1, IsaVersion.v80, InstFlags.CondRd),
                new(0x00400000, 0x0FE00010, condRnConstraints, InstName.SubR, T.SubRA1, IsaVersion.v80, InstFlags.CondRd),
                new(0x00400010, 0x0FE00090, condConstraints, InstName.SubRr, T.SubRrA1, IsaVersion.v80, InstFlags.CondRd),
                new(0x024D0000, 0x0FEF0000, condConstraints, InstName.SubSpI, T.SubSpIA1, IsaVersion.v80, InstFlags.CondRd),
                new(0x004D0000, 0x0FEF0010, condConstraints, InstName.SubSpR, T.SubSpRA1, IsaVersion.v80, InstFlags.CondRd),
                new(0x0F000000, 0x0F000000, condConstraints, InstName.Svc, T.SvcA1, IsaVersion.v80, InstFlags.Cond),
                new(0x06A00070, 0x0FF003F0, condRnConstraints3, InstName.Sxtab, T.SxtabA1, IsaVersion.v80, InstFlags.CondRd),
                new(0x06800070, 0x0FF003F0, condRnConstraints3, InstName.Sxtab16, T.Sxtab16A1, IsaVersion.v80, InstFlags.CondRd),
                new(0x06B00070, 0x0FF003F0, condRnConstraints3, InstName.Sxtah, T.SxtahA1, IsaVersion.v80, InstFlags.CondRd),
                new(0x06AF0070, 0x0FFF03F0, condConstraints, InstName.Sxtb, T.SxtbA1, IsaVersion.v80, InstFlags.CondRd),
                new(0x068F0070, 0x0FFF03F0, condConstraints, InstName.Sxtb16, T.Sxtb16A1, IsaVersion.v80, InstFlags.CondRd),
                new(0x06BF0070, 0x0FFF03F0, condConstraints, InstName.Sxth, T.SxthA1, IsaVersion.v80, InstFlags.CondRd),
                new(0x03300000, 0x0FF0F000, condConstraints, InstName.TeqI, T.TeqIA1, IsaVersion.v80, InstFlags.Cond),
                new(0x01300000, 0x0FF0F010, condConstraints, InstName.TeqR, T.TeqRA1, IsaVersion.v80, InstFlags.Cond),
                new(0x01300010, 0x0FF0F090, condConstraints, InstName.TeqRr, T.TeqRrA1, IsaVersion.v80, InstFlags.Cond),
                new(0x0320F012, 0x0FFFFFFF, condConstraints, InstName.Tsb, T.TsbA1, IsaVersion.v84, IsaFeature.FeatTrf, InstFlags.Cond),
                new(0x03100000, 0x0FF0F000, condConstraints, InstName.TstI, T.TstIA1, IsaVersion.v80, InstFlags.Cond),
                new(0x01100000, 0x0FF0F010, condConstraints, InstName.TstR, T.TstRA1, IsaVersion.v80, InstFlags.Cond),
                new(0x01100010, 0x0FF0F090, condConstraints, InstName.TstRr, T.TstRrA1, IsaVersion.v80, InstFlags.Cond),
                new(0x06500F10, 0x0FF00FF0, condConstraints, InstName.Uadd16, T.Uadd16A1, IsaVersion.v80, InstFlags.CondRd),
                new(0x06500F90, 0x0FF00FF0, condConstraints, InstName.Uadd8, T.Uadd8A1, IsaVersion.v80, InstFlags.CondRd),
                new(0x06500F30, 0x0FF00FF0, condConstraints, InstName.Uasx, T.UasxA1, IsaVersion.v80, InstFlags.CondRd),
                new(0x07E00050, 0x0FE00070, condConstraints, InstName.Ubfx, T.UbfxA1, IsaVersion.v80, InstFlags.CondRd),
                new(0xE7F000F0, 0xFFF000F0, InstName.Udf, T.UdfA1, IsaVersion.v80, InstFlags.None),
                new(0x0730F010, 0x0FF0F0F0, condConstraints, InstName.Udiv, T.UdivA1, IsaVersion.v80, InstFlags.CondRd16),
                new(0x06700F10, 0x0FF00FF0, condConstraints, InstName.Uhadd16, T.Uhadd16A1, IsaVersion.v80, InstFlags.CondRd),
                new(0x06700F90, 0x0FF00FF0, condConstraints, InstName.Uhadd8, T.Uhadd8A1, IsaVersion.v80, InstFlags.CondRd),
                new(0x06700F30, 0x0FF00FF0, condConstraints, InstName.Uhasx, T.UhasxA1, IsaVersion.v80, InstFlags.CondRd),
                new(0x06700F50, 0x0FF00FF0, condConstraints, InstName.Uhsax, T.UhsaxA1, IsaVersion.v80, InstFlags.CondRd),
                new(0x06700F70, 0x0FF00FF0, condConstraints, InstName.Uhsub16, T.Uhsub16A1, IsaVersion.v80, InstFlags.CondRd),
                new(0x06700FF0, 0x0FF00FF0, condConstraints, InstName.Uhsub8, T.Uhsub8A1, IsaVersion.v80, InstFlags.CondRd),
                new(0x00400090, 0x0FF000F0, condConstraints, InstName.Umaal, T.UmaalA1, IsaVersion.v80, InstFlags.CondRdLoHi),
                new(0x00A00090, 0x0FE000F0, condConstraints, InstName.Umlal, T.UmlalA1, IsaVersion.v80, InstFlags.CondRdLoHi),
                new(0x00800090, 0x0FE000F0, condConstraints, InstName.Umull, T.UmullA1, IsaVersion.v80, InstFlags.CondRdLoHi),
                new(0x06600F10, 0x0FF00FF0, condConstraints, InstName.Uqadd16, T.Uqadd16A1, IsaVersion.v80, InstFlags.CondRd),
                new(0x06600F90, 0x0FF00FF0, condConstraints, InstName.Uqadd8, T.Uqadd8A1, IsaVersion.v80, InstFlags.CondRd),
                new(0x06600F30, 0x0FF00FF0, condConstraints, InstName.Uqasx, T.UqasxA1, IsaVersion.v80, InstFlags.CondRd),
                new(0x06600F50, 0x0FF00FF0, condConstraints, InstName.Uqsax, T.UqsaxA1, IsaVersion.v80, InstFlags.CondRd),
                new(0x06600F70, 0x0FF00FF0, condConstraints, InstName.Uqsub16, T.Uqsub16A1, IsaVersion.v80, InstFlags.CondRd),
                new(0x06600FF0, 0x0FF00FF0, condConstraints, InstName.Uqsub8, T.Uqsub8A1, IsaVersion.v80, InstFlags.CondRd),
                new(0x0780F010, 0x0FF0F0F0, condConstraints, InstName.Usad8, T.Usad8A1, IsaVersion.v80, InstFlags.CondRd16),
                new(0x07800010, 0x0FF000F0, condRaConstraints, InstName.Usada8, T.Usada8A1, IsaVersion.v80, InstFlags.CondRd16),
                new(0x06E00010, 0x0FE00030, condConstraints, InstName.Usat, T.UsatA1, IsaVersion.v80, InstFlags.CondRd),
                new(0x06E00F30, 0x0FF00FF0, condConstraints, InstName.Usat16, T.Usat16A1, IsaVersion.v80, InstFlags.CondRd),
                new(0x06500F50, 0x0FF00FF0, condConstraints, InstName.Usax, T.UsaxA1, IsaVersion.v80, InstFlags.CondRd),
                new(0x06500F70, 0x0FF00FF0, condConstraints, InstName.Usub16, T.Usub16A1, IsaVersion.v80, InstFlags.CondRd),
                new(0x06500FF0, 0x0FF00FF0, condConstraints, InstName.Usub8, T.Usub8A1, IsaVersion.v80, InstFlags.CondRd),
                new(0x06E00070, 0x0FF003F0, condRnConstraints3, InstName.Uxtab, T.UxtabA1, IsaVersion.v80, InstFlags.CondRd),
                new(0x06C00070, 0x0FF003F0, condRnConstraints3, InstName.Uxtab16, T.Uxtab16A1, IsaVersion.v80, InstFlags.CondRd),
                new(0x06F00070, 0x0FF003F0, condRnConstraints3, InstName.Uxtah, T.UxtahA1, IsaVersion.v80, InstFlags.CondRd),
                new(0x06EF0070, 0x0FFF03F0, condConstraints, InstName.Uxtb, T.UxtbA1, IsaVersion.v80, InstFlags.CondRd),
                new(0x06CF0070, 0x0FFF03F0, condConstraints, InstName.Uxtb16, T.Uxtb16A1, IsaVersion.v80, InstFlags.CondRd),
                new(0x06FF0070, 0x0FFF03F0, condConstraints, InstName.Uxth, T.UxthA1, IsaVersion.v80, InstFlags.CondRd),
                new(0xF2000710, 0xFE800F10, sizeQvdQvnQvmConstraints, InstName.Vaba, T.VabaA1, IsaVersion.v80, InstFlags.None),
                new(0xF2800500, 0xFE800F50, sizeVdConstraints, InstName.Vabal, T.VabalA1, IsaVersion.v80, InstFlags.None),
                new(0xF2800700, 0xFE800F50, sizeVdConstraints, InstName.VabdlI, T.VabdlIA1, IsaVersion.v80, InstFlags.None),
                new(0xF3200D00, 0xFFB00F10, qvdQvnQvmConstraints, InstName.VabdF, T.VabdFA1, IsaVersion.v80, InstFlags.None),
                new(0xF3300D00, 0xFFB00F10, qvdQvnQvmConstraints, InstName.VabdF, T.VabdFA1, IsaVersion.v80, IsaFeature.FeatFp16, InstFlags.None),
                new(0xF2000700, 0xFE800F10, sizeQvdQvnQvmConstraints, InstName.VabdI, T.VabdIA1, IsaVersion.v80, InstFlags.None),
                new(0xF3B10300, 0xFFB30F90, sizeQvdQvmConstraints, InstName.Vabs, T.VabsA1, IsaVersion.v80, InstFlags.None),
                new(0xF3B90700, 0xFFBB0F90, sizeQvdQvmConstraints, InstName.Vabs, T.VabsA1, IsaVersion.v80, InstFlags.None),
                new(0xF3B50700, 0xFFBF0F90, sizeQvdQvmConstraints, InstName.Vabs, T.VabsA1, IsaVersion.v80, IsaFeature.FeatFp16, InstFlags.None),
                new(0x0EB00AC0, 0x0FBF0ED0, condConstraints, InstName.Vabs, T.VabsA2, IsaVersion.v80, InstFlags.Cond),
                new(0x0EB009C0, 0x0FBF0FD0, condConstraints, InstName.Vabs, T.VabsA2, IsaVersion.v80, IsaFeature.FeatFp16, InstFlags.Cond),
                new(0xF3000E10, 0xFFB00F10, qvdQvnQvmConstraints, InstName.Vacge, T.VacgeA1, IsaVersion.v80, InstFlags.None),
                new(0xF3100E10, 0xFFB00F10, qvdQvnQvmConstraints, InstName.Vacge, T.VacgeA1, IsaVersion.v80, IsaFeature.FeatFp16, InstFlags.None),
                new(0xF3200E10, 0xFFB00F10, qvdQvnQvmConstraints, InstName.Vacgt, T.VacgtA1, IsaVersion.v80, InstFlags.None),
                new(0xF3300E10, 0xFFB00F10, qvdQvnQvmConstraints, InstName.Vacgt, T.VacgtA1, IsaVersion.v80, IsaFeature.FeatFp16, InstFlags.None),
                new(0xF2800400, 0xFF800F50, sizeVnVmConstraints, InstName.Vaddhn, T.VaddhnA1, IsaVersion.v80, InstFlags.None),
                new(0xF2800000, 0xFE800F50, sizeVdOpvnConstraints, InstName.Vaddl, T.VaddlA1, IsaVersion.v80, InstFlags.None),
                new(0xF2800100, 0xFE800F50, sizeVdOpvnConstraints, InstName.Vaddw, T.VaddwA1, IsaVersion.v80, InstFlags.None),
                new(0xF2000D00, 0xFFB00F10, qvdQvnQvmConstraints, InstName.VaddF, T.VaddFA1, IsaVersion.v80, InstFlags.None),
                new(0xF2100D00, 0xFFB00F10, qvdQvnQvmConstraints, InstName.VaddF, T.VaddFA1, IsaVersion.v80, IsaFeature.FeatFp16, InstFlags.None),
                new(0x0E300A00, 0x0FB00E50, condConstraints, InstName.VaddF, T.VaddFA2, IsaVersion.v80, InstFlags.Cond),
                new(0x0E300900, 0x0FB00F50, condConstraints, InstName.VaddF, T.VaddFA2, IsaVersion.v80, IsaFeature.FeatFp16, InstFlags.Cond),
                new(0xF2000800, 0xFF800F10, qvdQvnQvmConstraints, InstName.VaddI, T.VaddIA1, IsaVersion.v80, InstFlags.None),
                new(0xF2000110, 0xFFB00F10, qvdQvnQvmConstraints, InstName.VandR, T.VandRA1, IsaVersion.v80, InstFlags.None),
                new(0xF2800130, 0xFEB809B0, cmodeCmodeQvdConstraints, InstName.VbicI, T.VbicIA1, IsaVersion.v80, InstFlags.None),
                new(0xF2800930, 0xFEB80DB0, cmodeCmodeQvdConstraints, InstName.VbicI, T.VbicIA2, IsaVersion.v80, InstFlags.None),
                new(0xF2100110, 0xFFB00F10, qvdQvnQvmConstraints, InstName.VbicR, T.VbicRA1, IsaVersion.v80, InstFlags.None),
                new(0xF3300110, 0xFFB00F10, qvdQvnQvmOpConstraints, InstName.Vbif, T.VbifA1, IsaVersion.v80, InstFlags.None),
                new(0xF3200110, 0xFFB00F10, qvdQvnQvmOpConstraints, InstName.Vbit, T.VbitA1, IsaVersion.v80, InstFlags.None),
                new(0xF3100110, 0xFFB00F10, qvdQvnQvmOpConstraints, InstName.Vbsl, T.VbslA1, IsaVersion.v80, InstFlags.None),
                new(0xFC900800, 0xFEB00F10, qvdQvnQvmConstraints, InstName.Vcadd, T.VcaddA1, IsaVersion.v83, IsaFeature.FeatFcma, InstFlags.None),
                new(0xFC800800, 0xFEB00F10, qvdQvnQvmConstraints, InstName.Vcadd, T.VcaddA1, IsaVersion.v83, IsaFeature.FeatFcma | IsaFeature.FeatFp16, InstFlags.None),
                new(0xF3B10100, 0xFFB30F90, sizeQvdQvmConstraints, InstName.VceqI, T.VceqIA1, IsaVersion.v80, InstFlags.None),
                new(0xF3B90500, 0xFFBB0F90, sizeQvdQvmConstraints, InstName.VceqI, T.VceqIA1, IsaVersion.v80, InstFlags.None),
                new(0xF3B50500, 0xFFBF0F90, sizeQvdQvmConstraints, InstName.VceqI, T.VceqIA1, IsaVersion.v80, IsaFeature.FeatFp16, InstFlags.None),
                new(0xF3000810, 0xFF800F10, qvdQvnQvmSizeConstraints, InstName.VceqR, T.VceqRA1, IsaVersion.v80, InstFlags.None),
                new(0xF2000E00, 0xFFB00F10, qvdQvnQvmConstraints, InstName.VceqR, T.VceqRA2, IsaVersion.v80, InstFlags.None),
                new(0xF2100E00, 0xFFB00F10, qvdQvnQvmConstraints, InstName.VceqR, T.VceqRA2, IsaVersion.v80, IsaFeature.FeatFp16, InstFlags.None),
                new(0xF3B10080, 0xFFB30F90, sizeQvdQvmConstraints, InstName.VcgeI, T.VcgeIA1, IsaVersion.v80, InstFlags.None),
                new(0xF3B90480, 0xFFBB0F90, sizeQvdQvmConstraints, InstName.VcgeI, T.VcgeIA1, IsaVersion.v80, InstFlags.None),
                new(0xF3B50480, 0xFFBF0F90, sizeQvdQvmConstraints, InstName.VcgeI, T.VcgeIA1, IsaVersion.v80, IsaFeature.FeatFp16, InstFlags.None),
                new(0xF2000310, 0xFE800F10, qvdQvnQvmSizeConstraints, InstName.VcgeR, T.VcgeRA1, IsaVersion.v80, InstFlags.None),
                new(0xF3000E00, 0xFFB00F10, qvdQvnQvmConstraints, InstName.VcgeR, T.VcgeRA2, IsaVersion.v80, InstFlags.None),
                new(0xF3100E00, 0xFFB00F10, qvdQvnQvmConstraints, InstName.VcgeR, T.VcgeRA2, IsaVersion.v80, IsaFeature.FeatFp16, InstFlags.None),
                new(0xF3B10000, 0xFFB30F90, sizeQvdQvmConstraints, InstName.VcgtI, T.VcgtIA1, IsaVersion.v80, InstFlags.None),
                new(0xF3B90400, 0xFFBB0F90, sizeQvdQvmConstraints, InstName.VcgtI, T.VcgtIA1, IsaVersion.v80, InstFlags.None),
                new(0xF3B50400, 0xFFBF0F90, sizeQvdQvmConstraints, InstName.VcgtI, T.VcgtIA1, IsaVersion.v80, IsaFeature.FeatFp16, InstFlags.None),
                new(0xF2000300, 0xFE800F10, qvdQvnQvmSizeConstraints, InstName.VcgtR, T.VcgtRA1, IsaVersion.v80, InstFlags.None),
                new(0xF3200E00, 0xFFB00F10, qvdQvnQvmConstraints, InstName.VcgtR, T.VcgtRA2, IsaVersion.v80, InstFlags.None),
                new(0xF3300E00, 0xFFB00F10, qvdQvnQvmConstraints, InstName.VcgtR, T.VcgtRA2, IsaVersion.v80, IsaFeature.FeatFp16, InstFlags.None),
                new(0xF3B10180, 0xFFB30F90, sizeQvdQvmConstraints, InstName.VcleI, T.VcleIA1, IsaVersion.v80, InstFlags.None),
                new(0xF3B90580, 0xFFBB0F90, sizeQvdQvmConstraints, InstName.VcleI, T.VcleIA1, IsaVersion.v80, InstFlags.None),
                new(0xF3B50580, 0xFFBF0F90, sizeQvdQvmConstraints, InstName.VcleI, T.VcleIA1, IsaVersion.v80, IsaFeature.FeatFp16, InstFlags.None),
                new(0xF3B00400, 0xFFB30F90, sizeQvdQvmConstraints, InstName.Vcls, T.VclsA1, IsaVersion.v80, InstFlags.None),
                new(0xF3B10200, 0xFFB30F90, sizeQvdQvmConstraints, InstName.VcltI, T.VcltIA1, IsaVersion.v80, InstFlags.None),
                new(0xF3B90600, 0xFFBB0F90, sizeQvdQvmConstraints, InstName.VcltI, T.VcltIA1, IsaVersion.v80, InstFlags.None),
                new(0xF3B50600, 0xFFBF0F90, sizeQvdQvmConstraints, InstName.VcltI, T.VcltIA1, IsaVersion.v80, IsaFeature.FeatFp16, InstFlags.None),
                new(0xF3B00480, 0xFFB30F90, sizeQvdQvmConstraints, InstName.Vclz, T.VclzA1, IsaVersion.v80, InstFlags.None),
                new(0xFC200800, 0xFE200F10, qvdQvnQvmConstraints, InstName.Vcmla, T.VcmlaA1, IsaVersion.v83, IsaFeature.FeatFcma, InstFlags.None),
                new(0xFE000800, 0xFF000F10, qvdQvnConstraints, InstName.VcmlaS, T.VcmlaSA1, IsaVersion.v83, IsaFeature.FeatFcma, InstFlags.None),
                new(0x0EB40A40, 0x0FBF0ED0, condConstraints, InstName.Vcmp, T.VcmpA1, IsaVersion.v80, InstFlags.Cond),
                new(0x0EB40940, 0x0FBF0FD0, condConstraints, InstName.Vcmp, T.VcmpA1, IsaVersion.v80, IsaFeature.FeatFp16, InstFlags.Cond),
                new(0x0EB50A40, 0x0FBF0EFF, condConstraints, InstName.Vcmp, T.VcmpA2, IsaVersion.v80, InstFlags.Cond),
                new(0x0EB50940, 0x0FBF0FFF, condConstraints, InstName.Vcmp, T.VcmpA2, IsaVersion.v80, IsaFeature.FeatFp16, InstFlags.Cond),
                new(0x0EB40AC0, 0x0FBF0ED0, condConstraints, InstName.Vcmpe, T.VcmpeA1, IsaVersion.v80, InstFlags.Cond),
                new(0x0EB409C0, 0x0FBF0FD0, condConstraints, InstName.Vcmpe, T.VcmpeA1, IsaVersion.v80, IsaFeature.FeatFp16, InstFlags.Cond),
                new(0x0EB50AC0, 0x0FBF0EFF, condConstraints, InstName.Vcmpe, T.VcmpeA2, IsaVersion.v80, InstFlags.Cond),
                new(0x0EB509C0, 0x0FBF0FFF, condConstraints, InstName.Vcmpe, T.VcmpeA2, IsaVersion.v80, IsaFeature.FeatFp16, InstFlags.Cond),
                new(0xF3B00500, 0xFFBF0F90, qvdQvmConstraints, InstName.Vcnt, T.VcntA1, IsaVersion.v80, InstFlags.None),
                new(0xF3BB0000, 0xFFBF0F10, qvdQvmConstraints, InstName.VcvtaAsimd, T.VcvtaAsimdA1, IsaVersion.v80, InstFlags.None),
                new(0xF3B70000, 0xFFBF0F10, qvdQvmConstraints, InstName.VcvtaAsimd, T.VcvtaAsimdA1, IsaVersion.v80, IsaFeature.FeatFp16, InstFlags.None),
                new(0xFEBC0A40, 0xFFBF0E50, sizeConstraints, InstName.VcvtaVfp, T.VcvtaVfpA1, IsaVersion.v80, InstFlags.None),
                new(0xFEBC0940, 0xFFBF0F50, sizeConstraints, InstName.VcvtaVfp, T.VcvtaVfpA1, IsaVersion.v80, IsaFeature.FeatFp16, InstFlags.None),
                new(0x0EB20A40, 0x0FBE0ED0, condConstraints, InstName.Vcvtb, T.VcvtbA1, IsaVersion.v80, InstFlags.Cond),
                new(0x0EB30940, 0x0FBF0FD0, condConstraints, InstName.VcvtbBfs, T.VcvtbBfsA1, IsaVersion.v86, IsaFeature.FeatAa32bf16, InstFlags.Cond),
                new(0xF3BB0300, 0xFFBF0F10, qvdQvmConstraints, InstName.VcvtmAsimd, T.VcvtmAsimdA1, IsaVersion.v80, InstFlags.None),
                new(0xF3B70300, 0xFFBF0F10, qvdQvmConstraints, InstName.VcvtmAsimd, T.VcvtmAsimdA1, IsaVersion.v80, IsaFeature.FeatFp16, InstFlags.None),
                new(0xFEBF0A40, 0xFFBF0E50, sizeConstraints, InstName.VcvtmVfp, T.VcvtmVfpA1, IsaVersion.v80, InstFlags.None),
                new(0xFEBF0940, 0xFFBF0F50, sizeConstraints, InstName.VcvtmVfp, T.VcvtmVfpA1, IsaVersion.v80, IsaFeature.FeatFp16, InstFlags.None),
                new(0xF3BB0100, 0xFFBF0F10, qvdQvmConstraints, InstName.VcvtnAsimd, T.VcvtnAsimdA1, IsaVersion.v80, InstFlags.None),
                new(0xF3B70100, 0xFFBF0F10, qvdQvmConstraints, InstName.VcvtnAsimd, T.VcvtnAsimdA1, IsaVersion.v80, IsaFeature.FeatFp16, InstFlags.None),
                new(0xFEBD0A40, 0xFFBF0E50, sizeConstraints, InstName.VcvtnVfp, T.VcvtnVfpA1, IsaVersion.v80, InstFlags.None),
                new(0xFEBD0940, 0xFFBF0F50, sizeConstraints, InstName.VcvtnVfp, T.VcvtnVfpA1, IsaVersion.v80, IsaFeature.FeatFp16, InstFlags.None),
                new(0xF3BB0200, 0xFFBF0F10, qvdQvmConstraints, InstName.VcvtpAsimd, T.VcvtpAsimdA1, IsaVersion.v80, InstFlags.None),
                new(0xF3B70200, 0xFFBF0F10, qvdQvmConstraints, InstName.VcvtpAsimd, T.VcvtpAsimdA1, IsaVersion.v80, IsaFeature.FeatFp16, InstFlags.None),
                new(0xFEBE0A40, 0xFFBF0E50, sizeConstraints, InstName.VcvtpVfp, T.VcvtpVfpA1, IsaVersion.v80, InstFlags.None),
                new(0xFEBE0940, 0xFFBF0F50, sizeConstraints, InstName.VcvtpVfp, T.VcvtpVfpA1, IsaVersion.v80, IsaFeature.FeatFp16, InstFlags.None),
                new(0x0EBC0A40, 0x0FBE0ED0, condConstraints, InstName.VcvtrIv, T.VcvtrIvA1, IsaVersion.v80, InstFlags.Cond),
                new(0x0EBC0940, 0x0FBE0FD0, condConstraints, InstName.VcvtrIv, T.VcvtrIvA1, IsaVersion.v80, IsaFeature.FeatFp16, InstFlags.Cond),
                new(0x0EB20AC0, 0x0FBE0ED0, condConstraints, InstName.Vcvtt, T.VcvttA1, IsaVersion.v80, InstFlags.Cond),
                new(0x0EB309C0, 0x0FBF0FD0, condConstraints, InstName.VcvttBfs, T.VcvttBfsA1, IsaVersion.v86, IsaFeature.FeatAa32bf16, InstFlags.Cond),
                new(0xF3B60640, 0xFFBF0FD0, vmConstraints, InstName.VcvtBfs, T.VcvtBfsA1, IsaVersion.v86, IsaFeature.FeatAa32bf16, InstFlags.None),
                new(0x0EB70AC0, 0x0FBF0ED0, condConstraints, InstName.VcvtDs, T.VcvtDsA1, IsaVersion.v80, InstFlags.Cond),
                new(0xF3B60600, 0xFFBF0ED0, opvdOpvmConstraints, InstName.VcvtHs, T.VcvtHsA1, IsaVersion.v80, InstFlags.None),
                new(0xF3BB0600, 0xFFBF0E10, qvdQvmConstraints, InstName.VcvtIs, T.VcvtIsA1, IsaVersion.v80, InstFlags.None),
                new(0xF3B70600, 0xFFBF0E10, qvdQvmConstraints, InstName.VcvtIs, T.VcvtIsA1, IsaVersion.v80, IsaFeature.FeatFp16, InstFlags.None),
                new(0x0EBC0AC0, 0x0FBE0ED0, condConstraints, InstName.VcvtIv, T.VcvtIvA1, IsaVersion.v80, InstFlags.Cond),
                new(0x0EBC09C0, 0x0FBE0FD0, condConstraints, InstName.VcvtIv, T.VcvtIvA1, IsaVersion.v80, IsaFeature.FeatFp16, InstFlags.Cond),
                new(0x0EB80A40, 0x0FBF0E50, condConstraints, InstName.VcvtVi, T.VcvtViA1, IsaVersion.v80, InstFlags.Cond),
                new(0x0EB80940, 0x0FBF0F50, condConstraints, InstName.VcvtVi, T.VcvtViA1, IsaVersion.v80, IsaFeature.FeatFp16, InstFlags.Cond),
                new(0xF2800E10, 0xFE800E90, imm6Opimm6Imm6QvdQvmConstraints, InstName.VcvtXs, T.VcvtXsA1, IsaVersion.v80, InstFlags.None),
                new(0xF2800C10, 0xFE800E90, imm6Opimm6Imm6QvdQvmConstraints, InstName.VcvtXs, T.VcvtXsA1, IsaVersion.v80, IsaFeature.FeatFp16, InstFlags.None),
                new(0x0EBA0A40, 0x0FBA0E50, condConstraints, InstName.VcvtXv, T.VcvtXvA1, IsaVersion.v80, InstFlags.Cond),
                new(0x0EBA0940, 0x0FBA0F50, condConstraints, InstName.VcvtXv, T.VcvtXvA1, IsaVersion.v80, IsaFeature.FeatFp16, InstFlags.Cond),
                new(0x0E800A00, 0x0FB00E50, condConstraints, InstName.Vdiv, T.VdivA1, IsaVersion.v80, InstFlags.Cond),
                new(0x0E800900, 0x0FB00F50, condConstraints, InstName.Vdiv, T.VdivA1, IsaVersion.v80, IsaFeature.FeatFp16, InstFlags.Cond),
                new(0xFC000D00, 0xFFB00F10, qvdQvnQvmConstraints, InstName.Vdot, T.VdotA1, IsaVersion.v86, IsaFeature.FeatAa32bf16, InstFlags.None),
                new(0xFE000D00, 0xFFB00F10, qvdQvnConstraints, InstName.VdotS, T.VdotSA1, IsaVersion.v86, IsaFeature.FeatAa32bf16, InstFlags.None),
                new(0x0E800B10, 0x0F900F5F, condQvdEbConstraints, InstName.VdupR, T.VdupRA1, IsaVersion.v80, InstFlags.CondRtRead),
                new(0xF3B00C00, 0xFFB00F90, imm4QvdConstraints, InstName.VdupS, T.VdupSA1, IsaVersion.v80, InstFlags.None),
                new(0xF3000110, 0xFFB00F10, qvdQvnQvmConstraints, InstName.Veor, T.VeorA1, IsaVersion.v80, InstFlags.None),
                new(0xF2B00000, 0xFFB00010, qvdQvnQvmQimm4Constraints, InstName.Vext, T.VextA1, IsaVersion.v80, InstFlags.None),
                new(0xF2000C10, 0xFFB00F10, qvdQvnQvmConstraints, InstName.Vfma, T.VfmaA1, IsaVersion.v80, InstFlags.None),
                new(0xF2100C10, 0xFFB00F10, qvdQvnQvmConstraints, InstName.Vfma, T.VfmaA1, IsaVersion.v80, IsaFeature.FeatFp16, InstFlags.None),
                new(0x0EA00A00, 0x0FB00E50, condConstraints, InstName.Vfma, T.VfmaA2, IsaVersion.v80, InstFlags.Cond),
                new(0x0EA00900, 0x0FB00F50, condConstraints, InstName.Vfma, T.VfmaA2, IsaVersion.v80, IsaFeature.FeatFp16, InstFlags.Cond),
                new(0xFC200810, 0xFFB00F10, qvdConstraints, InstName.Vfmal, T.VfmalA1, IsaVersion.v82, IsaFeature.FeatFhm, InstFlags.None),
                new(0xFE000810, 0xFFB00F10, qvdConstraints, InstName.VfmalS, T.VfmalSA1, IsaVersion.v82, IsaFeature.FeatFhm, InstFlags.None),
                new(0xFC300810, 0xFFB00F10, vdVnVmConstraints, InstName.VfmaBf, T.VfmaBfA1, IsaVersion.v86, IsaFeature.FeatAa32bf16, InstFlags.None),
                new(0xFE300810, 0xFFB00F10, vdVnConstraints, InstName.VfmaBfs, T.VfmaBfsA1, IsaVersion.v86, IsaFeature.FeatAa32bf16, InstFlags.None),
                new(0xF2200C10, 0xFFB00F10, qvdQvnQvmConstraints, InstName.Vfms, T.VfmsA1, IsaVersion.v80, InstFlags.None),
                new(0xF2300C10, 0xFFB00F10, qvdQvnQvmConstraints, InstName.Vfms, T.VfmsA1, IsaVersion.v80, IsaFeature.FeatFp16, InstFlags.None),
                new(0x0EA00A40, 0x0FB00E50, condConstraints, InstName.Vfms, T.VfmsA2, IsaVersion.v80, InstFlags.Cond),
                new(0x0EA00940, 0x0FB00F50, condConstraints, InstName.Vfms, T.VfmsA2, IsaVersion.v80, IsaFeature.FeatFp16, InstFlags.Cond),
                new(0xFCA00810, 0xFFB00F10, qvdConstraints, InstName.Vfmsl, T.VfmslA1, IsaVersion.v82, IsaFeature.FeatFhm, InstFlags.None),
                new(0xFE100810, 0xFFB00F10, qvdConstraints, InstName.VfmslS, T.VfmslSA1, IsaVersion.v82, IsaFeature.FeatFhm, InstFlags.None),
                new(0x0E900A40, 0x0FB00E50, condConstraints, InstName.Vfnma, T.VfnmaA1, IsaVersion.v80, InstFlags.Cond),
                new(0x0E900940, 0x0FB00F50, condConstraints, InstName.Vfnma, T.VfnmaA1, IsaVersion.v80, IsaFeature.FeatFp16, InstFlags.Cond),
                new(0x0E900A00, 0x0FB00E50, condConstraints, InstName.Vfnms, T.VfnmsA1, IsaVersion.v80, InstFlags.Cond),
                new(0x0E900900, 0x0FB00F50, condConstraints, InstName.Vfnms, T.VfnmsA1, IsaVersion.v80, IsaFeature.FeatFp16, InstFlags.Cond),
                new(0xF2000000, 0xFE800F10, qvdQvnQvmSizeConstraints, InstName.Vhadd, T.VhaddA1, IsaVersion.v80, InstFlags.None),
                new(0xF2000200, 0xFE800F10, qvdQvnQvmSizeConstraints, InstName.Vhsub, T.VhsubA1, IsaVersion.v80, InstFlags.None),
                new(0xFEB00AC0, 0xFFBF0FD0, InstName.Vins, T.VinsA1, IsaVersion.v82, IsaFeature.FeatFp16, InstFlags.None),
                new(0x0EB90BC0, 0x0FBF0FD0, condConstraints, InstName.Vjcvt, T.VjcvtA1, IsaVersion.v83, IsaFeature.FeatJscvt, InstFlags.Cond),
                new(0xF4A00000, 0xFFB00F10, sizeConstraints2, InstName.Vld11, T.Vld11A1, IsaVersion.v80, InstFlags.None),
                new(0xF4A00400, 0xFFB00F20, sizeConstraints2, InstName.Vld11, T.Vld11A2, IsaVersion.v80, InstFlags.None),
                new(0xF4A00800, 0xFFB00F40, sizeIndexAlignIndexAlignConstraints, InstName.Vld11, T.Vld11A3, IsaVersion.v80, InstFlags.None),
                new(0xF4A00C00, 0xFFB00F00, sizeSizeaConstraints, InstName.Vld1A, T.Vld1AA1, IsaVersion.v80, InstFlags.None),
                new(0xF4200700, 0xFFB00F00, alignConstraints, InstName.Vld1M, T.Vld1MA1, IsaVersion.v80, InstFlags.None),
                new(0xF4200A00, 0xFFB00F00, alignConstraints2, InstName.Vld1M, T.Vld1MA2, IsaVersion.v80, InstFlags.None),
                new(0xF4200600, 0xFFB00F00, alignConstraints, InstName.Vld1M, T.Vld1MA3, IsaVersion.v80, InstFlags.None),
                new(0xF4200200, 0xFFB00F00, InstName.Vld1M, T.Vld1MA4, IsaVersion.v80, InstFlags.None),
                new(0xF4A00100, 0xFFB00F00, sizeConstraints2, InstName.Vld21, T.Vld21A1, IsaVersion.v80, InstFlags.None),
                new(0xF4A00500, 0xFFB00F00, sizeConstraints2, InstName.Vld21, T.Vld21A2, IsaVersion.v80, InstFlags.None),
                new(0xF4A00900, 0xFFB00F20, sizeConstraints2, InstName.Vld21, T.Vld21A3, IsaVersion.v80, InstFlags.None),
                new(0xF4A00D00, 0xFFB00F00, sizeConstraints3, InstName.Vld2A, T.Vld2AA1, IsaVersion.v80, InstFlags.None),
                new(0xF4200800, 0xFFB00E00, alignSizeConstraints, InstName.Vld2M, T.Vld2MA1, IsaVersion.v80, InstFlags.None),
                new(0xF4200300, 0xFFB00F00, sizeConstraints3, InstName.Vld2M, T.Vld2MA2, IsaVersion.v80, InstFlags.None),
                new(0xF4A00200, 0xFFB00F10, sizeConstraints2, InstName.Vld31, T.Vld31A1, IsaVersion.v80, InstFlags.None),
                new(0xF4A00600, 0xFFB00F10, sizeConstraints2, InstName.Vld31, T.Vld31A2, IsaVersion.v80, InstFlags.None),
                new(0xF4A00A00, 0xFFB00F30, sizeConstraints2, InstName.Vld31, T.Vld31A3, IsaVersion.v80, InstFlags.None),
                new(0xF4A00E00, 0xFFB00F10, sizeAConstraints, InstName.Vld3A, T.Vld3AA1, IsaVersion.v80, InstFlags.None),
                new(0xF4200400, 0xFFB00E00, sizeAlignConstraints, InstName.Vld3M, T.Vld3MA1, IsaVersion.v80, InstFlags.None),
                new(0xF4A00300, 0xFFB00F00, sizeConstraints2, InstName.Vld41, T.Vld41A1, IsaVersion.v80, InstFlags.None),
                new(0xF4A00700, 0xFFB00F00, sizeConstraints2, InstName.Vld41, T.Vld41A2, IsaVersion.v80, InstFlags.None),
                new(0xF4A00B00, 0xFFB00F00, sizeIndexAlignConstraints, InstName.Vld41, T.Vld41A3, IsaVersion.v80, InstFlags.None),
                new(0xF4A00F00, 0xFFB00F00, sizeaConstraints, InstName.Vld4A, T.Vld4AA1, IsaVersion.v80, InstFlags.None),
                new(0xF4200000, 0xFFB00E00, sizeConstraints3, InstName.Vld4M, T.Vld4MA1, IsaVersion.v80, InstFlags.None),
                new(0x0C100B00, 0x0E100F01, condPuwPwPuwPuwConstraints, InstName.Vldm, T.VldmA1, IsaVersion.v80, InstFlags.CondWBack),
                new(0x0C100A00, 0x0E100F00, condPuwPwPuwPuwConstraints, InstName.Vldm, T.VldmA2, IsaVersion.v80, InstFlags.CondWBack),
                new(0x0D100A00, 0x0F300E00, condRnConstraints3, InstName.VldrI, T.VldrIA1, IsaVersion.v80, InstFlags.Cond),
                new(0x0D100900, 0x0F300F00, condRnConstraints3, InstName.VldrI, T.VldrIA1, IsaVersion.v80, IsaFeature.FeatFp16, InstFlags.Cond),
                new(0x0D1F0A00, 0x0F3F0E00, condConstraints, InstName.VldrL, T.VldrLA1, IsaVersion.v80, InstFlags.Cond),
                new(0x0D1F0900, 0x0F3F0F00, condConstraints, InstName.VldrL, T.VldrLA1, IsaVersion.v80, IsaFeature.FeatFp16, InstFlags.Cond),
                new(0xF3000F10, 0xFFB00F10, qvdQvnQvmConstraints, InstName.Vmaxnm, T.VmaxnmA1, IsaVersion.v80, InstFlags.None),
                new(0xF3100F10, 0xFFB00F10, qvdQvnQvmConstraints, InstName.Vmaxnm, T.VmaxnmA1, IsaVersion.v80, IsaFeature.FeatFp16, InstFlags.None),
                new(0xFE800A00, 0xFFB00E50, sizeConstraints, InstName.Vmaxnm, T.VmaxnmA2, IsaVersion.v80, InstFlags.None),
                new(0xFE800900, 0xFFB00F50, sizeConstraints, InstName.Vmaxnm, T.VmaxnmA2, IsaVersion.v80, IsaFeature.FeatFp16, InstFlags.None),
                new(0xF2000F00, 0xFFB00F10, qvdQvnQvmConstraints, InstName.VmaxF, T.VmaxFA1, IsaVersion.v80, InstFlags.None),
                new(0xF2100F00, 0xFFB00F10, qvdQvnQvmConstraints, InstName.VmaxF, T.VmaxFA1, IsaVersion.v80, IsaFeature.FeatFp16, InstFlags.None),
                new(0xF2000600, 0xFE800F10, qvdQvnQvmSizeConstraints, InstName.VmaxI, T.VmaxIA1, IsaVersion.v80, InstFlags.None),
                new(0xF3200F10, 0xFFB00F10, qvdQvnQvmConstraints, InstName.Vminnm, T.VminnmA1, IsaVersion.v80, InstFlags.None),
                new(0xF3300F10, 0xFFB00F10, qvdQvnQvmConstraints, InstName.Vminnm, T.VminnmA1, IsaVersion.v80, IsaFeature.FeatFp16, InstFlags.None),
                new(0xFE800A40, 0xFFB00E50, sizeConstraints, InstName.Vminnm, T.VminnmA2, IsaVersion.v80, InstFlags.None),
                new(0xFE800940, 0xFFB00F50, sizeConstraints, InstName.Vminnm, T.VminnmA2, IsaVersion.v80, IsaFeature.FeatFp16, InstFlags.None),
                new(0xF2200F00, 0xFFB00F10, qvdQvnQvmConstraints, InstName.VminF, T.VminFA1, IsaVersion.v80, InstFlags.None),
                new(0xF2300F00, 0xFFB00F10, qvdQvnQvmConstraints, InstName.VminF, T.VminFA1, IsaVersion.v80, IsaFeature.FeatFp16, InstFlags.None),
                new(0xF2000610, 0xFE800F10, qvdQvnQvmSizeConstraints, InstName.VminI, T.VminIA1, IsaVersion.v80, InstFlags.None),
                new(0xF2800800, 0xFE800F50, sizeVdConstraints, InstName.VmlalI, T.VmlalIA1, IsaVersion.v80, InstFlags.None),
                new(0xF2800240, 0xFE800F50, sizeSizeVdConstraints, InstName.VmlalS, T.VmlalSA1, IsaVersion.v80, InstFlags.None),
                new(0xF2000D10, 0xFFB00F10, qvdQvnQvmConstraints, InstName.VmlaF, T.VmlaFA1, IsaVersion.v80, InstFlags.None),
                new(0xF2100D10, 0xFFB00F10, qvdQvnQvmConstraints, InstName.VmlaF, T.VmlaFA1, IsaVersion.v80, IsaFeature.FeatFp16, InstFlags.None),
                new(0x0E000A00, 0x0FB00E50, condConstraints, InstName.VmlaF, T.VmlaFA2, IsaVersion.v80, InstFlags.Cond),
                new(0x0E000900, 0x0FB00F50, condConstraints, InstName.VmlaF, T.VmlaFA2, IsaVersion.v80, IsaFeature.FeatFp16, InstFlags.Cond),
                new(0xF2000900, 0xFF800F10, sizeQvdQvnQvmConstraints, InstName.VmlaI, T.VmlaIA1, IsaVersion.v80, InstFlags.None),
                new(0xF2A00040, 0xFEA00E50, sizeQvdQvnConstraints, InstName.VmlaS, T.VmlaSA1, IsaVersion.v80, InstFlags.None),
                new(0xF2900040, 0xFEB00F50, sizeQvdQvnConstraints, InstName.VmlaS, T.VmlaSA1, IsaVersion.v80, InstFlags.None),
                new(0xF2900140, 0xFEB00F50, sizeQvdQvnConstraints, InstName.VmlaS, T.VmlaSA1, IsaVersion.v80, IsaFeature.FeatFp16, InstFlags.None),
                new(0xF2800A00, 0xFE800F50, sizeVdConstraints, InstName.VmlslI, T.VmlslIA1, IsaVersion.v80, InstFlags.None),
                new(0xF2800640, 0xFE800F50, sizeSizeVdConstraints, InstName.VmlslS, T.VmlslSA1, IsaVersion.v80, InstFlags.None),
                new(0xF2200D10, 0xFFB00F10, qvdQvnQvmConstraints, InstName.VmlsF, T.VmlsFA1, IsaVersion.v80, InstFlags.None),
                new(0xF2300D10, 0xFFB00F10, qvdQvnQvmConstraints, InstName.VmlsF, T.VmlsFA1, IsaVersion.v80, IsaFeature.FeatFp16, InstFlags.None),
                new(0x0E000A40, 0x0FB00E50, condConstraints, InstName.VmlsF, T.VmlsFA2, IsaVersion.v80, InstFlags.Cond),
                new(0x0E000940, 0x0FB00F50, condConstraints, InstName.VmlsF, T.VmlsFA2, IsaVersion.v80, IsaFeature.FeatFp16, InstFlags.Cond),
                new(0xF3000900, 0xFF800F10, sizeQvdQvnQvmConstraints, InstName.VmlsI, T.VmlsIA1, IsaVersion.v80, InstFlags.None),
                new(0xF2A00440, 0xFEA00E50, sizeQvdQvnConstraints, InstName.VmlsS, T.VmlsSA1, IsaVersion.v80, InstFlags.None),
                new(0xF2900440, 0xFEB00F50, sizeQvdQvnConstraints, InstName.VmlsS, T.VmlsSA1, IsaVersion.v80, InstFlags.None),
                new(0xF2900540, 0xFEB00F50, sizeQvdQvnConstraints, InstName.VmlsS, T.VmlsSA1, IsaVersion.v80, IsaFeature.FeatFp16, InstFlags.None),
                new(0xFC000C40, 0xFFB00F50, vdVnVmConstraints, InstName.Vmmla, T.VmmlaA1, IsaVersion.v86, IsaFeature.FeatAa32bf16, InstFlags.None),
                new(0xF2800A10, 0xFE870FD0, imm3hImm3hImm3hImm3hImm3hVdConstraints, InstName.Vmovl, T.VmovlA1, IsaVersion.v80, InstFlags.None),
                new(0xF3B20200, 0xFFB30FD0, sizeVmConstraints, InstName.Vmovn, T.VmovnA1, IsaVersion.v80, InstFlags.None),
                new(0xFEB00A40, 0xFFBF0FD0, InstName.Vmovx, T.VmovxA1, IsaVersion.v82, IsaFeature.FeatFp16, InstFlags.None),
                new(0x0C400B10, 0x0FE00FD0, condConstraints, InstName.VmovD, T.VmovDA1, IsaVersion.v80, InstFlags.CondRt2Read),
                new(0x0E000910, 0x0FE00F7F, condConstraints, InstName.VmovH, T.VmovHA1, IsaVersion.v82, IsaFeature.FeatFp16, InstFlags.CondRt),
                new(0xF2800010, 0xFEB809B0, qvdConstraints, InstName.VmovI, T.VmovIA1, IsaVersion.v80, InstFlags.None),
                new(0x0EB00A00, 0x0FB00EF0, condConstraints, InstName.VmovI, T.VmovIA2, IsaVersion.v80, InstFlags.Cond),
                new(0x0EB00900, 0x0FB00FF0, condConstraints, InstName.VmovI, T.VmovIA2, IsaVersion.v80, IsaFeature.FeatFp16, InstFlags.Cond),
                new(0xF2800810, 0xFEB80DB0, qvdConstraints, InstName.VmovI, T.VmovIA3, IsaVersion.v80, InstFlags.None),
                new(0xF2800C10, 0xFEB80CB0, qvdConstraints, InstName.VmovI, T.VmovIA4, IsaVersion.v80, InstFlags.None),
                new(0xF2800E30, 0xFEB80FB0, qvdConstraints, InstName.VmovI, T.VmovIA5, IsaVersion.v80, InstFlags.None),
                new(0x0EB00A40, 0x0FBF0ED0, condConstraints, InstName.VmovR, T.VmovRA2, IsaVersion.v80, InstFlags.Cond),
                new(0x0E000B10, 0x0F900F1F, condOpc1opc2Constraints, InstName.VmovRs, T.VmovRsA1, IsaVersion.v80, InstFlags.CondRtRead),
                new(0x0E000A10, 0x0FE00F7F, condConstraints, InstName.VmovS, T.VmovSA1, IsaVersion.v80, InstFlags.CondRtRead),
                new(0x0E100B10, 0x0F100F1F, condUopc1opc2Uopc1opc2Constraints, InstName.VmovSr, T.VmovSrA1, IsaVersion.v80, InstFlags.CondRt),
                new(0x0C400A10, 0x0FE00FD0, condConstraints, InstName.VmovSs, T.VmovSsA1, IsaVersion.v80, InstFlags.CondRt2Read),
                new(0x0EF00A10, 0x0FF00FFF, condConstraints, InstName.Vmrs, T.VmrsA1, IsaVersion.v80, InstFlags.CondRt),
                new(0x0EE00A10, 0x0FF00FFF, condConstraints, InstName.Vmsr, T.VmsrA1, IsaVersion.v80, InstFlags.CondRtRead),
                new(0xF2800C00, 0xFE800D50, sizeOpuOpsizeVdConstraints, InstName.VmullI, T.VmullIA1, IsaVersion.v80, InstFlags.None),
                new(0xF2800A40, 0xFE800F50, sizeSizeVdConstraints, InstName.VmullS, T.VmullSA1, IsaVersion.v80, InstFlags.None),
                new(0xF3000D10, 0xFFB00F10, qvdQvnQvmConstraints, InstName.VmulF, T.VmulFA1, IsaVersion.v80, InstFlags.None),
                new(0xF3100D10, 0xFFB00F10, qvdQvnQvmConstraints, InstName.VmulF, T.VmulFA1, IsaVersion.v80, IsaFeature.FeatFp16, InstFlags.None),
                new(0x0E200A00, 0x0FB00E50, condConstraints, InstName.VmulF, T.VmulFA2, IsaVersion.v80, InstFlags.Cond),
                new(0x0E200900, 0x0FB00F50, condConstraints, InstName.VmulF, T.VmulFA2, IsaVersion.v80, IsaFeature.FeatFp16, InstFlags.Cond),
                new(0xF2000910, 0xFE800F10, sizeOpsizeOpsizeQvdQvnQvmConstraints, InstName.VmulI, T.VmulIA1, IsaVersion.v80, InstFlags.None),
                new(0xF2A00840, 0xFEA00E50, sizeQvdQvnConstraints, InstName.VmulS, T.VmulSA1, IsaVersion.v80, InstFlags.None),
                new(0xF2900840, 0xFEB00F50, sizeQvdQvnConstraints, InstName.VmulS, T.VmulSA1, IsaVersion.v80, InstFlags.None),
                new(0xF2900940, 0xFEB00F50, sizeQvdQvnConstraints, InstName.VmulS, T.VmulSA1, IsaVersion.v80, IsaFeature.FeatFp16, InstFlags.None),
                new(0xF2800030, 0xFEB809B0, cmodeQvdConstraints, InstName.VmvnI, T.VmvnIA1, IsaVersion.v80, InstFlags.None),
                new(0xF2800830, 0xFEB80DB0, cmodeQvdConstraints, InstName.VmvnI, T.VmvnIA2, IsaVersion.v80, InstFlags.None),
                new(0xF2800C30, 0xFEB80EB0, cmodeQvdConstraints, InstName.VmvnI, T.VmvnIA3, IsaVersion.v80, InstFlags.None),
                new(0xF3B00580, 0xFFBF0F90, qvdQvmConstraints, InstName.VmvnR, T.VmvnRA1, IsaVersion.v80, InstFlags.None),
                new(0xF3B10380, 0xFFB30F90, sizeQvdQvmConstraints, InstName.Vneg, T.VnegA1, IsaVersion.v80, InstFlags.None),
                new(0xF3B90780, 0xFFBB0F90, sizeQvdQvmConstraints, InstName.Vneg, T.VnegA1, IsaVersion.v80, InstFlags.None),
                new(0xF3B50780, 0xFFBF0F90, sizeQvdQvmConstraints, InstName.Vneg, T.VnegA1, IsaVersion.v80, IsaFeature.FeatFp16, InstFlags.None),
                new(0x0EB10A40, 0x0FBF0ED0, condConstraints, InstName.Vneg, T.VnegA2, IsaVersion.v80, InstFlags.Cond),
                new(0x0EB10940, 0x0FBF0FD0, condConstraints, InstName.Vneg, T.VnegA2, IsaVersion.v80, IsaFeature.FeatFp16, InstFlags.Cond),
                new(0x0E100A40, 0x0FB00E50, condConstraints, InstName.Vnmla, T.VnmlaA1, IsaVersion.v80, InstFlags.Cond),
                new(0x0E100940, 0x0FB00F50, condConstraints, InstName.Vnmla, T.VnmlaA1, IsaVersion.v80, IsaFeature.FeatFp16, InstFlags.Cond),
                new(0x0E100A00, 0x0FB00E50, condConstraints, InstName.Vnmls, T.VnmlsA1, IsaVersion.v80, InstFlags.Cond),
                new(0x0E100900, 0x0FB00F50, condConstraints, InstName.Vnmls, T.VnmlsA1, IsaVersion.v80, IsaFeature.FeatFp16, InstFlags.Cond),
                new(0x0E200A40, 0x0FB00E50, condConstraints, InstName.Vnmul, T.VnmulA1, IsaVersion.v80, InstFlags.Cond),
                new(0x0E200940, 0x0FB00F50, condConstraints, InstName.Vnmul, T.VnmulA1, IsaVersion.v80, IsaFeature.FeatFp16, InstFlags.Cond),
                new(0xF2300110, 0xFFB00F10, qvdQvnQvmConstraints, InstName.VornR, T.VornRA1, IsaVersion.v80, InstFlags.None),
                new(0xF2800110, 0xFEB809B0, cmodeCmodeQvdConstraints, InstName.VorrI, T.VorrIA1, IsaVersion.v80, InstFlags.None),
                new(0xF2800910, 0xFEB80DB0, cmodeCmodeQvdConstraints, InstName.VorrI, T.VorrIA2, IsaVersion.v80, InstFlags.None),
                new(0xF2200110, 0xFFB00F10, qvdQvnQvmConstraints, InstName.VorrR, T.VorrRA1, IsaVersion.v80, InstFlags.None),
                new(0xF3B00600, 0xFFB30F10, sizeQvdQvmConstraints, InstName.Vpadal, T.VpadalA1, IsaVersion.v80, InstFlags.None),
                new(0xF3B00200, 0xFFB30F10, sizeQvdQvmConstraints, InstName.Vpaddl, T.VpaddlA1, IsaVersion.v80, InstFlags.None),
                new(0xF3000D00, 0xFFB00F10, qConstraints, InstName.VpaddF, T.VpaddFA1, IsaVersion.v80, InstFlags.None),
                new(0xF3100D00, 0xFFB00F10, qConstraints, InstName.VpaddF, T.VpaddFA1, IsaVersion.v80, IsaFeature.FeatFp16, InstFlags.None),
                new(0xF2000B10, 0xFF800F10, sizeQConstraints, InstName.VpaddI, T.VpaddIA1, IsaVersion.v80, InstFlags.None),
                new(0xF3000F00, 0xFFB00F50, InstName.VpmaxF, T.VpmaxFA1, IsaVersion.v80, InstFlags.None),
                new(0xF3100F00, 0xFFB00F50, InstName.VpmaxF, T.VpmaxFA1, IsaVersion.v80, IsaFeature.FeatFp16, InstFlags.None),
                new(0xF2000A00, 0xFE800F50, sizeConstraints4, InstName.VpmaxI, T.VpmaxIA1, IsaVersion.v80, InstFlags.None),
                new(0xF3200F00, 0xFFB00F50, InstName.VpminF, T.VpminFA1, IsaVersion.v80, InstFlags.None),
                new(0xF3300F00, 0xFFB00F50, InstName.VpminF, T.VpminFA1, IsaVersion.v80, IsaFeature.FeatFp16, InstFlags.None),
                new(0xF2000A10, 0xFE800F50, sizeConstraints4, InstName.VpminI, T.VpminIA1, IsaVersion.v80, InstFlags.None),
                new(0xF3B00700, 0xFFB30F90, sizeQvdQvmConstraints, InstName.Vqabs, T.VqabsA1, IsaVersion.v80, InstFlags.None),
                new(0xF2000010, 0xFE800F10, qvdQvnQvmConstraints, InstName.Vqadd, T.VqaddA1, IsaVersion.v80, InstFlags.None),
                new(0xF2800900, 0xFF800F50, sizeSizeVdConstraints, InstName.Vqdmlal, T.VqdmlalA1, IsaVersion.v80, InstFlags.None),
                new(0xF2800340, 0xFF800F50, sizeSizeVdConstraints, InstName.Vqdmlal, T.VqdmlalA2, IsaVersion.v80, InstFlags.None),
                new(0xF2800B00, 0xFF800F50, sizeSizeVdConstraints, InstName.Vqdmlsl, T.VqdmlslA1, IsaVersion.v80, InstFlags.None),
                new(0xF2800740, 0xFF800F50, sizeSizeVdConstraints, InstName.Vqdmlsl, T.VqdmlslA2, IsaVersion.v80, InstFlags.None),
                new(0xF2000B00, 0xFF800F10, qvdQvnQvmSizeSizeConstraints, InstName.Vqdmulh, T.VqdmulhA1, IsaVersion.v80, InstFlags.None),
                new(0xF2800C40, 0xFE800F50, sizeSizeQvdQvnConstraints, InstName.Vqdmulh, T.VqdmulhA2, IsaVersion.v80, InstFlags.None),
                new(0xF2800D00, 0xFF800F50, sizeSizeVdConstraints, InstName.Vqdmull, T.VqdmullA1, IsaVersion.v80, InstFlags.None),
                new(0xF2800B40, 0xFF800F50, sizeSizeVdConstraints, InstName.Vqdmull, T.VqdmullA2, IsaVersion.v80, InstFlags.None),
                new(0xF3B20200, 0xFFB30F10, opSizeVmConstraints, InstName.Vqmovn, T.VqmovnA1, IsaVersion.v80, InstFlags.None),
                new(0xF3B00780, 0xFFB30F90, sizeQvdQvmConstraints, InstName.Vqneg, T.VqnegA1, IsaVersion.v80, InstFlags.None),
                new(0xF3000B10, 0xFF800F10, qvdQvnQvmSizeSizeConstraints, InstName.Vqrdmlah, T.VqrdmlahA1, IsaVersion.v81, IsaFeature.FeatRdm, InstFlags.None),
                new(0xF2800E40, 0xFE800F50, sizeSizeQvdQvnConstraints, InstName.Vqrdmlah, T.VqrdmlahA2, IsaVersion.v81, IsaFeature.FeatRdm, InstFlags.None),
                new(0xF3000C10, 0xFF800F10, qvdQvnQvmSizeSizeConstraints, InstName.Vqrdmlsh, T.VqrdmlshA1, IsaVersion.v81, IsaFeature.FeatRdm, InstFlags.None),
                new(0xF2800F40, 0xFE800F50, sizeSizeQvdQvnConstraints, InstName.Vqrdmlsh, T.VqrdmlshA2, IsaVersion.v81, IsaFeature.FeatRdm, InstFlags.None),
                new(0xF3000B00, 0xFF800F10, qvdQvnQvmSizeSizeConstraints, InstName.Vqrdmulh, T.VqrdmulhA1, IsaVersion.v80, InstFlags.None),
                new(0xF2800D40, 0xFE800F50, sizeSizeQvdQvnConstraints, InstName.Vqrdmulh, T.VqrdmulhA2, IsaVersion.v80, InstFlags.None),
                new(0xF2000510, 0xFE800F10, qvdQvmQvnConstraints, InstName.Vqrshl, T.VqrshlA1, IsaVersion.v80, InstFlags.None),
                new(0xF2800850, 0xFE800ED0, imm6UopVmConstraints, InstName.Vqrshrn, T.VqrshrnA1, IsaVersion.v80, InstFlags.None),
                new(0xF2800610, 0xFE800E10, imm6lUopQvdQvmConstraints, InstName.VqshlI, T.VqshlIA1, IsaVersion.v80, InstFlags.None),
                new(0xF2000410, 0xFE800F10, qvdQvmQvnConstraints, InstName.VqshlR, T.VqshlRA1, IsaVersion.v80, InstFlags.None),
                new(0xF2800810, 0xFE800ED0, imm6UopVmConstraints, InstName.Vqshrn, T.VqshrnA1, IsaVersion.v80, InstFlags.None),
                new(0xF2000210, 0xFE800F10, qvdQvnQvmConstraints, InstName.Vqsub, T.VqsubA1, IsaVersion.v80, InstFlags.None),
                new(0xF3800400, 0xFF800F50, sizeVnVmConstraints, InstName.Vraddhn, T.VraddhnA1, IsaVersion.v80, InstFlags.None),
                new(0xF3B30400, 0xFFB30E90, qvdQvmSizeSizeConstraints, InstName.Vrecpe, T.VrecpeA1, IsaVersion.v80, InstFlags.None),
                new(0xF2000F10, 0xFFB00F10, qvdQvnQvmConstraints, InstName.Vrecps, T.VrecpsA1, IsaVersion.v80, InstFlags.None),
                new(0xF2100F10, 0xFFB00F10, qvdQvnQvmConstraints, InstName.Vrecps, T.VrecpsA1, IsaVersion.v80, IsaFeature.FeatFp16, InstFlags.None),
                new(0xF3B00100, 0xFFB30F90, sizeSizeSizeQvdQvmConstraints, InstName.Vrev16, T.Vrev16A1, IsaVersion.v80, InstFlags.None),
                new(0xF3B00080, 0xFFB30F90, sizeSizeQvdQvmConstraints, InstName.Vrev32, T.Vrev32A1, IsaVersion.v80, InstFlags.None),
                new(0xF3B00000, 0xFFB30F90, sizeQvdQvmConstraints, InstName.Vrev64, T.Vrev64A1, IsaVersion.v80, InstFlags.None),
                new(0xF2000100, 0xFE800F10, qvdQvnQvmSizeConstraints, InstName.Vrhadd, T.VrhaddA1, IsaVersion.v80, InstFlags.None),
                new(0xF3BA0500, 0xFFBF0F90, qvdQvmConstraints, InstName.VrintaAsimd, T.VrintaAsimdA1, IsaVersion.v80, InstFlags.None),
                new(0xF3B60500, 0xFFBF0F90, qvdQvmConstraints, InstName.VrintaAsimd, T.VrintaAsimdA1, IsaVersion.v80, IsaFeature.FeatFp16, InstFlags.None),
                new(0xFEB80A40, 0xFFBF0ED0, sizeConstraints, InstName.VrintaVfp, T.VrintaVfpA1, IsaVersion.v80, InstFlags.None),
                new(0xFEB80940, 0xFFBF0FD0, sizeConstraints, InstName.VrintaVfp, T.VrintaVfpA1, IsaVersion.v80, IsaFeature.FeatFp16, InstFlags.None),
                new(0xF3BA0680, 0xFFBF0F90, qvdQvmConstraints, InstName.VrintmAsimd, T.VrintmAsimdA1, IsaVersion.v80, InstFlags.None),
                new(0xF3B60680, 0xFFBF0F90, qvdQvmConstraints, InstName.VrintmAsimd, T.VrintmAsimdA1, IsaVersion.v80, IsaFeature.FeatFp16, InstFlags.None),
                new(0xFEBB0A40, 0xFFBF0ED0, sizeConstraints, InstName.VrintmVfp, T.VrintmVfpA1, IsaVersion.v80, InstFlags.None),
                new(0xFEBB0940, 0xFFBF0FD0, sizeConstraints, InstName.VrintmVfp, T.VrintmVfpA1, IsaVersion.v80, IsaFeature.FeatFp16, InstFlags.None),
                new(0xF3BA0400, 0xFFBF0F90, qvdQvmConstraints, InstName.VrintnAsimd, T.VrintnAsimdA1, IsaVersion.v80, InstFlags.None),
                new(0xF3B60400, 0xFFBF0F90, qvdQvmConstraints, InstName.VrintnAsimd, T.VrintnAsimdA1, IsaVersion.v80, IsaFeature.FeatFp16, InstFlags.None),
                new(0xFEB90A40, 0xFFBF0ED0, sizeConstraints, InstName.VrintnVfp, T.VrintnVfpA1, IsaVersion.v80, InstFlags.None),
                new(0xFEB90940, 0xFFBF0FD0, sizeConstraints, InstName.VrintnVfp, T.VrintnVfpA1, IsaVersion.v80, IsaFeature.FeatFp16, InstFlags.None),
                new(0xF3BA0780, 0xFFBF0F90, qvdQvmConstraints, InstName.VrintpAsimd, T.VrintpAsimdA1, IsaVersion.v80, InstFlags.None),
                new(0xF3B60780, 0xFFBF0F90, qvdQvmConstraints, InstName.VrintpAsimd, T.VrintpAsimdA1, IsaVersion.v80, IsaFeature.FeatFp16, InstFlags.None),
                new(0xFEBA0A40, 0xFFBF0ED0, sizeConstraints, InstName.VrintpVfp, T.VrintpVfpA1, IsaVersion.v80, InstFlags.None),
                new(0xFEBA0940, 0xFFBF0FD0, sizeConstraints, InstName.VrintpVfp, T.VrintpVfpA1, IsaVersion.v80, IsaFeature.FeatFp16, InstFlags.None),
                new(0x0EB60A40, 0x0FBF0ED0, condConstraints, InstName.VrintrVfp, T.VrintrVfpA1, IsaVersion.v80, InstFlags.Cond),
                new(0x0EB60940, 0x0FBF0FD0, condConstraints, InstName.VrintrVfp, T.VrintrVfpA1, IsaVersion.v80, IsaFeature.FeatFp16, InstFlags.Cond),
                new(0xF3BA0480, 0xFFBF0F90, qvdQvmConstraints, InstName.VrintxAsimd, T.VrintxAsimdA1, IsaVersion.v80, InstFlags.None),
                new(0xF3B60480, 0xFFBF0F90, qvdQvmConstraints, InstName.VrintxAsimd, T.VrintxAsimdA1, IsaVersion.v80, IsaFeature.FeatFp16, InstFlags.None),
                new(0x0EB70A40, 0x0FBF0ED0, condConstraints, InstName.VrintxVfp, T.VrintxVfpA1, IsaVersion.v80, InstFlags.Cond),
                new(0x0EB70940, 0x0FBF0FD0, condConstraints, InstName.VrintxVfp, T.VrintxVfpA1, IsaVersion.v80, IsaFeature.FeatFp16, InstFlags.Cond),
                new(0xF3BA0580, 0xFFBF0F90, qvdQvmConstraints, InstName.VrintzAsimd, T.VrintzAsimdA1, IsaVersion.v80, InstFlags.None),
                new(0xF3B60580, 0xFFBF0F90, qvdQvmConstraints, InstName.VrintzAsimd, T.VrintzAsimdA1, IsaVersion.v80, IsaFeature.FeatFp16, InstFlags.None),
                new(0x0EB60AC0, 0x0FBF0ED0, condConstraints, InstName.VrintzVfp, T.VrintzVfpA1, IsaVersion.v80, InstFlags.Cond),
                new(0x0EB609C0, 0x0FBF0FD0, condConstraints, InstName.VrintzVfp, T.VrintzVfpA1, IsaVersion.v80, IsaFeature.FeatFp16, InstFlags.Cond),
                new(0xF2000500, 0xFE800F10, qvdQvmQvnConstraints, InstName.Vrshl, T.VrshlA1, IsaVersion.v80, InstFlags.None),
                new(0xF2800210, 0xFE800F10, imm6lQvdQvmConstraints, InstName.Vrshr, T.VrshrA1, IsaVersion.v80, InstFlags.None),
                new(0xF2800850, 0xFF800FD0, imm6VmConstraints, InstName.Vrshrn, T.VrshrnA1, IsaVersion.v80, InstFlags.None),
                new(0xF3B30480, 0xFFB30E90, qvdQvmSizeSizeConstraints, InstName.Vrsqrte, T.VrsqrteA1, IsaVersion.v80, InstFlags.None),
                new(0xF2200F10, 0xFFB00F10, qvdQvnQvmConstraints, InstName.Vrsqrts, T.VrsqrtsA1, IsaVersion.v80, InstFlags.None),
                new(0xF2300F10, 0xFFB00F10, qvdQvnQvmConstraints, InstName.Vrsqrts, T.VrsqrtsA1, IsaVersion.v80, IsaFeature.FeatFp16, InstFlags.None),
                new(0xF2800310, 0xFE800F10, imm6lQvdQvmConstraints, InstName.Vrsra, T.VrsraA1, IsaVersion.v80, InstFlags.None),
                new(0xF3800600, 0xFF800F50, sizeVnVmConstraints, InstName.Vrsubhn, T.VrsubhnA1, IsaVersion.v80, InstFlags.None),
                new(0xFC200D00, 0xFFB00F10, qvdQvnQvmConstraints, InstName.Vsdot, T.VsdotA1, IsaVersion.v82, IsaFeature.FeatDotprod, InstFlags.None),
                new(0xFE200D00, 0xFFB00F10, qvdQvnConstraints, InstName.VsdotS, T.VsdotSA1, IsaVersion.v82, IsaFeature.FeatDotprod, InstFlags.None),
                new(0xFE000A00, 0xFF800E50, sizeConstraints, InstName.Vsel, T.VselA1, IsaVersion.v80, InstFlags.None),
                new(0xFE000900, 0xFF800F50, sizeConstraints, InstName.Vsel, T.VselA1, IsaVersion.v80, IsaFeature.FeatFp16, InstFlags.None),
                new(0xF2800A10, 0xFE800FD0, imm6VdImm6Imm6Imm6Constraints, InstName.Vshll, T.VshllA1, IsaVersion.v80, InstFlags.None),
                new(0xF3B20300, 0xFFB30FD0, sizeVdConstraints2, InstName.Vshll, T.VshllA2, IsaVersion.v80, InstFlags.None),
                new(0xF2800510, 0xFF800F10, imm6lQvdQvmConstraints, InstName.VshlI, T.VshlIA1, IsaVersion.v80, InstFlags.None),
                new(0xF2000400, 0xFE800F10, qvdQvmQvnConstraints, InstName.VshlR, T.VshlRA1, IsaVersion.v80, InstFlags.None),
                new(0xF2800010, 0xFE800F10, imm6lQvdQvmConstraints, InstName.Vshr, T.VshrA1, IsaVersion.v80, InstFlags.None),
                new(0xF2800810, 0xFF800FD0, imm6VmConstraints, InstName.Vshrn, T.VshrnA1, IsaVersion.v80, InstFlags.None),
                new(0xF3800510, 0xFF800F10, imm6lQvdQvmConstraints, InstName.Vsli, T.VsliA1, IsaVersion.v80, InstFlags.None),
                new(0xFC200C40, 0xFFB00F50, vdVnVmConstraints, InstName.Vsmmla, T.VsmmlaA1, IsaVersion.v82, IsaFeature.FeatAa32i8mm, InstFlags.None),
                new(0x0EB10AC0, 0x0FBF0ED0, condConstraints, InstName.Vsqrt, T.VsqrtA1, IsaVersion.v80, InstFlags.Cond),
                new(0x0EB109C0, 0x0FBF0FD0, condConstraints, InstName.Vsqrt, T.VsqrtA1, IsaVersion.v80, IsaFeature.FeatFp16, InstFlags.Cond),
                new(0xF2800110, 0xFE800F10, imm6lQvdQvmConstraints, InstName.Vsra, T.VsraA1, IsaVersion.v80, InstFlags.None),
                new(0xF3800410, 0xFF800F10, imm6lQvdQvmConstraints, InstName.Vsri, T.VsriA1, IsaVersion.v80, InstFlags.None),
                new(0xF4800000, 0xFFB00F10, sizeConstraints2, InstName.Vst11, T.Vst11A1, IsaVersion.v80, InstFlags.None),
                new(0xF4800400, 0xFFB00F20, sizeConstraints2, InstName.Vst11, T.Vst11A2, IsaVersion.v80, InstFlags.None),
                new(0xF4800800, 0xFFB00F40, sizeIndexAlignIndexAlignConstraints, InstName.Vst11, T.Vst11A3, IsaVersion.v80, InstFlags.None),
                new(0xF4000700, 0xFFB00F00, alignConstraints, InstName.Vst1M, T.Vst1MA1, IsaVersion.v80, InstFlags.None),
                new(0xF4000A00, 0xFFB00F00, alignConstraints2, InstName.Vst1M, T.Vst1MA2, IsaVersion.v80, InstFlags.None),
                new(0xF4000600, 0xFFB00F00, alignConstraints, InstName.Vst1M, T.Vst1MA3, IsaVersion.v80, InstFlags.None),
                new(0xF4000200, 0xFFB00F00, InstName.Vst1M, T.Vst1MA4, IsaVersion.v80, InstFlags.None),
                new(0xF4800100, 0xFFB00F00, sizeConstraints2, InstName.Vst21, T.Vst21A1, IsaVersion.v80, InstFlags.None),
                new(0xF4800500, 0xFFB00F00, sizeConstraints2, InstName.Vst21, T.Vst21A2, IsaVersion.v80, InstFlags.None),
                new(0xF4800900, 0xFFB00F20, sizeConstraints2, InstName.Vst21, T.Vst21A3, IsaVersion.v80, InstFlags.None),
                new(0xF4000800, 0xFFB00E00, alignSizeConstraints, InstName.Vst2M, T.Vst2MA1, IsaVersion.v80, InstFlags.None),
                new(0xF4000300, 0xFFB00F00, sizeConstraints3, InstName.Vst2M, T.Vst2MA2, IsaVersion.v80, InstFlags.None),
                new(0xF4800200, 0xFFB00F10, sizeConstraints2, InstName.Vst31, T.Vst31A1, IsaVersion.v80, InstFlags.None),
                new(0xF4800600, 0xFFB00F10, sizeConstraints2, InstName.Vst31, T.Vst31A2, IsaVersion.v80, InstFlags.None),
                new(0xF4800A00, 0xFFB00F30, sizeConstraints2, InstName.Vst31, T.Vst31A3, IsaVersion.v80, InstFlags.None),
                new(0xF4000400, 0xFFB00E00, sizeAlignConstraints, InstName.Vst3M, T.Vst3MA1, IsaVersion.v80, InstFlags.None),
                new(0xF4800300, 0xFFB00F00, sizeConstraints2, InstName.Vst41, T.Vst41A1, IsaVersion.v80, InstFlags.None),
                new(0xF4800700, 0xFFB00F00, sizeConstraints2, InstName.Vst41, T.Vst41A2, IsaVersion.v80, InstFlags.None),
                new(0xF4800B00, 0xFFB00F00, sizeIndexAlignConstraints, InstName.Vst41, T.Vst41A3, IsaVersion.v80, InstFlags.None),
                new(0xF4000000, 0xFFB00E00, sizeConstraints3, InstName.Vst4M, T.Vst4MA1, IsaVersion.v80, InstFlags.None),
                new(0x0C000B00, 0x0E100F01, condPuwPwPuwPuwConstraints, InstName.Vstm, T.VstmA1, IsaVersion.v80, InstFlags.CondWBack),
                new(0x0C000A00, 0x0E100F00, condPuwPwPuwPuwConstraints, InstName.Vstm, T.VstmA2, IsaVersion.v80, InstFlags.CondWBack),
                new(0x0D000A00, 0x0F300E00, condConstraints, InstName.Vstr, T.VstrA1, IsaVersion.v80, InstFlags.Cond),
                new(0x0D000900, 0x0F300F00, condConstraints, InstName.Vstr, T.VstrA1, IsaVersion.v80, IsaFeature.FeatFp16, InstFlags.Cond),
                new(0xF2800600, 0xFF800F50, sizeVnVmConstraints, InstName.Vsubhn, T.VsubhnA1, IsaVersion.v80, InstFlags.None),
                new(0xF2800200, 0xFE800F50, sizeVdOpvnConstraints, InstName.Vsubl, T.VsublA1, IsaVersion.v80, InstFlags.None),
                new(0xF2800300, 0xFE800F50, sizeVdOpvnConstraints, InstName.Vsubw, T.VsubwA1, IsaVersion.v80, InstFlags.None),
                new(0xF2200D00, 0xFFB00F10, qvdQvnQvmConstraints, InstName.VsubF, T.VsubFA1, IsaVersion.v80, InstFlags.None),
                new(0xF2300D00, 0xFFB00F10, qvdQvnQvmConstraints, InstName.VsubF, T.VsubFA1, IsaVersion.v80, IsaFeature.FeatFp16, InstFlags.None),
                new(0x0E300A40, 0x0FB00E50, condConstraints, InstName.VsubF, T.VsubFA2, IsaVersion.v80, InstFlags.Cond),
                new(0x0E300940, 0x0FB00F50, condConstraints, InstName.VsubF, T.VsubFA2, IsaVersion.v80, IsaFeature.FeatFp16, InstFlags.Cond),
                new(0xF3000800, 0xFF800F10, qvdQvnQvmConstraints, InstName.VsubI, T.VsubIA1, IsaVersion.v80, InstFlags.None),
                new(0xFE800D10, 0xFFB00F10, qvdQvnConstraints, InstName.VsudotS, T.VsudotSA1, IsaVersion.v82, IsaFeature.FeatAa32i8mm, InstFlags.None),
                new(0xF3B20000, 0xFFBF0F90, qvdQvmConstraints, InstName.Vswp, T.VswpA1, IsaVersion.v80, InstFlags.None),
                new(0xF3B00800, 0xFFB00C10, InstName.Vtbl, T.VtblA1, IsaVersion.v80, InstFlags.None),
                new(0xF3B20080, 0xFFB30F90, sizeQvdQvmConstraints, InstName.Vtrn, T.VtrnA1, IsaVersion.v80, InstFlags.None),
                new(0xF2000810, 0xFF800F10, qvdQvnQvmSizeConstraints, InstName.Vtst, T.VtstA1, IsaVersion.v80, InstFlags.None),
                new(0xFC200D10, 0xFFB00F10, qvdQvnQvmConstraints, InstName.Vudot, T.VudotA1, IsaVersion.v82, IsaFeature.FeatDotprod, InstFlags.None),
                new(0xFE200D10, 0xFFB00F10, qvdQvnConstraints, InstName.VudotS, T.VudotSA1, IsaVersion.v82, IsaFeature.FeatDotprod, InstFlags.None),
                new(0xFC200C50, 0xFFB00F50, vdVnVmConstraints, InstName.Vummla, T.VummlaA1, IsaVersion.v82, IsaFeature.FeatAa32i8mm, InstFlags.None),
                new(0xFCA00D00, 0xFFB00F10, qvdQvnQvmConstraints, InstName.Vusdot, T.VusdotA1, IsaVersion.v82, IsaFeature.FeatAa32i8mm, InstFlags.None),
                new(0xFE800D00, 0xFFB00F10, qvdQvnConstraints, InstName.VusdotS, T.VusdotSA1, IsaVersion.v82, IsaFeature.FeatAa32i8mm, InstFlags.None),
                new(0xFCA00C40, 0xFFB00F50, vdVnVmConstraints, InstName.Vusmmla, T.VusmmlaA1, IsaVersion.v82, IsaFeature.FeatAa32i8mm, InstFlags.None),
                new(0xF3B20100, 0xFFB30F90, sizeQsizeQvdQvmConstraints, InstName.Vuzp, T.VuzpA1, IsaVersion.v80, InstFlags.None),
                new(0xF3B20180, 0xFFB30F90, sizeQsizeQvdQvmConstraints, InstName.Vzip, T.VzipA1, IsaVersion.v80, InstFlags.None),
                new(0x0320F002, 0x0FFFFFFF, condConstraints, InstName.Wfe, T.WfeA1, IsaVersion.v80, InstFlags.Cond),
                new(0x0320F003, 0x0FFFFFFF, condConstraints, InstName.Wfi, T.WfiA1, IsaVersion.v80, InstFlags.Cond),
                new(0x0320F001, 0x0FFFFFFF, condConstraints, InstName.Yield, T.YieldA1, IsaVersion.v80, InstFlags.Cond),
            ];

            _table = new(insts);
        }

        public static InstMeta GetMeta(uint encoding, IsaVersion version, IsaFeature features)
        {
            if (_table.TryFind(encoding, version, features, out InstInfoForTable info))
            {
                return info.Meta;
            }

            return new(InstName.Udf, T.UdfA1, IsaVersion.v80, IsaFeature.None, InstFlags.None);
        }
    }
}
