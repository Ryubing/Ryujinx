using ARMeilleure.Decoders;
using ARMeilleure.IntermediateRepresentation;
using ARMeilleure.State;
using ARMeilleure.Translation;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using static ARMeilleure.Instructions.InstEmitAluHelper;
using static ARMeilleure.Instructions.InstEmitHelper;
using static ARMeilleure.IntermediateRepresentation.Operand.Factory;

namespace ARMeilleure.Instructions
{
    [SuppressMessage("Style", "IDE0059: Remove unnecessary value assignment")]
    static partial class InstEmit32
    {
        public static void Add(ArmEmitterContext context)
        {
            IOpCode32Alu op = (IOpCode32Alu)context.CurrOp;

            Operand n = GetAluN(context);
            Operand m = GetAluM(context, setCarry: false);

            if (op.Rn == RegisterAlias.Aarch32Pc && op is OpCodeT32AluImm12)
            {
                // For ADR, PC is always 4 bytes aligned, even in Thumb mode.
                n = context.BitwiseAnd(n, Const(~3u));
            }

            Operand res = context.Add(n, m);

            if (ShouldSetFlags(context))
            {
                EmitNZFlagsCheck(context, res);

                EmitAddsCCheck(context, n, res);
                EmitAddsVCheck(context, n, m, res);
            }

            EmitAluStore(context, res);
        }

        public static void Adc(ArmEmitterContext context)
        {
            IOpCode32Alu op = (IOpCode32Alu)context.CurrOp;

            Operand n = GetAluN(context);
            Operand m = GetAluM(context, setCarry: false);

            Operand res = context.Add(n, m);

            Operand carry = GetFlag(PState.CFlag);

            res = context.Add(res, carry);

            if (ShouldSetFlags(context))
            {
                EmitNZFlagsCheck(context, res);

                EmitAdcsCCheck(context, n, res);
                EmitAddsVCheck(context, n, m, res);
            }

            EmitAluStore(context, res);
        }

        public static void And(ArmEmitterContext context)
        {
            IOpCode32Alu op = (IOpCode32Alu)context.CurrOp;

            Operand n = GetAluN(context);
            Operand m = GetAluM(context);

            Operand res = context.BitwiseAnd(n, m);

            if (ShouldSetFlags(context))
            {
                EmitNZFlagsCheck(context, res);
            }

            EmitAluStore(context, res);
        }

        public static void Bfc(ArmEmitterContext context)
        {
            IOpCode32AluBf op = (IOpCode32AluBf)context.CurrOp;

            Operand d = GetIntA32(context, op.Rd);
            Operand res = context.BitwiseAnd(d, Const(~op.DestMask));

            SetIntA32(context, op.Rd, res);
        }

        public static void Bfi(ArmEmitterContext context)
        {
            IOpCode32AluBf op = (IOpCode32AluBf)context.CurrOp;

            Operand n = GetIntA32(context, op.Rn);
            Operand d = GetIntA32(context, op.Rd);
            Operand part = context.BitwiseAnd(n, Const(op.SourceMask));

            if (op.Lsb != 0)
            {
                part = context.ShiftLeft(part, Const(op.Lsb));
            }

            Operand res = context.BitwiseAnd(d, Const(~op.DestMask));
            res = context.BitwiseOr(res, context.BitwiseAnd(part, Const(op.DestMask)));

            SetIntA32(context, op.Rd, res);
        }

        public static void Bic(ArmEmitterContext context)
        {
            IOpCode32Alu op = (IOpCode32Alu)context.CurrOp;

            Operand n = GetAluN(context);
            Operand m = GetAluM(context);

            Operand res = context.BitwiseAnd(n, context.BitwiseNot(m));

            if (ShouldSetFlags(context))
            {
                EmitNZFlagsCheck(context, res);
            }

            EmitAluStore(context, res);
        }

        public static void Clz(ArmEmitterContext context)
        {
            Operand m = GetAluM(context, setCarry: false);

            Operand res = context.CountLeadingZeros(m);
            EmitAluStore(context, res);
        }

        public static void Cmp(ArmEmitterContext context)
        {
            Operand n = GetAluN(context);
            Operand m = GetAluM(context, setCarry: false);

            Operand res = context.Subtract(n, m);

            EmitNZFlagsCheck(context, res);

            EmitSubsCCheck(context, n, res);
            EmitSubsVCheck(context, n, m, res);
        }

        public static void Cmn(ArmEmitterContext context)
        {
            Operand n = GetAluN(context);
            Operand m = GetAluM(context, setCarry: false);

            Operand res = context.Add(n, m);

            EmitNZFlagsCheck(context, res);

            EmitAddsCCheck(context, n, res);
            EmitAddsVCheck(context, n, m, res);
        }

