using ARMeilleure.Diagnostics;
using ARMeilleure.IntermediateRepresentation;
using ARMeilleure.State;
using System;
using System.Collections.Generic;
using System.Reflection;
using static ARMeilleure.IntermediateRepresentation.Operand.Factory;

namespace ARMeilleure.Translation
{
    class EmitterContext
    {
        private int _localsCount;

        private readonly Dictionary<Operand, BasicBlock> _irLabels;
        private readonly IntrusiveList<BasicBlock> _irBlocks;

        private BasicBlock _irBlock;
        private BasicBlock _ifBlock;

        private bool _needsNewBlock;
        private BasicBlockFrequency _nextBlockFreq;

        public EmitterContext()
        {
            _localsCount = 0;

            _irLabels = new Dictionary<Operand, BasicBlock>();
            _irBlocks = new IntrusiveList<BasicBlock>();

            _needsNewBlock = true;
            _nextBlockFreq = BasicBlockFrequency.Default;
        }

        public Operand AllocateLocal(OperandType type)
        {
            Operand local = Local(type);

            local.NumberLocal(++_localsCount);

            return local;
        }

        public Operand Add(Operand op1, Operand op2)
        {
            return Add(Instruction.Add, Local(op1.Type), op1, op2);
        }

        public Operand BitwiseAnd(Operand op1, Operand op2)
        {
            return Add(Instruction.BitwiseAnd, Local(op1.Type), op1, op2);
        }

        public Operand BitwiseExclusiveOr(Operand op1, Operand op2)
        {
            return Add(Instruction.BitwiseExclusiveOr, Local(op1.Type), op1, op2);
        }

        public Operand BitwiseNot(Operand op1)
        {
            return Add(Instruction.BitwiseNot, Local(op1.Type), op1);
        }

        public Operand BitwiseOr(Operand op1, Operand op2)
        {
            return Add(Instruction.BitwiseOr, Local(op1.Type), op1, op2);
        }

        public void Branch(Operand label)
        {
            NewNextBlockIfNeeded();

            BranchToLabel(label, uncond: true, BasicBlockFrequency.Default);
        }

        public void BranchIf(Operand label, Operand op1, Operand op2, Comparison comp, BasicBlockFrequency falseFreq = default)
        {
            Add(Instruction.BranchIf, default, op1, op2, Const((int)comp));

            BranchToLabel(label, uncond: false, falseFreq);
        }

        public void BranchIfFalse(Operand label, Operand op1, BasicBlockFrequency falseFreq = default)
        {
            BranchIf(label, op1, Const(op1.Type, 0), Comparison.Equal, falseFreq);
        }

        public void BranchIfTrue(Operand label, Operand op1, BasicBlockFrequency falseFreq = default)
        {
            BranchIf(label, op1, Const(op1.Type, 0), Comparison.NotEqual, falseFreq);
        }

        public Operand ByteSwap(Operand op1)
        {
            return Add(Instruction.ByteSwap, Local(op1.Type), op1);
        }

        public virtual Operand Call(MethodInfo info, params Operand[] callArgs)
        {
            nint funcPtr = Delegates.GetDelegateFuncPtr(info);

            OperandType returnType = GetOperandType(info.ReturnType);

            Symbols.Add((ulong)funcPtr.ToInt64(), info.Name);

            return Call(Const(funcPtr.ToInt64()), returnType, callArgs);
        }

        protected static OperandType GetOperandType(Type type)
        {
            if (type == typeof(bool) || type == typeof(byte) ||
                type == typeof(char) || type == typeof(short) ||
                type == typeof(int) || type == typeof(sbyte) ||
                type == typeof(ushort) || type == typeof(uint))
            {
                return OperandType.I32;
            }
            else if (type == typeof(long) || type == typeof(ulong))
            {
                return OperandType.I64;
            }
            else if (type == typeof(double))
            {
                return OperandType.FP64;
            }
            else if (type == typeof(float))
            {
                return OperandType.FP32;
            }
            else if (type == typeof(V128))
            {
                return OperandType.V128;
            }
            else if (type == typeof(void))
            {
                return OperandType.None;
            }
            else
            {
                throw new ArgumentException($"Invalid type \"{type.Name}\".");
            }
        }

        public Operand Call(Operand address, OperandType returnType, params Operand[] callArgs)
        {
            Operand[] args = new Operand[callArgs.Length + 1];

            args[0] = address;

            Array.Copy(callArgs, 0, args, 1, callArgs.Length);

            if (returnType != OperandType.None)
            {
                return Add(Instruction.Call, Local(returnType), args);
            }
            else
            {
                return Add(Instruction.Call, default, args);
            }
        }

        public void Tailcall(Operand address, params Operand[] callArgs)
        {
            Operand[] args = new Operand[callArgs.Length + 1];

            args[0] = address;

            Array.Copy(callArgs, 0, args, 1, callArgs.Length);

            Add(Instruction.Tailcall, default, args);

            _needsNewBlock = true;
        }

