using Ryujinx.Common.Memory;
using Ryujinx.Common.Utilities;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Ryujinx.Common
{
    public static class EmbeddedResources
    {
        private readonly static Assembly _resourceAssembly;

        static EmbeddedResources()
        {
            _resourceAssembly = Assembly.GetAssembly(typeof(EmbeddedResources));
        }

        public static byte[] Read(string filename)
        {
            (Assembly assembly, string path) = ResolveManifestPath(filename);

            return Read(assembly, path);
        }

        public static Task<byte[]> ReadAsync(string filename)
        {
            (Assembly assembly, string path) = ResolveManifestPath(filename);

            return ReadAsync(assembly, path);
        }

        public static byte[] Read(Assembly assembly, string filename)
        {
            using Stream stream = GetStream(assembly, filename);
            if (stream == null)
            {
                return null;
            }

            return StreamUtils.StreamToBytes(stream);
        }

        public static MemoryOwner<byte> ReadFileToRentedMemory(string filename)
        {
            (Assembly assembly, string path) = ResolveManifestPath(filename);

            return ReadFileToRentedMemory(assembly, path);
        }

        public static MemoryOwner<byte> ReadFileToRentedMemory(Assembly assembly, string filename)
        {
            using Stream stream = GetStream(assembly, filename);

            return stream is null
                ? null
                : StreamUtils.StreamToRentedMemory(stream);
        }

        public async static Task<byte[]> ReadAsync(Assembly assembly, string filename)
        {
            using Stream stream = GetStream(assembly, filename);
            if (stream == null)
            {
                return null;
            }

            return await StreamUtils.StreamToBytesAsync(stream);
        }

        public static string ReadAllText(string filename)
        {
            (Assembly assembly, string path) = ResolveManifestPath(filename);

            return ReadAllText(assembly, path);
        }

        public static Task<string> ReadAllTextAsync(string filename)
        {
            (Assembly assembly, string path) = ResolveManifestPath(filename);

            return ReadAllTextAsync(assembly, path);
        }

        public static string ReadAllText(Assembly assembly, string filename)
        {
            using Stream stream = GetStream(assembly, filename);
            if (stream == null)
            {
                return null;
            }

            using StreamReader reader = new(stream);
            return reader.ReadToEnd();
        }

        public async static Task<string> ReadAllTextAsync(Assembly assembly, string filename)
        {
            using Stream stream = GetStream(assembly, filename);
            if (stream == null)
            {
                return null;
            }

            using StreamReader reader = new(stream);
            return await reader.ReadToEndAsync();
        }

        public static Stream GetStream(string filename)
        {
            (Assembly assembly, string path) = ResolveManifestPath(filename);

            return GetStream(assembly, path);
        }

        public static Stream GetStream(Assembly assembly, string filename)
        {
            string @namespace = assembly.GetName().Name;
            string manifestUri = @namespace + "." + filename.Replace('/', '.');

            Stream stream = assembly.GetManifestResourceStream(manifestUri);

            return stream;
        }

        public static string[] GetAllAvailableResources(string path, string ext = "")
        {
            return ResolveManifestPath(path).Item1.GetManifestResourceNames()
                .Where(r => r.EndsWith(ext))
                .ToArray();
        }

        private static (Assembly, string) ResolveManifestPath(string filename)
        {
            string[] segments = filename.Split('/', 2, StringSplitOptions.RemoveEmptyEntries);

            if (segments.Length >= 2)
            {
                foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    if (assembly.GetName().Name == segments[0])
                    {
                        return (assembly, segments[1]);
                    }
                }
            }

            return (_resourceAssembly, filename);
        }
    }
}
