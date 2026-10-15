// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Special;
using Grasshopper.Kernel.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using NUnitAssert = NUnit.Framework.Assert;

namespace SAM.Core.Grasshopper.Tests
{
    /// <summary>
    /// <b>Safe migration of a published parameter rename, through the existing SAM updater.</b>
    /// <para>
    /// Grasshopper restores a component's parameters from the definition, so an older definition keeps the
    /// OLD name. Without help, the updater — which matches parameters by name — reports the wire and the
    /// typed-in value dropped. A component that declares the rename through
    /// <see cref="GH_SAMComponent.ParameterRenames"/> lets the updater resolve the old name to the new one,
    /// and lets the component's own <c>SolveInstance</c> keep working on a definition that has not been
    /// updated. These tests drive the rename end to end with the fixed-parameter fixture components, so the
    /// shared mechanism is proven independently of any product component.
    /// </para>
    /// </summary>
    [TestFixture]
    public class ParameterRenameMigrationTests
    {
        private const string SkipReason = "GH_Document requires the Rhino native runtime; run these tests in a Rhino-enabled environment.";

        private static GH_Document CreateDocumentOrSkip()
        {
            GH_Document result = TestDocument.TryCreate();
            Skip.If(result == null, SkipReason);
            return result;
        }

        // ---- The lookup legacy definitions rely on -----------------------------------------------------

        /// <summary>
        /// A definition saved under the old name keeps that name in the file. The declared lookup falls back
        /// to the legacy name so the component still computes without being updated.
        /// </summary>
        [Test]
        public void DeclaredLookup_FallsBackToTheLegacyNameOnAnOldDefinition()
        {
            CreateDocumentOrSkip();

            TestRenamedParameterComponent component = new TestRenamedParameterComponent();

            NUnitAssert.That(component.IndexOfOutputParameter("result"), Is.EqualTo(0));
            NUnitAssert.That(component.IndexOfInputParameter("value"), Is.EqualTo(0));

            // a reopened older definition carries the old names
            component.Params.Output[0].Name = "result0";
            component.Params.Input[0].Name = "value0";

            NUnitAssert.That(component.Params.IndexOfOutputParam("result"), Is.EqualTo(-1));
            NUnitAssert.That(component.IndexOfOutputParameter("result"), Is.EqualTo(0), "the declared name must fall back to the legacy name");
            NUnitAssert.That(component.IndexOfOutputParameter("result0"), Is.EqualTo(0));
            NUnitAssert.That(component.IndexOfInputParameter("value"), Is.EqualTo(0));
        }

        // ---- Wires -------------------------------------------------------------------------------------

        [Test]
        public void RenamedOutput_WireIsRestoredOnTheNewName()
        {
            GH_Document document = CreateDocumentOrSkip();
            TestRenamedParameterComponent component = TestDocument.AddComponent<TestRenamedParameterComponent>(document, 100, 100);
            LegacyName(component, GH_ParameterSide.Output, 0, "result0");

            Param_String recipient = TestDocument.AddFloatingParam(document, "recipient", 300, 100);
            recipient.AddSource(component.Params.Output[0]);

            TestDocument.MakeObsolete(component);

            List<GH_SAMComponent> updated = Modify.UpdateComponents(new GH_SAMComponent[] { component }, out Log log, out List<ManualReconnectionIssue> issues);

            GH_SAMComponent component_New = Single(updated);
            NUnitAssert.That(issues, Is.Empty);

            IGH_Param param_New = Output(component_New, "result");
            NUnitAssert.That(param_New, Is.Not.Null);
            NUnitAssert.That(recipient.Sources, Does.Contain(param_New));
            NUnitAssert.That(Output(component_New, "result0"), Is.Null);
            NUnitAssert.That(Texts(log), Has.Some.Contains("migrated by renamed output 'result0' → 'result'"));
        }

        [Test]
        public void RenamedInput_KeepsItsWire()
        {
            GH_Document document = CreateDocumentOrSkip();
            TestRenamedParameterComponent component = TestDocument.AddComponent<TestRenamedParameterComponent>(document, 100, 100);
            LegacyName(component, GH_ParameterSide.Input, 0, "value0");

            Param_String source = TestDocument.AddFloatingParam(document, "source", 0, 100);
            component.Params.Input[0].AddSource(source);

            TestDocument.MakeObsolete(component);

            List<GH_SAMComponent> updated = Modify.UpdateComponents(new GH_SAMComponent[] { component }, out Log log, out List<ManualReconnectionIssue> issues);

            GH_SAMComponent component_New = Single(updated);
            NUnitAssert.That(issues, Is.Empty);

            IGH_Param param_New = Input(component_New, "value");
            NUnitAssert.That(param_New, Is.Not.Null);
            NUnitAssert.That(param_New.Sources, Does.Contain(source));
        }

