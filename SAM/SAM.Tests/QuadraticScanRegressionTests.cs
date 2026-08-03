// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical;
using SAM.Geometry.Spatial;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace SAM.Tests
{
    /// <summary>
    /// Behaviour locks for the workflows touched by the quadratic-scan audit. These were
    /// written against the pre-optimisation implementations and must keep passing unchanged
    /// afterwards - the optimisations are broad-phase filtering and indexing only, they are
    /// not allowed to move a single result.
    /// </summary>
    public class QuadraticScanRegressionTests
    {
        // --- Modify.SplitFace3Ds(List<Shell>) -------------------------------------------------

        [Fact]
        public void SplitFace3Ds_DisjointShells_LeavesFaceCountsUnchanged()
        {
            List<Shell> shells = QuadraticScanFixtures.DisjointBoxShells(9);
            List<int> before = shells.ConvertAll(x => x.Face3Ds.Count);

            shells.SplitFace3Ds(Core.Tolerance.MacroDistance, Core.Tolerance.Angle, Core.Tolerance.Distance);

            Assert.Equal(9, shells.Count);
            Assert.Equal(before, shells.ConvertAll(x => x.Face3Ds.Count));
        }

        [Fact]
        public void SplitFace3Ds_TouchingShells_SplitsSharedWallOfLargerShell()
        {
            // A 4x4 box against a 4x2 box: the tall neighbour's shared wall covers only half of
            // the big box's wall, so the big box's wall gets split and the small one does not.
            Shell shell_Large = QuadraticScanFixtures.BoxShell(0, 0, 0, 4, 4, 3);
            Shell shell_Small = QuadraticScanFixtures.BoxShell(4, 0, 0, 4, 2, 3);

            List<Shell> shells = new List<Shell> { shell_Large, shell_Small };
            shells.SplitFace3Ds(Core.Tolerance.MacroDistance, Core.Tolerance.Angle, Core.Tolerance.Distance);

            Assert.Equal(2, shells.Count);
            Assert.True(shells[0].Face3Ds.Count >= 6);
            Assert.True(shells[1].Face3Ds.Count >= 6);

            // Total surface area is conserved by a split.
            Assert.Equal(TotalArea(shell_Large) + TotalArea(shell_Small), TotalArea(shells[0]) + TotalArea(shells[1]), 6);
        }

        [Fact]
        public void SplitFace3Ds_PreservesInputOrder()
        {
            List<Shell> shells = new List<Shell>
            {
                QuadraticScanFixtures.BoxShell(0, 0, 0, 4, 4, 3),
                QuadraticScanFixtures.BoxShell(100, 0, 0, 4, 4, 3),
                QuadraticScanFixtures.BoxShell(4, 0, 0, 4, 2, 3),
            };

            List<BoundingBox3D> before = shells.ConvertAll(x => x.GetBoundingBox());

            shells.SplitFace3Ds(Core.Tolerance.MacroDistance, Core.Tolerance.Angle, Core.Tolerance.Distance);

            for (int i = 0; i < before.Count; i++)
            {
                Assert.True(before[i].Min.AlmostEquals(shells[i].GetBoundingBox().Min, 1e-9));
                Assert.True(before[i].Max.AlmostEquals(shells[i].GetBoundingBox().Max, 1e-9));
            }
        }

        // --- Create.AdjacencyCluster(shells, spaces) ------------------------------------------

        [Fact]
        public void AdjacencyCluster_ByShells_DisjointBoxes_OneSpaceAndSixPanelsEach()
        {
            List<Shell> shells = QuadraticScanFixtures.DisjointBoxShells(4);

            AdjacencyCluster adjacencyCluster = Analytical.Create.AdjacencyCluster(shells, null);

            Assert.NotNull(adjacencyCluster);
            Assert.Equal(4, adjacencyCluster.GetSpaces().Count);
            Assert.Equal(24, adjacencyCluster.GetPanels().Count);
        }

        [Fact]
        public void AdjacencyCluster_ByShells_AdjacentBoxes_ShareTheCommonPanel()
        {
            // Two boxes sharing the x=4 wall: the shared face must resolve to a single panel
            // related to both spaces, which is exactly what the pairwise face scan is for.
            List<Shell> shells = new List<Shell>
            {
                QuadraticScanFixtures.BoxShell(0, 0, 0, 4, 4, 3),
                QuadraticScanFixtures.BoxShell(4, 0, 0, 4, 4, 3),
            };

            AdjacencyCluster adjacencyCluster = Analytical.Create.AdjacencyCluster(shells, null);

            Assert.NotNull(adjacencyCluster);

            List<Space> spaces = adjacencyCluster.GetSpaces();
            Assert.Equal(2, spaces.Count);

            List<Panel> panels = adjacencyCluster.GetPanels();
            Assert.Equal(11, panels.Count);

            List<Panel> shared = panels.FindAll(x => adjacencyCluster.GetRelatedObjects<Space>(x).Count == 2);
            Assert.Single(shared);
        }

        [Fact]
        public void AdjacencyCluster_ByShells_NamesGeneratedSpacesFromSuppliedNamesOnly()
        {
            // Documented current behaviour: the running index is probed against the *supplied*
            // space list only - generated spaces are never added back to it - so with no
            // supplied spaces every generated space is called "Space 1". The optimisation must
            // not change this.
            List<Shell> shells = QuadraticScanFixtures.DisjointBoxShells(3);

            AdjacencyCluster adjacencyCluster = Analytical.Create.AdjacencyCluster(shells, null);

            List<string> names = adjacencyCluster.GetSpaces().ConvertAll(x => x.Name);
            names.Sort(StringComparer.Ordinal);

            Assert.Equal(new List<string> { "Space 1", "Space 1", "Space 1" }, names);
        }

        [Fact]
        public void AdjacencyCluster_ByShells_ReusesSuppliedSpaceAndSkipsItsName()
        {
            // "Space 1" is taken by the supplied space, so the generated one must be "Space 2".
            List<Shell> shells = new List<Shell>
            {
                QuadraticScanFixtures.BoxShell(0, 0, 0, 4, 4, 3),
                QuadraticScanFixtures.BoxShell(100, 0, 0, 4, 4, 3),
            };

            List<Space> spaces = new List<Space> { new Space("Space 1", new Point3D(102, 2, 1.5)) };

            AdjacencyCluster adjacencyCluster = Analytical.Create.AdjacencyCluster(shells, spaces);

            List<string> names = adjacencyCluster.GetSpaces().ConvertAll(x => x.Name);
            names.Sort(StringComparer.Ordinal);

            Assert.Equal(new List<string> { "Space 1", "Space 2" }, names);
        }

        // --- Query.MergeOverlapPanels ---------------------------------------------------------

        [Fact]
        public void MergeOverlapPanels_DisjointFloors_KeepsEveryPanel()
        {
            List<Panel> panels = QuadraticScanFixtures.CoplanarFloorPanels(9);
            List<Panel> redundantPanels = new List<Panel>();

            List<Panel> result = Analytical.Query.MergeOverlapPanels(panels, 0.1, ref redundantPanels, false, Core.Tolerance.Distance);

            Assert.NotNull(result);
            Assert.Equal(9, result.Count);
            Assert.Empty(redundantPanels);
            Assert.Equal(9 * 16, result.Sum(x => x.GetArea()), 6);
        }

        [Fact]
        public void MergeOverlapPanels_StackedFloors_CollapsesToOne()
        {
            // Three identical floor slabs within the offset band collapse to a single panel and
            // the other two are reported redundant.
            List<Panel> panels = new List<Panel>
            {
                FloorPanel(0, 0, 0.00, 4),
                FloorPanel(0, 0, 0.02, 4),
                FloorPanel(0, 0, 0.04, 4),
            };

            List<Panel> redundantPanels = new List<Panel>();
            List<Panel> result = Analytical.Query.MergeOverlapPanels(panels, 0.1, ref redundantPanels, false, Core.Tolerance.Distance);

            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal(16, result[0].GetArea(), 4);

            // Documented current behaviour: a superseded panel is only reported as redundant
            // when it carries apertures, so an aperture-free stack reports none.
            Assert.Empty(redundantPanels);
        }

        [Fact]
        public void MergeOverlapPanels_PartiallyOverlappingFloors_SplitsIntoDisjointPieces()
        {
            List<Panel> panels = new List<Panel>
            {
                FloorPanel(0, 0, 0, 4),
                FloorPanel(2, 0, 0.01, 4),
            };

            List<Panel> redundantPanels = new List<Panel>();
            List<Panel> result = Analytical.Query.MergeOverlapPanels(panels, 0.1, ref redundantPanels, false, Core.Tolerance.Distance);

            Assert.NotNull(result);
            Assert.Equal(3, result.Count);

            // The union of the two 4x4 slabs offset by 2 in x is 6x4 = 24.
            Assert.Equal(24, result.Sum(x => x.GetArea()), 4);
        }

        [Fact]
        public void MergeOverlapPanels_DisjointWalls_KeepsEveryPanel()
        {
            List<Panel> panels = QuadraticScanFixtures.DisjointWallPanels(9);
            List<Panel> redundantPanels = new List<Panel>();

            List<Panel> result = Analytical.Query.MergeOverlapPanels(panels, 0.1, ref redundantPanels, false, Core.Tolerance.Distance);

            Assert.NotNull(result);
            Assert.Equal(9, result.Count);
            Assert.Equal(9 * 9, result.Sum(x => x.GetArea()), 6);
        }

        [Fact]
        public void MergeOverlapPanels_StackedWalls_CollapsesToOne()
        {
            List<Panel> panels = new List<Panel>
            {
                WallPanel(0.00, 3),
                WallPanel(0.02, 3),
                WallPanel(0.04, 3),
            };

            List<Panel> redundantPanels = new List<Panel>();
            List<Panel> result = Analytical.Query.MergeOverlapPanels(panels, 0.1, ref redundantPanels, false, Core.Tolerance.Distance);

            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal(9, result[0].GetArea(), 4);
        }

        [Fact]
        public void MergeOverlapPanels_KeepsApertureFromRedundantPanel()
        {
            // Both slabs on the same plane, so the aperture of the superseded one stays valid
            // on the merged face and must survive the merge.
            Panel panel_Keep = FloorPanel(0, 0, 0, 4);
            Panel panel_Drop = FloorPanel(0, 0, 0, 4);

            ApertureConstruction apertureConstruction = Analytical.Query.DefaultApertureConstruction(PanelType.Floor, ApertureType.Window);
            Aperture aperture = new Aperture(apertureConstruction, new Polygon3D(new Point3D[]
            {
                new Point3D(1, 1, 0),
                new Point3D(2, 1, 0),
                new Point3D(2, 2, 0),
                new Point3D(1, 2, 0),
            }));

            Assert.True(panel_Drop.AddAperture(aperture));

            List<Panel> redundantPanels = new List<Panel>();
            List<Panel> result = Analytical.Query.MergeOverlapPanels(new List<Panel> { panel_Keep, panel_Drop }, 0.1, ref redundantPanels, false, Core.Tolerance.Distance);

            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal(1, result[0].Apertures?.Count ?? -1);
            Assert.Single(redundantPanels);
        }

        // --- Query.MergeCoplanarPanels --------------------------------------------------------

        [Fact]
        public void MergeCoplanarPanels_TwoAbuttingFloors_MergeIntoOne()
        {
            List<Panel> panels = new List<Panel>
            {
                FloorPanel(0, 0, 0, 4),
                FloorPanel(4, 0, 0, 4),
            };

            List<Panel> redundantPanels = new List<Panel>();
            List<Panel> result = Analytical.Query.MergeCoplanarPanels(panels, 0.1, ref redundantPanels, false, Core.Tolerance.MacroDistance, Core.Tolerance.Distance);

            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal(32, result[0].GetArea(), 4);
            Assert.Single(redundantPanels);
        }

        [Fact]
        public void MergeCoplanarPanels_SeparatedFloors_StayApart()
        {
            List<Panel> panels = new List<Panel>
            {
                FloorPanel(0, 0, 0, 4),
                FloorPanel(100, 0, 0, 4),
            };

            List<Panel> redundantPanels = new List<Panel>();
            List<Panel> result = Analytical.Query.MergeCoplanarPanels(panels, 0.1, ref redundantPanels, false, Core.Tolerance.MacroDistance, Core.Tolerance.Distance);

            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Equal(32, result.Sum(x => x.GetArea()), 4);
            Assert.Empty(redundantPanels);
        }

        [Fact]
        public void MergeCoplanarPanels_NonCoplanarFloors_StayApart()
        {
            List<Panel> panels = new List<Panel>
            {
                FloorPanel(0, 0, 0, 4),
                FloorPanel(4, 0, 5, 4),
            };

            List<Panel> redundantPanels = new List<Panel>();
            List<Panel> result = Analytical.Query.MergeCoplanarPanels(panels, 0.1, ref redundantPanels, false, Core.Tolerance.MacroDistance, Core.Tolerance.Distance);

            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Equal(32, result.Sum(x => x.GetArea()), 4);
        }

        [Fact]
        public void MergeCoplanarPanels_GridOfFloors_MergesToSingleSlab()
        {
            List<Panel> panels = QuadraticScanFixtures.CoplanarFloorPanels(9, 5, 5);

            List<Panel> redundantPanels = new List<Panel>();
            List<Panel> result = Analytical.Query.MergeCoplanarPanels(panels, 0.1, ref redundantPanels, false, Core.Tolerance.MacroDistance, Core.Tolerance.Distance);

            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal(225, result[0].GetArea(), 4);
        }

        private static double TotalArea(Shell shell)
        {
            return shell.Face3Ds.Sum(x => x.GetArea());
        }

        private static Panel FloorPanel(double x, double y, double z, double size)
        {
            Face3D face3D = QuadraticScanFixtures.Quad(
                new Point3D(x, y, z),
                new Point3D(x + size, y, z),
                new Point3D(x + size, y + size, z),
                new Point3D(x, y + size, z));

            return Analytical.Create.Panel(Analytical.Query.DefaultConstruction(PanelType.Floor), PanelType.Floor, face3D);
        }

        private static Panel WallPanel(double y, double size)
        {
            Face3D face3D = QuadraticScanFixtures.Quad(
                new Point3D(0, y, 0),
                new Point3D(size, y, 0),
                new Point3D(size, y, size),
                new Point3D(0, y, size));

            return Analytical.Create.Panel(Analytical.Query.DefaultConstruction(PanelType.Wall), PanelType.Wall, face3D);
        }
    }
}
