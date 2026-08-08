// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical;
using SAM.Analytical.Enums;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json.Nodes;
using Xunit;

namespace SAM.Tests
{
    /// <summary>
    /// <c>OverheatingScenario</c>'s derived identity.
    /// <para>
    /// The whole class exists so that stating the same Approved Document O assessment twice - on two
    /// machines, before and after a save, in two different orders - produces one identity, and stating a
    /// different assessment produces a different one. Everything below is one half of that: either
    /// something that must not change the key, or something that must.
    /// </para>
    /// <para>
    /// Fixture is the real validation shape: the three-flat block with a communal corridor that the TAS run
    /// used, with MVRE - SAM's heat-recovery ventilation - on the flats.
    /// </para>
    /// </summary>
    public class OverheatingScenarioTests
    {
        private static readonly Guid guid_Flat_1 = new("11111111-1111-4111-8111-111111111111");
        private static readonly Guid guid_Flat_2 = new("22222222-2222-4222-8222-222222222222");
        private static readonly Guid guid_Corridor = new("33333333-3333-4333-8333-333333333333");

        // ---------------------------------------------------------------------------------------------
        // 1. The same engineering scenario is one scenario
        // ---------------------------------------------------------------------------------------------

        /// <summary>
        /// Two scenarios built independently from the same engineering statement derive one key. Nothing
        /// is shared between the two constructions - separate <c>SystemTemplate</c>s, separate assumption
        /// objects - so this fails the moment any part of the identity comes from the instance rather than
        /// from what it says.
        /// </summary>
        [Fact]
        public void SameEngineeringScenario_DerivesTheSameKey()
        {
            Assert.Equal(Scenario().Key, Scenario().Key);
        }

        /// <summary>
        /// The order the assumptions were stated in is not part of the assessment. Two engineers who set
        /// the same three assumptions in different orders described the same thing.
        /// </summary>
        [Fact]
        public void AssumptionOrder_DoesNotChangeTheKey()
        {
            OverheatingOperatingAssumptions overheatingOperatingAssumptions_1 = new();
            overheatingOperatingAssumptions_1.Set("Openings", "Unrestricted");
            overheatingOperatingAssumptions_1.Set("MechanicalRate_Lps", 21.0);
            overheatingOperatingAssumptions_1.Set("SummerBypass", false);

            OverheatingOperatingAssumptions overheatingOperatingAssumptions_2 = new();
            overheatingOperatingAssumptions_2.Set("SummerBypass", false);
            overheatingOperatingAssumptions_2.Set("MechanicalRate_Lps", 21.0);
            overheatingOperatingAssumptions_2.Set("Openings", "Unrestricted");

            Assert.Equal(Scenario(overheatingOperatingAssumptions: overheatingOperatingAssumptions_1).Key, Scenario(overheatingOperatingAssumptions: overheatingOperatingAssumptions_2).Key);
        }

        /// <summary>
        /// A key derived today equals the one derived by the code that first wrote this test. Everything
        /// else here compares two keys made by the same build, which cannot catch a change to the encoding
        /// itself - a reordered component, a dropped length prefix, a switch to a different hash - all of
        /// which would silently re-key every assessment ever recorded.
        /// <para>
        /// <b>This value is only ever regenerated together with a deliberate bump of
        /// <c>OverheatingScenario.IdentitySchema</c>.</b> A change here without one is a bug, not a
        /// stale expectation.
        /// </para>
        /// </summary>
        [Fact]
        public void Key_IsStableAcrossBuilds()
        {
            Assert.Equal("OverheatingScenario:v1", OverheatingScenario.IdentitySchema);
            Assert.Equal(new Guid("e81d5d9f-3672-801d-933b-f2e3c19bb284"), Scenario().Key);

            //Version 8 and the RFC 4122 variant, so a derived key is visibly not a model guid.
            byte[] bytes = Scenario().Key.ToByteArray();

            Assert.Equal(0x80, bytes[7] & 0xF0);
            Assert.Equal(0x80, bytes[8] & 0xC0);
        }

