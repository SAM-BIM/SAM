"""Regression tests for the canonical SAM-BIM Grasshopper metadata contract validator.

Run from the SAM repository root:

    python design/grasshopper-icons/tools/tests/test_param_contract.py
    python -m unittest discover -s design/grasshopper-icons/tools/tests -t .

Each test states the finding it protects. Synthetic repositories are written to a temporary directory,
so the tests never modify a repository, and the "reported case" tests read the real SAM component.
"""
import contextlib
import io
import json
import os
import sys
import tempfile
import unittest
import importlib.util

HERE = os.path.dirname(os.path.abspath(__file__))
TOOLS = os.path.dirname(HERE)
SAM_REPO = os.path.dirname(os.path.dirname(os.path.dirname(TOOLS)))
WORKSPACE = os.path.dirname(SAM_REPO)
spec = importlib.util.spec_from_file_location("param_contract", os.path.join(TOOLS, "param_contract.py"))
pc = importlib.util.module_from_spec(spec)
spec.loader.exec_module(pc)


# --- harness -----------------------------------------------------------------------------------------

CSPROJ = """<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0-windows</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Grasshopper" Version="8.0.0" />
  </ItemGroup>
</Project>
"""

CSPROJ_SINGLE_QUOTED = """<Project Sdk='Microsoft.NET.Sdk'>
  <PropertyGroup>
    <TargetFramework>net8.0-windows</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include='Grasshopper' Version='8.0.0' />
    <Compile Remove='Legacy/Excluded.cs' />
  </ItemGroup>
</Project>
"""


def write(path, text):
    directory = os.path.dirname(path)
    if directory and not os.path.isdir(directory):
        os.makedirs(directory)
    with open(path, "w", encoding="utf-8", newline="\n") as stream:
        stream.write(text)


def make_repo(root, sources, project="Demo.Grasshopper", csproj=CSPROJ, extra_projects=None):
    """A synthetic single-project repository: {relative path: C# source}."""
    write(os.path.join(root, project, project + ".csproj"), csproj)
    for name, source in sources.items():
        write(os.path.join(root, project, name), source)
    for project_name, (project_csproj, project_sources) in (extra_projects or {}).items():
        write(os.path.join(root, project_name, project_name + ".csproj"), project_csproj)
        for name, source in project_sources.items():
            write(os.path.join(root, project_name, name), source)
    return root


def run(repo, *arguments):
    """Run the validator CLI and return (exit code, stdout)."""
    buffer = io.StringIO()
    with contextlib.redirect_stdout(buffer):
        code = pc.main(["param_contract.py", "--repo", repo] + list(arguments))
    return code, buffer.getvalue()


def component(repo, name):
    for item in pc.scan(repo)["components"]:
        if item["class"] == name:
            return item
    raise AssertionError("component not found: %s" % name)


def parameter(component_entry, side, name):
    for item in component_entry[side]:
        if item["name"] == name:
            return item
    raise AssertionError("parameter not found: %s %s" % (side, name))


class Fixture(unittest.TestCase):
    def setUp(self):
        self._temporary = tempfile.TemporaryDirectory(prefix="gh-contract-test-")
        self.root = self._temporary.name

    def tearDown(self):
        self._temporary.cleanup()

    def repo(self, sources, **kwargs):
        return make_repo(self.root, sources, **kwargs)


# --- Major 1: parameter extraction ---------------------------------------------------------------------

HEADER = """// SPDX-License-Identifier: LGPL-3.0-or-later
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using SAM.Core.Grasshopper;

namespace Demo
{
    public class __NAME__ : GH_SAMVariableOutputParameterComponent
    {
        public override System.Guid ComponentGuid => new System.Guid("__GUID__");
        public override string LatestComponentVersion => "1.0.0";
        public __NAME__() : base("__NAME__", "__NAME__", "Demo", "SAM", "Demo") { }
__BODY__
    }
    public class GooPanelParam : Grasshopper.Kernel.Parameters.Param_Number { }
    public class GooWidgetParam : Grasshopper.Kernel.Parameters.Param_Number { }
}
"""


def component_source(name, body, guid="11111111-1111-1111-1111-111111111111"):
    return (HEADER.replace("__NAME__", name).replace("__GUID__", guid).replace("__BODY__", body))


