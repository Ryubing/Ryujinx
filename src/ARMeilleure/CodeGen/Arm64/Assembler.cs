using ARMeilleure.IntermediateRepresentation;
using System;
using System.Diagnostics;
using System.IO;
using static ARMeilleure.IntermediateRepresentation.Operand;

namespace ARMeilleure.CodeGen.Arm64
{
    class Assembler
    {
        public const uint SfFlag = 1u << 31;

        private const int SpRegister = 31;
        private const int ZrRegister = 31;

        private readonly Stream _stream;

        public Assembler(Stream stream)
        {
            _stream = stream;
        }

        public void Add(Operand rd, Operand rn, Operand rm, ArmExtensionType extensionType, int shiftAmount = 0)
        {
            WriteInstructionAuto(0x0b200000u, rd, rn, rm, extensionType, shiftAmount);
        }

        public void Add(Operand rd, Operand rn, Operand rm, ArmShiftType shiftType = ArmShiftType.Lsl, int shiftAmount = 0, bool immForm = false)
        {
            WriteInstructionAuto(0x11000000u, 0x0b000000u, rd, rn, rm, shiftType, shiftAmount, immForm);
        }

        public void And(Operand rd, Operand rn, Operand rm, ArmShiftType shiftType = ArmShiftType.Lsl, int shiftAmount = 0)
        {
            WriteInstructionBitwiseAuto(0x12000000u, 0x0a000000u, rd, rn, rm, shiftType, shiftAmount);
        }

        public void Ands(Operand rd, Operand rn, Operand rm, ArmShiftType shiftType = ArmShiftType.Lsl, int shiftAmount = 0)
        {
            WriteInstructionBitwiseAuto(0x72000000u, 0x6a000000u, rd, rn, rm, shiftType, shiftAmount);
        }

        public void Asr(Operand rd, Operand rn, Operand rm)
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

        public void Asrv(Operand rd, Operand rn, Operand rm)
        {
            WriteInstructionBitwiseAuto(0x1ac02800u, rd, rn, rm);
        }

        public void B(int imm)
        {
            WriteUInt32(0x14000000u | EncodeSImm26_2(imm));
        }

        public void B(ArmCondition condition, int imm)
        {
            WriteUInt32(0x54000000u | (uint)condition | (EncodeSImm19_2(imm) << 5));
        }

        public void Blr(Operand rn)
        {
            WriteUInt32(0xd63f0000u | (EncodeReg(rn) << 5));
        }

        public void Br(Operand rn)
        {
            WriteUInt32(0xd61f0000u | (EncodeReg(rn) << 5));
        }

        public void Brk()
        {
            WriteUInt32(0xd4200000u);
        }

        public void Cbz(Operand rt, int imm)
        {
            WriteInstructionAuto(0x34000000u | (EncodeSImm19_2(imm) << 5), rt);
        }

        public void Cbnz(Operand rt, int imm)
        {
            WriteInstructionAuto(0x35000000u | (EncodeSImm19_2(imm) << 5), rt);
        }

        public void Clrex(int crm = 15)
        {
            WriteUInt32(0xd503305fu | (EncodeUImm4(crm) << 8));
        }

        public void Clz(Operand rd, Operand rn)
        {
            WriteInstructionAuto(0x5ac01000u, rd, rn);
        }

        public void CmeqVector(Operand rd, Operand rn, Operand rm, int size, bool q = true)
        {
            Debug.Assert((uint)size < 4);
            WriteSimdInstruction(0x2e208c00u | ((uint)size << 22), rd, rn, rm, q);
        }

        public void Cmp(Operand rn, Operand rm, ArmShiftType shiftType = ArmShiftType.Lsl, int shiftAmount = 0)
        {
            Subs(Factory.Register(ZrRegister, RegisterType.Integer, rn.Type), rn, rm, shiftType, shiftAmount);
        }

        public void Csel(Operand rd, Operand rn, Operand rm, ArmCondition condition)
        {
            WriteInstructionBitwiseAuto(0x1a800000u | ((uint)condition << 12), rd, rn, rm);
        }

