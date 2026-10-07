// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Collections.Generic;

namespace SAM.Math
{
    /// <summary>One request to an <see cref="IObjectiveEvaluator"/>.</summary>
    public sealed class ObjectiveEvaluationRequest
    {
        internal ObjectiveEvaluationRequest(int simulation, int attempt, double[] coordinates)
        {
            Simulation = simulation;
            Attempt = attempt;
            Coordinates = System.Array.AsReadOnly((double[])coordinates.Clone());
        }

        /// <summary>Simulation number (1-based, in evaluation order). A retry keeps the same number.</summary>
        public int Simulation { get; }

        /// <summary>1 for the first attempt, 2 for the single retry.</summary>
        public int Attempt { get; }

        /// <summary>The point to evaluate (already float-rounded for pattern search).</summary>
        public IReadOnlyList<double> Coordinates { get; }
    }
}