        // ---------------------------------------------------------------------------------------------
        // 2. Serialisation
        // ---------------------------------------------------------------------------------------------

        /// <summary>
        /// A scenario written to JSON and read back is the same assessment. The key survives because it is
        /// re-derived from the state, not because it was saved.
        /// </summary>
        [Fact]
        public void JsonRoundTrip_DerivesTheSameKey()
        {
            OverheatingScenario overheatingScenario = Scenario();
            overheatingScenario.Name = "Flat 1 - base";
            overheatingScenario.Source = "SAM_zoningAM_v2zonesisDomestic.sam";

            OverheatingScenario overheatingScenario_RoundTrip = Core.Create.IJSAMObject<OverheatingScenario>(overheatingScenario.ToJsonObject().ToJsonString());

            Assert.NotNull(overheatingScenario_RoundTrip);
            Assert.Equal(overheatingScenario.Key, overheatingScenario_RoundTrip.Key);

            Assert.Equal(overheatingScenario.Scope, overheatingScenario_RoundTrip.Scope);
            Assert.Equal(overheatingScenario.ZoneGuid, overheatingScenario_RoundTrip.ZoneGuid);
            Assert.Equal(overheatingScenario.Iteration, overheatingScenario_RoundTrip.Iteration);
            Assert.Equal(overheatingScenario.Name, overheatingScenario_RoundTrip.Name);
            Assert.Equal(overheatingScenario.Source, overheatingScenario_RoundTrip.Source);
            Assert.Equal("MVRE", overheatingScenario_RoundTrip.SystemTemplate?.Ventilation);
            Assert.Equal("Unrestricted", overheatingScenario_RoundTrip.OperatingAssumptions.Value("Openings"));
        }

        /// <summary>
        /// <b>The key is never written.</b> A stored key is a second copy of an identity that is supposed
        /// to have one source, and a file carrying one that disagrees with its own contents would be
        /// believed. There is nothing to disagree with if nothing is stored.
        /// </summary>
        [Fact]
        public void Key_IsNeverSerialised()
        {
            OverheatingScenario overheatingScenario = Scenario();

            JsonObject jsonObject = overheatingScenario.ToJsonObject();

            foreach (KeyValuePair<string, JsonNode> keyValuePair in jsonObject)
            {
                Assert.DoesNotContain("key", keyValuePair.Key, StringComparison.OrdinalIgnoreCase);
            }

            Assert.DoesNotContain(overheatingScenario.Key.ToString("D"), jsonObject.ToJsonString(), StringComparison.OrdinalIgnoreCase);
        }

        // ---------------------------------------------------------------------------------------------
        // 3-6. What must change the key
        // ---------------------------------------------------------------------------------------------

        /// <summary>
        /// <b>Two flats called the same thing are two flats.</b> Name is not identity and the design zone
        /// guid is, so an identical name over a different dwelling is a different assessment - and the
        /// reverse holds too: renaming a flat does not orphan its assessment.
        /// </summary>
        [Fact]
        public void SameNameDifferentDwelling_DerivesADifferentKey()
        {
            OverheatingScenario overheatingScenario_1 = Scenario(guid_Zone: guid_Flat_1);
            overheatingScenario_1.Name = "Flat 2";

            OverheatingScenario overheatingScenario_2 = Scenario(guid_Zone: guid_Flat_2);
            overheatingScenario_2.Name = "Flat 2";

            Assert.NotEqual(overheatingScenario_1.Key, overheatingScenario_2.Key);
        }

        /// <summary>The other direction: a rename is not a new assessment.</summary>
        [Fact]
        public void DifferentNameSameDwelling_DerivesTheSameKey()
        {
            OverheatingScenario overheatingScenario_1 = Scenario();
            overheatingScenario_1.Name = "Flat 1";

            OverheatingScenario overheatingScenario_2 = Scenario();
            overheatingScenario_2.Name = "Apartment 1 (revised)";

            Assert.Equal(overheatingScenario_1.Key, overheatingScenario_2.Key);
        }

