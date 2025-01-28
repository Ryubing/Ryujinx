using ARMeilleure.Decoders;
using ARMeilleure.IntermediateRepresentation;
using ARMeilleure.State;
using ARMeilleure.Translation;

using static ARMeilleure.Instructions.InstEmitHelper;
using static ARMeilleure.Instructions.InstEmitMemoryExHelper;
using static ARMeilleure.IntermediateRepresentation.Operand.Factory;

namespace ARMeilleure.Instructions
{
    static partial class InstEmit32
    {
        public static void Clrex(ArmEmitterContext context)
        {
            EmitClearExclusive(context);
        }

        public static void Csdb(ArmEmitterContext context)
        {
            // Execute as no-op.
        }

        public static void Dmb(ArmEmitterContext context) => EmitBarrier(context);

        public static void Dsb(ArmEmitterContext context) => EmitBarrier(context);

        public static void Ldrex(ArmEmitterContext context)
        {
            EmitExLoadOrStore(context, WordSizeLog2, AccessType.LoadZx | AccessType.Exclusive);
        }

        public static void Ldrexb(ArmEmitterContext context)
        {
            EmitExLoadOrStore(context, ByteSizeLog2, AccessType.LoadZx | AccessType.Exclusive);
        }

        public static void Ldrexd(ArmEmitterContext context)
        {
            EmitExLoadOrStore(context, DWordSizeLog2, AccessType.LoadZx | AccessType.Exclusive);
        }

        public static void Ldrexh(ArmEmitterContext context)
        {
            EmitExLoadOrStore(context, HWordSizeLog2, AccessType.LoadZx | AccessType.Exclusive);
        }

        public static void Lda(ArmEmitterContext context)
        {
            EmitExLoadOrStore(context, WordSizeLog2, AccessType.LoadZx | AccessType.Ordered);
        }

        public static void Ldab(ArmEmitterContext context)
        {
            EmitExLoadOrStore(context, ByteSizeLog2, AccessType.LoadZx | AccessType.Ordered);
        }

        public static void Ldaex(ArmEmitterContext context)
        {
            EmitExLoadOrStore(context, WordSizeLog2, AccessType.LoadZx | AccessType.Exclusive | AccessType.Ordered);
        }

        public static void Ldaexb(ArmEmitterContext context)
        {
            EmitExLoadOrStore(context, ByteSizeLog2, AccessType.LoadZx | AccessType.Exclusive | AccessType.Ordered);
        }

        public static void Ldaexd(ArmEmitterContext context)
        {
            EmitExLoadOrStore(context, DWordSizeLog2, AccessType.LoadZx | AccessType.Exclusive | AccessType.Ordered);
        }

        public static void Ldaexh(ArmEmitterContext context)
        {
            EmitExLoadOrStore(context, HWordSizeLog2, AccessType.LoadZx | AccessType.Exclusive | AccessType.Ordered);
        }

        public static void Ldah(ArmEmitterContext context)
        {
            EmitExLoadOrStore(context, HWordSizeLog2, AccessType.LoadZx | AccessType.Ordered);
        }

        // Stores.

        public static void Strex(ArmEmitterContext context)
        {
            EmitExLoadOrStore(context, WordSizeLog2, AccessType.Store | AccessType.Exclusive);
        }

        public static void Strexb(ArmEmitterContext context)
        {
            EmitExLoadOrStore(context, ByteSizeLog2, AccessType.Store | AccessType.Exclusive);
        }

        public static void Strexd(ArmEmitterContext context)
        {
            EmitExLoadOrStore(context, DWordSizeLog2, AccessType.Store | AccessType.Exclusive);
        }

        public static void Strexh(ArmEmitterContext context)
        {
            EmitExLoadOrStore(context, HWordSizeLog2, AccessType.Store | AccessType.Exclusive);
        }

        public static void Stl(ArmEmitterContext context)
        {
            EmitExLoadOrStore(context, WordSizeLog2, AccessType.Store | AccessType.Ordered);
        }

        public static void Stlb(ArmEmitterContext context)
        {
            EmitExLoadOrStore(context, ByteSizeLog2, AccessType.Store | AccessType.Ordered);
        }

