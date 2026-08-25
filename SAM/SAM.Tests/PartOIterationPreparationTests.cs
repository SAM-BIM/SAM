// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical;
using SAM.Analytical.Enums;
using SAM.Geometry.Spatial;
using System;
using System.Collections.Generic;
using Xunit;
using AnalyticalCreate = SAM.Analytical.Create;

namespace SAM.Tests
{
    /// <summary>
    /// <b>Preparing a Part O iteration validates the model against the stage. It neither rewrites the model
    /// to fit the stage, nor labels the result with an assumption the model does not satisfy.</b>
    /// <para>
    /// <c>SAMAnalytical.PreparePartOIteration</c> exists to carry the Approved Document F airflows onto the
    /// internal conditions the simulation reads, and to state the scenarios the assessment is attributed to.
    /// It once did a third thing: it reset every <c>PartOOpeningProperties.OpeningRestriction</c> in the model
    /// to <c>Unrestricted</c> whenever the stage's "Openings Restricted" assumption was false. That was not
    /// the small change it looked like. <c>PartOOpeningProperties.Schedule</c> is <b>derived</b> from
    /// <c>OpeningRestriction</c>, not stored beside it, so the restriction IS the availability schedule's
    /// identity - and resetting it deleted the aperture's <c>PartO_DayOpen_HH_HH</c> schedule from the model
    /// that reached TAS. A modeller who authored <c>restriction_ = NightClosed</c> on
    /// <c>SAMAnalytical.AddOpeningPropertiesByPartO</c>, exactly as that component's own documented example
    /// says to, got a TBD with no Part O availability schedule on the aperture.
    /// </para>
    /// <para>
    /// The disagreement that leaves - a model TAS simulates with night closure while the stage states
    /// <c>Openings Restricted = false</c> - is <b>reported, not acted on</b>. Opening behaviour is
    /// orthogonal to the mitigation stage: a base case may legitimately mix restricted and unrestricted
    /// openings, so the stage-asserted assumption is the thing that is wrong, and moving it from the stage
    /// to the model is a separate change with its own scenario-identity consequences. These tests pin both
    /// halves - <b>the authored opening data survives untouched, and preparation neither rewrites it nor
    /// blocks on it.</b>
    /// </para>
    /// <para>
    /// <b>Why these test library calls and not the Grasshopper component.</b> <c>SAM.Tests</c> references no
    /// Grasshopper assembly, so <c>Prepared</c> below performs the same steps, in the same order and with the
    /// same gating, that <c>SAMAnalytical.PreparePartOIteration.SolveInstance</c> performs. Keeping the
    /// decision in the library rather than in the component is what makes it testable at all.
    /// </para>
    /// </summary>
    /// <remarks>
    /// Shares a collection with <c>PartFAirflowApplicationTests</c> - the suite's only other reader of the
    /// default Part F rule set - so the two never run at the same time. Both reach it through the process-wide
    /// <c>ActiveSetting.Setting</c>, whose lazy load and whose stored <c>PartFData</c> are shared by reference
    /// between every <c>PartFCalculator</c> built from them; running both classes concurrently made a wet
    /// room intermittently size no extract. Same precedent as the two readers of the default aperture
    /// construction library.
    /// </remarks>
    [Collection("SAM.Analytical.ActiveSetting default Part F data")]
    public class PartOIterationPreparationTests
    {
        // -------------------------------------------------------------------------------------------------
        // A. NightClosed under an iteration that assumes unrestricted openings
        // -------------------------------------------------------------------------------------------------

        /// <summary>
        /// The reported symptom, end to end in library terms: an aperture authored <c>NightClosed</c> still
        /// states <c>NightClosed</c> after preparation, and still derives the availability schedule the TAS
        /// aperture-control write looks for - name, hour count and all 24 values.
        /// </summary>
        [Fact]
        public void NightClosedAperture_SurvivesPreparation_WithItsAvailabilitySchedule()
        {
            Preparation preparation = Prepared(Model(OpeningRestriction.NightClosed), PartOIteration.BasePassive);

            PartOOpeningProperties partOOpeningProperties = PartO(preparation.AnalyticalModel);

            Assert.Equal(OpeningRestriction.NightClosed, partOOpeningProperties.OpeningRestriction);
            Assert.Equal(8, partOOpeningProperties.NightOpenFromHour);
            Assert.Equal(23, partOOpeningProperties.NightOpenToHour);

            DailyAvailabilitySchedule schedule = partOOpeningProperties.Schedule;

            Assert.NotNull(schedule);
            Assert.Equal("PartO_DayOpen_08_23", schedule.Name);
            Assert.Equal("000000001111111111111110", schedule.ValuesText);

            for (int hour = 0; hour < 24; hour++)
            {
                Assert.Equal(hour >= 8 && hour < 23, schedule[hour]);
            }
        }

        /// <summary>
        /// The other half of the same case: the disagreement between the model and the stage is REPORTED -
        /// naming the aperture, what it states and what the stage assumes - and nothing is blocked by it.
        /// The run stays successful and still states its scenario, because a base case may legitimately
        /// contain a night-closed opening; the stage-asserted assumption is what is wrong, not the model.
        /// </summary>
        [Fact]
        public void NightClosedAperture_UnderBasePassive_IsReportedWithoutBlocking()
        {
            Preparation preparation = Prepared(Model(OpeningRestriction.NightClosed), PartOIteration.BasePassive);

            Assert.Equal(PartOOpeningCompatibility.Incompatible, preparation.Compatibility);

            Assert.True(preparation.Successful);
            Assert.Empty(preparation.Refusals);
            Assert.Single(preparation.OverheatingScenarios);

            string summary = Assert.Single(preparation.Notes.FindAll(x => x.Contains("stage states")));

            Assert.Contains("Window", summary);
            Assert.Contains("NightClosed", summary);
            Assert.Contains("BasePassive", summary);
        }

