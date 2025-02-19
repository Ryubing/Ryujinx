using ARMeilleure.Decoders;
using ARMeilleure.IntermediateRepresentation;
using ARMeilleure.Translation;
using System.Collections.Generic;
using System.Reflection;
using static ARMeilleure.Instructions.InstEmitHelper;
using static ARMeilleure.Instructions.InstEmitSimdHelper;
using static ARMeilleure.IntermediateRepresentation.Operand.Factory;

namespace ARMeilleure.Instructions
{
    static partial class InstEmit
    {
        #region "Masks"
        private static readonly long[] _masksE0_Uzp =
        [
            13L << 56 | 09L << 48 | 05L << 40 | 01L << 32 | 12L << 24 | 08L << 16 | 04L << 8 | 00L << 0,
            11L << 56 | 10L << 48 | 03L << 40 | 02L << 32 | 09L << 24 | 08L << 16 | 01L << 8 | 00L << 0
        ];

        private static readonly long[] _masksE1_Uzp =
        [
            15L << 56 | 11L << 48 | 07L << 40 | 03L << 32 | 14L << 24 | 10L << 16 | 06L << 8 | 02L << 0,
            15L << 56 | 14L << 48 | 07L << 40 | 06L << 32 | 13L << 24 | 12L << 16 | 05L << 8 | 04L << 0
        ];
        #endregion

        public static void Dup_Gp(ArmEmitterContext context)
        {
            OpCodeSimdIns op = (OpCodeSimdIns)context.CurrOp;

            Operand n = GetIntOrZR(context, op.Rn);

            if (Optimizations.UseSse2)
            {
                switch (op.Size)
                {
                    case 0:
                        n = context.ZeroExtend8(n.Type, n);
                        n = context.Multiply(n, Const(n.Type, 0x01010101));
                        break;
                    case 1:
                        n = context.ZeroExtend16(n.Type, n);
                        n = context.Multiply(n, Const(n.Type, 0x00010001));
                        break;
                    case 2:
                        n = context.ZeroExtend32(n.Type, n);
                        break;
                }

                Operand res = context.VectorInsert(context.VectorZero(), n, 0);

                if (op.Size < 3)
                {
                    if (op.RegisterSize == RegisterSize.Simd64)
                    {
                        res = context.AddIntrinsic(Intrinsic.X86Shufps, res, res, Const(0xf0));
                    }
                    else
                    {
                        res = context.AddIntrinsic(Intrinsic.X86Shufps, res, res, Const(0));
                    }
                }
                else
                {
                    res = context.AddIntrinsic(Intrinsic.X86Movlhps, res, res);
                }

                context.Copy(GetVec(op.Rd), res);
            }
            else
            {
                Operand res = context.VectorZero();

                int elems = op.GetBytesCount() >> op.Size;

                for (int index = 0; index < elems; index++)
                {
                    res = EmitVectorInsert(context, res, n, index, op.Size);
                }

                context.Copy(GetVec(op.Rd), res);
            }
        }

        public static void Dup_S(ArmEmitterContext context)
        {
            OpCodeSimdIns op = (OpCodeSimdIns)context.CurrOp;

            Operand ne = EmitVectorExtractZx(context, op.Rn, op.DstIndex, op.Size);

            context.Copy(GetVec(op.Rd), EmitVectorInsert(context, context.VectorZero(), ne, 0, op.Size));
        }

