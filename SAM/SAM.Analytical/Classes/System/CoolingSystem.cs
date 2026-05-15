// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;
namespace SAM.Analytical
{
    public class CoolingSystem : MechanicalSystem
    {
        public CoolingSystem(string prefix, string id, CoolingSystemType coolingSystemType)
            : base(prefix, id, coolingSystemType)
        {

        }

        public CoolingSystem(string id, CoolingSystemType coolingSystemType)
            : base(id, coolingSystemType)
        {

        }

        public CoolingSystem(System.Guid guid, string id, CoolingSystem coolingSystem)
            : base(guid, id, coolingSystem)
        {

        }

        public CoolingSystem(CoolingSystem coolingSystem)
            : base(coolingSystem)
        {

        }

        public CoolingSystem(JObject jObject)
            : base(jObject?.Node as System.Text.Json.Nodes.JsonObject)
        {
        }


        public CoolingSystem(System.Text.Json.Nodes.JsonObject jsonObject)

            : base(jsonObject)

        {

        }

    }
}
