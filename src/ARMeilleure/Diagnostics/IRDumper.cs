using ARMeilleure.IntermediateRepresentation;
using ARMeilleure.Translation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ARMeilleure.Diagnostics
{
    class IRDumper
    {
        private const string Indentation = " ";

        private int _indentLevel;

        private readonly StringBuilder _builder;

        private readonly Dictionary<Operand, string> _localNames;
        private readonly Dictionary<ulong, string> _symbolNames;

        public IRDumper(int indent)
        {
            _indentLevel = indent;

            _builder = new StringBuilder();

            _localNames = new Dictionary<Operand, string>();
            _symbolNames = new Dictionary<ulong, string>();
        }

        private void Indent()
        {
            _builder.EnsureCapacity(_builder.Capacity + _indentLevel * Indentation.Length);

            for (int index = 0; index < _indentLevel; index++)
            {
#pragma warning disable CA1834 // Use StringBuilder.Append(char) for single character strings
                _builder.Append(Indentation);
#pragma warning restore CA1834
            }
        }

        private void IncreaseIndentation()
        {
            _indentLevel++;
        }

        private void DecreaseIndentation()
        {
            _indentLevel--;
        }

        private void DumpBlockName(BasicBlock block)
        {
            _builder.Append("block").Append(block.Index);
        }

        private void DumpBlockHeader(BasicBlock block)
        {
            DumpBlockName(block);

            if (block.Frequency == BasicBlockFrequency.Cold)
            {
                _builder.Append(" cold");
            }

            if (block.SuccessorsCount > 0)
            {
                _builder.Append(" (");

                for (int i = 0; i < block.SuccessorsCount; i++)
                {
                    DumpBlockName(block.GetSuccessor(i));

                    if (i < block.SuccessorsCount - 1)
                    {
                        _builder.Append(", ");
                    }
                }

                _builder.Append(')');
            }

            _builder.Append(':');
        }

        private void DumpOperand(Operand operand)
        {
            if (operand == default)
            {
                _builder.Append("<NULL>");
                return;
            }

            _builder.Append(GetTypeName(operand.Type)).Append(' ');

            switch (operand.Kind)
            {
                case OperandKind.LocalVariable:
                    if (!_localNames.TryGetValue(operand, out string localName))
                    {
                        localName = $"%{_localNames.Count}";

                        _localNames.Add(operand, localName);
                    }

                    _builder.Append(localName);
                    break;

                case OperandKind.Register:
                    Register reg = operand.GetRegister();

                    switch (reg.Type)
                    {
                        case RegisterType.Flag:
                            _builder.Append('b');
                            break;
                        case RegisterType.FpFlag:
                            _builder.Append('f');
                            break;
                        case RegisterType.Integer:
                            _builder.Append('r');
                            break;
                        case RegisterType.Vector:
                            _builder.Append('v');
                            break;
                    }

                    _builder.Append(reg.Index);
                    break;

                case OperandKind.Constant:
                    string symbolName = Symbols.Get(operand.Value);

                    if (symbolName != null && !_symbolNames.ContainsKey(operand.Value))
                    {
                        _symbolNames.Add(operand.Value, symbolName);
                    }

                    _builder.Append("0x").Append(operand.Value.ToString("X"));
                    break;

                case OperandKind.Memory:
                    MemoryOperand memOp = operand.GetMemory();

                    _builder.Append('[');

                    DumpOperand(memOp.BaseAddress);

                    if (memOp.Index != default)
                    {
                        _builder.Append(" + ");

                        DumpOperand(memOp.Index);

                        switch (memOp.Scale)
                        {
                            case Multiplier.x2:
                                _builder.Append("*2");
                                break;
                            case Multiplier.x4:
                                _builder.Append("*4");
                                break;
                            case Multiplier.x8:
                                _builder.Append("*8");
                                break;
                        }
                    }

                    if (memOp.Displacement != 0)
                    {
                        _builder.Append(" + 0x").Append(memOp.Displacement.ToString("X"));
                    }

                    _builder.Append(']');
                    break;

                default:
                    _builder.Append(operand.Type);
                    break;
            }
        }

        private void DumpNode(ControlFlowGraph cfg, Operation node)
        {
            for (int index = 0; index < node.DestinationsCount; index++)
            {
                DumpOperand(node.GetDestination(index));

                if (index == node.DestinationsCount - 1)
                {
                    _builder.Append(" = ");
                }
                else
                {
                    _builder.Append(", ");
                }
            }

            switch (node)
            {
                case Operation operation:
                    if (operation.Instruction == Instruction.Phi)
                    {
                        PhiOperation phi = operation.AsPhi();

                        _builder.Append("Phi ");

                        for (int index = 0; index < phi.SourcesCount; index++)
                        {
                            _builder.Append('(');

                            DumpBlockName(phi.GetBlock(cfg, index));

                            _builder.Append(": ");

                            DumpOperand(phi.GetSource(index));

                            _builder.Append(')');

                            if (index < phi.SourcesCount - 1)
                            {
                                _builder.Append(", ");
                            }
                        }

                        break;
                    }

                    bool comparison = false;

                    _builder.Append(operation.Instruction);

                    if (operation.Instruction == Instruction.Extended)
                    {
                        _builder.Append('.').Append(operation.Intrinsic);
                    }
                    else if (operation.Instruction == Instruction.BranchIf ||
                             operation.Instruction == Instruction.Compare)
                    {
                        comparison = true;
                    }

                    _builder.Append(' ');

                    for (int index = 0; index < operation.SourcesCount; index++)
                    {
                        Operand source = operation.GetSource(index);

                        if (index < operation.SourcesCount - 1)
                        {
                            DumpOperand(source);

                            _builder.Append(", ");
                        }
                        else if (comparison)
                        {
                            _builder.Append((Comparison)source.AsInt32());
                        }
                        else
                        {
                            DumpOperand(source);
                        }
                    }
                    break;
            }

            if (_symbolNames.Count == 1)
            {
                _builder.Append(" ;; ").Append(_symbolNames.First().Value);
            }
            else if (_symbolNames.Count > 1)
            {
                _builder.Append(" ;;");

                foreach ((ulong value, string name) in _symbolNames)
                {
                    _builder.Append(" 0x").Append(value.ToString("X")).Append(" = ").Append(name);
                }
            }

            // Reset the set of symbols for the next Node we're going to dump.
            _symbolNames.Clear();
        }

        public static string GetDump(ControlFlowGraph cfg)
        {
            IRDumper dumper = new(1);

            for (BasicBlock block = cfg.Blocks.First; block != null; block = block.ListNext)
            {
                dumper.Indent();
                dumper.DumpBlockHeader(block);

                dumper._builder.AppendLine();

                dumper.IncreaseIndentation();

                for (Operation node = block.Operations.First; node != default; node = node.ListNext)
                {
                    dumper.Indent();
                    dumper.DumpNode(cfg, node);

                    dumper._builder.AppendLine();
                }

                dumper.DecreaseIndentation();
            }

            return dumper._builder.ToString();
        }

        private static string GetTypeName(OperandType type)
        {
            return type switch
            {
                OperandType.None => "none",
                OperandType.I32 => "i32",
                OperandType.I64 => "i64",
                OperandType.FP32 => "f32",
                OperandType.FP64 => "f64",
                OperandType.V128 => "v128",
                _ => throw new ArgumentException($"Invalid operand type \"{type}\"."),
            };
        }
    }
}
