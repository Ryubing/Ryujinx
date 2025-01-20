using ARMeilleure.Decoders;
using ARMeilleure.IntermediateRepresentation;
using ARMeilleure.State;
using ARMeilleure.Translation;
using System;
using System.Diagnostics;
using System.Reflection;
using static ARMeilleure.Instructions.InstEmitHelper;
using static ARMeilleure.Instructions.InstEmitSimdHelper;
using static ARMeilleure.IntermediateRepresentation.Operand.Factory;

namespace ARMeilleure.Instructions
{
    using Func1I = Func<Operand, Operand>;

    static partial class InstEmit
    {
        public static void Fcvt_S(ArmEmitterContext context)
        {
            OpCodeSimd op = (OpCodeSimd)context.CurrOp;

            if (op.Size == 0 && op.Opc == 1) // Single -> Double.
            {
                if (Optimizations.UseSse2)
                {
                    Operand n = GetVec(op.Rn);

                    Operand res = context.AddIntrinsic(Intrinsic.X86Cvtss2sd, context.VectorZero(), n);

                    context.Copy(GetVec(op.Rd), res);
                }
                else
                {
                    Operand ne = context.VectorExtract(OperandType.FP32, GetVec(op.Rn), 0);

                    Operand res = context.ConvertToFP(OperandType.FP64, ne);

                    context.Copy(GetVec(op.Rd), context.VectorInsert(context.VectorZero(), res, 0));
                }
            }
            else if (op.Size == 1 && op.Opc == 0) // Double -> Single.
            {
                if (Optimizations.UseSse2)
                {
                    Operand n = GetVec(op.Rn);

                    Operand res = context.AddIntrinsic(Intrinsic.X86Cvtsd2ss, context.VectorZero(), n);

                    context.Copy(GetVec(op.Rd), res);
                }
                else
                {
                    Operand ne = context.VectorExtract(OperandType.FP64, GetVec(op.Rn), 0);

                    Operand res = context.ConvertToFP(OperandType.FP32, ne);

                    context.Copy(GetVec(op.Rd), context.VectorInsert(context.VectorZero(), res, 0));
                }
            }
            else if (op.Size == 0 && op.Opc == 3) // Single -> Half.
            {
                if (Optimizations.UseF16c)
                {
                    Debug.Assert(!Optimizations.ForceLegacySse);

                    Operand n = GetVec(op.Rn);

                    Operand res = context.AddIntrinsic(Intrinsic.X86Vcvtps2ph, n, Const(X86GetRoundControl(FPRoundingMode.ToNearest)));
                    res = context.AddIntrinsic(Intrinsic.X86Pslldq, res, Const(14)); // VectorZeroUpper112()
                    res = context.AddIntrinsic(Intrinsic.X86Psrldq, res, Const(14));

                    context.Copy(GetVec(op.Rd), res);
                }
                else
                {
                    Operand ne = context.VectorExtract(OperandType.FP32, GetVec(op.Rn), 0);

                    context.StoreToContext();
                    Operand res = context.Call(typeof(SoftFloat32_16).GetMethod(nameof(SoftFloat32_16.FPConvert)), ne);
                    context.LoadFromContext();

                    res = context.ZeroExtend16(OperandType.I64, res);

                    context.Copy(GetVec(op.Rd), EmitVectorInsert(context, context.VectorZero(), res, 0, 1));
                }
            }
            else if (op.Size == 3 && op.Opc == 0) // Half -> Single.
            {
                if (Optimizations.UseF16c)
                {
                    Debug.Assert(!Optimizations.ForceLegacySse);

                    Operand res = context.AddIntrinsic(Intrinsic.X86Vcvtph2ps, GetVec(op.Rn));
                    res = context.VectorZeroUpper96(res);

                    context.Copy(GetVec(op.Rd), res);
                }
                else
                {
                    Operand ne = EmitVectorExtractZx(context, op.Rn, 0, 1);

                    context.StoreToContext();
                    Operand res = context.Call(typeof(SoftFloat16_32).GetMethod(nameof(SoftFloat16_32.FPConvert)), ne);
                    context.LoadFromContext();

                    context.Copy(GetVec(op.Rd), context.VectorInsert(context.VectorZero(), res, 0));
                }
            }
            else if (op.Size == 1 && op.Opc == 3) // Double -> Half.
            {
                if (Optimizations.UseF16c)
                {
                    Debug.Assert(!Optimizations.ForceLegacySse);

                    Operand n = GetVec(op.Rn);

                    Operand res = context.AddIntrinsic(Intrinsic.X86Cvtsd2ss, context.VectorZero(), n);
                    res = context.AddIntrinsic(Intrinsic.X86Vcvtps2ph, res, Const(X86GetRoundControl(FPRoundingMode.ToNearest)));

                    context.Copy(GetVec(op.Rd), res);
                }
                else
                {
                    Operand ne = context.VectorExtract(OperandType.FP64, GetVec(op.Rn), 0);

                    context.StoreToContext();
                    Operand res = context.Call(typeof(SoftFloat64_16).GetMethod(nameof(SoftFloat64_16.FPConvert)), ne);
                    context.LoadFromContext();

                    res = context.ZeroExtend16(OperandType.I64, res);

                    context.Copy(GetVec(op.Rd), EmitVectorInsert(context, context.VectorZero(), res, 0, 1));
                }
            }
            else if (op.Size == 3 && op.Opc == 1) // Half -> Double.
            {
                if (Optimizations.UseF16c)
                {
                    Operand n = GetVec(op.Rn);

                    Operand res = context.AddIntrinsic(Intrinsic.X86Vcvtph2ps, GetVec(op.Rn));
                    res = context.AddIntrinsic(Intrinsic.X86Cvtss2sd, context.VectorZero(), res);
                    res = context.VectorZeroUpper64(res);

                    context.Copy(GetVec(op.Rd), res);
                }
                else
                {
                    Operand ne = EmitVectorExtractZx(context, op.Rn, 0, 1);

                    context.StoreToContext();
                    Operand res = context.Call(typeof(SoftFloat16_64).GetMethod(nameof(SoftFloat16_64.FPConvert)), ne);
                    context.LoadFromContext();

                    context.Copy(GetVec(op.Rd), context.VectorInsert(context.VectorZero(), res, 0));
                }
            }
            else // Invalid encoding.
            {
                Debug.Assert(false, $"type == {op.Size} && opc == {op.Opc}");
            }
        }

        public static void Fcvtas_Gp(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitScalarUnaryOpFToGp(context, Intrinsic.Arm64FcvtasGp);
            }
            else if (Optimizations.UseSse41)
            {
                EmitSse41Fcvts_Gp(context, FPRoundingMode.ToNearestAway, isFixed: false);
            }
            else
            {
                EmitFcvt_s_Gp(context, (op1) => EmitRoundMathCall(context, MidpointRounding.AwayFromZero, op1));
            }
        }

