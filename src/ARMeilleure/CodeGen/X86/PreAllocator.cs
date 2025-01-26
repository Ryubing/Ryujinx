using ARMeilleure.CodeGen.RegisterAllocators;
using ARMeilleure.IntermediateRepresentation;
using ARMeilleure.Translation;
using System;
using System.Diagnostics;
using static ARMeilleure.IntermediateRepresentation.Operand.Factory;
using static ARMeilleure.IntermediateRepresentation.Operation.Factory;

namespace ARMeilleure.CodeGen.X86
{
    class PreAllocator
    {
        public static void RunPass(CompilerContext cctx, StackAllocator stackAlloc, out int maxCallArgs)
        {
            maxCallArgs = -1;

            Span<Operation> buffer = default;

            CallConvName callConv = CallingConvention.GetCurrentCallConv();

            Operand[] preservedArgs = new Operand[CallingConvention.GetArgumentsOnRegsCount()];

            for (BasicBlock block = cctx.Cfg.Blocks.First; block != null; block = block.ListNext)
            {
                Operation nextNode;

                for (Operation node = block.Operations.First; node != default; node = nextNode)
                {
                    nextNode = node.ListNext;

                    if (node.Instruction == Instruction.Phi)
                    {
                        continue;
                    }

                    InsertConstantRegCopies(block.Operations, node);
                    InsertDestructiveRegCopies(block.Operations, node);
                    InsertConstrainedRegCopies(block.Operations, node);

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
                            if (callConv == CallConvName.Windows)
                            {
                                PreAllocatorWindows.InsertCallCopies(block.Operations, stackAlloc, node);
                            }
                            else /* if (callConv == CallConvName.SystemV) */
                            {
                                PreAllocatorSystemV.InsertCallCopies(block.Operations, node);
                            }
                            break;

                        case Instruction.ConvertToFPUI:
                            GenerateConvertToFPUI(block.Operations, node);
                            break;

                        case Instruction.LoadArgument:
                            if (callConv == CallConvName.Windows)
                            {
                                nextNode = PreAllocatorWindows.InsertLoadArgumentCopy(cctx, ref buffer, block.Operations, preservedArgs, node);
                            }
                            else /* if (callConv == CallConvName.SystemV) */
                            {
                                nextNode = PreAllocatorSystemV.InsertLoadArgumentCopy(cctx, ref buffer, block.Operations, preservedArgs, node);
                            }
                            break;

                        case Instruction.Negate:
                            if (!node.GetSource(0).Type.IsInteger())
                            {
                                GenerateNegate(block.Operations, node);
                            }
                            break;

                        case Instruction.Return:
                            if (callConv == CallConvName.Windows)
                            {
                                PreAllocatorWindows.InsertReturnCopy(cctx, block.Operations, preservedArgs, node);
                            }
                            else /* if (callConv == CallConvName.SystemV) */
                            {
                                PreAllocatorSystemV.InsertReturnCopy(block.Operations, node);
                            }
                            break;

                        case Instruction.Tailcall:
                            if (callConv == CallConvName.Windows)
                            {
                                PreAllocatorWindows.InsertTailcallCopies(block.Operations, node);
                            }
                            else
                            {
                                PreAllocatorSystemV.InsertTailcallCopies(block.Operations, node);
                            }
                            break;

                        case Instruction.VectorInsert8:
                            if (!HardwareCapabilities.SupportsSse41)
                            {
                                GenerateVectorInsert8(block.Operations, node);
                            }
                            break;

                        case Instruction.Extended:
                            if (node.Intrinsic == Intrinsic.X86Ldmxcsr)
                            {
                                int stackOffset = stackAlloc.Allocate(OperandType.I32);

                                node.SetSources([Const(stackOffset), node.GetSource(0)]);
                            }
                            else if (node.Intrinsic == Intrinsic.X86Stmxcsr)
                            {
                                int stackOffset = stackAlloc.Allocate(OperandType.I32);

                                node.SetSources([Const(stackOffset)]);
                            }
                            break;
                    }
                }
            }
        }

        protected static void InsertConstantRegCopies(IntrusiveList<Operation> nodes, Operation node)
        {
            if (node.SourcesCount == 0 || IsXmmIntrinsic(node))
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
                    src1 = AddXmmCopy(nodes, node, src1);

                    node.SetSource(0, src1);
                }
                else if (!HasConstSrc1(inst))
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
                        src1 = AddCopy(nodes, node, src1);

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
                    src2 = AddXmmCopy(nodes, node, src2);

