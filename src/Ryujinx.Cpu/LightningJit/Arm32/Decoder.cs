using ARMeilleure.Memory;
using Ryujinx.Cpu.LightningJit.Arm32.Target.Arm64;
using Ryujinx.Cpu.LightningJit.CodeGen.Arm64;
using System.Collections.Generic;
using System.Diagnostics;

namespace Ryujinx.Cpu.LightningJit.Arm32
{
    static class Decoder<T> where T : IInstEmit
    {
        public static MultiBlock DecodeMulti(CpuPreset cpuPreset, IMemoryManager memoryManager, ulong address, bool isThumb)
        {
            List<Block> blocks = [];
            List<ulong> branchTargets = [];

            while (true)
            {
                Block block = Decode(cpuPreset, memoryManager, address, isThumb);

                if (!block.IsTruncated && TryGetBranchTarget(block, out ulong targetAddress))
                {
                    branchTargets.Add(targetAddress);
                }

                blocks.Add(block);

                if (block.IsTruncated || !HasNextBlock(block, block.EndAddress - 4UL, branchTargets))
                {
                    break;
                }

                address = block.EndAddress;
            }

            branchTargets.Sort();
            SplitBlocks(blocks, branchTargets);

            return new(blocks);
        }

        private static bool TryGetBranchTarget(Block block, out ulong targetAddress)
        {
            // PC is 2 instructions ahead, since the end address is already one instruction after the last one, we just need to add
            // another instruction.

            ulong pc = block.EndAddress + (block.IsThumb ? 2UL : 4UL);

            return TryGetBranchTarget(block.Instructions[^1].Name, block.Instructions[^1].Flags, pc, block.Instructions[^1].Encoding, block.IsThumb, out targetAddress);
        }

        private static bool TryGetBranchTarget(InstName name, InstFlags flags, ulong pc, uint encoding, bool isThumb, out ulong targetAddress)
        {
            int originalOffset;

            switch (name)
            {
                case InstName.B:
                    if (isThumb)
                    {
                        if (flags.HasFlag(InstFlags.Thumb16))
                        {
                            if ((encoding & (1u << 29)) != 0)
                            {
                                InstImm11b16w11 inst = new(encoding);

                                originalOffset = ImmUtils.ExtractT16SImm11Times2(inst.Imm11);
                            }
                            else
                            {
                                InstCondb24w4Imm8b16w8 inst = new(encoding);

                                originalOffset = ImmUtils.ExtractT16SImm8Times2(inst.Imm8);
                            }
                        }
                        else
                        {
                            if ((encoding & (1u << 12)) != 0)
                            {
                                InstSb26w1Imm10b16w10J1b13w1J2b11w1Imm11b0w11 inst = new(encoding);

                                originalOffset = ImmUtils.CombineSImm24Times2(inst.Imm11, inst.Imm10, inst.J1, inst.J2, inst.S);
                            }
                            else
                            {
                                InstSb26w1Condb22w4Imm6b16w6J1b13w1J2b11w1Imm11b0w11 inst = new(encoding);

                                originalOffset = ImmUtils.CombineSImm20Times2(inst.Imm11, inst.Imm6, inst.J1, inst.J2, inst.S);
                            }
                        }
                    }
                    else
                    {
                        originalOffset = ImmUtils.ExtractSImm24Times4(encoding);
                    }

                    targetAddress = pc + (ulong)originalOffset;
                    Debug.Assert((targetAddress & 1) == 0);

                    return true;

                case InstName.Cbnz:
                    originalOffset = ImmUtils.ExtractT16UImm5Times2(encoding);
                    targetAddress = pc + (ulong)originalOffset;
                    Debug.Assert((targetAddress & 1) == 0);

                    return true;
            }

            targetAddress = 0;

            return false;
        }

