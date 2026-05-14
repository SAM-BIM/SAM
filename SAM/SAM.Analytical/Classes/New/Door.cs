// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;

using SAM.Geometry.Spatial;

namespace SAM.Analytical
{
    public class Door : Opening<DoorType>, IOpening
    {
        public Door(Door door)
            : base(door)
        {

        }

        public Door(JObject jObject)
            : base(jObject)
        {

        }

        public Door(DoorType doorType, Face3D face3D)
            : base(doorType, face3D)
        {

        }

        public Door(System.Guid guid, DoorType doorType, Face3D face3D)
            : base(guid, doorType, face3D)
        {

        }

        public Door(System.Guid guid, Door door, Face3D face3D)
            : base(guid, door, face3D)
        {

        }


    }
}
