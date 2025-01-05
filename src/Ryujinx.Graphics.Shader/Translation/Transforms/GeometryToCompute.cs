using Ryujinx.Graphics.Shader.IntermediateRepresentation;
using Ryujinx.Graphics.Shader.Translation.Optimizations;
using System.Collections.Generic;

using static Ryujinx.Graphics.Shader.IntermediateRepresentation.OperandHelper;

namespace Ryujinx.Graphics.Shader.Translation.Transforms
{
    class GeometryToCompute : ITransformPass
    {
        public static bool IsEnabled(IGpuAccessor gpuAccessor, ShaderStage stage, TargetLanguage targetLanguage, FeatureFlags usedFeatures)
        {
            return usedFeatures.HasFlag(FeatureFlags.VtgAsCompute);
        }

        public static LinkedListNode<INode> RunPass(TransformContext context, LinkedListNode<INode> node)
        {
            if (context.Definitions.Stage != ShaderStage.Geometry)
            {
                return node;
            }

            Operation operation = (Operation)node.Value;

            LinkedListNode<INode> newNode = node;

            switch (operation.Inst)
            {
                case Instruction.EmitVertex:
                    newNode = GenerateEmitVertex(context.Definitions, context.ResourceManager, node);
                    break;
                case Instruction.EndPrimitive:
                    newNode = GenerateEndPrimitive(context.Definitions, context.ResourceManager, node);
                    break;
                case Instruction.Load:
                    if (operation.StorageKind == StorageKind.Input)
                    {
                        IoVariable ioVariable = (IoVariable)operation.GetSource(0).Value;

                        if (TryGetOffset(context.ResourceManager, operation, StorageKind.Input, out int inputOffset))
                        {
                            Operand primVertex = ioVariable == IoVariable.UserDefined
                                ? operation.GetSource(2)
                                : operation.GetSource(1);

                            Operand vertexElemOffset = GenerateVertexOffset(context.ResourceManager, node, inputOffset, primVertex);

                            newNode = node.List.AddBefore(node, new Operation(
                                Instruction.Load,
                                StorageKind.StorageBuffer,
                                operation.Dest,
                                [Const(context.ResourceManager.Reservations.VertexOutputStorageBufferBinding), Const(0), vertexElemOffset]));
                        }
                        else
                        {
                            switch (ioVariable)
                            {
                                case IoVariable.InvocationId:
                                    newNode = GenerateInvocationId(node, operation.Dest);
                                    break;
                                case IoVariable.PrimitiveId:
                                    newNode = GeneratePrimitiveId(context.ResourceManager, node, operation.Dest);
                                    break;
                                case IoVariable.GlobalId:
                                case IoVariable.SubgroupEqMask:
                                case IoVariable.SubgroupGeMask:
                                case IoVariable.SubgroupGtMask:
                                case IoVariable.SubgroupLaneId:
                                case IoVariable.SubgroupLeMask:
                                case IoVariable.SubgroupLtMask:
                                    // Those are valid or expected for geometry shaders.
                                    break;
                                default:
                                    context.GpuAccessor.Log($"Invalid input \"{ioVariable}\".");
                                    break;
                            }
                        }
                    }
                    else if (operation.StorageKind == StorageKind.Output)
                    {
                        if (TryGetOffset(context.ResourceManager, operation, StorageKind.Output, out int outputOffset))
                        {
                            newNode = node.List.AddBefore(node, new Operation(
                                Instruction.Load,
                                StorageKind.LocalMemory,
                                operation.Dest,
                                [Const(context.ResourceManager.LocalVertexDataMemoryId), Const(outputOffset)]));
                        }
                        else
                        {
                            context.GpuAccessor.Log($"Invalid output \"{(IoVariable)operation.GetSource(0).Value}\".");
                        }
                    }
                    break;
                case Instruction.Store:
                    if (operation.StorageKind == StorageKind.Output)
                    {
                        if (TryGetOffset(context.ResourceManager, operation, StorageKind.Output, out int outputOffset))
                        {
                            Operand value = operation.GetSource(operation.SourcesCount - 1);

                            newNode = node.List.AddBefore(node, new Operation(
                                Instruction.Store,
                                StorageKind.LocalMemory,
                                (Operand)null,
                                [Const(context.ResourceManager.LocalVertexDataMemoryId), Const(outputOffset), value]));
                        }
                        else
                        {
                            context.GpuAccessor.Log($"Invalid output \"{(IoVariable)operation.GetSource(0).Value}\".");
                        }
                    }
                    break;
            }

            if (newNode != node)
            {
                Utils.DeleteNode(node, operation);
            }

            return newNode;
        }

