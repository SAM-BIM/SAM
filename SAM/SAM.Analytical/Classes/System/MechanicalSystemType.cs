// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;
using SAM.Core;
using System;
using System.Text.Json.Nodes;

namespace SAM.Analytical
{
    public abstract class MechanicalSystemType : SAMType, ISystemType, IAnalyticalObject
    {
        private string description;

        public MechanicalSystemType(string name, string description)
            : base(name)
        {
            this.description = description;
        }

        public MechanicalSystemType(Guid guid, string name, string description)
            : base(guid, name)
        {
            this.description = description;
        }

        public MechanicalSystemType(MechanicalSystemType mechanicalSystemType)
            : base(mechanicalSystemType)
        {
            description = mechanicalSystemType.description;
        }

        public MechanicalSystemType(JObject jObject)
            : base(jObject?.Node as System.Text.Json.Nodes.JsonObject)
        {
        }


        public MechanicalSystemType(System.Text.Json.Nodes.JsonObject jsonObject)

            : base(jsonObject)

        {

        }

        public string Description
        {
            get
            {
                return description;
            }
        }

        public override bool FromJsonObject(JsonObject jsonObject)
        {
            if (!base.FromJsonObject(jsonObject))
                return false;

            if (jsonObject.ContainsKey("Description"))
                description = jsonObject["Description"]?.GetValue<string>();

            return true;
        }

        public override JsonObject ToJsonObject()
        {
            JsonObject jsonObject = base.ToJsonObject();
            if (jsonObject == null)
                return null;

            if (description != null)
                jsonObject["Description"] = description;

            return jsonObject;
        }
    }
}
