using Ryujinx.Graphics.Shader.IntermediateRepresentation;
using System;
using System.Collections.Generic;
using static Ryujinx.Graphics.Shader.IntermediateRepresentation.OperandHelper;

namespace Ryujinx.Graphics.Shader.Translation
{
    class HelperFunctionManager
    {
        private readonly List<Function> _functionList;
        private readonly Dictionary<int, int> _functionIds;
        private readonly ShaderStage _stage;

        public HelperFunctionManager(List<Function> functionList, ShaderStage stage)
        {
            _functionList = functionList;
            _functionIds = [];
            _stage = stage;
        }

        public int AddFunction(Function function)
        {
            int functionId = _functionList.Count;
            _functionList.Add(function);

            return functionId;
        }

        public int GetOrCreateFunctionId(HelperFunctionName functionName)
        {
            if (_functionIds.TryGetValue((int)functionName, out int functionId))
            {
                return functionId;
            }

            Function function = GenerateFunction(functionName);
            functionId = AddFunction(function);
            _functionIds.Add((int)functionName, functionId);

            return functionId;
        }

        public int GetOrCreateFunctionId(HelperFunctionName functionName, int id)
        {
            int key = (int)functionName | (id << 16);

            if (_functionIds.TryGetValue(key, out int functionId))
            {
                return functionId;
            }

            Function function = GenerateFunction(functionName, id);
            functionId = AddFunction(function);
            _functionIds.Add(key, functionId);

            return functionId;
        }

        public int GetOrCreateShuffleFunctionId(HelperFunctionName functionName, int subgroupSize)
        {
            if (_functionIds.TryGetValue((int)functionName, out int functionId))
            {
                return functionId;
            }

            Function function = GenerateShuffleFunction(functionName, subgroupSize);
            functionId = AddFunction(function);
            _functionIds.Add((int)functionName, functionId);

            return functionId;
        }

        private Function GenerateFunction(HelperFunctionName functionName)
        {
            return functionName switch
            {
                HelperFunctionName.ConvertDoubleToFloat => GenerateConvertDoubleToFloatFunction(),
                HelperFunctionName.ConvertFloatToDouble => GenerateConvertFloatToDoubleFunction(),
                HelperFunctionName.TexelFetchScale => GenerateTexelFetchScaleFunction(),
                HelperFunctionName.TextureSizeUnscale => GenerateTextureSizeUnscaleFunction(),
                _ => throw new ArgumentException($"Invalid function name {functionName}"),
            };
        }

        private static Function GenerateConvertDoubleToFloatFunction()
        {
            EmitterContext context = new();

            Operand valueLow = Argument(0);
            Operand valueHigh = Argument(1);

            Operand mantissaLow = context.BitwiseAnd(valueLow, Const(((1 << 22) - 1)));
            Operand mantissa = context.ShiftRightU32(valueLow, Const(22));

            mantissa = context.BitwiseOr(mantissa, context.ShiftLeft(context.BitwiseAnd(valueHigh, Const(0xfffff)), Const(10)));
            mantissa = context.BitwiseOr(mantissa, context.ConditionalSelect(mantissaLow, Const(1), Const(0)));

            Operand exp = context.BitwiseAnd(context.ShiftRightU32(valueHigh, Const(20)), Const(0x7ff));
            Operand sign = context.ShiftRightS32(valueHigh, Const(31));

            Operand resultSign = context.ShiftLeft(sign, Const(31));

            Operand notZero = context.BitwiseOr(mantissa, exp);

            Operand lblNotZero = Label();

            context.BranchIfTrue(lblNotZero, notZero);

            context.Return(resultSign);

            context.MarkLabel(lblNotZero);

            Operand notNaNOrInf = context.ICompareNotEqual(exp, Const(0x7ff));

            mantissa = context.BitwiseOr(mantissa, Const(0x40000000));
            exp = context.ISubtract(exp, Const(0x381));

            // Note: Overflow cases are not handled here and might produce incorrect results.

            Operand roundBits = context.BitwiseAnd(mantissa, Const(0x7f));
            Operand roundBitsXor64 = context.BitwiseExclusiveOr(roundBits, Const(0x40));
            mantissa = context.ShiftRightU32(context.IAdd(mantissa, Const(0x40)), Const(7));
            mantissa = context.BitwiseAnd(mantissa, context.ConditionalSelect(roundBitsXor64, Const(~0), Const(~1)));

            exp = context.ConditionalSelect(mantissa, exp, Const(0));
            exp = context.ConditionalSelect(notNaNOrInf, exp, Const(0xff));

            Operand result = context.IAdd(context.IAdd(mantissa, context.ShiftLeft(exp, Const(23))), resultSign);

            context.Return(result);

            return new Function(ControlFlowGraph.Create(context.GetOperations()).Blocks, "ConvertDoubleToFloat", true, 2, 0);
        }

