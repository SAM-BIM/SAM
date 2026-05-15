// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace SAM.Geometry.Spatial
{
    public static partial class Create
    {
        public static List<ICurve3D> ICurve3Ds(this JsonArray jsonArray)
        {
            if (jsonArray == null)
                return null;

            List<ICurve3D> result = new List<ICurve3D>();

            foreach (JsonNode jsonNode in jsonArray)
                result.Add(ICurve3D(jsonNode as JsonObject));

            return result;
        }
    }
}
