// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;
using SAM.Core;
using System;
using System.Text.Json.Nodes;

namespace SAM.Analytical
{
    public class DegreeOfActivity : SAMObject, IAnalyticalObject
    {
        private double sensible;
        private double latent;

        public DegreeOfActivity(DegreeOfActivity degreeOfActivity)
            : base(degreeOfActivity)
        {
            sensible = degreeOfActivity.sensible;
            latent = degreeOfActivity.latent;
        }

        public DegreeOfActivity(Guid guid, DegreeOfActivity degreeOfActivity)
        : base(guid, degreeOfActivity)
        {
            sensible = degreeOfActivity.sensible;
            latent = degreeOfActivity.latent;
        }

        public DegreeOfActivity(string name, double sensible, double latent)
            : base(name)
        {
            this.sensible = sensible;
            this.latent = latent;
        }

        public DegreeOfActivity(JObject jObject)
            : base(jObject)
        {
        }

        /// <summary>
        /// Dry (sensible) total heat emission [W/p]
        /// </summary>
        public double Sensible
        {
            get
            {
                return sensible;
            }
        }

        /// <summary>
        /// Humid (latent) heat emission [W/p]
        /// </summary>
        public double Latent
        {
            get
            {
                return latent;
            }
        }

        public double GetTotal()
        {
            if (double.IsNaN(sensible) && double.IsNaN(latent))
                return double.NaN;

            double result = 0;
            if (!double.IsNaN(sensible))
                result += sensible;

            if (!double.IsNaN(latent))
                result += latent;

            return result;
        }

        public override bool FromJsonObject(JsonObject jsonObject)
        {
            if (!base.FromJsonObject(jsonObject))
                return false;

            if (jsonObject.ContainsKey("Sensible"))
                sensible = jsonObject["Sensible"]?.GetValue<double>() ?? double.NaN;
            else
                sensible = double.NaN;

            if (jsonObject.ContainsKey("Latent"))
                latent = jsonObject["Latent"]?.GetValue<double>() ?? double.NaN;
            else
                latent = double.NaN;

            return true;
        }

        public override JsonObject ToJsonObject()
        {
            JsonObject jsonObject = base.ToJsonObject();
            if (jsonObject == null)
                return jsonObject;

            if (sensible != double.NaN)
                jsonObject["Sensible"] = sensible;

            if (latent != double.NaN)
                jsonObject["Latent"] = latent;

            return jsonObject;
        }
    }
}
