using Ryujinx.Cpu.LightningJit.Table;
using System.Collections.Generic;

namespace Ryujinx.Cpu.LightningJit.Arm64.Target.Arm64
{
    static class InstTable
    {
        private static readonly InstTableLevel<InstInfo> _table;

        private readonly struct InstInfo : IInstInfo
        {
            public uint Encoding { get; }
            public uint EncodingMask { get; }
            public InstEncoding[] Constraints { get; }
            public InstName Name { get; }
            public IsaVersion Version { get; }
            public IsaFeature Feature { get; }
            public InstFlags Flags { get; }
            public AddressForm AddressForm { get; }

            public InstInfo(
                uint encoding,
                uint encodingMask,
                InstEncoding[] constraints,
                InstName name,
                IsaVersion isaVersion,
                IsaFeature isaFeature,
                InstFlags flags,
                AddressForm addressForm = AddressForm.None)
            {
                if (addressForm != AddressForm.None)
                {
                    flags |= InstFlags.Memory;
                }

                Encoding = encoding;
                EncodingMask = encodingMask;
                Constraints = constraints;
                Name = name;
                Version = isaVersion;
                Feature = isaFeature;
                Flags = flags;
                AddressForm = addressForm;
            }

            public InstInfo(
                uint encoding,
                uint encodingMask,
                InstEncoding[] constraints,
                InstName name,
                IsaVersion isaVersion,
                InstFlags flags,
                AddressForm addressForm = AddressForm.None) : this(encoding, encodingMask, constraints, name, isaVersion, IsaFeature.None, flags, addressForm)
            {
            }

            public InstInfo(
                uint encoding,
                uint encodingMask,
                InstName name,
                IsaVersion isaVersion,
                IsaFeature isaFeature,
                InstFlags flags,
                AddressForm addressForm = AddressForm.None) : this(encoding, encodingMask, null, name, isaVersion, isaFeature, flags, addressForm)
            {
            }

            public InstInfo(
                uint encoding,
                uint encodingMask,
                InstName name,
                IsaVersion isaVersion,
                InstFlags flags,
                AddressForm addressForm = AddressForm.None) : this(encoding, encodingMask, null, name, isaVersion, IsaFeature.None, flags, addressForm)
            {
            }

            public bool IsConstrained(uint encoding)
            {
                if (Constraints != null)
                {
                    foreach (InstEncoding constraint in Constraints)
                    {
                        if ((encoding & constraint.EncodingMask) == constraint.Encoding)
                        {
                            return true;
                        }
                    }
                }

                return false;
            }
        }

