// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;
using SAM.Core;
using System.Collections;
using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace SAM.Analytical
{
    public class CaseDataCollection : IJSAMObject, IAnalyticalObject, IEnumerable<CaseData>
    {
        private List<CaseData> caseDatas = [];

        public CaseDataCollection()
        {

        }

        public CaseDataCollection(CaseDataCollection caseDataCollection)
        {
            if (caseDataCollection != null)
            {
                foreach (CaseData caseData in caseDataCollection)
                {
                    if (caseData.Clone() is CaseData caseData_Temp)
                    {
                        this.caseDatas.Add(caseData_Temp);
                    }
                }
            }
        }

        public CaseDataCollection(IEnumerable<CaseData> caseDatas)
        {
            if (caseDatas != null)
            {
                foreach (CaseData caseData in caseDatas)
                {
                    if (caseData.Clone() is CaseData caseData_Temp)
                    {
                        this.caseDatas.Add(caseData_Temp);
                    }
                }
            }
        }

        public CaseDataCollection(JObject jObject)
        {
            FromJObject(jObject);
        }

        public bool FromJObject(JObject jObject)
        {
            return FromJsonObject(jObject?.Node as JsonObject);
        }

        public bool FromJsonObject(JsonObject jsonObject)
        {
            if (jsonObject == null)
            {
                return false;
            }

            if (jsonObject["CaseDatas"] is JsonArray caseDatasArray)
            {
                foreach (JsonNode node in caseDatasArray)
                {
                    if (node is JsonObject caseDataJson)
                    {
                        if (Core.Query.IJSAMObject<CaseData>(caseDataJson as JsonObject) is CaseData caseData)
                        {
                            caseDatas.Add(caseData);
                        }
                    }
                }
            }

            return true;
        }

        public List<CaseData> Values
        {
            get
            {
                return caseDatas?.ConvertAll(x => x.Clone());
            }
        }

        public bool Add(CaseData caseData)
        {
            if (caseData is null)
            {
                return false;
            }

            if (caseData.Clone() is CaseData caseData_Temp)
            {
                caseDatas.Add(caseData_Temp);
                return true;
            }

            return false;
        }

        public IEnumerator<CaseData> GetEnumerator()
        {
            return caseDatas.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public JObject ToJObject()
        {
            JsonObject jsonObject = ToJsonObject();
            return jsonObject == null ? null : new JObject(jsonObject);
        }

        public JsonObject ToJsonObject()
        {
            JsonObject jsonObject = new JsonObject
            {
                ["_type"] = Core.Query.FullTypeName(this)
            };

            if (caseDatas != null)
            {
                JsonArray caseDatasArray = new JsonArray();
                foreach (CaseData caseData in caseDatas)
                {
                    if (caseData?.ToJsonObject() is JsonObject caseDataJson)
                    {
                        caseDatasArray.Add(caseDataJson.DeepClone());
                    }
                }

                jsonObject["CaseDatas"] = caseDatasArray;
            }

            return jsonObject;
        }
    }
}
