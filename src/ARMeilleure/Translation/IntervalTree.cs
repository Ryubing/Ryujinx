using System;
using System.Collections.Generic;

namespace ARMeilleure.Translation
{
    /// <summary>
    /// An Augmented Interval Tree based off of the "TreeDictionary"'s Red-Black Tree. Allows fast overlap checking of ranges.
    /// </summary>
    /// <typeparam name="TK">Key</typeparam>
    /// <typeparam name="TV">Value</typeparam>
    public class IntervalTree<TK, TV> where TK : IComparable<TK>
    {
        private const int ArrayGrowthSize = 32;

        private const bool Black = true;
        private const bool Red = false;
        private IntervalTreeNode<TK, TV> _root = null;
        private int _count = 0;

        public int Count => _count;

        #region Public Methods

        /// <summary>
        /// Gets the values of the interval whose key is <paramref name="key"/>.
        /// </summary>
        /// <param name="key">Key of the node value to get</param>
        /// <param name="value">Value with the given <paramref name="key"/></param>
        /// <returns>True if the key is on the dictionary, false otherwise</returns>
        public bool TryGet(TK key, out TV value)
        {
            IntervalTreeNode<TK, TV> node = GetNode(key);

            if (node == null)
            {
                value = default;
                return false;
            }

            value = node.Value;
            return true;
        }

        /// <summary>
        /// Returns the start addresses of the intervals whose start and end keys overlap the given range.
        /// </summary>
        /// <param name="start">Start of the range</param>
        /// <param name="end">End of the range</param>
        /// <param name="overlaps">Overlaps array to place results in</param>
        /// <param name="overlapCount">Index to start writing results into the array. Defaults to 0</param>
        /// <returns>Number of intervals found</returns>
        public int Get(TK start, TK end, ref TK[] overlaps, int overlapCount = 0)
        {
            GetKeys(_root, start, end, ref overlaps, ref overlapCount);

            return overlapCount;
        }

        /// <summary>
        /// Adds a new interval into the tree whose start is <paramref name="start"/>, end is <paramref name="end"/> and value is <paramref name="value"/>.
        /// </summary>
        /// <param name="start">Start of the range to add</param>
        /// <param name="end">End of the range to insert</param>
        /// <param name="value">Value to add</param>
        /// <param name="updateFactoryCallback">Optional factory used to create a new value if <paramref name="start"/> is already on the tree</param>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is null</exception>
        /// <returns>True if the value was added, false if the start key was already in the dictionary</returns>
        public bool AddOrUpdate(TK start, TK end, TV value, Func<TK, TV, TV> updateFactoryCallback)
        {
            ArgumentNullException.ThrowIfNull(value);

            return BSTInsert(start, end, value, updateFactoryCallback, out _);
        }

        /// <summary>
        /// Gets an existing or adds a new interval into the tree whose start is <paramref name="start"/>, end is <paramref name="end"/> and value is <paramref name="value"/>.
        /// </summary>
        /// <param name="start">Start of the range to add</param>
        /// <param name="end">End of the range to insert</param>
        /// <param name="value">Value to add</param>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is null</exception>
        /// <returns><paramref name="value"/> if <paramref name="start"/> is not yet on the tree, or the existing value otherwise</returns>
        public TV GetOrAdd(TK start, TK end, TV value)
        {
            ArgumentNullException.ThrowIfNull(value);

            BSTInsert(start, end, value, null, out IntervalTreeNode<TK, TV> node);
            return node.Value;
        }

        /// <summary>
        /// Removes a value from the tree, searching for it with <paramref name="key"/>.
        /// </summary>
        /// <param name="key">Key of the node to remove</param>
        /// <returns>Number of deleted values</returns>
        public int Remove(TK key)
        {
            int removed = Delete(key);

            _count -= removed;

            return removed;
        }

        /// <summary>
        /// Adds all the nodes in the dictionary into <paramref name="list"/>.
        /// </summary>
        /// <returns>A list of all values sorted by Key Order</returns>
        public List<TV> AsList()
        {
            List<TV> list = [];

            AddToList(_root, list);

            return list;
        }

        #endregion