        private static Function GenerateConvertFloatToDoubleFunction()
        {
            EmitterContext context = new();

            Operand value = Argument(0);

            Operand mantissa = context.BitwiseAnd(value, Const(0x7fffff));
            Operand exp = context.BitwiseAnd(context.ShiftRightU32(value, Const(23)), Const(0xff));
            Operand sign = context.ShiftRightS32(value, Const(31));

            Operand notNaNOrInf = context.ICompareNotEqual(exp, Const(0xff));
            Operand expNotZero = context.ICompareNotEqual(exp, Const(0));
            Operand notDenorm = context.BitwiseOr(expNotZero, context.ICompareEqual(mantissa, Const(0)));

            exp = context.IAdd(exp, Const(0x380));

            Operand shiftDist = context.ISubtract(Const(32), context.FindMSBU32(mantissa));
            Operand normExp = context.ISubtract(context.ISubtract(Const(1), shiftDist), Const(1));
            Operand normMant = context.ShiftLeft(mantissa, shiftDist);

            exp = context.ConditionalSelect(notNaNOrInf, exp, Const(0x7ff));
            exp = context.ConditionalSelect(notDenorm, exp, normExp);
            mantissa = context.ConditionalSelect(expNotZero, mantissa, normMant);

            Operand resultLow = context.ShiftLeft(mantissa, Const(29));
            Operand resultHigh = context.ShiftRightU32(mantissa, Const(3));

            resultHigh = context.IAdd(resultHigh, context.ShiftLeft(exp, Const(20)));
            resultHigh = context.IAdd(resultHigh, context.ShiftLeft(sign, Const(31)));

            context.Copy(Argument(1), resultLow);
            context.Copy(Argument(2), resultHigh);
            context.Return();

            return new Function(ControlFlowGraph.Create(context.GetOperations()).Blocks, "ConvertFloatToDouble", false, 1, 2);
        }

        private static Function GenerateFunction(HelperFunctionName functionName, int id)
        {
            return functionName switch
            {
                HelperFunctionName.SharedAtomicMaxS32 => GenerateSharedAtomicSigned(id, isMin: false),
                HelperFunctionName.SharedAtomicMinS32 => GenerateSharedAtomicSigned(id, isMin: true),
                HelperFunctionName.SharedStore8 => GenerateSharedStore8(id),
                HelperFunctionName.SharedStore16 => GenerateSharedStore16(id),
                _ => throw new ArgumentException($"Invalid function name {functionName}"),
            };
        }

        private static Function GenerateSharedAtomicSigned(int id, bool isMin)
        {
            EmitterContext context = new();

            Operand wordOffset = Argument(0);
            Operand value = Argument(1);

            Operand result = GenerateSharedAtomicCasLoop(context, wordOffset, id, (memValue) =>
            {
                return isMin
                    ? context.IMinimumS32(memValue, value)
                    : context.IMaximumS32(memValue, value);
            });

            context.Return(result);

            return new Function(ControlFlowGraph.Create(context.GetOperations()).Blocks, $"SharedAtomic{(isMin ? "Min" : "Max")}_{id}", true, 2, 0);
        }

