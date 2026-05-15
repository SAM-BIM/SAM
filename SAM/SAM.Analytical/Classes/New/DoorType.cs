// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;
using SAM.Architectural;
using System.Collections.Generic;

namespace SAM.Analytical
{
    public class DoorType : OpeningType
    {
        public DoorType(DoorType doorType)
            : base(doorType)
        {

        }

        public DoorType(DoorType doorType, string name)
            : base(doorType, name)
        {

        }

        public DoorType(JObject jObject)
            : base(jObject?.Node as System.Text.Json.Nodes.JsonObject)
        {

        }


        public DoorType(System.Text.Json.Nodes.JsonObject jsonObject)

            : base(jsonObject)

        {

        }

        public DoorType(string name)
            : base(name)
        {

        }

        public DoorType(System.Guid guid, string name)
            : base(guid, name)
        {

        }

        public DoorType(string name, IEnumerable<MaterialLayer> paneMaterialLayers, IEnumerable<MaterialLayer> frameMaterialLayers = null)
            : base(name, paneMaterialLayers, frameMaterialLayers)
        {

        }

        public DoorType(System.Guid guid, string name, IEnumerable<MaterialLayer> paneMaterialLayers, IEnumerable<MaterialLayer> frameMaterialLayers = null)
            : base(guid, name, paneMaterialLayers, frameMaterialLayers)
        {

        }

    }
}
