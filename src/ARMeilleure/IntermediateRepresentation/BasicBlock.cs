using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace ARMeilleure.IntermediateRepresentation
{
    class BasicBlock : IEquatable<BasicBlock>, IIntrusiveListNode<BasicBlock>
    {
        private const uint MaxSuccessors = 2;

        private int _succCount;
        private BasicBlock _succ0;
        private readonly BasicBlock _succ1;
        private HashSet<BasicBlock> _domFrontiers;

        public int Index { get; set; }
        public BasicBlockFrequency Frequency { get; set; }
        public BasicBlock ListPrevious { get; set; }
        public BasicBlock ListNext { get; set; }
        public IntrusiveList<Operation> Operations { get; }
        public List<BasicBlock> Predecessors { get; }
        public BasicBlock ImmediateDominator { get; set; }

        public int SuccessorsCount => _succCount;

        public HashSet<BasicBlock> DominanceFrontiers
        {
            get
            {
                _domFrontiers ??= [];

                return _domFrontiers;
            }
        }

        public BasicBlock() : this(index: -1) { }

        public BasicBlock(int index)
        {
            Operations = new IntrusiveList<Operation>();
            Predecessors = [];

            Index = index;
        }

        public void AddSuccessor(BasicBlock block)
        {
            ArgumentNullException.ThrowIfNull(block);

            if ((uint)_succCount + 1 > MaxSuccessors)
            {
                ThrowSuccessorOverflow();
            }

            block.Predecessors.Add(this);

            GetSuccessorUnsafe(_succCount++) = block;
        }

        public void RemoveSuccessor(int index)
        {
            if ((uint)index >= (uint)_succCount)
            {
                ThrowOutOfRange(nameof(index));
            }

            ref BasicBlock oldBlock = ref GetSuccessorUnsafe(index);

            oldBlock.Predecessors.Remove(this);
            oldBlock = null;

            if (index == 0)
            {
                _succ0 = _succ1;
            }

            _succCount--;
        }

        public BasicBlock GetSuccessor(int index)
        {
            if ((uint)index >= (uint)_succCount)
            {
                ThrowOutOfRange(nameof(index));
            }

            return GetSuccessorUnsafe(index);
        }

        private ref BasicBlock GetSuccessorUnsafe(int index)
        {
            return ref Unsafe.Add(ref _succ0, index);
        }

        public void SetSuccessor(int index, BasicBlock block)
        {
            ArgumentNullException.ThrowIfNull(block);

            if ((uint)index >= (uint)_succCount)
            {
                ThrowOutOfRange(nameof(index));
            }

            ref BasicBlock oldBlock = ref GetSuccessorUnsafe(index);

            oldBlock.Predecessors.Remove(this);
            block.Predecessors.Add(this);

            oldBlock = block;
        }

        public void Append(Operation node)
        {
            Operation last = Operations.Last;

            // Append node before terminal or to end if no terminal.
            if (last == default)
            {
                Operations.AddLast(node);

                return;
            }

            switch (last.Instruction)
            {
                case Instruction.Return:
                case Instruction.Tailcall:
                case Instruction.BranchIf:
                    Operations.AddBefore(last, node);
                    break;

                default:
                    Operations.AddLast(node);
                    break;
            }
        }

        private static void ThrowOutOfRange(string name) => throw new ArgumentOutOfRangeException(name);
        private static void ThrowSuccessorOverflow() => throw new OverflowException($"BasicBlock can only have {MaxSuccessors} successors.");

        public bool Equals(BasicBlock other)
        {
            return other == this;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as BasicBlock);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
