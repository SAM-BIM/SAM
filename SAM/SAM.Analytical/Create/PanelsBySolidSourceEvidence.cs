// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core;
using SAM.Geometry.Planar;
using SAM.Geometry.Spatial;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SAM.Analytical
{
    public static partial class Create
    {
        /// <summary>
        /// Proof that the material of a closed solid fills a symmetric slab about a host plane, for the solids where that can be
        /// proved: closed, valid, layered prismatic ones. Anything else is no evidence (the caller keeps today's behaviour).
        /// <list type="number">
        /// <item><b>Closed and valid</b> (checked first, before any section): every face valid, at least four faces, every edge (outer
        /// and inner loops) straight, and every non-degenerate edge matched by exactly one other edge, either orientation,
        /// endpoints within the distance tolerance. <see cref="Shell.IsClosed"/> is not used alone: it is true when the shell has
        /// no naked segments to report. A face split by a T-junction fails.</item>
        /// <item><b>Positional classification</b> about the host normal n, with deviation bound delta equal to the distance
        /// tolerance: a face is <i>parallel</i> when the offsets of all its vertices along n differ by at most delta; otherwise it is
        /// <i>perpendicular</i> when every edge has <c>min(|n.e|, |e - (n.e)n|)</c> of at most delta, with at least one edge along
        /// n and one across; otherwise there is no evidence. Both measures are in metres over the actual extent, so a long, slightly
        /// sloped face fails whatever its angle. Nothing is judged by an angle.</item>
        /// <item><b>Layers:</b> the layers between the parallel-face offsets inside the slab (and the slab's two sides) are
        /// sampled once each, at their midpoint. Within delta every face is parallel or perpendicular and every edge of a perpendicular
        /// face runs along or across n, so the section changes only at a parallel face's offset, and one sample per layer proves
        /// material over the layer. The support is the intersection over the layers of the material regions of the section (the
        /// regions whose internal point is inside the solid; inner loops kept). A layer thinner than the snap tolerance cannot be
        /// sampled reliably and fails.</item>
        /// <item><b>Definite results:</b> no <c>Shell.Difference</c>; an empty or missing section is a failure, never proof;
        /// intersections go through <see cref="Query.DefiniteIntersection(IEnumerable{Face2D}, IEnumerable{Face2D}, double)"/>.</item>
        /// </list>
        /// Not checked, stated as residual: self-intersection of the solid and the outward orientation of its faces (the proof
        /// uses face geometry, not normal signs).
        /// </summary>
        /// <param name="shell">The solid, in metres.</param>
        /// <param name="host">The host plane (the element's mid-plane).</param>
        /// <param name="halfThickness">Half the slab thickness [m] to certify.</param>
        /// <param name="tolerance_Angle">Angle tolerance [rad] of the section.</param>
        /// <param name="tolerance_Distance">Distance tolerance [m]; also the positional deviation bound and the edge-pairing tolerance.</param>
        /// <param name="tolerance_Snap">Snap tolerance [m] of the section; also the thinnest layer that can be sampled.</param>
        /// <param name="support">The proved region, in the frame of <paramref name="host"/>, inner loops kept; null when there is no evidence.</param>
        /// <param name="reason">Why there is no evidence; null when certified.</param>
        /// <param name="section">Section of the solid on a plane (the solid's own <c>Section</c> by default); a seam for tests.</param>
        /// <param name="intersection">Intersection of two regions (<c>Face2D.Intersection</c> by default); a seam for tests.</param>
        /// <returns>True when the slab is proved to be material over a non-empty support.</returns>
        internal static bool TryCertifyMaterial(Shell shell, Plane host, double halfThickness, double tolerance_Angle, double tolerance_Distance, double tolerance_Snap, out List<Face2D> support, out string reason, Func<Shell, Plane, List<Face3D>> section = null, Func<Face2D, Face2D, double, List<Face2D>> intersection = null)
        {
            support = null;
            reason = null;

            if (shell == null || host == null || double.IsNaN(halfThickness) || double.IsInfinity(halfThickness) || halfThickness <= tolerance_Distance)
            {
                reason = "no slab to certify";
                return false;
            }

            if (!IsClosedAndValid(shell, tolerance_Distance, out reason))
            {
                reason = "not a closed, valid solid: " + reason;
                return false;
            }

            Vector3D normal = host.Normal.Unit;
            if (!TryClassifyPositionally(shell, normal, host.Origin, tolerance_Distance, out List<double> offsets, out reason))
            {
                return false;
            }

            // The slab's sides and every parallel-face offset strictly inside it.
            List<double> cuts = new List<double> { -halfThickness, halfThickness };
            foreach (double offset in offsets.OrderBy(x => x))
            {
                if (offset > -halfThickness + tolerance_Distance && offset < halfThickness - tolerance_Distance && !cuts.Exists(x => System.Math.Abs(x - offset) < tolerance_Distance))
                {
                    cuts.Add(offset);
                }
            }

            cuts.Sort();

            section = section ?? ((x, y) => x.Section(y, true, tolerance_Angle, tolerance_Distance, tolerance_Snap));

            List<Face2D> result = null;
            for (int i = 0; i + 1 < cuts.Count; i++)
            {
                if (cuts[i + 1] - cuts[i] < tolerance_Snap)
                {
                    reason = string.Format(System.Globalization.CultureInfo.InvariantCulture, "a layer at offset {0:0.#####} m is thinner than the snap tolerance", cuts[i]);
                    return false;
                }

                double middle = (cuts[i] + cuts[i + 1]) / 2;
                List<Face3D> sections = section(shell, new Plane(host, new Point3D(host.Origin.X + normal.X * middle, host.Origin.Y + normal.Y * middle, host.Origin.Z + normal.Z * middle)));
                if (sections == null || sections.Count == 0)
                {
                    reason = string.Format(System.Globalization.CultureInfo.InvariantCulture, "the section at layer offset {0:0.#####} m failed or is empty", middle);
                    return false;
                }

                // The section also returns the voids themselves as faces; keep only regions of material.
                List<Face2D> layer = new List<Face2D>();
                foreach (Face3D section_Face3D in sections)
                {
                    Point3D internalPoint = section_Face3D?.GetInternalPoint3D(tolerance_Distance);
                    if (internalPoint == null || !shell.Inside(internalPoint, tolerance_Snap, tolerance_Distance))
                    {
                        continue;
                    }

                    Face2D face2D = host.Convert(section_Face3D);
                    if (face2D != null && face2D.GetArea() > tolerance_Distance)
                    {
                        layer.Add(face2D);
                    }
                }

                if (layer.Count == 0)
                {
                    reason = string.Format(System.Globalization.CultureInfo.InvariantCulture, "no material in the layer at offset {0:0.#####} m", middle);
                    return false;
                }

                result = result == null ? layer : Query.DefiniteIntersection(result, layer, tolerance_Distance, intersection);
                if (result == null)
                {
                    reason = string.Format(System.Globalization.CultureInfo.InvariantCulture, "the intersection of regions failed at layer offset {0:0.#####} m", middle);
                    return false;
                }

                if (result.Count == 0)
                {
                    reason = string.Format(System.Globalization.CultureInfo.InvariantCulture, "no region is material through the layer at offset {0:0.#####} m", middle);
                    return false;
                }
            }

            support = result;
            return true;
        }

        /// <summary>
        /// The straight edges of a face (outer and inner loops) as point pairs; a null pair marks a loop that cannot be
        /// segmented (a curve).
        /// </summary>
        private static IEnumerable<(Point3D Start, Point3D End)> EdgesOf(Face3D face3D)
        {
            List<IClosedPlanar3D> loops = new List<IClosedPlanar3D> { face3D.GetExternalEdge3D() };
            loops.AddRange(face3D.GetInternalEdge3Ds() ?? new List<IClosedPlanar3D>());
            foreach (IClosedPlanar3D loop in loops)
            {
                if (!(loop is ISegmentable3D segmentable3D))
                {
                    yield return (null, null);
                    continue;
                }

                foreach (Segment3D segment3D in segmentable3D.GetSegments())
                {
                    yield return (segment3D[0], segment3D[1]);
                }
            }
        }

        /// <summary>
        /// Closed, valid shell precondition: every face valid, at least four faces, every edge straight, every non-degenerate edge
        /// matched by exactly one other edge (either orientation) within <paramref name="tolerance"/>.
        /// </summary>
        internal static bool IsClosedAndValid(Shell shell, double tolerance, out string reason)
        {
            reason = null;
            List<Face3D> face3Ds = shell?.Face3Ds;
            if (face3Ds == null || face3Ds.Count < 4)
            {
                reason = "fewer than 4 faces";
                return false;
            }

            List<(int Face, Point3D Start, Point3D End, double X)> edges = new List<(int, Point3D, Point3D, double)>();
            for (int i = 0; i < face3Ds.Count; i++)
            {
                Face3D face3D = face3Ds[i];
                if (face3D == null || !face3D.IsValid())
                {
                    reason = string.Format("face {0} is not valid", i);
                    return false;
                }

                foreach ((Point3D start, Point3D end) in EdgesOf(face3D))
                {
                    if (start == null || end == null)
                    {
                        reason = string.Format("face {0} has an edge that is not a straight segment", i);
                        return false;
                    }

                    if (start.Distance(end) > tolerance)
                    {
                        edges.Add((i, start, end, (start.X + end.X) / 2));
                    }
                }
            }

            // Matching edges have midpoints within tolerance of each other, so a sweep along X finds every candidate.
            edges = edges.OrderBy(x => x.X).ToList();
            int[] matches = new int[edges.Count];
            for (int i = 0; i < edges.Count; i++)
            {
                for (int j = i + 1; j < edges.Count && edges[j].X - edges[i].X <= 2 * tolerance; j++)
                {
                    bool same = (edges[i].Start.Distance(edges[j].Start) <= tolerance && edges[i].End.Distance(edges[j].End) <= tolerance)
                        || (edges[i].Start.Distance(edges[j].End) <= tolerance && edges[i].End.Distance(edges[j].Start) <= tolerance);
                    if (same)
                    {
                        matches[i]++;
                        matches[j]++;
                    }
                }
            }

            for (int i = 0; i < edges.Count; i++)
            {
                if (matches[i] != 1)
                {
                    reason = string.Format("an edge of face {0} is shared by {1} other edge(s), not exactly 1", edges[i].Face, matches[i]);
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Positional classification about <paramref name="normal"/>: every face is parallel (all vertex offsets along the normal
        /// within <paramref name="delta"/>) or perpendicular (every edge within <paramref name="delta"/> of along or across the
        /// normal, with an edge of each kind). Gives the offsets of the parallel faces.
        /// </summary>
        private static bool TryClassifyPositionally(Shell shell, Vector3D normal, Point3D origin, double delta, out List<double> parallelOffsets, out string reason)
        {
            parallelOffsets = new List<double>();
            reason = null;
            foreach (Face3D face3D in shell.Face3Ds)
            {
                List<(Point3D Start, Point3D End)> edges = EdgesOf(face3D).ToList();
                if (edges.Count == 0 || edges.Exists(x => x.Start == null || x.End == null))
                {
                    reason = "a face has an edge that is not a straight segment";
                    return false;
                }

                List<double> offsets = edges.ConvertAll(x => normal.DotProduct(new Vector3D(origin, x.Start)));
                double spread = offsets.Max() - offsets.Min();
                if (spread <= delta)
                {
                    parallelOffsets.Add(offsets.Average());
                    continue;
                }

                bool across = false, along = false;
                foreach ((Point3D start, Point3D end) in edges)
                {
                    Vector3D edge = new Vector3D(start, end);
                    double parallelPart = System.Math.Abs(normal.DotProduct(edge));
                    double perpendicularPart = System.Math.Sqrt(System.Math.Max(0, edge.DotProduct(edge) - parallelPart * parallelPart));
                    double deviation = System.Math.Min(parallelPart, perpendicularPart);
                    if (deviation > delta)
                    {
                        reason = string.Format(System.Globalization.CultureInfo.InvariantCulture, "a face is neither parallel nor perpendicular to the element: an edge deviates {0:0.##e+0} m from along or across it over a length of {1:0.#####} m (face spread {2:0.##e+0} m)", deviation, edge.Length, spread);
                        return false;
                    }

                    if (parallelPart <= delta)
                    {
                        across = true;
                    }
                    else
                    {
                        along = true;
                    }
                }

                if (!across || !along)
                {
                    reason = "a perpendicular face lacks an edge along or an edge across the element's normal";
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Half the slab thickness [m] about the host plane that every slab of the element plane covers: the least, over the pairs
        /// (and parallel pairs of the same element), of the distance from the host plane to the nearer side of the pair's slab. Zero or
        /// negative when a pair's slab does not contain the host plane.
        /// </summary>
        private static double GuaranteedHalfThickness(ElementPlane elementPlane)
        {
            Plane host = elementPlane.MidPlane;
            double result = double.PositiveInfinity;
            foreach (ThicknessPair pair in elementPlane.Pairs.Concat(elementPlane.Others))
            {
                double offset = Offset(host, pair.MidPlane);
                double half = pair.Thickness / 2;
                result = System.Math.Min(result, System.Math.Min(offset + half, half - offset));
            }

            return result;
        }

        /// <summary>
        /// The <see cref="SourceEvidence"/> of a host panel of <paramref name="elementPlane"/>, or null. <paramref name="support"/>
        /// is the element plane's proved support, in the frame of its mid-plane, and is certified once per element plane.
        /// </summary>
        private static SourceEvidence SourceEvidenceOf(Panel panel, ElementPlane elementPlane, double halfThickness, List<Face2D> support, double tolerance_Snap)
        {
            Plane frame = panel?.Plane;
            Face3D face3D = panel?.GetFace3D();
            if (frame == null || face3D == null || support == null || support.Count == 0)
            {
                return null;
            }

            Face2D footprint = frame.Convert(face3D);
            BoundingBox2D bounds = footprint?.GetBoundingBox();
            if (bounds == null)
            {
                return null;
            }

            // Support is expressed in the panel's own frame; only the regions near the panel are kept.
            List<Face2D> support_Panel = new List<Face2D>();
            foreach (Face2D region in support)
            {
                Face3D region3D = region == null ? null : elementPlane.MidPlane.Convert(region);
                Face2D region_Panel = region3D == null ? null : frame.Convert(region3D);
                BoundingBox2D regionBounds = region_Panel?.GetBoundingBox();
                if (regionBounds == null)
                {
                    return null;
                }

                if (regionBounds.Max.X < bounds.Min.X - tolerance_Snap || bounds.Max.X < regionBounds.Min.X - tolerance_Snap || regionBounds.Max.Y < bounds.Min.Y - tolerance_Snap || bounds.Max.Y < regionBounds.Min.Y - tolerance_Snap)
                {
                    continue;
                }

                support_Panel.Add(region_Panel);
            }

            if (support_Panel.Count == 0)
            {
                return null;
            }

            SourceEvidence result = new SourceEvidence(frame, 2 * halfThickness, support_Panel, new List<Face2D> { footprint }, new List<Face2D> { footprint });
            return result.IsComplete ? result : null;
        }
    }
}
