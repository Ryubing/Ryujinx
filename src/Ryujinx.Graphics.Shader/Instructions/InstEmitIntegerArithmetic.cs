using Ryujinx.Graphics.Shader.Decoders;
using Ryujinx.Graphics.Shader.IntermediateRepresentation;
using Ryujinx.Graphics.Shader.Translation;

using static Ryujinx.Graphics.Shader.Instructions.InstEmitAluHelper;
using static Ryujinx.Graphics.Shader.Instructions.InstEmitHelper;
using static Ryujinx.Graphics.Shader.IntermediateRepresentation.OperandHelper;

namespace Ryujinx.Graphics.Shader.Instructions
{
    static partial class InstEmit
    {
        public static void IaddR(EmitterContext context)
        {
            InstIaddR op = context.GetOp<InstIaddR>();

            Operand srcA = GetSrcReg(context, op.SrcA);
            Operand srcB = GetSrcReg(context, op.SrcB);

            EmitIadd(context, srcA, srcB, op.Dest, op.AvgMode, op.X, op.WriteCC);
        }

        public static void IaddI(EmitterContext context)
        {
            InstIaddI op = context.GetOp<InstIaddI>();

            Operand srcA = GetSrcReg(context, op.SrcA);
            Operand srcB = GetSrcImm(context, Imm20ToSInt(op.Imm20));

            EmitIadd(context, srcA, srcB, op.Dest, op.AvgMode, op.X, op.WriteCC);
        }

        public static void IaddC(EmitterContext context)
        {
            InstIaddC op = context.GetOp<InstIaddC>();

            Operand srcA = GetSrcReg(context, op.SrcA);
            Operand srcB = GetSrcCbuf(context, op.CbufSlot, op.CbufOffset);

            EmitIadd(context, srcA, srcB, op.Dest, op.AvgMode, op.X, op.WriteCC);
        }

        public static void Iadd32i(EmitterContext context)
        {
            InstIadd32i op = context.GetOp<InstIadd32i>();

            Operand srcA = GetSrcReg(context, op.SrcA);
            Operand srcB = GetSrcImm(context, op.Imm32);

            EmitIadd(context, srcA, srcB, op.Dest, op.AvgMode, op.X, op.WriteCC);
        }

        public static void Iadd3R(EmitterContext context)
        {
            InstIadd3R op = context.GetOp<InstIadd3R>();

            Operand srcA = GetSrcReg(context, op.SrcA);
            Operand srcB = GetSrcReg(context, op.SrcB);
            Operand srcC = GetSrcReg(context, op.SrcC);

            EmitIadd3(context, op.Lrs, srcA, srcB, srcC, op.Apart, op.Bpart, op.Cpart, op.Dest, op.NegA, op.NegB, op.NegC);
        }

        public static void Iadd3I(EmitterContext context)
        {
            InstIadd3I op = context.GetOp<InstIadd3I>();

            Operand srcA = GetSrcReg(context, op.SrcA);
            Operand srcB = GetSrcImm(context, Imm20ToSInt(op.Imm20));
            Operand srcC = GetSrcReg(context, op.SrcC);

            EmitIadd3(context, Lrs.None, srcA, srcB, srcC, HalfSelect.B32, HalfSelect.B32, HalfSelect.B32, op.Dest, op.NegA, op.NegB, op.NegC);
        }

        public static void Iadd3C(EmitterContext context)
        {
            InstIadd3C op = context.GetOp<InstIadd3C>();

            Operand srcA = GetSrcReg(context, op.SrcA);
            Operand srcB = GetSrcCbuf(context, op.CbufSlot, op.CbufOffset);
            Operand srcC = GetSrcReg(context, op.SrcC);

            EmitIadd3(context, Lrs.None, srcA, srcB, srcC, HalfSelect.B32, HalfSelect.B32, HalfSelect.B32, op.Dest, op.NegA, op.NegB, op.NegC);
        }

