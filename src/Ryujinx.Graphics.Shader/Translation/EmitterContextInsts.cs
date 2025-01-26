using Ryujinx.Graphics.Shader.IntermediateRepresentation;
using System;

using static Ryujinx.Graphics.Shader.IntermediateRepresentation.OperandHelper;

namespace Ryujinx.Graphics.Shader.Translation
{
    static class EmitterContextInsts
    {
        public static Operand AtomicAdd(this EmitterContext context, StorageKind storageKind, Operand a, Operand b, Operand c)
        {
            return context.Add(Instruction.AtomicAdd, storageKind, Local(), a, b, c);
        }

        public static Operand AtomicAnd(this EmitterContext context, StorageKind storageKind, Operand a, Operand b, Operand c)
        {
            return context.Add(Instruction.AtomicAnd, storageKind, Local(), a, b, c);
        }

        public static Operand AtomicCompareAndSwap(this EmitterContext context, StorageKind storageKind, Operand a, Operand b, Operand c, Operand d)
        {
            return context.Add(Instruction.AtomicCompareAndSwap, storageKind, Local(), a, b, c, d);
        }

        public static Operand AtomicMaxS32(this EmitterContext context, StorageKind storageKind, Operand a, Operand b, Operand c)
        {
            return context.Add(Instruction.AtomicMaxS32, storageKind, Local(), a, b, c);
        }

        public static Operand AtomicMaxU32(this EmitterContext context, StorageKind storageKind, Operand a, Operand b, Operand c)
        {
            return context.Add(Instruction.AtomicMaxU32, storageKind, Local(), a, b, c);
        }

        public static Operand AtomicMinS32(this EmitterContext context, StorageKind storageKind, Operand a, Operand b, Operand c)
        {
            return context.Add(Instruction.AtomicMinS32, storageKind, Local(), a, b, c);
        }

        public static Operand AtomicMinU32(this EmitterContext context, StorageKind storageKind, Operand a, Operand b, Operand c)
        {
            return context.Add(Instruction.AtomicMinU32, storageKind, Local(), a, b, c);
        }

        public static Operand AtomicOr(this EmitterContext context, StorageKind storageKind, Operand a, Operand b, Operand c)
        {
            return context.Add(Instruction.AtomicOr, storageKind, Local(), a, b, c);
        }

        public static Operand AtomicSwap(this EmitterContext context, StorageKind storageKind, Operand a, Operand b, Operand c)
        {
            return context.Add(Instruction.AtomicSwap, storageKind, Local(), a, b, c);
        }

        public static Operand AtomicXor(this EmitterContext context, StorageKind storageKind, Operand a, Operand b, Operand c)
        {
            return context.Add(Instruction.AtomicXor, storageKind, Local(), a, b, c);
        }

        public static Operand AtomicAdd(this EmitterContext context, StorageKind storageKind, int binding, Operand e0, Operand e1, Operand value)
        {
            return context.Add(Instruction.AtomicAdd, storageKind, Local(), Const(binding), e0, e1, value);
        }

        public static Operand AtomicAnd(this EmitterContext context, StorageKind storageKind, int binding, Operand e0, Operand e1, Operand value)
        {
            return context.Add(Instruction.AtomicAnd, storageKind, Local(), Const(binding), e0, e1, value);
        }

        public static Operand AtomicCompareAndSwap(this EmitterContext context, StorageKind storageKind, int binding, Operand e0, Operand compare, Operand value)
        {
            return context.Add(Instruction.AtomicCompareAndSwap, storageKind, Local(), Const(binding), e0, compare, value);
        }

        public static Operand AtomicCompareAndSwap(this EmitterContext context, StorageKind storageKind, int binding, Operand e0, Operand e1, Operand compare, Operand value)
        {
            return context.Add(Instruction.AtomicCompareAndSwap, storageKind, Local(), Const(binding), e0, e1, compare, value);
        }

        public static Operand AtomicMaxS32(this EmitterContext context, StorageKind storageKind, int binding, Operand e0, Operand e1, Operand value)
        {
            return context.Add(Instruction.AtomicMaxS32, storageKind, Local(), Const(binding), e0, e1, value);
        }

        public static Operand AtomicMaxU32(this EmitterContext context, StorageKind storageKind, int binding, Operand e0, Operand e1, Operand value)
        {
            return context.Add(Instruction.AtomicMaxU32, storageKind, Local(), Const(binding), e0, e1, value);
        }

