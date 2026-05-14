// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using SAM.Core;
using SAM.Tests.Helpers;
using Xunit;

namespace SAM.Tests
{
    public class MaterialTests
    {
        private static readonly Guid Fixed = Guid.Parse("33333333-4444-5555-6666-777777777777");

        [Fact]
        public void RoundTrip_GasMaterial_PreservesBaseProperties()
        {
            // GasMaterial inherits Material.{Group, DisplayName, Description,
            // ThermalConductivity, SpecificHeatCapacity, Density} through the
            // BCL FromJsonObject/ToJsonObject chain.
            GasMaterial expected = new GasMaterial(
                Fixed,
                "Air",
                "Default Air",
                "Air at 20C",
                thermalConductivity: 0.0257,
                density: 1.204,
                specificHeatCapacity: 1006.0,
                dynamicViscosity: 1.81e-5);

            GasMaterial result = RoundTrip.Once(expected);

            Assert.Equal("Air", result.Name);
            Assert.Equal(Fixed, result.Guid);
            Assert.Equal("Default Air", result.DisplayName);
            Assert.Equal("Air at 20C", result.Description);
            Assert.Equal(0.0257, result.ThermalConductivity);
            Assert.Equal(1.204, result.Density);
            Assert.Equal(1006.0, result.SpecificHeatCapacity);
            Assert.Equal(1.81e-5, result.DynamicViscosity);
        }

        [Fact]
        public void RoundTrip_GasMaterial_OmitsUnsetProperties()
        {
            // Material.ToJsonObject is supposed to skip emission for null
            // strings and NaN numbers. Construct a bare GasMaterial so we can
            // verify the on-wire JSON stays minimal.
            GasMaterial material = new GasMaterial("Bare");

            string json = SAM.Core.Convert.ToString(material);

            Assert.DoesNotContain("\"Group\":", json);
            Assert.DoesNotContain("\"DisplayName\":", json);
            Assert.DoesNotContain("\"Description\":", json);
            Assert.DoesNotContain("\"ThermalConductivity\":", json);
            Assert.DoesNotContain("\"SpecificHeatCapacity\":", json);
            Assert.DoesNotContain("\"Density\":", json);
        }
    }
}
