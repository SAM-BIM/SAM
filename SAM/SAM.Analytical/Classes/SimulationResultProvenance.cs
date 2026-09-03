// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Nodes;

namespace SAM.Analytical
{
    /// <summary>
    /// Which simulation results file an <see cref="AnalyticalModel"/> was produced from, and how to tell
    /// whether the two still belong together: the results file's path, length and write time, captured when
    /// the model was written, a fingerprint of the model's own content, and a fingerprint of the overheating
    /// scenarios the run was assessed under.
    /// <para>
    /// <b>Why this exists.</b> A model saved after a full-year simulation - the per-run
    /// <c>&lt;project&gt;.sam</c> the Part O workflow writes, or the user's own <c>.sam</c> afterwards - can
    /// be reopened in a later session and its overheating results reviewed WITHOUT rerunning the simulation,
    /// provided the results it pairs with are provably the ones it was produced from. This record is that
    /// proof. It is stamped onto the model the workflow returned, so it travels with the model wherever the
    /// model is saved.
    /// </para>
    ///
    /// <para><b>The validity rule, stated rather than guessed</b></para>
    /// <para>
    /// Three things must be bound together, because all three are read when a restored run is assessed, and
    /// any one of them moving underneath the other two would produce an assessment nobody ran:
    /// </para>
    /// <list type="number">
    /// <item>
    /// <b>The results file.</b> A candidate must exist AND match the recorded length and write time - a name
    /// alone is never enough, because two runs of one project write results at the same derived path.
    /// </item>
    /// <item>
    /// <b>The model's design state.</b> The reopened model's <see cref="AdjacencyCluster"/> - spaces,
    /// internal conditions, zones and their relations, the content an assessment actually reads - must hash
    /// to <see cref="Fingerprint_Model"/>, so a model edited after the run is refused rather than silently
    /// paired with results a different design produced.
    /// </item>
    /// <item>
    /// <b>The overheating scenarios.</b> The scenarios persisted on the model must hash to
    /// <see cref="Fingerprint_OverheatingScenarios"/>. They are model-level state, NOT part of the cluster,
    /// so the cluster fingerprint does not see them at all - and <c>PartORun.Restore</c> reads them back as
    /// the authoritative TM59 assessment context. Without this half, a model whose scenarios were changed
    /// after the run would still validate on its cluster and its file, and the old results would be
    /// reassessed against an assessment authority that did not produce them. That is the case this member
    /// exists to refuse.
    /// </item>
    /// </list>
    /// <para>
    /// <b>Kept as a separate fingerprint, deliberately.</b> The cluster hash is the expensive one - it costs
    /// in proportion to the size of the project - and the scenarios are a handful of derived keys. Holding
    /// them apart keeps the contract observable (a refusal names WHICH half moved) and keeps the
    /// large-project hashing seam untouched by the small one.
    /// </para>
    ///
    /// <para><b>Fail closed: a partial record is not a record</b></para>
    /// <para>
    /// Every field above is required. A record missing any of them cannot state that a model and its results
    /// belong together, so it does not get to; it is refused with its reason, never validated on the halves
    /// it happens to carry. This is NOT a compatibility problem: a model saved before this record existed
    /// carries no record at all, and a model carrying no record is handled where it should be - as a model
    /// that was never simulated on this path, with the ordinary "prepare and simulate" guidance. There is no
    /// third state in between, and inventing one would be exactly the permissiveness this type exists to
    /// remove.
    /// </para>
    ///
    /// <para><b>Provenance only.</b></para>
    /// <para>
    /// This records where the results are; it decides nothing about what they say. The assessment itself
    /// still resolves every space by identity and refuses what does not resolve.
    /// </para>
    /// </summary>
    public class SimulationResultProvenance : SAMObject
    {
        public SimulationResultProvenance()
        {
        }

