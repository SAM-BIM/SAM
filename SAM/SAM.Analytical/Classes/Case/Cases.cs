// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace SAM.Analytical.Classes
{
    public class Cases : IJSAMObject, IAnalyticalObject, IEnumerable<Case>
    {
        private List<Case> values = [];

        public Cases()
        {

        }

        public Cases(IEnumerable<Case> cases)
        {
            values = cases == null ? [] : [.. cases];
        }
        public Cases(System.Text.Json.Nodes.JsonObject jsonObject)

        {

            FromJsonObject(jsonObject);

        }

        public Type BaseType
        {
            get
            {
                if (values == null || values.Count == 0)
                {
                    return null;
                }

                Type type = values[0].GetType();

                if (values.TrueForAll(x => x.GetType() == type))
                {
                    return type;
                }

                return null;
            }
        }

        public int Count
        {
            get
            {
                return values?.Count ?? 0;
            }
        }

        public Case this[int index]
        {
            get
            {
                if (values == null || index < 0 || index >= values.Count)
                {
                    return null;
                }
                return values[index];
            }
            set
            {
                if (values == null || index < 0 || index >= values.Count)
                {
                    return;
                }
                values[index] = value;
            }
        }

        public void Add(Case @case)
        {
            if (@case == null)
            {
                return;
            }

            if (values == null)
            {
                values = [];
            }

            values.Add(@case);
        }
        public virtual bool FromJsonObject(JsonObject jsonObject)
        {
            if (jsonObject == null)
            {
                return false;
            }

            if (jsonObject["Values"] is JsonArray valuesArray)
            {
                values = [];
                foreach (JsonNode node in valuesArray)
                {
                    if (node is JsonObject caseJson)
                    {
                        Case @case = Core.Query.IJSAMObject<Case>(caseJson as JsonObject);
                        if (@case == null)
                        {
                            continue;
                        }

                        values.Add(@case);
                    }
                }
            }

            return true;
        }

        public List<TCase> GetCases<TCase>()
        {
            if (values is null)
            {
                return null;
            }

            List<TCase> result = [];
            foreach (Case @case in values)
            {
                if (@case is TCase case_Temp)
                {
                    result.Add(case_Temp);
                }
            }

            return result;
        }

        public IEnumerator<Case> GetEnumerator()
        {
            return values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
        public virtual JsonObject ToJsonObject()
        {
            JsonObject result = new JsonObject
            {
                ["_type"] = Core.Query.FullTypeName(this)
            };

            if (values != null)
            {
                JsonArray valuesArray = new JsonArray();
                foreach (Case value in values)
                {
                    if (value?.ToJsonObject() is JsonObject valueJson)
                    {
                        valuesArray.Add(valueJson.DeepClone());
                    }
                }

                result["Values"] = valuesArray;
            }

            return result;
        }
    }
}