class ExtractionTests(Fixture):
    """Major finding 1 - incomplete parameter extraction."""

    def test_reported_create_panels_by_solids_case(self):
        """The reported case: target-typed `new()`, a reused variable and a collection initializer."""
        entry = component(SAM_REPO, "SAMAnalyticalCreatePanelsBySolids")
        self.assertEqual("partial", entry["extraction"]["status"])
        self.assertTrue(all("constructor defaults" in reason for reason in entry["extraction"]["reasons"]))
        self.assertEqual(
            ["_solids", "apertureGeometries_", "minThickness_", "snap_", "maxThicknessRatio_",
             "airForOpenings_", "diagnosticDetail_"],
            [item["name"] for item in entry["inputs"]])
        self.assertEqual(["panels", "airPanels", "excluded", "log"], [item["name"] for item in entry["outputs"]])
        self.assertEqual("Param_Number", parameter(entry, "inputs", "maxThicknessRatio_")["type"])
        self.assertEqual("0.2", parameter(entry, "inputs", "maxThicknessRatio_")["persistentDefault"])
        self.assertEqual("Voluntary", parameter(entry, "inputs", "maxThicknessRatio_")["visibility"])
        self.assertEqual("SAM.Analytical.Grasshopper.GooPanelParam", parameter(entry, "outputs", "panels")["type"])

    def test_variable_reuse_and_target_typed_new(self):
        body = """        protected override GH_SAMParam[] Inputs
        {
            get
            {
                System.Collections.Generic.List<GH_SAMParam> result = new System.Collections.Generic.List<GH_SAMParam>();
                Param_Number number = new() { Name = "_a", NickName = "_a", Description = "A", Access = GH_ParamAccess.item };
                number.SetPersistentData(1.0);
                result.Add(new GH_SAMParam(number, ParamVisibility.Binding));
                number = new() { Name = "_b", NickName = "_b", Description = "B", Access = GH_ParamAccess.item };
                number.SetPersistentData(2.0);
                result.Add(new GH_SAMParam(number, ParamVisibility.Voluntary));
                return result.ToArray();
            }
        }
        protected override GH_SAMParam[] Outputs => new GH_SAMParam[0];
"""
        entry = component(self.repo({"C.cs": component_source("C", body)}), "C")
        self.assertEqual("verified", entry["extraction"]["status"], entry["extraction"]["reasons"])
        self.assertEqual("Param_Number", parameter(entry, "inputs", "_b")["type"])
        self.assertEqual("2.0", parameter(entry, "inputs", "_b")["persistentDefault"])

    def test_collection_initializer_outputs(self):
        body = """        protected override GH_SAMParam[] Inputs => new GH_SAMParam[0];
        protected override GH_SAMParam[] Outputs
        {
            get
            {
                System.Collections.Generic.List<GH_SAMParam> result =
                [
                    new GH_SAMParam(new Param_String() { Name = "Panels", NickName = "Panels", Description = "P", Access = GH_ParamAccess.list }, ParamVisibility.Binding),
                    new GH_SAMParam(new Param_Integer() { Name = "Count", NickName = "Count", Description = "C", Access = GH_ParamAccess.item }, ParamVisibility.Voluntary),
                ];
                return [.. result];
            }
        }
"""
        entry = component(self.repo({"C.cs": component_source("C", body)}), "C")
        self.assertEqual("verified", entry["extraction"]["status"], entry["extraction"]["reasons"])
        self.assertEqual(["Panels", "Count"], [item["name"] for item in entry["outputs"]])

    def test_object_initializer_without_constructor_parentheses(self):
        body = """        protected override GH_SAMParam[] Inputs
        {
            get
            {
                System.Collections.Generic.List<GH_SAMParam> result = new System.Collections.Generic.List<GH_SAMParam>();
                Param_Boolean flag = new Param_Boolean { Name = "_flag", NickName = "_flag", Description = "F", Access = GH_ParamAccess.item };
                result.Add(new GH_SAMParam(flag, ParamVisibility.Binding));
                return result.ToArray();
            }
        }
        protected override GH_SAMParam[] Outputs => new GH_SAMParam[0];
"""
        entry = component(self.repo({"C.cs": component_source("C", body)}), "C")
        self.assertEqual("verified", entry["extraction"]["status"], entry["extraction"]["reasons"])
        self.assertEqual("Param_Boolean", parameter(entry, "inputs", "_flag")["type"])

    def test_verbatim_identifier_is_the_same_variable(self):
        body = """        protected override GH_SAMParam[] Inputs
        {
            get
            {
                System.Collections.Generic.List<GH_SAMParam> result = new System.Collections.Generic.List<GH_SAMParam>();
                Param_Boolean @boolean = new Param_Boolean { Name = "_simplify_", NickName = "_simplify_", Description = "S", Access = GH_ParamAccess.item };
                boolean.SetPersistentData(true);
                result.Add(new GH_SAMParam(boolean, ParamVisibility.Binding));
                return result.ToArray();
            }
        }
        protected override GH_SAMParam[] Outputs => new GH_SAMParam[0];
"""
        entry = component(self.repo({"C.cs": component_source("C", body)}), "C")
        self.assertEqual("verified", entry["extraction"]["status"], entry["extraction"]["reasons"])
        self.assertEqual("true", parameter(entry, "inputs", "_simplify_")["persistentDefault"])

    def test_bare_declaration_then_target_typed_assignment(self):
        body = """        protected override GH_SAMParam[] Inputs
        {
            get
            {
                System.Collections.Generic.List<GH_SAMParam> result = new System.Collections.Generic.List<GH_SAMParam>();
                Param_String param_String;
                param_String = new() { Name = "_name_", NickName = "_name_", Description = "N", Access = GH_ParamAccess.item };
                result.Add(new GH_SAMParam(param_String, ParamVisibility.Binding));
                return result.ToArray();
            }
        }
        protected override GH_SAMParam[] Outputs => new GH_SAMParam[0];
"""
        entry = component(self.repo({"C.cs": component_source("C", body)}), "C")
        self.assertEqual("verified", entry["extraction"]["status"], entry["extraction"]["reasons"])
        self.assertEqual("Param_String", parameter(entry, "inputs", "_name_")["type"])

    def test_parameter_declared_inside_a_foreach_block(self):
        body = """        protected override GH_SAMParam[] Inputs
        {
            get
            {
                System.Collections.Generic.List<GH_SAMParam> result = new System.Collections.Generic.List<GH_SAMParam>();
                foreach (string name in new string[] { "_a", "_b" })
                {
                    Param_Number param_Number = new Param_Number() { Name = name, NickName = name, Description = "N", Access = GH_ParamAccess.item };
                    param_Number.SetPersistentData(0);
                    result.Add(new GH_SAMParam(param_Number, ParamVisibility.Binding));
                }
                return result.ToArray();
            }
        }
        protected override GH_SAMParam[] Outputs => new GH_SAMParam[0];
"""
        entry = component(self.repo({"C.cs": component_source("C", body)}), "C")
        # The loop variable is not a literal, so the name cannot be established - and the reason must
        # say so rather than the parameter silently disappearing.
        self.assertEqual("partial", entry["extraction"]["status"])
        self.assertTrue(any("Name is not a string literal" in reason for reason in entry["extraction"]["reasons"]),
                        entry["extraction"]["reasons"])
        self.assertEqual(1, len(entry["inputs"]))
        self.assertEqual("Param_Number", entry["inputs"][0]["type"])

    def test_manager_registration_and_typed_helper(self):
        body = """        protected override void RegisterInputParams(GH_InputParamManager manager)
        {
            manager.AddParameter(new GooWidgetParam(), "_widget", "_widget", "W", GH_ParamAccess.item);
            manager.AddNumberParameter("_size_", "_size_", "Size", GH_ParamAccess.list, 0.5);
        }
        protected override void RegisterOutputParams(GH_OutputParamManager manager)
        {
            manager.AddBooleanParameter("Successful", "Successful", "OK", GH_ParamAccess.item);
        }
"""
        entry = component(self.repo({"C.cs": component_source("C", body)}), "C")
        self.assertEqual("verified", entry["extraction"]["status"], entry["extraction"]["reasons"])
        self.assertEqual("Demo.GooWidgetParam", parameter(entry, "inputs", "_widget")["type"])
        self.assertEqual("list", parameter(entry, "inputs", "_size_")["access"])
        self.assertEqual("0.5", parameter(entry, "inputs", "_size_")["persistentDefault"])
        self.assertEqual("Param_Boolean", parameter(entry, "outputs", "Successful")["type"])

    def test_manager_optional_assignment_is_attributed(self):
        body = """        protected override void RegisterInputParams(GH_InputParamManager manager)
        {
            GooWidgetParam widget = new GooWidgetParam();
            widget.Optional = true;
            manager.AddParameter(widget, "_widget_", "_widget_", "W", GH_ParamAccess.item);
        }
        protected override void RegisterOutputParams(GH_OutputParamManager manager) { }
"""
        entry = component(self.repo({"C.cs": component_source("C", body)}), "C")
        self.assertEqual("verified", entry["extraction"]["status"], entry["extraction"]["reasons"])
        self.assertTrue(parameter(entry, "inputs", "_widget_")["optional"])

    def test_local_parameter_factory(self):
        body = """        protected override GH_SAMParam[] Inputs
        {
            get
            {
                System.Collections.Generic.List<GH_SAMParam> result = new System.Collections.Generic.List<GH_SAMParam>();
                result.Add(new GH_SAMParam(Number("_size_", "Size", 0.25), ParamVisibility.Binding));
                return result.ToArray();
            }
        }
        protected override GH_SAMParam[] Outputs => new GH_SAMParam[0];

        private static Param_Number Number(string name, string description, double defaultValue)
        {
            Param_Number result = new global::Grasshopper.Kernel.Parameters.Param_Number() { Name = name, NickName = name, Description = description, Access = GH_ParamAccess.item };
            result.SetPersistentData(defaultValue);
            return result;
        }
"""
        entry = component(self.repo({"C.cs": component_source("C", body)}), "C")
        self.assertEqual("partial", entry["extraction"]["status"])
        self.assertTrue(any("factory" in reason for reason in entry["extraction"]["reasons"]))
        self.assertEqual("Param_Number", parameter(entry, "inputs", "_size_")["type"])
        self.assertEqual("0.25", parameter(entry, "inputs", "_size_")["persistentDefault"])
        self.assertEqual("Size", parameter(entry, "inputs", "_size_")["description"])

    def test_inherited_registration_from_abstract_base_in_the_repository(self):
        base = """// SPDX-License-Identifier: LGPL-3.0-or-later
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
namespace Demo
{
    public abstract class BaseComponent : GH_Component
    {
        protected BaseComponent(string name) : base(name, name, "d", "SAM", "Demo") { }
        protected override void RegisterInputParams(GH_InputParamManager manager)
        {
            manager.AddParameter(new Param_String(), "_shared", "_shared", "S", GH_ParamAccess.item);
        }
        protected override void RegisterOutputParams(GH_OutputParamManager manager) { }
    }
}
"""
        derived = """// SPDX-License-Identifier: LGPL-3.0-or-later
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
namespace Demo
{
    public class DerivedComponent : BaseComponent
    {
        public override System.Guid ComponentGuid => new System.Guid("22222222-2222-2222-2222-222222222222");
        public override string LatestComponentVersion => "1.0.0";
        public DerivedComponent() : base("Demo.Derived") { }
    }
}
"""
        entry = component(self.repo({"Base.cs": base, "Derived.cs": derived}), "DerivedComponent")
        self.assertEqual("partial", entry["extraction"]["status"])  # constructor forwards an unresolved one-argument metadata contract
        self.assertEqual("inherited:BaseComponent", entry["registration"])
        self.assertEqual("_shared", entry["inputs"][0]["name"])

    def test_generic_base_type_argument_substitution(self):
        base = """// SPDX-License-Identifier: LGPL-3.0-or-later
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
namespace Demo
{
    public abstract class EnumComponent<T> : GH_Component
    {
        protected EnumComponent(string name) : base(name, name, "d", "SAM", "Demo") { }
        protected override void RegisterInputParams(GH_InputParamManager manager) { }
        protected override void RegisterOutputParams(GH_OutputParamManager manager)
        {
            manager.AddParameter(new Param_String() { Name = typeof(T).Name, NickName = typeof(T).Name, Description = typeof(T).Name, Access = GH_ParamAccess.item });
        }
    }
}
"""
        derived = """// SPDX-License-Identifier: LGPL-3.0-or-later
namespace Demo
{
    public class ColourComponent : EnumComponent<ColourKind>
    {
        public override System.Guid ComponentGuid => new System.Guid("33333333-3333-3333-3333-333333333333");
        public override string LatestComponentVersion => "1.0.0";
        public ColourComponent() : base("Demo.Colour") { }
    }
    public enum ColourKind { Red, Blue }
}
"""
        entry = component(self.repo({"Base.cs": base, "Colour.cs": derived}), "ColourComponent")
        self.assertEqual("partial", entry["extraction"]["status"])  # registration is known; inherited display metadata is not
        self.assertEqual("ColourKind", entry["outputs"][0]["name"])

    def test_missing_outputs_is_not_verified(self):
        body = """        protected override GH_SAMParam[] Inputs
        {
            get
            {
                System.Collections.Generic.List<GH_SAMParam> result = new System.Collections.Generic.List<GH_SAMParam>();
                result.Add(new GH_SAMParam(new Param_String() { Name = "_in", NickName = "_in", Description = "I", Access = GH_ParamAccess.item }, ParamVisibility.Binding));
                return result.ToArray();
            }
        }
"""
        entry = component(self.repo({"C.cs": component_source("C", body)}), "C")
        self.assertNotEqual("verified", entry["extraction"]["status"])

    def test_unrecognised_declaration_is_reported_not_ignored(self):
        body = """        protected override GH_SAMParam[] Inputs
        {
            get
            {
                System.Collections.Generic.List<GH_SAMParam> result = new System.Collections.Generic.List<GH_SAMParam>();
                Param_String param = new Param_String() { Name = "_a", NickName = "_a", Description = "A", Access = GH_ParamAccess.item };
                Params.RegisterInputParam(param);
                return result.ToArray();
            }
        }
        protected override GH_SAMParam[] Outputs => new GH_SAMParam[0];
"""
        entry = component(self.repo({"C.cs": component_source("C", body)}), "C")
        self.assertNotEqual("verified", entry["extraction"]["status"])

    def test_dynamic_name_is_not_a_literal(self):
        body = """        protected override GH_SAMParam[] Inputs
        {
            get
            {
                System.Collections.Generic.List<GH_SAMParam> result = new System.Collections.Generic.List<GH_SAMParam>();
                result.Add(new GH_SAMParam(new Param_String() { Name = string.Format("_{0}", "x"), NickName = "n", Description = "D", Access = GH_ParamAccess.item }, ParamVisibility.Binding));
                return result.ToArray();
            }
        }
        protected override GH_SAMParam[] Outputs => new GH_SAMParam[0];
"""
        entry = component(self.repo({"C.cs": component_source("C", body)}), "C")
        self.assertNotEqual("verified", entry["extraction"]["status"])


