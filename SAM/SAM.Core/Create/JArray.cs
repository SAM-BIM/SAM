// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;
using System.Collections.Generic;

namespace SAM.Core
{
    public static partial class Create
    {
        public static JArray JArray<T>(this IEnumerable<T> jSAMObjects) where T : IJSAMObject
        {
            if (jSAMObjects == null)
                return null;

            JArray jArray = new JArray();
            foreach (T t in jSAMObjects)
                jArray.Add(t?.ToJsonObject());

            return jArray;
        }
    }
}
