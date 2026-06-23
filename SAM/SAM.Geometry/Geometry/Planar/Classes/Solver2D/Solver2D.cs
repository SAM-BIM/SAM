// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Collections.Generic;

namespace SAM.Geometry.Planar
{
    public class Solver2D
    {
        private List<Solver2DData> solver2DDatas;
        private List<IClosed2D> obstacles2D;
        private IClosed2D area;

        public Solver2D(IClosed2D area, List<IClosed2D> obstacles2D)
        {
            this.area = area;
            this.obstacles2D = obstacles2D;
        }


        public bool Add(Solver2DData solver2DData)
        {
            if (solver2DData == null || solver2DData.Geometry2D<ISAMGeometry2D>() == null || solver2DData.Closed2D<IClosed2D>() == null)
            {
                return false;
            }
            if (solver2DDatas == null)
            {
                solver2DDatas = new List<Solver2DData>();
            }

            solver2DDatas.Add(solver2DData);
            return true;
        }
        public bool AddRange(List<Solver2DData> solver2DDatas)
        {
            if (solver2DDatas == null)
            {
                return false;
            }

            solver2DDatas.ForEach(x => Add(x));
            return true;
        }

        public List<Solver2DResult> Solve()
        {
            if (solver2DDatas == null || solver2DDatas.Count == 0)
            {
                return null;
            }

            List<Solver2DResult> result = new List<Solver2DResult>();
            // Apply priority order

            solver2DDatas.Sort((x, y) => x.Priority.CompareTo(y.Priority));

            // Spatial index over already-placed rectangles. Without it Solve() is ~O(N^2): every one of
            // the up-to IterationCount*8 candidate positions per label linearly scans every previously
            // placed label (see intersect), which is ~150 s on a ~10k-label floor plan. The grid returns
            // a superset of potential overlaps - all placed rectangles whose cells the candidate's
            // bounding box touches, plus a one-cell halo - and the exact InRange test in intersect is
            // unchanged, so placement results are identical to the linear scan. Built only above a size
            // threshold so small inputs (e.g. Mollier chart labels) keep the original path byte-for-byte.
            RectangleGrid grid = solver2DDatas.Count > 256 ? RectangleGrid.Create(solver2DDatas) : null;

            // Degenerate-layout backstop. Each label that cannot be placed first runs its full
            // IterationCount * 8 candidate sweep before giving up; when a whole batch is unplaceable (e.g. a
            // floor-plan section taken at the wrong elevation collapses every space to a sliver, so no label
            // centre fits its LimitArea) that is an O(N * IterationCount) blow-up - a ~2-minute hang on a 10k
            // -label plan. A long run of consecutive failures means the layout is degenerate, so once it is
            // hit we stop sweeping and give each remaining label a single anchor attempt. The counter resets
            // on any successful placement, so a normal plan with the odd unplaceable label is unaffected.
            const int maxConsecutiveUnplaced = 32;
            int consecutiveUnplaced = 0;

            // Hard wall-clock safety cap. The consecutive-unplaced backstop only catches the case where labels
            // *fail* to place; a degenerate layout can also be slow while every label *succeeds* - e.g. when all
            // anchors collapse onto the same point, each label still places but only after spiralling out past a
            // growing pile of already-placed rectangles (O(N^2)). This budget bounds the whole solve regardless of
            // the mechanism: once exceeded, the remaining labels skip the search and fall back to their anchor.
            // A full 10k-label plan solves in well under this, so a normal solve never reaches it.
            const double budgetMilliseconds = 5000;
            System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();

            foreach (Solver2DData solver2DData in solver2DDatas)
            {
                Rectangle2D rectangle2D = solver2DData.Closed2D<Rectangle2D>();
                Solver2DSettings solver2DSettings = solver2DData.Solver2DSettings;
                if (rectangle2D == null)
                {
                    throw new System.NotImplementedException();
                }
                Rectangle2D resultRectangle2D = null;

                ISAMGeometry2D sAMGeometry2D = solver2DData.Geometry2D<ISAMGeometry2D>();
                // With a non-positive ShiftDistance the candidate offset (StartingDistance + i * ShiftDistance)
                // does not grow with i, so every iteration tests the same positions - one pass is enough and
                // repeating it is pure cost. Guards a degenerate caller from an IterationCount-fold blow-up.
                double iterationCount = solver2DSettings.ShiftDistance > 0 ? solver2DSettings.IterationCount : 1;

                // Degenerate layout already detected (see maxConsecutiveUnplaced): skip the full sweep and
                // make a single anchor attempt for the rest, so the whole solve stays bounded.
                if (consecutiveUnplaced >= maxConsecutiveUnplaced)
                {
                    iterationCount = 1;
                }

                // Over the wall-clock budget: stop searching entirely and anchor every remaining label.
                if (stopwatch.Elapsed.TotalMilliseconds > budgetMilliseconds)
                {
                    iterationCount = 0;
                }

                if (sAMGeometry2D is Point2D)
                {
                    Point2D point2D = (Point2D)sAMGeometry2D;
                    Rectangle2D rectangle2DWithGivenPointInCenter = rectangle2D.GetMoved(new Vector2D(rectangle2D.GetCentroid(), point2D));
                    List<Vector2D> offsets = generateOffsets();

                    for (int i = 0; i < iterationCount; i++)
                    {
                        if (resultRectangle2D != null) break;

                        foreach (Vector2D offset in offsets)
                        {
                            Vector2D scaledOffset = offset * (solver2DSettings.StartingDistance + (i * solver2DSettings.ShiftDistance));
                            Rectangle2D rectangleTemp = rectangle2DWithGivenPointInCenter.GetMoved(scaledOffset);

                            if (area.Inside(rectangleTemp) && !intersect(rectangleTemp, result, grid))
                            {
                                if (solver2DSettings.LimitArea != null && !solver2DSettings.LimitArea.Inside(rectangleTemp.GetCentroid()))
                                {
                                    continue;
                                }
                                resultRectangle2D = rectangleTemp;
                                break;
                            }
                        }
                    }
                }
                else if (sAMGeometry2D is Polyline2D)
                {
                    Polyline2D polyline2D = (Polyline2D)sAMGeometry2D;
                    List<Segment2D> segment2Ds = polyline2D.GetSegments();
                    Point2D point = polyline2D.Closest(rectangle2D.GetCentroid());
                    double distanceToCenter = point.Distance(rectangle2D.GetCentroid());

                    for (int i = 0; i < iterationCount; i++)
                    {
                        if (resultRectangle2D != null) break;

                        for (int j = -1; j <= 1; j += 2)
                        {
                            double xNew = point.X + i * j * solver2DSettings.ShiftDistance;
                            double yNew = getY(polyline2D, xNew);
                            if (double.IsNaN(yNew))
                            {
                                continue;
                            }
                            Point2D newPoint = new Point2D(xNew, yNew);

                            List<Segment2D> segments = polyline2D.ClosestSegment2Ds(newPoint);
                            if (segments == null) continue;

                            Segment2D segment = segments[0];
                            bool clockwise = segment.Direction.GetPerpendicular().Y < 0;


                            Rectangle2D calculatedRectangle = Query.MoveToSegment2D(rectangle2D, segment, newPoint, distanceToCenter, clockwise);
                            Rectangle2D rectangleTemp = fix(Query.MoveToSegment2D(rectangle2D, segment, newPoint, distanceToCenter, clockwise), rectangle2D);


                            if (area.Inside(rectangleTemp) && !intersect(rectangleTemp, result, grid))
                            {
                                if (solver2DSettings.LimitArea != null && !solver2DSettings.LimitArea.Inside(rectangleTemp.GetCentroid()))
                                {
                                    continue;
                                }
                                resultRectangle2D = rectangleTemp;
                                break;
                            }
                        }
                    }
                }
                else
                {
                    throw new System.NotImplementedException();
                }

                result.Add(new Solver2DResult(solver2DData, resultRectangle2D));

                // Track consecutive failures for the degenerate-layout backstop above; any success resets it.
                if (resultRectangle2D == null)
                {
                    consecutiveUnplaced++;
                }
                else
                {
                    consecutiveUnplaced = 0;
                }

                // Mirror the placed rectangle into the spatial index for subsequent labels' overlap
                // tests. Unplaced labels (null) carry no footprint, exactly as the linear scan treats them.
                if (grid != null && resultRectangle2D != null)
                {
                    grid.Add(resultRectangle2D);
                }
            }

            return result;
        }


