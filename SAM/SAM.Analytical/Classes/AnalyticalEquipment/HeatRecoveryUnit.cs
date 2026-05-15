// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Text.Json.Nodes;

namespace SAM.Analytical
{
    /// <summary>
    /// Represents an heat recovery unit unit object in the analytical domain
    /// </summary>
    public class HeatRecoveryUnit : SimpleEquipment, ISection
    {
        private double winterSensibleEfficiency = double.NaN;
        private double winterLatentEfficiency = double.NaN;
        private double summerSensibleEfficiency = double.NaN;
        private double summerLatentEfficiency = double.NaN;
        private double winterRelativeHumidity = double.NaN;
        private double winterDryBulbTemperature = double.NaN;
        private double summerRelativeHumidity = double.NaN;
        private double summerDryBulbTemperature = double.NaN;

        public HeatRecoveryUnit(
            double winterSensibleEfficiency,
            double winterLatentEfficiency,
            double summerSensibleEfficiency,
            double summerLatentEfficiency,
            double winterRelativeHumidity,
            double winterDryBulbTemperature,
            double summerRelativeHumidity,
            double summerDryBulbTemperature)
            : base("Heat Recovery Unit")
        {
            this.winterSensibleEfficiency = winterSensibleEfficiency;
            this.winterLatentEfficiency = winterLatentEfficiency;
            this.summerSensibleEfficiency = summerSensibleEfficiency;
            this.summerLatentEfficiency = summerLatentEfficiency;
            this.winterRelativeHumidity = winterRelativeHumidity;
            this.winterDryBulbTemperature = winterDryBulbTemperature;
            this.summerRelativeHumidity = summerRelativeHumidity;
            this.summerDryBulbTemperature = summerDryBulbTemperature;
        }

        public HeatRecoveryUnit(string name,
            double winterSensibleEfficiency,
            double winterLatentEfficiency,
            double summerSensibleEfficiency,
            double summerLatentEfficiency,
            double winterRelativeHumidity,
            double winterDryBulbTemperature,
            double summerRelativeHumidity,
            double summerDryBulbTemperature)
            : base(name)
        {
            this.winterSensibleEfficiency = winterSensibleEfficiency;
            this.winterLatentEfficiency = winterLatentEfficiency;
            this.summerSensibleEfficiency = summerSensibleEfficiency;
            this.summerLatentEfficiency = summerLatentEfficiency;
            this.winterRelativeHumidity = winterRelativeHumidity;
            this.winterDryBulbTemperature = winterDryBulbTemperature;
            this.summerRelativeHumidity = summerRelativeHumidity;
            this.summerDryBulbTemperature = summerDryBulbTemperature;
        }
        public HeatRecoveryUnit(System.Text.Json.Nodes.JsonObject jsonObject)

            : base(jsonObject)

        {

        }

        public HeatRecoveryUnit(HeatRecoveryUnit heatRecoveryUnit)
            : base(heatRecoveryUnit)
        {
            if (heatRecoveryUnit != null)
            {
                winterSensibleEfficiency = heatRecoveryUnit.winterSensibleEfficiency;
                winterLatentEfficiency = heatRecoveryUnit.winterLatentEfficiency;
                summerSensibleEfficiency = heatRecoveryUnit.summerSensibleEfficiency;
                summerLatentEfficiency = heatRecoveryUnit.summerLatentEfficiency;
                winterRelativeHumidity = heatRecoveryUnit.winterRelativeHumidity;
                winterDryBulbTemperature = heatRecoveryUnit.winterDryBulbTemperature;
                summerRelativeHumidity = heatRecoveryUnit.summerRelativeHumidity;
                summerDryBulbTemperature = heatRecoveryUnit.summerDryBulbTemperature;
            }
        }

        public HeatRecoveryUnit(Guid guid, string name)
            : base(guid, name)
        {

        }

        public double WinterSensibleEfficiency
        {
            get
            {
                return winterSensibleEfficiency;
            }

            set
            {
                winterSensibleEfficiency = value;
            }
        }

        public double WinterLatentEfficiency
        {
            get
            {
                return winterLatentEfficiency;
            }

            set
            {
                winterLatentEfficiency = value;
            }
        }

        public double SummerSensibleEfficiency
        {
            get
            {
                return summerSensibleEfficiency;
            }

            set
            {
                summerSensibleEfficiency = value;
            }
        }

