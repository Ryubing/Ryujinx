using Ryujinx.Graphics.Shader.Decoders;
using Ryujinx.Graphics.Shader.IntermediateRepresentation;
using Ryujinx.Graphics.Shader.Translation;
using System;
using static Ryujinx.Graphics.Shader.Instructions.InstEmitAluHelper;
using static Ryujinx.Graphics.Shader.Instructions.InstEmitHelper;
using static Ryujinx.Graphics.Shader.IntermediateRepresentation.OperandHelper;

namespace Ryujinx.Graphics.Shader.Instructions
{
    static partial class InstEmit
    {
        public static void DsetR(EmitterContext context)
        {
            InstDsetR op = context.GetOp<InstDsetR>();

            Operand srcA = GetSrcReg(context, op.SrcA, isFP64: true);
            Operand srcB = GetSrcReg(context, op.SrcB, isFP64: true);

            EmitFset(
                context,
                op.FComp,
                op.Bop,
                srcA,
                srcB,
                op.SrcPred,
                op.SrcPredInv,
                op.Dest,
                op.AbsA,
                op.AbsB,
                op.NegA,
                op.NegB,
                op.BVal,
                op.WriteCC,
                isFP64: true);
        }

        public static void DsetI(EmitterContext context)
        {
            InstDsetI op = context.GetOp<InstDsetI>();

            Operand srcA = GetSrcReg(context, op.SrcA, isFP64: true);
            Operand srcB = GetSrcImm(context, Imm20ToFloat(op.Imm20), isFP64: true);

            EmitFset(
                context,
                op.FComp,
                op.Bop,
                srcA,
                srcB,
                op.SrcPred,
                op.SrcPredInv,
                op.Dest,
                op.AbsA,
                op.AbsB,
                op.NegA,
                op.NegB,
                op.BVal,
                op.WriteCC,
                isFP64: true);
        }

        public static void DsetC(EmitterContext context)
        {
            InstDsetC op = context.GetOp<InstDsetC>();

            Operand srcA = GetSrcReg(context, op.SrcA, isFP64: true);
            Operand srcB = GetSrcCbuf(context, op.CbufSlot, op.CbufOffset, isFP64: true);

            EmitFset(
                context,
                op.FComp,
                op.Bop,
                srcA,
                srcB,
                op.SrcPred,
                op.SrcPredInv,
                op.Dest,
                op.AbsA,
                op.AbsB,
                op.NegA,
                op.NegB,
                op.BVal,
                op.WriteCC,
                isFP64: true);
        }

        public static void DsetpR(EmitterContext context)
        {
            InstDsetpR op = context.GetOp<InstDsetpR>();

            Operand srcA = GetSrcReg(context, op.SrcA, isFP64: true);
            Operand srcB = GetSrcReg(context, op.SrcB, isFP64: true);

            EmitFsetp(
                context,
                op.FComp,
                op.Bop,
                srcA,
                srcB,
                op.SrcPred,
                op.SrcPredInv,
                op.DestPred,
                op.DestPredInv,
                op.AbsA,
                op.AbsB,
                op.NegA,
                op.NegB,
                writeCC: false,
                isFP64: true);
        }

        public static void DsetpI(EmitterContext context)
        {
            InstDsetpI op = context.GetOp<InstDsetpI>();

            Operand srcA = GetSrcReg(context, op.SrcA, isFP64: true);
            Operand srcB = GetSrcImm(context, Imm20ToFloat(op.Imm20), isFP64: true);

            EmitFsetp(
                context,
                op.FComp,
                op.Bop,
                srcA,
                srcB,
                op.SrcPred,
                op.SrcPredInv,
                op.DestPred,
                op.DestPredInv,
                op.AbsA,
                op.AbsB,
                op.NegA,
                op.NegB,
                writeCC: false,
                isFP64: true);
        }

