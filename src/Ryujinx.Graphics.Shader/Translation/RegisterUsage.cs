using Ryujinx.Graphics.Shader.Decoders;
using Ryujinx.Graphics.Shader.IntermediateRepresentation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;

namespace Ryujinx.Graphics.Shader.Translation
{
    static class RegisterUsage
    {
        private const int RegsCount = 256;
        private const int RegsMask = RegsCount - 1;

        private const int GprMasks = 4;
        private const int PredMasks = 1;
        private const int FlagMasks = 1;
        private const int TotalMasks = GprMasks + PredMasks + FlagMasks;

        private struct RegisterMask : IEquatable<RegisterMask>
        {
            public long GprMask0 { get; set; }
            public long GprMask1 { get; set; }
            public long GprMask2 { get; set; }
            public long GprMask3 { get; set; }
            public long PredMask { get; set; }
            public long FlagMask { get; set; }

            public RegisterMask(long gprMask0, long gprMask1, long gprMask2, long gprMask3, long predMask, long flagMask)
            {
                GprMask0 = gprMask0;
                GprMask1 = gprMask1;
                GprMask2 = gprMask2;
                GprMask3 = gprMask3;
                PredMask = predMask;
                FlagMask = flagMask;
            }

            public readonly long GetMask(int index)
            {
                return index switch
                {
                    0 => GprMask0,
                    1 => GprMask1,
                    2 => GprMask2,
                    3 => GprMask3,
                    4 => PredMask,
                    5 => FlagMask,
                    _ => throw new ArgumentOutOfRangeException(nameof(index)),
                };
            }

            public static RegisterMask operator &(RegisterMask x, RegisterMask y)
            {
                return new RegisterMask(
                    x.GprMask0 & y.GprMask0,
                    x.GprMask1 & y.GprMask1,
                    x.GprMask2 & y.GprMask2,
                    x.GprMask3 & y.GprMask3,
                    x.PredMask & y.PredMask,
                    x.FlagMask & y.FlagMask);
            }

            public static RegisterMask operator |(RegisterMask x, RegisterMask y)
            {
                return new RegisterMask(
                    x.GprMask0 | y.GprMask0,
                    x.GprMask1 | y.GprMask1,
                    x.GprMask2 | y.GprMask2,
                    x.GprMask3 | y.GprMask3,
                    x.PredMask | y.PredMask,
                    x.FlagMask | y.FlagMask);
            }

            public static RegisterMask operator ~(RegisterMask x)
            {
                return new RegisterMask(
                    ~x.GprMask0,
                    ~x.GprMask1,
                    ~x.GprMask2,
                    ~x.GprMask3,
                    ~x.PredMask,
                    ~x.FlagMask);
            }

            public static bool operator ==(RegisterMask x, RegisterMask y)
            {
                return x.Equals(y);
            }

            public static bool operator !=(RegisterMask x, RegisterMask y)
            {
                return !x.Equals(y);
            }

            public readonly override bool Equals(object obj)
            {
                return obj is RegisterMask regMask && Equals(regMask);
            }

            public readonly bool Equals(RegisterMask other)
            {
                return GprMask0 == other.GprMask0 &&
                       GprMask1 == other.GprMask1 &&
                       GprMask2 == other.GprMask2 &&
                       GprMask3 == other.GprMask3 &&
                       PredMask == other.PredMask &&
                       FlagMask == other.FlagMask;
            }

            public readonly override int GetHashCode()
            {
                return HashCode.Combine(GprMask0, GprMask1, GprMask2, GprMask3, PredMask, FlagMask);
            }
        }

        public readonly struct FunctionRegisterUsage
        {
            public Register[] InArguments { get; }
            public Register[] OutArguments { get; }

            public FunctionRegisterUsage(Register[] inArguments, Register[] outArguments)
            {
                InArguments = inArguments;
                OutArguments = outArguments;
            }
        }

