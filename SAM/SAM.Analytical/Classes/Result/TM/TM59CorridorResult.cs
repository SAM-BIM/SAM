// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Text.Json.Nodes;

namespace SAM.Analytical
{
    public class TM59CorridorResult : TM59Result
    {
        private int hoursExceeding28;

        //int.MinValue - the plain results' unset marker - where a caller built this without stating the
        //real annual series length, so a report reads it as absent rather than as a fabricated 0.
        private int annualHours = int.MinValue;

        public TM59CorridorResult(
            string name,
            string source,
            string reference,
            TM52BuildingCategory tM52BuildingCategory,
            int occupiedHours,
            int maxExceedableHours,
            int hoursExceeding28,
            bool pass,
            int annualHours = int.MinValue)
            : base(name, source, reference, tM52BuildingCategory, occupiedHours, maxExceedableHours, pass, TM59SpaceApplication.Undefined)
        {
            this.hoursExceeding28 = hoursExceeding28;
            this.annualHours = annualHours;
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
            bool pass,
            int annualHours = int.MinValue)
            : base(guid, name, source, reference, tM52BuildingCategory, occupiedHours, maxExceedableHours, pass, TM59SpaceApplication.Undefined)
        {
            this.hoursExceeding28 = hoursExceeding28;
            this.annualHours = annualHours;
        }

        public int HoursExceeding28
        {
            get
            {
                return hoursExceeding28;
            }
        }

        /// <summary>
        /// The number of hours in the annual series this check was evaluated over (typically 8760) - the
        /// real basis behind <c>MaxExceedableHours</c>, read directly off the calculation rather than
        /// reconstructed from it. <c>int.MinValue</c> where a caller never stated it.
        /// </summary>
        public int AnnualHours
        {
            get
            {
                return annualHours;
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

            if (jsonObject.ContainsKey("AnnualHours"))
            {
                annualHours = jsonObject["AnnualHours"]?.GetValue<int>() ?? int.MinValue;
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

            if (annualHours != int.MinValue)
            {
                result["AnnualHours"] = annualHours;
            }

            return result;
        }
    }
}
