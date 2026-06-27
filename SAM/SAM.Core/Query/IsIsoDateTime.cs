// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Globalization;

namespace SAM.Core
{
    public static partial class Query
    {
        public static bool IsIsoDateTime(string text)
        {
            if (text == null || text.Length < 19)
                return false;

            if (text[4] != '-' || text[7] != '-' || text[10] != 'T' || text[13] != ':' || text[16] != ':')
                return false;

            DateTime dateTime;
            return DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out dateTime);
        }
    }
}
