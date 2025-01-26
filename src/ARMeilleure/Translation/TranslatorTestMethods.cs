using ARMeilleure.CodeGen.X86;
using ARMeilleure.IntermediateRepresentation;
using ARMeilleure.State;
using System;
using System.Runtime.InteropServices;
using static ARMeilleure.IntermediateRepresentation.Operand.Factory;

namespace ARMeilleure.Translation
{
    public static class TranslatorTestMethods
    {
        public delegate int FpFlagsPInvokeTest(nint managedMethod);

        private static bool SetPlatformFtz(EmitterContext context, bool ftz)
        {
            if (Optimizations.UseSse2)
            {
                Operand mxcsr = context.AddIntrinsicInt(Intrinsic.X86Stmxcsr);

                if (ftz)
                {
                    mxcsr = context.BitwiseOr(mxcsr, Const((int)(Mxcsr.Ftz | Mxcsr.Um | Mxcsr.Dm)));
                }
                else
                {
                    mxcsr = context.BitwiseAnd(mxcsr, Const(~(int)Mxcsr.Ftz));
                }

                context.AddIntrinsicNoRet(Intrinsic.X86Ldmxcsr, mxcsr);

                return true;
            }
            else if (Optimizations.UseAdvSimd)
            {
                Operand fpcr = context.AddIntrinsicInt(Intrinsic.Arm64MrsFpcr);

                if (ftz)
                {
                    fpcr = context.BitwiseOr(fpcr, Const((int)FPCR.Fz));
                }
                else
                {
                    fpcr = context.BitwiseAnd(fpcr, Const(~(int)FPCR.Fz));
                }

                context.AddIntrinsicNoRet(Intrinsic.Arm64MsrFpcr, fpcr);

                return true;
            }
            else
            {
                return false;
            }
        }

        private static Operand FpBitsToInt(EmitterContext context, Operand fp)
        {
            Operand vec = context.VectorInsert(context.VectorZero(), fp, 0);
            return context.VectorExtract(OperandType.I32, vec, 0);
        }

        public static FpFlagsPInvokeTest GenerateFpFlagsPInvokeTest()
        {
            EmitterContext context = new();

            Operand methodAddress = context.Copy(context.LoadArgument(OperandType.I64, 0));

            // Verify that default dotnet fp state does not flush to zero.
            // This is required for SoftFloat to function.

            // Denormal + zero != 0

            Operand denormal = ConstF(BitConverter.Int32BitsToSingle(1)); // 1.40129846432e-45
            Operand zeroF = ConstF(0f);
            Operand zero = Const(0);

            Operand result = context.Add(zeroF, denormal);

            // Must not be zero.

            Operand correct1Label = Label();

            context.BranchIfFalse(correct1Label, context.ICompareEqual(FpBitsToInt(context, result), zero));

            context.Return(Const(1));

            context.MarkLabel(correct1Label);

            // Set flush to zero flag. If unsupported by the backend, just return true.

            if (!SetPlatformFtz(context, true))
            {
                context.Return(Const(0));
            }

            // Denormal + zero == 0

            Operand resultFz = context.Add(zeroF, denormal);

            // Must equal zero.

            Operand correct2Label = Label();

            context.BranchIfTrue(correct2Label, context.ICompareEqual(FpBitsToInt(context, resultFz), zero));

            SetPlatformFtz(context, false);

            context.Return(Const(2));

            context.MarkLabel(correct2Label);

            // Call a managed method. This method should not change Fz state.

            context.Call(methodAddress, OperandType.None);

            // Denormal + zero == 0

            Operand resultFz2 = context.Add(zeroF, denormal);

            // Must equal zero.

            Operand correct3Label = Label();

            context.BranchIfTrue(correct3Label, context.ICompareEqual(FpBitsToInt(context, resultFz2), zero));

            SetPlatformFtz(context, false);

            context.Return(Const(3));

            context.MarkLabel(correct3Label);

            // Success.

            SetPlatformFtz(context, false);

            context.Return(Const(0));

            // Compile and return the function.

            ControlFlowGraph cfg = context.GetControlFlowGraph();

            OperandType[] argTypes = [OperandType.I64];

            return Compiler.Compile(cfg, argTypes, OperandType.I32, CompilerOptions.HighCq, RuntimeInformation.ProcessArchitecture).Map<FpFlagsPInvokeTest>();
        }
    }
}
