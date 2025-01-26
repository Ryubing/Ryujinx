#define SimdMov32

using ARMeilleure.State;
using NUnit.Framework;
using NUnit.Framework.Internal;

namespace Ryujinx.Tests.Cpu
{
    [Category("SimdMov32")]
    public sealed class CpuTestSimdMov32 : CpuTest32
    {
#if SimdMov32
        private const int RndCntImm = 2;

        [Test, Pairwise, Description("VMOV.I<size> <Dd/Qd>, #<imm>")]
        public void Movi_V([Range(0u, 10u)] uint variant,
                           [Values(0u, 1u, 2u, 3u)] uint vd,
                           [Values(0x0u)] uint imm,
                           [Values] bool q)
        {
            uint[] variants =
            [
                // I32
                0b0000_0,
                0b0010_0,
                0b0100_0,
                0b0110_0,

                // I16
                0b1000_0,
                0b1010_0,

                // DT
                0b1100_0,
                0b1101_0,
                0b1110_0,
                0b1111_0,

                0b1110_1
            ];

            uint opcode = 0xf2800010u; // VMOV.I32 D0, #0

            uint cmodeOp = variants[variant];

            if (q)
            {
                vd <<= 1;
            }

            opcode |= ((cmodeOp & 1) << 5) | ((cmodeOp & 0x1e) << 7);
            opcode |= (q ? 1u : 0u) << 6;
            opcode |= (imm & 0xf) | ((imm & 0x70) << 12) | ((imm & 0x80) << 16);

            opcode |= (vd & 0x10) << 18;
            opcode |= (vd & 0xf) << 12;

            SingleOpcode(opcode);

            CompareAgainstUnicorn();
        }

        [Test, Pairwise, Description("VMOV.F<size> <Sd>, #<imm>")]
        public void Movi_S([Range(2u, 3u)] uint size,
                           [Values(0u, 1u, 2u, 3u)] uint vd,
                           [Values(0x0u)] uint imm)
        {
            uint opcode = 0xeeb00800u;
            opcode |= (size & 3) << 8;
            opcode |= (imm & 0xf) | ((imm & 0xf0) << 12);

            if (size == 2)
            {
                opcode |= ((vd & 0x1) << 22);
                opcode |= ((vd & 0x1e) << 11);
            }
            else
            {
                opcode |= ((vd & 0x10) << 18);
                opcode |= ((vd & 0xf) << 12);
            }

            SingleOpcode(opcode);

            CompareAgainstUnicorn();
        }

        [Test, Pairwise, Description("VMOV <Rd>, <Sd>")]
        public void Mov_GP([Values(0u, 1u, 2u, 3u)] uint vn,
                           [Values(0u, 1u, 2u, 3u)] uint rt,
                           [Random(RndCntImm)] uint valueRn,
                           [Random(RndCntImm)] ulong valueVn1,
                           [Random(RndCntImm)] ulong valueVn2,
                           [Values] bool op)
        {
            uint opcode = 0xee000a10u; // VMOV S0, R0
            opcode |= (vn & 1) << 7;
            opcode |= (vn & 0x1e) << 15;
            opcode |= (rt & 0xf) << 12;

            if (op)
            {
                opcode |= 1 << 20;
            }

            SingleOpcode(opcode, r0: valueRn, r1: valueRn, r2: valueRn, r3: valueRn, v0: new V128(valueVn1, valueVn2));

            CompareAgainstUnicorn();
        }

