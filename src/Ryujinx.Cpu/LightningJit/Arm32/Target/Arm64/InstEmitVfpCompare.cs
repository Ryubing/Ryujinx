using Ryujinx.Cpu.LightningJit.CodeGen;
using System.Diagnostics;

namespace Ryujinx.Cpu.LightningJit.Arm32.Target.Arm64
{
    static class InstEmitVfpCompare
    {
        public static void VcmpI(CodeGenContext context, uint cond, uint rd, uint size)
        {
            EmitVcmpVcmpe(context, cond, rd, 0, size, zero: true, e: false);
        }

        public static void VcmpR(CodeGenContext context, uint cond, uint rd, uint rm, uint size)
        {
            EmitVcmpVcmpe(context, cond, rd, rm, size, zero: false, e: false);
        }

        public static void VcmpeI(CodeGenContext context, uint cond, uint rd, uint size)
        {
            EmitVcmpVcmpe(context, cond, rd, 0, size, zero: true, e: true);
        }

        public static void VcmpeR(CodeGenContext context, uint cond, uint rd, uint rm, uint size)
        {
            EmitVcmpVcmpe(context, cond, rd, rm, size, zero: false, e: true);
        }

        private static void EmitVcmpVcmpe(CodeGenContext context, uint cond, uint rd, uint rm, uint size, bool zero, bool e)
        {
            Debug.Assert(size == 1 || size == 2 || size == 3);

            bool singleRegs = size != 3;
            uint ftype = size ^ 2u;
            uint opc = zero ? 1u : 0u;

            using ScopedRegister rdReg = InstEmitNeonCommon.MoveScalarToSide(context, rd, singleRegs);
            ScopedRegister rmReg;
            Operand rmOrZero;

            if (zero)
            {
                rmReg = default;
                rmOrZero = new Operand(0, RegisterType.Vector, OperandType.V128);
            }
            else
            {
                rmReg = InstEmitNeonCommon.MoveScalarToSide(context, rm, singleRegs);
                rmOrZero = rmReg.Operand;
            }

            using ScopedRegister oldFlags = context.RegisterAllocator.AllocateTempGprRegisterScoped();

            bool canPeepholeOptimize = CanFuseVcmpVmrs(context, cond);
            if (!canPeepholeOptimize)
            {
                InstEmitCommon.GetCurrentFlags(context, oldFlags.Operand);
            }

            if (e)
            {
                context.Arm64Assembler.FcmpeFloat(rdReg.Operand, rmOrZero, opc, ftype);
            }
            else
            {
                context.Arm64Assembler.FcmpFloat(rdReg.Operand, rmOrZero, opc, ftype);
            }

            // Save result flags from the FCMP operation on FPSCR register, then restore the old flags if needed.

            WriteUpdateFpsrNzcv(context);

            if (!canPeepholeOptimize)
            {
                InstEmitCommon.RestoreNzcvFlags(context, oldFlags.Operand);
            }

            if (!zero)
            {
                rmReg.Dispose();
            }
        }

        private static void WriteUpdateFpsrNzcv(CodeGenContext context)
        {
            using ScopedRegister fpsrRegister = context.RegisterAllocator.AllocateTempGprRegisterScoped();
            using ScopedRegister flagsRegister = context.RegisterAllocator.AllocateTempGprRegisterScoped();

            Operand ctx = InstEmitSystem.Register(context.RegisterAllocator.FixedContextRegister);

            context.Arm64Assembler.LdrRiUn(fpsrRegister.Operand, ctx, NativeContextOffsets.FpFlagsBaseOffset);

            InstEmitCommon.GetCurrentFlags(context, flagsRegister.Operand);

            context.Arm64Assembler.Bfi(fpsrRegister.Operand, flagsRegister.Operand, 28, 4);
            context.Arm64Assembler.StrRiUn(fpsrRegister.Operand, ctx, NativeContextOffsets.FpFlagsBaseOffset);
        }

        private static bool CanFuseVcmpVmrs(CodeGenContext context, uint vcmpCond)
        {
            // Conditions might be different for the VCMP and VMRS instructions if they are inside a IT block,
            // we don't bother to check right now, so just always skip if inside an IT block.
            if (context.InITBlock)
            {
                return false;
            }

            InstInfo nextInfo = context.PeekNextInstruction();

            // We're looking for a VMRS instructions.
            if (nextInfo.Name != InstName.Vmrs)
            {
                return false;
            }

            // Conditions must match.
            if (vcmpCond != (nextInfo.Encoding >> 28))
            {
                return false;
            }

            // Reg must be 1, Rt must be PC indicating VMRS to PSTATE.NZCV.
            if (((nextInfo.Encoding >> 16) & 0xf) != 1 || ((nextInfo.Encoding >> 12) & 0xf) != RegisterUtils.PcRegister)
            {
                return false;
            }

            context.SetSkipNextInstruction();

            return true;
        }
    }
}
