using System;
using System.Runtime.InteropServices;

namespace ARMeilleure.Instructions
{
    static class MathHelper
    {
        [UnmanagedCallersOnly]
        public static double Abs(double value)
        {
            return Math.Abs(value);
        }

        [UnmanagedCallersOnly]
        public static double Ceiling(double value)
        {
            return Math.Ceiling(value);
        }

        [UnmanagedCallersOnly]
        public static double Floor(double value)
        {
            return Math.Floor(value);
        }

        [UnmanagedCallersOnly]
        public static double Round(double value, int mode)
        {
            return Math.Round(value, (MidpointRounding)mode);
        }

        [UnmanagedCallersOnly]
        public static double Truncate(double value)
        {
            return Math.Truncate(value);
        }
    }

    static class MathHelperF
    {
        [UnmanagedCallersOnly]
        public static float Abs(float value)
        {
            return MathF.Abs(value);
        }

        [UnmanagedCallersOnly]
        public static float Ceiling(float value)
        {
            return MathF.Ceiling(value);
        }

        [UnmanagedCallersOnly]
        public static float Floor(float value)
        {
            return MathF.Floor(value);
        }

        [UnmanagedCallersOnly]
        public static float Round(float value, int mode)
        {
            return MathF.Round(value, (MidpointRounding)mode);
        }

        [UnmanagedCallersOnly]
        public static float Truncate(float value)
        {
            return MathF.Truncate(value);
        }
    }
}
