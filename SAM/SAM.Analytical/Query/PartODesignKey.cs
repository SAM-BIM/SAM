// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;

namespace SAM.Analytical
{
    public static partial class Query
    {
        /// <summary>The schema tag of <see cref="PartODesignKey"/>; it is the first thing hashed and the prefix of every key.</summary>
        public const string PartODesignKeySchema = "PartODesignKey:v1";

        /// <summary>
        /// A stable <b>semantic</b> key of a Part O design model, for one question: <i>has the engineering meaning of this design
        /// changed since a Part O result was produced from it?</i>
        ///
        /// <para><b>Why it is not <c>SimulationResultProvenance.Fingerprint</c></b></para>
        /// <para>
        /// That fingerprint hashes the model's serialized bytes. Several Part O design inputs are <c>SAMObject</c>s that mint
        /// <c>Guid.NewGuid()</c> whenever they are constructed - <see cref="PartOEquipmentSelection"/>, every
        /// <see cref="VentilationUnitReference"/> in its pool, <see cref="PartOProjectTestVentilationUnit"/> - and the UI rebuilds
        /// them each time it reads them back, so the same selection written twice has two fingerprints. A saved result then
        /// reported its design as changed when nothing had been decided differently. Every model <see cref="ParameterSet"/> also
        /// carries a guid derived from the build that created it. None of those identifiers is engineering meaning.
        /// </para>
        ///
        /// <para><b>What the key is</b></para>
        /// <para>
        /// SHA-256 over <b>one</b> deterministic canonical stream - never a mix of this and another hash. The stream is, in order:
        /// the schema tag; the cluster, material library, profile library and location serialized exactly as the model fingerprint
        /// serializes them (a design's cluster identities, such as space and zone guids, are the baseline and are meaning);
        /// the model's parameters <b>without</b> the parameter-set guids and without the parameters the model fingerprint
        /// already excludes (provenance, scenarios, case labels, view state) and without the four Part O design inputs; then
        /// those Part O inputs as canonical lines read by identity, never by guid:
        /// </para>
        /// <list type="bullet">
        /// <item><b>equipment selection</b> - the mode and the permitted products as manufacturer/model/reference, de-duplicated and
        /// ordered. An absent selection is the historic default (<see cref="Enums.PartOEquipmentSelectionMode.AutomaticAllProducts"/>,
        /// no pool), so absent and explicitly default are the same design;</item>
        /// <item><b>project test product</b> - name and both capacities where it is usable, otherwise none, as it offers nothing then;</item>
        /// <item><b>hand-picked per-dwelling products</b> - zone guid to product identity;</item>
        /// <item><b>dwelling strategies</b> - each strategy's <see cref="PartODwellingStrategy.CanonicalText"/>.</item>
        /// </list>
        ///
        /// <para><b>What it deliberately does not see</b></para>
        /// <para>
        /// The model's name, guid, description and address; the view state; and anything that only a result carries. A Part O
        /// design never carries result state, so there is nothing of it to exclude beyond what the model fingerprint excludes.
        /// </para>
        ///
        /// <para>
        /// A change to what this key covers is a change of <see cref="PartODesignKeySchema"/>: old keys then compare as different,
        /// which is the fail-closed direction. <see cref="SimulationResultProvenance.Fingerprint(AnalyticalModel)"/> is untouched.
        /// </para>
        /// </summary>
        /// <param name="analyticalModel">The design model. Not modified.</param>
        /// <returns><c>PartODesignKey:v1:</c> and 64 lower-case hex characters, or null for no model.</returns>
        public static string PartODesignKey(this AnalyticalModel analyticalModel)
        {
            if (analyticalModel is null)
            {
                return null;
            }

            using IncrementalHash incrementalHash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            using (PartODesignKeyStream partODesignKeyStream = new(incrementalHash))
            {
                byte[] bytes_Schema = Encoding.UTF8.GetBytes(PartODesignKeySchema + "\n");
                partODesignKeyStream.Write(bytes_Schema, 0, bytes_Schema.Length);

                //ORDER AND TAGS ARE PART OF THE KEY. Each section is tagged, and JSON sections are closed by 0xFF, a byte
                //valid UTF-8 never contains, so one section cannot bleed into the next.
                PartODesignKeySection(partODesignKeyStream, 1, analyticalModel.AdjacencyCluster?.ToJsonObject());
                PartODesignKeySection(partODesignKeyStream, 2, analyticalModel.MaterialLibrary?.ToJsonObject());
                PartODesignKeySection(partODesignKeyStream, 3, analyticalModel.ProfileLibrary?.ToJsonObject());
                PartODesignKeySection(partODesignKeyStream, 4, analyticalModel.Location?.ToJsonObject());

                PartODesignKeyLines(partODesignKeyStream, 5, PartODesignKeyParameterLines(analyticalModel));
                PartODesignKeyLines(partODesignKeyStream, 6, PartODesignKeyConfigurationLines(analyticalModel));
            }

            byte[] hash = incrementalHash.GetHashAndReset();

            StringBuilder stringBuilder = new(PartODesignKeySchema.Length + 1 + hash.Length * 2);
            stringBuilder.Append(PartODesignKeySchema).Append(':');
            foreach (byte @byte in hash)
            {
                stringBuilder.Append(@byte.ToString("x2", CultureInfo.InvariantCulture));
            }

            return stringBuilder.ToString();
        }