        private static Function GenerateSharedStore8(int id)
        {
            return GenerateSharedStore(id, 8);
        }

        private static Function GenerateSharedStore16(int id)
        {
            return GenerateSharedStore(id, 16);
        }

        private static Function GenerateSharedStore(int id, int bitSize)
        {
            EmitterContext context = new();

            Operand offset = Argument(0);
            Operand value = Argument(1);

            Operand wordOffset = context.ShiftRightU32(offset, Const(2));
            Operand bitOffset = GetBitOffset(context, offset);

            GenerateSharedAtomicCasLoop(context, wordOffset, id, (memValue) =>
            {
                return context.BitfieldInsert(memValue, value, bitOffset, Const(bitSize));
            });

            context.Return();

            return new Function(ControlFlowGraph.Create(context.GetOperations()).Blocks, $"SharedStore{bitSize}_{id}", false, 2, 0);
        }

        private static Function GenerateShuffleFunction(HelperFunctionName functionName, int subgroupSize)
        {
            return functionName switch
            {
                HelperFunctionName.Shuffle => GenerateShuffle(subgroupSize),
                HelperFunctionName.ShuffleDown => GenerateShuffleDown(subgroupSize),
                HelperFunctionName.ShuffleUp => GenerateShuffleUp(subgroupSize),
                HelperFunctionName.ShuffleXor => GenerateShuffleXor(subgroupSize),
                _ => throw new ArgumentException($"Invalid function name {functionName}"),
            };
        }

        private static Function GenerateShuffle(int subgroupSize)
        {
            EmitterContext context = new();

            Operand value = Argument(0);
            Operand index = Argument(1);
            Operand mask = Argument(2);

            Operand clamp = context.BitwiseAnd(mask, Const(0x1f));
            Operand segMask = context.BitwiseAnd(context.ShiftRightU32(mask, Const(8)), Const(0x1f));
            Operand minThreadId = context.BitwiseAnd(GenerateLoadSubgroupLaneId(context, subgroupSize), segMask);
            Operand maxThreadId = context.BitwiseOr(context.BitwiseAnd(clamp, context.BitwiseNot(segMask)), minThreadId);
            Operand srcThreadId = context.BitwiseOr(context.BitwiseAnd(index, context.BitwiseNot(segMask)), minThreadId);
            Operand valid = context.ICompareLessOrEqualUnsigned(srcThreadId, maxThreadId);

            context.Copy(Argument(3), valid);

            Operand result = context.Shuffle(value, GenerateSubgroupShuffleIndex(context, srcThreadId, subgroupSize));

            context.Return(context.ConditionalSelect(valid, result, value));

            return new Function(ControlFlowGraph.Create(context.GetOperations()).Blocks, "Shuffle", true, 3, 1);
        }

        private static Function GenerateShuffleDown(int subgroupSize)
        {
            EmitterContext context = new();

            Operand value = Argument(0);
            Operand index = Argument(1);
            Operand mask = Argument(2);

            Operand clamp = context.BitwiseAnd(mask, Const(0x1f));
            Operand segMask = context.BitwiseAnd(context.ShiftRightU32(mask, Const(8)), Const(0x1f));
            Operand laneId = GenerateLoadSubgroupLaneId(context, subgroupSize);
            Operand minThreadId = context.BitwiseAnd(laneId, segMask);
            Operand maxThreadId = context.BitwiseOr(context.BitwiseAnd(clamp, context.BitwiseNot(segMask)), minThreadId);
            Operand srcThreadId = context.IAdd(laneId, index);
            Operand valid = context.ICompareLessOrEqualUnsigned(srcThreadId, maxThreadId);

            context.Copy(Argument(3), valid);

            Operand result = context.Shuffle(value, GenerateSubgroupShuffleIndex(context, srcThreadId, subgroupSize));

            context.Return(context.ConditionalSelect(valid, result, value));

            return new Function(ControlFlowGraph.Create(context.GetOperations()).Blocks, "ShuffleDown", true, 3, 1);
        }

