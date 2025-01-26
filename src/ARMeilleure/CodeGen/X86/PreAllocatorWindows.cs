using ARMeilleure.CodeGen.RegisterAllocators;
using ARMeilleure.IntermediateRepresentation;
using ARMeilleure.Translation;
using System;
using System.Diagnostics;
using static ARMeilleure.IntermediateRepresentation.Operand.Factory;
using static ARMeilleure.IntermediateRepresentation.Operation.Factory;

namespace ARMeilleure.CodeGen.X86
{
    class PreAllocatorWindows : PreAllocator
    {
        public static void InsertCallCopies(IntrusiveList<Operation> nodes, StackAllocator stackAlloc, Operation node)
        {
            Operand dest = node.Destination;

            // Handle struct arguments.
            int retArgs = 0;
            int stackAllocOffset = 0;

            int AllocateOnStack(int size)
            {
                // We assume that the stack allocator is initially empty (TotalSize = 0).
                // Taking that into account, we can reuse the space allocated for other
                // calls by keeping track of our own allocated size (stackAllocOffset).
                // If the space allocated is not big enough, then we just expand it.
                int offset = stackAllocOffset;

                if (stackAllocOffset + size > stackAlloc.TotalSize)
                {
                    stackAlloc.Allocate((stackAllocOffset + size) - stackAlloc.TotalSize);
                }

                stackAllocOffset += size;

                return offset;
            }

            Operand arg0Reg = default;

            if (dest != default && dest.Type == OperandType.V128)
            {
                int stackOffset = AllocateOnStack(dest.Type.GetSizeInBytes());

                arg0Reg = Gpr(CallingConvention.GetIntArgumentRegister(0), OperandType.I64);

                Operation allocOp = Operation(Instruction.StackAlloc, arg0Reg, Const(stackOffset));

                nodes.AddBefore(node, allocOp);

                retArgs = 1;
            }

            int argsCount = node.SourcesCount - 1;
            int maxArgs = CallingConvention.GetArgumentsOnRegsCount() - retArgs;

            if (argsCount > maxArgs)
            {
                argsCount = maxArgs;
            }

            Operand[] sources = new Operand[1 + retArgs + argsCount];

            sources[0] = node.GetSource(0);

            if (arg0Reg != default)
            {
                sources[1] = arg0Reg;
            }

            for (int index = 1; index < node.SourcesCount; index++)
            {
                Operand source = node.GetSource(index);

                if (source.Type == OperandType.V128)
                {
                    Operand stackAddr = Local(OperandType.I64);

                    int stackOffset = AllocateOnStack(source.Type.GetSizeInBytes());

                    nodes.AddBefore(node, Operation(Instruction.StackAlloc, stackAddr, Const(stackOffset)));

                    Operation storeOp = Operation(Instruction.Store, default, stackAddr, source);

                    InsertConstantRegCopies(nodes, nodes.AddBefore(node, storeOp));

                    node.SetSource(index, stackAddr);
                }
            }

            // Handle arguments passed on registers.
            for (int index = 0; index < argsCount; index++)
            {
                Operand source = node.GetSource(index + 1);
                Operand argReg;

                int argIndex = index + retArgs;

                if (source.Type.IsInteger())
                {
                    argReg = Gpr(CallingConvention.GetIntArgumentRegister(argIndex), source.Type);
                }
                else
                {
                    argReg = Xmm(CallingConvention.GetVecArgumentRegister(argIndex), source.Type);
                }

                Operation copyOp = Operation(Instruction.Copy, argReg, source);

                InsertConstantRegCopies(nodes, nodes.AddBefore(node, copyOp));

                sources[1 + retArgs + index] = argReg;
            }

            // The remaining arguments (those that are not passed on registers)
            // should be passed on the stack, we write them to the stack with "SpillArg".
            for (int index = argsCount; index < node.SourcesCount - 1; index++)
            {
                Operand source = node.GetSource(index + 1);
                Operand offset = Const((index + retArgs) * 8);

                Operation spillOp = Operation(Instruction.SpillArg, default, offset, source);

                InsertConstantRegCopies(nodes, nodes.AddBefore(node, spillOp));
            }

            if (dest != default)
            {
                if (dest.Type == OperandType.V128)
                {
                    Operand retValueAddr = Local(OperandType.I64);

                    nodes.AddBefore(node, Operation(Instruction.Copy, retValueAddr, arg0Reg));

                    Operation loadOp = Operation(Instruction.Load, dest, retValueAddr);

                    nodes.AddAfter(node, loadOp);

                    node.Destination = default;
                }
                else
                {
                    Operand retReg = dest.Type.IsInteger()
                        ? Gpr(CallingConvention.GetIntReturnRegister(), dest.Type)
                        : Xmm(CallingConvention.GetVecReturnRegister(), dest.Type);

                    Operation copyOp = Operation(Instruction.Copy, dest, retReg);

                    nodes.AddAfter(node, copyOp);

                    node.Destination = retReg;
                }
            }

            node.SetSources(sources);
        }

