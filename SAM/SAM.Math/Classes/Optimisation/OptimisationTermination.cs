// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;

namespace SAM.Math
{
    /// <summary>Internal control flow: stops a run with the given outcome (GenOpt's error exits).</summary>
    internal sealed class OptimisationTermination : Exception
    {
        public OptimisationTermination(OptimisationOutcome outcome)
        {
            Outcome = outcome;
        }

        public OptimisationOutcome Outcome { get; }
    }
}