        /// <summary>
        /// Captures the record from the model and its results file as they stand at this moment.
        /// <para>
        /// Both fingerprints are taken from <paramref name="analyticalModel"/>, so the scenarios must already
        /// be stamped onto it when this is constructed - which is what <c>Modify.RunPartOSimulation</c> does,
        /// and why it does it in that order.
        /// </para>
        /// </summary>
        public SimulationResultProvenance(AnalyticalModel analyticalModel, string path_TSD)
        {
            Path_TSD = path_TSD;
            Fingerprint_Model = Fingerprint(analyticalModel);
            Fingerprint_OverheatingScenarios = Fingerprint_Scenarios(analyticalModel);

            if (!string.IsNullOrWhiteSpace(path_TSD))
            {
                FileInfo fileInfo = new(path_TSD);
                if (fileInfo.Exists)
                {
                    Length_TSD = fileInfo.Length;
                    Timestamp_TSD = fileInfo.LastWriteTimeUtc.Ticks;
                }
            }
        }

        public SimulationResultProvenance(SimulationResultProvenance simulationResultProvenance)
            : base(simulationResultProvenance)
        {
            if (simulationResultProvenance is not null)
            {
                Path_TSD = simulationResultProvenance.Path_TSD;
                Length_TSD = simulationResultProvenance.Length_TSD;
                Timestamp_TSD = simulationResultProvenance.Timestamp_TSD;
                Fingerprint_Model = simulationResultProvenance.Fingerprint_Model;
                Fingerprint_OverheatingScenarios = simulationResultProvenance.Fingerprint_OverheatingScenarios;
            }
        }

        public SimulationResultProvenance(JsonObject jsonObject)
            : base(jsonObject)
        {
        }

        /// <summary>The results file the model was produced from, as an absolute path when recorded. Required.</summary>
        public string Path_TSD { get; set; }

        /// <summary>The results file's length in bytes when recorded. Required; negative means absent.</summary>
        public long Length_TSD { get; set; } = -1;

        /// <summary>
        /// The results file's <see cref="DateTime.Ticks"/> of <see cref="FileInfo.LastWriteTimeUtc"/> when
        /// recorded. Required; negative means absent.
        /// </summary>
        public long Timestamp_TSD { get; set; } = -1;

        /// <summary>
        /// The model's own design-state fingerprint when recorded - see <see cref="Fingerprint(AnalyticalModel)"/>.
        /// <b>Required.</b> Absent (<see cref="string.Empty"/>) makes the whole record unusable, not partially
        /// usable.
        /// </summary>
        public string Fingerprint_Model { get; set; } = string.Empty;

        /// <summary>
        /// The overheating scenarios' fingerprint when recorded - see
        /// <see cref="Fingerprint_Scenarios(AnalyticalModel)"/>. <b>Required</b>, for the same reason: the
        /// scenarios are the assessment authority a restored run reads back, and they live outside the
        /// cluster the model fingerprint covers.
        /// </summary>
        public string Fingerprint_OverheatingScenarios { get; set; } = string.Empty;

