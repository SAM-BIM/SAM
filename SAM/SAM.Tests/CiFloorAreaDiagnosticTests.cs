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
    /// TEMPORARY diagnostic for SAM-BIM/SAM#65, second pass. The first pass established that on the CI runner
    /// the shell/panel creation overload returns a completely empty cluster - no spaces and no panels - which
    /// rules out the closure gate, since that removes spaces only. This pass walks the reconstruction stages
    /// that run before any space is created, so the stage that empties the model can be named.
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
            double silverSpacing = Core.Tolerance.MacroDistance;
            double tolerance_Distance = Core.Tolerance.Distance;
            double tolerance_Angle = Core.Tolerance.Angle;
            double maxDistance = 0.1;
            double maxAngle = 0.0872664626;
            double minArea = 0.1;
            double thinnessRatio = 0.001;

            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine();
            stringBuilder.AppendLine("===== SAM#65 CI diagnostic, pass 2 =====");
            stringBuilder.AppendLine("ProcessorCount = " + Environment.ProcessorCount);
            stringBuilder.AppendLine("Framework      = " + System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription);

            // Stage 0 - the shells the ShellsOnly overload actually reconstructs from.
            List<Shell> shells = new List<Shell> { BoxShell() };
            List<Shell> shells_Split = shells.Split(silverSpacing, tolerance_Angle, tolerance_Distance);
            stringBuilder.AppendLine();
            stringBuilder.AppendLine("Stage 0  Split -> " + Count(shells_Split));
            DescribeShells(stringBuilder, shells_Split);

            if (shells_Split == null || shells_Split.Count == 0)
            {
                stringBuilder.AppendLine("Split emptied the input; nothing further can run.");
                Assert.True(false, stringBuilder.ToString());
            }

            // Stage 1 - the panels the overload derives from those shells.
            List<Panel> panels = new List<Panel>();
            foreach (Shell shell_Split in shells_Split)
            {
                List<Panel> panels_Shell = Analytical.Create.Panels(shell_Split, silverSpacing, tolerance_Distance);
                if (panels_Shell != null)
                {
                    panels.AddRange(panels_Shell);
                }
            }
            stringBuilder.AppendLine();
            stringBuilder.AppendLine("Stage 1  Create.Panels -> " + panels.Count);

            // Stage 2 - the panel filter, and the union bounding box every shell is then range-checked against.
            // A shell failing that check is dropped, which is one way the model can end up empty.
            List<BoundingBox3D> boundingBox3Ds = new List<BoundingBox3D>();
            int rejected = 0;
            foreach (Panel panel in panels)
            {
                Face3D face3D = panel?.GetFace3D();
                if (face3D == null)
                {
                    rejected++;
                    continue;
                }

                if (face3D.GetPlane() == null)
                {
                    rejected++;
                    continue;
                }

                double area = face3D.GetArea();
                double thinness = face3D.ThinnessRatio();
                stringBuilder.AppendLine("  panel " + panel.PanelType + " area = " + area.ToString("R") + ", thinnessRatio = " + thinness.ToString("R") + (area < minArea || thinness < thinnessRatio ? "  <-- REJECTED" : string.Empty));
                if (area < minArea || thinness < thinnessRatio)
                {
                    rejected++;
                    continue;
                }

                boundingBox3Ds.Add(face3D.GetBoundingBox(tolerance_Distance));
            }
            stringBuilder.AppendLine("Stage 2  accepted = " + boundingBox3Ds.Count + ", rejected = " + rejected);

            if (boundingBox3Ds.Count != 0)
            {
                BoundingBox3D boundingBox3D_All = new BoundingBox3D(boundingBox3Ds);
                stringBuilder.AppendLine("  union bbox min = " + boundingBox3D_All.Min + ", max = " + boundingBox3D_All.Max);
                foreach (Shell shell_Split in shells_Split)
                {
                    BoundingBox3D boundingBox3D = shell_Split?.GetBoundingBox();
                    stringBuilder.AppendLine("  shell bbox min = " + boundingBox3D?.Min + ", max = " + boundingBox3D?.Max + ", InRange = " + (boundingBox3D == null ? "n/a" : boundingBox3D_All.InRange(boundingBox3D).ToString()));
                }
            }

            // Stage 3 - per-shell cleanup, mirroring the overload's parallel loop body.
            List<Shell> shells_Temp = new List<Shell>();
            foreach (Shell shell_Split in shells_Split)
            {
                Shell shell_Valid = shell_Split.RemoveInvalidFace3Ds(silverSpacing);
                stringBuilder.AppendLine();
                stringBuilder.AppendLine("Stage 3  RemoveInvalidFace3Ds -> " + (shell_Valid == null ? "null" : shell_Valid.Face3Ds.Count + " faces"));
                if (shell_Valid == null)
                {
                    continue;
                }

                Shell shell_Merge = shell_Valid.Merge(tolerance_Distance);
                stringBuilder.AppendLine("Stage 3  Merge -> " + (shell_Merge == null ? "null (falls back to a copy)" : shell_Merge.Face3Ds.Count + " faces"));
                shells_Temp.Add(shell_Merge ?? new Shell(shell_Valid));
            }
            stringBuilder.AppendLine("Stage 3  shells_Temp = " + shells_Temp.Count);

            // Stage 4 - coplanar panel merge.
            List<Panel> panels_Merged = Analytical.Query.MergeCoplanarPanels(panels, maxDistance, true, true, minArea, tolerance_Distance);
            stringBuilder.AppendLine();
            stringBuilder.AppendLine("Stage 4  MergeCoplanarPanels -> " + Count(panels_Merged));
            if (panels_Merged != null)
            {
                foreach (Panel panel in panels_Merged)
                {
                    Face3D face3D = panel?.GetFace3D();
                    stringBuilder.AppendLine("  merged panel " + panel?.PanelType + ", area = " + (face3D == null ? "no face" : face3D.GetArea().ToString("R")));
                }
            }

            // Stage 5 - the three shell rewrites, each reported immediately after it runs.
            List<Face3D> face3Ds_Merged = panels_Merged == null ? new List<Face3D>() : panels_Merged.ConvertAll(x => x.GetFace3D());
            bool filled = shells_Temp.FillFace3Ds(face3Ds_Merged, 0.1, maxDistance, maxAngle, silverSpacing, tolerance_Distance);
            stringBuilder.AppendLine();
            stringBuilder.AppendLine("Stage 5  FillFace3Ds returned " + filled);
            DescribeShells(stringBuilder, shells_Temp);

            bool split = shells_Temp.SplitCoplanarFace3Ds(tolerance_Angle, tolerance_Distance);
            stringBuilder.AppendLine("Stage 6  SplitCoplanarFace3Ds returned " + split);
            DescribeShells(stringBuilder, shells_Temp);

            shells_Temp = shells_Temp.Snap(shells_Split, silverSpacing, tolerance_Distance);
            stringBuilder.AppendLine("Stage 7  Snap -> " + Count(shells_Temp));
            DescribeShells(stringBuilder, shells_Temp);

            // Stage 8 - the space-to-shell match the overload performs next.
            if (shells_Temp != null)
            {
                Point3D point3D = new Point3D(2, 1.5, 1);
                List<Shell> shells_Space = Analytical.Query.SpaceShells(shells_Temp, point3D, silverSpacing, tolerance_Distance);
                stringBuilder.AppendLine("Stage 8  SpaceShells(2, 1.5, 1) -> " + Count(shells_Space));

                foreach (Shell shell_Temp in shells_Temp)
                {
                    stringBuilder.AppendLine("Stage 8  CalculatedInternalPoint3D = " + shell_Temp?.CalculatedInternalPoint3D(silverSpacing, tolerance_Distance));
                }
            }

            stringBuilder.AppendLine("===== end diagnostic =====");

            Assert.True(false, stringBuilder.ToString());
        }

        private static string Count<T>(List<T> values)
        {
            return values == null ? "null" : values.Count.ToString();
        }

        private static void DescribeShells(StringBuilder stringBuilder, List<Shell> shells)
        {
            if (shells == null)
            {
                stringBuilder.AppendLine("  shells = null");
                return;
            }

            for (int i = 0; i < shells.Count; i++)
            {
                Shell shell = shells[i];
                if (shell == null)
                {
                    stringBuilder.AppendLine("  [" + i + "] null");
                    continue;
                }

                BoundingBox3D boundingBox3D = shell.GetBoundingBox();
                stringBuilder.AppendLine("  [" + i + "] faces = " + (shell.Face3Ds == null ? "null" : shell.Face3Ds.Count.ToString())
                    + ", bbox = " + (boundingBox3D == null ? "null" : boundingBox3D.Min + " .. " + boundingBox3D.Max)
                    + ", IsClosed(10x) = " + shell.IsClosed(Core.Tolerance.MacroDistance * 10)
                    + ", Volume = " + shell.Volume(Core.Tolerance.MacroDistance, Core.Tolerance.Distance).ToString("R"));
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
