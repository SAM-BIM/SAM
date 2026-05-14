// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;
using System.Text.Json.Nodes;

namespace SAM.Analytical
{
    /// <summary>
    /// Scheduled Opening Properties
    /// </summary>
    public class ProfileOpeningProperties : OpeningProperties
    {
        private Profile profile;

        public ProfileOpeningProperties()
        {

        }

        public ProfileOpeningProperties(double dischargeCoefficient)
            : base(dischargeCoefficient)
        {

        }

        public ProfileOpeningProperties(JObject jObject)
            : base(jObject)
        {
        }

        public ProfileOpeningProperties(double dischargeCoefficient, Profile profile)
            : base(dischargeCoefficient)
        {
            this.profile = profile == null ? null : new Profile(profile);
        }

        public ProfileOpeningProperties(ProfileOpeningProperties profileOpeningProperties)
            : base(profileOpeningProperties)
        {
            profile = profileOpeningProperties.profile == null ? null : new Profile(profileOpeningProperties.profile);
        }

        public ProfileOpeningProperties(IOpeningProperties openingProperties, double dischargeCoefficient)
            : base(openingProperties, dischargeCoefficient)
        {
            if (openingProperties is ProfileOpeningProperties)
            {
                profile = ((ProfileOpeningProperties)openingProperties).profile == null ? null : new Profile(((ProfileOpeningProperties)openingProperties).profile);
            }
        }

        public Profile Profile
        {
            get
            {
                return profile == null ? null : new Profile(profile);
            }
        }

        protected override bool FromJsonObject(JsonObject jsonObject)
        {
            if (!base.FromJsonObject(jsonObject))
            {
                return false;
            }

            if (jsonObject["Profile"] is JsonObject profileJson)
            {
                profile = Core.Query.IJSAMObject<Profile>(new JObject((JsonObject)profileJson.DeepClone()));
            }

            return true;
        }

        protected override JsonObject ToJsonObject()
        {
            JsonObject jsonObject = base.ToJsonObject();
            if (jsonObject == null)
            {
                return null;
            }

            if (profile?.ToJObject()?.Node is JsonObject profileJson)
            {
                jsonObject["Profile"] = profileJson.DeepClone();
            }

            return jsonObject;
        }
    }
}
