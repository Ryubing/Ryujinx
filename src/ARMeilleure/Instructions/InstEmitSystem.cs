using ARMeilleure.Decoders;
using ARMeilleure.IntermediateRepresentation;
using ARMeilleure.State;
using ARMeilleure.Translation;
using System;
using System.Reflection;

using static ARMeilleure.Instructions.InstEmitHelper;
using static ARMeilleure.IntermediateRepresentation.Operand.Factory;

namespace ARMeilleure.Instructions
{
    static partial class InstEmit
    {
        private const int DczSizeLog2 = 4; // Log2 size in words
        public const int DczSizeInBytes = 4 << DczSizeLog2;

        public static void Isb(ArmEmitterContext context)
        {
            // Execute as no-op.
        }

        public static void Mrs(ArmEmitterContext context)
        {
            OpCodeSystem op = (OpCodeSystem)context.CurrOp;

            MethodInfo info;

            switch (GetPackedId(op))
            {
                case 0b11_011_0000_0000_001:
                    info = typeof(NativeInterface).GetMethod(nameof(NativeInterface.GetCtrEl0));
                    break;
                case 0b11_011_0000_0000_111:
                    info = typeof(NativeInterface).GetMethod(nameof(NativeInterface.GetDczidEl0));
                    break;
                case 0b11_011_0100_0010_000:
                    EmitGetNzcv(context);
                    return;
                case 0b11_011_0100_0100_000:
                    EmitGetFpcr(context);
                    return;
                case 0b11_011_0100_0100_001:
                    EmitGetFpsr(context);
                    return;
                case 0b11_011_1101_0000_010:
                    EmitGetTpidrEl0(context);
                    return;
                case 0b11_011_1101_0000_011:
                    EmitGetTpidrroEl0(context);
                    return;
                case 0b11_011_1101_0000_101:
                    EmitGetTpidr2El0(context);
                    return;
                case 0b11_011_1110_0000_000:
                    info = typeof(NativeInterface).GetMethod(nameof(NativeInterface.GetCntfrqEl0));
                    break;
                case 0b11_011_1110_0000_001:
                    info = typeof(NativeInterface).GetMethod(nameof(NativeInterface.GetCntpctEl0));
                    break;
                case 0b11_011_1110_0000_010:
                    info = typeof(NativeInterface).GetMethod(nameof(NativeInterface.GetCntvctEl0));
                    break;

                default:
                    throw new NotImplementedException($"Unknown MRS 0x{op.RawOpCode:X8} at 0x{op.Address:X16}.");
            }

            SetIntOrZR(context, op.Rt, context.Call(info));
        }

        public static void Msr(ArmEmitterContext context)
        {
            OpCodeSystem op = (OpCodeSystem)context.CurrOp;

            switch (GetPackedId(op))
            {
                case 0b11_011_0100_0010_000:
                    EmitSetNzcv(context);
                    return;
                case 0b11_011_0100_0100_000:
                    EmitSetFpcr(context);
                    return;
                case 0b11_011_0100_0100_001:
                    EmitSetFpsr(context);
                    return;
                case 0b11_011_1101_0000_010:
                    EmitSetTpidrEl0(context);
                    return;
                case 0b11_011_1101_0000_101:
                    EmitSetTpidr2El0(context);
                    return;

                default:
                    throw new NotImplementedException($"Unknown MSR 0x{op.RawOpCode:X8} at 0x{op.Address:X16}.");
            }
        }

        public static void Nop(ArmEmitterContext context)
        {
            // Do nothing.
        }

        public static void Sys(ArmEmitterContext context)
        {
            // This instruction is used to do some operations on the CPU like cache invalidation,
            // address translation and the like.
            // We treat it as no-op here since we don't have any cache being emulated anyway.
            OpCodeSystem op = (OpCodeSystem)context.CurrOp;

            switch (GetPackedId(op))
            {
                case 0b11_011_0111_0100_001:
                    {
                        // DC ZVA
                        Operand t = GetIntOrZR(context, op.Rt);

                        for (long offset = 0; offset < DczSizeInBytes; offset += 8)
                        {
                            Operand address = context.Add(t, Const(offset));

                            InstEmitMemoryHelper.EmitStore(context, address, RegisterConsts.ZeroIndex, 3);
                        }

                        break;
                    }

                // No-op
                case 0b11_011_0111_1110_001: // DC CIVAC
                    break;

                case 0b11_011_0111_0101_001: // IC IVAU
                    Operand target = Register(op.Rt, RegisterType.Integer, OperandType.I64);
                    context.Call(typeof(NativeInterface).GetMethod(nameof(NativeInterface.InvalidateCacheLine)), target);
                    break;
            }
        }

        private static int GetPackedId(OpCodeSystem op)
        {
            int id;

            id = op.Op2 << 0;
            id |= op.CRm << 3;
            id |= op.CRn << 7;
            id |= op.Op1 << 11;
            id |= op.Op0 << 14;

            return id;
        }

        private static void EmitGetNzcv(ArmEmitterContext context)
        {
            OpCodeSystem op = (OpCodeSystem)context.CurrOp;

            Operand nzcv = context.ShiftLeft(GetFlag(PState.VFlag), Const((int)PState.VFlag));
            nzcv = context.BitwiseOr(nzcv, context.ShiftLeft(GetFlag(PState.CFlag), Const((int)PState.CFlag)));
            nzcv = context.BitwiseOr(nzcv, context.ShiftLeft(GetFlag(PState.ZFlag), Const((int)PState.ZFlag)));
            nzcv = context.BitwiseOr(nzcv, context.ShiftLeft(GetFlag(PState.NFlag), Const((int)PState.NFlag)));

            SetIntOrZR(context, op.Rt, nzcv);
        }

