// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical;
using SAM.Core;
using SAM.Geometry.Spatial;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using static SAM.Tests.PanelsBySolidTests;

namespace SAM.Tests
{
    /// <summary>
    /// <see cref="Analytical.Create.PanelsBySolids"/> at junctions: blocks shorter than the wall thickness that close the
    /// junction they sit in (a kink in a party wall, the slices of a short return wall at a facade jog, a facade block
    /// at the end of a thick crossing wall) and a pier first read across its own jamb. Each case is a minimal copy of a
    /// Softhouse block (2026-10-26); the preservation cases keep piers, stubs and openings that close nothing.
    /// </summary>
    public class PanelsBySolidsJunctionTests
    {
        // The Softhouse chain setting: lower than the default, so a 75 mm jamb or slice passes as an element of its own.
        private const double MinThickness = 0.04;

        private static List<List<Panel>> Run(IList<Shell> shells, out Core.Diagnostics diagnostics, IEnumerable<ISAMGeometry3D> apertures = null, DiagnosticDetail detail = DiagnosticDetail.Detailed)
        {
            diagnostics = new Core.Diagnostics(detail);
            return shells.PanelsBySolids(diagnostics, apertures, MinThickness);
        }

        private static Diagnostic Continuation(Core.Diagnostics diagnostics, int index)
        {
            return diagnostics.FirstOrDefault(x => x.SourceIndex == index && x.Code == BySolidsDiagnosticCode.PanelsContinuation);
        }

        private static List<Point3D> Points(Panel panel)
        {
            return ((ISegmentable3D)panel.GetFace3D().GetExternalEdge3D()).GetPoints();
        }

        private static Shell Floor() => Box(-3, -6, -0.05, 3, 6, 0);

        private static Shell Ceiling() => Box(-3, -6, 3, 3, 6, 3.15);

        /// <summary>
        /// Houses A/B party wall (Softhouse #292, #326, #293): a straight 0.1 m wall, a 0.1 x 0.093 m block below its end,
        /// and the wall continuing beyond the block turned by 3.6 degrees, on a floor and under a ceiling.
        /// </summary>
        private static List<Shell> Kink(bool withTurnedWall = true, double turnedThickness = 0.1)
        {
            List<Shell> result = new List<Shell> { Floor(), Ceiling(), Box(0, 0, 0, 0.1, 3, 3), Box(0, -0.093, 0, 0.1, 0, 3) };
            if (withTurnedWall)
            {
                // Its end against the block is cut square; its far end is 0.24 m further west.
                List<(double U, double V)> footprint = new List<(double U, double V)> { (0, -0.093), (turnedThickness, -0.093), (turnedThickness - 0.24, -3.9), (-0.24, -3.9) };
                result.Add(Prism(XY, footprint, null, 0, 3));
            }

            return result;
        }

        [Fact]
        public void PanelsBySolids_BlockShorterThanThicknessInKinkedWall_ClosesTheKinkOnTheWallsMidPlane()
        {
            // Arrange
            List<Shell> shells = Kink();
            Assert.Empty(shells[3].PanelsBySolid(new Core.Diagnostics(), minThickness: MinThickness));

            // Act
            List<List<Panel>> panels = Run(shells, out Core.Diagnostics diagnostics);

            // Assert: one Wall on the wall's mid-plane over the block's length, not a 0.092 m wall across it.
            Panel block = Assert.Single(panels[3]);
            Assert.Equal(PanelType.Wall, block.PanelType);
            Assert.All(Points(block), x => Assert.Equal(0.05, x.X, 6));
            Assert.Equal(0.093 * 3, block.GetArea(), 6);
            Assert.True(block.TryGetValue(PanelSourceParameter.PhysicalThickness, out double thickness));
            Assert.Equal(0.1, thickness, 6);
            Diagnostic diagnostic = Continuation(diagnostics, 3);
            Assert.NotNull(diagnostic);
            Assert.Contains("junction block", diagnostic.Message);
            Assert.DoesNotContain(diagnostics, x => x.SourceIndex == 3 && x.Code == BySolidsDiagnosticCode.PanelsNotPanelLike);
        }

