// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical;
using SAM.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using Xunit;

namespace SAM.Tests
{
    /// <summary>
    /// Phase 2 of the unified Diagnostics: <see cref="Analytical.Create.PanelsBySolid(Geometry.Spatial.Shell, Core.Diagnostics, IEnumerable{Geometry.Spatial.ISAMGeometry3D}, double, double, bool, string, string, string, double, double, double, int?)"/>,
    /// <see cref="Analytical.Query.SnapToJunctions(IEnumerable{Panel}, Core.Diagnostics, double, double)"/> and
    /// <see cref="Analytical.Create.AperturesBySolids(IEnumerable{ApertureSolid}, Core.Diagnostics, out List{int}, IEnumerable{ApertureConstruction}, double, double, double, double)"/>.
    /// <para>
    /// The reference is <c>Golden/PanelsBySolid/bysolids_baseline_q4.json</c>, captured from the legacy
    /// <c>out List&lt;string&gt;</c> API before the migration (base <c>45f495e3</c>): the contemporary house at four
    /// settings, synthetic solids covering every skip reason, and house and synthetic apertures. It holds each scenario's
    /// log lines and an engineering fingerprint (types, areas, thicknesses, orientation, extents, source stamps, hosting).
    /// </para>
    /// </summary>
    public class BySolidsDiagnosticsTests
    {
        private static readonly string[] PanelScenarios = { "house-default", "house-ratio-0.25", "house-min-0.25", "house-min-0.05-ratio-0.3", "synthetic-solids" };

        private static readonly Lazy<JsonObject> Golden = new Lazy<JsonObject>(() =>
        {
            string path = Path.Combine(AppContext.BaseDirectory, "Golden", "PanelsBySolid", "bysolids_baseline_q4.json");
            return (JsonObject)((JsonObject)JsonNode.Parse(File.ReadAllText(path)))["Scenarios"];
        });

        private static List<string> GoldenLines(string scenario, string key)
        {
            return ((JsonArray)Golden.Value[scenario][key]).Select(x => (string)x).ToList();
        }

        private static List<PanelsBySolidComplexHouseTests.SourceSolid> Solids(string scenario)
        {
            return scenario == "synthetic-solids" ? BySolidsScenarios.SyntheticSolids() : PanelsBySolidComplexHouseTests.Load("Solids");
        }

        private static BySolidsScenarios.Settings SettingsOf(string scenario)
        {
            return BySolidsScenarios.HouseSettings.FirstOrDefault(x => x.Name == scenario) ?? new BySolidsScenarios.Settings { Name = scenario, MinThickness = 0.08, MaxThicknessRatio = 0.2 };
        }

        private static List<ApertureSolid> ApertureSolidsOf(string scenario)
        {
            return scenario == "synthetic-solids" ? null : BySolidsScenarios.HouseApertureSolids();
        }

        // The runs are deterministic and the results are only read, so each (scenario, level) is computed once per test process.
        // The determinism test below does not use the cache.
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, Lazy<(BySolidsScenarios.Result, Core.Diagnostics)>> Runs = new System.Collections.Concurrent.ConcurrentDictionary<string, Lazy<(BySolidsScenarios.Result, Core.Diagnostics)>>();

        private static (BySolidsScenarios.Result Result, Core.Diagnostics Diagnostics) RunPanelsUncached(string scenario, DiagnosticDetail detail)
        {
            Core.Diagnostics diagnostics = new Core.Diagnostics(detail);
            BySolidsScenarios.Result result = BySolidsScenarios.RunPanels(BySolidsScenarios.Structured(diagnostics), Solids(scenario), ApertureSolidsOf(scenario), SettingsOf(scenario));
            return (result, diagnostics);
        }

        private static (BySolidsScenarios.Result Result, Core.Diagnostics Diagnostics) RunPanels(string scenario, DiagnosticDetail detail)
        {
            return Runs.GetOrAdd(scenario + "|" + detail, _ => new Lazy<(BySolidsScenarios.Result, Core.Diagnostics)>(() => RunPanelsUncached(scenario, detail))).Value;
        }

        private static (BySolidsScenarios.Result Result, Core.Diagnostics Diagnostics) RunApertures(string scenario, DiagnosticDetail detail)
        {
            return Runs.GetOrAdd(scenario + "|" + detail, _ => new Lazy<(BySolidsScenarios.Result, Core.Diagnostics)>(() =>
            {
                Core.Diagnostics diagnostics = new Core.Diagnostics(detail);
                List<ApertureSolid> solids = scenario == "apertures-house" ? BySolidsScenarios.HouseApertureSolids() : BySolidsScenarios.SyntheticApertureSolids();
                return (BySolidsScenarios.RunApertures(BySolidsScenarios.Structured(diagnostics), solids), diagnostics);
            })).Value;
        }

