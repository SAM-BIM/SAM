// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical;
using SAM.Core;
using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace SAM.Tests
{
    /// <summary>
    /// Seeds <see cref="ActiveSetting"/> with the TM59 internal-condition text map before any test runs, so
    /// <c>TMOverheatingCalculator.Calculate_TM59</c> never sees a null <c>TextMap</c> and returns null in its
    /// place.
    /// <para>
    /// <b>Why this exists.</b> <c>Query.DefaultInternalConditionTextMap_TM59()</c> falls back to
    /// <c>Core.Query.ResourcesDirectory(Assembly.GetExecutingAssembly())</c>, which resolves a directory from
    /// <c>Assembly.CodeBase</c> - obsolete, and not reliably populated under the <c>dotnet test</c> host. On a
    /// developer machine with a real SAM install this is masked: a persisted <c>%APPDATA%\SAM\settings</c> file
    /// satisfies <c>ActiveSetting.Setting</c> before either fallback is ever reached. A clean CI runner has no
    /// such file, so every test that instantiates <c>TM59AssessmentCalculator</c> or
    /// <c>TMOverheatingCalculator</c> without setting <c>TextMap</c> explicitly gets a null <c>Calculate</c>
    /// result - which is exactly what broke <c>TM59AssessmentCalculatorTests</c>,
    /// <c>TMOverheatingCalculatorTests</c>, <c>VentilationStrategyTests</c>, <c>PartOResultAssociationTests</c>
    /// and <c>PartOIterationSliceTests</c> in a clean Release build, while passing on every machine that already
    /// had a settings file.
    /// </para>
    /// <para>
    /// <b>Fixed once, for the whole assembly, rather than in each test file.</b> None of the five files above
    /// set <c>TextMap</c> themselves - they were all written expecting the production default, and that is what
    /// this seeds. <c>AppContext.BaseDirectory</c> is used rather than <c>Assembly.CodeBase</c>: it is the one
    /// path .NET Core guarantees is both populated and correct under every hosting scenario, which is the
    /// property the code being routed around here does not have.
    /// </para>
    /// </summary>
    internal static class TM59TextMapModuleInitializer
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            string path = Path.Combine(AppContext.BaseDirectory, "Resources", "SAM_InternalConditionTextMap_TM59.JSON");
            if (!File.Exists(path))
            {
                //Left unset rather than thrown: a test that does not need TM59 must not fail here, and a test
                //that does will fail with a clear null-result assertion rather than a mysterious one from this
                //initializer.
                return;
            }

            TextMap textMap = Core.Create.IJSAMObject<TextMap>(File.ReadAllText(path));
            if (textMap == null)
            {
                return;
            }

            //Qualified: SAM.Core.ActiveSetting also exists, and DefaultInternalConditionTextMap_TM59 reads the
            //SAM.Analytical one specifically.
            Analytical.ActiveSetting.Setting?.SetValue(AnalyticalSettingParameter.InternalConditionTextMap_TM59, textMap);
        }
    }
}