        public static void Dup_V(ArmEmitterContext context)
        {
            OpCodeSimdIns op = (OpCodeSimdIns)context.CurrOp;

            if (Optimizations.UseSse2)
            {
                Operand res = GetVec(op.Rn);

                if (op.Size == 0)
                {
                    if (op.DstIndex != 0)
                    {
                        res = context.AddIntrinsic(Intrinsic.X86Psrldq, res, Const(op.DstIndex));
                    }

                    res = context.AddIntrinsic(Intrinsic.X86Punpcklbw, res, res);
                    res = context.AddIntrinsic(Intrinsic.X86Punpcklwd, res, res);
                    res = context.AddIntrinsic(Intrinsic.X86Shufps, res, res, Const(0));
                }
                else if (op.Size == 1)
                {
                    if (op.DstIndex != 0)
                    {
                        res = context.AddIntrinsic(Intrinsic.X86Psrldq, res, Const(op.DstIndex * 2));
                    }

                    res = context.AddIntrinsic(Intrinsic.X86Punpcklwd, res, res);
                    res = context.AddIntrinsic(Intrinsic.X86Shufps, res, res, Const(0));
                }
                else if (op.Size == 2)
                {
                    int mask = op.DstIndex * 0b01010101;

                    res = context.AddIntrinsic(Intrinsic.X86Shufps, res, res, Const(mask));
                }
                else if (op.DstIndex == 0 && op.RegisterSize != RegisterSize.Simd64)
                {
                    res = context.AddIntrinsic(Intrinsic.X86Movlhps, res, res);
                }
                else if (op.DstIndex == 1)
                {
                    res = context.AddIntrinsic(Intrinsic.X86Movhlps, res, res);
                }

                if (op.RegisterSize == RegisterSize.Simd64)
                {
                    res = context.VectorZeroUpper64(res);
                }

                context.Copy(GetVec(op.Rd), res);
            }
            else
            {
                Operand ne = EmitVectorExtractZx(context, op.Rn, op.DstIndex, op.Size);

                Operand res = context.VectorZero();

                int elems = op.GetBytesCount() >> op.Size;

                for (int index = 0; index < elems; index++)
                {
                    res = EmitVectorInsert(context, res, ne, index, op.Size);
                }

                context.Copy(GetVec(op.Rd), res);
            }
        }

        public static void Ext_V(ArmEmitterContext context)
        {
            OpCodeSimdExt op = (OpCodeSimdExt)context.CurrOp;

            if (Optimizations.UseSse2)
            {
                Operand nShifted = GetVec(op.Rn);

                if (op.RegisterSize == RegisterSize.Simd64)
                {
                    nShifted = context.VectorZeroUpper64(nShifted);
                }

                nShifted = context.AddIntrinsic(Intrinsic.X86Psrldq, nShifted, Const(op.Imm4));

                Operand mShifted = GetVec(op.Rm);

                mShifted = context.AddIntrinsic(Intrinsic.X86Pslldq, mShifted, Const(op.GetBytesCount() - op.Imm4));

                if (op.RegisterSize == RegisterSize.Simd64)
                {
                    mShifted = context.VectorZeroUpper64(mShifted);
                }

                Operand res = context.AddIntrinsic(Intrinsic.X86Por, nShifted, mShifted);

                context.Copy(GetVec(op.Rd), res);
            }
            else
            {
                Operand res = context.VectorZero();

                int bytes = op.GetBytesCount();

                int position = op.Imm4 & (bytes - 1);

                for (int index = 0; index < bytes; index++)
                {
                    int reg = op.Imm4 + index < bytes ? op.Rn : op.Rm;

                    Operand e = EmitVectorExtractZx(context, reg, position, 0);

                    position = (position + 1) & (bytes - 1);

                    res = EmitVectorInsert(context, res, e, index, 0);
                }

                context.Copy(GetVec(op.Rd), res);
            }
        }

        public static void Fcsel_S(ArmEmitterContext context)
        {
            OpCodeSimdFcond op = (OpCodeSimdFcond)context.CurrOp;

            Operand lblTrue = Label();
            Operand lblEnd = Label();

            Operand isTrue = InstEmitFlowHelper.GetCondTrue(context, op.Cond);

            context.BranchIfTrue(lblTrue, isTrue);

            OperandType type = op.Size == 0 ? OperandType.FP32 : OperandType.FP64;

            Operand me = context.VectorExtract(type, GetVec(op.Rm), 0);

            context.Copy(GetVec(op.Rd), context.VectorInsert(context.VectorZero(), me, 0));

            context.Branch(lblEnd);

            context.MarkLabel(lblTrue);

            Operand ne = context.VectorExtract(type, GetVec(op.Rn), 0);

            context.Copy(GetVec(op.Rd), context.VectorInsert(context.VectorZero(), ne, 0));

            context.MarkLabel(lblEnd);
        }

