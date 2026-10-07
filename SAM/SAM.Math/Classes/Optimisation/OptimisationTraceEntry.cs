// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Collections.Generic;

namespace SAM.Math
{
    /// <summary>
    /// One reported point. It is immutable. Entries correspond one-to-one to GenOpt's OutputListingAll rows
    /// (<see cref="OptimisationResult.Entries"/>) and OutputListingMain rows
    /// (<see cref="OptimisationResult.MainIterations"/>); spec §5.
    /// </summary>
    public sealed class OptimisationTraceEntry
    {
        internal OptimisationTraceEntry(EvaluationPoint point, int mainIteration, int subIteration)
        {
            Simulation = point.Simulation;
            MainIteration = mainIteration;
            SubIteration = subIteration;
            Coordinates = System.Array.AsReadOnly((double[])point.X.Clone());
            Outputs = System.Array.AsReadOnly((double[])point.F.Clone());
            Event = point.Event;
            Delta = point.Delta;
            ParameterIndex = point.ParameterIndex;
            Direction = point.Direction;
        }

        /// <summary>A minimum entry: the stored entry's numbers and values with a new event.</summary>
        internal OptimisationTraceEntry(OptimisationTraceEntry source, OptimisationEvent @event)
        {
            Simulation = source.Simulation;
            MainIteration = source.MainIteration;
            SubIteration = source.SubIteration;
            Coordinates = source.Coordinates;
            Outputs = source.Outputs;
            Event = @event;
            Delta = double.NaN;
            ParameterIndex = -1;
            Direction = 0;
        }

        /// <summary>
        /// Simulation number. For a cache hit it is the simulation count at the time of the lookup, not the number
        /// of the simulation that produced the value.
        /// </summary>
        public int Simulation { get; }

        public int MainIteration { get; }

        /// <summary>Sub-iteration counter. For a main-iteration entry this is the stored (already incremented) value.</summary>
        public int SubIteration { get; }

        public IReadOnlyList<double> Coordinates { get; }

        /// <summary>All outputs; the first is the objective.</summary>
        public IReadOnlyList<double> Outputs { get; }

        public double Objective => Outputs[0];

        public OptimisationEvent Event { get; }

        /// <summary>Mesh size Δ for <see cref="OptimisationEvent.ExplorationBase"/> and the step-size events; otherwise NaN.</summary>
        public double Delta { get; }

        /// <summary>The coordinate moved, for <see cref="OptimisationEvent.CostReduced"/>/<see cref="OptimisationEvent.CostNotReduced"/>; otherwise -1.</summary>
        public int ParameterIndex { get; }

        /// <summary>
        /// +1 for the "+Step" trial and -1 for the "−Step" trial (GenOpt's "+d"/"-d"), for the cost events.
        /// Otherwise 0. With a negative Step, "+Step" moves the coordinate down.
        /// </summary>
        public int Direction { get; }
    }
}
