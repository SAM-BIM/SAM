// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SAM.Core.Grasshopper
{
    public static partial class Modify
    {
        public static List<GH_SAMComponent> UpdateComponents(GH_Document gH_Document, out Log log)
        {
            return UpdateComponents(gH_Document, out log, out List<ManualReconnectionIssue> manualReconnectionIssues);
        }

        public static List<GH_SAMComponent> UpdateComponents(GH_Document gH_Document, out Log log, out List<ManualReconnectionIssue> manualReconnectionIssues)
        {
            log = new Log();
            manualReconnectionIssues = new List<ManualReconnectionIssue>();

            if (gH_Document == null)
            {
                return null;
            }

            IList<IGH_DocumentObject> gH_DocumentObjects = gH_Document.Objects;
            if (gH_DocumentObjects == null || gH_DocumentObjects.Count == 0)
            {
                return null;
            }

            List<GH_SAMComponent> gH_SAMComponents = new List<GH_SAMComponent>();
            foreach (IGH_DocumentObject gH_DocumentObject in gH_DocumentObjects)
            {
                if (!(gH_DocumentObject is GH_SAMComponent gH_SAMComponent))
                {
                    continue;
                }

                if (gH_SAMComponent.Obsolete)
                {
                    gH_SAMComponents.Add(gH_SAMComponent);
                }
            }

            if (gH_SAMComponents.Count == 0)
            {
                log.Add(new LogRecord("No obsolete components found.", LogRecordType.Message));
                return new List<GH_SAMComponent>();
            }

            return UpdateComponents(gH_SAMComponents, gH_Document, out log, out manualReconnectionIssues);
        }

        public static List<GH_SAMComponent> UpdateComponents(IEnumerable<GH_SAMComponent> gH_SAMComponents, out Log log)
        {
            return UpdateComponents(gH_SAMComponents, out log, out List<ManualReconnectionIssue> manualReconnectionIssues);
        }

        public static List<GH_SAMComponent> UpdateComponents(IEnumerable<GH_SAMComponent> gH_SAMComponents, out Log log, out List<ManualReconnectionIssue> manualReconnectionIssues)
        {
            log = new Log();
            manualReconnectionIssues = new List<ManualReconnectionIssue>();

            if (gH_SAMComponents == null || !gH_SAMComponents.Any())
            {
                return null;
            }

            GH_Document gH_Document = gH_SAMComponents.FirstOrDefault()?.OnPingDocument();
            if (gH_Document == null)
            {
                log.Add(new LogRecord("Could not access document.", LogRecordType.Error));
                return null;
            }

            return UpdateComponents(gH_SAMComponents, gH_Document, out log, out manualReconnectionIssues);
        }

        private static List<GH_SAMComponent> UpdateComponents(IEnumerable<GH_SAMComponent> gH_SAMComponents, GH_Document gH_Document, out Log log)
        {
            return UpdateComponents(gH_SAMComponents, gH_Document, out log, out List<ManualReconnectionIssue> manualReconnectionIssues);
        }

        private static List<GH_SAMComponent> UpdateComponents(IEnumerable<GH_SAMComponent> gH_SAMComponents, GH_Document gH_Document, out Log log, out List<ManualReconnectionIssue> manualReconnectionIssues)
        {
            log = new Log();
            manualReconnectionIssues = new List<ManualReconnectionIssue>();

            if (gH_SAMComponents == null || !gH_SAMComponents.Any())
            {
                return null;
            }

            if (gH_Document == null)
            {
                log.Add(new LogRecord("Document is null.", LogRecordType.Error));
                return null;
            }

            GH_SAMComponent[] components = gH_SAMComponents.ToArray();

            int obsoleteCount = components.Count(c => c != null && c.Obsolete);
            log.Add(new LogRecord("Update started. Targeting {0} component(s), {1} are obsolete.",
                LogRecordType.Message, components.Length, obsoleteCount));

            int updated = 0;
            int failed = 0;
            int warnings = 0;
            List<GH_SAMComponent> result = new List<GH_SAMComponent>();
            List<GH_SAMComponent> oldComponents = new List<GH_SAMComponent>();

            try
            {
                gH_Document.UndoUtil.RecordEvent("Update Obsolete Components");
            }
            catch
            {
                log.Add(new LogRecord("Undo grouping not available — updates will be individually undoable.", LogRecordType.Warning));
                warnings++;
            }

            foreach (GH_SAMComponent gH_SAMComponent in components)
            {
                if (gH_SAMComponent == null)
                {
                    continue;
                }

                if (!gH_SAMComponent.Obsolete)
                {
                    continue;
                }

                try
                {
                    GH_SAMComponent gH_SAMComponent_New = DuplicateComponent(gH_SAMComponent, out Log log_Temp, out List<ManualReconnectionWire> manualReconnectionWires);
                    if (gH_SAMComponent_New == null)
                    {
                        failed++;
                        if (log_Temp != null)
                        {
                            log.AddRange(log_Temp);
                        }

                        ManualReconnectionIssue manualReconnectionIssue = CreateManualReconnectionIssue(gH_SAMComponent, null, true);
                        if (manualReconnectionIssue != null)
                        {
                            manualReconnectionIssues.Add(manualReconnectionIssue);
                        }

                        continue;
                    }

                    result.Add(gH_SAMComponent_New);
                    oldComponents.Add(gH_SAMComponent);
                    updated++;

                    if (log_Temp != null)
                    {
                        log.AddRange(log_Temp);
                    }

                    if (manualReconnectionWires != null && manualReconnectionWires.Count != 0)
                    {
                        ManualReconnectionIssue manualReconnectionIssue = CreateManualReconnectionIssue(gH_SAMComponent_New, manualReconnectionWires, false);
                        if (manualReconnectionIssue != null)
                        {
                            manualReconnectionIssues.Add(manualReconnectionIssue);
                        }
                    }
                }
                catch (Exception ex)
                {
                    failed++;
                    log.Add(new LogRecord("  Failed to update {0}: {1}", LogRecordType.Error, gH_SAMComponent.Name, ex.Message));

                    ManualReconnectionIssue manualReconnectionIssue = CreateManualReconnectionIssue(gH_SAMComponent, null, true);
                    if (manualReconnectionIssue != null)
                    {
                        manualReconnectionIssues.Add(manualReconnectionIssue);
                    }
                }
            }

            foreach (GH_SAMComponent oldComponent in oldComponents)
            {
                gH_Document.RemoveObject(oldComponent, false);
            }

            //A replacement is a new object with a new InstanceGuid, which is what every group membership is
            //recorded against, so an updated component silently leaves the group it belonged to unless the
            //group is reconciled to the replacement's identity. See ReconcileGroupMembership.
            for (int i = 0; i < oldComponents.Count && i < result.Count; i++)
            {
                int groups = ReconcileGroupMembership(gH_Document, oldComponents[i], result[i], out Log log_Group);
                if (groups != 0 && log_Group != null)
                {
                    log.AddRange(log_Group);
                }
            }

            SortManualReconnectionIssues(manualReconnectionIssues);

            log.Add(new LogRecord("Update completed: {0} updated, {1} failed, {2} warning(s).",
                LogRecordType.Message, updated, failed, warnings));

            if (manualReconnectionIssues.Count != 0)
            {
                log.Add(new LogRecord("{0} component(s) require manual wire reconnection.", LogRecordType.Warning, manualReconnectionIssues.Count));
            }

            if (result.Count == 0)
            {
                return null;
            }

            return result;
        }

        /// <summary>
        /// <b>Moves a component's group membership from the object being replaced to its replacement.</b>
        /// <para>
        /// A <c>GH_Group</c> records its members as the <see cref="IGH_DocumentObject.InstanceGuid"/> of each
        /// one, and the updater replaces a component with a newly constructed instance that therefore has a
        /// new InstanceGuid. Left alone, the group keeps pointing at an object that is no longer in the
        /// document, and the component drops out of the group.
        /// </para>
        /// <para>
        /// Adopting the OLD InstanceGuid on the replacement instead would preserve group membership for
        /// free, but it is not safe: <c>InstanceGuid</c> is read-only and the only way to set a specific
        /// value is <c>NewInstanceGuid(Guid)</c>, whose purpose is to mint a fresh unique identity (paste,
        /// undo). The document keeps an identity-keyed object table (<c>GH_Document</c>'s object cache) that
        /// is populated at <c>AddObject</c> time, so rewiring identity afterwards relies on non-public
        /// bookkeeping being rebuilt, and a duplicated identity while both objects are alive is undefined.
        /// Reconciling the group through its public surface (<c>ObjectIDs</c>, <c>AddObject</c>,
        /// <c>RemoveObject</c>) is explicit, local and verifiable, and it leaves Grasshopper's own identity
        /// invariant intact.
        /// </para>
        /// <para>
        /// Groups are the only persisted reference to a component's identity that Grasshopper keeps outside
        /// the wires themselves: a reflection sweep of the loaded Rhino 8 Grasshopper assembly
        /// (<c>Grasshopper 8.35.26251</c>) found <c>GH_Group.ObjectIDs</c> as the only persisted
        /// <c>Guid</c> collection naming document objects - everything else is either the document's own
        /// object list/cache (managed by AddObject/RemoveObject), a per-attribute back-reference that is
        /// rebuilt with the attribute, a wire-level parameter reference (managed by re-adding sources), or
        /// transient session state.
        /// </para>
        /// </summary>
        /// <returns>The number of groups whose membership was moved.</returns>
        public static int ReconcileGroupMembership(GH_Document gH_Document, GH_SAMComponent gH_SAMComponent_Old, GH_SAMComponent gH_SAMComponent_New, out Log log)
        {
            log = new Log();

            if (gH_Document == null || gH_SAMComponent_Old == null || gH_SAMComponent_New == null)
            {
                return 0;
            }

            Guid instanceGuid_Old = gH_SAMComponent_Old.InstanceGuid;
            Guid instanceGuid_New = gH_SAMComponent_New.InstanceGuid;

            if (instanceGuid_Old == Guid.Empty || instanceGuid_New == Guid.Empty || instanceGuid_Old == instanceGuid_New)
            {
                return 0;
            }

            IList<IGH_DocumentObject> gH_DocumentObjects = gH_Document.Objects;
            if (gH_DocumentObjects == null || gH_DocumentObjects.Count == 0)
            {
                return 0;
            }

            int result = 0;

            foreach (IGH_DocumentObject gH_DocumentObject in gH_DocumentObjects)
            {
                if (!(gH_DocumentObject is GH_Group gH_Group))
                {
                    continue;
                }

                List<Guid> objectIDs = gH_Group.ObjectIDs;
                if (objectIDs == null || !objectIDs.Contains(instanceGuid_Old))
                {
                    continue;
                }

                try
                {
                    gH_Group.RemoveObject(instanceGuid_Old);
                    gH_Group.AddObject(instanceGuid_New);
                    result++;
                }
                catch (Exception ex)
                {
                    log.Add(new LogRecord("  Could not restore group membership for {0} in group '{1}': {2}",
                        LogRecordType.Warning, gH_SAMComponent_New.Name, gH_Group.NickName, ex.Message));
                }
            }

            if (result != 0)
            {
                log.Add(new LogRecord("Group membership preserved for {0} ({1} group(s)).",
                    LogRecordType.Message, gH_SAMComponent_New.Name, result));
            }

            return result;
        }

        private static ManualReconnectionIssue CreateManualReconnectionIssue(GH_SAMComponent gH_SAMComponent, List<ManualReconnectionWire> manualReconnectionWires, bool replacementFailed)
        {
            if (gH_SAMComponent == null)
            {
                return null;
            }

            System.Drawing.PointF pivot = gH_SAMComponent.Attributes?.Pivot ?? System.Drawing.PointF.Empty;

            ManualReconnectionIssue result = new ManualReconnectionIssue
            {
                ComponentInstanceGuid = gH_SAMComponent.InstanceGuid,
                ComponentName = gH_SAMComponent.Name,
                ComponentNickName = gH_SAMComponent.NickName,
                PivotX = pivot.X,
                PivotY = pivot.Y,
                ReplacementFailed = replacementFailed
            };

            if (manualReconnectionWires != null)
            {
                result.Wires.AddRange(manualReconnectionWires);
            }

            return result;
        }

        public static void SortManualReconnectionIssues(List<ManualReconnectionIssue> manualReconnectionIssues)
        {
            if (manualReconnectionIssues == null)
            {
                return;
            }

            manualReconnectionIssues.Sort((x, y) =>
            {
                if (x == null || y == null)
                {
                    return x == null ? (y == null ? 0 : 1) : -1;
                }

                int comparison = x.PivotY.CompareTo(y.PivotY);
                if (comparison != 0)
                {
                    return comparison;
                }

                comparison = x.PivotX.CompareTo(y.PivotX);
                if (comparison != 0)
                {
                    return comparison;
                }

                comparison = string.Compare(x.ComponentName, y.ComponentName, StringComparison.Ordinal);
                if (comparison != 0)
                {
                    return comparison;
                }

                return x.ComponentInstanceGuid.CompareTo(y.ComponentInstanceGuid);
            });
        }

        public static List<GH_SAMComponent> PreviewUpdateComponents(GH_Document gH_Document, out Log log)
        {
            log = new Log();

            if (gH_Document == null)
            {
                return null;
            }

            IList<IGH_DocumentObject> gH_DocumentObjects = gH_Document.Objects;
            if (gH_DocumentObjects == null || gH_DocumentObjects.Count == 0)
            {
                log.Add(new LogRecord("No components found in document.", LogRecordType.Message));
                return null;
            }

            List<GH_SAMComponent> gH_SAMComponents = new List<GH_SAMComponent>();
            foreach (IGH_DocumentObject gH_DocumentObject in gH_DocumentObjects)
            {
                if (!(gH_DocumentObject is GH_SAMComponent gH_SAMComponent))
                {
                    continue;
                }

                if (gH_SAMComponent.Obsolete)
                {
                    gH_SAMComponents.Add(gH_SAMComponent);
                }
            }

            if (gH_SAMComponents.Count == 0)
            {
                log.Add(new LogRecord("No obsolete components found.", LogRecordType.Message));
                return null;
            }

            return PreviewUpdateComponents(gH_SAMComponents, gH_Document, out log);
        }

        public static List<GH_SAMComponent> PreviewUpdateComponents(IEnumerable<GH_SAMComponent> gH_SAMComponents, out Log log)
        {
            log = new Log();
            if (gH_SAMComponents == null || !gH_SAMComponents.Any())
            {
                return null;
            }

            GH_Document gH_Document = gH_SAMComponents.FirstOrDefault()?.OnPingDocument();
            if (gH_Document == null)
            {
                log.Add(new LogRecord("Could not access document.", LogRecordType.Error));
                return null;
            }

            return PreviewUpdateComponents(gH_SAMComponents, gH_Document, out log);
        }

        private static List<GH_SAMComponent> PreviewUpdateComponents(IEnumerable<GH_SAMComponent> gH_SAMComponents, GH_Document gH_Document, out Log log)
        {
            log = new Log();

            if (gH_SAMComponents == null || !gH_SAMComponents.Any())
            {
                return null;
            }

            if (gH_Document == null)
            {
                log.Add(new LogRecord("Document is null.", LogRecordType.Error));
                return null;
            }

            GH_SAMComponent[] components = gH_SAMComponents.ToArray();

            log.Add(new LogRecord("--- DRY RUN --- Would update {0} component(s):", LogRecordType.Message, components.Length));

            int connectionsAtRisk = 0;
            List<GH_SAMComponent> result = new List<GH_SAMComponent>();

            foreach (GH_SAMComponent gH_SAMComponent in components)
            {
                if (gH_SAMComponent == null || !gH_SAMComponent.Obsolete)
                {
                    continue;
                }

                ObsoleteSeverity severity = gH_SAMComponent.ObsoleteSeverity;
                string severityLabel = severity == ObsoleteSeverity.Breaking ? " [BREAKING]" :
                                       severity == ObsoleteSeverity.Advisory ? " [patch]" : "";

                log.Add(new LogRecord("  {0}: {1} → {2}{3}",
                    LogRecordType.Message, gH_SAMComponent.Name, gH_SAMComponent.ComponentVersion,
                    gH_SAMComponent.LatestComponentVersion, severityLabel));

                result.Add(gH_SAMComponent);

                List<IGH_Param> outputParams = gH_SAMComponent.Params?.Output;
                List<IGH_Param> inputParams = gH_SAMComponent.Params?.Input;

                if (outputParams != null)
                {
                    foreach (IGH_Param param in outputParams)
                    {
                        if (param != null && param.Recipients != null && param.Recipients.Count > 0)
                        {
                            connectionsAtRisk += param.Recipients.Count;
                        }
                    }
                }

                if (inputParams != null)
                {
                    foreach (IGH_Param param in inputParams)
                    {
                        if (param != null && param.Sources != null && param.Sources.Count > 0)
                        {
                            connectionsAtRisk += param.Sources.Count;
                        }
                    }
                }
            }

            if (connectionsAtRisk > 0)
            {
                log.Add(new LogRecord("  {0} total connection(s) will be rewired.", LogRecordType.Message, connectionsAtRisk));
            }

            log.Add(new LogRecord("--- DRY RUN COMPLETE --- No changes were made to the document.", LogRecordType.Message));

            return result;
        }

        public static List<GH_SAMComponent> DuplicateComponents(
            IEnumerable<GH_SAMComponent> gH_SAMComponents,
            GH_Document gH_Document,
            out Log log,
            out List<GH_SAMComponent> oldComponents)
        {
            log = new Log();
            oldComponents = new List<GH_SAMComponent>();

            if (gH_Document == null)
            {
                log.Add(new LogRecord("Document is null.", LogRecordType.Error));
                return null;
            }

            List<GH_SAMComponent> targets;
            if (gH_SAMComponents != null && gH_SAMComponents.Any())
            {
                targets = gH_SAMComponents.ToList();
            }
            else
            {
                targets = new List<GH_SAMComponent>();
                IList<IGH_DocumentObject> gH_DocumentObjects = gH_Document.Objects;
                if (gH_DocumentObjects != null)
                {
                    foreach (IGH_DocumentObject gH_DocumentObject in gH_DocumentObjects)
                    {
                        if (gH_DocumentObject is GH_SAMComponent gH_SAMComponent && gH_SAMComponent.Obsolete)
                        {
                            targets.Add(gH_SAMComponent);
                        }
                    }
                }
            }

            if (targets.Count == 0)
            {
                log.Add(new LogRecord("No obsolete components found.", LogRecordType.Message));
                return new List<GH_SAMComponent>();
            }

            log.Add(new LogRecord("Update started. Found {0} obsolete component(s).", LogRecordType.Message, targets.Count));

            List<GH_SAMComponent> result = new List<GH_SAMComponent>();

            foreach (GH_SAMComponent gH_SAMComponent in targets)
            {
                if (gH_SAMComponent == null)
                {
                    continue;
                }

                if (!gH_SAMComponent.Obsolete)
                {
                    continue;
                }

                try
                {
                    GH_SAMComponent gH_SAMComponent_New = DuplicateComponent(gH_SAMComponent, out Log log_Temp);
                    if (gH_SAMComponent_New == null)
                    {
                        log.Add(new LogRecord("  Failed to update {0}", LogRecordType.Error, gH_SAMComponent.Name));
                        if (log_Temp != null)
                        {
                            log.AddRange(log_Temp);
                        }
                        continue;
                    }

                    result.Add(gH_SAMComponent_New);
                    oldComponents.Add(gH_SAMComponent);

                    if (log_Temp != null)
                    {
                        log.AddRange(log_Temp);
                    }
                }
                catch (Exception ex)
                {
                    log.Add(new LogRecord("  Failed to update {0}: {1}", LogRecordType.Error, gH_SAMComponent.Name, ex.Message));
                }
            }

            log.Add(new LogRecord("Components duplicated: {0} new created, {1} pending removal.",
                LogRecordType.Message, result.Count, oldComponents.Count));

            return result;
        }
    }
}
