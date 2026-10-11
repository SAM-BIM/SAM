// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

namespace SAM.Math
{
    /// <summary>A mutable working point of the kernel: coordinates, outputs, simulation number and the event it reports.</summary>
    internal sealed class EvaluationPoint
    {
        public EvaluationPoint(double[] x, double[] f)
        {
            X = x;
            F = f;
        }

        public double[] X;

        public double[] F;

        public int Simulation;

        public OptimisationEvent Event;

        public double Delta = double.NaN;

        public int ParameterIndex = -1;

        public int Direction;

        public EvaluationPoint Clone()
        {
            return new EvaluationPoint((double[])X.Clone(), (double[])F.Clone())
            {
                Simulation = Simulation,
                Event = Event,
                Delta = Delta,
                ParameterIndex = ParameterIndex,
                Direction = Direction,
            };
        }

        public void SetEvent(OptimisationEvent @event, double delta = double.NaN, int parameterIndex = -1, int direction = 0)
        {
            Event = @event;
            Delta = delta;
            ParameterIndex = parameterIndex;
            Direction = direction;
        }
    }
}
