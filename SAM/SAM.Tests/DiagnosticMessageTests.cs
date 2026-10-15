// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core;
using Xunit;

namespace SAM.Tests
{
    /// <summary>
    /// <see cref="Core.Query.DiagnosticMessage"/> and <see cref="Core.Query.Plural"/>: the SAM Diagnostics line format.
    /// </summary>
    public class DiagnosticMessageTests
    {
        [Theory]
        [InlineData(0, "panel", null, "0 panels")]
        [InlineData(1, "panel", null, "1 panel")]
        [InlineData(2, "Air Panel", null, "2 Air Panels")]
        [InlineData(1, "notch", "notches", "1 notch")]
        [InlineData(3, "notch", "notches", "3 notches")]
        public void Plural_UsesTheRightNumber(int count, string singular, string plural, string expected)
        {
            Assert.Equal(expected, Core.Query.Plural(count, singular, plural));
        }

        [Theory]
        [InlineData(LogRecordType.Message, "Info")]
        [InlineData(LogRecordType.Undefined, "Info")]
        [InlineData(LogRecordType.Warning, "Warning")]
        [InlineData(LogRecordType.Error, "Error")]
        public void DiagnosticMessage_StartsWithTheSeverity(LogRecordType logRecordType, string severity)
        {
            Assert.Equal(severity + ": Solid #3 'Wall': Skipped.", Core.Query.DiagnosticMessage(logRecordType, "Solid #3 'Wall'", "Skipped."));
        }

        [Fact]
        public void DiagnosticMessage_WithoutSubject_CapitalisesTheMessage()
        {
            Assert.Equal("Info: Created 3 panels from 2 solids.", Core.Query.DiagnosticMessage(LogRecordType.Message, null, " created 3 panels from 2 solids."));
        }
    }
}
