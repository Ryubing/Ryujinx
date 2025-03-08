using LibHac.Common;
using LibHac.Common.Keys;
using LibHac.Fs;
using LibHac.Fs.Fsa;
using LibHac.Ncm;
using LibHac.Ns;
using LibHac.Tools.FsSystem;
using LibHac.Tools.FsSystem.NcaUtils;
using Ryujinx.Ava.Common.Models;
using Ryujinx.Ava.Systems.Configuration;
using Ryujinx.Common.Configuration;
using Ryujinx.Common.Logging;
using Ryujinx.Common.Utilities;
using Ryujinx.HLE.FileSystem;
using Ryujinx.HLE.Loaders.Processes.Extensions;
using Ryujinx.HLE.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using ContentType = LibHac.Ncm.ContentType;
using Path = System.IO.Path;
using SpanHelpers = LibHac.Common.SpanHelpers;
using TitleUpdateMetadata = Ryujinx.Common.Configuration.TitleUpdateMetadata;

namespace Ryujinx.Ava.Utilities
{
    public static class TitleUpdatesHelper
    {
        private static readonly TitleUpdateMetadataJsonSerializerContext _serializerContext = new(JsonHelper.GetDefaultSerializerOptions());

        public static List<(TitleUpdateModel Update, bool IsSelected)> LoadTitleUpdatesJson(VirtualFileSystem vfs, ulong applicationIdBase)
        {
            string titleUpdatesJsonPath = PathToGameUpdatesJson(applicationIdBase);

            if (!File.Exists(titleUpdatesJsonPath))
            {
                return [];
            }

            try
            {
                TitleUpdateMetadata titleUpdateWindowData = JsonHelper.DeserializeFromFile(titleUpdatesJsonPath, _serializerContext.TitleUpdateMetadata);
                return LoadTitleUpdates(vfs, titleUpdateWindowData, applicationIdBase);
            }
            catch
            {
                Logger.Warning?.Print(LogClass.Application, $"Failed to deserialize title update data for {applicationIdBase:x16} at {titleUpdatesJsonPath}");
                return [];
            }
        }

        public static void SaveTitleUpdatesJson(ulong applicationIdBase, List<(TitleUpdateModel, bool IsSelected)> updates)
        {
            TitleUpdateMetadata titleUpdateWindowData = new()
            {
                Selected = string.Empty,
                Paths = [],
            };

            foreach ((TitleUpdateModel update, bool isSelected) in updates)
            {
                titleUpdateWindowData.Paths.Add(update.Path);
                if (isSelected)
                {
                    if (!string.IsNullOrEmpty(titleUpdateWindowData.Selected))
                    {
                        Logger.Error?.Print(LogClass.Application,
                            $"Tried to save two updates as 'IsSelected' for {applicationIdBase:x16}");
                        return;
                    }

                    titleUpdateWindowData.Selected = update.Path;
                }
            }

            string titleUpdatesJsonPath = PathToGameUpdatesJson(applicationIdBase);
            JsonHelper.SerializeToFile(titleUpdatesJsonPath, titleUpdateWindowData, _serializerContext.TitleUpdateMetadata);
        }

        private static List<(TitleUpdateModel Update, bool IsSelected)> LoadTitleUpdates(VirtualFileSystem vfs, TitleUpdateMetadata titleUpdateMetadata, ulong applicationIdBase)
        {
            List<(TitleUpdateModel, bool IsSelected)> result = [];

            foreach (string path in titleUpdateMetadata.Paths)
            {
                if (!File.Exists(path))
                    continue;

                try
                {
                    using IFileSystem pfs = PartitionFileSystemUtils.OpenApplicationFileSystem(path, vfs);

                    Dictionary<ulong, ContentMetaData> updates =
                        pfs.GetContentData(ContentMetaType.Patch, vfs, ConfigurationState.Instance.System.IntegrityCheckLevel);

                    if (!updates.TryGetValue(applicationIdBase, out ContentMetaData content))
                        continue;

                    Nca patchNca = content.GetNcaByType(vfs.KeySet, ContentType.Program);
                    Nca controlNca = content.GetNcaByType(vfs.KeySet, ContentType.Control);

                    if (controlNca is null || patchNca is null)
                        continue;

                    ApplicationControlProperty controlData = new();

                    using UniqueRef<IFile> nacpFile = new();

                    controlNca.OpenFileSystem(NcaSectionType.Data, IntegrityCheckLevel.None)
                        .OpenFile(ref nacpFile.Ref, "/control.nacp".ToU8Span(), OpenMode.Read).ThrowIfFailure();
                    nacpFile.Get.Read(out _, 0, SpanHelpers.AsByteSpan(ref controlData), ReadOption.None)
                        .ThrowIfFailure();

                    string displayVersion = controlData.DisplayVersionString.ToString();
                    TitleUpdateModel update = new(content.ApplicationId, content.Version.Version,
                        displayVersion, path);

                    result.Add((update, path == titleUpdateMetadata.Selected));
                }
                catch (MissingKeyException exception)
                {
                    Logger.Warning?.Print(LogClass.Application,
                        $"Your key set is missing a key with the name: {exception.Name}");
                }
                catch (InvalidDataException)
                {
                    Logger.Warning?.Print(LogClass.Application,
                        $"The header key is incorrect or missing and therefore the NCA header content type check has failed. Malformed File: {path}");
                }
                catch (IOException exception)
                {
                    Logger.Warning?.Print(LogClass.Application, exception.Message);
                }
                catch (Exception exception)
                {
                    Logger.Warning?.Print(LogClass.Application,
                        $"The file encountered was not of a valid type. File: '{path}' Error: {exception}");
                }
            }

            return result;
        }

        private static string PathToGameUpdatesJson(ulong applicationIdBase)
            => Path.Combine(AppDataManager.GamesDirPath, applicationIdBase.ToString("x16"), "updates.json");
    }
}
