// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Text.Json.Nodes;
using SAM.Core;
using Xunit;

namespace SAM.Tests
{
    public class RelationClusterGuidObjectTests
    {
        // Regression guard for the LinkedFace3D / SolarRelationCluster bug:
        // RelationCluster previously only honored Guids on ISAMObject. An IJSAMObject
        // that carried its own Guid (LinkedFace3D) was inserted under Guid.NewGuid()
        // and was unretrievable by its original Guid. The fix introduces IGuidObject
        // (ISAMObject : IGuidObject) and widens the cluster's Guid-extraction.

        private sealed class GuidObjectStub : IGuidObject
        {
            public GuidObjectStub(Guid guid) { Guid = guid; }
            public Guid Guid { get; }
            public JsonObject? ToJsonObject() => null;
            public bool FromJsonObject(JsonObject? jsonObject) => false;
        }

        private sealed class SAMObjectStub : ISAMObject
        {
            public SAMObjectStub(Guid guid, string name) { Guid = guid; Name = name; }
            public Guid Guid { get; }
            public string Name { get; }
            public JsonObject? ToJsonObject() => null;
            public bool FromJsonObject(JsonObject? jsonObject) => false;
        }

        private sealed class JSAMObjectStub : IJSAMObject
        {
            public JsonObject? ToJsonObject() => null;
            public bool FromJsonObject(JsonObject? jsonObject) => false;
        }

        [Fact]
        public void TryAddObject_IGuidObject_PreservesGuid()
        {
            Guid guid = Guid.NewGuid();
            GuidObjectStub @object = new GuidObjectStub(guid);
            RelationCluster<IJSAMObject> cluster = new RelationCluster<IJSAMObject>();

            Assert.True(cluster.AddObject(@object));

            GuidObjectStub retrieved = cluster.GetObject<GuidObjectStub>(guid);
            Assert.Same(@object, retrieved);
            Assert.Equal(guid, cluster.GetGuid(@object));
            Assert.True(cluster.Contains(@object));
        }

        [Fact]
        public void TryAddObject_ISAMObject_PreservesGuid()
        {
            Guid guid = Guid.NewGuid();
            SAMObjectStub @object = new SAMObjectStub(guid, "n");
            RelationCluster<IJSAMObject> cluster = new RelationCluster<IJSAMObject>();

            Assert.True(cluster.AddObject(@object));

            SAMObjectStub retrieved = cluster.GetObject<SAMObjectStub>(guid);
            Assert.Same(@object, retrieved);
            Assert.Equal(guid, cluster.GetGuid(@object));
        }

        [Fact]
        public void TryAddObject_PlainIJSAMObject_GeneratesNewGuid()
        {
            JSAMObjectStub @object = new JSAMObjectStub();
            RelationCluster<IJSAMObject> cluster = new RelationCluster<IJSAMObject>();

            Assert.True(cluster.AddObject(@object));

            Guid assigned = cluster.GetGuid(@object);
            Assert.NotEqual(Guid.Empty, assigned);
            Assert.Same(@object, cluster.GetObject<JSAMObjectStub>(assigned));
        }

        [Fact]
        public void TryRemoveObject_IGuidObject_RemovesByStoredGuid()
        {
            Guid guid = Guid.NewGuid();
            GuidObjectStub @object = new GuidObjectStub(guid);
            RelationCluster<IJSAMObject> cluster = new RelationCluster<IJSAMObject>();
            cluster.AddObject(@object);

            Assert.True(cluster.TryRemoveObject(@object, out _, out Guid removed));
            Assert.Equal(guid, removed);
            Assert.False(cluster.Contains(@object));
            Assert.Null(cluster.GetObject<GuidObjectStub>(guid));
        }

        [Fact]
        public void ISAMObject_IsAlso_IGuidObject()
        {
            // Belt-and-braces: the rebase must preserve the interface hierarchy.
            SAMObjectStub @object = new SAMObjectStub(Guid.NewGuid(), "n");
            Assert.IsAssignableFrom<IGuidObject>(@object);
            Assert.IsAssignableFrom<IJSAMObject>(@object);
        }
    }
}