        private static void SplitBlocks(List<Block> blocks, List<ulong> branchTargets)
        {
            int btIndex = 0;

            while (btIndex < branchTargets.Count)
            {
                for (int blockIndex = 0; blockIndex < blocks.Count && btIndex < branchTargets.Count; blockIndex++)
                {
                    Block block = blocks[blockIndex];
                    ulong currentBranchTarget = branchTargets[btIndex];

                    while (currentBranchTarget >= block.Address && currentBranchTarget < block.EndAddress)
                    {
                        if (block.Address != currentBranchTarget)
                        {
                            (Block leftBlock, Block rightBlock) = block.SplitAtAddress(currentBranchTarget);

                            if (leftBlock != null && rightBlock != null)
                            {
                                blocks.Insert(blockIndex, leftBlock);
                                blocks[blockIndex + 1] = rightBlock;

                                block = leftBlock;
                            }
                            else
                            {
                                // Split can only fail in thumb mode, where the instruction size is not fixed.

                                Debug.Assert(block.IsThumb);
                            }
                        }

                        btIndex++;

                        while (btIndex < branchTargets.Count && branchTargets[btIndex] == currentBranchTarget)
                        {
                            btIndex++;
                        }

                        if (btIndex >= branchTargets.Count)
                        {
                            break;
                        }

                        currentBranchTarget = branchTargets[btIndex];
                    }
                }

                Debug.Assert(btIndex < int.MaxValue);
                btIndex++;
            }
        }

        private static bool HasNextBlock(in Block block, ulong pc, List<ulong> branchTargets)
        {
            InstFlags lastInstFlags = block.Instructions[^1].Flags;

            // Thumb has separate encodings for conditional and unconditional branch instructions.
            if (lastInstFlags.HasFlag(InstFlags.Cond) && (block.IsThumb || (ArmCondition)(block.Instructions[^1].Encoding >> 28) < ArmCondition.Al))
            {
                return true;
            }

            switch (block.Instructions[^1].Name)
            {
                case InstName.B:
                    return branchTargets.Contains(pc + 4UL) ||
                        (TryGetBranchTarget(block, out ulong targetAddress) && targetAddress >= pc && targetAddress < pc + 0x1000);

                case InstName.Bx:
                case InstName.Bxj:
                    return branchTargets.Contains(pc + 4UL);

                case InstName.Cbnz:
                case InstName.BlI:
                case InstName.BlxR:
                    return true;
            }

            if (WritesToPC(block.Instructions[^1].Encoding, block.Instructions[^1].Name, lastInstFlags, block.IsThumb))
            {
                return branchTargets.Contains(pc + 4UL);
            }

            return !block.EndsWithBranch;
        }

        private static Block Decode(CpuPreset cpuPreset, IMemoryManager memoryManager, ulong address, bool isThumb)
        {
            ulong startAddress = address;

            List<InstInfo> insts = [];

            uint encoding;
            InstMeta meta;
            InstFlags extraFlags = InstFlags.None;
            bool hasHostCall = false;
            bool hasHostCallSkipContext = false;
            bool isTruncated = false;

            do
            {
                if (!memoryManager.IsMapped(address))
                {
                    encoding = 0;
                    meta = default;
                    isTruncated = true;
                    break;
                }

                if (isThumb)
                {
                    encoding = (uint)memoryManager.Read<ushort>(address) << 16;
                    address += 2UL;

                    extraFlags = InstFlags.Thumb16;

                    if (!InstTableT16<T>.TryGetMeta(encoding, cpuPreset.Version, cpuPreset.Features, out meta))
                    {
                        encoding |= memoryManager.Read<ushort>(address);

                        if (InstTableT32<T>.TryGetMeta(encoding, cpuPreset.Version, cpuPreset.Features, out meta))
                        {
                            address += 2UL;
                            extraFlags = InstFlags.None;
                        }
                    }
                }
                else
                {
                    encoding = memoryManager.Read<uint>(address);
                    address += 4UL;

                    meta = InstTableA32<T>.GetMeta(encoding, cpuPreset.Version, cpuPreset.Features);
                }

                if (meta.Name.IsSystemOrCall())
                {
                    if (!hasHostCall)
                    {
                        hasHostCall = InstEmitSystem.NeedsCall(meta.Name);
                    }

                    if (!hasHostCallSkipContext)
                    {
                        hasHostCallSkipContext = meta.Name.IsCall() || InstEmitSystem.NeedsCallSkipContext(meta.Name);
                    }
                }

                insts.Add(new(encoding, meta.Name, meta.EmitFunc, meta.Flags | extraFlags));
            }
            while (!IsControlFlow(encoding, meta.Name, meta.Flags | extraFlags, isThumb));

            bool isLoopEnd = false;

            if (!isTruncated && IsBackwardsBranch(meta.Name, encoding))
            {
                isLoopEnd = true;
                hasHostCallSkipContext = true;
            }

            return new(
                startAddress,
                address,
                insts,
                !isTruncated,
                hasHostCall,
                hasHostCallSkipContext,
                isTruncated,
                isLoopEnd,
                isThumb);
        }

