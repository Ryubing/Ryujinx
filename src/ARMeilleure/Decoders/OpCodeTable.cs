using ARMeilleure.Instructions;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace ARMeilleure.Decoders
{
    static class OpCodeTable
    {
        public delegate OpCode MakeOp(InstDescriptor inst, ulong address, int opCode);

        private const int FastLookupSize = 0x1000;

        private readonly struct InstInfo
        {
            public int Mask { get; }
            public int Value { get; }

            public InstDescriptor Inst { get; }

            public MakeOp MakeOp { get; }

            public InstInfo(int mask, int value, InstDescriptor inst, MakeOp makeOp)
            {
                Mask = mask;
                Value = value;
                Inst = inst;
                MakeOp = makeOp;
            }
        }

        private static readonly List<InstInfo> _allInstA32 = [];
        private static readonly List<InstInfo> _allInstT32 = [];
        private static readonly List<InstInfo> _allInstA64 = [];

        private static readonly InstInfo[][] _instA32FastLookup = new InstInfo[FastLookupSize][];
        private static readonly InstInfo[][] _instT32FastLookup = new InstInfo[FastLookupSize][];
        private static readonly InstInfo[][] _instA64FastLookup = new InstInfo[FastLookupSize][];

        static OpCodeTable()
        {
#pragma warning disable IDE0055 // Disable formatting
            #region "OpCode Table (AArch64)"
            // Base
            SetA64("x0011010000xxxxx000000xxxxxxxxxx", InstName.Adc,             InstEmit.Adc,             OpCodeAluRs.Create);
            SetA64("x0111010000xxxxx000000xxxxxxxxxx", InstName.Adcs,            InstEmit.Adcs,            OpCodeAluRs.Create);
            SetA64("x00100010xxxxxxxxxxxxxxxxxxxxxxx", InstName.Add,             InstEmit.Add,             OpCodeAluImm.Create);
            SetA64("00001011<<0xxxxx0xxxxxxxxxxxxxxx", InstName.Add,             InstEmit.Add,             OpCodeAluRs.Create);
            SetA64("10001011<<0xxxxxxxxxxxxxxxxxxxxx", InstName.Add,             InstEmit.Add,             OpCodeAluRs.Create);
            SetA64("x0001011001xxxxxxxx0xxxxxxxxxxxx", InstName.Add,             InstEmit.Add,             OpCodeAluRx.Create);
            SetA64("x0001011001xxxxxxxx100xxxxxxxxxx", InstName.Add,             InstEmit.Add,             OpCodeAluRx.Create);
            SetA64("x01100010xxxxxxxxxxxxxxxxxxxxxxx", InstName.Adds,            InstEmit.Adds,            OpCodeAluImm.Create);
            SetA64("00101011<<0xxxxx0xxxxxxxxxxxxxxx", InstName.Adds,            InstEmit.Adds,            OpCodeAluRs.Create);
            SetA64("10101011<<0xxxxxxxxxxxxxxxxxxxxx", InstName.Adds,            InstEmit.Adds,            OpCodeAluRs.Create);
            SetA64("x0101011001xxxxxxxx0xxxxxxxxxxxx", InstName.Adds,            InstEmit.Adds,            OpCodeAluRx.Create);
            SetA64("x0101011001xxxxxxxx100xxxxxxxxxx", InstName.Adds,            InstEmit.Adds,            OpCodeAluRx.Create);
            SetA64("0xx10000xxxxxxxxxxxxxxxxxxxxxxxx", InstName.Adr,             InstEmit.Adr,             OpCodeAdr.Create);
            SetA64("1xx10000xxxxxxxxxxxxxxxxxxxxxxxx", InstName.Adrp,            InstEmit.Adrp,            OpCodeAdr.Create);
            SetA64("0001001000xxxxxxxxxxxxxxxxxxxxxx", InstName.And,             InstEmit.And,             OpCodeAluImm.Create);
            SetA64("100100100xxxxxxxxxxxxxxxxxxxxxxx", InstName.And,             InstEmit.And,             OpCodeAluImm.Create);
            SetA64("00001010xx0xxxxx0xxxxxxxxxxxxxxx", InstName.And,             InstEmit.And,             OpCodeAluRs.Create);
            SetA64("10001010xx0xxxxxxxxxxxxxxxxxxxxx", InstName.And,             InstEmit.And,             OpCodeAluRs.Create);
            SetA64("0111001000xxxxxxxxxxxxxxxxxxxxxx", InstName.Ands,            InstEmit.Ands,            OpCodeAluImm.Create);
            SetA64("111100100xxxxxxxxxxxxxxxxxxxxxxx", InstName.Ands,            InstEmit.Ands,            OpCodeAluImm.Create);
            SetA64("01101010xx0xxxxx0xxxxxxxxxxxxxxx", InstName.Ands,            InstEmit.Ands,            OpCodeAluRs.Create);
            SetA64("11101010xx0xxxxxxxxxxxxxxxxxxxxx", InstName.Ands,            InstEmit.Ands,            OpCodeAluRs.Create);
            SetA64("x0011010110xxxxx001010xxxxxxxxxx", InstName.Asrv,            InstEmit.Asrv,            OpCodeAluRs.Create);
            SetA64("000101xxxxxxxxxxxxxxxxxxxxxxxxxx", InstName.B,               InstEmit.B,               OpCodeBImmAl.Create);
            SetA64("01010100xxxxxxxxxxxxxxxxxxx0xxxx", InstName.B_Cond,          InstEmit.B_Cond,          OpCodeBImmCond.Create);
            SetA64("00110011000xxxxx0xxxxxxxxxxxxxxx", InstName.Bfm,             InstEmit.Bfm,             OpCodeBfm.Create);
            SetA64("1011001101xxxxxxxxxxxxxxxxxxxxxx", InstName.Bfm,             InstEmit.Bfm,             OpCodeBfm.Create);
            SetA64("00001010xx1xxxxx0xxxxxxxxxxxxxxx", InstName.Bic,             InstEmit.Bic,             OpCodeAluRs.Create);
            SetA64("10001010xx1xxxxxxxxxxxxxxxxxxxxx", InstName.Bic,             InstEmit.Bic,             OpCodeAluRs.Create);
            SetA64("01101010xx1xxxxx0xxxxxxxxxxxxxxx", InstName.Bics,            InstEmit.Bics,            OpCodeAluRs.Create);
            SetA64("11101010xx1xxxxxxxxxxxxxxxxxxxxx", InstName.Bics,            InstEmit.Bics,            OpCodeAluRs.Create);
            SetA64("100101xxxxxxxxxxxxxxxxxxxxxxxxxx", InstName.Bl,              InstEmit.Bl,              OpCodeBImmAl.Create);
            SetA64("1101011000111111000000xxxxx00000", InstName.Blr,             InstEmit.Blr,             OpCodeBReg.Create);
            SetA64("1101011000011111000000xxxxx00000", InstName.Br,              InstEmit.Br,              OpCodeBReg.Create);
            SetA64("11010100001xxxxxxxxxxxxxxxx00000", InstName.Brk,             InstEmit.Brk,             OpCodeException.Create);
            SetA64("x0110101xxxxxxxxxxxxxxxxxxxxxxxx", InstName.Cbnz,            InstEmit.Cbnz,            OpCodeBImmCmp.Create);
            SetA64("x0110100xxxxxxxxxxxxxxxxxxxxxxxx", InstName.Cbz,             InstEmit.Cbz,             OpCodeBImmCmp.Create);
            SetA64("x0111010010xxxxxxxxx10xxxxx0xxxx", InstName.Ccmn,            InstEmit.Ccmn,            OpCodeCcmpImm.Create);
            SetA64("x0111010010xxxxxxxxx00xxxxx0xxxx", InstName.Ccmn,            InstEmit.Ccmn,            OpCodeCcmpReg.Create);
            SetA64("x1111010010xxxxxxxxx10xxxxx0xxxx", InstName.Ccmp,            InstEmit.Ccmp,            OpCodeCcmpImm.Create);
            SetA64("x1111010010xxxxxxxxx00xxxxx0xxxx", InstName.Ccmp,            InstEmit.Ccmp,            OpCodeCcmpReg.Create);
            SetA64("11010101000000110011xxxx01011111", InstName.Clrex,           InstEmit.Clrex,           OpCodeSystem.Create);
            SetA64("x101101011000000000101xxxxxxxxxx", InstName.Cls,             InstEmit.Cls,             OpCodeAlu.Create);
            SetA64("x101101011000000000100xxxxxxxxxx", InstName.Clz,             InstEmit.Clz,             OpCodeAlu.Create);
            SetA64("00011010110xxxxx010000xxxxxxxxxx", InstName.Crc32b,          InstEmit.Crc32b,          OpCodeAluBinary.Create);
            SetA64("00011010110xxxxx010001xxxxxxxxxx", InstName.Crc32h,          InstEmit.Crc32h,          OpCodeAluBinary.Create);
            SetA64("00011010110xxxxx010010xxxxxxxxxx", InstName.Crc32w,          InstEmit.Crc32w,          OpCodeAluBinary.Create);
            SetA64("10011010110xxxxx010011xxxxxxxxxx", InstName.Crc32x,          InstEmit.Crc32x,          OpCodeAluBinary.Create);
            SetA64("00011010110xxxxx010100xxxxxxxxxx", InstName.Crc32cb,         InstEmit.Crc32cb,         OpCodeAluBinary.Create);
            SetA64("00011010110xxxxx010101xxxxxxxxxx", InstName.Crc32ch,         InstEmit.Crc32ch,         OpCodeAluBinary.Create);
            SetA64("00011010110xxxxx010110xxxxxxxxxx", InstName.Crc32cw,         InstEmit.Crc32cw,         OpCodeAluBinary.Create);
            SetA64("10011010110xxxxx010111xxxxxxxxxx", InstName.Crc32cx,         InstEmit.Crc32cx,         OpCodeAluBinary.Create);
            SetA64("11010101000000110010001010011111", InstName.Csdb,            InstEmit.Csdb,            OpCodeSystem.Create);
            SetA64("x0011010100xxxxxxxxx00xxxxxxxxxx", InstName.Csel,            InstEmit.Csel,            OpCodeCsel.Create);
            SetA64("x0011010100xxxxxxxxx01xxxxxxxxxx", InstName.Csinc,           InstEmit.Csinc,           OpCodeCsel.Create);
            SetA64("x1011010100xxxxxxxxx00xxxxxxxxxx", InstName.Csinv,           InstEmit.Csinv,           OpCodeCsel.Create);
            SetA64("x1011010100xxxxxxxxx01xxxxxxxxxx", InstName.Csneg,           InstEmit.Csneg,           OpCodeCsel.Create);
            SetA64("11010101000000110011xxxx10111111", InstName.Dmb,             InstEmit.Dmb,             OpCodeSystem.Create);
            SetA64("11010101000000110011xxxx10011111", InstName.Dsb,             InstEmit.Dsb,             OpCodeSystem.Create);
            SetA64("01001010xx1xxxxx0xxxxxxxxxxxxxxx", InstName.Eon,             InstEmit.Eon,             OpCodeAluRs.Create);
            SetA64("11001010xx1xxxxxxxxxxxxxxxxxxxxx", InstName.Eon,             InstEmit.Eon,             OpCodeAluRs.Create);
            SetA64("0101001000xxxxxxxxxxxxxxxxxxxxxx", InstName.Eor,             InstEmit.Eor,             OpCodeAluImm.Create);
            SetA64("110100100xxxxxxxxxxxxxxxxxxxxxxx", InstName.Eor,             InstEmit.Eor,             OpCodeAluImm.Create);
            SetA64("01001010xx0xxxxx0xxxxxxxxxxxxxxx", InstName.Eor,             InstEmit.Eor,             OpCodeAluRs.Create);
            SetA64("11001010xx0xxxxxxxxxxxxxxxxxxxxx", InstName.Eor,             InstEmit.Eor,             OpCodeAluRs.Create);
            SetA64("00010011100xxxxx0xxxxxxxxxxxxxxx", InstName.Extr,            InstEmit.Extr,            OpCodeAluRs.Create);
            SetA64("10010011110xxxxxxxxxxxxxxxxxxxxx", InstName.Extr,            InstEmit.Extr,            OpCodeAluRs.Create);
            SetA64("11010101000000110010000011011111", InstName.Hint,            InstEmit.Nop,             OpCodeSystem.Create); // Reserved Hint
            SetA64("11010101000000110010000011111111", InstName.Hint,            InstEmit.Nop,             OpCodeSystem.Create); // Reserved Hint
            SetA64("110101010000001100100001xxx11111", InstName.Hint,            InstEmit.Nop,             OpCodeSystem.Create); // Reserved Hint
            SetA64("1101010100000011001000100xx11111", InstName.Hint,            InstEmit.Nop,             OpCodeSystem.Create); // Reserved Hint
            SetA64("1101010100000011001000101>>11111", InstName.Hint,            InstEmit.Nop,             OpCodeSystem.Create); // Reserved Hint
            SetA64("110101010000001100100011xxx11111", InstName.Hint,            InstEmit.Nop,             OpCodeSystem.Create); // Reserved Hint
            SetA64("11010101000000110010>>xxxxx11111", InstName.Hint,            InstEmit.Nop,             OpCodeSystem.Create); // Reserved Hint
            SetA64("11010101000000110011xxxx11011111", InstName.Isb,             InstEmit.Isb,             OpCodeSystem.Create);
            SetA64("xx001000110xxxxx1xxxxxxxxxxxxxxx", InstName.Ldar,            InstEmit.Ldar,            OpCodeMemEx.Create);
            SetA64("1x001000011xxxxx1xxxxxxxxxxxxxxx", InstName.Ldaxp,           InstEmit.Ldaxp,           OpCodeMemEx.Create);
            SetA64("xx001000010xxxxx1xxxxxxxxxxxxxxx", InstName.Ldaxr,           InstEmit.Ldaxr,           OpCodeMemEx.Create);
            SetA64("<<10100xx1xxxxxxxxxxxxxxxxxxxxxx", InstName.Ldp,             InstEmit.Ldp,             OpCodeMemPair.Create);
            SetA64("xx111000010xxxxxxxxxxxxxxxxxxxxx", InstName.Ldr,             InstEmit.Ldr,             OpCodeMemImm.Create);
            SetA64("xx11100101xxxxxxxxxxxxxxxxxxxxxx", InstName.Ldr,             InstEmit.Ldr,             OpCodeMemImm.Create);
            SetA64("xx111000011xxxxxxxxx10xxxxxxxxxx", InstName.Ldr,             InstEmit.Ldr,             OpCodeMemReg.Create);
            SetA64("xx011000xxxxxxxxxxxxxxxxxxxxxxxx", InstName.Ldr_Literal,     InstEmit.Ldr_Literal,     OpCodeMemLit.Create);
            SetA64("0x1110001x0xxxxxxxxxxxxxxxxxxxxx", InstName.Ldrs,            InstEmit.Ldrs,            OpCodeMemImm.Create);
            SetA64("0x1110011xxxxxxxxxxxxxxxxxxxxxxx", InstName.Ldrs,            InstEmit.Ldrs,            OpCodeMemImm.Create);
            SetA64("10111000100xxxxxxxxxxxxxxxxxxxxx", InstName.Ldrs,            InstEmit.Ldrs,            OpCodeMemImm.Create);
            SetA64("1011100110xxxxxxxxxxxxxxxxxxxxxx", InstName.Ldrs,            InstEmit.Ldrs,            OpCodeMemImm.Create);
            SetA64("0x1110001x1xxxxxxxxx10xxxxxxxxxx", InstName.Ldrs,            InstEmit.Ldrs,            OpCodeMemReg.Create);
            SetA64("10111000101xxxxxxxxx10xxxxxxxxxx", InstName.Ldrs,            InstEmit.Ldrs,            OpCodeMemReg.Create);
            SetA64("xx001000010xxxxx0xxxxxxxxxxxxxxx", InstName.Ldxr,            InstEmit.Ldxr,            OpCodeMemEx.Create);
            SetA64("1x001000011xxxxx0xxxxxxxxxxxxxxx", InstName.Ldxp,            InstEmit.Ldxp,            OpCodeMemEx.Create);
            SetA64("x0011010110xxxxx001000xxxxxxxxxx", InstName.Lslv,            InstEmit.Lslv,            OpCodeAluRs.Create);
            SetA64("x0011010110xxxxx001001xxxxxxxxxx", InstName.Lsrv,            InstEmit.Lsrv,            OpCodeAluRs.Create);
            SetA64("x0011011000xxxxx0xxxxxxxxxxxxxxx", InstName.Madd,            InstEmit.Madd,            OpCodeMul.Create);
            SetA64("0111001010xxxxxxxxxxxxxxxxxxxxxx", InstName.Movk,            InstEmit.Movk,            OpCodeMov.Create);
            SetA64("111100101xxxxxxxxxxxxxxxxxxxxxxx", InstName.Movk,            InstEmit.Movk,            OpCodeMov.Create);
            SetA64("0001001010xxxxxxxxxxxxxxxxxxxxxx", InstName.Movn,            InstEmit.Movn,            OpCodeMov.Create);
            SetA64("100100101xxxxxxxxxxxxxxxxxxxxxxx", InstName.Movn,            InstEmit.Movn,            OpCodeMov.Create);
            SetA64("0101001010xxxxxxxxxxxxxxxxxxxxxx", InstName.Movz,            InstEmit.Movz,            OpCodeMov.Create);
            SetA64("110100101xxxxxxxxxxxxxxxxxxxxxxx", InstName.Movz,            InstEmit.Movz,            OpCodeMov.Create);
            SetA64("110101010011xxxxxxxxxxxxxxxxxxxx", InstName.Mrs,             InstEmit.Mrs,             OpCodeSystem.Create);
            SetA64("110101010001xxxxxxxxxxxxxxxxxxxx", InstName.Msr,             InstEmit.Msr,             OpCodeSystem.Create);
            SetA64("x0011011000xxxxx1xxxxxxxxxxxxxxx", InstName.Msub,            InstEmit.Msub,            OpCodeMul.Create);
            SetA64("11010101000000110010000000011111", InstName.Nop,             InstEmit.Nop,             OpCodeSystem.Create);
            SetA64("00101010xx1xxxxx0xxxxxxxxxxxxxxx", InstName.Orn,             InstEmit.Orn,             OpCodeAluRs.Create);
            SetA64("10101010xx1xxxxxxxxxxxxxxxxxxxxx", InstName.Orn,             InstEmit.Orn,             OpCodeAluRs.Create);
            SetA64("0011001000xxxxxxxxxxxxxxxxxxxxxx", InstName.Orr,             InstEmit.Orr,             OpCodeAluImm.Create);
            SetA64("101100100xxxxxxxxxxxxxxxxxxxxxxx", InstName.Orr,             InstEmit.Orr,             OpCodeAluImm.Create);
            SetA64("00101010xx0xxxxx0xxxxxxxxxxxxxxx", InstName.Orr,             InstEmit.Orr,             OpCodeAluRs.Create);
            SetA64("10101010xx0xxxxxxxxxxxxxxxxxxxxx", InstName.Orr,             InstEmit.Orr,             OpCodeAluRs.Create);
            SetA64("1111100110xxxxxxxxxxxxxxxxxxxxxx", InstName.Prfm,            InstEmit.Prfm,            OpCodeMemImm.Create); // immediate
            SetA64("11111000100xxxxxxxxx00xxxxxxxxxx", InstName.Prfm,            InstEmit.Prfm,            OpCodeMemImm.Create); // prfum (unscaled offset)
            SetA64("11011000xxxxxxxxxxxxxxxxxxxxxxxx", InstName.Prfm,            InstEmit.Prfm,            OpCodeMemLit.Create); // literal
            SetA64("11111000101xxxxxxxxx10xxxxxxxxxx", InstName.Prfm,            InstEmit.Prfm,            OpCodeMemReg.Create); // register
            SetA64("x101101011000000000000xxxxxxxxxx", InstName.Rbit,            InstEmit.Rbit,            OpCodeAlu.Create);
            SetA64("1101011001011111000000xxxxx00000", InstName.Ret,             InstEmit.Ret,             OpCodeBReg.Create);
            SetA64("x101101011000000000001xxxxxxxxxx", InstName.Rev16,           InstEmit.Rev16,           OpCodeAlu.Create);
            SetA64("x101101011000000000010xxxxxxxxxx", InstName.Rev32,           InstEmit.Rev32,           OpCodeAlu.Create);
            SetA64("1101101011000000000011xxxxxxxxxx", InstName.Rev64,           InstEmit.Rev64,           OpCodeAlu.Create);
            SetA64("x0011010110xxxxx001011xxxxxxxxxx", InstName.Rorv,            InstEmit.Rorv,            OpCodeAluRs.Create);
            SetA64("x1011010000xxxxx000000xxxxxxxxxx", InstName.Sbc,             InstEmit.Sbc,             OpCodeAluRs.Create);
            SetA64("x1111010000xxxxx000000xxxxxxxxxx", InstName.Sbcs,            InstEmit.Sbcs,            OpCodeAluRs.Create);
            SetA64("00010011000xxxxx0xxxxxxxxxxxxxxx", InstName.Sbfm,            InstEmit.Sbfm,            OpCodeBfm.Create);
            SetA64("1001001101xxxxxxxxxxxxxxxxxxxxxx", InstName.Sbfm,            InstEmit.Sbfm,            OpCodeBfm.Create);
            SetA64("x0011010110xxxxx000011xxxxxxxxxx", InstName.Sdiv,            InstEmit.Sdiv,            OpCodeAluBinary.Create);
            SetA64("11010101000000110010000010011111", InstName.Sev,             InstEmit.Nop,             OpCodeSystem.Create);
            SetA64("11010101000000110010000010111111", InstName.Sevl,            InstEmit.Nop,             OpCodeSystem.Create);
            SetA64("10011011001xxxxx0xxxxxxxxxxxxxxx", InstName.Smaddl,          InstEmit.Smaddl,          OpCodeMul.Create);
            SetA64("10011011001xxxxx1xxxxxxxxxxxxxxx", InstName.Smsubl,          InstEmit.Smsubl,          OpCodeMul.Create);
            SetA64("10011011010xxxxx0xxxxxxxxxxxxxxx", InstName.Smulh,           InstEmit.Smulh,           OpCodeMul.Create);
            SetA64("xx001000100xxxxx1xxxxxxxxxxxxxxx", InstName.Stlr,            InstEmit.Stlr,            OpCodeMemEx.Create);
            SetA64("1x001000001xxxxx1xxxxxxxxxxxxxxx", InstName.Stlxp,           InstEmit.Stlxp,           OpCodeMemEx.Create);
            SetA64("xx001000000xxxxx1xxxxxxxxxxxxxxx", InstName.Stlxr,           InstEmit.Stlxr,           OpCodeMemEx.Create);
            SetA64("x010100xx0xxxxxxxxxxxxxxxxxxxxxx", InstName.Stp,             InstEmit.Stp,             OpCodeMemPair.Create);
            SetA64("xx111000000xxxxxxxxxxxxxxxxxxxxx", InstName.Str,             InstEmit.Str,             OpCodeMemImm.Create);
            SetA64("xx11100100xxxxxxxxxxxxxxxxxxxxxx", InstName.Str,             InstEmit.Str,             OpCodeMemImm.Create);
            SetA64("xx111000001xxxxxxxxx10xxxxxxxxxx", InstName.Str,             InstEmit.Str,             OpCodeMemReg.Create);
            SetA64("1x001000001xxxxx0xxxxxxxxxxxxxxx", InstName.Stxp,            InstEmit.Stxp,            OpCodeMemEx.Create);
            SetA64("xx001000000xxxxx0xxxxxxxxxxxxxxx", InstName.Stxr,            InstEmit.Stxr,            OpCodeMemEx.Create);
            SetA64("x10100010xxxxxxxxxxxxxxxxxxxxxxx", InstName.Sub,             InstEmit.Sub,             OpCodeAluImm.Create);
            SetA64("01001011<<0xxxxx0xxxxxxxxxxxxxxx", InstName.Sub,             InstEmit.Sub,             OpCodeAluRs.Create);
            SetA64("11001011<<0xxxxxxxxxxxxxxxxxxxxx", InstName.Sub,             InstEmit.Sub,             OpCodeAluRs.Create);
            SetA64("x1001011001xxxxxxxx0xxxxxxxxxxxx", InstName.Sub,             InstEmit.Sub,             OpCodeAluRx.Create);
            SetA64("x1001011001xxxxxxxx100xxxxxxxxxx", InstName.Sub,             InstEmit.Sub,             OpCodeAluRx.Create);
            SetA64("x11100010xxxxxxxxxxxxxxxxxxxxxxx", InstName.Subs,            InstEmit.Subs,            OpCodeAluImm.Create);
            SetA64("01101011<<0xxxxx0xxxxxxxxxxxxxxx", InstName.Subs,            InstEmit.Subs,            OpCodeAluRs.Create);
            SetA64("11101011<<0xxxxxxxxxxxxxxxxxxxxx", InstName.Subs,            InstEmit.Subs,            OpCodeAluRs.Create);
            SetA64("x1101011001xxxxxxxx0xxxxxxxxxxxx", InstName.Subs,            InstEmit.Subs,            OpCodeAluRx.Create);
            SetA64("x1101011001xxxxxxxx100xxxxxxxxxx", InstName.Subs,            InstEmit.Subs,            OpCodeAluRx.Create);
            SetA64("11010100000xxxxxxxxxxxxxxxx00001", InstName.Svc,             InstEmit.Svc,             OpCodeException.Create);
            SetA64("1101010100001xxxxxxxxxxxxxxxxxxx", InstName.Sys,             InstEmit.Sys,             OpCodeSystem.Create);
            SetA64("x0110111xxxxxxxxxxxxxxxxxxxxxxxx", InstName.Tbnz,            InstEmit.Tbnz,            OpCodeBImmTest.Create);
            SetA64("x0110110xxxxxxxxxxxxxxxxxxxxxxxx", InstName.Tbz,             InstEmit.Tbz,             OpCodeBImmTest.Create);
            SetA64("01010011000xxxxx0xxxxxxxxxxxxxxx", InstName.Ubfm,            InstEmit.Ubfm,            OpCodeBfm.Create);
            SetA64("1101001101xxxxxxxxxxxxxxxxxxxxxx", InstName.Ubfm,            InstEmit.Ubfm,            OpCodeBfm.Create);
            SetA64("x0011010110xxxxx000010xxxxxxxxxx", InstName.Udiv,            InstEmit.Udiv,            OpCodeAluBinary.Create);
            SetA64("10011011101xxxxx0xxxxxxxxxxxxxxx", InstName.Umaddl,          InstEmit.Umaddl,          OpCodeMul.Create);
            SetA64("10011011101xxxxx1xxxxxxxxxxxxxxx", InstName.Umsubl,          InstEmit.Umsubl,          OpCodeMul.Create);
            SetA64("10011011110xxxxx0xxxxxxxxxxxxxxx", InstName.Umulh,           InstEmit.Umulh,           OpCodeMul.Create);
            SetA64("11010101000000110010000001011111", InstName.Wfe,             InstEmit.Nop,             OpCodeSystem.Create);
            SetA64("11010101000000110010000001111111", InstName.Wfi,             InstEmit.Nop,             OpCodeSystem.Create);
            SetA64("11010101000000110010000000111111", InstName.Yield,           InstEmit.Nop,             OpCodeSystem.Create);

            // FP & SIMD
            SetA64("0101111011100000101110xxxxxxxxxx", InstName.Abs_S,           InstEmit.Abs_S,           OpCodeSimd.Create);
            SetA64("0>001110<<100000101110xxxxxxxxxx", InstName.Abs_V,           InstEmit.Abs_V,           OpCodeSimd.Create);
            SetA64("01011110111xxxxx100001xxxxxxxxxx", InstName.Add_S,           InstEmit.Add_S,           OpCodeSimdReg.Create);
            SetA64("0>001110<<1xxxxx100001xxxxxxxxxx", InstName.Add_V,           InstEmit.Add_V,           OpCodeSimdReg.Create);
            SetA64("0x001110<<1xxxxx010000xxxxxxxxxx", InstName.Addhn_V,         InstEmit.Addhn_V,         OpCodeSimdReg.Create);
            SetA64("0101111011110001101110xxxxxxxxxx", InstName.Addp_S,          InstEmit.Addp_S,          OpCodeSimd.Create);
            SetA64("0>001110<<1xxxxx101111xxxxxxxxxx", InstName.Addp_V,          InstEmit.Addp_V,          OpCodeSimdReg.Create);
            SetA64("000011100x110001101110xxxxxxxxxx", InstName.Addv_V,          InstEmit.Addv_V,          OpCodeSimd.Create);
            SetA64("01001110<<110001101110xxxxxxxxxx", InstName.Addv_V,          InstEmit.Addv_V,          OpCodeSimd.Create);
            SetA64("0100111000101000010110xxxxxxxxxx", InstName.Aesd_V,          InstEmit.Aesd_V,          OpCodeSimd.Create);
            SetA64("0100111000101000010010xxxxxxxxxx", InstName.Aese_V,          InstEmit.Aese_V,          OpCodeSimd.Create);
            SetA64("0100111000101000011110xxxxxxxxxx", InstName.Aesimc_V,        InstEmit.Aesimc_V,        OpCodeSimd.Create);
            SetA64("0100111000101000011010xxxxxxxxxx", InstName.Aesmc_V,         InstEmit.Aesmc_V,         OpCodeSimd.Create);
            SetA64("0x001110001xxxxx000111xxxxxxxxxx", InstName.And_V,           InstEmit.And_V,           OpCodeSimdReg.Create);
            SetA64("0x001110011xxxxx000111xxxxxxxxxx", InstName.Bic_V,           InstEmit.Bic_V,           OpCodeSimdReg.Create);
            SetA64("0x10111100000xxx0xx101xxxxxxxxxx", InstName.Bic_Vi,          InstEmit.Bic_Vi,          OpCodeSimdImm.Create);
            SetA64("0x10111100000xxx10x101xxxxxxxxxx", InstName.Bic_Vi,          InstEmit.Bic_Vi,          OpCodeSimdImm.Create);
            SetA64("0x101110111xxxxx000111xxxxxxxxxx", InstName.Bif_V,           InstEmit.Bif_V,           OpCodeSimdReg.Create);
            SetA64("0x101110101xxxxx000111xxxxxxxxxx", InstName.Bit_V,           InstEmit.Bit_V,           OpCodeSimdReg.Create);
            SetA64("0x101110011xxxxx000111xxxxxxxxxx", InstName.Bsl_V,           InstEmit.Bsl_V,           OpCodeSimdReg.Create);
            SetA64("0x001110<<100000010010xxxxxxxxxx", InstName.Cls_V,           InstEmit.Cls_V,           OpCodeSimd.Create);
            SetA64("0x101110<<100000010010xxxxxxxxxx", InstName.Clz_V,           InstEmit.Clz_V,           OpCodeSimd.Create);
            SetA64("01111110111xxxxx100011xxxxxxxxxx", InstName.Cmeq_S,          InstEmit.Cmeq_S,          OpCodeSimdReg.Create);
            SetA64("0101111011100000100110xxxxxxxxxx", InstName.Cmeq_S,          InstEmit.Cmeq_S,          OpCodeSimd.Create);
            SetA64("0>101110<<1xxxxx100011xxxxxxxxxx", InstName.Cmeq_V,          InstEmit.Cmeq_V,          OpCodeSimdReg.Create);
            SetA64("0>001110<<100000100110xxxxxxxxxx", InstName.Cmeq_V,          InstEmit.Cmeq_V,          OpCodeSimd.Create);
            SetA64("01011110111xxxxx001111xxxxxxxxxx", InstName.Cmge_S,          InstEmit.Cmge_S,          OpCodeSimdReg.Create);
            SetA64("0111111011100000100010xxxxxxxxxx", InstName.Cmge_S,          InstEmit.Cmge_S,          OpCodeSimd.Create);
            SetA64("0>001110<<1xxxxx001111xxxxxxxxxx", InstName.Cmge_V,          InstEmit.Cmge_V,          OpCodeSimdReg.Create);
            SetA64("0>101110<<100000100010xxxxxxxxxx", InstName.Cmge_V,          InstEmit.Cmge_V,          OpCodeSimd.Create);
            SetA64("01011110111xxxxx001101xxxxxxxxxx", InstName.Cmgt_S,          InstEmit.Cmgt_S,          OpCodeSimdReg.Create);
            SetA64("0101111011100000100010xxxxxxxxxx", InstName.Cmgt_S,          InstEmit.Cmgt_S,          OpCodeSimd.Create);
            SetA64("0>001110<<1xxxxx001101xxxxxxxxxx", InstName.Cmgt_V,          InstEmit.Cmgt_V,          OpCodeSimdReg.Create);
            SetA64("0>001110<<100000100010xxxxxxxxxx", InstName.Cmgt_V,          InstEmit.Cmgt_V,          OpCodeSimd.Create);
            SetA64("01111110111xxxxx001101xxxxxxxxxx", InstName.Cmhi_S,          InstEmit.Cmhi_S,          OpCodeSimdReg.Create);
            SetA64("0>101110<<1xxxxx001101xxxxxxxxxx", InstName.Cmhi_V,          InstEmit.Cmhi_V,          OpCodeSimdReg.Create);
            SetA64("01111110111xxxxx001111xxxxxxxxxx", InstName.Cmhs_S,          InstEmit.Cmhs_S,          OpCodeSimdReg.Create);
            SetA64("0>101110<<1xxxxx001111xxxxxxxxxx", InstName.Cmhs_V,          InstEmit.Cmhs_V,          OpCodeSimdReg.Create);
            SetA64("0111111011100000100110xxxxxxxxxx", InstName.Cmle_S,          InstEmit.Cmle_S,          OpCodeSimd.Create);
            SetA64("0>101110<<100000100110xxxxxxxxxx", InstName.Cmle_V,          InstEmit.Cmle_V,          OpCodeSimd.Create);
            SetA64("0101111011100000101010xxxxxxxxxx", InstName.Cmlt_S,          InstEmit.Cmlt_S,          OpCodeSimd.Create);
            SetA64("0>001110<<100000101010xxxxxxxxxx", InstName.Cmlt_V,          InstEmit.Cmlt_V,          OpCodeSimd.Create);
            SetA64("01011110111xxxxx100011xxxxxxxxxx", InstName.Cmtst_S,         InstEmit.Cmtst_S,         OpCodeSimdReg.Create);
            SetA64("0>001110<<1xxxxx100011xxxxxxxxxx", InstName.Cmtst_V,         InstEmit.Cmtst_V,         OpCodeSimdReg.Create);
            SetA64("0x00111000100000010110xxxxxxxxxx", InstName.Cnt_V,           InstEmit.Cnt_V,           OpCodeSimd.Create);
            SetA64("0>001110000x<>>>000011xxxxxxxxxx", InstName.Dup_Gp,          InstEmit.Dup_Gp,          OpCodeSimdIns.Create);
            SetA64("01011110000xxxxx000001xxxxxxxxxx", InstName.Dup_S,           InstEmit.Dup_S,           OpCodeSimdIns.Create);
            SetA64("0>001110000x<>>>000001xxxxxxxxxx", InstName.Dup_V,           InstEmit.Dup_V,           OpCodeSimdIns.Create);
            SetA64("0x101110001xxxxx000111xxxxxxxxxx", InstName.Eor_V,           InstEmit.Eor_V,           OpCodeSimdReg.Create);
            SetA64("0>101110000xxxxx0<xxx0xxxxxxxxxx", InstName.Ext_V,           InstEmit.Ext_V,           OpCodeSimdExt.Create);
            SetA64("011111101x1xxxxx110101xxxxxxxxxx", InstName.Fabd_S,          InstEmit.Fabd_S,          OpCodeSimdReg.Create);
            SetA64("0>1011101<1xxxxx110101xxxxxxxxxx", InstName.Fabd_V,          InstEmit.Fabd_V,          OpCodeSimdReg.Create);
            SetA64("000111100x100000110000xxxxxxxxxx", InstName.Fabs_S,          InstEmit.Fabs_S,          OpCodeSimd.Create);
            SetA64("0>0011101<100000111110xxxxxxxxxx", InstName.Fabs_V,          InstEmit.Fabs_V,          OpCodeSimd.Create);
            SetA64("011111100x1xxxxx111011xxxxxxxxxx", InstName.Facge_S,         InstEmit.Facge_S,         OpCodeSimdReg.Create);
            SetA64("0>1011100<1xxxxx111011xxxxxxxxxx", InstName.Facge_V,         InstEmit.Facge_V,         OpCodeSimdReg.Create);
            SetA64("011111101x1xxxxx111011xxxxxxxxxx", InstName.Facgt_S,         InstEmit.Facgt_S,         OpCodeSimdReg.Create);
            SetA64("0>1011101<1xxxxx111011xxxxxxxxxx", InstName.Facgt_V,         InstEmit.Facgt_V,         OpCodeSimdReg.Create);
            SetA64("000111100x1xxxxx001010xxxxxxxxxx", InstName.Fadd_S,          InstEmit.Fadd_S,          OpCodeSimdReg.Create);
            SetA64("0>0011100<1xxxxx110101xxxxxxxxxx", InstName.Fadd_V,          InstEmit.Fadd_V,          OpCodeSimdReg.Create);
            SetA64("011111100x110000110110xxxxxxxxxx", InstName.Faddp_S,         InstEmit.Faddp_S,         OpCodeSimd.Create);
            SetA64("0>1011100<1xxxxx110101xxxxxxxxxx", InstName.Faddp_V,         InstEmit.Faddp_V,         OpCodeSimdReg.Create);
            SetA64("000111100x1xxxxxxxxx01xxxxx0xxxx", InstName.Fccmp_S,         InstEmit.Fccmp_S,         OpCodeSimdFcond.Create);
            SetA64("000111100x1xxxxxxxxx01xxxxx1xxxx", InstName.Fccmpe_S,        InstEmit.Fccmpe_S,        OpCodeSimdFcond.Create);
            SetA64("010111100x1xxxxx111001xxxxxxxxxx", InstName.Fcmeq_S,         InstEmit.Fcmeq_S,         OpCodeSimdReg.Create);
            SetA64("010111101x100000110110xxxxxxxxxx", InstName.Fcmeq_S,         InstEmit.Fcmeq_S,         OpCodeSimd.Create);
            SetA64("0>0011100<1xxxxx111001xxxxxxxxxx", InstName.Fcmeq_V,         InstEmit.Fcmeq_V,         OpCodeSimdReg.Create);
            SetA64("0>0011101<100000110110xxxxxxxxxx", InstName.Fcmeq_V,         InstEmit.Fcmeq_V,         OpCodeSimd.Create);
            SetA64("011111100x1xxxxx111001xxxxxxxxxx", InstName.Fcmge_S,         InstEmit.Fcmge_S,         OpCodeSimdReg.Create);
            SetA64("011111101x100000110010xxxxxxxxxx", InstName.Fcmge_S,         InstEmit.Fcmge_S,         OpCodeSimd.Create);
            SetA64("0>1011100<1xxxxx111001xxxxxxxxxx", InstName.Fcmge_V,         InstEmit.Fcmge_V,         OpCodeSimdReg.Create);
            SetA64("0>1011101<100000110010xxxxxxxxxx", InstName.Fcmge_V,         InstEmit.Fcmge_V,         OpCodeSimd.Create);
            SetA64("011111101x1xxxxx111001xxxxxxxxxx", InstName.Fcmgt_S,         InstEmit.Fcmgt_S,         OpCodeSimdReg.Create);
            SetA64("010111101x100000110010xxxxxxxxxx", InstName.Fcmgt_S,         InstEmit.Fcmgt_S,         OpCodeSimd.Create);
            SetA64("0>1011101<1xxxxx111001xxxxxxxxxx", InstName.Fcmgt_V,         InstEmit.Fcmgt_V,         OpCodeSimdReg.Create);
            SetA64("0>0011101<100000110010xxxxxxxxxx", InstName.Fcmgt_V,         InstEmit.Fcmgt_V,         OpCodeSimd.Create);
            SetA64("011111101x100000110110xxxxxxxxxx", InstName.Fcmle_S,         InstEmit.Fcmle_S,         OpCodeSimd.Create);
            SetA64("0>1011101<100000110110xxxxxxxxxx", InstName.Fcmle_V,         InstEmit.Fcmle_V,         OpCodeSimd.Create);
            SetA64("010111101x100000111010xxxxxxxxxx", InstName.Fcmlt_S,         InstEmit.Fcmlt_S,         OpCodeSimd.Create);
            SetA64("0>0011101<100000111010xxxxxxxxxx", InstName.Fcmlt_V,         InstEmit.Fcmlt_V,         OpCodeSimd.Create);
            SetA64("000111100x1xxxxx001000xxxxx0x000", InstName.Fcmp_S,          InstEmit.Fcmp_S,          OpCodeSimdReg.Create);
            SetA64("000111100x1xxxxx001000xxxxx1x000", InstName.Fcmpe_S,         InstEmit.Fcmpe_S,         OpCodeSimdReg.Create);
            SetA64("000111100x1xxxxxxxxx11xxxxxxxxxx", InstName.Fcsel_S,         InstEmit.Fcsel_S,         OpCodeSimdFcond.Create);
            SetA64("00011110xx10001xx10000xxxxxxxxxx", InstName.Fcvt_S,          InstEmit.Fcvt_S,          OpCodeSimd.Create);
            SetA64("x00111100x100100000000xxxxxxxxxx", InstName.Fcvtas_Gp,       InstEmit.Fcvtas_Gp,       OpCodeSimdCvt.Create);
            SetA64("010111100x100001110010xxxxxxxxxx", InstName.Fcvtas_S,        InstEmit.Fcvtas_S,        OpCodeSimd.Create);
            SetA64("0>0011100<100001110010xxxxxxxxxx", InstName.Fcvtas_V,        InstEmit.Fcvtas_V,        OpCodeSimd.Create);
            SetA64("x00111100x100101000000xxxxxxxxxx", InstName.Fcvtau_Gp,       InstEmit.Fcvtau_Gp,       OpCodeSimdCvt.Create);
            SetA64("011111100x100001110010xxxxxxxxxx", InstName.Fcvtau_S,        InstEmit.Fcvtau_S,        OpCodeSimd.Create);
            SetA64("0>1011100<100001110010xxxxxxxxxx", InstName.Fcvtau_V,        InstEmit.Fcvtau_V,        OpCodeSimd.Create);
            SetA64("0x0011100x100001011110xxxxxxxxxx", InstName.Fcvtl_V,         InstEmit.Fcvtl_V,         OpCodeSimd.Create);
            SetA64("x00111100x110000000000xxxxxxxxxx", InstName.Fcvtms_Gp,       InstEmit.Fcvtms_Gp,       OpCodeSimdCvt.Create);
            SetA64("0>0011100<100001101110xxxxxxxxxx", InstName.Fcvtms_V,        InstEmit.Fcvtms_V,        OpCodeSimd.Create);
            SetA64("x00111100x110001000000xxxxxxxxxx", InstName.Fcvtmu_Gp,       InstEmit.Fcvtmu_Gp,       OpCodeSimdCvt.Create);
            SetA64("0x0011100x100001011010xxxxxxxxxx", InstName.Fcvtn_V,         InstEmit.Fcvtn_V,         OpCodeSimd.Create);
            SetA64("x00111100x100000000000xxxxxxxxxx", InstName.Fcvtns_Gp,       InstEmit.Fcvtns_Gp,       OpCodeSimdCvt.Create);
            SetA64("010111100x100001101010xxxxxxxxxx", InstName.Fcvtns_S,        InstEmit.Fcvtns_S,        OpCodeSimd.Create);
            SetA64("0>0011100<100001101010xxxxxxxxxx", InstName.Fcvtns_V,        InstEmit.Fcvtns_V,        OpCodeSimd.Create);
            SetA64("011111100x100001101010xxxxxxxxxx", InstName.Fcvtnu_S,        InstEmit.Fcvtnu_S,        OpCodeSimd.Create);
            SetA64("0>1011100<100001101010xxxxxxxxxx", InstName.Fcvtnu_V,        InstEmit.Fcvtnu_V,        OpCodeSimd.Create);
            SetA64("x00111100x101000000000xxxxxxxxxx", InstName.Fcvtps_Gp,       InstEmit.Fcvtps_Gp,       OpCodeSimdCvt.Create);
            SetA64("x00111100x101001000000xxxxxxxxxx", InstName.Fcvtpu_Gp,       InstEmit.Fcvtpu_Gp,       OpCodeSimdCvt.Create);
            SetA64("x00111100x111000000000xxxxxxxxxx", InstName.Fcvtzs_Gp,       InstEmit.Fcvtzs_Gp,       OpCodeSimdCvt.Create);
            SetA64(">00111100x011000>xxxxxxxxxxxxxxx", InstName.Fcvtzs_Gp_Fixed, InstEmit.Fcvtzs_Gp_Fixed, OpCodeSimdCvt.Create);
            SetA64("010111101x100001101110xxxxxxxxxx", InstName.Fcvtzs_S,        InstEmit.Fcvtzs_S,        OpCodeSimd.Create);
            SetA64("0>0011101<100001101110xxxxxxxxxx", InstName.Fcvtzs_V,        InstEmit.Fcvtzs_V,        OpCodeSimd.Create);
            SetA64("0x001111001xxxxx111111xxxxxxxxxx", InstName.Fcvtzs_V_Fixed,  InstEmit.Fcvtzs_V_Fixed,  OpCodeSimdShImm.Create);
            SetA64("0100111101xxxxxx111111xxxxxxxxxx", InstName.Fcvtzs_V_Fixed,  InstEmit.Fcvtzs_V_Fixed,  OpCodeSimdShImm.Create);
            SetA64("x00111100x111001000000xxxxxxxxxx", InstName.Fcvtzu_Gp,       InstEmit.Fcvtzu_Gp,       OpCodeSimdCvt.Create);
            SetA64(">00111100x011001>xxxxxxxxxxxxxxx", InstName.Fcvtzu_Gp_Fixed, InstEmit.Fcvtzu_Gp_Fixed, OpCodeSimdCvt.Create);
            SetA64("011111101x100001101110xxxxxxxxxx", InstName.Fcvtzu_S,        InstEmit.Fcvtzu_S,        OpCodeSimd.Create);
            SetA64("0>1011101<100001101110xxxxxxxxxx", InstName.Fcvtzu_V,        InstEmit.Fcvtzu_V,        OpCodeSimd.Create);
            SetA64("0x101111001xxxxx111111xxxxxxxxxx", InstName.Fcvtzu_V_Fixed,  InstEmit.Fcvtzu_V_Fixed,  OpCodeSimdShImm.Create);
            SetA64("0110111101xxxxxx111111xxxxxxxxxx", InstName.Fcvtzu_V_Fixed,  InstEmit.Fcvtzu_V_Fixed,  OpCodeSimdShImm.Create);
            SetA64("000111100x1xxxxx000110xxxxxxxxxx", InstName.Fdiv_S,          InstEmit.Fdiv_S,          OpCodeSimdReg.Create);
            SetA64("0>1011100<1xxxxx111111xxxxxxxxxx", InstName.Fdiv_V,          InstEmit.Fdiv_V,          OpCodeSimdReg.Create);
            SetA64("000111110x0xxxxx0xxxxxxxxxxxxxxx", InstName.Fmadd_S,         InstEmit.Fmadd_S,         OpCodeSimdReg.Create);
            SetA64("000111100x1xxxxx010010xxxxxxxxxx", InstName.Fmax_S,          InstEmit.Fmax_S,          OpCodeSimdReg.Create);
            SetA64("0>0011100<1xxxxx111101xxxxxxxxxx", InstName.Fmax_V,          InstEmit.Fmax_V,          OpCodeSimdReg.Create);
            SetA64("000111100x1xxxxx011010xxxxxxxxxx", InstName.Fmaxnm_S,        InstEmit.Fmaxnm_S,        OpCodeSimdReg.Create);
            SetA64("0>0011100<1xxxxx110001xxxxxxxxxx", InstName.Fmaxnm_V,        InstEmit.Fmaxnm_V,        OpCodeSimdReg.Create);
            SetA64("011111100x110000110010xxxxxxxxxx", InstName.Fmaxnmp_S,       InstEmit.Fmaxnmp_S,       OpCodeSimd.Create);
            SetA64("0>1011100<1xxxxx110001xxxxxxxxxx", InstName.Fmaxnmp_V,       InstEmit.Fmaxnmp_V,       OpCodeSimdReg.Create);
            SetA64("0110111000110000110010xxxxxxxxxx", InstName.Fmaxnmv_V,       InstEmit.Fmaxnmv_V,       OpCodeSimd.Create);
            SetA64("011111100x110000111110xxxxxxxxxx", InstName.Fmaxp_S,         InstEmit.Fmaxp_S,         OpCodeSimd.Create);
            SetA64("0>1011100<1xxxxx111101xxxxxxxxxx", InstName.Fmaxp_V,         InstEmit.Fmaxp_V,         OpCodeSimdReg.Create);
            SetA64("0110111000110000111110xxxxxxxxxx", InstName.Fmaxv_V,         InstEmit.Fmaxv_V,         OpCodeSimd.Create);
            SetA64("000111100x1xxxxx010110xxxxxxxxxx", InstName.Fmin_S,          InstEmit.Fmin_S,          OpCodeSimdReg.Create);
            SetA64("0>0011101<1xxxxx111101xxxxxxxxxx", InstName.Fmin_V,          InstEmit.Fmin_V,          OpCodeSimdReg.Create);
            SetA64("000111100x1xxxxx011110xxxxxxxxxx", InstName.Fminnm_S,        InstEmit.Fminnm_S,        OpCodeSimdReg.Create);
            SetA64("0>0011101<1xxxxx110001xxxxxxxxxx", InstName.Fminnm_V,        InstEmit.Fminnm_V,        OpCodeSimdReg.Create);
            SetA64("011111101x110000110010xxxxxxxxxx", InstName.Fminnmp_S,       InstEmit.Fminnmp_S,       OpCodeSimd.Create);
            SetA64("0>1011101<1xxxxx110001xxxxxxxxxx", InstName.Fminnmp_V,       InstEmit.Fminnmp_V,       OpCodeSimdReg.Create);
            SetA64("0110111010110000110010xxxxxxxxxx", InstName.Fminnmv_V,       InstEmit.Fminnmv_V,       OpCodeSimd.Create);
            SetA64("011111101x110000111110xxxxxxxxxx", InstName.Fminp_S,         InstEmit.Fminp_S,         OpCodeSimd.Create);
            SetA64("0>1011101<1xxxxx111101xxxxxxxxxx", InstName.Fminp_V,         InstEmit.Fminp_V,         OpCodeSimdReg.Create);
            SetA64("0110111010110000111110xxxxxxxxxx", InstName.Fminv_V,         InstEmit.Fminv_V,         OpCodeSimd.Create);
            SetA64("010111111xxxxxxx0001x0xxxxxxxxxx", InstName.Fmla_Se,         InstEmit.Fmla_Se,         OpCodeSimdRegElemF.Create);
            SetA64("0>0011100<1xxxxx110011xxxxxxxxxx", InstName.Fmla_V,          InstEmit.Fmla_V,          OpCodeSimdReg.Create);
            SetA64("0>0011111<xxxxxx0001x0xxxxxxxxxx", InstName.Fmla_Ve,         InstEmit.Fmla_Ve,         OpCodeSimdRegElemF.Create);
            SetA64("010111111xxxxxxx0101x0xxxxxxxxxx", InstName.Fmls_Se,         InstEmit.Fmls_Se,         OpCodeSimdRegElemF.Create);
            SetA64("0>0011101<1xxxxx110011xxxxxxxxxx", InstName.Fmls_V,          InstEmit.Fmls_V,          OpCodeSimdReg.Create);
            SetA64("0>0011111<xxxxxx0101x0xxxxxxxxxx", InstName.Fmls_Ve,         InstEmit.Fmls_Ve,         OpCodeSimdRegElemF.Create);
            SetA64("000111100x100000010000xxxxxxxxxx", InstName.Fmov_S,          InstEmit.Fmov_S,          OpCodeSimd.Create);
            SetA64("000111100x1xxxxxxxx10000000xxxxx", InstName.Fmov_Si,         InstEmit.Fmov_Si,         OpCodeSimdFmov.Create);
            SetA64("0x00111100000xxx111101xxxxxxxxxx", InstName.Fmov_Vi,         InstEmit.Fmov_Vi,         OpCodeSimdImm.Create);
            SetA64("0110111100000xxx111101xxxxxxxxxx", InstName.Fmov_Vi,         InstEmit.Fmov_Vi,         OpCodeSimdImm.Create);
            SetA64("0001111000100110000000xxxxxxxxxx", InstName.Fmov_Ftoi,       InstEmit.Fmov_Ftoi,       OpCodeSimd.Create);
            SetA64("1001111001100110000000xxxxxxxxxx", InstName.Fmov_Ftoi,       InstEmit.Fmov_Ftoi,       OpCodeSimd.Create);
            SetA64("0001111000100111000000xxxxxxxxxx", InstName.Fmov_Itof,       InstEmit.Fmov_Itof,       OpCodeSimd.Create);
            SetA64("1001111001100111000000xxxxxxxxxx", InstName.Fmov_Itof,       InstEmit.Fmov_Itof,       OpCodeSimd.Create);
            SetA64("1001111010101110000000xxxxxxxxxx", InstName.Fmov_Ftoi1,      InstEmit.Fmov_Ftoi1,      OpCodeSimd.Create);
            SetA64("1001111010101111000000xxxxxxxxxx", InstName.Fmov_Itof1,      InstEmit.Fmov_Itof1,      OpCodeSimd.Create);
            SetA64("000111110x0xxxxx1xxxxxxxxxxxxxxx", InstName.Fmsub_S,         InstEmit.Fmsub_S,         OpCodeSimdReg.Create);
            SetA64("000111100x1xxxxx000010xxxxxxxxxx", InstName.Fmul_S,          InstEmit.Fmul_S,          OpCodeSimdReg.Create);
            SetA64("010111111xxxxxxx1001x0xxxxxxxxxx", InstName.Fmul_Se,         InstEmit.Fmul_Se,         OpCodeSimdRegElemF.Create);
            SetA64("0>1011100<1xxxxx110111xxxxxxxxxx", InstName.Fmul_V,          InstEmit.Fmul_V,          OpCodeSimdReg.Create);
            SetA64("0>0011111<xxxxxx1001x0xxxxxxxxxx", InstName.Fmul_Ve,         InstEmit.Fmul_Ve,         OpCodeSimdRegElemF.Create);
            SetA64("010111100x1xxxxx110111xxxxxxxxxx", InstName.Fmulx_S,         InstEmit.Fmulx_S,         OpCodeSimdReg.Create);
            SetA64("011111111xxxxxxx1001x0xxxxxxxxxx", InstName.Fmulx_Se,        InstEmit.Fmulx_Se,        OpCodeSimdRegElemF.Create);
            SetA64("0>0011100<1xxxxx110111xxxxxxxxxx", InstName.Fmulx_V,         InstEmit.Fmulx_V,         OpCodeSimdReg.Create);
            SetA64("0>1011111<xxxxxx1001x0xxxxxxxxxx", InstName.Fmulx_Ve,        InstEmit.Fmulx_Ve,        OpCodeSimdRegElemF.Create);
            SetA64("000111100x100001010000xxxxxxxxxx", InstName.Fneg_S,          InstEmit.Fneg_S,          OpCodeSimd.Create);
            SetA64("0>1011101<100000111110xxxxxxxxxx", InstName.Fneg_V,          InstEmit.Fneg_V,          OpCodeSimd.Create);
            SetA64("000111110x1xxxxx0xxxxxxxxxxxxxxx", InstName.Fnmadd_S,        InstEmit.Fnmadd_S,        OpCodeSimdReg.Create);
            SetA64("000111110x1xxxxx1xxxxxxxxxxxxxxx", InstName.Fnmsub_S,        InstEmit.Fnmsub_S,        OpCodeSimdReg.Create);
            SetA64("000111100x1xxxxx100010xxxxxxxxxx", InstName.Fnmul_S,         InstEmit.Fnmul_S,         OpCodeSimdReg.Create);
            SetA64("010111101x100001110110xxxxxxxxxx", InstName.Frecpe_S,        InstEmit.Frecpe_S,        OpCodeSimd.Create);
            SetA64("0>0011101<100001110110xxxxxxxxxx", InstName.Frecpe_V,        InstEmit.Frecpe_V,        OpCodeSimd.Create);
            SetA64("010111100x1xxxxx111111xxxxxxxxxx", InstName.Frecps_S,        InstEmit.Frecps_S,        OpCodeSimdReg.Create);
            SetA64("0>0011100<1xxxxx111111xxxxxxxxxx", InstName.Frecps_V,        InstEmit.Frecps_V,        OpCodeSimdReg.Create);
            SetA64("010111101x100001111110xxxxxxxxxx", InstName.Frecpx_S,        InstEmit.Frecpx_S,        OpCodeSimd.Create);
            SetA64("000111100x100110010000xxxxxxxxxx", InstName.Frinta_S,        InstEmit.Frinta_S,        OpCodeSimd.Create);
            SetA64("0>1011100<100001100010xxxxxxxxxx", InstName.Frinta_V,        InstEmit.Frinta_V,        OpCodeSimd.Create);
            SetA64("000111100x100111110000xxxxxxxxxx", InstName.Frinti_S,        InstEmit.Frinti_S,        OpCodeSimd.Create);
            SetA64("0>1011101<100001100110xxxxxxxxxx", InstName.Frinti_V,        InstEmit.Frinti_V,        OpCodeSimd.Create);
            SetA64("000111100x100101010000xxxxxxxxxx", InstName.Frintm_S,        InstEmit.Frintm_S,        OpCodeSimd.Create);
            SetA64("0>0011100<100001100110xxxxxxxxxx", InstName.Frintm_V,        InstEmit.Frintm_V,        OpCodeSimd.Create);
            SetA64("000111100x100100010000xxxxxxxxxx", InstName.Frintn_S,        InstEmit.Frintn_S,        OpCodeSimd.Create);
            SetA64("0>0011100<100001100010xxxxxxxxxx", InstName.Frintn_V,        InstEmit.Frintn_V,        OpCodeSimd.Create);
            SetA64("000111100x100100110000xxxxxxxxxx", InstName.Frintp_S,        InstEmit.Frintp_S,        OpCodeSimd.Create);
            SetA64("0>0011101<100001100010xxxxxxxxxx", InstName.Frintp_V,        InstEmit.Frintp_V,        OpCodeSimd.Create);
            SetA64("000111100x100111010000xxxxxxxxxx", InstName.Frintx_S,        InstEmit.Frintx_S,        OpCodeSimd.Create);
            SetA64("0>1011100<100001100110xxxxxxxxxx", InstName.Frintx_V,        InstEmit.Frintx_V,        OpCodeSimd.Create);
            SetA64("000111100x100101110000xxxxxxxxxx", InstName.Frintz_S,        InstEmit.Frintz_S,        OpCodeSimd.Create);
            SetA64("0>0011101<100001100110xxxxxxxxxx", InstName.Frintz_V,        InstEmit.Frintz_V,        OpCodeSimd.Create);
            SetA64("011111101x100001110110xxxxxxxxxx", InstName.Frsqrte_S,       InstEmit.Frsqrte_S,       OpCodeSimd.Create);
            SetA64("0>1011101<100001110110xxxxxxxxxx", InstName.Frsqrte_V,       InstEmit.Frsqrte_V,       OpCodeSimd.Create);
            SetA64("010111101x1xxxxx111111xxxxxxxxxx", InstName.Frsqrts_S,       InstEmit.Frsqrts_S,       OpCodeSimdReg.Create);
            SetA64("0>0011101<1xxxxx111111xxxxxxxxxx", InstName.Frsqrts_V,       InstEmit.Frsqrts_V,       OpCodeSimdReg.Create);
            SetA64("000111100x100001110000xxxxxxxxxx", InstName.Fsqrt_S,         InstEmit.Fsqrt_S,         OpCodeSimd.Create);
            SetA64("0>1011101<100001111110xxxxxxxxxx", InstName.Fsqrt_V,         InstEmit.Fsqrt_V,         OpCodeSimd.Create);
            SetA64("000111100x1xxxxx001110xxxxxxxxxx", InstName.Fsub_S,          InstEmit.Fsub_S,          OpCodeSimdReg.Create);
            SetA64("0>0011101<1xxxxx110101xxxxxxxxxx", InstName.Fsub_V,          InstEmit.Fsub_V,          OpCodeSimdReg.Create);
            SetA64("01001110000xxxxx000111xxxxxxxxxx", InstName.Ins_Gp,          InstEmit.Ins_Gp,          OpCodeSimdIns.Create);
            SetA64("01101110000xxxxx0xxxx1xxxxxxxxxx", InstName.Ins_V,           InstEmit.Ins_V,           OpCodeSimdIns.Create);
            SetA64("0x00110001000000xxxxxxxxxxxxxxxx", InstName.Ld__Vms,         InstEmit.Ld__Vms,         OpCodeSimdMemMs.Create);
            SetA64("0x001100110xxxxxxxxxxxxxxxxxxxxx", InstName.Ld__Vms,         InstEmit.Ld__Vms,         OpCodeSimdMemMs.Create);
            SetA64("0x00110101x00000xxxxxxxxxxxxxxxx", InstName.Ld__Vss,         InstEmit.Ld__Vss,         OpCodeSimdMemSs.Create);
            SetA64("0x00110111xxxxxxxxxxxxxxxxxxxxxx", InstName.Ld__Vss,         InstEmit.Ld__Vss,         OpCodeSimdMemSs.Create);
            SetA64("<<10110xx1xxxxxxxxxxxxxxxxxxxxxx", InstName.Ldp,             InstEmit.Ldp,             OpCodeSimdMemPair.Create);
            SetA64("xx111100x10xxxxxxxxx00xxxxxxxxxx", InstName.Ldr,             InstEmit.Ldr,             OpCodeSimdMemImm.Create);
            SetA64("xx111100x10xxxxxxxxx01xxxxxxxxxx", InstName.Ldr,             InstEmit.Ldr,             OpCodeSimdMemImm.Create);
            SetA64("xx111100x10xxxxxxxxx11xxxxxxxxxx", InstName.Ldr,             InstEmit.Ldr,             OpCodeSimdMemImm.Create);
            SetA64("xx111101x1xxxxxxxxxxxxxxxxxxxxxx", InstName.Ldr,             InstEmit.Ldr,             OpCodeSimdMemImm.Create);
            SetA64("xx111100x11xxxxxx1xx10xxxxxxxxxx", InstName.Ldr,             InstEmit.Ldr,             OpCodeSimdMemReg.Create);
            SetA64("xx011100xxxxxxxxxxxxxxxxxxxxxxxx", InstName.Ldr_Literal,     InstEmit.Ldr_Literal,     OpCodeSimdMemLit.Create);
            SetA64("0x001110<<1xxxxx100101xxxxxxxxxx", InstName.Mla_V,           InstEmit.Mla_V,           OpCodeSimdReg.Create);
            SetA64("0x101111xxxxxxxx0000x0xxxxxxxxxx", InstName.Mla_Ve,          InstEmit.Mla_Ve,          OpCodeSimdRegElem.Create);
            SetA64("0x101110<<1xxxxx100101xxxxxxxxxx", InstName.Mls_V,           InstEmit.Mls_V,           OpCodeSimdReg.Create);
            SetA64("0x101111xxxxxxxx0100x0xxxxxxxxxx", InstName.Mls_Ve,          InstEmit.Mls_Ve,          OpCodeSimdRegElem.Create);
            SetA64("0x00111100000xxx0xx001xxxxxxxxxx", InstName.Movi_V,          InstEmit.Movi_V,          OpCodeSimdImm.Create);
            SetA64("0x00111100000xxx10x001xxxxxxxxxx", InstName.Movi_V,          InstEmit.Movi_V,          OpCodeSimdImm.Create);
            SetA64("0x00111100000xxx110x01xxxxxxxxxx", InstName.Movi_V,          InstEmit.Movi_V,          OpCodeSimdImm.Create);
            SetA64("0xx0111100000xxx111001xxxxxxxxxx", InstName.Movi_V,          InstEmit.Movi_V,          OpCodeSimdImm.Create);
            SetA64("0x001110<<1xxxxx100111xxxxxxxxxx", InstName.Mul_V,           InstEmit.Mul_V,           OpCodeSimdReg.Create);
            SetA64("0x001111xxxxxxxx1000x0xxxxxxxxxx", InstName.Mul_Ve,          InstEmit.Mul_Ve,          OpCodeSimdRegElem.Create);
            SetA64("0x10111100000xxx0xx001xxxxxxxxxx", InstName.Mvni_V,          InstEmit.Mvni_V,          OpCodeSimdImm.Create);
            SetA64("0x10111100000xxx10x001xxxxxxxxxx", InstName.Mvni_V,          InstEmit.Mvni_V,          OpCodeSimdImm.Create);
            SetA64("0x10111100000xxx110x01xxxxxxxxxx", InstName.Mvni_V,          InstEmit.Mvni_V,          OpCodeSimdImm.Create);
            SetA64("0111111011100000101110xxxxxxxxxx", InstName.Neg_S,           InstEmit.Neg_S,           OpCodeSimd.Create);
            SetA64("0>101110<<100000101110xxxxxxxxxx", InstName.Neg_V,           InstEmit.Neg_V,           OpCodeSimd.Create);
            SetA64("0x10111000100000010110xxxxxxxxxx", InstName.Not_V,           InstEmit.Not_V,           OpCodeSimd.Create);
            SetA64("0x001110111xxxxx000111xxxxxxxxxx", InstName.Orn_V,           InstEmit.Orn_V,           OpCodeSimdReg.Create);
            SetA64("0x001110101xxxxx000111xxxxxxxxxx", InstName.Orr_V,           InstEmit.Orr_V,           OpCodeSimdReg.Create);
            SetA64("0x00111100000xxx0xx101xxxxxxxxxx", InstName.Orr_Vi,          InstEmit.Orr_Vi,          OpCodeSimdImm.Create);
            SetA64("0x00111100000xxx10x101xxxxxxxxxx", InstName.Orr_Vi,          InstEmit.Orr_Vi,          OpCodeSimdImm.Create);
            SetA64("0x001110001xxxxx111000xxxxxxxxxx", InstName.Pmull_V,         InstEmit.Pmull_V,         OpCodeSimdReg.Create);
            SetA64("0x001110111xxxxx111000xxxxxxxxxx", InstName.Pmull_V,         InstEmit.Pmull_V,         OpCodeSimdReg.Create);
            SetA64("0x101110<<1xxxxx010000xxxxxxxxxx", InstName.Raddhn_V,        InstEmit.Raddhn_V,        OpCodeSimdReg.Create);
            SetA64("0x10111001100000010110xxxxxxxxxx", InstName.Rbit_V,          InstEmit.Rbit_V,          OpCodeSimd.Create);
            SetA64("0x00111000100000000110xxxxxxxxxx", InstName.Rev16_V,         InstEmit.Rev16_V,         OpCodeSimd.Create);
            SetA64("0x1011100x100000000010xxxxxxxxxx", InstName.Rev32_V,         InstEmit.Rev32_V,         OpCodeSimd.Create);
            SetA64("0x001110<<100000000010xxxxxxxxxx", InstName.Rev64_V,         InstEmit.Rev64_V,         OpCodeSimd.Create);
            SetA64("0x00111100>>>xxx100011xxxxxxxxxx", InstName.Rshrn_V,         InstEmit.Rshrn_V,         OpCodeSimdShImm.Create);
            SetA64("0x101110<<1xxxxx011000xxxxxxxxxx", InstName.Rsubhn_V,        InstEmit.Rsubhn_V,        OpCodeSimdReg.Create);
            SetA64("0x001110<<1xxxxx011111xxxxxxxxxx", InstName.Saba_V,          InstEmit.Saba_V,          OpCodeSimdReg.Create);
            SetA64("0x001110<<1xxxxx010100xxxxxxxxxx", InstName.Sabal_V,         InstEmit.Sabal_V,         OpCodeSimdReg.Create);
            SetA64("0x001110<<1xxxxx011101xxxxxxxxxx", InstName.Sabd_V,          InstEmit.Sabd_V,          OpCodeSimdReg.Create);
            SetA64("0x001110<<1xxxxx011100xxxxxxxxxx", InstName.Sabdl_V,         InstEmit.Sabdl_V,         OpCodeSimdReg.Create);
            SetA64("0x001110<<100000011010xxxxxxxxxx", InstName.Sadalp_V,        InstEmit.Sadalp_V,        OpCodeSimd.Create);
            SetA64("0x001110<<1xxxxx000000xxxxxxxxxx", InstName.Saddl_V,         InstEmit.Saddl_V,         OpCodeSimdReg.Create);
            SetA64("0x001110<<100000001010xxxxxxxxxx", InstName.Saddlp_V,        InstEmit.Saddlp_V,        OpCodeSimd.Create);
            SetA64("000011100x110000001110xxxxxxxxxx", InstName.Saddlv_V,        InstEmit.Saddlv_V,        OpCodeSimd.Create);
            SetA64("01001110<<110000001110xxxxxxxxxx", InstName.Saddlv_V,        InstEmit.Saddlv_V,        OpCodeSimd.Create);
            SetA64("0x001110<<1xxxxx000100xxxxxxxxxx", InstName.Saddw_V,         InstEmit.Saddw_V,         OpCodeSimdReg.Create);
            SetA64("x00111100x100010000000xxxxxxxxxx", InstName.Scvtf_Gp,        InstEmit.Scvtf_Gp,        OpCodeSimdCvt.Create);
            SetA64(">00111100x000010>xxxxxxxxxxxxxxx", InstName.Scvtf_Gp_Fixed,  InstEmit.Scvtf_Gp_Fixed,  OpCodeSimdCvt.Create);
            SetA64("010111100x100001110110xxxxxxxxxx", InstName.Scvtf_S,         InstEmit.Scvtf_S,         OpCodeSimd.Create);
            SetA64("010111110>>xxxxx111001xxxxxxxxxx", InstName.Scvtf_S_Fixed,   InstEmit.Scvtf_S_Fixed,   OpCodeSimdShImm.Create);
            SetA64("0>0011100<100001110110xxxxxxxxxx", InstName.Scvtf_V,         InstEmit.Scvtf_V,         OpCodeSimd.Create);
            SetA64("0x001111001xxxxx111001xxxxxxxxxx", InstName.Scvtf_V_Fixed,   InstEmit.Scvtf_V_Fixed,   OpCodeSimdShImm.Create);
            SetA64("0100111101xxxxxx111001xxxxxxxxxx", InstName.Scvtf_V_Fixed,   InstEmit.Scvtf_V_Fixed,   OpCodeSimdShImm.Create);
            SetA64("01011110000xxxxx000000xxxxxxxxxx", InstName.Sha1c_V,         InstEmit.Sha1c_V,         OpCodeSimdReg.Create);
            SetA64("0101111000101000000010xxxxxxxxxx", InstName.Sha1h_V,         InstEmit.Sha1h_V,         OpCodeSimd.Create);
            SetA64("01011110000xxxxx001000xxxxxxxxxx", InstName.Sha1m_V,         InstEmit.Sha1m_V,         OpCodeSimdReg.Create);
            SetA64("01011110000xxxxx000100xxxxxxxxxx", InstName.Sha1p_V,         InstEmit.Sha1p_V,         OpCodeSimdReg.Create);
            SetA64("01011110000xxxxx001100xxxxxxxxxx", InstName.Sha1su0_V,       InstEmit.Sha1su0_V,       OpCodeSimdReg.Create);
            SetA64("0101111000101000000110xxxxxxxxxx", InstName.Sha1su1_V,       InstEmit.Sha1su1_V,       OpCodeSimd.Create);
            SetA64("01011110000xxxxx010000xxxxxxxxxx", InstName.Sha256h_V,       InstEmit.Sha256h_V,       OpCodeSimdReg.Create);
            SetA64("01011110000xxxxx010100xxxxxxxxxx", InstName.Sha256h2_V,      InstEmit.Sha256h2_V,      OpCodeSimdReg.Create);
            SetA64("0101111000101000001010xxxxxxxxxx", InstName.Sha256su0_V,     InstEmit.Sha256su0_V,     OpCodeSimd.Create);
            SetA64("01011110000xxxxx011000xxxxxxxxxx", InstName.Sha256su1_V,     InstEmit.Sha256su1_V,     OpCodeSimdReg.Create);
            SetA64("0x001110<<1xxxxx000001xxxxxxxxxx", InstName.Shadd_V,         InstEmit.Shadd_V,         OpCodeSimdReg.Create);
            SetA64("0101111101xxxxxx010101xxxxxxxxxx", InstName.Shl_S,           InstEmit.Shl_S,           OpCodeSimdShImm.Create);
            SetA64("0x00111100>>>xxx010101xxxxxxxxxx", InstName.Shl_V,           InstEmit.Shl_V,           OpCodeSimdShImm.Create);
            SetA64("0100111101xxxxxx010101xxxxxxxxxx", InstName.Shl_V,           InstEmit.Shl_V,           OpCodeSimdShImm.Create);
            SetA64("0x101110<<100001001110xxxxxxxxxx", InstName.Shll_V,          InstEmit.Shll_V,          OpCodeSimd.Create);
            SetA64("0x00111100>>>xxx100001xxxxxxxxxx", InstName.Shrn_V,          InstEmit.Shrn_V,          OpCodeSimdShImm.Create);
            SetA64("0x001110<<1xxxxx001001xxxxxxxxxx", InstName.Shsub_V,         InstEmit.Shsub_V,         OpCodeSimdReg.Create);
            SetA64("0111111101xxxxxx010101xxxxxxxxxx", InstName.Sli_S,           InstEmit.Sli_S,           OpCodeSimdShImm.Create);
            SetA64("0x10111100>>>xxx010101xxxxxxxxxx", InstName.Sli_V,           InstEmit.Sli_V,           OpCodeSimdShImm.Create);
            SetA64("0110111101xxxxxx010101xxxxxxxxxx", InstName.Sli_V,           InstEmit.Sli_V,           OpCodeSimdShImm.Create);
            SetA64("0x001110<<1xxxxx011001xxxxxxxxxx", InstName.Smax_V,          InstEmit.Smax_V,          OpCodeSimdReg.Create);
            SetA64("0x001110<<1xxxxx101001xxxxxxxxxx", InstName.Smaxp_V,         InstEmit.Smaxp_V,         OpCodeSimdReg.Create);
            SetA64("000011100x110000101010xxxxxxxxxx", InstName.Smaxv_V,         InstEmit.Smaxv_V,         OpCodeSimd.Create);
            SetA64("01001110<<110000101010xxxxxxxxxx", InstName.Smaxv_V,         InstEmit.Smaxv_V,         OpCodeSimd.Create);
            SetA64("0x001110<<1xxxxx011011xxxxxxxxxx", InstName.Smin_V,          InstEmit.Smin_V,          OpCodeSimdReg.Create);
            SetA64("0x001110<<1xxxxx101011xxxxxxxxxx", InstName.Sminp_V,         InstEmit.Sminp_V,         OpCodeSimdReg.Create);
            SetA64("000011100x110001101010xxxxxxxxxx", InstName.Sminv_V,         InstEmit.Sminv_V,         OpCodeSimd.Create);
            SetA64("01001110<<110001101010xxxxxxxxxx", InstName.Sminv_V,         InstEmit.Sminv_V,         OpCodeSimd.Create);
            SetA64("0x001110<<1xxxxx100000xxxxxxxxxx", InstName.Smlal_V,         InstEmit.Smlal_V,         OpCodeSimdReg.Create);
            SetA64("0x001111xxxxxxxx0010x0xxxxxxxxxx", InstName.Smlal_Ve,        InstEmit.Smlal_Ve,        OpCodeSimdRegElem.Create);
            SetA64("0x001110<<1xxxxx101000xxxxxxxxxx", InstName.Smlsl_V,         InstEmit.Smlsl_V,         OpCodeSimdReg.Create);
            SetA64("0x001111xxxxxxxx0110x0xxxxxxxxxx", InstName.Smlsl_Ve,        InstEmit.Smlsl_Ve,        OpCodeSimdRegElem.Create);
            SetA64("0x001110000xxxxx001011xxxxxxxxxx", InstName.Smov_S,          InstEmit.Smov_S,          OpCodeSimdIns.Create);
            SetA64("0x001110<<1xxxxx110000xxxxxxxxxx", InstName.Smull_V,         InstEmit.Smull_V,         OpCodeSimdReg.Create);
            SetA64("0x001111xxxxxxxx1010x0xxxxxxxxxx", InstName.Smull_Ve,        InstEmit.Smull_Ve,        OpCodeSimdRegElem.Create);
            SetA64("01011110xx100000011110xxxxxxxxxx", InstName.Sqabs_S,         InstEmit.Sqabs_S,         OpCodeSimd.Create);
            SetA64("0>001110<<100000011110xxxxxxxxxx", InstName.Sqabs_V,         InstEmit.Sqabs_V,         OpCodeSimd.Create);
            SetA64("01011110xx1xxxxx000011xxxxxxxxxx", InstName.Sqadd_S,         InstEmit.Sqadd_S,         OpCodeSimdReg.Create);
            SetA64("0>001110<<1xxxxx000011xxxxxxxxxx", InstName.Sqadd_V,         InstEmit.Sqadd_V,         OpCodeSimdReg.Create);
            SetA64("01011110011xxxxx101101xxxxxxxxxx", InstName.Sqdmulh_S,       InstEmit.Sqdmulh_S,       OpCodeSimdReg.Create);
            SetA64("01011110101xxxxx101101xxxxxxxxxx", InstName.Sqdmulh_S,       InstEmit.Sqdmulh_S,       OpCodeSimdReg.Create);
            SetA64("0x001110011xxxxx101101xxxxxxxxxx", InstName.Sqdmulh_V,       InstEmit.Sqdmulh_V,       OpCodeSimdReg.Create);
            SetA64("0x001110101xxxxx101101xxxxxxxxxx", InstName.Sqdmulh_V,       InstEmit.Sqdmulh_V,       OpCodeSimdReg.Create);
            SetA64("0x00111101xxxxxx1100x0xxxxxxxxxx", InstName.Sqdmulh_Ve,      InstEmit.Sqdmulh_Ve,      OpCodeSimdRegElem.Create);
            SetA64("0x00111110xxxxxx1100x0xxxxxxxxxx", InstName.Sqdmulh_Ve,      InstEmit.Sqdmulh_Ve,      OpCodeSimdRegElem.Create);
            SetA64("01111110xx100000011110xxxxxxxxxx", InstName.Sqneg_S,         InstEmit.Sqneg_S,         OpCodeSimd.Create);
            SetA64("0>101110<<100000011110xxxxxxxxxx", InstName.Sqneg_V,         InstEmit.Sqneg_V,         OpCodeSimd.Create);
            SetA64("01111110011xxxxx101101xxxxxxxxxx", InstName.Sqrdmulh_S,      InstEmit.Sqrdmulh_S,      OpCodeSimdReg.Create);
            SetA64("01111110101xxxxx101101xxxxxxxxxx", InstName.Sqrdmulh_S,      InstEmit.Sqrdmulh_S,      OpCodeSimdReg.Create);
            SetA64("0x101110011xxxxx101101xxxxxxxxxx", InstName.Sqrdmulh_V,      InstEmit.Sqrdmulh_V,      OpCodeSimdReg.Create);
            SetA64("0x101110101xxxxx101101xxxxxxxxxx", InstName.Sqrdmulh_V,      InstEmit.Sqrdmulh_V,      OpCodeSimdReg.Create);
            SetA64("0x00111101xxxxxx1101x0xxxxxxxxxx", InstName.Sqrdmulh_Ve,     InstEmit.Sqrdmulh_Ve,     OpCodeSimdRegElem.Create);
            SetA64("0x00111110xxxxxx1101x0xxxxxxxxxx", InstName.Sqrdmulh_Ve,     InstEmit.Sqrdmulh_Ve,     OpCodeSimdRegElem.Create);
            SetA64("0>001110<<1xxxxx010111xxxxxxxxxx", InstName.Sqrshl_V,        InstEmit.Sqrshl_V,        OpCodeSimdReg.Create);
            SetA64("0101111100>>>xxx100111xxxxxxxxxx", InstName.Sqrshrn_S,       InstEmit.Sqrshrn_S,       OpCodeSimdShImm.Create);
            SetA64("0x00111100>>>xxx100111xxxxxxxxxx", InstName.Sqrshrn_V,       InstEmit.Sqrshrn_V,       OpCodeSimdShImm.Create);
            SetA64("0111111100>>>xxx100011xxxxxxxxxx", InstName.Sqrshrun_S,      InstEmit.Sqrshrun_S,      OpCodeSimdShImm.Create);
            SetA64("0x10111100>>>xxx100011xxxxxxxxxx", InstName.Sqrshrun_V,      InstEmit.Sqrshrun_V,      OpCodeSimdShImm.Create);
            SetA64("010111110>>>>xxx011101xxxxxxxxxx", InstName.Sqshl_Si,        InstEmit.Sqshl_Si,        OpCodeSimdShImm.Create);
            SetA64("0>001110<<1xxxxx010011xxxxxxxxxx", InstName.Sqshl_V,         InstEmit.Sqshl_V,         OpCodeSimdReg.Create);
            SetA64("0000111100>>>xxx011101xxxxxxxxxx", InstName.Sqshl_Vi,        InstEmit.Sqshl_Vi,        OpCodeSimdShImm.Create);
            SetA64("010011110>>>>xxx011101xxxxxxxxxx", InstName.Sqshl_Vi,        InstEmit.Sqshl_Vi,        OpCodeSimdShImm.Create);
            SetA64("0101111100>>>xxx100101xxxxxxxxxx", InstName.Sqshrn_S,        InstEmit.Sqshrn_S,        OpCodeSimdShImm.Create);
            SetA64("0x00111100>>>xxx100101xxxxxxxxxx", InstName.Sqshrn_V,        InstEmit.Sqshrn_V,        OpCodeSimdShImm.Create);
            SetA64("0111111100>>>xxx100001xxxxxxxxxx", InstName.Sqshrun_S,       InstEmit.Sqshrun_S,       OpCodeSimdShImm.Create);
            SetA64("0x10111100>>>xxx100001xxxxxxxxxx", InstName.Sqshrun_V,       InstEmit.Sqshrun_V,       OpCodeSimdShImm.Create);
            SetA64("01011110xx1xxxxx001011xxxxxxxxxx", InstName.Sqsub_S,         InstEmit.Sqsub_S,         OpCodeSimdReg.Create);
            SetA64("0>001110<<1xxxxx001011xxxxxxxxxx", InstName.Sqsub_V,         InstEmit.Sqsub_V,         OpCodeSimdReg.Create);
            SetA64("01011110<<100001010010xxxxxxxxxx", InstName.Sqxtn_S,         InstEmit.Sqxtn_S,         OpCodeSimd.Create);
            SetA64("0x001110<<100001010010xxxxxxxxxx", InstName.Sqxtn_V,         InstEmit.Sqxtn_V,         OpCodeSimd.Create);
            SetA64("01111110<<100001001010xxxxxxxxxx", InstName.Sqxtun_S,        InstEmit.Sqxtun_S,        OpCodeSimd.Create);
            SetA64("0x101110<<100001001010xxxxxxxxxx", InstName.Sqxtun_V,        InstEmit.Sqxtun_V,        OpCodeSimd.Create);
            SetA64("0x001110<<1xxxxx000101xxxxxxxxxx", InstName.Srhadd_V,        InstEmit.Srhadd_V,        OpCodeSimdReg.Create);
            SetA64("0111111101xxxxxx010001xxxxxxxxxx", InstName.Sri_S,           InstEmit.Sri_S,           OpCodeSimdShImm.Create);
            SetA64("0x10111100>>>xxx010001xxxxxxxxxx", InstName.Sri_V,           InstEmit.Sri_V,           OpCodeSimdShImm.Create);
            SetA64("0110111101xxxxxx010001xxxxxxxxxx", InstName.Sri_V,           InstEmit.Sri_V,           OpCodeSimdShImm.Create);
            SetA64("0>001110<<1xxxxx010101xxxxxxxxxx", InstName.Srshl_V,         InstEmit.Srshl_V,         OpCodeSimdReg.Create);
            SetA64("0101111101xxxxxx001001xxxxxxxxxx", InstName.Srshr_S,         InstEmit.Srshr_S,         OpCodeSimdShImm.Create);
            SetA64("0x00111100>>>xxx001001xxxxxxxxxx", InstName.Srshr_V,         InstEmit.Srshr_V,         OpCodeSimdShImm.Create);
            SetA64("0100111101xxxxxx001001xxxxxxxxxx", InstName.Srshr_V,         InstEmit.Srshr_V,         OpCodeSimdShImm.Create);
            SetA64("0101111101xxxxxx001101xxxxxxxxxx", InstName.Srsra_S,         InstEmit.Srsra_S,         OpCodeSimdShImm.Create);
            SetA64("0x00111100>>>xxx001101xxxxxxxxxx", InstName.Srsra_V,         InstEmit.Srsra_V,         OpCodeSimdShImm.Create);
            SetA64("0100111101xxxxxx001101xxxxxxxxxx", InstName.Srsra_V,         InstEmit.Srsra_V,         OpCodeSimdShImm.Create);
            SetA64("01011110111xxxxx010001xxxxxxxxxx", InstName.Sshl_S,          InstEmit.Sshl_S,          OpCodeSimdReg.Create);
            SetA64("0>001110<<1xxxxx010001xxxxxxxxxx", InstName.Sshl_V,          InstEmit.Sshl_V,          OpCodeSimdReg.Create);
            SetA64("0x00111100>>>xxx101001xxxxxxxxxx", InstName.Sshll_V,         InstEmit.Sshll_V,         OpCodeSimdShImm.Create);
            SetA64("0101111101xxxxxx000001xxxxxxxxxx", InstName.Sshr_S,          InstEmit.Sshr_S,          OpCodeSimdShImm.Create);
            SetA64("0x00111100>>>xxx000001xxxxxxxxxx", InstName.Sshr_V,          InstEmit.Sshr_V,          OpCodeSimdShImm.Create);
            SetA64("0100111101xxxxxx000001xxxxxxxxxx", InstName.Sshr_V,          InstEmit.Sshr_V,          OpCodeSimdShImm.Create);
            SetA64("0101111101xxxxxx000101xxxxxxxxxx", InstName.Ssra_S,          InstEmit.Ssra_S,          OpCodeSimdShImm.Create);
            SetA64("0x00111100>>>xxx000101xxxxxxxxxx", InstName.Ssra_V,          InstEmit.Ssra_V,          OpCodeSimdShImm.Create);
            SetA64("0100111101xxxxxx000101xxxxxxxxxx", InstName.Ssra_V,          InstEmit.Ssra_V,          OpCodeSimdShImm.Create);
            SetA64("0x001110<<1xxxxx001000xxxxxxxxxx", InstName.Ssubl_V,         InstEmit.Ssubl_V,         OpCodeSimdReg.Create);
            SetA64("0x001110<<1xxxxx001100xxxxxxxxxx", InstName.Ssubw_V,         InstEmit.Ssubw_V,         OpCodeSimdReg.Create);
            SetA64("0x00110000000000xxxxxxxxxxxxxxxx", InstName.St__Vms,         InstEmit.St__Vms,         OpCodeSimdMemMs.Create);
            SetA64("0x001100100xxxxxxxxxxxxxxxxxxxxx", InstName.St__Vms,         InstEmit.St__Vms,         OpCodeSimdMemMs.Create);
            SetA64("0x00110100x00000xxxxxxxxxxxxxxxx", InstName.St__Vss,         InstEmit.St__Vss,         OpCodeSimdMemSs.Create);
            SetA64("0x00110110xxxxxxxxxxxxxxxxxxxxxx", InstName.St__Vss,         InstEmit.St__Vss,         OpCodeSimdMemSs.Create);
            SetA64("<<10110xx0xxxxxxxxxxxxxxxxxxxxxx", InstName.Stp,             InstEmit.Stp,             OpCodeSimdMemPair.Create);
            SetA64("xx111100x00xxxxxxxxx00xxxxxxxxxx", InstName.Str,             InstEmit.Str,             OpCodeSimdMemImm.Create);
            SetA64("xx111100x00xxxxxxxxx01xxxxxxxxxx", InstName.Str,             InstEmit.Str,             OpCodeSimdMemImm.Create);
            SetA64("xx111100x00xxxxxxxxx11xxxxxxxxxx", InstName.Str,             InstEmit.Str,             OpCodeSimdMemImm.Create);
            SetA64("xx111101x0xxxxxxxxxxxxxxxxxxxxxx", InstName.Str,             InstEmit.Str,             OpCodeSimdMemImm.Create);
            SetA64("xx111100x01xxxxxx1xx10xxxxxxxxxx", InstName.Str,             InstEmit.Str,             OpCodeSimdMemReg.Create);
            SetA64("01111110111xxxxx100001xxxxxxxxxx", InstName.Sub_S,           InstEmit.Sub_S,           OpCodeSimdReg.Create);
            SetA64("0>101110<<1xxxxx100001xxxxxxxxxx", InstName.Sub_V,           InstEmit.Sub_V,           OpCodeSimdReg.Create);
            SetA64("0x001110<<1xxxxx011000xxxxxxxxxx", InstName.Subhn_V,         InstEmit.Subhn_V,         OpCodeSimdReg.Create);
            SetA64("01011110xx100000001110xxxxxxxxxx", InstName.Suqadd_S,        InstEmit.Suqadd_S,        OpCodeSimd.Create);
            SetA64("0>001110<<100000001110xxxxxxxxxx", InstName.Suqadd_V,        InstEmit.Suqadd_V,        OpCodeSimd.Create);
            SetA64("0x001110000xxxxx0xx000xxxxxxxxxx", InstName.Tbl_V,           InstEmit.Tbl_V,           OpCodeSimdTbl.Create);
            SetA64("0x001110000xxxxx0xx100xxxxxxxxxx", InstName.Tbx_V,           InstEmit.Tbx_V,           OpCodeSimdTbl.Create);
            SetA64("0>001110<<0xxxxx001010xxxxxxxxxx", InstName.Trn1_V,          InstEmit.Trn1_V,          OpCodeSimdReg.Create);
            SetA64("0>001110<<0xxxxx011010xxxxxxxxxx", InstName.Trn2_V,          InstEmit.Trn2_V,          OpCodeSimdReg.Create);
            SetA64("0x101110<<1xxxxx011111xxxxxxxxxx", InstName.Uaba_V,          InstEmit.Uaba_V,          OpCodeSimdReg.Create);
            SetA64("0x101110<<1xxxxx010100xxxxxxxxxx", InstName.Uabal_V,         InstEmit.Uabal_V,         OpCodeSimdReg.Create);
            SetA64("0x101110<<1xxxxx011101xxxxxxxxxx", InstName.Uabd_V,          InstEmit.Uabd_V,          OpCodeSimdReg.Create);
            SetA64("0x101110<<1xxxxx011100xxxxxxxxxx", InstName.Uabdl_V,         InstEmit.Uabdl_V,         OpCodeSimdReg.Create);
            SetA64("0x101110<<100000011010xxxxxxxxxx", InstName.Uadalp_V,        InstEmit.Uadalp_V,        OpCodeSimd.Create);
            SetA64("0x101110<<1xxxxx000000xxxxxxxxxx", InstName.Uaddl_V,         InstEmit.Uaddl_V,         OpCodeSimdReg.Create);
            SetA64("0x101110<<100000001010xxxxxxxxxx", InstName.Uaddlp_V,        InstEmit.Uaddlp_V,        OpCodeSimd.Create);
            SetA64("001011100x110000001110xxxxxxxxxx", InstName.Uaddlv_V,        InstEmit.Uaddlv_V,        OpCodeSimd.Create);
            SetA64("01101110<<110000001110xxxxxxxxxx", InstName.Uaddlv_V,        InstEmit.Uaddlv_V,        OpCodeSimd.Create);
            SetA64("0x101110<<1xxxxx000100xxxxxxxxxx", InstName.Uaddw_V,         InstEmit.Uaddw_V,         OpCodeSimdReg.Create);
            SetA64("x00111100x100011000000xxxxxxxxxx", InstName.Ucvtf_Gp,        InstEmit.Ucvtf_Gp,        OpCodeSimdCvt.Create);
            SetA64(">00111100x000011>xxxxxxxxxxxxxxx", InstName.Ucvtf_Gp_Fixed,  InstEmit.Ucvtf_Gp_Fixed,  OpCodeSimdCvt.Create);
            SetA64("011111100x100001110110xxxxxxxxxx", InstName.Ucvtf_S,         InstEmit.Ucvtf_S,         OpCodeSimd.Create);
            SetA64("011111110>>xxxxx111001xxxxxxxxxx", InstName.Ucvtf_S_Fixed,   InstEmit.Ucvtf_S_Fixed,   OpCodeSimdShImm.Create);
            SetA64("0>1011100<100001110110xxxxxxxxxx", InstName.Ucvtf_V,         InstEmit.Ucvtf_V,         OpCodeSimd.Create);
            SetA64("0x101111001xxxxx111001xxxxxxxxxx", InstName.Ucvtf_V_Fixed,   InstEmit.Ucvtf_V_Fixed,   OpCodeSimdShImm.Create);
            SetA64("0110111101xxxxxx111001xxxxxxxxxx", InstName.Ucvtf_V_Fixed,   InstEmit.Ucvtf_V_Fixed,   OpCodeSimdShImm.Create);
            SetA64("0x101110<<1xxxxx000001xxxxxxxxxx", InstName.Uhadd_V,         InstEmit.Uhadd_V,         OpCodeSimdReg.Create);
            SetA64("0x101110<<1xxxxx001001xxxxxxxxxx", InstName.Uhsub_V,         InstEmit.Uhsub_V,         OpCodeSimdReg.Create);
            SetA64("0x101110<<1xxxxx011001xxxxxxxxxx", InstName.Umax_V,          InstEmit.Umax_V,          OpCodeSimdReg.Create);
            SetA64("0x101110<<1xxxxx101001xxxxxxxxxx", InstName.Umaxp_V,         InstEmit.Umaxp_V,         OpCodeSimdReg.Create);
            SetA64("001011100x110000101010xxxxxxxxxx", InstName.Umaxv_V,         InstEmit.Umaxv_V,         OpCodeSimd.Create);
            SetA64("01101110<<110000101010xxxxxxxxxx", InstName.Umaxv_V,         InstEmit.Umaxv_V,         OpCodeSimd.Create);
            SetA64("0x101110<<1xxxxx011011xxxxxxxxxx", InstName.Umin_V,          InstEmit.Umin_V,          OpCodeSimdReg.Create);
            SetA64("0x101110<<1xxxxx101011xxxxxxxxxx", InstName.Uminp_V,         InstEmit.Uminp_V,         OpCodeSimdReg.Create);
            SetA64("001011100x110001101010xxxxxxxxxx", InstName.Uminv_V,         InstEmit.Uminv_V,         OpCodeSimd.Create);
            SetA64("01101110<<110001101010xxxxxxxxxx", InstName.Uminv_V,         InstEmit.Uminv_V,         OpCodeSimd.Create);
            SetA64("0x101110<<1xxxxx100000xxxxxxxxxx", InstName.Umlal_V,         InstEmit.Umlal_V,         OpCodeSimdReg.Create);
            SetA64("0x101111xxxxxxxx0010x0xxxxxxxxxx", InstName.Umlal_Ve,        InstEmit.Umlal_Ve,        OpCodeSimdRegElem.Create);
            SetA64("0x101110<<1xxxxx101000xxxxxxxxxx", InstName.Umlsl_V,         InstEmit.Umlsl_V,         OpCodeSimdReg.Create);
            SetA64("0x101111xxxxxxxx0110x0xxxxxxxxxx", InstName.Umlsl_Ve,        InstEmit.Umlsl_Ve,        OpCodeSimdRegElem.Create);
            SetA64("0x001110000xxxxx001111xxxxxxxxxx", InstName.Umov_S,          InstEmit.Umov_S,          OpCodeSimdIns.Create);
            SetA64("0x101110<<1xxxxx110000xxxxxxxxxx", InstName.Umull_V,         InstEmit.Umull_V,         OpCodeSimdReg.Create);
            SetA64("0x101111xxxxxxxx1010x0xxxxxxxxxx", InstName.Umull_Ve,        InstEmit.Umull_Ve,        OpCodeSimdRegElem.Create);
            SetA64("01111110xx1xxxxx000011xxxxxxxxxx", InstName.Uqadd_S,         InstEmit.Uqadd_S,         OpCodeSimdReg.Create);
            SetA64("0>101110<<1xxxxx000011xxxxxxxxxx", InstName.Uqadd_V,         InstEmit.Uqadd_V,         OpCodeSimdReg.Create);
            SetA64("0>101110<<1xxxxx010111xxxxxxxxxx", InstName.Uqrshl_V,        InstEmit.Uqrshl_V,        OpCodeSimdReg.Create);
            SetA64("0111111100>>>xxx100111xxxxxxxxxx", InstName.Uqrshrn_S,       InstEmit.Uqrshrn_S,       OpCodeSimdShImm.Create);
            SetA64("0x10111100>>>xxx100111xxxxxxxxxx", InstName.Uqrshrn_V,       InstEmit.Uqrshrn_V,       OpCodeSimdShImm.Create);
            SetA64("0>101110<<1xxxxx010011xxxxxxxxxx", InstName.Uqshl_V,         InstEmit.Uqshl_V,         OpCodeSimdReg.Create);
            SetA64("0111111100>>>xxx100101xxxxxxxxxx", InstName.Uqshrn_S,        InstEmit.Uqshrn_S,        OpCodeSimdShImm.Create);
            SetA64("0x10111100>>>xxx100101xxxxxxxxxx", InstName.Uqshrn_V,        InstEmit.Uqshrn_V,        OpCodeSimdShImm.Create);
            SetA64("01111110xx1xxxxx001011xxxxxxxxxx", InstName.Uqsub_S,         InstEmit.Uqsub_S,         OpCodeSimdReg.Create);
            SetA64("0>101110<<1xxxxx001011xxxxxxxxxx", InstName.Uqsub_V,         InstEmit.Uqsub_V,         OpCodeSimdReg.Create);
            SetA64("01111110<<100001010010xxxxxxxxxx", InstName.Uqxtn_S,         InstEmit.Uqxtn_S,         OpCodeSimd.Create);
            SetA64("0x101110<<100001010010xxxxxxxxxx", InstName.Uqxtn_V,         InstEmit.Uqxtn_V,         OpCodeSimd.Create);
            SetA64("0x101110<<1xxxxx000101xxxxxxxxxx", InstName.Urhadd_V,        InstEmit.Urhadd_V,        OpCodeSimdReg.Create);
            SetA64("0>101110<<1xxxxx010101xxxxxxxxxx", InstName.Urshl_V,         InstEmit.Urshl_V,         OpCodeSimdReg.Create);
            SetA64("0111111101xxxxxx001001xxxxxxxxxx", InstName.Urshr_S,         InstEmit.Urshr_S,         OpCodeSimdShImm.Create);
            SetA64("0x10111100>>>xxx001001xxxxxxxxxx", InstName.Urshr_V,         InstEmit.Urshr_V,         OpCodeSimdShImm.Create);
            SetA64("0110111101xxxxxx001001xxxxxxxxxx", InstName.Urshr_V,         InstEmit.Urshr_V,         OpCodeSimdShImm.Create);
            SetA64("0111111101xxxxxx001101xxxxxxxxxx", InstName.Ursra_S,         InstEmit.Ursra_S,         OpCodeSimdShImm.Create);
            SetA64("0x10111100>>>xxx001101xxxxxxxxxx", InstName.Ursra_V,         InstEmit.Ursra_V,         OpCodeSimdShImm.Create);
            SetA64("0110111101xxxxxx001101xxxxxxxxxx", InstName.Ursra_V,         InstEmit.Ursra_V,         OpCodeSimdShImm.Create);
            SetA64("01111110111xxxxx010001xxxxxxxxxx", InstName.Ushl_S,          InstEmit.Ushl_S,          OpCodeSimdReg.Create);
            SetA64("0>101110<<1xxxxx010001xxxxxxxxxx", InstName.Ushl_V,          InstEmit.Ushl_V,          OpCodeSimdReg.Create);
            SetA64("0x10111100>>>xxx101001xxxxxxxxxx", InstName.Ushll_V,         InstEmit.Ushll_V,         OpCodeSimdShImm.Create);
            SetA64("0111111101xxxxxx000001xxxxxxxxxx", InstName.Ushr_S,          InstEmit.Ushr_S,          OpCodeSimdShImm.Create);
            SetA64("0x10111100>>>xxx000001xxxxxxxxxx", InstName.Ushr_V,          InstEmit.Ushr_V,          OpCodeSimdShImm.Create);
            SetA64("0110111101xxxxxx000001xxxxxxxxxx", InstName.Ushr_V,          InstEmit.Ushr_V,          OpCodeSimdShImm.Create);
            SetA64("01111110xx100000001110xxxxxxxxxx", InstName.Usqadd_S,        InstEmit.Usqadd_S,        OpCodeSimd.Create);
            SetA64("0>101110<<100000001110xxxxxxxxxx", InstName.Usqadd_V,        InstEmit.Usqadd_V,        OpCodeSimd.Create);
            SetA64("0111111101xxxxxx000101xxxxxxxxxx", InstName.Usra_S,          InstEmit.Usra_S,          OpCodeSimdShImm.Create);
            SetA64("0x10111100>>>xxx000101xxxxxxxxxx", InstName.Usra_V,          InstEmit.Usra_V,          OpCodeSimdShImm.Create);
            SetA64("0110111101xxxxxx000101xxxxxxxxxx", InstName.Usra_V,          InstEmit.Usra_V,          OpCodeSimdShImm.Create);
            SetA64("0x101110<<1xxxxx001000xxxxxxxxxx", InstName.Usubl_V,         InstEmit.Usubl_V,         OpCodeSimdReg.Create);
            SetA64("0x101110<<1xxxxx001100xxxxxxxxxx", InstName.Usubw_V,         InstEmit.Usubw_V,         OpCodeSimdReg.Create);
            SetA64("0>001110<<0xxxxx000110xxxxxxxxxx", InstName.Uzp1_V,          InstEmit.Uzp1_V,          OpCodeSimdReg.Create);
            SetA64("0>001110<<0xxxxx010110xxxxxxxxxx", InstName.Uzp2_V,          InstEmit.Uzp2_V,          OpCodeSimdReg.Create);
            SetA64("0x001110<<100001001010xxxxxxxxxx", InstName.Xtn_V,           InstEmit.Xtn_V,           OpCodeSimd.Create);
            SetA64("0>001110<<0xxxxx001110xxxxxxxxxx", InstName.Zip1_V,          InstEmit.Zip1_V,          OpCodeSimdReg.Create);
            SetA64("0>001110<<0xxxxx011110xxxxxxxxxx", InstName.Zip2_V,          InstEmit.Zip2_V,          OpCodeSimdReg.Create);
            #endregion

            #region "OpCode Table (AArch32, A32)"
            // Base
            SetA32("<<<<0010101xxxxxxxxxxxxxxxxxxxxx", InstName.Adc,     InstEmit32.Adc,     OpCode32AluImm.Create);
            SetA32("<<<<0000101xxxxxxxxxxxxxxxx0xxxx", InstName.Adc,     InstEmit32.Adc,     OpCode32AluRsImm.Create);
            SetA32("<<<<0000101xxxxxxxxxxxxx0xx1xxxx", InstName.Adc,     InstEmit32.Adc,     OpCode32AluRsReg.Create);
            SetA32("<<<<0010100xxxxxxxxxxxxxxxxxxxxx", InstName.Add,     InstEmit32.Add,     OpCode32AluImm.Create);
            SetA32("<<<<0000100xxxxxxxxxxxxxxxx0xxxx", InstName.Add,     InstEmit32.Add,     OpCode32AluRsImm.Create);
            SetA32("<<<<0000100xxxxxxxxxxxxx0xx1xxxx", InstName.Add,     InstEmit32.Add,     OpCode32AluRsReg.Create);
            SetA32("<<<<0010000xxxxxxxxxxxxxxxxxxxxx", InstName.And,     InstEmit32.And,     OpCode32AluImm.Create);
            SetA32("<<<<0000000xxxxxxxxxxxxxxxx0xxxx", InstName.And,     InstEmit32.And,     OpCode32AluRsImm.Create);
            SetA32("<<<<0000000xxxxxxxxxxxxx0xx1xxxx", InstName.And,     InstEmit32.And,     OpCode32AluRsReg.Create);
            SetA32("<<<<1010xxxxxxxxxxxxxxxxxxxxxxxx", InstName.B,       InstEmit32.B,       OpCode32BImm.Create);
            SetA32("<<<<0111110xxxxxxxxxxxxxx0011111", InstName.Bfc,     InstEmit32.Bfc,     OpCode32AluBf.Create);
            SetA32("<<<<0111110xxxxxxxxxxxxxx001xxxx", InstName.Bfi,     InstEmit32.Bfi,     OpCode32AluBf.Create);
            SetA32("<<<<0011110xxxxxxxxxxxxxxxxxxxxx", InstName.Bic,     InstEmit32.Bic,     OpCode32AluImm.Create);
            SetA32("<<<<0001110xxxxxxxxxxxxxxxx0xxxx", InstName.Bic,     InstEmit32.Bic,     OpCode32AluRsImm.Create);
            SetA32("<<<<0001110xxxxxxxxxxxxx0xx1xxxx", InstName.Bic,     InstEmit32.Bic,     OpCode32AluRsReg.Create);
            SetA32("<<<<1011xxxxxxxxxxxxxxxxxxxxxxxx", InstName.Bl,      InstEmit32.Bl,      OpCode32BImm.Create);
            SetA32("1111101xxxxxxxxxxxxxxxxxxxxxxxxx", InstName.Blx,     InstEmit32.Blx,     OpCode32BImm.Create);
            SetA32("<<<<000100101111111111110011xxxx", InstName.Blx,     InstEmit32.Blxr,    OpCode32BReg.Create);
            SetA32("<<<<000100101111111111110001xxxx", InstName.Bx,      InstEmit32.Bx,      OpCode32BReg.Create);
            SetA32("11110101011111111111000000011111", InstName.Clrex,   InstEmit32.Clrex,   OpCode32.Create);
            SetA32("<<<<000101101111xxxx11110001xxxx", InstName.Clz,     InstEmit32.Clz,     OpCode32AluReg.Create);
            SetA32("<<<<00110111xxxx0000xxxxxxxxxxxx", InstName.Cmn,     InstEmit32.Cmn,     OpCode32AluImm.Create);
            SetA32("<<<<00010111xxxx0000xxxxxxx0xxxx", InstName.Cmn,     InstEmit32.Cmn,     OpCode32AluRsImm.Create);
            SetA32("<<<<00010111xxxx0000xxxx0xx1xxxx", InstName.Cmn,     InstEmit32.Cmn,     OpCode32AluRsReg.Create);
            SetA32("<<<<00110101xxxx0000xxxxxxxxxxxx", InstName.Cmp,     InstEmit32.Cmp,     OpCode32AluImm.Create);
            SetA32("<<<<00010101xxxx0000xxxxxxx0xxxx", InstName.Cmp,     InstEmit32.Cmp,     OpCode32AluRsImm.Create);
            SetA32("<<<<00010101xxxx0000xxxx0xx1xxxx", InstName.Cmp,     InstEmit32.Cmp,     OpCode32AluRsReg.Create);
            SetA32("<<<<00010000xxxxxxxx00000100xxxx", InstName.Crc32b,  InstEmit32.Crc32b,  OpCode32AluReg.Create);
            SetA32("<<<<00010000xxxxxxxx00100100xxxx", InstName.Crc32cb, InstEmit32.Crc32cb, OpCode32AluReg.Create);
            SetA32("<<<<00010010xxxxxxxx00100100xxxx", InstName.Crc32ch, InstEmit32.Crc32ch, OpCode32AluReg.Create);
            SetA32("<<<<00010100xxxxxxxx00100100xxxx", InstName.Crc32cw, InstEmit32.Crc32cw, OpCode32AluReg.Create);
            SetA32("<<<<00010010xxxxxxxx00000100xxxx", InstName.Crc32h,  InstEmit32.Crc32h,  OpCode32AluReg.Create);
            SetA32("<<<<00010100xxxxxxxx00000100xxxx", InstName.Crc32w,  InstEmit32.Crc32w,  OpCode32AluReg.Create);
            SetA32("<<<<0011001000001111000000010100", InstName.Csdb,    InstEmit32.Csdb,    OpCode32.Create);
            SetA32("1111010101111111111100000101xxxx", InstName.Dmb,     InstEmit32.Dmb,     OpCode32.Create);
            SetA32("1111010101111111111100000100xxxx", InstName.Dsb,     InstEmit32.Dsb,     OpCode32.Create);
            SetA32("<<<<0010001xxxxxxxxxxxxxxxxxxxxx", InstName.Eor,     InstEmit32.Eor,     OpCode32AluImm.Create);
            SetA32("<<<<0000001xxxxxxxxxxxxxxxx0xxxx", InstName.Eor,     InstEmit32.Eor,     OpCode32AluRsImm.Create);
            SetA32("<<<<0000001xxxxxxxxxxxxx0xx1xxxx", InstName.Eor,     InstEmit32.Eor,     OpCode32AluRsReg.Create);
            SetA32("<<<<0011001000001111000000010000", InstName.Esb,     InstEmit32.Nop,     OpCode32.Create); // Error Synchronization Barrier (FEAT_RAS)
            SetA32("<<<<001100100000111100000000011x", InstName.Hint,    InstEmit32.Nop,     OpCode32.Create); // Reserved Hint
            SetA32("<<<<0011001000001111000000001xxx", InstName.Hint,    InstEmit32.Nop,     OpCode32.Create); // Reserved Hint
            SetA32("<<<<0011001000001111000000010001", InstName.Hint,    InstEmit32.Nop,     OpCode32.Create); // Reserved Hint
            SetA32("<<<<0011001000001111000000010011", InstName.Hint,    InstEmit32.Nop,     OpCode32.Create); // Reserved Hint
            SetA32("<<<<0011001000001111000000010101", InstName.Hint,    InstEmit32.Nop,     OpCode32.Create); // Reserved Hint
            SetA32("<<<<001100100000111100000001011x", InstName.Hint,    InstEmit32.Nop,     OpCode32.Create); // Reserved Hint
            SetA32("<<<<0011001000001111000000011xxx", InstName.Hint,    InstEmit32.Nop,     OpCode32.Create); // Reserved Hint
            SetA32("<<<<00110010000011110000001xxxxx", InstName.Hint,    InstEmit32.Nop,     OpCode32.Create); // Reserved Hint
            SetA32("<<<<0011001000001111000001xxxxxx", InstName.Hint,    InstEmit32.Nop,     OpCode32.Create); // Reserved Hint
            SetA32("<<<<001100100000111100001xxxxxxx", InstName.Hint,    InstEmit32.Nop,     OpCode32.Create); // Reserved Hint
            SetA32("1111010101111111111100000110xxxx", InstName.Isb,     InstEmit32.Nop,     OpCode32.Create);
            SetA32("<<<<00011001xxxxxxxx110010011111", InstName.Lda,     InstEmit32.Lda,     OpCode32MemLdEx.Create);
            SetA32("<<<<00011101xxxxxxxx110010011111", InstName.Ldab,    InstEmit32.Ldab,    OpCode32MemLdEx.Create);
            SetA32("<<<<00011001xxxxxxxx111010011111", InstName.Ldaex,   InstEmit32.Ldaex,   OpCode32MemLdEx.Create);
            SetA32("<<<<00011101xxxxxxxx111010011111", InstName.Ldaexb,  InstEmit32.Ldaexb,  OpCode32MemLdEx.Create);
            SetA32("<<<<00011011xxxxxxxx111010011111", InstName.Ldaexd,  InstEmit32.Ldaexd,  OpCode32MemLdEx.Create);
            SetA32("<<<<00011111xxxxxxxx111010011111", InstName.Ldaexh,  InstEmit32.Ldaexh,  OpCode32MemLdEx.Create);
            SetA32("<<<<00011111xxxxxxxx110010011111", InstName.Ldah,    InstEmit32.Ldah,    OpCode32MemLdEx.Create);
            SetA32("<<<<100xx0x1xxxxxxxxxxxxxxxxxxxx", InstName.Ldm,     InstEmit32.Ldm,     OpCode32MemMult.Create);
            SetA32("<<<<010xx0x1xxxxxxxxxxxxxxxxxxxx", InstName.Ldr,     InstEmit32.Ldr,     OpCode32MemImm.Create);
            SetA32("<<<<011xx0x1xxxxxxxxxxxxxxx0xxxx", InstName.Ldr,     InstEmit32.Ldr,     OpCode32MemRsImm.Create);
            SetA32("<<<<010xx1x1xxxxxxxxxxxxxxxxxxxx", InstName.Ldrb,    InstEmit32.Ldrb,    OpCode32MemImm.Create);
            SetA32("<<<<011xx1x1xxxxxxxxxxxxxxx0xxxx", InstName.Ldrb,    InstEmit32.Ldrb,    OpCode32MemRsImm.Create);
            SetA32("<<<<000xx1x0xxxxxxxxxxxx1101xxxx", InstName.Ldrd,    InstEmit32.Ldrd,    OpCode32MemImm8.Create);
            SetA32("<<<<000xx0x0xxxxxxxx00001101xxxx", InstName.Ldrd,    InstEmit32.Ldrd,    OpCode32MemReg.Create);
            SetA32("<<<<00011001xxxxxxxx111110011111", InstName.Ldrex,   InstEmit32.Ldrex,   OpCode32MemLdEx.Create);
            SetA32("<<<<00011101xxxxxxxx111110011111", InstName.Ldrexb,  InstEmit32.Ldrexb,  OpCode32MemLdEx.Create);
            SetA32("<<<<00011011xxxxxxxx111110011111", InstName.Ldrexd,  InstEmit32.Ldrexd,  OpCode32MemLdEx.Create);
            SetA32("<<<<00011111xxxxxxxx111110011111", InstName.Ldrexh,  InstEmit32.Ldrexh,  OpCode32MemLdEx.Create);
            SetA32("<<<<000xx1x1xxxxxxxxxxxx1011xxxx", InstName.Ldrh,    InstEmit32.Ldrh,    OpCode32MemImm8.Create);
            SetA32("<<<<000xx0x1xxxxxxxx00001011xxxx", InstName.Ldrh,    InstEmit32.Ldrh,    OpCode32MemReg.Create);
            SetA32("<<<<000xx1x1xxxxxxxxxxxx1101xxxx", InstName.Ldrsb,   InstEmit32.Ldrsb,   OpCode32MemImm8.Create);
            SetA32("<<<<000xx0x1xxxxxxxx00001101xxxx", InstName.Ldrsb,   InstEmit32.Ldrsb,   OpCode32MemReg.Create);
            SetA32("<<<<000xx1x1xxxxxxxxxxxx1111xxxx", InstName.Ldrsh,   InstEmit32.Ldrsh,   OpCode32MemImm8.Create);
            SetA32("<<<<000xx0x1xxxxxxxx00001111xxxx", InstName.Ldrsh,   InstEmit32.Ldrsh,   OpCode32MemReg.Create);
            SetA32("<<<<1110xxx0xxxxxxxx111xxxx1xxxx", InstName.Mcr,     InstEmit32.Mcr,     OpCode32System.Create);
            SetA32("<<<<0000001xxxxxxxxxxxxx1001xxxx", InstName.Mla,     InstEmit32.Mla,     OpCode32AluMla.Create);
            SetA32("<<<<00000110xxxxxxxxxxxx1001xxxx", InstName.Mls,     InstEmit32.Mls,     OpCode32AluMla.Create);
            SetA32("<<<<0011101x0000xxxxxxxxxxxxxxxx", InstName.Mov,     InstEmit32.Mov,     OpCode32AluImm.Create);
            SetA32("<<<<0001101x0000xxxxxxxxxxx0xxxx", InstName.Mov,     InstEmit32.Mov,     OpCode32AluRsImm.Create);
            SetA32("<<<<0001101x0000xxxxxxxx0xx1xxxx", InstName.Mov,     InstEmit32.Mov,     OpCode32AluRsReg.Create);
            SetA32("<<<<00110000xxxxxxxxxxxxxxxxxxxx", InstName.Mov,     InstEmit32.Mov,     OpCode32AluImm16.Create);
            SetA32("<<<<00110100xxxxxxxxxxxxxxxxxxxx", InstName.Movt,    InstEmit32.Movt,    OpCode32AluImm16.Create);
            SetA32("<<<<1110xxx1xxxxxxxx111xxxx1xxxx", InstName.Mrc,     InstEmit32.Mrc,     OpCode32System.Create);
            SetA32("<<<<11000101xxxxxxxx111xxxxxxxxx", InstName.Mrrc,    InstEmit32.Mrrc,    OpCode32System.Create);
            SetA32("<<<<00010x001111xxxx000000000000", InstName.Mrs,     InstEmit32.Mrs,     OpCode32Mrs.Create);
            SetA32("<<<<00010x10xxxx111100000000xxxx", InstName.Msr,     InstEmit32.Msr,     OpCode32MsrReg.Create);
            SetA32("<<<<0000000xxxxx0000xxxx1001xxxx", InstName.Mul,     InstEmit32.Mul,     OpCode32AluMla.Create);
            SetA32("<<<<0011111x0000xxxxxxxxxxxxxxxx", InstName.Mvn,     InstEmit32.Mvn,     OpCode32AluImm.Create);
            SetA32("<<<<0001111x0000xxxxxxxxxxx0xxxx", InstName.Mvn,     InstEmit32.Mvn,     OpCode32AluRsImm.Create);
            SetA32("<<<<0001111x0000xxxxxxxx0xx1xxxx", InstName.Mvn,     InstEmit32.Mvn,     OpCode32AluRsReg.Create);
            SetA32("<<<<0011001000001111000000000000", InstName.Nop,     InstEmit32.Nop,     OpCode32.Create);
            SetA32("<<<<0011100xxxxxxxxxxxxxxxxxxxxx", InstName.Orr,     InstEmit32.Orr,     OpCode32AluImm.Create);
            SetA32("<<<<0001100xxxxxxxxxxxxxxxx0xxxx", InstName.Orr,     InstEmit32.Orr,     OpCode32AluRsImm.Create);
            SetA32("<<<<0001100xxxxxxxxxxxxx0xx1xxxx", InstName.Orr,     InstEmit32.Orr,     OpCode32AluRsReg.Create);
            SetA32("<<<<01101000xxxxxxxxxxxxxx01xxxx", InstName.Pkh,     InstEmit32.Pkh,     OpCode32AluRsImm.Create);
            SetA32("11110101xx01xxxx1111xxxxxxxxxxxx", InstName.Pld,     InstEmit32.Nop,     OpCode32.Create);
            SetA32("11110111xx01xxxx1111xxxxxxx0xxxx", InstName.Pld,     InstEmit32.Nop,     OpCode32.Create);
            SetA32("<<<<01100010xxxxxxxx11110001xxxx", InstName.Qadd16,  InstEmit32.Qadd16,  OpCode32AluReg.Create);
            SetA32("<<<<011011111111xxxx11110011xxxx", InstName.Rbit,    InstEmit32.Rbit,    OpCode32AluReg.Create);
            SetA32("<<<<011010111111xxxx11110011xxxx", InstName.Rev,     InstEmit32.Rev,     OpCode32AluReg.Create);
            SetA32("<<<<011010111111xxxx11111011xxxx", InstName.Rev16,   InstEmit32.Rev16,   OpCode32AluReg.Create);
            SetA32("<<<<011011111111xxxx11111011xxxx", InstName.Revsh,   InstEmit32.Revsh,   OpCode32AluReg.Create);
            SetA32("<<<<0010011xxxxxxxxxxxxxxxxxxxxx", InstName.Rsb,     InstEmit32.Rsb,     OpCode32AluImm.Create);
            SetA32("<<<<0000011xxxxxxxxxxxxxxxx0xxxx", InstName.Rsb,     InstEmit32.Rsb,     OpCode32AluRsImm.Create);
            SetA32("<<<<0000011xxxxxxxxxxxxx0xx1xxxx", InstName.Rsb,     InstEmit32.Rsb,     OpCode32AluRsReg.Create);
            SetA32("<<<<0010111xxxxxxxxxxxxxxxxxxxxx", InstName.Rsc,     InstEmit32.Rsc,     OpCode32AluImm.Create);
            SetA32("<<<<0000111xxxxxxxxxxxxxxxx0xxxx", InstName.Rsc,     InstEmit32.Rsc,     OpCode32AluRsImm.Create);
            SetA32("<<<<0000111xxxxxxxxxxxxx0xx1xxxx", InstName.Rsc,     InstEmit32.Rsc,     OpCode32AluRsReg.Create);
            SetA32("<<<<01100001xxxxxxxx11111001xxxx", InstName.Sadd8,   InstEmit32.Sadd8,   OpCode32AluReg.Create);
            SetA32("<<<<0010110xxxxxxxxxxxxxxxxxxxxx", InstName.Sbc,     InstEmit32.Sbc,     OpCode32AluImm.Create);
            SetA32("<<<<0000110xxxxxxxxxxxxxxxx0xxxx", InstName.Sbc,     InstEmit32.Sbc,     OpCode32AluRsImm.Create);
            SetA32("<<<<0000110xxxxxxxxxxxxx0xx1xxxx", InstName.Sbc,     InstEmit32.Sbc,     OpCode32AluRsReg.Create);
            SetA32("<<<<0111101xxxxxxxxxxxxxx101xxxx", InstName.Sbfx,    InstEmit32.Sbfx,    OpCode32AluBf.Create);
            SetA32("<<<<01110001xxxx1111xxxx0001xxxx", InstName.Sdiv,    InstEmit32.Sdiv,    OpCode32AluMla.Create);
            SetA32("<<<<01101000xxxxxxxx11111011xxxx", InstName.Sel,     InstEmit32.Sel,     OpCode32AluReg.Create);
            SetA32("<<<<0011001000001111000000000100", InstName.Sev,     InstEmit32.Nop,     OpCode32.Create);
            SetA32("<<<<0011001000001111000000000101", InstName.Sevl,    InstEmit32.Nop,     OpCode32.Create);
            SetA32("<<<<01100011xxxxxxxx11111001xxxx", InstName.Shadd8,  InstEmit32.Shadd8,  OpCode32AluReg.Create);
            SetA32("<<<<01100011xxxxxxxx11111111xxxx", InstName.Shsub8,  InstEmit32.Shsub8,  OpCode32AluReg.Create);
            SetA32("<<<<00010000xxxxxxxxxxxx1xx0xxxx", InstName.Smla__,  InstEmit32.Smla__,  OpCode32AluMla.Create);
            SetA32("<<<<0000111xxxxxxxxxxxxx1001xxxx", InstName.Smlal,   InstEmit32.Smlal,   OpCode32AluUmull.Create);
            SetA32("<<<<00010100xxxxxxxxxxxx1xx0xxxx", InstName.Smlal__, InstEmit32.Smlal__, OpCode32AluUmull.Create);
            SetA32("<<<<00010010xxxxxxxxxxxx1x00xxxx", InstName.Smlaw_,  InstEmit32.Smlaw_,  OpCode32AluMla.Create);
            SetA32("<<<<01110101xxxxxxxxxxxx00x1xxxx", InstName.Smmla,   InstEmit32.Smmla,   OpCode32AluMla.Create);
            SetA32("<<<<01110101xxxxxxxxxxxx11x1xxxx", InstName.Smmls,   InstEmit32.Smmls,   OpCode32AluMla.Create);
            SetA32("<<<<00010110xxxxxxxxxxxx1xx0xxxx", InstName.Smul__,  InstEmit32.Smul__,  OpCode32AluMla.Create);
            SetA32("<<<<0000110xxxxxxxxxxxxx1001xxxx", InstName.Smull,   InstEmit32.Smull,   OpCode32AluUmull.Create);
            SetA32("<<<<00010010xxxx0000xxxx1x10xxxx", InstName.Smulw_,  InstEmit32.Smulw_,  OpCode32AluMla.Create);
            SetA32("<<<<0110101xxxxxxxxxxxxxxx01xxxx", InstName.Ssat,    InstEmit32.Ssat,    OpCode32Sat.Create);
            SetA32("<<<<01101010xxxxxxxx11110011xxxx", InstName.Ssat16,  InstEmit32.Ssat16,  OpCode32Sat16.Create);
            SetA32("<<<<01100001xxxxxxxx11111111xxxx", InstName.Ssub8,   InstEmit32.Ssub8,   OpCode32AluReg.Create);
            SetA32("<<<<00011000xxxx111111001001xxxx", InstName.Stl,     InstEmit32.Stl,     OpCode32MemStEx.Create);
            SetA32("<<<<00011100xxxx111111001001xxxx", InstName.Stlb,    InstEmit32.Stlb,    OpCode32MemStEx.Create);
            SetA32("<<<<00011000xxxxxxxx11101001xxxx", InstName.Stlex,   InstEmit32.Stlex,   OpCode32MemStEx.Create);
            SetA32("<<<<00011100xxxxxxxx11101001xxxx", InstName.Stlexb,  InstEmit32.Stlexb,  OpCode32MemStEx.Create);
            SetA32("<<<<00011010xxxxxxxx11101001xxxx", InstName.Stlexd,  InstEmit32.Stlexd,  OpCode32MemStEx.Create);
            SetA32("<<<<00011110xxxxxxxx11101001xxxx", InstName.Stlexh,  InstEmit32.Stlexh,  OpCode32MemStEx.Create);
            SetA32("<<<<00011110xxxx111111001001xxxx", InstName.Stlh,    InstEmit32.Stlh,    OpCode32MemStEx.Create);
            SetA32("<<<<100xx0x0xxxxxxxxxxxxxxxxxxxx", InstName.Stm,     InstEmit32.Stm,     OpCode32MemMult.Create);
            SetA32("<<<<010xx0x0xxxxxxxxxxxxxxxxxxxx", InstName.Str,     InstEmit32.Str,     OpCode32MemImm.Create);
            SetA32("<<<<011xx0x0xxxxxxxxxxxxxxx0xxxx", InstName.Str,     InstEmit32.Str,     OpCode32MemRsImm.Create);
            SetA32("<<<<010xx1x0xxxxxxxxxxxxxxxxxxxx", InstName.Strb,    InstEmit32.Strb,    OpCode32MemImm.Create);
            SetA32("<<<<011xx1x0xxxxxxxxxxxxxxx0xxxx", InstName.Strb,    InstEmit32.Strb,    OpCode32MemRsImm.Create);
            SetA32("<<<<000xx1x0xxxxxxxxxxxx1111xxxx", InstName.Strd,    InstEmit32.Strd,    OpCode32MemImm8.Create);
            SetA32("<<<<000xx0x0xxxxxxxx00001111xxxx", InstName.Strd,    InstEmit32.Strd,    OpCode32MemReg.Create);
            SetA32("<<<<00011000xxxxxxxx11111001xxxx", InstName.Strex,   InstEmit32.Strex,   OpCode32MemStEx.Create);
            SetA32("<<<<00011100xxxxxxxx11111001xxxx", InstName.Strexb,  InstEmit32.Strexb,  OpCode32MemStEx.Create);
            SetA32("<<<<00011010xxxxxxxx11111001xxxx", InstName.Strexd,  InstEmit32.Strexd,  OpCode32MemStEx.Create);
            SetA32("<<<<00011110xxxxxxxx11111001xxxx", InstName.Strexh,  InstEmit32.Strexh,  OpCode32MemStEx.Create);
            SetA32("<<<<000xx1x0xxxxxxxxxxxx1011xxxx", InstName.Strh,    InstEmit32.Strh,    OpCode32MemImm8.Create);
            SetA32("<<<<000xx0x0xxxxxxxx00001011xxxx", InstName.Strh,    InstEmit32.Strh,    OpCode32MemReg.Create);
            SetA32("<<<<0010010xxxxxxxxxxxxxxxxxxxxx", InstName.Sub,     InstEmit32.Sub,     OpCode32AluImm.Create);
            SetA32("<<<<0000010xxxxxxxxxxxxxxxx0xxxx", InstName.Sub,     InstEmit32.Sub,     OpCode32AluRsImm.Create);
            SetA32("<<<<0000010xxxxxxxxxxxxx0xx1xxxx", InstName.Sub,     InstEmit32.Sub,     OpCode32AluRsReg.Create);
            SetA32("<<<<1111xxxxxxxxxxxxxxxxxxxxxxxx", InstName.Svc,     InstEmit32.Svc,     OpCode32Exception.Create);
            SetA32("<<<<01101010xxxxxxxxxx000111xxxx", InstName.Sxtb,    InstEmit32.Sxtb,    OpCode32AluUx.Create);
            SetA32("<<<<01101000xxxxxxxxxx000111xxxx", InstName.Sxtb16,  InstEmit32.Sxtb16,  OpCode32AluUx.Create);
            SetA32("<<<<01101011xxxxxxxxxx000111xxxx", InstName.Sxth,    InstEmit32.Sxth,    OpCode32AluUx.Create);
            SetA32("<<<<00110011xxxx0000xxxxxxxxxxxx", InstName.Teq,     InstEmit32.Teq,     OpCode32AluImm.Create);
            SetA32("<<<<00010011xxxx0000xxxxxxx0xxxx", InstName.Teq,     InstEmit32.Teq,     OpCode32AluRsImm.Create);
            SetA32("<<<<00010011xxxx0000xxxx0xx1xxxx", InstName.Teq,     InstEmit32.Teq,     OpCode32AluRsReg.Create);
            SetA32("<<<<0111111111111101111011111110", InstName.Trap,    InstEmit32.Trap,    OpCode32Exception.Create);
            SetA32("<<<<0011001000001111000000010010", InstName.Tsb,     InstEmit32.Nop,     OpCode32.Create); // Trace Synchronization Barrier (FEAT_TRF)
            SetA32("<<<<00110001xxxx0000xxxxxxxxxxxx", InstName.Tst,     InstEmit32.Tst,     OpCode32AluImm.Create);
            SetA32("<<<<00010001xxxx0000xxxxxxx0xxxx", InstName.Tst,     InstEmit32.Tst,     OpCode32AluRsImm.Create);
            SetA32("<<<<00010001xxxx0000xxxx0xx1xxxx", InstName.Tst,     InstEmit32.Tst,     OpCode32AluRsReg.Create);
            SetA32("<<<<01100101xxxxxxxx11111001xxxx", InstName.Uadd8,   InstEmit32.Uadd8,   OpCode32AluReg.Create);
            SetA32("<<<<0111111xxxxxxxxxxxxxx101xxxx", InstName.Ubfx,    InstEmit32.Ubfx,    OpCode32AluBf.Create);
            SetA32("<<<<01110011xxxx1111xxxx0001xxxx", InstName.Udiv,    InstEmit32.Udiv,    OpCode32AluMla.Create);
            SetA32("<<<<01100111xxxxxxxx11111001xxxx", InstName.Uhadd8,  InstEmit32.Uhadd8,  OpCode32AluReg.Create);
            SetA32("<<<<01100111xxxxxxxx11111111xxxx", InstName.Uhsub8,  InstEmit32.Uhsub8,  OpCode32AluReg.Create);
            SetA32("<<<<00000100xxxxxxxxxxxx1001xxxx", InstName.Umaal,   InstEmit32.Umaal,   OpCode32AluUmull.Create);
            SetA32("<<<<0000101xxxxxxxxxxxxx1001xxxx", InstName.Umlal,   InstEmit32.Umlal,   OpCode32AluUmull.Create);
            SetA32("<<<<0000100xxxxxxxxxxxxx1001xxxx", InstName.Umull,   InstEmit32.Umull,   OpCode32AluUmull.Create);
            SetA32("<<<<01100110xxxxxxxx11110001xxxx", InstName.Uqadd16, InstEmit32.Uqadd16, OpCode32AluReg.Create);
            SetA32("<<<<01100110xxxxxxxx11111001xxxx", InstName.Uqadd8,  InstEmit32.Uqadd8,  OpCode32AluReg.Create);
            SetA32("<<<<01100110xxxxxxxx11110111xxxx", InstName.Uqsub16, InstEmit32.Uqsub16, OpCode32AluReg.Create);
            SetA32("<<<<01100110xxxxxxxx11111111xxxx", InstName.Uqsub8,  InstEmit32.Uqsub8,  OpCode32AluReg.Create);
            SetA32("<<<<0110111xxxxxxxxxxxxxxx01xxxx", InstName.Usat,    InstEmit32.Usat,    OpCode32Sat.Create);
            SetA32("<<<<01101110xxxxxxxx11110011xxxx", InstName.Usat16,  InstEmit32.Usat16,  OpCode32Sat16.Create);
            SetA32("<<<<01100101xxxxxxxx11111111xxxx", InstName.Usub8,   InstEmit32.Usub8,   OpCode32AluReg.Create);
            SetA32("<<<<01101110xxxxxxxxxx000111xxxx", InstName.Uxtb,    InstEmit32.Uxtb,    OpCode32AluUx.Create);
            SetA32("<<<<01101100xxxxxxxxxx000111xxxx", InstName.Uxtb16,  InstEmit32.Uxtb16,  OpCode32AluUx.Create);
            SetA32("<<<<01101111xxxxxxxxxx000111xxxx", InstName.Uxth,    InstEmit32.Uxth,    OpCode32AluUx.Create);
            SetA32("<<<<0011001000001111000000000010", InstName.Wfe,     InstEmit32.Nop,     OpCode32.Create);
            SetA32("<<<<0011001000001111000000000011", InstName.Wfi,     InstEmit32.Nop,     OpCode32.Create);
            SetA32("<<<<0011001000001111000000000001", InstName.Yield,   InstEmit32.Nop,     OpCode32.Create);

            // VFP
            SetVfp("<<<<11101x110000xxxx101x11x0xxxx", InstName.Vabs,   InstEmit32.Vabs_S,   OpCode32SimdS.Create,           OpCode32SimdS.CreateT32);
            SetVfp("<<<<11100x11xxxxxxxx101xx0x0xxxx", InstName.Vadd,   InstEmit32.Vadd_S,   OpCode32SimdRegS.Create,        OpCode32SimdRegS.CreateT32);
            SetVfp("<<<<11101x11010xxxxx101x01x0xxxx", InstName.Vcmp,   InstEmit32.Vcmp,     OpCode32SimdS.Create,           OpCode32SimdS.CreateT32);
            SetVfp("<<<<11101x11010xxxxx101x11x0xxxx", InstName.Vcmpe,  InstEmit32.Vcmpe,    OpCode32SimdS.Create,           OpCode32SimdS.CreateT32);
            SetVfp("<<<<11101x110111xxxx101x11x0xxxx", InstName.Vcvt,   InstEmit32.Vcvt_FD,  OpCode32SimdS.Create,           OpCode32SimdS.CreateT32); // FP 32 and 64, scalar.
            SetVfp("<<<<11101x11110xxxxx101x11x0xxxx", InstName.Vcvt,   InstEmit32.Vcvt_FI,  OpCode32SimdCvtFI.Create,       OpCode32SimdCvtFI.CreateT32); // FP32 to int.
            SetVfp("<<<<11101x111000xxxx101xx1x0xxxx", InstName.Vcvt,   InstEmit32.Vcvt_FI,  OpCode32SimdCvtFI.Create,       OpCode32SimdCvtFI.CreateT32); // Int to FP32.
            SetVfp("111111101x1111xxxxxx101xx1x0xxxx", InstName.Vcvt,   InstEmit32.Vcvt_RM,  OpCode32SimdCvtFI.Create,       OpCode32SimdCvtFI.CreateT32); // The many FP32 to int encodings (fp).
            SetVfp("<<<<11101x11001xxxxx101xx1x0xxxx", InstName.Vcvt,   InstEmit32.Vcvt_TB,  OpCode32SimdCvtTB.Create,       OpCode32SimdCvtTB.CreateT32);
            SetVfp("<<<<11101x00xxxxxxxx101xx0x0xxxx", InstName.Vdiv,   InstEmit32.Vdiv_S,   OpCode32SimdRegS.Create,        OpCode32SimdRegS.CreateT32);
            SetVfp("<<<<11101xx0xxxxxxxx1011x0x10000", InstName.Vdup,   InstEmit32.Vdup,     OpCode32SimdDupGP.Create,       OpCode32SimdDupGP.CreateT32);
            SetVfp("<<<<11101x10xxxxxxxx101xx0x0xxxx", InstName.Vfma,   InstEmit32.Vfma_S,   OpCode32SimdRegS.Create,        OpCode32SimdRegS.CreateT32);
            SetVfp("<<<<11101x10xxxxxxxx101xx1x0xxxx", InstName.Vfms,   InstEmit32.Vfms_S,   OpCode32SimdRegS.Create,        OpCode32SimdRegS.CreateT32);
            SetVfp("<<<<11101x01xxxxxxxx101xx1x0xxxx", InstName.Vfnma,  InstEmit32.Vfnma_S,  OpCode32SimdRegS.Create,        OpCode32SimdRegS.CreateT32);
            SetVfp("<<<<11101x01xxxxxxxx101xx0x0xxxx", InstName.Vfnms,  InstEmit32.Vfnms_S,  OpCode32SimdRegS.Create,        OpCode32SimdRegS.CreateT32);
            SetVfp("<<<<11001x01xxxxxxxx1011xxxxxxx0", InstName.Vldm,   InstEmit32.Vldm,     OpCode32SimdMemMult.Create,     OpCode32SimdMemMult.CreateT32);
            SetVfp("<<<<11001x11xxxxxxxx1011xxxxxxx0", InstName.Vldm,   InstEmit32.Vldm,     OpCode32SimdMemMult.Create,     OpCode32SimdMemMult.CreateT32);
            SetVfp("<<<<11010x11xxxxxxxx1011xxxxxxx0", InstName.Vldm,   InstEmit32.Vldm,     OpCode32SimdMemMult.Create,     OpCode32SimdMemMult.CreateT32);
            SetVfp("<<<<11001x01xxxxxxxx1010xxxxxxxx", InstName.Vldm,   InstEmit32.Vldm,     OpCode32SimdMemMult.Create,     OpCode32SimdMemMult.CreateT32);
            SetVfp("<<<<11001x11xxxxxxxx1010xxxxxxxx", InstName.Vldm,   InstEmit32.Vldm,     OpCode32SimdMemMult.Create,     OpCode32SimdMemMult.CreateT32);
            SetVfp("<<<<11010x11xxxxxxxx1010xxxxxxxx", InstName.Vldm,   InstEmit32.Vldm,     OpCode32SimdMemMult.Create,     OpCode32SimdMemMult.CreateT32);
            SetVfp("<<<<1101xx01xxxxxxxx101xxxxxxxxx", InstName.Vldr,   InstEmit32.Vldr,     OpCode32SimdMemImm.Create,      OpCode32SimdMemImm.CreateT32);
            SetVfp("111111101x00xxxxxxxx10>>x0x0xxxx", InstName.Vmaxnm, InstEmit32.Vmaxnm_S, OpCode32SimdRegS.Create,        OpCode32SimdRegS.CreateT32);
            SetVfp("111111101x00xxxxxxxx10>>x1x0xxxx", InstName.Vminnm, InstEmit32.Vminnm_S, OpCode32SimdRegS.Create,        OpCode32SimdRegS.CreateT32);
            SetVfp("<<<<11100x00xxxxxxxx101xx0x0xxxx", InstName.Vmla,   InstEmit32.Vmla_S,   OpCode32SimdRegS.Create,        OpCode32SimdRegS.CreateT32);
            SetVfp("<<<<11100x00xxxxxxxx101xx1x0xxxx", InstName.Vmls,   InstEmit32.Vmls_S,   OpCode32SimdRegS.Create,        OpCode32SimdRegS.CreateT32);
            SetVfp("<<<<11100xx0xxxxxxxx1011xxx10000", InstName.Vmov,   InstEmit32.Vmov_G1,  OpCode32SimdMovGpElem.Create,   OpCode32SimdMovGpElem.CreateT32); // From gen purpose.
            SetVfp("<<<<1110xxx1xxxxxxxx1011xxx10000", InstName.Vmov,   InstEmit32.Vmov_G1,  OpCode32SimdMovGpElem.Create,   OpCode32SimdMovGpElem.CreateT32); // To gen purpose.
            SetVfp("<<<<1100010xxxxxxxxx101000x1xxxx", InstName.Vmov,   InstEmit32.Vmov_G2,  OpCode32SimdMovGpDouble.Create, OpCode32SimdMovGpDouble.CreateT32); // To/from gen purpose x2 and single precision x2.
            SetVfp("<<<<1100010xxxxxxxxx101100x1xxxx", InstName.Vmov,   InstEmit32.Vmov_GD,  OpCode32SimdMovGpDouble.Create, OpCode32SimdMovGpDouble.CreateT32); // To/from gen purpose x2 and double precision.
            SetVfp("<<<<1110000xxxxxxxxx1010x0010000", InstName.Vmov,   InstEmit32.Vmov_GS,  OpCode32SimdMovGp.Create,       OpCode32SimdMovGp.CreateT32); // To/from gen purpose and single precision.
            SetVfp("<<<<11101x11xxxxxxxx101x0000xxxx", InstName.Vmov,   InstEmit32.Vmov_I,   OpCode32SimdImm44.Create,       OpCode32SimdImm44.CreateT32); // Scalar f16/32/64 based on size 01 10 11.
            SetVfp("<<<<11101x110000xxxx101x01x0xxxx", InstName.Vmov,   InstEmit32.Vmov_S,   OpCode32SimdS.Create,           OpCode32SimdS.CreateT32);
            SetVfp("<<<<11101111xxxxxxxx101000010000", InstName.Vmrs,   InstEmit32.Vmrs,     OpCode32SimdSpecial.Create,     OpCode32SimdSpecial.CreateT32);
            SetVfp("<<<<11101110xxxxxxxx101000010000", InstName.Vmsr,   InstEmit32.Vmsr,     OpCode32SimdSpecial.Create,     OpCode32SimdSpecial.CreateT32);
            SetVfp("<<<<11100x10xxxxxxxx101xx0x0xxxx", InstName.Vmul,   InstEmit32.Vmul_S,   OpCode32SimdRegS.Create,        OpCode32SimdRegS.CreateT32);
            SetVfp("<<<<11101x110001xxxx101x01x0xxxx", InstName.Vneg,   InstEmit32.Vneg_S,   OpCode32SimdS.Create,           OpCode32SimdS.CreateT32);
            SetVfp("<<<<11100x01xxxxxxxx101xx1x0xxxx", InstName.Vnmla,  InstEmit32.Vnmla_S,  OpCode32SimdRegS.Create,        OpCode32SimdRegS.CreateT32);
            SetVfp("<<<<11100x01xxxxxxxx101xx0x0xxxx", InstName.Vnmls,  InstEmit32.Vnmls_S,  OpCode32SimdRegS.Create,        OpCode32SimdRegS.CreateT32);
            SetVfp("<<<<11100x10xxxxxxxx101xx1x0xxxx", InstName.Vnmul,  InstEmit32.Vnmul_S,  OpCode32SimdRegS.Create,        OpCode32SimdRegS.CreateT32);
            SetVfp("111111101x1110xxxxxx101x01x0xxxx", InstName.Vrint,  InstEmit32.Vrint_RM, OpCode32SimdS.Create,           OpCode32SimdS.CreateT32);
            SetVfp("<<<<11101x110110xxxx101x11x0xxxx", InstName.Vrint,  InstEmit32.Vrint_Z,  OpCode32SimdS.Create,           OpCode32SimdS.CreateT32);
            SetVfp("<<<<11101x110110xxxx101x01x0xxxx", InstName.Vrintr, InstEmit32.Vrintr_S, OpCode32SimdS.Create,           OpCode32SimdS.CreateT32);
            SetVfp("<<<<11101x110111xxxx101x01x0xxxx", InstName.Vrintx, InstEmit32.Vrintx_S, OpCode32SimdS.Create,           OpCode32SimdS.CreateT32);
            SetVfp("<<<<11101x110001xxxx101x11x0xxxx", InstName.Vsqrt,  InstEmit32.Vsqrt_S,  OpCode32SimdS.Create,           OpCode32SimdS.CreateT32);
            SetVfp("111111100xxxxxxxxxxx101xx0x0xxxx", InstName.Vsel,   InstEmit32.Vsel,     OpCode32SimdSel.Create,         OpCode32SimdSel.CreateT32);
            SetVfp("<<<<11001x00xxxxxxxx1011xxxxxxx0", InstName.Vstm,   InstEmit32.Vstm,     OpCode32SimdMemMult.Create,     OpCode32SimdMemMult.CreateT32);
            SetVfp("<<<<11001x10xxxxxxxx1011xxxxxxx0", InstName.Vstm,   InstEmit32.Vstm,     OpCode32SimdMemMult.Create,     OpCode32SimdMemMult.CreateT32);
            SetVfp("<<<<11010x10xxxxxxxx1011xxxxxxx0", InstName.Vstm,   InstEmit32.Vstm,     OpCode32SimdMemMult.Create,     OpCode32SimdMemMult.CreateT32);
            SetVfp("<<<<11001x00xxxxxxxx1010xxxxxxxx", InstName.Vstm,   InstEmit32.Vstm,     OpCode32SimdMemMult.Create,     OpCode32SimdMemMult.CreateT32);
            SetVfp("<<<<11001x10xxxxxxxx1010xxxxxxxx", InstName.Vstm,   InstEmit32.Vstm,     OpCode32SimdMemMult.Create,     OpCode32SimdMemMult.CreateT32);
            SetVfp("<<<<11010x10xxxxxxxx1010xxxxxxxx", InstName.Vstm,   InstEmit32.Vstm,     OpCode32SimdMemMult.Create,     OpCode32SimdMemMult.CreateT32);
            SetVfp("<<<<1101xx00xxxxxxxx101xxxxxxxxx", InstName.Vstr,   InstEmit32.Vstr,     OpCode32SimdMemImm.Create,      OpCode32SimdMemImm.CreateT32);
            SetVfp("<<<<11100x11xxxxxxxx101xx1x0xxxx", InstName.Vsub,   InstEmit32.Vsub_S,   OpCode32SimdRegS.Create,        OpCode32SimdRegS.CreateT32);

            // ASIMD
            SetAsimd("111100111x110000xxx0001101x0xxx0", InstName.Aesd_V,      InstEmit32.Aesd_V,       OpCode32Simd.Create,            OpCode32Simd.CreateT32);
            SetAsimd("111100111x110000xxx0001100x0xxx0", InstName.Aese_V,      InstEmit32.Aese_V,       OpCode32Simd.Create,            OpCode32Simd.CreateT32);
            SetAsimd("111100111x110000xxx0001111x0xxx0", InstName.Aesimc_V,    InstEmit32.Aesimc_V,     OpCode32Simd.Create,            OpCode32Simd.CreateT32);
            SetAsimd("111100111x110000xxx0001110x0xxx0", InstName.Aesmc_V,     InstEmit32.Aesmc_V,      OpCode32Simd.Create,            OpCode32Simd.CreateT32);
            SetAsimd("111100110x00xxx0xxx01100x1x0xxx0", InstName.Sha256h_V,   InstEmit32.Sha256h_V,    OpCode32SimdReg.Create,         OpCode32SimdReg.CreateT32);
            SetAsimd("111100110x01xxx0xxx01100x1x0xxx0", InstName.Sha256h2_V,  InstEmit32.Sha256h2_V,   OpCode32SimdReg.Create,         OpCode32SimdReg.CreateT32);
            SetAsimd("111100111x111010xxx0001111x0xxx0", InstName.Sha256su0_V, InstEmit32.Sha256su0_V,  OpCode32Simd.Create,            OpCode32Simd.CreateT32);
            SetAsimd("111100110x10xxx0xxx01100x1x0xxx0", InstName.Sha256su1_V, InstEmit32.Sha256su1_V,  OpCode32SimdReg.Create,         OpCode32SimdReg.CreateT32);
            SetAsimd("1111001x0x<<xxxxxxxx0111xxx0xxxx", InstName.Vabd,        InstEmit32.Vabd_I,       OpCode32SimdReg.Create,         OpCode32SimdReg.CreateT32);
            SetAsimd("1111001x1x<<xxxxxxxx0111x0x0xxxx", InstName.Vabdl,       InstEmit32.Vabdl_I,      OpCode32SimdRegLong.Create,     OpCode32SimdRegLong.CreateT32);
            SetAsimd("111100111x11<<01xxxx00110xx0xxxx", InstName.Vabs,        InstEmit32.Vabs_V,       OpCode32SimdCmpZ.Create,        OpCode32SimdCmpZ.CreateT32);
            SetAsimd("111100111x111001xxxx01110xx0xxxx", InstName.Vabs,        InstEmit32.Vabs_V,       OpCode32SimdCmpZ.Create,        OpCode32SimdCmpZ.CreateT32);
            SetAsimd("111100100xxxxxxxxxxx1000xxx0xxxx", InstName.Vadd,        InstEmit32.Vadd_I,       OpCode32SimdReg.Create,         OpCode32SimdReg.CreateT32);
            SetAsimd("111100100x00xxxxxxxx1101xxx0xxxx", InstName.Vadd,        InstEmit32.Vadd_V,       OpCode32SimdReg.Create,         OpCode32SimdReg.CreateT32);
            SetAsimd("1111001x1x<<xxxxxxx00000x0x0xxxx", InstName.Vaddl,       InstEmit32.Vaddl_I,      OpCode32SimdRegLong.Create,     OpCode32SimdRegLong.CreateT32);
            SetAsimd("1111001x1x<<xxxxxxx00001x0x0xxxx", InstName.Vaddw,       InstEmit32.Vaddw_I,      OpCode32SimdRegWide.Create,     OpCode32SimdRegWide.CreateT32);
            SetAsimd("111100100x00xxxxxxxx0001xxx1xxxx", InstName.Vand,        InstEmit32.Vand_I,       OpCode32SimdBinary.Create,      OpCode32SimdBinary.CreateT32);
            SetAsimd("111100100x01xxxxxxxx0001xxx1xxxx", InstName.Vbic,        InstEmit32.Vbic_I,       OpCode32SimdBinary.Create,      OpCode32SimdBinary.CreateT32);
            SetAsimd("1111001x1x000xxxxxxx<<x10x11xxxx", InstName.Vbic,        InstEmit32.Vbic_II,      OpCode32SimdImm.Create,         OpCode32SimdImm.CreateT32);
            SetAsimd("111100110x11xxxxxxxx0001xxx1xxxx", InstName.Vbif,        InstEmit32.Vbif,         OpCode32SimdBinary.Create,      OpCode32SimdBinary.CreateT32);
            SetAsimd("111100110x10xxxxxxxx0001xxx1xxxx", InstName.Vbit,        InstEmit32.Vbit,         OpCode32SimdBinary.Create,      OpCode32SimdBinary.CreateT32);
            SetAsimd("111100110x01xxxxxxxx0001xxx1xxxx", InstName.Vbsl,        InstEmit32.Vbsl,         OpCode32SimdBinary.Create,      OpCode32SimdBinary.CreateT32);
            SetAsimd("111100110x<<xxxxxxxx1000xxx1xxxx", InstName.Vceq,        InstEmit32.Vceq_I,       OpCode32SimdReg.Create,         OpCode32SimdReg.CreateT32);
            SetAsimd("111100100x00xxxxxxxx1110xxx0xxxx", InstName.Vceq,        InstEmit32.Vceq_V,       OpCode32SimdReg.Create,         OpCode32SimdReg.CreateT32);
            SetAsimd("111100111x11xx01xxxx0x010xx0xxxx", InstName.Vceq,        InstEmit32.Vceq_Z,       OpCode32SimdCmpZ.Create,        OpCode32SimdCmpZ.CreateT32);
            SetAsimd("1111001x0x<<xxxxxxxx0011xxx1xxxx", InstName.Vcge,        InstEmit32.Vcge_I,       OpCode32SimdReg.Create,         OpCode32SimdReg.CreateT32);
            SetAsimd("111100110x00xxxxxxxx1110xxx0xxxx", InstName.Vcge,        InstEmit32.Vcge_V,       OpCode32SimdReg.Create,         OpCode32SimdReg.CreateT32);
            SetAsimd("111100111x11xx01xxxx0x001xx0xxxx", InstName.Vcge,        InstEmit32.Vcge_Z,       OpCode32SimdCmpZ.Create,        OpCode32SimdCmpZ.CreateT32);
            SetAsimd("1111001x0x<<xxxxxxxx0011xxx0xxxx", InstName.Vcgt,        InstEmit32.Vcgt_I,       OpCode32SimdReg.Create,         OpCode32SimdReg.CreateT32);
            SetAsimd("111100110x10xxxxxxxx1110xxx0xxxx", InstName.Vcgt,        InstEmit32.Vcgt_V,       OpCode32SimdReg.Create,         OpCode32SimdReg.CreateT32);
            SetAsimd("111100111x11xx01xxxx0x000xx0xxxx", InstName.Vcgt,        InstEmit32.Vcgt_Z,       OpCode32SimdCmpZ.Create,        OpCode32SimdCmpZ.CreateT32);
            SetAsimd("111100111x11xx01xxxx0x011xx0xxxx", InstName.Vcle,        InstEmit32.Vcle_Z,       OpCode32SimdCmpZ.Create,        OpCode32SimdCmpZ.CreateT32);
            SetAsimd("111100111x11xx01xxxx0x100xx0xxxx", InstName.Vclt,        InstEmit32.Vclt_Z,       OpCode32SimdCmpZ.Create,        OpCode32SimdCmpZ.CreateT32);
            SetAsimd("111100111x110000xxxx01010xx0xxxx", InstName.Vcnt,        InstEmit32.Vcnt,         OpCode32SimdCmpZ.Create,        OpCode32SimdCmpZ.CreateT32);
            SetAsimd("111100111x111011xxxx011xxxx0xxxx", InstName.Vcvt,        InstEmit32.Vcvt_V,       OpCode32SimdCmpZ.Create,        OpCode32SimdCmpZ.CreateT32); // FP and integer, vector.
            SetAsimd("1111001x1x1xxxxxxxxx111x0xx1xxxx", InstName.Vcvt,        InstEmit32.Vcvt_V_Fixed, OpCode32SimdCvtFFixed.Create,   OpCode32SimdCvtFFixed.CreateT32); // Between floating point and fixed point, vector.
            SetAsimd("111100111x11xxxxxxxx11000xx0xxxx", InstName.Vdup,        InstEmit32.Vdup_1,       OpCode32SimdDupElem.Create,     OpCode32SimdDupElem.CreateT32);
            SetAsimd("111100110x00xxxxxxxx0001xxx1xxxx", InstName.Veor,        InstEmit32.Veor_I,       OpCode32SimdBinary.Create,      OpCode32SimdBinary.CreateT32);
            SetAsimd("111100101x11xxxxxxxxxxxxxxx0xxxx", InstName.Vext,        InstEmit32.Vext,         OpCode32SimdExt.Create,         OpCode32SimdExt.CreateT32);
            SetAsimd("111100100x00xxxxxxxx1100xxx1xxxx", InstName.Vfma,        InstEmit32.Vfma_V,       OpCode32SimdReg.Create,         OpCode32SimdReg.CreateT32);
            SetAsimd("111100100x10xxxxxxxx1100xxx1xxxx", InstName.Vfms,        InstEmit32.Vfms_V,       OpCode32SimdReg.Create,         OpCode32SimdReg.CreateT32);
            SetAsimd("1111001x0x<<xxxxxxxx0000xxx0xxxx", InstName.Vhadd,       InstEmit32.Vhadd,        OpCode32SimdReg.Create,         OpCode32SimdReg.CreateT32);
            SetAsimd("111101001x10xxxxxxxx0000xxx0xxxx", InstName.Vld1,        InstEmit32.Vld1,         OpCode32SimdMemSingle.Create,   OpCode32SimdMemSingle.CreateT32);
            SetAsimd("111101001x10xxxxxxxx0100xx0xxxxx", InstName.Vld1,        InstEmit32.Vld1,         OpCode32SimdMemSingle.Create,   OpCode32SimdMemSingle.CreateT32);
            SetAsimd("111101001x10xxxxxxxx1000x000xxxx", InstName.Vld1,        InstEmit32.Vld1,         OpCode32SimdMemSingle.Create,   OpCode32SimdMemSingle.CreateT32);
            SetAsimd("111101001x10xxxxxxxx1000x011xxxx", InstName.Vld1,        InstEmit32.Vld1,         OpCode32SimdMemSingle.Create,   OpCode32SimdMemSingle.CreateT32);
            SetAsimd("111101001x10xxxxxxxx110000x0xxxx", InstName.Vld1,        InstEmit32.Vld1,         OpCode32SimdMemSingle.Create,   OpCode32SimdMemSingle.CreateT32);
            SetAsimd("111101001x10xxxxxxxx110001xxxxxx", InstName.Vld1,        InstEmit32.Vld1,         OpCode32SimdMemSingle.Create,   OpCode32SimdMemSingle.CreateT32);
            SetAsimd("111101001x10xxxxxxxx110010xxxxxx", InstName.Vld1,        InstEmit32.Vld1,         OpCode32SimdMemSingle.Create,   OpCode32SimdMemSingle.CreateT32);
            SetAsimd("111101000x10xxxxxxxx0111xx0xxxxx", InstName.Vld1,        InstEmit32.Vld1,         OpCode32SimdMemPair.Create,     OpCode32SimdMemPair.CreateT32); // Regs = 1.
            SetAsimd("111101000x10xxxxxxxx1010xx<<xxxx", InstName.Vld1,        InstEmit32.Vld1,         OpCode32SimdMemPair.Create,     OpCode32SimdMemPair.CreateT32); // Regs = 2.
            SetAsimd("111101000x10xxxxxxxx0110xx0xxxxx", InstName.Vld1,        InstEmit32.Vld1,         OpCode32SimdMemPair.Create,     OpCode32SimdMemPair.CreateT32); // Regs = 3.
            SetAsimd("111101000x10xxxxxxxx0010xxxxxxxx", InstName.Vld1,        InstEmit32.Vld1,         OpCode32SimdMemPair.Create,     OpCode32SimdMemPair.CreateT32); // Regs = 4.
            SetAsimd("111101001x10xxxxxxxx0x01xxxxxxxx", InstName.Vld2,        InstEmit32.Vld2,         OpCode32SimdMemSingle.Create,   OpCode32SimdMemSingle.CreateT32);
            SetAsimd("111101001x10xxxxxxxx1001xx0xxxxx", InstName.Vld2,        InstEmit32.Vld2,         OpCode32SimdMemSingle.Create,   OpCode32SimdMemSingle.CreateT32);
            SetAsimd("111101001x10xxxxxxxx1101<<xxxxxx", InstName.Vld2,        InstEmit32.Vld2,         OpCode32SimdMemSingle.Create,   OpCode32SimdMemSingle.CreateT32);
            SetAsimd("111101000x10xxxxxxxx100x<<0xxxxx", InstName.Vld2,        InstEmit32.Vld2,         OpCode32SimdMemPair.Create,     OpCode32SimdMemPair.CreateT32); // Regs = 1, inc = 1/2 (itype).
            SetAsimd("111101000x10xxxxxxxx100x<<10xxxx", InstName.Vld2,        InstEmit32.Vld2,         OpCode32SimdMemPair.Create,     OpCode32SimdMemPair.CreateT32); // Regs = 1, inc = 1/2 (itype).
            SetAsimd("111101000x10xxxxxxxx0011<<xxxxxx", InstName.Vld2,        InstEmit32.Vld2,         OpCode32SimdMemPair.Create,     OpCode32SimdMemPair.CreateT32); // Regs = 2, inc = 2.
            SetAsimd("111101001x10xxxxxxxx0x10xxx0xxxx", InstName.Vld3,        InstEmit32.Vld3,         OpCode32SimdMemSingle.Create,   OpCode32SimdMemSingle.CreateT32);
            SetAsimd("111101001x10xxxxxxxx1010xx00xxxx", InstName.Vld3,        InstEmit32.Vld3,         OpCode32SimdMemSingle.Create,   OpCode32SimdMemSingle.CreateT32);
            SetAsimd("111101001x10xxxxxxxx1110<<x0xxxx", InstName.Vld3,        InstEmit32.Vld3,         OpCode32SimdMemSingle.Create,   OpCode32SimdMemSingle.CreateT32);
            SetAsimd("111101000x10xxxxxxxx010x<<0xxxxx", InstName.Vld3,        InstEmit32.Vld3,         OpCode32SimdMemPair.Create,     OpCode32SimdMemPair.CreateT32); // Inc = 1/2 (itype).
            SetAsimd("111101001x10xxxxxxxx0x11xxxxxxxx", InstName.Vld4,        InstEmit32.Vld4,         OpCode32SimdMemSingle.Create,   OpCode32SimdMemSingle.CreateT32);
            SetAsimd("111101001x10xxxxxxxx1011xx<<xxxx", InstName.Vld4,        InstEmit32.Vld4,         OpCode32SimdMemSingle.Create,   OpCode32SimdMemSingle.CreateT32);
            SetAsimd("111101001x10xxxxxxxx1111<<x>xxxx", InstName.Vld4,        InstEmit32.Vld4,         OpCode32SimdMemSingle.Create,   OpCode32SimdMemSingle.CreateT32);
            SetAsimd("111101000x10xxxxxxxx000x<<xxxxxx", InstName.Vld4,        InstEmit32.Vld4,         OpCode32SimdMemPair.Create,     OpCode32SimdMemPair.CreateT32); // Inc = 1/2 (itype).
            SetAsimd("1111001x0x<<xxxxxxxx0110xxx0xxxx", InstName.Vmax,        InstEmit32.Vmax_I,       OpCode32SimdReg.Create,         OpCode32SimdReg.CreateT32);
            SetAsimd("111100100x00xxxxxxxx1111xxx0xxxx", InstName.Vmax,        InstEmit32.Vmax_V,       OpCode32SimdReg.Create,         OpCode32SimdReg.CreateT32);
            SetAsimd("1111001x0x<<xxxxxxxx0110xxx1xxxx", InstName.Vmin,        InstEmit32.Vmin_I,       OpCode32SimdReg.Create,         OpCode32SimdReg.CreateT32);
            SetAsimd("111100100x10xxxxxxxx1111xxx0xxxx", InstName.Vmin,        InstEmit32.Vmin_V,       OpCode32SimdReg.Create,         OpCode32SimdReg.CreateT32);
            SetAsimd("111100110x0xxxxxxxxx1111xxx1xxxx", InstName.Vmaxnm,      InstEmit32.Vmaxnm_V,     OpCode32SimdReg.Create,         OpCode32SimdReg.CreateT32);
            SetAsimd("111100110x1xxxxxxxxx1111xxx1xxxx", InstName.Vminnm,      InstEmit32.Vminnm_V,     OpCode32SimdReg.Create,         OpCode32SimdReg.CreateT32);
            SetAsimd("1111001x1x<<xxxxxxxx000xx1x0xxxx", InstName.Vmla,        InstEmit32.Vmla_1,       OpCode32SimdRegElem.Create,     OpCode32SimdRegElem.CreateT32);
            SetAsimd("111100100xxxxxxxxxxx1001xxx0xxxx", InstName.Vmla,        InstEmit32.Vmla_I,       OpCode32SimdReg.Create,         OpCode32SimdReg.CreateT32);
            SetAsimd("111100100x00xxxxxxxx1101xxx1xxxx", InstName.Vmla,        InstEmit32.Vmla_V,       OpCode32SimdReg.Create,         OpCode32SimdReg.CreateT32);
            SetAsimd("1111001x1x<<xxxxxxx01000x0x0xxxx", InstName.Vmlal,       InstEmit32.Vmlal_I,      OpCode32SimdRegLong.Create,     OpCode32SimdRegLong.CreateT32);
            SetAsimd("1111001x1x<<xxxxxxxx010xx1x0xxxx", InstName.Vmls,        InstEmit32.Vmls_1,       OpCode32SimdRegElem.Create,     OpCode32SimdRegElem.CreateT32);
            SetAsimd("111100100x10xxxxxxxx1101xxx1xxxx", InstName.Vmls,        InstEmit32.Vmls_V,       OpCode32SimdReg.Create,         OpCode32SimdReg.CreateT32);
            SetAsimd("111100110xxxxxxxxxxx1001xxx0xxxx", InstName.Vmls,        InstEmit32.Vmls_I,       OpCode32SimdReg.Create,         OpCode32SimdReg.CreateT32);
            SetAsimd("1111001x1x<<xxxxxxx01010x0x0xxxx", InstName.Vmlsl,       InstEmit32.Vmlsl_I,      OpCode32SimdRegLong.Create,     OpCode32SimdRegLong.CreateT32);
            SetAsimd("1111001x1x000xxxxxxx0xx00x01xxxx", InstName.Vmov,        InstEmit32.Vmov_I,       OpCode32SimdImm.Create,         OpCode32SimdImm.CreateT32); // D/Q vector I32.
            SetAsimd("1111001x1x000xxxxxxx10x00x01xxxx", InstName.Vmov,        InstEmit32.Vmov_I,       OpCode32SimdImm.Create,         OpCode32SimdImm.CreateT32); // D/Q I16.
            SetAsimd("1111001x1x000xxxxxxx11xx0x01xxxx", InstName.Vmov,        InstEmit32.Vmov_I,       OpCode32SimdImm.Create,         OpCode32SimdImm.CreateT32); // D/Q (dt - from cmode).
            SetAsimd("1111001x1x000xxxxxxx11100x11xxxx", InstName.Vmov,        InstEmit32.Vmov_I,       OpCode32SimdImm.Create,         OpCode32SimdImm.CreateT32); // D/Q I64.
            SetAsimd("1111001x1x001000xxx0101000x1xxxx", InstName.Vmovl,       InstEmit32.Vmovl,        OpCode32SimdLong.Create,        OpCode32SimdLong.CreateT32);
            SetAsimd("1111001x1x010000xxx0101000x1xxxx", InstName.Vmovl,       InstEmit32.Vmovl,        OpCode32SimdLong.Create,        OpCode32SimdLong.CreateT32);
            SetAsimd("1111001x1x100000xxx0101000x1xxxx", InstName.Vmovl,       InstEmit32.Vmovl,        OpCode32SimdLong.Create,        OpCode32SimdLong.CreateT32);
            SetAsimd("111100111x11<<10xxxx001000x0xxx0", InstName.Vmovn,       InstEmit32.Vmovn,        OpCode32SimdMovn.Create,        OpCode32SimdMovn.CreateT32);
            SetAsimd("1111001x1x<<xxxxxxxx100xx1x0xxxx", InstName.Vmul,        InstEmit32.Vmul_1,       OpCode32SimdRegElem.Create,     OpCode32SimdRegElem.CreateT32);
            SetAsimd("111100100x<<xxxxxxxx1001xxx1xxxx", InstName.Vmul,        InstEmit32.Vmul_I,       OpCode32SimdReg.Create,         OpCode32SimdReg.CreateT32);
            SetAsimd("111100110x00xxxxxxxx1001xxx1xxxx", InstName.Vmul,        InstEmit32.Vmul_I,       OpCode32SimdReg.Create,         OpCode32SimdReg.CreateT32);
            SetAsimd("111100110x00xxxxxxxx1101xxx1xxxx", InstName.Vmul,        InstEmit32.Vmul_V,       OpCode32SimdReg.Create,         OpCode32SimdReg.CreateT32);
            SetAsimd("1111001x1x<<xxxxxxx01010x1x0xxxx", InstName.Vmull,       InstEmit32.Vmull_1,      OpCode32SimdRegElemLong.Create, OpCode32SimdRegElemLong.CreateT32);
            SetAsimd("1111001x1x<<xxxxxxx01100x0x0xxxx", InstName.Vmull,       InstEmit32.Vmull_I,      OpCode32SimdRegLong.Create,     OpCode32SimdRegLong.CreateT32);
            SetAsimd("111100101xx0xxxxxxx01110x0x0xxxx", InstName.Vmull,       InstEmit32.Vmull_I,      OpCode32SimdRegLong.Create,     OpCode32SimdRegLong.CreateT32); // P8/P64
            SetAsimd("111100111x110000xxxx01011xx0xxxx", InstName.Vmvn,        InstEmit32.Vmvn_I,       OpCode32SimdBinary.Create,      OpCode32SimdBinary.CreateT32);
            SetAsimd("1111001x1x000xxxxxxx0xx00x11xxxx", InstName.Vmvn,        InstEmit32.Vmvn_II,      OpCode32SimdImm.Create,         OpCode32SimdImm.CreateT32); // D/Q vector I32.
            SetAsimd("1111001x1x000xxxxxxx10x00x11xxxx", InstName.Vmvn,        InstEmit32.Vmvn_II,      OpCode32SimdImm.Create,         OpCode32SimdImm.CreateT32);
            SetAsimd("1111001x1x000xxxxxxx110x0x11xxxx", InstName.Vmvn,        InstEmit32.Vmvn_II,      OpCode32SimdImm.Create,         OpCode32SimdImm.CreateT32);
            SetAsimd("111100111x11<<01xxxx00111xx0xxxx", InstName.Vneg,        InstEmit32.Vneg_V,       OpCode32SimdCmpZ.Create,        OpCode32SimdCmpZ.CreateT32);
            SetAsimd("111100111x111001xxxx01111xx0xxxx", InstName.Vneg,        InstEmit32.Vneg_V,       OpCode32SimdCmpZ.Create,        OpCode32SimdCmpZ.CreateT32);
            SetAsimd("111100100x11xxxxxxxx0001xxx1xxxx", InstName.Vorn,        InstEmit32.Vorn_I,       OpCode32SimdBinary.Create,      OpCode32SimdBinary.CreateT32);
            SetAsimd("111100100x10xxxxxxxx0001xxx1xxxx", InstName.Vorr,        InstEmit32.Vorr_I,       OpCode32SimdBinary.Create,      OpCode32SimdBinary.CreateT32);
            SetAsimd("1111001x1x000xxxxxxx<<x10x01xxxx", InstName.Vorr,        InstEmit32.Vorr_II,      OpCode32SimdImm.Create,         OpCode32SimdImm.CreateT32);
            SetAsimd("111100100x<<xxxxxxxx1011x0x1xxxx", InstName.Vpadd,       InstEmit32.Vpadd_I,      OpCode32SimdReg.Create,         OpCode32SimdReg.CreateT32);
            SetAsimd("111100110x00xxxxxxxx1101x0x0xxxx", InstName.Vpadd,       InstEmit32.Vpadd_V,      OpCode32SimdReg.Create,         OpCode32SimdReg.CreateT32);
            SetAsimd("111100111x11<<00xxxx0110xxx0xxxx", InstName.Vpadal,      InstEmit32.Vpadal,       OpCode32SimdCmpZ.Create,        OpCode32SimdCmpZ.CreateT32);
            SetAsimd("111100111x11<<00xxxx0010xxx0xxxx", InstName.Vpaddl,      InstEmit32.Vpaddl,       OpCode32SimdCmpZ.Create,        OpCode32SimdCmpZ.CreateT32);
            SetAsimd("1111001x0x<<xxxxxxxx1010x0x0xxxx", InstName.Vpmax,       InstEmit32.Vpmax_I,      OpCode32SimdReg.Create,         OpCode32SimdReg.CreateT32);
            SetAsimd("111100110x00xxxxxxxx1111x0x0xxxx", InstName.Vpmax,       InstEmit32.Vpmax_V,      OpCode32SimdReg.Create,         OpCode32SimdReg.CreateT32);
            SetAsimd("1111001x0x<<xxxxxxxx1010x0x1xxxx", InstName.Vpmin,       InstEmit32.Vpmin_I,      OpCode32SimdReg.Create,         OpCode32SimdReg.CreateT32);
            SetAsimd("111100110x10xxxxxxxx1111x0x0xxxx", InstName.Vpmin,       InstEmit32.Vpmin_V,      OpCode32SimdReg.Create,         OpCode32SimdReg.CreateT32);
            SetAsimd("1111001x0xxxxxxxxxxx0000xxx1xxxx", InstName.Vqadd,       InstEmit32.Vqadd,        OpCode32SimdReg.Create,         OpCode32SimdReg.CreateT32);
            SetAsimd("111100100x01xxxxxxxx1011xxx0xxxx", InstName.Vqdmulh,     InstEmit32.Vqdmulh,      OpCode32SimdReg.Create,         OpCode32SimdReg.CreateT32);
            SetAsimd("111100100x10xxxxxxxx1011xxx0xxxx", InstName.Vqdmulh,     InstEmit32.Vqdmulh,      OpCode32SimdReg.Create,         OpCode32SimdReg.CreateT32);
            SetAsimd("111100111x11<<10xxxx00101xx0xxx0", InstName.Vqmovn,      InstEmit32.Vqmovn,       OpCode32SimdMovn.Create,        OpCode32SimdMovn.CreateT32);
            SetAsimd("111100111x11<<10xxxx001001x0xxx0", InstName.Vqmovun,     InstEmit32.Vqmovun,      OpCode32SimdMovn.Create,        OpCode32SimdMovn.CreateT32);
            SetAsimd("111100110x01xxxxxxxx1011xxx0xxxx", InstName.Vqrdmulh,    InstEmit32.Vqrdmulh,     OpCode32SimdReg.Create,         OpCode32SimdReg.CreateT32);
            SetAsimd("111100110x10xxxxxxxx1011xxx0xxxx", InstName.Vqrdmulh,    InstEmit32.Vqrdmulh,     OpCode32SimdReg.Create,         OpCode32SimdReg.CreateT32);
            SetAsimd("1111001x1x>>>xxxxxxx100101x1xxx0", InstName.Vqrshrn,     InstEmit32.Vqrshrn,      OpCode32SimdShImmNarrow.Create, OpCode32SimdShImmNarrow.CreateT32);
            SetAsimd("111100111x>>>xxxxxxx100001x1xxx0", InstName.Vqrshrun,    InstEmit32.Vqrshrun,     OpCode32SimdShImmNarrow.Create, OpCode32SimdShImmNarrow.CreateT32);
            SetAsimd("1111001x1x>>>xxxxxxx100100x1xxx0", InstName.Vqshrn,      InstEmit32.Vqshrn,       OpCode32SimdShImmNarrow.Create, OpCode32SimdShImmNarrow.CreateT32);
            SetAsimd("111100111x>>>xxxxxxx100000x1xxx0", InstName.Vqshrun,     InstEmit32.Vqshrun,      OpCode32SimdShImmNarrow.Create, OpCode32SimdShImmNarrow.CreateT32);
            SetAsimd("1111001x0xxxxxxxxxxx0010xxx1xxxx", InstName.Vqsub,       InstEmit32.Vqsub,        OpCode32SimdReg.Create,         OpCode32SimdReg.CreateT32);
            SetAsimd("111100111x111011xxxx010x0xx0xxxx", InstName.Vrecpe,      InstEmit32.Vrecpe,       OpCode32SimdSqrte.Create,       OpCode32SimdSqrte.CreateT32);
            SetAsimd("111100100x00xxxxxxxx1111xxx1xxxx", InstName.Vrecps,      InstEmit32.Vrecps,       OpCode32SimdReg.Create,         OpCode32SimdReg.CreateT32);
            SetAsimd("111100111x11xx00xxxx000<<xx0xxxx", InstName.Vrev,        InstEmit32.Vrev,         OpCode32SimdRev.Create,         OpCode32SimdRev.CreateT32);
            SetAsimd("1111001x0x<<xxxxxxxx0001xxx0xxxx", InstName.Vrhadd,      InstEmit32.Vrhadd,       OpCode32SimdReg.Create,         OpCode32SimdReg.CreateT32);
            SetAsimd("111100111x111010xxxx01010xx0xxxx", InstName.Vrinta,      InstEmit32.Vrinta_V,     OpCode32SimdCmpZ.Create,        OpCode32SimdCmpZ.CreateT32);
            SetAsimd("111100111x111010xxxx01101xx0xxxx", InstName.Vrintm,      InstEmit32.Vrintm_V,     OpCode32SimdCmpZ.Create,        OpCode32SimdCmpZ.CreateT32);
            SetAsimd("111100111x111010xxxx01000xx0xxxx", InstName.Vrintn,      InstEmit32.Vrintn_V,     OpCode32SimdCmpZ.Create,        OpCode32SimdCmpZ.CreateT32);
            SetAsimd("111100111x111010xxxx01111xx0xxxx", InstName.Vrintp,      InstEmit32.Vrintp_V,     OpCode32SimdCmpZ.Create,        OpCode32SimdCmpZ.CreateT32);
            SetAsimd("1111001x1x>>>xxxxxxx0010>xx1xxxx", InstName.Vrshr,       InstEmit32.Vrshr,        OpCode32SimdShImm.Create,       OpCode32SimdShImm.CreateT32);
            SetAsimd("111100101x>>>xxxxxxx100001x1xxx0", InstName.Vrshrn,      InstEmit32.Vrshrn,       OpCode32SimdShImmNarrow.Create, OpCode32SimdShImmNarrow.CreateT32);
            SetAsimd("111100111x111011xxxx010x1xx0xxxx", InstName.Vrsqrte,     InstEmit32.Vrsqrte,      OpCode32SimdSqrte.Create,       OpCode32SimdSqrte.CreateT32);
            SetAsimd("111100100x10xxxxxxxx1111xxx1xxxx", InstName.Vrsqrts,     InstEmit32.Vrsqrts,      OpCode32SimdReg.Create,         OpCode32SimdReg.CreateT32);
            SetAsimd("1111001x1x>>>xxxxxxx0011>xx1xxxx", InstName.Vrsra,       InstEmit32.Vrsra,        OpCode32SimdShImm.Create,       OpCode32SimdShImm.CreateT32);
            SetAsimd("111100101x>>>xxxxxxx0101>xx1xxxx", InstName.Vshl,        InstEmit32.Vshl,         OpCode32SimdShImm.Create,       OpCode32SimdShImm.CreateT32);
            SetAsimd("1111001x0xxxxxxxxxxx0100xxx0xxxx", InstName.Vshl,        InstEmit32.Vshl_I,       OpCode32SimdReg.Create,         OpCode32SimdReg.CreateT32);
            SetAsimd("1111001x1x>>>xxxxxxx101000x1xxxx", InstName.Vshll,       InstEmit32.Vshll,        OpCode32SimdShImmLong.Create,   OpCode32SimdShImmLong.CreateT32); // A1 encoding.
            SetAsimd("111100111x11<<10xxxx001100x0xxxx", InstName.Vshll,       InstEmit32.Vshll2,       OpCode32SimdMovn.Create,        OpCode32SimdMovn.CreateT32); // A2 encoding.
            SetAsimd("1111001x1x>>>xxxxxxx0000>xx1xxxx", InstName.Vshr,        InstEmit32.Vshr,         OpCode32SimdShImm.Create,       OpCode32SimdShImm.CreateT32);
            SetAsimd("111100101x>>>xxxxxxx100000x1xxx0", InstName.Vshrn,       InstEmit32.Vshrn,        OpCode32SimdShImmNarrow.Create, OpCode32SimdShImmNarrow.CreateT32);
            SetAsimd("111100111x>>>xxxxxxx0101>xx1xxxx", InstName.Vsli,        InstEmit32.Vsli_I,       OpCode32SimdShImm.Create,       OpCode32SimdShImm.CreateT32);
            SetAsimd("1111001x1x>>>xxxxxxx0001>xx1xxxx", InstName.Vsra,        InstEmit32.Vsra,         OpCode32SimdShImm.Create,       OpCode32SimdShImm.CreateT32);
            SetAsimd("111101001x00xxxxxxxx0000xxx0xxxx", InstName.Vst1,        InstEmit32.Vst1,         OpCode32SimdMemSingle.Create,   OpCode32SimdMemSingle.CreateT32);
            SetAsimd("111101001x00xxxxxxxx0100xx0xxxxx", InstName.Vst1,        InstEmit32.Vst1,         OpCode32SimdMemSingle.Create,   OpCode32SimdMemSingle.CreateT32);
            SetAsimd("111101001x00xxxxxxxx1000x000xxxx", InstName.Vst1,        InstEmit32.Vst1,         OpCode32SimdMemSingle.Create,   OpCode32SimdMemSingle.CreateT32);
            SetAsimd("111101001x00xxxxxxxx1000x011xxxx", InstName.Vst1,        InstEmit32.Vst1,         OpCode32SimdMemSingle.Create,   OpCode32SimdMemSingle.CreateT32);
            SetAsimd("111101000x00xxxxxxxx0111xx0xxxxx", InstName.Vst1,        InstEmit32.Vst1,         OpCode32SimdMemPair.Create,     OpCode32SimdMemPair.CreateT32); // Regs = 1.
            SetAsimd("111101000x00xxxxxxxx1010xx<<xxxx", InstName.Vst1,        InstEmit32.Vst1,         OpCode32SimdMemPair.Create,     OpCode32SimdMemPair.CreateT32); // Regs = 2.
            SetAsimd("111101000x00xxxxxxxx0110xx0xxxxx", InstName.Vst1,        InstEmit32.Vst1,         OpCode32SimdMemPair.Create,     OpCode32SimdMemPair.CreateT32); // Regs = 3.
            SetAsimd("111101000x00xxxxxxxx0010xxxxxxxx", InstName.Vst1,        InstEmit32.Vst1,         OpCode32SimdMemPair.Create,     OpCode32SimdMemPair.CreateT32); // Regs = 4.
            SetAsimd("111101001x00xxxxxxxx0x01xxxxxxxx", InstName.Vst2,        InstEmit32.Vst2,         OpCode32SimdMemSingle.Create,   OpCode32SimdMemSingle.CreateT32);
            SetAsimd("111101001x00xxxxxxxx1001xx0xxxxx", InstName.Vst2,        InstEmit32.Vst2,         OpCode32SimdMemSingle.Create,   OpCode32SimdMemSingle.CreateT32);
            SetAsimd("111101000x00xxxxxxxx100x<<0xxxxx", InstName.Vst2,        InstEmit32.Vst2,         OpCode32SimdMemPair.Create,     OpCode32SimdMemPair.CreateT32); // Regs = 1, inc = 1/2 (itype).
            SetAsimd("111101000x00xxxxxxxx100x<<10xxxx", InstName.Vst2,        InstEmit32.Vst2,         OpCode32SimdMemPair.Create,     OpCode32SimdMemPair.CreateT32); // Regs = 1, inc = 1/2 (itype).
            SetAsimd("111101000x00xxxxxxxx0011<<xxxxxx", InstName.Vst2,        InstEmit32.Vst2,         OpCode32SimdMemPair.Create,     OpCode32SimdMemPair.CreateT32); // Regs = 2, inc = 2.
            SetAsimd("111101001x00xxxxxxxx0x10xxx0xxxx", InstName.Vst3,        InstEmit32.Vst3,         OpCode32SimdMemSingle.Create,   OpCode32SimdMemSingle.CreateT32);
            SetAsimd("111101001x00xxxxxxxx1010xx00xxxx", InstName.Vst3,        InstEmit32.Vst3,         OpCode32SimdMemSingle.Create,   OpCode32SimdMemSingle.CreateT32);
            SetAsimd("111101000x00xxxxxxxx010x<<0xxxxx", InstName.Vst3,        InstEmit32.Vst3,         OpCode32SimdMemPair.Create,     OpCode32SimdMemPair.CreateT32); // Inc = 1/2 (itype).
            SetAsimd("111101001x00xxxxxxxx0x11xxxxxxxx", InstName.Vst4,        InstEmit32.Vst4,         OpCode32SimdMemSingle.Create,   OpCode32SimdMemSingle.CreateT32);
            SetAsimd("111101001x00xxxxxxxx1011xx<<xxxx", InstName.Vst4,        InstEmit32.Vst4,         OpCode32SimdMemSingle.Create,   OpCode32SimdMemSingle.CreateT32);
            SetAsimd("111101000x00xxxxxxxx000x<<xxxxxx", InstName.Vst4,        InstEmit32.Vst4,         OpCode32SimdMemPair.Create,     OpCode32SimdMemPair.CreateT32); // Inc = 1/2 (itype).
            SetAsimd("111100110xxxxxxxxxxx1000xxx0xxxx", InstName.Vsub,        InstEmit32.Vsub_I,       OpCode32SimdReg.Create,         OpCode32SimdReg.CreateT32);
            SetAsimd("111100100x10xxxxxxxx1101xxx0xxxx", InstName.Vsub,        InstEmit32.Vsub_V,       OpCode32SimdReg.Create,         OpCode32SimdReg.CreateT32);
            SetAsimd("1111001x1x<<xxxxxxx00010x0x0xxxx", InstName.Vsubl,       InstEmit32.Vsubl_I,      OpCode32SimdRegLong.Create,     OpCode32SimdRegLong.CreateT32);
            SetAsimd("1111001x1x<<xxxxxxx00011x0x0xxxx", InstName.Vsubw,       InstEmit32.Vsubw_I,      OpCode32SimdRegWide.Create,     OpCode32SimdRegWide.CreateT32);
            SetAsimd("111100111x110010xxxx00000xx0xxxx", InstName.Vswp,        InstEmit32.Vswp,         OpCode32Simd.Create,            OpCode32Simd.CreateT32);
            SetAsimd("111100111x11xxxxxxxx10xxxxx0xxxx", InstName.Vtbl,        InstEmit32.Vtbl,         OpCode32SimdTbl.Create,         OpCode32SimdTbl.CreateT32);
            SetAsimd("111100111x11<<10xxxx00001xx0xxxx", InstName.Vtrn,        InstEmit32.Vtrn,         OpCode32SimdCmpZ.Create,        OpCode32SimdCmpZ.CreateT32);
            SetAsimd("111100100x<<xxxxxxxx1000xxx1xxxx", InstName.Vtst,        InstEmit32.Vtst,         OpCode32SimdReg.Create,         OpCode32SimdReg.CreateT32);
            SetAsimd("111100111x11<<10xxxx00010xx0xxxx", InstName.Vuzp,        InstEmit32.Vuzp,         OpCode32SimdCmpZ.Create,        OpCode32SimdCmpZ.CreateT32);
            SetAsimd("111100111x11<<10xxxx00011xx0xxxx", InstName.Vzip,        InstEmit32.Vzip,         OpCode32SimdCmpZ.Create,        OpCode32SimdCmpZ.CreateT32);
            #endregion

            #region "OpCode Table (AArch32, T16)"
            SetT16("000<<xxxxxxxxxxx", InstName.Mov,    InstEmit32.Mov,     OpCodeT16ShiftImm.Create);
            SetT16("0001100xxxxxxxxx", InstName.Add,    InstEmit32.Add,     OpCodeT16AddSubReg.Create);
            SetT16("0001101xxxxxxxxx", InstName.Sub,    InstEmit32.Sub,     OpCodeT16AddSubReg.Create);
            SetT16("0001110xxxxxxxxx", InstName.Add,    InstEmit32.Add,     OpCodeT16AddSubImm3.Create);
            SetT16("0001111xxxxxxxxx", InstName.Sub,    InstEmit32.Sub,     OpCodeT16AddSubImm3.Create);
            SetT16("00100xxxxxxxxxxx", InstName.Mov,    InstEmit32.Mov,     OpCodeT16AluImm8.Create);
            SetT16("00101xxxxxxxxxxx", InstName.Cmp,    InstEmit32.Cmp,     OpCodeT16AluImm8.Create);
            SetT16("00110xxxxxxxxxxx", InstName.Add,    InstEmit32.Add,     OpCodeT16AluImm8.Create);
            SetT16("00111xxxxxxxxxxx", InstName.Sub,    InstEmit32.Sub,     OpCodeT16AluImm8.Create);
            SetT16("0100000000xxxxxx", InstName.And,    InstEmit32.And,     OpCodeT16AluRegLow.Create);
            SetT16("0100000001xxxxxx", InstName.Eor,    InstEmit32.Eor,     OpCodeT16AluRegLow.Create);
            SetT16("0100000010xxxxxx", InstName.Mov,    InstEmit32.Mov,     OpCodeT16ShiftReg.Create);
            SetT16("0100000011xxxxxx", InstName.Mov,    InstEmit32.Mov,     OpCodeT16ShiftReg.Create);
            SetT16("0100000100xxxxxx", InstName.Mov,    InstEmit32.Mov,     OpCodeT16ShiftReg.Create);
            SetT16("0100000101xxxxxx", InstName.Adc,    InstEmit32.Adc,     OpCodeT16AluRegLow.Create);
            SetT16("0100000110xxxxxx", InstName.Sbc,    InstEmit32.Sbc,     OpCodeT16AluRegLow.Create);
            SetT16("0100000111xxxxxx", InstName.Mov,    InstEmit32.Mov,     OpCodeT16ShiftReg.Create);
            SetT16("0100001000xxxxxx", InstName.Tst,    InstEmit32.Tst,     OpCodeT16AluRegLow.Create);
            SetT16("0100001001xxxxxx", InstName.Rsb,    InstEmit32.Rsb,     OpCodeT16AluImmZero.Create);
            SetT16("0100001010xxxxxx", InstName.Cmp,    InstEmit32.Cmp,     OpCodeT16AluRegLow.Create);
            SetT16("0100001011xxxxxx", InstName.Cmn,    InstEmit32.Cmn,     OpCodeT16AluRegLow.Create);
            SetT16("0100001100xxxxxx", InstName.Orr,    InstEmit32.Orr,     OpCodeT16AluRegLow.Create);
            SetT16("0100001101xxxxxx", InstName.Mul,    InstEmit32.Mul,     OpCodeT16AluRegLow.Create);
            SetT16("0100001110xxxxxx", InstName.Bic,    InstEmit32.Bic,     OpCodeT16AluRegLow.Create);
            SetT16("0100001111xxxxxx", InstName.Mvn,    InstEmit32.Mvn,     OpCodeT16AluRegLow.Create);
            SetT16("01000100xxxxxxxx", InstName.Add,    InstEmit32.Add,     OpCodeT16AluRegHigh.Create);
            SetT16("01000101xxxxxxxx", InstName.Cmp,    InstEmit32.Cmp,     OpCodeT16AluRegHigh.Create);
            SetT16("01000110xxxxxxxx", InstName.Mov,    InstEmit32.Mov,     OpCodeT16AluRegHigh.Create);
            SetT16("010001110xxxx000", InstName.Bx,     InstEmit32.Bx,      OpCodeT16BReg.Create);
            SetT16("010001111xxxx000", InstName.Blx,    InstEmit32.Blxr,    OpCodeT16BReg.Create);
            SetT16("01001xxxxxxxxxxx", InstName.Ldr,    InstEmit32.Ldr,     OpCodeT16MemLit.Create);
            SetT16("0101000xxxxxxxxx", InstName.Str,    InstEmit32.Str,     OpCodeT16MemReg.Create);
            SetT16("0101001xxxxxxxxx", InstName.Strh,   InstEmit32.Strh,    OpCodeT16MemReg.Create);
            SetT16("0101010xxxxxxxxx", InstName.Strb,   InstEmit32.Strb,    OpCodeT16MemReg.Create);
            SetT16("0101011xxxxxxxxx", InstName.Ldrsb,  InstEmit32.Ldrsb,   OpCodeT16MemReg.Create);
            SetT16("0101100xxxxxxxxx", InstName.Ldr,    InstEmit32.Ldr,     OpCodeT16MemReg.Create);
            SetT16("0101101xxxxxxxxx", InstName.Ldrh,   InstEmit32.Ldrh,    OpCodeT16MemReg.Create);
            SetT16("0101110xxxxxxxxx", InstName.Ldrb,   InstEmit32.Ldrb,    OpCodeT16MemReg.Create);
            SetT16("0101111xxxxxxxxx", InstName.Ldrsh,  InstEmit32.Ldrsh,   OpCodeT16MemReg.Create);
            SetT16("01100xxxxxxxxxxx", InstName.Str,    InstEmit32.Str,     OpCodeT16MemImm5.Create);
            SetT16("01101xxxxxxxxxxx", InstName.Ldr,    InstEmit32.Ldr,     OpCodeT16MemImm5.Create);
            SetT16("01110xxxxxxxxxxx", InstName.Strb,   InstEmit32.Strb,    OpCodeT16MemImm5.Create);
            SetT16("01111xxxxxxxxxxx", InstName.Ldrb,   InstEmit32.Ldrb,    OpCodeT16MemImm5.Create);
            SetT16("10000xxxxxxxxxxx", InstName.Strh,   InstEmit32.Strh,    OpCodeT16MemImm5.Create);
            SetT16("10001xxxxxxxxxxx", InstName.Ldrh,   InstEmit32.Ldrh,    OpCodeT16MemImm5.Create);
            SetT16("10010xxxxxxxxxxx", InstName.Str,    InstEmit32.Str,     OpCodeT16MemSp.Create);
            SetT16("10011xxxxxxxxxxx", InstName.Ldr,    InstEmit32.Ldr,     OpCodeT16MemSp.Create);
            SetT16("10100xxxxxxxxxxx", InstName.Adr,    InstEmit32.Adr,     OpCodeT16Adr.Create);
            SetT16("10101xxxxxxxxxxx", InstName.Add,    InstEmit32.Add,     OpCodeT16SpRel.Create);
            SetT16("101100000xxxxxxx", InstName.Add,    InstEmit32.Add,     OpCodeT16AddSubSp.Create);
            SetT16("101100001xxxxxxx", InstName.Sub,    InstEmit32.Sub,     OpCodeT16AddSubSp.Create);
            SetT16("1011001000xxxxxx", InstName.Sxth,   InstEmit32.Sxth,    OpCodeT16AluUx.Create);
            SetT16("1011001001xxxxxx", InstName.Sxtb,   InstEmit32.Sxtb,    OpCodeT16AluUx.Create);
            SetT16("1011001010xxxxxx", InstName.Uxth,   InstEmit32.Uxth,    OpCodeT16AluUx.Create);
            SetT16("1011001011xxxxxx", InstName.Uxtb,   InstEmit32.Uxtb,    OpCodeT16AluUx.Create);
            SetT16("101100x1xxxxxxxx", InstName.Cbz,    InstEmit32.Cbz,     OpCodeT16BImmCmp.Create);
            SetT16("1011010xxxxxxxxx", InstName.Push,   InstEmit32.Stm,     OpCodeT16MemStack.Create);
            SetT16("1011101000xxxxxx", InstName.Rev,    InstEmit32.Rev,     OpCodeT16AluRegLow.Create);
            SetT16("1011101001xxxxxx", InstName.Rev16,  InstEmit32.Rev16,   OpCodeT16AluRegLow.Create);
            SetT16("1011101011xxxxxx", InstName.Revsh,  InstEmit32.Revsh,   OpCodeT16AluRegLow.Create);
            SetT16("101110x1xxxxxxxx", InstName.Cbnz,   InstEmit32.Cbnz,    OpCodeT16BImmCmp.Create);
            SetT16("1011110xxxxxxxxx", InstName.Pop,    InstEmit32.Ldm,     OpCodeT16MemStack.Create);
            SetT16("1011111100000000", InstName.Nop,    InstEmit32.Nop,     OpCodeT16.Create);
            SetT16("1011111100010000", InstName.Yield,  InstEmit32.Nop,     OpCodeT16.Create);
            SetT16("1011111100100000", InstName.Wfe,    InstEmit32.Nop,     OpCodeT16.Create);
            SetT16("1011111100110000", InstName.Wfi,    InstEmit32.Nop,     OpCodeT16.Create);
            SetT16("1011111101000000", InstName.Sev,    InstEmit32.Nop,     OpCodeT16.Create);
            SetT16("1011111101010000", InstName.Sevl,   InstEmit32.Nop,     OpCodeT16.Create);
            SetT16("10111111011x0000", InstName.Hint,   InstEmit32.Nop,     OpCodeT16.Create); // Hint instruction
            SetT16("101111111xxx0000", InstName.Hint,   InstEmit32.Nop,     OpCodeT16.Create); // Hint instruction
            SetT16("10111111xxxx>>>>", InstName.It,     InstEmit32.It,      OpCodeT16IfThen.Create);
            SetT16("11000xxxxxxxxxxx", InstName.Stm,    InstEmit32.Stm,     OpCodeT16MemMult.Create);
            SetT16("11001xxxxxxxxxxx", InstName.Ldm,    InstEmit32.Ldm,     OpCodeT16MemMult.Create);
            SetT16("1101<<<xxxxxxxxx", InstName.B,      InstEmit32.B,       OpCodeT16BImm8.Create);
            SetT16("11011111xxxxxxxx", InstName.Svc,    InstEmit32.Svc,     OpCodeT16Exception.Create);
            SetT16("11100xxxxxxxxxxx", InstName.B,      InstEmit32.B,       OpCodeT16BImm11.Create);
            #endregion

            #region "OpCode Table (AArch32, T32)"
            // Base
            SetT32("11101011010xxxxx0xxxxxxxxxxxxxxx", InstName.Adc,      InstEmit32.Adc,      OpCodeT32AluRsImm.Create);
            SetT32("11110x01010xxxxx0xxxxxxxxxxxxxxx", InstName.Adc,      InstEmit32.Adc,      OpCodeT32AluImm.Create);
            SetT32("11101011000<xxxx0xxx<<<<xxxxxxxx", InstName.Add,      InstEmit32.Add,      OpCodeT32AluRsImm.Create);
            SetT32("11110x01000<xxxx0xxx<<<<xxxxxxxx", InstName.Add,      InstEmit32.Add,      OpCodeT32AluImm.Create);
            SetT32("11110x100000xxxx0xxxxxxxxxxxxxxx", InstName.Add,      InstEmit32.Add,      OpCodeT32AluImm12.Create);
            SetT32("11101010000<xxxx0xxx<<<<xxxxxxxx", InstName.And,      InstEmit32.And,      OpCodeT32AluRsImm.Create);
            SetT32("11110x00000<xxxx0xxx<<<<xxxxxxxx", InstName.And,      InstEmit32.And,      OpCodeT32AluImm.Create);
            SetT32("11110x<<<xxxxxxx10x0xxxxxxxxxxxx", InstName.B,        InstEmit32.B,        OpCodeT32BImm20.Create);
            SetT32("11110xxxxxxxxxxx10x1xxxxxxxxxxxx", InstName.B,        InstEmit32.B,        OpCodeT32BImm24.Create);
            SetT32("11110011011011110xxxxxxxxx0xxxxx", InstName.Bfc,      InstEmit32.Bfc,      OpCodeT32AluBf.Create);
            SetT32("111100110110<<<<0xxxxxxxxx0xxxxx", InstName.Bfi,      InstEmit32.Bfi,      OpCodeT32AluBf.Create);
            SetT32("11101010001xxxxx0xxxxxxxxxxxxxxx", InstName.Bic,      InstEmit32.Bic,      OpCodeT32AluRsImm.Create);
            SetT32("11110x00001xxxxx0xxxxxxxxxxxxxxx", InstName.Bic,      InstEmit32.Bic,      OpCodeT32AluImm.Create);
            SetT32("11110xxxxxxxxxxx11x1xxxxxxxxxxxx", InstName.Bl,       InstEmit32.Bl,       OpCodeT32BImm24.Create);
            SetT32("11110xxxxxxxxxxx11x0xxxxxxxxxxx0", InstName.Blx,      InstEmit32.Blx,      OpCodeT32BImm24.Create);
            SetT32("111110101011xxxx1111xxxx1000xxxx", InstName.Clz,      InstEmit32.Clz,      OpCodeT32AluReg.Create);
            SetT32("111010110001xxxx0xxx1111xxxxxxxx", InstName.Cmn,      InstEmit32.Cmn,      OpCodeT32AluRsImm.Create);
            SetT32("11110x010001xxxx0xxx1111xxxxxxxx", InstName.Cmn,      InstEmit32.Cmn,      OpCodeT32AluImm.Create);
            SetT32("111010111011xxxx0xxx1111xxxxxxxx", InstName.Cmp,      InstEmit32.Cmp,      OpCodeT32AluRsImm.Create);
            SetT32("11110x011011xxxx0xxx1111xxxxxxxx", InstName.Cmp,      InstEmit32.Cmp,      OpCodeT32AluImm.Create);
            SetT32("11110011101011111000000000010100", InstName.Csdb,     InstEmit32.Csdb,     OpCodeT32.Create);
            SetT32("11101010100<xxxx0xxx<<<<xxxxxxxx", InstName.Eor,      InstEmit32.Eor,      OpCodeT32AluRsImm.Create);
            SetT32("11110x00100<xxxx0xxx<<<<xxxxxxxx", InstName.Eor,      InstEmit32.Eor,      OpCodeT32AluImm.Create);
            SetT32("11110011101011111000000000010000", InstName.Esb,      InstEmit32.Nop,      OpCodeT32.Create); // Error Synchronization Barrier (FEAT_RAS)
            SetT32("1111001110101111100000000000011x", InstName.Hint,     InstEmit32.Nop,      OpCodeT32.Create); // Reserved Hint
            SetT32("11110011101011111000000000001xxx", InstName.Hint,     InstEmit32.Nop,      OpCodeT32.Create); // Reserved Hint
            SetT32("11110011101011111000000000010001", InstName.Hint,     InstEmit32.Nop,      OpCodeT32.Create); // Reserved Hint
            SetT32("11110011101011111000000000010011", InstName.Hint,     InstEmit32.Nop,      OpCodeT32.Create); // Reserved Hint
            SetT32("11110011101011111000000000010101", InstName.Hint,     InstEmit32.Nop,      OpCodeT32.Create); // Reserved Hint
            SetT32("1111001110101111100000000001011x", InstName.Hint,     InstEmit32.Nop,      OpCodeT32.Create); // Reserved Hint
            SetT32("11110011101011111000000000011xxx", InstName.Hint,     InstEmit32.Nop,      OpCodeT32.Create); // Reserved Hint
            SetT32("111100111010111110000000001xxxxx", InstName.Hint,     InstEmit32.Nop,      OpCodeT32.Create); // Reserved Hint
            SetT32("11110011101011111000000001xxxxxx", InstName.Hint,     InstEmit32.Nop,      OpCodeT32.Create); // Reserved Hint
            SetT32("1111001110101111100000001xxxxxxx", InstName.Hint,     InstEmit32.Nop,      OpCodeT32.Create); // Reserved Hint
            SetT32("111010001101xxxxxxxx111110101111", InstName.Lda,      InstEmit32.Lda,      OpCodeT32MemLdEx.Create);
            SetT32("111010001101xxxxxxxx111110001111", InstName.Ldab,     InstEmit32.Ldab,     OpCodeT32MemLdEx.Create);
            SetT32("111010001101xxxxxxxx111111101111", InstName.Ldaex,    InstEmit32.Ldaex,    OpCodeT32MemLdEx.Create);
            SetT32("111010001101xxxxxxxx111111001111", InstName.Ldaexb,   InstEmit32.Ldaexb,   OpCodeT32MemLdEx.Create);
            SetT32("111010001101xxxxxxxxxxxx11111111", InstName.Ldaexd,   InstEmit32.Ldaexd,   OpCodeT32MemLdEx.Create);
            SetT32("111010001101xxxxxxxx111111011111", InstName.Ldaexh,   InstEmit32.Ldaexh,   OpCodeT32MemLdEx.Create);
            SetT32("111010001101xxxxxxxx111110011111", InstName.Ldah,     InstEmit32.Ldah,     OpCodeT32MemLdEx.Create);
            SetT32("1110100010x1xxxxxxxxxxxxxxxxxxxx", InstName.Ldm,      InstEmit32.Ldm,      OpCodeT32MemMult.Create);
            SetT32("1110100100x1xxxxxxxxxxxxxxxxxxxx", InstName.Ldm,      InstEmit32.Ldm,      OpCodeT32MemMult.Create);
            SetT32("111110000101xxxxxxxx10x1xxxxxxxx", InstName.Ldr,      InstEmit32.Ldr,      OpCodeT32MemImm8.Create);
            SetT32("111110000101xxxxxxxx1100xxxxxxxx", InstName.Ldr,      InstEmit32.Ldr,      OpCodeT32MemImm8.Create);
            SetT32("111110000101xxxxxxxx11x1xxxxxxxx", InstName.Ldr,      InstEmit32.Ldr,      OpCodeT32MemImm8.Create);
            SetT32("111110001101xxxxxxxxxxxxxxxxxxxx", InstName.Ldr,      InstEmit32.Ldr,      OpCodeT32MemImm12.Create);
            SetT32("111110000101<<<<xxxx000000xxxxxx", InstName.Ldr,      InstEmit32.Ldr,      OpCodeT32MemRsImm.Create);
            SetT32("111110000001xxxxxxxx10x1xxxxxxxx", InstName.Ldrb,     InstEmit32.Ldrb,     OpCodeT32MemImm8.Create);
            SetT32("111110000001xxxx<<<<1100xxxxxxxx", InstName.Ldrb,     InstEmit32.Ldrb,     OpCodeT32MemImm8.Create);
            SetT32("111110000001xxxxxxxx11x1xxxxxxxx", InstName.Ldrb,     InstEmit32.Ldrb,     OpCodeT32MemImm8.Create);
            SetT32("111110001001xxxx<<<<xxxxxxxxxxxx", InstName.Ldrb,     InstEmit32.Ldrb,     OpCodeT32MemImm12.Create);
            SetT32("111110000001xxxx<<<<000000xxxxxx", InstName.Ldrb,     InstEmit32.Ldrb,     OpCodeT32MemRsImm.Create);
            SetT32("11101000x111<<<<xxxxxxxxxxxxxxxx", InstName.Ldrd,     InstEmit32.Ldrd,     OpCodeT32MemImm8D.Create);
            SetT32("11101001x1x1<<<<xxxxxxxxxxxxxxxx", InstName.Ldrd,     InstEmit32.Ldrd,     OpCodeT32MemImm8D.Create);
            SetT32("111110000011xxxxxxxx10x1xxxxxxxx", InstName.Ldrh,     InstEmit32.Ldrh,     OpCodeT32MemImm8.Create);
            SetT32("111110000011xxxx<<<<1100xxxxxxxx", InstName.Ldrh,     InstEmit32.Ldrh,     OpCodeT32MemImm8.Create);
            SetT32("111110000011xxxxxxxx11x1xxxxxxxx", InstName.Ldrh,     InstEmit32.Ldrh,     OpCodeT32MemImm8.Create);
            SetT32("111110001011xxxx<<<<xxxxxxxxxxxx", InstName.Ldrh,     InstEmit32.Ldrh,     OpCodeT32MemImm12.Create);
            SetT32("111110000011xxxx<<<<000000xxxxxx", InstName.Ldrh,     InstEmit32.Ldrh,     OpCodeT32MemRsImm.Create);
            SetT32("111110010001xxxxxxxx10x1xxxxxxxx", InstName.Ldrsb,    InstEmit32.Ldrsb,    OpCodeT32MemImm8.Create);
            SetT32("111110010001xxxx<<<<1100xxxxxxxx", InstName.Ldrsb,    InstEmit32.Ldrsb,    OpCodeT32MemImm8.Create);
            SetT32("111110010001xxxxxxxx11x1xxxxxxxx", InstName.Ldrsb,    InstEmit32.Ldrsb,    OpCodeT32MemImm8.Create);
            SetT32("111110011001xxxx<<<<xxxxxxxxxxxx", InstName.Ldrsb,    InstEmit32.Ldrsb,    OpCodeT32MemImm12.Create);
            SetT32("111110010001xxxx<<<<000000xxxxxx", InstName.Ldrsb,    InstEmit32.Ldrsb,    OpCodeT32MemRsImm.Create);
            SetT32("111110010011xxxxxxxx10x1xxxxxxxx", InstName.Ldrsh,    InstEmit32.Ldrsh,    OpCodeT32MemImm8.Create);
            SetT32("111110010011xxxx<<<<1100xxxxxxxx", InstName.Ldrsh,    InstEmit32.Ldrsh,    OpCodeT32MemImm8.Create);
            SetT32("111110010011xxxxxxxx11x1xxxxxxxx", InstName.Ldrsh,    InstEmit32.Ldrsh,    OpCodeT32MemImm8.Create);
            SetT32("111110011011xxxx<<<<xxxxxxxxxxxx", InstName.Ldrsh,    InstEmit32.Ldrsh,    OpCodeT32MemImm12.Create);
            SetT32("111110010011xxxx<<<<000000xxxxxx", InstName.Ldrsh,    InstEmit32.Ldrsh,    OpCodeT32MemRsImm.Create);
            SetT32("111110110000xxxx<<<<xxxx0000xxxx", InstName.Mla,      InstEmit32.Mla,      OpCodeT32AluMla.Create);
            SetT32("111110110000xxxxxxxxxxxx0001xxxx", InstName.Mls,      InstEmit32.Mls,      OpCodeT32AluMla.Create);
            SetT32("11101010010x11110xxxxxxxxxxxxxxx", InstName.Mov,      InstEmit32.Mov,      OpCodeT32AluRsImm.Create);
            SetT32("111110100xxxxxxx1111xxxx0000xxxx", InstName.Mov,      InstEmit32.Mov,      OpCodeT32ShiftReg.Create);
            SetT32("11110x00010x11110xxxxxxxxxxxxxxx", InstName.Mov,      InstEmit32.Mov,      OpCodeT32AluImm.Create);
            SetT32("11110x100100xxxx0xxxxxxxxxxxxxxx", InstName.Mov,      InstEmit32.Mov,      OpCodeT32MovImm16.Create);
            SetT32("11110x101100xxxx0xxxxxxxxxxxxxxx", InstName.Movt,     InstEmit32.Movt,     OpCodeT32MovImm16.Create);
            SetT32("111110110000xxxx1111xxxx0000xxxx", InstName.Mul,      InstEmit32.Mul,      OpCodeT32AluMla.Create);
            SetT32("11101010011x11110xxxxxxxxxxxxxxx", InstName.Mvn,      InstEmit32.Mvn,      OpCodeT32AluRsImm.Create);
            SetT32("11110x00011x11110xxxxxxxxxxxxxxx", InstName.Mvn,      InstEmit32.Mvn,      OpCodeT32AluImm.Create);
            SetT32("11110011101011111000000000000000", InstName.Nop,      InstEmit32.Nop,      OpCodeT32.Create);
            SetT32("11101010011x<<<<0xxxxxxxxxxxxxxx", InstName.Orn,      InstEmit32.Orn,      OpCodeT32AluRsImm.Create);
            SetT32("11110x00011x<<<<0xxxxxxxxxxxxxxx", InstName.Orn,      InstEmit32.Orn,      OpCodeT32AluImm.Create);
            SetT32("11101010010x<<<<0xxxxxxxxxxxxxxx", InstName.Orr,      InstEmit32.Orr,      OpCodeT32AluRsImm.Create);
            SetT32("11110x00010x<<<<0xxxxxxxxxxxxxxx", InstName.Orr,      InstEmit32.Orr,      OpCodeT32AluImm.Create);
            SetT32("1111100010x1xxxx1111xxxxxxxxxxxx", InstName.Pld,      InstEmit32.Nop,      OpCodeT32.Create);
            SetT32("1111100000x1xxxx11111100xxxxxxxx", InstName.Pld,      InstEmit32.Nop,      OpCodeT32.Create);
            SetT32("1111100000x1xxxx1111000000xxxxxx", InstName.Pld,      InstEmit32.Nop,      OpCodeT32.Create);
            SetT32("11101011110xxxxx0xxxxxxxxxxxxxxx", InstName.Rsb,      InstEmit32.Rsb,      OpCodeT32AluRsImm.Create);
            SetT32("11110x01110xxxxx0xxxxxxxxxxxxxxx", InstName.Rsb,      InstEmit32.Rsb,      OpCodeT32AluImm.Create);
            SetT32("111110101000xxxx1111xxxx0000xxxx", InstName.Sadd8,    InstEmit32.Sadd8,    OpCodeT32AluReg.Create);
            SetT32("11101011011xxxxx0xxxxxxxxxxxxxxx", InstName.Sbc,      InstEmit32.Sbc,      OpCodeT32AluRsImm.Create);
            SetT32("11110x01011xxxxx0xxxxxxxxxxxxxxx", InstName.Sbc,      InstEmit32.Sbc,      OpCodeT32AluImm.Create);
            SetT32("111100110100xxxx0xxxxxxxxx0xxxxx", InstName.Sbfx,     InstEmit32.Sbfx,     OpCodeT32AluBf.Create);
            SetT32("111110111001xxxx1111xxxx1111xxxx", InstName.Sdiv,     InstEmit32.Sdiv,     OpCodeT32AluMla.Create);
            SetT32("111110101010xxxx1111xxxx1000xxxx", InstName.Sel,      InstEmit32.Sel,      OpCodeT32AluReg.Create);
            SetT32("111110101000xxxx1111xxxx0010xxxx", InstName.Shadd8,   InstEmit32.Shadd8,   OpCodeT32AluReg.Create);
            SetT32("111110101100xxxx1111xxxx0010xxxx", InstName.Shsub8,   InstEmit32.Shsub8,   OpCodeT32AluReg.Create);
            SetT32("11110011101011111000000000000100", InstName.Sev,      InstEmit32.Nop,      OpCodeT32.Create);
            SetT32("11110011101011111000000000000101", InstName.Sevl,     InstEmit32.Nop,      OpCodeT32.Create);
            SetT32("111110110001xxxx<<<<xxxx00xxxxxx", InstName.Smla__,   InstEmit32.Smla__,   OpCodeT32AluMla.Create);
            SetT32("111110111100xxxxxxxxxxxx0000xxxx", InstName.Smlal,    InstEmit32.Smlal,    OpCodeT32AluUmull.Create);
            SetT32("111110111100xxxxxxxxxxxx10xxxxxx", InstName.Smlal__,  InstEmit32.Smlal__,  OpCodeT32AluUmull.Create);
            SetT32("111110110011xxxx<<<<xxxx000xxxxx", InstName.Smlaw_,   InstEmit32.Smlaw_,   OpCodeT32AluMla.Create);
            SetT32("111110110101xxxx<<<<xxxx000xxxxx", InstName.Smmla,    InstEmit32.Smmla,    OpCodeT32AluMla.Create);
            SetT32("111110110110xxxxxxxxxxxx000xxxxx", InstName.Smmls,    InstEmit32.Smmls,    OpCodeT32AluMla.Create);
            SetT32("111110110001xxxx1111xxxx00xxxxxx", InstName.Smul__,   InstEmit32.Smul__,   OpCodeT32AluMla.Create);
            SetT32("111110111000xxxxxxxxxxxx0000xxxx", InstName.Smull,    InstEmit32.Smull,    OpCodeT32AluUmull.Create);
            SetT32("111110110011xxxx1111xxxx000xxxxx", InstName.Smulw_,   InstEmit32.Smulw_,   OpCodeT32AluMla.Create);
            SetT32("111110101100xxxx1111xxxx0000xxxx", InstName.Ssub8,    InstEmit32.Ssub8,    OpCodeT32AluReg.Create);
            SetT32("111010001100xxxxxxxx111110101111", InstName.Stl,      InstEmit32.Stl,      OpCodeT32MemStEx.Create);
            SetT32("111010001100xxxxxxxx111110001111", InstName.Stlb,     InstEmit32.Stlb,     OpCodeT32MemStEx.Create);
            SetT32("111010001100xxxxxxxx11111110xxxx", InstName.Stlex,    InstEmit32.Stlex,    OpCodeT32MemStEx.Create);
            SetT32("111010001100xxxxxxxx11111100xxxx", InstName.Stlexb,   InstEmit32.Stlexb,   OpCodeT32MemStEx.Create);
            SetT32("111010001100xxxxxxxxxxxx1111xxxx", InstName.Stlexd,   InstEmit32.Stlexd,   OpCodeT32MemStEx.Create);
            SetT32("111010001100xxxxxxxx11111101xxxx", InstName.Stlexh,   InstEmit32.Stlexh,   OpCodeT32MemStEx.Create);
            SetT32("111010001100xxxxxxxx111110011111", InstName.Stlh,     InstEmit32.Stlh,     OpCodeT32MemStEx.Create);
            SetT32("1110100010x0xxxx0xxxxxxxxxxxxxxx", InstName.Stm,      InstEmit32.Stm,      OpCodeT32MemMult.Create);
            SetT32("1110100100x0xxxx0xxxxxxxxxxxxxxx", InstName.Stm,      InstEmit32.Stm,      OpCodeT32MemMult.Create);
            SetT32("111110000100<<<<xxxx10x1xxxxxxxx", InstName.Str,      InstEmit32.Str,      OpCodeT32MemImm8.Create);
            SetT32("111110000100<<<<xxxx1100xxxxxxxx", InstName.Str,      InstEmit32.Str,      OpCodeT32MemImm8.Create);
            SetT32("111110000100<<<<xxxx11x1xxxxxxxx", InstName.Str,      InstEmit32.Str,      OpCodeT32MemImm8.Create);
            SetT32("111110001100<<<<xxxxxxxxxxxxxxxx", InstName.Str,      InstEmit32.Str,      OpCodeT32MemImm12.Create);
            SetT32("111110000100<<<<xxxx000000xxxxxx", InstName.Str,      InstEmit32.Str,      OpCodeT32MemRsImm.Create);
            SetT32("111110000000<<<<xxxx10x1xxxxxxxx", InstName.Strb,     InstEmit32.Strb,     OpCodeT32MemImm8.Create);
            SetT32("111110000000<<<<xxxx1100xxxxxxxx", InstName.Strb,     InstEmit32.Strb,     OpCodeT32MemImm8.Create);
            SetT32("111110000000<<<<xxxx11x1xxxxxxxx", InstName.Strb,     InstEmit32.Strb,     OpCodeT32MemImm8.Create);
            SetT32("111110001000<<<<xxxxxxxxxxxxxxxx", InstName.Strb,     InstEmit32.Strb,     OpCodeT32MemImm12.Create);
            SetT32("111110000000<<<<xxxx000000xxxxxx", InstName.Strb,     InstEmit32.Strb,     OpCodeT32MemRsImm.Create);
            SetT32("11101000x110<<<<xxxxxxxxxxxxxxxx", InstName.Strd,     InstEmit32.Strd,     OpCodeT32MemImm8D.Create);
            SetT32("11101001x1x0<<<<xxxxxxxxxxxxxxxx", InstName.Strd,     InstEmit32.Strd,     OpCodeT32MemImm8D.Create);
            SetT32("111110000010<<<<xxxx10x1xxxxxxxx", InstName.Strh,     InstEmit32.Strh,     OpCodeT32MemImm8.Create);
            SetT32("111110000010<<<<xxxx1100xxxxxxxx", InstName.Strh,     InstEmit32.Strh,     OpCodeT32MemImm8.Create);
            SetT32("111110000010<<<<xxxx11x1xxxxxxxx", InstName.Strh,     InstEmit32.Strh,     OpCodeT32MemImm8.Create);
            SetT32("111110001010<<<<xxxxxxxxxxxxxxxx", InstName.Strh,     InstEmit32.Strh,     OpCodeT32MemImm12.Create);
            SetT32("111110000010<<<<xxxx000000xxxxxx", InstName.Strh,     InstEmit32.Strh,     OpCodeT32MemRsImm.Create);
            SetT32("11101011101<xxxx0xxx<<<<xxxxxxxx", InstName.Sub,      InstEmit32.Sub,      OpCodeT32AluRsImm.Create);
            SetT32("11110x01101<xxxx0xxx<<<<xxxxxxxx", InstName.Sub,      InstEmit32.Sub,      OpCodeT32AluImm.Create);
            SetT32("11110x101010xxxx0xxxxxxxxxxxxxxx", InstName.Sub,      InstEmit32.Sub,      OpCodeT32AluImm12.Create);
            SetT32("111110100100xxxx1111xxxx10xxxxxx", InstName.Sxtb,     InstEmit32.Sxtb,     OpCodeT32AluUx.Create);
            SetT32("111110100010xxxx1111xxxx10xxxxxx", InstName.Sxtb16,   InstEmit32.Sxtb16,   OpCodeT32AluUx.Create);
            SetT32("111110100000xxxx1111xxxx10xxxxxx", InstName.Sxth,     InstEmit32.Sxth,     OpCodeT32AluUx.Create);
            SetT32("111010001101xxxx111100000000xxxx", InstName.Tbb,      InstEmit32.Tbb,      OpCodeT32Tb.Create);
            SetT32("111010001101xxxx111100000001xxxx", InstName.Tbh,      InstEmit32.Tbh,      OpCodeT32Tb.Create);
            SetT32("111010101001xxxx0xxx1111xxxxxxxx", InstName.Teq,      InstEmit32.Teq,      OpCodeT32AluRsImm.Create);
            SetT32("11110x001001xxxx0xxx1111xxxxxxxx", InstName.Teq,      InstEmit32.Teq,      OpCodeT32AluImm.Create);
            SetT32("11110011101011111000000000010010", InstName.Tsb,      InstEmit32.Nop,      OpCodeT32.Create); // Trace Synchronization Barrier (FEAT_TRF)
            SetT32("111010100001xxxx0xxx1111xxxxxxxx", InstName.Tst,      InstEmit32.Tst,      OpCodeT32AluRsImm.Create);
            SetT32("11110x000001xxxx0xxx1111xxxxxxxx", InstName.Tst,      InstEmit32.Tst,      OpCodeT32AluImm.Create);
            SetT32("111110101000xxxx1111xxxx0100xxxx", InstName.Uadd8,    InstEmit32.Uadd8,    OpCodeT32AluReg.Create);
            SetT32("111100111100xxxx0xxxxxxxxx0xxxxx", InstName.Ubfx,     InstEmit32.Ubfx,     OpCodeT32AluBf.Create);
            SetT32("111110111011xxxx1111xxxx1111xxxx", InstName.Udiv,     InstEmit32.Udiv,     OpCodeT32AluMla.Create);
            SetT32("111110101000xxxx1111xxxx0110xxxx", InstName.Uhadd8,   InstEmit32.Uhadd8,   OpCodeT32AluReg.Create);
            SetT32("111110101100xxxx1111xxxx0110xxxx", InstName.Uhsub8,   InstEmit32.Uhsub8,   OpCodeT32AluReg.Create);
            SetT32("111110111110xxxxxxxxxxxx0110xxxx", InstName.Umaal,    InstEmit32.Umaal,    OpCodeT32AluUmull.Create);
            SetT32("111110111110xxxxxxxxxxxx0000xxxx", InstName.Umlal,    InstEmit32.Umlal,    OpCodeT32AluUmull.Create);
            SetT32("111110111010xxxxxxxxxxxx0000xxxx", InstName.Umull,    InstEmit32.Umull,    OpCodeT32AluUmull.Create);
            SetT32("111110101100xxxx1111xxxx0100xxxx", InstName.Usub8,    InstEmit32.Usub8,    OpCodeT32AluReg.Create);
            SetT32("111110100101xxxx1111xxxx10xxxxxx", InstName.Uxtb,     InstEmit32.Uxtb,     OpCodeT32AluUx.Create);
            SetT32("111110100011xxxx1111xxxx10xxxxxx", InstName.Uxtb16,   InstEmit32.Uxtb16,   OpCodeT32AluUx.Create);
            SetT32("111110100001xxxx1111xxxx10xxxxxx", InstName.Uxth,     InstEmit32.Uxth,     OpCodeT32AluUx.Create);
            SetT32("11110011101011111000000000000010", InstName.Wfe,      InstEmit32.Nop,      OpCodeT32.Create);
            SetT32("11110011101011111000000000000011", InstName.Wfi,      InstEmit32.Nop,      OpCodeT32.Create);
            SetT32("11110011101011111000000000000001", InstName.Yield,    InstEmit32.Nop,      OpCodeT32.Create);
            #endregion

            FillFastLookupTable(_instA32FastLookup, _allInstA32, ToFastLookupIndexA);
            FillFastLookupTable(_instT32FastLookup, _allInstT32, ToFastLookupIndexT);
            FillFastLookupTable(_instA64FastLookup, _allInstA64, ToFastLookupIndexA);
#pragma warning restore IDE0055
        }

        private static void FillFastLookupTable(InstInfo[][] table, List<InstInfo> allInsts, Func<int, int> toFastLookupIndex)
        {
            List<InstInfo>[] temp = new List<InstInfo>[FastLookupSize];

            for (int index = 0; index < temp.Length; index++)
            {
                temp[index] = [];
            }

            foreach (InstInfo inst in allInsts)
            {
                int mask = toFastLookupIndex(inst.Mask);
                int value = toFastLookupIndex(inst.Value);

                for (int index = 0; index < temp.Length; index++)
                {
                    if ((index & mask) == value)
                    {
                        temp[index].Add(inst);
                    }
                }
            }

            for (int index = 0; index < temp.Length; index++)
            {
                table[index] = temp[index].ToArray();
            }
        }

        private static void SetA32(string encoding, InstName name, InstEmitter emitter, MakeOp makeOp)
        {
            Set(encoding, _allInstA32, new InstDescriptor(name, emitter), makeOp);
        }

        private static void SetT16(string encoding, InstName name, InstEmitter emitter, MakeOp makeOp)
        {
            encoding = "xxxxxxxxxxxxxxxx" + encoding;
            Set(encoding, _allInstT32, new InstDescriptor(name, emitter), makeOp);
        }

        private static void SetT32(string encoding, InstName name, InstEmitter emitter, MakeOp makeOp)
        {
            string reversedEncoding = $"{encoding.AsSpan(16)}{encoding.AsSpan(0, 16)}";
            OpCode ReversedMakeOp(InstDescriptor inst, ulong address, int opCode)
                    => makeOp(inst, address, (int)BitOperations.RotateRight((uint)opCode, 16));
            Set(reversedEncoding, _allInstT32, new InstDescriptor(name, emitter), ReversedMakeOp);
        }

        private static void SetVfp(string encoding, InstName name, InstEmitter emitter, MakeOp makeOpA32, MakeOp makeOpT32)
        {
            SetA32(encoding, name, emitter, makeOpA32);

            string thumbEncoding = encoding;
            if (thumbEncoding.StartsWith("<<<<"))
            {
                thumbEncoding = $"1110{thumbEncoding.AsSpan(4)}";
            }
            SetT32(thumbEncoding, name, emitter, makeOpT32);
        }

        private static void SetAsimd(string encoding, InstName name, InstEmitter emitter, MakeOp makeOpA32, MakeOp makeOpT32)
        {
            SetA32(encoding, name, emitter, makeOpA32);

            string thumbEncoding = encoding;
            if (thumbEncoding.StartsWith("11110100"))
            {
                thumbEncoding = $"11111001{encoding.AsSpan(8)}";
            }
            else if (thumbEncoding.StartsWith("1111001x"))
            {
                thumbEncoding = $"111x1111{encoding.AsSpan(8)}";
            }
            else if (thumbEncoding.StartsWith("11110010"))
            {
                thumbEncoding = $"11101111{encoding.AsSpan(8)}";
            }
            else if (thumbEncoding.StartsWith("11110011"))
            {
                thumbEncoding = $"11111111{encoding.AsSpan(8)}";
            }
            else
            {
                throw new ArgumentException("Invalid ASIMD instruction encoding");
            }
            SetT32(thumbEncoding, name, emitter, makeOpT32);
        }

        private static void SetA64(string encoding, InstName name, InstEmitter emitter, MakeOp makeOp)
        {
            Set(encoding, _allInstA64, new InstDescriptor(name, emitter), makeOp);
        }

        private static void Set(string encoding, List<InstInfo> list, InstDescriptor inst, MakeOp makeOp)
        {
            int bit = encoding.Length - 1;
            int value = 0;
            int xMask = 0;
            int xBits = 0;

            int[] xPos = new int[encoding.Length];

            int blacklisted = 0;

            for (int index = 0; index < encoding.Length; index++, bit--)
            {
                // Note: < and > are used on special encodings.
                // The < means that we should never have ALL bits with the '<' set.
                // So, when the encoding has <<, it means that 00, 01, and 10 are valid,
                // but not 11. <<< is 000, 001, ..., 110 but NOT 111, and so on...
                // For >, the invalid value is zero. So, for >> 01, 10 and 11 are valid,
                // but 00 isn't.
                char chr = encoding[index];

                if (chr == '1')
                {
                    value |= 1 << bit;
                }
                else if (chr == 'x')
                {
                    xMask |= 1 << bit;
                }
                else if (chr == '>')
                {
                    xPos[xBits++] = bit;
                }
                else if (chr == '<')
                {
                    xPos[xBits++] = bit;

                    blacklisted |= 1 << bit;
                }
                else if (chr != '0')
                {
                    throw new ArgumentException($"Invalid encoding: {encoding}", nameof(encoding));
                }
            }

            xMask = ~xMask;

            if (xBits == 0)
            {
                list.Add(new InstInfo(xMask, value, inst, makeOp));

                return;
            }

            for (int index = 0; index < (1 << xBits); index++)
            {
                int mask = 0;

                for (int x = 0; x < xBits; x++)
                {
                    mask |= ((index >> x) & 1) << xPos[x];
                }

                if (mask != blacklisted)
                {
                    list.Add(new InstInfo(xMask, value | mask, inst, makeOp));
                }
            }
        }

        public static (InstDescriptor inst, MakeOp makeOp) GetInstA32(int opCode)
        {
            return GetInstFromList(_instA32FastLookup[ToFastLookupIndexA(opCode)], opCode);
        }

        public static (InstDescriptor inst, MakeOp makeOp) GetInstT32(int opCode)
        {
            return GetInstFromList(_instT32FastLookup[ToFastLookupIndexT(opCode)], opCode);
        }

        public static (InstDescriptor inst, MakeOp makeOp) GetInstA64(int opCode)
        {
            return GetInstFromList(_instA64FastLookup[ToFastLookupIndexA(opCode)], opCode);
        }

        private static (InstDescriptor inst, MakeOp makeOp) GetInstFromList(InstInfo[] insts, int opCode)
        {
            foreach (InstInfo info in insts)
            {
                if ((opCode & info.Mask) == info.Value)
                {
                    return (info.Inst, info.MakeOp);
                }
            }

            return (new InstDescriptor(InstName.Und, InstEmit.Und), null);
        }

        private static int ToFastLookupIndexA(int value)
        {
            return ((value >> 10) & 0x00F) | ((value >> 18) & 0xFF0);
        }

        private static int ToFastLookupIndexT(int value)
        {
            return (value >> 4) & 0xFFF;
        }
    }
}
