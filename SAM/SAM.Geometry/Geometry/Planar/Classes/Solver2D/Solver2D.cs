// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Collections.Generic;

namespace SAM.Geometry.Planar
{
    public class Solver2D
    {
        /// <summary>
        /// Default value of <see cref="WorkBudget"/>: the number of geometric comparisons a whole solve
        /// may make before the remaining items are dropped at their anchors.
        /// <para>
        /// This replaced a 10 000 ms wall-clock budget. The behaviour it bounds is the same, but a
        /// drawing's layout must not depend on how fast the machine that drew it is: with a stopwatch, the
        /// same saved view solved on a loaded laptop and on a build server could return different
        /// positions, and there is no way for either to know that happened. A count of comparisons is
        /// derived only from the input, so an identical input always produces an identical layout.
        /// </para>
        /// <para>
        /// Calibrated by measuring <see cref="WorkUnits"/> on the shapes the two existing consumers
        /// produce, at the floor plan's own <c>IterationCount</c> of 100 with a <c>LimitArea</c> per label:
        /// </para>
        /// <list type="bullet">
        /// <item>healthy plan, 5 000 space labels on a room-sized grid: <b>9 900</b> units, 0.4 s;</item>
        /// <item>healthy plan, 2 000 labels: <b>3 960</b> units - the cost is linear in the label count
        /// while each label places near its anchor, so a 10 000-label plan is around 20 000;</item>
        /// <item>degenerate collapse, 400 labels sharing one anchor: <b>620 263</b> units, 14.6 s - each
        /// label places, but only after spiralling out past the pile already there, and the cost grows
        /// with the square of the count.</item>
        /// </list>
        /// <para>
        /// So this sits more than an order of magnitude above the healthy case - which therefore never
        /// reaches it, and a test locks that - and bites into the degenerate one at roughly the point in
        /// time the 10 000 ms stopwatch used to. Time per unit is not constant (a candidate near a large
        /// pile costs more than one in open space), so the equivalence with the old budget is approximate
        /// by construction; that is the price of the layout not depending on the machine, and it is worth
        /// paying.
        /// </para>
        /// </summary>
        public const long DefaultWorkBudget = 500000;

        private List<Solver2DData> solver2DDatas;
        private List<IClosed2D> obstacles2D;
        private IClosed2D area;
        private long workBudget = DefaultWorkBudget;
        private long workUnits = 0;

        public Solver2D(IClosed2D area, List<IClosed2D> obstacles2D)
        {
            this.area = area;
            this.obstacles2D = obstacles2D;
        }

        /// <summary>
        /// Geometric comparisons the next <see cref="Solve"/> may make before it stops searching and drops
        /// each remaining item at its anchor as a <see cref="Solver2DResultType.Fallback"/>. Defaults to
        /// <see cref="DefaultWorkBudget"/>.
        /// <para>
        /// Deliberately a count and not a duration - see <see cref="DefaultWorkBudget"/>. A non-positive
        /// value removes the budget entirely, which leaves only the degenerate-layout backstop bounding the
        /// solve; use it only where the input size is known.
        /// </para>
        /// </summary>
        public long WorkBudget
        {
            get
            {
                return workBudget;
            }

            set
            {
                workBudget = value;
            }
        }

