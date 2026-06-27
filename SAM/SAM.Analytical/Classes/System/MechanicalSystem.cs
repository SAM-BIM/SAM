// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core;
using System.Text.Json.Nodes;

namespace SAM.Analytical
{
    public abstract class MechanicalSystem : SAMInstance<MechanicalSystemType>, ISystem, IAnalyticalObject
    {
        private string id;

        public MechanicalSystem(string id, MechanicalSystemType mechanicalSystemType)
            : base(mechanicalSystemType)
        {
            this.id = id;
        }

        public MechanicalSystem(string prefix, string id, MechanicalSystemType mechanicalSystemType)
            : base(prefix, mechanicalSystemType)
        {
            this.id = id;
        }

        public MechanicalSystem(System.Guid guid, string id, MechanicalSystem mechanicalSystem)
            : base(guid, mechanicalSystem)
        {
            this.id = id;
        }

        public MechanicalSystem(MechanicalSystem mechanicalSystem)
            : base(mechanicalSystem)
        {
            id = mechanicalSystem?.id;
        }
        public MechanicalSystem(System.Text.Json.Nodes.JsonObject jsonObject)

            : base(jsonObject)

        {

        }

        public string Id
        {
            get
            {
                return id;
            }
        }

        public string FullName
        {
            get
            {
                return string.Format("{0} {1}", name == null ? string.Empty : name, id == null ? string.Empty : id).Trim();
            }
        }

        public override bool FromJsonObject(JsonObject jsonObject)
        {
            if (!base.FromJsonObject(jsonObject))
                return false;

            if (jsonObject.ContainsKey("Id"))
            {
                id = jsonObject["Id"]?.GetValue<string>();
            }

            return true;
        }

        public override JsonObject ToJsonObject()
        {
            JsonObject jsonObject = base.ToJsonObject();
            if (jsonObject == null)
                return null;

            if (id != null)
                jsonObject["Id"] = id;

            return jsonObject;
        }
    }
}
