using ARMeilleure.Decoders;
using ARMeilleure.IntermediateRepresentation;
using ARMeilleure.Translation;
using System;
using static ARMeilleure.Instructions.InstEmitHelper;
using static ARMeilleure.Instructions.InstEmitSimdHelper;
using static ARMeilleure.Instructions.InstEmitSimdHelper32;
using static ARMeilleure.IntermediateRepresentation.Operand.Factory;

namespace ARMeilleure.Instructions
{
    static partial class InstEmit32
    {
        #region "Masks"
        // Same as InstEmitSimdMove, as the instructions do the same thing.
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

        public static void Vmov_I(ArmEmitterContext context)
        {
            EmitVectorImmUnaryOp32(context, (op1) => op1);
        }

        public static void Vmvn_I(ArmEmitterContext context)
        {
            if (Optimizations.UseAvx512Ortho)
            {
                EmitVectorUnaryOpSimd32(context, (op1) =>
                {
                    return context.AddIntrinsic(Intrinsic.X86Vpternlogd, op1, op1, Const(0b01010101));
                });
            }
            else if (Optimizations.UseSse2)
            {
                EmitVectorUnaryOpSimd32(context, (op1) =>
                {
                    Operand mask = X86GetAllElements(context, -1L);
                    return context.AddIntrinsic(Intrinsic.X86Pandn, op1, mask);
                });
            }
            else
            {
                EmitVectorUnaryOpZx32(context, (op1) => context.BitwiseNot(op1));
            }
        }

        public static void Vmvn_II(ArmEmitterContext context)
        {
            EmitVectorImmUnaryOp32(context, (op1) => context.BitwiseNot(op1));
        }

        public static void Vmov_GS(ArmEmitterContext context)
        {
            OpCode32SimdMovGp op = (OpCode32SimdMovGp)context.CurrOp;

            Operand vec = GetVecA32(op.Vn >> 2);
            if (op.Op == 1)
            {
                // To general purpose.
                Operand value = context.VectorExtract(OperandType.I32, vec, op.Vn & 0x3);
                SetIntA32(context, op.Rt, value);
            }
            else
            {
                // From general purpose.
                Operand value = GetIntA32(context, op.Rt);
                context.Copy(vec, context.VectorInsert(vec, value, op.Vn & 0x3));
            }
        }

        public static void Vmov_G1(ArmEmitterContext context)
        {
            OpCode32SimdMovGpElem op = (OpCode32SimdMovGpElem)context.CurrOp;

            int index = op.Index + ((op.Vd & 1) << (3 - op.Size));
            if (op.Op == 1)
            {
                // To general purpose.
                Operand value = EmitVectorExtract32(context, op.Vd >> 1, index, op.Size, !op.U);
                SetIntA32(context, op.Rt, value);
            }
            else
            {
                // From general purpose.
                Operand vec = GetVecA32(op.Vd >> 1);
                Operand value = GetIntA32(context, op.Rt);
                context.Copy(vec, EmitVectorInsert(context, vec, value, index, op.Size));
            }
        }

        public static void Vmov_G2(ArmEmitterContext context)
        {
            OpCode32SimdMovGpDouble op = (OpCode32SimdMovGpDouble)context.CurrOp;

            Operand vec = GetVecA32(op.Vm >> 2);
            int vm1 = op.Vm + 1;
            bool sameOwnerVec = (op.Vm >> 2) == (vm1 >> 2);
            Operand vec2 = sameOwnerVec ? vec : GetVecA32(vm1 >> 2);
            if (op.Op == 1)
            {
                // To general purpose.
                Operand lowValue = context.VectorExtract(OperandType.I32, vec, op.Vm & 3);
                SetIntA32(context, op.Rt, lowValue);

                Operand highValue = context.VectorExtract(OperandType.I32, vec2, vm1 & 3);
                SetIntA32(context, op.Rt2, highValue);
            }
            else
            {
                // From general purpose.
                Operand lowValue = GetIntA32(context, op.Rt);
                Operand resultVec = context.VectorInsert(vec, lowValue, op.Vm & 3);

                Operand highValue = GetIntA32(context, op.Rt2);

                if (sameOwnerVec)
                {
                    context.Copy(vec, context.VectorInsert(resultVec, highValue, vm1 & 3));
                }
                else
                {
                    context.Copy(vec, resultVec);
                    context.Copy(vec2, context.VectorInsert(vec2, highValue, vm1 & 3));
                }
            }
        }

