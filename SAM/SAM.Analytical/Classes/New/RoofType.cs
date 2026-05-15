// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Architectural;
using System.Collections.Generic;

namespace SAM.Analytical
{
    public class RoofType : HostPartitionType
    {
        public RoofType(RoofType roofType)
            : base(roofType)
        {

        }
        public RoofType(System.Text.Json.Nodes.JsonObject jsonObject)

            : base(jsonObject)

        {

        }

        public RoofType(string name)
            : base(name)
        {

        }

        public RoofType(System.Guid guid, string name)
        : base(guid, name)
        {

        }

        public RoofType(RoofType roofType, string name)
            : base(roofType, name)
        {

        }

        public RoofType(string name, IEnumerable<MaterialLayer> materialLayers)
            : base(name, materialLayers)
        {

        }

        public RoofType(System.Guid guid, string name, IEnumerable<MaterialLayer> materialLayers)
            : base(guid, name, materialLayers)
        {

        }

    }
}
