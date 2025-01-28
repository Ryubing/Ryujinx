using Ryujinx.Graphics.Shader.IntermediateRepresentation;
using Ryujinx.Graphics.Shader.Translation.Optimizations;
using System.Collections.Generic;

using static Ryujinx.Graphics.Shader.IntermediateRepresentation.OperandHelper;

namespace Ryujinx.Graphics.Shader.Translation.Transforms
{
    class VertexToCompute : ITransformPass
    {
        public static bool IsEnabled(IGpuAccessor gpuAccessor, ShaderStage stage, TargetLanguage targetLanguage, FeatureFlags usedFeatures)
        {
            return usedFeatures.HasFlag(FeatureFlags.VtgAsCompute);
        }

        public static LinkedListNode<INode> RunPass(TransformContext context, LinkedListNode<INode> node)
        {
            if (context.Definitions.Stage != ShaderStage.Vertex)
            {
                return node;
            }

            Operation operation = (Operation)node.Value;

            LinkedListNode<INode> newNode = node;

            if (operation.Inst == Instruction.Load && operation.StorageKind == StorageKind.Input)
            {
                Operand dest = operation.Dest;

                switch ((IoVariable)operation.GetSource(0).Value)
                {
                    case IoVariable.BaseInstance:
                        newNode = GenerateBaseInstanceLoad(context.ResourceManager, node, dest);
                        break;
                    case IoVariable.BaseVertex:
                        newNode = GenerateBaseVertexLoad(context.ResourceManager, node, dest);
                        break;
                    case IoVariable.InstanceId:
                        newNode = GenerateInstanceIdLoad(node, dest);
                        break;
                    case IoVariable.InstanceIndex:
                        newNode = GenerateInstanceIndexLoad(context.ResourceManager, node, dest);
                        break;
                    case IoVariable.VertexId:
                    case IoVariable.VertexIndex:
                        newNode = GenerateVertexIndexLoad(context.ResourceManager, node, dest);
                        break;
                    case IoVariable.UserDefined:
                        int location = operation.GetSource(1).Value;
                        int component = operation.GetSource(2).Value;

                        if (context.Definitions.IsAttributePacked(location))
                        {
                            bool needsSextNorm = context.Definitions.IsAttributePackedRgb10A2Signed(location);

                            SetBindingPair setAndBinding = context.ResourceManager.Reservations.GetVertexBufferTextureSetAndBinding(location);
                            Operand temp = needsSextNorm ? Local() : dest;
                            Operand vertexElemOffset = GenerateVertexOffset(context.ResourceManager, node, location, 0);

                            newNode = node.List.AddBefore(node, new TextureOperation(
                                Instruction.TextureSample,
                                SamplerType.TextureBuffer,
                                TextureFormat.Unknown,
                                TextureFlags.IntCoords,
                                setAndBinding.SetIndex,
                                setAndBinding.Binding,
                                1 << component,
                                [temp],
                                [vertexElemOffset]));

                            if (needsSextNorm)
                            {
                                bool sint = context.Definitions.IsAttributeSint(location);
                                CopySignExtendedNormalized(node, component == 3 ? 2 : 10, !sint, dest, temp);
                            }
                        }
                        else
                        {
                            SetBindingPair setAndBinding = context.ResourceManager.Reservations.GetVertexBufferTextureSetAndBinding(location);
                            Operand temp = component > 0 ? Local() : dest;
                            Operand vertexElemOffset = GenerateVertexOffset(context.ResourceManager, node, location, component);

                            newNode = node.List.AddBefore(node, new TextureOperation(
                                Instruction.TextureSample,
                                SamplerType.TextureBuffer,
                                TextureFormat.Unknown,
                                TextureFlags.IntCoords,
                                setAndBinding.SetIndex,
                                setAndBinding.Binding,
                                1,
                                [temp],
                                [vertexElemOffset]));

                            if (component > 0)
                            {
                                newNode = CopyMasked(context.ResourceManager, newNode, location, component, dest, temp);
                            }
                        }
                        break;
                    case IoVariable.GlobalId:
                    case IoVariable.SubgroupEqMask:
                    case IoVariable.SubgroupGeMask:
                    case IoVariable.SubgroupGtMask:
                    case IoVariable.SubgroupLaneId:
                    case IoVariable.SubgroupLeMask:
                    case IoVariable.SubgroupLtMask:
                        // Those are valid or expected for vertex shaders.
                        break;
                    default:
                        context.GpuAccessor.Log($"Invalid input \"{(IoVariable)operation.GetSource(0).Value}\".");
                        break;
                }
            }
            else if (operation.Inst == Instruction.Load && operation.StorageKind == StorageKind.Output)
            {
                if (TryGetOutputOffset(context.ResourceManager, operation, out int outputOffset))
                {
                    newNode = node.List.AddBefore(node, new Operation(
                        Instruction.Load,
                        StorageKind.LocalMemory,
                        operation.Dest,
                        new[] { Const(context.ResourceManager.LocalVertexDataMemoryId), Const(outputOffset) }));
                }
                else
                {
                    context.GpuAccessor.Log($"Invalid output \"{(IoVariable)operation.GetSource(0).Value}\".");
                }
            }
            else if (operation.Inst == Instruction.Store && operation.StorageKind == StorageKind.Output)
            {
                if (TryGetOutputOffset(context.ResourceManager, operation, out int outputOffset))
                {
                    Operand value = operation.GetSource(operation.SourcesCount - 1);

                    newNode = node.List.AddBefore(node, new Operation(
                        Instruction.Store,
                        StorageKind.LocalMemory,
                        (Operand)null,
                        new[] { Const(context.ResourceManager.LocalVertexDataMemoryId), Const(outputOffset), value }));
                }
                else
                {
                    context.GpuAccessor.Log($"Invalid output \"{(IoVariable)operation.GetSource(0).Value}\".");
                }
            }

            if (newNode != node)
            {
                Utils.DeleteNode(node, operation);
            }

            return newNode;
        }

