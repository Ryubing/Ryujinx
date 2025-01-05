using Ryujinx.Graphics.Shader.IntermediateRepresentation;
using Ryujinx.Graphics.Shader.StructuredIr;
using System.Collections.Generic;

using static Ryujinx.Graphics.Shader.IntermediateRepresentation.OperandHelper;

namespace Ryujinx.Graphics.Shader.Translation.Transforms
{
    class VectorComponentSelect : ITransformPass
    {
        public static bool IsEnabled(IGpuAccessor gpuAccessor, ShaderStage stage, TargetLanguage targetLanguage, FeatureFlags usedFeatures)
        {
            return gpuAccessor.QueryHostHasVectorIndexingBug();
        }

        public static LinkedListNode<INode> RunPass(TransformContext context, LinkedListNode<INode> node)
        {
            Operation operation = (Operation)node.Value;

            if (operation.Inst != Instruction.Load ||
                operation.StorageKind != StorageKind.ConstantBuffer ||
                operation.SourcesCount < 3)
            {
                return node;
            }

            Operand bindingIndex = operation.GetSource(0);
            Operand fieldIndex = operation.GetSource(1);
            Operand elemIndex = operation.GetSource(operation.SourcesCount - 1);

            if (bindingIndex.Type != OperandType.Constant ||
                fieldIndex.Type != OperandType.Constant ||
                elemIndex.Type == OperandType.Constant)
            {
                return node;
            }

            BufferDefinition buffer = context.ResourceManager.Properties.ConstantBuffers[bindingIndex.Value];
            StructureField field = buffer.Type.Fields[fieldIndex.Value];

            int elemCount = (field.Type & AggregateType.ElementCountMask) switch
            {
                AggregateType.Vector2 => 2,
                AggregateType.Vector3 => 3,
                AggregateType.Vector4 => 4,
                _ => 1
            };

            if (elemCount == 1)
            {
                return node;
            }

            Operand result = null;

            for (int i = 0; i < elemCount; i++)
            {
                Operand value = Local();
                Operand[] inputs = new Operand[operation.SourcesCount];

                for (int srcIndex = 0; srcIndex < inputs.Length - 1; srcIndex++)
                {
                    inputs[srcIndex] = operation.GetSource(srcIndex);
                }

                inputs[^1] = Const(i);

                Operation loadOp = new(Instruction.Load, StorageKind.ConstantBuffer, value, inputs);

                node.List.AddBefore(node, loadOp);

                if (i == 0)
                {
                    result = value;
                }
                else
                {
                    Operand isCurrentIndex = Local();
                    Operand selection = Local();

                    Operation compareOp = new(Instruction.CompareEqual, isCurrentIndex, [elemIndex, Const(i)]);
                    Operation selectOp = new(Instruction.ConditionalSelect, selection, [isCurrentIndex, value, result]);

                    node.List.AddBefore(node, compareOp);
                    node.List.AddBefore(node, selectOp);

                    result = selection;
                }
            }

            operation.TurnIntoCopy(result);

            return node;
        }
    }
}
