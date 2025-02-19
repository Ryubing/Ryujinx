using ARMeilleure.CodeGen.Linking;
using ARMeilleure.CodeGen.Optimizations;
using ARMeilleure.CodeGen.RegisterAllocators;
using ARMeilleure.CodeGen.Unwinding;
using ARMeilleure.Common;
using ARMeilleure.Diagnostics;
using ARMeilleure.IntermediateRepresentation;
using ARMeilleure.Translation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using static ARMeilleure.IntermediateRepresentation.Operand;
using static ARMeilleure.IntermediateRepresentation.Operand.Factory;

namespace ARMeilleure.CodeGen.Arm64
{
    static class CodeGenerator
    {
        private const int DWordScale = 3;

        private const int RegistersCount = 32;

        private const int FpRegister = 29;
        private const int LrRegister = 30;
        private const int SpRegister = 31;
        private const int ZrRegister = 31;

        private enum AccessSize
        {
            Byte,
            Hword,
            Auto,
        }

        private static readonly Action<CodeGenContext, Operation>[] _instTable;

        static CodeGenerator()
        {
            _instTable = new Action<CodeGenContext, Operation>[EnumUtils.GetCount(typeof(Instruction))];

#pragma warning disable IDE0055 // Disable formatting
            Add(Instruction.Add,                     GenerateAdd);
            Add(Instruction.BitwiseAnd,              GenerateBitwiseAnd);
            Add(Instruction.BitwiseExclusiveOr,      GenerateBitwiseExclusiveOr);
            Add(Instruction.BitwiseNot,              GenerateBitwiseNot);
            Add(Instruction.BitwiseOr,               GenerateBitwiseOr);
            Add(Instruction.BranchIf,                GenerateBranchIf);
            Add(Instruction.ByteSwap,                GenerateByteSwap);
            Add(Instruction.Call,                    GenerateCall);
            // Add(Instruction.Clobber,                 GenerateClobber);
            Add(Instruction.Compare,                 GenerateCompare);
            Add(Instruction.CompareAndSwap,          GenerateCompareAndSwap);
            Add(Instruction.CompareAndSwap16,        GenerateCompareAndSwap16);
            Add(Instruction.CompareAndSwap8,         GenerateCompareAndSwap8);
            Add(Instruction.ConditionalSelect,       GenerateConditionalSelect);
            Add(Instruction.ConvertI64ToI32,         GenerateConvertI64ToI32);
            Add(Instruction.ConvertToFP,             GenerateConvertToFP);
            Add(Instruction.ConvertToFPUI,           GenerateConvertToFPUI);
            Add(Instruction.Copy,                    GenerateCopy);
            Add(Instruction.CountLeadingZeros,       GenerateCountLeadingZeros);
            Add(Instruction.Divide,                  GenerateDivide);
            Add(Instruction.DivideUI,                GenerateDivideUI);
            Add(Instruction.Fill,                    GenerateFill);
            Add(Instruction.Load,                    GenerateLoad);
            Add(Instruction.Load16,                  GenerateLoad16);
            Add(Instruction.Load8,                   GenerateLoad8);
            Add(Instruction.MemoryBarrier,           GenerateMemoryBarrier);
            Add(Instruction.Multiply,                GenerateMultiply);
            Add(Instruction.Multiply64HighSI,        GenerateMultiply64HighSI);
            Add(Instruction.Multiply64HighUI,        GenerateMultiply64HighUI);
            Add(Instruction.Negate,                  GenerateNegate);
            Add(Instruction.Return,                  GenerateReturn);
            Add(Instruction.RotateRight,             GenerateRotateRight);
            Add(Instruction.ShiftLeft,               GenerateShiftLeft);
            Add(Instruction.ShiftRightSI,            GenerateShiftRightSI);
            Add(Instruction.ShiftRightUI,            GenerateShiftRightUI);
            Add(Instruction.SignExtend16,            GenerateSignExtend16);
            Add(Instruction.SignExtend32,            GenerateSignExtend32);
            Add(Instruction.SignExtend8,             GenerateSignExtend8);
            Add(Instruction.Spill,                   GenerateSpill);
            Add(Instruction.SpillArg,                GenerateSpillArg);
            Add(Instruction.StackAlloc,              GenerateStackAlloc);
            Add(Instruction.Store,                   GenerateStore);
            Add(Instruction.Store16,                 GenerateStore16);
            Add(Instruction.Store8,                  GenerateStore8);
            Add(Instruction.Subtract,                GenerateSubtract);
            Add(Instruction.Tailcall,                GenerateTailcall);
            Add(Instruction.VectorCreateScalar,      GenerateVectorCreateScalar);
            Add(Instruction.VectorExtract,           GenerateVectorExtract);
            Add(Instruction.VectorExtract16,         GenerateVectorExtract16);
            Add(Instruction.VectorExtract8,          GenerateVectorExtract8);
            Add(Instruction.VectorInsert,            GenerateVectorInsert);
            Add(Instruction.VectorInsert16,          GenerateVectorInsert16);
            Add(Instruction.VectorInsert8,           GenerateVectorInsert8);
            Add(Instruction.VectorOne,               GenerateVectorOne);
            Add(Instruction.VectorZero,              GenerateVectorZero);
            Add(Instruction.VectorZeroUpper64,       GenerateVectorZeroUpper64);
            Add(Instruction.VectorZeroUpper96,       GenerateVectorZeroUpper96);
            Add(Instruction.ZeroExtend16,            GenerateZeroExtend16);
            Add(Instruction.ZeroExtend32,            GenerateZeroExtend32);
            Add(Instruction.ZeroExtend8,             GenerateZeroExtend8);
#pragma warning restore IDE0055

            static void Add(Instruction inst, Action<CodeGenContext, Operation> func)
            {
                _instTable[(int)inst] = func;
            }
        }

