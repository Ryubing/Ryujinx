namespace ARMeilleure.Decoders
{
    class OpCode32SimdMovGpElem : OpCode32, IOpCode32Simd
    {
        public int Size { get; }

        public int Vd { get; }
        public int Rt { get; }
        public int Op { get; }
        public bool U { get; }

        public int Index { get; }

        public new static OpCode Create(InstDescriptor inst, ulong address, int opCode) => new OpCode32SimdMovGpElem(inst, address, opCode, false);
        public static OpCode CreateT32(InstDescriptor inst, ulong address, int opCode) => new OpCode32SimdMovGpElem(inst, address, opCode, true);

        public OpCode32SimdMovGpElem(InstDescriptor inst, ulong address, int opCode, bool isThumb) : base(inst, address, opCode)
        {
            IsThumb = isThumb;

            Op = (opCode >> 20) & 0x1;
            U = ((opCode >> 23) & 1) != 0;

            int opc = (((opCode >> 23) & 1) << 4) | (((opCode >> 21) & 0x3) << 2) | ((opCode >> 5) & 0x3);

            if ((opc & 0b01000) == 0b01000)
            {
                Size = 0;
                Index = opc & 0x7;
            }
            else if ((opc & 0b01001) == 0b00001)
            {
                Size = 1;
                Index = (opc >> 1) & 0x3;
            }
            else if ((opc & 0b11011) == 0)
            {
                Size = 2;
                Index = (opc >> 2) & 0x1;
            }
            else
            {
                Instruction = InstDescriptor.Undefined;
                return;
            }

            Vd = ((opCode >> 3) & 0x10) | ((opCode >> 16) & 0xf);
            Rt = (opCode >> 12) & 0xf;
        }
    }
}
