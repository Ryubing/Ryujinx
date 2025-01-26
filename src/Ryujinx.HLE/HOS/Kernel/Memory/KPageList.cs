using Ryujinx.Horizon.Common;
using System.Collections;
using System.Collections.Generic;

namespace Ryujinx.HLE.HOS.Kernel.Memory
{
    class KPageList : IEnumerable<KPageNode>
    {
        public LinkedList<KPageNode> Nodes { get; }

        public KPageList()
        {
            Nodes = [];
        }

        public Result AddRange(ulong address, ulong pagesCount)
        {
            if (pagesCount != 0)
            {
                if (Nodes.Last != null)
                {
                    KPageNode lastNode = Nodes.Last.Value;

                    if (lastNode.Address + lastNode.PagesCount * KPageTableBase.PageSize == address)
                    {
                        address = lastNode.Address;
                        pagesCount += lastNode.PagesCount;

                        Nodes.RemoveLast();
                    }
                }

                Nodes.AddLast(new KPageNode(address, pagesCount));
            }

            return Result.Success;
        }

        public ulong GetPagesCount()
        {
            ulong sum = 0;

            foreach (KPageNode node in Nodes)
            {
                sum += node.PagesCount;
            }

            return sum;
        }

        public bool IsEqual(KPageList other)
        {
            LinkedListNode<KPageNode> thisNode = Nodes.First;
            LinkedListNode<KPageNode> otherNode = other.Nodes.First;

            while (thisNode != null && otherNode != null)
            {
                if (thisNode.Value.Address != otherNode.Value.Address ||
                    thisNode.Value.PagesCount != otherNode.Value.PagesCount)
                {
                    return false;
                }

                thisNode = thisNode.Next;
                otherNode = otherNode.Next;
            }

            return thisNode == null && otherNode == null;
        }

        public void IncrementPagesReferenceCount(KMemoryManager manager)
        {
            foreach (KPageNode node in this)
            {
                manager.IncrementPagesReferenceCount(node.Address, node.PagesCount);
            }
        }

        public void DecrementPagesReferenceCount(KMemoryManager manager)
        {
            foreach (KPageNode node in this)
            {
                manager.DecrementPagesReferenceCount(node.Address, node.PagesCount);
            }
        }

        public IEnumerator<KPageNode> GetEnumerator()
        {
            return Nodes.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