        private static LinkedListNode<INode> GenerateEmitVertex(ShaderDefinitions definitions, ResourceManager resourceManager, LinkedListNode<INode> node)
        {
            int vbOutputBinding = resourceManager.Reservations.GeometryVertexOutputStorageBufferBinding;
            int ibOutputBinding = resourceManager.Reservations.GeometryIndexOutputStorageBufferBinding;
            int stride = resourceManager.Reservations.OutputSizePerInvocation;

            Operand outputPrimVertex = IncrementLocalMemory(node, resourceManager.LocalGeometryOutputVertexCountMemoryId);
            Operand baseVertexOffset = GenerateBaseOffset(
                resourceManager,
                node,
                definitions.MaxOutputVertices * definitions.ThreadsPerInputPrimitive,
                definitions.ThreadsPerInputPrimitive);
            Operand outputBaseVertex = Local();
            node.List.AddBefore(node, new Operation(Instruction.Add, outputBaseVertex, [baseVertexOffset, outputPrimVertex]));

            Operand outputPrimIndex = IncrementLocalMemory(node, resourceManager.LocalGeometryOutputIndexCountMemoryId);
            Operand baseIndexOffset = GenerateBaseOffset(
                resourceManager,
                node,
                definitions.GetGeometryOutputIndexBufferStride(),
                definitions.ThreadsPerInputPrimitive);
            Operand outputBaseIndex = Local();
            node.List.AddBefore(node, new Operation(Instruction.Add, outputBaseIndex, [baseIndexOffset, outputPrimIndex]));

            node.List.AddBefore(node, new Operation(
                Instruction.Store,
                StorageKind.StorageBuffer,
                null,
                [Const(ibOutputBinding), Const(0), outputBaseIndex, outputBaseVertex]));

            Operand baseOffset = Local();
            node.List.AddBefore(node, new Operation(Instruction.Multiply, baseOffset, [outputBaseVertex, Const(stride)]));

            LinkedListNode<INode> newNode = node;

            for (int offset = 0; offset < stride; offset++)
            {
                Operand vertexOffset;

                if (offset > 0)
                {
                    vertexOffset = Local();
                    node.List.AddBefore(node, new Operation(Instruction.Add, vertexOffset, [baseOffset, Const(offset)]));
                }
                else
                {
                    vertexOffset = baseOffset;
                }

                Operand value = Local();
                node.List.AddBefore(node, new Operation(
                    Instruction.Load,
                    StorageKind.LocalMemory,
                    value,
                    [Const(resourceManager.LocalVertexDataMemoryId), Const(offset)]));

                newNode = node.List.AddBefore(node, new Operation(
                    Instruction.Store,
                    StorageKind.StorageBuffer,
                    null,
                    [Const(vbOutputBinding), Const(0), vertexOffset, value]));
            }

            return newNode;
        }

        private static LinkedListNode<INode> GenerateEndPrimitive(ShaderDefinitions definitions, ResourceManager resourceManager, LinkedListNode<INode> node)
        {
            int ibOutputBinding = resourceManager.Reservations.GeometryIndexOutputStorageBufferBinding;

            Operand outputPrimIndex = IncrementLocalMemory(node, resourceManager.LocalGeometryOutputIndexCountMemoryId);
            Operand baseIndexOffset = GenerateBaseOffset(
                resourceManager,
                node,
                definitions.GetGeometryOutputIndexBufferStride(),
                definitions.ThreadsPerInputPrimitive);
            Operand outputBaseIndex = Local();
            node.List.AddBefore(node, new Operation(Instruction.Add, outputBaseIndex, [baseIndexOffset, outputPrimIndex]));

            return node.List.AddBefore(node, new Operation(
                Instruction.Store,
                StorageKind.StorageBuffer,
                null,
                [Const(ibOutputBinding), Const(0), outputBaseIndex, Const(-1)]));
        }

        private static Operand GenerateBaseOffset(ResourceManager resourceManager, LinkedListNode<INode> node, int stride, int threadsPerInputPrimitive)
        {
            Operand primitiveId = Local();
            GeneratePrimitiveId(resourceManager, node, primitiveId);

            Operand baseOffset = Local();
            node.List.AddBefore(node, new Operation(Instruction.Multiply, baseOffset, [primitiveId, Const(stride)]));

            Operand invocationId = Local();
            GenerateInvocationId(node, invocationId);

            Operand invocationOffset = Local();
            node.List.AddBefore(node, new Operation(Instruction.Multiply, invocationOffset, [invocationId, Const(stride / threadsPerInputPrimitive)]));

            Operand combinedOffset = Local();
            node.List.AddBefore(node, new Operation(Instruction.Add, combinedOffset, [baseOffset, invocationOffset]));

            return combinedOffset;
        }

        private static Operand IncrementLocalMemory(LinkedListNode<INode> node, int memoryId)
        {
            Operand oldValue = Local();
            node.List.AddBefore(node, new Operation(
                Instruction.Load,
                StorageKind.LocalMemory,
                oldValue,
                [Const(memoryId)]));

            Operand newValue = Local();
            node.List.AddBefore(node, new Operation(Instruction.Add, newValue, [oldValue, Const(1)]));

            node.List.AddBefore(node, new Operation(Instruction.Store, StorageKind.LocalMemory, null, [Const(memoryId), newValue]));

            return oldValue;
        }

