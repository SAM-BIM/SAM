// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core;
using SAM.Geometry.Planar;
using SAM.Geometry.Spatial;
using System.Collections.Generic;
using System.Linq;

namespace SAM.Analytical
{
    public static partial class Create
    {
        /// <summary>
        /// Converts a thin closed building element (wall, slab, ceiling, roof, gable) into analytical mid-surface panels.
        /// Geometry must already be in metres; no unit conversion is performed.
        /// <list type="number">
        /// <item><b>Thickness pair:</b> the two opposite parallel faces whose projections overlap the most. Unrelated
        /// parallel faces (ends, returns) overlap little, so they are not mistaken for the element's sides.</item>
        /// <item><b>Mid-surface:</b> the true section of the solid on the mid-plane (SAM's <c>Shell.Section</c>), not a
        /// convex hull. Notches, disconnected regions and inner loops are kept, and each region becomes a panel.</item>
        /// <item><b>Openings:</b> holes and boundary notches are measured against <paramref name="apertureGeometries"/>.
        /// An opening an aperture occupies (a window, a door leaf or frame) is filled, so the aperture can be added
        /// to the panel later. An unclaimed hole (e.g. a stair void) stays open in the host panel and, when
        /// <paramref name="airForUnclaimedOpenings"/> is true, is filled by a separate <see cref="PanelType.Air"/>
        /// panel instead of a solid one. Unclaimed notches are kept as the element's real outline.</item>
        /// </list>
        /// Panels end where the element ends, half a neighbour's thickness short of its mid-plane. Join them with
        /// <see cref="Query.SnapToJunctions"/> across the whole set (or resolve them with SAMOCCT.Solve3D).
        /// Every panel is stamped with <see cref="PanelSourceParameter"/> values for traceability.
        /// </summary>
        /// <param name="shell">Closed solid of one building element, in metres.</param>
        /// <param name="diagnostics">What was done, skipped and why.</param>
        /// <param name="apertureGeometries">Window and door geometry used to recognise aperture openings (optional).</param>
        /// <param name="minThickness">Elements thinner than this [m] are skipped (e.g. balustrades).</param>
        /// <param name="maxThicknessRatio">Elements whose thickness exceeds this fraction of their smallest in-plane extent are not panel-like and are skipped (e.g. stair treads).</param>
        /// <param name="airForUnclaimedOpenings">Fill unclaimed holes with Air panels.</param>
        /// <param name="sourceName">Source element name stamped on the panels.</param>
        /// <param name="sourceId">Source element identifier stamped on the panels.</param>
        /// <param name="sourceLayer">Source element layer stamped on the panels.</param>
        /// <param name="tolerance_Angle">Angle tolerance [rad].</param>
        /// <param name="tolerance_Distance">Distance tolerance [m].</param>
        /// <param name="tolerance_Snap">Snap tolerance [m]; openings smaller than its square are ignored.</param>
        /// <returns>The host mid-surface panel(s) followed by any Air panels; empty when the element is not panel-like.</returns>
        public static List<Panel> PanelsBySolid(
            this Shell shell,
            out List<string> diagnostics,
            IEnumerable<ISAMGeometry3D> apertureGeometries = null,
            double minThickness = 0.08,
            double maxThicknessRatio = 0.2,
            bool airForUnclaimedOpenings = true,
            string sourceName = null,
            string sourceId = null,
            string sourceLayer = null,
            double tolerance_Angle = Tolerance.Angle,
            double tolerance_Distance = Tolerance.Distance,
            double tolerance_Snap = Tolerance.MacroDistance)
        {
            diagnostics = new List<string>();
            List<Panel> result = new List<Panel>();
            string label = string.IsNullOrWhiteSpace(sourceName) ? "element" : string.Format("'{0}'", sourceName);

            List<Face3D> face3Ds = shell?.Face3Ds?.Where(x => x != null && x.GetPlane() != null && !double.IsNaN(x.GetArea())).ToList();
            if (face3Ds == null || face3Ds.Count < 4)
            {
                diagnostics.Add(string.Format("{0}: skipped (not a closed solid).", label));
                return result;
            }

            if (!TryThicknessPair(face3Ds, minThickness, tolerance_Angle, tolerance_Distance, out Plane midPlane, out double thickness, out double minExtent))
            {
                diagnostics.Add(string.Format("{0}: skipped (no pair of opposite parallel faces at least {1:0.###} m apart).", label, minThickness));
                return result;
            }

            if (minExtent <= 0 || thickness / minExtent > maxThicknessRatio)
            {
                diagnostics.Add(string.Format("{0}: skipped (not panel-like: thickness {1:0.###} m over smallest extent {2:0.###} m exceeds {3:0.###}).", label, thickness, minExtent, maxThicknessRatio));
                return result;
            }

            List<double> otherThicknesses = OtherThicknesses(face3Ds, midPlane, thickness, minThickness, tolerance_Angle, tolerance_Distance, tolerance_Snap);
            if (otherThicknesses.Count != 0)
            {
                diagnostics.Add(string.Format("{0}: variable thickness - parts of it are also {1} m thick; the physical thickness stored is the main one, {2:0.###} m (the largest pair of opposite faces).", label, string.Join(", ", otherThicknesses.ConvertAll(x => x.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture))), thickness));
            }

