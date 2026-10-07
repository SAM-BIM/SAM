// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace SAM.Math
{
    /// <summary>
    /// The parameters to vary and the number of outputs every evaluation returns. Only the first output is
    /// minimised; the others are recorded (spec §1.2).
    /// </summary>
    public sealed class OptimisationProblem
    {
        public OptimisationProblem(IEnumerable<OptimisationParameter> parameters, int outputCount = 1)
        {
            if (parameters == null)
            {
                throw new ArgumentNullException(nameof(parameters));
            }

            List<OptimisationParameter> list = parameters.ToList();
            if (list.Count == 0)
            {
                throw new ArgumentException("At least one parameter is required.", nameof(parameters));
            }

            if (list.Any(x => x == null))
            {
                throw new ArgumentException("Parameters must not be null.", nameof(parameters));
            }

            if (outputCount < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(outputCount), "At least one output (the objective) is required.");
            }

            Parameters = new ReadOnlyCollection<OptimisationParameter>(list);
            OutputCount = outputCount;
        }

        public IReadOnlyList<OptimisationParameter> Parameters { get; }

        /// <summary>
        /// Number of outputs per evaluation. The first is the objective. An evaluation that returns a different
        /// number counts as failed, as a missing output does in GenOpt.
        /// </summary>
        public int OutputCount { get; }
    }
}
