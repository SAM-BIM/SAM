// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;
using System.Text.Json.Nodes;

namespace SAM.Geometry.Spatial
{
    public static partial class Create
    {
        public static ICurve3D ICurve3D(this JObject jObject)
        {
            return Geometry.Create.ISAMGeometry(jObject) as ICurve3D;
        }

        public static ICurve3D ICurve3D(this JsonObject jsonObject)
        {
            return Geometry.Create.ISAMGeometry(jsonObject) as ICurve3D;
        }
    }
}
