// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Enums;
using System.Collections.Generic;

namespace SAM.Analytical
{
    public static partial class Query
    {
        /// <summary>
        /// Chooses the <b>minimum</b> system that can do everything required, from the systems offered.
        /// <para>
        /// <b>A pure function.</b> It reads no file, opens no template, consults no library and holds no
        /// state - the systems available are an argument, because which systems exist is a fact about
        /// whoever is asking, not about the engineering. That is what keeps this side of the boundary free
        /// of any particular repository's shipping set, and it is why choosing a system can never cost
        /// the megabyte-and-a-half a <c>SystemEnergyCentre</c> template weighs.
        /// </para>
        /// <para>
        /// <b>Minimum means fewest capabilities, not first found.</b> A system that can do more than was
        /// asked is a heavier answer: it implies plant nobody required, and on a Part O assessment it would
        /// quietly credit the dwelling with mitigation the design does not have. Asked only for continuous
        /// ventilation, this returns <c>MV</c> and not <c>MVRE</c> - not because heat recovery is worse,
        /// but because it was not asked for.
        /// </para>
        /// <para>
        /// <b>Ties are broken by identity, never by position.</b> Where two systems are equally minimal the
        /// answer is the lower of the two identities, compared field by field and ordinally - so a caller
        /// that built its list by enumerating a directory gets the same answer as one that hard-coded it,
        /// and a file system's ordering cannot decide an engineering question.
        /// </para>
        /// <para>
        /// <b>It refuses rather than approximates.</b> Where nothing offered can do everything required the
        /// result says so and names what was missing. There is no nearest match and no default: returning
        /// a system that cannot meet the requirement would assess a building that was never designed.
        /// </para>
        /// </summary>
        /// <param name="systemCapabilityDescriptors">The systems available to choose from.</param>
        /// <param name="systemCapabilityRequirement">What the system has to be able to do.</param>
        public static SystemCapabilitySelection SelectMinimumCapableSystem(this IEnumerable<SystemCapabilityDescriptor> systemCapabilityDescriptors, SystemCapabilityRequirement systemCapabilityRequirement)
        {
            if (systemCapabilityRequirement == null || !systemCapabilityRequirement.IsValid)
            {
                //Nothing was asked for. Returning "the smallest system" would be inventing a requirement.
                return SystemCapabilitySelection.Refused("No system capability was required, so no system can be chosen.");
            }

            List<SystemCapabilityDescriptor> systemCapabilityDescriptors_Valid = [];

            foreach (SystemCapabilityDescriptor systemCapabilityDescriptor in systemCapabilityDescriptors ?? [])
            {
                if (systemCapabilityDescriptor != null && systemCapabilityDescriptor.IsValid)
                {
                    systemCapabilityDescriptors_Valid.Add(systemCapabilityDescriptor);
                }
            }

            if (systemCapabilityDescriptors_Valid.Count == 0)
            {
                return SystemCapabilitySelection.Refused(string.Format("No systems were offered to meet {0}.", systemCapabilityRequirement));
            }

            SystemCapabilityDescriptor systemCapabilityDescriptor_Result = null;

            //Everything the offered systems can do between them, so a refusal can say what none of them had
            //rather than merely that none of them fitted.
            SystemCapability systemCapability_Available = SystemCapability.None;

            foreach (SystemCapabilityDescriptor systemCapabilityDescriptor in systemCapabilityDescriptors_Valid)
            {
                systemCapability_Available |= systemCapabilityDescriptor.Capabilities;

                if (!systemCapabilityRequirement.IsMetBy(systemCapabilityDescriptor.Capabilities))
                {
                    continue;
                }

                if (systemCapabilityDescriptor_Result == null)
                {
                    systemCapabilityDescriptor_Result = systemCapabilityDescriptor;
                    continue;
                }

                int compare = systemCapabilityDescriptor.CapabilityCount - systemCapabilityDescriptor_Result.CapabilityCount;

                //Equally minimal, so identity decides - never the order they arrived in.
                if (compare == 0)
                {
                    compare = SystemCapabilityDescriptor.CompareIdentity(systemCapabilityDescriptor, systemCapabilityDescriptor_Result);
                }

                if (compare < 0)
                {
                    systemCapabilityDescriptor_Result = systemCapabilityDescriptor;
                }
            }

            if (systemCapabilityDescriptor_Result == null)
            {
                SystemCapability systemCapability_Missing = systemCapabilityRequirement.Missing(systemCapability_Available);

                //Every capability exists somewhere, just never together on one system. Saying "nothing was
                //missing" would read as a contradiction, so say the real thing instead.
                string reason = systemCapability_Missing == SystemCapability.None
                    ? string.Format("No single system offers {0}, although between them the {1} systems offered do.", systemCapabilityRequirement, systemCapabilityDescriptors_Valid.Count)
                    : string.Format("No system offered provides {0}, required by {1}.", new SystemCapabilityRequirement(systemCapability_Missing), systemCapabilityRequirement);

                return SystemCapabilitySelection.Refused(reason, systemCapability_Missing);
            }

            return SystemCapabilitySelection.Selected(systemCapabilityDescriptor_Result);
        }
    }
}
