using ARMeilleure.IntermediateRepresentation;
using ARMeilleure.Memory;
using System;
using System.Runtime.CompilerServices;

namespace ARMeilleure.State
{
    class NativeContext : IDisposable
    {
        private unsafe struct NativeCtxStorage
        {
            public fixed ulong X[RegisterConsts.IntRegsCount];
            public fixed ulong V[RegisterConsts.VecRegsCount * 2];
            public fixed uint Flags[RegisterConsts.FlagsCount];
            public fixed uint FpFlags[RegisterConsts.FpFlagsCount];
            public long TpidrEl0;
            public long TpidrroEl0;
            public int Counter;
            public ulong DispatchAddress;
            public ulong ExclusiveAddress;
            public ulong ExclusiveValueLow;
            public ulong ExclusiveValueHigh;
            public int Running;
            public long Tpidr2El0;
        }

        private static NativeCtxStorage _dummyStorage = new();

        private readonly IJitMemoryBlock _block;

        public nint BasePtr => _block.Pointer;

        public NativeContext(IJitMemoryAllocator allocator)
        {
            _block = allocator.Allocate((ulong)Unsafe.SizeOf<NativeCtxStorage>());

            GetStorage().ExclusiveAddress = ulong.MaxValue;
        }

        public ulong GetPc()
        {
            // TODO: More precise tracking of PC value.
            return GetStorage().DispatchAddress;
        }

        public unsafe ulong GetX(int index)
        {
            if ((uint)index >= RegisterConsts.IntRegsCount)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            return GetStorage().X[index];
        }

        public unsafe void SetX(int index, ulong value)
        {
            if ((uint)index >= RegisterConsts.IntRegsCount)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            GetStorage().X[index] = value;
        }

        public unsafe V128 GetV(int index)
        {
            if ((uint)index >= RegisterConsts.VecRegsCount)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            return new V128(GetStorage().V[index * 2 + 0], GetStorage().V[index * 2 + 1]);
        }

        public unsafe void SetV(int index, V128 value)
        {
            if ((uint)index >= RegisterConsts.VecRegsCount)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            GetStorage().V[index * 2 + 0] = value.Extract<ulong>(0);
            GetStorage().V[index * 2 + 1] = value.Extract<ulong>(1);
        }

        public unsafe bool GetPstateFlag(PState flag)
        {
            if ((uint)flag >= RegisterConsts.FlagsCount)
            {
                throw new ArgumentException($"Invalid flag \"{flag}\" specified.");
            }

            return GetStorage().Flags[(int)flag] != 0;
        }

        public unsafe void SetPstateFlag(PState flag, bool value)
        {
            if ((uint)flag >= RegisterConsts.FlagsCount)
            {
                throw new ArgumentException($"Invalid flag \"{flag}\" specified.");
            }

            GetStorage().Flags[(int)flag] = value ? 1u : 0u;
        }

        public unsafe uint GetPstate()
        {
            uint value = 0;
            for (int flag = 0; flag < RegisterConsts.FlagsCount; flag++)
            {
                value |= GetStorage().Flags[flag] != 0 ? 1u << flag : 0u;
            }
            return value;
        }

        public unsafe void SetPstate(uint value)
        {
            for (int flag = 0; flag < RegisterConsts.FlagsCount; flag++)
            {
                uint bit = 1u << flag;
                GetStorage().Flags[flag] = (value & bit) == bit ? 1u : 0u;
            }
        }

        public unsafe bool GetFPStateFlag(FPState flag)
        {
            if ((uint)flag >= RegisterConsts.FpFlagsCount)
            {
                throw new ArgumentException($"Invalid flag \"{flag}\" specified.");
            }

            return GetStorage().FpFlags[(int)flag] != 0;
        }

        public unsafe void SetFPStateFlag(FPState flag, bool value)
        {
            if ((uint)flag >= RegisterConsts.FpFlagsCount)
            {
                throw new ArgumentException($"Invalid flag \"{flag}\" specified.");
            }

            GetStorage().FpFlags[(int)flag] = value ? 1u : 0u;
        }

        public unsafe uint GetFPState(uint mask = uint.MaxValue)
        {
            uint value = 0;
            for (int flag = 0; flag < RegisterConsts.FpFlagsCount; flag++)
            {
                uint bit = 1u << flag;

                if ((mask & bit) == bit)
                {
                    value |= GetStorage().FpFlags[flag] != 0 ? bit : 0u;
                }
            }
            return value;
        }

