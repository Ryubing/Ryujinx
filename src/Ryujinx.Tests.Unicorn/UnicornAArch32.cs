using System;
using UnicornEngine.Const;

namespace Ryujinx.Tests.Unicorn
{
    public class UnicornAArch32 : IDisposable
    {
        internal readonly UnicornEngine.Unicorn Uc;
        private bool _isDisposed;

        public IndexedProperty<int, uint> R => new(GetX, SetX);

        public IndexedProperty<int, SimdValue> Q => new(GetQ, SetQ);

        public uint LR
        {
            get => GetRegister(Arm.UC_ARM_REG_LR);
            set => SetRegister(Arm.UC_ARM_REG_LR, value);
        }

        public uint SP
        {
            get => GetRegister(Arm.UC_ARM_REG_SP);
            set => SetRegister(Arm.UC_ARM_REG_SP, value);
        }

        public uint PC
        {
            get => GetRegister(Arm.UC_ARM_REG_PC) & 0xfffffffeu;
            set => SetRegister(Arm.UC_ARM_REG_PC, (value & 0xfffffffeu) | (ThumbFlag ? 1u : 0u));
        }

        public uint CPSR
        {
            get => GetRegister(Arm.UC_ARM_REG_CPSR);
            set => SetRegister(Arm.UC_ARM_REG_CPSR, value);
        }

        public int Fpscr
        {
            get => (int)GetRegister(Arm.UC_ARM_REG_FPSCR) | ((int)GetRegister(Arm.UC_ARM_REG_FPSCR_NZCV));
            set => SetRegister(Arm.UC_ARM_REG_FPSCR, (uint)value);
        }

        public bool QFlag
        {
            get => (CPSR & 0x8000000u) != 0;
            set => CPSR = (CPSR & ~0x8000000u) | (value ? 0x8000000u : 0u);
        }

        public bool OverflowFlag
        {
            get => (CPSR & 0x10000000u) != 0;
            set => CPSR = (CPSR & ~0x10000000u) | (value ? 0x10000000u : 0u);
        }

        public bool CarryFlag
        {
            get => (CPSR & 0x20000000u) != 0;
            set => CPSR = (CPSR & ~0x20000000u) | (value ? 0x20000000u : 0u);
        }

        public bool ZeroFlag
        {
            get => (CPSR & 0x40000000u) != 0;
            set => CPSR = (CPSR & ~0x40000000u) | (value ? 0x40000000u : 0u);
        }

        public bool NegativeFlag
        {
            get => (CPSR & 0x80000000u) != 0;
            set => CPSR = (CPSR & ~0x80000000u) | (value ? 0x80000000u : 0u);
        }

        public bool ThumbFlag
        {
            get => (CPSR & 0x00000020u) != 0;
            set
            {
                CPSR = (CPSR & ~0x00000020u) | (value ? 0x00000020u : 0u);
                SetRegister(Arm.UC_ARM_REG_PC, (GetRegister(Arm.UC_ARM_REG_PC) & 0xfffffffeu) | (value ? 1u : 0u));
            }
        }

        public UnicornAArch32()
        {
            Uc = new UnicornEngine.Unicorn(Common.UC_ARCH_ARM, Common.UC_MODE_LITTLE_ENDIAN);

            SetRegister(Arm.UC_ARM_REG_C1_C0_2, GetRegister(Arm.UC_ARM_REG_C1_C0_2) | 0xf00000);
            SetRegister(Arm.UC_ARM_REG_FPEXC, 0x40000000);
        }

        ~UnicornAArch32()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                Uc.Close();
                _isDisposed = true;
            }
        }

        public void RunForCount(ulong count)
        {
            // FIXME: untilAddr should be 0xFFFFFFFFFFFFFFFFu
            Uc.EmuStart(this.PC, -1, 0, (long)count);
        }

        public void Step()
        {
            RunForCount(1);
        }

        private static readonly int[] _xRegisters =
        [
            Arm.UC_ARM_REG_R0,
            Arm.UC_ARM_REG_R1,
            Arm.UC_ARM_REG_R2,
            Arm.UC_ARM_REG_R3,
            Arm.UC_ARM_REG_R4,
            Arm.UC_ARM_REG_R5,
            Arm.UC_ARM_REG_R6,
            Arm.UC_ARM_REG_R7,
            Arm.UC_ARM_REG_R8,
            Arm.UC_ARM_REG_R9,
            Arm.UC_ARM_REG_R10,
            Arm.UC_ARM_REG_R11,
            Arm.UC_ARM_REG_R12,
            Arm.UC_ARM_REG_R13,
            Arm.UC_ARM_REG_R14,
            Arm.UC_ARM_REG_R15
        ];

