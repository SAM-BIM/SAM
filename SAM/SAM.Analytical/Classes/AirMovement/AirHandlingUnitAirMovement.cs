// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;
using SAM.Core;
using System.Text.Json.Nodes;

namespace SAM.Analytical
{
    public class AirHandlingUnitAirMovement : SAMObject, IAirMovementObject
    {
        private Profile heating;
        private Profile cooling;
        private Profile humidification;
        private Profile dehumidification;
        private Profile density;

        public AirHandlingUnitAirMovement(string name)
            : base(name)
        {

        }

        public AirHandlingUnitAirMovement(string name, Profile heating, Profile cooling, Profile humidification, Profile dehumidification, Profile density)
            : base(name)
        {
            this.heating = heating == null ? null : new Profile(heating);
            this.cooling = cooling == null ? null : new Profile(cooling);
            this.humidification = humidification == null ? null : new Profile(humidification);
            this.dehumidification = dehumidification == null ? null : new Profile(dehumidification);
            this.density = density == null ? null : new Profile(density);
        }

        public AirHandlingUnitAirMovement(AirHandlingUnitAirMovement airHandlingUnitAirMovement)
            : base(airHandlingUnitAirMovement)
        {
            if (airHandlingUnitAirMovement != null)
            {
                heating = airHandlingUnitAirMovement.heating == null ? null : new Profile(airHandlingUnitAirMovement.heating);
                cooling = airHandlingUnitAirMovement.cooling == null ? null : new Profile(airHandlingUnitAirMovement.cooling);
                humidification = airHandlingUnitAirMovement.humidification == null ? null : new Profile(airHandlingUnitAirMovement.humidification);
                dehumidification = airHandlingUnitAirMovement.dehumidification == null ? null : new Profile(airHandlingUnitAirMovement.dehumidification);
                density = airHandlingUnitAirMovement.density == null ? null : new Profile(airHandlingUnitAirMovement.density);
            }
        }

        public AirHandlingUnitAirMovement(JObject jObject)
            : base(jObject?.Node as System.Text.Json.Nodes.JsonObject)
        {

        }


        public AirHandlingUnitAirMovement(System.Text.Json.Nodes.JsonObject jsonObject)

            : base(jsonObject)

        {

        }

        public Profile Heating
        {
            get
            {
                return heating == null ? null : new Profile(heating);
            }
        }

        public Profile Cooling
        {
            get
            {
                return cooling == null ? null : new Profile(cooling);
            }
        }

        public Profile Humidification
        {
            get
            {
                return humidification == null ? null : new Profile(humidification);
            }
        }

        public Profile Dehumidification
        {
            get
            {
                return dehumidification == null ? null : new Profile(dehumidification);
            }
        }

        public Profile Density
        {
            get
            {
                return density == null ? null : new Profile(density);
            }
        }

        public override bool FromJsonObject(JsonObject jsonObject)
        {
            if (!base.FromJsonObject(jsonObject))
            {
                return false;
            }

            if (jsonObject["Heating"] is JsonObject heatingJson)
            {
                heating = new Profile((JsonObject)heatingJson.DeepClone());
            }

            if (jsonObject["Cooling"] is JsonObject coolingJson)
            {
                cooling = new Profile((JsonObject)coolingJson.DeepClone());
            }

            if (jsonObject["Humidification"] is JsonObject humidificationJson)
            {
                humidification = new Profile((JsonObject)humidificationJson.DeepClone());
            }

            if (jsonObject["Dehumidification"] is JsonObject dehumidificationJson)
            {
                dehumidification = new Profile((JsonObject)dehumidificationJson.DeepClone());
            }

            if (jsonObject["Density"] is JsonObject densityJson)
            {
                density = new Profile((JsonObject)densityJson.DeepClone());
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

            if (heating?.ToJsonObject() is JsonObject heatingJson)
            {
                jsonObject["Heating"] = heatingJson.DeepClone();
            }

            if (cooling?.ToJsonObject() is JsonObject coolingJson)
            {
                jsonObject["Cooling"] = coolingJson.DeepClone();
            }

            if (humidification?.ToJsonObject() is JsonObject humidificationJson)
            {
                jsonObject["Humidification"] = humidificationJson.DeepClone();
            }

            if (dehumidification?.ToJsonObject() is JsonObject dehumidificationJson)
            {
                jsonObject["Dehumidification"] = dehumidificationJson.DeepClone();
            }

            if (density?.ToJsonObject() is JsonObject densityJson)
            {
                jsonObject["Density"] = densityJson.DeepClone();
            }

            return jsonObject;
        }
    }
}