        public double SummerLatentEfficiency
        {
            get
            {
                return summerLatentEfficiency;
            }

            set
            {
                summerLatentEfficiency = value;
            }
        }

        public double WinterRelativeHumidity
        {
            get
            {
                return winterRelativeHumidity;
            }

            set
            {
                winterRelativeHumidity = value;
            }
        }

        public double WinterDryBulbTemperature
        {
            get
            {
                return winterDryBulbTemperature;
            }

            set
            {
                winterDryBulbTemperature = value;
            }
        }

        public double SummerRelativeHumidity
        {
            get
            {
                return summerRelativeHumidity;
            }

            set
            {
                summerRelativeHumidity = value;
            }
        }

        public double SummerDryBulbTemperature
        {
            get
            {
                return summerDryBulbTemperature;
            }

            set
            {
                summerDryBulbTemperature = value;
            }
        }

        public override bool FromJsonObject(JsonObject jsonObject)
        {
            if (!base.FromJsonObject(jsonObject))
                return false;

            if (jsonObject.ContainsKey("WinterSensibleEfficiency"))
            {
                winterSensibleEfficiency = jsonObject["WinterSensibleEfficiency"]?.GetValue<double>() ?? double.NaN;
            }

            if (jsonObject.ContainsKey("WinterLatentEfficiency"))
            {
                winterLatentEfficiency = jsonObject["WinterLatentEfficiency"]?.GetValue<double>() ?? double.NaN;
            }

            if (jsonObject.ContainsKey("SummerSensibleEfficiency"))
            {
                summerSensibleEfficiency = jsonObject["SummerSensibleEfficiency"]?.GetValue<double>() ?? double.NaN;
            }

            if (jsonObject.ContainsKey("SummerLatentEfficiency"))
            {
                summerLatentEfficiency = jsonObject["SummerLatentEfficiency"]?.GetValue<double>() ?? double.NaN;
            }

            if (jsonObject.ContainsKey("WinterRelativeHumidity"))
            {
                winterRelativeHumidity = jsonObject["WinterRelativeHumidity"]?.GetValue<double>() ?? double.NaN;
            }

            if (jsonObject.ContainsKey("WinterDryBulbTemperature"))
            {
                winterDryBulbTemperature = jsonObject["WinterDryBulbTemperature"]?.GetValue<double>() ?? double.NaN;
            }

            if (jsonObject.ContainsKey("SummerRelativeHumidity"))
            {
                summerRelativeHumidity = jsonObject["SummerRelativeHumidity"]?.GetValue<double>() ?? double.NaN;
            }

            if (jsonObject.ContainsKey("SummerDryBulbTemperature"))
            {
                summerDryBulbTemperature = jsonObject["SummerDryBulbTemperature"]?.GetValue<double>() ?? double.NaN;
            }

            return true;
        }

        public override JsonObject ToJsonObject()
        {
            JsonObject jsonObject = base.ToJsonObject();
            if (jsonObject == null)
                return null;

            if (!double.IsNaN(winterSensibleEfficiency))
            {
                jsonObject["WinterSensibleEfficiency"] = winterSensibleEfficiency;
            }

            if (!double.IsNaN(winterLatentEfficiency))
            {
                jsonObject["WinterLatentEfficiency"] = winterLatentEfficiency;
            }

            if (!double.IsNaN(summerSensibleEfficiency))
            {
                jsonObject["SummerSensibleEfficiency"] = summerSensibleEfficiency;
            }

            if (!double.IsNaN(summerLatentEfficiency))
            {
                jsonObject["SummerLatentEfficiency"] = summerLatentEfficiency;
            }

            if (!double.IsNaN(winterRelativeHumidity))
            {
                jsonObject["WinterRelativeHumidity"] = winterRelativeHumidity;
            }

            if (!double.IsNaN(winterDryBulbTemperature))
            {
                jsonObject["WinterDryBulbTemperature"] = winterDryBulbTemperature;
            }

            if (!double.IsNaN(summerRelativeHumidity))
            {
                jsonObject["SummerRelativeHumidity"] = summerRelativeHumidity;
            }

            if (!double.IsNaN(summerDryBulbTemperature))
            {
                jsonObject["SummerDryBulbTemperature"] = summerDryBulbTemperature;
            }

            return jsonObject;
        }
    }
}
