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
        public static void F2fR(EmitterContext context)
        {
            InstF2fR op = context.GetOp<InstF2fR>();

            Operand src = UnpackReg(context, op.SrcFmt, op.Sh, op.SrcB);

            EmitF2F(context, op.SrcFmt, op.DstFmt, op.RoundMode, src, op.Dest, op.AbsB, op.NegB, op.Sat);
        }

        public static void F2fI(EmitterContext context)
        {
            InstF2fI op = context.GetOp<InstF2fI>();

            Operand src = UnpackImm(context, op.SrcFmt, op.Sh, Imm20ToFloat(op.Imm20));

            EmitF2F(context, op.SrcFmt, op.DstFmt, op.RoundMode, src, op.Dest, op.AbsB, op.NegB, op.Sat);
        }

        public static void F2fC(EmitterContext context)
        {
            InstF2fC op = context.GetOp<InstF2fC>();

            Operand src = UnpackCbuf(context, op.SrcFmt, op.Sh, op.CbufSlot, op.CbufOffset);

            EmitF2F(context, op.SrcFmt, op.DstFmt, op.RoundMode, src, op.Dest, op.AbsB, op.NegB, op.Sat);
        }

        public static void F2iR(EmitterContext context)
        {
            InstF2iR op = context.GetOp<InstF2iR>();

            Operand src = UnpackReg(context, op.SrcFmt, op.Sh, op.SrcB);

            EmitF2I(context, op.SrcFmt, op.IDstFmt, op.RoundMode, src, op.Dest, op.AbsB, op.NegB);
        }

        public static void F2iI(EmitterContext context)
        {
            InstF2iI op = context.GetOp<InstF2iI>();

            Operand src = UnpackImm(context, op.SrcFmt, op.Sh, Imm20ToFloat(op.Imm20));

            EmitF2I(context, op.SrcFmt, op.IDstFmt, op.RoundMode, src, op.Dest, op.AbsB, op.NegB);
        }

        public static void F2iC(EmitterContext context)
        {
            InstF2iC op = context.GetOp<InstF2iC>();

            Operand src = UnpackCbuf(context, op.SrcFmt, op.Sh, op.CbufSlot, op.CbufOffset);

            EmitF2I(context, op.SrcFmt, op.IDstFmt, op.RoundMode, src, op.Dest, op.AbsB, op.NegB);
        }

        public static void I2fR(EmitterContext context)
        {
            InstI2fR op = context.GetOp<InstI2fR>();

            Operand src = GetSrcReg(context, op.SrcB);

            EmitI2F(context, op.ISrcFmt, op.DstFmt, src, op.ByteSel, op.Dest, op.AbsB, op.NegB);
        }

        public static void I2fI(EmitterContext context)
        {
            InstI2fI op = context.GetOp<InstI2fI>();

            Operand src = GetSrcImm(context, Imm20ToSInt(op.Imm20));

            EmitI2F(context, op.ISrcFmt, op.DstFmt, src, op.ByteSel, op.Dest, op.AbsB, op.NegB);
        }

        public static void I2fC(EmitterContext context)
        {
            InstI2fC op = context.GetOp<InstI2fC>();

            Operand src = GetSrcCbuf(context, op.CbufSlot, op.CbufOffset);

            EmitI2F(context, op.ISrcFmt, op.DstFmt, src, op.ByteSel, op.Dest, op.AbsB, op.NegB);
        }

        public static void I2iR(EmitterContext context)
        {
            InstI2iR op = context.GetOp<InstI2iR>();

            Operand src = GetSrcReg(context, op.SrcB);

            EmitI2I(context, op.ISrcFmt, op.IDstFmt, src, op.ByteSel, op.Dest, op.AbsB, op.NegB, op.Sat, op.WriteCC);
        }

        public static void I2iI(EmitterContext context)
        {
            InstI2iI op = context.GetOp<InstI2iI>();

            Operand src = GetSrcImm(context, Imm20ToSInt(op.Imm20));

            EmitI2I(context, op.ISrcFmt, op.IDstFmt, src, op.ByteSel, op.Dest, op.AbsB, op.NegB, op.Sat, op.WriteCC);
        }

        public static void I2iC(EmitterContext context)
        {
            InstI2iC op = context.GetOp<InstI2iC>();

            Operand src = GetSrcCbuf(context, op.CbufSlot, op.CbufOffset);

            EmitI2I(context, op.ISrcFmt, op.IDstFmt, src, op.ByteSel, op.Dest, op.AbsB, op.NegB, op.Sat, op.WriteCC);
        }

        private static void EmitF2F(
            EmitterContext context,
            DstFmt srcType,
            DstFmt dstType,
            IntegerRound roundingMode,
            Operand src,
            int rd,
            bool absolute,
            bool negate,
            bool saturate)
        {
            Operand srcB = context.FPAbsNeg(src, absolute, negate, srcType.ToInstFPType());

            if (srcType == dstType)
            {
                srcB = roundingMode switch
                {
                    IntegerRound.Round => context.FPRound(srcB, srcType.ToInstFPType()),
                    IntegerRound.Floor => context.FPFloor(srcB, srcType.ToInstFPType()),
                    IntegerRound.Ceil => context.FPCeiling(srcB, srcType.ToInstFPType()),
                    IntegerRound.Trunc => context.FPTruncate(srcB, srcType.ToInstFPType()),
                    _ => srcB,
                };
            }

            // We don't need to handle conversions between FP16 <-> FP32
            // since we do FP16 operations as FP32 directly.
            // FP16 <-> FP64 conversions are invalid.
            if (srcType == DstFmt.F32 && dstType == DstFmt.F64)
            {
                srcB = context.FP32ConvertToFP64(srcB);
            }
            else if (srcType == DstFmt.F64 && dstType == DstFmt.F32)
            {
                srcB = context.FP64ConvertToFP32(srcB);
            }

            srcB = context.FPSaturate(srcB, saturate, dstType.ToInstFPType());

            WriteFP(context, dstType, srcB, rd);

            // TODO: CC.
        }

        private static void EmitF2I(
            EmitterContext context,
            DstFmt srcType,
            IDstFmt dstType,
            RoundMode2 roundingMode,
            Operand src,
            int rd,
            bool absolute,
            bool negate)
        {
            if (dstType == IDstFmt.U64)
            {
                context.TranslatorContext.GpuAccessor.Log("Unimplemented 64-bits F2I.");
            }

            Instruction fpType = srcType.ToInstFPType();

            bool isSignedInt = dstType == IDstFmt.S16 || dstType == IDstFmt.S32 || dstType == IDstFmt.S64;
            bool isSmallInt = dstType == IDstFmt.U16 || dstType == IDstFmt.S16;

            Operand srcB = context.FPAbsNeg(src, absolute, negate, fpType);

            srcB = roundingMode switch
            {
                RoundMode2.Round => context.FPRound(srcB, fpType),
                RoundMode2.Floor => context.FPFloor(srcB, fpType),
                RoundMode2.Ceil => context.FPCeiling(srcB, fpType),
                RoundMode2.Trunc => context.FPTruncate(srcB, fpType),
                _ => srcB,
            };

            if (!isSignedInt)
            {
                // Negative float to uint cast is undefined, so we clamp the value before conversion.
                Operand c0 = srcType == DstFmt.F64 ? context.PackDouble2x32(0.0) : ConstF(0);

                srcB = context.FPMaximum(srcB, c0, fpType);
            }

            if (srcType == DstFmt.F64)
            {
                srcB = isSignedInt
                    ? context.FP64ConvertToS32(srcB)
                    : context.FP64ConvertToU32(srcB);
            }
            else
            {
                srcB = isSignedInt
                    ? context.FP32ConvertToS32(srcB)
                    : context.FP32ConvertToU32(srcB);
            }

            if (isSmallInt)
            {
                int min = (int)GetIntMin(dstType);
                int max = (int)GetIntMax(dstType);

                srcB = isSignedInt
                    ? context.IClampS32(srcB, Const(min), Const(max))
                    : context.IClampU32(srcB, Const(min), Const(max));
            }

            Operand dest = GetDest(rd);

            context.Copy(dest, srcB);

            // TODO: CC.
        }

        private static void EmitI2F(
            EmitterContext context,
            ISrcFmt srcType,
            DstFmt dstType,
            Operand src,
            ByteSel byteSelection,
            int rd,
            bool absolute,
            bool negate)
        {
            bool isSignedInt =
                srcType == ISrcFmt.S8 ||
                srcType == ISrcFmt.S16 ||
                srcType == ISrcFmt.S32 ||
                srcType == ISrcFmt.S64;
            bool isSmallInt =
                srcType == ISrcFmt.U16 ||
                srcType == ISrcFmt.S16 ||
                srcType == ISrcFmt.U8 ||
                srcType == ISrcFmt.S8;

            // TODO: Handle S/U64.

            Operand srcB = context.IAbsNeg(src, absolute, negate);

            if (isSmallInt)
            {
                int size = srcType == ISrcFmt.U16 || srcType == ISrcFmt.S16 ? 16 : 8;

                srcB = isSignedInt
                    ? context.BitfieldExtractS32(srcB, Const((int)byteSelection * 8), Const(size))
                    : context.BitfieldExtractU32(srcB, Const((int)byteSelection * 8), Const(size));
            }

            if (dstType == DstFmt.F64)
            {
                srcB = isSignedInt
                    ? context.IConvertS32ToFP64(srcB)
                    : context.IConvertU32ToFP64(srcB);
            }
            else
            {
                srcB = isSignedInt
                    ? context.IConvertS32ToFP32(srcB)
                    : context.IConvertU32ToFP32(srcB);
            }

            WriteFP(context, dstType, srcB, rd);

            // TODO: CC.
        }

        private static void EmitI2I(
            EmitterContext context,
            ISrcDstFmt srcType,
            ISrcDstFmt dstType,
            Operand src,
            ByteSel byteSelection,
            int rd,
            bool absolute,
            bool negate,
            bool saturate,
            bool writeCC)
        {
            if ((srcType & ~ISrcDstFmt.S8) > ISrcDstFmt.U32 || (dstType & ~ISrcDstFmt.S8) > ISrcDstFmt.U32)
            {
                context.TranslatorContext.GpuAccessor.Log("Invalid I2I encoding.");
                return;
            }

            bool srcIsSignedInt =
                srcType == ISrcDstFmt.S8 ||
                srcType == ISrcDstFmt.S16 ||
                srcType == ISrcDstFmt.S32;
            bool dstIsSignedInt =
                dstType == ISrcDstFmt.S8 ||
                dstType == ISrcDstFmt.S16 ||
                dstType == ISrcDstFmt.S32;
            bool srcIsSmallInt =
                srcType == ISrcDstFmt.U16 ||
                srcType == ISrcDstFmt.S16 ||
                srcType == ISrcDstFmt.U8 ||
                srcType == ISrcDstFmt.S8;

            if (srcIsSmallInt)
            {
                int size = srcType == ISrcDstFmt.U16 || srcType == ISrcDstFmt.S16 ? 16 : 8;

                src = srcIsSignedInt
                    ? context.BitfieldExtractS32(src, Const((int)byteSelection * 8), Const(size))
                    : context.BitfieldExtractU32(src, Const((int)byteSelection * 8), Const(size));
            }

            src = context.IAbsNeg(src, absolute, negate);

            if (saturate)
            {
                int min = (int)GetIntMin(dstType);
                int max = (int)GetIntMax(dstType);

                src = dstIsSignedInt
                    ? context.IClampS32(src, Const(min), Const(max))
                    : context.IClampU32(src, Const(min), Const(max));
            }

            context.Copy(GetDest(rd), src);

            SetZnFlags(context, src, writeCC);
        }

        private static Operand UnpackReg(EmitterContext context, DstFmt floatType, bool h, int reg)
        {
            if (floatType == DstFmt.F32)
            {
                return GetSrcReg(context, reg);
            }
            else if (floatType == DstFmt.F16)
            {
                return GetHalfUnpacked(context, GetSrcReg(context, reg), HalfSwizzle.F16)[h ? 1 : 0];
            }
            else if (floatType == DstFmt.F64)
            {
                return GetSrcReg(context, reg, isFP64: true);
            }

            throw new ArgumentException($"Invalid floating point type \"{floatType}\".");
        }

        private static Operand UnpackCbuf(EmitterContext context, DstFmt floatType, bool h, int cbufSlot, int cbufOffset)
        {
            if (floatType == DstFmt.F32)
            {
                return GetSrcCbuf(context, cbufSlot, cbufOffset);
            }
            else if (floatType == DstFmt.F16)
            {
                return GetHalfUnpacked(context, GetSrcCbuf(context, cbufSlot, cbufOffset), HalfSwizzle.F16)[h ? 1 : 0];
            }
            else if (floatType == DstFmt.F64)
            {
                return GetSrcCbuf(context, cbufSlot, cbufOffset, isFP64: true);
            }

            throw new ArgumentException($"Invalid floating point type \"{floatType}\".");
        }

        private static Operand UnpackImm(EmitterContext context, DstFmt floatType, bool h, int imm)
        {
            if (floatType == DstFmt.F32)
            {
                return GetSrcImm(context, imm);
            }
            else if (floatType == DstFmt.F16)
            {
                return GetHalfUnpacked(context, GetSrcImm(context, imm), HalfSwizzle.F16)[h ? 1 : 0];
            }
            else if (floatType == DstFmt.F64)
            {
                return GetSrcImm(context, imm, isFP64: true);
            }

            throw new ArgumentException($"Invalid floating point type \"{floatType}\".");
        }

        private static void WriteFP(EmitterContext context, DstFmt type, Operand srcB, int rd)
        {
            Operand dest = GetDest(rd);

            if (type == DstFmt.F32)
            {
                context.Copy(dest, srcB);
            }
            else if (type == DstFmt.F16)
            {
                context.Copy(dest, context.PackHalf2x16(srcB, ConstF(0)));
            }
            else /* if (type == FPType.FP64) */
            {
                Operand dest2 = GetDest2(rd);

                context.Copy(dest, context.UnpackDouble2x32Low(srcB));
                context.Copy(dest2, context.UnpackDouble2x32High(srcB));
            }
        }

        private static Instruction ToInstFPType(this DstFmt type)
        {
            return type == DstFmt.F64 ? Instruction.FP64 : Instruction.FP32;
        }
    }
}
