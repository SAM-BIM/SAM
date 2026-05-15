// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;
using SAM.Core;

namespace SAM.Analytical
{
    public class SpaceInternalConditionFilter : RelationFilter<InternalCondition>
    {
        public SpaceInternalConditionFilter(IFilter filter)
            : base(filter)
        {

        }

        public SpaceInternalConditionFilter(SpaceInternalConditionFilter spaceInternalConditionFilter)
            : base(spaceInternalConditionFilter)
        {

        }

        public SpaceInternalConditionFilter(JObject jObject)
            : base(jObject?.Node as System.Text.Json.Nodes.JsonObject)
        {

        }


        public SpaceInternalConditionFilter(System.Text.Json.Nodes.JsonObject jsonObject)

            : base(jsonObject)

        {

        }

        public override InternalCondition GetRelative(IJSAMObject jSAMObject)
        {
            return (jSAMObject as Space)?.InternalCondition;
        }
    }
}
