// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;
using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace SAM.Core
{
    public abstract class ComplexReferenceFilter : Filter, ISAMObjectRelationClusterFilter
    {
        public ISAMObjectRelationCluster SAMObjectRelationCluster { get; set; }

        public IComplexReference ComplexReference { get; set; }

        public ComplexReferenceFilter(JObject jObject)
            : base(jObject)
        {
        }

        public ComplexReferenceFilter()
            : base()
        {
        }

        public ComplexReferenceFilter(ComplexReferenceFilter complexReferenceFilter)
            : base(complexReferenceFilter)
        {
            ComplexReference = complexReferenceFilter?.ComplexReference?.Clone();
            SAMObjectRelationCluster = complexReferenceFilter?.SAMObjectRelationCluster;
        }

        protected override bool FromJsonObject(JsonObject jsonObject)
        {
            if (!base.FromJsonObject(jsonObject))
            {
                return false;
            }

            if (jsonObject["ComplexReference"] is JsonObject complexReferenceObject)
            {
                ComplexReference = Query.IJSAMObject<IComplexReference>(new JObject((JsonObject)complexReferenceObject.DeepClone()));
            }

            return true;
        }

        public override bool IsValid(IJSAMObject jSAMObject)
        {

            if (SAMObjectRelationCluster == null || !SAMObjectRelationCluster.TryGetValues(jSAMObject, ComplexReference, out List<object> values))
            {
                return false;
            }

            bool result = IsValid(values);
            if (Inverted)
            {
                result = !result;
            }

            return result;
        }

        protected abstract bool IsValid(IEnumerable<object> values);

        protected override JsonObject ToJsonObject()
        {
            JsonObject result = base.ToJsonObject();
            if (result == null)
            {
                return result;
            }

            if (ComplexReference != null)
            {
                if (ComplexReference.ToJObject()?.Node is JsonObject complexReferenceObject)
                    result["ComplexReference"] = complexReferenceObject.DeepClone();
            }

            return result;
        }
    }
}