        private static Operand GenerateVertexOffset(ResourceManager resourceManager, LinkedListNode<INode> node, int location, int component)
        {
            int vertexInfoCbBinding = resourceManager.Reservations.VertexInfoConstantBufferBinding;

            Operand vertexIdVr = Local();
            GenerateVertexIdVertexRateLoad(resourceManager, node, vertexIdVr);

            Operand vertexIdIr = Local();
            GenerateVertexIdInstanceRateLoad(resourceManager, node, vertexIdIr);

            Operand attributeOffset = Local();
            node.List.AddBefore(node, new Operation(
                Instruction.Load,
                StorageKind.ConstantBuffer,
                attributeOffset,
                new[] { Const(vertexInfoCbBinding), Const((int)VertexInfoBufferField.VertexOffsets), Const(location), Const(0) }));

            Operand isInstanceRate = Local();
            node.List.AddBefore(node, new Operation(
                Instruction.Load,
                StorageKind.ConstantBuffer,
                isInstanceRate,
                new[] { Const(vertexInfoCbBinding), Const((int)VertexInfoBufferField.VertexOffsets), Const(location), Const(1) }));

            Operand vertexId = Local();
            node.List.AddBefore(node, new Operation(
                Instruction.ConditionalSelect,
                vertexId,
                new[] { isInstanceRate, vertexIdIr, vertexIdVr }));

            Operand vertexStride = Local();
            node.List.AddBefore(node, new Operation(
                Instruction.Load,
                StorageKind.ConstantBuffer,
                vertexStride,
                new[] { Const(vertexInfoCbBinding), Const((int)VertexInfoBufferField.VertexStrides), Const(location), Const(0) }));

            Operand vertexBaseOffset = Local();
            node.List.AddBefore(node, new Operation(Instruction.Multiply, vertexBaseOffset, new[] { vertexId, vertexStride }));

            Operand vertexOffset = Local();
            node.List.AddBefore(node, new Operation(Instruction.Add, vertexOffset, new[] { attributeOffset, vertexBaseOffset }));

            Operand vertexElemOffset;

            if (component != 0)
            {
                vertexElemOffset = Local();

                node.List.AddBefore(node, new Operation(Instruction.Add, vertexElemOffset, new[] { vertexOffset, Const(component) }));
            }
            else
            {
                vertexElemOffset = vertexOffset;
            }

            return vertexElemOffset;
        }

        private static LinkedListNode<INode> CopySignExtendedNormalized(LinkedListNode<INode> node, int bits, bool normalize, Operand dest, Operand src)
        {
            Operand leftShifted = Local();
            node = node.List.AddAfter(node, new Operation(
                Instruction.ShiftLeft,
                leftShifted,
                new[] { src, Const(32 - bits) }));

            Operand rightShifted = normalize ? Local() : dest;
            node = node.List.AddAfter(node, new Operation(
                Instruction.ShiftRightS32,
                rightShifted,
                new[] { leftShifted, Const(32 - bits) }));

            if (normalize)
            {
                Operand asFloat = Local();
                node = node.List.AddAfter(node, new Operation(Instruction.ConvertS32ToFP32, asFloat, new[] { rightShifted }));
                node = node.List.AddAfter(node, new Operation(
                    Instruction.FP32 | Instruction.Multiply,
                    dest,
                    new[] { asFloat, ConstF(1f / (1 << (bits - 1))) }));
            }

            return node;
        }

        private static LinkedListNode<INode> CopyMasked(
            ResourceManager resourceManager,
            LinkedListNode<INode> node,
            int location,
            int component,
            Operand dest,
            Operand src)
        {
            Operand componentExists = Local();
            int vertexInfoCbBinding = resourceManager.Reservations.VertexInfoConstantBufferBinding;
            node = node.List.AddAfter(node, new Operation(
                Instruction.Load,
                StorageKind.ConstantBuffer,
                componentExists,
                new[] { Const(vertexInfoCbBinding), Const((int)VertexInfoBufferField.VertexStrides), Const(location), Const(component) }));

            return node.List.AddAfter(node, new Operation(
                Instruction.ConditionalSelect,
                dest,
                new[] { componentExists, src, ConstF(component == 3 ? 1f : 0f) }));
        }

