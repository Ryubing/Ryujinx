using Ryujinx.Graphics.Shader.IntermediateRepresentation;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ryujinx.Graphics.Shader.Decoders
{
    class PushOpInfo
    {
        public InstOp Op { get; }
        public Dictionary<Block, Operand> Consumers;

        public PushOpInfo(InstOp op)
        {
            Op = op;
            Consumers = new Dictionary<Block, Operand>();
        }
    }

    readonly struct SyncTarget
    {
        public PushOpInfo PushOpInfo { get; }
        public int PushOpId { get; }

        public SyncTarget(PushOpInfo pushOpInfo, int pushOpId)
        {
            PushOpInfo = pushOpInfo;
            PushOpId = pushOpId;
        }
    }

    class Block
    {
        public ulong Address { get; set; }
        public ulong EndAddress { get; set; }

        public List<Block> Predecessors { get; }
        public List<Block> Successors { get; }

        public List<InstOp> OpCodes { get; }
        public List<PushOpInfo> PushOpCodes { get; }
        public Dictionary<ulong, SyncTarget> SyncTargets { get; }

        public Block(ulong address)
        {
            Address = address;

            Predecessors = [];
            Successors = [];

            OpCodes = [];
            PushOpCodes = [];
            SyncTargets = new Dictionary<ulong, SyncTarget>();
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
            rightBlock.Successors.AddRange(Successors);
            rightBlock.Predecessors.Add(this);

            EndAddress = rightBlock.Address;

            Successors.Clear();
            Successors.Add(rightBlock);

            // Move ops.
            rightBlock.OpCodes.AddRange(OpCodes.GetRange(splitIndex, splitCount));

            OpCodes.RemoveRange(splitIndex, splitCount);

            // Update push consumers that points to this block.
            foreach (SyncTarget syncTarget in SyncTargets.Values)
            {
                PushOpInfo pushOpInfo = syncTarget.PushOpInfo;

                Operand local = pushOpInfo.Consumers[this];
                pushOpInfo.Consumers.Remove(this);
                pushOpInfo.Consumers.Add(rightBlock, local);
            }

            foreach ((ulong key, SyncTarget value) in SyncTargets)
            {
                rightBlock.SyncTargets.Add(key, value);
            }

            SyncTargets.Clear();

            // Move push ops.
            for (int i = 0; i < PushOpCodes.Count; i++)
            {
                if (PushOpCodes[i].Op.Address >= rightBlock.Address)
                {
                    int count = PushOpCodes.Count - i;
                    rightBlock.PushOpCodes.AddRange(PushOpCodes.Skip(i));
                    PushOpCodes.RemoveRange(i, count);
                    break;
                }
            }
        }

        private static int BinarySearch(List<InstOp> opCodes, ulong address)
        {
            int left = 0;
            int middle = 0;
            int right = opCodes.Count - 1;

            while (left <= right)
            {
                int size = right - left;

                middle = left + (size >> 1);

                InstOp opCode = opCodes[middle];

                if (address == opCode.Address)
                {
                    break;
                }

                if (address < opCode.Address)
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

        public InstOp GetLastOp()
        {
            if (OpCodes.Count != 0)
            {
                return OpCodes[^1];
            }

            return default;
        }

        public bool HasNext()
        {
            InstOp lastOp = GetLastOp();
            return OpCodes.Count != 0 && !Decoder.IsUnconditionalBranch(ref lastOp);
        }

        public void AddPushOp(InstOp op)
        {
            PushOpCodes.Add(new PushOpInfo(op));
        }
    }
}