        public static void ImadR(EmitterContext context)
        {
            InstImadR op = context.GetOp<InstImadR>();

            Operand srcA = GetSrcReg(context, op.SrcA);
            Operand srcB = GetSrcReg(context, op.SrcB);
            Operand srcC = GetSrcReg(context, op.SrcC);

            EmitImad(context, srcA, srcB, srcC, op.Dest, op.AvgMode, op.ASigned, op.BSigned, op.Hilo);
        }

        public static void ImadI(EmitterContext context)
        {
            InstImadI op = context.GetOp<InstImadI>();

            Operand srcA = GetSrcReg(context, op.SrcA);
            Operand srcB = GetSrcImm(context, Imm20ToSInt(op.Imm20));
            Operand srcC = GetSrcReg(context, op.SrcC);

            EmitImad(context, srcA, srcB, srcC, op.Dest, op.AvgMode, op.ASigned, op.BSigned, op.Hilo);
        }

        public static void ImadC(EmitterContext context)
        {
            InstImadC op = context.GetOp<InstImadC>();

            Operand srcA = GetSrcReg(context, op.SrcA);
            Operand srcB = GetSrcCbuf(context, op.CbufSlot, op.CbufOffset);
            Operand srcC = GetSrcReg(context, op.SrcC);

            EmitImad(context, srcA, srcB, srcC, op.Dest, op.AvgMode, op.ASigned, op.BSigned, op.Hilo);
        }

        public static void ImadRc(EmitterContext context)
        {
            InstImadRc op = context.GetOp<InstImadRc>();

            Operand srcA = GetSrcReg(context, op.SrcA);
            Operand srcB = GetSrcReg(context, op.SrcC);
            Operand srcC = GetSrcCbuf(context, op.CbufSlot, op.CbufOffset);

            EmitImad(context, srcA, srcB, srcC, op.Dest, op.AvgMode, op.ASigned, op.BSigned, op.Hilo);
        }

        public static void Imad32i(EmitterContext context)
        {
            InstImad32i op = context.GetOp<InstImad32i>();

            Operand srcA = GetSrcReg(context, op.SrcA);
            Operand srcB = GetSrcImm(context, op.Imm32);
            Operand srcC = GetSrcReg(context, op.Dest);

            EmitImad(context, srcA, srcB, srcC, op.Dest, op.AvgMode, op.ASigned, op.BSigned, op.Hilo);
        }

        public static void ImulR(EmitterContext context)
        {
            InstImulR op = context.GetOp<InstImulR>();

            Operand srcA = GetSrcReg(context, op.SrcA);
            Operand srcB = GetSrcReg(context, op.SrcB);

            EmitImad(context, srcA, srcB, Const(0), op.Dest, AvgMode.NoNeg, op.ASigned, op.BSigned, op.Hilo);
        }

        public static void ImulI(EmitterContext context)
        {
            InstImulI op = context.GetOp<InstImulI>();

            Operand srcA = GetSrcReg(context, op.SrcA);
            Operand srcB = GetSrcImm(context, Imm20ToSInt(op.Imm20));

            EmitImad(context, srcA, srcB, Const(0), op.Dest, AvgMode.NoNeg, op.ASigned, op.BSigned, op.Hilo);
        }

        public static void ImulC(EmitterContext context)
        {
            InstImulC op = context.GetOp<InstImulC>();

            Operand srcA = GetSrcReg(context, op.SrcA);
            Operand srcB = GetSrcCbuf(context, op.CbufSlot, op.CbufOffset);

            EmitImad(context, srcA, srcB, Const(0), op.Dest, AvgMode.NoNeg, op.ASigned, op.BSigned, op.Hilo);
        }

        public static void Imul32i(EmitterContext context)
        {
            InstImul32i op = context.GetOp<InstImul32i>();

            Operand srcA = GetSrcReg(context, op.SrcA);
            Operand srcB = GetSrcImm(context, op.Imm32);

            EmitImad(context, srcA, srcB, Const(0), op.Dest, AvgMode.NoNeg, op.ASigned, op.BSigned, op.Hilo);
        }

