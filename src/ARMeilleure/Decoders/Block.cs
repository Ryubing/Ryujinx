using System;
using System.Collections.Generic;

namespace ARMeilleure.Decoders
{
    class Block
    {
        public ulong Address { get; set; }
        public ulong EndAddress { get; set; }

        public Block Next { get; set; }
        public Block Branch { get; set; }

        public bool Exit { get; set; }

        public List<OpCode> OpCodes { get; }

        public Block()
        {
            OpCodes = [];
        }

        public Block(ulong address) : this()
        {
            Address = address;
        }

        public void Split(Block rightBlock)
        {
            int splitIndex = BinarySearch(OpCodes, rightBlock.Address);

            if (OpCodes[splitIndex].Address < rightBlock.Address)
            {
                splitIndex++;
            }

            int splitCount = OpCodes.Count - splitIndex;

            if (splitCount <= 0)
            {
                throw new ArgumentException("Can't split at right block address.");
            }

            rightBlock.EndAddress = EndAddress;

            rightBlock.Next = Next;
            rightBlock.Branch = Branch;

            rightBlock.OpCodes.AddRange(OpCodes.GetRange(splitIndex, splitCount));

            EndAddress = rightBlock.Address;

            Next = rightBlock;
            Branch = null;

            OpCodes.RemoveRange(splitIndex, splitCount);
        }

        private static int BinarySearch(List<OpCode> opCodes, ulong address)
        {
            int left = 0;
            int middle = 0;
            int right = opCodes.Count - 1;

            while (left <= right)
            {
                int size = right - left;

                middle = left + (size >> 1);

                OpCode opCode = opCodes[middle];

                if (address == (ulong)opCode.Address)
                {
                    break;
                }

                if (address < (ulong)opCode.Address)
                {
                    right = middle - 1;
                }
                else
                {
                    left = middle + 1;
                }
            }

            return middle;
        }

        public OpCode GetLastOp()
        {
            if (OpCodes.Count > 0)
            {
                return OpCodes[^1];
            }

            return null;
        }
    }
}
