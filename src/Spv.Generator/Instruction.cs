using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Spv.Generator
{
    public sealed class Instruction : IOperand, IEquatable<Instruction>
    {
        public const uint InvalidId = uint.MaxValue;

        public Specification.Op Opcode { get; private set; }
        private Instruction _resultType;
        private InstructionOperands _operands;

        public uint Id { get; set; }

        public Instruction() { }

        public void Set(Specification.Op opcode, uint id = InvalidId, Instruction resultType = null)
        {
            Opcode = opcode;
            Id = id;
            _resultType = resultType;

            _operands = new InstructionOperands();
        }

        public void SetId(uint id)
        {
            Id = id;
        }

        public OperandType Type => OperandType.Instruction;

        public ushort GetTotalWordCount()
        {
            ushort result = WordCount;

            if (Id != InvalidId)
            {
                result++;
            }

            if (_resultType != null)
            {
                result += _resultType.WordCount;
            }

            Span<IOperand> operands = _operands.AsSpan();
            for (int i = 0; i < operands.Length; i++)
            {
                result += operands[i].WordCount;
            }

            return result;
        }

        public ushort WordCount => 1;

        public void AddOperand(IOperand value)
        {
            Debug.Assert(value != null);
            _operands.Add(value);
        }

        public void AddOperand(ReadOnlySpan<IOperand> value)
        {
            foreach (IOperand instruction in value)
            {
                AddOperand(instruction);
            }
        }

        public void AddOperand(ReadOnlySpan<LiteralInteger> value)
        {
            foreach (LiteralInteger instruction in value)
            {
                AddOperand(instruction);
            }
        }

        public void AddOperand(LiteralInteger value)
        {
            AddOperand((IOperand)value);
        }

        public void AddOperand(ReadOnlySpan<Instruction> value)
        {
            foreach (Instruction instruction in value)
            {
                AddOperand(instruction);
            }
        }

        public void AddOperand(Instruction value)
        {
            AddOperand((IOperand)value);
        }

        public void AddOperand(string value)
        {
            AddOperand(new LiteralString(value));
        }

        public void AddOperand<T>(T value) where T : Enum
        {
            AddOperand(LiteralInteger.CreateForEnum(value));
        }

        public void Write(BinaryWriter writer)
        {
            // Word 0
            writer.Write((ushort)Opcode);
            writer.Write(GetTotalWordCount());

            _resultType?.WriteOperand(writer);

            if (Id != InvalidId)
            {
                writer.Write(Id);
            }

            Span<IOperand> operands = _operands.AsSpan();
            for (int i = 0; i < operands.Length; i++)
            {
                operands[i].WriteOperand(writer);
            }
        }

        public void WriteOperand(BinaryWriter writer)
        {
            Debug.Assert(Id != InvalidId);

            if (Id == InvalidId)
            {
                string methodToCall;

                if (Opcode == Specification.Op.OpVariable)
                {
                    methodToCall = "AddLocalVariable or AddGlobalVariable";
                }
                else if (Opcode == Specification.Op.OpLabel)
                {
                    methodToCall = "AddLabel";
                }
                else
                {
                    throw new InvalidOperationException("Internal error");
                }

                throw new InvalidOperationException($"Id wasn't bound to the module, please make sure to call {methodToCall}");
            }

            writer.Write(Id);
        }

        public override bool Equals(object obj)
        {
            return obj is Instruction instruction && Equals(instruction);
        }

        public bool Equals(Instruction cmpObj)
        {
            bool result = Type == cmpObj.Type && Id == cmpObj.Id;

            if (result)
            {
                if (_resultType != null && cmpObj._resultType != null)
                {
                    result &= _resultType.Equals(cmpObj._resultType);
                }
                else if (_resultType != null || cmpObj._resultType != null)
                {
                    return false;
                }
            }

            if (result)
            {
                result &= EqualsContent(cmpObj);
            }

            return result;
        }

        public bool EqualsContent(Instruction cmpObj)
        {
            Span<IOperand> thisOperands = _operands.AsSpan();
            Span<IOperand> cmpOperands = cmpObj._operands.AsSpan();

            if (thisOperands.Length != cmpOperands.Length)
            {
                return false;
            }

            for (int i = 0; i < thisOperands.Length; i++)
            {
                if (!thisOperands[i].Equals(cmpOperands[i]))
                {
                    return false;
                }
            }

            return true;
        }

        public bool EqualsResultType(Instruction cmpObj)
        {
            return _resultType.Opcode == cmpObj._resultType.Opcode && _resultType.EqualsContent(cmpObj._resultType);
        }

        public int GetHashCodeContent()
        {
            return DeterministicHashCode.Combine<IOperand>(_operands.AsSpan());
        }

        public int GetHashCodeResultType()
        {
            return DeterministicHashCode.Combine(_resultType.Opcode, _resultType.GetHashCodeContent());
        }

        public override int GetHashCode()
        {
            return DeterministicHashCode.Combine(Opcode, Id, _resultType, DeterministicHashCode.Combine<IOperand>(_operands.AsSpan()));
        }

        public bool Equals(IOperand obj)
        {
            return obj is Instruction instruction && Equals(instruction);
        }

        private static readonly Dictionary<Specification.Op, string[]> _operandLabels = new()
        {
            { Specification.Op.OpConstant, new [] { "Value" } },
            { Specification.Op.OpTypeInt, new [] { "Width", "Signed" } },
            { Specification.Op.OpTypeFloat, new [] { "Width" } },
        };

        public override string ToString()
        {
            var labels = _operandLabels.TryGetValue(Opcode, out var opLabels) ? opLabels : Array.Empty<string>();
            var result = _resultType == null ? string.Empty : $"{_resultType} ";
            return $"{result}{Opcode}{_operands.ToString(labels)}";
        }
    }
}
