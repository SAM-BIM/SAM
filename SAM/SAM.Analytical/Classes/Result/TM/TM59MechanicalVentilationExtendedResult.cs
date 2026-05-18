// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core;
using System.Collections.Generic;

namespace SAM.Analytical
{
    public class TM59MechanicalVentilationExtendedResult : TM59ExtendedResult
    {
        public TM59MechanicalVentilationExtendedResult(string name, string source, string reference, TM52BuildingCategory tM52BuildingCategory, HashSet<int> occupiedHourIndices, IndexedDoubles minAcceptableTemperatures, IndexedDoubles maxAcceptableTemperatures, IndexedDoubles operativeTemperatures, params TM59SpaceApplication[] tM59SpaceApplications)
            : base(name, source, reference, tM52BuildingCategory, occupiedHourIndices, minAcceptableTemperatures, maxAcceptableTemperatures, operativeTemperatures, tM59SpaceApplications)
        {

        }

        public TM59MechanicalVentilationExtendedResult(TM59MechanicalVentilationExtendedResult tM59MechanicalVentilationExtendedResult)
            : base(tM59MechanicalVentilationExtendedResult)
        {

        }

        public TM59MechanicalVentilationExtendedResult(TM59MechanicalVentilationExtendedResult tM59MechanicalVentilationExtendedResult, HashSet<int> occupiedHourIndices, IndexedDoubles minAcceptableTemperatures, IndexedDoubles maxAcceptableTemperatures, IndexedDoubles operativeTemperatures)
            : base(tM59MechanicalVentilationExtendedResult, occupiedHourIndices, minAcceptableTemperatures, maxAcceptableTemperatures, operativeTemperatures)
        {

        }
        public TM59MechanicalVentilationExtendedResult(System.Text.Json.Nodes.JsonObject jsonObject)

            : base(jsonObject)

        {

        }

        public int GetHoursNumberExceeding26()
        {
            HashSet<int> occupiedHourIndices = OccupiedHourIndices;
            if (occupiedHourIndices == null)
            {
                return -1;
            }

            IndexedDoubles operativeTemperatures = OperativeTemperatures;
            if (operativeTemperatures == null)
            {
                return -1;
            }

            int count = 0;
            foreach (int occupiedHourIndex in occupiedHourIndices)
            {
                double operativeTemperature = operativeTemperatures[occupiedHourIndex];
                if (operativeTemperature > 26)
                {
                    count++;
                }
            }

            return count;
        }

        public override bool Criterion1
        {
            get
            {
                return GetHoursNumberExceeding26() < MaxExceedableHours;
            }
        }

    }
}