        /// <summary>
        /// Geometric comparisons the last <see cref="Solve"/> made. Deterministic for a given input, which
        /// is what makes <see cref="WorkBudget"/> testable and lets a consumer log how close a real model
        /// comes to it.
        /// </summary>
        public long WorkUnits
        {
            get
            {
                return workUnits;
            }
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

            workUnits = 0;

            // Placement order, lowest Priority first, then the order the caller added the items in. It used
            // to be solver2DDatas.Sort(...) on Priority alone, which is List<T>.Sort - an unstable introsort
            // - so items of EQUAL priority were placed in an arbitrary order that varied with the number of
            // items. Placement order decides the layout (each item avoids the ones already placed), so two
            // solves of one saved drawing could return different positions. Every consumer here leaves
            // Priority at its default, i.e. all items are equal, so this was the normal case rather than an
            // edge one. Ordering by index as the tiebreak makes the comparison total, which removes the
            // dependency on the sort's stability altogether. Note the field itself is no longer re-ordered,
            // so a second Solve() of the same instance starts from the same order as the first.
            List<Solver2DData> solver2DDatas_Ordered = ordered();

            // Spatial index over already-placed rectangles. Without it Solve() is ~O(N^2): every one of
            // the up-to IterationCount*8 candidate positions per label linearly scans every previously
            // placed label (see intersect), which is ~150 s on a ~10k-label floor plan. The grid returns
            // a superset of potential overlaps - all placed rectangles whose cells the candidate's
            // bounding box touches, plus a one-cell halo - and the exact InRange test in intersect is
            // unchanged, so placement results are identical to the linear scan. Built only above a size
            // threshold so small inputs (e.g. Mollier chart labels) keep the original path byte-for-byte.
            RectangleGrid grid = solver2DDatas_Ordered.Count > 256 ? RectangleGrid.Create(solver2DDatas_Ordered) : null;

            // Degenerate-layout backstop. Each label that cannot be placed first runs its full
            // IterationCount * 8 candidate sweep before giving up; when a whole batch is unplaceable (e.g. a
            // floor-plan section taken at the wrong elevation collapses every space to a sliver, so no label
            // centre fits its LimitArea) that is an O(N * IterationCount) blow-up - a ~2-minute hang on a 10k
            // -label plan. A long run of consecutive failures means the layout is degenerate, so once it is
            // hit we stop sweeping and give each remaining label a single anchor attempt. The counter resets
            // on any successful placement, so a normal plan with the odd unplaceable label is unaffected.
            const int maxConsecutiveUnplaced = 32;
            int consecutiveUnplaced = 0;

            // The 8 search directions are identical for every label, so build them once rather than per label.
            List<Vector2D> offsets = generateOffsets();

            foreach (Solver2DData solver2DData in solver2DDatas_Ordered)
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

                // Hard safety cap on the whole solve. The consecutive-unplaced backstop only catches the case
                // where labels *fail* to place; a degenerate layout can also be slow while every label
                // *succeeds* - e.g. when all anchors collapse onto the same point, each label still places but
                // only after spiralling out past a growing pile of already-placed rectangles (O(N^2)). This
                // budget bounds the solve regardless of the mechanism: once exceeded, the remaining labels
                // skip the search and are placed AT their anchor (visible, possibly overlapping) rather than
                // dropped, because a consumer blanks an unplaced (null) label and tags would vanish. Such a
                // position was never tested, so it is reported as Fallback and never as Solved.
                //
                // Counted in geometric comparisons rather than elapsed time - see WorkBudget. A normal solve
                // of either real consumer never approaches it.
                bool overBudget = isOverBudget();

                if (sAMGeometry2D is Point2D)
                {
                    Point2D point2D = (Point2D)sAMGeometry2D;
                    Rectangle2D rectangle2DWithGivenPointInCenter = rectangle2D.GetMoved(new Vector2D(rectangle2D.GetCentroid(), point2D));

                    if (overBudget)
                    {
                        resultRectangle2D = rectangle2DWithGivenPointInCenter;
                    }
                    else
                    {
                        for (int i = 0; i < iterationCount; i++)
                        {
                            if (resultRectangle2D != null) break;

                            foreach (Vector2D offset in offsets)
                            {
                                Vector2D scaledOffset = offset * (solver2DSettings.StartingDistance + (i * solver2DSettings.ShiftDistance));
                                Rectangle2D rectangleTemp = rectangle2DWithGivenPointInCenter.GetMoved(scaledOffset);

                                workUnits++;

                                if (area.Inside(rectangleTemp) && !intersect(rectangleTemp, result, grid))
                                {
                                    if (solver2DSettings.LimitArea != null && !solver2DSettings.LimitArea.Inside(rectangleTemp.GetCentroid()))
                                    {
                                        continue;
                                    }
                                    resultRectangle2D = rectangleTemp;
                                    break;
                                }

                                // Re-checked WITHIN this label's own sweep, not only before it started. The
                                // outer overBudget snapshot bounds every OTHER label; on its own it does nothing
                                // for the single expensive label that is spending the budget right now - a large
                                // IterationCount against a crowded obstacle set can burn millions of comparisons
                                // in one label's foreach before the outer loop gets another chance to look. Once
                                // spent mid-sweep, this label falls back to its OWN anchor - the untested
                                // rectangle2DWithGivenPointInCenter, exactly as a label that started already over
                                // budget uses, and exactly what Fallback promises a caller: at the anchor, never
                                // at whatever arbitrary spiralled-out candidate happened to be under test when
                                // the budget ran out.
                                if (!overBudget && isOverBudget())
                                {
                                    overBudget = true;
                                    resultRectangle2D = rectangle2DWithGivenPointInCenter;
                                    break;
                                }
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

                    if (overBudget)
                    {
                        resultRectangle2D = rectangle2D;
                    }

                    for (int i = 0; !overBudget && i < iterationCount; i++)
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

                            workUnits++;

                            if (area.Inside(rectangleTemp) && !intersect(rectangleTemp, result, grid))
                            {
                                if (solver2DSettings.LimitArea != null && !solver2DSettings.LimitArea.Inside(rectangleTemp.GetCentroid()))
                                {
                                    continue;
                                }
                                resultRectangle2D = rectangleTemp;
                                break;
                            }

                            // Same re-check as the Point2D branch above, and the same anchor-only Fallback
                            // contract: the untested rectangle2D at its original position, never the arbitrary
                            // segment-relative candidate under test when the budget ran out.
                            if (!overBudget && isOverBudget())
                            {
                                overBudget = true;
                                resultRectangle2D = rectangle2D;
                                break;
                            }
                        }
                    }
                }
                else
                {
                    throw new System.NotImplementedException();
                }

                // Geometry that was tested against the area, the obstacles, the rectangles already placed and
                // the limit area is Solved; the untested anchor the budget forces is Fallback; nothing at all
                // is Unplaced. The three are not interchangeable to a consumer, which is the whole point of
                // reporting them - a Fallback rectangle may sit on top of anything.
                Solver2DResultType solver2DResultType = resultRectangle2D == null
                    ? Solver2DResultType.Unplaced
                    : (overBudget ? Solver2DResultType.Fallback : Solver2DResultType.Solved);

                result.Add(new Solver2DResult(solver2DData, resultRectangle2D, solver2DResultType));

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
        /// <summary>
        /// Placement order: <see cref="Solver2DData.Priority"/> ascending, then the order the items were
        /// added. Sorting a list of indices rather than the items makes the comparison total, so the result
        /// does not depend on <see cref="List{T}.Sort"/> being stable - it is not.
        /// </summary>
        private List<Solver2DData> ordered()
        {
            List<int> indexes = new List<int>(solver2DDatas.Count);
            for (int i = 0; i < solver2DDatas.Count; i++)
            {
                indexes.Add(i);
            }

            indexes.Sort((x, y) =>
            {
                int compare = solver2DDatas[x].Priority.CompareTo(solver2DDatas[y].Priority);

                return compare != 0 ? compare : x.CompareTo(y);
            });

            List<Solver2DData> result = new List<Solver2DData>(indexes.Count);
            foreach (int index in indexes)
            {
                result.Add(solver2DDatas[index]);
            }

            return result;
        }

        /// <summary>
        /// Whether the solve has spent its <see cref="WorkBudget"/>. A non-positive budget means unlimited.
        /// </summary>
        private bool isOverBudget()
        {
            return workBudget > 0 && workUnits > workBudget;
        }

        private bool intersect(Rectangle2D rectangle2D, List<Solver2DResult> solver2DResults, RectangleGrid grid)
        {
            // A null obstacle list is a legitimate "nothing to avoid" - Solver2D's own constructor accepts
            // one, and the caller that has no obstacles has no reason to allocate an empty list to say so.
            // It used to throw here.
            if (obstacles2D != null)
            {
                foreach (IClosed2D obstacle2D in obstacles2D)
                {
                    workUnits++;

                    if (obstacle2D.InRange(rectangle2D) == true)
                    {
                        return true;
                    }
                }
            }

            if (grid != null)
            {
                // Only the placed rectangles near rectangle2D can overlap it; the grid yields that set
                // and the InRange test below is the same as the linear path, so the outcome is identical.
                foreach (Rectangle2D placed in grid.Query(rectangle2D))
                {
                    workUnits++;

                    if (placed.InRange(rectangle2D) == true || rectangle2D.InRange(placed) == true)
                    {
                        return true;
                    }
                }

                return false;
            }

            // An item the solver could not place carries NO footprint - it is not drawn - so it is skipped
            // here, exactly as the grid path skips it. This used to be two List.Find calls that dereferenced
            // Closed2D<Rectangle2D>() unguarded, so a single earlier unplaceable item made every subsequent
            // item throw a NullReferenceException. It could only happen on this path, which is the one taken
            // for 256 items or fewer: a Mollier chart, or a small floor plan. Testing both directions per
            // rectangle in one pass rather than in two consecutive Find calls is the same predicate over the
            // same set, so which candidate positions are accepted is unchanged.
            foreach (Solver2DResult solver2DResult in solver2DResults)
            {
                Rectangle2D placed = solver2DResult?.Closed2D<Rectangle2D>();
                if (placed == null)
                {
                    continue;
                }

                workUnits++;

                if (placed.InRange(rectangle2D) == true || rectangle2D.InRange(placed) == true)
                {
                    return true;
                }
            }

            return false;
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

            // Reused across Query calls to de-duplicate the rectangles a query box's cells share, without
            // allocating a HashSet on every call. Query is enumerated fully and sequentially by the solver
            // (one query finishes before the next starts), so a single shared scratch set is safe here.
            private readonly HashSet<Rectangle2D> querySeen = new HashSet<Rectangle2D>();

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

                querySeen.Clear();

                // One-cell halo: absorbs the InRange tolerance and any box that straddles a cell border.
                for (long x = minX - 1; x <= maxX + 1; x++)
                {
                    for (long y = minY - 1; y <= maxY + 1; y++)
                    {
                        if (cells.TryGetValue((x << 32) ^ (y & 0xffffffffL), out List<Rectangle2D> list))
                        {
                            foreach (Rectangle2D placed in list)
                            {
                                if (querySeen.Add(placed))
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