# --- Major 2: parameter runtime type -------------------------------------------------------------------

class TypeTests(Fixture):
    """Major finding 2 - the runtime type is part of the frozen contract."""

    def contract_repo(self, type_name="Param_Number"):
        body = """        protected override GH_SAMParam[] Inputs
        {
            get
            {
                System.Collections.Generic.List<GH_SAMParam> result = new System.Collections.Generic.List<GH_SAMParam>();
                result.Add(new GH_SAMParam(new %s() { Name = "_value", NickName = "_value", Description = "V", Access = GH_ParamAccess.item }, ParamVisibility.Binding));
                return result.ToArray();
            }
        }
        protected override GH_SAMParam[] Outputs => new GH_SAMParam[0];
""" % type_name
        return self.repo({"C.cs": component_source("C", body)})

    def test_type_change_fails_the_compatibility_check(self):
        repo = self.contract_repo("Param_Number")
        self.assertEqual(0, run(repo)[0])
        write(os.path.join(repo, "Demo.Grasshopper", "C.cs"), component_source("C", """        protected override GH_SAMParam[] Inputs
        {
            get
            {
                System.Collections.Generic.List<GH_SAMParam> result = new System.Collections.Generic.List<GH_SAMParam>();
                result.Add(new GH_SAMParam(new Param_String() { Name = "_value", NickName = "_value", Description = "V", Access = GH_ParamAccess.item }, ParamVisibility.Binding));
                return result.ToArray();
            }
        }
        protected override GH_SAMParam[] Outputs => new GH_SAMParam[0];
"""))
        code, output = run(repo, "--check")
        self.assertEqual(1, code, output)
        self.assertIn("type", output)
        self.assertIn("Param_Number", output)

    def test_psychrometrics_pilot_type_change_is_detected(self):
        pilot = os.path.join(WORKSPACE, "SAM_Psychrometrics")
        if not os.path.isdir(pilot):
            self.skipTest("SAM_Psychrometrics is not checked out")
        # Copy actual component sources into a synthetic project, never mutate the pilot checkout.
        source_dir = os.path.join(pilot, "Grasshopper", "SAM.Core.Grasshopper.Psychrometrics", "Component")
        sources = {name: pc.read_text(os.path.join(source_dir, name))
                   for name in os.listdir(source_dir) if name.endswith(".cs")}
        make_repo(self.root, sources)
        self.assertEqual(0, run(self.root)[0])
        self.assertEqual(0, run(self.root, "--check")[0])
        path = os.path.join(self.root, "Demo.Grasshopper", "SAMPsychrometricsDewPointTemperature.cs")
        source = pc.read_text(path)
        self.assertIn("new global::Grasshopper.Kernel.Parameters.Param_Number()", source)
        write(path, source.replace("new global::Grasshopper.Kernel.Parameters.Param_Number()",
                                   "new global::Grasshopper.Kernel.Parameters.Param_String()", 1))
        code, output = run(self.root, "--check")
        self.assertEqual(1, code, output)
        self.assertIn("SAMPsychrometricsDewPointTemperature.inputs._dryBulbTemperature: type 'Param_Number' -> 'Param_String'", output)
        self.assertNotIn("component removed", output)
        self.assertNotIn("FAIL extraction", output)

    def test_custom_goo_type_is_supported(self):
        entry = component(self.contract_repo("GooPanelParam"), "C")
        self.assertEqual("verified", entry["extraction"]["status"], entry["extraction"]["reasons"])
        self.assertEqual("goo", parameter(entry, "inputs", "_value")["typeKind"])

    def test_external_parameter_identity_is_recorded_but_unknown_defaults_block_certification(self):
        repo = self.contract_repo("global::RhinoInside.Revit.GH.Parameters.HostObject")
        entry = component(repo, "C")
        self.assertEqual("partial", entry["extraction"]["status"])
        self.assertTrue(any("constructor defaults" in reason for reason in entry["extraction"]["reasons"]))
        self.assertEqual("external", parameter(entry, "inputs", "_value")["typeKind"])
        self.assertEqual(["RhinoInside.Revit.GH.Parameters.HostObject"], pc.scan(repo)["externalParameterTypes"])

    def test_unresolved_extraction_blocks_the_check(self):
        """A parameter whose identity cannot be established must never be reported as verified."""
        body = """        protected override GH_SAMParam[] Inputs
        {
            get
            {
                System.Collections.Generic.List<GH_SAMParam> result = new System.Collections.Generic.List<GH_SAMParam>();
                var value = Create.Param("_value");
                result.Add(new GH_SAMParam(value, ParamVisibility.Binding));
                return result.ToArray();
            }
        }
        protected override GH_SAMParam[] Outputs => new GH_SAMParam[0];

        private static Param_Number Create_Param(string name) { return null; }
"""
        repo = self.repo({"C.cs": component_source("C", body)})
        entry = component(repo, "C")
        self.assertNotEqual("verified", entry["extraction"]["status"], entry["extraction"])
        self.assertEqual(0, run(repo)[0])
        code, output = run(repo, "--check")
        self.assertEqual(1, code, output)
        self.assertIn("extraction", output)


