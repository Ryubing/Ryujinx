using System;
using System.Collections.Generic;

namespace ARMeilleure.Decoders.Optimizations
{
    static class TailCallRemover
    {
        public static Block[] RunPass(ulong entryAddress, List<Block> blocks)
        {
            // Detect tail calls:
            // - Assume this function spans the space covered by contiguous code blocks surrounding the entry address.
            // - A jump to an area outside this contiguous region will be treated as an exit block.
            // - Include a small allowance for jumps outside the contiguous range.

            if (!Decoder.BinarySearch(blocks, entryAddress, out int entryBlockId))
            {
                throw new InvalidOperationException("Function entry point is not contained in a block.");
            }

            const ulong Allowance = 4;

            Block entryBlock = blocks[entryBlockId];

            Block startBlock = entryBlock;
            Block endBlock = entryBlock;

            int startBlockIndex = entryBlockId;
            int endBlockIndex = entryBlockId;

            for (int i = entryBlockId + 1; i < blocks.Count; i++) // Search forwards.
            {
                Block block = blocks[i];

                if (endBlock.EndAddress < block.Address - Allowance)
                {
                    break; // End of contiguous function.
                }

                endBlock = block;
                endBlockIndex = i;
            }

            for (int i = entryBlockId - 1; i >= 0; i--) // Search backwards.
            {
                Block block = blocks[i];

                if (startBlock.Address > block.EndAddress + Allowance)
                {
                    break; // End of contiguous function.
                }

                startBlock = block;
                startBlockIndex = i;
            }

            if (startBlockIndex == 0 && endBlockIndex == blocks.Count - 1)
            {
                return blocks.ToArray(); // Nothing to do here.
            }

            // Mark branches whose target is outside of the contiguous region as an exit block.
            for (int i = startBlockIndex; i <= endBlockIndex; i++)
            {
                Block block = blocks[i];

                if (block.Branch != null && (block.Branch.Address > endBlock.EndAddress || block.Branch.EndAddress < startBlock.Address))
                {
                    block.Branch.Exit = true;
                }
            }

            List<Block> newBlocks = new(blocks.Count);

            // Finally, rebuild decoded block list, ignoring blocks outside the contiguous range.
            for (int i = 0; i < blocks.Count; i++)
            {
                Block block = blocks[i];

                if (block.Exit || (i >= startBlockIndex && i <= endBlockIndex))
                {
                    newBlocks.Add(block);
                }
            }

            return newBlocks.ToArray();
        }
    }
}
