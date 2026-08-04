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
    /// TEMPORARY diagnostic for SAM-BIM/SAM#65, third pass. Pass 1 showed the CI runner gets a completely
    /// empty cluster from the shell/panel creation overload. Pass 2 walked every reconstruction stage that
    /// runs before a space is created and found the CI results identical to a development machine's, so the
    /// divergence is in the tail of that overload.
    /// <para>
    /// The tail is what separates the failing overload from the two-argument one that passes: it alone runs
    /// the closure gate and <see cref="Query.FixEdges(AdjacencyCluster, bool, double)"/>. This pass applies
    /// those tail steps, one at a time, to the cluster the passing overload builds, so whichever one empties
    /// the model on CI is named.
    /// </para>
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
        public void Diagnostic_DumpReconstructionTail()
        {
            double silverSpacing = Core.Tolerance.MacroDistance;
            double tolerance_Distance = Core.Tolerance.Distance;

            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine();
            stringBuilder.AppendLine("===== SAM#65 CI diagnostic, pass 3 =====");
            stringBuilder.AppendLine("ProcessorCount = " + Environment.ProcessorCount);
            stringBuilder.AppendLine("Framework      = " + System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription);

            AdjacencyCluster adjacencyCluster = Analytical.Create.AdjacencyCluster(
                new List<Shell> { BoxShell() },
                new List<Space> { new Space("Test Space", new Point3D(2, 1.5, 1)) });

            Describe(stringBuilder, adjacencyCluster, "start (two-argument overload)");

            // The closure gate the failing overload applies to every space.
            List<Space> spaces = adjacencyCluster?.GetSpaces();
            if (spaces != null)
            {
                foreach (Space space in spaces)
                {
                    Shell shell = adjacencyCluster.Shell(space);
                    BoundingBox3D boundingBox3D = shell?.GetBoundingBox();
                    stringBuilder.AppendLine("closure gate for '" + space.Name + "':");
                    stringBuilder.AppendLine("  shell = " + (shell == null ? "null" : "present")
                        + ", bboxValid = " + (boundingBox3D == null ? "n/a" : boundingBox3D.IsValid().ToString())
                        + ", bboxVolume = " + (boundingBox3D == null ? "n/a" : boundingBox3D.GetVolume().ToString("R"))
                        + ", faces = " + (shell?.Face3Ds == null ? "n/a" : shell.Face3Ds.Count.ToString())
                        + ", IsClosed(10x) = " + (shell == null ? "n/a" : shell.IsClosed(silverSpacing * 10).ToString()));
                }
            }

            // The tail steps, applied one at a time so the one that empties the model is unambiguous.
            Step(stringBuilder, "RemoveInvalidAirPanels", ref adjacencyCluster, x =>
            {
                List<Guid> guids = x.RemoveInvalidAirPanels();
                return x;
            });

            Step(stringBuilder, "UpdateNormals", ref adjacencyCluster, x => x.UpdateNormals(false, true, false, silverSpacing, tolerance_Distance));

            Step(stringBuilder, "FixEdges", ref adjacencyCluster, x => x.FixEdges(false, tolerance_Distance));

            Step(stringBuilder, "Normalize", ref adjacencyCluster, x =>
            {
                x.Normalize(false);
                return x;
            });

            Step(stringBuilder, "UpdateFloorAreas", ref adjacencyCluster, x =>
            {
                x.UpdateFloorAreas(silverSpacing: silverSpacing, tolerance_Distance: tolerance_Distance);
                return x;
            });

            stringBuilder.AppendLine("===== end diagnostic =====");

            Assert.True(false, stringBuilder.ToString());
        }

        private static void Step(StringBuilder stringBuilder, string title, ref AdjacencyCluster adjacencyCluster, Func<AdjacencyCluster, AdjacencyCluster> func)
        {
            if (adjacencyCluster == null)
            {
                stringBuilder.AppendLine("after " + title + ": skipped, cluster already null");
                return;
            }

            try
            {
                adjacencyCluster = func(adjacencyCluster);
            }
            catch (Exception exception)
            {
                stringBuilder.AppendLine("after " + title + ": THREW " + exception.GetType().Name + ": " + exception.Message);
                return;
            }

            Describe(stringBuilder, adjacencyCluster, "after " + title);
        }

        private static void Describe(StringBuilder stringBuilder, AdjacencyCluster adjacencyCluster, string title)
        {
            if (adjacencyCluster == null)
            {
                stringBuilder.AppendLine(title + ": cluster = null");
                return;
            }

            List<Space> spaces = adjacencyCluster.GetSpaces();
            List<Panel> panels = adjacencyCluster.GetPanels();
            stringBuilder.AppendLine(title + ": spaces = " + (spaces == null ? "null" : spaces.Count.ToString())
                + ", panels = " + (panels == null ? "null" : panels.Count.ToString()));

            if (spaces != null)
            {
                foreach (Space space in spaces)
                {
                    space.TryGetValue(SpaceParameter.Area, out double area);
                    List<Panel> panels_Related = adjacencyCluster.GetRelatedObjects<Panel>(space);
                    stringBuilder.AppendLine("  '" + space.Name + "' Area = " + area.ToString("R") + ", related panels = " + (panels_Related == null ? "null" : panels_Related.Count.ToString()));
                }
            }
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
