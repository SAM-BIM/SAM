// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical;
using SAM.Geometry.Spatial;
using System.Collections.Generic;
using Xunit;

namespace SAM.Tests
{
    public class AdjacencyClusterFloorAreaTests
    {
        [Fact]
        public void CreateAdjacencyClusterByShells_SetsMidHeightPlanArea()
        {
            Shell shell = CreateBoxShell(0, 0, 0, 4, 3, 2);
            List<Shell> shells = new List<Shell> { shell };
            List<Space> spaces = new List<Space>
            {
                new Space("Test Space", new Point3D(2, 1.5, 1))
            };

            AdjacencyCluster adjacencyCluster = Analytical.Create.AdjacencyCluster(shells, spaces);

            Assert.NotNull(adjacencyCluster);
            Space space = Assert.Single(adjacencyCluster.GetSpaces());
            Assert.True(space.TryGetValue(SpaceParameter.Area, out double area));
            Assert.Equal(12, area, 6);
        }

        [Fact]
        public void UpdateSpaceAreas_FailedSection_DoesNotOverwriteExistingArea()
        {
            AdjacencyCluster adjacencyCluster = new AdjacencyCluster();
            Space space = new Space("Unbounded Space", new Point3D(0, 0, 0));
            space.SetValue(SpaceParameter.Area, 42.0);
            adjacencyCluster.AddObject(space);

            int updated = adjacencyCluster.UpdateSpaceAreas();

            Assert.Equal(0, updated);
            Space result = adjacencyCluster.GetObject<Space>(space.Guid);
            Assert.True(result.TryGetValue(SpaceParameter.Area, out double area));
            Assert.Equal(42.0, area, 6);
        }

        private static Shell CreateBoxShell(double minX, double minY, double minZ, double maxX, double maxY, double maxZ)
        {
            Point3D p000 = new Point3D(minX, minY, minZ);
            Point3D p100 = new Point3D(maxX, minY, minZ);
            Point3D p110 = new Point3D(maxX, maxY, minZ);
            Point3D p010 = new Point3D(minX, maxY, minZ);
            Point3D p001 = new Point3D(minX, minY, maxZ);
            Point3D p101 = new Point3D(maxX, minY, maxZ);
            Point3D p111 = new Point3D(maxX, maxY, maxZ);
            Point3D p011 = new Point3D(minX, maxY, maxZ);

            return new Shell(new List<Face3D>
            {
                CreateQuad(p000, p010, p110, p100),
                CreateQuad(p001, p101, p111, p011),
                CreateQuad(p000, p100, p101, p001),
                CreateQuad(p100, p110, p111, p101),
                CreateQuad(p110, p010, p011, p111),
                CreateQuad(p010, p000, p001, p011)
            });
        }

        private static Face3D CreateQuad(Point3D point1, Point3D point2, Point3D point3, Point3D point4)
        {
            return new Face3D(new Polygon3D(new[] { point1, point2, point3, point4 }));
        }
    }
}
