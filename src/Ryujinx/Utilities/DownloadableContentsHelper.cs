using LibHac.Common;
using LibHac.Fs;
using LibHac.Fs.Fsa;
using LibHac.Tools.FsSystem;
using LibHac.Tools.FsSystem.NcaUtils;
using Ryujinx.Ava.Common.Models;
using Ryujinx.Common.Configuration;
using Ryujinx.Common.Logging;
using Ryujinx.Common.Utilities;
using Ryujinx.HLE.FileSystem;
using Ryujinx.HLE.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using Path = System.IO.Path;

namespace Ryujinx.Ava.Utilities
{
    public static class DownloadableContentsHelper
    {
        private static readonly DownloadableContentJsonSerializerContext _serializerContext = new(JsonHelper.GetDefaultSerializerOptions());

        public static List<(DownloadableContentModel, bool IsEnabled)> LoadDownloadableContentsJson(VirtualFileSystem vfs, ulong applicationIdBase)
        {
            string downloadableContentJsonPath = PathToGameDLCJson(applicationIdBase);

            if (!File.Exists(downloadableContentJsonPath))
            {
                return [];
            }

            try
            {
                List<DownloadableContentContainer> downloadableContentContainerList = JsonHelper.DeserializeFromFile(downloadableContentJsonPath,
                    _serializerContext.ListDownloadableContentContainer);
                return LoadDownloadableContents(vfs, downloadableContentContainerList);
            }
            catch
            {
                Logger.Error?.Print(LogClass.Configuration, "Downloadable Content JSON failed to deserialize.");
                return [];
            }
        }

        public static void SaveDownloadableContentsJson(ulong applicationIdBase, List<(DownloadableContentModel, bool IsEnabled)> dlcs)
        {
            DownloadableContentContainer container = default;
            List<DownloadableContentContainer> downloadableContentContainerList = [];

            foreach ((DownloadableContentModel dlc, bool isEnabled) in dlcs)
            {
                if (container.ContainerPath != dlc.ContainerPath)
                {
                    if (!string.IsNullOrWhiteSpace(container.ContainerPath))
                    {
                        downloadableContentContainerList.Add(container);
                    }

                    container = new DownloadableContentContainer
                    {
                        ContainerPath = dlc.ContainerPath,
                        DownloadableContentNcaList = [],
                    };
                }

                container.DownloadableContentNcaList.Add(new DownloadableContentNca
                {
                    Enabled = isEnabled,
                    TitleId = dlc.TitleId,
                    FullPath = dlc.FullPath,
                });
            }

            if (!string.IsNullOrWhiteSpace(container.ContainerPath))
            {
                downloadableContentContainerList.Add(container);
            }

            string downloadableContentJsonPath = PathToGameDLCJson(applicationIdBase);
            JsonHelper.SerializeToFile(downloadableContentJsonPath, downloadableContentContainerList, _serializerContext.ListDownloadableContentContainer);
        }

        private static List<(DownloadableContentModel, bool IsEnabled)> LoadDownloadableContents(VirtualFileSystem vfs, List<DownloadableContentContainer> downloadableContentContainers)
        {
            List<(DownloadableContentModel, bool IsEnabled)> result = [];

            foreach (DownloadableContentContainer downloadableContentContainer in downloadableContentContainers)
            {
                if (!File.Exists(downloadableContentContainer.ContainerPath))
                {
                    continue;
                }

                using IFileSystem partitionFileSystem = PartitionFileSystemUtils.OpenApplicationFileSystem(downloadableContentContainer.ContainerPath, vfs);

                foreach (DownloadableContentNca downloadableContentNca in downloadableContentContainer.DownloadableContentNcaList)
                {
                    using UniqueRef<IFile> ncaFile = new();

                    partitionFileSystem.OpenFile(ref ncaFile.Ref, downloadableContentNca.FullPath.ToU8Span(), OpenMode.Read).ThrowIfFailure();

                    Nca nca = TryOpenNca(vfs, ncaFile.Get.AsStorage());
                    if (nca == null)
                    {
                        continue;
                    }

                    DownloadableContentModel content = new(nca.Header.TitleId,
                        downloadableContentContainer.ContainerPath,
                        downloadableContentNca.FullPath);

                    result.Add((content, downloadableContentNca.Enabled));
                }
            }

            return result;
        }

        private static Nca TryOpenNca(VirtualFileSystem vfs, IStorage ncaStorage)
        {
            try
            {
                return new Nca(vfs.KeySet, ncaStorage);
            }
            catch (Exception) { }

            return null;
        }

        private static string PathToGameDLCJson(ulong applicationIdBase)
        {
            return Path.Combine(AppDataManager.GamesDirPath, applicationIdBase.ToString("x16"), "dlc.json");
        }
    }
}
