// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core;

namespace SAM.Analytical
{
    public static partial class Query
    {
        /// <summary>
        /// Returns Default TM59 SAM Analytical InternalConditionLibrary
        /// </summary>
        /// <returns name="constructionLibrary"> Default TM59 SAM Analytical InternalConditionLibrary</returns>
        /// <search>Default SAM Analytical InternalCondition Library</search> 
        public static InternalConditionLibrary DefaultInternalConditionLibrary_TM59()
        {
            InternalConditionLibrary result = ActiveSetting.Setting?.GetValue<InternalConditionLibrary>(AnalyticalSettingParameter.DefaultInternalConditionLibrary_TM59);
            if (result != null)
                return result;

            // Falls back to reading the resource file directly - covers a persisted %APPDATA%\SAM\settings
            // Setting that predates the TM59 keys, in which case ActiveSetting.GetDefault() (the only code
            // that reads the JSON resource) never runs.
            string path = ActiveSetting.Setting?.DefaultPath(AnalyticalSettingParameter.DefaultInternalConditionLibraryFileName_TM59);
            if (string.IsNullOrWhiteSpace(path) || !System.IO.File.Exists(path))
                return null;

            return Core.Create.IJSAMObject<InternalConditionLibrary>(System.IO.File.ReadAllText(path));
        }
    }
}
