// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;
using SAM.Core;
using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace SAM.Analytical
{
    public class SystemTemplate : IJSAMObject, IAnalyticalObject
    {
        private string controls;
        private string cooling;
        private string heating;
        private string plantRoom;
        private string ventilation;
        private string version;

        public SystemTemplate()
        {
            
        }

        public SystemTemplate(string ventilation, string heating, string cooling, string plantRoom, string controls, string version)
        {
            Ventilation = ventilation;
            Heating = heating;
            Cooling = cooling;
            PlantRoom = plantRoom;
            Controls = controls;
            Version = version;

        }

        public SystemTemplate(SystemTemplate systemTemplate)
        {
            if (systemTemplate != null)
            {
                ventilation = systemTemplate.ventilation;
                heating = systemTemplate.heating;
                cooling = systemTemplate.cooling;
                plantRoom = systemTemplate.plantRoom;
                controls = systemTemplate.controls;
                version = systemTemplate.version;
            }
        }
        public SystemTemplate(System.Text.Json.Nodes.JsonObject jsonObject)

        {

            FromJsonObject(jsonObject);

        }

        public string Controls
        {
            get
            {
                return controls;
            }

            set
            {
                controls = value?.Replace(" ", string.Empty);
            }
        }

        public string Cooling
        {
            get
            {
                return cooling;
            }

            set
            {
                cooling = value?.Replace(" ", string.Empty);
            }
        }

        public string Heating
        {
            get
            {
                return heating;
            }

            set
            {
                heating = value?.Replace(" ", string.Empty);
            }
        }

        public bool IsValid
        {
            get
            {
                return !string.IsNullOrWhiteSpace(ToString());
            }
        }
        
        public string PlantRoom
        {
            get
            {
                return plantRoom;
            }

            set
            {
                plantRoom = value?.Replace(" ", string.Empty);
            }
        }

        public string Ventilation
        {
            get
            {
                return ventilation;
            }

            set
            {
                ventilation = value?.Replace(" ", string.Empty);
            }
        }
        
        public string Version
        {
            get
            {
                return version;
            }

            set
            {
                version = value?.Replace(" ", string.Empty);
            }
        }

        public bool IsUnheated()
        {
            return string.IsNullOrWhiteSpace(heating) || heating == "UH";
        }

        public bool IsUncooled()
        {
            return string.IsNullOrWhiteSpace(cooling) || cooling == "UC";
        }

        public bool IsUnventilated()
        {
            return string.IsNullOrWhiteSpace(ventilation) || ventilation == "UV";
        }

        public override bool Equals(object obj)
        {

            return base.Equals(obj);
        }
        public bool FromJsonObject(JsonObject jsonObject)
        {
            if (jsonObject == null)
            {
                return false;
            }

            if (jsonObject.ContainsKey("Ventilation"))
            {
                ventilation = jsonObject["Ventilation"]?.GetValue<string>();
            }

            if (jsonObject.ContainsKey("Heating"))
            {
                heating = jsonObject["Heating"]?.GetValue<string>();
            }

            if (jsonObject.ContainsKey("Cooling"))
            {
                cooling = jsonObject["Cooling"]?.GetValue<string>();
            }

            if (jsonObject.ContainsKey("PlantRoom"))
            {
                plantRoom = jsonObject["PlantRoom"]?.GetValue<string>();
            }

            if (jsonObject.ContainsKey("Controls"))
            {
                controls = jsonObject["Controls"]?.GetValue<string>();
            }

            if (jsonObject.ContainsKey("Version"))
            {
                version = jsonObject["Version"]?.GetValue<string>();
            }


            return true;
        }

        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }
        public JsonObject ToJsonObject()
        {
            JsonObject jsonObject = new JsonObject
            {
                ["_type"] = Core.Query.FullTypeName(this)
            };

            if (ventilation != null)
            {
                jsonObject["Ventilation"] = ventilation;
            }

            if (heating != null)
            {
                jsonObject["Heating"] = heating;
            }

            if (cooling != null)
            {
                jsonObject["Cooling"] = cooling;
            }

            if (plantRoom != null)
            {
                jsonObject["PlantRoom"] = plantRoom;
            }

            if (controls != null)
            {
                jsonObject["Controls"] = controls;
            }

            if (version != null)
            {
                jsonObject["Version"] = version;
            }


            return jsonObject;
        }

        public override string ToString()
        {
            List<string> strings = [];
            
            if(!string.IsNullOrWhiteSpace(ventilation))
            {
                strings.Add("V." + ventilation);
            }

            if (!string.IsNullOrWhiteSpace(heating))
            {
                strings.Add("H." + heating);
            }

            if (!string.IsNullOrWhiteSpace(cooling))
            {
                strings.Add("C." + cooling);
            }

            if (!string.IsNullOrWhiteSpace(plantRoom))
            {
                strings.Add("PR." + plantRoom);
            }

            if (!string.IsNullOrWhiteSpace(controls))
            {
                strings.Add("CTL." + controls);
            }

            if (!string.IsNullOrWhiteSpace(version))
            {
                strings.Add("VER." + version);
            }

            return string.Join("_", strings);
        }
    }
}
