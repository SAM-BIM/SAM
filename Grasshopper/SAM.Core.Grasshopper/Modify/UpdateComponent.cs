// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using Grasshopper.Kernel;
using System;
using System.Collections.Generic;

namespace SAM.Core.Grasshopper
{
    public static partial class Modify
    {
        public static GH_SAMComponent DuplicateComponent(GH_SAMComponent gH_SAMComponent, out Log log)
        {
            return DuplicateComponent(gH_SAMComponent, out log, out List<ManualReconnectionWire> manualReconnectionWires);
        }

        public static GH_SAMComponent DuplicateComponent(GH_SAMComponent gH_SAMComponent, out Log log, out List<ManualReconnectionWire> manualReconnectionWires)
        {
            log = new Log();
            manualReconnectionWires = new List<ManualReconnectionWire>();

            if (gH_SAMComponent == null)
            {
                log.Add(new LogRecord("Component is null", LogRecordType.Error));
                return null;
            }

            GH_Document gH_Document = gH_SAMComponent?.OnPingDocument();
            if (gH_Document == null)
            {
                log.Add(new LogRecord("Could not access document or component", LogRecordType.Error));
                return null;
            }

            GH_SAMComponent result = null;
            try
            {
                result = Activator.CreateInstance(gH_SAMComponent.GetType()) as GH_SAMComponent;
            }
            catch (Exception ex)
            {
                log.Add(new LogRecord("Failed to create new component '{0}': {1}", LogRecordType.Error, gH_SAMComponent.Name, ex.Message));
                return null;
            }

            if (result == null)
            {
                log.Add(new LogRecord("Failed to create new component: {0}", LogRecordType.Error, gH_SAMComponent.Name));
                return null;
            }

            ObsoleteSeverity severity = gH_SAMComponent.ObsoleteSeverity;
            string severityLabel = severity == ObsoleteSeverity.Breaking ? "[breaking]" :
                                   severity == ObsoleteSeverity.Advisory ? "[patch]" : "";
            bool isVariable = result is IGH_VariableParameterComponent;
            log.Add(new LogRecord("Component {0} updating. Old: {1} New: {2} {3} variable={4}",
                LogRecordType.Message, gH_SAMComponent.Name, gH_SAMComponent.ComponentVersion,
                gH_SAMComponent.LatestComponentVersion, severityLabel, isVariable));

            List<ParamConnection> capturedConnections = CaptureConnections(gH_SAMComponent);
            List<ConnectionSnapshot> connectionSnapshots = CaptureConnectionSnapshots(gH_SAMComponent);

            System.Drawing.PointF pivot = gH_SAMComponent.Attributes?.Pivot ?? System.Drawing.PointF.Empty;

            bool add = gH_Document.AddObject(result, false);
            if (!add)
            {
                log.Add(new LogRecord("Could not add component to document: {0}", LogRecordType.Error, gH_SAMComponent.Name));
                return null;
            }

            //Published renames the replacement declares. Read from the replacement - the new code is the
            //authority on what the current names are - and validated here so an ambiguous declaration is
            //reported rather than guessed. See ParameterRenameMap.
            ParameterRenameMap parameterRenameMap = ParameterRenameMap.Create(result);
            foreach (string problem in parameterRenameMap.Problems)
            {
                log.Add(new LogRecord("  {0}: {1}", LogRecordType.Warning, gH_SAMComponent.Name, problem));
            }

            //Everything after AddObject is transactional: if any step throws, the half-built replacement is
            //removed again and the original component is left untouched in the document, rather than leaving
            //an orphan object behind for a person to find.
            try
            {
                //BEFORE the wires and before the persistent data, and that order is the whole of the fix.
                //
                //A GH_SAMVariableOutputParameterComponent registers only its DEFAULT parameters, so a brand new
                //instance does not carry the voluntary output somebody inserted and wired up, nor the voluntary
                //input they typed a value into. RestoreConnections matches by name and rightly refuses to guess,
                //so before this call every such wire was reported dropped and every such value was left behind -
                //on exactly the components most recently modernised to declarative Inputs/Outputs, which are the
                //ones with parameters to insert in the first place.
                //
                //It is also after AddObject, because a parameter is registered on a component that belongs to a
                //document, the same way the zoomable insert on the canvas does it.
                ExpandVariableParameters(gH_SAMComponent, result);

                RestoreConnections(result, capturedConnections, parameterRenameMap, out Log log_Restore);
                if (log_Restore != null)
                {
                    log.AddRange(log_Restore);
                }

                CopyPersistentDataFromComponent(gH_SAMComponent, result, parameterRenameMap, out Log log_Persistent);
                if (log_Persistent != null)
                {
                    log.AddRange(log_Persistent);
                }

                result.Attributes.Pivot = pivot;
            }
            catch (Exception ex)
            {
                log.Add(new LogRecord("  Failed to complete replacement of {0}: {1}. The original component was left in place.",
                    LogRecordType.Error, gH_SAMComponent.Name, ex.Message));

                try
                {
                    gH_Document.RemoveObject(result, false);
                }
                catch
                {
                    // Best-effort cleanup of the half-built replacement.
                }

                return null;
            }

            manualReconnectionWires = Query.MissingConnections(result, connectionSnapshots, parameterRenameMap);

            return result;
        }

        public static GH_SAMComponent DuplicateComponent(GH_SAMComponent gH_SAMComponent)
        {
            return DuplicateComponent(gH_SAMComponent, out Log log);
        }
    }
}