        public static void Eor(ArmEmitterContext context)
        {
            IOpCode32Alu op = (IOpCode32Alu)context.CurrOp;

            Operand n = GetAluN(context);
            Operand m = GetAluM(context);

            Operand res = context.BitwiseExclusiveOr(n, m);

            if (ShouldSetFlags(context))
            {
                EmitNZFlagsCheck(context, res);
            }

            EmitAluStore(context, res);
        }

        public static void Mov(ArmEmitterContext context)
        {
            IOpCode32Alu op = (IOpCode32Alu)context.CurrOp;

            Operand m = GetAluM(context);

            if (ShouldSetFlags(context))
            {
                EmitNZFlagsCheck(context, m);
            }

            EmitAluStore(context, m);
        }

        public static void Movt(ArmEmitterContext context)
        {
            IOpCode32AluImm16 op = (IOpCode32AluImm16)context.CurrOp;

            Operand d = GetIntA32(context, op.Rd);
            Operand imm = Const(op.Immediate << 16); // Immeditate value as top halfword.
            Operand res = context.BitwiseAnd(d, Const(0x0000ffff));
            res = context.BitwiseOr(res, imm);

            EmitAluStore(context, res);
        }

        public static void Mul(ArmEmitterContext context)
        {
            IOpCode32Alu op = (IOpCode32Alu)context.CurrOp;

            Operand n = GetAluN(context);
            Operand m = GetAluM(context);

            Operand res = context.Multiply(n, m);

            if (ShouldSetFlags(context))
            {
                EmitNZFlagsCheck(context, res);
            }

            EmitAluStore(context, res);
        }

        public static void Mvn(ArmEmitterContext context)
        {
            IOpCode32Alu op = (IOpCode32Alu)context.CurrOp;
            Operand m = GetAluM(context);

            Operand res = context.BitwiseNot(m);

            if (ShouldSetFlags(context))
            {
                EmitNZFlagsCheck(context, res);
            }

            EmitAluStore(context, res);
        }

        public static void Orr(ArmEmitterContext context)
        {
            IOpCode32Alu op = (IOpCode32Alu)context.CurrOp;

            Operand n = GetAluN(context);
            Operand m = GetAluM(context);

            Operand res = context.BitwiseOr(n, m);

            if (ShouldSetFlags(context))
            {
                EmitNZFlagsCheck(context, res);
            }

            EmitAluStore(context, res);
        }

        public static void Orn(ArmEmitterContext context)
        {
            IOpCode32Alu op = (IOpCode32Alu)context.CurrOp;

            Operand n = GetAluN(context);
            Operand m = GetAluM(context);

            Operand res = context.BitwiseOr(n, context.BitwiseNot(m));

            if (ShouldSetFlags(context))
            {
                EmitNZFlagsCheck(context, res);
            }

            EmitAluStore(context, res);
        }

        public static void Pkh(ArmEmitterContext context)
        {
            OpCode32AluRsImm op = (OpCode32AluRsImm)context.CurrOp;

            Operand n = GetAluN(context);
            Operand m = GetAluM(context);

            Operand res;

            bool tbform = op.ShiftType == ShiftType.Asr;
            if (tbform)
            {
                res = context.BitwiseOr(context.BitwiseAnd(n, Const(0xFFFF0000)), context.BitwiseAnd(m, Const(0xFFFF)));
            }
            else
            {
                res = context.BitwiseOr(context.BitwiseAnd(m, Const(0xFFFF0000)), context.BitwiseAnd(n, Const(0xFFFF)));
            }

            EmitAluStore(context, res);
        }

        public static void Qadd16(ArmEmitterContext context)
        {
            OpCode32AluReg op = (OpCode32AluReg)context.CurrOp;

            SetIntA32(context, op.Rd, EmitSigned16BitPair(context, GetIntA32(context, op.Rn), GetIntA32(context, op.Rm), (d, n, m) =>
            {
                EmitSaturateRange(context, d, context.Add(n, m), 16, unsigned: false, setQ: false);
            }));
        }

        public static void Rbit(ArmEmitterContext context)
        {
            Operand m = GetAluM(context);

            Operand res = EmitReverseBits32Op(context, m);

            EmitAluStore(context, res);
        }

        public static void Rev(ArmEmitterContext context)
        {
            Operand m = GetAluM(context);

            Operand res = context.ByteSwap(m);

            EmitAluStore(context, res);
        }

        public static void Rev16(ArmEmitterContext context)
        {
            Operand m = GetAluM(context);

            Operand res = EmitReverseBytes16_32Op(context, m);

            EmitAluStore(context, res);
        }

        public static void Revsh(ArmEmitterContext context)
        {
            Operand m = GetAluM(context);

            Operand res = EmitReverseBytes16_32Op(context, m);

            EmitAluStore(context, context.SignExtend16(OperandType.I32, res));
        }

