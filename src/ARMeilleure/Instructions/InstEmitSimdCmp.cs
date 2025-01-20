using ARMeilleure.Decoders;
using ARMeilleure.IntermediateRepresentation;
using ARMeilleure.State;
using ARMeilleure.Translation;
using System;
using static ARMeilleure.Instructions.InstEmitHelper;
using static ARMeilleure.Instructions.InstEmitSimdHelper;
using static ARMeilleure.IntermediateRepresentation.Operand.Factory;

namespace ARMeilleure.Instructions
{
    using Func2I = Func<Operand, Operand, Operand>;

    static partial class InstEmit
    {
        public static void Cmeq_S(ArmEmitterContext context)
        {
            EmitCmpOp(context, (op1, op2) => context.ICompareEqual(op1, op2), scalar: true);
        }

        public static void Cmeq_V(ArmEmitterContext context)
        {
            if (Optimizations.UseSse41)
            {
                OpCodeSimd op = (OpCodeSimd)context.CurrOp;

                Operand n = GetVec(op.Rn);
                Operand m;

                if (op is OpCodeSimdReg binOp)
                {
                    m = GetVec(binOp.Rm);
                }
                else
                {
                    m = context.VectorZero();
                }

                Intrinsic cmpInst = X86PcmpeqInstruction[op.Size];

                Operand res = context.AddIntrinsic(cmpInst, n, m);

                if (op.RegisterSize == RegisterSize.Simd64)
                {
                    res = context.VectorZeroUpper64(res);
                }

                context.Copy(GetVec(op.Rd), res);
            }
            else
            {
                EmitCmpOp(context, (op1, op2) => context.ICompareEqual(op1, op2), scalar: false);
            }
        }

        public static void Cmge_S(ArmEmitterContext context)
        {
            EmitCmpOp(context, (op1, op2) => context.ICompareGreaterOrEqual(op1, op2), scalar: true);
        }

        public static void Cmge_V(ArmEmitterContext context)
        {
            if (Optimizations.UseSse42)
            {
                OpCodeSimd op = (OpCodeSimd)context.CurrOp;

                Operand n = GetVec(op.Rn);
                Operand m;

                if (op is OpCodeSimdReg binOp)
                {
                    m = GetVec(binOp.Rm);
                }
                else
                {
                    m = context.VectorZero();
                }

                Intrinsic cmpInst = X86PcmpgtInstruction[op.Size];

                Operand res = context.AddIntrinsic(cmpInst, m, n);

                Operand mask = X86GetAllElements(context, -1L);

                res = context.AddIntrinsic(Intrinsic.X86Pandn, res, mask);

                if (op.RegisterSize == RegisterSize.Simd64)
                {
                    res = context.VectorZeroUpper64(res);
                }

                context.Copy(GetVec(op.Rd), res);
            }
            else
            {
                EmitCmpOp(context, (op1, op2) => context.ICompareGreaterOrEqual(op1, op2), scalar: false);
            }
        }

        public static void Cmgt_S(ArmEmitterContext context)
        {
            EmitCmpOp(context, (op1, op2) => context.ICompareGreater(op1, op2), scalar: true);
        }

        public static void Cmgt_V(ArmEmitterContext context)
        {
            if (Optimizations.UseSse42)
            {
                OpCodeSimd op = (OpCodeSimd)context.CurrOp;

                Operand n = GetVec(op.Rn);
                Operand m;

                if (op is OpCodeSimdReg binOp)
                {
                    m = GetVec(binOp.Rm);
                }
                else
                {
                    m = context.VectorZero();
                }

                Intrinsic cmpInst = X86PcmpgtInstruction[op.Size];

                Operand res = context.AddIntrinsic(cmpInst, n, m);

                if (op.RegisterSize == RegisterSize.Simd64)
                {
                    res = context.VectorZeroUpper64(res);
                }

                context.Copy(GetVec(op.Rd), res);
            }
            else
            {
                EmitCmpOp(context, (op1, op2) => context.ICompareGreater(op1, op2), scalar: false);
            }
        }

        public static void Cmhi_S(ArmEmitterContext context)
        {
            EmitCmpOp(context, (op1, op2) => context.ICompareGreaterUI(op1, op2), scalar: true);
        }

