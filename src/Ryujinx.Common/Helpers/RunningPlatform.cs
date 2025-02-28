using System;
using System.Runtime.InteropServices;
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable InconsistentNaming

namespace Ryujinx.Common.Helper
{
    public enum OperatingSystemType
    {
        MacOS,
        Linux,
        Windows   
    }
    
    public static class RunningPlatform
    {
        public static readonly OperatingSystemType CurrentOS 
            = IsMacOS 
                ? OperatingSystemType.MacOS 
                : IsWindows
                    ? OperatingSystemType.Windows
                    : IsLinux
                        ? OperatingSystemType.Linux
                        : throw new PlatformNotSupportedException();

        public static Architecture Architecture => RuntimeInformation.OSArchitecture;
        public static Architecture CurrentProcessArchitecture => RuntimeInformation.ProcessArchitecture;
        
        public static bool IsMacOS => OperatingSystem.IsMacOS();
        public static bool IsWindows => OperatingSystem.IsWindows();
        public static bool IsLinux => OperatingSystem.IsLinux();
        
        public static bool IsArm => Architecture is Architecture.Arm64;
        
        public static bool IsX64 => Architecture is Architecture.X64;

        public static bool IsIntelMac => IsMacOS && IsX64;
        public static bool IsArmMac => IsMacOS && IsArm;
        
        public static bool IsX64Windows => IsWindows && IsX64;
        public static bool IsArmWindows => IsWindows && IsArm;
        
        public static bool IsX64Linux => IsLinux && IsX64;
        public static bool IsArmLinux => IsLinux && IsArmMac;
    }
}
