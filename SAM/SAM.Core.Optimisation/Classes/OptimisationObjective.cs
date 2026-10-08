// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

namespace SAM.Core.Optimisation
{
    /// <summary>
    /// The objective: the output that is optimised (also called the cost function in optimisation literature and in
    /// Tas GenOpt scripts) and whether it is minimised or maximised.
    /// </summary>
    public sealed class OptimisationObjective
    {
        public OptimisationObjective()
        {
        }

        public OptimisationObjective(string output, ObjectiveSense sense)
        {
            Output = output;
            Sense = sense;
        }

        public OptimisationObjective(OptimisationObjective optimisationObjective)
        {
            if (optimisationObjective == null)
            {
                return;
            }

            Output = optimisationObjective.Output;
            Sense = optimisationObjective.Sense;
        }

        /// <summary>The name of one of the definition's outputs. Required.</summary>
        public string Output { get; set; }

        /// <summary>Minimise or maximise. Required in the definition text; there is no implicit default there.</summary>
        public ObjectiveSense Sense { get; set; }
    }
}
