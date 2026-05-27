// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Collections.Generic;
using System.Text.Json.Nodes;
using SAM.Core;
using Xunit;
using DesignDay = SAM.Analytical.DesignDay;

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

        [Fact]
        public void TryConvert_JsonArrayOfObjects_ToList()
        {
            JsonArray jsonArray = new JsonArray(
                new DesignDay("DD1", 2024, 1, 1).ToJsonObject(),
                new DesignDay("DD2", 2024, 7, 15).ToJsonObject());

            Assert.True(Query.TryConvert((object)jsonArray, out List<DesignDay>? list));
            Assert.NotNull(list);
            Assert.Equal(2, list!.Count);
            Assert.Equal("DD1", list[0].Name);
            Assert.Equal("DD2", list[1].Name);
        }

        [Fact]
        public void TryConvert_JsonArrayOfObjects_ToArray()
        {
            JsonArray jsonArray = new JsonArray(
                new DesignDay("DD1", 2024, 1, 1).ToJsonObject(),
                new DesignDay("DD2", 2024, 7, 15).ToJsonObject());

            Assert.True(Query.TryConvert((object)jsonArray, out DesignDay[]? array));
            Assert.NotNull(array);
            Assert.Equal(2, array!.Length);
            Assert.Equal("DD2", array[1].Name);
        }

        [Fact]
        public void TryConvert_JsonArrayOfObjects_ToSAMCollection()
        {
            JsonArray jsonArray = new JsonArray(
                new DesignDay("DD1", 2024, 1, 1).ToJsonObject(),
                new DesignDay("DD2", 2024, 7, 15).ToJsonObject());

            Assert.True(Query.TryConvert((object)jsonArray, out SAMCollection<DesignDay>? collection));
            Assert.NotNull(collection);
            Assert.Equal(2, collection!.Count);
            Assert.Equal("DD1", collection[0].Name);
        }

        [Fact]
        public void TryConvert_JsonArrayOfPrimitives_StillFalseForList()
        {
            Assert.False(Query.TryConvert(JsonNode.Parse("[1,2,3]")!, out List<int>? numbers));
            Assert.Null(numbers);
        }
    }
}
