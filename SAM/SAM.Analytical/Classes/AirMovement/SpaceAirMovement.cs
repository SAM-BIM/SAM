// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;
using SAM.Core;
using System.Text.Json.Nodes;

namespace SAM.Analytical
{
    public class SpaceAirMovement : SAMObject, IAirMovementObject
    {
        private Profile profile;
        private double airFlow;
        private string from;
        private string to;

        public SpaceAirMovement(string name, double airFlow, Profile profile, string from, string to)
            : base(name)
        {
            this.profile = profile == null ? null : new Profile(profile);
            this.airFlow = airFlow;
            this.from = from;
            this.to = to;
        }

        public SpaceAirMovement(string name, double airflow, string from, string to)
            : base(name)
        {
            this.airFlow = airflow;
            this.profile = new Profile(name, ProfileType.Ventilation, new double[] { 1 });
            this.from = from;
            this.to = to;
        }

        public SpaceAirMovement(SpaceAirMovement spaceAirMovement)
            : base(spaceAirMovement)
        {
            if (spaceAirMovement != null)
            {
                profile = spaceAirMovement.profile == null ? null : new Profile(spaceAirMovement.profile);
                airFlow = spaceAirMovement.airFlow;
                from = spaceAirMovement.from;
                to = spaceAirMovement.to;
            }
        }

        public SpaceAirMovement(JObject jObject)
            : base(jObject)
        {

        }

        public Profile Profile
        {
            get
            {
                return profile == null ? null : new Profile(profile);
            }
        }

        public double AirFlow
        {
            get
            {
                return airFlow;
            }
        }

        public string From
        {
            get
            {
                return from;
            }
        }

        public string To
        {
            get
            {
                return to;
            }
        }

        public override bool FromJsonObject(JsonObject jsonObject)
        {
            if (!base.FromJsonObject(jsonObject))
            {
                return false;
            }

            if (jsonObject.ContainsKey("AirFlow"))
            {
                airFlow = jsonObject["AirFlow"]?.GetValue<double>() ?? double.NaN;
            }

            if (jsonObject["Profile"] is JsonObject profileJson)
            {
                profile = new Profile(new JObject((JsonObject)profileJson.DeepClone()));
            }

            if (jsonObject.ContainsKey("From"))
            {
                from = jsonObject["From"]?.GetValue<string>();
            }

            if (jsonObject.ContainsKey("To"))
            {
                to = jsonObject["To"]?.GetValue<string>();
            }

            return true;
        }

        public override JsonObject ToJsonObject()
        {
            JsonObject jsonObject = base.ToJsonObject();
            if (jsonObject == null)
            {
                return null;
            }

            if (!double.IsNaN(airFlow))
            {
                jsonObject["AirFlow"] = airFlow;
            }

            if (profile?.ToJsonObject() is JsonObject profileJson)
            {
                jsonObject["Profile"] = profileJson.DeepClone();
            }

            if (from != null)
            {
                jsonObject["From"] = from;
            }

            if (to != null)
            {
                jsonObject["To"] = to;
            }

            return jsonObject;
        }
    }
}