        public static CompiledFunction Generate(CompilerContext cctx)
        {
            ControlFlowGraph cfg = cctx.Cfg;

            Logger.StartPass(PassName.Optimization);

            if (cctx.Options.HasFlag(CompilerOptions.Optimize))
            {
                if (cctx.Options.HasFlag(CompilerOptions.SsaForm))
                {
                    Optimizer.RunPass(cfg);
                }

                BlockPlacement.RunPass(cfg);
            }

            Arm64Optimizer.RunPass(cfg);

            Logger.EndPass(PassName.Optimization, cfg);

            Logger.StartPass(PassName.PreAllocation);

            StackAllocator stackAlloc = new();

            PreAllocator.RunPass(cctx, out int maxCallArgs);

            Logger.EndPass(PassName.PreAllocation, cfg);

            Logger.StartPass(PassName.RegisterAllocation);

            if (cctx.Options.HasFlag(CompilerOptions.SsaForm))
            {
                Ssa.Deconstruct(cfg);
            }

            IRegisterAllocator regAlloc;

            if (cctx.Options.HasFlag(CompilerOptions.Lsra))
            {
                regAlloc = new LinearScanAllocator();
            }
            else
            {
                regAlloc = new HybridAllocator();
            }

            RegisterMasks regMasks = new(
                CallingConvention.GetIntAvailableRegisters(),
                CallingConvention.GetVecAvailableRegisters(),
                CallingConvention.GetIntCallerSavedRegisters(),
                CallingConvention.GetVecCallerSavedRegisters(),
                CallingConvention.GetIntCalleeSavedRegisters(),
                CallingConvention.GetVecCalleeSavedRegisters(),
                RegistersCount);

            AllocationResult allocResult = regAlloc.RunPass(cfg, stackAlloc, regMasks);

            Logger.EndPass(PassName.RegisterAllocation, cfg);

            Logger.StartPass(PassName.CodeGeneration);

            bool relocatable = (cctx.Options & CompilerOptions.Relocatable) != 0;

            CodeGenContext context = new(allocResult, maxCallArgs, relocatable);

            UnwindInfo unwindInfo = WritePrologue(context);

            for (BasicBlock block = cfg.Blocks.First; block != null; block = block.ListNext)
            {
                context.EnterBlock(block);

                for (Operation node = block.Operations.First; node != default;)
                {
                    node = GenerateOperation(context, node);
                }

                if (block.SuccessorsCount == 0)
                {
                    // The only blocks which can have 0 successors are exit blocks.
                    Operation last = block.Operations.Last;

                    Debug.Assert(last.Instruction == Instruction.Tailcall ||
                                 last.Instruction == Instruction.Return);
                }
                else
                {
                    BasicBlock succ = block.GetSuccessor(0);

                    if (succ != block.ListNext)
                    {
                        context.JumpTo(succ);
                    }
                }
            }

            (byte[] code, RelocInfo relocInfo) = context.GetCode();

            Logger.EndPass(PassName.CodeGeneration);

            return new CompiledFunction(code, unwindInfo, relocInfo);
        }

        private static Operation GenerateOperation(CodeGenContext context, Operation operation)
        {
            if (operation.Instruction == Instruction.Extended)
            {
                CodeGeneratorIntrinsic.GenerateOperation(context, operation);
            }
            else
            {
                if (IsLoadOrStore(operation) &&
                    operation.ListNext != default &&
                    operation.ListNext.Instruction == operation.Instruction &&
                    TryPairMemoryOp(context, operation, operation.ListNext))
                {
                    // Skip next operation if we managed to pair them.
                    return operation.ListNext.ListNext;
                }

                Action<CodeGenContext, Operation> func = _instTable[(int)operation.Instruction];

                if (func != null)
                {
                    func(context, operation);
                }
                else
                {
                    throw new ArgumentException($"Invalid instruction \"{operation.Instruction}\".");
                }
            }

            return operation.ListNext;
        }

        private static void GenerateAdd(CodeGenContext context, Operation operation)
        {
            Operand dest = operation.Destination;
            Operand src1 = operation.GetSource(0);
            Operand src2 = operation.GetSource(1);

            // ValidateBinOp(dest, src1, src2);

            if (dest.Type.IsInteger())
            {
                context.Assembler.Add(dest, src1, src2);
            }
            else
            {
                context.Assembler.FaddScalar(dest, src1, src2);
            }
        }

        private static void GenerateBitwiseAnd(CodeGenContext context, Operation operation)
        {
            Operand dest = operation.Destination;
            Operand src1 = operation.GetSource(0);
            Operand src2 = operation.GetSource(1);

            ValidateBinOp(dest, src1, src2);

            Debug.Assert(dest.Type.IsInteger());

            context.Assembler.And(dest, src1, src2);
        }

        private static void GenerateBitwiseExclusiveOr(CodeGenContext context, Operation operation)
        {
            Operand dest = operation.Destination;
            Operand src1 = operation.GetSource(0);
            Operand src2 = operation.GetSource(1);

            ValidateBinOp(dest, src1, src2);

            if (dest.Type.IsInteger())
            {
                context.Assembler.Eor(dest, src1, src2);
            }
            else
            {
                context.Assembler.EorVector(dest, src1, src2);
            }
        }

        private static void GenerateBitwiseNot(CodeGenContext context, Operation operation)
        {
            Operand dest = operation.Destination;
            Operand source = operation.GetSource(0);

            ValidateUnOp(dest, source);

            Debug.Assert(dest.Type.IsInteger());

            context.Assembler.Mvn(dest, source);
        }

        private static void GenerateBitwiseOr(CodeGenContext context, Operation operation)
        {
            Operand dest = operation.Destination;
            Operand src1 = operation.GetSource(0);
            Operand src2 = operation.GetSource(1);

            ValidateBinOp(dest, src1, src2);

            Debug.Assert(dest.Type.IsInteger());

            context.Assembler.Orr(dest, src1, src2);
        }

        private static void GenerateBranchIf(CodeGenContext context, Operation operation)
        {
            Operand comp = operation.GetSource(2);

            Debug.Assert(comp.Kind == OperandKind.Constant);

            ArmCondition cond = ((Comparison)comp.AsInt32()).ToArmCondition();

            GenerateCompareCommon(context, operation);

            context.JumpTo(cond, context.CurrBlock.GetSuccessor(1));
        }

        private static void GenerateByteSwap(CodeGenContext context, Operation operation)
        {
            Operand dest = operation.Destination;
            Operand source = operation.GetSource(0);

            ValidateUnOp(dest, source);

            Debug.Assert(dest.Type.IsInteger());

            context.Assembler.Rev(dest, source);
        }

        private static void GenerateCall(CodeGenContext context, Operation operation)
        {
            context.Assembler.Blr(operation.GetSource(0));
        }

        private static void GenerateCompare(CodeGenContext context, Operation operation)
        {
            Operand dest = operation.Destination;
            Operand comp = operation.GetSource(2);

            Debug.Assert(dest.Type == OperandType.I32);
            Debug.Assert(comp.Kind == OperandKind.Constant);

            ArmCondition cond = ((Comparison)comp.AsInt32()).ToArmCondition();

            GenerateCompareCommon(context, operation);

            context.Assembler.Cset(dest, cond);
        }

