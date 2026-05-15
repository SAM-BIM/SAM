// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors
// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace SAM.Tests.Helpers
{
    public static class JsonEquivalence
    {
        public static bool AreEquivalent(string? leftJson, string? rightJson, out string? difference)
        {
            difference = null;

            if (leftJson == null && rightJson == null)
            {
                return true;
            }

            if (leftJson == null || rightJson == null)
            {
                difference = "one side is null";
                return false;
            }

            using JsonDocument left = JsonDocument.Parse(leftJson);
            using JsonDocument right = JsonDocument.Parse(rightJson);

            return AreEquivalent(left.RootElement, right.RootElement, "$", out difference);
        }

        private static bool AreEquivalent(JsonElement left, JsonElement right, string path, out string? difference)
        {
            difference = null;

            if (left.ValueKind != right.ValueKind)
            {
                difference = $"{path}: kind {left.ValueKind} vs {right.ValueKind}";
                return false;
            }

            switch (left.ValueKind)
            {
                case JsonValueKind.Object:
                    return AreObjectsEquivalent(left, right, path, out difference);

                case JsonValueKind.Array:
                    return AreArraysEquivalent(left, right, path, out difference);

                case JsonValueKind.String:
                    if (left.GetString() == right.GetString())
                    {
                        return true;
                    }
                    difference = $"{path}: \"{left.GetString()}\" vs \"{right.GetString()}\"";
                    return false;

                case JsonValueKind.Number:
                    return AreNumbersEquivalent(left, right, path, out difference);

                case JsonValueKind.True:
                case JsonValueKind.False:
                case JsonValueKind.Null:
                case JsonValueKind.Undefined:
                    return true;

                default:
                    difference = $"{path}: unsupported kind {left.ValueKind}";
                    return false;
            }
        }

        private static bool AreObjectsEquivalent(JsonElement left, JsonElement right, string path, out string? difference)
        {
            difference = null;

            Dictionary<string, JsonElement> leftDictionary = left.EnumerateObject().ToDictionary(p => p.Name, p => p.Value);
            Dictionary<string, JsonElement> rightDictionary = right.EnumerateObject().ToDictionary(p => p.Name, p => p.Value);

            if (leftDictionary.Count != rightDictionary.Count)
            {
                IEnumerable<string> missingFromRight = leftDictionary.Keys.Except(rightDictionary.Keys);
                IEnumerable<string> missingFromLeft = rightDictionary.Keys.Except(leftDictionary.Keys);
                difference = $"{path}: property count {leftDictionary.Count} vs {rightDictionary.Count}; only-in-left=[{string.Join(",", missingFromRight)}], only-in-right=[{string.Join(",", missingFromLeft)}]";
                return false;
            }

            foreach (KeyValuePair<string, JsonElement> pair in leftDictionary)
            {
                if (!rightDictionary.TryGetValue(pair.Key, out JsonElement rightValue))
                {
                    difference = $"{path}: missing property '{pair.Key}' on right";
                    return false;
                }

                if (!AreEquivalent(pair.Value, rightValue, $"{path}.{pair.Key}", out difference))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool AreArraysEquivalent(JsonElement left, JsonElement right, string path, out string? difference)
        {
            difference = null;

            int leftLength = left.GetArrayLength();
            int rightLength = right.GetArrayLength();

            if (leftLength != rightLength)
            {
                difference = $"{path}: array length {leftLength} vs {rightLength}";
                return false;
            }

            for (int index = 0; index < leftLength; index++)
            {
                if (!AreEquivalent(left[index], right[index], $"{path}[{index}]", out difference))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool AreNumbersEquivalent(JsonElement left, JsonElement right, string path, out string? difference)
        {
            difference = null;

            if (left.TryGetDecimal(out decimal leftDecimal) && right.TryGetDecimal(out decimal rightDecimal))
            {
                if (leftDecimal == rightDecimal)
                {
                    return true;
                }
            }

            double leftDouble = left.GetDouble();
            double rightDouble = right.GetDouble();

            if (leftDouble == rightDouble)
            {
                return true;
            }

            if (double.IsNaN(leftDouble) && double.IsNaN(rightDouble))
            {
                return true;
            }

            difference = $"{path}: number {left.GetRawText()} vs {right.GetRawText()}";
            return false;
        }
    }
}
