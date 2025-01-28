using ARMeilleure.CodeGen.Linking;
using ARMeilleure.IntermediateRepresentation;
using Microsoft.IO;
using Ryujinx.Common.Memory;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace ARMeilleure.CodeGen.X86
{
    partial class Assembler
    {
        private const int ReservedBytesForJump = 1;

        private const int OpModRMBits = 24;

        private const byte RexPrefix = 0x40;
        private const byte RexWPrefix = 0x48;
        private const byte LockPrefix = 0xf0;

        private const int MaxRegNumber = 15;

        private struct Jump
        {
            public bool IsConditional { get; }
            public X86Condition Condition { get; }
            public Operand JumpLabel { get; }
            public long? JumpTarget { get; set; }
            public long JumpPosition { get; }
            public long Offset { get; set; }
            public int InstSize { get; set; }

            public Jump(Operand jumpLabel, long jumpPosition)
            {
                IsConditional = false;
                Condition = 0;
                JumpLabel = jumpLabel;
                JumpTarget = null;
                JumpPosition = jumpPosition;

                Offset = 0;
                InstSize = 0;
            }

            public Jump(X86Condition condition, Operand jumpLabel, long jumpPosition)
            {
                IsConditional = true;
                Condition = condition;
                JumpLabel = jumpLabel;
                JumpTarget = null;
                JumpPosition = jumpPosition;

                Offset = 0;
                InstSize = 0;
            }
        }

        private struct Reloc
        {
            public int JumpIndex { get; set; }
            public int Position { get; set; }
            public Symbol Symbol { get; set; }
        }

        private readonly List<Jump> _jumps;
        private readonly List<Reloc> _relocs;
        private readonly Dictionary<Operand, long> _labels;
        private readonly Stream _stream;

        public bool HasRelocs => _relocs != null;

        public Assembler(Stream stream, bool relocatable)
        {
            _stream = stream;
            _labels = new Dictionary<Operand, long>();
            _jumps = [];

            _relocs = relocatable ? [] : null;
        }

        public void MarkLabel(Operand label)
        {
            _labels.Add(label, _stream.Position);
        }

        public void Add(Operand dest, Operand source, OperandType type)
        {
            WriteInstruction(dest, source, type, X86Instruction.Add);
        }

        public void Addsd(Operand dest, Operand src1, Operand src2)
        {
            WriteInstruction(dest, src1, src2, X86Instruction.Addsd);
        }

        public void Addss(Operand dest, Operand src1, Operand src2)
        {
            WriteInstruction(dest, src1, src2, X86Instruction.Addss);
        }

        public void And(Operand dest, Operand source, OperandType type)
        {
            WriteInstruction(dest, source, type, X86Instruction.And);
        }

        public void Bsr(Operand dest, Operand source, OperandType type)
        {
            WriteInstruction(dest, source, type, X86Instruction.Bsr);
        }

        public void Bswap(Operand dest)
        {
            WriteInstruction(dest, default, dest.Type, X86Instruction.Bswap);
        }

        public void Call(Operand dest)
        {
            WriteInstruction(dest, default, OperandType.None, X86Instruction.Call);
        }

        public void Cdq()
        {
            WriteByte(0x99);
        }

        public void Cmovcc(Operand dest, Operand source, OperandType type, X86Condition condition)
        {
            ref readonly InstructionInfo info = ref _instTable[(int)X86Instruction.Cmovcc];

            WriteOpCode(dest, default, source, type, info.Flags, info.OpRRM | (int)condition, rrm: true);
        }

        public void Cmp(Operand src1, Operand src2, OperandType type)
        {
            WriteInstruction(src1, src2, type, X86Instruction.Cmp);
        }

        public void Cqo()
        {
            WriteByte(0x48);
            WriteByte(0x99);
        }

        public void Cmpxchg(Operand memOp, Operand src)
        {
            Debug.Assert(memOp.Kind == OperandKind.Memory);

            WriteByte(LockPrefix);

            WriteInstruction(memOp, src, src.Type, X86Instruction.Cmpxchg);
        }

        public void Cmpxchg16(Operand memOp, Operand src)
        {
            Debug.Assert(memOp.Kind == OperandKind.Memory);

            WriteByte(LockPrefix);
            WriteByte(0x66);

            WriteInstruction(memOp, src, src.Type, X86Instruction.Cmpxchg);
        }

        public void Cmpxchg16b(Operand memOp)
        {
            Debug.Assert(memOp.Kind == OperandKind.Memory);

            WriteByte(LockPrefix);

            WriteInstruction(memOp, default, OperandType.None, X86Instruction.Cmpxchg16b);
        }

        public void Cmpxchg8(Operand memOp, Operand src)
        {
            Debug.Assert(memOp.Kind == OperandKind.Memory);

            WriteByte(LockPrefix);

            WriteInstruction(memOp, src, src.Type, X86Instruction.Cmpxchg8);
        }

        public void Comisd(Operand src1, Operand src2)
        {
            WriteInstruction(src1, default, src2, X86Instruction.Comisd);
        }

        public void Comiss(Operand src1, Operand src2)
        {
            WriteInstruction(src1, default, src2, X86Instruction.Comiss);
        }

        public void Cvtsd2ss(Operand dest, Operand src1, Operand src2)
        {
            WriteInstruction(dest, src1, src2, X86Instruction.Cvtsd2ss);
        }

        public void Cvtsi2sd(Operand dest, Operand src1, Operand src2, OperandType type)
        {
            WriteInstruction(dest, src1, src2, X86Instruction.Cvtsi2sd, type);
        }

        public void Cvtsi2ss(Operand dest, Operand src1, Operand src2, OperandType type)
        {
            WriteInstruction(dest, src1, src2, X86Instruction.Cvtsi2ss, type);
        }

        public void Cvtss2sd(Operand dest, Operand src1, Operand src2)
        {
            WriteInstruction(dest, src1, src2, X86Instruction.Cvtss2sd);
        }

        public void Div(Operand source)
        {
            WriteInstruction(default, source, source.Type, X86Instruction.Div);
        }

        public void Divsd(Operand dest, Operand src1, Operand src2)
        {
            WriteInstruction(dest, src1, src2, X86Instruction.Divsd);
        }

        public void Divss(Operand dest, Operand src1, Operand src2)
        {
            WriteInstruction(dest, src1, src2, X86Instruction.Divss);
        }

        public void Idiv(Operand source)
        {
            WriteInstruction(default, source, source.Type, X86Instruction.Idiv);
        }

        public void Imul(Operand source)
        {
            WriteInstruction(default, source, source.Type, X86Instruction.Imul128);
        }

        public void Imul(Operand dest, Operand source, OperandType type)
        {
            if (source.Kind != OperandKind.Register)
            {
                throw new ArgumentException($"Invalid source operand kind \"{source.Kind}\".");
            }

            WriteInstruction(dest, source, type, X86Instruction.Imul);
        }

        public void Imul(Operand dest, Operand src1, Operand src2, OperandType type)
        {
            ref readonly InstructionInfo info = ref _instTable[(int)X86Instruction.Imul];

            if (src2.Kind != OperandKind.Constant)
            {
                throw new ArgumentException($"Invalid source 2 operand kind \"{src2.Kind}\".");
            }

            if (IsImm8(src2.Value, src2.Type) && info.OpRMImm8 != BadOp)
            {
                WriteOpCode(dest, default, src1, type, info.Flags, info.OpRMImm8, rrm: true);

                WriteByte(src2.AsByte());
            }
            else if (IsImm32(src2.Value, src2.Type) && info.OpRMImm32 != BadOp)
            {
                WriteOpCode(dest, default, src1, type, info.Flags, info.OpRMImm32, rrm: true);

                WriteInt32(src2.AsInt32());
            }
            else
            {
                throw new ArgumentException($"Failed to encode constant 0x{src2.Value:X}.");
            }
        }

        public void Insertps(Operand dest, Operand src1, Operand src2, byte imm)
        {
            WriteInstruction(dest, src1, src2, X86Instruction.Insertps);

            WriteByte(imm);
        }

        public void Jcc(X86Condition condition, Operand dest)
        {
            if (dest.Kind == OperandKind.Label)
            {
                _jumps.Add(new Jump(condition, dest, _stream.Position));

                // ReservedBytesForJump
                WriteByte(0);
            }
            else
            {
                throw new ArgumentException("Destination operand must be of kind Label", nameof(dest));
            }
        }

        public void Jcc(X86Condition condition, long offset)
        {
            if (ConstFitsOnS8(offset))
            {
                WriteByte((byte)(0x70 | (int)condition));

                WriteByte((byte)offset);
            }
            else if (ConstFitsOnS32(offset))
            {
                WriteByte(0x0f);
                WriteByte((byte)(0x80 | (int)condition));

                WriteInt32((int)offset);
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }
        }

        public void Jmp(long offset)
        {
            if (ConstFitsOnS8(offset))
            {
                WriteByte(0xeb);

                WriteByte((byte)offset);
            }
            else if (ConstFitsOnS32(offset))
            {
                WriteByte(0xe9);

                WriteInt32((int)offset);
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }
        }

        public void Jmp(Operand dest)
        {
            if (dest.Kind == OperandKind.Label)
            {
                _jumps.Add(new Jump(dest, _stream.Position));

                // ReservedBytesForJump
                WriteByte(0);
            }
            else
            {
                WriteInstruction(dest, default, OperandType.None, X86Instruction.Jmp);
            }
        }

        public void Ldmxcsr(Operand dest)
        {
            WriteInstruction(dest, default, OperandType.I32, X86Instruction.Ldmxcsr);
        }

        public void Lea(Operand dest, Operand source, OperandType type)
        {
            WriteInstruction(dest, source, type, X86Instruction.Lea);
        }

        public void LockOr(Operand dest, Operand source, OperandType type)
        {
            WriteByte(LockPrefix);
            WriteInstruction(dest, source, type, X86Instruction.Or);
        }

        public void Mov(Operand dest, Operand source, OperandType type)
        {
            WriteInstruction(dest, source, type, X86Instruction.Mov);
        }

        public void Mov16(Operand dest, Operand source)
        {
            WriteInstruction(dest, source, OperandType.None, X86Instruction.Mov16);
        }

        public void Mov8(Operand dest, Operand source)
        {
            WriteInstruction(dest, source, OperandType.None, X86Instruction.Mov8);
        }

        public void Movd(Operand dest, Operand source)
        {
            ref readonly InstructionInfo info = ref _instTable[(int)X86Instruction.Movd];

            if (source.Type.IsInteger() || source.Kind == OperandKind.Memory)
            {
                WriteOpCode(dest, default, source, OperandType.None, info.Flags, info.OpRRM, rrm: true);
            }
            else
            {
                WriteOpCode(dest, default, source, OperandType.None, info.Flags, info.OpRMR);
            }
        }

        public void Movdqu(Operand dest, Operand source)
        {
            WriteInstruction(dest, default, source, X86Instruction.Movdqu);
        }

        public void Movhlps(Operand dest, Operand src1, Operand src2)
        {
            WriteInstruction(dest, src1, src2, X86Instruction.Movhlps);
        }

        public void Movlhps(Operand dest, Operand src1, Operand src2)
        {
            WriteInstruction(dest, src1, src2, X86Instruction.Movlhps);
        }

        public void Movq(Operand dest, Operand source)
        {
            ref readonly InstructionInfo info = ref _instTable[(int)X86Instruction.Movd];

            InstructionFlags flags = info.Flags | InstructionFlags.RexW;

            if (source.Type.IsInteger() || source.Kind == OperandKind.Memory)
            {
                WriteOpCode(dest, default, source, OperandType.None, flags, info.OpRRM, rrm: true);
            }
            else if (dest.Type.IsInteger() || dest.Kind == OperandKind.Memory)
            {
                WriteOpCode(dest, default, source, OperandType.None, flags, info.OpRMR);
            }
            else
            {
                WriteInstruction(dest, source, OperandType.None, X86Instruction.Movq);
            }
        }

        public void Movsd(Operand dest, Operand src1, Operand src2)
        {
            WriteInstruction(dest, src1, src2, X86Instruction.Movsd);
        }

        public void Movss(Operand dest, Operand src1, Operand src2)
        {
            WriteInstruction(dest, src1, src2, X86Instruction.Movss);
        }

        public void Movsx16(Operand dest, Operand source, OperandType type)
        {
            WriteInstruction(dest, source, type, X86Instruction.Movsx16);
        }

        public void Movsx32(Operand dest, Operand source, OperandType type)
        {
            WriteInstruction(dest, source, type, X86Instruction.Movsx32);
        }

        public void Movsx8(Operand dest, Operand source, OperandType type)
        {
            WriteInstruction(dest, source, type, X86Instruction.Movsx8);
        }

        public void Movzx16(Operand dest, Operand source, OperandType type)
        {
            WriteInstruction(dest, source, type, X86Instruction.Movzx16);
        }

        public void Movzx8(Operand dest, Operand source, OperandType type)
        {
            WriteInstruction(dest, source, type, X86Instruction.Movzx8);
        }

        public void Mul(Operand source)
        {
            WriteInstruction(default, source, source.Type, X86Instruction.Mul128);
        }

        public void Mulsd(Operand dest, Operand src1, Operand src2)
        {
            WriteInstruction(dest, src1, src2, X86Instruction.Mulsd);
        }

        public void Mulss(Operand dest, Operand src1, Operand src2)
        {
            WriteInstruction(dest, src1, src2, X86Instruction.Mulss);
        }

        public void Neg(Operand dest)
        {
            WriteInstruction(dest, default, dest.Type, X86Instruction.Neg);
        }

        public void Not(Operand dest)
        {
            WriteInstruction(dest, default, dest.Type, X86Instruction.Not);
        }

        public void Or(Operand dest, Operand source, OperandType type)
        {
            WriteInstruction(dest, source, type, X86Instruction.Or);
        }

        public void Pclmulqdq(Operand dest, Operand source, byte imm)
        {
            WriteInstruction(dest, default, source, X86Instruction.Pclmulqdq);

            WriteByte(imm);
        }

        public void Pcmpeqw(Operand dest, Operand src1, Operand src2)
        {
            WriteInstruction(dest, src1, src2, X86Instruction.Pcmpeqw);
        }

        public void Pextrb(Operand dest, Operand source, byte imm)
        {
            WriteInstruction(dest, default, source, X86Instruction.Pextrb);

            WriteByte(imm);
        }

        public void Pextrd(Operand dest, Operand source, byte imm)
        {
            WriteInstruction(dest, default, source, X86Instruction.Pextrd);

            WriteByte(imm);
        }

        public void Pextrq(Operand dest, Operand source, byte imm)
        {
            WriteInstruction(dest, default, source, X86Instruction.Pextrq);

            WriteByte(imm);
        }

        public void Pextrw(Operand dest, Operand source, byte imm)
        {
            WriteInstruction(dest, default, source, X86Instruction.Pextrw);

            WriteByte(imm);
        }

        public void Pinsrb(Operand dest, Operand src1, Operand src2, byte imm)
        {
            WriteInstruction(dest, src1, src2, X86Instruction.Pinsrb);

            WriteByte(imm);
        }

        public void Pinsrd(Operand dest, Operand src1, Operand src2, byte imm)
        {
            WriteInstruction(dest, src1, src2, X86Instruction.Pinsrd);

            WriteByte(imm);
        }

        public void Pinsrq(Operand dest, Operand src1, Operand src2, byte imm)
        {
            WriteInstruction(dest, src1, src2, X86Instruction.Pinsrq);

            WriteByte(imm);
        }

        public void Pinsrw(Operand dest, Operand src1, Operand src2, byte imm)
        {
            WriteInstruction(dest, src1, src2, X86Instruction.Pinsrw);

            WriteByte(imm);
        }

        public void Pop(Operand dest)
        {
            if (dest.Kind == OperandKind.Register)
            {
                WriteCompactInst(dest, 0x58);
            }
            else
            {
                WriteInstruction(dest, default, dest.Type, X86Instruction.Pop);
            }
        }

        public void Popcnt(Operand dest, Operand source, OperandType type)
        {
            WriteInstruction(dest, source, type, X86Instruction.Popcnt);
        }

        public void Pshufd(Operand dest, Operand source, byte imm)
        {
            WriteInstruction(dest, default, source, X86Instruction.Pshufd);

            WriteByte(imm);
        }

        public void Push(Operand source)
        {
            if (source.Kind == OperandKind.Register)
            {
                WriteCompactInst(source, 0x50);
            }
            else
            {
                WriteInstruction(default, source, source.Type, X86Instruction.Push);
            }
        }

        public void Return()
        {
            WriteByte(0xc3);
        }

        public void Ror(Operand dest, Operand source, OperandType type)
        {
            WriteShiftInst(dest, source, type, X86Instruction.Ror);
        }

        public void Sar(Operand dest, Operand source, OperandType type)
        {
            WriteShiftInst(dest, source, type, X86Instruction.Sar);
        }

        public void Shl(Operand dest, Operand source, OperandType type)
        {
            WriteShiftInst(dest, source, type, X86Instruction.Shl);
        }

        public void Shr(Operand dest, Operand source, OperandType type)
        {
            WriteShiftInst(dest, source, type, X86Instruction.Shr);
        }

        public void Setcc(Operand dest, X86Condition condition)
        {
            ref readonly InstructionInfo info = ref _instTable[(int)X86Instruction.Setcc];

            WriteOpCode(dest, default, default, OperandType.None, info.Flags, info.OpRRM | (int)condition);
        }

        public void Stmxcsr(Operand dest)
        {
            WriteInstruction(dest, default, OperandType.I32, X86Instruction.Stmxcsr);
        }

        public void Sub(Operand dest, Operand source, OperandType type)
        {
            WriteInstruction(dest, source, type, X86Instruction.Sub);
        }

        public void Subsd(Operand dest, Operand src1, Operand src2)
        {
            WriteInstruction(dest, src1, src2, X86Instruction.Subsd);
        }

        public void Subss(Operand dest, Operand src1, Operand src2)
        {
            WriteInstruction(dest, src1, src2, X86Instruction.Subss);
        }

        public void Test(Operand src1, Operand src2, OperandType type)
        {
            WriteInstruction(src1, src2, type, X86Instruction.Test);
        }

        public void Xor(Operand dest, Operand source, OperandType type)
        {
            WriteInstruction(dest, source, type, X86Instruction.Xor);
        }

        public void Xorps(Operand dest, Operand src1, Operand src2)
        {
            WriteInstruction(dest, src1, src2, X86Instruction.Xorps);
        }

        public void WriteInstruction(
            X86Instruction inst,
            Operand dest,
            Operand source,
            OperandType type = OperandType.None)
        {
            WriteInstruction(dest, default, source, inst, type);
        }

        public void WriteInstruction(X86Instruction inst, Operand dest, Operand src1, Operand src2)
        {
            if (src2.Kind == OperandKind.Constant)
            {
                WriteInstruction(src1, dest, src2, inst);
            }
            else
            {
                WriteInstruction(dest, src1, src2, inst);
            }
        }

        public void WriteInstruction(
            X86Instruction inst,
            Operand dest,
            Operand src1,
            Operand src2,
            OperandType type)
        {
            WriteInstruction(dest, src1, src2, inst, type);
        }

        public void WriteInstruction(X86Instruction inst, Operand dest, Operand source, byte imm)
        {
            WriteInstruction(dest, default, source, inst);

            WriteByte(imm);
        }

        public void WriteInstruction(
            X86Instruction inst,
            Operand dest,
            Operand src1,
            Operand src2,
            Operand src3)
        {
            // 3+ operands can only be encoded with the VEX encoding scheme.
            Debug.Assert(HardwareCapabilities.SupportsVexEncoding);

            WriteInstruction(dest, src1, src2, inst);

            WriteByte((byte)(src3.AsByte() << 4));
        }

        public void WriteInstruction(
            X86Instruction inst,
            Operand dest,
            Operand src1,
            Operand src2,
            byte imm)
        {
            WriteInstruction(dest, src1, src2, inst);

            WriteByte(imm);
        }

        private void WriteShiftInst(Operand dest, Operand source, OperandType type, X86Instruction inst)
        {
            if (source.Kind == OperandKind.Register)
            {
                X86Register shiftReg = (X86Register)source.GetRegister().Index;

                Debug.Assert(shiftReg == X86Register.Rcx, $"Invalid shift register \"{shiftReg}\".");

                source = default;
            }
            else if (source.Kind == OperandKind.Constant)
            {
                source = Operand.Factory.Const((int)source.Value & (dest.Type == OperandType.I32 ? 0x1f : 0x3f));
            }

            WriteInstruction(dest, source, type, inst);
        }

        private void WriteInstruction(Operand dest, Operand source, OperandType type, X86Instruction inst)
        {
            ref readonly InstructionInfo info = ref _instTable[(int)inst];

            if (source != default)
            {
                if (source.Kind == OperandKind.Constant)
                {
                    ulong imm = source.Value;

                    if (inst == X86Instruction.Mov8)
                    {
                        WriteOpCode(dest, default, default, type, info.Flags, info.OpRMImm8);

                        WriteByte((byte)imm);
                    }
                    else if (inst == X86Instruction.Mov16)
                    {
                        WriteOpCode(dest, default, default, type, info.Flags, info.OpRMImm32);

                        WriteInt16((short)imm);
                    }
                    else if (IsImm8(imm, type) && info.OpRMImm8 != BadOp)
                    {
                        WriteOpCode(dest, default, default, type, info.Flags, info.OpRMImm8);

                        WriteByte((byte)imm);
                    }
                    else if (!source.Relocatable && IsImm32(imm, type) && info.OpRMImm32 != BadOp)
                    {
                        WriteOpCode(dest, default, default, type, info.Flags, info.OpRMImm32);

                        WriteInt32((int)imm);
                    }
                    else if (dest != default && dest.Kind == OperandKind.Register && info.OpRImm64 != BadOp)
                    {
                        int rexPrefix = GetRexPrefix(dest, source, type, rrm: false);

                        if (rexPrefix != 0)
                        {
                            WriteByte((byte)rexPrefix);
                        }

                        WriteByte((byte)(info.OpRImm64 + (dest.GetRegister().Index & 0b111)));

                        if (HasRelocs && source.Relocatable)
                        {
                            _relocs.Add(new Reloc
                            {
                                JumpIndex = _jumps.Count - 1,
                                Position = (int)_stream.Position,
                                Symbol = source.Symbol,
                            });
                        }

                        WriteUInt64(imm);
                    }
                    else
                    {
                        throw new ArgumentException($"Failed to encode constant 0x{imm:X}.");
                    }
                }
                else if (source.Kind == OperandKind.Register && info.OpRMR != BadOp)
                {
                    WriteOpCode(dest, default, source, type, info.Flags, info.OpRMR);
                }
                else if (info.OpRRM != BadOp)
                {
                    WriteOpCode(dest, default, source, type, info.Flags, info.OpRRM, rrm: true);
                }
                else
                {
                    throw new ArgumentException($"Invalid source operand kind \"{source.Kind}\".");
                }
            }
            else if (info.OpRRM != BadOp)
            {
                WriteOpCode(dest, default, source, type, info.Flags, info.OpRRM, rrm: true);
            }
            else if (info.OpRMR != BadOp)
            {
                WriteOpCode(dest, default, source, type, info.Flags, info.OpRMR);
            }
            else
            {
                throw new ArgumentNullException(nameof(source));
            }
        }

        private void WriteInstruction(
            Operand dest,
            Operand src1,
            Operand src2,
            X86Instruction inst,
            OperandType type = OperandType.None)
        {
            ref readonly InstructionInfo info = ref _instTable[(int)inst];

            if (src2 != default)
            {
                if (src2.Kind == OperandKind.Constant)
                {
                    ulong imm = src2.Value;

                    if ((byte)imm == imm && info.OpRMImm8 != BadOp)
                    {
                        WriteOpCode(dest, src1, default, type, info.Flags, info.OpRMImm8);

                        WriteByte((byte)imm);
                    }
                    else
                    {
                        throw new ArgumentException($"Failed to encode constant 0x{imm:X}.");
                    }
                }
                else if (src2.Kind == OperandKind.Register && info.OpRMR != BadOp)
                {
                    WriteOpCode(dest, src1, src2, type, info.Flags, info.OpRMR);
                }
                else if (info.OpRRM != BadOp)
                {
                    WriteOpCode(dest, src1, src2, type, info.Flags, info.OpRRM, rrm: true);
                }
                else
                {
                    throw new ArgumentException($"Invalid source operand kind \"{src2.Kind}\".");
                }
            }
            else if (info.OpRRM != BadOp)
            {
                WriteOpCode(dest, src1, src2, type, info.Flags, info.OpRRM, rrm: true);
            }
            else if (info.OpRMR != BadOp)
            {
                WriteOpCode(dest, src1, src2, type, info.Flags, info.OpRMR);
            }
            else
            {
                throw new ArgumentNullException(nameof(src2));
            }
        }

        private void WriteOpCode(
            Operand dest,
            Operand src1,
            Operand src2,
            OperandType type,
            InstructionFlags flags,
            int opCode,
            bool rrm = false)
        {
            int rexPrefix = GetRexPrefix(dest, src2, type, rrm);

            if ((flags & InstructionFlags.RexW) != 0)
            {
                rexPrefix |= RexWPrefix;
            }

            int modRM = (opCode >> OpModRMBits) << 3;

            MemoryOperand memOp = default;
            bool hasMemOp = false;

            if (dest != default)
            {
                if (dest.Kind == OperandKind.Register)
                {
                    int regIndex = dest.GetRegister().Index;

                    modRM |= (regIndex & 0b111) << (rrm ? 3 : 0);

                    if ((flags & InstructionFlags.Reg8Dest) != 0 && regIndex >= 4)
                    {
                        rexPrefix |= RexPrefix;
                    }
                }
                else if (dest.Kind == OperandKind.Memory)
                {
                    memOp = dest.GetMemory();
                    hasMemOp = true;
                }
                else
                {
                    throw new ArgumentException("Invalid destination operand kind \"" + dest.Kind + "\".");
                }
            }

            if (src2 != default)
            {
                if (src2.Kind == OperandKind.Register)
                {
                    int regIndex = src2.GetRegister().Index;

                    modRM |= (regIndex & 0b111) << (rrm ? 0 : 3);

                    if ((flags & InstructionFlags.Reg8Src) != 0 && regIndex >= 4)
                    {
                        rexPrefix |= RexPrefix;
                    }
                }
                else if (src2.Kind == OperandKind.Memory && !hasMemOp)
                {
                    memOp = src2.GetMemory();
                    hasMemOp = true;
                }
                else
                {
                    throw new ArgumentException("Invalid source operand kind \"" + src2.Kind + "\".");
                }
            }

            bool needsSibByte = false;
            bool needsDisplacement = false;

            int sib = 0;

            if (hasMemOp)
            {
                // Either source or destination is a memory operand.
                Register baseReg = memOp.BaseAddress.GetRegister();

                X86Register baseRegLow = (X86Register)(baseReg.Index & 0b111);

                needsSibByte = memOp.Index != default || baseRegLow == X86Register.Rsp;
                needsDisplacement = memOp.Displacement != 0 || baseRegLow == X86Register.Rbp;

                if (needsDisplacement)
                {
                    if (ConstFitsOnS8(memOp.Displacement))
                    {
                        modRM |= 0x40;
                    }
                    else /* if (ConstFitsOnS32(memOp.Displacement)) */
                    {
                        modRM |= 0x80;
                    }
                }

                if (baseReg.Index >= 8)
                {
                    Debug.Assert((uint)baseReg.Index <= MaxRegNumber);

                    rexPrefix |= RexPrefix | (baseReg.Index >> 3);
                }

                if (needsSibByte)
                {
                    sib = (int)baseRegLow;

                    if (memOp.Index != default)
                    {
                        int indexReg = memOp.Index.GetRegister().Index;

                        Debug.Assert(indexReg != (int)X86Register.Rsp, "Using RSP as index register on the memory operand is not allowed.");

                        if (indexReg >= 8)
                        {
                            Debug.Assert((uint)indexReg <= MaxRegNumber);

                            rexPrefix |= RexPrefix | (indexReg >> 3) << 1;
                        }

                        sib |= (indexReg & 0b111) << 3;
                    }
                    else
                    {
                        sib |= 0b100 << 3;
                    }

                    sib |= (int)memOp.Scale << 6;

                    modRM |= 0b100;
                }
                else
                {
                    modRM |= (int)baseRegLow;
                }
            }
            else
            {
                // Source and destination are registers.
                modRM |= 0xc0;
            }

            Debug.Assert(opCode != BadOp, "Invalid opcode value.");

            if ((flags & InstructionFlags.Evex) != 0 && HardwareCapabilities.SupportsEvexEncoding)
            {
                WriteEvexInst(dest, src1, src2, type, flags, opCode);

                opCode &= 0xff;
            }
            else if ((flags & InstructionFlags.Vex) != 0 && HardwareCapabilities.SupportsVexEncoding)
            {
                // In a vex encoding, only one prefix can be active at a time. The active prefix is encoded in the second byte using two bits.

                int vexByte2 = (flags & InstructionFlags.PrefixMask) switch
                {
                    InstructionFlags.Prefix66 => 1,
                    InstructionFlags.PrefixF3 => 2,
                    InstructionFlags.PrefixF2 => 3,
                    _ => 0,
                };

                if (src1 != default)
                {
                    vexByte2 |= (src1.GetRegister().Index ^ 0xf) << 3;
                }
                else
                {
                    vexByte2 |= 0b1111 << 3;
                }

                ushort opCodeHigh = (ushort)(opCode >> 8);

                if ((rexPrefix & 0b1011) == 0 && opCodeHigh == 0xf)
                {
                    // Two-byte form.
                    WriteByte(0xc5);

                    vexByte2 |= (~rexPrefix & 4) << 5;

                    WriteByte((byte)vexByte2);
                }
                else
                {
                    // Three-byte form.
                    WriteByte(0xc4);

                    int vexByte1 = (~rexPrefix & 7) << 5;

                    switch (opCodeHigh)
                    {
                        case 0xf:
                            vexByte1 |= 1;
                            break;
                        case 0xf38:
                            vexByte1 |= 2;
                            break;
                        case 0xf3a:
                            vexByte1 |= 3;
                            break;

                        default:
                            Debug.Assert(false, $"Failed to VEX encode opcode 0x{opCode:X}.");
                            break;
                    }

                    vexByte2 |= (rexPrefix & 8) << 4;

                    WriteByte((byte)vexByte1);
                    WriteByte((byte)vexByte2);
                }

                opCode &= 0xff;
            }
            else
            {
                if (flags.HasFlag(InstructionFlags.Prefix66))
                {
                    WriteByte(0x66);
                }

                if (flags.HasFlag(InstructionFlags.PrefixF2))
                {
                    WriteByte(0xf2);
                }

                if (flags.HasFlag(InstructionFlags.PrefixF3))
                {
                    WriteByte(0xf3);
                }

                if (rexPrefix != 0)
                {
                    WriteByte((byte)rexPrefix);
                }
            }

            if (dest != default && (flags & InstructionFlags.RegOnly) != 0)
            {
                opCode += dest.GetRegister().Index & 7;
            }

            if ((opCode & 0xff0000) != 0)
            {
                WriteByte((byte)(opCode >> 16));
            }

            if ((opCode & 0xff00) != 0)
            {
                WriteByte((byte)(opCode >> 8));
            }

            WriteByte((byte)opCode);

            if ((flags & InstructionFlags.RegOnly) == 0)
            {
                WriteByte((byte)modRM);

                if (needsSibByte)
                {
                    WriteByte((byte)sib);
                }

                if (needsDisplacement)
                {
                    if (ConstFitsOnS8(memOp.Displacement))
                    {
                        WriteByte((byte)memOp.Displacement);
                    }
                    else /* if (ConstFitsOnS32(memOp.Displacement)) */
                    {
                        WriteInt32(memOp.Displacement);
                    }
                }
            }
        }

        private void WriteEvexInst(
            Operand dest,
            Operand src1,
            Operand src2,
            OperandType type,
            InstructionFlags flags,
            int opCode,
            bool broadcast = false,
            int registerWidth = 128,
            int maskRegisterIdx = 0,
            bool zeroElements = false)
        {
            int op1Idx = dest.GetRegister().Index;
            int op2Idx = src1.GetRegister().Index;
            int op3Idx = src2.GetRegister().Index;

            WriteByte(0x62);

            // P0
            // Extend operand 1 register
            bool r = (op1Idx & 8) == 0;
            // Extend operand 3 register
            bool x = (op3Idx & 16) == 0;
            // Extend operand 3 register
            bool b = (op3Idx & 8) == 0;
            // Extend operand 1 register
            bool rp = (op1Idx & 16) == 0;
            // Escape code index
            byte mm = 0b00;

            switch ((ushort)(opCode >> 8))
            {
                case 0xf00:
                    mm = 0b01;
                    break;
                case 0xf38:
                    mm = 0b10;
                    break;
                case 0xf3a:
                    mm = 0b11;
                    break;

                default:
                    Debug.Fail($"Failed to EVEX encode opcode 0x{opCode:X}.");
                    break;
            }

            WriteByte(
                (byte)(
                    (r ? 0x80 : 0) |
                    (x ? 0x40 : 0) |
                    (b ? 0x20 : 0) |
                    (rp ? 0x10 : 0) |
                    mm));

            // P1
            // Specify 64-bit lane mode
            bool w = Is64Bits(type);
            // Operand 2 register index
            byte vvvv = (byte)(~op2Idx & 0b1111);
            // Opcode prefix
            byte pp = (flags & InstructionFlags.PrefixMask) switch
            {
                InstructionFlags.Prefix66 => 0b01,
                InstructionFlags.PrefixF3 => 0b10,
                InstructionFlags.PrefixF2 => 0b11,
                _ => 0,
            };
            WriteByte(
                (byte)(
                    (w ? 0x80 : 0) |
                    (vvvv << 3) |
                    0b100 |
                    pp));

            // P2
            // Mask register determines what elements to zero, rather than what elements to merge
            bool z = zeroElements;
            // Specifies register-width
            byte ll = 0b00;
            switch (registerWidth)
            {
                case 128:
                    ll = 0b00;
                    break;
                case 256:
                    ll = 0b01;
                    break;
                case 512:
                    ll = 0b10;
                    break;

                default:
                    Debug.Fail($"Invalid EVEX vector register width {registerWidth}.");
                    break;
            }
            // Embedded broadcast in the case of a memory operand
            bool bcast = broadcast;
            // Extend operand 2 register
            bool vp = (op2Idx & 16) == 0;
            // Mask register index
            Debug.Assert(maskRegisterIdx < 8, $"Invalid mask register index {maskRegisterIdx}.");
            byte aaa = (byte)(maskRegisterIdx & 0b111);

            WriteByte(
                (byte)(
                    (z ? 0x80 : 0) |
                    (ll << 5) |
                    (bcast ? 0x10 : 0) |
                    (vp ? 8 : 0) |
                    aaa));
        }

        private void WriteCompactInst(Operand operand, int opCode)
        {
            int regIndex = operand.GetRegister().Index;

            if (regIndex >= 8)
            {
                WriteByte(0x41);
            }

            WriteByte((byte)(opCode + (regIndex & 0b111)));
        }

        private static int GetRexPrefix(Operand dest, Operand source, OperandType type, bool rrm)
        {
            int rexPrefix = 0;

            if (Is64Bits(type))
            {
                rexPrefix = RexWPrefix;
            }

            void SetRegisterHighBit(Register reg, int bit)
            {
                if (reg.Index >= 8)
                {
                    rexPrefix |= RexPrefix | (reg.Index >> 3) << bit;
                }
            }

            if (dest != default && dest.Kind == OperandKind.Register)
            {
                SetRegisterHighBit(dest.GetRegister(), rrm ? 2 : 0);
            }

            if (source != default && source.Kind == OperandKind.Register)
            {
                SetRegisterHighBit(source.GetRegister(), rrm ? 0 : 2);
            }

            return rexPrefix;
        }

        public (byte[], RelocInfo) GetCode()
        {
            Span<Jump> jumps = CollectionsMarshal.AsSpan(_jumps);
            Span<Reloc> relocs = CollectionsMarshal.AsSpan(_relocs);

            // Write jump relative offsets.
            bool modified;

            do
            {
                modified = false;

                for (int i = 0; i < jumps.Length; i++)
                {
                    ref Jump jump = ref jumps[i];

                    // If jump target not resolved yet, resolve it.
                    jump.JumpTarget ??= _labels[jump.JumpLabel];

                    long jumpTarget = jump.JumpTarget.Value;
                    long offset = jumpTarget - jump.JumpPosition;

                    if (offset < 0)
                    {
                        for (int j = i - 1; j >= 0; j--)
                        {
                            ref Jump jump2 = ref jumps[j];

                            if (jump2.JumpPosition < jumpTarget)
                            {
                                break;
                            }

                            offset -= jump2.InstSize - ReservedBytesForJump;
                        }
                    }
                    else
                    {
                        for (int j = i + 1; j < jumps.Length; j++)
                        {
                            ref Jump jump2 = ref jumps[j];

                            if (jump2.JumpPosition >= jumpTarget)
                            {
                                break;
                            }

                            offset += jump2.InstSize - ReservedBytesForJump;
                        }

                        offset -= ReservedBytesForJump;
                    }

                    if (jump.IsConditional)
                    {
                        jump.InstSize = GetJccLength(offset);
                    }
                    else
                    {
                        jump.InstSize = GetJmpLength(offset);
                    }

                    // The jump is relative to the next instruction, not the current one.
                    // Since we didn't know the next instruction address when calculating
                    // the offset (as the size of the current jump instruction was not known),
                    // we now need to compensate the offset with the jump instruction size.
                    // It's also worth noting that:
                    // - This is only needed for backward jumps.
                    // - The GetJmpLength and GetJccLength also compensates the offset
                    // internally when computing the jump instruction size.
                    if (offset < 0)
                    {
                        offset -= jump.InstSize;
                    }

                    if (jump.Offset != offset)
                    {
                        jump.Offset = offset;

                        modified = true;
                    }
                }
            }
            while (modified);

            // Write the code, ignoring the dummy bytes after jumps, into a new stream.
            _stream.Seek(0, SeekOrigin.Begin);

            using RecyclableMemoryStream codeStream = MemoryStreamManager.Shared.GetStream();
            Assembler assembler = new(codeStream, HasRelocs);

            bool hasRelocs = HasRelocs;
            int relocIndex = 0;
            int relocOffset = 0;
            RelocEntry[] relocEntries = hasRelocs
                ? new RelocEntry[relocs.Length]
                : [];

            for (int i = 0; i < jumps.Length; i++)
            {
                ref Jump jump = ref jumps[i];

                // If has relocations, calculate their new positions compensating for jumps.
                if (hasRelocs)
                {
                    relocOffset += jump.InstSize - ReservedBytesForJump;

                    for (; relocIndex < relocEntries.Length; relocIndex++)
                    {
                        ref Reloc reloc = ref relocs[relocIndex];

                        if (reloc.JumpIndex > i)
                        {
                            break;
                        }

                        relocEntries[relocIndex] = new RelocEntry(reloc.Position + relocOffset, reloc.Symbol);
                    }
                }

                Span<byte> buffer = new byte[jump.JumpPosition - _stream.Position];

                _stream.ReadExactly(buffer);
                _stream.Seek(ReservedBytesForJump, SeekOrigin.Current);

                codeStream.Write(buffer);

                if (jump.IsConditional)
                {
                    assembler.Jcc(jump.Condition, jump.Offset);
                }
                else
                {
                    assembler.Jmp(jump.Offset);
                }
            }

            // Write remaining relocations. This case happens when there are no jumps assembled.
            for (; relocIndex < relocEntries.Length; relocIndex++)
            {
                ref Reloc reloc = ref relocs[relocIndex];

                relocEntries[relocIndex] = new RelocEntry(reloc.Position + relocOffset, reloc.Symbol);
            }

            _stream.CopyTo(codeStream);

            byte[] code = codeStream.ToArray();
            RelocInfo relocInfo = new(relocEntries);

            return (code, relocInfo);
        }

        private static bool Is64Bits(OperandType type)
        {
            return type == OperandType.I64 || type == OperandType.FP64;
        }

        private static bool IsImm8(ulong immediate, OperandType type)
        {
            long value = type == OperandType.I32 ? (int)immediate : (long)immediate;

            return ConstFitsOnS8(value);
        }

        private static bool IsImm32(ulong immediate, OperandType type)
        {
            long value = type == OperandType.I32 ? (int)immediate : (long)immediate;

            return ConstFitsOnS32(value);
        }

        private static int GetJccLength(long offset)
        {
            if (ConstFitsOnS8(offset < 0 ? offset - 2 : offset))
            {
                return 2;
            }
            else if (ConstFitsOnS32(offset < 0 ? offset - 6 : offset))
            {
                return 6;
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }
        }

        private static int GetJmpLength(long offset)
        {
            if (ConstFitsOnS8(offset < 0 ? offset - 2 : offset))
            {
                return 2;
            }
            else if (ConstFitsOnS32(offset < 0 ? offset - 5 : offset))
            {
                return 5;
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }
        }

        private static bool ConstFitsOnS8(long value)
        {
            return value == (sbyte)value;
        }

        private static bool ConstFitsOnS32(long value)
        {
            return value == (int)value;
        }

        private void WriteInt16(short value)
        {
            WriteUInt16((ushort)value);
        }

        private void WriteInt32(int value)
        {
            WriteUInt32((uint)value);
        }

        private void WriteByte(byte value)
        {
            _stream.WriteByte(value);
        }

        private void WriteUInt16(ushort value)
        {
            _stream.WriteByte((byte)(value >> 0));
            _stream.WriteByte((byte)(value >> 8));
        }

        private void WriteUInt32(uint value)
        {
            _stream.WriteByte((byte)(value >> 0));
            _stream.WriteByte((byte)(value >> 8));
            _stream.WriteByte((byte)(value >> 16));
            _stream.WriteByte((byte)(value >> 24));
        }

        private void WriteUInt64(ulong value)
        {
            _stream.WriteByte((byte)(value >> 0));
            _stream.WriteByte((byte)(value >> 8));
            _stream.WriteByte((byte)(value >> 16));
            _stream.WriteByte((byte)(value >> 24));
            _stream.WriteByte((byte)(value >> 32));
            _stream.WriteByte((byte)(value >> 40));
            _stream.WriteByte((byte)(value >> 48));
            _stream.WriteByte((byte)(value >> 56));
        }
    }
}
