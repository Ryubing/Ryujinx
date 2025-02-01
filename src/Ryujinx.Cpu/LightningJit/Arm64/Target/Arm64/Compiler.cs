using ARMeilleure.Common;
using ARMeilleure.Memory;
using Ryujinx.Cpu.LightningJit.CodeGen;
using Ryujinx.Cpu.LightningJit.CodeGen.Arm64;
using Ryujinx.Cpu.LightningJit.Graph;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;

namespace Ryujinx.Cpu.LightningJit.Arm64.Target.Arm64
{
    static class Compiler
    {
        private const int Encodable26BitsOffsetLimit = 0x2000000;

        private readonly struct Context
        {
            public readonly CodeWriter Writer;
            public readonly RegisterAllocator RegisterAllocator;
            public readonly TailMerger TailMerger;
            public readonly AddressTable<ulong> FuncTable;
            public readonly nint DispatchStubPointer;

            private readonly MultiBlock _multiBlock;
            private readonly RegisterSaveRestore _registerSaveRestore;
            private readonly nint _pageTablePointer;

            public Context(
                CodeWriter writer,
                RegisterAllocator registerAllocator,
                TailMerger tailMerger,
                RegisterSaveRestore registerSaveRestore,
                MultiBlock multiBlock,
                AddressTable<ulong> funcTable,
                nint dispatchStubPointer,
                nint pageTablePointer)
            {
                Writer = writer;
                RegisterAllocator = registerAllocator;
                TailMerger = tailMerger;
                _registerSaveRestore = registerSaveRestore;
                _multiBlock = multiBlock;
                FuncTable = funcTable;
                DispatchStubPointer = dispatchStubPointer;
                _pageTablePointer = pageTablePointer;
            }

            public readonly int GetLrRegisterIndex()
            {
                return RemapGprRegister(RegisterUtils.LrIndex);
            }

            public readonly int RemapGprRegister(int index)
            {
                return RegisterAllocator.RemapReservedGprRegister(index);
            }

            public readonly int GetReservedStackOffset()
            {
                return _registerSaveRestore.GetReservedStackOffset();
            }

            public readonly void WritePrologue()
            {
                Assembler asm = new(Writer);

                _registerSaveRestore.WritePrologue(ref asm);

                // If needed, set up the fixed registers with the pointers we will use.
                // First one is the context pointer (passed as first argument),
                // second one is the page table or address space base, it is at a fixed memory location and considered constant.

                if (RegisterAllocator.FixedContextRegister != 0)
                {
                    asm.Mov(Register(RegisterAllocator.FixedContextRegister), Register(0));
                }

                if (_multiBlock.HasMemoryInstruction)
                {
                    asm.Mov(Register(RegisterAllocator.FixedPageTableRegister), (ulong)_pageTablePointer);
                }

                // This assumes that the block with the index 0 is always the entry block.
                LoadFromContext(ref asm, _multiBlock.ReadMasks[0]);
            }

            public readonly void WriteEpilogueWithoutContext()
            {
                Assembler asm = new(Writer);

                _registerSaveRestore.WriteEpilogue(ref asm);
            }

            public void LoadFromContextAfterCall(int blockIndex)
            {
                Block block = _multiBlock.Blocks[blockIndex];

                if (block.SuccessorsCount != 0)
                {
                    Assembler asm = new(Writer);

                    RegisterMask readMask = _multiBlock.ReadMasks[block.GetSuccessor(0).Index];

                    for (int sIndex = 1; sIndex < block.SuccessorsCount; sIndex++)
                    {
                        IBlock successor = block.GetSuccessor(sIndex);

                        readMask |= _multiBlock.ReadMasks[successor.Index];
                    }

                    LoadFromContext(ref asm, readMask);
                }
            }

            private void LoadFromContext(ref Assembler asm, RegisterMask readMask)
            {
                LoadGprFromContext(ref asm, readMask.GprMask, NativeContextOffsets.GprBaseOffset);
                LoadFpSimdFromContext(ref asm, readMask.FpSimdMask, NativeContextOffsets.FpSimdBaseOffset);
                LoadPStateFromContext(ref asm, readMask.PStateMask, NativeContextOffsets.FlagsBaseOffset);
            }

            public void StoreToContextBeforeCall(int blockIndex, ulong? newLrValue = null)
            {
                Assembler asm = new(Writer);

                StoreToContext(ref asm, _multiBlock.WriteMasks[blockIndex], newLrValue);
            }

            private void StoreToContext(ref Assembler asm, RegisterMask writeMask, ulong? newLrValue)
            {
                StoreGprToContext(ref asm, writeMask.GprMask, NativeContextOffsets.GprBaseOffset, newLrValue);
                StoreFpSimdToContext(ref asm, writeMask.FpSimdMask, NativeContextOffsets.FpSimdBaseOffset);
                StorePStateToContext(ref asm, writeMask.PStateMask, NativeContextOffsets.FlagsBaseOffset);
            }

            private void LoadGprFromContext(ref Assembler asm, uint mask, int baseOffset)
            {
                Operand contextPtr = Register(RegisterAllocator.FixedContextRegister);

                while (mask != 0)
                {
                    int reg = BitOperations.TrailingZeroCount(mask);
                    int offset = baseOffset + reg * 8;

                    if (reg < 31 && (mask & (2u << reg)) != 0 && offset < RegisterSaveRestore.Encodable9BitsOffsetLimit)
                    {
                        mask &= ~(3u << reg);

                        asm.LdpRiUn(
                            Register(RegisterAllocator.RemapReservedGprRegister(reg)),
                            Register(RegisterAllocator.RemapReservedGprRegister(reg + 1)),
                            contextPtr,
                            offset);
                    }
                    else
                    {
                        mask &= ~(1u << reg);

                        asm.LdrRiUn(Register(RegisterAllocator.RemapReservedGprRegister(reg)), contextPtr, offset);
                    }
                }
            }

            private void LoadFpSimdFromContext(ref Assembler asm, uint mask, int baseOffset)
            {
                Operand contextPtr = Register(RegisterAllocator.FixedContextRegister);

                while (mask != 0)
                {
                    int reg = BitOperations.TrailingZeroCount(mask);
                    int offset = baseOffset + reg * 16;

                    mask &= ~(1u << reg);

                    asm.LdrRiUn(Register(reg, OperandType.V128), contextPtr, offset);
                }
            }

            private void LoadPStateFromContext(ref Assembler asm, uint mask, int baseOffset)
            {
                if (mask == 0)
                {
                    return;
                }

                Operand contextPtr = Register(RegisterAllocator.FixedContextRegister);

                int tempRegister = RegisterAllocator.AllocateTempGprRegister();

                Operand rt = Register(tempRegister, OperandType.I32);

                asm.LdrRiUn(rt, contextPtr, baseOffset);
                asm.MsrNzcv(rt);

                RegisterAllocator.FreeTempGprRegister(tempRegister);
            }

            private void StoreGprToContext(ref Assembler asm, uint mask, int baseOffset, ulong? newLrValue)
            {
                Operand contextPtr = Register(RegisterAllocator.FixedContextRegister);

                int tempRegister = -1;

                if (newLrValue.HasValue)
                {
                    // This is required for BLR X30 instructions, where we need to get the target address
                    // before it is overwritten with the return address that the call would write there.

                    tempRegister = RegisterAllocator.AllocateTempGprRegister();

                    asm.Mov(Register(tempRegister), newLrValue.Value);
                }

                while (mask != 0)
                {
                    int reg = BitOperations.TrailingZeroCount(mask);
                    int offset = baseOffset + reg * 8;

                    if (reg < 31 && (mask & (2u << reg)) != 0 && offset < RegisterSaveRestore.Encodable9BitsOffsetLimit)
                    {
                        mask &= ~(3u << reg);

                        asm.StpRiUn(
                            Register(RemapReservedGprRegister(reg, tempRegister)),
                            Register(RemapReservedGprRegister(reg + 1, tempRegister)),
                            contextPtr,
                            offset);
                    }
                    else
                    {
                        mask &= ~(1u << reg);

                        asm.StrRiUn(Register(RemapReservedGprRegister(reg, tempRegister)), contextPtr, offset);
                    }
                }

                if (tempRegister >= 0)
                {
                    RegisterAllocator.FreeTempGprRegister(tempRegister);
                }
            }

            private int RemapReservedGprRegister(int index, int tempRegister)
            {
                if (tempRegister >= 0 && index == RegisterUtils.LrIndex)
                {
                    return tempRegister;
                }

                return RegisterAllocator.RemapReservedGprRegister(index);
            }

            private void StoreFpSimdToContext(ref Assembler asm, uint mask, int baseOffset)
            {
                Operand contextPtr = Register(RegisterAllocator.FixedContextRegister);

                while (mask != 0)
                {
                    int reg = BitOperations.TrailingZeroCount(mask);
                    int offset = baseOffset + reg * 16;

                    mask &= ~(1u << reg);

                    asm.StrRiUn(Register(reg, OperandType.V128), contextPtr, offset);
                }
            }

            private void StorePStateToContext(ref Assembler asm, uint mask, int baseOffset)
            {
                if (mask == 0)
                {
                    return;
                }

                Operand contextPtr = Register(RegisterAllocator.FixedContextRegister);

                int tempRegister = RegisterAllocator.AllocateTempGprRegister();

                Operand rt = Register(tempRegister, OperandType.I32);

                asm.MrsNzcv(rt);
                asm.StrRiUn(rt, contextPtr, baseOffset);

                RegisterAllocator.FreeTempGprRegister(tempRegister);
            }
        }

