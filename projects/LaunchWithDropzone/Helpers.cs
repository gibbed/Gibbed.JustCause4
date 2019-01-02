using System;
using MessageBox = System.Windows.Forms.MessageBox;
using MessageBoxButtons = System.Windows.Forms.MessageBoxButtons;
using MessageBoxIcon = System.Windows.Forms.MessageBoxIcon;
using ProcessStartInfo = System.Diagnostics.ProcessStartInfo;
using Process = System.Diagnostics.Process;
using DialogResult = System.Windows.Forms.DialogResult;
using OpenFileDialog = System.Windows.Forms.OpenFileDialog;
using SecurityException = System.Security.SecurityException;
using Microsoft.Win32;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LaunchWithDropzone
{
    internal class Helpers
    {
        public static string Quote(string path)
        {
            int index = path.IndexOf('"');
            if (index < 0)
            {
                return path;
            }

            return "\"" + path.Replace("\"", "\\\"") + "\"";
        }

        public static void ShowError(string message)
        {
            MessageBox.Show(message, "Launch With Dropzone", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        public static string GetGamePathFromOpenFileDialog()
        {
            using (var openFileDialog = new OpenFileDialog()
            {
                AutoUpgradeEnabled = true,
                CheckFileExists = true,
                CheckPathExists = true,
                DefaultExt = ".exe",
                Filter = "JustCause4.exe (JustCause4.exe)|JustCause4.exe",
                RestoreDirectory = true,
                Title = "Select JustCase4.exe...",
            })
            {
                return openFileDialog.ShowDialog() != DialogResult.OK
                           ? null
                           : Path.GetFullPath(openFileDialog.FileName);
            }
        }

        public static string GetGamePathFromRegistry()
        {
            var installPath = GetGameLocationFromRegistry();
            return string.IsNullOrEmpty(installPath) == false
                       ? Path.Combine(installPath, "JustCause4.exe")
                       : null;
        }

        public static string GetGameLocationFromRegistry()
        {
            RegistryKey baseKey = null;
            RegistryKey subKey = null;
            try
            {
                baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
                subKey = baseKey.OpenSubKey(
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Steam App 517630");
                if (subKey == null)
                {
                    return null;
                }
                return (string)subKey.GetValue("InstallLocation", null);
            }
            catch (SecurityException)
            {
                return null;
            }
            finally
            {
                if (subKey != null)
                {
                    baseKey.Dispose();
                }

                if (baseKey != null)
                {
                    baseKey.Dispose();
                }
            }
        }

        public static bool StartProcess(string path, string name, IEnumerable<string> arguments)
        {
            var processStartInfo = new ProcessStartInfo()
            {
                WorkingDirectory = path,
                FileName = Path.Combine(path, name),
                Arguments = string.Join(" ", arguments),
                UseShellExecute = true,
            };
            using (var process = Process.Start(processStartInfo))
            {
                return process != null;
            }
        }
    }
}
