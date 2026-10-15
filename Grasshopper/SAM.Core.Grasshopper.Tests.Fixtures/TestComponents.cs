// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using System;
using System.Collections.Generic;

namespace SAM.Core.Grasshopper.Tests
{
    public abstract class TestComponentBase : GH_SAMComponent
    {
        protected TestComponentBase(string name, string nickname, string description, string category, string subCategory)
            : base(name, nickname, description, category, subCategory)
        {
        }

        public void SetComponentVersion(string value)
        {
            SetValue("SAM_ComponentVersion", value);
        }
    }

    public class TestUpdatableComponent : TestComponentBase
    {
        public override Guid ComponentGuid => new Guid("6f8a3f5e-9c1d-4b7a-8a2e-1d5c9b0e4a21");

        public override string LatestComponentVersion => "1.0.0";

        public TestUpdatableComponent()
            : base("TestUpdatable", "TestUpdatable", "Test component for update tests", "SAM", "Test")
        {
        }

        protected override void RegisterInputParams(GH_InputParamManager manager)
        {
            manager.AddParameter(new Param_String { Name = "a", NickName = "a", Description = "a", Access = GH_ParamAccess.item, Optional = true });
            manager.AddParameter(new Param_String { Name = "b", NickName = "b", Description = "b", Access = GH_ParamAccess.item, Optional = true });
        }

        protected override void RegisterOutputParams(GH_OutputParamManager manager)
        {
            manager.AddParameter(new Param_String { Name = "x", NickName = "x", Description = "x", Access = GH_ParamAccess.item });
            manager.AddParameter(new Param_String { Name = "y", NickName = "y", Description = "y", Access = GH_ParamAccess.item });
        }

        protected override void SolveInstance(IGH_DataAccess dataAccess)
        {
        }
    }

    /// <summary>A fixed-output component whose default input bakes in its own persistent data at registration,
    /// the way <c>SAMAnalyticalCreateCaseByApertureByAzimuths</c>'s "_ratios" does.</summary>
    public class TestDefaultPersistentDataComponent : TestComponentBase
    {
        public override Guid ComponentGuid => new Guid("d4e5f6a7-8b9c-4d3e-9f1a-2b3c4d5e6f70");

        public override string LatestComponentVersion => "1.0.0";

        public TestDefaultPersistentDataComponent()
            : base("TestDefaultPersistentData", "TestDefaultPersistentData", "Test component whose default input ships with its own baked-in persistent data", "SAM", "Test")
        {
        }

        protected override void RegisterInputParams(GH_InputParamManager manager)
        {
            Param_Number ratios = new Param_Number { Name = "ratios", NickName = "ratios", Description = "ratios", Access = GH_ParamAccess.list, Optional = true };
            ratios.SetPersistentData(0.15, 0.2, 0.25, 0.2);
            manager.AddParameter(ratios);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager manager)
        {
            manager.AddParameter(new Param_String { Name = "x", NickName = "x", Description = "x", Access = GH_ParamAccess.item });
        }

        protected override void SolveInstance(IGH_DataAccess dataAccess)
        {
        }
    }

    public class TestFailingComponent : TestComponentBase
    {        public static bool ThrowOnConstruct;

        public override Guid ComponentGuid => new Guid("2c4d9b7a-0e5f-4c3a-9d8b-7a6e5f4c3b12");

        public override string LatestComponentVersion => "1.0.0";

        public TestFailingComponent()
            : base("TestFailing", "TestFailing", "Test component that fails to construct", "SAM", "Test")
        {
            if (ThrowOnConstruct)
            {
                throw new InvalidOperationException("Construction failed");
            }
        }

        protected override void RegisterInputParams(GH_InputParamManager manager)
        {
            manager.AddParameter(new Param_String { Name = "a", NickName = "a", Description = "a", Access = GH_ParamAccess.item, Optional = true });
        }

        protected override void RegisterOutputParams(GH_OutputParamManager manager)
        {
            manager.AddParameter(new Param_String { Name = "x", NickName = "x", Description = "x", Access = GH_ParamAccess.item });
        }

        protected override void SolveInstance(IGH_DataAccess dataAccess)
        {
        }
    }