        public static void Rsc(ArmEmitterContext context)
        {
            IOpCode32Alu op = (IOpCode32Alu)context.CurrOp;

            Operand n = GetAluN(context);
            Operand m = GetAluM(context, setCarry: false);

            Operand res = context.Subtract(m, n);

            Operand borrow = context.BitwiseExclusiveOr(GetFlag(PState.CFlag), Const(1));

            res = context.Subtract(res, borrow);

            if (ShouldSetFlags(context))
            {
                EmitNZFlagsCheck(context, res);

                EmitSbcsCCheck(context, m, n);
                EmitSubsVCheck(context, m, n, res);
            }

            EmitAluStore(context, res);
        }

        public static void Rsb(ArmEmitterContext context)
        {
            IOpCode32Alu op = (IOpCode32Alu)context.CurrOp;

            Operand n = GetAluN(context);
            Operand m = GetAluM(context, setCarry: false);

            Operand res = context.Subtract(m, n);

            if (ShouldSetFlags(context))
            {
                EmitNZFlagsCheck(context, res);

                EmitSubsCCheck(context, m, res);
                EmitSubsVCheck(context, m, n, res);
            }

            EmitAluStore(context, res);
        }

        public static void Sadd8(ArmEmitterContext context)
        {
            EmitAddSub8(context, add: true, unsigned: false);
        }

        public static void Sbc(ArmEmitterContext context)
        {
            IOpCode32Alu op = (IOpCode32Alu)context.CurrOp;

            Operand n = GetAluN(context);
            Operand m = GetAluM(context, setCarry: false);

            Operand res = context.Subtract(n, m);

            Operand borrow = context.BitwiseExclusiveOr(GetFlag(PState.CFlag), Const(1));

            res = context.Subtract(res, borrow);

            if (ShouldSetFlags(context))
            {
                EmitNZFlagsCheck(context, res);

                EmitSbcsCCheck(context, n, m);
                EmitSubsVCheck(context, n, m, res);
            }

            EmitAluStore(context, res);
        }

        public static void Sbfx(ArmEmitterContext context)
        {
            IOpCode32AluBf op = (IOpCode32AluBf)context.CurrOp;

            int msb = op.Lsb + op.Msb; // For this instruction, the msb is actually a width.

            Operand n = GetIntA32(context, op.Rn);
            Operand res = context.ShiftRightSI(context.ShiftLeft(n, Const(31 - msb)), Const(31 - op.Msb));

            SetIntA32(context, op.Rd, res);
        }

        public static void Sdiv(ArmEmitterContext context)
        {
            EmitDiv(context, unsigned: false);
        }

        public static void Sel(ArmEmitterContext context)
        {
            IOpCode32AluReg op = (IOpCode32AluReg)context.CurrOp;

            Operand n = GetIntA32(context, op.Rn);
            Operand m = GetIntA32(context, op.Rm);

            Operand ge0 = context.ZeroExtend8(OperandType.I32, context.Negate(GetFlag(PState.GE0Flag)));
            Operand ge1 = context.ZeroExtend8(OperandType.I32, context.Negate(GetFlag(PState.GE1Flag)));
            Operand ge2 = context.ZeroExtend8(OperandType.I32, context.Negate(GetFlag(PState.GE2Flag)));
            Operand ge3 = context.Negate(GetFlag(PState.GE3Flag));

            Operand mask = context.BitwiseOr(ge0, context.ShiftLeft(ge1, Const(8)));
            mask = context.BitwiseOr(mask, context.ShiftLeft(ge2, Const(16)));
            mask = context.BitwiseOr(mask, context.ShiftLeft(ge3, Const(24)));

            Operand res = context.BitwiseOr(context.BitwiseAnd(n, mask), context.BitwiseAnd(m, context.BitwiseNot(mask)));

            SetIntA32(context, op.Rd, res);
        }

        public static void Shadd8(ArmEmitterContext context)
        {
            EmitHadd8(context, unsigned: false);
        }

        public static void Shsub8(ArmEmitterContext context)
        {
            EmitHsub8(context, unsigned: false);
        }

        public static void Ssat(ArmEmitterContext context)
        {
            OpCode32Sat op = (OpCode32Sat)context.CurrOp;

            EmitSat(context, -(1 << op.SatImm), (1 << op.SatImm) - 1);
        }

        public static void Ssat16(ArmEmitterContext context)
        {
            OpCode32Sat16 op = (OpCode32Sat16)context.CurrOp;

            EmitSat16(context, -(1 << op.SatImm), (1 << op.SatImm) - 1);
        }

        public static void Ssub8(ArmEmitterContext context)
        {
            EmitAddSub8(context, add: false, unsigned: false);
        }