        public static void Vmov_GD(ArmEmitterContext context)
        {
            OpCode32SimdMovGpDouble op = (OpCode32SimdMovGpDouble)context.CurrOp;

            Operand vec = GetVecA32(op.Vm >> 1);
            if (op.Op == 1)
            {
                // To general purpose.
                Operand value = context.VectorExtract(OperandType.I64, vec, op.Vm & 1);
                SetIntA32(context, op.Rt, context.ConvertI64ToI32(value));
                SetIntA32(context, op.Rt2, context.ConvertI64ToI32(context.ShiftRightUI(value, Const(32))));
            }
            else
            {
                // From general purpose.
                Operand lowValue = GetIntA32(context, op.Rt);
                Operand highValue = GetIntA32(context, op.Rt2);

                Operand value = context.BitwiseOr(
                    context.ZeroExtend32(OperandType.I64, lowValue),
                    context.ShiftLeft(context.ZeroExtend32(OperandType.I64, highValue), Const(32)));

                context.Copy(vec, context.VectorInsert(vec, value, op.Vm & 1));
            }
        }

        public static void Vmovl(ArmEmitterContext context)
        {
            OpCode32SimdLong op = (OpCode32SimdLong)context.CurrOp;

            Operand res = context.VectorZero();

            int elems = op.GetBytesCount() >> op.Size;

            for (int index = 0; index < elems; index++)
            {
                Operand me = EmitVectorExtract32(context, op.Qm, op.Im + index, op.Size, !op.U);

                if (op.Size == 2)
                {
                    if (op.U)
                    {
                        me = context.ZeroExtend32(OperandType.I64, me);
                    }
                    else
                    {
                        me = context.SignExtend32(OperandType.I64, me);
                    }
                }

                res = EmitVectorInsert(context, res, me, index, op.Size + 1);
            }

            context.Copy(GetVecA32(op.Qd), res);
        }

        public static void Vswp(ArmEmitterContext context)
        {
            OpCode32Simd op = (OpCode32Simd)context.CurrOp;

            if (op.Q)
            {
                Operand temp = context.Copy(GetVecA32(op.Qd));

                context.Copy(GetVecA32(op.Qd), GetVecA32(op.Qm));
                context.Copy(GetVecA32(op.Qm), temp);
            }
            else
            {
                Operand temp = ExtractScalar(context, OperandType.I64, op.Vd);

                InsertScalar(context, op.Vd, ExtractScalar(context, OperandType.I64, op.Vm));
                InsertScalar(context, op.Vm, temp);
            }
        }