        [Test, Pairwise, Description("VMOV.<size> <Rt>, <Dn[x]>")]
        public void Mov_GP_Elem([Range(0u, 7u)] uint vn,
                                [Values(0u, 1u, 2u, 3u)] uint rt,
                                [Range(0u, 2u)] uint size,
                                [Range(0u, 7u)] uint index,
                                [Random(1)] uint valueRn,
                                [Random(1)] ulong valueVn1,
                                [Random(1)] ulong valueVn2,
                                [Values] bool op,
                                [Values] bool u)
        {
            uint opcode = 0xee000b10u; // VMOV.32 D0[0], R0

            uint opEncode = 0b01000;
            switch (size)
            {
                case 0:
                    opEncode = (0b1000) | index & 7;
                    break;
                case 1:
                    opEncode = (0b0001) | ((index & 3) << 1);
                    break;
                case 2:
                    opEncode = (index & 1) << 2;
                    break;
            }

            opcode |= ((opEncode >> 2) << 21) | ((opEncode & 3) << 5);

            opcode |= (vn & 0x10) << 3;
            opcode |= (vn & 0xf) << 16;
            opcode |= (rt & 0xf) << 12;

            if (op)
            {
                opcode |= 1 << 20;
                if (u && size != 2)
                {
                    opcode |= 1 << 23;
                }
            }

            SingleOpcode(opcode, r0: valueRn, r1: valueRn, r2: valueRn, r3: valueRn, v0: new V128(valueVn1, valueVn2), v1: new V128(valueVn2, valueVn1));

            CompareAgainstUnicorn();
        }

        [Test, Pairwise, Description("(VMOV <Rt>, <Rt2>, <Dm>), (VMOV <Dm>, <Rt>, <Rt2>)")]
        public void Mov_GP_D([Values(0u, 1u, 2u, 3u)] uint vm,
                             [Values(0u, 1u, 2u, 3u)] uint rt,
                             [Values(0u, 1u, 2u, 3u)] uint rt2,
                             [Random(RndCntImm)] uint valueRt1,
                             [Random(RndCntImm)] uint valueRt2,
                             [Random(RndCntImm)] ulong valueVn1,
                             [Random(RndCntImm)] ulong valueVn2,
                             [Values] bool op)
        {
            uint opcode = 0xec400b10u; // VMOV D0, R0, R0
            opcode |= (vm & 0x10) << 1;
            opcode |= (vm & 0xf);
            opcode |= (rt & 0xf) << 12;
            opcode |= (rt2 & 0xf) << 16;

            if (op)
            {
                opcode |= 1 << 20;
            }

            SingleOpcode(opcode, r0: valueRt1, r1: valueRt2, r2: valueRt1, r3: valueRt2, v0: new V128(valueVn1, valueVn2));

            CompareAgainstUnicorn();
        }

        [Test, Pairwise, Description("(VMOV <Rt>, <Rt2>, <Sm>, <Sm1>), (VMOV <Sm>, <Sm1>, <Rt>, <Rt2>)")]
        public void Mov_GP_2([Range(0u, 7u)] uint vm,
                             [Values(0u, 1u, 2u, 3u)] uint rt,
                             [Values(0u, 1u, 2u, 3u)] uint rt2,
                             [Random(RndCntImm)] uint valueRt1,
                             [Random(RndCntImm)] uint valueRt2,
                             [Random(RndCntImm)] ulong valueVn1,
                             [Random(RndCntImm)] ulong valueVn2,
                             [Values] bool op)
        {
            uint opcode = 0xec400a10u; // VMOV S0, S1, R0, R0
            opcode |= (vm & 1) << 5;
            opcode |= (vm & 0x1e) >> 1;
            opcode |= (rt & 0xf) << 12;
            opcode |= (rt2 & 0xf) << 16;

            if (op)
            {
                opcode |= 1 << 20;
            }

            SingleOpcode(opcode, r0: valueRt1, r1: valueRt2, r2: valueRt1, r3: valueRt2, v0: new V128(valueVn1, valueVn2), v1: new V128(valueVn2, valueVn1));

            CompareAgainstUnicorn();
        }