        public static void Sub(ArmEmitterContext context)
        {
            IOpCode32Alu op = (IOpCode32Alu)context.CurrOp;

            Operand n = GetAluN(context);
            Operand m = GetAluM(context, setCarry: false);

            if (op.Rn == RegisterAlias.Aarch32Pc && op is OpCodeT32AluImm12)
            {
                // For ADR, PC is always 4 bytes aligned, even in Thumb mode.
                n = context.BitwiseAnd(n, Const(~3u));
            }

            Operand res = context.Subtract(n, m);

            if (ShouldSetFlags(context))
            {
                EmitNZFlagsCheck(context, res);

                EmitSubsCCheck(context, n, res);
                EmitSubsVCheck(context, n, m, res);
            }

            EmitAluStore(context, res);
        }

        public static void Sxtb(ArmEmitterContext context)
        {
            EmitSignExtend(context, true, 8);
        }

        public static void Sxtb16(ArmEmitterContext context)
        {
            EmitExtend16(context, true);
        }

        public static void Sxth(ArmEmitterContext context)
        {
            EmitSignExtend(context, true, 16);
        }

        public static void Teq(ArmEmitterContext context)
        {
            Operand n = GetAluN(context);
            Operand m = GetAluM(context);

            Operand res = context.BitwiseExclusiveOr(n, m);

            EmitNZFlagsCheck(context, res);
        }

        public static void Tst(ArmEmitterContext context)
        {
            Operand n = GetAluN(context);
            Operand m = GetAluM(context);

            Operand res = context.BitwiseAnd(n, m);
            EmitNZFlagsCheck(context, res);
        }

        public static void Uadd8(ArmEmitterContext context)
        {
            EmitAddSub8(context, add: true, unsigned: true);
        }

        public static void Ubfx(ArmEmitterContext context)
        {
            IOpCode32AluBf op = (IOpCode32AluBf)context.CurrOp;

            int msb = op.Lsb + op.Msb; // For this instruction, the msb is actually a width.

            Operand n = GetIntA32(context, op.Rn);
            Operand res = context.ShiftRightUI(context.ShiftLeft(n, Const(31 - msb)), Const(31 - op.Msb));

            SetIntA32(context, op.Rd, res);
        }

        public static void Udiv(ArmEmitterContext context)
        {
            EmitDiv(context, unsigned: true);
        }

        public static void Uhadd8(ArmEmitterContext context)
        {
            EmitHadd8(context, unsigned: true);
        }

        public static void Uhsub8(ArmEmitterContext context)
        {
            EmitHsub8(context, unsigned: true);
        }

        public static void Uqadd16(ArmEmitterContext context)
        {
            OpCode32AluReg op = (OpCode32AluReg)context.CurrOp;

            SetIntA32(context, op.Rd, EmitUnsigned16BitPair(context, GetIntA32(context, op.Rn), GetIntA32(context, op.Rm), (d, n, m) =>
            {
                EmitSaturateUqadd(context, d, context.Add(n, m), 16);
            }));
        }

        public static void Uqadd8(ArmEmitterContext context)
        {
            OpCode32AluReg op = (OpCode32AluReg)context.CurrOp;

            SetIntA32(context, op.Rd, EmitUnsigned8BitPair(context, GetIntA32(context, op.Rn), GetIntA32(context, op.Rm), (d, n, m) =>
            {
                EmitSaturateUqadd(context, d, context.Add(n, m), 8);
            }));
        }

        public static void Uqsub16(ArmEmitterContext context)
        {
            OpCode32AluReg op = (OpCode32AluReg)context.CurrOp;

            SetIntA32(context, op.Rd, EmitUnsigned16BitPair(context, GetIntA32(context, op.Rn), GetIntA32(context, op.Rm), (d, n, m) =>
            {
                EmitSaturateUqsub(context, d, context.Subtract(n, m), 16);
            }));
        }

        public static void Uqsub8(ArmEmitterContext context)
        {
            OpCode32AluReg op = (OpCode32AluReg)context.CurrOp;

            SetIntA32(context, op.Rd, EmitUnsigned8BitPair(context, GetIntA32(context, op.Rn), GetIntA32(context, op.Rm), (d, n, m) =>
            {
                EmitSaturateUqsub(context, d, context.Subtract(n, m), 8);
            }));
        }

        public static void Usat(ArmEmitterContext context)
        {
            OpCode32Sat op = (OpCode32Sat)context.CurrOp;

            EmitSat(context, 0, op.SatImm == 32 ? (int)(~0) : (1 << op.SatImm) - 1);
        }

        public static void Usat16(ArmEmitterContext context)
        {
            OpCode32Sat16 op = (OpCode32Sat16)context.CurrOp;

            EmitSat16(context, 0, (1 << op.SatImm) - 1);
        }

        public static void Usub8(ArmEmitterContext context)
        {
            EmitAddSub8(context, add: false, unsigned: true);
        }

        public static void Uxtb(ArmEmitterContext context)
        {
            EmitSignExtend(context, false, 8);
        }

        public static void Uxtb16(ArmEmitterContext context)
        {
            EmitExtend16(context, false);
        }

        public static void Uxth(ArmEmitterContext context)
        {
            EmitSignExtend(context, false, 16);
        }

