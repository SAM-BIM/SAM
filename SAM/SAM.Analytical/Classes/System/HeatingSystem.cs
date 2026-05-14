// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;

namespace SAM.Analytical
{
    public class HeatingSystem : MechanicalSystem
    {
        public HeatingSystem(string prefix, string id, HeatingSystemType heatingSystemType)
            : base(prefix, id, heatingSystemType)
        {

        }

        public HeatingSystem(string id, HeatingSystemType heatingSystemType)
            : base(id, heatingSystemType)
        {

        }

        public HeatingSystem(System.Guid guid, string id, HeatingSystem heatingSystem)
            : base(guid, id, heatingSystem)
        {

        }

        public HeatingSystem(HeatingSystem heatingSystem)
            : base(heatingSystem)
        {

        }

        public HeatingSystem(JObject jObject)
            : base(jObject)
        {
        }

    }
}
