// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;
using System.Text.Json.Nodes;

namespace SAM.Core
{
    public class Message : IJSAMObject
    {
        private MessageType messageType;
        private string text;

        public Message(MessageType messageType, string text)
        {
            this.messageType = messageType;
            this.text = text;
        }

        public Message(JObject jObject)
        {
            FromJObject(jObject);
        }

        public Message(Message message)
        {
            if (message != null)
            {
                messageType = message.messageType;
                text = message.text;
            }
        }

        public bool FromJObject(JObject jObject)
        {
            return FromJsonObject(jObject?.Node as JsonObject);
        }

        private bool FromJsonObject(JsonObject jsonObject)
        {
            if (jsonObject == null)
            {
                return false;
            }

            if (jsonObject.ContainsKey("Text"))
            {
                text = jsonObject["Text"]?.GetValue<string>();
            }


            messageType = MessageType.Undefined;
            if (jsonObject.ContainsKey("MessageType"))
            {
                messageType = Query.Enum<MessageType>(jsonObject["MessageType"]?.GetValue<string>());
            }

            return true;
        }

        public JObject ToJObject()
        {
            JsonObject jsonObject = ToJsonObject();
            return jsonObject == null ? null : new JObject(jsonObject);
        }

        private JsonObject ToJsonObject()
        {
            JsonObject jsonObject = new JsonObject
            {
                ["_type"] = Query.FullTypeName(this)
            };

            if (messageType != MessageType.Undefined)
                jsonObject["MessageType"] = messageType.ToString();

            if (text != null)
                jsonObject["Text"] = text;

            return jsonObject;
        }
    }
}
