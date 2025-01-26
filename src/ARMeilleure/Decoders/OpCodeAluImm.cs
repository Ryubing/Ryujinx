using System;

namespace ARMeilleure.Decoders
{
    class OpCodeAluImm : OpCodeAlu, IOpCodeAluImm
    {
        public long Immediate { get; }

        public new static OpCode Create(InstDescriptor inst, ulong address, int opCode) => new OpCodeAluImm(inst, address, opCode);

        public OpCodeAluImm(InstDescriptor inst, ulong address, int opCode) : base(inst, address, opCode)
        {
            if (DataOp == DataOp.Arithmetic)
            {
                Immediate = (opCode >> 10) & 0xfff;

                int shift = (opCode >> 22) & 3;

                Immediate <<= shift * 12;
            }
            else if (DataOp == DataOp.Logical)
            {
                DecoderHelper.BitMask bm = DecoderHelper.DecodeBitMask(opCode, true);

                if (bm.IsUndefined)
                {
                    Instruction = InstDescriptor.Undefined;

                    return;
                }

                Immediate = bm.WMask;
            }
            else
            {
                throw new ArgumentException($"Invalid data operation: {DataOp}", nameof(opCode));
            }
        }
    }
}