            List<Face3D> sections = shell.Section(midPlane, true, tolerance_Angle, tolerance_Distance, tolerance_Snap);
            if (sections == null || sections.Count == 0)
            {
                diagnostics.Add(string.Format("{0}: skipped (no section on the mid-plane).", label));
                return result;
            }

            double minOpeningArea = System.Math.Max(tolerance_Snap * tolerance_Snap, tolerance_Distance);
            List<Face2D> apertureFootprints = ApertureFootprints(apertureGeometries, midPlane, thickness, tolerance_Snap);

            int filledHoles = 0, airHoles = 0, filledNotches = 0, keptNotches = 0;
            foreach (Face3D section in sections)
            {
                // The section also returns the voids themselves as faces; keep only regions of material.
                Point3D internalPoint = section?.GetInternalPoint3D(tolerance_Distance);
                if (internalPoint == null || !shell.Inside(internalPoint, tolerance_Snap, tolerance_Distance))
                {
                    continue;
                }

                Face2D face2D = midPlane.Convert(section);
                IClosed2D external = face2D?.ExternalEdge2D;
                if (external == null || face2D.GetArea() < minOpeningArea)
                {
                    continue;
                }

                // Holes: claimed ones are filled, unclaimed ones stay open (and may become Air panels).
                List<IClosed2D> keptHoles = new List<IClosed2D>();
                List<Face2D> airFaces = new List<Face2D>();
                foreach (IClosed2D hole in face2D.InternalEdge2Ds ?? new List<IClosed2D>())
                {
                    Face2D holeFace = Geometry.Planar.Create.Face2D(hole, null);
                    if (holeFace == null || holeFace.GetArea() < minOpeningArea)
                    {
                        continue;
                    }

                    if (IsClaimed(holeFace, apertureFootprints, minOpeningArea))
                    {
                        filledHoles++;
                        continue;
                    }

                    keptHoles.Add(hole);
                    if (airForUnclaimedOpenings)
                    {
                        airFaces.Add(holeFace);
                    }
                }

                Face2D host = Geometry.Planar.Create.Face2D(external, keptHoles);

                // Notches (pockets between the outline and its convex hull): fill only those an aperture claims,
                // e.g. a door opening reaching the floor. Anything else is the element's real shape.
                List<Face2D> claimedNotches = new List<Face2D>();
                if (external is ISegmentable2D segmentable2D)
                {
                    List<Point2D> hull = segmentable2D.ConvexHull();
                    Face2D outer = Geometry.Planar.Create.Face2D(external, null);
                    if (hull != null && hull.Count >= 3 && outer != null)
                    {
                        Face2D hullFace = Geometry.Planar.Create.Face2D(new Polygon2D(hull), null);
                        foreach (Face2D notch in hullFace?.Difference(outer, tolerance_Distance) ?? new List<Face2D>())
                        {
                            if (notch == null || notch.GetArea() < minOpeningArea)
                            {
                                continue;
                            }

                            if (IsClaimed(notch, apertureFootprints, minOpeningArea))
                            {
                                claimedNotches.Add(notch);
                                filledNotches++;
                            }
                            else
                            {
                                keptNotches++;
                            }
                        }
                    }
                }

                if (claimedNotches.Count != 0 && host != null)
                {
                    List<Face2D> union = new List<Face2D> { host };
                    union.AddRange(claimedNotches);
                    host = Geometry.Planar.Query.Union(union, tolerance_Distance)?.OrderByDescending(x => x.GetArea()).FirstOrDefault() ?? host;
                }

                Face3D hostFace3D = host == null ? null : midPlane.Convert(host);
                if (hostFace3D == null || !hostFace3D.IsValid())
                {
                    diagnostics.Add(string.Format("{0}: a section region could not be turned into a panel.", label));
                    continue;
                }

                PanelType panelType = Query.PanelType(hostFace3D.GetPlane()?.Normal, tolerance_Angle);
                if (panelType == PanelType.Undefined)
                {
                    panelType = PanelType.Wall;
                }

                Panel panel = Panel(Query.DefaultConstruction(panelType), panelType, hostFace3D);
                if (panel != null)
                {
                    StampSource(panel, sourceName, sourceId, sourceLayer, thickness, null);
                    result.Add(panel);
                }

                foreach (Face2D airFace in airFaces)
                {
                    Face3D airFace3D = midPlane.Convert(airFace);
                    Panel airPanel = airFace3D == null ? null : Panel(null, PanelType.Air, airFace3D);
                    if (airPanel != null)
                    {
                        StampSource(airPanel, sourceName, sourceId, sourceLayer, double.NaN, "Opening");
                        result.Add(airPanel);
                        airHoles++;
                    }
                }
            }

