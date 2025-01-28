using System;
using System.Collections.Generic;
using System.Linq;

namespace Ryujinx.Common.Collections
{
    /// <summary>
    /// An Augmented Interval Tree based off of the "TreeDictionary"'s Red-Black Tree. Allows fast overlap checking of ranges.
    /// </summary>
    /// <typeparam name="TKey">Key</typeparam>
    /// <typeparam name="TValue">Value</typeparam>
    public class IntervalTree<TKey, TValue> : IntrusiveRedBlackTreeImpl<IntervalTreeNode<TKey, TValue>> where TKey : IComparable<TKey>
    {
        private const int ArrayGrowthSize = 32;

        #region Public Methods

        /// <summary>
        /// Gets the values of the interval whose key is <paramref name="key"/>.
        /// </summary>
        /// <param name="key">Key of the node value to get</param>
        /// <param name="overlaps">Overlaps array to place results in</param>
        /// <returns>Number of values found</returns>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is null</exception>
        public int Get(TKey key, ref TValue[] overlaps)
        {
            ArgumentNullException.ThrowIfNull(key);

            IntervalTreeNode<TKey, TValue> node = GetNode(key);

            if (node == null)
            {
                return 0;
            }

            if (node.Values.Count > overlaps.Length)
            {
                Array.Resize(ref overlaps, node.Values.Count);
            }

            int overlapsCount = 0;
            foreach (RangeNode<TKey, TValue> value in node.Values)
            {
                overlaps[overlapsCount++] = value.Value;
            }

            return overlapsCount;
        }

        /// <summary>
        /// Returns the values of the intervals whose start and end keys overlap the given range.
        /// </summary>
        /// <param name="start">Start of the range</param>
        /// <param name="end">End of the range</param>
        /// <param name="overlaps">Overlaps array to place results in</param>
        /// <param name="overlapCount">Index to start writing results into the array. Defaults to 0</param>
        /// <returns>Number of values found</returns>
        /// <exception cref="ArgumentNullException"><paramref name="start"/> or <paramref name="end"/> is null</exception>
        public int Get(TKey start, TKey end, ref TValue[] overlaps, int overlapCount = 0)
        {
            ArgumentNullException.ThrowIfNull(start);
            ArgumentNullException.ThrowIfNull(end);

            GetValues(Root, start, end, ref overlaps, ref overlapCount);

            return overlapCount;
        }

        /// <summary>
        /// Adds a new interval into the tree whose start is <paramref name="start"/>, end is <paramref name="end"/> and value is <paramref name="value"/>.
        /// </summary>
        /// <param name="start">Start of the range to add</param>
        /// <param name="end">End of the range to insert</param>
        /// <param name="value">Value to add</param>
        /// <exception cref="ArgumentNullException"><paramref name="start"/>, <paramref name="end"/> or <paramref name="value"/> are null</exception>
        public void Add(TKey start, TKey end, TValue value)
        {
            ArgumentNullException.ThrowIfNull(start);
            ArgumentNullException.ThrowIfNull(end);
            ArgumentNullException.ThrowIfNull(value);

            Insert(start, end, value);
        }

        /// <summary>
        /// Removes the given <paramref name="value"/> from the tree, searching for it with <paramref name="key"/>.
        /// </summary>
        /// <param name="key">Key of the node to remove</param>
        /// <param name="value">Value to remove</param>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is null</exception>
        /// <returns>Number of deleted values</returns>
        public int Remove(TKey key, TValue value)
        {
            ArgumentNullException.ThrowIfNull(key);

            int removed = Delete(key, value);

            Count -= removed;

            return removed;
        }

        /// <summary>
        /// Adds all the nodes in the dictionary into <paramref name="list"/>.
        /// </summary>
        /// <returns>A list of all RangeNodes sorted by Key Order</returns>
        public List<RangeNode<TKey, TValue>> AsList()
        {
            List<RangeNode<TKey, TValue>> list = [];

            AddToList(Root, list);

            return list;
        }

        #endregion

        #region Private Methods (BST)

        /// <summary>
        /// Adds all RangeNodes that are children of or contained within <paramref name="node"/> into <paramref name="list"/>, in Key Order.
        /// </summary>
        /// <param name="node">The node to search for RangeNodes within</param>
        /// <param name="list">The list to add RangeNodes to</param>
        private void AddToList(IntervalTreeNode<TKey, TValue> node, List<RangeNode<TKey, TValue>> list)
        {
            if (node == null)
            {
                return;
            }

            AddToList(node.Left, list);

            list.AddRange(node.Values);

            AddToList(node.Right, list);
        }

