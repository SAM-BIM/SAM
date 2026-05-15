// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;
using SAM.Core;

namespace SAM.Analytical
{
    public class AperturePaneAreaFilter : NumberFilter
    {
        public AperturePaneAreaFilter(NumberComparisonType numberComparisonType, double value)
            : base(numberComparisonType, value)
        {

        }

        public AperturePaneAreaFilter(AperturePaneAreaFilter aperturePaneAreaFilter)
            : base(aperturePaneAreaFilter)
        {

        }

        public AperturePaneAreaFilter(JObject jObject)
            : base(jObject?.Node as System.Text.Json.Nodes.JsonObject)
        {

        }


        public AperturePaneAreaFilter(System.Text.Json.Nodes.JsonObject jsonObject)

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

            number = aperture.GetPaneArea();
            return !double.IsNaN(number);
        }
    }
}
