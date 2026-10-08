// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace SAM.Core.Optimisation
{
    /// <summary>
    /// A JSON value of the definition text with the line and column it starts at, so that every diagnostic can point
    /// at its place. Numbers keep their exact text (parsed later with the runtime's correctly rounded parser), and an
    /// object keeps its properties in order, duplicates included, so the reader can report them.
    /// </summary>
    internal sealed class OptimisationJsonNode
    {
        private OptimisationJsonNode(OptimisationJsonKind kind, int line, int column)
        {
            Kind = kind;
            Line = line;
            Column = column;
        }

        public OptimisationJsonKind Kind { get; }

        public int Line { get; }

        public int Column { get; }

        /// <summary>The string's value, or the number's text exactly as written.</summary>
        public string Text { get; private set; }

        public bool Boolean { get; private set; }

        public List<OptimisationJsonProperty> Properties { get; } = new List<OptimisationJsonProperty>();

        public List<OptimisationJsonNode> Items { get; } = new List<OptimisationJsonNode>();

        /// <summary>"object", "array", "text", "number", "true/false" or "null", for messages.</summary>
        public string KindText
        {
            get
            {
                switch (Kind)
                {
                    case OptimisationJsonKind.Object:
                        return "an object";
                    case OptimisationJsonKind.Array:
                        return "a list";
                    case OptimisationJsonKind.String:
                        return "text";
                    case OptimisationJsonKind.Number:
                        return "a number";
                    case OptimisationJsonKind.Boolean:
                        return "true/false";
                    default:
                        return "null";
                }
            }
        }

        /// <summary>
        /// Parses <paramref name="text"/>. Comments and trailing commas are tolerated (<paramref name="comments"/> says
        /// whether any comment was skipped). Invalid JSON adds one OPT100/OPT101 diagnostic and returns null.
        /// </summary>
        public static OptimisationJsonNode Parse(string text, List<OptimisationDiagnostic> diagnostics, out bool comments)
        {
            comments = false;

            if (string.IsNullOrWhiteSpace(text))
            {
                diagnostics.Add(new OptimisationDiagnostic(DiagnosticSeverity.Error, "OPT101", "$", "The definition text is empty.", "Paste or type one JSON object that starts with { and ends with }.", 1, 1));
                return null;
            }

            byte[] bytes = Encoding.UTF8.GetBytes(text ?? string.Empty);
            LineIndex lineIndex = new LineIndex(bytes);

            JsonReaderOptions jsonReaderOptions = new JsonReaderOptions()
            {
                CommentHandling = JsonCommentHandling.Allow,
                AllowTrailingCommas = true,
                MaxDepth = 64,
            };

            Utf8JsonReader utf8JsonReader = new Utf8JsonReader(bytes, jsonReaderOptions);
            try
            {
                if (!Next(ref utf8JsonReader, ref comments))
                {
                    diagnostics.Add(new OptimisationDiagnostic(DiagnosticSeverity.Error, "OPT101", "$", "The definition text is empty.", "Paste or type one JSON object that starts with { and ends with }.", 1, 1));
                    return null;
                }

                if (utf8JsonReader.TokenType != JsonTokenType.StartObject)
                {
                    lineIndex.Position(utf8JsonReader.TokenStartIndex, out int line, out int column);
                    diagnostics.Add(new OptimisationDiagnostic(DiagnosticSeverity.Error, "OPT101", "$", "The definition must be one JSON object.", "Start the text with { and end it with }.", line, column));
                    return null;
                }

                OptimisationJsonNode result = Read(ref utf8JsonReader, lineIndex, ref comments);

                // Anything after the object other than whitespace or comments is invalid JSON; the reader throws.
                while (utf8JsonReader.Read())
                {
                    if (utf8JsonReader.TokenType == JsonTokenType.Comment)
                    {
                        comments = true;
                    }
                }

                return result;
            }
            catch (JsonException jsonException)
            {
                int line = (int)(jsonException.LineNumber ?? 0) + 1;
                int column = (int)(jsonException.BytePositionInLine ?? 0) + 1;
                diagnostics.Add(new OptimisationDiagnostic(DiagnosticSeverity.Error, "OPT100", "$", "The text is not valid JSON: " + Reason(jsonException.Message), "Check for a missing or extra comma, quote or bracket near this place.", line, column));
                return null;
            }
        }

        private static OptimisationJsonNode Read(ref Utf8JsonReader utf8JsonReader, LineIndex lineIndex, ref bool comments)
        {
            lineIndex.Position(utf8JsonReader.TokenStartIndex, out int line, out int column);

            switch (utf8JsonReader.TokenType)
            {
                case JsonTokenType.StartObject:
                    {
                        OptimisationJsonNode result = new OptimisationJsonNode(OptimisationJsonKind.Object, line, column);
                        while (Next(ref utf8JsonReader, ref comments) && utf8JsonReader.TokenType != JsonTokenType.EndObject)
                        {
                            lineIndex.Position(utf8JsonReader.TokenStartIndex, out int line_Property, out int column_Property);
                            string name = utf8JsonReader.GetString();
                            Next(ref utf8JsonReader, ref comments);
                            result.Properties.Add(new OptimisationJsonProperty(name, line_Property, column_Property, Read(ref utf8JsonReader, lineIndex, ref comments)));
                        }

                        return result;
                    }

                case JsonTokenType.StartArray:
                    {
                        OptimisationJsonNode result = new OptimisationJsonNode(OptimisationJsonKind.Array, line, column);
                        while (Next(ref utf8JsonReader, ref comments) && utf8JsonReader.TokenType != JsonTokenType.EndArray)
                        {
                            result.Items.Add(Read(ref utf8JsonReader, lineIndex, ref comments));
                        }

                        return result;
                    }

                case JsonTokenType.String:
                    return new OptimisationJsonNode(OptimisationJsonKind.String, line, column) { Text = utf8JsonReader.GetString() };

                case JsonTokenType.Number:
                    return new OptimisationJsonNode(OptimisationJsonKind.Number, line, column) { Text = Encoding.UTF8.GetString(utf8JsonReader.ValueSpan.ToArray()) };

                case JsonTokenType.True:
                case JsonTokenType.False:
                    return new OptimisationJsonNode(OptimisationJsonKind.Boolean, line, column) { Boolean = utf8JsonReader.TokenType == JsonTokenType.True };

                default:
                    return new OptimisationJsonNode(OptimisationJsonKind.Null, line, column);
            }
        }

        /// <summary>Reads the next token that is not a comment; false at the end of the text.</summary>
        private static bool Next(ref Utf8JsonReader utf8JsonReader, ref bool comments)
        {
            while (utf8JsonReader.Read())
            {
                if (utf8JsonReader.TokenType == JsonTokenType.Comment)
                {
                    comments = true;
                    continue;
                }

                return true;
            }

            return false;
        }

        /// <summary>The reader's message without its trailing "LineNumber: … | BytePositionInLine: …" part.</summary>
        private static string Reason(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return "unexpected text.";
            }

            int index = message.IndexOf(" LineNumber:", StringComparison.Ordinal);
            string result = (index >= 0 ? message.Substring(0, index) : message).Trim();
            return result.EndsWith(".", StringComparison.Ordinal) ? result : result + ".";
        }

        /// <summary>One-based line and column (in characters) of a UTF-8 byte offset.</summary>
        private sealed class LineIndex
        {
            private readonly byte[] bytes;
            private readonly List<int> lineStarts = new List<int>() { 0 };

            public LineIndex(byte[] bytes)
            {
                this.bytes = bytes;
                for (int i = 0; i < bytes.Length; i++)
                {
                    if (bytes[i] == (byte)'\n')
                    {
                        lineStarts.Add(i + 1);
                    }
                }
            }

            public void Position(long offset, out int line, out int column)
            {
                int index = lineStarts.BinarySearch((int)offset);
                if (index < 0)
                {
                    index = ~index - 1;
                }

                line = index + 1;

                // Count characters, not bytes: UTF-8 continuation bytes (10xxxxxx) do not start a character.
                column = 1;
                for (int i = lineStarts[index]; i < offset && i < bytes.Length; i++)
                {
                    if ((bytes[i] & 0xC0) != 0x80)
                    {
                        column++;
                    }
                }
            }
        }
    }
}
