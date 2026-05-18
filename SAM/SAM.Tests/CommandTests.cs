// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Collections.Generic;
using SAM.Core;
using SAM.Tests.Helpers;
using Xunit;

namespace SAM.Tests
{
    public class CommandTests
    {
        [Fact]
        public void RoundTrip_TextValue_PreservesValueSemantics()
        {
            Command command = new Command("'Some Text'");

            Command result = RoundTrip.Once(command);

            Assert.Equal(command, result);
            Assert.True(result.IsValue(out object value));
            Assert.Equal("Some Text", Assert.IsType<string>(value));
        }

        [Theory]
        [InlineData("42", 42)]
        [InlineData("42.5", 42.5)]
        public void RoundTrip_NumericValue_PreservesValueSemantics(string text, object expected)
        {
            Command result = RoundTrip.Once(new Command(text));

            Assert.True(result.IsValue(out object value));
            Assert.Equal(expected, value);
        }

        [Fact]
        public void RoundTrip_Operator_PreservesOperatorSemantics()
        {
            Command command = new Command(">=");

            Command result = RoundTrip.Once(command);

            Assert.True(result.IsOperator(out Enum @operator));
            Assert.Equal(RelationalOperator.GreaterThanOrEqual, @operator);
        }

        [Fact]
        public void RoundTrip_Object_PreservesNameAndMembers()
        {
            Command command = new Command("$Panel.Width.Height");

            Command result = RoundTrip.Once(command);

            Assert.True(result.IsObject(out string name, out List<Command> members));
            Assert.Equal("Panel", name);
            Assert.Collection(
                members,
                x => Assert.Equal("Width", x.Text),
                x => Assert.Equal("Height", x.Text));
        }

        [Fact]
        public void FromJson_MissingText_RoundTripsTypeOnly()
        {
            const string json = @"{""_type"":""SAM.Core.Command,SAM.Core""}";

            Command result = RoundTrip.FromJson<Command>(json);

            Assert.Null(result.Text);
            Assert.True(result.IsEmpty());
            Assert.False(result.IsValue());
            Assert.False(result.IsOperator());
        }
    }
}
