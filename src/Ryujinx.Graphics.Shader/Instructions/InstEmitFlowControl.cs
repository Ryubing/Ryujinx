using Ryujinx.Graphics.Shader.Decoders;
using Ryujinx.Graphics.Shader.IntermediateRepresentation;
using Ryujinx.Graphics.Shader.Translation;
using System.Collections.Generic;
using System.Linq;
using static Ryujinx.Graphics.Shader.IntermediateRepresentation.OperandHelper;

namespace Ryujinx.Graphics.Shader.Instructions
{
    static partial class InstEmit
    {
        public static void Bra(EmitterContext context)
        {
            context.GetOp<InstBra>();

            EmitBranch(context, context.CurrBlock.Successors[^1].Address);
        }

        public static void Brk(EmitterContext context)
        {
            context.GetOp<InstBrk>();

            EmitBrkContSync(context);
        }

        public static void Brx(EmitterContext context)
        {
            InstBrx op = context.GetOp<InstBrx>();
            InstOp currOp = context.CurrOp;
            int startIndex = context.CurrBlock.HasNext() ? 1 : 0;

            if (context.CurrBlock.Successors.Count <= startIndex)
            {
                context.TranslatorContext.GpuAccessor.Log($"Failed to find targets for BRX instruction at 0x{currOp.Address:X}.");
                return;
            }

            int offset = (int)currOp.GetAbsoluteAddress();

            Operand address = context.IAdd(Register(op.SrcA, RegisterType.Gpr), Const(offset));

            IEnumerable<Block> targets = context.CurrBlock.Successors.Skip(startIndex);

            bool allTargetsSinglePred = true;
            int total = context.CurrBlock.Successors.Count - startIndex;
            int count = 0;

            foreach (Block target in targets.OrderBy(x => x.Address))
            {
                if (++count < total && (target.Predecessors.Count > 1 || target.Address <= context.CurrBlock.Address))
                {
                    allTargetsSinglePred = false;
                    break;
                }
            }

            if (allTargetsSinglePred)
            {
                // Chain blocks, each target block will check if the BRX target address
                // matches its own address, if not, it jumps to the next target which will do the same check,
                // until it reaches the last possible target, which executed unconditionally.
                // We can only do this if the BRX block is the only predecessor of all target blocks.
                // Additionally, this is not supported for blocks located before the current block,
                // since it will be too late to insert a label, but this is something that can be improved
                // in the future if necessary.

                IOrderedEnumerable<Block> sortedTargets = targets.OrderBy(x => x.Address);

                Block currentTarget = null;
                ulong firstTargetAddress = 0;

                foreach (Block nextTarget in sortedTargets)
                {
                    if (currentTarget != null)
                    {
                        if (currentTarget.Address != nextTarget.Address)
                        {
                            context.SetBrxTarget(currentTarget.Address, address, (int)currentTarget.Address, nextTarget.Address);
                        }
                    }
                    else
                    {
                        firstTargetAddress = nextTarget.Address;
                    }

                    currentTarget = nextTarget;
                }

                context.Branch(context.GetLabel(firstTargetAddress));
            }
            else
            {
                // Emit the branches sequentially.
                // This generates slightly worse code, but should work for all cases.

                IOrderedEnumerable<Block> sortedTargets = targets.OrderByDescending(x => x.Address);
                ulong lastTargetAddress = ulong.MaxValue;

                count = 0;

                foreach (Block target in sortedTargets)
                {
                    Operand label = context.GetLabel(target.Address);

                    if (++count < total)
                    {
                        if (target.Address != lastTargetAddress)
                        {
                            context.BranchIfTrue(label, context.ICompareEqual(address, Const((int)target.Address)));
                        }

                        lastTargetAddress = target.Address;
                    }
                    else
                    {
                        context.Branch(label);
                    }
                }
            }
        }

        public static void Cal(EmitterContext context)
        {
            context.GetOp<InstCal>();

            DecodedFunction function = context.Program.GetFunctionByAddress(context.CurrOp.GetAbsoluteAddress());

            if (function.IsCompilerGenerated)
            {
                switch (function.Type)
                {
                    case FunctionType.BuiltInFSIBegin:
                        context.FSIBegin();
                        break;
                    case FunctionType.BuiltInFSIEnd:
                        context.FSIEnd();
                        break;
                }
            }
            else
            {
                context.Call(function.Id, false);
            }
        }

        public static void Cont(EmitterContext context)
        {
            context.GetOp<InstCont>();

            EmitBrkContSync(context);
        }

