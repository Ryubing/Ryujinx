using Ryujinx.Graphics.Shader.Decoders;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Ryujinx.Graphics.Shader.Translation
{
    static class FunctionMatch
    {
        private static readonly IPatternTreeNode[] _fsiGetAddressTree = PatternTrees.GetFsiGetAddress();
        private static readonly IPatternTreeNode[] _fsiGetAddressV2Tree = PatternTrees.GetFsiGetAddressV2();
        private static readonly IPatternTreeNode[] _fsiIsLastWarpThreadPatternTree = PatternTrees.GetFsiIsLastWarpThread();
        private static readonly IPatternTreeNode[] _fsiBeginPatternTree = PatternTrees.GetFsiBeginPattern();
        private static readonly IPatternTreeNode[] _fsiEndPatternTree = PatternTrees.GetFsiEndPattern();

        public static void RunPass(DecodedProgram program)
        {
            byte[] externalRegs = new byte[4];
            bool hasGetAddress = false;

            foreach (DecodedFunction function in program)
            {
                if (function == program.MainFunction)
                {
                    continue;
                }

                int externalReg4 = 0;

                TreeNode[] functionTree = BuildTree(function.Blocks);

                if (Matches(_fsiGetAddressTree, functionTree))
                {
                    externalRegs[1] = functionTree[0].GetRd();
                    externalRegs[2] = functionTree[2].GetRd();
                    externalRegs[3] = functionTree[1].GetRd();
                    externalReg4 = functionTree[3].GetRd();
                }
                else if (Matches(_fsiGetAddressV2Tree, functionTree))
                {
                    externalRegs[1] = functionTree[2].GetRd();
                    externalRegs[2] = functionTree[1].GetRd();
                    externalRegs[3] = functionTree[0].GetRd();
                    externalReg4 = functionTree[3].GetRd();
                }

                // Ensure the register allocation is valid.
                // If so, then we have a match.
                if (externalRegs[1] != externalRegs[2] &&
                    externalRegs[2] != externalRegs[3] &&
                    externalRegs[1] != externalRegs[3] &&
                    externalRegs[1] + 1 != externalRegs[2] &&
                    externalRegs[1] + 1 != externalRegs[3] &&
                    externalRegs[1] + 1 == externalReg4 &&
                    externalRegs[2] != RegisterConsts.RegisterZeroIndex &&
                    externalRegs[3] != RegisterConsts.RegisterZeroIndex &&
                    externalReg4 != RegisterConsts.RegisterZeroIndex)
                {
                    hasGetAddress = true;
                    function.Type = FunctionType.Unused;
                    break;
                }
            }

            foreach (DecodedFunction function in program)
            {
                if (function.IsCompilerGenerated || function == program.MainFunction)
                {
                    continue;
                }

                if (hasGetAddress)
                {
                    TreeNode[] functionTree = BuildTree(function.Blocks);

                    if (MatchesFsi(_fsiBeginPatternTree, program, function, functionTree, externalRegs))
                    {
                        function.Type = FunctionType.BuiltInFSIBegin;
                        continue;
                    }
                    else if (MatchesFsi(_fsiEndPatternTree, program, function, functionTree, externalRegs))
                    {
                        function.Type = FunctionType.BuiltInFSIEnd;
                        continue;
                    }
                }
            }
        }

        private readonly struct TreeNodeUse
        {
            public TreeNode Node { get; }
            public int Index { get; }
            public bool Inverted { get; }

            private TreeNodeUse(int index, bool inverted, TreeNode node)
            {
                Index = index;
                Inverted = inverted;
                Node = node;
            }

            public TreeNodeUse(int index, TreeNode node) : this(index, false, node)
            {
            }

            public TreeNodeUse Flip()
            {
                return new TreeNodeUse(Index, !Inverted, Node);
            }
        }

        private enum TreeNodeType : byte
        {
            Op,
            Label,
        }

        private class TreeNode
        {
            public readonly InstOp Op;
            public readonly List<TreeNodeUse> Uses;
            public TreeNodeType Type { get; }
            public byte Order { get; }

            public TreeNode(byte order)
            {
                Type = TreeNodeType.Label;
                Order = order;
            }

            public TreeNode(InstOp op, byte order)
            {
                Op = op;
                Uses = [];
                Type = TreeNodeType.Op;
                Order = order;
            }

            public byte GetPd()
            {
                return (byte)((Op.RawOpCode >> 3) & 7);
            }

            public byte GetRd()
            {
                return (byte)Op.RawOpCode;
            }
        }

        private static TreeNode[] BuildTree(Block[] blocks)
        {
            List<TreeNode> nodes = [];

            Dictionary<ulong, TreeNode> labels = new();

            TreeNodeUse[] predDefs = new TreeNodeUse[RegisterConsts.PredsCount];
            TreeNodeUse[] gprDefs = new TreeNodeUse[RegisterConsts.GprsCount];

            void DefPred(byte predIndex, int index, TreeNode node)
            {
                if (predIndex != RegisterConsts.PredicateTrueIndex)
                {
                    predDefs[predIndex] = new TreeNodeUse(index, node);
                }
            }

            void DefGpr(byte regIndex, int index, TreeNode node)
            {
                if (regIndex != RegisterConsts.RegisterZeroIndex)
                {
                    gprDefs[regIndex] = new TreeNodeUse(index, node);
                }
            }

            TreeNodeUse UsePred(byte predIndex, bool predInv)
            {
                if (predIndex != RegisterConsts.PredicateTrueIndex)
                {
                    TreeNodeUse use = predDefs[predIndex];

                    if (use.Node != null)
                    {
                        nodes.Remove(use.Node);
                    }
                    else
                    {
                        use = new TreeNodeUse(-(predIndex + 2), null);
                    }

                    return predInv ? use.Flip() : use;
                }

                return new TreeNodeUse(-1, null);
            }

            TreeNodeUse UseGpr(byte regIndex)
            {
                if (regIndex != RegisterConsts.RegisterZeroIndex)
                {
                    TreeNodeUse use = gprDefs[regIndex];

                    if (use.Node != null)
                    {
                        nodes.Remove(use.Node);
                    }
                    else
                    {
                        use = new TreeNodeUse(-(regIndex + 2), null);
                    }

                    return use;
                }

                return new TreeNodeUse(-1, null);
            }

            byte order = 0;

            for (int index = 0; index < blocks.Length; index++)
            {
                Block block = blocks[index];

                if (block.Predecessors.Count > 1)
                {
                    TreeNode label = new(order++);
                    nodes.Add(label);
                    labels.Add(block.Address, label);
                }

                for (int opIndex = 0; opIndex < block.OpCodes.Count; opIndex++)
                {
                    InstOp op = block.OpCodes[opIndex];

                    TreeNode node = new(op, IsOrderDependant(op.Name) ? order : (byte)0);

                    // Add uses.

                    if (!op.Props.HasFlag(InstProps.NoPred))
                    {
                        byte predIndex = (byte)((op.RawOpCode >> 16) & 7);
                        bool predInv = (op.RawOpCode & 0x80000) != 0;
                        node.Uses.Add(UsePred(predIndex, predInv));
                    }

                    if (op.Props.HasFlag(InstProps.Ps))
                    {
                        byte predIndex = (byte)((op.RawOpCode >> 39) & 7);
                        bool predInv = (op.RawOpCode & 0x40000000000) != 0;
                        node.Uses.Add(UsePred(predIndex, predInv));
                    }

                    if (op.Props.HasFlag(InstProps.Ra))
                    {
                        byte ra = (byte)(op.RawOpCode >> 8);
                        node.Uses.Add(UseGpr(ra));
                    }

                    if ((op.Props & (InstProps.Rb | InstProps.Rb2)) != 0)
                    {
                        byte rb = op.Props.HasFlag(InstProps.Rb2) ? (byte)op.RawOpCode : (byte)(op.RawOpCode >> 20);
                        node.Uses.Add(UseGpr(rb));
                    }

                    if (op.Props.HasFlag(InstProps.Rc))
                    {
                        byte rc = (byte)(op.RawOpCode >> 39);
                        node.Uses.Add(UseGpr(rc));
                    }

                    if (op.Name == InstName.Bra && labels.TryGetValue(op.GetAbsoluteAddress(), out TreeNode label))
                    {
                        node.Uses.Add(new TreeNodeUse(0, label));
                    }

                    // Make definitions.

                    int defIndex = 0;

                    InstProps pdType = op.Props & InstProps.PdMask;

                    if (pdType != 0)
                    {
                        int bit = pdType switch
                        {
                            InstProps.Pd => 3,
                            InstProps.LPd => 48,
                            InstProps.SPd => 30,
                            InstProps.TPd => 51,
                            InstProps.VPd => 45,
                            _ => throw new InvalidOperationException($"Table has unknown predicate destination {pdType}."),
                        };

                        byte predIndex = (byte)((op.RawOpCode >> bit) & 7);
                        DefPred(predIndex, defIndex++, node);
                    }

                    if (op.Props.HasFlag(InstProps.Rd))
                    {
                        byte rd = (byte)op.RawOpCode;
                        DefGpr(rd, defIndex++, node);
                    }

                    nodes.Add(node);
                }
            }

            return nodes.ToArray();
        }

        private static bool IsOrderDependant(InstName name)
        {
            switch (name)
            {
                case InstName.Atom:
                case InstName.AtomCas:
                case InstName.Atoms:
                case InstName.AtomsCas:
                case InstName.Ld:
                case InstName.Ldg:
                case InstName.Ldl:
                case InstName.Lds:
                case InstName.Suatom:
                case InstName.SuatomB:
                case InstName.SuatomB2:
                case InstName.SuatomCas:
                case InstName.SuatomCasB:
                case InstName.Suld:
                case InstName.SuldB:
                case InstName.SuldD:
                case InstName.SuldDB:
                    return true;
            }

            return false;
        }

        private interface IPatternTreeNode
        {
            List<PatternTreeNodeUse> Uses { get; }
            InstName Name { get; }
            TreeNodeType Type { get; }
            byte Order { get; }
            bool IsImm { get; }
            bool Matches(in InstOp opInfo);
        }

        private readonly struct PatternTreeNodeUse
        {
            public IPatternTreeNode Node { get; }
            public int Index { get; }
            public bool Inverted { get; }
            public PatternTreeNodeUse Inv => new(Index, !Inverted, Node);

            private PatternTreeNodeUse(int index, bool inverted, IPatternTreeNode node)
            {
                Index = index;
                Inverted = inverted;
                Node = node;
            }

            public PatternTreeNodeUse(int index, IPatternTreeNode node) : this(index, false, node)
            {
            }
        }

        private class PatternTreeNode<T> : IPatternTreeNode
        {
            public List<PatternTreeNodeUse> Uses { get; }
            private readonly Func<T, bool> _match;

            public InstName Name { get; }
            public TreeNodeType Type { get; }
            public byte Order { get; }
            public bool IsImm { get; }
            public PatternTreeNodeUse Out => new(0, this);

            public PatternTreeNode(InstName name, Func<T, bool> match, TreeNodeType type = TreeNodeType.Op, byte order = 0, bool isImm = false)
            {
                Name = name;
                _match = match;
                Type = type;
                Order = order;
                IsImm = isImm;
                Uses = [];
            }

            public PatternTreeNode<T> Use(PatternTreeNodeUse use)
            {
                Uses.Add(use);
                return this;
            }

            public PatternTreeNodeUse OutAt(int index)
            {
                return new PatternTreeNodeUse(index, this);
            }

            public bool Matches(in InstOp opInfo)
            {
                if (opInfo.Name != Name)
                {
                    return false;
                }

                ulong rawOp = opInfo.RawOpCode;
                T op = Unsafe.As<ulong, T>(ref rawOp);

                if (!_match(op))
                {
                    return false;
                }

                return true;
            }
        }

        private static bool MatchesFsi(
            IPatternTreeNode[] pattern,
            DecodedProgram program,
            DecodedFunction function,
            TreeNode[] functionTree,
            byte[] externalRegs)
        {
            if (function.Blocks.Length == 0)
            {
                return false;
            }

            InstOp callOp = function.Blocks[0].GetLastOp();

            if (callOp.Name != InstName.Cal)
            {
                return false;
            }

            DecodedFunction callTarget = program.GetFunctionByAddress(callOp.GetAbsoluteAddress());
            TreeNode[] callTargetTree;

            if (callTarget == null || !Matches(_fsiIsLastWarpThreadPatternTree, callTargetTree = BuildTree(callTarget.Blocks)))
            {
                return false;
            }

            externalRegs[0] = callTargetTree[0].GetPd();

            if (Matches(pattern, functionTree, externalRegs))
            {
                callTarget.RemoveCaller(function);
                return true;
            }

            return false;
        }

        private static bool Matches(IPatternTreeNode[] pTree, TreeNode[] cTree, byte[] externalRegs = null)
        {
            if (pTree.Length != cTree.Length)
            {
                return false;
            }

            for (int index = 0; index < pTree.Length; index++)
            {
                if (!Matches(pTree[index], cTree[index], externalRegs))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool Matches(IPatternTreeNode pTreeNode, TreeNode cTreeNode, byte[] externalRegs)
        {
            if (!pTreeNode.Matches(in cTreeNode.Op) ||
                pTreeNode.Type != cTreeNode.Type ||
                pTreeNode.Order != cTreeNode.Order ||
                pTreeNode.IsImm != cTreeNode.Op.Props.HasFlag(InstProps.Ib))
            {
                return false;
            }

            if (pTreeNode.Type == TreeNodeType.Op)
            {
                if (pTreeNode.Uses.Count != cTreeNode.Uses.Count)
                {
                    return false;
                }

                for (int index = 0; index < pTreeNode.Uses.Count; index++)
                {
                    PatternTreeNodeUse pUse = pTreeNode.Uses[index];
                    TreeNodeUse cUse = cTreeNode.Uses[index];

                    if (pUse.Index <= -2)
                    {
                        if (externalRegs[-pUse.Index - 2] != (-cUse.Index - 2))
                        {
                            return false;
                        }
                    }
                    else if (pUse.Index != cUse.Index)
                    {
                        return false;
                    }

                    if (pUse.Inverted != cUse.Inverted || (pUse.Node == null) != (cUse.Node == null))
                    {
                        return false;
                    }

                    if (pUse.Node != null && !Matches(pUse.Node, cUse.Node, externalRegs))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private static class PatternTrees
        {
            public static IPatternTreeNode[] GetFsiGetAddress()
            {
                PatternTreeNodeUse affinityValue = S2r(SReg.Affinity).Use(PT).Out;
                PatternTreeNodeUse orderingTicketValue = S2r(SReg.OrderingTicket).Use(PT).Out;

                return
                [
                    Iscadd(cc: true, 2, 0, 404)
                        .Use(PT)
                        .Use(Iscadd(cc: false, 8)
                            .Use(PT)
                            .Use(Lop32i(LogicOp.And, 0xff)
                                .Use(PT)
                                .Use(affinityValue).Out)
                            .Use(Lop32i(LogicOp.And, 0xff)
                                .Use(PT)
                                .Use(orderingTicketValue).Out).Out),
                    ShrU32W(16)
                        .Use(PT)
                        .Use(orderingTicketValue),
                    Iadd32i(0x200)
                        .Use(PT)
                        .Use(Lop32i(LogicOp.And, 0xfe00)
                            .Use(PT)
                            .Use(orderingTicketValue).Out),
                    Iadd(x: true, 0, 405).Use(PT).Use(RZ),
                    Ret().Use(PT)
                ];
            }

            public static IPatternTreeNode[] GetFsiGetAddressV2()
            {
                PatternTreeNodeUse affinityValue = S2r(SReg.Affinity).Use(PT).Out;
                PatternTreeNodeUse orderingTicketValue = S2r(SReg.OrderingTicket).Use(PT).Out;

                return
                [
                    ShrU32W(16)
                        .Use(PT)
                        .Use(orderingTicketValue),
                    Iadd32i(0x200)
                        .Use(PT)
                        .Use(Lop32i(LogicOp.And, 0xfe00)
                            .Use(PT)
                            .Use(orderingTicketValue).Out),
                    Iscadd(cc: true, 2, 0, 404)
                        .Use(PT)
                        .Use(Bfi(0x808)
                            .Use(PT)
                            .Use(affinityValue)
                            .Use(Lop32i(LogicOp.And, 0xff)
                                .Use(PT)
                                .Use(orderingTicketValue).Out).Out),
                    Iadd(x: true, 0, 405).Use(PT).Use(RZ),
                    Ret().Use(PT)
                ];
            }

            public static IPatternTreeNode[] GetFsiIsLastWarpThread()
            {
                PatternTreeNodeUse threadKillValue = S2r(SReg.ThreadKill).Use(PT).Out;
                PatternTreeNodeUse laneIdValue = S2r(SReg.LaneId).Use(PT).Out;

                return
                [
                    IsetpU32(IComp.Eq)
                        .Use(PT)
                        .Use(PT)
                        .Use(FloU32()
                            .Use(PT)
                            .Use(Vote(VoteMode.Any)
                                .Use(PT)
                                .Use(IsetpU32(IComp.Ne)
                                    .Use(PT)
                                    .Use(PT)
                                    .Use(Lop(negB: true, LogicOp.PassB)
                                        .Use(PT)
                                        .Use(RZ)
                                        .Use(threadKillValue).OutAt(1))
                                    .Use(RZ).Out).OutAt(1)).Out)
                        .Use(laneIdValue),
                    Ret().Use(PT)
                ];
            }

            public static IPatternTreeNode[] GetFsiBeginPattern()
            {
                PatternTreeNodeUse addressLowValue = CallArg(1);

                static PatternTreeNodeUse HighU16Equals(PatternTreeNodeUse x)
                {
                    PatternTreeNodeUse expectedValue = CallArg(3);

                    return IsetpU32(IComp.Eq)
                        .Use(PT)
                        .Use(PT)
                        .Use(ShrU32W(16).Use(PT).Use(x).Out)
                        .Use(expectedValue).Out;
                }

                PatternTreeNode<byte> label;

                return
                [
                    Cal(),
                    Ret().Use(CallArg(0).Inv),
                    Ret()
                        .Use(HighU16Equals(LdgE(CacheOpLd.Cg, LsSize.B32)
                            .Use(PT)
                            .Use(addressLowValue).Out)),
                    label = Label(),
                    Bra()
                        .Use(HighU16Equals(LdgE(CacheOpLd.Cg, LsSize.B32, 1)
                            .Use(PT)
                            .Use(addressLowValue).Out).Inv)
                        .Use(label.Out),
                    Ret().Use(PT)
                ];
            }

            public static IPatternTreeNode[] GetFsiEndPattern()
            {
                PatternTreeNodeUse voteResult = Vote(VoteMode.All).Use(PT).Use(PT).OutAt(1);
                PatternTreeNodeUse popcResult = Popc().Use(PT).Use(voteResult).Out;
                PatternTreeNodeUse threadKillValue = S2r(SReg.ThreadKill).Use(PT).Out;
                PatternTreeNodeUse laneIdValue = S2r(SReg.LaneId).Use(PT).Out;

                PatternTreeNodeUse addressLowValue = CallArg(1);
                PatternTreeNodeUse incrementValue = CallArg(2);

                return
                [
                    Cal(),
                    Ret().Use(CallArg(0).Inv),
                    Membar(Decoders.Membar.Vc).Use(PT),
                    Ret().Use(IsetpU32(IComp.Ne)
                        .Use(PT)
                        .Use(PT)
                        .Use(threadKillValue)
                        .Use(RZ).Out),
                    RedE(RedOp.Add, AtomSize.U32)
                        .Use(IsetpU32(IComp.Eq)
                            .Use(PT)
                            .Use(PT)
                            .Use(FloU32()
                                .Use(PT)
                                .Use(voteResult).Out)
                            .Use(laneIdValue).Out)
                        .Use(addressLowValue)
                        .Use(Xmad(XmadCop.Cbcc, psl: true, hiloA: true, hiloB: true)
                            .Use(PT)
                            .Use(incrementValue)
                            .Use(Xmad(XmadCop.Cfull, mrg: true, hiloB: true)
                                .Use(PT)
                                .Use(incrementValue)
                                .Use(popcResult)
                                .Use(RZ).Out)
                            .Use(Xmad(XmadCop.Cfull)
                                .Use(PT)
                                .Use(incrementValue)
                                .Use(popcResult)
                                .Use(RZ).Out).Out),
                    Ret().Use(PT)
                ];
            }

            private static PatternTreeNode<InstBfiI> Bfi(int imm)
            {
                return new(InstName.Bfi, (op) => !op.WriteCC && op.Imm20 == imm, isImm: true);
            }

            private static PatternTreeNode<InstBra> Bra()
            {
                return new(InstName.Bra, (op) => op.Ccc == Ccc.T && !op.Ca);
            }

            private static PatternTreeNode<InstCal> Cal()
            {
                return new(InstName.Cal, (op) => !op.Ca && op.Inc);
            }

            private static PatternTreeNode<InstFloR> FloU32()
            {
                return new(InstName.Flo, (op) => !op.Signed && !op.Sh && !op.NegB && !op.WriteCC);
            }

            private static PatternTreeNode<InstIaddC> Iadd(bool x, int cbufSlot, int cbufOffset)
            {
                return new(InstName.Iadd, (op) =>
                    !op.Sat &&
                    !op.WriteCC &&
                    op.X == x &&
                    op.AvgMode == AvgMode.NoNeg &&
                    op.CbufSlot == cbufSlot &&
                    op.CbufOffset == cbufOffset);
            }

            private static PatternTreeNode<InstIadd32i> Iadd32i(int imm)
            {
                return new(InstName.Iadd32i, (op) => !op.Sat && !op.WriteCC && !op.X && op.AvgMode == AvgMode.NoNeg && op.Imm32 == imm);
            }

            private static PatternTreeNode<InstIscaddR> Iscadd(bool cc, int imm)
            {
                return new(InstName.Iscadd, (op) => op.WriteCC == cc && op.AvgMode == AvgMode.NoNeg && op.Imm5 == imm);
            }

            private static PatternTreeNode<InstIscaddC> Iscadd(bool cc, int imm, int cbufSlot, int cbufOffset)
            {
                return new(InstName.Iscadd, (op) =>
                    op.WriteCC == cc &&
                    op.AvgMode == AvgMode.NoNeg &&
                    op.Imm5 == imm &&
                    op.CbufSlot == cbufSlot &&
                    op.CbufOffset == cbufOffset);
            }

            private static PatternTreeNode<InstIsetpR> IsetpU32(IComp comp)
            {
                return new(InstName.Isetp, (op) => !op.Signed && op.IComp == comp && op.Bop == BoolOp.And);
            }

            private static PatternTreeNode<byte> Label()
            {
                return new(InstName.Invalid, (op) => true, type: TreeNodeType.Label);
            }

            private static PatternTreeNode<InstLopR> Lop(bool negB, LogicOp logicOp)
            {
                return new(InstName.Lop, (op) => !op.NegA && op.NegB == negB && !op.WriteCC && !op.X && op.Lop == logicOp && op.PredicateOp == PredicateOp.F);
            }

            private static PatternTreeNode<InstLop32i> Lop32i(LogicOp logicOp, int imm)
            {
                return new(InstName.Lop32i, (op) => !op.NegA && !op.NegB && !op.X && !op.WriteCC && op.LogicOp == logicOp && op.Imm32 == imm);
            }

            private static PatternTreeNode<InstMembar> Membar(Membar membar)
            {
                return new(InstName.Membar, (op) => op.Membar == membar);
            }

            private static PatternTreeNode<InstPopcR> Popc()
            {
                return new(InstName.Popc, (op) => !op.NegB);
            }

            private static PatternTreeNode<InstRet> Ret()
            {
                return new(InstName.Ret, (op) => op.Ccc == Ccc.T);
            }

            private static PatternTreeNode<InstS2r> S2r(SReg reg)
            {
                return new(InstName.S2r, (op) => op.SReg == reg);
            }

            private static PatternTreeNode<InstShrI> ShrU32W(int imm)
            {
                return new(InstName.Shr, (op) => !op.Signed && !op.Brev && op.M && op.XMode == 0 && op.Imm20 == imm, isImm: true);
            }

            private static PatternTreeNode<InstLdg> LdgE(CacheOpLd cacheOp, LsSize size, byte order = 0)
            {
                return new(InstName.Ldg, (op) => op.E && op.CacheOp == cacheOp && op.LsSize == size, order: order);
            }

            private static PatternTreeNode<InstRed> RedE(RedOp redOp, AtomSize size, byte order = 0)
            {
                return new(InstName.Red, (op) => op.E && op.RedOp == redOp && op.RedSize == size, order: order);
            }

            private static PatternTreeNode<InstVote> Vote(VoteMode mode)
            {
                return new(InstName.Vote, (op) => op.VoteMode == mode);
            }

            private static PatternTreeNode<InstXmadR> Xmad(XmadCop cop, bool psl = false, bool mrg = false, bool hiloA = false, bool hiloB = false)
            {
                return new(InstName.Xmad, (op) => op.XmadCop == cop && op.Psl == psl && op.Mrg == mrg && op.HiloA == hiloA && op.HiloB == hiloB);
            }

            private static PatternTreeNodeUse PT => PTOrRZ();
            private static PatternTreeNodeUse RZ => PTOrRZ();

            private static PatternTreeNodeUse CallArg(int index)
            {
                return new PatternTreeNodeUse(-(index + 2), null);
            }

            private static PatternTreeNodeUse PTOrRZ()
            {
                return new PatternTreeNodeUse(-1, null);
            }
        }

        private static void PrintTreeNode(TreeNode node, string indentation)
        {
            Console.WriteLine($" {node.Op.Name}");

            for (int i = 0; i < node.Uses.Count; i++)
            {
                TreeNodeUse use = node.Uses[i];
                bool last = i == node.Uses.Count - 1;
                char separator = last ? '`' : '|';

                if (use.Node != null)
                {
                    Console.Write($"{indentation} {separator}- ({(use.Inverted ? "INV " : string.Empty)}{use.Index})");
                    PrintTreeNode(use.Node, indentation + (last ? "       " : " |     "));
                }
                else
                {
                    Console.WriteLine($"{indentation} {separator}- ({(use.Inverted ? "INV " : string.Empty)}{use.Index}) NULL");
                }
            }
        }

        private static void PrintTreeNode(IPatternTreeNode node, string indentation)
        {
            Console.WriteLine($" {node.Name}");

            for (int i = 0; i < node.Uses.Count; i++)
            {
                PatternTreeNodeUse use = node.Uses[i];
                bool last = i == node.Uses.Count - 1;
                char separator = last ? '`' : '|';

                if (use.Node != null)
                {
                    Console.Write($"{indentation} {separator}- ({(use.Inverted ? "INV " : string.Empty)}{use.Index})");
                    PrintTreeNode(use.Node, indentation + (last ? "       " : " |     "));
                }
                else
                {
                    Console.WriteLine($"{indentation} {separator}- ({(use.Inverted ? "INV " : string.Empty)}{use.Index}) NULL");
                }
            }
        }
    }
}