        [Fact]
        public void PanelsBySolids_BlockShorterThanThicknessAtFreeWallEnd_StaysSkipped()
        {
            // Arrange: the same block with nothing beyond it, a stub at the end of the wall.
            List<Shell> shells = Kink(withTurnedWall: false);

            // Act
            List<List<Panel>> panels = Run(shells, out Core.Diagnostics diagnostics);

            // Assert
            Assert.Empty(panels[3]);
            Assert.Null(Continuation(diagnostics, 3));
            Assert.Contains(diagnostics, x => x.SourceIndex == 3 && x.Code == BySolidsDiagnosticCode.PanelsNotPanelLike);
        }

        [Fact]
        public void PanelsBySolids_BlockShorterThanThicknessBetweenWallsOfOtherThickness_StaysSkipped()
        {
            // Arrange: the wall beyond the block is 0.2 m thick, so the block is not a piece of either wall.
            List<Shell> shells = Kink(turnedThickness: 0.2);

            // Act
            List<List<Panel>> panels = Run(shells, out Core.Diagnostics diagnostics);

            // Assert
            Assert.Empty(panels[3]);
            Assert.Null(Continuation(diagnostics, 3));
        }

        /// <summary>
        /// House D ground-floor corner (Softhouse #385, #384, #391, #389): the facade steps 0.76 m east; the 0.45 m return
        /// wall between the two facade lines is cut into a 76 mm slice and a 0.234 m slice, each shorter than the
        /// thickness. Floor and ceiling.
        /// </summary>
        private static List<Shell> Jog(double eastThickness = 0.45)
        {
            return new List<Shell>
            {
                Floor(), Ceiling(),
                Box(-0.45, -3, 0, 0, 0, 3),                           // facade, west line
                Box(0.31, 0.45, 0, 0.31 + eastThickness, 3.45, 3),    // facade, east line
                Box(0, 0, 0, 0.076, 0.45, 3),                         // slice read as a 76 mm wall across the return wall
                Box(0.076, 0, 0, 0.31, 0.45, 3),                      // slice skipped as not panel-like
            };
        }

        [Fact]
        public void PanelsBySolids_ReturnWallSlicedShorterThanThickness_GivesTheReturnWallOnItsMidPlane()
        {
            // Arrange: on its own the 76 mm slice is read as a wall across the return wall.
            List<Shell> shells = Jog();
            Panel across = Assert.Single(shells[4].PanelsBySolid(new Core.Diagnostics(), minThickness: MinThickness));
            Assert.True(System.Math.Abs(across.Normal.Unit.X) > 0.999);

            // Act
            List<List<Panel>> panels = Run(shells, out Core.Diagnostics diagnostics);

            // Assert: both slices give the return wall (normal along y, mid-plane y = 0.225), 0.45 m thick.
            foreach (int index in new[] { 4, 5 })
            {
                Panel slice = Assert.Single(panels[index]);
                Assert.True(System.Math.Abs(slice.Normal.Unit.Y) > 0.999, "slice " + index);
                Assert.All(Points(slice), x => Assert.Equal(0.225, x.Y, 6));
                Assert.True(slice.TryGetValue(PanelSourceParameter.PhysicalThickness, out double thickness));
                Assert.Equal(0.45, thickness, 6);
                Assert.Contains("junction block", Continuation(diagnostics, index)?.Message ?? string.Empty);
            }

            Assert.Contains("replaces", Continuation(diagnostics, 4).Message);
            Assert.DoesNotContain(diagnostics, x => x.SourceIndex == 4 && x.Code == BySolidsDiagnosticCode.PanelsCreated && System.Math.Abs(x.Value.GetValueOrDefault() - 0.076) < 1e-6);
        }

