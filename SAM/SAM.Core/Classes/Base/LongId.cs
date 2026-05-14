// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;
using System.Text.Json.Nodes;

namespace SAM.Core
{
    public class LongId : ParameterizedSAMObject, IId
    {
        private long id;

        public LongId(long id)
            : base()
        {
            this.id = id;
        }

        public LongId(int id)
            : base()
        {
            this.id = id;
        }

        public LongId(LongId longId)
            : base(longId)
        {
            id = longId.id;
        }

        public LongId(JObject jObject)
            : base(jObject)
        {

        }

        public long Id
        {
            get
            {
                return id;
            }
        }

        public static implicit operator LongId(long id)
        {
            return new LongId(id);
        }

        public static implicit operator LongId(int id)
        {
            return new LongId(System.Convert.ToInt64(id));
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

            id = jsonObject["Id"]?.GetValue<long>() ?? 0;
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
