using FluentAvalonia.UI.Controls;
using Ryujinx.Ava.Common.Locale;
using Ryujinx.Ava.UI.ViewModels;
using Ryujinx.Ava.Utilities;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Ryujinx.Ava
{
    internal static class Rebooter
    {

        private static readonly string _updateDir = Path.Combine(Path.GetTempPath(), "Ryujinx", "update");


        public static void RebootAppWithGame(string gamePath, List<string> args)
        {
            _ = Reboot(gamePath, args);

        }

        private static async Task Reboot(string gamePath, List<string> args)
        {

            bool shouldRestart = true;

            TaskDialog taskDialog = new()
            {
                Header = LocaleManager.Instance[LocaleKeys.RyujinxRebooter],
                SubHeader = LocaleManager.Instance[LocaleKeys.DialogRebooterMessage],
                IconSource = new SymbolIconSource { Symbol = Symbol.Games },
                XamlRoot = RyujinxApp.MainWindow,
            };

            if (shouldRestart)
            {
                List<string> arguments = CommandLineState.Arguments.ToList();
                string executableDirectory = AppDomain.CurrentDomain.BaseDirectory;

                // On macOS we perform the update at relaunch.
                if (OperatingSystem.IsMacOS())
                {
                    string baseBundlePath = Path.GetFullPath(Path.Combine(executableDirectory, "..", ".."));
                    string newBundlePath = Path.Combine(_updateDir, "Ryujinx.app");
                    string updaterScriptPath = Path.Combine(newBundlePath, "Contents", "Resources", "updater.sh");
                    string currentPid = Environment.ProcessId.ToString();

                    arguments.InsertRange(0, new List<string> { updaterScriptPath, baseBundlePath, newBundlePath, currentPid });
                    Process.Start("/bin/bash", arguments);
                }
                else
                {
                    var dialogTask = taskDialog.ShowAsync(true);
                    await Task.Delay(500);

                    // Find the process name.
                    string ryuName = Path.GetFileName(Environment.ProcessPath) ?? string.Empty;

                    // Some operating systems can see the renamed executable, so strip off the .ryuold if found.
                    if (ryuName.EndsWith(".ryuold"))
                    {
                        ryuName = ryuName[..^7];
                    }

                    // Fallback if the executable could not be found.
                    if (ryuName.Length == 0 || !Path.Exists(Path.Combine(executableDirectory, ryuName)))
                    {
                        ryuName = OperatingSystem.IsWindows() ? "Ryujinx.exe" : "Ryujinx";
                    }

                    ProcessStartInfo processStart = new(ryuName)
                    {
                        UseShellExecute = true,
                        WorkingDirectory = executableDirectory,
                    };

                    foreach (var arg in args)
                    {
                         processStart.ArgumentList.Add(arg);
                    }

                     processStart.ArgumentList.Add(gamePath);

                     Process.Start(processStart);
                }
                Environment.Exit(0);
            }
        }
    }
}