        [Test, Pairwise, Description("VMOVN.<size> <Dt>, <Qm>")]
        public void Movn_V([Range(0u, 1u, 2u)] uint size,
                           [Values(0u, 1u, 2u, 3u)] uint vd,
                           [Values(0u, 2u, 4u, 8u)] uint vm)
        {
            uint opcode = 0xf3b20200u; // VMOVN.I16 D0, Q0

            opcode |= (size & 0x3) << 18;
            opcode |= ((vm & 0x10) << 1);
            opcode |= ((vm & 0xf) << 0);

            opcode |= ((vd & 0x10) << 18);
            opcode |= ((vd & 0xf) << 12);

            V128 v0 = new(TestContext.CurrentContext.Random.NextULong(), TestContext.CurrentContext.Random.NextULong());
            V128 v1 = new(TestContext.CurrentContext.Random.NextULong(), TestContext.CurrentContext.Random.NextULong());
            V128 v2 = new(TestContext.CurrentContext.Random.NextULong(), TestContext.CurrentContext.Random.NextULong());
            V128 v3 = new(TestContext.CurrentContext.Random.NextULong(), TestContext.CurrentContext.Random.NextULong());

            SingleOpcode(opcode, v0: v0, v1: v1, v2: v2, v3: v3);

            CompareAgainstUnicorn();
        }

        [Test, Pairwise, Description("VMOVL.<size> <Qd>, <Dm>")]
        public void Vmovl([Values(0u, 1u, 2u, 3u)] uint vm,
                          [Values(0u, 2u, 4u, 6u)] uint vd,
                          [Values(1u, 2u, 4u)] uint imm3H,
                          [Values] bool u)
        {
            // This is not VMOVL because imm3H = 0, but once
            // we shift in the imm3H value it turns into VMOVL.
            uint opcode = 0xf2800a10u; // VMOV.I16 D0, #0

            opcode |= (vm & 0x10) << 1;
            opcode |= (vm & 0xf);
            opcode |= (vd & 0x10) << 18;
            opcode |= (vd & 0xf) << 12;
            opcode |= (imm3H & 0x7) << 19;
            if (u)
            {
                opcode |= 1 << 24;
            }

            V128 v0 = new(TestContext.CurrentContext.Random.NextULong(), TestContext.CurrentContext.Random.NextULong());
            V128 v1 = new(TestContext.CurrentContext.Random.NextULong(), TestContext.CurrentContext.Random.NextULong());
            V128 v2 = new(TestContext.CurrentContext.Random.NextULong(), TestContext.CurrentContext.Random.NextULong());
            V128 v3 = new(TestContext.CurrentContext.Random.NextULong(), TestContext.CurrentContext.Random.NextULong());

            SingleOpcode(opcode, v0: v0, v1: v1, v2: v2, v3: v3);

            CompareAgainstUnicorn();
        }

        [Test, Pairwise, Description("VMVN.<size> <Vt>, <Vm>")]
        public void Vmvn([Range(0u, 1u, 2u)] uint size,
                         [Values(0u, 1u, 2u, 3u)] uint vd,
                         [Values(0u, 2u, 4u, 8u)] uint vm,
                         [Values] bool q)
        {
            uint opcode = 0xf3b00580u; // VMVN D0, D0

            if (q)
            {
                opcode |= 1 << 6;
                vm <<= 1;
                vd <<= 1;
            }

            opcode |= (size & 0x3) << 18;
            opcode |= (vm & 0x10) << 1;
            opcode |= (vm & 0xf) << 0;

            opcode |= (vd & 0x10) << 18;
            opcode |= (vd & 0xf) << 12;

            V128 v0 = new(TestContext.CurrentContext.Random.NextULong(), TestContext.CurrentContext.Random.NextULong());
            V128 v1 = new(TestContext.CurrentContext.Random.NextULong(), TestContext.CurrentContext.Random.NextULong());
            V128 v2 = new(TestContext.CurrentContext.Random.NextULong(), TestContext.CurrentContext.Random.NextULong());
            V128 v3 = new(TestContext.CurrentContext.Random.NextULong(), TestContext.CurrentContext.Random.NextULong());

            SingleOpcode(opcode, v0: v0, v1: v1, v2: v2, v3: v3);

            CompareAgainstUnicorn();
        }

