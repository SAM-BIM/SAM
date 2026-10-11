// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

namespace SAM.Core.Optimisation
{
    /// <summary>When the search stops besides its own convergence criterion.</summary>
    public sealed class StoppingCriteria
    {
        public StoppingCriteria()
        {
        }

        public StoppingCriteria(int? maximumSimulations)
        {
            MaximumSimulations = maximumSimulations;
        }

        public StoppingCriteria(StoppingCriteria stoppingCriteria)
        {
            MaximumSimulations = stoppingCriteria?.MaximumSimulations;
        }

        /// <summary>The run stops after this many simulations (model evaluations). At least 1; null uses the engine default.</summary>
        public int? MaximumSimulations { get; set; }
    }
}
