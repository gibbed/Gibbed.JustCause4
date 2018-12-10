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
using Gibbed.JustCause4.FileFormats;
using NDesk.Options;

namespace RebuildFileLists
{
    internal class Program
    {
        private static string GetExecutableName()
        {
            return Path.GetFileName(System.Reflection.Assembly.GetExecutingAssembly().CodeBase);
        }

        private static string GetListPath(string installPath, string inputPath)
        {
            installPath = installPath.ToLowerInvariant();
            inputPath = inputPath.ToLowerInvariant();

            if (inputPath.StartsWith(installPath) == false)
            {
                return null;
            }

            var baseName = inputPath.Substring(installPath.Length + 1);

            string outputPath;
            outputPath = Path.Combine("files", baseName);
            outputPath = Path.ChangeExtension(outputPath, ".filelist");
            return outputPath;
        }

        public static void Main(string[] args)
        {
            bool showHelp = false;
            string currentProject = null;

            var options = new OptionSet()
            {
                { "h|help", "show this message and exit", v => showHelp = v != null },
                { "p|project=", "override current project", v => currentProject = v },
            };

            List<string> extras;

            try
            {
                extras = options.Parse(args);
            }
            catch (OptionException e)
            {
                Console.Write("{0}: ", GetExecutableName());
                Console.WriteLine(e.Message);
                Console.WriteLine("Try `{0} --help' for more information.", GetExecutableName());
                return;
            }

            if (extras.Count != 0 || showHelp == true)
            {
                Console.WriteLine("Usage: {0} [OPTIONS]+", GetExecutableName());
                Console.WriteLine();
                Console.WriteLine("Options:");
                options.WriteOptionDescriptions(Console.Out);
                return;
            }

            Console.WriteLine("Loading project...");

            var manager = Gibbed.ProjectData.Manager.Load(currentProject);
            if (manager.ActiveProject == null)
            {
                Console.WriteLine("Nothing to do: no active project loaded.");
                return;
            }

            var project = manager.ActiveProject;

            var hashes = manager.LoadFileLists();
            GuessExtensions(hashes);

            var installPath = project.InstallPath;
            var listsPath = project.ListsPath;

            if (installPath == null)
            {
                Console.WriteLine("Could not detect install path.");
                return;
            }

            if (listsPath == null)
            {
                Console.WriteLine("Could not detect lists path.");
                return;
            }

            Console.WriteLine("Searching for archives...");
            var inputPaths = new List<string>();

            var locations = new Dictionary<string, string>()
            {
                { "archives_win64", "game*.tab" },
                { "dlc", "*.tab" },
                { "patch_win64", "*.tab" },
            };

            foreach (var kv in locations)
            {
                var locationPath = Path.Combine(installPath, kv.Key);

                if (Directory.Exists(locationPath) == true)
                {
                    inputPaths.AddRange(Directory.GetFiles(locationPath, kv.Value, SearchOption.AllDirectories));
                }
            }

            var outputPaths = new List<string>();

            var breakdown = new Breakdown();
            var allNames = new List<string>();

            Console.WriteLine("Processing...");
            foreach (var inputPath in inputPaths)
            {
                var outputPath = GetListPath(installPath, inputPath);
                if (outputPath == null)
                {
                    throw new InvalidOperationException();
                }

                Console.WriteLine(outputPath);
                outputPath = Path.Combine(listsPath, outputPath);

                if (outputPaths.Contains(outputPath) == true)
                {
                    throw new InvalidOperationException();
                }

                outputPaths.Add(outputPath);

                var tab = new ArchiveTableFile();

                if (File.Exists(inputPath + ".bak") == true)
                {
                    using (var input = File.OpenRead(inputPath + ".bak"))
                    {
                        tab.Deserialize(input);
                    }
                }
                else
                {
                    using (var input = File.OpenRead(inputPath))
                    {
                        tab.Deserialize(input);
                    }
                }

                var localBreakdown = new Breakdown();

                var names = new List<string>();
                foreach (var nameHash in tab.Entries.Select(kv => kv.NameHash).Distinct())
                {
                    var name = hashes[nameHash];
                    if (name != null)
                    {
                        if (names.Contains(name) == false)
                        {
                            names.Add(name);
                            localBreakdown.Known++;
                        }

                        if (allNames.Contains(name) == false)
                        {
                            allNames.Add(name);
                        }
                    }

                    localBreakdown.Total++;
                }

                breakdown.Known += localBreakdown.Known;
                breakdown.Total += localBreakdown.Total;

                names.Sort();

                var outputParentPath = Path.GetDirectoryName(outputPath);
                if (string.IsNullOrEmpty(outputParentPath) == false)
                {
                    Directory.CreateDirectory(outputParentPath);
                }

                using (var output = new StreamWriter(outputPath))
                {
                    output.WriteLine("; {0}", localBreakdown);

                    foreach (string name in names)
                    {
                        output.WriteLine(name);
                    }
                }
            }

            using (var output = File.Create(Path.Combine(listsPath, "files", "status.txt")))
            using (var writer = new StreamWriter(output))
            {
                writer.WriteLine("{0}", breakdown);
            }
        }

