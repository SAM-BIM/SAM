// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

namespace SAM.Analytical
{
    public static partial class Query
    {
        /// <summary>
        /// True where <paramref name="specific"/> is a more specific refinement of
        /// <paramref name="general"/> rather than a genuinely different, disagreeing classification -
        /// e.g. Communal Circulation is Circulation, just outside the dwelling boundary rather than
        /// inside it. Used so <see cref="SpaceSemanticsResolver"/> does not report a space
        /// name/internal-condition disagreement as a conflict when one source is simply more specific
        /// than the other.
        /// </summary>
        public static bool IsCompatibleSpaceUseRefinement(Analytical.SpaceUse general, Analytical.SpaceUse specific)
        {
            return general == Analytical.SpaceUse.Circulation && specific == Analytical.SpaceUse.CommunalCirculation;
        }
    }
}