        public static void Vtbl(ArmEmitterContext context)
        {
            OpCode32SimdTbl op = (OpCode32SimdTbl)context.CurrOp;

            bool extension = op.Opc == 1;
            int length = op.Length + 1;

            if (Optimizations.UseSsse3)
            {
                Operand d = GetVecA32(op.Qd);
                Operand m = EmitMoveDoubleWordToSide(context, GetVecA32(op.Qm), op.Vm, 0);

                Operand res;
                Operand mask = X86GetAllElements(context, 0x0707070707070707L);

                // Fast path for single register table.
                {
                    Operand n = EmitMoveDoubleWordToSide(context, GetVecA32(op.Qn), op.Vn, 0);

                    Operand mMask = context.AddIntrinsic(Intrinsic.X86Pcmpgtb, m, mask);
                    mMask = context.AddIntrinsic(Intrinsic.X86Por, mMask, m);

                    res = context.AddIntrinsic(Intrinsic.X86Pshufb, n, mMask);
                }

                for (int index = 1; index < length; index++)
                {
                    int newVn = (op.Vn + index) & 0x1F;
                    (int qn, _) = GetQuadwordAndSubindex(newVn, op.RegisterSize);
                    Operand ni = EmitMoveDoubleWordToSide(context, GetVecA32(qn), newVn, 0);

                    Operand idxMask = X86GetAllElements(context, 0x0808080808080808L * index);

                    Operand mSubMask = context.AddIntrinsic(Intrinsic.X86Psubb, m, idxMask);

                    Operand mMask = context.AddIntrinsic(Intrinsic.X86Pcmpgtb, mSubMask, mask);
                    mMask = context.AddIntrinsic(Intrinsic.X86Por, mMask, mSubMask);

                    Operand res2 = context.AddIntrinsic(Intrinsic.X86Pshufb, ni, mMask);

                    res = context.AddIntrinsic(Intrinsic.X86Por, res, res2);
                }

                if (extension)
                {
                    Operand idxMask = X86GetAllElements(context, (0x0808080808080808L * length) - 0x0101010101010101L);
                    Operand zeroMask = context.VectorZero();

                    Operand mPosMask = context.AddIntrinsic(Intrinsic.X86Pcmpgtb, m, idxMask);
                    Operand mNegMask = context.AddIntrinsic(Intrinsic.X86Pcmpgtb, zeroMask, m);

                    Operand mMask = context.AddIntrinsic(Intrinsic.X86Por, mPosMask, mNegMask);

                    Operand dMask = context.AddIntrinsic(Intrinsic.X86Pand, EmitMoveDoubleWordToSide(context, d, op.Vd, 0), mMask);

                    res = context.AddIntrinsic(Intrinsic.X86Por, res, dMask);
                }

                res = EmitMoveDoubleWordToSide(context, res, 0, op.Vd);

                context.Copy(d, EmitDoubleWordInsert(context, d, res, op.Vd));
            }
            else
            {
                int elems = op.GetBytesCount() >> op.Size;

                (int Qx, int Ix)[] tableTuples = new (int, int)[length];
                for (int i = 0; i < length; i++)
                {
                    tableTuples[i] = GetQuadwordAndSubindex(op.Vn + i, op.RegisterSize);
                }

                int byteLength = length * 8;

                Operand res = GetVecA32(op.Qd);
                Operand m = GetVecA32(op.Qm);

                for (int index = 0; index < elems; index++)
                {
                    Operand selectedIndex = context.ZeroExtend8(OperandType.I32, context.VectorExtract8(m, index + op.Im));

                    Operand inRange = context.ICompareLess(selectedIndex, Const(byteLength));
                    Operand elemRes = default; // Note: This is I64 for ease of calculation.

                    // TODO: Branching rather than conditional select.

                    // Get indexed byte.
                    // To simplify (ha) the il, we get bytes from every vector and use a nested conditional select to choose the right result.
                    // This does have to extract `length` times for every element but certainly not as bad as it could be.

                    // Which vector number is the index on.
                    Operand vecIndex = context.ShiftRightUI(selectedIndex, Const(3));
                    // What should we shift by to extract it.
                    Operand subVecIndexShift = context.ShiftLeft(context.BitwiseAnd(selectedIndex, Const(7)), Const(3));

                    for (int i = 0; i < length; i++)
                    {
                        (int qx, int ix) = tableTuples[i];
                        // Get the whole vector, we'll get a byte out of it.
                        Operand lookupResult;
                        if (qx == op.Qd)
                        {
                            // Result contains the current state of the vector.
                            lookupResult = context.VectorExtract(OperandType.I64, res, ix);
                        }
                        else
                        {
                            lookupResult = EmitVectorExtract32(context, qx, ix, 3, false); // I64
                        }

                        lookupResult = context.ShiftRightUI(lookupResult, subVecIndexShift); // Get the relevant byte from this vector.

                        if (i == 0)
                        {
                            elemRes = lookupResult; // First result is always default.
                        }
                        else
                        {
                            Operand isThisElem = context.ICompareEqual(vecIndex, Const(i));
                            elemRes = context.ConditionalSelect(isThisElem, lookupResult, elemRes);
                        }
                    }

                    Operand fallback = (extension) ? context.ZeroExtend32(OperandType.I64, EmitVectorExtract32(context, op.Qd, index + op.Id, 0, false)) : Const(0L);

                    res = EmitVectorInsert(context, res, context.ConditionalSelect(inRange, elemRes, fallback), index + op.Id, 0);
                }

                context.Copy(GetVecA32(op.Qd), res);
            }
        }