        public static void Fmov_Ftoi(ArmEmitterContext context)
        {
            OpCodeSimd op = (OpCodeSimd)context.CurrOp;

            Operand ne = EmitVectorExtractZx(context, op.Rn, 0, op.Size + 2);

            SetIntOrZR(context, op.Rd, ne);
        }

        public static void Fmov_Ftoi1(ArmEmitterContext context)
        {
            OpCodeSimd op = (OpCodeSimd)context.CurrOp;

            Operand ne = EmitVectorExtractZx(context, op.Rn, 1, 3);

            SetIntOrZR(context, op.Rd, ne);
        }

        public static void Fmov_Itof(ArmEmitterContext context)
        {
            OpCodeSimd op = (OpCodeSimd)context.CurrOp;

            Operand n = GetIntOrZR(context, op.Rn);

            context.Copy(GetVec(op.Rd), EmitVectorInsert(context, context.VectorZero(), n, 0, op.Size + 2));
        }

        public static void Fmov_Itof1(ArmEmitterContext context)
        {
            OpCodeSimd op = (OpCodeSimd)context.CurrOp;

            Operand d = GetVec(op.Rd);
            Operand n = GetIntOrZR(context, op.Rn);

            context.Copy(d, EmitVectorInsert(context, d, n, 1, 3));
        }

        public static void Fmov_S(ArmEmitterContext context)
        {
            OpCodeSimd op = (OpCodeSimd)context.CurrOp;

            OperandType type = op.Size == 0 ? OperandType.FP32 : OperandType.FP64;

            Operand ne = context.VectorExtract(type, GetVec(op.Rn), 0);

            context.Copy(GetVec(op.Rd), context.VectorInsert(context.VectorZero(), ne, 0));
        }

        public static void Fmov_Si(ArmEmitterContext context)
        {
            OpCodeSimdFmov op = (OpCodeSimdFmov)context.CurrOp;

            if (Optimizations.UseSse2)
            {
                if (op.Size == 0)
                {
                    context.Copy(GetVec(op.Rd), X86GetScalar(context, (int)op.Immediate));
                }
                else
                {
                    context.Copy(GetVec(op.Rd), X86GetScalar(context, op.Immediate));
                }
            }
            else
            {
                Operand e = Const(op.Immediate);

                Operand res = context.VectorZero();

                res = EmitVectorInsert(context, res, e, 0, op.Size + 2);

                context.Copy(GetVec(op.Rd), res);
            }
        }

        public static void Fmov_Vi(ArmEmitterContext context)
        {
            OpCodeSimdImm op = (OpCodeSimdImm)context.CurrOp;

            if (Optimizations.UseSse2)
            {
                if (op.RegisterSize == RegisterSize.Simd128)
                {
                    context.Copy(GetVec(op.Rd), X86GetAllElements(context, op.Immediate));
                }
                else
                {
                    context.Copy(GetVec(op.Rd), X86GetScalar(context, op.Immediate));
                }
            }
            else
            {
                Operand e = Const(op.Immediate);

                Operand res = context.VectorZero();

                int elems = op.RegisterSize == RegisterSize.Simd128 ? 2 : 1;

                for (int index = 0; index < elems; index++)
                {
                    res = EmitVectorInsert(context, res, e, index, 3);
                }

                context.Copy(GetVec(op.Rd), res);
            }
        }

        public static void Ins_Gp(ArmEmitterContext context)
        {
            OpCodeSimdIns op = (OpCodeSimdIns)context.CurrOp;

            Operand d = GetVec(op.Rd);
            Operand n = GetIntOrZR(context, op.Rn);

            context.Copy(d, EmitVectorInsert(context, d, n, op.DstIndex, op.Size));
        }

