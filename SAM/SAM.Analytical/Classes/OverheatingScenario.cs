// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Enums;
using SAM.Core;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.Json.Nodes;

namespace SAM.Analytical
{
    /// <summary>
    /// One Approved Document O overheating assessment, stated as engineering intent: <b>which</b> part of
    /// the building is being assessed, <b>at what mitigation stage</b>, <b>with which system</b>, and
    /// <b>under what operating assumptions</b>.
    /// <para>
    /// <b>Intent, not execution.</b> A scenario says what is to be assessed and says nothing whatever about
    /// how. There is no simulation engine here, no file path, no TSD or TPD, no plant-room object and no
    /// result. That boundary is what lets the same scenario be answered by a simple TSD run today and a
    /// full HVAC TPD run later without its identity moving - and it is why the routing decision belongs to
    /// the TAS adapter, which is in a different assembly, on the other side of a reference this one does
    /// not have.
    /// </para>
    /// <para>
    /// <b><see cref="Key"/> is derived, never generated and never stored.</b> Two engineers on two machines
    /// who state the same assessment get the same guid, so results can be attributed to a scenario that was
    /// never saved anywhere, and a scenario reloaded from JSON is recognisably the same one. A
    /// <c>Guid.NewGuid()</c> would have made every rebuild of the same intent a new assessment, which is
    /// exactly the failure the Part F annotation keys were rewritten to avoid.
    /// </para>
    ///
    /// <para><b>What the key is made of, and why each part is in it</b></para>
    /// <list type="bullet">
    /// <item><b>Scope and design zone guid</b> - the thing being assessed, by stable design identity.
    /// <b>Never a name</b>: every flat in a block has a "Bedroom 2" and half of them are called "Flat 2" in
    /// somebody's model. Renaming a flat must not orphan its assessment, and two flats that happen to share
    /// a name must not share one.</item>
    /// <item><b>Iteration</b> - the mitigation stage. The same dwelling at base provision and with acoustic
    /// restriction are two engineering answers.</item>
    /// <item><b>System template</b> - the existing <see cref="SystemTemplate"/> identity, field by field.
    /// <b>No second equipment vocabulary is introduced here.</b> <c>MVRE</c> already is SAM's heat-recovery
    /// ventilation and its template is the one with the exchanger in it; adding an <c>MVHR</c> alongside
    /// would split an established concept in two. Boost and summer bypass are operating states of a system,
    /// so they belong in the assumptions, not in a new system type.</item>
    /// <item><b>Operating assumptions</b> - what makes two runs of the same dwelling on the same system two
    /// different assessments.</item>
    /// </list>
    ///
    /// <para><b>What is deliberately NOT in the key</b></para>
    /// <list type="bullet">
    /// <item><see cref="Name"/> - presentation. A scenario renamed is the same assessment.</item>
    /// <item><see cref="Source"/> - provenance only, exactly as on <c>TMOverheatingCalculator</c>. Where an
    /// answer came from is not part of what the question was.</item>
    /// <item>Anything engine-shaped - TAS, TSD, TPD, file paths, weather files, run settings. None of it is
    /// a member of this type at all, so it cannot reach the key even by accident.</item>
    /// <item>Simulation output, view settings, and the time the scenario was created. A key that moved when
    /// a result arrived would be useless for attributing that result.</item>
    /// </list>
    ///
    /// <para><b>The key cannot go stale</b></para>
    /// <para>
    /// It is recomputed from current state on every read and is never cached, never persisted and never
    /// round-tripped through JSON. The identity-defining state has no public setter, and the two mutable
    /// objects it is built from - <see cref="SystemTemplate"/> and
    /// <see cref="OverheatingOperatingAssumptions"/> - are copied on the way in and copied on the way out,
    /// so neither the caller's instance nor one read back off the property is the one the key is derived
    /// from. There is therefore no sequence of calls that leaves a scenario reporting a key that does not
    /// describe it.
    /// </para>
    ///
    /// <para><b>The encoding is canonical, not convenient</b></para>
    /// <para>
    /// Every component is UTF-8 and length-prefixed, in one fixed order, behind the schema marker
    /// <c>OverheatingScenario:v1</c>. Length prefixes rather than separators because concatenation is
    /// ambiguous - an assumption called <c>AB</c> with value <c>C</c> and one called <c>A</c> with value
    /// <c>BC</c> must not derive one key. Enums are hashed by <b>name</b>, so inserting a member into
    /// <see cref="PartOIteration"/> later cannot silently renumber existing assessments. And UTF-8 rather
    /// than <c>Core.Query.ComputeHash</c>, which encodes ASCII: it maps every non-ASCII character to
    /// <c>?</c>, so under it <c>café</c> and <c>cafè</c> are the same string. Fine for a checksum, unusable
    /// for an identity.
    /// </para>
    /// </summary>
    public class OverheatingScenario : IJSAMObject, IAnalyticalObject
    {
        /// <summary>
        /// The identity schema marker, hashed first. <b>Bump it only for a deliberate, breaking change to
        /// what the key is made of</b> - doing so re-keys every scenario in existence, which is the point:
        /// a key derived under different rules must not be mistaken for one derived under these.
        /// </summary>
        public const string IdentitySchema = "OverheatingScenario:v1";

