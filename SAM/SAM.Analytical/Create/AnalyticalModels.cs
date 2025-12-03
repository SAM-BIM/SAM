using SAM.Analytical.Classes;
using SAM.Core;
using SAM.Weather;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SAM.Analytical
{
    public static partial class Create
    {
        public static List<AnalyticalModel> AnalyticalModels(this AnalyticalModel analyticalModel, Cases cases)
        {
            if (analyticalModel == null || cases == null)
            {
                return null;
            }

            List<AnalyticalModel> result = [];
            foreach (Case @case in cases)
            {
                AnalyticalModel analyticalModel_Temp = AnalyticalModel(analyticalModel, @case);
                if (analyticalModel_Temp is null)
                {
                    continue;
                }

                result.Add(analyticalModel_Temp);
            }

            return result;
        }

        public static List<AnalyticalModel> AnalyticalModels(this AnalyticalModel analyticalModel, IEnumerable<Cases> cases)
        {
            if (analyticalModel == null || cases == null)
            {
                return null;
            }

            List<AnalyticalModel> result = [];
            foreach (Cases cases_Temp in cases)
            {
                if (AnalyticalModels(analyticalModel, cases_Temp) is not List<AnalyticalModel> analyticalModels)
                {
                    continue;
                }

                result.AddRange(analyticalModels);
            }

            return result;
        }

    }
}