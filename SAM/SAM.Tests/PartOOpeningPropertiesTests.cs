// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical;
using SAM.Core;
using SAM.Geometry.Planar;
using SAM.Geometry.Spatial;
using SAM.Tests.Helpers;
using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;
using Xunit;
using AnalyticalCreate = SAM.Analytical.Create;

namespace SAM.Tests
{
    /// <summary>
    /// <b>Aperture-level opening restriction, independent of the Part O scenario/iteration vocabulary.</b>
    /// <para>
    /// <c>PartOOpeningProperties.OpeningRestriction</c> states whether, and when, an opening may be used for
    /// overheating ventilation - a closed <c>Unrestricted</c>/<c>NightClosed</c>/<c>AlwaysClosed</c> set
    /// rather than independent booleans, so an opening cannot state a contradictory combination. These
    /// tests cover the domain model in isolation: JSON compatibility with data serialised before this
    /// member existed, copy/clone semantics, the derived availability <c>Profile</c>, and
    /// <c>Modify.ResetPartOOpeningRestrictions</c>, which is what lets a BasePassive ("openings operated
    /// without restriction") iteration enforce its own assumption on a copy of the model.
    /// </para>
    /// <para>
    /// What this does NOT cover: the TAS-side write (schedule creation/reuse on a real TBD aperture type),
    /// which needs the TAS COM interop and is exercised by SAM_Tas's own TM59 test project instead.
    /// </para>
    /// </summary>
    public class PartOOpeningPropertiesTests
    {
        // -------------------------------------------------------------------------------------------------
        // Defaults and legacy compatibility
        // -------------------------------------------------------------------------------------------------

        [Fact]
        public void Default_IsUnrestricted_WithNoProfile()
        {
            PartOOpeningProperties partOOpeningProperties = new PartOOpeningProperties(1.0, 1.0, 30.0);

            Assert.Equal(OpeningRestriction.Unrestricted, partOOpeningProperties.OpeningRestriction);
            Assert.Null(partOOpeningProperties.Profile);
        }

        /// <summary>
        /// A <c>PartOOpeningProperties</c> serialised before <c>OpeningRestriction</c> existed carries no
        /// such key. It must deserialise to <c>Unrestricted</c> - the legacy behaviour - not to some other
        /// state, and it must not throw or drop the fields it does carry.
        /// </summary>
        [Fact]
        public void LegacyJson_WithNoOpeningRestrictionKey_DeserialisesAsUnrestricted()
        {
            string legacyJson = @"{
                ""_type"": ""SAM.Analytical.PartOOpeningProperties"",
                ""Width"": 1.2,
                ""Height"": 1.0,
                ""OpeningAngle"": 30.0,
                ""Factor"": 1.0
            }";

            PartOOpeningProperties partOOpeningProperties = SAM.Core.Create.IJSAMObject<PartOOpeningProperties>(legacyJson);

            Assert.NotNull(partOOpeningProperties);
            Assert.Equal(OpeningRestriction.Unrestricted, partOOpeningProperties.OpeningRestriction);
            Assert.Null(partOOpeningProperties.Profile);
            Assert.Equal(1.2, partOOpeningProperties.Width);
            Assert.Equal(1.0, partOOpeningProperties.Height);
        }

        [Fact]
        public void JsonRoundTrip_PreservesNightClosedAndHours()
        {
            PartOOpeningProperties partOOpeningProperties = new PartOOpeningProperties(1.2, 1.0, 30.0, OpeningRestriction.NightClosed, 7, 22);

            PartOOpeningProperties reconstructed = RoundTrip.Once(partOOpeningProperties);

            Assert.Equal(OpeningRestriction.NightClosed, reconstructed.OpeningRestriction);
            Assert.Equal(7, reconstructed.NightOpenFromHour);
            Assert.Equal(22, reconstructed.NightOpenToHour);
        }

        [Fact]
        public void JsonRoundTrip_PreservesAlwaysClosed()
        {
            PartOOpeningProperties partOOpeningProperties = new PartOOpeningProperties(1.2, 1.0, 30.0, OpeningRestriction.AlwaysClosed);

            PartOOpeningProperties reconstructed = RoundTrip.Once(partOOpeningProperties);

            Assert.Equal(OpeningRestriction.AlwaysClosed, reconstructed.OpeningRestriction);
            Assert.Null(reconstructed.Profile);
        }

        // -------------------------------------------------------------------------------------------------
        // Copy constructor
        // -------------------------------------------------------------------------------------------------

