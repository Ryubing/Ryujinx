using ARMeilleure;
using ARMeilleure.State;
using NUnit.Framework;
using Ryujinx.Cpu.Jit;
using Ryujinx.Memory;
using Ryujinx.Tests.Unicorn;
using System;
using MemoryPermission = Ryujinx.Tests.Unicorn.MemoryPermission;

namespace Ryujinx.Tests.Cpu
{
    [TestFixture]
    public class CpuTest32
    {
        protected static readonly uint Size = (uint)MemoryBlock.GetPageSize();
#pragma warning disable CA2211 // Non-constant fields should not be visible
        protected static uint CodeBaseAddress = Size;
        protected static uint DataBaseAddress = CodeBaseAddress + Size;
#pragma warning restore CA2211

        private uint _currAddress;

        private MemoryBlock _ram;

        private MemoryManager _memory;

        private ExecutionContext _context;

        private CpuContext _cpuContext;
        private UnicornAArch32 _unicornEmu;

        private bool _usingMemory;

        [SetUp]
        public void Setup()
        {
            int pageBits = (int)ulong.Log2(Size);

            _ram = new MemoryBlock(Size * 2);
            _memory = new MemoryManager(_ram, 1ul << (pageBits + 4));
            _memory.IncrementReferenceCount();

            // Some tests depends on hardcoded address that were computed for 4KiB.
            // We change the layout on non 4KiB platforms to keep compat here.
            if (Size > 0x1000)
            {
                DataBaseAddress = 0;
                CodeBaseAddress = Size;
            }

            _currAddress = CodeBaseAddress;

            _memory.Map(CodeBaseAddress, 0, Size, MemoryMapFlags.Private);
            _memory.Map(DataBaseAddress, Size, Size, MemoryMapFlags.Private);

            _context = CpuContext.CreateExecutionContext();
            _context.IsAarch32 = true;

            _cpuContext = new CpuContext(_memory, for64Bit: false);

            // Prevent registering LCQ functions in the FunctionTable to avoid initializing and populating the table,
            // which improves test durations.
            Optimizations.AllowLcqInFunctionTable = false;
            Optimizations.UseUnmanagedDispatchLoop = false;

            _unicornEmu = new UnicornAArch32();
            _unicornEmu.MemoryMap(CodeBaseAddress, Size, MemoryPermission.Read | MemoryPermission.Exec);
            _unicornEmu.MemoryMap(DataBaseAddress, Size, MemoryPermission.Read | MemoryPermission.Write);
            _unicornEmu.PC = CodeBaseAddress;
        }

        [TearDown]
        public void Teardown()
        {
            _unicornEmu.Dispose();
            _unicornEmu = null;

            _memory.DecrementReferenceCount();
            _context.Dispose();
            _ram.Dispose();

            _memory = null;
            _context = null;
            _cpuContext = null;
            _unicornEmu = null;

            _usingMemory = false;
        }

        protected void Reset()
        {
            Teardown();
            Setup();
        }

        protected void Opcode(uint opcode)
        {
            _memory.Write(_currAddress, opcode);

            _unicornEmu.MemoryWrite32(_currAddress, opcode);

            _currAddress += 4;
        }

        protected void ThumbOpcode(ushort opcode)
        {
            _memory.Write(_currAddress, opcode);

            _unicornEmu.MemoryWrite16(_currAddress, opcode);

            _currAddress += 2;
        }

        protected ExecutionContext GetContext() => _context;

