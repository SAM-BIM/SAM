// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

namespace SAM.Tests.GenOptOracle
{
    /// <summary>
    /// Model of GenOpt's result database: a sorted map (red-black tree, insertion as in Cormen,
    /// Leiserson, Rivest and Stein, "Introduction to Algorithms", RB-INSERT-FIXUP) keyed by points
    /// compared with a tolerance. Because the comparison is approximate (not transitive), which
    /// stored point a lookup finds depends on the tree's shape, so a plain hash or "first match"
    /// lookup is not equivalent. Lookup and insertion descend from the root comparing the query
    /// (left operand) with each node key; there is no deletion.
    /// </summary>
    internal sealed class ApproximateRedBlackMap<TValue>
    {
        private readonly Func<double[], double[], int> compare;
        private Node? root;

        public ApproximateRedBlackMap(Func<double[], double[], int> compare)
        {
            this.compare = compare;
        }

        private sealed class Node
        {
            public Node(double[] key, TValue value, Node? parent)
            {
                Key = key;
                Value = value;
                Parent = parent;
            }

            public double[] Key;
            public TValue Value;
            public Node? Left;
            public Node? Right;
            public Node? Parent;
            public bool Black = true;
        }

        public bool TryGetValue(double[] key, out TValue value)
        {
            Node? p = root;
            while (p is not null)
            {
                int cmp = compare(key, p.Key);
                if (cmp < 0)
                {
                    p = p.Left;
                }
                else if (cmp > 0)
                {
                    p = p.Right;
                }
                else
                {
                    value = p.Value;
                    return true;
                }
            }

            value = default!;
            return false;
        }

        public void Put(double[] key, TValue value)
        {
            if (root is null)
            {
                root = new Node(key, value, null);
                return;
            }

            Node t = root;
            Node parent;
            int cmp;
            do
            {
                parent = t;
                cmp = compare(key, t.Key);
                if (cmp < 0)
                {
                    t = t.Left!;
                }
                else if (cmp > 0)
                {
                    t = t.Right!;
                }
                else
                {
                    t.Value = value;
                    return;
                }
            }
            while (t is not null);

            Node node = new Node(key, value, parent);
            if (cmp < 0)
            {
                parent.Left = node;
            }
            else
            {
                parent.Right = node;
            }

            InsertFixup(node);
        }

        private static bool IsBlack(Node? n) => n is null || n.Black;

        private void InsertFixup(Node z)
        {
            z.Black = false;
            while (z != root && z.Parent is not null && !z.Parent.Black)
            {
                Node parent = z.Parent;
                Node? grand = parent.Parent;
                if (grand is null)
                {
                    break;
                }

                if (parent == grand.Left)
                {
                    Node? uncle = grand.Right;
                    if (!IsBlack(uncle))
                    {
                        parent.Black = true;
                        uncle!.Black = true;
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

                        z.Parent!.Black = true;
                        z.Parent.Parent!.Black = false;
                        RotateRight(z.Parent.Parent);
                    }
                }
                else
                {
                    Node? uncle = grand.Left;
                    if (!IsBlack(uncle))
                    {
                        parent.Black = true;
                        uncle!.Black = true;
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

                        z.Parent!.Black = true;
                        z.Parent.Parent!.Black = false;
                        RotateLeft(z.Parent.Parent);
                    }
                }
            }

            root!.Black = true;
        }

        private void RotateLeft(Node x)
        {
            Node y = x.Right!;
            x.Right = y.Left;
            if (y.Left is not null)
            {
                y.Left.Parent = x;
            }

            y.Parent = x.Parent;
            if (x.Parent is null)
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
            Node y = x.Left!;
            x.Left = y.Right;
            if (y.Right is not null)
            {
                y.Right.Parent = x;
            }

            y.Parent = x.Parent;
            if (x.Parent is null)
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
