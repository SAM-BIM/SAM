// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;

using System;
using System.Text.Json.Nodes;

namespace SAM.Core
{
    public abstract class FluidMaterial : Material
    {
        private double dynamicViscosity = double.NaN;

        public FluidMaterial(string name)
            : base(name)
        {

        }

        public FluidMaterial(Guid guid, string name, string displayName, string description, double thermalConductivity, double density, double specificHeatCapacity, double dynamicViscosity)
        : base(guid, name, displayName, description, thermalConductivity, density, specificHeatCapacity)
        {
            this.dynamicViscosity = dynamicViscosity;
        }

        public FluidMaterial(string name, string group, string displayName, string description, double thermalConductivity, double specificHeatCapacity, double density, double dynamicViscosity)
            : base(name, group, displayName, description, thermalConductivity, specificHeatCapacity, density)
        {
            this.dynamicViscosity = dynamicViscosity;
        }

        public FluidMaterial(Guid guid, string name)
            : base(guid, name)
        {

        }

        public FluidMaterial(JObject jObject)
            : base(jObject)
        {
        }

        public FluidMaterial(FluidMaterial fluidMaterial)
            : base(fluidMaterial)
        {
            dynamicViscosity = fluidMaterial.dynamicViscosity;
        }

        public FluidMaterial(string name, Guid guid, FluidMaterial fluidMaterial, string displayName, string description)
            : base(name, guid, fluidMaterial, displayName, description)
        {
            dynamicViscosity = fluidMaterial.dynamicViscosity;
        }

        /// <summary>
        ///  Dynamic Viscosity of Fluid [kg/(m*s)]
        /// </summary>
        public double DynamicViscosity
        {
            get
            {
                return dynamicViscosity;
            }
        }

        public override bool FromJsonObject(JsonObject jsonObject)
        {
            if (!base.FromJsonObject(jsonObject))
                return false;

            if (jsonObject.ContainsKey("DynamicViscosity"))
                dynamicViscosity = jsonObject["DynamicViscosity"]?.GetValue<double>() ?? double.NaN;

            return true;
        }

        public override JsonObject ToJsonObject()
        {
            JsonObject jsonObject = base.ToJsonObject();
            if (jsonObject == null)
                return null;

            if (!double.IsNaN(dynamicViscosity))
                jsonObject["DynamicViscosity"] = dynamicViscosity;

            return jsonObject;
        }
    }
}
