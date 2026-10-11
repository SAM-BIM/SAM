// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Collections.Generic;
using System.Linq;

namespace SAM.Math
{
    /// <summary>
    /// Generalised pattern search (GPS) as GenOpt 3.1.1 runs it (documentation/GenOpt-3.1.1-Behaviour.md §2.1, §2.2
    /// and §3).
    /// <para>
    /// The search works in each parameter's original units, on the mesh Initial + Δ·Step·ℤ, with Δ = 1 / r^s. Before
    /// every evaluation each coordinate is rounded through <see cref="Java8FloatText"/>. An out-of-bounds point gets
    /// <see cref="double.MaxValue"/> outputs and is neither simulated nor reported. There are
    /// <see cref="NumberOfStepReduction"/> + 1 mesh levels, and the mesh never expands.
    /// </para>
    /// </summary>
    public abstract class GeneralisedPatternSearch : Optimiser
    {
        internal GeneralisedPatternSearch()
        {
        }

        /// <summary>GenOpt "MeshSizeDivider" r (at least 2).</summary>
        public int MeshSizeDivider { get; set; } = 2;

        /// <summary>GenOpt "InitialMeshSizeExponent" s0 (at least 0).</summary>
        public int InitialMeshSizeExponent { get; set; } = 0;

        /// <summary>GenOpt "MeshSizeExponentIncrement" t (at least 1).</summary>
        public int MeshSizeExponentIncrement { get; set; } = 1;

        /// <summary>GenOpt "NumberOfStepReduction" m (at least 1): the run stops on the first failure at the last level.</summary>
        public int NumberOfStepReduction { get; set; } = 4;

        /// <summary>True for Hooke-Jeeves (pattern move plus exploration), false for coordinate search.</summary>
        internal abstract bool GlobalSearch { get; }

        internal override void Validate(OptimisationProblem problem)
        {
            if (MeshSizeDivider < 2)
            {
                throw new InvalidOperationException("MeshSizeDivider must be at least 2.");
            }

            if (InitialMeshSizeExponent < 0)
            {
                throw new InvalidOperationException("InitialMeshSizeExponent must not be negative.");
            }

            if (MeshSizeExponentIncrement < 1)
            {
                throw new InvalidOperationException("MeshSizeExponentIncrement must be at least 1.");
            }

            if (NumberOfStepReduction < 1)
            {
                throw new InvalidOperationException("NumberOfStepReduction must be at least 1.");
            }
        }

        internal override OptimisationOutcome Execute(EvaluationContext context)
        {
            return new Search(this, context).Run();
        }

        /// <summary>The state of one run (GenOpt ModelGPS._run).</summary>
        private sealed class Search
        {
            private static readonly EvaluationPoint[] None = new EvaluationPoint[0];

            private readonly EvaluationContext context;
            private readonly bool globalSearch;
            private readonly int divider;
            private readonly int initialExponent;
            private readonly int increment;
            private readonly int maxReductions;
            private readonly int[] directionPointer;
            private double delta;

            public Search(GeneralisedPatternSearch settings, EvaluationContext context)
            {
                this.context = context;
                globalSearch = settings.GlobalSearch;
                divider = settings.MeshSizeDivider;
                initialExponent = settings.InitialMeshSizeExponent;
                increment = settings.MeshSizeExponentIncrement;
                maxReductions = settings.NumberOfStepReduction;
                directionPointer = new int[context.Dimension];
            }

