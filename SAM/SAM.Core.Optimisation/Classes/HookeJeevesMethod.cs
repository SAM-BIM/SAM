// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

namespace SAM.Core.Optimisation
{
    /// <summary>
    /// Hooke-Jeeves pattern search on one or more design variables: it starts at each variable's start value, moves
    /// by its step, and reduces the step as it converges.
    /// </summary>
    public sealed class HookeJeevesMethod : OptimisationMethod
    {
        public HookeJeevesMethod()
        {
        }

        public HookeJeevesMethod(HookeJeevesMethod hookeJeevesMethod)
        {
            if (hookeJeevesMethod == null)
            {
                return;
            }

            StepReductionFactor = hookeJeevesMethod.StepReductionFactor;
            InitialStepExponent = hookeJeevesMethod.InitialStepExponent;
            StepExponentIncrement = hookeJeevesMethod.StepExponentIncrement;
            StepReductions = hookeJeevesMethod.StepReductions;
        }

        public override OptimisationAlgorithm Algorithm => OptimisationAlgorithm.HookeJeeves;

        /// <summary>Each step reduction divides the step by this. A whole number of at least 2; null uses the engine default.</summary>
        public int? StepReductionFactor { get; set; }

        /// <summary>The initial step is the variable's step divided by the factor to this power. At least 0; null uses the engine default.</summary>
        public int? InitialStepExponent { get; set; }

        /// <summary>How much each reduction raises the step exponent. At least 1; null uses the engine default.</summary>
        public int? StepExponentIncrement { get; set; }

        /// <summary>The search stops once the step has been reduced this many times. At least 1; null uses the engine default.</summary>
        public int? StepReductions { get; set; }

        public override OptimisationMethod Clone()
        {
            return new HookeJeevesMethod(this);
        }
    }
}
