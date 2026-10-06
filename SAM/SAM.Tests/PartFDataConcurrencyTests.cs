// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical;
using SAM.Analytical.Enums;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Xunit;

namespace SAM.Tests
{
    /// <summary>
    /// Regression tests for the shared state behind Part F room classification: the two lazily built lookups
    /// in <see cref="PartFData"/> (the space use map behind <see cref="PartFData.GetPartFCategory(SpaceUse)"/>
    /// and the legacy name map used for categories with no SpaceUse) and the per-space cache of the
    /// <see cref="SpaceSemanticsResolver"/> it holds.
    /// </summary>
    /// <remarks>
    /// One <see cref="PartFData"/> is shared by every caller of <c>ActiveSetting</c>, so the first lookups can
    /// arrive from several threads at once. Both maps used to be published empty and then filled in place: a
    /// second thread could read one half filled (a wet room classified as nothing, or as the wrong category)
    /// or while it was being written (<c>InvalidOperationException: Operations that change non-concurrent
    /// collections must have exclusive access</c>, seen once in a parallel run of the full suite).
    /// </remarks>
    public class PartFDataConcurrencyTests
    {
        private const string firstBedroomName = "Bedroom (first)";
        private const string lastBedroomName = "Bedroom (last)";
        private const string kitchenName = "Kitchen";
        private const string bathroomName = "Bathroom";
        private const string undefinedName = "Legacy";

        /// <summary>
        /// Many duplicate bedroom categories between the kitchen and the bathroom. They lose under first
        /// category wins, so they never change the result; they exist to keep the map in the middle of being
        /// built for long enough that the other threads, released together, are certain to reach it then.
        /// </summary>
        private const int duplicateCount = 20000;

        private const int roundCount = 20;

        /// <summary>
        /// The categories in insertion order: the first bedroom, the kitchen, a legacy category with no
        /// SpaceUse, the duplicate bedrooms, the bathroom, then one more bedroom. The bathroom is deliberately
        /// near the end, so a reader that sees a half filled map misses it.
        /// </summary>
        private static Dictionary<string, PartFCategory> Categories()
        {
            Dictionary<string, PartFCategory> result = [];

            void Add(string name, PartFType partFType, PartFVentilationType partFVentilationType, double? minFlowRate_Lps, SpaceUse spaceUse)
            {
                result[name] = new PartFCategory(name, partFType, partFVentilationType, spaceUse == SpaceUse.Bedroom, minFlowRate_Lps,
                    true, true, partFVentilationType == PartFVentilationType.supply, partFVentilationType == PartFVentilationType.extract,
                    "RoomVolume", [name.ToLowerInvariant()], spaceUse: spaceUse);
            }

            Add(firstBedroomName, PartFType.Habitable, PartFVentilationType.supply, null, SpaceUse.Bedroom);
            Add(kitchenName, PartFType.WetRoom, PartFVentilationType.extract, 13, SpaceUse.Kitchen);
            Add(undefinedName, PartFType.WetRoom, PartFVentilationType.extract, 6, SpaceUse.Undefined);

            for (int i = 0; i < duplicateCount; i++)
            {
                Add($"Bedroom duplicate {i}", PartFType.Habitable, PartFVentilationType.supply, null, SpaceUse.Bedroom);
            }

            Add(bathroomName, PartFType.WetRoom, PartFVentilationType.extract, 8, SpaceUse.Bathroom);
            Add(lastBedroomName, PartFType.Habitable, PartFVentilationType.supply, null, SpaceUse.Bedroom);

            return result;
        }

        /// <summary>
        /// The bathroom first: it is the space use the old implementation lost when read mid build.
        /// </summary>
        private static readonly SpaceUse[] queries = [SpaceUse.Bathroom, SpaceUse.Bedroom, SpaceUse.Kitchen, SpaceUse.Storage, SpaceUse.Undefined];

