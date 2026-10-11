// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

namespace SAM.Core.Optimisation
{
    /// <summary>
    /// What the optimisation runs on, without any machine-specific path: the engine that evaluates a design (for
    /// example "tas-script") and a description of the model. Where the model's files are is a local setting of the
    /// engine, never part of the portable definition.
    /// </summary>
    public sealed class OptimisationModel
    {
        public OptimisationModel()
        {
        }

        public OptimisationModel(string engine, string description = null)
        {
            Engine = engine;
            Description = description;
        }

        public OptimisationModel(OptimisationModel optimisationModel)
        {
            if (optimisationModel == null)
            {
                return;
            }

            Engine = optimisationModel.Engine;
            Description = optimisationModel.Description;
        }

        /// <summary>The engine identifier, for example "tas-script". Required.</summary>
        public string Engine { get; set; }

        /// <summary>The model in engineering terms. Optional.</summary>
        public string Description { get; set; }
    }
}
