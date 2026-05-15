// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;
using System;
using System.Text.Json.Nodes;

namespace SAM.Core
{
    public class Result : SAMObject, IResult
    {
        private string source;
        private string reference;
        private DateTime dateTime;

        public Result(string name, string source, string reference)
            : base(name)
        {
            this.source = source;
            this.reference = reference;
            dateTime = DateTime.Now;
        }

        public Result(Guid guid, string name, string source, string reference)
            : base(guid, name)
        {
            this.source = source;
            this.reference = reference;
            dateTime = DateTime.Now;
        }

        public Result(Result result)
            : base(result)
        {
            source = result?.source;
            reference = result?.reference;
            dateTime = result == null ? DateTime.MinValue : result.dateTime;
        }

        public Result(Guid guid, Result result)
            : base(guid, result)
        {
            source = result?.source;
            reference = result?.reference;
            dateTime = result == null ? DateTime.MinValue : result.dateTime;
        }
        public Result(System.Text.Json.Nodes.JsonObject jsonObject)

            : base(jsonObject)

        {

        }

        public string Reference
        {
            get
            {
                return reference;
            }
        }

        public string Source
        {
            get
            {
                return source;
            }
        }

        public DateTime DateTime
        {
            get
            {
                return dateTime;
            }
        }

        public override bool FromJsonObject(JsonObject jsonObject)
        {
            if (!base.FromJsonObject(jsonObject))
                return false;

            if (jsonObject.ContainsKey("Source"))
                source = jsonObject["Source"]?.GetValue<string>();

            if (jsonObject.ContainsKey("Reference"))
                reference = jsonObject["Reference"]?.GetValue<string>();

            if (jsonObject.ContainsKey("DateTime"))
                dateTime = jsonObject["DateTime"]?.GetValue<DateTime>() ?? DateTime.MinValue;
            else
                dateTime = DateTime.MinValue;

            return true;
        }

        public override JsonObject ToJsonObject()
        {
            JsonObject jsonObject = base.ToJsonObject();
            if (jsonObject == null)
                return null;

            if (source != null)
                jsonObject["Source"] = source;

            if (reference != null)
                jsonObject["Reference"] = reference;

            if (dateTime != DateTime.MinValue)
                jsonObject["DateTime"] = JToken.ToNode(dateTime);

            return jsonObject;
        }
    }
}
