// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;

using SAM.Core;

namespace SAM.Analytical
{
    public abstract class BuildingElementType : SAMType, IAnalyticalObject
    {
        public BuildingElementType(BuildingElementType buildingElementType)
            : base(buildingElementType)
        {

        }

        public BuildingElementType(BuildingElementType buildingElementType, string name)
            : base(buildingElementType, name)
        {

        }

        public BuildingElementType(JObject jObject)
            : base(jObject?.Node as System.Text.Json.Nodes.JsonObject)
        {

        }


        public BuildingElementType(System.Text.Json.Nodes.JsonObject jsonObject)

            : base(jsonObject)

        {

        }

        public BuildingElementType(string name)
            : base(name)
        {

        }

        public BuildingElementType(System.Guid guid, string name)
            : base(guid, name)
        {

        }

    }
}