        /// <summary>
        /// The availability WINDOW is authored data too, not only the fact of a restriction: a non-default
        /// window must come out the far side naming its own schedule, or the reuse-by-value contract on the
        /// TAS side silently collapses every window onto one schedule.
        /// </summary>
        [Fact]
        public void NightClosedAperture_CustomWindow_SurvivesPreparationUnchanged()
        {
            Preparation preparation = Prepared(Model(OpeningRestriction.NightClosed, 7, 22), PartOIteration.BasePassive);

            PartOOpeningProperties partOOpeningProperties = PartO(preparation.AnalyticalModel);

            Assert.Equal(7, partOOpeningProperties.NightOpenFromHour);
            Assert.Equal(22, partOOpeningProperties.NightOpenToHour);
            Assert.Equal("PartO_DayOpen_07_22", partOOpeningProperties.Schedule.Name);
        }

        /// <summary>
        /// <c>AlwaysClosed</c> is the other authored restriction, and it carries no schedule at all - it is
        /// expressed downstream as an opening factor of zero. It survives for the same reason: it is a
        /// statement about the building, not about the assessment stage.
        /// </summary>
        [Fact]
        public void AlwaysClosedAperture_SurvivesPreparation()
        {
            Preparation preparation = Prepared(Model(OpeningRestriction.AlwaysClosed), PartOIteration.BasePassive);

            Assert.Equal(OpeningRestriction.AlwaysClosed, PartO(preparation.AnalyticalModel).OpeningRestriction);
            Assert.Equal(PartOOpeningCompatibility.Incompatible, preparation.Compatibility);
            Assert.True(preparation.Successful);
        }

        /// <summary>
        /// <b>The root cause, pinned.</b> This is the operation preparation used to perform, run explicitly:
        /// resetting the restriction does not merely change a restriction state, it deletes the availability
        /// schedule with it, because the schedule is derived from the restriction and stored nowhere else.
        /// That is the whole reason preparation no longer does this, and it is what a caller of
        /// <c>Modify.ResetPartOOpeningRestrictions</c> - which remains available for callers who do intend
        /// that destructive change - is now told to expect.
        /// </summary>
        [Fact]
        public void TheResetPreparationUsedToPerform_DeletesTheAvailabilityScheduleWithTheRestriction()
        {
            AnalyticalModel analyticalModel = Model(OpeningRestriction.NightClosed);

            Assert.NotNull(PartO(analyticalModel).Schedule);

            AnalyticalModel analyticalModel_Reset = analyticalModel.ResetPartOOpeningRestrictions(out List<string> notes);

            Assert.NotEmpty(notes);
            Assert.Equal(OpeningRestriction.Unrestricted, PartO(analyticalModel_Reset).OpeningRestriction);
            Assert.Null(PartO(analyticalModel_Reset).Schedule);
        }

        // -------------------------------------------------------------------------------------------------
        // B. The matching case
        // -------------------------------------------------------------------------------------------------

        /// <summary>
        /// A genuinely unrestricted opening at a stage that assumes unrestricted openings: nothing to refuse,
        /// and the assessment proceeds exactly as it always did.
        /// </summary>
        [Fact]
        public void UnrestrictedAperture_UnderBasePassive_ProceedsAndStatesItsScenario()
        {
            Preparation preparation = Prepared(Model(OpeningRestriction.Unrestricted), PartOIteration.BasePassive);

            Assert.Equal(PartOOpeningCompatibility.Compatible, preparation.Compatibility);
            Assert.Empty(preparation.Refusals);
            Assert.True(preparation.Successful);
            Assert.Single(preparation.OverheatingScenarios);
            Assert.Equal(OverheatingOperatingAssumptions.Text(false), preparation.OverheatingScenarios[0].OperatingAssumptions.Value(Analytical.Query.OpeningsRestricted));
        }

        /// <summary>A model with no operable opening cannot contradict an opening assumption.</summary>
        [Fact]
        public void ModelWithNoOperableOpening_IsCompatible()
        {
            AnalyticalModel analyticalModel = Model(OpeningRestriction.Unrestricted, openingProperties: OpeningPropertiesKind.None);

            Assert.Equal(PartOOpeningCompatibility.Compatible, analyticalModel.PartOIterationOpeningCompatibility(PartOIteration.BasePassive, out string summary, out List<string> _));
            Assert.Null(summary);
        }

        /// <summary>A stage that states nothing about openings cannot disagree with the model about them.</summary>
        [Fact]
        public void UndefinedIteration_IsCompatible()
        {
            AnalyticalModel analyticalModel = Model(OpeningRestriction.NightClosed);

            Assert.Equal(PartOOpeningCompatibility.Compatible, analyticalModel.PartOIterationOpeningCompatibility(PartOIteration.Undefined, out string summary, out List<string> _));
            Assert.Null(summary);
        }

