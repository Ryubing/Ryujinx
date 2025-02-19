using ARMeilleure.IntermediateRepresentation;
using ARMeilleure.State;
using System;
using System.Numerics;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using static ARMeilleure.IntermediateRepresentation.Operand.Factory;
using static ARMeilleure.IntermediateRepresentation.Operation.Factory;

namespace ARMeilleure.Translation
{
    static class RegisterUsage
    {
        private const int RegsCount = 32;
        private const int RegsMask = RegsCount - 1;

        private readonly struct RegisterMask : IEquatable<RegisterMask>
        {
            public long IntMask => Mask.GetElement(0);
            public long VecMask => Mask.GetElement(1);

            public Vector128<long> Mask { get; }

            public RegisterMask(Vector128<long> mask)
            {
                Mask = mask;
            }

            public RegisterMask(long intMask, long vecMask)
            {
                Mask = Vector128.Create(intMask, vecMask);
            }

            public static RegisterMask operator &(RegisterMask x, RegisterMask y)
            {
                if (Sse2.IsSupported)
                {
                    return new RegisterMask(Sse2.And(x.Mask, y.Mask));
                }

                return new RegisterMask(x.IntMask & y.IntMask, x.VecMask & y.VecMask);
            }

            public static RegisterMask operator |(RegisterMask x, RegisterMask y)
            {
                if (Sse2.IsSupported)
                {
                    return new RegisterMask(Sse2.Or(x.Mask, y.Mask));
                }

                return new RegisterMask(x.IntMask | y.IntMask, x.VecMask | y.VecMask);
            }

            public static RegisterMask operator ~(RegisterMask x)
            {
                if (Sse2.IsSupported)
                {
                    return new RegisterMask(Sse2.AndNot(x.Mask, Vector128<long>.AllBitsSet));
                }

                return new RegisterMask(~x.IntMask, ~x.VecMask);
            }

            public static bool operator ==(RegisterMask x, RegisterMask y)
            {
                return x.Equals(y);
            }

            public static bool operator !=(RegisterMask x, RegisterMask y)
            {
                return !x.Equals(y);
            }

            public override bool Equals(object obj)
            {
                return obj is RegisterMask regMask && Equals(regMask);
            }

            public bool Equals(RegisterMask other)
            {
                return Mask.Equals(other.Mask);
            }

            public override int GetHashCode()
            {
                return Mask.GetHashCode();
            }
        }

