// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Collections.Generic;

namespace SAM.Geometry.Spatial
{
    public static partial class Query
    {
        public static List<Segment3D> NakedSegment3Ds(this Shell shell, int maxCount = int.MaxValue, double tolerance = Core.Tolerance.Distance)
        {
            if (shell == null)
            {
                return null;
            }

            Shell shell_Temp = new Shell(shell);
            shell_Temp.SplitEdges(tolerance);

            List<Face3D> face3Ds = shell_Temp.Face3Ds;
            if (face3Ds == null)
            {
                return null;
            }

            List<Segment3D> result = new List<Segment3D>();

            List<Tuple<int, Segment3D>> tuples = new List<Tuple<int, Segment3D>>();
            for (int i = 0; i < face3Ds.Count; i++)
            {
                Face3D face3D = face3Ds[i];

                ISegmentable3D segmentable3D = face3D?.GetExternalEdge3D() as ISegmentable3D;
                if (segmentable3D != null)
                {
                    List<Segment3D> segment3Ds = segmentable3D.GetSegments();
                    if (segment3Ds != null)
                    {
                        foreach (Segment3D segment3D in segment3Ds)
                        {
                            if (segment3D == null || segment3D.GetLength() < tolerance)
                            {
                                continue;
                            }

                            tuples.Add(new Tuple<int, Segment3D>(i, segment3D));
                        }
                    }
                }

                List<ISegmentable3D> segmentable3Ds_Internal = face3D?.GetInternalEdge3Ds()?.FindAll(x => x is ISegmentable3D)?.ConvertAll(x => (ISegmentable3D)x);
                if (segmentable3Ds_Internal != null)
                {
                    foreach (ISegmentable3D segmentable3D_Internal in segmentable3Ds_Internal)
                    {
                        List<Segment3D> segment3Ds = segmentable3D_Internal?.GetSegments();
                        if (segment3Ds == null)
                        {
                            continue;
                        }

                        foreach (Segment3D segment3D in segment3Ds)
                        {
                            if (segment3D == null || segment3D.GetLength() < tolerance)
                            {
                                continue;
                            }

                            tuples.Add(new Tuple<int, Segment3D>(i, segment3D));
                        }
                    }
                }
            }

            if (tuples.Count == 0)
            {
                return result;
            }

            double cellSize = tolerance > 0 ? tolerance : Core.Tolerance.Distance;

            Tuple<long, long, long> CellKey(Point3D point3D)
            {
                return new Tuple<long, long, long>(
                    (long)System.Math.Floor(point3D.X / cellSize),
                    (long)System.Math.Floor(point3D.Y / cellSize),
                    (long)System.Math.Floor(point3D.Z / cellSize));
            }

            Dictionary<Tuple<long, long, long>, List<Tuple<int, int>>> dictionary = new Dictionary<Tuple<long, long, long>, List<Tuple<int, int>>>();
            for (int i = 0; i < tuples.Count; i++)
            {
                Segment3D segment3D = tuples[i].Item2;
                for (int j = 0; j < 2; j++)
                {
                    Tuple<long, long, long> key = CellKey(segment3D[j]);
                    if (!dictionary.TryGetValue(key, out List<Tuple<int, int>> list))
                    {
                        list = new List<Tuple<int, int>>();
                        dictionary[key] = list;
                    }

                    list.Add(new Tuple<int, int>(i, j));
                }
            }

            bool[] shared = new bool[tuples.Count];
            for (int i = 0; i < tuples.Count; i++)
            {
                if (shared[i])
                {
                    continue;
                }

                Segment3D segment3D = tuples[i].Item2;
                int faceIndex = tuples[i].Item1;

                for (int j = 0; j < 2 && !shared[i]; j++)
                {
                    Point3D point3D = segment3D[j];
                    Point3D point3D_Opposite = segment3D[1 - j];

                    Tuple<long, long, long> key = CellKey(point3D);

                    for (long dx = -1; dx <= 1 && !shared[i]; dx++)
                    {
                        for (long dy = -1; dy <= 1 && !shared[i]; dy++)
                        {
                            for (long dz = -1; dz <= 1 && !shared[i]; dz++)
                            {
                                if (!dictionary.TryGetValue(new Tuple<long, long, long>(key.Item1 + dx, key.Item2 + dy, key.Item3 + dz), out List<Tuple<int, int>> list))
                                {
                                    continue;
                                }

                                foreach (Tuple<int, int> tuple in list)
                                {
                                    int index = tuple.Item1;
                                    if (index == i || tuples[index].Item1 == faceIndex)
                                    {
                                        continue;
                                    }

                                    Segment3D segment3D_Candidate = tuples[index].Item2;
                                    if (point3D.Distance(segment3D_Candidate[tuple.Item2]) > tolerance)
                                    {
                                        continue;
                                    }

                                    if (point3D_Opposite.Distance(segment3D_Candidate[1 - tuple.Item2]) > tolerance)
                                    {
                                        continue;
                                    }

                                    shared[i] = true;
                                    shared[index] = true;
                                    break;
                                }
                            }
                        }
                    }
                }

                if (!shared[i])
                {
                    result.Add(segment3D);

                    if (result.Count >= maxCount)
                    {
                        return result;
                    }
                }
            }

            return result;
        }
    }
}