        private static void PartODesignKeySection(Stream stream, byte section, JsonNode jsonNode)
        {
            stream.WriteByte(section);

            if (jsonNode is not null)
            {
                //Streamed - the cluster is as large as the project, so no second copy of it is ever held.
                using System.Text.Json.Utf8JsonWriter utf8JsonWriter = new(stream);

                jsonNode.WriteTo(utf8JsonWriter);
            }

            stream.WriteByte(0xFF);
        }

        private static void PartODesignKeyLines(Stream stream, byte section, List<string> lines)
        {
            lines.Sort(StringComparer.Ordinal);

            stream.WriteByte(section);

            byte[] bytes = Encoding.UTF8.GetBytes(lines.Count.ToString(CultureInfo.InvariantCulture) + "\n");
            stream.Write(bytes, 0, bytes.Length);

            foreach (string line in lines)
            {
                byte[] bytes_Line = Encoding.UTF8.GetBytes(line);
                byte[] bytes_Length = Encoding.UTF8.GetBytes(bytes_Line.Length.ToString(CultureInfo.InvariantCulture) + ":");

                stream.Write(bytes_Length, 0, bytes_Length.Length);
                stream.Write(bytes_Line, 0, bytes_Line.Length);
                stream.WriteByte((byte)'\n');
            }

            stream.WriteByte(0xFF);
        }

        /// <summary>
        /// The model's own parameters as canonical lines: set name, parameter name and the parameter's value as JSON. The set's
        /// guid is not written (it is derived from the build that created the set), and neither are the parameters the model
        /// fingerprint excludes nor the four Part O inputs, which are read semantically instead.
        /// </summary>
        private static List<string> PartODesignKeyParameterLines(AnalyticalModel analyticalModel)
        {
            List<string> result = [];

            List<ParameterSet> parameterSets = analyticalModel.GetParameterSets();
            if (parameterSets is null)
            {
                return result;
            }

            List<string> names_Excluded = SimulationResultProvenance.ParameterNames_Excluded();
            foreach (AnalyticalModelParameter analyticalModelParameter in PartODesignInputParameters)
            {
                string name = Core.Attributes.ParameterProperties.Get(analyticalModelParameter)?.Name;
                if (!string.IsNullOrEmpty(name))
                {
                    names_Excluded.Add(name);
                }
            }

            foreach (ParameterSet parameterSet in parameterSets)
            {
                if (parameterSet is null)
                {
                    continue;
                }

                foreach (string name_Excluded in names_Excluded)
                {
                    parameterSet.Remove(name_Excluded);
                }

                if (parameterSet.ToJsonObject()?["Parameters"] is not JsonArray jsonArray)
                {
                    continue;
                }

                foreach (JsonNode jsonNode in jsonArray)
                {
                    if (jsonNode is JsonObject jsonObject)
                    {
                        result.Add(string.Join("|", "parameter", parameterSet.Name ?? string.Empty, jsonObject["Name"]?.ToString() ?? string.Empty, jsonObject["Value"]?.ToJsonString() ?? string.Empty));
                    }
                }
            }

            return result;
        }

