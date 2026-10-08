// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

namespace SAM.Core.Optimisation
{
    /// <summary>The values a design variable may take. JSON: "continuous", "integer", "discrete".</summary>
    public enum DesignVariableType
    {
        /// <summary>Any real value between the bounds.</summary>
        Continuous,

        /// <summary>Whole numbers between the bounds. Reserved: no engine runs it yet.</summary>
        Integer,

        /// <summary>One of a listed set of values. Reserved: no engine runs it yet.</summary>
        Discrete,
    }
}
