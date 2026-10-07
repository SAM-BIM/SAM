// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Globalization;
using System.Text;
using System.Text.Json;

namespace SAM.Tests.GenOptOracle
{
    /// <summary>One continuous GenOpt parameter. Values are written verbatim into Command.txt.</summary>
    public sealed class ParameterSpec
    {
        public string Name { get; set; } = "x1";

        /// <summary>Text written after "Ini = ". GenOpt parses it with java.io.StreamTokenizer.</summary>
        public string Ini { get; set; } = "0";

        /// <summary>Text after "Min = ", or null to omit (GenOpt then uses SMALL).</summary>
        public string? Min { get; set; }

        /// <summary>Text after "Max = ", or null to omit (GenOpt then uses BIG).</summary>
        public string? Max { get; set; }

        public string Step { get; set; } = "1";
    }

    /// <summary>A complete synthetic GenOpt experiment.</summary>
    public sealed class GenOptCase
    {
        public string Name { get; set; } = string.Empty;

        public string Purpose { get; set; } = string.Empty;

        public List<ParameterSpec> Parameters { get; set; } = new List<ParameterSpec>();

        public int MaxIte { get; set; } = 2000;

        public int MaxEqualResults { get; set; } = 100;

        /// <summary>Algorithm section lines after "Main = ...;", e.g. "MeshSizeDivider = 2;".</summary>
        public string Algorithm { get; set; } = "GPSHookeJeeves";

        public List<string> AlgorithmKeywords { get; set; } = new List<string>
        {
            "MeshSizeDivider = 2;",
            "InitialMeshSizeExponent = 0;",
            "MeshSizeExponentIncrement = 1;",
            "NumberOfStepReduction = 4;",
        };

        public FunctionSpec Function { get; set; } = new FunctionSpec();

        /// <summary>
        /// Writes Config.ini, Command.txt, config.txt, Template.txt and fakesim.json into
        /// <paramref name="directory"/>. The layout mirrors the files Tas Generic Optimisation writes
        /// (Template/Variables/Error/Output/config/Command), with the fake simulator as the command.
        /// </summary>
        public void Write(string directory, string simulatorExe)
        {
            Directory.CreateDirectory(directory);

            StringBuilder ini = new StringBuilder();
            ini.AppendLine("Simulation {");
            ini.AppendLine("Files {");
            ini.AppendLine("Template {").AppendLine("File1 = Template.txt;").AppendLine("}");
            ini.AppendLine("Input {").AppendLine("File1 = " + FakeSimulator.InputFileName + ";").AppendLine("}");
            ini.AppendLine("Log {").AppendLine("File1 = " + FakeSimulator.LogFileName + ";").AppendLine("}");
            ini.AppendLine("Output {").AppendLine("File1 = " + FakeSimulator.OutputFileName + ";").AppendLine("}");
            ini.AppendLine("Configuration {").AppendLine("File1 = config.txt;").AppendLine("}");
            ini.AppendLine("}");
            ini.AppendLine();
            ini.AppendLine("ObjectiveFunctionLocation {");
            for (int i = 0; i < Function.Outputs.Count; i++)
            {
                string index = (i + 1).ToString(CultureInfo.InvariantCulture);
                ini.AppendLine("Name" + index + " = " + Function.Outputs[i] + ";");
                ini.AppendLine("Delimiter" + index + " = \"" + Function.Outputs[i] + "::\";");
            }

            ini.AppendLine("}");
            ini.AppendLine();
            ini.AppendLine("}");
            ini.AppendLine();
            ini.AppendLine("Optimization {");
            ini.AppendLine("Files {");
            ini.AppendLine("Command {").AppendLine("File1 = Command.txt;").AppendLine("}");
            ini.AppendLine("}");
            ini.AppendLine("}");
            File.WriteAllText(Path.Combine(directory, "Config.ini"), ini.ToString());

            StringBuilder command = new StringBuilder();
            command.AppendLine("Vary {");
            foreach (ParameterSpec parameter in Parameters)
            {
                command.AppendLine("Parameter {");
                command.AppendLine("Name = " + parameter.Name + ";");
                if (parameter.Min is not null)
                {
                    command.AppendLine("Min = " + parameter.Min + ";");
                }

                command.AppendLine("Ini = " + parameter.Ini + ";");
                if (parameter.Max is not null)
                {
                    command.AppendLine("Max = " + parameter.Max + ";");
                }

                command.AppendLine("Step = " + parameter.Step + ";");
                command.AppendLine("}");
            }

            command.AppendLine("}");
            command.AppendLine();
            command.AppendLine("OptimizationSettings {");
            command.AppendLine("MaxIte = " + MaxIte.ToString(CultureInfo.InvariantCulture) + ";");
            command.AppendLine("MaxEqualResults = " + MaxEqualResults.ToString(CultureInfo.InvariantCulture) + ";");
            command.AppendLine("WriteStepNumber = false;");
            command.AppendLine("UnitsOfExecution = 1;");
            command.AppendLine("}");
            command.AppendLine();
            command.AppendLine("Algorithm {");
            command.AppendLine("Main = " + Algorithm + ";");
            foreach (string keyword in AlgorithmKeywords)
            {
                command.AppendLine(keyword);
            }

            command.AppendLine("}");
            File.WriteAllText(Path.Combine(directory, "Command.txt"), command.ToString());

            // GenOpt's Runtime.exec(String) splits the command on whitespace, so the simulator path
            // must not contain spaces. Backslashes are doubled inside the quoted GenOpt string.
            if (simulatorExe.Contains(' '))
            {
                throw new InvalidOperationException("The simulator path must not contain spaces: " + simulatorExe);
            }

            StringBuilder config = new StringBuilder();
            config.AppendLine("SimulationError {").AppendLine("ErrorMessage = \"Error\";").AppendLine("}");
            config.AppendLine("IO {").AppendLine("NumberFormat = Double;").AppendLine("}");
            config.AppendLine("SimulationStart {");
            config.AppendLine("Command = \"" + simulatorExe.Replace("\\", "\\\\") + " sim\";");
            config.AppendLine("WriteInputFileExtension = true;");
            config.AppendLine("}");
            File.WriteAllText(Path.Combine(directory, "config.txt"), config.ToString());

            StringBuilder template = new StringBuilder();
            foreach (ParameterSpec parameter in Parameters)
            {
                template.AppendLine(parameter.Name + "=%" + parameter.Name + "%");
            }

            File.WriteAllText(Path.Combine(directory, "Template.txt"), template.ToString());

            File.WriteAllText(Path.Combine(directory, FakeSimulator.SpecFileName), JsonSerializer.Serialize(Function, Json.Options));
        }
    }
}
