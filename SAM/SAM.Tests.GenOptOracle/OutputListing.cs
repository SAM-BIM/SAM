// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Globalization;

namespace SAM.Tests.GenOptOracle
{
    /// <summary>One data row of GenOpt's OutputListingAll.txt / OutputListingMain.txt.</summary>
    public sealed class ListingRow
    {
        public int Simulation { get; set; }

        public int MainIteration { get; set; }

        /// <summary>0 for OutputListingMain rows (the column does not exist there).</summary>
        public int SubIteration { get; set; }

        public int StepNumber { get; set; }

        /// <summary>Objective/output values exactly as GenOpt printed them (Java Double.toString).</summary>
        public List<string> F { get; set; } = new List<string>();

        /// <summary>Parameter values exactly as GenOpt printed them (Java Double.toString).</summary>
        public List<string> X { get; set; } = new List<string>();

        public string Comment { get; set; } = string.Empty;
    }

    /// <summary>
    /// Parses the data part of an output listing. The LBNL header text and timestamps are not kept:
    /// only the column header and the data rows are, which is what the golden traces store.
    /// </summary>
    public sealed class OutputListing
    {
        public List<string> Columns { get; } = new List<string>();

        public List<ListingRow> Rows { get; } = new List<ListingRow>();

        /// <summary>Free text after the last data row (e.g. GoldenSection's "Result overview").</summary>
        public List<string> Footer { get; } = new List<string>();

        public static OutputListing Read(string path, int outputCount)
        {
            OutputListing listing = new OutputListing();
            string[] lines = File.ReadAllLines(path);
            int header = Array.FindIndex(lines, l => l.StartsWith("Simulation Number\t", StringComparison.Ordinal));
            if (header < 0)
            {
                throw new FormatException("No column header in " + path);
            }

            listing.Columns.AddRange(lines[header].Split('\t'));
            bool all = listing.Columns.Count > 2 && listing.Columns[2] == "Sub Iteration";
            int fixedColumns = all ? 4 : 3;
            int parameterCount = listing.Columns.Count - fixedColumns - outputCount;

            int i = header + 1;
            for (; i < lines.Length; i++)
            {
                string line = lines[i];
                if (line.Length == 0)
                {
                    break;
                }

                string[] cells = line.Split('\t');
                ListingRow row = new ListingRow
                {
                    Simulation = int.Parse(cells[0], CultureInfo.InvariantCulture),
                    MainIteration = int.Parse(cells[1], CultureInfo.InvariantCulture),
                    SubIteration = all ? int.Parse(cells[2], CultureInfo.InvariantCulture) : 0,
                    StepNumber = int.Parse(cells[fixedColumns - 1], CultureInfo.InvariantCulture),
                };
                for (int k = 0; k < outputCount; k++)
                {
                    row.F.Add(cells[fixedColumns + k]);
                }

                for (int k = 0; k < parameterCount; k++)
                {
                    row.X.Add(cells[fixedColumns + outputCount + k]);
                }

                row.Comment = cells.Length > fixedColumns + outputCount + parameterCount
                    ? cells[fixedColumns + outputCount + parameterCount]
                    : string.Empty;
                listing.Rows.Add(row);
            }

            for (; i < lines.Length; i++)
            {
                if (lines[i].Length > 0)
                {
                    listing.Footer.Add(lines[i]);
                }
            }

            return listing;
        }

        /// <summary>Parses a Java Double.toString value (e.g. "1.0E-5", "-0.0", "Infinity").</summary>
        public static double ParseJavaDouble(string text)
        {
            return text switch
            {
                "Infinity" => double.PositiveInfinity,
                "-Infinity" => double.NegativeInfinity,
                "NaN" => double.NaN,
                _ => double.Parse(text, NumberStyles.Float, CultureInfo.InvariantCulture),
            };
        }
    }
}