        // -------------------------------------------------------------------------------------------------
        // C. The reverse mismatch
        // -------------------------------------------------------------------------------------------------

        /// <summary>
        /// A stage that states restricted openings over a model that restricts nothing is the same
        /// mis-statement as the reverse, and must not be the direction that passes quietly once a mitigated
        /// stage gains a settled operating condition. Reported, and the model is not mutated to add a
        /// restriction either.
        /// </summary>
        [Fact]
        public void UnrestrictedModel_UnderAcousticRestricted_IsReportedWithoutMutatingTheModel()
        {
            AnalyticalModel analyticalModel = Model(OpeningRestriction.Unrestricted);

            string json_Before = SAM.Core.Convert.ToString(analyticalModel);

            PartOOpeningCompatibility partOOpeningCompatibility = analyticalModel.PartOIterationOpeningCompatibility(PartOIteration.AcousticRestricted, out string summary, out List<string> _);

            Assert.Equal(PartOOpeningCompatibility.Incompatible, partOOpeningCompatibility);
            Assert.NotNull(summary);
            Assert.Contains("AcousticRestricted", summary);
            Assert.Contains("Window", summary);

            Assert.Equal(json_Before, SAM.Core.Convert.ToString(analyticalModel));
        }

        /// <summary>
        /// A model that restricts SOME openings satisfies a stage that assumes restricted openings - which
        /// openings are restricted is the modeller's business, not this validation's.
        /// </summary>
        [Fact]
        public void PartlyRestrictedModel_UnderAcousticRestricted_IsCompatible()
        {
            AnalyticalModel analyticalModel = Model(OpeningRestriction.NightClosed, second: OpeningRestriction.Unrestricted);

            Assert.Equal(PartOOpeningCompatibility.Compatible, analyticalModel.PartOIterationOpeningCompatibility(PartOIteration.AcousticRestricted, out string summary, out List<string> _));
            Assert.Null(summary);
        }

        // -------------------------------------------------------------------------------------------------
        // D. ProfileOpeningProperties - the other authoring path
        // -------------------------------------------------------------------------------------------------

        /// <summary>
        /// A first-class <c>DailyAvailabilitySchedule</c> IS deterministically classifiable: it is binary and
        /// exactly 24 hours long. Available every hour is positively unrestricted, so the assessment proceeds.
        /// </summary>
        [Fact]
        public void ProfileOpeningProperties_WithAnAllAvailableSchedule_IsPositivelyUnrestricted()
        {
            Preparation preparation = Prepared(Model(OpeningRestriction.Unrestricted, openingProperties: OpeningPropertiesKind.ScheduleAlwaysAvailable), PartOIteration.BasePassive);

            Assert.Equal(PartOOpeningCompatibility.Compatible, preparation.Compatibility);
            Assert.True(preparation.Successful);
            Assert.Single(preparation.OverheatingScenarios);
        }

        /// <summary>
        /// The same carrier stating a schedule with hours off is positively RESTRICTED, and refuses under a
        /// stage that assumes unrestricted openings - the identical treatment a <c>NightClosed</c>
        /// <c>PartOOpeningProperties</c> gets. The two authoring paths must not disagree about the same
        /// engineering fact; that asymmetry is what gave the original defect away.
        /// </summary>
        [Fact]
        public void ProfileOpeningProperties_WithARestrictingSchedule_IsReportedUnderBasePassive()
        {
            Preparation preparation = Prepared(Model(OpeningRestriction.Unrestricted, openingProperties: OpeningPropertiesKind.ScheduleNightShut), PartOIteration.BasePassive);

            Assert.Equal(PartOOpeningCompatibility.Incompatible, preparation.Compatibility);
            Assert.True(preparation.Successful);
            Assert.Contains(preparation.Notes, x => x.Contains("Night Shut"));

            //And the schedule itself is untouched by the refusal.
            Assert.True(SingleAperture(preparation.AnalyticalModel).TryGetValue(ApertureParameter.OpeningProperties, out IOpeningProperties openingProperties));
            Assert.Equal("Night Shut", ((ProfileOpeningProperties)openingProperties).Schedule.Name);
        }

        /// <summary>
        /// <b>Unknown is not unrestricted.</b> The legacy general-valued <c>Profile</c> carrier states
        /// availability in a form with no deterministic reading as restricted or unrestricted. Assuming it
        /// unrestricted would recreate the original defect through the other authoring path, so it is
        /// reported as unknown - a different verdict from a proven disagreement, and said so.
        /// </summary>
        [Fact]
        public void ProfileOpeningProperties_WithOnlyALegacyProfile_IsReportedAsUnknown()
        {
            Preparation preparation = Prepared(Model(OpeningRestriction.Unrestricted, openingProperties: OpeningPropertiesKind.LegacyProfile), PartOIteration.BasePassive);

            Assert.Equal(PartOOpeningCompatibility.Unknown, preparation.Compatibility);
            Assert.True(preparation.Successful);

            string summary = Assert.Single(preparation.Notes.FindAll(x => x.Contains("cannot classify")));

            Assert.Contains("Night Shut", summary);
        }