            /// <summary>
            /// GenOpt's do { switch (step) … } while (iterate &amp;&amp; simulations &lt; MaxIte) loop, followed literally.
            /// After a global improvement the switch breaks, so the update runs on the next pass, after the
            /// simulation-limit test (spec §3.2).
            /// </summary>
            public OptimisationOutcome Run()
            {
                int dim = context.Dimension;
                for (int i = 0; i < dim; i++)
                {
                    if (context.Initial[i] < context.Lower[i] || context.Initial[i] > context.Upper[i])
                    {
                        return OptimisationOutcome.InitialPointInfeasible;
                    }
                }

                OptimisationOutcome outcome = OptimisationOutcome.Undefined;
                int reductions = 0;
                int k = 0;
                bool iterate = true;
                int stepState = 0;
                List<EvaluationPoint> x = new List<EvaluationPoint>();
                EvaluationPoint[] global = None;
                EvaluationPoint[] local = None;
                int exponent = initialExponent;
                delta = 1.0 / System.Math.Pow(divider, initialExponent);

                do
                {
                    bool runCase1 = stepState <= 1;
                    bool runCase2 = stepState <= 2;
                    if (stepState == 0)
                    {
                        // Step 0: the initial point.
                        x.Add(context.EvaluateRounded(context.CreatePoint(context.Initial, double.MaxValue)));
                        x[k].SetEvent(OptimisationEvent.InitialPoint);
                        context.Report(x[k], false, true);
                        context.Report(x[k], true, true);
                    }

                    bool skipToUpdate = false;
                    if (runCase1)
                    {
                        // Step 1: global search.
                        global = globalSearch ? GlobalSearchHookeJeeves(x) : None;
                        if (global.Length != 0 && global[Lowest(global)].F[0] < x[k].F[0])
                        {
                            local = None;
                            stepState = 3;
                            skipToUpdate = true;
                        }
                        else
                        {
                            stepState = 2;
                        }
                    }

                    if (skipToUpdate)
                    {
                        continue;
                    }

                    if (runCase2)
                    {
                        // Step 2: local search (fall-through).
                        local = Explore(x[x.Count - 1]);
                    }

                    // Step 3: parameter update.
                    EvaluationPoint[] searchSet = local.Concat(global).ToArray();
                    int iLow = searchSet.Length == 0 ? -1 : Lowest(searchSet);
                    if (iLow != -1 && searchSet[iLow].F[0] < x[k].F[0])
                    {
                        EvaluationPoint next = searchSet[iLow].Clone();
                        next.SetEvent(iLow < local.Length ? OptimisationEvent.LocalSearchReducedCost : OptimisationEvent.GlobalSearchReducedCost);
                        x.Add(next);
                        context.Report(next, false, true);
                        context.Report(next, true, true);
                    }
                    else
                    {
                        if (local.Length <= dim && !context.MaximumSimulationsReached)
                        {
                            return OptimisationOutcome.AlgorithmError;
                        }

                        EvaluationPoint next = x[k].Clone();
                        x.Add(next);
                        exponent += increment;
                        delta = 1.0 / System.Math.Pow(divider, exponent);
                        if (reductions == maxReductions)
                        {
                            next.SetEvent(OptimisationEvent.MaximumStepReductionsReached, delta);
                            context.Report(next, false, true);
                            context.Report(next, true, true);
                            iterate = false;
                            context.ReportMinimum(OptimisationEvent.MinimumPoint);
                            outcome = OptimisationOutcome.Success;
                        }
                        else
                        {
                            next.SetEvent(OptimisationEvent.StepSizeReduced, delta);
                            context.Report(next, false, true);
                            context.Report(next, true, true);
                            reductions++;
                        }
                    }

                    global = None;
                    local = None;
                    k++;
                    stepState = 1;
                }
                while (iterate && !context.MaximumSimulationsReached);

                if (iterate)
                {
                    context.ReportMinimum(OptimisationEvent.CurrentLowestPoint);
                    outcome = OptimisationOutcome.MaximumSimulationsReached;
                }

                return outcome;
            }

            /// <summary>
            /// Hooke-Jeeves global search. For k &gt; 0 the pattern point b = 2·X[k] − X[k−1] is evaluated and reported;
            /// otherwise b = X[k] is not evaluated. Then Explore(b).
            /// </summary>
            private EvaluationPoint[] GlobalSearchHookeJeeves(List<EvaluationPoint> x)
            {
                int last = x.Count - 1;
                EvaluationPoint basePoint = x[last].Clone();
                basePoint.SetEvent(OptimisationEvent.ExplorationBase, delta);
                if (last > 0)
                {
                    for (int i = 0; i < context.Dimension; i++)
                    {
                        basePoint.X[i] = (2 * x[last].X[i]) - x[last - 1].X[i];
                    }

                    basePoint = context.EvaluateRounded(basePoint);
                    context.Report(basePoint, false, true);
                }

                return Explore(basePoint);
            }

            /// <summary>
            /// Coordinate polling (spec §3.3). For each coordinate, while simulations remain, try the remembered sign.
            /// If that fails, flip the remembered sign (the flip persists) and try the other one. Returns every
            /// trial outcome; a failed trial is returned as a copy of the point it started from.
            /// </summary>
            private EvaluationPoint[] Explore(EvaluationPoint start)
            {
                List<EvaluationPoint> points = new List<EvaluationPoint> { start.Clone() };
                int iLow = 0;
                for (int i = 0; i < context.Dimension; i++)
                {
                    if (!context.MaximumSimulationsReached)
                    {
                        points.Add(Perturb(points[iLow], i));
                        if (points[points.Count - 1].F[0] < points[iLow].F[0])
                        {
                            iLow = points.Count - 1;
                        }
                        else if (!context.MaximumSimulationsReached)
                        {
                            points.Add(Perturb(points[iLow], i));
                            if (points[points.Count - 1].F[0] < points[iLow].F[0])
                            {
                                iLow = points.Count - 1;
                            }
                        }
                    }
                }

                return points.Skip(1).ToArray();
            }

            private EvaluationPoint Perturb(EvaluationPoint best, int i)
            {
                bool plus = directionPointer[i] == 0;
                double direction = plus ? context.Step[i] : -context.Step[i];
                EvaluationPoint trial = best.Clone();
                trial.X[i] = best.X[i] + (delta * direction);
                trial = context.EvaluateRounded(trial);
                if (trial.F[0] < best.F[0])
                {
                    trial.SetEvent(OptimisationEvent.CostReduced, double.NaN, i, plus ? 1 : -1);
                    context.Report(trial, false, true);
                    return trial;
                }

                trial.SetEvent(OptimisationEvent.CostNotReduced, double.NaN, i, plus ? 1 : -1);
                context.Report(trial, false, true);
                directionPointer[i] = plus ? 1 : 0;
                return best.Clone();
            }

            /// <summary>Index of the first strict minimum of the objective.</summary>
            private static int Lowest(EvaluationPoint[] points)
            {
                int iLow = 0;
                for (int i = 1; i < points.Length; i++)
                {
                    if (points[i].F[0] < points[iLow].F[0])
                    {
                        iLow = i;
                    }
                }

                return iLow;
            }
        }
    }
}