        public static void RunPass(ControlFlowGraph cfg, ExecutionMode mode)
        {
            if (cfg.Entry.Predecessors.Count != 0)
            {
                // We expect the entry block to have no predecessors.
                // This is required because we have a implicit context load at the start of the function,
                // but if there is a jump to the start of the function, the context load would trash the modified values.
                // Here we insert a new entry block that will jump to the existing entry block.
                BasicBlock newEntry = new(cfg.Blocks.Count);

                cfg.UpdateEntry(newEntry);
            }

            // Compute local register inputs and outputs used inside blocks.
            RegisterMask[] localInputs = new RegisterMask[cfg.Blocks.Count];
            RegisterMask[] localOutputs = new RegisterMask[cfg.Blocks.Count];

            for (BasicBlock block = cfg.Blocks.First; block != null; block = block.ListNext)
            {
                for (Operation node = block.Operations.First; node != default; node = node.ListNext)
                {
                    for (int index = 0; index < node.SourcesCount; index++)
                    {
                        Operand source = node.GetSource(index);

                        if (source.Kind == OperandKind.Register)
                        {
                            Register register = source.GetRegister();

                            localInputs[block.Index] |= GetMask(register) & ~localOutputs[block.Index];
                        }
                    }

                    if (node.Destination != default && node.Destination.Kind == OperandKind.Register)
                    {
                        localOutputs[block.Index] |= GetMask(node.Destination.GetRegister());
                    }
                }
            }

            // Compute global register inputs and outputs used across blocks.
            RegisterMask[] globalCmnOutputs = new RegisterMask[cfg.Blocks.Count];

            RegisterMask[] globalInputs = new RegisterMask[cfg.Blocks.Count];
            RegisterMask[] globalOutputs = new RegisterMask[cfg.Blocks.Count];

            bool modified;
            bool firstPass = true;

            do
            {
                modified = false;

                // Compute register outputs.
                for (int index = cfg.PostOrderBlocks.Length - 1; index >= 0; index--)
                {
                    BasicBlock block = cfg.PostOrderBlocks[index];

                    if (block.Predecessors.Count != 0 && !HasContextLoad(block))
                    {
                        BasicBlock predecessor = block.Predecessors[0];

                        RegisterMask cmnOutputs = localOutputs[predecessor.Index] | globalCmnOutputs[predecessor.Index];
                        RegisterMask outputs = globalOutputs[predecessor.Index];

                        for (int pIndex = 1; pIndex < block.Predecessors.Count; pIndex++)
                        {
                            predecessor = block.Predecessors[pIndex];

                            cmnOutputs &= localOutputs[predecessor.Index] | globalCmnOutputs[predecessor.Index];
                            outputs |= globalOutputs[predecessor.Index];
                        }

                        globalInputs[block.Index] |= outputs & ~cmnOutputs;

                        if (!firstPass)
                        {
                            cmnOutputs &= globalCmnOutputs[block.Index];
                        }

                        modified |= Exchange(globalCmnOutputs, block.Index, cmnOutputs);
                        outputs |= localOutputs[block.Index];
                        modified |= Exchange(globalOutputs, block.Index, globalOutputs[block.Index] | outputs);
                    }
                    else
                    {
                        modified |= Exchange(globalOutputs, block.Index, localOutputs[block.Index]);
                    }
                }

                // Compute register inputs.
                for (int index = 0; index < cfg.PostOrderBlocks.Length; index++)
                {
                    BasicBlock block = cfg.PostOrderBlocks[index];

                    RegisterMask inputs = localInputs[block.Index];

                    for (int i = 0; i < block.SuccessorsCount; i++)
                    {
                        inputs |= globalInputs[block.GetSuccessor(i).Index];
                    }

                    inputs &= ~globalCmnOutputs[block.Index];

                    modified |= Exchange(globalInputs, block.Index, globalInputs[block.Index] | inputs);
                }

                firstPass = false;
            }
            while (modified);

            // Insert load and store context instructions where needed.
            for (BasicBlock block = cfg.Blocks.First; block != null; block = block.ListNext)
            {
                bool hasContextLoad = HasContextLoad(block);

                if (hasContextLoad)
                {
                    block.Operations.Remove(block.Operations.First);
                }

                Operand arg = default;

                // The only block without any predecessor should be the entry block.
                // It always needs a context load as it is the first block to run.
                if (block == cfg.Entry || hasContextLoad)
                {
                    long vecMask = globalInputs[block.Index].VecMask;
                    long intMask = globalInputs[block.Index].IntMask;

                    if (vecMask != 0 || intMask != 0)
                    {
                        arg = Local(OperandType.I64);

                        Operation loadArg = block.Operations.AddFirst(Operation(Instruction.LoadArgument, arg, Const(0)));

                        LoadLocals(block, vecMask, RegisterType.Vector, mode, loadArg, arg);
                        LoadLocals(block, intMask, RegisterType.Integer, mode, loadArg, arg);
                    }
                }

                bool hasContextStore = HasContextStore(block);

                if (hasContextStore)
                {
                    block.Operations.Remove(block.Operations.Last);
                }

                if (EndsWithReturn(block) || hasContextStore)
                {
                    long vecMask = globalOutputs[block.Index].VecMask;
                    long intMask = globalOutputs[block.Index].IntMask;

                    if (vecMask != 0 || intMask != 0)
                    {
                        if (arg == default)
                        {
                            arg = Local(OperandType.I64);

                            block.Append(Operation(Instruction.LoadArgument, arg, Const(0)));
                        }

                        StoreLocals(block, intMask, RegisterType.Integer, mode, arg);
                        StoreLocals(block, vecMask, RegisterType.Vector, mode, arg);
                    }
                }
            }
        }

        private static bool HasContextLoad(BasicBlock block)
        {
            return StartsWith(block, Instruction.LoadFromContext) && block.Operations.First.SourcesCount == 0;
        }

