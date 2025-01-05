using ARMeilleure.IntermediateRepresentation;
using System.Collections.Generic;

using static ARMeilleure.IntermediateRepresentation.Operand.Factory;

namespace ARMeilleure.Translation
{
    static class RegisterToLocal
    {
        public static void Rename(ControlFlowGraph cfg)
        {
            Dictionary<Register, Operand> registerToLocalMap = [];

            Operand GetLocal(Operand op)
            {
                Register register = op.GetRegister();

                if (!registerToLocalMap.TryGetValue(register, out Operand local))
                {
                    local = Local(op.Type);

                    registerToLocalMap.Add(register, local);
                }

                return local;
            }

            for (BasicBlock block = cfg.Blocks.First; block != null; block = block.ListNext)
            {
                for (Operation node = block.Operations.First; node != default; node = node.ListNext)
                {
                    Operand dest = node.Destination;

                    if (dest != default && dest.Kind == OperandKind.Register)
                    {
                        node.Destination = GetLocal(dest);
                    }

                    for (int index = 0; index < node.SourcesCount; index++)
                    {
                        Operand source = node.GetSource(index);

                        if (source.Kind == OperandKind.Register)
                        {
                            node.SetSource(index, GetLocal(source));
                        }
                    }
                }
            }
        }
    }
}
