// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;
using System;
using System.Text.Json.Nodes;

namespace SAM.Analytical
{
    public class TM59CorridorResult : TM59Result
    {
        private int hoursExceeding28;

        public TM59CorridorResult(
            string name,
            string source,
            string reference,
            TM52BuildingCategory tM52BuildingCategory,
            int occupiedHours,
            int maxExceedableHours,
            int hoursExceeding28,
            bool pass)
            : base(name, source, reference, tM52BuildingCategory, occupiedHours, maxExceedableHours, pass, TM59SpaceApplication.Undefined)
        {
            this.hoursExceeding28 = hoursExceeding28;
        }

        public TM59CorridorResult(
            Guid guid,
            string name,
            string source,
            string reference,
            TM52BuildingCategory tM52BuildingCategory,
            int occupiedHours,
            int maxExceedableHours,
            int hoursExceeding28,
            bool pass)
            : base(guid, name, source, reference, tM52BuildingCategory, occupiedHours, maxExceedableHours, pass, TM59SpaceApplication.Undefined)
        {
            this.hoursExceeding28 = hoursExceeding28;
        }

        public int HoursExceeding28
        {
            get
            {
                return hoursExceeding28;
            }
        }


        public override bool FromJsonObject(JsonObject jsonObject)
        {
            if (!base.FromJsonObject(jsonObject))
            {
                return false;
            }

            if (jsonObject.ContainsKey("HoursExceeding28"))
            {
                hoursExceeding28 = jsonObject["HoursExceeding28"]?.GetValue<int>() ?? 0;
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

            if (hoursExceeding28 != int.MinValue)
            {
                result["HoursExceeding28"] = hoursExceeding28;
            }

            return result;
        }
    }
}