        private readonly struct PendingBranch
        {
            public readonly int BlockIndex;
            public readonly ulong Pc;
            public readonly InstName Name;
            public readonly uint Encoding;
            public readonly int WriterPointer;

            public PendingBranch(int blockIndex, ulong pc, InstName name, uint encoding, int writerPointer)
            {
                BlockIndex = blockIndex;
                Pc = pc;
                Name = name;
                Encoding = encoding;
                WriterPointer = writerPointer;
            }
        }

        public static CompiledFunction Compile(CpuPreset cpuPreset, IMemoryManager memoryManager, ulong address, AddressTable<ulong> funcTable, nint dispatchStubPtr)
        {
            MultiBlock multiBlock = Decoder.DecodeMulti(cpuPreset, memoryManager, address);

            Dictionary<ulong, int> targets = new();
            List<PendingBranch> pendingBranches = [];

            uint gprUseMask = multiBlock.GlobalUseMask.GprMask;
            uint fpSimdUseMask = multiBlock.GlobalUseMask.FpSimdMask;
            uint pStateUseMask = multiBlock.GlobalUseMask.PStateMask;

            CodeWriter writer = new();
            RegisterAllocator regAlloc = new(memoryManager.Type, gprUseMask, fpSimdUseMask, pStateUseMask, multiBlock.HasHostCall);
            RegisterSaveRestore rsr = new(
                regAlloc.AllGprMask & AbiConstants.GprCalleeSavedRegsMask,
                regAlloc.AllFpSimdMask & AbiConstants.FpSimdCalleeSavedRegsMask,
                OperandType.FP64,
                multiBlock.HasHostCall,
                multiBlock.HasHostCall ? CalculateStackSizeForCallSpill(regAlloc.AllGprMask, regAlloc.AllFpSimdMask, regAlloc.AllPStateMask) : 0);

            TailMerger tailMerger = new();

            Context context = new(writer, regAlloc, tailMerger, rsr, multiBlock, funcTable, dispatchStubPtr, memoryManager.PageTablePointer);

            context.WritePrologue();

            ulong pc = address;

            for (int blockIndex = 0; blockIndex < multiBlock.Blocks.Count; blockIndex++)
            {
                Block block = multiBlock.Blocks[blockIndex];

                Debug.Assert(block.Address == pc);

                targets.Add(pc, writer.InstructionPointer);

                int instCount = block.EndsWithBranch ? block.Instructions.Count - 1 : block.Instructions.Count;

                for (int index = 0; index < instCount; index++)
                {
                    InstInfo instInfo = block.Instructions[index];

                    uint encoding = RegisterUtils.RemapRegisters(regAlloc, instInfo.Flags, instInfo.Encoding);

                    if (instInfo.AddressForm != AddressForm.None)
                    {
                        InstEmitMemory.RewriteInstruction(
                            memoryManager.AddressSpaceBits,
                            memoryManager.Type,
                            writer,
                            regAlloc,
                            instInfo.Name,
                            instInfo.Flags,
                            instInfo.AddressForm,
                            pc,
                            encoding);
                    }
                    else if (instInfo.Name == InstName.Sys)
                    {
                        InstEmitMemory.RewriteSysInstruction(memoryManager.AddressSpaceBits, memoryManager.Type, writer, regAlloc, encoding);
                    }
                    else if (instInfo.Name.IsSystem())
                    {
                        bool needsContextStoreLoad = InstEmitSystem.NeedsContextStoreLoad(instInfo.Name);

                        if (needsContextStoreLoad)
                        {
                            context.StoreToContextBeforeCall(blockIndex);
                        }

                        InstEmitSystem.RewriteInstruction(writer, regAlloc, tailMerger, instInfo.Name, pc, encoding, rsr.GetReservedStackOffset());

                        if (needsContextStoreLoad)
                        {
                            context.LoadFromContextAfterCall(blockIndex);
                        }
                    }
                    else
                    {
                        writer.WriteInstruction(encoding);
                    }

                    pc += 4UL;
                }

                if (block.IsLoopEnd)
                {
                    // If this is a loop, the code might run for a long time uninterrupted.
                    // We insert a "sync point" here to ensure the loop can be interrupted if needed.

                    InstEmitSystem.WriteSyncPoint(writer, context.RegisterAllocator, tailMerger, context.GetReservedStackOffset());
                }

                if (blockIndex < multiBlock.Blocks.Count - 1)
                {
                    InstInfo lastInstructionInfo = block.Instructions[^1];
                    InstName lastInstructionName = lastInstructionInfo.Name;
                    InstFlags lastInstructionFlags = lastInstructionInfo.Flags;
                    uint lastInstructionEncoding = lastInstructionInfo.Encoding;

                    lastInstructionEncoding = RegisterUtils.RemapRegisters(regAlloc, lastInstructionFlags, lastInstructionEncoding);

                    if (lastInstructionName.IsCall())
                    {
                        context.StoreToContextBeforeCall(blockIndex, pc + 4UL);

                        InstEmitSystem.RewriteCallInstruction(
                            writer,
                            regAlloc,
                            tailMerger,
                            context.WriteEpilogueWithoutContext,
                            funcTable,
                            dispatchStubPtr,
                            lastInstructionName,
                            pc,
                            lastInstructionEncoding,
                            context.GetReservedStackOffset());

                        context.LoadFromContextAfterCall(blockIndex);

                        pc += 4UL;
                    }
                    else if (lastInstructionName == InstName.Ret)
                    {
                        RewriteBranchInstruction(context, blockIndex, lastInstructionName, pc, lastInstructionEncoding);

                        pc += 4UL;
                    }
                    else if (block.EndsWithBranch)
                    {
                        pendingBranches.Add(new(blockIndex, pc, lastInstructionName, lastInstructionEncoding, writer.InstructionPointer));
                        writer.WriteInstruction(0u); // Placeholder.

                        pc += 4UL;
                    }
                }
            }

            int lastBlockIndex = multiBlock.Blocks[^1].Index;

            if (multiBlock.IsTruncated)
            {
                Assembler asm = new(writer);

                WriteTailCallConstant(context, ref asm, lastBlockIndex, pc);
            }
            else
            {
                InstInfo lastInstructionInfo = multiBlock.Blocks[^1].Instructions[^1];
                InstName lastInstructionName = lastInstructionInfo.Name;
                InstFlags lastInstructionFlags = lastInstructionInfo.Flags;
                uint lastInstructionEncoding = lastInstructionInfo.Encoding;

                lastInstructionEncoding = RegisterUtils.RemapRegisters(regAlloc, lastInstructionFlags, lastInstructionEncoding);

                RewriteBranchInstruction(context, lastBlockIndex, lastInstructionName, pc, lastInstructionEncoding);

                pc += 4;
            }

            foreach (PendingBranch pendingBranch in pendingBranches)
            {
                RewriteBranchInstructionWithTarget(
                    context,
                    pendingBranch.BlockIndex,
                    pendingBranch.Name,
                    pendingBranch.Pc,
                    pendingBranch.Encoding,
                    pendingBranch.WriterPointer,
                    targets);
            }

            tailMerger.WriteReturn(writer, context.WriteEpilogueWithoutContext);

            return new(writer.AsByteSpan(), (int)(pc - address));
        }

