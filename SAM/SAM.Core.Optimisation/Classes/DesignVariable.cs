// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

namespace SAM.Core.Optimisation
{
    /// <summary>
    /// A value the optimiser may change between simulations, within <see cref="Minimum"/> and <see cref="Maximum"/>.
    /// For a "tas-script" model, <see cref="Name"/> is the name the script reads (<c>Variables["name"]</c>).
    /// </summary>
    public sealed class DesignVariable
    {
        public DesignVariable()
        {
        }

        public DesignVariable(string name, double minimum, double maximum, double? start = null, double? step = null)
        {
            Name = name;
            Minimum = minimum;
            Maximum = maximum;
            Start = start;
            Step = step;
        }

        public DesignVariable(DesignVariable designVariable)
        {
            if (designVariable == null)
            {
                return;
            }

            Name = designVariable.Name;
            Description = designVariable.Description;
            Type = designVariable.Type;
            Quantity = designVariable.Quantity;
            Unit = designVariable.Unit;
            Minimum = designVariable.Minimum;
            Maximum = designVariable.Maximum;
            Start = designVariable.Start;
            Step = designVariable.Step;
        }

        /// <summary>The identifier; for a "tas-script" model, the name the script reads. Required, unique.</summary>
        public string Name { get; set; }

        /// <summary>The variable in engineering terms. Optional.</summary>
        public string Description { get; set; }

        public DesignVariableType Type { get; set; } = DesignVariableType.Continuous;

        /// <summary>The declared quantity; <see cref="OptimisationQuantity.Unspecified"/> when not stated.</summary>
        public OptimisationQuantity Quantity { get; set; } = OptimisationQuantity.Unspecified;

        /// <summary>The declared unit symbol, for example "°C". Optional. It describes the value; it is never verified against the model.</summary>
        public string Unit { get; set; }

        /// <summary>The lower bound (inclusive).</summary>
        public double Minimum { get; set; }

        /// <summary>The upper bound (inclusive).</summary>
        public double Maximum { get; set; }

        /// <summary>The start value. Required by pattern search; not used by golden section (kept and passed on when given).</summary>
        public double? Start { get; set; }

        /// <summary>The initial step. Required by pattern search; not used by golden section (kept and passed on when given).</summary>
        public double? Step { get; set; }
    }
}
