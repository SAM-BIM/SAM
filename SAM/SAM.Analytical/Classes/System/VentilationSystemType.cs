// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;
using System;

namespace SAM.Analytical
{
    public class VentilationSystemType : MechanicalSystemType
    {
        //private string description;

        public VentilationSystemType(string name, string description)
            : base(name, description)
        {

        }

        public VentilationSystemType(Guid guid, string name, string description)
            : base(guid, name, description)
        {
        }

        public VentilationSystemType(VentilationSystemType ventilationSystemType)
            : base(ventilationSystemType)
        {

        }

        public VentilationSystemType(JObject jObject)
            : base(jObject?.Node as System.Text.Json.Nodes.JsonObject)
        {
        }


        public VentilationSystemType(System.Text.Json.Nodes.JsonObject jsonObject)

            : base(jsonObject)

        {

        }

    }
}
