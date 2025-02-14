using ARMeilleure.Common;
using ARMeilleure.Memory;
using Ryujinx.Cpu.LightningJit.CodeGen;
using Ryujinx.Cpu.LightningJit.CodeGen.Arm64;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;

namespace Ryujinx.Cpu.LightningJit.Arm32.Target.Arm64
{
    static class Compiler
    {
        public const uint UsableGprsMask = 0x7fff;
        public const uint UsableFpSimdMask = 0xffff;
        public const uint UsablePStateMask = 0xf0000000;

        private const int Encodable26BitsOffsetLimit = 0x2000000;

        private readonly struct Context
        {
            public readonly CodeWriter Writer;
            public readonly RegisterAllocator RegisterAllocator;
            public readonly MemoryManagerType MemoryManagerType;
            public readonly TailMerger TailMerger;
            public readonly AddressTable<ulong> FuncTable;
            public readonly nint DispatchStubPointer;

            private readonly RegisterSaveRestore _registerSaveRestore;
            private readonly nint _pageTablePointer;

            public Context(
                CodeWriter writer,
                RegisterAllocator registerAllocator,
                MemoryManagerType mmType,
                TailMerger tailMerger,
                AddressTable<ulong> funcTable,
                RegisterSaveRestore registerSaveRestore,
                nint dispatchStubPointer,
                nint pageTablePointer)
            {
                Writer = writer;
                RegisterAllocator = registerAllocator;
                MemoryManagerType = mmType;
                TailMerger = tailMerger;
                FuncTable = funcTable;
                _registerSaveRestore = registerSaveRestore;
                DispatchStubPointer = dispatchStubPointer;
                _pageTablePointer = pageTablePointer;
            }

            public readonly int GetReservedStackOffset()
            {
                return _registerSaveRestore.GetReservedStackOffset();
            }

            public readonly void WritePrologueAt(int instructionPointer)
            {
                CodeWriter writer = new();
                Assembler asm = new(writer);

                _registerSaveRestore.WritePrologue(ref asm);

                // If needed, set up the fixed registers with the pointers we will use.
                // First one is the context pointer (passed as first argument),
                // second one is the page table or address space base, it is at a fixed memory location and considered constant.

                if (RegisterAllocator.FixedContextRegister != 0)
                {
                    asm.Mov(Register(RegisterAllocator.FixedContextRegister), Register(0));
                }

                asm.Mov(Register(RegisterAllocator.FixedPageTableRegister), (ulong)_pageTablePointer);

                LoadFromContext(ref asm);

                // Write the prologue at the specified position in our writer.
                Writer.WriteInstructionsAt(instructionPointer, writer);
            }

            public readonly void WriteEpilogueWithoutContext()
            {
                Assembler asm = new(Writer);

                _registerSaveRestore.WriteEpilogue(ref asm);
            }

            public void LoadFromContext()
            {
                Assembler asm = new(Writer);

                LoadFromContext(ref asm);
            }

            private void LoadFromContext(ref Assembler asm)
            {
                LoadGprFromContext(ref asm, RegisterAllocator.UsedGprsMask & UsableGprsMask, NativeContextOffsets.GprBaseOffset);
                LoadFpSimdFromContext(ref asm, RegisterAllocator.UsedFpSimdMask & UsableFpSimdMask, NativeContextOffsets.FpSimdBaseOffset);
                LoadPStateFromContext(ref asm, UsablePStateMask, NativeContextOffsets.FlagsBaseOffset);
            }

            public void StoreToContext()
            {
                Assembler asm = new(Writer);

                StoreToContext(ref asm);
            }

            private void StoreToContext(ref Assembler asm)
            {
                StoreGprToContext(ref asm, RegisterAllocator.UsedGprsMask & UsableGprsMask, NativeContextOffsets.GprBaseOffset);
                StoreFpSimdToContext(ref asm, RegisterAllocator.UsedFpSimdMask & UsableFpSimdMask, NativeContextOffsets.FpSimdBaseOffset);
                StorePStateToContext(ref asm, UsablePStateMask, NativeContextOffsets.FlagsBaseOffset);
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

                        asm.LdpRiUn(Register(reg), Register(reg + 1), contextPtr, offset);
                    }
                    else
                    {
                        mask &= ~(1u << reg);

                        asm.LdrRiUn(Register(reg), contextPtr, offset);
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

                using ScopedRegister tempRegister = RegisterAllocator.AllocateTempGprRegisterScoped();

                asm.LdrRiUn(tempRegister.Operand, contextPtr, baseOffset);
                asm.MsrNzcv(tempRegister.Operand);
            }

            private void StoreGprToContext(ref Assembler asm, uint mask, int baseOffset)
            {
                Operand contextPtr = Register(RegisterAllocator.FixedContextRegister);

                while (mask != 0)
                {
                    int reg = BitOperations.TrailingZeroCount(mask);
                    int offset = baseOffset + reg * 8;

                    if (reg < 31 && (mask & (2u << reg)) != 0 && offset < RegisterSaveRestore.Encodable9BitsOffsetLimit)
                    {
                        mask &= ~(3u << reg);

                        asm.StpRiUn(Register(reg), Register(reg + 1), contextPtr, offset);
                    }
                    else
                    {
                        mask &= ~(1u << reg);

                        asm.StrRiUn(Register(reg), contextPtr, offset);
                    }
                }
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

                using ScopedRegister tempRegister = RegisterAllocator.AllocateTempGprRegisterScoped();
                using ScopedRegister tempRegister2 = RegisterAllocator.AllocateTempGprRegisterScoped();

                asm.LdrRiUn(tempRegister.Operand, contextPtr, baseOffset);
                asm.MrsNzcv(tempRegister2.Operand);
                asm.And(tempRegister.Operand, tempRegister.Operand, InstEmitCommon.Const(0xfffffff));
                asm.Orr(tempRegister.Operand, tempRegister.Operand, tempRegister2.Operand);
                asm.StrRiUn(tempRegister.Operand, contextPtr, baseOffset);
            }
        }

