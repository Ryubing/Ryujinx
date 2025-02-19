using System.Collections.Generic;

namespace ARMeilleure.Decoders
{
    class OpCodeT16IfThen : OpCodeT16
    {
        public Condition[] IfThenBlockConds { get; }

        public int IfThenBlockSize { get { return IfThenBlockConds.Length; } }

        public new static OpCode Create(InstDescriptor inst, ulong address, int opCode) => new OpCodeT16IfThen(inst, address, opCode);

        public OpCodeT16IfThen(InstDescriptor inst, ulong address, int opCode) : base(inst, address, opCode)
        {
            List<Condition> conds = [];

            int cond = (opCode >> 4) & 0xf;
            int mask = opCode & 0xf;

            conds.Add((Condition)cond);

            while ((mask & 7) != 0)
            {
                int newLsb = (mask >> 3) & 1;
                cond = (cond & 0xe) | newLsb;
                mask <<= 1;
                conds.Add((Condition)cond);
            }

            IfThenBlockConds = conds.ToArray();
        }
    }
}
