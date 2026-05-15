// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;

using SAM.Geometry.Spatial;
using System;

namespace SAM.Analytical
{
    public class AirPartition : BuildingElement<BuildingElementType>, IPartition
    {

        public AirPartition(AirPartition airPartition)
            : base(airPartition)
        {

        }
        public AirPartition(System.Text.Json.Nodes.JsonObject jsonObject)

            : base(jsonObject)

        {

        }

        public AirPartition(Face3D face3D)
            : base(null, face3D)
        {

        }

        public AirPartition(Guid guid, Face3D face3D)
            : base(guid, null as BuildingElementType, face3D)
        {

        }

        public AirPartition(Guid guid, AirPartition airPartition, Face3D face3D)
            : base(guid, airPartition, face3D)
        {

        }

    }
}
