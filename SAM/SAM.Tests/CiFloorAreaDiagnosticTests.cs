// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical;
using SAM.Geometry.Spatial;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace SAM.Tests
{
    /// <summary>
    /// TEMPORARY diagnostic for SAM-BIM/SAM#65. Three <see cref="SpaceFloorAreaTests"/> box tests fail on the
    /// CI runner and on no development machine tried so far, with the adjacency cluster coming back with zero
    /// spaces. This test deliberately always fails so that its dump of the reconstruction state reaches the CI
    /// log, letting the divergence be located from evidence instead of guessed at.
    /// </summary>
    /// <remarks>
    /// Test-only: it touches no production code, and the whole file is reverted once the CI output is read.
    /// </remarks>
    public class CiFloorAreaDiagnosticTests
    {
        private const double Length = 4;
        private const double Width = 3;
        private const double Height = 2;

        [Fact]
        public void Diagnostic_DumpBoxReconstruction()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine();
            stringBuilder.AppendLine("===== SAM#65 CI diagnostic =====");
            stringBuilder.AppendLine("ProcessorCount = " + Environment.ProcessorCount);
            stringBuilder.AppendLine("OS             = " + System.Runtime.InteropServices.RuntimeInformation.OSDescription);
            stringBuilder.AppendLine("Framework      = " + System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription);
            stringBuilder.AppendLine("Architecture   = " + System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture);
            stringBuilder.AppendLine("Tolerance.Distance      = " + Core.Tolerance.Distance.ToString("R"));
            stringBuilder.AppendLine("Tolerance.MacroDistance = " + Core.Tolerance.MacroDistance.ToString("R"));
            stringBuilder.AppendLine("Tolerance.Angle         = " + Core.Tolerance.Angle.ToString("R"));

            // The raw shell, before any reconstruction runs. If this already differs the problem is in shell
            // construction rather than in Create.AdjacencyCluster.
            Shell shell_Raw = BoxShell();
            stringBuilder.AppendLine();
            stringBuilder.AppendLine("-- input shell --");
            Describe(stringBuilder, shell_Raw);

            List<Panel> panels_Raw = Analytical.Create.Panels(BoxShell());
            stringBuilder.AppendLine("Create.Panels count = " + (panels_Raw == null ? "null" : panels_Raw.Count.ToString()));
            if (panels_Raw != null)
            {
                foreach (Panel panel in panels_Raw)
                {
                    DescribePanel(stringBuilder, panel);
                }
            }

            // The control: this overload passes on CI, so anything it reports is known-good behaviour.
            Report(stringBuilder, "CONTROL AdjacencyCluster(shells, spaces)", () =>
                Analytical.Create.AdjacencyCluster(
                    new List<Shell> { BoxShell() },
                    new List<Space> { new Space("Test Space", new Point3D(2, 1.5, 1)) }));

            // The three that fail on CI.
            Report(stringBuilder, "FAILING AdjacencyCluster(shells)", () =>
                Analytical.Create.AdjacencyCluster(new List<Shell> { BoxShell() }));

            Report(stringBuilder, "FAILING AdjacencyCluster(shells, spaces, panels)", () =>
            {
                Shell shell = BoxShell();
                return Analytical.Create.AdjacencyCluster(
                    new List<Shell> { shell },
                    new List<Space> { new Space("Test Space", new Point3D(2, 1.5, 1)) },
                    Analytical.Create.Panels(shell),
                    true,
                    true);
            });

            Report(stringBuilder, "FAILING AdjacencyCluster(spaces, panels)", () =>
            {
                Shell shell = BoxShell();
                return Analytical.Create.AdjacencyCluster(
                    new List<Space> { new Space("Test Space", new Point3D(2, 1.5, 1)) },
                    Analytical.Create.Panels(shell),
                    0.1,
                    true,
                    true);
            });

            stringBuilder.AppendLine("===== end diagnostic =====");

            Assert.True(false, stringBuilder.ToString());
        }

        /// <summary>
        /// Runs one creation path and reports what came back. When no space survived, the panels the cluster
        /// still holds are reassembled into a shell and measured against the same three conditions the
        /// creation path's closure gate applies, so the gate that fired can be identified.
        /// </summary>
        private static void Report(StringBuilder stringBuilder, string title, Func<AdjacencyCluster> func)
        {
            stringBuilder.AppendLine();
            stringBuilder.AppendLine("-- " + title + " --");

            AdjacencyCluster adjacencyCluster;
            try
            {
                adjacencyCluster = func();
            }
            catch (Exception exception)
            {
                stringBuilder.AppendLine("THREW " + exception.GetType().Name + ": " + exception.Message);
                return;
            }

            if (adjacencyCluster == null)
            {
                stringBuilder.AppendLine("cluster = null");
                return;
            }

            List<Space> spaces = adjacencyCluster.GetSpaces();
            stringBuilder.AppendLine("spaces = " + (spaces == null ? "null" : spaces.Count.ToString()));

            if (spaces != null)
            {
                foreach (Space space in spaces)
                {
                    space.TryGetValue(SpaceParameter.Area, out double area);
                    stringBuilder.AppendLine("  space '" + space.Name + "' Area = " + area.ToString("R"));
                    Describe(stringBuilder, adjacencyCluster.Shell(space), "  space shell: ");
                }
            }

            List<Panel> panels = adjacencyCluster.GetPanels();
            stringBuilder.AppendLine("panels = " + (panels == null ? "null" : panels.Count.ToString()));
            if (panels == null || panels.Count == 0)
            {
                return;
            }

            foreach (Panel panel in panels)
            {
                DescribePanel(stringBuilder, panel);
            }

            List<Face3D> face3Ds = panels.ConvertAll(x => x.GetFace3D()).FindAll(x => x != null);
            if (face3Ds.Count != 0)
            {
                stringBuilder.AppendLine("shell rebuilt from the cluster's panels:");
                Describe(stringBuilder, new Shell(face3Ds), "  ");
            }
        }

        /// <summary>
        /// Reports the three quantities the closure gate tests: a valid bounding box with usable volume, more
        /// than two faces, and closure at ten times the sliver spacing.
        /// </summary>
        private static void Describe(StringBuilder stringBuilder, Shell shell, string prefix = "")
        {
            if (shell == null)
            {
                stringBuilder.AppendLine(prefix + "shell = null");
                return;
            }

            List<Face3D> face3Ds = shell.Face3Ds;
            stringBuilder.AppendLine(prefix + "face3Ds = " + (face3Ds == null ? "null" : face3Ds.Count.ToString()));

            BoundingBox3D boundingBox3D = shell.GetBoundingBox();
            if (boundingBox3D == null)
            {
                stringBuilder.AppendLine(prefix + "boundingBox3D = null");
            }
            else
            {
                stringBuilder.AppendLine(prefix + "boundingBox3D IsValid = " + boundingBox3D.IsValid() + ", volume = " + boundingBox3D.GetVolume().ToString("R") + ", min = " + boundingBox3D.Min + ", max = " + boundingBox3D.Max);
            }

            try
            {
                stringBuilder.AppendLine(prefix + "IsClosed(MacroDistance * 10) = " + shell.IsClosed(Core.Tolerance.MacroDistance * 10));
                stringBuilder.AppendLine(prefix + "IsClosed(MacroDistance)      = " + shell.IsClosed(Core.Tolerance.MacroDistance));
                stringBuilder.AppendLine(prefix + "Volume = " + shell.Volume(Core.Tolerance.MacroDistance, Core.Tolerance.Distance).ToString("R"));
            }
            catch (Exception exception)
            {
                stringBuilder.AppendLine(prefix + "closure/volume THREW " + exception.GetType().Name + ": " + exception.Message);
            }
        }

        private static void DescribePanel(StringBuilder stringBuilder, Panel panel)
        {
            Face3D face3D = panel?.GetFace3D();
            if (face3D == null)
            {
                stringBuilder.AppendLine("  panel with no Face3D, type = " + panel?.PanelType);
                return;
            }

            Vector3D normal = face3D.GetPlane()?.Normal;
            stringBuilder.AppendLine("  panel type = " + panel.PanelType + ", area = " + face3D.GetArea().ToString("R") + ", normal = " + (normal == null ? "null" : normal.ToString()));
        }

        private static Shell BoxShell()
        {
            Point3D p000 = new Point3D(0, 0, 0);
            Point3D p100 = new Point3D(Length, 0, 0);
            Point3D p110 = new Point3D(Length, Width, 0);
            Point3D p010 = new Point3D(0, Width, 0);

            Point3D p001 = new Point3D(0, 0, Height);
            Point3D p101 = new Point3D(Length, 0, Height);
            Point3D p111 = new Point3D(Length, Width, Height);
            Point3D p011 = new Point3D(0, Width, Height);

            return new Shell(new List<Face3D>
            {
                Quad(p000, p010, p110, p100),
                Quad(p001, p101, p111, p011),
                Quad(p000, p100, p101, p001),
                Quad(p100, p110, p111, p101),
                Quad(p110, p010, p011, p111),
                Quad(p010, p000, p001, p011)
            });
        }

        private static Face3D Quad(Point3D point1, Point3D point2, Point3D point3, Point3D point4)
        {
            Polygon3D polygon3D = Geometry.Spatial.Create.Polygon3D(new[] { point1, point2, point3, point4 }, Core.Tolerance.Distance);
            return polygon3D == null ? null : new Face3D(polygon3D);
        }
    }
}
