using Ryujinx.Graphics.Shader.IntermediateRepresentation;
using Ryujinx.Graphics.Shader.Translation.Optimizations;
using System.Collections.Generic;
using static Ryujinx.Graphics.Shader.IntermediateRepresentation.OperandHelper;

namespace Ryujinx.Graphics.Shader.Translation.Transforms
{
    class ShufflePass : ITransformPass
    {
        public static bool IsEnabled(IGpuAccessor gpuAccessor, ShaderStage stage, TargetLanguage targetLanguage, FeatureFlags usedFeatures)
        {
            return usedFeatures.HasFlag(FeatureFlags.Shuffle);
        }

        public static LinkedListNode<INode> RunPass(TransformContext context, LinkedListNode<INode> node)
        {
            Operation operation = (Operation)node.Value;

            HelperFunctionName functionName = operation.Inst switch
            {
                Instruction.Shuffle => HelperFunctionName.Shuffle,
                Instruction.ShuffleDown => HelperFunctionName.ShuffleDown,
                Instruction.ShuffleUp => HelperFunctionName.ShuffleUp,
                Instruction.ShuffleXor => HelperFunctionName.ShuffleXor,
                _ => HelperFunctionName.Invalid,
            };

            if (functionName == HelperFunctionName.Invalid || operation.SourcesCount != 3 || operation.DestsCount != 2)
            {
                return node;
            }

            int functionId = context.Hfm.GetOrCreateShuffleFunctionId(functionName, context.GpuAccessor.QueryHostSubgroupSize());

            Operand result = operation.GetDest(0);
            Operand valid = operation.GetDest(1);
            Operand value = operation.GetSource(0);
            Operand index = operation.GetSource(1);
            Operand mask = operation.GetSource(2);

            operation.Dest = null;

            Operand[] callArgs = [Const(functionId), value, index, mask, valid];

            LinkedListNode<INode> newNode = node.List.AddBefore(node, new Operation(Instruction.Call, 0, result, callArgs));

            Utils.DeleteNode(node, operation);

            return newNode;
        }
    }
}
