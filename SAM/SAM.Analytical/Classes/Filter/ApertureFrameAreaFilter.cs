// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;
using SAM.Core;

namespace SAM.Analytical
{
    public class ApertureFrameAreaFilter : NumberFilter
    {
        public ApertureFrameAreaFilter(NumberComparisonType numberComparisonType, double value)
            : base(numberComparisonType, value)
        {

        }

        public ApertureFrameAreaFilter(ApertureFrameAreaFilter apertureFrameAreaFilter)
            : base(apertureFrameAreaFilter)
        {

        }

        public ApertureFrameAreaFilter(JObject jObject)
            : base(jObject?.Node as System.Text.Json.Nodes.JsonObject)
        {

        }


        public ApertureFrameAreaFilter(System.Text.Json.Nodes.JsonObject jsonObject)

            : base(jsonObject)

        {

        }

        public override bool TryGetNumber(IJSAMObject jSAMObject, out double number)
        {
            number = double.NaN;
            Aperture aperture = jSAMObject as Aperture;
            if (aperture == null)
            {
                return false;
            }

            number = aperture.GetFrameArea();
            return !double.IsNaN(number);
        }
    }
}
