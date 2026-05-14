// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;
using System;

namespace SAM.Analytical
{
    /// <summary>
    /// Represents an empty section in the analytical domain
    /// </summary>
    public class EmptySection : SimpleEquipment, ISection
    {
        public EmptySection()
            : base("Empty Section")
        {

        }

        public EmptySection(string name)
            : base(name)
        {

        }

        public EmptySection(JObject jObject)
            : base(jObject)
        {

        }

        public EmptySection(EmptySection emptySection)
            : base(emptySection)
        {

        }

        public EmptySection(Guid guid, string name)
            : base(guid, name)
        {

        }

    }
}
