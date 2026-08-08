// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json.Nodes;

namespace SAM.Analytical
{
    /// <summary>
    /// The named operating assumptions that make one overheating assessment a different assessment from
    /// another over the same fabric - how the openings are operated, whether a mechanical system's boost or
    /// summer bypass is available, and so on.
    /// <para>
    /// <b>Identity-defining, and that is the point.</b> Two runs of the same dwelling with the same system
    /// but different opening behaviour are two engineering answers, not one answer computed twice, and they
    /// must not share a key. Everything in here therefore participates in
    /// <c>OverheatingScenario.Key</c>.
    /// </para>
    /// <para>
    /// <b>A bag of names and values rather than a typed set, deliberately.</b> The assumptions that define
    /// the mitigated Part O iterations are not settled yet, and inventing an enum for each of them now
    /// would fix a vocabulary before the engineering that uses it exists. What this does have to be is
    /// canonical: the assumptions are ordered by name, ordinally, so a scenario assembled in a different
    /// order derives the same key, and values are formatted invariantly so a machine's locale can never
    /// change an identity. The typed <see cref="Set(string, double)"/> and <see cref="Set(string, bool)"/>
    /// overloads exist so a caller cannot reach for <c>ToString()</c> and get a comma decimal separator
    /// into a key.
    /// </para>
    /// <para>
    /// <b>Not a parameter set.</b> <c>ParameterSet</c> already exists and is the right thing for values
    /// carried on a model object. This is a small immutable-in-practice value whose entire job is to hash
    /// canonically, and reusing <c>ParameterSet</c> would have meant depending on its ordering and its JSON
    /// shape as an identity contract - which they are not.
    /// </para>
    /// </summary>
    public class OverheatingOperatingAssumptions : IJSAMObject, IAnalyticalObject
    {
        //Ordinal, not culture-aware: a key must not depend on the machine's collation, and "SS" must never
        //be equal to "ß".
        private readonly SortedDictionary<string, string> dictionary = new(StringComparer.Ordinal);

        public OverheatingOperatingAssumptions()
        {

        }

        public OverheatingOperatingAssumptions(OverheatingOperatingAssumptions overheatingOperatingAssumptions)
        {
            if (overheatingOperatingAssumptions != null)
            {
                foreach (KeyValuePair<string, string> keyValuePair in overheatingOperatingAssumptions.dictionary)
                {
                    dictionary[keyValuePair.Key] = keyValuePair.Value;
                }
            }
        }

        public OverheatingOperatingAssumptions(JsonObject jsonObject)
        {
            FromJsonObject(jsonObject);
        }

        /// <summary>How many assumptions are stated.</summary>
        public int Count => dictionary.Count;

        /// <summary>
        /// The names stated, in the canonical ordinal order the key is derived in. A copy - adding to it
        /// changes nothing.
        /// </summary>
        public List<string> Names => [.. dictionary.Keys];

        /// <summary>Whether an assumption of this name is stated.</summary>
        public bool Contains(string name)
        {
            return name != null && dictionary.ContainsKey(name);
        }

        /// <summary>The value stated for an assumption, or null where it is not stated.</summary>
        public string Value(string name)
        {
            return name != null && dictionary.TryGetValue(name, out string result) ? result : null;
        }

        /// <summary>
        /// States an assumption. A blank name is ignored - an unnamed assumption cannot be read back and
        /// would only make the key depend on something invisible. A null value is stored as empty.
        /// </summary>
        public void Set(string name, string value)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return;
            }

            dictionary[name] = value ?? string.Empty;
        }

        /// <summary>
        /// States a numeric assumption, formatted round-trippably and invariantly. <b>Use this rather than
        /// formatting the number yourself</b> - a comma decimal separator picked up from the machine's
        /// locale would make the same scenario derive two different keys on two engineers' machines.
        /// </summary>
        public void Set(string name, double value)
        {
            Set(name, value.ToString("R", CultureInfo.InvariantCulture));
        }

        /// <summary>States a boolean assumption, invariantly.</summary>
        public void Set(string name, bool value)
        {
            Set(name, value ? "True" : "False");
        }

        /// <summary>Removes an assumption. Removing an unstated one does nothing.</summary>
        public bool Remove(string name)
        {
            return name != null && dictionary.Remove(name);
        }

        /// <summary>
        /// The assumptions as ordered name/value pairs, in the order the key is derived in. A copy.
        /// </summary>
        public List<KeyValuePair<string, string>> ToList()
        {
            return [.. dictionary];
        }

        public bool FromJsonObject(JsonObject jsonObject)
        {
            if (jsonObject == null)
            {
                return false;
            }

            dictionary.Clear();

            if (jsonObject["Assumptions"] is JsonObject jsonObject_Assumptions)
            {
                foreach (KeyValuePair<string, JsonNode> keyValuePair in jsonObject_Assumptions)
                {
                    Set(keyValuePair.Key, keyValuePair.Value?.GetValue<string>());
                }
            }

            return true;
        }

        public JsonObject ToJsonObject()
        {
            JsonObject jsonObject = new()
            {
                ["_type"] = Core.Query.FullTypeName(this)
            };

            JsonObject jsonObject_Assumptions = new();

            foreach (KeyValuePair<string, string> keyValuePair in dictionary)
            {
                jsonObject_Assumptions[keyValuePair.Key] = keyValuePair.Value;
            }

            jsonObject["Assumptions"] = jsonObject_Assumptions;

            return jsonObject;
        }

        public override string ToString()
        {
            List<string> strings = [];

            foreach (KeyValuePair<string, string> keyValuePair in dictionary)
            {
                strings.Add(string.Format(CultureInfo.InvariantCulture, "{0}={1}", keyValuePair.Key, keyValuePair.Value));
            }

            return string.Join(", ", strings);
        }
    }
}