        [Test, Pairwise, Description("VMVN.I<size> <Dd/Qd>, #<imm>")]
        public void Mvni_V([Range(0u, 7u)] uint variant,
                           [Values(0u, 1u, 2u, 3u)] uint vd,
                           [Values(0x0u)] uint imm,
                           [Values] bool q)
        {
            uint[] variants =
            [
                // I32
                0b0000,
                0b0010,
                0b0100,
                0b0110,

                // I16
                0b1000,
                0b1010,

                // I32
                0b1100,
                0b1101
            ];

            uint opcode = 0xf2800030u; // VMVN.I32 D0, #0

            uint cmodeOp = variants[variant];

            if (q)
            {
                vd <<= 1;
            }

            opcode |= (cmodeOp & 0xf) << 8;
            opcode |= (q ? 1u : 0u) << 6;
            opcode |= (imm & 0xf) | ((imm & 0x70) << 12) | ((imm & 0x80) << 16);

            opcode |= (vd & 0x10) << 18;
            opcode |= (vd & 0xf) << 12;

            SingleOpcode(opcode);

            CompareAgainstUnicorn();
        }

        [Test, Pairwise, Description("VTRN.<size> <Vd>, <Vm>")]
        public void Vtrn([Values(0u, 1u, 2u, 3u)] uint vm,
                         [Values(0u, 1u, 2u, 3u)] uint vd,
                         [Values(0u, 1u, 2u)] uint size,
                         [Values] bool q)
        {
            uint opcode = 0xf3b20080u; // VTRN.8 D0, D0
            if (vm == vd)
            {
                return; // Undefined.
            }

            if (q)
            {
                opcode |= 1 << 6;
                vd <<= 1;
                vm <<= 1;
            }

            opcode |= (vm & 0x10) << 1;
            opcode |= (vm & 0xf);
            opcode |= (vd & 0x10) << 18;
            opcode |= (vd & 0xf) << 12;
            opcode |= (size & 0x3) << 18;

            V128 v0 = new(TestContext.CurrentContext.Random.NextULong(), TestContext.CurrentContext.Random.NextULong());
            V128 v1 = new(TestContext.CurrentContext.Random.NextULong(), TestContext.CurrentContext.Random.NextULong());
            V128 v2 = new(TestContext.CurrentContext.Random.NextULong(), TestContext.CurrentContext.Random.NextULong());
            V128 v3 = new(TestContext.CurrentContext.Random.NextULong(), TestContext.CurrentContext.Random.NextULong());

            SingleOpcode(opcode, v0: v0, v1: v1, v2: v2, v3: v3);

            CompareAgainstUnicorn();
        }

        [Test, Pairwise, Description("VZIP.<size> <Vd>, <Vm>")]
        public void Vzip([Values(0u, 1u, 2u, 3u)] uint vm,
                         [Values(0u, 1u, 2u, 3u)] uint vd,
                         [Values(0u, 1u, 2u)] uint size,
                         [Values] bool q)
        {
            uint opcode = 0xf3b20180u; // VZIP.8 D0, D0
            if (vm == vd || (size == 2 && !q))
            {
                return; // Undefined.
            }

            if (q)
            {
                opcode |= 1 << 6;
                vd <<= 1;
                vm <<= 1;
            }

            opcode |= (vm & 0x10) << 1;
            opcode |= (vm & 0xf);
            opcode |= (vd & 0x10) << 18;
            opcode |= (vd & 0xf) << 12;
            opcode |= (size & 0x3) << 18;

            V128 v0 = new(TestContext.CurrentContext.Random.NextULong(), TestContext.CurrentContext.Random.NextULong());
            V128 v1 = new(TestContext.CurrentContext.Random.NextULong(), TestContext.CurrentContext.Random.NextULong());
            V128 v2 = new(TestContext.CurrentContext.Random.NextULong(), TestContext.CurrentContext.Random.NextULong());
            V128 v3 = new(TestContext.CurrentContext.Random.NextULong(), TestContext.CurrentContext.Random.NextULong());

            SingleOpcode(opcode, v0: v0, v1: v1, v2: v2, v3: v3);

            CompareAgainstUnicorn();
        }

