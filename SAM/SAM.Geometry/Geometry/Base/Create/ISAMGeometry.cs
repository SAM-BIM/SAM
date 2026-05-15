// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;
using System.Text.Json.Nodes;

namespace SAM.Geometry
{
    public static partial class Create
    {
        public static ISAMGeometry ISAMGeometry(this JObject jObject)
        {
            return Core.Create.IJSAMObject(jObject) as ISAMGeometry;
        }

        public static ISAMGeometry ISAMGeometry(this JsonObject jsonObject)
        {
            return Core.Create.IJSAMObject(jsonObject) as ISAMGeometry;
        }

        public static T ISAMGeometry<T>(this JObject jObject) where T : ISAMGeometry
        {
            return Core.Create.IJSAMObject<T>(jObject);
        }

        public static T ISAMGeometry<T>(this JsonObject jsonObject) where T : ISAMGeometry
        {
            return Core.Create.IJSAMObject<T>(jsonObject);
        }
    }
}
