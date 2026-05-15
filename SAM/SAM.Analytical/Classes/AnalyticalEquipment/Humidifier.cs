// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;

namespace SAM.Analytical
{
    /// <summary>
    /// Represents an heat humidifier unit unit object in the analytical domain
    /// </summary>
    public class Humidifier : SimpleEquipment, ISection
    {
        public Humidifier()
            : base("Humidifier")
        {

        }

        public Humidifier(string name)
            : base(name)
        {

        }
        public Humidifier(System.Text.Json.Nodes.JsonObject jsonObject)

            : base(jsonObject)

        {

        }

        public Humidifier(Humidifier humidifier)
            : base(humidifier)
        {

        }

        public Humidifier(Guid guid, string name)
            : base(guid, name)
        {

        }

    }
}
