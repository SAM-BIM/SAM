// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using Rhino;

namespace SAM.Geometry.Rhino
{
    public static partial class Query
    {
        /// <summary>
        /// A user-facing warning when the Rhino document is not in metres, or null when it is (or no document is
        /// open). SAM analytical geometry is in metres and Rhino geometry is converted to SAM without scaling, so a
        /// millimetre model would silently become a model a thousand times too large. This only detects and
        /// explains; it never converts.
        /// </summary>
        public static string ModelUnitsWarning(RhinoDoc rhinoDoc = null)
        {
            rhinoDoc = rhinoDoc ?? RhinoDoc.ActiveDoc;
            if (rhinoDoc == null || rhinoDoc.ModelUnitSystem == UnitSystem.Meters)
            {
                return null;
            }

            return string.Format(
                "Rhino document units are {0}, but SAM analytical geometry is in metres and Rhino geometry is not converted. Change Rhino Document Properties > Units > Model units to Meters, answer Yes to scale the objects, then recompute.",
                rhinoDoc.ModelUnitSystem);
        }
    }
}
