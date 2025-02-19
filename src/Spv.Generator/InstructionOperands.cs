using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace Spv.Generator
{
    public struct InstructionOperands
    {
        private const int InternalCount = 5;

        public int Count;
        public IOperand Operand1;
        public IOperand Operand2;
        public IOperand Operand3;
        public IOperand Operand4;
        public IOperand Operand5;
        public IOperand[] Overflow;

        public Span<IOperand> AsSpan()
        {
            if (Count > InternalCount)
            {
                return MemoryMarshal.CreateSpan(ref this.Overflow[0], Count);
            }
            else
            {
                return MemoryMarshal.CreateSpan(ref this.Operand1, Count);
            }
        }

        public void Add(IOperand operand)
        {
            if (Count < InternalCount)
            {
                MemoryMarshal.CreateSpan(ref this.Operand1, Count + 1)[Count] = operand;
                Count++;
            }
            else
            {
                if (Overflow == null)
                {
                    Overflow = new IOperand[InternalCount * 2];
                    MemoryMarshal.CreateSpan(ref this.Operand1, InternalCount).CopyTo(Overflow.AsSpan());
                }
                else if (Count == Overflow.Length)
                {
                    Array.Resize(ref Overflow, Overflow.Length * 2);
                }

                Overflow[Count++] = operand;
            }
        }

        private readonly IEnumerable<IOperand> AllOperands => new[] { Operand1, Operand2, Operand3, Operand4, Operand5 }
            .Concat(Overflow ?? [])
            .Take(Count);

        public readonly override string ToString()
        {
            return $"({string.Join(", ", AllOperands)})";
        }

        public readonly string ToString(string[] labels)
        {
            IEnumerable<string> labeledParams = AllOperands.Zip(labels, (op, label) => $"{label}: {op}");
            IEnumerable<string> unlabeledParams = AllOperands.Skip(labels.Length).Select(op => op.ToString());
            IEnumerable<string> paramsToPrint = labeledParams.Concat(unlabeledParams);
            return $"({string.Join(", ", paramsToPrint)})";
        }
    }
}