        private static void GenerateCompareAndSwap(CodeGenContext context, Operation operation)
        {
            if (operation.SourcesCount == 5) // CompareAndSwap128 has 5 sources, compared to CompareAndSwap64/32's 3.
            {
                Operand actualLow = operation.GetDestination(0);
                Operand actualHigh = operation.GetDestination(1);
                Operand temp0 = operation.GetDestination(2);
                Operand temp1 = operation.GetDestination(3);
                Operand address = operation.GetSource(0);
                Operand expectedLow = operation.GetSource(1);
                Operand expectedHigh = operation.GetSource(2);
                Operand desiredLow = operation.GetSource(3);
                Operand desiredHigh = operation.GetSource(4);

                GenerateAtomicDcas(
                    context,
                    address,
                    expectedLow,
                    expectedHigh,
                    desiredLow,
                    desiredHigh,
                    actualLow,
                    actualHigh,
                    temp0,
                    temp1);
            }
            else
            {
                Operand actual = operation.GetDestination(0);
                Operand result = operation.GetDestination(1);
                Operand address = operation.GetSource(0);
                Operand expected = operation.GetSource(1);
                Operand desired = operation.GetSource(2);

                GenerateAtomicCas(context, address, expected, desired, actual, result, AccessSize.Auto);
            }
        }

        private static void GenerateCompareAndSwap16(CodeGenContext context, Operation operation)
        {
            Operand actual = operation.GetDestination(0);
            Operand result = operation.GetDestination(1);
            Operand address = operation.GetSource(0);
            Operand expected = operation.GetSource(1);
            Operand desired = operation.GetSource(2);

            GenerateAtomicCas(context, address, expected, desired, actual, result, AccessSize.Hword);
        }

        private static void GenerateCompareAndSwap8(CodeGenContext context, Operation operation)
        {
            Operand actual = operation.GetDestination(0);
            Operand result = operation.GetDestination(1);
            Operand address = operation.GetSource(0);
            Operand expected = operation.GetSource(1);
            Operand desired = operation.GetSource(2);

            GenerateAtomicCas(context, address, expected, desired, actual, result, AccessSize.Byte);
        }

        private static void GenerateCompareCommon(CodeGenContext context, Operation operation)
        {
            Operand src1 = operation.GetSource(0);
            Operand src2 = operation.GetSource(1);

            EnsureSameType(src1, src2);

            Debug.Assert(src1.Type.IsInteger());

            context.Assembler.Cmp(src1, src2);
        }

        private static void GenerateConditionalSelect(CodeGenContext context, Operation operation)
        {
            Operand dest = operation.Destination;
            Operand src1 = operation.GetSource(0);
            Operand src2 = operation.GetSource(1);
            Operand src3 = operation.GetSource(2);

            EnsureSameType(dest, src2, src3);

            Debug.Assert(dest.Type.IsInteger());
            Debug.Assert(src1.Type == OperandType.I32);

            context.Assembler.Cmp(src1, Const(src1.Type, 0));
            context.Assembler.Csel(dest, src2, src3, ArmCondition.Ne);
        }

        private static void GenerateConvertI64ToI32(CodeGenContext context, Operation operation)
        {
            Operand dest = operation.Destination;
            Operand source = operation.GetSource(0);

            Debug.Assert(dest.Type == OperandType.I32 && source.Type == OperandType.I64);

            context.Assembler.Mov(dest, Register(source, OperandType.I32));
        }

        private static void GenerateConvertToFP(CodeGenContext context, Operation operation)
        {
            Operand dest = operation.Destination;
            Operand source = operation.GetSource(0);

            Debug.Assert(dest.Type == OperandType.FP32 || dest.Type == OperandType.FP64);
            Debug.Assert(dest.Type != source.Type);
            Debug.Assert(source.Type != OperandType.V128);

            if (source.Type.IsInteger())
            {
                context.Assembler.ScvtfScalar(dest, source);
            }
            else
            {
                context.Assembler.FcvtScalar(dest, source);
            }
        }

        private static void GenerateConvertToFPUI(CodeGenContext context, Operation operation)
        {
            Operand dest = operation.Destination;
            Operand source = operation.GetSource(0);

            Debug.Assert(dest.Type == OperandType.FP32 || dest.Type == OperandType.FP64);
            Debug.Assert(dest.Type != source.Type);
            Debug.Assert(source.Type.IsInteger());

            context.Assembler.UcvtfScalar(dest, source);
        }

        private static void GenerateCopy(CodeGenContext context, Operation operation)
        {
            Operand dest = operation.Destination;
            Operand source = operation.GetSource(0);

            EnsureSameType(dest, source);

            Debug.Assert(dest.Type.IsInteger() || source.Kind != OperandKind.Constant);

            // Moves to the same register are useless.
            if (dest.Kind == source.Kind && dest.Value == source.Value)
            {
                return;
            }

            if (dest.Kind == OperandKind.Register && source.Kind == OperandKind.Constant)
            {
                if (source.Relocatable)
                {
                    context.ReserveRelocatableConstant(dest, source.Symbol, source.Value);
                }
                else
                {
                    GenerateConstantCopy(context, dest, source.Value);
                }
            }
            else
            {
                context.Assembler.Mov(dest, source);
            }
        }

        private static void GenerateCountLeadingZeros(CodeGenContext context, Operation operation)
        {
            Operand dest = operation.Destination;
            Operand source = operation.GetSource(0);

            EnsureSameType(dest, source);

            Debug.Assert(dest.Type.IsInteger());

            context.Assembler.Clz(dest, source);
        }

        private static void GenerateDivide(CodeGenContext context, Operation operation)
        {
            Operand dest = operation.Destination;
            Operand dividend = operation.GetSource(0);
            Operand divisor = operation.GetSource(1);

            ValidateBinOp(dest, dividend, divisor);

            if (dest.Type.IsInteger())
            {
                context.Assembler.Sdiv(dest, dividend, divisor);
            }
            else
            {
                context.Assembler.FdivScalar(dest, dividend, divisor);
            }
        }

