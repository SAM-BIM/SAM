// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;
using System;

namespace SAM.Analytical
{
    public class HeatingSystemType : MechanicalSystemType
    {
        //private string description;

        public HeatingSystemType(string name, string description)
            : base(name, description)
        {

        }

        public HeatingSystemType(Guid guid, string name, string description)
            : base(guid, name, description)
        {
        }

        public HeatingSystemType(HeatingSystemType heatingSystemType)
            : base(heatingSystemType)
        {

        }

        public HeatingSystemType(JObject jObject)
            : base(jObject?.Node as System.Text.Json.Nodes.JsonObject)
        {
        }


        public HeatingSystemType(System.Text.Json.Nodes.JsonObject jsonObject)

            : base(jsonObject)

        {

        }

    }
}
