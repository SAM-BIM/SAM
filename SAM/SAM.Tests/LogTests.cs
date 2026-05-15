// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors
// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Collections.Generic;
using SAM.Core;
using SAM.Tests.Helpers;
using Xunit;

namespace SAM.Tests
{
    public class LogTests
    {
        [Fact]
        public void RoundTrip_LogRecord_PreservesAllFields()
        {
            // LogRecord round-trip: DateTime, Text and LogRecordType must
            // survive through the BCL JsonObject chain.
            LogRecord original = new LogRecord("test message {0}", LogRecordType.Warning, 42);
            DateTime originalDateTime = original.DateTime;

            LogRecord result = RoundTrip.Once(original);

            Assert.Equal("test message 42", result.Text);
            Assert.Equal(LogRecordType.Warning, result.LogRecordType);
            Assert.Equal(originalDateTime, result.DateTime);
        }

        [Fact]
        public void RoundTrip_LogRecord_UndefinedTypeOmittedOnWire()
        {
            // ToJsonObject is supposed to skip the LogRecordType key when it
            // equals Undefined. Verify the wire format keeps the omission.
            LogRecord original = new LogRecord("plain text");
            string json = SAM.Core.Convert.ToString(original);

            Assert.DoesNotContain("\"LogRecordType\":", json);
            Assert.Contains("\"Text\":", json);
        }

        [Fact]
        public void RoundTrip_Log_PreservesRecordOrder()
        {
            Log log = new Log("Build");
            log.Add("first message {0}", 1);
            log.Add("second message {0}", LogRecordType.Error, 2);

            Log result = RoundTrip.Once(log);

            List<LogRecord> records = new List<LogRecord>();
            foreach (LogRecord record in result)
            {
                records.Add(record);
            }

            Assert.Equal(2, records.Count);
            Assert.Equal("first message 1", records[0].Text);
            Assert.Equal(LogRecordType.Undefined, records[0].LogRecordType);
            Assert.Equal("second message 2", records[1].Text);
            Assert.Equal(LogRecordType.Error, records[1].LogRecordType);
        }
    }
}
