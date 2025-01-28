using Gommon;
using Ryujinx.Common.Logging;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace Ryujinx.Common.Helper
{
    public static partial class OpenHelper
    {
        [LibraryImport("shell32.dll", SetLastError = true)]
        private static partial int SHOpenFolderAndSelectItems(nint pidlFolder, uint cidl, nint apidl, uint dwFlags);

        [LibraryImport("shell32.dll", SetLastError = true)]
        private static partial void ILFree(nint pidlList);

        [LibraryImport("shell32.dll", SetLastError = true)]
        private static partial nint ILCreateFromPathW([MarshalAs(UnmanagedType.LPWStr)] string pszPath);

        public static void OpenFolder(string path)
        {
            if (Directory.Exists(path))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = path,
                    UseShellExecute = true,
                    Verb = "open",
                });
            }
            else
            {
                Logger.Notice.Print(LogClass.Application, $"Directory \"{path}\" doesn't exist!");
            }
        }

        public static void OpenFolder(FilePath path) => OpenFolder(path.Path);

        public static void LocateFile(string path)
        {
            if (File.Exists(path))
            {
                if (OperatingSystem.IsWindows())
                {
                    nint pidlList = ILCreateFromPathW(path);
                    if (pidlList != nint.Zero)
                    {
                        try
                        {
                            Marshal.ThrowExceptionForHR(SHOpenFolderAndSelectItems(pidlList, 0, nint.Zero, 0));
                        }
                        finally
                        {
                            ILFree(pidlList);
                        }
                    }
                }
                else if (OperatingSystem.IsMacOS())
                {
                    ObjectiveC.NSString nsStringPath = new(path);
                    ObjectiveC.Object nsUrl = new("NSURL");
                    ObjectiveC.Object urlPtr = nsUrl.GetFromMessage("fileURLWithPath:", nsStringPath);

                    ObjectiveC.Object nsArray = new("NSArray");
                    ObjectiveC.Object urlArray = nsArray.GetFromMessage("arrayWithObject:", urlPtr);

                    ObjectiveC.Object nsWorkspace = new("NSWorkspace");
                    ObjectiveC.Object sharedWorkspace = nsWorkspace.GetFromMessage("sharedWorkspace");

                    sharedWorkspace.SendMessage("activateFileViewerSelectingURLs:", urlArray);
                }
                else if (OperatingSystem.IsLinux())
                {
                    Process.Start("dbus-send", $"--session --print-reply --dest=org.freedesktop.FileManager1 --type=method_call /org/freedesktop/FileManager1 org.freedesktop.FileManager1.ShowItems array:string:\"file://{path}\" string:\"\"");
                }
                else
                {
                    OpenFolder(Path.GetDirectoryName(path));
                }
            }
            else
            {
                Logger.Notice.Print(LogClass.Application, $"File \"{path}\" doesn't exist!");
            }
        }

        public static void OpenUrl(string url)
        {
            if (OperatingSystem.IsWindows())
            {
                Process.Start(new ProcessStartInfo("cmd", $"/c start {url.Replace("&", "^&")}"));
            }
            else if (OperatingSystem.IsLinux())
            {
                Process.Start("xdg-open", url);
            }
            else if (OperatingSystem.IsMacOS())
            {
                ObjectiveC.NSString nsStringPath = new(url);
                ObjectiveC.Object nsUrl = new("NSURL");
                ObjectiveC.Object urlPtr = nsUrl.GetFromMessage("URLWithString:", nsStringPath);

                ObjectiveC.Object nsWorkspace = new("NSWorkspace");
                ObjectiveC.Object sharedWorkspace = nsWorkspace.GetFromMessage("sharedWorkspace");

                sharedWorkspace.GetBoolFromMessage("openURL:", urlPtr);
            }
            else
            {
                Logger.Notice.Print(LogClass.Application, $"Cannot open url \"{url}\" on this platform!");
            }
        }
    }
}
