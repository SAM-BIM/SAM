// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;
using SAM.Core;
using System.Text.Json.Nodes;

namespace SAM.Analytical
{
    /// <summary>
    /// Data for National Calculation Method
    /// </summary>
    public class NCMData : ParameterizedSAMObject
    {
        public NCMName NCMName { get; set; } = "Unoccupied and Unconditioned";

        public NCMSystemType SystemType { get; set; } = NCMSystemType.NaturalVentilation;

        public LightingOccupancyControls LightingOccupancyControls { get; set; } = LightingOccupancyControls.ManualOn_ManualOff;

        public LightingPhotoelectricControls LightingPhotoelectricControls { get; set; } = LightingPhotoelectricControls.None;

        public NCMCountry Country { get; set; } = NCMCountry.England;

        public bool LightingPhotoelectricBackSpaceSensor { get; set; } = false;

        public bool LightingPhotoelectricControlsTimeSwitch { get; set; } = false;

        public bool LightingDaylightFactorMethod { get; set; } = false;

        public bool IsMainsGasAvailable { get; set; } = false;


        /// <summary>
        /// Lighting Photoelectric Parasitic Power [W]
        /// </summary>
        public double LightingPhotoelectricParasiticPower { get; set; } = 0.1;

        public double AirPermeability { get; set; } = 0;

        public string Description { get; set; } = null;

        public string Name
        {
            get
            {
                return NCMName?.FullName;
            }

            set
            {
                NCMName = value;
            }
        }

        public NCMData()
            : base()
        {

        }

        public NCMData(JObject jObject)
            : base(jObject)
        {
        }

        public NCMData(NCMData nCMData)
            : base(nCMData)
        {
            if (nCMData != null)
            {
                LightingOccupancyControls = nCMData.LightingOccupancyControls;
                LightingPhotoelectricControls = nCMData.LightingPhotoelectricControls;
                Country = nCMData.Country;
                LightingPhotoelectricBackSpaceSensor = nCMData.LightingPhotoelectricBackSpaceSensor;
                LightingPhotoelectricControlsTimeSwitch = nCMData.LightingPhotoelectricControlsTimeSwitch;
                LightingDaylightFactorMethod = nCMData.LightingDaylightFactorMethod;
                IsMainsGasAvailable = nCMData.IsMainsGasAvailable;
                LightingPhotoelectricParasiticPower = nCMData.LightingPhotoelectricParasiticPower;
                AirPermeability = nCMData.AirPermeability;
                NCMName = nCMData.NCMName;
                SystemType = nCMData.SystemType;
                Description = nCMData.Description;
            }
        }

