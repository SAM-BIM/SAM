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
    public static partial class Query
    {
        /// <summary>
        /// Largest cosine deficit [1 - cos] of the angle between a record's normal (and Y axis) and the panel's for the frames to
        /// count as equal: the frame must be the one the panel was extracted with, not merely a similar one.
        /// </summary>
        private const double FrameDirectionTolerance = 1e-12;

        /// <summary>
        /// The panel's <see cref="Panel.SourceEvidence"/> when it still describes the panel, else null (no evidence).
        /// <para>
        /// The record is accepted only if all hold: (a) its frame equals the panel's current frame (origin within
        /// <see cref="Tolerance.MicroDistance"/>, normal and Y axis in the same direction), so a move, rotation, scaling or
        /// flip is detected; (b) the panel's current footprint equals <see cref="SourceEvidence.BoundFootprint"/>: the symmetric
        /// difference is at most <paramref name="tolerance_Distance"/> times the perimeter of the bound footprint, computed with
        /// <see cref="DefiniteIntersection(IEnumerable{Face2D}, IEnumerable{Face2D}, double)"/>, so an edit on the same plane (trim, extension, merge, split) is detected even though
        /// it keeps the frame; (c) the record is complete and its thickness is positive. A result that cannot be decided
        /// (kernel failure) is no evidence.
        /// </para>
        /// <para>
        /// This does not check what only a consumer can: that the panel's plane has not been moved since the record was read
        /// (inside a solver stage) and that the panel stands for one source element.
        /// </para>
        /// </summary>
        /// <param name="panel">The panel.</param>
        /// <param name="tolerance_Distance">Distance tolerance [m].</param>
        /// <returns>The record, or null.</returns>
        public static SourceEvidence ValidSourceEvidence(this Panel panel, double tolerance_Distance = Tolerance.Distance)
        {
            return ValidSourceEvidence(panel, out string _, tolerance_Distance);
        }

        /// <summary>
        /// <see cref="ValidSourceEvidence(Panel, double)"/> that also says why a record was not accepted.
        /// </summary>
        /// <param name="panel">The panel.</param>
        /// <param name="reason">Why there is no evidence; null when the record is valid.</param>
        /// <param name="tolerance_Distance">Distance tolerance [m].</param>
        /// <returns>The record, or null.</returns>
        public static SourceEvidence ValidSourceEvidence(this Panel panel, out string reason, double tolerance_Distance = Tolerance.Distance)
        {
            reason = null;

            SourceEvidence sourceEvidence = panel?.SourceEvidence;
            if (sourceEvidence == null)
            {
                reason = "the panel has no source evidence";
                return null;
            }

            if (!sourceEvidence.IsComplete)
            {
                reason = "the record is incomplete or its thickness is not positive";
                return null;
            }

            Plane plane = panel.Plane;
            Face3D face3D = panel.GetFace3D();
            if (plane == null || face3D == null)
            {
                reason = "the panel has no face";
                return null;
            }

            if (!SameFrame(sourceEvidence.Plane, plane))
            {
                reason = "the frame differs from the frame the record was made on";
                return null;
            }

            Face2D face2D = sourceEvidence.Plane.Convert(face3D);
            if (face2D == null)
            {
                reason = "the panel's footprint could not be expressed in the record's frame";
                return null;
            }

            List<Face2D> current = new List<Face2D> { face2D };
            List<Face2D> bound = sourceEvidence.BoundFootprint;
            List<Face2D> common = DefiniteIntersection(current, bound, tolerance_Distance);
            if (common == null)
            {
                reason = "the footprint comparison could not be decided";
                return null;
            }

            double difference = RegionArea(current) + RegionArea(bound) - 2 * RegionArea(common);
            double allowed = tolerance_Distance * RegionPerimeter(bound);
            if (!(difference <= allowed))
            {
                reason = string.Format(System.Globalization.CultureInfo.InvariantCulture, "the footprint differs from the footprint the record was bound to (symmetric difference {0:G6} m2, allowed {1:G3} m2)", difference, allowed);
                return null;
            }

            return sourceEvidence;
        }

        /// <summary>
        /// Intersection of two lists of regions in one frame whose result is definite: an empty list means certainly empty, null
        /// means the kernel could not decide. <see cref="Face2D"/> intersection returns null both for disjoint inputs and, possibly,
        /// when it fails to rebuild a result, so regions whose bounding boxes are certainly disjoint (exact test, with
        /// <paramref name="tolerance_Distance"/>) are an empty result, and a null result for regions whose bounds overlap is a
        /// failure for the whole call. Pieces of area <paramref name="tolerance_Distance"/> or less are dropped.
        /// </summary>
        /// <param name="face2Ds_1">Regions, in one frame.</param>
        /// <param name="face2Ds_2">Regions, in the same frame.</param>
        /// <param name="tolerance_Distance">Distance tolerance [m].</param>
        /// <returns>The pieces of the intersection, an empty list when it is certainly empty, or null when it cannot be decided.</returns>
        public static List<Face2D> DefiniteIntersection(IEnumerable<Face2D> face2Ds_1, IEnumerable<Face2D> face2Ds_2, double tolerance_Distance = Tolerance.Distance)
        {
            return DefiniteIntersection(face2Ds_1, face2Ds_2, tolerance_Distance, null);
        }

        internal static List<Face2D> DefiniteIntersection(IEnumerable<Face2D> face2Ds_1, IEnumerable<Face2D> face2Ds_2, double tolerance_Distance, Func<Face2D, Face2D, double, List<Face2D>> intersection)
        {
            if (face2Ds_1 == null || face2Ds_2 == null)
            {
                return null;
            }

            intersection = intersection ?? ((x, y, tolerance) => x.Intersection(y, tolerance));

            List<Face2D> result = new List<Face2D>();
            List<Face2D> list_2 = face2Ds_2.ToList();
            foreach (Face2D face2D_1 in face2Ds_1)
            {
                BoundingBox2D boundingBox2D_1 = face2D_1?.GetBoundingBox();
                if (boundingBox2D_1 == null)
                {
                    return null;
                }

                foreach (Face2D face2D_2 in list_2)
                {
                    BoundingBox2D boundingBox2D_2 = face2D_2?.GetBoundingBox();
                    if (boundingBox2D_2 == null)
                    {
                        return null;
                    }

                    if (boundingBox2D_1.Max.X < boundingBox2D_2.Min.X - tolerance_Distance || boundingBox2D_2.Max.X < boundingBox2D_1.Min.X - tolerance_Distance || boundingBox2D_1.Max.Y < boundingBox2D_2.Min.Y - tolerance_Distance || boundingBox2D_2.Max.Y < boundingBox2D_1.Min.Y - tolerance_Distance)
                    {
                        continue;
                    }

                    List<Face2D> intersection_Pair = intersection(face2D_1, face2D_2, tolerance_Distance);
                    if (intersection_Pair == null)
                    {
                        return null;
                    }

                    result.AddRange(intersection_Pair.Where(x => x != null && x.GetArea() > tolerance_Distance));
                }
            }

            return result;
        }

        /// <summary>
        /// Total area of the regions (inner loops excluded); zero for none.
        /// </summary>
        internal static double RegionArea(IEnumerable<Face2D> face2Ds)
        {
            return face2Ds == null ? 0 : face2Ds.Where(x => x != null).Sum(x => x.GetArea());
        }

        /// <summary>
        /// Total length of the outer and inner loops of the regions; zero for none.
        /// </summary>
        internal static double RegionPerimeter(IEnumerable<Face2D> face2Ds)
        {
            double result = 0;
            foreach (Face2D face2D in face2Ds ?? Enumerable.Empty<Face2D>())
            {
                if (face2D == null)
                {
                    continue;
                }

                foreach (IClosed2D closed2D in new[] { face2D.ExternalEdge2D }.Concat(face2D.InternalEdge2Ds ?? new List<IClosed2D>()))
                {
                    result += closed2D?.Perimeter() ?? 0;
                }
            }

            return result;
        }

        private static bool SameFrame(Plane plane_1, Plane plane_2)
        {
            if (plane_1 == null || plane_2 == null)
            {
                return false;
            }

            return plane_1.Origin.Distance(plane_2.Origin) <= Tolerance.MicroDistance
                && 1 - plane_1.Normal.Unit.DotProduct(plane_2.Normal.Unit) <= FrameDirectionTolerance
                && 1 - plane_1.AxisY.Unit.DotProduct(plane_2.AxisY.Unit) <= FrameDirectionTolerance;
        }
    }
}
