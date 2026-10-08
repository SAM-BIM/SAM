// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

namespace SAM.Core.Optimisation
{
    /// <summary>The kind of a <see cref="OptimisationJsonNode"/>.</summary>
    internal enum OptimisationJsonKind
    {
        Object,
        Array,
        String,
        Number,
        Boolean,
        Null,
    }
}
