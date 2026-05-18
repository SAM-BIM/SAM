// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using SAM.Core;
using SAM.Tests.Helpers;
using Xunit;

namespace SAM.Tests
{
    public class SAMObjectTests
    {
        [Fact]
        public void RoundTrip_GuidAndName_Preserved()
        {
            Guid guid = Guid.Parse("12345678-1234-1234-1234-123456789abc");
            OpaqueMaterial material = new OpaqueMaterial(guid, "Test material", "Steel", "carbon steel", 50, 7800, 450);

            OpaqueMaterial result = RoundTrip.Once(material);

            Assert.Equal(guid, result.Guid);
            Assert.Equal("Test material", result.Name);
        }

        [Fact]
        public void RoundTrip_NullName_Preserved()
        {
            Guid guid = Guid.Parse("87654321-4321-4321-4321-cba987654321");
            OpaqueMaterial material = new OpaqueMaterial(guid, null!, "Concrete", "structural concrete", 1.4, 2300, 900);

            OpaqueMaterial result = RoundTrip.Once(material);

            Assert.Equal(guid, result.Guid);
            Assert.Null(result.Name);
        }

        [Fact]
        public void RoundTrip_Idempotent_TwoPasses()
        {
            OpaqueMaterial material = new OpaqueMaterial(Guid.NewGuid(), "Idempotent", "Brick", "fired clay brick", 0.7, 1700, 800);

            string? first = SAM.Core.Convert.ToString(material);
            Assert.False(string.IsNullOrWhiteSpace(first));

            OpaqueMaterial second = Create.IJSAMObject<OpaqueMaterial>(first!);
            string? secondJson = SAM.Core.Convert.ToString(second);

            OpaqueMaterial third = Create.IJSAMObject<OpaqueMaterial>(secondJson!);
            string? thirdJson = SAM.Core.Convert.ToString(third);

            RoundTrip.AssertEquivalent(secondJson!, thirdJson!);
        }
    }
}
