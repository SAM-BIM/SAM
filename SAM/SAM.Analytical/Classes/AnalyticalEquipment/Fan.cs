// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;

namespace SAM.Analytical
{
    /// <summary>
    /// Represents an fan object in the analytical domain
    /// </summary>
    public class Fan : SimpleEquipment, ISection
    {
        public Fan()
            : base("Fan")
        {

        }

        public Fan(string name)
            : base(name)
        {

        }
        public Fan(System.Text.Json.Nodes.JsonObject jsonObject)

            : base(jsonObject)

        {

        }

        public Fan(Fan fan)
            : base(fan)
        {

        }

        public Fan(Guid guid, string name)
            : base(guid, name)
        {

        }

    }
}
