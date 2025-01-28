using Ryujinx.Graphics.Device;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace Ryujinx.Graphics.Gpu.Engine.MME
{
    /// <summary>
    /// Represents a Macro Just-in-Time compiler.
    /// </summary>R
    class MacroJitCompiler
    {
        private readonly DynamicMethod _meth;
        private readonly ILGenerator _ilGen;
        private readonly LocalBuilder[] _gprs;
        private readonly LocalBuilder _carry;
        private readonly LocalBuilder _methAddr;
        private readonly LocalBuilder _methIncr;

        /// <summary>
        /// Creates a new instance of the Macro Just-in-Time compiler.
        /// </summary>
        public MacroJitCompiler()
        {
            _meth = new DynamicMethod("Macro", typeof(void), [typeof(MacroJitContext), typeof(IDeviceState), typeof(int)
            ]);
            _ilGen = _meth.GetILGenerator();
            _gprs = new LocalBuilder[8];

            for (int i = 1; i < 8; i++)
            {
                _gprs[i] = _ilGen.DeclareLocal(typeof(int));
            }

            _carry = _ilGen.DeclareLocal(typeof(int));
            _methAddr = _ilGen.DeclareLocal(typeof(int));
            _methIncr = _ilGen.DeclareLocal(typeof(int));

            _ilGen.Emit(OpCodes.Ldarg_2);
            _ilGen.Emit(OpCodes.Stloc, _gprs[1]);
        }

        public delegate void MacroExecute(MacroJitContext context, IDeviceState state, int arg0);

        /// <summary>
        /// Translates a new piece of GPU Macro code into host executable code.
        /// </summary>
        /// <param name="code">Code to be translated</param>
        /// <returns>Delegate of the host compiled code</returns>
        public MacroExecute Compile(ReadOnlySpan<int> code)
        {
            Dictionary<int, Label> labels = new();

            int lastTarget = 0;
            int i;

            // Collect all branch targets.
            for (i = 0; i < code.Length; i++)
            {
                int opCode = code[i];

                if ((opCode & 7) == 7)
                {
                    int target = i + (opCode >> 14);

                    if (!labels.ContainsKey(target))
                    {
                        labels.Add(target, _ilGen.DefineLabel());
                    }

                    if (lastTarget < target)
                    {
                        lastTarget = target;
                    }
                }

                bool exit = (opCode & 0x80) != 0;

                if (exit && i >= lastTarget)
                {
                    break;
                }
            }

            // Code generation.
            for (i = 0; i < code.Length; i++)
            {
                if (labels.TryGetValue(i, out Label label))
                {
                    _ilGen.MarkLabel(label);
                }

                Emit(code, i, labels);

                int opCode = code[i];

                bool exit = (opCode & 0x80) != 0;

                if (exit)
                {
                    Emit(code, i + 1, labels);
                    _ilGen.Emit(OpCodes.Ret);

                    if (i >= lastTarget)
                    {
                        break;
                    }
                }
            }

            if (i == code.Length)
            {
                _ilGen.Emit(OpCodes.Ret);
            }

            return _meth.CreateDelegate<MacroExecute>();
        }

        /// <summary>
        /// Emits IL equivalent to the Macro instruction at a given offset.
        /// </summary>
        /// <param name="code">GPU Macro code</param>
        /// <param name="offset">Offset, in words, where the instruction is located</param>
        /// <param name="labels">Labels for Macro branch targets, used by branch instructions</param>
        private void Emit(ReadOnlySpan<int> code, int offset, Dictionary<int, Label> labels)
        {
            int opCode = code[offset];

            if ((opCode & 7) < 7)
            {
                // Operation produces a value.
                AssignmentOperation asgOp = (AssignmentOperation)((opCode >> 4) & 7);

                EmitAluOp(opCode);

                switch (asgOp)
                {
                    // Fetch parameter and ignore result.
                    case AssignmentOperation.IgnoreAndFetch:
                        _ilGen.Emit(OpCodes.Pop);
                        EmitFetchParam();
                        EmitStoreDstGpr(opCode);
                        break;
                    // Move result.
                    case AssignmentOperation.Move:
                        EmitStoreDstGpr(opCode);
                        break;
                    // Move result and use as Method Address.
                    case AssignmentOperation.MoveAndSetMaddr:
                        _ilGen.Emit(OpCodes.Dup);
                        EmitStoreDstGpr(opCode);
                        EmitStoreMethAddr();
                        break;
                    // Fetch parameter and send result.
                    case AssignmentOperation.FetchAndSend:
                        EmitFetchParam();
                        EmitStoreDstGpr(opCode);
                        EmitSend();
                        break;
                    // Move and send result.
                    case AssignmentOperation.MoveAndSend:
                        _ilGen.Emit(OpCodes.Dup);
                        EmitStoreDstGpr(opCode);
                        EmitSend();
                        break;
                    // Fetch parameter and use result as Method Address.
                    case AssignmentOperation.FetchAndSetMaddr:
                        EmitFetchParam();
                        EmitStoreDstGpr(opCode);
                        EmitStoreMethAddr();
                        break;
                    // Move result and use as Method Address, then fetch and send parameter.
                    case AssignmentOperation.MoveAndSetMaddrThenFetchAndSend:
                        _ilGen.Emit(OpCodes.Dup);
                        EmitStoreDstGpr(opCode);
                        EmitStoreMethAddr();
                        EmitFetchParam();
                        EmitSend();
                        break;
                    // Move result and use as Method Address, then send bits 17:12 of result.
                    case AssignmentOperation.MoveAndSetMaddrThenSendHigh:
                        _ilGen.Emit(OpCodes.Dup);
                        _ilGen.Emit(OpCodes.Dup);
                        EmitStoreDstGpr(opCode);
                        EmitStoreMethAddr();
                        _ilGen.Emit(OpCodes.Ldc_I4, 12);
                        _ilGen.Emit(OpCodes.Shr_Un);
                        _ilGen.Emit(OpCodes.Ldc_I4, 0x3f);
                        _ilGen.Emit(OpCodes.And);
                        EmitSend();
                        break;
                }
            }
            else
            {
                // Branch.
                bool onNotZero = ((opCode >> 4) & 1) != 0;

                EmitLoadGprA(opCode);

                Label lblSkip = _ilGen.DefineLabel();

                if (onNotZero)
                {
                    _ilGen.Emit(OpCodes.Brfalse, lblSkip);
                }
                else
                {
                    _ilGen.Emit(OpCodes.Brtrue, lblSkip);
                }

                bool noDelays = (opCode & 0x20) != 0;

                if (!noDelays)
                {
                    Emit(code, offset + 1, labels);
                }

                int target = offset + (opCode >> 14);

                _ilGen.Emit(OpCodes.Br, labels[target]);

                _ilGen.MarkLabel(lblSkip);
            }
        }

        /// <summary>
        /// Emits IL for a Arithmetic and Logic Unit instruction.
        /// </summary>
        /// <param name="opCode">Instruction to be translated</param>
        /// <exception cref="InvalidOperationException">Throw when the instruction encoding is invalid</exception>
        private void EmitAluOp(int opCode)
        {
            AluOperation op = (AluOperation)(opCode & 7);

            switch (op)
            {
                case AluOperation.AluReg:
                    EmitAluOp((AluRegOperation)((opCode >> 17) & 0x1f), opCode);
                    break;

                case AluOperation.AddImmediate:
                    EmitLoadGprA(opCode);
                    EmitLoadImm(opCode);
                    _ilGen.Emit(OpCodes.Add);
                    break;

                case AluOperation.BitfieldReplace:
                case AluOperation.BitfieldExtractLslImm:
                case AluOperation.BitfieldExtractLslReg:
                    int bfSrcBit = (opCode >> 17) & 0x1f;
                    int bfSize = (opCode >> 22) & 0x1f;
                    int bfDstBit = (opCode >> 27) & 0x1f;

                    int bfMask = (1 << bfSize) - 1;

                    switch (op)
                    {
                        case AluOperation.BitfieldReplace:
                            EmitLoadGprB(opCode);
                            _ilGen.Emit(OpCodes.Ldc_I4, bfSrcBit);
                            _ilGen.Emit(OpCodes.Shr_Un);
                            _ilGen.Emit(OpCodes.Ldc_I4, bfMask);
                            _ilGen.Emit(OpCodes.And);
                            _ilGen.Emit(OpCodes.Ldc_I4, bfDstBit);
                            _ilGen.Emit(OpCodes.Shl);
                            EmitLoadGprA(opCode);
                            _ilGen.Emit(OpCodes.Ldc_I4, ~(bfMask << bfDstBit));
                            _ilGen.Emit(OpCodes.And);
                            _ilGen.Emit(OpCodes.Or);
                            break;

                        case AluOperation.BitfieldExtractLslImm:
                            EmitLoadGprB(opCode);
                            EmitLoadGprA(opCode);
                            _ilGen.Emit(OpCodes.Shr_Un);
                            _ilGen.Emit(OpCodes.Ldc_I4, bfMask);
                            _ilGen.Emit(OpCodes.And);
                            _ilGen.Emit(OpCodes.Ldc_I4, bfDstBit);
                            _ilGen.Emit(OpCodes.Shl);
                            break;

                        case AluOperation.BitfieldExtractLslReg:
                            EmitLoadGprB(opCode);
                            _ilGen.Emit(OpCodes.Ldc_I4, bfSrcBit);
                            _ilGen.Emit(OpCodes.Shr_Un);
                            _ilGen.Emit(OpCodes.Ldc_I4, bfMask);
                            _ilGen.Emit(OpCodes.And);
                            EmitLoadGprA(opCode);
                            _ilGen.Emit(OpCodes.Shl);
                            break;
                    }
                    break;

                case AluOperation.ReadImmediate:
                    _ilGen.Emit(OpCodes.Ldarg_1);
                    EmitLoadGprA(opCode);
                    EmitLoadImm(opCode);
                    _ilGen.Emit(OpCodes.Add);
                    _ilGen.Emit(OpCodes.Call, typeof(MacroJitContext).GetMethod(nameof(MacroJitContext.Read)));
                    break;

                default:
                    throw new InvalidOperationException($"Invalid operation \"{op}\" on instruction 0x{opCode:X8}.");
            }
        }

        /// <summary>
        /// Emits IL for a binary Arithmetic and Logic Unit instruction.
        /// </summary>
        /// <param name="aluOp">Arithmetic and Logic Unit instruction</param>
        /// <param name="opCode">Raw instruction</param>
        /// <exception cref="InvalidOperationException">Throw when the instruction encoding is invalid</exception>
        private void EmitAluOp(AluRegOperation aluOp, int opCode)
        {
            switch (aluOp)
            {
                case AluRegOperation.Add:
                    EmitLoadGprA(opCode);
                    _ilGen.Emit(OpCodes.Conv_U8);
                    EmitLoadGprB(opCode);
                    _ilGen.Emit(OpCodes.Conv_U8);
                    _ilGen.Emit(OpCodes.Add);
                    _ilGen.Emit(OpCodes.Dup);
                    _ilGen.Emit(OpCodes.Ldc_I8, 0xffffffffL);
                    _ilGen.Emit(OpCodes.Cgt_Un);
                    _ilGen.Emit(OpCodes.Stloc, _carry);
                    _ilGen.Emit(OpCodes.Conv_U4);
                    break;
                case AluRegOperation.AddWithCarry:
                    EmitLoadGprA(opCode);
                    _ilGen.Emit(OpCodes.Conv_U8);
                    EmitLoadGprB(opCode);
                    _ilGen.Emit(OpCodes.Conv_U8);
                    _ilGen.Emit(OpCodes.Ldloc_S, _carry);
                    _ilGen.Emit(OpCodes.Conv_U8);
                    _ilGen.Emit(OpCodes.Add);
                    _ilGen.Emit(OpCodes.Add);
                    _ilGen.Emit(OpCodes.Dup);
                    _ilGen.Emit(OpCodes.Ldc_I8, 0xffffffffL);
                    _ilGen.Emit(OpCodes.Cgt_Un);
                    _ilGen.Emit(OpCodes.Stloc, _carry);
                    _ilGen.Emit(OpCodes.Conv_U4);
                    break;
                case AluRegOperation.Subtract:
                    EmitLoadGprA(opCode);
                    _ilGen.Emit(OpCodes.Conv_U8);
                    EmitLoadGprB(opCode);
                    _ilGen.Emit(OpCodes.Conv_U8);
                    _ilGen.Emit(OpCodes.Sub);
                    _ilGen.Emit(OpCodes.Dup);
                    _ilGen.Emit(OpCodes.Ldc_I8, 0x100000000L);
                    _ilGen.Emit(OpCodes.Clt_Un);
                    _ilGen.Emit(OpCodes.Stloc, _carry);
                    _ilGen.Emit(OpCodes.Conv_U4);
                    break;
                case AluRegOperation.SubtractWithBorrow:
                    EmitLoadGprA(opCode);
                    _ilGen.Emit(OpCodes.Conv_U8);
                    EmitLoadGprB(opCode);
                    _ilGen.Emit(OpCodes.Conv_U8);
                    _ilGen.Emit(OpCodes.Ldc_I4_1);
                    _ilGen.Emit(OpCodes.Ldloc_S, _carry);
                    _ilGen.Emit(OpCodes.Sub);
                    _ilGen.Emit(OpCodes.Conv_U8);
                    _ilGen.Emit(OpCodes.Sub);
                    _ilGen.Emit(OpCodes.Sub);
                    _ilGen.Emit(OpCodes.Dup);
                    _ilGen.Emit(OpCodes.Ldc_I8, 0x100000000L);
                    _ilGen.Emit(OpCodes.Clt_Un);
                    _ilGen.Emit(OpCodes.Stloc, _carry);
                    _ilGen.Emit(OpCodes.Conv_U4);
                    break;
                case AluRegOperation.BitwiseExclusiveOr:
                    EmitLoadGprA(opCode);
                    EmitLoadGprB(opCode);
                    _ilGen.Emit(OpCodes.Xor);
                    break;
                case AluRegOperation.BitwiseOr:
                    EmitLoadGprA(opCode);
                    EmitLoadGprB(opCode);
                    _ilGen.Emit(OpCodes.Or);
                    break;
                case AluRegOperation.BitwiseAnd:
                    EmitLoadGprA(opCode);
                    EmitLoadGprB(opCode);
                    _ilGen.Emit(OpCodes.And);
                    break;
                case AluRegOperation.BitwiseAndNot:
                    EmitLoadGprA(opCode);
                    EmitLoadGprB(opCode);
                    _ilGen.Emit(OpCodes.Not);
                    _ilGen.Emit(OpCodes.And);
                    break;
                case AluRegOperation.BitwiseNotAnd:
                    EmitLoadGprA(opCode);
                    EmitLoadGprB(opCode);
                    _ilGen.Emit(OpCodes.And);
                    _ilGen.Emit(OpCodes.Not);
                    break;
                default:
                    throw new InvalidOperationException($"Invalid operation \"{aluOp}\" on instruction 0x{opCode:X8}.");
            }
        }

        /// <summary>
        /// Loads a immediate value on the IL evaluation stack.
        /// </summary>
        /// <param name="opCode">Instruction from where the immediate should be extracted</param>
        private void EmitLoadImm(int opCode)
        {
            // Note: The immediate is signed, the sign-extension is intended here.
            _ilGen.Emit(OpCodes.Ldc_I4, opCode >> 14);
        }

        /// <summary>
        /// Loads a value from the General Purpose register specified as first operand on the IL evaluation stack.
        /// </summary>
        /// <param name="opCode">Instruction from where the register number should be extracted</param>
        private void EmitLoadGprA(int opCode)
        {
            EmitLoadGpr((opCode >> 11) & 7);
        }

        /// <summary>
        /// Loads a value from the General Purpose register specified as second operand on the IL evaluation stack.
        /// </summary>
        /// <param name="opCode">Instruction from where the register number should be extracted</param>
        private void EmitLoadGprB(int opCode)
        {
            EmitLoadGpr((opCode >> 14) & 7);
        }

        /// <summary>
        /// Loads a value a General Purpose register on the IL evaluation stack.
        /// </summary>
        /// <remarks>
        /// Register number 0 has a hardcoded value of 0.
        /// </remarks>
        /// <param name="index">Register number</param>
        private void EmitLoadGpr(int index)
        {
            if (index == 0)
            {
                _ilGen.Emit(OpCodes.Ldc_I4_0);
            }
            else
            {
                _ilGen.Emit(OpCodes.Ldloc_S, _gprs[index]);
            }
        }

        /// <summary>
        /// Emits a call to the method that fetches an argument from the arguments FIFO.
        /// The argument is pushed into the IL evaluation stack.
        /// </summary>
        private void EmitFetchParam()
        {
            _ilGen.Emit(OpCodes.Ldarg_0);
            _ilGen.Emit(OpCodes.Call, typeof(MacroJitContext).GetMethod(nameof(MacroJitContext.FetchParam)));
        }

        /// <summary>
        /// Stores the value on the top of the IL evaluation stack into a General Purpose register.
        /// </summary>
        /// <remarks>
        /// Register number 0 does not exist, reads are hardcoded to 0, and writes are simply discarded.
        /// </remarks>
        /// <param name="opCode">Instruction from where the register number should be extracted</param>
        private void EmitStoreDstGpr(int opCode)
        {
            int index = (opCode >> 8) & 7;

            if (index == 0)
            {
                _ilGen.Emit(OpCodes.Pop);
            }
            else
            {
                _ilGen.Emit(OpCodes.Stloc_S, _gprs[index]);
            }
        }

        /// <summary>
        /// Stores the value on the top of the IL evaluation stack as method address.
        /// This will be used on subsequent send calls as the destination method address.
        /// Additionally, the 6 bits starting at bit 12 will be used as increment value,
        /// added to the method address after each sent value.
        /// </summary>
        private void EmitStoreMethAddr()
        {
            _ilGen.Emit(OpCodes.Dup);
            _ilGen.Emit(OpCodes.Ldc_I4, 0xfff);
            _ilGen.Emit(OpCodes.And);
            _ilGen.Emit(OpCodes.Stloc_S, _methAddr);
            _ilGen.Emit(OpCodes.Ldc_I4, 12);
            _ilGen.Emit(OpCodes.Shr_Un);
            _ilGen.Emit(OpCodes.Ldc_I4, 0x3f);
            _ilGen.Emit(OpCodes.And);
            _ilGen.Emit(OpCodes.Stloc_S, _methIncr);
        }

        /// <summary>
        /// Sends the value on the top of the IL evaluation stack to the GPU,
        /// using the current method address.
        /// </summary>
        private void EmitSend()
        {
            _ilGen.Emit(OpCodes.Ldarg_1);
            _ilGen.Emit(OpCodes.Ldloc_S, _methAddr);
            _ilGen.Emit(OpCodes.Call, typeof(MacroJitContext).GetMethod(nameof(MacroJitContext.Send)));
            _ilGen.Emit(OpCodes.Ldloc_S, _methAddr);
            _ilGen.Emit(OpCodes.Ldloc_S, _methIncr);
            _ilGen.Emit(OpCodes.Add);
            _ilGen.Emit(OpCodes.Stloc_S, _methAddr);
        }
    }
}