        private static void EmitGetFpcr(ArmEmitterContext context)
        {
            OpCodeSystem op = (OpCodeSystem)context.CurrOp;

            Operand fpcr = Const(0);

            for (int flag = 0; flag < RegisterConsts.FpFlagsCount; flag++)
            {
                if (FPCR.Mask.HasFlag((FPCR)(1u << flag)))
                {
                    fpcr = context.BitwiseOr(fpcr, context.ShiftLeft(GetFpFlag((FPState)flag), Const(flag)));
                }
            }

            SetIntOrZR(context, op.Rt, fpcr);
        }

        private static void EmitGetFpsr(ArmEmitterContext context)
        {
            OpCodeSystem op = (OpCodeSystem)context.CurrOp;

            context.SyncQcFlag();

            Operand fpsr = Const(0);

            for (int flag = 0; flag < RegisterConsts.FpFlagsCount; flag++)
            {
                if (FPSR.Mask.HasFlag((FPSR)(1u << flag)))
                {
                    fpsr = context.BitwiseOr(fpsr, context.ShiftLeft(GetFpFlag((FPState)flag), Const(flag)));
                }
            }

            SetIntOrZR(context, op.Rt, fpsr);
        }

        private static void EmitGetTpidrEl0(ArmEmitterContext context)
        {
            OpCodeSystem op = (OpCodeSystem)context.CurrOp;

            Operand nativeContext = context.LoadArgument(OperandType.I64, 0);

            Operand result = context.Load(OperandType.I64, context.Add(nativeContext, Const((ulong)NativeContext.GetTpidrEl0Offset())));

            SetIntOrZR(context, op.Rt, result);
        }

        private static void EmitGetTpidrroEl0(ArmEmitterContext context)
        {
            OpCodeSystem op = (OpCodeSystem)context.CurrOp;

            Operand nativeContext = context.LoadArgument(OperandType.I64, 0);

            Operand result = context.Load(OperandType.I64, context.Add(nativeContext, Const((ulong)NativeContext.GetTpidrroEl0Offset())));

            SetIntOrZR(context, op.Rt, result);
        }

        private static void EmitGetTpidr2El0(ArmEmitterContext context)
        {
            OpCodeSystem op = (OpCodeSystem)context.CurrOp;

            Operand nativeContext = context.LoadArgument(OperandType.I64, 0);

            Operand result = context.Load(OperandType.I64, context.Add(nativeContext, Const((ulong)NativeContext.GetTpidr2El0Offset())));

            SetIntOrZR(context, op.Rt, result);
        }

        private static void EmitSetNzcv(ArmEmitterContext context)
        {
            OpCodeSystem op = (OpCodeSystem)context.CurrOp;

            Operand nzcv = GetIntOrZR(context, op.Rt);
            nzcv = context.ConvertI64ToI32(nzcv);

            SetFlag(context, PState.VFlag, context.BitwiseAnd(context.ShiftRightUI(nzcv, Const((int)PState.VFlag)), Const(1)));
            SetFlag(context, PState.CFlag, context.BitwiseAnd(context.ShiftRightUI(nzcv, Const((int)PState.CFlag)), Const(1)));
            SetFlag(context, PState.ZFlag, context.BitwiseAnd(context.ShiftRightUI(nzcv, Const((int)PState.ZFlag)), Const(1)));
            SetFlag(context, PState.NFlag, context.BitwiseAnd(context.ShiftRightUI(nzcv, Const((int)PState.NFlag)), Const(1)));
        }

        private static void EmitSetFpcr(ArmEmitterContext context)
        {
            OpCodeSystem op = (OpCodeSystem)context.CurrOp;

            Operand fpcr = GetIntOrZR(context, op.Rt);
            fpcr = context.ConvertI64ToI32(fpcr);

            for (int flag = 0; flag < RegisterConsts.FpFlagsCount; flag++)
            {
                if (FPCR.Mask.HasFlag((FPCR)(1u << flag)))
                {
                    SetFpFlag(context, (FPState)flag, context.BitwiseAnd(context.ShiftRightUI(fpcr, Const(flag)), Const(1)));
                }
            }

            context.UpdateArmFpMode();
        }

        private static void EmitSetFpsr(ArmEmitterContext context)
        {
            OpCodeSystem op = (OpCodeSystem)context.CurrOp;

            context.ClearQcFlagIfModified();

            Operand fpsr = GetIntOrZR(context, op.Rt);
            fpsr = context.ConvertI64ToI32(fpsr);

            for (int flag = 0; flag < RegisterConsts.FpFlagsCount; flag++)
            {
                if (FPSR.Mask.HasFlag((FPSR)(1u << flag)))
                {
                    SetFpFlag(context, (FPState)flag, context.BitwiseAnd(context.ShiftRightUI(fpsr, Const(flag)), Const(1)));
                }
            }

            context.UpdateArmFpMode();
        }

        private static void EmitSetTpidrEl0(ArmEmitterContext context)
        {
            OpCodeSystem op = (OpCodeSystem)context.CurrOp;

            Operand value = GetIntOrZR(context, op.Rt);

            Operand nativeContext = context.LoadArgument(OperandType.I64, 0);

            context.Store(context.Add(nativeContext, Const((ulong)NativeContext.GetTpidrEl0Offset())), value);
        }

        private static void EmitSetTpidr2El0(ArmEmitterContext context)
        {
            OpCodeSystem op = (OpCodeSystem)context.CurrOp;

            Operand value = GetIntOrZR(context, op.Rt);

            Operand nativeContext = context.LoadArgument(OperandType.I64, 0);

            context.Store(context.Add(nativeContext, Const((ulong)NativeContext.GetTpidr2El0Offset())), value);
        }
    }
}
