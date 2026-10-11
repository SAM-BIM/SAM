// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical;
using SAM.Analytical.Enums;
using SAM.Core;
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using Xunit;

namespace SAM.Tests
{
    /// <summary>
    /// <c>Query.PartODesignKey</c> - the stable semantic key that answers "has the engineering meaning of this design changed since a
    /// Part O result was produced from it?". The byte-level <c>SimulationResultProvenance.Fingerprint</c> moves when a Part O
    /// equipment selection is rebuilt with fresh guids; this key must not, and must still move for every real change.
    /// </summary>
    public class PartODesignKeyTests : IDisposable
    {
        private readonly string directory = Path.Combine(Path.GetTempPath(), "SAM.PartODesignKey." + Guid.NewGuid().ToString("N"));

        public PartODesignKeyTests()
        {
            Directory.CreateDirectory(directory);
        }

        public void Dispose()
        {
            try
            {
                Directory.Delete(directory, true);
            }
            catch
            {
            }
        }

        // ---- fixtures ----------------------------------------------------------------------------------------

        private static readonly Guid Guid_Zone = new("22222222-2222-2222-2222-222222222222");

        /// <summary>A design with a fixed space identity, as a saved design has: the identities of the baseline are stable, only the Part O inputs are rebuilt.</summary>
        private static AnalyticalModel Design(string name = "Design", string space = "Flat 1")
        {
            AdjacencyCluster adjacencyCluster = new();
            adjacencyCluster.AddObject(new Space(space));

            return new AnalyticalModel(name, null, null, null, adjacencyCluster, null, null);
        }

        private static VentilationUnitReference Product(string model = "X1") => new("Acme", model, "R1");

        /// <summary>Always a new object with new guids - exactly what the UI does every time it reads the selection back.</summary>
        private static PartOEquipmentSelection Selection(PartOEquipmentSelectionMode mode = PartOEquipmentSelectionMode.AutomaticSelectedPool, params string[] models)
        {
            return new PartOEquipmentSelection(mode, models.Select(x => Product(x)));
        }

        private static AnalyticalModel WithSelection(PartOEquipmentSelection partOEquipmentSelection, AnalyticalModel analyticalModel = null)
        {
            analyticalModel ??= Design();
            analyticalModel.SetValue(AnalyticalModelParameter.PartOEquipmentSelection, partOEquipmentSelection);

            return analyticalModel;
        }

        private static AnalyticalModel Reopened(AnalyticalModel analyticalModel) => new(analyticalModel.ToJsonObject());

        private static string Key(AnalyticalModel analyticalModel) => analyticalModel.PartODesignKey();

        // ---- same meaning, same key --------------------------------------------------------------------------

        /// <summary>The defect this exists for: identical selection content built twice (new guids on the selection and on every pooled reference) is the same design.</summary>
        [Fact]
        public void RegeneratedGuids_AloneDoNotMoveTheKey_ButDoMoveTheByteFingerprint()
        {
            AnalyticalModel design = Design();
            PartOEquipmentSelection partOEquipmentSelection_1 = Selection(PartOEquipmentSelectionMode.AutomaticSelectedPool, "X1", "X2");
            PartOEquipmentSelection partOEquipmentSelection_2 = Selection(PartOEquipmentSelectionMode.AutomaticSelectedPool, "X1", "X2");

            //The premise: the two are the same selection, with different guids everywhere.
            Assert.True(partOEquipmentSelection_1.Matches(partOEquipmentSelection_2));
            Assert.NotEqual(partOEquipmentSelection_1.Guid, partOEquipmentSelection_2.Guid);
            Assert.NotEqual(partOEquipmentSelection_1.AllowedVentilationUnitReferences[0].Guid, partOEquipmentSelection_2.AllowedVentilationUnitReferences[0].Guid);

            AnalyticalModel analyticalModel_1 = WithSelection(partOEquipmentSelection_1, new AnalyticalModel(design));
            AnalyticalModel analyticalModel_2 = WithSelection(partOEquipmentSelection_2, new AnalyticalModel(design));

            Assert.NotEqual(SimulationResultProvenance.Fingerprint(analyticalModel_1), SimulationResultProvenance.Fingerprint(analyticalModel_2));
            Assert.Equal(Key(analyticalModel_1), Key(analyticalModel_2));
        }

