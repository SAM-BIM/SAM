// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;
using SAM.Core;
using System.Text.Json.Nodes;

namespace SAM.Analytical
{
    public class ProfileModifier : IndexedSimpleModifier
    {
        public ProfileModifier(ArithmeticOperator arithmeticOperator, Profile profile, double setback)
        {
            ArithmeticOperator = arithmeticOperator;
            Profile = Core.Query.Clone(profile);
            Setback = setback;
        }

        public ProfileModifier(ProfileModifier scheduleModifier)
            : base(scheduleModifier)
        {
            if (scheduleModifier != null)
            {
                Profile = Core.Query.Clone(scheduleModifier.Profile);
                Setback = scheduleModifier.Setback;
            }
        }
        public ProfileModifier(System.Text.Json.Nodes.JsonObject jsonObject)

            : base(jsonObject)

        {

        }

        public double Setback { get; set; }

        private Profile Profile { get; set; }

        public override bool ContainsIndex(int index)
        {
            if (Profile == null)
            {
                return false;
            }

            return Profile.TryGetValue(index, out Profile profile, out double value);
        }

        public override bool FromJsonObject(JsonObject jsonObject)
        {
            bool result = base.FromJsonObject(jsonObject);
            if (!result)
            {
                return result;
            }

            if (jsonObject["Profile"] is JsonObject profileJson)
            {
                Profile = Core.Query.IJSAMObject<Profile>(profileJson as JsonObject);
            }

            if (jsonObject.ContainsKey("Setback"))
            {
                Setback = jsonObject["Setback"]?.GetValue<double>() ?? double.NaN;
            }

            return result;
        }

        public override double GetCalculatedValue(int index, double value)
        {
            if (!Profile.TryGetValue(index, out Profile profile, out double value_Temp))
            {
                return double.NaN;
            }

            if (value == 1)
            {
                return value;
            }

            if (value == 0)
            {
                return Setback;
            }

            return Setback * value;
        }

        public override JsonObject ToJsonObject()
        {
            JsonObject result = base.ToJsonObject();
            if (result == null)
            {
                return null;
            }

            if (Profile?.ToJsonObject() is JsonObject profileJson)
            {
                result["Profile"] = profileJson.DeepClone();
            }

            if (!double.IsNaN(Setback))
            {
                result["Setback"] = Setback;
            }

            return result;
        }
    }
}
