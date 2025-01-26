using Ryujinx.Graphics.Shader.Decoders;
using Ryujinx.Graphics.Shader.IntermediateRepresentation;
using Ryujinx.Graphics.Shader.Translation;

using static Ryujinx.Graphics.Shader.Instructions.InstEmitHelper;
using static Ryujinx.Graphics.Shader.IntermediateRepresentation.OperandHelper;

namespace Ryujinx.Graphics.Shader.Instructions
{
    static partial class InstEmit
    {
        public static void BfeR(EmitterContext context)
        {
            InstBfeR op = context.GetOp<InstBfeR>();

            Operand srcA = GetSrcReg(context, op.SrcA);
            Operand srcB = GetSrcReg(context, op.SrcB);

            EmitBfe(context, srcA, srcB, op.Dest, op.Brev, op.Signed);
        }

        public static void BfeI(EmitterContext context)
        {
            InstBfeI op = context.GetOp<InstBfeI>();

            Operand srcA = GetSrcReg(context, op.SrcA);
            Operand srcB = GetSrcImm(context, Imm20ToSInt(op.Imm20));

            EmitBfe(context, srcA, srcB, op.Dest, op.Brev, op.Signed);
        }

        public static void BfeC(EmitterContext context)
        {
            InstBfeC op = context.GetOp<InstBfeC>();

            Operand srcA = GetSrcReg(context, op.SrcA);
            Operand srcB = GetSrcCbuf(context, op.CbufSlot, op.CbufOffset);

            EmitBfe(context, srcA, srcB, op.Dest, op.Brev, op.Signed);
        }

        public static void BfiR(EmitterContext context)
        {
            InstBfiR op = context.GetOp<InstBfiR>();

            Operand srcA = GetSrcReg(context, op.SrcA);
            Operand srcB = GetSrcReg(context, op.SrcB);
            Operand srcC = GetSrcReg(context, op.SrcC);

            EmitBfi(context, srcA, srcB, srcC, op.Dest);
        }

        public static void BfiI(EmitterContext context)
        {
            InstBfiI op = context.GetOp<InstBfiI>();

            Operand srcA = GetSrcReg(context, op.SrcA);
            Operand srcB = GetSrcImm(context, Imm20ToSInt(op.Imm20));
            Operand srcC = GetSrcReg(context, op.SrcC);

            EmitBfi(context, srcA, srcB, srcC, op.Dest);
        }

        public static void BfiC(EmitterContext context)
        {
            InstBfiC op = context.GetOp<InstBfiC>();

            Operand srcA = GetSrcReg(context, op.SrcA);
            Operand srcB = GetSrcCbuf(context, op.CbufSlot, op.CbufOffset);
            Operand srcC = GetSrcReg(context, op.SrcC);

            EmitBfi(context, srcA, srcB, srcC, op.Dest);
        }

        public static void BfiRc(EmitterContext context)
        {
            InstBfiRc op = context.GetOp<InstBfiRc>();

            Operand srcA = GetSrcReg(context, op.SrcA);
            Operand srcB = GetSrcReg(context, op.SrcC);
            Operand srcC = GetSrcCbuf(context, op.CbufSlot, op.CbufOffset);

            EmitBfi(context, srcA, srcB, srcC, op.Dest);
        }

        public static void FloR(EmitterContext context)
        {
            InstFloR op = context.GetOp<InstFloR>();

            EmitFlo(context, GetSrcReg(context, op.SrcB), op.Dest, op.NegB, op.Sh, op.Signed);
        }

        public static void FloI(EmitterContext context)
        {
            InstFloI op = context.GetOp<InstFloI>();

            EmitFlo(context, GetSrcImm(context, Imm20ToSInt(op.Imm20)), op.Dest, op.NegB, op.Sh, op.Signed);
        }

        public static void FloC(EmitterContext context)
        {
            InstFloC op = context.GetOp<InstFloC>();

            EmitFlo(context, GetSrcCbuf(context, op.CbufSlot, op.CbufOffset), op.Dest, op.NegB, op.Sh, op.Signed);
        }

        public static void PopcR(EmitterContext context)
        {
            InstPopcR op = context.GetOp<InstPopcR>();

            EmitPopc(context, GetSrcReg(context, op.SrcB), op.Dest, op.NegB);
        }

        public static void PopcI(EmitterContext context)
        {
            InstPopcI op = context.GetOp<InstPopcI>();

            EmitPopc(context, GetSrcImm(context, Imm20ToSInt(op.Imm20)), op.Dest, op.NegB);
        }

        public static void PopcC(EmitterContext context)
        {
            InstPopcC op = context.GetOp<InstPopcC>();

            EmitPopc(context, GetSrcCbuf(context, op.CbufSlot, op.CbufOffset), op.Dest, op.NegB);
        }

        private static void EmitBfe(
            EmitterContext context,
            Operand srcA,
            Operand srcB,
            int rd,
            bool bitReverse,
            bool isSigned)
        {
            if (bitReverse)
            {
                srcA = context.BitfieldReverse(srcA);
            }

            Operand position = context.BitwiseAnd(srcB, Const(0xff));

            Operand size = context.BitfieldExtractU32(srcB, Const(8), Const(8));

            Operand res = isSigned
                ? context.BitfieldExtractS32(srcA, position, size)
                : context.BitfieldExtractU32(srcA, position, size);

            context.Copy(GetDest(rd), res);

            // TODO: CC, X, corner cases.
        }

        private static void EmitBfi(EmitterContext context, Operand srcA, Operand srcB, Operand srcC, int rd)
        {
            Operand position = context.BitwiseAnd(srcB, Const(0xff));

            Operand size = context.BitfieldExtractU32(srcB, Const(8), Const(8));

            Operand res = context.BitfieldInsert(srcC, srcA, position, size);

            context.Copy(GetDest(rd), res);
        }

        private static void EmitFlo(EmitterContext context, Operand src, int rd, bool invert, bool sh, bool isSigned)
        {
            Operand srcB = context.BitwiseNot(src, invert);

            Operand res;

            if (sh)
            {
                res = context.FindLSB(context.BitfieldReverse(srcB));
            }
            else
            {
                res = isSigned
                    ? context.FindMSBS32(srcB)
                    : context.FindMSBU32(srcB);
            }

            context.Copy(GetDest(rd), res);
        }

        private static void EmitPopc(EmitterContext context, Operand src, int rd, bool invert)
        {
            Operand srcB = context.BitwiseNot(src, invert);

            Operand res = context.BitCount(srcB);

            context.Copy(GetDest(rd), res);
        }
    }
}