        private static void GenerateDivideUI(CodeGenContext context, Operation operation)
        {
            Operand dest = operation.Destination;
            Operand dividend = operation.GetSource(0);
            Operand divisor = operation.GetSource(1);

            ValidateBinOp(dest, dividend, divisor);

            context.Assembler.Udiv(dest, dividend, divisor);
        }

        private static void GenerateLoad(CodeGenContext context, Operation operation)
        {
            Operand value = operation.Destination;
            Operand address = operation.GetSource(0);

            context.Assembler.Ldr(value, address);
        }

        private static void GenerateLoad16(CodeGenContext context, Operation operation)
        {
            Operand value = operation.Destination;
            Operand address = operation.GetSource(0);

            Debug.Assert(value.Type.IsInteger());

            context.Assembler.LdrhRiUn(value, address, 0);
        }

        private static void GenerateLoad8(CodeGenContext context, Operation operation)
        {
            Operand value = operation.Destination;
            Operand address = operation.GetSource(0);

            Debug.Assert(value.Type.IsInteger());

            context.Assembler.LdrbRiUn(value, address, 0);
        }

        private static void GenerateMemoryBarrier(CodeGenContext context, Operation operation)
        {
            context.Assembler.Dmb(0xf);
        }

        private static void GenerateMultiply(CodeGenContext context, Operation operation)
        {
            Operand dest = operation.Destination;
            Operand src1 = operation.GetSource(0);
            Operand src2 = operation.GetSource(1);

            EnsureSameType(dest, src1, src2);

            if (dest.Type.IsInteger())
            {
                context.Assembler.Mul(dest, src1, src2);
            }
            else
            {
                context.Assembler.FmulScalar(dest, src1, src2);
            }
        }

        private static void GenerateMultiply64HighSI(CodeGenContext context, Operation operation)
        {
            Operand dest = operation.Destination;
            Operand src1 = operation.GetSource(0);
            Operand src2 = operation.GetSource(1);

            EnsureSameType(dest, src1, src2);

            Debug.Assert(dest.Type == OperandType.I64);

            context.Assembler.Smulh(dest, src1, src2);
        }

        private static void GenerateMultiply64HighUI(CodeGenContext context, Operation operation)
        {
            Operand dest = operation.Destination;
            Operand src1 = operation.GetSource(0);
            Operand src2 = operation.GetSource(1);

            EnsureSameType(dest, src1, src2);

            Debug.Assert(dest.Type == OperandType.I64);

            context.Assembler.Umulh(dest, src1, src2);
        }

        private static void GenerateNegate(CodeGenContext context, Operation operation)
        {
            Operand dest = operation.Destination;
            Operand source = operation.GetSource(0);

            ValidateUnOp(dest, source);

            if (dest.Type.IsInteger())
            {
                context.Assembler.Neg(dest, source);
            }
            else
            {
                context.Assembler.FnegScalar(dest, source);
            }
        }

        private static void GenerateLoad(CodeGenContext context, Operand value, Operand address, int offset)
        {
            if (CodeGenCommon.ConstFitsOnUImm12(offset, value.Type))
            {
                context.Assembler.LdrRiUn(value, address, offset);
            }
            else if (CodeGenCommon.ConstFitsOnSImm9(offset))
            {
                context.Assembler.Ldur(value, address, offset);
            }
            else
            {
                Operand tempAddress = Register(CodeGenCommon.ReservedRegister);
                GenerateConstantCopy(context, tempAddress, (ulong)offset);
                context.Assembler.Add(tempAddress, address, tempAddress, ArmExtensionType.Uxtx); // Address might be SP and must be the first input.
                context.Assembler.LdrRiUn(value, tempAddress, 0);
            }
        }

        private static void GenerateReturn(CodeGenContext context, Operation operation)
        {
            WriteEpilogue(context);

            context.Assembler.Ret(Register(LrRegister));
        }

        private static void GenerateRotateRight(CodeGenContext context, Operation operation)
        {
            Operand dest = operation.Destination;
            Operand src1 = operation.GetSource(0);
            Operand src2 = operation.GetSource(1);

            ValidateShift(dest, src1, src2);

            context.Assembler.Ror(dest, src1, src2);
        }

        private static void GenerateShiftLeft(CodeGenContext context, Operation operation)
        {
            Operand dest = operation.Destination;
            Operand src1 = operation.GetSource(0);
            Operand src2 = operation.GetSource(1);

            ValidateShift(dest, src1, src2);

            context.Assembler.Lsl(dest, src1, src2);
        }

        private static void GenerateShiftRightSI(CodeGenContext context, Operation operation)
        {
            Operand dest = operation.Destination;
            Operand src1 = operation.GetSource(0);
            Operand src2 = operation.GetSource(1);

            ValidateShift(dest, src1, src2);

            context.Assembler.Asr(dest, src1, src2);
        }

        private static void GenerateShiftRightUI(CodeGenContext context, Operation operation)
        {
            Operand dest = operation.Destination;
            Operand src1 = operation.GetSource(0);
            Operand src2 = operation.GetSource(1);

            ValidateShift(dest, src1, src2);

            context.Assembler.Lsr(dest, src1, src2);
        }

        private static void GenerateSignExtend16(CodeGenContext context, Operation operation)
        {
            Operand dest = operation.Destination;
            Operand source = operation.GetSource(0);

            Debug.Assert(dest.Type.IsInteger() && source.Type.IsInteger());

            context.Assembler.Sxth(dest, source);
        }

        private static void GenerateSignExtend32(CodeGenContext context, Operation operation)
        {
            Operand dest = operation.Destination;
            Operand source = operation.GetSource(0);

            Debug.Assert(dest.Type.IsInteger() && source.Type.IsInteger());

            context.Assembler.Sxtw(dest, source);
        }

        private static void GenerateSignExtend8(CodeGenContext context, Operation operation)
        {
            Operand dest = operation.Destination;
            Operand source = operation.GetSource(0);

            Debug.Assert(dest.Type.IsInteger() && source.Type.IsInteger());

            context.Assembler.Sxtb(dest, source);
        }

        private static void GenerateFill(CodeGenContext context, Operation operation)
        {
            Operand dest = operation.Destination;
            Operand offset = operation.GetSource(0);

            Debug.Assert(offset.Kind == OperandKind.Constant);

            int offs = offset.AsInt32() + context.CallArgsRegionSize + context.FpLrSaveRegionSize;

            GenerateLoad(context, dest, Register(SpRegister), offs);
        }

