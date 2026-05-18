// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Text.Json.Nodes;

namespace SAM.Analytical
{
    public class TM52Result : TMResult
    {
        private int occupiedHours;
        private int maxExceedableHours;

        private int hoursExceedingComfortRange;

        private double peakDailyWeightedExceedance;
        private int hoursExceedingAbsoluteLimit;
        private bool pass;

        public TM52Result(
            string name,
            string source,
            string reference,
            TM52BuildingCategory tM52BuildingCategory,
            int occupiedHours,
            int maxExceedableHours,
            int hoursExceedingComfortRange,
            double peakDailyWeightedExceedance,
            int hoursExceedingAbsoluteLimit,
            bool pass)
            : base(name, source, reference, tM52BuildingCategory)
        {
            this.occupiedHours = occupiedHours;
            this.maxExceedableHours = maxExceedableHours;
            this.hoursExceedingComfortRange = hoursExceedingComfortRange;
            this.peakDailyWeightedExceedance = peakDailyWeightedExceedance;
            this.hoursExceedingAbsoluteLimit = hoursExceedingAbsoluteLimit;
            this.pass = pass;
        }

        public TM52Result(
            Guid guid,
            string name,
            string source,
            string reference,
            TM52BuildingCategory tM52BuildingCategory,
            int occupiedHours,
            int maxExceedableHours,
            int hoursExceedingComfortRange,
            double peakDailyWeightedExceedance,
            int hoursExceedingAbsoluteLimit,
            bool pass)
            : base(guid, name, source, reference, tM52BuildingCategory)
        {
            this.occupiedHours = occupiedHours;
            this.maxExceedableHours = maxExceedableHours;
            this.hoursExceedingComfortRange = hoursExceedingComfortRange;
            this.peakDailyWeightedExceedance = peakDailyWeightedExceedance;
            this.hoursExceedingAbsoluteLimit = hoursExceedingAbsoluteLimit;
            this.pass = pass;
        }

        public override int OccupiedHours
        {
            get
            {
                return occupiedHours;
            }
        }

        public override int MaxExceedableHours
        {
            get
            {
                return maxExceedableHours;
            }
        }

        public int HoursExceedingComfortRange
        {
            get
            {
                return hoursExceedingComfortRange;
            }
        }

        public double PeakDailyWeightedExceedance
        {
            get
            {
                return peakDailyWeightedExceedance;
            }
        }

        public int HoursExceedeingAbsoluteLimit
        {
            get
            {
                return hoursExceedingAbsoluteLimit;
            }
        }

        public override bool Pass
        {
            get
            {
                return pass;
            }
        }

        public override bool FromJsonObject(JsonObject jsonObject)
        {
            if (!base.FromJsonObject(jsonObject))
            {
                return false;
            }

            if (jsonObject.ContainsKey("OccupiedHours"))
            {
                occupiedHours = jsonObject["OccupiedHours"]?.GetValue<int>() ?? 0;
            }

            if (jsonObject.ContainsKey("MaxExceedableHours"))
            {
                maxExceedableHours = jsonObject["MaxExceedableHours"]?.GetValue<int>() ?? 0;
            }

            if (jsonObject.ContainsKey("HoursExceedingComfortRange"))
            {
                hoursExceedingComfortRange = jsonObject["HoursExceedingComfortRange"]?.GetValue<int>() ?? 0;
            }

            if (jsonObject.ContainsKey("PeakDailyWeightedExceedance"))
            {
                peakDailyWeightedExceedance = jsonObject["PeakDailyWeightedExceedance"]?.GetValue<double>() ?? double.NaN;
            }

            if (jsonObject.ContainsKey("HoursExceedeingAbsoluteLimit"))
            {
                hoursExceedingAbsoluteLimit = jsonObject["HoursExceedeingAbsoluteLimit"]?.GetValue<int>() ?? 0;
            }

            if (jsonObject.ContainsKey("Pass"))
            {
                pass = jsonObject["Pass"]?.GetValue<bool>() ?? false;
            }

            return true;
        }

        public override JsonObject ToJsonObject()
        {
            JsonObject result = base.ToJsonObject();
            if (result == null)
            {
                return null;
            }

            if (occupiedHours != int.MinValue)
            {
                result["OccupiedHours"] = occupiedHours;
            }

            if (maxExceedableHours != int.MinValue)
            {
                result["MaxExceedableHours"] = maxExceedableHours;
            }

            if (hoursExceedingComfortRange != int.MinValue)
            {
                result["HoursExceedingComfortRange"] = hoursExceedingComfortRange;
            }

            if (!double.IsNaN(peakDailyWeightedExceedance))
            {
                result["PeakDailyWeightedExceedance"] = peakDailyWeightedExceedance;
            }

            if (hoursExceedingAbsoluteLimit != int.MinValue)
            {
                result["HoursExceedingAbsoluteLimit"] = hoursExceedingAbsoluteLimit;
            }

            result["Pass"] = pass;

            return result;
        }
    }
}
