using ARMeilleure.State;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace ARMeilleure.Instructions
{
    static class SoftFloat
    {
        static SoftFloat()
        {
            RecipEstimateTable = BuildRecipEstimateTable();
            RecipSqrtEstimateTable = BuildRecipSqrtEstimateTable();
        }

        public static readonly byte[] RecipEstimateTable;
        public static readonly byte[] RecipSqrtEstimateTable;

        private static byte[] BuildRecipEstimateTable()
        {
            byte[] tbl = new byte[256];

            for (int idx = 0; idx < 256; idx++)
            {
                uint src = (uint)idx + 256u;

                Debug.Assert(256u <= src && src < 512u);

                src = (src << 1) + 1u;

                uint aux = (1u << 19) / src;

                uint dst = (aux + 1u) >> 1;

                Debug.Assert(256u <= dst && dst < 512u);

                tbl[idx] = (byte)(dst - 256u);
            }

            return tbl;
        }

        private static byte[] BuildRecipSqrtEstimateTable()
        {
            byte[] tbl = new byte[384];

            for (int idx = 0; idx < 384; idx++)
            {
                uint src = (uint)idx + 128u;

                Debug.Assert(128u <= src && src < 512u);

                if (src < 256u)
                {
                    src = (src << 1) + 1u;
                }
                else
                {
                    src = (src >> 1) << 1;
                    src = (src + 1u) << 1;
                }

                uint aux = 512u;

                while (src * (aux + 1u) * (aux + 1u) < (1u << 28))
                {
                    aux++;
                }

                uint dst = (aux + 1u) >> 1;

                Debug.Assert(256u <= dst && dst < 512u);

                tbl[idx] = (byte)(dst - 256u);
            }

            return tbl;
        }

        public static void FPProcessException(FPException exc, ExecutionContext context)
        {
            FPProcessException(exc, context, context.Fpcr);
        }

        public static void FPProcessException(FPException exc, ExecutionContext context, FPCR fpcr)
        {
            int enable = (int)exc + 8;

            if ((fpcr & (FPCR)(1 << enable)) != 0)
            {
                throw new NotImplementedException("Floating-point trap handling.");
            }
            else
            {
                context.Fpsr |= (FPSR)(1 << (int)exc);
            }
        }

        public static FPRoundingMode GetRoundingMode(this FPCR fpcr)
        {
            const int RModeShift = 22;

            return (FPRoundingMode)(((uint)fpcr >> RModeShift) & 3u);
        }
    }

    static class SoftFloat16
    {
        public static ushort FPDefaultNaN()
        {
            return (ushort)0x7E00u;
        }

        public static ushort FPInfinity(bool sign)
        {
            return sign ? (ushort)0xFC00u : (ushort)0x7C00u;
        }

        public static ushort FPZero(bool sign)
        {
            return sign ? (ushort)0x8000u : (ushort)0x0000u;
        }

        public static ushort FPMaxNormal(bool sign)
        {
            return sign ? (ushort)0xFBFFu : (ushort)0x7BFFu;
        }

        public static double FPUnpackCv(
            this ushort valueBits,
            out FPType type,
            out bool sign,
            ExecutionContext context)
        {
            sign = (~(uint)valueBits & 0x8000u) == 0u;

            uint exp16 = ((uint)valueBits & 0x7C00u) >> 10;
            uint frac16 = (uint)valueBits & 0x03FFu;

            double real;

            if (exp16 == 0u)
            {
                if (frac16 == 0u)
                {
                    type = FPType.Zero;
                    real = 0d;
                }
                else
                {
                    type = FPType.Nonzero; // Subnormal.
                    real = Math.Pow(2d, -14) * ((double)frac16 * Math.Pow(2d, -10));
                }
            }
            else if (exp16 == 0x1Fu && (context.Fpcr & FPCR.Ahp) == 0)
            {
                if (frac16 == 0u)
                {
                    type = FPType.Infinity;
                    real = Math.Pow(2d, 1000);
                }
                else
                {
                    type = (~frac16 & 0x0200u) == 0u ? FPType.QNaN : FPType.SNaN;
                    real = 0d;
                }
            }
            else
            {
                type = FPType.Nonzero; // Normal.
                real = Math.Pow(2d, (int)exp16 - 15) * (1d + (double)frac16 * Math.Pow(2d, -10));
            }

            return sign ? -real : real;
        }

        public static ushort FPRoundCv(double real, ExecutionContext context)
        {
            const int MinimumExp = -14;

            const int E = 5;
            const int F = 10;

            bool sign;
            double mantissa;

            if (real < 0d)
            {
                sign = true;
                mantissa = -real;
            }
            else
            {
                sign = false;
                mantissa = real;
            }

            int exponent = 0;

            while (mantissa < 1d)
            {
                mantissa *= 2d;
                exponent--;
            }

            while (mantissa >= 2d)
            {
                mantissa /= 2d;
                exponent++;
            }

            uint biasedExp = (uint)Math.Max(exponent - MinimumExp + 1, 0);

            if (biasedExp == 0u)
            {
                mantissa /= Math.Pow(2d, MinimumExp - exponent);
            }

            uint intMant = (uint)Math.Floor(mantissa * Math.Pow(2d, F));
            double error = mantissa * Math.Pow(2d, F) - (double)intMant;

            if (biasedExp == 0u && (error != 0d || (context.Fpcr & FPCR.Ufe) != 0))
            {
                SoftFloat.FPProcessException(FPException.Underflow, context);
            }

            bool overflowToInf;
            bool roundUp;

            switch (context.Fpcr.GetRoundingMode())
            {
                case FPRoundingMode.ToNearest:
                    roundUp = (error > 0.5d || (error == 0.5d && (intMant & 1u) == 1u));
                    overflowToInf = true;
                    break;

                case FPRoundingMode.TowardsPlusInfinity:
                    roundUp = (error != 0d && !sign);
                    overflowToInf = !sign;
                    break;

                case FPRoundingMode.TowardsMinusInfinity:
                    roundUp = (error != 0d && sign);
                    overflowToInf = sign;
                    break;

                case FPRoundingMode.TowardsZero:
                    roundUp = false;
                    overflowToInf = false;
                    break;

                default:
                    throw new ArgumentException($"Invalid rounding mode \"{context.Fpcr.GetRoundingMode()}\".");
            }

            if (roundUp)
            {
                intMant++;

                if (intMant == 1u << F)
                {
                    biasedExp = 1u;
                }

                if (intMant == 1u << (F + 1))
                {
                    biasedExp++;
                    intMant >>= 1;
                }
            }

            ushort resultBits;

            if ((context.Fpcr & FPCR.Ahp) == 0)
            {
                if (biasedExp >= (1u << E) - 1u)
                {
                    resultBits = overflowToInf ? FPInfinity(sign) : FPMaxNormal(sign);

                    SoftFloat.FPProcessException(FPException.Overflow, context);

                    error = 1d;
                }
                else
                {
                    resultBits = (ushort)((sign ? 1u : 0u) << 15 | (biasedExp & 0x1Fu) << 10 | (intMant & 0x03FFu));
                }
            }
            else
            {
                if (biasedExp >= 1u << E)
                {
                    resultBits = (ushort)((sign ? 1u : 0u) << 15 | 0x7FFFu);

                    SoftFloat.FPProcessException(FPException.InvalidOp, context);

                    error = 0d;
                }
                else
                {
                    resultBits = (ushort)((sign ? 1u : 0u) << 15 | (biasedExp & 0x1Fu) << 10 | (intMant & 0x03FFu));
                }
            }

            if (error != 0d)
            {
                SoftFloat.FPProcessException(FPException.Inexact, context);
            }

            return resultBits;
        }
    }

    static class SoftFloat16_32
    {
        [UnmanagedCallersOnly]
        public static float FPConvert(ushort valueBits)
        {
            ExecutionContext context = NativeInterface.GetContext();

            double real = valueBits.FPUnpackCv(out FPType type, out bool sign, context);

            float result;

            if (type == FPType.SNaN || type == FPType.QNaN)
            {
                if ((context.Fpcr & FPCR.Dn) != 0)
                {
                    result = SoftFloat32.FPDefaultNaN();
                }
                else
                {
                    result = FPConvertNaN(valueBits);
                }

                if (type == FPType.SNaN)
                {
                    SoftFloat.FPProcessException(FPException.InvalidOp, context);
                }
            }
            else if (type == FPType.Infinity)
            {
                result = SoftFloat32.FPInfinity(sign);
            }
            else if (type == FPType.Zero)
            {
                result = SoftFloat32.FPZero(sign);
            }
            else
            {
                result = FPRoundCv(real, context);
            }

            return result;
        }

        private static float FPRoundCv(double real, ExecutionContext context)
        {
            const int MinimumExp = -126;

            const int E = 8;
            const int F = 23;

            bool sign;
            double mantissa;

            if (real < 0d)
            {
                sign = true;
                mantissa = -real;
            }
            else
            {
                sign = false;
                mantissa = real;
            }

            int exponent = 0;

            while (mantissa < 1d)
            {
                mantissa *= 2d;
                exponent--;
            }

            while (mantissa >= 2d)
            {
                mantissa /= 2d;
                exponent++;
            }

            if ((context.Fpcr & FPCR.Fz) != 0 && exponent < MinimumExp)
            {
                context.Fpsr |= FPSR.Ufc;

                return SoftFloat32.FPZero(sign);
            }

            uint biasedExp = (uint)Math.Max(exponent - MinimumExp + 1, 0);

            if (biasedExp == 0u)
            {
                mantissa /= Math.Pow(2d, MinimumExp - exponent);
            }

            uint intMant = (uint)Math.Floor(mantissa * Math.Pow(2d, F));
            double error = mantissa * Math.Pow(2d, F) - (double)intMant;

            if (biasedExp == 0u && (error != 0d || (context.Fpcr & FPCR.Ufe) != 0))
            {
                SoftFloat.FPProcessException(FPException.Underflow, context);
            }

            bool overflowToInf;
            bool roundUp;

            switch (context.Fpcr.GetRoundingMode())
            {
                case FPRoundingMode.ToNearest:
                    roundUp = (error > 0.5d || (error == 0.5d && (intMant & 1u) == 1u));
                    overflowToInf = true;
                    break;

                case FPRoundingMode.TowardsPlusInfinity:
                    roundUp = (error != 0d && !sign);
                    overflowToInf = !sign;
                    break;

                case FPRoundingMode.TowardsMinusInfinity:
                    roundUp = (error != 0d && sign);
                    overflowToInf = sign;
                    break;

                case FPRoundingMode.TowardsZero:
                    roundUp = false;
                    overflowToInf = false;
                    break;

                default:
                    throw new ArgumentException($"Invalid rounding mode \"{context.Fpcr.GetRoundingMode()}\".");
            }

            if (roundUp)
            {
                intMant++;

                if (intMant == 1u << F)
                {
                    biasedExp = 1u;
                }

                if (intMant == 1u << (F + 1))
                {
                    biasedExp++;
                    intMant >>= 1;
                }
            }

            float result;

            if (biasedExp >= (1u << E) - 1u)
            {
                result = overflowToInf ? SoftFloat32.FPInfinity(sign) : SoftFloat32.FPMaxNormal(sign);

                SoftFloat.FPProcessException(FPException.Overflow, context);

                error = 1d;
            }
            else
            {
                result = BitConverter.Int32BitsToSingle(
                    (int)((sign ? 1u : 0u) << 31 | (biasedExp & 0xFFu) << 23 | (intMant & 0x007FFFFFu)));
            }

            if (error != 0d)
            {
                SoftFloat.FPProcessException(FPException.Inexact, context);
            }

            return result;
        }

        private static float FPConvertNaN(ushort valueBits)
        {
            return BitConverter.Int32BitsToSingle(
                (int)(((uint)valueBits & 0x8000u) << 16 | 0x7FC00000u | ((uint)valueBits & 0x01FFu) << 13));
        }
    }

    static class SoftFloat16_64
    {
        [UnmanagedCallersOnly]
        public static double FPConvert(ushort valueBits)
        {
            ExecutionContext context = NativeInterface.GetContext();

            double real = valueBits.FPUnpackCv(out FPType type, out bool sign, context);

            double result;

            if (type == FPType.SNaN || type == FPType.QNaN)
            {
                if ((context.Fpcr & FPCR.Dn) != 0)
                {
                    result = SoftFloat64.FPDefaultNaN();
                }
                else
                {
                    result = FPConvertNaN(valueBits);
                }

                if (type == FPType.SNaN)
                {
                    SoftFloat.FPProcessException(FPException.InvalidOp, context);
                }
            }
            else if (type == FPType.Infinity)
            {
                result = SoftFloat64.FPInfinity(sign);
            }
            else if (type == FPType.Zero)
            {
                result = SoftFloat64.FPZero(sign);
            }
            else
            {
                result = FPRoundCv(real, context);
            }

            return result;
        }

        private static double FPRoundCv(double real, ExecutionContext context)
        {
            const int MinimumExp = -1022;

            const int E = 11;
            const int F = 52;

            bool sign;
            double mantissa;

            if (real < 0d)
            {
                sign = true;
                mantissa = -real;
            }
            else
            {
                sign = false;
                mantissa = real;
            }

            int exponent = 0;

            while (mantissa < 1d)
            {
                mantissa *= 2d;
                exponent--;
            }

            while (mantissa >= 2d)
            {
                mantissa /= 2d;
                exponent++;
            }

            if ((context.Fpcr & FPCR.Fz) != 0 && exponent < MinimumExp)
            {
                context.Fpsr |= FPSR.Ufc;

                return SoftFloat64.FPZero(sign);
            }

            uint biasedExp = (uint)Math.Max(exponent - MinimumExp + 1, 0);

            if (biasedExp == 0u)
            {
                mantissa /= Math.Pow(2d, MinimumExp - exponent);
            }

            ulong intMant = (ulong)Math.Floor(mantissa * Math.Pow(2d, F));
            double error = mantissa * Math.Pow(2d, F) - (double)intMant;

            if (biasedExp == 0u && (error != 0d || (context.Fpcr & FPCR.Ufe) != 0))
            {
                SoftFloat.FPProcessException(FPException.Underflow, context);
            }

            bool overflowToInf;
            bool roundUp;

            switch (context.Fpcr.GetRoundingMode())
            {
                case FPRoundingMode.ToNearest:
                    roundUp = (error > 0.5d || (error == 0.5d && (intMant & 1u) == 1u));
                    overflowToInf = true;
                    break;

                case FPRoundingMode.TowardsPlusInfinity:
                    roundUp = (error != 0d && !sign);
                    overflowToInf = !sign;
                    break;

                case FPRoundingMode.TowardsMinusInfinity:
                    roundUp = (error != 0d && sign);
                    overflowToInf = sign;
                    break;

                case FPRoundingMode.TowardsZero:
                    roundUp = false;
                    overflowToInf = false;
                    break;

                default:
                    throw new ArgumentException($"Invalid rounding mode \"{context.Fpcr.GetRoundingMode()}\".");
            }

            if (roundUp)
            {
                intMant++;

                if (intMant == 1ul << F)
                {
                    biasedExp = 1u;
                }

                if (intMant == 1ul << (F + 1))
                {
                    biasedExp++;
                    intMant >>= 1;
                }
            }

            double result;

            if (biasedExp >= (1u << E) - 1u)
            {
                result = overflowToInf ? SoftFloat64.FPInfinity(sign) : SoftFloat64.FPMaxNormal(sign);

                SoftFloat.FPProcessException(FPException.Overflow, context);

                error = 1d;
            }
            else
            {
                result = BitConverter.Int64BitsToDouble(
                    (long)((sign ? 1ul : 0ul) << 63 | (biasedExp & 0x7FFul) << 52 | (intMant & 0x000FFFFFFFFFFFFFul)));
            }

            if (error != 0d)
            {
                SoftFloat.FPProcessException(FPException.Inexact, context);
            }

            return result;
        }

        private static double FPConvertNaN(ushort valueBits)
        {
            return BitConverter.Int64BitsToDouble(
                (long)(((ulong)valueBits & 0x8000ul) << 48 | 0x7FF8000000000000ul | ((ulong)valueBits & 0x01FFul) << 42));
        }
    }

    static class SoftFloat32_16
    {
        [UnmanagedCallersOnly]
        public static ushort FPConvert(float value)
        {
            ExecutionContext context = NativeInterface.GetContext();

            double real = value.FPUnpackCv(out FPType type, out bool sign, out uint valueBits, context);

            bool altHp = (context.Fpcr & FPCR.Ahp) != 0;

            ushort resultBits;

            if (type == FPType.SNaN || type == FPType.QNaN)
            {
                if (altHp)
                {
                    resultBits = SoftFloat16.FPZero(sign);
                }
                else if ((context.Fpcr & FPCR.Dn) != 0)
                {
                    resultBits = SoftFloat16.FPDefaultNaN();
                }
                else
                {
                    resultBits = FPConvertNaN(valueBits);
                }

                if (type == FPType.SNaN || altHp)
                {
                    SoftFloat.FPProcessException(FPException.InvalidOp, context);
                }
            }
            else if (type == FPType.Infinity)
            {
                if (altHp)
                {
                    resultBits = (ushort)((sign ? 1u : 0u) << 15 | 0x7FFFu);

                    SoftFloat.FPProcessException(FPException.InvalidOp, context);
                }
                else
                {
                    resultBits = SoftFloat16.FPInfinity(sign);
                }
            }
            else if (type == FPType.Zero)
            {
                resultBits = SoftFloat16.FPZero(sign);
            }
            else
            {
                resultBits = SoftFloat16.FPRoundCv(real, context);
            }

            return resultBits;
        }

        private static double FPUnpackCv(
            this float value,
            out FPType type,
            out bool sign,
            out uint valueBits,
            ExecutionContext context)
        {
            valueBits = (uint)BitConverter.SingleToInt32Bits(value);

            sign = (~valueBits & 0x80000000u) == 0u;

            uint exp32 = (valueBits & 0x7F800000u) >> 23;
            uint frac32 = valueBits & 0x007FFFFFu;

            double real;

            if (exp32 == 0u)
            {
                if (frac32 == 0u || (context.Fpcr & FPCR.Fz) != 0)
                {
                    type = FPType.Zero;
                    real = 0d;

                    if (frac32 != 0u)
                    {
                        SoftFloat.FPProcessException(FPException.InputDenorm, context);
                    }
                }
                else
                {
                    type = FPType.Nonzero; // Subnormal.
                    real = Math.Pow(2d, -126) * ((double)frac32 * Math.Pow(2d, -23));
                }
            }
            else if (exp32 == 0xFFu)
            {
                if (frac32 == 0u)
                {
                    type = FPType.Infinity;
                    real = Math.Pow(2d, 1000);
                }
                else
                {
                    type = (~frac32 & 0x00400000u) == 0u ? FPType.QNaN : FPType.SNaN;
                    real = 0d;
                }
            }
            else
            {
                type = FPType.Nonzero; // Normal.
                real = Math.Pow(2d, (int)exp32 - 127) * (1d + (double)frac32 * Math.Pow(2d, -23));
            }

            return sign ? -real : real;
        }

        private static ushort FPConvertNaN(uint valueBits)
        {
            return (ushort)((valueBits & 0x80000000u) >> 16 | 0x7E00u | (valueBits & 0x003FE000u) >> 13);
        }
    }

    static class SoftFloat32
    {
        [UnmanagedCallersOnly]
        public static float FPAdd(float value1, float value2)
        {
            return FPAddFpscrImpl(value1, value2, false);
        }

        [UnmanagedCallersOnly]
        public static float FPAddFpscr(float value1, float value2, byte standardFpscr)
        {
            return FPAddFpscrImpl(value1, value2, standardFpscr == 1);
        }

        private static float FPAddFpscrImpl(float value1, float value2, bool standardFpscr)
        {
            ExecutionContext context = NativeInterface.GetContext();
            FPCR fpcr = standardFpscr ? context.StandardFpcrValue : context.Fpcr;

            value1 = value1.FPUnpack(out FPType type1, out bool sign1, out uint op1, context, fpcr);
            value2 = value2.FPUnpack(out FPType type2, out bool sign2, out uint op2, context, fpcr);

            float result = FPProcessNaNs(type1, type2, op1, op2, out bool done, context, fpcr);

            if (!done)
            {
                bool inf1 = type1 == FPType.Infinity;
                bool zero1 = type1 == FPType.Zero;
                bool inf2 = type2 == FPType.Infinity;
                bool zero2 = type2 == FPType.Zero;

                if (inf1 && inf2 && sign1 == !sign2)
                {
                    result = FPDefaultNaN();

                    SoftFloat.FPProcessException(FPException.InvalidOp, context, fpcr);
                }
                else if ((inf1 && !sign1) || (inf2 && !sign2))
                {
                    result = FPInfinity(false);
                }
                else if ((inf1 && sign1) || (inf2 && sign2))
                {
                    result = FPInfinity(true);
                }
                else if (zero1 && zero2 && sign1 == sign2)
                {
                    result = FPZero(sign1);
                }
                else
                {
                    result = value1 + value2;

                    if ((fpcr & FPCR.Fz) != 0 && float.IsSubnormal(result))
                    {
                        context.Fpsr |= FPSR.Ufc;

                        result = FPZero(result < 0f);
                    }
                }
            }

            return result;
        }

        [UnmanagedCallersOnly]
        public static int FPCompare(float value1, float value2, byte signalNaNs)
        {
            ExecutionContext context = NativeInterface.GetContext();
            FPCR fpcr = context.Fpcr;

            value1 = value1.FPUnpack(out FPType type1, out _, out _, context, fpcr);
            value2 = value2.FPUnpack(out FPType type2, out _, out _, context, fpcr);

            int result;

            if (type1 == FPType.SNaN || type1 == FPType.QNaN || type2 == FPType.SNaN || type2 == FPType.QNaN)
            {
                result = 0b0011;

                if (type1 == FPType.SNaN || type2 == FPType.SNaN || signalNaNs == 1)
                {
                    SoftFloat.FPProcessException(FPException.InvalidOp, context, fpcr);
                }
            }
            else
            {
                if (value1 == value2)
                {
                    result = 0b0110;
                }
                else if (value1 < value2)
                {
                    result = 0b1000;
                }
                else
                {
                    result = 0b0010;
                }
            }

            return result;
        }

        [UnmanagedCallersOnly]
        public static float FPCompareEQ(float value1, float value2)
        {
            return FPCompareEQFpscrImpl(value1, value2, false);
        }

        private static float FPCompareEQFpscrImpl(float value1, float value2, bool standardFpscr)
        {
            ExecutionContext context = NativeInterface.GetContext();
            FPCR fpcr = standardFpscr ? context.StandardFpcrValue : context.Fpcr;

            value1 = value1.FPUnpack(out FPType type1, out _, out _, context, fpcr);
            value2 = value2.FPUnpack(out FPType type2, out _, out _, context, fpcr);

            float result;

            if (type1 == FPType.SNaN || type1 == FPType.QNaN || type2 == FPType.SNaN || type2 == FPType.QNaN)
            {
                result = ZerosOrOnes(false);

                if (type1 == FPType.SNaN || type2 == FPType.SNaN)
                {
                    SoftFloat.FPProcessException(FPException.InvalidOp, context, fpcr);
                }
            }
            else
            {
                result = ZerosOrOnes(value1 == value2);
            }

            return result;
        }

        [UnmanagedCallersOnly]
        public static float FPCompareEQFpscr(float value1, float value2, byte standardFpscr)
        {
            return FPCompareEQFpscrImpl(value1, value2, standardFpscr == 1);
        }

        [UnmanagedCallersOnly]
        public static float FPCompareGE(float value1, float value2)
        {
            return FPCompareGEFpscrImpl(value1, value2, false);
        }

        [UnmanagedCallersOnly]
        public static float FPCompareGEFpscr(float value1, float value2, byte standardFpscr)
        {
            return FPCompareGEFpscrImpl(value1, value2, standardFpscr == 1);
        }

        private static float FPCompareGEFpscrImpl(float value1, float value2, bool standardFpscr)
        {
            ExecutionContext context = NativeInterface.GetContext();
            FPCR fpcr = standardFpscr ? context.StandardFpcrValue : context.Fpcr;

            value1 = value1.FPUnpack(out FPType type1, out _, out _, context, fpcr);
            value2 = value2.FPUnpack(out FPType type2, out _, out _, context, fpcr);

            float result;

            if (type1 == FPType.SNaN || type1 == FPType.QNaN || type2 == FPType.SNaN || type2 == FPType.QNaN)
            {
                result = ZerosOrOnes(false);

                SoftFloat.FPProcessException(FPException.InvalidOp, context, fpcr);
            }
            else
            {
                result = ZerosOrOnes(value1 >= value2);
            }

            return result;
        }

        [UnmanagedCallersOnly]
        public static float FPCompareGT(float value1, float value2)
        {
            return FPCompareGTFpscrImpl(value1, value2, false);
        }

        [UnmanagedCallersOnly]
        public static float FPCompareGTFpscr(float value1, float value2, byte standardFpscr)
        {
            return FPCompareGTFpscrImpl(value1, value2, standardFpscr == 1);
        }

        private static float FPCompareGTFpscrImpl(float value1, float value2, bool standardFpscr)
        {
            ExecutionContext context = NativeInterface.GetContext();
            FPCR fpcr = standardFpscr ? context.StandardFpcrValue : context.Fpcr;

            value1 = value1.FPUnpack(out FPType type1, out _, out _, context, fpcr);
            value2 = value2.FPUnpack(out FPType type2, out _, out _, context, fpcr);

            float result;

            if (type1 == FPType.SNaN || type1 == FPType.QNaN || type2 == FPType.SNaN || type2 == FPType.QNaN)
            {
                result = ZerosOrOnes(false);

                SoftFloat.FPProcessException(FPException.InvalidOp, context, fpcr);
            }
            else
            {
                result = ZerosOrOnes(value1 > value2);
            }

            return result;
        }

        [UnmanagedCallersOnly]
        public static float FPCompareLE(float value1, float value2)
        {
            return FPCompareGEFpscrImpl(value2, value1, false);
        }

        [UnmanagedCallersOnly]
        public static float FPCompareLT(float value1, float value2)
        {
            return FPCompareGTFpscrImpl(value2, value1, false);
        }

        [UnmanagedCallersOnly]
        public static float FPCompareLEFpscr(float value1, float value2, byte standardFpscr)
        {
            return FPCompareGEFpscrImpl(value2, value1, standardFpscr == 1);
        }

        [UnmanagedCallersOnly]
        public static float FPCompareLTFpscr(float value1, float value2, byte standardFpscr)
        {
            return FPCompareGEFpscrImpl(value2, value1, standardFpscr == 1);
        }

        [UnmanagedCallersOnly]
        public static float FPDiv(float value1, float value2)
        {
            ExecutionContext context = NativeInterface.GetContext();
            FPCR fpcr = context.Fpcr;

            value1 = value1.FPUnpack(out FPType type1, out bool sign1, out uint op1, context, fpcr);
            value2 = value2.FPUnpack(out FPType type2, out bool sign2, out uint op2, context, fpcr);

            float result = FPProcessNaNs(type1, type2, op1, op2, out bool done, context, fpcr);

            if (!done)
            {
                bool inf1 = type1 == FPType.Infinity;
                bool zero1 = type1 == FPType.Zero;
                bool inf2 = type2 == FPType.Infinity;
                bool zero2 = type2 == FPType.Zero;

                if ((inf1 && inf2) || (zero1 && zero2))
                {
                    result = FPDefaultNaN();

                    SoftFloat.FPProcessException(FPException.InvalidOp, context, fpcr);
                }
                else if (inf1 || zero2)
                {
                    result = FPInfinity(sign1 ^ sign2);

                    if (!inf1)
                    {
                        SoftFloat.FPProcessException(FPException.DivideByZero, context, fpcr);
                    }
                }
                else if (zero1 || inf2)
                {
                    result = FPZero(sign1 ^ sign2);
                }
                else
                {
                    result = value1 / value2;

                    if ((fpcr & FPCR.Fz) != 0 && float.IsSubnormal(result))
                    {
                        context.Fpsr |= FPSR.Ufc;

                        result = FPZero(result < 0f);
                    }
                }
            }

            return result;
        }

        [UnmanagedCallersOnly]
        public static float FPMax(float value1, float value2)
        {
            return FPMaxFpscrImpl(value1, value2, false);
        }

        [UnmanagedCallersOnly]
        public static float FPMaxFpscr(float value1, float value2, byte standardFpscr)
        {
            return FPMaxFpscrImpl(value1, value2, standardFpscr == 1);
        }
        
        private static float FPMaxFpscrImpl(float value1, float value2, bool standardFpscr)
        {
            ExecutionContext context = NativeInterface.GetContext();
            FPCR fpcr = standardFpscr ? context.StandardFpcrValue : context.Fpcr;

            value1 = value1.FPUnpack(out FPType type1, out bool sign1, out uint op1, context, fpcr);
            value2 = value2.FPUnpack(out FPType type2, out bool sign2, out uint op2, context, fpcr);

            float result = FPProcessNaNs(type1, type2, op1, op2, out bool done, context, fpcr);

            if (!done)
            {
                if (value1 > value2)
                {
                    if (type1 == FPType.Infinity)
                    {
                        result = FPInfinity(sign1);
                    }
                    else if (type1 == FPType.Zero)
                    {
                        result = FPZero(sign1 && sign2);
                    }
                    else
                    {
                        result = value1;

                        if ((fpcr & FPCR.Fz) != 0 && float.IsSubnormal(result))
                        {
                            context.Fpsr |= FPSR.Ufc;

                            result = FPZero(result < 0f);
                        }
                    }
                }
                else
                {
                    if (type2 == FPType.Infinity)
                    {
                        result = FPInfinity(sign2);
                    }
                    else if (type2 == FPType.Zero)
                    {
                        result = FPZero(sign1 && sign2);
                    }
                    else
                    {
                        result = value2;

                        if ((fpcr & FPCR.Fz) != 0 && float.IsSubnormal(result))
                        {
                            context.Fpsr |= FPSR.Ufc;

                            result = FPZero(result < 0f);
                        }
                    }
                }
            }

            return result;
        }

        [UnmanagedCallersOnly]
        public static float FPMaxNum(float value1, float value2)
        {
            return FPMaxNumFpscrImpl(value1, value2, false);
        }

        private static float FPMaxNumFpscrImpl(float value1, float value2, bool standardFpscr)
        {
            ExecutionContext context = NativeInterface.GetContext();
            FPCR fpcr = standardFpscr ? context.StandardFpcrValue : context.Fpcr;

            value1.FPUnpack(out FPType type1, out _, out _, context, fpcr);
            value2.FPUnpack(out FPType type2, out _, out _, context, fpcr);

            if (type1 == FPType.QNaN && type2 != FPType.QNaN)
            {
                value1 = FPInfinity(true);
            }
            else if (type1 != FPType.QNaN && type2 == FPType.QNaN)
            {
                value2 = FPInfinity(true);
            }

            return FPMaxFpscrImpl(value1, value2, standardFpscr);
        }

        [UnmanagedCallersOnly]
        public static float FPMaxNumFpscr(float value1, float value2, byte standardFpscr)
        {
            return FPMaxNumFpscrImpl(value1, value2, standardFpscr == 1);
        }

        [UnmanagedCallersOnly]
        public static float FPMin(float value1, float value2)
        {
            return FPMinFpscrImpl(value1, value2, false);
        }

        [UnmanagedCallersOnly]
        public static float FPMinFpscr(float value1, float value2, byte standardFpscr)
        {
            return FPMinFpscrImpl(value1, value2, standardFpscr == 1);
        }

        private static float FPMinFpscrImpl(float value1, float value2, bool standardFpscr)
        {
            ExecutionContext context = NativeInterface.GetContext();
            FPCR fpcr = standardFpscr ? context.StandardFpcrValue : context.Fpcr;

            value1 = value1.FPUnpack(out FPType type1, out bool sign1, out uint op1, context, fpcr);
            value2 = value2.FPUnpack(out FPType type2, out bool sign2, out uint op2, context, fpcr);

            float result = FPProcessNaNs(type1, type2, op1, op2, out bool done, context, fpcr);

            if (!done)
            {
                if (value1 < value2)
                {
                    if (type1 == FPType.Infinity)
                    {
                        result = FPInfinity(sign1);
                    }
                    else if (type1 == FPType.Zero)
                    {
                        result = FPZero(sign1 || sign2);
                    }
                    else
                    {
                        result = value1;

                        if ((fpcr & FPCR.Fz) != 0 && float.IsSubnormal(result))
                        {
                            context.Fpsr |= FPSR.Ufc;

                            result = FPZero(result < 0f);
                        }
                    }
                }
                else
                {
                    if (type2 == FPType.Infinity)
                    {
                        result = FPInfinity(sign2);
                    }
                    else if (type2 == FPType.Zero)
                    {
                        result = FPZero(sign1 || sign2);
                    }
                    else
                    {
                        result = value2;

                        if ((fpcr & FPCR.Fz) != 0 && float.IsSubnormal(result))
                        {
                            context.Fpsr |= FPSR.Ufc;

                            result = FPZero(result < 0f);
                        }
                    }
                }
            }

            return result;
        }

        [UnmanagedCallersOnly]
        public static float FPMinNum(float value1, float value2)
        {
            return FPMinNumFpscrImpl(value1, value2, false);
        }

        [UnmanagedCallersOnly]
        public static float FPMinNumFpscr(float value1, float value2, byte standardFpscr)
        {
            return FPMinNumFpscrImpl(value1, value2, standardFpscr == 1);
        }

        private static float FPMinNumFpscrImpl(float value1, float value2, bool standardFpscr)
        {
            ExecutionContext context = NativeInterface.GetContext();
            FPCR fpcr = standardFpscr ? context.StandardFpcrValue : context.Fpcr;

            value1.FPUnpack(out FPType type1, out _, out _, context, fpcr);
            value2.FPUnpack(out FPType type2, out _, out _, context, fpcr);

            if (type1 == FPType.QNaN && type2 != FPType.QNaN)
            {
                value1 = FPInfinity(false);
            }
            else if (type1 != FPType.QNaN && type2 == FPType.QNaN)
            {
                value2 = FPInfinity(false);
            }

            return FPMinFpscrImpl(value1, value2, standardFpscr);
        }

        [UnmanagedCallersOnly]
        public static float FPMul(float value1, float value2)
        {
            return FPMulFpscrImpl(value1, value2, false);
        }

        [UnmanagedCallersOnly]
        public static float FPMulFpscr(float value1, float value2, byte standardFpscr)
        {
            return FPMulFpscrImpl(value1, value2, standardFpscr == 1);
        }

        private static float FPMulFpscrImpl(float value1, float value2, bool standardFpscr)
        {
            ExecutionContext context = NativeInterface.GetContext();
            FPCR fpcr = standardFpscr ? context.StandardFpcrValue : context.Fpcr;

            value1 = value1.FPUnpack(out FPType type1, out bool sign1, out uint op1, context, fpcr);
            value2 = value2.FPUnpack(out FPType type2, out bool sign2, out uint op2, context, fpcr);

            float result = FPProcessNaNs(type1, type2, op1, op2, out bool done, context, fpcr);

            if (!done)
            {
                bool inf1 = type1 == FPType.Infinity;
                bool zero1 = type1 == FPType.Zero;
                bool inf2 = type2 == FPType.Infinity;
                bool zero2 = type2 == FPType.Zero;

                if ((inf1 && zero2) || (zero1 && inf2))
                {
                    result = FPDefaultNaN();

                    SoftFloat.FPProcessException(FPException.InvalidOp, context, fpcr);
                }
                else if (inf1 || inf2)
                {
                    result = FPInfinity(sign1 ^ sign2);
                }
                else if (zero1 || zero2)
                {
                    result = FPZero(sign1 ^ sign2);
                }
                else
                {
                    result = value1 * value2;

                    if ((fpcr & FPCR.Fz) != 0 && float.IsSubnormal(result))
                    {
                        context.Fpsr |= FPSR.Ufc;

                        result = FPZero(result < 0f);
                    }
                }
            }

            return result;
        }

        [UnmanagedCallersOnly]
        public static float FPMulAdd(float valueA, float value1, float value2)
        {
            return FPMulAddFpscrImpl(valueA, value1, value2, false);
        }

        [UnmanagedCallersOnly]
        public static float FPMulAddFpscr(float valueA, float value1, float value2, byte standardFpscr)
        {
            return FPMulAddFpscrImpl(valueA, value1, value2, standardFpscr == 1);
        }

        private static float FPMulAddFpscrImpl(float valueA, float value1, float value2, bool standardFpscr)
        {
            ExecutionContext context = NativeInterface.GetContext();
            FPCR fpcr = standardFpscr ? context.StandardFpcrValue : context.Fpcr;

            valueA = valueA.FPUnpack(out FPType typeA, out bool signA, out uint addend, context, fpcr);
            value1 = value1.FPUnpack(out FPType type1, out bool sign1, out uint op1, context, fpcr);
            value2 = value2.FPUnpack(out FPType type2, out bool sign2, out uint op2, context, fpcr);

            bool inf1 = type1 == FPType.Infinity;
            bool zero1 = type1 == FPType.Zero;
            bool inf2 = type2 == FPType.Infinity;
            bool zero2 = type2 == FPType.Zero;

            float result = FPProcessNaNs3(typeA, type1, type2, addend, op1, op2, out bool done, context, fpcr);

            if (typeA == FPType.QNaN && ((inf1 && zero2) || (zero1 && inf2)))
            {
                result = FPDefaultNaN();

                SoftFloat.FPProcessException(FPException.InvalidOp, context, fpcr);
            }

            if (!done)
            {
                bool infA = typeA == FPType.Infinity;
                bool zeroA = typeA == FPType.Zero;

                bool signP = sign1 ^ sign2;
                bool infP = inf1 || inf2;
                bool zeroP = zero1 || zero2;

                if ((inf1 && zero2) || (zero1 && inf2) || (infA && infP && signA != signP))
                {
                    result = FPDefaultNaN();

                    SoftFloat.FPProcessException(FPException.InvalidOp, context, fpcr);
                }
                else if ((infA && !signA) || (infP && !signP))
                {
                    result = FPInfinity(false);
                }
                else if ((infA && signA) || (infP && signP))
                {
                    result = FPInfinity(true);
                }
                else if (zeroA && zeroP && signA == signP)
                {
                    result = FPZero(signA);
                }
                else
                {
                    result = MathF.FusedMultiplyAdd(value1, value2, valueA);

                    if ((fpcr & FPCR.Fz) != 0 && float.IsSubnormal(result))
                    {
                        context.Fpsr |= FPSR.Ufc;

                        result = FPZero(result < 0f);
                    }
                }
            }

            return result;
        }

        [UnmanagedCallersOnly]
        public static float FPMulSub(float valueA, float value1, float value2)
        {
            value1 = value1.FPNeg();

            return FPMulAddFpscrImpl(valueA, value1, value2, false);
        }

        [UnmanagedCallersOnly]
        public static float FPMulSubFpscr(float valueA, float value1, float value2, byte standardFpscr)
        {
            value1 = value1.FPNeg();

            return FPMulAddFpscrImpl(valueA, value1, value2, standardFpscr == 1);
        }

        [UnmanagedCallersOnly]
        public static float FPMulX(float value1, float value2)
        {
            ExecutionContext context = NativeInterface.GetContext();
            FPCR fpcr = context.Fpcr;

            value1 = value1.FPUnpack(out FPType type1, out bool sign1, out uint op1, context, fpcr);
            value2 = value2.FPUnpack(out FPType type2, out bool sign2, out uint op2, context, fpcr);

            float result = FPProcessNaNs(type1, type2, op1, op2, out bool done, context, fpcr);

            if (!done)
            {
                bool inf1 = type1 == FPType.Infinity;
                bool zero1 = type1 == FPType.Zero;
                bool inf2 = type2 == FPType.Infinity;
                bool zero2 = type2 == FPType.Zero;

                if ((inf1 && zero2) || (zero1 && inf2))
                {
                    result = FPTwo(sign1 ^ sign2);
                }
                else if (inf1 || inf2)
                {
                    result = FPInfinity(sign1 ^ sign2);
                }
                else if (zero1 || zero2)
                {
                    result = FPZero(sign1 ^ sign2);
                }
                else
                {
                    result = value1 * value2;

                    if ((fpcr & FPCR.Fz) != 0 && float.IsSubnormal(result))
                    {
                        context.Fpsr |= FPSR.Ufc;

                        result = FPZero(result < 0f);
                    }
                }
            }

            return result;
        }

        [UnmanagedCallersOnly]
        public static float FPNegMulAdd(float valueA, float value1, float value2)
        {
            valueA = valueA.FPNeg();
            value1 = value1.FPNeg();

            return FPMulAddFpscrImpl(valueA, value1, value2, false);
        }

        [UnmanagedCallersOnly]
        public static float FPNegMulSub(float valueA, float value1, float value2)
        {
            valueA = valueA.FPNeg();

            return FPMulAddFpscrImpl(valueA, value1, value2, false);
        }

        [UnmanagedCallersOnly]
        public static float FPRecipEstimate(float value)
        {
            return FPRecipEstimateFpscrImpl(value, false);
        }

        [UnmanagedCallersOnly]
        public static float FPRecipEstimateFpscr(float value, byte standardFpscr)
        {
            return FPRecipEstimateFpscrImpl(value, standardFpscr == 1);
        }

        private static float FPRecipEstimateFpscrImpl(float value, bool standardFpscr)
        {
            ExecutionContext context = NativeInterface.GetContext();
            FPCR fpcr = standardFpscr ? context.StandardFpcrValue : context.Fpcr;

            value.FPUnpack(out FPType type, out bool sign, out uint op, context, fpcr);

            float result;

            if (type == FPType.SNaN || type == FPType.QNaN)
            {
                result = FPProcessNaN(type, op, context, fpcr);
            }
            else if (type == FPType.Infinity)
            {
                result = FPZero(sign);
            }
            else if (type == FPType.Zero)
            {
                result = FPInfinity(sign);

                SoftFloat.FPProcessException(FPException.DivideByZero, context, fpcr);
            }
            else if (MathF.Abs(value) < MathF.Pow(2f, -128))
            {
                var overflowToInf = fpcr.GetRoundingMode() switch
                {
                    FPRoundingMode.ToNearest => true,
                    FPRoundingMode.TowardsPlusInfinity => !sign,
                    FPRoundingMode.TowardsMinusInfinity => sign,
                    FPRoundingMode.TowardsZero => false,
                    _ => throw new ArgumentException($"Invalid rounding mode \"{fpcr.GetRoundingMode()}\"."),
                };
                result = overflowToInf ? FPInfinity(sign) : FPMaxNormal(sign);

                SoftFloat.FPProcessException(FPException.Overflow, context, fpcr);
                SoftFloat.FPProcessException(FPException.Inexact, context, fpcr);
            }
            else if ((fpcr & FPCR.Fz) != 0 && (MathF.Abs(value) >= MathF.Pow(2f, 126)))
            {
                result = FPZero(sign);

                context.Fpsr |= FPSR.Ufc;
            }
            else
            {
                ulong fraction = (ulong)(op & 0x007FFFFFu) << 29;
                uint exp = (op & 0x7F800000u) >> 23;

                if (exp == 0u)
                {
                    if ((fraction & 0x0008000000000000ul) == 0ul)
                    {
                        fraction = (fraction & 0x0003FFFFFFFFFFFFul) << 2;
                        exp -= 1u;
                    }
                    else
                    {
                        fraction = (fraction & 0x0007FFFFFFFFFFFFul) << 1;
                    }
                }

                uint scaled = (uint)(((fraction & 0x000FF00000000000ul) | 0x0010000000000000ul) >> 44);

                uint resultExp = 253u - exp;

                uint estimate = (uint)SoftFloat.RecipEstimateTable[scaled - 256u] + 256u;

                fraction = (ulong)(estimate & 0xFFu) << 44;

                if (resultExp == 0u)
                {
                    fraction = ((fraction & 0x000FFFFFFFFFFFFEul) | 0x0010000000000000ul) >> 1;
                }
                else if (resultExp + 1u == 0u)
                {
                    fraction = ((fraction & 0x000FFFFFFFFFFFFCul) | 0x0010000000000000ul) >> 2;
                    resultExp = 0u;
                }

                result = BitConverter.Int32BitsToSingle(
                    (int)((sign ? 1u : 0u) << 31 | (resultExp & 0xFFu) << 23 | (uint)(fraction >> 29) & 0x007FFFFFu));
            }

            return result;
        }

        [UnmanagedCallersOnly]
        public static float FPRecipStep(float value1, float value2)
        {
            ExecutionContext context = NativeInterface.GetContext();
            FPCR fpcr = context.StandardFpcrValue;

            value1 = value1.FPUnpack(out FPType type1, out _, out uint op1, context, fpcr);
            value2 = value2.FPUnpack(out FPType type2, out _, out uint op2, context, fpcr);

            float result = FPProcessNaNs(type1, type2, op1, op2, out bool done, context, fpcr);

            if (!done)
            {
                bool inf1 = type1 == FPType.Infinity;
                bool zero1 = type1 == FPType.Zero;
                bool inf2 = type2 == FPType.Infinity;
                bool zero2 = type2 == FPType.Zero;

                float product;

                if ((inf1 && zero2) || (zero1 && inf2))
                {
                    product = FPZero(false);
                }
                else
                {
                    product = FPMulFpscrImpl(value1, value2, true);
                }

                result = FPSubFpscrImpl(FPTwo(false), product, true);
            }

            return result;
        }

        [UnmanagedCallersOnly]
        public static float FPRecipStepFused(float value1, float value2)
        {
            ExecutionContext context = NativeInterface.GetContext();
            FPCR fpcr = context.Fpcr;

            value1 = value1.FPNeg();

            value1 = value1.FPUnpack(out FPType type1, out bool sign1, out uint op1, context, fpcr);
            value2 = value2.FPUnpack(out FPType type2, out bool sign2, out uint op2, context, fpcr);

            float result = FPProcessNaNs(type1, type2, op1, op2, out bool done, context, fpcr);

            if (!done)
            {
                bool inf1 = type1 == FPType.Infinity;
                bool zero1 = type1 == FPType.Zero;
                bool inf2 = type2 == FPType.Infinity;
                bool zero2 = type2 == FPType.Zero;

                if ((inf1 && zero2) || (zero1 && inf2))
                {
                    result = FPTwo(false);
                }
                else if (inf1 || inf2)
                {
                    result = FPInfinity(sign1 ^ sign2);
                }
                else
                {
                    result = MathF.FusedMultiplyAdd(value1, value2, 2f);

                    if ((fpcr & FPCR.Fz) != 0 && float.IsSubnormal(result))
                    {
                        context.Fpsr |= FPSR.Ufc;

                        result = FPZero(result < 0f);
                    }
                }
            }

            return result;
        }

        [UnmanagedCallersOnly]
        public static float FPRecpX(float value)
        {
            ExecutionContext context = NativeInterface.GetContext();
            FPCR fpcr = context.Fpcr;

            value.FPUnpack(out FPType type, out bool sign, out uint op, context, fpcr);

            float result;

            if (type == FPType.SNaN || type == FPType.QNaN)
            {
                result = FPProcessNaN(type, op, context, fpcr);
            }
            else
            {
                uint notExp = (~op >> 23) & 0xFFu;
                uint maxExp = 0xFEu;

                result = BitConverter.Int32BitsToSingle(
                    (int)((sign ? 1u : 0u) << 31 | (notExp == 0xFFu ? maxExp : notExp) << 23));
            }

            return result;
        }

        [UnmanagedCallersOnly]
        public static float FPRSqrtEstimate(float value)
        {
            return FPRSqrtEstimateFpscrImpl(value, false);
        }

        [UnmanagedCallersOnly]
        public static float FPRSqrtEstimateFpscr(float value, byte standardFpscr)
        {
            return FPRSqrtEstimateFpscrImpl(value, standardFpscr == 1);
        }

        private static float FPRSqrtEstimateFpscrImpl(float value, bool standardFpscr)
        {
            ExecutionContext context = NativeInterface.GetContext();
            FPCR fpcr = standardFpscr ? context.StandardFpcrValue : context.Fpcr;

            value.FPUnpack(out FPType type, out bool sign, out uint op, context, fpcr);

            float result;

            if (type == FPType.SNaN || type == FPType.QNaN)
            {
                result = FPProcessNaN(type, op, context, fpcr);
            }
            else if (type == FPType.Zero)
            {
                result = FPInfinity(sign);

                SoftFloat.FPProcessException(FPException.DivideByZero, context, fpcr);
            }
            else if (sign)
            {
                result = FPDefaultNaN();

                SoftFloat.FPProcessException(FPException.InvalidOp, context, fpcr);
            }
            else if (type == FPType.Infinity)
            {
                result = FPZero(false);
            }
            else
            {
                ulong fraction = (ulong)(op & 0x007FFFFFu) << 29;
                uint exp = (op & 0x7F800000u) >> 23;

                if (exp == 0u)
                {
                    while ((fraction & 0x0008000000000000ul) == 0ul)
                    {
                        fraction = (fraction & 0x0007FFFFFFFFFFFFul) << 1;
                        exp -= 1u;
                    }

                    fraction = (fraction & 0x0007FFFFFFFFFFFFul) << 1;
                }

                uint scaled;

                if ((exp & 1u) == 0u)
                {
                    scaled = (uint)(((fraction & 0x000FF00000000000ul) | 0x0010000000000000ul) >> 44);
                }
                else
                {
                    scaled = (uint)(((fraction & 0x000FE00000000000ul) | 0x0010000000000000ul) >> 45);
                }

                uint resultExp = (380u - exp) >> 1;

                uint estimate = (uint)SoftFloat.RecipSqrtEstimateTable[scaled - 128u] + 256u;

                result = BitConverter.Int32BitsToSingle((int)((resultExp & 0xFFu) << 23 | (estimate & 0xFFu) << 15));
            }

            return result;
        }

        public static float FPHalvedSub(float value1, float value2, ExecutionContext context, FPCR fpcr)
        {
            value1 = value1.FPUnpack(out FPType type1, out bool sign1, out uint op1, context, fpcr);
            value2 = value2.FPUnpack(out FPType type2, out bool sign2, out uint op2, context, fpcr);

            float result = FPProcessNaNs(type1, type2, op1, op2, out bool done, context, fpcr);

            if (!done)
            {
                bool inf1 = type1 == FPType.Infinity;
                bool zero1 = type1 == FPType.Zero;
                bool inf2 = type2 == FPType.Infinity;
                bool zero2 = type2 == FPType.Zero;

                if (inf1 && inf2 && sign1 == sign2)
                {
                    result = FPDefaultNaN();

                    SoftFloat.FPProcessException(FPException.InvalidOp, context, fpcr);
                }
                else if ((inf1 && !sign1) || (inf2 && sign2))
                {
                    result = FPInfinity(false);
                }
                else if ((inf1 && sign1) || (inf2 && !sign2))
                {
                    result = FPInfinity(true);
                }
                else if (zero1 && zero2 && sign1 == !sign2)
                {
                    result = FPZero(sign1);
                }
                else
                {
                    result = (value1 - value2) / 2.0f;

                    if ((fpcr & FPCR.Fz) != 0 && float.IsSubnormal(result))
                    {
                        context.Fpsr |= FPSR.Ufc;

                        result = FPZero(result < 0f);
                    }
                }
            }

            return result;
        }

        [UnmanagedCallersOnly]
        public static float FPRSqrtStep(float value1, float value2)
        {
            ExecutionContext context = NativeInterface.GetContext();
            FPCR fpcr = context.StandardFpcrValue;

            value1 = value1.FPUnpack(out FPType type1, out _, out uint op1, context, fpcr);
            value2 = value2.FPUnpack(out FPType type2, out _, out uint op2, context, fpcr);

            float result = FPProcessNaNs(type1, type2, op1, op2, out bool done, context, fpcr);

            if (!done)
            {
                bool inf1 = type1 == FPType.Infinity;
                bool zero1 = type1 == FPType.Zero;
                bool inf2 = type2 == FPType.Infinity;
                bool zero2 = type2 == FPType.Zero;

                float product;

                if ((inf1 && zero2) || (zero1 && inf2))
                {
                    product = FPZero(false);
                }
                else
                {
                    product = FPMulFpscrImpl(value1, value2, true);
                }

                result = FPHalvedSub(FPThree(false), product, context, fpcr);
            }

            return result;
        }

        [UnmanagedCallersOnly]
        public static float FPRSqrtStepFused(float value1, float value2)
        {
            ExecutionContext context = NativeInterface.GetContext();
            FPCR fpcr = context.Fpcr;

            value1 = value1.FPNeg();

            value1 = value1.FPUnpack(out FPType type1, out bool sign1, out uint op1, context, fpcr);
            value2 = value2.FPUnpack(out FPType type2, out bool sign2, out uint op2, context, fpcr);

            float result = FPProcessNaNs(type1, type2, op1, op2, out bool done, context, fpcr);

            if (!done)
            {
                bool inf1 = type1 == FPType.Infinity;
                bool zero1 = type1 == FPType.Zero;
                bool inf2 = type2 == FPType.Infinity;
                bool zero2 = type2 == FPType.Zero;

                if ((inf1 && zero2) || (zero1 && inf2))
                {
                    result = FPOnePointFive(false);
                }
                else if (inf1 || inf2)
                {
                    result = FPInfinity(sign1 ^ sign2);
                }
                else
                {
                    result = MathF.FusedMultiplyAdd(value1, value2, 3f) / 2f;

                    if ((fpcr & FPCR.Fz) != 0 && float.IsSubnormal(result))
                    {
                        context.Fpsr |= FPSR.Ufc;

                        result = FPZero(result < 0f);
                    }
                }
            }

            return result;
        }

        [UnmanagedCallersOnly]
        public static float FPSqrt(float value)
        {
            ExecutionContext context = NativeInterface.GetContext();
            FPCR fpcr = context.Fpcr;

            value = value.FPUnpack(out FPType type, out bool sign, out uint op, context, fpcr);

            float result;

            if (type == FPType.SNaN || type == FPType.QNaN)
            {
                result = FPProcessNaN(type, op, context, fpcr);
            }
            else if (type == FPType.Zero)
            {
                result = FPZero(sign);
            }
            else if (type == FPType.Infinity && !sign)
            {
                result = FPInfinity(sign);
            }
            else if (sign)
            {
                result = FPDefaultNaN();

                SoftFloat.FPProcessException(FPException.InvalidOp, context, fpcr);
            }
            else
            {
                result = MathF.Sqrt(value);

                if ((fpcr & FPCR.Fz) != 0 && float.IsSubnormal(result))
                {
                    context.Fpsr |= FPSR.Ufc;

                    result = FPZero(result < 0f);
                }
            }

            return result;
        }

        [UnmanagedCallersOnly]
        public static float FPSub(float value1, float value2)
        {
            return FPSubFpscrImpl(value1, value2, false);
        }

        private static float FPSubFpscrImpl(float value1, float value2, bool standardFpscr)
        {
            ExecutionContext context = NativeInterface.GetContext();
            FPCR fpcr = standardFpscr ? context.StandardFpcrValue : context.Fpcr;

            value1 = value1.FPUnpack(out FPType type1, out bool sign1, out uint op1, context, fpcr);
            value2 = value2.FPUnpack(out FPType type2, out bool sign2, out uint op2, context, fpcr);

            float result = FPProcessNaNs(type1, type2, op1, op2, out bool done, context, fpcr);

            if (!done)
            {
                bool inf1 = type1 == FPType.Infinity;
                bool zero1 = type1 == FPType.Zero;
                bool inf2 = type2 == FPType.Infinity;
                bool zero2 = type2 == FPType.Zero;

                if (inf1 && inf2 && sign1 == sign2)
                {
                    result = FPDefaultNaN();

                    SoftFloat.FPProcessException(FPException.InvalidOp, context, fpcr);
                }
                else if ((inf1 && !sign1) || (inf2 && sign2))
                {
                    result = FPInfinity(false);
                }
                else if ((inf1 && sign1) || (inf2 && !sign2))
                {
                    result = FPInfinity(true);
                }
                else if (zero1 && zero2 && sign1 == !sign2)
                {
                    result = FPZero(sign1);
                }
                else
                {
                    result = value1 - value2;

                    if ((fpcr & FPCR.Fz) != 0 && float.IsSubnormal(result))
                    {
                        context.Fpsr |= FPSR.Ufc;

                        result = FPZero(result < 0f);
                    }
                }
            }

            return result;
        }

        public static float FPDefaultNaN()
        {
            return BitConverter.Int32BitsToSingle(0x7fc00000);
        }

        public static float FPInfinity(bool sign)
        {
            return sign ? float.NegativeInfinity : float.PositiveInfinity;
        }

        public static float FPZero(bool sign)
        {
            return sign ? -0f : +0f;
        }

        public static float FPMaxNormal(bool sign)
        {
            return sign ? float.MinValue : float.MaxValue;
        }

        private static float FPTwo(bool sign)
        {
            return sign ? -2f : +2f;
        }

        private static float FPThree(bool sign)
        {
            return sign ? -3f : +3f;
        }

        private static float FPOnePointFive(bool sign)
        {
            return sign ? -1.5f : +1.5f;
        }

        private static float FPNeg(this float value)
        {
            return -value;
        }

        private static float ZerosOrOnes(bool ones)
        {
            return BitConverter.Int32BitsToSingle(ones ? -1 : 0);
        }

        private static float FPUnpack(
            this float value,
            out FPType type,
            out bool sign,
            out uint valueBits,
            ExecutionContext context,
            FPCR fpcr)
        {
            valueBits = (uint)BitConverter.SingleToInt32Bits(value);

            sign = (~valueBits & 0x80000000u) == 0u;

            if ((valueBits & 0x7F800000u) == 0u)
            {
                if ((valueBits & 0x007FFFFFu) == 0u || (fpcr & FPCR.Fz) != 0)
                {
                    type = FPType.Zero;
                    value = FPZero(sign);

                    if ((valueBits & 0x007FFFFFu) != 0u)
                    {
                        SoftFloat.FPProcessException(FPException.InputDenorm, context, fpcr);
                    }
                }
                else
                {
                    type = FPType.Nonzero;
                }
            }
            else if ((~valueBits & 0x7F800000u) == 0u)
            {
                if ((valueBits & 0x007FFFFFu) == 0u)
                {
                    type = FPType.Infinity;
                }
                else
                {
                    type = (~valueBits & 0x00400000u) == 0u ? FPType.QNaN : FPType.SNaN;
                    value = FPZero(sign);
                }
            }
            else
            {
                type = FPType.Nonzero;
            }

            return value;
        }

        private static float FPProcessNaNs(
            FPType type1,
            FPType type2,
            uint op1,
            uint op2,
            out bool done,
            ExecutionContext context,
            FPCR fpcr)
        {
            done = true;

            if (type1 == FPType.SNaN)
            {
                return FPProcessNaN(type1, op1, context, fpcr);
            }
            else if (type2 == FPType.SNaN)
            {
                return FPProcessNaN(type2, op2, context, fpcr);
            }
            else if (type1 == FPType.QNaN)
            {
                return FPProcessNaN(type1, op1, context, fpcr);
            }
            else if (type2 == FPType.QNaN)
            {
                return FPProcessNaN(type2, op2, context, fpcr);
            }

            done = false;

            return FPZero(false);
        }

        private static float FPProcessNaNs3(
            FPType type1,
            FPType type2,
            FPType type3,
            uint op1,
            uint op2,
            uint op3,
            out bool done,
            ExecutionContext context,
            FPCR fpcr)
        {
            done = true;

            if (type1 == FPType.SNaN)
            {
                return FPProcessNaN(type1, op1, context, fpcr);
            }
            else if (type2 == FPType.SNaN)
            {
                return FPProcessNaN(type2, op2, context, fpcr);
            }
            else if (type3 == FPType.SNaN)
            {
                return FPProcessNaN(type3, op3, context, fpcr);
            }
            else if (type1 == FPType.QNaN)
            {
                return FPProcessNaN(type1, op1, context, fpcr);
            }
            else if (type2 == FPType.QNaN)
            {
                return FPProcessNaN(type2, op2, context, fpcr);
            }
            else if (type3 == FPType.QNaN)
            {
                return FPProcessNaN(type3, op3, context, fpcr);
            }

            done = false;

            return FPZero(false);
        }

        private static float FPProcessNaN(FPType type, uint op, ExecutionContext context, FPCR fpcr)
        {
            if (type == FPType.SNaN)
            {
                op |= 1u << 22;

                SoftFloat.FPProcessException(FPException.InvalidOp, context, fpcr);
            }

            if ((fpcr & FPCR.Dn) != 0)
            {
                return FPDefaultNaN();
            }

            return BitConverter.Int32BitsToSingle((int)op);
        }
    }

    static class SoftFloat64_16
    {
        [UnmanagedCallersOnly]
        public static ushort FPConvert(double value)
        {
            ExecutionContext context = NativeInterface.GetContext();

            double real = value.FPUnpackCv(out FPType type, out bool sign, out ulong valueBits, context);

            bool altHp = (context.Fpcr & FPCR.Ahp) != 0;

            ushort resultBits;

            if (type == FPType.SNaN || type == FPType.QNaN)
            {
                if (altHp)
                {
                    resultBits = SoftFloat16.FPZero(sign);
                }
                else if ((context.Fpcr & FPCR.Dn) != 0)
                {
                    resultBits = SoftFloat16.FPDefaultNaN();
                }
                else
                {
                    resultBits = FPConvertNaN(valueBits);
                }

                if (type == FPType.SNaN || altHp)
                {
                    SoftFloat.FPProcessException(FPException.InvalidOp, context);
                }
            }
            else if (type == FPType.Infinity)
            {
                if (altHp)
                {
                    resultBits = (ushort)((sign ? 1u : 0u) << 15 | 0x7FFFu);

                    SoftFloat.FPProcessException(FPException.InvalidOp, context);
                }
                else
                {
                    resultBits = SoftFloat16.FPInfinity(sign);
                }
            }
            else if (type == FPType.Zero)
            {
                resultBits = SoftFloat16.FPZero(sign);
            }
            else
            {
                resultBits = SoftFloat16.FPRoundCv(real, context);
            }

            return resultBits;
        }

        private static double FPUnpackCv(
            this double value,
            out FPType type,
            out bool sign,
            out ulong valueBits,
            ExecutionContext context)
        {
            valueBits = (ulong)BitConverter.DoubleToInt64Bits(value);

            sign = (~valueBits & 0x8000000000000000ul) == 0u;

            ulong exp64 = (valueBits & 0x7FF0000000000000ul) >> 52;
            ulong frac64 = valueBits & 0x000FFFFFFFFFFFFFul;

            double real;

            if (exp64 == 0u)
            {
                if (frac64 == 0u || (context.Fpcr & FPCR.Fz) != 0)
                {
                    type = FPType.Zero;
                    real = 0d;

                    if (frac64 != 0u)
                    {
                        SoftFloat.FPProcessException(FPException.InputDenorm, context);
                    }
                }
                else
                {
                    type = FPType.Nonzero; // Subnormal.
                    real = Math.Pow(2d, -1022) * ((double)frac64 * Math.Pow(2d, -52));
                }
            }
            else if (exp64 == 0x7FFul)
            {
                if (frac64 == 0u)
                {
                    type = FPType.Infinity;
                    real = Math.Pow(2d, 1000000);
                }
                else
                {
                    type = (~frac64 & 0x0008000000000000ul) == 0u ? FPType.QNaN : FPType.SNaN;
                    real = 0d;
                }
            }
            else
            {
                type = FPType.Nonzero; // Normal.
                real = Math.Pow(2d, (int)exp64 - 1023) * (1d + (double)frac64 * Math.Pow(2d, -52));
            }

            return sign ? -real : real;
        }

        private static ushort FPConvertNaN(ulong valueBits)
        {
            return (ushort)((valueBits & 0x8000000000000000ul) >> 48 | 0x7E00u | (valueBits & 0x0007FC0000000000ul) >> 42);
        }
    }

    static class SoftFloat64
    {
        [UnmanagedCallersOnly]
        public static double FPAdd(double value1, double value2)
        {
            return FPAddFpscrImpl(value1, value2, false);
        }

        [UnmanagedCallersOnly]
        public static double FPAddFpscr(double value1, double value2, byte standardFpscr)
        {
            return FPAddFpscrImpl(value1, value2, standardFpscr == 1);
        }

        private static double FPAddFpscrImpl(double value1, double value2, bool standardFpscr)
        {
            ExecutionContext context = NativeInterface.GetContext();
            FPCR fpcr = standardFpscr ? context.StandardFpcrValue : context.Fpcr;

            value1 = value1.FPUnpack(out FPType type1, out bool sign1, out ulong op1, context, fpcr);
            value2 = value2.FPUnpack(out FPType type2, out bool sign2, out ulong op2, context, fpcr);

            double result = FPProcessNaNs(type1, type2, op1, op2, out bool done, context, fpcr);

            if (!done)
            {
                bool inf1 = type1 == FPType.Infinity;
                bool zero1 = type1 == FPType.Zero;
                bool inf2 = type2 == FPType.Infinity;
                bool zero2 = type2 == FPType.Zero;

                if (inf1 && inf2 && sign1 == !sign2)
                {
                    result = FPDefaultNaN();

                    SoftFloat.FPProcessException(FPException.InvalidOp, context, fpcr);
                }
                else if ((inf1 && !sign1) || (inf2 && !sign2))
                {
                    result = FPInfinity(false);
                }
                else if ((inf1 && sign1) || (inf2 && sign2))
                {
                    result = FPInfinity(true);
                }
                else if (zero1 && zero2 && sign1 == sign2)
                {
                    result = FPZero(sign1);
                }
                else
                {
                    result = value1 + value2;

                    if ((fpcr & FPCR.Fz) != 0 && double.IsSubnormal(result))
                    {
                        context.Fpsr |= FPSR.Ufc;

                        result = FPZero(result < 0d);
                    }
                }
            }

            return result;
        }

        [UnmanagedCallersOnly]
        public static int FPCompare(double value1, double value2, byte signalNaNs)
        {
            ExecutionContext context = NativeInterface.GetContext();
            FPCR fpcr = context.Fpcr;

            value1 = value1.FPUnpack(out FPType type1, out _, out _, context, fpcr);
            value2 = value2.FPUnpack(out FPType type2, out _, out _, context, fpcr);

            int result;

            if (type1 == FPType.SNaN || type1 == FPType.QNaN || type2 == FPType.SNaN || type2 == FPType.QNaN)
            {
                result = 0b0011;

                if (type1 == FPType.SNaN || type2 == FPType.SNaN || signalNaNs == 1)
                {
                    SoftFloat.FPProcessException(FPException.InvalidOp, context, fpcr);
                }
            }
            else
            {
                if (value1 == value2)
                {
                    result = 0b0110;
                }
                else if (value1 < value2)
                {
                    result = 0b1000;
                }
                else
                {
                    result = 0b0010;
                }
            }

            return result;
        }

        [UnmanagedCallersOnly]
        public static double FPCompareEQ(double value1, double value2)
        {
            return FPCompareEQFpscrImpl(value1, value2, false);
        }

        [UnmanagedCallersOnly]
        public static double FPCompareEQFpscr(double value1, double value2, byte standardFpscr)
        {
            return FPCompareEQFpscrImpl(value1, value2, standardFpscr == 1);
        }

        private static double FPCompareEQFpscrImpl(double value1, double value2, bool standardFpscr)
        {
            ExecutionContext context = NativeInterface.GetContext();
            FPCR fpcr = standardFpscr ? context.StandardFpcrValue : context.Fpcr;

            value1 = value1.FPUnpack(out FPType type1, out _, out _, context, fpcr);
            value2 = value2.FPUnpack(out FPType type2, out _, out _, context, fpcr);

            double result;

            if (type1 == FPType.SNaN || type1 == FPType.QNaN || type2 == FPType.SNaN || type2 == FPType.QNaN)
            {
                result = ZerosOrOnes(false);

                if (type1 == FPType.SNaN || type2 == FPType.SNaN)
                {
                    SoftFloat.FPProcessException(FPException.InvalidOp, context, fpcr);
                }
            }
            else
            {
                result = ZerosOrOnes(value1 == value2);
            }

            return result;
        }

        [UnmanagedCallersOnly]
        public static double FPCompareGE(double value1, double value2)
        {
            return FPCompareGEFpscrImpl(value1, value2, false);
        }

        [UnmanagedCallersOnly]
        public static double FPCompareGEFpscr(double value1, double value2, byte standardFpscr)
        {
            return FPCompareGEFpscrImpl(value1, value2, standardFpscr == 1);
        }

        private static double FPCompareGEFpscrImpl(double value1, double value2, bool standardFpscr)
        {
            ExecutionContext context = NativeInterface.GetContext();
            FPCR fpcr = standardFpscr ? context.StandardFpcrValue : context.Fpcr;

            value1 = value1.FPUnpack(out FPType type1, out _, out _, context, fpcr);
            value2 = value2.FPUnpack(out FPType type2, out _, out _, context, fpcr);

            double result;

            if (type1 == FPType.SNaN || type1 == FPType.QNaN || type2 == FPType.SNaN || type2 == FPType.QNaN)
            {
                result = ZerosOrOnes(false);

                SoftFloat.FPProcessException(FPException.InvalidOp, context, fpcr);
            }
            else
            {
                result = ZerosOrOnes(value1 >= value2);
            }

            return result;
        }

        [UnmanagedCallersOnly]
        public static double FPCompareGT(double value1, double value2)
        {
            return FPCompareGTFpscrImpl(value1, value2, false);
        }

        [UnmanagedCallersOnly]
        public static double FPCompareGTFpscr(double value1, double value2, byte standardFpscr)
        {
            return FPCompareGTFpscrImpl(value1, value2, standardFpscr == 1);
        }

        private static double FPCompareGTFpscrImpl(double value1, double value2, bool standardFpscr)
        {
            ExecutionContext context = NativeInterface.GetContext();
            FPCR fpcr = standardFpscr ? context.StandardFpcrValue : context.Fpcr;

            value1 = value1.FPUnpack(out FPType type1, out _, out _, context, fpcr);
            value2 = value2.FPUnpack(out FPType type2, out _, out _, context, fpcr);

            double result;

            if (type1 == FPType.SNaN || type1 == FPType.QNaN || type2 == FPType.SNaN || type2 == FPType.QNaN)
            {
                result = ZerosOrOnes(false);

                SoftFloat.FPProcessException(FPException.InvalidOp, context, fpcr);
            }
            else
            {
                result = ZerosOrOnes(value1 > value2);
            }

            return result;
        }

        [UnmanagedCallersOnly]
        public static double FPCompareLE(double value1, double value2)
        {
            return FPCompareGEFpscrImpl(value2, value1, false);
        }

        [UnmanagedCallersOnly]
        public static double FPCompareLT(double value1, double value2)
        {
            return FPCompareGTFpscrImpl(value2, value1, false);
        }

        [UnmanagedCallersOnly]
        public static double FPCompareLEFpscr(double value1, double value2, byte standardFpscr)
        {
            return FPCompareGEFpscrImpl(value2, value1, standardFpscr == 1);
        }

        [UnmanagedCallersOnly]
        public static double FPCompareLTFpscr(double value1, double value2, byte standardFpscr)
        {
            return FPCompareGTFpscrImpl(value2, value1, standardFpscr == 1);
        }

        [UnmanagedCallersOnly]
        public static double FPDiv(double value1, double value2)
        {
            ExecutionContext context = NativeInterface.GetContext();
            FPCR fpcr = context.Fpcr;

            value1 = value1.FPUnpack(out FPType type1, out bool sign1, out ulong op1, context, fpcr);
            value2 = value2.FPUnpack(out FPType type2, out bool sign2, out ulong op2, context, fpcr);

            double result = FPProcessNaNs(type1, type2, op1, op2, out bool done, context, fpcr);

            if (!done)
            {
                bool inf1 = type1 == FPType.Infinity;
                bool zero1 = type1 == FPType.Zero;
                bool inf2 = type2 == FPType.Infinity;
                bool zero2 = type2 == FPType.Zero;

                if ((inf1 && inf2) || (zero1 && zero2))
                {
                    result = FPDefaultNaN();

                    SoftFloat.FPProcessException(FPException.InvalidOp, context, fpcr);
                }
                else if (inf1 || zero2)
                {
                    result = FPInfinity(sign1 ^ sign2);

                    if (!inf1)
                    {
                        SoftFloat.FPProcessException(FPException.DivideByZero, context, fpcr);
                    }
                }
                else if (zero1 || inf2)
                {
                    result = FPZero(sign1 ^ sign2);
                }
                else
                {
                    result = value1 / value2;

                    if ((fpcr & FPCR.Fz) != 0 && double.IsSubnormal(result))
                    {
                        context.Fpsr |= FPSR.Ufc;

                        result = FPZero(result < 0d);
                    }
                }
            }

            return result;
        }

        [UnmanagedCallersOnly]
        public static double FPMax(double value1, double value2)
        {
            return FPMaxFpscrImpl(value1, value2, false);
        }

        [UnmanagedCallersOnly]
        public static double FPMaxFpscr(double value1, double value2, byte standardFpscr)
        {
            return FPMaxFpscrImpl(value1, value2, standardFpscr == 1);
        }

        private static double FPMaxFpscrImpl(double value1, double value2, bool standardFpscr)
        {
            ExecutionContext context = NativeInterface.GetContext();
            FPCR fpcr = standardFpscr ? context.StandardFpcrValue : context.Fpcr;

            value1 = value1.FPUnpack(out FPType type1, out bool sign1, out ulong op1, context, fpcr);
            value2 = value2.FPUnpack(out FPType type2, out bool sign2, out ulong op2, context, fpcr);

            double result = FPProcessNaNs(type1, type2, op1, op2, out bool done, context, fpcr);

            if (!done)
            {
                if (value1 > value2)
                {
                    if (type1 == FPType.Infinity)
                    {
                        result = FPInfinity(sign1);
                    }
                    else if (type1 == FPType.Zero)
                    {
                        result = FPZero(sign1 && sign2);
                    }
                    else
                    {
                        result = value1;

                        if ((fpcr & FPCR.Fz) != 0 && double.IsSubnormal(result))
                        {
                            context.Fpsr |= FPSR.Ufc;

                            result = FPZero(result < 0d);
                        }
                    }
                }
                else
                {
                    if (type2 == FPType.Infinity)
                    {
                        result = FPInfinity(sign2);
                    }
                    else if (type2 == FPType.Zero)
                    {
                        result = FPZero(sign1 && sign2);
                    }
                    else
                    {
                        result = value2;

                        if ((fpcr & FPCR.Fz) != 0 && double.IsSubnormal(result))
                        {
                            context.Fpsr |= FPSR.Ufc;

                            result = FPZero(result < 0d);
                        }
                    }
                }
            }

            return result;
        }

        [UnmanagedCallersOnly]
        public static double FPMaxNum(double value1, double value2)
        {
            return FPMaxNumFpscrImpl(value1, value2, false);
        }

        [UnmanagedCallersOnly]
        public static double FPMaxNumFpscr(double value1, double value2, byte standardFpscr)
        {
            return FPMaxNumFpscrImpl(value1, value2, standardFpscr == 1);
        }

        private static double FPMaxNumFpscrImpl(double value1, double value2, bool standardFpscr)
        {
            ExecutionContext context = NativeInterface.GetContext();
            FPCR fpcr = standardFpscr ? context.StandardFpcrValue : context.Fpcr;

            value1.FPUnpack(out FPType type1, out _, out _, context, fpcr);
            value2.FPUnpack(out FPType type2, out _, out _, context, fpcr);

            if (type1 == FPType.QNaN && type2 != FPType.QNaN)
            {
                value1 = FPInfinity(true);
            }
            else if (type1 != FPType.QNaN && type2 == FPType.QNaN)
            {
                value2 = FPInfinity(true);
            }

            return FPMaxFpscrImpl(value1, value2, standardFpscr);
        }

        [UnmanagedCallersOnly]
        public static double FPMin(double value1, double value2)
        {
            return FPMinFpscrImpl(value1, value2, false);
        }

        [UnmanagedCallersOnly]
        public static double FPMinFpscr(double value1, double value2, byte standardFpscr)
        {
            return FPMinFpscrImpl(value1, value2, standardFpscr == 1);
        }

        private static double FPMinFpscrImpl(double value1, double value2, bool standardFpscr)
        {
            ExecutionContext context = NativeInterface.GetContext();
            FPCR fpcr = standardFpscr ? context.StandardFpcrValue : context.Fpcr;

            value1 = value1.FPUnpack(out FPType type1, out bool sign1, out ulong op1, context, fpcr);
            value2 = value2.FPUnpack(out FPType type2, out bool sign2, out ulong op2, context, fpcr);

            double result = FPProcessNaNs(type1, type2, op1, op2, out bool done, context, fpcr);

            if (!done)
            {
                if (value1 < value2)
                {
                    if (type1 == FPType.Infinity)
                    {
                        result = FPInfinity(sign1);
                    }
                    else if (type1 == FPType.Zero)
                    {
                        result = FPZero(sign1 || sign2);
                    }
                    else
                    {
                        result = value1;

                        if ((fpcr & FPCR.Fz) != 0 && double.IsSubnormal(result))
                        {
                            context.Fpsr |= FPSR.Ufc;

                            result = FPZero(result < 0d);
                        }
                    }
                }
                else
                {
                    if (type2 == FPType.Infinity)
                    {
                        result = FPInfinity(sign2);
                    }
                    else if (type2 == FPType.Zero)
                    {
                        result = FPZero(sign1 || sign2);
                    }
                    else
                    {
                        result = value2;

                        if ((fpcr & FPCR.Fz) != 0 && double.IsSubnormal(result))
                        {
                            context.Fpsr |= FPSR.Ufc;

                            result = FPZero(result < 0d);
                        }
                    }
                }
            }

            return result;
        }

        [UnmanagedCallersOnly]
        public static double FPMinNum(double value1, double value2)
        {
            return FPMinNumFpscrImpl(value1, value2, false);
        }

        [UnmanagedCallersOnly]
        public static double FPMinNumFpscr(double value1, double value2, byte standardFpscr)
        {
            return FPMinNumFpscrImpl(value1, value2, standardFpscr == 1);
        }

        private static double FPMinNumFpscrImpl(double value1, double value2, bool standardFpscr)
        {
            ExecutionContext context = NativeInterface.GetContext();
            FPCR fpcr = standardFpscr ? context.StandardFpcrValue : context.Fpcr;

            value1.FPUnpack(out FPType type1, out _, out _, context, fpcr);
            value2.FPUnpack(out FPType type2, out _, out _, context, fpcr);

            if (type1 == FPType.QNaN && type2 != FPType.QNaN)
            {
                value1 = FPInfinity(false);
            }
            else if (type1 != FPType.QNaN && type2 == FPType.QNaN)
            {
                value2 = FPInfinity(false);
            }

            return FPMinFpscrImpl(value1, value2, standardFpscr);
        }

        [UnmanagedCallersOnly]
        public static double FPMul(double value1, double value2)
        {
            return FPMulFpscrImpl(value1, value2, false);
        }

        [UnmanagedCallersOnly]
        public static double FPMulFpscr(double value1, double value2, byte standardFpscr)
        {
            return FPMulFpscrImpl(value1, value2, standardFpscr == 1);
        }

        private static double FPMulFpscrImpl(double value1, double value2, bool standardFpscr)
        {
            ExecutionContext context = NativeInterface.GetContext();
            FPCR fpcr = standardFpscr ? context.StandardFpcrValue : context.Fpcr;

            value1 = value1.FPUnpack(out FPType type1, out bool sign1, out ulong op1, context, fpcr);
            value2 = value2.FPUnpack(out FPType type2, out bool sign2, out ulong op2, context, fpcr);

            double result = FPProcessNaNs(type1, type2, op1, op2, out bool done, context, fpcr);

            if (!done)
            {
                bool inf1 = type1 == FPType.Infinity;
                bool zero1 = type1 == FPType.Zero;
                bool inf2 = type2 == FPType.Infinity;
                bool zero2 = type2 == FPType.Zero;

                if ((inf1 && zero2) || (zero1 && inf2))
                {
                    result = FPDefaultNaN();

                    SoftFloat.FPProcessException(FPException.InvalidOp, context, fpcr);
                }
                else if (inf1 || inf2)
                {
                    result = FPInfinity(sign1 ^ sign2);
                }
                else if (zero1 || zero2)
                {
                    result = FPZero(sign1 ^ sign2);
                }
                else
                {
                    result = value1 * value2;

                    if ((fpcr & FPCR.Fz) != 0 && double.IsSubnormal(result))
                    {
                        context.Fpsr |= FPSR.Ufc;

                        result = FPZero(result < 0d);
                    }
                }
            }

            return result;
        }

        [UnmanagedCallersOnly]
        public static double FPMulAdd(double valueA, double value1, double value2)
        {
            return FPMulAddFpscrImpl(valueA, value1, value2, false);
        }

        [UnmanagedCallersOnly]
        public static double FPMulAddFpscr(double valueA, double value1, double value2, byte standardFpscr)
        {
            return FPMulAddFpscrImpl(valueA, value1, value2, standardFpscr == 1);
        }

        private static double FPMulAddFpscrImpl(double valueA, double value1, double value2, bool standardFpscr)
        {
            ExecutionContext context = NativeInterface.GetContext();
            FPCR fpcr = standardFpscr ? context.StandardFpcrValue : context.Fpcr;

            valueA = valueA.FPUnpack(out FPType typeA, out bool signA, out ulong addend, context, fpcr);
            value1 = value1.FPUnpack(out FPType type1, out bool sign1, out ulong op1, context, fpcr);
            value2 = value2.FPUnpack(out FPType type2, out bool sign2, out ulong op2, context, fpcr);

            bool inf1 = type1 == FPType.Infinity;
            bool zero1 = type1 == FPType.Zero;
            bool inf2 = type2 == FPType.Infinity;
            bool zero2 = type2 == FPType.Zero;

            double result = FPProcessNaNs3(typeA, type1, type2, addend, op1, op2, out bool done, context, fpcr);

            if (typeA == FPType.QNaN && ((inf1 && zero2) || (zero1 && inf2)))
            {
                result = FPDefaultNaN();

                SoftFloat.FPProcessException(FPException.InvalidOp, context, fpcr);
            }

            if (!done)
            {
                bool infA = typeA == FPType.Infinity;
                bool zeroA = typeA == FPType.Zero;

                bool signP = sign1 ^ sign2;
                bool infP = inf1 || inf2;
                bool zeroP = zero1 || zero2;

                if ((inf1 && zero2) || (zero1 && inf2) || (infA && infP && signA != signP))
                {
                    result = FPDefaultNaN();

                    SoftFloat.FPProcessException(FPException.InvalidOp, context, fpcr);
                }
                else if ((infA && !signA) || (infP && !signP))
                {
                    result = FPInfinity(false);
                }
                else if ((infA && signA) || (infP && signP))
                {
                    result = FPInfinity(true);
                }
                else if (zeroA && zeroP && signA == signP)
                {
                    result = FPZero(signA);
                }
                else
                {
                    result = Math.FusedMultiplyAdd(value1, value2, valueA);

                    if ((fpcr & FPCR.Fz) != 0 && double.IsSubnormal(result))
                    {
                        context.Fpsr |= FPSR.Ufc;

                        result = FPZero(result < 0d);
                    }
                }
            }

            return result;
        }

        [UnmanagedCallersOnly]
        public static double FPMulSub(double valueA, double value1, double value2)
        {
            value1 = value1.FPNeg();

            return FPMulAddFpscrImpl(valueA, value1, value2, false);
        }

        [UnmanagedCallersOnly]
        public static double FPMulSubFpscr(double valueA, double value1, double value2, byte standardFpscr)
        {
            value1 = value1.FPNeg();

            return FPMulAddFpscrImpl(valueA, value1, value2, standardFpscr == 1);
        }

        [UnmanagedCallersOnly]
        public static double FPMulX(double value1, double value2)
        {
            ExecutionContext context = NativeInterface.GetContext();
            FPCR fpcr = context.Fpcr;

            value1 = value1.FPUnpack(out FPType type1, out bool sign1, out ulong op1, context, fpcr);
            value2 = value2.FPUnpack(out FPType type2, out bool sign2, out ulong op2, context, fpcr);

            double result = FPProcessNaNs(type1, type2, op1, op2, out bool done, context, fpcr);

            if (!done)
            {
                bool inf1 = type1 == FPType.Infinity;
                bool zero1 = type1 == FPType.Zero;
                bool inf2 = type2 == FPType.Infinity;
                bool zero2 = type2 == FPType.Zero;

                if ((inf1 && zero2) || (zero1 && inf2))
                {
                    result = FPTwo(sign1 ^ sign2);
                }
                else if (inf1 || inf2)
                {
                    result = FPInfinity(sign1 ^ sign2);
                }
                else if (zero1 || zero2)
                {
                    result = FPZero(sign1 ^ sign2);
                }
                else
                {
                    result = value1 * value2;

                    if ((fpcr & FPCR.Fz) != 0 && double.IsSubnormal(result))
                    {
                        context.Fpsr |= FPSR.Ufc;

                        result = FPZero(result < 0d);
                    }
                }
            }

            return result;
        }

        [UnmanagedCallersOnly]
        public static double FPNegMulAdd(double valueA, double value1, double value2)
        {
            valueA = valueA.FPNeg();
            value1 = value1.FPNeg();

            return FPMulAddFpscrImpl(valueA, value1, value2, false);
        }

        [UnmanagedCallersOnly]
        public static double FPNegMulSub(double valueA, double value1, double value2)
        {
            valueA = valueA.FPNeg();

            return FPMulAddFpscrImpl(valueA, value1, value2, false);
        }

        [UnmanagedCallersOnly]
        public static double FPRecipEstimate(double value)
        {
            return FPRecipEstimateFpscrImpl(value, false);
        }

        [UnmanagedCallersOnly]
        public static double FPRecipEstimateFpscr(double value, byte standardFpscr)
        {
            return FPRecipEstimateFpscrImpl(value, standardFpscr == 1);
        }

        private static double FPRecipEstimateFpscrImpl(double value, bool standardFpscr)
        {
            ExecutionContext context = NativeInterface.GetContext();
            FPCR fpcr = standardFpscr ? context.StandardFpcrValue : context.Fpcr;

            value.FPUnpack(out FPType type, out bool sign, out ulong op, context, fpcr);

            double result;

            if (type == FPType.SNaN || type == FPType.QNaN)
            {
                result = FPProcessNaN(type, op, context, fpcr);
            }
            else if (type == FPType.Infinity)
            {
                result = FPZero(sign);
            }
            else if (type == FPType.Zero)
            {
                result = FPInfinity(sign);

                SoftFloat.FPProcessException(FPException.DivideByZero, context, fpcr);
            }
            else if (Math.Abs(value) < Math.Pow(2d, -1024))
            {
                var overflowToInf = fpcr.GetRoundingMode() switch
                {
                    FPRoundingMode.ToNearest => true,
                    FPRoundingMode.TowardsPlusInfinity => !sign,
                    FPRoundingMode.TowardsMinusInfinity => sign,
                    FPRoundingMode.TowardsZero => false,
                    _ => throw new ArgumentException($"Invalid rounding mode \"{fpcr.GetRoundingMode()}\"."),
                };
                result = overflowToInf ? FPInfinity(sign) : FPMaxNormal(sign);

                SoftFloat.FPProcessException(FPException.Overflow, context, fpcr);
                SoftFloat.FPProcessException(FPException.Inexact, context, fpcr);
            }
            else if ((fpcr & FPCR.Fz) != 0 && (Math.Abs(value) >= Math.Pow(2d, 1022)))
            {
                result = FPZero(sign);

                context.Fpsr |= FPSR.Ufc;
            }
            else
            {
                ulong fraction = op & 0x000FFFFFFFFFFFFFul;
                uint exp = (uint)((op & 0x7FF0000000000000ul) >> 52);

                if (exp == 0u)
                {
                    if ((fraction & 0x0008000000000000ul) == 0ul)
                    {
                        fraction = (fraction & 0x0003FFFFFFFFFFFFul) << 2;
                        exp -= 1u;
                    }
                    else
                    {
                        fraction = (fraction & 0x0007FFFFFFFFFFFFul) << 1;
                    }
                }

                uint scaled = (uint)(((fraction & 0x000FF00000000000ul) | 0x0010000000000000ul) >> 44);

                uint resultExp = 2045u - exp;

                uint estimate = (uint)SoftFloat.RecipEstimateTable[scaled - 256u] + 256u;

                fraction = (ulong)(estimate & 0xFFu) << 44;

                if (resultExp == 0u)
                {
                    fraction = ((fraction & 0x000FFFFFFFFFFFFEul) | 0x0010000000000000ul) >> 1;
                }
                else if (resultExp + 1u == 0u)
                {
                    fraction = ((fraction & 0x000FFFFFFFFFFFFCul) | 0x0010000000000000ul) >> 2;
                    resultExp = 0u;
                }

                result = BitConverter.Int64BitsToDouble(
                    (long)((sign ? 1ul : 0ul) << 63 | (resultExp & 0x7FFul) << 52 | (fraction & 0x000FFFFFFFFFFFFFul)));
            }

            return result;
        }

        [UnmanagedCallersOnly]
        public static double FPRecipStep(double value1, double value2)
        {
            ExecutionContext context = NativeInterface.GetContext();
            FPCR fpcr = context.StandardFpcrValue;

            value1 = value1.FPUnpack(out FPType type1, out _, out ulong op1, context, fpcr);
            value2 = value2.FPUnpack(out FPType type2, out _, out ulong op2, context, fpcr);

            double result = FPProcessNaNs(type1, type2, op1, op2, out bool done, context, fpcr);

            if (!done)
            {
                bool inf1 = type1 == FPType.Infinity;
                bool zero1 = type1 == FPType.Zero;
                bool inf2 = type2 == FPType.Infinity;
                bool zero2 = type2 == FPType.Zero;

                double product;

                if ((inf1 && zero2) || (zero1 && inf2))
                {
                    product = FPZero(false);
                }
                else
                {
                    product = FPMulFpscrImpl(value1, value2, true);
                }

                result = FPSubFpscr(FPTwo(false), product, true);
            }

            return result;
        }

        [UnmanagedCallersOnly]
        public static double FPRecipStepFused(double value1, double value2)
        {
            ExecutionContext context = NativeInterface.GetContext();
            FPCR fpcr = context.Fpcr;

            value1 = value1.FPNeg();

            value1 = value1.FPUnpack(out FPType type1, out bool sign1, out ulong op1, context, fpcr);
            value2 = value2.FPUnpack(out FPType type2, out bool sign2, out ulong op2, context, fpcr);

            double result = FPProcessNaNs(type1, type2, op1, op2, out bool done, context, fpcr);

            if (!done)
            {
                bool inf1 = type1 == FPType.Infinity;
                bool zero1 = type1 == FPType.Zero;
                bool inf2 = type2 == FPType.Infinity;
                bool zero2 = type2 == FPType.Zero;

                if ((inf1 && zero2) || (zero1 && inf2))
                {
                    result = FPTwo(false);
                }
                else if (inf1 || inf2)
                {
                    result = FPInfinity(sign1 ^ sign2);
                }
                else
                {
                    result = Math.FusedMultiplyAdd(value1, value2, 2d);

                    if ((fpcr & FPCR.Fz) != 0 && double.IsSubnormal(result))
                    {
                        context.Fpsr |= FPSR.Ufc;

                        result = FPZero(result < 0d);
                    }
                }
            }

            return result;
        }

        [UnmanagedCallersOnly]
        public static double FPRecpX(double value)
        {
            ExecutionContext context = NativeInterface.GetContext();
            FPCR fpcr = context.Fpcr;

            value.FPUnpack(out FPType type, out bool sign, out ulong op, context, fpcr);

            double result;

            if (type == FPType.SNaN || type == FPType.QNaN)
            {
                result = FPProcessNaN(type, op, context, fpcr);
            }
            else
            {
                ulong notExp = (~op >> 52) & 0x7FFul;
                ulong maxExp = 0x7FEul;

                result = BitConverter.Int64BitsToDouble(
                    (long)((sign ? 1ul : 0ul) << 63 | (notExp == 0x7FFul ? maxExp : notExp) << 52));
            }

            return result;
        }

        [UnmanagedCallersOnly]
        public static double FPRSqrtEstimate(double value)
        {
            return FPRSqrtEstimateFpscrImpl(value, false);
        }

        [UnmanagedCallersOnly]
        public static double FPRSqrtEstimateFpscr(double value, byte standardFpscr)
        {
            return FPRSqrtEstimateFpscrImpl(value, standardFpscr == 1);
        }

        private static double FPRSqrtEstimateFpscrImpl(double value, bool standardFpscr)
        {
            ExecutionContext context = NativeInterface.GetContext();
            FPCR fpcr = standardFpscr ? context.StandardFpcrValue : context.Fpcr;

            value.FPUnpack(out FPType type, out bool sign, out ulong op, context, fpcr);

            double result;

            if (type == FPType.SNaN || type == FPType.QNaN)
            {
                result = FPProcessNaN(type, op, context, fpcr);
            }
            else if (type == FPType.Zero)
            {
                result = FPInfinity(sign);

                SoftFloat.FPProcessException(FPException.DivideByZero, context, fpcr);
            }
            else if (sign)
            {
                result = FPDefaultNaN();

                SoftFloat.FPProcessException(FPException.InvalidOp, context, fpcr);
            }
            else if (type == FPType.Infinity)
            {
                result = FPZero(false);
            }
            else
            {
                ulong fraction = op & 0x000FFFFFFFFFFFFFul;
                uint exp = (uint)((op & 0x7FF0000000000000ul) >> 52);

                if (exp == 0u)
                {
                    while ((fraction & 0x0008000000000000ul) == 0ul)
                    {
                        fraction = (fraction & 0x0007FFFFFFFFFFFFul) << 1;
                        exp -= 1u;
                    }

                    fraction = (fraction & 0x0007FFFFFFFFFFFFul) << 1;
                }

                uint scaled;

                if ((exp & 1u) == 0u)
                {
                    scaled = (uint)(((fraction & 0x000FF00000000000ul) | 0x0010000000000000ul) >> 44);
                }
                else
                {
                    scaled = (uint)(((fraction & 0x000FE00000000000ul) | 0x0010000000000000ul) >> 45);
                }

                uint resultExp = (3068u - exp) >> 1;

                uint estimate = (uint)SoftFloat.RecipSqrtEstimateTable[scaled - 128u] + 256u;

                result = BitConverter.Int64BitsToDouble((long)((resultExp & 0x7FFul) << 52 | (estimate & 0xFFul) << 44));
            }

            return result;
        }

        public static double FPHalvedSub(double value1, double value2, ExecutionContext context, FPCR fpcr)
        {
            value1 = value1.FPUnpack(out FPType type1, out bool sign1, out ulong op1, context, fpcr);
            value2 = value2.FPUnpack(out FPType type2, out bool sign2, out ulong op2, context, fpcr);

            double result = FPProcessNaNs(type1, type2, op1, op2, out bool done, context, fpcr);

            if (!done)
            {
                bool inf1 = type1 == FPType.Infinity;
                bool zero1 = type1 == FPType.Zero;
                bool inf2 = type2 == FPType.Infinity;
                bool zero2 = type2 == FPType.Zero;

                if (inf1 && inf2 && sign1 == sign2)
                {
                    result = FPDefaultNaN();

                    SoftFloat.FPProcessException(FPException.InvalidOp, context, fpcr);
                }
                else if ((inf1 && !sign1) || (inf2 && sign2))
                {
                    result = FPInfinity(false);
                }
                else if ((inf1 && sign1) || (inf2 && !sign2))
                {
                    result = FPInfinity(true);
                }
                else if (zero1 && zero2 && sign1 == !sign2)
                {
                    result = FPZero(sign1);
                }
                else
                {
                    result = (value1 - value2) / 2.0;

                    if ((fpcr & FPCR.Fz) != 0 && double.IsSubnormal(result))
                    {
                        context.Fpsr |= FPSR.Ufc;

                        result = FPZero(result < 0d);
                    }
                }
            }

            return result;
        }

        [UnmanagedCallersOnly]
        public static double FPRSqrtStep(double value1, double value2)
        {
            ExecutionContext context = NativeInterface.GetContext();
            FPCR fpcr = context.StandardFpcrValue;

            value1 = value1.FPUnpack(out FPType type1, out _, out ulong op1, context, fpcr);
            value2 = value2.FPUnpack(out FPType type2, out _, out ulong op2, context, fpcr);

            double result = FPProcessNaNs(type1, type2, op1, op2, out bool done, context, fpcr);

            if (!done)
            {
                bool inf1 = type1 == FPType.Infinity;
                bool zero1 = type1 == FPType.Zero;
                bool inf2 = type2 == FPType.Infinity;
                bool zero2 = type2 == FPType.Zero;

                double product;

                if ((inf1 && zero2) || (zero1 && inf2))
                {
                    product = FPZero(false);
                }
                else
                {
                    product = FPMulFpscrImpl(value1, value2, true);
                }

                result = FPHalvedSub(FPThree(false), product, context, fpcr);
            }

            return result;
        }

        [UnmanagedCallersOnly]
        public static double FPRSqrtStepFused(double value1, double value2)
        {
            ExecutionContext context = NativeInterface.GetContext();
            FPCR fpcr = context.Fpcr;

            value1 = value1.FPNeg();

            value1 = value1.FPUnpack(out FPType type1, out bool sign1, out ulong op1, context, fpcr);
            value2 = value2.FPUnpack(out FPType type2, out bool sign2, out ulong op2, context, fpcr);

            double result = FPProcessNaNs(type1, type2, op1, op2, out bool done, context, fpcr);

            if (!done)
            {
                bool inf1 = type1 == FPType.Infinity;
                bool zero1 = type1 == FPType.Zero;
                bool inf2 = type2 == FPType.Infinity;
                bool zero2 = type2 == FPType.Zero;

                if ((inf1 && zero2) || (zero1 && inf2))
                {
                    result = FPOnePointFive(false);
                }
                else if (inf1 || inf2)
                {
                    result = FPInfinity(sign1 ^ sign2);
                }
                else
                {
                    result = Math.FusedMultiplyAdd(value1, value2, 3d) / 2d;

                    if ((fpcr & FPCR.Fz) != 0 && double.IsSubnormal(result))
                    {
                        context.Fpsr |= FPSR.Ufc;

                        result = FPZero(result < 0d);
                    }
                }
            }

            return result;
        }

        [UnmanagedCallersOnly]
        public static double FPSqrt(double value)
        {
            ExecutionContext context = NativeInterface.GetContext();
            FPCR fpcr = context.Fpcr;

            value = value.FPUnpack(out FPType type, out bool sign, out ulong op, context, fpcr);

            double result;

            if (type == FPType.SNaN || type == FPType.QNaN)
            {
                result = FPProcessNaN(type, op, context, fpcr);
            }
            else if (type == FPType.Zero)
            {
                result = FPZero(sign);
            }
            else if (type == FPType.Infinity && !sign)
            {
                result = FPInfinity(sign);
            }
            else if (sign)
            {
                result = FPDefaultNaN();

                SoftFloat.FPProcessException(FPException.InvalidOp, context, fpcr);
            }
            else
            {
                result = Math.Sqrt(value);

                if ((fpcr & FPCR.Fz) != 0 && double.IsSubnormal(result))
                {
                    context.Fpsr |= FPSR.Ufc;

                    result = FPZero(result < 0d);
                }
            }

            return result;
        }

        [UnmanagedCallersOnly]
        public static double FPSub(double value1, double value2)
        {
            return FPSubFpscr(value1, value2, false);
        }

        public static double FPSubFpscr(double value1, double value2, bool standardFpscr)
        {
            ExecutionContext context = NativeInterface.GetContext();
            FPCR fpcr = standardFpscr ? context.StandardFpcrValue : context.Fpcr;

            value1 = value1.FPUnpack(out FPType type1, out bool sign1, out ulong op1, context, fpcr);
            value2 = value2.FPUnpack(out FPType type2, out bool sign2, out ulong op2, context, fpcr);

            double result = FPProcessNaNs(type1, type2, op1, op2, out bool done, context, fpcr);

            if (!done)
            {
                bool inf1 = type1 == FPType.Infinity;
                bool zero1 = type1 == FPType.Zero;
                bool inf2 = type2 == FPType.Infinity;
                bool zero2 = type2 == FPType.Zero;

                if (inf1 && inf2 && sign1 == sign2)
                {
                    result = FPDefaultNaN();

                    SoftFloat.FPProcessException(FPException.InvalidOp, context, fpcr);
                }
                else if ((inf1 && !sign1) || (inf2 && sign2))
                {
                    result = FPInfinity(false);
                }
                else if ((inf1 && sign1) || (inf2 && !sign2))
                {
                    result = FPInfinity(true);
                }
                else if (zero1 && zero2 && sign1 == !sign2)
                {
                    result = FPZero(sign1);
                }
                else
                {
                    result = value1 - value2;

                    if ((fpcr & FPCR.Fz) != 0 && double.IsSubnormal(result))
                    {
                        context.Fpsr |= FPSR.Ufc;

                        result = FPZero(result < 0d);
                    }
                }
            }

            return result;
        }

        public static double FPDefaultNaN()
        {
            return BitConverter.Int64BitsToDouble(0x7ff8000000000000);
        }

        public static double FPInfinity(bool sign)
        {
            return sign ? double.NegativeInfinity : double.PositiveInfinity;
        }

        public static double FPZero(bool sign)
        {
            return sign ? -0d : +0d;
        }

        public static double FPMaxNormal(bool sign)
        {
            return sign ? double.MinValue : double.MaxValue;
        }

        private static double FPTwo(bool sign)
        {
            return sign ? -2d : +2d;
        }

        private static double FPThree(bool sign)
        {
            return sign ? -3d : +3d;
        }

        private static double FPOnePointFive(bool sign)
        {
            return sign ? -1.5d : +1.5d;
        }

        private static double FPNeg(this double value)
        {
            return -value;
        }

        private static double ZerosOrOnes(bool ones)
        {
            return BitConverter.Int64BitsToDouble(ones ? -1L : 0L);
        }

        private static double FPUnpack(
            this double value,
            out FPType type,
            out bool sign,
            out ulong valueBits,
            ExecutionContext context,
            FPCR fpcr)
        {
            valueBits = (ulong)BitConverter.DoubleToInt64Bits(value);

            sign = (~valueBits & 0x8000000000000000ul) == 0ul;

            if ((valueBits & 0x7FF0000000000000ul) == 0ul)
            {
                if ((valueBits & 0x000FFFFFFFFFFFFFul) == 0ul || (fpcr & FPCR.Fz) != 0)
                {
                    type = FPType.Zero;
                    value = FPZero(sign);

                    if ((valueBits & 0x000FFFFFFFFFFFFFul) != 0ul)
                    {
                        SoftFloat.FPProcessException(FPException.InputDenorm, context, fpcr);
                    }
                }
                else
                {
                    type = FPType.Nonzero;
                }
            }
            else if ((~valueBits & 0x7FF0000000000000ul) == 0ul)
            {
                if ((valueBits & 0x000FFFFFFFFFFFFFul) == 0ul)
                {
                    type = FPType.Infinity;
                }
                else
                {
                    type = (~valueBits & 0x0008000000000000ul) == 0ul ? FPType.QNaN : FPType.SNaN;
                    value = FPZero(sign);
                }
            }
            else
            {
                type = FPType.Nonzero;
            }

            return value;
        }

        private static double FPProcessNaNs(
            FPType type1,
            FPType type2,
            ulong op1,
            ulong op2,
            out bool done,
            ExecutionContext context,
            FPCR fpcr)
        {
            done = true;

            if (type1 == FPType.SNaN)
            {
                return FPProcessNaN(type1, op1, context, fpcr);
            }
            else if (type2 == FPType.SNaN)
            {
                return FPProcessNaN(type2, op2, context, fpcr);
            }
            else if (type1 == FPType.QNaN)
            {
                return FPProcessNaN(type1, op1, context, fpcr);
            }
            else if (type2 == FPType.QNaN)
            {
                return FPProcessNaN(type2, op2, context, fpcr);
            }

            done = false;

            return FPZero(false);
        }

        private static double FPProcessNaNs3(
            FPType type1,
            FPType type2,
            FPType type3,
            ulong op1,
            ulong op2,
            ulong op3,
            out bool done,
            ExecutionContext context,
            FPCR fpcr)
        {
            done = true;

            if (type1 == FPType.SNaN)
            {
                return FPProcessNaN(type1, op1, context, fpcr);
            }
            else if (type2 == FPType.SNaN)
            {
                return FPProcessNaN(type2, op2, context, fpcr);
            }
            else if (type3 == FPType.SNaN)
            {
                return FPProcessNaN(type3, op3, context, fpcr);
            }
            else if (type1 == FPType.QNaN)
            {
                return FPProcessNaN(type1, op1, context, fpcr);
            }
            else if (type2 == FPType.QNaN)
            {
                return FPProcessNaN(type2, op2, context, fpcr);
            }
            else if (type3 == FPType.QNaN)
            {
                return FPProcessNaN(type3, op3, context, fpcr);
            }

            done = false;

            return FPZero(false);
        }

        private static double FPProcessNaN(FPType type, ulong op, ExecutionContext context, FPCR fpcr)
        {
            if (type == FPType.SNaN)
            {
                op |= 1ul << 51;

                SoftFloat.FPProcessException(FPException.InvalidOp, context, fpcr);
            }

            if ((fpcr & FPCR.Dn) != 0)
            {
                return FPDefaultNaN();
            }

            return BitConverter.Int64BitsToDouble((long)op);
        }
    }
}
