// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Text.Json.Nodes;

namespace SAM.Core
{
    public class IntegerId : ParameterizedSAMObject, IId
    {
        private int id;

        public IntegerId(int id)
            : base()
        {
            this.id = id;
        }

        public IntegerId(IntegerId? integerId)
            : base(integerId)
        {
            if(integerId is not null)
            {
                id = integerId.id;
            }
        }
        public IntegerId(System.Text.Json.Nodes.JsonObject jsonObject)

            : base(jsonObject)

        {

        }

        public int Id
        {
            get
            {
                return id;
            }
        }

        public static implicit operator IntegerId(int id)
        {
            return new IntegerId(id);
        }

        public override bool FromJsonObject(JsonObject? jsonObject)
        {
            if (jsonObject == null)
            {
                return false;
            }

            if (!base.FromJsonObject(jsonObject))
            {
                return false;
            }

            id = jsonObject["Id"]?.GetValue<int>() ?? 0;
            return true;
        }

        public override JsonObject? ToJsonObject()
        {
            JsonObject? result = base.ToJsonObject();
            if (result == null)
            {
                return result;
            }

            result["Id"] = id;

            return result;
        }
    }
}