# --- Major 3: discovery and classification --------------------------------------------------------------

class DiscoveryTests(Fixture):
    """Major finding 3 - verifiable classification and project discovery."""

    def test_a_component_named_like_a_parameter_is_a_component(self):
        source = """// SPDX-License-Identifier: LGPL-3.0-or-later
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using SAM.Core.Grasshopper;
namespace Demo
{
    public class NewComputeParam : GH_SAMComponent
    {
        public override System.Guid ComponentGuid => new System.Guid("44444444-4444-4444-4444-444444444444");
        public override string LatestComponentVersion => "1.0.0";
        public NewComputeParam() : base("Demo.NewComputeParam", "Demo.NewComputeParam", "d", "SAM", "Demo") { }
    }
}
"""
        model = pc.scan(self.repo({"C.cs": source}))
        self.assertIn("NewComputeParam", [item["class"] for item in model["components"]])

    def test_a_parameter_class_is_not_a_component_and_is_reported(self):
        source = """// SPDX-License-Identifier: LGPL-3.0-or-later
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
namespace Demo
{
    public class GooThingParam : GH_PersistentParam<GooThing>
    {
        public override System.Guid ComponentGuid => new System.Guid("55555555-5555-5555-5555-555555555555");
    }
    public class GooThing { }
}
"""
        model = pc.scan(self.repo({"C.cs": source}))
        self.assertNotIn("GooThingParam", [item["class"] for item in model["components"]])
        excluded = {item["class"]: item for item in model["parameterClasses"]}
        self.assertIn("GooThingParam", excluded)
        self.assertIn("GH_PersistentParam", excluded["GooThingParam"]["reason"])

    def test_unknown_ancestry_is_reported_not_silently_excluded(self):
        source = """// SPDX-License-Identifier: LGPL-3.0-or-later
namespace Demo
{
    public class MysteryComponent : SomethingFromAnotherAssembly
    {
        public override System.Guid ComponentGuid => new System.Guid("66666666-6666-6666-6666-666666666666");
    }
}
"""
        model = pc.scan(self.repo({"C.cs": source}))
        self.assertNotIn("MysteryComponent", [item["class"] for item in model["components"]])
        unclassified = {item["class"]: item for item in model["unclassifiedClasses"]}
        self.assertIn("MysteryComponent", unclassified)
        self.assertIn("SomethingFromAnotherAssembly", unclassified["MysteryComponent"]["reason"])

    def test_indirect_project_reference_discovers_the_project(self):
        consumer = """<Project Sdk="Microsoft.NET.Sdk">
  <ItemGroup>
    <ProjectReference Include="..\\Demo.Grasshopper\\Demo.Grasshopper.csproj" />
  </ItemGroup>
</Project>
"""
        source = component_source("C", """        protected override GH_SAMParam[] Inputs => new GH_SAMParam[0];
        protected override GH_SAMParam[] Outputs => new GH_SAMParam[0];
""")
        repo = self.repo(
            {"Base.cs": component_source("Base", "        protected override GH_SAMParam[] Inputs => new GH_SAMParam[0];\n        protected override GH_SAMParam[] Outputs => new GH_SAMParam[0];\n")},
            extra_projects={"Demo.Consumer": (consumer, {"C.cs": source})})
        model = pc.scan(repo)
        projects = {item["name"]: item for item in model["projects"]}
        self.assertEqual("indirect", projects["Demo.Consumer"]["grasshopper"])
        self.assertTrue(projects["Demo.Consumer"]["scanned"])
        self.assertIn("Demo.Grasshopper", " ".join(projects["Demo.Consumer"]["evidence"]))
        self.assertIn("C", [item["class"] for item in model["components"]])

    def test_startup_repo_discovers_grasshopper_via_project_reference(self):
        """Placeholder-free check that the evidence string names the referenced project."""
        consumer = """<Project Sdk="Microsoft.NET.Sdk">
  <ItemGroup>
    <ProjectReference Include="..\\Demo.Grasshopper\\Demo.Grasshopper.csproj" />
  </ItemGroup>
</Project>
"""
        repo = self.repo(
            {"Base.cs": component_source("Base", "        protected override GH_SAMParam[] Inputs => new GH_SAMParam[0];\n        protected override GH_SAMParam[] Outputs => new GH_SAMParam[0];\n")},
            extra_projects={"Demo.Consumer": (consumer, {})})
        projects = {item["name"]: item for item in pc.scan(repo)["projects"]}
        self.assertEqual(["indirect:Demo.Grasshopper"], projects["Demo.Consumer"]["evidence"])

    def test_single_quoted_xml_attributes_are_supported(self):
        repo = self.repo({"C.cs": component_source("C", """        protected override GH_SAMParam[] Inputs => new GH_SAMParam[0];
        protected override GH_SAMParam[] Outputs => new GH_SAMParam[0];
""")}, csproj=CSPROJ_SINGLE_QUOTED)
        model = pc.scan(repo)
        projects = {item["name"]: item for item in model["projects"]}
        self.assertEqual("direct", projects["Demo.Grasshopper"]["grasshopper"])
        self.assertEqual(1, len(model["components"]))

    def test_compile_removed_file_is_not_scanned(self):
        repo = self.repo({"C.cs": component_source("C", """        protected override GH_SAMParam[] Inputs => new GH_SAMParam[0];
        protected override GH_SAMParam[] Outputs => new GH_SAMParam[0];
""")}, csproj=CSPROJ)
        write(os.path.join(repo, "Demo.Grasshopper", "Legacy", "Excluded.cs"),
              component_source("Excluded", "        protected override GH_SAMParam[] Inputs => new GH_SAMParam[0];\n        protected override GH_SAMParam[] Outputs => new GH_SAMParam[0];\n"))
        self.assertIn("Excluded", [item["class"] for item in pc.scan(repo)["components"]])
        # with a <Compile Remove> that names it, it is not compiled and is no longer a document object
        write(os.path.join(repo, "Demo.Grasshopper", "Demo.Grasshopper.csproj"), CSPROJ_SINGLE_QUOTED)
        self.assertNotIn("Excluded", [item["class"] for item in pc.scan(repo)["components"]])
        self.assertIn("C", [item["class"] for item in pc.scan(repo)["components"]])

    def test_unparsable_csproj_is_reported(self):
        repo = self.repo({"C.cs": component_source("C", """        protected override GH_SAMParam[] Inputs => new GH_SAMParam[0];
        protected override GH_SAMParam[] Outputs => new GH_SAMParam[0];
""")})
        write(os.path.join(repo, "Broken", "Broken.csproj"), "<Project><PropertyGroup></Project>")
        model = pc.scan(repo)
        broken = [item for item in model["projects"] if item["name"] == "Broken"]
        self.assertEqual(1, len(broken))
        self.assertFalse(broken[0]["scanned"])
        self.assertTrue(broken[0]["evidence"])

    def test_sam_pilot_and_representative_sam_components(self):
        entry = component(SAM_REPO, "SAMAnalyticalCreateAperturesBySolids")
        self.assertEqual("partial", entry["extraction"]["status"])
        self.assertEqual("SAM.Analytical.Grasshopper.GooApertureConstructionParam",
                         parameter(entry, "inputs", "apertureConstructions_")["type"])
        enum_entry = component(SAM_REPO, "SAMAnalyticalApertureType")
        self.assertEqual("partial", enum_entry["extraction"]["status"])
        self.assertEqual("ApertureType", enum_entry["outputs"][0]["name"])


