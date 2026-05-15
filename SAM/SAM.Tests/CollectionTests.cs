// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors
// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Collections.Generic;
using System.Linq;
using SAM.Core;
using SAM.Tests.Helpers;
using Xunit;

namespace SAM.Tests
{
    public class CollectionTests
    {
        private static readonly Guid Fixed = Guid.Parse("aaaaaaaa-1111-2222-3333-444444444444");

        [Fact]
        public void RoundTrip_GuidCollection_PreservesOrderedGuids()
        {
            // GuidCollection holds a List<Guid> serialized as an array of Guid
            // strings. The migration moved emission to JsonArray.Add(string).
            Guid g1 = Guid.Parse("11111111-1111-1111-1111-111111111111");
            Guid g2 = Guid.Parse("22222222-2222-2222-2222-222222222222");
            Guid g3 = Guid.Parse("33333333-3333-3333-3333-333333333333");

            GuidCollection collection = new GuidCollection(Fixed, "Refs");
            collection.Add(g1);
            collection.Add(g2);
            collection.Add(g3);

            GuidCollection result = RoundTrip.Once(collection);

            Assert.Equal("Refs", result.Name);
            Assert.Equal(Fixed, result.Guid);
            List<Guid> roundTripped = result.ToList();
            Assert.Equal(new[] { g1, g2, g3 }, roundTripped);
        }

        [Fact]
        public void RoundTrip_SAMCollection_PreservesIdentityAndChildren()
        {
            // SAMCollection<T> now uses the protected JsonObject hook with
            // bridges for ParameterSets and the Collection array. Verify a
            // collection of Tag objects round-trips its identity and contents.
            SAMCollection<Tag> collection = new SAMCollection<Tag>();
            collection.Add(new Tag("alpha"));
            collection.Add(new Tag(42.5));
            collection.Add(new Tag(true));

            SAMCollection<Tag> result = RoundTrip.Once(collection);

            Assert.Equal(3, result.Count);
            Assert.Equal("alpha", result[0].GetValue<string>());
            Assert.Equal(42.5, result[1].GetValue<double>());
            Assert.True(result[2].GetValue<bool>());
        }

        [Fact]
        public void RoundTrip_IndexedObjects_PrimitiveValues()
        {
            // IndexedObjects<int> stores [index, value] pairs as JsonArray
            // entries. The migration keeps primitive JsonArray emission on the
            // non-shim JsonNode writer.
            IndexedObjects<int> indexed = new IndexedObjects<int>();
            indexed.Add(0, 100);
            indexed.Add(5, 500);
            indexed.Add(12, 1200);

            IndexedObjects<int> result = RoundTrip.Once(indexed);

            Assert.Equal(3, result.Count);
            Assert.Equal(100, result[0]);
            Assert.Equal(500, result[5]);
            Assert.Equal(1200, result[12]);
        }
    }
}
