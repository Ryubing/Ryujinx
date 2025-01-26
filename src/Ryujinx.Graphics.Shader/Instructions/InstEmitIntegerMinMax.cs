using Ryujinx.Graphics.Shader.Decoders;
using Ryujinx.Graphics.Shader.IntermediateRepresentation;
using Ryujinx.Graphics.Shader.Translation;

using static Ryujinx.Graphics.Shader.Instructions.InstEmitAluHelper;
using static Ryujinx.Graphics.Shader.Instructions.InstEmitHelper;

namespace Ryujinx.Graphics.Shader.Instructions
{
    static partial class InstEmit
    {
        public static void ImnmxR(EmitterContext context)
        {
            InstImnmxR op = context.GetOp<InstImnmxR>();

            Operand srcA = GetSrcReg(context, op.SrcA);
            Operand srcB = GetSrcReg(context, op.SrcB);
            Operand srcPred = GetPredicate(context, op.SrcPred, op.SrcPredInv);

            EmitImnmx(context, srcA, srcB, srcPred, op.Dest, op.Signed, op.WriteCC);
        }

        public static void ImnmxI(EmitterContext context)
        {
            InstImnmxI op = context.GetOp<InstImnmxI>();

            Operand srcA = GetSrcReg(context, op.SrcA);
            Operand srcB = GetSrcImm(context, Imm20ToSInt(op.Imm20));
            Operand srcPred = GetPredicate(context, op.SrcPred, op.SrcPredInv);

            EmitImnmx(context, srcA, srcB, srcPred, op.Dest, op.Signed, op.WriteCC);
        }

        public static void ImnmxC(EmitterContext context)
        {
            InstImnmxC op = context.GetOp<InstImnmxC>();

            Operand srcA = GetSrcReg(context, op.SrcA);
            Operand srcB = GetSrcCbuf(context, op.CbufSlot, op.CbufOffset);
            Operand srcPred = GetPredicate(context, op.SrcPred, op.SrcPredInv);

            EmitImnmx(context, srcA, srcB, srcPred, op.Dest, op.Signed, op.WriteCC);
        }

        private static void EmitImnmx(
            EmitterContext context,
            Operand srcA,
            Operand srcB,
            Operand srcPred,
            int rd,
            bool isSignedInt,
            bool writeCC)
        {
            Operand resMin = isSignedInt
                ? context.IMinimumS32(srcA, srcB)
                : context.IMinimumU32(srcA, srcB);

            Operand resMax = isSignedInt
                ? context.IMaximumS32(srcA, srcB)
                : context.IMaximumU32(srcA, srcB);

            Operand res = context.ConditionalSelect(srcPred, resMin, resMax);

            context.Copy(GetDest(rd), res);

            SetZnFlags(context, res, writeCC);

            // TODO: X flags.
        }
    }
}
