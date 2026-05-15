// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;
using SAM.Core;
using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace SAM.Analytical
{
    public abstract class TM59ExtendedResult : TMExtendedResult
    {
        private HashSet<TM59SpaceApplication> tM59SpaceApplications;

        public TM59ExtendedResult(string name, string source, string reference, TM52BuildingCategory tM52BuildingCategory, HashSet<int> occupiedHourIndices, IndexedDoubles minAcceptableTemperatures, IndexedDoubles maxAcceptableTemperatures, IndexedDoubles operativeTemperatures, params TM59SpaceApplication[] tM59SpaceApplications)
            : base(name, source, reference, tM52BuildingCategory, occupiedHourIndices, minAcceptableTemperatures, maxAcceptableTemperatures, operativeTemperatures)
        {
            this.tM59SpaceApplications = tM59SpaceApplications == null ? null : new HashSet<TM59SpaceApplication>(tM59SpaceApplications);
        }

        public TM59ExtendedResult(TM59ExtendedResult tM59SpaceExtendedResult)
            : base(tM59SpaceExtendedResult)
        {
            if (tM59SpaceExtendedResult != null)
            {
                tM59SpaceApplications = tM59SpaceExtendedResult.tM59SpaceApplications == null ? null : new HashSet<TM59SpaceApplication>(tM59SpaceExtendedResult.tM59SpaceApplications);
            }
        }

        public TM59ExtendedResult(TM59ExtendedResult tM59SpaceExtendedResult, HashSet<int> occupiedHourIndices, IndexedDoubles minAcceptableTemperatures, IndexedDoubles maxAcceptableTemperatures, IndexedDoubles operativeTemperatures)
            : base(tM59SpaceExtendedResult, occupiedHourIndices, minAcceptableTemperatures, maxAcceptableTemperatures, operativeTemperatures)
        {
            if (tM59SpaceExtendedResult != null)
            {
                tM59SpaceApplications = tM59SpaceExtendedResult.tM59SpaceApplications == null ? null : new HashSet<TM59SpaceApplication>(tM59SpaceApplications);
            }
        }
        public TM59ExtendedResult(System.Text.Json.Nodes.JsonObject jsonObject)

            : base(jsonObject)

        {

        }

        public HashSet<TM59SpaceApplication> TM59SpaceApplications
        {
            get
            {
                return tM59SpaceApplications == null ? null : new HashSet<TM59SpaceApplication>(tM59SpaceApplications);
            }
        }

        public override bool FromJsonObject(JsonObject jsonObject)
        {
            if (!base.FromJsonObject(jsonObject))
            {
                return false;
            }

            if (jsonObject["TM59SpaceApplications"] is JsonArray tM59SpaceApplicationsArray)
            {
                tM59SpaceApplications = new HashSet<TM59SpaceApplication>();
                foreach (JsonNode node in tM59SpaceApplicationsArray)
                {
                    TM59SpaceApplication tM59SpaceApplication = Core.Query.Enum<TM59SpaceApplication>(node?.GetValue<string>());
                    tM59SpaceApplications.Add(tM59SpaceApplication);
                }
            }

            return true;
        }

        public override JsonObject ToJsonObject()
        {
            JsonObject result = base.ToJsonObject();
            if (result == null)
            {
                return null;
            }

            if (tM59SpaceApplications != null)
            {
                JsonArray tM59SpaceApplicationsArray = new JsonArray();
                foreach (TM59SpaceApplication tM59SpaceApplication in tM59SpaceApplications)
                {
                    tM59SpaceApplicationsArray.Add(tM59SpaceApplication.ToString());
                }

                result["TM59SpaceApplications"] = tM59SpaceApplicationsArray;
            }

            return result;
        }
    }
}