        /// <summary>
        /// The scope is in the key in its own right, so a dwelling assessment and a common-space assessment
        /// over the same zone guid can never collide - which is the mechanism behind "the corridor is
        /// assessed but never attributed to a dwelling".
        /// </summary>
        [Fact]
        public void DifferentScope_DerivesADifferentKey()
        {
            Assert.NotEqual(
                Scenario(partOAssessmentScope: PartOAssessmentScope.Dwelling, guid_Zone: guid_Corridor).Key,
                Scenario(partOAssessmentScope: PartOAssessmentScope.CommonSpace, guid_Zone: guid_Corridor).Key);
        }

        /// <summary>
        /// The same dwelling at two mitigation stages is two engineering answers, and they must not share
        /// an identity - otherwise the second overwrites the first.
        /// </summary>
        [Fact]
        public void DifferentIteration_DerivesADifferentKey()
        {
            List<Guid> guids = [];

            foreach (PartOIteration partOIteration in Enum.GetValues(typeof(PartOIteration)))
            {
                Guid guid = Scenario(partOIteration: partOIteration).Key;

                Assert.DoesNotContain(guid, guids);
                guids.Add(guid);
            }

            Assert.Equal(Enum.GetValues(typeof(PartOIteration)).Length, guids.Count);
        }

        /// <summary>
        /// <b>Every field of the system identity participates - all six.</b> Asserted one field at a time
        /// rather than by changing the template wholesale, because a derivation that quietly dropped, say,
        /// <c>Controls</c> would still pass a single wholesale comparison and would then merge two
        /// genuinely different assessments.
        /// </summary>
        [Fact]
        public void DifferentSystemTemplate_DerivesADifferentKey()
        {
            Guid guid = Scenario().Key;

            Assert.NotEqual(guid, Scenario(systemTemplate: Template("NV")).Key);
            Assert.NotEqual(guid, Scenario(systemTemplate: Template(heating: "UH")).Key);
            Assert.NotEqual(guid, Scenario(systemTemplate: Template(cooling: "CH1")).Key);
            Assert.NotEqual(guid, Scenario(systemTemplate: Template(plantRoom: "PR2")).Key);
            Assert.NotEqual(guid, Scenario(systemTemplate: Template(controls: "CTL2")).Key);
            Assert.NotEqual(guid, Scenario(systemTemplate: Template(version: "2")).Key);

            //And no system stated at all is not the same as this one. Built directly rather than through
            //the fixture, whose null means "the usual one".
            Assert.NotEqual(guid, new OverheatingScenario(PartOAssessmentScope.Dwelling, guid_Flat_1, PartOIteration.Undefined, null, Assumptions()).Key);

            //A template with nothing stated in it IS no template - "no system" and "a system nothing is
            //said about" are the same statement, and normalising them here is what stops them being two.
            Assert.Equal(
                new OverheatingScenario(PartOAssessmentScope.Dwelling, guid_Flat_1, PartOIteration.Undefined, null, Assumptions()).Key,
                new OverheatingScenario(PartOAssessmentScope.Dwelling, guid_Flat_1, PartOIteration.Undefined, new SystemTemplate(), Assumptions()).Key);
        }

        /// <summary>
        /// An operating assumption is identity-defining however it differs - a changed value, a renamed
        /// assumption, or one more of them. All three are a different assessment of the same fabric.
        /// </summary>
        [Fact]
        public void DifferentOperatingAssumption_DerivesADifferentKey()
        {
            Guid guid = Scenario().Key;

            OverheatingOperatingAssumptions overheatingOperatingAssumptions;

            overheatingOperatingAssumptions = Assumptions();
            overheatingOperatingAssumptions.Set("Openings", "Restricted");
            Assert.NotEqual(guid, Scenario(overheatingOperatingAssumptions: overheatingOperatingAssumptions).Key);

            overheatingOperatingAssumptions = Assumptions();
            overheatingOperatingAssumptions.Remove("Openings");
            overheatingOperatingAssumptions.Set("OpeningBehaviour", "Unrestricted");
            Assert.NotEqual(guid, Scenario(overheatingOperatingAssumptions: overheatingOperatingAssumptions).Key);

            overheatingOperatingAssumptions = Assumptions();
            overheatingOperatingAssumptions.Set("Boost", true);
            Assert.NotEqual(guid, Scenario(overheatingOperatingAssumptions: overheatingOperatingAssumptions).Key);

            overheatingOperatingAssumptions = Assumptions();
            overheatingOperatingAssumptions.Set("MechanicalRate_Lps", 21.5);
            Assert.NotEqual(guid, Scenario(overheatingOperatingAssumptions: overheatingOperatingAssumptions).Key);
        }

