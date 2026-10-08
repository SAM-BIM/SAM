// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

namespace SAM.Core.Optimisation
{
    /// <summary>
    /// A result the model reports for every simulation. One output is the objective
    /// (<see cref="OptimisationDefinition.Objective"/>); the others are recorded. For a "tas-script" model,
    /// <see cref="Name"/> is the name the script writes (<c>ScriptOutput.SetValue("name", value)</c>), and the script
    /// computes the value (any aggregation, such as an annual total, happens there).
    /// </summary>
    public sealed class OptimisationOutput
    {
        public OptimisationOutput()
        {
        }

        public OptimisationOutput(string name, string description = null)
        {
            Name = name;
            Description = description;
        }

        public OptimisationOutput(OptimisationOutput optimisationOutput)
        {
            if (optimisationOutput == null)
            {
                return;
            }

            Name = optimisationOutput.Name;
            Description = optimisationOutput.Description;
            Quantity = optimisationOutput.Quantity;
            Unit = optimisationOutput.Unit;
            Aggregation = optimisationOutput.Aggregation;
        }

        /// <summary>The identifier; for a "tas-script" model, the name the script writes. Required, unique.</summary>
        public string Name { get; set; }

        /// <summary>The output in engineering terms. Optional.</summary>
        public string Description { get; set; }

        /// <summary>The declared quantity; <see cref="OptimisationQuantity.Unspecified"/> when not stated.</summary>
        public OptimisationQuantity Quantity { get; set; } = OptimisationQuantity.Unspecified;

        /// <summary>The declared unit symbol, for example "GBP". Optional. It describes the value; it is never verified against the model.</summary>
        public string Unit { get; set; }

        /// <summary>
        /// How the model aggregated the value, for example "annual-total" or "maximum". Optional and informational:
        /// SAM does not compute it for a script output.
        /// </summary>
        public string Aggregation { get; set; }
    }
}
