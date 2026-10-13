// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Attributes;
using System.ComponentModel;

namespace SAM.Analytical
{
    /// <summary>
    /// Traceability stamped on panels derived from a source building element (for example a Rhino solid
    /// converted by <see cref="Create.PanelsBySolid(Geometry.Spatial.Shell, out System.Collections.Generic.List{string}, System.Collections.Generic.IEnumerable{Geometry.Spatial.ISAMGeometry3D}, double, double, bool, string, string, string, double, double, double)"/>),
    /// so every analytical panel can be traced back to the object it came from.
    /// </summary>
    [AssociatedTypes(typeof(Panel)), Description("Panel Source Parameter")]
    public enum PanelSourceParameter
    {
        [ParameterProperties("Source Name", "Name of the source element the panel was derived from")] SourceName,
        [ParameterProperties("Source Id", "Identifier (e.g. Rhino object Guid) of the source element")] SourceId,
        [ParameterProperties("Source Layer", "Layer (full path) of the source element")] SourceLayer,
        [ParameterProperties("Source Thickness", "Thickness of the source element [m]"), DoubleParameterValue(0)] SourceThickness,

        /// <summary>
        /// Why a panel exists when it is not the element's own mid-surface: <c>"Opening"</c> for an Air panel
        /// filling an opening (e.g. a stair void) that no aperture geometry claims.
        /// </summary>
        [ParameterProperties("Source Role", "Role of the panel within its source element (e.g. Opening for an Air panel filling an unclaimed opening)")] SourceRole,
    }
}
