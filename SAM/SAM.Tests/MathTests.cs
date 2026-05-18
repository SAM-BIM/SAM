// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Math;
using SAM.Tests.Helpers;
using Xunit;

namespace SAM.Tests
{
    public class MathTests
    {
        [Fact]
        public void RoundTrip_LinearEquation_PreservesCoefficients()
        {
            LinearEquation expected = new LinearEquation(2.0, 3.0);

            LinearEquation result = RoundTrip.Once(expected);

            Assert.Equal(7.0, result.Evaluate(2.0));
        }

        [Fact]
        public void RoundTrip_Matrix_PreservesValues()
        {
            Matrix expected = new Matrix(new double[,]
            {
                { 1.0, 2.0 },
                { 3.0, 4.0 }
            });

            Matrix result = RoundTrip.Once(expected);

            Assert.Equal(1.0, result[0, 0]);
            Assert.Equal(4.0, result[1, 1]);
        }
    }
}
