// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core;
using SAM.Tests.Helpers;
using System.Text.Json.Nodes;
using Xunit;

namespace SAM.Tests
{
    public class MessageTests
    {
        [Fact]
        public void RoundTrip_WarningMessage_PreservesTypeAndText()
        {
            Message message = new Message(MessageType.Warning, "Mind the threshold: 42%");

            Message result = RoundTrip.Once(message);
            JsonObject jsonObject = ToJsonObject(result);

            Assert.Equal("SAM.Core.Message,SAM.Core", jsonObject["_type"]?.GetValue<string>());
            Assert.Equal("Warning", jsonObject["MessageType"]?.GetValue<string>());
            Assert.Equal("Mind the threshold: 42%", jsonObject["Text"]?.GetValue<string>());
        }

        [Fact]
        public void RoundTrip_UndefinedMessage_OmitsMessageType()
        {
            Message message = new Message(MessageType.Undefined, "Plain note");

            Message result = RoundTrip.Once(message);
            JsonObject jsonObject = ToJsonObject(result);

            Assert.False(jsonObject.ContainsKey("MessageType"));
            Assert.Equal("Plain note", jsonObject["Text"]?.GetValue<string>());
        }

        [Fact]
        public void FromJson_MissingMessageType_DefaultsToUndefined()
        {
            const string json = @"{""_type"":""SAM.Core.Message,SAM.Core"",""Text"":""No type supplied""}";

            Message result = RoundTrip.FromJson<Message>(json);
            JsonObject jsonObject = ToJsonObject(result);

            Assert.False(jsonObject.ContainsKey("MessageType"));
            Assert.Equal("No type supplied", jsonObject["Text"]?.GetValue<string>());
        }

        private static JsonObject ToJsonObject(Message message)
        {
            JsonNode? jsonNode = JsonNode.Parse(SAM.Core.Convert.ToString(message));
            return Assert.IsType<JsonObject>(jsonNode);
        }
    }
}
