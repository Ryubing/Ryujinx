using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.Versioning;

namespace Ryujinx.Common.Helper
{
    [SupportedOSPlatform("linux")]
    public static class LinuxHelper
    {
        // NOTE: This value was determined by manual tests and might need to be increased again.
        public const int RecommendedVmMaxMapCount = 524288;
        public const string VmMaxMapCountPath = "/proc/sys/vm/max_map_count";
        public const string SysCtlConfigPath = "/etc/sysctl.d/99-Ryujinx.conf";
        public static int VmMaxMapCount => int.Parse(File.ReadAllText(VmMaxMapCountPath));
        public static string PkExecPath { get; } = GetBinaryPath("pkexec");

        private static string GetBinaryPath(string binary)
        {
            string pathVar = Environment.GetEnvironmentVariable("PATH");

            if (pathVar is null || string.IsNullOrEmpty(binary))
            {
                return null;
            }

            foreach (string searchPath in pathVar.Split(":", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
            {
                string binaryPath = Path.Combine(searchPath, binary);

                if (File.Exists(binaryPath))
                {
                    return binaryPath;
                }
            }

            return null;
        }

        public static int RunPkExec(string command)
        {
            if (PkExecPath == null)
            {
                return 1;
            }

            using Process process = new()
            {
                StartInfo =
                {
                    FileName = PkExecPath,
                    ArgumentList = { "sh", "-c", command },
                },
            };

            process.Start();
            process.WaitForExit();

            return process.ExitCode;
        }
    }
}