        private static void EmitSignExtend(ArmEmitterContext context, bool signed, int bits)
        {
            IOpCode32AluUx op = (IOpCode32AluUx)context.CurrOp;

            Operand m = GetAluM(context);
            Operand res;

            if (op.RotateBits == 0)
            {
                res = m;
            }
            else
            {
                Operand rotate = Const(op.RotateBits);
                res = context.RotateRight(m, rotate);
            }

            switch (bits)
            {
                case 8:
                    res = (signed) ? context.SignExtend8(OperandType.I32, res) : context.ZeroExtend8(OperandType.I32, res);
                    break;
                case 16:
                    res = (signed) ? context.SignExtend16(OperandType.I32, res) : context.ZeroExtend16(OperandType.I32, res);
                    break;
            }

            if (op.Add)
            {
                res = context.Add(res, GetAluN(context));
            }

            EmitAluStore(context, res);
        }

        private static void EmitExtend16(ArmEmitterContext context, bool signed)
        {
            IOpCode32AluUx op = (IOpCode32AluUx)context.CurrOp;

            Operand m = GetAluM(context);
            Operand res;

            if (op.RotateBits == 0)
            {
                res = m;
            }
            else
            {
                Operand rotate = Const(op.RotateBits);
                res = context.RotateRight(m, rotate);
            }

            Operand low16, high16;
            if (signed)
            {
                low16 = context.SignExtend8(OperandType.I32, res);
                high16 = context.SignExtend8(OperandType.I32, context.ShiftRightUI(res, Const(16)));
            }
            else
            {
                low16 = context.ZeroExtend8(OperandType.I32, res);
                high16 = context.ZeroExtend8(OperandType.I32, context.ShiftRightUI(res, Const(16)));
            }

            if (op.Add)
            {
                Operand n = GetAluN(context);
                Operand lowAdd, highAdd;
                if (signed)
                {
                    lowAdd = context.SignExtend16(OperandType.I32, n);
                    highAdd = context.SignExtend16(OperandType.I32, context.ShiftRightUI(n, Const(16)));
                }
                else
                {
                    lowAdd = context.ZeroExtend16(OperandType.I32, n);
                    highAdd = context.ZeroExtend16(OperandType.I32, context.ShiftRightUI(n, Const(16)));
                }

                low16 = context.Add(low16, lowAdd);
                high16 = context.Add(high16, highAdd);
            }

            res = context.BitwiseOr(
                context.ZeroExtend16(OperandType.I32, low16),
                context.ShiftLeft(context.ZeroExtend16(OperandType.I32, high16), Const(16)));

            EmitAluStore(context, res);
        }

        private static void EmitDiv(ArmEmitterContext context, bool unsigned)
        {
            Operand n = GetAluN(context);
            Operand m = GetAluM(context);
            Operand zero = Const(m.Type, 0);

            Operand divisorIsZero = context.ICompareEqual(m, zero);

            Operand lblBadDiv = Label();
            Operand lblEnd = Label();

            context.BranchIfTrue(lblBadDiv, divisorIsZero);

            if (!unsigned)
            {
                // ARM64 behaviour: If Rn == INT_MIN && Rm == -1, Rd = INT_MIN (overflow).
                // TODO: tests to ensure A32 works the same

                Operand intMin = Const(int.MinValue);
                Operand minus1 = Const(-1);

                Operand nIsIntMin = context.ICompareEqual(n, intMin);
                Operand mIsMinus1 = context.ICompareEqual(m, minus1);

                Operand lblGoodDiv = Label();

                context.BranchIfFalse(lblGoodDiv, context.BitwiseAnd(nIsIntMin, mIsMinus1));

                EmitAluStore(context, intMin);

                context.Branch(lblEnd);

                context.MarkLabel(lblGoodDiv);
            }

            Operand res = unsigned
                ? context.DivideUI(n, m)
                : context.Divide(n, m);

            EmitAluStore(context, res);

            context.Branch(lblEnd);

            context.MarkLabel(lblBadDiv);

            EmitAluStore(context, zero);

            context.MarkLabel(lblEnd);
        }

        private static void EmitAddSub8(ArmEmitterContext context, bool add, bool unsigned)
        {
            IOpCode32AluReg op = (IOpCode32AluReg)context.CurrOp;

            Operand n = GetIntA32(context, op.Rn);
            Operand m = GetIntA32(context, op.Rm);

            Operand res = Const(0);

            for (int byteSel = 0; byteSel < 4; byteSel++)
            {
                Operand shift = Const(byteSel * 8);

                Operand nByte = context.ShiftRightUI(n, shift);
                Operand mByte = context.ShiftRightUI(m, shift);

                nByte = unsigned ? context.ZeroExtend8(OperandType.I32, nByte) : context.SignExtend8(OperandType.I32, nByte);
                mByte = unsigned ? context.ZeroExtend8(OperandType.I32, mByte) : context.SignExtend8(OperandType.I32, mByte);

                Operand resByte = add ? context.Add(nByte, mByte) : context.Subtract(nByte, mByte);

                res = context.BitwiseOr(res, context.ShiftLeft(context.ZeroExtend8(OperandType.I32, resByte), shift));

                SetFlag(context, PState.GE0Flag + byteSel, unsigned && add
                    ? context.ShiftRightUI(resByte, Const(8))
                    : context.ShiftRightUI(context.BitwiseNot(resByte), Const(31)));
            }

            SetIntA32(context, op.Rd, res);
        }

