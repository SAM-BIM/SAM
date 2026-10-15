// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using Grasshopper.Kernel;
using System.Collections.Generic;

namespace SAM.Core.Grasshopper
{
    public static partial class Query
    {
        public static List<ManualReconnectionWire> MissingConnections(GH_Component gH_Component, IEnumerable<ConnectionSnapshot> connectionSnapshots)
        {
            return MissingConnections(gH_Component, connectionSnapshots, ParameterRenameMap.Empty);
        }

        /// <summary>
        /// Re-checks the connections captured before a replacement to see which were NOT restored.
        /// <para>
        /// A wire on a parameter that has been renamed appears "missing" only if the verification looks for
        /// the OLD name: it was in fact restored onto the parameter that now carries it. Resolving each
        /// snapshot through <paramref name="parameterRenameMap"/> is what keeps a successful rename from
        /// being reported to a person as a wire that needs reconnecting by hand.
        /// </para>
        /// </summary>
        public static List<ManualReconnectionWire> MissingConnections(GH_Component gH_Component, IEnumerable<ConnectionSnapshot> connectionSnapshots, ParameterRenameMap parameterRenameMap)
        {
            List<ManualReconnectionWire> result = new List<ManualReconnectionWire>();

            if (connectionSnapshots == null)
            {
                return result;
            }

            parameterRenameMap = parameterRenameMap ?? ParameterRenameMap.Empty;

            List<IGH_Param> inputParams = gH_Component?.Params?.Input;
            List<IGH_Param> outputParams = gH_Component?.Params?.Output;
            GH_Document gH_Document = gH_Component?.OnPingDocument();

            foreach (ConnectionSnapshot connectionSnapshot in connectionSnapshots)
            {
                if (connectionSnapshot == null)
                {
                    continue;
                }

                string sideLabel = connectionSnapshot.Side == GH_ParameterSide.Input ? "Input" : "Output";
                string reason = null;

                string paramName = connectionSnapshot.ParamName;
                if (parameterRenameMap.TryResolve(connectionSnapshot.Side, paramName, out string targetName, out _))
                {
                    paramName = targetName;
                }

                if (gH_Component == null)
                {
                    reason = "Replacement component is not available.";
                }
                else if (connectionSnapshot.PeerParam == null)
                {
                    reason = connectionSnapshot.Side == GH_ParameterSide.Input ?
                        "Source parameter is no longer available." :
                        "Recipient parameter is no longer available.";
                }
                else if (gH_Document != null && !InDocument(connectionSnapshot.PeerParam, gH_Document))
                {
                    reason = connectionSnapshot.Side == GH_ParameterSide.Input ?
                        string.Format("Source '{0}' is no longer available in the document.", connectionSnapshot.PeerComponentName) :
                        string.Format("Recipient '{0}' is no longer available in the document.", connectionSnapshot.PeerComponentName);
                }
                else
                {
                    List<IGH_Param> matchingParams = new List<IGH_Param>();
                    List<IGH_Param> newParams = connectionSnapshot.Side == GH_ParameterSide.Input ? inputParams : outputParams;
                    if (newParams != null)
                    {
                        foreach (IGH_Param newParam in newParams)
                        {
                            if (newParam != null && newParam.Name == paramName)
                            {
                                matchingParams.Add(newParam);
                            }
                        }
                    }

                    if (matchingParams.Count == 0)
                    {
                        reason = string.Format("{0} '{1}' does not exist on the updated component.", sideLabel, paramName);
                    }
                    else
                    {
                        bool restored = false;
                        foreach (IGH_Param matchingParam in matchingParams)
                        {
                            if (connectionSnapshot.Side == GH_ParameterSide.Input)
                            {
                                if (matchingParam.Sources != null && matchingParam.Sources.Contains(connectionSnapshot.PeerParam))
                                {
                                    restored = true;
                                    break;
                                }
                            }
                            else
                            {
                                if (connectionSnapshot.PeerParam.Sources != null && connectionSnapshot.PeerParam.Sources.Contains(matchingParam))
                                {
                                    restored = true;
                                    break;
                                }
                            }
                        }

                        if (!restored)
                        {
                            if (matchingParams.Count > 1)
                            {
                                reason = string.Format("Several parameters named '{0}' exist on the updated component — no unambiguous match.", paramName);
                            }
                            else
                            {
                                reason = string.Format("Wire between {0} '{1}' and '{2}' ({3}) could not be restored automatically.",
                                    sideLabel.ToLower(), paramName, connectionSnapshot.PeerParamName, connectionSnapshot.PeerComponentName);
                            }
                        }
                    }
                }

                if (reason != null)
                {
                    result.Add(new ManualReconnectionWire(connectionSnapshot, reason));
                }
            }

            return result;
        }

        private static bool InDocument(IGH_Param param, GH_Document gH_Document)
        {
            if (param == null || gH_Document == null)
            {
                return false;
            }

            IGH_DocumentObject documentObject = param.Attributes?.GetTopLevel?.DocObject;
            if (documentObject == null)
            {
                return false;
            }

            return documentObject.OnPingDocument() == gH_Document;
        }
    }
}
