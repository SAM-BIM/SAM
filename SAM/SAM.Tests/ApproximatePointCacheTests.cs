// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Math;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace SAM.Tests
{
    /// <summary>
    /// Edge cases of the kernel's result cache (documentation/GenOpt-3.1.1-Behaviour.md §2.3). It uses a 1e-12
    /// relative, non-transitive point equality inside a red-black tree.
    /// </summary>
    public class ApproximatePointCacheTests
    {
        [Fact]
        public void AreEqual_ZeroMatchesOnlySignedZero()
        {
            Assert.True(ApproximatePointCache<int>.AreEqual(0.0, -0.0));
            Assert.True(ApproximatePointCache<int>.AreEqual(-0.0, 0.0));
            Assert.False(ApproximatePointCache<int>.AreEqual(0.0, double.Epsilon));
            Assert.False(ApproximatePointCache<int>.AreEqual(0.0, -1e-300));
        }

        [Fact]
        public void AreEqual_UsesRelativeToleranceOfOneEMinusTwelve_MirroredForNegatives()
        {
            Assert.True(ApproximatePointCache<int>.AreEqual(1.0, 1.0 + 0.9e-12));
            Assert.False(ApproximatePointCache<int>.AreEqual(1.0, 1.0 + 1.1e-12));
            Assert.True(ApproximatePointCache<int>.AreEqual(-1.0, -1.0 - 0.9e-12));
            Assert.False(ApproximatePointCache<int>.AreEqual(-1.0, -1.0 - 1.1e-12));
            Assert.True(ApproximatePointCache<int>.AreEqual(1e-30, 1e-30 * (1 + 0.5e-12)));
            Assert.False(ApproximatePointCache<int>.AreEqual(1e-30, 1.01e-30));
        }

        [Fact]
        public void AreEqual_IsNotTransitive()
        {
            double a = 1.0;
            double b = 1.0 + 0.9e-12;
            double c = 1.0 + 1.8e-12;

            Assert.True(ApproximatePointCache<int>.AreEqual(a, b));
            Assert.True(ApproximatePointCache<int>.AreEqual(b, c));
            Assert.False(ApproximatePointCache<int>.AreEqual(a, c));
        }

        [Fact]
        public void Compare_IsCoordinateWiseAndDescending()
        {
            Assert.Equal(-1, ApproximatePointCache<int>.Compare(new[] { 2.0, 0.0 }, new[] { 1.0, 5.0 }));
            Assert.Equal(1, ApproximatePointCache<int>.Compare(new[] { 1.0, 0.0 }, new[] { 1.0, 5.0 }));
            Assert.Equal(0, ApproximatePointCache<int>.Compare(new[] { 1.0, -0.0 }, new[] { 1.0 + 0.5e-12, 0.0 }));
        }

        [Fact]
        public void Put_EqualKeyReplacesValueAndKeepsCount()
        {
            ApproximatePointCache<int> cache = new ApproximatePointCache<int>();
            cache.Put(new[] { 1.0 }, 1);
            cache.Put(new[] { 1.0 + 0.5e-12 }, 2);

            Assert.Equal(1, cache.Count);
            Assert.True(cache.TryGetValue(new[] { 1.0 }, out int value));
            Assert.Equal(2, value);
            Assert.False(cache.TryGetValue(new[] { 1.0 + 1e-9 }, out _));
        }

        [Fact]
        public void Put_CopiesTheKey()
        {
            ApproximatePointCache<int> cache = new ApproximatePointCache<int>();
            double[] key = { 3.0 };
            cache.Put(key, 7);
            key[0] = 4.0;

            Assert.True(cache.TryGetValue(new[] { 3.0 }, out int value));
            Assert.Equal(7, value);
            Assert.False(cache.TryGetValue(new[] { 4.0 }, out _));
        }

        /// <summary>
        /// A query that equals two stored points finds the one the tree descent meets first, not the first one
        /// inserted. A first-match (or hash) cache returns a different value here, as it did in ec-gs-collapse.
        /// </summary>
        [Fact]
        public void TryGetValue_DependsOnTreeShape_NotOnInsertionOrder()
        {
            double[] a = { 1.0 };
            double[] b = { 1.0 + 1.5e-12 };
            double[] d = { 2.0 };
            double[] query = { 1.0 + 0.75e-12 };
            Assert.NotEqual(0, ApproximatePointCache<int>.Compare(b, a));
            Assert.Equal(0, ApproximatePointCache<int>.Compare(query, a));
            Assert.Equal(0, ApproximatePointCache<int>.Compare(query, b));

            ApproximatePointCache<string> cache = new ApproximatePointCache<string>();
            List<KeyValuePair<double[], string>> insertionOrder = new List<KeyValuePair<double[], string>>
            {
                new KeyValuePair<double[], string>(a, "a"),
                new KeyValuePair<double[], string>(b, "b"),
                new KeyValuePair<double[], string>(d, "d"),   // forces a right rotation: b becomes the root
            };
            foreach (KeyValuePair<double[], string> pair in insertionOrder)
            {
                cache.Put(pair.Key, pair.Value);
            }

            string firstMatch = insertionOrder.First(p => ApproximatePointCache<string>.Compare(query, p.Key) == 0).Value;
            Assert.True(cache.TryGetValue(query, out string? found));
            Assert.Equal("a", firstMatch);
            Assert.Equal("b", found);
        }

        [Fact]
        public void Tree_FindsEveryKey_AfterSortedInsertion()
        {
            ApproximatePointCache<int> cache = new ApproximatePointCache<int>();
            for (int i = 0; i < 20000; i++)
            {
                cache.Put(new[] { (double)i, -i }, i);
            }

            Assert.Equal(20000, cache.Count);
            for (int i = 0; i < 20000; i += 7)
            {
                Assert.True(cache.TryGetValue(new[] { (double)i, -i }, out int value));
                Assert.Equal(i, value);
            }
        }
    }
}
