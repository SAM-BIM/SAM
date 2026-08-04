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
    /// wraps it and reads zone membership fresh on every call. Per-Space classification is cached, but
    /// self-invalidates if a Space with the same Guid is later seen with a different Name - see
    /// classificationCacheName - since TM59Manager deliberately reuses one resolver instance across
    /// many Resolve calls over a live, mutable model.
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

        private static readonly string[] BedroomTypeKeys = { SingleBedroomConditionName, DoubleBedroomConditionName };

        private static readonly string[] DirectMatchKeys =
            NonHabitableConditionNames.Concat(BedroomTypeKeys).ToArray();

        private readonly TextMap textMap;
        private readonly InternalConditionLibrary internalConditionLibrary;

        private readonly Dictionary<Guid, TM59SpaceClassification> classificationCache = new Dictionary<Guid, TM59SpaceClassification>();
        private readonly Dictionary<Guid, string> matchedConditionNameCache = new Dictionary<Guid, string>();
        private readonly Dictionary<Guid, BedroomKeyword> explicitBedroomKeywordCache = new Dictionary<Guid, BedroomKeyword>();

        // The Name each Guid's cache entries above were computed from - Classify's only input besides
        // the Guid itself. TM59Manager deliberately reuses one resolver instance across many Resolve
        // calls, so a caller that constructs a new Space with the same Guid but a different Name (the
        // only way to "rename" a Space, since Name has no setter) must invalidate the stale entry
        // rather than have it silently returned forever.
        private readonly Dictionary<Guid, string> classificationCacheName = new Dictionary<Guid, string>();

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

            if (classificationCache.TryGetValue(space.Guid, out TM59SpaceClassification cached)
                && classificationCacheName.TryGetValue(space.Guid, out string cachedName)
                && string.Equals(cachedName, space.Name, StringComparison.Ordinal))
            {
                return cached;
            }

            TM59SpaceClassification classification = ClassifyCore(space, out string matchedConditionName, out BedroomKeyword explicitBedroomKeyword);

            classificationCache[space.Guid] = classification;
            matchedConditionNameCache[space.Guid] = matchedConditionName;
            explicitBedroomKeywordCache[space.Guid] = explicitBedroomKeyword;
            classificationCacheName[space.Guid] = space.Name;

            return classification;
        }

        private TM59SpaceClassification ClassifyCore(Space space, out string matchedConditionName, out BedroomKeyword explicitBedroomKeyword)
        {
            matchedConditionName = null;
            explicitBedroomKeyword = BedroomKeyword.None;

            string name = space?.Name;
            if (string.IsNullOrWhiteSpace(name) || textMap == null)
                return TM59SpaceClassification.Undefined;

            // Deliberately NOT TM59Manager.IsSleeping/IsLiving/IsCooking here: those use TextMap.GetSortedKeys,
            // whose substring scoring treats e.g. "room" as matching the "Sleeping" alias "bedroom" (a
            // whole word contains it) - so a plain "Living Room" would fuzzily read as Sleeping. The
            // classifier needs the same whole-token/phrase discipline as the direct-condition matches below.
            // TM59Manager's role methods stay untouched - SAM_Tas depends on their exact current behaviour.
            List<TM59TextMapMatch> sleepingMatches = textMap.TM59TextMapMatches(name, new[] { "Sleeping" });
            List<TM59TextMapMatch> livingMatches = textMap.TM59TextMapMatches(name, new[] { "Living" });
            List<TM59TextMapMatch> cookingMatches = textMap.TM59TextMapMatches(name, new[] { "Cooking" });

            bool sleeping = sleepingMatches.Count > 0;
            bool living = livingMatches.Count > 0;
            bool cooking = cookingMatches.Count > 0;
            bool roleExists = sleeping || living || cooking;

            int bestRoleTokenCount = 0;
            if (sleeping) bestRoleTokenCount = System.Math.Max(bestRoleTokenCount, sleepingMatches[0].TokenCount);
            if (living) bestRoleTokenCount = System.Math.Max(bestRoleTokenCount, livingMatches[0].TokenCount);
            if (cooking) bestRoleTokenCount = System.Math.Max(bestRoleTokenCount, cookingMatches[0].TokenCount);

            List<TM59TextMapMatch> bedroomTypeMatches = textMap.TM59TextMapMatches(name, BedroomTypeKeys);
            int bedroomTypeTokenCount = bedroomTypeMatches.Count > 0 ? bedroomTypeMatches[0].TokenCount : 0;

            List<TM59TextMapMatch> nonHabitableMatches = textMap.TM59TextMapMatches(name, NonHabitableConditionNames);
            int nonHabitableTokenCount = nonHabitableMatches.Count > 0 ? nonHabitableMatches[0].TokenCount : 0;

            // A bare bedroom-size modifier (single/double/twin/master/...) is weak, standalone evidence -
            // it only means "bedroom" when there is no more specific competing noun. So it must not beat a
            // non-habitable noun of equal or greater specificity (e.g. "Master Bathroom", "Double Bathroom",
            // "Twin Ensuite" must read as the bathroom condition, not Bedroom, even though "master"/"double"/
            // "twin" are also bedroom-size keywords) - hence the strict ">" below, mirroring tier 2's own
            // "generic keyword must be strictly more specific to win" rule but in the opposite direction.
            bool bedroomTypeBeatsNonHabitable = nonHabitableTokenCount == 0 || bedroomTypeTokenCount > nonHabitableTokenCount;

            // Tier 1: explicit bedroom-size keywords (Single/Double Bedroom). These are a REFINEMENT of the
            // Sleeping role, not a competing generic category, so a tie against the role's own best phrase
            // (e.g. bare "Twin" is simultaneously a Sleeping alias and a Single Bedroom alias, both 1 token)
            // is resolved in favour of the more specific bedroom-size reading - hence ">=", not ">" - but
            // only once it has already cleared the non-habitable check above.
            if (bedroomTypeMatches.Count > 0 && bedroomTypeBeatsNonHabitable && (!roleExists || bedroomTypeMatches[0].TokenCount >= bestRoleTokenCount))
            {
                string bedroomTypeKey = textMap.TM59BestTextMapKey(name, BedroomTypeKeys);
                if (bedroomTypeKey == SingleBedroomConditionName)
                {
                    explicitBedroomKeyword = BedroomKeyword.Single;
                    return TM59SpaceClassification.Bedroom;
                }

                if (bedroomTypeKey == DoubleBedroomConditionName)
                {
                    explicitBedroomKeyword = BedroomKeyword.Double;
                    return TM59SpaceClassification.Bedroom;
                }

                // bedroomTypeKey == null: "single" and "double" keywords tied with each other (e.g. a name
                // that somehow contains both at equal phrase length) - fall through and let the role-based
                // path decide Bedroom with no explicit size, rather than blocking the whole space.
            }

            // Tier 2: the 6 non-habitable conditions. These ARE a competing, generic category, so per
            // "generic keywords must not override Studio/Bedroom/Living Room/Kitchen/.../Bathroom/Ensuite"
            // they may only win when STRICTLY more specific (more tokens) than the best habitable role
            // match - a tie (e.g. "Kitchen"=Cooking vs "Store"=Cupboard, both 1 token) favours the role.
            //
            // A legacy Sleeping alias that is ALSO one of the bare bedroom-size modifiers (twin/double/dbl -
            // kept in that row only for SAM_Tas's own IsSleeping compatibility, see TM59ResourceTests) must
            // not be double-counted as independent role evidence once tier 1 has already tried and lost
            // that exact word against this same non-habitable match (e.g. "Twin Ensuite"/"Double Bathroom":
            // "twin"/"double" is simultaneously the Sleeping alias AND the losing bedroom-size candidate) -
            // otherwise the non-habitable noun could never win a case tier 1 already conceded.
            bool sleepingIsSpentBedroomTypeAlias = sleeping && !bedroomTypeBeatsNonHabitable
                && bedroomTypeMatches.Count > 0
                && string.Equals(sleepingMatches[0].Alias, bedroomTypeMatches[0].Alias, StringComparison.OrdinalIgnoreCase);

            int nonHabitableRoleComparisonTokenCount = bestRoleTokenCount;
            bool nonHabitableRoleExists = roleExists;
            if (sleepingIsSpentBedroomTypeAlias && !living && !cooking)
            {
                nonHabitableRoleComparisonTokenCount = 0;
                nonHabitableRoleExists = false;
            }

            if (nonHabitableMatches.Count > 0 && (!nonHabitableRoleExists || nonHabitableMatches[0].TokenCount > nonHabitableRoleComparisonTokenCount))
            {
                string nonHabitableKey = textMap.TM59BestTextMapKey(name, NonHabitableConditionNames);
                if (nonHabitableKey != null)
                {
                    matchedConditionName = nonHabitableKey;
                    return TM59SpaceClassification.NonHabitable;
                }

                // Ambiguous within the non-habitable tier itself (e.g. "Stair Lobby": "stair" vs "lobby",
                // both 1 token, both 5 characters) at a specificity that already dominates or matches any
                // role evidence - a genuine conflict the matcher must not silently guess at.
                return TM59SpaceClassification.Undefined;
            }

            // Tier 3: habitable role combination (unchanged priority order).
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

        /// <summary>True only for a finite, positive Area - never creates, updates, or infers one.</summary>
        private static bool TryGetArea(Space space, out double area)
        {
            area = 0;
            return space != null
                && space.TryGetValue(SpaceParameter.Area, out area)
                && !double.IsNaN(area)
                && !double.IsInfinity(area)
                && area > 0;
        }

        /// <summary>
        /// TM59 must never calculate, infer or write Area - it only validates what the model already has.
        /// A condition with a non-zero per-area equipment gain applied to a Space with no valid Area would
        /// otherwise silently produce a NaN (or a fabricated) gain downstream; this surfaces a diagnostic
        /// instead, directing the modeller to the existing SAM commands that actually establish Area.
        /// </summary>
        private static string ValidateAreaForPerAreaGain(Space space, InternalCondition condition)
        {
            if (condition == null)
                return null;

            if (!condition.TryGetValue(InternalConditionParameter.EquipmentSensibleGainPerArea, out double gainPerArea)
                || double.IsNaN(gainPerArea) || gainPerArea == 0)
                return null;

            if (TryGetArea(space, out _))
                return null;

            return $"'{condition.Name}' has a per-area equipment gain ({gainPerArea:0.##} W/m²) but this Space has no valid Area - " +
                   "TM59 does not calculate or write Area. Run SAMAnalytical.Check then SAMAnalytical.CalculateFloorArea before exporting.";
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
                    : ValidateAreaForPerAreaGain(space, nonHabitableCondition);

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
                    int withValidArea = unspecified.Count(x => TryGetArea(x, out _));
                    Space main;

                    if (withValidArea == unspecified.Count)
                    {
                        // every unresolved bedroom has a valid Area - area-based selection is safe.
                        main = unspecified
                            .OrderByDescending(x => { TryGetArea(x, out double a); return a; })
                            .ThenBy(x => x.Name, StringComparer.Ordinal)
                            .ThenBy(x => x.Guid)
                            .First();
                    }
                    else if (withValidArea == 0)
                    {
                        main = unspecified
                            .OrderBy(x => x.Name, StringComparer.Ordinal)
                            .ThenBy(x => x.Guid)
                            .First();
                        diagnostic = "No area data - main/double bedroom chosen by stable name/Guid ordering, not by size.";
                    }
                    else
                    {
                        // Partial: some bedrooms have Area, some don't. A missing Area must never be
                        // treated as zero (that would silently bias the "largest" comparison), so this
                        // falls back to the same stable, area-independent ordering as the no-area case.
                        main = unspecified
                            .OrderBy(x => x.Name, StringComparer.Ordinal)
                            .ThenBy(x => x.Guid)
                            .First();
                        diagnostic = "Partial bedroom area data - some bedrooms in this flat are missing Area, so the " +
                            "main/double bedroom was chosen by stable name/Guid ordering rather than size. Run " +
                            "SAMAnalytical.Check then SAMAnalytical.CalculateFloorArea to complete the area data.";
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

        /// <summary>Convenience wrapper: resolves the flat from the zone of the given category.</summary>
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

            // Read zone membership fresh every call, deliberately not memoized: TM59Manager reuses one
            // resolver instance across many Resolve calls, and a caller that adds/removes a Space from
            // this zone between two of those calls must see that change reflected immediately, not a
            // stale snapshot from the first call.
            List<Space> flatSpaces = adjacencyCluster.GetSpaces(zone) ?? new List<Space>();

            return Resolve(space, flatSpaces);
        }
    }
}