        /// <summary>
        /// Namespace for the derivation, so a scenario key can only ever collide with another scenario key
        /// and never with a real model guid or a Part F annotation key.
        /// </summary>
        private static readonly Guid guid_Namespace = new("2b7c9d14-5e63-4a8f-9c02-7d5a1e46b3f8");

        private PartOAssessmentScope partOAssessmentScope = PartOAssessmentScope.Undefined;
        private Guid guid_Zone = Guid.Empty;
        private PartOIteration partOIteration = PartOIteration.Undefined;
        private SystemTemplate systemTemplate = null;
        private OverheatingOperatingAssumptions overheatingOperatingAssumptions = new();

        public OverheatingScenario()
        {

        }

        /// <param name="partOAssessmentScope">Whether this assesses a dwelling or a common space.</param>
        /// <param name="guid_Zone">
        /// The <b>design</b> zone's guid - the identity that survives a rename and a simulation round trip.
        /// </param>
        /// <param name="partOIteration">The mitigation stage stated.</param>
        /// <param name="systemTemplate">
        /// The system identity, copied. An invalid or entirely blank template is stored as none, because
        /// "no system stated" and "a system with nothing stated about it" are the same statement.
        /// </param>
        /// <param name="overheatingOperatingAssumptions">The operating assumptions, copied.</param>
        public OverheatingScenario(PartOAssessmentScope partOAssessmentScope, Guid guid_Zone, PartOIteration partOIteration, SystemTemplate systemTemplate = null, OverheatingOperatingAssumptions overheatingOperatingAssumptions = null)
        {
            this.partOAssessmentScope = partOAssessmentScope;
            this.guid_Zone = guid_Zone;
            this.partOIteration = partOIteration;

            //Copied, not referenced: SystemTemplate is mutable, and a caller who kept their instance could
            //otherwise change this scenario's identity after the fact.
            this.systemTemplate = systemTemplate != null && systemTemplate.IsValid ? new SystemTemplate(systemTemplate) : null;
            this.overheatingOperatingAssumptions = new OverheatingOperatingAssumptions(overheatingOperatingAssumptions);
        }

        public OverheatingScenario(OverheatingScenario overheatingScenario)
        {
            if (overheatingScenario != null)
            {
                partOAssessmentScope = overheatingScenario.partOAssessmentScope;
                guid_Zone = overheatingScenario.guid_Zone;
                partOIteration = overheatingScenario.partOIteration;
                systemTemplate = overheatingScenario.systemTemplate == null ? null : new SystemTemplate(overheatingScenario.systemTemplate);
                overheatingOperatingAssumptions = new OverheatingOperatingAssumptions(overheatingScenario.overheatingOperatingAssumptions);

                Name = overheatingScenario.Name;
                Source = overheatingScenario.Source;
            }
        }

        public OverheatingScenario(JsonObject jsonObject)
        {
            FromJsonObject(jsonObject);
        }

        /// <summary>
        /// A label for people. <b>Presentation only</b> - it takes no part in <see cref="Key"/>, so a
        /// scenario renamed is still the same assessment and two scenarios sharing a name are still two.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Where this scenario came from - a model name, a workflow, a user. <b>Provenance only</b>, on the
        /// same terms as <c>TMOverheatingCalculator.Source</c>: it names no object, owns no result and
        /// takes no part in any identity.
        /// </summary>
        public string Source { get; set; }

        /// <summary>Whether a dwelling or a common space is being assessed. Identity-defining.</summary>
        public PartOAssessmentScope Scope => partOAssessmentScope;

        /// <summary>
        /// The design zone being assessed, by guid. Identity-defining, and <b>the reason a name is not</b>.
        /// </summary>
        public Guid ZoneGuid => guid_Zone;