    /// <summary>A component that has published a rename: the input <c>value0</c> is now <c>value</c>, and the
    /// output <c>result0</c> is now <c>result</c>. The declaration is what lets the SAM updater carry a
    /// definition saved under the old names onto the new parameters.</summary>
    public class TestRenamedParameterComponent : TestComponentBase
    {
        public override Guid ComponentGuid => new Guid("3a7c1e88-6b24-4f19-9a3d-52e8c4b71d05");

        public override string LatestComponentVersion => "1.1.0";

        public TestRenamedParameterComponent()
            : base("TestRenamedParameter", "TestRenamedParameter", "Test component that published a parameter rename", "SAM", "Test")
        {
        }

        public override IReadOnlyList<ParameterRename> ParameterRenames => new[]
        {
            new ParameterRename(GH_ParameterSide.Input, "value0", "value"),
            new ParameterRename(GH_ParameterSide.Output, "result0", "result"),
        };

        protected override void RegisterInputParams(GH_InputParamManager manager)
        {
            manager.AddParameter(new Param_String { Name = "value", NickName = "value", Description = "value", Access = GH_ParamAccess.item, Optional = true });
        }

        protected override void RegisterOutputParams(GH_OutputParamManager manager)
        {
            manager.AddParameter(new Param_String { Name = "result", NickName = "result", Description = "result", Access = GH_ParamAccess.item });
        }

        protected override void SolveInstance(IGH_DataAccess dataAccess)
        {
        }
    }

    /// <summary>A component whose rename declaration is ambiguous: two renames claim the same legacy output
    /// name. The migration must refuse to guess rather than attach a wire to an arbitrary one.</summary>
    public class TestConflictingRenameComponent : TestComponentBase
    {
        public override Guid ComponentGuid => new Guid("c1f2d3e4-5a6b-4c7d-8e9f-0a1b2c3d4e5f");

        public override string LatestComponentVersion => "1.1.0";

        public TestConflictingRenameComponent()
            : base("TestConflictingRename", "TestConflictingRename", "Test component with an ambiguous rename declaration", "SAM", "Test")
        {
        }

        public override IReadOnlyList<ParameterRename> ParameterRenames => new[]
        {
            new ParameterRename(GH_ParameterSide.Output, "dup0", "a"),
            new ParameterRename(GH_ParameterSide.Output, "dup0", "b"),
        };

        protected override void RegisterInputParams(GH_InputParamManager manager)
        {
            manager.AddParameter(new Param_String { Name = "value", NickName = "value", Description = "value", Access = GH_ParamAccess.item, Optional = true });
        }

        protected override void RegisterOutputParams(GH_OutputParamManager manager)
        {
            manager.AddParameter(new Param_String { Name = "a", NickName = "a", Description = "a", Access = GH_ParamAccess.item });
            manager.AddParameter(new Param_String { Name = "b", NickName = "b", Description = "b", Access = GH_ParamAccess.item });
        }

        protected override void SolveInstance(IGH_DataAccess dataAccess)
        {
        }
    }

    /// <summary>A component whose rename declaration cannot even be read. A broken declaration must not stop
    /// a component that is otherwise perfectly updatable.</summary>
    public class TestThrowingRenameComponent : TestComponentBase
    {
        public override Guid ComponentGuid => new Guid("9d8c7b6a-5e4f-4a3b-8c2d-1f0e9d8c7b6a");

        public override string LatestComponentVersion => "1.1.0";

        public TestThrowingRenameComponent()
            : base("TestThrowingRename", "TestThrowingRename", "Test component whose rename declaration throws", "SAM", "Test")
        {
        }

        public override IReadOnlyList<ParameterRename> ParameterRenames => throw new InvalidOperationException("broken declaration");

        protected override void RegisterInputParams(GH_InputParamManager manager)
        {
            manager.AddParameter(new Param_String { Name = "value", NickName = "value", Description = "value", Access = GH_ParamAccess.item, Optional = true });
        }

        protected override void RegisterOutputParams(GH_OutputParamManager manager)
        {
            manager.AddParameter(new Param_String { Name = "result", NickName = "result", Description = "result", Access = GH_ParamAccess.item });
        }

        protected override void SolveInstance(IGH_DataAccess dataAccess)
        {
        }
    }
}
