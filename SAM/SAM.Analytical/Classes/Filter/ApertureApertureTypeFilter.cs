// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;
using SAM.Core;

namespace SAM.Analytical
{
    public class ApertureApertureTypeFilter : EnumFilter<ApertureType>
    {

        public ApertureApertureTypeFilter(ApertureType apertureType)
            : base()
        {
            Value = apertureType;
        }

        public ApertureApertureTypeFilter(ApertureApertureTypeFilter apertureApertureTypeFilter)
            : base(apertureApertureTypeFilter)
        {

        }

        public ApertureApertureTypeFilter(JObject jObject)
            : base(jObject)
        {

        }

        public override bool TryGetEnum(IJSAMObject jSAMObject, out ApertureType apertureType)
        {
            apertureType = ApertureType.Undefined;

            Aperture aperture = jSAMObject as Aperture;
            if (aperture == null)
            {
                return false;
            }

            apertureType = aperture.ApertureType;
            return true;
        }
    }
}
