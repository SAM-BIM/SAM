// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;
using SAM.Core;
using SAM.Geometry.Spatial;
using System.Text.Json.Nodes;

namespace SAM.Analytical
{
    public class ExternalPanel : SAMInstance<Construction>, IPanel
    {
        private Face3D face3D;

        public ExternalPanel(Face3D face3D, Construction construction)
            : base(construction)
        {
            this.face3D = face3D == null ? null : new Face3D(face3D);
        }

        public ExternalPanel(Face3D face3D)
            : base(null as Construction)
        {
            this.face3D = face3D == null ? null : new Face3D(face3D);
        }

        public ExternalPanel(System.Guid guid, ExternalPanel externalPanel, Face3D face3D)
            : base(externalPanel)
        {
            this.face3D = face3D == null ? null : new Face3D(face3D);
        }

        public ExternalPanel(ExternalPanel externalPanel)
            : base(externalPanel)
        {
            face3D = externalPanel?.Face3D?.Clone<Face3D>();
        }

        public ExternalPanel(JObject jObject)
            : base(jObject)
        {
        }

        public Face3D Face3D
        {
            get
            {
                return face3D == null ? null : new Face3D(face3D);
            }
        }

        public Construction Construction
        {
            get
            {
                return Type;
            }
        }

        public void FlipNormal(bool flipX = true)
        {
            if (face3D == null)
            {
                return;
            }

            face3D.FlipNormal(flipX);
        }

        public override bool FromJsonObject(JsonObject jsonObject)
        {
            if (!base.FromJsonObject(jsonObject))
            {
                return false;
            }

            // NOTE: preserved verbatim - original code used jObject.Value<JObject>()
            // without a key, which returned the enclosing JObject itself (likely a bug).
            if (jsonObject.ContainsKey("Face3D"))
            {
                face3D = new Face3D(jsonObject);
            }

            return true;
        }

        public override JsonObject ToJsonObject()
        {
            JsonObject jsonObject = base.ToJsonObject();
            if (jsonObject == null)
            {
                return null;
            }

            if (face3D?.ToJsonObject() is JsonObject face3DJson)
            {
                jsonObject["Face3D"] = face3DJson.DeepClone();
            }

            return jsonObject;
        }
    }
}