        /// <summary>
        /// The model-content half of the validity rule: an FNV-1a digest of the <see cref="AdjacencyCluster"/>'s
        /// JSON - the spaces, internal conditions, zones and relations an assessment reads. Model-level
        /// parameters (view settings, the scenarios, this record itself) are deliberately not part of it; the
        /// scenarios have their own fingerprint precisely because this one cannot see them.
        /// <para>
        /// <b>Deliberately not <c>string.GetHashCode</c></b>, which is randomized per process - the digest is
        /// recorded and compared across sessions, so it must be stable, which the round trip is verified to
        /// preserve.
        /// </para>
        /// <para>
        /// <b>Streamed, because the cluster is as large as the project.</b> The digest is taken over the
        /// serialized bytes as they are written, never over a materialized copy of them. Measured on a real
        /// Part O run, one space is worth roughly 140 kB of cluster JSON; at the ~5,000 spaces a real SAM
        /// project reaches, holding that JSON as a string (2 bytes per char) AND as a UTF-8 byte array - which
        /// is what <c>Encoding.UTF8.GetBytes(node.ToJsonString())</c> does - is a multi-gigabyte allocation
        /// spike to compute sixteen characters. The bytes hashed are the same bytes either way, so the digest
        /// value is unchanged; only the peak memory is. Verified against the previous form in
        /// <c>SimulationResultProvenanceTests</c>.
        /// </para>
        /// </summary>
        public static string Fingerprint(AnalyticalModel analyticalModel)
        {
            JsonObject jsonObject = analyticalModel?.AdjacencyCluster?.ToJsonObject();

            using FNV1a64Stream fNV1a64Stream = new();

            if (jsonObject is not null)
            {
                //Default writer options - not indented, default encoder - so these are byte for byte the
                //bytes JsonNode.ToJsonString() produces, which is what keeps digests recorded by earlier
                //builds comparable.
                using (System.Text.Json.Utf8JsonWriter utf8JsonWriter = new(fNV1a64Stream))
                {
                    jsonObject.WriteTo(utf8JsonWriter);
                }
            }

            return fNV1a64Stream.Digest;
        }

        /// <summary>
        /// The assessment-context half of the validity rule: an FNV-1a digest of the overheating scenarios
        /// stamped on the model, taken over their <see cref="OverheatingScenario.Key"/>s.
        ///
        /// <para><b>Why the keys, and not the scenarios' JSON</b></para>
        /// <para>
        /// <see cref="OverheatingScenario.Key"/> IS the scenario's identity, derived canonically from exactly
        /// the state that decides what is being assessed: the scope and design zone, the mitigation
        /// iteration, the system template field by field, and the operating assumptions. It is never
        /// persisted and never round-tripped, so it cannot go stale, and two machines stating the same
        /// assessment derive the same guid. Hashing it therefore binds the <i>assessment authority</i>, which
        /// is the thing that must not move underneath a set of results.
        /// </para>
        /// <para>
        /// The alternative - digesting the scenarios' serialized form - would additionally bind
        /// <see cref="SAMObject.Name"/> and <c>Source</c>, which the scenario type documents as deliberately
        /// outside its identity: presentation and provenance. A renamed scenario is the same assessment, and
        /// refusing a perfectly valid set of results over it would be a false refusal, not caution.
        /// </para>
        ///
        /// <para><b>Ordered, so a reordering is not a difference</b></para>
        /// <para>
        /// The keys are sorted before hashing: the scenarios are a set of assessments, and the order a
        /// collection happens to hold them in is not part of what they state. The count is hashed with them,
        /// so a set is never confused with a subset or a superset of itself.
        /// </para>
        ///
        /// <para><b>Small, and kept small</b></para>
        /// <para>
        /// Sixteen bytes per scenario go through the same streaming digest the cluster uses; nothing is
        /// serialized to text and no intermediate copy of anything is made. A dwelling-scale run has tens of
        /// scenarios, so this half of the record costs essentially nothing next to the cluster half - which
        /// is the second reason the two are separate.
        /// </para>
        /// </summary>
        public static string Fingerprint_Scenarios(AnalyticalModel analyticalModel)
        {
            using FNV1a64Stream fNV1a64Stream = new();

            List<Guid> guids = [];

            if (analyticalModel is not null && analyticalModel.TryGetValue(AnalyticalModelParameter.OverheatingScenarios, out SAMCollection<OverheatingScenario> overheatingScenarios) && overheatingScenarios is not null)
            {
                foreach (OverheatingScenario overheatingScenario in overheatingScenarios)
                {
                    if (overheatingScenario is not null)
                    {
                        guids.Add(overheatingScenario.Key);
                    }
                }
            }

            guids.Sort();

            //The count first, so no set of scenarios digests to the same value as a differently sized one
            //whose keys happen to concatenate identically.
            byte[] bytes_Count = BitConverter.GetBytes(guids.Count);
            fNV1a64Stream.Write(bytes_Count, 0, bytes_Count.Length);

            foreach (Guid guid in guids)
            {
                byte[] bytes = guid.ToByteArray();

                fNV1a64Stream.Write(bytes, 0, bytes.Length);
            }

            return fNV1a64Stream.Digest;
        }

