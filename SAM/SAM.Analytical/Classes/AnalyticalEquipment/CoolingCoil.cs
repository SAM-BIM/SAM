// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;

namespace SAM.Analytical
{
    /// <summary>
    /// Represents an cooling coil object in the analytical domain
    /// </summary>
    public class CoolingCoil : Coil
    {
        public CoolingCoil(double fluidSupplyTemperature, double fluidReturnTemperature, double contactFactor, double offTemperature)
            : base("Cooling Coil", fluidSupplyTemperature, fluidReturnTemperature, contactFactor, offTemperature)
        {

        }

        public CoolingCoil(string name, double fluidSupplyTemperature, double fluidReturnTemperature, double contactFactor, double offTemperature)
            : base(name, fluidSupplyTemperature, fluidReturnTemperature, contactFactor, offTemperature)
        {

        }
        public CoolingCoil(System.Text.Json.Nodes.JsonObject jsonObject)

            : base(jsonObject)

        {

        }

        public CoolingCoil(CoolingCoil coolingCoil)
            : base(coolingCoil)
        {

        }

        public CoolingCoil(Guid guid, string name, double fluidSupplyTemperature, double fluidReturnTemperature, double contactFactor, double offTemperature)
            : base(guid, name, fluidSupplyTemperature, fluidReturnTemperature, contactFactor, offTemperature)
        {

        }

    }
}
