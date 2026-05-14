// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Drawing;
using SAM.Core;
using SAM.Tests.Helpers;
using Xunit;

namespace SAM.Tests
{
    public class TagTests
    {
        [Fact]
        public void RoundTrip_Primitives()
        {
            AssertTagRoundTrip(new Tag("label"), SAM.Core.ValueType.String, "label");
            AssertTagRoundTrip(new Tag(12.5), SAM.Core.ValueType.Double, 12.5);
            AssertTagRoundTrip(new Tag(true), SAM.Core.ValueType.Boolean, true);
        }

        [Fact]
        public void RoundTrip_GuidAndDateTime()
        {
            Guid guid = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
            DateTime dateTime = new DateTime(2026, 5, 14, 12, 30, 45, DateTimeKind.Utc);

            AssertTagRoundTrip(new Tag(guid), SAM.Core.ValueType.Guid, guid);

            Tag dateTag = RoundTrip.Once(new Tag(dateTime));
            Assert.Equal(SAM.Core.ValueType.DateTime, dateTag.ValueType);
            Assert.Equal(dateTime.ToUniversalTime(), dateTag.GetValue<DateTime>().ToUniversalTime());
        }

        [Fact]
        public void RoundTrip_Color()
        {
            Color color = Color.FromArgb(255, 12, 34, 56);

            Tag result = RoundTrip.Once(new Tag(color));

            Assert.Equal(SAM.Core.ValueType.Color, result.ValueType);
            Assert.Equal(color.ToArgb(), result.GetValue<Color>().ToArgb());
        }

        [Fact]
        public void RoundTrip_IJSAMObject()
        {
            Category category = new Category("Parent", new Category("Child"));

            Tag result = RoundTrip.Once(new Tag(category));

            Assert.Equal(SAM.Core.ValueType.IJSAMObject, result.ValueType);
            Category resultCategory = Assert.IsType<Category>(result.Value);
            Assert.Equal("Parent", resultCategory.Name);
            Assert.Equal("Child", resultCategory.SubCategory?.Name);
        }

        [Fact]
        public void FromJson_IntegerValueType()
        {
            const string json = @"{""_type"":""SAM.Core.Tag,SAM.Core"",""ValueType"":""Integer"",""Value"":7}";

            Tag result = RoundTrip.FromJson<Tag>(json);

            Assert.Equal(SAM.Core.ValueType.Integer, result.ValueType);
            Assert.Equal(7, result.GetValue<int>());
            Assert.IsType<int>(result.Value);
        }

        private static void AssertTagRoundTrip<T>(Tag tag, SAM.Core.ValueType valueType, T expected)
        {
            Tag result = RoundTrip.Once(tag);

            Assert.Equal(valueType, result.ValueType);
            Assert.Equal(expected, result.GetValue<T>());
            Assert.IsType<T>(result.Value);
        }
    }
}
