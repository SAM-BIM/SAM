// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;
using SAM.Core;
using System.ComponentModel;
using System.Text.Json.Nodes;

namespace SAM.Analytical.Classes
{
    public abstract class Case : IJSAMObject, IAnalyticalObject, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public Case()
        {

        }

        public Case(Case @case)
        {

        }

        public Case(JObject jObject)
        {
            FromJsonObject(jObject?.Node as System.Text.Json.Nodes.JsonObject);
        }


        public Case(System.Text.Json.Nodes.JsonObject jsonObject)

        {

            FromJsonObject(jsonObject);

        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        public virtual bool FromJsonObject(JsonObject jsonObject)
        {
            if (jsonObject == null)
            {
                return false;
            }

            return true;
        }
        public virtual JsonObject ToJsonObject()
        {
            JsonObject jsonObject = new JsonObject
            {
                ["_type"] = Core.Query.FullTypeName(this)
            };

            return jsonObject;
        }
    }
}