        public static CompiledFunction Compile(CpuPreset cpuPreset, IMemoryManager memoryManager, ulong address, AddressTable<ulong> funcTable, nint dispatchStubPtr, bool isThumb)
        {
            MultiBlock multiBlock = Decoder<InstEmit>.DecodeMulti(cpuPreset, memoryManager, address, isThumb);

            Dictionary<ulong, int> targets = new();

            CodeWriter writer = new();
            RegisterAllocator regAlloc = new();
            Assembler asm = new(writer);
            CodeGenContext cgContext = new(writer, asm, regAlloc, memoryManager.Type, isThumb);
            ArmCondition lastCondition = ArmCondition.Al;
            int lastConditionIp = 0;

            // Required for load/store to context.
            regAlloc.EnsureTempGprRegisters(2);

            ulong pc = address;

            for (int blockIndex = 0; blockIndex < multiBlock.Blocks.Count; blockIndex++)
            {
                Block block = multiBlock.Blocks[blockIndex];

                Debug.Assert(block.Address == pc);

                targets.Add(pc, writer.InstructionPointer);

                for (int index = 0; index < block.Instructions.Count; index++)
                {
                    InstInfo instInfo = block.Instructions[index];

                    if (index < block.Instructions.Count - 1)
                    {
                        cgContext.SetNextInstruction(block.Instructions[index + 1]);
                    }
                    else
                    {
                        cgContext.SetNextInstruction(default);
                    }

                    SetConditionalStart(cgContext, ref lastCondition, ref lastConditionIp, instInfo.Name, instInfo.Flags, instInfo.Encoding);

                    if (block.IsLoopEnd && index == block.Instructions.Count - 1)
                    {
                        // If this is a loop, the code might run for a long time uninterrupted.
                        // We insert a "sync point" here to ensure the loop can be interrupted if needed.

                        cgContext.AddPendingSyncPoint();

                        asm.B(0);
                    }

                    cgContext.SetPc((uint)pc);

                    instInfo.EmitFunc(cgContext, instInfo.Encoding);

                    if (cgContext.ConsumeNzcvModified())
                    {
                        ForceConditionalEnd(cgContext, ref lastCondition, lastConditionIp);
                    }

                    cgContext.UpdateItState();

                    pc += instInfo.Flags.HasFlag(InstFlags.Thumb16) ? 2UL : 4UL;
                }

                if (Decoder<InstEmit>.WritesToPC(block.Instructions[^1].Encoding, block.Instructions[^1].Name, block.Instructions[^1].Flags, block.IsThumb))
                {
                    // If the block ends with a PC register write, then we have a branch from register.

                    InstEmitCommon.SetThumbFlag(cgContext, regAlloc.RemapGprRegister(RegisterUtils.PcRegister));

                    cgContext.AddPendingIndirectBranch(block.Instructions[^1].Name, RegisterUtils.PcRegister);

                    asm.B(0);
                }

                ForceConditionalEnd(cgContext, ref lastCondition, lastConditionIp);
            }

            int reservedStackSize = 0;

            if (multiBlock.HasHostCall)
            {
                reservedStackSize = CalculateStackSizeForCallSpill(regAlloc.UsedGprsMask, regAlloc.UsedFpSimdMask, UsablePStateMask);
            }
            else if (multiBlock.HasHostCallSkipContext)
            {
                reservedStackSize = 2 * sizeof(ulong); // Context and page table pointers.
            }

            RegisterSaveRestore rsr = new(
                regAlloc.UsedGprsMask & AbiConstants.GprCalleeSavedRegsMask,
                regAlloc.UsedFpSimdMask & AbiConstants.FpSimdCalleeSavedRegsMask,
                OperandType.FP64,
                multiBlock.HasHostCall || multiBlock.HasHostCallSkipContext,
                reservedStackSize);

            TailMerger tailMerger = new();

            Context context = new(writer, regAlloc, memoryManager.Type, tailMerger, funcTable, rsr, dispatchStubPtr, memoryManager.PageTablePointer);

            InstInfo lastInstruction = multiBlock.Blocks[^1].Instructions[^1];
            bool lastInstIsConditional = GetCondition(lastInstruction, isThumb) != ArmCondition.Al;

            if (multiBlock.IsTruncated || lastInstIsConditional || lastInstruction.Name.IsCall() || IsConditionalBranch(lastInstruction))
            {
                WriteTailCallConstant(context, ref asm, (uint)pc);
            }

            IEnumerable<PendingBranch> pendingBranches = cgContext.GetPendingBranches();

            foreach (PendingBranch pendingBranch in pendingBranches)
            {
                RewriteBranchInstructionWithTarget(context, pendingBranch, targets);
            }

            tailMerger.WriteReturn(writer, context.WriteEpilogueWithoutContext);

            context.WritePrologueAt(0);

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

        private static void SetConditionalStart(
            CodeGenContext context,
            ref ArmCondition condition,
            ref int instructionPointer,
            InstName name,
            InstFlags flags,
            uint encoding)
        {
            if (!context.ConsumeItCondition(out ArmCondition currentCond))
            {
                currentCond = GetCondition(name, flags, encoding, context.IsThumb);
            }

            if (currentCond != condition)
            {
                WriteConditionalEnd(context, condition, instructionPointer);

                condition = currentCond;

                if (currentCond != ArmCondition.Al)
                {
                    instructionPointer = context.CodeWriter.InstructionPointer;
                    context.Arm64Assembler.B(currentCond.Invert(), 0);
                }
            }
        }

        private static bool IsConditionalBranch(in InstInfo instInfo)
        {
            return instInfo.Name == InstName.B && (ArmCondition)(instInfo.Encoding >> 28) != ArmCondition.Al;
        }

        private static ArmCondition GetCondition(in InstInfo instInfo, bool isThumb)
        {
            return GetCondition(instInfo.Name, instInfo.Flags, instInfo.Encoding, isThumb);
        }

        private static ArmCondition GetCondition(InstName name, InstFlags flags, uint encoding, bool isThumb)
        {
            // For branch, we handle conditional execution on the instruction itself.
            bool hasCond = flags.HasFlag(InstFlags.Cond) && !CanHandleConditionalInstruction(name, encoding, isThumb);

            return hasCond ? (ArmCondition)(encoding >> 28) : ArmCondition.Al;
        }

        private static bool CanHandleConditionalInstruction(InstName name, uint encoding, bool isThumb)
        {
            if (name == InstName.B)
            {
                return true;
            }

            // We can use CSEL for conditional MOV from registers, as long the instruction is not setting flags.
            // We don't handle thumb right now because the condition comes from the IT block which would be more complicated to handle.
            if (name == InstName.MovR && !isThumb && (encoding & (1u << 20)) == 0)
            {
                return true;
            }

            return false;
        }

        private static void ForceConditionalEnd(CodeGenContext context, ref ArmCondition condition, int instructionPointer)
        {
            WriteConditionalEnd(context, condition, instructionPointer);

            condition = ArmCondition.Al;
        }

        private static void WriteConditionalEnd(CodeGenContext context, ArmCondition condition, int instructionPointer)
        {
            if (condition != ArmCondition.Al)
            {
                int delta = context.CodeWriter.InstructionPointer - instructionPointer;
                uint branchInst = context.CodeWriter.ReadInstructionAt(instructionPointer) | (((uint)delta & 0x7ffff) << 5);
                Debug.Assert((int)((branchInst & ~0x1fu) << 8) >> 11 == delta * 4);

                context.CodeWriter.WriteInstructionAt(instructionPointer, branchInst);
            }
        }

        private static void RewriteBranchInstructionWithTarget(in Context context, in PendingBranch pendingBranch, Dictionary<ulong, int> targets)
        {
            switch (pendingBranch.BranchType)
            {
                case BranchType.Branch:
                    RewriteBranchInstructionWithTarget(context, pendingBranch.Name, pendingBranch.TargetAddress, pendingBranch.WriterPointer, targets);
                    break;
                case BranchType.Call:
                    RewriteCallInstructionWithTarget(context, pendingBranch.TargetAddress, pendingBranch.NextAddress, pendingBranch.WriterPointer);
                    break;
                case BranchType.IndirectBranch:
                    RewriteIndirectBranchInstructionWithTarget(context, pendingBranch.Name, pendingBranch.TargetAddress, pendingBranch.WriterPointer);
                    break;
                case BranchType.TableBranchByte:
                case BranchType.TableBranchHalfword:
                    RewriteTableBranchInstructionWithTarget(
                        context,
                        pendingBranch.BranchType == BranchType.TableBranchHalfword,
                        pendingBranch.TargetAddress,
                        pendingBranch.NextAddress,
                        pendingBranch.WriterPointer);
                    break;
                case BranchType.IndirectCall:
                    RewriteIndirectCallInstructionWithTarget(context, pendingBranch.TargetAddress, pendingBranch.NextAddress, pendingBranch.WriterPointer);
                    break;
                case BranchType.SyncPoint:
                case BranchType.SoftwareInterrupt:
                case BranchType.ReadCntpct:
                    RewriteHostCall(context, pendingBranch.Name, pendingBranch.BranchType, pendingBranch.TargetAddress, pendingBranch.NextAddress, pendingBranch.WriterPointer);
                    break;
                default:
                    Debug.Fail($"Invalid branch type '{pendingBranch.BranchType}'");
                    break;
            }
        }

        private static void RewriteBranchInstructionWithTarget(in Context context, InstName name, uint targetAddress, int branchIndex, Dictionary<ulong, int> targets)
        {
            CodeWriter writer = context.Writer;
            Assembler asm = new(writer);

            int delta;
            int targetIndex;
            uint encoding = writer.ReadInstructionAt(branchIndex);

            if (encoding == 0x14000000)
            {
                // Unconditional branch.

                if (targets.TryGetValue(targetAddress, out targetIndex))
                {
                    delta = targetIndex - branchIndex;

                    if (delta >= -Encodable26BitsOffsetLimit && delta < Encodable26BitsOffsetLimit)
                    {
                        writer.WriteInstructionAt(branchIndex, encoding | (uint)(delta & 0x3ffffff));

                        return;
                    }
                }

                targetIndex = writer.InstructionPointer;
                delta = targetIndex - branchIndex;

                writer.WriteInstructionAt(branchIndex, encoding | (uint)(delta & 0x3ffffff));
                WriteTailCallConstant(context, ref asm, targetAddress);
            }
            else
            {
                // Conditional branch.

                uint branchMask = 0x7ffff;
                int branchMax = (int)(branchMask + 1) / 2;

                if (targets.TryGetValue(targetAddress, out targetIndex))
                {
                    delta = targetIndex - branchIndex;

                    if (delta >= -branchMax && delta < branchMax)
                    {
                        writer.WriteInstructionAt(branchIndex, encoding | (uint)((delta & branchMask) << 5));

                        return;
                    }
                }

                targetIndex = writer.InstructionPointer;
                delta = targetIndex - branchIndex;

                if (delta >= -branchMax && delta < branchMax)
                {
                    writer.WriteInstructionAt(branchIndex, encoding | (uint)((delta & branchMask) << 5));
                    WriteTailCallConstant(context, ref asm, targetAddress);
                }
                else
                {
                    // If the branch target is too far away, we use a regular unconditional branch
                    // instruction instead which has a much higher range.
                    // We branch directly to the end of the function, where we put the conditional branch,
                    // and then branch back to the next instruction or return the branch target depending
                    // on the branch being taken or not.

                    uint branchInst = 0x14000000u | ((uint)delta & 0x3ffffff);
                    Debug.Assert((int)(branchInst << 6) >> 4 == delta * 4);

                    writer.WriteInstructionAt(branchIndex, branchInst);

                    int movedBranchIndex = writer.InstructionPointer;

                    writer.WriteInstruction(0u); // Placeholder
                    asm.B((branchIndex + 1 - writer.InstructionPointer) * 4);

                    delta = writer.InstructionPointer - movedBranchIndex;

                    writer.WriteInstructionAt(movedBranchIndex, encoding | (uint)((delta & branchMask) << 5));
                    WriteTailCallConstant(context, ref asm, targetAddress);
                }
            }

            Debug.Assert(name == InstName.B || name == InstName.Cbnz, $"Unknown branch instruction \"{name}\".");
        }

        private static void RewriteCallInstructionWithTarget(in Context context, uint targetAddress, uint nextAddress, int branchIndex)
        {
            CodeWriter writer = context.Writer;
            Assembler asm = new(writer);

            WriteBranchToCurrentPosition(context, branchIndex);

            asm.Mov(context.RegisterAllocator.RemapGprRegister(RegisterUtils.LrRegister), nextAddress);

            context.StoreToContext();
            InstEmitFlow.WriteCallWithGuestAddress(
                writer,
                ref asm,
                context.RegisterAllocator,
                context.TailMerger,
                context.WriteEpilogueWithoutContext,
                context.FuncTable,
                context.DispatchStubPointer,
                context.GetReservedStackOffset(),
                nextAddress,
                InstEmitCommon.Const((int)targetAddress));
            context.LoadFromContext();

            // Branch back to the next instruction (after the call).
            asm.B((branchIndex + 1 - writer.InstructionPointer) * 4);
        }

        private static void RewriteIndirectBranchInstructionWithTarget(in Context context, InstName name, uint targetRegister, int branchIndex)
        {
            CodeWriter writer = context.Writer;
            Assembler asm = new(writer);

            WriteBranchToCurrentPosition(context, branchIndex);

            using ScopedRegister target = context.RegisterAllocator.AllocateTempGprRegisterScoped();

            asm.And(target.Operand, context.RegisterAllocator.RemapGprRegister((int)targetRegister), InstEmitCommon.Const(~1));

            context.StoreToContext();

            if ((name == InstName.Bx && targetRegister == RegisterUtils.LrRegister) ||
                name == InstName.Ldm ||
                name == InstName.Ldmda ||
                name == InstName.Ldmdb ||
                name == InstName.Ldmib ||
                name == InstName.Pop)
            {
                // Arm32 does not have a return instruction, instead returns are implemented
                // either using BX LR (for leaf functions), or POP { ... PC }.

                asm.Mov(Register(0), target.Operand);

                context.TailMerger.AddUnconditionalReturn(writer, asm);
            }
            else
            {
                InstEmitFlow.WriteCallWithGuestAddress(
                    writer,
                    ref asm,
                    context.RegisterAllocator,
                    context.TailMerger,
                    context.WriteEpilogueWithoutContext,
                    context.FuncTable,
                    context.DispatchStubPointer,
                    context.GetReservedStackOffset(),
                    0u,
                    target.Operand,
                    isTail: true);
            }
        }

        private static void RewriteTableBranchInstructionWithTarget(in Context context, bool halfword, uint rn, uint rm, int branchIndex)
        {
            CodeWriter writer = context.Writer;
            Assembler asm = new(writer);

            WriteBranchToCurrentPosition(context, branchIndex);

            using ScopedRegister target = context.RegisterAllocator.AllocateTempGprRegisterScoped();

            asm.Add(
                target.Operand,
                context.RegisterAllocator.RemapGprRegister((int)rn),
                context.RegisterAllocator.RemapGprRegister((int)rm),
                ArmShiftType.Lsl,
                halfword ? 1 : 0);

            InstEmitMemory.WriteAddressTranslation(context.MemoryManagerType, context.RegisterAllocator, asm, target.Operand, target.Operand);

            if (halfword)
            {
                asm.LdrhRiUn(target.Operand, target.Operand, 0);
            }
            else
            {
                asm.LdrbRiUn(target.Operand, target.Operand, 0);
            }

            asm.Add(target.Operand, context.RegisterAllocator.RemapGprRegister(RegisterUtils.PcRegister), target.Operand, ArmShiftType.Lsl, 1);

            context.StoreToContext();

            InstEmitFlow.WriteCallWithGuestAddress(
                writer,
                ref asm,
                context.RegisterAllocator,
                context.TailMerger,
                context.WriteEpilogueWithoutContext,
                context.FuncTable,
                context.DispatchStubPointer,
                context.GetReservedStackOffset(),
                0u,
                target.Operand,
                isTail: true);
        }

        private static void RewriteIndirectCallInstructionWithTarget(in Context context, uint targetRegister, uint nextAddress, int branchIndex)
        {
            CodeWriter writer = context.Writer;
            Assembler asm = new(writer);

            WriteBranchToCurrentPosition(context, branchIndex);

            using ScopedRegister target = context.RegisterAllocator.AllocateTempGprRegisterScoped();

            asm.And(target.Operand, context.RegisterAllocator.RemapGprRegister((int)targetRegister), InstEmitCommon.Const(~1));
            asm.Mov(context.RegisterAllocator.RemapGprRegister(RegisterUtils.LrRegister), nextAddress);

            context.StoreToContext();
            InstEmitFlow.WriteCallWithGuestAddress(
                writer,
                ref asm,
                context.RegisterAllocator,
                context.TailMerger,
                context.WriteEpilogueWithoutContext,
                context.FuncTable,
                context.DispatchStubPointer,
                context.GetReservedStackOffset(),
                nextAddress & ~1u,
                target.Operand);
            context.LoadFromContext();

            // Branch back to the next instruction (after the call).
            asm.B((branchIndex + 1 - writer.InstructionPointer) * 4);
        }

        private static void RewriteHostCall(in Context context, InstName name, BranchType type, uint imm, uint pc, int branchIndex)
        {
            CodeWriter writer = context.Writer;
            Assembler asm = new(writer);

            uint encoding = writer.ReadInstructionAt(branchIndex);
            int targetIndex = writer.InstructionPointer;
            int delta = targetIndex - branchIndex;

            writer.WriteInstructionAt(branchIndex, encoding | (uint)(delta & 0x3ffffff));

            switch (type)
            {
                case BranchType.SyncPoint:
                    InstEmitSystem.WriteSyncPoint(
                        context.Writer,
                        ref asm,
                        context.RegisterAllocator,
                        context.TailMerger,
                        context.GetReservedStackOffset(),
                        context.StoreToContext,
                        context.LoadFromContext);
                    break;
                case BranchType.SoftwareInterrupt:
                    context.StoreToContext();
                    switch (name)
                    {
                        case InstName.Bkpt:
                            InstEmitSystem.WriteBkpt(context.Writer, context.RegisterAllocator, context.TailMerger, context.GetReservedStackOffset(), pc, imm);
                            break;
                        case InstName.Svc:
                            InstEmitSystem.WriteSvc(context.Writer, context.RegisterAllocator, context.TailMerger, context.GetReservedStackOffset(), pc, imm);
                            break;
                        case InstName.Udf:
                            InstEmitSystem.WriteUdf(context.Writer, context.RegisterAllocator, context.TailMerger, context.GetReservedStackOffset(), pc, imm);
                            break;
                    }
                    context.LoadFromContext();
                    break;
                case BranchType.ReadCntpct:
                    InstEmitSystem.WriteReadCntpct(context.Writer, context.RegisterAllocator, context.GetReservedStackOffset(), (int)imm, (int)pc);
                    break;
                default:
                    Debug.Fail($"Invalid branch type '{type}'");
                    break;
            }

            // Branch back to the next instruction.
            asm.B((branchIndex + 1 - writer.InstructionPointer) * 4);
        }

        private static void WriteBranchToCurrentPosition(in Context context, int branchIndex)
        {
            CodeWriter writer = context.Writer;

            int targetIndex = writer.InstructionPointer;

            if (branchIndex + 1 == targetIndex)
            {
                writer.RemoveLastInstruction();
            }
            else
            {
                uint encoding = writer.ReadInstructionAt(branchIndex);
                int delta = targetIndex - branchIndex;

                writer.WriteInstructionAt(branchIndex, encoding | (uint)(delta & 0x3ffffff));
            }
        }

        private static void WriteTailCallConstant(in Context context, ref Assembler asm, uint address)
        {
            context.StoreToContext();
            InstEmitFlow.WriteCallWithGuestAddress(
                context.Writer,
                ref asm,
                context.RegisterAllocator,
                context.TailMerger,
                context.WriteEpilogueWithoutContext,
                context.FuncTable,
                context.DispatchStubPointer,
                context.GetReservedStackOffset(),
                0u,
                InstEmitCommon.Const((int)address),
                isTail: true);
        }

        private static Operand Register(int register, OperandType type = OperandType.I64)
        {
            return new Operand(register, RegisterType.Integer, type);
        }

        public static void PrintStats()
        {
        }
    }
}
