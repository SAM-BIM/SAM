// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Collections.Generic;

namespace SAM.Geometry.Planar
{
    /// <summary>
    /// Operation-local uniform grid over <see cref="BoundingBox2D"/> instances, for
    /// growing result sets that used to be rescanned with an exact geometric predicate.
    /// <para>
    /// Insertion inflates each box by the grid tolerance and registers it in every cell it
    /// covers. A query probes the cells covering the uninflated query box, so an inserted
    /// box B is offered to a query box A whenever A is contained in B inflated by tolerance
    /// - the necessary condition for two geometries whose exact predicates require every
    /// bounding-box bound to agree within tolerance. One cell of padding on both operations
    /// covers floating-point cell-boundary rounding. Candidates arrive in ascending
    /// insertion order, so first-match behaviour against the kept set is unchanged.
    /// </para>
    /// <para>
    /// Boxes that are null, non-finite or would span an unreasonable number of cells are
    /// held aside and offered to every query, so no entry can silently disappear. A
    /// degenerate query falls back to the whole index. The grid never decides geometric
    /// equivalence; the exact predicate always has the final word.
    /// </para>
    /// </summary>
    internal sealed class BoundingBox2DGrid
    {
        private const int MaxCellsPerItem = 4096;

        private readonly double tolerance;
        private readonly double cellSize;
        private readonly List<BoundingBox2D> boundingBox2Ds;
        private readonly Dictionary<Tuple<long, long>, List<int>> dictionary;
        private readonly List<int> unbounded;

        /// <param name="tolerance">Exact-predicate tolerance; boxes are inflated by it on insert.</param>
        /// <param name="cellSizeHint">
        /// Typical box extent, e.g. the median extent of the indexed set. The grid is correct
        /// for any cell size - containment, not the cell size, defines the candidate superset -
        /// but a cell size far below the box size would make every box span thousands of cells.
        /// </param>
        public BoundingBox2DGrid(double tolerance, double cellSizeHint = 0)
        {
            this.tolerance = tolerance;
            cellSize = tolerance > 0 ? tolerance : Core.Tolerance.Distance;
            if (cellSizeHint > cellSize)
            {
                cellSize = cellSizeHint;
            }

            boundingBox2Ds = new List<BoundingBox2D>();
            dictionary = new Dictionary<Tuple<long, long>, List<int>>();
            unbounded = new List<int>();
        }

        public int Count
        {
            get
            {
                return boundingBox2Ds.Count;
            }
        }

        public BoundingBox2D this[int index]
        {
            get
            {
                return boundingBox2Ds[index];
            }
        }

        public int Add(BoundingBox2D boundingBox2D)
        {
            int index = boundingBox2Ds.Count;
            boundingBox2Ds.Add(boundingBox2D);

            if (!IsFinite(boundingBox2D))
            {
                unbounded.Add(index);
                return index;
            }

            Cells(boundingBox2D, tolerance, out long kx1, out long kx2, out long ky1, out long ky2);

            if (!IsValid(kx1, kx2, ky1, ky2) || CellCount(kx1, kx2, ky1, ky2) > MaxCellsPerItem)
            {
                unbounded.Add(index);
                return index;
            }

            for (long kx = kx1 - 1; kx <= kx2 + 1; kx++)
            {
                for (long ky = ky1 - 1; ky <= ky2 + 1; ky++)
                {
                    Tuple<long, long> key = new Tuple<long, long>(kx, ky);
                    if (!dictionary.TryGetValue(key, out List<int> list))
                    {
                        list = new List<int>();
                        dictionary[key] = list;
                    }

                    list.Add(index);
                }
            }

            return index;
        }

        /// <summary>
        /// Indices of every inserted box that could contain the query box within the grid
        /// tolerance, in ascending insertion order. Degenerate queries (null, NaN or
        /// infinite bounds, non-finite tolerance) return the whole index.
        /// </summary>
        public List<int> Candidates(BoundingBox2D boundingBox2D)
        {
            if (!IsFinite(boundingBox2D) || double.IsNaN(tolerance) || double.IsInfinity(tolerance))
            {
                return All();
            }

            Cells(boundingBox2D, 0, out long kx1, out long kx2, out long ky1, out long ky2);

            if (!IsValid(kx1, kx2, ky1, ky2) || CellCount(kx1, kx2, ky1, ky2) > MaxCellsPerItem)
            {
                // The query box cannot be quantised or spans an unreasonable number of cells -
                // fall back to the whole index rather than walking billions of cells.
                return All();
            }

            List<int> result = new List<int>(unbounded);
            for (long kx = kx1 - 1; kx <= kx2 + 1; kx++)
            {
                for (long ky = ky1 - 1; ky <= ky2 + 1; ky++)
                {
                    if (dictionary.TryGetValue(new Tuple<long, long>(kx, ky), out List<int> list))
                    {
                        result.AddRange(list);
                    }
                }
            }

            result.Sort();
            return result;
        }

        /// <summary>
        /// Median of the maximum extents of the finite boxes, for the cellSizeHint parameter.
        /// </summary>
        public static double CellSizeHint(IEnumerable<BoundingBox2D> boundingBox2Ds)
        {
            List<double> extents = new List<double>();
            foreach (BoundingBox2D boundingBox2D in boundingBox2Ds)
            {
                if (!IsFinite(boundingBox2D))
                {
                    continue;
                }

                extents.Add(System.Math.Max(boundingBox2D.Max.X - boundingBox2D.Min.X, boundingBox2D.Max.Y - boundingBox2D.Min.Y));
            }

            if (extents.Count == 0)
            {
                return 0;
            }

            extents.Sort();
            return extents[extents.Count / 2];
        }

        private List<int> All()
        {
            List<int> result = new List<int>(boundingBox2Ds.Count);
            for (int i = 0; i < boundingBox2Ds.Count; i++)
            {
                result.Add(i);
            }

            return result;
        }

        private void Cells(BoundingBox2D boundingBox2D, double inflation, out long kx1, out long kx2, out long ky1, out long ky2)
        {
            kx1 = (long)System.Math.Floor((boundingBox2D.Min.X - inflation) / cellSize);
            kx2 = (long)System.Math.Floor((boundingBox2D.Max.X + inflation) / cellSize);
            ky1 = (long)System.Math.Floor((boundingBox2D.Min.Y - inflation) / cellSize);
            ky2 = (long)System.Math.Floor((boundingBox2D.Max.Y + inflation) / cellSize);
        }

        private static bool IsFinite(BoundingBox2D boundingBox2D)
        {
            Point2D min = boundingBox2D?.Min;
            Point2D max = boundingBox2D?.Max;

            if (min == null || max == null)
            {
                return false;
            }

            return IsFinite(min.X) && IsFinite(min.Y) && IsFinite(max.X) && IsFinite(max.Y);
        }

        private static bool IsFinite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }

        private static bool IsValid(long kx1, long kx2, long ky1, long ky2)
        {
            return kx2 >= kx1 && ky2 >= ky1;
        }

        private static double CellCount(long kx1, long kx2, long ky1, long ky2)
        {
            // Double arithmetic on purpose: the long product can overflow before the guard sees it.
            return ((double)kx2 - kx1 + 1.0) * ((double)ky2 - ky1 + 1.0);
        }
    }
}
