// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using Grasshopper;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using System;
using System.Collections.Generic;

namespace SAM.Core.Grasshopper
{
    public static partial class Modify
    {
        public static bool NavigateTo(GH_Document gH_Document, Guid instanceGuid)
        {
            return NavigateTo(gH_Document, new Guid[] { instanceGuid });
        }

        public static bool NavigateTo(GH_Document gH_Document, IEnumerable<Guid> instanceGuids)
        {
            if (gH_Document == null || instanceGuids == null)
            {
                return false;
            }

            GH_Canvas gH_Canvas = Instances.ActiveCanvas;
            if (gH_Canvas == null || gH_Canvas.Document != gH_Document)
            {
                return false;
            }

            List<IGH_Attributes> attributesList = new List<IGH_Attributes>();
            foreach (Guid instanceGuid in instanceGuids)
            {
                IGH_DocumentObject gH_DocumentObject = gH_Document.FindObject(instanceGuid, true);

                IGH_Attributes attributes = gH_DocumentObject?.Attributes?.GetTopLevel;
                if (attributes == null)
                {
                    continue;
                }

                if (!attributesList.Contains(attributes))
                {
                    attributesList.Add(attributes);
                }
            }

            if (attributesList.Count == 0)
            {
                return false;
            }

            gH_Document.DeselectAll();

            foreach (IGH_Attributes attributes in attributesList)
            {
                attributes.Selected = true;
            }

            if (attributesList.Count == 1)
            {
                gH_Canvas.Viewport.Focus(attributesList[0]);
            }
            else
            {
                gH_Canvas.Viewport.Focus(attributesList);
            }

            gH_Canvas.Refresh();

            return true;
        }
    }
}
