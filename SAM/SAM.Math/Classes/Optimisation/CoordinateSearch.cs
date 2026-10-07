// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

namespace SAM.Math
{
    /// <summary>GenOpt "GPSCoordinateSearch": generalised pattern search with coordinate polling only (spec §3).</summary>
    public sealed class CoordinateSearch : GeneralisedPatternSearch
    {
        internal override bool GlobalSearch => false;
    }
}