        [Fact]
        public void PanelsBySolids_ReturnWallSlicesBetweenFacadesOfOtherThickness_KeepTheirFirstReading()
        {
            // Arrange: the east facade is 0.3 m thick, so the slices bridge nothing of their 0.45 m thickness.
            List<Shell> shells = Jog(eastThickness: 0.3);

            // Act
            List<List<Panel>> panels = Run(shells, out Core.Diagnostics diagnostics);

            // Assert: unchanged; the 76 mm slice keeps its own reading, the other slice stays skipped.
            Panel across = Assert.Single(panels[4]);
            Assert.True(System.Math.Abs(across.Normal.Unit.X) > 0.999);
            Assert.Null(Continuation(diagnostics, 4));
            Assert.Empty(panels[5]);
            Assert.Null(Continuation(diagnostics, 5));
        }

        [Fact]
        public void PanelsBySolids_SingleSliceAgainstWallEnd_KeepsItsOwnReading()
        {
            // Arrange: only the west facade and the 76 mm slice: nothing on its other side.
            List<Shell> shells = Jog();
            shells.RemoveRange(5, 1);
            shells.RemoveRange(3, 1);

            // Act
            List<List<Panel>> panels = Run(shells, out Core.Diagnostics diagnostics);

            // Assert
            Panel across = Assert.Single(panels[3]);
            Assert.True(System.Math.Abs(across.Normal.Unit.X) > 0.999);
            Assert.Null(Continuation(diagnostics, 3));
        }

        /// <summary>
        /// House D ground floor (Softhouse #389, #172, #388): a 0.45 m facade pier 1.588 m long with a window through it
        /// 75 mm from its end, a 0.4 m wall ending against the facade, and the facade beyond that wall. On its own the
        /// pier is not panel-like (0.45 / 1.588 m) and its 75 mm jamb is taken as a wall across the facade.
        /// </summary>
        private static List<Shell> Pier(bool withFacadeBeyond = true)
        {
            Shell pier = Prism(YZ, Rectangle(0, 0, 1.588, 3), new List<IList<(double U, double V)>> { Rectangle(0.075, 1.2, 1.225, 2.9) }, 0, 0.45);
            List<Shell> result = new List<Shell> { Floor(), Ceiling(), pier };
            if (withFacadeBeyond)
            {
                result.Add(Box(0.45, 1.588, 0, 2.95, 1.988, 3));
                result.Add(Box(0, 1.988, 0, 0.45, 5.988, 3));
            }

            return result;
        }

        // Vertical element in the YZ plane, thick along X.
        private static Point3D YZ(double u, double v, double w) => new Point3D(w, u, v);

        private static List<ISAMGeometry3D> PierWindow() => new List<ISAMGeometry3D> { Box(0.2, 0.075, 1.2, 0.25, 1.225, 2.9) };

        [Fact]
        public void PanelsBySolids_PierReadAcrossItsJambButPieceOfFacade_IsReadAsTheFacade()
        {
            // Arrange
            List<Shell> shells = Pier();
            Panel jamb = Assert.Single(shells[2].PanelsBySolid(new Core.Diagnostics(), PierWindow(), MinThickness));
            Assert.True(System.Math.Abs(jamb.Normal.Unit.Y) > 0.999);

            // Act
            List<List<Panel>> panels = Run(shells, out Core.Diagnostics diagnostics, PierWindow());

            // Assert: one facade Wall on x = 0.225 with the window hole filled for the aperture.
            Panel pier = Assert.Single(panels[2]);
            Assert.True(System.Math.Abs(pier.Normal.Unit.X) > 0.999);
            Assert.All(Points(pier), x => Assert.Equal(0.225, x.X, 6));
            Assert.Equal(1.588 * 3, pier.GetArea(), 6);
            Diagnostic diagnostic = Continuation(diagnostics, 2);
            Assert.NotNull(diagnostic);
            Assert.Contains("piece of an element", diagnostic.Message);
            Assert.Contains("replaces", diagnostic.Message);
        }

