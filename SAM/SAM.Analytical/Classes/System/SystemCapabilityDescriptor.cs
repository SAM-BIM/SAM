// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Enums;
using SAM.Core;
using System;
using System.Text.Json.Nodes;

namespace SAM.Analytical
{
    /// <summary>
    /// One system, named by the identity it already has, and what it is able to do.
    /// <para>
    /// <b>The lightweight thing a selection reads instead of a template.</b> A shipped
    /// <c>SystemEnergyCentre</c> is between one and two megabytes of plant rooms, energy sources and
    /// schematic - and none of it answers "can this boost?" any faster than the four bits here do.
    /// Choosing a system must never open one.
    /// </para>
    /// <para>
    /// <b>No new identity.</b> The descriptor is keyed on <see cref="SystemTemplate"/>, exactly as the
    /// scenario is. It adds capabilities to a system that already has a name; it does not name a system.
    /// </para>
    /// <para>
    /// <b>The values do not live in this assembly.</b> Which of the shipped templates provides what is a
    /// fact about <c>SAM_Systems</c>' own resources, and belongs beside them. <c>SAM.Analytical</c> owns
    /// the vocabulary, the Part F requirement rule and the selection rule, and is handed descriptors -
    /// so it never carries a list of another repository's files, and a template added there does not
    /// need a change here.
    /// </para>
    /// </summary>
    public class SystemCapabilityDescriptor : IJSAMObject, IAnalyticalObject
    {
        private SystemTemplate systemTemplate = null;
        private SystemCapability systemCapability = SystemCapability.None;

        public SystemCapabilityDescriptor()
        {

        }

        public SystemCapabilityDescriptor(SystemTemplate systemTemplate, SystemCapability systemCapability)
        {
            //Copied: SystemTemplate is mutable, and a descriptor a selector is reading must not change
            //underneath it.
            this.systemTemplate = systemTemplate == null ? null : new SystemTemplate(systemTemplate);
            this.systemCapability = systemCapability;
        }

        public SystemCapabilityDescriptor(SystemCapabilityDescriptor systemCapabilityDescriptor)
        {
            if (systemCapabilityDescriptor != null)
            {
                systemTemplate = systemCapabilityDescriptor.systemTemplate == null ? null : new SystemTemplate(systemCapabilityDescriptor.systemTemplate);
                systemCapability = systemCapabilityDescriptor.systemCapability;
            }
        }

        public SystemCapabilityDescriptor(JsonObject jsonObject)
        {
            FromJsonObject(jsonObject);
        }

        /// <summary>The system's existing identity. A copy.</summary>
        public SystemTemplate SystemTemplate => systemTemplate == null ? null : new SystemTemplate(systemTemplate);

        /// <summary>What it is able to do.</summary>
        public SystemCapability Capabilities => systemCapability;

        /// <summary>
        /// How many capabilities it has - the measure "minimum suitable" is minimum by. A system that can
        /// do more than was asked is a heavier answer than one that can do exactly what was asked.
        /// </summary>
        public int CapabilityCount
        {
            get
            {
                int result = 0;

                for (int value = (int)systemCapability; value != 0; value >>= 1)
                {
                    result += value & 1;
                }

                return result;
            }
        }

        /// <summary>Whether the descriptor names a system and says anything about it.</summary>
        public bool IsValid => systemTemplate != null && systemTemplate.IsValid;

        /// <summary>
        /// Orders two descriptors by their system identity alone, field by field and ordinally.
        /// <para>
        /// This is the tie-break that makes a selection independent of the order descriptors arrived in -
        /// a library that enumerated a directory would otherwise let the file system decide an engineering
        /// answer. Field by field rather than through <c>ToString()</c>, which is a display format.
        /// </para>
        /// </summary>
        public static int CompareIdentity(SystemCapabilityDescriptor systemCapabilityDescriptor_1, SystemCapabilityDescriptor systemCapabilityDescriptor_2)
        {
            SystemTemplate systemTemplate_1 = systemCapabilityDescriptor_1?.systemTemplate;
            SystemTemplate systemTemplate_2 = systemCapabilityDescriptor_2?.systemTemplate;

            if (systemTemplate_1 == null || systemTemplate_2 == null)
            {
                return (systemTemplate_1 == null ? 0 : 1) - (systemTemplate_2 == null ? 0 : 1);
            }

            int result = Compare(systemTemplate_1.Ventilation, systemTemplate_2.Ventilation);
            if (result != 0)
            {
                return result;
            }

            result = Compare(systemTemplate_1.Heating, systemTemplate_2.Heating);
            if (result != 0)
            {
                return result;
            }

            result = Compare(systemTemplate_1.Cooling, systemTemplate_2.Cooling);
            if (result != 0)
            {
                return result;
            }

            result = Compare(systemTemplate_1.PlantRoom, systemTemplate_2.PlantRoom);
            if (result != 0)
            {
                return result;
            }

            result = Compare(systemTemplate_1.Controls, systemTemplate_2.Controls);
            if (result != 0)
            {
                return result;
            }

            return Compare(systemTemplate_1.Version, systemTemplate_2.Version);
        }

        public bool FromJsonObject(JsonObject jsonObject)
        {
            if (jsonObject == null)
            {
                return false;
            }

            systemTemplate = jsonObject["SystemTemplate"] is JsonObject jsonObject_SystemTemplate ? new SystemTemplate(jsonObject_SystemTemplate) : null;

            SystemCapabilityRequirement systemCapabilityRequirement = jsonObject["Capabilities"] is JsonArray ? new SystemCapabilityRequirement(jsonObject) : null;

            systemCapability = systemCapabilityRequirement == null ? SystemCapability.None : systemCapabilityRequirement.Capabilities;

            return true;
        }

        public JsonObject ToJsonObject()
        {
            JsonObject jsonObject = new()
            {
                ["_type"] = Core.Query.FullTypeName(this)
            };

            if (systemTemplate != null)
            {
                jsonObject["SystemTemplate"] = systemTemplate.ToJsonObject();
            }

            //The same shape a requirement writes, so one reader serves both sides of the boundary.
            JsonObject jsonObject_Capabilities = new SystemCapabilityRequirement(systemCapability).ToJsonObject();

            jsonObject["Capabilities"] = jsonObject_Capabilities["Capabilities"]?.DeepClone();

            return jsonObject;
        }

        public override string ToString()
        {
            return string.Format("{0} [{1}]", systemTemplate == null ? "-" : systemTemplate.ToString(), new SystemCapabilityRequirement(systemCapability));
        }

        private static int Compare(string text_1, string text_2)
        {
            return string.CompareOrdinal(text_1 ?? string.Empty, text_2 ?? string.Empty);
        }
    }
}