        /// <summary>
        /// The lookup on a single thread, which is the behaviour the concurrent case must reproduce: the first
        /// of several categories sharing a space use wins, a space use with no category - and Undefined - give
        /// null, and a category with no SpaceUse is not reachable through this lookup.
        /// </summary>
        [Fact]
        public void GetPartFCategory_FirstCategoryWins_SingleThreaded()
        {
            Dictionary<string, PartFCategory> categories = Categories();
            PartFData partFData = new() { PartFCategories = categories };

            Assert.Same(categories[firstBedroomName], partFData.GetPartFCategory(SpaceUse.Bedroom));
            Assert.Same(categories[kitchenName], partFData.GetPartFCategory(SpaceUse.Kitchen));
            Assert.Same(categories[bathroomName], partFData.GetPartFCategory(SpaceUse.Bathroom));
            Assert.Null(partFData.GetPartFCategory(SpaceUse.Storage));
            Assert.Null(partFData.GetPartFCategory(SpaceUse.Undefined));
        }

        /// <summary>
        /// Every thread released into the very first lookups of a fresh <see cref="PartFData"/> together gets
        /// exactly the single threaded answer, with no exception, round after round. Each round uses a new
        /// instance so that every round exercises the lazy build, not the already published map.
        /// </summary>
        [Fact]
        public void GetPartFCategory_ConcurrentFirstUse_NoExceptionAndSameCategoriesAsSingleThreaded()
        {
            Dictionary<string, PartFCategory> categories = Categories();

            Dictionary<SpaceUse, PartFCategory?> expected = new()
            {
                [SpaceUse.Bathroom] = categories[bathroomName],
                [SpaceUse.Bedroom] = categories[firstBedroomName],
                [SpaceUse.Kitchen] = categories[kitchenName],
                [SpaceUse.Storage] = null,
                [SpaceUse.Undefined] = null,
            };

            for (int round = 0; round < roundCount; round++)
            {
                PartFData partFData = new() { PartFCategories = categories };

                RunConcurrently(round, mismatches =>
                {
                    foreach (SpaceUse spaceUse in queries)
                    {
                        PartFCategory? partFCategory = partFData.GetPartFCategory(spaceUse);
                        if (!ReferenceEquals(partFCategory, expected[spaceUse]))
                        {
                            mismatches.Enqueue($"{spaceUse}: expected '{expected[spaceUse]?.Name ?? "null"}', got '{partFCategory?.Name ?? "null"}'");
                        }
                    }
                });

                //The published map still answers correctly once the race is over.
                foreach (KeyValuePair<SpaceUse, PartFCategory?> keyValuePair in expected)
                {
                    Assert.Same(keyValuePair.Value, partFData.GetPartFCategory(keyValuePair.Key));
                }
            }
        }

        // ------------------------------------------------------------------
        // Legacy path: categories with no SpaceUse, matched by name
        // ------------------------------------------------------------------

        private const string zorbName = "Zorb";
        private const string quaxZorbName = "Quax Zorb";
        private const string vellAName = "Vell A";
        private const string vellBName = "Vell B";
        private const string mirkName = "Mirk";

        /// <summary>
        /// Legacy categories ahead of the targets that complete the answers below. They match none of the
        /// queried names; they exist only to keep the legacy map in the middle of being built for long enough
        /// that the released threads reach it then.
        /// </summary>
        private const int legacyFillerCount = 5000;

        /// <summary>
        /// Legacy categories (no SpaceUse) in insertion order. The generic and the first tied category come
        /// first and the categories that change each answer come last, so a reader of a half filled map gets
        /// a wrong answer rather than only a missing one:
        /// <list type="bullet">
        /// <item>"Quax Zorb 1" is "Quax Zorb" (the longer phrase); half built, it is the generic "Zorb";</item>
        /// <item>"Vell" ties between "Vell A" and "Vell B", so it is null; half built, it is "Vell A";</item>
        /// <item>"Mirk 2" is "Mirk", matched by the category name because it has no synonyms; half built, null.</item>
        /// </list>
        /// The names are invented so the shared space use vocabulary classifies none of them, which keeps every
        /// lookup on the legacy path.
        /// </summary>
        private static Dictionary<string, PartFCategory> LegacyCategories()
        {
            Dictionary<string, PartFCategory> result = [];

            void Add(string name, List<string>? synonyms)
            {
                result[name] = new PartFCategory(name, PartFType.WetRoom, PartFVentilationType.extract, false, 8,
                    true, true, false, true, "RoomVolume", synonyms!);
            }

            Add(zorbName, ["zorb"]);
            Add(vellAName, ["vell"]);

            for (int i = 0; i < legacyFillerCount; i++)
            {
                Add($"Filler {i}", [$"filler {i} plinth"]);
            }

            Add(quaxZorbName, ["quax zorb"]);
            Add(vellBName, ["vell"]);
            Add(mirkName, null);

            return result;
        }

