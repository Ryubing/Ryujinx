using ARMeilleure.Common;
using ARMeilleure.IntermediateRepresentation;
using ARMeilleure.State;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using static ARMeilleure.IntermediateRepresentation.Operand.Factory;

namespace ARMeilleure.Translation
{
    static partial class Ssa
    {
        private class DefMap
        {
            private readonly Dictionary<int, Operand> _map;
            private readonly BitMap _phiMasks;

            public DefMap()
            {
                _map = new Dictionary<int, Operand>();
                _phiMasks = new BitMap(Allocators.Default, RegisterConsts.TotalCount);
            }

            public bool TryAddOperand(int key, Operand operand)
            {
                return _map.TryAdd(key, operand);
            }

            public bool TryGetOperand(int key, out Operand operand)
            {
                return _map.TryGetValue(key, out operand);
            }

            public bool AddPhi(int key)
            {
                return _phiMasks.Set(key);
            }

            public bool HasPhi(int key)
            {
                return _phiMasks.IsSet(key);
            }
        }

        public static void Construct(ControlFlowGraph cfg)
        {
            DefMap[] globalDefs = new DefMap[cfg.Blocks.Count];
            Operand[] localDefs = new Operand[cfg.LocalsCount + RegisterConsts.TotalCount];

            Queue<BasicBlock> dfPhiBlocks = new();

            for (BasicBlock block = cfg.Blocks.First; block != null; block = block.ListNext)
            {
                globalDefs[block.Index] = new DefMap();
            }

            // First pass, get all defs and locals uses.
            for (BasicBlock block = cfg.Blocks.First; block != null; block = block.ListNext)
            {
                for (Operation node = block.Operations.First; node != default; node = node.ListNext)
                {
                    for (int index = 0; index < node.SourcesCount; index++)
                    {
                        Operand src = node.GetSource(index);

                        if (TryGetId(src, out int srcKey))
                        {
                            Operand local = localDefs[srcKey];

                            if (local == default)
                            {
                                local = src;
                            }

                            node.SetSource(index, local);
                        }
                    }

                    Operand dest = node.Destination;

                    if (TryGetId(dest, out int destKey))
                    {
                        Operand local = Local(dest.Type);

                        localDefs[destKey] = local;

                        node.Destination = local;
                    }
                }

                for (int key = 0; key < localDefs.Length; key++)
                {
                    Operand local = localDefs[key];

                    if (local == default)
                    {
                        continue;
                    }

                    globalDefs[block.Index].TryAddOperand(key, local);

                    dfPhiBlocks.Enqueue(block);

                    while (dfPhiBlocks.TryDequeue(out BasicBlock dfPhiBlock))
                    {
                        foreach (BasicBlock domFrontier in dfPhiBlock.DominanceFrontiers)
                        {
                            if (globalDefs[domFrontier.Index].AddPhi(key))
                            {
                                dfPhiBlocks.Enqueue(domFrontier);
                            }
                        }
                    }
                }

                Array.Clear(localDefs);
            }

            // Second pass, rename variables with definitions on different blocks.
            for (BasicBlock block = cfg.Blocks.First; block != null; block = block.ListNext)
            {
                for (Operation node = block.Operations.First; node != default; node = node.ListNext)
                {
                    for (int index = 0; index < node.SourcesCount; index++)
                    {
                        Operand src = node.GetSource(index);

                        if (TryGetId(src, out int key))
                        {
                            Operand local = localDefs[key];

                            if (local == default)
                            {
                                local = FindDef(globalDefs, block, src);
                                localDefs[key] = local;
                            }

                            node.SetSource(index, local);
                        }
                    }
                }

                Array.Clear(localDefs);
            }
        }

        private static Operand FindDef(DefMap[] globalDefs, BasicBlock current, Operand operand)
        {
            if (globalDefs[current.Index].HasPhi(GetId(operand)))
            {
                return InsertPhi(globalDefs, current, operand);
            }

            if (current != current.ImmediateDominator)
            {
                return FindDefOnPred(globalDefs, current.ImmediateDominator, operand);
            }

            return Undef();
        }

        private static Operand FindDefOnPred(DefMap[] globalDefs, BasicBlock current, Operand operand)
        {
            BasicBlock previous;

            do
            {
                DefMap defMap = globalDefs[current.Index];

                int key = GetId(operand);

                if (defMap.TryGetOperand(key, out Operand lastDef))
                {
                    return lastDef;
                }

                if (defMap.HasPhi(key))
                {
                    return InsertPhi(globalDefs, current, operand);
                }

                previous = current;
                current = current.ImmediateDominator;
            }
            while (previous != current);

            return Undef();
        }

        private static Operand InsertPhi(DefMap[] globalDefs, BasicBlock block, Operand operand)
        {
            // This block has a Phi that has not been materialized yet, but that
            // would define a new version of the variable we're looking for. We need
            // to materialize the Phi, add all the block/operand pairs into the Phi, and
            // then use the definition from that Phi.
            Operand local = Local(operand.Type);

            Operation operation = Operation.Factory.PhiOperation(local, block.Predecessors.Count);

            AddPhi(block, operation);

            globalDefs[block.Index].TryAddOperand(GetId(operand), local);

            PhiOperation phi = operation.AsPhi();

            for (int index = 0; index < block.Predecessors.Count; index++)
            {
                BasicBlock predecessor = block.Predecessors[index];

                phi.SetBlock(index, predecessor);
                phi.SetSource(index, FindDefOnPred(globalDefs, predecessor, operand));
            }

            return local;
        }

        private static void AddPhi(BasicBlock block, Operation phi)
        {
            Operation node = block.Operations.First;

            if (node != default)
            {
                while (node.ListNext != default && node.ListNext.Instruction == Instruction.Phi)
                {
                    node = node.ListNext;
                }
            }

            if (node != default && node.Instruction == Instruction.Phi)
            {
                block.Operations.AddAfter(node, phi);
            }
            else
            {
                block.Operations.AddFirst(phi);
            }
        }

        private static bool TryGetId(Operand operand, out int result)
        {
            if (operand != default)
            {
                if (operand.Kind == OperandKind.Register)
                {
                    Register reg = operand.GetRegister();

                    if (reg.Type == RegisterType.Integer)
                    {
                        result = reg.Index;
                    }
                    else if (reg.Type == RegisterType.Vector)
                    {
                        result = RegisterConsts.IntRegsCount + reg.Index;
                    }
                    else if (reg.Type == RegisterType.Flag)
                    {
                        result = RegisterConsts.IntAndVecRegsCount + reg.Index;
                    }
                    else /* if (reg.Type == RegisterType.FpFlag) */
                    {
                        result = RegisterConsts.FpFlagsOffset + reg.Index;
                    }

                    return true;
                }
                else if (operand.Kind == OperandKind.LocalVariable && operand.GetLocalNumber() > 0)
                {
                    result = RegisterConsts.TotalCount + operand.GetLocalNumber() - 1;

                    return true;
                }
            }

            result = -1;

            return false;
        }

        private static int GetId(Operand operand)
        {
            if (!TryGetId(operand, out int key))
            {
                Debug.Fail("OperandKind must be Register or a numbered LocalVariable.");
            }

            return key;
        }
    }
}