        /// <summary>
        /// A write-only sink that digests what is written to it and keeps none of it - FNV-1a, 64 bit, the
        /// same digest <c>PartOCanonicalTBD</c> uses and for the same reason: the question is whether an
        /// accidental change collides, and 64 bits answers that.
        /// </summary>
        private sealed class FNV1a64Stream : Stream
        {
            private ulong hash = 14695981039346656037;

            /// <summary>The digest of everything written so far, as sixteen lower-case hex characters.</summary>
            public string Digest => hash.ToString("x16", System.Globalization.CultureInfo.InvariantCulture);

            public override bool CanRead => false;

            public override bool CanSeek => false;

            public override bool CanWrite => true;

            public override long Length => throw new NotSupportedException();

            public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

            public override void Flush()
            {
            }

            public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

            public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

            public override void SetLength(long value) => throw new NotSupportedException();

            //The array form is the only one to override here: this assembly is netstandard2.0, where
            //Stream.Write(ReadOnlySpan<byte>) is neither present nor virtual, so every flush arrives through
            //this one. Held in a local through the loop so the digest is not re-read from the field per byte.
            public override void Write(byte[] buffer, int offset, int count)
            {
                if (buffer is null)
                {
                    throw new ArgumentNullException(nameof(buffer));
                }

                ulong result = hash;

                for (int i = offset; i < offset + count; i++)
                {
                    result ^= buffer[i];
                    result *= 1099511628211;
                }

                hash = result;
            }
        }

        /// <summary>
        /// Whether this record states all of what it must - the results file's path, length and write time,
        /// the model fingerprint and the scenario fingerprint. A record missing any of them is refused
        /// wholesale rather than validated on the rest; see the type's remarks for why there is no partial
        /// form.
        /// </summary>
        public bool IsComplete => !string.IsNullOrWhiteSpace(Path_TSD)
            && Length_TSD >= 0
            && Timestamp_TSD >= 0
            && !string.IsNullOrWhiteSpace(Fingerprint_Model)
            && !string.IsNullOrWhiteSpace(Fingerprint_OverheatingScenarios);

        /// <summary>
        /// Whether the file at <paramref name="path_TSD"/> is exactly the results this record was taken
        /// from: it exists, and its length and write time are the recorded ones. Both are checked, so a
        /// rewrite landing inside the filesystem's timestamp granularity is still seen - and a record that
        /// never captured either is never satisfied by any file.
        /// </summary>
        public bool IsCurrent(string path_TSD)
        {
            if (string.IsNullOrWhiteSpace(path_TSD) || Length_TSD < 0 || Timestamp_TSD < 0)
            {
                return false;
            }

            FileInfo fileInfo = new(path_TSD);

            return fileInfo.Exists && fileInfo.Length == Length_TSD && fileInfo.LastWriteTimeUtc.Ticks == Timestamp_TSD;
        }

        /// <summary>
        /// Whether a model carrying this record is still the model the results were produced from, on both
        /// the halves the model itself carries: its design state and its overheating scenarios. Fingerprints
        /// that were never recorded fail here rather than passing - absence is not a match.
        /// </summary>
        public bool IsCurrent(AnalyticalModel analyticalModel)
        {
            if (string.IsNullOrWhiteSpace(Fingerprint_Model) || string.IsNullOrWhiteSpace(Fingerprint_OverheatingScenarios))
            {
                return false;
            }

            return string.Equals(Fingerprint_Model, Fingerprint(analyticalModel), StringComparison.Ordinal)
                && string.Equals(Fingerprint_OverheatingScenarios, Fingerprint_Scenarios(analyticalModel), StringComparison.Ordinal);
        }

