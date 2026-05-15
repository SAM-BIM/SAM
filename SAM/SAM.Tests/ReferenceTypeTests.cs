// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;
using SAM.Core;
using SAM.Tests.Helpers;
using Xunit;

namespace SAM.Tests
{
    public class ReferenceTypeTests
    {
        private static readonly Guid Fixed = Guid.Parse("cccccccc-1111-2222-3333-444444444444");

        // Lightweight round-trip helper: ObjectReference/PathReference are
        // IJSAMObject but the existing RoundTrip.Once helper assumes IJSAMObject
        // serialization via Create.IJSAMObject<T>(string). Drive it manually so
        // this test exercises ObjectReference.ToJObject -> FromJObject directly.
        private static string ToJson(IJSAMObject jSAMObject) => SAM.Core.Convert.ToString(jSAMObject);

        private static T FromJson<T>(string json) where T : IJSAMObject
        {
            JsonObject jsonObject = JsonNode.Parse(json) as JsonObject;
            return Query.IJSAMObject<T>(jsonObject);
        }

        [Fact]
        public void RoundTrip_ObjectReference_TypeNameAndGuid()
        {
            ObjectReference expected = new ObjectReference(
                "SAM.Core.SAMObject,SAM.Core",
                new Reference(Fixed));

            string json = ToJson(expected);
            ObjectReference result = FromJson<ObjectReference>(json);
            string roundTripped = ToJson(result);

            RoundTrip.AssertEquivalent(json, roundTripped);
            Assert.Equal("SAM.Core.SAMObject,SAM.Core", result.TypeName);
            Assert.NotNull(result.Reference);
            Assert.Equal(Fixed.ToString("N"), result.Reference.Value.ToString());
        }

        [Fact]
        public void RoundTrip_PropertyReference_PreservesPropertyName()
        {
            PropertyReference expected = new PropertyReference(
                "SAM.Core.SAMObject,SAM.Core",
                new Reference(Fixed),
                "Name");

            string json = ToJson(expected);
            PropertyReference result = FromJson<PropertyReference>(json);
            string roundTripped = ToJson(result);

            RoundTrip.AssertEquivalent(json, roundTripped);
            Assert.Equal("SAM.Core.SAMObject,SAM.Core", result.TypeName);
            Assert.Equal("Name", result.PropertyName);
            Assert.Equal(Fixed.ToString("N"), result.Reference.Value.ToString());
        }

        [Fact]
        public void RoundTrip_PathReference_PreservesSegmentOrder()
        {
            ObjectReference first = new ObjectReference("Space", new Reference(Fixed));
            PropertyReference second = new PropertyReference("InternalCondition", "Name");

            PathReference expected = new PathReference(new List<ObjectReference> { first, second });

            string json = ToJson(expected);
            PathReference result = FromJson<PathReference>(json);
            string roundTripped = ToJson(result);

            RoundTrip.AssertEquivalent(json, roundTripped);

            List<ObjectReference> segments = new List<ObjectReference>();
            foreach (ObjectReference segment in result)
            {
                segments.Add(segment);
            }

            Assert.Equal(2, segments.Count);
            Assert.Equal("Space", segments[0].TypeName);
            Assert.Equal("InternalCondition", segments[1].TypeName);
            Assert.IsType<PropertyReference>(segments[1]);
            Assert.Equal("Name", ((PropertyReference)segments[1]).PropertyName);
        }
    }
}
