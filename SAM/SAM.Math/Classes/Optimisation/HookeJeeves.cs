// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

namespace SAM.Math
{
    /// <summary>
    /// GenOpt "GPSHookeJeeves": generalised pattern search whose global step evaluates the pattern point
    /// 2·X[k] − X[k−1] and explores around it before the local search (spec §3.2). Not textbook Hooke-Jeeves.
    /// </summary>
    public sealed class HookeJeeves : GeneralisedPatternSearch
    {
        internal override bool GlobalSearch => true;
    }
}