        /// <summary>
        /// Resolves the results file this record points at, validating the whole rule: the record is complete,
        /// a file candidate matches <see cref="IsCurrent(string)"/>, the model's design state matches
        /// <see cref="Fingerprint(AnalyticalModel)"/>, and its overheating scenarios match
        /// <see cref="Fingerprint_Scenarios(AnalyticalModel)"/>. Results are accepted only where they are
        /// provably the ones this model was produced from, under the scenarios it was assessed with. Every
        /// part must pass; none is guessed past.
        /// <para>
        /// <b>Two candidates, in order.</b> The recorded absolute path first. Where that fails - the whole
        /// output folder moved, or the file there was replaced - the file of the same name beside
        /// <paramref name="path_Model"/>, the file this record travelled with, is tried. The name only ever
        /// <i>locates</i> a candidate; the recorded length and write time are what accept or refuse it.
        /// </para>
        /// <para>
        /// <b>Cheapest first, deliberately.</b> Completeness is a handful of field tests, so an unusable
        /// record costs nothing. The file checks are then filesystem stats. Only after both does the model
        /// check digest the whole adjacency cluster, which costs in proportion to the size of the project -
        /// so a record whose results are absent, the ordinary case on a machine that never held them, is
        /// refused without digesting anything. The scenario digest is last only because it is meaningless
        /// before the model it belongs to has been accepted; it is by far the smaller of the two.
        /// </para>
        /// </summary>
        /// <param name="analyticalModel">The model carrying this record (reopened from disk).</param>
        /// <param name="path_Model">The file the model was opened from, if known.</param>
        /// <param name="path_TSD">The resolved results file.</param>
        /// <param name="refusal">Why no results file could be validated, or null where one was.</param>
        public bool TryResolvePath_TSD(AnalyticalModel analyticalModel, string path_Model, out string path_TSD, out string refusal)
        {
            path_TSD = null;
            refusal = null;

            if (string.IsNullOrWhiteSpace(Path_TSD))
            {
                refusal = "The model records no simulation results file, so its results cannot be reviewed without running the simulation again.";

                return false;
            }

            //Fail closed on an incomplete record, before anything is stat'ed or hashed. A record that cannot
            //state the whole rule does not get to state part of it - see the type's remarks.
            if (Length_TSD < 0 || Timestamp_TSD < 0)
            {
                refusal = string.Format("The model's record of the simulation results at '{0}' does not state their size and write time, so those results cannot be shown to be the ones it was produced from. Re-run the simulation to review results for this model.", Path_TSD);

                return false;
            }

            if (string.IsNullOrWhiteSpace(Fingerprint_Model))
            {
                refusal = string.Format("The model's record of the simulation results at '{0}' does not state the design they were produced from, so the model cannot be shown to be unchanged since. Re-run the simulation to review results for this model.", Path_TSD);

                return false;
            }

            if (string.IsNullOrWhiteSpace(Fingerprint_OverheatingScenarios))
            {
                refusal = string.Format("The model's record of the simulation results at '{0}' does not state the overheating scenarios they were assessed under, so those results cannot be shown to belong to the scenarios this model now carries. Re-run the simulation to review results for this model.", Path_TSD);

                return false;
            }

            //The results side next, because it is the cheap half: three filesystem stats against a recorded
            //path, length and write time. The model half below digests the whole adjacency cluster, which
            //costs in proportion to the size of the project - so a run whose results are simply gone, the
            //ordinary case on a machine that never held them, is refused without paying for it.
            string path_Candidate = null;

            if (IsCurrent(Path_TSD))
            {
                path_Candidate = Path_TSD;
            }
            else
            {
                //The folder the run wrote to may have moved wholesale; the model file and its results move
                //together then. The same length/timestamp check applies to the fallback - the name locates the
                //candidate, never validates it.
                string fileName = Path.GetFileName(Path_TSD);
                if (!string.IsNullOrWhiteSpace(path_Model) && !string.IsNullOrWhiteSpace(fileName))
                {
                    string path_BesideModel = Path.Combine(Path.GetDirectoryName(path_Model), fileName);
                    if (!string.Equals(path_BesideModel, Path_TSD, StringComparison.OrdinalIgnoreCase) && IsCurrent(path_BesideModel))
                    {
                        path_Candidate = path_BesideModel;
                    }
                }
            }

            if (path_Candidate is null)
            {
                refusal = File.Exists(Path_TSD)
                    ? string.Format("The simulation results at '{0}' have been rewritten since this model was produced from them, so they are no longer the results that belong with it. Re-run the simulation to review the current results.", Path_TSD)
                    : string.Format("The simulation results this model was produced from are no longer at '{0}'. Re-run the simulation to produce them again.", Path_TSD);

                return false;
            }

            //And the model half: a model edited since its run must not be paired with results the unedited
            //design produced, however intact the file is.
            if (!string.Equals(Fingerprint_Model, Fingerprint(analyticalModel), StringComparison.Ordinal))
            {
                refusal = string.Format("The model has changed since the simulation results at '{0}' were produced from it, so those results no longer describe it. Re-run the simulation to review results for the current model.", Path_TSD);

                return false;
            }

            //And the assessment authority: the scenarios decide which TM59 criterion applies to which space,
            //and they are model-level state the cluster fingerprint above cannot see. Changed scenarios over
            //unchanged results would reassess an old run against an assessment nobody ran.
            if (!string.Equals(Fingerprint_OverheatingScenarios, Fingerprint_Scenarios(analyticalModel), StringComparison.Ordinal))
            {
                refusal = string.Format("The overheating scenarios on this model are not the ones the simulation results at '{0}' were assessed under, so those results cannot be reviewed against them. Prepare the iteration again and re-run the simulation.", Path_TSD);

                return false;
            }

            path_TSD = path_Candidate;

            return true;
        }

