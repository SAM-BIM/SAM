// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Text.Json.Nodes;

namespace SAM.Weather
{
    public class WeightingCalculationMethod : SimpleArithmeticMeanCalculationMethod
    {
        public double Alpha { get; set; } = 0.8;

        public WeightingCalculationMethod()
            : base()
        {

        }

        public WeightingCalculationMethod(int sequentialDays, double alpha)
            : base(sequentialDays)
        {
            Alpha = alpha;
        }

        public WeightingCalculationMethod(WeightingCalculationMethod weightingCalculationMethod)
            : base(weightingCalculationMethod)
        {
            if (weightingCalculationMethod != null)
            {
                Alpha = weightingCalculationMethod.Alpha;
            }
        }

        public WeightingCalculationMethod(JsonObject jsonObject)
        {
            FromJsonObject(jsonObject);
        }

        public override bool FromJsonObject(JsonObject jsonObject)
        {
            if (!base.FromJsonObject(jsonObject))
            {
                return false;
            }

            if (jsonObject.ContainsKey("Alpha"))
            {
                Alpha = jsonObject["Alpha"]?.GetValue<double>() ?? double.NaN;
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

            result["Alpha"] = Alpha;

            return result;
        }
    }
}