        private static void EmitHadd8(ArmEmitterContext context, bool unsigned)
        {
            IOpCode32AluReg op = (IOpCode32AluReg)context.CurrOp;

            Operand m = GetIntA32(context, op.Rm);
            Operand n = GetIntA32(context, op.Rn);

            Operand xor, res, carry;

            // This relies on the equality x+y == ((x&y) << 1) + (x^y).
            // Note that x^y always contains the LSB of the result.
            // Since we want to calculate (x+y)/2, we can instead calculate (x&y) + ((x^y)>>1).
            // We mask by 0x7F to remove the LSB so that it doesn't leak into the field below.

            res = context.BitwiseAnd(m, n);
            carry = context.BitwiseExclusiveOr(m, n);
            xor = context.ShiftRightUI(carry, Const(1));
            xor = context.BitwiseAnd(xor, Const(0x7F7F7F7Fu));
            res = context.Add(res, xor);

            if (!unsigned)
            {
                // Propagates the sign bit from (x^y)>>1 upwards by one.
                carry = context.BitwiseAnd(carry, Const(0x80808080u));
                res = context.BitwiseExclusiveOr(res, carry);
            }

            SetIntA32(context, op.Rd, res);
        }

        private static void EmitHsub8(ArmEmitterContext context, bool unsigned)
        {
            IOpCode32AluReg op = (IOpCode32AluReg)context.CurrOp;

            Operand m = GetIntA32(context, op.Rm);
            Operand n = GetIntA32(context, op.Rn);
            Operand left, right, carry, res;

            // This relies on the equality x-y == (x^y) - (((x^y)&y) << 1).
            // Note that x^y always contains the LSB of the result.
            // Since we want to calculate (x+y)/2, we can instead calculate ((x^y)>>1) - ((x^y)&y).

            carry = context.BitwiseExclusiveOr(m, n);
            left = context.ShiftRightUI(carry, Const(1));
            right = context.BitwiseAnd(carry, m);

            // We must now perform a partitioned subtraction.
            // We can do this because minuend contains 7 bit fields.
            // We use the extra bit in minuend as a bit to borrow from; we set this bit.
            // We invert this bit at the end as this tells us if that bit was borrowed from.

            res = context.BitwiseOr(left, Const(0x80808080));
            res = context.Subtract(res, right);
            res = context.BitwiseExclusiveOr(res, Const(0x80808080));

            if (!unsigned)
            {
                // We then sign extend the result into this bit.
                carry = context.BitwiseAnd(carry, Const(0x80808080));
                res = context.BitwiseExclusiveOr(res, carry);
            }

            SetIntA32(context, op.Rd, res);
        }

        private static void EmitSat(ArmEmitterContext context, int intMin, int intMax)
        {
            OpCode32Sat op = (OpCode32Sat)context.CurrOp;

            Operand n = GetIntA32(context, op.Rn);

            int shift = DecodeImmShift(op.ShiftType, op.Imm5);

            switch (op.ShiftType)
            {
                case ShiftType.Lsl:
                    if (shift == 32)
                    {
                        n = Const(0);
                    }
                    else
                    {
                        n = context.ShiftLeft(n, Const(shift));
                    }
                    break;
                case ShiftType.Asr:
                    if (shift == 32)
                    {
                        n = context.ShiftRightSI(n, Const(31));
                    }
                    else
                    {
                        n = context.ShiftRightSI(n, Const(shift));
                    }
                    break;
            }

            Operand lblCheckLtIntMin = Label();
            Operand lblNoSat = Label();
            Operand lblEnd = Label();

            context.BranchIfFalse(lblCheckLtIntMin, context.ICompareGreater(n, Const(intMax)));

            SetFlag(context, PState.QFlag, Const(1));
            SetIntA32(context, op.Rd, Const(intMax));
            context.Branch(lblEnd);

            context.MarkLabel(lblCheckLtIntMin);
            context.BranchIfFalse(lblNoSat, context.ICompareLess(n, Const(intMin)));

            SetFlag(context, PState.QFlag, Const(1));
            SetIntA32(context, op.Rd, Const(intMin));
            context.Branch(lblEnd);

            context.MarkLabel(lblNoSat);

            SetIntA32(context, op.Rd, n);

            context.MarkLabel(lblEnd);
        }