        /// <summary>The project test product is rebuilt with a new guid (and a new reference guid) on every read as well.</summary>
        [Fact]
        public void ARebuiltProjectTestProduct_IsTheSameDesign()
        {
            AnalyticalModel design = Design();
            AnalyticalModel analyticalModel_1 = new(design);
            analyticalModel_1.SetValue(AnalyticalModelParameter.PartOProjectTestVentilationUnit, new PartOProjectTestVentilationUnit("Test 200", 200, 180));
            AnalyticalModel analyticalModel_2 = new(design);
            analyticalModel_2.SetValue(AnalyticalModelParameter.PartOProjectTestVentilationUnit, new PartOProjectTestVentilationUnit("Test 200", 200, 180));

            Assert.NotEqual(SimulationResultProvenance.Fingerprint(analyticalModel_1), SimulationResultProvenance.Fingerprint(analyticalModel_2));
            Assert.Equal(Key(analyticalModel_1), Key(analyticalModel_2));
        }

        /// <summary>Absent means the historic default, so a design that has never stated a selection is the design that states the default.</summary>
        [Fact]
        public void AnAbsentSelection_IsTheExplicitDefault()
        {
            AnalyticalModel design = Design();
            string key = Key(design);

            Assert.Equal(key, Key(WithSelection(new PartOEquipmentSelection(), new AnalyticalModel(design))));
            Assert.Equal(key, Key(WithSelection(new PartOEquipmentSelection(PartOEquipmentSelectionMode.AutomaticAllProducts), new AnalyticalModel(design))));

            //Absent and an unusable test product both offer nothing.
            AnalyticalModel analyticalModel = new(design);
            analyticalModel.SetValue(AnalyticalModelParameter.PartOProjectTestVentilationUnit, new PartOProjectTestVentilationUnit("Unfinished", double.NaN, double.NaN));
            Assert.Equal(key, Key(analyticalModel));
        }

        /// <summary>A pool listed the other way round, or with a product ticked twice, selects identically.</summary>
        [Fact]
        public void PoolOrderAndDuplicates_CarryNoMeaning()
        {
            AnalyticalModel design = Design();
            string key = Key(WithSelection(Selection(PartOEquipmentSelectionMode.AutomaticSelectedPool, "X1", "X2"), new AnalyticalModel(design)));

            Assert.Equal(key, Key(WithSelection(Selection(PartOEquipmentSelectionMode.AutomaticSelectedPool, "X2", "X1"), new AnalyticalModel(design))));
            Assert.Equal(key, Key(WithSelection(Selection(PartOEquipmentSelectionMode.AutomaticSelectedPool, "X2", "X1", "X1"), new AnalyticalModel(design))));
        }

        /// <summary>Names, labels and view state are not engineering meaning.</summary>
        [Fact]
        public void PresentationAndFilingState_DoNotMoveTheKey()
        {
            AnalyticalModel analyticalModel = WithSelection(Selection(PartOEquipmentSelectionMode.AutomaticSelectedPool, "X1"));
            string key = Key(analyticalModel);

            AnalyticalModel analyticalModel_Renamed = new(analyticalModel) { Name = "Renamed" };
            Assert.Equal(key, Key(analyticalModel_Renamed));

            AnalyticalModel analyticalModel_Labelled = new(analyticalModel);
            analyticalModel_Labelled.SetValue("CaseDescription", "a study");
            analyticalModel_Labelled.SetValue("UI Geometry Settings", "views");
            Assert.Equal(key, Key(analyticalModel_Labelled));
        }