        static InstTable()
        {
            InstEncoding[] qsizeConstraints =
            [
                new(0x00C00000, 0x40C00000)
            ];

            InstEncoding[] sizeConstraints =
            [
                new(0x00C00000, 0x00C00000)
            ];

            InstEncoding[] opuOpuOpuConstraints =
            [
                new(0x00001400, 0x00001C00),
                new(0x00001800, 0x00001C00),
                new(0x00001C00, 0x00001C00)
            ];

            InstEncoding[] shiftSfimm6Constraints =
            [
                new(0x00C00000, 0x00C00000),
                new(0x00008000, 0x80008000)
            ];

            InstEncoding[] qsizeSizeConstraints =
            [
                new(0x00800000, 0x40C00000),
                new(0x00C00000, 0x00C00000)
            ];

            InstEncoding[] nimmsNimmsNimmsNimmsNimmsNimmsNimmsNimmsSfnConstraints =
            [
                new(0x0040FC00, 0x0040FC00),
                new(0x00007C00, 0x0040FC00),
                new(0x0000BC00, 0x0040FC00),
                new(0x0000DC00, 0x0040FC00),
                new(0x0000EC00, 0x0040FC00),
                new(0x0000F400, 0x0040FC00),
                new(0x0000F800, 0x0040FC00),
                new(0x0000FC00, 0x0040FC00),
                new(0x00400000, 0x80400000)
            ];

            InstEncoding[] sfimm6Constraints =
            [
                new(0x00008000, 0x80008000)
            ];

            InstEncoding[] sfnSfnSfimmr5Sfimms5Constraints =
            [
                new(0x80000000, 0x80400000),
                new(0x00400000, 0x80400000),
                new(0x00200000, 0x80200000),
                new(0x00008000, 0x80008000)
            ];

            InstEncoding[] cmodeopqConstraints =
            [
                new(0x2000F000, 0x6000F000)
            ];

            InstEncoding[] rsRtConstraints =
            [
                new(0x00010000, 0x00010000),
                new(0x00000001, 0x00000001)
            ];

            InstEncoding[] sfszSfszSfszSfszConstraints =
            [
                new(0x80000000, 0x80000C00),
                new(0x80000400, 0x80000C00),
                new(0x80000800, 0x80000C00),
                new(0x00000C00, 0x80000C00)
            ];

            InstEncoding[] imm5Constraints =
            [
                new(0x00000000, 0x000F0000)
            ];

            InstEncoding[] imm5Imm5qConstraints =
            [
                new(0x00000000, 0x000F0000),
                new(0x00080000, 0x400F0000)
            ];

            InstEncoding[] nsfNsfSfimmsConstraints =
            [
                new(0x00400000, 0x80400000),
                new(0x80000000, 0x80400000),
                new(0x00008000, 0x80008000)
            ];

            InstEncoding[] qimm4Constraints =
            [
                new(0x00004000, 0x40004000)
            ];

            InstEncoding[] qszConstraints =
            [
                new(0x00400000, 0x40400000)
            ];

            InstEncoding[] euacEuacEuacConstraints =
            [
                new(0x00000800, 0x20800800),
                new(0x00800000, 0x20800800),
                new(0x00800800, 0x20800800)
            ];

            InstEncoding[] qszEuacEuacEuacConstraints =
            [
                new(0x00400000, 0x40400000),
                new(0x00000800, 0x20800800),
                new(0x00800000, 0x20800800),
                new(0x00800800, 0x20800800)
            ];

            InstEncoding[] szConstraints =
            [
                new(0x00400000, 0x00400000)
            ];

            InstEncoding[] sizeQsizeConstraints =
            [
                new(0x00000000, 0x00C00000),
                new(0x00C00000, 0x40C00000)
            ];

            InstEncoding[] sizeSizeSizelSizeqSizehqConstraints =
            [
                new(0x00000000, 0x00C00000),
                new(0x00C00000, 0x00C00000),
                new(0x00A00000, 0x00E00000),
                new(0x00800000, 0x40C00000),
                new(0x00400800, 0x40C00800)
            ];

            InstEncoding[] szConstraints2 =
            [
                new(0x00000000, 0x00400000)
            ];

            InstEncoding[] immhConstraints =
            [
                new(0x00000000, 0x00780000)
            ];

            InstEncoding[] immhQimmhConstraints =
            [
                new(0x00000000, 0x00780000),
                new(0x00400000, 0x40400000)
            ];

            InstEncoding[] sfscaleConstraints =
            [
                new(0x00000000, 0x80008000)
            ];

            InstEncoding[] ftypeopcFtypeopcFtypeopcFtypeopcFtypeOpcConstraints =
            [
                new(0x00000000, 0x00C18000),
                new(0x00408000, 0x00C18000),
                new(0x00810000, 0x00C18000),
                new(0x00C18000, 0x00C18000),
                new(0x00800000, 0x00C00000),
                new(0x00010000, 0x00018000)
            ];

            InstEncoding[] szlConstraints =
            [
                new(0x00600000, 0x00600000)
            ];

            InstEncoding[] szlQszConstraints =
            [
                new(0x00600000, 0x00600000),
                new(0x00400000, 0x40400000)
            ];

            InstEncoding[] qConstraints =
            [
                new(0x00000000, 0x40000000)
            ];

            InstEncoding[] sfftypermodeSfftypermodeConstraints =
            [
                new(0x00400000, 0x80C80000),
                new(0x80000000, 0x80C80000)
            ];

            InstEncoding[] uo1o2Constraints =
            [
                new(0x20800000, 0x20801000)
            ];

            InstEncoding[] qszUo1o2Constraints =
            [
                new(0x00400000, 0x40400000),
                new(0x20800000, 0x20801000)
            ];

            InstEncoding[] sConstraints =
            [
                new(0x00001000, 0x00001000)
            ];

            InstEncoding[] opcodesizeOpcodesizeOpcodesizesOpcodesizeConstraints =
            [
                new(0x00004400, 0x0000C400),
                new(0x00008800, 0x0000C800),
                new(0x00009400, 0x0000D400),
                new(0x0000C000, 0x0000C000)
            ];

            InstEncoding[] qsizeConstraints2 =
            [
                new(0x00000C00, 0x40000C00)
            ];

            InstEncoding[] rtRtConstraints =
            [
                new(0x00000018, 0x00000018),
                new(0x00000001, 0x00000001)
            ];

            InstEncoding[] opc1sizeOpc1sizeOpc1sizeConstraints =
            [
                new(0x40800000, 0xC0800000),
                new(0x80800000, 0xC0800000),
                new(0xC0800000, 0xC0800000)
            ];

            InstEncoding[] rtRt2Constraints =
            [
                new(0x0000001F, 0x0000001F),
                new(0x001F0000, 0x001F0000)
            ];

            InstEncoding[] opcConstraints =
            [
                new(0xC0000000, 0xC0000000)
            ];

            InstEncoding[] opcConstraints2 =
            [
                new(0x40000000, 0x40000000)
            ];

            InstEncoding[] opclOpcConstraints =
            [
                new(0x40000000, 0x40400000),
                new(0xC0000000, 0xC0000000)
            ];

            InstEncoding[] optionConstraints =
            [
                new(0x00000000, 0x00004000)
            ];

            InstEncoding[] opc1sizeOpc1sizeOpc1sizeOptionConstraints =
            [
                new(0x40800000, 0xC0800000),
                new(0x80800000, 0xC0800000),
                new(0xC0800000, 0xC0800000),
                new(0x00000000, 0x00004000)
            ];

            InstEncoding[] sizeSizeConstraints =
            [
                new(0x00000000, 0x00C00000),
                new(0x00C00000, 0x00C00000)
            ];

            InstEncoding[] sfhwConstraints =
            [
                new(0x00400000, 0x80400000)
            ];

            InstEncoding[] rtConstraints =
            [
                new(0x00000001, 0x00000001)
            ];

            InstEncoding[] usizeUsizeUsizeSizeConstraints =
            [
                new(0x20400000, 0x20C00000),
                new(0x20800000, 0x20C00000),
                new(0x20C00000, 0x20C00000),
                new(0x00C00000, 0x00C00000)
            ];

            InstEncoding[] sizeSizeConstraints2 =
            [
                new(0x00400000, 0x00C00000),
                new(0x00800000, 0x00C00000)
            ];

            InstEncoding[] rtConstraints2 =
            [
                new(0x00000018, 0x00000018)
            ];

            InstEncoding[] sfopcConstraints =
            [
                new(0x00000400, 0x80000400)
            ];

            InstEncoding[] sizeSizeSizeConstraints =
            [
                new(0x00400000, 0x00C00000),
                new(0x00800000, 0x00C00000),
                new(0x00C00000, 0x00C00000)
            ];

            InstEncoding[] sizeSizeConstraints3 =
            [
                new(0x00800000, 0x00C00000),
                new(0x00C00000, 0x00C00000)
            ];

            InstEncoding[] sfConstraints =
            [
                new(0x00000000, 0x80000000)
            ];

            InstEncoding[] immhImmhConstraints =
            [
                new(0x00000000, 0x00780000),
                new(0x00400000, 0x00400000)
            ];

            InstEncoding[] sizeSizeConstraints4 =
            [
                new(0x00C00000, 0x00C00000),
                new(0x00000000, 0x00C00000)
            ];

            InstEncoding[] ssizeSsizeSsizeConstraints =
            [
                new(0x00000000, 0x00C00800),
                new(0x00400000, 0x00C00800),
                new(0x00800000, 0x00C00800)
            ];

            InstEncoding[] immhOpuConstraints =
            [
                new(0x00000000, 0x00780000),
                new(0x00000000, 0x20001000)
            ];

            InstEncoding[] immhQimmhOpuConstraints =
            [
                new(0x00000000, 0x00780000),
                new(0x00400000, 0x40400000),
                new(0x00000000, 0x20001000)
            ];

            List<InstInfo> insts = 
            [
                new(0x5AC02000, 0x7FFFFC00, InstName.Abs, IsaVersion.v89, InstFlags.RdRn),
                new(0x5EE0B800, 0xFFFFFC00, InstName.AbsAdvsimdS, IsaVersion.v80, InstFlags.RdRnFpSimd),
                new(0x0E20B800, 0xBF3FFC00, qsizeConstraints, InstName.AbsAdvsimdV, IsaVersion.v80, InstFlags.RdRnFpSimd),
                new(0x1A000000, 0x7FE0FC00, InstName.Adc, IsaVersion.v80, InstFlags.RdRnRmC),
                new(0x3A000000, 0x7FE0FC00, InstName.Adcs, IsaVersion.v80, InstFlags.RdRnRmCS),
                new(0x91800000, 0xFFC0C000, InstName.Addg, IsaVersion.v85, InstFlags.None),
                new(0x0E204000, 0xBF20FC00, sizeConstraints, InstName.AddhnAdvsimd, IsaVersion.v80, InstFlags.RdReadRdRnRmFpSimd),
                new(0x5EF1B800, 0xFFFFFC00, InstName.AddpAdvsimdPair, IsaVersion.v80, InstFlags.RdRnFpSimd),
                new(0x0E20BC00, 0xBF20FC00, qsizeConstraints, InstName.AddpAdvsimdVec, IsaVersion.v80, InstFlags.RdRnRmFpSimd),
                new(0x2B200000, 0x7FE00000, opuOpuOpuConstraints, InstName.AddsAddsubExt, IsaVersion.v80, InstFlags.RdRnSPRmS),
                new(0x31000000, 0x7F800000, InstName.AddsAddsubImm, IsaVersion.v80, InstFlags.RdRnSPS),
                new(0x2B000000, 0x7F200000, shiftSfimm6Constraints, InstName.AddsAddsubShift, IsaVersion.v80, InstFlags.RdRnRmS),
                new(0x0E31B800, 0xBF3FFC00, qsizeSizeConstraints, InstName.AddvAdvsimd, IsaVersion.v80, InstFlags.RdRnFpSimd),
                new(0x0B200000, 0x7FE00000, opuOpuOpuConstraints, InstName.AddAddsubExt, IsaVersion.v80, InstFlags.RdSPRnSPRm),
                new(0x11000000, 0x7F800000, InstName.AddAddsubImm, IsaVersion.v80, InstFlags.RdSPRnSP),
                new(0x0B000000, 0x7F200000, shiftSfimm6Constraints, InstName.AddAddsubShift, IsaVersion.v80, InstFlags.RdRnRm),
                new(0x5EE08400, 0xFFE0FC00, InstName.AddAdvsimdS, IsaVersion.v80, InstFlags.RdRnRmFpSimd),
                new(0x0E208400, 0xBF20FC00, qsizeConstraints, InstName.AddAdvsimdV, IsaVersion.v80, InstFlags.RdRnRmFpSimd),
                new(0x10000000, 0x9F000000, InstName.Adr, IsaVersion.v80, InstFlags.Rd, AddressForm.Literal),
                new(0x90000000, 0x9F000000, InstName.Adrp, IsaVersion.v80, InstFlags.Rd, AddressForm.Literal),
                new(0x4E285800, 0xFFFFFC00, InstName.AesdAdvsimd, IsaVersion.v80, InstFlags.RdRnFpSimd),
                new(0x4E284800, 0xFFFFFC00, InstName.AeseAdvsimd, IsaVersion.v80, InstFlags.RdRnFpSimd),
                new(0x4E287800, 0xFFFFFC00, InstName.AesimcAdvsimd, IsaVersion.v80, InstFlags.RdRnFpSimd),
                new(0x4E286800, 0xFFFFFC00, InstName.AesmcAdvsimd, IsaVersion.v80, InstFlags.RdRnFpSimd),
                new(0x72000000, 0x7F800000, nimmsNimmsNimmsNimmsNimmsNimmsNimmsNimmsSfnConstraints, InstName.AndsLogImm, IsaVersion.v80, InstFlags.RdRnS),
                new(0x6A000000, 0x7F200000, sfimm6Constraints, InstName.AndsLogShift, IsaVersion.v80, InstFlags.RdRnRmS),
                new(0x0E201C00, 0xBFE0FC00, InstName.AndAdvsimd, IsaVersion.v80, InstFlags.RdRnRmFpSimd),
                new(0x12000000, 0x7F800000, nimmsNimmsNimmsNimmsNimmsNimmsNimmsNimmsSfnConstraints, InstName.AndLogImm, IsaVersion.v80, InstFlags.RdSPRn),
                new(0x0A000000, 0x7F200000, sfimm6Constraints, InstName.AndLogShift, IsaVersion.v80, InstFlags.RdRnRm),
                new(0x1AC02800, 0x7FE0FC00, InstName.Asrv, IsaVersion.v80, InstFlags.RdRnRm),
                new(0xDAC11800, 0xFFFFDC00, InstName.Autda, IsaVersion.v83, InstFlags.RdRnSP),
                new(0xDAC11C00, 0xFFFFDC00, InstName.Autdb, IsaVersion.v83, InstFlags.RdRnSP),
                new(0xDAC11000, 0xFFFFDC00, InstName.AutiaGeneral, IsaVersion.v83, InstFlags.RdRnSP),
                new(0xD503219F, 0xFFFFFDDF, InstName.AutiaSystem, IsaVersion.v83, InstFlags.None),
                new(0xDAC11400, 0xFFFFDC00, InstName.AutibGeneral, IsaVersion.v83, InstFlags.RdRnSP),
                new(0xD50321DF, 0xFFFFFDDF, InstName.AutibSystem, IsaVersion.v83, InstFlags.None),
                new(0xD500405F, 0xFFFFFFFF, InstName.Axflag, IsaVersion.v85, InstFlags.C),
                new(0xCE200000, 0xFFE08000, InstName.BcaxAdvsimd, IsaVersion.v82, InstFlags.RdRnRmRaFpSimd),
                new(0x54000010, 0xFF000010, InstName.BcCond, IsaVersion.v88, InstFlags.Nzcv),
                new(0x0EA16800, 0xBFFFFC00, InstName.BfcvtnAdvsimd, IsaVersion.v86, InstFlags.RdRnFpSimd),
                new(0x1E634000, 0xFFFFFC00, InstName.BfcvtFloat, IsaVersion.v86, InstFlags.RdRnFpSimd),
                new(0x0F40F000, 0xBFC0F400, InstName.BfdotAdvsimdElt, IsaVersion.v86, InstFlags.RdReadRdRnRmFpSimd),
                new(0x2E40FC00, 0xBFE0FC00, InstName.BfdotAdvsimdVec, IsaVersion.v86, InstFlags.RdReadRdRnRmFpSimd),
                new(0x33000000, 0x7F800000, sfnSfnSfimmr5Sfimms5Constraints, InstName.Bfm, IsaVersion.v80, InstFlags.RdReadRdRn),
                new(0x0FC0F000, 0xBFC0F400, InstName.BfmlalAdvsimdElt, IsaVersion.v86, InstFlags.RdRnRmFpSimd),
                new(0x2EC0FC00, 0xBFE0FC00, InstName.BfmlalAdvsimdVec, IsaVersion.v86, InstFlags.RdRnRmFpSimd),
                new(0x6E40EC00, 0xFFE0FC00, InstName.BfmmlaAdvsimd, IsaVersion.v86, InstFlags.RdRnRmFpSimd),
                new(0x6A200000, 0x7F200000, sfimm6Constraints, InstName.Bics, IsaVersion.v80, InstFlags.RdRnRmS),
                new(0x2F001400, 0xBFF81C00, cmodeopqConstraints, InstName.BicAdvsimdImm, IsaVersion.v80, InstFlags.RdFpSimd),
                new(0x0E601C00, 0xBFE0FC00, InstName.BicAdvsimdReg, IsaVersion.v80, InstFlags.RdRnRmFpSimd),
                new(0x0A200000, 0x7F200000, sfimm6Constraints, InstName.BicLogShift, IsaVersion.v80, InstFlags.RdRnRm),
                new(0x2EE01C00, 0xBFE0FC00, InstName.BifAdvsimd, IsaVersion.v80, InstFlags.RdReadRdRnRmFpSimd),
                new(0x2EA01C00, 0xBFE0FC00, InstName.BitAdvsimd, IsaVersion.v80, InstFlags.RdReadRdRnRmFpSimd),
                new(0x94000000, 0xFC000000, InstName.Bl, IsaVersion.v80, InstFlags.None),
                new(0xD63F0000, 0xFFFFFC1F, InstName.Blr, IsaVersion.v80, InstFlags.Rn),
                new(0xD63F0800, 0xFEFFF800, InstName.Blra, IsaVersion.v83, InstFlags.RnRm),
                new(0xD61F0000, 0xFFFFFC1F, InstName.Br, IsaVersion.v80, InstFlags.Rn),
                new(0xD61F0800, 0xFEFFF800, InstName.Bra, IsaVersion.v83, InstFlags.RnRm),
                new(0xD4200000, 0xFFE0001F, InstName.Brk, IsaVersion.v80, InstFlags.None),
                new(0x2E601C00, 0xBFE0FC00, InstName.BslAdvsimd, IsaVersion.v80, InstFlags.RdReadRdRnRmFpSimd),
                new(0xD503241F, 0xFFFFFF3F, InstName.Bti, IsaVersion.v85, InstFlags.None),
                new(0x54000000, 0xFF000010, InstName.BCond, IsaVersion.v80, InstFlags.Nzcv),
                new(0x14000000, 0xFC000000, InstName.BUncond, IsaVersion.v80, InstFlags.None),
                new(0x88A07C00, 0xBFA07C00, InstName.Cas, IsaVersion.v81, InstFlags.RtReadRtRnSPRs, AddressForm.BaseRegister),
                new(0x08A07C00, 0xFFA07C00, InstName.Casb, IsaVersion.v81, InstFlags.RtReadRtRnSPRs, AddressForm.BaseRegister),
                new(0x48A07C00, 0xFFA07C00, InstName.Cash, IsaVersion.v81, InstFlags.RtReadRtRnSPRs, AddressForm.BaseRegister),
                new(0x08207C00, 0xBFA07C00, rsRtConstraints, InstName.Casp, IsaVersion.v81, InstFlags.RtReadRtRnSPRs, AddressForm.BaseRegister),
                new(0x35000000, 0x7F000000, InstName.Cbnz, IsaVersion.v80, InstFlags.RtReadRt),
                new(0x34000000, 0x7F000000, InstName.Cbz, IsaVersion.v80, InstFlags.RtReadRt),
                new(0x3A400800, 0x7FE00C10, InstName.CcmnImm, IsaVersion.v80, InstFlags.RnNzcvS),
                new(0x3A400000, 0x7FE00C10, InstName.CcmnReg, IsaVersion.v80, InstFlags.RnRmNzcvS),
                new(0x7A400800, 0x7FE00C10, InstName.CcmpImm, IsaVersion.v80, InstFlags.RnNzcvS),
                new(0x7A400000, 0x7FE00C10, InstName.CcmpReg, IsaVersion.v80, InstFlags.RnRmNzcvS),
                new(0xD500401F, 0xFFFFFFFF, InstName.Cfinv, IsaVersion.v84, InstFlags.C),
                new(0xD503251F, 0xFFFFFFFF, InstName.Chkfeat, IsaVersion.None, InstFlags.None),
                new(0xD50322DF, 0xFFFFFFFF, InstName.Clrbhb, IsaVersion.v89, InstFlags.None),
                new(0xD503305F, 0xFFFFF0FF, InstName.Clrex, IsaVersion.v80, InstFlags.None),
                new(0x0E204800, 0xBF3FFC00, sizeConstraints, InstName.ClsAdvsimd, IsaVersion.v80, InstFlags.RdRnFpSimd),
                new(0x5AC01400, 0x7FFFFC00, InstName.ClsInt, IsaVersion.v80, InstFlags.RdRn),
                new(0x2E204800, 0xBF3FFC00, sizeConstraints, InstName.ClzAdvsimd, IsaVersion.v80, InstFlags.RdRnFpSimd),
                new(0x5AC01000, 0x7FFFFC00, InstName.ClzInt, IsaVersion.v80, InstFlags.RdRn),
                new(0x7EE08C00, 0xFFE0FC00, InstName.CmeqAdvsimdRegS, IsaVersion.v80, InstFlags.RdRnRmFpSimd),
                new(0x2E208C00, 0xBF20FC00, qsizeConstraints, InstName.CmeqAdvsimdRegV, IsaVersion.v80, InstFlags.RdRnRmFpSimd),
                new(0x5EE09800, 0xFFFFFC00, InstName.CmeqAdvsimdZeroS, IsaVersion.v80, InstFlags.RdRnFpSimd),
                new(0x0E209800, 0xBF3FFC00, qsizeConstraints, InstName.CmeqAdvsimdZeroV, IsaVersion.v80, InstFlags.RdRnFpSimd),
                new(0x5EE03C00, 0xFFE0FC00, InstName.CmgeAdvsimdRegS, IsaVersion.v80, InstFlags.RdRnRmFpSimd),
                new(0x0E203C00, 0xBF20FC00, qsizeConstraints, InstName.CmgeAdvsimdRegV, IsaVersion.v80, InstFlags.RdRnRmFpSimd),
                new(0x7EE08800, 0xFFFFFC00, InstName.CmgeAdvsimdZeroS, IsaVersion.v80, InstFlags.RdRnFpSimd),
                new(0x2E208800, 0xBF3FFC00, qsizeConstraints, InstName.CmgeAdvsimdZeroV, IsaVersion.v80, InstFlags.RdRnFpSimd),
                new(0x5EE03400, 0xFFE0FC00, InstName.CmgtAdvsimdRegS, IsaVersion.v80, InstFlags.RdRnRmFpSimd),
                new(0x0E203400, 0xBF20FC00, qsizeConstraints, InstName.CmgtAdvsimdRegV, IsaVersion.v80, InstFlags.RdRnRmFpSimd),
                new(0x5EE08800, 0xFFFFFC00, InstName.CmgtAdvsimdZeroS, IsaVersion.v80, InstFlags.RdRnFpSimd),
                new(0x0E208800, 0xBF3FFC00, qsizeConstraints, InstName.CmgtAdvsimdZeroV, IsaVersion.v80, InstFlags.RdRnFpSimd),
                new(0x7EE03400, 0xFFE0FC00, InstName.CmhiAdvsimdS, IsaVersion.v80, InstFlags.RdRnRmFpSimd),
                new(0x2E203400, 0xBF20FC00, qsizeConstraints, InstName.CmhiAdvsimdV, IsaVersion.v80, InstFlags.RdRnRmFpSimd),
                new(0x7EE03C00, 0xFFE0FC00, InstName.CmhsAdvsimdS, IsaVersion.v80, InstFlags.RdRnRmFpSimd),
                new(0x2E203C00, 0xBF20FC00, qsizeConstraints, InstName.CmhsAdvsimdV, IsaVersion.v80, InstFlags.RdRnRmFpSimd),
                new(0x7EE09800, 0xFFFFFC00, InstName.CmleAdvsimdS, IsaVersion.v80, InstFlags.RdRnFpSimd),
                new(0x2E209800, 0xBF3FFC00, qsizeConstraints, InstName.CmleAdvsimdV, IsaVersion.v80, InstFlags.RdRnFpSimd),
                new(0x5EE0A800, 0xFFFFFC00, InstName.CmltAdvsimdS, IsaVersion.v80, InstFlags.RdRnFpSimd),
                new(0x0E20A800, 0xBF3FFC00, qsizeConstraints, InstName.CmltAdvsimdV, IsaVersion.v80, InstFlags.RdRnFpSimd),
                new(0x5EE08C00, 0xFFE0FC00, InstName.CmtstAdvsimdS, IsaVersion.v80, InstFlags.RdRnRmFpSimd),
                new(0x0E208C00, 0xBF20FC00, qsizeConstraints, InstName.CmtstAdvsimdV, IsaVersion.v80, InstFlags.RdRnRmFpSimd),
                new(0x5AC01C00, 0x7FFFFC00, InstName.Cnt, IsaVersion.v89, InstFlags.RdRn),
                new(0x0E205800, 0xBFFFFC00, InstName.CntAdvsimd, IsaVersion.v80, InstFlags.RdRnFpSimd),
                new(0x19000400, 0x3F20FC00, InstName.Cpyfp, IsaVersion.v88, InstFlags.RdRnRsS),
                new(0x1900C400, 0x3F20FC00, InstName.Cpyfpn, IsaVersion.v88, InstFlags.RdRnRsS),
                new(0x19008400, 0x3F20FC00, InstName.Cpyfprn, IsaVersion.v88, InstFlags.RdRnRsS),
                new(0x19002400, 0x3F20FC00, InstName.Cpyfprt, IsaVersion.v88, InstFlags.RdRnRsS),
                new(0x1900E400, 0x3F20FC00, InstName.Cpyfprtn, IsaVersion.v88, InstFlags.RdRnRsS),
                new(0x1900A400, 0x3F20FC00, InstName.Cpyfprtrn, IsaVersion.v88, InstFlags.RdRnRsS),
                new(0x19006400, 0x3F20FC00, InstName.Cpyfprtwn, IsaVersion.v88, InstFlags.RdRnRsS),
                new(0x19003400, 0x3F20FC00, InstName.Cpyfpt, IsaVersion.v88, InstFlags.RdRnRsS),
                new(0x1900F400, 0x3F20FC00, InstName.Cpyfptn, IsaVersion.v88, InstFlags.RdRnRsS),
                new(0x1900B400, 0x3F20FC00, InstName.Cpyfptrn, IsaVersion.v88, InstFlags.RdRnRsS),
                new(0x19007400, 0x3F20FC00, InstName.Cpyfptwn, IsaVersion.v88, InstFlags.RdRnRsS),
                new(0x19004400, 0x3F20FC00, InstName.Cpyfpwn, IsaVersion.v88, InstFlags.RdRnRsS),
                new(0x19001400, 0x3F20FC00, InstName.Cpyfpwt, IsaVersion.v88, InstFlags.RdRnRsS),
                new(0x1900D400, 0x3F20FC00, InstName.Cpyfpwtn, IsaVersion.v88, InstFlags.RdRnRsS),
                new(0x19009400, 0x3F20FC00, InstName.Cpyfpwtrn, IsaVersion.v88, InstFlags.RdRnRsS),
                new(0x19005400, 0x3F20FC00, InstName.Cpyfpwtwn, IsaVersion.v88, InstFlags.RdRnRsS),
                new(0x1D000400, 0x3F20FC00, InstName.Cpyp, IsaVersion.v88, InstFlags.RdRnRsS),
                new(0x1D00C400, 0x3F20FC00, InstName.Cpypn, IsaVersion.v88, InstFlags.RdRnRsS),
                new(0x1D008400, 0x3F20FC00, InstName.Cpyprn, IsaVersion.v88, InstFlags.RdRnRsS),
                new(0x1D002400, 0x3F20FC00, InstName.Cpyprt, IsaVersion.v88, InstFlags.RdRnRsS),
                new(0x1D00E400, 0x3F20FC00, InstName.Cpyprtn, IsaVersion.v88, InstFlags.RdRnRsS),
                new(0x1D00A400, 0x3F20FC00, InstName.Cpyprtrn, IsaVersion.v88, InstFlags.RdRnRsS),
                new(0x1D006400, 0x3F20FC00, InstName.Cpyprtwn, IsaVersion.v88, InstFlags.RdRnRsS),
                new(0x1D003400, 0x3F20FC00, InstName.Cpypt, IsaVersion.v88, InstFlags.RdRnRsS),
                new(0x1D00F400, 0x3F20FC00, InstName.Cpyptn, IsaVersion.v88, InstFlags.RdRnRsS),
                new(0x1D00B400, 0x3F20FC00, InstName.Cpyptrn, IsaVersion.v88, InstFlags.RdRnRsS),
                new(0x1D007400, 0x3F20FC00, InstName.Cpyptwn, IsaVersion.v88, InstFlags.RdRnRsS),
                new(0x1D004400, 0x3F20FC00, InstName.Cpypwn, IsaVersion.v88, InstFlags.RdRnRsS),
                new(0x1D001400, 0x3F20FC00, InstName.Cpypwt, IsaVersion.v88, InstFlags.RdRnRsS),
                new(0x1D00D400, 0x3F20FC00, InstName.Cpypwtn, IsaVersion.v88, InstFlags.RdRnRsS),
                new(0x1D009400, 0x3F20FC00, InstName.Cpypwtrn, IsaVersion.v88, InstFlags.RdRnRsS),
                new(0x1D005400, 0x3F20FC00, InstName.Cpypwtwn, IsaVersion.v88, InstFlags.RdRnRsS),
                new(0x1AC04000, 0x7FE0F000, sfszSfszSfszSfszConstraints, InstName.Crc32, IsaVersion.v80, InstFlags.RdRnRm),
                new(0x1AC05000, 0x7FE0F000, sfszSfszSfszSfszConstraints, InstName.Crc32c, IsaVersion.v80, InstFlags.RdRnRm),
                new(0xD503229F, 0xFFFFFFFF, InstName.Csdb, IsaVersion.v80, InstFlags.None),
                new(0x1A800000, 0x7FE00C00, InstName.Csel, IsaVersion.v80, InstFlags.RdRnRmNzcv),
                new(0x1A800400, 0x7FE00C00, InstName.Csinc, IsaVersion.v80, InstFlags.RdRnRmNzcv),
                new(0x5A800000, 0x7FE00C00, InstName.Csinv, IsaVersion.v80, InstFlags.RdRnRmNzcv),
                new(0x5A800400, 0x7FE00C00, InstName.Csneg, IsaVersion.v80, InstFlags.RdRnRmNzcv),
                new(0x5AC01800, 0x7FFFFC00, InstName.Ctz, IsaVersion.v89, InstFlags.RdRn),
                new(0xD4A00001, 0xFFE0001F, InstName.Dcps1, IsaVersion.v80, InstFlags.None),
                new(0xD4A00002, 0xFFE0001F, InstName.Dcps2, IsaVersion.v80, InstFlags.None),
                new(0xD4A00003, 0xFFE0001F, InstName.Dcps3, IsaVersion.v80, InstFlags.None),
                new(0xD50320DF, 0xFFFFFFFF, InstName.Dgh, IsaVersion.v84, InstFlags.None),
                new(0xD50330BF, 0xFFFFF0FF, InstName.Dmb, IsaVersion.v80, InstFlags.None),
                new(0xD6BF03E0, 0xFFFFFFFF, InstName.Drps, IsaVersion.v80, InstFlags.None),
                new(0xD503309F, 0xFFFFF0FF, InstName.DsbDsbMemory, IsaVersion.v80, InstFlags.None),
                new(0xD503323F, 0xFFFFF3FF, InstName.DsbDsbNxs, IsaVersion.v87, InstFlags.None),
                new(0x5E000400, 0xFFE0FC00, imm5Constraints, InstName.DupAdvsimdEltScalarFromElement, IsaVersion.v80, InstFlags.RdRnFpSimd),
                new(0x0E000400, 0xBFE0FC00, imm5Imm5qConstraints, InstName.DupAdvsimdEltVectorFromElement, IsaVersion.v80, InstFlags.RdRnFpSimd),
                new(0x0E000C00, 0xBFE0FC00, imm5Imm5qConstraints, InstName.DupAdvsimdGen, IsaVersion.v80, InstFlags.RdRnFpSimdFromGpr),
                new(0x4A200000, 0x7F200000, sfimm6Constraints, InstName.Eon, IsaVersion.v80, InstFlags.RdRnRm),
                new(0xCE000000, 0xFFE08000, InstName.Eor3Advsimd, IsaVersion.v82, InstFlags.RdRnRmRaFpSimd),
                new(0x2E201C00, 0xBFE0FC00, InstName.EorAdvsimd, IsaVersion.v80, InstFlags.RdRnRmFpSimd),
                new(0x52000000, 0x7F800000, nimmsNimmsNimmsNimmsNimmsNimmsNimmsNimmsSfnConstraints, InstName.EorLogImm, IsaVersion.v80, InstFlags.RdSPRn),
                new(0x4A000000, 0x7F200000, sfimm6Constraints, InstName.EorLogShift, IsaVersion.v80, InstFlags.RdRnRm),
                new(0xD69F03E0, 0xFFFFFFFF, InstName.Eret, IsaVersion.v80, InstFlags.None),
                new(0xD69F0BFF, 0xFFFFFBFF, InstName.Ereta, IsaVersion.v83, InstFlags.None),
                new(0xD503221F, 0xFFFFFFFF, InstName.Esb, IsaVersion.v82, InstFlags.None),
                new(0x13800000, 0x7FA00000, nsfNsfSfimmsConstraints, InstName.Extr, IsaVersion.v80, InstFlags.RdRnRm),
                new(0x2E000000, 0xBFE08400, qimm4Constraints, InstName.ExtAdvsimd, IsaVersion.v80, InstFlags.RdRnRmFpSimd),
                new(0x7EC01400, 0xFFE0FC00, InstName.FabdAdvsimdSH, IsaVersion.v82, InstFlags.RdRnRmFpSimd),
                new(0x7EA0D400, 0xFFA0FC00, InstName.FabdAdvsimdS, IsaVersion.v80, InstFlags.RdRnRmFpSimd),
                new(0x2EC01400, 0xBFE0FC00, InstName.FabdAdvsimdVH, IsaVersion.v82, InstFlags.RdRnRmFpSimd),
                new(0x2EA0D400, 0xBFA0FC00, qszConstraints, InstName.FabdAdvsimdV, IsaVersion.v80, InstFlags.RdRnRmFpSimd),
                new(0x0EF8F800, 0xBFFFFC00, InstName.FabsAdvsimdHalf, IsaVersion.v82, InstFlags.RdRnFpSimd),
                new(0x0EA0F800, 0xBFBFFC00, qszConstraints, InstName.FabsAdvsimdSingleAndDouble, IsaVersion.v80, InstFlags.RdRnFpSimd),
                new(0x1E20C000, 0xFFBFFC00, InstName.FabsFloat, IsaVersion.v80, InstFlags.RdRnFpSimd),
                new(0x1EE0C000, 0xFFFFFC00, InstName.FabsFloat, IsaVersion.v80, InstFlags.RdRnFpSimd),
                new(0x7E402C00, 0xFFE0FC00, euacEuacEuacConstraints, InstName.FacgeAdvsimdSH, IsaVersion.v82, InstFlags.RdRnRmFpSimd),
                new(0x7E20EC00, 0xFFA0FC00, euacEuacEuacConstraints, InstName.FacgeAdvsimdS, IsaVersion.v80, InstFlags.RdRnRmFpSimd),
                new(0x2E402C00, 0xBFE0FC00, euacEuacEuacConstraints, InstName.FacgeAdvsimdVH, IsaVersion.v82, InstFlags.RdRnRmFpSimd),
                new(0x2E20EC00, 0xBFA0FC00, qszEuacEuacEuacConstraints, InstName.FacgeAdvsimdV, IsaVersion.v80, InstFlags.RdRnRmFpSimd),
                new(0x7EC02C00, 0xFFE0FC00, euacEuacEuacConstraints, InstName.FacgtAdvsimdSH, IsaVersion.v82, InstFlags.RdRnRmFpSimd),
                new(0x7EA0EC00, 0xFFA0FC00, euacEuacEuacConstraints, InstName.FacgtAdvsimdS, IsaVersion.v80, InstFlags.RdRnRmFpSimd),
                new(0x2EC02C00, 0xBFE0FC00, euacEuacEuacConstraints, InstName.FacgtAdvsimdVH, IsaVersion.v82, InstFlags.RdRnRmFpSimd),
                new(0x2EA0EC00, 0xBFA0FC00, qszEuacEuacEuacConstraints, InstName.FacgtAdvsimdV, IsaVersion.v80, InstFlags.RdRnRmFpSimd),
                new(0x5E30D800, 0xFFBFFC00, szConstraints, InstName.FaddpAdvsimdPairHalf, IsaVersion.v82, InstFlags.RdRnFpSimd),
                new(0x7E30D800, 0xFFBFFC00, InstName.FaddpAdvsimdPairSingleAndDouble, IsaVersion.v80, InstFlags.RdRnFpSimd),
                new(0x2E401400, 0xBFE0FC00, InstName.FaddpAdvsimdVecHalf, IsaVersion.v82, InstFlags.RdRnRmFpSimd),
                new(0x2E20D400, 0xBFA0FC00, qszConstraints, InstName.FaddpAdvsimdVecSingleAndDouble, IsaVersion.v80, InstFlags.RdRnRmFpSimd),
                new(0x0E401400, 0xBFE0FC00, InstName.FaddAdvsimdHalf, IsaVersion.v82, InstFlags.RdRnRmFpSimd),
                new(0x0E20D400, 0xBFA0FC00, qszConstraints, InstName.FaddAdvsimdSingleAndDouble, IsaVersion.v80, InstFlags.RdRnRmFpSimd),
                new(0x1E202800, 0xFFA0FC00, InstName.FaddFloat, IsaVersion.v80, InstFlags.RdRnRmFpSimd),
                new(0x1EE02800, 0xFFE0FC00, InstName.FaddFloat, IsaVersion.v80, InstFlags.RdRnRmFpSimd),
                new(0x2E80E400, 0xBFA0EC00, sizeQsizeConstraints, InstName.FcaddAdvsimdVec, IsaVersion.v83, InstFlags.RdRnRmFpSimd),
                new(0x2E40E400, 0xBFE0EC00, sizeQsizeConstraints, InstName.FcaddAdvsimdVec, IsaVersion.v83, InstFlags.RdRnRmFpSimd),
                new(0x1E200410, 0xFFA00C10, InstName.FccmpeFloat, IsaVersion.v80, InstFlags.RnRmNzcvSFpSimd),
                new(0x1EE00410, 0xFFE00C10, InstName.FccmpeFloat, IsaVersion.v80, InstFlags.RnRmNzcvSFpSimd),
                new(0x1E200400, 0xFFA00C10, InstName.FccmpFloat, IsaVersion.v80, InstFlags.RnRmNzcvSFpSimd),
                new(0x1EE00400, 0xFFE00C10, InstName.FccmpFloat, IsaVersion.v80, InstFlags.RnRmNzcvSFpSimd),
                new(0x5E402400, 0xFFE0FC00, euacEuacEuacConstraints, InstName.FcmeqAdvsimdRegSH, IsaVersion.v82, InstFlags.RdRnRmFpSimd),
                new(0x5E20E400, 0xFFA0FC00, euacEuacEuacConstraints, InstName.FcmeqAdvsimdRegS, IsaVersion.v80, InstFlags.RdRnRmFpSimd),
                new(0x0E402400, 0xBFE0FC00, euacEuacEuacConstraints, InstName.FcmeqAdvsimdRegVH, IsaVersion.v82, InstFlags.RdRnRmFpSimd),
                new(0x0E20E400, 0xBFA0FC00, qszEuacEuacEuacConstraints, InstName.FcmeqAdvsimdRegV, IsaVersion.v80, InstFlags.RdRnRmFpSimd),
                new(0x5EF8D800, 0xFFFFFC00, InstName.FcmeqAdvsimdZeroSH, IsaVersion.v82, InstFlags.RdRnFpSimd),
                new(0x5EA0D800, 0xFFBFFC00, InstName.FcmeqAdvsimdZeroS, IsaVersion.v80, InstFlags.RdRnFpSimd),
                new(0x0EF8D800, 0xBFFFFC00, InstName.FcmeqAdvsimdZeroVH, IsaVersion.v82, InstFlags.RdRnFpSimd),
                new(0x0EA0D800, 0xBFBFFC00, qszConstraints, InstName.FcmeqAdvsimdZeroV, IsaVersion.v80, InstFlags.RdRnFpSimd),
                new(0x7E402400, 0xFFE0FC00, euacEuacEuacConstraints, InstName.FcmgeAdvsimdRegSH, IsaVersion.v82, InstFlags.RdRnRmFpSimd),
                new(0x7E20E400, 0xFFA0FC00, euacEuacEuacConstraints, InstName.FcmgeAdvsimdRegS, IsaVersion.v80, InstFlags.RdRnRmFpSimd),
                new(0x2E402400, 0xBFE0FC00, euacEuacEuacConstraints, InstName.FcmgeAdvsimdRegVH, IsaVersion.v82, InstFlags.RdRnRmFpSimd),
                new(0x2E20E400, 0xBFA0FC00, qszEuacEuacEuacConstraints, InstName.FcmgeAdvsimdRegV, IsaVersion.v80, InstFlags.RdRnRmFpSimd),
                new(0x7EF8C800, 0xFFFFFC00, InstName.FcmgeAdvsimdZeroSH, IsaVersion.v82, InstFlags.RdRnFpSimd),
                new(0x7EA0C800, 0xFFBFFC00, InstName.FcmgeAdvsimdZeroS, IsaVersion.v80, InstFlags.RdRnFpSimd),
                new(0x2EF8C800, 0xBFFFFC00, InstName.FcmgeAdvsimdZeroVH, IsaVersion.v82, InstFlags.RdRnFpSimd),
                new(0x2EA0C800, 0xBFBFFC00, qszConstraints, InstName.FcmgeAdvsimdZeroV, IsaVersion.v80, InstFlags.RdRnFpSimd),
                new(0x7EC02400, 0xFFE0FC00, euacEuacEuacConstraints, InstName.FcmgtAdvsimdRegSH, IsaVersion.v82, InstFlags.RdRnRmFpSimd),
                new(0x7EA0E400, 0xFFA0FC00, euacEuacEuacConstraints, InstName.FcmgtAdvsimdRegS, IsaVersion.v80, InstFlags.RdRnRmFpSimd),
                new(0x2EC02400, 0xBFE0FC00, euacEuacEuacConstraints, InstName.FcmgtAdvsimdRegVH, IsaVersion.v82, InstFlags.RdRnRmFpSimd),
                new(0x2EA0E400, 0xBFA0FC00, qszEuacEuacEuacConstraints, InstName.FcmgtAdvsimdRegV, IsaVersion.v80, InstFlags.RdRnRmFpSimd),
                new(0x5EF8C800, 0xFFFFFC00, InstName.FcmgtAdvsimdZeroSH, IsaVersion.v82, InstFlags.RdRnFpSimd),
                new(0x5EA0C800, 0xFFBFFC00, InstName.FcmgtAdvsimdZeroS, IsaVersion.v80, InstFlags.RdRnFpSimd),
                new(0x0EF8C800, 0xBFFFFC00, InstName.FcmgtAdvsimdZeroVH, IsaVersion.v82, InstFlags.RdRnFpSimd),
                new(0x0EA0C800, 0xBFBFFC00, qszConstraints, InstName.FcmgtAdvsimdZeroV, IsaVersion.v80, InstFlags.RdRnFpSimd),
                new(0x2F801000, 0xBF809400, sizeSizeSizelSizeqSizehqConstraints, InstName.FcmlaAdvsimdElt, IsaVersion.v83, InstFlags.RdReadRdRnRmFpSimd),
                new(0x2F401000, 0xBFC09400, sizeSizeSizelSizeqSizehqConstraints, InstName.FcmlaAdvsimdElt, IsaVersion.v83, InstFlags.RdReadRdRnRmFpSimd),
                new(0x2E80C400, 0xBFA0E400, sizeQsizeConstraints, InstName.FcmlaAdvsimdVec, IsaVersion.v83, InstFlags.RdReadRdRnRmFpSimd),
                new(0x2E40C400, 0xBFE0E400, sizeQsizeConstraints, InstName.FcmlaAdvsimdVec, IsaVersion.v83, InstFlags.RdReadRdRnRmFpSimd),
                new(0x7EF8D800, 0xFFFFFC00, InstName.FcmleAdvsimdSH, IsaVersion.v82, InstFlags.RdRnFpSimd),
                new(0x7EA0D800, 0xFFBFFC00, InstName.FcmleAdvsimdS, IsaVersion.v80, InstFlags.RdRnFpSimd),
                new(0x2EF8D800, 0xBFFFFC00, InstName.FcmleAdvsimdVH, IsaVersion.v82, InstFlags.RdRnFpSimd),
                new(0x2EA0D800, 0xBFBFFC00, qszConstraints, InstName.FcmleAdvsimdV, IsaVersion.v80, InstFlags.RdRnFpSimd),
                new(0x5EF8E800, 0xFFFFFC00, InstName.FcmltAdvsimdSH, IsaVersion.v82, InstFlags.RdRnFpSimd),
                new(0x5EA0E800, 0xFFBFFC00, InstName.FcmltAdvsimdS, IsaVersion.v80, InstFlags.RdRnFpSimd),
                new(0x0EF8E800, 0xBFFFFC00, InstName.FcmltAdvsimdVH, IsaVersion.v82, InstFlags.RdRnFpSimd),
                new(0x0EA0E800, 0xBFBFFC00, qszConstraints, InstName.FcmltAdvsimdV, IsaVersion.v80, InstFlags.RdRnFpSimd),
                new(0x1E202010, 0xFFA0FC17, InstName.FcmpeFloat, IsaVersion.v80, InstFlags.RnRmSFpSimd),
                new(0x1EE02010, 0xFFE0FC17, InstName.FcmpeFloat, IsaVersion.v80, InstFlags.RnRmSFpSimd),
                new(0x1E202000, 0xFFA0FC17, InstName.FcmpFloat, IsaVersion.v80, InstFlags.RnRmSFpSimd),
                new(0x1EE02000, 0xFFE0FC17, InstName.FcmpFloat, IsaVersion.v80, InstFlags.RnRmSFpSimd),
                new(0x1E200C00, 0xFFA00C00, InstName.FcselFloat, IsaVersion.v80, InstFlags.RdRnRmNzcvFpSimd),
                new(0x1EE00C00, 0xFFE00C00, InstName.FcselFloat, IsaVersion.v80, InstFlags.RdRnRmNzcvFpSimd),
                new(0x5E79C800, 0xFFFFFC00, InstName.FcvtasAdvsimdSH, IsaVersion.v82, InstFlags.RdRnFpSimd),
                new(0x5E21C800, 0xFFBFFC00, InstName.FcvtasAdvsimdS, IsaVersion.v80, InstFlags.RdRnFpSimd),
                new(0x0E79C800, 0xBFFFFC00, InstName.FcvtasAdvsimdVH, IsaVersion.v82, InstFlags.RdRnFpSimd),
                new(0x0E21C800, 0xBFBFFC00, qszConstraints, InstName.FcvtasAdvsimdV, IsaVersion.v80, InstFlags.RdRnFpSimd),
                new(0x1E240000, 0x7FBFFC00, InstName.FcvtasFloat, IsaVersion.v80, InstFlags.RdRnFpSimdToGpr),
                new(0x1EE40000, 0x7FFFFC00, InstName.FcvtasFloat, IsaVersion.v80, InstFlags.RdRnFpSimdToGpr),
                new(0x7E79C800, 0xFFFFFC00, InstName.FcvtauAdvsimdSH, IsaVersion.v82, InstFlags.RdRnFpSimd),
                new(0x7E21C800, 0xFFBFFC00, InstName.FcvtauAdvsimdS, IsaVersion.v80, InstFlags.RdRnFpSimd),
                new(0x2E79C800, 0xBFFFFC00, InstName.FcvtauAdvsimdVH, IsaVersion.v82, InstFlags.RdRnFpSimd),
                new(0x2E21C800, 0xBFBFFC00, qszConstraints, InstName.FcvtauAdvsimdV, IsaVersion.v80, InstFlags.RdRnFpSimd),
                new(0x1E250000, 0x7FBFFC00, InstName.FcvtauFloat, IsaVersion.v80, InstFlags.RdRnFpSimdToGpr),
                new(0x1EE50000, 0x7FFFFC00, InstName.FcvtauFloat, IsaVersion.v80, InstFlags.RdRnFpSimdToGpr),
                new(0x0E217800, 0xBFBFFC00, InstName.FcvtlAdvsimd, IsaVersion.v80, InstFlags.RdRnFpSimd),
                new(0x5E79B800, 0xFFFFFC00, InstName.FcvtmsAdvsimdSH, IsaVersion.v82, InstFlags.RdRnFpSimd),
                new(0x5E21B800, 0xFFBFFC00, InstName.FcvtmsAdvsimdS, IsaVersion.v80, InstFlags.RdRnFpSimd),
                new(0x0E79B800, 0xBFFFFC00, InstName.FcvtmsAdvsimdVH, IsaVersion.v82, InstFlags.RdRnFpSimd),
                new(0x0E21B800, 0xBFBFFC00, qszConstraints, InstName.FcvtmsAdvsimdV, IsaVersion.v80, InstFlags.RdRnFpSimd),
                new(0x1E300000, 0x7FBFFC00, InstName.FcvtmsFloat, IsaVersion.v80, InstFlags.RdRnFpSimdToGpr),
                new(0x1EF00000, 0x7FFFFC00, InstName.FcvtmsFloat, IsaVersion.v80, InstFlags.RdRnFpSimdToGpr),
                new(0x7E79B800, 0xFFFFFC00, InstName.FcvtmuAdvsimdSH, IsaVersion.v82, InstFlags.RdRnFpSimd),
                new(0x7E21B800, 0xFFBFFC00, InstName.FcvtmuAdvsimdS, IsaVersion.v80, InstFlags.RdRnFpSimd),
                new(0x2E79B800, 0xBFFFFC00, InstName.FcvtmuAdvsimdVH, IsaVersion.v82, InstFlags.RdRnFpSimd),
                new(0x2E21B800, 0xBFBFFC00, qszConstraints, InstName.FcvtmuAdvsimdV, IsaVersion.v80, InstFlags.RdRnFpSimd),
                new(0x1E310000, 0x7FBFFC00, InstName.FcvtmuFloat, IsaVersion.v80, InstFlags.RdRnFpSimdToGpr),
                new(0x1EF10000, 0x7FFFFC00, InstName.FcvtmuFloat, IsaVersion.v80, InstFlags.RdRnFpSimdToGpr),
                new(0x5E79A800, 0xFFFFFC00, InstName.FcvtnsAdvsimdSH, IsaVersion.v82, InstFlags.RdRnFpSimd),
                new(0x5E21A800, 0xFFBFFC00, InstName.FcvtnsAdvsimdS, IsaVersion.v80, InstFlags.RdRnFpSimd),
                new(0x0E79A800, 0xBFFFFC00, InstName.FcvtnsAdvsimdVH, IsaVersion.v82, InstFlags.RdRnFpSimd),
                new(0x0E21A800, 0xBFBFFC00, qszConstraints, InstName.FcvtnsAdvsimdV, IsaVersion.v80, InstFlags.RdRnFpSimd),
                new(0x1E200000, 0x7FBFFC00, InstName.FcvtnsFloat, IsaVersion.v80, InstFlags.RdRnFpSimdToGpr),
                new(0x1EE00000, 0x7FFFFC00, InstName.FcvtnsFloat, IsaVersion.v80, InstFlags.RdRnFpSimdToGpr),
                new(0x7E79A800, 0xFFFFFC00, InstName.FcvtnuAdvsimdSH, IsaVersion.v82, InstFlags.RdRnFpSimd),
                new(0x7E21A800, 0xFFBFFC00, InstName.FcvtnuAdvsimdS, IsaVersion.v80, InstFlags.RdRnFpSimd),
                new(0x2E79A800, 0xBFFFFC00, InstName.FcvtnuAdvsimdVH, IsaVersion.v82, InstFlags.RdRnFpSimd),
                new(0x2E21A800, 0xBFBFFC00, qszConstraints, InstName.FcvtnuAdvsimdV, IsaVersion.v80, InstFlags.RdRnFpSimd),
                new(0x1E210000, 0x7FBFFC00, InstName.FcvtnuFloat, IsaVersion.v80, InstFlags.RdRnFpSimdToGpr),
                new(0x1EE10000, 0x7FFFFC00, InstName.FcvtnuFloat, IsaVersion.v80, InstFlags.RdRnFpSimdToGpr),
                new(0x0E216800, 0xBFBFFC00, InstName.FcvtnAdvsimd, IsaVersion.v80, InstFlags.RdReadRdRnFpSimd),
                new(0x5EF9A800, 0xFFFFFC00, InstName.FcvtpsAdvsimdSH, IsaVersion.v82, InstFlags.RdRnFpSimd),
                new(0x5EA1A800, 0xFFBFFC00, InstName.FcvtpsAdvsimdS, IsaVersion.v80, InstFlags.RdRnFpSimd),
                new(0x0EF9A800, 0xBFFFFC00, InstName.FcvtpsAdvsimdVH, IsaVersion.v82, InstFlags.RdRnFpSimd),
                new(0x0EA1A800, 0xBFBFFC00, qszConstraints, InstName.FcvtpsAdvsimdV, IsaVersion.v80, InstFlags.RdRnFpSimd),
                new(0x1E280000, 0x7FBFFC00, InstName.FcvtpsFloat, IsaVersion.v80, InstFlags.RdRnFpSimdToGpr),
                new(0x1EE80000, 0x7FFFFC00, InstName.FcvtpsFloat, IsaVersion.v80, InstFlags.RdRnFpSimdToGpr),
                new(0x7EF9A800, 0xFFFFFC00, InstName.FcvtpuAdvsimdSH, IsaVersion.v82, InstFlags.RdRnFpSimd),
                new(0x7EA1A800, 0xFFBFFC00, InstName.FcvtpuAdvsimdS, IsaVersion.v80, InstFlags.RdRnFpSimd),
                new(0x2EF9A800, 0xBFFFFC00, InstName.FcvtpuAdvsimdVH, IsaVersion.v82, InstFlags.RdRnFpSimd),
                new(0x2EA1A800, 0xBFBFFC00, qszConstraints, InstName.FcvtpuAdvsimdV, IsaVersion.v80, InstFlags.RdRnFpSimd),
                new(0x1E290000, 0x7FBFFC00, InstName.FcvtpuFloat, IsaVersion.v80, InstFlags.RdRnFpSimdToGpr),
                new(0x1EE90000, 0x7FFFFC00, InstName.FcvtpuFloat, IsaVersion.v80, InstFlags.RdRnFpSimdToGpr),
                new(0x7E216800, 0xFFBFFC00, szConstraints2, InstName.FcvtxnAdvsimdS, IsaVersion.v80, InstFlags.RdReadRdRnFpSimd),
                new(0x2E216800, 0xBFBFFC00, szConstraints2, InstName.FcvtxnAdvsimdV, IsaVersion.v80, InstFlags.RdReadRdRnFpSimd),
                new(0x5F40FC00, 0xFFC0FC00, immhConstraints, InstName.FcvtzsAdvsimdFixS, IsaVersion.v80, InstFlags.RdRnFpSimd),
                new(0x5F10FC00, 0xFFF0FC00, immhConstraints, InstName.FcvtzsAdvsimdFixS, IsaVersion.v80, InstFlags.RdRnFpSimd),
                new(0x5F20FC00, 0xFFE0FC00, immhConstraints, InstName.FcvtzsAdvsimdFixS, IsaVersion.v80, InstFlags.RdRnFpSimd),
                new(0x0F40FC00, 0xBFC0FC00, immhQimmhConstraints, InstName.FcvtzsAdvsimdFixV, IsaVersion.v80, InstFlags.RdRnFpSimd),
                new(0x0F10FC00, 0xBFF0FC00, immhQimmhConstraints, InstName.FcvtzsAdvsimdFixV, IsaVersion.v80, InstFlags.RdRnFpSimd),
                new(0x0F20FC00, 0xBFE0FC00, immhQimmhConstraints, InstName.FcvtzsAdvsimdFixV, IsaVersion.v80, InstFlags.RdRnFpSimd),
                new(0x5EF9B800, 0xFFFFFC00, InstName.FcvtzsAdvsimdIntSH, IsaVersion.v82, InstFlags.RdRnFpSimd),
                new(0x5EA1B800, 0xFFBFFC00, InstName.FcvtzsAdvsimdIntS, IsaVersion.v80, InstFlags.RdRnFpSimd),
                new(0x0EF9B800, 0xBFFFFC00, InstName.FcvtzsAdvsimdIntVH, IsaVersion.v82, InstFlags.RdRnFpSimd),
                new(0x0EA1B800, 0xBFBFFC00, qszConstraints, InstName.FcvtzsAdvsimdIntV, IsaVersion.v80, InstFlags.RdRnFpSimd),
                new(0x1E180000, 0x7FBF0000, sfscaleConstraints, InstName.FcvtzsFloatFix, IsaVersion.v80, InstFlags.RdRnFpSimdToGpr),
                new(0x1ED80000, 0x7FFF0000, sfscaleConstraints, InstName.FcvtzsFloatFix, IsaVersion.v80, InstFlags.RdRnFpSimdToGpr),
                new(0x1E380000, 0x7FBFFC00, InstName.FcvtzsFloatInt, IsaVersion.v80, InstFlags.RdRnFpSimdToGpr),
                new(0x1EF80000, 0x7FFFFC00, InstName.FcvtzsFloatInt, IsaVersion.v80, InstFlags.RdRnFpSimdToGpr),
                new(0x7F40FC00, 0xFFC0FC00, immhConstraints, InstName.FcvtzuAdvsimdFixS, IsaVersion.v80, InstFlags.RdRnFpSimd),
                new(0x7F10FC00, 0xFFF0FC00, immhConstraints, InstName.FcvtzuAdvsimdFixS, IsaVersion.v80, InstFlags.RdRnFpSimd),
                new(0x7F20FC00, 0xFFE0FC00, immhConstraints, InstName.FcvtzuAdvsimdFixS, IsaVersion.v80, InstFlags.RdRnFpSimd),
                new(0x2F40FC00, 0xBFC0FC00, immhQimmhConstraints, InstName.FcvtzuAdvsimdFixV, IsaVersion.v80, InstFlags.RdRnFpSimd),
                new(0x2F10FC00, 0xBFF0FC00, immhQimmhConstraints, InstName.FcvtzuAdvsimdFixV, IsaVersion.v80, InstFlags.RdRnFpSimd),
                new(0x2F20FC00, 0xBFE0FC00, immhQimmhConstraints, InstName.FcvtzuAdvsimdFixV, IsaVersion.v80, InstFlags.RdRnFpSimd),
                new(0x7EF9B800, 0xFFFFFC00, InstName.FcvtzuAdvsimdIntSH, IsaVersion.v82, InstFlags.RdRnFpSimd),
                new(0x7EA1B800, 0xFFBFFC00, InstName.FcvtzuAdvsimdIntS, IsaVersion.v80, InstFlags.RdRnFpSimd),
                new(0x2EF9B800, 0xBFFFFC00, InstName.FcvtzuAdvsimdIntVH, IsaVersion.v82, InstFlags.RdRnFpSimd),
                new(0x2EA1B800, 0xBFBFFC00, qszConstraints, InstName.FcvtzuAdvsimdIntV, IsaVersion.v80, InstFlags.RdRnFpSimd),
                new(0x1E190000, 0x7FBF0000, sfscaleConstraints, InstName.FcvtzuFloatFix, IsaVersion.v80, InstFlags.RdRnFpSimdToGpr),
                new(0x1ED90000, 0x7FFF0000, sfscaleConstraints, InstName.FcvtzuFloatFix, IsaVersion.v80, InstFlags.RdRnFpSimdToGpr),
                new(0x1E390000, 0x7FBFFC00, InstName.FcvtzuFloatInt, IsaVersion.v80, InstFlags.RdRnFpSimdToGpr),
                new(0x1EF90000, 0x7FFFFC00, InstName.FcvtzuFloatInt, IsaVersion.v80, InstFlags.RdRnFpSimdToGpr),
                new(0x1E224000, 0xFF3E7C00, ftypeopcFtypeopcFtypeopcFtypeopcFtypeOpcConstraints, InstName.FcvtFloat, IsaVersion.v80, InstFlags.RdRnFpSimd),
                new(0x2E403C00, 0xBFE0FC00, InstName.FdivAdvsimdHalf, IsaVersion.v82, InstFlags.RdRnRmFpSimd),
                new(0x2E20FC00, 0xBFA0FC00, qszConstraints, InstName.FdivAdvsimdSingleAndDouble, IsaVersion.v80, InstFlags.RdRnRmFpSimd),
                new(0x1E201800, 0xFFA0FC00, InstName.FdivFloat, IsaVersion.v80, InstFlags.RdRnRmFpSimd),
                new(0x1EE01800, 0xFFE0FC00, InstName.FdivFloat, IsaVersion.v80, InstFlags.RdRnRmFpSimd),
                new(0x1E7E0000, 0xFFFFFC00, InstName.Fjcvtzs, IsaVersion.v83, InstFlags.RdRnSFpSimd),
                new(0x1F000000, 0xFFA08000, InstName.FmaddFloat, IsaVersion.v80, InstFlags.RdRnRmRaFpSimd),
                new(0x1FC00000, 0xFFE08000, InstName.FmaddFloat, IsaVersion.v80, InstFlags.RdRnRmRaFpSimd),
                new(0x5E30C800, 0xFFBFFC00, szConstraints, InstName.FmaxnmpAdvsimdPairHalf, IsaVersion.v82, InstFlags.RdRnFpSimd),
                new(0x7E30C800, 0xFFBFFC00, InstName.FmaxnmpAdvsimdPairSingleAndDouble, IsaVersion.v80, InstFlags.RdRnFpSimd),
                new(0x2E400400, 0xBFE0FC00, InstName.FmaxnmpAdvsimdVecHalf, IsaVersion.v82, InstFlags.RdRnRmFpSimd),
                new(0x2E20C400, 0xBFA0FC00, qszConstraints, InstName.FmaxnmpAdvsimdVecSingleAndDouble, IsaVersion.v80, InstFlags.RdRnRmFpSimd),
                new(0x0E30C800, 0xBFFFFC00, InstName.FmaxnmvAdvsimdHalf, IsaVersion.v82, InstFlags.RdRnFpSimd),
                new(0x6E30C800, 0xFFFFFC00, InstName.FmaxnmvAdvsimdSingleAndDouble, IsaVersion.v80, InstFlags.RdRnFpSimd),
                new(0x0E400400, 0xBFE0FC00, InstName.FmaxnmAdvsimdHalf, IsaVersion.v82, InstFlags.RdRnRmFpSimd),
                new(0x0E20C400, 0xBFA0FC00, qszConstraints, InstName.FmaxnmAdvsimdSingleAndDouble, IsaVersion.v80, InstFlags.RdRnRmFpSimd),
                new(0x1E206800, 0xFFA0FC00, InstName.FmaxnmFloat, IsaVersion.v80, InstFlags.RdRnRmFpSimd),
                new(0x1EE06800, 0xFFE0FC00, InstName.FmaxnmFloat, IsaVersion.v80, InstFlags.RdRnRmFpSimd),
                new(0x5E30F800, 0xFFBFFC00, szConstraints, InstName.FmaxpAdvsimdPairHalf, IsaVersion.v82, InstFlags.RdRnFpSimd),
                new(0x7E30F800, 0xFFBFFC00, InstName.FmaxpAdvsimdPairSingleAndDouble, IsaVersion.v80, InstFlags.RdRnFpSimd),
                new(0x2E403400, 0xBFE0FC00, InstName.FmaxpAdvsimdVecHalf, IsaVersion.v82, InstFlags.RdRnRmFpSimd),
                new(0x2E20F400, 0xBFA0FC00, qszConstraints, InstName.FmaxpAdvsimdVecSingleAndDouble, IsaVersion.v80, InstFlags.RdRnRmFpSimd),
                new(0x0E30F800, 0xBFFFFC00, InstName.FmaxvAdvsimdHalf, IsaVersion.v82, InstFlags.RdRnFpSimd),
                new(0x6E30F800, 0xFFFFFC00, InstName.FmaxvAdvsimdSingleAndDouble, IsaVersion.v80, InstFlags.RdRnFpSimd),
                new(0x0E403400, 0xBFE0FC00, InstName.FmaxAdvsimdHalf, IsaVersion.v82, InstFlags.RdRnRmFpSimd),
                new(0x0E20F400, 0xBFA0FC00, qszConstraints, InstName.FmaxAdvsimdSingleAndDouble, IsaVersion.v80, InstFlags.RdRnRmFpSimd),
                new(0x1E204800, 0xFFA0FC00, InstName.FmaxFloat, IsaVersion.v80, InstFlags.RdRnRmFpSimd),
                new(0x1EE04800, 0xFFE0FC00, InstName.FmaxFloat, IsaVersion.v80, InstFlags.RdRnRmFpSimd),
                new(0x5EB0C800, 0xFFBFFC00, szConstraints, InstName.FminnmpAdvsimdPairHalf, IsaVersion.v82, InstFlags.RdRnFpSimd),
                new(0x7EB0C800, 0xFFBFFC00, InstName.FminnmpAdvsimdPairSingleAndDouble, IsaVersion.v80, InstFlags.RdRnFpSimd),
                new(0x2EC00400, 0xBFE0FC00, InstName.FminnmpAdvsimdVecHalf, IsaVersion.v82, InstFlags.RdRnRmFpSimd),
                new(0x2EA0C400, 0xBFA0FC00, qszConstraints, InstName.FminnmpAdvsimdVecSingleAndDouble, IsaVersion.v80, InstFlags.RdRnRmFpSimd),
                new(0x0EB0C800, 0xBFFFFC00, InstName.FminnmvAdvsimdHalf, IsaVersion.v82, InstFlags.RdRnFpSimd),
                new(0x6EB0C800, 0xFFFFFC00, InstName.FminnmvAdvsimdSingleAndDouble, IsaVersion.v80, InstFlags.RdRnFpSimd),
                new(0x0EC00400, 0xBFE0FC00, InstName.FminnmAdvsimdHalf, IsaVersion.v82, InstFlags.RdRnRmFpSimd),
                new(0x0EA0C400, 0xBFA0FC00, qszConstraints, InstName.FminnmAdvsimdSingleAndDouble, IsaVersion.v80, InstFlags.RdRnRmFpSimd),
                new(0x1E207800, 0xFFA0FC00, InstName.FminnmFloat, IsaVersion.v80, InstFlags.RdRnRmFpSimd),
                new(0x1EE07800, 0xFFE0FC00, InstName.FminnmFloat, IsaVersion.v80, InstFlags.RdRnRmFpSimd),
                new(0x5EB0F800, 0xFFBFFC00, szConstraints, InstName.FminpAdvsimdPairHalf, IsaVersion.v82, InstFlags.RdRnFpSimd),
                new(0x7EB0F800, 0xFFBFFC00, InstName.FminpAdvsimdPairSingleAndDouble, IsaVersion.v80, InstFlags.RdRnFpSimd),
                new(0x2EC03400, 0xBFE0FC00, InstName.FminpAdvsimdVecHalf, IsaVersion.v82, InstFlags.RdRnRmFpSimd),
                new(0x2EA0F400, 0xBFA0FC00, qszConstraints, InstName.FminpAdvsimdVecSingleAndDouble, IsaVersion.v80, InstFlags.RdRnRmFpSimd),
                new(0x0EB0F800, 0xBFFFFC00, InstName.FminvAdvsimdHalf, IsaVersion.v82, InstFlags.RdRnFpSimd),
                new(0x6EB0F800, 0xFFFFFC00, InstName.FminvAdvsimdSingleAndDouble, IsaVersion.v80, InstFlags.RdRnFpSimd),
                new(0x0EC03400, 0xBFE0FC00, InstName.FminAdvsimdHalf, IsaVersion.v82, InstFlags.RdRnRmFpSimd),
                new(0x0EA0F400, 0xBFA0FC00, qszConstraints, InstName.FminAdvsimdSingleAndDouble, IsaVersion.v80, InstFlags.RdRnRmFpSimd),
                new(0x1E205800, 0xFFA0FC00, InstName.FminFloat, IsaVersion.v80, InstFlags.RdRnRmFpSimd),
                new(0x1EE05800, 0xFFE0FC00, InstName.FminFloat, IsaVersion.v80, InstFlags.RdRnRmFpSimd),
                new(0x0F800000, 0xBFC0F400, szConstraints, InstName.FmlalAdvsimdEltFmlal, IsaVersion.v82, InstFlags.RdReadRdRnRmFpSimd),
                new(0x2F808000, 0xBFC0F400, szConstraints, InstName.FmlalAdvsimdEltFmlal2, IsaVersion.v82, InstFlags.RdReadRdRnRmFpSimd),
                new(0x0E20EC00, 0xBFE0FC00, szConstraints, InstName.FmlalAdvsimdVecFmlal, IsaVersion.v82, InstFlags.RdReadRdRnRmFpSimd),
                new(0x2E20CC00, 0xBFE0FC00, szConstraints, InstName.FmlalAdvsimdVecFmlal2, IsaVersion.v82, InstFlags.RdReadRdRnRmFpSimd),
                new(0x5F001000, 0xFFC0F400, InstName.FmlaAdvsimdElt2regScalarHalf, IsaVersion.v82, InstFlags.RdReadRdRnRmFpSimd),
                new(0x5F801000, 0xFF80F400, szlConstraints, InstName.FmlaAdvsimdElt2regScalarSingleAndDouble, IsaVersion.v80, InstFlags.RdReadRdRnRmFpSimd),
                new(0x0F001000, 0xBFC0F400, InstName.FmlaAdvsimdElt2regElementHalf, IsaVersion.v82, InstFlags.RdReadRdRnRmFpSimd),
                new(0x0F801000, 0xBF80F400, szlQszConstraints, InstName.FmlaAdvsimdElt2regElementSingleAndDouble, IsaVersion.v80, InstFlags.RdReadRdRnRmFpSimd),
                new(0x0E400C00, 0xBFE0FC00, InstName.FmlaAdvsimdVecHalf, IsaVersion.v82, InstFlags.RdReadRdRnRmFpSimd),
                new(0x0E20CC00, 0xBFA0FC00, qszConstraints, InstName.FmlaAdvsimdVecSingleAndDouble, IsaVersion.v80, InstFlags.RdReadRdRnRmFpSimd),
                new(0x0F804000, 0xBFC0F400, szConstraints, InstName.FmlslAdvsimdEltFmlsl, IsaVersion.v82, InstFlags.RdReadRdRnRmFpSimd),
                new(0x2F80C000, 0xBFC0F400, szConstraints, InstName.FmlslAdvsimdEltFmlsl2, IsaVersion.v82, InstFlags.RdReadRdRnRmFpSimd),
                new(0x0EA0EC00, 0xBFE0FC00, szConstraints, InstName.FmlslAdvsimdVecFmlsl, IsaVersion.v82, InstFlags.RdReadRdRnRmFpSimd),
                new(0x2EA0CC00, 0xBFE0FC00, szConstraints, InstName.FmlslAdvsimdVecFmlsl2, IsaVersion.v82, InstFlags.RdReadRdRnRmFpSimd),
                new(0x5F005000, 0xFFC0F400, InstName.FmlsAdvsimdElt2regScalarHalf, IsaVersion.v82, InstFlags.RdReadRdRnRmFpSimd),
                new(0x5F805000, 0xFF80F400, szlConstraints, InstName.FmlsAdvsimdElt2regScalarSingleAndDouble, IsaVersion.v80, InstFlags.RdReadRdRnRmFpSimd),
                new(0x0F005000, 0xBFC0F400, InstName.FmlsAdvsimdElt2regElementHalf, IsaVersion.v82, InstFlags.RdReadRdRnRmFpSimd),
                new(0x0F805000, 0xBF80F400, szlQszConstraints, InstName.FmlsAdvsimdElt2regElementSingleAndDouble, IsaVersion.v80, InstFlags.RdReadRdRnRmFpSimd),
                new(0x0EC00C00, 0xBFE0FC00, InstName.FmlsAdvsimdVecHalf, IsaVersion.v82, InstFlags.RdReadRdRnRmFpSimd),
                new(0x0EA0CC00, 0xBFA0FC00, qszConstraints, InstName.FmlsAdvsimdVecSingleAndDouble, IsaVersion.v80, InstFlags.RdReadRdRnRmFpSimd),
                new(0x0F00FC00, 0xBFF8FC00, InstName.FmovAdvsimdPerHalf, IsaVersion.v82, InstFlags.RdFpSimd),
                new(0x0F00F400, 0x9FF8FC00, qConstraints, InstName.FmovAdvsimdSingleAndDouble, IsaVersion.v80, InstFlags.RdFpSimd),
                new(0x1E204000, 0xFFBFFC00, InstName.FmovFloat, IsaVersion.v80, InstFlags.RdRnFpSimd),
                new(0x1EE04000, 0xFFFFFC00, InstName.FmovFloat, IsaVersion.v80, InstFlags.RdRnFpSimd),
                new(0x1E260000, 0xFFFEFC00, sfftypermodeSfftypermodeConstraints, InstName.FmovFloatGen, IsaVersion.v80, InstFlags.RdRnSFpSimdFromToGpr),
                new(0x9E660000, 0xFFFEFC00, sfftypermodeSfftypermodeConstraints, InstName.FmovFloatGen, IsaVersion.v80, InstFlags.RdRnSFpSimdFromToGpr),
                new(0x9EAE0000, 0xFFFEFC00, sfftypermodeSfftypermodeConstraints, InstName.FmovFloatGen, IsaVersion.v80, InstFlags.RdRnSFpSimdFromToGpr),
                new(0x1EE60000, 0x7FFEFC00, sfftypermodeSfftypermodeConstraints, InstName.FmovFloatGen, IsaVersion.v80, InstFlags.RdRnSFpSimdFromToGpr),
                new(0x1E201000, 0xFFA01FE0, InstName.FmovFloatImm, IsaVersion.v80, InstFlags.RdFpSimd),
                new(0x1EE01000, 0xFFE01FE0, InstName.FmovFloatImm, IsaVersion.v80, InstFlags.RdFpSimd),
                new(0x1F008000, 0xFFA08000, InstName.FmsubFloat, IsaVersion.v80, InstFlags.RdRnRmRaFpSimd),
                new(0x1FC08000, 0xFFE08000, InstName.FmsubFloat, IsaVersion.v80, InstFlags.RdRnRmRaFpSimd),
                new(0x7F009000, 0xFFC0F400, InstName.FmulxAdvsimdElt2regScalarHalf, IsaVersion.v82, InstFlags.RdRnRmFpSimd),
                new(0x7F809000, 0xFF80F400, szlConstraints, InstName.FmulxAdvsimdElt2regScalarSingleAndDouble, IsaVersion.v80, InstFlags.RdRnRmFpSimd),
                new(0x2F009000, 0xBFC0F400, InstName.FmulxAdvsimdElt2regElementHalf, IsaVersion.v82, InstFlags.RdRnRmFpSimd),
                new(0x2F809000, 0xBF80F400, szlQszConstraints, InstName.FmulxAdvsimdElt2regElementSingleAndDouble, IsaVersion.v80, InstFlags.RdRnRmFpSimd),
                new(0x5E401C00, 0xFFE0FC00, InstName.FmulxAdvsimdVecSH, IsaVersion.v82, InstFlags.RdRnRmFpSimd),
                new(0x5E20DC00, 0xFFA0FC00, InstName.FmulxAdvsimdVecS, IsaVersion.v80, InstFlags.RdRnRmFpSimd),
                new(0x0E401C00, 0xBFE0FC00, InstName.FmulxAdvsimdVecVH, IsaVersion.v82, InstFlags.RdRnRmFpSimd),
                new(0x0E20DC00, 0xBFA0FC00, qszConstraints, InstName.FmulxAdvsimdVecV, IsaVersion.v80, InstFlags.RdRnRmFpSimd),
                new(0x5F009000, 0xFFC0F400, InstName.FmulAdvsimdElt2regScalarHalf, IsaVersion.v82, InstFlags.RdRnRmFpSimd),
                new(0x5F809000, 0xFF80F400, szlConstraints, InstName.FmulAdvsimdElt2regScalarSingleAndDouble, IsaVersion.v80, InstFlags.RdRnRmFpSimd),
                new(0x0F009000, 0xBFC0F400, InstName.FmulAdvsimdElt2regElementHalf, IsaVersion.v82, InstFlags.RdRnRmFpSimd),
                new(0x0F809000, 0xBF80F400, szlQszConstraints, InstName.FmulAdvsimdElt2regElementSingleAndDouble, IsaVersion.v80, InstFlags.RdRnRmFpSimd),
                new(0x2E401C00, 0xBFE0FC00, InstName.FmulAdvsimdVecHalf, IsaVersion.v82, InstFlags.RdRnRmFpSimd),
                new(0x2E20DC00, 0xBFA0FC00, qszConstraints, InstName.FmulAdvsimdVecSingleAndDouble, IsaVersion.v80, InstFlags.RdRnRmFpSimd),
                new(0x1E200800, 0xFFA0FC00, InstName.FmulFloat, IsaVersion.v80, InstFlags.RdRnRmFpSimd),
                new(0x1EE00800, 0xFFE0FC00, InstName.FmulFloat, IsaVersion.v80, InstFlags.RdRnRmFpSimd),
                new(0x2EF8F800, 0xBFFFFC00, InstName.FnegAdvsimdHalf, IsaVersion.v82, InstFlags.RdRnFpSimd),
                new(0x2EA0F800, 0xBFBFFC00, qszConstraints, InstName.FnegAdvsimdSingleAndDouble, IsaVersion.v80, InstFlags.RdRnFpSimd),
                new(0x1E214000, 0xFFBFFC00, InstName.FnegFloat, IsaVersion.v80, InstFlags.RdRnFpSimd),
                new(0x1EE14000, 0xFFFFFC00, InstName.FnegFloat, IsaVersion.v80, InstFlags.RdRnFpSimd),
                new(0x1F200000, 0xFFA08000, InstName.FnmaddFloat, IsaVersion.v80, InstFlags.RdRnRmRaFpSimd),
                new(0x1FE00000, 0xFFE08000, InstName.FnmaddFloat, IsaVersion.v80, InstFlags.RdRnRmRaFpSimd),
                new(0x1F208000, 0xFFA08000, InstName.FnmsubFloat, IsaVersion.v80, InstFlags.RdRnRmRaFpSimd),
                new(0x1FE08000, 0xFFE08000, InstName.FnmsubFloat, IsaVersion.v80, InstFlags.RdRnRmRaFpSimd),
                new(0x1E208800, 0xFFA0FC00, InstName.FnmulFloat, IsaVersion.v80, InstFlags.RdRnRmFpSimd),
                new(0x1EE08800, 0xFFE0FC00, InstName.FnmulFloat, IsaVersion.v80, InstFlags.RdRnRmFpSimd),
                new(0x5EF9D800, 0xFFFFFC00, InstName.FrecpeAdvsimdSH, IsaVersion.v82, InstFlags.RdRnFpSimd),
                new(0x5EA1D800, 0xFFBFFC00, InstName.FrecpeAdvsimdS, IsaVersion.v80, InstFlags.RdRnFpSimd),
                new(0x0EF9D800, 0xBFFFFC00, InstName.FrecpeAdvsimdVH, IsaVersion.v82, InstFlags.RdRnFpSimd),
                new(0x0EA1D800, 0xBFBFFC00, qszConstraints, InstName.FrecpeAdvsimdV, IsaVersion.v80, InstFlags.RdRnFpSimd),
                new(0x5E403C00, 0xFFE0FC00, InstName.FrecpsAdvsimdSH, IsaVersion.v82, InstFlags.RdRnRmFpSimd),
                new(0x5E20FC00, 0xFFA0FC00, InstName.FrecpsAdvsimdS, IsaVersion.v80, InstFlags.RdRnRmFpSimd),
                new(0x0E403C00, 0xBFE0FC00, InstName.FrecpsAdvsimdVH, IsaVersion.v82, InstFlags.RdRnRmFpSimd),
                new(0x0E20FC00, 0xBFA0FC00, qszConstraints, InstName.FrecpsAdvsimdV, IsaVersion.v80, InstFlags.RdRnRmFpSimd),
                new(0x5EF9F800, 0xFFFFFC00, InstName.FrecpxAdvsimdHalf, IsaVersion.v82, InstFlags.RdRnFpSimd),
                new(0x5EA1F800, 0xFFBFFC00, InstName.FrecpxAdvsimdSingleAndDouble, IsaVersion.v80, InstFlags.RdRnFpSimd),
                new(0x2E21E800, 0xBFBFFC00, qszConstraints, InstName.Frint32xAdvsimd, IsaVersion.v85, InstFlags.RdRnFpSimd),
                new(0x1E28C000, 0xFFBFFC00, InstName.Frint32xFloat, IsaVersion.v85, InstFlags.RdRnFpSimd),
                new(0x0E21E800, 0xBFBFFC00, qszConstraints, InstName.Frint32zAdvsimd, IsaVersion.v85, InstFlags.RdRnFpSimd),
                new(0x1E284000, 0xFFBFFC00, InstName.Frint32zFloat, IsaVersion.v85, InstFlags.RdRnFpSimd),
                new(0x2E21F800, 0xBFBFFC00, qszConstraints, InstName.Frint64xAdvsimd, IsaVersion.v85, InstFlags.RdRnFpSimd),
                new(0x1E29C000, 0xFFBFFC00, InstName.Frint64xFloat, IsaVersion.v85, InstFlags.RdRnFpSimd),
                new(0x0E21F800, 0xBFBFFC00, qszConstraints, InstName.Frint64zAdvsimd, IsaVersion.v85, InstFlags.RdRnFpSimd),
                new(0x1E294000, 0xFFBFFC00, InstName.Frint64zFloat, IsaVersion.v85, InstFlags.RdRnFpSimd),
                new(0x2E798800, 0xBFFFFC00, uo1o2Constraints, InstName.FrintaAdvsimdHalf, IsaVersion.v82, InstFlags.RdRnFpSimd),
                new(0x2E218800, 0xBFBFFC00, qszUo1o2Constraints, InstName.FrintaAdvsimdSingleAndDouble, IsaVersion.v80, InstFlags.RdRnFpSimd),
                new(0x1E264000, 0xFFBFFC00, InstName.FrintaFloat, IsaVersion.v80, InstFlags.RdRnFpSimd),
                new(0x1EE64000, 0xFFFFFC00, InstName.FrintaFloat, IsaVersion.v80, InstFlags.RdRnFpSimd),
                new(0x2EF99800, 0xBFFFFC00, uo1o2Constraints, InstName.FrintiAdvsimdHalf, IsaVersion.v82, InstFlags.RdRnFpSimd),
                new(0x2EA19800, 0xBFBFFC00, qszUo1o2Constraints, InstName.FrintiAdvsimdSingleAndDouble, IsaVersion.v80, InstFlags.RdRnFpSimd),
                new(0x1E27C000, 0xFFBFFC00, InstName.FrintiFloat, IsaVersion.v80, InstFlags.RdRnFpSimd),
                new(0x1EE7C000, 0xFFFFFC00, InstName.FrintiFloat, IsaVersion.v80, InstFlags.RdRnFpSimd),
                new(0x0E799800, 0xBFFFFC00, uo1o2Constraints, InstName.FrintmAdvsimdHalf, IsaVersion.v82, InstFlags.RdRnFpSimd),
                new(0x0E219800, 0xBFBFFC00, qszUo1o2Constraints, InstName.FrintmAdvsimdSingleAndDouble, IsaVersion.v80, InstFlags.RdRnFpSimd),
                new(0x1E254000, 0xFFBFFC00, InstName.FrintmFloat, IsaVersion.v80, InstFlags.RdRnFpSimd),
                new(0x1EE54000, 0xFFFFFC00, InstName.FrintmFloat, IsaVersion.v80, InstFlags.RdRnFpSimd),
                new(0x0E798800, 0xBFFFFC00, uo1o2Constraints, InstName.FrintnAdvsimdHalf, IsaVersion.v82, InstFlags.RdRnFpSimd),
                new(0x0E218800, 0xBFBFFC00, qszUo1o2Constraints, InstName.FrintnAdvsimdSingleAndDouble, IsaVersion.v80, InstFlags.RdRnFpSimd),
                new(0x1E244000, 0xFFBFFC00, InstName.FrintnFloat, IsaVersion.v80, InstFlags.RdRnFpSimd),
                new(0x1EE44000, 0xFFFFFC00, InstName.FrintnFloat, IsaVersion.v80, InstFlags.RdRnFpSimd),
                new(0x0EF98800, 0xBFFFFC00, uo1o2Constraints, InstName.FrintpAdvsimdHalf, IsaVersion.v82, InstFlags.RdRnFpSimd),
                new(0x0EA18800, 0xBFBFFC00, qszUo1o2Constraints, InstName.FrintpAdvsimdSingleAndDouble, IsaVersion.v80, InstFlags.RdRnFpSimd),
                new(0x1E24C000, 0xFFBFFC00, InstName.FrintpFloat, IsaVersion.v80, InstFlags.RdRnFpSimd),
                new(0x1EE4C000, 0xFFFFFC00, InstName.FrintpFloat, IsaVersion.v80, InstFlags.RdRnFpSimd),
                new(0x2E799800, 0xBFFFFC00, uo1o2Constraints, InstName.FrintxAdvsimdHalf, IsaVersion.v82, InstFlags.RdRnFpSimd),
                new(0x2E219800, 0xBFBFFC00, qszUo1o2Constraints, InstName.FrintxAdvsimdSingleAndDouble, IsaVersion.v80, InstFlags.RdRnFpSimd),
                new(0x1E274000, 0xFFBFFC00, InstName.FrintxFloat, IsaVersion.v80, InstFlags.RdRnFpSimd),
                new(0x1EE74000, 0xFFFFFC00, InstName.FrintxFloat, IsaVersion.v80, InstFlags.RdRnFpSimd),
                new(0x0EF99800, 0xBFFFFC00, uo1o2Constraints, InstName.FrintzAdvsimdHalf, IsaVersion.v82, InstFlags.RdRnFpSimd),
                new(0x0EA19800, 0xBFBFFC00, qszUo1o2Constraints, InstName.FrintzAdvsimdSingleAndDouble, IsaVersion.v80, InstFlags.RdRnFpSimd),
                new(0x1E25C000, 0xFFBFFC00, InstName.FrintzFloat, IsaVersion.v80, InstFlags.RdRnFpSimd),
                new(0x1EE5C000, 0xFFFFFC00, InstName.FrintzFloat, IsaVersion.v80, InstFlags.RdRnFpSimd),
                new(0x7EF9D800, 0xFFFFFC00, InstName.FrsqrteAdvsimdSH, IsaVersion.v82, InstFlags.RdRnFpSimd),
                new(0x7EA1D800, 0xFFBFFC00, InstName.FrsqrteAdvsimdS, IsaVersion.v80, InstFlags.RdRnFpSimd),
                new(0x2EF9D800, 0xBFFFFC00, InstName.FrsqrteAdvsimdVH, IsaVersion.v82, InstFlags.RdRnFpSimd),
                new(0x2EA1D800, 0xBFBFFC00, qszConstraints, InstName.FrsqrteAdvsimdV, IsaVersion.v80, InstFlags.RdRnFpSimd),
                new(0x5EC03C00, 0xFFE0FC00, InstName.FrsqrtsAdvsimdSH, IsaVersion.v82, InstFlags.RdRnRmFpSimd),
                new(0x5EA0FC00, 0xFFA0FC00, InstName.FrsqrtsAdvsimdS, IsaVersion.v80, InstFlags.RdRnRmFpSimd),
                new(0x0EC03C00, 0xBFE0FC00, InstName.FrsqrtsAdvsimdVH, IsaVersion.v82, InstFlags.RdRnRmFpSimd),
                new(0x0EA0FC00, 0xBFA0FC00, qszConstraints, InstName.FrsqrtsAdvsimdV, IsaVersion.v80, InstFlags.RdRnRmFpSimd),
                new(0x2EF9F800, 0xBFFFFC00, InstName.FsqrtAdvsimdHalf, IsaVersion.v82, InstFlags.RdRnFpSimd),
                new(0x2EA1F800, 0xBFBFFC00, qszConstraints, InstName.FsqrtAdvsimdSingleAndDouble, IsaVersion.v80, InstFlags.RdRnFpSimd),
                new(0x1E21C000, 0xFFBFFC00, InstName.FsqrtFloat, IsaVersion.v80, InstFlags.RdRnFpSimd),
                new(0x1EE1C000, 0xFFFFFC00, InstName.FsqrtFloat, IsaVersion.v80, InstFlags.RdRnFpSimd),
                new(0x0EC01400, 0xBFE0FC00, InstName.FsubAdvsimdHalf, IsaVersion.v82, InstFlags.RdRnRmFpSimd),
                new(0x0EA0D400, 0xBFA0FC00, qszConstraints, InstName.FsubAdvsimdSingleAndDouble, IsaVersion.v80, InstFlags.RdRnRmFpSimd),
                new(0x1E203800, 0xFFA0FC00, InstName.FsubFloat, IsaVersion.v80, InstFlags.RdRnRmFpSimd),
                new(0x1EE03800, 0xFFE0FC00, InstName.FsubFloat, IsaVersion.v80, InstFlags.RdRnRmFpSimd),
                new(0xD503227F, 0xFFFFFFFF, InstName.Gcsb, IsaVersion.None, InstFlags.None),
                new(0xD91F0C00, 0xFFFFFC00, InstName.Gcsstr, IsaVersion.None, InstFlags.RtReadRtRnSP),
                new(0xD91F1C00, 0xFFFFFC00, InstName.Gcssttr, IsaVersion.None, InstFlags.RtReadRtRnSP),
                new(0x9AC01400, 0xFFE0FC00, InstName.Gmi, IsaVersion.v85, InstFlags.None),
                new(0xD503201F, 0xFFFFF01F, InstName.Hint, IsaVersion.v80, InstFlags.None),
                new(0xD4400000, 0xFFE0001F, InstName.Hlt, IsaVersion.v80, InstFlags.None),
                new(0xD4000002, 0xFFE0001F, InstName.Hvc, IsaVersion.v80, InstFlags.None),
                new(0x6E000400, 0xFFE08400, imm5Constraints, InstName.InsAdvsimdElt, IsaVersion.v80, InstFlags.RdReadRdRnFpSimd),
                new(0x4E001C00, 0xFFE0FC00, imm5Constraints, InstName.InsAdvsimdGen, IsaVersion.v80, InstFlags.RdReadRdRnFpSimdFromGpr),
                new(0x9AC01000, 0xFFE0FC00, InstName.Irg, IsaVersion.v85, InstFlags.None),
                new(0xD50330DF, 0xFFFFF0FF, InstName.Isb, IsaVersion.v80, InstFlags.None),
                new(0x0D40C000, 0xBFFFF000, sConstraints, InstName.Ld1rAdvsimdAsNoPostIndex, IsaVersion.v80, InstFlags.RtRnSPFpSimd, AddressForm.StructNoOffset),
                new(0x0DC0C000, 0xBFE0F000, sConstraints, InstName.Ld1rAdvsimdAsPostIndex, IsaVersion.v80, InstFlags.RtRnSPRmFpSimdMemWBack, AddressForm.StructPostIndexedReg),
                new(0x0C402000, 0xBFFFF000, InstName.Ld1AdvsimdMultAsNoPostIndex, IsaVersion.v80, InstFlags.RtSeqRnSPFpSimd, AddressForm.StructNoOffset),
                new(0x0C406000, 0xBFFFF000, InstName.Ld1AdvsimdMultAsNoPostIndex, IsaVersion.v80, InstFlags.RtSeqRnSPFpSimd, AddressForm.StructNoOffset),
                new(0x0C407000, 0xBFFFF000, InstName.Ld1AdvsimdMultAsNoPostIndex, IsaVersion.v80, InstFlags.RtSeqRnSPFpSimd, AddressForm.StructNoOffset),
                new(0x0C40A000, 0xBFFFF000, InstName.Ld1AdvsimdMultAsNoPostIndex, IsaVersion.v80, InstFlags.RtSeqRnSPFpSimd, AddressForm.StructNoOffset),
                new(0x0C406000, 0xBFFFF000, InstName.Ld1AdvsimdMultAsNoPostIndex, IsaVersion.v80, InstFlags.RtSeqRnSPFpSimd, AddressForm.StructNoOffset),
                new(0x0C407000, 0xBFFFF000, InstName.Ld1AdvsimdMultAsNoPostIndex, IsaVersion.v80, InstFlags.RtSeqRnSPFpSimd, AddressForm.StructNoOffset),
                new(0x0C40A000, 0xBFFFF000, InstName.Ld1AdvsimdMultAsNoPostIndex, IsaVersion.v80, InstFlags.RtSeqRnSPFpSimd, AddressForm.StructNoOffset),
                new(0x0CC02000, 0xBFE0F000, InstName.Ld1AdvsimdMultAsPostIndex, IsaVersion.v80, InstFlags.RtSeqRnSPRmFpSimdMemWBack, AddressForm.StructPostIndexedReg),
                new(0x0CC06000, 0xBFE0F000, InstName.Ld1AdvsimdMultAsPostIndex, IsaVersion.v80, InstFlags.RtSeqRnSPRmFpSimdMemWBack, AddressForm.StructPostIndexedReg),
                new(0x0CC07000, 0xBFE0F000, InstName.Ld1AdvsimdMultAsPostIndex, IsaVersion.v80, InstFlags.RtSeqRnSPRmFpSimdMemWBack, AddressForm.StructPostIndexedReg),
                new(0x0CC0A000, 0xBFE0F000, InstName.Ld1AdvsimdMultAsPostIndex, IsaVersion.v80, InstFlags.RtSeqRnSPRmFpSimdMemWBack, AddressForm.StructPostIndexedReg),
                new(0x0CC06000, 0xBFE0F000, InstName.Ld1AdvsimdMultAsPostIndex, IsaVersion.v80, InstFlags.RtSeqRnSPRmFpSimdMemWBack, AddressForm.StructPostIndexedReg),
                new(0x0CC07000, 0xBFE0F000, InstName.Ld1AdvsimdMultAsPostIndex, IsaVersion.v80, InstFlags.RtSeqRnSPRmFpSimdMemWBack, AddressForm.StructPostIndexedReg),
                new(0x0CC0A000, 0xBFE0F000, InstName.Ld1AdvsimdMultAsPostIndex, IsaVersion.v80, InstFlags.RtSeqRnSPRmFpSimdMemWBack, AddressForm.StructPostIndexedReg),
                new(0x0D400000, 0xBFFF2000, opcodesizeOpcodesizeOpcodesizesOpcodesizeConstraints, InstName.Ld1AdvsimdSnglAsNoPostIndex, IsaVersion.v80, InstFlags.RtReadRtRnSPFpSimd, AddressForm.StructNoOffset),
                new(0x0DC00000, 0xBFE02000, opcodesizeOpcodesizeOpcodesizesOpcodesizeConstraints, InstName.Ld1AdvsimdSnglAsPostIndex, IsaVersion.v80, InstFlags.RtReadRtRnSPRmFpSimdMemWBack, AddressForm.StructPostIndexedReg),
                new(0x0D60C000, 0xBFFFF000, sConstraints, InstName.Ld2rAdvsimdAsNoPostIndex, IsaVersion.v80, InstFlags.RtSeqRnSPFpSimd, AddressForm.StructNoOffset),
                new(0x0DE0C000, 0xBFE0F000, sConstraints, InstName.Ld2rAdvsimdAsPostIndex, IsaVersion.v80, InstFlags.RtSeqRnSPRmFpSimdMemWBack, AddressForm.StructPostIndexedReg),
                new(0x0C408000, 0xBFFFF000, qsizeConstraints2, InstName.Ld2AdvsimdMultAsNoPostIndex, IsaVersion.v80, InstFlags.RtSeqRnSPFpSimd, AddressForm.StructNoOffset),
                new(0x0CC08000, 0xBFE0F000, qsizeConstraints2, InstName.Ld2AdvsimdMultAsPostIndex, IsaVersion.v80, InstFlags.RtSeqRnSPRmFpSimdMemWBack, AddressForm.StructPostIndexedReg),
                new(0x0D600000, 0xBFFF2000, opcodesizeOpcodesizeOpcodesizesOpcodesizeConstraints, InstName.Ld2AdvsimdSnglAsNoPostIndex, IsaVersion.v80, InstFlags.RtSeqReadRtRnSPFpSimd, AddressForm.StructNoOffset),
                new(0x0DE00000, 0xBFE02000, opcodesizeOpcodesizeOpcodesizesOpcodesizeConstraints, InstName.Ld2AdvsimdSnglAsPostIndex, IsaVersion.v80, InstFlags.RtSeqReadRtRnSPRmFpSimdMemWBack, AddressForm.StructPostIndexedReg),
                new(0x0D40E000, 0xBFFFF000, sConstraints, InstName.Ld3rAdvsimdAsNoPostIndex, IsaVersion.v80, InstFlags.RtSeqRnSPFpSimd, AddressForm.StructNoOffset),
                new(0x0DC0E000, 0xBFE0F000, sConstraints, InstName.Ld3rAdvsimdAsPostIndex, IsaVersion.v80, InstFlags.RtSeqRnSPRmFpSimdMemWBack, AddressForm.StructPostIndexedReg),
                new(0x0C404000, 0xBFFFF000, qsizeConstraints2, InstName.Ld3AdvsimdMultAsNoPostIndex, IsaVersion.v80, InstFlags.RtSeqRnSPFpSimd, AddressForm.StructNoOffset),
                new(0x0CC04000, 0xBFE0F000, qsizeConstraints2, InstName.Ld3AdvsimdMultAsPostIndex, IsaVersion.v80, InstFlags.RtSeqRnSPRmFpSimdMemWBack, AddressForm.StructPostIndexedReg),
                new(0x0D402000, 0xBFFF2000, opcodesizeOpcodesizeOpcodesizesOpcodesizeConstraints, InstName.Ld3AdvsimdSnglAsNoPostIndex, IsaVersion.v80, InstFlags.RtSeqReadRtRnSPFpSimd, AddressForm.StructNoOffset),
                new(0x0DC02000, 0xBFE02000, opcodesizeOpcodesizeOpcodesizesOpcodesizeConstraints, InstName.Ld3AdvsimdSnglAsPostIndex, IsaVersion.v80, InstFlags.RtSeqReadRtRnSPRmFpSimdMemWBack, AddressForm.StructPostIndexedReg),
                new(0x0D60E000, 0xBFFFF000, sConstraints, InstName.Ld4rAdvsimdAsNoPostIndex, IsaVersion.v80, InstFlags.RtSeqRnSPFpSimd, AddressForm.StructNoOffset),
                new(0x0DE0E000, 0xBFE0F000, sConstraints, InstName.Ld4rAdvsimdAsPostIndex, IsaVersion.v80, InstFlags.RtSeqRnSPRmFpSimdMemWBack, AddressForm.StructPostIndexedReg),
                new(0x0C400000, 0xBFFFF000, qsizeConstraints2, InstName.Ld4AdvsimdMultAsNoPostIndex, IsaVersion.v80, InstFlags.RtSeqRnSPFpSimd, AddressForm.StructNoOffset),
                new(0x0CC00000, 0xBFE0F000, qsizeConstraints2, InstName.Ld4AdvsimdMultAsPostIndex, IsaVersion.v80, InstFlags.RtSeqRnSPRmFpSimdMemWBack, AddressForm.StructPostIndexedReg),
                new(0x0D602000, 0xBFFF2000, opcodesizeOpcodesizeOpcodesizesOpcodesizeConstraints, InstName.Ld4AdvsimdSnglAsNoPostIndex, IsaVersion.v80, InstFlags.RtSeqReadRtRnSPFpSimd, AddressForm.StructNoOffset),
                new(0x0DE02000, 0xBFE02000, opcodesizeOpcodesizeOpcodesizesOpcodesizeConstraints, InstName.Ld4AdvsimdSnglAsPostIndex, IsaVersion.v80, InstFlags.RtSeqReadRtRnSPRmFpSimdMemWBack, AddressForm.StructPostIndexedReg),
                new(0xF83FD000, 0xFFFFFC00, rtRtConstraints, InstName.Ld64b, IsaVersion.v87, InstFlags.RtRnSP),
                new(0xB8200000, 0xBF20FC00, InstName.Ldadd, IsaVersion.v81, InstFlags.RtRnSPRs),
                new(0x38200000, 0xFF20FC00, InstName.Ldaddb, IsaVersion.v81, InstFlags.RtRnSPRs),
                new(0x78200000, 0xFF20FC00, InstName.Ldaddh, IsaVersion.v81, InstFlags.RtRnSPRs),
                new(0x0D418400, 0xBFFFFC00, opcodesizeOpcodesizeOpcodesizesOpcodesizeConstraints, InstName.Ldap1AdvsimdSngl, IsaVersion.v82, InstFlags.RtReadRtRnSPFpSimd, AddressForm.StructNoOffset),
                new(0xB8BFC000, 0xBFFFFC00, InstName.LdaprBaseRegister, IsaVersion.v83, InstFlags.RtRnSP, AddressForm.BaseRegister),
                new(0x99C00800, 0xBFFFFC00, InstName.LdaprPostIndexed, IsaVersion.v82, InstFlags.RtRnSPMemWBack, AddressForm.PostIndexed),
                new(0x38BFC000, 0xFFFFFC00, InstName.Ldaprb, IsaVersion.v83, InstFlags.RtRnSP),
                new(0x78BFC000, 0xFFFFFC00, InstName.Ldaprh, IsaVersion.v83, InstFlags.RtRnSP),
                new(0x19400000, 0xFFE00C00, InstName.Ldapurb, IsaVersion.v84, InstFlags.RtRnSP, AddressForm.BasePlusOffset),
                new(0x59400000, 0xFFE00C00, InstName.Ldapurh, IsaVersion.v84, InstFlags.RtRnSP, AddressForm.BasePlusOffset),
                new(0x19800000, 0xFFA00C00, InstName.Ldapursb, IsaVersion.v84, InstFlags.RtRnSP, AddressForm.BasePlusOffset),
                new(0x59800000, 0xFFA00C00, InstName.Ldapursh, IsaVersion.v84, InstFlags.RtRnSP, AddressForm.BasePlusOffset),
                new(0x99800000, 0xFFE00C00, InstName.Ldapursw, IsaVersion.v84, InstFlags.RtRnSP, AddressForm.BasePlusOffset),
                new(0x1D400800, 0x3F600C00, opc1sizeOpc1sizeOpc1sizeConstraints, InstName.LdapurFpsimd, IsaVersion.v82, InstFlags.RtRnSPFpSimd, AddressForm.BasePlusOffset),
                new(0x99400000, 0xBFE00C00, InstName.LdapurGen, IsaVersion.v84, InstFlags.RtRnSP, AddressForm.BasePlusOffset),
                new(0x88DFFC00, 0xBFFFFC00, InstName.Ldar, IsaVersion.v80, InstFlags.RtRnSP, AddressForm.BaseRegister),
                new(0x08DFFC00, 0xFFFFFC00, InstName.Ldarb, IsaVersion.v80, InstFlags.RtRnSP, AddressForm.BaseRegister),
                new(0x48DFFC00, 0xFFFFFC00, InstName.Ldarh, IsaVersion.v80, InstFlags.RtRnSP, AddressForm.BaseRegister),
                new(0x887F8000, 0xBFFF8000, InstName.Ldaxp, IsaVersion.v80, InstFlags.RtRt2RnSP, AddressForm.BaseRegister),
                new(0x885FFC00, 0xBFFFFC00, InstName.Ldaxr, IsaVersion.v80, InstFlags.RtRnSP, AddressForm.BaseRegister),
                new(0x085FFC00, 0xFFFFFC00, InstName.Ldaxrb, IsaVersion.v80, InstFlags.RtRnSP, AddressForm.BaseRegister),
                new(0x485FFC00, 0xFFFFFC00, InstName.Ldaxrh, IsaVersion.v80, InstFlags.RtRnSP, AddressForm.BaseRegister),
                new(0xB8201000, 0xBF20FC00, InstName.Ldclr, IsaVersion.v81, InstFlags.RtRnSPRs),
                new(0x38201000, 0xFF20FC00, InstName.Ldclrb, IsaVersion.v81, InstFlags.RtRnSPRs),
                new(0x78201000, 0xFF20FC00, InstName.Ldclrh, IsaVersion.v81, InstFlags.RtRnSPRs),
                new(0x19201000, 0xFF20FC00, rtRt2Constraints, InstName.Ldclrp, IsaVersion.None, InstFlags.RtRt2RnSP),
                new(0xB8202000, 0xBF20FC00, InstName.Ldeor, IsaVersion.v81, InstFlags.RtRnSPRs),
                new(0x38202000, 0xFF20FC00, InstName.Ldeorb, IsaVersion.v81, InstFlags.RtRnSPRs),
                new(0x78202000, 0xFF20FC00, InstName.Ldeorh, IsaVersion.v81, InstFlags.RtRnSPRs),
                new(0xD9600000, 0xFFE00C00, InstName.Ldg, IsaVersion.v85, InstFlags.None),
                new(0xD9E00000, 0xFFFFFC00, InstName.Ldgm, IsaVersion.v85, InstFlags.None),
                new(0x99400800, 0xBFE0EC00, InstName.Ldiapp, IsaVersion.v82, InstFlags.RtRt2RnSP),
                new(0x88DF7C00, 0xBFFFFC00, InstName.Ldlar, IsaVersion.v81, InstFlags.RtRnSP, AddressForm.BaseRegister),
                new(0x08DF7C00, 0xFFFFFC00, InstName.Ldlarb, IsaVersion.v81, InstFlags.RtRnSP, AddressForm.BaseRegister),
                new(0x48DF7C00, 0xFFFFFC00, InstName.Ldlarh, IsaVersion.v81, InstFlags.RtRnSP, AddressForm.BaseRegister),
                new(0x2C400000, 0x3FC00000, opcConstraints, InstName.LdnpFpsimd, IsaVersion.v80, InstFlags.RtRt2RnSPFpSimd, AddressForm.SignedScaled),
                new(0x28400000, 0x7FC00000, opcConstraints2, InstName.LdnpGen, IsaVersion.v80, InstFlags.RtRt2RnSP, AddressForm.SignedScaled),
                new(0x68C00000, 0xFFC00000, InstName.LdpswPostIndexed, IsaVersion.v80, InstFlags.RtRt2RnSPMemWBack, AddressForm.PostIndexed),
                new(0x69C00000, 0xFFC00000, InstName.LdpswPreIndexed, IsaVersion.v80, InstFlags.RtRt2RnSPMemWBack, AddressForm.PreIndexed),
                new(0x69400000, 0xFFC00000, InstName.LdpswSignedScaledOffset, IsaVersion.v80, InstFlags.RtRt2RnSP, AddressForm.SignedScaled),
                new(0x2CC00000, 0x3FC00000, opcConstraints, InstName.LdpFpsimdPostIndexed, IsaVersion.v80, InstFlags.RtRt2RnSPFpSimdMemWBack, AddressForm.PostIndexed),
                new(0x2DC00000, 0x3FC00000, opcConstraints, InstName.LdpFpsimdPreIndexed, IsaVersion.v80, InstFlags.RtRt2RnSPFpSimdMemWBack, AddressForm.PreIndexed),
                new(0x2D400000, 0x3FC00000, opcConstraints, InstName.LdpFpsimdSignedScaledOffset, IsaVersion.v80, InstFlags.RtRt2RnSPFpSimd, AddressForm.SignedScaled),
                new(0x28C00000, 0x7FC00000, opclOpcConstraints, InstName.LdpGenPostIndexed, IsaVersion.v80, InstFlags.RtRt2RnSPMemWBack, AddressForm.PostIndexed),
                new(0x29C00000, 0x7FC00000, opclOpcConstraints, InstName.LdpGenPreIndexed, IsaVersion.v80, InstFlags.RtRt2RnSPMemWBack, AddressForm.PreIndexed),
                new(0x29400000, 0x7FC00000, opclOpcConstraints, InstName.LdpGenSignedScaledOffset, IsaVersion.v80, InstFlags.RtRt2RnSP, AddressForm.SignedScaled),
                new(0xF8200400, 0xFF200400, InstName.Ldra, IsaVersion.v83, InstFlags.RtRnSP, AddressForm.BasePlusOffset),
                new(0x38400400, 0xFFE00C00, InstName.LdrbImmPostIndexed, IsaVersion.v80, InstFlags.RtRnSPMemWBack, AddressForm.PostIndexed),
                new(0x38400C00, 0xFFE00C00, InstName.LdrbImmPreIndexed, IsaVersion.v80, InstFlags.RtRnSPMemWBack, AddressForm.PreIndexed),
                new(0x39400000, 0xFFC00000, InstName.LdrbImmUnsignedScaledOffset, IsaVersion.v80, InstFlags.RtRnSP, AddressForm.UnsignedScaled),
                new(0x38600800, 0xFFE00C00, optionConstraints, InstName.LdrbReg, IsaVersion.v80, InstFlags.RtRnSPRm, AddressForm.OffsetReg),
                new(0x78400400, 0xFFE00C00, InstName.LdrhImmPostIndexed, IsaVersion.v80, InstFlags.RtRnSPMemWBack, AddressForm.PostIndexed),
                new(0x78400C00, 0xFFE00C00, InstName.LdrhImmPreIndexed, IsaVersion.v80, InstFlags.RtRnSPMemWBack, AddressForm.PreIndexed),
                new(0x79400000, 0xFFC00000, InstName.LdrhImmUnsignedScaledOffset, IsaVersion.v80, InstFlags.RtRnSP, AddressForm.UnsignedScaled),
                new(0x78600800, 0xFFE00C00, optionConstraints, InstName.LdrhReg, IsaVersion.v80, InstFlags.RtRnSPRm, AddressForm.OffsetReg),
                new(0x38800400, 0xFFA00C00, InstName.LdrsbImmPostIndexed, IsaVersion.v80, InstFlags.RtRnSPMemWBack, AddressForm.PostIndexed),
                new(0x38800C00, 0xFFA00C00, InstName.LdrsbImmPreIndexed, IsaVersion.v80, InstFlags.RtRnSPMemWBack, AddressForm.PreIndexed),
                new(0x39800000, 0xFF800000, InstName.LdrsbImmUnsignedScaledOffset, IsaVersion.v80, InstFlags.RtRnSP, AddressForm.UnsignedScaled),
                new(0x38A00800, 0xFFA00C00, optionConstraints, InstName.LdrsbReg, IsaVersion.v80, InstFlags.RtRnSPRm, AddressForm.OffsetReg),
                new(0x78800400, 0xFFA00C00, InstName.LdrshImmPostIndexed, IsaVersion.v80, InstFlags.RtRnSPMemWBack, AddressForm.PostIndexed),
                new(0x78800C00, 0xFFA00C00, InstName.LdrshImmPreIndexed, IsaVersion.v80, InstFlags.RtRnSPMemWBack, AddressForm.PreIndexed),
                new(0x79800000, 0xFF800000, InstName.LdrshImmUnsignedScaledOffset, IsaVersion.v80, InstFlags.RtRnSP, AddressForm.UnsignedScaled),
                new(0x78A00800, 0xFFA00C00, optionConstraints, InstName.LdrshReg, IsaVersion.v80, InstFlags.RtRnSPRm, AddressForm.OffsetReg),
                new(0xB8800400, 0xFFE00C00, InstName.LdrswImmPostIndexed, IsaVersion.v80, InstFlags.RtRnSPMemWBack, AddressForm.PostIndexed),
                new(0xB8800C00, 0xFFE00C00, InstName.LdrswImmPreIndexed, IsaVersion.v80, InstFlags.RtRnSPMemWBack, AddressForm.PreIndexed),
                new(0xB9800000, 0xFFC00000, InstName.LdrswImmUnsignedScaledOffset, IsaVersion.v80, InstFlags.RtRnSP, AddressForm.UnsignedScaled),
                new(0x98000000, 0xFF000000, InstName.LdrswLit, IsaVersion.v80, InstFlags.Rt, AddressForm.Literal),
                new(0xB8A00800, 0xFFE00C00, optionConstraints, InstName.LdrswReg, IsaVersion.v80, InstFlags.RtRnSPRm, AddressForm.OffsetReg),
                new(0x3C400400, 0x3F600C00, opc1sizeOpc1sizeOpc1sizeConstraints, InstName.LdrImmFpsimdPostIndexed, IsaVersion.v80, InstFlags.RtRnSPFpSimdMemWBack, AddressForm.PostIndexed),
                new(0x3C400C00, 0x3F600C00, opc1sizeOpc1sizeOpc1sizeConstraints, InstName.LdrImmFpsimdPreIndexed, IsaVersion.v80, InstFlags.RtRnSPFpSimdMemWBack, AddressForm.PreIndexed),
                new(0x3D400000, 0x3F400000, opc1sizeOpc1sizeOpc1sizeConstraints, InstName.LdrImmFpsimdUnsignedScaledOffset, IsaVersion.v80, InstFlags.RtRnSPFpSimd, AddressForm.UnsignedScaled),
                new(0xB8400400, 0xBFE00C00, InstName.LdrImmGenPostIndexed, IsaVersion.v80, InstFlags.RtRnSPMemWBack, AddressForm.PostIndexed),
                new(0xB8400C00, 0xBFE00C00, InstName.LdrImmGenPreIndexed, IsaVersion.v80, InstFlags.RtRnSPMemWBack, AddressForm.PreIndexed),
                new(0xB9400000, 0xBFC00000, InstName.LdrImmGenUnsignedScaledOffset, IsaVersion.v80, InstFlags.RtRnSP, AddressForm.UnsignedScaled),
                new(0x1C000000, 0x3F000000, opcConstraints, InstName.LdrLitFpsimd, IsaVersion.v80, InstFlags.RtFpSimd, AddressForm.Literal),
                new(0x18000000, 0xBF000000, InstName.LdrLitGen, IsaVersion.v80, InstFlags.Rt, AddressForm.Literal),
                new(0x3C600800, 0x3F600C00, opc1sizeOpc1sizeOpc1sizeOptionConstraints, InstName.LdrRegFpsimd, IsaVersion.v80, InstFlags.RtRnSPRmFpSimd, AddressForm.OffsetReg),
                new(0xB8600800, 0xBFE00C00, optionConstraints, InstName.LdrRegGen, IsaVersion.v80, InstFlags.RtRnSPRm, AddressForm.OffsetReg),
                new(0xB8203000, 0xBF20FC00, InstName.Ldset, IsaVersion.v81, InstFlags.RtRnSPRs),
                new(0x38203000, 0xFF20FC00, InstName.Ldsetb, IsaVersion.v81, InstFlags.RtRnSPRs),
                new(0x78203000, 0xFF20FC00, InstName.Ldseth, IsaVersion.v81, InstFlags.RtRnSPRs),
                new(0x19203000, 0xFF20FC00, rtRt2Constraints, InstName.Ldsetp, IsaVersion.None, InstFlags.RtRt2RnSP),
                new(0xB8204000, 0xBF20FC00, InstName.Ldsmax, IsaVersion.v81, InstFlags.RtRnSPRs),
                new(0x38204000, 0xFF20FC00, InstName.Ldsmaxb, IsaVersion.v81, InstFlags.RtRnSPRs),
                new(0x78204000, 0xFF20FC00, InstName.Ldsmaxh, IsaVersion.v81, InstFlags.RtRnSPRs),
                new(0xB8205000, 0xBF20FC00, InstName.Ldsmin, IsaVersion.v81, InstFlags.RtRnSPRs),
                new(0x38205000, 0xFF20FC00, InstName.Ldsminb, IsaVersion.v81, InstFlags.RtRnSPRs),
                new(0x78205000, 0xFF20FC00, InstName.Ldsminh, IsaVersion.v81, InstFlags.RtRnSPRs),
                new(0xB8400800, 0xBFE00C00, InstName.Ldtr, IsaVersion.v80, InstFlags.RtRnSP, AddressForm.BasePlusOffset),
                new(0x38400800, 0xFFE00C00, InstName.Ldtrb, IsaVersion.v80, InstFlags.RtRnSP, AddressForm.BasePlusOffset),
                new(0x78400800, 0xFFE00C00, InstName.Ldtrh, IsaVersion.v80, InstFlags.RtRnSP, AddressForm.BasePlusOffset),
                new(0x38800800, 0xFFA00C00, InstName.Ldtrsb, IsaVersion.v80, InstFlags.RtRnSP, AddressForm.BasePlusOffset),
                new(0x78800800, 0xFFA00C00, InstName.Ldtrsh, IsaVersion.v80, InstFlags.RtRnSP, AddressForm.BasePlusOffset),
                new(0xB8800800, 0xFFE00C00, InstName.Ldtrsw, IsaVersion.v80, InstFlags.RtRnSP, AddressForm.BasePlusOffset),
                new(0xB8206000, 0xBF20FC00, InstName.Ldumax, IsaVersion.v81, InstFlags.RtRnSPRs),
                new(0x38206000, 0xFF20FC00, InstName.Ldumaxb, IsaVersion.v81, InstFlags.RtRnSPRs),
                new(0x78206000, 0xFF20FC00, InstName.Ldumaxh, IsaVersion.v81, InstFlags.RtRnSPRs),
                new(0xB8207000, 0xBF20FC00, InstName.Ldumin, IsaVersion.v81, InstFlags.RtRnSPRs),
                new(0x38207000, 0xFF20FC00, InstName.Lduminb, IsaVersion.v81, InstFlags.RtRnSPRs),
                new(0x78207000, 0xFF20FC00, InstName.Lduminh, IsaVersion.v81, InstFlags.RtRnSPRs),
                new(0x38400000, 0xFFE00C00, InstName.Ldurb, IsaVersion.v80, InstFlags.RtRnSP, AddressForm.BasePlusOffset),
                new(0x78400000, 0xFFE00C00, InstName.Ldurh, IsaVersion.v80, InstFlags.RtRnSP, AddressForm.BasePlusOffset),
                new(0x38800000, 0xFFA00C00, InstName.Ldursb, IsaVersion.v80, InstFlags.RtRnSP, AddressForm.BasePlusOffset),
                new(0x78800000, 0xFFA00C00, InstName.Ldursh, IsaVersion.v80, InstFlags.RtRnSP, AddressForm.BasePlusOffset),
                new(0xB8800000, 0xFFE00C00, InstName.Ldursw, IsaVersion.v80, InstFlags.RtRnSP, AddressForm.BasePlusOffset),
                new(0x3C400000, 0x3F600C00, opc1sizeOpc1sizeOpc1sizeConstraints, InstName.LdurFpsimd, IsaVersion.v80, InstFlags.RtRnSPFpSimd, AddressForm.BasePlusOffset),
                new(0xB8400000, 0xBFE00C00, InstName.LdurGen, IsaVersion.v80, InstFlags.RtRnSP, AddressForm.BasePlusOffset),
                new(0x887F0000, 0xBFFF8000, InstName.Ldxp, IsaVersion.v80, InstFlags.RtRt2RnSP, AddressForm.BaseRegister),
                new(0x885F7C00, 0xBFFFFC00, InstName.Ldxr, IsaVersion.v80, InstFlags.RtRnSP, AddressForm.BaseRegister),
                new(0x085F7C00, 0xFFFFFC00, InstName.Ldxrb, IsaVersion.v80, InstFlags.RtRnSP, AddressForm.BaseRegister),
                new(0x485F7C00, 0xFFFFFC00, InstName.Ldxrh, IsaVersion.v80, InstFlags.RtRnSP, AddressForm.BaseRegister),
                new(0x1AC02000, 0x7FE0FC00, InstName.Lslv, IsaVersion.v80, InstFlags.RdRnRm),
                new(0x1AC02400, 0x7FE0FC00, InstName.Lsrv, IsaVersion.v80, InstFlags.RdRnRm),
                new(0x1B000000, 0x7FE08000, InstName.Madd, IsaVersion.v80, InstFlags.RdRnRmRa),
                new(0x2F000000, 0xBF00F400, sizeSizeConstraints, InstName.MlaAdvsimdElt, IsaVersion.v80, InstFlags.RdReadRdRnRmFpSimd),
                new(0x0E209400, 0xBF20FC00, sizeConstraints, InstName.MlaAdvsimdVec, IsaVersion.v80, InstFlags.RdReadRdRnRmFpSimd),
                new(0x2F004000, 0xBF00F400, sizeSizeConstraints, InstName.MlsAdvsimdElt, IsaVersion.v80, InstFlags.RdReadRdRnRmFpSimd),
                new(0x2E209400, 0xBF20FC00, sizeConstraints, InstName.MlsAdvsimdVec, IsaVersion.v80, InstFlags.RdReadRdRnRmFpSimd),
                new(0x0F000400, 0x9FF80C00, cmodeopqConstraints, InstName.MoviAdvsimd, IsaVersion.v80, InstFlags.RdFpSimd),
                new(0x72800000, 0x7F800000, sfhwConstraints, InstName.Movk, IsaVersion.v80, InstFlags.RdReadRd),
                new(0x12800000, 0x7F800000, sfhwConstraints, InstName.Movn, IsaVersion.v80, InstFlags.Rd),
                new(0x52800000, 0x7F800000, sfhwConstraints, InstName.Movz, IsaVersion.v80, InstFlags.Rd),
                new(0xD5700000, 0xFFF00000, rtConstraints, InstName.Mrrs, IsaVersion.None, InstFlags.RtReadRt),
                new(0xD5300000, 0xFFF00000, InstName.Mrs, IsaVersion.v80, InstFlags.Rt),
                new(0xD5500000, 0xFFF00000, rtConstraints, InstName.Msrr, IsaVersion.None, InstFlags.RtReadRt),
                new(0xD500401F, 0xFFF8F01F, InstName.MsrImm, IsaVersion.v80, InstFlags.None),
                new(0xD5100000, 0xFFF00000, InstName.MsrReg, IsaVersion.v80, InstFlags.RtReadRt),
                new(0x1B008000, 0x7FE08000, InstName.Msub, IsaVersion.v80, InstFlags.RdRnRmRa),
                new(0x0F008000, 0xBF00F400, sizeSizeConstraints, InstName.MulAdvsimdElt, IsaVersion.v80, InstFlags.RdRnRmFpSimd),
                new(0x0E209C00, 0xBF20FC00, usizeUsizeUsizeSizeConstraints, InstName.MulAdvsimdVec, IsaVersion.v80, InstFlags.RdRnRmFpSimd),
                new(0x2F000400, 0xBFF80C00, cmodeopqConstraints, InstName.MvniAdvsimd, IsaVersion.v80, InstFlags.RdFpSimd),
                new(0x7EE0B800, 0xFFFFFC00, InstName.NegAdvsimdS, IsaVersion.v80, InstFlags.RdRnFpSimd),
                new(0x2E20B800, 0xBF3FFC00, qsizeConstraints, InstName.NegAdvsimdV, IsaVersion.v80, InstFlags.RdRnFpSimd),
                new(0xD503201F, 0xFFFFFFFF, InstName.Nop, IsaVersion.v80, InstFlags.None),
                new(0x2E205800, 0xBFFFFC00, InstName.NotAdvsimd, IsaVersion.v80, InstFlags.RdRnFpSimd),
                new(0x0EE01C00, 0xBFE0FC00, InstName.OrnAdvsimd, IsaVersion.v80, InstFlags.RdRnRmFpSimd),
                new(0x2A200000, 0x7F200000, sfimm6Constraints, InstName.OrnLogShift, IsaVersion.v80, InstFlags.RdRnRm),
                new(0x0F001400, 0xBFF81C00, InstName.OrrAdvsimdImm, IsaVersion.v80, InstFlags.RdFpSimd),
                new(0x0EA01C00, 0xBFE0FC00, InstName.OrrAdvsimdReg, IsaVersion.v80, InstFlags.RdRnRmFpSimd),
                new(0x32000000, 0x7F800000, nimmsNimmsNimmsNimmsNimmsNimmsNimmsNimmsSfnConstraints, InstName.OrrLogImm, IsaVersion.v80, InstFlags.RdSPRn),
                new(0x2A000000, 0x7F200000, sfimm6Constraints, InstName.OrrLogShift, IsaVersion.v80, InstFlags.RdRnRm),
                new(0xDAC10800, 0xFFFFDC00, InstName.Pacda, IsaVersion.v83, InstFlags.RdRnSP),
                new(0xDAC10C00, 0xFFFFDC00, InstName.Pacdb, IsaVersion.v83, InstFlags.RdRnSP),
                new(0x9AC03000, 0xFFE0FC00, InstName.Pacga, IsaVersion.v83, InstFlags.RdRnRm),
                new(0xDAC10000, 0xFFFFDC00, InstName.PaciaGeneral, IsaVersion.v83, InstFlags.RdRnSP),
                new(0xD503211F, 0xFFFFFDDF, InstName.PaciaSystem, IsaVersion.v83, InstFlags.None),
                new(0xDAC10400, 0xFFFFDC00, InstName.PacibGeneral, IsaVersion.v83, InstFlags.RdRnSP),
                new(0xD503215F, 0xFFFFFDDF, InstName.PacibSystem, IsaVersion.v83, InstFlags.None),
                new(0x0E20E000, 0xBFE0FC00, sizeSizeConstraints2, InstName.PmullAdvsimd, IsaVersion.v80, InstFlags.RdRnRmFpSimd),
                new(0x0EE0E000, 0xBFE0FC00, sizeSizeConstraints2, InstName.PmullAdvsimd, IsaVersion.v80, InstFlags.RdRnRmFpSimd),
                new(0x2E209C00, 0xBF20FC00, usizeUsizeUsizeSizeConstraints, InstName.PmulAdvsimd, IsaVersion.v80, InstFlags.RdRnRmFpSimd),
                new(0xF9800000, 0xFFC00000, InstName.PrfmImm, IsaVersion.v80, InstFlags.RnSP, AddressForm.UnsignedScaled),
                new(0xD8000000, 0xFF000000, InstName.PrfmLit, IsaVersion.v80, InstFlags.None, AddressForm.Literal),
                new(0xF8A04800, 0xFFE04C00, rtConstraints2, InstName.PrfmReg, IsaVersion.v80, InstFlags.RnSPRm, AddressForm.OffsetReg),
                new(0xF8800000, 0xFFE00C00, InstName.Prfum, IsaVersion.v80, InstFlags.RnSP, AddressForm.BasePlusOffset),
                new(0xD503223F, 0xFFFFFFFF, InstName.Psb, IsaVersion.v82, InstFlags.None),
                new(0x2E204000, 0xBF20FC00, sizeConstraints, InstName.RaddhnAdvsimd, IsaVersion.v80, InstFlags.RdReadRdRnRmFpSimd),
                new(0xCE608C00, 0xFFE0FC00, InstName.Rax1Advsimd, IsaVersion.v82, InstFlags.RdRnRmFpSimd),
                new(0x2E605800, 0xBFFFFC00, InstName.RbitAdvsimd, IsaVersion.v80, InstFlags.RdRnFpSimd),
                new(0x5AC00000, 0x7FFFFC00, InstName.RbitInt, IsaVersion.v80, InstFlags.RdRn),
                new(0x19200800, 0xFF20FC00, InstName.Rcwcas, IsaVersion.v89, InstFlags.RtReadRtRnSPRsS),
                new(0x19200C00, 0xFF20FC00, rsRtConstraints, InstName.Rcwcasp, IsaVersion.None, InstFlags.RtReadRtRnSPRsS),
                new(0x38209000, 0xFF20FC00, InstName.Rcwclr, IsaVersion.v89, InstFlags.RtReadRtRnSPRsS),
                new(0x19209000, 0xFF20FC00, rtRt2Constraints, InstName.Rcwclrp, IsaVersion.None, InstFlags.RtReadRtRt2RnSPS),
                new(0x59200800, 0xFF20FC00, InstName.Rcwscas, IsaVersion.v89, InstFlags.RtReadRtRnSPRsS),
                new(0x59200C00, 0xFF20FC00, rsRtConstraints, InstName.Rcwscasp, IsaVersion.None, InstFlags.RtReadRtRnSPRsS),
                new(0x78209000, 0xFF20FC00, InstName.Rcwsclr, IsaVersion.v89, InstFlags.RtReadRtRnSPRsS),
                new(0x59209000, 0xFF20FC00, rtRt2Constraints, InstName.Rcwsclrp, IsaVersion.None, InstFlags.RtReadRtRt2RnSPS),
                new(0x3820B000, 0xFF20FC00, InstName.Rcwset, IsaVersion.v89, InstFlags.RtReadRtRnSPRsS),
                new(0x1920B000, 0xFF20FC00, rtRt2Constraints, InstName.Rcwsetp, IsaVersion.None, InstFlags.RtReadRtRt2RnSPS),
                new(0x7820B000, 0xFF20FC00, InstName.Rcwsset, IsaVersion.v89, InstFlags.RtReadRtRnSPRsS),
                new(0x5920B000, 0xFF20FC00, rtRt2Constraints, InstName.Rcwssetp, IsaVersion.None, InstFlags.RtReadRtRt2RnSPS),
                new(0x7820A000, 0xFF20FC00, InstName.Rcwsswp, IsaVersion.v89, InstFlags.RtReadRtRnSPRsS),
                new(0x5920A000, 0xFF20FC00, rtRt2Constraints, InstName.Rcwsswpp, IsaVersion.None, InstFlags.RtReadRtRt2RnSPS),
                new(0x3820A000, 0xFF20FC00, InstName.Rcwswp, IsaVersion.v89, InstFlags.RtReadRtRnSPRsS),
                new(0x1920A000, 0xFF20FC00, rtRt2Constraints, InstName.Rcwswpp, IsaVersion.None, InstFlags.RtReadRtRt2RnSPS),
                new(0xD65F0000, 0xFFFFFC1F, InstName.Ret, IsaVersion.v80, InstFlags.Rn),
                new(0xD65F0BFF, 0xFFFFFBFF, InstName.Reta, IsaVersion.v83, InstFlags.None),
                new(0x5AC00800, 0x7FFFF800, sfopcConstraints, InstName.Rev, IsaVersion.v80, InstFlags.RdRn),
                new(0x0E201800, 0xBF3FFC00, sizeSizeSizeConstraints, InstName.Rev16Advsimd, IsaVersion.v80, InstFlags.RdRnFpSimd),
                new(0x5AC00400, 0x7FFFFC00, InstName.Rev16Int, IsaVersion.v80, InstFlags.RdRn),
                new(0x2E200800, 0xBF3FFC00, sizeSizeConstraints3, InstName.Rev32Advsimd, IsaVersion.v80, InstFlags.RdRnFpSimd),
                new(0xDAC00800, 0xFFFFFC00, sfConstraints, InstName.Rev32Int, IsaVersion.v80, InstFlags.RdRn),
                new(0x0E200800, 0xBF3FFC00, sizeConstraints, InstName.Rev64Advsimd, IsaVersion.v80, InstFlags.RdRnFpSimd),
                new(0xBA000400, 0xFFE07C10, InstName.Rmif, IsaVersion.v84, InstFlags.RnC),
                new(0x1AC02C00, 0x7FE0FC00, InstName.Rorv, IsaVersion.v80, InstFlags.RdRnRm),
                new(0xF8A04818, 0xFFE04C18, InstName.RprfmReg, IsaVersion.v80, InstFlags.RnSPRm, AddressForm.OffsetReg),
                new(0x0F008C00, 0xBF80FC00, immhImmhConstraints, InstName.RshrnAdvsimd, IsaVersion.v80, InstFlags.RdReadRdRnFpSimd),
                new(0x2E206000, 0xBF20FC00, sizeConstraints, InstName.RsubhnAdvsimd, IsaVersion.v80, InstFlags.RdReadRdRnRmFpSimd),
                new(0x0E205000, 0xBF20FC00, sizeConstraints, InstName.SabalAdvsimd, IsaVersion.v80, InstFlags.RdRnRmFpSimd),
                new(0x0E207C00, 0xBF20FC00, sizeConstraints, InstName.SabaAdvsimd, IsaVersion.v80, InstFlags.RdRnRmFpSimd),
                new(0x0E207000, 0xBF20FC00, sizeConstraints, InstName.SabdlAdvsimd, IsaVersion.v80, InstFlags.RdRnRmFpSimd),
                new(0x0E207400, 0xBF20FC00, sizeConstraints, InstName.SabdAdvsimd, IsaVersion.v80, InstFlags.RdRnRmFpSimd),
                new(0x0E206800, 0xBF3FFC00, sizeConstraints, InstName.SadalpAdvsimd, IsaVersion.v80, InstFlags.RdReadRdRnFpSimd),
                new(0x0E202800, 0xBF3FFC00, sizeConstraints, InstName.SaddlpAdvsimd, IsaVersion.v80, InstFlags.RdReadRdRnFpSimd),
                new(0x0E303800, 0xBF3FFC00, qsizeSizeConstraints, InstName.SaddlvAdvsimd, IsaVersion.v80, InstFlags.RdRnFpSimd),
                new(0x0E200000, 0xBF20FC00, sizeConstraints, InstName.SaddlAdvsimd, IsaVersion.v80, InstFlags.RdRnRmFpSimd),
                new(0x0E201000, 0xBF20FC00, sizeConstraints, InstName.SaddwAdvsimd, IsaVersion.v80, InstFlags.RdRnRmFpSimd),
                new(0xD50330FF, 0xFFFFFFFF, InstName.Sb, IsaVersion.v85, InstFlags.None),
                new(0x5A000000, 0x7FE0FC00, InstName.Sbc, IsaVersion.v80, InstFlags.RdRnRmC),
                new(0x7A000000, 0x7FE0FC00, InstName.Sbcs, IsaVersion.v80, InstFlags.RdRnRmCS),
                new(0x13000000, 0x7F800000, sfnSfnSfimmr5Sfimms5Constraints, InstName.Sbfm, IsaVersion.v80, InstFlags.RdRn),
                new(0x5F40E400, 0xFFC0FC00, immhConstraints, InstName.ScvtfAdvsimdFixS, IsaVersion.v80, InstFlags.RdRnFpSimd),
                new(0x5F10E400, 0xFFF0FC00, immhConstraints, InstName.ScvtfAdvsimdFixS, IsaVersion.v80, InstFlags.RdRnFpSimd),
                new(0x5F20E400, 0xFFE0FC00, immhConstraints, InstName.ScvtfAdvsimdFixS, IsaVersion.v80, InstFlags.RdRnFpSimd),
                new(0x0F40E400, 0xBFC0FC00, immhQimmhConstraints, InstName.ScvtfAdvsimdFixV, IsaVersion.v80, InstFlags.RdRnFpSimd),
                new(0x0F10E400, 0xBFF0FC00, immhQimmhConstraints, InstName.ScvtfAdvsimdFixV, IsaVersion.v80, InstFlags.RdRnFpSimd),
                new(0x0F20E400, 0xBFE0FC00, immhQimmhConstraints, InstName.ScvtfAdvsimdFixV, IsaVersion.v80, InstFlags.RdRnFpSimd),
                new(0x5E79D800, 0xFFFFFC00, InstName.ScvtfAdvsimdIntSH, IsaVersion.v82, InstFlags.RdRnFpSimd),
                new(0x5E21D800, 0xFFBFFC00, InstName.ScvtfAdvsimdIntS, IsaVersion.v80, InstFlags.RdRnFpSimd),
                new(0x0E79D800, 0xBFFFFC00, InstName.ScvtfAdvsimdIntVH, IsaVersion.v82, InstFlags.RdRnFpSimd),
                new(0x0E21D800, 0xBFBFFC00, qszConstraints, InstName.ScvtfAdvsimdIntV, IsaVersion.v80, InstFlags.RdRnFpSimd),
                new(0x1E020000, 0x7FBF0000, sfscaleConstraints, InstName.ScvtfFloatFix, IsaVersion.v80, InstFlags.RdRnFpSimdFromGpr),
                new(0x1EC20000, 0x7FFF0000, sfscaleConstraints, InstName.ScvtfFloatFix, IsaVersion.v80, InstFlags.RdRnFpSimdFromGpr),
                new(0x1E220000, 0x7FBFFC00, InstName.ScvtfFloatInt, IsaVersion.v80, InstFlags.RdRnFpSimdFromGpr),
                new(0x1EE20000, 0x7FFFFC00, InstName.ScvtfFloatInt, IsaVersion.v80, InstFlags.RdRnFpSimdFromGpr),
                new(0x1AC00C00, 0x7FE0FC00, InstName.Sdiv, IsaVersion.v80, InstFlags.RdRnRm),
                new(0x0F80E000, 0xBFC0F400, InstName.SdotAdvsimdElt, IsaVersion.v82, InstFlags.RdReadRdRnRmFpSimd),
                new(0x0E809400, 0xBFE0FC00, InstName.SdotAdvsimdVec, IsaVersion.v82, InstFlags.RdReadRdRnRmFpSimd),
                new(0x3A00080D, 0xFFFFBC1F, InstName.Setf, IsaVersion.v84, InstFlags.RnC),
                new(0x1DC00400, 0x3FE03C00, InstName.Setgp, IsaVersion.v88, InstFlags.RdRnRsS),
                new(0x1DC02400, 0x3FE03C00, InstName.Setgpn, IsaVersion.v88, InstFlags.RdRnRsS),
                new(0x1DC01400, 0x3FE03C00, InstName.Setgpt, IsaVersion.v88, InstFlags.RdRnRsS),
                new(0x1DC03400, 0x3FE03C00, InstName.Setgptn, IsaVersion.v88, InstFlags.RdRnRsS),
                new(0x19C00400, 0x3FE03C00, InstName.Setp, IsaVersion.v88, InstFlags.RdRnRsS),
                new(0x19C02400, 0x3FE03C00, InstName.Setpn, IsaVersion.v88, InstFlags.RdRnRsS),
                new(0x19C01400, 0x3FE03C00, InstName.Setpt, IsaVersion.v88, InstFlags.RdRnRsS),
                new(0x19C03400, 0x3FE03C00, InstName.Setptn, IsaVersion.v88, InstFlags.RdRnRsS),
                new(0xD503209F, 0xFFFFFFFF, InstName.Sev, IsaVersion.v80, InstFlags.None),
                new(0xD50320BF, 0xFFFFFFFF, InstName.Sevl, IsaVersion.v80, InstFlags.None),
                new(0x5E000000, 0xFFE0FC00, InstName.Sha1cAdvsimd, IsaVersion.v80, InstFlags.RdRnRmFpSimd),
                new(0x5E280800, 0xFFFFFC00, InstName.Sha1hAdvsimd, IsaVersion.v80, InstFlags.RdRnFpSimd),
                new(0x5E002000, 0xFFE0FC00, InstName.Sha1mAdvsimd, IsaVersion.v80, InstFlags.RdRnRmFpSimd),
                new(0x5E001000, 0xFFE0FC00, InstName.Sha1pAdvsimd, IsaVersion.v80, InstFlags.RdRnRmFpSimd),
                new(0x5E003000, 0xFFE0FC00, InstName.Sha1su0Advsimd, IsaVersion.v80, InstFlags.RdRnRmFpSimd),
                new(0x5E281800, 0xFFFFFC00, InstName.Sha1su1Advsimd, IsaVersion.v80, InstFlags.RdRnFpSimd),
                new(0x5E005000, 0xFFE0FC00, InstName.Sha256h2Advsimd, IsaVersion.v80, InstFlags.RdRnRmFpSimd),
                new(0x5E004000, 0xFFE0FC00, InstName.Sha256hAdvsimd, IsaVersion.v80, InstFlags.RdRnRmFpSimd),
                new(0x5E282800, 0xFFFFFC00, InstName.Sha256su0Advsimd, IsaVersion.v80, InstFlags.RdRnFpSimd),
                new(0x5E006000, 0xFFE0FC00, InstName.Sha256su1Advsimd, IsaVersion.v80, InstFlags.RdRnRmFpSimd),
                new(0xCE608400, 0xFFE0FC00, InstName.Sha512h2Advsimd, IsaVersion.v82, InstFlags.RdRnRmFpSimd),
                new(0xCE608000, 0xFFE0FC00, InstName.Sha512hAdvsimd, IsaVersion.v82, InstFlags.RdRnRmFpSimd),
                new(0xCEC08000, 0xFFFFFC00, InstName.Sha512su0Advsimd, IsaVersion.v82, InstFlags.RdRnFpSimd),
                new(0xCE608800, 0xFFE0FC00, InstName.Sha512su1Advsimd, IsaVersion.v82, InstFlags.RdRnRmFpSimd),
                new(0x0E200400, 0xBF20FC00, sizeConstraints, InstName.ShaddAdvsimd, IsaVersion.v80, InstFlags.RdRnRmFpSimd),
                new(0x2E213800, 0xBF3FFC00, sizeConstraints, InstName.ShllAdvsimd, IsaVersion.v80, InstFlags.RdRnFpSimd),
                new(0x5F405400, 0xFFC0FC00, immhConstraints, InstName.ShlAdvsimdS, IsaVersion.v80, InstFlags.RdRnFpSimd),
                new(0x0F005400, 0xBF80FC00, immhQimmhConstraints, InstName.ShlAdvsimdV, IsaVersion.v80, InstFlags.RdRnFpSimd),
                new(0x0F008400, 0xBF80FC00, immhImmhConstraints, InstName.ShrnAdvsimd, IsaVersion.v80, InstFlags.RdReadRdRnFpSimd),
                new(0x0E202400, 0xBF20FC00, sizeConstraints, InstName.ShsubAdvsimd, IsaVersion.v80, InstFlags.RdRnRmFpSimd),
                new(0x7F405400, 0xFFC0FC00, immhConstraints, InstName.SliAdvsimdS, IsaVersion.v80, InstFlags.RdReadRdRnFpSimd),
                new(0x2F005400, 0xBF80FC00, immhQimmhConstraints, InstName.SliAdvsimdV, IsaVersion.v80, InstFlags.RdReadRdRnFpSimd),
                new(0xCE60C000, 0xFFE0FC00, InstName.Sm3partw1Advsimd, IsaVersion.v82, InstFlags.RdRnRmFpSimd),
                new(0xCE60C400, 0xFFE0FC00, InstName.Sm3partw2Advsimd, IsaVersion.v82, InstFlags.RdRnRmFpSimd),
                new(0xCE400000, 0xFFE08000, InstName.Sm3ss1Advsimd, IsaVersion.v82, InstFlags.RdRnRmRaFpSimd),
                new(0xCE408000, 0xFFE0CC00, InstName.Sm3tt1aAdvsimd, IsaVersion.v82, InstFlags.RdRnRmFpSimd),
                new(0xCE408400, 0xFFE0CC00, InstName.Sm3tt1bAdvsimd, IsaVersion.v82, InstFlags.RdRnRmFpSimd),
                new(0xCE408800, 0xFFE0CC00, InstName.Sm3tt2aAdvsimd, IsaVersion.v82, InstFlags.RdRnRmFpSimd),
                new(0xCE408C00, 0xFFE0CC00, InstName.Sm3tt2bAdvsimd, IsaVersion.v82, InstFlags.RdRnRmFpSimd),
                new(0xCE60C800, 0xFFE0FC00, InstName.Sm4ekeyAdvsimd, IsaVersion.v82, InstFlags.RdRnRmFpSimd),
                new(0xCEC08400, 0xFFFFFC00, InstName.Sm4eAdvsimd, IsaVersion.v82, InstFlags.RdReadRdRnFpSimd),
                new(0x9B200000, 0xFFE08000, InstName.Smaddl, IsaVersion.v80, InstFlags.RdRnRmRa),
                new(0x0E20A400, 0xBF20FC00, sizeConstraints, InstName.SmaxpAdvsimd, IsaVersion.v80, InstFlags.RdRnRmFpSimd),
                new(0x0E30A800, 0xBF3FFC00, qsizeSizeConstraints, InstName.SmaxvAdvsimd, IsaVersion.v80, InstFlags.RdRnFpSimd),
                new(0x0E206400, 0xBF20FC00, sizeConstraints, InstName.SmaxAdvsimd, IsaVersion.v80, InstFlags.RdRnRmFpSimd),
                new(0x11C00000, 0x7FFC0000, InstName.SmaxImm, IsaVersion.v89, InstFlags.RdRn),
                new(0x1AC06000, 0x7FE0FC00, InstName.SmaxReg, IsaVersion.v89, InstFlags.RdRnRm),
                new(0xD4000003, 0xFFE0001F, InstName.Smc, IsaVersion.v80, InstFlags.None),
                new(0x0E20AC00, 0xBF20FC00, sizeConstraints, InstName.SminpAdvsimd, IsaVersion.v80, InstFlags.RdRnRmFpSimd),
                new(0x0E31A800, 0xBF3FFC00, qsizeSizeConstraints, InstName.SminvAdvsimd, IsaVersion.v80, InstFlags.RdRnFpSimd),
                new(0x0E206C00, 0xBF20FC00, sizeConstraints, InstName.SminAdvsimd, IsaVersion.v80, InstFlags.RdRnRmFpSimd),
                new(0x11C80000, 0x7FFC0000, InstName.SminImm, IsaVersion.v89, InstFlags.RdRn),
                new(0x1AC06800, 0x7FE0FC00, InstName.SminReg, IsaVersion.v89, InstFlags.RdRnRm),
                new(0x0F002000, 0xBF00F400, sizeSizeConstraints, InstName.SmlalAdvsimdElt, IsaVersion.v80, InstFlags.RdRnRmFpSimd),
                new(0x0E208000, 0xBF20FC00, sizeConstraints, InstName.SmlalAdvsimdVec, IsaVersion.v80, InstFlags.RdRnRmFpSimd),
                new(0x0F006000, 0xBF00F400, sizeSizeConstraints, InstName.SmlslAdvsimdElt, IsaVersion.v80, InstFlags.RdRnRmFpSimd),
                new(0x0E20A000, 0xBF20FC00, sizeConstraints, InstName.SmlslAdvsimdVec, IsaVersion.v80, InstFlags.RdRnRmFpSimd),
                new(0x4E80A400, 0xFFE0FC00, InstName.SmmlaAdvsimdVec, IsaVersion.v86, InstFlags.RdRnRmFpSimd),
                new(0x0E012C00, 0xBFE1FC00, InstName.SmovAdvsimd, IsaVersion.v80, InstFlags.RdRnFpSimdToGpr),
                new(0x0E022C00, 0xBFE3FC00, InstName.SmovAdvsimd, IsaVersion.v80, InstFlags.RdRnFpSimdToGpr),
                new(0x4E042C00, 0xFFE7FC00, InstName.SmovAdvsimd, IsaVersion.v80, InstFlags.RdRnFpSimdToGpr),
                new(0x9B208000, 0xFFE08000, InstName.Smsubl, IsaVersion.v80, InstFlags.RdRnRmRa),
                new(0x9B407C00, 0xFFE0FC00, InstName.Smulh, IsaVersion.v80, InstFlags.RdRnRm),
                new(0x0F00A000, 0xBF00F400, sizeSizeConstraints, InstName.SmullAdvsimdElt, IsaVersion.v80, InstFlags.RdRnRmFpSimd),
                new(0x0E20C000, 0xBF20FC00, sizeConstraints, InstName.SmullAdvsimdVec, IsaVersion.v80, InstFlags.RdRnRmFpSimd),
                new(0x5E207800, 0xFF3FFC00, InstName.SqabsAdvsimdS, IsaVersion.v80, InstFlags.RdRnQcFpSimd),
                new(0x0E207800, 0xBF3FFC00, qsizeConstraints, InstName.SqabsAdvsimdV, IsaVersion.v80, InstFlags.RdRnQcFpSimd),
                new(0x5E200C00, 0xFF20FC00, InstName.SqaddAdvsimdS, IsaVersion.v80, InstFlags.RdRnRmQcFpSimd),
                new(0x0E200C00, 0xBF20FC00, qsizeConstraints, InstName.SqaddAdvsimdV, IsaVersion.v80, InstFlags.RdRnRmQcFpSimd),
                new(0x5F003000, 0xFF00F400, sizeSizeConstraints, InstName.SqdmlalAdvsimdElt2regScalar, IsaVersion.v80, InstFlags.RdRnRmQcFpSimd),
                new(0x0F003000, 0xBF00F400, sizeSizeConstraints, InstName.SqdmlalAdvsimdElt2regElement, IsaVersion.v80, InstFlags.RdRnRmQcFpSimd),
                new(0x5E209000, 0xFF20FC00, sizeSizeConstraints, InstName.SqdmlalAdvsimdVecS, IsaVersion.v80, InstFlags.RdRnRmQcFpSimd),
                new(0x0E209000, 0xBF20FC00, sizeSizeConstraints, InstName.SqdmlalAdvsimdVecV, IsaVersion.v80, InstFlags.RdRnRmQcFpSimd),
                new(0x5F007000, 0xFF00F400, sizeSizeConstraints, InstName.SqdmlslAdvsimdElt2regScalar, IsaVersion.v80, InstFlags.RdRnRmQcFpSimd),
                new(0x0F007000, 0xBF00F400, sizeSizeConstraints, InstName.SqdmlslAdvsimdElt2regElement, IsaVersion.v80, InstFlags.RdRnRmQcFpSimd),
                new(0x5E20B000, 0xFF20FC00, sizeSizeConstraints, InstName.SqdmlslAdvsimdVecS, IsaVersion.v80, InstFlags.RdRnRmQcFpSimd),
                new(0x0E20B000, 0xBF20FC00, sizeSizeConstraints, InstName.SqdmlslAdvsimdVecV, IsaVersion.v80, InstFlags.RdRnRmQcFpSimd),
                new(0x5F00C000, 0xFF00F400, sizeSizeConstraints, InstName.SqdmulhAdvsimdElt2regScalar, IsaVersion.v80, InstFlags.RdRnRmFpSimd),
                new(0x0F00C000, 0xBF00F400, sizeSizeConstraints, InstName.SqdmulhAdvsimdElt2regElement, IsaVersion.v80, InstFlags.RdRnRmFpSimd),
                new(0x5E20B400, 0xFF20FC00, sizeSizeConstraints4, InstName.SqdmulhAdvsimdVecS, IsaVersion.v80, InstFlags.RdRnRmQcFpSimd),
                new(0x0E20B400, 0xBF20FC00, sizeSizeConstraints4, InstName.SqdmulhAdvsimdVecV, IsaVersion.v80, InstFlags.RdRnRmQcFpSimd),
                new(0x5F00B000, 0xFF00F400, sizeSizeConstraints, InstName.SqdmullAdvsimdElt2regScalar, IsaVersion.v80, InstFlags.RdRnRmQcFpSimd),
                new(0x0F00B000, 0xBF00F400, sizeSizeConstraints, InstName.SqdmullAdvsimdElt2regElement, IsaVersion.v80, InstFlags.RdRnRmQcFpSimd),
                new(0x5E20D000, 0xFF20FC00, sizeSizeConstraints, InstName.SqdmullAdvsimdVecS, IsaVersion.v80, InstFlags.RdRnRmQcFpSimd),
                new(0x0E20D000, 0xBF20FC00, sizeSizeConstraints, InstName.SqdmullAdvsimdVecV, IsaVersion.v80, InstFlags.RdRnRmQcFpSimd),
                new(0x7E207800, 0xFF3FFC00, InstName.SqnegAdvsimdS, IsaVersion.v80, InstFlags.RdRnQcFpSimd),
                new(0x2E207800, 0xBF3FFC00, qsizeConstraints, InstName.SqnegAdvsimdV, IsaVersion.v80, InstFlags.RdRnQcFpSimd),
                new(0x7F00D000, 0xFF00F400, sizeSizeConstraints, InstName.SqrdmlahAdvsimdElt2regScalar, IsaVersion.v81, InstFlags.RdReadRdRnRmQcFpSimd),
                new(0x2F00D000, 0xBF00F400, sizeSizeConstraints, InstName.SqrdmlahAdvsimdElt2regElement, IsaVersion.v81, InstFlags.RdReadRdRnRmQcFpSimd),
                new(0x7E008400, 0xFF20FC00, sizeSizeConstraints4, InstName.SqrdmlahAdvsimdVecS, IsaVersion.v81, InstFlags.RdReadRdRnRmQcFpSimd),
                new(0x2E008400, 0xBF20FC00, sizeSizeConstraints4, InstName.SqrdmlahAdvsimdVecV, IsaVersion.v81, InstFlags.RdReadRdRnRmQcFpSimd),
                new(0x7F00F000, 0xFF00F400, sizeSizeConstraints, InstName.SqrdmlshAdvsimdElt2regScalar, IsaVersion.v81, InstFlags.RdReadRdRnRmQcFpSimd),
                new(0x2F00F000, 0xBF00F400, sizeSizeConstraints, InstName.SqrdmlshAdvsimdElt2regElement, IsaVersion.v81, InstFlags.RdReadRdRnRmQcFpSimd),
                new(0x7E008C00, 0xFF20FC00, sizeSizeConstraints4, InstName.SqrdmlshAdvsimdVecS, IsaVersion.v81, InstFlags.RdReadRdRnRmQcFpSimd),
                new(0x2E008C00, 0xBF20FC00, sizeSizeConstraints4, InstName.SqrdmlshAdvsimdVecV, IsaVersion.v81, InstFlags.RdReadRdRnRmQcFpSimd),
                new(0x5F00D000, 0xFF00F400, sizeSizeConstraints, InstName.SqrdmulhAdvsimdElt2regScalar, IsaVersion.v80, InstFlags.RdRnRmQcFpSimd),
                new(0x0F00D000, 0xBF00F400, sizeSizeConstraints, InstName.SqrdmulhAdvsimdElt2regElement, IsaVersion.v80, InstFlags.RdRnRmQcFpSimd),
                new(0x7E20B400, 0xFF20FC00, sizeSizeConstraints4, InstName.SqrdmulhAdvsimdVecS, IsaVersion.v80, InstFlags.RdRnRmQcFpSimd),
                new(0x2E20B400, 0xBF20FC00, sizeSizeConstraints4, InstName.SqrdmulhAdvsimdVecV, IsaVersion.v80, InstFlags.RdRnRmQcFpSimd),
                new(0x5E205C00, 0xFF20FC00, ssizeSsizeSsizeConstraints, InstName.SqrshlAdvsimdS, IsaVersion.v80, InstFlags.RdRnRmQcFpSimd),
                new(0x0E205C00, 0xBF20FC00, qsizeConstraints, InstName.SqrshlAdvsimdV, IsaVersion.v80, InstFlags.RdRnRmQcFpSimd),
                new(0x5F009C00, 0xFF80FC00, immhImmhConstraints, InstName.SqrshrnAdvsimdS, IsaVersion.v80, InstFlags.RdReadRdRnQcFpSimd),
                new(0x0F009C00, 0xBF80FC00, immhImmhConstraints, InstName.SqrshrnAdvsimdV, IsaVersion.v80, InstFlags.RdReadRdRnQcFpSimd),
                new(0x7F008C00, 0xFF80FC00, immhImmhConstraints, InstName.SqrshrunAdvsimdS, IsaVersion.v80, InstFlags.RdReadRdRnQcFpSimd),
                new(0x2F008C00, 0xBF80FC00, immhImmhConstraints, InstName.SqrshrunAdvsimdV, IsaVersion.v80, InstFlags.RdReadRdRnQcFpSimd),
                new(0x7F006400, 0xFF80FC00, immhOpuConstraints, InstName.SqshluAdvsimdS, IsaVersion.v80, InstFlags.RdRnQcFpSimd),
                new(0x2F006400, 0xBF80FC00, immhQimmhOpuConstraints, InstName.SqshluAdvsimdV, IsaVersion.v80, InstFlags.RdRnQcFpSimd),
                new(0x5F007400, 0xFF80FC00, immhOpuConstraints, InstName.SqshlAdvsimdImmS, IsaVersion.v80, InstFlags.RdRnQcFpSimd),
                new(0x0F007400, 0xBF80FC00, immhQimmhOpuConstraints, InstName.SqshlAdvsimdImmV, IsaVersion.v80, InstFlags.RdRnQcFpSimd),
                new(0x5E204C00, 0xFF20FC00, ssizeSsizeSsizeConstraints, InstName.SqshlAdvsimdRegS, IsaVersion.v80, InstFlags.RdRnRmQcFpSimd),
                new(0x0E204C00, 0xBF20FC00, qsizeConstraints, InstName.SqshlAdvsimdRegV, IsaVersion.v80, InstFlags.RdRnRmQcFpSimd),
                new(0x5F009400, 0xFF80FC00, immhImmhConstraints, InstName.SqshrnAdvsimdS, IsaVersion.v80, InstFlags.RdReadRdRnQcFpSimd),
                new(0x0F009400, 0xBF80FC00, immhImmhConstraints, InstName.SqshrnAdvsimdV, IsaVersion.v80, InstFlags.RdReadRdRnQcFpSimd),
                new(0x7F008400, 0xFF80FC00, immhImmhConstraints, InstName.SqshrunAdvsimdS, IsaVersion.v80, InstFlags.RdReadRdRnQcFpSimd),
                new(0x2F008400, 0xBF80FC00, immhImmhConstraints, InstName.SqshrunAdvsimdV, IsaVersion.v80, InstFlags.RdReadRdRnQcFpSimd),
                new(0x5E202C00, 0xFF20FC00, InstName.SqsubAdvsimdS, IsaVersion.v80, InstFlags.RdRnRmQcFpSimd),
                new(0x0E202C00, 0xBF20FC00, qsizeConstraints, InstName.SqsubAdvsimdV, IsaVersion.v80, InstFlags.RdRnRmQcFpSimd),
                new(0x5E214800, 0xFF3FFC00, sizeConstraints, InstName.SqxtnAdvsimdS, IsaVersion.v80, InstFlags.RdReadRdRnQcFpSimd),
                new(0x0E214800, 0xBF3FFC00, sizeConstraints, InstName.SqxtnAdvsimdV, IsaVersion.v80, InstFlags.RdReadRdRnQcFpSimd),
                new(0x7E212800, 0xFF3FFC00, sizeConstraints, InstName.SqxtunAdvsimdS, IsaVersion.v80, InstFlags.RdReadRdRnQcFpSimd),
                new(0x2E212800, 0xBF3FFC00, sizeConstraints, InstName.SqxtunAdvsimdV, IsaVersion.v80, InstFlags.RdReadRdRnQcFpSimd),
                new(0x0E201400, 0xBF20FC00, sizeConstraints, InstName.SrhaddAdvsimd, IsaVersion.v80, InstFlags.RdRnRmFpSimd),
                new(0x7F404400, 0xFFC0FC00, immhConstraints, InstName.SriAdvsimdS, IsaVersion.v80, InstFlags.RdReadRdRnFpSimd),
                new(0x2F004400, 0xBF80FC00, immhQimmhConstraints, InstName.SriAdvsimdV, IsaVersion.v80, InstFlags.RdReadRdRnFpSimd),
                new(0x5E205400, 0xFF20FC00, ssizeSsizeSsizeConstraints, InstName.SrshlAdvsimdS, IsaVersion.v80, InstFlags.RdRnRmFpSimd),
                new(0x0E205400, 0xBF20FC00, qsizeConstraints, InstName.SrshlAdvsimdV, IsaVersion.v80, InstFlags.RdRnRmFpSimd),
                new(0x5F402400, 0xFFC0FC00, immhConstraints, InstName.SrshrAdvsimdS, IsaVersion.v80, InstFlags.RdRnFpSimd),
                new(0x0F002400, 0xBF80FC00, immhQimmhConstraints, InstName.SrshrAdvsimdV, IsaVersion.v80, InstFlags.RdRnFpSimd),
                new(0x5F403400, 0xFFC0FC00, immhConstraints, InstName.SrsraAdvsimdS, IsaVersion.v80, InstFlags.RdRnFpSimd),
                new(0x0F003400, 0xBF80FC00, immhQimmhConstraints, InstName.SrsraAdvsimdV, IsaVersion.v80, InstFlags.RdRnFpSimd),
                new(0x0F00A400, 0xBF80FC00, immhImmhConstraints, InstName.SshllAdvsimd, IsaVersion.v80, InstFlags.RdRnFpSimd),
                new(0x5E204400, 0xFF20FC00, ssizeSsizeSsizeConstraints, InstName.SshlAdvsimdS, IsaVersion.v80, InstFlags.RdRnRmFpSimd),
                new(0x0E204400, 0xBF20FC00, qsizeConstraints, InstName.SshlAdvsimdV, IsaVersion.v80, InstFlags.RdRnRmFpSimd),
                new(0x5F400400, 0xFFC0FC00, immhConstraints, InstName.SshrAdvsimdS, IsaVersion.v80, InstFlags.RdRnFpSimd),
                new(0x0F000400, 0xBF80FC00, immhQimmhConstraints, InstName.SshrAdvsimdV, IsaVersion.v80, InstFlags.RdRnFpSimd),
                new(0x5F401400, 0xFFC0FC00, immhConstraints, InstName.SsraAdvsimdS, IsaVersion.v80, InstFlags.RdRnFpSimd),
                new(0x0F001400, 0xBF80FC00, immhQimmhConstraints, InstName.SsraAdvsimdV, IsaVersion.v80, InstFlags.RdRnFpSimd),
                new(0x0E202000, 0xBF20FC00, sizeConstraints, InstName.SsublAdvsimd, IsaVersion.v80, InstFlags.RdRnRmFpSimd),
                new(0x0E203000, 0xBF20FC00, sizeConstraints, InstName.SsubwAdvsimd, IsaVersion.v80, InstFlags.RdRnRmFpSimd),
                new(0x0C002000, 0xBFFFF000, InstName.St1AdvsimdMultAsNoPostIndex, IsaVersion.v80, InstFlags.RtSeqReadRtRnSPFpSimd, AddressForm.StructNoOffset),
                new(0x0C006000, 0xBFFFF000, InstName.St1AdvsimdMultAsNoPostIndex, IsaVersion.v80, InstFlags.RtSeqReadRtRnSPFpSimd, AddressForm.StructNoOffset),
                new(0x0C007000, 0xBFFFF000, InstName.St1AdvsimdMultAsNoPostIndex, IsaVersion.v80, InstFlags.RtSeqReadRtRnSPFpSimd, AddressForm.StructNoOffset),
                new(0x0C00A000, 0xBFFFF000, InstName.St1AdvsimdMultAsNoPostIndex, IsaVersion.v80, InstFlags.RtSeqReadRtRnSPFpSimd, AddressForm.StructNoOffset),
                new(0x0C006000, 0xBFFFF000, InstName.St1AdvsimdMultAsNoPostIndex, IsaVersion.v80, InstFlags.RtSeqReadRtRnSPFpSimd, AddressForm.StructNoOffset),
                new(0x0C007000, 0xBFFFF000, InstName.St1AdvsimdMultAsNoPostIndex, IsaVersion.v80, InstFlags.RtSeqReadRtRnSPFpSimd, AddressForm.StructNoOffset),
                new(0x0C00A000, 0xBFFFF000, InstName.St1AdvsimdMultAsNoPostIndex, IsaVersion.v80, InstFlags.RtSeqReadRtRnSPFpSimd, AddressForm.StructNoOffset),
                new(0x0C802000, 0xBFE0F000, InstName.St1AdvsimdMultAsPostIndex, IsaVersion.v80, InstFlags.RtSeqReadRtRnSPRmFpSimdMemWBack, AddressForm.StructPostIndexedReg),
                new(0x0C806000, 0xBFE0F000, InstName.St1AdvsimdMultAsPostIndex, IsaVersion.v80, InstFlags.RtSeqReadRtRnSPRmFpSimdMemWBack, AddressForm.StructPostIndexedReg),
                new(0x0C807000, 0xBFE0F000, InstName.St1AdvsimdMultAsPostIndex, IsaVersion.v80, InstFlags.RtSeqReadRtRnSPRmFpSimdMemWBack, AddressForm.StructPostIndexedReg),
                new(0x0C80A000, 0xBFE0F000, InstName.St1AdvsimdMultAsPostIndex, IsaVersion.v80, InstFlags.RtSeqReadRtRnSPRmFpSimdMemWBack, AddressForm.StructPostIndexedReg),
                new(0x0C806000, 0xBFE0F000, InstName.St1AdvsimdMultAsPostIndex, IsaVersion.v80, InstFlags.RtSeqReadRtRnSPRmFpSimdMemWBack, AddressForm.StructPostIndexedReg),
                new(0x0C807000, 0xBFE0F000, InstName.St1AdvsimdMultAsPostIndex, IsaVersion.v80, InstFlags.RtSeqReadRtRnSPRmFpSimdMemWBack, AddressForm.StructPostIndexedReg),
                new(0x0C80A000, 0xBFE0F000, InstName.St1AdvsimdMultAsPostIndex, IsaVersion.v80, InstFlags.RtSeqReadRtRnSPRmFpSimdMemWBack, AddressForm.StructPostIndexedReg),
                new(0x0D000000, 0xBFFF2000, opcodesizeOpcodesizeOpcodesizesOpcodesizeConstraints, InstName.St1AdvsimdSnglAsNoPostIndex, IsaVersion.v80, InstFlags.RtReadRtRnSPFpSimd, AddressForm.StructNoOffset),
                new(0x0D800000, 0xBFE02000, opcodesizeOpcodesizeOpcodesizesOpcodesizeConstraints, InstName.St1AdvsimdSnglAsPostIndex, IsaVersion.v80, InstFlags.RtReadRtRnSPRmFpSimdMemWBack, AddressForm.StructPostIndexedReg),
                new(0xD9A00400, 0xFFE00C00, InstName.St2gPostIndexed, IsaVersion.v85, InstFlags.MemWBack, AddressForm.PostIndexed),
                new(0xD9A00C00, 0xFFE00C00, InstName.St2gPreIndexed, IsaVersion.v85, InstFlags.MemWBack, AddressForm.PreIndexed),
                new(0xD9A00800, 0xFFE00C00, InstName.St2gSignedScaledOffset, IsaVersion.v85, InstFlags.None, AddressForm.SignedScaled),
                new(0x0C008000, 0xBFFFF000, qsizeConstraints2, InstName.St2AdvsimdMultAsNoPostIndex, IsaVersion.v80, InstFlags.RtSeqReadRtRnSPFpSimd, AddressForm.StructNoOffset),
                new(0x0C808000, 0xBFE0F000, qsizeConstraints2, InstName.St2AdvsimdMultAsPostIndex, IsaVersion.v80, InstFlags.RtSeqReadRtRnSPRmFpSimdMemWBack, AddressForm.StructPostIndexedReg),
                new(0x0D200000, 0xBFFF2000, opcodesizeOpcodesizeOpcodesizesOpcodesizeConstraints, InstName.St2AdvsimdSnglAsNoPostIndex, IsaVersion.v80, InstFlags.RtSeqReadRtRnSPFpSimd, AddressForm.StructNoOffset),
                new(0x0DA00000, 0xBFE02000, opcodesizeOpcodesizeOpcodesizesOpcodesizeConstraints, InstName.St2AdvsimdSnglAsPostIndex, IsaVersion.v80, InstFlags.RtSeqReadRtRnSPRmFpSimdMemWBack, AddressForm.StructPostIndexedReg),
                new(0x0C004000, 0xBFFFF000, qsizeConstraints2, InstName.St3AdvsimdMultAsNoPostIndex, IsaVersion.v80, InstFlags.RtSeqReadRtRnSPFpSimd, AddressForm.StructNoOffset),
                new(0x0C804000, 0xBFE0F000, qsizeConstraints2, InstName.St3AdvsimdMultAsPostIndex, IsaVersion.v80, InstFlags.RtSeqReadRtRnSPRmFpSimdMemWBack, AddressForm.StructPostIndexedReg),
                new(0x0D002000, 0xBFFF2000, opcodesizeOpcodesizeOpcodesizesOpcodesizeConstraints, InstName.St3AdvsimdSnglAsNoPostIndex, IsaVersion.v80, InstFlags.RtSeqReadRtRnSPFpSimd, AddressForm.StructNoOffset),
                new(0x0D802000, 0xBFE02000, opcodesizeOpcodesizeOpcodesizesOpcodesizeConstraints, InstName.St3AdvsimdSnglAsPostIndex, IsaVersion.v80, InstFlags.RtSeqReadRtRnSPRmFpSimdMemWBack, AddressForm.StructPostIndexedReg),
                new(0x0C000000, 0xBFFFF000, qsizeConstraints2, InstName.St4AdvsimdMultAsNoPostIndex, IsaVersion.v80, InstFlags.RtSeqReadRtRnSPFpSimd, AddressForm.StructNoOffset),
                new(0x0C800000, 0xBFE0F000, qsizeConstraints2, InstName.St4AdvsimdMultAsPostIndex, IsaVersion.v80, InstFlags.RtSeqReadRtRnSPRmFpSimdMemWBack, AddressForm.StructPostIndexedReg),
                new(0x0D202000, 0xBFFF2000, opcodesizeOpcodesizeOpcodesizesOpcodesizeConstraints, InstName.St4AdvsimdSnglAsNoPostIndex, IsaVersion.v80, InstFlags.RtSeqReadRtRnSPFpSimd, AddressForm.StructNoOffset),
                new(0x0DA02000, 0xBFE02000, opcodesizeOpcodesizeOpcodesizesOpcodesizeConstraints, InstName.St4AdvsimdSnglAsPostIndex, IsaVersion.v80, InstFlags.RtSeqReadRtRnSPRmFpSimdMemWBack, AddressForm.StructPostIndexedReg),
                new(0xF83F9000, 0xFFFFFC00, rtRtConstraints, InstName.St64b, IsaVersion.v87, InstFlags.RtReadRtRnSP),
                new(0xF820B000, 0xFFE0FC00, rtRtConstraints, InstName.St64bv, IsaVersion.v87, InstFlags.RtReadRtRnSPRs),
                new(0xF820A000, 0xFFE0FC00, rtRtConstraints, InstName.St64bv0, IsaVersion.v87, InstFlags.RtReadRtRnSPRs),
                new(0xD9200400, 0xFFE00C00, InstName.StgPostIndexed, IsaVersion.v85, InstFlags.MemWBack, AddressForm.PostIndexed),
                new(0xD9200C00, 0xFFE00C00, InstName.StgPreIndexed, IsaVersion.v85, InstFlags.MemWBack, AddressForm.PreIndexed),
                new(0xD9200800, 0xFFE00C00, InstName.StgSignedScaledOffset, IsaVersion.v85, InstFlags.None, AddressForm.SignedScaled),
                new(0xD9A00000, 0xFFFFFC00, InstName.Stgm, IsaVersion.v85, InstFlags.None),
                new(0x68800000, 0xFFC00000, InstName.StgpPostIndexed, IsaVersion.v85, InstFlags.MemWBack, AddressForm.PostIndexed),
                new(0x69800000, 0xFFC00000, InstName.StgpPreIndexed, IsaVersion.v85, InstFlags.MemWBack, AddressForm.PreIndexed),
                new(0x69000000, 0xFFC00000, InstName.StgpSignedScaledOffset, IsaVersion.v85, InstFlags.None, AddressForm.SignedScaled),
                new(0x99000800, 0xBFE0EC00, InstName.Stilp, IsaVersion.v82, InstFlags.RtReadRtRt2RnSP),
                new(0x0D018400, 0xBFFFFC00, opcodesizeOpcodesizeOpcodesizesOpcodesizeConstraints, InstName.Stl1AdvsimdSngl, IsaVersion.v82, InstFlags.RtReadRtRnSPFpSimd, AddressForm.StructNoOffset),
                new(0x889F7C00, 0xBFFFFC00, InstName.Stllr, IsaVersion.v81, InstFlags.RtReadRtRnSP, AddressForm.BaseRegister),
                new(0x089F7C00, 0xFFFFFC00, InstName.Stllrb, IsaVersion.v81, InstFlags.RtReadRtRnSP, AddressForm.BaseRegister),
                new(0x489F7C00, 0xFFFFFC00, InstName.Stllrh, IsaVersion.v81, InstFlags.RtReadRtRnSP, AddressForm.BaseRegister),
                new(0x889FFC00, 0xBFFFFC00, InstName.StlrBaseRegister, IsaVersion.v80, InstFlags.RtReadRtRnSP, AddressForm.BaseRegister),
                new(0x99800800, 0xBFFFFC00, InstName.StlrPreIndexed, IsaVersion.v82, InstFlags.RtReadRtRnSPMemWBack, AddressForm.PreIndexed),
                new(0x089FFC00, 0xFFFFFC00, InstName.Stlrb, IsaVersion.v80, InstFlags.RtReadRtRnSP, AddressForm.BaseRegister),
                new(0x489FFC00, 0xFFFFFC00, InstName.Stlrh, IsaVersion.v80, InstFlags.RtReadRtRnSP, AddressForm.BaseRegister),
                new(0x19000000, 0xFFE00C00, InstName.Stlurb, IsaVersion.v84, InstFlags.RtReadRtRnSP, AddressForm.BasePlusOffset),
                new(0x59000000, 0xFFE00C00, InstName.Stlurh, IsaVersion.v84, InstFlags.RtReadRtRnSP, AddressForm.BasePlusOffset),
                new(0x1D000800, 0x3F600C00, opc1sizeOpc1sizeOpc1sizeConstraints, InstName.StlurFpsimd, IsaVersion.v82, InstFlags.RtReadRtRnSPFpSimd, AddressForm.BasePlusOffset),
                new(0x99000000, 0xBFE00C00, InstName.StlurGen, IsaVersion.v84, InstFlags.RtReadRtRnSP, AddressForm.BasePlusOffset),
                new(0x88208000, 0xBFE08000, InstName.Stlxp, IsaVersion.v80, InstFlags.RtReadRtRt2RnSPRs, AddressForm.BaseRegister),
                new(0x8800FC00, 0xBFE0FC00, InstName.Stlxr, IsaVersion.v80, InstFlags.RtReadRtRnSPRs, AddressForm.BaseRegister),
                new(0x0800FC00, 0xFFE0FC00, InstName.Stlxrb, IsaVersion.v80, InstFlags.RtReadRtRnSPRs, AddressForm.BaseRegister),
                new(0x4800FC00, 0xFFE0FC00, InstName.Stlxrh, IsaVersion.v80, InstFlags.RtReadRtRnSPRs, AddressForm.BaseRegister),
                new(0x2C000000, 0x3FC00000, opcConstraints, InstName.StnpFpsimd, IsaVersion.v80, InstFlags.RtReadRtRt2RnSPFpSimd, AddressForm.SignedScaled),
                new(0x28000000, 0x7FC00000, opcConstraints2, InstName.StnpGen, IsaVersion.v80, InstFlags.RtReadRtRt2RnSP, AddressForm.SignedScaled),
                new(0x2C800000, 0x3FC00000, opcConstraints, InstName.StpFpsimdPostIndexed, IsaVersion.v80, InstFlags.RtReadRtRt2RnSPFpSimdMemWBack, AddressForm.PostIndexed),
                new(0x2D800000, 0x3FC00000, opcConstraints, InstName.StpFpsimdPreIndexed, IsaVersion.v80, InstFlags.RtReadRtRt2RnSPFpSimdMemWBack, AddressForm.PreIndexed),
                new(0x2D000000, 0x3FC00000, opcConstraints, InstName.StpFpsimdSignedScaledOffset, IsaVersion.v80, InstFlags.RtReadRtRt2RnSPFpSimd, AddressForm.SignedScaled),
                new(0x28800000, 0x7FC00000, opclOpcConstraints, InstName.StpGenPostIndexed, IsaVersion.v80, InstFlags.RtReadRtRt2RnSPMemWBack, AddressForm.PostIndexed),
                new(0x29800000, 0x7FC00000, opclOpcConstraints, InstName.StpGenPreIndexed, IsaVersion.v80, InstFlags.RtReadRtRt2RnSPMemWBack, AddressForm.PreIndexed),
                new(0x29000000, 0x7FC00000, opclOpcConstraints, InstName.StpGenSignedScaledOffset, IsaVersion.v80, InstFlags.RtReadRtRt2RnSP, AddressForm.SignedScaled),
                new(0x38000400, 0xFFE00C00, InstName.StrbImmPostIndexed, IsaVersion.v80, InstFlags.RtReadRtRnSPMemWBack, AddressForm.PostIndexed),
                new(0x38000C00, 0xFFE00C00, InstName.StrbImmPreIndexed, IsaVersion.v80, InstFlags.RtReadRtRnSPMemWBack, AddressForm.PreIndexed),
                new(0x39000000, 0xFFC00000, InstName.StrbImmUnsignedScaledOffset, IsaVersion.v80, InstFlags.RtReadRtRnSP, AddressForm.UnsignedScaled),
                new(0x38200800, 0xFFE00C00, optionConstraints, InstName.StrbReg, IsaVersion.v80, InstFlags.RtReadRtRnSPRm, AddressForm.OffsetReg),
                new(0x78000400, 0xFFE00C00, InstName.StrhImmPostIndexed, IsaVersion.v80, InstFlags.RtReadRtRnSPMemWBack, AddressForm.PostIndexed),
                new(0x78000C00, 0xFFE00C00, InstName.StrhImmPreIndexed, IsaVersion.v80, InstFlags.RtReadRtRnSPMemWBack, AddressForm.PreIndexed),
                new(0x79000000, 0xFFC00000, InstName.StrhImmUnsignedScaledOffset, IsaVersion.v80, InstFlags.RtReadRtRnSP, AddressForm.UnsignedScaled),
                new(0x78200800, 0xFFE00C00, optionConstraints, InstName.StrhReg, IsaVersion.v80, InstFlags.RtReadRtRnSPRm, AddressForm.OffsetReg),
                new(0x3C000400, 0x3F600C00, opc1sizeOpc1sizeOpc1sizeConstraints, InstName.StrImmFpsimdPostIndexed, IsaVersion.v80, InstFlags.RtReadRtRnSPFpSimdMemWBack, AddressForm.PostIndexed),
                new(0x3C000C00, 0x3F600C00, opc1sizeOpc1sizeOpc1sizeConstraints, InstName.StrImmFpsimdPreIndexed, IsaVersion.v80, InstFlags.RtReadRtRnSPFpSimdMemWBack, AddressForm.PreIndexed),
                new(0x3D000000, 0x3F400000, opc1sizeOpc1sizeOpc1sizeConstraints, InstName.StrImmFpsimdUnsignedScaledOffset, IsaVersion.v80, InstFlags.RtReadRtRnSPFpSimd, AddressForm.UnsignedScaled),
                new(0xB8000400, 0xBFE00C00, InstName.StrImmGenPostIndexed, IsaVersion.v80, InstFlags.RtReadRtRnSPMemWBack, AddressForm.PostIndexed),
                new(0xB8000C00, 0xBFE00C00, InstName.StrImmGenPreIndexed, IsaVersion.v80, InstFlags.RtReadRtRnSPMemWBack, AddressForm.PreIndexed),
                new(0xB9000000, 0xBFC00000, InstName.StrImmGenUnsignedScaledOffset, IsaVersion.v80, InstFlags.RtReadRtRnSP, AddressForm.UnsignedScaled),
                new(0x3C200800, 0x3F600C00, opc1sizeOpc1sizeOpc1sizeOptionConstraints, InstName.StrRegFpsimd, IsaVersion.v80, InstFlags.RtReadRtRnSPRmFpSimd, AddressForm.OffsetReg),
                new(0xB8200800, 0xBFE00C00, optionConstraints, InstName.StrRegGen, IsaVersion.v80, InstFlags.RtReadRtRnSPRm, AddressForm.OffsetReg),
                new(0xB8000800, 0xBFE00C00, InstName.Sttr, IsaVersion.v80, InstFlags.RtReadRtRnSP, AddressForm.BasePlusOffset),
                new(0x38000800, 0xFFE00C00, InstName.Sttrb, IsaVersion.v80, InstFlags.RtReadRtRnSP, AddressForm.BasePlusOffset),
                new(0x78000800, 0xFFE00C00, InstName.Sttrh, IsaVersion.v80, InstFlags.RtReadRtRnSP, AddressForm.BasePlusOffset),
                new(0x38000000, 0xFFE00C00, InstName.Sturb, IsaVersion.v80, InstFlags.RtReadRtRnSP, AddressForm.BasePlusOffset),
                new(0x78000000, 0xFFE00C00, InstName.Sturh, IsaVersion.v80, InstFlags.RtReadRtRnSP, AddressForm.BasePlusOffset),
                new(0x3C000000, 0x3F600C00, opc1sizeOpc1sizeOpc1sizeConstraints, InstName.SturFpsimd, IsaVersion.v80, InstFlags.RtReadRtRnSPFpSimd, AddressForm.BasePlusOffset),
                new(0xB8000000, 0xBFE00C00, InstName.SturGen, IsaVersion.v80, InstFlags.RtReadRtRnSP, AddressForm.BasePlusOffset),
                new(0x88200000, 0xBFE08000, InstName.Stxp, IsaVersion.v80, InstFlags.RtReadRtRt2RnSPRs, AddressForm.BaseRegister),
                new(0x88007C00, 0xBFE0FC00, InstName.Stxr, IsaVersion.v80, InstFlags.RtReadRtRnSPRs, AddressForm.BaseRegister),
                new(0x08007C00, 0xFFE0FC00, InstName.Stxrb, IsaVersion.v80, InstFlags.RtReadRtRnSPRs, AddressForm.BaseRegister),
                new(0x48007C00, 0xFFE0FC00, InstName.Stxrh, IsaVersion.v80, InstFlags.RtReadRtRnSPRs, AddressForm.BaseRegister),
                new(0xD9E00400, 0xFFE00C00, InstName.Stz2gPostIndexed, IsaVersion.v85, InstFlags.MemWBack, AddressForm.PostIndexed),
                new(0xD9E00C00, 0xFFE00C00, InstName.Stz2gPreIndexed, IsaVersion.v85, InstFlags.MemWBack, AddressForm.PreIndexed),
                new(0xD9E00800, 0xFFE00C00, InstName.Stz2gSignedScaledOffset, IsaVersion.v85, InstFlags.None, AddressForm.SignedScaled),
                new(0xD9600400, 0xFFE00C00, InstName.StzgPostIndexed, IsaVersion.v85, InstFlags.MemWBack, AddressForm.PostIndexed),
                new(0xD9600C00, 0xFFE00C00, InstName.StzgPreIndexed, IsaVersion.v85, InstFlags.MemWBack, AddressForm.PreIndexed),
                new(0xD9600800, 0xFFE00C00, InstName.StzgSignedScaledOffset, IsaVersion.v85, InstFlags.None, AddressForm.SignedScaled),
                new(0xD9200000, 0xFFFFFC00, InstName.Stzgm, IsaVersion.v85, InstFlags.None),
                new(0xD1800000, 0xFFC0C000, InstName.Subg, IsaVersion.v85, InstFlags.None),
                new(0x0E206000, 0xBF20FC00, sizeConstraints, InstName.SubhnAdvsimd, IsaVersion.v80, InstFlags.RdReadRdRnRmFpSimd),
                new(0x9AC00000, 0xFFE0FC00, InstName.Subp, IsaVersion.v85, InstFlags.None),
                new(0xBAC00000, 0xFFE0FC00, InstName.Subps, IsaVersion.v85, InstFlags.S),
                new(0x6B200000, 0x7FE00000, opuOpuOpuConstraints, InstName.SubsAddsubExt, IsaVersion.v80, InstFlags.RdRnSPRmS),
                new(0x71000000, 0x7F800000, InstName.SubsAddsubImm, IsaVersion.v80, InstFlags.RdRnSPS),
                new(0x6B000000, 0x7F200000, shiftSfimm6Constraints, InstName.SubsAddsubShift, IsaVersion.v80, InstFlags.RdRnRmS),
                new(0x4B200000, 0x7FE00000, opuOpuOpuConstraints, InstName.SubAddsubExt, IsaVersion.v80, InstFlags.RdSPRnSPRm),
                new(0x51000000, 0x7F800000, InstName.SubAddsubImm, IsaVersion.v80, InstFlags.RdSPRnSP),
                new(0x4B000000, 0x7F200000, shiftSfimm6Constraints, InstName.SubAddsubShift, IsaVersion.v80, InstFlags.RdRnRm),
                new(0x7EE08400, 0xFFE0FC00, InstName.SubAdvsimdS, IsaVersion.v80, InstFlags.RdRnRmFpSimd),
                new(0x2E208400, 0xBF20FC00, qsizeConstraints, InstName.SubAdvsimdV, IsaVersion.v80, InstFlags.RdRnRmFpSimd),
                new(0x0F00F000, 0xBFC0F400, InstName.SudotAdvsimdElt, IsaVersion.v86, InstFlags.RdReadRdRnRmFpSimd),
                new(0x5E203800, 0xFF3FFC00, InstName.SuqaddAdvsimdS, IsaVersion.v80, InstFlags.RdReadRdRnQcFpSimd),
                new(0x0E203800, 0xBF3FFC00, qsizeConstraints, InstName.SuqaddAdvsimdV, IsaVersion.v80, InstFlags.RdReadRdRnQcFpSimd),
                new(0xD4000001, 0xFFE0001F, InstName.Svc, IsaVersion.v80, InstFlags.None),
                new(0xB8208000, 0xBF20FC00, InstName.Swp, IsaVersion.v81, InstFlags.RtReadRtRnSPRs),
                new(0x38208000, 0xFF20FC00, InstName.Swpb, IsaVersion.v81, InstFlags.RtReadRtRnSPRs),
                new(0x78208000, 0xFF20FC00, InstName.Swph, IsaVersion.v81, InstFlags.RtReadRtRnSPRs),
                new(0x19208000, 0xFF20FC00, rtRt2Constraints, InstName.Swpp, IsaVersion.None, InstFlags.RtReadRtRt2RnSP),
                new(0xD5080000, 0xFFF80000, InstName.Sys, IsaVersion.v80, InstFlags.RtReadRt),
                new(0xD5280000, 0xFFF80000, InstName.Sysl, IsaVersion.v80, InstFlags.Rt),
                new(0xD5480000, 0xFFF80000, InstName.Sysp, IsaVersion.None, InstFlags.RtReadRt),
                new(0x0E000000, 0xBFE09C00, InstName.TblAdvsimd, IsaVersion.v80, InstFlags.RdRnSeqRmFpSimd),
                new(0x37000000, 0x7F000000, InstName.Tbnz, IsaVersion.v80, InstFlags.RtReadRt),
                new(0x0E001000, 0xBFE09C00, InstName.TbxAdvsimd, IsaVersion.v80, InstFlags.RdRnSeqRmFpSimd),
                new(0x36000000, 0x7F000000, InstName.Tbz, IsaVersion.v80, InstFlags.RtReadRt),
                new(0xD4600000, 0xFFE0001F, InstName.Tcancel, IsaVersion.None, InstFlags.None),
                new(0xD503307F, 0xFFFFFFFF, InstName.Tcommit, IsaVersion.None, InstFlags.None),
                new(0x0E002800, 0xBF20FC00, qsizeConstraints, InstName.Trn1Advsimd, IsaVersion.v80, InstFlags.RdRnRmFpSimd),
                new(0x0E006800, 0xBF20FC00, qsizeConstraints, InstName.Trn2Advsimd, IsaVersion.v80, InstFlags.RdRnRmFpSimd),
                new(0xD503225F, 0xFFFFFFFF, InstName.Tsb, IsaVersion.v84, InstFlags.None),
                new(0xD5233060, 0xFFFFFFE0, InstName.Tstart, IsaVersion.None, InstFlags.RtReadRt),
                new(0xD5233160, 0xFFFFFFE0, InstName.Ttest, IsaVersion.None, InstFlags.RtReadRt),
                new(0x2E205000, 0xBF20FC00, sizeConstraints, InstName.UabalAdvsimd, IsaVersion.v80, InstFlags.RdRnRmFpSimd),
                new(0x2E207C00, 0xBF20FC00, sizeConstraints, InstName.UabaAdvsimd, IsaVersion.v80, InstFlags.RdRnRmFpSimd),
                new(0x2E207000, 0xBF20FC00, sizeConstraints, InstName.UabdlAdvsimd, IsaVersion.v80, InstFlags.RdRnRmFpSimd),
                new(0x2E207400, 0xBF20FC00, sizeConstraints, InstName.UabdAdvsimd, IsaVersion.v80, InstFlags.RdRnRmFpSimd),
                new(0x2E206800, 0xBF3FFC00, sizeConstraints, InstName.UadalpAdvsimd, IsaVersion.v80, InstFlags.RdReadRdRnFpSimd),
                new(0x2E202800, 0xBF3FFC00, sizeConstraints, InstName.UaddlpAdvsimd, IsaVersion.v80, InstFlags.RdReadRdRnFpSimd),
                new(0x2E303800, 0xBF3FFC00, qsizeSizeConstraints, InstName.UaddlvAdvsimd, IsaVersion.v80, InstFlags.RdRnFpSimd),
                new(0x2E200000, 0xBF20FC00, sizeConstraints, InstName.UaddlAdvsimd, IsaVersion.v80, InstFlags.RdRnRmFpSimd),
                new(0x2E201000, 0xBF20FC00, sizeConstraints, InstName.UaddwAdvsimd, IsaVersion.v80, InstFlags.RdRnRmFpSimd),
                new(0x53000000, 0x7F800000, sfnSfnSfimmr5Sfimms5Constraints, InstName.Ubfm, IsaVersion.v80, InstFlags.RdRn),
                new(0x7F40E400, 0xFFC0FC00, immhConstraints, InstName.UcvtfAdvsimdFixS, IsaVersion.v80, InstFlags.RdRnFpSimd),
                new(0x7F10E400, 0xFFF0FC00, immhConstraints, InstName.UcvtfAdvsimdFixS, IsaVersion.v80, InstFlags.RdRnFpSimd),
                new(0x7F20E400, 0xFFE0FC00, immhConstraints, InstName.UcvtfAdvsimdFixS, IsaVersion.v80, InstFlags.RdRnFpSimd),
                new(0x2F40E400, 0xBFC0FC00, immhQimmhConstraints, InstName.UcvtfAdvsimdFixV, IsaVersion.v80, InstFlags.RdRnFpSimd),
                new(0x2F10E400, 0xBFF0FC00, immhQimmhConstraints, InstName.UcvtfAdvsimdFixV, IsaVersion.v80, InstFlags.RdRnFpSimd),
                new(0x2F20E400, 0xBFE0FC00, immhQimmhConstraints, InstName.UcvtfAdvsimdFixV, IsaVersion.v80, InstFlags.RdRnFpSimd),
                new(0x7E79D800, 0xFFFFFC00, InstName.UcvtfAdvsimdIntSH, IsaVersion.v82, InstFlags.RdRnFpSimd),
                new(0x7E21D800, 0xFFBFFC00, InstName.UcvtfAdvsimdIntS, IsaVersion.v80, InstFlags.RdRnFpSimd),
                new(0x2E79D800, 0xBFFFFC00, InstName.UcvtfAdvsimdIntVH, IsaVersion.v82, InstFlags.RdRnFpSimd),
                new(0x2E21D800, 0xBFBFFC00, qszConstraints, InstName.UcvtfAdvsimdIntV, IsaVersion.v80, InstFlags.RdRnFpSimd),
                new(0x1E030000, 0x7FBF0000, sfscaleConstraints, InstName.UcvtfFloatFix, IsaVersion.v80, InstFlags.RdRnFpSimdFromGpr),
                new(0x1EC30000, 0x7FFF0000, sfscaleConstraints, InstName.UcvtfFloatFix, IsaVersion.v80, InstFlags.RdRnFpSimdFromGpr),
                new(0x1E230000, 0x7FBFFC00, InstName.UcvtfFloatInt, IsaVersion.v80, InstFlags.RdRnFpSimdFromGpr),
                new(0x1EE30000, 0x7FFFFC00, InstName.UcvtfFloatInt, IsaVersion.v80, InstFlags.RdRnFpSimdFromGpr),
                new(0x00000000, 0xFFFF0000, InstName.UdfPermUndef, IsaVersion.v80, InstFlags.None),
                new(0x1AC00800, 0x7FE0FC00, InstName.Udiv, IsaVersion.v80, InstFlags.RdRnRm),
                new(0x2F80E000, 0xBFC0F400, InstName.UdotAdvsimdElt, IsaVersion.v82, InstFlags.RdReadRdRnRmFpSimd),
                new(0x2E809400, 0xBFE0FC00, InstName.UdotAdvsimdVec, IsaVersion.v82, InstFlags.RdReadRdRnRmFpSimd),
                new(0x2E200400, 0xBF20FC00, sizeConstraints, InstName.UhaddAdvsimd, IsaVersion.v80, InstFlags.RdRnRmFpSimd),
                new(0x2E202400, 0xBF20FC00, sizeConstraints, InstName.UhsubAdvsimd, IsaVersion.v80, InstFlags.RdRnRmFpSimd),
                new(0x9BA00000, 0xFFE08000, InstName.Umaddl, IsaVersion.v80, InstFlags.RdRnRmRa),
                new(0x2E20A400, 0xBF20FC00, sizeConstraints, InstName.UmaxpAdvsimd, IsaVersion.v80, InstFlags.RdRnRmFpSimd),
                new(0x2E30A800, 0xBF3FFC00, qsizeSizeConstraints, InstName.UmaxvAdvsimd, IsaVersion.v80, InstFlags.RdRnFpSimd),
                new(0x2E206400, 0xBF20FC00, sizeConstraints, InstName.UmaxAdvsimd, IsaVersion.v80, InstFlags.RdRnRmFpSimd),
                new(0x11C40000, 0x7FFC0000, InstName.UmaxImm, IsaVersion.v89, InstFlags.RdRn),
                new(0x1AC06400, 0x7FE0FC00, InstName.UmaxReg, IsaVersion.v89, InstFlags.RdRnRm),
                new(0x2E20AC00, 0xBF20FC00, sizeConstraints, InstName.UminpAdvsimd, IsaVersion.v80, InstFlags.RdRnRmFpSimd),
                new(0x2E31A800, 0xBF3FFC00, qsizeSizeConstraints, InstName.UminvAdvsimd, IsaVersion.v80, InstFlags.RdRnFpSimd),
                new(0x2E206C00, 0xBF20FC00, sizeConstraints, InstName.UminAdvsimd, IsaVersion.v80, InstFlags.RdRnRmFpSimd),
                new(0x11CC0000, 0x7FFC0000, InstName.UminImm, IsaVersion.v89, InstFlags.RdRn),
                new(0x1AC06C00, 0x7FE0FC00, InstName.UminReg, IsaVersion.v89, InstFlags.RdRnRm),
                new(0x2F002000, 0xBF00F400, sizeSizeConstraints, InstName.UmlalAdvsimdElt, IsaVersion.v80, InstFlags.RdRnRmFpSimd),
                new(0x2E208000, 0xBF20FC00, sizeConstraints, InstName.UmlalAdvsimdVec, IsaVersion.v80, InstFlags.RdRnRmFpSimd),
                new(0x2F006000, 0xBF00F400, sizeSizeConstraints, InstName.UmlslAdvsimdElt, IsaVersion.v80, InstFlags.RdRnRmFpSimd),
                new(0x2E20A000, 0xBF20FC00, sizeConstraints, InstName.UmlslAdvsimdVec, IsaVersion.v80, InstFlags.RdRnRmFpSimd),
                new(0x6E80A400, 0xFFE0FC00, InstName.UmmlaAdvsimdVec, IsaVersion.v86, InstFlags.RdRnRmFpSimd),
                new(0x0E013C00, 0xFFE1FC00, InstName.UmovAdvsimd, IsaVersion.v80, InstFlags.RdRnFpSimdToGpr),
                new(0x0E023C00, 0xFFE3FC00, InstName.UmovAdvsimd, IsaVersion.v80, InstFlags.RdRnFpSimdToGpr),
                new(0x0E043C00, 0xFFE7FC00, InstName.UmovAdvsimd, IsaVersion.v80, InstFlags.RdRnFpSimdToGpr),
                new(0x4E083C00, 0xFFEFFC00, InstName.UmovAdvsimd, IsaVersion.v80, InstFlags.RdRnFpSimdToGpr),
                new(0x9BA08000, 0xFFE08000, InstName.Umsubl, IsaVersion.v80, InstFlags.RdRnRmRa),
                new(0x9BC07C00, 0xFFE0FC00, InstName.Umulh, IsaVersion.v80, InstFlags.RdRnRm),
                new(0x2F00A000, 0xBF00F400, sizeSizeConstraints, InstName.UmullAdvsimdElt, IsaVersion.v80, InstFlags.RdRnRmFpSimd),
                new(0x2E20C000, 0xBF20FC00, sizeConstraints, InstName.UmullAdvsimdVec, IsaVersion.v80, InstFlags.RdRnRmFpSimd),
                new(0x7E200C00, 0xFF20FC00, InstName.UqaddAdvsimdS, IsaVersion.v80, InstFlags.RdRnRmQcFpSimd),
                new(0x2E200C00, 0xBF20FC00, qsizeConstraints, InstName.UqaddAdvsimdV, IsaVersion.v80, InstFlags.RdRnRmQcFpSimd),
                new(0x7E205C00, 0xFF20FC00, ssizeSsizeSsizeConstraints, InstName.UqrshlAdvsimdS, IsaVersion.v80, InstFlags.RdRnRmQcFpSimd),
                new(0x2E205C00, 0xBF20FC00, qsizeConstraints, InstName.UqrshlAdvsimdV, IsaVersion.v80, InstFlags.RdRnRmQcFpSimd),
                new(0x7F009C00, 0xFF80FC00, immhImmhConstraints, InstName.UqrshrnAdvsimdS, IsaVersion.v80, InstFlags.RdReadRdRnQcFpSimd),
                new(0x2F009C00, 0xBF80FC00, immhImmhConstraints, InstName.UqrshrnAdvsimdV, IsaVersion.v80, InstFlags.RdReadRdRnQcFpSimd),
                new(0x7F007400, 0xFF80FC00, immhOpuConstraints, InstName.UqshlAdvsimdImmS, IsaVersion.v80, InstFlags.RdRnQcFpSimd),
                new(0x2F007400, 0xBF80FC00, immhQimmhOpuConstraints, InstName.UqshlAdvsimdImmV, IsaVersion.v80, InstFlags.RdRnQcFpSimd),
                new(0x7E204C00, 0xFF20FC00, ssizeSsizeSsizeConstraints, InstName.UqshlAdvsimdRegS, IsaVersion.v80, InstFlags.RdRnRmQcFpSimd),
                new(0x2E204C00, 0xBF20FC00, qsizeConstraints, InstName.UqshlAdvsimdRegV, IsaVersion.v80, InstFlags.RdRnRmQcFpSimd),
                new(0x7F009400, 0xFF80FC00, immhImmhConstraints, InstName.UqshrnAdvsimdS, IsaVersion.v80, InstFlags.RdReadRdRnQcFpSimd),
                new(0x2F009400, 0xBF80FC00, immhImmhConstraints, InstName.UqshrnAdvsimdV, IsaVersion.v80, InstFlags.RdReadRdRnQcFpSimd),
                new(0x7E202C00, 0xFF20FC00, InstName.UqsubAdvsimdS, IsaVersion.v80, InstFlags.RdRnRmQcFpSimd),
                new(0x2E202C00, 0xBF20FC00, qsizeConstraints, InstName.UqsubAdvsimdV, IsaVersion.v80, InstFlags.RdRnRmQcFpSimd),
                new(0x7E214800, 0xFF3FFC00, sizeConstraints, InstName.UqxtnAdvsimdS, IsaVersion.v80, InstFlags.RdReadRdRnQcFpSimd),
                new(0x2E214800, 0xBF3FFC00, sizeConstraints, InstName.UqxtnAdvsimdV, IsaVersion.v80, InstFlags.RdReadRdRnQcFpSimd),
                new(0x0EA1C800, 0xBFBFFC00, szConstraints, InstName.UrecpeAdvsimd, IsaVersion.v80, InstFlags.RdRnFpSimd),
                new(0x2E201400, 0xBF20FC00, sizeConstraints, InstName.UrhaddAdvsimd, IsaVersion.v80, InstFlags.RdRnRmFpSimd),
                new(0x7E205400, 0xFF20FC00, ssizeSsizeSsizeConstraints, InstName.UrshlAdvsimdS, IsaVersion.v80, InstFlags.RdRnRmFpSimd),
                new(0x2E205400, 0xBF20FC00, qsizeConstraints, InstName.UrshlAdvsimdV, IsaVersion.v80, InstFlags.RdRnRmFpSimd),
                new(0x7F402400, 0xFFC0FC00, immhConstraints, InstName.UrshrAdvsimdS, IsaVersion.v80, InstFlags.RdRnFpSimd),
                new(0x2F002400, 0xBF80FC00, immhQimmhConstraints, InstName.UrshrAdvsimdV, IsaVersion.v80, InstFlags.RdRnFpSimd),
                new(0x2EA1C800, 0xBFBFFC00, szConstraints, InstName.UrsqrteAdvsimd, IsaVersion.v80, InstFlags.RdRnFpSimd),
                new(0x7F403400, 0xFFC0FC00, immhConstraints, InstName.UrsraAdvsimdS, IsaVersion.v80, InstFlags.RdRnFpSimd),
                new(0x2F003400, 0xBF80FC00, immhQimmhConstraints, InstName.UrsraAdvsimdV, IsaVersion.v80, InstFlags.RdRnFpSimd),
                new(0x0F80F000, 0xBFC0F400, InstName.UsdotAdvsimdElt, IsaVersion.v86, InstFlags.RdReadRdRnRmFpSimd),
                new(0x0E809C00, 0xBFE0FC00, InstName.UsdotAdvsimdVec, IsaVersion.v86, InstFlags.RdReadRdRnRmFpSimd),
                new(0x2F00A400, 0xBF80FC00, immhImmhConstraints, InstName.UshllAdvsimd, IsaVersion.v80, InstFlags.RdRnFpSimd),
                new(0x7E204400, 0xFF20FC00, ssizeSsizeSsizeConstraints, InstName.UshlAdvsimdS, IsaVersion.v80, InstFlags.RdRnRmFpSimd),
                new(0x2E204400, 0xBF20FC00, qsizeConstraints, InstName.UshlAdvsimdV, IsaVersion.v80, InstFlags.RdRnRmFpSimd),
                new(0x7F400400, 0xFFC0FC00, immhConstraints, InstName.UshrAdvsimdS, IsaVersion.v80, InstFlags.RdRnFpSimd),
                new(0x2F000400, 0xBF80FC00, immhQimmhConstraints, InstName.UshrAdvsimdV, IsaVersion.v80, InstFlags.RdRnFpSimd),
                new(0x4E80AC00, 0xFFE0FC00, InstName.UsmmlaAdvsimdVec, IsaVersion.v86, InstFlags.RdRnRmFpSimd),
                new(0x7E203800, 0xFF3FFC00, InstName.UsqaddAdvsimdS, IsaVersion.v80, InstFlags.RdReadRdRnQcFpSimd),
                new(0x2E203800, 0xBF3FFC00, qsizeConstraints, InstName.UsqaddAdvsimdV, IsaVersion.v80, InstFlags.RdReadRdRnQcFpSimd),
                new(0x7F401400, 0xFFC0FC00, immhConstraints, InstName.UsraAdvsimdS, IsaVersion.v80, InstFlags.RdRnFpSimd),
                new(0x2F001400, 0xBF80FC00, immhQimmhConstraints, InstName.UsraAdvsimdV, IsaVersion.v80, InstFlags.RdRnFpSimd),
                new(0x2E202000, 0xBF20FC00, sizeConstraints, InstName.UsublAdvsimd, IsaVersion.v80, InstFlags.RdRnRmFpSimd),
                new(0x2E203000, 0xBF20FC00, sizeConstraints, InstName.UsubwAdvsimd, IsaVersion.v80, InstFlags.RdRnRmFpSimd),
                new(0x0E001800, 0xBF20FC00, qsizeConstraints, InstName.Uzp1Advsimd, IsaVersion.v80, InstFlags.RdRnRmFpSimd),
                new(0x0E005800, 0xBF20FC00, qsizeConstraints, InstName.Uzp2Advsimd, IsaVersion.v80, InstFlags.RdRnRmFpSimd),
                new(0xD503205F, 0xFFFFFFFF, InstName.Wfe, IsaVersion.v80, InstFlags.None),
                new(0xD5031000, 0xFFFFFFE0, InstName.Wfet, IsaVersion.v87, InstFlags.Rd),
                new(0xD503207F, 0xFFFFFFFF, InstName.Wfi, IsaVersion.v80, InstFlags.None),
                new(0xD5031020, 0xFFFFFFE0, InstName.Wfit, IsaVersion.v87, InstFlags.Rd),
                new(0xD500403F, 0xFFFFFFFF, InstName.Xaflag, IsaVersion.v85, InstFlags.C),
                new(0xCE800000, 0xFFE00000, InstName.XarAdvsimd, IsaVersion.v82, InstFlags.RdRnRmFpSimd),
                new(0xDAC143E0, 0xFFFFFBE0, InstName.XpacGeneral, IsaVersion.v83, InstFlags.Rd),
                new(0xD50320FF, 0xFFFFFFFF, InstName.XpacSystem, IsaVersion.v83, InstFlags.None),
                new(0x0E212800, 0xBF3FFC00, sizeConstraints, InstName.XtnAdvsimd, IsaVersion.v80, InstFlags.RdReadRdRnFpSimd),
                new(0xD503203F, 0xFFFFFFFF, InstName.Yield, IsaVersion.v80, InstFlags.None),
                new(0x0E003800, 0xBF20FC00, qsizeConstraints, InstName.Zip1Advsimd, IsaVersion.v80, InstFlags.RdRnRmFpSimd),
                new(0x0E007800, 0xBF20FC00, qsizeConstraints, InstName.Zip2Advsimd, IsaVersion.v80, InstFlags.RdRnRmFpSimd),
            ];

            _table = new(insts);
        }

        public static (InstName, InstFlags, AddressForm) GetInstNameAndFlags(uint encoding, IsaVersion version, IsaFeature features)
        {
            if (_table.TryFind(encoding, version, features, out InstInfo info))
            {
                return (info.Name, info.Flags, info.AddressForm);
            }

            return new(InstName.UdfPermUndef, InstFlags.None, AddressForm.None);
        }
    }
}