        protected void SetContext(uint r0 = 0,
                                  uint r1 = 0,
                                  uint r2 = 0,
                                  uint r3 = 0,
                                  uint sp = 0,
                                  V128 v0 = default,
                                  V128 v1 = default,
                                  V128 v2 = default,
                                  V128 v3 = default,
                                  V128 v4 = default,
                                  V128 v5 = default,
                                  V128 v14 = default,
                                  V128 v15 = default,
                                  bool saturation = false,
                                  bool overflow = false,
                                  bool carry = false,
                                  bool zero = false,
                                  bool negative = false,
                                  int fpscr = 0,
                                  bool thumb = false)
        {
            _context.SetX(0, r0);
            _context.SetX(1, r1);
            _context.SetX(2, r2);
            _context.SetX(3, r3);
            _context.SetX(13, sp);

            _context.SetV(0, v0);
            _context.SetV(1, v1);
            _context.SetV(2, v2);
            _context.SetV(3, v3);
            _context.SetV(4, v4);
            _context.SetV(5, v5);
            _context.SetV(14, v14);
            _context.SetV(15, v15);

            _context.SetPstateFlag(PState.QFlag, saturation);
            _context.SetPstateFlag(PState.VFlag, overflow);
            _context.SetPstateFlag(PState.CFlag, carry);
            _context.SetPstateFlag(PState.ZFlag, zero);
            _context.SetPstateFlag(PState.NFlag, negative);

            _context.Fpscr = (FPSCR)fpscr;

            _context.SetPstateFlag(PState.TFlag, thumb);

            _unicornEmu.R[0] = r0;
            _unicornEmu.R[1] = r1;
            _unicornEmu.R[2] = r2;
            _unicornEmu.R[3] = r3;
            _unicornEmu.SP = sp;

            _unicornEmu.Q[0] = V128ToSimdValue(v0);
            _unicornEmu.Q[1] = V128ToSimdValue(v1);
            _unicornEmu.Q[2] = V128ToSimdValue(v2);
            _unicornEmu.Q[3] = V128ToSimdValue(v3);
            _unicornEmu.Q[4] = V128ToSimdValue(v4);
            _unicornEmu.Q[5] = V128ToSimdValue(v5);
            _unicornEmu.Q[14] = V128ToSimdValue(v14);
            _unicornEmu.Q[15] = V128ToSimdValue(v15);

            _unicornEmu.QFlag = saturation;
            _unicornEmu.OverflowFlag = overflow;
            _unicornEmu.CarryFlag = carry;
            _unicornEmu.ZeroFlag = zero;
            _unicornEmu.NegativeFlag = negative;

            _unicornEmu.Fpscr = fpscr;

            _unicornEmu.ThumbFlag = thumb;
        }

        protected void ExecuteOpcodes(bool runUnicorn = true)
        {
            _cpuContext.Execute(_context, CodeBaseAddress);

            if (runUnicorn)
            {
                _unicornEmu.RunForCount((_currAddress - CodeBaseAddress - 4) / 4);
            }
        }

        protected ExecutionContext SingleOpcode(uint opcode,
                                                uint r0 = 0,
                                                uint r1 = 0,
                                                uint r2 = 0,
                                                uint r3 = 0,
                                                uint sp = 0,
                                                V128 v0 = default,
                                                V128 v1 = default,
                                                V128 v2 = default,
                                                V128 v3 = default,
                                                V128 v4 = default,
                                                V128 v5 = default,
                                                V128 v14 = default,
                                                V128 v15 = default,
                                                bool saturation = false,
                                                bool overflow = false,
                                                bool carry = false,
                                                bool zero = false,
                                                bool negative = false,
                                                int fpscr = 0,
                                                bool runUnicorn = true)
        {
            Opcode(opcode);
            Opcode(0xE12FFF1E); // BX LR
            SetContext(r0, r1, r2, r3, sp, v0, v1, v2, v3, v4, v5, v14, v15, saturation, overflow, carry, zero, negative, fpscr);
            ExecuteOpcodes(runUnicorn);

            return GetContext();
        }

        protected ExecutionContext SingleThumbOpcode(ushort opcode,
                                                     uint r0 = 0,
                                                     uint r1 = 0,
                                                     uint r2 = 0,
                                                     uint r3 = 0,
                                                     uint sp = 0,
                                                     bool saturation = false,
                                                     bool overflow = false,
                                                     bool carry = false,
                                                     bool zero = false,
                                                     bool negative = false,
                                                     int fpscr = 0,
                                                     bool runUnicorn = true)
        {
            ThumbOpcode(opcode);
            ThumbOpcode(0x4770); // BX LR
            SetContext(r0, r1, r2, r3, sp, default, default, default, default, default, default, default, default, saturation, overflow, carry, zero, negative, fpscr, thumb: true);
            ExecuteOpcodes(runUnicorn);

            return GetContext();
        }