        [Test, Pairwise, Description("VUZP.<size> <Vd>, <Vm>")]
        public void Vuzp([Values(0u, 1u, 2u, 3u)] uint vm,
                         [Values(0u, 1u, 2u, 3u)] uint vd,
                         [Values(0u, 1u, 2u)] uint size,
                         [Values] bool q)
        {
            uint opcode = 0xf3b20100u; // VUZP.8 d0, d0
            if (vm == vd || (size == 2 && !q))
            {
                return; // Undefined.
            }

            if (q)
            {
                opcode |= 1 << 6;
                vd <<= 1;
                vm <<= 1;
            }

            opcode |= (vm & 0x10) << 1;
            opcode |= (vm & 0xf);
            opcode |= (vd & 0x10) << 18;
            opcode |= (vd & 0xf) << 12;
            opcode |= (size & 0x3) << 18;

            V128 v0 = new(TestContext.CurrentContext.Random.NextULong(), TestContext.CurrentContext.Random.NextULong());
            V128 v1 = new(TestContext.CurrentContext.Random.NextULong(), TestContext.CurrentContext.Random.NextULong());
            V128 v2 = new(TestContext.CurrentContext.Random.NextULong(), TestContext.CurrentContext.Random.NextULong());
            V128 v3 = new(TestContext.CurrentContext.Random.NextULong(), TestContext.CurrentContext.Random.NextULong());

            SingleOpcode(opcode, v0: v0, v1: v1, v2: v2, v3: v3);

            CompareAgainstUnicorn();
        }

        [Test, Pairwise, Description("VTBL.8 <Dd>, {list}, <Dm>")]
        public void Vtbl([Range(0u, 6u)] uint vm, // Indices, include potentially invalid.
                         [Range(4u, 12u)] uint vn, // Selection.
                         [Values(0u, 1u)] uint vd, // Destinations.
                         [Range(0u, 3u)] uint length,
                         [Values] bool x)
        {
            uint opcode = 0xf3b00800u; // VTBL.8 D0, {D0}, D0
            if (vn + length > 31)
            {
                return; // Undefined.
            }

            if (x)
            {
                opcode |= 1 << 6;
            }
            opcode |= (vm & 0x10) << 1;
            opcode |= (vm & 0xf);
            opcode |= (vd & 0x10) << 18;
            opcode |= (vd & 0xf) << 12;

            opcode |= (vn & 0x10) << 3;
            opcode |= (vn & 0xf) << 16;
            opcode |= (length & 0x3) << 8;

            Randomizer rnd = TestContext.CurrentContext.Random;
            V128 v2 = new(TestContext.CurrentContext.Random.NextULong(), TestContext.CurrentContext.Random.NextULong());
            V128 v3 = new(TestContext.CurrentContext.Random.NextULong(), TestContext.CurrentContext.Random.NextULong());
            V128 v4 = new(TestContext.CurrentContext.Random.NextULong(), TestContext.CurrentContext.Random.NextULong());
            V128 v5 = new(TestContext.CurrentContext.Random.NextULong(), TestContext.CurrentContext.Random.NextULong());

            byte maxIndex = (byte)(length * 8 - 1);
            byte[] b0 = new byte[16];
            byte[] b1 = new byte[16];
            for (int i = 0; i < 16; i++)
            {
                b0[i] = rnd.NextByte(maxIndex);
                b1[i] = rnd.NextByte(maxIndex);
            }

            V128 v0 = new(b0);
            V128 v1 = new(b1);

            SingleOpcode(opcode, v0: v0, v1: v1, v2: v2, v3: v3, v4: v4, v5: v5);

            CompareAgainstUnicorn();
        }

