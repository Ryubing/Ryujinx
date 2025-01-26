using ARMeilleure.Decoders;
using ARMeilleure.IntermediateRepresentation;
using ARMeilleure.State;
using ARMeilleure.Translation;
using System;

using static ARMeilleure.Instructions.InstEmitHelper;
using static ARMeilleure.Instructions.InstEmitSimdHelper;
using static ARMeilleure.Instructions.InstEmitSimdHelper32;
using static ARMeilleure.IntermediateRepresentation.Operand.Factory;

namespace ARMeilleure.Instructions
{
    using Func2I = Func<Operand, Operand, Operand>;

    static partial class InstEmit32
    {
        public static void Vceq_V(ArmEmitterContext context)
        {
            if (Optimizations.FastFP && Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelper32Arm64.EmitCmpOpF32(context, CmpCondition.Equal, false);
            }
            else if (Optimizations.FastFP && Optimizations.UseSse2)
            {
                EmitSse2OrAvxCmpOpF32(context, CmpCondition.Equal, false);
            }
            else
            {
                EmitCmpOpF32(context, nameof(SoftFloat32.FPCompareEQFpscr), false);
            }
        }

        public static void Vceq_I(ArmEmitterContext context)
        {
            EmitCmpOpI32(context, context.ICompareEqual, context.ICompareEqual, false, false);
        }

        public static void Vceq_Z(ArmEmitterContext context)
        {
            OpCode32Simd op = (OpCode32Simd)context.CurrOp;

            if (op.F)
            {
                if (Optimizations.FastFP && Optimizations.UseAdvSimd)
                {
                    InstEmitSimdHelper32Arm64.EmitCmpOpF32(context, CmpCondition.Equal, true);
                }
                else if (Optimizations.FastFP && Optimizations.UseSse2)
                {
                    EmitSse2OrAvxCmpOpF32(context, CmpCondition.Equal, true);
                }
                else
                {
                    EmitCmpOpF32(context, nameof(SoftFloat32.FPCompareEQFpscr), true);
                }
            }
            else
            {
                EmitCmpOpI32(context, context.ICompareEqual, context.ICompareEqual, true, false);
            }
        }

        public static void Vcge_V(ArmEmitterContext context)
        {
            if (Optimizations.FastFP && Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelper32Arm64.EmitCmpOpF32(context, CmpCondition.GreaterThanOrEqual, false);
            }
            else if (Optimizations.FastFP && Optimizations.UseAvx)
            {
                EmitSse2OrAvxCmpOpF32(context, CmpCondition.GreaterThanOrEqual, false);
            }
            else
            {
                EmitCmpOpF32(context, nameof(SoftFloat32.FPCompareGEFpscr), false);
            }
        }

        public static void Vcge_I(ArmEmitterContext context)
        {
            OpCode32SimdReg op = (OpCode32SimdReg)context.CurrOp;

            EmitCmpOpI32(context, context.ICompareGreaterOrEqual, context.ICompareGreaterOrEqualUI, false, !op.U);
        }

        public static void Vcge_Z(ArmEmitterContext context)
        {
            OpCode32Simd op = (OpCode32Simd)context.CurrOp;

            if (op.F)
            {
                if (Optimizations.FastFP && Optimizations.UseAdvSimd)
                {
                    InstEmitSimdHelper32Arm64.EmitCmpOpF32(context, CmpCondition.GreaterThanOrEqual, true);
                }
                else if (Optimizations.FastFP && Optimizations.UseAvx)
                {
                    EmitSse2OrAvxCmpOpF32(context, CmpCondition.GreaterThanOrEqual, true);
                }
                else
                {
                    EmitCmpOpF32(context, nameof(SoftFloat32.FPCompareGEFpscr), true);
                }
            }
            else
            {
                EmitCmpOpI32(context, context.ICompareGreaterOrEqual, context.ICompareGreaterOrEqualUI, true, true);
            }
        }

        public static void Vcgt_V(ArmEmitterContext context)
        {
            if (Optimizations.FastFP && Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelper32Arm64.EmitCmpOpF32(context, CmpCondition.GreaterThan, false);
            }
            else if (Optimizations.FastFP && Optimizations.UseAvx)
            {
                EmitSse2OrAvxCmpOpF32(context, CmpCondition.GreaterThan, false);
            }
            else
            {
                EmitCmpOpF32(context, nameof(SoftFloat32.FPCompareGTFpscr), false);
            }
        }

        public static void Vcgt_I(ArmEmitterContext context)
        {
            OpCode32SimdReg op = (OpCode32SimdReg)context.CurrOp;

            EmitCmpOpI32(context, context.ICompareGreater, context.ICompareGreaterUI, false, !op.U);
        }

