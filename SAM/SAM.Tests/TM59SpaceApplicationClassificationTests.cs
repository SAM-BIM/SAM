// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical;
using SAM.Tests.Helpers;
using Xunit;

namespace SAM.Tests
{
    /// <summary>
    /// Pins the current, investigated-and-not-a-defect TM59 space-application outcome for a multi-bedroom
    /// apartment's Kitchen/Living Room InternalCondition - the finding behind the Flat1 report's
    /// <c>Kitchen_4</c> / <c>Kitchen_7</c> rows showing "Sleeping, Cooking" rather than "Cooking" alone.
    /// <para>
    /// <b>Root cause, traced to the token level.</b> <c>TM59InternalConditionResolver</c> names a
    /// multi-bedroom apartment's kitchen condition <c>"{bedroomCount} Bed Apt. Kitchen"</c> (e.g.
    /// <c>"2 Bed Apt. Kitchen"</c>) - the bedroom COUNT is part of the condition's own name.
    /// <c>TM59Manager.TM59SpaceApplications(InternalCondition, TextMap)</c> then classifies that whole name
    /// through <c>TextMap.GetSortedKeys</c>, which splits it into words and checks each one against every
    /// keyword in the map. The word "Bed" is both the apartment-size qualifier here AND, independently, one
    /// of the TM59 "Sleeping" keywords - so the same token that means "this apartment has 2 bedrooms" is
    /// read as "this room is used for sleeping".
    /// </para>
    /// <para>
    /// <b>Why it is reported here rather than fixed at the source.</b>
    /// <c>TM59Manager.IsSleeping</c>/<c>IsLiving</c>/<c>IsCooking</c>/<c>TM59SpaceApplications</c> are shared,
    /// widely depended-on entry points - <c>TM59InternalConditionResolver</c> itself documents "TM59Manager's
    /// role methods stay untouched - SAM_Tas depends on their exact current behaviour", and SAM_Tas's
    /// <c>RoomUse.cs</c>, <c>ToSAP.cs</c> and the legacy <c>OverheatingCalculator.cs</c> all call the same
    /// InternalCondition-based overloads directly. The mechanical &gt;26 °C numbers this classification feeds
    /// into are unaffected either way - <c>Sleeping</c> only changes which natural-ventilation result type a
    /// space gets routed to, and Kitchen_4/Kitchen_7 are mechanically ventilated - and the real Flat1 TAS
    /// comparison in <c>documentation/PartO-TAS-VALIDATION.md</c> already confirms both kitchens match TAS's
    /// own figures exactly. A surgical local fix (e.g. stripping the leading "N Bed Apt." qualifier before
    /// classifying) would still leave the pre-existing, separately-documented "room" vs "bedroom" substring
    /// collision in <c>GetSortedKeys</c> unaddressed for a non-bedroom Living Room in the same apartment size
    /// range, so a real fix belongs in a follow-up focused on <c>TM59Manager</c>'s matching engine itself, not
    /// bundled into this report-presentation task.
    /// </para>
    /// <para>
    /// This test exists so that follow-up work is a deliberate, visible change to this assertion, not a
    /// silent behaviour drift.
    /// </para>
    /// </summary>
    public class TM59SpaceApplicationClassificationTests
    {
        [Theory]
        [InlineData("2 Bed Apt. Kitchen")]
        [InlineData("3 Bed Apt. Kitchen")]
        public void MultiBedroomApartmentKitchenCondition_CurrentlyAlsoClassifiesAsSleeping_BedTokenCollision(string internalConditionName)
        {
            InternalCondition internalCondition = new(internalConditionName);

            System.Collections.Generic.List<TM59SpaceApplication> tM59SpaceApplications = TM59Manager.TM59SpaceApplications(internalCondition, TM59TestData.TextMap);

            Assert.Contains(TM59SpaceApplication.Cooking, tM59SpaceApplications);

            //The known artifact: "Bed" in the apartment-size qualifier is also a literal Sleeping keyword.
            Assert.Contains(TM59SpaceApplication.Sleeping, tM59SpaceApplications);
        }

        /// <summary>
        /// A single-word condition name with no apartment-size qualifier - "Double Bedroom", "Single
        /// Bedroom", "Studio" - is unaffected: the collision is specific to the compound
        /// "N Bed Apt. ..." naming convention, not a general failure to recognise Sleeping.
        /// </summary>
        [Theory]
        [InlineData("Double Bedroom")]
        [InlineData("Single Bedroom")]
        public void BedroomCondition_ClassifiesAsSleepingOnly(string internalConditionName)
        {
            InternalCondition internalCondition = new(internalConditionName);

            System.Collections.Generic.List<TM59SpaceApplication> tM59SpaceApplications = TM59Manager.TM59SpaceApplications(internalCondition, TM59TestData.TextMap);

            Assert.Contains(TM59SpaceApplication.Sleeping, tM59SpaceApplications);
            Assert.DoesNotContain(TM59SpaceApplication.Cooking, tM59SpaceApplications);
            Assert.DoesNotContain(TM59SpaceApplication.Living, tM59SpaceApplications);
        }
    }
}
