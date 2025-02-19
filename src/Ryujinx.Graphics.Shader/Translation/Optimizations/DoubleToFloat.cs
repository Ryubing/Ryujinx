using Ryujinx.Graphics.Shader.IntermediateRepresentation;
using System.Collections.Generic;

using static Ryujinx.Graphics.Shader.IntermediateRepresentation.OperandHelper;

namespace Ryujinx.Graphics.Shader.Translation.Optimizations
{
    static class DoubleToFloat
    {
        public static void RunPass(HelperFunctionManager hfm, BasicBlock block)
        {
            for (LinkedListNode<INode> node = block.Operations.First; node != null; node = node.Next)
            {
                if (node.Value is not Operation)
                {
                    continue;
                }

                node = InsertSoftFloat64(hfm, node);
            }
        }

        private static LinkedListNode<INode> InsertSoftFloat64(HelperFunctionManager hfm, LinkedListNode<INode> node)
        {
            Operation operation = (Operation)node.Value;

            if (operation.Inst == Instruction.PackDouble2x32)
            {
                int functionId = hfm.GetOrCreateFunctionId(HelperFunctionName.ConvertDoubleToFloat);

                Operand[] callArgs = [Const(functionId), operation.GetSource(0), operation.GetSource(1)];

                Operand floatValue = operation.Dest;

                operation.Dest = null;

                LinkedListNode<INode> newNode = node.List.AddBefore(node, new Operation(Instruction.Call, 0, floatValue, callArgs));

                Utils.DeleteNode(node, operation);

                return newNode;
            }
            else if (operation.Inst == Instruction.UnpackDouble2x32)
            {
                int functionId = hfm.GetOrCreateFunctionId(HelperFunctionName.ConvertFloatToDouble);

                // TODO: Allow UnpackDouble2x32 to produce two outputs and get rid of "operation.Index".

                Operand resultLow = operation.Index == 0 ? operation.Dest : Local();
                Operand resultHigh = operation.Index == 1 ? operation.Dest : Local();

                operation.Dest = null;

                Operand[] callArgs = [Const(functionId), operation.GetSource(0), resultLow, resultHigh];

                LinkedListNode<INode> newNode = node.List.AddBefore(node, new Operation(Instruction.Call, 0, (Operand)null, callArgs));

                Utils.DeleteNode(node, operation);

                return newNode;
            }
            else
            {
                operation.TurnDoubleIntoFloat();

                return node;
            }
        }
    }
}