# --- Major 4: compatibility versus naming conventions ---------------------------------------------------

class CompatibilityTests(Fixture):
    """Major finding 4 - compatibility is against the baseline, conventions are separate."""

    def acme(self, access="item", nickname=None, name="_value", optional=False, default=None, comment=""):
        nickname = nickname or name
        optional_text = ", Optional = true" if optional else ""
        body = """        protected override GH_SAMParam[] Inputs
        {
            get
            {
                System.Collections.Generic.List<GH_SAMParam> result = new System.Collections.Generic.List<GH_SAMParam>();
                Param_Number number = new Param_Number() { Name = "%s", NickName = "%s", Description = "V%s", Access = GH_ParamAccess.%s%s };
%s                result.Add(new GH_SAMParam(number, ParamVisibility.Binding));
                return result.ToArray();
            }
        }
        protected override GH_SAMParam[] Outputs => new GH_SAMParam[0];
""" % (name, nickname, comment, access, optional_text,
       ("                number.SetPersistentData(%s);\n" % default) if default is not None else "")
        return self.repo({"C.cs": component_source("C", body)})

    def test_list_and_tree_access_are_legitimate(self):
        for access in ("list", "tree"):
            repo = self.acme(access=access)
            entry = component(repo, "C")
            self.assertEqual(access, entry["inputs"][0]["access"])
            code, output = run(repo)
            self.assertEqual(0, code, output)
            self.assertEqual(0, run(repo, "--check")[0], output)
            self.assertNotIn("unexpected access", pc.description_notes(pc.scan(repo)["components"]))

    def test_existing_nickname_is_valid_and_only_advisory(self):
        repo = self.acme(nickname="_v")
        self.assertEqual(0, run(repo)[0])
        code, output = run(repo, "--check")
        self.assertEqual(0, code, output)
        self.assertIn("NOTE advisory", output)

    def test_code_side_default_does_not_require_set_persistent_data(self):
        repo = self.acme(name="offset_", optional=True, access="item")
        entry = component(repo, "C")
        self.assertIsNone(parameter(entry, "inputs", "offset_")["persistentDefault"])
        self.assertEqual(0, run(repo)[0])
        self.assertEqual(0, run(repo, "--check")[0])

    def test_new_naming_deviation_needs_an_approved_exception(self):
        repo = self.acme(name="_value")
        self.assertEqual(0, run(repo)[0])
        write(os.path.join(repo, "Demo.Grasshopper", "C.cs"), component_source("C", """        protected override GH_SAMParam[] Inputs
        {
            get
            {
                System.Collections.Generic.List<GH_SAMParam> result = new System.Collections.Generic.List<GH_SAMParam>();
                Param_Number number = new Param_Number() { Name = "value", NickName = "value", Description = "V", Access = GH_ParamAccess.item };
                result.Add(new GH_SAMParam(number, ParamVisibility.Binding));
                return result.ToArray();
            }
        }
        protected override GH_SAMParam[] Outputs => new GH_SAMParam[0];
"""))
        code, output = run(repo, "--check")
        self.assertEqual(1, code, output)
        self.assertIn("conventions", output)

    def test_stale_naming_exception_fails(self):
        repo = self.acme(name="_value")
        self.assertEqual(0, run(repo)[0])
        manifest = os.path.join(repo, "design", "grasshopper-icons", "param_contract.json")
        with open(manifest, encoding="utf-8") as stream:
            baseline = json.load(stream)
        baseline["namingExceptions"].append("C.input._gone_")
        write(manifest, json.dumps(baseline))
        code, output = run(repo, "--check")
        self.assertEqual(1, code, output)
        self.assertIn("no longer deviations", output)

    def test_description_change_is_not_a_compatibility_problem(self):
        repo = self.acme()
        self.assertEqual(0, run(repo)[0])
        write(os.path.join(repo, "Demo.Grasshopper", "C.cs"), component_source("C", """        protected override GH_SAMParam[] Inputs
        {
            get
            {
                System.Collections.Generic.List<GH_SAMParam> result = new System.Collections.Generic.List<GH_SAMParam>();
                Param_Number number = new Param_Number() { Name = "_value", NickName = "_value", Description = "Better text", Access = GH_ParamAccess.item };
                result.Add(new GH_SAMParam(number, ParamVisibility.Binding));
                return result.ToArray();
            }
        }
        protected override GH_SAMParam[] Outputs => new GH_SAMParam[0];
"""))
        code, output = run(repo, "--check")
        self.assertEqual(0, code, output)
        self.assertNotIn("FAIL compatibility", output)

    def test_description_quality_is_advisory_unless_strict(self):
        repo = self.acme(name="_value", comment="")
        # empty description
        write(os.path.join(repo, "Demo.Grasshopper", "C.cs"), component_source("C", """        protected override GH_SAMParam[] Inputs
        {
            get
            {
                System.Collections.Generic.List<GH_SAMParam> result = new System.Collections.Generic.List<GH_SAMParam>();
                Param_Number number = new Param_Number() { Name = "_value", NickName = "_value", Description = "", Access = GH_ParamAccess.item };
                result.Add(new GH_SAMParam(number, ParamVisibility.Binding));
                return result.ToArray();
            }
        }
        protected override GH_SAMParam[] Outputs => new GH_SAMParam[0];
"""))
        self.assertEqual(0, run(repo)[0])
        self.assertEqual(0, run(repo, "--check")[0])
        code, _ = run(repo, "--check", "--strict-descriptions")
        self.assertEqual(1, code)

    def test_version_only_change_is_a_manifest_refresh_not_a_compatibility_violation(self):
        repo = self.acme()
        self.assertEqual(0, run(repo)[0])
        write(os.path.join(repo, "Demo.Grasshopper", "C.cs"),
              component_source("C", """        protected override GH_SAMParam[] Inputs
        {
            get
            {
                System.Collections.Generic.List<GH_SAMParam> result = new System.Collections.Generic.List<GH_SAMParam>();
                Param_Number number = new Param_Number() { Name = "_value", NickName = "_value", Description = "V", Access = GH_ParamAccess.item };
                result.Add(new GH_SAMParam(number, ParamVisibility.Binding));
                return result.ToArray();
            }
        }
        protected override GH_SAMParam[] Outputs => new GH_SAMParam[0];
""").replace('"1.0.0"', '"1.0.1"'))
        code, output = run(repo, "--check")
        self.assertEqual(1, code, output)
        self.assertIn("manifest:", output)
        self.assertIn("not a compatibility violation", output)

    def test_missing_baseline_is_refused(self):
        repo = self.acme()
        code, output = run(repo, "--check")
        self.assertEqual(2, code, output)
        self.assertIn("no approved baseline", output)

    def test_incomplete_baseline_blocks_the_check(self):
        repo = self.acme()
        self.assertEqual(0, run(repo)[0])
        manifest = os.path.join(repo, "design", "grasshopper-icons", "param_contract.json")
        with open(manifest, encoding="utf-8") as stream:
            baseline = json.load(stream)
        baseline["components"][0]["extraction"] = {"status": "partial", "reasons": ["recorded as partial"]}
        write(manifest, json.dumps(baseline))
        code, output = run(repo, "--check")
        self.assertEqual(1, code, output)
        self.assertIn("extraction", output)

    def test_inventory_mode_does_not_claim_compatibility(self):
        repo = self.acme()
        code, output = run(repo)
        self.assertEqual(0, code, output)
        self.assertIn("NOT compatibility validation", output)


