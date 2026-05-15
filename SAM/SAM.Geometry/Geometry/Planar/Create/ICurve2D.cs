// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;
using System.Text.Json.Nodes;

namespace SAM.Geometry.Planar
{
    public static partial class Create
    {
        public static ICurve2D ICurve2D(this JObject jObject)
        {
            return Geometry.Create.ISAMGeometry(jObject) as ICurve2D;
        }

        public static ICurve2D ICurve2D(this JsonObject jsonObject)
        {
            return Geometry.Create.ISAMGeometry(jsonObject) as ICurve2D;
        }
    }
}