        /// <summary>The guid of a parameter set is derived from the build that created it, so a model rebuilt by another build is not a different design.</summary>
        [Fact]
        public void AParameterSetsOwnGuid_IsNotMeaning()
        {
            AnalyticalModel analyticalModel = Design();
            analyticalModel.SetValue(AnalyticalModelParameter.NorthAngle, 1.5);

            JsonObject jsonObject = analyticalModel.ToJsonObject();
            int count = 0;
            void Regenerate(JsonNode jsonNode)
            {
                if (jsonNode is JsonObject jsonObject_Node)
                {
                    if ((jsonObject_Node["_type"]?.ToString() ?? string.Empty).StartsWith("SAM.Core.ParameterSet", StringComparison.Ordinal) && jsonObject_Node.ContainsKey("Guid"))
                    {
                        jsonObject_Node["Guid"] = Guid.NewGuid().ToString();
                        count++;
                    }

                    foreach (JsonNode child in jsonObject_Node.Select(x => x.Value).ToList())
                    {
                        Regenerate(child);
                    }
                }
                else if (jsonNode is JsonArray jsonArray)
                {
                    foreach (JsonNode child in jsonArray.ToList())
                    {
                        Regenerate(child);
                    }
                }
            }

            Regenerate(jsonObject);
            Assert.True(count > 0);

            AnalyticalModel analyticalModel_Regenerated = new(jsonObject);
            Assert.NotEqual(SimulationResultProvenance.Fingerprint(analyticalModel), SimulationResultProvenance.Fingerprint(analyticalModel_Regenerated));
            Assert.Equal(Key(analyticalModel), Key(analyticalModel_Regenerated));
        }

        // ---- real change, different key ----------------------------------------------------------------------

        [Fact]
        public void AMeaningfulEquipmentChange_MovesTheKey()
        {
            AnalyticalModel design = Design();
            AnalyticalModel With(PartOEquipmentSelection partOEquipmentSelection) => WithSelection(partOEquipmentSelection, new AnalyticalModel(design));

            string key = Key(With(Selection(PartOEquipmentSelectionMode.AutomaticSelectedPool, "X1")));

            //Mode.
            Assert.NotEqual(key, Key(With(Selection(PartOEquipmentSelectionMode.ManualPerDwelling, "X1"))));
            Assert.NotEqual(key, Key(With(Selection(PartOEquipmentSelectionMode.AutomaticAllProducts, "X1"))));

            //Pool: a different product, an added one, a removed one.
            Assert.NotEqual(key, Key(With(Selection(PartOEquipmentSelectionMode.AutomaticSelectedPool, "X2"))));
            Assert.NotEqual(key, Key(With(Selection(PartOEquipmentSelectionMode.AutomaticSelectedPool, "X1", "X2"))));
            Assert.NotEqual(key, Key(With(Selection(PartOEquipmentSelectionMode.AutomaticSelectedPool))));

            //A product's identity includes its manufacturer and reference.
            Assert.NotEqual(key, Key(With(new PartOEquipmentSelection(PartOEquipmentSelectionMode.AutomaticSelectedPool, [new VentilationUnitReference("Acme", "X1", "R2")]))));
            Assert.NotEqual(key, Key(With(new PartOEquipmentSelection(PartOEquipmentSelectionMode.AutomaticSelectedPool, [new VentilationUnitReference("Other", "X1", "R1")]))));
        }

        [Fact]
        public void AMeaningfulProjectTestProductChange_MovesTheKey()
        {
            AnalyticalModel design = Design();
            AnalyticalModel With(PartOProjectTestVentilationUnit partOProjectTestVentilationUnit)
            {
                AnalyticalModel analyticalModel = new(design);
                analyticalModel.SetValue(AnalyticalModelParameter.PartOProjectTestVentilationUnit, partOProjectTestVentilationUnit);

                return analyticalModel;
            }

            string key = Key(With(new PartOProjectTestVentilationUnit("Test 200", 200, 180)));

            Assert.NotEqual(Key(design), key);
            Assert.NotEqual(key, Key(With(new PartOProjectTestVentilationUnit("Test 200", 250, 180))));
            Assert.NotEqual(key, Key(With(new PartOProjectTestVentilationUnit("Test 200", 200, 190))));
            Assert.NotEqual(key, Key(With(new PartOProjectTestVentilationUnit("Test 201", 200, 180))));
        }