        /// <summary>
        /// The unknown verdict is about UNCLASSIFIABLE availability, not about the carrier type. A
        /// <c>ProfileOpeningProperties</c> stating neither schedule nor profile supplies the TAS write with no
        /// schedule source at all, so the opening is available every hour - provably, not by assumption.
        /// </summary>
        [Fact]
        public void ProfileOpeningProperties_StatingNoAvailabilityAtAll_IsPositivelyUnrestricted()
        {
            AnalyticalModel analyticalModel = Model(OpeningRestriction.Unrestricted, openingProperties: OpeningPropertiesKind.ProfileCarrierWithNothing);

            Assert.Equal(PartOOpeningCompatibility.Compatible, analyticalModel.PartOIterationOpeningCompatibility(PartOIteration.BasePassive, out string summary, out List<string> _));
            Assert.Null(summary);
        }

        /// <summary>
        /// One pane nobody can classify makes the whole aperture unclassifiable, even beside a pane that is
        /// provably restricted: calling the aperture "restricted" would be a claim resting on a pane nobody
        /// has read.
        /// </summary>
        [Fact]
        public void PartOOpeningRestricted_UnknownDominatesWithinOneAperture()
        {
            Aperture aperture = AnalyticalCreate.Aperture(windowConstruction, ApertureFace());

            aperture.AddSingleOpeningProperties(new PartOOpeningProperties(1.2, 1.0, 30.0, OpeningRestriction.NightClosed));
            aperture.AddSingleOpeningProperties(new ProfileOpeningProperties(0.6, new Profile("Legacy", ProfileType.Other, Values(1))));

            Assert.Null(aperture.PartOOpeningRestricted(out string evidence));
            Assert.NotNull(evidence);
        }

        /// <summary>
        /// The classification table itself, read off what the TAS write will be given rather than off the
        /// authoring vocabulary. A restricted pane beside an unrestricted one restricts the aperture.
        /// </summary>
        [Fact]
        public void PartOOpeningRestricted_ClassifiesEachCarrier()
        {
            Assert.False(Restricted(new PartOOpeningProperties(1.2, 1.0, 30.0, OpeningRestriction.Unrestricted)));
            Assert.True(Restricted(new PartOOpeningProperties(1.2, 1.0, 30.0, OpeningRestriction.NightClosed)));
            Assert.True(Restricted(new PartOOpeningProperties(1.2, 1.0, 30.0, OpeningRestriction.AlwaysClosed)));

            Assert.False(Restricted(new ProfileOpeningProperties(0.6, AllAvailable())));
            Assert.True(Restricted(new ProfileOpeningProperties(0.6, NightShut())));
            Assert.False(Restricted(new ProfileOpeningProperties(0.6)));
            Assert.Null(Restricted(new ProfileOpeningProperties(0.6, new Profile("Legacy", ProfileType.Other, Values(1)))));

            Assert.False(Restricted(new OpeningProperties(0.6)));

            //Schedule wins over the legacy profile beside it - the same precedence the TAS write applies.
            Assert.True(Restricted(new ProfileOpeningProperties(0.6, new Profile("Legacy", ProfileType.Other, Values(1)), NightShut())));

            Aperture aperture = AnalyticalCreate.Aperture(windowConstruction, ApertureFace());
            aperture.AddSingleOpeningProperties(new PartOOpeningProperties(1.2, 1.0, 30.0, OpeningRestriction.Unrestricted));
            aperture.AddSingleOpeningProperties(new PartOOpeningProperties(0.6, 1.0, 30.0, OpeningRestriction.NightClosed));

            Assert.True(aperture.PartOOpeningRestricted(out string _));
        }

        // -------------------------------------------------------------------------------------------------
        // E. Unrelated schedules and profile references, including through a refusal
        // -------------------------------------------------------------------------------------------------

        /// <summary>
        /// Every profile reference an internal condition carries - occupancy, both equipment gains, lighting,
        /// infiltration, pollutant, heating, cooling, humidification, dehumidification and ventilation - must
        /// come out of preparation naming exactly the same profile. Preparation clones each sized space's
        /// internal condition, and a clone that copied a hand-picked subset of the fields would lose the rest
        /// silently. Run over the REFUSING case on purpose: a refusal must not be a licence to leave the
        /// model half-transformed.
        /// </summary>
        [Fact]
        public void Preparation_PreservesEveryProfileNameOnEveryInternalCondition()
        {
            AnalyticalModel analyticalModel = Model(OpeningRestriction.NightClosed);

            Dictionary<string, Dictionary<ProfileType, string>> before = ProfileNames(analyticalModel);

            Assert.NotEmpty(before);

            Dictionary<string, Dictionary<ProfileType, string>> after = ProfileNames(Prepared(analyticalModel, PartOIteration.BasePassive).AnalyticalModel);

            Assert.Equal(before.Count, after.Count);

            foreach (KeyValuePair<string, Dictionary<ProfileType, string>> keyValuePair in before)
            {
                Assert.True(after.ContainsKey(keyValuePair.Key), string.Format("Space '{0}' lost its internal condition.", keyValuePair.Key));

                Dictionary<ProfileType, string> profileNames = after[keyValuePair.Key];

                Assert.Equal(keyValuePair.Value.Count, profileNames.Count);

                foreach (KeyValuePair<ProfileType, string> keyValuePair_Profile in keyValuePair.Value)
                {
                    Assert.True(profileNames.ContainsKey(keyValuePair_Profile.Key), string.Format("Space '{0}' lost its {1} profile reference.", keyValuePair.Key, keyValuePair_Profile.Key));
                    Assert.Equal(keyValuePair_Profile.Value, profileNames[keyValuePair_Profile.Key]);
                }
            }
        }

