using Ryujinx.HLE.HOS.Tamper.Operations;
using System.Collections.Generic;

namespace Ryujinx.HLE.HOS.Tamper
{
    class CompilationContext
    {
        public OperationBlock CurrentBlock => BlockStack.Peek();
        public List<IOperation> CurrentOperations => CurrentBlock.Operations;

        public ITamperedProcess Process { get; }
        public Parameter<long> PressedKeys { get; }
        public Stack<OperationBlock> BlockStack { get; }
        public Dictionary<byte, Register> Registers { get; }
        public Dictionary<byte, Register> SavedRegisters { get; }
        public Dictionary<byte, Register> StaticRegisters { get; }
        public ulong ExeAddress { get; }
        public ulong HeapAddress { get; }
        public ulong AliasAddress { get; }
        public ulong AslrAddress { get; }

        public CompilationContext(ulong exeAddress, ulong heapAddress, ulong aliasAddress, ulong aslrAddress, ITamperedProcess process)
        {
            Process = process;
            PressedKeys = new Parameter<long>(0);
            BlockStack = new Stack<OperationBlock>();
            Registers = [];
            SavedRegisters = [];
            StaticRegisters = [];
            ExeAddress = exeAddress;
            HeapAddress = heapAddress;
            AliasAddress = aliasAddress;
            AslrAddress = aslrAddress;
        }

        public Register GetRegister(byte index)
        {
            if (Registers.TryGetValue(index, out Register register))
            {
                return register;
            }

            register = new Register($"R_{index:X2}");
            Registers.Add(index, register);

            return register;
        }

        public Register GetSavedRegister(byte index)
        {
            if (SavedRegisters.TryGetValue(index, out Register register))
            {
                return register;
            }

            register = new Register($"S_{index:X2}");
            SavedRegisters.Add(index, register);

            return register;
        }

        public Register GetStaticRegister(byte index)
        {
            if (SavedRegisters.TryGetValue(index, out Register register))
            {
                return register;
            }

            register = new Register($"T_{index:X2}");
            SavedRegisters.Add(index, register);

            return register;
        }
    }
}
