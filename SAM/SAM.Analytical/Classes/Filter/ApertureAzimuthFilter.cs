// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;
using SAM.Core;

namespace SAM.Analytical
{
    public class ApertureAzimuthFilter : NumberFilter
    {
        public ApertureAzimuthFilter(NumberComparisonType numberComparisonType, double value)
            : base(numberComparisonType, value)
        {

        }

        public ApertureAzimuthFilter(ApertureAzimuthFilter apertureAzimuthFilter)
            : base(apertureAzimuthFilter)
        {

        }

        public ApertureAzimuthFilter(JObject jObject)
            : base(jObject)
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

            number = Query.Azimuth(aperture);
            return !double.IsNaN(number);
        }
    }
}
