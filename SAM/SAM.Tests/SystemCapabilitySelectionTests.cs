// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical;
using SAM.Analytical.Enums;
using System;
using System.Collections.Generic;
using System.Reflection;
using Xunit;

namespace SAM.Tests
{
    /// <summary>
    /// Choosing the minimum system that meets an Approved Document F requirement.
    /// <para>
    /// <b>Only the analytical half is here, and that is the design.</b> <c>SAM.Analytical</c> owns the
    /// capability vocabulary, the rule that reads a requirement off a Part F assessment, and the rule that
    /// picks the minimum system meeting it. Which of the shipped <c>SystemEnergyCentre</c> templates
    /// actually provides what is a fact about <c>SAM_Systems</c>' own resources and lives beside them - so
    /// nothing in this assembly names a template file, and the descriptors below are a <b>test fixture</b>
    /// standing in for that catalog, not a copy of it.
    /// </para>
    /// </summary>
    public class SystemCapabilitySelectionTests
    {
        // ---------------------------------------------------------------------------------------------
        // The selection rule
        // ---------------------------------------------------------------------------------------------

        /// <summary>
        /// Asked only for continuous ventilation, the answer is the simplest system that provides it -
        /// <b>not</b> the first capable one in the list, and not a more capable one. A system that can do
        /// more than was asked implies plant nobody required, and on a Part O assessment it would quietly
        /// credit the dwelling with mitigation the design does not have.
        /// </summary>
        [Fact]
        public void ContinuousOnly_SelectsTheMinimumCapableSystem()
        {
            SystemCapabilitySelection systemCapabilitySelection = Descriptors().SelectMinimumCapableSystem(Requirement(SystemCapability.ContinuousVentilation));

            Assert.True(systemCapabilitySelection.IsSelected);
            Assert.Equal("NV", systemCapabilitySelection.SystemTemplate?.Ventilation);
            Assert.Null(systemCapabilitySelection.Reason);
        }

        /// <summary>
        /// Requiring boost removes every system that cannot boost, and the answer is again the least
        /// capable of what remains - the mechanical system without heat recovery, not the one with it.
        /// </summary>
        [Fact]
        public void BoostRequired_ExcludesSystemsThatCannotBoost()
        {
            SystemCapabilitySelection systemCapabilitySelection = Descriptors().SelectMinimumCapableSystem(Requirement(SystemCapability.ContinuousVentilation | SystemCapability.Boost));

            Assert.True(systemCapabilitySelection.IsSelected);
            Assert.Equal("MV", systemCapabilitySelection.SystemTemplate?.Ventilation);
        }

        /// <summary>
        /// Requiring summer bypass removes everything that cannot bypass, which here leaves only the
        /// heat-recovery system - because bypass is a state of a heat exchanger and there is nothing to
        /// bypass without one.
        /// </summary>
        [Fact]
        public void SummerBypassRequired_ExcludesSystemsThatCannotBypass()
        {
            SystemCapabilitySelection systemCapabilitySelection = Descriptors().SelectMinimumCapableSystem(Requirement(SystemCapability.ContinuousVentilation | SystemCapability.SummerBypass));

            Assert.True(systemCapabilitySelection.IsSelected);
            Assert.Equal("MVRE", systemCapabilitySelection.SystemTemplate?.Ventilation);
        }

        /// <summary>
        /// <b>The order the systems arrived in cannot change the answer.</b> A caller that built its list
        /// by enumerating a directory would otherwise let the file system decide an engineering question,
        /// and the answer would differ between two machines with the same library.
        /// </summary>
        [Fact]
        public void SystemOrdering_CannotChangeTheSelection()
        {
            SystemCapabilityRequirement systemCapabilityRequirement = Requirement(SystemCapability.ContinuousVentilation | SystemCapability.Boost);

            List<SystemCapabilityDescriptor> systemCapabilityDescriptors = Descriptors();

            string expected = systemCapabilityDescriptors.SelectMinimumCapableSystem(systemCapabilityRequirement).SystemTemplate?.Ventilation;

            Assert.Equal("MV", expected);

            //Every rotation of the list, and the reverse of each.
            for (int i = 0; i < systemCapabilityDescriptors.Count; i++)
            {
                List<SystemCapabilityDescriptor> systemCapabilityDescriptors_Rotated = [];

                for (int j = 0; j < systemCapabilityDescriptors.Count; j++)
                {
                    systemCapabilityDescriptors_Rotated.Add(systemCapabilityDescriptors[(i + j) % systemCapabilityDescriptors.Count]);
                }

                Assert.Equal(expected, systemCapabilityDescriptors_Rotated.SelectMinimumCapableSystem(systemCapabilityRequirement).SystemTemplate?.Ventilation);

                systemCapabilityDescriptors_Rotated.Reverse();

                Assert.Equal(expected, systemCapabilityDescriptors_Rotated.SelectMinimumCapableSystem(systemCapabilityRequirement).SystemTemplate?.Ventilation);
            }
        }

