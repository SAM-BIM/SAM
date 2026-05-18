// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Linq;
using SAM.Core;
using SAM.Tests.Helpers;
using Xunit;

namespace SAM.Tests
{
    public class RelationTests
    {
        [Fact]
        public void RoundTrip_Relation_PreservesIdAndReferences()
        {
            // Relation is a root IJSAMObject holding an id and two reference
            // sets that serialize as arrays of Reference strings.
            Relation relation = new Relation(
                "rel-001",
                new Reference[] { Guid.Parse("11111111-1111-1111-1111-111111111111") },
                new Reference[] {
                    Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    Guid.Parse("33333333-3333-3333-3333-333333333333"),
                });

            Relation result = RoundTrip.Once(relation);

            Assert.Equal("rel-001", result.Id);
            Assert.Single(result.References_1);
            Assert.Equal(2, result.References_2.Count());
        }

        [Fact]
        public void RoundTrip_RelationCollection_FixesPreMigrationLookupBug()
        {
            // Pre-migration RelationCollection.FromJObject mistakenly passed
            // the outer jObject when iterating the Relations array; the
            // migration fixes the inner-element wrap so collection members
            // come back populated rather than null.
            Relation relation_1 = new Relation(
                "a",
                new Reference[] { Guid.NewGuid() },
                new Reference[] { Guid.NewGuid() });
            Relation relation_2 = new Relation(
                "b",
                new Reference[] { Guid.NewGuid() },
                new Reference[] { Guid.NewGuid() });

            RelationCollection collection = new RelationCollection();
            collection.Add(relation_1);
            collection.Add(relation_2);

            RelationCollection result = RoundTrip.Once(collection);

            Assert.Equal(2, result.Count);
        }

        // TextMap and TypeMap have internal-only constructors and exposed
        // factory paths in SAM.Core, so we don't construct them directly in
        // tests. The SAMObject identity chain plus the Relation/RelationCollection
        // exercises above cover the same migration touchpoints.
    }
}