        public void Cset(Operand rd, ArmCondition condition)
        {
            Operand zr = Factory.Register(ZrRegister, RegisterType.Integer, rd.Type);
            Csinc(rd, zr, zr, (ArmCondition)((int)condition ^ 1));
        }

        public void Csinc(Operand rd, Operand rn, Operand rm, ArmCondition condition)
        {
            WriteInstructionBitwiseAuto(0x1a800400u | ((uint)condition << 12), rd, rn, rm);
        }

        public void Dmb(uint option)
        {
            WriteUInt32(0xd50330bfu | (option << 8));
        }

        public void DupScalar(Operand rd, Operand rn, int index, int size)
        {
            WriteInstruction(0x5e000400u | (EncodeIndexSizeImm5(index, size) << 16), rd, rn);
        }

        public void Eor(Operand rd, Operand rn, Operand rm, ArmShiftType shiftType = ArmShiftType.Lsl, int shiftAmount = 0)
        {
            WriteInstructionBitwiseAuto(0x52000000u, 0x4a000000u, rd, rn, rm, shiftType, shiftAmount);
        }

        public void EorVector(Operand rd, Operand rn, Operand rm, bool q = true)
        {
            WriteSimdInstruction(0x2e201c00u, rd, rn, rm, q);
        }

        public void Extr(Operand rd, Operand rn, Operand rm, int imms)
        {
            uint n = rd.Type == OperandType.I64 ? 1u << 22 : 0u;
            WriteInstructionBitwiseAuto(0x13800000u | n | (EncodeUImm6(imms) << 10), rd, rn, rm);
        }

        public void FaddScalar(Operand rd, Operand rn, Operand rm)
        {
            WriteFPInstructionAuto(0x1e202800u, rd, rn, rm);
        }

        public void FcvtScalar(Operand rd, Operand rn)
        {
            uint instruction = 0x1e224000u | (rd.Type == OperandType.FP64 ? 1u << 15 : 1u << 22);
            WriteUInt32(instruction | EncodeReg(rd) | (EncodeReg(rn) << 5));
        }

        public void FdivScalar(Operand rd, Operand rn, Operand rm)
        {
            WriteFPInstructionAuto(0x1e201800u, rd, rn, rm);
        }

        public void Fmov(Operand rd, Operand rn)
        {
            WriteFPInstructionAuto(0x1e204000u, rd, rn);
        }

        public void Fmov(Operand rd, Operand rn, bool topHalf)
        {
            Debug.Assert(rd.Type.IsInteger() != rn.Type.IsInteger());
            Debug.Assert(rd.Type == OperandType.I64 || rn.Type == OperandType.I64 || !topHalf);

            uint opcode = rd.Type.IsInteger() ? 0b110u : 0b111u;

            uint rmode = topHalf ? 1u << 19 : 0u;
            uint ftype = rd.Type == OperandType.FP64 || rn.Type == OperandType.FP64 ? 1u << 22 : 0u;
            uint sf = rd.Type == OperandType.I64 || rn.Type == OperandType.I64 ? SfFlag : 0u;

            WriteUInt32(0x1e260000u | (opcode << 16) | rmode | ftype | sf | EncodeReg(rd) | (EncodeReg(rn) << 5));
        }

        public void FmulScalar(Operand rd, Operand rn, Operand rm)
        {
            WriteFPInstructionAuto(0x1e200800u, rd, rn, rm);
        }

        public void FnegScalar(Operand rd, Operand rn)
        {
            WriteFPInstructionAuto(0x1e214000u, rd, rn);
        }

        public void FsubScalar(Operand rd, Operand rn, Operand rm)
        {
            WriteFPInstructionAuto(0x1e203800u, rd, rn, rm);
        }

        public void Ins(Operand rd, Operand rn, int index, int size)
        {
            WriteInstruction(0x4e001c00u | (EncodeIndexSizeImm5(index, size) << 16), rd, rn);
        }