        private double getY(Polyline2D polyLine2D, double x)
        {
            List<Segment2D> polyLine2DSegments = polyLine2D.Segment2Ds();
            Segment2D resultSegment = null;

            foreach (Segment2D segment in polyLine2DSegments)
            {
                if (segment.Min.X <= x && x <= segment.Max.X)
                {
                    resultSegment = segment;
                    break;
                }
            }
            if (resultSegment == null) return double.NaN;

            List<Point2D> points = resultSegment.GetPoints();
            if (points == null || points.Count < 2) return double.NaN;

            Math.LinearEquation linearEquation = Math.Create.LinearEquation(points[0].X, points[0].Y, points[1].X, points[1].Y);
            if (linearEquation == null) return double.NaN;

            return linearEquation.Evaluate(x);
        }

        /// <summary>
        /// Generates unit vectors in 8 directions (angles: 0, 45, 90, 135...)
        /// </summary>
        /// <returns>List of offsets</returns>        
        private List<Vector2D> generateOffsets()
        {
            List<Vector2D> offsets = new List<Vector2D>();

            double offsetAngle = 90;
            for (double angle = 0; angle < 360; angle += offsetAngle)
            {
                double radians = System.Math.PI * angle / 180;
                double offsetX = System.Math.Sin(radians);
                double offsetY = System.Math.Cos(radians);

                offsets.Add(new Vector2D(offsetX, offsetY));
            }

            for (double angle = 45; angle < 360; angle += offsetAngle)
            {
                double radians = System.Math.PI * angle / 180; ;
                double offsetX = System.Math.Sin(radians);
                double offsetY = System.Math.Cos(radians);

                offsets.Add(new Vector2D(offsetX, offsetY));
            }

            return offsets;
        }
        private Rectangle2D fix(Rectangle2D calculatedRectangle, Rectangle2D defaultRectangle)
        {
            if (calculatedRectangle == null || defaultRectangle == null)
            {
                return calculatedRectangle;
            }
            if (System.Math.Abs(defaultRectangle.Width - calculatedRectangle.Width) < Core.Tolerance.MacroDistance)
            {
                return calculatedRectangle;
            }

            Rectangle2D result = new Rectangle2D(calculatedRectangle.Origin, -calculatedRectangle.Height, calculatedRectangle.Width, calculatedRectangle.WidthDirection);
            return result;
        }
        private bool intersect(Rectangle2D rectangle2D, List<Solver2DResult> solver2DResults, RectangleGrid grid)
        {
            if (obstacles2D.Find(x => x.InRange(rectangle2D) == true) != null)
            {
                return true;
            }

            if (grid != null)
            {
                // Only the placed rectangles near rectangle2D can overlap it; the grid yields that set
                // and the InRange test below is the same as the linear path, so the outcome is identical.
                foreach (Rectangle2D placed in grid.Query(rectangle2D))
                {
                    if (placed.InRange(rectangle2D) == true || rectangle2D.InRange(placed) == true)
                    {
                        return true;
                    }
                }

                return false;
            }

            return (solver2DResults.Find(x => x.Closed2D<Rectangle2D>().InRange(rectangle2D) == true) != null) ||
                    (solver2DResults.Find(x => rectangle2D.InRange(x.Closed2D<Rectangle2D>()) == true) != null);
        }

