// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Globalization;
using System.Text;

namespace SAM.Core.Optimisation
{
    /// <summary>
    /// One finding about an optimisation definition, worded for an engineer, with a stable code and the place it
    /// refers to. The same diagnostics serve the visual form (by <see cref="Path"/>), the definition text editor (by
    /// <see cref="Line"/> and <see cref="Column"/>) and an AI assistant asked to fix the definition (by <see cref="Code"/>).
    /// <para>
    /// Codes: OPT1xx text and structure (the definition cannot be read), OPT2xx meaning (references, ranges), OPT3xx
    /// units, OPT4xx search method and engine capability; OPT5xx is reserved for execution checks made by an engine.
    /// </para>
    /// </summary>
    public sealed class OptimisationDiagnostic
    {
        public OptimisationDiagnostic(DiagnosticSeverity severity, string code, string path, string message, string hint = null, int? line = null, int? column = null)
        {
            Severity = severity;
            Code = code;
            Path = path;
            Message = message;
            Hint = hint;
            Line = line;
            Column = column;
        }

        public OptimisationDiagnostic(OptimisationDiagnostic optimisationDiagnostic, int? line, int? column)
            : this(optimisationDiagnostic.Severity, optimisationDiagnostic.Code, optimisationDiagnostic.Path, optimisationDiagnostic.Message, optimisationDiagnostic.Hint, line, column)
        {
        }

        public DiagnosticSeverity Severity { get; }

        /// <summary>A stable code such as "OPT203".</summary>
        public string Code { get; }

        /// <summary>Where in the definition, as a JSON path such as "$.variables[0].minimum"; "$" for the whole definition.</summary>
        public string Path { get; }

        /// <summary>One-based line in the definition text, when the diagnostic came from text.</summary>
        public int? Line { get; }

        /// <summary>One-based column in the definition text, when the diagnostic came from text.</summary>
        public int? Column { get; }

        /// <summary>What is wrong, in engineering terms.</summary>
        public string Message { get; }

        /// <summary>How to fix it, when there is a useful suggestion; otherwise null.</summary>
        public string Hint { get; }

        /// <summary>"Error OPT203 at $.variables[0] (line 9, column 7): message Hint"</summary>
        public override string ToString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append(Severity).Append(' ').Append(Code);
            if (!string.IsNullOrEmpty(Path))
            {
                stringBuilder.Append(" at ").Append(Path);
            }

            if (Line != null)
            {
                stringBuilder.Append(string.Format(CultureInfo.InvariantCulture, " (line {0}, column {1})", Line, Column ?? 1));
            }

            stringBuilder.Append(": ").Append(Message);
            if (!string.IsNullOrEmpty(Hint))
            {
                stringBuilder.Append(' ').Append(Hint);
            }

            return stringBuilder.ToString();
        }
    }
}
