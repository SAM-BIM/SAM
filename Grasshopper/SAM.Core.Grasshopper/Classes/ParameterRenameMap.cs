// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using Grasshopper.Kernel;
using System;
using System.Collections.Generic;

namespace SAM.Core.Grasshopper
{
    /// <summary>
    /// <b>A component's declared <see cref="ParameterRename"/>s, validated and ready to resolve an old name
    /// to exactly one current name — or to refuse, when the declaration is ambiguous.</b>
    /// <para>
    /// The map is built from the component that will receive the values and connections, which is the one
    /// that declares the current names (the replacement produced by <c>Modify.DuplicateComponent</c>). The
    /// resolution is deliberately conservative: a name that no rename mentions is returned unchanged, a
    /// legacy name that exactly one rename covers is redirected to its canonical name, and a legacy name
    /// covered by more than one rename — or by a declaration that is otherwise malformed — is refused and
    /// reported rather than guessed. Guessing would attach a person's wire or value to the wrong parameter.
    /// </para>
    /// </summary>
    public sealed class ParameterRenameMap
    {
        private readonly Dictionary<GH_ParameterSide, Dictionary<string, string>> map = new Dictionary<GH_ParameterSide, Dictionary<string, string>>();
        private readonly Dictionary<GH_ParameterSide, Dictionary<string, string>> conflicts = new Dictionary<GH_ParameterSide, Dictionary<string, string>>();
        private readonly List<string> problems = new List<string>();

        private ParameterRenameMap()
        {
        }

        /// <summary>A map with no renames: every name resolves to itself.</summary>
        public static ParameterRenameMap Empty { get; } = new ParameterRenameMap();

        /// <summary>
        /// Reads and validates the renames a component declares. A component that throws from
        /// <see cref="GH_SAMComponent.ParameterRenames"/> yields an empty map plus a problem, so a broken
        /// declaration can never stop a component that is otherwise perfectly updatable.
        /// </summary>
        public static ParameterRenameMap Create(GH_SAMComponent gH_SAMComponent)
        {
            if (gH_SAMComponent == null)
            {
                return Empty;
            }

            IReadOnlyList<ParameterRename> parameterRenames;
            try
            {
                parameterRenames = gH_SAMComponent.ParameterRenames;
            }
            catch (Exception ex)
            {
                ParameterRenameMap result_Exception = new ParameterRenameMap();
                result_Exception.problems.Add(string.Format("{0}: ParameterRenames could not be read ({1}); no legacy names will be migrated.", gH_SAMComponent.Name, ex.Message));
                return result_Exception;
            }

            return Create(parameterRenames);
        }

        /// <summary>Validates a raw declaration list.</summary>
        public static ParameterRenameMap Create(IReadOnlyList<ParameterRename> parameterRenames)
        {
            ParameterRenameMap result = new ParameterRenameMap();

            if (parameterRenames == null || parameterRenames.Count == 0)
            {
                return result;
            }

            List<ParameterRename> entries = new List<ParameterRename>();
            foreach (ParameterRename parameterRename in parameterRenames)
            {
                if (parameterRename == null)
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(parameterRename.LegacyName) || string.IsNullOrWhiteSpace(parameterRename.Name))
                {
                    result.problems.Add(string.Format("A rename with an empty name was ignored ({0} '{1}' → '{2}').", parameterRename.Side, parameterRename.LegacyName, parameterRename.Name));
                    continue;
                }

                if (string.Equals(parameterRename.LegacyName, parameterRename.Name, StringComparison.Ordinal))
                {
                    result.problems.Add(string.Format("A rename of {0} '{1}' to the same name was ignored.", parameterRename.Side, parameterRename.LegacyName));
                    continue;
                }

                entries.Add(parameterRename);
            }

            HashSet<ParameterRename> rejected = new HashSet<ParameterRename>();

            // A legacy name must resolve to exactly one canonical name. Two renames that claim the same
            // legacy name are ambiguous and are refused in full - never resolved to an arbitrary one.
            Dictionary<string, List<ParameterRename>> byLegacy = new Dictionary<string, List<ParameterRename>>();
            foreach (ParameterRename entry in entries)
            {
                string key = Key(entry.Side, entry.LegacyName);
                if (!byLegacy.TryGetValue(key, out List<ParameterRename> list))
                {
                    list = new List<ParameterRename>();
                    byLegacy[key] = list;
                }

                list.Add(entry);
            }