        public static Operand AtomicMinS32(this EmitterContext context, StorageKind storageKind, int binding, Operand e0, Operand e1, Operand value)
        {
            return context.Add(Instruction.AtomicMinS32, storageKind, Local(), Const(binding), e0, e1, value);
        }

        public static Operand AtomicMinU32(this EmitterContext context, StorageKind storageKind, int binding, Operand e0, Operand e1, Operand value)
        {
            return context.Add(Instruction.AtomicMinU32, storageKind, Local(), Const(binding), e0, e1, value);
        }

        public static Operand AtomicOr(this EmitterContext context, StorageKind storageKind, int binding, Operand e0, Operand e1, Operand value)
        {
            return context.Add(Instruction.AtomicOr, storageKind, Local(), Const(binding), e0, e1, value);
        }

        public static Operand AtomicSwap(this EmitterContext context, StorageKind storageKind, int binding, Operand e0, Operand e1, Operand value)
        {
            return context.Add(Instruction.AtomicSwap, storageKind, Local(), Const(binding), e0, e1, value);
        }

        public static Operand AtomicXor(this EmitterContext context, StorageKind storageKind, int binding, Operand e0, Operand e1, Operand value)
        {
            return context.Add(Instruction.AtomicXor, storageKind, Local(), Const(binding), e0, e1, value);
        }

        public static Operand Ballot(this EmitterContext context, Operand a, int index)
        {
            Operand dest = Local();

            context.Add(new Operation(Instruction.Ballot, index, dest, a));

            return dest;
        }

        public static Operand Barrier(this EmitterContext context)
        {
            return context.Add(Instruction.Barrier);
        }

        public static Operand BitCount(this EmitterContext context, Operand a)
        {
            return context.Add(Instruction.BitCount, Local(), a);
        }

        public static Operand BitfieldExtractS32(this EmitterContext context, Operand a, Operand b, Operand c)
        {
            return context.Add(Instruction.BitfieldExtractS32, Local(), a, b, c);
        }

        public static Operand BitfieldExtractU32(this EmitterContext context, Operand a, Operand b, Operand c)
        {
            return context.Add(Instruction.BitfieldExtractU32, Local(), a, b, c);
        }

        public static Operand BitfieldInsert(this EmitterContext context, Operand a, Operand b, Operand c, Operand d)
        {
            return context.Add(Instruction.BitfieldInsert, Local(), a, b, c, d);
        }

        public static Operand BitfieldReverse(this EmitterContext context, Operand a)
        {
            return context.Add(Instruction.BitfieldReverse, Local(), a);
        }

        public static Operand BitwiseAnd(this EmitterContext context, Operand a, Operand b)
        {
            return context.Add(Instruction.BitwiseAnd, Local(), a, b);
        }

        public static Operand BitwiseExclusiveOr(this EmitterContext context, Operand a, Operand b)
        {
            return context.Add(Instruction.BitwiseExclusiveOr, Local(), a, b);
        }

        public static Operand BitwiseNot(this EmitterContext context, Operand a, bool invert)
        {
            if (invert)
            {
                a = context.BitwiseNot(a);
            }

            return a;
        }

        public static Operand BitwiseNot(this EmitterContext context, Operand a)
        {
            return context.Add(Instruction.BitwiseNot, Local(), a);
        }

        public static Operand BitwiseOr(this EmitterContext context, Operand a, Operand b)
        {
            return context.Add(Instruction.BitwiseOr, Local(), a, b);
        }

        public static Operand Branch(this EmitterContext context, Operand d)
        {
            return context.Add(Instruction.Branch, d);
        }

        public static Operand BranchIfFalse(this EmitterContext context, Operand d, Operand a)
        {
            return context.Add(Instruction.BranchIfFalse, d, a);
        }

        public static Operand BranchIfTrue(this EmitterContext context, Operand d, Operand a)
        {
            return context.Add(Instruction.BranchIfTrue, d, a);
        }

        public static Operand Call(this EmitterContext context, int funcId, bool returns, params Operand[] args)
        {
            Operand[] args2 = new Operand[args.Length + 1];

            args2[0] = Const(funcId);
            args.CopyTo(args2, 1);

            return context.Add(Instruction.Call, returns ? Local() : null, args2);
        }

        public static Operand ConditionalSelect(this EmitterContext context, Operand a, Operand b, Operand c)
        {
            return context.Add(Instruction.ConditionalSelect, Local(), a, b, c);
        }

