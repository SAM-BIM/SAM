// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;
using System;
using System.Text.Json.Nodes;

namespace SAM.Analytical
{
    public class TM59NaturalVentilationResult : TM59Result
    {
        private int hoursExceedingComfortRange;
        private int summerOccupiedHours;
        private int maxExceedableSummerHours;

        public TM59NaturalVentilationResult(
            string name,
            string source,
            string reference,
            TM52BuildingCategory tM52BuildingCategory,
            int occupiedHours,
            int maxExceedableHours,
            int summerOccupiedHours,
            int maxExceedableSummerHours,
            int hoursExceedingComfortRange,
            bool pass,
            params TM59SpaceApplication[] tM59SpaceApplications)
            : base(name, source, reference, tM52BuildingCategory, occupiedHours, maxExceedableHours, pass, tM59SpaceApplications)
        {
            this.hoursExceedingComfortRange = hoursExceedingComfortRange;
            this.summerOccupiedHours = summerOccupiedHours;
            this.maxExceedableSummerHours = maxExceedableSummerHours;
        }

        public TM59NaturalVentilationResult(
            Guid guid,
            string name,
            string source,
            string reference,
            TM52BuildingCategory tM52BuildingCategory,
            int occupiedHours,
            int maxExceedableHours,
            int summerOccupiedHours,
            int maxExceedableSummerHours,
            int hoursExceedingComfortRange,
            bool pass,
            params TM59SpaceApplication[] tM59SpaceApplications)
            : base(guid, name, source, reference, tM52BuildingCategory, occupiedHours, maxExceedableHours, pass, tM59SpaceApplications)
        {
            this.hoursExceedingComfortRange = hoursExceedingComfortRange;
            this.summerOccupiedHours = summerOccupiedHours;
            this.maxExceedableSummerHours = maxExceedableSummerHours;
        }

        public int HoursExceedingComfortRange
        {
            get
            {
                return hoursExceedingComfortRange;
            }
        }

        public int SummerOccupiedHours
        {
            get
            {
                return summerOccupiedHours;
            }
        }

        public int MaxExceedableSummerHours
        {
            get
            {
                return maxExceedableSummerHours;
            }
        }

        public override bool FromJsonObject(JsonObject jsonObject)
        {
            if (!base.FromJsonObject(jsonObject))
            {
                return false;
            }

            if (jsonObject.ContainsKey("HoursExceedingComfortRange"))
            {
                hoursExceedingComfortRange = jsonObject["HoursExceedingComfortRange"]?.GetValue<int>() ?? 0;
            }

            if (jsonObject.ContainsKey("SummerOccupiedHours"))
            {
                summerOccupiedHours = jsonObject["SummerOccupiedHours"]?.GetValue<int>() ?? 0;
            }

            if (jsonObject.ContainsKey("MaxExceedableSummerHours"))
            {
                maxExceedableSummerHours = jsonObject["MaxExceedableSummerHours"]?.GetValue<int>() ?? 0;
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

            if (hoursExceedingComfortRange != int.MinValue)
            {
                result["HoursExceedingComfortRange"] = hoursExceedingComfortRange;
            }

            if (summerOccupiedHours != int.MinValue)
            {
                result["SummerOccupiedHours"] = summerOccupiedHours;
            }

            if (maxExceedableSummerHours != int.MinValue)
            {
                result["MaxExceedableSummerHours"] = maxExceedableSummerHours;
            }

            return result;
        }
    }
}
