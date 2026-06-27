// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace SAM.Core
{
    public class Relation : IJSAMObject
    {
        private string id = null;
        private HashSet<Reference> references_1 = null;
        private HashSet<Reference> references_2 = null;

        public Relation(string id, IEnumerable<Reference> references_1, IEnumerable<Reference> references_2)
        {
            this.id = id;
            this.references_1 = references_1 == null ? null : new HashSet<Reference>(references_1);
            this.references_2 = references_2 == null ? null : new HashSet<Reference>(references_2);
        }

        public Relation(string id, Reference reference, IEnumerable<Reference> references)
        {
            this.id = id;
            references_1 = new HashSet<Reference>();
            references_1.Add(reference);
            references_2 = references == null ? null : new HashSet<Reference>(references);
        }

        public Relation(string id, IEnumerable<Reference> references, Reference reference)
        {
            this.id = id;
            references_2 = new HashSet<Reference>();
            references_2.Add(reference);
            references_1 = references == null ? null : new HashSet<Reference>(references);
        }

        public Relation(string id, Reference reference_1, Reference reference_2)
        {
            this.id = id;

            references_1 = new HashSet<Reference>();
            references_1.Add(reference_1);

            references_2 = new HashSet<Reference>();
            references_2.Add(reference_2);
        }
        public Relation(JsonObject jsonObject)
        {
            FromJsonObject(jsonObject);
        }

        public Relation(Relation relation)
        {
            if (relation != null)
            {
                id = relation.Id;
                references_1 = relation.references_1 == null ? null : new HashSet<Reference>(relation.references_1);
                references_2 = relation.references_2 == null ? null : new HashSet<Reference>(relation.references_2);
            }
        }

        public string Id
        {
            get
            {
                return id;
            }
        }

        public HashSet<Reference> References
        {
            get
            {
                if (references_1 == null && references_2 == null)
                {
                    return null;
                }

                HashSet<Reference> result = new HashSet<Reference>();
                if (references_1 != null)
                {
                    foreach (Reference reference in references_1)
                    {
                        result.Add(reference);
                    }
                }

                if (references_2 != null)
                {
                    foreach (Reference reference in references_2)
                    {
                        result.Add(reference);
                    }
                }

                return result;
            }
        }

        public HashSet<Reference> References_1
        {
            get
            {
                return references_1 == null ? null : new HashSet<Reference>(references_1);
            }
        }

        public HashSet<Reference> References_2
        {
            get
            {
                return references_2 == null ? null : new HashSet<Reference>(references_2);
            }
        }

        public bool Contains(Reference reference)
        {
            return Contains_1(reference) || Contains_2(reference);
        }

        public bool Contains_1(Reference reference)
        {
            return references_1 == null ? false : references_1.Contains(reference);
        }

        public bool Contains_2(Reference reference)
        {
            return references_2 == null ? false : references_2.Contains(reference);
        }

        public bool FromJsonObject(JsonObject jsonObject)
        {
            if (jsonObject == null)
            {
                return false;
            }

            if (jsonObject.ContainsKey("Id"))
            {
                id = jsonObject["Id"]?.GetValue<string>();
            }

            if (jsonObject["References_1"] is JsonArray references1Array)
            {
                references_1 = new HashSet<Reference>();
                foreach (JsonNode node in references1Array)
                {
                    string value = node?.GetValue<string>();
                    if (value != null)
                    {
                        references_1.Add(value);
                    }
                }
            }

            if (jsonObject["References_2"] is JsonArray references2Array)
            {
                references_2 = new HashSet<Reference>();
                foreach (JsonNode node in references2Array)
                {
                    string value = node?.GetValue<string>();
                    if (value != null)
                    {
                        references_2.Add(value);
                    }
                }
            }

            return true;
        }

        public JsonObject ToJsonObject()
        {
            JsonObject result = new JsonObject
            {
                ["_type"] = Query.FullTypeName(this)
            };

            if (id != null)
            {
                result["Id"] = id;
            }

            if (references_1 != null)
            {
                JsonArray references1Array = new JsonArray();
                foreach (Reference reference in references_1)
                {
                    references1Array.Add(reference.ToString());
                }
                result["References_1"] = references1Array;
            }

            if (references_2 != null)
            {
                JsonArray references2Array = new JsonArray();
                foreach (Reference reference in references_2)
                {
                    references2Array.Add(reference.ToString());
                }
                result["References_2"] = references2Array;
            }

            return result;
        }

        public RelationType RelationType
        {
            get
            {
                return Query.RelationType(references_1 == null ? 0 : references_1.Count, references_2 == null ? 0 : references_2.Count);
            }
        }
    }
}