        public void RunPrecomputedTestCase(PrecomputedThumbTestCase test)
        {
            foreach (ushort instruction in test.Instructions)
            {
                ThumbOpcode(instruction);
            }

            for (int i = 0; i < 15; i++)
            {
                GetContext().SetX(i, test.StartRegs[i]);
            }

            uint startCpsr = test.StartRegs[15];
            for (int i = 0; i < 32; i++)
            {
                GetContext().SetPstateFlag((PState)i, (startCpsr & (1u << i)) != 0);
            }

            ExecuteOpcodes(runUnicorn: false);

            for (int i = 0; i < 15; i++)
            {
                Assert.That(GetContext().GetX(i), Is.EqualTo(test.FinalRegs[i]));
            }

            uint finalCpsr = test.FinalRegs[15];
            Assert.That(GetContext().Pstate, Is.EqualTo(finalCpsr));
        }

        public void RunPrecomputedTestCase(PrecomputedMemoryThumbTestCase test)
        {
            byte[] testMem = new byte[Size];

            for (ulong i = 0; i < Size; i += 2)
            {
                testMem[i + 0] = (byte)((i + DataBaseAddress) >> 0);
                testMem[i + 1] = (byte)((i + DataBaseAddress) >> 8);
            }

            SetWorkingMemory(0, testMem);

            RunPrecomputedTestCase(new PrecomputedThumbTestCase
            {
                Instructions = test.Instructions,
                StartRegs = test.StartRegs,
                FinalRegs = test.FinalRegs,
            });

            foreach ((ulong address, ushort value) in test.MemoryDelta)
            {
                testMem[address - DataBaseAddress + 0] = (byte)(value >> 0);
                testMem[address - DataBaseAddress + 1] = (byte)(value >> 8);
            }

            byte[] mem = _memory.GetSpan(DataBaseAddress, (int)Size).ToArray();

            Assert.That(mem, Is.EqualTo(testMem), "testmem");
        }

        protected void SetWorkingMemory(uint offset, byte[] data)
        {
            _memory.Write(DataBaseAddress + offset, data);

            _unicornEmu.MemoryWrite(DataBaseAddress + offset, data);

            _usingMemory = true; // When true, CompareAgainstUnicorn checks the working memory for equality too.
        }

        /// <summary>Rounding Mode control field.</summary>
        public enum RMode
        {
            /// <summary>Round to Nearest mode.</summary>
            Rn,
            /// <summary>Round towards Plus Infinity mode.</summary>
            Rp,
            /// <summary>Round towards Minus Infinity mode.</summary>
            Rm,
            /// <summary>Round towards Zero mode.</summary>
            Rz,
        }

        /// <summary>Floating-point Control Register.</summary>
        protected enum Fpcr
        {
            /// <summary>Rounding Mode control field.</summary>
            RMode = 22,
            /// <summary>Flush-to-zero mode control bit.</summary>
            Fz = 24,
            /// <summary>Default NaN mode control bit.</summary>
            Dn = 25,
            /// <summary>Alternative half-precision control bit.</summary>
            Ahp = 26,
        }

        /// <summary>Floating-point Status Register.</summary>
        [Flags]
        protected enum Fpsr
        {
            None = 0,

            /// <summary>Invalid Operation cumulative floating-point exception bit.</summary>
            Ioc = 1 << 0,
            /// <summary>Divide by Zero cumulative floating-point exception bit.</summary>
            Dzc = 1 << 1,
            /// <summary>Overflow cumulative floating-point exception bit.</summary>
            Ofc = 1 << 2,
            /// <summary>Underflow cumulative floating-point exception bit.</summary>
            Ufc = 1 << 3,
            /// <summary>Inexact cumulative floating-point exception bit.</summary>
            Ixc = 1 << 4,
            /// <summary>Input Denormal cumulative floating-point exception bit.</summary>
            Idc = 1 << 7,

            /// <summary>Cumulative saturation bit.</summary>
            Qc = 1 << 27,

            /// <summary>NZCV flags.</summary>
            Nzcv = (1 << 31) | (1 << 30) | (1 << 29) | (1 << 28),
        }

        [Flags]
        protected enum FpSkips
        {
            None = 0,

            IfNaNS = 1,
            IfNaND = 2,

            IfUnderflow = 4,
            IfOverflow = 8,
        }