        public static void DsetpC(EmitterContext context)
        {
            InstDsetpC op = context.GetOp<InstDsetpC>();

            Operand srcA = GetSrcReg(context, op.SrcA, isFP64: true);
            Operand srcB = GetSrcCbuf(context, op.CbufSlot, op.CbufOffset, isFP64: true);

            EmitFsetp(
                context,
                op.FComp,
                op.Bop,
                srcA,
                srcB,
                op.SrcPred,
                op.SrcPredInv,
                op.DestPred,
                op.DestPredInv,
                op.AbsA,
                op.AbsB,
                op.NegA,
                op.NegB,
                writeCC: false,
                isFP64: true);
        }

        public static void FcmpR(EmitterContext context)
        {
            InstFcmpR op = context.GetOp<InstFcmpR>();

            Operand srcA = GetSrcReg(context, op.SrcA);
            Operand srcB = GetSrcReg(context, op.SrcB);
            Operand srcC = GetSrcReg(context, op.SrcC);

            EmitFcmp(context, op.FComp, srcA, srcB, srcC, op.Dest);
        }

        public static void FcmpI(EmitterContext context)
        {
            InstFcmpI op = context.GetOp<InstFcmpI>();

            Operand srcA = GetSrcReg(context, op.SrcA);
            Operand srcB = GetSrcImm(context, Imm20ToFloat(op.Imm20));
            Operand srcC = GetSrcReg(context, op.SrcC);

            EmitFcmp(context, op.FComp, srcA, srcB, srcC, op.Dest);
        }

        public static void FcmpC(EmitterContext context)
        {
            InstFcmpC op = context.GetOp<InstFcmpC>();

            Operand srcA = GetSrcReg(context, op.SrcA);
            Operand srcB = GetSrcCbuf(context, op.CbufSlot, op.CbufOffset);
            Operand srcC = GetSrcReg(context, op.SrcC);

            EmitFcmp(context, op.FComp, srcA, srcB, srcC, op.Dest);
        }

        public static void FcmpRc(EmitterContext context)
        {
            InstFcmpRc op = context.GetOp<InstFcmpRc>();

            Operand srcA = GetSrcReg(context, op.SrcA);
            Operand srcB = GetSrcReg(context, op.SrcC);
            Operand srcC = GetSrcCbuf(context, op.CbufSlot, op.CbufOffset);

            EmitFcmp(context, op.FComp, srcA, srcB, srcC, op.Dest);
        }

        public static void FsetR(EmitterContext context)
        {
            InstFsetR op = context.GetOp<InstFsetR>();

            Operand srcA = GetSrcReg(context, op.SrcA);
            Operand srcB = GetSrcReg(context, op.SrcB);

            EmitFset(context, op.FComp, op.Bop, srcA, srcB, op.SrcPred, op.SrcPredInv, op.Dest, op.AbsA, op.AbsB, op.NegA, op.NegB, op.BVal, op.WriteCC);
        }

        public static void FsetC(EmitterContext context)
        {
            InstFsetC op = context.GetOp<InstFsetC>();

            Operand srcA = GetSrcReg(context, op.SrcA);
            Operand srcB = GetSrcCbuf(context, op.CbufSlot, op.CbufOffset);

            EmitFset(context, op.FComp, op.Bop, srcA, srcB, op.SrcPred, op.SrcPredInv, op.Dest, op.AbsA, op.AbsB, op.NegA, op.NegB, op.BVal, op.WriteCC);
        }

        public static void FsetI(EmitterContext context)
        {
            InstFsetI op = context.GetOp<InstFsetI>();

            Operand srcA = GetSrcReg(context, op.SrcA);
            Operand srcB = GetSrcImm(context, Imm20ToFloat(op.Imm20));

            EmitFset(context, op.FComp, op.Bop, srcA, srcB, op.SrcPred, op.SrcPredInv, op.Dest, op.AbsA, op.AbsB, op.NegA, op.NegB, op.BVal, op.WriteCC);
        }

