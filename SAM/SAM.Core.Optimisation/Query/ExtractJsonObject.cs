// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Text.Json;

namespace SAM.Core.Optimisation
{
    public static partial class Query
    {
        /// <summary>
        /// The JSON object in pasted text, for example an AI reply that wrapped it in a Markdown code fence or added an
        /// explanation despite being asked not to. Each "{" is tried in turn; the first one that opens a balanced object
        /// (quotes, escapes and comments respected) which is valid JSON is returned. If none is valid, the first balanced
        /// object is returned, so that its syntax errors can be reported.
        /// </summary>
        /// <param name="text">The pasted text.</param>
        /// <param name="trimmed">True when anything other than white space surrounded the object.</param>
        /// <returns>The object's text, or null when the text holds no balanced object.</returns>
        public static string ExtractJsonObject(string text, out bool trimmed)
        {
            trimmed = false;
            if (string.IsNullOrEmpty(text))
            {
                return null;
            }

            string first = null;
            int start_First = -1;
            int end_First = -1;

            for (int start = text.IndexOf('{'); start >= 0; start = text.IndexOf('{', start + 1))
            {
                int end = End(text, start);
                if (end < 0)
                {
                    continue;
                }

                string candidate = text.Substring(start, end - start + 1);
                if (first == null)
                {
                    first = candidate;
                    start_First = start;
                    end_First = end;
                }

                if (IsJson(candidate))
                {
                    trimmed = !string.IsNullOrWhiteSpace(text.Substring(0, start)) || !string.IsNullOrWhiteSpace(text.Substring(end + 1));
                    return candidate;
                }
            }

            if (first != null)
            {
                trimmed = !string.IsNullOrWhiteSpace(text.Substring(0, start_First)) || !string.IsNullOrWhiteSpace(text.Substring(end_First + 1));
            }

            return first;
        }

        /// <summary>The index of the "}" that closes the "{" at <paramref name="start"/>, or -1.</summary>
        private static int End(string text, int start)
        {
            int depth = 0;
            bool inString = false;
            for (int i = start; i < text.Length; i++)
            {
                char c = text[i];
                if (inString)
                {
                    if (c == '\\')
                    {
                        i++;
                    }
                    else if (c == '"')
                    {
                        inString = false;
                    }

                    continue;
                }

                if (c == '"')
                {
                    inString = true;
                }
                else if (c == '/' && i + 1 < text.Length && text[i + 1] == '/')
                {
                    int index = text.IndexOf('\n', i);
                    if (index < 0)
                    {
                        return -1;
                    }

                    i = index;
                }
                else if (c == '/' && i + 1 < text.Length && text[i + 1] == '*')
                {
                    int index = text.IndexOf("*/", i + 2, System.StringComparison.Ordinal);
                    if (index < 0)
                    {
                        return -1;
                    }

                    i = index + 1;
                }
                else if (c == '{')
                {
                    depth++;
                }
                else if (c == '}')
                {
                    depth--;
                    if (depth == 0)
                    {
                        return i;
                    }
                }
            }

            return -1;
        }

        private static bool IsJson(string text)
        {
            try
            {
                using (JsonDocument.Parse(text, new JsonDocumentOptions() { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true }))
                {
                    return true;
                }
            }
            catch (JsonException)
            {
                return false;
            }
        }
    }
}