        private static readonly Lazy<Dictionary<string, BySolidsScenarios.Result>> LegacyRuns = new Lazy<Dictionary<string, BySolidsScenarios.Result>>(() =>
            PanelScenarios.ToDictionary(x => x, x => BySolidsScenarios.RunPanels(BySolidsScenarios.Legacy, Solids(x), ApertureSolidsOf(x), SettingsOf(x))));

        public static IEnumerable<object[]> PanelScenarioLevels()
        {
            foreach (string scenario in PanelScenarios)
            {
                foreach (DiagnosticDetail detail in Enum.GetValues(typeof(DiagnosticDetail)))
                {
                    yield return new object[] { scenario, detail };
                }
            }
        }

        public static IEnumerable<object[]> PanelScenarios_Only()
        {
            foreach (string scenario in PanelScenarios)
            {
                yield return new object[] { scenario, DiagnosticDetail.Summary };
            }
        }

        public static IEnumerable<object[]> ApertureScenarioLevels()
        {
            foreach (string scenario in new[] { "apertures-house", "apertures-synthetic" })
            {
                foreach (DiagnosticDetail detail in Enum.GetValues(typeof(DiagnosticDetail)))
                {
                    yield return new object[] { scenario, detail };
                }
            }
        }

        // ---- 4. Legacy string output compatibility ------------------------------------------------------

        [Theory]
        [MemberData(nameof(PanelScenarios_Only))]
        public void LegacyOverloads_Panels_ReturnTheLinesAndGeometryOfTheBaseline(string scenario, DiagnosticDetail _)
        {
            BySolidsScenarios.Result result = LegacyRuns.Value[scenario];

            Assert.Equal(GoldenLines(scenario, "log"), result.Log);
            Assert.Equal(GoldenLines(scenario, "engineering"), result.Engineering);
        }

        [Theory]
        [InlineData("apertures-house")]
        [InlineData("apertures-synthetic")]
        public void LegacyOverloads_Apertures_ReturnTheLinesAndGeometryOfTheBaseline(string scenario)
        {
            List<ApertureSolid> solids = scenario == "apertures-house" ? BySolidsScenarios.HouseApertureSolids() : BySolidsScenarios.SyntheticApertureSolids();

            BySolidsScenarios.Result result = BySolidsScenarios.RunApertures(BySolidsScenarios.Legacy, solids);

            Assert.Equal(GoldenLines(scenario, "log"), result.Log);
            Assert.Equal(GoldenLines(scenario, "engineering"), result.Engineering);
        }

        [Fact]
        public void LegacyOverloads_UseOnlyTheDiagnosticFormat()
        {
            foreach (string scenario in PanelScenarios.Concat(new[] { "apertures-house", "apertures-synthetic" }))
            {
                foreach (string line in GoldenLines(scenario, "log"))
                {
                    string text = line.Substring(line.IndexOf(']') + 1).TrimStart();
                    Assert.Matches(@"^(Info|Warning|Error): ", text.StartsWith("[snap]") ? text.Substring(6).TrimStart() : text);
                }
            }
        }

        // ---- 1. Summary, Standard and Detailed output ---------------------------------------------------

        [Fact]
        public void Detailed_IsTheLegacyLogExactly()
        {
            foreach (string scenario in PanelScenarios)
            {
                Assert.Equal(GoldenLines(scenario, "log"), RunPanels(scenario, DiagnosticDetail.Detailed).Result.Log);
            }

            foreach (string scenario in new[] { "apertures-house", "apertures-synthetic" })
            {
                Assert.Equal(GoldenLines(scenario, "log"), RunApertures(scenario, DiagnosticDetail.Detailed).Result.Log);
            }
        }

        [Theory]
        [MemberData(nameof(PanelScenarioLevels))]
        public void Panels_EachLevelIsTheDetailedRecordsFilteredToIt(string scenario, DiagnosticDetail detail)
        {
            Core.Diagnostics detailed = RunPanels(scenario, DiagnosticDetail.Detailed).Diagnostics;
            Core.Diagnostics level = RunPanels(scenario, detail).Diagnostics;

            Assert.Equal(detailed.Filter(detail).Select(x => x.ToString()), level.Format());
        }

