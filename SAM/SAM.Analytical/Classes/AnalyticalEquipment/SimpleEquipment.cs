// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;
using SAM.Core;
using System;

namespace SAM.Analytical
{
    /// <summary>
    /// Represents an simple equipment object in the analytical domain
    /// </summary>
    public abstract class SimpleEquipment : SAMObject, ISimpleEquipment
    {
        public SimpleEquipment(string name)
            : base(name)
        {

        }

        public SimpleEquipment(JObject jObject)
            : base(jObject?.Node as System.Text.Json.Nodes.JsonObject)
        {

        }


        public SimpleEquipment(System.Text.Json.Nodes.JsonObject jsonObject)

            : base(jsonObject)

        {

        }

        public SimpleEquipment(SimpleEquipment simpleEquipment)
            : base(simpleEquipment)
        {

        }

        public SimpleEquipment(Guid guid, string name)
            : base(guid, name)
        {

        }

    }
}