        /// <summary>
        /// <b>Concatenation is ambiguous and length-prefixing is why this passes.</b> An assumption called
        /// <c>AB</c> with value <c>C</c> and one called <c>A</c> with value <c>BC</c> flatten to the same
        /// characters. They are not the same assumption, and a derivation that simply joined its
        /// components would give them one key.
        /// </summary>
        [Fact]
        public void ComponentsThatFlattenAlike_DeriveDifferentKeys()
        {
            OverheatingOperatingAssumptions overheatingOperatingAssumptions_1 = new();
            overheatingOperatingAssumptions_1.Set("AB", "C");

            OverheatingOperatingAssumptions overheatingOperatingAssumptions_2 = new();
            overheatingOperatingAssumptions_2.Set("A", "BC");

            Assert.NotEqual(Scenario(overheatingOperatingAssumptions: overheatingOperatingAssumptions_1).Key, Scenario(overheatingOperatingAssumptions: overheatingOperatingAssumptions_2).Key);

            //The same trap across the system fields: "MV" + "RE1" is not "MVRE" + "1".
            Assert.NotEqual(Scenario(systemTemplate: Template("MV", version: "RE1")).Key, Scenario(systemTemplate: Template("MVRE", version: "1")).Key);
        }

        /// <summary>
        /// An unstated component and one stated as blank are different statements, and the derivation keeps
        /// them apart.
        /// </summary>
        [Fact]
        public void UnstatedAndBlank_DeriveDifferentKeys()
        {
            OverheatingOperatingAssumptions overheatingOperatingAssumptions_Blank = new();
            overheatingOperatingAssumptions_Blank.Set("Openings", string.Empty);

            OverheatingOperatingAssumptions overheatingOperatingAssumptions_Unstated = new();

            Assert.NotEqual(Scenario(overheatingOperatingAssumptions: overheatingOperatingAssumptions_Blank).Key, Scenario(overheatingOperatingAssumptions: overheatingOperatingAssumptions_Unstated).Key);
        }

        // ---------------------------------------------------------------------------------------------
        // 7-8. What must NOT change the key
        // ---------------------------------------------------------------------------------------------

        /// <summary>
        /// <b>Provenance is not identity.</b> Where an answer came from is not part of what the question
        /// was, so re-running the same assessment from a different model file, workflow or user is the same
        /// assessment - which is what makes it comparable with the earlier run rather than a new one.
        /// </summary>
        [Fact]
        public void ProvenanceChange_DerivesTheSameKey()
        {
            OverheatingScenario overheatingScenario = Scenario();

            Guid guid = overheatingScenario.Key;

            overheatingScenario.Source = "SAM_zoningAM_v2zonesisDomestic.sam";
            Assert.Equal(guid, overheatingScenario.Key);

            overheatingScenario.Source = "Rebuilt by hand, 2026-08-08";
            Assert.Equal(guid, overheatingScenario.Key);

            overheatingScenario.Name = "anything at all";
            Assert.Equal(guid, overheatingScenario.Key);
        }