        public static Operand Copy(this EmitterContext context, Operand a)
        {
            return context.Add(Instruction.Copy, Local(), a);
        }

        public static void Copy(this EmitterContext context, Operand d, Operand a)
        {
            if (d.Type == OperandType.Constant)
            {
                return;
            }

            context.Add(Instruction.Copy, d, a);
        }

        public static Operand Discard(this EmitterContext context)
        {
            return context.Add(Instruction.Discard);
        }

        public static Operand EmitVertex(this EmitterContext context)
        {
            return context.Add(Instruction.EmitVertex);
        }

        public static Operand EndPrimitive(this EmitterContext context)
        {
            return context.Add(Instruction.EndPrimitive);
        }

        public static Operand FindLSB(this EmitterContext context, Operand a)
        {
            return context.Add(Instruction.FindLSB, Local(), a);
        }

        public static Operand FindMSBS32(this EmitterContext context, Operand a)
        {
            return context.Add(Instruction.FindMSBS32, Local(), a);
        }

        public static Operand FindMSBU32(this EmitterContext context, Operand a)
        {
            return context.Add(Instruction.FindMSBU32, Local(), a);
        }

        public static Operand FP32ConvertToFP64(this EmitterContext context, Operand a)
        {
            return context.Add(Instruction.ConvertFP32ToFP64, Local(), a);
        }

        public static Operand FP64ConvertToFP32(this EmitterContext context, Operand a)
        {
            return context.Add(Instruction.ConvertFP64ToFP32, Local(), a);
        }

        public static Operand FPAbsNeg(this EmitterContext context, Operand a, bool abs, bool neg, Instruction fpType = Instruction.FP32)
        {
            return context.FPNegate(context.FPAbsolute(a, abs, fpType), neg, fpType);
        }

        public static Operand FPAbsolute(this EmitterContext context, Operand a, bool abs, Instruction fpType = Instruction.FP32)
        {
            if (abs)
            {
                a = context.FPAbsolute(a, fpType);
            }

            return a;
        }

        public static Operand FPAbsolute(this EmitterContext context, Operand a, Instruction fpType = Instruction.FP32)
        {
            return context.Add(fpType | Instruction.Absolute, Local(), a);
        }

        public static Operand FPAdd(this EmitterContext context, Operand a, Operand b, Instruction fpType = Instruction.FP32)
        {
            return context.Add(fpType | Instruction.Add, Local(), a, b);
        }

        public static Operand FPCeiling(this EmitterContext context, Operand a, Instruction fpType = Instruction.FP32)
        {
            return context.Add(fpType | Instruction.Ceiling, Local(), a);
        }

        public static Operand FPCompareEqual(this EmitterContext context, Operand a, Operand b, Instruction fpType = Instruction.FP32)
        {
            return context.Add(fpType | Instruction.CompareEqual, Local(), a, b);
        }

        public static Operand FPCompareLess(this EmitterContext context, Operand a, Operand b, Instruction fpType = Instruction.FP32)
        {
            return context.Add(fpType | Instruction.CompareLess, Local(), a, b);
        }

        public static Operand FP32ConvertToS32(this EmitterContext context, Operand a)
        {
            return context.Add(Instruction.ConvertFP32ToS32, Local(), a);
        }

        public static Operand FP32ConvertToU32(this EmitterContext context, Operand a)
        {
            return context.Add(Instruction.ConvertFP32ToU32, Local(), a);
        }

        public static Operand FP64ConvertToS32(this EmitterContext context, Operand a)
        {
            return context.Add(Instruction.ConvertFP64ToS32, Local(), a);
        }

        public static Operand FP64ConvertToU32(this EmitterContext context, Operand a)
        {
            return context.Add(Instruction.ConvertFP64ToU32, Local(), a);
        }

        public static Operand FPCosine(this EmitterContext context, Operand a)
        {
            return context.Add(Instruction.FP32 | Instruction.Cosine, Local(), a);
        }

        public static Operand FPDivide(this EmitterContext context, Operand a, Operand b, Instruction fpType = Instruction.FP32)
        {
            return context.Add(fpType | Instruction.Divide, Local(), a, b);
        }

        public static Operand FPExponentB2(this EmitterContext context, Operand a)
        {
            return context.Add(Instruction.FP32 | Instruction.ExponentB2, Local(), a);
        }

        public static Operand FPFloor(this EmitterContext context, Operand a, Instruction fpType = Instruction.FP32)
        {
            return context.Add(fpType | Instruction.Floor, Local(), a);
        }

