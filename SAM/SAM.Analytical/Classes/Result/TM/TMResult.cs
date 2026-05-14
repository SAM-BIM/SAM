// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;
using SAM.Core;
using System;
using System.Text.Json.Nodes;

namespace SAM.Analytical
{
    public abstract class TMResult : Result, IAnalyticalObject
    {
        private TM52BuildingCategory tM52BuildingCategory;

        public TMResult(string name, string source, string reference, TM52BuildingCategory tM52BuildingCategory)
            : base(name, source, reference)
        {
            this.tM52BuildingCategory = tM52BuildingCategory;
        }

        public TMResult(Guid guid, string name, string source, string reference, TM52BuildingCategory tM52BuildingCategory)
            : base(guid, name, source, reference)
        {
            this.tM52BuildingCategory = tM52BuildingCategory;
        }

        public TMResult(TMResult tMResult)
            : base(tMResult)
        {
            if (tMResult != null)
            {
                tM52BuildingCategory = tMResult.tM52BuildingCategory;
            }
        }

        public TMResult(Guid guid, TMResult tMResult)
            : base(guid, tMResult)
        {
            if (tMResult != null)
            {
                tM52BuildingCategory = tMResult.tM52BuildingCategory;
            }
        }

        public TMResult(JObject jObject)
            : base(jObject)
        {

        }

        public TM52BuildingCategory TM52BuildingCategory
        {
            get
            {
                return tM52BuildingCategory;
            }
        }

        public abstract int OccupiedHours { get; }

        public abstract int MaxExceedableHours { get; }

        public abstract bool Pass { get; }

        public override bool FromJsonObject(JsonObject jsonObject)
        {
            if (!base.FromJsonObject(jsonObject))
            {
                return false;
            }

            if (jsonObject.ContainsKey("TM52BuildingCategory"))
            {
                tM52BuildingCategory = Core.Query.Enum<TM52BuildingCategory>(jsonObject["TM52BuildingCategory"]?.GetValue<string>());
            }

            return true;
        }

        public override JsonObject ToJsonObject()
        {
            JsonObject result = base.ToJsonObject();
            if (result == null)
            {
                return null;
            }

            result["TM52BuildingCategory"] = tM52BuildingCategory.ToString();

            return result;
        }
    }
}
