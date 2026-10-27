// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core;
using SAM.Geometry.Planar;
using SAM.Geometry.Spatial;
using System.Collections.Generic;
using System.Linq;

namespace SAM.Analytical
{
    public static partial class Query
    {
        /// <summary>
        /// Moves the outer edges of mid-surface panels onto the mid-planes of the panels they meet, so walls,
        /// slabs and roofs that end at a neighbour's face (half a thickness short of its mid-plane) meet on shared
        /// mid-planes. Typical input is the output of <see cref="Create.PanelsBySolid"/>.
        /// <para>
        /// Each outer edge is moved within its own plane, along its outward in-plane normal. A target is another
        /// panel's plane that runs parallel to the edge, lies within <paramref name="snapDistance"/> of it, and
        /// has a point of the moved edge (its middle, ends or quarter points) within <paramref name="snapDistance"/> of
        /// the target's actual face. That last check confirms a real local junction, not just an infinite plane, also
        /// where the target's face covers only part of the edge (a slab with a void reaching the facade).
        /// </para>
        /// <para>
        /// The farthest target ahead (an extension) wins. A wall that passes a ceiling at a T-junction carries on
        /// to the roof above it, which is what closes an attic. When nothing lies ahead, the nearest target behind
        /// wins (a trim back to the neighbour's mid-plane, e.g. a wall running past a corner).
        /// </para>
        /// <para>
        /// A panel whose edges snap together (it lies within the thickness of the elements it meets) is dropped, and a
        /// dropped panel is not a junction for any other panel. The dropped set is found by repeated passes: every panel
        /// is snapped against the panels not dropped so far, and the dropped set becomes the panels that close up. This
        /// stops when the set no longer changes, so a panel that closed up only on a dropped panel is kept. If the set does
        /// not settle (a set repeats, or the pass limit is reached), snapping is not applied and the panels are returned
        /// unsnapped with an Error. The result does not depend on the input order.
        /// </para>
        /// <para>
        /// Holes are left untouched. Air panels are neither moved nor used as targets. Final junction resolution
        /// remains the 3D solver's job (SAMOCCT.Solve3D).
        /// </para>
        /// </summary>
        /// <param name="panels">Mid-surface panels, in metres.</param>
        /// <param name="diagnostics">How many edges were extended or trimmed.</param>
        /// <param name="snapDistance">Maximum edge move [m]; zero returns copies of the input.</param>
        /// <param name="tolerance_Distance">Distance tolerance [m].</param>
        /// <returns>New panels (same Guids, constructions and parameters) with snapped outer edges, without the collapsed ones.</returns>
        public static List<Panel> SnapToJunctions(this IEnumerable<Panel> panels, out List<string> diagnostics, double snapDistance = 0.3, double tolerance_Distance = Tolerance.Distance)
        {
            Core.Diagnostics diagnostics_Structured = new Core.Diagnostics(DiagnosticDetail.Detailed);
            List<Panel> result = SnapToJunctions(panels, diagnostics_Structured, snapDistance, tolerance_Distance);
            diagnostics = diagnostics_Structured.Format();
            return result;
        }

        /// <summary>
        /// <see cref="SnapToJunctions(IEnumerable{Panel}, out List{string}, double, double)"/> with structured diagnostics:
        /// one Summary Info record (<see cref="BySolidsDiagnosticCode.SnapChecked"/>, Value = snap distance [m]; its edge
        /// counts are those of the final pass, zero when snapping is not applied), and a Warning
        /// (<see cref="BySolidsDiagnosticCode.SnapCollapsed"/>) for each panel snapping trimmed to nothing, which is dropped.
        /// If the dropped set does not settle (a set repeats, or the pass limit is reached), no pass is a valid result:
        /// snapping is not applied, the panels are returned unsnapped (as with a zero snap distance) and an Error
        /// (<see cref="BySolidsDiagnosticCode.SnapNotSettled"/>) names the unsettled panels.
        /// The panels do not depend on the detail level.
        /// </summary>
        /// <param name="panels">Mid-surface panels, in metres.</param>
        /// <param name="diagnostics">Collection the record is appended to. Null collects nothing.</param>
        /// <param name="snapDistance">Maximum edge move [m]; zero returns copies of the input.</param>
        /// <param name="tolerance_Distance">Distance tolerance [m].</param>
        /// <returns>New panels (same Guids, constructions and parameters) with snapped outer edges.</returns>
        public static List<Panel> SnapToJunctions(this IEnumerable<Panel> panels, Core.Diagnostics diagnostics, double snapDistance = 0.3, double tolerance_Distance = Tolerance.Distance)
        {
            return SnapToJunctions(panels, diagnostics, snapDistance, tolerance_Distance, SnapPasses);
        }

