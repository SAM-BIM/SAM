// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core;

namespace SAM.Analytical
{
    public static partial class Query
    {
        /// <summary>
        /// Returns Default TM59 SAM Analytical ProfileLibrary
        /// </summary>
        /// <returns name="constructionLibrary"> Default TM59 SAM Analytical ProfileLibrary</returns>
        /// <search>Default SAM Analytical Profile Library</search> 
        public static ProfileLibrary DefaultProfileLibrary_TM59()
        {
            ProfileLibrary result = ActiveSetting.Setting?.GetValue<ProfileLibrary>(AnalyticalSettingParameter.DefaultProfileLibrary_TM59);
            if (result != null)
                return result;

            // Falls back to reading the resource file directly - covers a persisted %APPDATA%\SAM\settings
            // Setting that predates the TM59 keys, in which case ActiveSetting.GetDefault() (the only code
            // that reads the JSON resource) never runs.
            string path = ActiveSetting.Setting?.DefaultPath(AnalyticalSettingParameter.DefaultProfileLibraryFileName_TM59);
            if (string.IsNullOrWhiteSpace(path) || !System.IO.File.Exists(path))
                return null;

            return Core.Create.IJSAMObject<ProfileLibrary>(System.IO.File.ReadAllText(path));
        }
    }
}
