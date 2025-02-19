using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Ryujinx.Cpu.LightningJit.CodeGen.Arm64
{
    class TailMerger
    {
        private enum BranchType
        {
            Conditional,
            Unconditional,
        }

        private readonly List<(BranchType, int)> _branchPointers;

        public TailMerger()
        {
            _branchPointers = [];
        }

        public void AddConditionalReturn(CodeWriter writer, in Assembler asm, ArmCondition returnCondition)
        {
            _branchPointers.Add((BranchType.Conditional, writer.InstructionPointer));
            asm.B(returnCondition, 0);
        }

        public void AddConditionalZeroReturn(CodeWriter writer, in Assembler asm, Operand value)
        {
            _branchPointers.Add((BranchType.Conditional, writer.InstructionPointer));
            asm.Cbz(value, 0);
        }

        public void AddUnconditionalReturn(CodeWriter writer, in Assembler asm)
        {
            _branchPointers.Add((BranchType.Unconditional, writer.InstructionPointer));
            asm.B(0);
        }

        public void WriteReturn(CodeWriter writer, Action writeEpilogue)
        {
            if (_branchPointers.Count == 0)
            {
                return;
            }

            int targetIndex = writer.InstructionPointer;
            int startIndex = _branchPointers.Count - 1;

            if (startIndex >= 0 &&
                _branchPointers[startIndex].Item1 == BranchType.Unconditional &&
                _branchPointers[startIndex].Item2 == targetIndex - 1)
            {
                // Remove the last branch if it is redundant.
                writer.RemoveLastInstruction();
                startIndex--;
                targetIndex--;
            }

            Assembler asm = new(writer);

            writeEpilogue();
            asm.Ret();

            for (int i = startIndex; i >= 0; i--)
            {
                (BranchType type, int branchIndex) = _branchPointers[i];

                uint encoding = writer.ReadInstructionAt(branchIndex);
                int delta = targetIndex - branchIndex;

                if (type == BranchType.Conditional)
                {
                    uint branchMask = 0x7ffff;
                    int branchMax = (int)(branchMask + 1) / 2;

                    if (delta >= -branchMax && delta < branchMax)
                    {
                        writer.WriteInstructionAt(branchIndex, (encoding & ~(branchMask << 5)) | (uint)((delta & branchMask) << 5));
                    }
                    else
                    {
                        // If the branch target is too far away, we use a regular unconditional branch
                        // instruction instead which has a much higher range.
                        // We branch directly to the end of the function, where we put the conditional branch,
                        // and then branch back to the next instruction or return the branch target depending
                        // on the branch being taken or not.

                        delta = writer.InstructionPointer - branchIndex;

                        uint branchInst = 0x14000000u | ((uint)delta & 0x3ffffff);
                        Debug.Assert(ExtractSImm26Times4(branchInst) == delta * 4);

                        writer.WriteInstructionAt(branchIndex, branchInst);

                        int movedBranchIndex = writer.InstructionPointer;

                        writer.WriteInstruction(0u); // Placeholder
                        asm.B((branchIndex + 1 - writer.InstructionPointer) * 4);

                        delta = targetIndex - movedBranchIndex;

                        writer.WriteInstructionAt(movedBranchIndex, (encoding & ~(branchMask << 5)) | (uint)((delta & branchMask) << 5));
                    }
                }
                else
                {
                    Debug.Assert(type == BranchType.Unconditional);

                    writer.WriteInstructionAt(branchIndex, (encoding & ~0x3ffffffu) | (uint)(delta & 0x3ffffff));
                }
            }
        }

        private static int ExtractSImm26Times4(uint encoding)
        {
            return (int)(encoding << 6) >> 4;
        }
    }
}
