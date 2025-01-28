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
using static ARMeilleure.IntermediateRepresentation.Operand.Factory;

namespace ARMeilleure.CodeGen.X86
{
    static class CodeGenerator
    {
        private const int RegistersCount = 16;
        private const int PageSize = 0x1000;
        private const int StackGuardSize = 0x2000;

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
            Add(Instruction.Clobber,                 GenerateClobber);
            Add(Instruction.Compare,                 GenerateCompare);
            Add(Instruction.CompareAndSwap,          GenerateCompareAndSwap);
            Add(Instruction.CompareAndSwap16,        GenerateCompareAndSwap16);
            Add(Instruction.CompareAndSwap8,         GenerateCompareAndSwap8);
            Add(Instruction.ConditionalSelect,       GenerateConditionalSelect);
            Add(Instruction.ConvertI64ToI32,         GenerateConvertI64ToI32);
            Add(Instruction.ConvertToFP,             GenerateConvertToFP);
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

            X86Optimizer.RunPass(cfg);

            Logger.EndPass(PassName.Optimization, cfg);

            Logger.StartPass(PassName.PreAllocation);

            StackAllocator stackAlloc = new();

            PreAllocator.RunPass(cctx, stackAlloc, out int maxCallArgs);

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

            CodeGenContext context = new(allocResult, maxCallArgs, cfg.Blocks.Count, relocatable);

            UnwindInfo unwindInfo = WritePrologue(context);