        public static void Ins_V(ArmEmitterContext context)
        {
            OpCodeSimdIns op = (OpCodeSimdIns)context.CurrOp;

            Operand d = GetVec(op.Rd);
            Operand ne = EmitVectorExtractZx(context, op.Rn, op.SrcIndex, op.Size);

            context.Copy(d, EmitVectorInsert(context, d, ne, op.DstIndex, op.Size));
        }

        public static void Movi_V(ArmEmitterContext context)
        {
            if (Optimizations.UseSse2)
            {
                EmitSse2VectorMoviMvniOp(context, not: false);
            }
            else
            {
                EmitVectorImmUnaryOp(context, (op1) => op1);
            }
        }

        public static void Mvni_V(ArmEmitterContext context)
        {
            if (Optimizations.UseSse2)
            {
                EmitSse2VectorMoviMvniOp(context, not: true);
            }
            else
            {
                EmitVectorImmUnaryOp(context, (op1) => context.BitwiseNot(op1));
            }
        }

        public static void Smov_S(ArmEmitterContext context)
        {
            OpCodeSimdIns op = (OpCodeSimdIns)context.CurrOp;

            Operand ne = EmitVectorExtractSx(context, op.Rn, op.DstIndex, op.Size);

            if (op.RegisterSize == RegisterSize.Simd64)
            {
                ne = context.ZeroExtend32(OperandType.I64, ne);
            }

            SetIntOrZR(context, op.Rd, ne);
        }

        public static void Tbl_V(ArmEmitterContext context)
        {
            EmitTableVectorLookup(context, isTbl: true);
        }

        public static void Tbx_V(ArmEmitterContext context)
        {
            EmitTableVectorLookup(context, isTbl: false);
        }

        public static void Trn1_V(ArmEmitterContext context)
        {
            EmitVectorTranspose(context, part: 0);
        }

        public static void Trn2_V(ArmEmitterContext context)
        {
            EmitVectorTranspose(context, part: 1);
        }

        public static void Umov_S(ArmEmitterContext context)
        {
            OpCodeSimdIns op = (OpCodeSimdIns)context.CurrOp;

            Operand ne = EmitVectorExtractZx(context, op.Rn, op.DstIndex, op.Size);

            SetIntOrZR(context, op.Rd, ne);
        }

        public static void Uzp1_V(ArmEmitterContext context)
        {
            EmitVectorUnzip(context, part: 0);
        }

        public static void Uzp2_V(ArmEmitterContext context)
        {
            EmitVectorUnzip(context, part: 1);
        }

        public static void Xtn_V(ArmEmitterContext context)
        {
            OpCodeSimd op = (OpCodeSimd)context.CurrOp;

            if (Optimizations.UseSsse3)
            {
                Operand d = GetVec(op.Rd);

                Operand res = context.VectorZeroUpper64(d);

                Operand mask = X86GetAllElements(context, EvenMasks[op.Size]);

                Operand res2 = context.AddIntrinsic(Intrinsic.X86Pshufb, GetVec(op.Rn), mask);

                Intrinsic movInst = op.RegisterSize == RegisterSize.Simd128
                    ? Intrinsic.X86Movlhps
                    : Intrinsic.X86Movhlps;

                res = context.AddIntrinsic(movInst, res, res2);

                context.Copy(d, res);
            }
            else
            {
                int elems = 8 >> op.Size;

                int part = op.RegisterSize == RegisterSize.Simd128 ? elems : 0;

                Operand d = GetVec(op.Rd);

                Operand res = part == 0 ? context.VectorZero() : context.Copy(d);

                for (int index = 0; index < elems; index++)
                {
                    Operand ne = EmitVectorExtractZx(context, op.Rn, index, op.Size + 1);

                    res = EmitVectorInsert(context, res, ne, part + index, op.Size);
                }

                context.Copy(d, res);
            }
        }

        public static void Zip1_V(ArmEmitterContext context)
        {
            EmitVectorZip(context, part: 0);
        }

