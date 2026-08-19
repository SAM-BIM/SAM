// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core;
using System.Collections.Generic;

namespace SAM.Analytical
{
    public class TM59CorridorExtendedResult : TM59ExtendedResult
    {
        public TM59CorridorExtendedResult(string name, string source, string reference, TM52BuildingCategory tM52BuildingCategory, HashSet<int> occupiedHourIndices, IndexedDoubles minAcceptableTemperatures, IndexedDoubles maxAcceptableTemperatures, IndexedDoubles operativeTemperatures)
            : base(name, source, reference, tM52BuildingCategory, occupiedHourIndices, minAcceptableTemperatures, maxAcceptableTemperatures, operativeTemperatures, TM59SpaceApplication.Undefined)
        {

        }

        public TM59CorridorExtendedResult(TM59CorridorExtendedResult tM59CorridorExtendedResult)
            : base(tM59CorridorExtendedResult)
        {

        }

        public TM59CorridorExtendedResult(TM59CorridorExtendedResult tM59CorridorExtendedResult, HashSet<int> occupiedHourIndices, IndexedDoubles minAcceptableTemperatures, IndexedDoubles maxAcceptableTemperatures, IndexedDoubles operativeTemperatures)
            : base(tM59CorridorExtendedResult, occupiedHourIndices, minAcceptableTemperatures, maxAcceptableTemperatures, operativeTemperatures)
        {

        }
        public TM59CorridorExtendedResult(System.Text.Json.Nodes.JsonObject jsonObject)

            : base(jsonObject)

        {

        }

        /// <summary>
        /// The number of hours in the operative-temperature series this check was evaluated over - the
        /// real annual basis <see cref="MaxExceedableHours"/> and <see cref="GetHoursNumberExceeding28()"/>
        /// already derive their counts from, exposed on its own so a report can state the basis it used
        /// rather than reconstruct it by dividing a truncated <see cref="MaxExceedableHours"/> back through
        /// <see cref="TMExtendedResult.ExceedanceFactor"/>.
        /// </summary>
        public int GetAnnualHours()
        {
            IndexedDoubles operativeTemperatures = OperativeTemperatures;
            if (operativeTemperatures == null)
            {
                return -1;
            }

            if (operativeTemperatures.Count <= 0)
            {
                return 0;
            }

            if (operativeTemperatures.GetMaxIndex() is not int maxIndex || operativeTemperatures.GetMinIndex() is not int minIndex)
            {
                return 0;
            }

            return maxIndex - minIndex + 1;
        }

        public int GetHoursNumberExceeding28()
        {
            IndexedDoubles operativeTemperatures = OperativeTemperatures;
            if (operativeTemperatures == null)
            {
                return -1;
            }

            if (operativeTemperatures.Count <= 0)
            {
                return 0;
            }

            if (operativeTemperatures.GetMaxIndex() is not int maxIndex || operativeTemperatures.GetMinIndex() is not int minIndex)
            {
                return 0;
            }

            int count = 0;
            foreach (double operativeTemperature in operativeTemperatures)
            {
                if (operativeTemperature > 28)
                {
                    count++;
                }
            }

            return count;
        }

        public override int MaxExceedableHours
        {
            get
            {
                IndexedDoubles operativeTemperatures = OperativeTemperatures;
                if (operativeTemperatures == null)
                {
                    return -1;
                }

                if(operativeTemperatures.Count <= 0)
                {
                    return 0;
                }

                if (operativeTemperatures.GetMaxIndex() is not int maxIndex || operativeTemperatures.GetMinIndex() is not int minIndex)
                {
                    return 0;
                }

                int count = maxIndex - minIndex + 1;
                return System.Convert.ToInt32(System.Math.Truncate(count * ExceedanceFactor));
            }
        }

        public override bool Criterion1
        {
            get
            {
                return GetHoursNumberExceeding28() < MaxExceedableHours;
            }
        }

    }
}