        // ---- Persistent data ---------------------------------------------------------------------------

        [Test]
        public void RenamedInput_PersistentDataIsTransferred()
        {
            GH_Document document = CreateDocumentOrSkip();
            TestRenamedParameterComponent component = TestDocument.AddComponent<TestRenamedParameterComponent>(document, 100, 100);
            LegacyName(component, GH_ParameterSide.Input, 0, "value0");
            ((Param_String)component.Params.Input[0]).PersistentData.Append(new GH_String("typed by hand"));

            TestDocument.MakeObsolete(component);

            List<GH_SAMComponent> updated = Modify.UpdateComponents(new GH_SAMComponent[] { component }, out Log log, out List<ManualReconnectionIssue> issues);

            GH_SAMComponent component_New = Single(updated);
            NUnitAssert.That(issues, Is.Empty);

            Param_String param_New = Input(component_New, "value") as Param_String;
            NUnitAssert.That(param_New, Is.Not.Null);
            NUnitAssert.That(param_New.PersistentData.get_FirstItem(true)?.Value, Is.EqualTo("typed by hand"));
            NUnitAssert.That(Texts(log), Has.Some.Contains("input migrated by renamed input 'value0' → 'value'"));
        }

        // ---- Refusals: never guess ---------------------------------------------------------------------

        /// <summary>A redirect that crosses a parameter type would be a different wire, so it is refused and
        /// reported, not made.</summary>
        [Test]
        public void RenamedParameter_WithAChangedType_IsRefusedAndReported()
        {
            GH_Document document = CreateDocumentOrSkip();
            TestRenamedParameterComponent component = TestDocument.AddComponent<TestRenamedParameterComponent>(document, 100, 100);

            // an older definition whose input was a number, named under the legacy name
            component.Params.UnregisterInputParameter(component.Params.Input[0]);
            Param_Number legacyNumber = new Param_Number { Name = "value0", NickName = "value0", Description = "value0", Access = GH_ParamAccess.item, Optional = true };
            component.Params.RegisterInputParam(legacyNumber, 0);
            component.Params.OnParametersChanged();
            legacyNumber.PersistentData.Append(new GH_Number(1.5));

            Param_String source = TestDocument.AddFloatingParam(document, "source", 0, 100);
            legacyNumber.AddSource(source);

            TestDocument.MakeObsolete(component);

            List<GH_SAMComponent> updated = Modify.UpdateComponents(new GH_SAMComponent[] { component }, out Log log, out List<ManualReconnectionIssue> issues);

            GH_SAMComponent component_New = Single(updated);

            IGH_Param param_New = Input(component_New, "value");
            NUnitAssert.That(param_New, Is.Not.Null);
            NUnitAssert.That(param_New.Sources, Is.Empty);
            NUnitAssert.That(((Param_String)param_New).PersistentData.AllData(true), Is.Empty);

            ManualReconnectionIssue issue = Single(issues);
            NUnitAssert.That(issue.MissingInputNames, Does.Contain("value0"));
            NUnitAssert.That(Texts(log), Has.Some.Contains("parameter type changed"));
        }

        /// <summary>An ambiguous declaration (two renames on the same legacy name) is reported, and nothing
        /// is redirected.</summary>
        [Test]
        public void ConflictingRenameDeclaration_IsReportedAndNotApplied()
        {
            GH_Document document = CreateDocumentOrSkip();
            TestConflictingRenameComponent component = TestDocument.AddComponent<TestConflictingRenameComponent>(document, 100, 100);
            LegacyName(component, GH_ParameterSide.Output, 0, "dup0");

            Param_String recipient = TestDocument.AddFloatingParam(document, "recipient", 300, 100);
            recipient.AddSource(component.Params.Output[0]);

            TestDocument.MakeObsolete(component);

            List<GH_SAMComponent> updated = Modify.UpdateComponents(new GH_SAMComponent[] { component }, out Log log, out List<ManualReconnectionIssue> issues);

            GH_SAMComponent component_New = Single(updated);

            NUnitAssert.That(component_New.Params.Output.Select(x => x.Name), Does.Contain("a"));
            NUnitAssert.That(recipient.Sources, Is.Empty);
            NUnitAssert.That(issues.Count, Is.EqualTo(1));
            NUnitAssert.That(Texts(log), Has.Some.Contains("Conflicting renames"));
            NUnitAssert.That(Texts(log), Has.Some.Contains("NOT MIGRATED"));
        }