        public static void Cmhi_V(ArmEmitterContext context)
        {
            OpCodeSimdReg op = (OpCodeSimdReg)context.CurrOp;

            if (Optimizations.UseSse41 && op.Size < 3)
            {
                Operand n = GetVec(op.Rn);
                Operand m = GetVec(op.Rm);

                Intrinsic maxInst = X86PmaxuInstruction[op.Size];

                Operand res = context.AddIntrinsic(maxInst, m, n);

                Intrinsic cmpInst = X86PcmpeqInstruction[op.Size];

                res = context.AddIntrinsic(cmpInst, res, m);

                Operand mask = X86GetAllElements(context, -1L);

                res = context.AddIntrinsic(Intrinsic.X86Pandn, res, mask);

                if (op.RegisterSize == RegisterSize.Simd64)
                {
                    res = context.VectorZeroUpper64(res);
                }

                context.Copy(GetVec(op.Rd), res);
            }
            else
            {
                EmitCmpOp(context, (op1, op2) => context.ICompareGreaterUI(op1, op2), scalar: false);
            }
        }

        public static void Cmhs_S(ArmEmitterContext context)
        {
            EmitCmpOp(context, (op1, op2) => context.ICompareGreaterOrEqualUI(op1, op2), scalar: true);
        }

        public static void Cmhs_V(ArmEmitterContext context)
        {
            OpCodeSimdReg op = (OpCodeSimdReg)context.CurrOp;

            if (Optimizations.UseSse41 && op.Size < 3)
            {
                Operand n = GetVec(op.Rn);
                Operand m = GetVec(op.Rm);

                Intrinsic maxInst = X86PmaxuInstruction[op.Size];

                Operand res = context.AddIntrinsic(maxInst, n, m);

                Intrinsic cmpInst = X86PcmpeqInstruction[op.Size];

                res = context.AddIntrinsic(cmpInst, res, n);

                if (op.RegisterSize == RegisterSize.Simd64)
                {
                    res = context.VectorZeroUpper64(res);
                }

                context.Copy(GetVec(op.Rd), res);
            }
            else
            {
                EmitCmpOp(context, (op1, op2) => context.ICompareGreaterOrEqualUI(op1, op2), scalar: false);
            }
        }

        public static void Cmle_S(ArmEmitterContext context)
        {
            EmitCmpOp(context, (op1, op2) => context.ICompareLessOrEqual(op1, op2), scalar: true);
        }

        public static void Cmle_V(ArmEmitterContext context)
        {
            if (Optimizations.UseSse42)
            {
                OpCodeSimd op = (OpCodeSimd)context.CurrOp;

                Operand n = GetVec(op.Rn);

                Intrinsic cmpInst = X86PcmpgtInstruction[op.Size];

                Operand res = context.AddIntrinsic(cmpInst, n, context.VectorZero());

                Operand mask = X86GetAllElements(context, -1L);

                res = context.AddIntrinsic(Intrinsic.X86Pandn, res, mask);

                if (op.RegisterSize == RegisterSize.Simd64)
                {
                    res = context.VectorZeroUpper64(res);
                }

                context.Copy(GetVec(op.Rd), res);
            }
            else
            {
                EmitCmpOp(context, (op1, op2) => context.ICompareLessOrEqual(op1, op2), scalar: false);
            }
        }

        public static void Cmlt_S(ArmEmitterContext context)
        {
            EmitCmpOp(context, (op1, op2) => context.ICompareLess(op1, op2), scalar: true);
        }

        public static void Cmlt_V(ArmEmitterContext context)
        {
            if (Optimizations.UseSse42)
            {
                OpCodeSimd op = (OpCodeSimd)context.CurrOp;

                Operand n = GetVec(op.Rn);

                Intrinsic cmpInst = X86PcmpgtInstruction[op.Size];

                Operand res = context.AddIntrinsic(cmpInst, context.VectorZero(), n);

                if (op.RegisterSize == RegisterSize.Simd64)
                {
                    res = context.VectorZeroUpper64(res);
                }

                context.Copy(GetVec(op.Rd), res);
            }
            else
            {
                EmitCmpOp(context, (op1, op2) => context.ICompareLess(op1, op2), scalar: false);
            }
        }

        public static void Cmtst_S(ArmEmitterContext context)
        {
            EmitCmtstOp(context, scalar: true);
        }

