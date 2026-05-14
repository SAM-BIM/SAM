// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Collections.Generic;
using SAM.Core;
using SAM.Tests.Helpers;
using Xunit;

namespace SAM.Tests
{
    public class ExpressionTests
    {
        [Fact]
        public void RoundTrip_Text_PreservesVariables()
        {
            Expression expression = new Expression("[Area] * [Height] + [Offset]");

            Expression result = RoundTrip.Once(expression);

            Assert.Equal(expression, result);
            Assert.Equal("[Area] * [Height] + [Offset]", result.Text);

            List<ExpressionVariable> variables = result.GetExpressionVariables();
            Assert.NotNull(variables);
            Assert.Collection(
                variables,
                x => Assert.Equal("Area", x.Text),
                x => Assert.Equal("Height", x.Text),
                x => Assert.Equal("Offset", x.Text));
        }

        [Fact]
        public void FromJson_MissingText_RoundTripsTypeOnly()
        {
            const string json = @"{""_type"":""SAM.Core.Expression,SAM.Core""}";

            Expression result = RoundTrip.FromJson<Expression>(json);

            Assert.Null(result.Text);
            Assert.Null(result.GetExpressionVariables());
        }
    }
}
