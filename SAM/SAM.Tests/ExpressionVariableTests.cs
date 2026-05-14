// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core;
using SAM.Tests.Helpers;
using Xunit;

namespace SAM.Tests
{
    public class ExpressionVariableTests
    {
        [Fact]
        public void RoundTrip_Text_PreservesNameAndNestedVariable()
        {
            ExpressionVariable expressionVariable = new ExpressionVariable("Space[Area]");

            ExpressionVariable result = RoundTrip.Once(expressionVariable);

            Assert.Equal(expressionVariable, result);
            Assert.Equal("Space[Area]", result.Text);
            Assert.Equal("Space", result.GetName());

            ExpressionVariable nested = result.GetExpressionVariable();
            Assert.NotNull(nested);
            Assert.Equal("Area", nested.Text);
        }

        [Fact]
        public void RoundTrip_PlainText_ReturnsNameWithoutNestedVariable()
        {
            ExpressionVariable expressionVariable = new ExpressionVariable("Height");

            ExpressionVariable result = RoundTrip.Once(expressionVariable);

            Assert.Equal("Height", result.GetName());
            Assert.Null(result.GetExpressionVariable());
        }

        [Fact]
        public void FromJson_MissingText_RoundTripsTypeOnly()
        {
            const string json = @"{""_type"":""SAM.Core.ExpressionVariable,SAM.Core""}";

            ExpressionVariable result = RoundTrip.FromJson<ExpressionVariable>(json);

            Assert.Null(result.Text);
            Assert.Null(result.GetName());
            Assert.Null(result.GetExpressionVariable());
        }
    }
}
