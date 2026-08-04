// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;

namespace SAM.Analytical
{
    public class TM59Manager : IJSAMObject
    {
        private TextMap textMap;
        private TM59InternalConditionResolver resolver;
        private InternalConditionLibrary resolverLibrary;

        public TM59Manager(TextMap textMap)
        {
            this.textMap = textMap == null ? Query.DefaultInternalConditionTextMap_TM59() : Core.Create.TextMap(textMap);
        }

        public TM59Manager()
        {
            this.textMap = Query.DefaultInternalConditionTextMap_TM59();
        }
        public TM59Manager(System.Text.Json.Nodes.JsonObject jsonObject)

        {

            FromJsonObject(jsonObject);

        }
        public bool FromJsonObject(JsonObject jsonObject)
        {
            if (jsonObject == null)
            {
                return false;
            }

            if (jsonObject["TextMap"] is JsonObject textMapJson)
            {
                textMap = Core.Create.TextMap((JsonObject)textMapJson.DeepClone());
            }

            return true;
        }
        public JsonObject ToJsonObject()
        {
            JsonObject jsonObject = new JsonObject
            {
                ["_type"] = Core.Query.FullTypeName(this)
            };

            if (textMap?.ToJsonObject() is JsonObject textMapJson)
            {
                jsonObject["TextMap"] = textMapJson.DeepClone();
            }

            return jsonObject;
        }

        public bool IsSleeping(Space space)
        {
            return IsSleeping(space, textMap);
        }

        public bool IsSleeping(InternalCondition internalCondition)
        {
            return IsSleeping(internalCondition, textMap);
        }

        public bool IsLiving(Space space)
        {
            return IsLiving(space, textMap);
        }

        public bool IsLiving(InternalCondition internalCondition)
        {
            return IsLiving(internalCondition, textMap);
        }

        public bool IsCooking(Space space)
        {
            return IsCooking(space, textMap);
        }

        public bool IsCooking(InternalCondition internalCondition)
        {
            return IsCooking(internalCondition, textMap);
        }

        public List<TM59SpaceApplication> TM59SpaceApplications(Space space)
        {
            return TM59SpaceApplications(space, textMap);
        }
        public List<TM59SpaceApplication> TM59SpaceApplications(InternalCondition internalCondition)
        {
            return TM59SpaceApplications(internalCondition, textMap);
        }

        public int Occupancy(Space space)
        {
            return Count(space?.Name, textMap);
        }

        public int Occupancy(InternalCondition internalCondition)
        {
            return Count(internalCondition?.Name, textMap);
        }

        /// <summary>
        /// Builds (and caches) a TM59InternalConditionResolver for the given library, sharing this
        /// manager's TextMap. The resolver is deterministic and zone-independent for non-habitable
        /// spaces (corridors, bathrooms, risers, ...) - see GetInternalConditionResult.
        /// </summary>
        public TM59InternalConditionResolver Resolver(InternalConditionLibrary internalConditionLibrary)
        {
            if (resolver == null || !ReferenceEquals(resolverLibrary, internalConditionLibrary))
            {
                resolver = new TM59InternalConditionResolver(textMap, internalConditionLibrary);
                resolverLibrary = internalConditionLibrary;
            }

            return resolver;
        }

        /// <summary>
        /// Resolves a Space to a TM59 InternalCondition and returns the full result, including
        /// classification, occupancy and a Diagnostic explaining any manual-review outcome.
        /// </summary>
        public TM59InternalConditionResult GetInternalConditionResult(AdjacencyCluster adjacencyCluster, InternalConditionLibrary internalConditionLibrary, Space space, string zoneType)
        {
            return Resolver(internalConditionLibrary).Resolve(adjacencyCluster, space, zoneType);
        }

        /// <summary>Occupancy (people) for a resolved TM59 InternalConditionResult, per the TM59 occupancy convention.</summary>
        public static int TM59Occupancy(TM59InternalConditionResult result)
        {
            return result?.Occupancy ?? 0;
        }

        private static readonly Dictionary<string, int> tm59OccupancyByConditionName = new Dictionary<string, int>
        {
            ["Studio"] = 2,
            ["1 Bed Apt. Living Room/Kitchen"] = 2,
            ["1 Bed Apt. Living Room"] = 2,
            ["1 Bed Apt. Kitchen"] = 2,
            ["2 Bed Apt. Living Room/Kitchen"] = 3,
            ["2 Bed Apt. Living Room"] = 3,
            ["2 Bed Apt. Kitchen"] = 3,
            ["3 Bed Apt. Living Room/Kitchen"] = 4,
            ["3 Bed Apt. Living Room"] = 4,
            ["3 Bed Apt. Kitchen"] = 4,
            ["Double Bedroom"] = 2,
            ["Single Bedroom"] = 1,
        };

