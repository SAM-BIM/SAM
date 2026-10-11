// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical;
using SAM.Tests.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using Xunit;

namespace SAM.Tests
{
    /// <summary>
    /// Every ApertureConstruction in the default library must carry a stable identity.
    /// <para>
    /// The JSON reader replaces a Guid it cannot parse with <c>Guid.NewGuid()</c>, so a malformed value in the
    /// resource file silently gives that construction a new identity on every load. SAM_UI's Glazing window and
    /// the native Optimisation glazing choice identify systems by Guid, so that is a real defect, and the
    /// round-trip test in <see cref="LibraryFixtureTests"/> cannot see it (it compares the second and third
    /// serialisations, after the random value has already been assigned).
    /// </para>
    /// </summary>
    public class ApertureConstructionLibraryGuidTests
    {
        private const string FileName = "SAM_ApertureConstructionLibrary.JSON";

        [Fact]
        public void EveryApertureConstruction_HasGuidThatParses()
        {
            JsonArray objects = (JsonArray)JsonNode.Parse(Fixtures.ReadAllText(FileName))!["Objects"]!;
            Assert.NotEmpty(objects);

            foreach (JsonNode? jsonNode in objects)
            {
                string? name = jsonNode?["Name"]?.GetValue<string>();
                string? text = jsonNode?["Guid"]?.GetValue<string>();

                Assert.True(Guid.TryParseExact(text, "D", out Guid guid) && guid != Guid.Empty, $"ApertureConstruction '{name}' has an invalid Guid '{text}'.");
            }
        }

        [Fact]
        public void EveryApertureConstruction_GuidIsStableAcrossLoads()
        {
            JsonArray objects = (JsonArray)JsonNode.Parse(Fixtures.ReadAllText(FileName))!["Objects"]!;
            List<Guid> file = objects.Select(x => Guid.Parse(x!["Guid"]!.GetValue<string>())).OrderBy(x => x).ToList();

            List<Guid> first = Load();
            List<Guid> second = Load();

            Assert.Equal(file.Count, file.Distinct().Count());
            Assert.Equal(file, first);
            Assert.Equal(file, second);

            //The SIM_EXT_GLZ entry whose Guid was missing a digit; downstream glazing choices refer to this value.
            ApertureConstruction apertureConstruction = Core.Create.IJSAMObject<ApertureConstructionLibrary>(Fixtures.ReadAllText(FileName))
                .GetApertureConstructions()
                .Single(x => x.Guid == new Guid("04d00dd0-f646-4fbb-90e6-f8d9cd6634eb"));

            Assert.Equal("SIM_EXT_GLZ", apertureConstruction.Name);
            Assert.Equal(ApertureType.Window, apertureConstruction.ApertureType);
        }

        private static List<Guid> Load()
        {
            ApertureConstructionLibrary library = Core.Create.IJSAMObject<ApertureConstructionLibrary>(Fixtures.ReadAllText(FileName));
            Assert.NotNull(library);

            return library.GetApertureConstructions().ConvertAll(x => x.Guid).OrderBy(x => x).ToList();
        }
    }
}
