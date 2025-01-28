using Ryujinx.Common.Logging;
using Ryujinx.Graphics.Device;
using System;
using System.Collections.Generic;

namespace Ryujinx.Graphics.Gpu.Engine.MME
{
    /// <summary>
    /// Macro code interpreter.
    /// </summary>
    class MacroInterpreter : IMacroEE
    {
        /// <summary>
        /// Arguments FIFO.
        /// </summary>
        public Queue<FifoWord> Fifo { get; }

        private readonly int[] _gprs;

        private int _methAddr;
        private int _methIncr;

        private bool _carry;

        private int _opCode;
        private int _pipeOp;

        private bool _ignoreExitFlag;

        private int _pc;

        /// <summary>
        /// Creates a new instance of the macro code interpreter.
        /// </summary>
        public MacroInterpreter()
        {
            Fifo = new Queue<FifoWord>();

            _gprs = new int[8];
        }

        /// <summary>
        /// Executes a macro program until it exits.
        /// </summary>
        /// <param name="code">Code of the program to execute</param>
        /// <param name="state">Current GPU state</param>
        /// <param name="arg0">Optional argument passed to the program, 0 if not used</param>
        public void Execute(ReadOnlySpan<int> code, IDeviceState state, int arg0)
        {
            Reset();

            _gprs[1] = arg0;

            _pc = 0;

            FetchOpCode(code);

            while (Step(code, state))
            {
            }

            // Due to the delay slot, we still need to execute
            // one more instruction before we actually exit.
            Step(code, state);
        }

        /// <summary>
        /// Resets the internal interpreter state.
        /// Call each time you run a new program.
        /// </summary>
        private void Reset()
        {
            for (int index = 0; index < _gprs.Length; index++)
            {
                _gprs[index] = 0;
            }

            _methAddr = 0;
            _methIncr = 0;

            _carry = false;
        }

        /// <summary>
        /// Executes a single instruction of the program.
        /// </summary>
        /// <param name="code">Program code to execute</param>
        /// <param name="state">Current GPU state</param>
        /// <returns>True to continue execution, false if the program exited</returns>
        private bool Step(ReadOnlySpan<int> code, IDeviceState state)
        {
            int baseAddr = _pc - 1;

            FetchOpCode(code);

            if ((_opCode & 7) < 7)
            {
                // Operation produces a value.
                AssignmentOperation asgOp = (AssignmentOperation)((_opCode >> 4) & 7);

                int result = GetAluResult(state);

                switch (asgOp)
                {
                    // Fetch parameter and ignore result.
                    case AssignmentOperation.IgnoreAndFetch:
                        SetDstGpr(FetchParam());
                        break;
                    // Move result.
                    case AssignmentOperation.Move:
                        SetDstGpr(result);
                        break;
                    // Move result and use as Method Address.
                    case AssignmentOperation.MoveAndSetMaddr:
                        SetDstGpr(result);
                        SetMethAddr(result);
                        break;
                    // Fetch parameter and send result.
                    case AssignmentOperation.FetchAndSend:
                        SetDstGpr(FetchParam());
                        Send(state, result);
                        break;
                    // Move and send result.
                    case AssignmentOperation.MoveAndSend:
                        SetDstGpr(result);
                        Send(state, result);
                        break;
                    // Fetch parameter and use result as Method Address.
                    case AssignmentOperation.FetchAndSetMaddr:
                        SetDstGpr(FetchParam());
                        SetMethAddr(result);
                        break;
                    // Move result and use as Method Address, then fetch and send parameter.
                    case AssignmentOperation.MoveAndSetMaddrThenFetchAndSend:
                        SetDstGpr(result);
                        SetMethAddr(result);
                        Send(state, FetchParam());
                        break;
                    // Move result and use as Method Address, then send bits 17:12 of result.
                    case AssignmentOperation.MoveAndSetMaddrThenSendHigh:
                        SetDstGpr(result);
                        SetMethAddr(result);
                        Send(state, (result >> 12) & 0x3f);
                        break;
                }
            }
            else
            {
                // Branch.
                bool onNotZero = ((_opCode >> 4) & 1) != 0;

                bool taken = onNotZero
                    ? GetGprA() != 0
                    : GetGprA() == 0;

                if (taken)
                {
                    _pc = baseAddr + GetImm();

                    bool noDelays = (_opCode & 0x20) != 0;

                    if (noDelays)
                    {
                        FetchOpCode(code);
                    }
                    else
                    {
                        // The delay slot instruction exit flag should be ignored.
                        _ignoreExitFlag = true;
                    }

                    return true;
                }
            }

            bool exit = (_opCode & 0x80) != 0 && !_ignoreExitFlag;

            _ignoreExitFlag = false;

            return !exit;
        }

        /// <summary>
        /// Fetches a single operation code from the program code.
        /// </summary>
        /// <param name="code">Program code</param>
        private void FetchOpCode(ReadOnlySpan<int> code)
        {
            _opCode = _pipeOp;
            _pipeOp = code[_pc++];
        }