        public static void Fcvtas_S(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitScalarUnaryOpF(context, Intrinsic.Arm64FcvtasS);
            }
            else if (Optimizations.UseSse41)
            {
                EmitSse41FcvtsOpF(context, FPRoundingMode.ToNearestAway, scalar: true);
            }
            else
            {
                EmitFcvt(context, (op1) => EmitRoundMathCall(context, MidpointRounding.AwayFromZero, op1), signed: true, scalar: true);
            }
        }

        public static void Fcvtas_V(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitVectorUnaryOpF(context, Intrinsic.Arm64FcvtasS);
            }
            else if (Optimizations.UseSse41)
            {
                EmitSse41FcvtsOpF(context, FPRoundingMode.ToNearestAway, scalar: false);
            }
            else
            {
                EmitFcvt(context, (op1) => EmitRoundMathCall(context, MidpointRounding.AwayFromZero, op1), signed: true, scalar: false);
            }
        }

        public static void Fcvtau_Gp(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitScalarUnaryOpFToGp(context, Intrinsic.Arm64FcvtauGp);
            }
            else if (Optimizations.UseSse41)
            {
                EmitSse41Fcvtu_Gp(context, FPRoundingMode.ToNearestAway, isFixed: false);
            }
            else
            {
                EmitFcvt_u_Gp(context, (op1) => EmitRoundMathCall(context, MidpointRounding.AwayFromZero, op1));
            }
        }

        public static void Fcvtau_S(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitScalarUnaryOpF(context, Intrinsic.Arm64FcvtauS);
            }
            else if (Optimizations.UseSse41)
            {
                EmitSse41FcvtuOpF(context, FPRoundingMode.ToNearestAway, scalar: true);
            }
            else
            {
                EmitFcvt(context, (op1) => EmitRoundMathCall(context, MidpointRounding.AwayFromZero, op1), signed: false, scalar: true);
            }
        }

        public static void Fcvtau_V(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitVectorUnaryOpF(context, Intrinsic.Arm64FcvtauV);
            }
            else if (Optimizations.UseSse41)
            {
                EmitSse41FcvtuOpF(context, FPRoundingMode.ToNearestAway, scalar: false);
            }
            else
            {
                EmitFcvt(context, (op1) => EmitRoundMathCall(context, MidpointRounding.AwayFromZero, op1), signed: false, scalar: false);
            }
        }

        public static void Fcvtl_V(ArmEmitterContext context)
        {
            OpCodeSimd op = (OpCodeSimd)context.CurrOp;

            int sizeF = op.Size & 1;

            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitVectorUnaryOpF(context, Intrinsic.Arm64FcvtlV);
            }
            else if (Optimizations.UseSse2 && sizeF == 1)
            {
                Operand n = GetVec(op.Rn);

                Operand res = op.RegisterSize == RegisterSize.Simd128 ? context.AddIntrinsic(Intrinsic.X86Movhlps, n, n) : n;
                res = context.AddIntrinsic(Intrinsic.X86Cvtps2pd, res);

                context.Copy(GetVec(op.Rd), res);
            }
            else if (Optimizations.UseF16c && sizeF == 0)
            {
                Debug.Assert(!Optimizations.ForceLegacySse);

                Operand n = GetVec(op.Rn);

                Operand res = op.RegisterSize == RegisterSize.Simd128 ? context.AddIntrinsic(Intrinsic.X86Movhlps, n, n) : n;
                res = context.AddIntrinsic(Intrinsic.X86Vcvtph2ps, res);

                context.Copy(GetVec(op.Rd), res);
            }
            else
            {
                Operand res = context.VectorZero();

                int elems = 4 >> sizeF;

                int part = op.RegisterSize == RegisterSize.Simd128 ? elems : 0;

                for (int index = 0; index < elems; index++)
                {
                    if (sizeF == 0)
                    {
                        Operand ne = EmitVectorExtractZx(context, op.Rn, part + index, 1);

                        context.StoreToContext();
                        Operand e = context.Call(typeof(SoftFloat16_32).GetMethod(nameof(SoftFloat16_32.FPConvert)), ne);
                        context.LoadFromContext();

                        res = context.VectorInsert(res, e, index);
                    }
                    else /* if (sizeF == 1) */
                    {
                        Operand ne = context.VectorExtract(OperandType.FP32, GetVec(op.Rn), part + index);

                        Operand e = context.ConvertToFP(OperandType.FP64, ne);

                        res = context.VectorInsert(res, e, index);
                    }
                }

                context.Copy(GetVec(op.Rd), res);
            }
        }

        public static void Fcvtms_Gp(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitScalarUnaryOpFToGp(context, Intrinsic.Arm64FcvtmsGp);
            }
            else if (Optimizations.UseSse41)
            {
                EmitSse41Fcvts_Gp(context, FPRoundingMode.TowardsMinusInfinity, isFixed: false);
            }
            else
            {
                EmitFcvt_s_Gp(context, (op1) => EmitUnaryMathCall(context, nameof(MathHelper.Floor), op1));
            }
        }

        public static void Fcvtms_V(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitVectorUnaryOpF(context, Intrinsic.Arm64FcvtmsV);
            }
            else if (Optimizations.UseSse41)
            {
                EmitSse41FcvtsOpF(context, FPRoundingMode.TowardsMinusInfinity, scalar: false);
            }
            else
            {
                EmitFcvt(context, (op1) => EmitUnaryMathCall(context, nameof(MathHelper.Floor), op1), signed: true, scalar: false);
            }
        }

        public static void Fcvtmu_Gp(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitScalarUnaryOpFToGp(context, Intrinsic.Arm64FcvtmuGp);
            }
            else if (Optimizations.UseSse41)
            {
                EmitSse41Fcvtu_Gp(context, FPRoundingMode.TowardsMinusInfinity, isFixed: false);
            }
            else
            {
                EmitFcvt_u_Gp(context, (op1) => EmitUnaryMathCall(context, nameof(MathHelper.Floor), op1));
            }
        }

        public static void Fcvtn_V(ArmEmitterContext context)
        {
            OpCodeSimd op = (OpCodeSimd)context.CurrOp;

            int sizeF = op.Size & 1;

            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitVectorBinaryOpFRd(context, Intrinsic.Arm64FcvtnV);
            }
            else if (Optimizations.UseSse2 && sizeF == 1)
            {
                Operand d = GetVec(op.Rd);

                Intrinsic movInst = op.RegisterSize == RegisterSize.Simd128 ? Intrinsic.X86Movlhps : Intrinsic.X86Movhlps;

                Operand nInt = context.AddIntrinsic(Intrinsic.X86Cvtpd2ps, GetVec(op.Rn));
                nInt = context.AddIntrinsic(Intrinsic.X86Movlhps, nInt, nInt);

                Operand res = context.VectorZeroUpper64(d);
                res = context.AddIntrinsic(movInst, res, nInt);

                context.Copy(d, res);
            }
            else if (Optimizations.UseF16c && sizeF == 0)
            {
                Debug.Assert(!Optimizations.ForceLegacySse);

                Operand d = GetVec(op.Rd);
                Operand n = GetVec(op.Rn);

                Intrinsic movInst = op.RegisterSize == RegisterSize.Simd128 ? Intrinsic.X86Movlhps : Intrinsic.X86Movhlps;

                Operand nInt = context.AddIntrinsic(Intrinsic.X86Vcvtps2ph, n, Const(X86GetRoundControl(FPRoundingMode.ToNearest)));
                nInt = context.AddIntrinsic(Intrinsic.X86Movlhps, nInt, nInt);

                Operand res = context.VectorZeroUpper64(d);
                res = context.AddIntrinsic(movInst, res, nInt);

                context.Copy(d, res);
            }
            else
            {
                OperandType type = sizeF == 0 ? OperandType.FP32 : OperandType.FP64;

                int elems = 4 >> sizeF;

                int part = op.RegisterSize == RegisterSize.Simd128 ? elems : 0;

                Operand d = GetVec(op.Rd);

                Operand res = part == 0 ? context.VectorZero() : context.Copy(d);

                for (int index = 0; index < elems; index++)
                {
                    Operand ne = context.VectorExtract(type, GetVec(op.Rn), index);

                    if (sizeF == 0)
                    {
                        context.StoreToContext();
                        Operand e = context.Call(typeof(SoftFloat32_16).GetMethod(nameof(SoftFloat32_16.FPConvert)), ne);
                        context.LoadFromContext();

                        res = EmitVectorInsert(context, res, e, part + index, 1);
                    }
                    else /* if (sizeF == 1) */
                    {
                        Operand e = context.ConvertToFP(OperandType.FP32, ne);

                        res = context.VectorInsert(res, e, part + index);
                    }
                }

                context.Copy(d, res);
            }
        }

        public static void Fcvtns_Gp(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitScalarUnaryOpFToGp(context, Intrinsic.Arm64FcvtnsGp);
            }
            else if (Optimizations.UseSse41)
            {
                EmitSse41Fcvts_Gp(context, FPRoundingMode.ToNearest, isFixed: false);
            }
            else
            {
                EmitFcvt_s_Gp(context, (op1) => EmitRoundMathCall(context, MidpointRounding.ToEven, op1));
            }
        }

        public static void Fcvtns_S(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitScalarUnaryOpF(context, Intrinsic.Arm64FcvtnsS);
            }
            else if (Optimizations.UseSse41)
            {
                EmitSse41FcvtsOpF(context, FPRoundingMode.ToNearest, scalar: true);
            }
            else
            {
                EmitFcvt(context, (op1) => EmitRoundMathCall(context, MidpointRounding.ToEven, op1), signed: true, scalar: true);
            }
        }

        public static void Fcvtns_V(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitVectorUnaryOpF(context, Intrinsic.Arm64FcvtnsV);
            }
            else if (Optimizations.UseSse41)
            {
                EmitSse41FcvtsOpF(context, FPRoundingMode.ToNearest, scalar: false);
            }
            else
            {
                EmitFcvt(context, (op1) => EmitRoundMathCall(context, MidpointRounding.ToEven, op1), signed: true, scalar: false);
            }
        }

        public static void Fcvtnu_S(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitScalarUnaryOpF(context, Intrinsic.Arm64FcvtnuS);
            }
            else if (Optimizations.UseSse41)
            {
                EmitSse41FcvtuOpF(context, FPRoundingMode.ToNearest, scalar: true);
            }
            else
            {
                EmitFcvt(context, (op1) => EmitRoundMathCall(context, MidpointRounding.ToEven, op1), signed: false, scalar: true);
            }
        }

        public static void Fcvtnu_V(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitVectorUnaryOpF(context, Intrinsic.Arm64FcvtnuV);
            }
            else if (Optimizations.UseSse41)
            {
                EmitSse41FcvtuOpF(context, FPRoundingMode.ToNearest, scalar: false);
            }
            else
            {
                EmitFcvt(context, (op1) => EmitRoundMathCall(context, MidpointRounding.ToEven, op1), signed: false, scalar: false);
            }
        }

        public static void Fcvtps_Gp(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitScalarUnaryOpFToGp(context, Intrinsic.Arm64FcvtpsGp);
            }
            else if (Optimizations.UseSse41)
            {
                EmitSse41Fcvts_Gp(context, FPRoundingMode.TowardsPlusInfinity, isFixed: false);
            }
            else
            {
                EmitFcvt_s_Gp(context, (op1) => EmitUnaryMathCall(context, nameof(MathHelper.Ceiling), op1));
            }
        }

        public static void Fcvtpu_Gp(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitScalarUnaryOpFToGp(context, Intrinsic.Arm64FcvtpuGp);
            }
            else if (Optimizations.UseSse41)
            {
                EmitSse41Fcvtu_Gp(context, FPRoundingMode.TowardsPlusInfinity, isFixed: false);
            }
            else
            {
                EmitFcvt_u_Gp(context, (op1) => EmitUnaryMathCall(context, nameof(MathHelper.Ceiling), op1));
            }
        }

        public static void Fcvtzs_Gp(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitScalarUnaryOpFToGp(context, Intrinsic.Arm64FcvtzsGp);
            }
            else if (Optimizations.UseSse41)
            {
                EmitSse41Fcvts_Gp(context, FPRoundingMode.TowardsZero, isFixed: false);
            }
            else
            {
                EmitFcvt_s_Gp(context, (op1) => op1);
            }
        }

        public static void Fcvtzs_Gp_Fixed(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                OpCodeSimdCvt op = (OpCodeSimdCvt)context.CurrOp;

                InstEmitSimdHelperArm64.EmitScalarConvertBinaryOpFToGp(context, Intrinsic.Arm64FcvtzsGpFixed, op.FBits);
            }
            else if (Optimizations.UseSse41)
            {
                EmitSse41Fcvts_Gp(context, FPRoundingMode.TowardsZero, isFixed: true);
            }
            else
            {
                EmitFcvtzs_Gp_Fixed(context);
            }
        }

        public static void Fcvtzs_S(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitScalarUnaryOpF(context, Intrinsic.Arm64FcvtzsS);
            }
            else if (Optimizations.UseSse41)
            {
                EmitSse41FcvtsOpF(context, FPRoundingMode.TowardsZero, scalar: true);
            }
            else
            {
                EmitFcvtz(context, signed: true, scalar: true);
            }
        }

        public static void Fcvtzs_V(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitVectorUnaryOpF(context, Intrinsic.Arm64FcvtzsV);
            }
            else if (Optimizations.UseSse41)
            {
                EmitSse41FcvtsOpF(context, FPRoundingMode.TowardsZero, scalar: false);
            }
            else
            {
                EmitFcvtz(context, signed: true, scalar: false);
            }
        }

        public static void Fcvtzs_V_Fixed(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitVectorConvertBinaryOpF(context, Intrinsic.Arm64FcvtzsVFixed, GetFBits(context));
            }
            else if (Optimizations.UseSse41)
            {
                EmitSse41FcvtsOpF(context, FPRoundingMode.TowardsZero, scalar: false);
            }
            else
            {
                EmitFcvtz(context, signed: true, scalar: false);
            }
        }

        public static void Fcvtzu_Gp(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitScalarUnaryOpFToGp(context, Intrinsic.Arm64FcvtzuGp);
            }
            else if (Optimizations.UseSse41)
            {
                EmitSse41Fcvtu_Gp(context, FPRoundingMode.TowardsZero, isFixed: false);
            }
            else
            {
                EmitFcvt_u_Gp(context, (op1) => op1);
            }
        }

        public static void Fcvtzu_Gp_Fixed(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                OpCodeSimdCvt op = (OpCodeSimdCvt)context.CurrOp;

                InstEmitSimdHelperArm64.EmitScalarConvertBinaryOpFToGp(context, Intrinsic.Arm64FcvtzuGpFixed, op.FBits);
            }
            else if (Optimizations.UseSse41)
            {
                EmitSse41Fcvtu_Gp(context, FPRoundingMode.TowardsZero, isFixed: true);
            }
            else
            {
                EmitFcvtzu_Gp_Fixed(context);
            }
        }

        public static void Fcvtzu_S(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitScalarUnaryOpF(context, Intrinsic.Arm64FcvtzuS);
            }
            else if (Optimizations.UseSse41)
            {
                EmitSse41FcvtuOpF(context, FPRoundingMode.TowardsZero, scalar: true);
            }
            else
            {
                EmitFcvtz(context, signed: false, scalar: true);
            }
        }

        public static void Fcvtzu_V(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitVectorUnaryOpF(context, Intrinsic.Arm64FcvtzuV);
            }
            else if (Optimizations.UseSse41)
            {
                EmitSse41FcvtuOpF(context, FPRoundingMode.TowardsZero, scalar: false);
            }
            else
            {
                EmitFcvtz(context, signed: false, scalar: false);
            }
        }

        public static void Fcvtzu_V_Fixed(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitVectorConvertBinaryOpF(context, Intrinsic.Arm64FcvtzuVFixed, GetFBits(context));
            }
            else if (Optimizations.UseSse41)
            {
                EmitSse41FcvtuOpF(context, FPRoundingMode.TowardsZero, scalar: false);
            }
            else
            {
                EmitFcvtz(context, signed: false, scalar: false);
            }
        }

        public static void Scvtf_Gp(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitScalarUnaryOpFFromGp(context, Intrinsic.Arm64ScvtfGp);
            }
            else
            {
                OpCodeSimdCvt op = (OpCodeSimdCvt)context.CurrOp;

                Operand res = GetIntOrZR(context, op.Rn);

                if (op.RegisterSize == RegisterSize.Int32)
                {
                    res = context.SignExtend32(OperandType.I64, res);
                }

                res = EmitFPConvert(context, res, op.Size, signed: true);

                context.Copy(GetVec(op.Rd), context.VectorInsert(context.VectorZero(), res, 0));
            }
        }

        public static void Scvtf_Gp_Fixed(ArmEmitterContext context)
        {
            OpCodeSimdCvt op = (OpCodeSimdCvt)context.CurrOp;

            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitScalarConvertBinaryOpFFromGp(context, Intrinsic.Arm64ScvtfGpFixed, op.FBits);
            }
            else
            {
                Operand res = GetIntOrZR(context, op.Rn);

                if (op.RegisterSize == RegisterSize.Int32)
                {
                    res = context.SignExtend32(OperandType.I64, res);
                }

                res = EmitFPConvert(context, res, op.Size, signed: true);

                res = EmitI2fFBitsMul(context, res, op.FBits);

                context.Copy(GetVec(op.Rd), context.VectorInsert(context.VectorZero(), res, 0));
            }
        }

        public static void Scvtf_S(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitScalarUnaryOpF(context, Intrinsic.Arm64ScvtfS);
            }
            else if (Optimizations.UseSse2)
            {
                EmitSse2ScvtfOp(context, scalar: true);
            }
            else
            {
                EmitCvtf(context, signed: true, scalar: true);
            }
        }

        public static void Scvtf_S_Fixed(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitScalarConvertBinaryOpF(context, Intrinsic.Arm64ScvtfSFixed, GetFBits(context));
            }
            else if (Optimizations.UseSse2)
            {
                EmitSse2ScvtfOp(context, scalar: true);
            }
            else
            {
                EmitCvtf(context, signed: true, scalar: true);
            }
        }

        public static void Scvtf_V(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitVectorUnaryOpF(context, Intrinsic.Arm64ScvtfV);
            }
            else if (Optimizations.UseSse2)
            {
                EmitSse2ScvtfOp(context, scalar: false);
            }
            else
            {
                EmitCvtf(context, signed: true, scalar: false);
            }
        }

        public static void Scvtf_V_Fixed(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitVectorConvertBinaryOpF(context, Intrinsic.Arm64ScvtfVFixed, GetFBits(context));
            }
            else if (Optimizations.UseSse2)
            {
                EmitSse2ScvtfOp(context, scalar: false);
            }
            else
            {
                EmitCvtf(context, signed: true, scalar: false);
            }
        }

        public static void Ucvtf_Gp(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitScalarUnaryOpFFromGp(context, Intrinsic.Arm64UcvtfGp);
            }
            else
            {
                OpCodeSimdCvt op = (OpCodeSimdCvt)context.CurrOp;

                Operand res = GetIntOrZR(context, op.Rn);

                res = EmitFPConvert(context, res, op.Size, signed: false);

                context.Copy(GetVec(op.Rd), context.VectorInsert(context.VectorZero(), res, 0));
            }
        }

        public static void Ucvtf_Gp_Fixed(ArmEmitterContext context)
        {
            OpCodeSimdCvt op = (OpCodeSimdCvt)context.CurrOp;

            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitScalarConvertBinaryOpFFromGp(context, Intrinsic.Arm64UcvtfGpFixed, op.FBits);
            }
            else
            {
                Operand res = GetIntOrZR(context, op.Rn);

                res = EmitFPConvert(context, res, op.Size, signed: false);

                res = EmitI2fFBitsMul(context, res, op.FBits);

                context.Copy(GetVec(op.Rd), context.VectorInsert(context.VectorZero(), res, 0));
            }
        }

        public static void Ucvtf_S(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitScalarUnaryOpF(context, Intrinsic.Arm64UcvtfS);
            }
            else if (Optimizations.UseSse2)
            {
                EmitSse2UcvtfOp(context, scalar: true);
            }
            else
            {
                EmitCvtf(context, signed: false, scalar: true);
            }
        }

        public static void Ucvtf_S_Fixed(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitScalarConvertBinaryOpF(context, Intrinsic.Arm64UcvtfSFixed, GetFBits(context));
            }
            else if (Optimizations.UseSse2)
            {
                EmitSse2UcvtfOp(context, scalar: true);
            }
            else
            {
                EmitCvtf(context, signed: false, scalar: true);
            }
        }

        public static void Ucvtf_V(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitVectorUnaryOpF(context, Intrinsic.Arm64UcvtfV);
            }
            else if (Optimizations.UseSse2)
            {
                EmitSse2UcvtfOp(context, scalar: false);
            }
            else
            {
                EmitCvtf(context, signed: false, scalar: false);
            }
        }

        public static void Ucvtf_V_Fixed(ArmEmitterContext context)
        {
            if (Optimizations.UseAdvSimd)
            {
                InstEmitSimdHelperArm64.EmitVectorConvertBinaryOpF(context, Intrinsic.Arm64UcvtfVFixed, GetFBits(context));
            }
            else if (Optimizations.UseSse2)
            {
                EmitSse2UcvtfOp(context, scalar: false);
            }
            else
            {
                EmitCvtf(context, signed: false, scalar: false);
            }
        }

        private static void EmitFcvt(ArmEmitterContext context, Func1I emit, bool signed, bool scalar)
        {
            OpCodeSimd op = (OpCodeSimd)context.CurrOp;

            Operand res = context.VectorZero();

            Operand n = GetVec(op.Rn);

            int sizeF = op.Size & 1;
            int sizeI = sizeF + 2;

            OperandType type = sizeF == 0 ? OperandType.FP32 : OperandType.FP64;

            int elems = !scalar ? op.GetBytesCount() >> sizeI : 1;

            for (int index = 0; index < elems; index++)
            {
                Operand ne = context.VectorExtract(type, n, index);

                Operand e = emit(ne);

                if (sizeF == 0)
                {
                    MethodInfo info = signed
                        ? typeof(SoftFallback).GetMethod(nameof(SoftFallback.SatF32ToS32))
                        : typeof(SoftFallback).GetMethod(nameof(SoftFallback.SatF32ToU32));

                    e = context.Call(info, e);

                    e = context.ZeroExtend32(OperandType.I64, e);
                }
                else /* if (sizeF == 1) */
                {
                    MethodInfo info = signed
                        ? typeof(SoftFallback).GetMethod(nameof(SoftFallback.SatF64ToS64))
                        : typeof(SoftFallback).GetMethod(nameof(SoftFallback.SatF64ToU64));

                    e = context.Call(info, e);
                }

                res = EmitVectorInsert(context, res, e, index, sizeI);
            }

            context.Copy(GetVec(op.Rd), res);
        }

        private static void EmitFcvtz(ArmEmitterContext context, bool signed, bool scalar)
        {
            OpCodeSimd op = (OpCodeSimd)context.CurrOp;

            Operand res = context.VectorZero();

            Operand n = GetVec(op.Rn);

            int sizeF = op.Size & 1;
            int sizeI = sizeF + 2;

            OperandType type = sizeF == 0 ? OperandType.FP32 : OperandType.FP64;

            int fBits = GetFBits(context);

            int elems = !scalar ? op.GetBytesCount() >> sizeI : 1;

            for (int index = 0; index < elems; index++)
            {
                Operand ne = context.VectorExtract(type, n, index);

                Operand e = EmitF2iFBitsMul(context, ne, fBits);

                if (sizeF == 0)
                {
                    MethodInfo info = signed
                        ? typeof(SoftFallback).GetMethod(nameof(SoftFallback.SatF32ToS32))
                        : typeof(SoftFallback).GetMethod(nameof(SoftFallback.SatF32ToU32));

                    e = context.Call(info, e);

                    e = context.ZeroExtend32(OperandType.I64, e);
                }
                else /* if (sizeF == 1) */
                {
                    MethodInfo info = signed
                        ? typeof(SoftFallback).GetMethod(nameof(SoftFallback.SatF64ToS64))
                        : typeof(SoftFallback).GetMethod(nameof(SoftFallback.SatF64ToU64));

                    e = context.Call(info, e);
                }

                res = EmitVectorInsert(context, res, e, index, sizeI);
            }

            context.Copy(GetVec(op.Rd), res);
        }

        private static void EmitFcvt_s_Gp(ArmEmitterContext context, Func1I emit)
        {
            EmitFcvt___Gp(context, emit, signed: true);
        }

        private static void EmitFcvt_u_Gp(ArmEmitterContext context, Func1I emit)
        {
            EmitFcvt___Gp(context, emit, signed: false);
        }

        private static void EmitFcvt___Gp(ArmEmitterContext context, Func1I emit, bool signed)
        {
            OpCodeSimdCvt op = (OpCodeSimdCvt)context.CurrOp;

            OperandType type = op.Size == 0 ? OperandType.FP32 : OperandType.FP64;

            Operand ne = context.VectorExtract(type, GetVec(op.Rn), 0);

            Operand res = signed
                ? EmitScalarFcvts(context, emit(ne), 0)
                : EmitScalarFcvtu(context, emit(ne), 0);

            SetIntOrZR(context, op.Rd, res);
        }

        private static void EmitFcvtzs_Gp_Fixed(ArmEmitterContext context)
        {
            EmitFcvtz__Gp_Fixed(context, signed: true);
        }

        private static void EmitFcvtzu_Gp_Fixed(ArmEmitterContext context)
        {
            EmitFcvtz__Gp_Fixed(context, signed: false);
        }

        private static void EmitFcvtz__Gp_Fixed(ArmEmitterContext context, bool signed)
        {
            OpCodeSimdCvt op = (OpCodeSimdCvt)context.CurrOp;

            OperandType type = op.Size == 0 ? OperandType.FP32 : OperandType.FP64;

            Operand ne = context.VectorExtract(type, GetVec(op.Rn), 0);

            Operand res = signed
                ? EmitScalarFcvts(context, ne, op.FBits)
                : EmitScalarFcvtu(context, ne, op.FBits);

            SetIntOrZR(context, op.Rd, res);
        }

        private static void EmitCvtf(ArmEmitterContext context, bool signed, bool scalar)
        {
            OpCodeSimd op = (OpCodeSimd)context.CurrOp;

            Operand res = context.VectorZero();

            int sizeF = op.Size & 1;
            int sizeI = sizeF + 2;

            int fBits = GetFBits(context);

            int elems = !scalar ? op.GetBytesCount() >> sizeI : 1;

            for (int index = 0; index < elems; index++)
            {
                Operand ne = EmitVectorLongExtract(context, op.Rn, index, sizeI);

                Operand e = EmitFPConvert(context, ne, sizeF, signed);

                e = EmitI2fFBitsMul(context, e, fBits);

                res = context.VectorInsert(res, e, index);
            }

            context.Copy(GetVec(op.Rd), res);
        }

        private static int GetFBits(ArmEmitterContext context)
        {
            if (context.CurrOp is OpCodeSimdShImm op)
            {
                return GetImmShr(op);
            }

            return 0;
        }

        private static Operand EmitFPConvert(ArmEmitterContext context, Operand value, int size, bool signed)
        {
            Debug.Assert(value.Type == OperandType.I32 || value.Type == OperandType.I64);
            Debug.Assert((uint)size < 2);

            OperandType type = size == 0 ? OperandType.FP32 : OperandType.FP64;

            if (signed)
            {
                return context.ConvertToFP(type, value);
            }
            else
            {
                return context.ConvertToFPUI(type, value);
            }
        }

        private static Operand EmitScalarFcvts(ArmEmitterContext context, Operand value, int fBits)
        {
            Debug.Assert(value.Type == OperandType.FP32 || value.Type == OperandType.FP64);

            value = EmitF2iFBitsMul(context, value, fBits);

            MethodInfo info;

            if (context.CurrOp.RegisterSize == RegisterSize.Int32)
            {
                info = value.Type == OperandType.FP32
                    ? typeof(SoftFallback).GetMethod(nameof(SoftFallback.SatF32ToS32))
                    : typeof(SoftFallback).GetMethod(nameof(SoftFallback.SatF64ToS32));
            }
            else
            {
                info = value.Type == OperandType.FP32
                    ? typeof(SoftFallback).GetMethod(nameof(SoftFallback.SatF32ToS64))
                    : typeof(SoftFallback).GetMethod(nameof(SoftFallback.SatF64ToS64));
            }

            return context.Call(info, value);
        }

        private static Operand EmitScalarFcvtu(ArmEmitterContext context, Operand value, int fBits)
        {
            Debug.Assert(value.Type == OperandType.FP32 || value.Type == OperandType.FP64);

            value = EmitF2iFBitsMul(context, value, fBits);

            MethodInfo info;

            if (context.CurrOp.RegisterSize == RegisterSize.Int32)
            {
                info = value.Type == OperandType.FP32
                    ? typeof(SoftFallback).GetMethod(nameof(SoftFallback.SatF32ToU32))
                    : typeof(SoftFallback).GetMethod(nameof(SoftFallback.SatF64ToU32));
            }
            else
            {
                info = value.Type == OperandType.FP32
                    ? typeof(SoftFallback).GetMethod(nameof(SoftFallback.SatF32ToU64))
                    : typeof(SoftFallback).GetMethod(nameof(SoftFallback.SatF64ToU64));
            }

            return context.Call(info, value);
        }

        private static Operand EmitF2iFBitsMul(ArmEmitterContext context, Operand value, int fBits)
        {
            Debug.Assert(value.Type == OperandType.FP32 || value.Type == OperandType.FP64);

            if (fBits == 0)
            {
                return value;
            }

            if (value.Type == OperandType.FP32)
            {
                return context.Multiply(value, ConstF(MathF.Pow(2f, fBits)));
            }
            else /* if (value.Type == OperandType.FP64) */
            {
                return context.Multiply(value, ConstF(Math.Pow(2d, fBits)));
            }
        }

        private static Operand EmitI2fFBitsMul(ArmEmitterContext context, Operand value, int fBits)
        {
            Debug.Assert(value.Type == OperandType.FP32 || value.Type == OperandType.FP64);

            if (fBits == 0)
            {
                return value;
            }

            if (value.Type == OperandType.FP32)
            {
                return context.Multiply(value, ConstF(1f / MathF.Pow(2f, fBits)));
            }
            else /* if (value.Type == OperandType.FP64) */
            {
                return context.Multiply(value, ConstF(1d / Math.Pow(2d, fBits)));
            }
        }

        public static Operand EmitSse2CvtDoubleToInt64OpF(ArmEmitterContext context, Operand opF, bool scalar)
        {
            Debug.Assert(opF.Type == OperandType.V128);

            Operand longL = context.AddIntrinsicLong(Intrinsic.X86Cvtsd2si, opF); // opFL
            Operand res = context.VectorCreateScalar(longL);

            if (!scalar)
            {
                Operand opFH = context.AddIntrinsic(Intrinsic.X86Movhlps, res, opF); // res doesn't matter.
                Operand longH = context.AddIntrinsicLong(Intrinsic.X86Cvtsd2si, opFH);
                Operand resH = context.VectorCreateScalar(longH);
                res = context.AddIntrinsic(Intrinsic.X86Movlhps, res, resH);
            }

            return res;
        }

        private static Operand EmitSse2CvtInt64ToDoubleOp(ArmEmitterContext context, Operand op, bool scalar)
        {
            Debug.Assert(op.Type == OperandType.V128);

            Operand longL = context.AddIntrinsicLong(Intrinsic.X86Cvtsi2si, op); // opL
            Operand res = context.AddIntrinsic(Intrinsic.X86Cvtsi2sd, context.VectorZero(), longL);

            if (!scalar)
            {
                Operand opH = context.AddIntrinsic(Intrinsic.X86Movhlps, res, op);    // res doesn't matter.
                Operand longH = context.AddIntrinsicLong(Intrinsic.X86Cvtsi2si, opH);
                Operand resH = context.AddIntrinsic(Intrinsic.X86Cvtsi2sd, res, longH); // res doesn't matter.
                res = context.AddIntrinsic(Intrinsic.X86Movlhps, res, resH);
            }

            return res;
        }

        private static void EmitSse2ScvtfOp(ArmEmitterContext context, bool scalar)
        {
            OpCodeSimd op = (OpCodeSimd)context.CurrOp;

            Operand n = GetVec(op.Rn);

            // sizeF == ((OpCodeSimdShImm)op).Size - 2
            int sizeF = op.Size & 1;

            if (sizeF == 0)
            {
                Operand res = context.AddIntrinsic(Intrinsic.X86Cvtdq2ps, n);

                if (op is OpCodeSimdShImm fixedOp)
                {
                    int fBits = GetImmShr(fixedOp);

                    // BitConverter.Int32BitsToSingle(fpScaled) == 1f / MathF.Pow(2f, fBits)
                    int fpScaled = 0x3F800000 - fBits * 0x800000;

                    Operand fpScaledMask = scalar
                        ? X86GetScalar(context, fpScaled)
                        : X86GetAllElements(context, fpScaled);

                    res = context.AddIntrinsic(Intrinsic.X86Mulps, res, fpScaledMask);
                }

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
                Operand res = EmitSse2CvtInt64ToDoubleOp(context, n, scalar);

                if (op is OpCodeSimdShImm fixedOp)
                {
                    int fBits = GetImmShr(fixedOp);

                    // BitConverter.Int64BitsToDouble(fpScaled) == 1d / Math.Pow(2d, fBits)
                    long fpScaled = 0x3FF0000000000000L - fBits * 0x10000000000000L;

                    Operand fpScaledMask = scalar
                        ? X86GetScalar(context, fpScaled)
                        : X86GetAllElements(context, fpScaled);

                    res = context.AddIntrinsic(Intrinsic.X86Mulpd, res, fpScaledMask);
                }

                if (scalar)
                {
                    res = context.VectorZeroUpper64(res);
                }

                context.Copy(GetVec(op.Rd), res);
            }
        }

        private static void EmitSse2UcvtfOp(ArmEmitterContext context, bool scalar)
        {
            OpCodeSimd op = (OpCodeSimd)context.CurrOp;

            Operand n = GetVec(op.Rn);

            // sizeF == ((OpCodeSimdShImm)op).Size - 2
            int sizeF = op.Size & 1;

            if (sizeF == 0)
            {
                Operand mask = scalar // 65536.000f (1 << 16)
                    ? X86GetScalar(context, 0x47800000)
                    : X86GetAllElements(context, 0x47800000);

                Operand res = context.AddIntrinsic(Intrinsic.X86Psrld, n, Const(16));
                res = context.AddIntrinsic(Intrinsic.X86Cvtdq2ps, res);
                res = context.AddIntrinsic(Intrinsic.X86Mulps, res, mask);

                Operand res2 = context.AddIntrinsic(Intrinsic.X86Pslld, n, Const(16));
                res2 = context.AddIntrinsic(Intrinsic.X86Psrld, res2, Const(16));
                res2 = context.AddIntrinsic(Intrinsic.X86Cvtdq2ps, res2);

                res = context.AddIntrinsic(Intrinsic.X86Addps, res, res2);

                if (op is OpCodeSimdShImm fixedOp)
                {
                    int fBits = GetImmShr(fixedOp);

                    // BitConverter.Int32BitsToSingle(fpScaled) == 1f / MathF.Pow(2f, fBits)
                    int fpScaled = 0x3F800000 - fBits * 0x800000;

                    Operand fpScaledMask = scalar
                        ? X86GetScalar(context, fpScaled)
                        : X86GetAllElements(context, fpScaled);

                    res = context.AddIntrinsic(Intrinsic.X86Mulps, res, fpScaledMask);
                }

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
                Operand mask = scalar // 4294967296.0000000d (1L << 32)
                    ? X86GetScalar(context, 0x41F0000000000000L)
                    : X86GetAllElements(context, 0x41F0000000000000L);

                Operand res = context.AddIntrinsic(Intrinsic.X86Psrlq, n, Const(32));
                res = EmitSse2CvtInt64ToDoubleOp(context, res, scalar);
                res = context.AddIntrinsic(Intrinsic.X86Mulpd, res, mask);

                Operand res2 = context.AddIntrinsic(Intrinsic.X86Psllq, n, Const(32));
                res2 = context.AddIntrinsic(Intrinsic.X86Psrlq, res2, Const(32));
                res2 = EmitSse2CvtInt64ToDoubleOp(context, res2, scalar);

                res = context.AddIntrinsic(Intrinsic.X86Addpd, res, res2);

                if (op is OpCodeSimdShImm fixedOp)
                {
                    int fBits = GetImmShr(fixedOp);

                    // BitConverter.Int64BitsToDouble(fpScaled) == 1d / Math.Pow(2d, fBits)
                    long fpScaled = 0x3FF0000000000000L - fBits * 0x10000000000000L;

                    Operand fpScaledMask = scalar
                        ? X86GetScalar(context, fpScaled)
                        : X86GetAllElements(context, fpScaled);

                    res = context.AddIntrinsic(Intrinsic.X86Mulpd, res, fpScaledMask);
                }

                if (scalar)
                {
                    res = context.VectorZeroUpper64(res);
                }

                context.Copy(GetVec(op.Rd), res);
            }
        }

        private static void EmitSse41FcvtsOpF(ArmEmitterContext context, FPRoundingMode roundMode, bool scalar)
        {
            OpCodeSimd op = (OpCodeSimd)context.CurrOp;

            Operand n = GetVec(op.Rn);

            // sizeF == ((OpCodeSimdShImm)op).Size - 2
            int sizeF = op.Size & 1;

            if (sizeF == 0)
            {
                Operand nRes = context.AddIntrinsic(Intrinsic.X86Cmpps, n, n, Const((int)CmpCondition.OrderedQ));
                nRes = context.AddIntrinsic(Intrinsic.X86Pand, nRes, n);

                if (op is OpCodeSimdShImm fixedOp)
                {
                    int fBits = GetImmShr(fixedOp);

                    // BitConverter.Int32BitsToSingle(fpScaled) == MathF.Pow(2f, fBits)
                    int fpScaled = 0x3F800000 + fBits * 0x800000;

                    Operand fpScaledMask = scalar
                        ? X86GetScalar(context, fpScaled)
                        : X86GetAllElements(context, fpScaled);

                    nRes = context.AddIntrinsic(Intrinsic.X86Mulps, nRes, fpScaledMask);
                }

                if (roundMode != FPRoundingMode.ToNearestAway)
                {
                    nRes = context.AddIntrinsic(Intrinsic.X86Roundps, nRes, Const(X86GetRoundControl(roundMode)));
                }
                else
                {
                    nRes = EmitSse41RoundToNearestWithTiesToAwayOpF(context, nRes, scalar);
                }

                Operand nInt = context.AddIntrinsic(Intrinsic.X86Cvtps2dq, nRes);

                Operand fpMaxValMask = scalar // 2.14748365E9f (2147483648)
                    ? X86GetScalar(context, 0x4F000000)
                    : X86GetAllElements(context, 0x4F000000);

                nRes = context.AddIntrinsic(Intrinsic.X86Cmpps, nRes, fpMaxValMask, Const((int)CmpCondition.NotLessThan));

                Operand dRes = context.AddIntrinsic(Intrinsic.X86Pxor, nInt, nRes);

                if (scalar)
                {
                    dRes = context.VectorZeroUpper96(dRes);
                }
                else if (op.RegisterSize == RegisterSize.Simd64)
                {
                    dRes = context.VectorZeroUpper64(dRes);
                }

                context.Copy(GetVec(op.Rd), dRes);
            }
            else /* if (sizeF == 1) */
            {
                Operand nRes = context.AddIntrinsic(Intrinsic.X86Cmppd, n, n, Const((int)CmpCondition.OrderedQ));
                nRes = context.AddIntrinsic(Intrinsic.X86Pand, nRes, n);

                if (op is OpCodeSimdShImm fixedOp)
                {
                    int fBits = GetImmShr(fixedOp);

                    // BitConverter.Int64BitsToDouble(fpScaled) == Math.Pow(2d, fBits)
                    long fpScaled = 0x3FF0000000000000L + fBits * 0x10000000000000L;

                    Operand fpScaledMask = scalar
                        ? X86GetScalar(context, fpScaled)
                        : X86GetAllElements(context, fpScaled);

                    nRes = context.AddIntrinsic(Intrinsic.X86Mulpd, nRes, fpScaledMask);
                }

                if (roundMode != FPRoundingMode.ToNearestAway)
                {
                    nRes = context.AddIntrinsic(Intrinsic.X86Roundpd, nRes, Const(X86GetRoundControl(roundMode)));
                }
                else
                {
                    nRes = EmitSse41RoundToNearestWithTiesToAwayOpF(context, nRes, scalar);
                }

                Operand nLong = EmitSse2CvtDoubleToInt64OpF(context, nRes, scalar);

                Operand fpMaxValMask = scalar // 9.2233720368547760E18d (9223372036854775808)
                    ? X86GetScalar(context, 0x43E0000000000000L)
                    : X86GetAllElements(context, 0x43E0000000000000L);

                nRes = context.AddIntrinsic(Intrinsic.X86Cmppd, nRes, fpMaxValMask, Const((int)CmpCondition.NotLessThan));

                Operand dRes = context.AddIntrinsic(Intrinsic.X86Pxor, nLong, nRes);

                if (scalar)
                {
                    dRes = context.VectorZeroUpper64(dRes);
                }

                context.Copy(GetVec(op.Rd), dRes);
            }
        }

        private static void EmitSse41FcvtuOpF(ArmEmitterContext context, FPRoundingMode roundMode, bool scalar)
        {
            OpCodeSimd op = (OpCodeSimd)context.CurrOp;

            Operand n = GetVec(op.Rn);

            // sizeF == ((OpCodeSimdShImm)op).Size - 2
            int sizeF = op.Size & 1;

            if (sizeF == 0)
            {
                Operand nRes = context.AddIntrinsic(Intrinsic.X86Cmpps, n, n, Const((int)CmpCondition.OrderedQ));
                nRes = context.AddIntrinsic(Intrinsic.X86Pand, nRes, n);

                if (op is OpCodeSimdShImm fixedOp)
                {
                    int fBits = GetImmShr(fixedOp);

                    // BitConverter.Int32BitsToSingle(fpScaled) == MathF.Pow(2f, fBits)
                    int fpScaled = 0x3F800000 + fBits * 0x800000;

                    Operand fpScaledMask = scalar
                        ? X86GetScalar(context, fpScaled)
                        : X86GetAllElements(context, fpScaled);

                    nRes = context.AddIntrinsic(Intrinsic.X86Mulps, nRes, fpScaledMask);
                }

                if (roundMode != FPRoundingMode.ToNearestAway)
                {
                    nRes = context.AddIntrinsic(Intrinsic.X86Roundps, nRes, Const(X86GetRoundControl(roundMode)));
                }
                else
                {
                    nRes = EmitSse41RoundToNearestWithTiesToAwayOpF(context, nRes, scalar);
                }

                Operand zero = context.VectorZero();

                Operand nCmp = context.AddIntrinsic(Intrinsic.X86Cmpps, nRes, zero, Const((int)CmpCondition.NotLessThanOrEqual));
                nRes = context.AddIntrinsic(Intrinsic.X86Pand, nRes, nCmp);

                Operand fpMaxValMask = scalar // 2.14748365E9f (2147483648)
                    ? X86GetScalar(context, 0x4F000000)
                    : X86GetAllElements(context, 0x4F000000);

                Operand nInt = context.AddIntrinsic(Intrinsic.X86Cvtps2dq, nRes);

                nRes = context.AddIntrinsic(Intrinsic.X86Subps, nRes, fpMaxValMask);

                nCmp = context.AddIntrinsic(Intrinsic.X86Cmpps, nRes, zero, Const((int)CmpCondition.NotLessThanOrEqual));
                nRes = context.AddIntrinsic(Intrinsic.X86Pand, nRes, nCmp);

                Operand nInt2 = context.AddIntrinsic(Intrinsic.X86Cvtps2dq, nRes);

                nRes = context.AddIntrinsic(Intrinsic.X86Cmpps, nRes, fpMaxValMask, Const((int)CmpCondition.NotLessThan));

                Operand dRes = context.AddIntrinsic(Intrinsic.X86Pxor, nInt2, nRes);
                dRes = context.AddIntrinsic(Intrinsic.X86Paddd, dRes, nInt);

                if (scalar)
                {
                    dRes = context.VectorZeroUpper96(dRes);
                }
                else if (op.RegisterSize == RegisterSize.Simd64)
                {
                    dRes = context.VectorZeroUpper64(dRes);
                }

                context.Copy(GetVec(op.Rd), dRes);
            }
            else /* if (sizeF == 1) */
            {
                Operand nRes = context.AddIntrinsic(Intrinsic.X86Cmppd, n, n, Const((int)CmpCondition.OrderedQ));
                nRes = context.AddIntrinsic(Intrinsic.X86Pand, nRes, n);

                if (op is OpCodeSimdShImm fixedOp)
                {
                    int fBits = GetImmShr(fixedOp);

                    // BitConverter.Int64BitsToDouble(fpScaled) == Math.Pow(2d, fBits)
                    long fpScaled = 0x3FF0000000000000L + fBits * 0x10000000000000L;

                    Operand fpScaledMask = scalar
                        ? X86GetScalar(context, fpScaled)
                        : X86GetAllElements(context, fpScaled);

                    nRes = context.AddIntrinsic(Intrinsic.X86Mulpd, nRes, fpScaledMask);
                }

                if (roundMode != FPRoundingMode.ToNearestAway)
                {
                    nRes = context.AddIntrinsic(Intrinsic.X86Roundpd, nRes, Const(X86GetRoundControl(roundMode)));
                }
                else
                {
                    nRes = EmitSse41RoundToNearestWithTiesToAwayOpF(context, nRes, scalar);
                }

                Operand zero = context.VectorZero();

                Operand nCmp = context.AddIntrinsic(Intrinsic.X86Cmppd, nRes, zero, Const((int)CmpCondition.NotLessThanOrEqual));
                nRes = context.AddIntrinsic(Intrinsic.X86Pand, nRes, nCmp);

                Operand fpMaxValMask = scalar // 9.2233720368547760E18d (9223372036854775808)
                    ? X86GetScalar(context, 0x43E0000000000000L)
                    : X86GetAllElements(context, 0x43E0000000000000L);

                Operand nLong = EmitSse2CvtDoubleToInt64OpF(context, nRes, scalar);

                nRes = context.AddIntrinsic(Intrinsic.X86Subpd, nRes, fpMaxValMask);

                nCmp = context.AddIntrinsic(Intrinsic.X86Cmppd, nRes, zero, Const((int)CmpCondition.NotLessThanOrEqual));
                nRes = context.AddIntrinsic(Intrinsic.X86Pand, nRes, nCmp);

                Operand nLong2 = EmitSse2CvtDoubleToInt64OpF(context, nRes, scalar);

                nRes = context.AddIntrinsic(Intrinsic.X86Cmppd, nRes, fpMaxValMask, Const((int)CmpCondition.NotLessThan));

                Operand dRes = context.AddIntrinsic(Intrinsic.X86Pxor, nLong2, nRes);
                dRes = context.AddIntrinsic(Intrinsic.X86Paddq, dRes, nLong);

                if (scalar)
                {
                    dRes = context.VectorZeroUpper64(dRes);
                }

                context.Copy(GetVec(op.Rd), dRes);
            }
        }

        private static void EmitSse41Fcvts_Gp(ArmEmitterContext context, FPRoundingMode roundMode, bool isFixed)
        {
            OpCodeSimdCvt op = (OpCodeSimdCvt)context.CurrOp;

            Operand n = GetVec(op.Rn);

            if (op.Size == 0)
            {
                Operand nRes = context.AddIntrinsic(Intrinsic.X86Cmpss, n, n, Const((int)CmpCondition.OrderedQ));
                nRes = context.AddIntrinsic(Intrinsic.X86Pand, nRes, n);

                if (isFixed)
                {
                    // BitConverter.Int32BitsToSingle(fpScaled) == MathF.Pow(2f, op.FBits)
                    int fpScaled = 0x3F800000 + op.FBits * 0x800000;

                    Operand fpScaledMask = X86GetScalar(context, fpScaled);

                    nRes = context.AddIntrinsic(Intrinsic.X86Mulss, nRes, fpScaledMask);
                }

                if (roundMode != FPRoundingMode.ToNearestAway)
                {
                    nRes = context.AddIntrinsic(Intrinsic.X86Roundss, nRes, Const(X86GetRoundControl(roundMode)));
                }
                else
                {
                    nRes = EmitSse41RoundToNearestWithTiesToAwayOpF(context, nRes, scalar: true);
                }

                Operand nIntOrLong = op.RegisterSize == RegisterSize.Int32
                    ? context.AddIntrinsicInt(Intrinsic.X86Cvtss2si, nRes)
                    : context.AddIntrinsicLong(Intrinsic.X86Cvtss2si, nRes);

                int fpMaxVal = op.RegisterSize == RegisterSize.Int32
                    ? 0x4F000000  // 2.14748365E9f (2147483648)
                    : 0x5F000000; // 9.223372E18f  (9223372036854775808)

                Operand fpMaxValMask = X86GetScalar(context, fpMaxVal);

                nRes = context.AddIntrinsic(Intrinsic.X86Cmpss, nRes, fpMaxValMask, Const((int)CmpCondition.NotLessThan));

                Operand nInt = context.AddIntrinsicInt(Intrinsic.X86Cvtsi2si, nRes);

                if (op.RegisterSize == RegisterSize.Int64)
                {
                    nInt = context.SignExtend32(OperandType.I64, nInt);
                }

                Operand dRes = context.BitwiseExclusiveOr(nIntOrLong, nInt);

                SetIntOrZR(context, op.Rd, dRes);
            }
            else /* if (op.Size == 1) */
            {
                Operand nRes = context.AddIntrinsic(Intrinsic.X86Cmpsd, n, n, Const((int)CmpCondition.OrderedQ));
                nRes = context.AddIntrinsic(Intrinsic.X86Pand, nRes, n);

                if (isFixed)
                {
                    // BitConverter.Int64BitsToDouble(fpScaled) == Math.Pow(2d, op.FBits)
                    long fpScaled = 0x3FF0000000000000L + op.FBits * 0x10000000000000L;

                    Operand fpScaledMask = X86GetScalar(context, fpScaled);

                    nRes = context.AddIntrinsic(Intrinsic.X86Mulsd, nRes, fpScaledMask);
                }

                if (roundMode != FPRoundingMode.ToNearestAway)
                {
                    nRes = context.AddIntrinsic(Intrinsic.X86Roundsd, nRes, Const(X86GetRoundControl(roundMode)));
                }
                else
                {
                    nRes = EmitSse41RoundToNearestWithTiesToAwayOpF(context, nRes, scalar: true);
                }

                Operand nIntOrLong = op.RegisterSize == RegisterSize.Int32
                    ? context.AddIntrinsicInt(Intrinsic.X86Cvtsd2si, nRes)
                    : context.AddIntrinsicLong(Intrinsic.X86Cvtsd2si, nRes);

                long fpMaxVal = op.RegisterSize == RegisterSize.Int32
                    ? 0x41E0000000000000L  // 2147483648.0000000d    (2147483648)
                    : 0x43E0000000000000L; // 9.2233720368547760E18d (9223372036854775808)

                Operand fpMaxValMask = X86GetScalar(context, fpMaxVal);

                nRes = context.AddIntrinsic(Intrinsic.X86Cmpsd, nRes, fpMaxValMask, Const((int)CmpCondition.NotLessThan));

                Operand nLong = context.AddIntrinsicLong(Intrinsic.X86Cvtsi2si, nRes);

                if (op.RegisterSize == RegisterSize.Int32)
                {
                    nLong = context.ConvertI64ToI32(nLong);
                }

                Operand dRes = context.BitwiseExclusiveOr(nIntOrLong, nLong);

                SetIntOrZR(context, op.Rd, dRes);
            }
        }

        private static void EmitSse41Fcvtu_Gp(ArmEmitterContext context, FPRoundingMode roundMode, bool isFixed)
        {
            OpCodeSimdCvt op = (OpCodeSimdCvt)context.CurrOp;

            Operand n = GetVec(op.Rn);

            if (op.Size == 0)
            {
                Operand nRes = context.AddIntrinsic(Intrinsic.X86Cmpss, n, n, Const((int)CmpCondition.OrderedQ));
                nRes = context.AddIntrinsic(Intrinsic.X86Pand, nRes, n);

                if (isFixed)
                {
                    // BitConverter.Int32BitsToSingle(fpScaled) == MathF.Pow(2f, op.FBits)
                    int fpScaled = 0x3F800000 + op.FBits * 0x800000;

                    Operand fpScaledMask = X86GetScalar(context, fpScaled);

                    nRes = context.AddIntrinsic(Intrinsic.X86Mulss, nRes, fpScaledMask);
                }

                if (roundMode != FPRoundingMode.ToNearestAway)
                {
                    nRes = context.AddIntrinsic(Intrinsic.X86Roundss, nRes, Const(X86GetRoundControl(roundMode)));
                }
                else
                {
                    nRes = EmitSse41RoundToNearestWithTiesToAwayOpF(context, nRes, scalar: true);
                }

                Operand zero = context.VectorZero();

                Operand nCmp = context.AddIntrinsic(Intrinsic.X86Cmpss, nRes, zero, Const((int)CmpCondition.NotLessThanOrEqual));
                nRes = context.AddIntrinsic(Intrinsic.X86Pand, nRes, nCmp);

                int fpMaxVal = op.RegisterSize == RegisterSize.Int32
                    ? 0x4F000000  // 2.14748365E9f (2147483648)
                    : 0x5F000000; // 9.223372E18f  (9223372036854775808)

                Operand fpMaxValMask = X86GetScalar(context, fpMaxVal);

                Operand nIntOrLong = op.RegisterSize == RegisterSize.Int32
                    ? context.AddIntrinsicInt(Intrinsic.X86Cvtss2si, nRes)
                    : context.AddIntrinsicLong(Intrinsic.X86Cvtss2si, nRes);

                nRes = context.AddIntrinsic(Intrinsic.X86Subss, nRes, fpMaxValMask);

                nCmp = context.AddIntrinsic(Intrinsic.X86Cmpss, nRes, zero, Const((int)CmpCondition.NotLessThanOrEqual));
                nRes = context.AddIntrinsic(Intrinsic.X86Pand, nRes, nCmp);

                Operand nIntOrLong2 = op.RegisterSize == RegisterSize.Int32
                    ? context.AddIntrinsicInt(Intrinsic.X86Cvtss2si, nRes)
                    : context.AddIntrinsicLong(Intrinsic.X86Cvtss2si, nRes);

                nRes = context.AddIntrinsic(Intrinsic.X86Cmpss, nRes, fpMaxValMask, Const((int)CmpCondition.NotLessThan));

                Operand nInt = context.AddIntrinsicInt(Intrinsic.X86Cvtsi2si, nRes);

                if (op.RegisterSize == RegisterSize.Int64)
                {
                    nInt = context.SignExtend32(OperandType.I64, nInt);
                }

                Operand dRes = context.BitwiseExclusiveOr(nIntOrLong2, nInt);
                dRes = context.Add(dRes, nIntOrLong);

                SetIntOrZR(context, op.Rd, dRes);
            }
            else /* if (op.Size == 1) */
            {
                Operand nRes = context.AddIntrinsic(Intrinsic.X86Cmpsd, n, n, Const((int)CmpCondition.OrderedQ));
                nRes = context.AddIntrinsic(Intrinsic.X86Pand, nRes, n);

                if (isFixed)
                {
                    // BitConverter.Int64BitsToDouble(fpScaled) == Math.Pow(2d, op.FBits)
                    long fpScaled = 0x3FF0000000000000L + op.FBits * 0x10000000000000L;

                    Operand fpScaledMask = X86GetScalar(context, fpScaled);

                    nRes = context.AddIntrinsic(Intrinsic.X86Mulsd, nRes, fpScaledMask);
                }

                if (roundMode != FPRoundingMode.ToNearestAway)
                {
                    nRes = context.AddIntrinsic(Intrinsic.X86Roundsd, nRes, Const(X86GetRoundControl(roundMode)));
                }
                else
                {
                    nRes = EmitSse41RoundToNearestWithTiesToAwayOpF(context, nRes, scalar: true);
                }

                Operand zero = context.VectorZero();

                Operand nCmp = context.AddIntrinsic(Intrinsic.X86Cmpsd, nRes, zero, Const((int)CmpCondition.NotLessThanOrEqual));
                nRes = context.AddIntrinsic(Intrinsic.X86Pand, nRes, nCmp);

                long fpMaxVal = op.RegisterSize == RegisterSize.Int32
                    ? 0x41E0000000000000L  // 2147483648.0000000d    (2147483648)
                    : 0x43E0000000000000L; // 9.2233720368547760E18d (9223372036854775808)

                Operand fpMaxValMask = X86GetScalar(context, fpMaxVal);

                Operand nIntOrLong = op.RegisterSize == RegisterSize.Int32
                    ? context.AddIntrinsicInt(Intrinsic.X86Cvtsd2si, nRes)
                    : context.AddIntrinsicLong(Intrinsic.X86Cvtsd2si, nRes);

                nRes = context.AddIntrinsic(Intrinsic.X86Subsd, nRes, fpMaxValMask);

                nCmp = context.AddIntrinsic(Intrinsic.X86Cmpsd, nRes, zero, Const((int)CmpCondition.NotLessThanOrEqual));
                nRes = context.AddIntrinsic(Intrinsic.X86Pand, nRes, nCmp);

                Operand nIntOrLong2 = op.RegisterSize == RegisterSize.Int32
                    ? context.AddIntrinsicInt(Intrinsic.X86Cvtsd2si, nRes)
                    : context.AddIntrinsicLong(Intrinsic.X86Cvtsd2si, nRes);

                nRes = context.AddIntrinsic(Intrinsic.X86Cmpsd, nRes, fpMaxValMask, Const((int)CmpCondition.NotLessThan));

                Operand nLong = context.AddIntrinsicLong(Intrinsic.X86Cvtsi2si, nRes);

                if (op.RegisterSize == RegisterSize.Int32)
                {
                    nLong = context.ConvertI64ToI32(nLong);
                }

                Operand dRes = context.BitwiseExclusiveOr(nIntOrLong2, nLong);
                dRes = context.Add(dRes, nIntOrLong);

                SetIntOrZR(context, op.Rd, dRes);
            }
        }

        private static Operand EmitVectorLongExtract(ArmEmitterContext context, int reg, int index, int size)
        {
            OperandType type = size == 3 ? OperandType.I64 : OperandType.I32;

            return context.VectorExtract(type, GetVec(reg), index);
        }
    }
}