        [Fact]
        public void PanelsBySolids_PierWithJambAlone_KeepsItsOwnReading()
        {
            // Arrange: the same pier with no facade to be a piece of.
            List<Shell> shells = Pier(withFacadeBeyond: false);

            // Act
            List<List<Panel>> panels = Run(shells, out Core.Diagnostics diagnostics, PierWindow());

            // Assert
            Panel jamb = Assert.Single(panels[2]);
            Assert.True(System.Math.Abs(jamb.Normal.Unit.Y) > 0.999);
            Assert.Null(Continuation(diagnostics, 2));
        }

        /// <summary>
        /// House D first-floor facade (Softhouse #361, #353): a 0.45 m facade piece 5.463 m long that is almost all window,
        /// leaving a 76 mm jamb, a 37 mm jamb and a 48 mm head; a 0.1 m wall ending against the facade; the facade beyond.
        /// On its own the piece is read on its jamb and head, and its 0.45 m plane is dropped as their cross-cut.
        /// </summary>
        private static List<Shell> FacadeAroundLargeWindow(bool withFacadeBeyond = true)
        {
            Shell piece = Prism(XZ, Rectangle(0, 0, 5.463, 2.85), new List<IList<(double U, double V)>> { Rectangle(0.037, 0.002, 5.387, 2.802) }, 0, 0.45);
            List<Shell> result = new List<Shell> { Floor(), Ceiling(), piece };
            if (withFacadeBeyond)
            {
                result.Add(Box(5.463, 0.45, 0, 5.563, 4, 2.85));
                result.Add(Box(5.563, 0, 0, 10, 0.45, 2.85));
            }

            return result;
        }

        private static List<ISAMGeometry3D> LargeWindow() => new List<ISAMGeometry3D> { Box(0.037, 0.2, 0.002, 5.387, 0.25, 2.802) };

        [Fact]
        public void PanelsBySolids_FacadeReadOnJambAndHeadButPieceOfFacade_IsReadAsTheFacade()
        {
            // Arrange
            List<Shell> shells = FacadeAroundLargeWindow();
            List<Panel> single = shells[2].PanelsBySolid(new Core.Diagnostics(), LargeWindow(), MinThickness);
            Assert.Equal(2, single.Count);
            Assert.DoesNotContain(single, x => System.Math.Abs(x.Normal.Unit.Y) > 0.999);

            // Act
            List<List<Panel>> panels = Run(shells, out Core.Diagnostics diagnostics, LargeWindow());

            // Assert: one facade Wall on y = 0.225 with the window filled; the record says the plane is panel-like.
            Panel facade = Assert.Single(panels[2]);
            Assert.True(System.Math.Abs(facade.Normal.Unit.Y) > 0.999);
            Assert.All(Points(facade), x => Assert.Equal(0.225, x.Y, 6));
            Assert.Equal(5.463 * 2.85, facade.GetArea(), 6);
            Diagnostic diagnostic = Continuation(diagnostics, 2);
            Assert.NotNull(diagnostic);
            Assert.StartsWith("Read on another of its planes", diagnostic.Message);
            Assert.Contains("It replaces 2 Panels of the 0.048 and 0.076 m planes", diagnostic.Message);
        }

        [Fact]
        public void PanelsBySolids_FacadeReadOnJambAndHeadAlone_KeepsItsOwnReading()
        {
            // Arrange: the same piece with no facade to be a piece of.
            List<Shell> shells = FacadeAroundLargeWindow(withFacadeBeyond: false);

            // Act
            List<List<Panel>> panels = Run(shells, out Core.Diagnostics diagnostics, LargeWindow());

            // Assert
            Assert.Equal(2, panels[2].Count);
            Assert.Null(Continuation(diagnostics, 2));
        }

