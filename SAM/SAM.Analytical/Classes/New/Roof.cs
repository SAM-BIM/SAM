// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors


using SAM.Geometry.Spatial;

namespace SAM.Analytical
{
    public class Roof : HostPartition<RoofType>
    {
        public Roof(Roof roof)
            : base(roof)
        {

        }
        public Roof(System.Text.Json.Nodes.JsonObject jsonObject)

            : base(jsonObject)

        {

        }

        public Roof(RoofType roofType, Face3D face3D)
            : base(roofType, face3D)
        {

        }

        public Roof(System.Guid guid, RoofType roofType, Face3D face3D)
            : base(guid, roofType, face3D)
        {

        }

        public Roof(System.Guid guid, Roof roof, Face3D face3D, double tolerance = Core.Tolerance.Distance)
            : base(guid, roof, face3D, tolerance)
        {

        }

    }
}