        public static void FsetpR(EmitterContext context)
        {
            InstFsetpR op = context.GetOp<InstFsetpR>();

            Operand srcA = GetSrcReg(context, op.SrcA);
            Operand srcB = GetSrcReg(context, op.SrcB);

            EmitFsetp(
                context,
                op.FComp,
                op.Bop,
                srcA,
                srcB,
                op.SrcPred,
                op.SrcPredInv,
                op.DestPred,
                op.DestPredInv,
                op.AbsA,
                op.AbsB,
                op.NegA,
                op.NegB,
                op.WriteCC);
        }

        public static void FsetpI(EmitterContext context)
        {
            InstFsetpI op = context.GetOp<InstFsetpI>();

            Operand srcA = GetSrcReg(context, op.SrcA);
            Operand srcB = GetSrcImm(context, Imm20ToFloat(op.Imm20));

            EmitFsetp(
                context,
                op.FComp,
                op.Bop,
                srcA,
                srcB,
                op.SrcPred,
                op.SrcPredInv,
                op.DestPred,
                op.DestPredInv,
                op.AbsA,
                op.AbsB,
                op.NegA,
                op.NegB,
                op.WriteCC);
        }

        public static void FsetpC(EmitterContext context)
        {
            InstFsetpC op = context.GetOp<InstFsetpC>();

            Operand srcA = GetSrcReg(context, op.SrcA);
            Operand srcB = GetSrcCbuf(context, op.CbufSlot, op.CbufOffset);

            EmitFsetp(
                context,
                op.FComp,
                op.Bop,
                srcA,
                srcB,
                op.SrcPred,
                op.SrcPredInv,
                op.DestPred,
                op.DestPredInv,
                op.AbsA,
                op.AbsB,
                op.NegA,
                op.NegB,
                op.WriteCC);
        }

        public static void Hset2R(EmitterContext context)
        {
            InstHset2R op = context.GetOp<InstHset2R>();

            Operand[] srcA = GetHalfSrc(context, op.ASwizzle, op.SrcA, op.NegA, op.AbsA);
            Operand[] srcB = GetHalfSrc(context, op.BSwizzle, op.SrcB, op.NegB, op.AbsB);

            EmitHset2(context, op.Cmp, op.Bop, srcA, srcB, op.SrcPred, op.SrcPredInv, op.Dest, op.Bval);
        }

        public static void Hset2I(EmitterContext context)
        {
            InstHset2I op = context.GetOp<InstHset2I>();

            Operand[] srcA = GetHalfSrc(context, op.ASwizzle, op.SrcA, op.NegA, op.AbsA);
            Operand[] srcB = GetHalfSrc(context, op.BimmH0, op.BimmH1);

            EmitHset2(context, op.Cmp, op.Bop, srcA, srcB, op.SrcPred, op.SrcPredInv, op.Dest, op.Bval);
        }

        public static void Hset2C(EmitterContext context)
        {
            InstHset2C op = context.GetOp<InstHset2C>();

            Operand[] srcA = GetHalfSrc(context, op.ASwizzle, op.SrcA, op.NegA, op.AbsA);
            Operand[] srcB = GetHalfSrc(context, HalfSwizzle.F32, op.CbufSlot, op.CbufOffset, op.NegB, false);

            EmitHset2(context, op.Cmp, op.Bop, srcA, srcB, op.SrcPred, op.SrcPredInv, op.Dest, op.Bval);
        }

        public static void Hsetp2R(EmitterContext context)
        {
            InstHsetp2R op = context.GetOp<InstHsetp2R>();

            Operand[] srcA = GetHalfSrc(context, op.ASwizzle, op.SrcA, op.NegA, op.AbsA);
            Operand[] srcB = GetHalfSrc(context, op.BSwizzle, op.SrcB, op.NegB, op.AbsB);

            EmitHsetp2(context, op.FComp2, op.Bop, srcA, srcB, op.SrcPred, op.SrcPredInv, op.DestPred, op.DestPredInv, op.HAnd);
        }

