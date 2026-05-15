// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;
using System;
using System.Text.Json.Nodes;

namespace SAM.Analytical
{
    /// <summary>
    /// Represents an coil unit object in the analytical domain
    /// </summary>
    public abstract class Coil : SimpleEquipment, ISection
    {
        private double fluidReturnTemperature = double.NaN;
        private double fluidSupplyTemperature = double.NaN;
        private double contactFactor = double.NaN;

        private bool summer = true;
        private double summerOffTemperature = double.NaN;

        private bool winter = true;
        private double winterOffTemperature = double.NaN;

        public Coil(string name, double fluidSupplyTemperature, double fluidReturnTemperature, double contactFactor, double offTemperature)
            : base(name)
        {
            this.fluidReturnTemperature = fluidReturnTemperature;
            this.fluidSupplyTemperature = fluidSupplyTemperature;
            this.contactFactor = contactFactor;
            summerOffTemperature = offTemperature;
            winterOffTemperature = offTemperature;
        }

        public Coil(string name, double fluidSupplyTemperature, double fluidReturnTemperature, double contactFactor, double summerOffTemperature, double winterOffTemperature)
            : base(name)
        {
            this.fluidReturnTemperature = fluidReturnTemperature;
            this.fluidSupplyTemperature = fluidSupplyTemperature;
            this.contactFactor = contactFactor;
            this.summerOffTemperature = summerOffTemperature;
            this.winterOffTemperature = winterOffTemperature;
        }
        public Coil(System.Text.Json.Nodes.JsonObject jsonObject)

            : base(jsonObject)

        {

        }

        public Coil(Coil coil)
            : base(coil)
        {
            if (coil != null)
            {
                fluidReturnTemperature = coil.fluidReturnTemperature;
                fluidSupplyTemperature = coil.fluidSupplyTemperature;
                contactFactor = coil.contactFactor;
                winterOffTemperature = coil.winterOffTemperature;
                summerOffTemperature = coil.summerOffTemperature;
            }
        }

        public Coil(Guid guid, string name, double fluidSupplyTemperature, double fluidReturnTemperature, double contactFactor, double offTemperature)
            : base(guid, name)
        {
            this.fluidReturnTemperature = fluidReturnTemperature;
            this.fluidSupplyTemperature = fluidSupplyTemperature;
            this.contactFactor = contactFactor;
            summerOffTemperature = offTemperature;
            winterOffTemperature = offTemperature;
        }

        public double FluidReturnTemperature
        {
            get
            {
                return fluidReturnTemperature;
            }

            set
            {
                fluidReturnTemperature = value;
            }
        }

        public double FluidSupplyTemperature
        {
            get
            {
                return fluidSupplyTemperature;
            }

            set
            {
                fluidReturnTemperature = value;
            }
        }

        public double ContactFactor
        {
            get
            {
                return contactFactor;
            }

            set
            {
                contactFactor = value;
            }
        }

        /// <summary>
        /// Winter temperature of air exiting the coil
        /// </summary>
        public double WinterOffTemperature
        {
            get
            {
                return winterOffTemperature;
            }

            set
            {
                winterOffTemperature = value;
            }
        }

        /// <summary>
        /// Summer temperature of air exiting the coil
        /// </summary>
        public double SummerOffTemperature
        {
            get
            {
                return summerOffTemperature;
            }

            set
            {
                summerOffTemperature = value;
            }
        }

        /// <summary>
        /// Off/On during summer season
        /// </summary>
        public bool Summer
        {
            get
            {
                return summer;
            }

            set
            {
                summer = value;
            }
        }

        /// <summary>
        /// Off/On during winter season
        /// </summary>
        public bool Winter
        {
            get
            {
                return winter;
            }

            set
            {
                winter = value;
            }
        }

        public override bool FromJsonObject(JsonObject jsonObject)
        {
            if (!base.FromJsonObject(jsonObject))
                return false;

            if (jsonObject.ContainsKey("FluidReturnTemperature"))
            {
                fluidReturnTemperature = jsonObject["FluidReturnTemperature"]?.GetValue<double>() ?? double.NaN;
            }

            if (jsonObject.ContainsKey("FluidSupplyTemperature"))
            {
                fluidSupplyTemperature = jsonObject["FluidSupplyTemperature"]?.GetValue<double>() ?? double.NaN;
            }

            if (jsonObject.ContainsKey("ContactFactor"))
            {
                contactFactor = jsonObject["ContactFactor"]?.GetValue<double>() ?? double.NaN;
            }

            if (jsonObject.ContainsKey("SummerOffTemperature"))
            {
                summerOffTemperature = jsonObject["SummerOffTemperature"]?.GetValue<double>() ?? double.NaN;
            }

            if (jsonObject.ContainsKey("WinterOffTemperature"))
            {
                winterOffTemperature = jsonObject["WinterOffTemperature"]?.GetValue<double>() ?? double.NaN;
            }

            return true;
        }

        public override JsonObject ToJsonObject()
        {
            JsonObject jsonObject = base.ToJsonObject();
            if (jsonObject == null)
                return null;

            if (!double.IsNaN(fluidSupplyTemperature))
            {
                jsonObject["FluidSupplyTemperature"] = fluidSupplyTemperature;
            }

            if (!double.IsNaN(fluidReturnTemperature))
            {
                jsonObject["FluidReturnTemperature"] = fluidReturnTemperature;
            }

            if (!double.IsNaN(contactFactor))
            {
                jsonObject["ContactFactor"] = contactFactor;
            }

            if (!double.IsNaN(summerOffTemperature))
            {
                jsonObject["SummerOffTemperature"] = summerOffTemperature;
            }

            if (!double.IsNaN(winterOffTemperature))
            {
                jsonObject["WinterOffTemperature"] = winterOffTemperature;
            }

            return jsonObject;
        }
    }
}
