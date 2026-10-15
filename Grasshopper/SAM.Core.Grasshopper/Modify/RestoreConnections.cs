// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using Grasshopper.Kernel;
using System;
using System.Collections.Generic;

namespace SAM.Core.Grasshopper
{
    public static partial class Modify
    {
        public static void RestoreConnections(GH_SAMComponent gH_SAMComponent, List<ParamConnection> connections, out Log log)
        {
            RestoreConnections(gH_SAMComponent, connections, ParameterRenameMap.Empty, out log);
        }

        /// <summary>
        /// Reattaches the connections captured from the component being replaced, resolving a published
        /// rename through <paramref name="parameterRenameMap"/> so a wire on a legacy name lands on the
        /// parameter that now carries it.
        /// <para>
        /// A legacy name is only redirected when it resolves to exactly one canonical name AND the target
        /// parameter has the same runtime type as the old one. An ambiguous declaration, or a target whose
        /// type changed, is refused and reported: leaving the wire visibly dropped — as it is today — is
        /// safer than silently attaching it to a parameter that means something else.
        /// </para>
        /// </summary>
        public static void RestoreConnections(GH_SAMComponent gH_SAMComponent, List<ParamConnection> connections, ParameterRenameMap parameterRenameMap, out Log log)
        {
            log = new Log();

            if (gH_SAMComponent == null || connections == null || connections.Count == 0)
            {
                return;
            }

            parameterRenameMap = parameterRenameMap ?? ParameterRenameMap.Empty;

            List<IGH_Param> outputParams = gH_SAMComponent.Params?.Output;
            List<IGH_Param> inputParams = gH_SAMComponent.Params?.Input;

            Dictionary<string, IGH_Param> newOutputParams = new Dictionary<string, IGH_Param>();
            Dictionary<string, IGH_Param> newInputParams = new Dictionary<string, IGH_Param>();

            if (outputParams != null)
            {
                foreach (IGH_Param param in outputParams)
                {
                    if (param != null)
                    {
                        newOutputParams[param.Name] = param;
                    }
                }
            }

            if (inputParams != null)
            {
                foreach (IGH_Param param in inputParams)
                {
                    if (param != null)
                    {
                        newInputParams[param.Name] = param;
                    }
                }
            }

            foreach (ParamConnection connection in connections)
            {
                string sideLabel = connection.Side == GH_ParameterSide.Output ? "output" : "input";

                if (!parameterRenameMap.TryResolve(connection.Side, connection.ParamName, out string targetName, out string problem))
                {
                    if (connection.ConnectedParams.Count > 0)
                    {
                        log.Add(new LogRecord("  NOT MIGRATED: {0} {1} '{2}' — {3} {4} left unconnected.",
                            LogRecordType.Warning, gH_SAMComponent.Name, sideLabel, connection.ParamName, problem,
                            connection.ConnectedParams.Count == 1 ? "connection" : "connections"));
                    }

                    continue;
                }

                Dictionary<string, IGH_Param> targetDict = connection.Side == GH_ParameterSide.Output ? newOutputParams : newInputParams;

                if (!targetDict.TryGetValue(targetName, out IGH_Param newParam))
                {
                    if (connection.ConnectedParams.Count > 0)
                    {
                        log.Add(new LogRecord("  DROPPED: {0} {1} '{2}' not in new version ({3} {4} lost). Manual reconnect needed.",
                            LogRecordType.Warning, gH_SAMComponent.Name, sideLabel, connection.ParamName,
                            connection.ConnectedParams.Count, connection.ConnectedParams.Count == 1 ? "connection" : "connections"));

                        foreach (IGH_Param cp in connection.ConnectedParams)
                        {
                            if (cp != null)
                            {
                                string dest = "?";
                                try { dest = cp.Attributes?.ToString() ?? cp.Name; } catch { dest = cp.Name; }
                                log.Add(new LogRecord("    was connected to: {0}", LogRecordType.Warning, dest));
                            }
                        }
                    }
                    continue;
                }

                bool redirected = !string.Equals(targetName, connection.ParamName, StringComparison.Ordinal);

                if (redirected && !CompatibleParameterTypes(connection.ParamTypeName, newParam))
                {
                    if (connection.ConnectedParams.Count > 0)
                    {
                        log.Add(new LogRecord("  NOT MIGRATED: {0} {1} '{2}' was renamed to '{3}', but the parameter type changed from {4} to {5}; {6} {7} left unconnected.",
                            LogRecordType.Warning, gH_SAMComponent.Name, sideLabel, connection.ParamName, targetName,
                            connection.ParamTypeName ?? "?", newParam.GetType().Name,
                            connection.ConnectedParams.Count, connection.ConnectedParams.Count == 1 ? "connection" : "connections"));
                    }

                    continue;
                }

                if (redirected && connection.ConnectedParams.Count > 0)
                {
                    log.Add(new LogRecord("{0} migrated by renamed {1} '{2}' → '{3}' ({4} {5} restored).",
                        LogRecordType.Message, gH_SAMComponent.Name, sideLabel, connection.ParamName, targetName,
                        connection.ConnectedParams.Count, connection.ConnectedParams.Count == 1 ? "connection" : "connections"));
                }

                foreach (IGH_Param connectedParam in connection.ConnectedParams)
                {
                    if (connectedParam == null)
                    {
                        continue;
                    }

                    try
                    {
                        if (connection.Side == GH_ParameterSide.Output)
                        {
                            connectedParam.AddSource(newParam);
                        }
                        else
                        {
                            newParam.AddSource(connectedParam);
                        }
                    }
                    catch (Exception ex)
                    {
                        log.Add(new LogRecord("  Warning: failed to reconnect '{0}' → '{1}': {2}",
                            LogRecordType.Warning, connection.ParamName,
                            connectedParam.Name ?? "(unnamed)", ex.Message));
                    }
                }
            }
        }

        /// <summary>
        /// The runtime type the parameter had before the rename must equal the one it has now. A redirect
        /// that crosses a parameter type would be a different wire, so it is refused rather than made.
        /// </summary>
        private static bool CompatibleParameterTypes(string paramTypeName, IGH_Param newParam)
        {
            if (string.IsNullOrEmpty(paramTypeName) || newParam == null)
            {
                return true;
            }

            return string.Equals(paramTypeName, newParam.GetType().Name, StringComparison.Ordinal);
        }
    }
}