            diagnostics.Add(string.Format(
                "{0}: {1} panel(s) at thickness {2:0.###} m; holes: {3} filled for apertures, {4} left open as Air panel(s); notches: {5} filled for apertures, {6} kept as outline.",
                label, result.Count(x => x.PanelType != PanelType.Air), thickness, filledHoles, airHoles, filledNotches, keptNotches));

            return result;
        }

        private static void StampSource(Panel panel, string sourceName, string sourceId, string sourceLayer, double thickness, string role)
        {
            if (!string.IsNullOrWhiteSpace(sourceName))
            {
                panel.SetValue(PanelSourceParameter.SourceName, sourceName);
            }

            if (!string.IsNullOrWhiteSpace(sourceId))
            {
                panel.SetValue(PanelSourceParameter.SourceId, sourceId);
            }

            if (!string.IsNullOrWhiteSpace(sourceLayer))
            {
                panel.SetValue(PanelSourceParameter.SourceLayer, sourceLayer);
            }

            if (!double.IsNaN(thickness))
            {
                panel.SetValue(PanelSourceParameter.PhysicalThickness, thickness);
            }

            if (!string.IsNullOrWhiteSpace(role))
            {
                panel.SetValue(PanelSourceParameter.SourceRole, role);
            }
        }

        /// <summary>
        /// The pair of opposite parallel faces whose projections overlap the most (ties: the thinner pair), and the
        /// mid-plane between them.
        /// </summary>
        private static bool TryThicknessPair(List<Face3D> face3Ds, double minThickness, double tolerance_Angle, double tolerance_Distance, out Plane midPlane, out double thickness, out double minExtent)
        {
            midPlane = null;
            thickness = 0;
            minExtent = 0;
            double cosAngle = System.Math.Cos(tolerance_Angle);
            double bestOverlap = 0;

            for (int i = 0; i < face3Ds.Count; i++)
            {
                Plane plane_i = face3Ds[i].GetPlane();
                for (int j = i + 1; j < face3Ds.Count; j++)
                {
                    Plane plane_j = face3Ds[j].GetPlane();
                    if (System.Math.Abs(plane_i.Normal.DotProduct(plane_j.Normal)) < cosAngle)
                    {
                        continue;
                    }

                    Vector3D normal = plane_i.Normal;
                    Point3D origin_i = plane_i.Origin, origin_j = plane_j.Origin;
                    double offset = normal.DotProduct(new Vector3D(origin_i, origin_j));
                    double distance = System.Math.Abs(offset);
                    if (distance < minThickness - tolerance_Distance)
                    {
                        continue;
                    }

                    Face2D face2D_i = plane_i.Convert(face3Ds[i]);
                    Face2D face2D_j = plane_i.Convert(face3Ds[j]);
                    List<Face2D> overlap = face2D_i == null || face2D_j == null ? null : face2D_i.Intersection(face2D_j, tolerance_Distance);
                    double overlapArea = overlap?.Sum(x => x.GetArea()) ?? 0;
                    if (overlapArea <= tolerance_Distance)
                    {
                        continue;
                    }

                    bool better = overlapArea > bestOverlap * (1 + 1e-9) || (System.Math.Abs(overlapArea - bestOverlap) <= bestOverlap * 1e-9 && distance < thickness);
                    if (!better)
                    {
                        continue;
                    }

                    bestOverlap = overlapArea;
                    thickness = distance;
                    Point3D midOrigin = new Point3D(origin_i.X + normal.X * offset / 2, origin_i.Y + normal.Y * offset / 2, origin_i.Z + normal.Z * offset / 2);
                    midPlane = new Plane(plane_i, midOrigin);

                    List<Point2D> point2Ds = overlap.SelectMany(x => (x.ExternalEdge2D as ISegmentable2D)?.GetPoints() ?? new List<Point2D>()).ToList();
                    BoundingBox2D boundingBox2D = point2Ds.Count == 0 ? null : new BoundingBox2D(point2Ds);
                    minExtent = boundingBox2D == null ? 0 : System.Math.Min(boundingBox2D.Width, boundingBox2D.Height);
                }
            }

            return midPlane != null;
        }