        /// <summary>
        /// <see cref="SnapToJunctions(IEnumerable{Panel}, Core.Diagnostics, double, double)"/> with at most
        /// <paramref name="passes"/> passes to settle the dropped set (a repeated set ends it earlier).
        /// </summary>
        internal static List<Panel> SnapToJunctions(IEnumerable<Panel> panels, Core.Diagnostics diagnostics, double snapDistance, double tolerance_Distance, int passes)
        {
            diagnostics = diagnostics ?? new Core.Diagnostics();
            List<Panel> panels_Temp = panels?.Where(x => x != null && x.GetFace3D() != null).ToList() ?? new List<Panel>();
            if (snapDistance <= 0)
            {
                return panels_Temp.ConvertAll(x => Create.Panel(x));
            }

            List<SnapTarget> targets = new List<SnapTarget>();
            foreach (Panel panel in panels_Temp)
            {
                if (panel.PanelType == Analytical.PanelType.Air)
                {
                    continue;
                }

                Face3D face3D = panel.GetFace3D();
                Plane plane = face3D.GetPlane();
                Face2D face2D = plane?.Convert(face3D);
                if (face2D != null)
                {
                    targets.Add(new SnapTarget(panel.Guid, plane, face2D));
                }
            }

            // Each panel's own target and counter-clockwise outer edge; a panel without them is copied unchanged.
            int count = panels_Temp.Count;
            SnapTarget[] selves = new SnapTarget[count];
            Polygon2D[] externals = new Polygon2D[count];
            for (int i = 0; i < count; i++)
            {
                SnapTarget self = targets.Find(x => x.Guid == panels_Temp[i].Guid);
                Polygon2D external = (self?.Face2D.ExternalEdge2D as ISegmentable2D)?.GetPoints() is List<Point2D> point2Ds && point2Ds.Count >= 3 ? new Polygon2D(point2Ds) : null;
                if (self == null || external == null)
                {
                    continue;
                }

                if (external.GetOrientation() == Geometry.Orientation.Clockwise)
                {
                    external.Reverse();
                }

                selves[i] = self;
                externals[i] = external;
            }

            // A dropped panel is not a junction for any other panel. Snap every panel against the targets outside the
            // dropped set, make the panels that close up the new dropped set, and repeat until it no longer changes. The
            // set is re-evaluated, not only grown: a panel that closed up only on a dropped target is kept again.
            HashSet<System.Guid> dropped = new HashSet<System.Guid>();
            List<HashSet<System.Guid>> states = new List<HashSet<System.Guid>> { dropped };
            HashSet<System.Guid> unsettled = null;
            int repeat = -1;
            Polygon2D[] snapped = new Polygon2D[count];
            int extended = 0, trimmed = 0, pass = 0;
            while (true)
            {
                pass++;
                List<SnapTarget> targets_Pass = targets.FindAll(x => !dropped.Contains(x.Guid));
                HashSet<System.Guid> collapsed = new HashSet<System.Guid>();
                extended = 0;
                trimmed = 0;
                for (int i = 0; i < count; i++)
                {
                    snapped[i] = selves[i] == null ? null : Snap(externals[i], selves[i], targets_Pass, snapDistance, tolerance_Distance, ref extended, ref trimmed);
                    if (selves[i] != null && snapped[i] == null)
                    {
                        collapsed.Add(panels_Temp[i].Guid);
                    }
                }

                if (collapsed.SetEquals(dropped))
                {
                    break;
                }

                // A dropped set seen before is a cycle: no set is consistent (a panel is dropped only while another is a
                // junction, and that one only while the first is not). Unsettled are the panels dropped in some of its
                // states but not all. Without a repeat, the pass limit leaves those changing between the last two passes.
                repeat = states.FindIndex(x => x.SetEquals(collapsed));
                if (repeat != -1 || pass >= passes)
                {
                    List<HashSet<System.Guid>> cycle = repeat != -1 ? states.GetRange(repeat, states.Count - repeat) : new List<HashSet<System.Guid>> { dropped, collapsed };
                    unsettled = new HashSet<System.Guid>(cycle.SelectMany(x => x));
                    unsettled.RemoveWhere(x => cycle.TrueForAll(y => y.Contains(x)));
                    break;
                }

                states.Add(collapsed);
                dropped = collapsed;
            }

            if (unsettled != null)
            {
                extended = 0;
                trimmed = 0;
            }

            diagnostics.Add(DiagnosticSeverity.Info, DiagnosticDetail.Summary, null, () => string.Format(System.Globalization.CultureInfo.InvariantCulture, "Checked {0} for junctions: extended {1} and trimmed {2} (snap distance {3:0.###} m).", Core.Query.Plural(panels_Temp.Count, "panel"), Core.Query.Plural(extended, "edge"), Core.Query.Plural(trimmed, "edge"), snapDistance), BySolidsDiagnosticCode.SnapChecked, null, snapDistance, null, "m");

            // Not settled: no pass is a valid result, as each still lets a dropped panel move its neighbours. Snapping is
            // not applied (the panels come back unsnapped, as with a zero snap distance), and the run reports an Error.
            if (unsettled != null)
            {
                List<string> names = panels_Temp.FindAll(x => unsettled.Contains(x.Guid)).ConvertAll(SnapName).Distinct().OrderBy(x => x, System.StringComparer.Ordinal).ToList();
                string list = string.Join(", ", names.Take(10).Select(x => string.Format("'{0}'", x))) + (names.Count > 10 ? string.Format(" and {0} more", names.Count - 10) : string.Empty);
                string reason = repeat != -1
                    ? string.Format(System.Globalization.CultureInfo.InvariantCulture, "the set of panels it drops repeats after {0} without settling: {1} are dropped or kept depending on which of them are junctions", Core.Query.Plural(pass, "pass", "passes"), list)
                    : string.Format(System.Globalization.CultureInfo.InvariantCulture, "the set of panels it drops did not settle within {0}: {1} still changed in the last pass", Core.Query.Plural(passes, "pass", "passes"), list);
                diagnostics.Add(new Diagnostic(DiagnosticSeverity.Error, DiagnosticDetail.Summary, null, string.Format(System.Globalization.CultureInfo.InvariantCulture, "Snapping to junctions was not applied: {0}. The panels are returned unsnapped (as with snap distance 0).", reason), BySolidsDiagnosticCode.SnapNotSettled, null, unsettled.Count, passes, null));
                return panels_Temp.ConvertAll(x => Create.Panel(x));
            }

            List<Panel> result = new List<Panel>();
            List<Panel> collapsed_Panels = new List<Panel>();
            for (int i = 0; i < count; i++)
            {
                Panel panel = panels_Temp[i];
                if (selves[i] == null)
                {
                    result.Add(Create.Panel(panel));
                    continue;
                }

                if (snapped[i] == null)
                {
                    collapsed_Panels.Add(panel);
                    continue;
                }

                Face2D face2D = Geometry.Planar.Create.Face2D(snapped[i], selves[i].Face2D.InternalEdge2Ds);
                Face3D face3D = face2D == null ? null : selves[i].Plane.Convert(face2D);
                Panel snappedPanel = face3D == null || !face3D.IsValid() ? null : Create.Panel(panel.Guid, panel, face3D, null, false);
                result.Add(snappedPanel ?? Create.Panel(panel));
            }

            foreach (Panel panel in collapsed_Panels)
            {
                diagnostics.Add(new Diagnostic(DiagnosticSeverity.Warning, DiagnosticDetail.Summary, string.Format("Panel '{0}'", SnapName(panel)), string.Format(System.Globalization.CultureInfo.InvariantCulture, "Dropped. Snapping its edges to the junctions closed it up (no wider than the distance tolerance, or turned inside out): it lies within the thickness of the elements it meets. Area before snapping: {0:G4} m2.", panel.GetArea()), BySolidsDiagnosticCode.SnapCollapsed, null, panel.GetArea(), null, "m2"));
            }

            return result;
        }

