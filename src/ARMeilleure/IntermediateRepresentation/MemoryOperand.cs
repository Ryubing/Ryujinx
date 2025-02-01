using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace ARMeilleure.IntermediateRepresentation
{
    readonly unsafe struct MemoryOperand
    {
        private struct Data
        {
#pragma warning disable CS0649 // Field is never assigned to
            public byte Kind;
            public byte Type;
#pragma warning restore CS0649
            public byte Scale;
            public Operand BaseAddress;
            public Operand Index;
            public int Displacement;
        }

        private readonly Data* _data;

        public MemoryOperand(Operand operand)
        {
            Debug.Assert(operand.Kind == OperandKind.Memory);

            _data = (Data*)Unsafe.As<Operand, nint>(ref operand);
        }

        public Operand BaseAddress
        {
            get => _data->BaseAddress;
            set => _data->BaseAddress = value;
        }

        public Operand Index
        {
            get => _data->Index;
            set => _data->Index = value;
        }

        public Multiplier Scale
        {
            get => (Multiplier)_data->Scale;
            set => _data->Scale = (byte)value;
        }

        public int Displacement
        {
            get => _data->Displacement;
            set => _data->Displacement = value;
        }
    }
}
