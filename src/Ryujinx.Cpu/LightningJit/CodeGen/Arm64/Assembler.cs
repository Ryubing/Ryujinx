using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Ryujinx.Cpu.LightningJit.CodeGen.Arm64
{
    struct Assembler
    {
        private const uint SfFlag = 1u << 31;

        public const int SpRegister = 31;
        public const int ZrRegister = 31;

        private readonly List<uint> _code;

        private class LabelState
        {
            public int BranchIndex;
            public int TargetIndex;
            public bool HasBranch;
            public bool HasTarget;
        }

        private readonly List<LabelState> _labels;

        public Assembler(CodeWriter writer)
        {
            _code = writer.GetList();
            _labels = [];
        }

        public readonly Operand CreateLabel()
        {
            int labelIndex = _labels.Count;
            _labels.Add(new LabelState());

            return new Operand(OperandKind.Label, OperandType.None, (ulong)labelIndex);
        }

        public readonly void MarkLabel(Operand label)
        {
            int targetIndex = _code.Count;

            LabelState state = _labels[label.AsInt32()];

            state.TargetIndex = targetIndex;
            state.HasTarget = true;

            if (state.HasBranch)
            {
                int imm = (targetIndex - state.BranchIndex) * sizeof(uint);
                uint code = _code[state.BranchIndex];

                if ((code & 0xfc000000u) == 0x14000000u)
                {
                    _code[state.BranchIndex] = code | EncodeSImm26_2(imm);
                }
                else
                {
                    _code[state.BranchIndex] = code | (EncodeSImm19_2(imm) << 5);
                }
            }
        }

        // Base

        public readonly void B(Operand label, ArmCondition condition = ArmCondition.Al)
        {
            int branchIndex = _code.Count;

            LabelState state = _labels[label.AsInt32()];

            state.BranchIndex = branchIndex;
            state.HasBranch = true;

            int imm = 0;

            if (state.HasTarget)
            {
                imm = (state.TargetIndex - branchIndex) * sizeof(uint);
            }

            if (condition == ArmCondition.Al)
            {
                B(imm);
            }
            else
            {
                B(condition, imm);
            }
        }

        public readonly void Cbz(Operand rt, Operand label)
        {
            int branchIndex = _code.Count;

            LabelState state = _labels[label.AsInt32()];

            state.BranchIndex = branchIndex;
            state.HasBranch = true;

            int imm = 0;

            if (state.HasTarget)
            {
                imm = (state.TargetIndex - branchIndex) * sizeof(uint);
            }

            Cbz(rt, imm);
        }

        public readonly void Cbnz(Operand rt, Operand label)
        {
            int branchIndex = _code.Count;

            LabelState state = _labels[label.AsInt32()];

            state.BranchIndex = branchIndex;
            state.HasBranch = true;

            int imm = 0;

            if (state.HasTarget)
            {
                imm = (state.TargetIndex - branchIndex) * sizeof(uint);
            }

            Cbnz(rt, imm);
        }

        public readonly void Adc(Operand rd, Operand rn, Operand rm)
        {
            WriteInstructionRm16(0x1a000000u, rd, rn, rm);
        }

        public readonly void Adcs(Operand rd, Operand rn, Operand rm)
        {
            WriteInstructionRm16(0x3a000000u, rd, rn, rm);
        }

        public readonly void Add(Operand rd, Operand rn, Operand rm, ArmExtensionType extensionType, int shiftAmount = 0)
        {
            WriteInstructionAuto(0x0b200000u, rd, rn, rm, extensionType, shiftAmount);
        }

        public readonly void Add(Operand rd, Operand rn, Operand rm)
        {
            Add(rd, rn, rm, ArmShiftType.Lsl, 0);
        }

        public readonly void Add(Operand rd, Operand rn, Operand rm, ArmShiftType shiftType, int shiftAmount)
        {
            Add(rd, rn, rm, shiftType, shiftAmount, false);
        }

        public readonly void Add(Operand rd, Operand rn, Operand rm, ArmShiftType shiftType, int shiftAmount, bool immForm)
        {
            WriteInstructionAuto(0x11000000u, 0x0b000000u, rd, rn, rm, shiftType, shiftAmount, immForm);
        }

        public readonly void Adds(Operand rd, Operand rn, Operand rm)
        {
            Adds(rd, rn, rm, ArmShiftType.Lsl, 0);
        }

        public readonly void Adds(Operand rd, Operand rn, Operand rm, ArmShiftType shiftType, int shiftAmount)
        {
            Adds(rd, rn, rm, shiftType, shiftAmount, false);
        }

        public readonly void Adds(Operand rd, Operand rn, Operand rm, ArmShiftType shiftType, int shiftAmount, bool immForm)
        {
            WriteInstructionAuto(0x31000000u, 0x2b000000u, rd, rn, rm, shiftType, shiftAmount, immForm);
        }

        public readonly void And(Operand rd, Operand rn, Operand rm)
        {
            And(rd, rn, rm, ArmShiftType.Lsl, 0);
        }

        public readonly void And(Operand rd, Operand rn, Operand rm, ArmShiftType shiftType, int shiftAmount)
        {
            WriteInstructionBitwiseAuto(0x12000000u, 0x0a000000u, rd, rn, rm, shiftType, shiftAmount);
        }

        public readonly void Ands(Operand rd, Operand rn, Operand rm)
        {
            Ands(rd, rn, rm, ArmShiftType.Lsl, 0);
        }

        public readonly void Ands(Operand rd, Operand rn, Operand rm, ArmShiftType shiftType, int shiftAmount)
        {
            WriteInstructionBitwiseAuto(0x72000000u, 0x6a000000u, rd, rn, rm, shiftType, shiftAmount);
        }

        public readonly void Asr(Operand rd, Operand rn, Operand rm)
        {
            if (rm.Kind == OperandKind.Constant)
            {
                int shift = rm.AsInt32();
                int mask = rd.Type == OperandType.I64 ? 63 : 31;
                shift &= mask;
                Sbfm(rd, rn, shift, mask);
            }
            else
            {
                Asrv(rd, rn, rm);
            }
        }

        public readonly void Asrv(Operand rd, Operand rn, Operand rm)
        {
            WriteInstructionBitwiseAuto(0x1ac02800u, rd, rn, rm);
        }

        public readonly void B(int imm)
        {
            WriteUInt32(0x14000000u | EncodeSImm26_2(imm));
        }

        public readonly void B(ArmCondition condition, int imm)
        {
            WriteUInt32(0x54000000u | (uint)condition | (EncodeSImm19_2(imm) << 5));
        }

        public readonly void Bfc(Operand rd, int lsb, int width)
        {
            Bfi(rd, new Operand(ZrRegister, RegisterType.Integer, OperandType.I32), lsb, width);
        }

        public readonly void Bfi(Operand rd, Operand rn, int lsb, int width)
        {
            Bfm(rd, rn, -lsb & 31, width - 1);
        }

        public readonly void Bfm(Operand rd, Operand rn, int immr, int imms)
        {
            WriteInstruction(0x33000000u | (EncodeUImm6(imms) << 10) | (EncodeUImm6(immr) << 16), rd, rn);
        }

        public readonly void Bfxil(Operand rd, Operand rn, int lsb, int width)
        {
            Bfm(rd, rn, lsb, lsb + width - 1);
        }

        public readonly void Bic(Operand rd, Operand rn, Operand rm)
        {
            Bic(rd, rn, rm, ArmShiftType.Lsl, 0);
        }

        public readonly void Bic(Operand rd, Operand rn, Operand rm, ArmShiftType shiftType, int shiftAmount)
        {
            WriteInstructionAuto(0x0a200000u, rd, rn, rm, shiftType, shiftAmount);
        }

        public readonly void Bics(Operand rd, Operand rn, Operand rm)
        {
            Bics(rd, rn, rm, ArmShiftType.Lsl, 0);
        }

        public readonly void Bics(Operand rd, Operand rn, Operand rm, ArmShiftType shiftType, int shiftAmount)
        {
            WriteInstructionAuto(0x6a200000u, rd, rn, rm, shiftType, shiftAmount);
        }

        public readonly void Blr(Operand rn)
        {
            WriteUInt32(0xd63f0000u | (EncodeReg(rn) << 5));
        }

        public readonly void Br(Operand rn)
        {
            WriteUInt32(0xd61f0000u | (EncodeReg(rn) << 5));
        }

        public readonly void Brk()
        {
            WriteUInt32(0xd4200000u);
        }

        public readonly void Cbz(Operand rt, int imm)
        {
            WriteInstructionAuto(0x34000000u | (EncodeSImm19_2(imm) << 5), rt);
        }

        public readonly void Cbnz(Operand rt, int imm)
        {
            WriteInstructionAuto(0x35000000u | (EncodeSImm19_2(imm) << 5), rt);
        }

        public readonly void Crc32(Operand rd, Operand rn, Operand rm, uint sz)
        {
            Debug.Assert(sz <= 3);
            WriteInstructionRm16(0x1ac04000u | (sz << 10), rd, rn, rm);
        }

        public readonly void Crc32c(Operand rd, Operand rn, Operand rm, uint sz)
        {
            Debug.Assert(sz <= 3);
            WriteInstructionRm16(0x1ac05000u | (sz << 10), rd, rn, rm);
        }

        public readonly void Clrex(int crm = 15)
        {
            WriteUInt32(0xd503305fu | (EncodeUImm4(crm) << 8));
        }

        public readonly void Clz(Operand rd, Operand rn)
        {
            WriteInstructionAuto(0x5ac01000u, rd, rn);
        }

        public readonly void Cmn(Operand rn, Operand rm)
        {
            Cmn(rn, rm, ArmShiftType.Lsl, 0);
        }

        public readonly void Cmn(Operand rn, Operand rm, ArmShiftType shiftType, int shiftAmount)
        {
            Adds(new Operand(ZrRegister, RegisterType.Integer, rn.Type), rn, rm, shiftType, shiftAmount);
        }

        public readonly void Cmp(Operand rn, Operand rm)
        {
            Cmp(rn, rm, ArmShiftType.Lsl, 0);
        }

        public readonly void Cmp(Operand rn, Operand rm, ArmShiftType shiftType, int shiftAmount)
        {
            Subs(new Operand(ZrRegister, RegisterType.Integer, rn.Type), rn, rm, shiftType, shiftAmount);
        }

        public readonly void Csdb()
        {
            WriteUInt32(0xd503229fu);
        }

        public readonly void Csel(Operand rd, Operand rn, Operand rm, ArmCondition condition)
        {
            WriteInstructionBitwiseAuto(0x1a800000u | ((uint)condition << 12), rd, rn, rm);
        }

        public readonly void Cset(Operand rd, ArmCondition condition)
        {
            Operand zr = new(ZrRegister, RegisterType.Integer, rd.Type);
            Csinc(rd, zr, zr, (ArmCondition)((int)condition ^ 1));
        }

        public readonly void Csinc(Operand rd, Operand rn, Operand rm, ArmCondition condition)
        {
            WriteInstructionBitwiseAuto(0x1a800400u | ((uint)condition << 12), rd, rn, rm);
        }

        public readonly void Dmb(uint option)
        {
            WriteUInt32(0xd50330bfu | (option << 8));
        }

        public readonly void Dsb(uint option)
        {
            WriteUInt32(0xd503309fu | (option << 8));
        }

        public readonly void Eor(Operand rd, Operand rn, Operand rm)
        {
            Eor(rd, rn, rm, ArmShiftType.Lsl, 0);
        }

        public readonly void Eor(Operand rd, Operand rn, Operand rm, ArmShiftType shiftType, int shiftAmount)
        {
            WriteInstructionBitwiseAuto(0x52000000u, 0x4a000000u, rd, rn, rm, shiftType, shiftAmount);
        }

        public readonly void Eors(Operand rd, Operand rn, Operand rm)
        {
            Eors(rd, rn, rm, ArmShiftType.Lsl, 0);
        }

        public readonly void Eors(Operand rd, Operand rn, Operand rm, ArmShiftType shiftType, int shiftAmount)
        {
            Eor(rd, rn, rm, shiftType, shiftAmount);
            Tst(rd, rd);
        }

        public readonly void Esb()
        {
            WriteUInt32(0xd503221fu);
        }

        public readonly void Extr(Operand rd, Operand rn, Operand rm, int imms)
        {
            uint n = rd.Type == OperandType.I64 ? 1u << 22 : 0u;
            WriteInstructionBitwiseAuto(0x13800000u | n | (EncodeUImm6(imms) << 10), rd, rn, rm);
        }

        public readonly void Isb(uint option)
        {
            WriteUInt32(0xd50330dfu | (option << 8));
        }

        public readonly void Ldar(Operand rt, Operand rn)
        {
            WriteInstruction(0x88dffc00u | ((rt.Type == OperandType.I64 ? 3u : 2u) << 30), rt, rn);
        }

        public readonly void Ldarb(Operand rt, Operand rn)
        {
            WriteInstruction(0x08dffc00u, rt, rn);
        }

        public readonly void Ldarh(Operand rt, Operand rn)
        {
            WriteInstruction(0x48dffc00u, rt, rn);
        }

        public readonly void Ldaxp(Operand rt, Operand rt2, Operand rn)
        {
            WriteInstruction(0x887f8000u | ((rt.Type == OperandType.I64 ? 3u : 2u) << 30), rt, rn, rt2);
        }

        public readonly void Ldaxr(Operand rt, Operand rn)
        {
            WriteInstruction(0x885ffc00u | ((rt.Type == OperandType.I64 ? 3u : 2u) << 30), rt, rn);
        }

        public readonly void Ldaxrb(Operand rt, Operand rn)
        {
            WriteInstruction(0x085ffc00u, rt, rn);
        }

        public readonly void Ldaxrh(Operand rt, Operand rn)
        {
            WriteInstruction(0x485ffc00u, rt, rn);
        }

        public readonly void LdpRiPost(Operand rt, Operand rt2, Operand rn, int imm)
        {
            uint instruction = GetLdpStpInstruction(0x28c00000u, 0x2cc00000u, imm, rt.Type);
            WriteInstruction(instruction, rt, rn, rt2);
        }

        public readonly void LdpRiPre(Operand rt, Operand rt2, Operand rn, int imm)
        {
            uint instruction = GetLdpStpInstruction(0x29c00000u, 0x2dc00000u, imm, rt.Type);
            WriteInstruction(instruction, rt, rn, rt2);
        }

        public readonly void LdpRiUn(Operand rt, Operand rt2, Operand rn, int imm)
        {
            uint instruction = GetLdpStpInstruction(0x29400000u, 0x2d400000u, imm, rt.Type);
            WriteInstruction(instruction, rt, rn, rt2);
        }

        public readonly void LdrLit(Operand rt, int offset)
        {
            uint instruction = 0x18000000u | (EncodeSImm19_2(offset) << 5);

            if (rt.Type == OperandType.I64)
            {
                instruction |= 1u << 30;
            }

            WriteInstruction(instruction, rt);
        }

        public readonly void LdrRiPost(Operand rt, Operand rn, int imm)
        {
            uint instruction = GetLdrStrInstruction(0xb8400400u, 0x3c400400u, rt.Type) | (EncodeSImm9(imm) << 12);
            WriteInstruction(instruction, rt, rn);
        }

        public readonly void LdrRiPre(Operand rt, Operand rn, int imm)
        {
            uint instruction = GetLdrStrInstruction(0xb8400c00u, 0x3c400c00u, rt.Type) | (EncodeSImm9(imm) << 12);
            WriteInstruction(instruction, rt, rn);
        }

        public readonly void LdrRiUn(Operand rt, Operand rn, int imm)
        {
            uint instruction = GetLdrStrInstruction(0xb9400000u, 0x3d400000u, rt.Type) | (EncodeUImm12(imm, rt.Type) << 10);
            WriteInstruction(instruction, rt, rn);
        }

        public readonly void LdrRiUn(Operand rt, Operand rn, int imm, uint size)
        {
            uint instruction = GetLdrStrInstruction(0xb9400000u, 0x3d400000u, rt.Type, size) | (EncodeUImm12(imm, (int)size) << 10);
            WriteInstruction(instruction, rt, rn);
        }

        public readonly void LdrRr(Operand rt, Operand rn, Operand rm, ArmExtensionType extensionType, bool shift)
        {
            uint instruction = GetLdrStrInstruction(0xb8600800u, 0x3ce00800u, rt.Type);
            WriteInstructionLdrStrAuto(instruction, rt, rn, rm, extensionType, shift);
        }

        public readonly void LdrbRiPost(Operand rt, Operand rn, int imm)
        {
            uint instruction = 0x38400400u | (EncodeSImm9(imm) << 12);
            WriteInstruction(instruction, rt, rn);
        }

        public readonly void LdrbRiPre(Operand rt, Operand rn, int imm)
        {
            uint instruction = 0x38400c00u | (EncodeSImm9(imm) << 12);
            WriteInstruction(instruction, rt, rn);
        }

        public readonly void LdrbRiUn(Operand rt, Operand rn, int imm)
        {
            uint instruction = 0x39400000u | (EncodeUImm12(imm, 0) << 10);
            WriteInstruction(instruction, rt, rn);
        }

        public readonly void LdrhRiPost(Operand rt, Operand rn, int imm)
        {
            uint instruction = 0x78400400u | (EncodeSImm9(imm) << 12);
            WriteInstruction(instruction, rt, rn);
        }

        public readonly void LdrhRiPre(Operand rt, Operand rn, int imm)
        {
            uint instruction = 0x78400c00u | (EncodeSImm9(imm) << 12);
            WriteInstruction(instruction, rt, rn);
        }

        public readonly void LdrhRiUn(Operand rt, Operand rn, int imm)
        {
            uint instruction = 0x79400000u | (EncodeUImm12(imm, 1) << 10);
            WriteInstruction(instruction, rt, rn);
        }

        public readonly void LdrsbRiPost(Operand rt, Operand rn, int imm)
        {
            WriteInstruction(0x38800400u | (EncodeSImm9(imm) << 12), rt, rn);
        }

        public readonly void LdrsbRiPre(Operand rt, Operand rn, int imm)
        {
            WriteInstruction(0x38800c00u | (EncodeSImm9(imm) << 12), rt, rn);
        }

        public readonly void LdrsbRiUn(Operand rt, Operand rn, int imm)
        {
            WriteInstruction(0x39800000u | (EncodeUImm12(imm, 0) << 10), rt, rn);
        }

        public readonly void LdrshRiPost(Operand rt, Operand rn, int imm)
        {
            WriteInstruction(0x78800400u | (EncodeSImm9(imm) << 12), rt, rn);
        }

        public readonly void LdrshRiPre(Operand rt, Operand rn, int imm)
        {
            WriteInstruction(0x78800c00u | (EncodeSImm9(imm) << 12), rt, rn);
        }

        public readonly void LdrshRiUn(Operand rt, Operand rn, int imm)
        {
            WriteInstruction(0x79800000u | (EncodeUImm12(imm, 1) << 10), rt, rn);
        }

        public readonly void LdrswRiPost(Operand rt, Operand rn, int imm)
        {
            WriteInstruction(0xb8800400u | (EncodeSImm9(imm) << 12), rt, rn);
        }

        public readonly void LdrswRiPre(Operand rt, Operand rn, int imm)
        {
            WriteInstruction(0xb8800c00u | (EncodeSImm9(imm) << 12), rt, rn);
        }

        public readonly void LdrswRiUn(Operand rt, Operand rn, int imm)
        {
            WriteInstruction(0xb9800000u | (EncodeUImm12(imm, 2) << 10), rt, rn);
        }

        public readonly void Ldur(Operand rt, Operand rn, int imm)
        {
            uint instruction = GetLdrStrInstruction(0xb8400000u, 0x3c400000u, rt.Type) | (EncodeSImm9(imm) << 12);
            WriteInstruction(instruction, rt, rn);
        }

        public readonly void Ldur(Operand rt, Operand rn, int imm, uint size)
        {
            uint instruction = GetLdrStrInstruction(0xb8400000u, 0x3c400000u, rt.Type, size) | (EncodeSImm9(imm) << 12);
            WriteInstruction(instruction, rt, rn);
        }

        public readonly void Ldurb(Operand rt, Operand rn, int imm)
        {
            uint instruction = 0x38400000u | (EncodeSImm9(imm) << 12);
            WriteInstruction(instruction, rt, rn);
        }

        public readonly void Ldurh(Operand rt, Operand rn, int imm)
        {
            uint instruction = 0x78400000u | (EncodeSImm9(imm) << 12);
            WriteInstruction(instruction, rt, rn);
        }

        public readonly void Ldursb(Operand rt, Operand rn, int imm)
        {
            uint instruction = 0x38800000u | (EncodeSImm9(imm) << 12);
            WriteInstruction(instruction, rt, rn);
        }

        public readonly void Ldursh(Operand rt, Operand rn, int imm)
        {
            uint instruction = 0x78800000u | (EncodeSImm9(imm) << 12);
            WriteInstruction(instruction, rt, rn);
        }

        public readonly void Ldursw(Operand rt, Operand rn, int imm)
        {
            uint instruction = 0xb8800000u | (EncodeSImm9(imm) << 12);
            WriteInstruction(instruction, rt, rn);
        }

        public readonly void Lsl(Operand rd, Operand rn, Operand rm)
        {
            if (rm.Kind == OperandKind.Constant)
            {
                int shift = rm.AsInt32();
                int mask = rd.Type == OperandType.I64 ? 63 : 31;
                shift &= mask;
                Ubfm(rd, rn, -shift & mask, mask - shift);
            }
            else
            {
                Lslv(rd, rn, rm);
            }
        }

        public readonly void Lslv(Operand rd, Operand rn, Operand rm)
        {
            WriteInstructionBitwiseAuto(0x1ac02000u, rd, rn, rm);
        }

        public readonly void Lsr(Operand rd, Operand rn, Operand rm)
        {
            if (rm.Kind == OperandKind.Constant)
            {
                int shift = rm.AsInt32();
                int mask = rd.Type == OperandType.I64 ? 63 : 31;
                shift &= mask;
                Ubfm(rd, rn, shift, mask);
            }
            else
            {
                Lsrv(rd, rn, rm);
            }
        }

        public readonly void Lsrv(Operand rd, Operand rn, Operand rm)
        {
            WriteInstructionBitwiseAuto(0x1ac02400u, rd, rn, rm);
        }

        public readonly void Madd(Operand rd, Operand rn, Operand rm, Operand ra)
        {
            WriteInstructionAuto(0x1b000000u, rd, rn, rm, ra);
        }

        public readonly void Msub(Operand rd, Operand rn, Operand rm, Operand ra)
        {
            WriteInstructionAuto(0x1b008000u, rd, rn, rm, ra);
        }

        public readonly void Mul(Operand rd, Operand rn, Operand rm)
        {
            Madd(rd, rn, rm, new Operand(ZrRegister, RegisterType.Integer, rd.Type));
        }

        public readonly void Mov(Operand rd, Operand rn)
        {
            Debug.Assert(rd.Type.IsInteger());
            Orr(rd, new Operand(ZrRegister, RegisterType.Integer, rd.Type), rn);
        }

        public readonly void MovSp(Operand rd, Operand rn)
        {
            if (rd.GetRegister().Index == SpRegister ||
                rn.GetRegister().Index == SpRegister)
            {
                Add(rd, rn, new Operand(rd.Type, 0), ArmShiftType.Lsl, 0, immForm: true);
            }
            else
            {
                Mov(rd, rn);
            }
        }

        public readonly void Mov(Operand rd, ulong value)
        {
            if (value == 0)
            {
                Mov(rd, new Operand(ZrRegister, RegisterType.Integer, rd.Type));
            }
            else if (CodeGenCommon.TryEncodeBitMask(rd.Type, value, out _, out _, out _))
            {
                Orr(rd, new Operand(ZrRegister, RegisterType.Integer, rd.Type), new Operand(OperandKind.Constant, rd.Type, value));
            }
            else if (value == ulong.MaxValue || (value == uint.MaxValue && rd.Type == OperandType.I32))
            {
                Mvn(rd, new Operand(ZrRegister, RegisterType.Integer, rd.Type));
            }
            else
            {
                int hw = 0;
                bool first = true;

                while (value != 0)
                {
                    int valueLow = (ushort)value;
                    if (valueLow != 0)
                    {
                        if (first)
                        {
                            Movz(rd, valueLow, hw);
                            first = false;
                        }
                        else
                        {
                            Movk(rd, valueLow, hw);
                        }
                    }

                    hw++;
                    value >>= 16;
                }
            }
        }

        public readonly void Mov(Operand rd, int imm)
        {
            Movz(rd, imm, 0);
        }

        public readonly void Movz(Operand rd, int imm, int hw)
        {
            Debug.Assert((hw & (rd.Type == OperandType.I64 ? 3 : 1)) == hw);
            WriteInstructionAuto(0x52800000u | (EncodeUImm16(imm) << 5) | ((uint)hw << 21), rd);
        }

        public readonly void Movk(Operand rd, int imm, int hw)
        {
            Debug.Assert((hw & (rd.Type == OperandType.I64 ? 3 : 1)) == hw);
            WriteInstructionAuto(0x72800000u | (EncodeUImm16(imm) << 5) | ((uint)hw << 21), rd);
        }

        public readonly void Mrs(Operand rt, uint o0, uint op1, uint crn, uint crm, uint op2)
        {
            WriteSysRegInstruction(0xd5300000u, rt, o0, op1, crn, crm, op2);
        }

        public readonly void MrsNzcv(Operand rt)
        {
            WriteInstruction(0xd53b4200u, rt);
        }

        public readonly void MrsFpcr(Operand rt)
        {
            WriteInstruction(0xd53b4400u, rt);
        }

        public readonly void MrsFpsr(Operand rt)
        {
            WriteInstruction(0xd53b4420u, rt);
        }

        public readonly void MrsTpidrEl0(Operand rt)
        {
            WriteInstruction(0xd53bd040u, rt);
        }

        public readonly void MrsTpidrroEl0(Operand rt)
        {
            WriteInstruction(0xd53bd060u, rt);
        }

        public readonly void Msr(Operand rt, uint o0, uint op1, uint crn, uint crm, uint op2)
        {
            WriteSysRegInstruction(0xd5100000u, rt, o0, op1, crn, crm, op2);
        }

        public readonly void MsrNzcv(Operand rt)
        {
            WriteInstruction(0xd51b4200u, rt);
        }

        public readonly void MsrFpcr(Operand rt)
        {
            WriteInstruction(0xd51b4400, rt);
        }

        public readonly void MsrFpsr(Operand rt)
        {
            WriteInstruction(0xd51b4420, rt);
        }

        public readonly void Mvn(Operand rd, Operand rn, ArmShiftType shiftType = ArmShiftType.Lsl, int shiftAmount = 0)
        {
            Orn(rd, new Operand(ZrRegister, RegisterType.Integer, rd.Type), rn, shiftType, shiftAmount);
        }

        public readonly void Neg(Operand rd, Operand rn, ArmShiftType shiftType = ArmShiftType.Lsl, int shiftAmount = 0)
        {
            Sub(rd, new Operand(ZrRegister, RegisterType.Integer, rd.Type), rn, shiftType, shiftAmount);
        }

        public readonly void Negs(Operand rd, Operand rn, ArmShiftType shiftType = ArmShiftType.Lsl, int shiftAmount = 0)
        {
            Subs(rd, new Operand(ZrRegister, RegisterType.Integer, rd.Type), rn, shiftType, shiftAmount);
        }

        public readonly void Orn(Operand rd, Operand rn, Operand rm)
        {
            Orn(rd, rn, rm, ArmShiftType.Lsl, 0);
        }

        public readonly void Orn(Operand rd, Operand rn, Operand rm, ArmShiftType shiftType, int shiftAmount)
        {
            WriteInstructionBitwiseAuto(0x2a200000u, rd, rn, rm, shiftType, shiftAmount);
        }

        public readonly void Orns(Operand rd, Operand rn, Operand rm)
        {
            Orns(rd, rn, rm, ArmShiftType.Lsl, 0);
        }

        public readonly void Orns(Operand rd, Operand rn, Operand rm, ArmShiftType shiftType, int shiftAmount)
        {
            Orn(rd, rn, rm, shiftType, shiftAmount);
            Tst(rd, rd);
        }

        public readonly void Orr(Operand rd, Operand rn, Operand rm)
        {
            Orr(rd, rn, rm, ArmShiftType.Lsl, 0);
        }

        public readonly void Orr(Operand rd, Operand rn, Operand rm, ArmShiftType shiftType, int shiftAmount)
        {
            WriteInstructionBitwiseAuto(0x32000000u, 0x2a000000u, rd, rn, rm, shiftType, shiftAmount);
        }

        public readonly void Orrs(Operand rd, Operand rn, Operand rm)
        {
            Orrs(rd, rn, rm, ArmShiftType.Lsl, 0);
        }

        public readonly void Orrs(Operand rd, Operand rn, Operand rm, ArmShiftType shiftType, int shiftAmount)
        {
            Orr(rd, rn, rm, shiftType, shiftAmount);
            Tst(rd, rd);
        }

        public readonly void PrfmI(Operand rn, int imm, uint type, uint target, uint policy)
        {
            Operand rt = new((int)EncodeTypeTargetPolicy(type, target, policy), RegisterType.Integer, OperandType.I32);
            WriteInstruction(0xf9800000u | (EncodeUImm12(imm, 3) << 10), rt, rn);
        }

        public readonly void PrfmR(Operand rt, Operand rn)
        {
            WriteInstruction(0xf8a04800u, rt, rn);
        }

        public readonly void Prfum(Operand rn, int imm, uint type, uint target, uint policy)
        {
            Operand rt = new((int)EncodeTypeTargetPolicy(type, target, policy), RegisterType.Integer, OperandType.I32);
            WriteInstruction(0xf8800000u | (EncodeSImm9(imm) << 12), rt, rn);
        }

        public readonly void Rbit(Operand rd, Operand rn)
        {
            WriteInstructionAuto(0x5ac00000u, rd, rn);
        }

        public readonly void Ret()
        {
            Ret(new Operand(30, RegisterType.Integer, OperandType.I64));
        }

        public readonly void Ret(Operand rn)
        {
            WriteUInt32(0xd65f0000u | (EncodeReg(rn) << 5));
        }

        public readonly void Rev(Operand rd, Operand rn)
        {
            uint opc0 = rd.Type == OperandType.I64 ? 1u << 10 : 0u;
            WriteInstructionAuto(0x5ac00800u | opc0, rd, rn);
        }

        public readonly void Rev16(Operand rd, Operand rn)
        {
            WriteInstructionAuto(0x5ac00400u, rd, rn);
        }

        public readonly void Rev32(Operand rd, Operand rn)
        {
            WriteInstructionAuto(0xdac00800u, rd, rn);
        }

        public readonly void Ror(Operand rd, Operand rn, Operand rm)
        {
            if (rm.Kind == OperandKind.Constant)
            {
                int shift = rm.AsInt32();
                int mask = rd.Type == OperandType.I64 ? 63 : 31;
                shift &= mask;
                Extr(rd, rn, rn, shift);
            }
            else
            {
                Rorv(rd, rn, rm);
            }
        }

        public readonly void Rorv(Operand rd, Operand rn, Operand rm)
        {
            WriteInstructionBitwiseAuto(0x1ac02c00u, rd, rn, rm);
        }

        public readonly void Sbc(Operand rd, Operand rn, Operand rm)
        {
            WriteInstructionRm16(0x5a000000u, rd, rn, rm);
        }

        public readonly void Sbcs(Operand rd, Operand rn, Operand rm)
        {
            WriteInstructionRm16(0x7a000000u, rd, rn, rm);
        }

        public readonly void Sbfm(Operand rd, Operand rn, int immr, int imms)
        {
            uint n = rd.Type == OperandType.I64 ? 1u << 22 : 0u;
            WriteInstructionAuto(0x13000000u | n | (EncodeUImm6(imms) << 10) | (EncodeUImm6(immr) << 16), rd, rn);
        }

        public readonly void Sbfx(Operand rd, Operand rn, int lsb, int width)
        {
            Sbfm(rd, rn, lsb, lsb + width - 1);
        }

        public readonly void Sdiv(Operand rd, Operand rn, Operand rm)
        {
            WriteInstructionRm16Auto(0x1ac00c00u, rd, rn, rm);
        }

        public readonly void Sev()
        {
            WriteUInt32(0xd503209fu);
        }

        public readonly void Sevl()
        {
            WriteUInt32(0xd50320bfu);
        }

        public readonly void Smaddl(Operand rd, Operand rn, Operand rm, Operand ra)
        {
            WriteInstruction(0x9b200000u, rd, rn, rm, ra);
        }

        public readonly void Smsubl(Operand rd, Operand rn, Operand rm, Operand ra)
        {
            WriteInstruction(0x9b208000u, rd, rn, rm, ra);
        }

        public readonly void Smulh(Operand rd, Operand rn, Operand rm)
        {
            WriteInstructionRm16(0x9b407c00u, rd, rn, rm);
        }

        public readonly void Smull(Operand rd, Operand rn, Operand rm)
        {
            Smaddl(rd, rn, rm, new Operand(ZrRegister, RegisterType.Integer, rd.Type));
        }

        public readonly void Stlr(Operand rt, Operand rn)
        {
            WriteInstruction(0x889ffc00u | ((rt.Type == OperandType.I64 ? 3u : 2u) << 30), rt, rn);
        }

        public readonly void Stlrb(Operand rt, Operand rn)
        {
            WriteInstruction(0x089ffc00u, rt, rn);
        }

        public readonly void Stlrh(Operand rt, Operand rn)
        {
            WriteInstruction(0x489ffc00u, rt, rn);
        }

        public readonly void Stlxp(Operand rs, Operand rt, Operand rt2, Operand rn)
        {
            WriteInstruction(0x88208000u | ((rt.Type == OperandType.I64 ? 3u : 2u) << 30), rt, rn, rs, rt2);
        }

        public readonly void Stlxr(Operand rs, Operand rt, Operand rn)
        {
            WriteInstructionRm16(0x8800fc00u | ((rt.Type == OperandType.I64 ? 3u : 2u) << 30), rt, rn, rs);
        }

        public readonly void Stlxrb(Operand rs, Operand rt, Operand rn)
        {
            WriteInstructionRm16(0x0800fc00u, rt, rn, rs);
        }

        public readonly void Stlxrh(Operand rs, Operand rt, Operand rn)
        {
            WriteInstructionRm16(0x4800fc00u, rt, rn, rs);
        }

        public readonly void StpRiPost(Operand rt, Operand rt2, Operand rn, int imm)
        {
            uint instruction = GetLdpStpInstruction(0x28800000u, 0x2c800000u, imm, rt.Type);
            WriteInstruction(instruction, rt, rn, rt2);
        }

        public readonly void StpRiPre(Operand rt, Operand rt2, Operand rn, int imm)
        {
            uint instruction = GetLdpStpInstruction(0x29800000u, 0x2d800000u, imm, rt.Type);
            WriteInstruction(instruction, rt, rn, rt2);
        }

        public readonly void StpRiUn(Operand rt, Operand rt2, Operand rn, int imm)
        {
            uint instruction = GetLdpStpInstruction(0x29000000u, 0x2d000000u, imm, rt.Type);
            WriteInstruction(instruction, rt, rn, rt2);
        }

        public readonly void StrRiPost(Operand rt, Operand rn, int imm)
        {
            uint instruction = GetLdrStrInstruction(0xb8000400u, 0x3c000400u, rt.Type) | (EncodeSImm9(imm) << 12);
            WriteInstruction(instruction, rt, rn);
        }

        public readonly void StrRiPre(Operand rt, Operand rn, int imm)
        {
            uint instruction = GetLdrStrInstruction(0xb8000c00u, 0x3c000c00u, rt.Type) | (EncodeSImm9(imm) << 12);
            WriteInstruction(instruction, rt, rn);
        }

        public readonly void StrRiUn(Operand rt, Operand rn, int imm)
        {
            uint instruction = GetLdrStrInstruction(0xb9000000u, 0x3d000000u, rt.Type) | (EncodeUImm12(imm, rt.Type) << 10);
            WriteInstruction(instruction, rt, rn);
        }

        public readonly void StrRiUn(Operand rt, Operand rn, int imm, uint size)
        {
            uint instruction = GetLdrStrInstruction(0xb9000000u, 0x3d000000u, rt.Type, size) | (EncodeUImm12(imm, (int)size) << 10);
            WriteInstruction(instruction, rt, rn);
        }

        public readonly void StrRr(Operand rt, Operand rn, Operand rm, ArmExtensionType extensionType, bool shift)
        {
            uint instruction = GetLdrStrInstruction(0xb8200800u, 0x3ca00800u, rt.Type);
            WriteInstructionLdrStrAuto(instruction, rt, rn, rm, extensionType, shift);
        }

        public readonly void StrbRiPost(Operand rt, Operand rn, int imm)
        {
            uint instruction = 0x38000400u | (EncodeSImm9(imm) << 12);
            WriteInstruction(instruction, rt, rn);
        }

        public readonly void StrbRiPre(Operand rt, Operand rn, int imm)
        {
            uint instruction = 0x38000c00u | (EncodeSImm9(imm) << 12);
            WriteInstruction(instruction, rt, rn);
        }

        public readonly void StrbRiUn(Operand rt, Operand rn, int imm)
        {
            uint instruction = 0x39000000u | (EncodeUImm12(imm, 0) << 10);
            WriteInstruction(instruction, rt, rn);
        }

        public readonly void StrhRiPost(Operand rt, Operand rn, int imm)
        {
            uint instruction = 0x78000400u | (EncodeSImm9(imm) << 12);
            WriteInstruction(instruction, rt, rn);
        }

        public readonly void StrhRiPre(Operand rt, Operand rn, int imm)
        {
            uint instruction = 0x78000c00u | (EncodeSImm9(imm) << 12);
            WriteInstruction(instruction, rt, rn);
        }

        public readonly void StrhRiUn(Operand rt, Operand rn, int imm)
        {
            uint instruction = 0x79000000u | (EncodeUImm12(imm, 1) << 10);
            WriteInstruction(instruction, rt, rn);
        }

        public readonly void Stur(Operand rt, Operand rn, int imm)
        {
            uint instruction = GetLdrStrInstruction(0xb8000000u, 0x3c000000u, rt.Type) | (EncodeSImm9(imm) << 12);
            WriteInstruction(instruction, rt, rn);
        }

        public readonly void Stur(Operand rt, Operand rn, int imm, uint size)
        {
            uint instruction = GetLdrStrInstruction(0xb8000000u, 0x3c000000u, rt.Type, size) | (EncodeSImm9(imm) << 12);
            WriteInstruction(instruction, rt, rn);
        }

        public readonly void Sturb(Operand rt, Operand rn, int imm)
        {
            uint instruction = 0x38000000u | (EncodeSImm9(imm) << 12);
            WriteInstruction(instruction, rt, rn);
        }

        public readonly void Sturh(Operand rt, Operand rn, int imm)
        {
            uint instruction = 0x78000000u | (EncodeSImm9(imm) << 12);
            WriteInstruction(instruction, rt, rn);
        }

        public readonly void Sub(Operand rd, Operand rn, Operand rm, ArmExtensionType extensionType, int shiftAmount = 0)
        {
            WriteInstructionAuto(0x4b200000u, rd, rn, rm, extensionType, shiftAmount);
        }

        public readonly void Sub(Operand rd, Operand rn, Operand rm)
        {
            Sub(rd, rn, rm, ArmShiftType.Lsl, 0);
        }

        public readonly void Sub(Operand rd, Operand rn, Operand rm, ArmShiftType shiftType, int shiftAmount)
        {
            WriteInstructionAuto(0x51000000u, 0x4b000000u, rd, rn, rm, shiftType, shiftAmount);
        }

        public readonly void Subs(Operand rd, Operand rn, Operand rm)
        {
            Subs(rd, rn, rm, ArmShiftType.Lsl, 0);
        }

        public readonly void Subs(Operand rd, Operand rn, Operand rm, ArmShiftType shiftType, int shiftAmount)
        {
            WriteInstructionAuto(0x71000000u, 0x6b000000u, rd, rn, rm, shiftType, shiftAmount);
        }

        public readonly void Sxtb(Operand rd, Operand rn)
        {
            Sbfm(rd, rn, 0, 7);
        }

        public readonly void Sxth(Operand rd, Operand rn)
        {
            Sbfm(rd, rn, 0, 15);
        }

        public readonly void Sxtw(Operand rd, Operand rn)
        {
            Sbfm(rd, rn, 0, 31);
        }

        public readonly void Tsb()
        {
            WriteUInt32(0xd503225fu);
        }

        public readonly void Tst(Operand rn, Operand rm)
        {
            Tst(rn, rm, ArmShiftType.Lsl, 0);
        }

        public readonly void Tst(Operand rn, Operand rm, ArmShiftType shiftType, int shiftAmount)
        {
            Ands(new Operand(ZrRegister, RegisterType.Integer, rn.Type), rn, rm, shiftType, shiftAmount);
        }

        public readonly void Ubfm(Operand rd, Operand rn, int immr, int imms)
        {
            uint n = rd.Type == OperandType.I64 ? 1u << 22 : 0u;
            WriteInstructionAuto(0x53000000u | n | (EncodeUImm6(imms) << 10) | (EncodeUImm6(immr) << 16), rd, rn);
        }

        public readonly void Ubfx(Operand rd, Operand rn, int lsb, int width)
        {
            Ubfm(rd, rn, lsb, lsb + width - 1);
        }

        public readonly void Udiv(Operand rd, Operand rn, Operand rm)
        {
            WriteInstructionRm16Auto(0x1ac00800u, rd, rn, rm);
        }

        public readonly void Umaddl(Operand rd, Operand rn, Operand rm, Operand ra)
        {
            WriteInstructionAuto(0x9ba00000u, rd, rn, rm, ra);
        }

        public readonly void Umulh(Operand rd, Operand rn, Operand rm)
        {
            WriteInstructionRm16(0x9bc07c00u, rd, rn, rm);
        }

        public readonly void Umull(Operand rd, Operand rn, Operand rm)
        {
            Umaddl(rd, rn, rm, new Operand(ZrRegister, RegisterType.Integer, rd.Type));
        }

        public readonly void Uxtb(Operand rd, Operand rn)
        {
            Ubfm(rd, rn, 0, 7);
        }

        public readonly void Uxth(Operand rd, Operand rn)
        {
            Ubfm(rd, rn, 0, 15);
        }

        // FP & SIMD

        public readonly void AbsS(Operand rd, Operand rn, uint size)
        {
            WriteInstruction(0x5e20b800u | (size << 22), rd, rn);
        }

        public readonly void AbsV(Operand rd, Operand rn, uint size, uint q)
        {
            WriteInstruction(0x0e20b800u | (size << 22) | (q << 30), rd, rn);
        }

        public readonly void Addhn(Operand rd, Operand rn, Operand rm, uint size, uint q)
        {
            WriteInstructionRm16(0x0e204000u | (size << 22) | (q << 30), rd, rn, rm);
        }

        public readonly void AddpPair(Operand rd, Operand rn, uint size)
        {
            WriteInstruction(0x5e31b800u | (size << 22), rd, rn);
        }

        public readonly void AddpVec(Operand rd, Operand rn, Operand rm, uint size, uint q)
        {
            WriteInstructionRm16(0x0e20bc00u | (size << 22) | (q << 30), rd, rn, rm);
        }

        public readonly void Addv(Operand rd, Operand rn, uint size, uint q)
        {
            WriteInstruction(0x0e31b800u | (size << 22) | (q << 30), rd, rn);
        }

        public readonly void AddS(Operand rd, Operand rn, Operand rm, uint size)
        {
            WriteInstructionRm16(0x5e208400u | (size << 22), rd, rn, rm);
        }

        public readonly void AddV(Operand rd, Operand rn, Operand rm, uint size, uint q)
        {
            WriteInstructionRm16(0x0e208400u | (size << 22) | (q << 30), rd, rn, rm);
        }

        public readonly void Aesd(Operand rd, Operand rn)
        {
            WriteInstruction(0x4e285800u, rd, rn);
        }

        public readonly void Aese(Operand rd, Operand rn)
        {
            WriteInstruction(0x4e284800u, rd, rn);
        }

        public readonly void Aesimc(Operand rd, Operand rn)
        {
            WriteInstruction(0x4e287800u, rd, rn);
        }

        public readonly void Aesmc(Operand rd, Operand rn)
        {
            WriteInstruction(0x4e286800u, rd, rn);
        }

        public readonly void And(Operand rd, Operand rn, Operand rm, uint q)
        {
            WriteInstructionRm16(0x0e201c00u | (q << 30), rd, rn, rm);
        }

        public readonly void Bcax(Operand rd, Operand rn, Operand rm, Operand ra)
        {
            WriteInstruction(0xce200000u, rd, rn, rm, ra);
        }

        public readonly void Bfcvtn(Operand rd, Operand rn, uint q)
        {
            WriteInstruction(0x0ea16800u | (q << 30), rd, rn);
        }

        public readonly void BfcvtFloat(Operand rd, Operand rn)
        {
            WriteInstruction(0x1e634000u, rd, rn);
        }

        public readonly void BfdotElt(Operand rd, Operand rn, uint h, Operand rm, uint m, uint l, uint q)
        {
            WriteInstructionRm16(0x0f40f000u | (h << 11) | (m << 20) | (l << 21) | (q << 30), rd, rn, rm);
        }

        public readonly void BfdotVec(Operand rd, Operand rn, Operand rm, uint q)
        {
            WriteInstructionRm16(0x2e40fc00u | (q << 30), rd, rn, rm);
        }

        public readonly void BfmlalElt(Operand rd, Operand rn, uint h, Operand rm, uint m, uint l, uint q)
        {
            WriteInstructionRm16(0x0fc0f000u | (h << 11) | (m << 20) | (l << 21) | (q << 30), rd, rn, rm);
        }

        public readonly void BfmlalVec(Operand rd, Operand rn, Operand rm, uint q)
        {
            WriteInstructionRm16(0x2ec0fc00u | (q << 30), rd, rn, rm);
        }

        public readonly void Bfmmla(Operand rd, Operand rn, Operand rm)
        {
            WriteInstructionRm16(0x6e40ec00u, rd, rn, rm);
        }

        public readonly void BicImm(Operand rd, uint h, uint g, uint f, uint e, uint d, uint c, uint b, uint a, uint q)
        {
            WriteInstruction(0x2f001400u | (h << 5) | (g << 6) | (f << 7) | (e << 8) | (d << 9) | (c << 16) | (b << 17) | (a << 18) | (q << 30), rd);
        }

        public readonly void BicReg(Operand rd, Operand rn, Operand rm, uint q)
        {
            WriteInstructionRm16(0x0e601c00u | (q << 30), rd, rn, rm);
        }

        public readonly void Bif(Operand rd, Operand rn, Operand rm, uint q)
        {
            WriteInstructionRm16(0x2ee01c00u | (q << 30), rd, rn, rm);
        }

        public readonly void Bit(Operand rd, Operand rn, Operand rm, uint q)
        {
            WriteInstructionRm16(0x2ea01c00u | (q << 30), rd, rn, rm);
        }

        public readonly void Bsl(Operand rd, Operand rn, Operand rm, uint q)
        {
            WriteInstructionRm16(0x2e601c00u | (q << 30), rd, rn, rm);
        }

        public readonly void Cls(Operand rd, Operand rn, uint size, uint q)
        {
            WriteInstruction(0x0e204800u | (size << 22) | (q << 30), rd, rn);
        }

        public readonly void Clz(Operand rd, Operand rn, uint size, uint q)
        {
            WriteInstruction(0x2e204800u | (size << 22) | (q << 30), rd, rn);
        }

        public readonly void CmeqRegS(Operand rd, Operand rn, Operand rm, uint size)
        {
            WriteInstructionRm16(0x7e208c00u | (size << 22), rd, rn, rm);
        }

        public readonly void CmeqRegV(Operand rd, Operand rn, Operand rm, uint size, uint q)
        {
            WriteInstructionRm16(0x2e208c00u | (size << 22) | (q << 30), rd, rn, rm);
        }

        public readonly void CmeqZeroS(Operand rd, Operand rn, uint size)
        {
            WriteInstruction(0x5e209800u | (size << 22), rd, rn);
        }

        public readonly void CmeqZeroV(Operand rd, Operand rn, uint size, uint q)
        {
            WriteInstruction(0x0e209800u | (size << 22) | (q << 30), rd, rn);
        }

        public readonly void CmgeRegS(Operand rd, Operand rn, Operand rm, uint size)
        {
            WriteInstructionRm16(0x5e203c00u | (size << 22), rd, rn, rm);
        }

        public readonly void CmgeRegV(Operand rd, Operand rn, Operand rm, uint size, uint q)
        {
            WriteInstructionRm16(0x0e203c00u | (size << 22) | (q << 30), rd, rn, rm);
        }

        public readonly void CmgeZeroS(Operand rd, Operand rn, uint size)
        {
            WriteInstruction(0x7e208800u | (size << 22), rd, rn);
        }

        public readonly void CmgeZeroV(Operand rd, Operand rn, uint size, uint q)
        {
            WriteInstruction(0x2e208800u | (size << 22) | (q << 30), rd, rn);
        }

        public readonly void CmgtRegS(Operand rd, Operand rn, Operand rm, uint size)
        {
            WriteInstructionRm16(0x5e203400u | (size << 22), rd, rn, rm);
        }

        public readonly void CmgtRegV(Operand rd, Operand rn, Operand rm, uint size, uint q)
        {
            WriteInstructionRm16(0x0e203400u | (size << 22) | (q << 30), rd, rn, rm);
        }

        public readonly void CmgtZeroS(Operand rd, Operand rn, uint size)
        {
            WriteInstruction(0x5e208800u | (size << 22), rd, rn);
        }

        public readonly void CmgtZeroV(Operand rd, Operand rn, uint size, uint q)
        {
            WriteInstruction(0x0e208800u | (size << 22) | (q << 30), rd, rn);
        }

        public readonly void CmhiS(Operand rd, Operand rn, Operand rm, uint size)
        {
            WriteInstructionRm16(0x7e203400u | (size << 22), rd, rn, rm);
        }

        public readonly void CmhiV(Operand rd, Operand rn, Operand rm, uint size, uint q)
        {
            WriteInstructionRm16(0x2e203400u | (size << 22) | (q << 30), rd, rn, rm);
        }

        public readonly void CmhsS(Operand rd, Operand rn, Operand rm, uint size)
        {
            WriteInstructionRm16(0x7e203c00u | (size << 22), rd, rn, rm);
        }

        public readonly void CmhsV(Operand rd, Operand rn, Operand rm, uint size, uint q)
        {
            WriteInstructionRm16(0x2e203c00u | (size << 22) | (q << 30), rd, rn, rm);
        }

        public readonly void CmleS(Operand rd, Operand rn, uint size)
        {
            WriteInstruction(0x7e209800u | (size << 22), rd, rn);
        }

        public readonly void CmleV(Operand rd, Operand rn, uint size, uint q)
        {
            WriteInstruction(0x2e209800u | (size << 22) | (q << 30), rd, rn);
        }

        public readonly void CmltS(Operand rd, Operand rn, uint size)
        {
            WriteInstruction(0x5e20a800u | (size << 22), rd, rn);
        }

        public readonly void CmltV(Operand rd, Operand rn, uint size, uint q)
        {
            WriteInstruction(0x0e20a800u | (size << 22) | (q << 30), rd, rn);
        }

        public readonly void CmtstS(Operand rd, Operand rn, Operand rm, uint size)
        {
            WriteInstructionRm16(0x5e208c00u | (size << 22), rd, rn, rm);
        }

        public readonly void CmtstV(Operand rd, Operand rn, Operand rm, uint size, uint q)
        {
            WriteInstructionRm16(0x0e208c00u | (size << 22) | (q << 30), rd, rn, rm);
        }

        public readonly void Cnt(Operand rd, Operand rn, uint size, uint q)
        {
            WriteInstruction(0x0e205800u | (size << 22) | (q << 30), rd, rn);
        }

        public readonly void DupEltScalarFromElement(Operand rd, Operand rn, uint imm5)
        {
            WriteInstruction(0x5e000400u | (imm5 << 16), rd, rn);
        }

        public readonly void DupEltVectorFromElement(Operand rd, Operand rn, uint imm5, uint q)
        {
            WriteInstruction(0x0e000400u | (imm5 << 16) | (q << 30), rd, rn);
        }

        public readonly void DupGen(Operand rd, Operand rn, uint imm5, uint q)
        {
            WriteInstruction(0x0e000c00u | (imm5 << 16) | (q << 30), rd, rn);
        }

        public readonly void Eor3(Operand rd, Operand rn, Operand rm, Operand ra)
        {
            WriteInstruction(0xce000000u, rd, rn, rm, ra);
        }

        public readonly void Eor(Operand rd, Operand rn, Operand rm, uint q)
        {
            WriteInstructionRm16(0x2e201c00u | (q << 30), rd, rn, rm);
        }

        public readonly void Ext(Operand rd, Operand rn, uint imm4, Operand rm, uint q)
        {
            WriteInstructionRm16(0x2e000000u | (imm4 << 11) | (q << 30), rd, rn, rm);
        }

        public readonly void FabdSH(Operand rd, Operand rn, Operand rm)
        {
            WriteInstructionRm16(0x7ec01400u, rd, rn, rm);
        }

        public readonly void FabdS(Operand rd, Operand rn, Operand rm, uint sz)
        {
            WriteInstructionRm16(0x7ea0d400u | (sz << 22), rd, rn, rm);
        }

        public readonly void FabdVH(Operand rd, Operand rn, Operand rm, uint q)
        {
            WriteInstructionRm16(0x2ec01400u | (q << 30), rd, rn, rm);
        }

        public readonly void FabdV(Operand rd, Operand rn, Operand rm, uint sz, uint q)
        {
            WriteInstructionRm16(0x2ea0d400u | (sz << 22) | (q << 30), rd, rn, rm);
        }

        public readonly void FabsHalf(Operand rd, Operand rn, uint q)
        {
            WriteInstruction(0x0ef8f800u | (q << 30), rd, rn);
        }

        public readonly void FabsSingleAndDouble(Operand rd, Operand rn, uint sz, uint q)
        {
            WriteInstruction(0x0ea0f800u | (sz << 22) | (q << 30), rd, rn);
        }

        public readonly void FabsFloat(Operand rd, Operand rn, uint ftype)
        {
            WriteInstruction(0x1e20c000u | (ftype << 22), rd, rn);
        }

        public readonly void FacgeSH(Operand rd, Operand rn, Operand rm)
        {
            WriteInstructionRm16(0x7e402c00u, rd, rn, rm);
        }

        public readonly void FacgeS(Operand rd, Operand rn, Operand rm, uint sz)
        {
            WriteInstructionRm16(0x7e20ec00u | (sz << 22), rd, rn, rm);
        }

        public readonly void FacgeVH(Operand rd, Operand rn, Operand rm, uint q)
        {
            WriteInstructionRm16(0x2e402c00u | (q << 30), rd, rn, rm);
        }

        public readonly void FacgeV(Operand rd, Operand rn, Operand rm, uint sz, uint q)
        {
            WriteInstructionRm16(0x2e20ec00u | (sz << 22) | (q << 30), rd, rn, rm);
        }

        public readonly void FacgtSH(Operand rd, Operand rn, Operand rm)
        {
            WriteInstructionRm16(0x7ec02c00u, rd, rn, rm);
        }

        public readonly void FacgtS(Operand rd, Operand rn, Operand rm, uint sz)
        {
            WriteInstructionRm16(0x7ea0ec00u | (sz << 22), rd, rn, rm);
        }

        public readonly void FacgtVH(Operand rd, Operand rn, Operand rm, uint q)
        {
            WriteInstructionRm16(0x2ec02c00u | (q << 30), rd, rn, rm);
        }

        public readonly void FacgtV(Operand rd, Operand rn, Operand rm, uint sz, uint q)
        {
            WriteInstructionRm16(0x2ea0ec00u | (sz << 22) | (q << 30), rd, rn, rm);
        }

        public readonly void FaddpPairHalf(Operand rd, Operand rn, uint sz)
        {
            WriteInstruction(0x5e30d800u | (sz << 22), rd, rn);
        }

        public readonly void FaddpPairSingleAndDouble(Operand rd, Operand rn, uint sz)
        {
            WriteInstruction(0x7e30d800u | (sz << 22), rd, rn);
        }

        public readonly void FaddpVecHalf(Operand rd, Operand rn, Operand rm, uint q)
        {
            WriteInstructionRm16(0x2e401400u | (q << 30), rd, rn, rm);
        }

        public readonly void FaddpVecSingleAndDouble(Operand rd, Operand rn, Operand rm, uint sz, uint q)
        {
            WriteInstructionRm16(0x2e20d400u | (sz << 22) | (q << 30), rd, rn, rm);
        }

        public readonly void FaddHalf(Operand rd, Operand rn, Operand rm, uint q)
        {
            WriteInstructionRm16(0x0e401400u | (q << 30), rd, rn, rm);
        }

        public readonly void FaddSingleAndDouble(Operand rd, Operand rn, Operand rm, uint sz, uint q)
        {
            WriteInstructionRm16(0x0e20d400u | (sz << 22) | (q << 30), rd, rn, rm);
        }

        public readonly void FaddFloat(Operand rd, Operand rn, Operand rm, uint ftype)
        {
            WriteInstructionRm16(0x1e202800u | (ftype << 22), rd, rn, rm);
        }

        public readonly void FcaddVec(Operand rd, Operand rn, uint rot, Operand rm, uint size, uint q)
        {
            WriteInstructionRm16(0x2e00e400u | (rot << 12) | (size << 22) | (q << 30), rd, rn, rm);
        }

        public readonly void FccmpeFloat(uint nzcv, Operand rn, uint cond, Operand rm, uint ftype)
        {
            WriteInstructionRm16(0x1e200410u | (nzcv << 0) | (cond << 12) | (ftype << 22), rn, rm);
        }

        public readonly void FccmpFloat(uint nzcv, Operand rn, uint cond, Operand rm, uint ftype)
        {
            WriteInstructionRm16(0x1e200400u | (nzcv << 0) | (cond << 12) | (ftype << 22), rn, rm);
        }

        public readonly void FcmeqRegSH(Operand rd, Operand rn, Operand rm)
        {
            WriteInstructionRm16(0x5e402400u, rd, rn, rm);
        }

        public readonly void FcmeqRegS(Operand rd, Operand rn, Operand rm, uint sz)
        {
            WriteInstructionRm16(0x5e20e400u | (sz << 22), rd, rn, rm);
        }

        public readonly void FcmeqRegVH(Operand rd, Operand rn, Operand rm, uint q)
        {
            WriteInstructionRm16(0x0e402400u | (q << 30), rd, rn, rm);
        }

        public readonly void FcmeqRegV(Operand rd, Operand rn, Operand rm, uint sz, uint q)
        {
            WriteInstructionRm16(0x0e20e400u | (sz << 22) | (q << 30), rd, rn, rm);
        }

        public readonly void FcmeqZeroSH(Operand rd, Operand rn)
        {
            WriteInstruction(0x5ef8d800u, rd, rn);
        }

        public readonly void FcmeqZeroS(Operand rd, Operand rn, uint sz)
        {
            WriteInstruction(0x5ea0d800u | (sz << 22), rd, rn);
        }

        public readonly void FcmeqZeroVH(Operand rd, Operand rn, uint q)
        {
            WriteInstruction(0x0ef8d800u | (q << 30), rd, rn);
        }

        public readonly void FcmeqZeroV(Operand rd, Operand rn, uint sz, uint q)
        {
            WriteInstruction(0x0ea0d800u | (sz << 22) | (q << 30), rd, rn);
        }

        public readonly void FcmgeRegSH(Operand rd, Operand rn, Operand rm)
        {
            WriteInstructionRm16(0x7e402400u, rd, rn, rm);
        }

        public readonly void FcmgeRegS(Operand rd, Operand rn, Operand rm, uint sz)
        {
            WriteInstructionRm16(0x7e20e400u | (sz << 22), rd, rn, rm);
        }

        public readonly void FcmgeRegVH(Operand rd, Operand rn, Operand rm, uint q)
        {
            WriteInstructionRm16(0x2e402400u | (q << 30), rd, rn, rm);
        }

        public readonly void FcmgeRegV(Operand rd, Operand rn, Operand rm, uint sz, uint q)
        {
            WriteInstructionRm16(0x2e20e400u | (sz << 22) | (q << 30), rd, rn, rm);
        }

        public readonly void FcmgeZeroSH(Operand rd, Operand rn)
        {
            WriteInstruction(0x7ef8c800u, rd, rn);
        }

        public readonly void FcmgeZeroS(Operand rd, Operand rn, uint sz)
        {
            WriteInstruction(0x7ea0c800u | (sz << 22), rd, rn);
        }

        public readonly void FcmgeZeroVH(Operand rd, Operand rn, uint q)
        {
            WriteInstruction(0x2ef8c800u | (q << 30), rd, rn);
        }

        public readonly void FcmgeZeroV(Operand rd, Operand rn, uint sz, uint q)
        {
            WriteInstruction(0x2ea0c800u | (sz << 22) | (q << 30), rd, rn);
        }

        public readonly void FcmgtRegSH(Operand rd, Operand rn, Operand rm)
        {
            WriteInstructionRm16(0x7ec02400u, rd, rn, rm);
        }

        public readonly void FcmgtRegS(Operand rd, Operand rn, Operand rm, uint sz)
        {
            WriteInstructionRm16(0x7ea0e400u | (sz << 22), rd, rn, rm);
        }

        public readonly void FcmgtRegVH(Operand rd, Operand rn, Operand rm, uint q)
        {
            WriteInstructionRm16(0x2ec02400u | (q << 30), rd, rn, rm);
        }

        public readonly void FcmgtRegV(Operand rd, Operand rn, Operand rm, uint sz, uint q)
        {
            WriteInstructionRm16(0x2ea0e400u | (sz << 22) | (q << 30), rd, rn, rm);
        }

        public readonly void FcmgtZeroSH(Operand rd, Operand rn)
        {
            WriteInstruction(0x5ef8c800u, rd, rn);
        }

        public readonly void FcmgtZeroS(Operand rd, Operand rn, uint sz)
        {
            WriteInstruction(0x5ea0c800u | (sz << 22), rd, rn);
        }

        public readonly void FcmgtZeroVH(Operand rd, Operand rn, uint q)
        {
            WriteInstruction(0x0ef8c800u | (q << 30), rd, rn);
        }

        public readonly void FcmgtZeroV(Operand rd, Operand rn, uint sz, uint q)
        {
            WriteInstruction(0x0ea0c800u | (sz << 22) | (q << 30), rd, rn);
        }

        public readonly void FcmlaElt(Operand rd, Operand rn, uint h, uint rot, Operand rm, uint m, uint l, uint size, uint q)
        {
            WriteInstructionRm16(0x2f001000u | (h << 11) | (rot << 13) | (m << 20) | (l << 21) | (size << 22) | (q << 30), rd, rn, rm);
        }

        public readonly void FcmlaVec(Operand rd, Operand rn, uint rot, Operand rm, uint size, uint q)
        {
            WriteInstructionRm16(0x2e00c400u | (rot << 11) | (size << 22) | (q << 30), rd, rn, rm);
        }

        public readonly void FcmleSH(Operand rd, Operand rn)
        {
            WriteInstruction(0x7ef8d800u, rd, rn);
        }

        public readonly void FcmleS(Operand rd, Operand rn, uint sz)
        {
            WriteInstruction(0x7ea0d800u | (sz << 22), rd, rn);
        }

        public readonly void FcmleVH(Operand rd, Operand rn, uint q)
        {
            WriteInstruction(0x2ef8d800u | (q << 30), rd, rn);
        }

        public readonly void FcmleV(Operand rd, Operand rn, uint sz, uint q)
        {
            WriteInstruction(0x2ea0d800u | (sz << 22) | (q << 30), rd, rn);
        }

        public readonly void FcmltSH(Operand rd, Operand rn)
        {
            WriteInstruction(0x5ef8e800u, rd, rn);
        }

        public readonly void FcmltS(Operand rd, Operand rn, uint sz)
        {
            WriteInstruction(0x5ea0e800u | (sz << 22), rd, rn);
        }

        public readonly void FcmltVH(Operand rd, Operand rn, uint q)
        {
            WriteInstruction(0x0ef8e800u | (q << 30), rd, rn);
        }

        public readonly void FcmltV(Operand rd, Operand rn, uint sz, uint q)
        {
            WriteInstruction(0x0ea0e800u | (sz << 22) | (q << 30), rd, rn);
        }

        public readonly void FcmpeFloat(Operand rn, Operand rm, uint opc, uint ftype)
        {
            WriteInstructionRm16(0x1e202010u | (opc << 3) | (ftype << 22), rn, rm);
        }

        public readonly void FcmpFloat(Operand rn, Operand rm, uint opc, uint ftype)
        {
            WriteInstructionRm16(0x1e202000u | (opc << 3) | (ftype << 22), rn, rm);
        }

        public readonly void FcselFloat(Operand rd, Operand rn, uint cond, Operand rm, uint ftype)
        {
            WriteInstructionRm16(0x1e200c00u | (cond << 12) | (ftype << 22), rd, rn, rm);
        }

        public readonly void FcvtasSH(Operand rd, Operand rn)
        {
            WriteInstruction(0x5e79c800u, rd, rn);
        }

        public readonly void FcvtasS(Operand rd, Operand rn, uint sz)
        {
            WriteInstruction(0x5e21c800u | (sz << 22), rd, rn);
        }

        public readonly void FcvtasVH(Operand rd, Operand rn, uint q)
        {
            WriteInstruction(0x0e79c800u | (q << 30), rd, rn);
        }

        public readonly void FcvtasV(Operand rd, Operand rn, uint sz, uint q)
        {
            WriteInstruction(0x0e21c800u | (sz << 22) | (q << 30), rd, rn);
        }

        public readonly void FcvtasFloat(Operand rd, Operand rn, uint ftype, uint sf)
        {
            WriteInstruction(0x1e240000u | (ftype << 22) | (sf << 31), rd, rn);
        }

        public readonly void FcvtauSH(Operand rd, Operand rn)
        {
            WriteInstruction(0x7e79c800u, rd, rn);
        }

        public readonly void FcvtauS(Operand rd, Operand rn, uint sz)
        {
            WriteInstruction(0x7e21c800u | (sz << 22), rd, rn);
        }

        public readonly void FcvtauVH(Operand rd, Operand rn, uint q)
        {
            WriteInstruction(0x2e79c800u | (q << 30), rd, rn);
        }

        public readonly void FcvtauV(Operand rd, Operand rn, uint sz, uint q)
        {
            WriteInstruction(0x2e21c800u | (sz << 22) | (q << 30), rd, rn);
        }

        public readonly void FcvtauFloat(Operand rd, Operand rn, uint ftype, uint sf)
        {
            WriteInstruction(0x1e250000u | (ftype << 22) | (sf << 31), rd, rn);
        }

        public readonly void Fcvtl(Operand rd, Operand rn, uint sz, uint q)
        {
            WriteInstruction(0x0e217800u | (sz << 22) | (q << 30), rd, rn);
        }

        public readonly void FcvtmsSH(Operand rd, Operand rn)
        {
            WriteInstruction(0x5e79b800u, rd, rn);
        }

        public readonly void FcvtmsS(Operand rd, Operand rn, uint sz)
        {
            WriteInstruction(0x5e21b800u | (sz << 22), rd, rn);
        }

        public readonly void FcvtmsVH(Operand rd, Operand rn, uint q)
        {
            WriteInstruction(0x0e79b800u | (q << 30), rd, rn);
        }

        public readonly void FcvtmsV(Operand rd, Operand rn, uint sz, uint q)
        {
            WriteInstruction(0x0e21b800u | (sz << 22) | (q << 30), rd, rn);
        }

        public readonly void FcvtmsFloat(Operand rd, Operand rn, uint ftype, uint sf)
        {
            WriteInstruction(0x1e300000u | (ftype << 22) | (sf << 31), rd, rn);
        }

        public readonly void FcvtmuSH(Operand rd, Operand rn)
        {
            WriteInstruction(0x7e79b800u, rd, rn);
        }

        public readonly void FcvtmuS(Operand rd, Operand rn, uint sz)
        {
            WriteInstruction(0x7e21b800u | (sz << 22), rd, rn);
        }

        public readonly void FcvtmuVH(Operand rd, Operand rn, uint q)
        {
            WriteInstruction(0x2e79b800u | (q << 30), rd, rn);
        }

        public readonly void FcvtmuV(Operand rd, Operand rn, uint sz, uint q)
        {
            WriteInstruction(0x2e21b800u | (sz << 22) | (q << 30), rd, rn);
        }

        public readonly void FcvtmuFloat(Operand rd, Operand rn, uint ftype, uint sf)
        {
            WriteInstruction(0x1e310000u | (ftype << 22) | (sf << 31), rd, rn);
        }

        public readonly void FcvtnsSH(Operand rd, Operand rn)
        {
            WriteInstruction(0x5e79a800u, rd, rn);
        }

        public readonly void FcvtnsS(Operand rd, Operand rn, uint sz)
        {
            WriteInstruction(0x5e21a800u | (sz << 22), rd, rn);
        }

        public readonly void FcvtnsVH(Operand rd, Operand rn, uint q)
        {
            WriteInstruction(0x0e79a800u | (q << 30), rd, rn);
        }

        public readonly void FcvtnsV(Operand rd, Operand rn, uint sz, uint q)
        {
            WriteInstruction(0x0e21a800u | (sz << 22) | (q << 30), rd, rn);
        }

        public readonly void FcvtnsFloat(Operand rd, Operand rn, uint ftype, uint sf)
        {
            WriteInstruction(0x1e200000u | (ftype << 22) | (sf << 31), rd, rn);
        }

        public readonly void FcvtnuSH(Operand rd, Operand rn)
        {
            WriteInstruction(0x7e79a800u, rd, rn);
        }

        public readonly void FcvtnuS(Operand rd, Operand rn, uint sz)
        {
            WriteInstruction(0x7e21a800u | (sz << 22), rd, rn);
        }

        public readonly void FcvtnuVH(Operand rd, Operand rn, uint q)
        {
            WriteInstruction(0x2e79a800u | (q << 30), rd, rn);
        }

        public readonly void FcvtnuV(Operand rd, Operand rn, uint sz, uint q)
        {
            WriteInstruction(0x2e21a800u | (sz << 22) | (q << 30), rd, rn);
        }

        public readonly void FcvtnuFloat(Operand rd, Operand rn, uint ftype, uint sf)
        {
            WriteInstruction(0x1e210000u | (ftype << 22) | (sf << 31), rd, rn);
        }

        public readonly void Fcvtn(Operand rd, Operand rn, uint sz, uint q)
        {
            WriteInstruction(0x0e216800u | (sz << 22) | (q << 30), rd, rn);
        }

        public readonly void FcvtpsSH(Operand rd, Operand rn)
        {
            WriteInstruction(0x5ef9a800u, rd, rn);
        }

        public readonly void FcvtpsS(Operand rd, Operand rn, uint sz)
        {
            WriteInstruction(0x5ea1a800u | (sz << 22), rd, rn);
        }

        public readonly void FcvtpsVH(Operand rd, Operand rn, uint q)
        {
            WriteInstruction(0x0ef9a800u | (q << 30), rd, rn);
        }

        public readonly void FcvtpsV(Operand rd, Operand rn, uint sz, uint q)
        {
            WriteInstruction(0x0ea1a800u | (sz << 22) | (q << 30), rd, rn);
        }

        public readonly void FcvtpsFloat(Operand rd, Operand rn, uint ftype, uint sf)
        {
            WriteInstruction(0x1e280000u | (ftype << 22) | (sf << 31), rd, rn);
        }

        public readonly void FcvtpuSH(Operand rd, Operand rn)
        {
            WriteInstruction(0x7ef9a800u, rd, rn);
        }

        public readonly void FcvtpuS(Operand rd, Operand rn, uint sz)
        {
            WriteInstruction(0x7ea1a800u | (sz << 22), rd, rn);
        }

        public readonly void FcvtpuVH(Operand rd, Operand rn, uint q)
        {
            WriteInstruction(0x2ef9a800u | (q << 30), rd, rn);
        }

        public readonly void FcvtpuV(Operand rd, Operand rn, uint sz, uint q)
        {
            WriteInstruction(0x2ea1a800u | (sz << 22) | (q << 30), rd, rn);
        }

        public readonly void FcvtpuFloat(Operand rd, Operand rn, uint ftype, uint sf)
        {
            WriteInstruction(0x1e290000u | (ftype << 22) | (sf << 31), rd, rn);
        }

        public readonly void FcvtxnS(Operand rd, Operand rn, uint sz)
        {
            WriteInstruction(0x7e216800u | (sz << 22), rd, rn);
        }

        public readonly void FcvtxnV(Operand rd, Operand rn, uint sz, uint q)
        {
            WriteInstruction(0x2e216800u | (sz << 22) | (q << 30), rd, rn);
        }

        public readonly void FcvtzsFixS(Operand rd, Operand rn, uint immb, uint immh)
        {
            WriteInstruction(0x5f00fc00u | (immb << 16) | (immh << 19), rd, rn);
        }

        public readonly void FcvtzsFixV(Operand rd, Operand rn, uint immb, uint immh, uint q)
        {
            WriteInstruction(0x0f00fc00u | (immb << 16) | (immh << 19) | (q << 30), rd, rn);
        }

        public readonly void FcvtzsIntSH(Operand rd, Operand rn)
        {
            WriteInstruction(0x5ef9b800u, rd, rn);
        }

        public readonly void FcvtzsIntS(Operand rd, Operand rn, uint sz)
        {
            WriteInstruction(0x5ea1b800u | (sz << 22), rd, rn);
        }

        public readonly void FcvtzsIntVH(Operand rd, Operand rn, uint q)
        {
            WriteInstruction(0x0ef9b800u | (q << 30), rd, rn);
        }

        public readonly void FcvtzsIntV(Operand rd, Operand rn, uint sz, uint q)
        {
            WriteInstruction(0x0ea1b800u | (sz << 22) | (q << 30), rd, rn);
        }

        public readonly void FcvtzsFloatFix(Operand rd, Operand rn, uint scale, uint ftype, uint sf)
        {
            WriteInstruction(0x1e180000u | (scale << 10) | (ftype << 22) | (sf << 31), rd, rn);
        }

        public readonly void FcvtzsFloatInt(Operand rd, Operand rn, uint ftype, uint sf)
        {
            WriteInstruction(0x1e380000u | (ftype << 22) | (sf << 31), rd, rn);
        }

        public readonly void FcvtzuFixS(Operand rd, Operand rn, uint immb, uint immh)
        {
            WriteInstruction(0x7f00fc00u | (immb << 16) | (immh << 19), rd, rn);
        }

        public readonly void FcvtzuFixV(Operand rd, Operand rn, uint immb, uint immh, uint q)
        {
            WriteInstruction(0x2f00fc00u | (immb << 16) | (immh << 19) | (q << 30), rd, rn);
        }

        public readonly void FcvtzuIntSH(Operand rd, Operand rn)
        {
            WriteInstruction(0x7ef9b800u, rd, rn);
        }

        public readonly void FcvtzuIntS(Operand rd, Operand rn, uint sz)
        {
            WriteInstruction(0x7ea1b800u | (sz << 22), rd, rn);
        }

        public readonly void FcvtzuIntVH(Operand rd, Operand rn, uint q)
        {
            WriteInstruction(0x2ef9b800u | (q << 30), rd, rn);
        }

        public readonly void FcvtzuIntV(Operand rd, Operand rn, uint sz, uint q)
        {
            WriteInstruction(0x2ea1b800u | (sz << 22) | (q << 30), rd, rn);
        }

        public readonly void FcvtzuFloatFix(Operand rd, Operand rn, uint scale, uint ftype, uint sf)
        {
            WriteInstruction(0x1e190000u | (scale << 10) | (ftype << 22) | (sf << 31), rd, rn);
        }

        public readonly void FcvtzuFloatInt(Operand rd, Operand rn, uint ftype, uint sf)
        {
            WriteInstruction(0x1e390000u | (ftype << 22) | (sf << 31), rd, rn);
        }

        public readonly void FcvtFloat(Operand rd, Operand rn, uint opc, uint ftype)
        {
            WriteInstruction(0x1e224000u | (opc << 15) | (ftype << 22), rd, rn);
        }

        public readonly void FdivHalf(Operand rd, Operand rn, Operand rm, uint q)
        {
            WriteInstructionRm16(0x2e403c00u | (q << 30), rd, rn, rm);
        }

        public readonly void FdivSingleAndDouble(Operand rd, Operand rn, Operand rm, uint sz, uint q)
        {
            WriteInstructionRm16(0x2e20fc00u | (sz << 22) | (q << 30), rd, rn, rm);
        }

        public readonly void FdivFloat(Operand rd, Operand rn, Operand rm, uint ftype)
        {
            WriteInstructionRm16(0x1e201800u | (ftype << 22), rd, rn, rm);
        }

        public readonly void Fjcvtzs(Operand rd, Operand rn)
        {
            WriteInstruction(0x1e7e0000u, rd, rn);
        }

        public readonly void FmaddFloat(Operand rd, Operand rn, Operand rm, Operand ra, uint ftype)
        {
            WriteInstruction(0x1f000000u | (ftype << 22), rd, rn, rm, ra);
        }

        public readonly void FmaxnmpPairHalf(Operand rd, Operand rn, uint sz)
        {
            WriteInstruction(0x5e30c800u | (sz << 22), rd, rn);
        }

        public readonly void FmaxnmpPairSingleAndDouble(Operand rd, Operand rn, uint sz)
        {
            WriteInstruction(0x7e30c800u | (sz << 22), rd, rn);
        }

        public readonly void FmaxnmpVecHalf(Operand rd, Operand rn, Operand rm, uint q)
        {
            WriteInstructionRm16(0x2e400400u | (q << 30), rd, rn, rm);
        }

        public readonly void FmaxnmpVecSingleAndDouble(Operand rd, Operand rn, Operand rm, uint sz, uint q)
        {
            WriteInstructionRm16(0x2e20c400u | (sz << 22) | (q << 30), rd, rn, rm);
        }

        public readonly void FmaxnmvHalf(Operand rd, Operand rn, uint q)
        {
            WriteInstruction(0x0e30c800u | (q << 30), rd, rn);
        }

        public readonly void FmaxnmvSingleAndDouble(Operand rd, Operand rn, uint sz, uint q)
        {
            WriteInstruction(0x2e30c800u | (sz << 22) | (q << 30), rd, rn);
        }

        public readonly void FmaxnmHalf(Operand rd, Operand rn, Operand rm, uint q)
        {
            WriteInstructionRm16(0x0e400400u | (q << 30), rd, rn, rm);
        }

        public readonly void FmaxnmSingleAndDouble(Operand rd, Operand rn, Operand rm, uint sz, uint q)
        {
            WriteInstructionRm16(0x0e20c400u | (sz << 22) | (q << 30), rd, rn, rm);
        }

        public readonly void FmaxnmFloat(Operand rd, Operand rn, Operand rm, uint ftype)
        {
            WriteInstructionRm16(0x1e206800u | (ftype << 22), rd, rn, rm);
        }

        public readonly void FmaxpPairHalf(Operand rd, Operand rn, uint sz)
        {
            WriteInstruction(0x5e30f800u | (sz << 22), rd, rn);
        }

        public readonly void FmaxpPairSingleAndDouble(Operand rd, Operand rn, uint sz)
        {
            WriteInstruction(0x7e30f800u | (sz << 22), rd, rn);
        }

        public readonly void FmaxpVecHalf(Operand rd, Operand rn, Operand rm, uint q)
        {
            WriteInstructionRm16(0x2e403400u | (q << 30), rd, rn, rm);
        }

        public readonly void FmaxpVecSingleAndDouble(Operand rd, Operand rn, Operand rm, uint sz, uint q)
        {
            WriteInstructionRm16(0x2e20f400u | (sz << 22) | (q << 30), rd, rn, rm);
        }

        public readonly void FmaxvHalf(Operand rd, Operand rn, uint q)
        {
            WriteInstruction(0x0e30f800u | (q << 30), rd, rn);
        }

        public readonly void FmaxvSingleAndDouble(Operand rd, Operand rn, uint sz, uint q)
        {
            WriteInstruction(0x2e30f800u | (sz << 22) | (q << 30), rd, rn);
        }

        public readonly void FmaxHalf(Operand rd, Operand rn, Operand rm, uint q)
        {
            WriteInstructionRm16(0x0e403400u | (q << 30), rd, rn, rm);
        }

        public readonly void FmaxSingleAndDouble(Operand rd, Operand rn, Operand rm, uint sz, uint q)
        {
            WriteInstructionRm16(0x0e20f400u | (sz << 22) | (q << 30), rd, rn, rm);
        }

        public readonly void FmaxFloat(Operand rd, Operand rn, Operand rm, uint ftype)
        {
            WriteInstructionRm16(0x1e204800u | (ftype << 22), rd, rn, rm);
        }

        public readonly void FminnmpPairHalf(Operand rd, Operand rn, uint sz)
        {
            WriteInstruction(0x5eb0c800u | (sz << 22), rd, rn);
        }

        public readonly void FminnmpPairSingleAndDouble(Operand rd, Operand rn, uint sz)
        {
            WriteInstruction(0x7eb0c800u | (sz << 22), rd, rn);
        }

        public readonly void FminnmpVecHalf(Operand rd, Operand rn, Operand rm, uint q)
        {
            WriteInstructionRm16(0x2ec00400u | (q << 30), rd, rn, rm);
        }

        public readonly void FminnmpVecSingleAndDouble(Operand rd, Operand rn, Operand rm, uint sz, uint q)
        {
            WriteInstructionRm16(0x2ea0c400u | (sz << 22) | (q << 30), rd, rn, rm);
        }

        public readonly void FminnmvHalf(Operand rd, Operand rn, uint q)
        {
            WriteInstruction(0x0eb0c800u | (q << 30), rd, rn);
        }

        public readonly void FminnmvSingleAndDouble(Operand rd, Operand rn, uint sz, uint q)
        {
            WriteInstruction(0x2eb0c800u | (sz << 22) | (q << 30), rd, rn);
        }

        public readonly void FminnmHalf(Operand rd, Operand rn, Operand rm, uint q)
        {
            WriteInstructionRm16(0x0ec00400u | (q << 30), rd, rn, rm);
        }

        public readonly void FminnmSingleAndDouble(Operand rd, Operand rn, Operand rm, uint sz, uint q)
        {
            WriteInstructionRm16(0x0ea0c400u | (sz << 22) | (q << 30), rd, rn, rm);
        }

        public readonly void FminnmFloat(Operand rd, Operand rn, Operand rm, uint ftype)
        {
            WriteInstructionRm16(0x1e207800u | (ftype << 22), rd, rn, rm);
        }

        public readonly void FminpPairHalf(Operand rd, Operand rn, uint sz)
        {
            WriteInstruction(0x5eb0f800u | (sz << 22), rd, rn);
        }

        public readonly void FminpPairSingleAndDouble(Operand rd, Operand rn, uint sz)
        {
            WriteInstruction(0x7eb0f800u | (sz << 22), rd, rn);
        }

        public readonly void FminpVecHalf(Operand rd, Operand rn, Operand rm, uint q)
        {
            WriteInstructionRm16(0x2ec03400u | (q << 30), rd, rn, rm);
        }

        public readonly void FminpVecSingleAndDouble(Operand rd, Operand rn, Operand rm, uint sz, uint q)
        {
            WriteInstructionRm16(0x2ea0f400u | (sz << 22) | (q << 30), rd, rn, rm);
        }

        public readonly void FminvHalf(Operand rd, Operand rn, uint q)
        {
            WriteInstruction(0x0eb0f800u | (q << 30), rd, rn);
        }

        public readonly void FminvSingleAndDouble(Operand rd, Operand rn, uint sz, uint q)
        {
            WriteInstruction(0x2eb0f800u | (sz << 22) | (q << 30), rd, rn);
        }

        public readonly void FminHalf(Operand rd, Operand rn, Operand rm, uint q)
        {
            WriteInstructionRm16(0x0ec03400u | (q << 30), rd, rn, rm);
        }

        public readonly void FminSingleAndDouble(Operand rd, Operand rn, Operand rm, uint sz, uint q)
        {
            WriteInstructionRm16(0x0ea0f400u | (sz << 22) | (q << 30), rd, rn, rm);
        }

        public readonly void FminFloat(Operand rd, Operand rn, Operand rm, uint ftype)
        {
            WriteInstructionRm16(0x1e205800u | (ftype << 22), rd, rn, rm);
        }

        public readonly void FmlalEltFmlal(Operand rd, Operand rn, uint h, Operand rm, uint m, uint l, uint q)
        {
            WriteInstructionRm16(0x0f800000u | (h << 11) | (m << 20) | (l << 21) | (q << 30), rd, rn, rm);
        }

        public readonly void FmlalEltFmlal2(Operand rd, Operand rn, uint h, Operand rm, uint m, uint l, uint q)
        {
            WriteInstructionRm16(0x2f808000u | (h << 11) | (m << 20) | (l << 21) | (q << 30), rd, rn, rm);
        }

        public readonly void FmlalVecFmlal(Operand rd, Operand rn, Operand rm, uint q)
        {
            WriteInstructionRm16(0x0e20ec00u | (q << 30), rd, rn, rm);
        }

        public readonly void FmlalVecFmlal2(Operand rd, Operand rn, Operand rm, uint q)
        {
            WriteInstructionRm16(0x2e20cc00u | (q << 30), rd, rn, rm);
        }

        public readonly void FmlaElt2regScalarHalf(Operand rd, Operand rn, uint h, Operand rm, uint m, uint l)
        {
            WriteInstructionRm16(0x5f001000u | (h << 11) | (m << 20) | (l << 21), rd, rn, rm);
        }

        public readonly void FmlaElt2regScalarSingleAndDouble(Operand rd, Operand rn, uint h, Operand rm, uint m, uint l, uint sz)
        {
            WriteInstructionRm16(0x5f801000u | (h << 11) | (m << 20) | (l << 21) | (sz << 22), rd, rn, rm);
        }

        public readonly void FmlaElt2regElementHalf(Operand rd, Operand rn, uint h, Operand rm, uint m, uint l, uint q)
        {
            WriteInstructionRm16(0x0f001000u | (h << 11) | (m << 20) | (l << 21) | (q << 30), rd, rn, rm);
        }

        public readonly void FmlaElt2regElementSingleAndDouble(Operand rd, Operand rn, uint h, Operand rm, uint m, uint l, uint sz, uint q)
        {
            WriteInstructionRm16(0x0f801000u | (h << 11) | (m << 20) | (l << 21) | (sz << 22) | (q << 30), rd, rn, rm);
        }

        public readonly void FmlaVecHalf(Operand rd, Operand rn, Operand rm, uint q)
        {
            WriteInstructionRm16(0x0e400c00u | (q << 30), rd, rn, rm);
        }

        public readonly void FmlaVecSingleAndDouble(Operand rd, Operand rn, Operand rm, uint sz, uint q)
        {
            WriteInstructionRm16(0x0e20cc00u | (sz << 22) | (q << 30), rd, rn, rm);
        }

        public readonly void FmlslEltFmlsl(Operand rd, Operand rn, uint h, Operand rm, uint m, uint l, uint q)
        {
            WriteInstructionRm16(0x0f804000u | (h << 11) | (m << 20) | (l << 21) | (q << 30), rd, rn, rm);
        }

        public readonly void FmlslEltFmlsl2(Operand rd, Operand rn, uint h, Operand rm, uint m, uint l, uint q)
        {
            WriteInstructionRm16(0x2f80c000u | (h << 11) | (m << 20) | (l << 21) | (q << 30), rd, rn, rm);
        }

        public readonly void FmlslVecFmlsl(Operand rd, Operand rn, Operand rm, uint q)
        {
            WriteInstructionRm16(0x0ea0ec00u | (q << 30), rd, rn, rm);
        }

        public readonly void FmlslVecFmlsl2(Operand rd, Operand rn, Operand rm, uint q)
        {
            WriteInstructionRm16(0x2ea0cc00u | (q << 30), rd, rn, rm);
        }

        public readonly void FmlsElt2regScalarHalf(Operand rd, Operand rn, uint h, Operand rm, uint m, uint l)
        {
            WriteInstructionRm16(0x5f005000u | (h << 11) | (m << 20) | (l << 21), rd, rn, rm);
        }

        public readonly void FmlsElt2regScalarSingleAndDouble(Operand rd, Operand rn, uint h, Operand rm, uint m, uint l, uint sz)
        {
            WriteInstructionRm16(0x5f805000u | (h << 11) | (m << 20) | (l << 21) | (sz << 22), rd, rn, rm);
        }

        public readonly void FmlsElt2regElementHalf(Operand rd, Operand rn, uint h, Operand rm, uint m, uint l, uint q)
        {
            WriteInstructionRm16(0x0f005000u | (h << 11) | (m << 20) | (l << 21) | (q << 30), rd, rn, rm);
        }

        public readonly void FmlsElt2regElementSingleAndDouble(Operand rd, Operand rn, uint h, Operand rm, uint m, uint l, uint sz, uint q)
        {
            WriteInstructionRm16(0x0f805000u | (h << 11) | (m << 20) | (l << 21) | (sz << 22) | (q << 30), rd, rn, rm);
        }

        public readonly void FmlsVecHalf(Operand rd, Operand rn, Operand rm, uint q)
        {
            WriteInstructionRm16(0x0ec00c00u | (q << 30), rd, rn, rm);
        }

        public readonly void FmlsVecSingleAndDouble(Operand rd, Operand rn, Operand rm, uint sz, uint q)
        {
            WriteInstructionRm16(0x0ea0cc00u | (sz << 22) | (q << 30), rd, rn, rm);
        }

        public readonly void FmovPerHalf(Operand rd, uint h, uint g, uint f, uint e, uint d, uint c, uint b, uint a, uint q)
        {
            WriteInstruction(0x0f00fc00u | (h << 5) | (g << 6) | (f << 7) | (e << 8) | (d << 9) | (c << 16) | (b << 17) | (a << 18) | (q << 30), rd);
        }

        public readonly void FmovSingleAndDouble(Operand rd, uint h, uint g, uint f, uint e, uint d, uint c, uint b, uint a, uint op, uint q)
        {
            WriteInstruction(0x0f00f400u | (h << 5) | (g << 6) | (f << 7) | (e << 8) | (d << 9) | (c << 16) | (b << 17) | (a << 18) | (op << 29) | (q << 30), rd);
        }

        public readonly void FmovFloat(Operand rd, Operand rn, uint ftype)
        {
            WriteInstruction(0x1e204000u | (ftype << 22), rd, rn);
        }

        public readonly void FmovFloatGen(Operand rd, Operand rn, uint ftype, uint sf, uint dir, uint top)
        {
            WriteInstruction(0x1e260000u | (dir << 16) | (top << 19) | (ftype << 22) | (sf << 31), rd, rn);
        }

        public readonly void FmovFloatImm(Operand rd, uint imm8, uint ftype)
        {
            WriteInstruction(0x1e201000u | (imm8 << 13) | (ftype << 22), rd);
        }

        public readonly void FmsubFloat(Operand rd, Operand rn, Operand rm, Operand ra, uint ftype)
        {
            WriteInstruction(0x1f008000u | (ftype << 22), rd, rn, rm, ra);
        }

        public readonly void FmulxElt2regScalarHalf(Operand rd, Operand rn, uint h, Operand rm, uint m, uint l)
        {
            WriteInstructionRm16(0x7f009000u | (h << 11) | (m << 20) | (l << 21), rd, rn, rm);
        }

        public readonly void FmulxElt2regScalarSingleAndDouble(Operand rd, Operand rn, uint h, Operand rm, uint m, uint l, uint sz)
        {
            WriteInstructionRm16(0x7f809000u | (h << 11) | (m << 20) | (l << 21) | (sz << 22), rd, rn, rm);
        }

        public readonly void FmulxElt2regElementHalf(Operand rd, Operand rn, uint h, Operand rm, uint m, uint l, uint q)
        {
            WriteInstructionRm16(0x2f009000u | (h << 11) | (m << 20) | (l << 21) | (q << 30), rd, rn, rm);
        }

        public readonly void FmulxElt2regElementSingleAndDouble(Operand rd, Operand rn, uint h, Operand rm, uint m, uint l, uint sz, uint q)
        {
            WriteInstructionRm16(0x2f809000u | (h << 11) | (m << 20) | (l << 21) | (sz << 22) | (q << 30), rd, rn, rm);
        }

        public readonly void FmulxVecSH(Operand rd, Operand rn, Operand rm)
        {
            WriteInstructionRm16(0x5e401c00u, rd, rn, rm);
        }

        public readonly void FmulxVecS(Operand rd, Operand rn, Operand rm, uint sz)
        {
            WriteInstructionRm16(0x5e20dc00u | (sz << 22), rd, rn, rm);
        }

        public readonly void FmulxVecVH(Operand rd, Operand rn, Operand rm, uint q)
        {
            WriteInstructionRm16(0x0e401c00u | (q << 30), rd, rn, rm);
        }

        public readonly void FmulxVecV(Operand rd, Operand rn, Operand rm, uint sz, uint q)
        {
            WriteInstructionRm16(0x0e20dc00u | (sz << 22) | (q << 30), rd, rn, rm);
        }

        public readonly void FmulElt2regScalarHalf(Operand rd, Operand rn, uint h, Operand rm, uint m, uint l)
        {
            WriteInstructionRm16(0x5f009000u | (h << 11) | (m << 20) | (l << 21), rd, rn, rm);
        }

        public readonly void FmulElt2regScalarSingleAndDouble(Operand rd, Operand rn, uint h, Operand rm, uint m, uint l, uint sz)
        {
            WriteInstructionRm16(0x5f809000u | (h << 11) | (m << 20) | (l << 21) | (sz << 22), rd, rn, rm);
        }

        public readonly void FmulElt2regElementHalf(Operand rd, Operand rn, uint h, Operand rm, uint m, uint l, uint q)
        {
            WriteInstructionRm16(0x0f009000u | (h << 11) | (m << 20) | (l << 21) | (q << 30), rd, rn, rm);
        }

        public readonly void FmulElt2regElementSingleAndDouble(Operand rd, Operand rn, uint h, Operand rm, uint m, uint l, uint sz, uint q)
        {
            WriteInstructionRm16(0x0f809000u | (h << 11) | (m << 20) | (l << 21) | (sz << 22) | (q << 30), rd, rn, rm);
        }

        public readonly void FmulVecHalf(Operand rd, Operand rn, Operand rm, uint q)
        {
            WriteInstructionRm16(0x2e401c00u | (q << 30), rd, rn, rm);
        }

        public readonly void FmulVecSingleAndDouble(Operand rd, Operand rn, Operand rm, uint sz, uint q)
        {
            WriteInstructionRm16(0x2e20dc00u | (sz << 22) | (q << 30), rd, rn, rm);
        }

        public readonly void FmulFloat(Operand rd, Operand rn, Operand rm, uint ftype)
        {
            WriteInstructionRm16(0x1e200800u | (ftype << 22), rd, rn, rm);
        }

        public readonly void FnegHalf(Operand rd, Operand rn, uint q)
        {
            WriteInstruction(0x2ef8f800u | (q << 30), rd, rn);
        }

        public readonly void FnegSingleAndDouble(Operand rd, Operand rn, uint sz, uint q)
        {
            WriteInstruction(0x2ea0f800u | (sz << 22) | (q << 30), rd, rn);
        }

        public readonly void FnegFloat(Operand rd, Operand rn, uint ftype)
        {
            WriteInstruction(0x1e214000u | (ftype << 22), rd, rn);
        }

        public readonly void FnmaddFloat(Operand rd, Operand rn, Operand rm, Operand ra, uint ftype)
        {
            WriteInstruction(0x1f200000u | (ftype << 22), rd, rn, rm, ra);
        }

        public readonly void FnmsubFloat(Operand rd, Operand rn, Operand rm, Operand ra, uint ftype)
        {
            WriteInstruction(0x1f208000u | (ftype << 22), rd, rn, rm, ra);
        }

        public readonly void FnmulFloat(Operand rd, Operand rn, Operand rm, uint ftype)
        {
            WriteInstructionRm16(0x1e208800u | (ftype << 22), rd, rn, rm);
        }

        public readonly void FrecpeSH(Operand rd, Operand rn)
        {
            WriteInstruction(0x5ef9d800u, rd, rn);
        }

        public readonly void FrecpeS(Operand rd, Operand rn, uint sz)
        {
            WriteInstruction(0x5ea1d800u | (sz << 22), rd, rn);
        }

        public readonly void FrecpeVH(Operand rd, Operand rn, uint q)
        {
            WriteInstruction(0x0ef9d800u | (q << 30), rd, rn);
        }

        public readonly void FrecpeV(Operand rd, Operand rn, uint sz, uint q)
        {
            WriteInstruction(0x0ea1d800u | (sz << 22) | (q << 30), rd, rn);
        }

        public readonly void FrecpsSH(Operand rd, Operand rn, Operand rm)
        {
            WriteInstructionRm16(0x5e403c00u, rd, rn, rm);
        }

        public readonly void FrecpsS(Operand rd, Operand rn, Operand rm, uint sz)
        {
            WriteInstructionRm16(0x5e20fc00u | (sz << 22), rd, rn, rm);
        }

        public readonly void FrecpsVH(Operand rd, Operand rn, Operand rm, uint q)
        {
            WriteInstructionRm16(0x0e403c00u | (q << 30), rd, rn, rm);
        }

        public readonly void FrecpsV(Operand rd, Operand rn, Operand rm, uint sz, uint q)
        {
            WriteInstructionRm16(0x0e20fc00u | (sz << 22) | (q << 30), rd, rn, rm);
        }

        public readonly void FrecpxHalf(Operand rd, Operand rn)
        {
            WriteInstruction(0x5ef9f800u, rd, rn);
        }

        public readonly void FrecpxSingleAndDouble(Operand rd, Operand rn, uint sz)
        {
            WriteInstruction(0x5ea1f800u | (sz << 22), rd, rn);
        }

        public readonly void Frint32x(Operand rd, Operand rn, uint sz, uint q)
        {
            WriteInstruction(0x2e21e800u | (sz << 22) | (q << 30), rd, rn);
        }

        public readonly void Frint32xFloat(Operand rd, Operand rn)
        {
            WriteInstruction(0x1e28c000u, rd, rn);
        }

        public readonly void Frint32z(Operand rd, Operand rn, uint sz, uint q)
        {
            WriteInstruction(0x0e21e800u | (sz << 22) | (q << 30), rd, rn);
        }

        public readonly void Frint32zFloat(Operand rd, Operand rn)
        {
            WriteInstruction(0x1e284000u, rd, rn);
        }

        public readonly void Frint64x(Operand rd, Operand rn, uint sz, uint q)
        {
            WriteInstruction(0x2e21f800u | (sz << 22) | (q << 30), rd, rn);
        }

        public readonly void Frint64xFloat(Operand rd, Operand rn)
        {
            WriteInstruction(0x1e29c000u, rd, rn);
        }

        public readonly void Frint64z(Operand rd, Operand rn, uint sz, uint q)
        {
            WriteInstruction(0x0e21f800u | (sz << 22) | (q << 30), rd, rn);
        }

        public readonly void Frint64zFloat(Operand rd, Operand rn)
        {
            WriteInstruction(0x1e294000u, rd, rn);
        }

        public readonly void FrintaHalf(Operand rd, Operand rn, uint q)
        {
            WriteInstruction(0x2e798800u | (q << 30), rd, rn);
        }

        public readonly void FrintaSingleAndDouble(Operand rd, Operand rn, uint sz, uint q)
        {
            WriteInstruction(0x2e218800u | (sz << 22) | (q << 30), rd, rn);
        }

        public readonly void FrintaFloat(Operand rd, Operand rn, uint ftype)
        {
            WriteInstruction(0x1e264000u | (ftype << 22), rd, rn);
        }

        public readonly void FrintiHalf(Operand rd, Operand rn, uint q)
        {
            WriteInstruction(0x2ef99800u | (q << 30), rd, rn);
        }

        public readonly void FrintiSingleAndDouble(Operand rd, Operand rn, uint sz, uint q)
        {
            WriteInstruction(0x2ea19800u | (sz << 22) | (q << 30), rd, rn);
        }

        public readonly void FrintiFloat(Operand rd, Operand rn, uint ftype)
        {
            WriteInstruction(0x1e27c000u | (ftype << 22), rd, rn);
        }

        public readonly void FrintmHalf(Operand rd, Operand rn, uint q)
        {
            WriteInstruction(0x0e799800u | (q << 30), rd, rn);
        }

        public readonly void FrintmSingleAndDouble(Operand rd, Operand rn, uint sz, uint q)
        {
            WriteInstruction(0x0e219800u | (sz << 22) | (q << 30), rd, rn);
        }

        public readonly void FrintmFloat(Operand rd, Operand rn, uint ftype)
        {
            WriteInstruction(0x1e254000u | (ftype << 22), rd, rn);
        }

        public readonly void FrintnHalf(Operand rd, Operand rn, uint q)
        {
            WriteInstruction(0x0e798800u | (q << 30), rd, rn);
        }

        public readonly void FrintnSingleAndDouble(Operand rd, Operand rn, uint sz, uint q)
        {
            WriteInstruction(0x0e218800u | (sz << 22) | (q << 30), rd, rn);
        }

        public readonly void FrintnFloat(Operand rd, Operand rn, uint ftype)
        {
            WriteInstruction(0x1e244000u | (ftype << 22), rd, rn);
        }

        public readonly void FrintpHalf(Operand rd, Operand rn, uint q)
        {
            WriteInstruction(0x0ef98800u | (q << 30), rd, rn);
        }

        public readonly void FrintpSingleAndDouble(Operand rd, Operand rn, uint sz, uint q)
        {
            WriteInstruction(0x0ea18800u | (sz << 22) | (q << 30), rd, rn);
        }

        public readonly void FrintpFloat(Operand rd, Operand rn, uint ftype)
        {
            WriteInstruction(0x1e24c000u | (ftype << 22), rd, rn);
        }

        public readonly void FrintxHalf(Operand rd, Operand rn, uint q)
        {
            WriteInstruction(0x2e799800u | (q << 30), rd, rn);
        }

        public readonly void FrintxSingleAndDouble(Operand rd, Operand rn, uint sz, uint q)
        {
            WriteInstruction(0x2e219800u | (sz << 22) | (q << 30), rd, rn);
        }

        public readonly void FrintxFloat(Operand rd, Operand rn, uint ftype)
        {
            WriteInstruction(0x1e274000u | (ftype << 22), rd, rn);
        }

        public readonly void FrintzHalf(Operand rd, Operand rn, uint q)
        {
            WriteInstruction(0x0ef99800u | (q << 30), rd, rn);
        }

        public readonly void FrintzSingleAndDouble(Operand rd, Operand rn, uint sz, uint q)
        {
            WriteInstruction(0x0ea19800u | (sz << 22) | (q << 30), rd, rn);
        }

        public readonly void FrintzFloat(Operand rd, Operand rn, uint ftype)
        {
            WriteInstruction(0x1e25c000u | (ftype << 22), rd, rn);
        }

        public readonly void FrsqrteSH(Operand rd, Operand rn)
        {
            WriteInstruction(0x7ef9d800u, rd, rn);
        }

        public readonly void FrsqrteS(Operand rd, Operand rn, uint sz)
        {
            WriteInstruction(0x7ea1d800u | (sz << 22), rd, rn);
        }

        public readonly void FrsqrteVH(Operand rd, Operand rn, uint q)
        {
            WriteInstruction(0x2ef9d800u | (q << 30), rd, rn);
        }

        public readonly void FrsqrteV(Operand rd, Operand rn, uint sz, uint q)
        {
            WriteInstruction(0x2ea1d800u | (sz << 22) | (q << 30), rd, rn);
        }

        public readonly void FrsqrtsSH(Operand rd, Operand rn, Operand rm)
        {
            WriteInstructionRm16(0x5ec03c00u, rd, rn, rm);
        }

        public readonly void FrsqrtsS(Operand rd, Operand rn, Operand rm, uint sz)
        {
            WriteInstructionRm16(0x5ea0fc00u | (sz << 22), rd, rn, rm);
        }

        public readonly void FrsqrtsVH(Operand rd, Operand rn, Operand rm, uint q)
        {
            WriteInstructionRm16(0x0ec03c00u | (q << 30), rd, rn, rm);
        }

        public readonly void FrsqrtsV(Operand rd, Operand rn, Operand rm, uint sz, uint q)
        {
            WriteInstructionRm16(0x0ea0fc00u | (sz << 22) | (q << 30), rd, rn, rm);
        }

        public readonly void FsqrtHalf(Operand rd, Operand rn, uint q)
        {
            WriteInstruction(0x2ef9f800u | (q << 30), rd, rn);
        }

        public readonly void FsqrtSingleAndDouble(Operand rd, Operand rn, uint sz, uint q)
        {
            WriteInstruction(0x2ea1f800u | (sz << 22) | (q << 30), rd, rn);
        }

        public readonly void FsqrtFloat(Operand rd, Operand rn, uint ftype)
        {
            WriteInstruction(0x1e21c000u | (ftype << 22), rd, rn);
        }

        public readonly void FsubHalf(Operand rd, Operand rn, Operand rm, uint q)
        {
            WriteInstructionRm16(0x0ec01400u | (q << 30), rd, rn, rm);
        }

        public readonly void FsubSingleAndDouble(Operand rd, Operand rn, Operand rm, uint sz, uint q)
        {
            WriteInstructionRm16(0x0ea0d400u | (sz << 22) | (q << 30), rd, rn, rm);
        }

        public readonly void FsubFloat(Operand rd, Operand rn, Operand rm, uint ftype)
        {
            WriteInstructionRm16(0x1e203800u | (ftype << 22), rd, rn, rm);
        }

        public readonly void InsElt(Operand rd, Operand rn, uint imm4, uint imm5)
        {
            WriteInstruction(0x6e000400u | (imm4 << 11) | (imm5 << 16), rd, rn);
        }

        public readonly void InsGen(Operand rd, Operand rn, uint imm5)
        {
            WriteInstruction(0x4e001c00u | (imm5 << 16), rd, rn);
        }

        public readonly void Ld1rAsNoPostIndex(Operand rt, Operand rn, uint size, uint q)
        {
            WriteInstruction(0x0d40c000u | (size << 10) | (q << 30), rt, rn);
        }

        public readonly void Ld1rAsPostIndex(Operand rt, Operand rn, Operand rm, uint size, uint q)
        {
            WriteInstructionRm16(0x0dc0c000u | (size << 10) | (q << 30), rt, rn, rm);
        }

        public readonly void Ld1MultAsNoPostIndex(Operand rt, Operand rn, uint registersCount, uint size, uint q)
        {
            WriteInstruction(0x0c402000u | (size << 10) | (EncodeLdSt1MultOpcode(registersCount) << 12) | (q << 30), rt, rn);
        }

        public readonly void Ld1MultAsPostIndex(Operand rt, Operand rn, Operand rm, uint registersCount, uint size, uint q)
        {
            WriteInstructionRm16(0x0cc02000u | (size << 10) | (EncodeLdSt1MultOpcode(registersCount) << 12) | (q << 30), rt, rn, rm);
        }

        public readonly void Ld1SnglAsNoPostIndex(Operand rt, Operand rn, uint index, uint size)
        {
            WriteInstruction(EncodeIndexSizeSngl(0x0d400000u, index, size), rt, rn);
        }

        public readonly void Ld1SnglAsPostIndex(Operand rt, Operand rn, Operand rm, uint index, uint size)
        {
            WriteInstructionRm16(EncodeIndexSizeSngl(0x0dc00000u, index, size), rt, rn, rm);
        }

        public readonly void Ld2rAsNoPostIndex(Operand rt, Operand rn, uint size, uint q)
        {
            WriteInstruction(0x0d60c000u | (size << 10) | (q << 30), rt, rn);
        }

        public readonly void Ld2rAsPostIndex(Operand rt, Operand rn, Operand rm, uint size, uint q)
        {
            WriteInstructionRm16(0x0de0c000u | (size << 10) | (q << 30), rt, rn, rm);
        }

        public readonly void Ld2MultAsNoPostIndex(Operand rt, Operand rn, uint size, uint q)
        {
            WriteInstruction(0x0c408000u | (size << 10) | (q << 30), rt, rn);
        }

        public readonly void Ld2MultAsPostIndex(Operand rt, Operand rn, Operand rm, uint size, uint q)
        {
            WriteInstructionRm16(0x0cc08000u | (size << 10) | (q << 30), rt, rn, rm);
        }

        public readonly void Ld2SnglAsNoPostIndex(Operand rt, Operand rn, uint index, uint size)
        {
            WriteInstruction(EncodeIndexSizeSngl(0x0d600000u, index, size), rt, rn);
        }

        public readonly void Ld2SnglAsPostIndex(Operand rt, Operand rn, Operand rm, uint index, uint size)
        {
            WriteInstructionRm16(EncodeIndexSizeSngl(0x0de00000u, index, size), rt, rn, rm);
        }

        public readonly void Ld3rAsNoPostIndex(Operand rt, Operand rn, uint size, uint q)
        {
            WriteInstruction(0x0d40e000u | (size << 10) | (q << 30), rt, rn);
        }

        public readonly void Ld3rAsPostIndex(Operand rt, Operand rn, Operand rm, uint size, uint q)
        {
            WriteInstructionRm16(0x0dc0e000u | (size << 10) | (q << 30), rt, rn, rm);
        }

        public readonly void Ld3MultAsNoPostIndex(Operand rt, Operand rn, uint size, uint q)
        {
            WriteInstruction(0x0c404000u | (size << 10) | (q << 30), rt, rn);
        }

        public readonly void Ld3MultAsPostIndex(Operand rt, Operand rn, Operand rm, uint size, uint q)
        {
            WriteInstructionRm16(0x0cc04000u | (size << 10) | (q << 30), rt, rn, rm);
        }

        public readonly void Ld3SnglAsNoPostIndex(Operand rt, Operand rn, uint index, uint size)
        {
            WriteInstruction(EncodeIndexSizeSngl(0x0d402000u, index, size), rt, rn);
        }

        public readonly void Ld3SnglAsPostIndex(Operand rt, Operand rn, Operand rm, uint index, uint size)
        {
            WriteInstructionRm16(EncodeIndexSizeSngl(0x0dc02000u, index, size), rt, rn, rm);
        }

        public readonly void Ld4rAsNoPostIndex(Operand rt, Operand rn, uint size, uint q)
        {
            WriteInstruction(0x0d60e000u | (size << 10) | (q << 30), rt, rn);
        }

        public readonly void Ld4rAsPostIndex(Operand rt, Operand rn, Operand rm, uint size, uint q)
        {
            WriteInstructionRm16(0x0de0e000u | (size << 10) | (q << 30), rt, rn, rm);
        }

        public readonly void Ld4MultAsNoPostIndex(Operand rt, Operand rn, uint size, uint q)
        {
            WriteInstruction(0x0c400000u | (size << 10) | (q << 30), rt, rn);
        }

        public readonly void Ld4MultAsPostIndex(Operand rt, Operand rn, Operand rm, uint size, uint q)
        {
            WriteInstructionRm16(0x0cc00000u | (size << 10) | (q << 30), rt, rn, rm);
        }

        public readonly void Ld4SnglAsNoPostIndex(Operand rt, Operand rn, uint index, uint size)
        {
            WriteInstruction(EncodeIndexSizeSngl(0x0d602000u, index, size), rt, rn);
        }

        public readonly void Ld4SnglAsPostIndex(Operand rt, Operand rn, Operand rm, uint index, uint size)
        {
            WriteInstructionRm16(EncodeIndexSizeSngl(0x0de02000u, index, size), rt, rn, rm);
        }

        public readonly void Ldap1Sngl(Operand rt, Operand rn, uint q)
        {
            WriteInstruction(0x0d418400u | (q << 30), rt, rn);
        }

        public readonly void Ldra(Operand rt, Operand rn, uint w, uint imm9, uint s, uint m)
        {
            WriteInstruction(0xf8200400u | (w << 11) | (imm9 << 12) | (s << 22) | (m << 23), rt, rn);
        }

        public readonly void MlaElt(Operand rd, Operand rn, uint h, Operand rm, uint m, uint l, uint size, uint q)
        {
            WriteInstructionRm16(0x2f000000u | (h << 11) | (m << 20) | (l << 21) | (size << 22) | (q << 30), rd, rn, rm);
        }

        public readonly void MlaVec(Operand rd, Operand rn, Operand rm, uint size, uint q)
        {
            WriteInstructionRm16(0x0e209400u | (size << 22) | (q << 30), rd, rn, rm);
        }

        public readonly void MlsElt(Operand rd, Operand rn, uint h, Operand rm, uint m, uint l, uint size, uint q)
        {
            WriteInstructionRm16(0x2f004000u | (h << 11) | (m << 20) | (l << 21) | (size << 22) | (q << 30), rd, rn, rm);
        }

        public readonly void MlsVec(Operand rd, Operand rn, Operand rm, uint size, uint q)
        {
            WriteInstructionRm16(0x2e209400u | (size << 22) | (q << 30), rd, rn, rm);
        }

        public readonly void Movi(Operand rd, uint h, uint g, uint f, uint e, uint d, uint cmode, uint c, uint b, uint a, uint op, uint q)
        {
            WriteInstruction(0x0f000400u | (h << 5) | (g << 6) | (f << 7) | (e << 8) | (d << 9) | (cmode << 12) | (c << 16) | (b << 17) | (a << 18) | (op << 29) | (q << 30), rd);
        }

        public readonly void MulElt(Operand rd, Operand rn, uint h, Operand rm, uint m, uint l, uint size, uint q)
        {
            WriteInstructionRm16(0x0f008000u | (h << 11) | (m << 20) | (l << 21) | (size << 22) | (q << 30), rd, rn, rm);
        }

        public readonly void MulVec(Operand rd, Operand rn, Operand rm, uint size, uint q)
        {
            WriteInstructionRm16(0x0e209c00u | (size << 22) | (q << 30), rd, rn, rm);
        }

        public readonly void Mvni(Operand rd, uint h, uint g, uint f, uint e, uint d, uint cmode, uint c, uint b, uint a, uint q)
        {
            WriteInstruction(0x2f000400u | (h << 5) | (g << 6) | (f << 7) | (e << 8) | (d << 9) | (cmode << 12) | (c << 16) | (b << 17) | (a << 18) | (q << 30), rd);
        }

        public readonly void NegS(Operand rd, Operand rn, uint size)
        {
            WriteInstruction(0x7e20b800u | (size << 22), rd, rn);
        }

        public readonly void NegV(Operand rd, Operand rn, uint size, uint q)
        {
            WriteInstruction(0x2e20b800u | (size << 22) | (q << 30), rd, rn);
        }

        public readonly void Not(Operand rd, Operand rn, uint q)
        {
            WriteInstruction(0x2e205800u | (q << 30), rd, rn);
        }

        public readonly void Orn(Operand rd, Operand rn, Operand rm, uint q)
        {
            WriteInstructionRm16(0x0ee01c00u | (q << 30), rd, rn, rm);
        }

        public readonly void OrrImm(Operand rd, uint h, uint g, uint f, uint e, uint d, uint c, uint b, uint a, uint q)
        {
            WriteInstruction(0x0f001400u | (h << 5) | (g << 6) | (f << 7) | (e << 8) | (d << 9) | (c << 16) | (b << 17) | (a << 18) | (q << 30), rd);
        }

        public readonly void OrrReg(Operand rd, Operand rn, Operand rm, uint q)
        {
            WriteInstructionRm16(0x0ea01c00u | (q << 30), rd, rn, rm);
        }

        public readonly void Pmull(Operand rd, Operand rn, Operand rm, uint size, uint q)
        {
            WriteInstructionRm16(0x0e20e000u | (size << 22) | (q << 30), rd, rn, rm);
        }

        public readonly void Pmul(Operand rd, Operand rn, Operand rm, uint size, uint q)
        {
            WriteInstructionRm16(0x2e209c00u | (size << 22) | (q << 30), rd, rn, rm);
        }

        public readonly void Raddhn(Operand rd, Operand rn, Operand rm, uint size, uint q)
        {
            WriteInstructionRm16(0x2e204000u | (size << 22) | (q << 30), rd, rn, rm);
        }

        public readonly void Rax1(Operand rd, Operand rn, Operand rm)
        {
            WriteInstructionRm16(0xce608c00u, rd, rn, rm);
        }

        public readonly void Rbit(Operand rd, Operand rn, uint q)
        {
            WriteInstruction(0x2e605800u | (q << 30), rd, rn);
        }

        public readonly void Rev16(Operand rd, Operand rn, uint size, uint q)
        {
            WriteInstruction(0x0e201800u | (size << 22) | (q << 30), rd, rn);
        }

        public readonly void Rev32(Operand rd, Operand rn, uint size, uint q)
        {
            WriteInstruction(0x2e200800u | (size << 22) | (q << 30), rd, rn);
        }

        public readonly void Rev64(Operand rd, Operand rn, uint size, uint q)
        {
            WriteInstruction(0x0e200800u | (size << 22) | (q << 30), rd, rn);
        }

        public readonly void Rshrn(Operand rd, Operand rn, uint immb, uint immh, uint q)
        {
            WriteInstruction(0x0f008c00u | (immb << 16) | (immh << 19) | (q << 30), rd, rn);
        }

        public readonly void Rsubhn(Operand rd, Operand rn, Operand rm, uint size, uint q)
        {
            WriteInstructionRm16(0x2e206000u | (size << 22) | (q << 30), rd, rn, rm);
        }

        public readonly void Sabal(Operand rd, Operand rn, Operand rm, uint size, uint q)
        {
            WriteInstructionRm16(0x0e205000u | (size << 22) | (q << 30), rd, rn, rm);
        }

        public readonly void Saba(Operand rd, Operand rn, Operand rm, uint size, uint q)
        {
            WriteInstructionRm16(0x0e207c00u | (size << 22) | (q << 30), rd, rn, rm);
        }

        public readonly void Sabdl(Operand rd, Operand rn, Operand rm, uint size, uint q)
        {
            WriteInstructionRm16(0x0e207000u | (size << 22) | (q << 30), rd, rn, rm);
        }

        public readonly void Sabd(Operand rd, Operand rn, Operand rm, uint size, uint q)
        {
            WriteInstructionRm16(0x0e207400u | (size << 22) | (q << 30), rd, rn, rm);
        }

        public readonly void Sadalp(Operand rd, Operand rn, uint size, uint q)
        {
            WriteInstruction(0x0e206800u | (size << 22) | (q << 30), rd, rn);
        }

        public readonly void Saddlp(Operand rd, Operand rn, uint size, uint q)
        {
            WriteInstruction(0x0e202800u | (size << 22) | (q << 30), rd, rn);
        }

        public readonly void Saddlv(Operand rd, Operand rn, uint size, uint q)
        {
            WriteInstruction(0x0e303800u | (size << 22) | (q << 30), rd, rn);
        }

        public readonly void Saddl(Operand rd, Operand rn, Operand rm, uint size, uint q)
        {
            WriteInstructionRm16(0x0e200000u | (size << 22) | (q << 30), rd, rn, rm);
        }

        public readonly void Saddw(Operand rd, Operand rn, Operand rm, uint size, uint q)
        {
            WriteInstructionRm16(0x0e201000u | (size << 22) | (q << 30), rd, rn, rm);
        }

        public readonly void ScvtfFixS(Operand rd, Operand rn, uint immb, uint immh)
        {
            WriteInstruction(0x5f00e400u | (immb << 16) | (immh << 19), rd, rn);
        }

        public readonly void ScvtfFixV(Operand rd, Operand rn, uint immb, uint immh, uint q)
        {
            WriteInstruction(0x0f00e400u | (immb << 16) | (immh << 19) | (q << 30), rd, rn);
        }

        public readonly void ScvtfIntSH(Operand rd, Operand rn)
        {
            WriteInstruction(0x5e79d800u, rd, rn);
        }

        public readonly void ScvtfIntS(Operand rd, Operand rn, uint sz)
        {
            WriteInstruction(0x5e21d800u | (sz << 22), rd, rn);
        }

        public readonly void ScvtfIntVH(Operand rd, Operand rn, uint q)
        {
            WriteInstruction(0x0e79d800u | (q << 30), rd, rn);
        }

        public readonly void ScvtfIntV(Operand rd, Operand rn, uint sz, uint q)
        {
            WriteInstruction(0x0e21d800u | (sz << 22) | (q << 30), rd, rn);
        }

        public readonly void ScvtfFloatFix(Operand rd, Operand rn, uint scale, uint ftype, uint sf)
        {
            WriteInstruction(0x1e020000u | (scale << 10) | (ftype << 22) | (sf << 31), rd, rn);
        }

        public readonly void ScvtfFloatInt(Operand rd, Operand rn, uint ftype, uint sf)
        {
            WriteInstruction(0x1e220000u | (ftype << 22) | (sf << 31), rd, rn);
        }

        public readonly void SdotElt(Operand rd, Operand rn, uint h, Operand rm, uint m, uint l, uint size, uint q)
        {
            WriteInstructionRm16(0x0f00e000u | (h << 11) | (m << 20) | (l << 21) | (size << 22) | (q << 30), rd, rn, rm);
        }

        public readonly void SdotVec(Operand rd, Operand rn, Operand rm, uint size, uint q)
        {
            WriteInstructionRm16(0x0e009400u | (size << 22) | (q << 30), rd, rn, rm);
        }

        public readonly void Sha1c(Operand rd, Operand rn, Operand rm)
        {
            WriteInstructionRm16(0x5e000000u, rd, rn, rm);
        }

        public readonly void Sha1h(Operand rd, Operand rn)
        {
            WriteInstruction(0x5e280800u, rd, rn);
        }

        public readonly void Sha1m(Operand rd, Operand rn, Operand rm)
        {
            WriteInstructionRm16(0x5e002000u, rd, rn, rm);
        }

        public readonly void Sha1p(Operand rd, Operand rn, Operand rm)
        {
            WriteInstructionRm16(0x5e001000u, rd, rn, rm);
        }

        public readonly void Sha1su0(Operand rd, Operand rn, Operand rm)
        {
            WriteInstructionRm16(0x5e003000u, rd, rn, rm);
        }

        public readonly void Sha1su1(Operand rd, Operand rn)
        {
            WriteInstruction(0x5e281800u, rd, rn);
        }

        public readonly void Sha256h2(Operand rd, Operand rn, Operand rm)
        {
            WriteInstructionRm16(0x5e005000u, rd, rn, rm);
        }

        public readonly void Sha256h(Operand rd, Operand rn, Operand rm)
        {
            WriteInstructionRm16(0x5e004000u, rd, rn, rm);
        }

        public readonly void Sha256su0(Operand rd, Operand rn)
        {
            WriteInstruction(0x5e282800u, rd, rn);
        }

        public readonly void Sha256su1(Operand rd, Operand rn, Operand rm)
        {
            WriteInstructionRm16(0x5e006000u, rd, rn, rm);
        }

        public readonly void Sha512h2(Operand rd, Operand rn, Operand rm)
        {
            WriteInstructionRm16(0xce608400u, rd, rn, rm);
        }

        public readonly void Sha512h(Operand rd, Operand rn, Operand rm)
        {
            WriteInstructionRm16(0xce608000u, rd, rn, rm);
        }

        public readonly void Sha512su0(Operand rd, Operand rn)
        {
            WriteInstruction(0xcec08000u, rd, rn);
        }

        public readonly void Sha512su1(Operand rd, Operand rn, Operand rm)
        {
            WriteInstructionRm16(0xce608800u, rd, rn, rm);
        }

        public readonly void Shadd(Operand rd, Operand rn, Operand rm, uint size, uint q)
        {
            WriteInstructionRm16(0x0e200400u | (size << 22) | (q << 30), rd, rn, rm);
        }

        public readonly void Shll(Operand rd, Operand rn, uint size, uint q)
        {
            WriteInstruction(0x2e213800u | (size << 22) | (q << 30), rd, rn);
        }

        public readonly void ShlS(Operand rd, Operand rn, uint immb, uint immh)
        {
            WriteInstruction(0x5f005400u | (immb << 16) | (immh << 19), rd, rn);
        }

        public readonly void ShlV(Operand rd, Operand rn, uint immb, uint immh, uint q)
        {
            WriteInstruction(0x0f005400u | (immb << 16) | (immh << 19) | (q << 30), rd, rn);
        }

        public readonly void Shrn(Operand rd, Operand rn, uint immb, uint immh, uint q)
        {
            WriteInstruction(0x0f008400u | (immb << 16) | (immh << 19) | (q << 30), rd, rn);
        }

        public readonly void Shsub(Operand rd, Operand rn, Operand rm, uint size, uint q)
        {
            WriteInstructionRm16(0x0e202400u | (size << 22) | (q << 30), rd, rn, rm);
        }

        public readonly void SliS(Operand rd, Operand rn, uint immb, uint immh)
        {
            WriteInstruction(0x7f005400u | (immb << 16) | (immh << 19), rd, rn);
        }

        public readonly void SliV(Operand rd, Operand rn, uint immb, uint immh, uint q)
        {
            WriteInstruction(0x2f005400u | (immb << 16) | (immh << 19) | (q << 30), rd, rn);
        }

        public readonly void Sm3partw1(Operand rd, Operand rn, Operand rm)
        {
            WriteInstructionRm16(0xce60c000u, rd, rn, rm);
        }

        public readonly void Sm3partw2(Operand rd, Operand rn, Operand rm)
        {
            WriteInstructionRm16(0xce60c400u, rd, rn, rm);
        }

        public readonly void Sm3ss1(Operand rd, Operand rn, Operand rm, Operand ra)
        {
            WriteInstruction(0xce400000u, rd, rn, rm, ra);
        }

        public readonly void Sm3tt1a(Operand rd, Operand rn, uint imm2, Operand rm)
        {
            WriteInstructionRm16(0xce408000u | (imm2 << 12), rd, rn, rm);
        }

        public readonly void Sm3tt1b(Operand rd, Operand rn, uint imm2, Operand rm)
        {
            WriteInstructionRm16(0xce408400u | (imm2 << 12), rd, rn, rm);
        }

        public readonly void Sm3tt2a(Operand rd, Operand rn, uint imm2, Operand rm)
        {
            WriteInstructionRm16(0xce408800u | (imm2 << 12), rd, rn, rm);
        }

        public readonly void Sm3tt2b(Operand rd, Operand rn, uint imm2, Operand rm)
        {
            WriteInstructionRm16(0xce408c00u | (imm2 << 12), rd, rn, rm);
        }

        public readonly void Sm4ekey(Operand rd, Operand rn, Operand rm)
        {
            WriteInstructionRm16(0xce60c800u, rd, rn, rm);
        }

        public readonly void Sm4e(Operand rd, Operand rn)
        {
            WriteInstruction(0xcec08400u, rd, rn);
        }

        public readonly void Smaxp(Operand rd, Operand rn, Operand rm, uint size, uint q)
        {
            WriteInstructionRm16(0x0e20a400u | (size << 22) | (q << 30), rd, rn, rm);
        }

        public readonly void Smaxv(Operand rd, Operand rn, uint size, uint q)
        {
            WriteInstruction(0x0e30a800u | (size << 22) | (q << 30), rd, rn);
        }

        public readonly void Smax(Operand rd, Operand rn, Operand rm, uint size, uint q)
        {
            WriteInstructionRm16(0x0e206400u | (size << 22) | (q << 30), rd, rn, rm);
        }

        public readonly void Sminp(Operand rd, Operand rn, Operand rm, uint size, uint q)
        {
            WriteInstructionRm16(0x0e20ac00u | (size << 22) | (q << 30), rd, rn, rm);
        }

        public readonly void Sminv(Operand rd, Operand rn, uint size, uint q)
        {
            WriteInstruction(0x0e31a800u | (size << 22) | (q << 30), rd, rn);
        }

        public readonly void Smin(Operand rd, Operand rn, Operand rm, uint size, uint q)
        {
            WriteInstructionRm16(0x0e206c00u | (size << 22) | (q << 30), rd, rn, rm);
        }

        public readonly void SmlalElt(Operand rd, Operand rn, uint h, Operand rm, uint m, uint l, uint size, uint q)
        {
            WriteInstructionRm16(0x0f002000u | (h << 11) | (m << 20) | (l << 21) | (size << 22) | (q << 30), rd, rn, rm);
        }

        public readonly void SmlalVec(Operand rd, Operand rn, Operand rm, uint size, uint q)
        {
            WriteInstructionRm16(0x0e208000u | (size << 22) | (q << 30), rd, rn, rm);
        }

        public readonly void SmlslElt(Operand rd, Operand rn, uint h, Operand rm, uint m, uint l, uint size, uint q)
        {
            WriteInstructionRm16(0x0f006000u | (h << 11) | (m << 20) | (l << 21) | (size << 22) | (q << 30), rd, rn, rm);
        }

        public readonly void SmlslVec(Operand rd, Operand rn, Operand rm, uint size, uint q)
        {
            WriteInstructionRm16(0x0e20a000u | (size << 22) | (q << 30), rd, rn, rm);
        }

        public readonly void SmmlaVec(Operand rd, Operand rn, Operand rm)
        {
            WriteInstructionRm16(0x4e80a400u, rd, rn, rm);
        }

        public readonly void Smov(Operand rd, Operand rn, int index, int size)
        {
            uint q = size == 2 ? 1u << 30 : 0u;
            WriteInstruction(0x0e002c00u | (EncodeIndexSizeImm5(index, size) << 16) | q, rd, rn);
        }

        public readonly void SmullElt(Operand rd, Operand rn, uint h, Operand rm, uint m, uint l, uint size, uint q)
        {
            WriteInstructionRm16(0x0f00a000u | (h << 11) | (m << 20) | (l << 21) | (size << 22) | (q << 30), rd, rn, rm);
        }

        public readonly void SmullVec(Operand rd, Operand rn, Operand rm, uint size, uint q)
        {
            WriteInstructionRm16(0x0e20c000u | (size << 22) | (q << 30), rd, rn, rm);
        }

        public readonly void SqabsS(Operand rd, Operand rn, uint size)
        {
            WriteInstruction(0x5e207800u | (size << 22), rd, rn);
        }

        public readonly void SqabsV(Operand rd, Operand rn, uint size, uint q)
        {
            WriteInstruction(0x0e207800u | (size << 22) | (q << 30), rd, rn);
        }

        public readonly void SqaddS(Operand rd, Operand rn, Operand rm, uint size)
        {
            WriteInstructionRm16(0x5e200c00u | (size << 22), rd, rn, rm);
        }

        public readonly void SqaddV(Operand rd, Operand rn, Operand rm, uint size, uint q)
        {
            WriteInstructionRm16(0x0e200c00u | (size << 22) | (q << 30), rd, rn, rm);
        }

        public readonly void SqdmlalElt2regScalar(Operand rd, Operand rn, uint h, Operand rm, uint m, uint l, uint size)
        {
            WriteInstructionRm16(0x5f003000u | (h << 11) | (m << 20) | (l << 21) | (size << 22), rd, rn, rm);
        }

        public readonly void SqdmlalElt2regElement(Operand rd, Operand rn, uint h, Operand rm, uint m, uint l, uint size, uint q)
        {
            WriteInstructionRm16(0x0f003000u | (h << 11) | (m << 20) | (l << 21) | (size << 22) | (q << 30), rd, rn, rm);
        }

        public readonly void SqdmlalVecS(Operand rd, Operand rn, Operand rm, uint size)
        {
            WriteInstructionRm16(0x5e209000u | (size << 22), rd, rn, rm);
        }

        public readonly void SqdmlalVecV(Operand rd, Operand rn, Operand rm, uint size, uint q)
        {
            WriteInstructionRm16(0x0e209000u | (size << 22) | (q << 30), rd, rn, rm);
        }

        public readonly void SqdmlslElt2regScalar(Operand rd, Operand rn, uint h, Operand rm, uint m, uint l, uint size)
        {
            WriteInstructionRm16(0x5f007000u | (h << 11) | (m << 20) | (l << 21) | (size << 22), rd, rn, rm);
        }

        public readonly void SqdmlslElt2regElement(Operand rd, Operand rn, uint h, Operand rm, uint m, uint l, uint size, uint q)
        {
            WriteInstructionRm16(0x0f007000u | (h << 11) | (m << 20) | (l << 21) | (size << 22) | (q << 30), rd, rn, rm);
        }

        public readonly void SqdmlslVecS(Operand rd, Operand rn, Operand rm, uint size)
        {
            WriteInstructionRm16(0x5e20b000u | (size << 22), rd, rn, rm);
        }

        public readonly void SqdmlslVecV(Operand rd, Operand rn, Operand rm, uint size, uint q)
        {
            WriteInstructionRm16(0x0e20b000u | (size << 22) | (q << 30), rd, rn, rm);
        }

        public readonly void SqdmulhElt2regScalar(Operand rd, Operand rn, uint h, Operand rm, uint m, uint l, uint size)
        {
            WriteInstructionRm16(0x5f00c000u | (h << 11) | (m << 20) | (l << 21) | (size << 22), rd, rn, rm);
        }

        public readonly void SqdmulhElt2regElement(Operand rd, Operand rn, uint h, Operand rm, uint m, uint l, uint size, uint q)
        {
            WriteInstructionRm16(0x0f00c000u | (h << 11) | (m << 20) | (l << 21) | (size << 22) | (q << 30), rd, rn, rm);
        }

        public readonly void SqdmulhVecS(Operand rd, Operand rn, Operand rm, uint size)
        {
            WriteInstructionRm16(0x5e20b400u | (size << 22), rd, rn, rm);
        }

        public readonly void SqdmulhVecV(Operand rd, Operand rn, Operand rm, uint size, uint q)
        {
            WriteInstructionRm16(0x0e20b400u | (size << 22) | (q << 30), rd, rn, rm);
        }

        public readonly void SqdmullElt2regScalar(Operand rd, Operand rn, uint h, Operand rm, uint m, uint l, uint size)
        {
            WriteInstructionRm16(0x5f00b000u | (h << 11) | (m << 20) | (l << 21) | (size << 22), rd, rn, rm);
        }

        public readonly void SqdmullElt2regElement(Operand rd, Operand rn, uint h, Operand rm, uint m, uint l, uint size, uint q)
        {
            WriteInstructionRm16(0x0f00b000u | (h << 11) | (m << 20) | (l << 21) | (size << 22) | (q << 30), rd, rn, rm);
        }

        public readonly void SqdmullVecS(Operand rd, Operand rn, Operand rm, uint size)
        {
            WriteInstructionRm16(0x5e20d000u | (size << 22), rd, rn, rm);
        }

        public readonly void SqdmullVecV(Operand rd, Operand rn, Operand rm, uint size, uint q)
        {
            WriteInstructionRm16(0x0e20d000u | (size << 22) | (q << 30), rd, rn, rm);
        }

        public readonly void SqnegS(Operand rd, Operand rn, uint size)
        {
            WriteInstruction(0x7e207800u | (size << 22), rd, rn);
        }

        public readonly void SqnegV(Operand rd, Operand rn, uint size, uint q)
        {
            WriteInstruction(0x2e207800u | (size << 22) | (q << 30), rd, rn);
        }

        public readonly void SqrdmlahElt2regScalar(Operand rd, Operand rn, uint h, Operand rm, uint m, uint l, uint size)
        {
            WriteInstructionRm16(0x7f00d000u | (h << 11) | (m << 20) | (l << 21) | (size << 22), rd, rn, rm);
        }

        public readonly void SqrdmlahElt2regElement(Operand rd, Operand rn, uint h, Operand rm, uint m, uint l, uint size, uint q)
        {
            WriteInstructionRm16(0x2f00d000u | (h << 11) | (m << 20) | (l << 21) | (size << 22) | (q << 30), rd, rn, rm);
        }

        public readonly void SqrdmlahVecS(Operand rd, Operand rn, Operand rm, uint size)
        {
            WriteInstructionRm16(0x7e008400u | (size << 22), rd, rn, rm);
        }

        public readonly void SqrdmlahVecV(Operand rd, Operand rn, Operand rm, uint size, uint q)
        {
            WriteInstructionRm16(0x2e008400u | (size << 22) | (q << 30), rd, rn, rm);
        }

        public readonly void SqrdmlshElt2regScalar(Operand rd, Operand rn, uint h, Operand rm, uint m, uint l, uint size)
        {
            WriteInstructionRm16(0x7f00f000u | (h << 11) | (m << 20) | (l << 21) | (size << 22), rd, rn, rm);
        }

        public readonly void SqrdmlshElt2regElement(Operand rd, Operand rn, uint h, Operand rm, uint m, uint l, uint size, uint q)
        {
            WriteInstructionRm16(0x2f00f000u | (h << 11) | (m << 20) | (l << 21) | (size << 22) | (q << 30), rd, rn, rm);
        }

        public readonly void SqrdmlshVecS(Operand rd, Operand rn, Operand rm, uint size)
        {
            WriteInstructionRm16(0x7e008c00u | (size << 22), rd, rn, rm);
        }

        public readonly void SqrdmlshVecV(Operand rd, Operand rn, Operand rm, uint size, uint q)
        {
            WriteInstructionRm16(0x2e008c00u | (size << 22) | (q << 30), rd, rn, rm);
        }

        public readonly void SqrdmulhElt2regScalar(Operand rd, Operand rn, uint h, Operand rm, uint m, uint l, uint size)
        {
            WriteInstructionRm16(0x5f00d000u | (h << 11) | (m << 20) | (l << 21) | (size << 22), rd, rn, rm);
        }

        public readonly void SqrdmulhElt2regElement(Operand rd, Operand rn, uint h, Operand rm, uint m, uint l, uint size, uint q)
        {
            WriteInstructionRm16(0x0f00d000u | (h << 11) | (m << 20) | (l << 21) | (size << 22) | (q << 30), rd, rn, rm);
        }

        public readonly void SqrdmulhVecS(Operand rd, Operand rn, Operand rm, uint size)
        {
            WriteInstructionRm16(0x7e20b400u | (size << 22), rd, rn, rm);
        }

        public readonly void SqrdmulhVecV(Operand rd, Operand rn, Operand rm, uint size, uint q)
        {
            WriteInstructionRm16(0x2e20b400u | (size << 22) | (q << 30), rd, rn, rm);
        }

        public readonly void SqrshlS(Operand rd, Operand rn, Operand rm, uint size)
        {
            WriteInstructionRm16(0x5e205c00u | (size << 22), rd, rn, rm);
        }

        public readonly void SqrshlV(Operand rd, Operand rn, Operand rm, uint size, uint q)
        {
            WriteInstructionRm16(0x0e205c00u | (size << 22) | (q << 30), rd, rn, rm);
        }

        public readonly void SqrshrnS(Operand rd, Operand rn, uint immb, uint immh)
        {
            WriteInstruction(0x5f009c00u | (immb << 16) | (immh << 19), rd, rn);
        }

        public readonly void SqrshrnV(Operand rd, Operand rn, uint immb, uint immh, uint q)
        {
            WriteInstruction(0x0f009c00u | (immb << 16) | (immh << 19) | (q << 30), rd, rn);
        }

        public readonly void SqrshrunS(Operand rd, Operand rn, uint immb, uint immh)
        {
            WriteInstruction(0x7f008c00u | (immb << 16) | (immh << 19), rd, rn);
        }

        public readonly void SqrshrunV(Operand rd, Operand rn, uint immb, uint immh, uint q)
        {
            WriteInstruction(0x2f008c00u | (immb << 16) | (immh << 19) | (q << 30), rd, rn);
        }

        public readonly void SqshluS(Operand rd, Operand rn, uint immb, uint immh)
        {
            WriteInstruction(0x7f006400u | (immb << 16) | (immh << 19), rd, rn);
        }

        public readonly void SqshluV(Operand rd, Operand rn, uint immb, uint immh, uint q)
        {
            WriteInstruction(0x2f006400u | (immb << 16) | (immh << 19) | (q << 30), rd, rn);
        }

        public readonly void SqshlImmS(Operand rd, Operand rn, uint immb, uint immh)
        {
            WriteInstruction(0x5f007400u | (immb << 16) | (immh << 19), rd, rn);
        }

        public readonly void SqshlImmV(Operand rd, Operand rn, uint immb, uint immh, uint q)
        {
            WriteInstruction(0x0f007400u | (immb << 16) | (immh << 19) | (q << 30), rd, rn);
        }

        public readonly void SqshlRegS(Operand rd, Operand rn, Operand rm, uint size)
        {
            WriteInstructionRm16(0x5e204c00u | (size << 22), rd, rn, rm);
        }

        public readonly void SqshlRegV(Operand rd, Operand rn, Operand rm, uint size, uint q)
        {
            WriteInstructionRm16(0x0e204c00u | (size << 22) | (q << 30), rd, rn, rm);
        }

        public readonly void SqshrnS(Operand rd, Operand rn, uint immb, uint immh)
        {
            WriteInstruction(0x5f009400u | (immb << 16) | (immh << 19), rd, rn);
        }

        public readonly void SqshrnV(Operand rd, Operand rn, uint immb, uint immh, uint q)
        {
            WriteInstruction(0x0f009400u | (immb << 16) | (immh << 19) | (q << 30), rd, rn);
        }

        public readonly void SqshrunS(Operand rd, Operand rn, uint immb, uint immh)
        {
            WriteInstruction(0x7f008400u | (immb << 16) | (immh << 19), rd, rn);
        }

        public readonly void SqshrunV(Operand rd, Operand rn, uint immb, uint immh, uint q)
        {
            WriteInstruction(0x2f008400u | (immb << 16) | (immh << 19) | (q << 30), rd, rn);
        }

        public readonly void SqsubS(Operand rd, Operand rn, Operand rm, uint size)
        {
            WriteInstructionRm16(0x5e202c00u | (size << 22), rd, rn, rm);
        }

        public readonly void SqsubV(Operand rd, Operand rn, Operand rm, uint size, uint q)
        {
            WriteInstructionRm16(0x0e202c00u | (size << 22) | (q << 30), rd, rn, rm);
        }

        public readonly void SqxtnS(Operand rd, Operand rn, uint size)
        {
            WriteInstruction(0x5e214800u | (size << 22), rd, rn);
        }

        public readonly void SqxtnV(Operand rd, Operand rn, uint size, uint q)
        {
            WriteInstruction(0x0e214800u | (size << 22) | (q << 30), rd, rn);
        }

        public readonly void SqxtunS(Operand rd, Operand rn, uint size)
        {
            WriteInstruction(0x7e212800u | (size << 22), rd, rn);
        }

        public readonly void SqxtunV(Operand rd, Operand rn, uint size, uint q)
        {
            WriteInstruction(0x2e212800u | (size << 22) | (q << 30), rd, rn);
        }

        public readonly void Srhadd(Operand rd, Operand rn, Operand rm, uint size, uint q)
        {
            WriteInstructionRm16(0x0e201400u | (size << 22) | (q << 30), rd, rn, rm);
        }

        public readonly void SriS(Operand rd, Operand rn, uint immb, uint immh)
        {
            WriteInstruction(0x7f004400u | (immb << 16) | (immh << 19), rd, rn);
        }

        public readonly void SriV(Operand rd, Operand rn, uint immb, uint immh, uint q)
        {
            WriteInstruction(0x2f004400u | (immb << 16) | (immh << 19) | (q << 30), rd, rn);
        }

        public readonly void SrshlS(Operand rd, Operand rn, Operand rm, uint size)
        {
            WriteInstructionRm16(0x5e205400u | (size << 22), rd, rn, rm);
        }

        public readonly void SrshlV(Operand rd, Operand rn, Operand rm, uint size, uint q)
        {
            WriteInstructionRm16(0x0e205400u | (size << 22) | (q << 30), rd, rn, rm);
        }

        public readonly void SrshrS(Operand rd, Operand rn, uint immb, uint immh)
        {
            WriteInstruction(0x5f002400u | (immb << 16) | (immh << 19), rd, rn);
        }

        public readonly void SrshrV(Operand rd, Operand rn, uint immb, uint immh, uint q)
        {
            WriteInstruction(0x0f002400u | (immb << 16) | (immh << 19) | (q << 30), rd, rn);
        }

        public readonly void SrsraS(Operand rd, Operand rn, uint immb, uint immh)
        {
            WriteInstruction(0x5f003400u | (immb << 16) | (immh << 19), rd, rn);
        }

        public readonly void SrsraV(Operand rd, Operand rn, uint immb, uint immh, uint q)
        {
            WriteInstruction(0x0f003400u | (immb << 16) | (immh << 19) | (q << 30), rd, rn);
        }

        public readonly void Sshll(Operand rd, Operand rn, uint immb, uint immh, uint q)
        {
            WriteInstruction(0x0f00a400u | (immb << 16) | (immh << 19) | (q << 30), rd, rn);
        }

        public readonly void SshlS(Operand rd, Operand rn, Operand rm, uint size)
        {
            WriteInstructionRm16(0x5e204400u | (size << 22), rd, rn, rm);
        }

        public readonly void SshlV(Operand rd, Operand rn, Operand rm, uint size, uint q)
        {
            WriteInstructionRm16(0x0e204400u | (size << 22) | (q << 30), rd, rn, rm);
        }

        public readonly void SshrS(Operand rd, Operand rn, uint immb, uint immh)
        {
            WriteInstruction(0x5f000400u | (immb << 16) | (immh << 19), rd, rn);
        }

        public readonly void SshrV(Operand rd, Operand rn, uint immb, uint immh, uint q)
        {
            WriteInstruction(0x0f000400u | (immb << 16) | (immh << 19) | (q << 30), rd, rn);
        }

        public readonly void SsraS(Operand rd, Operand rn, uint immb, uint immh)
        {
            WriteInstruction(0x5f001400u | (immb << 16) | (immh << 19), rd, rn);
        }

        public readonly void SsraV(Operand rd, Operand rn, uint immb, uint immh, uint q)
        {
            WriteInstruction(0x0f001400u | (immb << 16) | (immh << 19) | (q << 30), rd, rn);
        }

        public readonly void Ssubl(Operand rd, Operand rn, Operand rm, uint size, uint q)
        {
            WriteInstructionRm16(0x0e202000u | (size << 22) | (q << 30), rd, rn, rm);
        }

        public readonly void Ssubw(Operand rd, Operand rn, Operand rm, uint size, uint q)
        {
            WriteInstructionRm16(0x0e203000u | (size << 22) | (q << 30), rd, rn, rm);
        }

        public readonly void St1MultAsNoPostIndex(Operand rt, Operand rn, uint registersCount, uint size, uint q)
        {
            WriteInstruction(0x0c002000u | (size << 10) | (EncodeLdSt1MultOpcode(registersCount) << 12) | (q << 30), rt, rn);
        }

        public readonly void St1MultAsPostIndex(Operand rt, Operand rn, Operand rm, uint registersCount, uint size, uint q)
        {
            WriteInstructionRm16(0x0c802000u | (size << 10) | (EncodeLdSt1MultOpcode(registersCount) << 12) | (q << 30), rt, rn, rm);
        }

        public readonly void St1SnglAsNoPostIndex(Operand rt, Operand rn, uint index, uint size)
        {
            WriteInstruction(EncodeIndexSizeSngl(0x0d000000u, index, size), rt, rn);
        }

        public readonly void St1SnglAsPostIndex(Operand rt, Operand rn, Operand rm, uint index, uint size)
        {
            WriteInstructionRm16(EncodeIndexSizeSngl(0x0d800000u, index, size), rt, rn, rm);
        }

        public readonly void St2MultAsNoPostIndex(Operand rt, Operand rn, uint size, uint q)
        {
            WriteInstruction(0x0c008000u | (size << 10) | (q << 30), rt, rn);
        }

        public readonly void St2MultAsPostIndex(Operand rt, Operand rn, Operand rm, uint size, uint q)
        {
            WriteInstructionRm16(0x0c808000u | (size << 10) | (q << 30), rt, rn, rm);
        }

        public readonly void St2SnglAsNoPostIndex(Operand rt, Operand rn, uint index, uint size)
        {
            WriteInstruction(EncodeIndexSizeSngl(0x0d200000u, index, size), rt, rn);
        }

        public readonly void St2SnglAsPostIndex(Operand rt, Operand rn, Operand rm, uint index, uint size)
        {
            WriteInstructionRm16(EncodeIndexSizeSngl(0x0da00000u, index, size), rt, rn, rm);
        }

        public readonly void St3MultAsNoPostIndex(Operand rt, Operand rn, uint size, uint q)
        {
            WriteInstruction(0x0c004000u | (size << 10) | (q << 30), rt, rn);
        }

        public readonly void St3MultAsPostIndex(Operand rt, Operand rn, Operand rm, uint size, uint q)
        {
            WriteInstructionRm16(0x0c804000u | (size << 10) | (q << 30), rt, rn, rm);
        }

        public readonly void St3SnglAsNoPostIndex(Operand rt, Operand rn, uint index, uint size)
        {
            WriteInstruction(EncodeIndexSizeSngl(0x0d002000u, index, size), rt, rn);
        }

        public readonly void St3SnglAsPostIndex(Operand rt, Operand rn, Operand rm, uint index, uint size)
        {
            WriteInstructionRm16(EncodeIndexSizeSngl(0x0d802000u, index, size), rt, rn, rm);
        }

        public readonly void St4MultAsNoPostIndex(Operand rt, Operand rn, uint size, uint q)
        {
            WriteInstruction(0x0c000000u | (size << 10) | (q << 30), rt, rn);
        }

        public readonly void St4MultAsPostIndex(Operand rt, Operand rn, Operand rm, uint size, uint q)
        {
            WriteInstructionRm16(0x0c800000u | (size << 10) | (q << 30), rt, rn, rm);
        }

        public readonly void St4SnglAsNoPostIndex(Operand rt, Operand rn, uint index, uint size)
        {
            WriteInstruction(EncodeIndexSizeSngl(0x0d202000u, index, size), rt, rn);
        }

        public readonly void St4SnglAsPostIndex(Operand rt, Operand rn, Operand rm, uint index, uint size)
        {
            WriteInstructionRm16(EncodeIndexSizeSngl(0x0da02000u, index, size), rt, rn, rm);
        }

        public readonly void Stl1Sngl(Operand rt, Operand rn, uint q)
        {
            WriteInstruction(0x0d018400u | (q << 30), rt, rn);
        }

        public readonly void Subhn(Operand rd, Operand rn, Operand rm, uint size, uint q)
        {
            WriteInstructionRm16(0x0e206000u | (size << 22) | (q << 30), rd, rn, rm);
        }

        public readonly void SubS(Operand rd, Operand rn, Operand rm, uint size)
        {
            WriteInstructionRm16(0x7e208400u | (size << 22), rd, rn, rm);
        }

        public readonly void SubV(Operand rd, Operand rn, Operand rm, uint size, uint q)
        {
            WriteInstructionRm16(0x2e208400u | (size << 22) | (q << 30), rd, rn, rm);
        }

        public readonly void SudotElt(Operand rd, Operand rn, uint h, Operand rm, uint m, uint l, uint q)
        {
            WriteInstructionRm16(0x0f00f000u | (h << 11) | (m << 20) | (l << 21) | (q << 30), rd, rn, rm);
        }

        public readonly void SuqaddS(Operand rd, Operand rn, uint size)
        {
            WriteInstruction(0x5e203800u | (size << 22), rd, rn);
        }

        public readonly void SuqaddV(Operand rd, Operand rn, uint size, uint q)
        {
            WriteInstruction(0x0e203800u | (size << 22) | (q << 30), rd, rn);
        }

        public readonly void Tbl(Operand rd, Operand rn, uint len, Operand rm, uint q)
        {
            WriteInstructionRm16(0x0e000000u | (len << 13) | (q << 30), rd, rn, rm);
        }

        public readonly void Tbx(Operand rd, Operand rn, uint len, Operand rm, uint q)
        {
            WriteInstructionRm16(0x0e001000u | (len << 13) | (q << 30), rd, rn, rm);
        }

        public readonly void Trn1(Operand rd, Operand rn, Operand rm, uint size, uint q)
        {
            WriteInstructionRm16(0x0e002800u | (size << 22) | (q << 30), rd, rn, rm);
        }

        public readonly void Trn2(Operand rd, Operand rn, Operand rm, uint size, uint q)
        {
            WriteInstructionRm16(0x0e006800u | (size << 22) | (q << 30), rd, rn, rm);
        }

        public readonly void Uabal(Operand rd, Operand rn, Operand rm, uint size, uint q)
        {
            WriteInstructionRm16(0x2e205000u | (size << 22) | (q << 30), rd, rn, rm);
        }

        public readonly void Uaba(Operand rd, Operand rn, Operand rm, uint size, uint q)
        {
            WriteInstructionRm16(0x2e207c00u | (size << 22) | (q << 30), rd, rn, rm);
        }

        public readonly void Uabdl(Operand rd, Operand rn, Operand rm, uint size, uint q)
        {
            WriteInstructionRm16(0x2e207000u | (size << 22) | (q << 30), rd, rn, rm);
        }

        public readonly void Uabd(Operand rd, Operand rn, Operand rm, uint size, uint q)
        {
            WriteInstructionRm16(0x2e207400u | (size << 22) | (q << 30), rd, rn, rm);
        }

        public readonly void Uadalp(Operand rd, Operand rn, uint size, uint q)
        {
            WriteInstruction(0x2e206800u | (size << 22) | (q << 30), rd, rn);
        }

        public readonly void Uaddlp(Operand rd, Operand rn, uint size, uint q)
        {
            WriteInstruction(0x2e202800u | (size << 22) | (q << 30), rd, rn);
        }

        public readonly void Uaddlv(Operand rd, Operand rn, uint size, uint q)
        {
            WriteInstruction(0x2e303800u | (size << 22) | (q << 30), rd, rn);
        }

        public readonly void Uaddl(Operand rd, Operand rn, Operand rm, uint size, uint q)
        {
            WriteInstructionRm16(0x2e200000u | (size << 22) | (q << 30), rd, rn, rm);
        }

        public readonly void Uaddw(Operand rd, Operand rn, Operand rm, uint size, uint q)
        {
            WriteInstructionRm16(0x2e201000u | (size << 22) | (q << 30), rd, rn, rm);
        }

        public readonly void UcvtfFixS(Operand rd, Operand rn, uint immb, uint immh)
        {
            WriteInstruction(0x7f00e400u | (immb << 16) | (immh << 19), rd, rn);
        }

        public readonly void UcvtfFixV(Operand rd, Operand rn, uint immb, uint immh, uint q)
        {
            WriteInstruction(0x2f00e400u | (immb << 16) | (immh << 19) | (q << 30), rd, rn);
        }

        public readonly void UcvtfIntSH(Operand rd, Operand rn)
        {
            WriteInstruction(0x7e79d800u, rd, rn);
        }

        public readonly void UcvtfIntS(Operand rd, Operand rn, uint sz)
        {
            WriteInstruction(0x7e21d800u | (sz << 22), rd, rn);
        }

        public readonly void UcvtfIntVH(Operand rd, Operand rn, uint q)
        {
            WriteInstruction(0x2e79d800u | (q << 30), rd, rn);
        }

        public readonly void UcvtfIntV(Operand rd, Operand rn, uint sz, uint q)
        {
            WriteInstruction(0x2e21d800u | (sz << 22) | (q << 30), rd, rn);
        }

        public readonly void UcvtfFloatFix(Operand rd, Operand rn, uint scale, uint ftype, uint sf)
        {
            WriteInstruction(0x1e030000u | (scale << 10) | (ftype << 22) | (sf << 31), rd, rn);
        }

        public readonly void UcvtfFloatInt(Operand rd, Operand rn, uint ftype, uint sf)
        {
            WriteInstruction(0x1e230000u | (ftype << 22) | (sf << 31), rd, rn);
        }

        public readonly void UdotElt(Operand rd, Operand rn, uint h, Operand rm, uint m, uint l, uint size, uint q)
        {
            WriteInstructionRm16(0x2f00e000u | (h << 11) | (m << 20) | (l << 21) | (size << 22) | (q << 30), rd, rn, rm);
        }

        public readonly void UdotVec(Operand rd, Operand rn, Operand rm, uint size, uint q)
        {
            WriteInstructionRm16(0x2e009400u | (size << 22) | (q << 30), rd, rn, rm);
        }

        public readonly void Uhadd(Operand rd, Operand rn, Operand rm, uint size, uint q)
        {
            WriteInstructionRm16(0x2e200400u | (size << 22) | (q << 30), rd, rn, rm);
        }

        public readonly void Uhsub(Operand rd, Operand rn, Operand rm, uint size, uint q)
        {
            WriteInstructionRm16(0x2e202400u | (size << 22) | (q << 30), rd, rn, rm);
        }

        public readonly void Umaxp(Operand rd, Operand rn, Operand rm, uint size, uint q)
        {
            WriteInstructionRm16(0x2e20a400u | (size << 22) | (q << 30), rd, rn, rm);
        }

        public readonly void Umaxv(Operand rd, Operand rn, uint size, uint q)
        {
            WriteInstruction(0x2e30a800u | (size << 22) | (q << 30), rd, rn);
        }

        public readonly void Umax(Operand rd, Operand rn, Operand rm, uint size, uint q)
        {
            WriteInstructionRm16(0x2e206400u | (size << 22) | (q << 30), rd, rn, rm);
        }

        public readonly void Uminp(Operand rd, Operand rn, Operand rm, uint size, uint q)
        {
            WriteInstructionRm16(0x2e20ac00u | (size << 22) | (q << 30), rd, rn, rm);
        }

        public readonly void Uminv(Operand rd, Operand rn, uint size, uint q)
        {
            WriteInstruction(0x2e31a800u | (size << 22) | (q << 30), rd, rn);
        }

        public readonly void Umin(Operand rd, Operand rn, Operand rm, uint size, uint q)
        {
            WriteInstructionRm16(0x2e206c00u | (size << 22) | (q << 30), rd, rn, rm);
        }

        public readonly void UmlalElt(Operand rd, Operand rn, uint h, Operand rm, uint m, uint l, uint size, uint q)
        {
            WriteInstructionRm16(0x2f002000u | (h << 11) | (m << 20) | (l << 21) | (size << 22) | (q << 30), rd, rn, rm);
        }

        public readonly void UmlalVec(Operand rd, Operand rn, Operand rm, uint size, uint q)
        {
            WriteInstructionRm16(0x2e208000u | (size << 22) | (q << 30), rd, rn, rm);
        }

        public readonly void UmlslElt(Operand rd, Operand rn, uint h, Operand rm, uint m, uint l, uint size, uint q)
        {
            WriteInstructionRm16(0x2f006000u | (h << 11) | (m << 20) | (l << 21) | (size << 22) | (q << 30), rd, rn, rm);
        }

        public readonly void UmlslVec(Operand rd, Operand rn, Operand rm, uint size, uint q)
        {
            WriteInstructionRm16(0x2e20a000u | (size << 22) | (q << 30), rd, rn, rm);
        }

        public readonly void UmmlaVec(Operand rd, Operand rn, Operand rm)
        {
            WriteInstructionRm16(0x6e80a400u, rd, rn, rm);
        }

        public readonly void Umov(Operand rd, Operand rn, int index, int size)
        {
            uint q = size == 3 ? 1u << 30 : 0u;
            WriteInstruction(0x0e003c00u | (EncodeIndexSizeImm5(index, size) << 16) | q, rd, rn);
        }

        public readonly void UmullElt(Operand rd, Operand rn, uint h, Operand rm, uint m, uint l, uint size, uint q)
        {
            WriteInstructionRm16(0x2f00a000u | (h << 11) | (m << 20) | (l << 21) | (size << 22) | (q << 30), rd, rn, rm);
        }

        public readonly void UmullVec(Operand rd, Operand rn, Operand rm, uint size, uint q)
        {
            WriteInstructionRm16(0x2e20c000u | (size << 22) | (q << 30), rd, rn, rm);
        }

        public readonly void UqaddS(Operand rd, Operand rn, Operand rm, uint size)
        {
            WriteInstructionRm16(0x7e200c00u | (size << 22), rd, rn, rm);
        }

        public readonly void UqaddV(Operand rd, Operand rn, Operand rm, uint size, uint q)
        {
            WriteInstructionRm16(0x2e200c00u | (size << 22) | (q << 30), rd, rn, rm);
        }

        public readonly void UqrshlS(Operand rd, Operand rn, Operand rm, uint size)
        {
            WriteInstructionRm16(0x7e205c00u | (size << 22), rd, rn, rm);
        }

        public readonly void UqrshlV(Operand rd, Operand rn, Operand rm, uint size, uint q)
        {
            WriteInstructionRm16(0x2e205c00u | (size << 22) | (q << 30), rd, rn, rm);
        }

        public readonly void UqrshrnS(Operand rd, Operand rn, uint immb, uint immh)
        {
            WriteInstruction(0x7f009c00u | (immb << 16) | (immh << 19), rd, rn);
        }

        public readonly void UqrshrnV(Operand rd, Operand rn, uint immb, uint immh, uint q)
        {
            WriteInstruction(0x2f009c00u | (immb << 16) | (immh << 19) | (q << 30), rd, rn);
        }

        public readonly void UqshlImmS(Operand rd, Operand rn, uint immb, uint immh)
        {
            WriteInstruction(0x7f007400u | (immb << 16) | (immh << 19), rd, rn);
        }

        public readonly void UqshlImmV(Operand rd, Operand rn, uint immb, uint immh, uint q)
        {
            WriteInstruction(0x2f007400u | (immb << 16) | (immh << 19) | (q << 30), rd, rn);
        }

        public readonly void UqshlRegS(Operand rd, Operand rn, Operand rm, uint size)
        {
            WriteInstructionRm16(0x7e204c00u | (size << 22), rd, rn, rm);
        }

        public readonly void UqshlRegV(Operand rd, Operand rn, Operand rm, uint size, uint q)
        {
            WriteInstructionRm16(0x2e204c00u | (size << 22) | (q << 30), rd, rn, rm);
        }

        public readonly void UqshrnS(Operand rd, Operand rn, uint immb, uint immh)
        {
            WriteInstruction(0x7f009400u | (immb << 16) | (immh << 19), rd, rn);
        }

        public readonly void UqshrnV(Operand rd, Operand rn, uint immb, uint immh, uint q)
        {
            WriteInstruction(0x2f009400u | (immb << 16) | (immh << 19) | (q << 30), rd, rn);
        }

        public readonly void UqsubS(Operand rd, Operand rn, Operand rm, uint size)
        {
            WriteInstructionRm16(0x7e202c00u | (size << 22), rd, rn, rm);
        }

        public readonly void UqsubV(Operand rd, Operand rn, Operand rm, uint size, uint q)
        {
            WriteInstructionRm16(0x2e202c00u | (size << 22) | (q << 30), rd, rn, rm);
        }

        public readonly void UqxtnS(Operand rd, Operand rn, uint size)
        {
            WriteInstruction(0x7e214800u | (size << 22), rd, rn);
        }

        public readonly void UqxtnV(Operand rd, Operand rn, uint size, uint q)
        {
            WriteInstruction(0x2e214800u | (size << 22) | (q << 30), rd, rn);
        }

        public readonly void Urecpe(Operand rd, Operand rn, uint sz, uint q)
        {
            WriteInstruction(0x0ea1c800u | (sz << 22) | (q << 30), rd, rn);
        }

        public readonly void Urhadd(Operand rd, Operand rn, Operand rm, uint size, uint q)
        {
            WriteInstructionRm16(0x2e201400u | (size << 22) | (q << 30), rd, rn, rm);
        }

        public readonly void UrshlS(Operand rd, Operand rn, Operand rm, uint size)
        {
            WriteInstructionRm16(0x7e205400u | (size << 22), rd, rn, rm);
        }

        public readonly void UrshlV(Operand rd, Operand rn, Operand rm, uint size, uint q)
        {
            WriteInstructionRm16(0x2e205400u | (size << 22) | (q << 30), rd, rn, rm);
        }

        public readonly void UrshrS(Operand rd, Operand rn, uint immb, uint immh)
        {
            WriteInstruction(0x7f002400u | (immb << 16) | (immh << 19), rd, rn);
        }

        public readonly void UrshrV(Operand rd, Operand rn, uint immb, uint immh, uint q)
        {
            WriteInstruction(0x2f002400u | (immb << 16) | (immh << 19) | (q << 30), rd, rn);
        }

        public readonly void Ursqrte(Operand rd, Operand rn, uint sz, uint q)
        {
            WriteInstruction(0x2ea1c800u | (sz << 22) | (q << 30), rd, rn);
        }

        public readonly void UrsraS(Operand rd, Operand rn, uint immb, uint immh)
        {
            WriteInstruction(0x7f003400u | (immb << 16) | (immh << 19), rd, rn);
        }

        public readonly void UrsraV(Operand rd, Operand rn, uint immb, uint immh, uint q)
        {
            WriteInstruction(0x2f003400u | (immb << 16) | (immh << 19) | (q << 30), rd, rn);
        }

        public readonly void UsdotElt(Operand rd, Operand rn, uint h, Operand rm, uint m, uint l, uint q)
        {
            WriteInstructionRm16(0x0f80f000u | (h << 11) | (m << 20) | (l << 21) | (q << 30), rd, rn, rm);
        }

        public readonly void UsdotVec(Operand rd, Operand rn, Operand rm, uint q)
        {
            WriteInstructionRm16(0x0e809c00u | (q << 30), rd, rn, rm);
        }

        public readonly void Ushll(Operand rd, Operand rn, uint immb, uint immh, uint q)
        {
            WriteInstruction(0x2f00a400u | (immb << 16) | (immh << 19) | (q << 30), rd, rn);
        }

        public readonly void UshlS(Operand rd, Operand rn, Operand rm, uint size)
        {
            WriteInstructionRm16(0x7e204400u | (size << 22), rd, rn, rm);
        }

        public readonly void UshlV(Operand rd, Operand rn, Operand rm, uint size, uint q)
        {
            WriteInstructionRm16(0x2e204400u | (size << 22) | (q << 30), rd, rn, rm);
        }

        public readonly void UshrS(Operand rd, Operand rn, uint immb, uint immh)
        {
            WriteInstruction(0x7f000400u | (immb << 16) | (immh << 19), rd, rn);
        }

        public readonly void UshrV(Operand rd, Operand rn, uint immb, uint immh, uint q)
        {
            WriteInstruction(0x2f000400u | (immb << 16) | (immh << 19) | (q << 30), rd, rn);
        }

        public readonly void UsmmlaVec(Operand rd, Operand rn, Operand rm)
        {
            WriteInstructionRm16(0x4e80ac00u, rd, rn, rm);
        }

        public readonly void UsqaddS(Operand rd, Operand rn, uint size)
        {
            WriteInstruction(0x7e203800u | (size << 22), rd, rn);
        }

        public readonly void UsqaddV(Operand rd, Operand rn, uint size, uint q)
        {
            WriteInstruction(0x2e203800u | (size << 22) | (q << 30), rd, rn);
        }

        public readonly void UsraS(Operand rd, Operand rn, uint immb, uint immh)
        {
            WriteInstruction(0x7f001400u | (immb << 16) | (immh << 19), rd, rn);
        }

        public readonly void UsraV(Operand rd, Operand rn, uint immb, uint immh, uint q)
        {
            WriteInstruction(0x2f001400u | (immb << 16) | (immh << 19) | (q << 30), rd, rn);
        }

        public readonly void Usubl(Operand rd, Operand rn, Operand rm, uint size, uint q)
        {
            WriteInstructionRm16(0x2e202000u | (size << 22) | (q << 30), rd, rn, rm);
        }

        public readonly void Usubw(Operand rd, Operand rn, Operand rm, uint size, uint q)
        {
            WriteInstructionRm16(0x2e203000u | (size << 22) | (q << 30), rd, rn, rm);
        }

        public readonly void Uzp1(Operand rd, Operand rn, Operand rm, uint size, uint q)
        {
            WriteInstructionRm16(0x0e001800u | (size << 22) | (q << 30), rd, rn, rm);
        }

        public readonly void Uzp2(Operand rd, Operand rn, Operand rm, uint size, uint q)
        {
            WriteInstructionRm16(0x0e005800u | (size << 22) | (q << 30), rd, rn, rm);
        }

        public readonly void Wfe()
        {
            WriteUInt32(0xd503205fu);
        }

        public readonly void Wfi()
        {
            WriteUInt32(0xd503207fu);
        }

        public readonly void Xar(Operand rd, Operand rn, uint imm6, Operand rm)
        {
            WriteInstructionRm16(0xce800000u | (imm6 << 10), rd, rn, rm);
        }

        public readonly void Xtn(Operand rd, Operand rn, uint size, uint q)
        {
            WriteInstruction(0x0e212800u | (size << 22) | (q << 30), rd, rn);
        }

        public readonly void Yield()
        {
            WriteUInt32(0xd503203fu);
        }

        public readonly void Zip1(Operand rd, Operand rn, Operand rm, uint size, uint q)
        {
            WriteInstructionRm16(0x0e003800u | (size << 22) | (q << 30), rd, rn, rm);
        }

        public readonly void Zip2(Operand rd, Operand rn, Operand rm, uint size, uint q)
        {
            WriteInstructionRm16(0x0e007800u | (size << 22) | (q << 30), rd, rn, rm);
        }

        // Utility

        public readonly void WriteSysRegInstruction(uint inst, Operand rt, uint o0, uint op1, uint crn, uint crm, uint op2)
        {
            inst |= (op2 & 7) << 5;
            inst |= (crm & 15) << 8;
            inst |= (crn & 15) << 12;
            inst |= (op1 & 7) << 16;
            inst |= (o0 & 1) << 19;

            WriteInstruction(inst, rt);
        }

        private readonly void WriteInstructionAuto(
            uint instI,
            uint instR,
            Operand rd,
            Operand rn,
            Operand rm,
            ArmShiftType shiftType = ArmShiftType.Lsl,
            int shiftAmount = 0,
            bool immForm = false)
        {
            if (rm.Kind == OperandKind.Constant && (rm.Value != 0 || immForm))
            {
                Debug.Assert(shiftAmount == 0);
                int imm = rm.AsInt32();
                Debug.Assert((uint)imm == rm.Value);
                if (imm != 0 && (imm & 0xfff) == 0)
                {
                    instI |= 1 << 22; // sh flag
                    imm >>= 12;
                }
                WriteInstructionAuto(instI | (EncodeUImm12(imm, 0) << 10), rd, rn);
            }
            else
            {
                Debug.Assert((uint)shiftType < 3); // ROR shift is a reserved encoding for ADD and SUB.
                instR |= EncodeUImm6(shiftAmount) << 10;
                instR |= (uint)shiftType << 22;

                WriteInstructionRm16Auto(instR, rd, rn, rm);
            }
        }

        private readonly void WriteInstructionAuto(
            uint instR,
            Operand rd,
            Operand rn,
            Operand rm,
            ArmShiftType shiftType = ArmShiftType.Lsl,
            int shiftAmount = 0)
        {
            instR |= EncodeUImm6(shiftAmount) << 10;
            instR |= (uint)shiftType << 22;

            WriteInstructionRm16Auto(instR, rd, rn, rm);
        }

        private readonly void WriteInstructionAuto(
            uint instruction,
            Operand rd,
            Operand rn,
            Operand rm,
            ArmExtensionType extensionType,
            int shiftAmount = 0)
        {
            Debug.Assert((uint)shiftAmount <= 4);

            instruction |= (uint)shiftAmount << 10;
            instruction |= (uint)extensionType << 13;

            WriteInstructionRm16Auto(instruction, rd, rn, rm);
        }

        private readonly void WriteInstructionBitwiseAuto(
            uint instI,
            uint instR,
            Operand rd,
            Operand rn,
            Operand rm,
            ArmShiftType shiftType = ArmShiftType.Lsl,
            int shiftAmount = 0)
        {
            if (rm.Kind == OperandKind.Constant && rm.Value != 0)
            {
                Debug.Assert(shiftAmount == 0);
                bool canEncode = CodeGenCommon.TryEncodeBitMask(rm, out int immN, out int immS, out int immR);
                Debug.Assert(canEncode);
                uint instruction = instI | ((uint)immS << 10) | ((uint)immR << 16) | ((uint)immN << 22);

                WriteInstructionAuto(instruction, rd, rn);
            }
            else
            {
                WriteInstructionBitwiseAuto(instR, rd, rn, rm, shiftType, shiftAmount);
            }
        }

        private readonly void WriteInstructionBitwiseAuto(
            uint instruction,
            Operand rd,
            Operand rn,
            Operand rm,
            ArmShiftType shiftType = ArmShiftType.Lsl,
            int shiftAmount = 0)
        {
            if (rd.Type == OperandType.I64)
            {
                instruction |= SfFlag;
            }

            instruction |= EncodeUImm6(shiftAmount) << 10;
            instruction |= (uint)shiftType << 22;

            WriteInstructionRm16(instruction, rd, rn, rm);
        }

        private readonly void WriteInstructionLdrStrAuto(
            uint instruction,
            Operand rd,
            Operand rn,
            Operand rm,
            ArmExtensionType extensionType,
            bool shift)
        {
            if (shift)
            {
                instruction |= 1u << 12;
            }

            instruction |= (uint)extensionType << 13;

            if (rd.Type == OperandType.I64)
            {
                instruction |= 1u << 30;
            }

            WriteInstructionRm16(instruction, rd, rn, rm);
        }

        private readonly void WriteInstructionAuto(uint instruction, Operand rd)
        {
            if (rd.Type == OperandType.I64)
            {
                instruction |= SfFlag;
            }

            WriteInstruction(instruction, rd);
        }

        public readonly void WriteInstructionAuto(uint instruction, Operand rd, Operand rn)
        {
            if (rd.Type == OperandType.I64)
            {
                instruction |= SfFlag;
            }

            WriteInstruction(instruction, rd, rn);
        }

        private readonly void WriteInstructionAuto(uint instruction, Operand rd, Operand rn, Operand rm, Operand ra)
        {
            if (rd.Type == OperandType.I64)
            {
                instruction |= SfFlag;
            }

            WriteInstruction(instruction, rd, rn, rm, ra);
        }

        public readonly void WriteInstruction(uint instruction, Operand rd)
        {
            WriteUInt32(instruction | EncodeReg(rd));
        }

        public readonly void WriteInstruction(uint instruction, Operand rd, Operand rn)
        {
            WriteUInt32(instruction | EncodeReg(rd) | (EncodeReg(rn) << 5));
        }

        public readonly void WriteInstruction(uint instruction, Operand rd, Operand rn, Operand rm)
        {
            WriteUInt32(instruction | EncodeReg(rd) | (EncodeReg(rn) << 5) | (EncodeReg(rm) << 10));
        }

        public readonly void WriteInstruction(uint instruction, Operand rd, Operand rn, Operand rm, Operand ra)
        {
            WriteUInt32(instruction | EncodeReg(rd) | (EncodeReg(rn) << 5) | (EncodeReg(ra) << 10) | (EncodeReg(rm) << 16));
        }

        private readonly void WriteInstructionRm16Auto(uint instruction, Operand rd, Operand rn, Operand rm)
        {
            if (rd.Type == OperandType.I64)
            {
                instruction |= SfFlag;
            }

            WriteInstructionRm16(instruction, rd, rn, rm);
        }

        public readonly void WriteInstructionRm16(uint instruction, Operand rn, Operand rm)
        {
            WriteUInt32(instruction | (EncodeReg(rn) << 5) | (EncodeReg(rm) << 16));
        }

        public readonly void WriteInstructionRm16(uint instruction, Operand rd, Operand rn, Operand rm)
        {
            WriteUInt32(instruction | EncodeReg(rd) | (EncodeReg(rn) << 5) | (EncodeReg(rm) << 16));
        }

        private static uint GetLdpStpInstruction(uint intInst, uint vecInst, int imm, OperandType type)
        {
            uint instruction;
            int scale;

            if (type.IsInteger())
            {
                instruction = intInst;

                if (type == OperandType.I64)
                {
                    instruction |= SfFlag;
                    scale = 3;
                }
                else
                {
                    scale = 2;
                }
            }
            else
            {
                int opc = type switch
                {
                    OperandType.FP32 => 0,
                    OperandType.FP64 => 1,
                    _ => 2,
                };

                instruction = vecInst | ((uint)opc << 30);
                scale = 2 + opc;
            }

            instruction |= (EncodeSImm7(imm, scale) << 15);

            return instruction;
        }

        private static uint GetLdrStrInstruction(uint intInst, uint vecInst, OperandType type)
        {
            uint instruction;

            if (type.IsInteger())
            {
                instruction = intInst;

                if (type == OperandType.I64)
                {
                    instruction |= 1 << 30;
                }
            }
            else
            {
                instruction = vecInst;

                if (type == OperandType.V128)
                {
                    instruction |= 1u << 23;
                }
                else
                {
                    instruction |= type == OperandType.FP32 ? 2u << 30 : 3u << 30;
                }
            }

            return instruction;
        }

        private static uint GetLdrStrInstruction(uint intInst, uint vecInst, OperandType type, uint size)
        {
            uint instruction;

            if (type.IsInteger())
            {
                instruction = intInst;

                if (type == OperandType.I64)
                {
                    instruction |= 1 << 30;
                }
            }
            else
            {
                instruction = vecInst;

                if (type == OperandType.V128)
                {
                    instruction |= 1u << 23;
                }
                else
                {
                    instruction |= size << 30;
                }
            }

            return instruction;
        }

        private static uint EncodeIndexSizeImm5(int index, int size)
        {
            Debug.Assert((uint)size < 4);
            Debug.Assert((uint)index < (16u >> size), $"Invalid index {index} and size {size} combination.");
            return ((uint)index << (size + 1)) | (1u << size);
        }

        private static uint EncodeSImm7(int value, int scale)
        {
            uint imm = (uint)(value >> scale) & 0x7f;
            Debug.Assert(((int)imm << 25) >> (25 - scale) == value, $"Failed to encode constant 0x{value:X} with scale {scale}.");
            return imm;
        }

        private static uint EncodeSImm9(int value)
        {
            uint imm = (uint)value & 0x1ff;
            Debug.Assert(((int)imm << 23) >> 23 == value, $"Failed to encode constant 0x{value:X}.");
            return imm;
        }

        private static uint EncodeSImm19_2(int value)
        {
            uint imm = (uint)(value >> 2) & 0x7ffff;
            Debug.Assert(((int)imm << 13) >> 11 == value, $"Failed to encode constant 0x{value:X}.");
            return imm;
        }

        private static uint EncodeSImm26_2(int value)
        {
            uint imm = (uint)(value >> 2) & 0x3ffffff;
            Debug.Assert(((int)imm << 6) >> 4 == value, $"Failed to encode constant 0x{value:X}.");
            return imm;
        }

        private static uint EncodeUImm4(int value)
        {
            uint imm = (uint)value & 0xf;
            Debug.Assert((int)imm == value, $"Failed to encode constant 0x{value:X}.");
            return imm;
        }

        private static uint EncodeUImm6(int value)
        {
            uint imm = (uint)value & 0x3f;
            Debug.Assert((int)imm == value, $"Failed to encode constant 0x{value:X}.");
            return imm;
        }

        private static uint EncodeUImm12(int value, OperandType type)
        {
            return EncodeUImm12(value, GetScaleForType(type));
        }

        private static uint EncodeUImm12(int value, int scale)
        {
            uint imm = (uint)(value >> scale) & 0xfff;
            Debug.Assert((int)imm << scale == value, $"Failed to encode constant 0x{value:X} with scale {scale}.");
            return imm;
        }

        private static uint EncodeUImm16(int value)
        {
            uint imm = (uint)value & 0xffff;
            Debug.Assert((int)imm == value, $"Failed to encode constant 0x{value:X}.");
            return imm;
        }

        private static uint EncodeReg(Operand reg)
        {
            if (reg.Kind == OperandKind.Constant && reg.Value == 0)
            {
                return ZrRegister;
            }

            uint regIndex = (uint)reg.GetRegister().Index;
            Debug.Assert(reg.Kind == OperandKind.Register);
            Debug.Assert(regIndex < 32);
            return regIndex;
        }

        private static uint EncodeTypeTargetPolicy(uint type, uint target, uint policy)
        {
            Debug.Assert(type <= 2);
            Debug.Assert(target <= 2);
            Debug.Assert(policy <= 1);

            return (type << 3) | (target << 1) | policy;
        }

        private static uint EncodeIndexSizeSngl(uint inst, uint index, uint size)
        {
            uint opcode = Math.Min(2u, size) << 1;

            index <<= (int)size;

            if (size == 3)
            {
                index |= 1u;
            }

            size = index & 3;
            uint s = (index >> 2) & 1;
            uint q = index >> 3;

            Debug.Assert(q <= 1);

            return inst | (size << 10) | (s << 12) | (opcode << 13) | (q << 30);
        }

        private static uint EncodeLdSt1MultOpcode(uint registersCount)
        {
            return registersCount switch
            {
                2 => 0b1010,
                3 => 0b0110,
                4 => 0b0010,
                _ => 0b0111,
            };
        }

        private static int GetScaleForType(OperandType type)
        {
            return type switch
            {
                OperandType.I32 => 2,
                OperandType.I64 => 3,
                OperandType.FP32 => 2,
                OperandType.FP64 => 3,
                OperandType.V128 => 4,
                _ => throw new ArgumentException($"Invalid type {type}."),
            };
        }

        private readonly void WriteUInt32(uint value)
        {
            _code.Add(value);
        }
    }
}
