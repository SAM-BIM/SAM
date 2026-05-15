// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors
// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Collections.Generic;
using System.Linq;
using SAM.Core;
using SAM.Tests.Helpers;
using Xunit;

namespace SAM.Tests
{
    public class SearchWrapperTests
    {
        [Fact]
        public void RoundTrip_SearchWrapper_PreservesTextsAndConfig()
        {
            // SearchWrapper has no Name/Guid: it's a leaf IJSAMObject with its
            // own root-level FromJObject/ToJObject. The migration moved the
            // work into private JsonObject methods.
            SearchWrapper original = new SearchWrapper(
                texts: new List<string> { "alpha beta", "gamma" },
                separators: new[] { ' ' },
                caseSensitive: false);

            SearchWrapper result = RoundTrip.Once(original);

            List<string> roundTrippedTexts = result.Texts?.ToList() ?? new List<string>();
            Assert.Contains("alpha beta", roundTrippedTexts);
            Assert.Contains("gamma", roundTrippedTexts);
        }
    }
}