        private static void GenerateStore(CodeGenContext context, Operand value, Operand address, int offset)
        {
            if (CodeGenCommon.ConstFitsOnUImm12(offset, value.Type))
            {
                context.Assembler.StrRiUn(value, address, offset);
            }
            else if (CodeGenCommon.ConstFitsOnSImm9(offset))
            {
                context.Assembler.Stur(value, address, offset);
            }
            else
            {
                Operand tempAddress = Register(CodeGenCommon.ReservedRegister);
                GenerateConstantCopy(context, tempAddress, (ulong)offset);
                context.Assembler.Add(tempAddress, address, tempAddress, ArmExtensionType.Uxtx); // Address might be SP and must be the first input.
                context.Assembler.StrRiUn(value, tempAddress, 0);
            }
        }

        private static void GenerateSpill(CodeGenContext context, Operation operation)
        {
            GenerateSpill(context, operation, context.CallArgsRegionSize + context.FpLrSaveRegionSize);
        }

        private static void GenerateSpillArg(CodeGenContext context, Operation operation)
        {
            GenerateSpill(context, operation, 0);
        }

        private static void GenerateStackAlloc(CodeGenContext context, Operation operation)
        {
            Operand dest = operation.Destination;
            Operand offset = operation.GetSource(0);

            Debug.Assert(offset.Kind == OperandKind.Constant);

            int offs = offset.AsInt32() + context.CallArgsRegionSize + context.FpLrSaveRegionSize;

            context.Assembler.Add(dest, Register(SpRegister), Const(dest.Type, offs));
        }

        private static void GenerateStore(CodeGenContext context, Operation operation)
        {
            Operand value = operation.GetSource(1);
            Operand address = operation.GetSource(0);

            context.Assembler.Str(value, address);
        }

        private static void GenerateStore16(CodeGenContext context, Operation operation)
        {
            Operand value = operation.GetSource(1);
            Operand address = operation.GetSource(0);

            Debug.Assert(value.Type.IsInteger());

            context.Assembler.StrhRiUn(value, address, 0);
        }

        private static void GenerateStore8(CodeGenContext context, Operation operation)
        {
            Operand value = operation.GetSource(1);
            Operand address = operation.GetSource(0);

            Debug.Assert(value.Type.IsInteger());

            context.Assembler.StrbRiUn(value, address, 0);
        }

        private static void GenerateSpill(CodeGenContext context, Operation operation, int baseOffset)
        {
            Operand offset = operation.GetSource(0);
            Operand source = operation.GetSource(1);

            Debug.Assert(offset.Kind == OperandKind.Constant);

            int offs = offset.AsInt32() + baseOffset;

            GenerateStore(context, source, Register(SpRegister), offs);
        }

        private static void GenerateSubtract(CodeGenContext context, Operation operation)
        {
            Operand dest = operation.Destination;
            Operand src1 = operation.GetSource(0);
            Operand src2 = operation.GetSource(1);

            // ValidateBinOp(dest, src1, src2);

            if (dest.Type.IsInteger())
            {
                context.Assembler.Sub(dest, src1, src2);
            }
            else
            {
                context.Assembler.FsubScalar(dest, src1, src2);
            }
        }

        private static void GenerateTailcall(CodeGenContext context, Operation operation)
        {
            WriteEpilogue(context);

            context.Assembler.Br(operation.GetSource(0));
        }

        private static void GenerateVectorCreateScalar(CodeGenContext context, Operation operation)
        {
            Operand dest = operation.Destination;
            Operand source = operation.GetSource(0);

            if (dest != default)
            {
                Debug.Assert(!dest.Type.IsInteger() && source.Type.IsInteger());

                OperandType destType = source.Type == OperandType.I64 ? OperandType.FP64 : OperandType.FP32;

                context.Assembler.Fmov(Register(dest, destType), source, topHalf: false);
            }
        }

        private static void GenerateVectorExtract(CodeGenContext context, Operation operation)
        {
            Operand dest = operation.Destination;  // Value
            Operand src1 = operation.GetSource(0); // Vector
            Operand src2 = operation.GetSource(1); // Index

            Debug.Assert(src1.Type == OperandType.V128);
            Debug.Assert(src2.Kind == OperandKind.Constant);

            byte index = src2.AsByte();

            Debug.Assert(index < OperandType.V128.GetSizeInBytes() / dest.Type.GetSizeInBytes());

            if (dest.Type.IsInteger())
            {
                context.Assembler.Umov(dest, src1, index, dest.Type == OperandType.I64 ? 3 : 2);
            }
            else
            {
                context.Assembler.DupScalar(dest, src1, index, dest.Type == OperandType.FP64 ? 3 : 2);
            }
        }

        private static void GenerateVectorExtract16(CodeGenContext context, Operation operation)
        {
            Operand dest = operation.Destination;  // Value
            Operand src1 = operation.GetSource(0); // Vector
            Operand src2 = operation.GetSource(1); // Index

            Debug.Assert(src1.Type == OperandType.V128);
            Debug.Assert(src2.Kind == OperandKind.Constant);

            byte index = src2.AsByte();

            Debug.Assert(index < 8);

            context.Assembler.Umov(dest, src1, index, 1);
        }

        private static void GenerateVectorExtract8(CodeGenContext context, Operation operation)
        {
            Operand dest = operation.Destination;  // Value
            Operand src1 = operation.GetSource(0); // Vector
            Operand src2 = operation.GetSource(1); // Index

            Debug.Assert(src1.Type == OperandType.V128);
            Debug.Assert(src2.Kind == OperandKind.Constant);

            byte index = src2.AsByte();

            Debug.Assert(index < 16);

            context.Assembler.Umov(dest, src1, index, 0);
        }

        private static void GenerateVectorInsert(CodeGenContext context, Operation operation)
        {
            Operand dest = operation.Destination;
            Operand src1 = operation.GetSource(0); // Vector
            Operand src2 = operation.GetSource(1); // Value
            Operand src3 = operation.GetSource(2); // Index

            EnsureSameReg(dest, src1);

            Debug.Assert(src1.Type == OperandType.V128);
            Debug.Assert(src3.Kind == OperandKind.Constant);

            byte index = src3.AsByte();

            if (src2.Type.IsInteger())
            {
                context.Assembler.Ins(dest, src2, index, src2.Type == OperandType.I64 ? 3 : 2);
            }
            else
            {
                context.Assembler.Ins(dest, src2, 0, index, src2.Type == OperandType.FP64 ? 3 : 2);
            }
        }

