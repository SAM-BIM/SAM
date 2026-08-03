// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SAM.Analytical
{
    /// <summary>
    /// Resolves a Space to a TM59 InternalCondition, deterministically and without requiring a zone
    /// for non-habitable spaces (corridors, bathrooms, risers, ...). Primary entry point is
    /// Resolve(Space, IEnumerable&lt;Space&gt;) - a pure function of the space and the other spaces in
    /// its flat/unit, so it is unit-testable without an AdjacencyCluster. The AdjacencyCluster overload
    /// wraps it and memoizes per Zone.
    /// </summary>
    public class TM59InternalConditionResolver
    {
        private enum BedroomKeyword { None, Single, Double }

        private static readonly string[] NonHabitableConditionNames = new[]
        {
            "TM59_Bathroom/internal corridors",
            "TM59_Communal Corridor (including pipework gains)",
            "TM59_Stairs",
            "TM59_Cupboard/riser/lift/void",
            "TM59_Cupboard with HIU",
            "TM59_Riser Communal pipework",
        };

        private const string SingleBedroomConditionName = "Single Bedroom";
        private const string DoubleBedroomConditionName = "Double Bedroom";
        private const string StudioConditionName = "Studio";

        private static readonly string[] DirectMatchKeys =
            NonHabitableConditionNames.Concat(new[] { SingleBedroomConditionName, DoubleBedroomConditionName }).ToArray();

        private readonly TextMap textMap;
        private readonly InternalConditionLibrary internalConditionLibrary;

        private readonly Dictionary<Guid, TM59SpaceClassification> classificationCache = new Dictionary<Guid, TM59SpaceClassification>();
        private readonly Dictionary<Guid, string> matchedConditionNameCache = new Dictionary<Guid, string>();
        private readonly Dictionary<Guid, BedroomKeyword> explicitBedroomKeywordCache = new Dictionary<Guid, BedroomKeyword>();
        private readonly Dictionary<Guid, List<Space>> flatSpacesCache = new Dictionary<Guid, List<Space>>();

        public TM59InternalConditionResolver(TextMap textMap, InternalConditionLibrary internalConditionLibrary)
        {
            this.textMap = textMap;
            this.internalConditionLibrary = internalConditionLibrary;
        }

        /// <summary>Single-space classification, independent of any flat/zone context.</summary>
        public TM59SpaceClassification Classify(Space space)
        {
            if (space == null)
                return TM59SpaceClassification.Undefined;

            if (classificationCache.TryGetValue(space.Guid, out TM59SpaceClassification cached))
                return cached;

            TM59SpaceClassification classification = ClassifyCore(space, out string matchedConditionName, out BedroomKeyword explicitBedroomKeyword);

            classificationCache[space.Guid] = classification;
            matchedConditionNameCache[space.Guid] = matchedConditionName;
            explicitBedroomKeywordCache[space.Guid] = explicitBedroomKeyword;

            return classification;
        }

        private TM59SpaceClassification ClassifyCore(Space space, out string matchedConditionName, out BedroomKeyword explicitBedroomKeyword)
        {
            matchedConditionName = null;
            explicitBedroomKeyword = BedroomKeyword.None;

            string name = space?.Name;
            if (string.IsNullOrWhiteSpace(name) || textMap == null)
                return TM59SpaceClassification.Undefined;

            string directKey = textMap.TM59BestTextMapKey(name, DirectMatchKeys);
            if (directKey != null)
            {
                if (NonHabitableConditionNames.Contains(directKey))
                {
                    matchedConditionName = directKey;
                    return TM59SpaceClassification.NonHabitable;
                }

                if (directKey == SingleBedroomConditionName)
                {
                    explicitBedroomKeyword = BedroomKeyword.Single;
                    return TM59SpaceClassification.Bedroom;
                }

                if (directKey == DoubleBedroomConditionName)
                {
                    explicitBedroomKeyword = BedroomKeyword.Double;
                    return TM59SpaceClassification.Bedroom;
                }
            }

            // Deliberately NOT TM59Manager.IsSleeping/IsLiving/IsCooking here: those use TextMap.GetSortedKeys,
            // whose substring scoring treats e.g. "room" as matching the "Sleeping" alias "bedroom" (a
            // whole word contains it) - so a plain "Living Room" would fuzzily read as Sleeping. The
            // classifier needs the same whole-token/phrase discipline as the direct-condition match above.
            // TM59Manager's role methods stay untouched - SAM_Tas depends on their exact current behaviour.
            bool sleeping = HasRole(name, "Sleeping");
            bool living = HasRole(name, "Living");
            bool cooking = HasRole(name, "Cooking");

            if (sleeping && living && cooking)
                return TM59SpaceClassification.Studio;

            if (sleeping)
                return TM59SpaceClassification.Bedroom;

            if (living && cooking)
                return TM59SpaceClassification.LivingRoomKitchen;

            if (cooking)
                return TM59SpaceClassification.Kitchen;

            if (living)
                return TM59SpaceClassification.LivingRoom;

            return TM59SpaceClassification.Undefined;
        }

        private bool HasRole(string name, string roleKey)
        {
            return textMap.TM59TextMapMatches(name, new[] { roleKey }).Count > 0;
        }

        private static bool IsHabitable(TM59SpaceClassification classification)
        {
            switch (classification)
            {
                case TM59SpaceClassification.Bedroom:
                case TM59SpaceClassification.LivingRoom:
                case TM59SpaceClassification.Kitchen:
                case TM59SpaceClassification.LivingRoomKitchen:
                case TM59SpaceClassification.Studio:
                    return true;
                default:
                    return false;
            }
        }

        private static int OccupancyForBedroomCount(int bedroomCount)
        {
            switch (bedroomCount)
            {
                case 1: return 2;
                case 2: return 3;
                case 3: return 4;
                default: return 0;
            }
        }

        private static bool TryGetArea(Space space, out double area)
        {
            area = 0;
            return space != null && space.TryGetValue(SpaceParameter.Area, out area) && area > 0;
        }

        /// <summary>
        /// Primary, unit-testable entry point. flatSpaces should include the flat's own spaces
        /// (space itself may or may not be included; both are handled). May be null/empty for
        /// zone-less non-habitable spaces.
        /// </summary>
        public TM59InternalConditionResult Resolve(Space space, IEnumerable<Space> flatSpaces)
        {
            if (space == null)
                return new TM59InternalConditionResult(null, TM59SpaceClassification.Undefined, 0, 0, "Space is null.");

            TM59SpaceClassification classification = Classify(space);

            if (classification == TM59SpaceClassification.NonHabitable)
            {
                string conditionName = matchedConditionNameCache.TryGetValue(space.Guid, out string cachedName) ? cachedName : null;
                InternalCondition nonHabitableCondition = internalConditionLibrary?.GetInternalConditions(conditionName)?.FirstOrDefault();
                string diagnostic = nonHabitableCondition == null
                    ? $"Matched non-habitable condition '{conditionName}' not found in the selected InternalConditionLibrary."
                    : null;

                return new TM59InternalConditionResult(nonHabitableCondition, classification, 0, 0, diagnostic);
            }

            List<Space> flatSpaces_Temp = (flatSpaces ?? Enumerable.Empty<Space>()).Where(x => x != null).ToList();
            if (flatSpaces_Temp.All(x => x.Guid != space.Guid))
                flatSpaces_Temp.Add(space);

            List<Space> bedroomSpaces = flatSpaces_Temp.Where(x => Classify(x) == TM59SpaceClassification.Bedroom).ToList();
            int bedroomCount = bedroomSpaces.Count;

            if (classification == TM59SpaceClassification.Undefined)
                return new TM59InternalConditionResult(null, classification, 0, bedroomCount, "No defensible automatic mapping - assign manually.");

            if (classification == TM59SpaceClassification.Studio)
                return ResolveNamedCondition(StudioConditionName, TM59SpaceClassification.Studio, 2, bedroomCount);

            if (classification == TM59SpaceClassification.LivingRoomKitchen && bedroomCount == 0)
            {
                List<Space> habitableSpaces = flatSpaces_Temp.Where(x => IsHabitable(Classify(x))).ToList();
                bool soleHabitableSpace = habitableSpaces.Count == 1 && habitableSpaces[0].Guid == space.Guid;

                if (soleHabitableSpace)
                    return ResolveNamedCondition(StudioConditionName, TM59SpaceClassification.Studio, 2, 0,
                        "Classified as Studio: sole combined living/kitchen space with no separate bedroom in the flat.");
            }

            if (classification == TM59SpaceClassification.Bedroom)
                return ResolveBedroom(space, bedroomSpaces, bedroomCount);

            // LivingRoom / Kitchen / LivingRoomKitchen (not the zero-bedroom Studio case above)
            if (bedroomCount == 0)
                return new TM59InternalConditionResult(null, classification, 0, 0,
                    "Flat has no identifiable bedroom - cannot determine apartment size. Assign manually.");

            if (bedroomCount > 3)
                return new TM59InternalConditionResult(null, classification, 0, bedroomCount,
                    $"Flat has {bedroomCount} bedrooms; TM59 library defines apartment sizes only up to 3-bed. Assign manually.");

            string suffix;
            switch (classification)
            {
                case TM59SpaceClassification.LivingRoomKitchen:
                    suffix = "Bed Apt. Living Room/Kitchen";
                    break;
                case TM59SpaceClassification.Kitchen:
                    suffix = "Bed Apt. Kitchen";
                    break;
                default:
                    suffix = "Bed Apt. Living Room";
                    break;
            }

            string apartmentConditionName = $"{bedroomCount} {suffix}";
            return ResolveNamedCondition(apartmentConditionName, classification, OccupancyForBedroomCount(bedroomCount), bedroomCount);
        }

        private TM59InternalConditionResult ResolveNamedCondition(string conditionName, TM59SpaceClassification classification, int occupancy, int bedroomCount, string diagnostic = null)
        {
            InternalCondition internalCondition = internalConditionLibrary?.GetInternalConditions(conditionName)?.FirstOrDefault();
            if (internalCondition == null)
                diagnostic = $"Condition '{conditionName}' not found in the selected InternalConditionLibrary.";

            return new TM59InternalConditionResult(internalCondition, classification, occupancy, bedroomCount, diagnostic);
        }

        private TM59InternalConditionResult ResolveBedroom(Space space, List<Space> bedroomSpaces, int bedroomCount)
        {
            bool isDouble;
            string diagnostic = null;

            BedroomKeyword ownKeyword = explicitBedroomKeywordCache.TryGetValue(space.Guid, out BedroomKeyword cachedKeyword) ? cachedKeyword : BedroomKeyword.None;

            if (ownKeyword == BedroomKeyword.Double)
            {
                isDouble = true;
            }
            else if (ownKeyword == BedroomKeyword.Single)
            {
                isDouble = false;
            }
            else if (bedroomCount <= 1)
            {
                // TM59 convention: a flat's sole bedroom is the main/double bedroom.
                isDouble = true;
            }
            else
            {
                List<Space> explicitDouble = bedroomSpaces.Where(x => ExplicitKeyword(x) == BedroomKeyword.Double).ToList();
                List<Space> unspecified = bedroomSpaces.Where(x => ExplicitKeyword(x) == BedroomKeyword.None).ToList();

                if (explicitDouble.Any(x => x.Guid == space.Guid))
                {
                    isDouble = true;
                }
                else if (explicitDouble.Count > 0)
                {
                    // another bedroom already explicitly claims Double; this one defaults to Single.
                    isDouble = false;
                }
                else if (unspecified.Count == 0)
                {
                    // every bedroom explicitly named Single - respect that literally, no forced Double.
                    isDouble = false;
                }
                else
                {
                    bool anyArea = unspecified.Any(x => TryGetArea(x, out _));
                    Space main;
                    if (anyArea)
                    {
                        main = unspecified
                            .OrderByDescending(x => { TryGetArea(x, out double a); return a; })
                            .ThenBy(x => x.Name, StringComparer.Ordinal)
                            .ThenBy(x => x.Guid)
                            .First();
                    }
                    else
                    {
                        main = unspecified
                            .OrderBy(x => x.Name, StringComparer.Ordinal)
                            .ThenBy(x => x.Guid)
                            .First();
                        diagnostic = "No area data - main/double bedroom chosen by stable name/Guid ordering, not by size.";
                    }

                    isDouble = main.Guid == space.Guid;
                }
            }

            string conditionName = isDouble ? DoubleBedroomConditionName : SingleBedroomConditionName;
            InternalCondition internalCondition = internalConditionLibrary?.GetInternalConditions(conditionName)?.FirstOrDefault();
            if (internalCondition == null)
                diagnostic = $"Condition '{conditionName}' not found in the selected InternalConditionLibrary.";

            int occupancy = isDouble ? 2 : 1;
            return new TM59InternalConditionResult(internalCondition, TM59SpaceClassification.Bedroom, occupancy, bedroomCount, diagnostic);
        }

        private BedroomKeyword ExplicitKeyword(Space space)
        {
            Classify(space);
            return explicitBedroomKeywordCache.TryGetValue(space.Guid, out BedroomKeyword keyword) ? keyword : BedroomKeyword.None;
        }

        /// <summary>Convenience wrapper: resolves the flat from the zone of the given category and memoizes per Zone.Guid.</summary>
        public TM59InternalConditionResult Resolve(AdjacencyCluster adjacencyCluster, Space space, string zoneCategory)
        {
            if (space == null)
                return new TM59InternalConditionResult(null, TM59SpaceClassification.Undefined, 0, 0, "Space is null.");

            if (adjacencyCluster == null)
                return Resolve(space, null);

            Zone zone = adjacencyCluster.GetZones(space, zoneCategory)?.FirstOrDefault();
            if (zone == null)
            {
                // Non-habitable spaces need no zone at all (defect: corridors/bathrooms were previously
                // unreachable because GetInternalCondition returned null before any name matching ran).
                if (Classify(space) == TM59SpaceClassification.NonHabitable)
                    return Resolve(space, null);

                return new TM59InternalConditionResult(null, Classify(space), 0, 0,
                    string.IsNullOrWhiteSpace(zoneCategory)
                        ? "Space is not assigned to any zone - select a Zone Type to enable automatic mapping."
                        : $"Space is not assigned to a zone of category '{zoneCategory}'.");
            }

            if (!flatSpacesCache.TryGetValue(zone.Guid, out List<Space> flatSpaces))
            {
                flatSpaces = adjacencyCluster.GetSpaces(zone) ?? new List<Space>();
                flatSpacesCache[zone.Guid] = flatSpaces;
            }

            return Resolve(space, flatSpaces);
        }
    }
}
