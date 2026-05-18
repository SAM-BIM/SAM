// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

namespace SAM.Core
{
    /// <summary>
    /// JSON output style for the <c>Convert.ToString</c> overloads. Replaces
    /// the formatting enum that previously lived under the
    /// <c>SAM.Core.Json</c> compatibility shim, so callers using
    /// <c>Convert.ToString(obj, Formatting.Indented)</c> keep compiling after
    /// the shim is removed.
    /// </summary>
    public enum Formatting
    {
        None,
        Indented
    }
}
