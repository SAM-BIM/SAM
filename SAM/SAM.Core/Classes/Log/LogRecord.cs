// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;
using System;
using System.Text.Json.Nodes;

namespace SAM.Core
{
    public class LogRecord : IJSAMObject
    {
        private DateTime dateTime;
        private string text;
        private LogRecordType logRecordType;

        public LogRecord(LogRecord logRecord)
        {
            dateTime = logRecord.dateTime;
            logRecordType = logRecord.logRecordType;
            text = logRecord.text;
        }

        public LogRecord(string text)
        {
            dateTime = DateTime.UtcNow;
            logRecordType = LogRecordType.Undefined;
            this.text = text;
        }

        public LogRecord(string format, params object[] values)
        {
            dateTime = DateTime.UtcNow;
            logRecordType = LogRecordType.Undefined;

            if (format != null)
                text = string.Format(format, values);
            else
                text = string.Empty;
        }

        public LogRecord(string format, LogRecordType logRecordType, params object[] values)
        {
            dateTime = DateTime.UtcNow;
            this.logRecordType = logRecordType;

            if (format != null)
                text = string.Format(format, values);
            else
                text = string.Empty;
        }

        public LogRecord(JObject jObject)
        {
            FromJObject(jObject);
        }

        public LogRecord(DateTime dateTime, string text)
        {
            this.dateTime = dateTime;
            logRecordType = LogRecordType.Undefined;

            if (text == null)
                this.text = string.Empty;
            else
                this.text = text;
        }

        public DateTime DateTime
        {
            get
            {
                return dateTime;
            }
        }

        public string Text
        {
            get
            {
                return text;
            }
        }

        public LogRecordType LogRecordType
        {
            get
            {
                return logRecordType;
            }
        }

        public override string ToString()
        {
            string text_Temp = text;
            if (string.IsNullOrWhiteSpace(text_Temp))
                text_Temp = string.Empty;

            if (logRecordType == LogRecordType.Undefined)
                return string.Format("[{0}]\t{1}", dateTime.ToString("yyyy-MM-dd HH:mm:ss.f"), text_Temp);
            else
                return string.Format("[{0}\t{1}]\t{2}", dateTime.ToString("yyyy-MM-dd HH:mm:ss.f"), logRecordType.ToString(), text_Temp);
        }

        public virtual bool FromJObject(JObject jObject)
        {
            return FromJsonObject(jObject?.Node as JsonObject);
        }

        protected virtual bool FromJsonObject(JsonObject jsonObject)
        {
            if (jsonObject == null)
                return false;

            dateTime = jsonObject["DateTime"]?.GetValue<DateTime>() ?? default;
            text = jsonObject["Text"]?.GetValue<string>();

            logRecordType = LogRecordType.Undefined;
            if (jsonObject.ContainsKey("LogRecordType"))
                Enum.TryParse(jsonObject["LogRecordType"]?.GetValue<string>(), out logRecordType);

            return true;
        }

        public virtual JObject ToJObject()
        {
            JsonObject jsonObject = ToJsonObject();
            return jsonObject == null ? null : new JObject(jsonObject);
        }

        protected virtual JsonObject ToJsonObject()
        {
            JsonObject jsonObject = new JsonObject
            {
                ["_type"] = Query.FullTypeName(this),
                ["DateTime"] = dateTime
            };

            if (logRecordType != LogRecordType.Undefined)
                jsonObject["LogRecordType"] = logRecordType.ToString();

            if (text != null)
                jsonObject["Text"] = text;

            return jsonObject;
        }

        public bool Write(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return false;

            try
            {
                System.IO.File.AppendAllText(path, ToString() + Environment.NewLine);
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }
    }
}
