// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Collections.Generic;
using System.Linq;

namespace SAM.Core.Optimisation
{
    /// <summary>
    /// The design variables and outputs a model makes available, so that a person or an AI assistant chooses from
    /// real names instead of inventing them. An engine fills it from what it can discover: for a Tas script, the names
    /// the script text reads and writes (name-only entries); for an engine that changes the model itself, the model
    /// items it can change (entries with a target) and the results it can measure (entries with a measure), with their
    /// current values and units.
    /// </summary>
    public sealed class OptimisationCatalogue
    {
        public OptimisationCatalogue(IEnumerable<OptimisationCatalogueEntry> variables, IEnumerable<OptimisationCatalogueEntry> outputs, string source = null)
        {
            Variables = (variables ?? Enumerable.Empty<OptimisationCatalogueEntry>()).Where(x => x != null && !string.IsNullOrWhiteSpace(x.Name)).ToList().AsReadOnly();
            Outputs = (outputs ?? Enumerable.Empty<OptimisationCatalogueEntry>()).Where(x => x != null && !string.IsNullOrWhiteSpace(x.Name)).ToList().AsReadOnly();
            Source = source;
        }

        public IReadOnlyList<OptimisationCatalogueEntry> Variables { get; }

        public IReadOnlyList<OptimisationCatalogueEntry> Outputs { get; }

        /// <summary>Where the names come from, in words, for example "found in the Tas script text". Optional.</summary>
        public string Source { get; }

        /// <summary>True when the catalogue lists no variable and no output.</summary>
        public bool IsEmpty => Variables.Count == 0 && Outputs.Count == 0;

        /// <summary>
        /// True when the catalogue lists model items (any variable entry with a target or output entry with a measure).
        /// Then it is the complete list of what the model offers, and a binding to anything else is reported (OPT609).
        /// </summary>
        public bool HasBindings => Variables.Any(x => x.Target != null) || Outputs.Any(x => x.Measure != null);
    }
}
