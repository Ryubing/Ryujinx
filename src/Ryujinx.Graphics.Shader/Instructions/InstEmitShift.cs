using Ryujinx.Graphics.Shader.Decoders;
using Ryujinx.Graphics.Shader.IntermediateRepresentation;
using Ryujinx.Graphics.Shader.Translation;

using static Ryujinx.Graphics.Shader.Instructions.InstEmitHelper;
using static Ryujinx.Graphics.Shader.IntermediateRepresentation.OperandHelper;

namespace Ryujinx.Graphics.Shader.Instructions
{
    static partial class InstEmit
    {
        public static void ShfLR(EmitterContext context)
        {
            InstShfLR op = context.GetOp<InstShfLR>();

            Operand srcA = GetSrcReg(context, op.SrcA);
            Operand srcB = GetSrcReg(context, op.SrcB);
            Operand srcC = GetSrcReg(context, op.SrcC);

            EmitShf(context, op.MaxShift, srcA, srcB, srcC, op.Dest, op.M, left: true, op.WriteCC);
        }

        public static void ShfRR(EmitterContext context)
        {
            InstShfRR op = context.GetOp<InstShfRR>();

            Operand srcA = GetSrcReg(context, op.SrcA);
            Operand srcB = GetSrcReg(context, op.SrcB);
            Operand srcC = GetSrcReg(context, op.SrcC);

            EmitShf(context, op.MaxShift, srcA, srcB, srcC, op.Dest, op.M, left: false, op.WriteCC);
        }

        public static void ShfLI(EmitterContext context)
        {
            InstShfLI op = context.GetOp<InstShfLI>();

            Operand srcA = GetSrcReg(context, op.SrcA);
            Operand srcB = Const(op.Imm6);
            Operand srcC = GetSrcReg(context, op.SrcC);

            EmitShf(context, op.MaxShift, srcA, srcB, srcC, op.Dest, op.M, left: true, op.WriteCC);
        }

        public static void ShfRI(EmitterContext context)
        {
            InstShfRI op = context.GetOp<InstShfRI>();

            Operand srcA = GetSrcReg(context, op.SrcA);
            Operand srcB = Const(op.Imm6);
            Operand srcC = GetSrcReg(context, op.SrcC);

            EmitShf(context, op.MaxShift, srcA, srcB, srcC, op.Dest, op.M, left: false, op.WriteCC);
        }

        public static void ShlR(EmitterContext context)
        {
            InstShlR op = context.GetOp<InstShlR>();

            EmitShl(context, GetSrcReg(context, op.SrcA), GetSrcReg(context, op.SrcB), op.Dest, op.M);
        }

        public static void ShlI(EmitterContext context)
        {
            InstShlI op = context.GetOp<InstShlI>();

            EmitShl(context, GetSrcReg(context, op.SrcA), GetSrcImm(context, Imm20ToSInt(op.Imm20)), op.Dest, op.M);
        }

        public static void ShlC(EmitterContext context)
        {
            InstShlC op = context.GetOp<InstShlC>();

            EmitShl(context, GetSrcReg(context, op.SrcA), GetSrcCbuf(context, op.CbufSlot, op.CbufOffset), op.Dest, op.M);
        }

        public static void ShrR(EmitterContext context)
        {
            InstShrR op = context.GetOp<InstShrR>();

            Operand srcA = GetSrcReg(context, op.SrcA);
            Operand srcB = GetSrcReg(context, op.SrcB);

            EmitShr(context, srcA, srcB, op.Dest, op.M, op.Brev, op.Signed);
        }

        public static void ShrI(EmitterContext context)
        {
            InstShrI op = context.GetOp<InstShrI>();

            Operand srcA = GetSrcReg(context, op.SrcA);
            Operand srcB = GetSrcImm(context, Imm20ToSInt(op.Imm20));

            EmitShr(context, srcA, srcB, op.Dest, op.M, op.Brev, op.Signed);
        }

        public static void ShrC(EmitterContext context)
        {
            InstShrC op = context.GetOp<InstShrC>();

            Operand srcA = GetSrcReg(context, op.SrcA);
            Operand srcB = GetSrcCbuf(context, op.CbufSlot, op.CbufOffset);

            EmitShr(context, srcA, srcB, op.Dest, op.M, op.Brev, op.Signed);
        }

