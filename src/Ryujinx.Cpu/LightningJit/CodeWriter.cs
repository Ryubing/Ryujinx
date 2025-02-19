using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Ryujinx.Cpu.LightningJit
{
    class CodeWriter
    {
        private readonly List<uint> _instructions;

        public int InstructionPointer => _instructions.Count;

        public CodeWriter()
        {
            _instructions = [];
        }

        public void WriteInstruction(uint instruction)
        {
            _instructions.Add(instruction);
        }

        public void WriteInstructionAt(int index, uint instruction)
        {
            _instructions[index] = instruction;
        }

        public void WriteInstructionsAt(int index, CodeWriter writer)
        {
            _instructions.InsertRange(index, writer._instructions);
        }

        public uint ReadInstructionAt(int index)
        {
            return _instructions[index];
        }

        public List<uint> GetList()
        {
            return _instructions;
        }

        public void RemoveLastInstruction()
        {
            if (_instructions.Count > 0)
            {
                _instructions.RemoveAt(_instructions.Count - 1);
            }
        }

        public ReadOnlySpan<uint> AsSpan()
        {
            return CollectionsMarshal.AsSpan(_instructions);
        }

        public ReadOnlySpan<byte> AsByteSpan()
        {
            return MemoryMarshal.Cast<uint, byte>(AsSpan());
        }
    }
}