        /// <summary>
        /// Gets the result of the current Arithmetic and Logic unit operation.
        /// </summary>
        /// <param name="state">Current GPU state</param>
        /// <returns>Operation result</returns>
        private int GetAluResult(IDeviceState state)
        {
            AluOperation op = (AluOperation)(_opCode & 7);

            switch (op)
            {
                case AluOperation.AluReg:
                    return GetAluResult((AluRegOperation)((_opCode >> 17) & 0x1f), GetGprA(), GetGprB());

                case AluOperation.AddImmediate:
                    return GetGprA() + GetImm();

                case AluOperation.BitfieldReplace:
                case AluOperation.BitfieldExtractLslImm:
                case AluOperation.BitfieldExtractLslReg:
                    int bfSrcBit = (_opCode >> 17) & 0x1f;
                    int bfSize = (_opCode >> 22) & 0x1f;
                    int bfDstBit = (_opCode >> 27) & 0x1f;

                    int bfMask = (1 << bfSize) - 1;

                    int dst = GetGprA();
                    int src = GetGprB();

                    switch (op)
                    {
                        case AluOperation.BitfieldReplace:
                            src = (int)((uint)src >> bfSrcBit) & bfMask;

                            dst &= ~(bfMask << bfDstBit);

                            dst |= src << bfDstBit;

                            return dst;

                        case AluOperation.BitfieldExtractLslImm:
                            src = (int)((uint)src >> dst) & bfMask;

                            return src << bfDstBit;

                        case AluOperation.BitfieldExtractLslReg:
                            src = (int)((uint)src >> bfSrcBit) & bfMask;

                            return src << dst;
                    }

                    break;

                case AluOperation.ReadImmediate:
                    return Read(state, GetGprA() + GetImm());
            }

            throw new InvalidOperationException($"Invalid operation \"{op}\" on instruction 0x{_opCode:X8}.");
        }

        /// <summary>
        /// Gets the result of an Arithmetic and Logic operation using registers.
        /// </summary>
        /// <param name="aluOp">Arithmetic and Logic unit operation with registers</param>
        /// <param name="a">First operand value</param>
        /// <param name="b">Second operand value</param>
        /// <returns>Operation result</returns>
        private int GetAluResult(AluRegOperation aluOp, int a, int b)
        {
            ulong result;

            switch (aluOp)
            {
                case AluRegOperation.Add:
                    result = (ulong)a + (ulong)b;

                    _carry = result > 0xffffffff;

                    return (int)result;

                case AluRegOperation.AddWithCarry:
                    result = (ulong)a + (ulong)b + (_carry ? 1UL : 0UL);

                    _carry = result > 0xffffffff;

                    return (int)result;

                case AluRegOperation.Subtract:
                    result = (ulong)a - (ulong)b;

                    _carry = result < 0x100000000;

                    return (int)result;

                case AluRegOperation.SubtractWithBorrow:
                    result = (ulong)a - (ulong)b - (_carry ? 0UL : 1UL);

                    _carry = result < 0x100000000;

                    return (int)result;

                case AluRegOperation.BitwiseExclusiveOr:
                    return a ^ b;
                case AluRegOperation.BitwiseOr:
                    return a | b;
                case AluRegOperation.BitwiseAnd:
                    return a & b;
                case AluRegOperation.BitwiseAndNot:
                    return a & ~b;
                case AluRegOperation.BitwiseNotAnd:
                    return ~(a & b);
            }

            throw new InvalidOperationException($"Invalid operation \"{aluOp}\" on instruction 0x{_opCode:X8}.");
        }

        /// <summary>
        /// Extracts a 32-bits signed integer constant from the current operation code.
        /// </summary>
        /// <returns>The 32-bits immediate value encoded at the current operation code</returns>
        private int GetImm()
        {
            // Note: The immediate is signed, the sign-extension is intended here.
            return _opCode >> 14;
        }

        /// <summary>
        /// Sets the current method address, for method calls.
        /// </summary>
        /// <param name="value">Packed address and increment value</param>
        private void SetMethAddr(int value)
        {
            _methAddr = (value >> 0) & 0xfff;
            _methIncr = (value >> 12) & 0x3f;
        }

        /// <summary>
        /// Sets the destination register value.
        /// </summary>
        /// <param name="value">Value to set (usually the operation result)</param>
        private void SetDstGpr(int value)
        {
            _gprs[(_opCode >> 8) & 7] = value;
        }

        /// <summary>
        /// Gets first operand value from the respective register.
        /// </summary>
        /// <returns>Operand value</returns>
        private int GetGprA()
        {
            return GetGprValue((_opCode >> 11) & 7);
        }

        /// <summary>
        /// Gets second operand value from the respective register.
        /// </summary>
        /// <returns>Operand value</returns>
        private int GetGprB()
        {
            return GetGprValue((_opCode >> 14) & 7);
        }

        /// <summary>
        /// Gets the value from a register, or 0 if the R0 register is specified.
        /// </summary>
        /// <param name="index">Index of the register</param>
        /// <returns>Register value</returns>
        private int GetGprValue(int index)
        {
            return index != 0 ? _gprs[index] : 0;
        }

        /// <summary>
        /// Fetches a call argument from the call argument FIFO.
        /// </summary>
        /// <returns>The call argument, or 0 if the FIFO is empty</returns>
        private int FetchParam()
        {
            if (!Fifo.TryDequeue(out FifoWord value))
            {
                Logger.Warning?.Print(LogClass.Gpu, "Macro attempted to fetch an inexistent argument.");

                return 0;
            }

            return value.Word;
        }

        /// <summary>
        /// Reads data from a GPU register.
        /// </summary>
        /// <param name="state">Current GPU state</param>
        /// <param name="reg">Register offset to read</param>
        /// <returns>GPU register value</returns>
        private static int Read(IDeviceState state, int reg)
        {
            return state.Read(reg * 4);
        }

        /// <summary>
        /// Performs a GPU method call.
        /// </summary>
        /// <param name="state">Current GPU state</param>
        /// <param name="value">Call argument</param>
        private void Send(IDeviceState state, int value)
        {
            state.Write(_methAddr * 4, value);

            _methAddr += _methIncr;
        }
    }
}