        public static void Vtrn(ArmEmitterContext context)
        {
            OpCode32SimdCmpZ op = (OpCode32SimdCmpZ)context.CurrOp;

            if (Optimizations.UseSsse3)
            {
                EmitVectorShuffleOpSimd32(context, (m, d) =>
                {
                    Operand mask = default;

                    if (op.Size < 3)
                    {
                        long maskE0 = EvenMasks[op.Size];
                        long maskE1 = OddMasks[op.Size];

                        mask = X86GetScalar(context, maskE0);

                        mask = EmitVectorInsert(context, mask, Const(maskE1), 1, 3);
                    }

                    if (op.Size < 3)
                    {
                        d = context.AddIntrinsic(Intrinsic.X86Pshufb, d, mask);
                        m = context.AddIntrinsic(Intrinsic.X86Pshufb, m, mask);
                    }

                    Operand resD = context.AddIntrinsic(X86PunpcklInstruction[op.Size], d, m);
                    Operand resM = context.AddIntrinsic(X86PunpckhInstruction[op.Size], d, m);

                    return (resM, resD);
                });
            }
            else
            {
                int elems = op.GetBytesCount() >> op.Size;
                int pairs = elems >> 1;

                bool overlap = op.Qm == op.Qd;

                Operand resD = GetVecA32(op.Qd);
                Operand resM = GetVecA32(op.Qm);

                for (int index = 0; index < pairs; index++)
                {
                    int pairIndex = index << 1;
                    Operand d2 = EmitVectorExtract32(context, op.Qd, pairIndex + 1 + op.Id, op.Size, false);
                    Operand m1 = EmitVectorExtract32(context, op.Qm, pairIndex + op.Im, op.Size, false);

                    resD = EmitVectorInsert(context, resD, m1, pairIndex + 1 + op.Id, op.Size);

                    if (overlap)
                    {
                        resM = resD;
                    }

                    resM = EmitVectorInsert(context, resM, d2, pairIndex + op.Im, op.Size);

                    if (overlap)
                    {
                        resD = resM;
                    }
                }

                context.Copy(GetVecA32(op.Qd), resD);
                if (!overlap)
                {
                    context.Copy(GetVecA32(op.Qm), resM);
                }
            }
        }