        #region Private Methods (BST)

        /// <summary>
        /// Adds all values that are children of or contained within <paramref name="node"/> into <paramref name="list"/>, in Key Order.
        /// </summary>
        /// <param name="node">The node to search for values within</param>
        /// <param name="list">The list to add values to</param>
        private void AddToList(IntervalTreeNode<TK, TV> node, List<TV> list)
        {
            if (node == null)
            {
                return;
            }

            AddToList(node.Left, list);

            list.Add(node.Value);

            AddToList(node.Right, list);
        }

        /// <summary>
        /// Retrieve the node reference whose key is <paramref name="key"/>, or null if no such node exists.
        /// </summary>
        /// <param name="key">Key of the node to get</param>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is null</exception>
        /// <returns>Node reference in the tree</returns>
        private IntervalTreeNode<TK, TV> GetNode(TK key)
        {
            ArgumentNullException.ThrowIfNull(key);

            IntervalTreeNode<TK, TV> node = _root;
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
        /// Retrieve all keys that overlap the given start and end keys.
        /// </summary>
        /// <param name="start">Start of the range</param>
        /// <param name="end">End of the range</param>
        /// <param name="overlaps">Overlaps array to place results in</param>
        /// <param name="overlapCount">Overlaps count to update</param>
        private void GetKeys(IntervalTreeNode<TK, TV> node, TK start, TK end, ref TK[] overlaps, ref int overlapCount)
        {
            if (node == null || start.CompareTo(node.Max) >= 0)
            {
                return;
            }

            GetKeys(node.Left, start, end, ref overlaps, ref overlapCount);

            bool endsOnRight = end.CompareTo(node.Start) > 0;
            if (endsOnRight)
            {
                if (start.CompareTo(node.End) < 0)
                {
                    if (overlaps.Length <= overlapCount)
                    {
                        Array.Resize(ref overlaps, overlapCount + ArrayGrowthSize);
                    }

                    overlaps[overlapCount++] = node.Start;
                }

                GetKeys(node.Right, start, end, ref overlaps, ref overlapCount);
            }
        }

        /// <summary>
        /// Propagate an increase in max value starting at the given node, heading up the tree.
        /// This should only be called if the max increases - not for rebalancing or removals.
        /// </summary>
        /// <param name="node">The node to start propagating from</param>
        private static void PropagateIncrease(IntervalTreeNode<TK, TV> node)
        {
            TK max = node.Max;
            IntervalTreeNode<TK, TV> ptr = node;

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
        private static void PropagateFull(IntervalTreeNode<TK, TV> node)
        {
            IntervalTreeNode<TK, TV> ptr = node;

            do
            {
                TK max = ptr.End;

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
        /// <param name="updateFactoryCallback">Optional factory used to create a new value if <paramref name="start"/> is already on the tree</param>
        /// <param name="outNode">Node that was inserted or modified</param>
        /// <returns>True if <paramref name="start"/> was not yet on the tree, false otherwise</returns>
        private bool BSTInsert(TK start, TK end, TV value, Func<TK, TV, TV> updateFactoryCallback, out IntervalTreeNode<TK, TV> outNode)
        {
            IntervalTreeNode<TK, TV> parent = null;
            IntervalTreeNode<TK, TV> node = _root;

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
                    outNode = node;

                    if (updateFactoryCallback != null)
                    {
                        // Replace
                        node.Value = updateFactoryCallback(start, node.Value);

                        int endCmp = end.CompareTo(node.End);

                        if (endCmp > 0)
                        {
                            node.End = end;
                            if (end.CompareTo(node.Max) > 0)
                            {
                                node.Max = end;
                                PropagateIncrease(node);
                                RestoreBalanceAfterInsertion(node);
                            }
                        }
                        else if (endCmp < 0)
                        {
                            node.End = end;
                            PropagateFull(node);
                        }
                    }

                    return false;
                }
            }
            IntervalTreeNode<TK, TV> newNode = new(start, end, value, parent);
            if (newNode.Parent == null)
            {
                _root = newNode;
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
            _count++;
            RestoreBalanceAfterInsertion(newNode);
            outNode = newNode;
            return true;
        }

        /// <summary>
        /// Removes the value from the dictionary after searching for it with <paramref name="key"/>.
        /// </summary>
        /// <param name="key">Key to search for</param>
        /// <returns>Number of deleted values</returns>
        private int Delete(TK key)
        {
            IntervalTreeNode<TK, TV> nodeToDelete = GetNode(key);

            if (nodeToDelete == null)
            {
                return 0;
            }

            IntervalTreeNode<TK, TV> replacementNode;

            if (LeftOf(nodeToDelete) == null || RightOf(nodeToDelete) == null)
            {
                replacementNode = nodeToDelete;
            }
            else
            {
                replacementNode = PredecessorOf(nodeToDelete);
            }

            IntervalTreeNode<TK, TV> tmp = LeftOf(replacementNode) ?? RightOf(replacementNode);

            if (tmp != null)
            {
                tmp.Parent = ParentOf(replacementNode);
            }

            if (ParentOf(replacementNode) == null)
            {
                _root = tmp;
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
                nodeToDelete.Value = replacementNode.Value;
                nodeToDelete.End = replacementNode.End;
                nodeToDelete.Max = replacementNode.Max;
            }

            PropagateFull(replacementNode);

            if (tmp != null && ColorOf(replacementNode) == Black)
            {
                RestoreBalanceAfterRemoval(tmp);
            }

            return 1;
        }

        /// <summary>
        /// Returns the node with the largest key where <paramref name="node"/> is considered the root node.
        /// </summary>
        /// <param name="node">Root Node</param>
        /// <returns>Node with the maximum key in the tree of <paramref name="node"/></returns>
        private static IntervalTreeNode<TK, TV> Maximum(IntervalTreeNode<TK, TV> node)
        {
            IntervalTreeNode<TK, TV> tmp = node;
            while (tmp.Right != null)
            {
                tmp = tmp.Right;
            }

            return tmp;
        }

        /// <summary>
        /// Finds the node whose key is immediately less than <paramref name="node"/>.
        /// </summary>
        /// <param name="node">Node to find the predecessor of</param>
        /// <returns>Predecessor of <paramref name="node"/></returns>
        private static IntervalTreeNode<TK, TV> PredecessorOf(IntervalTreeNode<TK, TV> node)
        {
            if (node.Left != null)
            {
                return Maximum(node.Left);
            }
            IntervalTreeNode<TK, TV> parent = node.Parent;
            while (parent != null && node == parent.Left)
            {
                node = parent;
                parent = parent.Parent;
            }
            return parent;
        }

        #endregion

        #region Private Methods (RBL)

        private void RestoreBalanceAfterRemoval(IntervalTreeNode<TK, TV> balanceNode)
        {
            IntervalTreeNode<TK, TV> ptr = balanceNode;

            while (ptr != _root && ColorOf(ptr) == Black)
            {
                if (ptr == LeftOf(ParentOf(ptr)))
                {
                    IntervalTreeNode<TK, TV> sibling = RightOf(ParentOf(ptr));

                    if (ColorOf(sibling) == Red)
                    {
                        SetColor(sibling, Black);
                        SetColor(ParentOf(ptr), Red);
                        RotateLeft(ParentOf(ptr));
                        sibling = RightOf(ParentOf(ptr));
                    }
                    if (ColorOf(LeftOf(sibling)) == Black && ColorOf(RightOf(sibling)) == Black)
                    {
                        SetColor(sibling, Red);
                        ptr = ParentOf(ptr);
                    }
                    else
                    {
                        if (ColorOf(RightOf(sibling)) == Black)
                        {
                            SetColor(LeftOf(sibling), Black);
                            SetColor(sibling, Red);
                            RotateRight(sibling);
                            sibling = RightOf(ParentOf(ptr));
                        }
                        SetColor(sibling, ColorOf(ParentOf(ptr)));
                        SetColor(ParentOf(ptr), Black);
                        SetColor(RightOf(sibling), Black);
                        RotateLeft(ParentOf(ptr));
                        ptr = _root;
                    }
                }
                else
                {
                    IntervalTreeNode<TK, TV> sibling = LeftOf(ParentOf(ptr));

                    if (ColorOf(sibling) == Red)
                    {
                        SetColor(sibling, Black);
                        SetColor(ParentOf(ptr), Red);
                        RotateRight(ParentOf(ptr));
                        sibling = LeftOf(ParentOf(ptr));
                    }
                    if (ColorOf(RightOf(sibling)) == Black && ColorOf(LeftOf(sibling)) == Black)
                    {
                        SetColor(sibling, Red);
                        ptr = ParentOf(ptr);
                    }
                    else
                    {
                        if (ColorOf(LeftOf(sibling)) == Black)
                        {
                            SetColor(RightOf(sibling), Black);
                            SetColor(sibling, Red);
                            RotateLeft(sibling);
                            sibling = LeftOf(ParentOf(ptr));
                        }
                        SetColor(sibling, ColorOf(ParentOf(ptr)));
                        SetColor(ParentOf(ptr), Black);
                        SetColor(LeftOf(sibling), Black);
                        RotateRight(ParentOf(ptr));
                        ptr = _root;
                    }
                }
            }
            SetColor(ptr, Black);
        }

        private void RestoreBalanceAfterInsertion(IntervalTreeNode<TK, TV> balanceNode)
        {
            SetColor(balanceNode, Red);
            while (balanceNode != null && balanceNode != _root && ColorOf(ParentOf(balanceNode)) == Red)
            {
                if (ParentOf(balanceNode) == LeftOf(ParentOf(ParentOf(balanceNode))))
                {
                    IntervalTreeNode<TK, TV> sibling = RightOf(ParentOf(ParentOf(balanceNode)));

                    if (ColorOf(sibling) == Red)
                    {
                        SetColor(ParentOf(balanceNode), Black);
                        SetColor(sibling, Black);
                        SetColor(ParentOf(ParentOf(balanceNode)), Red);
                        balanceNode = ParentOf(ParentOf(balanceNode));
                    }
                    else
                    {
                        if (balanceNode == RightOf(ParentOf(balanceNode)))
                        {
                            balanceNode = ParentOf(balanceNode);
                            RotateLeft(balanceNode);
                        }
                        SetColor(ParentOf(balanceNode), Black);
                        SetColor(ParentOf(ParentOf(balanceNode)), Red);
                        RotateRight(ParentOf(ParentOf(balanceNode)));
                    }
                }
                else
                {
                    IntervalTreeNode<TK, TV> sibling = LeftOf(ParentOf(ParentOf(balanceNode)));

                    if (ColorOf(sibling) == Red)
                    {
                        SetColor(ParentOf(balanceNode), Black);
                        SetColor(sibling, Black);
                        SetColor(ParentOf(ParentOf(balanceNode)), Red);
                        balanceNode = ParentOf(ParentOf(balanceNode));
                    }
                    else
                    {
                        if (balanceNode == LeftOf(ParentOf(balanceNode)))
                        {
                            balanceNode = ParentOf(balanceNode);
                            RotateRight(balanceNode);
                        }
                        SetColor(ParentOf(balanceNode), Black);
                        SetColor(ParentOf(ParentOf(balanceNode)), Red);
                        RotateLeft(ParentOf(ParentOf(balanceNode)));
                    }
                }
            }
            SetColor(_root, Black);
        }

        private void RotateLeft(IntervalTreeNode<TK, TV> node)
        {
            if (node != null)
            {
                IntervalTreeNode<TK, TV> right = RightOf(node);
                node.Right = LeftOf(right);
                if (node.Right != null)
                {
                    node.Right.Parent = node;
                }
                IntervalTreeNode<TK, TV> nodeParent = ParentOf(node);
                right.Parent = nodeParent;
                if (nodeParent == null)
                {
                    _root = right;
                }
                else if (node == LeftOf(nodeParent))
                {
                    nodeParent.Left = right;
                }
                else
                {
                    nodeParent.Right = right;
                }
                right.Left = node;
                node.Parent = right;

                PropagateFull(node);
            }
        }

        private void RotateRight(IntervalTreeNode<TK, TV> node)
        {
            if (node != null)
            {
                IntervalTreeNode<TK, TV> left = LeftOf(node);
                node.Left = RightOf(left);
                if (node.Left != null)
                {
                    node.Left.Parent = node;
                }
                IntervalTreeNode<TK, TV> nodeParent = ParentOf(node);
                left.Parent = nodeParent;
                if (nodeParent == null)
                {
                    _root = left;
                }
                else if (node == RightOf(nodeParent))
                {
                    nodeParent.Right = left;
                }
                else
                {
                    nodeParent.Left = left;
                }
                left.Right = node;
                node.Parent = left;

                PropagateFull(node);
            }
        }

        #endregion

        #region Safety-Methods

        // These methods save memory by allowing us to forego sentinel nil nodes, as well as serve as protection against NullReferenceExceptions.

        /// <summary>
        /// Returns the color of <paramref name="node"/>, or Black if it is null.
        /// </summary>
        /// <param name="node">Node</param>
        /// <returns>The boolean color of <paramref name="node"/>, or black if null</returns>
        private static bool ColorOf(IntervalTreeNode<TK, TV> node)
        {
            return node == null || node.Color;
        }

        /// <summary>
        /// Sets the color of <paramref name="node"/> node to <paramref name="color"/>.
        /// <br></br>
        /// This method does nothing if <paramref name="node"/> is null.
        /// </summary>
        /// <param name="node">Node to set the color of</param>
        /// <param name="color">Color (Boolean)</param>
        private static void SetColor(IntervalTreeNode<TK, TV> node, bool color)
        {
            if (node != null)
            {
                node.Color = color;
            }
        }

        /// <summary>
        /// This method returns the left node of <paramref name="node"/>, or null if <paramref name="node"/> is null.
        /// </summary>
        /// <param name="node">Node to retrieve the left child from</param>
        /// <returns>Left child of <paramref name="node"/></returns>
        private static IntervalTreeNode<TK, TV> LeftOf(IntervalTreeNode<TK, TV> node)
        {
            return node?.Left;
        }

        /// <summary>
        /// This method returns the right node of <paramref name="node"/>, or null if <paramref name="node"/> is null.
        /// </summary>
        /// <param name="node">Node to retrieve the right child from</param>
        /// <returns>Right child of <paramref name="node"/></returns>
        private static IntervalTreeNode<TK, TV> RightOf(IntervalTreeNode<TK, TV> node)
        {
            return node?.Right;
        }

        /// <summary>
        /// Returns the parent node of <paramref name="node"/>, or null if <paramref name="node"/> is null.
        /// </summary>
        /// <param name="node">Node to retrieve the parent from</param>
        /// <returns>Parent of <paramref name="node"/></returns>
        private static IntervalTreeNode<TK, TV> ParentOf(IntervalTreeNode<TK, TV> node)
        {
            return node?.Parent;
        }

        #endregion

        public bool ContainsKey(TK key)
        {
            return GetNode(key) != null;
        }

        public void Clear()
        {
            _root = null;
            _count = 0;
        }
    }

    /// <summary>
    /// Represents a node in the IntervalTree which contains start and end keys of type K, and a value of generic type V.
    /// </summary>
    /// <typeparam name="TK">Key type of the node</typeparam>
    /// <typeparam name="TV">Value type of the node</typeparam>
    class IntervalTreeNode<TK, TV>
    {
        public bool Color = true;
        public IntervalTreeNode<TK, TV> Left = null;
        public IntervalTreeNode<TK, TV> Right = null;
        public IntervalTreeNode<TK, TV> Parent = null;

        /// <summary>
        /// The start of the range.
        /// </summary>
        public TK Start;

        /// <summary>
        /// The end of the range.
        /// </summary>
        public TK End;

        /// <summary>
        /// The maximum end value of this node and all its children.
        /// </summary>
        public TK Max;

        /// <summary>
        /// Value stored on this node.
        /// </summary>
        public TV Value;

        public IntervalTreeNode(TK start, TK end, TV value, IntervalTreeNode<TK, TV> parent)
        {
            Start = start;
            End = end;
            Max = end;
            Value = value;
            Parent = parent;
        }
    }
}
