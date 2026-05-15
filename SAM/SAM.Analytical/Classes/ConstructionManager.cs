// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;
using SAM.Core;
using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace SAM.Analytical
{
    public class ConstructionManager : IJSAMObject, IAnalyticalObject
    {
        public string Name { get; set; }
        public string Description { get; set; }

        private ApertureConstructionLibrary apertureConstructionLibrary;
        private ConstructionLibrary constructionLibrary;

        private MaterialLibrary materialLibrary;

        public ConstructionManager(ApertureConstructionLibrary apertureConstructionLibrary, ConstructionLibrary constructionLibrary, MaterialLibrary materialLibrary)
        {
            this.apertureConstructionLibrary = apertureConstructionLibrary;
            this.constructionLibrary = constructionLibrary;
            this.materialLibrary = materialLibrary;
        }

        public ConstructionManager(IEnumerable<ApertureConstruction> apertureConstructions, IEnumerable<Construction> constructions, MaterialLibrary materialLibrary)
        {
            this.materialLibrary = materialLibrary == null ? null : new MaterialLibrary(materialLibrary);

            apertureConstructionLibrary = new ApertureConstructionLibrary("Default ApertureConstruction Library");
            if (apertureConstructions != null)
            {
                foreach (ApertureConstruction apertureConstruction in apertureConstructions)
                {
                    apertureConstructionLibrary.Add(apertureConstruction);
                }
            }

            constructionLibrary = new ConstructionLibrary("Default Construction Library");
            if (constructions != null)
            {
                foreach (Construction construction in constructions)
                {
                    constructionLibrary.Add(construction);
                }
            }

        }

        public ConstructionManager(JObject jObject)
        {
            FromJsonObject(jObject?.Node as System.Text.Json.Nodes.JsonObject);
        }


        public ConstructionManager(System.Text.Json.Nodes.JsonObject jsonObject)

        {

            FromJsonObject(jsonObject);

        }

        public ConstructionManager(ConstructionManager constructionManager)
        {
            if (constructionManager != null)
            {
                apertureConstructionLibrary = constructionManager.apertureConstructionLibrary?.Clone();
                constructionLibrary = constructionManager.constructionLibrary?.Clone();
                materialLibrary = constructionManager.materialLibrary?.Clone();

                Name = constructionManager.Name;
                Description = constructionManager.Description;
            }
        }

        public ConstructionManager()
        {

        }

        public List<ApertureConstruction> ApertureConstructions
        {
            get
            {
                return apertureConstructionLibrary?.GetApertureConstructions()?.ConvertAll(x => x?.Clone());
            }
        }

        public List<Construction> Constructions
        {
            get
            {
                return constructionLibrary?.GetConstructions()?.ConvertAll(x => x?.Clone());
            }
        }

        public List<IMaterial> Materials
        {
            get
            {
                return materialLibrary?.GetMaterials()?.ConvertAll(x => x?.Clone());
            }
        }

        public MaterialLibrary MaterialLibrary
        {
            get
            {
                return materialLibrary == null ? null : new MaterialLibrary(materialLibrary);
            }
        }

        public bool Remove(IMaterial material)
        {
            if (material == null || materialLibrary == null)
            {
                return false;
            }

            return materialLibrary.Remove(material);
        }

        public bool Remove(ApertureConstruction apertureConstruction)
        {
            if (apertureConstruction == null || apertureConstructionLibrary == null)
            {
                return false;
            }

            return apertureConstructionLibrary.Remove(apertureConstruction);
        }

        public bool Remove(Construction construction)
        {
            if (construction == null || constructionLibrary == null)
            {
                return false;
            }

            return constructionLibrary.Remove(construction);
        }

        public bool Add(IMaterial material)
        {
            if (material == null)
            {
                return false;
            }

            if (materialLibrary == null)
            {
                materialLibrary = new MaterialLibrary(string.Empty);
            }

            return materialLibrary.Add(material);
        }

        public bool Add(ApertureConstruction apertureConstruction)
        {
            if (apertureConstruction == null)
            {
                return false;
            }

            if (apertureConstructionLibrary == null)
            {
                apertureConstructionLibrary = new ApertureConstructionLibrary(string.Empty);
            }

            return apertureConstructionLibrary.Add(apertureConstruction);
        }

        public bool Add(Construction construction)
        {
            if (construction == null)
            {
                return false;
            }

            if (constructionLibrary == null)
            {
                constructionLibrary = new ConstructionLibrary(string.Empty);
            }

            return constructionLibrary.Add(construction);
        }

        public bool Add(Construction construction, PanelType panelType)
        {
            if (construction == null)
            {
                return false;
            }

            Construction construction_New = new Construction(construction);
            construction_New.SetValue(ConstructionParameter.DefaultPanelType, panelType);

            return Add(construction_New);
        }

        public bool Add(ApertureConstruction apertureConstruction, PanelType panelType)
        {
            if (apertureConstruction == null)
            {
                return false;
            }

            ApertureConstruction apertureConstruction_New = new ApertureConstruction(apertureConstruction);
            apertureConstruction_New.SetValue(ApertureConstructionParameter.DefaultPanelType, panelType);

            return Add(apertureConstruction_New);
        }

        public Construction GetConstruction(System.Guid guid)
        {
            return constructionLibrary?.GetConstructions()?.Find(x => x.Guid == guid);
        }

        public ApertureConstruction GetApertureConstruction(System.Guid guid)
        {
            return apertureConstructionLibrary?.GetApertureConstructions()?.Find(x => x.Guid == guid);
        }

        public List<Construction> GetConstructions(string text, TextComparisonType textComparisonType = TextComparisonType.Equals, bool caseSensitive = true)
        {
            return constructionLibrary?.GetConstructions(text, textComparisonType, caseSensitive);
        }

        public List<Construction> GetConstructions(PanelType panelType)
        {
            List<Construction> constructions = constructionLibrary?.GetConstructions();
            if (constructions == null)
            {
                return null;
            }

            List<Construction> result = new List<Construction>();
            foreach (Construction construction in constructions)
            {
                if (construction == null)
                {
                    continue;
                }

                if (!construction.TryGetValue(ConstructionParameter.DefaultPanelType, out string panelTypeString) || string.IsNullOrWhiteSpace(panelTypeString))
                {
                    continue;
                }

                if (!Core.Query.TryGetEnum(panelTypeString, out PanelType panelType_Temp))
                {
                    continue;
                }

                if (panelType_Temp == panelType)
                {
                    result.Add(construction);
                }
            }

            return result;
        }

        public List<ApertureConstruction> GetApertureConstructions(string text, TextComparisonType textComparisonType = TextComparisonType.Equals, bool caseSensitive = true)
        {
            return apertureConstructionLibrary?.GetApertureConstructions(text, textComparisonType, caseSensitive);
        }

        public List<ApertureConstruction> GetApertureConstructions(ApertureType apertureType, string text, TextComparisonType textComparisonType = TextComparisonType.Equals, bool caseSensitive = true)
        {
            return apertureConstructionLibrary?.GetApertureConstructions(text, textComparisonType, caseSensitive, apertureType: apertureType);
        }

        public List<ApertureConstruction> GetApertureConstructions(ApertureType apertureType, PanelType panelType)
        {
            List<ApertureConstruction> apertureConstructions = apertureConstructionLibrary?.GetApertureConstructions();
            if (apertureConstructions == null)
            {
                return null;
            }

            List<ApertureConstruction> result = new List<ApertureConstruction>();
            foreach (ApertureConstruction apertureConstruction in apertureConstructions)
            {
                if (apertureConstruction == null)
                {
                    continue;
                }

                if (apertureConstruction.ApertureType != apertureType)
                {
                    continue;
                }

                if (!apertureConstruction.TryGetValue(ApertureConstructionParameter.DefaultPanelType, out string panelTypeString) || string.IsNullOrWhiteSpace(panelTypeString))
                {
                    continue;
                }

                if (!Core.Query.TryGetEnum(panelTypeString, out PanelType panelType_Temp))
                {
                    continue;
                }

                if (panelType_Temp == panelType)
                {
                    result.Add(apertureConstruction);
                }
            }

            return result;
        }

        public IMaterial GetMaterial(string name)
        {
            return materialLibrary?.GetMaterial(name);
        }

        public List<T> GetMaterials<T>(Construction construction) where T : IMaterial
        {
            if (construction == null || materialLibrary == null)
            {
                return null;
            }

            List<ConstructionLayer> constructionLayers = construction.ConstructionLayers;
            if (constructionLayers == null)
            {
                return null;
            }

            List<T> result = new List<T>();
            foreach (ConstructionLayer constructionLayer in constructionLayers)
            {
                IMaterial material = materialLibrary.GetMaterial(constructionLayer.Name);
                if (material is T)
                {
                    result.Add((T)material);
                }
            }

            return result;
        }

        public List<T> GetMaterials<T>(ApertureConstruction apertureConstruction) where T : IMaterial
        {
            if (apertureConstruction == null || materialLibrary == null)
            {
                return null;
            }

            List<T> result = new List<T>();

            List<ConstructionLayer> constructionLayers = null;

            constructionLayers = apertureConstruction.FrameConstructionLayers;
            if (constructionLayers != null)
            {
                foreach (ConstructionLayer constructionLayer in constructionLayers)
                {
                    IMaterial material = materialLibrary.GetMaterial(constructionLayer.Name);
                    if (material is T && result.Find(x => x.Name == material.Name) == null)
                    {
                        result.Add((T)material);
                    }
                }
            }

            constructionLayers = apertureConstruction.PaneConstructionLayers;
            if (constructionLayers != null)
            {
                foreach (ConstructionLayer constructionLayer in constructionLayers)
                {
                    IMaterial material = materialLibrary.GetMaterial(constructionLayer.Name);
                    if (material is T && result.Find(x => x.Name == material.Name) == null)
                    {
                        result.Add((T)material);
                    }
                }
            }

            return result;
        }

        public bool FromJObject(JObject jObject)
        {
            return FromJsonObject(jObject?.Node as JsonObject);
        }

        public bool FromJsonObject(JsonObject jsonObject)
        {
            if (jsonObject == null)
            {
                return false;
            }

            if (jsonObject.ContainsKey("Name"))
            {
                Name = jsonObject["Name"]?.GetValue<string>();
            }

            if (jsonObject.ContainsKey("Description"))
            {
                Description = jsonObject["Description"]?.GetValue<string>();
            }

            if (jsonObject["ApertureConstructionLibrary"] is JsonObject apertureConstructionLibraryJson)
            {
                apertureConstructionLibrary = Core.Query.IJSAMObject<ApertureConstructionLibrary>(apertureConstructionLibraryJson as JsonObject);
            }

            if (jsonObject["ConstructionLibrary"] is JsonObject constructionLibraryJson)
            {
                constructionLibrary = Core.Query.IJSAMObject<ConstructionLibrary>(constructionLibraryJson as JsonObject);
            }

            if (jsonObject["MaterialLibrary"] is JsonObject materialLibraryJson)
            {
                materialLibrary = Core.Query.IJSAMObject<MaterialLibrary>(materialLibraryJson as JsonObject);
            }

            return true;
        }

        public JObject ToJObject()
        {
            JsonObject jsonObject = ToJsonObject();
            return jsonObject == null ? null : new JObject(jsonObject);
        }

        public JsonObject ToJsonObject()
        {
            JsonObject jsonObject = new JsonObject
            {
                ["_type"] = Core.Query.FullTypeName(this)
            };

            if (Name != null)
            {
                jsonObject["Name"] = Name;
            }

            if (Description != null)
            {
                jsonObject["Description"] = Description;
            }

            if (apertureConstructionLibrary?.ToJsonObject() is JsonObject apertureConstructionLibraryJson)
            {
                jsonObject["ApertureConstructionLibrary"] = apertureConstructionLibraryJson.DeepClone();
            }

            if (constructionLibrary?.ToJsonObject() is JsonObject constructionLibraryJson)
            {
                jsonObject["ConstructionLibrary"] = constructionLibraryJson.DeepClone();
            }

            if (materialLibrary?.ToJsonObject() is JsonObject materialLibraryJson)
            {
                jsonObject["MaterialLibrary"] = materialLibraryJson.DeepClone();
            }

            return jsonObject;
        }
    }
}
