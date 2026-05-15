// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Text.Json.Nodes;


namespace SAM.Core
{
    public class Location : SAMObject
    {
        private double longitude;
        private double latitude;
        private double elevation;

        public Location(Location location)
            : base(location)
        {
            if (location != null)
            {
                longitude = location.longitude;
                latitude = location.latitude;
                elevation = location.elevation;
            }
        }

        public Location(string name, double longitude, double latitude, double elevation)
            : base(name)
        {
            this.longitude = longitude;
            this.latitude = latitude;
            this.elevation = elevation;
        }

        public Location(Guid guid, string name, double longitude, double latitude, double elevation)
            : base(guid, name)
        {
            this.longitude = longitude;
            this.latitude = latitude;
            this.elevation = elevation;
        }
        public Location(JsonObject jsonObject)
            : base(jsonObject)
        {
        }

        public double Longitude
        {
            get
            {
                return longitude;
            }
        }

        public double Latitude
        {
            get
            {
                return latitude;
            }
        }

        public double Elevation
        {
            get
            {
                return elevation;
            }
        }

        public override bool FromJsonObject(JsonObject jsonObject)
        {
            if (!base.FromJsonObject(jsonObject))
                return false;

            longitude = jsonObject["Longitude"]?.GetValue<double>() ?? 0;
            latitude = jsonObject["Latitude"]?.GetValue<double>() ?? 0;
            elevation = jsonObject["Elevation"]?.GetValue<double>() ?? 0;

            return true;
        }

        public override JsonObject ToJsonObject()
        {
            JsonObject jsonObject = base.ToJsonObject();
            if (jsonObject == null)
                return null;

            jsonObject["Longitude"] = longitude;
            jsonObject["Latitude"] = latitude;
            jsonObject["Elevation"] = elevation;

            return jsonObject;
        }
    }
}