        public static Operand FPFusedMultiplyAdd(this EmitterContext context, Operand a, Operand b, Operand c, Instruction fpType = Instruction.FP32)
        {
            return context.Add(fpType | Instruction.FusedMultiplyAdd, Local(), a, b, c);
        }

        public static Operand FPLogarithmB2(this EmitterContext context, Operand a)
        {
            return context.Add(Instruction.FP32 | Instruction.LogarithmB2, Local(), a);
        }

        public static Operand FPMaximum(this EmitterContext context, Operand a, Operand b, Instruction fpType = Instruction.FP32)
        {
            return context.Add(fpType | Instruction.Maximum, Local(), a, b);
        }

        public static Operand FPMinimum(this EmitterContext context, Operand a, Operand b, Instruction fpType = Instruction.FP32)
        {
            return context.Add(fpType | Instruction.Minimum, Local(), a, b);
        }

        public static Operand FPModulo(this EmitterContext context, Operand a, Operand b, Instruction fpType = Instruction.FP32)
        {
            return context.Add(fpType | Instruction.Modulo, Local(), a, b);
        }

        public static Operand FPMultiply(this EmitterContext context, Operand a, Operand b, Instruction fpType = Instruction.FP32)
        {
            return context.Add(fpType | Instruction.Multiply, Local(), a, b);
        }

        public static Operand FPNegate(this EmitterContext context, Operand a, bool neg, Instruction fpType = Instruction.FP32)
        {
            if (neg)
            {
                a = context.FPNegate(a, fpType);
            }

            return a;
        }

        public static Operand FPNegate(this EmitterContext context, Operand a, Instruction fpType = Instruction.FP32)
        {
            return context.Add(fpType | Instruction.Negate, Local(), a);
        }

        public static Operand FPReciprocal(this EmitterContext context, Operand a, Instruction fpType = Instruction.FP32)
        {
            return context.FPDivide(fpType == Instruction.FP64 ? context.PackDouble2x32(1.0) : ConstF(1), a, fpType);
        }

        public static Operand FPReciprocalSquareRoot(this EmitterContext context, Operand a, Instruction fpType = Instruction.FP32)
        {
            return context.Add(fpType | Instruction.ReciprocalSquareRoot, Local(), a);
        }

        public static Operand FPRound(this EmitterContext context, Operand a, Instruction fpType = Instruction.FP32)
        {
            return context.Add(fpType | Instruction.Round, Local(), a);
        }

        public static Operand FPSaturate(this EmitterContext context, Operand a, bool sat, Instruction fpType = Instruction.FP32)
        {
            if (sat)
            {
                a = context.FPSaturate(a, fpType);
            }

            return a;
        }

        public static Operand FPSaturate(this EmitterContext context, Operand a, Instruction fpType = Instruction.FP32)
        {
            return fpType == Instruction.FP64
                ? context.Add(fpType | Instruction.Clamp, Local(), a, context.PackDouble2x32(0.0), context.PackDouble2x32(1.0))
                : context.Add(fpType | Instruction.Clamp, Local(), a, ConstF(0), ConstF(1));
        }

        public static Operand FPSine(this EmitterContext context, Operand a)
        {
            return context.Add(Instruction.FP32 | Instruction.Sine, Local(), a);
        }

        public static Operand FPSquareRoot(this EmitterContext context, Operand a)
        {
            return context.Add(Instruction.FP32 | Instruction.SquareRoot, Local(), a);
        }

        public static Operand FPSubtract(this EmitterContext context, Operand a, Operand b, Instruction fpType = Instruction.FP32)
        {
            return context.Add(fpType | Instruction.Subtract, Local(), a, b);
        }

        public static Operand FPTruncate(this EmitterContext context, Operand a, Instruction fpType = Instruction.FP32)
        {
            return context.Add(fpType | Instruction.Truncate, Local(), a);
        }

        public static Operand FPSwizzleAdd(this EmitterContext context, Operand a, Operand b, int mask)
        {
            return context.Add(Instruction.SwizzleAdd, Local(), a, b, Const(mask));
        }

        public static void FSIBegin(this EmitterContext context)
        {
            context.Add(Instruction.FSIBegin);
        }

        public static void FSIEnd(this EmitterContext context)
        {
            context.Add(Instruction.FSIEnd);
        }

        public static Operand GroupMemoryBarrier(this EmitterContext context)
        {
            return context.Add(Instruction.GroupMemoryBarrier);
        }

