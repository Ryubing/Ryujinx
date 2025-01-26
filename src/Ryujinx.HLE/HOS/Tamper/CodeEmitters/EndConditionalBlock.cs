using Ryujinx.HLE.Exceptions;
using Ryujinx.HLE.HOS.Tamper.Conditions;
using Ryujinx.HLE.HOS.Tamper.Operations;
using System.Collections.Generic;

namespace Ryujinx.HLE.HOS.Tamper.CodeEmitters
{
    /// <summary>
    /// Code type 2 marks the end of a conditional block (started by Code Type 1, Code Type 8 or Code Type C0).
    /// </summary>
    class EndConditionalBlock
    {
        const int TerminationTypeIndex = 1;

        private const byte End = 0; // True end of the conditional.
        private const byte Else = 1; // End of the 'then' block and beginning of 'else' block.

        public static void Emit(byte[] instruction, CompilationContext context)
        {
            Emit(instruction, context, null);
        }

        private static void Emit(byte[] instruction, CompilationContext context, IEnumerable<IOperation> operationsElse)
        {
            // 2X000000
            // X: End type (0 = End, 1 = Else).

            byte terminationType = instruction[TerminationTypeIndex];

            switch (terminationType)
            {
                case End:
                    break;
                case Else:
                    // Start a new operation block with the 'else' instruction to signal that there is the 'then' block just above it.
                    context.BlockStack.Push(new OperationBlock(instruction));
                    return;
                default:
                    throw new TamperCompilationException($"Unknown conditional termination type {terminationType}");
            }

            // Use the conditional begin instruction stored in the stack.
            byte[] upperInstruction = context.CurrentBlock.BaseInstruction;
            CodeType codeType = InstructionHelper.GetCodeType(upperInstruction);

            // Pop the current block of operations from the stack so control instructions
            // for the conditional can be emitted in the upper block.
            IEnumerable<IOperation> operations = context.CurrentOperations;
            context.BlockStack.Pop();

            // If the else operations are already set, then the upper block must not be another end.
            if (operationsElse != null && codeType == CodeType.EndConditionalBlock)
            {
                throw new TamperCompilationException("Expected an upper 'if' conditional instead of 'end conditional'");
            }

            ICondition condition;

            switch (codeType)
            {
                case CodeType.BeginMemoryConditionalBlock:
                    condition = MemoryConditional.Emit(upperInstruction, context);
                    break;
                case CodeType.BeginKeypressConditionalBlock:
                    condition = KeyPressConditional.Emit(upperInstruction, context);
                    break;
                case CodeType.BeginRegisterConditionalBlock:
                    condition = RegisterConditional.Emit(upperInstruction, context);
                    break;
                case CodeType.EndConditionalBlock:
                    terminationType = upperInstruction[TerminationTypeIndex];
                    // If there is an end instruction above then it must be an else.
                    if (terminationType != Else)
                    {
                        throw new TamperCompilationException($"Expected an upper 'else' conditional instead of {terminationType}");
                    }
                    // Re-run the Emit with the else operations set.
                    Emit(instruction, context, operations);
                    return;
                default:
                    throw new TamperCompilationException($"Conditional end does not match code type {codeType} in Atmosphere cheat");
            }

            // Create a conditional block with the current operations and nest it in the upper
            // block of the stack.

            IfBlock block = new(condition, operations, operationsElse);
            context.CurrentOperations.Add(block);
        }
    }
}