        private static void EmitSat16(ArmEmitterContext context, int intMin, int intMax)
        {
            OpCode32Sat16 op = (OpCode32Sat16)context.CurrOp;

            void SetD(int part, Operand value)
            {
                if (part == 0)
                {
                    SetIntA32(context, op.Rd, context.ZeroExtend16(OperandType.I32, value));
                }
                else
                {
                    SetIntA32(context, op.Rd, context.BitwiseOr(GetIntA32(context, op.Rd), context.ShiftLeft(value, Const(16))));
                }
            }

            Operand n = GetIntA32(context, op.Rn);

            Operand nLow = context.SignExtend16(OperandType.I32, n);
            Operand nHigh = context.ShiftRightSI(n, Const(16));

            for (int part = 0; part < 2; part++)
            {
                Operand nPart = part == 0 ? nLow : nHigh;

                Operand lblCheckLtIntMin = Label();
                Operand lblNoSat = Label();
                Operand lblEnd = Label();

                context.BranchIfFalse(lblCheckLtIntMin, context.ICompareGreater(nPart, Const(intMax)));

                SetFlag(context, PState.QFlag, Const(1));
                SetD(part, Const(intMax));
                context.Branch(lblEnd);

                context.MarkLabel(lblCheckLtIntMin);
                context.BranchIfFalse(lblNoSat, context.ICompareLess(nPart, Const(intMin)));

                SetFlag(context, PState.QFlag, Const(1));
                SetD(part, Const(intMin));
                context.Branch(lblEnd);

                context.MarkLabel(lblNoSat);

                SetD(part, nPart);

                context.MarkLabel(lblEnd);
            }
        }

        private static void EmitSaturateRange(ArmEmitterContext context, Operand result, Operand value, uint saturateTo, bool unsigned, bool setQ = true)
        {
            Debug.Assert(saturateTo <= 32);
            Debug.Assert(!unsigned || saturateTo < 32);

            if (!unsigned && saturateTo == 32)
            {
                // No saturation possible for this case.

                context.Copy(result, value);

                return;
            }
            else if (saturateTo == 0)
            {
                // Result is always zero if we saturate 0 bits.

                context.Copy(result, Const(0));

                return;
            }

            Operand satValue;

            if (unsigned)
            {
                // Negative values always saturate (to zero).
                // So we must always ignore the sign bit when masking, so that the truncated value will differ from the original one.

                satValue = context.BitwiseAnd(value, Const((int)(uint.MaxValue >> (32 - (int)saturateTo))));
            }
            else
            {
                satValue = context.ShiftLeft(value, Const(32 - (int)saturateTo));
                satValue = context.ShiftRightSI(satValue, Const(32 - (int)saturateTo));
            }

            // If the result is 0, the values are equal and we don't need saturation.
            Operand lblNoSat = Label();
            context.BranchIfFalse(lblNoSat, context.Subtract(value, satValue));

            // Saturate and set Q flag.
            if (unsigned)
            {
                if (saturateTo == 31)
                {
                    // Only saturation case possible when going from 32 bits signed to 32 or 31 bits unsigned
                    // is when the signed input is negative, as all positive values are representable on a 31 bits range.

                    satValue = Const(0);
                }
                else
                {
                    satValue = context.ShiftRightSI(value, Const(31));
                    satValue = context.BitwiseNot(satValue);
                    satValue = context.ShiftRightUI(satValue, Const(32 - (int)saturateTo));
                }
            }
            else
            {
                if (saturateTo == 1)
                {
                    satValue = context.ShiftRightSI(value, Const(31));
                }
                else
                {
                    satValue = Const(uint.MaxValue >> (33 - (int)saturateTo));
                    satValue = context.BitwiseExclusiveOr(satValue, context.ShiftRightSI(value, Const(31)));
                }
            }

            if (setQ)
            {
                SetFlag(context, PState.QFlag, Const(1));
            }

            context.Copy(result, satValue);

            Operand lblExit = Label();
            context.Branch(lblExit);

            context.MarkLabel(lblNoSat);

            context.Copy(result, value);

            context.MarkLabel(lblExit);
        }

        private static void EmitSaturateUqadd(ArmEmitterContext context, Operand result, Operand value, uint saturateTo)
        {
            Debug.Assert(saturateTo <= 32);

            if (saturateTo == 32)
            {
                // No saturation possible for this case.

                context.Copy(result, value);

                return;
            }
            else if (saturateTo == 0)
            {
                // Result is always zero if we saturate 0 bits.

                context.Copy(result, Const(0));

                return;
            }

            // If the result is 0, the values are equal and we don't need saturation.
            Operand lblNoSat = Label();
            context.BranchIfFalse(lblNoSat, context.ShiftRightUI(value, Const((int)saturateTo)));

            // Saturate.
            context.Copy(result, Const(uint.MaxValue >> (32 - (int)saturateTo)));

            Operand lblExit = Label();
            context.Branch(lblExit);

            context.MarkLabel(lblNoSat);

            context.Copy(result, value);

            context.MarkLabel(lblExit);
        }

