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
            bool isVariable = result is IGH_VariableParameterComponent;
            log.Add(new LogRecord("Component {0} updating. Old: {1} New: {2} {3} variable={4}",
                LogRecordType.Message, gH_SAMComponent.Name, gH_SAMComponent.ComponentVersion,
                gH_SAMComponent.LatestComponentVersion, severityLabel, isVariable));

            List<ParamConnection> capturedConnections = CaptureConnections(gH_SAMComponent);

            System.Drawing.PointF pivot = gH_SAMComponent.Attributes?.Pivot ?? System.Drawing.PointF.Empty;

            bool add = gH_Document.AddObject(result, false);
            if (!add)
            {
                log.Add(new LogRecord("Could not add component to document: {0}", LogRecordType.Error, gH_SAMComponent.Name));
                return null;
            }

            ExpandVariableParameters(gH_SAMComponent, result);

            RestoreConnections(result, capturedConnections, out Log log_Restore);
            if (log_Restore != null)
            {
                log.AddRange(log_Restore);
            }

            CopyPersistentDataFromComponent(gH_SAMComponent, result);

            result.Attributes.Pivot = pivot;

            return result;
        }

        public static GH_SAMComponent DuplicateComponent(GH_SAMComponent gH_SAMComponent)
        {
            return DuplicateComponent(gH_SAMComponent, out Log log);
        }

        internal static void ExpandVariableParameters(GH_SAMComponent gH_SAMComponent_From, GH_SAMComponent gH_SAMComponent_To)
        {
            if (!(gH_SAMComponent_To is GH_SAMVariableOutputParameterComponent variableOutput))
            {
                return;
            }

            ExpandSide(GH_ParameterSide.Input, gH_SAMComponent_From, gH_SAMComponent_To, variableOutput);
            ExpandSide(GH_ParameterSide.Output, gH_SAMComponent_From, gH_SAMComponent_To, variableOutput);
        }

        private static void ExpandSide(GH_ParameterSide side, GH_SAMComponent from, GH_SAMComponent to, GH_SAMVariableOutputParameterComponent variableOutput)
        {
            List<IGH_Param> fromParams = side == GH_ParameterSide.Output ? from.Params?.Output : from.Params?.Input;
            List<IGH_Param> toParams = side == GH_ParameterSide.Output ? to.Params?.Output : to.Params?.Input;

            if (fromParams == null || toParams == null)
            {
                return;
            }

            HashSet<string> fromParamNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (IGH_Param param in fromParams)
            {
                if (param != null)
                {
                    fromParamNames.Add(param.Name);
                }
            }

            HashSet<string> toParamNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (IGH_Param param in toParams)
            {
                if (param != null)
                {
                    toParamNames.Add(param.Name);
                }
            }

            List<string> missingNames = new List<string>();
            foreach (string name in fromParamNames)
            {
                if (!toParamNames.Contains(name))
                {
                    missingNames.Add(name);
                }
            }

            if (missingNames.Count == 0)
            {
                return;
            }

            int attempts = 0;
            int maxAttempts = missingNames.Count * 3;
            while (missingNames.Count > 0 && attempts < maxAttempts)
            {
                attempts++;
                bool added = false;

                for (int i = toParams.Count - 1; i >= 0; i--)
                {
                    if (!variableOutput.CanInsertParameter(side, i))
                    {
                        continue;
                    }

                    IGH_Param newParam = null;
                    try
                    {
                        newParam = variableOutput.CreateParameter(side, i);
                    }
                    catch
                    {
                        continue;
                    }

                    if (newParam == null)
                    {
                        continue;
                    }

                    if (missingNames.Contains(newParam.Name))
                    {
                        toParams.Add(newParam);
                        missingNames.Remove(newParam.Name);
                        added = true;
                        break;
                    }
                }

                if (!added)
                {
                    break;
                }
            }
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