        public Operand CompareAndSwap(Operand address, Operand expected, Operand desired)
        {
            return Add(Instruction.CompareAndSwap, Local(desired.Type), address, expected, desired);
        }

        public Operand CompareAndSwap16(Operand address, Operand expected, Operand desired)
        {
            return Add(Instruction.CompareAndSwap16, Local(OperandType.I32), address, expected, desired);
        }

        public Operand CompareAndSwap8(Operand address, Operand expected, Operand desired)
        {
            return Add(Instruction.CompareAndSwap8, Local(OperandType.I32), address, expected, desired);
        }

        public Operand ConditionalSelect(Operand op1, Operand op2, Operand op3)
        {
            return Add(Instruction.ConditionalSelect, Local(op2.Type), op1, op2, op3);
        }

        public Operand ConvertI64ToI32(Operand op1)
        {
            if (op1.Type != OperandType.I64)
            {
                throw new ArgumentException($"Invalid operand type \"{op1.Type}\".");
            }

            return Add(Instruction.ConvertI64ToI32, Local(OperandType.I32), op1);
        }

        public Operand ConvertToFP(OperandType type, Operand op1)
        {
            return Add(Instruction.ConvertToFP, Local(type), op1);
        }

        public Operand ConvertToFPUI(OperandType type, Operand op1)
        {
            return Add(Instruction.ConvertToFPUI, Local(type), op1);
        }

        public Operand Copy(Operand op1)
        {
            return Add(Instruction.Copy, Local(op1.Type), op1);
        }

        public Operand Copy(Operand dest, Operand op1)
        {
            if (dest.Kind != OperandKind.Register &&
                (dest.Kind != OperandKind.LocalVariable || dest.GetLocalNumber() == 0))
            {
                throw new ArgumentException($"Destination operand must be a Register or a numbered LocalVariable.");
            }

            return Add(Instruction.Copy, dest, op1);
        }

        public Operand CountLeadingZeros(Operand op1)
        {
            return Add(Instruction.CountLeadingZeros, Local(op1.Type), op1);
        }

        public Operand Divide(Operand op1, Operand op2)
        {
            return Add(Instruction.Divide, Local(op1.Type), op1, op2);
        }

        public Operand DivideUI(Operand op1, Operand op2)
        {
            return Add(Instruction.DivideUI, Local(op1.Type), op1, op2);
        }

        public Operand ICompare(Operand op1, Operand op2, Comparison comp)
        {
            return Add(Instruction.Compare, Local(OperandType.I32), op1, op2, Const((int)comp));
        }

        public Operand ICompareEqual(Operand op1, Operand op2)
        {
            return ICompare(op1, op2, Comparison.Equal);
        }

        public Operand ICompareGreater(Operand op1, Operand op2)
        {
            return ICompare(op1, op2, Comparison.Greater);
        }

        public Operand ICompareGreaterOrEqual(Operand op1, Operand op2)
        {
            return ICompare(op1, op2, Comparison.GreaterOrEqual);
        }

        public Operand ICompareGreaterOrEqualUI(Operand op1, Operand op2)
        {
            return ICompare(op1, op2, Comparison.GreaterOrEqualUI);
        }

        public Operand ICompareGreaterUI(Operand op1, Operand op2)
        {
            return ICompare(op1, op2, Comparison.GreaterUI);
        }

        public Operand ICompareLess(Operand op1, Operand op2)
        {
            return ICompare(op1, op2, Comparison.Less);
        }

        public Operand ICompareLessOrEqual(Operand op1, Operand op2)
        {
            return ICompare(op1, op2, Comparison.LessOrEqual);
        }

        public Operand ICompareLessOrEqualUI(Operand op1, Operand op2)
        {
            return ICompare(op1, op2, Comparison.LessOrEqualUI);
        }

        public Operand ICompareLessUI(Operand op1, Operand op2)
        {
            return ICompare(op1, op2, Comparison.LessUI);
        }

        public Operand ICompareNotEqual(Operand op1, Operand op2)
        {
            return ICompare(op1, op2, Comparison.NotEqual);
        }

        public Operand Load(OperandType type, Operand address)
        {
            return Add(Instruction.Load, Local(type), address);
        }

        public Operand Load16(Operand address)
        {
            return Add(Instruction.Load16, Local(OperandType.I32), address);
        }

        public Operand Load8(Operand address)
        {
            return Add(Instruction.Load8, Local(OperandType.I32), address);
        }

        public Operand LoadArgument(OperandType type, int index)
        {
            return Add(Instruction.LoadArgument, Local(type), Const(index));
        }

        public void LoadFromContext()
        {
            _needsNewBlock = true;

            Add(Instruction.LoadFromContext);
        }

