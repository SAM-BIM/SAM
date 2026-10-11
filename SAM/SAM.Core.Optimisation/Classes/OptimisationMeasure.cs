// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Collections.Generic;

namespace SAM.Core.Optimisation
{
    /// <summary>
    /// What an output measures in the model's results (<see cref="OptimisationOutput.Measure"/>): a kind the engine can
    /// read, the model item when the kind needs one, and its parameters (for example an overheating threshold). The
    /// engine computes the output's value from the results of every simulation.
    /// </summary>
    public sealed class OptimisationMeasure : OptimisationBinding
    {
        public OptimisationMeasure()
        {
        }

        public OptimisationMeasure(string kind, IDictionary<string, string> reference = null, IDictionary<string, double> parameters = null)
            : base(kind, reference, parameters)
        {
        }

        public OptimisationMeasure(OptimisationMeasure optimisationMeasure)
            : base(optimisationMeasure)
        {
        }
    }
}
