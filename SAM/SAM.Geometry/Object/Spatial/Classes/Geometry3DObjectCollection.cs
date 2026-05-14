// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;
using SAM.Core;
using System.Text.Json.Nodes;

namespace SAM.Geometry.Object.Spatial
{
    public class Geometry3DObjectCollection : SAMGeometry3DObjectCollection, ITaggable
    {
        public Tag Tag { get; set; }

        public Geometry3DObjectCollection()
            : base()
        {

        }

        public Geometry3DObjectCollection(JObject jObject)
            : base(jObject)
        {
            FromJObject(jObject);
        }

        public Geometry3DObjectCollection(Geometry3DObjectCollection geometryObjectCollection)
            : base(geometryObjectCollection)
        {
            Tag = geometryObjectCollection?.Tag;
        }

        protected override bool FromJsonObject(JsonObject jsonObject)
        {
            if (!base.FromJsonObject(jsonObject))
            {
                return false;
            }

            Tag = Core.Query.Tag(new JObject(jsonObject));

            return true;
        }

        protected override JsonObject ToJsonObject()
        {
            JsonObject jsonObject = base.ToJsonObject();
            if (jsonObject == null)
            {
                return null;
            }

            Core.Modify.Add(new JObject(jsonObject), Tag);

            return jsonObject;
        }
    }
}