        /// <summary>
        /// Retrieve the node reference whose key is <paramref name="key"/>, or null if no such node exists.
        /// </summary>
        /// <param name="key">Key of the node to get</param>
        /// <returns>Node reference in the tree</returns>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is null</exception>
        private IntervalTreeNode<TKey, TValue> GetNode(TKey key)
        {
            ArgumentNullException.ThrowIfNull(key);

            IntervalTreeNode<TKey, TValue> node = Root;
            while (node != null)
            {
                int cmp = key.CompareTo(node.Start);
                if (cmp < 0)
                {
                    node = node.Left;
                }
                else if (cmp > 0)
                {
                    node = node.Right;
                }
                else
                {
                    return node;
                }
            }
            return null;
        }

        /// <summary>
        /// Retrieve all values that overlap the given start and end keys.
        /// </summary>
        /// <param name="start">Start of the range</param>
        /// <param name="end">End of the range</param>
        /// <param name="overlaps">Overlaps array to place results in</param>
        /// <param name="overlapCount">Overlaps count to update</param>
        private void GetValues(IntervalTreeNode<TKey, TValue> node, TKey start, TKey end, ref TValue[] overlaps, ref int overlapCount)
        {
            if (node == null || start.CompareTo(node.Max) >= 0)
            {
                return;
            }

            GetValues(node.Left, start, end, ref overlaps, ref overlapCount);

            bool endsOnRight = end.CompareTo(node.Start) > 0;
            if (endsOnRight)
            {
                if (start.CompareTo(node.End) < 0)
                {
                    // Contains this node. Add overlaps to list.
                    foreach (RangeNode<TKey, TValue> overlap in node.Values)
                    {
                        if (start.CompareTo(overlap.End) < 0)
                        {
                            if (overlaps.Length <= overlapCount)
                            {
                                Array.Resize(ref overlaps, overlapCount + ArrayGrowthSize);
                            }

                            overlaps[overlapCount++] = overlap.Value;
                        }
                    }
                }

                GetValues(node.Right, start, end, ref overlaps, ref overlapCount);
            }
        }

        /// <summary>
        /// Inserts a new node into the tree with a given <paramref name="start"/>, <paramref name="end"/> and <paramref name="value"/>.
        /// </summary>
        /// <param name="start">Start of the range to insert</param>
        /// <param name="end">End of the range to insert</param>
        /// <param name="value">Value to insert</param>
        private void Insert(TKey start, TKey end, TValue value)
        {
            IntervalTreeNode<TKey, TValue> newNode = BSTInsert(start, end, value);
            RestoreBalanceAfterInsertion(newNode);
        }

        /// <summary>
        /// Propagate an increase in max value starting at the given node, heading up the tree.
        /// This should only be called if the max increases - not for rebalancing or removals.
        /// </summary>
        /// <param name="node">The node to start propagating from</param>
        private static void PropagateIncrease(IntervalTreeNode<TKey, TValue> node)
        {
            TKey max = node.Max;
            IntervalTreeNode<TKey, TValue> ptr = node;

            while ((ptr = ptr.Parent) != null)
            {
                if (max.CompareTo(ptr.Max) > 0)
                {
                    ptr.Max = max;
                }
                else
                {
                    break;
                }
            }
        }

        /// <summary>
        /// Propagate recalculating max value starting at the given node, heading up the tree.
        /// This fully recalculates the max value from all children when there is potential for it to decrease.
        /// </summary>
        /// <param name="node">The node to start propagating from</param>
        private static void PropagateFull(IntervalTreeNode<TKey, TValue> node)
        {
            IntervalTreeNode<TKey, TValue> ptr = node;

            do
            {
                TKey max = ptr.End;

                if (ptr.Left != null && ptr.Left.Max.CompareTo(max) > 0)
                {
                    max = ptr.Left.Max;
                }

                if (ptr.Right != null && ptr.Right.Max.CompareTo(max) > 0)
                {
                    max = ptr.Right.Max;
                }

                ptr.Max = max;
            } while ((ptr = ptr.Parent) != null);
        }

        /// <summary>
        /// Insertion Mechanism for the interval tree. Similar to a BST insert, with the start of the range as the key.
        /// Iterates the tree starting from the root and inserts a new node where all children in the left subtree are less than <paramref name="start"/>, and all children in the right subtree are greater than <paramref name="start"/>.
        /// Each node can contain multiple values, and has an end address which is the maximum of all those values.
        /// Post insertion, the "max" value of the node and all parents are updated.
        /// </summary>
        /// <param name="start">Start of the range to insert</param>
        /// <param name="end">End of the range to insert</param>
        /// <param name="value">Value to insert</param>
        /// <returns>The inserted Node</returns>
        private IntervalTreeNode<TKey, TValue> BSTInsert(TKey start, TKey end, TValue value)
        {
            IntervalTreeNode<TKey, TValue> parent = null;
            IntervalTreeNode<TKey, TValue> node = Root;

            while (node != null)
            {
                parent = node;
                int cmp = start.CompareTo(node.Start);
                if (cmp < 0)
                {
                    node = node.Left;
                }
                else if (cmp > 0)
                {
                    node = node.Right;
                }
                else
                {
                    node.Values.Add(new RangeNode<TKey, TValue>(start, end, value));

                    if (end.CompareTo(node.End) > 0)
                    {
                        node.End = end;
                        if (end.CompareTo(node.Max) > 0)
                        {
                            node.Max = end;
                            PropagateIncrease(node);
                        }
                    }

                    Count++;
                    return node;
                }
            }
            IntervalTreeNode<TKey, TValue> newNode = new(start, end, value, parent);
            if (newNode.Parent == null)
            {
                Root = newNode;
            }
            else if (start.CompareTo(parent.Start) < 0)
            {
                parent.Left = newNode;
            }
            else
            {
                parent.Right = newNode;
            }

            PropagateIncrease(newNode);
            Count++;
            return newNode;
        }