        /// <summary>
        /// <b>Two systems that are equally minimal are separated by identity, not by position.</b> Same
        /// capabilities, same everything except the version - and whichever way round they are offered, the
        /// lower identity wins.
        /// </summary>
        [Fact]
        public void EquallyMinimalSystems_AreSeparatedByIdentity()
        {
            SystemCapabilityDescriptor systemCapabilityDescriptor_1 = new(new SystemTemplate("MV", "RAD1", "UC1", "PR1", "CTL1", "1"), SystemCapability.ContinuousVentilation);
            SystemCapabilityDescriptor systemCapabilityDescriptor_2 = new(new SystemTemplate("MV", "RAD1", "UC1", "PR1", "CTL1", "2"), SystemCapability.ContinuousVentilation);

            SystemCapabilityRequirement systemCapabilityRequirement = Requirement(SystemCapability.ContinuousVentilation);

            Assert.Equal("1", new List<SystemCapabilityDescriptor> { systemCapabilityDescriptor_1, systemCapabilityDescriptor_2 }.SelectMinimumCapableSystem(systemCapabilityRequirement).SystemTemplate?.Version);
            Assert.Equal("1", new List<SystemCapabilityDescriptor> { systemCapabilityDescriptor_2, systemCapabilityDescriptor_1 }.SelectMinimumCapableSystem(systemCapabilityRequirement).SystemTemplate?.Version);
        }

        // ---------------------------------------------------------------------------------------------
        // Refusal
        // ---------------------------------------------------------------------------------------------

        /// <summary>
        /// <b>Nothing capable means a refusal, never the nearest thing.</b> The result names the capability
        /// no system offered, so a report can say what the library was short of rather than that something
        /// unspecified went wrong.
        /// </summary>
        [Fact]
        public void NoCapableSystem_IsRefusedAndSaysWhatWasMissing()
        {
            List<SystemCapabilityDescriptor> systemCapabilityDescriptors =
            [
                new(Template("NV"), SystemCapability.ContinuousVentilation),
                new(Template("MV"), SystemCapability.ContinuousVentilation | SystemCapability.Boost)
            ];

            SystemCapabilitySelection systemCapabilitySelection = systemCapabilityDescriptors.SelectMinimumCapableSystem(Requirement(SystemCapability.ContinuousVentilation | SystemCapability.SummerBypass));

            Assert.False(systemCapabilitySelection.IsSelected);
            Assert.Null(systemCapabilitySelection.Descriptor);
            Assert.Null(systemCapabilitySelection.SystemTemplate);

            Assert.Equal(SystemCapability.SummerBypass, systemCapabilitySelection.Missing);
            Assert.Contains("SummerBypass", systemCapabilitySelection.Reason);
        }

        /// <summary>
        /// The harder refusal: every capability exists somewhere, just never together on one system. Saying
        /// nothing was missing would read as a contradiction, so the reason says the real thing.
        /// </summary>
        [Fact]
        public void CapabilitiesSplitAcrossSystems_AreRefusedWithATruthfulReason()
        {
            List<SystemCapabilityDescriptor> systemCapabilityDescriptors =
            [
                new(Template("NV"), SystemCapability.ContinuousVentilation),
                new(Template("XX"), SystemCapability.SummerBypass)
            ];

            SystemCapabilitySelection systemCapabilitySelection = systemCapabilityDescriptors.SelectMinimumCapableSystem(Requirement(SystemCapability.ContinuousVentilation | SystemCapability.SummerBypass));

            Assert.False(systemCapabilitySelection.IsSelected);
            Assert.Equal(SystemCapability.None, systemCapabilitySelection.Missing);
            Assert.Contains("No single system", systemCapabilitySelection.Reason);
        }

