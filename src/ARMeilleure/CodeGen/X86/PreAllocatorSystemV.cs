using ARMeilleure.IntermediateRepresentation;
using ARMeilleure.Translation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using static ARMeilleure.IntermediateRepresentation.Operand.Factory;
using static ARMeilleure.IntermediateRepresentation.Operation.Factory;

namespace ARMeilleure.CodeGen.X86
{
    class PreAllocatorSystemV : PreAllocator
    {
        public static void InsertCallCopies(IntrusiveList<Operation> nodes, Operation node)
        {
            Operand dest = node.Destination;

            List<Operand> sources =
            [
                node.GetSource(0)
            ];

            int argsCount = node.SourcesCount - 1;

            int intMax = CallingConvention.GetIntArgumentsOnRegsCount();
            int vecMax = CallingConvention.GetVecArgumentsOnRegsCount();

            int intCount = 0;
            int vecCount = 0;

            int stackOffset = 0;

            for (int index = 0; index < argsCount; index++)
            {
                Operand source = node.GetSource(index + 1);

                bool passOnReg;

                if (source.Type.IsInteger())
                {
                    passOnReg = intCount < intMax;
                }
                else if (source.Type == OperandType.V128)
                {
                    passOnReg = intCount + 1 < intMax;
                }
                else
                {
                    passOnReg = vecCount < vecMax;
                }

                if (source.Type == OperandType.V128 && passOnReg)
                {
                    // V128 is a struct, we pass each half on a GPR if possible.
                    Operand argReg = Gpr(CallingConvention.GetIntArgumentRegister(intCount++), OperandType.I64);
                    Operand argReg2 = Gpr(CallingConvention.GetIntArgumentRegister(intCount++), OperandType.I64);

                    nodes.AddBefore(node, Operation(Instruction.VectorExtract, argReg, source, Const(0)));
                    nodes.AddBefore(node, Operation(Instruction.VectorExtract, argReg2, source, Const(1)));

                    continue;
                }

                if (passOnReg)
                {
                    Operand argReg = source.Type.IsInteger()
                        ? Gpr(CallingConvention.GetIntArgumentRegister(intCount++), source.Type)
                        : Xmm(CallingConvention.GetVecArgumentRegister(vecCount++), source.Type);

                    Operation copyOp = Operation(Instruction.Copy, argReg, source);

                    InsertConstantRegCopies(nodes, nodes.AddBefore(node, copyOp));

                    sources.Add(argReg);
                }
                else
                {
                    Operand offset = Const(stackOffset);

                    Operation spillOp = Operation(Instruction.SpillArg, default, offset, source);

                    InsertConstantRegCopies(nodes, nodes.AddBefore(node, spillOp));

                    stackOffset += source.Type.GetSizeInBytes();
                }
            }

            node.SetSources(sources.ToArray());

            if (dest != default)
            {
                if (dest.Type == OperandType.V128)
                {
                    Operand retLReg = Gpr(CallingConvention.GetIntReturnRegister(), OperandType.I64);
                    Operand retHReg = Gpr(CallingConvention.GetIntReturnRegisterHigh(), OperandType.I64);

                    Operation operation = node;

                    node = nodes.AddAfter(node, Operation(Instruction.VectorCreateScalar, dest, retLReg));
                    nodes.AddAfter(node, Operation(Instruction.VectorInsert, dest, dest, retHReg, Const(1)));

                    operation.Destination = default;
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
        }

        public static void InsertTailcallCopies(IntrusiveList<Operation> nodes, Operation node)
        {
            List<Operand> sources =
            [
                node.GetSource(0)
            ];

            int argsCount = node.SourcesCount - 1;

            int intMax = CallingConvention.GetIntArgumentsOnRegsCount();
            int vecMax = CallingConvention.GetVecArgumentsOnRegsCount();

            int intCount = 0;
            int vecCount = 0;

            // Handle arguments passed on registers.
            for (int index = 0; index < argsCount; index++)
            {
                Operand source = node.GetSource(1 + index);

                bool passOnReg;

                if (source.Type.IsInteger())
                {
                    passOnReg = intCount + 1 < intMax;
                }
                else
                {
                    passOnReg = vecCount < vecMax;
                }

                if (source.Type == OperandType.V128 && passOnReg)
                {
                    // V128 is a struct, we pass each half on a GPR if possible.
                    Operand argReg = Gpr(CallingConvention.GetIntArgumentRegister(intCount++), OperandType.I64);
                    Operand argReg2 = Gpr(CallingConvention.GetIntArgumentRegister(intCount++), OperandType.I64);

                    nodes.AddBefore(node, Operation(Instruction.VectorExtract, argReg, source, Const(0)));
                    nodes.AddBefore(node, Operation(Instruction.VectorExtract, argReg2, source, Const(1)));

                    continue;
                }

                if (passOnReg)
                {
                    Operand argReg = source.Type.IsInteger()
                        ? Gpr(CallingConvention.GetIntArgumentRegister(intCount++), source.Type)
                        : Xmm(CallingConvention.GetVecArgumentRegister(vecCount++), source.Type);

                    Operation copyOp = Operation(Instruction.Copy, argReg, source);

                    InsertConstantRegCopies(nodes, nodes.AddBefore(node, copyOp));

                    sources.Add(argReg);
                }
                else
                {
                    throw new NotImplementedException("Spilling is not currently supported for tail calls. (too many arguments)");
                }
            }

            // The target address must be on the return registers, since we
            // don't return anything and it is guaranteed to not be a
            // callee saved register (which would be trashed on the epilogue).
            Operand retReg = Gpr(CallingConvention.GetIntReturnRegister(), OperandType.I64);

            Operation addrCopyOp = Operation(Instruction.Copy, retReg, node.GetSource(0));

            nodes.AddBefore(node, addrCopyOp);

            sources[0] = retReg;

            node.SetSources(sources.ToArray());
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

            int index = source.AsInt32();

            int intCount = 0;
            int vecCount = 0;

            for (int cIndex = 0; cIndex < index; cIndex++)
            {
                OperandType argType = cctx.FuncArgTypes[cIndex];

                if (argType.IsInteger())
                {
                    intCount++;
                }
                else if (argType == OperandType.V128)
                {
                    intCount += 2;
                }
                else
                {
                    vecCount++;
                }
            }

            bool passOnReg;

            if (source.Type.IsInteger())
            {
                passOnReg = intCount < CallingConvention.GetIntArgumentsOnRegsCount();
            }
            else if (source.Type == OperandType.V128)
            {
                passOnReg = intCount + 1 < CallingConvention.GetIntArgumentsOnRegsCount();
            }
            else
            {
                passOnReg = vecCount < CallingConvention.GetVecArgumentsOnRegsCount();
            }

            if (passOnReg)
            {
                Operand dest = node.Destination;

                if (preservedArgs[index] == default)
                {
                    if (dest.Type == OperandType.V128)
                    {
                        // V128 is a struct, we pass each half on a GPR if possible.
                        Operand pArg = Local(OperandType.V128);

                        Operand argLReg = Gpr(CallingConvention.GetIntArgumentRegister(intCount), OperandType.I64);
                        Operand argHReg = Gpr(CallingConvention.GetIntArgumentRegister(intCount + 1), OperandType.I64);

                        Operation copyL = Operation(Instruction.VectorCreateScalar, pArg, argLReg);
                        Operation copyH = Operation(Instruction.VectorInsert, pArg, pArg, argHReg, Const(1));

                        cctx.Cfg.Entry.Operations.AddFirst(copyH);
                        cctx.Cfg.Entry.Operations.AddFirst(copyL);

                        preservedArgs[index] = pArg;
                    }
                    else
                    {
                        Operand pArg = Local(dest.Type);

                        Operand argReg = dest.Type.IsInteger()
                            ? Gpr(CallingConvention.GetIntArgumentRegister(intCount), dest.Type)
                            : Xmm(CallingConvention.GetVecArgumentRegister(vecCount), dest.Type);

                        Operation copyOp = Operation(Instruction.Copy, pArg, argReg);

                        cctx.Cfg.Entry.Operations.AddFirst(copyOp);

                        preservedArgs[index] = pArg;
                    }
                }

                Operation nextNode;

                if (dest.AssignmentsCount == 1)
                {
                    // Let's propagate the argument if we can to avoid copies.
                    PreAllocatorCommon.Propagate(ref buffer, dest, preservedArgs[index]);
                    nextNode = node.ListNext;
                }
                else
                {
                    Operation argCopyOp = Operation(Instruction.Copy, dest, preservedArgs[index]);
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

        public static void InsertReturnCopy(IntrusiveList<Operation> nodes, Operation node)
        {
            if (node.SourcesCount == 0)
            {
                return;
            }

            Operand source = node.GetSource(0);

            if (source.Type == OperandType.V128)
            {
                Operand retLReg = Gpr(CallingConvention.GetIntReturnRegister(), OperandType.I64);
                Operand retHReg = Gpr(CallingConvention.GetIntReturnRegisterHigh(), OperandType.I64);

                nodes.AddBefore(node, Operation(Instruction.VectorExtract, retLReg, source, Const(0)));
                nodes.AddBefore(node, Operation(Instruction.VectorExtract, retHReg, source, Const(1)));
            }
            else
            {
                Operand retReg = source.Type.IsInteger()
                    ? Gpr(CallingConvention.GetIntReturnRegister(), source.Type)
                    : Xmm(CallingConvention.GetVecReturnRegister(), source.Type);

                Operation retCopyOp = Operation(Instruction.Copy, retReg, source);

                nodes.AddBefore(node, retCopyOp);
            }
        }
    }
}