        private static void EmitShf(
            EmitterContext context,
            MaxShift maxShift,
            Operand srcA,
            Operand srcB,
            Operand srcC,
            int rd,
            bool mask,
            bool left,
            bool writeCC)
        {
            bool isLongShift = maxShift == MaxShift.U64 || maxShift == MaxShift.S64;
            bool signedShift = maxShift == MaxShift.S64;
            int maxShiftConst = isLongShift ? 64 : 32;

            if (mask)
            {
                srcB = context.BitwiseAnd(srcB, Const(maxShiftConst - 1));
            }

            Operand res;

            if (left)
            {
                // res = (C << B) | (A >> (32 - B))
                res = context.ShiftLeft(srcC, srcB);
                res = context.BitwiseOr(res, context.ShiftRightU32(srcA, context.ISubtract(Const(32), srcB)));

                if (isLongShift)
                {
                    // res = B >= 32 ? A << (B - 32) : res
                    Operand lowerShift = context.ShiftLeft(srcA, context.ISubtract(srcB, Const(32)));

                    Operand shiftGreaterThan31 = context.ICompareGreaterOrEqualUnsigned(srcB, Const(32));
                    res = context.ConditionalSelect(shiftGreaterThan31, lowerShift, res);
                }
            }
            else
            {
                // res = (A >> B) | (C << (32 - B))
                res = context.ShiftRightU32(srcA, srcB);
                res = context.BitwiseOr(res, context.ShiftLeft(srcC, context.ISubtract(Const(32), srcB)));

                if (isLongShift)
                {
                    // res = B >= 32 ? C >> (B - 32) : res
                    Operand upperShift = signedShift
                        ? context.ShiftRightS32(srcC, context.ISubtract(srcB, Const(32)))
                        : context.ShiftRightU32(srcC, context.ISubtract(srcB, Const(32)));

                    Operand shiftGreaterThan31 = context.ICompareGreaterOrEqualUnsigned(srcB, Const(32));
                    res = context.ConditionalSelect(shiftGreaterThan31, upperShift, res);
                }
            }

            if (!mask)
            {
                // Clamped shift value.
                Operand isLessThanMax = context.ICompareLessUnsigned(srcB, Const(maxShiftConst));

                res = context.ConditionalSelect(isLessThanMax, res, Const(0));
            }

            context.Copy(GetDest(rd), res);

            if (writeCC)
            {
                InstEmitAluHelper.SetZnFlags(context, res, writeCC);
            }

            // TODO: X.
        }

        private static void EmitShl(EmitterContext context, Operand srcA, Operand srcB, int rd, bool mask)
        {
            if (mask)
            {
                srcB = context.BitwiseAnd(srcB, Const(0x1f));
            }

            Operand res = context.ShiftLeft(srcA, srcB);

            if (!mask)
            {
                // Clamped shift value.
                Operand isLessThan32 = context.ICompareLessUnsigned(srcB, Const(32));

                res = context.ConditionalSelect(isLessThan32, res, Const(0));
            }

            // TODO: X, CC.

            context.Copy(GetDest(rd), res);
        }

        private static void EmitShr(
            EmitterContext context,
            Operand srcA,
            Operand srcB,
            int rd,
            bool mask,
            bool bitReverse,
            bool isSigned)
        {
            if (bitReverse)
            {
                srcA = context.BitfieldReverse(srcA);
            }

            if (mask)
            {
                srcB = context.BitwiseAnd(srcB, Const(0x1f));
            }

            Operand res = isSigned
                ? context.ShiftRightS32(srcA, srcB)
                : context.ShiftRightU32(srcA, srcB);

            if (!mask)
            {
                // Clamped shift value.
                Operand resShiftBy32;

                if (isSigned)
                {
                    resShiftBy32 = context.ShiftRightS32(srcA, Const(31));
                }
                else
                {
                    resShiftBy32 = Const(0);
                }

                Operand isLessThan32 = context.ICompareLessUnsigned(srcB, Const(32));

                res = context.ConditionalSelect(isLessThan32, res, resShiftBy32);
            }

            // TODO: X, CC.

            context.Copy(GetDest(rd), res);
        }
    }
}