        public static void Cmtst_V(ArmEmitterContext context)
        {
            EmitCmtstOp(context, scalar: false);
        }

        public static void Facge_S(ArmEmitterContext context)
        {
            if (Optimizations.FastFP && Optimizations.UseAvx)
            {
                EmitSse2OrAvxCmpOpF(context, CmpCondition.GreaterThanOrEqual, scalar: true, absolute: true);
            }
            else
            {
                EmitCmpOpF(context, nameof(SoftFloat32.FPCompareGE), scalar: true, absolute: true);
            }
        }

        public static void Facge_V(ArmEmitterContext context)
        {
            if (Optimizations.FastFP && Optimizations.UseAvx)
            {
                EmitSse2OrAvxCmpOpF(context, CmpCondition.GreaterThanOrEqual, scalar: false, absolute: true);
            }
            else
            {
                EmitCmpOpF(context, nameof(SoftFloat32.FPCompareGE), scalar: false, absolute: true);
            }
        }

        public static void Facgt_S(ArmEmitterContext context)
        {
            if (Optimizations.FastFP && Optimizations.UseAvx)
            {
                EmitSse2OrAvxCmpOpF(context, CmpCondition.GreaterThan, scalar: true, absolute: true);
            }
            else
            {
                EmitCmpOpF(context, nameof(SoftFloat32.FPCompareGT), scalar: true, absolute: true);
            }
        }

        public static void Facgt_V(ArmEmitterContext context)
        {
            if (Optimizations.FastFP && Optimizations.UseAvx)
            {
                EmitSse2OrAvxCmpOpF(context, CmpCondition.GreaterThan, scalar: false, absolute: true);
            }
            else
            {
                EmitCmpOpF(context, nameof(SoftFloat32.FPCompareGT), scalar: false, absolute: true);
            }
        }

        public static void Fccmp_S(ArmEmitterContext context)
        {
            EmitFccmpOrFccmpe(context, signalNaNs: false);
        }

        public static void Fccmpe_S(ArmEmitterContext context)
        {
            EmitFccmpOrFccmpe(context, signalNaNs: true);
        }

        public static void Fcmeq_S(ArmEmitterContext context)
        {
            if (Optimizations.FastFP && Optimizations.UseSse2)
            {
                EmitSse2OrAvxCmpOpF(context, CmpCondition.Equal, scalar: true);
            }
            else
            {
                EmitCmpOpF(context, nameof(SoftFloat32.FPCompareEQ), scalar: true);
            }
        }

        public static void Fcmeq_V(ArmEmitterContext context)
        {
            if (Optimizations.FastFP && Optimizations.UseSse2)
            {
                EmitSse2OrAvxCmpOpF(context, CmpCondition.Equal, scalar: false);
            }
            else
            {
                EmitCmpOpF(context, nameof(SoftFloat32.FPCompareEQ), scalar: false);
            }
        }

        public static void Fcmge_S(ArmEmitterContext context)
        {
            if (Optimizations.FastFP && Optimizations.UseAvx)
            {
                EmitSse2OrAvxCmpOpF(context, CmpCondition.GreaterThanOrEqual, scalar: true);
            }
            else
            {
                EmitCmpOpF(context, nameof(SoftFloat32.FPCompareGE), scalar: true);
            }
        }

        public static void Fcmge_V(ArmEmitterContext context)
        {
            if (Optimizations.FastFP && Optimizations.UseAvx)
            {
                EmitSse2OrAvxCmpOpF(context, CmpCondition.GreaterThanOrEqual, scalar: false);
            }
            else
            {
                EmitCmpOpF(context, nameof(SoftFloat32.FPCompareGE), scalar: false);
            }
        }

        public static void Fcmgt_S(ArmEmitterContext context)
        {
            if (Optimizations.FastFP && Optimizations.UseAvx)
            {
                EmitSse2OrAvxCmpOpF(context, CmpCondition.GreaterThan, scalar: true);
            }
            else
            {
                EmitCmpOpF(context, nameof(SoftFloat32.FPCompareGT), scalar: true);
            }
        }

        public static void Fcmgt_V(ArmEmitterContext context)
        {
            if (Optimizations.FastFP && Optimizations.UseAvx)
            {
                EmitSse2OrAvxCmpOpF(context, CmpCondition.GreaterThan, scalar: false);
            }
            else
            {
                EmitCmpOpF(context, nameof(SoftFloat32.FPCompareGT), scalar: false);
            }
        }

