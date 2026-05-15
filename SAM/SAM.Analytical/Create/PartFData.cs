// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Classes;
using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace SAM.Analytical
{
    public static partial class Create
    {
        public static PartFData PartFData(string path)
        {
            PartFData result = new ();

            if(!string.IsNullOrWhiteSpace(path) && System.IO.File.Exists(path))
            {
                string json = System.IO.File.ReadAllText(path);

                JsonObject jsonObject = JsonNode.Parse(json) as JsonObject;

                if(jsonObject != null && jsonObject["WholeDwellingRates_Lps"] is JsonObject wholeDwellingRates)
                {
                    foreach (KeyValuePair<string, JsonNode> property in wholeDwellingRates)
                    {
                        string name = property.Key;
                        JsonNode value = property.Value;

                        double value_Temp = value?.GetValue<double>() ?? double.NaN;

                        if(name == "IncrementAbove5" && !double.IsNaN(value_Temp))
                        {
                            result.IncrementAbove5 = value_Temp;
                            continue;
                        }
                        else if (name == "AreaRate_LpsPerM2" && !double.IsNaN(value_Temp))
                        {
                            result.AreaRate_LpsPerM2 = value_Temp;
                            continue;
                        }
                        else if(Core.Query.TryConvert<int>(name, out int @int) && !double.IsNaN(value_Temp))
                        {
                            result.WholeDwellingRates_Lps[@int] = value_Temp;
                        }

                    }
                }

                if(jsonObject != null && jsonObject["Categories"] is JsonArray categoriesArray)
                {
                    if(categoriesArray != null)
                    {
                        foreach(JsonNode categoryNode in categoriesArray)
                        {
                            JsonObject jsonObject_Category = categoryNode as JsonObject;
                            if(jsonObject_Category == null)
                            {
                                continue;
                            }

                            string name = jsonObject_Category["Category"]?.GetValue<string>();
                            if(string.IsNullOrWhiteSpace(name))
                            {
                                continue;
                            }

                            Enums.PartFType partFType = Enums.PartFType.Habitable;
                            string category = jsonObject_Category["PartFCategory"]?.GetValue<string>();
                            if (!string.IsNullOrWhiteSpace(category))
                            {
                                partFType = Core.Query.Enum<Enums.PartFType>(category);
                            }

                            Enums.PartFVentilationType partFVentilationType = Enums.PartFVentilationType.supply;
                            string ventilationType = jsonObject_Category["VentilationType"]?.GetValue<string>();
                            if (!string.IsNullOrWhiteSpace(ventilationType))
                            {
                                partFVentilationType = Core.Query.Enum<Enums.PartFVentilationType>(ventilationType);
                            }

                            bool isBedroom = false;
                            if (jsonObject_Category["IsBedroom"] != null)
                            {
                                isBedroom = jsonObject_Category["IsBedroom"].GetValue<bool>();
                            }

                            double? minFlowRate_Lps = jsonObject_Category["MinFlowRate_Lps"]?.GetValue<double?>();

                            bool includeInFloorAreaCheck = false;
                            if (jsonObject_Category["IncludeInFloorAreaCheck"] != null)
                            {
                                includeInFloorAreaCheck = jsonObject_Category["IncludeInFloorAreaCheck"].GetValue<bool>();
                            }

                            bool isTerminalSpace = false;
                            if (jsonObject_Category["IsTerminalSpace"] != null)
                            {
                                isTerminalSpace = jsonObject_Category["IsTerminalSpace"].GetValue<bool>();
                            }

                            bool scaleSupplyWithVolume = false;
                            if (jsonObject_Category["ScaleSupplyWithVolume"] != null)
                            {
                                scaleSupplyWithVolume = jsonObject_Category["ScaleSupplyWithVolume"].GetValue<bool>();
                            }

                            bool scaleExtractAboveMinimum = false;
                            if (jsonObject_Category["ScaleExtractAboveMinimum"] != null)
                            {
                                scaleExtractAboveMinimum = jsonObject_Category["ScaleExtractAboveMinimum"].GetValue<bool>();
                            }

                            string defaultFlowWeightBasis = jsonObject_Category["DefaultFlowWeightBasis"]?.GetValue<string>();

                            List<string> synonyms = [];
                            if (jsonObject_Category["Synonyms"] is JsonArray synonymsArray)
                            {
                                foreach(JsonNode synonymNode in synonymsArray)
                                {
                                    string synonym = synonymNode?.GetValue<string>();
                                    if (synonym != null)
                                    {
                                        synonyms.Add(synonym);
                                    }
                                }
                            }

                            PartFCategory partFCategory = new (
                                name,
                                partFType,
                                partFVentilationType,
                                isBedroom,
                                minFlowRate_Lps,
                                includeInFloorAreaCheck,
                                isTerminalSpace,
                                scaleSupplyWithVolume,
                                scaleExtractAboveMinimum,
                                defaultFlowWeightBasis,
                                synonyms);

                            result.PartFCategories[partFCategory.Name] = partFCategory;
                        }
                    }
                }
            }

            return result;
        }
    }
}
