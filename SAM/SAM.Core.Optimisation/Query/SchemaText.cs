// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.IO;

namespace SAM.Core.Optimisation
{
    public static partial class Query
    {
        private const string schemaResourceName = "SAM.Core.Optimisation.Resources.sam.optimisation-1.schema.json";

        /// <summary>
        /// The JSON Schema (draft 2020-12) of <see cref="OptimisationDefinition.Schema"/>, for documentation and for
        /// tools outside SAM. SAM itself reads definitions with its own strict reader, which gives engineering messages
        /// with line and column; the schema and the reader describe the same fields.
        /// </summary>
        public static string SchemaText()
        {
            using (Stream stream = typeof(OptimisationDefinition).Assembly.GetManifestResourceStream(schemaResourceName))
            {
                if (stream == null)
                {
                    return null;
                }

                using (StreamReader streamReader = new StreamReader(stream))
                {
                    return streamReader.ReadToEnd();
                }
            }
        }
    }
}
