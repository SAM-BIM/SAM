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
            log = new Log();

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
            log.Add(new LogRecord("Component {0} updating. Old version: {1} New version: {2} {3}",
                LogRecordType.Message, gH_SAMComponent.Name, gH_SAMComponent.ComponentVersion,
                gH_SAMComponent.LatestComponentVersion, severityLabel));

            List<ParamConnection> capturedConnections = CaptureConnections(gH_SAMComponent);

            System.Drawing.PointF pivot = gH_SAMComponent.Attributes?.Pivot ?? System.Drawing.PointF.Empty;

            bool add = gH_Document.AddObject(result, false);
            if (!add)
            {
                log.Add(new LogRecord("Could not add component to document: {0}", LogRecordType.Error, gH_SAMComponent.Name));
                return null;
            }

            result.Attributes.Pivot = pivot;

            RestoreConnections(result, capturedConnections, out Log log_Restore);
            if (log_Restore != null)
            {
                log.AddRange(log_Restore);
            }

            CopyPersistentDataFromComponent(gH_SAMComponent, result);

            return result;
        }

        public static GH_SAMComponent DuplicateComponent(GH_SAMComponent gH_SAMComponent)
        {
            return DuplicateComponent(gH_SAMComponent, out Log log);
        }

        internal static void CopyPersistentDataFromComponent(GH_SAMComponent gH_SAMComponent_From, GH_SAMComponent gH_SAMComponent_To)
        {
            if (gH_SAMComponent_From == null || gH_SAMComponent_To == null)
            {
                return;
            }

            List<IGH_Param> oldInputs = gH_SAMComponent_From.Params?.Input;
            List<IGH_Param> newInputs = gH_SAMComponent_To.Params?.Input;

            if (oldInputs == null || newInputs == null)
            {
                return;
            }

            Dictionary<string, IGH_Param> newInputDict = new Dictionary<string, IGH_Param>();
            foreach (IGH_Param param in newInputs)
            {
                if (param != null)
                {
                    newInputDict[param.Name] = param;
                }
            }

            foreach (IGH_Param oldParam in oldInputs)
            {
                if (oldParam == null)
                {
                    continue;
                }

                if (!newInputDict.TryGetValue(oldParam.Name, out IGH_Param newParam))
                {
                    continue;
                }

                CopyPersistentData(oldParam, newParam);
            }
        }
    }
}