        public static void IscaddR(EmitterContext context)
        {
            InstIscaddR op = context.GetOp<InstIscaddR>();

            Operand srcA = GetSrcReg(context, op.SrcA);
            Operand srcB = GetSrcReg(context, op.SrcB);

            EmitIscadd(context, srcA, srcB, op.Dest, op.Imm5, op.AvgMode, op.WriteCC);
        }

        public static void IscaddI(EmitterContext context)
        {
            InstIscaddI op = context.GetOp<InstIscaddI>();

            Operand srcA = GetSrcReg(context, op.SrcA);
            Operand srcB = GetSrcImm(context, Imm20ToSInt(op.Imm20));

            EmitIscadd(context, srcA, srcB, op.Dest, op.Imm5, op.AvgMode, op.WriteCC);
        }

        public static void IscaddC(EmitterContext context)
        {
            InstIscaddC op = context.GetOp<InstIscaddC>();

            Operand srcA = GetSrcReg(context, op.SrcA);
            Operand srcB = GetSrcCbuf(context, op.CbufSlot, op.CbufOffset);

            EmitIscadd(context, srcA, srcB, op.Dest, op.Imm5, op.AvgMode, op.WriteCC);
        }

        public static void Iscadd32i(EmitterContext context)
        {
            InstIscadd32i op = context.GetOp<InstIscadd32i>();

            Operand srcA = GetSrcReg(context, op.SrcA);
            Operand srcB = GetSrcImm(context, op.Imm32);

            EmitIscadd(context, srcA, srcB, op.Dest, op.Imm5, AvgMode.NoNeg, op.WriteCC);
        }

        public static void LeaR(EmitterContext context)
        {
            InstLeaR op = context.GetOp<InstLeaR>();

            Operand srcA = GetSrcReg(context, op.SrcA);
            Operand srcB = GetSrcReg(context, op.SrcB);

            EmitLea(context, srcA, srcB, op.Dest, op.NegA, op.ImmU5);
        }

        public static void LeaI(EmitterContext context)
        {
            InstLeaI op = context.GetOp<InstLeaI>();

            Operand srcA = GetSrcReg(context, op.SrcA);
            Operand srcB = GetSrcImm(context, Imm20ToSInt(op.Imm20));

            EmitLea(context, srcA, srcB, op.Dest, op.NegA, op.ImmU5);
        }

        public static void LeaC(EmitterContext context)
        {
            InstLeaC op = context.GetOp<InstLeaC>();

            Operand srcA = GetSrcReg(context, op.SrcA);
            Operand srcB = GetSrcCbuf(context, op.CbufSlot, op.CbufOffset);

            EmitLea(context, srcA, srcB, op.Dest, op.NegA, op.ImmU5);
        }

        public static void LeaHiR(EmitterContext context)
        {
            InstLeaHiR op = context.GetOp<InstLeaHiR>();

            Operand srcA = GetSrcReg(context, op.SrcA);
            Operand srcB = GetSrcReg(context, op.SrcB);
            Operand srcC = GetSrcReg(context, op.SrcC);

            EmitLeaHi(context, srcA, srcB, srcC, op.Dest, op.NegA, op.ImmU5);
        }

        public static void LeaHiC(EmitterContext context)
        {
            InstLeaHiC op = context.GetOp<InstLeaHiC>();

            Operand srcA = GetSrcReg(context, op.SrcA);
            Operand srcB = GetSrcCbuf(context, op.CbufSlot, op.CbufOffset);
            Operand srcC = GetSrcReg(context, op.SrcC);

            EmitLeaHi(context, srcA, srcB, srcC, op.Dest, op.NegA, op.ImmU5);
        }

        public static void XmadR(EmitterContext context)
        {
            InstXmadR op = context.GetOp<InstXmadR>();

            Operand srcA = GetSrcReg(context, op.SrcA);
            Operand srcB = GetSrcReg(context, op.SrcB);
            Operand srcC = GetSrcReg(context, op.SrcC);

            EmitXmad(context, op.XmadCop, srcA, srcB, srcC, op.Dest, op.ASigned, op.BSigned, op.HiloA, op.HiloB, op.Psl, op.Mrg, op.X, op.WriteCC);
        }

