// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace SAM.Core
{
    /// <summary>
    /// A collection of Guids that inherits from SAMObject and implements the IEnumerable interface.
    /// </summary>
    public class GuidCollection : SAMObject, IEnumerable<Guid>
    {
        private List<Guid> guids = new List<Guid>();

        /// <summary>
        /// Constructor for a GuidCollection with a Guid and a name.
        /// </summary>
        public GuidCollection(Guid guid, string name)
            : base(guid, name)
        {

        }

        /// <summary>
        /// Constructor for a GuidCollection with only a name.
        /// </summary>
        public GuidCollection(string name)
            : base(name)
        {

        }

        /// <summary>
        /// Constructor for a GuidCollection with a name and a ParameterSet.
        /// </summary>
        public GuidCollection(string name, ParameterSet parameterSet)
            : base(Guid.NewGuid(), name, new ParameterSet[] { parameterSet })
        {

        }

        /// <summary>
        /// Constructor for a GuidCollection from a JObject.
        /// </summary>
        public GuidCollection(System.Text.Json.Nodes.JsonObject jsonObject)
        {
            FromJsonObject(jsonObject);
        }

        /// <summary>
        /// Constructor for a GuidCollection from another GuidCollection.
        /// </summary>
        public GuidCollection(GuidCollection guidCollection)
            : base(guidCollection)
        {

            if (guidCollection?.guids != null)
                guids = new List<Guid>(guidCollection.guids);
        }

        /// <summary>
        /// Default constructor for a GuidCollection.
        /// </summary>
        public GuidCollection()
            : base()
        {

        }

        /// <summary>
        /// Constructor for a GuidCollection from an IEnumerable of Guids.
        /// </summary>
        public GuidCollection(IEnumerable<Guid> guids)
            : base()
        {
            foreach (Guid guid in guids)
                this.guids.Add(guid);
        }

        /// <summary>
        /// Adds a Guid to the GuidCollection.
        /// </summary>
        public virtual void Add(Guid guid)
        {
            guids.Add(guid);
        }

        /// <summary>
        /// Removes a Guid from the GuidCollection.
        /// </summary>
        public bool Remove(Guid guid)
        {
            return guids.Remove(guid);
        }

        /// <summary>
        /// Removes a collection of Guids from the GuidCollection.
        /// </summary>
        public List<bool> Remove(IEnumerable<Guid> guids)
        {
            if (guids == null)
                return null;

            List<bool> result = new List<bool>();

            foreach (Guid guid in guids)
                result.Add(Remove(guid));

            return result;
        }

        /// <summary>
        /// Populates the GuidCollection's Collection array from a JsonObject.
        /// </summary>
        public override bool FromJsonObject(JsonObject jsonObject)
        {
            if (!base.FromJsonObject(jsonObject))
                return false;

            if (jsonObject["Collection"] is JsonArray collectionArray)
            {
                guids = new List<Guid>();
                foreach (JsonNode node in collectionArray)
                    guids.Add(Query.Guid(node));
            }

            return true;
        }

        /// <summary>
        /// Emits the GuidCollection's Collection array onto the JsonObject.
        /// </summary>
        public override JsonObject ToJsonObject()
        {
            JsonObject jsonObject = base.ToJsonObject();
            if (jsonObject == null)
                return null;

            JsonArray collectionArray = new JsonArray();
            foreach (Guid guid in this)
                collectionArray.Add(guid.ToString());

            jsonObject["Collection"] = collectionArray;

            return jsonObject;
        }

        /// <summary>
        /// Implements the IEnumerable interface for the GuidCollection.
        /// </summary>
        public IEnumerator<Guid> GetEnumerator()
        {
            return guids?.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return guids?.GetEnumerator();
        }
    }
}
