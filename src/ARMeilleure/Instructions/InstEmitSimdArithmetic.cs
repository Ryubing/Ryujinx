// https://github.com/intel/ARM_NEON_2_x86_SSE/blob/master/NEON_2_SSE.h
// https://www.agner.org/optimize/#vectorclass @ vectori128.h

using ARMeilleure.Decoders;
using ARMeilleure.IntermediateRepresentation;
using ARMeilleure.State;
using ARMeilleure.Translation;
using System;
using System.Diagnostics;
using static ARMeilleure.Instructions.InstEmitHelper;
using static ARMeilleure.Instructions.InstEmitSimdHelper;
using static ARMeilleure.Instructions.InstEmitSimdHelper32;
using static ARMeilleure.IntermediateRepresentation.Operand.Factory;

namespace ARMeilleure.Instructions
{
    using Func2I = Func<Operand, Operand, Operand>;

    static partial class InstEmit
    {
        public static void Abs_S(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitScalarUnaryOp(context, Intrinsic.Arm64AbsS);
            }
            else
            {
                EmitScalarUnaryOpSx(context, (op1) => EmitAbs(context, op1));
            }
        }

        public static void Abs_V(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitVectorUnaryOp(context, Intrinsic.Arm64AbsV);
            }
            else
            {
                EmitVectorUnaryOpSx(context, (op1) => EmitAbs(context, op1));
            }
        }

        public static void Add_S(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitScalarBinaryOp(context, Intrinsic.Arm64AddS);
            }
            else
            {
                EmitScalarBinaryOpZx(context, (op1, op2) => context.Add(op1, op2));
            }
        }

        public static void Add_V(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitVectorBinaryOp(context, Intrinsic.Arm64AddV);
            }
            else if (Optimizations.UseSse2)
            {
                OpCodeSimdReg op = (OpCodeSimdReg)context.CurrOp;

                Operand n = GetVec(op.Rn);
                Operand m = GetVec(op.Rm);

                Intrinsic addInst = X86PaddInstruction[op.Size];

                Operand res = context.AddIntrinsic(addInst, n, m);

                if (op.RegisterSize == RegisterSize.Simd64)
                {
                    res = context.VectorZeroUpper64(res);
                }

                context.Copy(GetVec(op.Rd), res);
            }
            else
            {
                EmitVectorBinaryOpZx(context, (op1, op2) => context.Add(op1, op2));
            }
        }

        public static void Addhn_V(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitVectorTernaryOpRd(context, Intrinsic.Arm64AddhnV);
            }
            else
            {
                EmitHighNarrow(context, (op1, op2) => context.Add(op1, op2), round: false);
            }
        }

        public static void Addp_S(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitScalarUnaryOp(context, Intrinsic.Arm64AddpS);
            }
            else
            {
                OpCodeSimd op = (OpCodeSimd)context.CurrOp;

                Operand ne0 = EmitVectorExtractZx(context, op.Rn, 0, op.Size);
                Operand ne1 = EmitVectorExtractZx(context, op.Rn, 1, op.Size);

                Operand res = context.Add(ne0, ne1);

                context.Copy(GetVec(op.Rd), EmitVectorInsert(context, context.VectorZero(), res, 0, op.Size));
            }
        }

        public static void Addp_V(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitVectorBinaryOp(context, Intrinsic.Arm64AddpV);
            }
            else if (Optimizations.UseSsse3)
            {
                EmitSsse3VectorPairwiseOp(context, X86PaddInstruction);
            }
            else
            {
                EmitVectorPairwiseOpZx(context, (op1, op2) => context.Add(op1, op2));
            }
        }

        public static void Addv_V(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitVectorUnaryOp(context, Intrinsic.Arm64AddvV);
            }
            else
            {
                EmitVectorAcrossVectorOpZx(context, (op1, op2) => context.Add(op1, op2));
            }
        }

        public static void Cls_V(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitVectorUnaryOp(context, Intrinsic.Arm64ClsV);
            }
            else
            {
                OpCodeSimd op = (OpCodeSimd)context.CurrOp;

                Operand res = context.VectorZero();

                int elems = op.GetBytesCount() >> op.Size;

                int eSize = 8 << op.Size;

                for (int index = 0; index < elems; index++)
                {
                    Operand ne = EmitVectorExtractZx(context, op.Rn, index, op.Size);

                    Operand de = context.Call(typeof(SoftFallback).GetMethod(nameof(SoftFallback.CountLeadingSigns)), ne, Const(eSize));

                    res = EmitVectorInsert(context, res, de, index, op.Size);
                }

                context.Copy(GetVec(op.Rd), res);
            }
        }

        public static void Clz_V(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitVectorUnaryOp(context, Intrinsic.Arm64ClzV);
            }
            else
            {
                OpCodeSimd op = (OpCodeSimd)context.CurrOp;

                int eSize = 8 << op.Size;

                Operand res = eSize switch
                {
                    8 => Clz_V_I8(context, GetVec(op.Rn)),
                    16 => Clz_V_I16(context, GetVec(op.Rn)),
                    32 => Clz_V_I32(context, GetVec(op.Rn)),
                    _ => default,
                };

                if (res != default)
                {
                    if (op.RegisterSize == RegisterSize.Simd64)
                    {
                        res = context.VectorZeroUpper64(res);
                    }
                }
                else
                {
                    int elems = op.GetBytesCount() >> op.Size;

                    res = context.VectorZero();

                    for (int index = 0; index < elems; index++)
                    {
                        Operand ne = EmitVectorExtractZx(context, op.Rn, index, op.Size);

                        Operand de = context.Call(typeof(SoftFallback).GetMethod(nameof(SoftFallback.CountLeadingZeros)), ne, Const(eSize));

                        res = EmitVectorInsert(context, res, de, index, op.Size);
                    }
                }

                context.Copy(GetVec(op.Rd), res);
            }
        }

        private static Operand Clz_V_I8(ArmEmitterContext context, Operand arg)
        {
            if (!Optimizations.UseSsse3)
            {
                return default;
            }

            // CLZ nibble table.
            Operand clzTable = X86GetScalar(context, 0x01_01_01_01_02_02_03_04);

            Operand maskLow = X86GetAllElements(context, 0x0f_0f_0f_0f);
            Operand c04 = X86GetAllElements(context, 0x04_04_04_04);

            // CLZ of low 4 bits of elements in arg.
            Operand loClz = context.AddIntrinsic(Intrinsic.X86Pshufb, clzTable, arg);

            // Get the high 4 bits of elements in arg.
            Operand hiArg = context.AddIntrinsic(Intrinsic.X86Psrlw, arg, Const(4));
            hiArg = context.AddIntrinsic(Intrinsic.X86Pand, hiArg, maskLow);

            // CLZ of high 4 bits of elements in arg.
            Operand hiClz = context.AddIntrinsic(Intrinsic.X86Pshufb, clzTable, hiArg);

            // If high 4 bits are not all zero, we discard the CLZ of the low 4 bits.
            Operand mask = context.AddIntrinsic(Intrinsic.X86Pcmpeqb, hiClz, c04);
            loClz = context.AddIntrinsic(Intrinsic.X86Pand, loClz, mask);

            return context.AddIntrinsic(Intrinsic.X86Paddb, loClz, hiClz);
        }

        private static Operand Clz_V_I16(ArmEmitterContext context, Operand arg)
        {
            if (!Optimizations.UseSsse3)
            {
                return default;
            }

            Operand maskSwap = X86GetElements(context, 0x80_0f_80_0d_80_0b_80_09, 0x80_07_80_05_80_03_80_01);
            Operand maskLow = X86GetAllElements(context, 0x00ff_00ff);
            Operand c0008 = X86GetAllElements(context, 0x0008_0008);

            // CLZ pair of high 8 and low 8 bits of elements in arg.
            Operand hiloClz = Clz_V_I8(context, arg);
            // Get CLZ of low 8 bits in each pair.
            Operand loClz = context.AddIntrinsic(Intrinsic.X86Pand, hiloClz, maskLow);
            // Get CLZ of high 8 bits in each pair.
            Operand hiClz = context.AddIntrinsic(Intrinsic.X86Pshufb, hiloClz, maskSwap);

            // If high 8 bits are not all zero, we discard the CLZ of the low 8 bits.
            Operand mask = context.AddIntrinsic(Intrinsic.X86Pcmpeqw, hiClz, c0008);
            loClz = context.AddIntrinsic(Intrinsic.X86Pand, loClz, mask);

            return context.AddIntrinsic(Intrinsic.X86Paddw, loClz, hiClz);
        }

        private static Operand Clz_V_I32(ArmEmitterContext context, Operand arg)
        {
            // TODO: Use vplzcntd when AVX-512 is supported.
            if (!Optimizations.UseSse2)
            {
                return default;
            }

#pragma warning disable IDE0055 // Disable formatting
            Operand AddVectorI32(Operand op0, Operand op1)      => context.AddIntrinsic(Intrinsic.X86Paddd, op0, op1);
            Operand SubVectorI32(Operand op0, Operand op1)      => context.AddIntrinsic(Intrinsic.X86Psubd, op0, op1);
            Operand ShiftRightVectorUI32(Operand op0, int imm8) => context.AddIntrinsic(Intrinsic.X86Psrld, op0, Const(imm8));
            Operand OrVector(Operand op0, Operand op1)          => context.AddIntrinsic(Intrinsic.X86Por, op0, op1);
            Operand AndVector(Operand op0, Operand op1)         => context.AddIntrinsic(Intrinsic.X86Pand, op0, op1);
            Operand NotVector(Operand op0)                      => context.AddIntrinsic(Intrinsic.X86Pandn, op0, context.VectorOne());
#pragma warning restore IDE0055

            Operand c55555555 = X86GetAllElements(context, 0x55555555);
            Operand c33333333 = X86GetAllElements(context, 0x33333333);
            Operand c0f0f0f0f = X86GetAllElements(context, 0x0f0f0f0f);
            Operand c0000003f = X86GetAllElements(context, 0x0000003f);

            Operand tmp0;
            Operand tmp1;
            Operand res;

            // Set all bits after highest set bit to 1.
            res = OrVector(ShiftRightVectorUI32(arg, 1), arg);
            res = OrVector(ShiftRightVectorUI32(res, 2), res);
            res = OrVector(ShiftRightVectorUI32(res, 4), res);
            res = OrVector(ShiftRightVectorUI32(res, 8), res);
            res = OrVector(ShiftRightVectorUI32(res, 16), res);

            // Make leading 0s into leading 1s.
            res = NotVector(res);

            // Count leading 1s, which is the population count.
            tmp0 = ShiftRightVectorUI32(res, 1);
            tmp0 = AndVector(tmp0, c55555555);
            res = SubVectorI32(res, tmp0);

            tmp0 = ShiftRightVectorUI32(res, 2);
            tmp0 = AndVector(tmp0, c33333333);
            tmp1 = AndVector(res, c33333333);
            res = AddVectorI32(tmp0, tmp1);

            tmp0 = ShiftRightVectorUI32(res, 4);
            tmp0 = AddVectorI32(tmp0, res);
            res = AndVector(tmp0, c0f0f0f0f);

            tmp0 = ShiftRightVectorUI32(res, 8);
            res = AddVectorI32(tmp0, res);

            tmp0 = ShiftRightVectorUI32(res, 16);
            res = AddVectorI32(tmp0, res);

            res = AndVector(res, c0000003f);

            return res;
        }

        public static void Cnt_V(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitVectorUnaryOp(context, Intrinsic.Arm64CntV);
            }
            else
            {
                OpCodeSimd op = (OpCodeSimd)context.CurrOp;

                Operand res = context.VectorZero();

                int elems = op.RegisterSize == RegisterSize.Simd128 ? 16 : 8;

                for (int index = 0; index < elems; index++)
                {
                    Operand ne = EmitVectorExtractZx(context, op.Rn, index, 0);

                    Operand de;

                    if (Optimizations.UsePopCnt)
                    {
                        de = context.AddIntrinsicLong(Intrinsic.X86Popcnt, ne);
                    }
                    else
                    {
                        de = EmitCountSetBits8(context, ne);
                    }

                    res = EmitVectorInsert(context, res, de, index, 0);
                }

                context.Copy(GetVec(op.Rd), res);
            }
        }

        public static void Fabd_S(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitScalarBinaryOpF(context, Intrinsic.Arm64FabdS);
            }
            else if (Optimizations.FastFP && Optimizations.UseSse2)
            {
                OpCodeSimdReg op = (OpCodeSimdReg)context.CurrOp;

                int sizeF = op.Size & 1;

                if (sizeF == 0)
                {
                    Operand res = context.AddIntrinsic(Intrinsic.X86Subss, GetVec(op.Rn), GetVec(op.Rm));

                    res = EmitFloatAbs(context, res, true, false);

                    context.Copy(GetVec(op.Rd), context.VectorZeroUpper96(res));
                }
                else /* if (sizeF == 1) */
                {
                    Operand res = context.AddIntrinsic(Intrinsic.X86Subsd, GetVec(op.Rn), GetVec(op.Rm));

                    res = EmitFloatAbs(context, res, false, false);

                    context.Copy(GetVec(op.Rd), context.VectorZeroUpper64(res));
                }
            }
            else
            {
                EmitScalarBinaryOpF(context, (op1, op2) =>
                {
                    Operand res = EmitSoftFloatCall(context, nameof(SoftFloat32.FPSub), op1, op2);

                    return EmitUnaryMathCall(context, nameof(MathHelper.Abs), res);
                });
            }
        }

        public static void Fabd_V(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitVectorBinaryOpF(context, Intrinsic.Arm64FabdV);
            }
            else if (Optimizations.FastFP && Optimizations.UseSse2)
            {
                OpCodeSimdReg op = (OpCodeSimdReg)context.CurrOp;

                int sizeF = op.Size & 1;

                if (sizeF == 0)
                {
                    Operand res = context.AddIntrinsic(Intrinsic.X86Subps, GetVec(op.Rn), GetVec(op.Rm));

                    res = EmitFloatAbs(context, res, true, true);

                    if (op.RegisterSize == RegisterSize.Simd64)
                    {
                        res = context.VectorZeroUpper64(res);
                    }

                    context.Copy(GetVec(op.Rd), res);
                }
                else /* if (sizeF == 1) */
                {
                    Operand res = context.AddIntrinsic(Intrinsic.X86Subpd, GetVec(op.Rn), GetVec(op.Rm));

                    res = EmitFloatAbs(context, res, false, true);

                    context.Copy(GetVec(op.Rd), res);
                }
            }
            else
            {
                EmitVectorBinaryOpF(context, (op1, op2) =>
                {
                    Operand res = EmitSoftFloatCall(context, nameof(SoftFloat32.FPSub), op1, op2);

                    return EmitUnaryMathCall(context, nameof(MathHelper.Abs), res);
                });
            }
        }

        public static void Fabs_S(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitScalarUnaryOpF(context, Intrinsic.Arm64FabsS);
            }
            else if (Optimizations.UseSse2)
            {
                OpCodeSimd op = (OpCodeSimd)context.CurrOp;

                if (op.Size == 0)
                {
                    Operand res = EmitFloatAbs(context, GetVec(op.Rn), true, false);

                    context.Copy(GetVec(op.Rd), context.VectorZeroUpper96(res));
                }
                else /* if (op.Size == 1) */
                {
                    Operand res = EmitFloatAbs(context, GetVec(op.Rn), false, false);

                    context.Copy(GetVec(op.Rd), context.VectorZeroUpper64(res));
                }
            }
            else
            {
                EmitScalarUnaryOpF(context, (op1) =>
                {
                    return EmitUnaryMathCall(context, nameof(MathHelper.Abs), op1);
                });
            }
        }

        public static void Fabs_V(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitVectorUnaryOpF(context, Intrinsic.Arm64FabsV);
            }
            else if (Optimizations.UseSse2)
            {
                OpCodeSimd op = (OpCodeSimd)context.CurrOp;

                int sizeF = op.Size & 1;

                if (sizeF == 0)
                {
                    Operand res = EmitFloatAbs(context, GetVec(op.Rn), true, true);

                    if (op.RegisterSize == RegisterSize.Simd64)
                    {
                        res = context.VectorZeroUpper64(res);
                    }

                    context.Copy(GetVec(op.Rd), res);
                }
                else /* if (sizeF == 1) */
                {
                    Operand res = EmitFloatAbs(context, GetVec(op.Rn), false, true);

                    context.Copy(GetVec(op.Rd), res);
                }
            }
            else
            {
                EmitVectorUnaryOpF(context, (op1) =>
                {
                    return EmitUnaryMathCall(context, nameof(MathHelper.Abs), op1);
                });
            }
        }

        public static void Fadd_S(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitScalarBinaryOpF(context, Intrinsic.Arm64FaddS);
            }
            else if (Optimizations.FastFP && Optimizations.UseSse2)
            {
                EmitScalarBinaryOpF(context, Intrinsic.X86Addss, Intrinsic.X86Addsd);
            }
            else if (Optimizations.FastFP)
            {
                EmitScalarBinaryOpF(context, (op1, op2) => context.Add(op1, op2));
            }
            else
            {
                EmitScalarBinaryOpF(context, (op1, op2) =>
                {
                    return EmitSoftFloatCall(context, nameof(SoftFloat32.FPAdd), op1, op2);
                });
            }
        }

        public static void Fadd_V(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitVectorBinaryOpF(context, Intrinsic.Arm64FaddV);
            }
            else if (Optimizations.FastFP && Optimizations.UseSse2)
            {
                EmitVectorBinaryOpF(context, Intrinsic.X86Addps, Intrinsic.X86Addpd);
            }
            else if (Optimizations.FastFP)
            {
                EmitVectorBinaryOpF(context, (op1, op2) => context.Add(op1, op2));
            }
            else
            {
                EmitVectorBinaryOpF(context, (op1, op2) =>
                {
                    return EmitSoftFloatCall(context, nameof(SoftFloat32.FPAdd), op1, op2);
                });
            }
        }

        public static void Faddp_S(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitScalarUnaryOpF(context, Intrinsic.Arm64FaddpS);
            }
            else if (Optimizations.FastFP && Optimizations.UseSse3)
            {
                OpCodeSimd op = (OpCodeSimd)context.CurrOp;

                if ((op.Size & 1) == 0)
                {
                    Operand res = context.AddIntrinsic(Intrinsic.X86Haddps, GetVec(op.Rn), GetVec(op.Rn));

                    context.Copy(GetVec(op.Rd), context.VectorZeroUpper96(res));
                }
                else /* if ((op.Size & 1) == 1) */
                {
                    Operand res = context.AddIntrinsic(Intrinsic.X86Haddpd, GetVec(op.Rn), GetVec(op.Rn));

                    context.Copy(GetVec(op.Rd), context.VectorZeroUpper64(res));
                }
            }
            else
            {
                EmitScalarPairwiseOpF(context, (op1, op2) =>
                {
                    return EmitSoftFloatCall(context, nameof(SoftFloat32.FPAdd), op1, op2);
                });
            }
        }

        public static void Faddp_V(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitVectorBinaryOpF(context, Intrinsic.Arm64FaddpV);
            }
            else if (Optimizations.FastFP && Optimizations.UseSse41)
            {
                EmitSse2VectorPairwiseOpF(context, (op1, op2) =>
                {
                    return EmitSse41ProcessNaNsOpF(context, (op1, op2) =>
                    {
                        IOpCodeSimd op = (IOpCodeSimd)context.CurrOp;

                        Intrinsic addInst = (op.Size & 1) == 0 ? Intrinsic.X86Addps : Intrinsic.X86Addpd;

                        return context.AddIntrinsic(addInst, op1, op2);
                    }, scalar: false, op1, op2);
                });
            }
            else
            {
                EmitVectorPairwiseOpF(context, (op1, op2) =>
                {
                    return EmitSoftFloatCall(context, nameof(SoftFloat32.FPAdd), op1, op2);
                });
            }
        }

        public static void Fdiv_S(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitScalarBinaryOpF(context, Intrinsic.Arm64FdivS);
            }
            else if (Optimizations.FastFP && Optimizations.UseSse2)
            {
                EmitScalarBinaryOpF(context, Intrinsic.X86Divss, Intrinsic.X86Divsd);
            }
            else if (Optimizations.FastFP)
            {
                EmitScalarBinaryOpF(context, (op1, op2) => context.Divide(op1, op2));
            }
            else
            {
                EmitScalarBinaryOpF(context, (op1, op2) =>
                {
                    return EmitSoftFloatCall(context, nameof(SoftFloat32.FPDiv), op1, op2);
                });
            }
        }

        public static void Fdiv_V(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitVectorBinaryOpF(context, Intrinsic.Arm64FdivV);
            }
            else if (Optimizations.FastFP && Optimizations.UseSse2)
            {
                EmitVectorBinaryOpF(context, Intrinsic.X86Divps, Intrinsic.X86Divpd);
            }
            else if (Optimizations.FastFP)
            {
                EmitVectorBinaryOpF(context, (op1, op2) => context.Divide(op1, op2));
            }
            else
            {
                EmitVectorBinaryOpF(context, (op1, op2) =>
                {
                    return EmitSoftFloatCall(context, nameof(SoftFloat32.FPDiv), op1, op2);
                });
            }
        }

        public static void Fmadd_S(ArmEmitterContext context) // Fused.
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitScalarTernaryOpF(context, Intrinsic.Arm64FmaddS);
            }
            else if (Optimizations.FastFP && Optimizations.UseSse2)
            {
                OpCodeSimdReg op = (OpCodeSimdReg)context.CurrOp;

                Operand d = GetVec(op.Rd);
                Operand a = GetVec(op.Ra);
                Operand n = GetVec(op.Rn);
                Operand m = GetVec(op.Rm);

                Operand res;

                if (op.Size == 0)
                {
                    if (Optimizations.UseFma)
                    {
                        res = context.AddIntrinsic(Intrinsic.X86Vfmadd231ss, a, n, m);
                    }
                    else
                    {
                        res = context.AddIntrinsic(Intrinsic.X86Mulss, n, m);
                        res = context.AddIntrinsic(Intrinsic.X86Addss, a, res);
                    }

                    context.Copy(d, context.VectorZeroUpper96(res));
                }
                else /* if (op.Size == 1) */
                {
                    if (Optimizations.UseFma)
                    {
                        res = context.AddIntrinsic(Intrinsic.X86Vfmadd231sd, a, n, m);
                    }
                    else
                    {
                        res = context.AddIntrinsic(Intrinsic.X86Mulsd, n, m);
                        res = context.AddIntrinsic(Intrinsic.X86Addsd, a, res);
                    }

                    context.Copy(d, context.VectorZeroUpper64(res));
                }
            }
            else
            {
                EmitScalarTernaryRaOpF(context, (op1, op2, op3) =>
                {
                    return EmitSoftFloatCall(context, nameof(SoftFloat32.FPMulAdd), op1, op2, op3);
                });
            }
        }

        public static void Fmax_S(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitScalarBinaryOpF(context, Intrinsic.Arm64FmaxS);
            }
            else if (Optimizations.FastFP && Optimizations.UseSse41)
            {
                EmitSse41ProcessNaNsOpF(context, (op1, op2) =>
                {
                    return EmitSse2VectorMaxMinOpF(context, op1, op2, isMax: true);
                }, scalar: true);
            }
            else
            {
                EmitScalarBinaryOpF(context, (op1, op2) =>
                {
                    return EmitSoftFloatCall(context, nameof(SoftFloat32.FPMax), op1, op2);
                });
            }
        }

        public static void Fmax_V(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitVectorBinaryOpF(context, Intrinsic.Arm64FmaxV);
            }
            else if (Optimizations.FastFP && Optimizations.UseSse41)
            {
                EmitSse41ProcessNaNsOpF(context, (op1, op2) =>
                {
                    return EmitSse2VectorMaxMinOpF(context, op1, op2, isMax: true);
                }, scalar: false);
            }
            else
            {
                EmitVectorBinaryOpF(context, (op1, op2) =>
                {
                    return EmitSoftFloatCall(context, nameof(SoftFloat32.FPMax), op1, op2);
                });
            }
        }

        public static void Fmaxnm_S(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitScalarBinaryOpF(context, Intrinsic.Arm64FmaxnmS);
            }
            else if (Optimizations.FastFP && Optimizations.UseSse41)
            {
                EmitSse41MaxMinNumOpF(context, isMaxNum: true, scalar: true);
            }
            else
            {
                EmitScalarBinaryOpF(context, (op1, op2) =>
                {
                    return EmitSoftFloatCall(context, nameof(SoftFloat32.FPMaxNum), op1, op2);
                });
            }
        }

        public static void Fmaxnm_V(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitVectorBinaryOpF(context, Intrinsic.Arm64FmaxnmV);
            }
            else if (Optimizations.FastFP && Optimizations.UseSse41)
            {
                EmitSse41MaxMinNumOpF(context, isMaxNum: true, scalar: false);
            }
            else
            {
                EmitVectorBinaryOpF(context, (op1, op2) =>
                {
                    return EmitSoftFloatCall(context, nameof(SoftFloat32.FPMaxNum), op1, op2);
                });
            }
        }

        public static void Fmaxnmp_S(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitScalarUnaryOpF(context, Intrinsic.Arm64FmaxnmpS);
            }
            else if (Optimizations.FastFP && Optimizations.UseSse41)
            {
                EmitSse2ScalarPairwiseOpF(context, (op1, op2) =>
                {
                    return EmitSse41MaxMinNumOpF(context, isMaxNum: true, scalar: true, op1, op2);
                });
            }
            else
            {
                EmitScalarPairwiseOpF(context, (op1, op2) =>
                {
                    return EmitSoftFloatCall(context, nameof(SoftFloat32.FPMaxNum), op1, op2);
                });
            }
        }

        public static void Fmaxnmp_V(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitVectorBinaryOpF(context, Intrinsic.Arm64FmaxnmpV);
            }
            else if (Optimizations.FastFP && Optimizations.UseSse41)
            {
                EmitSse2VectorPairwiseOpF(context, (op1, op2) =>
                {
                    return EmitSse41MaxMinNumOpF(context, isMaxNum: true, scalar: false, op1, op2);
                });
            }
            else
            {
                EmitVectorPairwiseOpF(context, (op1, op2) =>
                {
                    return EmitSoftFloatCall(context, nameof(SoftFloat32.FPMaxNum), op1, op2);
                });
            }
        }

        public static void Fmaxnmv_V(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitVectorUnaryOpF(context, Intrinsic.Arm64FmaxnmvV);
            }
            else if (Optimizations.FastFP && Optimizations.UseSse41)
            {
                EmitSse2VectorAcrossVectorOpF(context, (op1, op2) =>
                {
                    return EmitSse41MaxMinNumOpF(context, isMaxNum: true, scalar: false, op1, op2);
                });
            }
            else
            {
                EmitVectorAcrossVectorOpF(context, (op1, op2) =>
                {
                    return EmitSoftFloatCall(context, nameof(SoftFloat32.FPMaxNum), op1, op2);
                });
            }
        }

        public static void Fmaxp_S(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitScalarUnaryOpF(context, Intrinsic.Arm64FmaxpS);
            }
            else if (Optimizations.FastFP && Optimizations.UseSse41)
            {
                EmitSse2ScalarPairwiseOpF(context, (op1, op2) =>
                {
                    return EmitSse41ProcessNaNsOpF(context, (op1, op2) =>
                    {
                        return EmitSse2VectorMaxMinOpF(context, op1, op2, isMax: true);
                    }, scalar: true, op1, op2);
                });
            }
            else
            {
                EmitScalarPairwiseOpF(context, (op1, op2) =>
                {
                    return EmitSoftFloatCall(context, nameof(SoftFloat32.FPMax), op1, op2);
                });
            }
        }

        public static void Fmaxp_V(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitVectorBinaryOpF(context, Intrinsic.Arm64FmaxpV);
            }
            else if (Optimizations.FastFP && Optimizations.UseSse41)
            {
                EmitSse2VectorPairwiseOpF(context, (op1, op2) =>
                {
                    return EmitSse41ProcessNaNsOpF(context, (op1, op2) =>
                    {
                        return EmitSse2VectorMaxMinOpF(context, op1, op2, isMax: true);
                    }, scalar: false, op1, op2);
                });
            }
            else
            {
                EmitVectorPairwiseOpF(context, (op1, op2) =>
                {
                    return EmitSoftFloatCall(context, nameof(SoftFloat32.FPMax), op1, op2);
                });
            }
        }

        public static void Fmaxv_V(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitVectorUnaryOpF(context, Intrinsic.Arm64FmaxvV);
            }
            else if (Optimizations.FastFP && Optimizations.UseSse41)
            {
                EmitSse2VectorAcrossVectorOpF(context, (op1, op2) =>
                {
                    return EmitSse41ProcessNaNsOpF(context, (op1, op2) =>
                    {
                        return EmitSse2VectorMaxMinOpF(context, op1, op2, isMax: true);
                    }, scalar: false, op1, op2);
                });
            }
            else
            {
                EmitVectorAcrossVectorOpF(context, (op1, op2) =>
                {
                    return EmitSoftFloatCall(context, nameof(SoftFloat32.FPMax), op1, op2);
                });
            }
        }

        public static void Fmin_S(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitScalarBinaryOpF(context, Intrinsic.Arm64FminS);
            }
            else if (Optimizations.FastFP && Optimizations.UseSse41)
            {
                EmitSse41ProcessNaNsOpF(context, (op1, op2) =>
                {
                    return EmitSse2VectorMaxMinOpF(context, op1, op2, isMax: false);
                }, scalar: true);
            }
            else
            {
                EmitScalarBinaryOpF(context, (op1, op2) =>
                {
                    return EmitSoftFloatCall(context, nameof(SoftFloat32.FPMin), op1, op2);
                });
            }
        }

        public static void Fmin_V(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitVectorBinaryOpF(context, Intrinsic.Arm64FminV);
            }
            else if (Optimizations.FastFP && Optimizations.UseSse41)
            {
                EmitSse41ProcessNaNsOpF(context, (op1, op2) =>
                {
                    return EmitSse2VectorMaxMinOpF(context, op1, op2, isMax: false);
                }, scalar: false);
            }
            else
            {
                EmitVectorBinaryOpF(context, (op1, op2) =>
                {
                    return EmitSoftFloatCall(context, nameof(SoftFloat32.FPMin), op1, op2);
                });
            }
        }

        public static void Fminnm_S(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitScalarBinaryOpF(context, Intrinsic.Arm64FminnmS);
            }
            else if (Optimizations.FastFP && Optimizations.UseSse41)
            {
                EmitSse41MaxMinNumOpF(context, isMaxNum: false, scalar: true);
            }
            else
            {
                EmitScalarBinaryOpF(context, (op1, op2) =>
                {
                    return EmitSoftFloatCall(context, nameof(SoftFloat32.FPMinNum), op1, op2);
                });
            }
        }

        public static void Fminnm_V(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitVectorBinaryOpF(context, Intrinsic.Arm64FminnmV);
            }
            else if (Optimizations.FastFP && Optimizations.UseSse41)
            {
                EmitSse41MaxMinNumOpF(context, isMaxNum: false, scalar: false);
            }
            else
            {
                EmitVectorBinaryOpF(context, (op1, op2) =>
                {
                    return EmitSoftFloatCall(context, nameof(SoftFloat32.FPMinNum), op1, op2);
                });
            }
        }

        public static void Fminnmp_S(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitScalarUnaryOpF(context, Intrinsic.Arm64FminnmpS);
            }
            else if (Optimizations.FastFP && Optimizations.UseSse41)
            {
                EmitSse2ScalarPairwiseOpF(context, (op1, op2) =>
                {
                    return EmitSse41MaxMinNumOpF(context, isMaxNum: false, scalar: true, op1, op2);
                });
            }
            else
            {
                EmitScalarPairwiseOpF(context, (op1, op2) =>
                {
                    return EmitSoftFloatCall(context, nameof(SoftFloat32.FPMinNum), op1, op2);
                });
            }
        }

        public static void Fminnmp_V(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitVectorBinaryOpF(context, Intrinsic.Arm64FminnmpV);
            }
            else if (Optimizations.FastFP && Optimizations.UseSse41)
            {
                EmitSse2VectorPairwiseOpF(context, (op1, op2) =>
                {
                    return EmitSse41MaxMinNumOpF(context, isMaxNum: false, scalar: false, op1, op2);
                });
            }
            else
            {
                EmitVectorPairwiseOpF(context, (op1, op2) =>
                {
                    return EmitSoftFloatCall(context, nameof(SoftFloat32.FPMinNum), op1, op2);
                });
            }
        }

        public static void Fminnmv_V(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitVectorUnaryOpF(context, Intrinsic.Arm64FminnmvV);
            }
            else if (Optimizations.FastFP && Optimizations.UseSse41)
            {
                EmitSse2VectorAcrossVectorOpF(context, (op1, op2) =>
                {
                    return EmitSse41MaxMinNumOpF(context, isMaxNum: false, scalar: false, op1, op2);
                });
            }
            else
            {
                EmitVectorAcrossVectorOpF(context, (op1, op2) =>
                {
                    return EmitSoftFloatCall(context, nameof(SoftFloat32.FPMinNum), op1, op2);
                });
            }
        }

        public static void Fminp_S(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitScalarUnaryOpF(context, Intrinsic.Arm64FminpS);
            }
            else if (Optimizations.FastFP && Optimizations.UseSse41)
            {
                EmitSse2ScalarPairwiseOpF(context, (op1, op2) =>
                {
                    return EmitSse41ProcessNaNsOpF(context, (op1, op2) =>
                    {
                        return EmitSse2VectorMaxMinOpF(context, op1, op2, isMax: false);
                    }, scalar: true, op1, op2);
                });
            }
            else
            {
                EmitScalarPairwiseOpF(context, (op1, op2) =>
                {
                    return EmitSoftFloatCall(context, nameof(SoftFloat32.FPMin), op1, op2);
                });
            }
        }

        public static void Fminp_V(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitVectorBinaryOpF(context, Intrinsic.Arm64FminpV);
            }
            else if (Optimizations.FastFP && Optimizations.UseSse41)
            {
                EmitSse2VectorPairwiseOpF(context, (op1, op2) =>
                {
                    return EmitSse41ProcessNaNsOpF(context, (op1, op2) =>
                    {
                        return EmitSse2VectorMaxMinOpF(context, op1, op2, isMax: false);
                    }, scalar: false, op1, op2);
                });
            }
            else
            {
                EmitVectorPairwiseOpF(context, (op1, op2) =>
                {
                    return EmitSoftFloatCall(context, nameof(SoftFloat32.FPMin), op1, op2);
                });
            }
        }

        public static void Fminv_V(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitVectorUnaryOpF(context, Intrinsic.Arm64FminvV);
            }
            else if (Optimizations.FastFP && Optimizations.UseSse41)
            {
                EmitSse2VectorAcrossVectorOpF(context, (op1, op2) =>
                {
                    return EmitSse41ProcessNaNsOpF(context, (op1, op2) =>
                    {
                        return EmitSse2VectorMaxMinOpF(context, op1, op2, isMax: false);
                    }, scalar: false, op1, op2);
                });
            }
            else
            {
                EmitVectorAcrossVectorOpF(context, (op1, op2) =>
                {
                    return EmitSoftFloatCall(context, nameof(SoftFloat32.FPMin), op1, op2);
                });
            }
        }

        public static void Fmla_Se(ArmEmitterContext context) // Fused.
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitScalarTernaryOpFRdByElem(context, Intrinsic.Arm64FmlaSe);
            }
            else if (Optimizations.UseFma)
            {
                OpCodeSimdRegElemF op = (OpCodeSimdRegElemF)context.CurrOp;

                Operand d = GetVec(op.Rd);
                Operand n = GetVec(op.Rn);
                Operand m = GetVec(op.Rm);

                int sizeF = op.Size & 1;

                if (sizeF == 0)
                {
                    int shuffleMask = op.Index | op.Index << 2 | op.Index << 4 | op.Index << 6;

                    Operand res = context.AddIntrinsic(Intrinsic.X86Shufps, m, m, Const(shuffleMask));

                    res = context.AddIntrinsic(Intrinsic.X86Vfmadd231ss, d, n, res);

                    context.Copy(d, context.VectorZeroUpper96(res));
                }
                else /* if (sizeF == 1) */
                {
                    int shuffleMask = op.Index | op.Index << 1;

                    Operand res = context.AddIntrinsic(Intrinsic.X86Shufpd, m, m, Const(shuffleMask));

                    res = context.AddIntrinsic(Intrinsic.X86Vfmadd231sd, d, n, res);

                    context.Copy(d, context.VectorZeroUpper64(res));
                }
            }
            else
            {
                EmitScalarTernaryOpByElemF(context, (op1, op2, op3) =>
                {
                    return context.Add(op1, context.Multiply(op2, op3));
                });
            }
        }

        public static void Fmla_V(ArmEmitterContext context) // Fused.
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitVectorTernaryOpFRd(context, Intrinsic.Arm64FmlaV);
            }
            else if (Optimizations.FastFP && Optimizations.UseSse2)
            {
                OpCodeSimdReg op = (OpCodeSimdReg)context.CurrOp;

                Operand d = GetVec(op.Rd);
                Operand n = GetVec(op.Rn);
                Operand m = GetVec(op.Rm);

                int sizeF = op.Size & 1;

                Operand res;

                if (sizeF == 0)
                {
                    if (Optimizations.UseFma)
                    {
                        res = context.AddIntrinsic(Intrinsic.X86Vfmadd231ps, d, n, m);
                    }
                    else
                    {
                        res = context.AddIntrinsic(Intrinsic.X86Mulps, n, m);
                        res = context.AddIntrinsic(Intrinsic.X86Addps, d, res);
                    }

                    if (op.RegisterSize == RegisterSize.Simd64)
                    {
                        res = context.VectorZeroUpper64(res);
                    }

                    context.Copy(d, res);
                }
                else /* if (sizeF == 1) */
                {
                    if (Optimizations.UseFma)
                    {
                        res = context.AddIntrinsic(Intrinsic.X86Vfmadd231pd, d, n, m);
                    }
                    else
                    {
                        res = context.AddIntrinsic(Intrinsic.X86Mulpd, n, m);
                        res = context.AddIntrinsic(Intrinsic.X86Addpd, d, res);
                    }

                    context.Copy(d, res);
                }
            }
            else
            {
                EmitVectorTernaryOpF(context, (op1, op2, op3) =>
                {
                    return EmitSoftFloatCall(context, nameof(SoftFloat32.FPMulAdd), op1, op2, op3);
                });
            }
        }

        public static void Fmla_Ve(ArmEmitterContext context) // Fused.
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitVectorTernaryOpFRdByElem(context, Intrinsic.Arm64FmlaVe);
            }
            else if (Optimizations.FastFP && Optimizations.UseSse2)
            {
                OpCodeSimdRegElemF op = (OpCodeSimdRegElemF)context.CurrOp;

                Operand d = GetVec(op.Rd);
                Operand n = GetVec(op.Rn);
                Operand m = GetVec(op.Rm);

                int sizeF = op.Size & 1;

                if (sizeF == 0)
                {
                    int shuffleMask = op.Index | op.Index << 2 | op.Index << 4 | op.Index << 6;

                    Operand res = context.AddIntrinsic(Intrinsic.X86Shufps, m, m, Const(shuffleMask));

                    if (Optimizations.UseFma)
                    {
                        res = context.AddIntrinsic(Intrinsic.X86Vfmadd231ps, d, n, res);
                    }
                    else
                    {
                        res = context.AddIntrinsic(Intrinsic.X86Mulps, n, res);
                        res = context.AddIntrinsic(Intrinsic.X86Addps, d, res);
                    }

                    if (op.RegisterSize == RegisterSize.Simd64)
                    {
                        res = context.VectorZeroUpper64(res);
                    }

                    context.Copy(d, res);
                }
                else /* if (sizeF == 1) */
                {
                    int shuffleMask = op.Index | op.Index << 1;

                    Operand res = context.AddIntrinsic(Intrinsic.X86Shufpd, m, m, Const(shuffleMask));

                    if (Optimizations.UseFma)
                    {
                        res = context.AddIntrinsic(Intrinsic.X86Vfmadd231pd, d, n, res);
                    }
                    else
                    {
                        res = context.AddIntrinsic(Intrinsic.X86Mulpd, n, res);
                        res = context.AddIntrinsic(Intrinsic.X86Addpd, d, res);
                    }

                    context.Copy(d, res);
                }
            }
            else
            {
                EmitVectorTernaryOpByElemF(context, (op1, op2, op3) =>
                {
                    return EmitSoftFloatCall(context, nameof(SoftFloat32.FPMulAdd), op1, op2, op3);
                });
            }
        }

        public static void Fmls_Se(ArmEmitterContext context) // Fused.
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitScalarTernaryOpFRdByElem(context, Intrinsic.Arm64FmlsSe);
            }
            else if (Optimizations.UseFma)
            {
                OpCodeSimdRegElemF op = (OpCodeSimdRegElemF)context.CurrOp;

                Operand d = GetVec(op.Rd);
                Operand n = GetVec(op.Rn);
                Operand m = GetVec(op.Rm);

                int sizeF = op.Size & 1;

                if (sizeF == 0)
                {
                    int shuffleMask = op.Index | op.Index << 2 | op.Index << 4 | op.Index << 6;

                    Operand res = context.AddIntrinsic(Intrinsic.X86Shufps, m, m, Const(shuffleMask));

                    res = context.AddIntrinsic(Intrinsic.X86Vfnmadd231ss, d, n, res);

                    context.Copy(d, context.VectorZeroUpper96(res));
                }
                else /* if (sizeF == 1) */
                {
                    int shuffleMask = op.Index | op.Index << 1;

                    Operand res = context.AddIntrinsic(Intrinsic.X86Shufpd, m, m, Const(shuffleMask));

                    res = context.AddIntrinsic(Intrinsic.X86Vfnmadd231sd, d, n, res);

                    context.Copy(d, context.VectorZeroUpper64(res));
                }
            }
            else
            {
                EmitScalarTernaryOpByElemF(context, (op1, op2, op3) =>
                {
                    return context.Subtract(op1, context.Multiply(op2, op3));
                });
            }
        }

        public static void Fmls_V(ArmEmitterContext context) // Fused.
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitVectorTernaryOpFRd(context, Intrinsic.Arm64FmlsV);
            }
            else if (Optimizations.FastFP && Optimizations.UseSse2)
            {
                OpCodeSimdReg op = (OpCodeSimdReg)context.CurrOp;

                Operand d = GetVec(op.Rd);
                Operand n = GetVec(op.Rn);
                Operand m = GetVec(op.Rm);

                int sizeF = op.Size & 1;

                Operand res;

                if (sizeF == 0)
                {
                    if (Optimizations.UseFma)
                    {
                        res = context.AddIntrinsic(Intrinsic.X86Vfnmadd231ps, d, n, m);
                    }
                    else
                    {
                        res = context.AddIntrinsic(Intrinsic.X86Mulps, n, m);
                        res = context.AddIntrinsic(Intrinsic.X86Subps, d, res);
                    }

                    if (op.RegisterSize == RegisterSize.Simd64)
                    {
                        res = context.VectorZeroUpper64(res);
                    }

                    context.Copy(d, res);
                }
                else /* if (sizeF == 1) */
                {
                    if (Optimizations.UseFma)
                    {
                        res = context.AddIntrinsic(Intrinsic.X86Vfnmadd231pd, d, n, m);
                    }
                    else
                    {
                        res = context.AddIntrinsic(Intrinsic.X86Mulpd, n, m);
                        res = context.AddIntrinsic(Intrinsic.X86Subpd, d, res);
                    }

                    context.Copy(d, res);
                }
            }
            else
            {
                EmitVectorTernaryOpF(context, (op1, op2, op3) =>
                {
                    return EmitSoftFloatCall(context, nameof(SoftFloat32.FPMulSub), op1, op2, op3);
                });
            }
        }

        public static void Fmls_Ve(ArmEmitterContext context) // Fused.
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitVectorTernaryOpFRdByElem(context, Intrinsic.Arm64FmlsVe);
            }
            else if (Optimizations.FastFP && Optimizations.UseSse2)
            {
                OpCodeSimdRegElemF op = (OpCodeSimdRegElemF)context.CurrOp;

                Operand d = GetVec(op.Rd);
                Operand n = GetVec(op.Rn);
                Operand m = GetVec(op.Rm);

                int sizeF = op.Size & 1;

                if (sizeF == 0)
                {
                    int shuffleMask = op.Index | op.Index << 2 | op.Index << 4 | op.Index << 6;

                    Operand res = context.AddIntrinsic(Intrinsic.X86Shufps, m, m, Const(shuffleMask));

                    if (Optimizations.UseFma)
                    {
                        res = context.AddIntrinsic(Intrinsic.X86Vfnmadd231ps, d, n, res);
                    }
                    else
                    {
                        res = context.AddIntrinsic(Intrinsic.X86Mulps, n, res);
                        res = context.AddIntrinsic(Intrinsic.X86Subps, d, res);
                    }

                    if (op.RegisterSize == RegisterSize.Simd64)
                    {
                        res = context.VectorZeroUpper64(res);
                    }

                    context.Copy(d, res);
                }
                else /* if (sizeF == 1) */
                {
                    int shuffleMask = op.Index | op.Index << 1;

                    Operand res = context.AddIntrinsic(Intrinsic.X86Shufpd, m, m, Const(shuffleMask));

                    if (Optimizations.UseFma)
                    {
                        res = context.AddIntrinsic(Intrinsic.X86Vfnmadd231pd, d, n, res);
                    }
                    else
                    {
                        res = context.AddIntrinsic(Intrinsic.X86Mulpd, n, res);
                        res = context.AddIntrinsic(Intrinsic.X86Subpd, d, res);
                    }

                    context.Copy(d, res);
                }
            }
            else
            {
                EmitVectorTernaryOpByElemF(context, (op1, op2, op3) =>
                {
                    return EmitSoftFloatCall(context, nameof(SoftFloat32.FPMulSub), op1, op2, op3);
                });
            }
        }

        public static void Fmsub_S(ArmEmitterContext context) // Fused.
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitScalarTernaryOpF(context, Intrinsic.Arm64FmsubS);
            }
            else if (Optimizations.FastFP && Optimizations.UseSse2)
            {
                OpCodeSimdReg op = (OpCodeSimdReg)context.CurrOp;

                Operand d = GetVec(op.Rd);
                Operand a = GetVec(op.Ra);
                Operand n = GetVec(op.Rn);
                Operand m = GetVec(op.Rm);

                Operand res;

                if (op.Size == 0)
                {
                    if (Optimizations.UseFma)
                    {
                        res = context.AddIntrinsic(Intrinsic.X86Vfnmadd231ss, a, n, m);
                    }
                    else
                    {
                        res = context.AddIntrinsic(Intrinsic.X86Mulss, n, m);
                        res = context.AddIntrinsic(Intrinsic.X86Subss, a, res);
                    }

                    context.Copy(d, context.VectorZeroUpper96(res));
                }
                else /* if (op.Size == 1) */
                {
                    if (Optimizations.UseFma)
                    {
                        res = context.AddIntrinsic(Intrinsic.X86Vfnmadd231sd, a, n, m);
                    }
                    else
                    {
                        res = context.AddIntrinsic(Intrinsic.X86Mulsd, n, m);
                        res = context.AddIntrinsic(Intrinsic.X86Subsd, a, res);
                    }

                    context.Copy(d, context.VectorZeroUpper64(res));
                }
            }
            else
            {
                EmitScalarTernaryRaOpF(context, (op1, op2, op3) =>
                {
                    return EmitSoftFloatCall(context, nameof(SoftFloat32.FPMulSub), op1, op2, op3);
                });
            }
        }

        public static void Fmul_S(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitScalarBinaryOpF(context, Intrinsic.Arm64FmulS);
            }
            else if (Optimizations.FastFP && Optimizations.UseSse2)
            {
                EmitScalarBinaryOpF(context, Intrinsic.X86Mulss, Intrinsic.X86Mulsd);
            }
            else if (Optimizations.FastFP)
            {
                EmitScalarBinaryOpF(context, (op1, op2) => context.Multiply(op1, op2));
            }
            else
            {
                EmitScalarBinaryOpF(context, (op1, op2) =>
                {
                    return EmitSoftFloatCall(context, nameof(SoftFloat32.FPMul), op1, op2);
                });
            }
        }

        public static void Fmul_Se(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitScalarBinaryOpFByElem(context, Intrinsic.Arm64FmulSe);
            }
            else
            {
                EmitScalarBinaryOpByElemF(context, (op1, op2) => context.Multiply(op1, op2));
            }
        }

        public static void Fmul_V(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitVectorBinaryOpF(context, Intrinsic.Arm64FmulV);
            }
            else if (Optimizations.FastFP && Optimizations.UseSse2)
            {
                EmitVectorBinaryOpF(context, Intrinsic.X86Mulps, Intrinsic.X86Mulpd);
            }
            else if (Optimizations.FastFP)
            {
                EmitVectorBinaryOpF(context, (op1, op2) => context.Multiply(op1, op2));
            }
            else
            {
                EmitVectorBinaryOpF(context, (op1, op2) =>
                {
                    return EmitSoftFloatCall(context, nameof(SoftFloat32.FPMul), op1, op2);
                });
            }
        }

        public static void Fmul_Ve(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitVectorBinaryOpFByElem(context, Intrinsic.Arm64FmulVe);
            }
            else if (Optimizations.FastFP && Optimizations.UseSse2)
            {
                OpCodeSimdRegElemF op = (OpCodeSimdRegElemF)context.CurrOp;

                Operand n = GetVec(op.Rn);
                Operand m = GetVec(op.Rm);

                int sizeF = op.Size & 1;

                if (sizeF == 0)
                {
                    int shuffleMask = op.Index | op.Index << 2 | op.Index << 4 | op.Index << 6;

                    Operand res = context.AddIntrinsic(Intrinsic.X86Shufps, m, m, Const(shuffleMask));

                    res = context.AddIntrinsic(Intrinsic.X86Mulps, n, res);

                    if (op.RegisterSize == RegisterSize.Simd64)
                    {
                        res = context.VectorZeroUpper64(res);
                    }

                    context.Copy(GetVec(op.Rd), res);
                }
                else /* if (sizeF == 1) */
                {
                    int shuffleMask = op.Index | op.Index << 1;

                    Operand res = context.AddIntrinsic(Intrinsic.X86Shufpd, m, m, Const(shuffleMask));

                    res = context.AddIntrinsic(Intrinsic.X86Mulpd, n, res);

                    context.Copy(GetVec(op.Rd), res);
                }
            }
            else if (Optimizations.FastFP)
            {
                EmitVectorBinaryOpByElemF(context, (op1, op2) => context.Multiply(op1, op2));
            }
            else
            {
                EmitVectorBinaryOpByElemF(context, (op1, op2) =>
                {
                    return EmitSoftFloatCall(context, nameof(SoftFloat32.FPMul), op1, op2);
                });
            }
        }

        public static void Fmulx_S(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitScalarBinaryOpF(context, Intrinsic.Arm64FmulxS);
            }
            else
            {
                EmitScalarBinaryOpF(context, (op1, op2) =>
                {
                    return EmitSoftFloatCall(context, nameof(SoftFloat32.FPMulX), op1, op2);
                });
            }
        }

        public static void Fmulx_Se(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitScalarBinaryOpFByElem(context, Intrinsic.Arm64FmulxSe);
            }
            else
            {
                EmitScalarBinaryOpByElemF(context, (op1, op2) =>
                {
                    return EmitSoftFloatCall(context, nameof(SoftFloat32.FPMulX), op1, op2);
                });
            }
        }

        public static void Fmulx_V(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitVectorBinaryOpF(context, Intrinsic.Arm64FmulxV);
            }
            else
            {
                EmitVectorBinaryOpF(context, (op1, op2) =>
                {
                    return EmitSoftFloatCall(context, nameof(SoftFloat32.FPMulX), op1, op2);
                });
            }
        }

        public static void Fmulx_Ve(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitVectorBinaryOpFByElem(context, Intrinsic.Arm64FmulxVe);
            }
            else
            {
                EmitVectorBinaryOpByElemF(context, (op1, op2) =>
                {
                    return EmitSoftFloatCall(context, nameof(SoftFloat32.FPMulX), op1, op2);
                });
            }
        }

        public static void Fneg_S(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitScalarUnaryOpF(context, Intrinsic.Arm64FnegS);
            }
            else if (Optimizations.UseSse2)
            {
                OpCodeSimd op = (OpCodeSimd)context.CurrOp;

                if (op.Size == 0)
                {
                    Operand mask = X86GetScalar(context, -0f);

                    Operand res = context.AddIntrinsic(Intrinsic.X86Xorps, mask, GetVec(op.Rn));

                    context.Copy(GetVec(op.Rd), context.VectorZeroUpper96(res));
                }
                else /* if (op.Size == 1) */
                {
                    Operand mask = X86GetScalar(context, -0d);

                    Operand res = context.AddIntrinsic(Intrinsic.X86Xorpd, mask, GetVec(op.Rn));

                    context.Copy(GetVec(op.Rd), context.VectorZeroUpper64(res));
                }
            }
            else
            {
                EmitScalarUnaryOpF(context, (op1) => context.Negate(op1));
            }
        }

        public static void Fneg_V(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitVectorUnaryOpF(context, Intrinsic.Arm64FnegV);
            }
            else if (Optimizations.UseSse2)
            {
                OpCodeSimd op = (OpCodeSimd)context.CurrOp;

                int sizeF = op.Size & 1;

                if (sizeF == 0)
                {
                    Operand mask = X86GetAllElements(context, -0f);

                    Operand res = context.AddIntrinsic(Intrinsic.X86Xorps, mask, GetVec(op.Rn));

                    if (op.RegisterSize == RegisterSize.Simd64)
                    {
                        res = context.VectorZeroUpper64(res);
                    }

                    context.Copy(GetVec(op.Rd), res);
                }
                else /* if (sizeF == 1) */
                {
                    Operand mask = X86GetAllElements(context, -0d);

                    Operand res = context.AddIntrinsic(Intrinsic.X86Xorpd, mask, GetVec(op.Rn));

                    context.Copy(GetVec(op.Rd), res);
                }
            }
            else
            {
                EmitVectorUnaryOpF(context, (op1) => context.Negate(op1));
            }
        }

        public static void Fnmadd_S(ArmEmitterContext context) // Fused.
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitScalarTernaryOpF(context, Intrinsic.Arm64FnmaddS);
            }
            else if (Optimizations.FastFP && Optimizations.UseSse2)
            {
                OpCodeSimdReg op = (OpCodeSimdReg)context.CurrOp;

                Operand d = GetVec(op.Rd);
                Operand a = GetVec(op.Ra);
                Operand n = GetVec(op.Rn);
                Operand m = GetVec(op.Rm);

                Operand res;

                if (op.Size == 0)
                {
                    if (Optimizations.UseFma)
                    {
                        res = context.AddIntrinsic(Intrinsic.X86Vfnmsub231ss, a, n, m);
                    }
                    else
                    {
                        Operand mask = X86GetScalar(context, -0f);
                        Operand aNeg = context.AddIntrinsic(Intrinsic.X86Xorps, mask, a);

                        res = context.AddIntrinsic(Intrinsic.X86Mulss, n, m);
                        res = context.AddIntrinsic(Intrinsic.X86Subss, aNeg, res);
                    }

                    context.Copy(d, context.VectorZeroUpper96(res));
                }
                else /* if (op.Size == 1) */
                {
                    if (Optimizations.UseFma)
                    {
                        res = context.AddIntrinsic(Intrinsic.X86Vfnmsub231sd, a, n, m);
                    }
                    else
                    {
                        Operand mask = X86GetScalar(context, -0d);
                        Operand aNeg = context.AddIntrinsic(Intrinsic.X86Xorpd, mask, a);

                        res = context.AddIntrinsic(Intrinsic.X86Mulsd, n, m);
                        res = context.AddIntrinsic(Intrinsic.X86Subsd, aNeg, res);
                    }

                    context.Copy(d, context.VectorZeroUpper64(res));
                }
            }
            else
            {
                EmitScalarTernaryRaOpF(context, (op1, op2, op3) =>
                {
                    return EmitSoftFloatCall(context, nameof(SoftFloat32.FPNegMulAdd), op1, op2, op3);
                });
            }
        }

        public static void Fnmsub_S(ArmEmitterContext context) // Fused.
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitScalarTernaryOpF(context, Intrinsic.Arm64FnmsubS);
            }
            else if (Optimizations.FastFP && Optimizations.UseSse2)
            {
                OpCodeSimdReg op = (OpCodeSimdReg)context.CurrOp;

                Operand d = GetVec(op.Rd);
                Operand a = GetVec(op.Ra);
                Operand n = GetVec(op.Rn);
                Operand m = GetVec(op.Rm);

                Operand res;

                if (op.Size == 0)
                {
                    if (Optimizations.UseFma)
                    {
                        res = context.AddIntrinsic(Intrinsic.X86Vfmsub231ss, a, n, m);
                    }
                    else
                    {
                        Operand mask = X86GetScalar(context, -0f);
                        Operand aNeg = context.AddIntrinsic(Intrinsic.X86Xorps, mask, a);

                        res = context.AddIntrinsic(Intrinsic.X86Mulss, n, m);
                        res = context.AddIntrinsic(Intrinsic.X86Addss, aNeg, res);
                    }

                    context.Copy(d, context.VectorZeroUpper96(res));
                }
                else /* if (op.Size == 1) */
                {
                    if (Optimizations.UseFma)
                    {
                        res = context.AddIntrinsic(Intrinsic.X86Vfmsub231sd, a, n, m);
                    }
                    else
                    {
                        Operand mask = X86GetScalar(context, -0d);
                        Operand aNeg = context.AddIntrinsic(Intrinsic.X86Xorpd, mask, a);

                        res = context.AddIntrinsic(Intrinsic.X86Mulsd, n, m);
                        res = context.AddIntrinsic(Intrinsic.X86Addsd, aNeg, res);
                    }

                    context.Copy(d, context.VectorZeroUpper64(res));
                }
            }
            else
            {
                EmitScalarTernaryRaOpF(context, (op1, op2, op3) =>
                {
                    return EmitSoftFloatCall(context, nameof(SoftFloat32.FPNegMulSub), op1, op2, op3);
                });
            }
        }

        public static void Fnmul_S(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitScalarBinaryOpF(context, Intrinsic.Arm64FnmulS);
            }
            else
            {
                EmitScalarBinaryOpF(context, (op1, op2) => context.Negate(context.Multiply(op1, op2)));
            }
        }

        public static void Frecpe_S(ArmEmitterContext context)
        {
            OpCodeSimd op = (OpCodeSimd)context.CurrOp;

            int sizeF = op.Size & 1;

            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitScalarUnaryOpF(context, Intrinsic.Arm64FrecpeS);
            }
            else if (Optimizations.FastFP && Optimizations.UseSse41 && sizeF == 0)
            {
                Operand res = EmitSse41Round32Exp8OpF(context, context.AddIntrinsic(Intrinsic.X86Rcpss, GetVec(op.Rn)), scalar: true);

                context.Copy(GetVec(op.Rd), context.VectorZeroUpper96(res));
            }
            else
            {
                EmitScalarUnaryOpF(context, (op1) =>
                {
                    return EmitSoftFloatCall(context, nameof(SoftFloat32.FPRecipEstimate), op1);
                });
            }
        }

        public static void Frecpe_V(ArmEmitterContext context)
        {
            OpCodeSimd op = (OpCodeSimd)context.CurrOp;

            int sizeF = op.Size & 1;

            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitVectorUnaryOpF(context, Intrinsic.Arm64FrecpeV);
            }
            else if (Optimizations.FastFP && Optimizations.UseSse41 && sizeF == 0)
            {
                Operand res = EmitSse41Round32Exp8OpF(context, context.AddIntrinsic(Intrinsic.X86Rcpps, GetVec(op.Rn)), scalar: false);

                if (op.RegisterSize == RegisterSize.Simd64)
                {
                    res = context.VectorZeroUpper64(res);
                }

                context.Copy(GetVec(op.Rd), res);
            }
            else
            {
                EmitVectorUnaryOpF(context, (op1) =>
                {
                    return EmitSoftFloatCall(context, nameof(SoftFloat32.FPRecipEstimate), op1);
                });
            }
        }

        public static void Frecps_S(ArmEmitterContext context) // Fused.
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitScalarBinaryOpF(context, Intrinsic.Arm64FrecpsS);
            }
            else if (Optimizations.FastFP && Optimizations.UseSse41)
            {
                OpCodeSimdReg op = (OpCodeSimdReg)context.CurrOp;

                Operand n = GetVec(op.Rn);
                Operand m = GetVec(op.Rm);

                int sizeF = op.Size & 1;

                Operand res;

                if (sizeF == 0)
                {
                    Operand mask = X86GetScalar(context, 2f);

                    if (Optimizations.UseFma)
                    {
                        res = context.AddIntrinsic(Intrinsic.X86Vfnmadd231ss, mask, n, m);
                    }
                    else
                    {
                        res = context.AddIntrinsic(Intrinsic.X86Mulss, n, m);
                        res = context.AddIntrinsic(Intrinsic.X86Subss, mask, res);
                    }

                    res = EmitSse41RecipStepSelectOpF(context, n, m, res, mask, scalar: true, sizeF);

                    context.Copy(GetVec(op.Rd), context.VectorZeroUpper96(res));
                }
                else /* if (sizeF == 1) */
                {
                    Operand mask = X86GetScalar(context, 2d);

                    if (Optimizations.UseFma)
                    {
                        res = context.AddIntrinsic(Intrinsic.X86Vfnmadd231sd, mask, n, m);
                    }
                    else
                    {
                        res = context.AddIntrinsic(Intrinsic.X86Mulsd, n, m);
                        res = context.AddIntrinsic(Intrinsic.X86Subsd, mask, res);
                    }

                    res = EmitSse41RecipStepSelectOpF(context, n, m, res, mask, scalar: true, sizeF);

                    context.Copy(GetVec(op.Rd), context.VectorZeroUpper64(res));
                }
            }
            else
            {
                EmitScalarBinaryOpF(context, (op1, op2) =>
                {
                    return EmitSoftFloatCall(context, nameof(SoftFloat32.FPRecipStepFused), op1, op2);
                });
            }
        }

        public static void Frecps_V(ArmEmitterContext context) // Fused.
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitVectorBinaryOpF(context, Intrinsic.Arm64FrecpsV);
            }
            else if (Optimizations.FastFP && Optimizations.UseSse41)
            {
                OpCodeSimdReg op = (OpCodeSimdReg)context.CurrOp;

                Operand n = GetVec(op.Rn);
                Operand m = GetVec(op.Rm);

                int sizeF = op.Size & 1;

                Operand res;

                if (sizeF == 0)
                {
                    Operand mask = X86GetAllElements(context, 2f);

                    if (Optimizations.UseFma)
                    {
                        res = context.AddIntrinsic(Intrinsic.X86Vfnmadd231ps, mask, n, m);
                    }
                    else
                    {
                        res = context.AddIntrinsic(Intrinsic.X86Mulps, n, m);
                        res = context.AddIntrinsic(Intrinsic.X86Subps, mask, res);
                    }

                    res = EmitSse41RecipStepSelectOpF(context, n, m, res, mask, scalar: false, sizeF);

                    if (op.RegisterSize == RegisterSize.Simd64)
                    {
                        res = context.VectorZeroUpper64(res);
                    }

                    context.Copy(GetVec(op.Rd), res);
                }
                else /* if (sizeF == 1) */
                {
                    Operand mask = X86GetAllElements(context, 2d);

                    if (Optimizations.UseFma)
                    {
                        res = context.AddIntrinsic(Intrinsic.X86Vfnmadd231pd, mask, n, m);
                    }
                    else
                    {
                        res = context.AddIntrinsic(Intrinsic.X86Mulpd, n, m);
                        res = context.AddIntrinsic(Intrinsic.X86Subpd, mask, res);
                    }

                    res = EmitSse41RecipStepSelectOpF(context, n, m, res, mask, scalar: false, sizeF);

                    context.Copy(GetVec(op.Rd), res);
                }
            }
            else
            {
                EmitVectorBinaryOpF(context, (op1, op2) =>
                {
                    return EmitSoftFloatCall(context, nameof(SoftFloat32.FPRecipStepFused), op1, op2);
                });
            }
        }

        public static void Frecpx_S(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitScalarBinaryOpF(context, Intrinsic.Arm64FrecpxS);
            }
            else
            {
                EmitScalarUnaryOpF(context, (op1) =>
                {
                    return EmitSoftFloatCall(context, nameof(SoftFloat32.FPRecpX), op1);
                });
            }
        }

        public static void Frinta_S(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitScalarUnaryOpF(context, Intrinsic.Arm64FrintaS);
            }
            else if (Optimizations.UseSse41)
            {
                EmitSse41ScalarRoundOpF(context, FPRoundingMode.ToNearestAway);
            }
            else
            {
                EmitScalarUnaryOpF(context, (op1) =>
                {
                    return EmitRoundMathCall(context, MidpointRounding.AwayFromZero, op1);
                });
            }
        }

        public static void Frinta_V(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitVectorUnaryOpF(context, Intrinsic.Arm64FrintaV);
            }
            else if (Optimizations.UseSse41)
            {
                EmitSse41VectorRoundOpF(context, FPRoundingMode.ToNearestAway);
            }
            else
            {
                EmitVectorUnaryOpF(context, (op1) =>
                {
                    return EmitRoundMathCall(context, MidpointRounding.AwayFromZero, op1);
                });
            }
        }

        public static void Frinti_S(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitScalarUnaryOpF(context, Intrinsic.Arm64FrintiS);
            }
            else
            {
                EmitScalarUnaryOpF(context, (op1) =>
                {
                    return EmitRoundByRMode(context, op1);
                });
            }
        }

        public static void Frinti_V(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitVectorUnaryOpF(context, Intrinsic.Arm64FrintiV);
            }
            else
            {
                EmitVectorUnaryOpF(context, (op1) =>
                {
                    return EmitRoundByRMode(context, op1);
                });
            }
        }

        public static void Frintm_S(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitScalarUnaryOpF(context, Intrinsic.Arm64FrintmS);
            }
            else if (Optimizations.UseSse41)
            {
                EmitSse41ScalarRoundOpF(context, FPRoundingMode.TowardsMinusInfinity);
            }
            else
            {
                EmitScalarUnaryOpF(context, (op1) =>
                {
                    return EmitUnaryMathCall(context, nameof(MathHelper.Floor), op1);
                });
            }
        }

        public static void Frintm_V(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitVectorUnaryOpF(context, Intrinsic.Arm64FrintmV);
            }
            else if (Optimizations.UseSse41)
            {
                EmitSse41VectorRoundOpF(context, FPRoundingMode.TowardsMinusInfinity);
            }
            else
            {
                EmitVectorUnaryOpF(context, (op1) =>
                {
                    return EmitUnaryMathCall(context, nameof(MathHelper.Floor), op1);
                });
            }
        }

        public static void Frintn_S(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitScalarUnaryOpF(context, Intrinsic.Arm64FrintnS);
            }
            else if (Optimizations.UseSse41)
            {
                EmitSse41ScalarRoundOpF(context, FPRoundingMode.ToNearest);
            }
            else
            {
                EmitScalarUnaryOpF(context, (op1) =>
                {
                    return EmitRoundMathCall(context, MidpointRounding.ToEven, op1);
                });
            }
        }

        public static void Frintn_V(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitVectorUnaryOpF(context, Intrinsic.Arm64FrintnV);
            }
            else if (Optimizations.UseSse41)
            {
                EmitSse41VectorRoundOpF(context, FPRoundingMode.ToNearest);
            }
            else
            {
                EmitVectorUnaryOpF(context, (op1) =>
                {
                    return EmitRoundMathCall(context, MidpointRounding.ToEven, op1);
                });
            }
        }

        public static void Frintp_S(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitScalarUnaryOpF(context, Intrinsic.Arm64FrintpS);
            }
            else if (Optimizations.UseSse41)
            {
                EmitSse41ScalarRoundOpF(context, FPRoundingMode.TowardsPlusInfinity);
            }
            else
            {
                EmitScalarUnaryOpF(context, (op1) =>
                {
                    return EmitUnaryMathCall(context, nameof(MathHelper.Ceiling), op1);
                });
            }
        }

        public static void Frintp_V(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitVectorUnaryOpF(context, Intrinsic.Arm64FrintpV);
            }
            else if (Optimizations.UseSse41)
            {
                EmitSse41VectorRoundOpF(context, FPRoundingMode.TowardsPlusInfinity);
            }
            else
            {
                EmitVectorUnaryOpF(context, (op1) =>
                {
                    return EmitUnaryMathCall(context, nameof(MathHelper.Ceiling), op1);
                });
            }
        }

        public static void Frintx_S(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitScalarUnaryOpF(context, Intrinsic.Arm64FrintxS);
            }
            else
            {
                EmitScalarUnaryOpF(context, (op1) =>
                {
                    return EmitRoundByRMode(context, op1);
                });
            }
        }

        public static void Frintx_V(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitVectorUnaryOpF(context, Intrinsic.Arm64FrintxV);
            }
            else
            {
                EmitVectorUnaryOpF(context, (op1) =>
                {
                    return EmitRoundByRMode(context, op1);
                });
            }
        }

        public static void Frintz_S(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitScalarUnaryOpF(context, Intrinsic.Arm64FrintzS);
            }
            else if (Optimizations.UseSse41)
            {
                EmitSse41ScalarRoundOpF(context, FPRoundingMode.TowardsZero);
            }
            else
            {
                EmitScalarUnaryOpF(context, (op1) =>
                {
                    return EmitUnaryMathCall(context, nameof(MathHelper.Truncate), op1);
                });
            }
        }

        public static void Frintz_V(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitVectorUnaryOpF(context, Intrinsic.Arm64FrintzV);
            }
            else if (Optimizations.UseSse41)
            {
                EmitSse41VectorRoundOpF(context, FPRoundingMode.TowardsZero);
            }
            else
            {
                EmitVectorUnaryOpF(context, (op1) =>
                {
                    return EmitUnaryMathCall(context, nameof(MathHelper.Truncate), op1);
                });
            }
        }

        public static void Frsqrte_S(ArmEmitterContext context)
        {
            OpCodeSimd op = (OpCodeSimd)context.CurrOp;

            int sizeF = op.Size & 1;

            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitScalarUnaryOpF(context, Intrinsic.Arm64FrsqrteS);
            }
            else if (Optimizations.FastFP && Optimizations.UseSse41 && sizeF == 0)
            {
                // RSQRTSS handles subnormals as zero, which differs from Arm, so we can't use it here.

                Operand res = context.AddIntrinsic(Intrinsic.X86Sqrtss, GetVec(op.Rn));
                res = context.AddIntrinsic(Intrinsic.X86Rcpss, res);
                res = EmitSse41Round32Exp8OpF(context, res, scalar: true);

                context.Copy(GetVec(op.Rd), context.VectorZeroUpper96(res));
            }
            else
            {
                EmitScalarUnaryOpF(context, (op1) =>
                {
                    return EmitSoftFloatCall(context, nameof(SoftFloat32.FPRSqrtEstimate), op1);
                });
            }
        }

        public static void Frsqrte_V(ArmEmitterContext context)
        {
            OpCodeSimd op = (OpCodeSimd)context.CurrOp;

            int sizeF = op.Size & 1;

            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitVectorUnaryOpF(context, Intrinsic.Arm64FrsqrteV);
            }
            else if (Optimizations.FastFP && Optimizations.UseSse41 && sizeF == 0)
            {
                // RSQRTPS handles subnormals as zero, which differs from Arm, so we can't use it here.

                Operand res = context.AddIntrinsic(Intrinsic.X86Sqrtps, GetVec(op.Rn));
                res = context.AddIntrinsic(Intrinsic.X86Rcpps, res);
                res = EmitSse41Round32Exp8OpF(context, res, scalar: false);

                if (op.RegisterSize == RegisterSize.Simd64)
                {
                    res = context.VectorZeroUpper64(res);
                }

                context.Copy(GetVec(op.Rd), res);
            }
            else
            {
                EmitVectorUnaryOpF(context, (op1) =>
                {
                    return EmitSoftFloatCall(context, nameof(SoftFloat32.FPRSqrtEstimate), op1);
                });
            }
        }

        public static void Frsqrts_S(ArmEmitterContext context) // Fused.
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitScalarBinaryOpF(context, Intrinsic.Arm64FrsqrtsS);
            }
            else if (Optimizations.FastFP && Optimizations.UseSse41)
            {
                OpCodeSimdReg op = (OpCodeSimdReg)context.CurrOp;

                Operand n = GetVec(op.Rn);
                Operand m = GetVec(op.Rm);

                int sizeF = op.Size & 1;

                Operand res;

                if (sizeF == 0)
                {
                    Operand maskHalf = X86GetScalar(context, 0.5f);
                    Operand maskThree = X86GetScalar(context, 3f);
                    Operand maskOneHalf = X86GetScalar(context, 1.5f);

                    if (Optimizations.UseFma)
                    {
                        res = context.AddIntrinsic(Intrinsic.X86Vfnmadd231ss, maskThree, n, m);
                    }
                    else
                    {
                        res = context.AddIntrinsic(Intrinsic.X86Mulss, n, m);
                        res = context.AddIntrinsic(Intrinsic.X86Subss, maskThree, res);
                    }

                    res = context.AddIntrinsic(Intrinsic.X86Mulss, maskHalf, res);
                    res = EmitSse41RecipStepSelectOpF(context, n, m, res, maskOneHalf, scalar: true, sizeF);

                    context.Copy(GetVec(op.Rd), context.VectorZeroUpper96(res));
                }
                else /* if (sizeF == 1) */
                {
                    Operand maskHalf = X86GetScalar(context, 0.5d);
                    Operand maskThree = X86GetScalar(context, 3d);
                    Operand maskOneHalf = X86GetScalar(context, 1.5d);

                    if (Optimizations.UseFma)
                    {
                        res = context.AddIntrinsic(Intrinsic.X86Vfnmadd231sd, maskThree, n, m);
                    }
                    else
                    {
                        res = context.AddIntrinsic(Intrinsic.X86Mulsd, n, m);
                        res = context.AddIntrinsic(Intrinsic.X86Subsd, maskThree, res);
                    }

                    res = context.AddIntrinsic(Intrinsic.X86Mulsd, maskHalf, res);
                    res = EmitSse41RecipStepSelectOpF(context, n, m, res, maskOneHalf, scalar: true, sizeF);

                    context.Copy(GetVec(op.Rd), context.VectorZeroUpper64(res));
                }
            }
            else
            {
                EmitScalarBinaryOpF(context, (op1, op2) =>
                {
                    return EmitSoftFloatCall(context, nameof(SoftFloat32.FPRSqrtStepFused), op1, op2);
                });
            }
        }

        public static void Frsqrts_V(ArmEmitterContext context) // Fused.
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitVectorBinaryOpF(context, Intrinsic.Arm64FrsqrtsV);
            }
            else if (Optimizations.FastFP && Optimizations.UseSse41)
            {
                OpCodeSimdReg op = (OpCodeSimdReg)context.CurrOp;

                Operand n = GetVec(op.Rn);
                Operand m = GetVec(op.Rm);

                int sizeF = op.Size & 1;

                Operand res;

                if (sizeF == 0)
                {
                    Operand maskHalf = X86GetAllElements(context, 0.5f);
                    Operand maskThree = X86GetAllElements(context, 3f);
                    Operand maskOneHalf = X86GetAllElements(context, 1.5f);

                    if (Optimizations.UseFma)
                    {
                        res = context.AddIntrinsic(Intrinsic.X86Vfnmadd231ps, maskThree, n, m);
                    }
                    else
                    {
                        res = context.AddIntrinsic(Intrinsic.X86Mulps, n, m);
                        res = context.AddIntrinsic(Intrinsic.X86Subps, maskThree, res);
                    }

                    res = context.AddIntrinsic(Intrinsic.X86Mulps, maskHalf, res);
                    res = EmitSse41RecipStepSelectOpF(context, n, m, res, maskOneHalf, scalar: false, sizeF);

                    if (op.RegisterSize == RegisterSize.Simd64)
                    {
                        res = context.VectorZeroUpper64(res);
                    }

                    context.Copy(GetVec(op.Rd), res);
                }
                else /* if (sizeF == 1) */
                {
                    Operand maskHalf = X86GetAllElements(context, 0.5d);
                    Operand maskThree = X86GetAllElements(context, 3d);
                    Operand maskOneHalf = X86GetAllElements(context, 1.5d);

                    if (Optimizations.UseFma)
                    {
                        res = context.AddIntrinsic(Intrinsic.X86Vfnmadd231pd, maskThree, n, m);
                    }
                    else
                    {
                        res = context.AddIntrinsic(Intrinsic.X86Mulpd, n, m);
                        res = context.AddIntrinsic(Intrinsic.X86Subpd, maskThree, res);
                    }

                    res = context.AddIntrinsic(Intrinsic.X86Mulpd, maskHalf, res);
                    res = EmitSse41RecipStepSelectOpF(context, n, m, res, maskOneHalf, scalar: false, sizeF);

                    context.Copy(GetVec(op.Rd), res);
                }
            }
            else
            {
                EmitVectorBinaryOpF(context, (op1, op2) =>
                {
                    return EmitSoftFloatCall(context, nameof(SoftFloat32.FPRSqrtStepFused), op1, op2);
                });
            }
        }

        public static void Fsqrt_S(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitScalarUnaryOpF(context, Intrinsic.Arm64FsqrtS);
            }
            else if (Optimizations.FastFP && Optimizations.UseSse2)
            {
                EmitScalarUnaryOpF(context, Intrinsic.X86Sqrtss, Intrinsic.X86Sqrtsd);
            }
            else
            {
                EmitScalarUnaryOpF(context, (op1) =>
                {
                    return EmitSoftFloatCall(context, nameof(SoftFloat32.FPSqrt), op1);
                });
            }
        }

        public static void Fsqrt_V(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitVectorUnaryOpF(context, Intrinsic.Arm64FsqrtV);
            }
            else if (Optimizations.FastFP && Optimizations.UseSse2)
            {
                EmitVectorUnaryOpF(context, Intrinsic.X86Sqrtps, Intrinsic.X86Sqrtpd);
            }
            else
            {
                EmitVectorUnaryOpF(context, (op1) =>
                {
                    return EmitSoftFloatCall(context, nameof(SoftFloat32.FPSqrt), op1);
                });
            }
        }

        public static void Fsub_S(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitScalarBinaryOpF(context, Intrinsic.Arm64FsubS);
            }
            else if (Optimizations.FastFP && Optimizations.UseSse2)
            {
                EmitScalarBinaryOpF(context, Intrinsic.X86Subss, Intrinsic.X86Subsd);
            }
            else if (Optimizations.FastFP)
            {
                EmitScalarBinaryOpF(context, (op1, op2) => context.Subtract(op1, op2));
            }
            else
            {
                EmitScalarBinaryOpF(context, (op1, op2) =>
                {
                    return EmitSoftFloatCall(context, nameof(SoftFloat32.FPSub), op1, op2);
                });
            }
        }

        public static void Fsub_V(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitVectorBinaryOpF(context, Intrinsic.Arm64FsubV);
            }
            else if (Optimizations.FastFP && Optimizations.UseSse2)
            {
                EmitVectorBinaryOpF(context, Intrinsic.X86Subps, Intrinsic.X86Subpd);
            }
            else if (Optimizations.FastFP)
            {
                EmitVectorBinaryOpF(context, (op1, op2) => context.Subtract(op1, op2));
            }
            else
            {
                EmitVectorBinaryOpF(context, (op1, op2) =>
                {
                    return EmitSoftFloatCall(context, nameof(SoftFloat32.FPSub), op1, op2);
                });
            }
        }

        public static void Mla_V(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitVectorTernaryOpRd(context, Intrinsic.Arm64MlaV);
            }
            else if (Optimizations.UseSse41)
            {
                EmitSse41VectorMul_AddSub(context, AddSub.Add);
            }
            else
            {
                EmitVectorTernaryOpZx(context, (op1, op2, op3) =>
                {
                    return context.Add(op1, context.Multiply(op2, op3));
                });
            }
        }

        public static void Mla_Ve(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitVectorTernaryOpRdByElem(context, Intrinsic.Arm64MlaVe);
            }
            else
            {
                EmitVectorTernaryOpByElemZx(context, (op1, op2, op3) =>
                {
                    return context.Add(op1, context.Multiply(op2, op3));
                });
            }
        }

        public static void Mls_V(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitVectorTernaryOpRd(context, Intrinsic.Arm64MlsV);
            }
            else if (Optimizations.UseSse41)
            {
                EmitSse41VectorMul_AddSub(context, AddSub.Subtract);
            }
            else
            {
                EmitVectorTernaryOpZx(context, (op1, op2, op3) =>
                {
                    return context.Subtract(op1, context.Multiply(op2, op3));
                });
            }
        }

        public static void Mls_Ve(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitVectorTernaryOpRdByElem(context, Intrinsic.Arm64MlsVe);
            }
            else
            {
                EmitVectorTernaryOpByElemZx(context, (op1, op2, op3) =>
                {
                    return context.Subtract(op1, context.Multiply(op2, op3));
                });
            }
        }

        public static void Mul_V(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitVectorBinaryOp(context, Intrinsic.Arm64MulV);
            }
            else if (Optimizations.UseSse41)
            {
                EmitSse41VectorMul_AddSub(context, AddSub.None);
            }
            else
            {
                EmitVectorBinaryOpZx(context, (op1, op2) => context.Multiply(op1, op2));
            }
        }

        public static void Mul_Ve(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitVectorBinaryOpByElem(context, Intrinsic.Arm64MulVe);
            }
            else
            {
                EmitVectorBinaryOpByElemZx(context, (op1, op2) => context.Multiply(op1, op2));
            }
        }

        public static void Neg_S(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitScalarUnaryOp(context, Intrinsic.Arm64NegS);
            }
            else
            {
                EmitScalarUnaryOpSx(context, (op1) => context.Negate(op1));
            }
        }

        public static void Neg_V(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitVectorUnaryOp(context, Intrinsic.Arm64NegV);
            }
            else if (Optimizations.UseSse2)
            {
                OpCodeSimd op = (OpCodeSimd)context.CurrOp;

                Intrinsic subInst = X86PsubInstruction[op.Size];

                Operand res = context.AddIntrinsic(subInst, context.VectorZero(), GetVec(op.Rn));

                if (op.RegisterSize == RegisterSize.Simd64)
                {
                    res = context.VectorZeroUpper64(res);
                }

                context.Copy(GetVec(op.Rd), res);
            }
            else
            {
                EmitVectorUnaryOpSx(context, (op1) => context.Negate(op1));
            }
        }

        public static void Pmull_V(ArmEmitterContext context)
        {
            OpCodeSimdReg op = (OpCodeSimdReg)context.CurrOp;

            if (Optimizations.UseArm64Pmull)
            {
                InstEmitSimdHelperArm64.EmitVectorBinaryOp(context, Intrinsic.Arm64PmullV);
            }
            else if (Optimizations.UsePclmulqdq && op.Size == 3)
            {
                Operand n = GetVec(op.Rn);
                Operand m = GetVec(op.Rm);

                int imm8 = op.RegisterSize == RegisterSize.Simd64 ? 0b0000_0000 : 0b0001_0001;

                Operand res = context.AddIntrinsic(Intrinsic.X86Pclmulqdq, n, m, Const(imm8));

                context.Copy(GetVec(op.Rd), res);
            }
            else if (Optimizations.UseSse41)
            {
                Operand n = GetVec(op.Rn);
                Operand m = GetVec(op.Rm);

                if (op.RegisterSize == RegisterSize.Simd64)
                {
                    n = context.VectorZeroUpper64(n);
                    m = context.VectorZeroUpper64(m);
                }
                else /* if (op.RegisterSize == RegisterSize.Simd128) */
                {
                    n = context.AddIntrinsic(Intrinsic.X86Psrldq, n, Const(8));
                    m = context.AddIntrinsic(Intrinsic.X86Psrldq, m, Const(8));
                }

                Operand res = context.VectorZero();

                if (op.Size == 0)
                {
                    n = context.AddIntrinsic(Intrinsic.X86Pmovzxbw, n);
                    m = context.AddIntrinsic(Intrinsic.X86Pmovzxbw, m);

                    for (int i = 0; i < 8; i++)
                    {
                        Operand mask = context.AddIntrinsic(Intrinsic.X86Psllw, n, Const(15 - i));
                        mask = context.AddIntrinsic(Intrinsic.X86Psraw, mask, Const(15));

                        Operand tmp = context.AddIntrinsic(Intrinsic.X86Psllw, m, Const(i));
                        tmp = context.AddIntrinsic(Intrinsic.X86Pand, tmp, mask);

                        res = context.AddIntrinsic(Intrinsic.X86Pxor, res, tmp);
                    }
                }
                else /* if (op.Size == 3) */
                {
                    Operand zero = context.VectorZero();

                    for (int i = 0; i < 64; i++)
                    {
                        Operand mask = context.AddIntrinsic(Intrinsic.X86Movlhps, n, n);
                        mask = context.AddIntrinsic(Intrinsic.X86Psllq, mask, Const(63 - i));
                        mask = context.AddIntrinsic(Intrinsic.X86Psrlq, mask, Const(63));
                        mask = context.AddIntrinsic(Intrinsic.X86Psubq, zero, mask);

                        Operand tmp = EmitSse2Sll_128(context, m, i);
                        tmp = context.AddIntrinsic(Intrinsic.X86Pand, tmp, mask);

                        res = context.AddIntrinsic(Intrinsic.X86Pxor, res, tmp);
                    }
                }

                context.Copy(GetVec(op.Rd), res);
            }
            else
            {
                Operand n = GetVec(op.Rn);
                Operand m = GetVec(op.Rm);

                Operand res;

                if (op.Size == 0)
                {
                    res = context.VectorZero();

                    int part = op.RegisterSize == RegisterSize.Simd64 ? 0 : 8;

                    for (int index = 0; index < 8; index++)
                    {
                        Operand ne = context.VectorExtract8(n, part + index);
                        Operand me = context.VectorExtract8(m, part + index);

                        Operand de = EmitPolynomialMultiply(context, ne, me, 8);

                        res = EmitVectorInsert(context, res, de, index, 1);
                    }
                }
                else /* if (op.Size == 3) */
                {
                    int part = op.RegisterSize == RegisterSize.Simd64 ? 0 : 1;

                    Operand ne = context.VectorExtract(OperandType.I64, n, part);
                    Operand me = context.VectorExtract(OperandType.I64, m, part);

                    res = context.Call(typeof(SoftFallback).GetMethod(nameof(SoftFallback.PolynomialMult64_128)), ne, me);
                }

                context.Copy(GetVec(op.Rd), res);
            }
        }

        public static void Raddhn_V(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitVectorTernaryOpRd(context, Intrinsic.Arm64RaddhnV);
            }
            else
            {
                EmitHighNarrow(context, (op1, op2) => context.Add(op1, op2), round: true);
            }
        }

        public static void Rsubhn_V(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitVectorTernaryOpRd(context, Intrinsic.Arm64RsubhnV);
            }
            else
            {
                EmitHighNarrow(context, (op1, op2) => context.Subtract(op1, op2), round: true);
            }
        }

        public static void Saba_V(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitVectorTernaryOpRd(context, Intrinsic.Arm64SabaV);
            }
            else
            {
                EmitVectorTernaryOpSx(context, (op1, op2, op3) =>
                {
                    return context.Add(op1, EmitAbs(context, context.Subtract(op2, op3)));
                });
            }
        }

        public static void Sabal_V(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitVectorTernaryOpRd(context, Intrinsic.Arm64SabalV);
            }
            else
            {
                EmitVectorWidenRnRmTernaryOpSx(context, (op1, op2, op3) =>
                {
                    return context.Add(op1, EmitAbs(context, context.Subtract(op2, op3)));
                });
            }
        }

        public static void Sabd_V(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitVectorBinaryOp(context, Intrinsic.Arm64SabdV);
            }
            else if (Optimizations.UseSse41)
            {
                OpCodeSimdReg op = (OpCodeSimdReg)context.CurrOp;

                Operand n = GetVec(op.Rn);
                Operand m = GetVec(op.Rm);

                EmitSse41VectorSabdOp(context, op, n, m, isLong: false);
            }
            else
            {
                EmitVectorBinaryOpSx(context, (op1, op2) =>
                {
                    return EmitAbs(context, context.Subtract(op1, op2));
                });
            }
        }

        public static void Sabdl_V(ArmEmitterContext context)
        {
            OpCodeSimdReg op = (OpCodeSimdReg)context.CurrOp;

            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitVectorBinaryOp(context, Intrinsic.Arm64SabdlV);
            }
            else if (Optimizations.UseSse41 && op.Size < 2)
            {
                Operand n = GetVec(op.Rn);
                Operand m = GetVec(op.Rm);

                if (op.RegisterSize == RegisterSize.Simd128)
                {
                    n = context.AddIntrinsic(Intrinsic.X86Psrldq, n, Const(8));
                    m = context.AddIntrinsic(Intrinsic.X86Psrldq, m, Const(8));
                }

                Intrinsic movInst = op.Size == 0
                    ? Intrinsic.X86Pmovsxbw
                    : Intrinsic.X86Pmovsxwd;

                n = context.AddIntrinsic(movInst, n);
                m = context.AddIntrinsic(movInst, m);

                EmitSse41VectorSabdOp(context, op, n, m, isLong: true);
            }
            else
            {
                EmitVectorWidenRnRmBinaryOpSx(context, (op1, op2) =>
                {
                    return EmitAbs(context, context.Subtract(op1, op2));
                });
            }
        }

        public static void Sadalp_V(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitVectorBinaryOpRd(context, Intrinsic.Arm64SadalpV);
            }
            else
            {
                EmitAddLongPairwise(context, signed: true, accumulate: true);
            }
        }

        public static void Saddl_V(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitVectorBinaryOp(context, Intrinsic.Arm64SaddlV);
            }
            else if (Optimizations.UseSse41)
            {
                OpCodeSimdReg op = (OpCodeSimdReg)context.CurrOp;

                Operand n = GetVec(op.Rn);
                Operand m = GetVec(op.Rm);

                if (op.RegisterSize == RegisterSize.Simd128)
                {
                    n = context.AddIntrinsic(Intrinsic.X86Psrldq, n, Const(8));
                    m = context.AddIntrinsic(Intrinsic.X86Psrldq, m, Const(8));
                }

                Intrinsic movInst = X86PmovsxInstruction[op.Size];

                n = context.AddIntrinsic(movInst, n);
                m = context.AddIntrinsic(movInst, m);

                Intrinsic addInst = X86PaddInstruction[op.Size + 1];

                context.Copy(GetVec(op.Rd), context.AddIntrinsic(addInst, n, m));
            }
            else
            {
                EmitVectorWidenRnRmBinaryOpSx(context, (op1, op2) => context.Add(op1, op2));
            }
        }

        public static void Saddlp_V(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitVectorUnaryOp(context, Intrinsic.Arm64SaddlpV);
            }
            else
            {
                EmitAddLongPairwise(context, signed: true, accumulate: false);
            }
        }

        public static void Saddlv_V(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitVectorUnaryOp(context, Intrinsic.Arm64SaddlvV);
            }
            else
            {
                EmitVectorLongAcrossVectorOpSx(context, (op1, op2) => context.Add(op1, op2));
            }
        }

        public static void Saddw_V(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitVectorBinaryOp(context, Intrinsic.Arm64SaddwV);
            }
            else if (Optimizations.UseSse41)
            {
                OpCodeSimdReg op = (OpCodeSimdReg)context.CurrOp;

                Operand n = GetVec(op.Rn);
                Operand m = GetVec(op.Rm);

                if (op.RegisterSize == RegisterSize.Simd128)
                {
                    m = context.AddIntrinsic(Intrinsic.X86Psrldq, m, Const(8));
                }

                Intrinsic movInst = X86PmovsxInstruction[op.Size];

                m = context.AddIntrinsic(movInst, m);

                Intrinsic addInst = X86PaddInstruction[op.Size + 1];

                context.Copy(GetVec(op.Rd), context.AddIntrinsic(addInst, n, m));
            }
            else
            {
                EmitVectorWidenRmBinaryOpSx(context, (op1, op2) => context.Add(op1, op2));
            }
        }

        public static void Shadd_V(ArmEmitterContext context)
        {
            OpCodeSimdReg op = (OpCodeSimdReg)context.CurrOp;

            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitVectorBinaryOp(context, Intrinsic.Arm64ShaddV);
            }
            else if (Optimizations.UseSse2 && op.Size > 0)
            {
                Operand n = GetVec(op.Rn);
                Operand m = GetVec(op.Rm);

                Operand res = context.AddIntrinsic(Intrinsic.X86Pand, n, m);
                Operand res2 = context.AddIntrinsic(Intrinsic.X86Pxor, n, m);

                Intrinsic shiftInst = op.Size == 1 ? Intrinsic.X86Psraw : Intrinsic.X86Psrad;

                res2 = context.AddIntrinsic(shiftInst, res2, Const(1));

                Intrinsic addInst = X86PaddInstruction[op.Size];

                res = context.AddIntrinsic(addInst, res, res2);

                if (op.RegisterSize == RegisterSize.Simd64)
                {
                    res = context.VectorZeroUpper64(res);
                }

                context.Copy(GetVec(op.Rd), res);
            }
            else
            {
                EmitVectorBinaryOpSx(context, (op1, op2) =>
                {
                    return context.ShiftRightSI(context.Add(op1, op2), Const(1));
                });
            }
        }

        public static void Shsub_V(ArmEmitterContext context)
        {
            OpCodeSimdReg op = (OpCodeSimdReg)context.CurrOp;

            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitVectorBinaryOp(context, Intrinsic.Arm64ShsubV);
            }
            else if (Optimizations.UseSse2 && op.Size < 2)
            {
                Operand n = GetVec(op.Rn);
                Operand m = GetVec(op.Rm);

                Operand mask = X86GetAllElements(context, (int)(op.Size == 0 ? 0x80808080u : 0x80008000u));

                Intrinsic addInst = X86PaddInstruction[op.Size];

                Operand nPlusMask = context.AddIntrinsic(addInst, n, mask);
                Operand mPlusMask = context.AddIntrinsic(addInst, m, mask);

                Intrinsic avgInst = op.Size == 0 ? Intrinsic.X86Pavgb : Intrinsic.X86Pavgw;

                Operand res = context.AddIntrinsic(avgInst, nPlusMask, mPlusMask);

                Intrinsic subInst = X86PsubInstruction[op.Size];

                res = context.AddIntrinsic(subInst, nPlusMask, res);

                if (op.RegisterSize == RegisterSize.Simd64)
                {
                    res = context.VectorZeroUpper64(res);
                }

                context.Copy(GetVec(op.Rd), res);
            }
            else
            {
                EmitVectorBinaryOpSx(context, (op1, op2) =>
                {
                    return context.ShiftRightSI(context.Subtract(op1, op2), Const(1));
                });
            }
        }

        public static void Smax_V(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitVectorBinaryOp(context, Intrinsic.Arm64SmaxV);
            }
            else if (Optimizations.UseSse41)
            {
                OpCodeSimdReg op = (OpCodeSimdReg)context.CurrOp;

                Operand n = GetVec(op.Rn);
                Operand m = GetVec(op.Rm);

                Intrinsic maxInst = X86PmaxsInstruction[op.Size];

                Operand res = context.AddIntrinsic(maxInst, n, m);

                if (op.RegisterSize == RegisterSize.Simd64)
                {
                    res = context.VectorZeroUpper64(res);
                }

                context.Copy(GetVec(op.Rd), res);
            }
            else
            {
                EmitVectorBinaryOpSx(context, (op1, op2) => EmitMax64Op(context, op1, op2, signed: true));
            }
        }

        public static void Smaxp_V(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitVectorBinaryOp(context, Intrinsic.Arm64SmaxpV);
            }
            else if (Optimizations.UseSsse3)
            {
                EmitSsse3VectorPairwiseOp(context, X86PmaxsInstruction);
            }
            else
            {
                EmitVectorPairwiseOpSx(context, (op1, op2) => EmitMax64Op(context, op1, op2, signed: true));
            }
        }

        public static void Smaxv_V(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitVectorUnaryOp(context, Intrinsic.Arm64SmaxvV);
            }
            else
            {
                EmitVectorAcrossVectorOpSx(context, (op1, op2) => EmitMax64Op(context, op1, op2, signed: true));
            }
        }

        public static void Smin_V(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitVectorBinaryOp(context, Intrinsic.Arm64SminV);
            }
            else if (Optimizations.UseSse41)
            {
                OpCodeSimdReg op = (OpCodeSimdReg)context.CurrOp;

                Operand n = GetVec(op.Rn);
                Operand m = GetVec(op.Rm);

                Intrinsic minInst = X86PminsInstruction[op.Size];

                Operand res = context.AddIntrinsic(minInst, n, m);

                if (op.RegisterSize == RegisterSize.Simd64)
                {
                    res = context.VectorZeroUpper64(res);
                }

                context.Copy(GetVec(op.Rd), res);
            }
            else
            {
                EmitVectorBinaryOpSx(context, (op1, op2) => EmitMin64Op(context, op1, op2, signed: true));
            }
        }

        public static void Sminp_V(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitVectorBinaryOp(context, Intrinsic.Arm64SminpV);
            }
            else if (Optimizations.UseSsse3)
            {
                EmitSsse3VectorPairwiseOp(context, X86PminsInstruction);
            }
            else
            {
                EmitVectorPairwiseOpSx(context, (op1, op2) => EmitMin64Op(context, op1, op2, signed: true));
            }
        }

        public static void Sminv_V(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitVectorUnaryOp(context, Intrinsic.Arm64SminvV);
            }
            else
            {
                EmitVectorAcrossVectorOpSx(context, (op1, op2) => EmitMin64Op(context, op1, op2, signed: true));
            }
        }

        public static void Smlal_V(ArmEmitterContext context)
        {
            OpCodeSimdReg op = (OpCodeSimdReg)context.CurrOp;

            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitVectorTernaryOpRd(context, Intrinsic.Arm64SmlalV);
            }
            else if (Optimizations.UseSse41 && op.Size < 2)
            {
                Operand d = GetVec(op.Rd);
                Operand n = GetVec(op.Rn);
                Operand m = GetVec(op.Rm);

                if (op.RegisterSize == RegisterSize.Simd128)
                {
                    n = context.AddIntrinsic(Intrinsic.X86Psrldq, n, Const(8));
                    m = context.AddIntrinsic(Intrinsic.X86Psrldq, m, Const(8));
                }

                Intrinsic movInst = X86PmovsxInstruction[op.Size];

                n = context.AddIntrinsic(movInst, n);
                m = context.AddIntrinsic(movInst, m);

                Intrinsic mullInst = op.Size == 0 ? Intrinsic.X86Pmullw : Intrinsic.X86Pmulld;

                Operand res = context.AddIntrinsic(mullInst, n, m);

                Intrinsic addInst = X86PaddInstruction[op.Size + 1];

                context.Copy(d, context.AddIntrinsic(addInst, d, res));
            }
            else
            {
                EmitVectorWidenRnRmTernaryOpSx(context, (op1, op2, op3) =>
                {
                    return context.Add(op1, context.Multiply(op2, op3));
                });
            }
        }

        public static void Smlal_Ve(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitVectorTernaryOpRdByElem(context, Intrinsic.Arm64SmlalVe);
            }
            else
            {
                EmitVectorWidenTernaryOpByElemSx(context, (op1, op2, op3) =>
                {
                    return context.Add(op1, context.Multiply(op2, op3));
                });
            }
        }

        public static void Smlsl_V(ArmEmitterContext context)
        {
            OpCodeSimdReg op = (OpCodeSimdReg)context.CurrOp;

            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitVectorTernaryOpRd(context, Intrinsic.Arm64SmlslV);
            }
            else if (Optimizations.UseSse41 && op.Size < 2)
            {
                Operand d = GetVec(op.Rd);
                Operand n = GetVec(op.Rn);
                Operand m = GetVec(op.Rm);

                if (op.RegisterSize == RegisterSize.Simd128)
                {
                    n = context.AddIntrinsic(Intrinsic.X86Psrldq, n, Const(8));
                    m = context.AddIntrinsic(Intrinsic.X86Psrldq, m, Const(8));
                }

                Intrinsic movInst = op.Size == 0 ? Intrinsic.X86Pmovsxbw : Intrinsic.X86Pmovsxwd;

                n = context.AddIntrinsic(movInst, n);
                m = context.AddIntrinsic(movInst, m);

                Intrinsic mullInst = op.Size == 0 ? Intrinsic.X86Pmullw : Intrinsic.X86Pmulld;

                Operand res = context.AddIntrinsic(mullInst, n, m);

                Intrinsic subInst = X86PsubInstruction[op.Size + 1];

                context.Copy(d, context.AddIntrinsic(subInst, d, res));
            }
            else
            {
                EmitVectorWidenRnRmTernaryOpSx(context, (op1, op2, op3) =>
                {
                    return context.Subtract(op1, context.Multiply(op2, op3));
                });
            }
        }

        public static void Smlsl_Ve(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitVectorTernaryOpRdByElem(context, Intrinsic.Arm64SmlslVe);
            }
            else
            {
                EmitVectorWidenTernaryOpByElemSx(context, (op1, op2, op3) =>
                {
                    return context.Subtract(op1, context.Multiply(op2, op3));
                });
            }
        }

        public static void Smull_V(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitVectorBinaryOp(context, Intrinsic.Arm64SmullV);
            }
            else
            {
                EmitVectorWidenRnRmBinaryOpSx(context, (op1, op2) => context.Multiply(op1, op2));
            }
        }

        public static void Smull_Ve(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitVectorBinaryOpByElem(context, Intrinsic.Arm64SmullVe);
            }
            else
            {
                EmitVectorWidenBinaryOpByElemSx(context, (op1, op2) => context.Multiply(op1, op2));
            }
        }

        public static void Sqabs_S(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitScalarSaturatingUnaryOp(context, Intrinsic.Arm64SqabsS);
            }
            else
            {
                EmitScalarSaturatingUnaryOpSx(context, (op1) => EmitAbs(context, op1));
            }
        }

        public static void Sqabs_V(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitVectorSaturatingUnaryOp(context, Intrinsic.Arm64SqabsV);
            }
            else
            {
                EmitVectorSaturatingUnaryOpSx(context, (op1) => EmitAbs(context, op1));
            }
        }

        public static void Sqadd_S(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitScalarSaturatingBinaryOp(context, Intrinsic.Arm64SqaddS);
            }
            else
            {
                EmitScalarSaturatingBinaryOpSx(context, flags: SaturatingFlags.Add);
            }
        }

        public static void Sqadd_V(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitVectorSaturatingBinaryOp(context, Intrinsic.Arm64SqaddV);
            }
            else
            {
                EmitVectorSaturatingBinaryOpSx(context, flags: SaturatingFlags.Add);
            }
        }

        public static void Sqdmulh_S(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitScalarSaturatingBinaryOp(context, Intrinsic.Arm64SqdmulhS);
            }
            else
            {
                EmitScalarSaturatingBinaryOpSx(context, (op1, op2) => EmitDoublingMultiplyHighHalf(context, op1, op2, round: false));
            }
        }

        public static void Sqdmulh_V(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitVectorSaturatingBinaryOp(context, Intrinsic.Arm64SqdmulhV);
            }
            else
            {
                EmitVectorSaturatingBinaryOpSx(context, (op1, op2) => EmitDoublingMultiplyHighHalf(context, op1, op2, round: false));
            }
        }

        public static void Sqdmulh_Ve(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitVectorSaturatingBinaryOpByElem(context, Intrinsic.Arm64SqdmulhVe);
            }
            else
            {
                EmitVectorSaturatingBinaryOpByElemSx(context, (op1, op2) => EmitDoublingMultiplyHighHalf(context, op1, op2, round: false));
            }
        }

        public static void Sqneg_S(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitScalarSaturatingUnaryOp(context, Intrinsic.Arm64SqnegS);
            }
            else
            {
                EmitScalarSaturatingUnaryOpSx(context, (op1) => context.Negate(op1));
            }
        }

        public static void Sqneg_V(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitVectorSaturatingUnaryOp(context, Intrinsic.Arm64SqnegV);
            }
            else
            {
                EmitVectorSaturatingUnaryOpSx(context, (op1) => context.Negate(op1));
            }
        }

        public static void Sqrdmulh_S(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitScalarSaturatingBinaryOp(context, Intrinsic.Arm64SqrdmulhS);
            }
            else
            {
                EmitScalarSaturatingBinaryOpSx(context, (op1, op2) => EmitDoublingMultiplyHighHalf(context, op1, op2, round: true));
            }
        }

        public static void Sqrdmulh_V(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitVectorSaturatingBinaryOp(context, Intrinsic.Arm64SqrdmulhV);
            }
            else
            {
                EmitVectorSaturatingBinaryOpSx(context, (op1, op2) => EmitDoublingMultiplyHighHalf(context, op1, op2, round: true));
            }
        }

        public static void Sqrdmulh_Ve(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitVectorSaturatingBinaryOpByElem(context, Intrinsic.Arm64SqrdmulhVe);
            }
            else
            {
                EmitVectorSaturatingBinaryOpByElemSx(context, (op1, op2) => EmitDoublingMultiplyHighHalf(context, op1, op2, round: true));
            }
        }

        public static void Sqsub_S(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitScalarSaturatingBinaryOp(context, Intrinsic.Arm64SqsubS);
            }
            else
            {
                EmitScalarSaturatingBinaryOpSx(context, flags: SaturatingFlags.Sub);
            }
        }

        public static void Sqsub_V(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitVectorSaturatingBinaryOp(context, Intrinsic.Arm64SqsubV);
            }
            else
            {
                EmitVectorSaturatingBinaryOpSx(context, flags: SaturatingFlags.Sub);
            }
        }

        public static void Sqxtn_S(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitScalarSaturatingBinaryOpRd(context, Intrinsic.Arm64SqxtnS);
            }
            else
            {
                EmitSaturatingNarrowOp(context, SaturatingNarrowFlags.ScalarSxSx);
            }
        }

        public static void Sqxtn_V(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitVectorSaturatingBinaryOpRd(context, Intrinsic.Arm64SqxtnV);
            }
            else
            {
                EmitSaturatingNarrowOp(context, SaturatingNarrowFlags.VectorSxSx);
            }
        }

        public static void Sqxtun_S(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitScalarSaturatingBinaryOpRd(context, Intrinsic.Arm64SqxtunS);
            }
            else
            {
                EmitSaturatingNarrowOp(context, SaturatingNarrowFlags.ScalarSxZx);
            }
        }

        public static void Sqxtun_V(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitVectorSaturatingBinaryOpRd(context, Intrinsic.Arm64SqxtunV);
            }
            else
            {
                EmitSaturatingNarrowOp(context, SaturatingNarrowFlags.VectorSxZx);
            }
        }

        public static void Srhadd_V(ArmEmitterContext context)
        {
            OpCodeSimdReg op = (OpCodeSimdReg)context.CurrOp;

            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitVectorBinaryOp(context, Intrinsic.Arm64SrhaddV);
            }
            else if (Optimizations.UseSse2 && op.Size < 2)
            {
                Operand n = GetVec(op.Rn);
                Operand m = GetVec(op.Rm);

                Operand mask = X86GetAllElements(context, (int)(op.Size == 0 ? 0x80808080u : 0x80008000u));

                Intrinsic subInst = X86PsubInstruction[op.Size];

                Operand nMinusMask = context.AddIntrinsic(subInst, n, mask);
                Operand mMinusMask = context.AddIntrinsic(subInst, m, mask);

                Intrinsic avgInst = op.Size == 0 ? Intrinsic.X86Pavgb : Intrinsic.X86Pavgw;

                Operand res = context.AddIntrinsic(avgInst, nMinusMask, mMinusMask);

                Intrinsic addInst = X86PaddInstruction[op.Size];

                res = context.AddIntrinsic(addInst, mask, res);

                if (op.RegisterSize == RegisterSize.Simd64)
                {
                    res = context.VectorZeroUpper64(res);
                }

                context.Copy(GetVec(op.Rd), res);
            }
            else
            {
                EmitVectorBinaryOpSx(context, (op1, op2) =>
                {
                    Operand res = context.Add(op1, op2);

                    res = context.Add(res, Const(1L));

                    return context.ShiftRightSI(res, Const(1));
                });
            }
        }

        public static void Ssubl_V(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitVectorBinaryOp(context, Intrinsic.Arm64SsublV);
            }
            else if (Optimizations.UseSse41)
            {
                OpCodeSimdReg op = (OpCodeSimdReg)context.CurrOp;

                Operand n = GetVec(op.Rn);
                Operand m = GetVec(op.Rm);

                if (op.RegisterSize == RegisterSize.Simd128)
                {
                    n = context.AddIntrinsic(Intrinsic.X86Psrldq, n, Const(8));
                    m = context.AddIntrinsic(Intrinsic.X86Psrldq, m, Const(8));
                }

                Intrinsic movInst = X86PmovsxInstruction[op.Size];

                n = context.AddIntrinsic(movInst, n);
                m = context.AddIntrinsic(movInst, m);

                Intrinsic subInst = X86PsubInstruction[op.Size + 1];

                context.Copy(GetVec(op.Rd), context.AddIntrinsic(subInst, n, m));
            }
            else
            {
                EmitVectorWidenRnRmBinaryOpSx(context, (op1, op2) => context.Subtract(op1, op2));
            }
        }

        public static void Ssubw_V(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitVectorBinaryOp(context, Intrinsic.Arm64SsubwV);
            }
            else if (Optimizations.UseSse41)
            {
                OpCodeSimdReg op = (OpCodeSimdReg)context.CurrOp;

                Operand n = GetVec(op.Rn);
                Operand m = GetVec(op.Rm);

                if (op.RegisterSize == RegisterSize.Simd128)
                {
                    m = context.AddIntrinsic(Intrinsic.X86Psrldq, m, Const(8));
                }

                Intrinsic movInst = X86PmovsxInstruction[op.Size];

                m = context.AddIntrinsic(movInst, m);

                Intrinsic subInst = X86PsubInstruction[op.Size + 1];

                context.Copy(GetVec(op.Rd), context.AddIntrinsic(subInst, n, m));
            }
            else
            {
                EmitVectorWidenRmBinaryOpSx(context, (op1, op2) => context.Subtract(op1, op2));
            }
        }

        public static void Sub_S(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitScalarBinaryOp(context, Intrinsic.Arm64SubS);
            }
            else
            {
                EmitScalarBinaryOpZx(context, (op1, op2) => context.Subtract(op1, op2));
            }
        }

        public static void Sub_V(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitVectorBinaryOp(context, Intrinsic.Arm64SubV);
            }
            else if (Optimizations.UseSse2)
            {
                OpCodeSimdReg op = (OpCodeSimdReg)context.CurrOp;

                Operand n = GetVec(op.Rn);
                Operand m = GetVec(op.Rm);

                Intrinsic subInst = X86PsubInstruction[op.Size];

                Operand res = context.AddIntrinsic(subInst, n, m);

                if (op.RegisterSize == RegisterSize.Simd64)
                {
                    res = context.VectorZeroUpper64(res);
                }

                context.Copy(GetVec(op.Rd), res);
            }
            else
            {
                EmitVectorBinaryOpZx(context, (op1, op2) => context.Subtract(op1, op2));
            }
        }

        public static void Subhn_V(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitVectorTernaryOpRd(context, Intrinsic.Arm64SubhnV);
            }
            else
            {
                EmitHighNarrow(context, (op1, op2) => context.Subtract(op1, op2), round: false);
            }
        }

        public static void Suqadd_S(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitScalarSaturatingBinaryOpRd(context, Intrinsic.Arm64SuqaddS);
            }
            else
            {
                EmitScalarSaturatingBinaryOpSx(context, flags: SaturatingFlags.Accumulate);
            }
        }

        public static void Suqadd_V(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitVectorSaturatingBinaryOpRd(context, Intrinsic.Arm64SuqaddV);
            }
            else
            {
                EmitVectorSaturatingBinaryOpSx(context, flags: SaturatingFlags.Accumulate);
            }
        }

        public static void Uaba_V(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitVectorTernaryOpRd(context, Intrinsic.Arm64UabaV);
            }
            else
            {
                EmitVectorTernaryOpZx(context, (op1, op2, op3) =>
                {
                    return context.Add(op1, EmitAbs(context, context.Subtract(op2, op3)));
                });
            }
        }

        public static void Uabal_V(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitVectorTernaryOpRd(context, Intrinsic.Arm64UabalV);
            }
            else
            {
                EmitVectorWidenRnRmTernaryOpZx(context, (op1, op2, op3) =>
                {
                    return context.Add(op1, EmitAbs(context, context.Subtract(op2, op3)));
                });
            }
        }

        public static void Uabd_V(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitVectorBinaryOp(context, Intrinsic.Arm64UabdV);
            }
            else if (Optimizations.UseSse41)
            {
                OpCodeSimdReg op = (OpCodeSimdReg)context.CurrOp;

                Operand n = GetVec(op.Rn);
                Operand m = GetVec(op.Rm);

                EmitSse41VectorUabdOp(context, op, n, m, isLong: false);
            }
            else
            {
                EmitVectorBinaryOpZx(context, (op1, op2) =>
                {
                    return EmitAbs(context, context.Subtract(op1, op2));
                });
            }
        }

        public static void Uabdl_V(ArmEmitterContext context)
        {
            OpCodeSimdReg op = (OpCodeSimdReg)context.CurrOp;

            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitVectorBinaryOp(context, Intrinsic.Arm64UabdlV);
            }
            else if (Optimizations.UseSse41 && op.Size < 2)
            {
                Operand n = GetVec(op.Rn);
                Operand m = GetVec(op.Rm);

                if (op.RegisterSize == RegisterSize.Simd128)
                {
                    n = context.AddIntrinsic(Intrinsic.X86Psrldq, n, Const(8));
                    m = context.AddIntrinsic(Intrinsic.X86Psrldq, m, Const(8));
                }

                Intrinsic movInst = op.Size == 0
                    ? Intrinsic.X86Pmovzxbw
                    : Intrinsic.X86Pmovzxwd;

                n = context.AddIntrinsic(movInst, n);
                m = context.AddIntrinsic(movInst, m);

                EmitSse41VectorUabdOp(context, op, n, m, isLong: true);
            }
            else
            {
                EmitVectorWidenRnRmBinaryOpZx(context, (op1, op2) =>
                {
                    return EmitAbs(context, context.Subtract(op1, op2));
                });
            }
        }

        public static void Uadalp_V(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitVectorBinaryOpRd(context, Intrinsic.Arm64UadalpV);
            }
            else
            {
                EmitAddLongPairwise(context, signed: false, accumulate: true);
            }
        }

        public static void Uaddl_V(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitVectorBinaryOp(context, Intrinsic.Arm64UaddlV);
            }
            else if (Optimizations.UseSse41)
            {
                OpCodeSimdReg op = (OpCodeSimdReg)context.CurrOp;

                Operand n = GetVec(op.Rn);
                Operand m = GetVec(op.Rm);

                if (op.RegisterSize == RegisterSize.Simd128)
                {
                    n = context.AddIntrinsic(Intrinsic.X86Psrldq, n, Const(8));
                    m = context.AddIntrinsic(Intrinsic.X86Psrldq, m, Const(8));
                }

                Intrinsic movInst = X86PmovzxInstruction[op.Size];

                n = context.AddIntrinsic(movInst, n);
                m = context.AddIntrinsic(movInst, m);

                Intrinsic addInst = X86PaddInstruction[op.Size + 1];

                context.Copy(GetVec(op.Rd), context.AddIntrinsic(addInst, n, m));
            }
            else
            {
                EmitVectorWidenRnRmBinaryOpZx(context, (op1, op2) => context.Add(op1, op2));
            }
        }

        public static void Uaddlp_V(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitVectorUnaryOp(context, Intrinsic.Arm64UaddlpV);
            }
            else
            {
                EmitAddLongPairwise(context, signed: false, accumulate: false);
            }
        }

        public static void Uaddlv_V(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitVectorUnaryOp(context, Intrinsic.Arm64UaddlvV);
            }
            else
            {
                EmitVectorLongAcrossVectorOpZx(context, (op1, op2) => context.Add(op1, op2));
            }
        }

        public static void Uaddw_V(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitVectorBinaryOp(context, Intrinsic.Arm64UaddwV);
            }
            else if (Optimizations.UseSse41)
            {
                OpCodeSimdReg op = (OpCodeSimdReg)context.CurrOp;

                Operand n = GetVec(op.Rn);
                Operand m = GetVec(op.Rm);

                if (op.RegisterSize == RegisterSize.Simd128)
                {
                    m = context.AddIntrinsic(Intrinsic.X86Psrldq, m, Const(8));
                }

                Intrinsic movInst = X86PmovzxInstruction[op.Size];

                m = context.AddIntrinsic(movInst, m);

                Intrinsic addInst = X86PaddInstruction[op.Size + 1];

                context.Copy(GetVec(op.Rd), context.AddIntrinsic(addInst, n, m));
            }
            else
            {
                EmitVectorWidenRmBinaryOpZx(context, (op1, op2) => context.Add(op1, op2));
            }
        }

        public static void Uhadd_V(ArmEmitterContext context)
        {
            OpCodeSimdReg op = (OpCodeSimdReg)context.CurrOp;

            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitVectorBinaryOp(context, Intrinsic.Arm64UhaddV);
            }
            else if (Optimizations.UseSse2 && op.Size > 0)
            {
                Operand n = GetVec(op.Rn);
                Operand m = GetVec(op.Rm);

                Operand res = context.AddIntrinsic(Intrinsic.X86Pand, n, m);
                Operand res2 = context.AddIntrinsic(Intrinsic.X86Pxor, n, m);

                Intrinsic shiftInst = op.Size == 1 ? Intrinsic.X86Psrlw : Intrinsic.X86Psrld;

                res2 = context.AddIntrinsic(shiftInst, res2, Const(1));

                Intrinsic addInst = X86PaddInstruction[op.Size];

                res = context.AddIntrinsic(addInst, res, res2);

                if (op.RegisterSize == RegisterSize.Simd64)
                {
                    res = context.VectorZeroUpper64(res);
                }

                context.Copy(GetVec(op.Rd), res);
            }
            else
            {
                EmitVectorBinaryOpZx(context, (op1, op2) =>
                {
                    return context.ShiftRightUI(context.Add(op1, op2), Const(1));
                });
            }
        }

        public static void Uhsub_V(ArmEmitterContext context)
        {
            OpCodeSimdReg op = (OpCodeSimdReg)context.CurrOp;

            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitVectorBinaryOp(context, Intrinsic.Arm64UhsubV);
            }
            else if (Optimizations.UseSse2 && op.Size < 2)
            {
                Operand n = GetVec(op.Rn);
                Operand m = GetVec(op.Rm);

                Intrinsic avgInst = op.Size == 0 ? Intrinsic.X86Pavgb : Intrinsic.X86Pavgw;

                Operand res = context.AddIntrinsic(avgInst, n, m);

                Intrinsic subInst = X86PsubInstruction[op.Size];

                res = context.AddIntrinsic(subInst, n, res);

                if (op.RegisterSize == RegisterSize.Simd64)
                {
                    res = context.VectorZeroUpper64(res);
                }

                context.Copy(GetVec(op.Rd), res);
            }
            else
            {
                EmitVectorBinaryOpZx(context, (op1, op2) =>
                {
                    return context.ShiftRightUI(context.Subtract(op1, op2), Const(1));
                });
            }
        }

        public static void Umax_V(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitVectorBinaryOp(context, Intrinsic.Arm64UmaxV);
            }
            else if (Optimizations.UseSse41)
            {
                OpCodeSimdReg op = (OpCodeSimdReg)context.CurrOp;

                Operand n = GetVec(op.Rn);
                Operand m = GetVec(op.Rm);

                Intrinsic maxInst = X86PmaxuInstruction[op.Size];

                Operand res = context.AddIntrinsic(maxInst, n, m);

                if (op.RegisterSize == RegisterSize.Simd64)
                {
                    res = context.VectorZeroUpper64(res);
                }

                context.Copy(GetVec(op.Rd), res);
            }
            else
            {
                EmitVectorBinaryOpZx(context, (op1, op2) => EmitMax64Op(context, op1, op2, signed: false));
            }
        }

        public static void Umaxp_V(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitVectorBinaryOp(context, Intrinsic.Arm64UmaxpV);
            }
            else if (Optimizations.UseSsse3)
            {
                EmitSsse3VectorPairwiseOp(context, X86PmaxuInstruction);
            }
            else
            {
                EmitVectorPairwiseOpZx(context, (op1, op2) => EmitMax64Op(context, op1, op2, signed: false));
            }
        }

        public static void Umaxv_V(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitVectorUnaryOp(context, Intrinsic.Arm64UmaxvV);
            }
            else
            {
                EmitVectorAcrossVectorOpZx(context, (op1, op2) => EmitMax64Op(context, op1, op2, signed: false));
            }
        }

        public static void Umin_V(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitVectorBinaryOp(context, Intrinsic.Arm64UminV);
            }
            else if (Optimizations.UseSse41)
            {
                OpCodeSimdReg op = (OpCodeSimdReg)context.CurrOp;

                Operand n = GetVec(op.Rn);
                Operand m = GetVec(op.Rm);

                Intrinsic minInst = X86PminuInstruction[op.Size];

                Operand res = context.AddIntrinsic(minInst, n, m);

                if (op.RegisterSize == RegisterSize.Simd64)
                {
                    res = context.VectorZeroUpper64(res);
                }

                context.Copy(GetVec(op.Rd), res);
            }
            else
            {
                EmitVectorBinaryOpZx(context, (op1, op2) => EmitMin64Op(context, op1, op2, signed: false));
            }
        }

        public static void Uminp_V(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitVectorBinaryOp(context, Intrinsic.Arm64UminpV);
            }
            else if (Optimizations.UseSsse3)
            {
                EmitSsse3VectorPairwiseOp(context, X86PminuInstruction);
            }
            else
            {
                EmitVectorPairwiseOpZx(context, (op1, op2) => EmitMin64Op(context, op1, op2, signed: false));
            }
        }

        public static void Uminv_V(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitVectorUnaryOp(context, Intrinsic.Arm64UminvV);
            }
            else
            {
                EmitVectorAcrossVectorOpZx(context, (op1, op2) => EmitMin64Op(context, op1, op2, signed: false));
            }
        }

        public static void Umlal_V(ArmEmitterContext context)
        {
            OpCodeSimdReg op = (OpCodeSimdReg)context.CurrOp;

            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitVectorTernaryOpRd(context, Intrinsic.Arm64UmlalV);
            }
            else if (Optimizations.UseSse41 && op.Size < 2)
            {
                Operand d = GetVec(op.Rd);
                Operand n = GetVec(op.Rn);
                Operand m = GetVec(op.Rm);

                if (op.RegisterSize == RegisterSize.Simd128)
                {
                    n = context.AddIntrinsic(Intrinsic.X86Psrldq, n, Const(8));
                    m = context.AddIntrinsic(Intrinsic.X86Psrldq, m, Const(8));
                }

                Intrinsic movInst = X86PmovzxInstruction[op.Size];

                n = context.AddIntrinsic(movInst, n);
                m = context.AddIntrinsic(movInst, m);

                Intrinsic mullInst = op.Size == 0 ? Intrinsic.X86Pmullw : Intrinsic.X86Pmulld;

                Operand res = context.AddIntrinsic(mullInst, n, m);

                Intrinsic addInst = X86PaddInstruction[op.Size + 1];

                context.Copy(d, context.AddIntrinsic(addInst, d, res));
            }
            else
            {
                EmitVectorWidenRnRmTernaryOpZx(context, (op1, op2, op3) =>
                {
                    return context.Add(op1, context.Multiply(op2, op3));
                });
            }
        }

        public static void Umlal_Ve(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitVectorTernaryOpRdByElem(context, Intrinsic.Arm64UmlalVe);
            }
            else
            {
                EmitVectorWidenTernaryOpByElemZx(context, (op1, op2, op3) =>
                {
                    return context.Add(op1, context.Multiply(op2, op3));
                });
            }
        }

        public static void Umlsl_V(ArmEmitterContext context)
        {
            OpCodeSimdReg op = (OpCodeSimdReg)context.CurrOp;

            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitVectorTernaryOpRd(context, Intrinsic.Arm64UmlslV);
            }
            else if (Optimizations.UseSse41 && op.Size < 2)
            {
                Operand d = GetVec(op.Rd);
                Operand n = GetVec(op.Rn);
                Operand m = GetVec(op.Rm);

                if (op.RegisterSize == RegisterSize.Simd128)
                {
                    n = context.AddIntrinsic(Intrinsic.X86Psrldq, n, Const(8));
                    m = context.AddIntrinsic(Intrinsic.X86Psrldq, m, Const(8));
                }

                Intrinsic movInst = op.Size == 0 ? Intrinsic.X86Pmovzxbw : Intrinsic.X86Pmovzxwd;

                n = context.AddIntrinsic(movInst, n);
                m = context.AddIntrinsic(movInst, m);

                Intrinsic mullInst = op.Size == 0 ? Intrinsic.X86Pmullw : Intrinsic.X86Pmulld;

                Operand res = context.AddIntrinsic(mullInst, n, m);

                Intrinsic subInst = X86PsubInstruction[op.Size + 1];

                context.Copy(d, context.AddIntrinsic(subInst, d, res));
            }
            else
            {
                EmitVectorWidenRnRmTernaryOpZx(context, (op1, op2, op3) =>
                {
                    return context.Subtract(op1, context.Multiply(op2, op3));
                });
            }
        }

        public static void Umlsl_Ve(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitVectorTernaryOpRdByElem(context, Intrinsic.Arm64UmlslVe);
            }
            else
            {
                EmitVectorWidenTernaryOpByElemZx(context, (op1, op2, op3) =>
                {
                    return context.Subtract(op1, context.Multiply(op2, op3));
                });
            }
        }

        public static void Umull_V(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitVectorBinaryOp(context, Intrinsic.Arm64UmullV);
            }
            else
            {
                EmitVectorWidenRnRmBinaryOpZx(context, (op1, op2) => context.Multiply(op1, op2));
            }
        }

        public static void Umull_Ve(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitVectorBinaryOpByElem(context, Intrinsic.Arm64UmullVe);
            }
            else
            {
                EmitVectorWidenBinaryOpByElemZx(context, (op1, op2) => context.Multiply(op1, op2));
            }
        }

        public static void Uqadd_S(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitScalarSaturatingBinaryOp(context, Intrinsic.Arm64UqaddS);
            }
            else
            {
                EmitScalarSaturatingBinaryOpZx(context, SaturatingFlags.Add);
            }
        }

        public static void Uqadd_V(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitVectorSaturatingBinaryOp(context, Intrinsic.Arm64UqaddV);
            }
            else
            {
                EmitVectorSaturatingBinaryOpZx(context, SaturatingFlags.Add);
            }
        }

        public static void Uqsub_S(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitScalarSaturatingBinaryOp(context, Intrinsic.Arm64UqsubS);
            }
            else
            {
                EmitScalarSaturatingBinaryOpZx(context, SaturatingFlags.Sub);
            }
        }

        public static void Uqsub_V(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitVectorSaturatingBinaryOp(context, Intrinsic.Arm64UqsubV);
            }
            else
            {
                EmitVectorSaturatingBinaryOpZx(context, SaturatingFlags.Sub);
            }
        }

        public static void Uqxtn_S(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitScalarSaturatingBinaryOpRd(context, Intrinsic.Arm64UqxtnS);
            }
            else
            {
                EmitSaturatingNarrowOp(context, SaturatingNarrowFlags.ScalarZxZx);
            }
        }

        public static void Uqxtn_V(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitVectorSaturatingBinaryOpRd(context, Intrinsic.Arm64UqxtnV);
            }
            else
            {
                EmitSaturatingNarrowOp(context, SaturatingNarrowFlags.VectorZxZx);
            }
        }

        public static void Urhadd_V(ArmEmitterContext context)
        {
            OpCodeSimdReg op = (OpCodeSimdReg)context.CurrOp;

            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitVectorBinaryOp(context, Intrinsic.Arm64UrhaddV);
            }
            else if (Optimizations.UseSse2 && op.Size < 2)
            {
                Operand n = GetVec(op.Rn);
                Operand m = GetVec(op.Rm);

                Intrinsic avgInst = op.Size == 0 ? Intrinsic.X86Pavgb : Intrinsic.X86Pavgw;

                Operand res = context.AddIntrinsic(avgInst, n, m);

                if (op.RegisterSize == RegisterSize.Simd64)
                {
                    res = context.VectorZeroUpper64(res);
                }

                context.Copy(GetVec(op.Rd), res);
            }
            else
            {
                EmitVectorBinaryOpZx(context, (op1, op2) =>
                {
                    Operand res = context.Add(op1, op2);

                    res = context.Add(res, Const(1L));

                    return context.ShiftRightUI(res, Const(1));
                });
            }
        }

        public static void Usqadd_S(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitScalarSaturatingBinaryOpRd(context, Intrinsic.Arm64UsqaddS);
            }
            else
            {
                EmitScalarSaturatingBinaryOpZx(context, SaturatingFlags.Accumulate);
            }
        }

        public static void Usqadd_V(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitVectorSaturatingBinaryOpRd(context, Intrinsic.Arm64UsqaddV);
            }
            else
            {
                EmitVectorSaturatingBinaryOpZx(context, SaturatingFlags.Accumulate);
            }
        }

        public static void Usubl_V(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitVectorBinaryOp(context, Intrinsic.Arm64UsublV);
            }
            else if (Optimizations.UseSse41)
            {
                OpCodeSimdReg op = (OpCodeSimdReg)context.CurrOp;

                Operand n = GetVec(op.Rn);
                Operand m = GetVec(op.Rm);

                if (op.RegisterSize == RegisterSize.Simd128)
                {
                    n = context.AddIntrinsic(Intrinsic.X86Psrldq, n, Const(8));
                    m = context.AddIntrinsic(Intrinsic.X86Psrldq, m, Const(8));
                }

                Intrinsic movInst = X86PmovzxInstruction[op.Size];

                n = context.AddIntrinsic(movInst, n);
                m = context.AddIntrinsic(movInst, m);

                Intrinsic subInst = X86PsubInstruction[op.Size + 1];

                context.Copy(GetVec(op.Rd), context.AddIntrinsic(subInst, n, m));
            }
            else
            {
                EmitVectorWidenRnRmBinaryOpZx(context, (op1, op2) => context.Subtract(op1, op2));
            }
        }

        public static void Usubw_V(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitVectorBinaryOp(context, Intrinsic.Arm64UsubwV);
            }
            else if (Optimizations.UseSse41)
            {
                OpCodeSimdReg op = (OpCodeSimdReg)context.CurrOp;

                Operand n = GetVec(op.Rn);
                Operand m = GetVec(op.Rm);

                if (op.RegisterSize == RegisterSize.Simd128)
                {
                    m = context.AddIntrinsic(Intrinsic.X86Psrldq, m, Const(8));
                }

                Intrinsic movInst = X86PmovzxInstruction[op.Size];

                m = context.AddIntrinsic(movInst, m);

                Intrinsic subInst = X86PsubInstruction[op.Size + 1];

                context.Copy(GetVec(op.Rd), context.AddIntrinsic(subInst, n, m));
            }
            else
            {
                EmitVectorWidenRmBinaryOpZx(context, (op1, op2) => context.Subtract(op1, op2));
            }
        }

        private static Operand EmitAbs(ArmEmitterContext context, Operand value)
        {
            Operand isPositive = context.ICompareGreaterOrEqual(value, Const(value.Type, 0));

            return context.ConditionalSelect(isPositive, value, context.Negate(value));
        }

        private static void EmitAddLongPairwise(ArmEmitterContext context, bool signed, bool accumulate)
        {
            OpCodeSimd op = (OpCodeSimd)context.CurrOp;

            Operand res = context.VectorZero();

            int pairs = op.GetPairsCount() >> op.Size;

            for (int index = 0; index < pairs; index++)
            {
                int pairIndex = index << 1;

                Operand ne0 = EmitVectorExtract(context, op.Rn, pairIndex, op.Size, signed);
                Operand ne1 = EmitVectorExtract(context, op.Rn, pairIndex + 1, op.Size, signed);

                Operand e = context.Add(ne0, ne1);

                if (accumulate)
                {
                    Operand de = EmitVectorExtract(context, op.Rd, index, op.Size + 1, signed);

                    e = context.Add(e, de);
                }

                res = EmitVectorInsert(context, res, e, index, op.Size + 1);
            }

            context.Copy(GetVec(op.Rd), res);
        }

        private static Operand EmitDoublingMultiplyHighHalf(
            ArmEmitterContext context,
            Operand n,
            Operand m,
            bool round)
        {
            OpCodeSimdReg op = (OpCodeSimdReg)context.CurrOp;

            int eSize = 8 << op.Size;

            Operand res = context.Multiply(n, m);

            if (!round)
            {
                res = context.ShiftRightSI(res, Const(eSize - 1));
            }
            else
            {
                long roundConst = 1L << (eSize - 1);

                res = context.ShiftLeft(res, Const(1));

                res = context.Add(res, Const(roundConst));

                res = context.ShiftRightSI(res, Const(eSize));

                Operand isIntMin = context.ICompareEqual(res, Const((long)int.MinValue));

                res = context.ConditionalSelect(isIntMin, context.Negate(res), res);
            }

            return res;
        }

        private static void EmitHighNarrow(ArmEmitterContext context, Func2I emit, bool round)
        {
            OpCodeSimdReg op = (OpCodeSimdReg)context.CurrOp;

            int elems = 8 >> op.Size;
            int eSize = 8 << op.Size;

            int part = op.RegisterSize == RegisterSize.Simd128 ? elems : 0;

            Operand d = GetVec(op.Rd);

            Operand res = part == 0 ? context.VectorZero() : context.Copy(d);

            long roundConst = 1L << (eSize - 1);

            for (int index = 0; index < elems; index++)
            {
                Operand ne = EmitVectorExtractZx(context, op.Rn, index, op.Size + 1);
                Operand me = EmitVectorExtractZx(context, op.Rm, index, op.Size + 1);

                Operand de = emit(ne, me);

                if (round)
                {
                    de = context.Add(de, Const(roundConst));
                }

                de = context.ShiftRightUI(de, Const(eSize));

                res = EmitVectorInsert(context, res, de, part + index, op.Size);
            }

            context.Copy(d, res);
        }

        private static Operand EmitMax64Op(ArmEmitterContext context, Operand op1, Operand op2, bool signed)
        {
            Debug.Assert(op1.Type == OperandType.I64 && op2.Type == OperandType.I64);

            Operand cmp = signed
                ? context.ICompareGreaterOrEqual(op1, op2)
                : context.ICompareGreaterOrEqualUI(op1, op2);

            return context.ConditionalSelect(cmp, op1, op2);
        }

        private static Operand EmitMin64Op(ArmEmitterContext context, Operand op1, Operand op2, bool signed)
        {
            Debug.Assert(op1.Type == OperandType.I64 && op2.Type == OperandType.I64);

            Operand cmp = signed
                ? context.ICompareLessOrEqual(op1, op2)
                : context.ICompareLessOrEqualUI(op1, op2);

            return context.ConditionalSelect(cmp, op1, op2);
        }

        private static void EmitSse41ScalarRoundOpF(ArmEmitterContext context, FPRoundingMode roundMode)
        {
            OpCodeSimd op = (OpCodeSimd)context.CurrOp;

            Operand n = GetVec(op.Rn);

            Operand res;

            if (roundMode != FPRoundingMode.ToNearestAway)
            {
                Intrinsic inst = (op.Size & 1) != 0 ? Intrinsic.X86Roundsd : Intrinsic.X86Roundss;

                res = context.AddIntrinsic(inst, n, Const(X86GetRoundControl(roundMode)));
            }
            else
            {
                res = EmitSse41RoundToNearestWithTiesToAwayOpF(context, n, scalar: true);
            }

            if ((op.Size & 1) != 0)
            {
                res = context.VectorZeroUpper64(res);
            }
            else
            {
                res = context.VectorZeroUpper96(res);
            }

            context.Copy(GetVec(op.Rd), res);
        }

        private static void EmitSse41VectorRoundOpF(ArmEmitterContext context, FPRoundingMode roundMode)
        {
            OpCodeSimd op = (OpCodeSimd)context.CurrOp;

            Operand n = GetVec(op.Rn);

            Operand res;

            if (roundMode != FPRoundingMode.ToNearestAway)
            {
                Intrinsic inst = (op.Size & 1) != 0 ? Intrinsic.X86Roundpd : Intrinsic.X86Roundps;

                res = context.AddIntrinsic(inst, n, Const(X86GetRoundControl(roundMode)));
            }
            else
            {
                res = EmitSse41RoundToNearestWithTiesToAwayOpF(context, n, scalar: false);
            }

            if (op.RegisterSize == RegisterSize.Simd64)
            {
                res = context.VectorZeroUpper64(res);
            }

            context.Copy(GetVec(op.Rd), res);
        }

        private static Operand EmitSse41Round32Exp8OpF(ArmEmitterContext context, Operand value, bool scalar)
        {
            Operand roundMask;
            Operand truncMask;
            Operand expMask;

            if (scalar)
            {
                roundMask = X86GetScalar(context, 0x4000);
                truncMask = X86GetScalar(context, unchecked((int)0xFFFF8000));
                expMask = X86GetScalar(context, 0x7F800000);
            }
            else
            {
                roundMask = X86GetAllElements(context, 0x4000);
                truncMask = X86GetAllElements(context, unchecked((int)0xFFFF8000));
                expMask = X86GetAllElements(context, 0x7F800000);
            }

            Operand oValue = value;
            Operand masked = context.AddIntrinsic(Intrinsic.X86Pand, value, expMask);
            Operand isNaNInf = context.AddIntrinsic(Intrinsic.X86Pcmpeqd, masked, expMask);

            value = context.AddIntrinsic(Intrinsic.X86Paddd, value, roundMask);
            value = context.AddIntrinsic(Intrinsic.X86Pand, value, truncMask);

            return context.AddIntrinsic(Intrinsic.X86Blendvps, value, oValue, isNaNInf);
        }

        private static Operand EmitSse41RecipStepSelectOpF(
            ArmEmitterContext context,
            Operand n,
            Operand m,
            Operand res,
            Operand mask,
            bool scalar,
            int sizeF)
        {
            Intrinsic cmpOp;
            Intrinsic shlOp;
            Intrinsic blendOp;
            Operand zero = context.VectorZero();
            Operand expMask;

            if (sizeF == 0)
            {
                cmpOp = Intrinsic.X86Pcmpeqd;
                shlOp = Intrinsic.X86Pslld;
                blendOp = Intrinsic.X86Blendvps;
                expMask = scalar ? X86GetScalar(context, 0x7F800000 << 1) : X86GetAllElements(context, 0x7F800000 << 1);
            }
            else /* if (sizeF == 1) */
            {
                cmpOp = Intrinsic.X86Pcmpeqq;
                shlOp = Intrinsic.X86Psllq;
                blendOp = Intrinsic.X86Blendvpd;
                expMask = scalar ? X86GetScalar(context, 0x7FF0000000000000L << 1) : X86GetAllElements(context, 0x7FF0000000000000L << 1);
            }

            n = context.AddIntrinsic(shlOp, n, Const(1));
            m = context.AddIntrinsic(shlOp, m, Const(1));

            Operand nZero = context.AddIntrinsic(cmpOp, n, zero);
            Operand mZero = context.AddIntrinsic(cmpOp, m, zero);
            Operand nInf = context.AddIntrinsic(cmpOp, n, expMask);
            Operand mInf = context.AddIntrinsic(cmpOp, m, expMask);

            Operand nmZero = context.AddIntrinsic(Intrinsic.X86Por, nZero, mZero);
            Operand nmInf = context.AddIntrinsic(Intrinsic.X86Por, nInf, mInf);
            Operand nmZeroInf = context.AddIntrinsic(Intrinsic.X86Pand, nmZero, nmInf);

            return context.AddIntrinsic(blendOp, res, mask, nmZeroInf);
        }

        public static void EmitSse2VectorIsNaNOpF(
            ArmEmitterContext context,
            Operand opF,
            out Operand qNaNMask,
            out Operand sNaNMask,
            bool? isQNaN = null)
        {
            IOpCodeSimd op = (IOpCodeSimd)context.CurrOp;

            if ((op.Size & 1) == 0)
            {
                const int QBit = 22;

                Operand qMask = X86GetAllElements(context, 1 << QBit);

                Operand mask1 = context.AddIntrinsic(Intrinsic.X86Cmpps, opF, opF, Const((int)CmpCondition.UnorderedQ));

                Operand mask2 = context.AddIntrinsic(Intrinsic.X86Pand, opF, qMask);
                mask2 = context.AddIntrinsic(Intrinsic.X86Cmpps, mask2, qMask, Const((int)CmpCondition.Equal));

                qNaNMask = isQNaN == null || (bool)isQNaN ? context.AddIntrinsic(Intrinsic.X86Andps, mask2, mask1) : default;
                sNaNMask = isQNaN == null || !(bool)isQNaN ? context.AddIntrinsic(Intrinsic.X86Andnps, mask2, mask1) : default;
            }
            else /* if ((op.Size & 1) == 1) */
            {
                const int QBit = 51;

                Operand qMask = X86GetAllElements(context, 1L << QBit);

                Operand mask1 = context.AddIntrinsic(Intrinsic.X86Cmppd, opF, opF, Const((int)CmpCondition.UnorderedQ));

                Operand mask2 = context.AddIntrinsic(Intrinsic.X86Pand, opF, qMask);
                mask2 = context.AddIntrinsic(Intrinsic.X86Cmppd, mask2, qMask, Const((int)CmpCondition.Equal));

                qNaNMask = isQNaN == null || (bool)isQNaN ? context.AddIntrinsic(Intrinsic.X86Andpd, mask2, mask1) : default;
                sNaNMask = isQNaN == null || !(bool)isQNaN ? context.AddIntrinsic(Intrinsic.X86Andnpd, mask2, mask1) : default;
            }
        }

        public static Operand EmitSse41ProcessNaNsOpF(
            ArmEmitterContext context,
            Func2I emit,
            bool scalar,
            Operand n = default,
            Operand m = default)
        {
            Operand nCopy = n == default ? context.Copy(GetVec(((OpCodeSimdReg)context.CurrOp).Rn)) : n;
            Operand mCopy = m == default ? context.Copy(GetVec(((OpCodeSimdReg)context.CurrOp).Rm)) : m;

            EmitSse2VectorIsNaNOpF(context, nCopy, out Operand nQNaNMask, out Operand nSNaNMask);
            EmitSse2VectorIsNaNOpF(context, mCopy, out _, out Operand mSNaNMask, isQNaN: false);

            int sizeF = ((IOpCodeSimd)context.CurrOp).Size & 1;

            if (sizeF == 0)
            {
                const int QBit = 22;

                Operand qMask = scalar ? X86GetScalar(context, 1 << QBit) : X86GetAllElements(context, 1 << QBit);

                Operand resNaNMask = context.AddIntrinsic(Intrinsic.X86Pandn, mSNaNMask, nQNaNMask);
                resNaNMask = context.AddIntrinsic(Intrinsic.X86Por, resNaNMask, nSNaNMask);

                Operand resNaN = context.AddIntrinsic(Intrinsic.X86Blendvps, mCopy, nCopy, resNaNMask);
                resNaN = context.AddIntrinsic(Intrinsic.X86Por, resNaN, qMask);

                Operand resMask = context.AddIntrinsic(Intrinsic.X86Cmpps, nCopy, mCopy, Const((int)CmpCondition.OrderedQ));

                Operand res = context.AddIntrinsic(Intrinsic.X86Blendvps, resNaN, emit(nCopy, mCopy), resMask);

                if (n != default || m != default)
                {
                    return res;
                }

                if (scalar)
                {
                    res = context.VectorZeroUpper96(res);
                }
                else if (((OpCodeSimdReg)context.CurrOp).RegisterSize == RegisterSize.Simd64)
                {
                    res = context.VectorZeroUpper64(res);
                }

                context.Copy(GetVec(((OpCodeSimdReg)context.CurrOp).Rd), res);

                return default;
            }
            else /* if (sizeF == 1) */
            {
                const int QBit = 51;

                Operand qMask = scalar ? X86GetScalar(context, 1L << QBit) : X86GetAllElements(context, 1L << QBit);

                Operand resNaNMask = context.AddIntrinsic(Intrinsic.X86Pandn, mSNaNMask, nQNaNMask);
                resNaNMask = context.AddIntrinsic(Intrinsic.X86Por, resNaNMask, nSNaNMask);

                Operand resNaN = context.AddIntrinsic(Intrinsic.X86Blendvpd, mCopy, nCopy, resNaNMask);
                resNaN = context.AddIntrinsic(Intrinsic.X86Por, resNaN, qMask);

                Operand resMask = context.AddIntrinsic(Intrinsic.X86Cmppd, nCopy, mCopy, Const((int)CmpCondition.OrderedQ));

                Operand res = context.AddIntrinsic(Intrinsic.X86Blendvpd, resNaN, emit(nCopy, mCopy), resMask);

                if (n != default || m != default)
                {
                    return res;
                }

                if (scalar)
                {
                    res = context.VectorZeroUpper64(res);
                }

                context.Copy(GetVec(((OpCodeSimdReg)context.CurrOp).Rd), res);

                return default;
            }
        }

        private static Operand EmitSse2VectorMaxMinOpF(ArmEmitterContext context, Operand n, Operand m, bool isMax)
        {
            IOpCodeSimd op = (IOpCodeSimd)context.CurrOp;

            if ((op.Size & 1) == 0)
            {
                Operand mask = X86GetAllElements(context, -0f);

                Operand res = context.AddIntrinsic(isMax ? Intrinsic.X86Maxps : Intrinsic.X86Minps, n, m);
                res = context.AddIntrinsic(Intrinsic.X86Andnps, mask, res);

                Operand resSign = context.AddIntrinsic(isMax ? Intrinsic.X86Pand : Intrinsic.X86Por, n, m);
                resSign = context.AddIntrinsic(Intrinsic.X86Andps, mask, resSign);

                return context.AddIntrinsic(Intrinsic.X86Por, res, resSign);
            }
            else /* if ((op.Size & 1) == 1) */
            {
                Operand mask = X86GetAllElements(context, -0d);

                Operand res = context.AddIntrinsic(isMax ? Intrinsic.X86Maxpd : Intrinsic.X86Minpd, n, m);
                res = context.AddIntrinsic(Intrinsic.X86Andnpd, mask, res);

                Operand resSign = context.AddIntrinsic(isMax ? Intrinsic.X86Pand : Intrinsic.X86Por, n, m);
                resSign = context.AddIntrinsic(Intrinsic.X86Andpd, mask, resSign);

                return context.AddIntrinsic(Intrinsic.X86Por, res, resSign);
            }
        }

        private static Operand EmitSse41MaxMinNumOpF(
            ArmEmitterContext context,
            bool isMaxNum,
            bool scalar,
            Operand n = default,
            Operand m = default)
        {
            Operand nCopy = n == default ? context.Copy(GetVec(((OpCodeSimdReg)context.CurrOp).Rn)) : n;
            Operand mCopy = m == default ? context.Copy(GetVec(((OpCodeSimdReg)context.CurrOp).Rm)) : m;

            EmitSse2VectorIsNaNOpF(context, nCopy, out Operand nQNaNMask, out _, isQNaN: true);
            EmitSse2VectorIsNaNOpF(context, mCopy, out Operand mQNaNMask, out _, isQNaN: true);

            int sizeF = ((IOpCodeSimd)context.CurrOp).Size & 1;

            if (sizeF == 0)
            {
                Operand negInfMask = scalar
                    ? X86GetScalar(context, isMaxNum ? float.NegativeInfinity : float.PositiveInfinity)
                    : X86GetAllElements(context, isMaxNum ? float.NegativeInfinity : float.PositiveInfinity);

                Operand nMask = context.AddIntrinsic(Intrinsic.X86Andnps, mQNaNMask, nQNaNMask);
                Operand mMask = context.AddIntrinsic(Intrinsic.X86Andnps, nQNaNMask, mQNaNMask);

                nCopy = context.AddIntrinsic(Intrinsic.X86Blendvps, nCopy, negInfMask, nMask);
                mCopy = context.AddIntrinsic(Intrinsic.X86Blendvps, mCopy, negInfMask, mMask);

                Operand res = EmitSse41ProcessNaNsOpF(context, (op1, op2) =>
                {
                    return EmitSse2VectorMaxMinOpF(context, op1, op2, isMax: isMaxNum);
                }, scalar: scalar, nCopy, mCopy);

                if (n != default || m != default)
                {
                    return res;
                }

                if (scalar)
                {
                    res = context.VectorZeroUpper96(res);
                }
                else if (((OpCodeSimdReg)context.CurrOp).RegisterSize == RegisterSize.Simd64)
                {
                    res = context.VectorZeroUpper64(res);
                }

                context.Copy(GetVec(((OpCodeSimdReg)context.CurrOp).Rd), res);

                return default;
            }
            else /* if (sizeF == 1) */
            {
                Operand negInfMask = scalar
                    ? X86GetScalar(context, isMaxNum ? double.NegativeInfinity : double.PositiveInfinity)
                    : X86GetAllElements(context, isMaxNum ? double.NegativeInfinity : double.PositiveInfinity);

                Operand nMask = context.AddIntrinsic(Intrinsic.X86Andnpd, mQNaNMask, nQNaNMask);
                Operand mMask = context.AddIntrinsic(Intrinsic.X86Andnpd, nQNaNMask, mQNaNMask);

                nCopy = context.AddIntrinsic(Intrinsic.X86Blendvpd, nCopy, negInfMask, nMask);
                mCopy = context.AddIntrinsic(Intrinsic.X86Blendvpd, mCopy, negInfMask, mMask);

                Operand res = EmitSse41ProcessNaNsOpF(context, (op1, op2) =>
                {
                    return EmitSse2VectorMaxMinOpF(context, op1, op2, isMax: isMaxNum);
                }, scalar: scalar, nCopy, mCopy);

                if (n != default || m != default)
                {
                    return res;
                }

                if (scalar)
                {
                    res = context.VectorZeroUpper64(res);
                }

                context.Copy(GetVec(((OpCodeSimdReg)context.CurrOp).Rd), res);

                return default;
            }
        }

        private enum AddSub
        {
            None,
            Add,
            Subtract,
        }

        private static void EmitSse41VectorMul_AddSub(ArmEmitterContext context, AddSub addSub)
        {
            OpCodeSimdReg op = (OpCodeSimdReg)context.CurrOp;

            Operand n = GetVec(op.Rn);
            Operand m = GetVec(op.Rm);

            Operand res;

            if (op.Size == 0)
            {
                Operand ns8 = context.AddIntrinsic(Intrinsic.X86Psrlw, n, Const(8));
                Operand ms8 = context.AddIntrinsic(Intrinsic.X86Psrlw, m, Const(8));

                res = context.AddIntrinsic(Intrinsic.X86Pmullw, ns8, ms8);

                res = context.AddIntrinsic(Intrinsic.X86Psllw, res, Const(8));

                Operand res2 = context.AddIntrinsic(Intrinsic.X86Pmullw, n, m);

                Operand mask = X86GetAllElements(context, 0x00FF00FF);

                res = context.AddIntrinsic(Intrinsic.X86Pblendvb, res, res2, mask);
            }
            else if (op.Size == 1)
            {
                res = context.AddIntrinsic(Intrinsic.X86Pmullw, n, m);
            }
            else
            {
                res = context.AddIntrinsic(Intrinsic.X86Pmulld, n, m);
            }

            Operand d = GetVec(op.Rd);

            if (addSub == AddSub.Add)
            {
                Intrinsic addInst = X86PaddInstruction[op.Size];

                res = context.AddIntrinsic(addInst, d, res);
            }
            else if (addSub == AddSub.Subtract)
            {
                Intrinsic subInst = X86PsubInstruction[op.Size];

                res = context.AddIntrinsic(subInst, d, res);
            }

            if (op.RegisterSize == RegisterSize.Simd64)
            {
                res = context.VectorZeroUpper64(res);
            }

            context.Copy(d, res);
        }

        private static void EmitSse41VectorSabdOp(
            ArmEmitterContext context,
            OpCodeSimdReg op,
            Operand n,
            Operand m,
            bool isLong)
        {
            int size = isLong ? op.Size + 1 : op.Size;

            Intrinsic cmpgtInst = X86PcmpgtInstruction[size];

            Operand cmpMask = context.AddIntrinsic(cmpgtInst, n, m);

            Intrinsic subInst = X86PsubInstruction[size];

            Operand res = context.AddIntrinsic(subInst, n, m);

            res = context.AddIntrinsic(Intrinsic.X86Pand, cmpMask, res);

            Operand res2 = context.AddIntrinsic(subInst, m, n);

            res2 = context.AddIntrinsic(Intrinsic.X86Pandn, cmpMask, res2);

            res = context.AddIntrinsic(Intrinsic.X86Por, res, res2);

            if (!isLong && op.RegisterSize == RegisterSize.Simd64)
            {
                res = context.VectorZeroUpper64(res);
            }

            context.Copy(GetVec(op.Rd), res);
        }

        private static void EmitSse41VectorUabdOp(
            ArmEmitterContext context,
            OpCodeSimdReg op,
            Operand n,
            Operand m,
            bool isLong)
        {
            int size = isLong ? op.Size + 1 : op.Size;

            Intrinsic maxInst = X86PmaxuInstruction[size];

            Operand max = context.AddIntrinsic(maxInst, m, n);

            Intrinsic cmpeqInst = X86PcmpeqInstruction[size];

            Operand cmpMask = context.AddIntrinsic(cmpeqInst, max, m);

            Operand onesMask = X86GetAllElements(context, -1L);

            cmpMask = context.AddIntrinsic(Intrinsic.X86Pandn, cmpMask, onesMask);

            Intrinsic subInst = X86PsubInstruction[size];

            Operand res = context.AddIntrinsic(subInst, n, m);
            Operand res2 = context.AddIntrinsic(subInst, m, n);

            res = context.AddIntrinsic(Intrinsic.X86Pand, cmpMask, res);
            res2 = context.AddIntrinsic(Intrinsic.X86Pandn, cmpMask, res2);

            res = context.AddIntrinsic(Intrinsic.X86Por, res, res2);

            if (!isLong && op.RegisterSize == RegisterSize.Simd64)
            {
                res = context.VectorZeroUpper64(res);
            }

            context.Copy(GetVec(op.Rd), res);
        }

        private static Operand EmitSse2Sll_128(ArmEmitterContext context, Operand op, int shift)
        {
            // The upper part of op is assumed to be zero.
            Debug.Assert(shift >= 0 && shift < 64);

            if (shift == 0)
            {
                return op;
            }

            Operand high = context.AddIntrinsic(Intrinsic.X86Pslldq, op, Const(8));
            high = context.AddIntrinsic(Intrinsic.X86Psrlq, high, Const(64 - shift));

            Operand low = context.AddIntrinsic(Intrinsic.X86Psllq, op, Const(shift));

            return context.AddIntrinsic(Intrinsic.X86Por, high, low);
        }
    }
}
