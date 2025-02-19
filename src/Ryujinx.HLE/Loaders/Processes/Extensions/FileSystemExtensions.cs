using LibHac.Common;
using LibHac.Fs;
using LibHac.Fs.Fsa;
using LibHac.Loader;
using LibHac.Ns;
using LibHac.Tools.FsSystem;
using Ryujinx.Common.Configuration;
using Ryujinx.Common.Logging;
using Ryujinx.Graphics.Gpu;
using Ryujinx.HLE.Loaders.Executables;
using Ryujinx.Memory;
using System;
using System.Linq;
using static Ryujinx.HLE.HOS.ModLoader;

namespace Ryujinx.HLE.Loaders.Processes.Extensions
{
    static class FileSystemExtensions
    {
        public static MetaLoader GetNpdm(this IFileSystem fileSystem)
        {
            MetaLoader metaLoader = new();

            if (fileSystem == null || !fileSystem.FileExists(ProcessConst.MainNpdmPath))
            {
                Logger.Warning?.Print(LogClass.Loader, "NPDM file not found, using default values!");

                metaLoader.LoadDefault();
            }
            else
            {
                metaLoader.LoadFromFile(fileSystem);
            }

            return metaLoader;
        }

        public static ProcessResult Load(this IFileSystem exeFs, Switch device, BlitStruct<ApplicationControlProperty> nacpData, MetaLoader metaLoader, byte programIndex, bool isHomebrew = false)
        {
            ulong programId = metaLoader.GetProgramId();

            // Replace the whole ExeFs partition by the modded one.
            if (device.Configuration.VirtualFileSystem.ModLoader.ReplaceExefsPartition(programId, ref exeFs))
            {
                metaLoader = null;
            }

            // Reload the MetaLoader in case of ExeFs partition replacement.
            metaLoader ??= exeFs.GetNpdm();

            NsoExecutable[] nsoExecutables = new NsoExecutable[ProcessConst.ExeFsPrefixes.Length];

            for (int i = 0; i < nsoExecutables.Length; i++)
            {
                string name = ProcessConst.ExeFsPrefixes[i];

                if (!exeFs.FileExists($"/{name}"))
                {
                    continue; // File doesn't exist, skip.
                }

                Logger.Info?.Print(LogClass.Loader, $"Loading {name}...");

                using UniqueRef<IFile> nsoFile = new();

                exeFs.OpenFile(ref nsoFile.Ref, $"/{name}".ToU8Span(), OpenMode.Read).ThrowIfFailure();

                nsoExecutables[i] = new NsoExecutable(nsoFile.Release().AsStorage(), name);
            }

            // ExeFs file replacements.
            ModLoadResult modLoadResult = device.Configuration.VirtualFileSystem.ModLoader.ApplyExefsMods(programId, nsoExecutables);

            // Take the Npdm from mods if present.
            if (modLoadResult.Npdm != null)
            {
                metaLoader = modLoadResult.Npdm;
            }

            // Collect the Nsos, ignoring ones that aren't used.
            nsoExecutables = nsoExecutables.Where(x => x != null).ToArray();

            // Apply Nsos patches.
            device.Configuration.VirtualFileSystem.ModLoader.ApplyNsoPatches(programId, nsoExecutables);

            string programName = string.Empty;

            if (!isHomebrew && programId > 0x010000000000FFFF)
            {
                programName = nacpData.Value.Title[(int)device.System.State.DesiredTitleLanguage].NameString.ToString();

                if (string.IsNullOrWhiteSpace(programName))
                {
                    programName = Array.Find(nacpData.Value.Title.ItemsRo.ToArray(), x => x.Name[0] != 0).NameString.ToString();
                }
            }

            // Initialize GPU.
            GraphicsConfig.TitleId = programId.ToString("X16");
            device.Gpu.HostInitalized.Set();

            if (!MemoryBlock.SupportsFlags(MemoryAllocationFlags.ViewCompatible))
            {
                device.Configuration.MemoryManagerMode = MemoryManagerMode.SoftwarePageTable;
            }

            ProcessResult processResult = ProcessLoaderHelper.LoadNsos(
                device,
                device.System.KernelContext,
                metaLoader,
                nacpData,
                device.System.EnablePtc,
                modLoadResult.Hash,
                true,
                programName,
                metaLoader.GetProgramId(),
                programIndex,
                null,
                nsoExecutables);

            // TODO: This should be stored using ProcessId instead.
            device.System.LibHacHorizonManager.ArpIReader.ApplicationId = new LibHac.ApplicationId(metaLoader.GetProgramId());

            return processResult;
        }
    }
}