        public static void Hsetp2I(EmitterContext context)
        {
            InstHsetp2I op = context.GetOp<InstHsetp2I>();

            Operand[] srcA = GetHalfSrc(context, op.ASwizzle, op.SrcA, op.NegA, op.AbsA);
            Operand[] srcB = GetHalfSrc(context, op.BimmH0, op.BimmH1);

            EmitHsetp2(context, op.FComp, op.Bop, srcA, srcB, op.SrcPred, op.SrcPredInv, op.DestPred, op.DestPredInv, op.HAnd);
        }

        public static void Hsetp2C(EmitterContext context)
        {
            InstHsetp2C op = context.GetOp<InstHsetp2C>();

            Operand[] srcA = GetHalfSrc(context, op.ASwizzle, op.SrcA, op.NegA, op.AbsA);
            Operand[] srcB = GetHalfSrc(context, HalfSwizzle.F32, op.CbufSlot, op.CbufOffset, op.NegB, op.AbsB);

            EmitHsetp2(context, op.FComp, op.Bop, srcA, srcB, op.SrcPred, op.SrcPredInv, op.DestPred, op.DestPredInv, op.HAnd);
        }

        private static void EmitFcmp(EmitterContext context, FComp cmpOp, Operand srcA, Operand srcB, Operand srcC, int rd)
        {
            Operand cmpRes = GetFPComparison(context, cmpOp, srcC, ConstF(0));

            Operand res = context.ConditionalSelect(cmpRes, srcA, srcB);

            context.Copy(GetDest(rd), res);
        }

        private static void EmitFset(
            EmitterContext context,
            FComp cmpOp,
            BoolOp logicOp,
            Operand srcA,
            Operand srcB,
            int srcPred,
            bool srcPredInv,
            int rd,
            bool absoluteA,
            bool absoluteB,
            bool negateA,
            bool negateB,
            bool boolFloat,
            bool writeCC,
            bool isFP64 = false)
        {
            Instruction fpType = isFP64 ? Instruction.FP64 : Instruction.FP32;

            srcA = context.FPAbsNeg(srcA, absoluteA, negateA, fpType);
            srcB = context.FPAbsNeg(srcB, absoluteB, negateB, fpType);

            Operand res = GetFPComparison(context, cmpOp, srcA, srcB, fpType);
            Operand pred = GetPredicate(context, srcPred, srcPredInv);

            res = GetPredLogicalOp(context, logicOp, res, pred);

            Operand dest = GetDest(rd);

            if (boolFloat)
            {
                res = context.ConditionalSelect(res, ConstF(1), Const(0));

                context.Copy(dest, res);

                SetFPZnFlags(context, res, writeCC);
            }
            else
            {
                context.Copy(dest, res);

                SetZnFlags(context, res, writeCC, extended: false);
            }
        }

        private static void EmitFsetp(
            EmitterContext context,
            FComp cmpOp,
            BoolOp logicOp,
            Operand srcA,
            Operand srcB,
            int srcPred,
            bool srcPredInv,
            int destPred,
            int destPredInv,
            bool absoluteA,
            bool absoluteB,
            bool negateA,
            bool negateB,
            bool writeCC,
            bool isFP64 = false)
        {
            Instruction fpType = isFP64 ? Instruction.FP64 : Instruction.FP32;

            srcA = context.FPAbsNeg(srcA, absoluteA, negateA, fpType);
            srcB = context.FPAbsNeg(srcB, absoluteB, negateB, fpType);

            Operand p0Res = GetFPComparison(context, cmpOp, srcA, srcB, fpType);
            Operand p1Res = context.BitwiseNot(p0Res);
            Operand pred = GetPredicate(context, srcPred, srcPredInv);

            p0Res = GetPredLogicalOp(context, logicOp, p0Res, pred);
            p1Res = GetPredLogicalOp(context, logicOp, p1Res, pred);

            context.Copy(Register(destPred, RegisterType.Predicate), p0Res);
            context.Copy(Register(destPredInv, RegisterType.Predicate), p1Res);
        }