        public static void Stlex(ArmEmitterContext context)
        {
            EmitExLoadOrStore(context, WordSizeLog2, AccessType.Store | AccessType.Exclusive | AccessType.Ordered);
        }

        public static void Stlexb(ArmEmitterContext context)
        {
            EmitExLoadOrStore(context, ByteSizeLog2, AccessType.Store | AccessType.Exclusive | AccessType.Ordered);
        }

        public static void Stlexd(ArmEmitterContext context)
        {
            EmitExLoadOrStore(context, DWordSizeLog2, AccessType.Store | AccessType.Exclusive | AccessType.Ordered);
        }

        public static void Stlexh(ArmEmitterContext context)
        {
            EmitExLoadOrStore(context, HWordSizeLog2, AccessType.Store | AccessType.Exclusive | AccessType.Ordered);
        }

        public static void Stlh(ArmEmitterContext context)
        {
            EmitExLoadOrStore(context, HWordSizeLog2, AccessType.Store | AccessType.Ordered);
        }

        private static void EmitExLoadOrStore(ArmEmitterContext context, int size, AccessType accType)
        {
            IOpCode32MemEx op = (IOpCode32MemEx)context.CurrOp;

            Operand address = context.Copy(GetIntA32(context, op.Rn));

            bool exclusive = (accType & AccessType.Exclusive) != 0;
            bool ordered = (accType & AccessType.Ordered) != 0;

            if ((accType & AccessType.Load) != 0)
            {
                if (ordered)
                {
                    EmitBarrier(context);
                }

                if (size == DWordSizeLog2)
                {
                    // Keep loads atomic - make the call to get the whole region and then decompose it into parts
                    // for the registers.

                    Operand value = EmitLoadExclusive(context, address, exclusive, size);

                    Operand valueLow = context.ConvertI64ToI32(value);

                    valueLow = context.ZeroExtend32(OperandType.I64, valueLow);

                    Operand valueHigh = context.ShiftRightUI(value, Const(32));

                    Operand lblBigEndian = Label();
                    Operand lblEnd = Label();

                    context.BranchIfTrue(lblBigEndian, GetFlag(PState.EFlag));

                    SetIntA32(context, op.Rt, valueLow);
                    SetIntA32(context, op.Rt2, valueHigh);

                    context.Branch(lblEnd);

                    context.MarkLabel(lblBigEndian);

                    SetIntA32(context, op.Rt2, valueLow);
                    SetIntA32(context, op.Rt, valueHigh);

                    context.MarkLabel(lblEnd);
                }
                else
                {
                    SetIntA32(context, op.Rt, EmitLoadExclusive(context, address, exclusive, size));
                }
            }
            else
            {
                if (size == DWordSizeLog2)
                {
                    // Split the result into 2 words (based on endianness)

                    Operand lo = context.ZeroExtend32(OperandType.I64, GetIntA32(context, op.Rt));
                    Operand hi = context.ZeroExtend32(OperandType.I64, GetIntA32(context, op.Rt2));

                    Operand lblBigEndian = Label();
                    Operand lblEnd = Label();

                    context.BranchIfTrue(lblBigEndian, GetFlag(PState.EFlag));

                    Operand leResult = context.BitwiseOr(lo, context.ShiftLeft(hi, Const(32)));
                    EmitStoreExclusive(context, address, leResult, exclusive, size, op.Rd, a32: true);

                    context.Branch(lblEnd);

                    context.MarkLabel(lblBigEndian);

                    Operand beResult = context.BitwiseOr(hi, context.ShiftLeft(lo, Const(32)));
                    EmitStoreExclusive(context, address, beResult, exclusive, size, op.Rd, a32: true);

                    context.MarkLabel(lblEnd);
                }
                else
                {
                    Operand value = context.ZeroExtend32(OperandType.I64, GetIntA32(context, op.Rt));
                    EmitStoreExclusive(context, address, value, exclusive, size, op.Rd, a32: true);
                }

                if (ordered)
                {
                    EmitBarrier(context);
                }
            }
        }

        private static void EmitBarrier(ArmEmitterContext context)
        {
            // Note: This barrier is most likely not necessary, and probably
            // doesn't make any difference since we need to do a ton of stuff
            // (software MMU emulation) to read or write anything anyway.
        }
    }
}
