// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

namespace SAM.Core.Optimisation
{
    /// <summary>One key of a binding's reference, as an engine defines it, for example "internalCondition".</summary>
    public sealed class OptimisationReferenceKey
    {
        public OptimisationReferenceKey(string name, string displayName = null, bool required = true)
        {
            Name = name;
            DisplayName = displayName;
            Required = required;
        }

        /// <summary>The key written in the reference, for example "plantRoom".</summary>
        public string Name { get; }

        /// <summary>The model item in words, for messages, for example "plant room". Optional.</summary>
        public string DisplayName { get; }

        /// <summary>True when every binding of the kind must give this key.</summary>
        public bool Required { get; }
    }
}
