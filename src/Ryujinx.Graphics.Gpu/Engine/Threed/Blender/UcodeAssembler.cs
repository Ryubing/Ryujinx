using System;
using System.Collections.Generic;

namespace Ryujinx.Graphics.Gpu.Engine.Threed.Blender
{
    /// <summary>
    /// Blend microcode instruction.
    /// </summary>
    enum Instruction
    {
        Mmadd = 0,
        Mmsub = 1,
        Min = 2,
        Max = 3,
        Rcp = 4,
        Add = 5,
        Sub = 6,
    }

    /// <summary>
    /// Blend microcode condition code.
    /// </summary>
    enum CC
    {
        F = 0,
        T = 1,
        EQ = 2,
        NE = 3,
        LT = 4,
        LE = 5,
        GT = 6,
        GE = 7,
    }

    /// <summary>
    /// Blend microcode opend B or D value.
    /// </summary>
    enum OpBD
    {
        ConstantZero = 0x0,
        ConstantOne = 0x1,
        SrcRGB = 0x2,
        SrcAAA = 0x3,
        OneMinusSrcAAA = 0x4,
        DstRGB = 0x5,
        DstAAA = 0x6,
        OneMinusDstAAA = 0x7,
        Temp0 = 0x9,
        Temp1 = 0xa,
        Temp2 = 0xb,
        PBR = 0xc,
        ConstantRGB = 0xd,
    }

    /// <summary>
    /// Blend microcode operand A or C value.
    /// </summary>
    enum OpAC
    {
        SrcRGB = 0,
        DstRGB = 1,
        SrcAAA = 2,
        DstAAA = 3,
        Temp0 = 4,
        Temp1 = 5,
        Temp2 = 6,
        PBR = 7,
    }

    /// <summary>
    /// Blend microcode destination operand.
    /// </summary>
    enum OpDst
    {
        Temp0 = 0,
        Temp1 = 1,
        Temp2 = 2,
        PBR = 3,
    }

    /// <summary>
    /// Blend microcode input swizzle.
    /// </summary>
    enum Swizzle
    {
        RGB = 0,
        GBR = 1,
        RRR = 2,
        GGG = 3,
        BBB = 4,
        RToA = 5,
    }

    /// <summary>
    /// Blend microcode output components.
    /// </summary>
    enum WriteMask
    {
        RGB = 0,
        R = 1,
        G = 2,
        B = 3,
    }

    /// <summary>
    /// Floating-point RGB color values.
    /// </summary>
    readonly struct RgbFloat
    {
        /// <summary>
        /// Red component value.
        /// </summary>
        public float R { get; }

        /// <summary>
        /// Green component value.
        /// </summary>
        public float G { get; }

        /// <summary>
        /// Blue component value.
        /// </summary>
        public float B { get; }

        /// <summary>
        /// Creates a new floating-point RGB value.
        /// </summary>
        /// <param name="r">Red component value</param>
        /// <param name="g">Green component value</param>
        /// <param name="b">Blue component value</param>
        public RgbFloat(float r, float g, float b)
        {
            R = r;
            G = g;
            B = b;
        }
    }

    /// <summary>
    /// Blend microcode destination operand, including swizzle, write mask and condition code update flag.
    /// </summary>
    readonly struct Dest
    {
        public static Dest Temp0 => new(OpDst.Temp0, Swizzle.RGB, WriteMask.RGB, false);
        public static Dest Temp1 => new(OpDst.Temp1, Swizzle.RGB, WriteMask.RGB, false);
        public static Dest Temp2 => new(OpDst.Temp2, Swizzle.RGB, WriteMask.RGB, false);
        public static Dest PBR => new(OpDst.PBR, Swizzle.RGB, WriteMask.RGB, false);

        public Dest GBR => new(Dst, Swizzle.GBR, WriteMask, WriteCC);
        public Dest RRR => new(Dst, Swizzle.RRR, WriteMask, WriteCC);
        public Dest GGG => new(Dst, Swizzle.GGG, WriteMask, WriteCC);
        public Dest BBB => new(Dst, Swizzle.BBB, WriteMask, WriteCC);
        public Dest RToA => new(Dst, Swizzle.RToA, WriteMask, WriteCC);

        public Dest R => new(Dst, Swizzle, WriteMask.R, WriteCC);
        public Dest G => new(Dst, Swizzle, WriteMask.G, WriteCC);
        public Dest B => new(Dst, Swizzle, WriteMask.B, WriteCC);

        public Dest CC => new(Dst, Swizzle, WriteMask, true);

        public OpDst Dst { get; }
        public Swizzle Swizzle { get; }
        public WriteMask WriteMask { get; }
        public bool WriteCC { get; }

        /// <summary>
        /// Creates a new blend microcode destination operand.
        /// </summary>
        /// <param name="dst">Operand</param>
        /// <param name="swizzle">Swizzle</param>
        /// <param name="writeMask">Write maks</param>
        /// <param name="writeCC">Indicates if condition codes should be updated</param>
        public Dest(OpDst dst, Swizzle swizzle, WriteMask writeMask, bool writeCC)
        {
            Dst = dst;
            Swizzle = swizzle;
            WriteMask = writeMask;
            WriteCC = writeCC;
        }
    }

