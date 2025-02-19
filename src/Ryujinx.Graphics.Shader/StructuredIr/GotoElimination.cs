using Ryujinx.Graphics.Shader.IntermediateRepresentation;
using System;
using System.Collections.Generic;
using static Ryujinx.Graphics.Shader.StructuredIr.AstHelper;

namespace Ryujinx.Graphics.Shader.StructuredIr
{
    static class GotoElimination
    {
        // This is a modified version of the algorithm presented on the paper
        // "Taming Control Flow: A Structured Approach to Eliminating Goto Statements".
        public static void Eliminate(GotoStatement[] gotos)
        {
            for (int index = gotos.Length - 1; index >= 0; index--)
            {
                GotoStatement stmt = gotos[index];

                AstBlock gBlock = ParentBlock(stmt.Goto);
                AstBlock lBlock = ParentBlock(stmt.Label);

                int gLevel = Level(gBlock);
                int lLevel = Level(lBlock);

                if (IndirectlyRelated(gBlock, lBlock, gLevel, lLevel))
                {
                    AstBlock drBlock = gBlock;

                    int drLevel = gLevel;

                    do
                    {
                        drBlock = drBlock.Parent;

                        drLevel--;
                    }
                    while (!DirectlyRelated(drBlock, lBlock, drLevel, lLevel));

                    MoveOutward(stmt, gLevel, drLevel);

                    gBlock = drBlock;
                    gLevel = drLevel;

                    if (Previous(stmt.Goto) is AstBlock elseBlock && elseBlock.Type == AstBlockType.Else)
                    {
                        // It's possible that the label was enclosed inside an else block,
                        // in this case we need to update the block and level.
                        // We also need to set the IsLoop for the case when the label is
                        // now before the goto, due to the newly introduced else block.
                        lBlock = ParentBlock(stmt.Label);

                        lLevel = Level(lBlock);

                        if (!IndirectlyRelated(elseBlock, lBlock, gLevel + 1, lLevel))
                        {
                            stmt.IsLoop = true;
                        }
                    }
                }

                if (DirectlyRelated(gBlock, lBlock, gLevel, lLevel))
                {
                    if (gLevel > lLevel)
                    {
                        MoveOutward(stmt, gLevel, lLevel);
                    }
                    else
                    {
                        if (stmt.IsLoop)
                        {
                            Lift(stmt);
                        }

                        MoveInward(stmt);
                    }
                }

                gBlock = ParentBlock(stmt.Goto);

                if (stmt.IsLoop)
                {
                    EncloseDoWhile(stmt, gBlock, stmt.Label);
                }
                else
                {
                    Enclose(gBlock, AstBlockType.If, stmt.Condition, Next(stmt.Goto), stmt.Label);
                }

                gBlock.Remove(stmt.Goto);
            }
        }

        private static bool IndirectlyRelated(AstBlock lBlock, AstBlock rBlock, int lLevel, int rlevel)
        {
            return !(lBlock == rBlock || DirectlyRelated(lBlock, rBlock, lLevel, rlevel));
        }

        private static bool DirectlyRelated(AstBlock lBlock, AstBlock rBlock, int lLevel, int rLevel)
        {
            // If the levels are equal, they can be either siblings or indirectly related.
            if (lLevel == rLevel)
            {
                return false;
            }

            IAstNode block;
            IAstNode other;

            int blockLvl, otherLvl;

            if (lLevel > rLevel)
            {
                block = lBlock;
                blockLvl = lLevel;
                other = rBlock;
                otherLvl = rLevel;
            }
            else /* if (rLevel > lLevel) */
            {
                block = rBlock;
                blockLvl = rLevel;
                other = lBlock;
                otherLvl = lLevel;
            }

            while (blockLvl >= otherLvl)
            {
                if (block == other)
                {
                    return true;
                }

                block = block.Parent;

                blockLvl--;
            }

            return false;
        }