        public static void InsertTailcallCopies(IntrusiveList<Operation> nodes, Operation node)
        {
            int argsCount = node.SourcesCount - 1;
            int maxArgs = CallingConvention.GetArgumentsOnRegsCount();

            if (argsCount > maxArgs)
            {
                throw new NotImplementedException("Spilling is not currently supported for tail calls. (too many arguments)");
            }

            Operand[] sources = new Operand[1 + argsCount];

            // Handle arguments passed on registers.
            for (int index = 0; index < argsCount; index++)
            {
                Operand source = node.GetSource(1 + index);
                Operand argReg = source.Type.IsInteger()
                    ? Gpr(CallingConvention.GetIntArgumentRegister(index), source.Type)
                    : Xmm(CallingConvention.GetVecArgumentRegister(index), source.Type);

                Operation copyOp = Operation(Instruction.Copy, argReg, source);

                InsertConstantRegCopies(nodes, nodes.AddBefore(node, copyOp));

                sources[1 + index] = argReg;
            }

            // The target address must be on the return registers, since we
            // don't return anything and it is guaranteed to not be a
            // callee saved register (which would be trashed on the epilogue).
            Operand retReg = Gpr(CallingConvention.GetIntReturnRegister(), OperandType.I64);

            Operation addrCopyOp = Operation(Instruction.Copy, retReg, node.GetSource(0));

            nodes.AddBefore(node, addrCopyOp);

            sources[0] = retReg;

            node.SetSources(sources);
        }

        public static Operation InsertLoadArgumentCopy(
            CompilerContext cctx,
            ref Span<Operation> buffer,
            IntrusiveList<Operation> nodes,
            Operand[] preservedArgs,
            Operation node)
        {
            Operand source = node.GetSource(0);

            Debug.Assert(source.Kind == OperandKind.Constant, "Non-constant LoadArgument source kind.");

            int retArgs = cctx.FuncReturnType == OperandType.V128 ? 1 : 0;

            int index = source.AsInt32() + retArgs;

            if (index < CallingConvention.GetArgumentsOnRegsCount())
            {
                Operand dest = node.Destination;

                if (preservedArgs[index] == default)
                {
                    Operand argReg, pArg;

                    if (dest.Type.IsInteger())
                    {
                        argReg = Gpr(CallingConvention.GetIntArgumentRegister(index), dest.Type);
                        pArg = Local(dest.Type);
                    }
                    else if (dest.Type == OperandType.V128)
                    {
                        argReg = Gpr(CallingConvention.GetIntArgumentRegister(index), OperandType.I64);
                        pArg = Local(OperandType.I64);
                    }
                    else
                    {
                        argReg = Xmm(CallingConvention.GetVecArgumentRegister(index), dest.Type);
                        pArg = Local(dest.Type);
                    }

                    Operation copyOp = Operation(Instruction.Copy, pArg, argReg);

                    cctx.Cfg.Entry.Operations.AddFirst(copyOp);

                    preservedArgs[index] = pArg;
                }

                Operation nextNode;

                if (dest.Type != OperandType.V128 && dest.AssignmentsCount == 1)
                {
                    // Let's propagate the argument if we can to avoid copies.
                    PreAllocatorCommon.Propagate(ref buffer, dest, preservedArgs[index]);
                    nextNode = node.ListNext;
                }
                else
                {
                    Operation argCopyOp = Operation(dest.Type == OperandType.V128
                        ? Instruction.Load
                        : Instruction.Copy, dest, preservedArgs[index]);

                    nextNode = nodes.AddBefore(node, argCopyOp);
                }

                Delete(nodes, node);
                return nextNode;
            }
            else
            {
                // TODO: Pass on stack.
                return node;
            }
        }

        public static void InsertReturnCopy(
            CompilerContext cctx,
            IntrusiveList<Operation> nodes,
            Operand[] preservedArgs,
            Operation node)
        {
            if (node.SourcesCount == 0)
            {
                return;
            }

            Operand source = node.GetSource(0);
            Operand retReg;

            if (source.Type.IsInteger())
            {
                retReg = Gpr(CallingConvention.GetIntReturnRegister(), source.Type);
            }
            else if (source.Type == OperandType.V128)
            {
                if (preservedArgs[0] == default)
                {
                    Operand preservedArg = Local(OperandType.I64);
                    Operand arg0 = Gpr(CallingConvention.GetIntArgumentRegister(0), OperandType.I64);

                    Operation copyOp = Operation(Instruction.Copy, preservedArg, arg0);

                    cctx.Cfg.Entry.Operations.AddFirst(copyOp);

                    preservedArgs[0] = preservedArg;
                }

                retReg = preservedArgs[0];
            }
            else
            {
                retReg = Xmm(CallingConvention.GetVecReturnRegister(), source.Type);
            }

            if (source.Type == OperandType.V128)
            {
                Operation retStoreOp = Operation(Instruction.Store, default, retReg, source);

                nodes.AddBefore(node, retStoreOp);
            }
            else
            {
                Operation retCopyOp = Operation(Instruction.Copy, retReg, source);

                nodes.AddBefore(node, retCopyOp);
            }

            node.SetSources([]);
        }
    }
}