        public static void Zip2_V(ArmEmitterContext context)
        {
            EmitVectorZip(context, part: 1);
        }

        private static void EmitSse2VectorMoviMvniOp(ArmEmitterContext context, bool not)
        {
            OpCodeSimdImm op = (OpCodeSimdImm)context.CurrOp;

            long imm = op.Immediate;

            switch (op.Size)
            {
                case 0:
                    imm *= 0x01010101;
                    break;
                case 1:
                    imm *= 0x00010001;
                    break;
            }

            if (not)
            {
                imm = ~imm;
            }

            Operand mask;

            if (op.Size < 3)
            {
                mask = X86GetAllElements(context, (int)imm);
            }
            else
            {
                mask = X86GetAllElements(context, imm);
            }

            if (op.RegisterSize == RegisterSize.Simd64)
            {
                mask = context.VectorZeroUpper64(mask);
            }

            context.Copy(GetVec(op.Rd), mask);
        }

        private static void EmitTableVectorLookup(ArmEmitterContext context, bool isTbl)
        {
            OpCodeSimdTbl op = (OpCodeSimdTbl)context.CurrOp;

            if (Optimizations.UseSsse3)
            {
                Operand d = GetVec(op.Rd);
                Operand m = GetVec(op.Rm);

                Operand res;

                Operand mask = X86GetAllElements(context, 0x0F0F0F0F0F0F0F0FL);

                // Fast path for single register table.
                {
                    Operand n = GetVec(op.Rn);

                    Operand mMask = context.AddIntrinsic(Intrinsic.X86Pcmpgtb, m, mask);
                    mMask = context.AddIntrinsic(Intrinsic.X86Por, mMask, m);

                    res = context.AddIntrinsic(Intrinsic.X86Pshufb, n, mMask);
                }

                for (int index = 1; index < op.Size; index++)
                {
                    Operand ni = GetVec((op.Rn + index) & 0x1F);

                    Operand idxMask = X86GetAllElements(context, 0x1010101010101010L * index);

                    Operand mSubMask = context.AddIntrinsic(Intrinsic.X86Psubb, m, idxMask);

                    Operand mMask = context.AddIntrinsic(Intrinsic.X86Pcmpgtb, mSubMask, mask);
                    mMask = context.AddIntrinsic(Intrinsic.X86Por, mMask, mSubMask);

                    Operand res2 = context.AddIntrinsic(Intrinsic.X86Pshufb, ni, mMask);

                    res = context.AddIntrinsic(Intrinsic.X86Por, res, res2);
                }

                if (!isTbl)
                {
                    Operand idxMask = X86GetAllElements(context, (0x1010101010101010L * op.Size) - 0x0101010101010101L);
                    Operand zeroMask = context.VectorZero();

                    Operand mPosMask = context.AddIntrinsic(Intrinsic.X86Pcmpgtb, m, idxMask);
                    Operand mNegMask = context.AddIntrinsic(Intrinsic.X86Pcmpgtb, zeroMask, m);

                    Operand mMask = context.AddIntrinsic(Intrinsic.X86Por, mPosMask, mNegMask);

                    Operand dMask = context.AddIntrinsic(Intrinsic.X86Pand, d, mMask);

                    res = context.AddIntrinsic(Intrinsic.X86Por, res, dMask);
                }

                if (op.RegisterSize == RegisterSize.Simd64)
                {
                    res = context.VectorZeroUpper64(res);
                }

                context.Copy(d, res);
            }
            else
            {
                Operand d = GetVec(op.Rd);

                List<Operand> args = [];

                if (!isTbl)
                {
                    args.Add(d);
                }

                args.Add(GetVec(op.Rm));

                args.Add(Const(op.RegisterSize == RegisterSize.Simd64 ? 8 : 16));

                for (int index = 0; index < op.Size; index++)
                {
                    args.Add(GetVec((op.Rn + index) & 0x1F));
                }

                MethodInfo info = null;

                if (isTbl)
                {
                    switch (op.Size)
                    {
                        case 1:
                            info = typeof(SoftFallback).GetMethod(nameof(SoftFallback.Tbl1));
                            break;
                        case 2:
                            info = typeof(SoftFallback).GetMethod(nameof(SoftFallback.Tbl2));
                            break;
                        case 3:
                            info = typeof(SoftFallback).GetMethod(nameof(SoftFallback.Tbl3));
                            break;
                        case 4:
                            info = typeof(SoftFallback).GetMethod(nameof(SoftFallback.Tbl4));
                            break;
                    }
                }
                else
                {
                    switch (op.Size)
                    {
                        case 1:
                            info = typeof(SoftFallback).GetMethod(nameof(SoftFallback.Tbx1));
                            break;
                        case 2:
                            info = typeof(SoftFallback).GetMethod(nameof(SoftFallback.Tbx2));
                            break;
                        case 3:
                            info = typeof(SoftFallback).GetMethod(nameof(SoftFallback.Tbx3));
                            break;
                        case 4:
                            info = typeof(SoftFallback).GetMethod(nameof(SoftFallback.Tbx4));
                            break;
                    }
                }

                context.Copy(d, context.Call(info, args.ToArray()));
            }
        }

