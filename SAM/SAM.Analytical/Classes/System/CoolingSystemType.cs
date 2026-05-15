// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;
using System;

namespace SAM.Analytical
{
    public class CoolingSystemType : MechanicalSystemType
    {
        //private string description;

        public CoolingSystemType(string name, string description)
            : base(name, description)
        {

        }

        public CoolingSystemType(Guid guid, string name, string description)
            : base(guid, name, description)
        {
        }

        public CoolingSystemType(CoolingSystemType coolingSystemType)
            : base(coolingSystemType)
        {

        }

        public CoolingSystemType(JObject jObject)
            : base(jObject?.Node as System.Text.Json.Nodes.JsonObject)
        {
        }


        public CoolingSystemType(System.Text.Json.Nodes.JsonObject jsonObject)

            : base(jsonObject)

        {

        }

    }
}
