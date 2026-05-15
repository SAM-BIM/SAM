// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors
// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.IO;

namespace SAM.Tests.Helpers
{
    public static class Fixtures
    {
        private static string? root;

        public static string ResourcesDirectory
        {
            get
            {
                root ??= Resolve();
                return System.IO.Path.Combine(root, "files", "resources", "Analytical");
            }
        }

        public static string GetPath(string fileName)
        {
            return System.IO.Path.Combine(ResourcesDirectory, fileName);
        }

        public static string ReadAllText(string fileName)
        {
            return File.ReadAllText(GetPath(fileName));
        }

        private static string Resolve()
        {
            DirectoryInfo? directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory != null)
            {
                if (File.Exists(System.IO.Path.Combine(directory.FullName, "SAM.sln")))
                {
                    return directory.FullName;
                }

                directory = directory.Parent;
            }

            throw new InvalidOperationException("Could not locate SAM.sln above test bin directory.");
        }
    }
}