        private static readonly AnalyticalModelParameter[] PartODesignInputParameters =
        [
            AnalyticalModelParameter.PartOEquipmentSelection,
            AnalyticalModelParameter.PartOProjectTestVentilationUnit,
            AnalyticalModelParameter.PartOManualEquipmentSelection,
            AnalyticalModelParameter.PartODwellingStrategies,
        ];

        /// <summary>The Part O design inputs as canonical, guid-free lines. See <see cref="PartODesignKey(AnalyticalModel)"/>.</summary>
        private static List<string> PartODesignKeyConfigurationLines(AnalyticalModel analyticalModel)
        {
            List<string> result = [];

            //Absent means the historic default - the same design as a stated default.
            PartOEquipmentSelection partOEquipmentSelection = analyticalModel.GetValue<PartOEquipmentSelection>(AnalyticalModelParameter.PartOEquipmentSelection) ?? new PartOEquipmentSelection();
            result.Add("equipment|" + partOEquipmentSelection.Mode.ToString());
            foreach (string line in (partOEquipmentSelection.AllowedVentilationUnitReferences ?? []).Select(PartODesignKeyProduct).Distinct(StringComparer.Ordinal))
            {
                result.Add("pool|" + line);
            }

            PartOProjectTestVentilationUnit partOProjectTestVentilationUnit = analyticalModel.GetValue<PartOProjectTestVentilationUnit>(AnalyticalModelParameter.PartOProjectTestVentilationUnit);
            result.Add(partOProjectTestVentilationUnit is not null && partOProjectTestVentilationUnit.IsValid
                ? string.Join("|", "testunit", partOProjectTestVentilationUnit.Name, FingerprintNumber(partOProjectTestVentilationUnit.MaximumSupplyFlowRate_Lps), FingerprintNumber(partOProjectTestVentilationUnit.MaximumExtractFlowRate_Lps))
                : "testunit|none");

            PartOManualEquipmentSelection partOManualEquipmentSelection = analyticalModel.GetValue<PartOManualEquipmentSelection>(AnalyticalModelParameter.PartOManualEquipmentSelection);
            foreach (Guid guid_Zone in partOManualEquipmentSelection?.ZoneGuids ?? [])
            {
                result.Add(string.Join("|", "manual", guid_Zone.ToString("D", CultureInfo.InvariantCulture), PartODesignKeyProduct(partOManualEquipmentSelection.Product(guid_Zone))));
            }

            PartODwellingStrategySet partODwellingStrategySet = analyticalModel.GetValue<PartODwellingStrategySet>(AnalyticalModelParameter.PartODwellingStrategies);
            if (partODwellingStrategySet is not null)
            {
                foreach (PartODwellingStrategy partODwellingStrategy in partODwellingStrategySet.Strategies)
                {
                    result.Add("strategy|" + partODwellingStrategy.CanonicalText());
                }

                foreach (Guid guid_Zone in partODwellingStrategySet.Conflicts)
                {
                    result.Add("strategy-conflict|" + guid_Zone.ToString("D", CultureInfo.InvariantCulture));
                }

                if (partODwellingStrategySet.SchemaRead != PartODwellingStrategySet.Schema)
                {
                    result.Add("strategy-schema|" + partODwellingStrategySet.SchemaRead);
                }
            }

            return result;
        }

        /// <summary>A product by the identity <see cref="VentilationUnitReference.Matches"/> compares - never its guid or display name.</summary>
        private static string PartODesignKeyProduct(VentilationUnitReference ventilationUnitReference)
        {
            return ventilationUnitReference is null
                ? "-"
                : string.Join("/", ventilationUnitReference.Manufacturer ?? string.Empty, ventilationUnitReference.Model ?? string.Empty, ventilationUnitReference.Reference ?? string.Empty);
        }

        /// <summary>A write-only sink feeding an <see cref="IncrementalHash"/>; it keeps none of what is written to it.</summary>
        private sealed class PartODesignKeyStream(IncrementalHash incrementalHash) : Stream
        {
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

            public override void Write(byte[] buffer, int offset, int count)
            {
                if (buffer is null)
                {
                    throw new ArgumentNullException(nameof(buffer));
                }

                incrementalHash.AppendData(buffer, offset, count);
            }
        }
    }
}
