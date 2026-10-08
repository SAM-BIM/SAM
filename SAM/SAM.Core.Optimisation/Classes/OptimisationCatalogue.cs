// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Collections.Generic;
using System.Linq;

namespace SAM.Core.Optimisation
{
    /// <summary>
    /// The design variables and outputs a model makes available, so that a person or an AI assistant chooses from
    /// real names instead of inventing them. An engine fills it from what it can discover (for a Tas script, the names
    /// the script text reads and writes); later sources may add descriptions, quantities and units.
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
    }
}
