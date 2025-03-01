// https://github.com/MicrosoftDocs/cpp-docs/blob/master/docs/build/exception-handling-x64.md

using ARMeilleure.CodeGen.Unwinding;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace ARMeilleure.Translation.Cache
{
    static partial class JitUnwindWindows
    {
        private const int MaxUnwindCodesArraySize = 32; // Must be an even value.

        private struct RuntimeFunction
        {
            public uint BeginAddress;
            public uint EndAddress;
            public uint UnwindData;
        }

        private struct UnwindInfo
        {
            public byte VersionAndFlags;
            public byte SizeOfProlog;
            public byte CountOfUnwindCodes;
            public byte FrameRegister;
            public unsafe fixed ushort UnwindCodes[MaxUnwindCodesArraySize];
        }

        private enum UnwindOp
        {
            PushNonvol = 0,
            AllocLarge = 1,
            AllocSmall = 2,
            SetFpreg = 3,
            SaveNonvol = 4,
            SaveNonvolFar = 5,
            SaveXmm128 = 8,
            SaveXmm128Far = 9,
            PushMachframe = 10,
        }

        private unsafe delegate RuntimeFunction* GetRuntimeFunctionCallback(ulong controlPc, nint context);

        [LibraryImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static unsafe partial bool RtlInstallFunctionTableCallback(
            ulong tableIdentifier,
            ulong baseAddress,
            uint length,
            GetRuntimeFunctionCallback callback,
            nint context,
            [MarshalAs(UnmanagedType.LPWStr)] string outOfProcessCallbackDll);

        [LibraryImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static unsafe partial bool RtlDeleteFunctionTable(
            ulong tableIdentifier);

        private static GetRuntimeFunctionCallback _getRuntimeFunctionCallback;

        private static int _sizeOfRuntimeFunction;

        private unsafe static RuntimeFunction* _runtimeFunction;

        private unsafe static UnwindInfo* _unwindInfo;

        public static void InstallFunctionTableHandler(nint codeCachePointer, uint codeCacheLength, nint workBufferPtr)
        {
            ulong codeCachePtr = (ulong)codeCachePointer.ToInt64();

            _sizeOfRuntimeFunction = Marshal.SizeOf<RuntimeFunction>();

            bool result;

            unsafe
            {
                _runtimeFunction = (RuntimeFunction*)workBufferPtr;

                _unwindInfo = (UnwindInfo*)(workBufferPtr + _sizeOfRuntimeFunction);

                _getRuntimeFunctionCallback = new GetRuntimeFunctionCallback(FunctionTableHandler);

                result = RtlInstallFunctionTableCallback(
                    codeCachePtr | 3,
                    codeCachePtr,
                    codeCacheLength,
                    _getRuntimeFunctionCallback,
                    codeCachePointer,
                    null);
            }

            if (!result)
            {
                throw new InvalidOperationException("Failure installing function table callback.");
            }
        }

        public static void RemoveFunctionTableHandler(nint codeCachePointer)
        {
            ulong codeCachePtr = (ulong)codeCachePointer.ToInt64();

            bool result;

            unsafe
            {
                result = RtlDeleteFunctionTable(codeCachePtr | 3);
            }

            if (!result)
            {
                throw new InvalidOperationException("Failure removing function table callback.");
            }
        }

        private static unsafe RuntimeFunction* FunctionTableHandler(ulong controlPc, nint context)
        {
            int offset = (int)((long)controlPc - context.ToInt64());

            if (!JitCache.TryFind(offset, out CacheEntry funcEntry, out _))
            {
                return null; // Not found.
            }

            CodeGen.Unwinding.UnwindInfo unwindInfo = funcEntry.UnwindInfo;

            int codeIndex = 0;

            for (int index = unwindInfo.PushEntries.Length - 1; index >= 0; index--)
            {
                UnwindPushEntry entry = unwindInfo.PushEntries[index];

                switch (entry.PseudoOp)
                {
                    case UnwindPseudoOp.SaveXmm128:
                        {
                            int stackOffset = entry.StackOffsetOrAllocSize;

                            Debug.Assert(stackOffset % 16 == 0);

                            if (stackOffset <= 0xFFFF0)
                            {
                                _unwindInfo->UnwindCodes[codeIndex++] = PackUnwindOp(UnwindOp.SaveXmm128, entry.PrologOffset, entry.RegIndex);
                                _unwindInfo->UnwindCodes[codeIndex++] = (ushort)(stackOffset / 16);
                            }
                            else
                            {
                                _unwindInfo->UnwindCodes[codeIndex++] = PackUnwindOp(UnwindOp.SaveXmm128Far, entry.PrologOffset, entry.RegIndex);
                                _unwindInfo->UnwindCodes[codeIndex++] = (ushort)(stackOffset >> 0);
                                _unwindInfo->UnwindCodes[codeIndex++] = (ushort)(stackOffset >> 16);
                            }

                            break;
                        }

                    case UnwindPseudoOp.AllocStack:
                        {
                            int allocSize = entry.StackOffsetOrAllocSize;

                            Debug.Assert(allocSize % 8 == 0);

                            if (allocSize <= 128)
                            {
                                _unwindInfo->UnwindCodes[codeIndex++] = PackUnwindOp(UnwindOp.AllocSmall, entry.PrologOffset, (allocSize / 8) - 1);
                            }
                            else if (allocSize <= 0x7FFF8)
                            {
                                _unwindInfo->UnwindCodes[codeIndex++] = PackUnwindOp(UnwindOp.AllocLarge, entry.PrologOffset, 0);
                                _unwindInfo->UnwindCodes[codeIndex++] = (ushort)(allocSize / 8);
                            }
                            else
                            {
                                _unwindInfo->UnwindCodes[codeIndex++] = PackUnwindOp(UnwindOp.AllocLarge, entry.PrologOffset, 1);
                                _unwindInfo->UnwindCodes[codeIndex++] = (ushort)(allocSize >> 0);
                                _unwindInfo->UnwindCodes[codeIndex++] = (ushort)(allocSize >> 16);
                            }

                            break;
                        }

                    case UnwindPseudoOp.PushReg:
                        {
                            _unwindInfo->UnwindCodes[codeIndex++] = PackUnwindOp(UnwindOp.PushNonvol, entry.PrologOffset, entry.RegIndex);

                            break;
                        }

                    default:
                        throw new NotImplementedException($"({nameof(entry.PseudoOp)} = {entry.PseudoOp})");
                }
            }

            Debug.Assert(codeIndex <= MaxUnwindCodesArraySize);

            _unwindInfo->VersionAndFlags = 1; // Flags: The function has no handler.
            _unwindInfo->SizeOfProlog = (byte)unwindInfo.PrologSize;
            _unwindInfo->CountOfUnwindCodes = (byte)codeIndex;
            _unwindInfo->FrameRegister = 0;

            _runtimeFunction->BeginAddress = (uint)funcEntry.Offset;
            _runtimeFunction->EndAddress = (uint)(funcEntry.Offset + funcEntry.Size);
            _runtimeFunction->UnwindData = (uint)_sizeOfRuntimeFunction;

            return _runtimeFunction;
        }

        private static ushort PackUnwindOp(UnwindOp op, int prologOffset, int opInfo)
        {
            return (ushort)(prologOffset | ((int)op << 8) | (opInfo << 12));
        }
    }
}
