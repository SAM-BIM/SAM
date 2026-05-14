// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;

using SAM.Core;
using System.Text.Json.Nodes;

namespace SAM.Architectural
{
    public class Level : SAMObject, IArchitecturalObject
    {
        private double elevation;

        public Level(Level level)
            : base(level)
        {
            elevation = level.elevation;
        }

        public Level(double elevation)
            : base()
        {
            this.elevation = elevation;
        }

        public Level(string name, double elevation)
            : base(name)
        {
            this.elevation = elevation;
        }

        public Level(JObject jObject)
            : base(jObject)
        {
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

            elevation = jsonObject["Elevation"]?.GetValue<double>() ?? 0;
            return true;
        }

        public Geometry.Spatial.Plane GetPlane()
        {
            return new Geometry.Spatial.Plane(new Geometry.Spatial.Point3D(0, 0, elevation), Geometry.Spatial.Vector3D.WorldZ);
        }

        public override JsonObject ToJsonObject()
        {
            JsonObject jsonObject = base.ToJsonObject();

            if (jsonObject == null)
                return null;

            jsonObject["Elevation"] = elevation;

            return jsonObject;
        }
    }
}
