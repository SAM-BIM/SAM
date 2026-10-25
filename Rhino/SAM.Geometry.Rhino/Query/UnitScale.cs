// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using Rhino;

namespace SAM.Geometry.Rhino
{
    public static partial class Query
    {
        /// <summary>
        /// Scale from metres (SAM units) to the active document's model units. Without an active document
        /// (Rhino.Inside, Compute/Hops, tests) SAM geometry is taken as metres and 1 is returned, so the
        /// converters' tolerances stay finite.
        /// </summary>
        public static double UnitScale()
        {
            RhinoDoc rhinoDoc = RhinoDoc.ActiveDoc;
            if (rhinoDoc == null)
            {
                return 1;
            }

            return UnitScale(rhinoDoc);
        }

        public static double UnitScale(RhinoDoc rhinoDoc)
        {
            if (rhinoDoc == null)
            {
                return double.NaN;
            }

            return RhinoMath.UnitScale(UnitSystem.Meters, rhinoDoc.ModelUnitSystem);
        }
    }
}
