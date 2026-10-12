// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.ComponentModel;

namespace SAM.Analytical
{
    /// <summary>
    /// The units an <see cref="AnalyticalModelSimulationResult"/> holds its consumptions and peak loads in, as decided from
    /// what the result records: its <see cref="AnalyticalModelSimulationResultParameter.ResultUnitsVersion"/> marker, or,
    /// without one, its producer (<see cref="Core.Result.Source"/>). Never decided from the size of a value.
    /// <para>Members are appended, never reordered.</para>
    /// </summary>
    [Description("Result Units State.")]
    public enum ResultUnitsState
    {
        /// <summary>No result.</summary>
        [Description("Undefined")] Undefined,

        /// <summary>Marker version 1: consumption in kWh, peak loads in kW, as labelled.</summary>
        [Description("Marked")] Marked,

        /// <summary>No marker, written by SAM_Tas before the units correction: consumption in Wh, peak loads in W.</summary>
        [Description("Legacy Tas")] LegacyTas,

        /// <summary>No marker, written by SAM_OpenStudio, which always wrote kWh and kW.</summary>
        [Description("Legacy OpenStudio")] LegacyOpenStudio,

        /// <summary>A marker this version of SAM does not know (not 1, or not a whole number): the units are not known.</summary>
        [Description("Unsupported Version")] UnsupportedVersion,

        /// <summary>No marker and a producer other than SAM_Tas or SAM_OpenStudio: the units are not known.</summary>
        [Description("Unknown Producer")] UnknownProducer,
    }
}