        public static void Fcmle_S(ArmEmitterContext context)
        {
            if (Optimizations.FastFP && Optimizations.UseSse2)
            {
                EmitSse2OrAvxCmpOpF(context, CmpCondition.LessThanOrEqual, scalar: true);
            }
            else
            {
                EmitCmpOpF(context, nameof(SoftFloat32.FPCompareLE), scalar: true);
            }
        }

        public static void Fcmle_V(ArmEmitterContext context)
        {
            if (Optimizations.FastFP && Optimizations.UseSse2)
            {
                EmitSse2OrAvxCmpOpF(context, CmpCondition.LessThanOrEqual, scalar: false);
            }
            else
            {
                EmitCmpOpF(context, nameof(SoftFloat32.FPCompareLE), scalar: false);
            }
        }

        public static void Fcmlt_S(ArmEmitterContext context)
        {
            if (Optimizations.FastFP && Optimizations.UseSse2)
            {
                EmitSse2OrAvxCmpOpF(context, CmpCondition.LessThan, scalar: true);
            }
            else
            {
                EmitCmpOpF(context, nameof(SoftFloat32.FPCompareLT), scalar: true);
            }
        }

        public static void Fcmlt_V(ArmEmitterContext context)
        {
            if (Optimizations.FastFP && Optimizations.UseSse2)
            {
                EmitSse2OrAvxCmpOpF(context, CmpCondition.LessThan, scalar: false);
            }
            else
            {
                EmitCmpOpF(context, nameof(SoftFloat32.FPCompareLT), scalar: false);
            }
        }

        public static void Fcmp_S(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitFcmpOrFcmpe(context, signalNaNs: false);
            }
            else
            {
                EmitFcmpOrFcmpe(context, signalNaNs: false);
            }
        }

