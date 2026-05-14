// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical;
using SAM.Core;
using SAM.Tests.Helpers;
using Xunit;

namespace SAM.Tests
{
    public class LibraryFixtureTests
    {
        [Fact]
        public void MaterialLibrary_RoundTrip()
        {
            string json = Fixtures.ReadAllText("SAM_MaterialLibrary.JSON");
            MaterialLibrary library = RoundTrip.FromJson<MaterialLibrary>(json);

            Assert.NotNull(library);
            Assert.False(string.IsNullOrEmpty(library.Name));
        }

        [Fact]
        public void GasMaterialLibrary_RoundTrip()
        {
            string json = Fixtures.ReadAllText("SAM_GasMaterialLibrary.JSON");
            MaterialLibrary library = RoundTrip.FromJson<MaterialLibrary>(json);

            Assert.NotNull(library);
        }

        [Fact]
        public void ConstructionLibrary_RoundTrip()
        {
            string json = Fixtures.ReadAllText("SAM_ConstructionLibrary.JSON");
            ConstructionLibrary library = RoundTrip.FromJson<ConstructionLibrary>(json);

            Assert.NotNull(library);
        }

        [Fact]
        public void ApertureConstructionLibrary_RoundTrip()
        {
            string json = Fixtures.ReadAllText("SAM_ApertureConstructionLibrary.JSON");
            ApertureConstructionLibrary library = RoundTrip.FromJson<ApertureConstructionLibrary>(json);

            Assert.NotNull(library);
        }

        [Fact]
        public void ProfileLibrary_RoundTrip()
        {
            string json = Fixtures.ReadAllText("SAM_ProfileLibrary.JSON");
            ProfileLibrary library = RoundTrip.FromJson<ProfileLibrary>(json);

            Assert.NotNull(library);
        }

        [Fact]
        public void ProfileLibrary_TM59_RoundTrip()
        {
            string json = Fixtures.ReadAllText("SAM_ProfileLibrary_TM59.JSON");
            ProfileLibrary library = RoundTrip.FromJson<ProfileLibrary>(json);

            Assert.NotNull(library);
        }

        [Fact]
        public void InternalConditionLibrary_RoundTrip()
        {
            string json = Fixtures.ReadAllText("SAM_InternalConditionLibrary.JSON");
            InternalConditionLibrary library = RoundTrip.FromJson<InternalConditionLibrary>(json);

            Assert.NotNull(library);
        }

        [Fact]
        public void InternalConditionLibrary_TM59_RoundTrip()
        {
            string json = Fixtures.ReadAllText("SAM_InternalConditionLibrary_TM59.JSON");
            InternalConditionLibrary library = RoundTrip.FromJson<InternalConditionLibrary>(json);

            Assert.NotNull(library);
        }

        [Fact]
        public void DegreeOfActivityLibrary_RoundTrip()
        {
            string json = Fixtures.ReadAllText("SAM_DegreeOfActivityLibrary.JSON");
            DegreeOfActivityLibrary library = RoundTrip.FromJson<DegreeOfActivityLibrary>(json);

            Assert.NotNull(library);
        }

        [Fact]
        public void OpeningTypeLibrary_RoundTrip()
        {
            string json = Fixtures.ReadAllText("SAM_OpeningTypeLibrary.JSON");
            OpeningTypeLibrary library = RoundTrip.FromJson<OpeningTypeLibrary>(json);

            Assert.NotNull(library);
        }

        [Fact]
        public void SystemTypeLibrary_RoundTrip()
        {
            string json = Fixtures.ReadAllText("SAM_SystemTypeLibrary.JSON");
            SystemTypeLibrary library = RoundTrip.FromJson<SystemTypeLibrary>(json);

            Assert.NotNull(library);
        }

        [Fact]
        public void HostPartitionTypeLibrary_RoundTrip()
        {
            string json = Fixtures.ReadAllText("SAM_HostPartitionTypeLibrary.JSON");
            HostPartitionTypeLibrary library = RoundTrip.FromJson<HostPartitionTypeLibrary>(json);

            Assert.NotNull(library);
        }

        [Fact]
        public void NCMNameCollection_RoundTrip()
        {
            string json = Fixtures.ReadAllText("SAM_NCMNameCollection.JSON");
            NCMNameCollection collection = RoundTrip.FromJson<NCMNameCollection>(json);

            Assert.NotNull(collection);
        }

        [Fact]
        public void MergeSettings_RoundTrip()
        {
            string json = Fixtures.ReadAllText("SAM_MergeSettings.JSON");
            MergeSettings settings = RoundTrip.FromJson<MergeSettings>(json);

            Assert.NotNull(settings);
        }
    }
}
