// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;
using SAM.Core;

namespace SAM.Analytical
{
    public class SpaceHeatingSystemTypeNameFilter : SpaceMechanicalSystemTypeNameFilter<HeatingSystem>
    {
        public SpaceHeatingSystemTypeNameFilter(TextComparisonType textComparisonType, string value)
            : base(textComparisonType, value)
        {

        }

        public SpaceHeatingSystemTypeNameFilter(SpaceHeatingSystemTypeNameFilter spaceHeatingSystemTypeNameFilter)
            : base(spaceHeatingSystemTypeNameFilter)
        {

        }

        public SpaceHeatingSystemTypeNameFilter(JObject jObject)
            : base(jObject?.Node as System.Text.Json.Nodes.JsonObject)
        {

        }


        public SpaceHeatingSystemTypeNameFilter(System.Text.Json.Nodes.JsonObject jsonObject)

            : base(jsonObject)

        {

        }
    }
}
