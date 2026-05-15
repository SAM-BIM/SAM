// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;
using System;

namespace SAM.Core
{
    public class Group : SAMObject
    {
        public Group(string name)
            : base(name)
        {

        }

        public Group(Guid guid, string name)
            : base(guid, name)
        {

        }

        public Group(Group group)
            : base(group)
        {

        }

        public Group(Group group, string name)
            : base(name, group)
        {

        }
        public Group(System.Text.Json.Nodes.JsonObject jsonObject)

            : base(jsonObject)

        {

        }

    }
}
