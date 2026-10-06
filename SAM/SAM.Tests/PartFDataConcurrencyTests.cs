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
    /// Regression tests for the lazily built space use map behind
    /// <see cref="PartFData.GetPartFCategory(SpaceUse)"/>.
    /// </summary>
    /// <remarks>
    /// One <see cref="PartFData"/> is shared by every caller of <c>ActiveSetting</c>, so the first lookups can
    /// arrive from several threads at once. The map used to be published empty and then filled in place: a
    /// second thread could read it half filled (a wet room classified as nothing) or while it was being
    /// written (<c>InvalidOperationException: Operations that change non-concurrent collections must have
    /// exclusive access</c>, seen once in a parallel run of the full suite).
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

            int threadCount = System.Math.Max(4, Environment.ProcessorCount);

            for (int round = 0; round < roundCount; round++)
            {
                PartFData partFData = new() { PartFCategories = categories };

                ConcurrentQueue<Exception> exceptions = new();
                ConcurrentQueue<string> mismatches = new();

                //The barrier releases every thread at the same moment, so they all reach the lazy build
                //together instead of the first one finishing it before the others start.
                using Barrier barrier = new(threadCount);

                List<Thread> threads = [];
                for (int i = 0; i < threadCount; i++)
                {
                    Thread thread = new(() =>
                    {
                        try
                        {
                            barrier.SignalAndWait();

                            foreach (SpaceUse spaceUse in queries)
                            {
                                PartFCategory? partFCategory = partFData.GetPartFCategory(spaceUse);
                                if (!ReferenceEquals(partFCategory, expected[spaceUse]))
                                {
                                    mismatches.Enqueue($"{spaceUse}: expected '{expected[spaceUse]?.Name ?? "null"}', got '{partFCategory?.Name ?? "null"}'");
                                }
                            }
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

                //The published map still answers correctly once the race is over.
                foreach (KeyValuePair<SpaceUse, PartFCategory?> keyValuePair in expected)
                {
                    Assert.Same(keyValuePair.Value, partFData.GetPartFCategory(keyValuePair.Key));
                }
            }
        }
    }
}
