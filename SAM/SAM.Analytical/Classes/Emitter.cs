// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;
using SAM.Core;
using System;
using System.Text.Json.Nodes;

namespace SAM.Analytical
{
    public class Emitter : SAMObject, IAnalyticalObject
    {
        private EmitterCategory emitterCategory;
        private double radiantProportion;
        private double viewCoefficient;

        public Emitter(string name)
            : base(name)
        {
            radiantProportion = double.NaN;
            viewCoefficient = double.NaN;
            emitterCategory = EmitterCategory.Undefined;
        }

        public Emitter(Guid guid, string name)
            : base(guid, name)
        {
            radiantProportion = double.NaN;
            viewCoefficient = double.NaN;
            emitterCategory = EmitterCategory.Undefined;
        }

        public Emitter(Guid guid, string name, EmitterCategory emitterCategory, double radiantProportion, double viewCoefficient)
            : base(guid, name)
        {
            this.radiantProportion = radiantProportion;
            this.viewCoefficient = viewCoefficient;
            this.emitterCategory = emitterCategory;
        }

        public Emitter(Emitter emitter)
            : base(emitter)
        {
            radiantProportion = emitter.radiantProportion;
            viewCoefficient = emitter.viewCoefficient;
            emitterCategory = emitter.emitterCategory;
        }

        public Emitter(JObject jObject)
            : base(jObject)
        {
        }

        public double RadiantProportion
        {
            get
            {
                return radiantProportion;
            }
        }

        public double ViewCoefficient
        {
            get
            {
                return viewCoefficient;
            }
        }

        public EmitterCategory EmitterCategory
        {
            get
            {
                return emitterCategory;
            }
        }

        public override bool FromJsonObject(JsonObject jsonObject)
        {
            if (!base.FromJsonObject(jsonObject))
                return false;

            if (jsonObject.ContainsKey("RadiantProportion"))
                radiantProportion = jsonObject["RadiantProportion"]?.GetValue<double>() ?? double.NaN;

            if (jsonObject.ContainsKey("ViewCoefficient"))
                viewCoefficient = jsonObject["ViewCoefficient"]?.GetValue<double>() ?? double.NaN;

            if (jsonObject.ContainsKey("EmitterCategory"))
                emitterCategory = jsonObject["EmitterCategory"]?.GetValue<string>().Enum<EmitterCategory>() ?? EmitterCategory.Undefined;

            return true;
        }

        public override JsonObject ToJsonObject()
        {
            JsonObject jsonObject = base.ToJsonObject();
            if (jsonObject == null)
                return null;

            if (!double.IsNaN(radiantProportion))
                jsonObject["RadiantProportion"] = radiantProportion;

            if (!double.IsNaN(viewCoefficient))
                jsonObject["ViewCoefficient"] = viewCoefficient;

            if (emitterCategory != EmitterCategory.Undefined)
                jsonObject["EmitterCategory"] = emitterCategory.ToString();

            return jsonObject;
        }
    }
}
