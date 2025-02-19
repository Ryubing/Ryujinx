using Ryujinx.Graphics.Shader.IntermediateRepresentation;
using System.Collections.Generic;

namespace Ryujinx.Graphics.Shader.Translation
{
    class ControlFlowGraph
    {
        public BasicBlock[] Blocks { get; }
        public BasicBlock[] PostOrderBlocks { get; }
        public int[] PostOrderMap { get; }

        public ControlFlowGraph(BasicBlock[] blocks)
        {
            Blocks = blocks;

            HashSet<BasicBlock> visited = [];

            Stack<BasicBlock> blockStack = new();

            List<BasicBlock> postOrderBlocks = new(blocks.Length);

            PostOrderMap = new int[blocks.Length];

            visited.Add(blocks[0]);

            blockStack.Push(blocks[0]);

            while (blockStack.TryPop(out BasicBlock block))
            {
                if (block.Next != null && visited.Add(block.Next))
                {
                    blockStack.Push(block);
                    blockStack.Push(block.Next);
                }
                else if (block.Branch != null && visited.Add(block.Branch))
                {
                    blockStack.Push(block);
                    blockStack.Push(block.Branch);
                }
                else
                {
                    PostOrderMap[block.Index] = postOrderBlocks.Count;

                    postOrderBlocks.Add(block);
                }
            }

            PostOrderBlocks = postOrderBlocks.ToArray();
        }

        public static ControlFlowGraph Create(Operation[] operations)
        {
            Dictionary<Operand, BasicBlock> labels = new();

            List<BasicBlock> blocks = [];

            BasicBlock currentBlock = null;

            void NextBlock(BasicBlock nextBlock)
            {
                if (currentBlock != null && !EndsWithUnconditionalInst(currentBlock.GetLastOp()))
                {
                    currentBlock.Next = nextBlock;
                }

                currentBlock = nextBlock;
            }

            void NewNextBlock()
            {
                BasicBlock block = new(blocks.Count);

                blocks.Add(block);

                NextBlock(block);
            }

            bool needsNewBlock = true;

            for (int index = 0; index < operations.Length; index++)
            {
                Operation operation = operations[index];

                if (operation.Inst == Instruction.MarkLabel)
                {
                    Operand label = operation.Dest;

                    if (labels.TryGetValue(label, out BasicBlock nextBlock))
                    {
                        nextBlock.Index = blocks.Count;

                        blocks.Add(nextBlock);

                        NextBlock(nextBlock);
                    }
                    else
                    {
                        NewNextBlock();

                        labels.Add(label, currentBlock);
                    }
                }
                else
                {
                    if (needsNewBlock)
                    {
                        NewNextBlock();
                    }

                    currentBlock.Operations.AddLast(operation);
                }

                needsNewBlock = operation.Inst == Instruction.Branch ||
                                operation.Inst == Instruction.BranchIfTrue ||
                                operation.Inst == Instruction.BranchIfFalse;

                if (needsNewBlock)
                {
                    Operand label = operation.Dest;

                    if (!labels.TryGetValue(label, out BasicBlock branchBlock))
                    {
                        branchBlock = new BasicBlock();

                        labels.Add(label, branchBlock);
                    }

                    currentBlock.Branch = branchBlock;
                }
            }

            // Remove unreachable blocks.
            bool hasUnreachable;

            do
            {
                hasUnreachable = false;

                for (int blkIndex = 1; blkIndex < blocks.Count; blkIndex++)
                {
                    BasicBlock block = blocks[blkIndex];

                    if (block.Predecessors.Count == 0)
                    {
                        block.Next = null;
                        block.Branch = null;
                        blocks.RemoveAt(blkIndex--);
                        hasUnreachable = true;
                    }
                    else
                    {
                        block.Index = blkIndex;
                    }
                }
            } while (hasUnreachable);

            return new ControlFlowGraph(blocks.ToArray());
        }

        private static bool EndsWithUnconditionalInst(INode node)
        {
            if (node is Operation operation)
            {
                switch (operation.Inst)
                {
                    case Instruction.Branch:
                    case Instruction.Discard:
                    case Instruction.Return:
                        return true;
                }
            }

            return false;
        }
    }
}
