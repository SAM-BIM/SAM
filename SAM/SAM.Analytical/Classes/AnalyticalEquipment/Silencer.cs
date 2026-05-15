// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;
using System;

namespace SAM.Analytical
{
    /// <summary>
    /// Represents an fan object in the analytical domain
    /// </summary>
    public class Silencer : SimpleEquipment, ISection
    {
        public Silencer()
            : base("Silencer")
        {

        }

        public Silencer(string name)
            : base(name)
        {

        }

        public Silencer(JObject jObject)
            : base(jObject?.Node as System.Text.Json.Nodes.JsonObject)
        {

        }


        public Silencer(System.Text.Json.Nodes.JsonObject jsonObject)

            : base(jsonObject)

        {

        }

        public Silencer(Silencer silencer)
            : base(silencer)
        {

        }

        public Silencer(Guid guid, string name)
            : base(guid, name)
        {

        }

    }
}