        private static void GenerateVectorInsert16(CodeGenContext context, Operation operation)
        {
            Operand dest = operation.Destination;
            Operand src1 = operation.GetSource(0); // Vector
            Operand src2 = operation.GetSource(1); // Value
            Operand src3 = operation.GetSource(2); // Index

            EnsureSameReg(dest, src1);

            Debug.Assert(src1.Type == OperandType.V128);
            Debug.Assert(src3.Kind == OperandKind.Constant);

            byte index = src3.AsByte();

            context.Assembler.Ins(dest, src2, index, 1);
        }

        private static void GenerateVectorInsert8(CodeGenContext context, Operation operation)
        {
            Operand dest = operation.Destination;
            Operand src1 = operation.GetSource(0); // Vector
            Operand src2 = operation.GetSource(1); // Value
            Operand src3 = operation.GetSource(2); // Index

            EnsureSameReg(dest, src1);

            Debug.Assert(src1.Type == OperandType.V128);
            Debug.Assert(src3.Kind == OperandKind.Constant);

            byte index = src3.AsByte();

            context.Assembler.Ins(dest, src2, index, 0);
        }

        private static void GenerateVectorOne(CodeGenContext context, Operation operation)
        {
            Operand dest = operation.Destination;

            Debug.Assert(!dest.Type.IsInteger());

            context.Assembler.CmeqVector(dest, dest, dest, 2);
        }

        private static void GenerateVectorZero(CodeGenContext context, Operation operation)
        {
            Operand dest = operation.Destination;

            Debug.Assert(!dest.Type.IsInteger());

            context.Assembler.EorVector(dest, dest, dest);
        }

        private static void GenerateVectorZeroUpper64(CodeGenContext context, Operation operation)
        {
            Operand dest = operation.Destination;
            Operand source = operation.GetSource(0);

            Debug.Assert(dest.Type == OperandType.V128 && source.Type == OperandType.V128);

            context.Assembler.Fmov(Register(dest, OperandType.FP64), Register(source, OperandType.FP64));
        }

        private static void GenerateVectorZeroUpper96(CodeGenContext context, Operation operation)
        {
            Operand dest = operation.Destination;
            Operand source = operation.GetSource(0);

            Debug.Assert(dest.Type == OperandType.V128 && source.Type == OperandType.V128);

            context.Assembler.Fmov(Register(dest, OperandType.FP32), Register(source, OperandType.FP32));
        }

        private static void GenerateZeroExtend16(CodeGenContext context, Operation operation)
        {
            Operand dest = operation.Destination;
            Operand source = operation.GetSource(0);

            Debug.Assert(dest.Type.IsInteger() && source.Type.IsInteger());

            context.Assembler.Uxth(dest, source);
        }

        private static void GenerateZeroExtend32(CodeGenContext context, Operation operation)
        {
            Operand dest = operation.Destination;
            Operand source = operation.GetSource(0);

            Debug.Assert(dest.Type.IsInteger() && source.Type.IsInteger());

            // We can eliminate the move if source is already 32-bit and the registers are the same.
            if (dest.Value == source.Value && source.Type == OperandType.I32)
            {
                return;
            }

            context.Assembler.Mov(Register(dest.GetRegister().Index, OperandType.I32), source);
        }

        private static void GenerateZeroExtend8(CodeGenContext context, Operation operation)
        {
            Operand dest = operation.Destination;
            Operand source = operation.GetSource(0);

            Debug.Assert(dest.Type.IsInteger() && source.Type.IsInteger());

            context.Assembler.Uxtb(dest, source);
        }

        private static UnwindInfo WritePrologue(CodeGenContext context)
        {
            List<UnwindPushEntry> pushEntries = [];

            Operand rsp = Register(SpRegister);

            int intMask = CallingConvention.GetIntCalleeSavedRegisters() & context.AllocResult.IntUsedRegisters;
            int vecMask = CallingConvention.GetFpCalleeSavedRegisters() & context.AllocResult.VecUsedRegisters;

            int intCalleeSavedRegsCount = BitOperations.PopCount((uint)intMask);
            int vecCalleeSavedRegsCount = BitOperations.PopCount((uint)vecMask);

            int calleeSaveRegionSize = Align16(intCalleeSavedRegsCount * 8 + vecCalleeSavedRegsCount * 8);

            int offset = 0;

            WritePrologueCalleeSavesPreIndexed(context, pushEntries, ref intMask, ref offset, calleeSaveRegionSize, OperandType.I64);
            WritePrologueCalleeSavesPreIndexed(context, pushEntries, ref vecMask, ref offset, calleeSaveRegionSize, OperandType.FP64);

            int localSize = Align16(context.AllocResult.SpillRegionSize + context.FpLrSaveRegionSize);
            int outArgsSize = context.CallArgsRegionSize;

            if (CodeGenCommon.ConstFitsOnSImm7(localSize, DWordScale))
            {
                if (context.HasCall)
                {
                    context.Assembler.StpRiPre(Register(FpRegister), Register(LrRegister), rsp, -localSize);
                    context.Assembler.MovSp(Register(FpRegister), rsp);
                }

                if (outArgsSize != 0)
                {
                    context.Assembler.Sub(rsp, rsp, Const(OperandType.I64, outArgsSize));
                }
            }
            else
            {
                int frameSize = localSize + outArgsSize;
                if (frameSize != 0)
                {
                    if (CodeGenCommon.ConstFitsOnUImm12(frameSize))
                    {
                        context.Assembler.Sub(rsp, rsp, Const(OperandType.I64, frameSize));
                    }
                    else
                    {
                        Operand tempSize = Register(CodeGenCommon.ReservedRegister);
                        GenerateConstantCopy(context, tempSize, (ulong)frameSize);
                        context.Assembler.Sub(rsp, rsp, tempSize, ArmExtensionType.Uxtx);
                    }
                }

                context.Assembler.StpRiUn(Register(FpRegister), Register(LrRegister), rsp, outArgsSize);

                if (outArgsSize != 0)
                {
                    context.Assembler.Add(Register(FpRegister), Register(SpRegister), Const(OperandType.I64, outArgsSize));
                }
                else
                {
                    context.Assembler.MovSp(Register(FpRegister), Register(SpRegister));
                }
            }

            return new UnwindInfo(pushEntries.ToArray(), context.StreamOffset);
        }

