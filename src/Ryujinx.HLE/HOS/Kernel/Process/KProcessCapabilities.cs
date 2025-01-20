using Ryujinx.HLE.HOS.Kernel.Memory;
using Ryujinx.HLE.HOS.Kernel.Threading;
using Ryujinx.Horizon.Common;
using System;
using System.Numerics;

namespace Ryujinx.HLE.HOS.Kernel.Process
{
    class KProcessCapabilities
    {
        private const int SvcMaskElementBits = 8;

        public byte[] SvcAccessMask { get; }
        public byte[] IrqAccessMask { get; }

        public ulong AllowedCpuCoresMask { get; private set; }
        public ulong AllowedThreadPriosMask { get; private set; }

        public uint DebuggingFlags { get; private set; }
        public uint HandleTableSize { get; private set; }
        public uint KernelReleaseVersion { get; private set; }
        public uint ApplicationType { get; private set; }

        public KProcessCapabilities()
        {
            // length / number of bits of the underlying type
            SvcAccessMask = new byte[KernelConstants.SupervisorCallCount / SvcMaskElementBits];
            IrqAccessMask = new byte[0x80];
        }

        public Result InitializeForKernel(ReadOnlySpan<uint> capabilities, KPageTableBase memoryManager)
        {
            AllowedCpuCoresMask = 0xf;
            AllowedThreadPriosMask = ulong.MaxValue;
            DebuggingFlags &= ~3u;
            KernelReleaseVersion = KProcess.KernelVersionPacked;

            return Parse(capabilities, memoryManager, false);
        }

        public Result InitializeForUser(ReadOnlySpan<uint> capabilities, KPageTableBase memoryManager, bool isApplication)
        {
            return Parse(capabilities, memoryManager, isApplication);
        }

        private Result Parse(ReadOnlySpan<uint> capabilities, KPageTableBase memoryManager, bool isApplication)
        {
            int mask0 = 0;
            int mask1 = 0;

            for (int index = 0; index < capabilities.Length; index++)
            {
                uint cap = capabilities[index];

                if (cap.GetCapabilityType() != CapabilityType.MapRange)
                {
                    Result result = ParseCapability(cap, ref mask0, ref mask1, memoryManager, isApplication);

                    if (result != Result.Success)
                    {
                        return result;
                    }
                }
                else
                {
                    if ((uint)index + 1 >= capabilities.Length)
                    {
                        return KernelResult.InvalidCombination;
                    }

                    uint prevCap = cap;

                    cap = capabilities[++index];

                    if (((cap + 1) & ~cap) != 0x40)
                    {
                        return KernelResult.InvalidCombination;
                    }

                    if ((cap & 0x78000000) != 0)
                    {
                        return KernelResult.MaximumExceeded;
                    }

                    if ((cap & 0x7ffff80) == 0)
                    {
                        return KernelResult.InvalidSize;
                    }

                    long address = ((long)prevCap << 5) & 0xffffff000;
                    long size = ((long)cap << 5) & 0xfffff000;

                    if (((ulong)(address + size - 1) >> 36) != 0)
                    {
                        return KernelResult.InvalidAddress;
                    }

                    KMemoryPermission perm = (prevCap >> 31) != 0
                        ? KMemoryPermission.Read
                        : KMemoryPermission.ReadAndWrite;

                    Result result;

                    if ((cap >> 31) != 0)
                    {
                        result = KPageTableBase.MapNormalMemory(address, size, perm);
                    }
                    else
                    {
                        result = KPageTableBase.MapIoMemory(address, size, perm);
                    }

                    if (result != Result.Success)
                    {
                        return result;
                    }
                }
            }

            return Result.Success;
        }