        /// <summary>
        /// Removes instances of <paramref name="value"> from the dictionary after searching for it with <paramref name="key">.
        /// </summary>
        /// <param name="key">Key to search for</param>
        /// <param name="value">Value to delete</param>
        /// <returns>Number of deleted values</returns>
        private int Delete(TKey key, TValue value)
        {
            IntervalTreeNode<TKey, TValue> nodeToDelete = GetNode(key);

            if (nodeToDelete == null)
            {
                return 0;
            }

            int removed = nodeToDelete.Values.RemoveAll(node => node.Value.Equals(value));

            if (nodeToDelete.Values.Count > 0)
            {
                if (removed > 0)
                {
                    nodeToDelete.End = nodeToDelete.Values.Max(node => node.End);

                    // Recalculate max from children and new end.
                    PropagateFull(nodeToDelete);
                }

                return removed;
            }

            IntervalTreeNode<TKey, TValue> replacementNode;

            if (LeftOf(nodeToDelete) == null || RightOf(nodeToDelete) == null)
            {
                replacementNode = nodeToDelete;
            }
            else
            {
                replacementNode = PredecessorOf(nodeToDelete);
            }

            IntervalTreeNode<TKey, TValue> tmp = LeftOf(replacementNode) ?? RightOf(replacementNode);

            if (tmp != null)
            {
                tmp.Parent = ParentOf(replacementNode);
            }

            if (ParentOf(replacementNode) == null)
            {
                Root = tmp;
            }
            else if (replacementNode == LeftOf(ParentOf(replacementNode)))
            {
                ParentOf(replacementNode).Left = tmp;
            }
            else
            {
                ParentOf(replacementNode).Right = tmp;
            }

            if (replacementNode != nodeToDelete)
            {
                nodeToDelete.Start = replacementNode.Start;
                nodeToDelete.Values = replacementNode.Values;
                nodeToDelete.End = replacementNode.End;
                nodeToDelete.Max = replacementNode.Max;
            }

            PropagateFull(replacementNode);

            if (tmp != null && ColorOf(replacementNode) == Black)
            {
                RestoreBalanceAfterRemoval(tmp);
            }

            return removed;
        }

        #endregion

        protected override void RotateLeft(IntervalTreeNode<TKey, TValue> node)
        {
            if (node != null)
            {
                base.RotateLeft(node);

                PropagateFull(node);
            }
        }

        protected override void RotateRight(IntervalTreeNode<TKey, TValue> node)
        {
            if (node != null)
            {
                base.RotateRight(node);

                PropagateFull(node);
            }
        }

        public bool ContainsKey(TKey key)
        {
            ArgumentNullException.ThrowIfNull(key);

            return GetNode(key) != null;
        }
    }

    /// <summary>
    /// Represents a value and its start and end keys.
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    public readonly struct RangeNode<TKey, TValue>
    {
        public readonly TKey Start;
        public readonly TKey End;
        public readonly TValue Value;

        public RangeNode(TKey start, TKey end, TValue value)
        {
            Start = start;
            End = end;
            Value = value;
        }
    }

    /// <summary>
    /// Represents a node in the IntervalTree which contains start and end keys of type K, and a value of generic type V.
    /// </summary>
    /// <typeparam name="TKey">Key type of the node</typeparam>
    /// <typeparam name="TValue">Value type of the node</typeparam>
    public class IntervalTreeNode<TKey, TValue> : IntrusiveRedBlackTreeNode<IntervalTreeNode<TKey, TValue>>
    {
        /// <summary>
        /// The start of the range.
        /// </summary>
        internal TKey Start;

        /// <summary>
        /// The end of the range - maximum of all in the Values list.
        /// </summary>
        internal TKey End;

        /// <summary>
        /// The maximum end value of this node and all its children.
        /// </summary>
        internal TKey Max;

        /// <summary>
        /// Values contained on the node that shares a common Start value.
        /// </summary>
        internal List<RangeNode<TKey, TValue>> Values;

        internal IntervalTreeNode(TKey start, TKey end, TValue value, IntervalTreeNode<TKey, TValue> parent)
        {
            Start = start;
            End = end;
            Max = end;
            Values = [new RangeNode<TKey, TValue>(start, end, value)];
            Parent = parent;
        }
    }
}
