using LibHac.Common;
using LibHac.Fs;
using LibHac.Fs.Fsa;
using LibHac.Loader;
using LibHac.Util;
using Ryujinx.Common;
using System;

namespace Ryujinx.HLE.Loaders.Processes.Extensions
{
    public static class MetaLoaderExtensions
    {
        public static ulong GetProgramId(this MetaLoader metaLoader)
        {
            metaLoader.GetNpdm(out LibHac.Loader.Npdm npdm).ThrowIfFailure();

            return npdm.Aci.ProgramId.Value;
        }

        public static string GetProgramName(this MetaLoader metaLoader)
        {
            metaLoader.GetNpdm(out LibHac.Loader.Npdm npdm).ThrowIfFailure();

            return StringUtils.Utf8ZToString(npdm.Meta.ProgramName);
        }

        public static bool IsProgram64Bit(this MetaLoader metaLoader)
        {
            metaLoader.GetNpdm(out LibHac.Loader.Npdm npdm).ThrowIfFailure();

            return (npdm.Meta.Flags & 1) != 0;
        }

        public static void LoadDefault(this MetaLoader metaLoader)
        {
            byte[] npdmBuffer = EmbeddedResources.Read("Ryujinx.HLE/Homebrew.npdm");

            metaLoader.Load(npdmBuffer).ThrowIfFailure();
        }

        public static void LoadFromFile(this MetaLoader metaLoader, IFileSystem fileSystem, string path = "")
        {
            if (string.IsNullOrEmpty(path))
            {
                path = ProcessConst.MainNpdmPath;
            }

            using UniqueRef<IFile> npdmFile = new();

            fileSystem.OpenFile(ref npdmFile.Ref, path.ToU8Span(), OpenMode.Read).ThrowIfFailure();

            npdmFile.Get.GetSize(out long fileSize).ThrowIfFailure();

            Span<byte> npdmBuffer = new byte[fileSize];

            npdmFile.Get.Read(out _, 0, npdmBuffer).ThrowIfFailure();

            metaLoader.Load(npdmBuffer).ThrowIfFailure();
        }
    }
}