        [Fact]
        public void AHandPickedProduct_AndADwellingStrategy_MoveTheKey()
        {
            AnalyticalModel design = Design();
            string key = Key(design);

            AnalyticalModel Manual(string model)
            {
                PartOManualEquipmentSelection partOManualEquipmentSelection = new();
                Assert.True(partOManualEquipmentSelection.Set(Guid_Zone, Product(model)));
                AnalyticalModel analyticalModel = new(design);
                analyticalModel.SetValue(AnalyticalModelParameter.PartOManualEquipmentSelection, partOManualEquipmentSelection);

                return analyticalModel;
            }

            string key_Manual = Key(Manual("X1"));
            Assert.NotEqual(key, key_Manual);

            //The same hand-picked product, rebuilt with new guids, is the same design; another product is not.
            Assert.Equal(key_Manual, Key(Manual("X1")));
            Assert.NotEqual(key_Manual, Key(Manual("X2")));

            AnalyticalModel Strategy(PartOVentilationMode partOVentilationMode)
            {
                AnalyticalModel analyticalModel = new(design);
                analyticalModel.SetValue(AnalyticalModelParameter.PartODwellingStrategies, new PartODwellingStrategySet([new PartODwellingStrategy(Guid_Zone, partOVentilationMode)]));

                return analyticalModel;
            }

            string key_Strategy = Key(Strategy(PartOVentilationMode.NaturalVentilation));
            Assert.NotEqual(key, key_Strategy);
            Assert.Equal(key_Strategy, Key(Strategy(PartOVentilationMode.NaturalVentilation)));
            Assert.NotEqual(key_Strategy, Key(Strategy(PartOVentilationMode.MVHR)));
        }

        [Fact]
        public void ARelevantModelChange_MovesTheKey()
        {
            AnalyticalModel design = Design();
            AnalyticalModel analyticalModel = WithSelection(Selection(PartOEquipmentSelectionMode.AutomaticSelectedPool, "X1"), new AnalyticalModel(design));
            string key = Key(analyticalModel);

            //The cluster.
            Assert.NotEqual(key, Key(WithSelection(Selection(PartOEquipmentSelectionMode.AutomaticSelectedPool, "X1"), Design(space: "Flat 2"))));

            AdjacencyCluster adjacencyCluster = new(analyticalModel.AdjacencyCluster);
            adjacencyCluster.AddObject(new Space("Bedroom"));
            Assert.NotEqual(key, Key(new AnalyticalModel(analyticalModel, adjacencyCluster)));

            //Model-level parameters and the location.
            AnalyticalModel analyticalModel_North = new(analyticalModel);
            analyticalModel_North.SetValue(AnalyticalModelParameter.NorthAngle, 1.5);
            string key_North = Key(analyticalModel_North);
            Assert.NotEqual(key, key_North);

            AnalyticalModel analyticalModel_North_Changed = new(analyticalModel);
            analyticalModel_North_Changed.SetValue(AnalyticalModelParameter.NorthAngle, 2.5);
            Assert.NotEqual(key_North, Key(analyticalModel_North_Changed));

            Assert.NotEqual(key, Key(new AnalyticalModel("Design", null, new Location("London", -0.13, 51.5, 11), null, analyticalModel.AdjacencyCluster, null, null)));
        }

        // ---- deterministic, serializable, one hash -----------------------------------------------------------

        /// <summary>Save and reopen, in memory and through a real .sam file, is the same design.</summary>
        [Fact]
        public void SaveAndReopen_KeepsTheKey()
        {
            AnalyticalModel analyticalModel = WithSelection(Selection(PartOEquipmentSelectionMode.AutomaticSelectedPool, "X1", "X2"));
            analyticalModel.SetValue(AnalyticalModelParameter.PartOProjectTestVentilationUnit, new PartOProjectTestVentilationUnit("Test 200", 200, 180));
            analyticalModel.SetValue(AnalyticalModelParameter.NorthAngle, 1.5);
            string key = Key(analyticalModel);

            Assert.Equal(key, Key(Reopened(analyticalModel)));

            string path = Path.Combine(directory, "Design.sam");
            Assert.True(Core.Convert.ToFile(analyticalModel, path, SAMFileType.SAM));
            AnalyticalModel analyticalModel_File = Core.Convert.ToSAM<AnalyticalModel>(path).OfType<AnalyticalModel>().Single();

            Assert.Equal(key, Key(analyticalModel_File));

            //Reopened, then the selection rebuilt with new guids the way the UI does it: still the same design.
            PartOEquipmentSelection partOEquipmentSelection = analyticalModel_File.GetValue<PartOEquipmentSelection>(AnalyticalModelParameter.PartOEquipmentSelection);
            analyticalModel_File.SetValue(AnalyticalModelParameter.PartOEquipmentSelection, new PartOEquipmentSelection(partOEquipmentSelection.Mode, partOEquipmentSelection.AllowedVentilationUnitReferences.Select(x => new VentilationUnitReference(x.Manufacturer, x.Model, x.Reference))));

            Assert.Equal(key, Key(analyticalModel_File));
        }