        public void Ins(Operand rd, Operand rn, int srcIndex, int dstIndex, int size)
        {
            uint imm4 = (uint)srcIndex << size;
            Debug.Assert((uint)srcIndex < (16u >> size));
            WriteInstruction(0x6e000400u | (imm4 << 11) | (EncodeIndexSizeImm5(dstIndex, size) << 16), rd, rn);
        }

        public void Ldaxp(Operand rt, Operand rt2, Operand rn)
        {
            WriteInstruction(0x887f8000u | ((rt.Type == OperandType.I64 ? 3u : 2u) << 30), rt, rn, rt2);
        }

        public void Ldaxr(Operand rt, Operand rn)
        {
            WriteInstruction(0x085ffc00u | ((rt.Type == OperandType.I64 ? 3u : 2u) << 30), rt, rn);
        }

        public void Ldaxrb(Operand rt, Operand rn)
        {
            WriteInstruction(0x085ffc00u, rt, rn);
        }

        public void Ldaxrh(Operand rt, Operand rn)
        {
            WriteInstruction(0x085ffc00u | (1u << 30), rt, rn);
        }

        public void LdpRiPost(Operand rt, Operand rt2, Operand rn, int imm)
        {
            uint instruction = GetLdpStpInstruction(0x28c00000u, 0x2cc00000u, imm, rt.Type);
            WriteInstruction(instruction, rt, rn, rt2);
        }

        public void LdpRiPre(Operand rt, Operand rt2, Operand rn, int imm)
        {
            uint instruction = GetLdpStpInstruction(0x29c00000u, 0x2dc00000u, imm, rt.Type);
            WriteInstruction(instruction, rt, rn, rt2);
        }

        public void LdpRiUn(Operand rt, Operand rt2, Operand rn, int imm)
        {
            uint instruction = GetLdpStpInstruction(0x29400000u, 0x2d400000u, imm, rt.Type);
            WriteInstruction(instruction, rt, rn, rt2);
        }

        public void Ldr(Operand rt, Operand rn)
        {
            if (rn.Kind == OperandKind.Memory)
            {
                MemoryOperand memOp = rn.GetMemory();

                if (memOp.Index != default)
                {
                    Debug.Assert(memOp.Displacement == 0);
                    Debug.Assert(memOp.Scale == Multiplier.x1 || (int)memOp.Scale == GetScaleForType(rt.Type));
                    LdrRr(rt, memOp.BaseAddress, memOp.Index, ArmExtensionType.Uxtx, memOp.Scale != Multiplier.x1);
                }
                else
                {
                    LdrRiUn(rt, memOp.BaseAddress, memOp.Displacement);
                }
            }
            else
            {
                LdrRiUn(rt, rn, 0);
            }
        }

        public void LdrLit(Operand rt, int offset)
        {
            uint instruction = 0x18000000u | (EncodeSImm19_2(offset) << 5);

            if (rt.Type == OperandType.I64)
            {
                instruction |= 1u << 30;
            }

            WriteInstruction(instruction, rt);
        }

        public void LdrRiPost(Operand rt, Operand rn, int imm)
        {
            uint instruction = GetLdrStrInstruction(0xb8400400u, 0x3c400400u, rt.Type) | (EncodeSImm9(imm) << 12);
            WriteInstruction(instruction, rt, rn);
        }

        public void LdrRiPre(Operand rt, Operand rn, int imm)
        {
            uint instruction = GetLdrStrInstruction(0xb8400c00u, 0x3c400c00u, rt.Type) | (EncodeSImm9(imm) << 12);
            WriteInstruction(instruction, rt, rn);
        }

        public void LdrRiUn(Operand rt, Operand rn, int imm)
        {
            uint instruction = GetLdrStrInstruction(0xb9400000u, 0x3d400000u, rt.Type) | (EncodeUImm12(imm, rt.Type) << 10);
            WriteInstruction(instruction, rt, rn);
        }

        public void LdrRr(Operand rt, Operand rn, Operand rm, ArmExtensionType extensionType, bool shift)
        {
            uint instruction = GetLdrStrInstruction(0xb8600800u, 0x3ce00800u, rt.Type);
            WriteInstructionLdrStrAuto(instruction, rt, rn, rm, extensionType, shift);
        }

