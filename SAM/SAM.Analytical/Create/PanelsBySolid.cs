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
        /// Converts a closed solid of thin building elements (wall, slab, ceiling, roof, gable) into analytical mid-surface
        /// panels. One solid may hold several planar elements, e.g. a ring of external walls or a roof with two pitches;
        /// each element plane gives its own panels. Geometry must already be in metres; no unit conversion is performed.
        /// <list type="number">
        /// <item><b>Element planes:</b> pairs of parallel faces with material between them; pairs sharing a mid-plane
        /// are one element, whose thickness is that of the pair whose projections overlap the most. Unrelated parallel
        /// faces (ends, returns, the far side of a ring of walls, a window sill over a wall's base) are not mistaken for
        /// an element's sides. See <see cref="ElementPlanes"/>.</item>
        /// <item><b>Mid-surface:</b> the true section of the solid on each mid-plane (SAM's <c>Shell.Section</c>), not a
        /// convex hull. Notches, disconnected regions and inner loops are kept, and each region becomes a panel. Regions
        /// where the mid-plane only cuts across another element of the same solid are left to that element's plane.</item>
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

            List<ElementPlane> elementPlanes = ElementPlanes(shell, face3Ds, minThickness, maxThicknessRatio, tolerance_Angle, tolerance_Distance, tolerance_Snap, out List<ElementPlane> rejected);
            if (elementPlanes.Count == 0)
            {
                ElementPlane best = rejected.FirstOrDefault();
                diagnostics.Add(best == null
                    ? string.Format("{0}: skipped (no pair of opposite parallel faces at least {1:0.###} m apart with material between them).", label, minThickness)
                    : string.Format("{0}: skipped (not panel-like: thickness {1:0.###} m over smallest extent {2:0.###} m = {3:0.###} exceeds {4:0.###}; a maxThicknessRatio above {3:0.###} would keep it).", label, best.Thickness, best.MinExtent, best.MinExtent <= 0 ? double.PositiveInfinity : best.Thickness / best.MinExtent, maxThicknessRatio));
                return result;
            }

            if (rejected.Count != 0)
            {
                diagnostics.Add(string.Format("{0}: {1} other face pair plane(s) not panel-like, ignored (thickness/smallest extent: {2}).", label, rejected.Count, string.Join(", ", rejected.Take(5).Select(x => string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0:0.###}/{1:0.###} m", x.Thickness, x.MinExtent)))));
            }

            double minOpeningArea = System.Math.Max(tolerance_Snap * tolerance_Snap, tolerance_Distance);
            for (int k = 0; k < elementPlanes.Count; k++)
            {
                ElementPlane elementPlane = elementPlanes[k];
                string label_Plane = elementPlanes.Count == 1 ? label : string.Format("{0} [plane {1}/{2}, normal ({3:0.###}, {4:0.###}, {5:0.###})]", label, k + 1, elementPlanes.Count, elementPlane.MidPlane.Normal.X, elementPlane.MidPlane.Normal.Y, elementPlane.MidPlane.Normal.Z);
                result.AddRange(PanelsByElementPlane(shell, elementPlane, diagnostics, label_Plane, apertureGeometries, airForUnclaimedOpenings, sourceName, sourceId, sourceLayer, minOpeningArea, tolerance_Angle, tolerance_Distance, tolerance_Snap));
            }

            return result;
        }

        /// <summary>
        /// Panels of one element plane of <paramref name="shell"/>: the material regions of the section on the mid-plane
        /// that the plane's own thickness pairs support, with aperture-claimed openings filled and unclaimed holes
        /// left open (optionally as Air panels).
        /// </summary>
        private static List<Panel> PanelsByElementPlane(Shell shell, ElementPlane elementPlane, List<string> diagnostics, string label, IEnumerable<ISAMGeometry3D> apertureGeometries, bool airForUnclaimedOpenings, string sourceName, string sourceId, string sourceLayer, double minOpeningArea, double tolerance_Angle, double tolerance_Distance, double tolerance_Snap)
        {
            List<Panel> result = new List<Panel>();
            Plane midPlane = elementPlane.MidPlane;
            double thickness = elementPlane.Thickness;

            if (elementPlane.OtherThicknesses.Count != 0)
            {
                diagnostics.Add(string.Format("{0}: variable thickness - parts of it are also {1} m thick; the physical thickness stored is the main one, {2:0.###} m (the largest pair of opposite faces).", label, string.Join(", ", elementPlane.OtherThicknesses.ConvertAll(x => x.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture))), thickness));
            }

            List<Face3D> sections = shell.Section(midPlane, true, tolerance_Angle, tolerance_Distance, tolerance_Snap);
            if (sections == null || sections.Count == 0)
            {
                diagnostics.Add(string.Format("{0}: skipped (no section on the mid-plane).", label));
                return result;
            }

            List<Face2D> apertureFootprints = ApertureFootprints(apertureGeometries, midPlane, thickness, tolerance_Snap);
            List<Face2D> pairFootprints = elementPlane.Footprints.ConvertAll(x => midPlane.Convert(x)).FindAll(x => x != null);

            int filledHoles = 0, airHoles = 0, filledNotches = 0, keptNotches = 0, crossSections = 0;
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

                // In a solid holding several elements (a ring of walls, a roof with two pitches) the mid-plane also cuts
                // across the other elements. Such a region is not backed by this plane's opposite faces: it belongs to
                // another element plane.
                if (!IsClaimed(face2D, pairFootprints, minOpeningArea))
                {
                    crossSections++;
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
                label, result.Count(x => x.PanelType != PanelType.Air), thickness, filledHoles, airHoles, filledNotches, keptNotches)
                + (crossSections == 0 ? string.Empty : string.Format(" {0} cross-section(s) of the solid's other elements left to their own planes.", crossSections)));

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
        /// The element planes of a solid: one mid-plane per planar building element it contains.
        /// <list type="number">
        /// <item><b>Pairs:</b> two parallel faces at least <paramref name="minThickness"/> apart whose projections
        /// overlap, with material between them. The material test rejects pairs across a void, such as the outer face
        /// of one wall and the inner face of the wall opposite it in a ring of walls.</item>
        /// <item><b>Planes:</b> pairs sharing a mid-plane form one element plane (the coplanar walls of a ring, or a
        /// face split in several pieces).</item>
        /// <item><b>Ownership:</b> material belongs to the element in which it is most panel-like (lowest thickness over
        /// smallest extent). Planes are taken in that order, and a pair whose material lies inside an accepted, non-parallel element (e.g. the wall
        /// under a window sill, cut horizontally by the sill and the wall's base) is a cross-cut of that element, not
        /// an element of its own.</item>
        /// <item><b>Panel-like:</b> a plane whose thickness exceeds <paramref name="maxThicknessRatio"/> of its smallest
        /// in-plane extent is returned in <paramref name="rejected"/>.</item>
        /// <item><b>One element, several thicknesses:</b> a parallel plane whose slab contains, or lies within, the
        /// slab of a plane with a larger overlap is part of that element (a stepped wall, or the thinner wall behind a
        /// window niche). Its thickness is reported, not turned into a second panel.</item>
        /// </list>
        /// Mid-plane normals are oriented so that <see cref="Query.PanelType(Vector3D, double)"/> classifies them
        /// independently of face order: vertical planes are walls, horizontal planes face down (floor or slab) and
        /// sloped planes face up (roof).
        /// </summary>
        private static List<ElementPlane> ElementPlanes(Shell shell, List<Face3D> face3Ds, double minThickness, double maxThicknessRatio, double tolerance_Angle, double tolerance_Distance, double tolerance_Snap, out List<ElementPlane> rejected)
        {
            rejected = new List<ElementPlane>();
            double cosAngle = System.Math.Cos(tolerance_Angle);

            List<ThicknessPair> pairs = new List<ThicknessPair>();
            for (int i = 0; i < face3Ds.Count; i++)
            {
                Plane plane_i = face3Ds[i].GetPlane();
                Face2D face2D_i = plane_i.Convert(face3Ds[i]);
                for (int j = i + 1; j < face3Ds.Count; j++)
                {
                    Plane plane_j = face3Ds[j].GetPlane();
                    if (System.Math.Abs(plane_i.Normal.DotProduct(plane_j.Normal)) < cosAngle)
                    {
                        continue;
                    }

                    Vector3D normal = plane_i.Normal;
                    Point3D origin_i = plane_i.Origin;
                    double offset = normal.DotProduct(new Vector3D(origin_i, plane_j.Origin));
                    double distance = System.Math.Abs(offset);
                    if (distance < minThickness - tolerance_Distance)
                    {
                        continue;
                    }

                    Face2D face2D_j = plane_i.Convert(face3Ds[j]);
                    List<Face2D> overlap = face2D_i == null || face2D_j == null ? null : face2D_i.Intersection(face2D_j, tolerance_Distance);
                    overlap = overlap?.FindAll(x => x != null && x.GetArea() > tolerance_Distance);
                    double overlapArea = overlap?.Sum(x => x.GetArea()) ?? 0;
                    if (overlapArea <= tolerance_Distance)
                    {
                        continue;
                    }

                    Point3D point3D = plane_i.Convert(overlap.OrderByDescending(x => x.GetArea()).First())?.GetInternalPoint3D(tolerance_Distance);
                    if (!HasMaterialBetween(shell, point3D, normal, offset, tolerance_Distance, tolerance_Snap))
                    {
                        continue;
                    }

                    Point3D midOrigin = new Point3D(origin_i.X + normal.X * offset / 2, origin_i.Y + normal.Y * offset / 2, origin_i.Z + normal.Z * offset / 2);
                    Point3D sample = new Point3D(point3D.X + normal.X * offset / 2, point3D.Y + normal.Y * offset / 2, point3D.Z + normal.Z * offset / 2);
                    pairs.Add(new ThicknessPair(new Plane(plane_i, midOrigin), distance, overlapArea, overlap.ConvertAll(x => plane_i.Convert(x)), sample));
                }
            }

            // Pairs on one mid-plane are one element plane.
            List<ElementPlane> elementPlanes = new List<ElementPlane>();
            foreach (ThicknessPair pair in pairs)
            {
                ElementPlane elementPlane = elementPlanes.Find(x => IsParallel(x.MidPlane, pair.MidPlane, cosAngle) && System.Math.Abs(Offset(x.MidPlane, pair.MidPlane)) <= tolerance_Snap);
                if (elementPlane == null)
                {
                    elementPlanes.Add(new ElementPlane(pair));
                }
                else
                {
                    elementPlane.Pairs.Add(pair);
                }
            }

            // Most panel-like first: material crossed by several planes belongs to the one in which it is most
            // panel-like (thickness over the band width of its overlap), whatever maxThicknessRatio is. The band width,
            // not the bounding box, is what tells the walls of a ring (thin across, long) from the ring's top and bottom
            // faces (an 8 x 6 m box around a 0.3 m band). Drop cross-cuts through accepted elements, then test what is
            // left against the threshold.
            List<ElementPlane> accepted = new List<ElementPlane>();
            foreach (ElementPlane elementPlane in elementPlanes.OrderBy(x => x.Thickness / x.BandWidth()).ThenByDescending(x => x.Overlap))
            {
                elementPlane.Pairs.RemoveAll(pair => accepted.Exists(x => !IsParallel(x.MidPlane, pair.MidPlane, cosAngle) && x.Owns(pair.Sample, tolerance_Distance)));
                if (elementPlane.Pairs.Count == 0)
                {
                    continue;
                }

                elementPlane.UpdateMinExtent();
                if (elementPlane.Slenderness > maxThicknessRatio)
                {
                    rejected.Add(elementPlane);
                    continue;
                }

                accepted.Add(elementPlane);
            }

            rejected = rejected.OrderByDescending(x => System.Math.Round(x.Overlap, 6)).ThenBy(x => x.Thickness).ToList();

            // Parallel planes of one element (stepped wall, niche): the largest overlap (ties: the thinner) is the host.
            List<ElementPlane> result = new List<ElementPlane>();
            foreach (ElementPlane elementPlane in accepted.OrderByDescending(x => System.Math.Round(x.Overlap, 6)).ThenBy(x => x.Thickness))
            {
                ElementPlane host = result.Find(x => IsParallel(x.MidPlane, elementPlane.MidPlane, cosAngle) && IsNested(x, elementPlane, tolerance_Snap));
                if (host == null)
                {
                    result.Add(elementPlane);
                    continue;
                }

                host.Others.AddRange(elementPlane.Pairs);
            }

            foreach (ElementPlane elementPlane in result)
            {
                double maxOverlap = elementPlane.Pairs.Concat(elementPlane.Others).Max(x => x.Overlap);
                elementPlane.OtherThicknesses.AddRange(elementPlane.Pairs.Concat(elementPlane.Others)
                    .Where(x => x.Overlap >= 0.05 * maxOverlap && System.Math.Abs(x.Thickness - elementPlane.Thickness) > tolerance_Snap)
                    .Select(x => System.Math.Round(x.Thickness / tolerance_Snap) * tolerance_Snap)
                    .Distinct()
                    .OrderBy(x => x));

                elementPlane.MidPlane = Oriented(elementPlane.MidPlane, tolerance_Angle);
            }

            return result;
        }

        /// <summary>
        /// True when the segment between the two faces of a pair, at a point of their overlap, runs through the
        /// solid's material (sampled at a quarter, half and three quarters of the way).
        /// </summary>
        private static bool HasMaterialBetween(Shell shell, Point3D point3D, Vector3D normal, double offset, double tolerance_Distance, double tolerance_Snap)
        {
            if (point3D == null)
            {
                return false;
            }

            foreach (double factor in new[] { 0.25, 0.5, 0.75 })
            {
                double t = offset * factor;
                if (!shell.Inside(new Point3D(point3D.X + normal.X * t, point3D.Y + normal.Y * t, point3D.Z + normal.Z * t), tolerance_Snap, tolerance_Distance))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsParallel(Plane plane_1, Plane plane_2, double cosAngle)
        {
            return System.Math.Abs(plane_1.Normal.DotProduct(plane_2.Normal)) >= cosAngle;
        }

        private static double Offset(Plane plane_1, Plane plane_2)
        {
            return plane_1.Normal.DotProduct(new Vector3D(plane_1.Origin, plane_2.Origin));
        }

        /// <summary>
        /// True when the slab (mid-plane +/- half the thickness) of one parallel element plane lies within the other's.
        /// </summary>
        private static bool IsNested(ElementPlane elementPlane_1, ElementPlane elementPlane_2, double tolerance)
        {
            double offset = Offset(elementPlane_1.MidPlane, elementPlane_2.MidPlane);
            double min_1 = -elementPlane_1.Thickness / 2, max_1 = elementPlane_1.Thickness / 2;
            double min_2 = offset - elementPlane_2.Thickness / 2, max_2 = offset + elementPlane_2.Thickness / 2;
            return (min_2 >= min_1 - tolerance && max_2 <= max_1 + tolerance) || (min_1 >= min_2 - tolerance && max_1 <= max_2 + tolerance);
        }

        /// <summary>
        /// Vertical planes are kept; horizontal planes face down (Floor) and sloped planes face up (Roof), so the panel
        /// type no longer depends on which face of the pair came first.
        /// </summary>
        private static Plane Oriented(Plane plane, double tolerance_Angle)
        {
            double z = plane.Normal.Unit.Z;
            if (System.Math.Abs(z) <= tolerance_Angle)
            {
                return plane;
            }

            bool horizontal = System.Math.Abs(z) >= System.Math.Cos(tolerance_Angle);
            bool flip = horizontal ? z > 0 : z < 0;
            return flip ? new Plane(plane.Origin, plane.Normal.GetNegated()) : plane;
        }

        /// <summary>
        /// Two parallel faces of a solid with material between them: their mid-plane, separation, projected overlap
        /// and a point of material half-way between them.
        /// </summary>
        private sealed class ThicknessPair
        {
            public ThicknessPair(Plane midPlane, double thickness, double overlap, List<Face3D> footprints, Point3D sample)
            {
                MidPlane = midPlane;
                Thickness = thickness;
                Overlap = overlap;
                Footprints = footprints;
                Sample = sample;
            }

            public Plane MidPlane { get; }

            public double Thickness { get; }

            public double Overlap { get; }

            public List<Face3D> Footprints { get; }

            public Point3D Sample { get; }
        }

        /// <summary>
        /// One planar building element of a solid: the pairs on its mid-plane, the parallel pairs of the same element at
        /// other thicknesses (<see cref="Others"/>), and the main thickness (the largest-overlap pair; ties: the thinner).
        /// </summary>
        private sealed class ElementPlane
        {
            public ElementPlane(ThicknessPair pair)
            {
                MidPlane = pair.MidPlane;
                Pairs.Add(pair);
            }

            public Plane MidPlane { get; set; }

            public List<ThicknessPair> Pairs { get; } = new List<ThicknessPair>();

            public List<ThicknessPair> Others { get; } = new List<ThicknessPair>();

            public double Thickness => Pairs.OrderByDescending(x => System.Math.Round(x.Overlap, 6)).ThenBy(x => x.Thickness).First().Thickness;

            public double Overlap => Pairs.Sum(x => x.Overlap);

            public double MinExtent { get; private set; }

            /// <summary>
            /// Thickness over the smallest in-plane extent of the pairs' overlap (infinite without an overlap).
            /// </summary>
            public double Slenderness => MinExtent <= 0 ? double.PositiveInfinity : Thickness / MinExtent;

            /// <summary>
            /// Typical in-plane width of the pairs' overlap: twice its area over its perimeter (the width of a long
            /// strip or band; zero without an overlap).
            /// </summary>
            public double BandWidth()
            {
                double area = 0, perimeter = 0;
                foreach (Face2D face2D in Footprints.ConvertAll(x => MidPlane.Convert(x)).FindAll(x => x != null))
                {
                    area += face2D.GetArea();
                    perimeter += face2D.ExternalEdge2D.Perimeter() + (face2D.InternalEdge2Ds?.Sum(x => x.Perimeter()) ?? 0);
                }

                return perimeter <= 0 ? 0 : 2 * area / perimeter;
            }

            public void UpdateMinExtent()
            {
                List<Point2D> point2Ds = Footprints.ConvertAll(x => MidPlane.Convert(x)).SelectMany(x => (x?.ExternalEdge2D as ISegmentable2D)?.GetPoints() ?? new List<Point2D>()).ToList();
                BoundingBox2D boundingBox2D = point2Ds.Count == 0 ? null : new BoundingBox2D(point2Ds);
                MinExtent = boundingBox2D == null ? 0 : System.Math.Min(boundingBox2D.Width, boundingBox2D.Height);
            }

            public List<Face3D> Footprints => Pairs.Concat(Others).SelectMany(x => x.Footprints).ToList();

            public List<double> OtherThicknesses { get; } = new List<double>();

            /// <summary>
            /// True when <paramref name="point3D"/> (a point of the solid's material) lies in this element: within half
            /// its thickness of the mid-plane, and inside the overlap of its own pairs or within one thickness of it
            /// (the corner where the element meets another one).
            /// </summary>
            public bool Owns(Point3D point3D, double tolerance)
            {
                if (point3D == null || System.Math.Abs(MidPlane.Normal.DotProduct(new Vector3D(MidPlane.Origin, point3D))) > Thickness / 2 + tolerance)
                {
                    return false;
                }

                Point2D point2D = MidPlane.Convert(point3D);
                foreach (Face2D footprint in Pairs.SelectMany(x => x.Footprints).Select(x => MidPlane.Convert(x)))
                {
                    if (footprint != null && (footprint.Inside(point2D, tolerance) || (footprint.ExternalEdge2D as ISegmentable2D)?.Distance(point2D) <= Thickness + tolerance))
                    {
                        return true;
                    }
                }

                return false;
            }
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
