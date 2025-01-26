using Ryujinx.Graphics.Shader.Translation;
using System.Collections.Generic;

namespace Ryujinx.Graphics.Shader.StructuredIr
{
    class StructuredFunction
    {
        public AstBlock MainBlock { get; }

        public string Name { get; }

        public AggregateType ReturnType { get; }

        public AggregateType[] InArguments { get; }
        public AggregateType[] OutArguments { get; }

        public HashSet<AstOperand> Locals { get; }

        public StructuredFunction(
            AstBlock mainBlock,
            string name,
            AggregateType returnType,
            AggregateType[] inArguments,
            AggregateType[] outArguments)
        {
            MainBlock = mainBlock;
            Name = name;
            ReturnType = returnType;
            InArguments = inArguments;
            OutArguments = outArguments;

            Locals = [];
        }

        public AggregateType GetArgumentType(int index)
        {
            return index >= InArguments.Length
                ? OutArguments[index - InArguments.Length]
                : InArguments[index];
        }
    }
}
