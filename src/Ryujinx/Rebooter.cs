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

                var dialogTask = taskDialog.ShowAsync(true);
                await Task.Delay(500);

                // Find the process name.
                string ryuName = Path.GetFileName(Environment.ProcessPath) ?? string.Empty;

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

                Environment.Exit(0);
            }
        }
    }
}
