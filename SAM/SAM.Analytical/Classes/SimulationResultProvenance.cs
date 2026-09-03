// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core;
using System;
using System.IO;
using System.Text.Json.Nodes;

namespace SAM.Analytical
{
    /// <summary>
    /// Which simulation results file an <see cref="AnalyticalModel"/> was produced from, and how to tell
    /// whether the two still belong together: the results file's path, length and write time, captured when
    /// the model was written, and a fingerprint of the model's own content.
    /// <para>
    /// <b>Why this exists.</b> A model saved after a full-year simulation - the per-run
    /// <c>&lt;project&gt;.json</c> the TAS workflow writes, or the user's own <c>.sam</c> afterwards - can be
    /// reopened in a later session and its overheating results reviewed WITHOUT rerunning the simulation,
    /// provided the results it pairs with are provably the ones it was produced from. This record is that
    /// proof. It is stamped onto the model the workflow returned, so it travels with the model wherever the
    /// model is saved.
    /// </para>
    /// <para>
    /// <b>The validity rule, stated rather than guessed.</b> Both halves are checked. The results side: a
    /// candidate file must exist AND match the recorded length and write time - a name alone is never enough,
    /// because two runs of one project write results at the same derived path. The model side: the reopened
    /// model's <see cref="AdjacencyCluster"/> - spaces, internal conditions, zones and their relations, the
    /// content an assessment actually reads - must hash to the recorded fingerprint, so a model edited after
    /// the run is refused rather than silently paired with results a different design produced. Either
    /// failure is reported as what it is; neither is guessed past.
    /// </para>
    /// <para>
    /// <b>Provenance only.</b> This records where the results are; it decides nothing about what they say.
    /// The assessment itself still resolves every space by identity and refuses what does not resolve.
    /// </para>
    /// </summary>
    public class SimulationResultProvenance : SAMObject
    {
        public SimulationResultProvenance()
        {
        }

        /// <summary>Captures the record from the model and its results file as they stand at this moment.</summary>
        public SimulationResultProvenance(AnalyticalModel analyticalModel, string path_TSD)
        {
            Path_TSD = path_TSD;
            Fingerprint_Model = Fingerprint(analyticalModel);

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
            }
        }

        public SimulationResultProvenance(JsonObject jsonObject)
            : base(jsonObject)
        {
        }

        /// <summary>The results file the model was produced from, as an absolute path when recorded.</summary>
        public string Path_TSD { get; set; }

        /// <summary>The results file's length in bytes when recorded.</summary>
        public long Length_TSD { get; set; } = -1;

        /// <summary>The results file's <see cref="DateTime.Ticks"/> of <see cref="FileInfo.LastWriteTimeUtc"/> when recorded.</summary>
        public long Timestamp_TSD { get; set; } = -1;

        /// <summary>
        /// The model's own content fingerprint when recorded - see <see cref="Fingerprint(AnalyticalModel)"/>.
        /// Absent (<see cref="string.Empty"/>) on a record written before it was kept: such a record is
        /// validated on its results side only.
        /// </summary>
        public string Fingerprint_Model { get; set; } = string.Empty;

        /// <summary>
        /// The model-content half of the validity rule: an FNV-1a digest of the <see cref="AdjacencyCluster"/>'s
        /// JSON - the spaces, internal conditions, zones and relations an assessment reads. Model-level
        /// parameters (view settings, this record itself) are deliberately not part of it.
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
        /// Whether the file at <paramref name="path_TSD"/> is exactly the results this record was taken
        /// from: it exists, and its length and write time are the recorded ones. Both are checked, so a
        /// rewrite landing inside the filesystem's timestamp granularity is still seen.
        /// </summary>
        public bool IsCurrent(string path_TSD)
        {
            if (string.IsNullOrWhiteSpace(path_TSD))
            {
                return false;
            }

            FileInfo fileInfo = new(path_TSD);

            return fileInfo.Exists && fileInfo.Length == Length_TSD && fileInfo.LastWriteTimeUtc.Ticks == Timestamp_TSD;
        }

        /// <summary>
        /// Whether a model carrying this record is still the model the results were produced from. True where
        /// the fingerprints match - and, for a record written before a model fingerprint was kept, true:
        /// absence is not a mismatch, the results-side check still stands.
        /// </summary>
        public bool IsCurrent(AnalyticalModel analyticalModel)
        {
            if (string.IsNullOrWhiteSpace(Fingerprint_Model))
            {
                return true;
            }

            return string.Equals(Fingerprint_Model, Fingerprint(analyticalModel), StringComparison.Ordinal);
        }

        /// <summary>
        /// Resolves the results file this record points at, validating every file candidate by
        /// <see cref="IsCurrent(string)"/> and then the model by <see cref="IsCurrent(AnalyticalModel)"/> -
        /// so results are accepted only where they are provably the ones this model was produced from. Both
        /// halves must pass; neither is guessed past.
        /// <para>
        /// <b>Two candidates, in order.</b> The recorded absolute path first. Where that fails - the whole
        /// output folder moved, or the file there was replaced - the file of the same name beside
        /// <paramref name="path_Model"/>, the file this record travelled with, is tried. The name only ever
        /// <i>locates</i> a candidate; the recorded length and write time are what accept or refuse it.
        /// </para>
        /// <para>
        /// <b>Cheap half first, deliberately.</b> The file checks are filesystem stats; the model check
        /// digests the whole adjacency cluster and so costs in proportion to the size of the project. A
        /// record whose results are absent - the ordinary case on a machine that never held them - is
        /// therefore refused without digesting anything.
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

            //The results side first, because it is the cheap half: three filesystem stats against a recorded
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
            if (!IsCurrent(analyticalModel))
            {
                refusal = string.Format("The model has changed since the simulation results at '{0}' were produced from it, so those results no longer describe it. Re-run the simulation to review results for the current model.", Path_TSD);

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

            return result;
        }
    }
}
