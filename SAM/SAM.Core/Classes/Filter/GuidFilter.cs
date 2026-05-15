// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors


namespace SAM.Core
{
    public class GuidFilter : TextFilter
    {
        public GuidFilter(System.Text.Json.Nodes.JsonObject jsonObject)
            : base(jsonObject)
        {
        }

        public GuidFilter(GuidFilter guidFilter)
            : base(guidFilter)
        {

        }

        public GuidFilter(TextComparisonType textComparisonType, string value)
            : base(textComparisonType, value)
        {

        }

        public override bool TryGetText(IJSAMObject jSAMObject, out string text)
        {
            text = null;

            ISAMObject sAMObject = jSAMObject as ISAMObject;
            if (sAMObject == null)
            {
                return false;
            }

            text = sAMObject.Guid.ToString();
            return true;
        }
    }
}
