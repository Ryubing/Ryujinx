using System;

namespace Ryujinx.Common.GraphicsDriver
{
    public static class DriverUtilities
    {
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
