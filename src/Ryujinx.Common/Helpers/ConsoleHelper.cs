using Ryujinx.Common.Logging;
using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Ryujinx.Common.Helper
{
    public static partial class ConsoleHelper
    {
        [SupportedOSPlatform("windows")]
        [LibraryImport("kernel32")]
        private static partial nint GetConsoleWindow();

        [SupportedOSPlatform("windows")]
        [LibraryImport("user32")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool ShowWindow(nint hWnd, int nCmdShow);

        [SupportedOSPlatform("windows")]
        [LibraryImport("user32")]
        private static partial nint GetForegroundWindow();
        
        [SupportedOSPlatform("windows")]
        [LibraryImport("user32")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool SetForegroundWindow(nint hWnd);

        public static bool SetConsoleWindowStateSupported => OperatingSystem.IsWindows();

        public static void SetConsoleWindowState(bool show)
        {
            if (OperatingSystem.IsWindows())
            {
                SetConsoleWindowStateWindows(show);
            }
            else if (show == false)
            {
                Logger.Warning?.Print(LogClass.Application, "OS doesn't support hiding console window");
            }
        }

        [SupportedOSPlatform("windows")]
        private static void SetConsoleWindowStateWindows(bool show)
        {
            const int SW_HIDE = 0;
            const int SW_SHOW = 5;

            nint hWnd = GetConsoleWindow();

            if (hWnd == nint.Zero)
            {
                Logger.Warning?.Print(LogClass.Application, "Attempted to show/hide console window but console window does not exist");
                return;
            }

            SetForegroundWindow(hWnd);

            hWnd = GetForegroundWindow();

            ShowWindow(hWnd, show ? SW_SHOW : SW_HIDE);
        }
    }
}