        /// <summary>
        /// An empty requirement is refused rather than answered with the smallest system on the shelf.
        /// Choosing a system nobody asked for is inventing a requirement, and the same goes for an empty or
        /// absent library.
        /// </summary>
        [Fact]
        public void NothingRequiredOrNothingOffered_IsRefused()
        {
            Assert.False(Descriptors().SelectMinimumCapableSystem(new SystemCapabilityRequirement()).IsSelected);
            Assert.False(Descriptors().SelectMinimumCapableSystem(null).IsSelected);

            SystemCapabilityRequirement systemCapabilityRequirement = Requirement(SystemCapability.ContinuousVentilation);

            Assert.False(new List<SystemCapabilityDescriptor>().SelectMinimumCapableSystem(systemCapabilityRequirement).IsSelected);
            Assert.False(((List<SystemCapabilityDescriptor>)null).SelectMinimumCapableSystem(systemCapabilityRequirement).IsSelected);

            //A descriptor naming no system is not a system, and a list of them is an empty library.
            List<SystemCapabilityDescriptor> systemCapabilityDescriptors = [null, new SystemCapabilityDescriptor(), new SystemCapabilityDescriptor(null, SystemCapability.ContinuousVentilation)];

            Assert.False(systemCapabilityDescriptors.SelectMinimumCapableSystem(systemCapabilityRequirement).IsSelected);
        }

        // ---------------------------------------------------------------------------------------------
        // The Part F rule
        // ---------------------------------------------------------------------------------------------

        /// <summary>
        /// A sized dwelling requires continuous ventilation, and requires boost exactly when the balanced
        /// high-rate extract exceeds the continuous design rate.
        /// </summary>
        [Fact]
        public void PartFRequirement_IsContinuousAndBoostWhenTheHighRateExceedsIt()
        {
            PartFDwellingResult partFDwellingResult = new("Flat 1") { ContinuousDesignSystemRate_Lps = 21.0, TotalHighExtract_Lps = 19.0 };

            Assert.Equal(SystemCapability.ContinuousVentilation, partFDwellingResult.PartFSystemCapabilityRequirement().Capabilities);

            partFDwellingResult.TotalHighExtract_Lps = 39.0;

            Assert.Equal(SystemCapability.ContinuousVentilation | SystemCapability.Boost, partFDwellingResult.PartFSystemCapabilityRequirement().Capabilities);

            //Equal rates are not a demand for extra plant.
            partFDwellingResult.TotalHighExtract_Lps = 21.0;

            Assert.False(partFDwellingResult.PartFSystemCapabilityRequirement().Requires(SystemCapability.Boost));
        }

        /// <summary>
        /// <b>Part F never asks for summer bypass or heat recovery, whatever the dwelling looks like.</b>
        /// Those are mitigation an Approved Document O scenario states, and a dwelling credited with
        /// mitigation its design does not have would pass an overheating assessment it should fail. Swept
        /// over a range of dwellings so it cannot pass on one convenient case.
        /// </summary>
        [Fact]
        public void PartFRequirement_NeverAsksForMitigation()
        {
            foreach (double continuous in new[] { 0.0, 0.0005, 13.0, 21.0, 250.0 })
            {
                foreach (double high in new[] { 0.0, 13.0, 21.0, 39.0, 600.0 })
                {
                    SystemCapabilityRequirement systemCapabilityRequirement = new PartFDwellingResult("Flat 1") { ContinuousDesignSystemRate_Lps = continuous, TotalHighExtract_Lps = high }.PartFSystemCapabilityRequirement();

                    Assert.False(systemCapabilityRequirement.Requires(SystemCapability.SummerBypass));
                    Assert.False(systemCapabilityRequirement.Requires(SystemCapability.HeatRecovery));
                }
            }
        }

        /// <summary>
        /// An unsized dwelling requires nothing, and a null one does not throw. A requirement of nothing is
        /// then refused by the selector rather than answered - so an unsized dwelling cannot acquire a
        /// system by accident.
        /// </summary>
        [Fact]
        public void UnsizedDwelling_RequiresNothingAndSelectsNothing()
        {
            SystemCapabilityRequirement systemCapabilityRequirement = new PartFDwellingResult("Flat 1").PartFSystemCapabilityRequirement();

            Assert.False(systemCapabilityRequirement.IsValid);
            Assert.Equal(SystemCapability.None, systemCapabilityRequirement.Capabilities);
            Assert.False(Descriptors().SelectMinimumCapableSystem(systemCapabilityRequirement).IsSelected);

            Assert.False(((PartFDwellingResult)null).PartFSystemCapabilityRequirement().IsValid);
        }