        public void LdrbRiPost(Operand rt, Operand rn, int imm)
        {
            uint instruction = 0x38400400u | (EncodeSImm9(imm) << 12);
            WriteInstruction(instruction, rt, rn);
        }

        public void LdrbRiPre(Operand rt, Operand rn, int imm)
        {
            uint instruction = 0x38400c00u | (EncodeSImm9(imm) << 12);
            WriteInstruction(instruction, rt, rn);
        }

        public void LdrbRiUn(Operand rt, Operand rn, int imm)
        {
            uint instruction = 0x39400000u | (EncodeUImm12(imm, 0) << 10);
            WriteInstruction(instruction, rt, rn);
        }

        public void LdrhRiPost(Operand rt, Operand rn, int imm)
        {
            uint instruction = 0x78400400u | (EncodeSImm9(imm) << 12);
            WriteInstruction(instruction, rt, rn);
        }

        public void LdrhRiPre(Operand rt, Operand rn, int imm)
        {
            uint instruction = 0x78400c00u | (EncodeSImm9(imm) << 12);
            WriteInstruction(instruction, rt, rn);
        }

        public void LdrhRiUn(Operand rt, Operand rn, int imm)
        {
            uint instruction = 0x79400000u | (EncodeUImm12(imm, 1) << 10);
            WriteInstruction(instruction, rt, rn);
        }

        public void Ldur(Operand rt, Operand rn, int imm)
        {
            uint instruction = GetLdrStrInstruction(0xb8400000u, 0x3c400000u, rt.Type) | (EncodeSImm9(imm) << 12);
            WriteInstruction(instruction, rt, rn);
        }

        public void Lsl(Operand rd, Operand rn, Operand rm)
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

        public void Lslv(Operand rd, Operand rn, Operand rm)
        {
            WriteInstructionBitwiseAuto(0x1ac02000u, rd, rn, rm);
        }

        public void Lsr(Operand rd, Operand rn, Operand rm)
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

        public void Lsrv(Operand rd, Operand rn, Operand rm)
        {
            WriteInstructionBitwiseAuto(0x1ac02400u, rd, rn, rm);
        }

        public void Madd(Operand rd, Operand rn, Operand rm, Operand ra)
        {
            WriteInstructionAuto(0x1b000000u, rd, rn, rm, ra);
        }

        public void Mul(Operand rd, Operand rn, Operand rm)
        {
            Madd(rd, rn, rm, Factory.Register(ZrRegister, RegisterType.Integer, rd.Type));
        }

        public void Mov(Operand rd, Operand rn)
        {
            if (rd.Type.IsInteger())
            {
                Orr(rd, Factory.Register(ZrRegister, RegisterType.Integer, rd.Type), rn);
            }
            else
            {
                OrrVector(rd, rn, rn);
            }
        }

        public void MovSp(Operand rd, Operand rn)
        {
            if (rd.GetRegister().Index == SpRegister ||
                rn.GetRegister().Index == SpRegister)
            {
                Add(rd, rn, Factory.Const(rd.Type, 0), immForm: true);
            }
            else
            {
                Mov(rd, rn);
            }
        }

        public void Mov(Operand rd, int imm)
        {
            Movz(rd, imm, 0);
        }

        public void Movz(Operand rd, int imm, int hw)
        {
            Debug.Assert((hw & (rd.Type == OperandType.I64 ? 3 : 1)) == hw);
            WriteInstructionAuto(0x52800000u | (EncodeUImm16(imm) << 5) | ((uint)hw << 21), rd);
        }

        public void Movk(Operand rd, int imm, int hw)
        {
            Debug.Assert((hw & (rd.Type == OperandType.I64 ? 3 : 1)) == hw);
            WriteInstructionAuto(0x72800000u | (EncodeUImm16(imm) << 5) | ((uint)hw << 21), rd);
        }

