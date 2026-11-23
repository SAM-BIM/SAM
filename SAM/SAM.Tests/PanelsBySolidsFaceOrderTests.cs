// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical;
using SAM.Core;
using SAM.Geometry.Spatial;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Xunit;
using static SAM.Tests.PanelsBySolidTests;

namespace SAM.Tests
{
    /// <summary>
    /// <see cref="Analytical.Create.PanelsBySolid"/> and <see cref="Analytical.Create.PanelsBySolids"/> on the same solids
    /// with their faces listed in another order. A solid that is not watertight (here a slab with a stray face inside it,
    /// close to one side) answers a point-in-solid test differently along different rays; the extraction must not let the
    /// order of the faces choose the ray. Equal geometry gives the same Panels: extent, openings and properties.
    /// </summary>
    public class PanelsBySolidsFaceOrderTests
    {
        private const double MinThickness = 0.04;

        private static Face3D Quad(Point3D a, Point3D b, Point3D c, Point3D d) => new Face3D(new Polygon3D(new List<Point3D> { a, b, c, d }));

        /// <summary>
        /// A 0.15 m slab (x 0..6, y -1..4, z -0.15..0) with one stray face standing inside it 0.1 m from its west side.
        /// </summary>
        private static Shell SlabWithStrayFace()
        {
            List<Face3D> face3Ds = Box(0, -1, -0.15, 6, 4, 0).Face3Ds;
            face3Ds.Add(Quad(new Point3D(0.1, 0, -0.15), new Point3D(0.1, 3, -0.15), new Point3D(0.1, 3, 0), new Point3D(0.1, 0, 0)));
            return new Shell(face3Ds);
        }

        private static Shell Ceiling() => Box(0, -1, 3, 6, 4, 3.15);

        /// <summary>
        /// A 0.45 m facade (x 0..0.45) ends against a block 0.228 m long, shorter than the facade's thickness; a 0.45 m wall
        /// running east (y -0.45..0) ends with its face on the block's other edge. Below, the slab with the stray face; above,
        /// a ceiling. The block is index 2.
        /// </summary>
        private static List<Shell> Corner(bool withFacade = true, bool withCrossingWall = true)
        {
            List<Shell> result = new List<Shell> { SlabWithStrayFace(), Ceiling(), Box(0, 0, 0, 0.45, 0.228, 3) };
            if (withFacade)
            {
                result.Add(Box(0, 0.228, 0, 0.45, 3, 3));
            }

            if (withCrossingWall)
            {
                result.Add(Box(0.45, -0.45, 0, 5, 0, 3));
            }

            return result;
        }

        /// <summary>
        /// The same solid with its faces listed in every rotation of the original order, and each rotation reversed.
        /// </summary>
        private static IEnumerable<(string Name, Shell Shell)> FaceOrders(Shell shell)
        {
            List<Face3D> face3Ds = shell.Face3Ds;
            for (int i = 0; i < face3Ds.Count; i++)
            {
                List<Face3D> rotation = face3Ds.Skip(i).Concat(face3Ds.Take(i)).ToList();
                yield return (string.Format(CultureInfo.InvariantCulture, "rotation {0}", i), new Shell(rotation));
                yield return (string.Format(CultureInfo.InvariantCulture, "rotation {0} reversed", i), new Shell(Enumerable.Reverse(rotation)));
            }
        }

        /// <summary>
        /// What a Panel is physically, independent of which face of a pair came first: type, thickness, area, extent,
        /// the line of its normal, holes and source role.
        /// </summary>
        private static string Physical(Panel panel)
        {
            Face3D face3D = panel.GetFace3D();
            Vector3D normal = face3D.GetPlane().Normal.Unit;
            BoundingBox3D box = face3D.GetBoundingBox();
            panel.TryGetValue(PanelSourceParameter.PhysicalThickness, out double thickness);
            panel.TryGetValue(PanelSourceParameter.SourceRole, out string role);
            string F(double x) => System.Math.Round(x, 6).ToString("0.000000", CultureInfo.InvariantCulture);
            return string.Join("|", panel.PanelType, F(thickness), F(panel.GetArea()), F(System.Math.Abs(normal.X)), F(System.Math.Abs(normal.Y)), F(System.Math.Abs(normal.Z)),
                F(box.Min.X), F(box.Min.Y), F(box.Min.Z), F(box.Max.X), F(box.Max.Y), F(box.Max.Z), face3D.GetInternalEdge3Ds()?.Count ?? 0, role);
        }

        private static List<string> Physical(IEnumerable<Panel> panels) => panels.Select(Physical).OrderBy(x => x, System.StringComparer.Ordinal).ToList();

        [Fact]
        public void PanelsBySolid_SlabWithStrayFaceInAnyFaceOrder_GivesTheSameSlab()
        {
            // Arrange
            List<string> expected = null;

            foreach ((string name, Shell shell) in FaceOrders(SlabWithStrayFace()))
            {
                // Act
                List<Panel> panels = shell.PanelsBySolid(new Core.Diagnostics(), minThickness: MinThickness);

                // Assert: one 0.15 m Floor over the whole slab, whatever the order of its faces.
                Assert.True(panels.Count == 1, name);
                Panel floor = panels[0];
                Assert.True(floor.PanelType == PanelType.Floor, name);
                Assert.True(floor.TryGetValue(PanelSourceParameter.PhysicalThickness, out double thickness), name);
                Assert.Equal(0.15, thickness, 6);
                Assert.Equal(30, floor.GetArea(), 6);
                expected = expected ?? Physical(panels);
                Assert.True(expected.SequenceEqual(Physical(panels)), name);
            }
        }