        // Uniform-grid spatial index over placed label rectangles, keyed by their (tolerance-expanded)
        // bounding-box cells. A rectangle is inserted into every cell its box overlaps; a query returns
        // every rectangle in the cells the query box overlaps plus a one-cell halo. Two rectangles can
        // only be InRange if their boxes overlap (within tolerance), so an overlapping pair always shares
        // a queried cell - the index never drops a real overlap, only skips the far-apart ones the linear
        // scan would have tested and rejected. Cell size only affects speed, not correctness.
        private sealed class RectangleGrid
        {
            private readonly double cellSize;
            private readonly Dictionary<long, List<Rectangle2D>> cells = new Dictionary<long, List<Rectangle2D>>();

            private RectangleGrid(double cellSize)
            {
                this.cellSize = cellSize;
            }

            public static RectangleGrid Create(List<Solver2DData> solver2DDatas)
            {
                double maxDimension = 0;
                foreach (Solver2DData solver2DData in solver2DDatas)
                {
                    Rectangle2D rectangle2D = solver2DData?.Closed2D<Rectangle2D>();
                    BoundingBox2D boundingBox2D = rectangle2D?.GetBoundingBox();
                    if (boundingBox2D == null)
                    {
                        continue;
                    }

                    maxDimension = System.Math.Max(maxDimension, System.Math.Max(boundingBox2D.Width, boundingBox2D.Height));
                }

                // No usable footprint to size the grid by - let the caller fall back to the linear scan.
                return maxDimension > Core.Tolerance.Distance ? new RectangleGrid(maxDimension) : null;
            }

