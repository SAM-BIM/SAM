// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

namespace SAM.Core
{
    /// <summary>
    /// One immutable diagnostic record. Runtime value only (not serialised); see <see cref="Diagnostics"/>.
    /// </summary>
    public sealed class Diagnostic
    {
        /// <summary>Structured severity.</summary>
        public DiagnosticSeverity Severity { get; }

        /// <summary>Lowest <see cref="DiagnosticDetail"/> at which an Info record is shown. Ignored for Warning and Error.</summary>
        public DiagnosticDetail Detail { get; }

        /// <summary>Optional stable machine-readable code (e.g. <c>PBS001</c>); null when not assigned.</summary>
        public string Code { get; }

        /// <summary>Source the record is about (e.g. <c>Solid #3 'Name'</c>); null or empty for summaries.</summary>
        public string Subject { get; }

        /// <summary>Index of the source in its input list; null when not applicable.</summary>
        public int? SourceIndex { get; }

        /// <summary>Message: full sentences, units on every value.</summary>
        public string Message { get; }

        /// <summary>Optional measured/computed engineering value; null when absent.</summary>
        public double? Value { get; }

        /// <summary>Optional limit or threshold the <see cref="Value"/> was compared with; null when absent.</summary>
        public double? Limit { get; }

        /// <summary>Optional unit of <see cref="Value"/> and <see cref="Limit"/> (e.g. <c>m</c>); null when absent.</summary>
        public string Unit { get; }

        public Diagnostic(DiagnosticSeverity severity, DiagnosticDetail detail, string subject, string message, string code = null, int? sourceIndex = null, double? value = null, double? limit = null, string unit = null)
        {
            Severity = severity;
            Detail = detail;
            Subject = subject;
            Message = message ?? string.Empty;
            Code = code;
            SourceIndex = sourceIndex;
            Value = value;
            Limit = limit;
            Unit = unit;
        }

        /// <summary>The record as one line, in the <see cref="Query.DiagnosticMessage"/> format.</summary>
        public override string ToString()
        {
            LogRecordType logRecordType = Severity == DiagnosticSeverity.Error ? LogRecordType.Error : Severity == DiagnosticSeverity.Warning ? LogRecordType.Warning : LogRecordType.Message;
            return Query.DiagnosticMessage(logRecordType, Subject, Message);
        }
    }
}
