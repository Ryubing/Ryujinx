using ARMeilleure.IntermediateRepresentation;
using System.Diagnostics;

namespace ARMeilleure.Translation
{
    static class Dominance
    {
        // Those methods are an implementation of the algorithms on "A Simple, Fast Dominance Algorithm".
        // https://www.cs.rice.edu/~keith/EMBED/dom.pdf
        public static void FindDominators(ControlFlowGraph cfg)
        {
            BasicBlock Intersect(BasicBlock block1, BasicBlock block2)
            {
                while (block1 != block2)
                {
                    while (cfg.PostOrderMap[block1.Index] < cfg.PostOrderMap[block2.Index])
                    {
                        block1 = block1.ImmediateDominator;
                    }

                    while (cfg.PostOrderMap[block2.Index] < cfg.PostOrderMap[block1.Index])
                    {
                        block2 = block2.ImmediateDominator;
                    }
                }

                return block1;
            }

            cfg.Entry.ImmediateDominator = cfg.Entry;

            Debug.Assert(cfg.Entry == cfg.PostOrderBlocks[^1]);

            bool modified;

            do
            {
                modified = false;

                for (int blkIndex = cfg.PostOrderBlocks.Length - 2; blkIndex >= 0; blkIndex--)
                {
                    BasicBlock block = cfg.PostOrderBlocks[blkIndex];

                    BasicBlock newIDom = null;

                    foreach (BasicBlock predecessor in block.Predecessors)
                    {
                        if (predecessor.ImmediateDominator != null)
                        {
                            if (newIDom != null)
                            {
                                newIDom = Intersect(predecessor, newIDom);
                            }
                            else
                            {
                                newIDom = predecessor;
                            }
                        }
                    }

                    if (block.ImmediateDominator != newIDom)
                    {
                        block.ImmediateDominator = newIDom;

                        modified = true;
                    }
                }
            }
            while (modified);
        }

        public static void FindDominanceFrontiers(ControlFlowGraph cfg)
        {
            for (BasicBlock block = cfg.Blocks.First; block != null; block = block.ListNext)
            {
                if (block.Predecessors.Count < 2)
                {
                    continue;
                }
                
                for (int pBlkIndex = 0; pBlkIndex < block.Predecessors.Count; pBlkIndex++)
                {
                    BasicBlock current = block.Predecessors[pBlkIndex];

                    while (current != block.ImmediateDominator)
                    {
                        current.DominanceFrontiers.Add(block);

                        current = current.ImmediateDominator;
                    }
                }
            }
        }
    }
}
