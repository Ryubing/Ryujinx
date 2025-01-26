using Ryujinx.Cpu.LightningJit.Graph;
using System;
using System.Collections.Generic;

namespace Ryujinx.Cpu.LightningJit.Arm64
{
    class MultiBlock : IBlockList
    {
        public readonly List<Block> Blocks;
        public readonly RegisterMask[] ReadMasks;
        public readonly RegisterMask[] WriteMasks;
        public readonly RegisterMask GlobalUseMask;
        public readonly bool HasHostCall;
        public readonly bool HasMemoryInstruction;
        public readonly bool IsTruncated;

        public int Count => Blocks.Count;

        public IBlock this[int index] => Blocks[index];

        public MultiBlock(List<Block> blocks, RegisterMask globalUseMask, bool hasHostCall, bool hasMemoryInstruction)
        {
            Blocks = blocks;

            (ReadMasks, WriteMasks) = DataFlow.GetGlobalUses(this);

            GlobalUseMask = globalUseMask;
            HasHostCall = hasHostCall;
            HasMemoryInstruction = hasMemoryInstruction;
            IsTruncated = blocks[^1].IsTruncated;
        }

        public void PrintDebugInfo()
        {
            foreach (Block block in Blocks)
            {
                Console.WriteLine($"bb {block.Index}");

                List<int> predList = [];
                List<int> succList = [];

                for (int index = 0; index < block.PredecessorsCount; index++)
                {
                    predList.Add(block.GetPredecessor(index).Index);
                }

                for (int index = 0; index < block.SuccessorsCount; index++)
                {
                    succList.Add(block.GetSuccessor(index).Index);
                }

                Console.WriteLine($" predecessors: {string.Join(' ', predList)}");
                Console.WriteLine($" successors: {string.Join(' ', succList)}");
                Console.WriteLine($" gpr read mask: 0x{ReadMasks[block.Index].GprMask:X} 0x{block.ComputeUseMasks().Read.GprMask:X}");
                Console.WriteLine($" gpr write mask: 0x{WriteMasks[block.Index].GprMask:X}");

                for (int index = 0; index < block.Instructions.Count; index++)
                {
                    Console.WriteLine($"  {index} 0x{block.Instructions[index].Encoding:X8} {block.Instructions[index].Name}");
                }
            }
        }
    }
}