        private static Operand GenerateVertexOffset(
            ResourceManager resourceManager,
            LinkedListNode<INode> node,
            int elementOffset,
            Operand primVertex)
        {
            int vertexInfoCbBinding = resourceManager.Reservations.VertexInfoConstantBufferBinding;

            Operand vertexCount = Local();
            node.List.AddBefore(node, new Operation(
                Instruction.Load,
                StorageKind.ConstantBuffer,
                vertexCount,
                [Const(vertexInfoCbBinding), Const((int)VertexInfoBufferField.VertexCounts), Const(0)]));

            Operand primInputVertex = Local();
            node.List.AddBefore(node, new Operation(
                Instruction.Load,
                StorageKind.LocalMemory,
                primInputVertex,
                [Const(resourceManager.LocalTopologyRemapMemoryId), primVertex]));

            Operand instanceIndex = Local();
            node.List.AddBefore(node, new Operation(
                Instruction.Load,
                StorageKind.Input,
                instanceIndex,
                [Const((int)IoVariable.GlobalId), Const(1)]));

            Operand baseVertex = Local();
            node.List.AddBefore(node, new Operation(Instruction.Multiply, baseVertex, [instanceIndex, vertexCount]));

            Operand vertexIndex = Local();
            node.List.AddBefore(node, new Operation(Instruction.Add, vertexIndex, [baseVertex, primInputVertex]));

            Operand vertexBaseOffset = Local();
            node.List.AddBefore(node, new Operation(
                Instruction.Multiply,
                vertexBaseOffset,
                [vertexIndex, Const(resourceManager.Reservations.InputSizePerInvocation)]));

            Operand vertexElemOffset;

            if (elementOffset != 0)
            {
                vertexElemOffset = Local();

                node.List.AddBefore(node, new Operation(Instruction.Add, vertexElemOffset, [vertexBaseOffset, Const(elementOffset)]));
            }
            else
            {
                vertexElemOffset = vertexBaseOffset;
            }

            return vertexElemOffset;
        }

        private static LinkedListNode<INode> GeneratePrimitiveId(ResourceManager resourceManager, LinkedListNode<INode> node, Operand dest)
        {
            int vertexInfoCbBinding = resourceManager.Reservations.VertexInfoConstantBufferBinding;

            Operand vertexCount = Local();
            node.List.AddBefore(node, new Operation(
                Instruction.Load,
                StorageKind.ConstantBuffer,
                vertexCount,
                [Const(vertexInfoCbBinding), Const((int)VertexInfoBufferField.VertexCounts), Const(0)]));

            Operand vertexIndex = Local();
            node.List.AddBefore(node, new Operation(
                Instruction.Load,
                StorageKind.Input,
                vertexIndex,
                [Const((int)IoVariable.GlobalId), Const(0)]));

            Operand instanceIndex = Local();
            node.List.AddBefore(node, new Operation(
                Instruction.Load,
                StorageKind.Input,
                instanceIndex,
                [Const((int)IoVariable.GlobalId), Const(1)]));

            Operand baseVertex = Local();
            node.List.AddBefore(node, new Operation(Instruction.Multiply, baseVertex, [instanceIndex, vertexCount]));

            return node.List.AddBefore(node, new Operation(Instruction.Add, dest, [baseVertex, vertexIndex]));
        }

        private static LinkedListNode<INode> GenerateInvocationId(LinkedListNode<INode> node, Operand dest)
        {
            return node.List.AddBefore(node, new Operation(
                Instruction.Load,
                StorageKind.Input,
                dest,
                [Const((int)IoVariable.GlobalId), Const(2)]));
        }

        private static bool TryGetOffset(ResourceManager resourceManager, Operation operation, StorageKind storageKind, out int outputOffset)
        {
            bool isStore = operation.Inst == Instruction.Store;

            IoVariable ioVariable = (IoVariable)operation.GetSource(0).Value;

            bool isValidOutput;

            if (ioVariable == IoVariable.UserDefined)
            {
                int lastIndex = operation.SourcesCount - (isStore ? 2 : 1);

                int location = operation.GetSource(1).Value;
                int component = operation.GetSource(lastIndex).Value;

                isValidOutput = resourceManager.Reservations.TryGetOffset(storageKind, location, component, out outputOffset);
            }
            else
            {
                if (ResourceReservations.IsVectorOrArrayVariable(ioVariable))
                {
                    int component = operation.GetSource(operation.SourcesCount - (isStore ? 2 : 1)).Value;

                    isValidOutput = resourceManager.Reservations.TryGetOffset(storageKind, ioVariable, component, out outputOffset);
                }
                else
                {
                    isValidOutput = resourceManager.Reservations.TryGetOffset(storageKind, ioVariable, out outputOffset);
                }
            }

            return isValidOutput;
        }
    }
}
