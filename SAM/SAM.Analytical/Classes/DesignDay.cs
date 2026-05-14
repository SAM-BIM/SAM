// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;
using System.Text.Json.Nodes;

namespace SAM.Analytical
{
    public class DesignDay : Weather.WeatherDay, IAnalyticalObject
    {
        private string name;
        private short year;
        private byte month;
        private byte day;
        private string description;

        private LoadType loadType = LoadType.Undefined;

        public DesignDay(DesignDay designDay)
            : base(designDay)
        {
            if (designDay != null)
            {
                name = designDay.name;
                description = designDay.description;
                year = designDay.year;
                month = designDay.month;
                day = designDay.day;
                loadType = designDay.loadType;
            }
        }

        public DesignDay(DesignDay designDay, LoadType loadType)
            : base(designDay)
        {
            if (designDay != null)
            {
                name = designDay.name;
                description = designDay.description;
                year = designDay.year;
                month = designDay.month;
                day = designDay.day;

            }

            this.loadType = loadType;
        }

        public DesignDay(JObject jObject)
            : base(jObject)
        {
        }

        public DesignDay(string name, short year, byte month, byte day)
            : base()
        {
            this.name = name;
            this.year = year;
            this.month = month;
            this.day = day;
        }

        public DesignDay(string name, string description, short year, byte month, byte day)
            : base()
        {
            this.name = name;
            this.description = description;
            this.year = year;
            this.month = month;
            this.day = day;
        }

        public string Name
        {
            get
            {
                return name;
            }
        }

        public string Description
        {
            get
            {
                return description;
            }
        }

        public short Year
        {
            get
            {
                return year;
            }
        }

        public byte Month
        {
            get
            {
                return month;
            }
        }

        public byte Day
        {
            get
            {
                return day;
            }
        }

        public System.DateTime GetDateTime()
        {
            if (year < 1 && year > 9999)
            {
                return System.DateTime.MinValue;
            }

            if (month < 1 && month > 12)
            {
                return System.DateTime.MinValue;
            }

            if (day < 1)
            {
                return System.DateTime.MinValue;
            }

            return new System.DateTime(year, month, day);
        }

        public Weather.WeatherYear GetWeatherYear()
        {
            System.DateTime dateTime = GetDateTime();
            if (dateTime == System.DateTime.MinValue)
            {
                return null;
            }

            Weather.WeatherYear result = new Weather.WeatherYear(dateTime.Year);
            result[dateTime.DayOfYear - 1] = new Weather.WeatherDay(this);

            return result;
        }

        public override bool FromJsonObject(JsonObject jsonObject)
        {
            if (!base.FromJsonObject(jsonObject))
                return false;

            if (jsonObject == null)
                return false;

            if (jsonObject.ContainsKey("Name"))
            {
                name = jsonObject["Name"]?.GetValue<string>();
            }

            if (jsonObject.ContainsKey("Description"))
            {
                description = jsonObject["Description"]?.GetValue<string>();
            }

            if (jsonObject.ContainsKey("Year"))
            {
                year = System.Convert.ToInt16(jsonObject["Year"]?.GetValue<int>() ?? 0);
            }

            if (jsonObject.ContainsKey("Month"))
            {
                month = System.Convert.ToByte(jsonObject["Month"]?.GetValue<int>() ?? 0);
            }

            if (jsonObject.ContainsKey("Day"))
            {
                day = System.Convert.ToByte(jsonObject["Day"]?.GetValue<int>() ?? 0);
            }

            if (jsonObject.ContainsKey("LoadType"))
            {
                loadType = Core.Query.Enum<LoadType>(jsonObject["LoadType"]?.GetValue<string>());
            }

            return true;
        }

        public override JsonObject ToJsonObject()
        {
            JsonObject jsonObject = base.ToJsonObject();
            if (jsonObject == null)
                return null;

            if (name != null)
            {
                jsonObject["Name"] = name;
            }

            if (description != null)
            {
                jsonObject["Description"] = description;
            }

            jsonObject["Year"] = System.Convert.ToInt32(year);
            jsonObject["Month"] = System.Convert.ToInt32(month);
            jsonObject["Day"] = System.Convert.ToInt32(day);

            if (loadType != LoadType.Undefined)
            {
                jsonObject["LoadType"] = loadType.ToString();
            }

            return jsonObject;
        }
    }
}
