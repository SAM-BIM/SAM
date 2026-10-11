// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;

namespace SAM.Math
{
    /// <summary>
    /// Result database of the optimisation kernel. It is a sorted map (red-black tree, insertion as in Cormen,
    /// Leiserson, Rivest and Stein, "Introduction to Algorithms", RB-INSERT-FIXUP) keyed by points that are
    /// compared with a relative tolerance of 1e-12 (<see cref="Compare"/>).
    /// <para>
    /// The comparison is approximate, so it is not transitive. Which stored point a lookup finds therefore depends
    /// on the tree's shape. A hash map, a quantised key or a "first match" scan is <b>not</b> equivalent
    /// (documentation/GenOpt-3.1.1-Behaviour.md §2.3, trace ec-gs-collapse). Lookup and insertion descend from the
    /// root, comparing the query (left operand) with each node's key. Entries are never removed. Not thread-safe.
    /// </para>
    /// </summary>
    public sealed class ApproximatePointCache<TValue>
    {
        private const double OneMinus = 1 - 1E-12;
        private const double OnePlus = 1 + 1E-12;

        private Node root;

        /// <summary>Number of stored points.</summary>
        public int Count { get; private set; }

        /// <summary>
        /// Coordinate equality: relative tolerance 1e-12 around <paramref name="x1"/>, mirrored for x1 ≤ 0, so 0
        /// equals only ±0.
        /// </summary>
        public static bool AreEqual(double x1, double x2)
        {
            if (x1 > 0)
            {
                return OnePlus * x1 >= x2 && OneMinus * x1 <= x2;
            }

            return OneMinus * x1 >= x2 && OnePlus * x1 <= x2;
        }

        /// <summary>
        /// Point order: coordinate by coordinate, the first coordinate that is not <see cref="AreEqual"/> decides,
        /// in descending order (a larger query coordinate sorts first). Returns 0 when every coordinate is equal.
        /// </summary>
        public static int Compare(double[] query, double[] stored)
        {
            if (query == null)
            {
                throw new ArgumentNullException(nameof(query));
            }

            if (stored == null)
            {
                throw new ArgumentNullException(nameof(stored));
            }

            if (query.Length != stored.Length)
            {
                throw new ArgumentException("Points must have the same dimension.", nameof(stored));
            }

            for (int i = 0; i < query.Length; i++)
            {
                if (!AreEqual(query[i], stored[i]))
                {
                    return query[i] > stored[i] ? -1 : 1;
                }
            }

            return 0;
        }

        /// <summary>Looks the point up by descending from the root.</summary>
        public bool TryGetValue(double[] point, out TValue value)
        {
            Node node = root;
            while (node != null)
            {
                int comparison = Compare(point, node.Key);
                if (comparison < 0)
                {
                    node = node.Left;
                }
                else if (comparison > 0)
                {
                    node = node.Right;
                }
                else
                {
                    value = node.Value;
                    return true;
                }
            }

            value = default(TValue);
            return false;
        }

        /// <summary>
        /// Stores a copy of the point. If the descent meets an equal key, that node's value is replaced and its key
        /// is kept.
        /// </summary>
        public void Put(double[] point, TValue value)
        {
            if (point == null)
            {
                throw new ArgumentNullException(nameof(point));
            }

            double[] key = (double[])point.Clone();
            if (root == null)
            {
                root = new Node(key, value, null);
                Count = 1;
                return;
            }

            Node node = root;
            Node parent;
            int comparison;
            do
            {
                parent = node;
                comparison = Compare(key, node.Key);
                if (comparison < 0)
                {
                    node = node.Left;
                }
                else if (comparison > 0)
                {
                    node = node.Right;
                }
                else
                {
                    node.Value = value;
                    return;
                }
            }
            while (node != null);

            Node inserted = new Node(key, value, parent);
            if (comparison < 0)
            {
                parent.Left = inserted;
            }
            else
            {
                parent.Right = inserted;
            }

            Count++;
            InsertFixup(inserted);
        }

        private sealed class Node
        {
            public Node(double[] key, TValue value, Node parent)
            {
                Key = key;
                Value = value;
                Parent = parent;
            }

            public readonly double[] Key;
            public TValue Value;
            public Node Left;
            public Node Right;
            public Node Parent;
            public bool Black = true;
        }

        private static bool IsBlack(Node node) => node == null || node.Black;

        private void InsertFixup(Node z)
        {
            z.Black = false;
            while (z != root && z.Parent != null && !z.Parent.Black)
            {
                Node parent = z.Parent;
                Node grand = parent.Parent;
                if (grand == null)
                {
                    break;
                }

                if (parent == grand.Left)
                {
                    Node uncle = grand.Right;
                    if (!IsBlack(uncle))
                    {
                        parent.Black = true;
                        uncle.Black = true;
                        grand.Black = false;
                        z = grand;
                    }
                    else
                    {
                        if (z == parent.Right)
                        {
                            z = parent;
                            RotateLeft(z);
                        }

                        z.Parent.Black = true;
                        z.Parent.Parent.Black = false;
                        RotateRight(z.Parent.Parent);
                    }
                }
                else
                {
                    Node uncle = grand.Left;
                    if (!IsBlack(uncle))
                    {
                        parent.Black = true;
                        uncle.Black = true;
                        grand.Black = false;
                        z = grand;
                    }
                    else
                    {
                        if (z == parent.Left)
                        {
                            z = parent;
                            RotateRight(z);
                        }

                        z.Parent.Black = true;
                        z.Parent.Parent.Black = false;
                        RotateLeft(z.Parent.Parent);
                    }
                }
            }

            root.Black = true;
        }

        private void RotateLeft(Node x)
        {
            Node y = x.Right;
            x.Right = y.Left;
            if (y.Left != null)
            {
                y.Left.Parent = x;
            }

            y.Parent = x.Parent;
            if (x.Parent == null)
            {
                root = y;
            }
            else if (x == x.Parent.Left)
            {
                x.Parent.Left = y;
            }
            else
            {
                x.Parent.Right = y;
            }

            y.Left = x;
            x.Parent = y;
        }

        private void RotateRight(Node x)
        {
            Node y = x.Left;
            x.Left = y.Right;
            if (y.Right != null)
            {
                y.Right.Parent = x;
            }

            y.Parent = x.Parent;
            if (x.Parent == null)
            {
                root = y;
            }
            else if (x == x.Parent.Right)
            {
                x.Parent.Right = y;
            }
            else
            {
                x.Parent.Left = y;
            }

            y.Right = x;
            x.Parent = y;
        }
    }
}