        public static FunctionRegisterUsage RunPass(ControlFlowGraph cfg)
        {
            List<Register> inArguments = [];
            List<Register> outArguments = [];

            // Compute local register inputs and outputs used inside blocks.
            RegisterMask[] localInputs = new RegisterMask[cfg.Blocks.Length];
            RegisterMask[] localOutputs = new RegisterMask[cfg.Blocks.Length];

            foreach (BasicBlock block in cfg.Blocks)
            {
                for (LinkedListNode<INode> node = block.Operations.First; node != null; node = node.Next)
                {
                    Operation operation = node.Value as Operation;

                    for (int srcIndex = 0; srcIndex < operation.SourcesCount; srcIndex++)
                    {
                        Operand source = operation.GetSource(srcIndex);

                        if (source.Type != OperandType.Register)
                        {
                            continue;
                        }

                        Register register = source.GetRegister();

                        localInputs[block.Index] |= GetMask(register) & ~localOutputs[block.Index];
                    }

                    for (int dstIndex = 0; dstIndex < operation.DestsCount; dstIndex++)
                    {
                        Operand dest = operation.GetDest(dstIndex);

                        if (dest != null && dest.Type == OperandType.Register)
                        {
                            localOutputs[block.Index] |= GetMask(dest.GetRegister());
                        }
                    }
                }
            }

            // Compute global register inputs and outputs used across blocks.
            RegisterMask[] globalCmnOutputs = new RegisterMask[cfg.Blocks.Length];

            RegisterMask[] globalInputs = new RegisterMask[cfg.Blocks.Length];
            RegisterMask[] globalOutputs = new RegisterMask[cfg.Blocks.Length];

            RegisterMask allOutputs = new();
            RegisterMask allCmnOutputs = new(-1L, -1L, -1L, -1L, -1L, -1L);

            bool modified;

            bool firstPass = true;

            do
            {
                modified = false;

                // Compute register outputs.
                for (int index = cfg.PostOrderBlocks.Length - 1; index >= 0; index--)
                {
                    BasicBlock block = cfg.PostOrderBlocks[index];

                    if (block.Predecessors.Count != 0)
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

                        if (EndsWithReturn(block))
                        {
                            allCmnOutputs &= cmnOutputs | localOutputs[block.Index];
                        }

                        if (Exchange(globalCmnOutputs, block.Index, cmnOutputs))
                        {
                            modified = true;
                        }

                        outputs |= localOutputs[block.Index];

                        if (Exchange(globalOutputs, block.Index, globalOutputs[block.Index] | outputs))
                        {
                            allOutputs |= outputs;
                            modified = true;
                        }
                    }
                    else if (Exchange(globalOutputs, block.Index, localOutputs[block.Index]))
                    {
                        allOutputs |= localOutputs[block.Index];
                        modified = true;
                    }
                }

                // Compute register inputs.
                for (int index = 0; index < cfg.PostOrderBlocks.Length; index++)
                {
                    BasicBlock block = cfg.PostOrderBlocks[index];

                    RegisterMask inputs = localInputs[block.Index];

                    if (block.Next != null)
                    {
                        inputs |= globalInputs[block.Next.Index];
                    }

                    if (block.Branch != null)
                    {
                        inputs |= globalInputs[block.Branch.Index];
                    }

                    inputs &= ~globalCmnOutputs[block.Index];

                    if (Exchange(globalInputs, block.Index, globalInputs[block.Index] | inputs))
                    {
                        modified = true;
                    }
                }

                firstPass = false;
            }
            while (modified);

            // Insert load and store context instructions where needed.
            foreach (BasicBlock block in cfg.Blocks)
            {
                // The only block without any predecessor should be the entry block.
                // It always needs a context load as it is the first block to run.
                if (block.Predecessors.Count == 0)
                {
                    RegisterMask inputs = globalInputs[block.Index] | (allOutputs & ~allCmnOutputs);

                    LoadLocals(block, inputs, inArguments);
                }

                if (EndsWithReturn(block))
                {
                    StoreLocals(block, allOutputs, inArguments.Count, outArguments);
                }
            }

            return new FunctionRegisterUsage(inArguments.ToArray(), outArguments.ToArray());
        }

        public static void FixupCalls(BasicBlock[] blocks, FunctionRegisterUsage[] frus)
        {
            foreach (BasicBlock block in blocks)
            {
                for (LinkedListNode<INode> node = block.Operations.First; node != null; node = node.Next)
                {
                    Operation operation = node.Value as Operation;

                    if (operation.Inst == Instruction.Call)
                    {
                        Operand funcId = operation.GetSource(0);

                        Debug.Assert(funcId.Type == OperandType.Constant);

                        FunctionRegisterUsage fru = frus[funcId.Value];

                        Operand[] inRegs = new Operand[fru.InArguments.Length];

                        for (int i = 0; i < fru.InArguments.Length; i++)
                        {
                            inRegs[i] = OperandHelper.Register(fru.InArguments[i]);
                        }

                        operation.AppendSources(inRegs);

                        Operand[] outRegs = new Operand[1 + fru.OutArguments.Length];

                        for (int i = 0; i < fru.OutArguments.Length; i++)
                        {
                            outRegs[1 + i] = OperandHelper.Register(fru.OutArguments[i]);
                        }

                        operation.AppendDests(outRegs);
                    }
                }
            }
        }

