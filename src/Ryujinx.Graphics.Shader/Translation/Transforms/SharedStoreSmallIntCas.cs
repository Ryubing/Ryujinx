using Ryujinx.Graphics.Shader.IntermediateRepresentation;
using Ryujinx.Graphics.Shader.Translation.Optimizations;
using System.Collections.Generic;
using System.Diagnostics;

using static Ryujinx.Graphics.Shader.IntermediateRepresentation.OperandHelper;

namespace Ryujinx.Graphics.Shader.Translation.Transforms
{
    class SharedStoreSmallIntCas : ITransformPass
    {
        public static bool IsEnabled(IGpuAccessor gpuAccessor, ShaderStage stage, TargetLanguage targetLanguage, FeatureFlags usedFeatures)
        {
            return stage == ShaderStage.Compute && usedFeatures.HasFlag(FeatureFlags.SharedMemory);
        }

        public static LinkedListNode<INode> RunPass(TransformContext context, LinkedListNode<INode> node)
        {
            Operation operation = (Operation)node.Value;
            HelperFunctionName name;

            if (operation.StorageKind == StorageKind.SharedMemory8)
            {
                name = HelperFunctionName.SharedStore8;
            }
            else if (operation.StorageKind == StorageKind.SharedMemory16)
            {
                name = HelperFunctionName.SharedStore16;
            }
            else
            {
                return node;
            }

            if (operation.Inst != Instruction.Store)
            {
                return node;
            }

            Operand memoryId = operation.GetSource(0);
            Operand byteOffset = operation.GetSource(1);
            Operand value = operation.GetSource(2);

            Debug.Assert(memoryId.Type == OperandType.Constant);

            int functionId = context.Hfm.GetOrCreateFunctionId(name, memoryId.Value);

            Operand[] callArgs = [Const(functionId), byteOffset, value];

            LinkedListNode<INode> newNode = node.List.AddBefore(node, new Operation(Instruction.Call, 0, (Operand)null, callArgs));

            Utils.DeleteNode(node, operation);

            return newNode;
        }
    }
}