        /// <summary>
        /// The system-type references beside the profile references, which are the other named lookup an
        /// internal condition carries into TAS.
        /// </summary>
        [Fact]
        public void Preparation_PreservesSystemTypeNames()
        {
            Preparation preparation = Prepared(Model(OpeningRestriction.NightClosed), PartOIteration.BasePassive);

            foreach (Space space in preparation.AnalyticalModel.GetSpaces())
            {
                InternalCondition internalCondition = space.InternalCondition;

                Assert.Equal("Ventilation System", internalCondition.GetSystemTypeName<VentilationSystemType>());
                Assert.Equal("Heating System", internalCondition.GetSystemTypeName<HeatingSystemType>());
                Assert.Equal("Cooling System", internalCondition.GetSystemTypeName<CoolingSystemType>());
            }
        }

        /// <summary>
        /// The profiles the names point at are model data too - a preserved name pointing into an emptied
        /// library resolves to nothing.
        /// </summary>
        [Fact]
        public void Preparation_PreservesTheProfileLibrary()
        {
            ProfileLibrary profileLibrary = Prepared(Model(OpeningRestriction.NightClosed), PartOIteration.BasePassive).AnalyticalModel.ProfileLibrary;

            Assert.NotNull(profileLibrary);

            Assert.NotNull(profileLibrary.GetProfile("Occupancy 24h", ProfileType.Occupancy));
            Assert.NotNull(profileLibrary.GetProfile("Heating Setpoint", ProfileType.Heating));
            Assert.NotNull(profileLibrary.GetProfile("Cooling Setpoint", ProfileType.Cooling));
            Assert.NotNull(profileLibrary.GetProfile("Infiltration Constant", ProfileType.Infiltration));
        }

        /// <summary>
        /// The parameters carried beside the restriction on the same opening properties - the TAS aperture
        /// function and the opening factor - are the fields a partial rebuild of the object would drop.
        /// </summary>
        [Fact]
        public void Preparation_PreservesTheOpeningFunctionAndFactor()
        {
            PartOOpeningProperties partOOpeningProperties = PartO(Prepared(Model(OpeningRestriction.NightClosed), PartOIteration.BasePassive).AnalyticalModel);

            Assert.True(partOOpeningProperties.TryGetValue(OpeningPropertiesParameter.Function, out string function));
            Assert.Equal("zdwno,0,19.00,21.00,99.00", function);
            Assert.Equal(0.75, partOOpeningProperties.Factor);
        }

        /// <summary>The model handed in is never the model handed back, and is never touched.</summary>
        [Fact]
        public void Preparation_LeavesTheSuppliedModelUnchanged()
        {
            AnalyticalModel analyticalModel = Model(OpeningRestriction.NightClosed);

            string json_Before = SAM.Core.Convert.ToString(analyticalModel);

            Prepared(analyticalModel, PartOIteration.BasePassive);

            Assert.Equal(json_Before, SAM.Core.Convert.ToString(analyticalModel));
        }

        // -------------------------------------------------------------------------------------------------
        // F. The change the iteration IS supposed to make still happens
        // -------------------------------------------------------------------------------------------------

        /// <summary>
        /// The guard against "fixing" schedule loss by disabling the transformation. Preparation must still
        /// write the Part F continuous design rate onto the internal condition the simulation reads, and must
        /// still clear the bases <c>CalculatedSupplyAirFlow</c> would otherwise SUM with it.
        /// </summary>
        [Fact]
        public void Preparation_StillAppliesThePartFRate_AndStillClearsTheSummingBases()
        {
            AnalyticalModel analyticalModel = Model(OpeningRestriction.Unrestricted, supplyAirFlowPerArea: 0.005);

            Space space_Before = analyticalModel.GetSpaces().Find(x => x.Name == "Bedroom 1");

            Assert.True(space_Before.InternalCondition.TryGetValue(InternalConditionParameter.SupplyAirFlowPerArea, out double perArea_Before) && perArea_Before > 0, "The fixture did not seed a competing per-area rate, so this test would prove nothing.");

            Preparation preparation = Prepared(analyticalModel, PartOIteration.BasePassive);

            Space space = preparation.AnalyticalModel.GetSpaces().Find(x => x.Name == "Bedroom 1");
            PartFSpaceData partFSpaceData = space.GetValue<PartFSpaceData>(SpaceParameter.PartFSpaceData);

            Assert.True(partFSpaceData.ContinuousSupplyFlowRate_Lps > 0, "The fixture did not size a supply rate, so this test would prove nothing.");

            //Read back through the query the simulation uses, so this is the number the export sees.
            Assert.Equal(partFSpaceData.ContinuousSupplyFlowRate_Lps.Value / 1000.0, space.CalculatedSupplyAirFlow(), 9);

            Assert.True(space.InternalCondition.TryGetValue(InternalConditionParameter.SupplyAirFlowPerArea, out double perArea));
            Assert.Equal(0, perArea);
        }