            foreach (KeyValuePair<string, List<ParameterRename>> keyValuePair in byLegacy)
            {
                if (keyValuePair.Value.Count <= 1)
                {
                    continue;
                }

                List<string> targets = new List<string>();
                foreach (ParameterRename entry in keyValuePair.Value)
                {
                    rejected.Add(entry);
                    targets.Add(entry.Name);
                }

                ParameterRename first = keyValuePair.Value[0];
                string duplicateProblem = string.Format("Conflicting renames for {0} '{1}' → {2}; no legacy name will be migrated for it.",
                    first.Side, first.LegacyName, string.Join(", ", targets));
                result.problems.Add(duplicateProblem);
                result.ConflictMap(first.Side)[first.LegacyName] = duplicateProblem;
            }

            // A rename whose legacy name is itself the canonical name of another rename (a chain) has no
            // single answer for an older definition, so it is refused as well.
            foreach (ParameterRename entry in entries)
            {
                if (rejected.Contains(entry))
                {
                    continue;
                }

                foreach (ParameterRename other in entries)
                {
                    if (other == entry || other.Side != entry.Side || rejected.Contains(other))
                    {
                        continue;
                    }

                    if (string.Equals(other.Name, entry.LegacyName, StringComparison.Ordinal))
                    {
                        rejected.Add(entry);
                        string chainProblem = string.Format("Chained rename {0} '{1}' → '{2}' collides with another rename's target; it was refused.",
                            entry.Side, entry.LegacyName, entry.Name);
                        result.problems.Add(chainProblem);
                        result.ConflictMap(entry.Side)[entry.LegacyName] = chainProblem;
                        break;
                    }
                }
            }

            foreach (ParameterRename entry in entries)
            {
                if (rejected.Contains(entry))
                {
                    continue;
                }

                Dictionary<string, string> sideMap = result.SideMap(entry.Side);
                if (sideMap.ContainsKey(entry.LegacyName))
                {
                    // Should not happen after the duplicate check above, but a duplicated entry with the
                    // SAME target is harmless and is collapsed.
                    continue;
                }

                sideMap[entry.LegacyName] = entry.Name;
            }

            return result;
        }

        /// <summary>Declaration problems found while building the map, in a stable order.</summary>
        public IReadOnlyList<string> Problems => problems;

        /// <summary>True when at least one legacy name can be redirected.</summary>
        public bool HasRenames
        {
            get
            {
                foreach (Dictionary<string, string> sideMap in map.Values)
                {
                    if (sideMap.Count != 0)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        /// <summary>
        /// Resolves a published parameter name. Returns <c>true</c> and the name to use (the current name,
        /// or the unchanged input when no rename covers it) for a normal or redirected name. Returns
        /// <c>false</c> with a reason when the declaration is ambiguous, in which case the caller must not
        /// redirect anything.
        /// </summary>
        public bool TryResolve(GH_ParameterSide side, string name, out string target, out string problem)
        {
            target = name;
            problem = null;

            if (string.IsNullOrEmpty(name))
            {
                return true;
            }

            if (conflicts.TryGetValue(side, out Dictionary<string, string> sideConflicts) && sideConflicts.TryGetValue(name, out string conflict))
            {
                problem = conflict;
                return false;
            }

            if (map.TryGetValue(side, out Dictionary<string, string> sideMap) && sideMap.TryGetValue(name, out string canonical))
            {
                target = canonical;
            }

            return true;
        }

        private Dictionary<string, string> SideMap(GH_ParameterSide side)
        {
            if (!map.TryGetValue(side, out Dictionary<string, string> result))
            {
                result = new Dictionary<string, string>(StringComparer.Ordinal);
                map[side] = result;
            }

            return result;
        }

        private Dictionary<string, string> ConflictMap(GH_ParameterSide side)
        {
            if (!conflicts.TryGetValue(side, out Dictionary<string, string> result))
            {
                result = new Dictionary<string, string>(StringComparer.Ordinal);
                conflicts[side] = result;
            }

            return result;
        }

        private static string Key(GH_ParameterSide side, string name)
        {
            return ((int)side).ToString() + "|" + name;
        }
    }
}
