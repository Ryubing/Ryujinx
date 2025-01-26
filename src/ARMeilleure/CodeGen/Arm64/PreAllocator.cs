using ARMeilleure.IntermediateRepresentation;
using ARMeilleure.Translation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using static ARMeilleure.IntermediateRepresentation.Operand.Factory;
using static ARMeilleure.IntermediateRepresentation.Operation.Factory;

namespace ARMeilleure.CodeGen.Arm64
{
    static class PreAllocator
    {
        private class ConstantDict
        {
            private readonly Dictionary<(ulong, OperandType), Operand> _constants;

            public ConstantDict()
            {
                _constants = new Dictionary<(ulong, OperandType), Operand>();
            }

            public void Add(ulong value, OperandType type, Operand local)
            {
                _constants.Add((value, type), local);
            }

            public bool TryGetValue(ulong value, OperandType type, out Operand local)
            {
                return _constants.TryGetValue((value, type), out local);
            }
        }

        public static void RunPass(CompilerContext cctx, out int maxCallArgs)
        {
            maxCallArgs = -1;

            Span<Operation> buffer = default;

            Operand[] preservedArgs = new Operand[CallingConvention.GetArgumentsOnRegsCount()];

            for (BasicBlock block = cctx.Cfg.Blocks.First; block != null; block = block.ListNext)
            {
                ConstantDict constants = new();

                Operation nextNode;

                for (Operation node = block.Operations.First; node != default; node = nextNode)
                {
                    nextNode = node.ListNext;

                    if (node.Instruction == Instruction.Phi)
                    {
                        continue;
                    }

                    InsertConstantRegCopies(constants, block.Operations, node);
                    InsertDestructiveRegCopies(block.Operations, node);

                    switch (node.Instruction)
                    {
                        case Instruction.Call:
                            // Get the maximum number of arguments used on a call.
                            // On windows, when a struct is returned from the call,
                            // we also need to pass the pointer where the struct
                            // should be written on the first argument.
                            int argsCount = node.SourcesCount - 1;

                            if (node.Destination != default && node.Destination.Type == OperandType.V128)
                            {
                                argsCount++;
                            }

                            if (maxCallArgs < argsCount)
                            {
                                maxCallArgs = argsCount;
                            }

                            // Copy values to registers expected by the function
                            // being called, as mandated by the ABI.
                            InsertCallCopies(constants, block.Operations, node);
                            break;
                        case Instruction.CompareAndSwap:
                        case Instruction.CompareAndSwap16:
                        case Instruction.CompareAndSwap8:
                            nextNode = GenerateCompareAndSwap(block.Operations, node);
                            break;
                        case Instruction.LoadArgument:
                            nextNode = InsertLoadArgumentCopy(cctx, ref buffer, block.Operations, preservedArgs, node);
                            break;
                        case Instruction.Return:
                            InsertReturnCopy(block.Operations, node);
                            break;
                        case Instruction.Tailcall:
                            InsertTailcallCopies(constants, block.Operations, node, node);
                            break;
                    }
                }
            }
        }

