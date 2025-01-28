using ARMeilleure.CodeGen.Optimizations;
using ARMeilleure.IntermediateRepresentation;
using ARMeilleure.Translation;
using System.Collections.Generic;
using static ARMeilleure.IntermediateRepresentation.Operand.Factory;
using static ARMeilleure.IntermediateRepresentation.Operation.Factory;

namespace ARMeilleure.CodeGen.Arm64
{
    static class Arm64Optimizer
    {
        private const int MaxConstantUses = 10000;

        public static void RunPass(ControlFlowGraph cfg)
        {
            Dictionary<ulong, Operand> constants = new();

            Operand GetConstantCopy(BasicBlock block, Operation operation, Operand source)
            {
                // If the constant has many uses, we also force a new constant mov to be added, in order
                // to avoid overflow of the counts field (that is limited to 16 bits).
                if (!constants.TryGetValue(source.Value, out Operand constant) || constant.UsesCount > MaxConstantUses)
                {
                    constant = Local(source.Type);

                    Operation copyOp = Operation(Instruction.Copy, constant, source);

                    block.Operations.AddBefore(operation, copyOp);

                    constants[source.Value] = constant;
                }

                return constant;
            }

            for (BasicBlock block = cfg.Blocks.First; block != null; block = block.ListNext)
            {
                constants.Clear();

                Operation nextNode;

                for (Operation node = block.Operations.First; node != default; node = nextNode)
                {
                    nextNode = node.ListNext;

                    // Insert copies for constants that can't fit on a 32-bit immediate.
                    // Doing this early unblocks a few optimizations.
                    if (node.Instruction == Instruction.Add)
                    {
                        Operand src1 = node.GetSource(0);
                        Operand src2 = node.GetSource(1);

                        if (src1.Kind == OperandKind.Constant && (src1.Relocatable || ConstTooLong(src1, OperandType.I32)))
                        {
                            node.SetSource(0, GetConstantCopy(block, node, src1));
                        }

                        if (src2.Kind == OperandKind.Constant && (src2.Relocatable || ConstTooLong(src2, OperandType.I32)))
                        {
                            node.SetSource(1, GetConstantCopy(block, node, src2));
                        }
                    }

                    // Try to fold something like:
                    //  lsl x1, x1, #2
                    //  add x0, x0, x1
                    //  ldr x0, [x0]
                    //  add x2, x2, #16
                    //  ldr x2, [x2]
                    // Into:
                    //  ldr x0, [x0, x1, lsl #2]
                    //  ldr x2, [x2, #16]
                    if (IsMemoryLoadOrStore(node.Instruction))
                    {
                        OperandType type;

                        if (node.Destination != default)
                        {
                            type = node.Destination.Type;
                        }
                        else
                        {
                            type = node.GetSource(1).Type;
                        }

                        Operand memOp = GetMemoryOperandOrNull(node.GetSource(0), type);

                        if (memOp != default)
                        {
                            node.SetSource(0, memOp);
                        }
                    }
                }
            }

            Optimizer.RemoveUnusedNodes(cfg);
        }

        private static Operand GetMemoryOperandOrNull(Operand addr, OperandType type)
        {
            Operand baseOp = addr;

            // First we check if the address is the result of a local X with immediate
            // addition. If that is the case, then the baseOp is X, and the memory operand immediate
            // becomes the addition immediate. Otherwise baseOp keeps being the address.
            int imm = GetConstOp(ref baseOp, type);
            if (imm != 0)
            {
                return MemoryOp(type, baseOp, default, Multiplier.x1, imm);
            }

            // Now we check if the baseOp is the result of a local Y with a local Z addition.
            // If that is the case, we now set baseOp to Y and indexOp to Z. We further check
            // if Z is the result of a left shift of local W by a value == 0 or == Log2(AccessSize),
            // if that is the case, we set indexOp to W and adjust the scale value of the memory operand
            // to match that of the left shift.
            // There is one missed case, which is the address being a shift result, but this is
            // probably not worth optimizing as it should never happen.
            (Operand indexOp, Multiplier scale) = GetIndexOp(ref baseOp, type);

            // If baseOp is still equal to address, then there's nothing that can be optimized.
            if (baseOp == addr)
            {
                return default;
            }

            return MemoryOp(type, baseOp, indexOp, scale, 0);
        }

