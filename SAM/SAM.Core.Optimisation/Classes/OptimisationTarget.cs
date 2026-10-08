// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Collections.Generic;
using System.Linq;

namespace SAM.Core.Optimisation
{
    /// <summary>
    /// What a design variable changes in the model (<see cref="DesignVariable.Target"/>): a kind the engine can change,
    /// the model item and, for a choice between named model items (a "discrete" variable, for example one of four
    /// glazing constructions), the <see cref="Options"/>. The engine writes the variable's value into that item for
    /// every simulation.
    /// </summary>
    public sealed class OptimisationTarget : OptimisationBinding
    {
        public OptimisationTarget()
        {
        }

        public OptimisationTarget(string kind, IDictionary<string, string> reference = null, IDictionary<string, double> parameters = null, IEnumerable<string> options = null)
            : base(kind, reference, parameters)
        {
            Options = options?.ToList() ?? new List<string>();
        }

        public OptimisationTarget(OptimisationTarget optimisationTarget)
            : base(optimisationTarget)
        {
            Options = optimisationTarget?.Options?.ToList() ?? new List<string>();
        }

        /// <summary>
        /// The model item names a choice target chooses between, in order; empty for a value target. Only a kind that
        /// accepts options (<see cref="OptimisationBindingCapability.AcceptsOptions"/>) takes them, and the variable is
        /// then "discrete" with minimum 1 and maximum the number of options: the value 1 is the first option.
        /// </summary>
        public List<string> Options { get; set; } = new List<string>();
    }
}