        private static void Lift(GotoStatement stmt)
        {
            AstBlock block = ParentBlock(stmt.Goto);

            AstBlock[] path = BackwardsPath(block, ParentBlock(stmt.Label));

            AstBlock loopFirstStmt = path[^1];

            if (loopFirstStmt.Type == AstBlockType.Else)
            {
                loopFirstStmt = Previous(loopFirstStmt) as AstBlock;

                if (loopFirstStmt == null || loopFirstStmt.Type != AstBlockType.If)
                {
                    throw new InvalidOperationException("Found an else without a matching if.");
                }
            }

            AstBlock newBlock = EncloseDoWhile(stmt, block, loopFirstStmt);

            block.Remove(stmt.Goto);

            newBlock.AddFirst(stmt.Goto);

            stmt.IsLoop = false;
        }

        private static void MoveOutward(GotoStatement stmt, int gLevel, int lLevel)
        {
            AstBlock origin = ParentBlock(stmt.Goto);

            AstBlock block = origin;

            // Check if a loop is enclosing the goto, and the block that is
            // directly related to the label is above the loop block.
            // In that case, we need to introduce a break to get out of the loop.
            AstBlock loopBlock = origin;

            int loopLevel = gLevel;

            while (loopLevel > lLevel)
            {
                AstBlock child = loopBlock;

                loopBlock = loopBlock.Parent;

                loopLevel--;

                if (child.Type == AstBlockType.DoWhile)
                {
                    EncloseSingleInst(stmt, Instruction.LoopBreak);

                    block.Remove(stmt.Goto);

                    loopBlock.AddAfter(child, stmt.Goto);

                    block = loopBlock;
                    gLevel = loopLevel;
                }
            }

            // Insert ifs to skip the parts that shouldn't be executed due to the goto.
            bool tryInsertElse = stmt.IsUnconditional && origin.Type == AstBlockType.If;

            while (gLevel > lLevel)
            {
                Enclose(block, AstBlockType.If, stmt.Condition, Next(stmt.Goto));

                block.Remove(stmt.Goto);

                AstBlock child = block;

                // We can't move the goto in the middle of a if and a else block, in
                // this case we need to move it after the else.
                // IsLoop may need to be updated if the label is inside the else, as
                // introducing a loop is the only way to ensure the else will be executed.
                if (Next(child) is AstBlock elseBlock && elseBlock.Type == AstBlockType.Else)
                {
                    child = elseBlock;
                }

                block = block.Parent;

                block.AddAfter(child, stmt.Goto);

                gLevel--;

                if (tryInsertElse && child == origin)
                {
                    AstBlock lBlock = ParentBlock(stmt.Label);

                    IAstNode last = block == lBlock && !stmt.IsLoop ? stmt.Label : null;

                    AstBlock newBlock = Enclose(block, AstBlockType.Else, null, Next(stmt.Goto), last);

                    if (newBlock != null)
                    {
                        block.Remove(stmt.Goto);

                        block.AddAfter(newBlock, stmt.Goto);
                    }
                }
            }
        }

        private static void MoveInward(GotoStatement stmt)
        {
            AstBlock block = ParentBlock(stmt.Goto);

            AstBlock[] path = BackwardsPath(block, ParentBlock(stmt.Label));

            for (int index = path.Length - 1; index >= 0; index--)
            {
                AstBlock child = path[index];
                AstBlock last = child;

                if (child.Type == AstBlockType.If)
                {
                    // Modify the if condition to allow it to be entered by the goto.
                    if (!ContainsCondComb(child.Condition, Instruction.LogicalOr, stmt.Condition))
                    {
                        child.OrCondition(stmt.Condition);
                    }
                }
                else if (child.Type == AstBlockType.Else)
                {
                    // Modify the matching if condition to force the else to be entered by the goto.
                    if (Previous(child) is not AstBlock ifBlock || ifBlock.Type != AstBlockType.If)
                    {
                        throw new InvalidOperationException("Found an else without a matching if.");
                    }

                    IAstNode cond = InverseCond(stmt.Condition);

                    if (!ContainsCondComb(ifBlock.Condition, Instruction.LogicalAnd, cond))
                    {
                        ifBlock.AndCondition(cond);
                    }

                    last = ifBlock;
                }

                Enclose(block, AstBlockType.If, stmt.Condition, Next(stmt.Goto), last);

                block.Remove(stmt.Goto);

                child.AddFirst(stmt.Goto);

                block = child;
            }
        }

