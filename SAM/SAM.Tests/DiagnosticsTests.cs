// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Linq;
using SAM.Core;
using Xunit;

namespace SAM.Tests
{
    /// <summary>
    /// <see cref="Diagnostic"/> and <see cref="Core.Diagnostics"/>: structured severity, detail filtering and formatting.
    /// </summary>
    public class DiagnosticsTests
    {
        private static Core.Diagnostics Sample(DiagnosticDetail detail)
        {
            Core.Diagnostics diagnostics = new Core.Diagnostics(detail);
            diagnostics.Add(new Diagnostic(DiagnosticSeverity.Info, DiagnosticDetail.Summary, null, "created 3 panels."));
            diagnostics.Add(new Diagnostic(DiagnosticSeverity.Info, DiagnosticDetail.Standard, "Solid #1 'A'", "Converted."));
            diagnostics.Add(new Diagnostic(DiagnosticSeverity.Info, DiagnosticDetail.Detailed, "Solid #1 'A'", "Thickness 0.3 m.", value: 0.3, unit: "m"));
            diagnostics.Add(new Diagnostic(DiagnosticSeverity.Warning, DiagnosticDetail.Detailed, "Solid #2 'B'", "Skipped."));
            diagnostics.Add(new Diagnostic(DiagnosticSeverity.Error, DiagnosticDetail.Detailed, null, "Failed."));
            return diagnostics;
        }

        [Fact]
        public void EnumValues_AreStable()
        {
            Assert.Equal(0, (int)DiagnosticSeverity.Info);
            Assert.Equal(1, (int)DiagnosticSeverity.Warning);
            Assert.Equal(2, (int)DiagnosticSeverity.Error);
            Assert.Equal(0, (int)DiagnosticDetail.Summary);
            Assert.Equal(1, (int)DiagnosticDetail.Standard);
            Assert.Equal(2, (int)DiagnosticDetail.Detailed);
        }

        [Fact]
        public void DefaultDetail_IsSummary()
        {
            Assert.Equal(DiagnosticDetail.Summary, new Core.Diagnostics().Detail);
            Assert.Equal(DiagnosticDetail.Summary, default(DiagnosticDetail));
        }

        [Theory]
        [InlineData(DiagnosticDetail.Summary, 3)]
        [InlineData(DiagnosticDetail.Standard, 4)]
        [InlineData(DiagnosticDetail.Detailed, 5)]
        public void Add_KeepsInfoUpToTheRequestedLevel_AndAlwaysWarningsAndErrors(DiagnosticDetail detail, int expected)
        {
            Core.Diagnostics diagnostics = Sample(detail);
            Assert.Equal(expected, diagnostics.Count);
            Assert.Equal(1, diagnostics.Count(x => x.Severity == DiagnosticSeverity.Warning));
            Assert.Equal(1, diagnostics.Count(x => x.Severity == DiagnosticSeverity.Error));
            Assert.True(diagnostics.HasErrors);
        }

        [Theory]
        [InlineData(DiagnosticDetail.Summary, 3)]
        [InlineData(DiagnosticDetail.Standard, 4)]
        [InlineData(DiagnosticDetail.Detailed, 5)]
        public void Filter_IncludesLowerLevels_AndNeverDropsWarningsOrErrors(DiagnosticDetail level, int expected)
        {
            Core.Diagnostics diagnostics = Sample(DiagnosticDetail.Detailed);
            var filtered = diagnostics.Filter(level);
            Assert.Equal(expected, filtered.Count);
            Assert.Contains(filtered, x => x.Severity == DiagnosticSeverity.Warning);
            Assert.Contains(filtered, x => x.Severity == DiagnosticSeverity.Error);
        }

        [Fact]
        public void Wants_ComparesAgainstTheRequestedLevel()
        {
            Core.Diagnostics diagnostics = new Core.Diagnostics(DiagnosticDetail.Standard);
            Assert.True(diagnostics.Wants(DiagnosticDetail.Summary));
            Assert.True(diagnostics.Wants(DiagnosticDetail.Standard));
            Assert.False(diagnostics.Wants(DiagnosticDetail.Detailed));
        }

        [Fact]
        public void LazyAdd_DoesNotBuildTheMessageForDroppedInfo_ButBuildsWarnings()
        {
            Core.Diagnostics diagnostics = new Core.Diagnostics();
            int calls = 0;
            Assert.False(diagnostics.Add(DiagnosticSeverity.Info, DiagnosticDetail.Detailed, "S", () => { calls++; return "x."; }));
            Assert.Equal(0, calls);
            Assert.True(diagnostics.Add(DiagnosticSeverity.Warning, DiagnosticDetail.Detailed, "S", () => { calls++; return "x."; }));
            Assert.Equal(1, calls);
        }

        [Fact]
        public void HasErrors_IsFalseForInfoAndWarning_AndSeverityIsNotInferredFromText()
        {
            Core.Diagnostics diagnostics = new Core.Diagnostics();
            diagnostics.Add(new Diagnostic(DiagnosticSeverity.Info, DiagnosticDetail.Summary, null, "Error: looks like an error but is info."));
            diagnostics.Add(new Diagnostic(DiagnosticSeverity.Warning, DiagnosticDetail.Summary, null, "Careful."));
            Assert.False(diagnostics.HasErrors);
        }

        [Fact]
        public void Format_IsDeterministic_AndMatchesDiagnosticMessage()
        {
            var lines = Sample(DiagnosticDetail.Detailed).Format();
            Assert.Equal(new[]
            {
                "Info: Created 3 panels.",
                "Info: Solid #1 'A': Converted.",
                "Info: Solid #1 'A': Thickness 0.3 m.",
                "Warning: Solid #2 'B': Skipped.",
                "Error: Failed."
            }, lines);
            Assert.Equal(lines, Sample(DiagnosticDetail.Detailed).Format());
            Assert.Equal(Core.Query.DiagnosticMessage(LogRecordType.Warning, "Solid #2 'B'", "Skipped."), lines[3]);
        }

        [Fact]
        public void OptionalFields_DefaultToNull_AndNullMessageIsEmpty()
        {
            Diagnostic diagnostic = new Diagnostic(DiagnosticSeverity.Info, DiagnosticDetail.Summary, null, null);
            Assert.Null(diagnostic.Code);
            Assert.Null(diagnostic.Subject);
            Assert.Null(diagnostic.SourceIndex);
            Assert.Null(diagnostic.Value);
            Assert.Null(diagnostic.Limit);
            Assert.Null(diagnostic.Unit);
            Assert.Equal(string.Empty, diagnostic.Message);
            Assert.Equal("Info: ", diagnostic.ToString());

            Diagnostic full = new Diagnostic(DiagnosticSeverity.Warning, DiagnosticDetail.Standard, "S", "m.", "X1", 4, 0.2, 0.25, "m");
            Assert.Equal("X1", full.Code);
            Assert.Equal(4, full.SourceIndex);
            Assert.Equal(0.2, full.Value);
            Assert.Equal(0.25, full.Limit);
            Assert.Equal("m", full.Unit);
        }

        [Fact]
        public void NullDiagnostic_IsIgnored()
        {
            Core.Diagnostics diagnostics = new Core.Diagnostics();
            Assert.False(diagnostics.Add((Diagnostic)null));
            Assert.Equal(0, diagnostics.Count);
        }

        [Fact]
        public void ExistingLog_IsUnaffected()
        {
            Log log = new Log();
            log.Add("hello");
            Assert.Single(log);
        }
    }
}
