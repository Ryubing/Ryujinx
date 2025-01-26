using Ryujinx.Graphics.Shader.IntermediateRepresentation;
using System;
using System.Collections;
using System.Collections.Generic;

using static Ryujinx.Graphics.Shader.StructuredIr.AstHelper;

namespace Ryujinx.Graphics.Shader.StructuredIr
{
    class AstBlock : AstNode, IEnumerable<IAstNode>
    {
        public AstBlockType Type { get; private set; }

        private IAstNode _condition;

        public IAstNode Condition
        {
            get
            {
                return _condition;
            }
            set
            {
                RemoveUse(_condition, this);

                AddUse(value, this);

                _condition = value;
            }
        }

        private readonly LinkedList<IAstNode> _nodes;

        public IAstNode First => _nodes.First?.Value;
        public IAstNode Last => _nodes.Last?.Value;

        public int Count => _nodes.Count;

        public AstBlock(AstBlockType type, IAstNode condition = null)
        {
            Type = type;
            Condition = condition;

            _nodes = [];
        }

        public void Add(IAstNode node)
        {
            Add(node, _nodes.AddLast(node));
        }

        public void AddFirst(IAstNode node)
        {
            Add(node, _nodes.AddFirst(node));
        }

        public void AddBefore(IAstNode next, IAstNode node)
        {
            Add(node, _nodes.AddBefore(next.LLNode, node));
        }

        public void AddAfter(IAstNode prev, IAstNode node)
        {
            Add(node, _nodes.AddAfter(prev.LLNode, node));
        }

        private void Add(IAstNode node, LinkedListNode<IAstNode> newNode)
        {
            if (node.Parent != null)
            {
                throw new ArgumentException("Node already belongs to a block.");
            }

            node.Parent = this;
            node.LLNode = newNode;
        }

        public void Remove(IAstNode node)
        {
            _nodes.Remove(node.LLNode);

            node.Parent = null;
            node.LLNode = null;
        }

        public void AndCondition(IAstNode cond)
        {
            Condition = new AstOperation(Instruction.LogicalAnd, Condition, cond);
        }

        public void OrCondition(IAstNode cond)
        {
            Condition = new AstOperation(Instruction.LogicalOr, Condition, cond);
        }
        public void TurnIntoIf(IAstNode cond)
        {
            Condition = cond;

            Type = AstBlockType.If;
        }

        public void TurnIntoElseIf()
        {
            Type = AstBlockType.ElseIf;
        }

        public IEnumerator<IAstNode> GetEnumerator()
        {
            return _nodes.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