        /// <summary>
        /// A reported disagreement over opening behaviour does not disturb the Part F application: the rates
        /// are still applied, still readable through the query the simulation uses, and the run is still
        /// successful.
        /// </summary>
        [Fact]
        public void AReportedOpeningDisagreement_StillAppliesThePartFRates()
        {
            Preparation preparation = Prepared(Model(OpeningRestriction.NightClosed), PartOIteration.BasePassive);

            Assert.True(preparation.Successful);

            Space space = preparation.AnalyticalModel.GetSpaces().Find(x => x.Name == "Bedroom 1");
            PartFSpaceData partFSpaceData = space.GetValue<PartFSpaceData>(SpaceParameter.PartFSpaceData);

            Assert.Equal(partFSpaceData.ContinuousSupplyFlowRate_Lps.Value / 1000.0, space.CalculatedSupplyAirFlow(), 9);
        }

        // -------------------------------------------------------------------------------------------------
        // G. Repeated preparation
        // -------------------------------------------------------------------------------------------------

        /// <summary>
        /// Preparing an already-prepared model must not degrade it. Full model equality is deliberately NOT
        /// asserted: each pass gives every sized space its own freshly named, freshly guided internal
        /// condition, which is the documented behaviour that stops one room's Part F rate landing on another's.
        /// What must not drift is everything this task is about - the opening restrictions, the schedules
        /// derived from them, the profile references each condition carries, and the verdict itself.
        /// </summary>
        [Fact]
        public void PreparingTwice_LosesNoRestrictionScheduleOrProfileReference()
        {
            Preparation preparation_Once = Prepared(Model(OpeningRestriction.NightClosed), PartOIteration.BasePassive);
            Preparation preparation_Twice = Prepared(preparation_Once.AnalyticalModel, PartOIteration.BasePassive);

            Assert.Equal(OpeningRestriction.NightClosed, PartO(preparation_Twice.AnalyticalModel).OpeningRestriction);
            Assert.Equal("PartO_DayOpen_08_23", PartO(preparation_Twice.AnalyticalModel).Schedule.Name);
            Assert.Equal(preparation_Once.Compatibility, preparation_Twice.Compatibility);

            Dictionary<string, Dictionary<ProfileType, string>> once = ProfileNames(preparation_Once.AnalyticalModel);
            Dictionary<string, Dictionary<ProfileType, string>> twice = ProfileNames(preparation_Twice.AnalyticalModel);

            Assert.Equal(once.Count, twice.Count);

            foreach (KeyValuePair<string, Dictionary<ProfileType, string>> keyValuePair in once)
            {
                Assert.Equal(keyValuePair.Value, twice[keyValuePair.Key]);
            }
        }

        /// <summary>The compatible path is idempotent too, scenarios included.</summary>
        [Fact]
        public void PreparingTwice_OnACompatibleModel_StatesTheSameScenario()
        {
            Preparation preparation_Once = Prepared(Model(OpeningRestriction.Unrestricted), PartOIteration.BasePassive);
            Preparation preparation_Twice = Prepared(preparation_Once.AnalyticalModel, PartOIteration.BasePassive);

            Assert.True(preparation_Twice.Successful);
            Assert.Equal(preparation_Once.OverheatingScenarios[0].Key, preparation_Twice.OverheatingScenarios[0].Key);
        }

        // -------------------------------------------------------------------------------------------------
        // Fixture
        // -------------------------------------------------------------------------------------------------

        /// <summary>What one run of <c>SAMAnalytical.PreparePartOIteration</c> produced.</summary>
        private sealed class Preparation
        {
            public AnalyticalModel AnalyticalModel;
            public PartOOpeningCompatibility Compatibility;
            public List<string> Refusals = new List<string>();
            public List<string> Notes = new List<string>();
            public List<OverheatingScenario> OverheatingScenarios = new List<OverheatingScenario>();

            /// <summary>The component's own definition: nothing was refused.</summary>
            public bool Successful => Refusals.Count == 0;
        }

        /// <summary>
        /// The library steps <c>SAMAnalytical.PreparePartOIteration</c> performs, in the order it performs
        /// them and with the same gating - including that an incompatible or unclassifiable model states no
        /// scenario.
        /// </summary>
        private static Preparation Prepared(AnalyticalModel analyticalModel, PartOIteration partOIteration)
        {
            Preparation result = new Preparation();

            PartFOperatingMode? partFOperatingMode = partOIteration.PartOIterationOperatingMode(out string refusal_OperatingMode);

            Assert.True(partFOperatingMode.HasValue, refusal_OperatingMode);

            result.AnalyticalModel = analyticalModel.ApplyPartFVentilationRates(partFOperatingMode.Value, out List<string> refusals_Airflow, out List<string> notes);

            Assert.NotNull(result.AnalyticalModel);

            result.Refusals.AddRange(refusals_Airflow);
            result.Notes.AddRange(notes);

            result.Compatibility = result.AnalyticalModel.PartOIterationOpeningCompatibility(partOIteration, out string summary_Openings, out List<string> evidence);

            result.Notes.AddRange(evidence);

            //A note and a warning, never a refusal - see the class remarks. The verdict never gates the
            //scenario, because opening behaviour is orthogonal to the mitigation stage.
            if (summary_Openings != null)
            {
                result.Notes.Add(summary_Openings);
            }

            result.OverheatingScenarios = Scenarios(result.AnalyticalModel, partOIteration);

            return result;
        }