        private static Dictionary<string, string?> LegacyExpectedNames()
        {
            return new Dictionary<string, string?>
            {
                ["Quax Zorb 1"] = quaxZorbName,
                ["Vell"] = null,
                ["Mirk 2"] = mirkName,
                ["Zorb"] = zorbName,
                ["Nothing Here"] = null,
            };
        }

        /// <summary>
        /// The legacy lookup on a single thread: the longest matching phrase wins, an equal top rank between
        /// two categories gives null rather than a guess, and a category without synonyms matches on its name.
        /// </summary>
        [Fact]
        public void GetPartFCategory_Legacy_SingleThreaded()
        {
            Dictionary<string, PartFCategory> categories = LegacyCategories();
            PartFData partFData = new() { PartFCategories = categories };

            foreach (KeyValuePair<string, string?> keyValuePair in LegacyExpectedNames())
            {
                PartFCategory? partFCategory = partFData.GetPartFCategory(keyValuePair.Key);
                Assert.Same(keyValuePair.Value is null ? null : categories[keyValuePair.Value], partFCategory);
            }
        }

        /// <summary>
        /// The legacy name map built concurrently: every thread released into the first legacy lookups of a
        /// fresh <see cref="PartFData"/> gets exactly the single threaded answer, with no exception.
        /// <para>
        /// The spaces are resolved once through <see cref="PartFData.SpaceSemanticsResolver"/> before the
        /// threads start. That builds the resolver and fills its per-space cache without touching the legacy
        /// map, so the threads only read the resolver and the lazy build under test is the only first-time
        /// work they race on.
        /// </para>
        /// </summary>
        [Fact]
        public void GetPartFCategory_Legacy_ConcurrentFirstUse_NoExceptionAndSameCategoriesAsSingleThreaded()
        {
            Dictionary<string, PartFCategory> categories = LegacyCategories();

            Dictionary<string, string?> expectedNames = LegacyExpectedNames();

            for (int round = 0; round < roundCount; round++)
            {
                PartFData partFData = new() { PartFCategories = categories };

                List<(Space Space, PartFCategory? Expected)> lookups = [];
                foreach (KeyValuePair<string, string?> keyValuePair in expectedNames)
                {
                    Space space = new(keyValuePair.Key);
                    Assert.Equal(SpaceUse.Undefined, partFData.SpaceSemanticsResolver.Resolve(space).SpaceUse);

                    lookups.Add((space, keyValuePair.Value is null ? null : categories[keyValuePair.Value]));
                }

                RunConcurrently(round, mismatches =>
                {
                    foreach ((Space space, PartFCategory? expected) in lookups)
                    {
                        PartFCategory? partFCategory = partFData.GetPartFCategory(space, out SpaceSemantics _);
                        if (!ReferenceEquals(partFCategory, expected))
                        {
                            mismatches.Enqueue($"'{space.Name}': expected '{expected?.Name ?? "null"}', got '{partFCategory?.Name ?? "null"}'");
                        }
                    }
                });

                //The published map still answers correctly once the race is over.
                foreach ((Space space, PartFCategory? expected) in lookups)
                {
                    Assert.Same(expected, partFData.GetPartFCategory(space, out SpaceSemantics _));
                }
            }
        }

        // ------------------------------------------------------------------
        // Shared SpaceSemanticsResolver cache, reached through GetPartFCategory(Space)
        // ------------------------------------------------------------------

        private const int spacesPerThread = 200;