    /// <summary>
    /// Blend microcode operaiton.
    /// </summary>
    readonly struct UcodeOp
    {
        public readonly uint Word;

        /// <summary>
        /// Creates a new blend microcode operation.
        /// </summary>
        /// <param name="cc">Condition code that controls whenever the operation is executed or not</param>
        /// <param name="inst">Instruction</param>
        /// <param name="constIndex">Index on the constant table of the constant used by any constant operand</param>
        /// <param name="dest">Destination operand</param>
        /// <param name="srcA">First input operand</param>
        /// <param name="srcB">Second input operand</param>
        /// <param name="srcC">Third input operand</param>
        /// <param name="srcD">Fourth input operand</param>
        public UcodeOp(CC cc, Instruction inst, int constIndex, Dest dest, OpAC srcA, OpBD srcB, OpAC srcC, OpBD srcD)
        {
            Word = (uint)cc |
                ((uint)inst << 3) |
                ((uint)constIndex << 6) |
                ((uint)srcA << 9) |
                ((uint)srcB << 12) |
                ((uint)srcC << 16) |
                ((uint)srcD << 19) |
                ((uint)dest.Swizzle << 23) |
                ((uint)dest.WriteMask << 26) |
                ((uint)dest.Dst << 28) |
                (dest.WriteCC ? (1u << 31) : 0);
        }
    }

    /// <summary>
    /// Blend microcode assembler.
    /// </summary>
    struct UcodeAssembler
    {
        private List<uint> _code;
        private RgbFloat[] _constants;
        private int _constantIndex;

        public void Mul(CC cc, Dest dest, OpAC srcA, OpBD srcB)
        {
            Assemble(cc, Instruction.Mmadd, dest, srcA, srcB, OpAC.SrcRGB, OpBD.ConstantZero);
        }

        public void Madd(CC cc, Dest dest, OpAC srcA, OpBD srcB, OpAC srcC)
        {
            Assemble(cc, Instruction.Mmadd, dest, srcA, srcB, srcC, OpBD.ConstantOne);
        }

        public void Mmadd(CC cc, Dest dest, OpAC srcA, OpBD srcB, OpAC srcC, OpBD srcD)
        {
            Assemble(cc, Instruction.Mmadd, dest, srcA, srcB, srcC, srcD);
        }

        public void Mmsub(CC cc, Dest dest, OpAC srcA, OpBD srcB, OpAC srcC, OpBD srcD)
        {
            Assemble(cc, Instruction.Mmsub, dest, srcA, srcB, srcC, srcD);
        }

        public void Min(CC cc, Dest dest, OpAC srcA, OpBD srcB)
        {
            Assemble(cc, Instruction.Min, dest, srcA, srcB, OpAC.SrcRGB, OpBD.ConstantZero);
        }

        public void Max(CC cc, Dest dest, OpAC srcA, OpBD srcB)
        {
            Assemble(cc, Instruction.Max, dest, srcA, srcB, OpAC.SrcRGB, OpBD.ConstantZero);
        }

        public void Rcp(CC cc, Dest dest, OpAC srcA)
        {
            Assemble(cc, Instruction.Rcp, dest, srcA, OpBD.ConstantZero, OpAC.SrcRGB, OpBD.ConstantZero);
        }

        public void Mov(CC cc, Dest dest, OpBD srcB)
        {
            Assemble(cc, Instruction.Add, dest, OpAC.SrcRGB, srcB, OpAC.SrcRGB, OpBD.ConstantZero);
        }

        public void Add(CC cc, Dest dest, OpBD srcB, OpBD srcD)
        {
            Assemble(cc, Instruction.Add, dest, OpAC.SrcRGB, srcB, OpAC.SrcRGB, srcD);
        }

        public void Sub(CC cc, Dest dest, OpBD srcB, OpBD srcD)
        {
            Assemble(cc, Instruction.Sub, dest, OpAC.SrcRGB, srcB, OpAC.SrcRGB, srcD);
        }

        private void Assemble(CC cc, Instruction inst, Dest dest, OpAC srcA, OpBD srcB, OpAC srcC, OpBD srcD)
        {
            (_code ??= []).Add(new UcodeOp(cc, inst, _constantIndex, dest, srcA, srcB, srcC, srcD).Word);
        }

        public void SetConstant(int index, float r, float g, float b)
        {
            if (_constants == null)
            {
                _constants = new RgbFloat[index + 1];
            }
            else if (_constants.Length <= index)
            {
                Array.Resize(ref _constants, index + 1);
            }

            _constants[index] = new RgbFloat(r, g, b);
            _constantIndex = index;
        }

        public readonly uint[] GetCode()
        {
            return _code?.ToArray();
        }

        public readonly RgbFloat[] GetConstants()
        {
            return _constants;
        }
    }
}
