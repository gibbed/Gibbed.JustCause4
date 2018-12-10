/* Copyright (c) 2018 Rick (rick 'at' gibbed 'dot' us)
 * 
 * This software is provided 'as-is', without any express or implied
 * warranty. In no event will the authors be held liable for any damages
 * arising from the use of this software.
 * 
 * Permission is granted to anyone to use this software for any purpose,
 * including commercial applications, and to alter it and redistribute it
 * freely, subject to the following restrictions:
 * 
 * 1. The origin of this software must not be misrepresented; you must not
 *    claim that you wrote the original software. If you use this software
 *    in a product, an acknowledgment in the product documentation would
 *    be appreciated but is not required.
 * 
 * 2. Altered source versions must be plainly marked as such, and must not
 *    be misrepresented as being the original software.
 * 
 * 3. This notice may not be removed or altered from any source
 *    distribution.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LaunchWithDropzone
{
    internal class Program
    {
        [STAThreadAttribute]
        private static void Main(string[] args)
        {
            if (args.Length > 1)
            {
                Helpers.ShowError("Invalid command-line arguments.");
                return;
            }

            var installPath = Helpers.GetInstallPathFromRegistry();
            if (string.IsNullOrEmpty(installPath) == true)
            {
                installPath = Helpers.GetInstallPathFromOpenFileDialog();
            }

            if (string.IsNullOrEmpty(installPath) == true)
            {
                return;
            }

            string language = "eng";
            if (args.Length > 0)
            {
                var languages = new[]
                {
                    "eng", "fre", "ger", "ita", "spa", "rus", "pol",
                    "jap", "bra", "mex", "ara", "kor", "tzh", "szh",
                };
                var candidate = args[0].ToLowerInvariant();
                if (languages.Contains(candidate) == false)
                {
                    Helpers.ShowError("Invalid command-line argument.\nValid choices:" + string.Join("|", languages));
                    return;
                }
                language = candidate;
            }

            var defaultSources = new[]
            {
                new VFSArchive(Path.Combine("archives_win64", "boot"), 1000),
                new VFSArchive(Path.Combine("archives_win64", "boot_patch"), 999),
                new VFSArchive(Path.Combine("archives_win64", "main"), 1000),
                new VFSArchive(Path.Combine("archives_win64", "main_patch"), 999),
            };

            var sources = new List<VFSSource>();
            sources.Add(new VFSFileSystem("dropzone", -1000));
            foreach (var source in defaultSources)
            {
                sources.Add(source);
                sources.Add(new VFSArchive(Path.Combine(source.Path, "hires"), source.Priority));
                sources.Add(new VFSArchive(Path.Combine(source.Path, language), source.Priority));
                sources.Add(new VFSArchive(Path.Combine(source.Path, "hires", language), source.Priority));
            }

            var archivesPath = Path.Combine(installPath, "archives_win64");
            foreach (var contentPackPath in Directory.GetDirectories(archivesPath, "cp_*"))
            {
                var contentPackName = Path.GetFileName(contentPackPath);
                if (string.IsNullOrEmpty(contentPackName) == true)
                {
                    continue;
                }
                sources.Add(new VFSArchive(Path.Combine("archives_win64", contentPackName), 1001));
                sources.Add(new VFSArchive(Path.Combine("archives_win64", contentPackName, language), 1001));
            }

            Helpers.StartProcess(
                installPath,
                "JustCause4.exe",
                sources.OrderBy(s => s.Priority).Select(s => s.GetCommandLine()));
        }

        private abstract class VFSSource
        {
            private readonly int _Priority;

            public VFSSource(int priority)
            {
                this._Priority = priority;
            }

            public int Priority
            {
                get { return this._Priority; }
            }

            public abstract string GetCommandLine();
        }

        private class VFSFileSystem : VFSSource
        {
            private readonly string _Path;

            public VFSFileSystem(string path, int priority)
                : base(priority)
            {
                this._Path = path;
            }

            public string Path
            {
                get { return this._Path; }
            }

            public override string GetCommandLine()
            {
                return string.Format("--vfs-fs {0}", Helpers.Quote(this._Path));
            }
        }

        private class VFSArchive : VFSSource
        {
            private readonly string _Path;

            public VFSArchive(string path, int priority)
                : base(priority)
            {
                this._Path = path;
            }

            public string Path
            {
                get { return this._Path; }
            }

            public override string GetCommandLine()
            {
                return string.Format("--vfs-archive {0}", Helpers.Quote(this._Path));
            }
        }
    }
}