        /// <summary>End to end: a Part F assessment picks a system, through the two rules and nothing else.</summary>
        [Fact]
        public void PartFAssessment_SelectsASystemEndToEnd()
        {
            PartFDwellingResult partFDwellingResult = new("Flat 1") { ContinuousDesignSystemRate_Lps = 21.0, TotalHighExtract_Lps = 39.0 };

            Assert.Equal("MV", Descriptors().SelectMinimumCapableSystem(partFDwellingResult.PartFSystemCapabilityRequirement()).SystemTemplate?.Ventilation);
        }

        // ---------------------------------------------------------------------------------------------
        // The boundary
        // ---------------------------------------------------------------------------------------------

        /// <summary>
        /// <b>Nothing here can open a template.</b> Choosing a system must not cost the megabyte and a half
        /// a <c>SystemEnergyCentre</c> weighs, and the way that is guaranteed is that this side of the
        /// boundary has no way to read one: no member of the selection types names a file, a path, a
        /// directory or an energy centre, and the systems available are an argument rather than something
        /// looked up.
        /// </summary>
        [Fact]
        public void SelectionTypes_CannotReachATemplate()
        {
            string[] forbidden = ["file", "path", "directory", "load", "energycentre", "resource", "stream", "read"];

            foreach (Type type in new[] { typeof(SystemCapabilityDescriptor), typeof(SystemCapabilityRequirement), typeof(SystemCapabilitySelection) })
            {
                foreach (MemberInfo memberInfo in type.GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
                {
                    foreach (string text in forbidden)
                    {
                        Assert.False(memberInfo.Name.ToLowerInvariant().Contains(text), string.Format("{0} declares '{1}', which names {2}. Choosing a system must not be able to open a template.", type.Name, memberInfo.Name, text));
                    }
                }
            }
        }

        /// <summary>
        /// <b>And the capability values do not live in this assembly.</b> Which of the shipped templates
        /// provides what is a fact about <c>SAM_Systems</c>' resources; a default catalog here would put
        /// the names of another repository's files in the core library and would have to be kept in step
        /// with them by hand. Asserted as the absence of any static member handing out descriptors.
        /// </summary>
        [Fact]
        public void NoDefaultCatalog_LivesInTheAnalyticalAssembly()
        {
            foreach (Type type in typeof(SystemCapabilityDescriptor).Assembly.GetTypes())
            {
                foreach (MemberInfo memberInfo in type.GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly))
                {
                    Type type_Returned = (memberInfo as PropertyInfo)?.PropertyType ?? (memberInfo as FieldInfo)?.FieldType;

                    if (type_Returned == null)
                    {
                        continue;
                    }

                    bool descriptors = type_Returned == typeof(SystemCapabilityDescriptor)
                        || type_Returned == typeof(SystemCapabilityDescriptor[])
                        || type_Returned == typeof(List<SystemCapabilityDescriptor>)
                        || type_Returned == typeof(IEnumerable<SystemCapabilityDescriptor>);

                    Assert.False(descriptors, string.Format("{0}.{1} is a static system-capability catalog. The values belong beside the templates in SAM_Systems, not here.", type.FullName, memberInfo.Name));
                }
            }
        }

        /// <summary>
        /// <c>MVRE</c> stays the heat-recovery identity and nothing is added beside it. Heat recovery is a
        /// <b>capability</b> of that system, which is precisely why a requirement that does not ask for it
        /// returns the simpler system - the distinction is made without a second name for one concept.
        /// </summary>
        [Fact]
        public void HeatRecovery_IsACapabilityOfMvreAndNotASecondIdentity()
        {
            Assert.DoesNotContain("MVHR", Enum.GetNames(typeof(SystemCapability)));

            SystemCapabilitySelection systemCapabilitySelection = Descriptors().SelectMinimumCapableSystem(Requirement(SystemCapability.ContinuousVentilation | SystemCapability.Boost | SystemCapability.HeatRecovery));

            Assert.Equal("MVRE", systemCapabilitySelection.SystemTemplate?.Ventilation);

            //Not asked for, so not chosen - the whole reason HeatRecovery is in the vocabulary.
            Assert.Equal("MV", Descriptors().SelectMinimumCapableSystem(Requirement(SystemCapability.ContinuousVentilation | SystemCapability.Boost)).SystemTemplate?.Ventilation);
        }

        // ---------------------------------------------------------------------------------------------
        // Serialisation
        // ---------------------------------------------------------------------------------------------

