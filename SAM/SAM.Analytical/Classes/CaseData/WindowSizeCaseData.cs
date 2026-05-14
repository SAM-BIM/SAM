// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;
using System.Text.Json.Nodes;

namespace SAM.Analytical
{
    public class WindowSizeCaseData : BuiltInCaseData
    {
        private double apertureScaleFactor;

        public WindowSizeCaseData(double apertureScaleFactor)
            : base(nameof(WindowSizeCaseData))
        {
            this.apertureScaleFactor = apertureScaleFactor;
        }

        public WindowSizeCaseData(JObject jObject)
            : base(jObject)
        {

        }

        public WindowSizeCaseData(WindowSizeCaseData windowSizeCaseData)
            : base(windowSizeCaseData)
        {
            if (windowSizeCaseData != null)
            {
                apertureScaleFactor = windowSizeCaseData.apertureScaleFactor;
            }
        }

        public double ApertureScaleFactor
        {
            get
            {
                return apertureScaleFactor;
            }
        }

        protected override bool FromJsonObject(JsonObject jsonObject)
        {
            bool result = base.FromJsonObject(jsonObject);
            if (!result)
            {
                return false;
            }

            if (jsonObject.ContainsKey("ApertureScaleFactor"))
            {
                apertureScaleFactor = jsonObject["ApertureScaleFactor"]?.GetValue<double>() ?? double.NaN;
            }

            return result;
        }

        protected override JsonObject ToJsonObject()
        {
            JsonObject result = base.ToJsonObject();
            if (result is null)
            {
                return result;
            }

            if (!double.IsNaN(apertureScaleFactor))
            {
                result["ApertureScaleFactor"] = apertureScaleFactor;
            }

            return result;
        }
    }
}
