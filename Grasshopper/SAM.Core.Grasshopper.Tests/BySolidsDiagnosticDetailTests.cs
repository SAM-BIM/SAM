// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using GH_IO.Serialization;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Types;
using NUnit.Framework;
using Rhino.Geometry;
using SAM.Analytical.Grasshopper;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SAM.Core.Grasshopper.Tests
{
    /// <summary>
    /// <b>The optional <c>diagnosticDetail_</c> input of <c>SAMAnalytical.CreatePanelsBySolids</c> and
    /// <c>SAMAnalytical.CreateAperturesBySolids</c>.</b>
    /// <para>
    /// Compatibility: GUIDs, the original parameter order, names and types are unchanged; the new input is a voluntary
    /// (hidden) <see cref="Param_Integer"/> appended last, so a component saved before it existed still loads with the
    /// parameters it had. Behaviour: the Info in <c>log</c> follows the level, Warnings never do, an invalid level is
    /// reported and falls back to Summary, and the engineering outputs do not depend on it.
    /// </para>
    /// <para>
    /// Needs the headless Rhino 8 + Grasshopper runtime (<see cref="GrasshopperTestSetup"/>); run locally, not in CI.
    /// </para>
    /// </summary>
    [TestFixture]
    public class BySolidsDiagnosticDetailTests
    {
        // Rhino types are used in the SetUp and TearDown bodies only (see RoundTrip): the fields are typed object.
        private object model;

        private object activeBefore;

        /// <summary>
        /// The components refuse a Rhino document that is not in metres, and the headless Rhino 8 of Rhino.Testing has no
        /// metre document active: without one every solve ends at the units Error and the log is empty.
        /// </summary>
        [SetUp]
        public void UseAMetreModel()
        {
            activeBefore = global::Rhino.RhinoDoc.ActiveDoc;
            global::Rhino.RhinoDoc doc = global::Rhino.RhinoDoc.CreateHeadless(null);
            doc.ModelUnitSystem = global::Rhino.UnitSystem.Meters;
            global::Rhino.RhinoDoc.ActiveDoc = doc;
            model = doc;
        }

        [TearDown]
        public void RestoreTheActiveModel()
        {
            global::Rhino.RhinoDoc.ActiveDoc = (global::Rhino.RhinoDoc)activeBefore;
            ((global::Rhino.RhinoDoc)model)?.Dispose();
            model = null;
        }

        private static readonly Guid PanelsGuid = new Guid("8b0f5f2c-6a8e-4f6d-9d1c-3e2a7b5c4d10");

        private static readonly Guid ApertureGuid = new Guid("b115fc0d-c40f-4a98-938b-9b829a28cb95");

        // Registration order before diagnosticDetail_ existed (PR #200 state), then the full declared order.
        private static readonly string[] PanelsDefaultInputs = { "_solids", "apertureGeometries_", "minThickness_", "snap_" };

        private static readonly string[] PanelsDeclaredInputs = { "_solids", "apertureGeometries_", "minThickness_", "snap_", "maxThicknessRatio_", "airForOpenings_", "diagnosticDetail_" };

        private static readonly string[] PanelsDefaultOutputs = { "panels", "log" };

        private static readonly string[] ApertureDefaultInputs = { "windows_", "doors_", "apertureConstructions_" };

        private static readonly string[] ApertureDeclaredInputs = { "windows_", "doors_", "apertureConstructions_", "maxGap_", "useRhinoGroups_", "diagnosticDetail_" };

        private static readonly string[] ApertureDefaultOutputs = { "apertures", "log" };

        private static string[] Names(IEnumerable<IGH_Param> params_) => params_.Select(x => x.Name).ToArray();

        /// <summary>Inserts the next voluntary input the way the canvas does, through the component's own contract.</summary>
        private static IGH_Param InsertNextInput(GH_SAMComponent component)
        {
            IGH_VariableParameterComponent variable = (IGH_VariableParameterComponent)component;
            int index = component.Params.Input.Count;
            Assert.True(variable.CanInsertParameter(GH_ParameterSide.Input, index));

            IGH_Param result = variable.CreateParameter(GH_ParameterSide.Input, index);
            Assert.NotNull(result);
            component.Params.RegisterInputParam(result, index);
            component.Params.OnParametersChanged();
            return result;
        }

        // No Grasshopper type in a generic constraint: the CLR loads constraints with the declaring class, and NUnit's
        // discovery enumerates this class before Rhino.Testing has hosted Grasshopper (see SAM.Core.Grasshopper.Tests.Fixtures).
        private static T RoundTrip<T>(T component) where T : class
        {
            GH_SAMComponent source = (GH_SAMComponent)(object)component;
            source.CreateAttributes();
            GH_LooseChunk chunk = new GH_LooseChunk("component");
            Assert.True(source.Write(chunk));

            GH_SAMComponent result = (GH_SAMComponent)Activator.CreateInstance(component.GetType());
            result.CreateAttributes();
            Assert.True(result.Read(chunk));
            return (T)(object)result;
        }

        // ---- Identity and registration ------------------------------------------------------------------

        [Test]
        public void ComponentGuids_AreUnchanged()
        {
            Assert.Equal(PanelsGuid, new SAMAnalyticalCreatePanelsBySolids().ComponentGuid);
            Assert.Equal(ApertureGuid, new SAMAnalyticalCreateAperturesBySolids().ComponentGuid);
        }

        [Test]
        public void FreshComponents_RegisterTheSameParametersAsBefore_TheNewInputIsHidden()
        {
            SAMAnalyticalCreatePanelsBySolids panels = new SAMAnalyticalCreatePanelsBySolids();
            SAMAnalyticalCreateAperturesBySolids apertures = new SAMAnalyticalCreateAperturesBySolids();

            Assert.Equal(PanelsDefaultInputs, Names(panels.Params.Input));
            Assert.Equal(PanelsDefaultOutputs, Names(panels.Params.Output));
            Assert.Equal(ApertureDefaultInputs, Names(apertures.Params.Input));
            Assert.Equal(ApertureDefaultOutputs, Names(apertures.Params.Output));
        }

        [Test]
        public void DiagnosticDetailInput_IsAppendedLast_AndTheOriginalOrderIsPreserved()
        {
            SAMAnalyticalCreatePanelsBySolids panels = new SAMAnalyticalCreatePanelsBySolids();
            while (((IGH_VariableParameterComponent)panels).CanInsertParameter(GH_ParameterSide.Input, panels.Params.Input.Count))
            {
                InsertNextInput(panels);
            }

            Assert.Equal(PanelsDeclaredInputs, Names(panels.Params.Input));

            SAMAnalyticalCreateAperturesBySolids apertures = new SAMAnalyticalCreateAperturesBySolids();
            while (((IGH_VariableParameterComponent)apertures).CanInsertParameter(GH_ParameterSide.Input, apertures.Params.Input.Count))
            {
                InsertNextInput(apertures);
            }

            Assert.Equal(ApertureDeclaredInputs, Names(apertures.Params.Input));
        }

        [Test]
        public void DiagnosticDetailInput_IsAnOptionalInteger_DefaultingToSummary()
        {
            SAMAnalyticalCreatePanelsBySolids panels = new SAMAnalyticalCreatePanelsBySolids();
            while (((IGH_VariableParameterComponent)panels).CanInsertParameter(GH_ParameterSide.Input, panels.Params.Input.Count))
            {
                InsertNextInput(panels);
            }

            Param_Integer param = (Param_Integer)panels.Params.Input.Last();
            Assert.Equal("diagnosticDetail_", param.Name);
            Assert.True(param.Optional);
            Assert.Equal(GH_ParamAccess.item, param.Access);
            Assert.Equal(0, param.PersistentData.AllData(true).OfType<GH_Integer>().Single().Value);
        }

        [Test]
        public void ComponentVersions_AreBumped_AndTheOutputsAreStillNamedLog()
        {
            // 1.1.0: pieces of fragmented elements are kept (Create.PanelsBySolids), a geometry change.
            Assert.Equal("1.1.0", new SAMAnalyticalCreatePanelsBySolids().LatestComponentVersion);
            Assert.Equal("1.0.2", new SAMAnalyticalCreateAperturesBySolids().LatestComponentVersion);
            Assert.Equal("log", new SAMAnalyticalCreatePanelsBySolids().Params.Output.Last().Name);
            Assert.Equal("log", new SAMAnalyticalCreateAperturesBySolids().Params.Output.Last().Name);
        }

        // ---- Saved definitions --------------------------------------------------------------------------

        [Test]
        public void ADefinitionSavedWithoutTheNewInput_LoadsWithTheParametersItHad()
        {
            // A component with only its default parameters writes exactly what an older release wrote.
            SAMAnalyticalCreatePanelsBySolids panels = RoundTrip(new SAMAnalyticalCreatePanelsBySolids());
            SAMAnalyticalCreateAperturesBySolids apertures = RoundTrip(new SAMAnalyticalCreateAperturesBySolids());

            Assert.Equal(PanelsDefaultInputs, Names(panels.Params.Input));
            Assert.Equal(PanelsDefaultOutputs, Names(panels.Params.Output));
            Assert.Equal(-1, panels.Params.IndexOfInputParam("diagnosticDetail_"));
            Assert.Equal(ApertureDefaultInputs, Names(apertures.Params.Input));
            Assert.Equal(-1, apertures.Params.IndexOfInputParam("diagnosticDetail_"));
        }

        [Test]
        public void ADefinitionSavedWithAllVoluntaryInputsOfTheOldRelease_LoadsWithThem_AndWithoutTheNewOne()
        {
            SAMAnalyticalCreatePanelsBySolids old = new SAMAnalyticalCreatePanelsBySolids();
            InsertNextInput(old);
            InsertNextInput(old);

            SAMAnalyticalCreatePanelsBySolids loaded = RoundTrip(old);

            Assert.Equal(PanelsDeclaredInputs.Take(6).ToArray(), Names(loaded.Params.Input));
            Assert.Equal(-1, loaded.Params.IndexOfInputParam("diagnosticDetail_"));
        }

        [Test]
        public void ADefinitionSavedWithTheNewInput_KeepsItAndItsValue()
        {
            SAMAnalyticalCreatePanelsBySolids panels = new SAMAnalyticalCreatePanelsBySolids();
            while (((IGH_VariableParameterComponent)panels).CanInsertParameter(GH_ParameterSide.Input, panels.Params.Input.Count))
            {
                InsertNextInput(panels);
            }

            Param_Integer param = (Param_Integer)panels.Params.Input.Last();
            param.PersistentData.Clear();
            param.SetPersistentData(2);

            SAMAnalyticalCreatePanelsBySolids loaded = RoundTrip(panels);

            Assert.Equal(PanelsDeclaredInputs, Names(loaded.Params.Input));
            Assert.Equal(2, ((Param_Integer)loaded.Params.Input.Last()).PersistentData.AllData(true).OfType<GH_Integer>().Single().Value);
        }

        // ---- Behaviour ----------------------------------------------------------------------------------

        private static GH_Brep Wall()
        {
            return new GH_Brep(Brep.CreateFromBox(new BoundingBox(0, 0, 0, 4, 0.25, 3)));
        }

        private static GH_Brep Tread()
        {
            // Not panel-like at the default ratio: a Warning at every detail level.
            return new GH_Brep(Brep.CreateFromBox(new BoundingBox(10, 0, 0, 11, 0.3, 0.25)));
        }

        private static void SetSolids(SAMAnalyticalCreatePanelsBySolids component, params GH_Brep[] solids)
        {
            Param_Brep param = (Param_Brep)component.Params.Input[component.Params.IndexOfInputParam("_solids")];
            param.PersistentData.Clear();
            foreach (GH_Brep solid in solids)
            {
                param.PersistentData.Append(solid);
            }
        }

        private static void SetDetail(SAMAnalyticalCreatePanelsBySolids component, int? value)
        {
            if (!value.HasValue)
            {
                return;
            }

            while (component.Params.IndexOfInputParam("diagnosticDetail_") == -1)
            {
                InsertNextInput(component);
            }

            Param_Integer param = (Param_Integer)component.Params.Input[component.Params.IndexOfInputParam("diagnosticDetail_")];
            param.PersistentData.Clear();
            param.SetPersistentData(value.Value);
        }

        private static (List<string> Log, int PanelCount, List<string> Warnings) Solve(int? detail, params GH_Brep[] solids)
        {
            SAMAnalyticalCreatePanelsBySolids component = new SAMAnalyticalCreatePanelsBySolids();
            component.CreateAttributes();
            SetSolids(component, solids);
            SetDetail(component, detail);

            component.ClearData();
            component.CollectData();
            component.ComputeData();

            List<string> log = component.Params.Output[component.Params.IndexOfOutputParam("log")].VolatileData.AllData(true).OfType<GH_String>().Select(x => x.Value).ToList();
            int panelCount = component.Params.Output[component.Params.IndexOfOutputParam("panels")].VolatileData.AllData(true).Count();
            List<string> warnings = component.RuntimeMessages(GH_RuntimeMessageLevel.Warning).ToList();
            return (log, panelCount, warnings);
        }

        [Test]
        public void Solve_PierOfAWall_IsKeptAndReportedAtSummaryWithoutWarning()
        {
            // A 0.45 m wall and a 1.5 m pier of it (0.3, above the default maxThicknessRatio_ 0.2).
            var result = Solve(0, new GH_Brep(Brep.CreateFromBox(new BoundingBox(0, 0, 0, 6, 0.45, 3))), new GH_Brep(Brep.CreateFromBox(new BoundingBox(6, 0, 0, 7.5, 0.45, 3))));

            Assert.Equal(2, result.PanelCount);
            Assert.True(result.Log[0].StartsWith("Info: Created 2 panels (0 Air Panels) from 2 solids. Kept 1 solid as pieces of larger elements. No solids were skipped."));
            Assert.True(result.Log.Any(x => x.StartsWith("Info: Solid #1: Kept although not panel-like on its own")));
            Assert.Empty(result.Warnings);
        }

        [Test]
        public void Summary_IsTheDefault_AndTheLogHoldsTheTotalsAndEveryWarning()
        {
            var absent = Solve(null, Wall(), Tread());
            var summary = Solve(0, Wall(), Tread());

            Assert.Equal(absent.Log, summary.Log);
            Assert.True(summary.Log[0].StartsWith("Info: Created 1 panel (0 Air Panels) from 2 solids."));
            Assert.True(summary.Log.Any(x => x.StartsWith("Warning: Solid #1") && x.Contains("not panel-like")));
            Assert.False(summary.Log.Any(x => x.Contains("Created 1 panel, 0.25 m thick.")));
        }

        [Test]
        public void Standard_AddsOneLinePerSolid_AndDetailedIsAtLeastAsLong()
        {
            var summary = Solve(0, Wall(), Tread());
            var standard = Solve(1, Wall(), Tread());
            var detailed = Solve(2, Wall(), Tread());

            Assert.True(standard.Log.Any(x => x.StartsWith("Info: Solid #0") && x.Contains("Created 1 panel, 0.25 m thick.")));
            Assert.True(standard.Log.Count > summary.Log.Count);
            Assert.True(detailed.Log.Count >= standard.Log.Count);
        }

        [Test]
        public void Warnings_AreTheSameAtEveryLevel_AndPanelsDoNotChange()
        {
            var summary = Solve(0, Wall(), Tread());
            var standard = Solve(1, Wall(), Tread());
            var detailed = Solve(2, Wall(), Tread());

            Assert.Equal(summary.Log.Where(x => x.StartsWith("Warning: ")).ToList(), standard.Log.Where(x => x.StartsWith("Warning: ")).ToList());
            Assert.Equal(summary.Log.Where(x => x.StartsWith("Warning: ")).ToList(), detailed.Log.Where(x => x.StartsWith("Warning: ")).ToList());
            Assert.Equal(summary.PanelCount, standard.PanelCount);
            Assert.Equal(summary.PanelCount, detailed.PanelCount);
            Assert.Equal(summary.Warnings, detailed.Warnings);
        }

        [TestCase(-1)]
        [TestCase(3)]
        [TestCase(99)]
        public void AnInvalidLevel_IsReported_FallsBackToSummary_AndDoesNotChangeThePanels(int value)
        {
            var summary = Solve(0, Wall(), Tread());
            var invalid = Solve(value, Wall(), Tread());

            Assert.Equal(summary.PanelCount, invalid.PanelCount);
            Assert.True(invalid.Warnings.Any(x => x.Contains("diagnosticDetail_") && x.Contains(value.ToString())));
            Assert.True(invalid.Log.Any(x => x.StartsWith("Warning: ") && x.Contains("diagnosticDetail_ is " + value)));
            Assert.False(invalid.Log.Any(x => x.Contains("Created 1 panel, 0.25 m thick.")));
            Assert.Equal(summary.Log.Where(x => x.StartsWith("Warning: ")).ToList(), invalid.Log.Where(x => x.StartsWith("Warning: ") && !x.Contains("diagnosticDetail_")).ToList());
        }

        [Test]
        public void ApertureComponent_AnInvalidLevelIsReported_AndTheLogKeepsItsWarnings()
        {
            SAMAnalyticalCreateAperturesBySolids component = new SAMAnalyticalCreateAperturesBySolids();
            component.CreateAttributes();
            while (component.Params.IndexOfInputParam("diagnosticDetail_") == -1)
            {
                InsertNextInput(component);
            }

            Param_Integer detail = (Param_Integer)component.Params.Input[component.Params.IndexOfInputParam("diagnosticDetail_")];
            detail.PersistentData.Clear();
            detail.SetPersistentData(7);

            // A cube is deep, not a planar window: skipped with a Warning at every level.
            Param_Brep windows = (Param_Brep)component.Params.Input[component.Params.IndexOfInputParam("windows_")];
            windows.PersistentData.Append(new GH_Brep(Brep.CreateFromBox(new BoundingBox(0, 0, 0, 2, 2, 2))));

            component.ClearData();
            component.CollectData();
            component.ComputeData();

            List<string> log = component.Params.Output[component.Params.IndexOfOutputParam("log")].VolatileData.AllData(true).OfType<GH_String>().Select(x => x.Value).ToList();
            Assert.True(log.Any(x => x.StartsWith("Warning: ") && x.Contains("diagnosticDetail_ is 7")));
            Assert.True(log.Any(x => x.StartsWith("Warning: ") && x.Contains("Not planar")));
            Assert.True(component.RuntimeMessages(GH_RuntimeMessageLevel.Warning).Any(x => x.Contains("diagnosticDetail_")));
        }
    }
}