        protected enum FpTolerances
        {
            None,

            UpToOneUlpsS,
            UpToOneUlpsD,
        }

        protected void CompareAgainstUnicorn(
            Fpsr fpsrMask = Fpsr.None,
            FpSkips fpSkips = FpSkips.None,
            FpTolerances fpTolerances = FpTolerances.None)
        {
            if (fpSkips != FpSkips.None)
            {
                ManageFpSkips(fpSkips);
            }

            Assert.That(_context.GetX(0), Is.EqualTo(_unicornEmu.R[0]), "R0");
            Assert.That(_context.GetX(1), Is.EqualTo(_unicornEmu.R[1]), "R1");
            Assert.That(_context.GetX(2), Is.EqualTo(_unicornEmu.R[2]), "R2");
            Assert.That(_context.GetX(3), Is.EqualTo(_unicornEmu.R[3]), "R3");
            Assert.That(_context.GetX(4), Is.EqualTo(_unicornEmu.R[4]));
            Assert.That(_context.GetX(5), Is.EqualTo(_unicornEmu.R[5]));
            Assert.That(_context.GetX(6), Is.EqualTo(_unicornEmu.R[6]));
            Assert.That(_context.GetX(7), Is.EqualTo(_unicornEmu.R[7]));
            Assert.That(_context.GetX(8), Is.EqualTo(_unicornEmu.R[8]));
            Assert.That(_context.GetX(9), Is.EqualTo(_unicornEmu.R[9]));
            Assert.That(_context.GetX(10), Is.EqualTo(_unicornEmu.R[10]));
            Assert.That(_context.GetX(11), Is.EqualTo(_unicornEmu.R[11]));
            Assert.That(_context.GetX(12), Is.EqualTo(_unicornEmu.R[12]));
            Assert.That(_context.GetX(13), Is.EqualTo(_unicornEmu.SP), "SP");
            Assert.That(_context.GetX(14), Is.EqualTo(_unicornEmu.R[14]));

            if (fpTolerances == FpTolerances.None)
            {
                Assert.That(V128ToSimdValue(_context.GetV(0)), Is.EqualTo(_unicornEmu.Q[0]), "V0");
            }
            else
            {
                ManageFpTolerances(fpTolerances);
            }
            Assert.That(V128ToSimdValue(_context.GetV(1)), Is.EqualTo(_unicornEmu.Q[1]), "V1");
            Assert.That(V128ToSimdValue(_context.GetV(2)), Is.EqualTo(_unicornEmu.Q[2]), "V2");
            Assert.That(V128ToSimdValue(_context.GetV(3)), Is.EqualTo(_unicornEmu.Q[3]), "V3");
            Assert.That(V128ToSimdValue(_context.GetV(4)), Is.EqualTo(_unicornEmu.Q[4]), "V4");
            Assert.That(V128ToSimdValue(_context.GetV(5)), Is.EqualTo(_unicornEmu.Q[5]), "V5");
            Assert.That(V128ToSimdValue(_context.GetV(6)), Is.EqualTo(_unicornEmu.Q[6]));
            Assert.That(V128ToSimdValue(_context.GetV(7)), Is.EqualTo(_unicornEmu.Q[7]));
            Assert.That(V128ToSimdValue(_context.GetV(8)), Is.EqualTo(_unicornEmu.Q[8]));
            Assert.That(V128ToSimdValue(_context.GetV(9)), Is.EqualTo(_unicornEmu.Q[9]));
            Assert.That(V128ToSimdValue(_context.GetV(10)), Is.EqualTo(_unicornEmu.Q[10]));
            Assert.That(V128ToSimdValue(_context.GetV(11)), Is.EqualTo(_unicornEmu.Q[11]));
            Assert.That(V128ToSimdValue(_context.GetV(12)), Is.EqualTo(_unicornEmu.Q[12]));
            Assert.That(V128ToSimdValue(_context.GetV(13)), Is.EqualTo(_unicornEmu.Q[13]));
            Assert.That(V128ToSimdValue(_context.GetV(14)), Is.EqualTo(_unicornEmu.Q[14]), "V14");
            Assert.That(V128ToSimdValue(_context.GetV(15)), Is.EqualTo(_unicornEmu.Q[15]), "V15");

            Assert.Multiple(() =>
            {
                Assert.That(_context.GetPstateFlag(PState.GE0Flag), Is.EqualTo((_unicornEmu.CPSR & (1u << 16)) != 0), "GE0Flag");
                Assert.That(_context.GetPstateFlag(PState.GE1Flag), Is.EqualTo((_unicornEmu.CPSR & (1u << 17)) != 0), "GE1Flag");
                Assert.That(_context.GetPstateFlag(PState.GE2Flag), Is.EqualTo((_unicornEmu.CPSR & (1u << 18)) != 0), "GE2Flag");
                Assert.That(_context.GetPstateFlag(PState.GE3Flag), Is.EqualTo((_unicornEmu.CPSR & (1u << 19)) != 0), "GE3Flag");
                Assert.That(_context.GetPstateFlag(PState.QFlag), Is.EqualTo(_unicornEmu.QFlag), "QFlag");
                Assert.That(_context.GetPstateFlag(PState.VFlag), Is.EqualTo(_unicornEmu.OverflowFlag), "VFlag");
                Assert.That(_context.GetPstateFlag(PState.CFlag), Is.EqualTo(_unicornEmu.CarryFlag), "CFlag");
                Assert.That(_context.GetPstateFlag(PState.ZFlag), Is.EqualTo(_unicornEmu.ZeroFlag), "ZFlag");
                Assert.That(_context.GetPstateFlag(PState.NFlag), Is.EqualTo(_unicornEmu.NegativeFlag), "NFlag");
            });

            Assert.That((int)_context.Fpscr & (int)fpsrMask, Is.EqualTo(_unicornEmu.Fpscr & (int)fpsrMask), "Fpscr");

            if (_usingMemory)
            {
                byte[] mem = _memory.GetSpan(DataBaseAddress, (int)Size).ToArray();
                byte[] unicornMem = _unicornEmu.MemoryRead(DataBaseAddress, Size);

                Assert.That(mem, Is.EqualTo(unicornMem), "Data");
            }
        }

