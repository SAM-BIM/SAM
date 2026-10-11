// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;

namespace SAM.Math
{
    /// <summary>
    /// One continuous optimisation parameter, used in its original units. Pattern search starts at
    /// <see cref="Initial"/> and moves by ±Δ·<see cref="Step"/>; golden section uses only the bounds.
    /// </summary>
    public sealed class OptimisationParameter
    {
        /// <param name="name">Name, used only to label results.</param>
        /// <param name="initial">Initial value (pattern search).</param>
        /// <param name="minimum">Lower bound; <see cref="double.NegativeInfinity"/> when unbounded.</param>
        /// <param name="maximum">Upper bound; <see cref="double.PositiveInfinity"/> when unbounded.</param>
        /// <param name="step">
        /// Pattern-search step. 0 never moves the coordinate; a negative step swaps the order of the two trial
        /// directions (spec §3.1).
        /// </param>
        public OptimisationParameter(string name, double initial, double minimum, double maximum, double step)
        {
            if (double.IsNaN(initial) || double.IsInfinity(initial))
            {
                throw new ArgumentOutOfRangeException(nameof(initial), "The initial value must be finite.");
            }

            if (double.IsNaN(minimum) || double.IsNaN(maximum))
            {
                throw new ArgumentOutOfRangeException(nameof(minimum), "Bounds must not be NaN.");
            }

            if (double.IsNaN(step) || double.IsInfinity(step))
            {
                throw new ArgumentOutOfRangeException(nameof(step), "The step must be finite.");
            }

            Name = name;
            Initial = initial;
            Minimum = minimum;
            Maximum = maximum;
            Step = step;
        }

        /// <summary>Creates an unbounded parameter.</summary>
        public OptimisationParameter(string name, double initial, double step)
            : this(name, initial, double.NegativeInfinity, double.PositiveInfinity, step)
        {
        }

        public string Name { get; }

        public double Initial { get; }

        public double Minimum { get; }

        public double Maximum { get; }

        public double Step { get; }
    }
}