        /// <summary>
        /// Occupancy (people) for a TM59 InternalCondition, looked up by its name against the
        /// documented TM59 occupancy table - independent of any live resolver/flat context, for
        /// callers (e.g. the UI, after the dialog has closed) that only have the final condition.
        /// Returns 0 for non-habitable conditions or any name not in the table.
        /// </summary>
        public static int TM59Occupancy(InternalCondition internalCondition)
        {
            if (internalCondition?.Name == null)
            {
                return 0;
            }

            return tm59OccupancyByConditionName.TryGetValue(internalCondition.Name, out int occupancy) ? occupancy : 0;
        }

        public InternalCondition GetInternalCondition(AdjacencyCluster adjacencyCluster, InternalConditionLibrary internalConditionLibrary, Space space, string zoneType)
        {
            return GetInternalConditionResult(adjacencyCluster, internalConditionLibrary, space, zoneType)?.InternalCondition;
        }

        public static bool IsSleeping(string name, TextMap textMap)
        {
            return Is(name, textMap, "Sleeping");
        }

        public static bool IsSleeping(Space space, TextMap textMap)
        {
            return IsSleeping(space?.Name, textMap);
        }

        public static bool IsSleeping(InternalCondition internalCondition, TextMap textMap)
        {
            return IsSleeping(internalCondition?.Name, textMap);
        }

        public static bool IsLiving(Space space, TextMap textMap)
        {
            return IsLiving(space?.Name, textMap);
        }

        public static bool IsLiving(InternalCondition internalCondition, TextMap textMap)
        {
            return IsLiving(internalCondition?.Name, textMap);
        }

        public static bool IsLiving(string name, TextMap textMap)
        {
            return Is(name, textMap, "Living");
        }

        public static bool IsCooking(Space space, TextMap textMap)
        {
            return IsCooking(space?.Name, textMap);
        }

        public static bool IsCooking(InternalCondition internalCondition, TextMap textMap)
        {
            return IsCooking(internalCondition?.Name, textMap);
        }

        public static bool IsCooking(string name, TextMap textMap)
        {
            return Is(name, textMap, "Cooking");
        }

        public static List<TM59SpaceApplication> TM59SpaceApplications(Space space, TextMap textMap)
        {
            if (space == null || textMap == null)
            {
                return null;
            }

            List<TM59SpaceApplication> result = new List<TM59SpaceApplication>();
            if (IsSleeping(space, textMap))
            {
                result.Add(TM59SpaceApplication.Sleeping);
            }

            if (IsLiving(space, textMap))
            {
                result.Add(TM59SpaceApplication.Living);
            }

            if (IsCooking(space, textMap))
            {
                result.Add(TM59SpaceApplication.Cooking);
            }

            return result;

        }

        public static List<TM59SpaceApplication> TM59SpaceApplications(InternalCondition internalCondition, TextMap textMap)
        {
            if (internalCondition == null || textMap == null)
            {
                return null;
            }

            List<TM59SpaceApplication> result = new List<TM59SpaceApplication>();
            if (IsSleeping(internalCondition, textMap))
            {
                result.Add(TM59SpaceApplication.Sleeping);
            }

            if (IsLiving(internalCondition, textMap))
            {
                result.Add(TM59SpaceApplication.Living);
            }

            if (IsCooking(internalCondition, textMap))
            {
                result.Add(TM59SpaceApplication.Cooking);
            }

            return result;

        }

        private static int Count(string name, TextMap textMap)
        {
            if (string.IsNullOrWhiteSpace(name) || textMap == null)
            {
                return 0;
            }

            HashSet<string> values = textMap.GetSortedKeys(name);
            foreach (string value in values)
            {
                if (Core.Query.TryConvert(value, out int result))
                {
                    return result;
                }
            }

            return 0;
        }

        private static bool Is(string name, TextMap textMap, string key)
        {
            if (string.IsNullOrWhiteSpace(name) || textMap == null || string.IsNullOrEmpty(key))
            {
                return false;
            }

            HashSet<string> values = textMap.GetSortedKeys(name);
            return values != null && values.Contains(key);
        }

    }
}
