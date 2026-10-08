// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

namespace SAM.Core.Optimisation
{
    /// <summary>
    /// A limit on an output that an acceptable design must respect, for example OverheatingHours at most 100 h.
    /// Exactly one of <see cref="AtMost"/> and <see cref="AtLeast"/> is given. Part of the schema; a definition with a
    /// constraint is not runnable on an engine that cannot enforce constraints
    /// (<see cref="IOptimisationCapabilities.SupportsConstraints"/>).
    /// </summary>
    public sealed class OptimisationConstraint
    {
        public OptimisationConstraint()
        {
        }

        public OptimisationConstraint(OptimisationConstraint optimisationConstraint)
        {
            if (optimisationConstraint == null)
            {
                return;
            }

            Output = optimisationConstraint.Output;
            AtMost = optimisationConstraint.AtMost;
            AtLeast = optimisationConstraint.AtLeast;
            Unit = optimisationConstraint.Unit;
        }

        /// <summary>The name of one of the definition's outputs. Required.</summary>
        public string Output { get; set; }

        /// <summary>The largest acceptable value (inclusive).</summary>
        public double? AtMost { get; set; }

        /// <summary>The smallest acceptable value (inclusive).</summary>
        public double? AtLeast { get; set; }

        /// <summary>The unit of the limit. Optional; when given it must suit the output's quantity.</summary>
        public string Unit { get; set; }
    }
}
