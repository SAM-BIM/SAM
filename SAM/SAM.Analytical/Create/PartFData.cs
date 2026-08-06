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
                        else if (name == "SetbackFlowRateFactor" || name == "BackgroundFlowRateFactor")
                        {
                            //Assigned through the property, which rejects zero, a negative factor, a
                            //factor above 1, NaN and infinity and substitutes the documented default
                            //rather than letting a bad data file produce a setback rate above the
                            //continuous design rate or a rate that is not a number.
                            //
                            //BackgroundFlowRateFactor is still accepted so a rule set written by the
                            //interim build that used that name keeps working.
                            result.SetbackFlowRateFactor = value_Temp;
                            continue;
                        }
                        else if (name == "OneHabitableRoomRate_Lps" && !double.IsNaN(value_Temp))
                        {
                            result.OneHabitableRoomRate_Lps = value_Temp;
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

                            bool isCookingSpace = false;
                            if (jsonObject_Category["IsCookingSpace"] != null)
                            {
                                isCookingSpace = jsonObject_Category["IsCookingSpace"].GetValue<bool>();
                            }

                            //Links the category to the shared semantic vocabulary. Absent in a rule set
                            //written before that vocabulary existed, in which case the category is
                            //matched by its Synonyms alone.
                            SpaceUse spaceUse = SpaceUse.Undefined;
                            string spaceUseName = jsonObject_Category["SpaceUse"]?.GetValue<string>();
                            if (!string.IsNullOrWhiteSpace(spaceUseName))
                            {
                                spaceUse = Core.Query.Enum<SpaceUse>(spaceUseName);
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
                                synonyms,
                                isCookingSpace,
                                spaceUse);

                            result.PartFCategories[partFCategory.Name] = partFCategory;
                        }
                    }
                }
            }

            return result;
        }
    }
}
