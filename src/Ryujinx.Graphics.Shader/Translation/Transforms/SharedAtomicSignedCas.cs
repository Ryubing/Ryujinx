using Ryujinx.Graphics.Shader.IntermediateRepresentation;
using Ryujinx.Graphics.Shader.Translation.Optimizations;
using System.Collections.Generic;
using System.Diagnostics;

using static Ryujinx.Graphics.Shader.IntermediateRepresentation.OperandHelper;

namespace Ryujinx.Graphics.Shader.Translation.Transforms
{
    class SharedAtomicSignedCas : ITransformPass
    {
        public static bool IsEnabled(IGpuAccessor gpuAccessor, ShaderStage stage, TargetLanguage targetLanguage, FeatureFlags usedFeatures)
        {
            return targetLanguage != TargetLanguage.Spirv && stage == ShaderStage.Compute && usedFeatures.HasFlag(FeatureFlags.SharedMemory);
        }

        public static LinkedListNode<INode> RunPass(TransformContext context, LinkedListNode<INode> node)
        {
            Operation operation = (Operation)node.Value;
            HelperFunctionName name;

            if (operation.Inst == Instruction.AtomicMaxS32)
            {
                name = HelperFunctionName.SharedAtomicMaxS32;
            }
            else if (operation.Inst == Instruction.AtomicMinS32)
            {
                name = HelperFunctionName.SharedAtomicMinS32;
            }
            else
            {
                return node;
            }

            if (operation.StorageKind != StorageKind.SharedMemory)
            {
                return node;
            }

            Operand result = operation.Dest;
            Operand memoryId = operation.GetSource(0);
            Operand byteOffset = operation.GetSource(1);
            Operand value = operation.GetSource(2);

            Debug.Assert(memoryId.Type == OperandType.Constant);

            int functionId = context.Hfm.GetOrCreateFunctionId(name, memoryId.Value);

            Operand[] callArgs = [Const(functionId), byteOffset, value];

            LinkedListNode<INode> newNode = node.List.AddBefore(node, new Operation(Instruction.Call, 0, result, callArgs));

            Utils.DeleteNode(node, operation);

            return newNode;
        }
    }
}