        public static void XmadI(EmitterContext context)
        {
            InstXmadI op = context.GetOp<InstXmadI>();

            Operand srcA = GetSrcReg(context, op.SrcA);
            Operand srcB = GetSrcImm(context, op.Imm16);
            Operand srcC = GetSrcReg(context, op.SrcC);

            EmitXmad(context, op.XmadCop, srcA, srcB, srcC, op.Dest, op.ASigned, op.BSigned, op.HiloA, false, op.Psl, op.Mrg, op.X, op.WriteCC);
        }

        public static void XmadC(EmitterContext context)
        {
            InstXmadC op = context.GetOp<InstXmadC>();

            Operand srcA = GetSrcReg(context, op.SrcA);
            Operand srcB = GetSrcCbuf(context, op.CbufSlot, op.CbufOffset);
            Operand srcC = GetSrcReg(context, op.SrcC);

            EmitXmad(context, op.XmadCop, srcA, srcB, srcC, op.Dest, op.ASigned, op.BSigned, op.HiloA, op.HiloB, op.Psl, op.Mrg, op.X, op.WriteCC);
        }

        public static void XmadRc(EmitterContext context)
        {
            InstXmadRc op = context.GetOp<InstXmadRc>();

            Operand srcA = GetSrcReg(context, op.SrcA);
            Operand srcB = GetSrcReg(context, op.SrcC);
            Operand srcC = GetSrcCbuf(context, op.CbufSlot, op.CbufOffset);

            EmitXmad(context, op.XmadCop, srcA, srcB, srcC, op.Dest, op.ASigned, op.BSigned, op.HiloA, op.HiloB, false, false, op.X, op.WriteCC);
        }

        private static void EmitIadd(
            EmitterContext context,
            Operand srcA,
            Operand srcB,
            int rd,
            AvgMode avgMode,
            bool extended,
            bool writeCC)
        {
            srcA = context.INegate(srcA, avgMode == AvgMode.NegA);
            srcB = context.INegate(srcB, avgMode == AvgMode.NegB);

            Operand res = context.IAdd(srcA, srcB);

            if (extended)
            {
                res = context.IAdd(res, context.BitwiseAnd(GetCF(), Const(1)));
            }

            SetIaddFlags(context, res, srcA, srcB, writeCC, extended);

            // TODO: SAT.

            context.Copy(GetDest(rd), res);
        }

        private static void EmitIadd3(
            EmitterContext context,
            Lrs mode,
            Operand srcA,
            Operand srcB,
            Operand srcC,
            HalfSelect partA,
            HalfSelect partB,
            HalfSelect partC,
            int rd,
            bool negateA,
            bool negateB,
            bool negateC)
        {
            Operand Extend(Operand src, HalfSelect part)
            {
                if (part == HalfSelect.B32)
                {
                    return src;
                }

                if (part == HalfSelect.H0)
                {
                    return context.BitwiseAnd(src, Const(0xffff));
                }
                else if (part == HalfSelect.H1)
                {
                    return context.ShiftRightU32(src, Const(16));
                }
                else
                {
                    context.TranslatorContext.GpuAccessor.Log($"Iadd3 has invalid component selection {part}.");
                }

                return src;
            }

            srcA = context.INegate(Extend(srcA, partA), negateA);
            srcB = context.INegate(Extend(srcB, partB), negateB);
            srcC = context.INegate(Extend(srcC, partC), negateC);

            Operand res = context.IAdd(srcA, srcB);

            if (mode != Lrs.None)
            {
                if (mode == Lrs.LeftShift)
                {
                    res = context.ShiftLeft(res, Const(16));
                }
                else if (mode == Lrs.RightShift)
                {
                    res = context.ShiftRightU32(res, Const(16));
                }
                else
                {
                    // TODO: Warning.
                }
            }

            res = context.IAdd(res, srcC);

            context.Copy(GetDest(rd), res);

            // TODO: CC, X, corner cases.
        }

