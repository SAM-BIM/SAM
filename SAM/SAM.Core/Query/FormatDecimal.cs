// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Globalization;

namespace SAM.Core
{
    public static partial class Query
    {
        // Helper used by ToJsonNode to preserve Newtonsoft-style wire format for
        // decimal values: whole numbers emit a trailing ".0".
        private static string FormatDecimal(decimal value)
        {
            string text = value.ToString(CultureInfo.InvariantCulture);
            if (text.IndexOf('.') < 0)
                text += ".0";

            return text;
        }
    }
}
