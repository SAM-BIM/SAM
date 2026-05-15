// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;

using SAM.Geometry.Spatial;

namespace SAM.Analytical
{
    public class Floor : HostPartition<FloorType>
    {
        public Floor(Floor floor)
            : base(floor)
        {

        }

        public Floor(JObject jObject)
            : base(jObject?.Node as System.Text.Json.Nodes.JsonObject)
        {

        }


        public Floor(System.Text.Json.Nodes.JsonObject jsonObject)

            : base(jsonObject)

        {

        }

        public Floor(FloorType floorType, Face3D face3D)
            : base(floorType, face3D)
        {

        }

        public Floor(System.Guid guid, FloorType floorType, Face3D face3D)
            : base(guid, floorType, face3D)
        {

        }

        public Floor(System.Guid guid, Floor floor, Face3D face3D, double tolerance = Core.Tolerance.Distance)
            : base(guid, floor, face3D, tolerance)
        {

        }

    }
}