        public static void Vcgt_Z(ArmEmitterContext context)
        {
            OpCode32Simd op = (OpCode32Simd)context.CurrOp;

            if (op.F)
            {
                if (Optimizations.FastFP && Optimizations.UseAdvSimd)
                {
                    InstEmitSimdHelper32Arm64.EmitCmpOpF32(context, CmpCondition.GreaterThan, true);
                }
                else if (Optimizations.FastFP && Optimizations.UseAvx)
                {
                    EmitSse2OrAvxCmpOpF32(context, CmpCondition.GreaterThan, true);
                }
                else
                {
                    EmitCmpOpF32(context, nameof(SoftFloat32.FPCompareGTFpscr), true);
                }
            }
            else
            {
                EmitCmpOpI32(context, context.ICompareGreater, context.ICompareGreaterUI, true, true);
            }
        }

        public static void Vcle_Z(ArmEmitterContext context)
        {
            OpCode32Simd op = (OpCode32Simd)context.CurrOp;

            if (op.F)
            {
                if (Optimizations.FastFP && Optimizations.UseAdvSimd)
                {
                    InstEmitSimdHelper32Arm64.EmitCmpOpF32(context, CmpCondition.LessThanOrEqual, true);
                }
                else if (Optimizations.FastFP && Optimizations.UseSse2)
                {
                    EmitSse2OrAvxCmpOpF32(context, CmpCondition.LessThanOrEqual, true);
                }
                else
                {
                    EmitCmpOpF32(context, nameof(SoftFloat32.FPCompareLEFpscr), true);
                }
            }
            else
            {
                EmitCmpOpI32(context, context.ICompareLessOrEqual, context.ICompareLessOrEqualUI, true, true);
            }
        }

        public static void Vclt_Z(ArmEmitterContext context)
        {
            OpCode32Simd op = (OpCode32Simd)context.CurrOp;

            if (op.F)
            {
                if (Optimizations.FastFP && Optimizations.UseAdvSimd)
                {
                    InstEmitSimdHelper32Arm64.EmitCmpOpF32(context, CmpCondition.LessThan, true);
                }
                else if (Optimizations.FastFP && Optimizations.UseSse2)
                {
                    EmitSse2OrAvxCmpOpF32(context, CmpCondition.LessThan, true);
                }
                else
                {
                    EmitCmpOpF32(context, nameof(SoftFloat32.FPCompareLTFpscr), true);
                }
            }
            else
            {
                EmitCmpOpI32(context, context.ICompareLess, context.ICompareLessUI, true, true);
            }
        }

        private static void EmitCmpOpF32(ArmEmitterContext context, string name, bool zero)
        {
            if (zero)
            {
                EmitVectorUnaryOpF32(context, (m) =>
                {
                    Operand zeroOp = m.Type == OperandType.FP64 ? ConstF(0.0d) : ConstF(0.0f);

                    return EmitSoftFloatCallDefaultFpscr(context, name, m, zeroOp);
                });
            }
            else
            {
                EmitVectorBinaryOpF32(context, (n, m) =>
                {
                    return EmitSoftFloatCallDefaultFpscr(context, name, n, m);
                });
            }
        }

        private static Operand ZerosOrOnes(ArmEmitterContext context, Operand fromBool, OperandType baseType)
        {
            Operand ones = (baseType == OperandType.I64) ? Const(-1L) : Const(-1);

            return context.ConditionalSelect(fromBool, ones, Const(baseType, 0L));
        }

        private static void EmitCmpOpI32(
            ArmEmitterContext context,
            Func2I signedOp,
            Func2I unsignedOp,
            bool zero,
            bool signed)
        {
            if (zero)
            {
                if (signed)
                {
                    EmitVectorUnaryOpSx32(context, (m) =>
                    {
                        OperandType type = m.Type;
                        Operand zeroV = (type == OperandType.I64) ? Const(0L) : Const(0);

                        return ZerosOrOnes(context, signedOp(m, zeroV), type);
                    });
                }
                else
                {
                    EmitVectorUnaryOpZx32(context, (m) =>
                    {
                        OperandType type = m.Type;
                        Operand zeroV = (type == OperandType.I64) ? Const(0L) : Const(0);

                        return ZerosOrOnes(context, unsignedOp(m, zeroV), type);
                    });
                }
            }
            else
            {
                if (signed)
                {
                    EmitVectorBinaryOpSx32(context, (n, m) => ZerosOrOnes(context, signedOp(n, m), n.Type));
                }
                else
                {
                    EmitVectorBinaryOpZx32(context, (n, m) => ZerosOrOnes(context, unsignedOp(n, m), n.Type));
                }
            }
        }

        public static void Vcmp(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelper32Arm64.EmitVcmpOrVcmpe(context, false);
            }
            else
            {
                EmitVcmpOrVcmpe(context, false);
            }
        }