        [Fact]
        public void CopyConstructor_PreservesOpeningRestrictionAndHours()
        {
            PartOOpeningProperties partOOpeningProperties = new PartOOpeningProperties(1.2, 1.0, 30.0, OpeningRestriction.NightClosed, 9, 20);

            PartOOpeningProperties copy = new PartOOpeningProperties(partOOpeningProperties);

            Assert.Equal(OpeningRestriction.NightClosed, copy.OpeningRestriction);
            Assert.Equal(9, copy.NightOpenFromHour);
            Assert.Equal(20, copy.NightOpenToHour);
        }

        // -------------------------------------------------------------------------------------------------
        // Derived availability profile
        // -------------------------------------------------------------------------------------------------

        [Fact]
        public void NightClosed_DefaultWindow_ProducesDeterministicallyNamed24HourProfile()
        {
            PartOOpeningProperties partOOpeningProperties = new PartOOpeningProperties(1.2, 1.0, 30.0, OpeningRestriction.NightClosed);

            Profile profile = partOOpeningProperties.Profile;

            Assert.NotNull(profile);
            Assert.Equal("PartO_DayOpen_08_23", profile.Name);

            double[] values = profile.GetDailyValues();
            Assert.Equal(24, values.Length);

            for (int hour = 0; hour < 24; hour++)
            {
                double expected = (hour >= 8 && hour < 23) ? 1 : 0;
                Assert.Equal(expected, values[hour]);
            }
        }

        [Fact]
        public void NightClosed_CustomWindow_NamesAndValuesMatchTheWindow()
        {
            PartOOpeningProperties partOOpeningProperties = new PartOOpeningProperties(1.2, 1.0, 30.0, OpeningRestriction.NightClosed, 6, 21);

            Profile profile = partOOpeningProperties.Profile;

            Assert.NotNull(profile);
            Assert.Equal("PartO_DayOpen_06_21", profile.Name);

            double[] values = profile.GetDailyValues();
            for (int hour = 0; hour < 24; hour++)
            {
                double expected = (hour >= 6 && hour < 21) ? 1 : 0;
                Assert.Equal(expected, values[hour]);
            }
        }

        [Fact]
        public void TwoNightClosedInstances_WithTheSameWindow_ProduceTheSameProfileName()
        {
            //Reusability across apertures depends on this: the TAS-side writer looks the schedule up by
            //name before creating one, so the same window must always name the same profile.
            PartOOpeningProperties a = new PartOOpeningProperties(1.2, 1.0, 30.0, OpeningRestriction.NightClosed);
            PartOOpeningProperties b = new PartOOpeningProperties(0.6, 1.4, 90.0, OpeningRestriction.NightClosed);

            Assert.Equal(a.Profile.Name, b.Profile.Name);
        }

        [Fact]
        public void AlwaysClosed_HasNoProfile()
        {
            PartOOpeningProperties partOOpeningProperties = new PartOOpeningProperties(1.2, 1.0, 30.0, OpeningRestriction.AlwaysClosed);

            Assert.Null(partOOpeningProperties.Profile);
        }

        [Fact]
        public void Unrestricted_HasNoProfile()
        {
            PartOOpeningProperties partOOpeningProperties = new PartOOpeningProperties(1.2, 1.0, 30.0, OpeningRestriction.Unrestricted);

            Assert.Null(partOOpeningProperties.Profile);
        }

        // -------------------------------------------------------------------------------------------------
        // Modify.ResetPartOOpeningRestrictions
        // -------------------------------------------------------------------------------------------------

        private static readonly Construction wallConstruction = new Construction(Guid.NewGuid(), "Wall");
        private static readonly ApertureConstruction windowConstruction = new ApertureConstruction(Guid.NewGuid(), "Window", ApertureType.Window);

        private static Point3D P(double x, double y, double z) => new Point3D(x, y, z);

        private static Face3D WallFace() => new Face3D(new Polygon3D(new[] { P(0, 0, 0), P(10, 0, 0), P(10, 10, 0), P(0, 10, 0) }));
        private static Face3D ApertureFace() => new Face3D(new Polygon3D(new[] { P(1, 1, 0), P(3, 1, 0), P(3, 3, 0), P(1, 3, 0) }));

