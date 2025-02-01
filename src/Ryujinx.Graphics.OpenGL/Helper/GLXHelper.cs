using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Ryujinx.Graphics.OpenGL.Helper
{
    [SupportedOSPlatform("linux")]
    internal static partial class GLXHelper
    {
        private const string LibraryName = "glx.dll";

        static GLXHelper()
        {
            NativeLibrary.SetDllImportResolver(typeof(GLXHelper).Assembly, (name, assembly, path) =>
            {
                if (name != LibraryName)
                {
                    return nint.Zero;
                }

                if (!NativeLibrary.TryLoad("libGL.so.1", assembly, path, out nint result))
                {
                    if (!NativeLibrary.TryLoad("libGL.so", assembly, path, out result))
                    {
                        return nint.Zero;
                    }
                }

                return result;
            });
        }

        [LibraryImport(LibraryName, EntryPoint = "glXGetCurrentContext")]
        public static partial nint GetCurrentContext();
    }
}
