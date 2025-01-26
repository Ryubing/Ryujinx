namespace ARMeilleure.Decoders
{
    class OpCode32SimdDupElem : OpCode32Simd
    {
        public int Index { get; }

        public new static OpCode Create(InstDescriptor inst, ulong address, int opCode) => new OpCode32SimdDupElem(inst, address, opCode, false);
        public new static OpCode CreateT32(InstDescriptor inst, ulong address, int opCode) => new OpCode32SimdDupElem(inst, address, opCode, true);

        public OpCode32SimdDupElem(InstDescriptor inst, ulong address, int opCode, bool isThumb) : base(inst, address, opCode, isThumb)
        {
            int opc = (opCode >> 16) & 0xf;

            if ((opc & 0b1) == 1)
            {
                Size = 0;
                Index = (opc >> 1) & 0x7;
            }
            else if ((opc & 0b11) == 0b10)
            {
                Size = 1;
                Index = (opc >> 2) & 0x3;
            }
            else if ((opc & 0b111) == 0b100)
            {
                Size = 2;
                Index = (opc >> 3) & 0x1;
            }
            else
            {
                Instruction = InstDescriptor.Undefined;
            }

            Vd = ((opCode >> 18) & 0x10) | ((opCode >> 12) & 0xf);
            Vm = ((opCode >> 1) & 0x10) | ((opCode >> 0) & 0xf);

            if (DecoderHelper.VectorArgumentsInvalid(Q, Vd))
            {
                Instruction = InstDescriptor.Undefined;
            }
        }
    }
}