        private static void WritePrologueCalleeSavesPreIndexed(
            CodeGenContext context,
            List<UnwindPushEntry> pushEntries,
            ref int mask,
            ref int offset,
            int calleeSaveRegionSize,
            OperandType type)
        {
            if ((BitOperations.PopCount((uint)mask) & 1) != 0)
            {
                int reg = BitOperations.TrailingZeroCount(mask);

                pushEntries.Add(new UnwindPushEntry(UnwindPseudoOp.PushReg, context.StreamOffset, regIndex: reg));

                mask &= ~(1 << reg);

                if (offset != 0)
                {
                    context.Assembler.StrRiUn(Register(reg, type), Register(SpRegister), offset);
                }
                else
                {
                    context.Assembler.StrRiPre(Register(reg, type), Register(SpRegister), -calleeSaveRegionSize);
                }

                offset += type.GetSizeInBytes();
            }

            while (mask != 0)
            {
                int reg = BitOperations.TrailingZeroCount(mask);

                pushEntries.Add(new UnwindPushEntry(UnwindPseudoOp.PushReg, context.StreamOffset, regIndex: reg));

                mask &= ~(1 << reg);

                int reg2 = BitOperations.TrailingZeroCount(mask);

                pushEntries.Add(new UnwindPushEntry(UnwindPseudoOp.PushReg, context.StreamOffset, regIndex: reg2));

                mask &= ~(1 << reg2);

                if (offset != 0)
                {
                    context.Assembler.StpRiUn(Register(reg, type), Register(reg2, type), Register(SpRegister), offset);
                }
                else
                {
                    context.Assembler.StpRiPre(Register(reg, type), Register(reg2, type), Register(SpRegister), -calleeSaveRegionSize);
                }

                offset += type.GetSizeInBytes() * 2;
            }
        }

        private static void WriteEpilogue(CodeGenContext context)
        {
            Operand rsp = Register(SpRegister);

            int localSize = Align16(context.AllocResult.SpillRegionSize + context.FpLrSaveRegionSize);
            int outArgsSize = context.CallArgsRegionSize;

            if (CodeGenCommon.ConstFitsOnSImm7(localSize, DWordScale))
            {
                if (outArgsSize != 0)
                {
                    context.Assembler.Add(rsp, rsp, Const(OperandType.I64, outArgsSize));
                }

                if (context.HasCall)
                {
                    context.Assembler.LdpRiPost(Register(FpRegister), Register(LrRegister), rsp, localSize);
                }
            }
            else
            {
                if (context.HasCall)
                {
                    context.Assembler.LdpRiUn(Register(FpRegister), Register(LrRegister), rsp, outArgsSize);
                }

                int frameSize = localSize + outArgsSize;
                if (frameSize != 0)
                {
                    if (CodeGenCommon.ConstFitsOnUImm12(frameSize))
                    {
                        context.Assembler.Add(rsp, rsp, Const(OperandType.I64, frameSize));
                    }
                    else
                    {
                        Operand tempSize = Register(CodeGenCommon.ReservedRegister);
                        GenerateConstantCopy(context, tempSize, (ulong)frameSize);
                        context.Assembler.Add(rsp, rsp, tempSize, ArmExtensionType.Uxtx);
                    }
                }
            }

            int intMask = CallingConvention.GetIntCalleeSavedRegisters() & context.AllocResult.IntUsedRegisters;
            int vecMask = CallingConvention.GetFpCalleeSavedRegisters() & context.AllocResult.VecUsedRegisters;

            int intCalleeSavedRegsCount = BitOperations.PopCount((uint)intMask);
            int vecCalleeSavedRegsCount = BitOperations.PopCount((uint)vecMask);

            int offset = intCalleeSavedRegsCount * 8 + vecCalleeSavedRegsCount * 8;
            int calleeSaveRegionSize = Align16(offset);

            WriteEpilogueCalleeSavesPostIndexed(context, ref vecMask, ref offset, calleeSaveRegionSize, OperandType.FP64);
            WriteEpilogueCalleeSavesPostIndexed(context, ref intMask, ref offset, calleeSaveRegionSize, OperandType.I64);
        }

        private static void WriteEpilogueCalleeSavesPostIndexed(
            CodeGenContext context,
            ref int mask,
            ref int offset,
            int calleeSaveRegionSize,
            OperandType type)
        {
            while (mask != 0)
            {
                int reg = BitUtils.HighestBitSet(mask);

                mask &= ~(1 << reg);

                if (mask != 0)
                {
                    int reg2 = BitUtils.HighestBitSet(mask);

                    mask &= ~(1 << reg2);

                    offset -= type.GetSizeInBytes() * 2;

                    if (offset != 0)
                    {
                        context.Assembler.LdpRiUn(Register(reg2, type), Register(reg, type), Register(SpRegister), offset);
                    }
                    else
                    {
                        context.Assembler.LdpRiPost(Register(reg2, type), Register(reg, type), Register(SpRegister), calleeSaveRegionSize);
                    }
                }
                else
                {
                    offset -= type.GetSizeInBytes();

                    if (offset != 0)
                    {
                        context.Assembler.LdrRiUn(Register(reg, type), Register(SpRegister), offset);
                    }
                    else
                    {
                        context.Assembler.LdrRiPost(Register(reg, type), Register(SpRegister), calleeSaveRegionSize);
                    }
                }
            }
        }

        private static void GenerateConstantCopy(CodeGenContext context, Operand dest, ulong value)
        {
            if (value == 0)
            {
                context.Assembler.Mov(dest, Register(ZrRegister, dest.Type));
            }
            else if (CodeGenCommon.TryEncodeBitMask(dest.Type, value, out _, out _, out _))
            {
                context.Assembler.Orr(dest, Register(ZrRegister, dest.Type), Const(dest.Type, (long)value));
            }
            else
            {
                int hw = 0;
                bool first = true;

                while (value != 0)
                {
                    int valueLow = (ushort)value;
                    if (valueLow != 0)
                    {
                        if (first)
                        {
                            context.Assembler.Movz(dest, valueLow, hw);
                            first = false;
                        }
                        else
                        {
                            context.Assembler.Movk(dest, valueLow, hw);
                        }
                    }

                    hw++;
                    value >>= 16;
                }
            }
        }