        public static void Vcmpe(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelper32Arm64.EmitVcmpOrVcmpe(context, true);
            }
            else
            {
                EmitVcmpOrVcmpe(context, true);
            }
        }

        private static void EmitVcmpOrVcmpe(ArmEmitterContext context, bool signalNaNs)
        {
            OpCode32SimdS op = (OpCode32SimdS)context.CurrOp;

            bool cmpWithZero = (op.Opc & 2) != 0;
            int sizeF = op.Size & 1;

            if (Optimizations.FastFP && (signalNaNs ? Optimizations.UseAvx : Optimizations.UseSse2))
            {
                CmpCondition cmpOrdered = signalNaNs ? CmpCondition.OrderedS : CmpCondition.OrderedQ;

                bool doubleSize = sizeF != 0;
                int shift = doubleSize ? 1 : 2;
                Operand m = GetVecA32(op.Vm >> shift);
                Operand n = GetVecA32(op.Vd >> shift);

                n = EmitSwapScalar(context, n, op.Vd, doubleSize);
                m = cmpWithZero ? context.VectorZero() : EmitSwapScalar(context, m, op.Vm, doubleSize);

                Operand lblNaN = Label();
                Operand lblEnd = Label();

                if (!doubleSize)
                {
                    Operand ordMask = context.AddIntrinsic(Intrinsic.X86Cmpss, n, m, Const((int)cmpOrdered));

                    Operand isOrdered = context.AddIntrinsicInt(Intrinsic.X86Cvtsi2si, ordMask);

                    context.BranchIfFalse(lblNaN, isOrdered);

                    Operand cf = context.AddIntrinsicInt(Intrinsic.X86Comissge, n, m);
                    Operand zf = context.AddIntrinsicInt(Intrinsic.X86Comisseq, n, m);
                    Operand nf = context.AddIntrinsicInt(Intrinsic.X86Comisslt, n, m);

                    SetFpFlag(context, FPState.VFlag, Const(0));
                    SetFpFlag(context, FPState.CFlag, cf);
                    SetFpFlag(context, FPState.ZFlag, zf);
                    SetFpFlag(context, FPState.NFlag, nf);
                }
                else
                {
                    Operand ordMask = context.AddIntrinsic(Intrinsic.X86Cmpsd, n, m, Const((int)cmpOrdered));

                    Operand isOrdered = context.AddIntrinsicLong(Intrinsic.X86Cvtsi2si, ordMask);

                    context.BranchIfFalse(lblNaN, isOrdered);

                    Operand cf = context.AddIntrinsicInt(Intrinsic.X86Comisdge, n, m);
                    Operand zf = context.AddIntrinsicInt(Intrinsic.X86Comisdeq, n, m);
                    Operand nf = context.AddIntrinsicInt(Intrinsic.X86Comisdlt, n, m);

                    SetFpFlag(context, FPState.VFlag, Const(0));
                    SetFpFlag(context, FPState.CFlag, cf);
                    SetFpFlag(context, FPState.ZFlag, zf);
                    SetFpFlag(context, FPState.NFlag, nf);
                }

                context.Branch(lblEnd);

                context.MarkLabel(lblNaN);

                SetFpFlag(context, FPState.VFlag, Const(1));
                SetFpFlag(context, FPState.CFlag, Const(1));
                SetFpFlag(context, FPState.ZFlag, Const(0));
                SetFpFlag(context, FPState.NFlag, Const(0));

                context.MarkLabel(lblEnd);
            }
            else
            {
                OperandType type = sizeF != 0 ? OperandType.FP64 : OperandType.FP32;

                Operand ne = ExtractScalar(context, type, op.Vd);
                Operand me;

                if (cmpWithZero)
                {
                    me = sizeF == 0 ? ConstF(0f) : ConstF(0d);
                }
                else
                {
                    me = ExtractScalar(context, type, op.Vm);
                }

                Operand nzcv = EmitSoftFloatCall(context, nameof(SoftFloat32.FPCompare), ne, me, Const(signalNaNs));

                EmitSetFpscrNzcv(context, nzcv);
            }
        }

        private static void EmitSetFpscrNzcv(ArmEmitterContext context, Operand nzcv)
        {
            Operand Extract(Operand value, int bit)
            {
                if (bit != 0)
                {
                    value = context.ShiftRightUI(value, Const(bit));
                }

                value = context.BitwiseAnd(value, Const(1));

                return value;
            }

            SetFpFlag(context, FPState.VFlag, Extract(nzcv, 0));
            SetFpFlag(context, FPState.CFlag, Extract(nzcv, 1));
            SetFpFlag(context, FPState.ZFlag, Extract(nzcv, 2));
            SetFpFlag(context, FPState.NFlag, Extract(nzcv, 3));
        }

        private static void EmitSse2OrAvxCmpOpF32(ArmEmitterContext context, CmpCondition cond, bool zero)
        {
            OpCode32Simd op = (OpCode32Simd)context.CurrOp;

            int sizeF = op.Size & 1;
            Intrinsic inst = (sizeF == 0) ? Intrinsic.X86Cmpps : Intrinsic.X86Cmppd;

            if (zero)
            {
                EmitVectorUnaryOpSimd32(context, (m) =>
                {
                    return context.AddIntrinsic(inst, m, context.VectorZero(), Const((int)cond));
                });
            }
            else
            {
                EmitVectorBinaryOpSimd32(context, (n, m) =>
                {
                    return context.AddIntrinsic(inst, n, m, Const((int)cond));
                });
            }
        }
    }
}