            public void Add(Rectangle2D rectangle2D)
            {
                if (!range(rectangle2D, out long minX, out long minY, out long maxX, out long maxY))
                {
                    return;
                }

                for (long x = minX; x <= maxX; x++)
                {
                    for (long y = minY; y <= maxY; y++)
                    {
                        long key = (x << 32) ^ (y & 0xffffffffL);
                        if (!cells.TryGetValue(key, out List<Rectangle2D> list))
                        {
                            list = new List<Rectangle2D>();
                            cells[key] = list;
                        }

                        list.Add(rectangle2D);
                    }
                }
            }

            public IEnumerable<Rectangle2D> Query(Rectangle2D rectangle2D)
            {
                if (!range(rectangle2D, out long minX, out long minY, out long maxX, out long maxY))
                {
                    yield break;
                }

                HashSet<Rectangle2D> seen = new HashSet<Rectangle2D>();

                // One-cell halo: absorbs the InRange tolerance and any box that straddles a cell border.
                for (long x = minX - 1; x <= maxX + 1; x++)
                {
                    for (long y = minY - 1; y <= maxY + 1; y++)
                    {
                        if (cells.TryGetValue((x << 32) ^ (y & 0xffffffffL), out List<Rectangle2D> list))
                        {
                            foreach (Rectangle2D placed in list)
                            {
                                if (seen.Add(placed))
                                {
                                    yield return placed;
                                }
                            }
                        }
                    }
                }
            }

            private bool range(Rectangle2D rectangle2D, out long minX, out long minY, out long maxX, out long maxY)
            {
                minX = minY = maxX = maxY = 0;

                BoundingBox2D boundingBox2D = rectangle2D?.GetBoundingBox(Core.Tolerance.Distance);
                if (boundingBox2D == null)
                {
                    return false;
                }

                Point2D min = boundingBox2D.Min;
                Point2D max = boundingBox2D.Max;
                minX = (long)System.Math.Floor(min.X / cellSize);
                minY = (long)System.Math.Floor(min.Y / cellSize);
                maxX = (long)System.Math.Floor(max.X / cellSize);
                maxY = (long)System.Math.Floor(max.Y / cellSize);
                return true;
            }
        }

    }
}
