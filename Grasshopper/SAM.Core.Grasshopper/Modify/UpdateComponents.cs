// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using Grasshopper.Kernel;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SAM.Core.Grasshopper
{
    public static partial class Modify
    {
        public static List<GH_SAMComponent> UpdateComponents(GH_Document gH_Document, out Log log)
        {
            log = new Log();

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

            return UpdateComponents(gH_SAMComponents, gH_Document, out log);
        }

        public static List<GH_SAMComponent> UpdateComponents(IEnumerable<GH_SAMComponent> gH_SAMComponents, out Log log)
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

            return UpdateComponents(gH_SAMComponents, gH_Document, out log);
        }

        private static List<GH_SAMComponent> UpdateComponents(IEnumerable<GH_SAMComponent> gH_SAMComponents, GH_Document gH_Document, out Log log)
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

            log.Add(new LogRecord("Update started. Found {0} obsolete component(s).", LogRecordType.Message, components.Length));

            int updated = 0;
            int failed = 0;
            int warnings = 0;
            List<GH_SAMComponent> result = new List<GH_SAMComponent>();

            try
            {
                gH_Document.UndoUtil.RecordEvent("Update Obsolete Components");
            }
            catch
            {
                // UndoUtil not available in all contexts; proceed without undo grouping
                log.Add(new LogRecord("Undo grouping not available — updates will be individually undoable.", LogRecordType.Warning));
                warnings++;
            }

            foreach (GH_SAMComponent gH_SAMComponent in components)
            {
                if (gH_SAMComponent == null)
                {
                    continue;
                }

                try
                {
                    GH_SAMComponent gH_SAMComponent_New = DuplicateComponent(gH_SAMComponent, out Log log_Temp);
                    if (gH_SAMComponent_New == null)
                    {
                        failed++;
                        if (log_Temp != null)
                        {
                            log.AddRange(log_Temp);
                        }
                        continue;
                    }

                    result.Add(gH_SAMComponent_New);
                    updated++;

                    if (log_Temp != null)
                    {
                        log.AddRange(log_Temp);
                    }
                }
                catch (Exception ex)
                {
                    failed++;
                    log.Add(new LogRecord("  Failed to update {0}: {1}", LogRecordType.Error, gH_SAMComponent.Name, ex.Message));
                }
            }

            log.Add(new LogRecord("Update completed: {0} updated, {1} failed, {2} warning(s).",
                LogRecordType.Message, updated, failed, warnings));

            if (result.Count == 0)
            {
                return null;
            }

            return result;
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
                if (gH_SAMComponent == null)
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
    }
}
