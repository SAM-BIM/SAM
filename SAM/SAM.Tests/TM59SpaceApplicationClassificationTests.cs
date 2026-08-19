// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical;
using SAM.Tests.Helpers;
using System.Collections.Generic;
using Xunit;

namespace SAM.Tests
{
    /// <summary>
    /// Fixes the TM59 space-application defect behind the Flat1 report's <c>Kitchen_4</c> / <c>Kitchen_7</c>
    /// rows showing "Sleeping, Cooking" rather than "Cooking" alone.
    /// <para>
    /// <b>Root cause, traced to the token level.</b> <c>TM59InternalConditionResolver</c> names a
    /// multi-bedroom apartment's kitchen condition <c>"{bedroomCount} Bed Apt. Kitchen"</c> (e.g.
    /// <c>"1 Bed Apt. Kitchen"</c>) - the apartment's bedroom COUNT is part of the condition's own name.
    /// <c>TM59Manager.TM59SpaceApplications(InternalCondition, TextMap)</c> classifies that whole name
    /// through <c>TextMap.GetSortedKeys</c>, which splits it into words and checks each one against every
    /// keyword in the map. The word "Bed" is both the apartment-size qualifier here AND, independently, one
    /// of the TM59 "Sleeping" keywords - so the same token that means "this apartment has 1 bedroom" was
    /// read as "this room is used for sleeping".
    /// </para>
    /// <para>
    /// <b>The fix.</b> <c>TM59Manager.RoleMatchName</c> strips a leading apartment bedroom-count qualifier
    /// (<c>"N Bed Apt."</c>) before an InternalCondition's name is matched against the Sleeping/Living/
    /// Cooking keyword lists - applied only to the InternalCondition-based
    /// <c>IsSleeping</c>/<c>IsLiving</c>/<c>IsCooking</c> overloads (and therefore
    /// <c>TM59SpaceApplications(InternalCondition, TextMap)</c>, which calls them), never to Space-name
    /// classification. A plain condition name with no apartment-size qualifier - "Double Bedroom", "Single
    /// Bedroom", "Studio" - is untouched, since it never matches the prefix.
    /// </para>
    /// <para>
    /// <b>What this does not fix.</b> <c>TextMap.GetSortedKeys</c> has a separate, pre-existing "room" vs
    /// "bedroom" substring collision (the reason <c>TM59InternalConditionResolver</c> built its own
    /// whole-token matcher for Space classification rather than reuse it): a bare apartment "Living Room"
    /// condition (no "/Kitchen" suffix) still misreads Sleeping, because "room" is its own token there and
    /// "bedroom".Contains("room") is true. Recorded as remaining follow-up work in
    /// <c>documentation/PartO-TAS-VALIDATION.md</c>. The Kitchen and combined "Living Room/Kitchen" cases
    /// are unaffected by that separate bug, because "Kitchen" alone never collides with "bedroom" and
    /// "Room/Kitchen" (no space around the slash) is one token, not two.
    /// </para>
    /// </summary>
    public class TM59SpaceApplicationClassificationTests
    {
        [Theory]
        [InlineData("1 Bed Apt. Kitchen")]
        [InlineData("2 Bed Apt. Kitchen")]
        [InlineData("3 Bed Apt. Kitchen")]
        public void ApartmentKitchenCondition_ClassifiesAsCookingOnly_NotSleeping(string internalConditionName)
        {
            InternalCondition internalCondition = new(internalConditionName);

            List<TM59SpaceApplication> tM59SpaceApplications = TM59Manager.TM59SpaceApplications(internalCondition, TM59TestData.TextMap);

            Assert.Contains(TM59SpaceApplication.Cooking, tM59SpaceApplications);

            //The fixed defect: the apartment-size "Bed" token no longer reads as Sleeping evidence.
            Assert.DoesNotContain(TM59SpaceApplication.Sleeping, tM59SpaceApplications);
            Assert.DoesNotContain(TM59SpaceApplication.Living, tM59SpaceApplications);
        }

        /// <summary>The combined Living Room/Kitchen apartment condition is fixed the same way - "Room/Kitchen" is one token, so the separate "room" vs "bedroom" collision does not apply here.</summary>
        [Theory]
        [InlineData("1 Bed Apt. Living Room/Kitchen")]
        [InlineData("2 Bed Apt. Living Room/Kitchen")]
        [InlineData("3 Bed Apt. Living Room/Kitchen")]
        public void ApartmentLivingRoomKitchenCondition_ClassifiesAsLivingAndCooking_NotSleeping(string internalConditionName)
        {
            InternalCondition internalCondition = new(internalConditionName);

            List<TM59SpaceApplication> tM59SpaceApplications = TM59Manager.TM59SpaceApplications(internalCondition, TM59TestData.TextMap);

            Assert.Contains(TM59SpaceApplication.Living, tM59SpaceApplications);
            Assert.Contains(TM59SpaceApplication.Cooking, tM59SpaceApplications);
            Assert.DoesNotContain(TM59SpaceApplication.Sleeping, tM59SpaceApplications);
        }

        /// <summary>
        /// A single-word condition name with no apartment-size qualifier - "Double Bedroom", "Single
        /// Bedroom", "Studio" - is unaffected by the fix: the prefix strip only ever matches
        /// "N Bed Apt. ...", so plain bedroom/studio conditions never engage it.
        /// </summary>
        [Theory]
        [InlineData("Double Bedroom")]
        [InlineData("Single Bedroom")]
        public void BedroomCondition_ClassifiesAsSleepingOnly(string internalConditionName)
        {
            InternalCondition internalCondition = new(internalConditionName);

            List<TM59SpaceApplication> tM59SpaceApplications = TM59Manager.TM59SpaceApplications(internalCondition, TM59TestData.TextMap);

            Assert.Contains(TM59SpaceApplication.Sleeping, tM59SpaceApplications);
            Assert.DoesNotContain(TM59SpaceApplication.Cooking, tM59SpaceApplications);
            Assert.DoesNotContain(TM59SpaceApplication.Living, tM59SpaceApplications);
        }

        /// <summary>
        /// The fix is scoped to InternalCondition classification only. A Space literally named
        /// "1 Bed Apt. Kitchen" (not a realistic room name, but the boundary the fix must respect) still
        /// reads as Sleeping through the unmodified <c>IsSleeping(Space, TextMap)</c> path - proving the
        /// apartment-prefix strip was not applied to Space-name matching, which callers elsewhere rely on
        /// exactly as it was.
        /// </summary>
        [Fact]
        public void SpaceNameClassification_StillReadsTheRawBedToken_TheFixDoesNotTouchIt()
        {
            Space space = new("1 Bed Apt. Kitchen");

            Assert.True(TM59Manager.IsSleeping(space, TM59TestData.TextMap));
            Assert.True(TM59Manager.IsCooking(space, TM59TestData.TextMap));
        }
    }
}