        /// <summary>The component's scenario half. Ungated: the opening verdict is advisory.</summary>
        private static List<OverheatingScenario> Scenarios(AnalyticalModel analyticalModel, PartOIteration partOIteration)
        {
            List<Zone> zones = analyticalModel.GetZones() ?? new List<Zone>();

            Assert.NotEmpty(zones);

            Dictionary<Guid, string> dictionary = new Dictionary<Guid, string>();
            foreach (Zone zone in zones)
            {
                dictionary[zone.Guid] = "MVRE";
            }

            return AnalyticalCreate.OverheatingScenarios(zones, partOIteration, dictionary, out List<string> _);
        }

        private static readonly Construction wallConstruction = new Construction(Guid.NewGuid(), "Wall");
        private static readonly ApertureConstruction windowConstruction = new ApertureConstruction(Guid.NewGuid(), "Window", ApertureType.Window);

        private static Point3D P(double x, double y, double z) => new Point3D(x, y, z);

        private static Face3D WallFace() => new Face3D(new Polygon3D(new Point3D[] { P(0, 0, 0), P(10, 0, 0), P(10, 10, 0), P(0, 10, 0) }));
        private static Face3D ApertureFace(double offset = 0) => new Face3D(new Polygon3D(new Point3D[] { P(1 + offset, 1, 0), P(3 + offset, 1, 0), P(3 + offset, 3, 0), P(1 + offset, 3, 0) }));

        /// <summary>Which opening carrier the fixture states, so each classification branch has a model.</summary>
        private enum OpeningPropertiesKind
        {
            /// <summary>A PartOOpeningProperties stating the restriction under test.</summary>
            PartO,

            /// <summary>No opening properties at all - not an operable opening.</summary>
            None,

            /// <summary>A first-class availability schedule, available all 24 hours.</summary>
            ScheduleAlwaysAvailable,

            /// <summary>A first-class availability schedule with the night hours off.</summary>
            ScheduleNightShut,

            /// <summary>The legacy general-valued carrier, with no schedule beside it.</summary>
            LegacyProfile,

            /// <summary>The profile carrier stating neither schedule nor profile.</summary>
            ProfileCarrierWithNothing,
        }

        private static bool? Restricted(IOpeningProperties openingProperties)
        {
            return openingProperties.PartOOpeningRestricted(out string _);
        }

        private static DailyAvailabilitySchedule AllAvailable()
        {
            bool[] values = new bool[DailyAvailabilitySchedule.HourCount];
            for (int hour = 0; hour < values.Length; hour++)
            {
                values[hour] = true;
            }

            return new DailyAvailabilitySchedule("Always Available", values);
        }

        private static DailyAvailabilitySchedule NightShut()
        {
            bool[] values = new bool[DailyAvailabilitySchedule.HourCount];
            for (int hour = 0; hour < values.Length; hour++)
            {
                values[hour] = hour >= 8 && hour < 23;
            }

            return new DailyAvailabilitySchedule("Night Shut", values);
        }

        private static PartOOpeningProperties PartO(AnalyticalModel analyticalModel)
        {
            Assert.True(SingleAperture(analyticalModel).TryGetValue(ApertureParameter.OpeningProperties, out IOpeningProperties openingProperties));

            return Assert.IsType<PartOOpeningProperties>(openingProperties);
        }

        private static Aperture SingleAperture(AnalyticalModel analyticalModel)
        {
            List<Aperture> apertures = analyticalModel.AdjacencyCluster.GetApertures();

            Assert.NotNull(apertures);

            return apertures[0];
        }

        /// <summary>Every profile reference every space's internal condition carries, keyed by space name.</summary>
        private static Dictionary<string, Dictionary<ProfileType, string>> ProfileNames(AnalyticalModel analyticalModel)
        {
            Dictionary<string, Dictionary<ProfileType, string>> result = new Dictionary<string, Dictionary<ProfileType, string>>();

            foreach (Space space in analyticalModel.GetSpaces())
            {
                InternalCondition internalCondition = space?.InternalCondition;
                if (internalCondition == null)
                {
                    continue;
                }

                result[space.Name] = internalCondition.GetProfileTypeDictionary();
            }

            return result;
        }

