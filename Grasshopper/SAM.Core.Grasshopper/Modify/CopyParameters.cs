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
        public class ParamConnection
        {
            public GH_ParameterSide Side;
            public string ParamName;
            public List<IGH_Param> ConnectedParams = new List<IGH_Param>();
        }

        public static void CopyParameters(GH_ParameterSide parameterSide, GH_SAMComponent gH_SAMComponent_From, GH_SAMComponent gH_SAMComponent_To)
        {
            if (gH_SAMComponent_From == null || gH_SAMComponent_To == null)
            {
                return;
            }

            List<IGH_Param> gH_Params;

            Dictionary<string, IGH_Param> dictionary_Old = new Dictionary<string, IGH_Param>();
            gH_Params = parameterSide == GH_ParameterSide.Output ? gH_SAMComponent_From.Params.Output : gH_SAMComponent_From.Params.Input;
            if (gH_Params != null)
            {
                foreach (IGH_Param gH_Param in gH_Params)
                {
                    if (gH_Param == null)
                    {
                        continue;
                    }
                    dictionary_Old[gH_Param.Name] = gH_Param;
                }
            }

            Dictionary<string, IGH_Param> dictionary_New = new Dictionary<string, IGH_Param>();
            gH_Params = parameterSide == GH_ParameterSide.Output ? gH_SAMComponent_To.Params.Output : gH_SAMComponent_To.Params.Input;

            if (gH_SAMComponent_To is GH_SAMVariableOutputParameterComponent gH_SAMVariableOutputParameterComponent && gH_Params != null)
            {
                List<IGH_Param> gH_Params_Snapshot = new List<IGH_Param>(gH_Params);
                for (int i = gH_Params_Snapshot.Count - 1; i >= 0; i--)
                {
                    if (gH_SAMVariableOutputParameterComponent.CanInsertParameter(parameterSide, i))
                    {
                        gH_Params.Add(gH_SAMVariableOutputParameterComponent.CreateParameter(parameterSide, i));
                    }
                }
            }

            if (gH_Params != null)
            {
                foreach (IGH_Param gH_Param in gH_Params)
                {
                    if (gH_Param == null)
                    {
                        continue;
                    }
                    dictionary_New[gH_Param.Name] = gH_Param;
                }
            }

            foreach (KeyValuePair<string, IGH_Param> keyValuePair in dictionary_Old)
            {
                IEnumerable<IGH_Param> gH_Params_Connect = parameterSide == GH_ParameterSide.Output ? keyValuePair.Value.Recipients : keyValuePair.Value.Sources;
                if (gH_Params_Connect == null || !gH_Params_Connect.Any())
                {
                    continue;
                }

                if (!dictionary_New.TryGetValue(keyValuePair.Key, out IGH_Param gH_Param_New))
                {
                    continue;
                }

                IGH_Param[] connectedParams = gH_Params_Connect.ToArray();

                foreach (IGH_Param gH_Param in connectedParams)
                {
                    if (parameterSide == GH_ParameterSide.Output)
                    {
                        gH_Param.AddSource(gH_Param_New);
                    }
                    else
                    {
                        gH_Param_New.AddSource(gH_Param);
                    }
                }
            }

            if (gH_SAMComponent_To is GH_SAMVariableOutputParameterComponent gH_SAMVariableOutputParameterComponent2)
            {
                GH_ComponentParamServer gH_ComponentParamServer = gH_SAMComponent_To.Params;

                gH_Params = parameterSide == GH_ParameterSide.Output ? gH_ComponentParamServer.Output : gH_ComponentParamServer.Input;
                if (gH_Params != null)
                {
                    for (int i = gH_Params.Count - 1; i >= 0; i--)
                    {
                        if (gH_Params[i] == null)
                        {
                            continue;
                        }

                        if (dictionary_Old.ContainsKey(gH_Params[i].Name))
                        {
                            continue;
                        }

                        if (gH_SAMVariableOutputParameterComponent2.CanRemoveParameter(parameterSide, i))
                        {
                            if (parameterSide == GH_ParameterSide.Output)
                            {
                                gH_ComponentParamServer.UnregisterOutputParameter(gH_Params[i]);
                            }
                            else
                            {
                                gH_ComponentParamServer.UnregisterInputParameter(gH_Params[i]);
                            }
                        }
                    }
                }
            }
        }

        public static List<ParamConnection> CaptureConnections(GH_SAMComponent gH_SAMComponent)
        {
            List<ParamConnection> result = new List<ParamConnection>();

            if (gH_SAMComponent == null)
            {
                return result;
            }

            List<IGH_Param> outputParams = gH_SAMComponent.Params?.Output;
            if (outputParams != null)
            {
                foreach (IGH_Param param in outputParams)
                {
                    if (param == null || param.Recipients == null || param.Recipients.Count == 0)
                    {
                        continue;
                    }

                    result.Add(new ParamConnection
                    {
                        Side = GH_ParameterSide.Output,
                        ParamName = param.Name,
                        ConnectedParams = new List<IGH_Param>(param.Recipients)
                    });
                }
            }

            List<IGH_Param> inputParams = gH_SAMComponent.Params?.Input;
            if (inputParams != null)
            {
                foreach (IGH_Param param in inputParams)
                {
                    if (param == null || param.Sources == null || param.Sources.Count == 0)
                    {
                        continue;
                    }

                    result.Add(new ParamConnection
                    {
                        Side = GH_ParameterSide.Input,
                        ParamName = param.Name,
                        ConnectedParams = new List<IGH_Param>(param.Sources)
                    });
                }
            }

            return result;
        }

        public static void RestoreConnections(GH_SAMComponent gH_SAMComponent, List<ParamConnection> connections, out Log log)
        {
            log = new Log();

            if (gH_SAMComponent == null || connections == null || connections.Count == 0)
            {
                return;
            }

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
                Dictionary<string, IGH_Param> targetDict = connection.Side == GH_ParameterSide.Output ? newOutputParams : newInputParams;

                if (!targetDict.TryGetValue(connection.ParamName, out IGH_Param newParam))
                {
                    if (connection.ConnectedParams.Count > 0)
                    {
                        log.Add(new LogRecord("  Warning: parameter '{0}' not found in new version — {1} connection(s) dropped on component {2}",
                            LogRecordType.Warning, connection.ParamName, connection.ConnectedParams.Count, gH_SAMComponent.Name));
                    }
                    continue;
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

        internal static void CopyPersistentData(IGH_Param oldParam, IGH_Param newParam)
        {
            if (oldParam == null || newParam == null)
            {
                return;
            }

            if (oldParam.GetType() != newParam.GetType())
            {
                return;
            }

            try
            {
                Type type = oldParam.GetType();
                while (type != null && type != typeof(object))
                {
                    if (type.IsGenericType && type.GetGenericTypeDefinition().FullName == "Grasshopper.Kernel.GH_PersistentParam`1")
                    {
                        System.Reflection.PropertyInfo persistentDataProp = type.GetProperty("PersistentData");
                        if (persistentDataProp == null)
                        {
                            break;
                        }

                        object persistentData = persistentDataProp.GetValue(oldParam);
                        if (persistentData == null)
                        {
                            break;
                        }

                        if (!(persistentData is System.Collections.IEnumerable enumerable))
                        {
                            break;
                        }

                        System.Reflection.MethodInfo addMethod = type.GetMethod("AddPersistentData");
                        if (addMethod == null)
                        {
                            break;
                        }

                        foreach (object item in enumerable)
                        {
                            addMethod.Invoke(newParam, new[] { item });
                        }
                        break;
                    }
                    type = type.BaseType;
                }
            }
            catch
            {
                // Best-effort: persistent data copy is non-critical
            }
        }
    }
}
