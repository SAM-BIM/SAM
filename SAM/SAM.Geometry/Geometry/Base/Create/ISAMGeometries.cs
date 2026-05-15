// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;
using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace SAM.Geometry
{
    public static partial class Create
    {
        public static List<T> ISAMGeometries<T>(this JsonArray jsonArray) where T : ISAMGeometry
        {
            return Core.Create.IJSAMObjects<T>(jsonArray);
        }
    }
}
