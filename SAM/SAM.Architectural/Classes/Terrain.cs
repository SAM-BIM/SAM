// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;
using SAM.Geometry.Object.Spatial;
using SAM.Geometry.Spatial;
using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace SAM.Architectural
{
    public abstract class Terrain : Core.SAMObject, ITerrain, IArchitecturalObject
    {
        private List<MaterialLayer> materialLayers;

        public Terrain()
            : base()
        {
        }

        public Terrain(Terrain terrain)
            : base(terrain)
        {
        }

        public Terrain(JObject jObject)
            : base(jObject)
        {
        }

        public abstract bool Below(Face3D face3D, double tolerance = Core.Tolerance.Distance);

        public bool Below(IFace3DObject face3DObject, double tolerance = Core.Tolerance.Distance)
        {
            return Below(face3DObject?.Face3D, tolerance);
        }

        public override bool FromJsonObject(JsonObject jsonObject)
        {
            if (!base.FromJsonObject(jsonObject))
            {
                return false;
            }

            if (jsonObject["MaterialLayers"] is JsonArray materialLayersArray)
            {
                materialLayers = Core.Create.IJSAMObjects<MaterialLayer>(materialLayersArray);
            }

            return true;
        }

        public abstract bool On(Face3D face3D, double tolerance = Core.Tolerance.Distance);

        public bool On(IFace3DObject face3DObject, double tolerance = Core.Tolerance.Distance)
        {
            return On(face3DObject?.Face3D, tolerance);
        }

        public override JsonObject ToJsonObject()
        {
            JsonObject jsonObject = base.ToJsonObject();

            if (jsonObject == null)
            {
                return null;
            }

            if (materialLayers != null)
            {
                JsonArray materialLayersArray = new JsonArray();
                foreach (MaterialLayer materialLayer in materialLayers)
                {
                    if (materialLayer?.ToJsonObject() is JsonObject materialLayerJson)
                    {
                        materialLayersArray.Add(materialLayerJson.DeepClone());
                    }
                }
                jsonObject["MaterialLayers"] = materialLayersArray;
            }

            return jsonObject;
        }
    }
}
