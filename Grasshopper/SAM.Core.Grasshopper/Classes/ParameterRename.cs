// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using Grasshopper.Kernel;
using System;

namespace SAM.Core.Grasshopper
{
    /// <summary>
    /// <b>One published parameter rename: the name a component used to declare, and the name it declares now.</b>
    /// <para>
    /// A Grasshopper definition stores a component's parameters in its own archive and restores them from
    /// there, so a component reopened from an older file keeps the names the FILE has, not the names its
    /// class now declares. Renaming a published parameter is therefore a breaking change for every existing
    /// definition: <c>Params.IndexOfInputParam</c>/<c>IndexOfOutputParam</c> stop resolving, wires and
    /// typed-in values are matched by name and are lost, and the SAM updater would report them dropped.
    /// </para>
    /// <para>
    /// A component declares its renames through <see cref="GH_SAMComponent.ParameterRenames"/> so the shared
    /// updater (<c>Modify.DuplicateComponent</c>) can carry the old name to the new parameter, and so its own
    /// <c>SolveInstance</c> can fall back to the old name on a definition that has not been updated yet.
    /// </para>
    /// </summary>
    public sealed class ParameterRename
    {
        /// <param name="side">Whether the renamed parameter is an input or an output.</param>
        /// <param name="legacyName">The published name before the rename — the name an older definition still carries.</param>
        /// <param name="name">The current, canonical published name the component now declares.</param>
        public ParameterRename(GH_ParameterSide side, string legacyName, string name)
        {
            Side = side;
            LegacyName = legacyName;
            Name = name;
        }

        /// <summary>The side the rename applies to.</summary>
        public GH_ParameterSide Side { get; }

        /// <summary>The published name before the rename.</summary>
        public string LegacyName { get; }

        /// <summary>The current, canonical published name.</summary>
        public string Name { get; }

        public override string ToString()
        {
            return string.Format("{0} {1} → {2}", Side == GH_ParameterSide.Input ? "input" : "output", LegacyName, Name);
        }
    }
}
