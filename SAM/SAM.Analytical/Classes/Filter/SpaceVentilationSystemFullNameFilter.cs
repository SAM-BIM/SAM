// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;
using SAM.Core;

namespace SAM.Analytical
{
    public class SpaceVentilationSystemFullNameFilter : SpaceMechanicalSystemFullNameFilter<VentilationSystem>
    {
        public SpaceVentilationSystemFullNameFilter(TextComparisonType textComparisonType, string value)
            : base(textComparisonType, value)
        {

        }

        public SpaceVentilationSystemFullNameFilter(SpaceVentilationSystemFullNameFilter spaceVentilationSystemFullNameFilter)
            : base(spaceVentilationSystemFullNameFilter)
        {

        }
        public SpaceVentilationSystemFullNameFilter(System.Text.Json.Nodes.JsonObject jsonObject)

            : base(jsonObject)

        {

        }
    }
}
