// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;
using SAM.Core;

namespace SAM.Analytical
{
    public class SpaceHeatingSystemFullNameFilter : SpaceMechanicalSystemFullNameFilter<HeatingSystem>
    {
        public SpaceHeatingSystemFullNameFilter(TextComparisonType textComparisonType, string value)
            : base(textComparisonType, value)
        {

        }

        public SpaceHeatingSystemFullNameFilter(SpaceHeatingSystemFullNameFilter spaceHeatingSystemFullNameFilter)
            : base(spaceHeatingSystemFullNameFilter)
        {

        }

        public SpaceHeatingSystemFullNameFilter(JObject jObject)
            : base(jObject)
        {

        }
    }
}