        private static void InsertConstantRegCopies(ConstantDict constants, IntrusiveList<Operation> nodes, Operation node)
        {
            if (node.SourcesCount == 0 || IsIntrinsicWithConst(node))
            {
                return;
            }

            Instruction inst = node.Instruction;

            Operand src1 = node.GetSource(0);
            Operand src2;

            if (src1.Kind == OperandKind.Constant)
            {
                if (!src1.Type.IsInteger())
                {
                    // Handle non-integer types (FP32, FP64 and V128).
                    // For instructions without an immediate operand, we do the following:
                    // - Insert a copy with the constant value (as integer) to a GPR.
                    // - Insert a copy from the GPR to a XMM register.
                    // - Replace the constant use with the XMM register.
                    src1 = AddFloatConstantCopy(constants, nodes, node, src1);

                    node.SetSource(0, src1);
                }
                else if (!HasConstSrc1(node, src1.Value))
                {
                    // Handle integer types.
                    // Most ALU instructions accepts a 32-bits immediate on the second operand.
                    // We need to ensure the following:
                    // - If the constant is on operand 1, we need to move it.
                    // -- But first, we try to swap operand 1 and 2 if the instruction is commutative.
                    // -- Doing so may allow us to encode the constant as operand 2 and avoid a copy.
                    // - If the constant is on operand 2, we check if the instruction supports it,
                    // if not, we also add a copy. 64-bits constants are usually not supported.
                    if (IsCommutative(node))
                    {
                        src2 = node.GetSource(1);

                        (src2, src1) = (src1, src2);

                        node.SetSource(0, src1);
                        node.SetSource(1, src2);
                    }

                    if (src1.Kind == OperandKind.Constant)
                    {
                        src1 = AddIntConstantCopy(constants, nodes, node, src1);

                        node.SetSource(0, src1);
                    }
                }
            }

            if (node.SourcesCount < 2)
            {
                return;
            }

            src2 = node.GetSource(1);

            if (src2.Kind == OperandKind.Constant)
            {
                if (!src2.Type.IsInteger())
                {
                    src2 = AddFloatConstantCopy(constants, nodes, node, src2);

                    node.SetSource(1, src2);
                }
                else if (!HasConstSrc2(inst, src2))
                {
                    src2 = AddIntConstantCopy(constants, nodes, node, src2);

                    node.SetSource(1, src2);
                }
            }

            if (node.SourcesCount < 3 ||
                node.Instruction == Instruction.BranchIf ||
                node.Instruction == Instruction.Compare ||
                node.Instruction == Instruction.VectorInsert ||
                node.Instruction == Instruction.VectorInsert16 ||
                node.Instruction == Instruction.VectorInsert8)
            {
                return;
            }

            for (int srcIndex = 2; srcIndex < node.SourcesCount; srcIndex++)
            {
                Operand src = node.GetSource(srcIndex);

                if (src.Kind == OperandKind.Constant)
                {
                    if (!src.Type.IsInteger())
                    {
                        src = AddFloatConstantCopy(constants, nodes, node, src);

                        node.SetSource(srcIndex, src);
                    }
                    else
                    {
                        src = AddIntConstantCopy(constants, nodes, node, src);

                        node.SetSource(srcIndex, src);
                    }
                }
            }
        }

        private static void InsertDestructiveRegCopies(IntrusiveList<Operation> nodes, Operation node)
        {
            if (node.Destination == default || node.SourcesCount == 0)
            {
                return;
            }

            Operand dest = node.Destination;
            Operand src1 = node.GetSource(0);

            if (IsSameOperandDestSrc1(node) && src1.Kind == OperandKind.LocalVariable)
            {
                bool useNewLocal = false;

                for (int srcIndex = 1; srcIndex < node.SourcesCount; srcIndex++)
                {
                    if (node.GetSource(srcIndex) == dest)
                    {
                        useNewLocal = true;

                        break;
                    }
                }

                if (useNewLocal)
                {
                    // Dest is being used as some source already, we need to use a new
                    // local to store the temporary value, otherwise the value on dest
                    // local would be overwritten.
                    Operand temp = Local(dest.Type);

                    nodes.AddBefore(node, Operation(Instruction.Copy, temp, src1));

                    node.SetSource(0, temp);

                    nodes.AddAfter(node, Operation(Instruction.Copy, dest, temp));

                    node.Destination = temp;
                }
                else
                {
                    nodes.AddBefore(node, Operation(Instruction.Copy, dest, src1));

                    node.SetSource(0, dest);
                }
            }
        }

        private static void InsertCallCopies(ConstantDict constants, IntrusiveList<Operation> nodes, Operation node)
        {
            Operation operation = node;

            Operand dest = operation.Destination;

            List<Operand> sources =
            [
                operation.GetSource(0)
            ];

            int argsCount = operation.SourcesCount - 1;

            int intMax = CallingConvention.GetArgumentsOnRegsCount();
            int vecMax = CallingConvention.GetArgumentsOnRegsCount();

            int intCount = 0;
            int vecCount = 0;

            int stackOffset = 0;

            for (int index = 0; index < argsCount; index++)
            {
                Operand source = operation.GetSource(index + 1);

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

                    InsertConstantRegCopies(constants, nodes, nodes.AddBefore(node, copyOp));

                    sources.Add(argReg);
                }
                else
                {
                    Operand offset = Const(stackOffset);

                    Operation spillOp = Operation(Instruction.SpillArg, default, offset, source);

                    InsertConstantRegCopies(constants, nodes, nodes.AddBefore(node, spillOp));

                    stackOffset += source.Type.GetSizeInBytes();
                }
            }