        private static Function GenerateShuffleUp(int subgroupSize)
        {
            EmitterContext context = new();

            Operand value = Argument(0);
            Operand index = Argument(1);
            Operand mask = Argument(2);

            Operand segMask = context.BitwiseAnd(context.ShiftRightU32(mask, Const(8)), Const(0x1f));
            Operand laneId = GenerateLoadSubgroupLaneId(context, subgroupSize);
            Operand minThreadId = context.BitwiseAnd(laneId, segMask);
            Operand srcThreadId = context.ISubtract(laneId, index);
            Operand valid = context.ICompareGreaterOrEqual(srcThreadId, minThreadId);

            context.Copy(Argument(3), valid);

            Operand result = context.Shuffle(value, GenerateSubgroupShuffleIndex(context, srcThreadId, subgroupSize));

            context.Return(context.ConditionalSelect(valid, result, value));

            return new Function(ControlFlowGraph.Create(context.GetOperations()).Blocks, "ShuffleUp", true, 3, 1);
        }

        private static Function GenerateShuffleXor(int subgroupSize)
        {
            EmitterContext context = new();

            Operand value = Argument(0);
            Operand index = Argument(1);
            Operand mask = Argument(2);

            Operand clamp = context.BitwiseAnd(mask, Const(0x1f));
            Operand segMask = context.BitwiseAnd(context.ShiftRightU32(mask, Const(8)), Const(0x1f));
            Operand laneId = GenerateLoadSubgroupLaneId(context, subgroupSize);
            Operand minThreadId = context.BitwiseAnd(laneId, segMask);
            Operand maxThreadId = context.BitwiseOr(context.BitwiseAnd(clamp, context.BitwiseNot(segMask)), minThreadId);
            Operand srcThreadId = context.BitwiseExclusiveOr(laneId, index);
            Operand valid = context.ICompareLessOrEqualUnsigned(srcThreadId, maxThreadId);

            context.Copy(Argument(3), valid);

            Operand result = context.Shuffle(value, GenerateSubgroupShuffleIndex(context, srcThreadId, subgroupSize));

            context.Return(context.ConditionalSelect(valid, result, value));

            return new Function(ControlFlowGraph.Create(context.GetOperations()).Blocks, "ShuffleXor", true, 3, 1);
        }

        private static Operand GenerateLoadSubgroupLaneId(EmitterContext context, int subgroupSize)
        {
            if (subgroupSize <= 32)
            {
                return context.Load(StorageKind.Input, IoVariable.SubgroupLaneId);
            }

            return context.BitwiseAnd(context.Load(StorageKind.Input, IoVariable.SubgroupLaneId), Const(0x1f));
        }

        private static Operand GenerateSubgroupShuffleIndex(EmitterContext context, Operand srcThreadId, int subgroupSize)
        {
            if (subgroupSize <= 32)
            {
                return srcThreadId;
            }

            return context.BitwiseOr(
                context.BitwiseAnd(context.Load(StorageKind.Input, IoVariable.SubgroupLaneId), Const(0x60)),
                srcThreadId);
        }