        private void ManageFpSkips(FpSkips fpSkips)
        {
            if (fpSkips.HasFlag(FpSkips.IfNaNS))
            {
                if (float.IsNaN(_unicornEmu.Q[0].AsFloat()))
                {
                    Assert.Ignore("NaN test.");
                }
            }
            else if (fpSkips.HasFlag(FpSkips.IfNaND))
            {
                if (double.IsNaN(_unicornEmu.Q[0].AsDouble()))
                {
                    Assert.Ignore("NaN test.");
                }
            }

            if (fpSkips.HasFlag(FpSkips.IfUnderflow))
            {
                if ((_unicornEmu.Fpscr & (int)Fpsr.Ufc) != 0)
                {
                    Assert.Ignore("Underflow test.");
                }
            }

            if (fpSkips.HasFlag(FpSkips.IfOverflow))
            {
                if ((_unicornEmu.Fpscr & (int)Fpsr.Ofc) != 0)
                {
                    Assert.Ignore("Overflow test.");
                }
            }
        }

        private void ManageFpTolerances(FpTolerances fpTolerances)
        {
            bool IsNormalOrSubnormalS(float f) => float.IsNormal(f) || float.IsSubnormal(f);
            bool IsNormalOrSubnormalD(double d) => double.IsNormal(d) || double.IsSubnormal(d);

            if (!Is.EqualTo(_unicornEmu.Q[0]).ApplyTo(V128ToSimdValue(_context.GetV(0))).IsSuccess)
            {
                if (fpTolerances == FpTolerances.UpToOneUlpsS)
                {
                    if (IsNormalOrSubnormalS(_unicornEmu.Q[0].AsFloat()) &&
                        IsNormalOrSubnormalS(_context.GetV(0).As<float>()))
                    {
                        Assert.Multiple(() =>
                        {
                            Assert.That(_context.GetV(0).Extract<float>(0),
                                Is.EqualTo(_unicornEmu.Q[0].GetFloat(0)).Within(1).Ulps, "V0[0]");
                            Assert.That(_context.GetV(0).Extract<float>(1),
                                Is.EqualTo(_unicornEmu.Q[0].GetFloat(1)).Within(1).Ulps, "V0[1]");
                            Assert.That(_context.GetV(0).Extract<float>(2),
                                Is.EqualTo(_unicornEmu.Q[0].GetFloat(2)).Within(1).Ulps, "V0[2]");
                            Assert.That(_context.GetV(0).Extract<float>(3),
                                Is.EqualTo(_unicornEmu.Q[0].GetFloat(3)).Within(1).Ulps, "V0[3]");
                        });

                        Console.WriteLine(fpTolerances);
                    }
                    else
                    {
                        Assert.That(V128ToSimdValue(_context.GetV(0)), Is.EqualTo(_unicornEmu.Q[0]));
                    }
                }

                if (fpTolerances == FpTolerances.UpToOneUlpsD)
                {
                    if (IsNormalOrSubnormalD(_unicornEmu.Q[0].AsDouble()) &&
                        IsNormalOrSubnormalD(_context.GetV(0).As<double>()))
                    {
                        Assert.Multiple(() =>
                        {
                            Assert.That(_context.GetV(0).Extract<double>(0),
                                Is.EqualTo(_unicornEmu.Q[0].GetDouble(0)).Within(1).Ulps, "V0[0]");
                            Assert.That(_context.GetV(0).Extract<double>(1),
                                Is.EqualTo(_unicornEmu.Q[0].GetDouble(1)).Within(1).Ulps, "V0[1]");
                        });

                        Console.WriteLine(fpTolerances);
                    }
                    else
                    {
                        Assert.That(V128ToSimdValue(_context.GetV(0)), Is.EqualTo(_unicornEmu.Q[0]));
                    }
                }
            }
        }