        /// <summary>
        /// A panel's name in snapping diagnostics: its source name, else its Guid.
        /// </summary>
        private static string SnapName(Panel panel)
        {
            return panel.TryGetValue(PanelSourceParameter.SourceName, out string sourceName) && !string.IsNullOrWhiteSpace(sourceName) ? sourceName : panel.Guid.ToString();
        }

        /// <summary>
        /// One panel's outer edge (counter-clockwise in its own plane) with every edge moved onto its junction among
        /// <paramref name="targets"/>, or null when the edges close up and nothing of the panel is left. Counts the
        /// extended and trimmed edges.
        /// </summary>
        private static Polygon2D Snap(Polygon2D external, SnapTarget self, IEnumerable<SnapTarget> targets, double snapDistance, double tolerance_Distance, ref int extended, ref int trimmed)
        {
            List<Point2D> points = external.GetPoints();
            int count = points.Count;
            Point2D[] linePoints = new Point2D[count];
            Vector2D[] lineDirections = new Vector2D[count];
            for (int k = 0; k < count; k++)
            {
                Point2D a = points[k], b = points[(k + 1) % count];
                Vector2D direction = new Vector2D(a, b).Unit;
                Vector2D outward = new Vector2D(direction.Y, -direction.X);
                Point2D middle = new Point2D((a.X + b.X) / 2, (a.Y + b.Y) / 2);

                Point3D middle3D = self.Plane.Convert(middle);
                Vector3D direction3D = new Vector3D(self.Plane.Convert(a), self.Plane.Convert(b)).Unit;
                Vector3D outward3D = new Vector3D(middle3D, self.Plane.Convert(middle.GetMoved(outward))).Unit;

                double ahead = 0, behind = double.NegativeInfinity;
                foreach (SnapTarget target in targets)
                {
                    if (target.Guid == self.Guid)
                    {
                        continue;
                    }

                    Vector3D normal = target.Plane.Normal;
                    if (System.Math.Abs(direction3D.DotProduct(normal)) > 1e-3)
                    {
                        continue; // the edge must run parallel to the target plane
                    }

                    double denominator = outward3D.DotProduct(normal);
                    if (System.Math.Abs(denominator) < 0.05)
                    {
                        continue;
                    }

                    double t = new Vector3D(middle3D, target.Plane.Origin).DotProduct(normal) / denominator;
                    if (System.Math.Abs(t) <= tolerance_Distance || System.Math.Abs(t) > snapDistance)
                    {
                        continue;
                    }

                    // A real junction: the target's face is near the moved edge somewhere along it, not only at its
                    // middle (a slab with a void at the facade still meets the wall at both ends of its edge).
                    bool junction = false;
                    foreach (double factor in EdgeSamples)
                    {
                        Point3D sample = self.Plane.Convert(new Point2D(a.X + (b.X - a.X) * factor, a.Y + (b.Y - a.Y) * factor));
                        Point3D moved = new Point3D(sample.X + outward3D.X * t, sample.Y + outward3D.Y * t, sample.Z + outward3D.Z * t);
                        if (target.Face2D.Distance(target.Plane.Convert(moved), tolerance_Distance) <= snapDistance)
                        {
                            junction = true;
                            break;
                        }
                    }

                    if (!junction)
                    {
                        continue; // no real junction there: the target's face is not near this edge
                    }

                    if (t > 0)
                    {
                        ahead = System.Math.Max(ahead, t);
                    }
                    else
                    {
                        behind = System.Math.Max(behind, t);
                    }
                }

                double best = ahead > 0 ? ahead : (double.IsNegativeInfinity(behind) ? 0 : behind);
                if (best > 0)
                {
                    extended++;
                }
                else if (best < 0)
                {
                    trimmed++;
                }

                linePoints[k] = middle.GetMoved(outward * best);
                lineDirections[k] = direction;
            }

            List<Point2D> snapped = new List<Point2D>();
            for (int k = 0; k < count; k++)
            {
                int j = (k - 1 + count) % count;
                double cross = lineDirections[j].X * lineDirections[k].Y - lineDirections[j].Y * lineDirections[k].X;
                if (System.Math.Abs(cross) < 1e-9)
                {
                    // Collinear neighbours: keep the vertex, moved onto line k.
                    Vector2D offset = new Vector2D(points[k], linePoints[k]);
                    double along = offset.X * lineDirections[k].X + offset.Y * lineDirections[k].Y;
                    snapped.Add(linePoints[k].GetMoved(lineDirections[k] * -along));
                    continue;
                }

                double s = ((linePoints[k].X - linePoints[j].X) * lineDirections[k].Y - (linePoints[k].Y - linePoints[j].Y) * lineDirections[k].X) / cross;
                snapped.Add(linePoints[j].GetMoved(lineDirections[j] * s));
            }

            // Opposite edges trimmed onto one junction (or past each other): the panel lies within its neighbours'
            // thickness and nothing of it is left. Dropped, never returned as a degenerate face. Collapsed means its mean
            // width (twice the area over the perimeter, a length) is within the distance tolerance, or the outline is
            // turned inside out; a small but real face is many tolerances wide and is kept.
            Polygon2D polygon2D = new Polygon2D(snapped);
            double perimeter = polygon2D.Perimeter();
            if (!(perimeter > 0) || 2 * polygon2D.GetArea() / perimeter <= tolerance_Distance || polygon2D.GetOrientation() != Geometry.Orientation.CounterClockwise)
            {
                return null;
            }

            return polygon2D;
        }

        /// <summary>
        /// Where along an outer edge (start 0, end 1) a junction with a target face is looked for.
        /// </summary>
        private static readonly double[] EdgeSamples = { 0.5, 0, 1, 0.25, 0.75 };

        /// <summary>
        /// Most passes <see cref="SnapToJunctions(IEnumerable{Panel}, Core.Diagnostics, double, double)"/> takes to settle
        /// the dropped set (a building model typically needs two or three).
        /// </summary>
        private const int SnapPasses = 10;

        private sealed class SnapTarget
        {
            public SnapTarget(System.Guid guid, Plane plane, Face2D face2D)
            {
                Guid = guid;
                Plane = plane;
                Face2D = face2D;
            }

            public System.Guid Guid { get; }

            public Plane Plane { get; }

            public Face2D Face2D { get; }
        }
    }
}