        /// <summary>
        /// The key is one SHA-256 over one canonical stream: the schema tag, then the tagged sections. Recomputed here independently, so the
        /// definition is pinned and no other hash (the model fingerprint's FNV state, say) is mixed in.
        /// </summary>
        [Fact]
        public void TheKey_IsOneSha256_OverTheDocumentedCanonicalStream()
        {
            AnalyticalModel analyticalModel = Design();
            string key = Key(analyticalModel);

            Assert.Matches("^PartODesignKey:v1:[0-9a-f]{64}$", key);
            Assert.StartsWith(Analytical.Query.PartODesignKeySchema + ":", key);

            using MemoryStream memoryStream = new();
            void Bytes(params byte[] bytes) => memoryStream.Write(bytes, 0, bytes.Length);
            void Text(string text) => Bytes(Encoding.UTF8.GetBytes(text));

            Text("PartODesignKey:v1\n");
            Bytes(1);
            Text(analyticalModel.AdjacencyCluster.ToJsonObject().ToJsonString());
            Bytes(0xFF, 2, 0xFF, 3, 0xFF, 4, 0xFF);

            //No parameters; the Part O inputs read as the default and no test product.
            Bytes(5);
            Text("0\n");
            Bytes(0xFF, 6);
            Text("2\n");
            foreach (string line in new[] { "equipment|AutomaticAllProducts", "testunit|none" })
            {
                Text(Encoding.UTF8.GetByteCount(line) + ":" + line + "\n");
            }

            Bytes(0xFF);

            using SHA256 sHA256 = SHA256.Create();
            string expected = BitConverter.ToString(sHA256.ComputeHash(memoryStream.ToArray())).Replace("-", string.Empty).ToLowerInvariant();

            Assert.Equal("PartODesignKey:v1:" + expected, key);
            Assert.Null(Analytical.Query.PartODesignKey(null));
        }

        /// <summary>Taking the key changes nothing: not the model, and not the model's byte fingerprint.</summary>
        [Fact]
        public void TakingTheKey_DoesNotTouchTheModel_OrItsFingerprint()
        {
            AnalyticalModel analyticalModel = WithSelection(Selection(PartOEquipmentSelectionMode.AutomaticSelectedPool, "X1"));
            string json = analyticalModel.ToJsonObject().ToJsonString();
            string fingerprint = SimulationResultProvenance.Fingerprint(analyticalModel);

            _ = Key(analyticalModel);

            Assert.Equal(json, analyticalModel.ToJsonObject().ToJsonString());
            Assert.Equal(fingerprint, SimulationResultProvenance.Fingerprint(analyticalModel));
        }

        // ---- the reference and its resolution ----------------------------------------------------------------

        private string Save(AnalyticalModel analyticalModel, params string[] relative)
        {
            string path = Path.Combine([directory, .. relative]);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            Assert.True(Core.Convert.ToFile(analyticalModel, path, SAMFileType.SAM));

            return path;
        }

        private string Directory_Result => Path.Combine(directory, "PartO", "Iteration1a", "tas");