            if (dest != default)
            {
                if (dest.Type == OperandType.V128)
                {
                    Operand retLReg = Gpr(CallingConvention.GetIntReturnRegister(), OperandType.I64);
                    Operand retHReg = Gpr(CallingConvention.GetIntReturnRegisterHigh(), OperandType.I64);

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

                    operation.Destination = retReg;
                }
            }

            operation.SetSources(sources.ToArray());
        }

        private static void InsertTailcallCopies(ConstantDict constants,
            IntrusiveList<Operation> nodes,
            Operation node,
            Operation operation)
        {
            List<Operand> sources =
            [
                operation.GetSource(0)
            ];

            int argsCount = operation.SourcesCount - 1;

            int intMax = CallingConvention.GetArgumentsOnRegsCount();
            int vecMax = CallingConvention.GetArgumentsOnRegsCount();

            int intCount = 0;
            int vecCount = 0;

            // Handle arguments passed on registers.
            for (int index = 0; index < argsCount; index++)
            {
                Operand source = operation.GetSource(1 + index);

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

                    InsertConstantRegCopies(constants, nodes, nodes.AddBefore(node, copyOp));

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
            Operand tcAddress = Gpr(CodeGenCommon.TcAddressRegister, OperandType.I64);

            Operation addrCopyOp = Operation(Instruction.Copy, tcAddress, operation.GetSource(0));

            nodes.AddBefore(node, addrCopyOp);

            sources[0] = tcAddress;

            operation.SetSources(sources.ToArray());
        }

        private static Operation GenerateCompareAndSwap(IntrusiveList<Operation> nodes, Operation node)
        {
            Operand expected = node.GetSource(1);

            if (expected.Type == OperandType.V128)
            {
                Operand dest = node.Destination;
                Operand expectedLow = Local(OperandType.I64);
                Operand expectedHigh = Local(OperandType.I64);
                Operand desiredLow = Local(OperandType.I64);
                Operand desiredHigh = Local(OperandType.I64);
                Operand actualLow = Local(OperandType.I64);
                Operand actualHigh = Local(OperandType.I64);

                Operand address = node.GetSource(0);
                Operand desired = node.GetSource(2);

                void SplitOperand(Operand source, Operand low, Operand high)
                {
                    nodes.AddBefore(node, Operation(Instruction.VectorExtract, low, source, Const(0)));
                    nodes.AddBefore(node, Operation(Instruction.VectorExtract, high, source, Const(1)));
                }

                SplitOperand(expected, expectedLow, expectedHigh);
                SplitOperand(desired, desiredLow, desiredHigh);

                Operation operation = node;

                // Update the sources and destinations with split 64-bit halfs of the whole 128-bit values.
                // We also need a additional registers that will be used to store temporary information.
                operation.SetDestinations([actualLow, actualHigh, Local(OperandType.I64), Local(OperandType.I64)]);
                operation.SetSources([address, expectedLow, expectedHigh, desiredLow, desiredHigh]);

                // Add some dummy uses of the input operands, as the CAS operation will be a loop,
                // so they can't be used as destination operand.
                for (int i = 0; i < operation.SourcesCount; i++)
                {
                    Operand src = operation.GetSource(i);
                    node = nodes.AddAfter(node, Operation(Instruction.Copy, src, src));
                }

                // Assemble the vector with the 64-bit values at the given memory location.
                node = nodes.AddAfter(node, Operation(Instruction.VectorCreateScalar, dest, actualLow));
                node = nodes.AddAfter(node, Operation(Instruction.VectorInsert, dest, dest, actualHigh, Const(1)));
            }
            else
            {
                // We need a additional register where the store result will be written to.
                node.SetDestinations([node.Destination, Local(OperandType.I32)]);

                // Add some dummy uses of the input operands, as the CAS operation will be a loop,
                // so they can't be used as destination operand.
                Operation operation = node;

                for (int i = 0; i < operation.SourcesCount; i++)
                {
                    Operand src = operation.GetSource(i);
                    node = nodes.AddAfter(node, Operation(Instruction.Copy, src, src));
                }
            }

            return node.ListNext;
        }

        private static void InsertReturnCopy(IntrusiveList<Operation> nodes, Operation node)
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

        private static Operation InsertLoadArgumentCopy(
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
                passOnReg = intCount < CallingConvention.GetArgumentsOnRegsCount();
            }
            else if (source.Type == OperandType.V128)
            {
                passOnReg = intCount + 1 < CallingConvention.GetArgumentsOnRegsCount();
            }
            else
            {
                passOnReg = vecCount < CallingConvention.GetArgumentsOnRegsCount();
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

        private static Operand AddFloatConstantCopy(
            ConstantDict constants,
            IntrusiveList<Operation> nodes,
            Operation node,
            Operand source)
        {
            Operand temp = Local(source.Type);

            Operand intConst = AddIntConstantCopy(constants, nodes, node, GetIntConst(source));

            Operation copyOp = Operation(Instruction.VectorCreateScalar, temp, intConst);

            nodes.AddBefore(node, copyOp);

            return temp;
        }

        private static Operand AddIntConstantCopy(
            ConstantDict constants,
            IntrusiveList<Operation> nodes,
            Operation node,
            Operand source)
        {
            if (constants.TryGetValue(source.Value, source.Type, out Operand temp))
            {
                return temp;
            }

            temp = Local(source.Type);

            Operation copyOp = Operation(Instruction.Copy, temp, source);

            nodes.AddBefore(node, copyOp);

            constants.Add(source.Value, source.Type, temp);

            return temp;
        }

        private static Operand GetIntConst(Operand value)
        {
            if (value.Type == OperandType.FP32)
            {
                return Const(value.AsInt32());
            }
            else if (value.Type == OperandType.FP64)
            {
                return Const(value.AsInt64());
            }

            return value;
        }

        private static void Delete(IntrusiveList<Operation> nodes, Operation node)
        {
            node.Destination = default;

            for (int index = 0; index < node.SourcesCount; index++)
            {
                node.SetSource(index, default);
            }

            nodes.Remove(node);
        }

        private static Operand Gpr(int register, OperandType type)
        {
            return Register(register, RegisterType.Integer, type);
        }

        private static Operand Xmm(int register, OperandType type)
        {
            return Register(register, RegisterType.Vector, type);
        }

        private static bool IsSameOperandDestSrc1(Operation operation)
        {
            switch (operation.Instruction)
            {
                case Instruction.Extended:
                    return IsSameOperandDestSrc1(operation.Intrinsic);
                case Instruction.VectorInsert:
                case Instruction.VectorInsert16:
                case Instruction.VectorInsert8:
                    return true;
            }

            return false;
        }

        private static bool IsSameOperandDestSrc1(Intrinsic intrinsic)
        {
            IntrinsicInfo info = IntrinsicTable.GetInfo(intrinsic & ~(Intrinsic.Arm64VTypeMask | Intrinsic.Arm64VSizeMask));

            return info.Type == IntrinsicType.ScalarBinaryRd ||
                   info.Type == IntrinsicType.ScalarTernaryFPRdByElem ||
                   info.Type == IntrinsicType.ScalarTernaryShlRd ||
                   info.Type == IntrinsicType.ScalarTernaryShrRd ||
                   info.Type == IntrinsicType.Vector128BinaryRd ||
                   info.Type == IntrinsicType.VectorBinaryRd ||
                   info.Type == IntrinsicType.VectorInsertByElem ||
                   info.Type == IntrinsicType.VectorTernaryRd ||
                   info.Type == IntrinsicType.VectorTernaryRdBitwise ||
                   info.Type == IntrinsicType.VectorTernaryFPRdByElem ||
                   info.Type == IntrinsicType.VectorTernaryRdByElem ||
                   info.Type == IntrinsicType.VectorTernaryShlRd ||
                   info.Type == IntrinsicType.VectorTernaryShrRd;
        }

        private static bool HasConstSrc1(Operation node, ulong value)
        {
            switch (node.Instruction)
            {
                case Instruction.Add:
                case Instruction.BranchIf:
                case Instruction.Compare:
                case Instruction.Subtract:
                    // The immediate encoding of those instructions does not allow Rn to be
                    // XZR (it will be SP instead), so we can't allow a Rn constant in this case.
                    return value == 0 && NotConstOrConst0(node.GetSource(1));
                case Instruction.BitwiseAnd:
                case Instruction.BitwiseExclusiveOr:
                case Instruction.BitwiseNot:
                case Instruction.BitwiseOr:
                case Instruction.ByteSwap:
                case Instruction.CountLeadingZeros:
                case Instruction.Multiply:
                case Instruction.Negate:
                case Instruction.RotateRight:
                case Instruction.ShiftLeft:
                case Instruction.ShiftRightSI:
                case Instruction.ShiftRightUI:
                    return value == 0;
                case Instruction.Copy:
                case Instruction.LoadArgument:
                case Instruction.Spill:
                case Instruction.SpillArg:
                    return true;
                case Instruction.Extended:
                    return value == 0;
            }

            return false;
        }

        private static bool NotConstOrConst0(Operand operand)
        {
            return operand.Kind != OperandKind.Constant || operand.Value == 0;
        }

        private static bool HasConstSrc2(Instruction inst, Operand operand)
        {
            ulong value = operand.Value;

            switch (inst)
            {
                case Instruction.Add:
                case Instruction.BranchIf:
                case Instruction.Compare:
                case Instruction.Subtract:
                    return ConstFitsOnUImm12Sh(value);
                case Instruction.BitwiseAnd:
                case Instruction.BitwiseExclusiveOr:
                case Instruction.BitwiseOr:
                    return value == 0 || CodeGenCommon.TryEncodeBitMask(operand, out _, out _, out _);
                case Instruction.Multiply:
                case Instruction.Store:
                case Instruction.Store16:
                case Instruction.Store8:
                    return value == 0;
                case Instruction.RotateRight:
                case Instruction.ShiftLeft:
                case Instruction.ShiftRightSI:
                case Instruction.ShiftRightUI:
                case Instruction.VectorExtract:
                case Instruction.VectorExtract16:
                case Instruction.VectorExtract8:
                    return true;
                case Instruction.Extended:
                    // TODO: Check if actual intrinsic is supposed to have consts here?
                    // Right now we only hit this case for fixed-point int <-> FP conversion instructions.
                    return true;
            }

            return false;
        }

        private static bool IsCommutative(Operation operation)
        {
            switch (operation.Instruction)
            {
                case Instruction.Add:
                case Instruction.BitwiseAnd:
                case Instruction.BitwiseExclusiveOr:
                case Instruction.BitwiseOr:
                case Instruction.Multiply:
                    return true;

                case Instruction.BranchIf:
                case Instruction.Compare:
                    {
                        Operand comp = operation.GetSource(2);

                        Debug.Assert(comp.Kind == OperandKind.Constant);

                        Comparison compType = (Comparison)comp.AsInt32();

                        return compType == Comparison.Equal || compType == Comparison.NotEqual;
                    }
            }

            return false;
        }

        private static bool ConstFitsOnUImm12Sh(ulong value)
        {
            return (value & ~0xfffUL) == 0 || (value & ~0xfff000UL) == 0;
        }

        private static bool IsIntrinsicWithConst(Operation operation)
        {
            bool isIntrinsic = IsIntrinsic(operation.Instruction);

            if (isIntrinsic)
            {
                Intrinsic intrinsic = operation.Intrinsic;
                IntrinsicInfo info = IntrinsicTable.GetInfo(intrinsic & ~(Intrinsic.Arm64VTypeMask | Intrinsic.Arm64VSizeMask));

                // Those have integer inputs that don't support consts.
                return info.Type != IntrinsicType.ScalarFPConvGpr &&
                       info.Type != IntrinsicType.ScalarFPConvFixedGpr &&
                       info.Type != IntrinsicType.SetRegister;
            }

            return false;
        }

        private static bool IsIntrinsic(Instruction inst)
        {
            return inst == Instruction.Extended;
        }
    }
}