        private static int GetConstOp(ref Operand baseOp, OperandType accessType)
        {
            Operation operation = GetAsgOpWithInst(baseOp, Instruction.Add);

            if (operation == default)
            {
                return 0;
            }

            Operand src1 = operation.GetSource(0);
            Operand src2 = operation.GetSource(1);

            Operand constOp;
            Operand otherOp;

            if (src1.Kind == OperandKind.Constant && src2.Kind == OperandKind.LocalVariable)
            {
                constOp = src1;
                otherOp = src2;
            }
            else if (src1.Kind == OperandKind.LocalVariable && src2.Kind == OperandKind.Constant)
            {
                constOp = src2;
                otherOp = src1;
            }
            else
            {
                return 0;
            }

            // If we have addition by a constant that we can't encode on the instruction,
            // then we can't optimize it further.
            if (ConstTooLong(constOp, accessType))
            {
                return 0;
            }

            baseOp = otherOp;

            return constOp.AsInt32();
        }

        private static (Operand, Multiplier) GetIndexOp(ref Operand baseOp, OperandType accessType)
        {
            Operand indexOp = default;

            Multiplier scale = Multiplier.x1;

            Operation addOp = GetAsgOpWithInst(baseOp, Instruction.Add);

            if (addOp == default)
            {
                return (indexOp, scale);
            }

            Operand src1 = addOp.GetSource(0);
            Operand src2 = addOp.GetSource(1);

            if (src1.Kind != OperandKind.LocalVariable || src2.Kind != OperandKind.LocalVariable)
            {
                return (indexOp, scale);
            }

            baseOp = src1;
            indexOp = src2;

            Operation shlOp = GetAsgOpWithInst(src1, Instruction.ShiftLeft);

            bool indexOnSrc2 = false;

            if (shlOp == default)
            {
                shlOp = GetAsgOpWithInst(src2, Instruction.ShiftLeft);

                indexOnSrc2 = true;
            }

            if (shlOp != default)
            {
                Operand shSrc = shlOp.GetSource(0);
                Operand shift = shlOp.GetSource(1);

                int maxShift = Assembler.GetScaleForType(accessType);

                if (shSrc.Kind == OperandKind.LocalVariable &&
                    shift.Kind == OperandKind.Constant &&
                    (shift.Value == 0 || shift.Value == (ulong)maxShift))
                {
                    scale = shift.Value switch
                    {
                        1 => Multiplier.x2,
                        2 => Multiplier.x4,
                        3 => Multiplier.x8,
                        4 => Multiplier.x16,
                        _ => Multiplier.x1,
                    };

                    baseOp = indexOnSrc2 ? src1 : src2;
                    indexOp = shSrc;
                }
            }

            return (indexOp, scale);
        }

        private static Operation GetAsgOpWithInst(Operand op, Instruction inst)
        {
            // If we have multiple assignments, folding is not safe
            // as the value may be different depending on the
            // control flow path.
            if (op.AssignmentsCount != 1)
            {
                return default;
            }

            Operation asgOp = op.Assignments[0];

            if (asgOp.Instruction != inst)
            {
                return default;
            }

            return asgOp;
        }

        private static bool IsMemoryLoadOrStore(Instruction inst)
        {
            return inst == Instruction.Load || inst == Instruction.Store;
        }

        private static bool ConstTooLong(Operand constOp, OperandType accessType)
        {
            if ((uint)constOp.Value != constOp.Value)
            {
                return true;
            }

            return !CodeGenCommon.ConstFitsOnUImm12(constOp.AsInt32(), accessType);
        }
    }
}
