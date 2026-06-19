// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using NetTopologySuite.Operation.Valid;

namespace SAM.Geometry.Planar
{
    public static partial class Query
    {
        /// <summary>
        /// Tests whether the geometry is topologically valid according to the OGC
        /// simple-features rules (no self-intersections, correctly oriented rings,
        /// holes contained within and not overlapping their shell, etc.).
        /// Unlike the structural <c>IsValid</c> checks elsewhere in SAM (null/NaN/point-count),
        /// this delegates to NetTopologySuite's <see cref="IsValidOp"/> and reports the reason
        /// when invalid.
        /// </summary>
        /// <param name="geometry">NetTopologySuite geometry to test.</param>
        /// <param name="reason">Human readable explanation when the geometry is invalid; otherwise null.</param>
        /// <returns>True if the geometry is topologically valid; otherwise false.</returns>
        public static bool IsValidTopologically(this NetTopologySuite.Geometries.Geometry geometry, out string reason)
        {
            reason = null;

            if (geometry == null)
            {
                reason = "Geometry is null.";
                return false;
            }

            if (geometry.IsValid)
            {
                return true;
            }

            TopologyValidationError topologyValidationError = new IsValidOp(geometry).ValidationError;
            reason = topologyValidationError != null ? topologyValidationError.Message : "Geometry is topologically invalid.";
            return false;
        }

        /// <summary>
        /// Tests whether the geometry is topologically valid according to the OGC
        /// simple-features rules using NetTopologySuite's <see cref="IsValidOp"/>.
        /// </summary>
        /// <param name="geometry">NetTopologySuite geometry to test.</param>
        /// <returns>True if the geometry is topologically valid; otherwise false.</returns>
        public static bool IsValidTopologically(this NetTopologySuite.Geometries.Geometry geometry)
        {
            return IsValidTopologically(geometry, out string _);
        }

        /// <summary>
        /// Tests whether the <see cref="Face2D"/> is topologically valid (no self-intersecting
        /// edges, holes inside the outer loop and not overlapping each other, etc.) by converting
        /// it to NetTopologySuite and running <see cref="IsValidOp"/>.
        /// </summary>
        /// <param name="face2D">Face2D to test.</param>
        /// <param name="reason">Human readable explanation when the geometry is invalid; otherwise null.</param>
        /// <param name="tolerance">Tolerance used when converting to NetTopologySuite.</param>
        /// <returns>True if the Face2D is topologically valid; otherwise false.</returns>
        public static bool IsValidTopologically(this Face2D face2D, out string reason, double tolerance = Core.Tolerance.MicroDistance)
        {
            reason = null;

            if (face2D == null)
            {
                reason = "Geometry is null.";
                return false;
            }

            NetTopologySuite.Geometries.Geometry geometry;
            try
            {
                geometry = Convert.ToNTS(face2D, tolerance);
            }
            catch
            {
                geometry = null;
            }

            if (geometry == null)
            {
                reason = "Geometry could not be converted to NetTopologySuite.";
                return false;
            }

            return IsValidTopologically(geometry, out reason);
        }

        /// <summary>
        /// Tests whether the <see cref="Face2D"/> is topologically valid using NetTopologySuite's <see cref="IsValidOp"/>.
        /// </summary>
        /// <param name="face2D">Face2D to test.</param>
        /// <param name="tolerance">Tolerance used when converting to NetTopologySuite.</param>
        /// <returns>True if the Face2D is topologically valid; otherwise false.</returns>
        public static bool IsValidTopologically(this Face2D face2D, double tolerance = Core.Tolerance.MicroDistance)
        {
            return IsValidTopologically(face2D, out string _, tolerance);
        }

        /// <summary>
        /// Tests whether the <see cref="Polygon2D"/> is topologically valid (no self-intersecting
        /// edges) by converting it to NetTopologySuite and running <see cref="IsValidOp"/>.
        /// </summary>
        /// <param name="polygon2D">Polygon2D to test.</param>
        /// <param name="reason">Human readable explanation when the geometry is invalid; otherwise null.</param>
        /// <param name="tolerance">Tolerance used when converting to NetTopologySuite.</param>
        /// <returns>True if the Polygon2D is topologically valid; otherwise false.</returns>
        public static bool IsValidTopologically(this Polygon2D polygon2D, out string reason, double tolerance = Core.Tolerance.MicroDistance)
        {
            reason = null;

            if (polygon2D == null)
            {
                reason = "Geometry is null.";
                return false;
            }

            NetTopologySuite.Geometries.Geometry geometry;
            try
            {
                geometry = Convert.ToNTS_Polygon(polygon2D, tolerance);
            }
            catch
            {
                geometry = null;
            }

            if (geometry == null)
            {
                reason = "Geometry could not be converted to NetTopologySuite.";
                return false;
            }

            return IsValidTopologically(geometry, out reason);
        }

        /// <summary>
        /// Tests whether the <see cref="Polygon2D"/> is topologically valid using NetTopologySuite's <see cref="IsValidOp"/>.
        /// </summary>
        /// <param name="polygon2D">Polygon2D to test.</param>
        /// <param name="tolerance">Tolerance used when converting to NetTopologySuite.</param>
        /// <returns>True if the Polygon2D is topologically valid; otherwise false.</returns>
        public static bool IsValidTopologically(this Polygon2D polygon2D, double tolerance = Core.Tolerance.MicroDistance)
        {
            return IsValidTopologically(polygon2D, out string _, tolerance);
        }
    }
}