        public static Operand IAbsNeg(this EmitterContext context, Operand a, bool abs, bool neg)
        {
            return context.INegate(context.IAbsolute(a, abs), neg);
        }

        public static Operand IAbsolute(this EmitterContext context, Operand a, bool abs)
        {
            if (abs)
            {
                a = context.IAbsolute(a);
            }

            return a;
        }

        public static Operand IAbsolute(this EmitterContext context, Operand a)
        {
            return context.Add(Instruction.Absolute, Local(), a);
        }

        public static Operand IAdd(this EmitterContext context, Operand a, Operand b)
        {
            return context.Add(Instruction.Add, Local(), a, b);
        }

        public static Operand IClampS32(this EmitterContext context, Operand a, Operand b, Operand c)
        {
            return context.Add(Instruction.Clamp, Local(), a, b, c);
        }

        public static Operand IClampU32(this EmitterContext context, Operand a, Operand b, Operand c)
        {
            return context.Add(Instruction.ClampU32, Local(), a, b, c);
        }

        public static Operand ICompareEqual(this EmitterContext context, Operand a, Operand b)
        {
            return context.Add(Instruction.CompareEqual, Local(), a, b);
        }

        public static Operand ICompareGreater(this EmitterContext context, Operand a, Operand b)
        {
            return context.Add(Instruction.CompareGreater, Local(), a, b);
        }

        public static Operand ICompareGreaterOrEqual(this EmitterContext context, Operand a, Operand b)
        {
            return context.Add(Instruction.CompareGreaterOrEqual, Local(), a, b);
        }

        public static Operand ICompareGreaterOrEqualUnsigned(this EmitterContext context, Operand a, Operand b)
        {
            return context.Add(Instruction.CompareGreaterOrEqualU32, Local(), a, b);
        }

        public static Operand ICompareGreaterUnsigned(this EmitterContext context, Operand a, Operand b)
        {
            return context.Add(Instruction.CompareGreaterU32, Local(), a, b);
        }

        public static Operand ICompareLess(this EmitterContext context, Operand a, Operand b)
        {
            return context.Add(Instruction.CompareLess, Local(), a, b);
        }

        public static Operand ICompareLessOrEqual(this EmitterContext context, Operand a, Operand b)
        {
            return context.Add(Instruction.CompareLessOrEqual, Local(), a, b);
        }

        public static Operand ICompareLessOrEqualUnsigned(this EmitterContext context, Operand a, Operand b)
        {
            return context.Add(Instruction.CompareLessOrEqualU32, Local(), a, b);
        }

        public static Operand ICompareLessUnsigned(this EmitterContext context, Operand a, Operand b)
        {
            return context.Add(Instruction.CompareLessU32, Local(), a, b);
        }

        public static Operand ICompareNotEqual(this EmitterContext context, Operand a, Operand b)
        {
            return context.Add(Instruction.CompareNotEqual, Local(), a, b);
        }

        public static Operand IConvertS32ToFP32(this EmitterContext context, Operand a)
        {
            return context.Add(Instruction.ConvertS32ToFP32, Local(), a);
        }

        public static Operand IConvertS32ToFP64(this EmitterContext context, Operand a)
        {
            return context.Add(Instruction.ConvertS32ToFP64, Local(), a);
        }

        public static Operand IConvertU32ToFP32(this EmitterContext context, Operand a)
        {
            return context.Add(Instruction.ConvertU32ToFP32, Local(), a);
        }

        public static Operand IConvertU32ToFP64(this EmitterContext context, Operand a)
        {
            return context.Add(Instruction.ConvertU32ToFP64, Local(), a);
        }

        public static Operand IMaximumS32(this EmitterContext context, Operand a, Operand b)
        {
            return context.Add(Instruction.Maximum, Local(), a, b);
        }

        public static Operand IMaximumU32(this EmitterContext context, Operand a, Operand b)
        {
            return context.Add(Instruction.MaximumU32, Local(), a, b);
        }

        public static Operand IMinimumS32(this EmitterContext context, Operand a, Operand b)
        {
            return context.Add(Instruction.Minimum, Local(), a, b);
        }

        public static Operand IMinimumU32(this EmitterContext context, Operand a, Operand b)
        {
            return context.Add(Instruction.MinimumU32, Local(), a, b);
        }

        public static Operand IMultiply(this EmitterContext context, Operand a, Operand b)
        {
            return context.Add(Instruction.Multiply, Local(), a, b);
        }