        /// <summary>The mitigation stage stated. Identity-defining.</summary>
        public PartOIteration Iteration => partOIteration;

        /// <summary>
        /// The system identity, or null where none is stated. Identity-defining. <b>A copy</b> - changing
        /// what this returns changes nothing.
        /// </summary>
        public SystemTemplate SystemTemplate => systemTemplate == null ? null : new SystemTemplate(systemTemplate);

        /// <summary>
        /// The operating assumptions. Identity-defining. <b>A copy</b> - changing what this returns changes
        /// nothing.
        /// </summary>
        public OverheatingOperatingAssumptions OperatingAssumptions => new(overheatingOperatingAssumptions);

        /// <summary>
        /// The scenario's identity, derived from its engineering content. Recomputed on every read, so it
        /// can never disagree with the state it describes.
        /// </summary>
        public Guid Key => Derive();

        /// <summary>
        /// Whether the scenario names something assessable: a stated scope over a real design zone. The
        /// iteration and the system may legitimately be unstated.
        /// </summary>
        public bool IsValid => partOAssessmentScope != PartOAssessmentScope.Undefined && guid_Zone != Guid.Empty;

        /// <summary>Two scenarios are the same assessment when they derive the same key.</summary>
        public override bool Equals(object obj)
        {
            return obj is OverheatingScenario overheatingScenario && Key == overheatingScenario.Key;
        }

        public override int GetHashCode()
        {
            return Key.GetHashCode();
        }

        public bool FromJsonObject(JsonObject jsonObject)
        {
            if (jsonObject == null)
            {
                return false;
            }

            Name = jsonObject.ContainsKey("Name") ? jsonObject["Name"]?.GetValue<string>() : null;
            Source = jsonObject.ContainsKey("Source") ? jsonObject["Source"]?.GetValue<string>() : null;

            //Enums are read by name. An unrecognised name is Undefined rather than an exception: a scenario
            //written by a later version must not make a file unreadable, and Undefined is visibly not an
            //assessment rather than quietly the first member.
            string text;

            partOAssessmentScope = PartOAssessmentScope.Undefined;
            text = jsonObject.ContainsKey("Scope") ? jsonObject["Scope"]?.GetValue<string>() : null;
            if (!string.IsNullOrWhiteSpace(text) && Enum.TryParse(text, out PartOAssessmentScope partOAssessmentScope_Temp))
            {
                partOAssessmentScope = partOAssessmentScope_Temp;
            }

            partOIteration = PartOIteration.Undefined;
            text = jsonObject.ContainsKey("Iteration") ? jsonObject["Iteration"]?.GetValue<string>() : null;
            if (!string.IsNullOrWhiteSpace(text) && Enum.TryParse(text, out PartOIteration partOIteration_Temp))
            {
                partOIteration = partOIteration_Temp;
            }

            guid_Zone = Guid.Empty;
            text = jsonObject.ContainsKey("ZoneGuid") ? jsonObject["ZoneGuid"]?.GetValue<string>() : null;
            if (!string.IsNullOrWhiteSpace(text) && Guid.TryParse(text, out Guid guid_Zone_Temp))
            {
                guid_Zone = guid_Zone_Temp;
            }

            systemTemplate = null;
            if (jsonObject["SystemTemplate"] is JsonObject jsonObject_SystemTemplate)
            {
                SystemTemplate systemTemplate_Temp = new(jsonObject_SystemTemplate);
                systemTemplate = systemTemplate_Temp.IsValid ? systemTemplate_Temp : null;
            }

            overheatingOperatingAssumptions = jsonObject["OperatingAssumptions"] is JsonObject jsonObject_OperatingAssumptions ? new OverheatingOperatingAssumptions(jsonObject_OperatingAssumptions) : new OverheatingOperatingAssumptions();

            return true;
        }