        public override bool FromJsonObject(JsonObject jsonObject)
        {
            bool result = base.FromJsonObject(jsonObject);

            if (!result)
            {
                return false;
            }

            if (jsonObject["NCMName"] is JsonObject ncmNameJson)
            {
                NCMName = new NCMName((JsonObject)ncmNameJson.DeepClone());
            }
            else if (jsonObject.ContainsKey("Type"))
            {
                NCMName = jsonObject["Type"]?.GetValue<string>();
            }

            // Enum fields default to non-Undefined values, but ToJObject below
            // skips emission when the parsed value is Undefined. Reset to
            // Undefined when the key is missing or unparseable so a no-key input
            // round-trips back to a no-key output (otherwise the constructor
            // default leaks into the next serialization round).
            SystemType = jsonObject.ContainsKey("SystemType")
                ? Core.Query.Enum<NCMSystemType>(jsonObject["SystemType"]?.GetValue<string>())
                : NCMSystemType.Undefined;

            LightingOccupancyControls = jsonObject.ContainsKey("LightingOccupancyControls")
                ? Core.Query.Enum<LightingOccupancyControls>(jsonObject["LightingOccupancyControls"]?.GetValue<string>())
                : LightingOccupancyControls.Undefined;

            LightingPhotoelectricControls = jsonObject.ContainsKey("LightingPhotoelectricControls")
                ? Core.Query.Enum<LightingPhotoelectricControls>(jsonObject["LightingPhotoelectricControls"]?.GetValue<string>())
                : LightingPhotoelectricControls.Undefined;

            Country = jsonObject.ContainsKey("Country")
                ? Core.Query.Enum<NCMCountry>(jsonObject["Country"]?.GetValue<string>())
                : NCMCountry.Undefined;

            if (jsonObject.ContainsKey("LightingPhotoelectricBackSpaceSensor"))
            {
                LightingPhotoelectricBackSpaceSensor = jsonObject["LightingPhotoelectricBackSpaceSensor"]?.GetValue<bool>() ?? false;
            }

            if (jsonObject.ContainsKey("LightingPhotoelectricControlsTimeSwitch"))
            {
                LightingPhotoelectricControlsTimeSwitch = jsonObject["LightingPhotoelectricControlsTimeSwitch"]?.GetValue<bool>() ?? false;
            }

            if (jsonObject.ContainsKey("LightingDaylightFactorMethod"))
            {
                LightingDaylightFactorMethod = jsonObject["LightingDaylightFactorMethod"]?.GetValue<bool>() ?? false;
            }

            if (jsonObject.ContainsKey("IsMainsGasAvailable"))
            {
                IsMainsGasAvailable = jsonObject["IsMainsGasAvailable"]?.GetValue<bool>() ?? false;
            }

            if (jsonObject.ContainsKey("LightingPhotoelectricParasiticPower"))
            {
                LightingPhotoelectricParasiticPower = jsonObject["LightingPhotoelectricParasiticPower"]?.GetValue<double>() ?? double.NaN;
            }

            if (jsonObject.ContainsKey("AirPermeability"))
            {
                AirPermeability = jsonObject["AirPermeability"]?.GetValue<double>() ?? double.NaN;
            }

            if (jsonObject.ContainsKey("Description"))
            {
                Description = jsonObject["Description"]?.GetValue<string>();
            }

            return result;
        }

        public override JsonObject ToJsonObject()
        {
            JsonObject jsonObject = base.ToJsonObject();
            if (jsonObject == null)
            {
                return null;
            }

            if (NCMName?.ToJsonObject() is JsonObject ncmNameJson)
            {
                jsonObject["NCMName"] = ncmNameJson.DeepClone();
            }

            if (SystemType != NCMSystemType.Undefined)
            {
                jsonObject["SystemType"] = SystemType.ToString();
            }

            if (LightingOccupancyControls != LightingOccupancyControls.Undefined)
            {
                jsonObject["LightingOccupancyControls"] = LightingOccupancyControls.ToString();
            }

            if (LightingPhotoelectricControls != LightingPhotoelectricControls.Undefined)
            {
                jsonObject["LightingPhotoelectricControls"] = LightingPhotoelectricControls.ToString();
            }

            if (Country != NCMCountry.Undefined)
            {
                jsonObject["Country"] = Country.ToString();
            }

            jsonObject["LightingPhotoelectricBackSpaceSensor"] = LightingPhotoelectricBackSpaceSensor;
            jsonObject["LightingPhotoelectricControlsTimeSwitch"] = LightingPhotoelectricControlsTimeSwitch;
            jsonObject["LightingDaylightFactorMethod"] = LightingDaylightFactorMethod;
            jsonObject["IsMainsGasAvailable"] = IsMainsGasAvailable;

            if (!double.IsNaN(LightingPhotoelectricParasiticPower))
            {
                jsonObject["LightingPhotoelectricParasiticPower"] = LightingPhotoelectricParasiticPower;
            }

            if (!double.IsNaN(AirPermeability))
            {
                jsonObject["AirPermeability"] = AirPermeability;
            }

            if (Description != null)
            {
                jsonObject["Description"] = Description;
            }

            return jsonObject;
        }
    }
}