        /// <summary>
        /// House D first floor (Softhouse #368, #251, #367, #250, #247, #366): a 0.45 m facade, two 0.1 m walls ending
        /// against it 0.548 m apart and a 0.548 m wall between them; the facade block at the end of the thick wall lies
        /// within that wall's thickness.
        /// </summary>
        private static List<Shell> FacadeAtThickWallEnd(bool withFacadeBeyond = true)
        {
            List<Shell> result = new List<Shell>
            {
                Floor(), Ceiling(),
                Box(0, -1, 0, 0.45, 3.129, 3),          // facade, south
                Box(0.45, 3.129, 0, 3.4, 3.229, 3),     // 0.1 m wall
                Box(0, 3.229, 0, 0.45, 3.777, 3),       // facade block
                Box(0.45, 3.229, 0, 3.4, 3.777, 3),     // 0.548 m wall
                Box(0.45, 3.777, 0, 3.4, 3.877, 3),     // 0.1 m wall
            };

            if (withFacadeBeyond)
            {
                result.Add(Box(0, 3.877, 0, 0.45, 5.5, 3));
            }

            return result;
        }

        [Fact]
        public void PanelsBySolids_FacadeBlockWithinThickWallEndBetweenFacadePieces_IsKeptAsFacade()
        {
            // Arrange
            List<Shell> shells = FacadeAtThickWallEnd();

            // Act
            List<List<Panel>> panels = Run(shells, out Core.Diagnostics diagnostics);

            // Assert: a facade Wall on x = 0.225, not left out as the end of the 0.548 m wall.
            Panel block = Assert.Single(panels[4]);
            Assert.True(System.Math.Abs(block.Normal.Unit.X) > 0.999);
            Assert.All(Points(block), x => Assert.Equal(0.225, x.X, 6));
            Diagnostic diagnostic = Continuation(diagnostics, 4);
            Assert.NotNull(diagnostic);
            Assert.Contains("piece of an element", diagnostic.Message);
            Assert.Contains("not their end", diagnostic.Message);
        }

        [Fact]
        public void PanelsBySolids_FacadeBlockWithinThickWallEndWithFacadeOnOneSide_StaysSkipped()
        {
            // Arrange: no facade beyond the block: it is the end of the thick wall.
            List<Shell> shells = FacadeAtThickWallEnd(withFacadeBeyond: false);

            // Act
            List<List<Panel>> panels = Run(shells, out Core.Diagnostics diagnostics);

            // Assert
            Assert.Empty(panels[4]);
            Assert.Null(Continuation(diagnostics, 4));
        }

        [Fact]
        public void PanelsBySolids_JunctionCases_GiveTheSamePanelsAtEveryDetailLevel()
        {
            // Arrange
            List<(List<Shell> Shells, List<ISAMGeometry3D> Apertures)> cases = new List<(List<Shell>, List<ISAMGeometry3D>)>
            {
                (Kink(), null), (Jog(), null), (Pier(), PierWindow()), (FacadeAtThickWallEnd(), null),
            };

            foreach ((List<Shell> shells, List<ISAMGeometry3D> apertures) in cases)
            {
                // Act
                List<List<string>> results = new[] { DiagnosticDetail.Summary, DiagnosticDetail.Standard, DiagnosticDetail.Detailed }
                    .Select(x => Run(shells, out Core.Diagnostics _, apertures, x).ConvertAll(y => string.Join(";", y.Select(p => p.PanelType + ":" + System.Math.Round(p.GetArea(), 6) + ":" + string.Join(",", Points(p).Select(q => q.ToString())))))).ToList();

                // Assert: detail never changes an engineering result; the kept solids are reported at Summary.
                Assert.Equal(results[0], results[1]);
                Assert.Equal(results[0], results[2]);
            }

            Run(Jog(), out Core.Diagnostics summary, null, DiagnosticDetail.Summary);
            Assert.Equal(2, summary.Count(x => x.Code == BySolidsDiagnosticCode.PanelsContinuation && x.Severity == DiagnosticSeverity.Info));
        }
    }
}
