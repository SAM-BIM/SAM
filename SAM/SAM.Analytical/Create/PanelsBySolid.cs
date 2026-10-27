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
        /// <param name="sourceIndex">Position of the solid in the caller's input list, used to number it in the
        /// diagnostics (<c>Solid #3 'Name'</c>); optional.</param>
        /// <returns>The host mid-surface panel(s) followed by any Air panels; empty when the element is skipped.</returns>
        /// <remarks>
        /// Diagnostics follow <see cref="Core.Query.DiagnosticMessage"/>: <c>Info</c> for panels created, <c>Warning</c>
        /// for a skipped solid. A skipped solid's reason contains <c>below minThickness</c>, <c>not panel-like</c> or
        /// <c>not a closed solid</c>.
        /// </remarks>
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
            double tolerance_Snap = Tolerance.MacroDistance,
            int? sourceIndex = null)
        {
            // Detailed keeps every line this overload has always returned. PanelsNone is new and structured-only.
            Core.Diagnostics diagnostics_Structured = new Core.Diagnostics(DiagnosticDetail.Detailed);
            List<Panel> result = PanelsBySolid(shell, diagnostics_Structured, apertureGeometries, minThickness, maxThicknessRatio, airForUnclaimedOpenings, sourceName, sourceId, sourceLayer, tolerance_Angle, tolerance_Distance, tolerance_Snap, sourceIndex);
            diagnostics = diagnostics_Structured.Where(x => x.Code != BySolidsDiagnosticCode.PanelsNone).Select(x => x.ToString()).ToList();
            return result;
        }

        /// <summary>
        /// <see cref="PanelsBySolid(Shell, out List{string}, IEnumerable{ISAMGeometry3D}, double, double, bool, string, string, string, double, double, double, int?)"/>
        /// with structured diagnostics: same geometry and parameters, same message text, but each record also carries its
        /// severity, <see cref="BySolidsDiagnosticCode"/>, <see cref="Diagnostic.SourceIndex"/> and, where there is one, the measured
        /// value and its limit. The result does not depend on the detail level of <paramref name="diagnostics"/>.
        /// <list type="bullet">
        /// <item><b>Warning</b>, at every level: a skipped solid, or a section region that could not be turned into a panel.</item>
        /// <item><b>Info, Standard</b>: panels created per element plane, and variable thickness.</item>
        /// <item><b>Info, Detailed</b>: the other face-pair planes that were ignored (their messages are built only when kept).</item>
        /// </list>
        /// </summary>
        /// <param name="shell">Closed solid of one building element, in metres.</param>
        /// <param name="diagnostics">Collection the records are appended to; its <see cref="Core.Diagnostics.Detail"/> decides which Info records are kept. Null collects nothing.</param>
        /// <remarks>All other parameters are those of the <c>out List&lt;string&gt;</c> overload.</remarks>
        public static List<Panel> PanelsBySolid(
            this Shell shell,
            Core.Diagnostics diagnostics,
            IEnumerable<ISAMGeometry3D> apertureGeometries = null,
            double minThickness = 0.08,
            double maxThicknessRatio = 0.2,
            bool airForUnclaimedOpenings = true,
            string sourceName = null,
            string sourceId = null,
            string sourceLayer = null,
            double tolerance_Angle = Tolerance.Angle,
            double tolerance_Distance = Tolerance.Distance,
            double tolerance_Snap = Tolerance.MacroDistance,
            int? sourceIndex = null)
        {
            return PanelsBySolid(shell, diagnostics, apertureGeometries, minThickness, maxThicknessRatio, airForUnclaimedOpenings, sourceName, sourceId, sourceLayer, tolerance_Angle, tolerance_Distance, tolerance_Snap, sourceIndex, out List<ElementPlane> _, out List<ElementPlane> _);
        }

        /// <summary>
        /// The structured overload, also returning the element planes rejected as not panel-like and the planes dropped
        /// as cross-cuts of a plane taken (for <see cref="PanelsBySolids"/>).
        /// </summary>
        private static List<Panel> PanelsBySolid(Shell shell, Core.Diagnostics diagnostics, IEnumerable<ISAMGeometry3D> apertureGeometries, double minThickness, double maxThicknessRatio, bool airForUnclaimedOpenings, string sourceName, string sourceId, string sourceLayer, double tolerance_Angle, double tolerance_Distance, double tolerance_Snap, int? sourceIndex, out List<ElementPlane> rejectedNotPanelLike, out List<ElementPlane> crossCuts)
        {
            diagnostics = diagnostics ?? new Core.Diagnostics();
            List<Panel> result = new List<Panel>();
            string subject = SolidSubject(sourceName, sourceIndex);
            rejectedNotPanelLike = new List<ElementPlane>();
            crossCuts = new List<ElementPlane>();

            List<Face3D> face3Ds = shell?.Face3Ds?.Where(x => x != null && x.GetPlane() != null && !double.IsNaN(x.GetArea())).ToList();
            if (face3Ds == null || face3Ds.Count < 4)
            {
                AddWarning(diagnostics, BySolidsDiagnosticCode.PanelsNotClosed, subject, sourceIndex, "Skipped. It is not a closed solid.");
                return result;
            }

            List<ElementPlane> elementPlanes = ElementPlanes(shell, face3Ds, minThickness, maxThicknessRatio, tolerance_Angle, tolerance_Distance, tolerance_Snap, out List<ElementPlane> rejected, out crossCuts);
            List<ElementPlane> thin = rejected.FindAll(x => x.TooThin);
            List<ElementPlane> notPanelLike = rejected.FindAll(x => !x.TooThin);
            rejectedNotPanelLike = notPanelLike;
            if (elementPlanes.Count == 0)
            {
                // The reason given is that of the element's main plane (the largest overlap): for a 0.15 m wall below
                // minThickness that is its two sides, not its ends or its top and bottom.
                ElementPlane best = rejected.FirstOrDefault();
                if (best == null)
                {
                    AddWarning(diagnostics, BySolidsDiagnosticCode.PanelsNoFacePair, subject, sourceIndex, "Skipped. No pair of opposite parallel faces with material between them was found.");
                }
                else if (best.TooThin)
                {
                    AddWarning(diagnostics, BySolidsDiagnosticCode.PanelsBelowMinThickness, subject, sourceIndex, FormatInvariant("Skipped as below minThickness: its thickness, {0:0.###} m, is less than minThickness ({1:0.###} m). If it is a wall, floor or roof, lower minThickness below {0:0.###} m.", best.Thickness, minThickness), best.Thickness, minThickness, "m");
                }
                else
                {
                    AddWarning(diagnostics, BySolidsDiagnosticCode.PanelsNotPanelLike, subject, sourceIndex, FormatInvariant("Skipped as not panel-like: thickness {0:0.###} m over smallest extent {1:0.###} m gives a ratio of {2:0.###}, above maxThicknessRatio ({3:0.###}). A maxThicknessRatio above {2:0.###} would keep it.", best.Thickness, best.MinExtent, best.Slenderness, maxThicknessRatio), best.Slenderness, maxThicknessRatio, "-");
                }

                return result;
            }

            if (notPanelLike.Count != 0)
            {
                diagnostics.Add(DiagnosticSeverity.Info, DiagnosticDetail.Detailed, subject, () => FormatInvariant("Ignored {0} that {1} not panel-like (thickness / smallest extent: {2}).", Core.Query.Plural(notPanelLike.Count, "other face-pair plane"), notPanelLike.Count == 1 ? "is" : "are", string.Join(", ", notPanelLike.Take(5).Select(x => FormatInvariant("{0:0.###} / {1:0.###} m", x.Thickness, x.MinExtent)))), BySolidsDiagnosticCode.PanelsIgnoredNotPanelLike, sourceIndex);
            }

            if (thin.Count != 0)
            {
                diagnostics.Add(DiagnosticSeverity.Info, DiagnosticDetail.Detailed, subject, () => FormatInvariant("Ignored {0} below minThickness ({1:0.###} m): {2} m.", Core.Query.Plural(thin.Count, "other face-pair plane"), minThickness, string.Join(", ", thin.Take(5).Select(x => FormatInvariant("{0:0.###}", x.Thickness)))), BySolidsDiagnosticCode.PanelsIgnoredBelowMinThickness, sourceIndex, null, minThickness, "m");
            }

            bool warned = false;
            double minOpeningArea = System.Math.Max(tolerance_Snap * tolerance_Snap, tolerance_Distance);
            for (int k = 0; k < elementPlanes.Count; k++)
            {
                ElementPlane elementPlane = elementPlanes[k];
                string subject_Plane = elementPlanes.Count == 1 ? subject : FormatInvariant("{0}, plane {1} of {2} (normal {3:0.###}, {4:0.###}, {5:0.###})", subject, k + 1, elementPlanes.Count, elementPlane.MidPlane.Normal.X, elementPlane.MidPlane.Normal.Y, elementPlane.MidPlane.Normal.Z);
                result.AddRange(PanelsByElementPlane(shell, elementPlane, diagnostics, subject_Plane, sourceIndex, apertureGeometries, airForUnclaimedOpenings, sourceName, sourceId, sourceLayer, minOpeningArea, tolerance_Angle, tolerance_Distance, tolerance_Snap, out bool warned_Plane));
                warned |= warned_Plane;
            }

            // A solid that gave nothing must say why at every detail level; the per-plane Info line is only Standard.
            if (result.Count == 0 && !warned)
            {
                AddWarning(diagnostics, BySolidsDiagnosticCode.PanelsNone, subject, sourceIndex, "Skipped. No panel could be created from its element planes.");
            }

            return result;
        }

        /// <summary>
        /// Deterministic diagnostics subject of a source solid: <c>Solid #3 'Name'</c>, <c>Solid #3</c>,
        /// <c>Solid 'Name'</c> or <c>Solid</c>. A name equal to the index label (<c>#3</c>) is not repeated.
        /// </summary>
        private static string SolidSubject(string sourceName, int? sourceIndex)
        {
            string index = sourceIndex.HasValue ? FormatInvariant("#{0}", sourceIndex.Value) : null;
            bool named = !string.IsNullOrWhiteSpace(sourceName) && sourceName != index;
            if (index == null)
            {
                return named ? string.Format("Solid '{0}'", sourceName) : "Solid";
            }

            return named ? string.Format("Solid {0} '{1}'", index, sourceName) : string.Format("Solid {0}", index);
        }

        private static void AddWarning(Core.Diagnostics diagnostics, string code, string subject, int? sourceIndex, string message, double? value = null, double? limit = null, string unit = null)
        {
            diagnostics.Add(new Diagnostic(DiagnosticSeverity.Warning, DiagnosticDetail.Summary, subject, message, code, sourceIndex, value, limit, unit));
        }

        private static string FormatInvariant(string format, params object[] args)
        {
            return string.Format(System.Globalization.CultureInfo.InvariantCulture, format, args);
        }

        /// <summary>
        /// Panels of one element plane of <paramref name="shell"/>: the material regions of the section on the mid-plane
        /// that the plane's own thickness pairs support, with aperture-claimed openings filled and unclaimed holes
        /// left open (optionally as Air panels).
        /// </summary>
        private static List<Panel> PanelsByElementPlane(Shell shell, ElementPlane elementPlane, Core.Diagnostics diagnostics, string label, int? sourceIndex, IEnumerable<ISAMGeometry3D> apertureGeometries, bool airForUnclaimedOpenings, string sourceName, string sourceId, string sourceLayer, double minOpeningArea, double tolerance_Angle, double tolerance_Distance, double tolerance_Snap, out bool warned)
        {
            warned = false;
            List<Panel> result = new List<Panel>();
            Plane midPlane = elementPlane.MidPlane;
            double thickness = elementPlane.Thickness;

            if (elementPlane.OtherThicknesses.Count != 0)
            {
                diagnostics.Add(DiagnosticSeverity.Info, DiagnosticDetail.Standard, label, () => FormatInvariant("Variable thickness: parts of it are also {0} m thick. The PhysicalThickness stored is the main one, {1:0.###} m (the largest pair of opposite faces).", string.Join(", ", elementPlane.OtherThicknesses.ConvertAll(x => x.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture))), thickness), BySolidsDiagnosticCode.PanelsVariableThickness, sourceIndex, thickness, null, "m");
            }

            List<Face3D> sections = shell.Section(midPlane, true, tolerance_Angle, tolerance_Distance, tolerance_Snap);
            if (sections == null || sections.Count == 0)
            {
                AddWarning(diagnostics, BySolidsDiagnosticCode.PanelsMidPlaneMisses, label, sourceIndex, "Skipped. The mid-plane does not cut the solid.");
                warned = true;
                return result;
            }

            List<Face2D> apertureFootprints = ApertureFootprints(apertureGeometries, midPlane, thickness, tolerance_Snap);
            List<Face2D> pairFootprints = elementPlane.Footprints.ConvertAll(x => midPlane.Convert(x)).FindAll(x => x != null);

            int filledHoles = 0, openHoles = 0, airHoles = 0, filledNotches = 0, keptNotches = 0, crossSections = 0, failedRegions = 0;
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
                    openHoles++;
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
                    failedRegions++;
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

            // One Warning per element plane, however many regions failed.
            if (failedRegions != 0)
            {
                AddWarning(diagnostics, BySolidsDiagnosticCode.PanelsRegionFailed, label, sourceIndex, failedRegions == 1 ? "A section region could not be turned into a panel." : FormatInvariant("{0} section regions could not be turned into panels.", failedRegions), failedRegions);
                warned = true;
            }

            diagnostics.Add(DiagnosticSeverity.Info, DiagnosticDetail.Standard, label, () =>
            {
                // Only what happened is listed: no sentence for a count of zero.
                List<string> sentences = new List<string> { FormatInvariant("Created {0}, {1:0.###} m thick.", Core.Query.Plural(result.Count(x => x.PanelType != PanelType.Air), "panel"), thickness) };
                if (filledHoles != 0 || filledNotches != 0)
                {
                    sentences.Add(FormatInvariant("Filled {0} for apertures.", string.Join(" and ", new[] { filledHoles == 0 ? null : Core.Query.Plural(filledHoles, "hole"), filledNotches == 0 ? null : Core.Query.Plural(filledNotches, "notch", "notches") }.Where(x => x != null))));
                }

                if (openHoles != 0)
                {
                    sentences.Add(FormatInvariant("Left {0} open in the host panel{1}.", Core.Query.Plural(openHoles, "unclaimed hole"), airHoles == 0 ? string.Empty : FormatInvariant(", filled by {0}", Core.Query.Plural(airHoles, "Air Panel"))));
                }

                if (keptNotches != 0)
                {
                    sentences.Add(FormatInvariant("Kept {0} as the outline.", Core.Query.Plural(keptNotches, "unclaimed notch", "unclaimed notches")));
                }

                if (crossSections != 0)
                {
                    sentences.Add(FormatInvariant("Left {0} of the solid's other elements to their own planes.", Core.Query.Plural(crossSections, "cross-section")));
                }

                return string.Join(" ", sentences);
            }, BySolidsDiagnosticCode.PanelsCreated, sourceIndex, thickness, null, "m");

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
        /// an element of its own. A pair's material is tested at the point of its overlap farthest from the overlap's
        /// edges, so a thin element at the edge of a frame-shaped overlap (a jamb) does not take the whole pair.</item>
        /// <item><b>Panel-like:</b> a plane whose thickness exceeds <paramref name="maxThicknessRatio"/> of its smallest
        /// in-plane extent is returned in <paramref name="rejected"/>.</item>
        /// <item><b>One element, several thicknesses:</b> a parallel plane whose slab contains, or lies within, the
        /// slab of a plane with a larger overlap is part of that element (a stepped wall, or the thinner wall behind a
        /// window niche). Its thickness is reported, not turned into a second panel.</item>
        /// </list>
        /// Mid-plane normals are oriented so that <see cref="Query.PanelType(Vector3D, double)"/> classifies them
        /// independently of face order: vertical planes are walls, horizontal planes face down (floor or slab) and
        /// sloped planes face up (roof).
        /// <para>
        /// A plane all of whose pairs are cross-cuts is returned, with those pairs, in <paramref name="crossCuts"/>: it
        /// gives nothing here, but <see cref="PanelsBySolids"/> may find that the plane taken is a slice of it.
        /// </para>
        /// </summary>
        private static List<ElementPlane> ElementPlanes(Shell shell, List<Face3D> face3Ds, double minThickness, double maxThicknessRatio, double tolerance_Angle, double tolerance_Distance, double tolerance_Snap, out List<ElementPlane> rejected, out List<ElementPlane> crossCuts)
        {
            rejected = new List<ElementPlane>();
            crossCuts = new List<ElementPlane>();
            double cosAngle = System.Math.Cos(tolerance_Angle);

            // Faces closer than 10 snap tolerances (1 cm by default) are steps and reveals, not an element's sides.
            double minPairDistance = System.Math.Max(System.Math.Min(minThickness, 10 * tolerance_Snap), tolerance_Snap);

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
                    // Pairs thinner than minThickness are kept here and set aside after ranking: they still claim their
                    // material, so a 0.15 m wall below minThickness is reported as too thin instead of its ends or its
                    // top and bottom being taken for a 2.75 m thick element.
                    double distance = System.Math.Abs(offset);
                    if (distance < minPairDistance - tolerance_Distance)
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

                    Point3D point3D = plane_i.Convert(DeepestPoint(overlap.OrderByDescending(x => x.GetArea()).First(), tolerance_Distance, tolerance_Snap));
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
            // Planes below minThickness own their material as well (claimants) but are returned as rejected.
            List<ElementPlane> accepted = new List<ElementPlane>();
            List<ElementPlane> claimants = new List<ElementPlane>();
            foreach (ElementPlane elementPlane in elementPlanes.OrderBy(x => x.Thickness / x.BandWidth()).ThenByDescending(x => x.Overlap))
            {
                List<ThicknessPair> pairs_Plane = new List<ThicknessPair>(elementPlane.Pairs);
                elementPlane.Pairs.RemoveAll(pair => claimants.Exists(x => !IsParallel(x.MidPlane, pair.MidPlane, cosAngle) && x.Owns(pair.Sample, tolerance_Distance)));
                if (elementPlane.Pairs.Count == 0)
                {
                    ElementPlane crossCut = new ElementPlane(pairs_Plane[0]);
                    crossCut.Pairs.AddRange(pairs_Plane.Skip(1));
                    crossCut.UpdateMinExtent();
                    crossCuts.Add(crossCut);
                    continue;
                }

                elementPlane.UpdateMinExtent();
                if (elementPlane.Slenderness > maxThicknessRatio)
                {
                    rejected.Add(elementPlane);
                    continue;
                }

                // A panel-like plane below minThickness is a real element that is too thin (a 0.15 m wall when
                // minThickness is 0.25 m): it keeps its material from cross-cuts, but gives no panel.
                if (elementPlane.Thickness < minThickness - tolerance_Distance)
                {
                    elementPlane.TooThin = true;
                    claimants.Add(elementPlane);
                    rejected.Add(elementPlane);
                    continue;
                }

                claimants.Add(elementPlane);
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

        /// <summary>
        /// The point of <paramref name="face2D"/> farthest from its edges (the centre of its largest inscribed circle,
        /// to <paramref name="tolerance_Snap"/>): the point that stands for a pair's material. In a frame-shaped overlap
        /// (a wall around a large opening) it lies in the head, not in a 13 mm jamb that a thinner element owns. Falls
        /// back to any internal point.
        /// </summary>
        private static Point2D DeepestPoint(Face2D face2D, double tolerance_Distance, double tolerance_Snap)
        {
            Point2D result = null;
            try
            {
                NetTopologySuite.Geometries.Point center = NetTopologySuite.Algorithm.Construct.MaximumInscribedCircle.GetCenter(face2D.ToNTS(), tolerance_Snap);
                result = center == null || center.IsEmpty ? null : new Point2D(center.X, center.Y);
            }
            catch (System.Exception)
            {
                result = null;
            }

            return result != null && face2D.Inside(result, tolerance_Distance) ? result : face2D.GetInternalPoint2D(tolerance_Distance);
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
            /// True when the plane was set aside because it is thinner than minThickness.
            /// </summary>
            public bool TooThin { get; set; }

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