        private static void EmitImad(
            EmitterContext context,
            Operand srcA,
            Operand srcB,
            Operand srcC,
            int rd,
            AvgMode avgMode,
            bool signedA,
            bool signedB,
            bool high)
        {
            srcB = context.INegate(srcB, avgMode == AvgMode.NegA);
            srcC = context.INegate(srcC, avgMode == AvgMode.NegB);

            Operand res;

            if (high)
            {
                if (signedA && signedB)
                {
                    res = context.MultiplyHighS32(srcA, srcB);
                }
                else
                {
                    res = context.MultiplyHighU32(srcA, srcB);

                    if (signedA)
                    {
                        res = context.IAdd(res, context.IMultiply(srcB, context.ShiftRightS32(srcA, Const(31))));
                    }
                    else if (signedB)
                    {
                        res = context.IAdd(res, context.IMultiply(srcA, context.ShiftRightS32(srcB, Const(31))));
                    }
                }
            }
            else
            {
                res = context.IMultiply(srcA, srcB);
            }

            if (srcC.Type != OperandType.Constant || srcC.Value != 0)
            {
                res = context.IAdd(res, srcC);
            }

            // TODO: CC, X, SAT, and more?

            context.Copy(GetDest(rd), res);
        }

        private static void EmitIscadd(
            EmitterContext context,
            Operand srcA,
            Operand srcB,
            int rd,
            int shift,
            AvgMode avgMode,
            bool writeCC)
        {
            srcA = context.ShiftLeft(srcA, Const(shift));

            srcA = context.INegate(srcA, avgMode == AvgMode.NegA);
            srcB = context.INegate(srcB, avgMode == AvgMode.NegB);

            Operand res = context.IAdd(srcA, srcB);

            SetIaddFlags(context, res, srcA, srcB, writeCC, false);

            context.Copy(GetDest(rd), res);
        }

        public static void EmitLea(EmitterContext context, Operand srcA, Operand srcB, int rd, bool negateA, int shift)
        {
            srcA = context.ShiftLeft(srcA, Const(shift));
            srcA = context.INegate(srcA, negateA);

            Operand res = context.IAdd(srcA, srcB);

            context.Copy(GetDest(rd), res);

            // TODO: CC, X.
        }

        private static void EmitLeaHi(
            EmitterContext context,
            Operand srcA,
            Operand srcB,
            Operand srcC,
            int rd,
            bool negateA,
            int shift)
        {
            Operand aLow = context.ShiftLeft(srcA, Const(shift));
            Operand aHigh = shift == 0 ? Const(0) : context.ShiftRightU32(srcA, Const(32 - shift));
            aHigh = context.BitwiseOr(aHigh, context.ShiftLeft(srcC, Const(shift)));

            if (negateA)
            {
                // Perform 64-bit negation by doing bitwise not of the value,
                // then adding 1 and carrying over from low to high.
                aLow = context.BitwiseNot(aLow);
                aHigh = context.BitwiseNot(aHigh);

#pragma warning disable IDE0059 // Remove unnecessary value assignment
                aLow = AddWithCarry(context, aLow, Const(1), out Operand aLowCOut);
#pragma warning restore IDE0059
                aHigh = context.IAdd(aHigh, aLowCOut);
            }

            Operand res = context.IAdd(aHigh, srcB);

            context.Copy(GetDest(rd), res);

            // TODO: CC, X.
        }

        public static void EmitXmad(
            EmitterContext context,
            XmadCop2 mode,
            Operand srcA,
            Operand srcB,
            Operand srcC,
            int rd,
            bool signedA,
            bool signedB,
            bool highA,
            bool highB,
            bool productShiftLeft,
            bool merge,
            bool extended,
            bool writeCC)
        {
            XmadCop modeConv;
            switch (mode)
            {
                case XmadCop2.Cfull:
                    modeConv = XmadCop.Cfull;
                    break;
                case XmadCop2.Clo:
                    modeConv = XmadCop.Clo;
                    break;
                case XmadCop2.Chi:
                    modeConv = XmadCop.Chi;
                    break;
                case XmadCop2.Csfu:
                    modeConv = XmadCop.Csfu;
                    break;
                default:
                    context.TranslatorContext.GpuAccessor.Log($"Invalid XMAD mode \"{mode}\".");
                    return;
            }

            EmitXmad(context, modeConv, srcA, srcB, srcC, rd, signedA, signedB, highA, highB, productShiftLeft, merge, extended, writeCC);
        }