        [Theory]
        [MemberData(nameof(ApertureScenarioLevels))]
        public void Apertures_EachLevelIsTheDetailedRecordsFilteredToIt(string scenario, DiagnosticDetail detail)
        {
            Core.Diagnostics detailed = RunApertures(scenario, DiagnosticDetail.Detailed).Diagnostics;
            Core.Diagnostics level = RunApertures(scenario, detail).Diagnostics;

            Assert.Equal(detailed.Filter(detail).Select(x => x.ToString()), level.Format());
        }

        [Fact]
        public void Summary_Panels_HoldsNoPerSolidInfo_OnlyWarningsAndTheSnapLine()
        {
            Core.Diagnostics diagnostics = RunPanels("house-ratio-0.25", DiagnosticDetail.Summary).Diagnostics;

            Assert.All(diagnostics.Where(x => x.Severity == DiagnosticSeverity.Info), x => Assert.Equal(BySolidsDiagnosticCode.SnapChecked, x.Code));
            Assert.Single(diagnostics, x => x.Code == BySolidsDiagnosticCode.SnapChecked);
            Assert.Single(diagnostics, x => x.Severity == DiagnosticSeverity.Warning);
        }

        [Fact]
        public void Standard_Panels_AddsTheCreatedAndVariableThicknessLines_NotTheIgnoredPlanes()
        {
            Core.Diagnostics standard = RunPanels("synthetic-solids", DiagnosticDetail.Standard).Diagnostics;
            Core.Diagnostics detailed = RunPanels("synthetic-solids", DiagnosticDetail.Detailed).Diagnostics;

            Assert.Contains(standard, x => x.Code == BySolidsDiagnosticCode.PanelsCreated);
            Assert.DoesNotContain(standard, x => x.Code == BySolidsDiagnosticCode.PanelsIgnoredNotPanelLike);
            Diagnostic ignored = Assert.Single(detailed, x => x.Code == BySolidsDiagnosticCode.PanelsIgnoredNotPanelLike);
            Assert.Equal(DiagnosticDetail.Detailed, ignored.Detail);
            Assert.Equal("Solid #1 'slab-void'", ignored.Subject);
        }

        [Fact]
        public void Standard_Apertures_AddsOneCreatedLinePerAssembly()
        {
            Core.Diagnostics summary = RunApertures("apertures-house", DiagnosticDetail.Summary).Diagnostics;
            Core.Diagnostics standard = RunApertures("apertures-house", DiagnosticDetail.Standard).Diagnostics;

            Assert.Empty(summary);
            Assert.Equal(18, standard.Count(x => x.Code == BySolidsDiagnosticCode.AperturesCreated));
        }

        // ---- 2. Warning and Error preservation at every level -------------------------------------------

        [Theory]
        [MemberData(nameof(PanelScenarioLevels))]
        public void Panels_WarningsAreIdenticalAtEveryLevel_AndEqualTheBaselineWarnings(string scenario, DiagnosticDetail detail)
        {
            List<string> expected = GoldenLines(scenario, "log").Where(x => x.Contains("] Warning: ")).ToList();

            Core.Diagnostics diagnostics = RunPanels(scenario, detail).Diagnostics;
            List<string> actual = diagnostics.Where(x => x.Severity == DiagnosticSeverity.Warning).Select(x => x.ToString()).ToList();

            Assert.NotEmpty(expected);
            Assert.Equal(expected.Select(x => x.Substring(x.IndexOf("Warning: "))), actual);
            Assert.DoesNotContain(diagnostics, x => x.Severity == DiagnosticSeverity.Error);
        }