# --- end-to-end: the real fleet, read-only -----------------------------------------------------------------

class FinalRepairTests(Fixture):
    """Second independent review: reproduce each false PASS and assert its actual failure category."""

    def declared(self, parameter_type="Param_Number", mutation="", wrapper="new GH_SAMParam("):
        return component_source("C", '''
        protected override GH_SAMParam[] Inputs { get {
            var result = new List<GH_SAMParam>();
            TYPE p = new TYPE() { Name = "_value", NickName = "_value", Description = "Value", Access = GH_ParamAccess.item };
            result.Add(WRAPPERp, ParamVisibility.Binding));
            MUTATION
            return result.ToArray();
        } }
        protected override GH_SAMParam[] Outputs => [];
'''.replace("TYPE", parameter_type).replace("WRAPPER", wrapper).replace("MUTATION", mutation))

    def assert_mutation_fails(self, before, after, message, category="compatibility", extra=None):
        repo = self.repo(dict({"C.cs": before}, **(extra or {})))
        self.assertEqual(0, run(repo)[0])
        code, output = run(repo, "--check")
        self.assertEqual(0, code, output)
        write(os.path.join(repo, "Demo.Grasshopper", "C.cs"), after)
        code, output = run(repo, "--check")
        self.assertEqual(1, code, output)
        self.assertIn("FAIL " + category + ":", output)
        self.assertIn(message, output)
        self.assertNotIn("component removed", output)
        self.assertNotIn("OK compatibility", output)

    def test_qualified_external_types_are_distinct(self):
        types = '''namespace VendorA {
public class HostObject : Grasshopper.Kernel.Parameters.Param_Number { }
}
namespace VendorB {
public class HostObject : Grasshopper.Kernel.Parameters.Param_Number { }
}'''
        self.assert_mutation_fails(self.declared("global::VendorA.HostObject"),
                                   self.declared("global::VendorB.HostObject"),
                                   "type 'VendorA.HostObject' -> 'VendorB.HostObject'", extra={"Types.cs": types})

    def test_closed_generic_type_arguments_are_distinct(self):
        types = "namespace Demo { public class GooValueParam<T> : Grasshopper.Kernel.Parameters.Param_Number { } }"
        self.assert_mutation_fails(self.declared("GooValueParam<int>"), self.declared("GooValueParam<string>"),
                                   "Demo.GooValueParam<System.Int32>", extra={"Types.cs": types})

    def test_equivalent_global_and_builtin_spellings_pass(self):
        repo = self.repo({"C.cs": self.declared("Param_Number")})
        self.assertEqual(0, run(repo)[0])
        write(os.path.join(repo, "Demo.Grasshopper", "C.cs"), self.declared("global::Grasshopper.Kernel.Parameters.Param_Number"))
        self.assertEqual(0, run(repo, "--check")[0])

    def test_type_alias_resolves_and_alias_target_change_fails(self):
        types = '''namespace VendorA {
public class HostObject : Grasshopper.Kernel.Parameters.Param_Number { }
}
namespace VendorB {
public class HostObject : Grasshopper.Kernel.Parameters.Param_Number { }
}'''
        before = "using Alias = global::VendorA.HostObject;\n" + self.declared("Alias")
        after = before.replace("VendorA", "VendorB")
        self.assert_mutation_fails(before, after, "type 'VendorA.HostObject' -> 'VendorB.HostObject'", extra={"Types.cs": types})

    def test_ambiguous_imported_type_blocks_verification(self):
        source = "using VendorA; using VendorB;\n" + self.declared("HostObject")
        types = "namespace VendorA { public class HostObject { } }\nnamespace VendorB { public class HostObject { } }"
        repo = self.repo({"C.cs": source, "Types.cs": types})
        entry = component(repo, "C")
        self.assertEqual("partial", entry["extraction"]["status"])
        self.assertEqual(0, run(repo)[0])
        code, output = run(repo, "--check")
        self.assertEqual(1, code, output)
        self.assertIn("identity is unresolved or ambiguous", output)

    def test_constructor_whitespace_preserves_contract(self):
        repo = self.repo({"C.cs": self.declared()})
        self.assertEqual(0, run(repo)[0])
        spaced = self.declared(wrapper="new GH_SAMParam ( ")
        write(os.path.join(repo, "Demo.Grasshopper", "C.cs"), spaced)
        self.assertEqual(0, run(repo, "--check")[0])
        self.assert_mutation_fails(spaced, spaced.replace('Name = "_value"', 'Name = "_renamed"'), "names/order changed")

    def test_declarative_optional_assignment_after_wrapping_is_compared(self):
        self.assert_mutation_fails(self.declared(mutation="p.Optional = false;"),
                                   self.declared(mutation="p.Optional = true;"), "optional False -> True")

    def test_declarative_optional_assignment_before_wrapping_is_compared(self):
        before = self.declared().replace("result.Add(", "p.Optional = false; result.Add(")
        self.assert_mutation_fails(before, before.replace("p.Optional = false", "p.Optional = true"), "optional False -> True")

    def test_persistent_data_after_wrapping_is_compared(self):
        self.assert_mutation_fails(self.declared(mutation="p.SetPersistentData(1);"),
                                   self.declared(mutation="p.SetPersistentData(2);"), "persistentDefault '1' -> '2'")

    def test_reassigned_parameter_does_not_mutate_preceding_wrapper(self):
        source = self.declared(mutation='p = new Param_Number() { Name = "_other", NickName = "_other", Access = GH_ParamAccess.item }; p.Optional = true;')
        entry = component(self.repo({"C.cs": source}), "C")
        self.assertEqual("verified", entry["extraction"]["status"], entry["extraction"])
        self.assertFalse(entry["inputs"][0]["optional"])

    def test_unknown_reassignment_invalidates_the_old_binding(self):
        source = self.declared().replace("result.Add(", "p = UnknownFactory(); result.Add(")
        entry = component(self.repo({"C.cs": source}), "C")
        self.assertNotEqual("verified", entry["extraction"]["status"])

    def test_helper_returned_outputs_cannot_be_certified_empty(self):
        source = self.declared().replace("Outputs => []", "Outputs => BuildOutputs()")
        repo = self.repo({"C.cs": source})
        self.assertEqual(0, run(repo)[0])
        code, output = run(repo, "--check")
        self.assertEqual(1, code, output)
        self.assertIn("unsupported output return expression", output)
        self.assertNotIn("OK compatibility", output)

    def test_loop_with_literal_name_cannot_be_certified_as_one_parameter(self):
        source = self.declared().replace("result.Add(new", "foreach (var unused in new [] { 1, 2 }) { result.Add(new").replace("p, ParamVisibility.Binding));", "p, ParamVisibility.Binding)); }")
        entry = component(self.repo({"C.cs": source}), "C")
        self.assertNotEqual("verified", entry["extraction"]["status"])
        self.assertTrue(any("control flow" in r for r in entry["extraction"]["reasons"]))

    def test_filtering_a_return_collection_is_not_certified(self):
        source = self.declared().replace("result.ToArray()", "result.Where(p => false).ToArray()")
        entry = component(self.repo({"C.cs": source}), "C")
        self.assertNotEqual("verified", entry["extraction"]["status"])

    def test_discarding_the_populated_collection_is_not_certified(self):
        source = self.declared().replace("return result.ToArray()", "return []")
        entry = component(self.repo({"C.cs": source}), "C")
        self.assertNotEqual("verified", entry["extraction"]["status"])

    def test_global_type_does_not_bind_to_a_relative_shadow(self):
        context = {"namespace": "Demo", "types": {"Demo.VendorA.HostObject", "VendorA.HostObject"}}
        self.assertEqual("VendorA.HostObject", pc.type_identity("global::VendorA.HostObject", context))
        self.assertEqual("Demo.VendorA.HostObject", pc.type_identity("VendorA.HostObject", context))

    def test_genuinely_empty_declarations_are_verified(self):
        source = component_source("C", "protected override GH_SAMParam[] Inputs => [];\nprotected override GH_SAMParam[] Outputs => new GH_SAMParam[0];")
        entry = component(self.repo({"C.cs": source}), "C")
        self.assertEqual("verified", entry["extraction"]["status"], entry["extraction"])

    def test_no_registration_evidence_is_unresolved(self):
        entry = component(self.repo({"C.cs": component_source("C", "")}), "C")
        self.assertEqual("unresolved", entry["extraction"]["status"])
        self.assertTrue(entry["extraction"]["reasons"])

    def test_revit_paramdefinition_registration_is_explicitly_unresolved(self):
        source = component_source("C", '''
protected override ParamDefinition[] Inputs => [new ParamDefinition(new Param_Number() { Name = "_value" }, ParamRelevance.Binding)];
protected override ParamDefinition[] Outputs => [];
''')
        repo = self.repo({"C.cs": source})
        entry = component(repo, "C")
        self.assertEqual("unresolved", entry["extraction"]["status"])
        self.assertEqual(0, run(repo)[0])
        code, output = run(repo, "--check")
        self.assertEqual(1, code, output)
        self.assertIn("ParamDefinition[]", output)

    def test_multilevel_inheritance_reads_the_declaring_base(self):
        base = '''using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
namespace Demo {
public abstract class Base : GH_Component {
protected override void RegisterInputParams(GH_InputParamManager m) { m.AddNumberParameter("_shared", "_shared", "Shared", GH_ParamAccess.item); }
protected override void RegisterOutputParams(GH_OutputParamManager m) { }
}
public abstract class Middle : Base { }
}'''
        derived = component_source("C", "").replace(": GH_SAMVariableOutputParameterComponent", ": Middle")
        repo = self.repo({"C.cs": derived, "Base.cs": base})
        entry = component(repo, "C")
        self.assertEqual("verified", entry["extraction"]["status"], entry["extraction"])
        self.assertEqual("_shared", entry["inputs"][0]["name"])
        self.assertEqual(0, run(repo)[0])
        self.assertEqual(0, run(repo, "--check")[0])
        write(os.path.join(repo, "Demo.Grasshopper", "Base.cs"), base.replace("AddNumberParameter", "AddTextParameter"))
        code, output = run(repo, "--check")
        self.assertEqual(1, code, output)
        self.assertIn("type 'Param_Number' -> 'Param_String'", output)

    def test_unknown_ancestry_is_a_fatal_discovery_finding(self):
        repo = self.repo({"C.cs": self.declared()})
        self.assertEqual(0, run(repo)[0])
        self.assertEqual(0, run(repo, "--check")[0])
        write(os.path.join(repo, "Demo.Grasshopper", "Unknown.cs"), component_source("Mystery", "").replace(": GH_SAMVariableOutputParameterComponent", ": UnknownExternalBase"))
        code, output = run(repo, "--check")
        self.assertEqual(1, code, output)
        self.assertIn("unclassified class Mystery at Demo.Grasshopper/Unknown.cs", output)
        self.assertNotIn("component removed", output)

    def test_malformed_project_is_a_fatal_discovery_finding(self):
        repo = self.repo({"C.cs": self.declared()})
        self.assertEqual(0, run(repo)[0])
        write(os.path.join(repo, "Broken", "Broken.csproj"), "<Project><PropertyGroup></Project>")
        code, output = run(repo, "--check")
        self.assertEqual(1, code, output)
        self.assertIn("unparseable project Broken/Broken.csproj", output)
        self.assertNotIn("component removed", output)

    def test_non_component_guid_reference_is_not_an_unclassified_object(self):
        repo = self.repo({"C.cs": self.declared(), "Query.cs": '''namespace Demo {
public static class Query { public static object Copy(GH_Component c) { return c.ComponentGuid; } }
}'''})
        self.assertEqual([], pc.scan(repo)["unclassifiedClasses"])
        self.assertEqual(0, run(repo)[0])
        code, output = run(repo, "--check")
        self.assertEqual(0, code, output)

    def test_v3_baseline_without_extraction_evidence_fails(self):
        repo = self.repo({"C.cs": self.declared()})
        self.assertEqual(0, run(repo)[0])
        path = os.path.join(repo, "design", "grasshopper-icons", "param_contract.json")
        baseline = json.loads(pc.read_text(path))
        del baseline["components"][0]["extraction"]
        write(path, json.dumps(baseline))
        code, output = run(repo, "--check")
        self.assertEqual(1, code, output)
        self.assertIn("baseline has no extraction evidence", output)

    def test_custom_constructor_defaults_are_not_assumed_to_be_grasshopper_defaults(self):
        types = '''namespace Demo {
public class CustomParam : Grasshopper.Kernel.Parameters.Param_Number {
    public CustomParam() { Optional = true; SetPersistentData(123); }
}
}'''
        repo = self.repo({"C.cs": self.declared("CustomParam"), "Types.cs": types})
        entry = component(repo, "C")
        self.assertEqual("partial", entry["extraction"]["status"])
        self.assertEqual(0, run(repo)[0])
        code, output = run(repo, "--check")
        self.assertEqual(1, code, output)
        self.assertIn("constructor defaults of parameter type Demo.CustomParam", output)

    def test_unknown_component_metadata_cannot_pass_as_matching_nulls(self):
        source = self.declared().replace('base("C", "C", "Demo", "SAM", "Demo")', 'base(GetName(), "C", "Demo", "SAM", "Demo")')
        repo = self.repo({"C.cs": source})
        self.assertEqual(0, run(repo)[0])
        code, output = run(repo, "--check")
        self.assertEqual(1, code, output)
        self.assertIn("component name is not a proven constructor literal", output)