        public static Operand INegate(this EmitterContext context, Operand a, bool neg)
        {
            if (neg)
            {
                a = context.INegate(a);
            }

            return a;
        }

        public static Operand INegate(this EmitterContext context, Operand a)
        {
            return context.Add(Instruction.Negate, Local(), a);
        }

        public static Operand ISubtract(this EmitterContext context, Operand a, Operand b)
        {
            return context.Add(Instruction.Subtract, Local(), a, b);
        }

        public static Operand ImageAtomic(
            this EmitterContext context,
            SamplerType type,
            TextureFormat format,
            TextureFlags flags,
            SetBindingPair setAndBinding,
            Operand[] sources)
        {
            Operand dest = Local();

            context.Add(new TextureOperation(
                Instruction.ImageAtomic,
                type,
                format,
                flags,
                setAndBinding.SetIndex,
                setAndBinding.Binding,
                0,
                [dest],
                sources));

            return dest;
        }

        public static void ImageLoad(
            this EmitterContext context,
            SamplerType type,
            TextureFormat format,
            TextureFlags flags,
            SetBindingPair setAndBinding,
            int compMask,
            Operand[] dests,
            Operand[] sources)
        {
            context.Add(new TextureOperation(
                Instruction.ImageLoad,
                type,
                format,
                flags,
                setAndBinding.SetIndex,
                setAndBinding.Binding,
                compMask,
                dests,
                sources));
        }

        public static void ImageStore(
            this EmitterContext context,
            SamplerType type,
            TextureFormat format,
            TextureFlags flags,
            SetBindingPair setAndBinding,
            Operand[] sources)
        {
            context.Add(new TextureOperation(
                Instruction.ImageStore,
                type,
                format,
                flags,
                setAndBinding.SetIndex,
                setAndBinding.Binding,
                0,
                null,
                sources));
        }

        public static Operand IsNan(this EmitterContext context, Operand a, Instruction fpType = Instruction.FP32)
        {
            return context.Add(fpType | Instruction.IsNan, Local(), a);
        }

        public static Operand Load(this EmitterContext context, StorageKind storageKind, Operand e0, Operand e1)
        {
            return context.Add(Instruction.Load, storageKind, Local(), e0, e1);
        }

        public static Operand Load(this EmitterContext context, StorageKind storageKind, int binding)
        {
            return context.Add(Instruction.Load, storageKind, Local(), Const(binding));
        }

        public static Operand Load(this EmitterContext context, StorageKind storageKind, int binding, Operand e0)
        {
            return context.Add(Instruction.Load, storageKind, Local(), Const(binding), e0);
        }

        public static Operand Load(this EmitterContext context, StorageKind storageKind, int binding, Operand e0, Operand e1)
        {
            return context.Add(Instruction.Load, storageKind, Local(), Const(binding), e0, e1);
        }

        public static Operand Load(this EmitterContext context, StorageKind storageKind, int binding, Operand e0, Operand e1, Operand e2)
        {
            return context.Add(Instruction.Load, storageKind, Local(), Const(binding), e0, e1, e2);
        }

        public static Operand Load(this EmitterContext context, StorageKind storageKind, IoVariable ioVariable, Operand primVertex = null)
        {
            return primVertex != null
                ? context.Load(storageKind, (int)ioVariable, primVertex)
                : context.Load(storageKind, (int)ioVariable);
        }

        public static Operand Load(
            this EmitterContext context,
            StorageKind storageKind,
            IoVariable ioVariable,
            Operand primVertex,
            Operand elemIndex)
        {
            return primVertex != null
                ? context.Load(storageKind, (int)ioVariable, primVertex, elemIndex)
                : context.Load(storageKind, (int)ioVariable, elemIndex);
        }

        public static Operand Load(
            this EmitterContext context,
            StorageKind storageKind,
            IoVariable ioVariable,
            Operand primVertex,
            Operand arrayIndex,
            Operand elemIndex)
        {
            return primVertex != null
                ? context.Load(storageKind, (int)ioVariable, primVertex, arrayIndex, elemIndex)
                : context.Load(storageKind, (int)ioVariable, arrayIndex, elemIndex);
        }

        public static Operand Lod(
            this EmitterContext context,
            SamplerType type,
            TextureFlags flags,
            SetBindingPair setAndBinding,
            int compIndex,
            Operand[] sources)
        {
            Operand dest = Local();

            context.Add(new TextureOperation(
                Instruction.Lod,
                type,
                TextureFormat.Unknown,
                flags,
                setAndBinding.SetIndex,
                setAndBinding.Binding,
                compIndex,
                [dest],
                sources));

            return dest;
        }

