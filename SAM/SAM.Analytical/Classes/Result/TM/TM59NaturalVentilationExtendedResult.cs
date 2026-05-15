// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core;
using System.Collections.Generic;

namespace SAM.Analytical
{
    public class TM59NaturalVentilationExtendedResult : TM59ExtendedResult
    {
        public TM59NaturalVentilationExtendedResult(string name, string source, string reference, TM52BuildingCategory tM52BuildingCategory, HashSet<int> occupiedHourIndices, IndexedDoubles minAcceptableTemperatures, IndexedDoubles maxAcceptableTemperatures, IndexedDoubles operativeTemperatures, params TM59SpaceApplication[] tM59SpaceApplications)
            : base(name, source, reference, tM52BuildingCategory, occupiedHourIndices, minAcceptableTemperatures, maxAcceptableTemperatures, operativeTemperatures, tM59SpaceApplications)
        {

        }

        public TM59NaturalVentilationExtendedResult(TM59NaturalVentilationExtendedResult tM59NaturalVentilationExtendedResult)
            : base(tM59NaturalVentilationExtendedResult)
        {

        }

        public TM59NaturalVentilationExtendedResult(TM59NaturalVentilationExtendedResult tM59NaturalVentilationExtendedResult, HashSet<int> occupiedHourIndices, IndexedDoubles minAcceptableTemperatures, IndexedDoubles maxAcceptableTemperatures, IndexedDoubles operativeTemperatures)
            : base(tM59NaturalVentilationExtendedResult, occupiedHourIndices, minAcceptableTemperatures, maxAcceptableTemperatures, operativeTemperatures)
        {

        }
        public TM59NaturalVentilationExtendedResult(System.Text.Json.Nodes.JsonObject jsonObject)

            : base(jsonObject)

        {

        }

        public HashSet<int> GetSummerOccupiedHourIndices()
        {
            HashSet<int> occupiedHourIndices = OccupiedHourIndices;
            if (occupiedHourIndices == null)
            {
                return null;
            }

            HashSet<int> result = new HashSet<int>();
            foreach (int occupiedHourIndex in occupiedHourIndices)
            {
                if (occupiedHourIndex >= HourOfYear.SummerStartIndex && occupiedHourIndex <= HourOfYear.SummerEndIndex)
                {
                    result.Add(occupiedHourIndex);
                }
            }

            return result;
        }

        public int GetSummerOccupiedHours()
        {
            return GetSummerOccupiedHourIndices().Count;
        }

        public int GetSummerMaxExceedableHours()
        {
            return System.Convert.ToInt32(System.Math.Truncate(GetSummerOccupiedHours() * 0.03));
        }

    }
}