        /// <summary>
        /// Thicknesses other than <paramref name="thickness"/> at which further pairs of opposite faces parallel to the
        /// mid-plane overlap substantially (at least 5% of the largest such overlap): a stepped or partly thickened
        /// element. Distinct values, rounded to <paramref name="tolerance_Snap"/>; empty for a uniform element.
        /// </summary>
        private static List<double> OtherThicknesses(List<Face3D> face3Ds, Plane midPlane, double thickness, double minThickness, double tolerance_Angle, double tolerance_Distance, double tolerance_Snap)
        {
            double cosAngle = System.Math.Cos(tolerance_Angle);
            Vector3D normal = midPlane.Normal;
            List<Face3D> parallel = face3Ds.FindAll(x => System.Math.Abs(x.GetPlane().Normal.DotProduct(normal)) >= cosAngle);

            List<System.Tuple<double, double>> pairs = new List<System.Tuple<double, double>>();
            for (int i = 0; i < parallel.Count; i++)
            {
                Plane plane_i = parallel[i].GetPlane();
                Face2D face2D_i = midPlane.Convert(parallel[i]);
                for (int j = i + 1; j < parallel.Count; j++)
                {
                    double distance = System.Math.Abs(normal.DotProduct(new Vector3D(plane_i.Origin, parallel[j].GetPlane().Origin)));
                    if (distance < minThickness - tolerance_Distance)
                    {
                        continue;
                    }

                    Face2D face2D_j = midPlane.Convert(parallel[j]);
                    double overlapArea = face2D_i == null || face2D_j == null ? 0 : face2D_i.Intersection(face2D_j, tolerance_Distance)?.Sum(x => x.GetArea()) ?? 0;
                    if (overlapArea > tolerance_Distance)
                    {
                        pairs.Add(new System.Tuple<double, double>(distance, overlapArea));
                    }
                }
            }

            double maxOverlap = pairs.Count == 0 ? 0 : pairs.Max(x => x.Item2);
            return pairs
                .Where(x => x.Item2 >= 0.05 * maxOverlap && System.Math.Abs(x.Item1 - thickness) > tolerance_Snap)
                .Select(x => System.Math.Round(x.Item1 / tolerance_Snap) * tolerance_Snap)
                .Distinct()
                .OrderBy(x => x)
                .ToList();
        }

        /// <summary>
        /// The in-plane footprint (bounding rectangle) of each aperture geometry that lies within the element's
        /// thickness band around the mid-plane.
        /// </summary>
        private static List<Face2D> ApertureFootprints(IEnumerable<ISAMGeometry3D> apertureGeometries, Plane midPlane, double thickness, double tolerance_Snap)
        {
            List<Face2D> result = new List<Face2D>();
            if (apertureGeometries == null)
            {
                return result;
            }

            double halfBand = thickness / 2 + tolerance_Snap;
            foreach (ISAMGeometry3D geometry3D in apertureGeometries)
            {
                BoundingBox3D boundingBox3D = (geometry3D as IBoundable3D)?.GetBoundingBox();
                if (boundingBox3D == null)
                {
                    continue;
                }

                Point3D min = boundingBox3D.Min, max = boundingBox3D.Max;
                List<Point3D> corners = new List<Point3D>
                {
                    new Point3D(min.X, min.Y, min.Z), new Point3D(max.X, min.Y, min.Z), new Point3D(min.X, max.Y, min.Z), new Point3D(max.X, max.Y, min.Z),
                    new Point3D(min.X, min.Y, max.Z), new Point3D(max.X, min.Y, max.Z), new Point3D(min.X, max.Y, max.Z), new Point3D(max.X, max.Y, max.Z),
                };

                List<double> offsets = corners.ConvertAll(x => midPlane.Normal.DotProduct(new Vector3D(midPlane.Origin, x)));
                if (offsets.Max() < -halfBand || offsets.Min() > halfBand)
                {
                    continue;
                }

                BoundingBox2D boundingBox2D = new BoundingBox2D(corners.ConvertAll(x => midPlane.Convert(x)));
                if (boundingBox2D.Width <= tolerance_Snap || boundingBox2D.Height <= tolerance_Snap)
                {
                    continue;
                }

                Face2D footprint = Geometry.Planar.Create.Face2D(new Polygon2D(new List<Point2D> { boundingBox2D.Min, new Point2D(boundingBox2D.Max.X, boundingBox2D.Min.Y), boundingBox2D.Max, new Point2D(boundingBox2D.Min.X, boundingBox2D.Max.Y) }), null);
                if (footprint != null)
                {
                    result.Add(footprint);
                }
            }

            return result;
        }

        private static bool IsClaimed(Face2D opening, List<Face2D> apertureFootprints, double minOpeningArea)
        {
            foreach (Face2D footprint in apertureFootprints)
            {
                List<Face2D> intersection = opening.Intersection(footprint);
                if (intersection != null && intersection.Sum(x => x.GetArea()) >= minOpeningArea)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
