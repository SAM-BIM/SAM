// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;
using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace SAM.Analytical.Classes
{
    public class PanelShadeCase : Case
    {
        private List<Panel> panels = [];

        public PanelShadeCase()
            : base()
        {

        }
        public PanelShadeCase(System.Text.Json.Nodes.JsonObject jsonObject)

            : base(jsonObject)

        {

        }

        public PanelShadeCase(PanelShadeCase panelShadeCase)
            : base(panelShadeCase)
        {
            if (panelShadeCase != null)
            {
                panels = panelShadeCase.panels?.ConvertAll(x => Core.Query.Clone(x));
            }
        }

        public List<Panel> Panels
        {
            get
            {
                return panels?.ConvertAll(x => Core.Query.Clone(x));
            }

            set
            {
                panels = value?.ConvertAll(x => Core.Query.Clone(x));
                OnPropertyChanged(nameof(Panels));
            }
        }

        public override bool FromJsonObject(JsonObject jsonObject)
        {
            bool result = base.FromJsonObject(jsonObject);
            if (!result)
            {
                return false;
            }

            if (jsonObject["Panels"] is JsonArray panelsArray)
            {
                panels = Core.Convert.ToList<Panel>(new JArray(panelsArray));
            }

            return true;
        }

        public override JsonObject ToJsonObject()
        {
            JsonObject result = base.ToJsonObject();
            if (result is null)
            {
                return result;
            }

            if (panels is not null)
            {
                JsonArray panelsArray = new JsonArray();

                foreach (Panel panel in panels)
                {
                    if (panel?.ToJsonObject() is JsonObject panelJson)
                    {
                        panelsArray.Add(panelJson.DeepClone());
                    }
                    else
                    {
                        panelsArray.Add(null);
                    }
                }

                result["Panels"] = panelsArray;
            }

            return result;
        }
    }
}
