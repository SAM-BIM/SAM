// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace SAM.Core
{
    /// <summary>
    /// Ordered collection of <see cref="Diagnostic"/> records for one run, collected at a requested
    /// <see cref="DiagnosticDetail"/> (default <see cref="DiagnosticDetail.Summary"/>).
    /// Info records above the requested level are dropped on <c>Add</c> (so <see cref="Filter"/> can only narrow, never widen; collect at <see cref="DiagnosticDetail.Detailed"/> to re-filter later); Warnings and Errors are always kept.
    /// Runtime value only: not serialised, independent of <see cref="Log"/>.
    /// </summary>
    public class Diagnostics : IEnumerable<Diagnostic>
    {
        private readonly List<Diagnostic> diagnostics = new List<Diagnostic>();

        public Diagnostics(DiagnosticDetail detail = DiagnosticDetail.Summary)
        {
            Detail = detail;
        }

        /// <summary>Requested detail level.</summary>
        public DiagnosticDetail Detail { get; }

        public int Count => diagnostics.Count;

        /// <summary>True when any Error record is present.</summary>
        public bool HasErrors => diagnostics.Any(x => x.Severity == DiagnosticSeverity.Error);

        /// <summary>True when an Info record of the given detail would be kept. Use to skip building expensive messages.</summary>
        public bool Wants(DiagnosticDetail detail)
        {
            return detail <= Detail;
        }

        /// <summary>Adds a record. An Info record above <see cref="Detail"/> is dropped; returns whether it was kept.</summary>
        public bool Add(Diagnostic diagnostic)
        {
            if (diagnostic == null)
            {
                return false;
            }

            if (diagnostic.Severity == DiagnosticSeverity.Info && !Wants(diagnostic.Detail))
            {
                return false;
            }

            diagnostics.Add(diagnostic);
            return true;
        }

        /// <summary>Adds a record whose message is built only if it is kept (avoids allocations at Summary level).</summary>
        public bool Add(DiagnosticSeverity severity, DiagnosticDetail detail, string subject, Func<string> message, string code = null, int? sourceIndex = null, double? value = null, double? limit = null, string unit = null)
        {
            if (severity == DiagnosticSeverity.Info && !Wants(detail))
            {
                return false;
            }

            return Add(new Diagnostic(severity, detail, subject, message?.Invoke(), code, sourceIndex, value, limit, unit));
        }

        /// <summary>Records at the given level: Info up to and including it, plus every Warning and Error. Order is preserved.</summary>
        public List<Diagnostic> Filter(DiagnosticDetail detail)
        {
            return diagnostics.FindAll(x => x.Severity != DiagnosticSeverity.Info || x.Detail <= detail);
        }

        /// <summary>The records as lines in the <see cref="Query.DiagnosticMessage"/> format, in insertion order.</summary>
        public List<string> Format()
        {
            return diagnostics.ConvertAll(x => x.ToString());
        }

        public IEnumerator<Diagnostic> GetEnumerator()
        {
            return diagnostics.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