        /// <summary>
        /// <b>Routing is not identity.</b> The same scenario answered by a simple TSD run and by a full
        /// HVAC TPD run is one assessment computed two ways - that boundary is the point of Iteration 0's
        /// architecture, and a key that moved with the route would destroy it.
        /// <para>
        /// Asserted behaviourally here and structurally in
        /// <see cref="ScenarioType_HasNoEngineOrRoutingMember"/>: nothing engine-shaped is even a member of
        /// the type, so there is nothing that could reach the key.
        /// </para>
        /// </summary>
        [Fact]
        public void RoutingChange_DerivesTheSameKey()
        {
            OverheatingScenario overheatingScenario = Scenario();

            Guid guid = overheatingScenario.Key;

            foreach (string text in new[] { "TAS 9.5.7.0 TSD", "TAS 9.5.7.0 TPD", @"C:\SAM_daily\2027-08-03-HVAC\000000_SAM_AnalyticalModel.tsd" })
            {
                overheatingScenario.Source = text;
                Assert.Equal(guid, overheatingScenario.Key);
            }
        }

        /// <summary>
        /// The structural half of the same promise. <c>SAM.Analytical</c> is engine-free, and a scenario
        /// describes intent - so no member of it may name an engine, a route or a file, and the assembly
        /// may not reference a TAS one.
        /// </summary>
        [Fact]
        public void ScenarioType_HasNoEngineOrRoutingMember()
        {
            string[] forbidden = ["tas", "tsd", "tpd", "path", "file", "directory", "weather", "simulat", "result"];

            foreach (MemberInfo memberInfo in typeof(OverheatingScenario).GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
            {
                foreach (string text in forbidden)
                {
                    Assert.False(memberInfo.Name.ToLowerInvariant().Contains(text), string.Format("OverheatingScenario declares '{0}', which names {1}. A scenario states intent only.", memberInfo.Name, text));
                }
            }

            foreach (AssemblyName assemblyName in typeof(OverheatingScenario).Assembly.GetReferencedAssemblies())
            {
                Assert.DoesNotContain("Tas", assemblyName.Name, StringComparison.OrdinalIgnoreCase);
            }
        }

        // ---------------------------------------------------------------------------------------------
        // 9. Encoding
        // ---------------------------------------------------------------------------------------------

        /// <summary>
        /// <b>Non-ASCII values stay distinct - and the first line of this test is why the derivation does
        /// not use <c>Core.Query.ComputeHash</c>.</b> That helper encodes ASCII, which maps every character
        /// outside it to <c>?</c>, so under it "café" and "cafè" are one string. Zone and assumption text
        /// comes from engineers' models, and an accented name is not exotic.
        /// </summary>
        [Fact]
        public void NonAsciiValues_RemainDistinct()
        {
            string text_1 = "caf\u00E9";
            string text_2 = "caf\u00E8";

            //The positive control. If this ever stops being true, ComputeHash was fixed and this test's
            //premise - not its conclusion - has changed.
            Assert.Equal(Core.Query.ComputeHash(text_1), Core.Query.ComputeHash(text_2));

            OverheatingOperatingAssumptions overheatingOperatingAssumptions_1 = new();
            overheatingOperatingAssumptions_1.Set("Openings", text_1);

            OverheatingOperatingAssumptions overheatingOperatingAssumptions_2 = new();
            overheatingOperatingAssumptions_2.Set("Openings", text_2);

            Assert.NotEqual(Scenario(overheatingOperatingAssumptions: overheatingOperatingAssumptions_1).Key, Scenario(overheatingOperatingAssumptions: overheatingOperatingAssumptions_2).Key);

            //The same, in an assumption's name rather than its value.
            OverheatingOperatingAssumptions overheatingOperatingAssumptions_3 = new();
            overheatingOperatingAssumptions_3.Set(text_1, "Unrestricted");

            OverheatingOperatingAssumptions overheatingOperatingAssumptions_4 = new();
            overheatingOperatingAssumptions_4.Set(text_2, "Unrestricted");

            Assert.NotEqual(Scenario(overheatingOperatingAssumptions: overheatingOperatingAssumptions_3).Key, Scenario(overheatingOperatingAssumptions: overheatingOperatingAssumptions_4).Key);

            //And it survives JSON, which is where an encoding mistake would otherwise appear.
            OverheatingScenario overheatingScenario = Scenario(overheatingOperatingAssumptions: overheatingOperatingAssumptions_1);
            Assert.Equal(overheatingScenario.Key, Core.Create.IJSAMObject<OverheatingScenario>(overheatingScenario.ToJsonObject().ToJsonString()).Key);
        }

        /// <summary>
        /// A numeric assumption is formatted invariantly, so the same scenario cannot derive two keys on
        /// two machines because one of them writes a comma decimal separator. Set through the typed
        /// overload, which is the only reason a caller does not have to think about it.
        /// </summary>
        [Fact]
        public void NumericAssumption_IsFormattedInvariantly()
        {
            OverheatingOperatingAssumptions overheatingOperatingAssumptions = new();
            overheatingOperatingAssumptions.Set("MechanicalRate_Lps", 21.5);

            Assert.Equal("21.5", overheatingOperatingAssumptions.Value("MechanicalRate_Lps"));
        }

        // ---------------------------------------------------------------------------------------------
        // 10. The key cannot go stale
        // ---------------------------------------------------------------------------------------------

        /// <summary>
        /// <b>No mutable input can leave the key describing something the scenario no longer says.</b> Both
        /// objects a scenario is built from are mutable, so this changes every one of them - the instances
        /// handed to the constructor, and the instances read back off the properties - and the key does not
        /// move, because none of them is the object it is derived from.
        /// </summary>
        [Fact]
        public void MutableIdentityInput_CannotLeaveAStaleKey()
        {
            SystemTemplate systemTemplate = Template();
            OverheatingOperatingAssumptions overheatingOperatingAssumptions = Assumptions();

            OverheatingScenario overheatingScenario = new(PartOAssessmentScope.Dwelling, guid_Flat_1, PartOIteration.Undefined, systemTemplate, overheatingOperatingAssumptions);

            Guid guid = overheatingScenario.Key;

            //The caller's own instances.
            systemTemplate.Ventilation = "NV";
            systemTemplate.Cooling = "CH1";
            overheatingOperatingAssumptions.Set("Openings", "Restricted");
            overheatingOperatingAssumptions.Set("Boost", true);

            Assert.Equal(guid, overheatingScenario.Key);

            //And what the properties hand back.
            overheatingScenario.SystemTemplate.Ventilation = "NV";
            overheatingScenario.OperatingAssumptions.Set("Openings", "Restricted");
            overheatingScenario.OperatingAssumptions.Remove("Openings");

            Assert.Equal(guid, overheatingScenario.Key);
            Assert.Equal("MVRE", overheatingScenario.SystemTemplate?.Ventilation);
            Assert.Equal("Unrestricted", overheatingScenario.OperatingAssumptions.Value("Openings"));
        }

        /// <summary>
        /// The structural reason the test above holds rather than happening to: identity-defining state has
        /// no public setter, so there is no supported way to change what a scenario says after it is built.
        /// A future property added with one would fail here rather than in a misattributed result.
        /// </summary>
        [Fact]
        public void IdentityDefiningState_HasNoPublicSetter()
        {
            foreach (string text in new[] { "Scope", "ZoneGuid", "Iteration", "SystemTemplate", "OperatingAssumptions", "Key" })
            {
                PropertyInfo propertyInfo = typeof(OverheatingScenario).GetProperty(text);

                Assert.NotNull(propertyInfo);
                Assert.True(propertyInfo.SetMethod == null || !propertyInfo.SetMethod.IsPublic, string.Format("OverheatingScenario.{0} is identity-defining and must not be settable.", text));
            }
        }

        /// <summary>
        /// A scenario read from JSON written by a later version - an iteration this build has never heard
        /// of - is <c>Undefined</c> rather than an exception or, worse, the first member. An unreadable
        /// file helps nobody and a silently wrong mitigation stage is a wrong engineering answer.
        /// </summary>
        [Fact]
        public void UnknownEnumName_ReadsAsUndefined()
        {
            JsonObject jsonObject = Scenario().ToJsonObject();
            jsonObject["Iteration"] = "SomethingFromALaterVersion";

            OverheatingScenario overheatingScenario = new(jsonObject);

            Assert.Equal(PartOIteration.Undefined, overheatingScenario.Iteration);
        }

        /// <summary>
        /// <b>There is no foundation-stage iteration member.</b> The Iteration 0 work is a stage of this
        /// codebase, not an operating scenario of a building, and naming it in an engineering identity
        /// would outlive the schedule it describes. A scenario built during that work states
        /// <c>Undefined</c>, which is true.
        /// </summary>
        [Fact]
        public void PartOIteration_HasNoFoundationStageMember()
        {
            foreach (string text in Enum.GetNames(typeof(PartOIteration)))
            {
                Assert.DoesNotContain("iteration", text, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("0", text, StringComparison.OrdinalIgnoreCase);
            }

            Assert.Equal(PartOIteration.Undefined, default(PartOIteration));
            Assert.Equal(PartOAssessmentScope.Undefined, default(PartOAssessmentScope));
        }

        /// <summary>
        /// A scenario that names nothing assessable says so. The iteration and the system may legitimately
        /// be unstated - a foundation-stage scenario over a real dwelling is valid.
        /// </summary>
        [Fact]
        public void IsValid_NeedsAScopeAndADesignZone()
        {
            Assert.True(Scenario().IsValid);
            Assert.True(new OverheatingScenario(PartOAssessmentScope.Dwelling, guid_Flat_1, PartOIteration.Undefined).IsValid);

            Assert.False(new OverheatingScenario().IsValid);
            Assert.False(new OverheatingScenario(PartOAssessmentScope.Undefined, guid_Flat_1, PartOIteration.BasePassive).IsValid);
            Assert.False(new OverheatingScenario(PartOAssessmentScope.Dwelling, Guid.Empty, PartOIteration.BasePassive).IsValid);
        }

        /// <summary>
        /// Equality is the key, so a scenario reconstructed from JSON is found in a set built before it was
        /// saved - which is how a result will be attributed to a scenario nobody kept a reference to.
        /// </summary>
        [Fact]
        public void Equality_IsTheDerivedKey()
        {
            OverheatingScenario overheatingScenario = Scenario();

            HashSet<OverheatingScenario> overheatingScenarios = [overheatingScenario];

            Assert.Contains(Core.Create.IJSAMObject<OverheatingScenario>(overheatingScenario.ToJsonObject().ToJsonString()), overheatingScenarios);
            Assert.DoesNotContain(Scenario(guid_Zone: guid_Flat_2), overheatingScenarios);
        }

        // ---------------------------------------------------------------------------------------------
        // Fixture
        // ---------------------------------------------------------------------------------------------

        /// <summary>Flat 1 of the validation block, on MVRE, with a stated set of assumptions.</summary>
        private static OverheatingScenario Scenario(PartOAssessmentScope partOAssessmentScope = PartOAssessmentScope.Dwelling, Guid? guid_Zone = null, PartOIteration partOIteration = PartOIteration.Undefined, SystemTemplate systemTemplate = null, OverheatingOperatingAssumptions overheatingOperatingAssumptions = null)
        {
            return new OverheatingScenario(partOAssessmentScope, guid_Zone ?? guid_Flat_1, partOIteration, systemTemplate ?? Template(), overheatingOperatingAssumptions ?? Assumptions());
        }

        /// <summary>MVRE - SAM's heat-recovery ventilation - with radiators and no cooling.</summary>
        private static SystemTemplate Template(string ventilation = "MVRE", string heating = "RAD1", string cooling = "UC1", string plantRoom = "PR1", string controls = "CTL1", string version = "1")
        {
            return new SystemTemplate(ventilation, heating, cooling, plantRoom, controls, version);
        }

        private static OverheatingOperatingAssumptions Assumptions()
        {
            OverheatingOperatingAssumptions result = new();

            result.Set("Openings", "Unrestricted");
            result.Set("MechanicalRate_Lps", 21.0);
            result.Set("SummerBypass", false);

            return result;
        }
    }
}
