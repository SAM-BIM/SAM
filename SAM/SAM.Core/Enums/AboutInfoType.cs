// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.ComponentModel;

namespace SAM.Core
{
    [Description("About Info Type")]
    public enum AboutInfoType
    {
        [Description("SAM-BIM Info")] HoareLea,
        [Description("SAM Info")] SAM,
        [Description("Other Info")] Other,
    }
}
