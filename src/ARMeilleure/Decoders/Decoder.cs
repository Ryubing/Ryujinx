using ARMeilleure.Decoders.Optimizations;
using ARMeilleure.Instructions;
using ARMeilleure.Memory;
using ARMeilleure.State;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace ARMeilleure.Decoders
{
    static class Decoder
    {
        // We define a limit on the number of instructions that a function may have,
        // this prevents functions being potentially too large, which would
        // take too long to compile and use too much memory.
        private const int MaxInstsPerFunction = 2500;

        // For lower code quality translation, we set a lower limit since we're blocking execution.
        private const int MaxInstsPerFunctionLowCq = 500;

        public static Block[] Decode(IMemoryManager memory, ulong address, ExecutionMode mode, bool highCq, DecoderMode dMode)
        {
            List<Block> blocks = [];

            Queue<Block> workQueue = new();

            Dictionary<ulong, Block> visited = new();

            Debug.Assert(MaxInstsPerFunctionLowCq <= MaxInstsPerFunction);

            int opsCount = 0;

            int instructionLimit = highCq ? MaxInstsPerFunction : MaxInstsPerFunctionLowCq;

            Block GetBlock(ulong blkAddress)
            {
                if (!visited.TryGetValue(blkAddress, out Block block))
                {
                    block = new Block(blkAddress);

                    if ((dMode != DecoderMode.MultipleBlocks && visited.Count >= 1) ||
                        opsCount > instructionLimit ||
                        (visited.Count > 0 && !memory.IsMapped(blkAddress)))
                    {
                        block.Exit = true;
                        block.EndAddress = blkAddress;
                    }

                    workQueue.Enqueue(block);

                    visited.Add(blkAddress, block);
                }

                return block;
            }

            GetBlock(address);

            while (workQueue.TryDequeue(out Block currBlock))
            {
                // Check if the current block is inside another block.
                if (BinarySearch(blocks, currBlock.Address, out int nBlkIndex))
                {
                    Block nBlock = blocks[nBlkIndex];

                    if (nBlock.Address == currBlock.Address)
                    {
                        throw new InvalidOperationException("Found duplicate block address on the list.");
                    }

                    currBlock.Exit = false;

                    nBlock.Split(currBlock);

                    blocks.Insert(nBlkIndex + 1, currBlock);

                    continue;
                }

                if (!currBlock.Exit)
                {
                    // If we have a block after the current one, set the limit address.
                    ulong limitAddress = ulong.MaxValue;

                    if (nBlkIndex != blocks.Count)
                    {
                        Block nBlock = blocks[nBlkIndex];

                        int nextIndex = nBlkIndex + 1;

                        if (nBlock.Address < currBlock.Address && nextIndex < blocks.Count)
                        {
                            limitAddress = blocks[nextIndex].Address;
                        }
                        else if (nBlock.Address > currBlock.Address)
                        {
                            limitAddress = blocks[nBlkIndex].Address;
                        }
                    }

                    if (dMode == DecoderMode.SingleInstruction)
                    {
                        // Only read at most one instruction
                        limitAddress = currBlock.Address + 1;
                    }

                    FillBlock(memory, mode, currBlock, limitAddress);

                    opsCount += currBlock.OpCodes.Count;

                    if (currBlock.OpCodes.Count != 0)
                    {
                        // Set child blocks. "Branch" is the block the branch instruction
                        // points to (when taken), "Next" is the block at the next address,
                        // executed when the branch is not taken. For Unconditional Branches
                        // (except BL/BLR that are sub calls) or end of executable, Next is null.
                        OpCode lastOp = currBlock.GetLastOp();

                        bool isCall = IsCall(lastOp);

                        if (lastOp is IOpCodeBImm op && !isCall)
                        {
                            currBlock.Branch = GetBlock((ulong)op.Immediate);
                        }

                        if (isCall || !(IsUnconditionalBranch(lastOp) || IsTrap(lastOp)))
                        {
                            currBlock.Next = GetBlock(currBlock.EndAddress);
                        }
                    }
                }

                // Insert the new block on the list (sorted by address).
                if (blocks.Count != 0)
                {
                    Block nBlock = blocks[nBlkIndex];

                    blocks.Insert(nBlkIndex + (nBlock.Address < currBlock.Address ? 1 : 0), currBlock);
                }
                else
                {
                    blocks.Add(currBlock);
                }
            }

            if (blocks.Count == 1 && blocks[0].OpCodes.Count == 0)
            {
                Debug.Assert(blocks[0].Exit);
                Debug.Assert(blocks[0].Address == blocks[0].EndAddress);

                throw new InvalidOperationException($"Decoded a single empty exit block. Entry point = 0x{address:X}.");
            }

            if (dMode == DecoderMode.MultipleBlocks)
            {
                return TailCallRemover.RunPass(address, blocks);
            }
            else
            {
                return blocks.ToArray();
            }
        }

        public static bool BinarySearch(List<Block> blocks, ulong address, out int index)
        {
            index = 0;

            int left = 0;
            int right = blocks.Count - 1;

            while (left <= right)
            {
                int size = right - left;

                int middle = left + (size >> 1);

                Block block = blocks[middle];

                index = middle;

                if (address >= block.Address && address < block.EndAddress)
                {
                    return true;
                }

                if (address < block.Address)
                {
                    right = middle - 1;
                }
                else
                {
                    left = middle + 1;
                }
            }

            return false;
        }

        private static void FillBlock(
            IMemoryManager memory,
            ExecutionMode mode,
            Block block,
            ulong limitAddress)
        {
            ulong address = block.Address;
            int itBlockSize = 0;

            OpCode opCode;

            do
            {
                if (address >= limitAddress && itBlockSize == 0)
                {
                    break;
                }

                opCode = DecodeOpCode(memory, address, mode);

                block.OpCodes.Add(opCode);

                address += (ulong)opCode.OpCodeSizeInBytes;

                if (opCode is OpCodeT16IfThen it)
                {
                    itBlockSize = it.IfThenBlockSize;
                }
                else if (itBlockSize > 0)
                {
                    itBlockSize--;
                }
            }
            while (!(IsBranch(opCode) || IsException(opCode)));

            block.EndAddress = address;
        }

        private static bool IsBranch(OpCode opCode)
        {
            return opCode is OpCodeBImm ||
                   opCode is OpCodeBReg || IsAarch32Branch(opCode);
        }

        private static bool IsUnconditionalBranch(OpCode opCode)
        {
            return opCode is OpCodeBImmAl ||
                   opCode is OpCodeBReg || IsAarch32UnconditionalBranch(opCode);
        }

        private static bool IsAarch32UnconditionalBranch(OpCode opCode)
        {
            if (opCode is not OpCode32 op)
            {
                return false;
            }

            // Compare and branch instructions are always conditional.
            if (opCode.Instruction.Name == InstName.Cbz ||
                opCode.Instruction.Name == InstName.Cbnz)
            {
                return false;
            }

            // Note: On ARM32, most instructions have conditional execution,
            // so there's no "Always" (unconditional) branch like on ARM64.
            // We need to check if the condition is "Always" instead.
            return IsAarch32Branch(op) && op.Cond >= Condition.Al;
        }

        private static bool IsAarch32Branch(OpCode opCode)
        {
            // Note: On ARM32, most ALU operations can write to R15 (PC),
            // so we must consider such operations as a branch in potential aswell.
            if (opCode is IOpCode32Alu opAlu && opAlu.Rd == RegisterAlias.Aarch32Pc)
            {
                if (opCode is OpCodeT32)
                {
                    return opCode.Instruction.Name != InstName.Tst && opCode.Instruction.Name != InstName.Teq &&
                           opCode.Instruction.Name != InstName.Cmp && opCode.Instruction.Name != InstName.Cmn;
                }
                return true;
            }

            // Same thing for memory operations. We have the cases where PC is a target
            // register (Rt == 15 or (mask & (1 << 15)) != 0), and cases where there is
            // a write back to PC (wback == true && Rn == 15), however the later may
            // be "undefined" depending on the CPU, so compilers should not produce that.
            if (opCode is IOpCode32Mem || opCode is IOpCode32MemMult)
            {
                int rt, rn;

                bool wBack, isLoad;

                if (opCode is IOpCode32Mem opMem)
                {
                    rt = opMem.Rt;
                    rn = opMem.Rn;
                    wBack = opMem.WBack;
                    isLoad = opMem.IsLoad;

                    // For the dual load, we also need to take into account the
                    // case were Rt2 == 15 (PC).
                    if (rt == 14 && opMem.Instruction.Name == InstName.Ldrd)
                    {
                        rt = RegisterAlias.Aarch32Pc;
                    }
                }
                else if (opCode is IOpCode32MemMult opMemMult)
                {
                    const int PCMask = 1 << RegisterAlias.Aarch32Pc;

                    rt = (opMemMult.RegisterMask & PCMask) != 0 ? RegisterAlias.Aarch32Pc : 0;
                    rn = opMemMult.Rn;
                    wBack = opMemMult.PostOffset != 0;
                    isLoad = opMemMult.IsLoad;
                }
                else
                {
                    throw new NotImplementedException($"The type \"{opCode.GetType().Name}\" is not implemented on the decoder.");
                }

                if ((rt == RegisterAlias.Aarch32Pc && isLoad) ||
                    (rn == RegisterAlias.Aarch32Pc && wBack))
                {
                    return true;
                }
            }

            // Explicit branch instructions.
            return opCode is IOpCode32BImm ||
                   opCode is IOpCode32BReg;
        }

        private static bool IsCall(OpCode opCode)
        {
            return opCode.Instruction.Name == InstName.Bl ||
                   opCode.Instruction.Name == InstName.Blr ||
                   opCode.Instruction.Name == InstName.Blx;
        }

        private static bool IsException(OpCode opCode)
        {
            return IsTrap(opCode) || opCode.Instruction.Name == InstName.Svc;
        }

        private static bool IsTrap(OpCode opCode)
        {
            return opCode.Instruction.Name == InstName.Brk ||
                   opCode.Instruction.Name == InstName.Trap ||
                   opCode.Instruction.Name == InstName.Und;
        }

        public static OpCode DecodeOpCode(IMemoryManager memory, ulong address, ExecutionMode mode)
        {
            int opCode = memory.Read<int>(address);

            InstDescriptor inst;

            OpCodeTable.MakeOp makeOp;

            if (mode == ExecutionMode.Aarch64)
            {
                (inst, makeOp) = OpCodeTable.GetInstA64(opCode);
            }
            else
            {
                if (mode == ExecutionMode.Aarch32Arm)
                {
                    (inst, makeOp) = OpCodeTable.GetInstA32(opCode);
                }
                else /* if (mode == ExecutionMode.Aarch32Thumb) */
                {
                    (inst, makeOp) = OpCodeTable.GetInstT32(opCode);
                }
            }

            if (makeOp != null)
            {
                return makeOp(inst, address, opCode);
            }
            else
            {
                if (mode == ExecutionMode.Aarch32Thumb)
                {
                    return new OpCodeT16(inst, address, opCode);
                }
                else
                {
                    return new OpCode(inst, address, opCode);
                }
            }
        }
    }
}
