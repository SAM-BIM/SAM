// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Collections.Generic;
using System.Linq;

namespace SAM.Core.Optimisation
{
    /// <summary>
    /// A portable, declarative optimisation problem (schema <see cref="Schema"/>): which model engine is used, which
    /// design variables may change and within which bounds, which outputs the model reports, which output is the
    /// objective, the search method and when it stops. It holds no machine-specific path and nothing executable: an
    /// engine (for example SAM_Tas) runs it with its own local settings.
    /// <para>
    /// The same object backs a visual form, the definition text (<see cref="Convert.ToJson"/>,
    /// <see cref="Create.OptimisationDefinition(string, out List{OptimisationDiagnostic}, IOptimisationCapabilities, bool)"/>)
    /// and the AI exchange (<see cref="Query.AIExchangeText"/>). A definition may be semantically invalid (for example
    /// a minimum above its maximum) so that either view can show and fix it; <see cref="Query.Diagnostics"/> says what
    /// is wrong and <see cref="Query.IsRunnable(OptimisationDefinition, IOptimisationCapabilities)"/> whether it can run.
    /// </para>
    /// </summary>
    public sealed class OptimisationDefinition
    {
        /// <summary>The schema identifier written as "schema" in the definition text.</summary>
        public const string Schema = "sam.optimisation/1";

        /// <summary>The schema name before the "/" and the version.</summary>
        public const string SchemaName = "sam.optimisation";

        /// <summary>The major schema version this library reads and writes.</summary>
        public const int SchemaVersion = 1;

        public OptimisationDefinition()
        {
        }

        public OptimisationDefinition(OptimisationDefinition optimisationDefinition)
        {
            if (optimisationDefinition == null)
            {
                return;
            }

            Name = optimisationDefinition.Name;
            Description = optimisationDefinition.Description;
            Notes = optimisationDefinition.Notes;
            Model = optimisationDefinition.Model == null ? null : new OptimisationModel(optimisationDefinition.Model);
            Variables = optimisationDefinition.Variables?.Select(x => x == null ? null : new DesignVariable(x)).ToList() ?? new List<DesignVariable>();
            Outputs = optimisationDefinition.Outputs?.Select(x => x == null ? null : new OptimisationOutput(x)).ToList() ?? new List<OptimisationOutput>();
            Objective = optimisationDefinition.Objective == null ? null : new OptimisationObjective(optimisationDefinition.Objective);
            Constraints = optimisationDefinition.Constraints?.Select(x => x == null ? null : new OptimisationConstraint(x)).ToList() ?? new List<OptimisationConstraint>();
            Method = optimisationDefinition.Method?.Clone();
            Stopping = optimisationDefinition.Stopping == null ? null : new StoppingCriteria(optimisationDefinition.Stopping);
        }

        /// <summary>A short name, for example "Systems Demo – heating setpoint (golden section)". Optional.</summary>
        public string Name { get; set; }

        /// <summary>What the optimisation is for, in engineering terms. Optional.</summary>
        public string Description { get; set; }

        /// <summary>Free engineering notes. Optional; never interpreted.</summary>
        public string Notes { get; set; }

        /// <summary>The model engine and a description of the model. Required.</summary>
        public OptimisationModel Model { get; set; }

        /// <summary>The design variables, in the order the engine receives them.</summary>
        public List<DesignVariable> Variables { get; set; } = new List<DesignVariable>();

        /// <summary>Every output the model reports for each simulation: the objective's and the recorded ones.</summary>
        public List<OptimisationOutput> Outputs { get; set; } = new List<OptimisationOutput>();

        /// <summary>The output that is optimised, and whether it is minimised or maximised. Required.</summary>
        public OptimisationObjective Objective { get; set; }

        /// <summary>Limits on outputs. Part of the schema; an engine that cannot enforce them makes the definition non-runnable.</summary>
        public List<OptimisationConstraint> Constraints { get; set; } = new List<OptimisationConstraint>();

        /// <summary>The search method and its settings. Required.</summary>
        public OptimisationMethod Method { get; set; }

        /// <summary>When the search stops besides its own convergence. Optional: null uses the engine's defaults.</summary>
        public StoppingCriteria Stopping { get; set; }

        /// <summary>The variable named <paramref name="name"/> (ordinal comparison), or null.</summary>
        public DesignVariable Variable(string name)
        {
            return Variables?.Find(x => x != null && x.Name == name);
        }

        /// <summary>The output named <paramref name="name"/> (ordinal comparison), or null.</summary>
        public OptimisationOutput Output(string name)
        {
            return Outputs?.Find(x => x != null && x.Name == name);
        }

        /// <summary>The outputs that are recorded but not optimised: every output except the objective's.</summary>
        public List<OptimisationOutput> RecordedOutputs()
        {
            string name = Objective?.Output;
            return Outputs?.FindAll(x => x != null && x.Name != name) ?? new List<OptimisationOutput>();
        }
    }
}
