// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;
using SAM.Core;
using SAM.Geometry.Spatial;
using System;
using System.Text.Json.Nodes;

namespace SAM.Analytical
{
    public class Space : SAMObject, ISpace
    {
        private InternalCondition internalCondition;
        private Point3D location;
        public Space(Space space)
            : base(space)
        {
            location = space.Location;
            internalCondition = space.InternalCondition;
        }

        public Space(Guid guid, Space space)
        : base(guid, space)
        {
            location = space.Location;
            internalCondition = space.InternalCondition;
        }

        public Space(Guid guid, string name, Point3D location)
            : base(guid, name)
        {
            if (location != null)
            {
                this.location = new Point3D(location);
            }
        }

        public Space(Guid guid, Space space, string name, Point3D location)
            : base(name, guid, space)
        {
            if (location != null)
            {
                this.location = new Point3D(location);
            }
        }

        public Space(string name)
            : base(name)
        {
        }

        public Space(string name, Point3D location)
            : base(name)
        {
            if (location != null)
            {
                this.location = new Point3D(location);
            }
        }

        public Space(Space space, string name, Point3D location)
            : base(name, space)
        {
            internalCondition = space.InternalCondition;

            if (location != null)
            {
                this.location = new Point3D(location);
            }
        }

        public Space(JObject jObject)
            : base(jObject)
        {
        }

        public InternalCondition InternalCondition
        {
            get
            {
                if (internalCondition == null)
                    return null;

                return new InternalCondition(internalCondition);
            }

            set
            {
                if (value == null)
                    internalCondition = null;
                else
                    internalCondition = new InternalCondition(Guid.NewGuid(), value);
            }
        }

        public Point3D Location
        {
            get
            {
                if (location == null)
                    return null;

                return new Point3D(location);
            }
        }

        public new string Name
        {
            get
            {
                return name;
            }

            set
            {
                name = value;
            }
        }

        public override bool FromJsonObject(JsonObject jsonObject)
        {
            if (!base.FromJsonObject(jsonObject))
                return false;

            if (jsonObject["Location"] is JsonObject locationJson)
                location = new Point3D(new JObject((JsonObject)locationJson.DeepClone()));

            if (jsonObject["InternalCondition"] is JsonObject internalConditionJson)
                internalCondition = new InternalCondition(new JObject((JsonObject)internalConditionJson.DeepClone()));

            return true;
        }

        public bool IsPlaced()
        {
            return location != null && location.IsValid();
        }
        public void Move(Vector3D vector3D)
        {
            location = location?.GetMoved(vector3D) as Point3D;
        }

        public override JsonObject ToJsonObject()
        {
            JsonObject jsonObject = base.ToJsonObject();
            if (jsonObject == null)
                return jsonObject;

            if (location?.ToJsonObject() is JsonObject locationJson)
                jsonObject["Location"] = locationJson.DeepClone();

            if (internalCondition?.ToJsonObject() is JsonObject internalConditionJson)
                jsonObject["InternalCondition"] = internalConditionJson.DeepClone();

            return jsonObject;
        }

        public void Transform(Transform3D transform3D)
        {
            if (location != null)
                location = Location.Transform(transform3D);
        }
    }
}