        public static Operand MemoryBarrier(this EmitterContext context)
        {
            return context.Add(Instruction.MemoryBarrier);
        }

        public static Operand MultiplyHighS32(this EmitterContext context, Operand a, Operand b)
        {
            return context.Add(Instruction.MultiplyHighS32, Local(), a, b);
        }

        public static Operand MultiplyHighU32(this EmitterContext context, Operand a, Operand b)
        {
            return context.Add(Instruction.MultiplyHighU32, Local(), a, b);
        }

        public static Operand PackDouble2x32(this EmitterContext context, double value)
        {
            long valueAsLong = BitConverter.DoubleToInt64Bits(value);

            return context.Add(Instruction.PackDouble2x32, Local(), Const((int)valueAsLong), Const((int)(valueAsLong >> 32)));
        }

        public static Operand PackDouble2x32(this EmitterContext context, Operand a, Operand b)
        {
            return context.Add(Instruction.PackDouble2x32, Local(), a, b);
        }

        public static Operand PackHalf2x16(this EmitterContext context, Operand a, Operand b)
        {
            return context.Add(Instruction.PackHalf2x16, Local(), a, b);
        }

        public static void Return(this EmitterContext context)
        {
            context.Add(Instruction.Return);
        }

        public static void Return(this EmitterContext context, Operand returnValue)
        {
            context.Add(Instruction.Return, null, returnValue);
        }

        public static Operand ShiftLeft(this EmitterContext context, Operand a, Operand b)
        {
            return context.Add(Instruction.ShiftLeft, Local(), a, b);
        }

        public static Operand ShiftRightS32(this EmitterContext context, Operand a, Operand b)
        {
            return context.Add(Instruction.ShiftRightS32, Local(), a, b);
        }

        public static Operand ShiftRightU32(this EmitterContext context, Operand a, Operand b)
        {
            return context.Add(Instruction.ShiftRightU32, Local(), a, b);
        }

        public static Operand Shuffle(this EmitterContext context, Operand a, Operand b)
        {
            return context.Add(Instruction.Shuffle, Local(), a, b);
        }

        public static (Operand, Operand) Shuffle(this EmitterContext context, Operand a, Operand b, Operand c)
        {
            return context.Add(Instruction.Shuffle, (Local(), Local()), a, b, c);
        }

        public static Operand ShuffleDown(this EmitterContext context, Operand a, Operand b)
        {
            return context.Add(Instruction.ShuffleDown, Local(), a, b);
        }

        public static (Operand, Operand) ShuffleDown(this EmitterContext context, Operand a, Operand b, Operand c)
        {
            return context.Add(Instruction.ShuffleDown, (Local(), Local()), a, b, c);
        }

        public static Operand ShuffleUp(this EmitterContext context, Operand a, Operand b)
        {
            return context.Add(Instruction.ShuffleUp, Local(), a, b);
        }

        public static (Operand, Operand) ShuffleUp(this EmitterContext context, Operand a, Operand b, Operand c)
        {
            return context.Add(Instruction.ShuffleUp, (Local(), Local()), a, b, c);
        }

        public static Operand ShuffleXor(this EmitterContext context, Operand a, Operand b)
        {
            return context.Add(Instruction.ShuffleXor, Local(), a, b);
        }

        public static (Operand, Operand) ShuffleXor(this EmitterContext context, Operand a, Operand b, Operand c)
        {
            return context.Add(Instruction.ShuffleXor, (Local(), Local()), a, b, c);
        }

        public static Operand Store(this EmitterContext context, StorageKind storageKind, Operand e0, Operand e1, Operand value)
        {
            return context.Add(Instruction.Store, storageKind, null, e0, e1, value);
        }

        public static Operand Store(this EmitterContext context, StorageKind storageKind, int binding, Operand value)
        {
            return context.Add(Instruction.Store, storageKind, null, Const(binding), value);
        }

        public static Operand Store(this EmitterContext context, StorageKind storageKind, int binding, Operand e0, Operand value)
        {
            return context.Add(Instruction.Store, storageKind, null, Const(binding), e0, value);
        }

        public static Operand Store(this EmitterContext context, StorageKind storageKind, int binding, Operand e0, Operand e1, Operand value)
        {
            return context.Add(Instruction.Store, storageKind, null, Const(binding), e0, e1, value);
        }