                    node.SetSource(1, src2);
                }
                else if (!HasConstSrc2(inst) || CodeGenCommon.IsLongConst(src2))
                {
                    src2 = AddCopy(nodes, node, src2);

                    node.SetSource(1, src2);
                }
            }
        }

        protected static void InsertConstrainedRegCopies(IntrusiveList<Operation> nodes, Operation node)
        {
            Operand dest = node.Destination;

            switch (node.Instruction)
            {
                case Instruction.CompareAndSwap:
                case Instruction.CompareAndSwap16:
                case Instruction.CompareAndSwap8:
                    {
                        OperandType type = node.GetSource(1).Type;

                        if (type == OperandType.V128)
                        {
                            // Handle the many restrictions of the compare and exchange (16 bytes) instruction:
                            // - The expected value should be in RDX:RAX.
                            // - The new value to be written should be in RCX:RBX.
                            // - The value at the memory location is loaded to RDX:RAX.
                            void SplitOperand(Operand source, Operand lr, Operand hr)
                            {
                                nodes.AddBefore(node, Operation(Instruction.VectorExtract, lr, source, Const(0)));
                                nodes.AddBefore(node, Operation(Instruction.VectorExtract, hr, source, Const(1)));
                            }

                            Operand rax = Gpr(X86Register.Rax, OperandType.I64);
                            Operand rbx = Gpr(X86Register.Rbx, OperandType.I64);
                            Operand rcx = Gpr(X86Register.Rcx, OperandType.I64);
                            Operand rdx = Gpr(X86Register.Rdx, OperandType.I64);

                            SplitOperand(node.GetSource(1), rax, rdx);
                            SplitOperand(node.GetSource(2), rbx, rcx);

                            Operation operation = node;

                            node = nodes.AddAfter(node, Operation(Instruction.VectorCreateScalar, dest, rax));
                            nodes.AddAfter(node, Operation(Instruction.VectorInsert, dest, dest, rdx, Const(1)));

                            operation.SetDestinations([rdx, rax]);
                            operation.SetSources([operation.GetSource(0), rdx, rax, rcx, rbx]);
                        }
                        else
                        {
                            // Handle the many restrictions of the compare and exchange (32/64) instruction:
                            // - The expected value should be in (E/R)AX.
                            // - The value at the memory location is loaded to (E/R)AX.
                            Operand expected = node.GetSource(1);
                            Operand newValue = node.GetSource(2);

                            Operand rax = Gpr(X86Register.Rax, expected.Type);

                            nodes.AddBefore(node, Operation(Instruction.Copy, rax, expected));

                            // We need to store the new value into a temp, since it may
                            // be a constant, and this instruction does not support immediate operands.
                            Operand temp = Local(newValue.Type);

                            nodes.AddBefore(node, Operation(Instruction.Copy, temp, newValue));

                            node.SetSources([node.GetSource(0), rax, temp]);

                            nodes.AddAfter(node, Operation(Instruction.Copy, dest, rax));

                            node.Destination = rax;
                        }

                        break;
                    }

                case Instruction.Divide:
                case Instruction.DivideUI:
                    {
                        // Handle the many restrictions of the division instructions:
                        // - The dividend is always in RDX:RAX.
                        // - The result is always in RAX.
                        // - Additionally it also writes the remainder in RDX.
                        if (dest.Type.IsInteger())
                        {
                            Operand src1 = node.GetSource(0);

                            Operand rax = Gpr(X86Register.Rax, src1.Type);
                            Operand rdx = Gpr(X86Register.Rdx, src1.Type);

                            nodes.AddBefore(node, Operation(Instruction.Copy, rax, src1));
                            nodes.AddBefore(node, Operation(Instruction.Clobber, rdx));

                            nodes.AddAfter(node, Operation(Instruction.Copy, dest, rax));

                            node.SetSources([rdx, rax, node.GetSource(1)]);
                            node.Destination = rax;
                        }

                        break;
                    }

                case Instruction.Extended:
                    {
                        bool isBlend = node.Intrinsic == Intrinsic.X86Blendvpd ||
                                   node.Intrinsic == Intrinsic.X86Blendvps ||
                                   node.Intrinsic == Intrinsic.X86Pblendvb;

                        // BLENDVPD, BLENDVPS, PBLENDVB last operand is always implied to be XMM0 when VEX is not supported.
                        // SHA256RNDS2 always has an implied XMM0 as a last operand.
                        if ((isBlend && !HardwareCapabilities.SupportsVexEncoding) || node.Intrinsic == Intrinsic.X86Sha256Rnds2)
                        {
                            Operand xmm0 = Xmm(X86Register.Xmm0, OperandType.V128);

                            nodes.AddBefore(node, Operation(Instruction.Copy, xmm0, node.GetSource(2)));

                            node.SetSource(2, xmm0);
                        }

                        break;
                    }

                case Instruction.Multiply64HighSI:
                case Instruction.Multiply64HighUI:
                    {
                        // Handle the many restrictions of the i64 * i64 = i128 multiply instructions:
                        // - The multiplicand is always in RAX.
                        // - The lower 64-bits of the result is always in RAX.
                        // - The higher 64-bits of the result is always in RDX.
                        Operand src1 = node.GetSource(0);

                        Operand rax = Gpr(X86Register.Rax, src1.Type);
                        Operand rdx = Gpr(X86Register.Rdx, src1.Type);

                        nodes.AddBefore(node, Operation(Instruction.Copy, rax, src1));

                        node.SetSource(0, rax);

                        nodes.AddAfter(node, Operation(Instruction.Copy, dest, rdx));

                        node.SetDestinations([rdx, rax]);

                        break;
                    }

                case Instruction.RotateRight:
                case Instruction.ShiftLeft:
                case Instruction.ShiftRightSI:
                case Instruction.ShiftRightUI:
                    {
                        // The shift register is always implied to be CL (low 8-bits of RCX or ECX).
                        if (node.GetSource(1).Kind == OperandKind.LocalVariable)
                        {
                            Operand rcx = Gpr(X86Register.Rcx, OperandType.I32);

                            nodes.AddBefore(node, Operation(Instruction.Copy, rcx, node.GetSource(1)));

                            node.SetSource(1, rcx);
                        }

                        break;
                    }
            }
        }

        protected static void InsertDestructiveRegCopies(IntrusiveList<Operation> nodes, Operation node)
        {
            if (node.Destination == default || node.SourcesCount == 0)
            {
                return;
            }

            Instruction inst = node.Instruction;

            Operand dest = node.Destination;
            Operand src1 = node.GetSource(0);

            // The multiply instruction (that maps to IMUL) is somewhat special, it has
            // a three operand form where the second source is a immediate value.
            bool threeOperandForm = inst == Instruction.Multiply && node.GetSource(1).Kind == OperandKind.Constant;

            if (IsSameOperandDestSrc1(node) && src1.Kind == OperandKind.LocalVariable && !threeOperandForm)
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
            else if (inst == Instruction.ConditionalSelect)
            {
                Operand src2 = node.GetSource(1);
                Operand src3 = node.GetSource(2);

                if (src1 == dest || src2 == dest)
                {
                    Operand temp = Local(dest.Type);

                    nodes.AddBefore(node, Operation(Instruction.Copy, temp, src3));

                    node.SetSource(2, temp);

                    nodes.AddAfter(node, Operation(Instruction.Copy, dest, temp));

                    node.Destination = temp;
                }
                else
                {
                    nodes.AddBefore(node, Operation(Instruction.Copy, dest, src3));

                    node.SetSource(2, dest);
                }
            }
        }

        private static void GenerateConvertToFPUI(IntrusiveList<Operation> nodes, Operation node)
        {
            // Unsigned integer to FP conversions are not supported on X86.
            // We need to turn them into signed integer to FP conversions, and
            // adjust the final result.
            Operand dest = node.Destination;
            Operand source = node.GetSource(0);

            Debug.Assert(source.Type.IsInteger(), $"Invalid source type \"{source.Type}\".");

            Operation currentNode = node;

            if (source.Type == OperandType.I32)
            {
                // For 32-bits integers, we can just zero-extend to 64-bits,
                // and then use the 64-bits signed conversion instructions.
                Operand zex = Local(OperandType.I64);

                node = nodes.AddAfter(node, Operation(Instruction.ZeroExtend32, zex, source));
                nodes.AddAfter(node, Operation(Instruction.ConvertToFP, dest, zex));
            }
            else /* if (source.Type == OperandType.I64) */
            {
                // For 64-bits integers, we need to do the following:
                // - Ensure that the integer has the most significant bit clear.
                // -- This can be done by shifting the value right by 1, that is, dividing by 2.
                // -- The least significant bit is lost in this case though.
                // - We can then convert the shifted value with a signed integer instruction.
                // - The result still needs to be corrected after that.
                // -- First, we need to multiply the result by 2, as we divided it by 2 before.
                // --- This can be done efficiently by adding the result to itself.
                // -- Then, we need to add the least significant bit that was shifted out.
                // --- We can convert the least significant bit to float, and add it to the result.
                Operand lsb = Local(OperandType.I64);
                Operand half = Local(OperandType.I64);

                Operand lsbF = Local(dest.Type);

                node = nodes.AddAfter(node, Operation(Instruction.Copy, lsb, source));
                node = nodes.AddAfter(node, Operation(Instruction.Copy, half, source));

                node = nodes.AddAfter(node, Operation(Instruction.BitwiseAnd, lsb, lsb, Const(1L)));
                node = nodes.AddAfter(node, Operation(Instruction.ShiftRightUI, half, half, Const(1)));

                node = nodes.AddAfter(node, Operation(Instruction.ConvertToFP, lsbF, lsb));
                node = nodes.AddAfter(node, Operation(Instruction.ConvertToFP, dest, half));

                node = nodes.AddAfter(node, Operation(Instruction.Add, dest, dest, dest));
                nodes.AddAfter(node, Operation(Instruction.Add, dest, dest, lsbF));
            }

            Delete(nodes, currentNode);
        }

        private static void GenerateNegate(IntrusiveList<Operation> nodes, Operation node)
        {
            // There's no SSE FP negate instruction, so we need to transform that into
            // a XOR of the value to be negated with a mask with the highest bit set.
            // This also produces -0 for a negation of the value 0.
            Operand dest = node.Destination;
            Operand source = node.GetSource(0);

            Debug.Assert(dest.Type == OperandType.FP32 ||
                         dest.Type == OperandType.FP64, $"Invalid destination type \"{dest.Type}\".");

            Operation currentNode = node;

            Operand res = Local(dest.Type);

            node = nodes.AddAfter(node, Operation(Instruction.VectorOne, res));

            if (dest.Type == OperandType.FP32)
            {
                node = nodes.AddAfter(node, Operation(Intrinsic.X86Pslld, res, res, Const(31)));
            }
            else /* if (dest.Type == OperandType.FP64) */
            {
                node = nodes.AddAfter(node, Operation(Intrinsic.X86Psllq, res, res, Const(63)));
            }

            node = nodes.AddAfter(node, Operation(Intrinsic.X86Xorps, res, res, source));

            nodes.AddAfter(node, Operation(Instruction.Copy, dest, res));

            Delete(nodes, currentNode);
        }

        private static void GenerateVectorInsert8(IntrusiveList<Operation> nodes, Operation node)
        {
            // Handle vector insertion, when SSE 4.1 is not supported.
            Operand dest = node.Destination;
            Operand src1 = node.GetSource(0); // Vector
            Operand src2 = node.GetSource(1); // Value
            Operand src3 = node.GetSource(2); // Index

            Debug.Assert(src3.Kind == OperandKind.Constant);

            byte index = src3.AsByte();

            Debug.Assert(index < 16);

            Operation currentNode = node;

            Operand temp1 = Local(OperandType.I32);
            Operand temp2 = Local(OperandType.I32);

            node = nodes.AddAfter(node, Operation(Instruction.Copy, temp2, src2));

            Operation vextOp = Operation(Instruction.VectorExtract16, temp1, src1, Const(index >> 1));

            node = nodes.AddAfter(node, vextOp);

            if ((index & 1) != 0)
            {
                node = nodes.AddAfter(node, Operation(Instruction.ZeroExtend8, temp1, temp1));
                node = nodes.AddAfter(node, Operation(Instruction.ShiftLeft, temp2, temp2, Const(8)));
                node = nodes.AddAfter(node, Operation(Instruction.BitwiseOr, temp1, temp1, temp2));
            }
            else
            {
                node = nodes.AddAfter(node, Operation(Instruction.ZeroExtend8, temp2, temp2));
                node = nodes.AddAfter(node, Operation(Instruction.BitwiseAnd, temp1, temp1, Const(0xff00)));
                node = nodes.AddAfter(node, Operation(Instruction.BitwiseOr, temp1, temp1, temp2));
            }

            Operation vinsOp = Operation(Instruction.VectorInsert16, dest, src1, temp1, Const(index >> 1));

            nodes.AddAfter(node, vinsOp);

            Delete(nodes, currentNode);
        }

        protected static Operand AddXmmCopy(IntrusiveList<Operation> nodes, Operation node, Operand source)
        {
            Operand temp = Local(source.Type);
            Operand intConst = AddCopy(nodes, node, GetIntConst(source));

            Operation copyOp = Operation(Instruction.VectorCreateScalar, temp, intConst);

            nodes.AddBefore(node, copyOp);

            return temp;
        }

        protected static Operand AddCopy(IntrusiveList<Operation> nodes, Operation node, Operand source)
        {
            Operand temp = Local(source.Type);

            Operation copyOp = Operation(Instruction.Copy, temp, source);

            nodes.AddBefore(node, copyOp);

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

        protected static void Delete(IntrusiveList<Operation> nodes, Operation node)
        {
            node.Destination = default;

            for (int index = 0; index < node.SourcesCount; index++)
            {
                node.SetSource(index, default);
            }

            nodes.Remove(node);
        }

        protected static Operand Gpr(X86Register register, OperandType type)
        {
            return Register((int)register, RegisterType.Integer, type);
        }

        protected static Operand Xmm(X86Register register, OperandType type)
        {
            return Register((int)register, RegisterType.Vector, type);
        }

        private static bool IsSameOperandDestSrc1(Operation operation)
        {
            switch (operation.Instruction)
            {
                case Instruction.Add:
                    return !HardwareCapabilities.SupportsVexEncoding && !operation.Destination.Type.IsInteger();
                case Instruction.Multiply:
                case Instruction.Subtract:
                    return !HardwareCapabilities.SupportsVexEncoding || operation.Destination.Type.IsInteger();

                case Instruction.BitwiseAnd:
                case Instruction.BitwiseExclusiveOr:
                case Instruction.BitwiseNot:
                case Instruction.BitwiseOr:
                case Instruction.ByteSwap:
                case Instruction.Negate:
                case Instruction.RotateRight:
                case Instruction.ShiftLeft:
                case Instruction.ShiftRightSI:
                case Instruction.ShiftRightUI:
                    return true;

                case Instruction.Divide:
                    return !HardwareCapabilities.SupportsVexEncoding && !operation.Destination.Type.IsInteger();

                case Instruction.VectorInsert:
                case Instruction.VectorInsert16:
                case Instruction.VectorInsert8:
                    return !HardwareCapabilities.SupportsVexEncoding;

                case Instruction.Extended:
                    return IsIntrinsicSameOperandDestSrc1(operation);
            }

            return IsVexSameOperandDestSrc1(operation);
        }

        private static bool IsIntrinsicSameOperandDestSrc1(Operation operation)
        {
            IntrinsicInfo info = IntrinsicTable.GetInfo(operation.Intrinsic);

            return info.Type == IntrinsicType.Crc32 || info.Type == IntrinsicType.Fma || IsVexSameOperandDestSrc1(operation);
        }

        private static bool IsVexSameOperandDestSrc1(Operation operation)
        {
            if (IsIntrinsic(operation.Instruction))
            {
                IntrinsicInfo info = IntrinsicTable.GetInfo(operation.Intrinsic);

                bool hasVex = HardwareCapabilities.SupportsVexEncoding && Assembler.SupportsVexPrefix(info.Inst);

                bool isUnary = operation.SourcesCount < 2;

                bool hasVecDest = operation.Destination != default && operation.Destination.Type == OperandType.V128;

                return !hasVex && !isUnary && hasVecDest;
            }

            return false;
        }

        private static bool HasConstSrc1(Instruction inst)
        {
            return inst switch
            {
                Instruction.Copy or Instruction.LoadArgument or Instruction.Spill or Instruction.SpillArg => true,
                _ => false,
            };
        }

        private static bool HasConstSrc2(Instruction inst)
        {
            switch (inst)
            {
                case Instruction.Add:
                case Instruction.BitwiseAnd:
                case Instruction.BitwiseExclusiveOr:
                case Instruction.BitwiseOr:
                case Instruction.BranchIf:
                case Instruction.Compare:
                case Instruction.Multiply:
                case Instruction.RotateRight:
                case Instruction.ShiftLeft:
                case Instruction.ShiftRightSI:
                case Instruction.ShiftRightUI:
                case Instruction.Store:
                case Instruction.Store16:
                case Instruction.Store8:
                case Instruction.Subtract:
                case Instruction.VectorExtract:
                case Instruction.VectorExtract16:
                case Instruction.VectorExtract8:
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

        private static bool IsIntrinsic(Instruction inst)
        {
            return inst == Instruction.Extended;
        }

        private static bool IsXmmIntrinsic(Operation operation)
        {
            if (operation.Instruction != Instruction.Extended)
            {
                return false;
            }

            IntrinsicInfo info = IntrinsicTable.GetInfo(operation.Intrinsic);

            return info.Type != IntrinsicType.Crc32;
        }
    }
}
