// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using SAM.Core;
using SAM.Core.Json;
using Xunit;

namespace SAM.Tests
{
    public class SAMObjectWrapperTests
    {
        private static readonly Guid Fixed = Guid.Parse("eeeeeeee-1111-2222-3333-444444444444");

        // JSAMObjectWrapper now stores the underlying JsonObject directly.
        // Verify identity (Guid, Name) is read through the BCL-side accessors.
        [Fact]
        public void Wrapper_ReadsIdentityThroughJsonObject()
        {
            string json = $"{{\"_type\":\"SAM.Core.SAMObject,SAM.Core\",\"Name\":\"Foo\",\"Guid\":\"{Fixed}\"}}";

            JSAMObjectWrapper wrapper = new JSAMObjectWrapper(JObject.Parse(json));

            Assert.Equal("Foo", wrapper.Name);
            Assert.Equal(Fixed, wrapper.Guid);
            Assert.Equal("SAM.Core.SAMObject", wrapper.GetTypeName());
            Assert.Equal("SAM.Core", wrapper.GetAssemblyName());
        }

        [Fact]
        public void Wrapper_ToIJSAMObject_RehydratesConcreteType()
        {
            string json = $"{{\"_type\":\"SAM.Core.SAMObject,SAM.Core\",\"Name\":\"Bar\",\"Guid\":\"{Fixed}\"}}";

            JSAMObjectWrapper wrapper = new JSAMObjectWrapper(JObject.Parse(json));
            IJSAMObject inner = wrapper.ToIJSAMObject();

            SAMObject sAMObject = Assert.IsType<SAMObject>(inner);
            Assert.Equal("Bar", sAMObject.Name);
            Assert.Equal(Fixed, sAMObject.Guid);
        }

        [Fact]
        public void Wrapper_ToJObject_PreservesContent()
        {
            // The wrapper reconstructs a JObject from the stored JsonObject on
            // every ToJObject call. Verify the round-trip JSON is equivalent
            // even though the JObject identity is new.
            string json = $"{{\"_type\":\"SAM.Core.SAMObject,SAM.Core\",\"Name\":\"Baz\",\"Guid\":\"{Fixed}\"}}";

            JSAMObjectWrapper wrapper = new JSAMObjectWrapper(JObject.Parse(json));
            JObject roundTripped = wrapper.ToJObject();

            Assert.NotNull(roundTripped);
            Assert.Equal("Baz", Query.Name(roundTripped));
            Assert.Equal(Fixed, Query.Guid(roundTripped));
        }
    }
}