        private static bool StartsWith(BasicBlock block, Instruction inst)
        {
            if (block.Operations.Count == 0)
            {
                return false;
            }

            return block.Operations.First.Value is Operation operation && operation.Inst == inst;
        }

        private static bool EndsWith(BasicBlock block, Instruction inst)
        {
            if (block.Operations.Count == 0)
            {
                return false;
            }

            return block.Operations.Last.Value is Operation operation && operation.Inst == inst;
        }

        private static RegisterMask GetMask(Register register)
        {
            Span<long> gprMasks = stackalloc long[4];
            long predMask = 0;
            long flagMask = 0;

            switch (register.Type)
            {
                case RegisterType.Gpr:
                    gprMasks[register.Index >> 6] = 1L << (register.Index & 0x3f);
                    break;
                case RegisterType.Predicate:
                    predMask = 1L << register.Index;
                    break;
                case RegisterType.Flag:
                    flagMask = 1L << register.Index;
                    break;
            }

            return new RegisterMask(gprMasks[0], gprMasks[1], gprMasks[2], gprMasks[3], predMask, flagMask);
        }

        private static bool Exchange(RegisterMask[] masks, int blkIndex, RegisterMask value)
        {
            RegisterMask oldValue = masks[blkIndex];

            masks[blkIndex] = value;

            return oldValue != value;
        }

        private static void LoadLocals(BasicBlock block, RegisterMask masks, List<Register> inArguments)
        {
            bool fillArgsList = inArguments.Count == 0;
            LinkedListNode<INode> node = null;
            int argIndex = 0;

            for (int i = 0; i < TotalMasks; i++)
            {
                (RegisterType regType, int baseRegIndex) = GetRegTypeAndBaseIndex(i);
                long mask = masks.GetMask(i);

                while (mask != 0)
                {
                    int bit = BitOperations.TrailingZeroCount(mask);

                    mask &= ~(1L << bit);

                    Register register = new(baseRegIndex + bit, regType);

                    if (fillArgsList)
                    {
                        inArguments.Add(register);
                    }

                    Operation copyOp = new(Instruction.Copy, OperandHelper.Register(register), OperandHelper.Argument(argIndex++));

                    if (node == null)
                    {
                        node = block.Operations.AddFirst(copyOp);
                    }
                    else
                    {
                        node = block.Operations.AddAfter(node, copyOp);
                    }
                }
            }

            Debug.Assert(argIndex <= inArguments.Count);
        }

        private static void StoreLocals(BasicBlock block, RegisterMask masks, int inArgumentsCount, List<Register> outArguments)
        {
            LinkedListNode<INode> node = null;
            int argIndex = inArgumentsCount;
            bool fillArgsList = outArguments.Count == 0;

            for (int i = 0; i < TotalMasks; i++)
            {
                (RegisterType regType, int baseRegIndex) = GetRegTypeAndBaseIndex(i);
                long mask = masks.GetMask(i);

                while (mask != 0)
                {
                    int bit = BitOperations.TrailingZeroCount(mask);

                    mask &= ~(1L << bit);

                    Register register = new(baseRegIndex + bit, regType);

                    if (fillArgsList)
                    {
                        outArguments.Add(register);
                    }

                    Operation copyOp = new(Instruction.Copy, OperandHelper.Argument(argIndex++), OperandHelper.Register(register));

                    if (node == null)
                    {
                        node = block.Operations.AddBefore(block.Operations.Last, copyOp);
                    }
                    else
                    {
                        node = block.Operations.AddAfter(node, copyOp);
                    }
                }
            }

            Debug.Assert(argIndex <= inArgumentsCount + outArguments.Count);
        }

        private static (RegisterType RegType, int BaseRegIndex) GetRegTypeAndBaseIndex(int i)
        {
            RegisterType regType = RegisterType.Gpr;
            int baseRegIndex = 0;

            if (i < GprMasks)
            {
                baseRegIndex = i * sizeof(long) * 8;
            }
            else if (i == GprMasks)
            {
                regType = RegisterType.Predicate;
            }
            else
            {
                regType = RegisterType.Flag;
            }

            return (regType, baseRegIndex);
        }

        private static bool EndsWithReturn(BasicBlock block)
        {
            if (block.GetLastOp() is not Operation operation)
            {
                return false;
            }

            return operation.Inst == Instruction.Return;
        }
    }
}
