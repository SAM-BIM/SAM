// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using Grasshopper.Kernel;
using System.Collections.Generic;

namespace SAM.Core.Grasshopper
{
    public static partial class Modify
    {
        internal static void CopyPersistentDataFromComponent(GH_SAMComponent gH_SAMComponent_From, GH_SAMComponent gH_SAMComponent_To)
        {
            CopyPersistentDataFromComponent(gH_SAMComponent_From, gH_SAMComponent_To, ParameterRenameMap.Empty, out Log _);
        }

        /// <summary>
        /// Carries the values a person typed into the inputs of the component being replaced onto its
        /// replacement, resolving a published rename through <paramref name="parameterRenameMap"/> so a
        /// typed-in value on a legacy input name lands on the parameter that now carries it.
        /// <para>
        /// A renamed input is only redirected when the old and new parameters have the same runtime type
        /// (<c>CopyPersistentData</c> already refuses a type change), and an ambiguous declaration is
        /// refused and reported. Values are never discarded silently.
        /// </para>
        /// </summary>
        internal static void CopyPersistentDataFromComponent(GH_SAMComponent gH_SAMComponent_From, GH_SAMComponent gH_SAMComponent_To, ParameterRenameMap parameterRenameMap, out Log log)
        {
            log = new Log();

            if (gH_SAMComponent_From == null || gH_SAMComponent_To == null)
            {
                return;
            }

            parameterRenameMap = parameterRenameMap ?? ParameterRenameMap.Empty;

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

                if (!parameterRenameMap.TryResolve(GH_ParameterSide.Input, oldParam.Name, out string targetName, out string problem))
                {
                    if (newInputDict.TryGetValue(oldParam.Name, out IGH_Param directParam))
                    {
                        CopyPersistentData(oldParam, directParam);
                    }
                    else
                    {
                        log.Add(new LogRecord("{0} input '{1}' could not be migrated: {2}", LogRecordType.Warning, gH_SAMComponent_To.Name, oldParam.Name, problem));
                    }

                    continue;
                }

                if (!newInputDict.TryGetValue(targetName, out IGH_Param newParam))
                {
                    continue;
                }

                bool redirected = !string.Equals(targetName, oldParam.Name, System.StringComparison.Ordinal);
                if (redirected && oldParam.GetType() != newParam.GetType())
                {
                    log.Add(new LogRecord("{0} input '{1}' was renamed to '{2}', but the parameter type changed from {3} to {4}; its value was not migrated.",
                        LogRecordType.Warning, gH_SAMComponent_To.Name, oldParam.Name, targetName, oldParam.GetType().Name, newParam.GetType().Name));
                    continue;
                }

                if (CopyPersistentData(oldParam, newParam) && redirected)
                {
                    log.Add(new LogRecord("{0} input migrated by renamed input '{1}' → '{2}'.", LogRecordType.Message, gH_SAMComponent_To.Name, oldParam.Name, targetName));
                }
            }
        }
    }
}
