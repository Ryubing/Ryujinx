using Ryujinx.Graphics.Shader.IntermediateRepresentation;
using Ryujinx.Graphics.Shader.StructuredIr;
using System.Diagnostics;

using static Ryujinx.Graphics.Shader.CodeGen.Glsl.Instructions.InstGenHelper;

namespace Ryujinx.Graphics.Shader.CodeGen.Glsl.Instructions
{
    static class InstGenCall
    {
        public static string Call(CodeGenContext context, AstOperation operation)
        {
            AstOperand funcId = (AstOperand)operation.GetSource(0);

            Debug.Assert(funcId.Type == OperandType.Constant);

            StructuredFunction function = context.GetFunction(funcId.Value);

            string[] args = new string[operation.SourcesCount - 1];

            for (int i = 0; i < args.Length; i++)
            {
                args[i] = GetSourceExpr(context, operation.GetSource(i + 1), function.GetArgumentType(i));
            }

            return $"{function.Name}({string.Join(", ", args)})";
        }
    }
}
