// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical;
using SAM.Tests.Helpers;
using System;
using Xunit;

namespace SAM.Tests
{
    /// <summary>
    /// Deterministic keyword matching: normalization, whole-token/phrase matching, and
    /// specific-beats-generic ranking, exercised through TM59InternalConditionResolver.Classify
    /// and Resolve against the real shipped TM59 TextMap/Library.
    /// </summary>
    public class TM59MatcherTests
    {
        private static Space NewSpace(string name) => new Space(Guid.NewGuid(), name, null);

        [Theory]
        [InlineData("Kitchen4", "kitchen")]
        [InlineData("Bedroom 23", "bedroom")]
        [InlineData("Ensuite5", "ensuite")]
        [InlineData("En-suite 5", "en suite")]
        [InlineData("Corridor1", "corridor")]
        [InlineData("Corridor", "corridor")]
        public void TM59NormalizedName_Strips_Trailing_Room_Numbers_And_Collapses_Punctuation(string input, string expected)
        {
            Assert.Equal(expected, Query.TM59NormalizedName(input));
        }

        [Theory]
        [InlineData("Corridor1", TM59SpaceClassification.NonHabitable)]
        [InlineData("Bathroom2", TM59SpaceClassification.NonHabitable)]
        [InlineData("Ensuite5", TM59SpaceClassification.NonHabitable)]
        [InlineData("En-suite 5", TM59SpaceClassification.NonHabitable)]
        [InlineData("WC 3", TM59SpaceClassification.NonHabitable)]
        [InlineData("Studio 10", TM59SpaceClassification.Studio)]
        [InlineData("Bedroom 3", TM59SpaceClassification.Bedroom)]
        [InlineData("Bedroom 4", TM59SpaceClassification.Bedroom)]
        [InlineData("Bedroom 13", TM59SpaceClassification.Bedroom)]
        [InlineData("Bedroom 23", TM59SpaceClassification.Bedroom)]
        [InlineData("Bedroom Space", TM59SpaceClassification.Bedroom)]
        [InlineData("Master Bedroom", TM59SpaceClassification.Bedroom)]
        [InlineData("Internal Corridor 2", TM59SpaceClassification.NonHabitable)]
        [InlineData("Corridor 2", TM59SpaceClassification.NonHabitable)]
        [InlineData("Communal Riser 1", TM59SpaceClassification.NonHabitable)]
        [InlineData("Riser 1", TM59SpaceClassification.NonHabitable)]
        [InlineData("Plant Room 3", TM59SpaceClassification.Undefined)]
        public void Classify_Matches_Expected_Classification(string name, TM59SpaceClassification expected)
        {
            TM59InternalConditionResolver resolver = TM59TestData.NewResolver();
            Assert.Equal(expected, resolver.Classify(NewSpace(name)));
        }

        [Fact]
        public void InternalCorridor_And_Corridor_Resolve_To_Different_NonHabitable_Conditions()
        {
            TM59InternalConditionResolver resolver = TM59TestData.NewResolver();

            TM59InternalConditionResult internalCorridor = resolver.Resolve(NewSpace("Internal Corridor 2"), null);
            TM59InternalConditionResult corridor = resolver.Resolve(NewSpace("Corridor 2"), null);

            Assert.Equal("TM59_Bathroom/internal corridors", internalCorridor.InternalCondition?.Name);
            Assert.Equal("TM59_Communal Corridor (including pipework gains)", corridor.InternalCondition?.Name);
        }

        [Fact]
        public void CommunalRiser_And_Riser_Resolve_To_Different_NonHabitable_Conditions()
        {
            TM59InternalConditionResolver resolver = TM59TestData.NewResolver();

            TM59InternalConditionResult communalRiser = resolver.Resolve(NewSpace("Communal Riser 1"), null);
            TM59InternalConditionResult riser = resolver.Resolve(NewSpace("Riser 1"), null);

            Assert.Equal("TM59_Riser Communal pipework", communalRiser.InternalCondition?.Name);
            Assert.Equal("TM59_Cupboard/riser/lift/void", riser.InternalCondition?.Name);
        }

        [Fact]
        public void Bathroom_And_Corridor_And_Ensuite_Resolve_To_The_Combined_Condition()
        {
            TM59InternalConditionResolver resolver = TM59TestData.NewResolver();

            foreach (string name in new[] { "Bathroom2", "Ensuite5", "En-suite 5" })
            {
                TM59InternalConditionResult result = resolver.Resolve(NewSpace(name), null);
                Assert.Equal("TM59_Bathroom/internal corridors", result.InternalCondition?.Name);
            }
        }

        [Fact]
        public void Corridor1_Resolves_To_Communal_Corridor()
        {
            TM59InternalConditionResolver resolver = TM59TestData.NewResolver();
            TM59InternalConditionResult result = resolver.Resolve(NewSpace("Corridor1"), null);

            Assert.Equal("TM59_Communal Corridor (including pipework gains)", result.InternalCondition?.Name);
        }

        [Fact]
        public void Unknown_Room_Type_Returns_Manual_Review_Without_Throwing()
        {
            TM59InternalConditionResolver resolver = TM59TestData.NewResolver();
            TM59InternalConditionResult result = resolver.Resolve(NewSpace("Plant Room 3"), null);

            Assert.Null(result.InternalCondition);
            Assert.False(string.IsNullOrWhiteSpace(result.Diagnostic));
        }

        [Fact]
        public void Generic_Space_Keyword_Never_Beats_A_Specific_Bedroom_Match()
        {
            // "space" is deliberately not a keyword for TM59_Cupboard/riser/lift/void, precisely so a
            // name like "Bedroom Space" is not misclassified as an unconditioned void.
            TM59InternalConditionResolver resolver = TM59TestData.NewResolver();
            Assert.Equal(TM59SpaceClassification.Bedroom, resolver.Classify(NewSpace("Bedroom Space")));
        }
    }
}
