// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

namespace SAM.Core
{
    public static partial class Query
    {
        /// <summary>
        /// A count followed by the noun in the right number, for user-facing messages: "1 panel", "0 panels",
        /// "18 windows", "2 Air Panels".
        /// </summary>
        /// <param name="count">Count.</param>
        /// <param name="singular">Noun used when <paramref name="count"/> is 1.</param>
        /// <param name="plural">Noun used otherwise (default: <paramref name="singular"/> + "s").</param>
        /// <returns>The count and the noun, separated by a space.</returns>
        public static string Plural(int count, string singular, string plural = null)
        {
            string noun = count == 1 ? singular : plural ?? string.Format("{0}s", singular);
            return string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0} {1}", count, noun);
        }
    }
}
