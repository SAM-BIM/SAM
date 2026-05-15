// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;

using SAM.Geometry.Spatial;

namespace SAM.Analytical
{
    public class Wall : HostPartition<WallType>
    {
        public Wall(Wall wall)
            : base(wall)
        {

        }
        public Wall(System.Text.Json.Nodes.JsonObject jsonObject)

            : base(jsonObject)

        {

        }

        public Wall(WallType wallType, Face3D face3D)
            : base(wallType, face3D)
        {

        }

        public Wall(System.Guid guid, WallType wallType, Face3D face3D)
            : base(guid, wallType, face3D)
        {

        }

        public Wall(System.Guid guid, Wall wall, Face3D face3D, double tolerance = Core.Tolerance.Distance)
            : base(guid, wall, face3D, tolerance)
        {

        }

    }
}