        public unsafe void SetFPState(uint value, uint mask = uint.MaxValue)
        {
            for (int flag = 0; flag < RegisterConsts.FpFlagsCount; flag++)
            {
                uint bit = 1u << flag;

                if ((mask & bit) == bit)
                {
                    GetStorage().FpFlags[flag] = (value & bit) == bit ? 1u : 0u;
                }
            }
        }

        public long GetTpidrEl0() => GetStorage().TpidrEl0;
        public void SetTpidrEl0(long value) => GetStorage().TpidrEl0 = value;

        public long GetTpidrroEl0() => GetStorage().TpidrroEl0;
        public void SetTpidrroEl0(long value) => GetStorage().TpidrroEl0 = value;

        public long GetTpidr2El0() => GetStorage().Tpidr2El0;
        public void SetTpidr2El0(long value) => GetStorage().Tpidr2El0 = value;

        public int GetCounter() => GetStorage().Counter;
        public void SetCounter(int value) => GetStorage().Counter = value;

        public bool GetRunning() => GetStorage().Running != 0;
        public void SetRunning(bool value) => GetStorage().Running = value ? 1 : 0;

        public unsafe static int GetRegisterOffset(Register reg)
        {
            if (reg.Type == RegisterType.Integer)
            {
                if ((uint)reg.Index >= RegisterConsts.IntRegsCount)
                {
                    throw new ArgumentException("Invalid register.");
                }

                return StorageOffset(ref _dummyStorage, ref _dummyStorage.X[reg.Index]);
            }
            else if (reg.Type == RegisterType.Vector)
            {
                if ((uint)reg.Index >= RegisterConsts.VecRegsCount)
                {
                    throw new ArgumentException("Invalid register.");
                }

                return StorageOffset(ref _dummyStorage, ref _dummyStorage.V[reg.Index * 2]);
            }
            else if (reg.Type == RegisterType.Flag)
            {
                if ((uint)reg.Index >= RegisterConsts.FlagsCount)
                {
                    throw new ArgumentException("Invalid register.");
                }

                return StorageOffset(ref _dummyStorage, ref _dummyStorage.Flags[reg.Index]);
            }
            else /* if (reg.Type == RegisterType.FpFlag) */
            {
                if ((uint)reg.Index >= RegisterConsts.FpFlagsCount)
                {
                    throw new ArgumentException("Invalid register.");
                }

                return StorageOffset(ref _dummyStorage, ref _dummyStorage.FpFlags[reg.Index]);
            }
        }

        public static int GetTpidrEl0Offset()
        {
            return StorageOffset(ref _dummyStorage, ref _dummyStorage.TpidrEl0);
        }

        public static int GetTpidrroEl0Offset()
        {
            return StorageOffset(ref _dummyStorage, ref _dummyStorage.TpidrroEl0);
        }

        public static int GetTpidr2El0Offset()
        {
            return StorageOffset(ref _dummyStorage, ref _dummyStorage.Tpidr2El0);
        }

        public static int GetCounterOffset()
        {
            return StorageOffset(ref _dummyStorage, ref _dummyStorage.Counter);
        }

        public static int GetDispatchAddressOffset()
        {
            return StorageOffset(ref _dummyStorage, ref _dummyStorage.DispatchAddress);
        }

        public static int GetExclusiveAddressOffset()
        {
            return StorageOffset(ref _dummyStorage, ref _dummyStorage.ExclusiveAddress);
        }

        public static int GetExclusiveValueOffset()
        {
            return StorageOffset(ref _dummyStorage, ref _dummyStorage.ExclusiveValueLow);
        }

        public static int GetRunningOffset()
        {
            return StorageOffset(ref _dummyStorage, ref _dummyStorage.Running);
        }

        private static int StorageOffset<T>(ref NativeCtxStorage storage, ref T target)
        {
            return (int)Unsafe.ByteOffset(ref Unsafe.As<NativeCtxStorage, T>(ref storage), ref target);
        }

        private unsafe ref NativeCtxStorage GetStorage() => ref Unsafe.AsRef<NativeCtxStorage>((void*)_block.Pointer);

        public void Dispose() => _block.Dispose();
    }
}
