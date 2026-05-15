// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;
using SAM.Core;
using System;
using System.Collections.Generic;

namespace SAM.Analytical
{
    public class InternalConditionLibrary : SAMLibrary<InternalCondition>, IAnalyticalObject
    {
        public InternalConditionLibrary(string name)
            : base(name)
        {

        }

        public InternalConditionLibrary(Guid guid, string name)
            : base(guid, name)
        {

        }

        public InternalConditionLibrary(JObject jObject)
            : base(jObject?.Node as System.Text.Json.Nodes.JsonObject)
        {

        }


        public InternalConditionLibrary(System.Text.Json.Nodes.JsonObject jsonObject)

            : base(jsonObject)

        {

        }

        public InternalConditionLibrary(InternalConditionLibrary internalConditionLibrary)
            : base(internalConditionLibrary)
        {

        }

        public override string GetUniqueId(InternalCondition internalCondition)
        {
            if (internalCondition == null)
                return null;

            return internalCondition.Name;
        }

        public override bool IsValid(InternalCondition internalCondition)
        {
            if (!base.IsValid(internalCondition))
                return false;

            return true;
        }

        public List<InternalCondition> GetInternalConditions()
        {
            return GetObjects<InternalCondition>();
        }

        public List<InternalCondition> GetInternalConditions(string text, TextComparisonType textComparisonType = TextComparisonType.Equals, bool caseSensitive = true)
        {
            if (string.IsNullOrWhiteSpace(text))
                return null;

            List<InternalCondition> internalConditions = GetInternalConditions();
            if (internalConditions == null || internalConditions.Count == 0)
                return null;

            return internalConditions.FindAll(x => Core.Query.Compare(x.Name, text, textComparisonType, caseSensitive));
        }
    }
}