        private static LinkedListNode<INode> GenerateBaseVertexLoad(ResourceManager resourceManager, LinkedListNode<INode> node, Operand dest)
        {
            int vertexInfoCbBinding = resourceManager.Reservations.VertexInfoConstantBufferBinding;

            return node.List.AddBefore(node, new Operation(
                Instruction.Load,
                StorageKind.ConstantBuffer,
                dest,
                new[] { Const(vertexInfoCbBinding), Const((int)VertexInfoBufferField.VertexCounts), Const(2) }));
        }

        private static LinkedListNode<INode> GenerateBaseInstanceLoad(ResourceManager resourceManager, LinkedListNode<INode> node, Operand dest)
        {
            int vertexInfoCbBinding = resourceManager.Reservations.VertexInfoConstantBufferBinding;

            return node.List.AddBefore(node, new Operation(
                Instruction.Load,
                StorageKind.ConstantBuffer,
                dest,
                new[] { Const(vertexInfoCbBinding), Const((int)VertexInfoBufferField.VertexCounts), Const(3) }));
        }

        private static LinkedListNode<INode> GenerateVertexIndexLoad(ResourceManager resourceManager, LinkedListNode<INode> node, Operand dest)
        {
            Operand baseVertex = Local();
            Operand vertexId = Local();

            GenerateBaseVertexLoad(resourceManager, node, baseVertex);
            GenerateVertexIdVertexRateLoad(resourceManager, node, vertexId);

            return node.List.AddBefore(node, new Operation(Instruction.Add, dest, new[] { baseVertex, vertexId }));
        }

        private static LinkedListNode<INode> GenerateInstanceIndexLoad(ResourceManager resourceManager, LinkedListNode<INode> node, Operand dest)
        {
            Operand baseInstance = Local();
            Operand instanceId = Local();

            GenerateBaseInstanceLoad(resourceManager, node, baseInstance);

            node.List.AddBefore(node, new Operation(
                Instruction.Load,
                StorageKind.Input,
                instanceId,
                new[] { Const((int)IoVariable.GlobalId), Const(1) }));

            return node.List.AddBefore(node, new Operation(Instruction.Add, dest, new[] { baseInstance, instanceId }));
        }

        private static LinkedListNode<INode> GenerateVertexIdVertexRateLoad(ResourceManager resourceManager, LinkedListNode<INode> node, Operand dest)
        {
            Operand[] sources = [Const(resourceManager.LocalVertexIndexVertexRateMemoryId)];

            return node.List.AddBefore(node, new Operation(Instruction.Load, StorageKind.LocalMemory, dest, sources));
        }

        private static LinkedListNode<INode> GenerateVertexIdInstanceRateLoad(ResourceManager resourceManager, LinkedListNode<INode> node, Operand dest)
        {
            Operand[] sources = [Const(resourceManager.LocalVertexIndexInstanceRateMemoryId)];

            return node.List.AddBefore(node, new Operation(Instruction.Load, StorageKind.LocalMemory, dest, sources));
        }

        private static LinkedListNode<INode> GenerateInstanceIdLoad(LinkedListNode<INode> node, Operand dest)
        {
            Operand[] sources = [Const((int)IoVariable.GlobalId), Const(1)];

            return node.List.AddBefore(node, new Operation(Instruction.Load, StorageKind.Input, dest, sources));
        }

        private static bool TryGetOutputOffset(ResourceManager resourceManager, Operation operation, out int outputOffset)
        {
            bool isStore = operation.Inst == Instruction.Store;

            IoVariable ioVariable = (IoVariable)operation.GetSource(0).Value;

            bool isValidOutput;

            if (ioVariable == IoVariable.UserDefined)
            {
                int lastIndex = operation.SourcesCount - (isStore ? 2 : 1);

                int location = operation.GetSource(1).Value;
                int component = operation.GetSource(lastIndex).Value;

                isValidOutput = resourceManager.Reservations.TryGetOffset(StorageKind.Output, location, component, out outputOffset);
            }
            else
            {
                if (ResourceReservations.IsVectorOrArrayVariable(ioVariable))
                {
                    int component = operation.GetSource(operation.SourcesCount - (isStore ? 2 : 1)).Value;

                    isValidOutput = resourceManager.Reservations.TryGetOffset(StorageKind.Output, ioVariable, component, out outputOffset);
                }
                else
                {
                    isValidOutput = resourceManager.Reservations.TryGetOffset(StorageKind.Output, ioVariable, out outputOffset);
                }
            }

            return isValidOutput;
        }
    }
}
