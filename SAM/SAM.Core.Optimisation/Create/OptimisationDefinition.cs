// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Collections.Generic;

namespace SAM.Core.Optimisation
{
    public static partial class Create
    {
        /// <summary>
        /// Reads an optimisation definition from its text and validates it.
        /// <para>
        /// The text must be one JSON object of schema <see cref="OptimisationDefinition.Schema"/>. Reading is strict: an
        /// unknown or repeated property, a value of the wrong type (including a number written as text), a missing
        /// required property, an unknown enum value or a newer schema version is an error (OPT1xx) and the result is
        /// null. Comments and trailing commas are tolerated with a warning (comments are not kept). Enum values that
        /// differ only in case or separators are accepted and normalised.
        /// </para>
        /// <para>
        /// A definition that can be read is returned even when it is not valid: <paramref name="diagnostics"/> then also
        /// holds the meaning, unit and method findings (OPT2xx–OPT4xx, see <see cref="Query.Diagnostics"/>), each with
        /// its line and column in <paramref name="text"/>.
        /// </para>
        /// </summary>
        /// <param name="text">The definition text.</param>
        /// <param name="diagnostics">Every finding, errors first, then in text order.</param>
        /// <param name="capabilities">The engine's capabilities; null skips the capability checks.</param>
        /// <param name="extract">
        /// True for pasted text (for example an AI reply): the first JSON object is taken from it, and any Markdown code
        /// fence or other text around it is dropped with a warning (OPT115).
        /// </param>
        /// <returns>The definition, or null when the text cannot be read (OPT1xx error).</returns>
        public static OptimisationDefinition OptimisationDefinition(string text, out List<OptimisationDiagnostic> diagnostics, IOptimisationCapabilities capabilities = null, bool extract = false)
        {
            diagnostics = new List<OptimisationDiagnostic>();

            string value = text ?? string.Empty;
            if (value.Length != 0 && value[0] == '﻿')
            {
                value = value.Substring(1);
            }

            if (extract)
            {
                string extracted = Query.ExtractJsonObject(value, out bool trimmed);
                if (extracted == null)
                {
                    diagnostics.Add(new OptimisationDiagnostic(DiagnosticSeverity.Error, "OPT101", "$", "No JSON object was found in the pasted text.", "The reply must be one JSON object that starts with { and ends with }."));
                    return null;
                }

                if (trimmed)
                {
                    diagnostics.Add(new OptimisationDiagnostic(DiagnosticSeverity.Warning, "OPT115", "$", "Text around the JSON object (such as a Markdown code fence or an explanation) was removed.", "Only the JSON object is used; line numbers refer to it."));
                }

                value = extracted;
            }

            OptimisationJsonNode optimisationJsonNode = OptimisationJsonNode.Parse(value, diagnostics, out bool comments);
            if (optimisationJsonNode == null)
            {
                return null;
            }

            if (comments)
            {
                diagnostics.Add(new OptimisationDiagnostic(DiagnosticSeverity.Warning, "OPT114", "$", "Comments in the definition text are ignored and are not kept.", "Put explanations in a \"description\" or \"notes\" field instead.", 1, 1));
            }

            OptimisationDefinitionReader optimisationDefinitionReader = new OptimisationDefinitionReader(diagnostics);
            Optimisation.OptimisationDefinition result = optimisationDefinitionReader.Read(optimisationJsonNode);
            if (result == null || diagnostics.Exists(x => x.Severity == DiagnosticSeverity.Error))
            {
                Sort(diagnostics);
                return null;
            }

            foreach (OptimisationDiagnostic optimisationDiagnostic in result.Diagnostics(capabilities))
            {
                diagnostics.Add(optimisationDefinitionReader.Locate(optimisationDiagnostic));
            }

            Sort(diagnostics);
            return result;
        }

        /// <summary>Errors first, then warnings, then information; each in text order (stable).</summary>
        private static void Sort(List<OptimisationDiagnostic> diagnostics)
        {
            List<OptimisationDiagnostic> sorted = new List<OptimisationDiagnostic>(diagnostics);
            sorted.Sort((x, y) =>
            {
                int result = y.Severity.CompareTo(x.Severity);
                if (result != 0)
                {
                    return result;
                }

                result = (x.Line ?? int.MaxValue).CompareTo(y.Line ?? int.MaxValue);
                if (result != 0)
                {
                    return result;
                }

                result = (x.Column ?? int.MaxValue).CompareTo(y.Column ?? int.MaxValue);
                return result != 0 ? result : diagnostics.IndexOf(x).CompareTo(diagnostics.IndexOf(y));
            });

            diagnostics.Clear();
            diagnostics.AddRange(sorted);
        }
    }
}