        private static bool ContainsCondComb(IAstNode node, Instruction inst, IAstNode newCond)
        {
            while (node is AstOperation operation && operation.SourcesCount == 2)
            {
                if (operation.Inst == inst && IsSameCond(operation.GetSource(1), newCond))
                {
                    return true;
                }

                node = operation.GetSource(0);
            }

            return false;
        }

        private static AstBlock EncloseDoWhile(GotoStatement stmt, AstBlock block, IAstNode first)
        {
            if (block.Type == AstBlockType.DoWhile && first == block.First)
            {
                // We only need to insert the continue if we're not at the end of the loop,
                // or if our condition is different from the loop condition.
                if (Next(stmt.Goto) != null || block.Condition != stmt.Condition)
                {
                    EncloseSingleInst(stmt, Instruction.LoopContinue);
                }

                // Modify the do-while condition to allow it to continue.
                if (!ContainsCondComb(block.Condition, Instruction.LogicalOr, stmt.Condition))
                {
                    block.OrCondition(stmt.Condition);
                }

                return block;
            }

            return Enclose(block, AstBlockType.DoWhile, stmt.Condition, first, stmt.Goto);
        }

        private static void EncloseSingleInst(GotoStatement stmt, Instruction inst)
        {
            AstBlock block = ParentBlock(stmt.Goto);

            AstBlock newBlock = new(AstBlockType.If, stmt.Condition);

            block.AddAfter(stmt.Goto, newBlock);

            newBlock.AddFirst(new AstOperation(inst));
        }

        private static AstBlock Enclose(
            AstBlock block,
            AstBlockType type,
            IAstNode cond,
            IAstNode first,
            IAstNode last = null)
        {
            if (first == last)
            {
                return null;
            }

            if (type == AstBlockType.If)
            {
                cond = InverseCond(cond);
            }

            // Do a quick check, if we are enclosing a single block,
            // and the block type/condition matches the one we're going
            // to create, then we don't need a new block, we can just
            // return the old one.
            bool hasSingleNode = Next(first) == last;

            if (hasSingleNode && BlockMatches(first, type, cond))
            {
                return first as AstBlock;
            }

            AstBlock newBlock = new(type, cond);

            block.AddBefore(first, newBlock);

            while (first != last)
            {
                IAstNode next = Next(first);

                block.Remove(first);

                newBlock.Add(first);

                first = next;
            }

            return newBlock;
        }

        private static bool BlockMatches(IAstNode node, AstBlockType type, IAstNode cond)
        {
            if (node is not AstBlock block)
            {
                return false;
            }

            return block.Type == type && IsSameCond(block.Condition, cond);
        }

        private static bool IsSameCond(IAstNode lCond, IAstNode rCond)
        {
            if (lCond is AstOperation lCondOp && lCondOp.Inst == Instruction.LogicalNot)
            {
                if (rCond is not AstOperation rCondOp || rCondOp.Inst != lCondOp.Inst)
                {
                    return false;
                }

                lCond = lCondOp.GetSource(0);
                rCond = rCondOp.GetSource(0);
            }

            return lCond == rCond;
        }

        private static AstBlock ParentBlock(IAstNode node)
        {
            if (node is AstBlock block)
            {
                return block.Parent;
            }

            while (node is not AstBlock)
            {
                node = node.Parent;
            }

            return node as AstBlock;
        }

        private static AstBlock[] BackwardsPath(AstBlock top, AstBlock bottom)
        {
            AstBlock block = bottom;

            List<AstBlock> path = [];

            while (block != top)
            {
                path.Add(block);

                block = block.Parent;
            }

            return path.ToArray();
        }

        private static int Level(IAstNode node)
        {
            int level = 0;

            while (node != null)
            {
                level++;

                node = node.Parent;
            }

            return level;
        }
    }
}
