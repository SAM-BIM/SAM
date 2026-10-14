// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Attributes;
using System.ComponentModel;

namespace SAM.Analytical
{
    /// <summary>
    /// Traceability stamped on apertures derived from source window and door solids by
    /// <see cref="Create.AperturesBySolids"/>. An aperture built from several members (frame, glass, leaf) lists
    /// each member's value in member order, separated by "; ".
    /// </summary>
    [AssociatedTypes(typeof(Aperture)), Description("Aperture Source Parameter")]
    public enum ApertureSourceParameter
    {
        [ParameterProperties("Source Name", "Names of the source members the aperture was derived from")] SourceName,
        [ParameterProperties("Source Id", "Identifiers (e.g. Rhino object Guids) of the source members")] SourceId,
        [ParameterProperties("Source Layer", "Distinct layers (full path) of the source members")] SourceLayer,
        [ParameterProperties("Source Thickness", "Depth of the source assembly normal to the aperture [m]"), DoubleParameterValue(0)] SourceThickness,
    }
}
