namespace Ryujinx.Cpu.LightningJit.Arm32.Target.Arm64
{
    static class InstEmitVfpMove
    {
        public static void Vsel(CodeGenContext context, uint rd, uint rn, uint rm, uint cc, uint size)
        {
            bool singleRegs = size != 3;
            uint cond = (cc << 2) | ((cc & 2) ^ ((cc << 1) & 2));

            using ScopedRegister rnReg = InstEmitNeonCommon.MoveScalarToSide(context, rn, singleRegs);
            using ScopedRegister rmReg = InstEmitNeonCommon.MoveScalarToSide(context, rm, singleRegs);

            using ScopedRegister tempRegister = InstEmitNeonCommon.PickSimdRegister(context.RegisterAllocator, rnReg, rmReg);

            context.Arm64Assembler.FcselFloat(tempRegister.Operand, rnReg.Operand, cond, rmReg.Operand, size ^ 2u);

            InstEmitNeonCommon.InsertResult(context, tempRegister.Operand, rd, singleRegs);
        }
    }
}