        public static void Vzip(ArmEmitterContext context)
        {
            OpCode32SimdCmpZ op = (OpCode32SimdCmpZ)context.CurrOp;

            if (Optimizations.UseAdvSimd)
            {
                EmitVectorZipUzpOpSimd32(context, Intrinsic.Arm64Zip1V, Intrinsic.Arm64Zip2V);
            }
            else if (Optimizations.UseSse2)
            {
                EmitVectorShuffleOpSimd32(context, (m, d) =>
                {
                    if (op.RegisterSize == RegisterSize.Simd128)
                    {
                        Operand resD = context.AddIntrinsic(X86PunpcklInstruction[op.Size], d, m);
                        Operand resM = context.AddIntrinsic(X86PunpckhInstruction[op.Size], d, m);

                        return (resM, resD);
                    }
                    else
                    {
                        Operand res = context.AddIntrinsic(X86PunpcklInstruction[op.Size], d, m);

                        Operand resD = context.AddIntrinsic(Intrinsic.X86Punpcklqdq, res, context.VectorZero());
                        Operand resM = context.AddIntrinsic(Intrinsic.X86Punpckhqdq, res, context.VectorZero());
                        return (resM, resD);
                    }
                });
            }
            else
            {
                int elems = op.GetBytesCount() >> op.Size;
                int pairs = elems >> 1;

                bool overlap = op.Qm == op.Qd;

                Operand resD = GetVecA32(op.Qd);
                Operand resM = GetVecA32(op.Qm);

                for (int index = 0; index < pairs; index++)
                {
                    int pairIndex = index << 1;
                    Operand dRowD = EmitVectorExtract32(context, op.Qd, index + op.Id, op.Size, false);
                    Operand mRowD = EmitVectorExtract32(context, op.Qm, index + op.Im, op.Size, false);

                    Operand dRowM = EmitVectorExtract32(context, op.Qd, index + op.Id + pairs, op.Size, false);
                    Operand mRowM = EmitVectorExtract32(context, op.Qm, index + op.Im + pairs, op.Size, false);

                    resD = EmitVectorInsert(context, resD, dRowD, pairIndex + op.Id, op.Size);
                    resD = EmitVectorInsert(context, resD, mRowD, pairIndex + 1 + op.Id, op.Size);

                    if (overlap)
                    {
                        resM = resD;
                    }

                    resM = EmitVectorInsert(context, resM, dRowM, pairIndex + op.Im, op.Size);
                    resM = EmitVectorInsert(context, resM, mRowM, pairIndex + 1 + op.Im, op.Size);

                    if (overlap)
                    {
                        resD = resM;
                    }
                }

                context.Copy(GetVecA32(op.Qd), resD);
                if (!overlap)
                {
                    context.Copy(GetVecA32(op.Qm), resM);
                }
            }
        }

