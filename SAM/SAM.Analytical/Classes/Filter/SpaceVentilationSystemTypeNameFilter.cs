// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;
using SAM.Core;

namespace SAM.Analytical
{
    public class SpaceVentilationSystemTypeNameFilter : SpaceMechanicalSystemTypeNameFilter<VentilationSystem>
    {
        public SpaceVentilationSystemTypeNameFilter(TextComparisonType textComparisonType, string value)
            : base(textComparisonType, value)
        {

        }

        public SpaceVentilationSystemTypeNameFilter(SpaceVentilationSystemTypeNameFilter spaceVentilationSystemTypeNameFilter)
            : base(spaceVentilationSystemTypeNameFilter)
        {

        }

        public SpaceVentilationSystemTypeNameFilter(JObject jObject)
            : base(jObject?.Node as System.Text.Json.Nodes.JsonObject)
        {

        }


        public SpaceVentilationSystemTypeNameFilter(System.Text.Json.Nodes.JsonObject jsonObject)

            : base(jsonObject)

        {

        }
    }
}