            for (BasicBlock block = cfg.Blocks.First; block != null; block = block.ListNext)
            {
                context.EnterBlock(block);

                for (Operation node = block.Operations.First; node != default; node = node.ListNext)
                {
                    GenerateOperation(context, node);
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

            (byte[] code, RelocInfo relocInfo) = context.Assembler.GetCode();

            Logger.EndPass(PassName.CodeGeneration);

            return new CompiledFunction(code, unwindInfo, relocInfo);
        }

        private static void GenerateOperation(CodeGenContext context, Operation operation)
        {
            if (operation.Instruction == Instruction.Extended)
            {
                IntrinsicInfo info = IntrinsicTable.GetInfo(operation.Intrinsic);

                switch (info.Type)
                {
                    case IntrinsicType.Comis_:
                        {
                            Operand dest = operation.Destination;
                            Operand src1 = operation.GetSource(0);
                            Operand src2 = operation.GetSource(1);

                            switch (operation.Intrinsic)
                            {
                                case Intrinsic.X86Comisdeq:
                                    context.Assembler.Comisd(src1, src2);
                                    context.Assembler.Setcc(dest, X86Condition.Equal);
                                    break;

                                case Intrinsic.X86Comisdge:
                                    context.Assembler.Comisd(src1, src2);
                                    context.Assembler.Setcc(dest, X86Condition.AboveOrEqual);
                                    break;

                                case Intrinsic.X86Comisdlt:
                                    context.Assembler.Comisd(src1, src2);
                                    context.Assembler.Setcc(dest, X86Condition.Below);
                                    break;

                                case Intrinsic.X86Comisseq:
                                    context.Assembler.Comiss(src1, src2);
                                    context.Assembler.Setcc(dest, X86Condition.Equal);
                                    break;

                                case Intrinsic.X86Comissge:
                                    context.Assembler.Comiss(src1, src2);
                                    context.Assembler.Setcc(dest, X86Condition.AboveOrEqual);
                                    break;

                                case Intrinsic.X86Comisslt:
                                    context.Assembler.Comiss(src1, src2);
                                    context.Assembler.Setcc(dest, X86Condition.Below);
                                    break;
                            }

                            context.Assembler.Movzx8(dest, dest, OperandType.I32);

                            break;
                        }

                    case IntrinsicType.Mxcsr:
                        {
                            Operand offset = operation.GetSource(0);

                            Debug.Assert(offset.Kind == OperandKind.Constant);
                            Debug.Assert(offset.Type == OperandType.I32);

                            int offs = offset.AsInt32() + context.CallArgsRegionSize;

                            Operand rsp = Register(X86Register.Rsp);
                            Operand memOp = MemoryOp(OperandType.I32, rsp, default, Multiplier.x1, offs);

                            Debug.Assert(HardwareCapabilities.SupportsSse || HardwareCapabilities.SupportsVexEncoding);

                            if (operation.Intrinsic == Intrinsic.X86Ldmxcsr)
                            {
                                Operand bits = operation.GetSource(1);
                                Debug.Assert(bits.Type == OperandType.I32);

                                context.Assembler.Mov(memOp, bits, OperandType.I32);
                                context.Assembler.Ldmxcsr(memOp);
                            }
                            else if (operation.Intrinsic == Intrinsic.X86Stmxcsr)
                            {
                                Operand dest = operation.Destination;
                                Debug.Assert(dest.Type == OperandType.I32);

                                context.Assembler.Stmxcsr(memOp);
                                context.Assembler.Mov(dest, memOp, OperandType.I32);
                            }

                            break;
                        }

                    case IntrinsicType.PopCount:
                        {
                            Operand dest = operation.Destination;
                            Operand source = operation.GetSource(0);

                            EnsureSameType(dest, source);

                            Debug.Assert(dest.Type.IsInteger());

                            context.Assembler.Popcnt(dest, source, dest.Type);

                            break;
                        }

                    case IntrinsicType.Unary:
                        {
                            Operand dest = operation.Destination;
                            Operand source = operation.GetSource(0);

                            EnsureSameType(dest, source);

                            Debug.Assert(!dest.Type.IsInteger());

                            context.Assembler.WriteInstruction(info.Inst, dest, source);

                            break;
                        }

                    case IntrinsicType.UnaryToGpr:
                        {
                            Operand dest = operation.Destination;
                            Operand source = operation.GetSource(0);

                            Debug.Assert(dest.Type.IsInteger() && !source.Type.IsInteger());

                            if (operation.Intrinsic == Intrinsic.X86Cvtsi2si)
                            {
                                if (dest.Type == OperandType.I32)
                                {
                                    context.Assembler.Movd(dest, source); // int _mm_cvtsi128_si32(__m128i a)
                                }
                                else /* if (dest.Type == OperandType.I64) */
                                {
                                    context.Assembler.Movq(dest, source); // __int64 _mm_cvtsi128_si64(__m128i a)
                                }
                            }
                            else
                            {
                                context.Assembler.WriteInstruction(info.Inst, dest, source, dest.Type);
                            }

                            break;
                        }

                    case IntrinsicType.Binary:
                        {
                            Operand dest = operation.Destination;
                            Operand src1 = operation.GetSource(0);
                            Operand src2 = operation.GetSource(1);

                            EnsureSameType(dest, src1);

                            if (!HardwareCapabilities.SupportsVexEncoding)
                            {
                                EnsureSameReg(dest, src1);
                            }

                            Debug.Assert(!dest.Type.IsInteger());
                            Debug.Assert(!src2.Type.IsInteger() || src2.Kind == OperandKind.Constant);

                            context.Assembler.WriteInstruction(info.Inst, dest, src1, src2);

                            break;
                        }

                    case IntrinsicType.BinaryGpr:
                        {
                            Operand dest = operation.Destination;
                            Operand src1 = operation.GetSource(0);
                            Operand src2 = operation.GetSource(1);

                            EnsureSameType(dest, src1);

                            if (!HardwareCapabilities.SupportsVexEncoding)
                            {
                                EnsureSameReg(dest, src1);
                            }

                            Debug.Assert(!dest.Type.IsInteger() && src2.Type.IsInteger());

                            context.Assembler.WriteInstruction(info.Inst, dest, src1, src2, src2.Type);

                            break;
                        }

                    case IntrinsicType.Crc32:
                        {
                            Operand dest = operation.Destination;
                            Operand src1 = operation.GetSource(0);
                            Operand src2 = operation.GetSource(1);

                            EnsureSameReg(dest, src1);

                            Debug.Assert(dest.Type.IsInteger() && src1.Type.IsInteger() && src2.Type.IsInteger());

                            context.Assembler.WriteInstruction(info.Inst, dest, src2, dest.Type);

                            break;
                        }

                    case IntrinsicType.BinaryImm:
                        {
                            Operand dest = operation.Destination;
                            Operand src1 = operation.GetSource(0);
                            Operand src2 = operation.GetSource(1);

                            EnsureSameType(dest, src1);

                            if (!HardwareCapabilities.SupportsVexEncoding)
                            {
                                EnsureSameReg(dest, src1);
                            }

                            Debug.Assert(!dest.Type.IsInteger() && src2.Kind == OperandKind.Constant);

                            context.Assembler.WriteInstruction(info.Inst, dest, src1, src2.AsByte());

                            break;
                        }

                    case IntrinsicType.Ternary:
                        {
                            Operand dest = operation.Destination;
                            Operand src1 = operation.GetSource(0);
                            Operand src2 = operation.GetSource(1);
                            Operand src3 = operation.GetSource(2);

                            EnsureSameType(dest, src1, src2, src3);

                            Debug.Assert(!dest.Type.IsInteger());

                            if (info.Inst == X86Instruction.Blendvpd && HardwareCapabilities.SupportsVexEncoding)
                            {
                                context.Assembler.WriteInstruction(X86Instruction.Vblendvpd, dest, src1, src2, src3);
                            }
                            else if (info.Inst == X86Instruction.Blendvps && HardwareCapabilities.SupportsVexEncoding)
                            {
                                context.Assembler.WriteInstruction(X86Instruction.Vblendvps, dest, src1, src2, src3);
                            }
                            else if (info.Inst == X86Instruction.Pblendvb && HardwareCapabilities.SupportsVexEncoding)
                            {
                                context.Assembler.WriteInstruction(X86Instruction.Vpblendvb, dest, src1, src2, src3);
                            }
                            else
                            {
                                EnsureSameReg(dest, src1);

                                Debug.Assert(src3.GetRegister().Index == 0);

                                context.Assembler.WriteInstruction(info.Inst, dest, src1, src2);
                            }

                            break;
                        }

                    case IntrinsicType.TernaryImm:
                        {
                            Operand dest = operation.Destination;
                            Operand src1 = operation.GetSource(0);
                            Operand src2 = operation.GetSource(1);
                            Operand src3 = operation.GetSource(2);

                            EnsureSameType(dest, src1, src2);

                            if (!HardwareCapabilities.SupportsVexEncoding)
                            {
                                EnsureSameReg(dest, src1);
                            }

                            Debug.Assert(!dest.Type.IsInteger() && src3.Kind == OperandKind.Constant);

                            context.Assembler.WriteInstruction(info.Inst, dest, src1, src2, src3.AsByte());

                            break;
                        }

                    case IntrinsicType.Fma:
                        {
                            Operand dest = operation.Destination;
                            Operand src1 = operation.GetSource(0);
                            Operand src2 = operation.GetSource(1);
                            Operand src3 = operation.GetSource(2);

                            Debug.Assert(HardwareCapabilities.SupportsVexEncoding);

                            Debug.Assert(dest.Kind == OperandKind.Register && src1.Kind == OperandKind.Register && src2.Kind == OperandKind.Register);
                            Debug.Assert(src3.Kind == OperandKind.Register || src3.Kind == OperandKind.Memory);

                            EnsureSameType(dest, src1, src2, src3);
                            Debug.Assert(dest.Type == OperandType.V128);

                            Debug.Assert(dest.Value == src1.Value);

                            context.Assembler.WriteInstruction(info.Inst, dest, src2, src3);

                            break;
                        }
                }
            }
            else
            {
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
        }

        private static void GenerateAdd(CodeGenContext context, Operation operation)
        {
            Operand dest = operation.Destination;
            Operand src1 = operation.GetSource(0);
            Operand src2 = operation.GetSource(1);

            if (dest.Type.IsInteger())
            {
                // If Destination and Source 1 Operands are the same, perform a standard add as there are no benefits to using LEA.
                if (dest.Kind == src1.Kind && dest.Value == src1.Value)
                {
                    ValidateBinOp(dest, src1, src2);

                    context.Assembler.Add(dest, src2, dest.Type);
                }
                else
                {
                    EnsureSameType(dest, src1, src2);

                    int offset;
                    Operand index;

                    if (src2.Kind == OperandKind.Constant)
                    {
                        offset = src2.AsInt32();
                        index = default;
                    }
                    else
                    {
                        offset = 0;
                        index = src2;
                    }

                    Operand memOp = MemoryOp(dest.Type, src1, index, Multiplier.x1, offset);

                    context.Assembler.Lea(dest, memOp, dest.Type);
                }
            }
            else
            {
                ValidateBinOp(dest, src1, src2);

                if (dest.Type == OperandType.FP32)
                {
                    context.Assembler.Addss(dest, src1, src2);
                }
                else /* if (dest.Type == OperandType.FP64) */
                {
                    context.Assembler.Addsd(dest, src1, src2);
                }
            }
        }

        private static void GenerateBitwiseAnd(CodeGenContext context, Operation operation)
        {
            Operand dest = operation.Destination;
            Operand src1 = operation.GetSource(0);
            Operand src2 = operation.GetSource(1);

            ValidateBinOp(dest, src1, src2);

            Debug.Assert(dest.Type.IsInteger());

            // Note: GenerateCompareCommon makes the assumption that BitwiseAnd will emit only a single `and`
            // instruction.
            context.Assembler.And(dest, src2, dest.Type);
        }

        private static void GenerateBitwiseExclusiveOr(CodeGenContext context, Operation operation)
        {
            Operand dest = operation.Destination;
            Operand src1 = operation.GetSource(0);
            Operand src2 = operation.GetSource(1);

            ValidateBinOp(dest, src1, src2);

            if (dest.Type.IsInteger())
            {
                context.Assembler.Xor(dest, src2, dest.Type);
            }
            else
            {
                context.Assembler.Xorps(dest, src1, src2);
            }
        }

        private static void GenerateBitwiseNot(CodeGenContext context, Operation operation)
        {
            Operand dest = operation.Destination;
            Operand source = operation.GetSource(0);

            ValidateUnOp(dest, source);

            Debug.Assert(dest.Type.IsInteger());

            context.Assembler.Not(dest);
        }

        private static void GenerateBitwiseOr(CodeGenContext context, Operation operation)
        {
            Operand dest = operation.Destination;
            Operand src1 = operation.GetSource(0);
            Operand src2 = operation.GetSource(1);

            ValidateBinOp(dest, src1, src2);

            Debug.Assert(dest.Type.IsInteger());

            context.Assembler.Or(dest, src2, dest.Type);
        }

        private static void GenerateBranchIf(CodeGenContext context, Operation operation)
        {
            Operand comp = operation.GetSource(2);

            Debug.Assert(comp.Kind == OperandKind.Constant);

            X86Condition cond = ((Comparison)comp.AsInt32()).ToX86Condition();

            GenerateCompareCommon(context, operation);

            context.JumpTo(cond, context.CurrBlock.GetSuccessor(1));
        }

        private static void GenerateByteSwap(CodeGenContext context, Operation operation)
        {
            Operand dest = operation.Destination;
            Operand source = operation.GetSource(0);

            ValidateUnOp(dest, source);

            Debug.Assert(dest.Type.IsInteger());

            context.Assembler.Bswap(dest);
        }

        private static void GenerateCall(CodeGenContext context, Operation operation)
        {
            context.Assembler.Call(operation.GetSource(0));
        }

        private static void GenerateClobber(CodeGenContext context, Operation operation)
        {
            // This is only used to indicate that a register is clobbered to the
            // register allocator, we don't need to produce any code.
        }

        private static void GenerateCompare(CodeGenContext context, Operation operation)
        {
            Operand dest = operation.Destination;
            Operand comp = operation.GetSource(2);

            Debug.Assert(dest.Type == OperandType.I32);
            Debug.Assert(comp.Kind == OperandKind.Constant);

            X86Condition cond = ((Comparison)comp.AsInt32()).ToX86Condition();

            GenerateCompareCommon(context, operation);

            context.Assembler.Setcc(dest, cond);
            context.Assembler.Movzx8(dest, dest, OperandType.I32);
        }

        private static void GenerateCompareCommon(CodeGenContext context, Operation operation)
        {
            Operand src1 = operation.GetSource(0);
            Operand src2 = operation.GetSource(1);

            EnsureSameType(src1, src2);

            Debug.Assert(src1.Type.IsInteger());

            if (src2.Kind == OperandKind.Constant && src2.Value == 0)
            {
                if (MatchOperation(operation.ListPrevious, Instruction.BitwiseAnd, src1.Type, src1.GetRegister()))
                {
                    // Since the `test` and `and` instruction set the status flags in the same way, we can omit the
                    // `test r,r` instruction when it is immediately preceded by an `and r,*` instruction.
                    //
                    // For example:
                    //
                    //  and eax, 0x3
                    //  test eax, eax
                    //  jz .L0
                    //
                    // =>
                    //
                    //  and eax, 0x3
                    //  jz .L0
                }
                else
                {
                    context.Assembler.Test(src1, src1, src1.Type);
                }
            }
            else
            {
                context.Assembler.Cmp(src1, src2, src1.Type);
            }
        }

        private static void GenerateCompareAndSwap(CodeGenContext context, Operation operation)
        {
            Operand src1 = operation.GetSource(0);

            if (operation.SourcesCount == 5) // CompareAndSwap128 has 5 sources, compared to CompareAndSwap64/32's 3.
            {
                Operand memOp = MemoryOp(OperandType.I64, src1);

                context.Assembler.Cmpxchg16b(memOp);
            }
            else
            {
                Operand src2 = operation.GetSource(1);
                Operand src3 = operation.GetSource(2);

                EnsureSameType(src2, src3);

                Operand memOp = MemoryOp(src3.Type, src1);

                context.Assembler.Cmpxchg(memOp, src3);
            }
        }

        private static void GenerateCompareAndSwap16(CodeGenContext context, Operation operation)
        {
            Operand src1 = operation.GetSource(0);
            Operand src2 = operation.GetSource(1);
            Operand src3 = operation.GetSource(2);

            EnsureSameType(src2, src3);

            Operand memOp = MemoryOp(src3.Type, src1);

            context.Assembler.Cmpxchg16(memOp, src3);
        }

        private static void GenerateCompareAndSwap8(CodeGenContext context, Operation operation)
        {
            Operand src1 = operation.GetSource(0);
            Operand src2 = operation.GetSource(1);
            Operand src3 = operation.GetSource(2);

            EnsureSameType(src2, src3);

            Operand memOp = MemoryOp(src3.Type, src1);

            context.Assembler.Cmpxchg8(memOp, src3);
        }

        private static void GenerateConditionalSelect(CodeGenContext context, Operation operation)
        {
            Operand dest = operation.Destination;
            Operand src1 = operation.GetSource(0);
            Operand src2 = operation.GetSource(1);
            Operand src3 = operation.GetSource(2);

            EnsureSameReg(dest, src3);
            EnsureSameType(dest, src2, src3);

            Debug.Assert(dest.Type.IsInteger());
            Debug.Assert(src1.Type == OperandType.I32);

            context.Assembler.Test(src1, src1, src1.Type);
            context.Assembler.Cmovcc(dest, src2, dest.Type, X86Condition.NotEqual);
        }

        private static void GenerateConvertI64ToI32(CodeGenContext context, Operation operation)
        {
            Operand dest = operation.Destination;
            Operand source = operation.GetSource(0);

            Debug.Assert(dest.Type == OperandType.I32 && source.Type == OperandType.I64);

            context.Assembler.Mov(dest, source, OperandType.I32);
        }

        private static void GenerateConvertToFP(CodeGenContext context, Operation operation)
        {
            Operand dest = operation.Destination;
            Operand source = operation.GetSource(0);

            Debug.Assert(dest.Type == OperandType.FP32 || dest.Type == OperandType.FP64);

            if (dest.Type == OperandType.FP32)
            {
                Debug.Assert(source.Type.IsInteger() || source.Type == OperandType.FP64);

                if (source.Type.IsInteger())
                {
                    context.Assembler.Xorps(dest, dest, dest);
                    context.Assembler.Cvtsi2ss(dest, dest, source, source.Type);
                }
                else /* if (source.Type == OperandType.FP64) */
                {
                    context.Assembler.Cvtsd2ss(dest, dest, source);

                    GenerateZeroUpper96(context, dest, dest);
                }
            }
            else /* if (dest.Type == OperandType.FP64) */
            {
                Debug.Assert(source.Type.IsInteger() || source.Type == OperandType.FP32);

                if (source.Type.IsInteger())
                {
                    context.Assembler.Xorps(dest, dest, dest);
                    context.Assembler.Cvtsi2sd(dest, dest, source, source.Type);
                }
                else /* if (source.Type == OperandType.FP32) */
                {
                    context.Assembler.Cvtss2sd(dest, dest, source);

                    GenerateZeroUpper64(context, dest, dest);
                }
            }
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

            if (dest.Kind == OperandKind.Register &&
                source.Kind == OperandKind.Constant && source.Value == 0)
            {
                // Assemble "mov reg, 0" as "xor reg, reg" as the later is more efficient.
                context.Assembler.Xor(dest, dest, OperandType.I32);
            }
            else if (dest.Type.IsInteger())
            {
                context.Assembler.Mov(dest, source, dest.Type);
            }
            else
            {
                context.Assembler.Movdqu(dest, source);
            }
        }

        private static void GenerateCountLeadingZeros(CodeGenContext context, Operation operation)
        {
            Operand dest = operation.Destination;
            Operand source = operation.GetSource(0);

            EnsureSameType(dest, source);

            Debug.Assert(dest.Type.IsInteger());

            context.Assembler.Bsr(dest, source, dest.Type);

            int operandSize = dest.Type == OperandType.I32 ? 32 : 64;
            int operandMask = operandSize - 1;

            // When the input operand is 0, the result is undefined, however the
            // ZF flag is set. We are supposed to return the operand size on that
            // case. So, add an additional jump to handle that case, by moving the
            // operand size constant to the destination register.
            Operand neLabel = Label();

            context.Assembler.Jcc(X86Condition.NotEqual, neLabel);

            context.Assembler.Mov(dest, Const(operandSize | operandMask), OperandType.I32);

            context.Assembler.MarkLabel(neLabel);

            // BSR returns the zero based index of the last bit set on the operand,
            // starting from the least significant bit. However we are supposed to
            // return the number of 0 bits on the high end. So, we invert the result
            // of the BSR using XOR to get the correct value.
            context.Assembler.Xor(dest, Const(operandMask), OperandType.I32);
        }

        private static void GenerateDivide(CodeGenContext context, Operation operation)
        {
            Operand dest = operation.Destination;
            Operand dividend = operation.GetSource(0);
            Operand divisor = operation.GetSource(1);

            if (!dest.Type.IsInteger())
            {
                ValidateBinOp(dest, dividend, divisor);
            }

            if (dest.Type.IsInteger())
            {
                divisor = operation.GetSource(2);

                EnsureSameType(dest, divisor);

                if (divisor.Type == OperandType.I32)
                {
                    context.Assembler.Cdq();
                }
                else
                {
                    context.Assembler.Cqo();
                }

                context.Assembler.Idiv(divisor);
            }
            else if (dest.Type == OperandType.FP32)
            {
                context.Assembler.Divss(dest, dividend, divisor);
            }
            else /* if (dest.Type == OperandType.FP64) */
            {
                context.Assembler.Divsd(dest, dividend, divisor);
            }
        }

        private static void GenerateDivideUI(CodeGenContext context, Operation operation)
        {
            Operand divisor = operation.GetSource(2);

            Operand rdx = Register(X86Register.Rdx);

            Debug.Assert(divisor.Type.IsInteger());

            context.Assembler.Xor(rdx, rdx, OperandType.I32);
            context.Assembler.Div(divisor);
        }

        private static void GenerateFill(CodeGenContext context, Operation operation)
        {
            Operand dest = operation.Destination;
            Operand offset = operation.GetSource(0);

            Debug.Assert(offset.Kind == OperandKind.Constant);

            int offs = offset.AsInt32() + context.CallArgsRegionSize;

            Operand rsp = Register(X86Register.Rsp);

            Operand memOp = MemoryOp(dest.Type, rsp, default, Multiplier.x1, offs);

            GenerateLoad(context, memOp, dest);
        }

        private static void GenerateLoad(CodeGenContext context, Operation operation)
        {
            Operand value = operation.Destination;
            Operand address = Memory(operation.GetSource(0), value.Type);

            GenerateLoad(context, address, value);
        }

        private static void GenerateLoad16(CodeGenContext context, Operation operation)
        {
            Operand value = operation.Destination;
            Operand address = Memory(operation.GetSource(0), value.Type);

            Debug.Assert(value.Type.IsInteger());

            context.Assembler.Movzx16(value, address, value.Type);
        }

        private static void GenerateLoad8(CodeGenContext context, Operation operation)
        {
            Operand value = operation.Destination;
            Operand address = Memory(operation.GetSource(0), value.Type);

            Debug.Assert(value.Type.IsInteger());

            context.Assembler.Movzx8(value, address, value.Type);
        }

        private static void GenerateMemoryBarrier(CodeGenContext context, Operation operation)
        {
            context.Assembler.LockOr(MemoryOp(OperandType.I64, Register(X86Register.Rsp)), Const(0), OperandType.I32);
        }

        private static void GenerateMultiply(CodeGenContext context, Operation operation)
        {
            Operand dest = operation.Destination;
            Operand src1 = operation.GetSource(0);
            Operand src2 = operation.GetSource(1);

            if (src2.Kind != OperandKind.Constant)
            {
                EnsureSameReg(dest, src1);
            }

            EnsureSameType(dest, src1, src2);

            if (dest.Type.IsInteger())
            {
                if (src2.Kind == OperandKind.Constant)
                {
                    context.Assembler.Imul(dest, src1, src2, dest.Type);
                }
                else
                {
                    context.Assembler.Imul(dest, src2, dest.Type);
                }
            }
            else if (dest.Type == OperandType.FP32)
            {
                context.Assembler.Mulss(dest, src1, src2);
            }
            else /* if (dest.Type == OperandType.FP64) */
            {
                context.Assembler.Mulsd(dest, src1, src2);
            }
        }

        private static void GenerateMultiply64HighSI(CodeGenContext context, Operation operation)
        {
            Operand source = operation.GetSource(1);

            Debug.Assert(source.Type == OperandType.I64);

            context.Assembler.Imul(source);
        }

        private static void GenerateMultiply64HighUI(CodeGenContext context, Operation operation)
        {
            Operand source = operation.GetSource(1);

            Debug.Assert(source.Type == OperandType.I64);

            context.Assembler.Mul(source);
        }

        private static void GenerateNegate(CodeGenContext context, Operation operation)
        {
            Operand dest = operation.Destination;
            Operand source = operation.GetSource(0);

            ValidateUnOp(dest, source);

            Debug.Assert(dest.Type.IsInteger());

            context.Assembler.Neg(dest);
        }

        private static void GenerateReturn(CodeGenContext context, Operation operation)
        {
            WriteEpilogue(context);

            context.Assembler.Return();
        }

        private static void GenerateRotateRight(CodeGenContext context, Operation operation)
        {
            Operand dest = operation.Destination;
            Operand src1 = operation.GetSource(0);
            Operand src2 = operation.GetSource(1);

            ValidateShift(dest, src1, src2);

            context.Assembler.Ror(dest, src2, dest.Type);
        }

        private static void GenerateShiftLeft(CodeGenContext context, Operation operation)
        {
            Operand dest = operation.Destination;
            Operand src1 = operation.GetSource(0);
            Operand src2 = operation.GetSource(1);

            ValidateShift(dest, src1, src2);

            context.Assembler.Shl(dest, src2, dest.Type);
        }

        private static void GenerateShiftRightSI(CodeGenContext context, Operation operation)
        {
            Operand dest = operation.Destination;
            Operand src1 = operation.GetSource(0);
            Operand src2 = operation.GetSource(1);

            ValidateShift(dest, src1, src2);

            context.Assembler.Sar(dest, src2, dest.Type);
        }

        private static void GenerateShiftRightUI(CodeGenContext context, Operation operation)
        {
            Operand dest = operation.Destination;
            Operand src1 = operation.GetSource(0);
            Operand src2 = operation.GetSource(1);

            ValidateShift(dest, src1, src2);

            context.Assembler.Shr(dest, src2, dest.Type);
        }

        private static void GenerateSignExtend16(CodeGenContext context, Operation operation)
        {
            Operand dest = operation.Destination;
            Operand source = operation.GetSource(0);

            Debug.Assert(dest.Type.IsInteger() && source.Type.IsInteger());

            context.Assembler.Movsx16(dest, source, dest.Type);
        }

        private static void GenerateSignExtend32(CodeGenContext context, Operation operation)
        {
            Operand dest = operation.Destination;
            Operand source = operation.GetSource(0);

            Debug.Assert(dest.Type.IsInteger() && source.Type.IsInteger());

            context.Assembler.Movsx32(dest, source, dest.Type);
        }

        private static void GenerateSignExtend8(CodeGenContext context, Operation operation)
        {
            Operand dest = operation.Destination;
            Operand source = operation.GetSource(0);

            Debug.Assert(dest.Type.IsInteger() && source.Type.IsInteger());

            context.Assembler.Movsx8(dest, source, dest.Type);
        }

        private static void GenerateSpill(CodeGenContext context, Operation operation)
        {
            GenerateSpill(context, operation, context.CallArgsRegionSize);
        }

        private static void GenerateSpillArg(CodeGenContext context, Operation operation)
        {
            GenerateSpill(context, operation, 0);
        }

        private static void GenerateSpill(CodeGenContext context, Operation operation, int baseOffset)
        {
            Operand offset = operation.GetSource(0);
            Operand source = operation.GetSource(1);

            Debug.Assert(offset.Kind == OperandKind.Constant);

            int offs = offset.AsInt32() + baseOffset;

            Operand rsp = Register(X86Register.Rsp);

            Operand memOp = MemoryOp(source.Type, rsp, default, Multiplier.x1, offs);

            GenerateStore(context, memOp, source);
        }

        private static void GenerateStackAlloc(CodeGenContext context, Operation operation)
        {
            Operand dest = operation.Destination;
            Operand offset = operation.GetSource(0);

            Debug.Assert(offset.Kind == OperandKind.Constant);

            int offs = offset.AsInt32() + context.CallArgsRegionSize;

            Operand rsp = Register(X86Register.Rsp);

            Operand memOp = MemoryOp(OperandType.I64, rsp, default, Multiplier.x1, offs);

            context.Assembler.Lea(dest, memOp, OperandType.I64);
        }

        private static void GenerateStore(CodeGenContext context, Operation operation)
        {
            Operand value = operation.GetSource(1);
            Operand address = Memory(operation.GetSource(0), value.Type);

            GenerateStore(context, address, value);
        }

        private static void GenerateStore16(CodeGenContext context, Operation operation)
        {
            Operand value = operation.GetSource(1);
            Operand address = Memory(operation.GetSource(0), value.Type);

            Debug.Assert(value.Type.IsInteger());

            context.Assembler.Mov16(address, value);
        }

        private static void GenerateStore8(CodeGenContext context, Operation operation)
        {
            Operand value = operation.GetSource(1);
            Operand address = Memory(operation.GetSource(0), value.Type);

            Debug.Assert(value.Type.IsInteger());

            context.Assembler.Mov8(address, value);
        }

        private static void GenerateSubtract(CodeGenContext context, Operation operation)
        {
            Operand dest = operation.Destination;
            Operand src1 = operation.GetSource(0);
            Operand src2 = operation.GetSource(1);

            ValidateBinOp(dest, src1, src2);

            if (dest.Type.IsInteger())
            {
                context.Assembler.Sub(dest, src2, dest.Type);
            }
            else if (dest.Type == OperandType.FP32)
            {
                context.Assembler.Subss(dest, src1, src2);
            }
            else /* if (dest.Type == OperandType.FP64) */
            {
                context.Assembler.Subsd(dest, src1, src2);
            }
        }

        private static void GenerateTailcall(CodeGenContext context, Operation operation)
        {
            WriteEpilogue(context);

            context.Assembler.Jmp(operation.GetSource(0));
        }

        private static void GenerateVectorCreateScalar(CodeGenContext context, Operation operation)
        {
            Operand dest = operation.Destination;
            Operand source = operation.GetSource(0);

            Debug.Assert(!dest.Type.IsInteger() && source.Type.IsInteger());

            if (source.Type == OperandType.I32)
            {
                context.Assembler.Movd(dest, source); // (__m128i _mm_cvtsi32_si128(int a))
            }
            else /* if (source.Type == OperandType.I64) */
            {
                context.Assembler.Movq(dest, source); // (__m128i _mm_cvtsi64_si128(__int64 a))
            }
        }

        private static void GenerateVectorExtract(CodeGenContext context, Operation operation)
        {
            Operand dest = operation.Destination;  //Value
            Operand src1 = operation.GetSource(0); //Vector
            Operand src2 = operation.GetSource(1); //Index

            Debug.Assert(src1.Type == OperandType.V128);
            Debug.Assert(src2.Kind == OperandKind.Constant);

            byte index = src2.AsByte();

            Debug.Assert(index < OperandType.V128.GetSizeInBytes() / dest.Type.GetSizeInBytes());

            if (dest.Type == OperandType.I32)
            {
                if (index == 0)
                {
                    context.Assembler.Movd(dest, src1);
                }
                else if (HardwareCapabilities.SupportsSse41)
                {
                    context.Assembler.Pextrd(dest, src1, index);
                }
                else
                {
                    int mask0 = 0b11_10_01_00;
                    int mask1 = 0b11_10_01_00;

                    mask0 = BitUtils.RotateRight(mask0, index * 2, 8);
                    mask1 = BitUtils.RotateRight(mask1, 8 - index * 2, 8);

                    context.Assembler.Pshufd(src1, src1, (byte)mask0);
                    context.Assembler.Movd(dest, src1);
                    context.Assembler.Pshufd(src1, src1, (byte)mask1);
                }
            }
            else if (dest.Type == OperandType.I64)
            {
                if (index == 0)
                {
                    context.Assembler.Movq(dest, src1);
                }
                else if (HardwareCapabilities.SupportsSse41)
                {
                    context.Assembler.Pextrq(dest, src1, index);
                }
                else
                {
                    const byte Mask = 0b01_00_11_10;

                    context.Assembler.Pshufd(src1, src1, Mask);
                    context.Assembler.Movq(dest, src1);
                    context.Assembler.Pshufd(src1, src1, Mask);
                }
            }
            else
            {
                // Floating-point types.
                if ((index >= 2 && dest.Type == OperandType.FP32) ||
                    (index == 1 && dest.Type == OperandType.FP64))
                {
                    context.Assembler.Movhlps(dest, dest, src1);
                    context.Assembler.Movq(dest, dest);
                }
                else
                {
                    context.Assembler.Movq(dest, src1);
                }

                if (dest.Type == OperandType.FP32)
                {
                    context.Assembler.Pshufd(dest, dest, (byte)(0xfc | (index & 1)));
                }
            }
        }

        private static void GenerateVectorExtract16(CodeGenContext context, Operation operation)
        {
            Operand dest = operation.Destination;  //Value
            Operand src1 = operation.GetSource(0); //Vector
            Operand src2 = operation.GetSource(1); //Index

            Debug.Assert(src1.Type == OperandType.V128);
            Debug.Assert(src2.Kind == OperandKind.Constant);

            byte index = src2.AsByte();

            Debug.Assert(index < 8);

            context.Assembler.Pextrw(dest, src1, index);
        }

        private static void GenerateVectorExtract8(CodeGenContext context, Operation operation)
        {
            Operand dest = operation.Destination;  //Value
            Operand src1 = operation.GetSource(0); //Vector
            Operand src2 = operation.GetSource(1); //Index

            Debug.Assert(src1.Type == OperandType.V128);
            Debug.Assert(src2.Kind == OperandKind.Constant);

            byte index = src2.AsByte();

            Debug.Assert(index < 16);

            if (HardwareCapabilities.SupportsSse41)
            {
                context.Assembler.Pextrb(dest, src1, index);
            }
            else
            {
                context.Assembler.Pextrw(dest, src1, (byte)(index >> 1));

                if ((index & 1) != 0)
                {
                    context.Assembler.Shr(dest, Const(8), OperandType.I32);
                }
                else
                {
                    context.Assembler.Movzx8(dest, dest, OperandType.I32);
                }
            }
        }

        private static void GenerateVectorInsert(CodeGenContext context, Operation operation)
        {
            Operand dest = operation.Destination;
            Operand src1 = operation.GetSource(0); //Vector
            Operand src2 = operation.GetSource(1); //Value
            Operand src3 = operation.GetSource(2); //Index

            if (!HardwareCapabilities.SupportsVexEncoding)
            {
                EnsureSameReg(dest, src1);
            }

            Debug.Assert(src1.Type == OperandType.V128);
            Debug.Assert(src3.Kind == OperandKind.Constant);

            byte index = src3.AsByte();

            void InsertIntSse2(int words)
            {
                if (dest.GetRegister() != src1.GetRegister())
                {
                    context.Assembler.Movdqu(dest, src1);
                }

                for (int word = 0; word < words; word++)
                {
                    // Insert lower 16-bits.
                    context.Assembler.Pinsrw(dest, dest, src2, (byte)(index * words + word));

                    // Move next word down.
                    context.Assembler.Ror(src2, Const(16), src2.Type);
                }
            }

            if (src2.Type == OperandType.I32)
            {
                Debug.Assert(index < 4);

                if (HardwareCapabilities.SupportsSse41)
                {
                    context.Assembler.Pinsrd(dest, src1, src2, index);
                }
                else
                {
                    InsertIntSse2(2);
                }
            }
            else if (src2.Type == OperandType.I64)
            {
                Debug.Assert(index < 2);

                if (HardwareCapabilities.SupportsSse41)
                {
                    context.Assembler.Pinsrq(dest, src1, src2, index);
                }
                else
                {
                    InsertIntSse2(4);
                }
            }
            else if (src2.Type == OperandType.FP32)
            {
                Debug.Assert(index < 4);

                if (index != 0)
                {
                    if (HardwareCapabilities.SupportsSse41)
                    {
                        context.Assembler.Insertps(dest, src1, src2, (byte)(index << 4));
                    }
                    else
                    {
                        if (src1.GetRegister() == src2.GetRegister())
                        {
                            int mask = 0b11_10_01_00;

                            mask &= ~(0b11 << index * 2);

                            context.Assembler.Pshufd(dest, src1, (byte)mask);
                        }
                        else
                        {
                            int mask0 = 0b11_10_01_00;
                            int mask1 = 0b11_10_01_00;

                            mask0 = BitUtils.RotateRight(mask0, index * 2, 8);
                            mask1 = BitUtils.RotateRight(mask1, 8 - index * 2, 8);

                            context.Assembler.Pshufd(src1, src1, (byte)mask0); // Lane to be inserted in position 0.
                            context.Assembler.Movss(dest, src1, src2);         // dest[127:0] = src1[127:32] | src2[31:0]
                            context.Assembler.Pshufd(dest, dest, (byte)mask1); // Inserted lane in original position.

                            if (dest.GetRegister() != src1.GetRegister())
                            {
                                context.Assembler.Pshufd(src1, src1, (byte)mask1); // Restore src1.
                            }
                        }
                    }
                }
                else
                {
                    context.Assembler.Movss(dest, src1, src2);
                }
            }
            else /* if (src2.Type == OperandType.FP64) */
            {
                Debug.Assert(index < 2);

                if (index != 0)
                {
                    context.Assembler.Movlhps(dest, src1, src2);
                }
                else
                {
                    context.Assembler.Movsd(dest, src1, src2);
                }
            }
        }

        private static void GenerateVectorInsert16(CodeGenContext context, Operation operation)
        {
            Operand dest = operation.Destination;
            Operand src1 = operation.GetSource(0); //Vector
            Operand src2 = operation.GetSource(1); //Value
            Operand src3 = operation.GetSource(2); //Index

            if (!HardwareCapabilities.SupportsVexEncoding)
            {
                EnsureSameReg(dest, src1);
            }

            Debug.Assert(src1.Type == OperandType.V128);
            Debug.Assert(src3.Kind == OperandKind.Constant);

            byte index = src3.AsByte();

            context.Assembler.Pinsrw(dest, src1, src2, index);
        }

        private static void GenerateVectorInsert8(CodeGenContext context, Operation operation)
        {
            Operand dest = operation.Destination;
            Operand src1 = operation.GetSource(0); //Vector
            Operand src2 = operation.GetSource(1); //Value
            Operand src3 = operation.GetSource(2); //Index

            // It's not possible to emulate this instruction without
            // SSE 4.1 support without the use of a temporary register,
            // so we instead handle that case on the pre-allocator when
            // SSE 4.1 is not supported on the CPU.
            Debug.Assert(HardwareCapabilities.SupportsSse41);

            if (!HardwareCapabilities.SupportsVexEncoding)
            {
                EnsureSameReg(dest, src1);
            }

            Debug.Assert(src1.Type == OperandType.V128);
            Debug.Assert(src3.Kind == OperandKind.Constant);

            byte index = src3.AsByte();

            context.Assembler.Pinsrb(dest, src1, src2, index);
        }

        private static void GenerateVectorOne(CodeGenContext context, Operation operation)
        {
            Operand dest = operation.Destination;

            Debug.Assert(!dest.Type.IsInteger());

            context.Assembler.Pcmpeqw(dest, dest, dest);
        }

        private static void GenerateVectorZero(CodeGenContext context, Operation operation)
        {
            Operand dest = operation.Destination;

            Debug.Assert(!dest.Type.IsInteger());

            context.Assembler.Xorps(dest, dest, dest);
        }

        private static void GenerateVectorZeroUpper64(CodeGenContext context, Operation operation)
        {
            Operand dest = operation.Destination;
            Operand source = operation.GetSource(0);

            Debug.Assert(dest.Type == OperandType.V128 && source.Type == OperandType.V128);

            GenerateZeroUpper64(context, dest, source);
        }

        private static void GenerateVectorZeroUpper96(CodeGenContext context, Operation operation)
        {
            Operand dest = operation.Destination;
            Operand source = operation.GetSource(0);

            Debug.Assert(dest.Type == OperandType.V128 && source.Type == OperandType.V128);

            GenerateZeroUpper96(context, dest, source);
        }

        private static void GenerateZeroExtend16(CodeGenContext context, Operation operation)
        {
            Operand dest = operation.Destination;
            Operand source = operation.GetSource(0);

            Debug.Assert(dest.Type.IsInteger() && source.Type.IsInteger());

            context.Assembler.Movzx16(dest, source, OperandType.I32);
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

            context.Assembler.Mov(dest, source, OperandType.I32);
        }

        private static void GenerateZeroExtend8(CodeGenContext context, Operation operation)
        {
            Operand dest = operation.Destination;
            Operand source = operation.GetSource(0);

            Debug.Assert(dest.Type.IsInteger() && source.Type.IsInteger());

            context.Assembler.Movzx8(dest, source, OperandType.I32);
        }

        private static void GenerateLoad(CodeGenContext context, Operand address, Operand value)
        {
            switch (value.Type)
            {
                case OperandType.I32:
                    context.Assembler.Mov(value, address, OperandType.I32);
                    break;
                case OperandType.I64:
                    context.Assembler.Mov(value, address, OperandType.I64);
                    break;
                case OperandType.FP32:
                    context.Assembler.Movd(value, address);
                    break;
                case OperandType.FP64:
                    context.Assembler.Movq(value, address);
                    break;
                case OperandType.V128:
                    context.Assembler.Movdqu(value, address);
                    break;

                default:
                    Debug.Assert(false);
                    break;
            }
        }

        private static void GenerateStore(CodeGenContext context, Operand address, Operand value)
        {
            switch (value.Type)
            {
                case OperandType.I32:
                    context.Assembler.Mov(address, value, OperandType.I32);
                    break;
                case OperandType.I64:
                    context.Assembler.Mov(address, value, OperandType.I64);
                    break;
                case OperandType.FP32:
                    context.Assembler.Movd(address, value);
                    break;
                case OperandType.FP64:
                    context.Assembler.Movq(address, value);
                    break;
                case OperandType.V128:
                    context.Assembler.Movdqu(address, value);
                    break;

                default:
                    Debug.Assert(false);
                    break;
            }
        }

        private static void GenerateZeroUpper64(CodeGenContext context, Operand dest, Operand source)
        {
            context.Assembler.Movq(dest, source);
        }

        private static void GenerateZeroUpper96(CodeGenContext context, Operand dest, Operand source)
        {
            context.Assembler.Movq(dest, source);
            context.Assembler.Pshufd(dest, dest, 0xfc);
        }

        private static bool MatchOperation(Operation node, Instruction inst, OperandType destType, Register destReg)
        {
            if (node == default || node.DestinationsCount == 0)
            {
                return false;
            }

            if (node.Instruction != inst)
            {
                return false;
            }

            Operand dest = node.Destination;

            return dest.Kind == OperandKind.Register &&
                   dest.Type == destType &&
                   dest.GetRegister() == destReg;
        }

        [Conditional("DEBUG")]
        private static void ValidateUnOp(Operand dest, Operand source)
        {
            EnsureSameReg(dest, source);
            EnsureSameType(dest, source);
        }

        [Conditional("DEBUG")]
        private static void ValidateBinOp(Operand dest, Operand src1, Operand src2)
        {
            EnsureSameReg(dest, src1);
            EnsureSameType(dest, src1, src2);
        }

        [Conditional("DEBUG")]
        private static void ValidateShift(Operand dest, Operand src1, Operand src2)
        {
            EnsureSameReg(dest, src1);
            EnsureSameType(dest, src1);

            Debug.Assert(dest.Type.IsInteger() && src2.Type == OperandType.I32);
        }

        private static void EnsureSameReg(Operand op1, Operand op2)
        {
            if (!op1.Type.IsInteger() && HardwareCapabilities.SupportsVexEncoding)
            {
                return;
            }

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

        private static void EnsureSameType(Operand op1, Operand op2, Operand op3, Operand op4)
        {
            Debug.Assert(op1.Type == op2.Type);
            Debug.Assert(op1.Type == op3.Type);
            Debug.Assert(op1.Type == op4.Type);
        }

        private static UnwindInfo WritePrologue(CodeGenContext context)
        {
            List<UnwindPushEntry> pushEntries = [];

            Operand rsp = Register(X86Register.Rsp);

            int mask = CallingConvention.GetIntCalleeSavedRegisters() & context.AllocResult.IntUsedRegisters;

            while (mask != 0)
            {
                int bit = BitOperations.TrailingZeroCount(mask);

                context.Assembler.Push(Register((X86Register)bit));

                pushEntries.Add(new UnwindPushEntry(UnwindPseudoOp.PushReg, context.StreamOffset, regIndex: bit));

                mask &= ~(1 << bit);
            }

            int reservedStackSize = context.CallArgsRegionSize + context.AllocResult.SpillRegionSize;

            reservedStackSize += context.XmmSaveRegionSize;

            if (reservedStackSize >= StackGuardSize)
            {
                GenerateInlineStackProbe(context, reservedStackSize);
            }

            if (reservedStackSize != 0)
            {
                context.Assembler.Sub(rsp, Const(reservedStackSize), OperandType.I64);

                pushEntries.Add(new UnwindPushEntry(UnwindPseudoOp.AllocStack, context.StreamOffset, stackOffsetOrAllocSize: reservedStackSize));
            }

            int offset = reservedStackSize;

            mask = CallingConvention.GetVecCalleeSavedRegisters() & context.AllocResult.VecUsedRegisters;

            while (mask != 0)
            {
                int bit = BitOperations.TrailingZeroCount(mask);

                offset -= 16;

                Operand memOp = MemoryOp(OperandType.V128, rsp, default, Multiplier.x1, offset);

                context.Assembler.Movdqu(memOp, Xmm((X86Register)bit));

                pushEntries.Add(new UnwindPushEntry(UnwindPseudoOp.SaveXmm128, context.StreamOffset, bit, offset));

                mask &= ~(1 << bit);
            }

            return new UnwindInfo(pushEntries.ToArray(), context.StreamOffset);
        }

        private static void WriteEpilogue(CodeGenContext context)
        {
            Operand rsp = Register(X86Register.Rsp);

            int reservedStackSize = context.CallArgsRegionSize + context.AllocResult.SpillRegionSize;

            reservedStackSize += context.XmmSaveRegionSize;

            int offset = reservedStackSize;

            int mask = CallingConvention.GetVecCalleeSavedRegisters() & context.AllocResult.VecUsedRegisters;

            while (mask != 0)
            {
                int bit = BitOperations.TrailingZeroCount(mask);

                offset -= 16;

                Operand memOp = MemoryOp(OperandType.V128, rsp, default, Multiplier.x1, offset);

                context.Assembler.Movdqu(Xmm((X86Register)bit), memOp);

                mask &= ~(1 << bit);
            }

            if (reservedStackSize != 0)
            {
                context.Assembler.Add(rsp, Const(reservedStackSize), OperandType.I64);
            }

            mask = CallingConvention.GetIntCalleeSavedRegisters() & context.AllocResult.IntUsedRegisters;

            while (mask != 0)
            {
                int bit = BitUtils.HighestBitSet(mask);

                context.Assembler.Pop(Register((X86Register)bit));

                mask &= ~(1 << bit);
            }
        }

        private static void GenerateInlineStackProbe(CodeGenContext context, int size)
        {
            // Windows does lazy stack allocation, and there are just 2
            // guard pages on the end of the stack. So, if the allocation
            // size we make is greater than this guard size, we must ensure
            // that the OS will map all pages that we'll use. We do that by
            // doing a dummy read on those pages, forcing a page fault and
            // the OS to map them. If they are already mapped, nothing happens.
            const int PageMask = PageSize - 1;

            size = (size + PageMask) & ~PageMask;

            Operand rsp = Register(X86Register.Rsp);
            Operand temp = Register(CallingConvention.GetIntReturnRegister());

            for (int offset = PageSize; offset < size; offset += PageSize)
            {
                Operand memOp = MemoryOp(OperandType.I32, rsp, default, Multiplier.x1, -offset);

                context.Assembler.Mov(temp, memOp, OperandType.I32);
            }
        }

        private static Operand Memory(Operand operand, OperandType type)
        {
            if (operand.Kind == OperandKind.Memory)
            {
                return operand;
            }

            return MemoryOp(type, operand);
        }

        private static Operand Register(X86Register register, OperandType type = OperandType.I64)
        {
            return Operand.Factory.Register((int)register, RegisterType.Integer, type);
        }

        private static Operand Xmm(X86Register register)
        {
            return Operand.Factory.Register((int)register, RegisterType.Vector, OperandType.V128);
        }
    }
}
