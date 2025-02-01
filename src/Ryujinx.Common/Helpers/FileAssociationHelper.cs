using Microsoft.Win32;
using Ryujinx.Common.Logging;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Ryujinx.Common.Helper
{
    public static partial class FileAssociationHelper
    {
        private static readonly string[] _fileExtensions = [".nca", ".nro", ".nso", ".nsp", ".xci"];

        [SupportedOSPlatform("linux")]
        private static readonly string _mimeDbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "share", "mime");

        private const int SHCNE_ASSOCCHANGED = 0x8000000;
        private const int SHCNF_FLUSH = 0x1000;

        [LibraryImport("shell32.dll", SetLastError = true)]
        public static partial void SHChangeNotify(uint wEventId, uint uFlags, nint dwItem1, nint dwItem2);

        public static bool IsTypeAssociationSupported => (OperatingSystem.IsLinux() || OperatingSystem.IsWindows());
        
        public static bool AreMimeTypesRegistered
        {
            get
            {
                if (OperatingSystem.IsLinux())
                {
                    return AreMimeTypesRegisteredLinux();
                }

                if (OperatingSystem.IsWindows())
                {
                    return AreMimeTypesRegisteredWindows();
                }

                // TODO: Add macOS support.

                return false;
            }
        }

        [SupportedOSPlatform("linux")]
        private static bool AreMimeTypesRegisteredLinux() => File.Exists(Path.Combine(_mimeDbPath, "packages", "Ryujinx.xml"));

        [SupportedOSPlatform("linux")]
        private static bool InstallLinuxMimeTypes(bool uninstall = false)
        {
            string installKeyword = uninstall ? "uninstall" : "install";

            if ((uninstall && AreMimeTypesRegisteredLinux()) || (!uninstall && !AreMimeTypesRegisteredLinux()))
            {
                string mimeTypesFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "mime", "Ryujinx.xml");
                string additionalArgs = !uninstall ? "--novendor" : string.Empty;

                using Process mimeProcess = new();

                mimeProcess.StartInfo.FileName = "xdg-mime";
                mimeProcess.StartInfo.Arguments = $"{installKeyword} {additionalArgs} --mode user {mimeTypesFile}";

                mimeProcess.Start();
                mimeProcess.WaitForExit();

                if (mimeProcess.ExitCode != 0)
                {
                    Logger.Error?.PrintMsg(LogClass.Application, $"Unable to {installKeyword} mime types. Make sure xdg-utils is installed. Process exited with code: {mimeProcess.ExitCode}");

                    return false;
                }

                using Process updateMimeProcess = new();

                updateMimeProcess.StartInfo.FileName = "update-mime-database";
                updateMimeProcess.StartInfo.Arguments = _mimeDbPath;

                updateMimeProcess.Start();
                updateMimeProcess.WaitForExit();

                if (updateMimeProcess.ExitCode != 0)
                {
                    Logger.Error?.PrintMsg(LogClass.Application, $"Could not update local mime database. Process exited with code: {updateMimeProcess.ExitCode}");
                }
            }

            return true;
        }

        [SupportedOSPlatform("windows")]
        private static bool AreMimeTypesRegisteredWindows()
        {
            return _fileExtensions.Aggregate(false, 
                (current, ext) => current | CheckRegistering(ext)
            );
            
            static bool CheckRegistering(string ext)
            {
                RegistryKey key = Registry.CurrentUser.OpenSubKey(@$"Software\Classes\{ext}");

                RegistryKey openCmd = key?.OpenSubKey(@"shell\open\command");
                
                if (openCmd is null)
                {
                    return false;
                }
                
                string keyValue = (string)openCmd.GetValue(string.Empty);

                return keyValue is not null && (keyValue.Contains("Ryujinx") || keyValue.Contains(AppDomain.CurrentDomain.FriendlyName));
            }
        }

        [SupportedOSPlatform("windows")]
        private static bool InstallWindowsMimeTypes(bool uninstall = false)
        {
            bool registered = _fileExtensions.Aggregate(false, 
                (current, ext) => current | RegisterExtension(ext, uninstall)
            );

            // Notify Explorer the file association has been changed.
            SHChangeNotify(SHCNE_ASSOCCHANGED, SHCNF_FLUSH, nint.Zero, nint.Zero);

            return registered;
            
            static bool RegisterExtension(string ext, bool uninstall = false)
            {
                string keyString = @$"Software\Classes\{ext}";

                if (uninstall)
                {
                    // If the types don't already exist, there's nothing to do, and we can call this operation successful.
                    if (!AreMimeTypesRegisteredWindows())
                    {
                        return true;
                    }
                    Logger.Debug?.Print(LogClass.Application, $"Removing type association {ext}");
                    Registry.CurrentUser.DeleteSubKeyTree(keyString);
                    Logger.Debug?.Print(LogClass.Application, $"Removed type association {ext}");
                }
                else
                {
                    using RegistryKey key = Registry.CurrentUser.CreateSubKey(keyString);

                    if (key is null)
                    {
                        return false;
                    }

                    Logger.Debug?.Print(LogClass.Application, $"Adding type association {ext}");
                    using RegistryKey openCmd = key.CreateSubKey(@"shell\open\command");
                    openCmd.SetValue(string.Empty, $"\"{Environment.ProcessPath}\" \"%1\"");
                    Logger.Debug?.Print(LogClass.Application, $"Added type association {ext}");

                }

                return true;
            }
        }

        public static bool Install()
        {
            if (OperatingSystem.IsLinux())
            {
                return InstallLinuxMimeTypes();
            }

            if (OperatingSystem.IsWindows())
            {
                return InstallWindowsMimeTypes();
            }

            // TODO: Add macOS support.

            return false;
        }

        public static bool Uninstall()
        {
            if (OperatingSystem.IsLinux())
            {
                return InstallLinuxMimeTypes(true);
            }

            if (OperatingSystem.IsWindows())
            {
                return InstallWindowsMimeTypes(true);
            }

            // TODO: Add macOS support.

            return false;
        }
    }
}