        public static void Fcmpe_S(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitFcmpOrFcmpe(context, signalNaNs: true);
            }
            else
            {
                EmitFcmpOrFcmpe(context, signalNaNs: true);
            }
        }

        private static void EmitFccmpOrFccmpe(ArmEmitterContext context, bool signalNaNs)
        {
            OpCodeSimdFcond op = (OpCodeSimdFcond)context.CurrOp;

            Operand lblTrue = Label();
            Operand lblEnd = Label();

            context.BranchIfTrue(lblTrue, InstEmitFlowHelper.GetCondTrue(context, op.Cond));

            EmitSetNzcv(context, op.Nzcv);

            context.Branch(lblEnd);

            context.MarkLabel(lblTrue);

            EmitFcmpOrFcmpe(context, signalNaNs);

            context.MarkLabel(lblEnd);
        }

        private static void EmitSetNzcv(ArmEmitterContext context, int nzcv)
        {
            static Operand Extract(int value, int bit)
            {
                if (bit != 0)
                {
                    value >>= bit;
                }

                value &= 1;

                return Const(value);
            }

            SetFlag(context, PState.VFlag, Extract(nzcv, 0));
            SetFlag(context, PState.CFlag, Extract(nzcv, 1));
            SetFlag(context, PState.ZFlag, Extract(nzcv, 2));
            SetFlag(context, PState.NFlag, Extract(nzcv, 3));
        }

        private static void EmitFcmpOrFcmpe(ArmEmitterContext context, bool signalNaNs)
        {
            OpCodeSimdReg op = (OpCodeSimdReg)context.CurrOp;

            bool cmpWithZero = op is not OpCodeSimdFcond && op.Bit3;

            if (Optimizations.FastFP && (signalNaNs ? Optimizations.UseAvx : Optimizations.UseSse2))
            {
                Operand n = GetVec(op.Rn);
                Operand m = cmpWithZero ? context.VectorZero() : GetVec(op.Rm);

                CmpCondition cmpOrdered = signalNaNs ? CmpCondition.OrderedS : CmpCondition.OrderedQ;

                Operand lblNaN = Label();
                Operand lblEnd = Label();

                if (op.Size == 0)
                {
                    Operand ordMask = context.AddIntrinsic(Intrinsic.X86Cmpss, n, m, Const((int)cmpOrdered));

                    Operand isOrdered = context.AddIntrinsicInt(Intrinsic.X86Cvtsi2si, ordMask);

                    context.BranchIfFalse(lblNaN, isOrdered);

                    Operand nCopy = context.Copy(n);
                    Operand mCopy = cmpWithZero ? context.VectorZero() : context.Copy(m);

                    Operand cf = context.AddIntrinsicInt(Intrinsic.X86Comissge, nCopy, mCopy);
                    Operand zf = context.AddIntrinsicInt(Intrinsic.X86Comisseq, nCopy, mCopy);
                    Operand nf = context.AddIntrinsicInt(Intrinsic.X86Comisslt, nCopy, mCopy);

                    SetFlag(context, PState.VFlag, Const(0));
                    SetFlag(context, PState.CFlag, cf);
                    SetFlag(context, PState.ZFlag, zf);
                    SetFlag(context, PState.NFlag, nf);
                }
                else /* if (op.Size == 1) */
                {
                    Operand ordMask = context.AddIntrinsic(Intrinsic.X86Cmpsd, n, m, Const((int)cmpOrdered));

                    Operand isOrdered = context.AddIntrinsicLong(Intrinsic.X86Cvtsi2si, ordMask);

                    context.BranchIfFalse(lblNaN, isOrdered);

                    Operand nCopy = context.Copy(n);
                    Operand mCopy = cmpWithZero ? context.VectorZero() : context.Copy(m);

                    Operand cf = context.AddIntrinsicInt(Intrinsic.X86Comisdge, nCopy, mCopy);
                    Operand zf = context.AddIntrinsicInt(Intrinsic.X86Comisdeq, nCopy, mCopy);
                    Operand nf = context.AddIntrinsicInt(Intrinsic.X86Comisdlt, nCopy, mCopy);

                    SetFlag(context, PState.VFlag, Const(0));
                    SetFlag(context, PState.CFlag, cf);
                    SetFlag(context, PState.ZFlag, zf);
                    SetFlag(context, PState.NFlag, nf);
                }

                context.Branch(lblEnd);

                context.MarkLabel(lblNaN);

                SetFlag(context, PState.VFlag, Const(1));
                SetFlag(context, PState.CFlag, Const(1));
                SetFlag(context, PState.ZFlag, Const(0));
                SetFlag(context, PState.NFlag, Const(0));

                context.MarkLabel(lblEnd);
            }
            else
            {
                OperandType type = op.Size != 0 ? OperandType.FP64 : OperandType.FP32;

                Operand ne = context.VectorExtract(type, GetVec(op.Rn), 0);
                Operand me;

                if (cmpWithZero)
                {
                    me = op.Size == 0 ? ConstF(0f) : ConstF(0d);
                }
                else
                {
                    me = context.VectorExtract(type, GetVec(op.Rm), 0);
                }

                Operand nzcv = EmitSoftFloatCall(context, nameof(SoftFloat32.FPCompare), ne, me, Const(signalNaNs));

                EmitSetNzcv(context, nzcv);
            }
        }

        private static void EmitSetNzcv(ArmEmitterContext context, Operand nzcv)
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

            SetFlag(context, PState.VFlag, Extract(nzcv, 0));
            SetFlag(context, PState.CFlag, Extract(nzcv, 1));
            SetFlag(context, PState.ZFlag, Extract(nzcv, 2));
            SetFlag(context, PState.NFlag, Extract(nzcv, 3));
        }

        private static void EmitCmpOp(ArmEmitterContext context, Func2I emitCmp, bool scalar)
        {
            OpCodeSimd op = (OpCodeSimd)context.CurrOp;

            Operand res = context.VectorZero();

            int elems = !scalar ? op.GetBytesCount() >> op.Size : 1;

            ulong szMask = ulong.MaxValue >> (64 - (8 << op.Size));

            for (int index = 0; index < elems; index++)
            {
                Operand ne = EmitVectorExtractSx(context, op.Rn, index, op.Size);
                Operand me;

                if (op is OpCodeSimdReg binOp)
                {
                    me = EmitVectorExtractSx(context, binOp.Rm, index, op.Size);
                }
                else
                {
                    me = Const(0L);
                }

                Operand isTrue = emitCmp(ne, me);

                Operand mask = context.ConditionalSelect(isTrue, Const(szMask), Const(0L));

                res = EmitVectorInsert(context, res, mask, index, op.Size);
            }

            context.Copy(GetVec(op.Rd), res);
        }

        private static void EmitCmtstOp(ArmEmitterContext context, bool scalar)
        {
            OpCodeSimdReg op = (OpCodeSimdReg)context.CurrOp;

            Operand res = context.VectorZero();

            int elems = !scalar ? op.GetBytesCount() >> op.Size : 1;

            ulong szMask = ulong.MaxValue >> (64 - (8 << op.Size));

            for (int index = 0; index < elems; index++)
            {
                Operand ne = EmitVectorExtractZx(context, op.Rn, index, op.Size);
                Operand me = EmitVectorExtractZx(context, op.Rm, index, op.Size);

                Operand test = context.BitwiseAnd(ne, me);

                Operand isTrue = context.ICompareNotEqual(test, Const(0L));

                Operand mask = context.ConditionalSelect(isTrue, Const(szMask), Const(0L));

                res = EmitVectorInsert(context, res, mask, index, op.Size);
            }

            context.Copy(GetVec(op.Rd), res);
        }

        private static void EmitCmpOpF(ArmEmitterContext context, string name, bool scalar, bool absolute = false)
        {
            OpCodeSimd op = (OpCodeSimd)context.CurrOp;

            Operand res = context.VectorZero();

            int sizeF = op.Size & 1;

            OperandType type = sizeF != 0 ? OperandType.FP64 : OperandType.FP32;

            int elems = !scalar ? op.GetBytesCount() >> sizeF + 2 : 1;

            for (int index = 0; index < elems; index++)
            {
                Operand ne = context.VectorExtract(type, GetVec(op.Rn), index);
                Operand me;

                if (op is OpCodeSimdReg binOp)
                {
                    me = context.VectorExtract(type, GetVec(binOp.Rm), index);
                }
                else
                {
                    me = sizeF == 0 ? ConstF(0f) : ConstF(0d);
                }

                if (absolute)
                {
                    ne = EmitUnaryMathCall(context, nameof(MathHelper.Abs), ne);
                    me = EmitUnaryMathCall(context, nameof(MathHelper.Abs), me);
                }

                Operand e = EmitSoftFloatCall(context, name, ne, me);

                res = context.VectorInsert(res, e, index);
            }

            context.Copy(GetVec(op.Rd), res);
        }

        private static void EmitSse2OrAvxCmpOpF(ArmEmitterContext context, CmpCondition cond, bool scalar, bool absolute = false)
        {
            OpCodeSimd op = (OpCodeSimd)context.CurrOp;

            Operand n = GetVec(op.Rn);
            Operand m = op is OpCodeSimdReg binOp ? GetVec(binOp.Rm) : context.VectorZero();

            int sizeF = op.Size & 1;

            if (sizeF == 0)
            {
                if (absolute)
                {
                    Operand mask = scalar ? X86GetScalar(context, int.MaxValue) : X86GetAllElements(context, int.MaxValue);

                    n = context.AddIntrinsic(Intrinsic.X86Andps, n, mask);
                    m = context.AddIntrinsic(Intrinsic.X86Andps, m, mask);
                }

                Intrinsic inst = scalar ? Intrinsic.X86Cmpss : Intrinsic.X86Cmpps;

                Operand res = context.AddIntrinsic(inst, n, m, Const((int)cond));

                if (scalar)
                {
                    res = context.VectorZeroUpper96(res);
                }
                else if (op.RegisterSize == RegisterSize.Simd64)
                {
                    res = context.VectorZeroUpper64(res);
                }

                context.Copy(GetVec(op.Rd), res);
            }
            else /* if (sizeF == 1) */
            {
                if (absolute)
                {
                    Operand mask = scalar ? X86GetScalar(context, long.MaxValue) : X86GetAllElements(context, long.MaxValue);

                    n = context.AddIntrinsic(Intrinsic.X86Andpd, n, mask);
                    m = context.AddIntrinsic(Intrinsic.X86Andpd, m, mask);
                }

                Intrinsic inst = scalar ? Intrinsic.X86Cmpsd : Intrinsic.X86Cmppd;

                Operand res = context.AddIntrinsic(inst, n, m, Const((int)cond));

                if (scalar)
                {
                    res = context.VectorZeroUpper64(res);
                }

                context.Copy(GetVec(op.Rd), res);
            }
        }
    }
}
