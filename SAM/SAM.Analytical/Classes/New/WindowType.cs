// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;
using SAM.Architectural;
using System.Collections.Generic;

namespace SAM.Analytical
{
    public class WindowType : OpeningType
    {
        public WindowType(WindowType windowType)
            : base(windowType)
        {

        }

        public WindowType(WindowType windowType, string name)
            : base(windowType, name)
        {

        }

        public WindowType(JObject jObject)
            : base(jObject)
        {

        }

        public WindowType(string name)
            : base(name)
        {

        }

        public WindowType(System.Guid guid, string name)
            : base(guid, name)
        {

        }

        public WindowType(string name, IEnumerable<MaterialLayer> paneMaterialLayers, IEnumerable<MaterialLayer> frameMaterialLayers = null)
            : base(name, paneMaterialLayers, frameMaterialLayers)
        {

        }

        public WindowType(System.Guid guid, string name, IEnumerable<MaterialLayer> paneMaterialLayers, IEnumerable<MaterialLayer> frameMaterialLayers = null)
            : base(guid, name, paneMaterialLayers, frameMaterialLayers)
        {

        }

    }
}