        public JsonObject ToJsonObject()
        {
            //Key is deliberately absent. Writing it would create a second, storable copy of an identity that
            //is supposed to have exactly one source - the state below - and a file edited by hand or written
            //by an older version could then assert a key that does not describe its own contents.
            JsonObject jsonObject = new()
            {
                ["_type"] = Core.Query.FullTypeName(this)
            };

            if (Name != null)
            {
                jsonObject["Name"] = Name;
            }

            if (Source != null)
            {
                jsonObject["Source"] = Source;
            }

            //By name, matching the derivation: a renumbered enum must not re-key stored scenarios.
            jsonObject["Scope"] = partOAssessmentScope.ToString();
            jsonObject["ZoneGuid"] = guid_Zone.ToString("D", CultureInfo.InvariantCulture);
            jsonObject["Iteration"] = partOIteration.ToString();

            if (systemTemplate != null)
            {
                jsonObject["SystemTemplate"] = systemTemplate.ToJsonObject();
            }

            jsonObject["OperatingAssumptions"] = overheatingOperatingAssumptions.ToJsonObject();

            return jsonObject;
        }

        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, "{0} {1} {2} [{3}]", partOAssessmentScope, partOIteration, systemTemplate == null ? "-" : systemTemplate.ToString(), guid_Zone.ToString("D", CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Derives the key: the namespace, then every identity-defining component in one fixed order, each
        /// UTF-8 and length-prefixed; SHA-256; the first sixteen bytes stamped with the version and variant
        /// bits, in the manner of RFC 4122's name-based UUIDs and exactly as <c>PartFAnnotationKey</c> does.
        /// <para>
        /// The hash is a spreading function and not a security primitive - the whole point is that it is
        /// reproducible, which is why it is not salted and must never become so.
        /// </para>
        /// </summary>
        private Guid Derive()
        {
            List<byte> bytes = [.. guid_Namespace.ToByteArray()];

            //First, and before anything that could change: a key derived under a different schema must be a
            //different key.
            Append(bytes, IdentitySchema);

            //The thing being assessed. Guid formatted invariantly as text so every component of the
            //derivation goes through one encoding rule and none through a byte layout.
            Append(bytes, partOAssessmentScope.ToString());
            Append(bytes, guid_Zone.ToString("D", CultureInfo.InvariantCulture));

            //The mitigation stage.
            Append(bytes, partOIteration.ToString());

            //The system, field by field rather than through ToString(): ToString() is a display format that
            //may reasonably be improved one day, and improving it must not re-key every assessment.
            Append(bytes, systemTemplate?.Ventilation);
            Append(bytes, systemTemplate?.Heating);
            Append(bytes, systemTemplate?.Cooling);
            Append(bytes, systemTemplate?.PlantRoom);
            Append(bytes, systemTemplate?.Controls);
            Append(bytes, systemTemplate?.Version);

            //The assumptions, in their canonical ordinal order, counted first so a trailing assumption
            //cannot be confused with a longer value on the one before it.
            List<KeyValuePair<string, string>> keyValuePairs = overheatingOperatingAssumptions.ToList();

            AppendLength(bytes, keyValuePairs.Count);

            foreach (KeyValuePair<string, string> keyValuePair in keyValuePairs)
            {
                Append(bytes, keyValuePair.Key);
                Append(bytes, keyValuePair.Value);
            }

            byte[] hash;

            using (System.Security.Cryptography.SHA256 sHA256 = System.Security.Cryptography.SHA256.Create())
            {
                hash = sHA256.ComputeHash([.. bytes]);
            }

            byte[] result = new byte[16];
            Array.Copy(hash, result, 16);

            //Version 8 (custom) and the RFC 4122 variant, so this is a valid guid and is visibly not a model
            //identity that happens to look similar.
            result[7] = (byte)((result[7] & 0x0F) | 0x80);
            result[8] = (byte)((result[8] & 0x3F) | 0x80);

            return new Guid(result);
        }

        /// <summary>
        /// Appends one component: its UTF-8 length, then its UTF-8 bytes. Length-prefixed rather than
        /// separated, so no combination of component values can produce the byte sequence of a different
        /// combination. A null component is written as length -1, which is distinct from an empty one:
        /// "not stated" and "stated as blank" are different statements.
        /// </summary>
        private static void Append(List<byte> bytes, string text)
        {
            if (text == null)
            {
                AppendLength(bytes, -1);
                return;
            }

            byte[] bytes_Text = Encoding.UTF8.GetBytes(text);

            AppendLength(bytes, bytes_Text.Length);
            bytes.AddRange(bytes_Text);
        }

        /// <summary>
        /// Writes an int as four bytes, least significant first, explicitly rather than through
        /// <c>BitConverter</c> - which is endian-dependent, and a key must not depend on the architecture
        /// that derived it.
        /// </summary>
        private static void AppendLength(List<byte> bytes, int value)
        {
            unchecked
            {
                bytes.Add((byte)value);
                bytes.Add((byte)(value >> 8));
                bytes.Add((byte)(value >> 16));
                bytes.Add((byte)(value >> 24));
            }
        }
    }
}
