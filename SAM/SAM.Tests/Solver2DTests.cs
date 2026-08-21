// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Geometry.Planar;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace SAM.Tests
{
    /// <summary>
    /// Behaviour locks for the shared 2D placement engine <see cref="Solver2D"/>, which places the
    /// floor-plan space labels, the Mollier chart labels and the Part F airflow tags.
    /// <para>
    /// Written for the hardening pass that made it the common annotation engine, and covering one thing
    /// each: the two null paths, the placement order, the result type, and the deterministic work budget
    /// that replaced a wall-clock one. The determinism tests are the load-bearing ones - a saved drawing
    /// that redraws with its labels somewhere else is not a saved drawing.
    /// </para>
    /// </summary>
    public class Solver2DTests
    {
        private readonly ITestOutputHelper testOutputHelper;

        public Solver2DTests(ITestOutputHelper testOutputHelper)
        {
            this.testOutputHelper = testOutputHelper;
        }

        // --- The two null paths ---------------------------------------------------------------------

        /// <summary>
        /// A null obstacle list means "nothing to avoid". The constructor accepts one, so the solve has to
        /// as well; it used to throw a NullReferenceException out of obstacles2D.Find on the first
        /// candidate of the first item. This is the path a consumer with no obstacles takes.
        /// </summary>
        [Fact]
        public void Solve_NullObstacleList_PlacesInsteadOfThrowing()
        {
            Solver2D solver2D = new Solver2D(Area(), null);
            solver2D.Add(Data(new Point2D(0, 0), "only"));

            List<Solver2DResult> solver2DResults = solver2D.Solve();

            Assert.NotNull(solver2DResults);
            Assert.Single(solver2DResults);
            Assert.Equal(Solver2DResultType.Solved, solver2DResults[0].ResultType);
            Assert.NotNull(solver2DResults[0].Closed2D<Rectangle2D>());
        }

        /// <summary>
        /// An item nothing could be found for carries no footprint, so the items after it must be solved
        /// against the ones that WERE placed and must not see the gap. Exercises the small non-grid path
        /// (256 items or fewer) - a Mollier chart or a small floor plan - where the overlap test reads the
        /// results collected so far and so meets the unplaced entry.
        /// </summary>
        [Fact]
        public void Solve_ItemLeftUnplaced_LaterItemsStillSolve()
        {
            Solver2D solver2D = new Solver2D(Area(), new List<IClosed2D>());

            //Unplaceable by construction: its centre is required to land in a square 40 m away, and the
            //sweep only reaches 4.5 m.
            Solver2DData solver2DData_Unplaceable = Data(new Point2D(0, 0), "unplaceable");
            solver2DData_Unplaceable.Solver2DSettings.LimitArea = new Rectangle2D(new Point2D(40, 40), 1, 1);

            solver2D.Add(solver2DData_Unplaceable);
            solver2D.Add(Data(new Point2D(0, 0), "after"));

            List<Solver2DResult> solver2DResults = solver2D.Solve();

            Assert.Equal(2, solver2DResults.Count);

            Assert.Equal(Solver2DResultType.Unplaced, solver2DResults[0].ResultType);
            Assert.Null(solver2DResults[0].Closed2D<Rectangle2D>());

            Assert.Equal(Solver2DResultType.Solved, solver2DResults[1].ResultType);
            Assert.NotNull(solver2DResults[1].Closed2D<Rectangle2D>());
        }

        // --- Deterministic order -------------------------------------------------------------------

        /// <summary>
        /// Items of equal priority are placed in the order they were added. Twenty of them, because
        /// List&lt;T&gt;.Sort switches from a stable insertion sort to an unstable introsort above sixteen -
        /// which is exactly why sorting on priority alone reordered equal-priority labels and made a
        /// redraw move them.
        /// </summary>
        [Fact]
        public void Solve_EqualPriority_PlacesInInsertionOrder()
        {
            Solver2D solver2D = new Solver2D(Area(), new List<IClosed2D>());

            List<string> tags = new List<string>();
            for (int i = 0; i < 20; i++)
            {
                string tag = string.Format("tag {0}", i);

                tags.Add(tag);
                solver2D.Add(Data(new Point2D(0, 0), tag));
            }

            List<Solver2DResult> solver2DResults = solver2D.Solve();

            Assert.Equal(tags, solver2DResults.ConvertAll(x => x.Tag as string));

            //Placed first, so it keeps the anchor and everything else moves around it.
            Point2D point2D_Centroid = solver2DResults[0].Closed2D<Rectangle2D>().GetCentroid();
            Assert.Equal(0, point2D_Centroid.X, 6);
            Assert.Equal(0, point2D_Centroid.Y, 6);
        }

        /// <summary>Priority still wins, and only ties fall back to insertion order.</summary>
        [Fact]
        public void Solve_LowerPriorityAddedLast_IsStillPlacedFirst()
        {
            Solver2D solver2D = new Solver2D(Area(), new List<IClosed2D>());

            Solver2DData solver2DData_Second = Data(new Point2D(0, 0), "high");
            solver2DData_Second.Priority = 10;

            Solver2DData solver2DData_First = Data(new Point2D(0, 0), "low");
            solver2DData_First.Priority = 1;

            solver2D.Add(solver2DData_Second);
            solver2D.Add(solver2DData_First);

            List<Solver2DResult> solver2DResults = solver2D.Solve();

            Assert.Equal("low", solver2DResults[0].Tag as string);
            Assert.Equal("high", solver2DResults[1].Tag as string);

            //The one placed first keeps the anchor.
            Assert.Equal(0, solver2DResults[0].Closed2D<Rectangle2D>().GetCentroid().X, 6);
        }

        /// <summary>
        /// The same input solved twice returns the same geometry, both from a second instance and from a
        /// second call on the same instance - the latter used to re-sort the item list in place, so call
        /// two started from the order call one left behind.
        /// </summary>
        [Fact]
        public void Solve_IdenticalInput_ReturnsIdenticalPlacement()
        {
            List<Solver2DResult> solver2DResults_1 = Solver2D_Crowded().Solve();

            Solver2D solver2D = Solver2D_Crowded();
            List<Solver2DResult> solver2DResults_2 = solver2D.Solve();
            List<Solver2DResult> solver2DResults_3 = solver2D.Solve();

            AssertSamePlacement(solver2DResults_1, solver2DResults_2);
            AssertSamePlacement(solver2DResults_1, solver2DResults_3);
        }

        /// <summary>
        /// The work the solve does is a function of its input, which is what makes the budget - and so the
        /// layout it produces - independent of the machine. A stopwatch budget could not promise this.
        /// </summary>
        [Fact]
        public void Solve_IdenticalInput_ConsumesIdenticalWork()
        {
            Solver2D solver2D_1 = Solver2D_Crowded();
            Solver2D solver2D_2 = Solver2D_Crowded();

            solver2D_1.Solve();
            solver2D_2.Solve();

            Assert.True(solver2D_1.WorkUnits > 0);
            Assert.Equal(solver2D_1.WorkUnits, solver2D_2.WorkUnits);
        }

        // --- Result type ----------------------------------------------------------------------------

        /// <summary>
        /// Once the budget is spent the remaining items are dropped at their anchor untested, and that has
        /// to be visible: the geometry is not null, it can overlap what is already there, and a caller
        /// reading only Closed2D cannot tell it from a solved placement. Fallback is how it can.
        /// </summary>
        [Fact]
        public void Solve_BudgetExhausted_ReportsFallbackAndNotSolved()
        {
            Solver2D solver2D = new Solver2D(Area(), new List<IClosed2D>());
            solver2D.WorkBudget = 1;

            for (int i = 0; i < 6; i++)
            {
                solver2D.Add(Data(new Point2D(0, 0), string.Format("tag {0}", i)));
            }

            List<Solver2DResult> solver2DResults = solver2D.Solve();

            Assert.Equal(Solver2DResultType.Solved, solver2DResults[0].ResultType);

            List<Solver2DResult> solver2DResults_Fallback = solver2DResults.FindAll(x => x.ResultType == Solver2DResultType.Fallback);
            Assert.NotEmpty(solver2DResults_Fallback);

            Rectangle2D rectangle2D_Solved = solver2DResults[0].Closed2D<Rectangle2D>();

            foreach (Solver2DResult solver2DResult in solver2DResults_Fallback)
            {
                Rectangle2D rectangle2D = solver2DResult.Closed2D<Rectangle2D>();

                //Geometry, so a consumer that only checks for null would draw it as though it were solved.
                Assert.NotNull(rectangle2D);

                //At the anchor, and therefore on top of the item that was solved there.
                Assert.Equal(0, rectangle2D.GetCentroid().X, 6);
                Assert.Equal(0, rectangle2D.GetCentroid().Y, 6);
                Assert.True(rectangle2D.InRange(rectangle2D_Solved));
            }
        }

        /// <summary>A non-positive budget removes the cap, so nothing falls back.</summary>
        [Fact]
        public void Solve_NoBudget_NeverFallsBack()
        {
            Solver2D solver2D = Solver2D_Crowded();
            solver2D.WorkBudget = 0;

            List<Solver2DResult> solver2DResults = solver2D.Solve();

            Assert.DoesNotContain(Solver2DResultType.Fallback, solver2DResults.ConvertAll(x => x.ResultType));
        }

        /// <summary>
        /// The result type a caller reads is never the default one: a defaulted value that said "solved"
        /// would be the confusion the type was added to remove.
        /// </summary>
        [Fact]
        public void Solve_EveryResult_CarriesAnExplicitResultType()
        {
            List<Solver2DResult> solver2DResults = Solver2D_Crowded().Solve();

            Assert.DoesNotContain(Solver2DResultType.Undefined, solver2DResults.ConvertAll(x => x.ResultType));
        }

        /// <summary>
        /// The constructor that predates the result type still works, deriving it from the geometry. It is
        /// deliberately not what Solver2D uses, because a fallback rectangle would come out of it as solved.
        /// </summary>
        [Fact]
        public void Solver2DResult_WithoutResultType_DerivesItFromTheGeometry()
        {
            Solver2DData solver2DData = Data(new Point2D(0, 0), "tag");

            Assert.Equal(Solver2DResultType.Solved, new Solver2DResult(solver2DData, new Rectangle2D(1, 1)).ResultType);
            Assert.Equal(Solver2DResultType.Unplaced, new Solver2DResult(solver2DData, null).ResultType);
        }

        // --- LimitArea semantics --------------------------------------------------------------------

        /// <summary>
        /// LimitArea constrains the CENTROID only, and the rectangle may overhang it. Locked because the
        /// name reads as though it constrained the whole rectangle, and because all three consumers depend
        /// on the looser meaning: an ensuite cannot contain a whole text box, and requiring it to would
        /// leave the smallest rooms unlabelled.
        /// </summary>
        [Fact]
        public void Solve_LimitArea_ConstrainsTheCentroidAndNotTheWholeRectangle()
        {
            Solver2D solver2D = new Solver2D(Area(), new List<IClosed2D>());

            //A limit area 0.8 m across cannot contain the 4 m by 1 m label, only its centre.
            Rectangle2D rectangle2D_Limit = new Rectangle2D(new Point2D(-0.4, -0.4), 0.8, 0.8);

            Solver2DData solver2DData = new Solver2DData(new Rectangle2D(new Point2D(-2, -0.5), 4, 1), new Point2D(0, 0));
            solver2DData.Tag = "wide";
            solver2DData.Solver2DSettings = new Solver2DSettings()
            {
                StartingDistance = 0,
                ShiftDistance = 0.5,
                IterationCount = 10,
                LimitArea = rectangle2D_Limit,
            };

            solver2D.Add(solver2DData);

            List<Solver2DResult> solver2DResults = solver2D.Solve();

            Rectangle2D rectangle2D = solver2DResults[0].Closed2D<Rectangle2D>();

            Assert.Equal(Solver2DResultType.Solved, solver2DResults[0].ResultType);
            Assert.NotNull(rectangle2D);

            //Centre inside the limit area...
            Assert.True(rectangle2D_Limit.Inside(rectangle2D.GetCentroid()));

            //...and the rectangle itself sticking well outside it.
            Assert.False(rectangle2D_Limit.Inside(rectangle2D));
        }

        // --- The Mollier chart's shape of input -----------------------------------------------------

        /// <summary>
        /// The Mollier chart's own shape of input, which has no test of its own because its adapter lives
        /// above OxyPlot in SAM_UI: point labels at their default priority, curve labels anchored to a
        /// Polyline2D at priorities 2 to 4, and a circle obstacle per point. Locks the three things the
        /// hardening pass could have disturbed there - that priority still decides the order, that equal
        /// priority now follows the order the chart added them in, and that obstacles are still avoided -
        /// and it is the only cover the Polyline2D anchor branch has.
        /// </summary>
        [Fact]
        public void Solve_MollierShapedInput_KeepsPriorityOrderAvoidsObstaclesAndRepeats()
        {
            List<Solver2DResult> solver2DResults_1 = Solver2D_Mollier(out List<IClosed2D> obstacle2Ds).Solve();
            List<Solver2DResult> solver2DResults_2 = Solver2D_Mollier(out List<IClosed2D> _).Solve();

            //Point labels first - their priority is the default int.MinValue - in the order they were added,
            //then the curve labels by priority. This is the order the chart draws them in.
            List<object> tags_Expected = new List<object>();
            for (int i = 0; i < 20; i++)
            {
                tags_Expected.Add(string.Format("point {0}", i));
            }

            tags_Expected.Add("curve 2");
            tags_Expected.Add("curve 3");
            tags_Expected.Add("curve 4");

            Assert.Equal(tags_Expected, solver2DResults_1.ConvertAll(x => x.Tag));

            AssertSamePlacement(solver2DResults_1, solver2DResults_2);

            //A label the solver accepted never sits on an obstacle.
            foreach (Solver2DResult solver2DResult in solver2DResults_1)
            {
                Rectangle2D rectangle2D = solver2DResult.Closed2D<Rectangle2D>();
                if (solver2DResult.ResultType != Solver2DResultType.Solved || rectangle2D == null)
                {
                    continue;
                }

                Assert.DoesNotContain(true, obstacle2Ds.ConvertAll(x => x.InRange(rectangle2D)));
            }

            //And the curve labels, which take the Polyline2D branch, were actually placed.
            Assert.All(solver2DResults_1.GetRange(20, 3), x => Assert.Equal(Solver2DResultType.Solved, x.ResultType));
        }

        // --- Budget calibration ---------------------------------------------------------------------

        /// <summary>
        /// A healthy plan-sized solve stays an order of magnitude inside <see cref="Solver2D.DefaultWorkBudget"/>,
        /// so the cap never changes a real drawing's layout. This is the measurement the default is
        /// calibrated against: 5 000 labels arranged as a floor plan's spaces are, each with a limit area
        /// and the floor plan's own IterationCount of 100. It cost 9 900 units when the budget was set.
        /// </summary>
        [Fact]
        public void Solve_HealthyPlanSizedInput_StaysWellInsideTheDefaultBudget()
        {
            Solver2D solver2D = new Solver2D(Area(2000), new List<IClosed2D>());

            for (int i = 0; i < 5000; i++)
            {
                //Laid out on a 5 m grid, as rooms are - each label has room to place at or near its anchor.
                Point2D point2D = new Point2D((i % 50) * 5, (i / 50) * 5);

                Solver2DData solver2DData = new Solver2DData(new Rectangle2D(new Point2D(point2D.X - 1, point2D.Y - 0.15), 2, 0.3), point2D);
                solver2DData.Tag = i;
                solver2DData.Solver2DSettings = new Solver2DSettings()
                {
                    StartingDistance = 0,
                    ShiftDistance = 0.04,
                    IterationCount = 100,
                    LimitArea = new Rectangle2D(new Point2D(point2D.X - 2, point2D.Y - 2), 4, 4),
                };

                solver2D.Add(solver2DData);
            }

            List<Solver2DResult> solver2DResults = solver2D.Solve();

            testOutputHelper.WriteLine(string.Format("5000 labels: {0} work units, budget {1}", solver2D.WorkUnits, Solver2D.DefaultWorkBudget));

            Assert.All(solver2DResults, x => Assert.Equal(Solver2DResultType.Solved, x.ResultType));
            Assert.True(solver2D.WorkUnits < Solver2D.DefaultWorkBudget / 10, string.Format("{0} work units is not an order of magnitude inside the {1} default budget", solver2D.WorkUnits, Solver2D.DefaultWorkBudget));
        }

        /// <summary>
        /// The degenerate layout the budget exists for: every anchor collapsed onto one point, so each
        /// label places only after spiralling past the growing pile of the ones before it. The work grows
        /// as the square of the count, which is what has to be stopped - and the items it stops are
        /// reported as Fallback rather than passed off as placed.
        /// </summary>
        [Fact]
        public void Solve_DegenerateLayout_IsStoppedByTheBudgetAndSaysSo()
        {
            Solver2D solver2D = new Solver2D(Area(), new List<IClosed2D>());

            //A small explicit budget so the test measures the mechanism rather than spending the default.
            solver2D.WorkBudget = 20000;

            for (int i = 0; i < 400; i++)
            {
                solver2D.Add(Data(new Point2D(0, 0), i));
            }

            List<Solver2DResult> solver2DResults = solver2D.Solve();

            testOutputHelper.WriteLine(string.Format("400 collapsed labels: {0} work units", solver2D.WorkUnits));

            Assert.Contains(Solver2DResultType.Fallback, solver2DResults.ConvertAll(x => x.ResultType));

            //Bounded: once over budget every remaining item costs nothing, so the overrun is one item's worth.
            Assert.True(solver2D.WorkUnits < 200000, string.Format("{0} work units overran the 20000 budget by more than one item's search", solver2D.WorkUnits));
        }

        /// <summary>
        /// The grid path, above 256 items, orders and places identically twice over as well. It shares the
        /// ordering fix with the linear path but not the overlap test, so it is worth its own lock.
        /// </summary>
        [Fact]
        public void Solve_GridPath_IsDeterministic()
        {
            List<Solver2DResult> solver2DResults_1 = Solver2D_Grid().Solve();
            List<Solver2DResult> solver2DResults_2 = Solver2D_Grid().Solve();

            Assert.Equal(300, solver2DResults_1.Count);
            AssertSamePlacement(solver2DResults_1, solver2DResults_2);
        }

        // --- Helpers ---------------------------------------------------------------------------------

        private static Rectangle2D Area(double size = 100)
        {
            return new Rectangle2D(new Point2D(-size, -size), size * 3, size * 3);
        }

        private static Solver2DData Data(Point2D point2D, object tag)
        {
            Solver2DData result = new Solver2DData(new Rectangle2D(new Point2D(point2D.X - 1, point2D.Y - 0.5), 2, 1), point2D);

            result.Tag = tag;
            result.Solver2DSettings = new Solver2DSettings()
            {
                StartingDistance = 0,
                ShiftDistance = 0.5,
                IterationCount = 10,
            };

            return result;
        }

        /// <summary>Ten items on one anchor, so every one after the first has to be displaced.</summary>
        private static Solver2D Solver2D_Crowded()
        {
            Solver2D result = new Solver2D(Area(), new List<IClosed2D>());

            for (int i = 0; i < 10; i++)
            {
                result.Add(Data(new Point2D(0, 0), string.Format("tag {0}", i)));
            }

            return result;
        }

        /// <summary>
        /// A Mollier chart's shape of input: twenty point labels at the default priority with a circle
        /// obstacle each, and three curve labels anchored to a polyline at priorities 2, 3 and 4.
        /// </summary>
        private static Solver2D Solver2D_Mollier(out List<IClosed2D> obstacle2Ds)
        {
            obstacle2Ds = new List<IClosed2D>();

            List<Solver2DData> solver2DDatas = new List<Solver2DData>();

            for (int i = 0; i < 20; i++)
            {
                Point2D point2D = new Point2D(i * 2.5, (i % 3) * 1.5);

                obstacle2Ds.Add(new Circle2D(point2D, 0.14));

                Solver2DData solver2DData = new Solver2DData(new Rectangle2D(new Point2D(point2D.X - 0.6, point2D.Y + 0.4), 1.2, 0.3), point2D);
                solver2DData.Tag = string.Format("point {0}", i);
                solver2DData.Solver2DSettings = new Solver2DSettings()
                {
                    StartingDistance = 0.2,
                    ShiftDistance = 0.1,
                    IterationCount = 10,
                };

                solver2DDatas.Add(solver2DData);
            }

            Polyline2D polyline2D = new Polyline2D(new List<Segment2D>
            {
                new Segment2D(new Point2D(0, -3), new Point2D(25, -1)),
                new Segment2D(new Point2D(25, -1), new Point2D(50, -3)),
            });

            for (int priority = 2; priority <= 4; priority++)
            {
                Solver2DData solver2DData = new Solver2DData(new Rectangle2D(new Point2D(24, -0.6), 2, 0.3), polyline2D);
                solver2DData.Tag = string.Format("curve {0}", priority);
                solver2DData.Priority = priority;
                solver2DData.Solver2DSettings = new Solver2DSettings()
                {
                    StartingDistance = 0,
                    ShiftDistance = 0.5,
                    IterationCount = 100,
                };

                solver2DDatas.Add(solver2DData);
            }

            Solver2D result = new Solver2D(new Rectangle2D(new BoundingBox2D(new Point2D(-10, -10), new Point2D(60, 20))), obstacle2Ds);
            result.AddRange(solver2DDatas);

            return result;
        }

        /// <summary>Three hundred items, which is over the threshold where the spatial index is built.</summary>
        private static Solver2D Solver2D_Grid()
        {
            Solver2D result = new Solver2D(Area(500), new List<IClosed2D>());

            for (int i = 0; i < 300; i++)
            {
                //Deliberately crowded in pairs, so placement order matters and a reordering would show.
                result.Add(Data(new Point2D((i / 2) * 2.5, 0), i));
            }

            return result;
        }

        private static void AssertSamePlacement(List<Solver2DResult> solver2DResults_1, List<Solver2DResult> solver2DResults_2)
        {
            Assert.Equal(solver2DResults_1.Count, solver2DResults_2.Count);

            for (int i = 0; i < solver2DResults_1.Count; i++)
            {
                Assert.Equal(solver2DResults_1[i].Tag, solver2DResults_2[i].Tag);
                Assert.Equal(solver2DResults_1[i].ResultType, solver2DResults_2[i].ResultType);

                Rectangle2D rectangle2D_1 = solver2DResults_1[i].Closed2D<Rectangle2D>();
                Rectangle2D rectangle2D_2 = solver2DResults_2[i].Closed2D<Rectangle2D>();

                if (rectangle2D_1 == null || rectangle2D_2 == null)
                {
                    Assert.Null(rectangle2D_1);
                    Assert.Null(rectangle2D_2);
                    continue;
                }

                Assert.Equal(rectangle2D_1.Origin.X, rectangle2D_2.Origin.X, 9);
                Assert.Equal(rectangle2D_1.Origin.Y, rectangle2D_2.Origin.Y, 9);
                Assert.Equal(rectangle2D_1.Width, rectangle2D_2.Width, 9);
                Assert.Equal(rectangle2D_1.Height, rectangle2D_2.Height, 9);
            }
        }
    }
}