        private static void EmitSaturateUqsub(ArmEmitterContext context, Operand result, Operand value, uint saturateTo)
        {
            Debug.Assert(saturateTo <= 32);

            if (saturateTo == 32)
            {
                // No saturation possible for this case.

                context.Copy(result, value);

                return;
            }
            else if (saturateTo == 0)
            {
                // Result is always zero if we saturate 0 bits.

                context.Copy(result, Const(0));

                return;
            }

            // If the result is 0, the values are equal and we don't need saturation.
            Operand lblNoSat = Label();
            context.BranchIf(lblNoSat, value, Const(0), Comparison.GreaterOrEqual);

            // Saturate.
            // Assumes that the value can only underflow, since this is only used for unsigned subtraction.
            context.Copy(result, Const(0));

            Operand lblExit = Label();
            context.Branch(lblExit);

            context.MarkLabel(lblNoSat);

            context.Copy(result, value);

            context.MarkLabel(lblExit);
        }

        private static Operand EmitSigned16BitPair(ArmEmitterContext context, Operand rn, Operand rm, Action<Operand, Operand, Operand> elementAction)
        {
            Operand tempD = context.AllocateLocal(OperandType.I32);

            Operand tempN = context.SignExtend16(OperandType.I32, rn);
            Operand tempM = context.SignExtend16(OperandType.I32, rm);
            elementAction(tempD, tempN, tempM);
            Operand tempD2 = context.ZeroExtend16(OperandType.I32, tempD);

            tempN = context.ShiftRightSI(rn, Const(16));
            tempM = context.ShiftRightSI(rm, Const(16));
            elementAction(tempD, tempN, tempM);
            return context.BitwiseOr(tempD2, context.ShiftLeft(tempD, Const(16)));
        }

        private static Operand EmitUnsigned16BitPair(ArmEmitterContext context, Operand rn, Operand rm, Action<Operand, Operand, Operand> elementAction)
        {
            Operand tempD = context.AllocateLocal(OperandType.I32);

            Operand tempN = context.ZeroExtend16(OperandType.I32, rn);
            Operand tempM = context.ZeroExtend16(OperandType.I32, rm);
            elementAction(tempD, tempN, tempM);
            Operand tempD2 = context.ZeroExtend16(OperandType.I32, tempD);

            tempN = context.ShiftRightUI(rn, Const(16));
            tempM = context.ShiftRightUI(rm, Const(16));
            elementAction(tempD, tempN, tempM);
            return context.BitwiseOr(tempD2, context.ShiftLeft(tempD, Const(16)));
        }

        private static Operand EmitSigned8BitPair(ArmEmitterContext context, Operand rn, Operand rm, Action<Operand, Operand, Operand> elementAction)
        {
            return Emit8BitPair(context, rn, rm, elementAction, unsigned: false);
        }

        private static Operand EmitUnsigned8BitPair(ArmEmitterContext context, Operand rn, Operand rm, Action<Operand, Operand, Operand> elementAction)
        {
            return Emit8BitPair(context, rn, rm, elementAction, unsigned: true);
        }

        private static Operand Emit8BitPair(ArmEmitterContext context, Operand rn, Operand rm, Action<Operand, Operand, Operand> elementAction, bool unsigned)
        {
            Operand tempD = context.AllocateLocal(OperandType.I32);
            Operand result = default;

            for (int b = 0; b < 4; b++)
            {
                Operand nByte = b != 0 ? context.ShiftRightUI(rn, Const(b * 8)) : rn;
                Operand mByte = b != 0 ? context.ShiftRightUI(rm, Const(b * 8)) : rm;

                if (unsigned)
                {
                    nByte = context.ZeroExtend8(OperandType.I32, nByte);
                    mByte = context.ZeroExtend8(OperandType.I32, mByte);
                }
                else
                {
                    nByte = context.SignExtend8(OperandType.I32, nByte);
                    mByte = context.SignExtend8(OperandType.I32, mByte);
                }

                elementAction(tempD, nByte, mByte);

                if (b == 0)
                {
                    result = context.ZeroExtend8(OperandType.I32, tempD);
                }
                else if (b < 3)
                {
                    result = context.BitwiseOr(result, context.ShiftLeft(context.ZeroExtend8(OperandType.I32, tempD), Const(b * 8)));
                }
                else
                {
                    result = context.BitwiseOr(result, context.ShiftLeft(tempD, Const(24)));
                }
            }

            return result;
        }

        private static void EmitAluStore(ArmEmitterContext context, Operand value)
        {
            IOpCode32Alu op = (IOpCode32Alu)context.CurrOp;

            EmitGenericAluStoreA32(context, op.Rd, ShouldSetFlags(context), value);
        }
    }
}
