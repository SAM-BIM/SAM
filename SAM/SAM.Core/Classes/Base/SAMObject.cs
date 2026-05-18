// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace SAM.Core
{
    public class SAMObject : ParameterizedSAMObject, ISAMObject
    {
        protected string name;
        private Guid guid;

        public SAMObject(SAMObject sAMObject)
            : base(sAMObject)
        {
            if (sAMObject != null)
            {
                guid = sAMObject.Guid;
                name = sAMObject.Name;
            }
        }

        public SAMObject(string name, SAMObject sAMObject)
            : base(sAMObject)
        {
            guid = sAMObject == null ? Guid.Empty : sAMObject.Guid;
            this.name = name;
        }

        public SAMObject(string name, Guid guid, SAMObject sAMObject)
            : base(sAMObject)
        {
            this.guid = guid;
            this.name = name;
        }

        public SAMObject(Guid guid, SAMObject sAMObject)
            : base(sAMObject)
        {
            this.guid = guid;
            name = sAMObject?.Name;
        }

        public SAMObject(Guid guid, string name, IEnumerable<ParameterSet> parameterSets)
            : base(parameterSets)
        {
            this.guid = guid;
            this.name = name;
        }

        public SAMObject(Guid guid, string name)
            : base()
        {
            this.guid = guid;
            this.name = name;
        }

        public SAMObject()
            : base()
        {
            guid = Guid.NewGuid();
        }
        public SAMObject(JsonObject jsonObject)
            : base(jsonObject)
        {
        }

        public SAMObject(Guid guid)
            : base()
        {
            this.guid = guid;
        }

        public SAMObject(string name)
            : base()
        {
            this.name = name;
            guid = Guid.NewGuid();
        }

        public Guid Guid
        {
            get
            {
                return guid;
            }
        }

        public string Name
        {
            get
            {
                return name;
            }
        }

        public override bool FromJsonObject(JsonObject jsonObject)
        {
            if (jsonObject == null)
            {
                return false;
            }

            if (!base.FromJsonObject(jsonObject))
            {
                return false;
            }

            name = Query.Name(jsonObject);
            guid = Query.Guid(jsonObject);
            return true;
        }

        public override JsonObject ToJsonObject()
        {
            JsonObject jsonObject = base.ToJsonObject();
            if (jsonObject == null)
            {
                return null;
            }

            if (name != null)
                jsonObject["Name"] = name;

            jsonObject["Guid"] = guid.ToString();

            return jsonObject;
        }
    }
}