        public void MemoryBarrier()
        {
            Add(Instruction.MemoryBarrier);
        }

        public Operand Multiply(Operand op1, Operand op2)
        {
            return Add(Instruction.Multiply, Local(op1.Type), op1, op2);
        }

        public Operand Multiply64HighSI(Operand op1, Operand op2)
        {
            return Add(Instruction.Multiply64HighSI, Local(OperandType.I64), op1, op2);
        }

        public Operand Multiply64HighUI(Operand op1, Operand op2)
        {
            return Add(Instruction.Multiply64HighUI, Local(OperandType.I64), op1, op2);
        }

        public Operand Negate(Operand op1)
        {
            return Add(Instruction.Negate, Local(op1.Type), op1);
        }

        public void Return()
        {
            Add(Instruction.Return);

            _needsNewBlock = true;
        }

        public void Return(Operand op1)
        {
            Add(Instruction.Return, default, op1);

            _needsNewBlock = true;
        }

        public Operand RotateRight(Operand op1, Operand op2)
        {
            return Add(Instruction.RotateRight, Local(op1.Type), op1, op2);
        }

        public Operand ShiftLeft(Operand op1, Operand op2)
        {
            return Add(Instruction.ShiftLeft, Local(op1.Type), op1, op2);
        }

        public Operand ShiftRightSI(Operand op1, Operand op2)
        {
            return Add(Instruction.ShiftRightSI, Local(op1.Type), op1, op2);
        }

        public Operand ShiftRightUI(Operand op1, Operand op2)
        {
            return Add(Instruction.ShiftRightUI, Local(op1.Type), op1, op2);
        }

        public Operand SignExtend16(OperandType type, Operand op1)
        {
            return Add(Instruction.SignExtend16, Local(type), op1);
        }

        public Operand SignExtend32(OperandType type, Operand op1)
        {
            return Add(Instruction.SignExtend32, Local(type), op1);
        }

        public Operand SignExtend8(OperandType type, Operand op1)
        {
            return Add(Instruction.SignExtend8, Local(type), op1);
        }

        public void Store(Operand address, Operand value)
        {
            Add(Instruction.Store, default, address, value);
        }

        public void Store16(Operand address, Operand value)
        {
            Add(Instruction.Store16, default, address, value);
        }

        public void Store8(Operand address, Operand value)
        {
            Add(Instruction.Store8, default, address, value);
        }

        public void StoreToContext()
        {
            Add(Instruction.StoreToContext);

            _needsNewBlock = true;
        }

        public Operand Subtract(Operand op1, Operand op2)
        {
            return Add(Instruction.Subtract, Local(op1.Type), op1, op2);
        }

        public Operand VectorCreateScalar(Operand value)
        {
            return Add(Instruction.VectorCreateScalar, Local(OperandType.V128), value);
        }

        public Operand VectorExtract(OperandType type, Operand vector, int index)
        {
            return Add(Instruction.VectorExtract, Local(type), vector, Const(index));
        }

        public Operand VectorExtract16(Operand vector, int index)
        {
            return Add(Instruction.VectorExtract16, Local(OperandType.I32), vector, Const(index));
        }

        public Operand VectorExtract8(Operand vector, int index)
        {
            return Add(Instruction.VectorExtract8, Local(OperandType.I32), vector, Const(index));
        }

        public Operand VectorInsert(Operand vector, Operand value, int index)
        {
            return Add(Instruction.VectorInsert, Local(OperandType.V128), vector, value, Const(index));
        }

        public Operand VectorInsert16(Operand vector, Operand value, int index)
        {
            return Add(Instruction.VectorInsert16, Local(OperandType.V128), vector, value, Const(index));
        }

        public Operand VectorInsert8(Operand vector, Operand value, int index)
        {
            return Add(Instruction.VectorInsert8, Local(OperandType.V128), vector, value, Const(index));
        }

        public Operand VectorOne()
        {
            return Add(Instruction.VectorOne, Local(OperandType.V128));
        }

        public Operand VectorZero()
        {
            return Add(Instruction.VectorZero, Local(OperandType.V128));
        }

        public Operand VectorZeroUpper64(Operand vector)
        {
            return Add(Instruction.VectorZeroUpper64, Local(OperandType.V128), vector);
        }

        public Operand VectorZeroUpper96(Operand vector)
        {
            return Add(Instruction.VectorZeroUpper96, Local(OperandType.V128), vector);
        }

        public Operand ZeroExtend16(OperandType type, Operand op1)
        {
            return Add(Instruction.ZeroExtend16, Local(type), op1);
        }

        public Operand ZeroExtend32(OperandType type, Operand op1)
        {
            return Add(Instruction.ZeroExtend32, Local(type), op1);
        }

