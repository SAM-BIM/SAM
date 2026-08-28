// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Collections.Generic;

namespace SAM.Analytical
{
    public static partial class Query
    {
        /// <summary>
        /// Every ventilation unit product offered that can move both duties - <b>sufficiency, and nothing
        /// else</b>.
        /// <para>
        /// <b>Both sides, independently.</b> A product is a candidate only when its supply capacity
        /// covers the supply duty <i>and</i> its extract capacity covers the extract duty. Checking a
        /// combined total instead would let a 200/50 unit answer a 100/100 dwelling.
        /// </para>
        /// <para>
        /// Returned smallest first - size, then the catalogue's rank, then product identity - so a caller
        /// that wants the smallest compliant unit takes the first, and a caller with a different policy
        /// has the whole compliant set to apply it to. The ordering is deterministic and independent of
        /// the order the descriptors arrived in.
        /// </para>
        /// <para>
        /// <b>A pure function.</b> It reads no file and consults no library - which products exist is an
        /// argument, because that is a fact about whoever is asking. This is the same boundary
        /// <see cref="CapableSystems"/> draws, and it is what keeps <c>SAM.Analytical</c> free of any
        /// particular repository's shipping set.
        /// </para>
        /// </summary>
        /// <param name="ventilationUnitCapacityDescriptors">The products available to choose from.</param>
        /// <param name="supplyDuty_Lps">The supply duty [l/s] the unit has to be able to move.</param>
        /// <param name="extractDuty_Lps">The extract duty [l/s] the unit has to be able to move.</param>
        /// <param name="tolerance_Lps">
        /// The margin in l/s within which a duty counts as met, so a unit rated at exactly the duty is
        /// sufficient rather than failing on a rounding bit.
        /// </param>
        public static List<VentilationUnitCapacityDescriptor> CapableVentilationUnits(this IEnumerable<VentilationUnitCapacityDescriptor> ventilationUnitCapacityDescriptors, double supplyDuty_Lps, double extractDuty_Lps, double tolerance_Lps = 0.001)
        {
            List<VentilationUnitCapacityDescriptor> result = [];

            if (double.IsNaN(supplyDuty_Lps) || double.IsNaN(extractDuty_Lps))
            {
                //No duty was stated, so nothing is compliant with it. Returning "every product" here would
                //let an unsized dwelling be given the smallest unit on the shelf.
                return result;
            }

            foreach (VentilationUnitCapacityDescriptor ventilationUnitCapacityDescriptor in ventilationUnitCapacityDescriptors ?? [])
            {
                if (ventilationUnitCapacityDescriptor is not null && ventilationUnitCapacityDescriptor.IsSufficientFor(supplyDuty_Lps, extractDuty_Lps, tolerance_Lps))
                {
                    result.Add(ventilationUnitCapacityDescriptor);
                }
            }

            //Sorted through an index list so the insertion index is available as the FINAL tie-break: two
            //descriptors of the same size, rank AND identity then keep the order they arrived in instead of
            //being ordered arbitrarily. The same arrangement, for the same reason, as Query.CapableSystems.
            List<int> indices = [];
            for (int i = 0; i < result.Count; i++)
            {
                indices.Add(i);
            }

            indices.Sort((x, y) =>
            {
                int compare = VentilationUnitCapacityDescriptor.Compare(result[x], result[y]);

                return compare != 0 ? compare : x.CompareTo(y);
            });

            List<VentilationUnitCapacityDescriptor> result_Ordered = [];
            foreach (int index in indices)
            {
                result_Ordered.Add(result[index]);
            }

            return result_Ordered;
        }

