// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors


namespace SAM.Core
{
    public class NameFilter : TextFilter
    {
        public NameFilter(System.Text.Json.Nodes.JsonObject jsonObject)
            : base(jsonObject)
        {
        }

        public NameFilter(NameFilter nameFilter)
            : base(nameFilter)
        {
        }

        public NameFilter(TextComparisonType textComparisonType, string value)
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

            text = sAMObject.Name;
            return true;
        }
    }
}