        public Operand ZeroExtend8(OperandType type, Operand op1)
        {
            return Add(Instruction.ZeroExtend8, Local(type), op1);
        }

        private void NewNextBlockIfNeeded()
        {
            if (_needsNewBlock)
            {
                NewNextBlock();
            }
        }

        private Operand Add(Instruction inst, Operand dest = default)
        {
            NewNextBlockIfNeeded();

            Operation operation = Operation.Factory.Operation(inst, dest);

            _irBlock.Operations.AddLast(operation);

            return dest;
        }

        private Operand Add(Instruction inst, Operand dest, Operand[] sources)
        {
            NewNextBlockIfNeeded();

            Operation operation = Operation.Factory.Operation(inst, dest, sources);

            _irBlock.Operations.AddLast(operation);

            return dest;
        }

        private Operand Add(Instruction inst, Operand dest, Operand source0)
        {
            NewNextBlockIfNeeded();

            Operation operation = Operation.Factory.Operation(inst, dest, source0);

            _irBlock.Operations.AddLast(operation);

            return dest;
        }

        private Operand Add(Instruction inst, Operand dest, Operand source0, Operand source1)
        {
            NewNextBlockIfNeeded();

            Operation operation = Operation.Factory.Operation(inst, dest, source0, source1);

            _irBlock.Operations.AddLast(operation);

            return dest;
        }

        private Operand Add(Instruction inst, Operand dest, Operand source0, Operand source1, Operand source2)
        {
            NewNextBlockIfNeeded();

            Operation operation = Operation.Factory.Operation(inst, dest, source0, source1, source2);

            _irBlock.Operations.AddLast(operation);

            return dest;
        }

        public Operand AddIntrinsic(Intrinsic intrin, params ReadOnlySpan<Operand> args)
        {
            return Add(intrin, Local(OperandType.V128), args);
        }

        public Operand AddIntrinsicInt(Intrinsic intrin, params ReadOnlySpan<Operand> args)
        {
            return Add(intrin, Local(OperandType.I32), args);
        }

        public Operand AddIntrinsicLong(Intrinsic intrin, params ReadOnlySpan<Operand> args)
        {
            return Add(intrin, Local(OperandType.I64), args);
        }

        public void AddIntrinsicNoRet(Intrinsic intrin, params ReadOnlySpan<Operand> args)
        {
            Add(intrin, default, args);
        }

        private Operand Add(Intrinsic intrin, Operand dest, params ReadOnlySpan<Operand> sources)
        {
            NewNextBlockIfNeeded();

            Operation operation = Operation.Factory.Operation(intrin, dest, sources);

            _irBlock.Operations.AddLast(operation);

            return dest;
        }

        private void BranchToLabel(Operand label, bool uncond, BasicBlockFrequency nextFreq)
        {
            if (!_irLabels.TryGetValue(label, out BasicBlock branchBlock))
            {
                branchBlock = new BasicBlock();

                _irLabels.Add(label, branchBlock);
            }

            if (uncond)
            {
                _irBlock.AddSuccessor(branchBlock);
            }
            else
            {
                // Defer registration of successor to _irBlock so that the order of successors is correct.
                _ifBlock = branchBlock;
            }

            _needsNewBlock = true;
            _nextBlockFreq = nextFreq;
        }

        public void MarkLabel(Operand label, BasicBlockFrequency nextFreq = default)
        {
            _nextBlockFreq = nextFreq;

            if (_irLabels.TryGetValue(label, out BasicBlock nextBlock))
            {
                nextBlock.Index = _irBlocks.Count;

                _irBlocks.AddLast(nextBlock);

                NextBlock(nextBlock);
            }
            else
            {
                NewNextBlock();

                _irLabels.Add(label, _irBlock);
            }
        }

        private void NewNextBlock()
        {
            BasicBlock block = new(_irBlocks.Count);

            _irBlocks.AddLast(block);

            NextBlock(block);
        }

        private void NextBlock(BasicBlock nextBlock)
        {
            if (_irBlock?.SuccessorsCount == 0 && !EndsWithUnconditional(_irBlock))
            {
                _irBlock.AddSuccessor(nextBlock);

                if (_ifBlock != null)
                {
                    _irBlock.AddSuccessor(_ifBlock);

                    _ifBlock = null;
                }
            }

            _irBlock = nextBlock;
            _irBlock.Frequency = _nextBlockFreq;

            _needsNewBlock = false;
            _nextBlockFreq = BasicBlockFrequency.Default;
        }

        private static bool EndsWithUnconditional(BasicBlock block)
        {
            Operation last = block.Operations.Last;

            return last != default &&
               (last.Instruction == Instruction.Return ||
                last.Instruction == Instruction.Tailcall);
        }

        public ControlFlowGraph GetControlFlowGraph()
        {
            return new ControlFlowGraph(_irBlocks.First, _irBlocks, _localsCount);
        }
    }
}
