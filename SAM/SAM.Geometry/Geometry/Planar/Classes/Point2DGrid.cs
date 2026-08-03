// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Collections.Generic;

namespace SAM.Geometry.Planar
{
    /// <summary>
    /// Operation-local uniform grid over <see cref="Point2D"/> instances, for proximity
    /// searches that used to rescan a growing or repeatedly enumerated point list.
    /// <para>
    /// The grid is a broad-phase index only: it never decides geometric equivalence.
    /// <see cref="Candidates"/> returns the indices of every point that could possibly lie
    /// within the grid tolerance of the query point (plus a margin of one cell against
    /// floating-point cell-boundary rounding), in ascending insertion order, so an exact
    /// predicate evaluated over the candidates sees the same matches in the same order as a
    /// full scan of the same points.
    /// </para>
    /// <para>
    /// Null and non-finite points cannot be quantised into cells; they are held aside and
    /// offered to every query. A null, non-finite or otherwise degenerate query falls back
    /// to the full index. The index is mutable: <see cref="Replace"/> keeps it synchronised
    /// when a caller moves a point, which workflows that average endpoints in place require.
    /// </para>
    /// </summary>
    internal sealed class Point2DGrid
    {
        private readonly double tolerance;
        private readonly double cellSize;
        private readonly List<Point2D> point2Ds;
        private readonly Dictionary<Tuple<long, long>, List<int>> dictionary;
        private readonly List<int> unbounded;

        public Point2DGrid(double tolerance)
        {
            this.tolerance = tolerance;
            cellSize = tolerance > 0 ? tolerance : Core.Tolerance.Distance;

            point2Ds = new List<Point2D>();
            dictionary = new Dictionary<Tuple<long, long>, List<int>>();
            unbounded = new List<int>();
        }

        public int Count
        {
            get
            {
                return point2Ds.Count;
            }
        }

        public Point2D this[int index]
        {
            get
            {
                return point2Ds[index];
            }
        }

        public int Add(Point2D point2D)
        {
            int index = point2Ds.Count;
            point2Ds.Add(point2D);

            Tuple<long, long> key = Key(point2D, cellSize);
            if (key == null)
            {
                unbounded.Add(index);
                return index;
            }

            if (!dictionary.TryGetValue(key, out List<int> list))
            {
                list = new List<int>();
                dictionary[key] = list;
            }

            list.Add(index);
            return index;
        }

        public void Replace(int index, Point2D point2D)
        {
            Point2D point2D_Old = point2Ds[index];

            Tuple<long, long> key_Old = Key(point2D_Old, cellSize);
            if (key_Old == null)
            {
                unbounded.Remove(index);
            }
            else if (dictionary.TryGetValue(key_Old, out List<int> list_Old))
            {
                list_Old.Remove(index);
            }

            point2Ds[index] = point2D;

            Tuple<long, long> key = Key(point2D, cellSize);
            if (key == null)
            {
                unbounded.Add(index);
                return;
            }

            if (!dictionary.TryGetValue(key, out List<int> list))
            {
                list = new List<int>();
                dictionary[key] = list;
            }

            list.Add(index);
        }

        /// <summary>
        /// Indices of every point that could lie within the grid tolerance of
        /// <paramref name="point2D"/>, in ascending insertion order. Degenerate queries
        /// (null, NaN or infinite coordinates, non-finite tolerance) return the whole index.
        /// </summary>
        public List<int> Candidates(Point2D point2D)
        {
            if (!IsFinite(point2D) || double.IsNaN(tolerance) || double.IsInfinity(tolerance))
            {
                return All();
            }

            double minX = point2D.X - tolerance;
            double maxX = point2D.X + tolerance;
            double minY = point2D.Y - tolerance;
            double maxY = point2D.Y + tolerance;

            if (!IsFinite(minX) || !IsFinite(maxX) || !IsFinite(minY) || !IsFinite(maxY))
            {
                return All();
            }

            // One cell of padding around the tolerance box: cell assignment involves a
            // floating-point division, so a point legitimately within tolerance can land one
            // cell outside the mathematically exact range at a cell boundary.
            long kx1 = (long)System.Math.Floor(minX / cellSize) - 1;
            long kx2 = (long)System.Math.Floor(maxX / cellSize) + 1;
            long ky1 = (long)System.Math.Floor(minY / cellSize) - 1;
            long ky2 = (long)System.Math.Floor(maxY / cellSize) + 1;

            List<int> result = new List<int>(unbounded);
            for (long kx = kx1; kx <= kx2; kx++)
            {
                for (long ky = ky1; ky <= ky2; ky++)
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

        private List<int> All()
        {
            List<int> result = new List<int>(point2Ds.Count);
            for (int i = 0; i < point2Ds.Count; i++)
            {
                result.Add(i);
            }

            return result;
        }

        private static Tuple<long, long> Key(Point2D point2D, double cellSize)
        {
            if (!IsFinite(point2D))
            {
                return null;
            }

            return new Tuple<long, long>(
                (long)System.Math.Floor(point2D.X / cellSize),
                (long)System.Math.Floor(point2D.Y / cellSize));
        }

        private static bool IsFinite(Point2D point2D)
        {
            if (point2D == null)
            {
                return false;
            }

            return IsFinite(point2D.X) && IsFinite(point2D.Y);
        }

        private static bool IsFinite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }
    }
}