        public void Mrs(Operand rt, uint o0, uint op1, uint crn, uint crm, uint op2)
        {
            uint instruction = 0xd5300000u;

            instruction |= (op2 & 7) << 5;
            instruction |= (crm & 15) << 8;
            instruction |= (crn & 15) << 12;
            instruction |= (op1 & 7) << 16;
            instruction |= (o0 & 1) << 19;

            WriteInstruction(instruction, rt);
        }

        public void Mvn(Operand rd, Operand rn, ArmShiftType shiftType = ArmShiftType.Lsl, int shiftAmount = 0)
        {
            Orn(rd, Factory.Register(ZrRegister, RegisterType.Integer, rd.Type), rn, shiftType, shiftAmount);
        }

        public void Neg(Operand rd, Operand rn, ArmShiftType shiftType = ArmShiftType.Lsl, int shiftAmount = 0)
        {
            Sub(rd, Factory.Register(ZrRegister, RegisterType.Integer, rd.Type), rn, shiftType, shiftAmount);
        }

        public void Orn(Operand rd, Operand rn, Operand rm, ArmShiftType shiftType = ArmShiftType.Lsl, int shiftAmount = 0)
        {
            WriteInstructionBitwiseAuto(0x2a200000u, rd, rn, rm, shiftType, shiftAmount);
        }

        public void Orr(Operand rd, Operand rn, Operand rm, ArmShiftType shiftType = ArmShiftType.Lsl, int shiftAmount = 0)
        {
            WriteInstructionBitwiseAuto(0x32000000u, 0x2a000000u, rd, rn, rm, shiftType, shiftAmount);
        }

        public void OrrVector(Operand rd, Operand rn, Operand rm, bool q = true)
        {
            WriteSimdInstruction(0x0ea01c00u, rd, rn, rm, q);
        }

        public void Ret(Operand rn)
        {
            WriteUInt32(0xd65f0000u | (EncodeReg(rn) << 5));
        }

        public void Rev(Operand rd, Operand rn)
        {
            uint opc0 = rd.Type == OperandType.I64 ? 1u << 10 : 0u;
            WriteInstructionAuto(0x5ac00800u | opc0, rd, rn);
        }

        public void Ror(Operand rd, Operand rn, Operand rm)
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

        public void Rorv(Operand rd, Operand rn, Operand rm)
        {
            WriteInstructionBitwiseAuto(0x1ac02c00u, rd, rn, rm);
        }

        public void Sbfm(Operand rd, Operand rn, int immr, int imms)
        {
            uint n = rd.Type == OperandType.I64 ? 1u << 22 : 0u;
            WriteInstructionAuto(0x13000000u | n | (EncodeUImm6(imms) << 10) | (EncodeUImm6(immr) << 16), rd, rn);
        }

        public void ScvtfScalar(Operand rd, Operand rn)
        {
            uint instruction = 0x1e220000u;

            if (rn.Type == OperandType.I64)
            {
                instruction |= SfFlag;
            }

            WriteFPInstructionAuto(instruction, rd, rn);
        }

        public void Sdiv(Operand rd, Operand rn, Operand rm)
        {
            WriteInstructionRm16Auto(0x1ac00c00u, rd, rn, rm);
        }

        public void Smulh(Operand rd, Operand rn, Operand rm)
        {
            WriteInstructionRm16(0x9b407c00u, rd, rn, rm);
        }

        public void Stlxp(Operand rt, Operand rt2, Operand rn, Operand rs)
        {
            WriteInstruction(0x88208000u | ((rt.Type == OperandType.I64 ? 3u : 2u) << 30), rt, rn, rs, rt2);
        }

        public void Stlxr(Operand rt, Operand rn, Operand rs)
        {
            WriteInstructionRm16(0x0800fc00u | ((rt.Type == OperandType.I64 ? 3u : 2u) << 30), rt, rn, rs);
        }

        public void Stlxrb(Operand rt, Operand rn, Operand rs)
        {
            WriteInstructionRm16(0x0800fc00u, rt, rn, rs);
        }