        /// <summary>A broken rename declaration must not stop a component that is otherwise updatable.</summary>
        [Test]
        public void ThrowingRenameDeclaration_DoesNotStopTheUpdate()
        {
            GH_Document document = CreateDocumentOrSkip();
            TestThrowingRenameComponent component = TestDocument.AddComponent<TestThrowingRenameComponent>(document, 100, 100);

            TestDocument.MakeObsolete(component);

            List<GH_SAMComponent> updated = Modify.UpdateComponents(new GH_SAMComponent[] { component }, out Log log, out List<ManualReconnectionIssue> issues);

            NUnitAssert.That(updated.Count, Is.EqualTo(1));
            NUnitAssert.That(Texts(log), Has.Some.Contains("ParameterRenames could not be read"));
        }

        // ---- Group membership (identity) ---------------------------------------------------------------

        /// <summary>
        /// The replacement has a new InstanceGuid, so a group membership recorded against the old identity
        /// must be moved to the replacement or the component silently leaves its group.
        /// </summary>
        [Test]
        public void Update_MovesGroupMembershipToTheReplacement()
        {
            GH_Document document = CreateDocumentOrSkip();
            TestUpdatableComponent component = TestDocument.AddComponent<TestUpdatableComponent>(document, 100, 100);

            GH_Group group = new GH_Group();
            group.CreateAttributes();
            group.NickName = "Group";
            document.AddObject(group, false);
            group.AddObject(component.InstanceGuid);

            TestDocument.MakeObsolete(component);

            List<GH_SAMComponent> updated = Modify.UpdateComponents(new GH_SAMComponent[] { component }, out Log log, out List<ManualReconnectionIssue> issues);

            GH_SAMComponent component_New = Single(updated);
            NUnitAssert.That(component_New.InstanceGuid, Is.Not.EqualTo(component.InstanceGuid));
            NUnitAssert.That(group.ObjectIDs, Does.Contain(component_New.InstanceGuid));
            NUnitAssert.That(group.ObjectIDs, Does.Not.Contain(component.InstanceGuid));
            NUnitAssert.That(Texts(log), Has.Some.Contains("Group membership preserved"));
        }

        /// <summary>Group membership is restored even when there is no rename at all.</summary>
        [Test]
        public void Update_WithoutRenames_StillReconcilesGroups()
        {
            GH_Document document = CreateDocumentOrSkip();
            TestUpdatableComponent component = TestDocument.AddComponent<TestUpdatableComponent>(document, 100, 100);

            GH_Group group = new GH_Group();
            group.CreateAttributes();
            group.NickName = "Group";
            document.AddObject(group, false);
            group.AddObject(component.InstanceGuid);

            TestDocument.MakeObsolete(component);

            List<GH_SAMComponent> updated = Modify.UpdateComponents(new GH_SAMComponent[] { component }, out Log log, out List<ManualReconnectionIssue> issues);

            GH_SAMComponent component_New = Single(updated);
            NUnitAssert.That(group.ObjectIDs, Does.Contain(component_New.InstanceGuid));
        }

        // ---- Behaviour that must not change ------------------------------------------------------------

        /// <summary>
        /// A replacement that cannot be built leaves the original component exactly where it was and does not
        /// leave a half-built object behind in the document.
        /// </summary>
        [Test]
        public void ForcedReplacementFailure_LeavesTheDocumentUnchanged()
        {
            GH_Document document = CreateDocumentOrSkip();
            TestFailingComponent component = TestDocument.AddComponent<TestFailingComponent>(document, 100, 100);
            TestDocument.MakeObsolete(component);

            TestFailingComponent.ThrowOnConstruct = true;
            try
            {
                List<GH_SAMComponent> updated = Modify.UpdateComponents(new GH_SAMComponent[] { component }, out Log log, out List<ManualReconnectionIssue> issues);

                NUnitAssert.That(updated, Is.Null);
                NUnitAssert.That(document.FindObject(component.InstanceGuid, true), Is.SameAs(component));
                NUnitAssert.That(document.Objects.Count, Is.EqualTo(1), "no orphan replacement may be left in the document");

                ManualReconnectionIssue issue = Single(issues);
                NUnitAssert.That(issue.ReplacementFailed, Is.True);
                NUnitAssert.That(issue.ComponentInstanceGuid, Is.EqualTo(component.InstanceGuid));
            }
            finally
            {
                TestFailingComponent.ThrowOnConstruct = false;
            }
        }