        private Function GenerateTexelFetchScaleFunction()
        {
            EmitterContext context = new();

            Operand input = Argument(0);
            Operand samplerIndex = Argument(1);
            Operand index = GetScaleIndex(context, samplerIndex);

            Operand scale = context.Load(StorageKind.ConstantBuffer, 0, Const((int)SupportBufferField.RenderScale), index);

            Operand scaleIsOne = context.FPCompareEqual(scale, ConstF(1f));
            Operand lblScaleNotOne = Label();

            context.BranchIfFalse(lblScaleNotOne, scaleIsOne);
            context.Return(input);
            context.MarkLabel(lblScaleNotOne);

            int inArgumentsCount;

            if (_stage == ShaderStage.Fragment)
            {
                Operand scaleIsLessThanZero = context.FPCompareLess(scale, ConstF(0f));
                Operand lblScaleGreaterOrEqualZero = Label();

                context.BranchIfFalse(lblScaleGreaterOrEqualZero, scaleIsLessThanZero);

                Operand negScale = context.FPNegate(scale);
                Operand inputScaled = context.FPMultiply(context.IConvertS32ToFP32(input), negScale);
                Operand fragCoordX = context.Load(StorageKind.Input, IoVariable.FragmentCoord, null, Const(0));
                Operand fragCoordY = context.Load(StorageKind.Input, IoVariable.FragmentCoord, null, Const(1));
                Operand fragCoord = context.ConditionalSelect(Argument(2), fragCoordY, fragCoordX);
                Operand inputBias = context.FPModulo(fragCoord, negScale);
                Operand inputWithBias = context.FPAdd(inputScaled, inputBias);

                context.Return(context.FP32ConvertToS32(inputWithBias));
                context.MarkLabel(lblScaleGreaterOrEqualZero);

                inArgumentsCount = 3;
            }
            else
            {
                inArgumentsCount = 2;
            }

            Operand inputScaled2 = context.FPMultiply(context.IConvertS32ToFP32(input), scale);

            context.Return(context.FP32ConvertToS32(inputScaled2));

            return new Function(ControlFlowGraph.Create(context.GetOperations()).Blocks, "TexelFetchScale", true, inArgumentsCount, 0);
        }

        private Function GenerateTextureSizeUnscaleFunction()
        {
            EmitterContext context = new();

            Operand input = Argument(0);
            Operand samplerIndex = Argument(1);
            Operand index = GetScaleIndex(context, samplerIndex);

            Operand scale = context.FPAbsolute(context.Load(StorageKind.ConstantBuffer, 0, Const((int)SupportBufferField.RenderScale), index));

            Operand scaleIsOne = context.FPCompareEqual(scale, ConstF(1f));
            Operand lblScaleNotOne = Label();

            context.BranchIfFalse(lblScaleNotOne, scaleIsOne);
            context.Return(input);
            context.MarkLabel(lblScaleNotOne);

            Operand inputUnscaled = context.FPDivide(context.IConvertS32ToFP32(input), scale);

            context.Return(context.FP32ConvertToS32(inputUnscaled));

            return new Function(ControlFlowGraph.Create(context.GetOperations()).Blocks, "TextureSizeUnscale", true, 2, 0);
        }

        private Operand GetScaleIndex(EmitterContext context, Operand index)
        {
            switch (_stage)
            {
                case ShaderStage.Vertex:
                    Operand fragScaleCount = context.Load(StorageKind.ConstantBuffer, 0, Const((int)SupportBufferField.FragmentRenderScaleCount));
                    return context.IAdd(Const(1), context.IAdd(index, fragScaleCount));
                default:
                    return context.IAdd(Const(1), index);
            }
        }

        public static Operand GetBitOffset(EmitterContext context, Operand offset)
        {
            return context.ShiftLeft(context.BitwiseAnd(offset, Const(3)), Const(3));
        }

        private static Operand GenerateSharedAtomicCasLoop(EmitterContext context, Operand wordOffset, int id, Func<Operand, Operand> opCallback)
        {
            Operand lblLoopHead = Label();

            context.MarkLabel(lblLoopHead);

            Operand oldValue = context.Load(StorageKind.SharedMemory, id, wordOffset);
            Operand newValue = opCallback(oldValue);

            Operand casResult = context.AtomicCompareAndSwap(StorageKind.SharedMemory, id, wordOffset, oldValue, newValue);

            Operand casFail = context.ICompareNotEqual(casResult, oldValue);

            context.BranchIfTrue(lblLoopHead, casFail);

            return oldValue;
        }

    }
}