        public static Operand Store(
            this EmitterContext context,
            StorageKind storageKind,
            IoVariable ioVariable,
            Operand invocationId,
            Operand value)
        {
            return invocationId != null
                ? context.Add(Instruction.Store, storageKind, null, Const((int)ioVariable), invocationId, value)
                : context.Add(Instruction.Store, storageKind, null, Const((int)ioVariable), value);
        }

        public static Operand Store(
            this EmitterContext context,
            StorageKind storageKind,
            IoVariable ioVariable,
            Operand invocationId,
            Operand elemIndex,
            Operand value)
        {
            return invocationId != null
                ? context.Add(Instruction.Store, storageKind, null, Const((int)ioVariable), invocationId, elemIndex, value)
                : context.Add(Instruction.Store, storageKind, null, Const((int)ioVariable), elemIndex, value);
        }

        public static Operand Store(
            this EmitterContext context,
            StorageKind storageKind,
            IoVariable ioVariable,
            Operand invocationId,
            Operand arrayIndex,
            Operand elemIndex,
            Operand value)
        {
            return invocationId != null
                ? context.Add(Instruction.Store, storageKind, null, Const((int)ioVariable), invocationId, arrayIndex, elemIndex, value)
                : context.Add(Instruction.Store, storageKind, null, Const((int)ioVariable), arrayIndex, elemIndex, value);
        }

        public static void TextureSample(
            this EmitterContext context,
            SamplerType type,
            TextureFlags flags,
            SetBindingPair setAndBinding,
            int compMask,
            Operand[] dests,
            Operand[] sources)
        {
            context.Add(new TextureOperation(
                Instruction.TextureSample,
                type,
                TextureFormat.Unknown,
                flags,
                setAndBinding.SetIndex,
                setAndBinding.Binding,
                compMask,
                dests,
                sources));
        }

        public static Operand TextureQuerySamples(
            this EmitterContext context,
            SamplerType type,
            TextureFlags flags,
            SetBindingPair setAndBinding,
            Operand[] sources)
        {
            Operand dest = Local();

            context.Add(new TextureOperation(
                Instruction.TextureQuerySamples,
                type,
                TextureFormat.Unknown,
                flags,
                setAndBinding.SetIndex,
                setAndBinding.Binding,
                0,
                [dest],
                sources));

            return dest;
        }

        public static Operand TextureQuerySize(
            this EmitterContext context,
            SamplerType type,
            TextureFlags flags,
            SetBindingPair setAndBinding,
            int compIndex,
            Operand[] sources)
        {
            Operand dest = Local();

            context.Add(new TextureOperation(
                Instruction.TextureQuerySize,
                type,
                TextureFormat.Unknown,
                flags,
                setAndBinding.SetIndex,
                setAndBinding.Binding,
                compIndex,
                [dest],
                sources));

            return dest;
        }

        public static Operand UnpackDouble2x32High(this EmitterContext context, Operand a)
        {
            return UnpackDouble2x32(context, a, 1);
        }

        public static Operand UnpackDouble2x32Low(this EmitterContext context, Operand a)
        {
            return UnpackDouble2x32(context, a, 0);
        }

        private static Operand UnpackDouble2x32(this EmitterContext context, Operand a, int index)
        {
            Operand dest = Local();

            context.Add(new Operation(Instruction.UnpackDouble2x32, index, dest, a));

            return dest;
        }

        public static Operand UnpackHalf2x16High(this EmitterContext context, Operand a)
        {
            return UnpackHalf2x16(context, a, 1);
        }

        public static Operand UnpackHalf2x16Low(this EmitterContext context, Operand a)
        {
            return UnpackHalf2x16(context, a, 0);
        }

        private static Operand UnpackHalf2x16(this EmitterContext context, Operand a, int index)
        {
            Operand dest = Local();

            context.Add(new Operation(Instruction.UnpackHalf2x16, index, dest, a));

            return dest;
        }

        public static Operand VoteAll(this EmitterContext context, Operand a)
        {
            return context.Add(Instruction.VoteAll, Local(), a);
        }

        public static Operand VoteAllEqual(this EmitterContext context, Operand a)
        {
            return context.Add(Instruction.VoteAllEqual, Local(), a);
        }

        public static Operand VoteAny(this EmitterContext context, Operand a)
        {
            return context.Add(Instruction.VoteAny, Local(), a);
        }
    }
}
