// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

namespace SAM.Core
{
    public static partial class Query
    {
        /// <summary>
        /// One line of a SAM Diagnostics output: <c>Severity: Subject: Message</c>, e.g.
        /// <c>Warning: Solid #18 'GF_Slab_Ground': Skipped. Its thickness, 0.2 m, is below minThickness (0.25 m).</c>
        /// <list type="bullet">
        /// <item>Severity is <c>Info</c>, <c>Warning</c> or <c>Error</c> (<see cref="LogRecordType.Message"/> and
        /// <see cref="LogRecordType.Undefined"/> are written as <c>Info</c>). Info is what was done, Warning is input that
        /// was skipped or changed, Error is a run that could not complete.</item>
        /// <item>Subject identifies the source deterministically (e.g. <c>Solid #3 'Name'</c>); it is omitted for
        /// component summaries.</item>
        /// <item>Message is one or more full sentences starting with a capital letter, with units on every value.</item>
        /// </list>
        /// </summary>
        /// <param name="logRecordType">Severity.</param>
        /// <param name="subject">Source the line is about (optional).</param>
        /// <param name="message">Message; its first letter is capitalised.</param>
        /// <returns>The formatted line.</returns>
        public static string DiagnosticMessage(LogRecordType logRecordType, string subject, string message)
        {
            string severity = logRecordType == LogRecordType.Warning ? "Warning" : logRecordType == LogRecordType.Error ? "Error" : "Info";

            message = message?.Trim() ?? string.Empty;
            if (message.Length != 0 && char.IsLower(message[0]))
            {
                message = char.ToUpperInvariant(message[0]) + message.Substring(1);
            }

            return string.IsNullOrWhiteSpace(subject) ? string.Format("{0}: {1}", severity, message) : string.Format("{0}: {1}: {2}", severity, subject, message);
        }
    }
}