        private static SimdValue V128ToSimdValue(V128 value)
        {
            return new SimdValue(value.Extract<ulong>(0), value.Extract<ulong>(1));
        }

        protected static V128 MakeVectorScalar(float value) => new(value);
        protected static V128 MakeVectorScalar(double value) => new(value);

        protected static V128 MakeVectorE0(ulong e0) => new(e0, 0);
        protected static V128 MakeVectorE1(ulong e1) => new(0, e1);

        protected static V128 MakeVectorE0E1(ulong e0, ulong e1) => new(e0, e1);

        protected static V128 MakeVectorE0E1E2E3(uint e0, uint e1, uint e2, uint e3)
        {
            return new V128(e0, e1, e2, e3);
        }

        protected static ulong GetVectorE0(V128 vector) => vector.Extract<ulong>(0);
        protected static ulong GetVectorE1(V128 vector) => vector.Extract<ulong>(1);

        protected static ushort GenNormalH()
        {
            uint rnd;

            do
                rnd = TestContext.CurrentContext.Random.NextUShort();
            while ((rnd & 0x7C00u) == 0u ||
                   (~rnd & 0x7C00u) == 0u);

            return (ushort)rnd;
        }

        protected static ushort GenSubnormalH()
        {
            uint rnd;

            do
                rnd = TestContext.CurrentContext.Random.NextUShort();
            while ((rnd & 0x03FFu) == 0u);

            return (ushort)(rnd & 0x83FFu);
        }

        protected static uint GenNormalS()
        {
            uint rnd;

            do
                rnd = TestContext.CurrentContext.Random.NextUInt();
            while ((rnd & 0x7F800000u) == 0u ||
                   (~rnd & 0x7F800000u) == 0u);

            return rnd;
        }

        protected static uint GenSubnormalS()
        {
            uint rnd;

            do
                rnd = TestContext.CurrentContext.Random.NextUInt();
            while ((rnd & 0x007FFFFFu) == 0u);

            return rnd & 0x807FFFFFu;
        }

        protected static ulong GenNormalD()
        {
            ulong rnd;

            do
                rnd = TestContext.CurrentContext.Random.NextULong();
            while ((rnd & 0x7FF0000000000000ul) == 0ul ||
                   (~rnd & 0x7FF0000000000000ul) == 0ul);

            return rnd;
        }

        protected static ulong GenSubnormalD()
        {
            ulong rnd;

            do
                rnd = TestContext.CurrentContext.Random.NextULong();
            while ((rnd & 0x000FFFFFFFFFFFFFul) == 0ul);

            return rnd & 0x800FFFFFFFFFFFFFul;
        }
    }
}