        private static void GuessExtensions(Gibbed.ProjectData.HashList<uint> hashes)
        {
            var extensionGroups = new[]
            {
                new[]
                {
                    //".ddsc", // Causes unnecessary collisions.
                    ".atx1", ".atx2", /*".atx3", ".atx4", ".atx5", ".atx6", ".atx7", ".atx8", ".atx9",*/
                },
                new[]
                {
                    ".ee", ".epe", ".epe_adf", ".wtunec", ".ftunec",
                    null,
                    ".resourcebundle",
                },
                new[]
                {
                    ".bl", ".blo", ".blo_adf", ".blo.mdic",
                    ".fl", ".flo", ".flo_adf", ".fl.mdic",
                    ".nl", ".nl.mdic",
                    ".pfx_breakablecompoundc",
                    ".pfx_staticcompoundc",
                    ".obc",
                    null,
                    ".resourcebundle",
                },
            };

            var oldNames = hashes.GetStrings().ToArray();
            foreach (var oldName in oldNames)
            {
                foreach (var extensionGroup in extensionGroups)
                {
                    string oldExtension = null;
                    int oldIndex = -1;
                    for (int i = 0; i < extensionGroup.Length; i++)
                    {
                        var extension = extensionGroup[i];
                        if (extension == null)
                        {
                            break;
                        }

                        if (oldName.EndsWith(extension) == true)
                        {
                            oldExtension = extension;
                            oldIndex = i;
                            break;
                        }
                    }

                    if (oldExtension == null)
                    {
                        continue;
                    }

                    for (int i = 0; i < extensionGroup.Length; i++)
                    {
                        if (i == oldIndex)
                        {
                            continue;
                        }

                        var newExtension = extensionGroup[i];
                        if (newExtension == null)
                        {
                            continue;
                        }

                        var newName = oldName.Substring(0, oldName.Length - oldExtension.Length) + newExtension;
                        var newHash = newName.HashJenkins();

                        if (hashes.Contains(newHash) == true)
                        {
                            var existingName = hashes[newHash];
                            if (existingName != newName)
                            {
                                throw new InvalidOperationException(
                                string.Format(
                                    "hash collision ('{0}' vs '{1}')",
                                    existingName,
                                    newName));
                            }
                        }
                        else
                        {
                            hashes.Add(newHash, newName);
                        }
                    }
                }
            }
        }

        private class Breakdown
        {
            public long Known = 0;
            public long Total = 0;

            public int Percent
            {
                get
                {
                    if (this.Total == 0)
                    {
                        return 0;
                    }

                    return (int)Math.Floor(((float)this.Known /
                                            (float)this.Total) * 100.0);
                }
            }

            public override string ToString()
            {
                return string.Format("{0}/{1} ({2}%)",
                                     this.Known,
                                     this.Total,
                                     this.Percent);
            }
        }
    }
}
