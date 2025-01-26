using System;
using System.Runtime.InteropServices;
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable InconsistentNaming

namespace Ryujinx.Common.Helper
{
    public static class RunningPlatform
    {
        public static bool IsMacOS => OperatingSystem.IsMacOS();
        public static bool IsWindows => OperatingSystem.IsWindows();
        public static bool IsLinux => OperatingSystem.IsLinux();

        public static bool IsIntelMac => IsMacOS && RuntimeInformation.OSArchitecture is Architecture.X64;
        public static bool IsArmMac => IsMacOS && RuntimeInformation.OSArchitecture is Architecture.Arm64;
        
        public static bool IsX64Windows => IsWindows && (RuntimeInformation.OSArchitecture is Architecture.X64);
        public static bool IsArmWindows => IsWindows && (RuntimeInformation.OSArchitecture is Architecture.Arm64);
        
        public static bool IsX64Linux => IsLinux && (RuntimeInformation.OSArchitecture is Architecture.X64);
        public static bool IsArmLinux => IsLinux && (RuntimeInformation.OSArchitecture is Architecture.Arm64);
    }
}
