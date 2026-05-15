// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core;
using SAM.Geometry.Spatial;
using System.Text.Json.Nodes;

namespace SAM.Geometry.Object.Spatial
{
    public class ShellObject : Shell, IShellObject, ITaggable
    {
        public SurfaceAppearance SurfaceAppearance { get; set; }

        public Shell Shell
        {
            get
            {
                return new Shell(this);
            }
        }

        public Tag Tag { get; set; }

        public ShellObject(Shell shell)
            : base(shell)
        {

        }
        public ShellObject(System.Text.Json.Nodes.JsonObject jsonObject)

            : base(jsonObject)

        {

        }

        public ShellObject(ShellObject shellObject)
            : base(shellObject)
        {
            if (shellObject?.SurfaceAppearance != null)
            {
                SurfaceAppearance = new SurfaceAppearance(shellObject?.SurfaceAppearance);
            }

            Tag = shellObject?.Tag;
        }

        public ShellObject(Shell shell, SurfaceAppearance surfaceAppearance)
            : base(shell)
        {
            if (surfaceAppearance != null)
            {
                SurfaceAppearance = new SurfaceAppearance(surfaceAppearance);
            }
        }

        public ShellObject(Shell shell, System.Drawing.Color surfaceColor, System.Drawing.Color curveColor, double curveThickness)
            : base(shell)
        {
            SurfaceAppearance = new SurfaceAppearance(surfaceColor, curveColor, curveThickness);
        }

        public override bool FromJsonObject(JsonObject jsonObject)
        {
            if (!base.FromJsonObject(jsonObject))
            {
                return false;
            }

            if (jsonObject["SurfaceAppearance"] is JsonObject jsonObject_SurfaceAppearance)
            {
                SurfaceAppearance = new SurfaceAppearance((JsonObject)jsonObject_SurfaceAppearance.DeepClone());
            }
            Tag = Core.Query.Tag(jsonObject);

            return true;
        }

        public override JsonObject ToJsonObject()
        {
            JsonObject jsonObject = base.ToJsonObject();
            if (jsonObject == null)
            {
                return null;
            }

            if (SurfaceAppearance?.ToJsonObject() is JsonObject surfaceJson)
            {
                jsonObject["SurfaceAppearance"] = surfaceJson.DeepClone();
            }

            Core.Modify.Add(jsonObject, Tag);

            return jsonObject;
        }
    }
}
