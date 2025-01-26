using Ryujinx.Graphics.Shader.Decoders;
using Ryujinx.Graphics.Shader.IntermediateRepresentation;
using Ryujinx.Graphics.Shader.Translation;
using System;
using System.Runtime.CompilerServices;
using static Ryujinx.Graphics.Shader.IntermediateRepresentation.OperandHelper;

namespace Ryujinx.Graphics.Shader.Instructions
{
    static class InstEmitHelper
    {
        public static Operand GetZF()
        {
            return Register(0, RegisterType.Flag);
        }

        public static Operand GetNF()
        {
            return Register(1, RegisterType.Flag);
        }

        public static Operand GetCF()
        {
            return Register(2, RegisterType.Flag);
        }

        public static Operand GetVF()
        {
            return Register(3, RegisterType.Flag);
        }

        public static Operand GetDest(int rd)
        {
            return Register(rd, RegisterType.Gpr);
        }

        public static Operand GetDest2(int rd)
        {
            return Register(rd | 1, RegisterType.Gpr);
        }

        public static Operand GetSrcCbuf(EmitterContext context, int cbufSlot, int cbufOffset, bool isFP64 = false)
        {
            if (isFP64)
            {
                return context.PackDouble2x32(
                    Cbuf(cbufSlot, cbufOffset),
                    Cbuf(cbufSlot, cbufOffset + 1));
            }
            else
            {
                return Cbuf(cbufSlot, cbufOffset);
            }
        }

        public static Operand GetSrcImm(EmitterContext context, int imm, bool isFP64 = false)
        {
            if (isFP64)
            {
                return context.PackDouble2x32(Const(0), Const(imm));
            }
            else
            {
                return Const(imm);
            }
        }

        public static Operand GetSrcReg(EmitterContext context, int reg, bool isFP64 = false)
        {
            if (isFP64)
            {
                return context.PackDouble2x32(Register(reg, RegisterType.Gpr), Register(reg | 1, RegisterType.Gpr));
            }
            else
            {
                return Register(reg, RegisterType.Gpr);
            }
        }

        public static Operand[] GetHalfSrc(
            EmitterContext context,
            HalfSwizzle swizzle,
            int ra,
            bool negate,
            bool absolute)
        {
            Operand[] operands = GetHalfUnpacked(context, GetSrcReg(context, ra), swizzle);

            return FPAbsNeg(context, operands, absolute, negate);
        }

        public static Operand[] GetHalfSrc(
            EmitterContext context,
            HalfSwizzle swizzle,
            int cbufSlot,
            int cbufOffset,
            bool negate,
            bool absolute)
        {
            Operand[] operands = GetHalfUnpacked(context, GetSrcCbuf(context, cbufSlot, cbufOffset), swizzle);

            return FPAbsNeg(context, operands, absolute, negate);
        }

        public static Operand[] GetHalfSrc(EmitterContext context, int immH0, int immH1)
        {
            ushort low = (ushort)(immH0 << 6);
            ushort high = (ushort)(immH1 << 6);

            return
            [
                ConstF((float)Unsafe.As<ushort, Half>(ref low)),
                ConstF((float)Unsafe.As<ushort, Half>(ref high))
            ];
        }

        public static Operand[] GetHalfSrc(EmitterContext context, int imm32)
        {
            ushort low = (ushort)imm32;
            ushort high = (ushort)(imm32 >> 16);

            return
            [
                ConstF((float)Unsafe.As<ushort, Half>(ref low)),
                ConstF((float)Unsafe.As<ushort, Half>(ref high))
            ];
        }

        public static Operand[] FPAbsNeg(EmitterContext context, Operand[] operands, bool abs, bool neg)
        {
            for (int index = 0; index < operands.Length; index++)
            {
                operands[index] = context.FPAbsNeg(operands[index], abs, neg);
            }

            return operands;
        }

        public static Operand[] GetHalfUnpacked(EmitterContext context, Operand src, HalfSwizzle swizzle)
        {
            return swizzle switch
            {
                HalfSwizzle.F16 =>
                [
                    context.UnpackHalf2x16Low (src),
                        context.UnpackHalf2x16High(src)
                ],
                HalfSwizzle.F32 => [src, src],
                HalfSwizzle.H0H0 =>
                [
                    context.UnpackHalf2x16Low(src),
                        context.UnpackHalf2x16Low(src)
                ],
                HalfSwizzle.H1H1 =>
                [
                    context.UnpackHalf2x16High(src),
                        context.UnpackHalf2x16High(src)
                ],
                _ => throw new ArgumentException($"Invalid swizzle \"{swizzle}\"."),
            };
        }

        public static Operand GetHalfPacked(EmitterContext context, OFmt swizzle, Operand[] results, int rd)
        {
            switch (swizzle)
            {
                case OFmt.F16:
                    return context.PackHalf2x16(results[0], results[1]);

                case OFmt.F32:
                    return results[0];

                case OFmt.MrgH0:
                    {
                        Operand h1 = GetHalfDest(context, rd, isHigh: true);

                        return context.PackHalf2x16(results[0], h1);
                    }

                case OFmt.MrgH1:
                    {
                        Operand h0 = GetHalfDest(context, rd, isHigh: false);

                        return context.PackHalf2x16(h0, results[1]);
                    }
            }

            throw new ArgumentException($"Invalid swizzle \"{swizzle}\".");
        }

        public static Operand GetHalfDest(EmitterContext context, int rd, bool isHigh)
        {
            if (isHigh)
            {
                return context.UnpackHalf2x16High(GetDest(rd));
            }
            else
            {
                return context.UnpackHalf2x16Low(GetDest(rd));
            }
        }

        public static Operand GetPredicate(EmitterContext context, int pred, bool not)
        {
            Operand local = Register(pred, RegisterType.Predicate);

            if (not)
            {
                local = context.BitwiseNot(local);
            }

            return local;
        }

        public static void SetDest(EmitterContext context, Operand value, int rd, bool isFP64)
        {
            if (isFP64)
            {
                context.Copy(GetDest(rd), context.UnpackDouble2x32Low(value));
                context.Copy(GetDest2(rd), context.UnpackDouble2x32High(value));
            }
            else
            {
                context.Copy(GetDest(rd), value);
            }
        }

        public static int Imm16ToSInt(int imm16)
        {
            return (short)imm16;
        }

        public static int Imm20ToFloat(int imm20)
        {
            return imm20 << 12;
        }

        public static int Imm20ToSInt(int imm20)
        {
            return (imm20 << 12) >> 12;
        }

        public static int Imm24ToSInt(int imm24)
        {
            return (imm24 << 8) >> 8;
        }

        public static Operand SignExtendTo32(EmitterContext context, Operand src, int srcBits)
        {
            return context.BitfieldExtractS32(src, Const(0), Const(srcBits));
        }

        public static Operand ZeroExtendTo32(EmitterContext context, Operand src, int srcBits)
        {
            int mask = (int)(uint.MaxValue >> (32 - srcBits));

            return context.BitwiseAnd(src, Const(mask));
        }
    }
}
