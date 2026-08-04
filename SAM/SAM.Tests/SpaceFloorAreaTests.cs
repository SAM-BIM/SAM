// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical;
using SAM.Geometry.Spatial;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace SAM.Tests
{
    /// <summary>
    /// Regression tests for the canonical space floor area:
    /// <see cref="Query.FloorArea(AdjacencyCluster, Space, out FloorAreaCalculationMethod, double, double, double, double)"/>
    /// and its creation-time application via
    /// <see cref="Modify.UpdateFloorAreas(AdjacencyCluster, double, double, double, double)"/>.
    /// </summary>
    public class SpaceFloorAreaTests
    {
        private const double Length = 4;
        private const double Width = 3;
        private const double Height = 2;
        private const double ExpectedBoxArea = Length * Width; // 12 m2

        // ---------------------------------------------------------------------------------------------
        // Horizontal box, every principal SAM creation path
        // ---------------------------------------------------------------------------------------------

        [Fact]
        public void CreateAdjacencyCluster_ShellsAndSpaces_HorizontalBox_SetsFloorArea()
        {
            List<Shell> shells = new List<Shell> { BoxShell(0, 0, 0, Length, Width, Height) };
            List<Space> spaces = new List<Space> { new Space("Test Space", new Point3D(2, 1.5, 1)) };

            AdjacencyCluster adjacencyCluster = Analytical.Create.AdjacencyCluster(shells, spaces);

            Assert.Equal(ExpectedBoxArea, StoredArea(adjacencyCluster), 3);
        }

        [Fact]
        public void CreateAdjacencyCluster_ShellsOnly_HorizontalBox_SetsFloorArea()
        {
            List<Shell> shells = new List<Shell> { BoxShell(0, 0, 0, Length, Width, Height) };

            AdjacencyCluster adjacencyCluster = Analytical.Create.AdjacencyCluster(shells);

            Assert.Equal(ExpectedBoxArea, StoredArea(adjacencyCluster), 3);
        }

        [Fact]
        public void CreateAdjacencyCluster_ShellsSpacesAndPanels_HorizontalBox_SetsFloorArea()
        {
            Shell shell = BoxShell(0, 0, 0, Length, Width, Height);
            List<Shell> shells = new List<Shell> { shell };
            List<Panel> panels = Analytical.Create.Panels(shell);
            List<Space> spaces = new List<Space> { new Space("Test Space", new Point3D(2, 1.5, 1)) };

            AdjacencyCluster adjacencyCluster = Analytical.Create.AdjacencyCluster(shells, spaces, panels, true, true);

            Assert.Equal(ExpectedBoxArea, StoredArea(adjacencyCluster), 3);
        }

        [Fact]
        public void CreateAdjacencyCluster_SpacesAndPanels_HorizontalBox_SetsFloorArea()
        {
            Shell shell = BoxShell(0, 0, 0, Length, Width, Height);
            List<Panel> panels = Analytical.Create.Panels(shell);
            List<Space> spaces = new List<Space> { new Space("Test Space", new Point3D(2, 1.5, 1)) };

            AdjacencyCluster adjacencyCluster = Analytical.Create.AdjacencyCluster(spaces, panels, 0.1, true, true);

            Assert.Equal(ExpectedBoxArea, StoredArea(adjacencyCluster), 3);
        }

        /// <summary>
        /// The canonical behaviour must not depend on the caller's compile-time collection type. The removed
        /// implementation intercepted <c>List&lt;Shell&gt;</c> with its own overload, so an
        /// <c>IEnumerable&lt;Shell&gt;</c> caller silently got a different area definition.
        /// </summary>
        [Fact]
        public void CreateAdjacencyCluster_ListVersusIEnumerable_ProducesSameFloorArea()
        {
            List<Shell> shells_List = new List<Shell> { BoxShell(0, 0, 0, Length, Width, Height) };
            IEnumerable<Shell> shells_Enumerable = new List<Shell> { BoxShell(0, 0, 0, Length, Width, Height) }.Select(x => x);

            List<Space> spaces_List = new List<Space> { new Space("Test Space", new Point3D(2, 1.5, 1)) };
            IEnumerable<Space> spaces_Enumerable = new List<Space> { new Space("Test Space", new Point3D(2, 1.5, 1)) }.Select(x => x);

            double area_List = StoredArea(Analytical.Create.AdjacencyCluster(shells_List, spaces_List));
            double area_Enumerable = StoredArea(Analytical.Create.AdjacencyCluster(shells_Enumerable, spaces_Enumerable));

            Assert.Equal(ExpectedBoxArea, area_List, 3);
            Assert.Equal(area_List, area_Enumerable, 6);
        }

        // ---------------------------------------------------------------------------------------------
        // Tilted / ramped floor
        // ---------------------------------------------------------------------------------------------

        /// <summary>
        /// A ramp must contribute its actual sloped walking surface, which is strictly larger than both its
        /// horizontal projection and a horizontal shell section.
        /// </summary>
        [Fact]
        public void FloorArea_RampedFloor_UsesSlopedSurfaceNotProjection()
        {
            double rise = 1;
            Shell shell = RampShell(Length, Width, rise, 3);
            AdjacencyCluster adjacencyCluster = Analytical.Create.AdjacencyCluster(new List<Shell> { shell }, new List<Space> { new Space("Ramp", new Point3D(2, 1.5, 2)) });

            Space space = Assert.Single(adjacencyCluster.GetSpaces());
            double area = adjacencyCluster.FloorArea(space, out FloorAreaCalculationMethod method);

            double projectedArea = Length * Width;
            double slopedArea = Width * System.Math.Sqrt((Length * Length) + (rise * rise));

            Assert.Equal(FloorAreaCalculationMethod.GeometricalFloorPanels, method);
            Assert.Equal(slopedArea, area, 3);
            Assert.True(area > projectedArea, string.Format("Sloped floor area {0} must exceed the projected area {1}.", area, projectedArea));
            Assert.Equal(area, StoredArea(adjacencyCluster), 3);

            // slopeRatio is a diagnostic only: it must never replace the canonical surface area.
            double slopeRatio = area / projectedArea;
            Assert.True(slopeRatio > 1);
        }

        // ---------------------------------------------------------------------------------------------
        // Only the floor contributes
        // ---------------------------------------------------------------------------------------------

        [Fact]
        public void FloorArea_FlatFloorAndSlopedRoof_CountsFloorOnly()
        {
            // Flat floor at z=0, roof rising from z=2 to z=3: the roof's surface area is larger than the
            // floor's, so counting it would be unmissable.
            Face3D floor = Quad(new Point3D(0, 0, 0), new Point3D(Length, 0, 0), new Point3D(Length, Width, 0), new Point3D(0, Width, 0));
            Face3D roof = Quad(new Point3D(0, 0, 2), new Point3D(Length, 0, 3), new Point3D(Length, Width, 3), new Point3D(0, Width, 2));
            Face3D wall_X0 = Quad(new Point3D(0, 0, 0), new Point3D(0, Width, 0), new Point3D(0, Width, 2), new Point3D(0, 0, 2));
            Face3D wall_X1 = Quad(new Point3D(Length, 0, 0), new Point3D(Length, 0, 3), new Point3D(Length, Width, 3), new Point3D(Length, Width, 0));
            Face3D wall_Y0 = Quad(new Point3D(0, 0, 0), new Point3D(0, 0, 2), new Point3D(Length, 0, 3), new Point3D(Length, 0, 0));
            Face3D wall_Y1 = Quad(new Point3D(0, Width, 0), new Point3D(Length, Width, 0), new Point3D(Length, Width, 3), new Point3D(0, Width, 2));

            Shell shell = new Shell(new List<Face3D> { floor, roof, wall_X0, wall_X1, wall_Y0, wall_Y1 });
            AdjacencyCluster adjacencyCluster = Analytical.Create.AdjacencyCluster(new List<Shell> { shell }, new List<Space> { new Space("Loft", new Point3D(2, 1.5, 1)) });

            Space space = Assert.Single(adjacencyCluster.GetSpaces());
            double area = adjacencyCluster.FloorArea(space, out FloorAreaCalculationMethod method);

            Assert.Equal(FloorAreaCalculationMethod.GeometricalFloorPanels, method);
            Assert.Equal(ExpectedBoxArea, area, 3);
        }

        [Fact]
        public void FloorArea_MultipleFloorPanels_SumsEachExactlyOnce()
        {
            // The box floor is supplied as two abutting halves rather than one face.
            List<Face3D> face3Ds = BoxFaces(0, 0, 0, Length, Width, Height);
            face3Ds.RemoveAt(0); // the single bottom face
            face3Ds.Add(Quad(new Point3D(0, 0, 0), new Point3D(0, Width, 0), new Point3D(2, Width, 0), new Point3D(2, 0, 0)));
            face3Ds.Add(Quad(new Point3D(2, 0, 0), new Point3D(2, Width, 0), new Point3D(Length, Width, 0), new Point3D(Length, 0, 0)));

            AdjacencyCluster adjacencyCluster = ClusterFromFaces(face3Ds, out Space space, Analytical.PanelType.Floor, Analytical.PanelType.Floor);

            double area = adjacencyCluster.FloorArea(space, out FloorAreaCalculationMethod method, out List<Panel> panels);

            Assert.Equal(FloorAreaCalculationMethod.GeometricalFloorPanels, method);
            Assert.Equal(2, panels.Count);
            Assert.Equal(ExpectedBoxArea, area, 3);
        }

        // ---------------------------------------------------------------------------------------------
        // Panel-type protection
        // ---------------------------------------------------------------------------------------------

        /// <summary>
        /// Geometry is the primary classification, so a floor whose metadata is missing must still count -
        /// imported models routinely arrive with <see cref="PanelType.Undefined"/>.
        /// </summary>
        [Fact]
        public void FloorArea_UndefinedPanelType_StillCounted()
        {
            AdjacencyCluster adjacencyCluster = ClusterFromFaces(BoxFaces(0, 0, 0, Length, Width, Height), out Space space, Analytical.PanelType.Undefined);

            double area = adjacencyCluster.FloorArea(space, out FloorAreaCalculationMethod method, out List<Panel> panels);

            Assert.Equal(FloorAreaCalculationMethod.GeometricalFloorPanels, method);
            Assert.Equal(Analytical.PanelType.Undefined, Assert.Single(panels).PanelType);
            Assert.Equal(ExpectedBoxArea, area, 3);
        }

        /// <summary>
        /// An explicitly incompatible type overrides the geometric test: the downward-facing panel is typed
        /// Wall, so the panel calculation must reject it and the horizontal section fallback must answer.
        /// </summary>
        [Fact]
        public void FloorArea_DownwardPanelTypedWall_RejectedAndFallsBackToSection()
        {
            AdjacencyCluster adjacencyCluster = ClusterFromFaces(BoxFaces(0, 0, 0, Length, Width, Height), out Space space, Analytical.PanelType.Wall);

            double area = adjacencyCluster.FloorArea(space, out FloorAreaCalculationMethod method, out List<Panel> panels);

            Assert.Equal(FloorAreaCalculationMethod.HorizontalSection, method);
            Assert.Null(panels);
            Assert.Equal(ExpectedBoxArea, area, 3);
        }

        [Theory]
        [InlineData(Analytical.PanelType.Roof)]
        [InlineData(Analytical.PanelType.Ceiling)]
        [InlineData(Analytical.PanelType.Shade)]
        [InlineData(Analytical.PanelType.Air)]
        [InlineData(Analytical.PanelType.SolarPanel)]
        [InlineData(Analytical.PanelType.CurtainWall)]
        [InlineData(Analytical.PanelType.WallInternal)]
        [InlineData(Analytical.PanelType.WallExternal)]
        public void FloorArea_IncompatibleFloorPanelType_NotCountedAsFloorPanel(PanelType panelType)
        {
            AdjacencyCluster adjacencyCluster = ClusterFromFaces(BoxFaces(0, 0, 0, Length, Width, Height), out Space space, panelType);

            adjacencyCluster.FloorArea(space, out FloorAreaCalculationMethod method, out List<Panel> panels);

            Assert.NotEqual(FloorAreaCalculationMethod.GeometricalFloorPanels, method);
            Assert.Null(panels);
        }

        [Theory]
        [InlineData(Analytical.PanelType.Floor)]
        [InlineData(Analytical.PanelType.FloorInternal)]
        [InlineData(Analytical.PanelType.FloorExposed)]
        [InlineData(Analytical.PanelType.FloorRaised)]
        [InlineData(Analytical.PanelType.SlabOnGrade)]
        [InlineData(Analytical.PanelType.UndergroundSlab)]
        [InlineData(Analytical.PanelType.Undefined)]
        public void FloorArea_AcceptedFloorPanelType_CountedAsFloorPanel(PanelType panelType)
        {
            AdjacencyCluster adjacencyCluster = ClusterFromFaces(BoxFaces(0, 0, 0, Length, Width, Height), out Space space, panelType);

            double area = adjacencyCluster.FloorArea(space, out FloorAreaCalculationMethod method, out List<Panel> panels);

            Assert.Equal(FloorAreaCalculationMethod.GeometricalFloorPanels, method);
            Assert.Single(panels);
            Assert.Equal(ExpectedBoxArea, area, 3);
        }

        // ---------------------------------------------------------------------------------------------
        // Invalid / incomplete geometry: preserve, never clobber
        // ---------------------------------------------------------------------------------------------

        [Fact]
        public void FloorArea_NoPanels_PreservesExistingArea()
        {
            AdjacencyCluster adjacencyCluster = new AdjacencyCluster();
            Space space = new Space("Unbounded", new Point3D(0, 0, 0));
            space.SetValue(SpaceParameter.Area, 42.0);
            adjacencyCluster.AddObject(space);

            double area = adjacencyCluster.FloorArea(space, out FloorAreaCalculationMethod method);

            Assert.Equal(FloorAreaCalculationMethod.Existing, method);
            Assert.Equal(42.0, area, 6);
            Assert.Equal(0, adjacencyCluster.UpdateFloorAreas());
            Assert.Equal(42.0, StoredArea(adjacencyCluster), 6);
        }

        [Fact]
        public void FloorArea_IncompleteShell_PreservesExistingArea()
        {
            // Two loose vertical panels: no closed shell, and nothing downward facing.
            AdjacencyCluster adjacencyCluster = new AdjacencyCluster();
            Space space = new Space("Incomplete", new Point3D(2, 1.5, 1));
            space.SetValue(SpaceParameter.Area, 42.0);
            adjacencyCluster.AddObject(space);

            foreach (Face3D face3D in new List<Face3D>
            {
                Quad(new Point3D(0, 0, 0), new Point3D(0, Width, 0), new Point3D(0, Width, Height), new Point3D(0, 0, Height)),
                Quad(new Point3D(Length, 0, 0), new Point3D(Length, Width, 0), new Point3D(Length, Width, Height), new Point3D(Length, 0, Height))
            })
            {
                Panel panel = Analytical.Create.Panel(Analytical.Query.DefaultConstruction(Analytical.PanelType.Wall), Analytical.PanelType.Wall, face3D);
                adjacencyCluster.AddObject(panel);
                adjacencyCluster.AddRelation(space, panel);
            }

            double area = adjacencyCluster.FloorArea(space, out FloorAreaCalculationMethod method);

            Assert.Equal(FloorAreaCalculationMethod.Existing, method);
            Assert.Equal(42.0, area, 6);
            Assert.Equal(0, adjacencyCluster.UpdateFloorAreas());
            Assert.Equal(42.0, StoredArea(adjacencyCluster), 6);
        }

        [Fact]
        public void FloorArea_NothingCalculableAndNoExistingValue_StoresNoArea()
        {
            AdjacencyCluster adjacencyCluster = new AdjacencyCluster();
            Space space = new Space("Unbounded", new Point3D(0, 0, 0));
            adjacencyCluster.AddObject(space);

            double area = adjacencyCluster.FloorArea(space, out FloorAreaCalculationMethod method);

            Assert.Equal(FloorAreaCalculationMethod.Undefined, method);
            Assert.True(double.IsNaN(area));

            // Neither 0, NaN nor infinity may be silently stored.
            Assert.Equal(0, adjacencyCluster.UpdateFloorAreas());
            Assert.False(adjacencyCluster.GetObject<Space>(space.Guid).TryGetValue(SpaceParameter.Area, out double _));
        }

        [Fact]
        public void FloorArea_ZeroAreaFloorPanel_ExcludedFromSum()
        {
            List<Face3D> face3Ds = BoxFaces(0, 0, 0, Length, Width, Height);
            AdjacencyCluster adjacencyCluster = ClusterFromFaces(face3Ds, out Space space, Analytical.PanelType.Floor);

            // A degenerate (collinear) horizontal floor panel: zero area, and it must contribute nothing
            // rather than corrupting the sum.
            Face3D face3D_Degenerate = Quad(new Point3D(0, 0, 0), new Point3D(1, 0, 0), new Point3D(2, 0, 0), new Point3D(3, 0, 0));
            if (face3D_Degenerate != null)
            {
                Panel panel_Degenerate = Analytical.Create.Panel(Analytical.Query.DefaultConstruction(Analytical.PanelType.Floor), Analytical.PanelType.Floor, face3D_Degenerate);
                if (panel_Degenerate != null)
                {
                    adjacencyCluster.AddObject(panel_Degenerate);
                    adjacencyCluster.AddRelation(space, panel_Degenerate);
                }
            }

            double area = adjacencyCluster.FloorArea(space, out FloorAreaCalculationMethod method, out List<Panel> panels);

            Assert.Equal(FloorAreaCalculationMethod.GeometricalFloorPanels, method);
            Assert.Equal(ExpectedBoxArea, area, 3);
            Assert.DoesNotContain(panels, x => x.GetArea() <= 0 || double.IsNaN(x.GetArea()));
        }

        [Fact]
        public void UpdateFloorAreas_InvalidResult_DoesNotOverwriteValidExistingArea()
        {
            // A cluster with one calculable space and one uncalculable space carrying a valid value: the
            // calculable one is updated, the other keeps exactly what it had.
            AdjacencyCluster adjacencyCluster = ClusterFromFaces(BoxFaces(0, 0, 0, Length, Width, Height), out Space space_Valid, Analytical.PanelType.Floor);

            Space space_Preserved = new Space("Preserved", new Point3D(100, 100, 100));
            space_Preserved.SetValue(SpaceParameter.Area, 42.0);
            adjacencyCluster.AddObject(space_Preserved);

            Assert.Equal(1, adjacencyCluster.UpdateFloorAreas());

            Assert.True(adjacencyCluster.GetObject<Space>(space_Valid.Guid).TryGetValue(SpaceParameter.Area, out double area_Valid));
            Assert.Equal(ExpectedBoxArea, area_Valid, 3);

            Assert.True(adjacencyCluster.GetObject<Space>(space_Preserved.Guid).TryGetValue(SpaceParameter.Area, out double area_Preserved));
            Assert.Equal(42.0, area_Preserved, 6);
        }

        [Fact]
        public void FloorArea_NullSpace_ReturnsNaNAndDoesNotThrow()
        {
            AdjacencyCluster adjacencyCluster = new AdjacencyCluster();

            Assert.True(double.IsNaN(adjacencyCluster.FloorArea(null, out FloorAreaCalculationMethod method)));
            Assert.Equal(FloorAreaCalculationMethod.Undefined, method);
            Assert.Equal(0, Analytical.Modify.UpdateFloorAreas(null));
        }

        // ---------------------------------------------------------------------------------------------
        // Helpers
        // ---------------------------------------------------------------------------------------------

        private static double StoredArea(AdjacencyCluster adjacencyCluster)
        {
            Assert.NotNull(adjacencyCluster);
            Space space = Assert.Single(adjacencyCluster.GetSpaces());
            Assert.True(space.TryGetValue(SpaceParameter.Area, out double area), "SpaceParameter.Area was not set.");
            return area;
        }

        /// <summary>
        /// Builds an adjacency cluster by hand from a closed set of faces, forcing the panel type of the first
        /// (bottom) faces so the panel-type protection can be exercised without UpdatePanelTypes overriding it.
        /// </summary>
        private static AdjacencyCluster ClusterFromFaces(List<Face3D> face3Ds, out Space space, params PanelType[] bottomPanelTypes)
        {
            AdjacencyCluster adjacencyCluster = new AdjacencyCluster();
            Shell shell = new Shell(face3Ds);
            space = new Space("Test Space", shell.InternalPoint3D());
            adjacencyCluster.AddObject(space);

            double minZ = face3Ds.ConvertAll(x => x.GetBoundingBox().Min.Z).Min();

            int bottomIndex = 0;
            foreach (Face3D face3D in face3Ds)
            {
                bool downward = System.Math.Abs(Geometry.Spatial.Query.Tilt(face3D.GetPlane().Normal) - 90) > 45;
                PanelType panelType = Analytical.PanelType.Wall;

                if (downward && face3D.GetBoundingBox().Max.Z <= minZ + Core.Tolerance.MacroDistance)
                {
                    panelType = bottomIndex < bottomPanelTypes.Length ? bottomPanelTypes[bottomIndex] : bottomPanelTypes[bottomPanelTypes.Length - 1];
                    bottomIndex++;
                }
                else if (downward)
                {
                    panelType = Analytical.PanelType.Roof;
                }

                Panel panel = TypedPanel(panelType, face3D);
                adjacencyCluster.AddObject(panel);
                adjacencyCluster.AddRelation(space, panel);
            }

            return adjacencyCluster;
        }

        /// <summary>
        /// Builds a panel carrying exactly the requested type. Create.Panel(construction, panelType, face3D)
        /// refuses <see cref="PanelType.Undefined"/>, so an Undefined panel - which real models do contain,
        /// from deserialization and from retyping - is produced the same way Modify.UpdatePanelTypes does.
        /// </summary>
        private static Panel TypedPanel(PanelType panelType, Face3D face3D)
        {
            if (panelType != Analytical.PanelType.Undefined)
            {
                return Analytical.Create.Panel(Analytical.Query.DefaultConstruction(panelType), panelType, face3D);
            }

            Panel panel = Analytical.Create.Panel(Analytical.Query.DefaultConstruction(Analytical.PanelType.Floor), Analytical.PanelType.Floor, face3D);
            return Analytical.Create.Panel(panel, Analytical.PanelType.Undefined);
        }

        private static List<Face3D> BoxFaces(double minX, double minY, double minZ, double maxX, double maxY, double maxZ)
        {
            Point3D p000 = new Point3D(minX, minY, minZ);
            Point3D p100 = new Point3D(maxX, minY, minZ);
            Point3D p110 = new Point3D(maxX, maxY, minZ);
            Point3D p010 = new Point3D(minX, maxY, minZ);
            Point3D p001 = new Point3D(minX, minY, maxZ);
            Point3D p101 = new Point3D(maxX, minY, maxZ);
            Point3D p111 = new Point3D(maxX, maxY, maxZ);
            Point3D p011 = new Point3D(minX, maxY, maxZ);

            return new List<Face3D>
            {
                Quad(p000, p010, p110, p100), // bottom - index 0
                Quad(p001, p101, p111, p011), // top
                Quad(p000, p100, p101, p001),
                Quad(p100, p110, p111, p101),
                Quad(p110, p010, p011, p111),
                Quad(p010, p000, p001, p011)
            };
        }

        private static Shell BoxShell(double minX, double minY, double minZ, double maxX, double maxY, double maxZ)
        {
            return new Shell(BoxFaces(minX, minY, minZ, maxX, maxY, maxZ));
        }

        /// <summary>A closed prism whose lower boundary is a single tilted rectangle rising by <paramref name="rise"/> over <paramref name="length"/>.</summary>
        private static Shell RampShell(double length, double width, double rise, double top)
        {
            Point3D f00 = new Point3D(0, 0, 0);
            Point3D f10 = new Point3D(length, 0, rise);
            Point3D f11 = new Point3D(length, width, rise);
            Point3D f01 = new Point3D(0, width, 0);

            Point3D t00 = new Point3D(0, 0, top);
            Point3D t10 = new Point3D(length, 0, top);
            Point3D t11 = new Point3D(length, width, top);
            Point3D t01 = new Point3D(0, width, top);

            return new Shell(new List<Face3D>
            {
                Quad(f00, f10, f11, f01), // tilted floor
                Quad(t00, t01, t11, t10), // flat roof
                Quad(f00, f01, t01, t00), // low end
                Quad(f10, t10, t11, f11), // high end
                Quad(f00, t00, t10, f10), // y = 0
                Quad(f01, f11, t11, t01)  // y = width
            });
        }

        private static Face3D Quad(Point3D point1, Point3D point2, Point3D point3, Point3D point4)
        {
            Polygon3D polygon3D = Geometry.Spatial.Create.Polygon3D(new[] { point1, point2, point3, point4 }, Core.Tolerance.Distance);
            return polygon3D == null ? null : new Face3D(polygon3D);
        }
    }
}
