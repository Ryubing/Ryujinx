using System;

namespace Ryujinx.Common.GraphicsDriver
{
    public static class DriverUtilities
    {
        private static void AddMesaFlags(string envVar, string newFlags)
        {
            string existingFlags = Environment.GetEnvironmentVariable(envVar);

            string flags = existingFlags == null ? newFlags : $"{existingFlags},{newFlags}";

            Environment.SetEnvironmentVariable(envVar, flags);
        }

        public static void InitDriverConfig(bool oglThreading)
        {
            if (OperatingSystem.IsLinux())
            {
                AddMesaFlags("RADV_DEBUG", "nodcc");
            }

            ToggleOGLThreading(oglThreading);
        }
        public static void ToggleOGLThreading(bool enabled)
        {
            Environment.SetEnvironmentVariable("mesa_glthread", enabled.ToString().ToLower());
            Environment.SetEnvironmentVariable("__GL_THREADED_OPTIMIZATIONS", enabled ? "1" : "0");

            try
            {
                NVThreadedOptimization.SetThreadedOptimization(enabled);
            }
            catch
            {
                // NVAPI is not available, or couldn't change the application profile.
            }
        }
    }
}