        /// <summary>
        /// Both types round-trip, because they cross an assembly boundary: <c>SAM_Systems</c> writes the
        /// descriptors and this assembly reads the requirement. Capabilities are written by <b>name</b>, so
        /// adding a member cannot renumber a stored file, and a name a build does not know is ignored
        /// rather than turned into a bit nothing understands.
        /// </summary>
        [Fact]
        public void RequirementAndDescriptor_RoundTrip()
        {
            SystemCapabilityRequirement systemCapabilityRequirement = Requirement(SystemCapability.ContinuousVentilation | SystemCapability.SummerBypass);

            SystemCapabilityRequirement systemCapabilityRequirement_RoundTrip = Core.Create.IJSAMObject<SystemCapabilityRequirement>(systemCapabilityRequirement.ToJsonObject().ToJsonString());

            Assert.NotNull(systemCapabilityRequirement_RoundTrip);
            Assert.Equal(systemCapabilityRequirement.Capabilities, systemCapabilityRequirement_RoundTrip.Capabilities);

            SystemCapabilityDescriptor systemCapabilityDescriptor = new(Template("MVRE"), SystemCapability.ContinuousVentilation | SystemCapability.Boost | SystemCapability.SummerBypass | SystemCapability.HeatRecovery);

            SystemCapabilityDescriptor systemCapabilityDescriptor_RoundTrip = Core.Create.IJSAMObject<SystemCapabilityDescriptor>(systemCapabilityDescriptor.ToJsonObject().ToJsonString());

            Assert.NotNull(systemCapabilityDescriptor_RoundTrip);
            Assert.Equal(systemCapabilityDescriptor.Capabilities, systemCapabilityDescriptor_RoundTrip.Capabilities);
            Assert.Equal("MVRE", systemCapabilityDescriptor_RoundTrip.SystemTemplate?.Ventilation);
            Assert.Equal(4, systemCapabilityDescriptor_RoundTrip.CapabilityCount);

            //Written by name, and a name from a later version is ignored rather than misread.
            Assert.Contains("SummerBypass", systemCapabilityRequirement.ToJsonObject().ToJsonString());

            System.Text.Json.Nodes.JsonObject jsonObject = systemCapabilityRequirement.ToJsonObject();
            (jsonObject["Capabilities"] as System.Text.Json.Nodes.JsonArray)?.Add("SomethingFromALaterVersion");

            Assert.Equal(systemCapabilityRequirement.Capabilities, new SystemCapabilityRequirement(jsonObject).Capabilities);
        }

        /// <summary>
        /// A requirement handed to a selector cannot change underneath it: <c>With</c> returns a new
        /// instance, and a descriptor copies the mutable <c>SystemTemplate</c> it was given.
        /// </summary>
        [Fact]
        public void RequirementAndDescriptor_DoNotChangeUnderneathACaller()
        {
            SystemCapabilityRequirement systemCapabilityRequirement = Requirement(SystemCapability.ContinuousVentilation);

            systemCapabilityRequirement.With(SystemCapability.SummerBypass);

            Assert.Equal(SystemCapability.ContinuousVentilation, systemCapabilityRequirement.Capabilities);

            SystemTemplate systemTemplate = Template("MV");

            SystemCapabilityDescriptor systemCapabilityDescriptor = new(systemTemplate, SystemCapability.ContinuousVentilation);

            systemTemplate.Ventilation = "NV";
            systemCapabilityDescriptor.SystemTemplate.Ventilation = "NV";

            Assert.Equal("MV", systemCapabilityDescriptor.SystemTemplate?.Ventilation);
        }

        // ---------------------------------------------------------------------------------------------
        // Fixture - a stand-in for the SAM_Systems catalog, NOT a copy of it
        // ---------------------------------------------------------------------------------------------

        private static List<SystemCapabilityDescriptor> Descriptors()
        {
            return
            [
                new(Template("UV"), SystemCapability.None),
                new(Template("NV"), SystemCapability.ContinuousVentilation),
                new(Template("MV"), SystemCapability.ContinuousVentilation | SystemCapability.Boost),
                new(Template("MVRE"), SystemCapability.ContinuousVentilation | SystemCapability.Boost | SystemCapability.SummerBypass | SystemCapability.HeatRecovery)
            ];
        }

        private static SystemTemplate Template(string ventilation)
        {
            return new SystemTemplate(ventilation, "RAD1", "UC1", "PR1", "CTL1", "1");
        }

        private static SystemCapabilityRequirement Requirement(SystemCapability systemCapability)
        {
            return new SystemCapabilityRequirement(systemCapability);
        }
    }
}