        [Fact]
        public void TheDesignReference_RecordsTheKey_AndItRoundTrips()
        {
            AnalyticalModel design = WithSelection(Selection(PartOEquipmentSelectionMode.AutomaticSelectedPool, "X1"));

            PartOBaselineReference partOBaselineReference = Analytical.Create.PartOBaselineReferenceFromDesign(PartODerivedCase.Iteration1a, design, null, Directory_Result);

            Assert.Equal(Key(design), partOBaselineReference.Design.DesignKey);
            Assert.Equal(SimulationResultProvenance.Fingerprint(design), partOBaselineReference.Design.Fingerprint);

            PartOBaselineReference read = new(partOBaselineReference.ToJsonObject());
            Assert.True(read.IsValid);
            Assert.Equal(partOBaselineReference.Design.DesignKey, read.Design.DesignKey);
            Assert.Equal(partOBaselineReference.ToJsonObject().ToJsonString(), read.ToJsonObject().ToJsonString());

            //A reference saved before the key existed has none, reads as valid, and writes none back.
            JsonObject jsonObject = partOBaselineReference.ToJsonObject();
            ((JsonObject)jsonObject["Design"]).Remove("DesignKey");
            PartOBaselineReference legacy = new(jsonObject);
            Assert.True(legacy.IsValid);
            Assert.Null(legacy.Design.DesignKey);
            Assert.DoesNotContain("DesignKey", legacy.ToJsonObject().ToJsonString());

            //A result that derives from a result inherits the design's key with the rest of the design reference.
            AnalyticalModel source = new(design);
            Assert.True(source.StampPartOBaselineReference(partOBaselineReference));
            source.SetValue(AnalyticalModelParameter.SimulationResultProvenance, new SimulationResultProvenance(source, null));
            PartOBaselineReference partOBaselineReference_2B = Analytical.Create.PartOBaselineReferenceFromResult(PartODerivedCase.Iteration2B, source, null, Directory_Result);
            Assert.Equal(Key(design), partOBaselineReference_2B.Design.DesignKey);
        }

        /// <summary>
        /// The reason for all of it, end to end: the design file is saved, the equipment selection is later rebuilt with new guids and the design
        /// written again, and the result still finds its design unchanged. A real change is still reported.
        /// </summary>
        [Fact]
        public void Resolution_PrefersTheKey_AndIsNotFooledByRegeneratedGuids()
        {
            AnalyticalModel design = WithSelection(Selection(PartOEquipmentSelectionMode.AutomaticSelectedPool, "X1"));
            string path_Design = Save(design, "Design.sam");
            PartOBaselineReference partOBaselineReference = Analytical.Create.PartOBaselineReferenceFromDesign(PartODerivedCase.Iteration1a, design, path_Design, Directory_Result);
            string path_Result = Path.Combine(Directory_Result, "Design.sam");

            Assert.Equal(PartOBaselineResolutionStatus.Resolved, partOBaselineReference.Design.PartOModelResolution(path_Result).Status);

            //The next session rebuilds the same selection with new guids and the design is written again.
            AnalyticalModel design_Rewritten = WithSelection(Selection(PartOEquipmentSelectionMode.AutomaticSelectedPool, "X1"), new AnalyticalModel(design));
            Save(design_Rewritten, "Design.sam");

            Assert.NotEqual(SimulationResultProvenance.Fingerprint(design), SimulationResultProvenance.Fingerprint(design_Rewritten));
            Assert.Equal(PartOBaselineResolutionStatus.Resolved, partOBaselineReference.Design.PartOModelResolution(path_Result).Status);

            //A reference without a key falls back to the byte fingerprint, which is the old (and noisier) behaviour - nothing is inferred.
            PartOBaselineReference partOBaselineReference_Legacy = new(partOBaselineReference);
            partOBaselineReference_Legacy.Design.DesignKey = null;
            Assert.Equal(PartOBaselineResolutionStatus.Changed, partOBaselineReference_Legacy.Design.PartOModelResolution(path_Result).Status);

            //A real change is still a change, with or without the key.
            AnalyticalModel design_Edited = WithSelection(Selection(PartOEquipmentSelectionMode.AutomaticSelectedPool, "X2"), new AnalyticalModel(design));
            Save(design_Edited, "Design.sam");

            Assert.Equal(PartOBaselineResolutionStatus.Changed, partOBaselineReference.Design.PartOModelResolution(path_Result).Status);
            Assert.Equal(PartOBaselineResolutionStatus.Changed, partOBaselineReference_Legacy.Design.PartOModelResolution(path_Result).Status);
        }
    }
}