        [Fact]
        public void PanelsBySolids_JunctionBlockOnSlabWithStrayFaceInAnyFaceOrder_KeepsTheSameJunction()
        {
            // Arrange
            List<Shell> shells = Corner();

            // The watertight slab is the reference: there the block is kept as the junction it is.
            List<Shell> watertight = new List<Shell>(shells) { [0] = Box(0, -1, -0.15, 6, 4, 0) };
            List<List<Panel>> reference = watertight.PanelsBySolids(new Core.Diagnostics(), null, MinThickness);
            Assert.True(reference[2].Count == 1, "watertight slab");
            List<string> expected = reference.ConvertAll(x => string.Join(";", Physical(x)));
            List<List<(string Name, Shell Shell)>> orders = shells.ConvertAll(x => FaceOrders(x).ToList());

            // Every solid in its k-th order (rotations of different lengths wrap round), so each order is tried for each solid.
            int count = orders.Max(x => x.Count);
            for (int k = 0; k < count; k++)
            {
                List<Shell> shells_Order = orders.ConvertAll(x => x[k % x.Count].Shell);
                string name = string.Join(", ", orders.Select(x => x[k % x.Count].Name));

                // Act
                Core.Diagnostics diagnostics = new Core.Diagnostics(DiagnosticDetail.Detailed);
                List<List<Panel>> panels = shells_Order.PanelsBySolids(diagnostics, null, MinThickness);

                // Assert: the block is kept as a wall on the facade's mid-plane over its own length, and every solid gives
                // the same Panels as in any other order.
                Assert.True(panels[2].Count == 1, name);
                Panel block = panels[2][0];
                Assert.True(block.PanelType == PanelType.Wall, name);
                Assert.All(((ISegmentable3D)block.GetFace3D().GetExternalEdge3D()).GetPoints(), x => Assert.Equal(0.225, x.X, 6));
                Assert.Equal(0.228 * 3, block.GetArea(), 6);
                Assert.True(block.TryGetValue(PanelSourceParameter.PhysicalThickness, out double thickness), name);
                Assert.Equal(0.45, thickness, 6);
                Diagnostic diagnostic = diagnostics.FirstOrDefault(x => x.SourceIndex == 2 && x.Code == BySolidsDiagnosticCode.PanelsContinuation);
                Assert.True(diagnostic != null && diagnostic.Message.Contains("junction block"), name);
                List<string> actual = panels.ConvertAll(x => string.Join(";", Physical(x)));
                expected = expected ?? actual;
                Assert.True(expected.SequenceEqual(actual), name);
            }
        }

        /// <summary>
        /// Controls on the same slab, in every face order: what has no wall of its thickness beyond it (an isolated block,
        /// a block at a free wall end), a stair flight and a reveal lining narrower than minThickness stay skipped, as
        /// they are when the slab is watertight.
        /// </summary>
        [Fact]
        public void PanelsBySolids_BlocksStairAndRevealOnSlabWithStrayFaceInAnyFaceOrder_StaySkipped()
        {
            // Arrange: a solid stepped flight (8 risers of 0.25 m, goings of 0.3 m, 1 m wide) on the slab, and a 0.03 m reveal
            // lining against the crossing wall.
            List<(double U, double V)> flight = new List<(double U, double V)> { (1.5, 0), (3.9, 0) };
            for (int i = 8; i >= 1; i--)
            {
                flight.Add((1.5 + 0.3 * i, 0.25 * i));
                flight.Add((1.5 + 0.3 * (i - 1), 0.25 * i));
            }

            Shell stair = Prism((u, v, w) => new Point3D(u, w, v), flight, null, 1.5, 2.5);
            Shell reveal = Box(2, 0, 0, 2.03, 0.12, 2.2);
            List<(string Case, List<Shell> Shells, int Index)> cases = new List<(string, List<Shell>, int)>
            {
                ("isolated block", Corner(withFacade: false, withCrossingWall: false), 2),
                ("block at a free wall end", Corner(withCrossingWall: false), 2),
                ("stair flight", new List<Shell> { SlabWithStrayFace(), Ceiling(), stair }, 2),
                ("reveal lining", new List<Shell> { SlabWithStrayFace(), Ceiling(), Box(0.45, -0.45, 0, 5, 0, 3), reveal }, 3),
            };

            foreach ((string name, List<Shell> shells, int index) in cases)
            {
                // The watertight slab is the reference: the stray face changes nothing.
                List<Shell> watertight = new List<Shell>(shells) { [0] = Box(0, -1, -0.15, 6, 4, 0) };
                Assert.True(watertight.PanelsBySolids(new Core.Diagnostics(), null, MinThickness)[index].Count == 0, name);

                foreach ((string order, Shell slab) in FaceOrders(shells[0]))
                {
                    // Act
                    List<Shell> shells_Order = new List<Shell>(shells) { [0] = slab };
                    Core.Diagnostics diagnostics = new Core.Diagnostics(DiagnosticDetail.Detailed);
                    List<List<Panel>> panels = shells_Order.PanelsBySolids(diagnostics, null, MinThickness);

                    // Assert
                    Assert.True(panels[0].Count == 1, name + ", slab " + order);
                    Assert.True(panels[index].Count == 0, name + ", slab " + order);
                    Assert.True(!diagnostics.Any(x => x.SourceIndex == index && x.Code == BySolidsDiagnosticCode.PanelsContinuation), name + ", slab " + order);
                }
            }
        }
    }
}
