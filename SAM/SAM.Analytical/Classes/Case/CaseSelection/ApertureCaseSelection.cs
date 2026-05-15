// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Collections.Generic;

namespace SAM.Analytical
{
    public class ApertureCaseSelection : SAMObjectCaseSelection<Aperture>
    {
        public ApertureCaseSelection()
            : base()
        {
        }

        public ApertureCaseSelection(IEnumerable<Aperture> apertures)
            : base(apertures)
        {
        }
        public ApertureCaseSelection(System.Text.Json.Nodes.JsonObject jsonObject)
            : base(jsonObject)
        {
        }
    }
}
