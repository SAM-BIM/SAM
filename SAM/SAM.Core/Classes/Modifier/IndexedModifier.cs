// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;

namespace SAM.Core
{
    public abstract class IndexedModifier : Modifier, IIndexedModifier
    {
        public IndexedModifier()
        {

        }

        public IndexedModifier(IndexedModifier indexedModifier)
            : base(indexedModifier)
        {

        }
        public IndexedModifier(System.Text.Json.Nodes.JsonObject jsonObject)

            : base(jsonObject)

        {

        }

        public abstract bool ContainsIndex(int index);

        public abstract double GetCalculatedValue(int index, double value);
    }
}