        public static void EmitXmad(
            EmitterContext context,
            XmadCop mode,
            Operand srcA,
            Operand srcB,
            Operand srcC,
            int rd,
            bool signedA,
            bool signedB,
            bool highA,
            bool highB,
            bool productShiftLeft,
            bool merge,
            bool extended,
            bool writeCC)
        {
            Operand srcBUnmodified = srcB;

            Operand Extend16To32(Operand src, bool high, bool signed)
            {
                if (signed && high)
                {
                    return context.ShiftRightS32(src, Const(16));
                }
                else if (signed)
                {
                    return context.BitfieldExtractS32(src, Const(0), Const(16));
                }
                else if (high)
                {
                    return context.ShiftRightU32(src, Const(16));
                }
                else
                {
                    return context.BitwiseAnd(src, Const(0xffff));
                }
            }

            srcA = Extend16To32(srcA, highA, signedA);
            srcB = Extend16To32(srcB, highB, signedB);

            Operand res = context.IMultiply(srcA, srcB);

            if (productShiftLeft)
            {
                res = context.ShiftLeft(res, Const(16));
            }

            switch (mode)
            {
                case XmadCop.Cfull:
                    break;

                case XmadCop.Clo:
                    srcC = Extend16To32(srcC, high: false, signed: false);
                    break;
                case XmadCop.Chi:
                    srcC = Extend16To32(srcC, high: true, signed: false);
                    break;

                case XmadCop.Cbcc:
                    srcC = context.IAdd(srcC, context.ShiftLeft(srcBUnmodified, Const(16)));
                    break;

                case XmadCop.Csfu:
                    Operand signAdjustA = context.ShiftLeft(context.ShiftRightU32(srcA, Const(31)), Const(16));
                    Operand signAdjustB = context.ShiftLeft(context.ShiftRightU32(srcB, Const(31)), Const(16));

                    srcC = context.ISubtract(srcC, context.IAdd(signAdjustA, signAdjustB));
                    break;

                default:
                    context.TranslatorContext.GpuAccessor.Log($"Invalid XMAD mode \"{mode}\".");
                    return;
            }

            Operand product = res;

            if (extended)
            {
                // Add with carry.
                res = context.IAdd(res, context.BitwiseAnd(GetCF(), Const(1)));
            }
            else
            {
                // Add (no carry in).
                res = context.IAdd(res, srcC);
            }

            SetIaddFlags(context, res, product, srcC, writeCC, extended);

            if (merge)
            {
                res = context.BitwiseAnd(res, Const(0xffff));
                res = context.BitwiseOr(res, context.ShiftLeft(srcBUnmodified, Const(16)));
            }

            context.Copy(GetDest(rd), res);
        }

        private static void SetIaddFlags(EmitterContext context, Operand res, Operand srcA, Operand srcB, bool setCC, bool extended)
        {
            if (!setCC)
            {
                return;
            }

            if (extended)
            {
                // C = (d == a && CIn) || d < a
                Operand tempC0 = context.ICompareEqual(res, srcA);
                Operand tempC1 = context.ICompareLessUnsigned(res, srcA);

                tempC0 = context.BitwiseAnd(tempC0, GetCF());

                context.Copy(GetCF(), context.BitwiseOr(tempC0, tempC1));
            }
            else
            {
                // C = d < a
                context.Copy(GetCF(), context.ICompareLessUnsigned(res, srcA));
            }

            // V = (d ^ a) & ~(a ^ b) < 0
            Operand tempV0 = context.BitwiseExclusiveOr(res, srcA);
            Operand tempV1 = context.BitwiseExclusiveOr(srcA, srcB);

            tempV1 = context.BitwiseNot(tempV1);

            Operand tempV = context.BitwiseAnd(tempV0, tempV1);

            context.Copy(GetVF(), context.ICompareLess(tempV, Const(0)));

            SetZnFlags(context, res, setCC: true, extended: extended);
        }
    }
}