        [Test]
        public void Update_AComponentWithoutRenames_BehavesExactlyAsBefore()
        {
            GH_Document document = CreateDocumentOrSkip();
            TestUpdatableComponent component = TestDocument.AddComponent<TestUpdatableComponent>(document, 100, 100);
            Param_String source = TestDocument.AddFloatingParam(document, "source", 0, 100);
            Param_String recipient = TestDocument.AddFloatingParam(document, "recipient", 300, 100);

            component.Params.Input[0].AddSource(source);
            recipient.AddSource(component.Params.Output[0]);
            TestDocument.MakeObsolete(component);

            List<GH_SAMComponent> updated = Modify.UpdateComponents(new GH_SAMComponent[] { component }, out Log log, out List<ManualReconnectionIssue> issues);

            GH_SAMComponent component_New = Single(updated);
            NUnitAssert.That(issues, Is.Empty);
            NUnitAssert.That(component_New.Params.Input[0].Sources, Does.Contain(source));
            NUnitAssert.That(recipient.Sources, Does.Contain(component_New.Params.Output[0]));
            NUnitAssert.That(Texts(log).Any(x => x.Contains("migrated by renamed")), Is.False, "an unrenamed component must not report any migration");
        }

        [Test]
        public void Update_IsIdempotent()
        {
            GH_Document document = CreateDocumentOrSkip();
            TestRenamedParameterComponent component = TestDocument.AddComponent<TestRenamedParameterComponent>(document, 100, 100);
            LegacyName(component, GH_ParameterSide.Output, 0, "result0");
            Param_String recipient = TestDocument.AddFloatingParam(document, "recipient", 300, 100);
            recipient.AddSource(component.Params.Output[0]);
            TestDocument.MakeObsolete(component);

            List<GH_SAMComponent> updated = Modify.UpdateComponents(new GH_SAMComponent[] { component }, out Log log, out List<ManualReconnectionIssue> issues);
            GH_SAMComponent component_New = Single(updated);

            // a second pass finds nothing to do and changes nothing
            List<GH_SAMComponent> updated_Again = Modify.UpdateComponents(new GH_SAMComponent[] { component_New }, out Log log_Again, out List<ManualReconnectionIssue> issues_Again);

            NUnitAssert.That(updated_Again, Is.Null);
            NUnitAssert.That(component_New.Obsolete, Is.False);
            NUnitAssert.That(recipient.Sources, Does.Contain(Output(component_New, "result")));
        }

        [Test]
        public void RenameMap_Resolution_UsesTheDeclaredNames()
        {
            ParameterRenameMap map = ParameterRenameMap.Create(new TestRenamedParameterComponent());

            NUnitAssert.That(map.HasRenames, Is.True);
            NUnitAssert.That(map.Problems, Is.Empty);

            NUnitAssert.That(map.TryResolve(GH_ParameterSide.Output, "result0", out string target, out string problem), Is.True);
            NUnitAssert.That(problem, Is.Null);
            NUnitAssert.That(target, Is.EqualTo("result"));

            // a name no rename mentions is unchanged
            NUnitAssert.That(map.TryResolve(GH_ParameterSide.Output, "unrelated", out target, out problem), Is.True);
            NUnitAssert.That(problem, Is.Null);
            NUnitAssert.That(target, Is.EqualTo("unrelated"));
        }

        // ---- Helpers -----------------------------------------------------------------------------------

        /// <summary>Names a placed parameter the way reopening a definition saved before the rename does.</summary>
        private static void LegacyName(GH_SAMComponent component, GH_ParameterSide side, int index, string legacyName)
        {
            IGH_Param param = side == GH_ParameterSide.Input ? component.Params.Input[index] : component.Params.Output[index];
            param.Name = legacyName;
            param.NickName = legacyName;
        }

        private static IGH_Param Input(IGH_Component component, string name)
        {
            return component.Params.Input.FirstOrDefault(x => x != null && x.Name == name);
        }

        private static IGH_Param Output(IGH_Component component, string name)
        {
            return component.Params.Output.FirstOrDefault(x => x != null && x.Name == name);
        }

        private static List<string> Texts(Log log)
        {
            return log.Select(x => x.Text).ToList();
        }

        private static T Single<T>(IEnumerable<T> values)
        {
            List<T> list = values.ToList();
            NUnitAssert.That(list, Has.Count.EqualTo(1));
            return list[0];
        }
    }
}
