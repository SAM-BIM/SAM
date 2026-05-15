// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;
using SAM.Core;
using SAM.Geometry.Spatial;
using System;
using System.Text.Json.Nodes;

namespace SAM.Analytical
{
    public class ExternalSpace : SAMObject, ISpace
    {
        private Point3D location;

        public Point3D Location
        {
            get
            {
                return location == null ? null : new Point3D(location);
            }
        }

        public ExternalSpace(string name, Point3D location)
            : base(name)
        {
            this.location = location == null ? null : new Point3D(Location);
        }

        public ExternalSpace(Guid guid, string name)
            : base(guid, name)
        {

        }

        public ExternalSpace(ExternalSpace externalSpace, string name)
            : base(name, externalSpace)
        {
            location = externalSpace?.Location?.Clone<Point3D>();
        }

        public ExternalSpace(ExternalSpace externalSpace)
            : base(externalSpace)
        {
            location = externalSpace?.Location?.Clone<Point3D>();
        }

        public ExternalSpace(JObject jObject)
            : base(jObject)
        {
        }

        public override bool FromJsonObject(JsonObject jsonObject)
        {
            if (!base.FromJsonObject(jsonObject))
            {
                return false;
            }

            if (jsonObject["Location"] is JsonObject locationJson)
            {
                location = new Point3D((JsonObject)locationJson.DeepClone());
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

            if (location?.ToJsonObject() is JsonObject locationJson)
            {
                jsonObject["Location"] = locationJson.DeepClone();
            }

            return jsonObject;
        }
    }
}
