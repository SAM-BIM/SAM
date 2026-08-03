// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core;

namespace SAM.Analytical
{
    public static partial class Query
    {
        /// <summary>
        /// Returns Default TM59 SAM Analytical InternalCondition TextMap
        /// </summary>
        /// <returns name="TextMap">Default TM59 SAM Analytical InternalCondition TextMap</returns>
        /// <search>Default SAM Analytical InternalCondition, IC, InternalCondition, TextMap</search> 
        public static TextMap DefaultInternalConditionTextMap_TM59()
        {
            TextMap result = ActiveSetting.Setting?.GetValue<TextMap>(AnalyticalSettingParameter.InternalConditionTextMap_TM59);
            if (result != null)
                return result;

            // Falls back to reading the resource file directly - covers a persisted %APPDATA%\SAM\settings
            // Setting that predates the TM59 keys, in which case ActiveSetting.GetDefault() (the only code
            // that reads the JSON resource) never runs.
            string path = ActiveSetting.Setting?.DefaultPath(AnalyticalSettingParameter.DefaultInternaConditionTextMaplFileName_TM59);
            if (string.IsNullOrWhiteSpace(path) || !System.IO.File.Exists(path))
                return null;

            return Core.Create.IJSAMObject<TextMap>(System.IO.File.ReadAllText(path));
        }
    }
}