        private static bool HasContextStore(BasicBlock block)
        {
            return EndsWith(block, Instruction.StoreToContext) && block.Operations.Last.SourcesCount == 0;
        }

        private static bool StartsWith(BasicBlock block, Instruction inst)
        {
            if (block.Operations.Count > 0)
            {
                Operation first = block.Operations.First;

                return first != default && first.Instruction == inst;
            }

            return false;
        }

        private static bool EndsWith(BasicBlock block, Instruction inst)
        {
            if (block.Operations.Count > 0)
            {
                Operation last = block.Operations.Last;

                return last != default && last.Instruction == inst;
            }

            return false;
        }

        private static RegisterMask GetMask(Register register)
        {
            long intMask = 0;
            long vecMask = 0;

            switch (register.Type)
            {
#pragma warning disable IDE0055 // Disable formatting
                case RegisterType.Flag:    intMask = (1L << RegsCount) << register.Index; break;
                case RegisterType.Integer: intMask =  1L               << register.Index; break;
                case RegisterType.FpFlag:  vecMask = (1L << RegsCount) << register.Index; break;
                case RegisterType.Vector:  vecMask =  1L               << register.Index; break;
#pragma warning restore IDE0055
            }

            return new RegisterMask(intMask, vecMask);
        }

        private static bool Exchange(RegisterMask[] masks, int blkIndex, RegisterMask value)
        {
            ref RegisterMask curValue = ref masks[blkIndex];

            bool changed = curValue != value;

            curValue = value;

            return changed;
        }

        private static void LoadLocals(
            BasicBlock block,
            long inputs,
            RegisterType baseType,
            ExecutionMode mode,
            Operation loadArg,
            Operand arg)
        {
            while (inputs != 0)
            {
                int bit = 63 - BitOperations.LeadingZeroCount((ulong)inputs);

                Operand dest = GetRegFromBit(bit, baseType, mode);
                Operand offset = Const((long)NativeContext.GetRegisterOffset(dest.GetRegister()));
                Operand addr = Local(OperandType.I64);

                block.Operations.AddAfter(loadArg, Operation(Instruction.Load, dest, addr));
                block.Operations.AddAfter(loadArg, Operation(Instruction.Add, addr, arg, offset));

                inputs &= ~(1L << bit);
            }
        }

        private static void StoreLocals(
            BasicBlock block,
            long outputs,
            RegisterType baseType,
            ExecutionMode mode,
            Operand arg)
        {
            while (outputs != 0)
            {
                int bit = BitOperations.TrailingZeroCount(outputs);

                Operand source = GetRegFromBit(bit, baseType, mode);
                Operand offset = Const((long)NativeContext.GetRegisterOffset(source.GetRegister()));
                Operand addr = Local(OperandType.I64);

                block.Append(Operation(Instruction.Add, addr, arg, offset));
                block.Append(Operation(Instruction.Store, default, addr, source));

                outputs &= ~(1L << bit);
            }
        }

        private static Operand GetRegFromBit(int bit, RegisterType baseType, ExecutionMode mode)
        {
            if (bit < RegsCount)
            {
                return Register(bit, baseType, GetOperandType(baseType, mode));
            }
            else if (baseType == RegisterType.Integer)
            {
                return Register(bit & RegsMask, RegisterType.Flag, OperandType.I32);
            }
            else if (baseType == RegisterType.Vector)
            {
                return Register(bit & RegsMask, RegisterType.FpFlag, OperandType.I32);
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(bit));
            }
        }

        private static OperandType GetOperandType(RegisterType type, ExecutionMode mode)
        {
            return type switch
            {
                RegisterType.Flag => OperandType.I32,
                RegisterType.FpFlag => OperandType.I32,
                RegisterType.Integer => (mode == ExecutionMode.Aarch64) ? OperandType.I64 : OperandType.I32,
                RegisterType.Vector => OperandType.V128,
                _ => throw new ArgumentException($"Invalid register type \"{type}\"."),
            };
        }

        private static bool EndsWithReturn(BasicBlock block)
        {
            Operation last = block.Operations.Last;

            return last != default && last.Instruction == Instruction.Return;
        }
    }
}
