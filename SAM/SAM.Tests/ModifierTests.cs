// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core;
using SAM.Tests.Helpers;
using Xunit;

namespace SAM.Tests
{
    public class ModifierTests
    {
        [Fact]
        public void RoundTrip_ModifiableValue_BareValue()
        {
            // ModifiableValue is a leaf IJSAMObject; root-level migration moved
            // the work into protected JsonObject helpers.
            ModifiableValue value = new ModifiableValue(42.5);

            ModifiableValue result = RoundTrip.Once(value);

            Assert.Equal(42.5, result.Value);
            Assert.Null(result.Modifier);
        }

        [Fact]
        public void RoundTrip_TableModifier_PreservesArithmeticOperator()
        {
            // TableModifier nests two levels deep: TableModifier -> SimpleModifier
            // -> Modifier. Each level migrated to the protected JsonObject hook
            // pair; the round-trip below exercises every level.
            TableModifier modifier = new TableModifier(ArithmeticOperator.Multiplication, new[] { "Temperature", "Factor" });

            TableModifier result = RoundTrip.Once(modifier);

            Assert.Equal(ArithmeticOperator.Multiplication, result.ArithmeticOperator);
        }
    }
}