        private static void GenerateAtomicCas(
            CodeGenContext context,
            Operand address,
            Operand expected,
            Operand desired,
            Operand actual,
            Operand result,
            AccessSize accessSize)
        {
            int startOffset = context.StreamOffset;

            switch (accessSize)
            {
                case AccessSize.Byte:
                    context.Assembler.Ldaxrb(actual, address);
                    break;
                case AccessSize.Hword:
                    context.Assembler.Ldaxrh(actual, address);
                    break;
                default:
                    context.Assembler.Ldaxr(actual, address);
                    break;
            }

            context.Assembler.Cmp(actual, expected);

            context.JumpToNear(ArmCondition.Ne);

            switch (accessSize)
            {
                case AccessSize.Byte:
                    context.Assembler.Stlxrb(desired, address, result);
                    break;
                case AccessSize.Hword:
                    context.Assembler.Stlxrh(desired, address, result);
                    break;
                default:
                    context.Assembler.Stlxr(desired, address, result);
                    break;
            }

            context.Assembler.Cbnz(result, startOffset - context.StreamOffset); // Retry if store failed.

            context.JumpHere();

            context.Assembler.Clrex();
        }

        private static void GenerateAtomicDcas(
            CodeGenContext context,
            Operand address,
            Operand expectedLow,
            Operand expectedHigh,
            Operand desiredLow,
            Operand desiredHigh,
            Operand actualLow,
            Operand actualHigh,
            Operand temp0,
            Operand temp1)
        {
            int startOffset = context.StreamOffset;

            context.Assembler.Ldaxp(actualLow, actualHigh, address);
            context.Assembler.Eor(temp0, actualHigh, expectedHigh);
            context.Assembler.Eor(temp1, actualLow, expectedLow);
            context.Assembler.Orr(temp0, temp1, temp0);

            context.JumpToNearIfNotZero(temp0);

            Operand result = Register(temp0, OperandType.I32);

            context.Assembler.Stlxp(desiredLow, desiredHigh, address, result);
            context.Assembler.Cbnz(result, startOffset - context.StreamOffset); // Retry if store failed.

            context.JumpHere();

            context.Assembler.Clrex();
        }

        private static bool TryPairMemoryOp(CodeGenContext context, Operation currentOp, Operation nextOp)
        {
            if (!TryGetMemOpBaseAndOffset(currentOp, out Operand op1Base, out int op1Offset))
            {
                return false;
            }

            if (!TryGetMemOpBaseAndOffset(nextOp, out Operand op2Base, out int op2Offset))
            {
                return false;
            }

            if (op1Base != op2Base)
            {
                return false;
            }

            OperandType valueType = GetMemOpValueType(currentOp);

            if (valueType != GetMemOpValueType(nextOp) || op1Offset + valueType.GetSizeInBytes() != op2Offset)
            {
                return false;
            }

            if (!CodeGenCommon.ConstFitsOnSImm7(op1Offset, valueType.GetSizeInBytesLog2()))
            {
                return false;
            }

            if (currentOp.Instruction == Instruction.Load)
            {
                context.Assembler.LdpRiUn(currentOp.Destination, nextOp.Destination, op1Base, op1Offset);
            }
            else if (currentOp.Instruction == Instruction.Store)
            {
                context.Assembler.StpRiUn(currentOp.GetSource(1), nextOp.GetSource(1), op1Base, op1Offset);
            }
            else
            {
                return false;
            }

            return true;
        }

        private static bool IsLoadOrStore(Operation operation)
        {
            return operation.Instruction == Instruction.Load || operation.Instruction == Instruction.Store;
        }

        private static OperandType GetMemOpValueType(Operation operation)
        {
            if (operation.Destination != default)
            {
                return operation.Destination.Type;
            }

            return operation.GetSource(1).Type;
        }

        private static bool TryGetMemOpBaseAndOffset(Operation operation, out Operand baseAddress, out int offset)
        {
            baseAddress = default;
            offset = 0;
            Operand address = operation.GetSource(0);

            if (address.Kind != OperandKind.Memory)
            {
                return false;
            }

            MemoryOperand memOp = address.GetMemory();
            Operand baseOp = memOp.BaseAddress;

            if (baseOp == default)
            {
                baseOp = memOp.Index;

                if (baseOp == default || memOp.Scale != Multiplier.x1)
                {
                    return false;
                }
            }
            if (memOp.Index != default)
            {
                return false;
            }

            baseAddress = memOp.BaseAddress;
            offset = memOp.Displacement;

            return true;
        }

        private static Operand Register(Operand operand, OperandType type = OperandType.I64)
        {
            return Register(operand.GetRegister().Index, type);
        }

        private static Operand Register(int register, OperandType type = OperandType.I64)
        {
            return Factory.Register(register, RegisterType.Integer, type);
        }

        private static int Align16(int value)
        {
            return (value + 0xf) & ~0xf;
        }

        [Conditional("DEBUG")]
        private static void ValidateUnOp(Operand dest, Operand source)
        {
            // Destination and source aren't forced to be equals
            // EnsureSameReg (dest, source);
            EnsureSameType(dest, source);
        }

        [Conditional("DEBUG")]
        private static void ValidateBinOp(Operand dest, Operand src1, Operand src2)
        {
            // Destination and source aren't forced to be equals
            // EnsureSameReg (dest, src1);
            EnsureSameType(dest, src1, src2);
        }

        [Conditional("DEBUG")]
        private static void ValidateShift(Operand dest, Operand src1, Operand src2)
        {
            // Destination and source aren't forced to be equals
            // EnsureSameReg (dest, src1);
            EnsureSameType(dest, src1);

            Debug.Assert(dest.Type.IsInteger() && src2.Type == OperandType.I32);
        }

        private static void EnsureSameReg(Operand op1, Operand op2)
        {
            Debug.Assert(op1.Kind == OperandKind.Register || op1.Kind == OperandKind.Memory);
            Debug.Assert(op1.Kind == op2.Kind);
            Debug.Assert(op1.Value == op2.Value);
        }

        private static void EnsureSameType(Operand op1, Operand op2)
        {
            Debug.Assert(op1.Type == op2.Type);
        }

        private static void EnsureSameType(Operand op1, Operand op2, Operand op3)
        {
            Debug.Assert(op1.Type == op2.Type);
            Debug.Assert(op1.Type == op3.Type);
        }

#pragma warning disable IDE0051 // Remove unused private member
        private static void EnsureSameType(Operand op1, Operand op2, Operand op3, Operand op4)
        {
            Debug.Assert(op1.Type == op2.Type);
            Debug.Assert(op1.Type == op3.Type);
            Debug.Assert(op1.Type == op4.Type);
        }
#pragma warning restore IDE0051
    }
}