class RealRepositoryTests(unittest.TestCase):
    def test_wave_one_repositories_report_honest_coverage(self):
        expected = {"SAM_GEM": 1, "SAM_BHoM": 3, "SAM_Python": 3}
        for name, count in expected.items():
            repo = os.path.join(WORKSPACE, name)
            if not os.path.isdir(repo):
                self.skipTest("%s is not checked out" % name)
            model = pc.scan(repo)
            summary = pc.summarise(model)
            self.assertEqual(count, summary["components"], name)
            self.assertEqual(count, sum(summary[key] for key in ("verified", "partial", "unresolved")), name)
            self.assertTrue(all(c["extraction"]["reasons"] for c in model["components"]
                                if c["extraction"]["status"] != "verified"), name)

    def test_real_revit_align_walls_is_not_certified_empty(self):
        repo = os.path.join(WORKSPACE, "SAM_Revit")
        if not os.path.isdir(repo):
            self.skipTest("SAM_Revit is not checked out")
        entry = component(repo, "RevitAlignWalls")
        self.assertEqual("unresolved", entry["extraction"]["status"], entry)
        self.assertTrue(any("ParamDefinition" in r for r in entry["extraction"]["reasons"]))

    def test_psychrometrics_baseline_passes_and_is_still_the_pilot(self):
        pilot = os.path.join(WORKSPACE, "SAM_Psychrometrics")
        if not os.path.isdir(pilot):
            self.skipTest("SAM_Psychrometrics is not checked out")
        code, output = run(pilot, "--check")
        self.assertEqual(0, code, output)
        self.assertIn("3 components discovered, 3 fully verified", output)
        self.assertIn("OK compatibility", output)


if __name__ == "__main__":
    unittest.main(verbosity=2)