        public static void Vuzp(ArmEmitterContext context)
        {
            OpCode32SimdCmpZ op = (OpCode32SimdCmpZ)context.CurrOp;

            if (Optimizations.UseAdvSimd)
            {
                EmitVectorZipUzpOpSimd32(context, Intrinsic.Arm64Uzp1V, Intrinsic.Arm64Uzp2V);
            }
            else if (Optimizations.UseSsse3)
            {
                EmitVectorShuffleOpSimd32(context, (m, d) =>
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

                            d = context.AddIntrinsic(Intrinsic.X86Pshufb, d, mask);
                            m = context.AddIntrinsic(Intrinsic.X86Pshufb, m, mask);
                        }

                        Operand resD = context.AddIntrinsic(Intrinsic.X86Punpcklqdq, d, m);
                        Operand resM = context.AddIntrinsic(Intrinsic.X86Punpckhqdq, d, m);

                        return (resM, resD);
                    }
                    else
                    {
                        Intrinsic punpcklInst = X86PunpcklInstruction[op.Size];

                        Operand res = context.AddIntrinsic(punpcklInst, d, m);

                        if (op.Size < 2)
                        {
                            long maskE0 = _masksE0_Uzp[op.Size];
                            long maskE1 = _masksE1_Uzp[op.Size];

                            Operand mask = X86GetScalar(context, maskE0);

                            mask = EmitVectorInsert(context, mask, Const(maskE1), 1, 3);

                            res = context.AddIntrinsic(Intrinsic.X86Pshufb, res, mask);
                        }

                        Operand resD = context.AddIntrinsic(Intrinsic.X86Punpcklqdq, res, context.VectorZero());
                        Operand resM = context.AddIntrinsic(Intrinsic.X86Punpckhqdq, res, context.VectorZero());

                        return (resM, resD);
                    }
                });
            }
            else
            {
                int elems = op.GetBytesCount() >> op.Size;
                int pairs = elems >> 1;

                bool overlap = op.Qm == op.Qd;

                Operand resD = GetVecA32(op.Qd);
                Operand resM = GetVecA32(op.Qm);

                for (int index = 0; index < elems; index++)
                {
                    Operand dIns, mIns;
                    if (index >= pairs)
                    {
                        int pairIndex = index - pairs;
                        dIns = EmitVectorExtract32(context, op.Qm, (pairIndex << 1) + op.Im, op.Size, false);
                        mIns = EmitVectorExtract32(context, op.Qm, ((pairIndex << 1) | 1) + op.Im, op.Size, false);
                    }
                    else
                    {
                        dIns = EmitVectorExtract32(context, op.Qd, (index << 1) + op.Id, op.Size, false);
                        mIns = EmitVectorExtract32(context, op.Qd, ((index << 1) | 1) + op.Id, op.Size, false);
                    }

                    resD = EmitVectorInsert(context, resD, dIns, index + op.Id, op.Size);

                    if (overlap)
                    {
                        resM = resD;
                    }

                    resM = EmitVectorInsert(context, resM, mIns, index + op.Im, op.Size);

                    if (overlap)
                    {
                        resD = resM;
                    }
                }

                context.Copy(GetVecA32(op.Qd), resD);
                if (!overlap)
                {
                    context.Copy(GetVecA32(op.Qm), resM);
                }
            }
        }

        private static void EmitVectorZipUzpOpSimd32(ArmEmitterContext context, Intrinsic inst1, Intrinsic inst2)
        {
            OpCode32SimdCmpZ op = (OpCode32SimdCmpZ)context.CurrOp;

            bool overlap = op.Qm == op.Qd;

            Operand d = GetVecA32(op.Qd);
            Operand m = GetVecA32(op.Qm);

            Operand dPart = d;
            Operand mPart = m;

            if (!op.Q) // Register swap: move relevant doubleword to destination side.
            {
                dPart = InstEmitSimdHelper32Arm64.EmitMoveDoubleWordToSide(context, d, op.Vd, 0);
                mPart = InstEmitSimdHelper32Arm64.EmitMoveDoubleWordToSide(context, m, op.Vm, 0);
            }

            Intrinsic vSize = op.Q ? Intrinsic.Arm64V128 : Intrinsic.Arm64V64;

            vSize |= (Intrinsic)(op.Size << (int)Intrinsic.Arm64VSizeShift);

            Operand resD = context.AddIntrinsic(inst1 | vSize, dPart, mPart);
            Operand resM = context.AddIntrinsic(inst2 | vSize, dPart, mPart);

            if (!op.Q) // Register insert.
            {
                resD = context.AddIntrinsic(Intrinsic.Arm64InsVe | Intrinsic.Arm64VDWord, d, Const(op.Vd & 1), resD, Const(0));

                if (overlap)
                {
                    resD = context.AddIntrinsic(Intrinsic.Arm64InsVe | Intrinsic.Arm64VDWord, resD, Const(op.Vm & 1), resM, Const(0));
                }
                else
                {
                    resM = context.AddIntrinsic(Intrinsic.Arm64InsVe | Intrinsic.Arm64VDWord, m, Const(op.Vm & 1), resM, Const(0));
                }
            }

            context.Copy(d, resD);
            if (!overlap)
            {
                context.Copy(m, resM);
            }
        }

        private static void EmitVectorShuffleOpSimd32(ArmEmitterContext context, Func<Operand, Operand, (Operand, Operand)> shuffleFunc)
        {
            OpCode32Simd op = (OpCode32Simd)context.CurrOp;

            Operand m = GetVecA32(op.Qm);
            Operand d = GetVecA32(op.Qd);
            Operand initialM = m;
            Operand initialD = d;

            if (!op.Q) // Register swap: move relevant doubleword to side 0, for consistency.
            {
                m = EmitMoveDoubleWordToSide(context, m, op.Vm, 0);
                d = EmitMoveDoubleWordToSide(context, d, op.Vd, 0);
            }

            (Operand resM, Operand resD) = shuffleFunc(m, d);

            bool overlap = op.Qm == op.Qd;

            if (!op.Q) // Register insert.
            {
                resM = EmitDoubleWordInsert(context, initialM, EmitMoveDoubleWordToSide(context, resM, 0, op.Vm), op.Vm);
                resD = EmitDoubleWordInsert(context, overlap ? resM : initialD, EmitMoveDoubleWordToSide(context, resD, 0, op.Vd), op.Vd);
            }

            if (!overlap)
            {
                context.Copy(initialM, resM);
            }

            context.Copy(initialD, resD);
        }
    }
}
