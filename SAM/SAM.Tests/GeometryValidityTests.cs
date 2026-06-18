// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Collections.Generic;
using System.Linq;
using SAM.Geometry.Planar;
using Xunit;

namespace SAM.Tests
{
    // Covers the NetTopologySuite-backed topological validity check
    // (Query.IsValidTopologically) and the GeometryFixer-backed repair
    // (Query.Fix) added to close the SAM/NTS geometry-repair gap.
    public class GeometryValidityTests
    {
        private static Polygon2D UnitSquare()
        {
            return new Polygon2D(new List<Point2D>()
            {
                new Point2D(0, 0),
                new Point2D(1, 0),
                new Point2D(1, 1),
                new Point2D(0, 1),
            });
        }

        // (0,0)->(1,1)->(1,0)->(0,1) is a self-intersecting "bowtie": the two
        // diagonals cross at (0.5, 0.5), so the ring is topologically invalid.
        private static Polygon2D Bowtie()
        {
            return new Polygon2D(new List<Point2D>()
            {
                new Point2D(0, 0),
                new Point2D(1, 1),
                new Point2D(1, 0),
                new Point2D(0, 1),
            });
        }

        [Fact]
        public void IsValidTopologically_UnitSquare_IsValid()
        {
            Polygon2D polygon2D = UnitSquare();
            Assert.True(polygon2D.IsValidTopologically(out string reason));
            Assert.Null(reason);

            Face2D face2D = polygon2D;
            Assert.True(face2D.IsValidTopologically());
        }

        [Fact]
        public void IsValidTopologically_Bowtie_IsInvalidWithReason()
        {
            Polygon2D polygon2D = Bowtie();
            Assert.False(polygon2D.IsValidTopologically(out string reason));
            Assert.False(string.IsNullOrWhiteSpace(reason));
        }

        [Fact]
        public void Fix_ValidFace_ReturnedUnchanged()
        {
            Face2D face2D = UnitSquare();
            Face2D result = face2D.Fix();

            Assert.NotNull(result);
            Assert.Same(face2D, result);
        }

        [Fact]
        public void Fix_Bowtie_ProducesValidGeometry()
        {
            Face2D face2D = Bowtie();
            Assert.False(face2D.IsValidTopologically());

            Face2D result = face2D.Fix();

            Assert.NotNull(result);
            Assert.True(result.IsValidTopologically());
            Assert.True(result.GetArea() > 0);
        }

        [Fact]
        public void Fix_Collection_KeepsEveryRepairedFace()
        {
            List<Face2D> face2Ds = new List<Face2D>() { UnitSquare(), Bowtie() };

            List<Face2D> result = face2Ds.Fix();

            Assert.NotNull(result);

            // The valid square survives and the bowtie repairs into the two
            // triangles it describes, so nothing is silently dropped.
            Assert.True(result.Count >= 2);
            Assert.All(result, x => Assert.True(x.IsValidTopologically()));

            // Square (area 1) + bowtie triangles (combined area ~0.5).
            double totalArea = result.Sum(x => x.GetArea());
            Assert.True(totalArea > 1.0);
        }
    }
}