        public void Stlxrh(Operand rt, Operand rn, Operand rs)
        {
            WriteInstructionRm16(0x0800fc00u | (1u << 30), rt, rn, rs);
        }

        public void StpRiPost(Operand rt, Operand rt2, Operand rn, int imm)
        {
            uint instruction = GetLdpStpInstruction(0x28800000u, 0x2c800000u, imm, rt.Type);
            WriteInstruction(instruction, rt, rn, rt2);
        }

        public void StpRiPre(Operand rt, Operand rt2, Operand rn, int imm)
        {
            uint instruction = GetLdpStpInstruction(0x29800000u, 0x2d800000u, imm, rt.Type);
            WriteInstruction(instruction, rt, rn, rt2);
        }

        public void StpRiUn(Operand rt, Operand rt2, Operand rn, int imm)
        {
            uint instruction = GetLdpStpInstruction(0x29000000u, 0x2d000000u, imm, rt.Type);
            WriteInstruction(instruction, rt, rn, rt2);
        }

        public void Str(Operand rt, Operand rn)
        {
            if (rn.Kind == OperandKind.Memory)
            {
                MemoryOperand memOp = rn.GetMemory();

                if (memOp.Index != default)
                {
                    Debug.Assert(memOp.Displacement == 0);
                    Debug.Assert(memOp.Scale == Multiplier.x1 || (int)memOp.Scale == GetScaleForType(rt.Type));
                    StrRr(rt, memOp.BaseAddress, memOp.Index, ArmExtensionType.Uxtx, memOp.Scale != Multiplier.x1);
                }
                else
                {
                    StrRiUn(rt, memOp.BaseAddress, memOp.Displacement);
                }
            }
            else
            {
                StrRiUn(rt, rn, 0);
            }
        }

        public void StrRiPost(Operand rt, Operand rn, int imm)
        {
            uint instruction = GetLdrStrInstruction(0xb8000400u, 0x3c000400u, rt.Type) | (EncodeSImm9(imm) << 12);
            WriteInstruction(instruction, rt, rn);
        }

        public void StrRiPre(Operand rt, Operand rn, int imm)
        {
            uint instruction = GetLdrStrInstruction(0xb8000c00u, 0x3c000c00u, rt.Type) | (EncodeSImm9(imm) << 12);
            WriteInstruction(instruction, rt, rn);
        }

        public void StrRiUn(Operand rt, Operand rn, int imm)
        {
            uint instruction = GetLdrStrInstruction(0xb9000000u, 0x3d000000u, rt.Type) | (EncodeUImm12(imm, rt.Type) << 10);
            WriteInstruction(instruction, rt, rn);
        }

        public void StrRr(Operand rt, Operand rn, Operand rm, ArmExtensionType extensionType, bool shift)
        {
            uint instruction = GetLdrStrInstruction(0xb8200800u, 0x3ca00800u, rt.Type);
            WriteInstructionLdrStrAuto(instruction, rt, rn, rm, extensionType, shift);
        }

        public void StrbRiPost(Operand rt, Operand rn, int imm)
        {
            uint instruction = 0x38000400u | (EncodeSImm9(imm) << 12);
            WriteInstruction(instruction, rt, rn);
        }

        public void StrbRiPre(Operand rt, Operand rn, int imm)
        {
            uint instruction = 0x38000c00u | (EncodeSImm9(imm) << 12);
            WriteInstruction(instruction, rt, rn);
        }

        public void StrbRiUn(Operand rt, Operand rn, int imm)
        {
            uint instruction = 0x39000000u | (EncodeUImm12(imm, 0) << 10);
            WriteInstruction(instruction, rt, rn);
        }

        public void StrhRiPost(Operand rt, Operand rn, int imm)
        {
            uint instruction = 0x78000400u | (EncodeSImm9(imm) << 12);
            WriteInstruction(instruction, rt, rn);
        }

        public void StrhRiPre(Operand rt, Operand rn, int imm)
        {
            uint instruction = 0x78000c00u | (EncodeSImm9(imm) << 12);
            WriteInstruction(instruction, rt, rn);
        }

