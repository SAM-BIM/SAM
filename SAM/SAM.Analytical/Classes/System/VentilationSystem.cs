// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors


namespace SAM.Analytical
{
    public class VentilationSystem : MechanicalSystem
    {
        public VentilationSystem(string prefix, string id, VentilationSystemType ventilationSystemType)
            : base(prefix, id, ventilationSystemType)
        {

        }

        public VentilationSystem(string id, VentilationSystemType ventilationSystemType)
            : base(id, ventilationSystemType)
        {

        }

        public VentilationSystem(System.Guid guid, string id, VentilationSystem VentilationSystem)
            : base(guid, id, VentilationSystem)
        {

        }

        public VentilationSystem(VentilationSystem ventilationSystem)
            : base(ventilationSystem)
        {

        }
        public VentilationSystem(System.Text.Json.Nodes.JsonObject jsonObject)

            : base(jsonObject)

        {

        }

    }
}