        [Fact]
        public void ResetPartOOpeningRestrictions_NightClosedAperture_BecomesUnrestricted()
        {
            Panel panel = AnalyticalCreate.Panel(wallConstruction, PanelType.Wall, WallFace());
            Aperture aperture = AnalyticalCreate.Aperture(windowConstruction, ApertureFace());
            aperture.AddSingleOpeningProperties(new PartOOpeningProperties(1.2, 1.0, 30.0, OpeningRestriction.NightClosed));
            panel.AddAperture(aperture);

            AdjacencyCluster adjacencyCluster = new AdjacencyCluster();
            adjacencyCluster.AddObject(panel);

            AnalyticalModel analyticalModel = new AnalyticalModel("Reset Test", null, null, null, adjacencyCluster);

            AnalyticalModel analyticalModel_Reset = analyticalModel.ResetPartOOpeningRestrictions(out List<string> notes);

            Assert.NotEmpty(notes);

            Aperture aperture_Reset = analyticalModel_Reset.AdjacencyCluster.GetApertures().Find(x => x.Guid == aperture.Guid);
            Assert.True(aperture_Reset.TryGetValue(ApertureParameter.OpeningProperties, out IOpeningProperties openingProperties_Reset));
            Assert.IsType<PartOOpeningProperties>(openingProperties_Reset);
            Assert.Equal(OpeningRestriction.Unrestricted, ((PartOOpeningProperties)openingProperties_Reset).OpeningRestriction);
        }

        /// <summary>Invariant §2/11: preparation works on a copy, and the original is never mutated.</summary>
        [Fact]
        public void ResetPartOOpeningRestrictions_OriginalModelIsUnchanged()
        {
            Panel panel = AnalyticalCreate.Panel(wallConstruction, PanelType.Wall, WallFace());
            Aperture aperture = AnalyticalCreate.Aperture(windowConstruction, ApertureFace());
            aperture.AddSingleOpeningProperties(new PartOOpeningProperties(1.2, 1.0, 30.0, OpeningRestriction.NightClosed));
            panel.AddAperture(aperture);

            AdjacencyCluster adjacencyCluster = new AdjacencyCluster();
            adjacencyCluster.AddObject(panel);

            AnalyticalModel analyticalModel = new AnalyticalModel("Reset Test", null, null, null, adjacencyCluster);

            string json_Before = SAM.Core.Convert.ToString(analyticalModel);

            analyticalModel.ResetPartOOpeningRestrictions(out List<string> _);

            string json_After = SAM.Core.Convert.ToString(analyticalModel);

            Assert.True(JsonEquivalence.AreEquivalent(json_Before, json_After, out string difference), difference);
        }

        /// <summary>Non-Part-O opening data (here, a plain OpeningProperties) is never this method's to change.</summary>
        [Fact]
        public void ResetPartOOpeningRestrictions_NonPartOOpeningProperties_IsLeftUntouched()
        {
            Panel panel = AnalyticalCreate.Panel(wallConstruction, PanelType.Wall, WallFace());
            Aperture aperture = AnalyticalCreate.Aperture(windowConstruction, ApertureFace());

            OpeningProperties openingProperties = new OpeningProperties(0.6) { Factor = 0.5 };
            aperture.AddSingleOpeningProperties(openingProperties);
            panel.AddAperture(aperture);

            AdjacencyCluster adjacencyCluster = new AdjacencyCluster();
            adjacencyCluster.AddObject(panel);

            AnalyticalModel analyticalModel = new AnalyticalModel("Reset Test", null, null, null, adjacencyCluster);

            AnalyticalModel analyticalModel_Reset = analyticalModel.ResetPartOOpeningRestrictions(out List<string> notes);

            Assert.Empty(notes);

            Aperture aperture_Reset = analyticalModel_Reset.AdjacencyCluster.GetApertures().Find(x => x.Guid == aperture.Guid);
            Assert.True(aperture_Reset.TryGetValue(ApertureParameter.OpeningProperties, out IOpeningProperties openingProperties_Reset));
            Assert.IsType<OpeningProperties>(openingProperties_Reset);
            Assert.Equal(0.5, ((OpeningProperties)openingProperties_Reset).Factor);
        }

        [Fact]
        public void ResetPartOOpeningRestrictions_AlreadyUnrestricted_ReturnsSameModelUnchanged()
        {
            Panel panel = AnalyticalCreate.Panel(wallConstruction, PanelType.Wall, WallFace());
            Aperture aperture = AnalyticalCreate.Aperture(windowConstruction, ApertureFace());
            aperture.AddSingleOpeningProperties(new PartOOpeningProperties(1.2, 1.0, 30.0));
            panel.AddAperture(aperture);

            AdjacencyCluster adjacencyCluster = new AdjacencyCluster();
            adjacencyCluster.AddObject(panel);

            AnalyticalModel analyticalModel = new AnalyticalModel("Reset Test", null, null, null, adjacencyCluster);

            AnalyticalModel analyticalModel_Reset = analyticalModel.ResetPartOOpeningRestrictions(out List<string> notes);

            Assert.Empty(notes);
            Assert.Same(analyticalModel, analyticalModel_Reset);
        }
    }
}
