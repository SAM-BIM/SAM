// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using SAM.Core;
using SAM.Tests.Helpers;
using Xunit;

namespace SAM.Tests
{
    public class ReferenceTests
    {
        [Fact]
        public void RoundTrip_StringReference_PreservesValue()
        {
            Reference reference = "Room-101";

            Reference result = RoundTrip.Once(reference);

            Assert.True(result.IsValid());
            Assert.Equal("Room-101", result.ToString());
            Assert.Equal(reference, result);
        }

        [Fact]
        public void RoundTrip_GuidReference_UsesCompactGuid()
        {
            Guid guid = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
            Reference reference = guid;

            Reference result = RoundTrip.Once(reference);

            Assert.True(result.IsValid());
            Assert.Equal("aaaaaaaabbbbccccddddeeeeeeeeeeee", result.ToString());
            Assert.Equal(reference, result);
        }

        [Fact]
        public void FromJson_MissingValue_IsInvalid()
        {
            const string json = @"{""_type"":""SAM.Core.Reference,SAM.Core""}";

            Reference result = RoundTrip.FromJson<Reference>(json);

            Assert.False(result.IsValid());
            Assert.Null(result.ToString());
        }
    }
}
