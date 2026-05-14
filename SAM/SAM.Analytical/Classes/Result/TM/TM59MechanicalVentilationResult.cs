// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;
using System;
using System.Text.Json.Nodes;

namespace SAM.Analytical
{
    public class TM59MechanicalVentilationResult : TM59Result
    {
        private int hoursExceeding26;

        public TM59MechanicalVentilationResult(
            string name,
            string source,
            string reference,
            TM52BuildingCategory tM52BuildingCategory,
            int occupiedHours,
            int maxExceedableHours,
            int hoursExceeding26,
            bool pass,
            params TM59SpaceApplication[] tM59SpaceApplications)
            : base(name, source, reference, tM52BuildingCategory, occupiedHours, maxExceedableHours, pass, tM59SpaceApplications)
        {
            this.hoursExceeding26 = hoursExceeding26;
        }

        public TM59MechanicalVentilationResult(
            Guid guid,
            string name,
            string source,
            string reference,
            TM52BuildingCategory tM52BuildingCategory,
            int occupiedHours,
            int maxExceedableHours,
            int hoursExceeding26,
            bool pass,
            params TM59SpaceApplication[] tM59SpaceApplications)
            : base(guid, name, source, reference, tM52BuildingCategory, occupiedHours, maxExceedableHours, pass, tM59SpaceApplications)
        {
            this.hoursExceeding26 = hoursExceeding26;
        }

        public int HoursExceeding26
        {
            get
            {
                return hoursExceeding26;
            }
        }


        public override bool FromJsonObject(JsonObject jsonObject)
        {
            if (!base.FromJsonObject(jsonObject))
            {
                return false;
            }

            if (jsonObject.ContainsKey("HoursExceeding26"))
            {
                hoursExceeding26 = jsonObject["HoursExceeding26"]?.GetValue<int>() ?? 0;
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

            if (hoursExceeding26 != int.MinValue)
            {
                result["HoursExceeding26"] = hoursExceeding26;
            }

            return result;
        }
    }
}
