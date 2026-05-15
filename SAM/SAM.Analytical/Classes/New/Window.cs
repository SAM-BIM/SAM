// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;

using SAM.Geometry.Spatial;

namespace SAM.Analytical
{
    public class Window : Opening<WindowType>, IOpening
    {
        public Window(Window window)
            : base(window)
        {

        }

        public Window(JObject jObject)
            : base(jObject?.Node as System.Text.Json.Nodes.JsonObject)
        {

        }


        public Window(System.Text.Json.Nodes.JsonObject jsonObject)

            : base(jsonObject)

        {

        }

        public Window(WindowType windowType, Face3D face3D)
            : base(windowType, face3D)
        {

        }

        public Window(System.Guid guid, WindowType windowType, Face3D face3D)
            : base(guid, windowType, face3D)
        {

        }

        public Window(System.Guid guid, Window window, Face3D face3D)
            : base(guid, window, face3D)
        {

        }

    }
}
