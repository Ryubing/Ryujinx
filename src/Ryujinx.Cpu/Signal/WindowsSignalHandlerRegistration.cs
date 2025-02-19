using System.Runtime.InteropServices;

namespace Ryujinx.Cpu.Signal
{
    static partial class WindowsSignalHandlerRegistration
    {
        [LibraryImport("kernel32.dll")]
        private static partial nint AddVectoredExceptionHandler(uint first, nint handler);

        [LibraryImport("kernel32.dll")]
        private static partial ulong RemoveVectoredExceptionHandler(nint handle);

        public static nint RegisterExceptionHandler(nint action)
        {
            return AddVectoredExceptionHandler(1, action);
        }

        public static bool RemoveExceptionHandler(nint handle)
        {
            return RemoveVectoredExceptionHandler(handle) != 0;
        }
    }
}
