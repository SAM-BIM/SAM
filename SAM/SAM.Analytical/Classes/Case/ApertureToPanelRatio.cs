// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;
using SAM.Core;
using System.Text.Json.Nodes;

namespace SAM.Analytical.Classes
{
    public class ApertureToPanelRatio : IJSAMObject
    {
        private ApertureConstruction apertureConstruction;
        private Range<double> azimuthRange;
        private double ratio;

        public ApertureToPanelRatio(Range<double> azimuthRange, double ratio, ApertureConstruction apertureConstruction)
        {
            this.azimuthRange = azimuthRange;
            this.ratio = ratio;
            this.apertureConstruction = apertureConstruction;
        }

        public ApertureToPanelRatio(JObject jObject)
        {
            FromJObject(jObject);
        }

        public ApertureToPanelRatio(ApertureToPanelRatio apertureToPanelRatio)
        {
            if (apertureToPanelRatio != null)
            {
                azimuthRange = apertureToPanelRatio.AzimuthRange;
                ratio = apertureToPanelRatio.Ratio;
                apertureConstruction = apertureToPanelRatio.ApertureConstruction;
            }
        }

        public ApertureConstruction ApertureConstruction
        {
            get
            {
                return apertureConstruction;
            }

            set
            {
                apertureConstruction = value;
            }
        }

        public string ApertureConstructionName
        {
            get
            {
                return apertureConstruction?.Name;
            }
        }

        public Range<double> AzimuthRange
        {
            get
            {
                return azimuthRange;
            }
        }

        public double Max
        {
            get
            {
                if (azimuthRange is null)
                {
                    azimuthRange = new Range<double>(0.0, 0.0);
                }

                return azimuthRange.Max;
            }

            set
            {
                if (azimuthRange is null)
                {
                    azimuthRange = new Range<double>(value, value);
                }
                else
                {
                    azimuthRange = new Range<double>(azimuthRange.Min, value);
                }
            }
        }

        public double Min
        {
            get
            {
                if (azimuthRange is null)
                {
                    azimuthRange = new Range<double>(0.0, 0.0);
                }

                return azimuthRange.Min;
            }

            set
            {
                if (azimuthRange is null)
                {
                    azimuthRange = new Range<double>(value, value);
                }
                else
                {
                    azimuthRange = new Range<double>(value, azimuthRange.Max);
                }
            }
        }

        public double Ratio
        {
            get
            {
                return ratio;
            }

            set
            {
                ratio = value;
            }
        }

        public virtual bool FromJObject(JObject jObject)
        {
            return FromJsonObject(jObject?.Node as JsonObject);
        }

        protected virtual bool FromJsonObject(JsonObject jsonObject)
        {
            if (jsonObject == null)
            {
                return false;
            }

            if (jsonObject.ContainsKey("Ratio"))
            {
                ratio = jsonObject["Ratio"]?.GetValue<double>() ?? double.NaN;
            }

            if (jsonObject["AzimuthRange"] is JsonObject azimuthRangeJson)
            {
                azimuthRange = Core.Query.IJSAMObject<Range<double>>(new JObject((JsonObject)azimuthRangeJson.DeepClone()));
            }

            if (jsonObject["ApertureConstruction"] is JsonObject apertureConstructionJson)
            {
                apertureConstruction = Core.Query.IJSAMObject<ApertureConstruction>(new JObject((JsonObject)apertureConstructionJson.DeepClone()));
            }

            return true;
        }

        public virtual JObject ToJObject()
        {
            JsonObject jsonObject = ToJsonObject();
            return jsonObject == null ? null : new JObject(jsonObject);
        }

        protected virtual JsonObject ToJsonObject()
        {
            JsonObject result = new JsonObject
            {
                ["_type"] = Core.Query.FullTypeName(this)
            };

            if (azimuthRange?.ToJObject()?.Node is JsonObject azimuthRangeJson)
            {
                result["AzimuthRange"] = azimuthRangeJson.DeepClone();
            }

            if (double.IsNaN(ratio))
            {
                result["Ratio"] = ratio;
            }

            if (apertureConstruction?.ToJObject()?.Node is JsonObject apertureConstructionJson)
            {
                result["ApertureConstruction"] = apertureConstructionJson.DeepClone();
            }

            return result;
        }
    }
}
