// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;

using System;

namespace SAM.Core
{
    public class OpaqueMaterial : SolidMaterial
    {
        public OpaqueMaterial(string name)
            : base(name)
        {

        }

        public OpaqueMaterial(string name, string group, string displayName, string description, double thermalConductivity, double specificHeatCapacity, double density)
            : base(name, group, displayName, description, thermalConductivity, specificHeatCapacity, density)
        {

        }

        public OpaqueMaterial(Guid guid, string name)
            : base(guid, name)
        {

        }

        public OpaqueMaterial(string name, Guid guid, OpaqueMaterial opaqueMaterial, string displayName, string description)
            : base(name, guid, opaqueMaterial, displayName, description)
        {

        }

        public OpaqueMaterial(Guid guid, string name, string displayName, string description, double thermalConductivity, double density, double specificHeatCapacity)
            : base(guid, name, displayName, description, thermalConductivity, density, specificHeatCapacity)
        {

        }

        public OpaqueMaterial(JObject jObject)
            : base(jObject?.Node as System.Text.Json.Nodes.JsonObject)
        {
        }


        public OpaqueMaterial(System.Text.Json.Nodes.JsonObject jsonObject)

            : base(jsonObject)

        {

        }

        public OpaqueMaterial(OpaqueMaterial opaqueMaterial)
            : base(opaqueMaterial)
        {
        }

    }
}