        /// <summary>
        /// Every <c>GetPartFCategory(Space, ...)</c> call resolves the space through the one
        /// <see cref="SpaceSemanticsResolver"/> the <see cref="PartFData"/> holds, which caches each result by
        /// space Guid. Threads released together, each classifying its own new spaces plus a set they all
        /// share, must neither corrupt that cache nor read another space's entry from it: every space gets its
        /// own category and its own SpaceUse back. The fresh <see cref="PartFData"/> per round also races the
        /// space use map and the resolver's own lazy creation.
        /// </summary>
        [Fact]
        public void GetPartFCategory_Space_ConcurrentResolve_SharedResolverCacheStaysCorrect()
        {
            //Not Categories(): the resolver merges every category's synonyms into its TextMap, so the
            //20 000 duplicate bedrooms would make each Resolve match against 20 000 aliases. A small rule set
            //keeps every Resolve cheap, so the threads spend their time in the shared cache.
            Dictionary<string, PartFCategory> categories = [];
            foreach (PartFCategory partFCategory in Categories().Values)
            {
                if (partFCategory.Name is firstBedroomName or kitchenName or bathroomName)
                {
                    categories[partFCategory.Name] = partFCategory;
                }
            }

            (string Name, SpaceUse SpaceUse, PartFCategory Expected)[] kinds =
            [
                ("Bathroom", SpaceUse.Bathroom, categories[bathroomName]),
                ("Kitchen", SpaceUse.Kitchen, categories[kitchenName]),
                ("Bedroom", SpaceUse.Bedroom, categories[firstBedroomName]),
            ];

            for (int round = 0; round < roundCount; round++)
            {
                PartFData partFData = new() { PartFCategories = categories };

                List<Space> spaces_Shared = [];
                for (int k = 0; k < spacesPerThread; k++)
                {
                    spaces_Shared.Add(new Space($"{kinds[k % kinds.Length].Name} {k}"));
                }

                RunConcurrently(round, mismatches =>
                {
                    for (int k = 0; k < spacesPerThread; k++)
                    {
                        (string name, SpaceUse spaceUse, PartFCategory expected) = kinds[k % kinds.Length];

                        //The shared space and one only this thread sees, so the cache takes both repeated
                        //and new Guids from every thread at once.
                        foreach (Space space in new[] { spaces_Shared[k], new Space($"{name} {k}") })
                        {
                            PartFCategory? partFCategory = partFData.GetPartFCategory(space, out SpaceSemantics spaceSemantics);
                            if (!ReferenceEquals(partFCategory, expected) || spaceSemantics?.SpaceUse != spaceUse)
                            {
                                mismatches.Enqueue($"'{space.Name}': expected '{expected.Name}' / {spaceUse}, got '{partFCategory?.Name ?? "null"}' / {spaceSemantics?.SpaceUse.ToString() ?? "null"}");
                            }
                        }
                    }
                });
            }
        }

        /// <summary>
        /// Runs <paramref name="lookups"/> on max(4, processor count) dedicated threads released together by a
        /// barrier, so they all reach a lazy build at the same moment instead of the first one finishing it
        /// before the others start. Fails on any exception or any mismatch the lookups report.
        /// </summary>
        private static void RunConcurrently(int round, Action<ConcurrentQueue<string>> lookups)
        {
            int threadCount = System.Math.Max(4, Environment.ProcessorCount);

            ConcurrentQueue<Exception> exceptions = new();
            ConcurrentQueue<string> mismatches = new();

            using Barrier barrier = new(threadCount);

            List<Thread> threads = [];
            for (int i = 0; i < threadCount; i++)
            {
                Thread thread = new(() =>
                {
                    try
                    {
                        barrier.SignalAndWait();
                        lookups(mismatches);
                    }
                    catch (Exception exception)
                    {
                        exceptions.Enqueue(exception);
                    }
                })
                {
                    IsBackground = true,
                };

                threads.Add(thread);
                thread.Start();
            }

            foreach (Thread thread in threads)
            {
                Assert.True(thread.Join(TimeSpan.FromMinutes(1)), $"Round {round}: a lookup thread did not finish.");
            }

            Assert.True(exceptions.IsEmpty, $"Round {round}: {string.Join(Environment.NewLine, exceptions)}");
            Assert.True(mismatches.IsEmpty, $"Round {round}: {string.Join("; ", mismatches)}");
        }
    }
}
