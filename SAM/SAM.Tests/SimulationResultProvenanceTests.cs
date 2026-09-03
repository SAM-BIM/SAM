// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical;
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
        /// the whole reopen path - and an edit changes it. A record with no fingerprint (written before it
        /// was kept) validates on its results side alone.
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

            //A record written before the fingerprint was kept is not a mismatch.
            provenance.Fingerprint_Model = string.Empty;
            Assert.True(provenance.IsCurrent(new AnalyticalModel(analyticalModel, adjacencyCluster)));
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
    }
}