        [Test, Pairwise, Description("VEXT.8 {<Vd>,} <Vn>, <Vm>, #<imm>")]
        public void Vext([Values(0u, 1u, 2u, 3u)] uint vm,
                         [Values(0u, 1u, 2u, 3u)] uint vn,
                         [Values(0u, 1u, 2u, 3u)] uint vd,
                         [Values(0u, 15u)] uint imm4,
                         [Values] bool q)
        {
            uint opcode = 0xf2b00000; // VEXT.32 D0, D0, D0, #0

            if (q)
            {
                opcode |= 1 << 6;
                vd <<= 1;
                vm <<= 1;
                vn <<= 1;
            }
            else if (imm4 > 7)
            {
                return; // Undefined.
            }
            opcode |= (vm & 0x10) << 1;
            opcode |= (vm & 0xf);
            opcode |= (vd & 0x10) << 18;
            opcode |= (vd & 0xf) << 12;
            opcode |= (vn & 0x10) << 3;
            opcode |= (vn & 0xf) << 16;
            opcode |= (imm4 & 0xf) << 8;

            V128 v0 = new(TestContext.CurrentContext.Random.NextULong(), TestContext.CurrentContext.Random.NextULong());
            V128 v1 = new(TestContext.CurrentContext.Random.NextULong(), TestContext.CurrentContext.Random.NextULong());
            V128 v2 = new(TestContext.CurrentContext.Random.NextULong(), TestContext.CurrentContext.Random.NextULong());
            V128 v3 = new(TestContext.CurrentContext.Random.NextULong(), TestContext.CurrentContext.Random.NextULong());

            SingleOpcode(opcode, v0: v0, v1: v1, v2: v2, v3: v3);

            CompareAgainstUnicorn();
        }

        [Test, Pairwise, Description("VDUP.<size> <Vd>, <Rt>")]
        public void Vdup_GP([Values(0u, 1u, 2u, 3u)] uint vd,
                            [Values(0u, 1u, 2u, 3u)] uint rt,
                            [Values(0u, 1u, 2u)] uint size,
                            [Random(RndCntImm)] uint valueRn,
                            [Random(RndCntImm)] ulong valueVn1,
                            [Random(RndCntImm)] ulong valueVn2,
                            [Values] bool q)
        {
            uint opcode = 0xee800b10; // VDUP.32 d0, r0

            if (q)
            {
                opcode |= 1 << 21;
                vd <<= 1;
            }

            opcode |= (vd & 0x10) << 3;
            opcode |= (vd & 0xf) << 16;
            opcode |= (rt & 0xf) << 12;

            opcode |= (size & 1) << 5; // E
            opcode |= (size & 2) << 21; // B

            V128 v1 = new(TestContext.CurrentContext.Random.NextULong(), TestContext.CurrentContext.Random.NextULong());

            SingleOpcode(opcode, r0: valueRn, r1: valueRn, r2: valueRn, r3: valueRn, v0: new V128(valueVn1, valueVn2), v1: v1);

            CompareAgainstUnicorn();
        }

        [Test, Pairwise, Description("VDUP.<size> <Vd>, <Dm[x]>")]
        public void Vdup_S([Values(0u, 1u, 2u, 3u)] uint vd,
                           [Values(0u, 1u, 2u, 3u)] uint vm,
                           [Values(0u, 1u, 2u)] uint size,
                           [Range(0u, 7u)] uint index,
                           [Random(RndCntImm)] ulong valueVn1,
                           [Random(RndCntImm)] ulong valueVn2,
                           [Values] bool q)
        {
            uint opcode = 0xf3b00c00;

            if (q)
            {
                opcode |= 1 << 6;
                vd <<= 1;
            }

            opcode |= (vd & 0x10) << 18;
            opcode |= (vd & 0xf) << 12;
            opcode |= (vm & 0x10) << 1;
            opcode |= (vm & 0xf);

            uint imm4 = 0;
            switch (size)
            {
                case 0:
                    imm4 |= 0b0100 | ((index & 1) << 3);
                    break;
                case 1:
                    imm4 |= 0b0010 | ((index & 3) << 2);
                    break;
                case 2:
                    imm4 |= 0b0001 | ((index & 7) << 1);
                    break;
            }

            opcode |= imm4 << 16;

            V128 v1 = new(TestContext.CurrentContext.Random.NextULong(), TestContext.CurrentContext.Random.NextULong());
            V128 v2 = new(TestContext.CurrentContext.Random.NextULong(), TestContext.CurrentContext.Random.NextULong());
            V128 v3 = new(TestContext.CurrentContext.Random.NextULong(), TestContext.CurrentContext.Random.NextULong());

            SingleOpcode(opcode, v0: new V128(valueVn1, valueVn2), v1: v1, v2: v2, v3: v3);

            CompareAgainstUnicorn();
        }
#endif
    }
}
