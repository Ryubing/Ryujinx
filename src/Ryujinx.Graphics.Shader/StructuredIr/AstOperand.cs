using Ryujinx.Graphics.Shader.IntermediateRepresentation;
using Ryujinx.Graphics.Shader.Translation;
using System.Collections.Generic;

namespace Ryujinx.Graphics.Shader.StructuredIr
{
    class AstOperand : AstNode
    {
        public HashSet<IAstNode> Defs { get; }
        public HashSet<IAstNode> Uses { get; }

        public OperandType Type { get; }

        public AggregateType VarType { get; set; }

        public int Value { get; }

        private AstOperand()
        {
            Defs = [];
            Uses = [];

            VarType = AggregateType.S32;
        }

        public AstOperand(Operand operand) : this()
        {
            Type = operand.Type;
            Value = operand.Value;
        }

        public AstOperand(OperandType type, int value = 0) : this()
        {
            Type = type;
            Value = value;
        }
    }
}
