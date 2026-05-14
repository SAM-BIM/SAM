// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;
using SAM.Core;

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

        public override bool FromJObject(JObject jObject)
        {
            bool result = base.FromJObject(jObject);

            if (!result)
            {
                return false;
            }

            if (jObject.ContainsKey("NCMName"))
            {
                NCMName = new NCMName(jObject.Value<JObject>("NCMName"));
            }
            else if (jObject.ContainsKey("Type"))
            {
                NCMName = jObject.Value<string>("Type");
            }

            // Enum fields default to non-Undefined values, but ToJObject below
            // skips emission when the parsed value is Undefined. Reset to
            // Undefined when the key is missing or unparseable so a no-key input
            // round-trips back to a no-key output (otherwise the constructor
            // default leaks into the next serialization round).
            SystemType = jObject.ContainsKey("SystemType")
                ? Core.Query.Enum<NCMSystemType>(jObject.Value<string>("SystemType"))
                : NCMSystemType.Undefined;

            LightingOccupancyControls = jObject.ContainsKey("LightingOccupancyControls")
                ? Core.Query.Enum<LightingOccupancyControls>(jObject.Value<string>("LightingOccupancyControls"))
                : LightingOccupancyControls.Undefined;

            LightingPhotoelectricControls = jObject.ContainsKey("LightingPhotoelectricControls")
                ? Core.Query.Enum<LightingPhotoelectricControls>(jObject.Value<string>("LightingPhotoelectricControls"))
                : LightingPhotoelectricControls.Undefined;

            Country = jObject.ContainsKey("Country")
                ? Core.Query.Enum<NCMCountry>(jObject.Value<string>("Country"))
                : NCMCountry.Undefined;

            if (jObject.ContainsKey("LightingPhotoelectricBackSpaceSensor"))
            {
                LightingPhotoelectricBackSpaceSensor = jObject.Value<bool>("LightingPhotoelectricBackSpaceSensor");
            }

            if (jObject.ContainsKey("LightingPhotoelectricControlsTimeSwitch"))
            {
                LightingPhotoelectricControlsTimeSwitch = jObject.Value<bool>("LightingPhotoelectricControlsTimeSwitch");
            }

            if (jObject.ContainsKey("LightingDaylightFactorMethod"))
            {
                LightingDaylightFactorMethod = jObject.Value<bool>("LightingDaylightFactorMethod");
            }

            if (jObject.ContainsKey("IsMainsGasAvailable"))
            {
                IsMainsGasAvailable = jObject.Value<bool>("IsMainsGasAvailable");
            }

            if (jObject.ContainsKey("LightingPhotoelectricParasiticPower"))
            {
                LightingPhotoelectricParasiticPower = jObject.Value<double>("LightingPhotoelectricParasiticPower");
            }

            if (jObject.ContainsKey("AirPermeability"))
            {
                AirPermeability = jObject.Value<double>("AirPermeability");
            }

            if (jObject.ContainsKey("Description"))
            {
                Description = jObject.Value<string>("Description");
            }

            return result;
        }

        public override JObject ToJObject()
        {
            JObject jObject = base.ToJObject();
            if (jObject == null)
            {
                return null;
            }

            if (NCMName != null)
            {
                jObject.Add("NCMName", NCMName.ToJObject());
            }

            if (SystemType != NCMSystemType.Undefined)
            {
                jObject.Add("SystemType", SystemType.ToString());
            }

            if (LightingOccupancyControls != LightingOccupancyControls.Undefined)
            {
                jObject.Add("LightingOccupancyControls", LightingOccupancyControls.ToString());
            }

            if (LightingPhotoelectricControls != LightingPhotoelectricControls.Undefined)
            {
                jObject.Add("LightingPhotoelectricControls", LightingPhotoelectricControls.ToString());
            }

            if (Country != NCMCountry.Undefined)
            {
                jObject.Add("Country", Country.ToString());
            }

            jObject.Add("LightingPhotoelectricBackSpaceSensor", LightingPhotoelectricBackSpaceSensor);
            jObject.Add("LightingPhotoelectricControlsTimeSwitch", LightingPhotoelectricControlsTimeSwitch);
            jObject.Add("LightingDaylightFactorMethod", LightingDaylightFactorMethod);
            jObject.Add("IsMainsGasAvailable", IsMainsGasAvailable);

            if (!double.IsNaN(LightingPhotoelectricParasiticPower))
            {
                jObject.Add("LightingPhotoelectricParasiticPower", LightingPhotoelectricParasiticPower);
            }

            if (!double.IsNaN(AirPermeability))
            {
                jObject.Add("AirPermeability", AirPermeability);
            }

            if (Description != null)
            {
                jObject.Add("Description", Description);
            }

            return jObject;
        }
    }
}