        private static bool IsControlFlow(uint encoding, InstName name, InstFlags flags, bool isThumb)
        {
            switch (name)
            {
                case InstName.B:
                case InstName.BlI:
                case InstName.BlxR:
                case InstName.Bx:
                case InstName.Bxj:
                case InstName.Cbnz:
                case InstName.Tbb:
                    return true;
            }

            return WritesToPC(encoding, name, flags, isThumb);
        }

        public static bool WritesToPC(uint encoding, InstName name, InstFlags flags, bool isThumb)
        {
            return (GetRegisterWriteMask(encoding, name, flags, isThumb) & (1u << RegisterUtils.PcRegister)) != 0;
        }

        private static uint GetRegisterWriteMask(uint encoding, InstName name, InstFlags flags, bool isThumb)
        {
            uint mask = 0;

            if (isThumb)
            {
                if (flags.HasFlag(InstFlags.Thumb16))
                {
                    if (flags.HasFlag(InstFlags.Rdn))
                    {
                        mask |= 1u << RegisterUtils.ExtractRdn(flags, encoding);
                    }

                    if (flags.HasFlag(InstFlags.Rd))
                    {
                        mask |= 1u << RegisterUtils.ExtractRdT16(flags, encoding);
                    }

                    Debug.Assert(!flags.HasFlag(InstFlags.RdHi));

                    if (IsRegisterWrite(flags, InstFlags.Rt))
                    {
                        mask |= 1u << RegisterUtils.ExtractRtT16(flags, encoding);
                    }

                    Debug.Assert(!flags.HasFlag(InstFlags.Rt2));

                    if (IsRegisterWrite(flags, InstFlags.Rlist))
                    {
                        mask |= (byte)(encoding >> 16);

                        if (name == InstName.Push)
                        {
                            mask |= (encoding >> 10) & 0x4000; // LR
                        }
                        else if (name == InstName.Pop)
                        {
                            mask |= (encoding >> 9) & 0x8000; // PC
                        }
                    }

                    Debug.Assert(!flags.HasFlag(InstFlags.WBack));
                }
                else
                {
                    if (flags.HasFlag(InstFlags.Rd))
                    {
                        mask |= 1u << RegisterUtils.ExtractRdT32(flags, encoding);
                    }

                    if (flags.HasFlag(InstFlags.RdLo))
                    {
                        mask |= 1u << RegisterUtils.ExtractRdLoT32(encoding);
                    }

                    if (flags.HasFlag(InstFlags.RdHi))
                    {
                        mask |= 1u << RegisterUtils.ExtractRdHiT32(encoding);
                    }

                    if (IsRegisterWrite(flags, InstFlags.Rt) && IsRtWrite(name, encoding) && !IsR15RtEncodingSpecial(name, encoding))
                    {
                        mask |= 1u << RegisterUtils.ExtractRtT32(encoding);
                    }

                    if (IsRegisterWrite(flags, InstFlags.Rt2) && IsRtWrite(name, encoding))
                    {
                        mask |= 1u << RegisterUtils.ExtractRt2T32(encoding);
                    }

                    if (IsRegisterWrite(flags, InstFlags.Rlist))
                    {
                        mask |= (ushort)encoding;
                    }

                    if (flags.HasFlag(InstFlags.WBack) && HasWriteBackT32(name, encoding))
                    {
                        mask |= 1u << RegisterUtils.ExtractRn(encoding); // This is at the same bit position as A32.
                    }
                }
            }
            else
            {
                if (flags.HasFlag(InstFlags.Rd))
                {
                    mask |= 1u << RegisterUtils.ExtractRd(flags, encoding);
                }

                if (flags.HasFlag(InstFlags.RdHi))
                {
                    mask |= 1u << RegisterUtils.ExtractRdHi(encoding);
                }

                if (IsRegisterWrite(flags, InstFlags.Rt) && IsRtWrite(name, encoding) && !IsR15RtEncodingSpecial(name, encoding))
                {
                    mask |= 1u << RegisterUtils.ExtractRt(encoding);
                }

                if (IsRegisterWrite(flags, InstFlags.Rt2) && IsRtWrite(name, encoding))
                {
                    mask |= 1u << RegisterUtils.ExtractRt2(encoding);
                }

                if (IsRegisterWrite(flags, InstFlags.Rlist))
                {
                    mask |= (ushort)encoding;
                }

                if (flags.HasFlag(InstFlags.WBack) && HasWriteBack(name, encoding))
                {
                    mask |= 1u << RegisterUtils.ExtractRn(encoding);
                }
            }

            return mask;
        }