        private static void EmitVectorTranspose(ArmEmitterContext context, int part)
        {
            OpCodeSimdReg op = (OpCodeSimdReg)context.CurrOp;

            if (Optimizations.UseSsse3)
            {
                Operand mask = default;

                if (op.Size < 3)
                {
                    long maskE0 = EvenMasks[op.Size];
                    long maskE1 = OddMasks[op.Size];

                    mask = X86GetScalar(context, maskE0);

                    mask = EmitVectorInsert(context, mask, Const(maskE1), 1, 3);
                }

                Operand n = GetVec(op.Rn);

                if (op.Size < 3)
                {
                    n = context.AddIntrinsic(Intrinsic.X86Pshufb, n, mask);
                }

                Operand m = GetVec(op.Rm);

                if (op.Size < 3)
                {
                    m = context.AddIntrinsic(Intrinsic.X86Pshufb, m, mask);
                }

                Intrinsic punpckInst = part == 0
                    ? X86PunpcklInstruction[op.Size]
                    : X86PunpckhInstruction[op.Size];

                Operand res = context.AddIntrinsic(punpckInst, n, m);

                if (op.RegisterSize == RegisterSize.Simd64)
                {
                    res = context.VectorZeroUpper64(res);
                }

                context.Copy(GetVec(op.Rd), res);
            }
            else
            {
                Operand res = context.VectorZero();

                int pairs = op.GetPairsCount() >> op.Size;

                for (int index = 0; index < pairs; index++)
                {
                    int pairIndex = index << 1;

                    Operand ne = EmitVectorExtractZx(context, op.Rn, pairIndex + part, op.Size);
                    Operand me = EmitVectorExtractZx(context, op.Rm, pairIndex + part, op.Size);

                    res = EmitVectorInsert(context, res, ne, pairIndex, op.Size);
                    res = EmitVectorInsert(context, res, me, pairIndex + 1, op.Size);
                }

                context.Copy(GetVec(op.Rd), res);
            }
        }

