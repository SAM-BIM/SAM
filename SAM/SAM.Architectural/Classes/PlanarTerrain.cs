// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;

using SAM.Geometry.Spatial;
using System.Text.Json.Nodes;

namespace SAM.Architectural
{
    public class PlanarTerrain : Terrain
    {
        private Plane plane;

        public PlanarTerrain(PlanarTerrain planarTerrain)
            : base(planarTerrain)
        {
            plane = planarTerrain?.plane == null ? null : new Plane(planarTerrain.plane);
        }

        public PlanarTerrain(JObject jObject)
            : base(jObject)
        {
        }

        public PlanarTerrain(Plane plane)
            : base()
        {
            this.plane = new Plane(plane);
        }

        public override bool Below(Face3D face3D, double tolerance = Core.Tolerance.Distance)
        {
            if (face3D == null)
            {
                return false;
            }

            return face3D.Below(plane, tolerance);
        }

        // The pre-migration code had FromJObject and ToJObject swapped:
        // FromJObject wrote a "Plane" key into the incoming JObject (instead
        // of reading from it), and ToJObject read "Plane" from the output of
        // base.ToJObject() (which never contains it) instead of writing it.
        // Net effect: the Plane field never round-tripped through JSON. This
        // migration fixes the swap — read on FromJsonObject, write on
        // ToJsonObject — so PlanarTerrain instances now serialize and
        // deserialize their Plane.
        public override bool FromJsonObject(JsonObject jsonObject)
        {
            if (!base.FromJsonObject(jsonObject))
            {
                return false;
            }

            if (jsonObject["Plane"] is JsonObject planeJson)
            {
                plane = new Plane(new JObject((JsonObject)planeJson.DeepClone()));
            }

            return true;
        }

        public override bool On(Face3D face3D, double tolerance = Core.Tolerance.Distance)
        {
            return plane.On(face3D, tolerance);
        }

        public override JsonObject ToJsonObject()
        {
            JsonObject jsonObject = base.ToJsonObject();

            if (jsonObject == null)
            {
                return null;
            }

            if (plane?.ToJsonObject() is JsonObject planeJson)
            {
                jsonObject["Plane"] = planeJson.DeepClone();
            }

            return jsonObject;
        }
    }
}