        [Theory]
        [MemberData(nameof(ApertureScenarioLevels))]
        public void Apertures_WarningsAreIdenticalAtEveryLevel(string scenario, DiagnosticDetail detail)
        {
            List<string> expected = GoldenLines(scenario, "log").Where(x => x.StartsWith("Warning: ")).ToList();

            List<string> actual = RunApertures(scenario, detail).Diagnostics.Where(x => x.Severity == DiagnosticSeverity.Warning).Select(x => x.ToString()).ToList();

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void Warnings_CarryTheirSubjectSourceIndexCodeAndEngineeringValues()
        {
            Core.Diagnostics house = RunPanels("house-default", DiagnosticDetail.Summary).Diagnostics;
            List<Diagnostic> warnings = house.Where(x => x.Severity == DiagnosticSeverity.Warning).ToList();
            Assert.Equal(2, warnings.Count);

            Diagnostic stair = warnings[0];
            Assert.Equal(BySolidsDiagnosticCode.PanelsNotPanelLike, stair.Code);
            Assert.Equal("Solid #0 'ST_Stair_GF_to_FF'", stair.Subject);
            Assert.Equal(0, stair.SourceIndex);
            Assert.Equal(0.398, stair.Value.Value, 3);
            Assert.Equal(0.2, stair.Limit.Value, 6);
            Assert.Equal("-", stair.Unit);

            Diagnostic gable = warnings[1];
            Assert.Equal(2, gable.SourceIndex);
            Assert.Equal(0.214, gable.Value.Value, 3);

            Core.Diagnostics thin = RunPanels("house-min-0.25", DiagnosticDetail.Summary).Diagnostics;
            Diagnostic below = thin.First(x => x.Code == BySolidsDiagnosticCode.PanelsBelowMinThickness);
            Assert.Equal(0.25, below.Limit.Value, 6);
            Assert.True(below.Value.Value < 0.25);
            Assert.Equal("m", below.Unit);
            Assert.NotNull(below.SourceIndex);

            Core.Diagnostics synthetic = RunPanels("synthetic-solids", DiagnosticDetail.Summary).Diagnostics;
            Assert.Equal(new[] { BySolidsDiagnosticCode.PanelsNotPanelLike, BySolidsDiagnosticCode.PanelsBelowMinThickness, BySolidsDiagnosticCode.PanelsNotClosed }, synthetic.Where(x => x.Severity == DiagnosticSeverity.Warning).Select(x => x.Code).ToArray());
            Assert.Equal(new int?[] { 2, 3, 4 }, synthetic.Where(x => x.Severity == DiagnosticSeverity.Warning).Select(x => x.SourceIndex).ToArray());
        }

        [Fact]
        public void ApertureWarnings_CarryTheirCodeSubjectAndSourceIndex()
        {
            List<Diagnostic> warnings = RunApertures("apertures-synthetic", DiagnosticDetail.Summary).Diagnostics.ToList();

            Assert.Equal(2, warnings.Count);
            Assert.Equal(BySolidsDiagnosticCode.AperturesNotClosed, warnings[0].Code);
            Assert.Equal(3, warnings[0].SourceIndex);
            Assert.Equal("Solid #3 'nothing'", warnings[0].Subject);
            Assert.Equal(BySolidsDiagnosticCode.AperturesNotPlanar, warnings[1].Code);
            Assert.Equal(4, warnings[1].SourceIndex);
            Assert.Equal(1.0, warnings[1].Value.Value, 6);
            Assert.Equal(0.5, warnings[1].Limit.Value, 6);
        }

        [Fact]
        public void CreatedRecords_CarryTheThicknessAndSourceIndex()
        {
            Core.Diagnostics diagnostics = RunPanels("synthetic-solids", DiagnosticDetail.Standard).Diagnostics;

            Diagnostic wall = diagnostics.First(x => x.Code == BySolidsDiagnosticCode.PanelsCreated);
            Assert.Equal(0, wall.SourceIndex);
            Assert.Equal(0.25, wall.Value.Value, 6);
            Assert.Equal("m", wall.Unit);
            Assert.Equal(DiagnosticSeverity.Info, wall.Severity);
        }

        [Fact]
        public void SnapRecord_CarriesTheSnapDistance()
        {
            Diagnostic snap = RunPanels("synthetic-solids", DiagnosticDetail.Summary).Diagnostics.Single(x => x.Code == BySolidsDiagnosticCode.SnapChecked);

            Assert.Equal(DiagnosticSeverity.Info, snap.Severity);
            Assert.Equal(0.3, snap.Value.Value, 6);
            Assert.Null(snap.Subject);
        }

        [Theory]
        [MemberData(nameof(PanelScenarioLevels))]
        public void Panels_EverySolidThatGaveNoPanel_HasAWarningAtEveryLevel(string scenario, DiagnosticDetail detail)
        {
            (BySolidsScenarios.Result result, Core.Diagnostics diagnostics) = RunPanels(scenario, detail);

            HashSet<int> withPanels = new HashSet<int>(result.Engineering.Where(x => x.StartsWith("[") && char.IsDigit(x[1])).Select(x => int.Parse(x.Substring(1, x.IndexOf(']') - 1))));
            HashSet<int> skipped = new HashSet<int>(Enumerable.Range(0, Solids(scenario).Count).Where(x => !withPanels.Contains(x)));
            HashSet<int> warned = new HashSet<int>(diagnostics.Where(x => x.Severity == DiagnosticSeverity.Warning).Select(x => x.SourceIndex.Value));

            Assert.NotEmpty(skipped);
            Assert.Equal(skipped, warned);
            Assert.DoesNotContain(diagnostics, x => x.Code == BySolidsDiagnosticCode.PanelsNone);
        }

        // ---- 3. Detail input handling -------------------------------------------------------------------

        [Theory]
        [InlineData(0, DiagnosticDetail.Summary, true)]
        [InlineData(1, DiagnosticDetail.Standard, true)]
        [InlineData(2, DiagnosticDetail.Detailed, true)]
        [InlineData(-1, DiagnosticDetail.Summary, false)]
        [InlineData(3, DiagnosticDetail.Summary, false)]
        [InlineData(99, DiagnosticDetail.Summary, false)]
        [InlineData(int.MinValue, DiagnosticDetail.Summary, false)]
        [InlineData(int.MaxValue, DiagnosticDetail.Summary, false)]
        public void TryGetDiagnosticDetail_AcceptsOnlyTheDefinedLevels_AndFallsBackToSummary(int value, DiagnosticDetail expected, bool valid)
        {
            bool result = Core.Query.TryGetDiagnosticDetail(value, out DiagnosticDetail detail);

            Assert.Equal(valid, result);
            Assert.Equal(expected, detail);
        }

        [Fact]
        public void NullDiagnostics_AreAccepted_AndGiveTheSameGeometry()
        {
            List<PanelsBySolidComplexHouseTests.SourceSolid> solids = BySolidsScenarios.SyntheticSolids();

            List<Panel> withNull = solids[1].Shell.PanelsBySolid((Core.Diagnostics)null);
            List<Panel> withSink = solids[1].Shell.PanelsBySolid(new Core.Diagnostics(DiagnosticDetail.Detailed));

            Assert.Equal(BySolidsScenarios.Fingerprint(withSink), BySolidsScenarios.Fingerprint(withNull));
            Assert.Empty(new[] { new Panel[0] }.SelectMany(x => x.SnapToJunctions((Core.Diagnostics)null)));
            Assert.Empty(new List<ApertureSolid>().AperturesBySolids((Core.Diagnostics)null, out List<int> excluded));
            Assert.Empty(excluded);
        }

        // ---- 5. Deterministic ordering and formatting ---------------------------------------------------

        [Theory]
        [MemberData(nameof(PanelScenarios_Only))]
        public void Panels_RepeatedRunsGiveIdenticalRecordsInIdenticalOrder(string scenario, DiagnosticDetail _)
        {
            (BySolidsScenarios.Result first, Core.Diagnostics firstDiagnostics) = RunPanelsUncached(scenario, DiagnosticDetail.Detailed);
            (BySolidsScenarios.Result second, Core.Diagnostics secondDiagnostics) = RunPanelsUncached(scenario, DiagnosticDetail.Detailed);

            Assert.Equal(first.Log, second.Log);
            Assert.Equal(firstDiagnostics.Select(x => (x.Severity, x.Detail, x.Code, x.Subject, x.SourceIndex, x.Message, x.Value, x.Limit, x.Unit)), secondDiagnostics.Select(x => (x.Severity, x.Detail, x.Code, x.Subject, x.SourceIndex, x.Message, x.Value, x.Limit, x.Unit)));
        }

        [Fact]
        public void Records_AreInInputOrder_SnapLast()
        {
            List<Diagnostic> records = RunPanels("house-ratio-0.25", DiagnosticDetail.Detailed).Diagnostics.Where(x => x.Code != null && x.Code.StartsWith("PBS") || x.Code == BySolidsDiagnosticCode.SnapChecked).ToList();

            List<int> indexes = records.Where(x => x.SourceIndex.HasValue).Select(x => x.SourceIndex.Value).ToList();
            Assert.Equal(indexes.OrderBy(x => x), indexes);
            Assert.Equal(BySolidsDiagnosticCode.SnapChecked, records.Last().Code);
        }

        [Fact]
        public void Records_FollowTheDiagnosticMessageFormat_AndEveryCodeIsKnown()
        {
            HashSet<string> known = new HashSet<string>(typeof(BySolidsDiagnosticCode).GetFields().Select(x => (string)x.GetRawConstantValue()));

            foreach (string scenario in PanelScenarios)
            {
                foreach (Diagnostic diagnostic in RunPanels(scenario, DiagnosticDetail.Detailed).Diagnostics)
                {
                    Assert.Contains(diagnostic.Code, known);
                    Assert.Equal(Core.Query.DiagnosticMessage(diagnostic.Severity == DiagnosticSeverity.Warning ? LogRecordType.Warning : diagnostic.Severity == DiagnosticSeverity.Error ? LogRecordType.Error : LogRecordType.Message, diagnostic.Subject, diagnostic.Message), diagnostic.ToString());
                    Assert.Matches(@"^[A-Z]", diagnostic.Message);
                }
            }
        }

        // ---- 6. Identical engineering results at every detail level -------------------------------------

        [Theory]
        [MemberData(nameof(PanelScenarioLevels))]
        public void Panels_EngineeringResultsAreIdenticalAtEveryLevel_AndToTheBaseline(string scenario, DiagnosticDetail detail)
        {
            BySolidsScenarios.Result result = RunPanels(scenario, detail).Result;

            Assert.Equal(GoldenLines(scenario, "engineering"), result.Engineering);
        }

        [Theory]
        [MemberData(nameof(ApertureScenarioLevels))]
        public void Apertures_EngineeringResultsAreIdenticalAtEveryLevel_AndToTheBaseline(string scenario, DiagnosticDetail detail)
        {
            BySolidsScenarios.Result result = RunApertures(scenario, detail).Result;

            Assert.Equal(GoldenLines(scenario, "engineering"), result.Engineering);
        }

        [Theory]
        [InlineData("house-default", 38, "0,2")]
        [InlineData("house-ratio-0.25", 39, "0")]
        [InlineData("house-min-0.25", 29, "0,7,8,9,10,12,13,14,15,18")]
        [InlineData("house-min-0.05-ratio-0.3", 39, "0")]
        public void House_PanelCountsAndSkippedSolids_AreTheBaselineOnes_AtEveryLevel(string scenario, int panelCount, string skippedSolids)
        {
            // 38 panels at the default and 39 at ratio 0.25 are the PR #198 / #200 figures; the others are the committed baseline.
            foreach (DiagnosticDetail detail in Enum.GetValues(typeof(DiagnosticDetail)))
            {
                List<string> panels = RunPanels(scenario, detail).Result.Engineering.Where(x => x.StartsWith("[") && char.IsDigit(x[1])).ToList();
                Assert.Equal(panelCount, panels.Count);
                Assert.Equal(skippedSolids, string.Join(",", Enumerable.Range(0, 19).Where(i => !panels.Any(x => x.StartsWith("[" + i + "] ")))));
            }
        }

        [Fact]
        public void House_RatioAboveThePr200Limit_StaysUnchanged_NoClassificationFixIsIncluded()
        {
            // PR #200 limitation: keep maxThicknessRatio_ <= 0.3 (>= 0.4 turns the stair into a 1.125 m wall). Nothing here changes it:
            // at 0.3 the stair is still skipped with its Warning, exactly as in the baseline.
            string stairLine = GoldenLines("house-min-0.05-ratio-0.3", "log").First(x => x.Contains("ST_Stair_GF_to_FF"));
            Assert.Contains("Warning: ", stairLine);

            Assert.Equal(GoldenLines("house-min-0.05-ratio-0.3", "log"), BySolidsScenarios.RunPanels(BySolidsScenarios.Legacy, Solids("house-min-0.05-ratio-0.3"), ApertureSolidsOf("house-min-0.05-ratio-0.3"), SettingsOf("house-min-0.05-ratio-0.3")).Log);
        }

        // ---- Public API compatibility -------------------------------------------------------------------

        [Fact]
        public void LegacyOverloads_StillExistWithTheirOutParameters()
        {
            Assert.NotNull(typeof(Analytical.Create).GetMethods().Single(x => x.Name == nameof(Analytical.Create.PanelsBySolid) && x.GetParameters()[1].ParameterType == typeof(List<string>).MakeByRefType()));
            Assert.NotNull(typeof(Analytical.Create).GetMethods().Single(x => x.Name == nameof(Analytical.Create.AperturesBySolids) && x.GetParameters()[1].ParameterType == typeof(List<string>).MakeByRefType()));
            Assert.NotNull(typeof(Analytical.Query).GetMethods().Single(x => x.Name == nameof(Analytical.Query.SnapToJunctions) && x.GetParameters()[1].ParameterType == typeof(List<string>).MakeByRefType()));
        }
    }
}
