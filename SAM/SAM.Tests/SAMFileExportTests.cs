// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text.Json.Nodes;
using SAM.Core;
using Xunit;

namespace SAM.Tests
{
    public class SAMFileExportTests
    {
        [Fact]
        public void ToFile_SAM_ReturnsFalse_ForEmptyPayload()
        {
            string path = TemporarySAMPath();

            bool result = SAM.Core.Convert.ToFile(new List<IJSAMObject>(), path, SAMFileType.SAM);

            Assert.False(result);
            Assert.False(File.Exists(path));
        }

        [Fact]
        public void ToFile_SAM_ReturnsFalse_WhenNoObjectSerializes()
        {
            string path = TemporarySAMPath();

            bool result = SAM.Core.Convert.ToFile(new IJSAMObject[] { null!, new NonSerializableObject() }, path, SAMFileType.SAM);

            Assert.False(result);
            Assert.False(File.Exists(path));
        }

        [Fact]
        public void ToFile_SAM_WritesPayloadAndArchiveInfo_ForSerializableObject()
        {
            string path = TemporarySAMPath();

            bool result = SAM.Core.Convert.ToFile(new Message(MessageType.Information, "test export"), path, SAMFileType.SAM);

            Assert.True(result);
            Assert.True(File.Exists(path));

            using (ZipArchive zipArchive = ZipFile.OpenRead(path))
            {
                Assert.Equal(2, zipArchive.Entries.Count);
                Assert.NotNull(zipArchive.GetEntry(ZipArchiveInfo.EntryName));
            }

            List<IJSAMObject> objects = SAM.Core.Convert.ToSAM(path);
            Message message = Assert.IsType<Message>(Assert.Single(objects));
            JsonObject jsonObject = message.ToJsonObject();
            Assert.Equal("test export", jsonObject["Text"]?.GetValue<string>());
        }

        private static string TemporarySAMPath()
        {
            return Path.Combine(Path.GetTempPath(), string.Format("{0}.sam", Guid.NewGuid()));
        }

        private sealed class NonSerializableObject : IJSAMObject
        {
            public bool FromJsonObject(JsonObject? jsonObject)
            {
                return false;
            }

            public JsonObject? ToJsonObject()
            {
                return null;
            }
        }
    }
}
