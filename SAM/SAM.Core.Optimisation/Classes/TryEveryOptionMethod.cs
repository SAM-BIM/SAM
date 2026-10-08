// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

namespace SAM.Core.Optimisation
{
    /// <summary>
    /// Try every option: one simulation per option of a choice (a "discrete" variable numbered 1 to n), in order 1 to
    /// n. The best is the lowest objective; a tie goes to the lower option number. It has no settings: the options are
    /// the whole search, so there is no tolerance, start or step, and the run needs as many simulations as there are
    /// options (<see cref="StoppingCriteria.MaximumSimulations"/> must allow them).
    /// </summary>
    public sealed class TryEveryOptionMethod : OptimisationMethod
    {
        public TryEveryOptionMethod()
        {
        }

        public TryEveryOptionMethod(TryEveryOptionMethod tryEveryOptionMethod)
        {
        }

        public override OptimisationAlgorithm Algorithm => OptimisationAlgorithm.TryEveryOption;

        public override OptimisationMethod Clone()
        {
            return new TryEveryOptionMethod(this);
        }
    }
}