        public override bool FromJsonObject(JsonObject jsonObject)
        {
            if (!base.FromJsonObject(jsonObject))
            {
                return false;
            }

            if (jsonObject.ContainsKey("Path_TSD"))
            {
                Path_TSD = jsonObject["Path_TSD"]?.GetValue<string>();
            }

            if (jsonObject.ContainsKey("Length_TSD"))
            {
                Length_TSD = jsonObject["Length_TSD"]?.GetValue<long>() ?? -1;
            }

            if (jsonObject.ContainsKey("Timestamp_TSD"))
            {
                Timestamp_TSD = jsonObject["Timestamp_TSD"]?.GetValue<long>() ?? -1;
            }

            if (jsonObject.ContainsKey("Fingerprint_Model"))
            {
                Fingerprint_Model = jsonObject["Fingerprint_Model"]?.GetValue<string>() ?? string.Empty;
            }

            if (jsonObject.ContainsKey("Fingerprint_OverheatingScenarios"))
            {
                Fingerprint_OverheatingScenarios = jsonObject["Fingerprint_OverheatingScenarios"]?.GetValue<string>() ?? string.Empty;
            }

            return true;
        }

        public override JsonObject ToJsonObject()
        {
            JsonObject result = base.ToJsonObject();
            if (result is null)
            {
                return result;
            }

            if (Path_TSD is not null)
            {
                result["Path_TSD"] = Path_TSD;
            }

            result["Length_TSD"] = Length_TSD;
            result["Timestamp_TSD"] = Timestamp_TSD;

            if (!string.IsNullOrWhiteSpace(Fingerprint_Model))
            {
                result["Fingerprint_Model"] = Fingerprint_Model;
            }

            if (!string.IsNullOrWhiteSpace(Fingerprint_OverheatingScenarios))
            {
                result["Fingerprint_OverheatingScenarios"] = Fingerprint_OverheatingScenarios;
            }

            return result;
        }
    }
}