        public void StrhRiUn(Operand rt, Operand rn, int imm)
        {
            uint instruction = 0x79000000u | (EncodeUImm12(imm, 1) << 10);
            WriteInstruction(instruction, rt, rn);
        }

        public void Stur(Operand rt, Operand rn, int imm)
        {
            uint instruction = GetLdrStrInstruction(0xb8000000u, 0x3c000000u, rt.Type) | (EncodeSImm9(imm) << 12);
            WriteInstruction(instruction, rt, rn);
        }

        public void Sub(Operand rd, Operand rn, Operand rm, ArmExtensionType extensionType, int shiftAmount = 0)
        {
            WriteInstructionAuto(0x4b200000u, rd, rn, rm, extensionType, shiftAmount);
        }

        public void Sub(Operand rd, Operand rn, Operand rm, ArmShiftType shiftType = ArmShiftType.Lsl, int shiftAmount = 0)
        {
            WriteInstructionAuto(0x51000000u, 0x4b000000u, rd, rn, rm, shiftType, shiftAmount);
        }

        public void Subs(Operand rd, Operand rn, Operand rm, ArmShiftType shiftType = ArmShiftType.Lsl, int shiftAmount = 0)
        {
            WriteInstructionAuto(0x71000000u, 0x6b000000u, rd, rn, rm, shiftType, shiftAmount);
        }

        public void Sxtb(Operand rd, Operand rn)
        {
            Sbfm(rd, rn, 0, 7);
        }

        public void Sxth(Operand rd, Operand rn)
        {
            Sbfm(rd, rn, 0, 15);
        }

        public void Sxtw(Operand rd, Operand rn)
        {
            Sbfm(rd, rn, 0, 31);
        }

        public void Tst(Operand rn, Operand rm, ArmShiftType shiftType = ArmShiftType.Lsl, int shiftAmount = 0)
        {
            Ands(Factory.Register(ZrRegister, RegisterType.Integer, rn.Type), rn, rm, shiftType, shiftAmount);
        }

        public void Ubfm(Operand rd, Operand rn, int immr, int imms)
        {
            uint n = rd.Type == OperandType.I64 ? 1u << 22 : 0u;
            WriteInstructionAuto(0x53000000u | n | (EncodeUImm6(imms) << 10) | (EncodeUImm6(immr) << 16), rd, rn);
        }

        public void UcvtfScalar(Operand rd, Operand rn)
        {
            uint instruction = 0x1e230000u;

            if (rn.Type == OperandType.I64)
            {
                instruction |= SfFlag;
            }

            WriteFPInstructionAuto(instruction, rd, rn);
        }

        public void Udiv(Operand rd, Operand rn, Operand rm)
        {
            WriteInstructionRm16Auto(0x1ac00800u, rd, rn, rm);
        }

        public void Umov(Operand rd, Operand rn, int index, int size)
        {
            uint q = size == 3 ? 1u << 30 : 0u;
            WriteInstruction(0x0e003c00u | (EncodeIndexSizeImm5(index, size) << 16) | q, rd, rn);
        }

        public void Umulh(Operand rd, Operand rn, Operand rm)
        {
            WriteInstructionRm16(0x9bc07c00u, rd, rn, rm);
        }

        public void Uxtb(Operand rd, Operand rn)
        {
            Ubfm(rd, rn, 0, 7);
        }

        public void Uxth(Operand rd, Operand rn)
        {
            Ubfm(rd, rn, 0, 15);
        }

        private void WriteInstructionAuto(
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
                instR |= EncodeUImm6(shiftAmount) << 10;
                instR |= (uint)shiftType << 22;

                WriteInstructionRm16Auto(instR, rd, rn, rm);
            }
        }

