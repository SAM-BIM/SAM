// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;
using SAM.Architectural;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;

namespace SAM.Analytical
{
    public abstract class HostPartitionType : BuildingElementType
    {
        private List<MaterialLayer> materialLayers;

        public HostPartitionType(HostPartitionType hostPartitionType)
            : base(hostPartitionType)
        {

        }

        public HostPartitionType(JObject jObject)
            : base(jObject)
        {

        }

        public HostPartitionType(string name)
            : base(name)
        {

        }

        public HostPartitionType(System.Guid guid, string name)
            : base(guid, name)
        {

        }

        public HostPartitionType(string name, IEnumerable<MaterialLayer> materialLayers)
            : base(name)
        {
            this.materialLayers = materialLayers?.ToList().ConvertAll(x => new MaterialLayer(x));
        }

        public HostPartitionType(System.Guid guid, string name, IEnumerable<MaterialLayer> materialLayers)
            : base(guid, name)
        {
            this.materialLayers = materialLayers?.ToList().ConvertAll(x => new MaterialLayer(x));
        }

        public HostPartitionType(HostPartitionType hostPartitionType, string name)
            : base(hostPartitionType, name)
        {
            materialLayers = hostPartitionType?.materialLayers?.ToList().ConvertAll(x => new MaterialLayer(x));
        }

        public List<MaterialLayer> MaterialLayers
        {
            get
            {
                if (materialLayers == null)
                {
                    return null;
                }

                return materialLayers.ConvertAll(x => new MaterialLayer(x));
            }

            set
            {
                if (value == null)
                {
                    return;
                }

                materialLayers = value?.ConvertAll(x => new MaterialLayer(x));
            }
        }

        public MaterialLayer this[int i]
        {
            get
            {
                if (materialLayers == null)
                {
                    return null;
                }

                return new MaterialLayer(materialLayers[i]);
            }

            set
            {
                if (materialLayers == null)
                {
                    return;
                }

                materialLayers[i] = new MaterialLayer(value);
            }
        }

        public double GetThickness()
        {
            if (materialLayers == null)
            {
                return double.NaN;
            }

            double result = 0;
            foreach (MaterialLayer materialLayer in materialLayers)
            {
                double thickness = materialLayer.Thickness;
                if (double.IsNaN(thickness))
                {
                    continue;
                }

                result += thickness;
            }

            return result;
        }

        public override bool FromJsonObject(JsonObject jsonObject)
        {
            if (!base.FromJsonObject(jsonObject))
                return false;

            if (jsonObject["MaterialLayers"] is JsonArray materialLayersArray)
                materialLayers = Core.Create.IJSAMObjects<MaterialLayer>(materialLayersArray);

            return true;
        }

        public override JsonObject ToJsonObject()
        {
            JsonObject jsonObject = base.ToJsonObject();

            if (jsonObject == null)
                return jsonObject;

            if (materialLayers != null)
            {
                JsonArray materialLayersArray = new JsonArray();
                foreach (MaterialLayer layer in materialLayers)
                {
                    if (layer?.ToJsonObject() is JsonObject layerJson)
                    {
                        materialLayersArray.Add(layerJson.DeepClone());
                    }
                }
                jsonObject["MaterialLayers"] = materialLayersArray;
            }

            return jsonObject;
        }

    }
}
