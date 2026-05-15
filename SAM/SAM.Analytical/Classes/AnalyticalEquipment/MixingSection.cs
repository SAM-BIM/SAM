// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;

namespace SAM.Analytical
{
    /// <summary>
    /// Represents an mixing section in the analytical domain
    /// </summary>
    public class MixingSection : SimpleEquipment, ISection
    {
        public MixingSection()
            : base("Mixing Section")
        {

        }

        public MixingSection(string name)
            : base(name)
        {

        }
        public MixingSection(System.Text.Json.Nodes.JsonObject jsonObject)

            : base(jsonObject)

        {

        }

        public MixingSection(MixingSection mixingSection)
            : base(mixingSection)
        {

        }

        public MixingSection(Guid guid, string name)
            : base(guid, name)
        {

        }

    }
}