        private void WriteInstructionAuto(
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

        private void WriteInstructionBitwiseAuto(
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

        private void WriteInstructionBitwiseAuto(
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

        private void WriteInstructionLdrStrAuto(
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

        private void WriteInstructionAuto(uint instruction, Operand rd)
        {
            if (rd.Type == OperandType.I64)
            {
                instruction |= SfFlag;
            }

            WriteInstruction(instruction, rd);
        }

        public void WriteInstructionAuto(uint instruction, Operand rd, Operand rn)
        {
            if (rd.Type == OperandType.I64)
            {
                instruction |= SfFlag;
            }

            WriteInstruction(instruction, rd, rn);
        }

        private void WriteInstructionAuto(uint instruction, Operand rd, Operand rn, Operand rm, Operand ra)
        {
            if (rd.Type == OperandType.I64)
            {
                instruction |= SfFlag;
            }

            WriteInstruction(instruction, rd, rn, rm, ra);
        }

        public void WriteInstruction(uint instruction, Operand rd)
        {
            WriteUInt32(instruction | EncodeReg(rd));
        }

        public void WriteInstruction(uint instruction, Operand rd, Operand rn)
        {
            WriteUInt32(instruction | EncodeReg(rd) | (EncodeReg(rn) << 5));
        }

        public void WriteInstruction(uint instruction, Operand rd, Operand rn, Operand rm)
        {
            WriteUInt32(instruction | EncodeReg(rd) | (EncodeReg(rn) << 5) | (EncodeReg(rm) << 10));
        }

        public void WriteInstruction(uint instruction, Operand rd, Operand rn, Operand rm, Operand ra)
        {
            WriteUInt32(instruction | EncodeReg(rd) | (EncodeReg(rn) << 5) | (EncodeReg(ra) << 10) | (EncodeReg(rm) << 16));
        }

        private void WriteFPInstructionAuto(uint instruction, Operand rd, Operand rn)
        {
            if (rd.Type == OperandType.FP64)
            {
                instruction |= 1u << 22;
            }

            WriteUInt32(instruction | EncodeReg(rd) | (EncodeReg(rn) << 5));
        }

        private void WriteFPInstructionAuto(uint instruction, Operand rd, Operand rn, Operand rm)
        {
            if (rd.Type == OperandType.FP64)
            {
                instruction |= 1u << 22;
            }

            WriteInstructionRm16(instruction, rd, rn, rm);
        }

        private void WriteSimdInstruction(uint instruction, Operand rd, Operand rn, Operand rm, bool q = true)
        {
            if (q)
            {
                instruction |= 1u << 30;
            }

            WriteInstructionRm16(instruction, rd, rn, rm);
        }

        private void WriteInstructionRm16Auto(uint instruction, Operand rd, Operand rn, Operand rm)
        {
            if (rd.Type == OperandType.I64)
            {
                instruction |= SfFlag;
            }

            WriteInstructionRm16(instruction, rd, rn, rm);
        }

        public void WriteInstructionRm16(uint instruction, Operand rd, Operand rn, Operand rm)
        {
            WriteUInt32(instruction | EncodeReg(rd) | (EncodeReg(rn) << 5) | (EncodeReg(rm) << 16));
        }

        public void WriteInstructionRm16NoRet(uint instruction, Operand rn, Operand rm)
        {
            WriteUInt32(instruction | (EncodeReg(rn) << 5) | (EncodeReg(rm) << 16));
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

        public static int GetScaleForType(OperandType type)
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

#pragma warning disable IDE0051 // Remove unused private member
        private void WriteInt16(short value)
        {
            WriteUInt16((ushort)value);
        }

        private void WriteInt32(int value)
        {
            WriteUInt32((uint)value);
        }

        private void WriteByte(byte value)
        {
            _stream.WriteByte(value);
        }
#pragma warning restore IDE0051

        private void WriteUInt16(ushort value)
        {
            _stream.WriteByte((byte)(value >> 0));
            _stream.WriteByte((byte)(value >> 8));
        }

        private void WriteUInt32(uint value)
        {
            _stream.WriteByte((byte)(value >> 0));
            _stream.WriteByte((byte)(value >> 8));
            _stream.WriteByte((byte)(value >> 16));
            _stream.WriteByte((byte)(value >> 24));
        }
    }
}