        public static void Exit(EmitterContext context)
        {
            InstExit op = context.GetOp<InstExit>();

            if (context.IsNonMain)
            {
                context.TranslatorContext.GpuAccessor.Log("Invalid exit on non-main function.");
                return;
            }

            if (op.Ccc == Ccc.T)
            {
                if (context.PrepareForReturn())
                {
                    context.Return();
                }
            }
            else
            {
                Operand cond = GetCondition(context, op.Ccc, IrConsts.False);

                // If the condition is always false, we don't need to do anything.
                if (cond.Type != OperandType.Constant || cond.Value != IrConsts.False)
                {
                    Operand lblSkip = Label();
                    context.BranchIfFalse(lblSkip, cond);

                    if (context.PrepareForReturn())
                    {
                        context.Return();
                    }

                    context.MarkLabel(lblSkip);
                }
            }
        }

        public static void Kil(EmitterContext context)
        {
            context.GetOp<InstKil>();

            context.Discard();
        }

        public static void Pbk(EmitterContext context)
        {
            context.GetOp<InstPbk>();

            EmitPbkPcntSsy(context);
        }

        public static void Pcnt(EmitterContext context)
        {
            context.GetOp<InstPcnt>();

            EmitPbkPcntSsy(context);
        }

        public static void Ret(EmitterContext context)
        {
            context.GetOp<InstRet>();

            if (context.IsNonMain)
            {
                context.Return();
            }
            else
            {
                context.TranslatorContext.GpuAccessor.Log("Invalid return on main function.");
            }
        }

        public static void Ssy(EmitterContext context)
        {
            context.GetOp<InstSsy>();

            EmitPbkPcntSsy(context);
        }

        public static void Sync(EmitterContext context)
        {
            context.GetOp<InstSync>();

            EmitBrkContSync(context);
        }

        private static void EmitPbkPcntSsy(EmitterContext context)
        {
            Dictionary<Block, Operand> consumers = context.CurrBlock.PushOpCodes.First(x => x.Op.Address == context.CurrOp.Address).Consumers;

            foreach (KeyValuePair<Block, Operand> kv in consumers)
            {
                Block consumerBlock = kv.Key;
                Operand local = kv.Value;

                int id = consumerBlock.SyncTargets[context.CurrOp.Address].PushOpId;

                context.Copy(local, Const(id));
            }
        }

        private static void EmitBrkContSync(EmitterContext context)
        {
            Dictionary<ulong, SyncTarget> targets = context.CurrBlock.SyncTargets;

            if (targets.Count == 1)
            {
                // If we have only one target, then the SSY/PBK is basically
                // a branch, we can produce better codegen for this case.
                EmitBranch(context, targets.Values.First().PushOpInfo.Op.GetAbsoluteAddress());
            }
            else
            {
                // TODO: Support CC here as well (condition).
                foreach (SyncTarget target in targets.Values)
                {
                    PushOpInfo pushOpInfo = target.PushOpInfo;

                    Operand label = context.GetLabel(pushOpInfo.Op.GetAbsoluteAddress());
                    Operand local = pushOpInfo.Consumers[context.CurrBlock];

                    context.BranchIfTrue(label, context.ICompareEqual(local, Const(target.PushOpId)));
                }
            }
        }

        private static void EmitBranch(EmitterContext context, ulong address)
        {
            InstOp op = context.CurrOp;
            InstConditional opCond = new(op.RawOpCode);

            // If we're branching to the next instruction, then the branch
            // is useless and we can ignore it.
            if (address == op.Address + 8)
            {
                return;
            }

            Operand label = context.GetLabel(address);

            Operand pred = Register(opCond.Pred, RegisterType.Predicate);

            if (opCond.Ccc != Ccc.T)
            {
                Operand cond = GetCondition(context, opCond.Ccc);

                if (opCond.Pred == RegisterConsts.PredicateTrueIndex)
                {
                    pred = cond;
                }
                else if (opCond.PredInv)
                {
                    pred = context.BitwiseAnd(context.BitwiseNot(pred), cond);
                }
                else
                {
                    pred = context.BitwiseAnd(pred, cond);
                }

                context.BranchIfTrue(label, pred);
            }
            else if (opCond.Pred == RegisterConsts.PredicateTrueIndex)
            {
                context.Branch(label);
            }
            else if (opCond.PredInv)
            {
                context.BranchIfFalse(label, pred);
            }
            else
            {
                context.BranchIfTrue(label, pred);
            }
        }
    }
}
