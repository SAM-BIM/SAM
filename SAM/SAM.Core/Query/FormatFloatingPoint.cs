// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Globalization;

namespace SAM.Core
{
    public static partial class Query
    {
        // Helper used by ToJsonNode to preserve Newtonsoft-style wire format for
        // double/float: whole numbers emit a trailing ".0" so that round-tripping
        // through STJ does not lose the original decimal-typed shape, and NaN /
        // Infinity emit as JSON null.
        private static string FormatFloatingPoint(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                return "null";

            string text = value.ToString("R", CultureInfo.InvariantCulture);
            if (text.IndexOf('.') < 0 && text.IndexOf('e') < 0 && text.IndexOf('E') < 0)
                text += ".0";

            return text;
        }
    }
}
