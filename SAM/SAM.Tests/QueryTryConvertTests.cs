// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Text.Json.Nodes;
using SAM.Core;
using Xunit;

namespace SAM.Tests
{
    public class QueryTryConvertTests
    {
        [Fact]
        public void TryConvert_JsonNodeValue_ToPrimitives()
        {
            Assert.True(Query.TryConvert(JsonNode.Parse("\"Shade\"")!, out string? text));
            Assert.Equal("Shade", text);

            Assert.True(Query.TryConvert(JsonNode.Parse("12.5")!, out double number));
            Assert.Equal(12.5, number);

            Assert.True(Query.TryConvert(JsonNode.Parse("42")!, out int integer));
            Assert.Equal(42, integer);

            Assert.True(Query.TryConvert(JsonNode.Parse("true")!, out bool boolean));
            Assert.True(boolean);
        }

        [Fact]
        public void TryConvert_JsonNodeStringValue_UsesExistingParsing()
        {
            Assert.True(Query.TryConvert(JsonNode.Parse("\"12,5\"")!, out double number));
            Assert.Equal(12.5, number);

            Assert.True(Query.TryConvert(JsonNode.Parse("\"YES\"")!, out bool boolean));
            Assert.True(boolean);
        }

        [Fact]
        public void TryConvert_JsonNodeContainer_ToJsonString()
        {
            Assert.True(Query.TryConvert(JsonNode.Parse("[\"A\",\"B\"]")!, out string? arrayText));
            Assert.Equal("[\"A\",\"B\"]", arrayText);

            Assert.True(Query.TryConvert(JsonNode.Parse("{\"Name\":\"Shade\",\"Value\":1}")!, out string? objectText));
            Assert.Equal("{\"Name\":\"Shade\",\"Value\":1}", objectText);
        }
    }
}
