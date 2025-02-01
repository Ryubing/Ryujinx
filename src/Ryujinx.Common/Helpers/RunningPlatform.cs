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
        
        public static bool IsArm => RuntimeInformation.OSArchitecture is Architecture.Arm64;
        
        public static bool IsX64 => RuntimeInformation.OSArchitecture is Architecture.X64;

        public static bool IsIntelMac => IsMacOS && IsX64;
        public static bool IsArmMac => IsMacOS && IsArm;
        
        public static bool IsX64Windows => IsWindows && IsX64;
        public static bool IsArmWindows => IsWindows && IsArm;
        
        public static bool IsX64Linux => IsLinux && IsX64;
        public static bool IsArmLinux => IsLinux && IsArmMac;
    }
}