#pragma warning disable IDE0051, IDE0052 // Remove unused private member
        private static readonly int[] _qRegisters =
        [
            Arm.UC_ARM_REG_Q0,
            Arm.UC_ARM_REG_Q1,
            Arm.UC_ARM_REG_Q2,
            Arm.UC_ARM_REG_Q3,
            Arm.UC_ARM_REG_Q4,
            Arm.UC_ARM_REG_Q5,
            Arm.UC_ARM_REG_Q6,
            Arm.UC_ARM_REG_Q7,
            Arm.UC_ARM_REG_Q8,
            Arm.UC_ARM_REG_Q9,
            Arm.UC_ARM_REG_Q10,
            Arm.UC_ARM_REG_Q11,
            Arm.UC_ARM_REG_Q12,
            Arm.UC_ARM_REG_Q13,
            Arm.UC_ARM_REG_Q14,
            Arm.UC_ARM_REG_Q15
        ];
#pragma warning restore IDE0051, IDE0052

        public uint GetX(int index)
        {
            if ((uint)index > 15)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            return GetRegister(_xRegisters[index]);
        }

        public void SetX(int index, uint value)
        {
            if ((uint)index > 15)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            SetRegister(_xRegisters[index], value);
        }

        public SimdValue GetQ(int index)
        {
            if ((uint)index > 15)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            // Getting quadword registers from Unicorn A32 seems to be broken, so we combine its 2 doubleword registers instead.
            return GetVector(Arm.UC_ARM_REG_D0 + index * 2);
        }

        public void SetQ(int index, SimdValue value)
        {
            if ((uint)index > 15)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            SetVector(Arm.UC_ARM_REG_D0 + index * 2, value);
        }

        public uint GetRegister(int register)
        {
            byte[] data = new byte[4];

            Uc.RegRead(register, data);

            return BitConverter.ToUInt32(data, 0);
        }

        public void SetRegister(int register, uint value)
        {
            byte[] data = BitConverter.GetBytes(value);

            Uc.RegWrite(register, data);
        }

        public SimdValue GetVector(int register)
        {
            byte[] data = new byte[8];

            Uc.RegRead(register, data);
            ulong lo = BitConverter.ToUInt64(data, 0);
            Uc.RegRead(register + 1, data);
            ulong hi = BitConverter.ToUInt64(data, 0);

            return new SimdValue(lo, hi);
        }

        private void SetVector(int register, SimdValue value)
        {
            byte[] data = BitConverter.GetBytes(value.GetUInt64(0));
            Uc.RegWrite(register, data);
            data = BitConverter.GetBytes(value.GetUInt64(1));
            Uc.RegWrite(register + 1, data);
        }

        public byte[] MemoryRead(ulong address, ulong size)
        {
            byte[] value = new byte[size];

            Uc.MemRead((long)address, value);

            return value;
        }

        public byte MemoryRead8(ulong address) => MemoryRead(address, 1)[0];
        public ushort MemoryRead16(ulong address) => BitConverter.ToUInt16(MemoryRead(address, 2), 0);
        public uint MemoryRead32(ulong address) => BitConverter.ToUInt32(MemoryRead(address, 4), 0);
        public ulong MemoryRead64(ulong address) => BitConverter.ToUInt64(MemoryRead(address, 8), 0);

        public void MemoryWrite(ulong address, byte[] value)
        {
            Uc.MemWrite((long)address, value);
        }

        public void MemoryWrite8(ulong address, byte value) => MemoryWrite(address, [value]);
        public void MemoryWrite16(ulong address, short value) => MemoryWrite(address, BitConverter.GetBytes(value));
        public void MemoryWrite16(ulong address, ushort value) => MemoryWrite(address, BitConverter.GetBytes(value));
        public void MemoryWrite32(ulong address, int value) => MemoryWrite(address, BitConverter.GetBytes(value));
        public void MemoryWrite32(ulong address, uint value) => MemoryWrite(address, BitConverter.GetBytes(value));
        public void MemoryWrite64(ulong address, long value) => MemoryWrite(address, BitConverter.GetBytes(value));
        public void MemoryWrite64(ulong address, ulong value) => MemoryWrite(address, BitConverter.GetBytes(value));

        public void MemoryMap(ulong address, ulong size, MemoryPermission permissions)
        {
            Uc.MemMap((long)address, (long)size, (int)permissions);
        }

        public void MemoryUnmap(ulong address, ulong size)
        {
            Uc.MemUnmap((long)address, (long)size);
        }

        public void MemoryProtect(ulong address, ulong size, MemoryPermission permissions)
        {
            Uc.MemProtect((long)address, (long)size, (int)permissions);
        }
    }
}
