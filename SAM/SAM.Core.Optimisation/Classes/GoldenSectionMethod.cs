// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

namespace SAM.Core.Optimisation
{
    /// <summary>
    /// Golden section: a line search on exactly one design variable between its bounds. It does not use the
    /// variable's start or step.
    /// </summary>
    public sealed class GoldenSectionMethod : OptimisationMethod
    {
        public GoldenSectionMethod()
        {
        }

        public GoldenSectionMethod(GoldenSectionMethod goldenSectionMethod)
        {
            Tolerance = goldenSectionMethod?.Tolerance;
        }

        public override OptimisationAlgorithm Algorithm => OptimisationAlgorithm.GoldenSection;

        /// <summary>
        /// Stops when the objective values of the interval differ by less than this (in the objective's own unit).
        /// Greater than 0; null uses the engine default.
        /// </summary>
        public double? Tolerance { get; set; }

        public override OptimisationMethod Clone()
        {
            return new GoldenSectionMethod(this);
        }
    }
}
