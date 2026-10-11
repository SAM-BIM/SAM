// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

namespace SAM.Core.Optimisation
{
    /// <summary>A property of a JSON object with the place its name starts.</summary>
    internal sealed class OptimisationJsonProperty
    {
        public OptimisationJsonProperty(string name, int line, int column, OptimisationJsonNode value)
        {
            Name = name;
            Line = line;
            Column = column;
            Value = value;
        }

        public string Name { get; }

        public int Line { get; }

        public int Column { get; }

        public OptimisationJsonNode Value { get; }
    }
}
