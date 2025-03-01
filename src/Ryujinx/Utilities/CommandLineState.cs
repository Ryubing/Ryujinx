using Gommon;
using Ryujinx.Common.Logging;
using System.Collections.Generic;

namespace Ryujinx.Ava.Utilities
{
    public static class CommandLineState
    {
        public static string[] Arguments { get; private set; }
        public static int CountArguments { get; private set; }
        public static bool? OverrideDockedMode { get; private set; }
        public static bool? OverrideHardwareAcceleration { get; private set; }
        public static string OverrideGraphicsBackend { get; private set; }
        public static string OverrideBackendThreading { get; private set; }
        public static string OverrideBackendThreadingAfterReboot { get; private set; }
        public static string OverridePPTC { get; private set; }
        public static string OverrideMemoryManagerMode { get; private set; } 
        public static string OverrideSystemRegion { get; private set; } 
        public static string OverrideSystemLanguage { get; private set; } 
        public static string OverrideHideCursor { get; private set; }
        public static string BaseDirPathArg { get; private set; }
        public static FilePath FirmwareToInstallPathArg { get; set; }
        public static string Profile { get; private set; }
        public static string LaunchPathArg { get; private set; }
        public static string LaunchApplicationId { get; private set; }
        public static bool StartFullscreenArg { get; private set; }
        public static bool HideAvailableUpdates { get; private set; }


        public static void ParseArguments(string[] args)
        {
            List<string> arguments = [];

            // Parse Arguments.
            for (int i = 0; i < args.Length; ++i)
            {
                string arg = args[i];

                if (arg.Contains("-") || arg.Contains("--"))
                {
                    CountArguments++; 
                }

                switch (arg)
                {
                    case "-r":
                    case "--root-data-dir":
                        if (i + 1 >= args.Length)
                        {
                            Logger.Error?.Print(LogClass.Application, $"Invalid option '{arg}'");

                            continue;
                        }

                        BaseDirPathArg = args[++i];

                        arguments.Add(arg);
                        arguments.Add(args[i]);
                        break;
                    case "--install-firmware":
                        if (i + 1 >= args.Length)
                        {
                            Logger.Error?.Print(LogClass.Application, $"Invalid option '{arg}'");

                            continue;
                        }

                        FirmwareToInstallPathArg = new FilePath(args[++i]);

                        arguments.Add(arg);
                        arguments.Add(args[i]);
                        break;
                    case "-p":
                    case "--profile":
                        if (i + 1 >= args.Length)
                        {
                            Logger.Error?.Print(LogClass.Application, $"Invalid option '{arg}'");

                            continue;
                        }

                        Profile = args[++i];

                        arguments.Add(arg);
                        arguments.Add(args[i]);
                        break;
                    case "-f":
                    case "--fullscreen":
                        StartFullscreenArg = true;

                        arguments.Add(arg);
                        break;
                    case "-g":
                    case "--graphics-backend":
                        if (i + 1 >= args.Length)
                        {
                            Logger.Error?.Print(LogClass.Application, $"Invalid option '{arg}'");

                            continue;
                        }

                        OverrideGraphicsBackend = args[++i];
                        break;
                    case "--backend-threading":
                        if (i + 1 >= args.Length)
                        {
                            Logger.Error?.Print(LogClass.Application, $"Invalid option '{arg}'");

                            continue;
                        }

                        OverrideBackendThreading = args[++i];
                        break;
                    case "--bt":
                        if (i + 1 >= args.Length)
                        {
                            Logger.Error?.Print(LogClass.Application, $"Invalid option '{arg}'");

                            continue;
                        }

                        OverrideBackendThreadingAfterReboot = args[++i];
                        break;
                    case "--pptc":
                        if (i + 1 >= args.Length)
                        {
                            Logger.Error?.Print(LogClass.Application, $"Invalid option '{arg}'");

                            continue;
                        }

                        OverridePPTC = args[++i];
                        break;
                    case "-m":
                    case "--memory-manager-mode":
                        if (i + 1 >= args.Length)
                        {
                            Logger.Error?.Print(LogClass.Application, $"Invalid option '{arg}'");

                            continue;
                        }

                        OverrideMemoryManagerMode = args[++i];
                        break;
                    case "--system-region":
                        if (i + 1 >= args.Length)
                        {
                            Logger.Error?.Print(LogClass.Application, $"Invalid option '{arg}'");

                            continue;
                        }

                        OverrideSystemRegion = args[++i];
                        break;
                    case "--system-language":
                        if (i + 1 >= args.Length)
                        {
                            Logger.Error?.Print(LogClass.Application, $"Invalid option '{arg}'");

                            continue;
                        }

                        OverrideSystemLanguage = args[++i];
                        break;
                    case "-i":
                    case "--application-id":
                        LaunchApplicationId = args[++i];
                        break;
                    case "--docked-mode":
                        OverrideDockedMode = true;
                        break;
                    case "--handheld-mode":
                        OverrideDockedMode = false;
                        break;
                    case "--hide-cursor":
                        if (i + 1 >= args.Length)
                        {
                            Logger.Error?.Print(LogClass.Application, $"Invalid option '{arg}'");

                            continue;
                        }

                        OverrideHideCursor = args[++i];
                        break;
                    case "--hide-updates":
                        HideAvailableUpdates = true;
                        break;
                    case "--software-gui":
                        OverrideHardwareAcceleration = false;
                        break;
                    default:
                        LaunchPathArg = arg;
                        break;
                }
            }

            Arguments = arguments.ToArray();
        }
    }
}
