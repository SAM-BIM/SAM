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
        /// Converts window and door solids (frames, frame members, glass, door leaves) into one SAM
        /// <see cref="Analytical.Aperture"/> per window or door assembly. Geometry must already be in metres; no unit
        /// conversion is performed. The apertures are not hosted: add them with
        /// <see cref="Modify.AddAperturesByApertures"/> (GH <c>SAMAnalytical.AddAperturesByAperture</c>).
        /// <list type="number">
        /// <item><b>Assemblies:</b> members sharing a <see cref="ApertureSolid.GroupKey"/> form one assembly. Members
        /// whose surfaces are within <paramref name="maxGap"/> of each other (a glass pane inside its frame, a leaf in
        /// its door frame, frame members meeting at a corner) join the same assembly; members with different group keys
        /// are never merged. Each frame member is therefore not assumed to be an aperture of its own.</item>
        /// <item><b>Type:</b> an assembly with any Door member is a Door (e.g. a door frame given as window geometry
        /// with its leaf), otherwise a Window.</item>
        /// <item><b>Plane:</b> the member face normal along which the whole assembly is thinnest; the aperture lies
        /// on the plane half-way through that depth (the mid-surface).</item>
        /// <item><b>Boundary:</b> the outline of the union of all members projected onto that plane, holes dropped:
        /// the full opening including the frame, with its true shape and dimensions. Members separated by gaps up to
        /// <paramref name="maxGap"/> are closed over.</item>
        /// </list>
        /// Assemblies deeper than <paramref name="maxDepthRatio"/> of their smallest in-plane extent (e.g. a corner
        /// window spanning two walls) are not planar and are skipped with a reason. Every aperture is stamped with
        /// <see cref="ApertureSourceParameter"/> values listing its members.
        /// </summary>
        /// <param name="apertureSolids">Window and door members, each with its explicit aperture type and source.</param>
        /// <param name="diagnostics">One line per aperture and per skipped member or assembly.</param>
        /// <param name="excluded">Indexes of members that did not contribute to any aperture.</param>
        /// <param name="apertureConstructions">Optional constructions; each aperture takes the first matching its
        /// type. Without one, the default external construction for the type and orientation is used (as
        /// <see cref="Apertures"/> does); internal doors can be re-defaulted by host after hosting with
        /// <see cref="Modify.SetDefaultApertureConstruction"/>.</param>
        /// <param name="maxGap">Members closer than this [m] belong to the same assembly.</param>
        /// <param name="maxDepthRatio">Maximum assembly depth over its smallest in-plane extent.</param>
        /// <param name="tolerance_Angle">Angle tolerance [rad].</param>
        /// <param name="tolerance_Distance">Distance tolerance [m].</param>
        /// <returns>One aperture per assembly, in order of each assembly's first member.</returns>
        public static List<Aperture> AperturesBySolids(
            this IEnumerable<ApertureSolid> apertureSolids,
            out List<string> diagnostics,
            out List<int> excluded,
            IEnumerable<ApertureConstruction> apertureConstructions = null,
            double maxGap = 0.01,
            double maxDepthRatio = 0.5,
            double tolerance_Angle = Tolerance.Angle,
            double tolerance_Distance = Tolerance.Distance)
        {
            diagnostics = new List<string>();
            excluded = new List<int>();
            List<Aperture> result = new List<Aperture>();

            List<ApertureSolid> solids = apertureSolids?.ToList();
            if (solids == null || solids.Count == 0)
            {
                return result;
            }

            double minArea = Tolerance.MacroDistance;
            maxGap = System.Math.Max(maxGap, tolerance_Distance);

            // Members: faces, vertices and bounding box of each usable solid.
            List<ApertureMember> members = new List<ApertureMember>();
            for (int i = 0; i < solids.Count; i++)
            {
                ApertureMember member = ApertureMember.Create(i, solids[i], tolerance_Distance);
                if (member == null)
                {
                    excluded.Add(i);
                    diagnostics.Add(Core.Query.DiagnosticMessage(LogRecordType.Warning, "Solid " + MemberLabel(solids[i], i), "Skipped. It is not a closed solid or a planar face."));
                    continue;
                }

                members.Add(member);
            }

            // Assemblies: explicit group keys first, then proximity; never merge two different keys.
            int[] parents = Enumerable.Range(0, members.Count).ToArray();
            string[] keys = members.ConvertAll(x => string.IsNullOrWhiteSpace(x.Solid.GroupKey) ? null : x.Solid.GroupKey).ToArray();

            int Find(int i)
            {
                while (parents[i] != i)
                {
                    parents[i] = parents[parents[i]];
                    i = parents[i];
                }

                return i;
            }

            bool Union(int i, int j)
            {
                int root_i = Find(i), root_j = Find(j);
                if (root_i == root_j)
                {
                    return true;
                }

                if (keys[root_i] != null && keys[root_j] != null && keys[root_i] != keys[root_j])
                {
                    return false;
                }

                int root = System.Math.Min(root_i, root_j), other = System.Math.Max(root_i, root_j);
                parents[other] = root;
                keys[root] = keys[root] ?? keys[other];
                return true;
            }

            for (int i = 0; i < members.Count; i++)
            {
                for (int j = i + 1; j < members.Count; j++)
                {
                    if (keys[i] != null && keys[i] == keys[j])
                    {
                        Union(i, j);
                    }
                }
            }

            for (int i = 0; i < members.Count; i++)
            {
                for (int j = i + 1; j < members.Count; j++)
                {
                    if (Find(i) == Find(j) || !members[i].BoundingBox3D.InRange(members[j].BoundingBox3D, maxGap))
                    {
                        continue;
                    }

                    if (members[i].Near(members[j], maxGap, tolerance_Distance) || members[j].Near(members[i], maxGap, tolerance_Distance))
                    {
                        Union(i, j);
                    }
                }
            }

            List<List<ApertureMember>> assemblies = members
                .Select((x, i) => new { Member = x, Root = Find(i) })
                .GroupBy(x => x.Root)
                .OrderBy(x => x.Key)
                .Select(x => x.Select(y => y.Member).ToList())
                .ToList();

            List<ApertureConstruction> apertureConstructions_Input = apertureConstructions?.Where(x => x != null).ToList();
            foreach (List<ApertureMember> assembly in assemblies)
            {
                string label = (assembly.Count == 1 ? "Solid " : "Solids ") + string.Join(" + ", assembly.ConvertAll(x => MemberLabel(x.Solid, x.Index)));
                ApertureType apertureType = assembly.Any(x => x.Solid.ApertureType == ApertureType.Door) ? ApertureType.Door : ApertureType.Window;

                List<Aperture> apertures = AperturesByAssembly(assembly, apertureType, apertureConstructions_Input, maxGap, maxDepthRatio, minArea, tolerance_Angle, tolerance_Distance, out string message);
                if (apertures == null || apertures.Count == 0)
                {
                    excluded.AddRange(assembly.ConvertAll(x => x.Index));
                    diagnostics.Add(Core.Query.DiagnosticMessage(LogRecordType.Warning, label, string.Format("Skipped. {0}.", message)));
                    continue;
                }

                bool mixed = assembly.Any(x => x.Solid.ApertureType == ApertureType.Window) && apertureType == ApertureType.Door;
                diagnostics.Add(Core.Query.DiagnosticMessage(LogRecordType.Message, label, string.Format("Created {0} from {1}{2}: {3}.", Core.Query.Plural(apertures.Count, apertureType.ToString()), Core.Query.Plural(assembly.Count, "solid"), mixed ? " (window members joined a door)" : string.Empty, message)));
                result.AddRange(apertures);
            }

            excluded.Sort();
            return result;
        }

        private static List<Aperture> AperturesByAssembly(List<ApertureMember> assembly, ApertureType apertureType, List<ApertureConstruction> apertureConstructions, double maxGap, double maxDepthRatio, double minArea, double tolerance_Angle, double tolerance_Distance, out string message)
        {
            message = null;

            // Plane: the candidate face normal (largest faces first) along which the assembly is thinnest.
            List<Point3D> point3Ds = assembly.SelectMany(x => x.Point3Ds).ToList();
            double cosAngle = System.Math.Cos(tolerance_Angle);
            Plane plane_Best = null;
            double depth = double.MaxValue, depthMin = 0;
            foreach (Face3D face3D in assembly.SelectMany(x => x.Face3Ds).OrderByDescending(x => x.GetArea()))
            {
                Plane plane = face3D.GetPlane();
                if (plane_Best != null && System.Math.Abs(plane.Normal.DotProduct(plane_Best.Normal)) >= cosAngle)
                {
                    continue;
                }

                Vector3D normal = plane.Normal;
                List<double> offsets = point3Ds.ConvertAll(x => normal.DotProduct(new Vector3D(plane.Origin, x)));
                double depth_Plane = offsets.Max() - offsets.Min();
                if (depth_Plane < depth - tolerance_Distance)
                {
                    depth = depth_Plane;
                    depthMin = offsets.Min();
                    plane_Best = plane;
                }
            }

            if (plane_Best == null)
            {
                message = "It has no planar faces";
                return null;
            }

            Vector3D normal_Best = plane_Best.Normal;
            double shift = depthMin + depth / 2;
            Point3D origin = plane_Best.Origin;
            Plane midPlane = new Plane(plane_Best, new Point3D(origin.X + normal_Best.X * shift, origin.Y + normal_Best.Y * shift, origin.Z + normal_Best.Z * shift));

            // Boundary: union of every member face projected onto the mid-plane (faces edge-on to it add nothing).
            double sinAngle = System.Math.Sin(tolerance_Angle);
            List<Face2D> face2Ds = new List<Face2D>();
            foreach (Face3D face3D in assembly.SelectMany(x => x.Face3Ds))
            {
                if (System.Math.Abs(face3D.GetPlane().Normal.DotProduct(normal_Best)) <= sinAngle)
                {
                    continue;
                }

                Face2D face2D = midPlane.Convert(midPlane.Project(face3D));
                if (face2D != null && face2D.GetArea() > tolerance_Distance)
                {
                    face2Ds.Add(face2D);
                }
            }

            List<Face2D> outlines = Outlines(face2Ds, tolerance_Distance);
            if (outlines.Count > 1)
            {
                // Close the gaps between members (at most maxGap) by offsetting out and back in.
                double offset = maxGap / 2 * 1.01 + tolerance_Distance;
                List<Face2D> grown = outlines
                    .SelectMany(x => Geometry.Planar.Create.Polygon2D(x.ExternalEdge2D)?.Offset(offset, tolerance_Distance) ?? new List<Polygon2D>())
                    .Select(x => Geometry.Planar.Create.Face2D(x, null))
                    .Where(x => x != null)
                    .ToList();

                List<Face2D> closed = Outlines(grown, tolerance_Distance)
                    .SelectMany(x => Geometry.Planar.Create.Polygon2D(x.ExternalEdge2D)?.Offset(-offset, tolerance_Distance) ?? new List<Polygon2D>())
                    .Select(x => Geometry.Planar.Create.Face2D(x, null))
                    .Where(x => x != null)
                    .ToList();

                if (closed.Count != 0)
                {
                    outlines = Outlines(closed, tolerance_Distance);
                }
            }

            outlines.RemoveAll(x => x.GetArea() <= minArea);
            if (outlines.Count == 0)
            {
                message = "It has no opening area on the assembly plane";
                return null;
            }

            List<Point2D> point2Ds = outlines.SelectMany(x => (x.ExternalEdge2D as ISegmentable2D)?.GetPoints() ?? new List<Point2D>()).ToList();
            BoundingBox2D boundingBox2D = new BoundingBox2D(point2Ds);
            double minExtent = System.Math.Min(boundingBox2D.Width, boundingBox2D.Height);
            if (minExtent <= 0 || depth / minExtent > maxDepthRatio)
            {
                message = string.Format(System.Globalization.CultureInfo.InvariantCulture, "Not planar: depth {0:0.###} m over smallest extent {1:0.###} m exceeds maxDepthRatio ({2:0.###}). Put separate assemblies in different Rhino groups", depth, minExtent, maxDepthRatio);
                return null;
            }

            ApertureConstruction apertureConstruction = apertureConstructions?.Find(x => x.ApertureType == apertureType);
            if (apertureConstruction == null)
            {
                apertureConstruction = Query.DefaultApertureConstruction(normal_Best.PanelType(), apertureType);
            }

            if (apertureConstruction == null)
            {
                message = string.Format("No {0} aperture construction is available", apertureType);
                return null;
            }

            string sourceName = string.Join("; ", assembly.ConvertAll(x => x.Solid.SourceName ?? string.Empty));
            string sourceId = string.Join("; ", assembly.ConvertAll(x => x.Solid.SourceId ?? string.Empty));
            string sourceLayer = string.Join("; ", assembly.Select(x => x.Solid.SourceLayer).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct());

            List<Aperture> result = new List<Aperture>();
            List<string> sizes = new List<string>();
            foreach (Face2D outline in outlines.OrderByDescending(x => x.GetArea()))
            {
                Face3D face3D = midPlane.Convert(outline);
                Aperture aperture = Aperture(apertureConstruction, face3D);
                if (aperture == null)
                {
                    continue;
                }

                if (sourceName.Trim(';', ' ').Length != 0)
                {
                    aperture.SetValue(ApertureSourceParameter.SourceName, sourceName);
                }

                if (sourceId.Trim(';', ' ').Length != 0)
                {
                    aperture.SetValue(ApertureSourceParameter.SourceId, sourceId);
                }

                if (sourceLayer.Length != 0)
                {
                    aperture.SetValue(ApertureSourceParameter.SourceLayer, sourceLayer);
                }

                aperture.SetValue(ApertureSourceParameter.SourceThickness, depth);

                BoundingBox2D boundingBox2D_Outline = new BoundingBox2D((outline.ExternalEdge2D as ISegmentable2D)?.GetPoints() ?? point2Ds);
                sizes.Add(string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0:0.###} x {1:0.###} m ({2:0.###} m²)", boundingBox2D_Outline.Width, boundingBox2D_Outline.Height, outline.GetArea()));
                result.Add(aperture);
            }

            message = string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}, depth {1:0.###} m{2}", string.Join(", ", sizes), depth, result.Count > 1 ? string.Format(" ({0} separate openings: members further apart than maxGap)", result.Count) : string.Empty);
            return result;
        }

        /// <summary>
        /// Outer outlines (holes dropped) of the union of the given faces.
        /// </summary>
        private static List<Face2D> Outlines(List<Face2D> face2Ds, double tolerance_Distance)
        {
            List<Face2D> union = face2Ds.Count == 0 ? new List<Face2D>() : Geometry.Planar.Query.Union(face2Ds, tolerance_Distance) ?? new List<Face2D>();
            return union
                .Where(x => x?.ExternalEdge2D != null)
                .Select(x => Geometry.Planar.Create.Face2D(x.ExternalEdge2D, null))
                .Where(x => x != null)
                .ToList();
        }

        private static string MemberLabel(ApertureSolid apertureSolid, int index)
        {
            return string.IsNullOrWhiteSpace(apertureSolid?.SourceName) ? string.Format("#{0}", index) : string.Format("#{0} '{1}'", index, apertureSolid.SourceName);
        }

        private sealed class ApertureMember
        {
            public int Index { get; private set; }

            public ApertureSolid Solid { get; private set; }

            public Shell Shell { get; private set; }

            public List<Face3D> Face3Ds { get; private set; }

            public List<Point3D> Point3Ds { get; private set; }

            public BoundingBox3D BoundingBox3D { get; private set; }

            public static ApertureMember Create(int index, ApertureSolid apertureSolid, double tolerance_Distance)
            {
                Shell shell = apertureSolid?.Geometry as Shell;
                List<Face3D> face3Ds = shell != null ? shell.Face3Ds : apertureSolid?.Geometry is Face3D face3D ? new List<Face3D> { face3D } : null;
                face3Ds = face3Ds?.Where(x => x != null && x.GetPlane() != null && x.GetArea() > tolerance_Distance).ToList();
                if (face3Ds == null || face3Ds.Count == 0 || (shell != null && face3Ds.Count < 4))
                {
                    return null;
                }

                List<Point3D> point3Ds = face3Ds
                    .SelectMany(x => x.GetEdge3Ds() ?? new List<IClosedPlanar3D>())
                    .SelectMany(x => (x as ISegmentable3D)?.GetPoints() ?? new List<Point3D>())
                    .ToList();

                if (point3Ds.Count == 0)
                {
                    return null;
                }

                return new ApertureMember
                {
                    Index = index,
                    Solid = apertureSolid,
                    Shell = shell,
                    Face3Ds = face3Ds,
                    Point3Ds = point3Ds,
                    BoundingBox3D = new BoundingBox3D(point3Ds),
                };
            }

            /// <summary>
            /// True when a vertex of this member lies inside the other solid or within maxGap of its surface.
            /// </summary>
            public bool Near(ApertureMember other, double maxGap, double tolerance_Distance)
            {
                BoundingBox3D boundingBox3D = new BoundingBox3D(other.BoundingBox3D, maxGap);
                foreach (Point3D point3D in Point3Ds)
                {
                    if (!boundingBox3D.Inside(point3D, true, tolerance_Distance))
                    {
                        continue;
                    }

                    if (other.Face3Ds.Any(x => x.Distance(point3D, tolerance_Distance) <= maxGap))
                    {
                        return true;
                    }

                    if (other.Shell != null && other.Shell.Inside(point3D, Tolerance.MacroDistance, tolerance_Distance))
                    {
                        return true;
                    }
                }

                return false;
            }
        }
    }
}
