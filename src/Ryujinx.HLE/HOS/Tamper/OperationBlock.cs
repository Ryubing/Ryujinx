using Ryujinx.HLE.HOS.Tamper.Operations;
using System.Collections.Generic;

namespace Ryujinx.HLE.HOS.Tamper
{
    readonly struct OperationBlock
    {
        public byte[] BaseInstruction { get; }
        public List<IOperation> Operations { get; }

        public OperationBlock(byte[] baseInstruction)
        {
            BaseInstruction = baseInstruction;
            Operations = [];
        }
    }
}