        private static bool IsRtWrite(InstName name, uint encoding)
        {
            // Some instructions can move GPR to FP/SIMD or FP/SIMD to GPR depending on the encoding.
            // Detect those cases so that we can tell if we're actually doing a register write.

            switch (name)
            {
                case InstName.VmovD:
                case InstName.VmovH:
                case InstName.VmovS:
                case InstName.VmovSs:
                    return (encoding & (1u << 20)) != 0;
            }

            return true;
        }

        private static bool HasWriteBack(InstName name, uint encoding)
        {
            if (IsLoadStoreMultiple(name))
            {
                return (encoding & (1u << 21)) != 0;
            }

            if (IsVLDnVSTn(name))
            {
                return (encoding & 0xf) != RegisterUtils.PcRegister;
            }

            bool w = (encoding & (1u << 21)) != 0;
            bool p = (encoding & (1u << 24)) != 0;

            return !p || w;
        }

        private static bool HasWriteBackT32(InstName name, uint encoding)
        {
            if (IsLoadStoreMultiple(name))
            {
                return (encoding & (1u << 21)) != 0;
            }

            if (IsVLDnVSTn(name))
            {
                return (encoding & 0xf) != RegisterUtils.PcRegister;
            }

            return (encoding & (1u << 8)) != 0;
        }

        private static bool IsLoadStoreMultiple(InstName name)
        {
            switch (name)
            {
                case InstName.Ldm:
                case InstName.Ldmda:
                case InstName.Ldmdb:
                case InstName.LdmE:
                case InstName.Ldmib:
                case InstName.LdmU:
                case InstName.Stm:
                case InstName.Stmda:
                case InstName.Stmdb:
                case InstName.Stmib:
                case InstName.StmU:
                case InstName.Fldmx:
                case InstName.Fstmx:
                case InstName.Vldm:
                case InstName.Vstm:
                    return true;
            }

            return false;
        }

        private static bool IsVLDnVSTn(InstName name)
        {
            switch (name)
            {
                case InstName.Vld11:
                case InstName.Vld1A:
                case InstName.Vld1M:
                case InstName.Vld21:
                case InstName.Vld2A:
                case InstName.Vld2M:
                case InstName.Vld31:
                case InstName.Vld3A:
                case InstName.Vld3M:
                case InstName.Vld41:
                case InstName.Vld4A:
                case InstName.Vld4M:
                case InstName.Vst11:
                case InstName.Vst1M:
                case InstName.Vst21:
                case InstName.Vst2M:
                case InstName.Vst31:
                case InstName.Vst3M:
                case InstName.Vst41:
                case InstName.Vst4M:
                    return true;
            }

            return false;
        }

        private static bool IsR15RtEncodingSpecial(InstName name, uint encoding)
        {
            if (name == InstName.Vmrs)
            {
                return ((encoding >> 16) & 0xf) == 1;
            }

            return false;
        }

        private static bool IsRegisterWrite(InstFlags flags, InstFlags testFlag)
        {
            return flags.HasFlag(testFlag) && !flags.HasFlag(InstFlags.ReadRd);
        }

        private static bool IsBackwardsBranch(InstName name, uint encoding)
        {
            if (name == InstName.B)
            {
                return ImmUtils.ExtractSImm24Times4(encoding) < 0;
            }

            return false;
        }
    }
}
