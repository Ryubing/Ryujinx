using ARMeilleure.Common;
using Ryujinx.Cpu.LightningJit.Cache;
using Ryujinx.Cpu.LightningJit.CodeGen;
using Ryujinx.Cpu.LightningJit.CodeGen.Arm64;
using Ryujinx.Cpu.LightningJit.State;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Ryujinx.Cpu.LightningJit
{
    delegate void DispatcherFunction(nint nativeContext, ulong startAddress);

    /// <summary>
    /// Represents a stub manager.
    /// </summary>
    class TranslatorStubs : IDisposable
    {
        private delegate ulong GetFunctionAddressDelegate(nint framePointer, ulong address);

        private readonly Lazy<nint> _slowDispatchStub;

        private bool _disposed;

        private readonly IAddressTable<ulong> _functionTable;
        private readonly NoWxCache _noWxCache;
        private readonly GetFunctionAddressDelegate _getFunctionAddressRef;
        private readonly nint _getFunctionAddress;
        private readonly Lazy<nint> _dispatchStub;
        private readonly Lazy<DispatcherFunction> _dispatchLoop;

        /// <summary>
        /// Gets the dispatch stub.
        /// </summary>
        /// <exception cref="ObjectDisposedException"><see cref="TranslatorStubs"/> instance was disposed</exception>
        public nint DispatchStub
        {
            get
            {
                ObjectDisposedException.ThrowIf(_disposed, this);

                return _dispatchStub.Value;
            }
        }

        /// <summary>
        /// Gets the slow dispatch stub.
        /// </summary>
        /// <exception cref="ObjectDisposedException"><see cref="TranslatorStubs"/> instance was disposed</exception>
        public nint SlowDispatchStub
        {
            get
            {
                ObjectDisposedException.ThrowIf(_disposed, this);

                return _slowDispatchStub.Value;
            }
        }

        /// <summary>
        /// Gets the dispatch loop function.
        /// </summary>
        /// <exception cref="ObjectDisposedException"><see cref="TranslatorStubs"/> instance was disposed</exception>
        public DispatcherFunction DispatchLoop
        {
            get
            {
                ObjectDisposedException.ThrowIf(_disposed, this);

                return _dispatchLoop.Value;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TranslatorStubs"/> class with the specified
        /// <see cref="Translator"/> instance.
        /// </summary>
        /// <param name="functionTable">Function table used to store pointers to the functions that the guest code will call</param>
        /// <param name="noWxCache">Cache used on platforms that enforce W^X, otherwise should be null</param>
        /// <exception cref="ArgumentNullException"><paramref name="translator"/> is null</exception>
        public TranslatorStubs(IAddressTable<ulong> functionTable, NoWxCache noWxCache)
        {
            ArgumentNullException.ThrowIfNull(functionTable);

            _functionTable = functionTable;
            _noWxCache = noWxCache;
            _getFunctionAddressRef = NativeInterface.GetFunctionAddress;
            _getFunctionAddress = Marshal.GetFunctionPointerForDelegate(_getFunctionAddressRef);
            _slowDispatchStub = new(GenerateSlowDispatchStub, isThreadSafe: true);
            _dispatchStub = new(GenerateDispatchStub, isThreadSafe: true);
            _dispatchLoop = new(GenerateDispatchLoop, isThreadSafe: true);
        }

        /// <summary>
        /// Releases all resources used by the <see cref="TranslatorStubs"/> instance.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases all unmanaged and optionally managed resources used by the <see cref="TranslatorStubs"/> instance.
        /// </summary>
        /// <param name="disposing"><see langword="true"/> to dispose managed resources also; otherwise just unmanaged resouces</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (_noWxCache == null)
                {
                    if (_dispatchStub.IsValueCreated)
                    {
                        JitCache.Unmap(_dispatchStub.Value);
                    }

                    if (_dispatchLoop.IsValueCreated)
                    {
                        JitCache.Unmap(Marshal.GetFunctionPointerForDelegate(_dispatchLoop.Value));
                    }
                }

                _disposed = true;
            }
        }

        /// <summary>
        /// Frees resources used by the <see cref="TranslatorStubs"/> instance.
        /// </summary>
        ~TranslatorStubs()
        {
            Dispose(false);
        }

        /// <summary>
        /// Generates a <see cref="DispatchStub"/>.
        /// </summary>
        /// <returns>Generated <see cref="DispatchStub"/></returns>
        private nint GenerateDispatchStub()
        {
            List<int> branchToFallbackOffsets = [];

            CodeWriter writer = new();

            if (RuntimeInformation.ProcessArchitecture == Architecture.Arm64)
            {
                Assembler asm = new(writer);
                RegisterSaveRestore rsr = new((1u << 19) | (1u << 21) | (1u << 22), hasCall: true);

                rsr.WritePrologue(ref asm);

                Operand context = Register(19);
                asm.Mov(context, Register(0));

                // Load the target guest address from the native context.
                Operand guestAddress = Register(16);

                asm.LdrRiUn(guestAddress, context, NativeContext.GetDispatchAddressOffset());

                // Check if guest address is within range of the AddressTable.
                asm.And(Register(17), guestAddress, Const(~_functionTable.Mask));

                branchToFallbackOffsets.Add(writer.InstructionPointer);

                asm.Cbnz(Register(17), 0);

                Operand page = Register(17);
                Operand index = Register(21);
                Operand mask = Register(22);

                asm.Mov(page, (ulong)_functionTable.Base);

                for (int i = 0; i < _functionTable.Levels.Length; i++)
                {
                    ref AddressTableLevel level = ref _functionTable.Levels[i];

                    asm.Mov(mask, level.Mask >> level.Index);
                    asm.And(index, mask, guestAddress, ArmShiftType.Lsr, level.Index);

                    if (i < _functionTable.Levels.Length - 1)
                    {
                        asm.LdrRr(page, page, index, ArmExtensionType.Uxtx, true);

                        branchToFallbackOffsets.Add(writer.InstructionPointer);

                        asm.Cbz(page, 0);
                    }
                }

                asm.LdrRr(page, page, index, ArmExtensionType.Uxtx, true);

                rsr.WriteEpilogue(ref asm);

                asm.Br(page);

                foreach (int branchOffset in branchToFallbackOffsets)
                {
                    uint branchInst = writer.ReadInstructionAt(branchOffset);
                    Debug.Assert(writer.InstructionPointer > branchOffset);
                    writer.WriteInstructionAt(branchOffset, branchInst | ((uint)(writer.InstructionPointer - branchOffset) << 5));
                }

                // Fallback.
                asm.Mov(Register(0), Register(29));
                asm.Mov(Register(1), guestAddress);
                asm.Mov(Register(16), (ulong)_getFunctionAddress);
                asm.Blr(Register(16));
                asm.Mov(Register(16), Register(0));
                asm.Mov(Register(0), Register(19));

                rsr.WriteEpilogue(ref asm);

                asm.Br(Register(16));
            }
            else
            {
                throw new PlatformNotSupportedException();
            }

            return Map(writer.AsByteSpan());
        }

        /// <summary>
        /// Generates a <see cref="SlowDispatchStub"/>.
        /// </summary>
        /// <returns>Generated <see cref="SlowDispatchStub"/></returns>
        private nint GenerateSlowDispatchStub()
        {
            CodeWriter writer = new();

            if (RuntimeInformation.ProcessArchitecture == Architecture.Arm64)
            {
                Assembler asm = new(writer);
                RegisterSaveRestore rsr = new(1u << 19, hasCall: true);

                rsr.WritePrologue(ref asm);

                Operand context = Register(19);
                asm.Mov(context, Register(0));

                // Load the target guest address from the native context.
                asm.Mov(Register(0), Register(29));
                asm.LdrRiUn(Register(1), context, NativeContext.GetDispatchAddressOffset());
                asm.Mov(Register(16), (ulong)_getFunctionAddress);
                asm.Blr(Register(16));
                asm.Mov(Register(16), Register(0));
                asm.Mov(Register(0), Register(19));

                rsr.WriteEpilogue(ref asm);

                asm.Br(Register(16));
            }
            else
            {
                throw new PlatformNotSupportedException();
            }

            return Map(writer.AsByteSpan());
        }

        /// <summary>
        /// Emits code that syncs FP state before executing guest code, or returns it to normal.
        /// </summary>
        /// <param name="asm">Assembler</param>
        /// <param name="context">Pointer to the native context</param>
        /// <param name="tempRegister">First temporary register</param>
        /// <param name="tempRegister2">Second temporary register</param>
        /// <param name="enter">True if entering guest code, false otherwise</param>
        private static void EmitSyncFpContext(ref Assembler asm, Operand context, Operand tempRegister, Operand tempRegister2, bool enter)
        {
            if (enter)
            {
                EmitSwapFpFlags(ref asm, context, tempRegister, tempRegister2, NativeContext.GetFpFlagsOffset(), NativeContext.GetHostFpFlagsOffset());
            }
            else
            {
                EmitSwapFpFlags(ref asm, context, tempRegister, tempRegister2, NativeContext.GetHostFpFlagsOffset(), NativeContext.GetFpFlagsOffset());
            }
        }

        /// <summary>
        /// Swaps the FPCR and FPSR values with values stored in the native context.
        /// </summary>
        /// <param name="asm">Assembler</param>
        /// <param name="context">Pointer to the native context</param>
        /// <param name="tempRegister">First temporary register</param>
        /// <param name="tempRegister2">Second temporary register</param>
        /// <param name="loadOffset">Offset of the new flags that will be loaded</param>
        /// <param name="storeOffset">Offset where the current flags should be saved</param>
        private static void EmitSwapFpFlags(ref Assembler asm, Operand context, Operand tempRegister, Operand tempRegister2, int loadOffset, int storeOffset)
        {
            asm.MrsFpcr(tempRegister);
            asm.MrsFpsr(tempRegister2);
            asm.Orr(tempRegister, tempRegister, tempRegister2);

            asm.StrRiUn(tempRegister, context, storeOffset);

            asm.LdrRiUn(tempRegister, context, loadOffset);
            asm.MsrFpcr(tempRegister);
            asm.MsrFpsr(tempRegister2);
        }

        /// <summary>
        /// Generates a <see cref="DispatchLoop"/> function.
        /// </summary>
        /// <returns><see cref="DispatchLoop"/> function</returns>
        private DispatcherFunction GenerateDispatchLoop()
        {
            CodeWriter writer = new();

            if (RuntimeInformation.ProcessArchitecture == Architecture.Arm64)
            {
                Assembler asm = new(writer);
                RegisterSaveRestore rsr = new(1u << 19, hasCall: true);

                rsr.WritePrologue(ref asm);

                Operand context = Register(19);
                asm.Mov(context, Register(0));

                EmitSyncFpContext(ref asm, context, Register(16, OperandType.I32), Register(17, OperandType.I32), true);

                // Load the target guest address from the native context.
                Operand guestAddress = Register(16);

                asm.Mov(guestAddress, Register(1));

                int loopStartIndex = writer.InstructionPointer;

                asm.StrRiUn(guestAddress, context, NativeContext.GetDispatchAddressOffset());
                asm.Mov(Register(0), context);
                asm.Mov(Register(17), (ulong)DispatchStub);
                asm.Blr(Register(17));
                asm.Mov(guestAddress, Register(0));
                asm.Cbz(guestAddress, 16);
                asm.LdrRiUn(Register(17), context, NativeContext.GetRunningOffset());
                asm.Cbz(Register(17), 8);
                asm.B((loopStartIndex - writer.InstructionPointer) * 4);

                EmitSyncFpContext(ref asm, context, Register(16, OperandType.I32), Register(17, OperandType.I32), false);

                rsr.WriteEpilogue(ref asm);

                asm.Ret();
            }
            else
            {
                throw new PlatformNotSupportedException();
            }

            nint pointer = Map(writer.AsByteSpan());

            return Marshal.GetDelegateForFunctionPointer<DispatcherFunction>(pointer);
        }

        private nint Map(ReadOnlySpan<byte> code)
        {
            if (_noWxCache != null)
            {
                return _noWxCache.MapPageAligned(code);
            }
            else
            {
                return JitCache.Map(code);
            }
        }

        private static Operand Register(int register, OperandType type = OperandType.I64)
        {
            return new Operand(register, RegisterType.Integer, type);
        }

        private static Operand Const(ulong value)
        {
            return new(OperandKind.Constant, OperandType.I64, value);
        }
    }
}