        /// <summary>
        /// A Part-F-sized dwelling whose internal conditions carry a full set of profile references, one zone
        /// to state a scenario over, and one wall carrying the authored opening data under test. The Part F
        /// sizing is the real calculator over the shipped rule set - nothing is stubbed, so what the tests
        /// read back is what the production path produces.
        /// </summary>
        private static AnalyticalModel Model(OpeningRestriction openingRestriction, int nightOpenFromHour = 8, int nightOpenToHour = 23, double? supplyAirFlowPerArea = null, OpeningPropertiesKind openingProperties = OpeningPropertiesKind.PartO, OpeningRestriction? second = null)
        {
            AdjacencyCluster adjacencyCluster = new AdjacencyCluster();

            //Named so the shared space-use classification recognises them.
            Dictionary<string, double> dictionary = new Dictionary<string, double>()
            {
                { "Living Room", 30.0 },
                { "Bedroom 1", 16.0 },
                { "Bedroom 2", 11.0 },
                { "Kitchen", 12.0 },
                { "Bathroom", 6.0 },
            };

            foreach (KeyValuePair<string, double> keyValuePair in dictionary)
            {
                Space space = new Space(keyValuePair.Key);

                space.SetValue(SpaceParameter.Area, keyValuePair.Value);
                space.SetValue(SpaceParameter.Volume, keyValuePair.Value * 2.5);

                InternalCondition internalCondition = new InternalCondition(keyValuePair.Key + " IC");

                internalCondition.SetProfileName(ProfileType.Occupancy, "Occupancy 24h");
                internalCondition.SetProfileName(ProfileType.EquipmentSensible, "Equipment Sensible Weekday");
                internalCondition.SetProfileName(ProfileType.EquipmentLatent, "Equipment Latent Weekday");
                internalCondition.SetProfileName(ProfileType.Lighting, "Lighting Weekday");
                internalCondition.SetProfileName(ProfileType.Infiltration, "Infiltration Constant");
                internalCondition.SetProfileName(ProfileType.Pollutant, "Pollutant Constant");
                internalCondition.SetProfileName(ProfileType.Heating, "Heating Setpoint");
                internalCondition.SetProfileName(ProfileType.Cooling, "Cooling Setpoint");
                internalCondition.SetProfileName(ProfileType.Humidification, "Humidification Setpoint");
                internalCondition.SetProfileName(ProfileType.Dehumidification, "Dehumidification Setpoint");
                internalCondition.SetProfileName(ProfileType.Ventilation, "Ventilation Continuous");

                internalCondition.SetValue(InternalConditionParameter.VentilationSystemTypeName, "Ventilation System");
                internalCondition.SetValue(InternalConditionParameter.HeatingSystemTypeName, "Heating System");
                internalCondition.SetValue(InternalConditionParameter.CoolingSystemTypeName, "Cooling System");

                if (supplyAirFlowPerArea.HasValue)
                {
                    internalCondition.SetValue(InternalConditionParameter.SupplyAirFlowPerArea, supplyAirFlowPerArea.Value);
                }

                space.InternalCondition = internalCondition;

                adjacencyCluster.AddObject(space);
            }

            ProfileLibrary profileLibrary = new ProfileLibrary("Part O Fixture");

            profileLibrary.Add(new Profile("Occupancy 24h", ProfileType.Occupancy, Values(1)));
            profileLibrary.Add(new Profile("Heating Setpoint", ProfileType.Heating, Values(20)));
            profileLibrary.Add(new Profile("Cooling Setpoint", ProfileType.Cooling, Values(24)));
            profileLibrary.Add(new Profile("Infiltration Constant", ProfileType.Infiltration, Values(0.25)));

            AnalyticalModel analyticalModel = new AnalyticalModel("Part O Dwelling", null, null, null, adjacencyCluster, null, profileLibrary);

            PartFCalculator partFCalculator = Analytical.Query.DefaultPartFCalculator();

            Assert.NotNull(partFCalculator);

            partFCalculator.AdjacencyCluster = analyticalModel.AdjacencyCluster;

            Assert.True(partFCalculator.Calculate(), "The Part F calculation did not run, so every test resting on it would be meaningless.");

            //The wall and the zone are added AFTER the sizing, so the fixture states exactly the openings under
            //test and the Part F calculation is the same one PartFAirflowApplicationTests exercises.
            AdjacencyCluster adjacencyCluster_Sized = partFCalculator.AdjacencyCluster;

            adjacencyCluster_Sized.AddObject(new Zone("Flat 1"));

            Panel panel = AnalyticalCreate.Panel(wallConstruction, PanelType.Wall, WallFace());

            Aperture aperture = AnalyticalCreate.Aperture(windowConstruction, ApertureFace());

            ISingleOpeningProperties singleOpeningProperties = null;
            switch (openingProperties)
            {
                case OpeningPropertiesKind.None:
                    break;

                case OpeningPropertiesKind.ScheduleAlwaysAvailable:
                    singleOpeningProperties = new ProfileOpeningProperties(0.6, AllAvailable());
                    break;

                case OpeningPropertiesKind.ScheduleNightShut:
                    singleOpeningProperties = new ProfileOpeningProperties(0.6, NightShut());
                    break;

                case OpeningPropertiesKind.LegacyProfile:
                    singleOpeningProperties = new ProfileOpeningProperties(0.6, new Profile("Night Shut", ProfileType.Other, Values(1)));
                    break;

                case OpeningPropertiesKind.ProfileCarrierWithNothing:
                    singleOpeningProperties = new ProfileOpeningProperties(0.6);
                    break;

                default:
                    singleOpeningProperties = new PartOOpeningProperties(1.2, 1.0, 30.0, openingRestriction, nightOpenFromHour, nightOpenToHour) { Factor = 0.75 };
                    break;
            }

            if (singleOpeningProperties != null)
            {
                singleOpeningProperties.SetValue(OpeningPropertiesParameter.Function, "zdwno,0,19.00,21.00,99.00");

                aperture.AddSingleOpeningProperties(singleOpeningProperties);
            }

            panel.AddAperture(aperture);

            if (second.HasValue)
            {
                Aperture aperture_Second = AnalyticalCreate.Aperture(windowConstruction, ApertureFace(4));
                aperture_Second.AddSingleOpeningProperties(new PartOOpeningProperties(1.2, 1.0, 30.0, second.Value));

                panel.AddAperture(aperture_Second);
            }

            adjacencyCluster_Sized.AddObject(panel);

            return new AnalyticalModel(analyticalModel, adjacencyCluster_Sized);
        }

        private static IEnumerable<double> Values(double value)
        {
            List<double> result = new List<double>();
            for (int i = 0; i < 24; i++)
            {
                result.Add(value);
            }

            return result;
        }
    }
}