        private Result ParseCapability(uint cap, ref int mask0, ref int mask1, KPageTableBase memoryManager, bool isApplication)
        {
            CapabilityType code = cap.GetCapabilityType();

            if (code == CapabilityType.Invalid)
            {
                return KernelResult.InvalidCapability;
            }
            else if (code == CapabilityType.Padding)
            {
                return Result.Success;
            }

            int codeMask = 1 << (32 - BitOperations.LeadingZeroCount(code.GetFlag() + 1));

            // Check if the property was already set.
            if (((mask0 & codeMask) & 0x1e008) != 0)
            {
                return KernelResult.InvalidCombination;
            }

            mask0 |= codeMask;

            switch (code)
            {
                case CapabilityType.CorePriority:
                    {
                        if (AllowedCpuCoresMask != 0 || AllowedThreadPriosMask != 0)
                        {
                            return KernelResult.InvalidCapability;
                        }

                        uint lowestCpuCore = (cap >> 16) & 0xff;
                        uint highestCpuCore = (cap >> 24) & 0xff;

                        if (lowestCpuCore > highestCpuCore)
                        {
                            return KernelResult.InvalidCombination;
                        }

                        uint highestThreadPrio = (cap >> 4) & 0x3f;
                        uint lowestThreadPrio = (cap >> 10) & 0x3f;

                        if (lowestThreadPrio > highestThreadPrio)
                        {
                            return KernelResult.InvalidCombination;
                        }

                        if (highestCpuCore >= KScheduler.CpuCoresCount)
                        {
                            return KernelResult.InvalidCpuCore;
                        }

                        AllowedCpuCoresMask = GetMaskFromMinMax(lowestCpuCore, highestCpuCore);
                        AllowedThreadPriosMask = GetMaskFromMinMax(lowestThreadPrio, highestThreadPrio);

                        if (isApplication)
                            Ryujinx.Common.Logging.Logger.Info?.Print(Ryujinx.Common.Logging.LogClass.Application, $"Application requested cores with index range {lowestCpuCore} to {highestCpuCore}");

                        break;
                    }

                case CapabilityType.SyscallMask:
                    {
                        int slot = ((int)cap >> 29) & 7;

                        int svcSlotMask = 1 << slot;

                        if ((mask1 & svcSlotMask) != 0)
                        {
                            return KernelResult.InvalidCombination;
                        }

                        mask1 |= svcSlotMask;

                        uint svcMask = (cap >> 5) & 0xffffff;

                        int baseSvc = slot * 24;

                        for (int index = 0; index < 24; index++)
                        {
                            if (((svcMask >> index) & 1) == 0)
                            {
                                continue;
                            }

                            int svcId = baseSvc + index;

                            if (svcId >= KernelConstants.SupervisorCallCount)
                            {
                                return KernelResult.MaximumExceeded;
                            }

                            SvcAccessMask[svcId / SvcMaskElementBits] |= (byte)(1 << (svcId % SvcMaskElementBits));
                        }

                        break;
                    }

                case CapabilityType.MapIoPage:
                    {
                        long address = ((long)cap << 4) & 0xffffff000;

                        KPageTableBase.MapIoMemory(address, KPageTableBase.PageSize, KMemoryPermission.ReadAndWrite);

                        break;
                    }

                case CapabilityType.MapRegion:
                    {
                        // TODO: Implement capabilities for MapRegion

                        break;
                    }

                case CapabilityType.InterruptPair:
                    {
                        // TODO: GIC distributor check.
                        int irq0 = ((int)cap >> 12) & 0x3ff;
                        int irq1 = ((int)cap >> 22) & 0x3ff;

                        if (irq0 != 0x3ff)
                        {
                            IrqAccessMask[irq0 / 8] |= (byte)(1 << (irq0 & 7));
                        }

                        if (irq1 != 0x3ff)
                        {
                            IrqAccessMask[irq1 / 8] |= (byte)(1 << (irq1 & 7));
                        }

                        break;
                    }

                case CapabilityType.ProgramType:
                    {
                        uint applicationType = (cap >> 14);

                        if (applicationType > 7)
                        {
                            return KernelResult.ReservedValue;
                        }

                        ApplicationType = applicationType;

                        break;
                    }

                case CapabilityType.KernelVersion:
                    {
                        // Note: This check is bugged on kernel too, we are just replicating the bug here.
                        if ((KernelReleaseVersion >> 17) != 0 || cap < 0x80000)
                        {
                            return KernelResult.ReservedValue;
                        }

                        KernelReleaseVersion = cap;

                        break;
                    }

                case CapabilityType.HandleTable:
                    {
                        uint handleTableSize = cap >> 26;

                        if (handleTableSize > 0x3ff)
                        {
                            return KernelResult.ReservedValue;
                        }

                        HandleTableSize = handleTableSize;

                        break;
                    }

                case CapabilityType.DebugFlags:
                    {
                        uint debuggingFlags = cap >> 19;

                        if (debuggingFlags > 3)
                        {
                            return KernelResult.ReservedValue;
                        }

                        DebuggingFlags &= ~3u;
                        DebuggingFlags |= debuggingFlags;

                        break;
                    }
                default:
                    return KernelResult.InvalidCapability;
            }

            return Result.Success;
        }

        private static ulong GetMaskFromMinMax(uint min, uint max)
        {
            uint range = max - min + 1;

            if (range == 64)
            {
                return ulong.MaxValue;
            }

            ulong mask = (1UL << (int)range) - 1;

            return mask << (int)min;
        }

        public bool IsSvcPermitted(int svcId)
        {
            int index = svcId / SvcMaskElementBits;
            int mask = 1 << (svcId % SvcMaskElementBits);

            return (uint)svcId < KernelConstants.SupervisorCallCount && (SvcAccessMask[index] & mask) != 0;
        }
    }
}
