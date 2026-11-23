// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Geometry.Spatial;
using System.Collections.Generic;

namespace SAM.Analytical
{
    public static partial class Create
    {
        /// <summary>
        /// True when <paramref name="point3D"/> lies in the material of <paramref name="shell"/>, whatever the order of its
        /// faces. As <see cref="Shell.Inside"/>, a ray along the bounding-box diagonal decides when it meets no edge; otherwise
        /// one ray towards each face, and the parity of the rays meeting no edge decides by majority (a tie, or no such ray,
        /// is not inside). <see cref="Shell.Inside"/> takes the first such ray in face order instead: on a watertight solid
        /// every ray agrees, but on a solid that is not (a face missing, or overlapping faces at a slot) rays disagree, and the
        /// answer would then change with the order in which the same faces are listed.
        /// </summary>
        private static bool Contains(Shell shell, Point3D point3D, double silverSpacing, double tolerance)
        {
            BoundingBox3D boundingBox3D = shell?.GetBoundingBox();
            List<Face3D> face3Ds = shell?.Face3Ds;
            if (point3D == null || boundingBox3D == null || face3Ds == null || face3Ds.Count == 0)
            {
                return false;
            }

            boundingBox3D = new BoundingBox3D(boundingBox3D, tolerance);
            if (!boundingBox3D.InRange(point3D))
            {
                return false;
            }

            Vector3D diagonal = new Vector3D(boundingBox3D.Min, boundingBox3D.Max);
            double length = diagonal.Length;
            if (length < silverSpacing)
            {
                return false;
            }

            // null: the ray meets an edge (or nothing) and does not decide.
            bool? Parity(Vector3D vector3D)
            {
                List<ISAMGeometry3D> geometry3Ds = shell.IntersectionGeometry3Ds<ISAMGeometry3D>(new Segment3D(point3D, vector3D), true, tolerance);
                if (geometry3Ds == null || geometry3Ds.Count == 0 || geometry3Ds.Exists(x => x is ISegmentable3D || shell.OnEdge(x as Point3D, silverSpacing)))
                {
                    return null;
                }

                return geometry3Ds.Count % 2 != 0;
            }

            bool? parity = Parity(diagonal);
            if (parity.HasValue)
            {
                return parity.Value;
            }

            // Stop as soon as the rays left cannot change the majority: the answer is the same in any face order.
            int inside = 0, outside = 0;
            for (int i = 0; i < face3Ds.Count; i++)
            {
                int left = face3Ds.Count - i;
                if (inside > outside + left)
                {
                    return true;
                }

                if (outside >= inside + left)
                {
                    return false;
                }

                Point3D internalPoint = face3Ds[i].InternalPoint3D(tolerance);
                if (internalPoint == null)
                {
                    continue;
                }

                Vector3D vector3D = new Vector3D(point3D, internalPoint);
                vector3D.Normalize();
                parity = Parity(vector3D * length);
                if (parity == true)
                {
                    inside++;
                }
                else if (parity == false)
                {
                    outside++;
                }
            }

            return inside > outside;
        }
    }
}
