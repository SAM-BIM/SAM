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
        /// <see cref="PanelsBySolid(Shell, Core.Diagnostics, IEnumerable{ISAMGeometry3D}, double, double, bool, string, string, string, double, double, double, int?)"/>
        /// on every solid of a set, followed by a pass that judges fragments as parts of their element.
        /// <para>
        /// Models rebuilt from other tools split one wall or slab into pieces: piers between windows, pieces between
        /// junctions, slab patches. A piece can fail <paramref name="maxThicknessRatio"/> on its own (a 0.45 m wall 2.1 m
        /// long) although the element it belongs to is panel-like. Here a skipped solid is kept when one of its element
        /// planes rejected as not panel-like (whatever reason the solid's warning gives: a wall around a large opening is
        /// reported by its thin jambs) is part of a panel-like element:
        /// </para>
        /// <list type="number">
        /// <item><b>Pieces of one element:</b> element planes of different solids (the skipped ones and the Panels created)
        /// that are parallel, on the same mid-plane and of the same thickness (within <paramref name="tolerance_Continuation"/>),
        /// and joined: touching, across a junction (a non-parallel Panel at least as thick as the gap runs through it), or
        /// across an opening (an aperture of <paramref name="apertureGeometries"/> touches both).</item>
        /// <item><b>Panel-like element:</b> the thickness over the smallest extent of all joined pieces together is within
        /// <paramref name="maxThicknessRatio"/>. A piece joined to a created Panel of its own element plane therefore
        /// usually passes; pieces joined only to one another pass when together they are long and high enough.</item>
        /// <item><b>Not kept:</b> a piece narrower than <paramref name="minThickness"/> in its plane (a sliver) or thicker
        /// than its smallest extent (the top and bottom of a pier), a plane
        /// thinner than <paramref name="minThickness"/>, and a piece of nothing (a stair, a landing on its own). From each
        /// kept solid the most panel-like such plane gives the Panels.</item>
        /// </list>
        /// </summary>
        /// <param name="shells">Closed solids, in metres. A null entry is reported as not a closed solid.</param>
        /// <param name="diagnostics">Collection the records are appended to, solid by solid in input order. A kept
        /// solid's records are a Summary Info (<see cref="BySolidsDiagnosticCode.PanelsContinuation"/>) and those of its
        /// kept plane, instead of its skip Warning. Null collects nothing.</param>
        /// <param name="sourceNames">Source name per solid (optional, index-aligned).</param>
        /// <param name="sourceIds">Source identifier per solid (optional, index-aligned).</param>
        /// <param name="sourceLayers">Source layer per solid (optional, index-aligned).</param>
        /// <param name="tolerance_Continuation">Mid-plane offset, thickness difference and contact gap [m] within which
        /// planes are pieces of one element; 0 disables the pass.</param>
        /// <returns>The Panels of each solid, index-aligned with <paramref name="shells"/>; an empty list for a skipped solid.</returns>
        /// <remarks>All other parameters are those of <c>PanelsBySolid</c>; the solids are numbered by their index.</remarks>
        public static List<List<Panel>> PanelsBySolids(
            this IList<Shell> shells,
            Core.Diagnostics diagnostics,
            IEnumerable<ISAMGeometry3D> apertureGeometries = null,
            double minThickness = 0.08,
            double maxThicknessRatio = 0.2,
            bool airForUnclaimedOpenings = true,
            IList<string> sourceNames = null,
            IList<string> sourceIds = null,
            IList<string> sourceLayers = null,
            double tolerance_Continuation = 0.01,
            double tolerance_Angle = Tolerance.Angle,
            double tolerance_Distance = Tolerance.Distance,
            double tolerance_Snap = Tolerance.MacroDistance)
        {
            diagnostics = diagnostics ?? new Core.Diagnostics();
            List<List<Panel>> result = new List<List<Panel>>();
            if (shells == null)
            {
                return result;
            }

            List<ISAMGeometry3D> apertureGeometries_List = apertureGeometries?.ToList();
            List<Core.Diagnostics> records = new List<Core.Diagnostics>();
            List<Piece> pieces = new List<Piece>();
            for (int i = 0; i < shells.Count; i++)
            {
                Core.Diagnostics records_Solid = new Core.Diagnostics(diagnostics.Detail);
                List<Panel> panels = PanelsBySolid(shells[i], records_Solid, apertureGeometries_List, minThickness, maxThicknessRatio, airForUnclaimedOpenings, Item(sourceNames, i), Item(sourceIds, i), Item(sourceLayers, i), tolerance_Angle, tolerance_Distance, tolerance_Snap, i, out List<ElementPlane> notPanelLike);
                result.Add(panels);
                records.Add(records_Solid);

                foreach (Panel panel in panels)
                {
                    Face3D face3D = panel?.GetFace3D();
                    if (panel.PanelType != PanelType.Air && face3D?.GetPlane() != null && panel.TryGetValue(PanelSourceParameter.PhysicalThickness, out double thickness))
                    {
                        pieces.Add(new Piece(i, null, face3D.GetPlane(), thickness, new List<Face3D> { face3D }));
                    }
                }

                // Any skipped solid with a plane rejected as not panel-like, whatever reason its warning gives (a wall around a
                // large opening is reported by its thin jambs). Never a plane thinner than minThickness, a sliver, or a plane
                // thicker than it is wide (the top and bottom of a pier, which would join every pier of a storey).
                if (panels.Count != 0 || notPanelLike.Count == 0)
                {
                    continue;
                }

                foreach (ElementPlane elementPlane in notPanelLike)
                {
                    if (elementPlane.Thickness < minThickness - tolerance_Distance || elementPlane.MinExtent < minThickness - tolerance_Distance || elementPlane.Slenderness > 1)
                    {
                        continue;
                    }

                    Plane midPlane = elementPlane.MidPlane;
                    List<Face3D> footprints = elementPlane.Footprints.ConvertAll(x => midPlane.Convert(x)).FindAll(x => x?.ExternalEdge2D is ISegmentable2D).ConvertAll(x => midPlane.Convert(x));
                    if (footprints.Count != 0)
                    {
                        pieces.Add(new Piece(i, elementPlane, midPlane, elementPlane.Thickness, footprints));
                    }
                }
            }

            if (tolerance_Continuation > 0)
            {
                KeepPiecesOfElements(shells, pieces, result, records, diagnostics.Detail, apertureGeometries_List, maxThicknessRatio, airForUnclaimedOpenings, sourceNames, sourceIds, sourceLayers, tolerance_Continuation, tolerance_Angle, tolerance_Distance, tolerance_Snap);
            }

            foreach (Core.Diagnostics records_Solid in records)
            {
                foreach (Diagnostic diagnostic in records_Solid)
                {
                    diagnostics.Add(diagnostic);
                }
            }

            return result;
        }

        private static void KeepPiecesOfElements(IList<Shell> shells, List<Piece> pieces, List<List<Panel>> result, List<Core.Diagnostics> records, DiagnosticDetail detail, List<ISAMGeometry3D> apertureGeometries, double maxThicknessRatio, bool airForUnclaimedOpenings, IList<string> sourceNames, IList<string> sourceIds, IList<string> sourceLayers, double tolerance, double tolerance_Angle, double tolerance_Distance, double tolerance_Snap)
        {
            double cosAngle = System.Math.Cos(tolerance_Angle);
            double minOpeningArea = System.Math.Max(tolerance_Snap * tolerance_Snap, tolerance_Distance);

            // Repeat while solids are kept: their Panels join further pieces and frame further gaps on the next pass.
            bool changed = true;
            while (changed)
            {
                changed = false;

                // Every decision of a pass reads one immutable snapshot of the pieces; the pieces change only after all
                // solids of the pass are decided, so no decision depends on the order of the solids.
                List<Piece> snapshot = new List<Piece>(pieces);
                List<Piece> candidates = snapshot.FindAll(x => x.ElementPlane != null);
                if (candidates.Count == 0)
                {
                    return;
                }

                List<Piece> panelPieces = snapshot.FindAll(x => x.ElementPlane == null);
                Dictionary<Piece, List<Face2D>> openings = candidates.ToDictionary(x => x, x => ApertureFootprints(apertureGeometries, x.Plane, x.Thickness, tolerance_Snap));

                // Join every candidate with the pieces of its element: union-find over the snapshot, membership by piece.
                int[] parent = Enumerable.Range(0, snapshot.Count).ToArray();
                int Find(int x) => parent[x] == x ? x : parent[x] = Find(parent[x]);
                Dictionary<Piece, int> positions = Enumerable.Range(0, snapshot.Count).ToDictionary(i => snapshot[i], i => i);
                foreach (Piece candidate in candidates)
                {
                    int index = positions[candidate];
                    for (int j = 0; j < snapshot.Count; j++)
                    {
                        Piece piece = snapshot[j];
                        if (piece.Index == candidate.Index || Find(j) == Find(index) || System.Math.Abs(piece.Thickness - candidate.Thickness) > tolerance || !IsParallel(candidate.Plane, piece.Plane, cosAngle) || System.Math.Abs(Offset(candidate.Plane, piece.Plane)) > tolerance)
                        {
                            continue;
                        }

                        if (Joined(candidate, piece, openings[candidate], panelPieces, cosAngle, tolerance))
                        {
                            parent[Find(j)] = Find(index);
                        }
                    }
                }

                Dictionary<int, List<Piece>> roots = Enumerable.Range(0, snapshot.Count).GroupBy(Find).ToDictionary(x => x.Key, x => x.Select(i => snapshot[i]).ToList());
                Dictionary<Piece, List<Piece>> elements = snapshot.ToDictionary(x => x, x => roots[Find(positions[x])]);

                List<(int Solid, ElementPlane ElementPlane, List<Piece> Element, double Slenderness)> decisions = new List<(int, ElementPlane, List<Piece>, double)>();
                foreach (int solid in candidates.Select(x => x.Index).Distinct().OrderBy(x => x).ToList())
                {
                    // A piece is kept when its element is panel-like (thickness over the smallest extent of all its pieces
                    // together), or when it closes a gap: every edge abuts a crossing Panel or an opening.
                    List<(Piece Piece, List<Piece> Element, double Slenderness)> kept = new List<(Piece, List<Piece>, double)>();
                    foreach (Piece candidate in candidates.FindAll(x => x.Index == solid))
                    {
                        // A piece within the thickness of a crossing element (a strip at slab level over a wall) is that
                        // element's edge, not an element: kept, it would enclose a sliver of space.
                        if (panelPieces.Exists(x => Embeds(x, candidate, cosAngle, tolerance)))
                        {
                            continue;
                        }

                        List<Piece> element = elements[candidate];
                        if (element.Count >= 2)
                        {
                            List<Point2D> point2Ds = element.SelectMany(x => x.Faces).SelectMany(x => (candidate.Plane.Convert(x)?.ExternalEdge2D as ISegmentable2D)?.GetPoints() ?? new List<Point2D>()).ToList();
                            BoundingBox2D boundingBox2D = point2Ds.Count == 0 ? null : new BoundingBox2D(point2Ds);
                            double minExtent = boundingBox2D == null ? 0 : System.Math.Min(boundingBox2D.Width, boundingBox2D.Height);
                            double slenderness = minExtent <= 0 ? double.PositiveInfinity : candidate.Thickness / minExtent;
                            if (slenderness <= maxThicknessRatio)
                            {
                                kept.Add((candidate, element, slenderness));
                                continue;
                            }
                        }

                        if (Framed(candidate, openings[candidate], panelPieces, cosAngle, tolerance_Angle, tolerance))
                        {
                            kept.Add((candidate, null, double.NaN));
                        }
                    }

                    if (kept.Count == 0)
                    {
                        continue;
                    }

                    (Piece piece, List<Piece> element_Kept, double slenderness_Element) = kept.OrderBy(x => x.Piece.ElementPlane.Slenderness).First();
                    decisions.Add((solid, piece.ElementPlane, element_Kept, slenderness_Element));
                }

                // Apply the pass: Panels of the kept solids, which take part as Panels from the next pass on.
                foreach ((int solid, ElementPlane elementPlane, List<Piece> element_Kept, double slenderness_Element) in decisions)
                {
                    elementPlane.MidPlane = Oriented(elementPlane.MidPlane, tolerance_Angle);

                    string subject = SolidSubject(Item(sourceNames, solid), solid);
                    string reason;
                    if (element_Kept == null)
                    {
                        reason = "it closes a gap: every edge meets a crossing Panel or an opening (a short wall or slab between other elements).";
                    }
                    else
                    {
                        List<int> created = element_Kept.FindAll(x => x.ElementPlane == null).Select(x => x.Index).Distinct().OrderBy(x => x).ToList();
                        List<int> others = element_Kept.FindAll(x => x.ElementPlane != null && x.Index != solid).Select(x => x.Index).Distinct().OrderBy(x => x).ToList();
                        string pieces_Text = string.Join("; ", new[] { created.Count == 0 ? null : "Panels of " + Solids(created, sourceNames), others.Count == 0 ? null : "skipped pieces " + Solids(others, sourceNames) }.Where(x => x != null));
                        reason = FormatInvariant("it is a piece of an element on the same mid-plane and of the same thickness, whose pieces together give {0:0.###}. Other pieces: {1}.", slenderness_Element, pieces_Text);
                    }

                    Core.Diagnostics records_Solid = new Core.Diagnostics(detail);
                    records_Solid.Add(new Diagnostic(DiagnosticSeverity.Info, DiagnosticDetail.Summary, subject, FormatInvariant("Kept although not panel-like on its own (thickness {0:0.###} m over smallest extent {1:0.###} m gives {2:0.###}, above maxThicknessRatio {3:0.###}): {4}", elementPlane.Thickness, elementPlane.MinExtent, elementPlane.Slenderness, maxThicknessRatio, reason), BySolidsDiagnosticCode.PanelsContinuation, solid, elementPlane.Slenderness, maxThicknessRatio, "-"));

                    List<Panel> panels = PanelsByElementPlane(shells[solid], elementPlane, records_Solid, subject, solid, apertureGeometries, airForUnclaimedOpenings, Item(sourceNames, solid), Item(sourceIds, solid), Item(sourceLayers, solid), minOpeningArea, tolerance_Angle, tolerance_Distance, tolerance_Snap, out bool _);

                    // Kept or not, the planes of this solid are no longer candidates.
                    pieces.RemoveAll(x => x.Index == solid && x.ElementPlane != null);
                    if (panels.Count == 0)
                    {
                        continue;
                    }

                    result[solid] = panels;
                    records[solid] = records_Solid;
                    foreach (Panel panel in panels.FindAll(x => x.PanelType != PanelType.Air))
                    {
                        pieces.Add(new Piece(solid, null, panel.GetFace3D().GetPlane(), elementPlane.Thickness, new List<Face3D> { panel.GetFace3D() }));
                    }

                    changed = true;
                }
            }
        }

        /// <summary>
        /// True when a vertical or horizontal piece closes a gap between other elements: each edge of its outline abuts a
        /// crossing (non-parallel) Panel, within half that Panel's thickness of its mid-plane, or an opening. A sloped
        /// piece (a stair flight) is never framed.
        /// </summary>
        private static bool Framed(Piece piece, List<Face2D> openings, List<Piece> panelPieces, double cosAngle, double tolerance_Angle, double tolerance)
        {
            Plane plane = piece.Plane;
            double z = System.Math.Abs(plane.Normal.Unit.Z);
            if (z > System.Math.Sin(tolerance_Angle) && z < cosAngle)
            {
                return false;
            }

            List<Segment2D> segment2Ds = piece.Faces.ConvertAll(x => plane.Convert(x)?.ExternalEdge2D as ISegmentable2D).FindAll(x => x != null).SelectMany(x => x.GetSegments()).Where(x => x.GetLength() > tolerance).ToList();
            if (segment2Ds.Count == 0)
            {
                return false;
            }

            foreach (Segment2D segment2D in segment2Ds)
            {
                Point2D mid = segment2D.Mid();
                if (openings.Exists(x => x.Inside(mid, tolerance) || ((ISegmentable2D)x.ExternalEdge2D).Distance(mid) <= tolerance))
                {
                    continue;
                }

                Point3D point3D = plane.Convert(mid);
                if (!panelPieces.Exists(x => Abuts(x, point3D, piece.Thickness, cosAngle, plane, tolerance)))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// True when every corner of <paramref name="piece"/> lies within the thickness of the crossing (non-parallel)
        /// Panel <paramref name="crossing"/>, next to it.
        /// </summary>
        private static bool Embeds(Piece crossing, Piece piece, double cosAngle, double tolerance)
        {
            if (IsParallel(piece.Plane, crossing.Plane, cosAngle))
            {
                return false;
            }

            double reach = crossing.Thickness / 2 + tolerance;
            List<Point3D> point3Ds = piece.Faces.SelectMany(x => (x.GetExternalEdge3D() as ISegmentable3D)?.GetPoints() ?? new List<Point3D>()).ToList();
            if (point3Ds.Count == 0 || point3Ds.Exists(x => System.Math.Abs(crossing.Plane.Normal.Unit.DotProduct(new Vector3D(crossing.Plane.Origin, x))) > reach))
            {
                return false;
            }

            BoundingBox3D boundingBox3D = new BoundingBox3D(crossing.Faces.ConvertAll(x => x.GetBoundingBox(reach + piece.Thickness / 2)));
            return boundingBox3D.Intersect(new BoundingBox3D(point3Ds));
        }

        /// <summary>
        /// True when a point on <paramref name="plane"/> (the middle of an edge of a piece) lies against a crossing Panel:
        /// within half its thickness of its mid-plane and over its face. Before snapping the crossing Panel stops half the
        /// piece's thickness short of the piece's plane.
        /// </summary>
        private static bool Abuts(Piece crossing, Point3D point3D, double thickness, double cosAngle, Plane plane, double tolerance)
        {
            if (IsParallel(plane, crossing.Plane, cosAngle) || System.Math.Abs(crossing.Plane.Normal.Unit.DotProduct(new Vector3D(crossing.Plane.Origin, point3D))) > crossing.Thickness / 2 + tolerance)
            {
                return false;
            }

            Point2D point2D = crossing.Plane.Convert(crossing.Plane.Project(point3D));
            foreach (Face3D face3D in crossing.Faces)
            {
                Face2D face2D = crossing.Plane.Convert(face3D);
                if (face2D?.ExternalEdge2D is ISegmentable2D outline && (face2D.Inside(point2D, tolerance) || outline.Distance(point2D) <= thickness / 2 + tolerance))
                {
                    return true;
                }
            }

            return false;
        }

        private static string Solids(List<int> indexes, IList<string> sourceNames)
        {
            string text = string.Join(", ", indexes.Take(5).Select(x => SolidSubject(Item(sourceNames, x), x).Substring("Solid ".Length)));
            return indexes.Count > 5 ? FormatInvariant("{0} and {1} more", text, indexes.Count - 5) : text;
        }

        private static string Item(IList<string> values, int index)
        {
            return values != null && index < values.Count ? values[index] : null;
        }

        /// <summary>
        /// True when two pieces on one mid-plane are joined: they touch, an opening touches both, or a non-parallel Panel
        /// at least as thick as the gap runs through it (a junction).
        /// </summary>
        private static bool Joined(Piece piece_1, Piece piece_2, List<Face2D> openings, List<Piece> panelPieces, double cosAngle, double tolerance)
        {
            Plane plane = piece_1.Plane;
            List<Face2D> faces_1 = piece_1.Faces.ConvertAll(x => plane.Convert(x)).FindAll(x => x?.ExternalEdge2D is ISegmentable2D);
            List<Face2D> faces_2 = piece_2.Faces.ConvertAll(x => plane.Convert(x)).FindAll(x => x?.ExternalEdge2D is ISegmentable2D);
            if (faces_1.Count == 0 || faces_2.Count == 0)
            {
                return false;
            }

            double gap = faces_1.Min(x => faces_2.Min(y => Gap(x, y, tolerance)));
            if (gap <= tolerance)
            {
                return true;
            }

            if (openings.Exists(x => faces_1.Exists(y => Gap(x, y, tolerance) <= tolerance) && faces_2.Exists(y => Gap(x, y, tolerance) <= tolerance)))
            {
                return true;
            }

            BoundingBox3D boundingBox3D_1 = new BoundingBox3D(piece_1.Faces.ConvertAll(x => x.GetBoundingBox()));
            BoundingBox3D boundingBox3D_2 = new BoundingBox3D(piece_2.Faces.ConvertAll(x => x.GetBoundingBox()));
            foreach (Piece crossing in panelPieces)
            {
                if (crossing.Thickness < gap - tolerance || IsParallel(plane, crossing.Plane, cosAngle))
                {
                    continue;
                }

                // Before snapping, Panels end half a neighbour's thickness short of its mid-plane: the crossing Panel stops
                // half the pieces' thickness away from their plane.
                double reach = crossing.Thickness / 2 + tolerance;
                BoundingBox3D boundingBox3D = new BoundingBox3D(crossing.Faces.ConvertAll(x => x.GetBoundingBox(reach + piece_1.Thickness / 2)));
                if (!boundingBox3D.Intersect(boundingBox3D_1) || !boundingBox3D.Intersect(boundingBox3D_2))
                {
                    continue;
                }

                if (faces_1.Exists(x => LineDistance(x, plane, crossing.Plane) <= reach) && faces_2.Exists(x => LineDistance(x, plane, crossing.Plane) <= reach))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Distance between two faces on one plane: 0 when they overlap, otherwise between their outlines.
        /// </summary>
        private static double Gap(Face2D face2D_1, Face2D face2D_2, double tolerance)
        {
            ISegmentable2D segmentable2D_1 = (ISegmentable2D)face2D_1.ExternalEdge2D;
            ISegmentable2D segmentable2D_2 = (ISegmentable2D)face2D_2.ExternalEdge2D;
            double distance = segmentable2D_1.Distance(segmentable2D_2);
            if (distance <= tolerance)
            {
                return distance;
            }

            // Outlines apart: overlapping only if one face holds the other.
            return face2D_1.Inside(segmentable2D_2.GetPoints()[0], tolerance) || face2D_2.Inside(segmentable2D_1.GetPoints()[0], tolerance) ? 0 : distance;
        }

        /// <summary>
        /// Distance from a face on <paramref name="plane"/> to the line where <paramref name="plane_Crossing"/> cuts that
        /// plane: 0 when the line runs through it.
        /// </summary>
        private static double LineDistance(Face2D face2D, Plane plane, Plane plane_Crossing)
        {
            Vector3D normal = plane_Crossing.Normal.Unit;
            double length = (normal - plane.Normal.Unit * normal.DotProduct(plane.Normal.Unit)).Length;
            if (length <= 0)
            {
                return double.PositiveInfinity;
            }

            List<double> distances = ((ISegmentable2D)face2D.ExternalEdge2D).GetPoints().ConvertAll(x => normal.DotProduct(new Vector3D(plane_Crossing.Origin, plane.Convert(x))) / length);
            return distances.Min() <= 0 && distances.Max() >= 0 ? 0 : distances.Min(x => System.Math.Abs(x));
        }

        /// <summary>
        /// An element plane of a solid: a candidate piece (<see cref="ElementPlane"/> set) or a created Panel, with its
        /// faces on its mid-plane.
        /// </summary>
        private sealed class Piece
        {
            public Piece(int index, ElementPlane elementPlane, Plane plane, double thickness, List<Face3D> faces)
            {
                Index = index;
                ElementPlane = elementPlane;
                Plane = plane;
                Thickness = thickness;
                Faces = faces;
            }

            public int Index { get; }

            public ElementPlane ElementPlane { get; }

            public Plane Plane { get; }

            public double Thickness { get; }

            public List<Face3D> Faces { get; }
        }
    }
}
