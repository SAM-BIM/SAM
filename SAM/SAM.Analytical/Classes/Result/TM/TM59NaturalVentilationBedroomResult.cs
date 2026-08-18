// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Text.Json.Nodes;

namespace SAM.Analytical
{
    public class TM59NaturalVentilationBedroomResult : TM59NaturalVentilationResult
    {
        private int annualNightOccupiedHours;

        private int maxExceedableNightHours;
        private int nightHoursNumberExceeding26;

        public TM59NaturalVentilationBedroomResult(
            string name,
            string source,
            string reference,
            TM52BuildingCategory tM52BuildingCategory,
            int occupiedHours,
            int maxExceedableHours,
            int hoursExceedingComfortRange,
            int annualNightOccupiedHours,
            int summerOccupiedHours,
            int maxExceedableSummerHours,
            int maxExceedableNightHours,
            int nightHoursNumberExceeding26,
            bool pass)
            : base(name, source, reference, tM52BuildingCategory, occupiedHours, maxExceedableHours, summerOccupiedHours, maxExceedableSummerHours, hoursExceedingComfortRange, pass, TM59SpaceApplication.Sleeping)
        {
            this.annualNightOccupiedHours = annualNightOccupiedHours;
            this.maxExceedableNightHours = maxExceedableNightHours;
            this.nightHoursNumberExceeding26 = nightHoursNumberExceeding26;
        }

        public TM59NaturalVentilationBedroomResult(
            Guid guid,
            string name,
            string source,
            string reference,
            TM52BuildingCategory tM52BuildingCategory,
            int occupiedHours,
            int maxExceedableHours,
            int hoursExceedingComfortRange,
            int annualNightOccupiedHours,
            int summerOccupiedHours,
            int maxExceedableSummerHours,
            int maxExceedableNightHours,
            int nightHoursNumberExceeding26,
            bool pass)
            : base(guid, name, source, reference, tM52BuildingCategory, occupiedHours, maxExceedableHours, summerOccupiedHours, maxExceedableSummerHours, hoursExceedingComfortRange, pass, TM59SpaceApplication.Sleeping)
        {
            this.annualNightOccupiedHours = annualNightOccupiedHours;
            this.maxExceedableNightHours = maxExceedableNightHours;
            this.nightHoursNumberExceeding26 = nightHoursNumberExceeding26;
        }

        public int AnnualNightOccupiedHours
        {
            get
            {
                return annualNightOccupiedHours;
            }
        }

        public int MaxExceedableNightHours
        {
            get
            {
                return maxExceedableNightHours;
            }
        }

        public int NightHoursNumberExceeding26
        {
            get
            {
                return nightHoursNumberExceeding26;
            }
        }

        /// <summary>
        /// The TM59 bedroom night-time criterion, restated on the simplified result from the two figures it
        /// already carries - the same comparison, in the same direction, as
        /// <see cref="TM59NaturalVentilationBedroomExtendedResult.Criterion2"/>.
        /// <para>
        /// <b>Why it had to be added.</b> <c>Pass</c> on every TM59 result is Criterion 1 alone
        /// (<c>TMExtendedResult.Pass =&gt; Criterion1</c>); Criterion 2 is never folded into it. So a caller
        /// holding a simplified result - which is what <c>TM59AssessmentCalculator.Calculate</c> returns unless
        /// extended results were asked for - had the night-time hours and the night-time limit but no statement
        /// of the verdict, and would have had to restate the comparison itself. Restating it outside the domain
        /// is how the two copies drift, and this criterion is <b>inclusive</b> where the others are strict,
        /// which is exactly the detail a restatement gets wrong.
        /// </para>
        /// </summary>
        public bool Criterion2
        {
            get
            {
                return maxExceedableNightHours >= nightHoursNumberExceeding26;
            }
        }

        public override bool FromJsonObject(JsonObject jsonObject)
        {
            if (!base.FromJsonObject(jsonObject))
            {
                return false;
            }

            if (jsonObject.ContainsKey("AnnualNightOccupiedHours"))
            {
                annualNightOccupiedHours = jsonObject["AnnualNightOccupiedHours"]?.GetValue<int>() ?? 0;
            }

            if (jsonObject.ContainsKey("MaxExceedableNightHours"))
            {
                maxExceedableNightHours = jsonObject["MaxExceedableNightHours"]?.GetValue<int>() ?? 0;
            }

            if (jsonObject.ContainsKey("NightHoursNumberExceeding26"))
            {
                nightHoursNumberExceeding26 = jsonObject["NightHoursNumberExceeding26"]?.GetValue<int>() ?? 0;
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

            if (annualNightOccupiedHours != int.MinValue)
            {
                result["AnnualNightOccupiedHours"] = annualNightOccupiedHours;
            }

            if (maxExceedableNightHours != int.MinValue)
            {
                result["MaxExceedableNightHours"] = maxExceedableNightHours;
            }

            if (nightHoursNumberExceeding26 != int.MinValue)
            {
                result["NightHoursNumberExceeding26"] = nightHoursNumberExceeding26;
            }

            return result;
        }
    }
}
