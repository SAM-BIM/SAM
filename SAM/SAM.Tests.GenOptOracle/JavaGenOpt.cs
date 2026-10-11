// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Diagnostics;
using System.Globalization;

namespace SAM.Tests.GenOptOracle
{
    /// <summary>Runs a locally installed Java GenOpt on a prepared case directory.</summary>
    internal sealed class JavaGenOpt
    {
        public JavaGenOpt(string javaExe, string genOptJar, string userHome)
        {
            JavaExe = javaExe;
            GenOptJar = genOptJar;
            UserHome = userHome;
        }

        public string JavaExe { get; }

        public string GenOptJar { get; }

        /// <summary>
        /// Passed as -Duser.home so GenOpt writes its preference file (.genopt3.1.1\properties.txt)
        /// into the oracle workspace instead of the real user profile.
        /// </summary>
        public string UserHome { get; }

        public JavaRunResult Run(string caseDirectory, TimeSpan timeout)
        {
            Directory.CreateDirectory(UserHome);

            ProcessStartInfo startInfo = new ProcessStartInfo(JavaExe)
            {
                WorkingDirectory = caseDirectory,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };
            startInfo.ArgumentList.Add("-Duser.home=" + UserHome);
            startInfo.ArgumentList.Add("-classpath");
            startInfo.ArgumentList.Add(GenOptJar);
            startInfo.ArgumentList.Add("genopt.GenOpt");
            startInfo.ArgumentList.Add(Path.Combine(caseDirectory, "Config.ini"));

            using Process process = Process.Start(startInfo) ?? throw new InvalidOperationException("Java did not start.");
            Task<string> stdout = process.StandardOutput.ReadToEndAsync();
            Task<string> stderr = process.StandardError.ReadToEndAsync();
            if (!process.WaitForExit((int)timeout.TotalMilliseconds))
            {
                process.Kill(entireProcessTree: true);
                throw new TimeoutException("GenOpt did not finish within " + timeout.TotalSeconds.ToString(CultureInfo.InvariantCulture) + " s in " + caseDirectory);
            }

            return new JavaRunResult(process.ExitCode, stdout.Result, stderr.Result);
        }
    }

    internal sealed record JavaRunResult(int ExitCode, string StandardOutput, string StandardError);
}
