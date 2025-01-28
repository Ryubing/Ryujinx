using Ryujinx.Cpu.LightningJit.Graph;
using System.Collections.Generic;
using System.Diagnostics;

namespace Ryujinx.Cpu.LightningJit.Arm64
{
    class Block : IBlock
    {
        public int Index { get; private set; }

        private readonly List<Block> _predecessors;
        private readonly List<Block> _successors;

        public int PredecessorsCount => _predecessors.Count;
        public int SuccessorsCount => _successors.Count;

        public readonly ulong Address;
        public readonly ulong EndAddress;
        public readonly List<InstInfo> Instructions;
        public readonly bool EndsWithBranch;
        public readonly bool IsTruncated;
        public readonly bool IsLoopEnd;

        public Block(ulong address, ulong endAddress, List<InstInfo> instructions, bool endsWithBranch, bool isTruncated, bool isLoopEnd)
        {
            Debug.Assert((int)((endAddress - address) / 4) == instructions.Count);

            _predecessors = [];
            _successors = [];
            Address = address;
            EndAddress = endAddress;
            Instructions = instructions;
            EndsWithBranch = endsWithBranch;
            IsTruncated = isTruncated;
            IsLoopEnd = isLoopEnd;
        }

        public (Block, Block) SplitAtAddress(ulong address)
        {
            int splitIndex = (int)((address - Address) / 4);
            int splitCount = Instructions.Count - splitIndex;

            // Technically those are valid, but we don't want to create empty blocks.
            Debug.Assert(splitIndex != 0);
            Debug.Assert(splitCount != 0);

            Block leftBlock = new(
                Address,
                address,
                Instructions.GetRange(0, splitIndex),
                false,
                false,
                false);

            Block rightBlock = new(
                address,
                EndAddress,
                Instructions.GetRange(splitIndex, splitCount),
                EndsWithBranch,
                IsTruncated,
                IsLoopEnd);

            return (leftBlock, rightBlock);
        }

        public void Number(int index)
        {
            Index = index;
        }

        public void AddSuccessor(Block block)
        {
            if (!_successors.Contains(block))
            {
                _successors.Add(block);
            }
        }

        public void AddPredecessor(Block block)
        {
            if (!_predecessors.Contains(block))
            {
                _predecessors.Add(block);
            }
        }

        public IBlock GetSuccessor(int index)
        {
            return _successors[index];
        }

        public IBlock GetPredecessor(int index)
        {
            return _predecessors[index];
        }

        public RegisterUse ComputeUseMasks()
        {
            if (Instructions.Count == 0)
            {
                return new(0u, 0u, 0u, 0u, 0u, 0u);
            }

            RegisterUse use = Instructions[0].RegisterUse;

            for (int index = 1; index < Instructions.Count; index++)
            {
                RegisterUse currentUse = Instructions[index].RegisterUse;

                use = new(use.Read | (currentUse.Read & ~use.Write), use.Write | currentUse.Write);
            }

            return use;
        }

        public bool EndsWithContextLoad()
        {
            return !IsTruncated && EndsWithContextStoreAndLoad();
        }

        public bool EndsWithContextStore()
        {
            return EndsWithContextStoreAndLoad();
        }

        private bool EndsWithContextStoreAndLoad()
        {
            if (Instructions.Count == 0)
            {
                return false;
            }

            InstName lastInstructionName = Instructions[^1].Name;

            return lastInstructionName.IsCall() || lastInstructionName.IsException();
        }
    }
}
