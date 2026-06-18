// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using NetTopologySuite.Geometries.Utilities;
using System.Collections.Generic;

namespace SAM.Geometry.Planar
{
    public static partial class Query
    {
        /// <summary>
        /// Repairs a topologically invalid geometry (self-intersections, mis-oriented rings,
        /// holes overlapping the shell, etc.) using NetTopologySuite's <see cref="GeometryFixer"/>.
        /// This is the modern, area-preserving replacement for the legacy <c>Buffer(0)</c> trick.
        /// Already-valid geometries are returned unchanged.
        /// </summary>
        /// <param name="geometry">NetTopologySuite geometry to repair.</param>
        /// <returns>A valid geometry, or null when the input is null or cannot be repaired.</returns>
        public static NetTopologySuite.Geometries.Geometry Fix(this NetTopologySuite.Geometries.Geometry geometry)
        {
            if (geometry == null)
            {
                return null;
            }

            if (geometry.IsValid)
            {
                return geometry;
            }

            return GeometryFixer.Fix(geometry);
        }

        /// <summary>
        /// Repairs a topologically invalid <see cref="Face2D"/> by round-tripping it through
        /// NetTopologySuite's <see cref="GeometryFixer"/>. A single repaired Face2D is returned
        /// where possible; when the repair splits the geometry into several faces the largest is
        /// returned (see <see cref="Fix(IEnumerable{Face2D}, double)"/> to keep every resulting face).
        /// Already-valid faces are returned unchanged.
        /// </summary>
        /// <param name="face2D">Face2D to repair.</param>
        /// <param name="tolerance">Tolerance used when converting to and from NetTopologySuite.</param>
        /// <returns>
        /// A repaired Face2D; the original face unchanged when it is already valid or cannot be
        /// represented in NetTopologySuite (e.g. curved edges); or null when the input is null or
        /// is invalid and cannot be repaired.
        /// </returns>
        public static Face2D Fix(this Face2D face2D, double tolerance = Core.Tolerance.MicroDistance)
        {
            if (face2D == null)
            {
                return null;
            }

            NetTopologySuite.Geometries.Geometry geometry = ToNTS(face2D, tolerance);

            // Cannot be represented in NetTopologySuite (e.g. curved edges) -> nothing to repair,
            // so preserve the face rather than dropping it.
            if (geometry == null || geometry.IsValid)
            {
                return face2D;
            }

            List<Face2D> face2Ds = FixToFace2Ds(geometry, tolerance);
            if (face2Ds == null || face2Ds.Count == 0)
            {
                return null;
            }

            Face2D result = face2Ds[0];
            double maxArea = result.GetArea();
            for (int i = 1; i < face2Ds.Count; i++)
            {
                double area = face2Ds[i].GetArea();
                if (area > maxArea)
                {
                    maxArea = area;
                    result = face2Ds[i];
                }
            }

            return result;
        }

        /// <summary>
        /// Repairs each <see cref="Face2D"/> in a collection, keeping every face produced by the
        /// repair. A self-intersecting "bowtie", for example, is split into the two valid faces it
        /// describes rather than being collapsed or discarded, so no area is silently lost.
        /// Already-valid faces, and faces that cannot be represented in NetTopologySuite (e.g.
        /// curved edges), are passed through unchanged; only faces that are invalid and cannot be
        /// repaired are dropped.
        /// </summary>
        /// <param name="face2Ds">Faces to repair.</param>
        /// <param name="tolerance">Tolerance used when converting to and from NetTopologySuite.</param>
        /// <returns>The repaired faces, or null when the input is null.</returns>
        public static List<Face2D> Fix(this IEnumerable<Face2D> face2Ds, double tolerance = Core.Tolerance.MicroDistance)
        {
            if (face2Ds == null)
            {
                return null;
            }

            List<Face2D> result = new List<Face2D>();
            foreach (Face2D face2D in face2Ds)
            {
                if (face2D == null)
                {
                    continue;
                }

                NetTopologySuite.Geometries.Geometry geometry = ToNTS(face2D, tolerance);

                // Cannot be represented in NetTopologySuite (e.g. curved edges) or already valid
                // -> preserve the face rather than dropping it.
                if (geometry == null || geometry.IsValid)
                {
                    result.Add(face2D);
                    continue;
                }

                List<Face2D> face2Ds_Fixed = FixToFace2Ds(geometry, tolerance);
                if (face2Ds_Fixed != null)
                {
                    result.AddRange(face2Ds_Fixed);
                }
            }

            return result;
        }

        private static NetTopologySuite.Geometries.Geometry ToNTS(Face2D face2D, double tolerance)
        {
            try
            {
                return Convert.ToNTS(face2D, tolerance);
            }
            catch
            {
                return null;
            }
        }

        private static List<Face2D> FixToFace2Ds(NetTopologySuite.Geometries.Geometry geometry, double tolerance)
        {
            if (geometry == null)
            {
                return null;
            }

            NetTopologySuite.Geometries.Geometry geometry_Fixed = GeometryFixer.Fix(geometry);
            if (geometry_Fixed == null || geometry_Fixed.IsEmpty)
            {
                return null;
            }

            List<Face2D> result = new List<Face2D>();

            if (geometry_Fixed is NetTopologySuite.Geometries.Polygon polygon)
            {
                Face2D face2D_Fixed = polygon.ToSAM(tolerance);
                if (face2D_Fixed != null)
                {
                    result.Add(face2D_Fixed);
                }
            }
            else if (geometry_Fixed is NetTopologySuite.Geometries.MultiPolygon multiPolygon)
            {
                List<Face2D> face2Ds = multiPolygon.ToSAM(tolerance);
                if (face2Ds != null)
                {
                    result.AddRange(face2Ds);
                }
            }

            return result;
        }
    }
}