        private static int CalculateStackSizeForCallSpill(uint gprUseMask, uint fpSimdUseMask, uint pStateUseMask)
        {
            // Note that we don't discard callee saved FP/SIMD register because only the lower 64 bits is callee saved,
            // so if the function is using the full register, that won't be enough.
            // We could do better, but it's likely not worth it since this case happens very rarely in practice.

            return BitOperations.PopCount(gprUseMask & ~AbiConstants.GprCalleeSavedRegsMask) * 8 +
                   BitOperations.PopCount(fpSimdUseMask) * 16 +
                   (pStateUseMask != 0 ? 8 : 0);
        }

        private static void RewriteBranchInstruction(in Context context, int blockIndex, InstName name, ulong pc, uint encoding)
        {
            CodeWriter writer = context.Writer;
            Assembler asm = new(writer);

            int originalOffset;
            ulong nextAddress = pc + 4UL;
            ulong targetAddress;

            switch (name)
            {
                case InstName.BUncond:
                    originalOffset = ImmUtils.ExtractSImm26Times4(encoding);
                    targetAddress = pc + (ulong)originalOffset;

                    WriteTailCallConstant(context, ref asm, blockIndex, targetAddress);
                    break;

                case InstName.Bl:
                case InstName.Blr:
                case InstName.Br:
                    if (name == InstName.Bl)
                    {
                        asm.Mov(Register(context.GetLrRegisterIndex()), nextAddress);

                        int imm = ImmUtils.ExtractSImm26Times4(encoding);

                        WriteTailCallConstant(context, ref asm, blockIndex, pc + (ulong)imm);
                    }
                    else
                    {
                        bool isCall = name == InstName.Blr;
                        if (isCall)
                        {
                            context.StoreToContextBeforeCall(blockIndex, nextAddress);
                        }
                        else
                        {
                            context.StoreToContextBeforeCall(blockIndex);
                        }

                        InstEmitSystem.RewriteCallInstruction(
                            context.Writer,
                            context.RegisterAllocator,
                            context.TailMerger,
                            context.WriteEpilogueWithoutContext,
                            context.FuncTable,
                            context.DispatchStubPointer,
                            name,
                            pc,
                            encoding,
                            context.GetReservedStackOffset(),
                            isTail: true);
                    }
                    break;

                case InstName.Ret:
                    int rnIndex = RegisterUtils.ExtractRn(encoding);
                    if (rnIndex == RegisterUtils.ZrIndex)
                    {
                        WriteTailCallConstant(context, ref asm, blockIndex, 0UL);
                    }
                    else
                    {
                        rnIndex = context.RemapGprRegister(rnIndex);
                        context.StoreToContextBeforeCall(blockIndex);

                        if (rnIndex != 0)
                        {
                            asm.Mov(Register(0), Register(rnIndex));
                        }

                        context.TailMerger.AddUnconditionalReturn(writer, asm);
                    }
                    break;

                case InstName.BCond:
                case InstName.Cbnz:
                case InstName.Cbz:
                case InstName.Tbnz:
                case InstName.Tbz:
                    uint branchMask;

                    if (name == InstName.Tbnz || name == InstName.Tbz)
                    {
                        originalOffset = ImmUtils.ExtractSImm14Times4(encoding);
                        branchMask = 0x3fff;
                    }
                    else
                    {
                        originalOffset = ImmUtils.ExtractSImm19Times4(encoding);
                        branchMask = 0x7ffff;
                    }

                    targetAddress = pc + (ulong)originalOffset;

                    int branchIndex = writer.InstructionPointer;

                    writer.WriteInstruction(0u); // Reserved for branch.
                    WriteTailCallConstant(context, ref asm, blockIndex, nextAddress);

                    int targetIndex = writer.InstructionPointer;

                    writer.WriteInstructionAt(branchIndex, (encoding & ~(branchMask << 5)) | (uint)(((targetIndex - branchIndex) & branchMask) << 5));
                    WriteTailCallConstant(context, ref asm, blockIndex, targetAddress);
                    break;

                default:
                    Debug.Fail($"Unknown branch instruction \"{name}\".");
                    break;
            }
        }

