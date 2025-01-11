using Ryujinx.Graphics.Shader.IntermediateRepresentation;
using System.Collections.Generic;

namespace Ryujinx.Graphics.Shader.Translation.Optimizations
{
    static class Utils
    {
        public static bool IsInputLoad(INode node)
        {
            return (node is Operation operation) &&
                   operation.Inst == Instruction.Load &&
                   operation.StorageKind == StorageKind.Input;
        }

        public static bool IsInputLoad(INode node, IoVariable ioVariable, int elemIndex)
        {
            if (node is not Operation operation ||
                operation.Inst != Instruction.Load ||
                operation.StorageKind != StorageKind.Input ||
                operation.SourcesCount != 2)
            {
                return false;
            }

            Operand ioVariableSrc = operation.GetSource(0);

            if (ioVariableSrc.Type != OperandType.Constant || (IoVariable)ioVariableSrc.Value != ioVariable)
            {
                return false;
            }

            Operand elemIndexSrc = operation.GetSource(1);

            return elemIndexSrc.Type == OperandType.Constant && elemIndexSrc.Value == elemIndex;
        }

        private static bool IsSameOperand(Operand x, Operand y)
        {
            if (x.Type != y.Type || x.Value != y.Value)
            {
                return false;
            }

            // TODO: Handle Load operations with the same storage and the same constant parameters.
            return x == y || x.Type == OperandType.Constant || x.Type == OperandType.ConstantBuffer;
        }

        private static bool AreAllSourcesEqual(Operation node, Operation otherNode)
        {
            if (node.SourcesCount != otherNode.SourcesCount)
            {
                return false;
            }

            for (int index = 0; index < node.SourcesCount; index++)
            {
                if (!IsSameOperand(node.GetSource(index), otherNode.GetSource(index)))
                {
                    return false;
                }
            }

            return true;
        }

        public static bool AreAllSourcesTheSameOperand(INode node)
        {
            Operand firstSrc = node.GetSource(0);

            for (int index = 1; index < node.SourcesCount; index++)
            {
                if (!IsSameOperand(firstSrc, node.GetSource(index)))
                {
                    return false;
                }
            }

            return true;
        }

        private static Operation FindBranchSource(BasicBlock block)
        {
            foreach (BasicBlock sourceBlock in block.Predecessors)
            {
                if (sourceBlock.Operations.Count > 0)
                {
                    if (sourceBlock.GetLastOp() is Operation lastOp && IsConditionalBranch(lastOp.Inst) && sourceBlock.Next == block)
                    {
                        return lastOp;
                    }
                }
            }

            return null;
        }

        private static bool IsConditionalBranch(Instruction inst)
        {
            return inst == Instruction.BranchIfFalse || inst == Instruction.BranchIfTrue;
        }

        private static bool IsSameCondition(Operand currentCondition, Operand queryCondition)
        {
            if (currentCondition == queryCondition)
            {
                return true;
            }

            return currentCondition.AsgOp is Operation currentOperation &&
                queryCondition.AsgOp is Operation queryOperation &&
                currentOperation.Inst == queryOperation.Inst &&
                AreAllSourcesEqual(currentOperation, queryOperation);
        }

        private static bool BlockConditionsMatch(BasicBlock currentBlock, BasicBlock queryBlock)
        {
            // Check if all the conditions for the query block are satisfied by the current block.
            // Just checks the top-most conditional for now.

            Operation currentBranch = FindBranchSource(currentBlock);
            Operation queryBranch = FindBranchSource(queryBlock);

            Operand currentCondition = currentBranch?.GetSource(0);
            Operand queryCondition = queryBranch?.GetSource(0);

            // The condition should be the same operand instance.

            return currentBranch != null && queryBranch != null &&
                   currentBranch.Inst == queryBranch.Inst &&
                   IsSameCondition(currentCondition, queryCondition);
        }

        public static Operand FindLastOperation(Operand source, BasicBlock block, bool recurse = true)
        {
            if (source.AsgOp is PhiNode phiNode)
            {
                // This source can have a different value depending on a previous branch.
                // Ensure that conditions met for that branch are also met for the current one.
                // Prefer the latest sources for the phi node.

                int undefCount = 0;

                for (int i = phiNode.SourcesCount - 1; i >= 0; i--)
                {
                    BasicBlock phiBlock = phiNode.GetBlock(i);
                    Operand phiSource = phiNode.GetSource(i);

                    if (BlockConditionsMatch(block, phiBlock))
                    {
                        return phiSource;
                    }
                    else if (recurse && phiSource.AsgOp is PhiNode)
                    {
                        // Phi source is another phi.
                        // Let's check if that phi has a block that matches our condition.

                        Operand match = FindLastOperation(phiSource, block, false);

                        if (match != phiSource)
                        {
                            return match;
                        }
                    }
                    else if (phiSource.Type == OperandType.Undefined)
                    {
                        undefCount++;
                    }
                }

                // If all sources but one are undefined, we can assume that the one
                // that is not undefined is the right one.

                if (undefCount == phiNode.SourcesCount - 1)
                {
                    for (int i = phiNode.SourcesCount - 1; i >= 0; i--)
                    {
                        Operand phiSource = phiNode.GetSource(i);

                        if (phiSource.Type != OperandType.Undefined)
                        {
                            return phiSource;
                        }
                    }
                }
            }

            return source;
        }

        public static void DeleteNode(LinkedListNode<INode> node, Operation operation)
        {
            node.List.Remove(node);

            for (int srcIndex = 0; srcIndex < operation.SourcesCount; srcIndex++)
            {
                operation.SetSource(srcIndex, null);
            }

            operation.Dest = null;
        }
    }
}
