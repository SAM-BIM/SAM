// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical;
using SAM.Analytical.Enums;
using SAM.Core;
using System;
using System.IO;
using System.Text;
using Xunit;

namespace SAM.Tests
{
    /// <summary>
    /// <c>SimulationResultProvenance</c> - the persisted record that lets a reopened model be paired with
    /// the results it was produced from, and refuses the pairing when either side no longer matches.
    /// </summary>
    public class SimulationResultProvenanceTests
    {
        private static string WriteResults(string path)
        {
            File.WriteAllText(path, string.Format("results - {0}", Guid.NewGuid()));

            return path;
        }

        private static AnalyticalModel Model(string name)
        {
            AdjacencyCluster adjacencyCluster = new();
            adjacencyCluster.AddObject(new Space("Bedroom 1"));

            return new AnalyticalModel(name, null, null, null, adjacencyCluster);
        }

        /// <summary>The record round-trips through JSON with every field intact.</summary>
        [Fact]
        public void JsonRoundTrip_PreservesTheRecord()
        {
            string path_TSD = WriteResults(Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".tsd"));

            try
            {
                SimulationResultProvenance provenance = new(Model("run"), path_TSD);

                SimulationResultProvenance read = new(provenance.ToJsonObject());

                Assert.Equal(provenance.Path_TSD, read.Path_TSD);
                Assert.Equal(provenance.Length_TSD, read.Length_TSD);
                Assert.Equal(provenance.Timestamp_TSD, read.Timestamp_TSD);
                Assert.Equal(provenance.Fingerprint_Model, read.Fingerprint_Model);
                Assert.Equal(provenance.Fingerprint_OverheatingScenarios, read.Fingerprint_OverheatingScenarios);

                //Every required field is there, so the record can state the rule rather than part of it.
                Assert.True(read.IsComplete);
            }
            finally
            {
                File.Delete(path_TSD);
            }
        }

        /// <summary>Current means: exists, same length, same write time. Anything less is not these results.</summary>
        [Fact]
        public void IsCurrent_RequiresTheExactRecordedFile()
        {
            string path_TSD = WriteResults(Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".tsd"));

