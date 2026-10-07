// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

namespace SAM.Math
{
    /// <summary>Stopping rule of <see cref="GoldenSection"/> (documentation/GenOpt-3.1.1-Behaviour.md §4).</summary>
    public enum GoldenSectionStoppingCriterion
    {
        /// <summary>No keyword: stop after MaximumSimulations - 1 interval reductions (9 if MaximumSimulations ≤ 1).</summary>
        MaximumSimulations,

        /// <summary>
        /// GenOpt "AbsDiffFunction": stop when the function values differ by less than
        /// <see cref="GoldenSection.AbsoluteDifference"/>, or after MaximumSimulations - 1 reductions.
        /// </summary>
        AbsoluteDifference,

        /// <summary>GenOpt "IntervalReduction": stop when the interval has shrunk by <see cref="GoldenSection.IntervalReduction"/>.</summary>
        IntervalReduction,
    }
}
