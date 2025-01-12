using Ryujinx.Cpu.LightningJit.CodeGen;
using Ryujinx.Cpu.LightningJit.CodeGen.Arm64;
using System;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Ryujinx.Cpu.LightningJit.Arm32.Target.Arm64
{
    static class InstEmitSystem
    {
        private delegate void SoftwareInterruptHandler(ulong address, int imm);
        private delegate ulong Get64();
        private delegate bool GetBool();

        private const int SpIndex = 31;

        public static void Bkpt(CodeGenContext context, uint imm)
        {
            context.AddPendingBkpt(imm);

            context.Arm64Assembler.B(0);
        }

        public static void Cps(CodeGenContext context, uint imod, uint m, uint a, uint i, uint f, uint mode)
        {
            // NOP in user mode.
        }

        public static void Dbg(CodeGenContext context, uint option)
        {
            // NOP in ARMv8.
        }

        public static void Hlt(CodeGenContext context, uint imm)
        {
        }

        public static void Mcr(CodeGenContext context, uint encoding, uint coproc, uint opc1, uint rt, uint crn, uint crm, uint opc2)
        {
            if (coproc != 15 || opc1 != 0)
            {
                Udf(context, encoding, 0);

                return;
            }

            Operand ctx = Register(context.RegisterAllocator.FixedContextRegister);
            Operand rtOperand = InstEmitCommon.GetInputGpr(context, rt);

            switch (crn)
            {
                case 13: // Process and Thread Info.
                    if (crm == 0)
                    {
                        switch (opc2)
                        {
                            case 2:
                                context.Arm64Assembler.StrRiUn(rtOperand, ctx, NativeContextOffsets.TpidrEl0Offset);
                                return;
                        }
                    }
                    break;
            }
        }

        public static void Mcrr(CodeGenContext context, uint encoding, uint coproc, uint opc1, uint rt, uint crm)
        {
            if (coproc != 15 || opc1 != 0)
            {
                Udf(context, encoding, 0);

                return;
            }

            // We don't have any system register that needs to be modified using a 64-bit value.
        }

        public static void Mrc(CodeGenContext context, uint encoding, uint coproc, uint opc1, uint rt, uint crn, uint crm, uint opc2)
        {
            if (coproc != 15 || opc1 != 0)
            {
                Udf(context, encoding, 0);

                return;
            }

            Operand ctx = Register(context.RegisterAllocator.FixedContextRegister);
            Operand rtOperand = InstEmitCommon.GetInputGpr(context, rt);
            bool hasValue = false;

            using ScopedRegister tempRegister = context.RegisterAllocator.AllocateTempGprRegisterScoped();
            Operand dest = rt == RegisterUtils.PcRegister ? tempRegister.Operand : rtOperand;

            switch (crn)
            {
                case 13: // Process and Thread Info.
                    if (crm == 0)
                    {
                        switch (opc2)
                        {
                            case 2:
                                context.Arm64Assembler.LdrRiUn(dest, ctx, NativeContextOffsets.TpidrEl0Offset);
                                hasValue = true;
                                break;
                            case 3:
                                context.Arm64Assembler.LdrRiUn(dest, ctx, NativeContextOffsets.TpidrroEl0Offset);
                                hasValue = true;
                                break;
                        }
                    }
                    break;
            }

            if (rt == RegisterUtils.PcRegister)
            {
                context.Arm64Assembler.MsrNzcv(dest);
                context.SetNzcvModified();
            }
            else if (!hasValue)
            {
                context.Arm64Assembler.Mov(dest, 0u);
            }
        }

        public static void Mrrc(CodeGenContext context, uint encoding, uint coproc, uint opc1, uint rt, uint rt2, uint crm)
        {
            if (coproc != 15)
            {
                Udf(context, encoding, 0);

                return;
            }

            switch (crm)
            {
                case 14:
                    switch (opc1)
                    {
                        case 0:
                            context.AddPendingReadCntpct(rt, rt2);
                            context.Arm64Assembler.B(0);
                            return;
                    }
                    break;
            }

            // Unsupported system register.
            context.Arm64Assembler.Mov(InstEmitCommon.GetOutputGpr(context, rt), 0u);
            context.Arm64Assembler.Mov(InstEmitCommon.GetOutputGpr(context, rt2), 0u);
        }

        public static void Mrs(CodeGenContext context, uint rd, bool r)
        {
            Operand rdOperand = InstEmitCommon.GetOutputGpr(context, rd);

            if (r)
            {
                // Reads SPSR, unpredictable in user mode.

                context.Arm64Assembler.Mov(rdOperand, 0u);
            }
            else
            {
                Operand ctx = Register(context.RegisterAllocator.FixedContextRegister);

                using ScopedRegister tempRegister = context.RegisterAllocator.AllocateTempGprRegisterScoped();

                context.Arm64Assembler.LdrRiUn(tempRegister.Operand, ctx, NativeContextOffsets.FlagsBaseOffset);

                // Copy GE flags to destination register.
                context.Arm64Assembler.Ubfx(rdOperand, tempRegister.Operand, 16, 4);

                // Insert Q flag.
                context.Arm64Assembler.And(tempRegister.Operand, tempRegister.Operand, InstEmitCommon.Const(1 << 27));
                context.Arm64Assembler.Orr(rdOperand, rdOperand, tempRegister.Operand);

                // Insert NZCV flags.
                context.Arm64Assembler.MrsNzcv(tempRegister.Operand);
                context.Arm64Assembler.Orr(rdOperand, rdOperand, tempRegister.Operand);

                // All other flags can't be accessed in user mode or have "unknown" values.
            }
        }

        public static void MrsBr(CodeGenContext context, uint rd, uint m1, bool r)
        {
            Operand rdOperand = InstEmitCommon.GetOutputGpr(context, rd);

            // Reads banked register, unpredictable in user mode.

            context.Arm64Assembler.Mov(rdOperand, 0u);
        }

        public static void MsrBr(CodeGenContext context, uint rn, uint m1, bool r)
        {
            // Writes banked register, unpredictable in user mode.
        }

        public static void MsrI(CodeGenContext context, uint imm, uint mask, bool r)
        {
            if (r)
            {
                // Writes SPSR, unpredictable in user mode.
            }
            else
            {
                Operand ctx = Register(context.RegisterAllocator.FixedContextRegister);

                using ScopedRegister tempRegister = context.RegisterAllocator.AllocateTempGprRegisterScoped();
                using ScopedRegister tempRegister2 = context.RegisterAllocator.AllocateTempGprRegisterScoped();

                context.Arm64Assembler.LdrRiUn(tempRegister.Operand, ctx, NativeContextOffsets.FlagsBaseOffset);

                if ((mask & 2) != 0)
                {
                    // Endian flag.

                    context.Arm64Assembler.Mov(tempRegister2.Operand, (imm >> 9) & 1);
                    context.Arm64Assembler.Bfi(tempRegister.Operand, tempRegister2.Operand, 9, 1);
                }

                if ((mask & 4) != 0)
                {
                    // GE flags.

                    context.Arm64Assembler.Mov(tempRegister2.Operand, (imm >> 16) & 0xf);
                    context.Arm64Assembler.Bfi(tempRegister.Operand, tempRegister2.Operand, 16, 4);
                }

                if ((mask & 8) != 0)
                {
                    // NZCVQ flags.

                    context.Arm64Assembler.Mov(tempRegister2.Operand, (imm >> 27) & 0x1f);
                    context.Arm64Assembler.Bfi(tempRegister.Operand, tempRegister2.Operand, 27, 5);
                    context.Arm64Assembler.Mov(tempRegister2.Operand, (imm >> 28) & 0xf);
                    InstEmitCommon.RestoreNzcvFlags(context, tempRegister2.Operand);
                    context.SetNzcvModified();
                }
            }
        }

        public static void MsrR(CodeGenContext context, uint rn, uint mask, bool r)
        {
            Operand rnOperand = InstEmitCommon.GetInputGpr(context, rn);

            if (r)
            {
                // Writes SPSR, unpredictable in user mode.
            }
            else
            {
                Operand ctx = Register(context.RegisterAllocator.FixedContextRegister);

                using ScopedRegister tempRegister = context.RegisterAllocator.AllocateTempGprRegisterScoped();
                using ScopedRegister tempRegister2 = context.RegisterAllocator.AllocateTempGprRegisterScoped();

                context.Arm64Assembler.LdrRiUn(tempRegister.Operand, ctx, NativeContextOffsets.FlagsBaseOffset);

                if ((mask & 2) != 0)
                {
                    // Endian flag.

                    context.Arm64Assembler.Lsr(tempRegister2.Operand, rnOperand, InstEmitCommon.Const(9));
                    context.Arm64Assembler.Bfi(tempRegister.Operand, tempRegister2.Operand, 9, 1);
                }

                if ((mask & 4) != 0)
                {
                    // GE flags.

                    context.Arm64Assembler.Lsr(tempRegister2.Operand, rnOperand, InstEmitCommon.Const(16));
                    context.Arm64Assembler.Bfi(tempRegister.Operand, tempRegister2.Operand, 16, 4);
                }

                if ((mask & 8) != 0)
                {
                    // NZCVQ flags.

                    context.Arm64Assembler.Lsr(tempRegister2.Operand, rnOperand, InstEmitCommon.Const(27));
                    context.Arm64Assembler.Bfi(tempRegister.Operand, tempRegister2.Operand, 27, 5);
                    context.Arm64Assembler.Lsr(tempRegister2.Operand, rnOperand, InstEmitCommon.Const(28));
                    InstEmitCommon.RestoreNzcvFlags(context, tempRegister2.Operand);
                    context.SetNzcvModified();
                }
            }
        }

        public static void Setend(CodeGenContext context, bool e)
        {
            Operand ctx = Register(context.RegisterAllocator.FixedContextRegister);

            using ScopedRegister tempRegister = context.RegisterAllocator.AllocateTempGprRegisterScoped();

            context.Arm64Assembler.LdrRiUn(tempRegister.Operand, ctx, NativeContextOffsets.FlagsBaseOffset);

            if (e)
            {
                context.Arm64Assembler.Orr(tempRegister.Operand, tempRegister.Operand, InstEmitCommon.Const(1 << 9));
            }
            else
            {
                context.Arm64Assembler.Bfc(tempRegister.Operand, 9, 1);
            }

            context.Arm64Assembler.StrRiUn(tempRegister.Operand, ctx, NativeContextOffsets.FlagsBaseOffset);
        }

        public static void Svc(CodeGenContext context, uint imm)
        {
            context.AddPendingSvc(imm);
            context.Arm64Assembler.B(0);
        }

        public static void Udf(CodeGenContext context, uint encoding, uint imm)
        {
            context.AddPendingUdf(encoding);
            context.Arm64Assembler.B(0);
        }

        public static void PrivilegedInstruction(CodeGenContext context, uint encoding)
        {
            Udf(context, encoding, 0);
        }

        private static nint GetBkptHandlerPtr()
        {
            return Marshal.GetFunctionPointerForDelegate<SoftwareInterruptHandler>(NativeInterface.Break);
        }

        private static nint GetSvcHandlerPtr()
        {
            return Marshal.GetFunctionPointerForDelegate<SoftwareInterruptHandler>(NativeInterface.SupervisorCall);
        }

        private static nint GetUdfHandlerPtr()
        {
            return Marshal.GetFunctionPointerForDelegate<SoftwareInterruptHandler>(NativeInterface.Undefined);
        }

        private static nint GetCntpctEl0Ptr()
        {
            return Marshal.GetFunctionPointerForDelegate<Get64>(NativeInterface.GetCntpctEl0);
        }

        private static nint CheckSynchronizationPtr()
        {
            return Marshal.GetFunctionPointerForDelegate<GetBool>(NativeInterface.CheckSynchronization);
        }

        public static bool NeedsCall(InstName name)
        {
            // All instructions that might do a host call should be included here.
            // That is required to reserve space on the stack for caller saved registers.

            return name == InstName.Mrrc;
        }

        public static bool NeedsCallSkipContext(InstName name)
        {
            // All instructions that might do a host call should be included here.
            // That is required to reserve space on the stack for caller saved registers.

            switch (name)
            {
                case InstName.Mcr:
                case InstName.Mrc:
                case InstName.Svc:
                case InstName.Udf:
                    return true;
            }

            return false;
        }

        public static void WriteBkpt(CodeWriter writer, RegisterAllocator regAlloc, TailMerger tailMerger, int spillBaseOffset, uint pc, uint imm)
        {
            Assembler asm = new(writer);

            WriteCall(ref asm, regAlloc, GetBkptHandlerPtr(), skipContext: true, spillBaseOffset, null, pc, imm);
            WriteSyncPoint(writer, ref asm, regAlloc, tailMerger, spillBaseOffset);
        }

        public static void WriteSvc(CodeWriter writer, RegisterAllocator regAlloc, TailMerger tailMerger, int spillBaseOffset, uint pc, uint svcId)
        {
            Assembler asm = new(writer);

            WriteCall(ref asm, regAlloc, GetSvcHandlerPtr(), skipContext: true, spillBaseOffset, null, pc, svcId);
            WriteSyncPoint(writer, ref asm, regAlloc, tailMerger, spillBaseOffset);
        }

        public static void WriteUdf(CodeWriter writer, RegisterAllocator regAlloc, TailMerger tailMerger, int spillBaseOffset, uint pc, uint imm)
        {
            Assembler asm = new(writer);

            WriteCall(ref asm, regAlloc, GetUdfHandlerPtr(), skipContext: true, spillBaseOffset, null, pc, imm);
            WriteSyncPoint(writer, ref asm, regAlloc, tailMerger, spillBaseOffset);
        }

        public static void WriteReadCntpct(CodeWriter writer, RegisterAllocator regAlloc, int spillBaseOffset, int rt, int rt2)
        {
            Assembler asm = new(writer);

            uint resultMask = (1u << rt) | (1u << rt2);
            int tempRegister = 0;

            while ((resultMask & (1u << tempRegister)) != 0 && tempRegister < 32)
            {
                tempRegister++;
            }

            Debug.Assert(tempRegister < 32);

            WriteSpill(ref asm, regAlloc, resultMask, skipContext: false, spillBaseOffset, tempRegister);

            Operand rn = Register(tempRegister);

            asm.Mov(rn, (ulong)GetCntpctEl0Ptr());
            asm.Blr(rn);

            if (rt != rt2)
            {
                asm.Lsr(Register(rt2), Register(0), InstEmitCommon.Const(32));
            }

            asm.Mov(Register(rt, OperandType.I32), Register(0, OperandType.I32)); // Zero-extend.

            WriteFill(ref asm, regAlloc, resultMask, skipContext: false, spillBaseOffset, tempRegister);
        }

        public static void WriteSyncPoint(
            CodeWriter writer,
            ref Assembler asm,
            RegisterAllocator regAlloc,
            TailMerger tailMerger,
            int spillBaseOffset,
            Action storeToContext = null,
            Action loadFromContext = null)
        {
            int tempRegister = regAlloc.AllocateTempGprRegister();

            Operand rt = Register(tempRegister, OperandType.I32);

            asm.LdrRiUn(rt, Register(regAlloc.FixedContextRegister), NativeContextOffsets.CounterOffset);

            int branchIndex = writer.InstructionPointer;
            asm.Cbnz(rt, 0);

            storeToContext?.Invoke();
            WriteSpill(ref asm, regAlloc, 1u << tempRegister, skipContext: true, spillBaseOffset, tempRegister);

            Operand rn = Register(tempRegister == 0 ? 1 : 0);

            asm.Mov(rn, (ulong)CheckSynchronizationPtr());
            asm.Blr(rn);

            tailMerger.AddConditionalZeroReturn(writer, asm, Register(0, OperandType.I32));

            WriteFill(ref asm, regAlloc, 1u << tempRegister, skipContext: true, spillBaseOffset, tempRegister);
            loadFromContext?.Invoke();

            asm.LdrRiUn(rt, Register(regAlloc.FixedContextRegister), NativeContextOffsets.CounterOffset);

            uint branchInst = writer.ReadInstructionAt(branchIndex);
            writer.WriteInstructionAt(branchIndex, branchInst | (((uint)(writer.InstructionPointer - branchIndex) & 0x7ffff) << 5));

            asm.Sub(rt, rt, new Operand(OperandKind.Constant, OperandType.I32, 1));
            asm.StrRiUn(rt, Register(regAlloc.FixedContextRegister), NativeContextOffsets.CounterOffset);

            regAlloc.FreeTempGprRegister(tempRegister);
        }

        private static void WriteCall(
            ref Assembler asm,
            RegisterAllocator regAlloc,
            nint funcPtr,
            bool skipContext,
            int spillBaseOffset,
            int? resultRegister,
            params ReadOnlySpan<ulong> callArgs)
        {
            uint resultMask = 0u;

            if (resultRegister.HasValue)
            {
                resultMask = 1u << resultRegister.Value;
            }

            int tempRegister = callArgs.Length;

            if (resultRegister.HasValue && tempRegister == resultRegister.Value)
            {
                tempRegister++;
            }

            WriteSpill(ref asm, regAlloc, resultMask, skipContext, spillBaseOffset, tempRegister);

            // We only support up to 7 arguments right now.
            // ABI defines the first 8 integer arguments to be passed on registers X0-X7.
            // We need at least one register to put the function address on, so that reduces the number of
            // registers we can use for that by one.

            Debug.Assert(callArgs.Length < 8);

            for (int index = 0; index < callArgs.Length; index++)
            {
                asm.Mov(Register(index), callArgs[index]);
            }

            Operand rn = Register(tempRegister);

            asm.Mov(rn, (ulong)funcPtr);
            asm.Blr(rn);

            if (resultRegister.HasValue && resultRegister.Value != 0)
            {
                asm.Mov(Register(resultRegister.Value), Register(0));
            }

            WriteFill(ref asm, regAlloc, resultMask, skipContext, spillBaseOffset, tempRegister);
        }

        private static void WriteSpill(ref Assembler asm, RegisterAllocator regAlloc, uint exceptMask, bool skipContext, int spillOffset, int tempRegister)
        {
            if (skipContext)
            {
                InstEmitFlow.WriteSpillSkipContext(ref asm, regAlloc, spillOffset);
            }
            else
            {
                WriteSpillOrFill(ref asm, regAlloc, exceptMask, spillOffset, tempRegister, spill: true);
            }
        }

        private static void WriteFill(ref Assembler asm, RegisterAllocator regAlloc, uint exceptMask, bool skipContext, int spillOffset, int tempRegister)
        {
            if (skipContext)
            {
                InstEmitFlow.WriteFillSkipContext(ref asm, regAlloc, spillOffset);
            }
            else
            {
                WriteSpillOrFill(ref asm, regAlloc, exceptMask, spillOffset, tempRegister, spill: false);
            }
        }

        private static void WriteSpillOrFill(
            ref Assembler asm,
            RegisterAllocator regAlloc,
            uint exceptMask,
            int spillOffset,
            int tempRegister,
            bool spill)
        {
            uint gprMask = regAlloc.UsedGprsMask & ~(AbiConstants.GprCalleeSavedRegsMask | exceptMask);

            if (!spill)
            {
                // We must reload the status register before reloading the GPRs,
                // since we might otherwise trash one of them by using it as temp register.

                Operand rt = Register(tempRegister, OperandType.I32);

                asm.LdrRiUn(rt, Register(SpIndex), spillOffset + BitOperations.PopCount(gprMask) * 8);
                asm.MsrNzcv(rt);
            }

            while (gprMask != 0)
            {
                int reg = BitOperations.TrailingZeroCount(gprMask);

                if (reg < 31 && (gprMask & (2u << reg)) != 0 && spillOffset < RegisterSaveRestore.Encodable9BitsOffsetLimit)
                {
                    if (spill)
                    {
                        asm.StpRiUn(Register(reg), Register(reg + 1), Register(SpIndex), spillOffset);
                    }
                    else
                    {
                        asm.LdpRiUn(Register(reg), Register(reg + 1), Register(SpIndex), spillOffset);
                    }

                    gprMask &= ~(3u << reg);
                    spillOffset += 16;
                }
                else
                {
                    if (spill)
                    {
                        asm.StrRiUn(Register(reg), Register(SpIndex), spillOffset);
                    }
                    else
                    {
                        asm.LdrRiUn(Register(reg), Register(SpIndex), spillOffset);
                    }

                    gprMask &= ~(1u << reg);
                    spillOffset += 8;
                }
            }

            if (spill)
            {
                Operand rt = Register(tempRegister, OperandType.I32);

                asm.MrsNzcv(rt);
                asm.StrRiUn(rt, Register(SpIndex), spillOffset);
            }

            spillOffset += 8;

            if ((spillOffset & 8) != 0)
            {
                spillOffset += 8;
            }

            uint fpSimdMask = regAlloc.UsedFpSimdMask;

            while (fpSimdMask != 0)
            {
                int reg = BitOperations.TrailingZeroCount(fpSimdMask);

                if (reg < 31 && (fpSimdMask & (2u << reg)) != 0 && spillOffset < RegisterSaveRestore.Encodable9BitsOffsetLimit)
                {
                    if (spill)
                    {
                        asm.StpRiUn(Register(reg, OperandType.V128), Register(reg + 1, OperandType.V128), Register(SpIndex), spillOffset);
                    }
                    else
                    {
                        asm.LdpRiUn(Register(reg, OperandType.V128), Register(reg + 1, OperandType.V128), Register(SpIndex), spillOffset);
                    }

                    fpSimdMask &= ~(3u << reg);
                    spillOffset += 32;
                }
                else
                {
                    if (spill)
                    {
                        asm.StrRiUn(Register(reg, OperandType.V128), Register(SpIndex), spillOffset);
                    }
                    else
                    {
                        asm.LdrRiUn(Register(reg, OperandType.V128), Register(SpIndex), spillOffset);
                    }

                    fpSimdMask &= ~(1u << reg);
                    spillOffset += 16;
                }
            }
        }

        public static Operand Register(int register, OperandType type = OperandType.I64)
        {
            return new Operand(register, RegisterType.Integer, type);
        }
    }
}