            try
            {
                SimulationResultProvenance provenance = new(Model("run"), path_TSD);

                Assert.True(provenance.IsCurrent(path_TSD));
                Assert.False(provenance.IsCurrent(path_TSD + ".other.tsd"));

                File.AppendAllText(path_TSD, "changed");

                Assert.False(provenance.IsCurrent(path_TSD));
            }
            finally
            {
                File.Delete(path_TSD);
            }
        }

        /// <summary>
        /// The model half: the same model hashes to the same fingerprint after a JSON round trip - which is
        /// the whole reopen path - and an edit changes it.
        /// </summary>
        [Fact]
        public void IsCurrent_ModelFingerprint()
        {
            AnalyticalModel analyticalModel = Model("run");

            string fingerprint = SimulationResultProvenance.Fingerprint(analyticalModel);

            //Stable across a save and a reload - the identities survive serialization, and so does the hash.
            AnalyticalModel analyticalModel_Reopened = new(analyticalModel.ToJsonObject());
            Assert.Equal(fingerprint, SimulationResultProvenance.Fingerprint(analyticalModel_Reopened));

            SimulationResultProvenance provenance = new(analyticalModel, null);

            Assert.True(provenance.IsCurrent(analyticalModel_Reopened));

            AdjacencyCluster adjacencyCluster = analyticalModel.AdjacencyCluster;
            adjacencyCluster.AddObject(new Space("Kitchen"));
            Assert.False(provenance.IsCurrent(new AnalyticalModel(analyticalModel, adjacencyCluster)));
        }

        /// <summary>
        /// <b>A record missing either fingerprint validates nothing.</b> This replaces the rule that let an
        /// absent model fingerprint pass on the results side alone. That permissiveness had no legacy to
        /// serve - a model saved before this record existed carries no record at all, and is handled as a
        /// model that was never simulated - and it made a half-written record look like a whole one.
        /// </summary>
        [Fact]
        public void IsCurrent_FailsClosedOnAMissingFingerprint()
        {
            AnalyticalModel analyticalModel = Model("run");

            SimulationResultProvenance provenance = new(analyticalModel, null);

            //The unmodified record still validates the model it was taken from.
            Assert.True(provenance.IsCurrent(analyticalModel));
            Assert.False(provenance.IsComplete);       //no results file was recorded above

            //Neither half may be absent.
            SimulationResultProvenance provenance_NoModel = new(provenance) { Fingerprint_Model = string.Empty };
            Assert.False(provenance_NoModel.IsCurrent(analyticalModel));

            SimulationResultProvenance provenance_NoScenarios = new(provenance) { Fingerprint_OverheatingScenarios = string.Empty };
            Assert.False(provenance_NoScenarios.IsCurrent(analyticalModel));
        }

        /// <summary>
        /// <b>The scenario half, which the cluster fingerprint cannot see.</b> The overheating scenarios are
        /// model-level state, so a model whose scenarios were swapped after its run has an UNCHANGED cluster
        /// fingerprint - and <c>PartORun.Restore</c> reads those scenarios back as the authority the results
        /// are reassessed under. Pinned here at the fingerprint: same scenarios digest the same across a save
        /// and a reload, a different set does not, and a reordering of the same set is not a difference.
        /// </summary>
        [Fact]
        public void Fingerprint_Scenarios_BindsTheAssessmentContext()
        {
            OverheatingScenario overheatingScenario_A = new(PartOAssessmentScope.Dwelling, Guid.NewGuid(), PartOIteration.BasePassive);
            OverheatingScenario overheatingScenario_B = new(PartOAssessmentScope.Dwelling, Guid.NewGuid(), PartOIteration.AcousticRestricted);

            AnalyticalModel analyticalModel = Model("run");
            analyticalModel.SetValue(AnalyticalModelParameter.OverheatingScenarios, new SAMCollection<OverheatingScenario>([overheatingScenario_A, overheatingScenario_B]));

            string fingerprint = SimulationResultProvenance.Fingerprint_Scenarios(analyticalModel);

            //Survives the save and the reload, which is the whole reopen path.
            Assert.Equal(fingerprint, SimulationResultProvenance.Fingerprint_Scenarios(new AnalyticalModel(analyticalModel.ToJsonObject())));

            //The order a collection happens to hold a set of assessments in is not part of what they state.
            AnalyticalModel analyticalModel_Reordered = Model("run");
            analyticalModel_Reordered.SetValue(AnalyticalModelParameter.OverheatingScenarios, new SAMCollection<OverheatingScenario>([overheatingScenario_B, overheatingScenario_A]));
            Assert.Equal(fingerprint, SimulationResultProvenance.Fingerprint_Scenarios(analyticalModel_Reordered));

            //A different assessment authority is a different fingerprint - a changed iteration...
            AnalyticalModel analyticalModel_Changed = Model("run");
            analyticalModel_Changed.SetValue(AnalyticalModelParameter.OverheatingScenarios, new SAMCollection<OverheatingScenario>([overheatingScenario_A, new OverheatingScenario(PartOAssessmentScope.Dwelling, overheatingScenario_B.ZoneGuid, PartOIteration.BasePassive)]));
            Assert.NotEqual(fingerprint, SimulationResultProvenance.Fingerprint_Scenarios(analyticalModel_Changed));

            //...and a dropped scenario, which is what makes the count part of the digest.
            AnalyticalModel analyticalModel_Subset = Model("run");
            analyticalModel_Subset.SetValue(AnalyticalModelParameter.OverheatingScenarios, new SAMCollection<OverheatingScenario>([overheatingScenario_A]));
            Assert.NotEqual(fingerprint, SimulationResultProvenance.Fingerprint_Scenarios(analyticalModel_Subset));

            //A model carrying none is not a model carrying some.
            Assert.NotEqual(fingerprint, SimulationResultProvenance.Fingerprint_Scenarios(Model("run")));
            Assert.Equal(16, SimulationResultProvenance.Fingerprint_Scenarios(null).Length);
        }

        /// <summary>
        /// <b>The fingerprint is exactly the digest of the cluster's JSON, and is computed without
        /// materializing it.</b> The definition is what the record documents and what earlier builds wrote,
        /// so it has to stay byte-for-byte the same as hashing <c>ToJsonString()</c> - this pins that against
        /// a future drift in writer options, which would silently invalidate every recorded digest. The
        /// streaming exists because a real ~5,000-space project's cluster JSON is hundreds of megabytes, and
        /// holding it as a string and a byte array to produce sixteen characters is a multi-gigabyte spike.
        /// </summary>
        [Fact]
        public void Fingerprint_IsTheDigestOfTheClusterJson()
        {
            AnalyticalModel analyticalModel = Model("run");

            ulong expected = 14695981039346656037;
            foreach (byte value in Encoding.UTF8.GetBytes(analyticalModel.AdjacencyCluster.ToJsonObject().ToJsonString()))
            {
                expected ^= value;
                expected *= 1099511628211;
            }

            Assert.Equal(expected.ToString("x16", System.Globalization.CultureInfo.InvariantCulture), SimulationResultProvenance.Fingerprint(analyticalModel));

            //A model with nothing to hash still answers, rather than throwing on the way to a null cluster.
            Assert.Equal(16, SimulationResultProvenance.Fingerprint(null).Length);
        }

        /// <summary>
        /// <b>Absent results are refused on the results side, before the model is digested.</b> The file
        /// checks are stats; the model check hashes the whole project. This pins the order through the
        /// refusal it produces: a record whose file is gone AND whose model has since changed reports the
        /// missing file - the half that was reached - not the model.
        /// </summary>
        [Fact]
        public void TryResolvePath_TSD_RefusesAbsentResultsWithoutTheModelHalf()
        {
            string path_TSD = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".tsd");

            AnalyticalModel analyticalModel = Model("run");

            WriteResults(path_TSD);
            SimulationResultProvenance provenance = new(analyticalModel, path_TSD);
            File.Delete(path_TSD);

            //Both halves now fail: the file is gone, and the model is not the one recorded.
            AdjacencyCluster adjacencyCluster = analyticalModel.AdjacencyCluster;
            adjacencyCluster.AddObject(new Space("Kitchen"));

            Assert.False(provenance.TryResolvePath_TSD(new AnalyticalModel(analyticalModel, adjacencyCluster), null, out string path_Resolved, out string refusal));

            Assert.Null(path_Resolved);
            Assert.Contains("no longer at", refusal);
            Assert.DoesNotContain("The model has changed", refusal);
        }

        /// <summary>
        /// And the model half still refuses where the results file is intact - the check moved order, not
        /// away.
        /// </summary>
        [Fact]
        public void TryResolvePath_TSD_StillRefusesAChangedModel()
        {
            string path_TSD = WriteResults(Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".tsd"));

            try
            {
                AnalyticalModel analyticalModel = Model("run");

                SimulationResultProvenance provenance = new(analyticalModel, path_TSD);

                Assert.True(provenance.TryResolvePath_TSD(analyticalModel, null, out string path_Resolved, out string refusal));
                Assert.Equal(path_TSD, path_Resolved);
                Assert.Null(refusal);

                AdjacencyCluster adjacencyCluster = analyticalModel.AdjacencyCluster;
                adjacencyCluster.AddObject(new Space("Kitchen"));

                Assert.False(provenance.TryResolvePath_TSD(new AnalyticalModel(analyticalModel, adjacencyCluster), null, out string _, out string refusal_Changed));
                Assert.Contains("The model has changed", refusal_Changed);
            }
            finally
            {
                File.Delete(path_TSD);
            }
        }

        /// <summary>
        /// <b>The case the model fingerprint alone cannot see.</b> The cluster is untouched and the results
        /// file is byte-for-byte the recorded one; only the overheating scenarios were swapped after the run.
        /// Before the scenario fingerprint existed, that validated - and the old results would then have been
        /// reassessed under an authority that did not produce them. It is now refused, and the refusal names
        /// the scenarios rather than blaming the model or the file.
        /// </summary>
        [Fact]
        public void TryResolvePath_TSD_RefusesScenariosChangedUnderUnchangedResults()
        {
            string path_TSD = WriteResults(Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".tsd"));

            try
            {
                Guid guid_Zone = Guid.NewGuid();

                AnalyticalModel analyticalModel = Model("run");
                analyticalModel.SetValue(AnalyticalModelParameter.OverheatingScenarios, new SAMCollection<OverheatingScenario>([new OverheatingScenario(PartOAssessmentScope.Dwelling, guid_Zone, PartOIteration.BasePassive)]));

                SimulationResultProvenance provenance = new(analyticalModel, path_TSD);

                Assert.True(provenance.TryResolvePath_TSD(analyticalModel, null, out string path_Resolved, out string refusal));
                Assert.Equal(path_TSD, path_Resolved);
                Assert.Null(refusal);

                //Scenario A becomes Scenario B on the SAME model file: same cluster, same results, different
                //assessment authority. This is the exact sequence the record now binds.
                analyticalModel.SetValue(AnalyticalModelParameter.OverheatingScenarios, new SAMCollection<OverheatingScenario>([new OverheatingScenario(PartOAssessmentScope.Dwelling, guid_Zone, PartOIteration.AcousticRestricted)]));

                //The unchanged halves are still unchanged - so the refusal below is the scenario half alone.
                Assert.Equal(provenance.Fingerprint_Model, SimulationResultProvenance.Fingerprint(analyticalModel));
                Assert.True(provenance.IsCurrent(path_TSD));

                Assert.False(provenance.TryResolvePath_TSD(analyticalModel, null, out string _, out string refusal_Scenarios));
                Assert.Contains("overheating scenarios", refusal_Scenarios);
                Assert.DoesNotContain("The model has changed", refusal_Scenarios);
            }
            finally
            {
                File.Delete(path_TSD);
            }
        }

        /// <summary>
        /// <b>An incomplete record resolves nothing, and says which field is missing.</b> Every required
        /// field is checked before the filesystem is touched, so a half-written record is refused without
        /// being partially believed - and without hashing a large project on the way to refusing it.
        /// </summary>
        [Fact]
        public void TryResolvePath_TSD_RefusesAnIncompleteRecord()
        {
            string path_TSD = WriteResults(Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".tsd"));

            try
            {
                AnalyticalModel analyticalModel = Model("run");

                SimulationResultProvenance provenance = new(analyticalModel, path_TSD);

                Assert.True(provenance.IsComplete);
                Assert.True(provenance.TryResolvePath_TSD(analyticalModel, null, out string _, out string _));

                //No model fingerprint.
                SimulationResultProvenance provenance_NoModel = new(provenance) { Fingerprint_Model = string.Empty };
                Assert.False(provenance_NoModel.IsComplete);
                Assert.False(provenance_NoModel.TryResolvePath_TSD(analyticalModel, null, out string path_NoModel, out string refusal_NoModel));
                Assert.Null(path_NoModel);
                Assert.Contains("does not state the design", refusal_NoModel);

                //No scenario fingerprint.
                SimulationResultProvenance provenance_NoScenarios = new(provenance) { Fingerprint_OverheatingScenarios = string.Empty };
                Assert.False(provenance_NoScenarios.IsComplete);
                Assert.False(provenance_NoScenarios.TryResolvePath_TSD(analyticalModel, null, out string path_NoScenarios, out string refusal_NoScenarios));
                Assert.Null(path_NoScenarios);
                Assert.Contains("does not state the overheating scenarios", refusal_NoScenarios);

                //No recorded size or write time - a file check that could never be satisfied is refused as
                //what it is, rather than left to fail as a missing file.
                SimulationResultProvenance provenance_NoFileStats = new(provenance) { Length_TSD = -1, Timestamp_TSD = -1 };
                Assert.False(provenance_NoFileStats.IsComplete);
                Assert.False(provenance_NoFileStats.IsCurrent(path_TSD));
                Assert.False(provenance_NoFileStats.TryResolvePath_TSD(analyticalModel, null, out string path_NoFileStats, out string refusal_NoFileStats));
                Assert.Null(path_NoFileStats);
                Assert.Contains("size and write time", refusal_NoFileStats);
            }
            finally
            {
                File.Delete(path_TSD);
            }
        }
    }
}