        private static void EmitHset2(
            EmitterContext context,
            FComp cmpOp,
            BoolOp logicOp,
            Operand[] srcA,
            Operand[] srcB,
            int srcPred,
            bool srcPredInv,
            int rd,
            bool boolFloat)
        {
            Operand[] res = new Operand[2];

            res[0] = GetFPComparison(context, cmpOp, srcA[0], srcB[0]);
            res[1] = GetFPComparison(context, cmpOp, srcA[1], srcB[1]);

            Operand pred = GetPredicate(context, srcPred, srcPredInv);

            res[0] = GetPredLogicalOp(context, logicOp, res[0], pred);
            res[1] = GetPredLogicalOp(context, logicOp, res[1], pred);

            if (boolFloat)
            {
                res[0] = context.ConditionalSelect(res[0], ConstF(1), Const(0));
                res[1] = context.ConditionalSelect(res[1], ConstF(1), Const(0));

                context.Copy(GetDest(rd), context.PackHalf2x16(res[0], res[1]));
            }
            else
            {
                Operand low = context.BitwiseAnd(res[0], Const(0xffff));
                Operand high = context.ShiftLeft(res[1], Const(16));

                Operand packed = context.BitwiseOr(low, high);

                context.Copy(GetDest(rd), packed);
            }
        }

        private static void EmitHsetp2(
            EmitterContext context,
            FComp cmpOp,
            BoolOp logicOp,
            Operand[] srcA,
            Operand[] srcB,
            int srcPred,
            bool srcPredInv,
            int destPred,
            int destPredInv,
            bool hAnd)
        {
            Operand p0Res = GetFPComparison(context, cmpOp, srcA[0], srcB[0]);
            Operand p1Res = GetFPComparison(context, cmpOp, srcA[1], srcB[1]);

            if (hAnd)
            {
                p0Res = context.BitwiseAnd(p0Res, p1Res);
                p1Res = context.BitwiseNot(p0Res);
            }

            Operand pred = GetPredicate(context, srcPred, srcPredInv);

            p0Res = GetPredLogicalOp(context, logicOp, p0Res, pred);
            p1Res = GetPredLogicalOp(context, logicOp, p1Res, pred);

            context.Copy(Register(destPred, RegisterType.Predicate), p0Res);
            context.Copy(Register(destPredInv, RegisterType.Predicate), p1Res);
        }

        private static Operand GetFPComparison(EmitterContext context, FComp cond, Operand srcA, Operand srcB, Instruction fpType = Instruction.FP32)
        {
            Operand res;

            if (cond == FComp.T)
            {
                res = Const(IrConsts.True);
            }
            else if (cond == FComp.F)
            {
                res = Const(IrConsts.False);
            }
            else if (cond == FComp.Nan || cond == FComp.Num)
            {
                res = context.BitwiseOr(context.IsNan(srcA, fpType), context.IsNan(srcB, fpType));

                if (cond == FComp.Num)
                {
                    res = context.BitwiseNot(res);
                }
            }
            else
            {
                Instruction inst = (cond & ~FComp.Nan) switch
                {
                    FComp.Lt => Instruction.CompareLess,
                    FComp.Eq => Instruction.CompareEqual,
                    FComp.Le => Instruction.CompareLessOrEqual,
                    FComp.Gt => Instruction.CompareGreater,
                    FComp.Ne => Instruction.CompareNotEqual,
                    FComp.Ge => Instruction.CompareGreaterOrEqual,
                    _ => throw new ArgumentException($"Unexpected condition \"{cond}\"."),
                };
                res = context.Add(inst | fpType, Local(), srcA, srcB);

                if ((cond & FComp.Nan) != 0)
                {
                    res = context.BitwiseOr(res, context.IsNan(srcA, fpType));
                    res = context.BitwiseOr(res, context.IsNan(srcB, fpType));
                }
            }

            return res;
        }
    }
}