        private static void RewriteBranchInstructionWithTarget(
            in Context context,
            int blockIndex,
            InstName name,
            ulong pc,
            uint encoding,
            int branchIndex,
            Dictionary<ulong, int> targets)
        {
            CodeWriter writer = context.Writer;
            Assembler asm = new(writer);

            int delta;
            int targetIndex;
            int originalOffset;
            ulong targetAddress;

            switch (name)
            {
                case InstName.BUncond:
                    originalOffset = ImmUtils.ExtractSImm26Times4(encoding);
                    targetAddress = pc + (ulong)originalOffset;

                    if (targets.TryGetValue(targetAddress, out targetIndex))
                    {
                        delta = targetIndex - branchIndex;

                        if (delta >= -Encodable26BitsOffsetLimit && delta < Encodable26BitsOffsetLimit)
                        {
                            writer.WriteInstructionAt(branchIndex, (encoding & ~0x3ffffffu) | (uint)(delta & 0x3ffffff));
                            break;
                        }
                    }

                    targetIndex = writer.InstructionPointer;
                    delta = targetIndex - branchIndex;

                    writer.WriteInstructionAt(branchIndex, (encoding & ~0x3ffffffu) | (uint)(delta & 0x3ffffff));
                    WriteTailCallConstant(context, ref asm, blockIndex, targetAddress);
                    break;

                case InstName.BCond:
                case InstName.Cbnz:
                case InstName.Cbz:
                case InstName.Tbnz:
                case InstName.Tbz:
                    uint branchMask;

                    if (name == InstName.Tbnz || name == InstName.Tbz)
                    {
                        originalOffset = ImmUtils.ExtractSImm14Times4(encoding);
                        branchMask = 0x3fff;
                    }
                    else
                    {
                        originalOffset = ImmUtils.ExtractSImm19Times4(encoding);
                        branchMask = 0x7ffff;
                    }

                    int branchMax = (int)(branchMask + 1) / 2;

                    targetAddress = pc + (ulong)originalOffset;

                    if (targets.TryGetValue(targetAddress, out targetIndex))
                    {
                        delta = targetIndex - branchIndex;

                        if (delta >= -branchMax && delta < branchMax)
                        {
                            writer.WriteInstructionAt(branchIndex, (encoding & ~(branchMask << 5)) | (uint)((delta & branchMask) << 5));
                            break;
                        }
                    }

                    targetIndex = writer.InstructionPointer;
                    delta = targetIndex - branchIndex;

                    if (delta >= -branchMax && delta < branchMax)
                    {
                        writer.WriteInstructionAt(branchIndex, (encoding & ~(branchMask << 5)) | (uint)((delta & branchMask) << 5));
                        WriteTailCallConstant(context, ref asm, blockIndex, targetAddress);
                    }
                    else
                    {
                        // If the branch target is too far away, we use a regular unconditional branch
                        // instruction instead which has a much higher range.
                        // We branch directly to the end of the function, where we put the conditional branch,
                        // and then branch back to the next instruction or return the branch target depending
                        // on the branch being taken or not.

                        uint branchInst = 0x14000000u | ((uint)delta & 0x3ffffff);
                        Debug.Assert(ImmUtils.ExtractSImm26Times4(branchInst) == delta * 4);

                        writer.WriteInstructionAt(branchIndex, branchInst);

                        int movedBranchIndex = writer.InstructionPointer;

                        writer.WriteInstruction(0u); // Placeholder
                        asm.B((branchIndex + 1 - writer.InstructionPointer) * 4);

                        delta = writer.InstructionPointer - movedBranchIndex;

                        writer.WriteInstructionAt(movedBranchIndex, (encoding & ~(branchMask << 5)) | (uint)((delta & branchMask) << 5));
                        WriteTailCallConstant(context, ref asm, blockIndex, targetAddress);
                    }
                    break;

                default:
                    Debug.Fail($"Unknown branch instruction \"{name}\".");
                    break;
            }
        }

        private static void WriteTailCallConstant(in Context context, ref Assembler asm, int blockIndex, ulong address)
        {
            context.StoreToContextBeforeCall(blockIndex);
            InstEmitSystem.WriteCallWithGuestAddress(
                context.Writer,
                ref asm,
                context.RegisterAllocator,
                context.TailMerger,
                context.WriteEpilogueWithoutContext,
                context.FuncTable,
                context.DispatchStubPointer,
                context.GetReservedStackOffset(),
                0UL,
                new Operand(OperandKind.Constant, OperandType.I64, address),
                isTail: true);
        }

        private static Operand Register(int register, OperandType type = OperandType.I64)
        {
            return new Operand(register, RegisterType.Integer, type);
        }
    }
}
