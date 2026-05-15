// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;
using SAM.Core;

namespace SAM.Analytical
{
    public class SpaceCoolingSystemTypeNameFilter : SpaceMechanicalSystemTypeNameFilter<CoolingSystem>
    {
        public SpaceCoolingSystemTypeNameFilter(TextComparisonType textComparisonType, string value)
            : base(textComparisonType, value)
        {

        }

        public SpaceCoolingSystemTypeNameFilter(SpaceCoolingSystemTypeNameFilter spaceCoolingSystemTypeNameFilter)
            : base(spaceCoolingSystemTypeNameFilter)
        {

        }

        public SpaceCoolingSystemTypeNameFilter(JObject jObject)
            : base(jObject?.Node as System.Text.Json.Nodes.JsonObject)
        {

        }


        public SpaceCoolingSystemTypeNameFilter(System.Text.Json.Nodes.JsonObject jsonObject)

            : base(jsonObject)

        {

        }
    }
}