        /// <summary>
        /// Chooses the smallest ventilation unit product that can move both duties.
        /// <para>
        /// <b>Smallest compliant, never nearest.</b> Given a 115 l/s duty and units of 100, 150, 180 and
        /// 220, the answer is 150. An absolute-distance "nearest" would answer 100, which cannot ventilate
        /// the dwelling - the whole point of the rule is that being under is disqualifying and being over
        /// is merely headroom.
        /// </para>
        /// <para>
        /// <b>It refuses rather than approximates.</b> Where nothing offered can move both duties the
        /// result says so and names the largest capacity the catalogue had on the side that fell short -
        /// there is no undersized fallback, because a dwelling fitted with plant it outgrew would be
        /// assessed as a building nobody could build.
        /// </para>
        /// <para>
        /// <b>And it refuses rather than guesses.</b> Where two <i>different</i> products are the same
        /// size on both sides and the catalogue has given them the same rank, it has not said which is
        /// preferred, and breaking that tie on a name would let an alphabetical accident choose a
        /// dwelling's plant. Refusing on ambiguity is the rule
        /// <see cref="SelectPreferredCapableSystem"/> already follows for the same reason. One product
        /// listed twice is a duplicated entry, not a choice, and is answered normally.
        /// </para>
        /// </summary>
        public static VentilationUnitSelection SelectSmallestCapableVentilationUnit(this IEnumerable<VentilationUnitCapacityDescriptor> ventilationUnitCapacityDescriptors, double supplyDuty_Lps, double extractDuty_Lps, double tolerance_Lps = 0.001)
        {
            if (double.IsNaN(supplyDuty_Lps) || double.IsNaN(extractDuty_Lps))
            {
                return VentilationUnitSelection.Refused("No design duty was stated, so no ventilation unit can be chosen. Realize the Approved Document F requirements as design terminals first.", supplyDuty_Lps, extractDuty_Lps);
            }

            List<VentilationUnitCapacityDescriptor> ventilationUnitCapacityDescriptors_Valid = [];

            //The largest capacity anything offered had, on each side, so a refusal can say how far short the
            //catalogue fell rather than merely that it did.
            double maximumSupply_Lps = double.NaN;
            double maximumExtract_Lps = double.NaN;

            foreach (VentilationUnitCapacityDescriptor ventilationUnitCapacityDescriptor in ventilationUnitCapacityDescriptors ?? [])
            {
                if (ventilationUnitCapacityDescriptor is null || !ventilationUnitCapacityDescriptor.IsValid)
                {
                    continue;
                }

                ventilationUnitCapacityDescriptors_Valid.Add(ventilationUnitCapacityDescriptor);

                if (double.IsNaN(maximumSupply_Lps) || ventilationUnitCapacityDescriptor.MaximumSupplyFlowRate_Lps > maximumSupply_Lps)
                {
                    maximumSupply_Lps = ventilationUnitCapacityDescriptor.MaximumSupplyFlowRate_Lps;
                }

                if (double.IsNaN(maximumExtract_Lps) || ventilationUnitCapacityDescriptor.MaximumExtractFlowRate_Lps > maximumExtract_Lps)
                {
                    maximumExtract_Lps = ventilationUnitCapacityDescriptor.MaximumExtractFlowRate_Lps;
                }
            }

            if (ventilationUnitCapacityDescriptors_Valid.Count == 0)
            {
                return VentilationUnitSelection.Refused(
                    string.Format("No ventilation unit product was offered to meet a design duty of {0:0.###} l/s supply and {1:0.###} l/s extract.", supplyDuty_Lps, extractDuty_Lps),
                    supplyDuty_Lps,
                    extractDuty_Lps);
            }

            List<VentilationUnitCapacityDescriptor> ventilationUnitCapacityDescriptors_Capable = CapableVentilationUnits(ventilationUnitCapacityDescriptors_Valid, supplyDuty_Lps, extractDuty_Lps, tolerance_Lps);

            if (ventilationUnitCapacityDescriptors_Capable.Count == 0)
            {
                List<string> shortfalls = [];

                if (maximumSupply_Lps + tolerance_Lps < supplyDuty_Lps)
                {
                    shortfalls.Add(string.Format("the largest supply capacity offered is {0:0.###} l/s against a duty of {1:0.###} l/s", maximumSupply_Lps, supplyDuty_Lps));
                }

                if (maximumExtract_Lps + tolerance_Lps < extractDuty_Lps)
                {
                    shortfalls.Add(string.Format("the largest extract capacity offered is {0:0.###} l/s against a duty of {1:0.###} l/s", maximumExtract_Lps, extractDuty_Lps));
                }

                //Every side is individually covered by something, just never by one product. Saying "the
                //largest offered is too small" would read as a contradiction, so say the real thing instead.
                string reason = shortfalls.Count == 0
                    ? string.Format(
                        "No single ventilation unit product of the {0} offered can move both {1:0.###} l/s supply and {2:0.###} l/s extract, although between them they cover each side.",
                        ventilationUnitCapacityDescriptors_Valid.Count,
                        supplyDuty_Lps,
                        extractDuty_Lps)
                    : string.Format(
                        "No ventilation unit product offered can meet the design duty of {0:0.###} l/s supply and {1:0.###} l/s extract: {2}. Nothing was selected - an undersized unit is not an answer.",
                        supplyDuty_Lps,
                        extractDuty_Lps,
                        string.Join("; ", shortfalls));

                return VentilationUnitSelection.Refused(reason, supplyDuty_Lps, extractDuty_Lps);
            }

            VentilationUnitCapacityDescriptor result = ventilationUnitCapacityDescriptors_Capable[0];

            //Checked across every entry the ordering could not separate, not only index 1: the list is
            //sorted by size, then supply side, then rank, then identity, so a genuinely different product
            //can sit at index 2 or later behind a DUPLICATE of the preferred entry at index 1. Stopping at
            //index 1 would see two identical identities, correctly call that a duplicate rather than an
            //ambiguity, and never look far enough to find the real alternative one place on. The same trap
            //Query.SelectPreferredCapableSystem was hardened for.
            for (int i = 1; i < ventilationUnitCapacityDescriptors_Capable.Count; i++)
            {
                VentilationUnitCapacityDescriptor ventilationUnitCapacityDescriptor = ventilationUnitCapacityDescriptors_Capable[i];

                if (ventilationUnitCapacityDescriptor.MaximumSupplyFlowRate_Lps != result.MaximumSupplyFlowRate_Lps
                    || ventilationUnitCapacityDescriptor.MaximumExtractFlowRate_Lps != result.MaximumExtractFlowRate_Lps
                    || ventilationUnitCapacityDescriptor.Rank != result.Rank)
                {
                    break;
                }

                if (VentilationUnitCapacityDescriptor.CompareIdentity(ventilationUnitCapacityDescriptor, result) != 0)
                {
                    return VentilationUnitSelection.Refused(
                        string.Format(
                            "'{0}' and '{1}' are both the smallest product that can meet {2:0.###} l/s supply and {3:0.###} l/s extract, and both are ranked {4}, so the catalogue has not said which is preferred.",
                            result.VentilationUnitReference,
                            ventilationUnitCapacityDescriptor.VentilationUnitReference,
                            supplyDuty_Lps,
                            extractDuty_Lps,
                            result.Rank),
                        supplyDuty_Lps,
                        extractDuty_Lps);
                }
            }

            return VentilationUnitSelection.Selected(result, supplyDuty_Lps, extractDuty_Lps);
        }
    }
}
