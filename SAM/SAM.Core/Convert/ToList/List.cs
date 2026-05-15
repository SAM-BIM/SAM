// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace SAM.Core
{
    public static partial class Convert
    {
        public static List<string[]> ToList(this string text, DelimitedFileType delimitedFileType = DelimitedFileType.Csv)
        {
            return ToList(text, Query.Separator(delimitedFileType));
        }

        public static List<string[]> ToList(this string text, char separator = ',')
        {
            if (string.IsNullOrEmpty(text))
                return null;

            string[] lines = text.Split('\n');

            DelimitedFileTable delimitedFileTable = new DelimitedFileTable(new DelimitedFileReader(separator, lines));

            List<string> columnNames = delimitedFileTable.GetColumnNames();

            List<string[]> result = new List<string[]>();
            foreach (string columnName in columnNames)
            {
                string[] values = new string[delimitedFileTable.RowCount];
                for (int i = 0; i < delimitedFileTable.RowCount; i++)
                {
                    object value = delimitedFileTable[i, columnName];
                    if (value != null)
                        values[i] = value.ToString();
                }
                result.Add(values);
            }
            return result;
        }

        public static List<T> ToList<T>(this JsonArray jsonArray, bool skipInvalid = false, bool tryConvert = false)
        {
            if (jsonArray == null)
                return null;

            List<T> result = new List<T>();
            foreach (JsonNode jsonNode in jsonArray)
            {
                object object_Temp = jsonNode.ToObject();

                if (object_Temp is T)
                {
                    result.Add((T)object_Temp);
                    continue;
                }

                if (!tryConvert)
                {
                    if (!skipInvalid)
                        result.Add(default);

                    continue;
                }

                T value = default;
                if (!Query.TryConvert(object_Temp, out value))
                {
                    if (skipInvalid)
                        continue;

                    value = default;
                }

                result.Add(value);
            }

            return result;
        }
    }
}
