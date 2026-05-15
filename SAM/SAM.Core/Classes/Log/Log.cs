// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;

namespace SAM.Core
{
    public class Log : SAMObject, IEnumerable<LogRecord>, IJSAMObject
    {
        private List<LogRecord> logRecords = new List<LogRecord>();

        public Log(string name)
            : base(name)
        {
        }

        public Log()
            : base()
        {

        }

        public Log(Log log)
            : base(log)
        {
            logRecords = log?.logRecords?.ConvertAll(x => new LogRecord(x));
        }

        public Log(JObject jObject)
            : base(jObject?.Node as System.Text.Json.Nodes.JsonObject)
        {

        }


        public Log(System.Text.Json.Nodes.JsonObject jsonObject)

            : base(jsonObject)

        {

        }

        public LogRecord Add(string format, params object[] values)
        {
            LogRecord logRecord = new LogRecord(format, values);
            logRecords.Add(logRecord);
            return logRecord;
        }

        public LogRecord Add(LogRecord logRecord)
        {
            if (logRecord == null)
            {
                return null;
            }

            LogRecord result = logRecord.Clone();
            if (result != null)
            {
                logRecords.Add(result);
            }


            return result;
        }

        public LogRecord Add(string format, LogRecordType logRecordType, params object[] values)
        {
            LogRecord logRecord = new LogRecord(format, logRecordType, values);
            logRecords.Add(logRecord);
            return logRecord;
        }

        public List<LogRecord> AddRange(IEnumerable<LogRecord> logRecords)
        {
            if (logRecords == null)
                return null;

            List<LogRecord> result = new List<LogRecord>();
            foreach (LogRecord logRecord in logRecords)
            {
                if (logRecord == null)
                    continue;

                LogRecord logRecord_New = logRecord.Clone();
                if (logRecord_New == null)
                    continue;

                this.logRecords.Add(logRecord_New);
                result.Add(logRecord_New);
            }
            return result;
        }

        public override string ToString()
        {
            return string.Join(Environment.NewLine, logRecords.ConvertAll(x => x.ToString()));
        }

        public override bool FromJsonObject(JsonObject jsonObject)
        {
            if (!base.FromJsonObject(jsonObject))
                return false;

            logRecords = new List<LogRecord>();

            if (jsonObject["LogRecords"] is JsonArray logRecordsArray)
            {
                foreach (JsonNode node in logRecordsArray)
                {
                    if (node is JsonObject logRecordJson)
                    {
                        logRecords.Add(new LogRecord((JsonObject)logRecordJson.DeepClone()));
                    }
                }
            }

            return true;
        }

        public override JsonObject ToJsonObject()
        {
            JsonObject jsonObject = base.ToJsonObject();
            if (jsonObject == null)
                return null;

            JsonArray logRecordsArray = new JsonArray();
            foreach (LogRecord logRecord in logRecords)
            {
                if (logRecord?.ToJsonObject() is JsonObject logRecordJson)
                {
                    logRecordsArray.Add(logRecordJson.DeepClone());
                }
            }
            jsonObject["LogRecords"] = logRecordsArray;

            return jsonObject;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IEnumerator<LogRecord> GetEnumerator()
        {
            return logRecords.GetEnumerator();
        }

        public void Clear()
        {
            logRecords.Clear();
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

        public void Sort()
        {
            LogRecordType[] logRecordTypes = { LogRecordType.Error, LogRecordType.Warning, LogRecordType.Message };

            List<LogRecord> logRecords_Sorted = new List<LogRecord>();

            foreach (LogRecordType logRecordType in logRecordTypes)
            {
                List<LogRecord> logRecords_Temp =  logRecords.FindAll(x => x.LogRecordType == logRecordType);
                logRecords_Temp.Sort((x, y) => x.Text.CompareTo(y.Text));
                logRecords_Sorted.AddRange(logRecords_Temp);
            }

            logRecords = logRecords_Sorted;
        }
    }
}
