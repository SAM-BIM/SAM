// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Enums;
using System.Collections.Generic;
using System.Linq;

namespace SAM.Analytical
{
    public static partial class Query
    {
        /// <summary>
        /// The zone categories in this model that a Part F calculation would find dwellings in, in name order.
        /// <para>
        /// Answers "which category holds the flats" without anybody having to hard-code "Flats". A caller that
        /// gets exactly one name can use it; more than one is genuinely ambiguous and is the user's choice to
        /// make; none means the model has no dwelling-zone structure at all, and the calculation's whole-house
        /// mode applies.
        /// </para>
        /// <para>
        /// <b>Deliberately the same rule as <c>PartFCalculator</c>'s own zone selection</b>, which is what
        /// makes the answer trustworthy - a category this returns really does size at least one dwelling:
        /// </para>
        /// <list type="bullet">
        /// <item>where NO zone in the category carries <see cref="ZoneParameter.IsDwelling"/>, every zone in it
        /// is sized as a dwelling. That is the legacy behaviour the calculation keeps so that models predating
        /// the parameter still work, so the category qualifies.</item>
        /// <item>otherwise only a zone with an explicit <c>IsDwelling = true</c> is a dwelling - an unmarked
        /// zone beside marked ones is NOT sized, and neither is an explicit false, which is how a shared
        /// corridor or a landlord area is kept out. The category qualifies where at least one such zone
        /// remains.</item>
        /// </list>
        /// <para>
        /// <c>PartFAirflowPresetTests.DwellingCategories_AgreeWithTheCalculator</c>, in SAM_UI where the
        /// fixture with real geometry lives, runs this and the calculator over the same model and asserts the
        /// two agree - so the duplicated rule cannot drift silently.
        /// </para>
        /// </summary>
        public static List<string> PartFDwellingZoneCategories(this AdjacencyCluster adjacencyCluster)
        {
            List<string> result = [];

            List<Zone> zones = adjacencyCluster?.GetZones();
            if (zones == null)
            {
                return result;
            }

            Dictionary<string, List<Zone>> dictionary = [];

            foreach (Zone zone in zones)
            {
                string name = zone?.GetValue<string>(ZoneParameter.ZoneCategory);
                if (string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }

                if (!dictionary.TryGetValue(name, out List<Zone> zones_Category))
                {
                    zones_Category = [];
                    dictionary[name] = zones_Category;
                }

                zones_Category.Add(zone);
            }

            foreach (KeyValuePair<string, List<Zone>> keyValuePair in dictionary)
            {
                bool marked = false;
                bool dwelling = false;

                foreach (Zone zone in keyValuePair.Value)
                {
                    //TryGetValue distinguishes "explicitly false" from "no value at all", which GetValue
                    //cannot: both come back as false, and a model predating the parameter would then look as
                    //though it held no dwellings.
                    if (!zone.TryGetValue(ZoneParameter.IsDwelling, out bool isDwelling))
                    {
                        continue;
                    }

                    marked = true;

                    if (isDwelling)
                    {
                        dwelling = true;
                        break;
                    }
                }

                if (!marked || dwelling)
                {
                    result.Add(keyValuePair.Key);
                }
            }

            result.Sort(System.StringComparer.Ordinal);

            return result;
        }
    }
}