        private static void EmitVectorUnzip(ArmEmitterContext context, int part)
        {
            OpCodeSimdReg op = (OpCodeSimdReg)context.CurrOp;

            if (Optimizations.UseSsse3)
            {
                if (op.RegisterSize == RegisterSize.Simd128)
                {
                    Operand mask = default;

                    if (op.Size < 3)
                    {
                        long maskE0 = EvenMasks[op.Size];
                        long maskE1 = OddMasks[op.Size];

                        mask = X86GetScalar(context, maskE0);

                        mask = EmitVectorInsert(context, mask, Const(maskE1), 1, 3);
                    }

                    Operand n = GetVec(op.Rn);

                    if (op.Size < 3)
                    {
                        n = context.AddIntrinsic(Intrinsic.X86Pshufb, n, mask);
                    }

                    Operand m = GetVec(op.Rm);

                    if (op.Size < 3)
                    {
                        m = context.AddIntrinsic(Intrinsic.X86Pshufb, m, mask);
                    }

                    Intrinsic punpckInst = part == 0
                        ? Intrinsic.X86Punpcklqdq
                        : Intrinsic.X86Punpckhqdq;

                    Operand res = context.AddIntrinsic(punpckInst, n, m);

                    context.Copy(GetVec(op.Rd), res);
                }
                else
                {
                    Operand n = GetVec(op.Rn);
                    Operand m = GetVec(op.Rm);

                    Intrinsic punpcklInst = X86PunpcklInstruction[op.Size];

                    Operand res = context.AddIntrinsic(punpcklInst, n, m);

                    if (op.Size < 2)
                    {
                        long maskE0 = _masksE0_Uzp[op.Size];
                        long maskE1 = _masksE1_Uzp[op.Size];

                        Operand mask = X86GetScalar(context, maskE0);

                        mask = EmitVectorInsert(context, mask, Const(maskE1), 1, 3);

                        res = context.AddIntrinsic(Intrinsic.X86Pshufb, res, mask);
                    }

                    Intrinsic punpckInst = part == 0
                        ? Intrinsic.X86Punpcklqdq
                        : Intrinsic.X86Punpckhqdq;

                    res = context.AddIntrinsic(punpckInst, res, context.VectorZero());

                    context.Copy(GetVec(op.Rd), res);
                }
            }
            else
            {
                Operand res = context.VectorZero();

                int pairs = op.GetPairsCount() >> op.Size;

                for (int index = 0; index < pairs; index++)
                {
                    int idx = index << 1;

                    Operand ne = EmitVectorExtractZx(context, op.Rn, idx + part, op.Size);
                    Operand me = EmitVectorExtractZx(context, op.Rm, idx + part, op.Size);

                    res = EmitVectorInsert(context, res, ne, index, op.Size);
                    res = EmitVectorInsert(context, res, me, pairs + index, op.Size);
                }

                context.Copy(GetVec(op.Rd), res);
            }
        }

        private static void EmitVectorZip(ArmEmitterContext context, int part)
        {
            OpCodeSimdReg op = (OpCodeSimdReg)context.CurrOp;

            if (Optimizations.UseSse2)
            {
                Operand n = GetVec(op.Rn);
                Operand m = GetVec(op.Rm);

                if (op.RegisterSize == RegisterSize.Simd128)
                {
                    Intrinsic punpckInst = part == 0
                        ? X86PunpcklInstruction[op.Size]
                        : X86PunpckhInstruction[op.Size];

                    Operand res = context.AddIntrinsic(punpckInst, n, m);

                    context.Copy(GetVec(op.Rd), res);
                }
                else
                {
                    Operand res = context.AddIntrinsic(X86PunpcklInstruction[op.Size], n, m);

                    Intrinsic punpckInst = part == 0
                        ? Intrinsic.X86Punpcklqdq
                        : Intrinsic.X86Punpckhqdq;

                    res = context.AddIntrinsic(punpckInst, res, context.VectorZero());

                    context.Copy(GetVec(op.Rd), res);
                }
            }
            else
            {
                Operand res = context.VectorZero();

                int pairs = op.GetPairsCount() >> op.Size;

                int baseIndex = part != 0 ? pairs : 0;

                for (int index = 0; index < pairs; index++)
                {
                    int pairIndex = index << 1;

                    Operand ne = EmitVectorExtractZx(context, op.Rn, baseIndex + index, op.Size);
                    Operand me = EmitVectorExtractZx(context, op.Rm, baseIndex + index, op.Size);

                    res = EmitVectorInsert(context, res, ne, pairIndex, op.Size);
                    res = EmitVectorInsert(context, res, me, pairIndex + 1, op.Size);
                }

                context.Copy(GetVec(op.Rd), res);
            }
        }
    }
}
