// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace SAM.Analytical
{
    public class TM59Result : TMResult
    {
        private HashSet<TM59SpaceApplication> tM59SpaceApplications;

        private int occupiedHours;
        private int maxExceedableHours;

        private bool pass;

        public TM59Result(
            string name,
            string source,
            string reference,
            TM52BuildingCategory tM52BuildingCategory,
            int occupiedHours,
            int maxExceedableHours,
            bool pass,
            params TM59SpaceApplication[] tM59SpaceApplications)
            : base(name, source, reference, tM52BuildingCategory)
        {
            this.occupiedHours = occupiedHours;
            this.maxExceedableHours = maxExceedableHours;
            this.pass = pass;
            this.tM59SpaceApplications = tM59SpaceApplications == null ? null : new HashSet<TM59SpaceApplication>(tM59SpaceApplications);
        }

        public TM59Result(
            Guid guid,
            string name,
            string source,
            string reference,
            TM52BuildingCategory tM52BuildingCategory,
            int occupiedHours,
            int maxExceedableHours,
            bool pass,
            params TM59SpaceApplication[] tM59SpaceApplications)
            : base(guid, name, source, reference, tM52BuildingCategory)
        {
            this.occupiedHours = occupiedHours;
            this.maxExceedableHours = maxExceedableHours;
            this.pass = pass;
            this.tM59SpaceApplications = tM59SpaceApplications == null ? null : new HashSet<TM59SpaceApplication>(tM59SpaceApplications);
        }

        public override int OccupiedHours
        {
            get
            {
                return occupiedHours;
            }
        }

        public override int MaxExceedableHours
        {
            get
            {
                return maxExceedableHours;
            }
        }

        public override bool Pass
        {
            get
            {
                return pass;
            }
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

            if (jsonObject.ContainsKey("OccupiedHours"))
            {
                occupiedHours = jsonObject["OccupiedHours"]?.GetValue<int>() ?? 0;
            }

            if (jsonObject.ContainsKey("MaxExceedableHours"))
            {
                maxExceedableHours = jsonObject["MaxExceedableHours"]?.GetValue<int>() ?? 0;
            }

            if (jsonObject.ContainsKey("Pass"))
            {
                pass = jsonObject["Pass"]?.GetValue<bool>() ?? false;
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

            if (occupiedHours != int.MinValue)
            {
                result["OccupiedHours"] = occupiedHours;
            }

            if (maxExceedableHours != int.MinValue)
            {
                result["MaxExceedableHours"] = maxExceedableHours;
            }

            result["Pass"] = pass;

            return result;
        }
    }
}
