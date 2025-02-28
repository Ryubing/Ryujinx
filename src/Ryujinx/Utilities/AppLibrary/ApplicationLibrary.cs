using DynamicData;
using DynamicData.Kernel;
using Gommon;
using LibHac;
using LibHac.Common;
using LibHac.Fs;
using LibHac.Fs.Fsa;
using LibHac.FsSystem;
using LibHac.Ncm;
using LibHac.Ns;
using LibHac.Tools.Fs;
using LibHac.Tools.FsSystem;
using LibHac.Tools.FsSystem.NcaUtils;
using Ryujinx.Ava.Common.Models;
using Ryujinx.Ava.Utilities.Configuration;
using Ryujinx.Ava.Utilities.Configuration.System;
using Ryujinx.Common;
using Ryujinx.Common.Configuration;
using Ryujinx.Common.Configuration.Multiplayer;
using Ryujinx.Common.Logging;
using Ryujinx.Common.Utilities;
using Ryujinx.HLE.FileSystem;
using Ryujinx.HLE.HOS.SystemState;
using Ryujinx.HLE.Loaders.Npdm;
using Ryujinx.HLE.Loaders.Processes.Extensions;
using Ryujinx.HLE.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ContentType = LibHac.Ncm.ContentType;
using MissingKeyException = LibHac.Common.Keys.MissingKeyException;
using Path = System.IO.Path;
using TimeSpan = System.TimeSpan;

namespace Ryujinx.Ava.Utilities.AppLibrary
{
    public class ApplicationLibrary
    {
        public Language DesiredLanguage { get; set; }
        public event EventHandler<ApplicationCountUpdatedEventArgs> ApplicationCountUpdated;
        public event Action<LdnGameDataReceivedEventArgs> LdnGameDataReceived;

        public readonly IObservableCache<ApplicationData, ulong> Applications;
        public readonly IObservableCache<(TitleUpdateModel TitleUpdate, bool IsSelected), TitleUpdateModel> TitleUpdates;
        public readonly IObservableCache<(DownloadableContentModel Dlc, bool IsEnabled), DownloadableContentModel> DownloadableContents;

        private readonly byte[] _nspIcon;
        private readonly byte[] _xciIcon;
        private readonly byte[] _ncaIcon;
        private readonly byte[] _nroIcon;
        private readonly byte[] _nsoIcon;

        private readonly VirtualFileSystem _virtualFileSystem;
        private readonly IntegrityCheckLevel _checkLevel;
        private CancellationTokenSource _cancellationToken;
        private readonly SourceCache<ApplicationData, ulong> _applications = new(it => it.Id);
        private readonly SourceCache<(TitleUpdateModel TitleUpdate, bool IsSelected), TitleUpdateModel> _titleUpdates = new(it => it.TitleUpdate);
        private readonly SourceCache<(DownloadableContentModel Dlc, bool IsEnabled), DownloadableContentModel> _downloadableContents = new(it => it.Dlc);

        private static readonly ApplicationJsonSerializerContext _serializerContext = new(JsonHelper.GetDefaultSerializerOptions());
        private static readonly LdnGameDataSerializerContext _ldnDataSerializerContext = new(JsonHelper.GetDefaultSerializerOptions());

        public ApplicationLibrary(VirtualFileSystem virtualFileSystem, IntegrityCheckLevel checkLevel)
        {
            _virtualFileSystem = virtualFileSystem;
            _checkLevel = checkLevel;

            Applications = _applications.AsObservableCache();
            TitleUpdates = _titleUpdates.AsObservableCache();
            DownloadableContents = _downloadableContents.AsObservableCache();

            _nspIcon = EmbeddedResources.Read("Ryujinx/Assets.UIImages.Icon_NSP.png");
            _xciIcon = EmbeddedResources.Read("Ryujinx/Assets.UIImages.Icon_XCI.png");
            _ncaIcon = EmbeddedResources.Read("Ryujinx/Assets.UIImages.Icon_NCA.png");
            _nroIcon = EmbeddedResources.Read("Ryujinx/Assets.UIImages.Icon_NRO.png");
            _nsoIcon = EmbeddedResources.Read("Ryujinx/Assets.UIImages.Icon_NSO.png");
        }

        /// <exception cref="Ryujinx.HLE.Exceptions.InvalidNpdmException">The npdm file doesn't contain valid data.</exception>
        /// <exception cref="NotImplementedException">The FsAccessHeader.ContentOwnerId section is not implemented.</exception>
        /// <exception cref="ArgumentException">An error occured while reading bytes from the stream.</exception>
        /// <exception cref="EndOfStreamException">The end of the stream is reached.</exception>
        /// <exception cref="IOException">An I/O error occurred.</exception>
        private ApplicationData GetApplicationFromExeFs(PartitionFileSystem pfs, string filePath)
        {
            ApplicationData data = new()
            {
                Icon = _nspIcon,
                Path = filePath,
            };

            using UniqueRef<IFile> npdmFile = new();

            Result result = pfs.OpenFile(ref npdmFile.Ref, "/main.npdm".ToU8Span(), OpenMode.Read);

            if (ResultFs.PathNotFound.Includes(result))
            {
                Npdm npdm = new(npdmFile.Get.AsStream());

                data.Name = npdm.TitleName;
                data.Id = npdm.Aci0.TitleId;
            }

            return data;
        }

        /// <summary>
        ///     Gets a name for an available content file based on the Application ID '<paramref name="id"/>'.
        ///     <br/><br/>
        ///     For Applications, this returns the localized name of the app found in the file.
        ///     For DLCs, this returns the name of the file that contains the DLC, minus the file extension.
        /// </summary>
        /// <param name="id">The Application ID to search for.</param>
        /// <remarks>
        /// If the provided Application ID does not have a corresponding Application OR DLC file,
        /// <paramref name="id"/> formatted as hexadecimal is returned.
        /// </remarks>
        /// <returns>A formatted Application name, or <paramref name="id"/> as hexadecimal if none is found.</returns>
        public string GetNameForApplicationId(ulong id)
        {
            DynamicData.Kernel.Optional<ApplicationData> appData = Applications.Lookup(id);
            if (appData.HasValue)
                return appData.Value.Name;

            if (!DownloadableContents.Keys.FindFirst(x => x.TitleId == id).TryGet(out DownloadableContentModel dlcData))
                return id.ToString("X16");
            
            string name = Path.GetFileNameWithoutExtension(dlcData.FileName)!;
            int idx = name.IndexOf('[');
            if (idx != -1)
                name = name[..idx];

            return name;
        }

        public bool FindApplication(ulong id, out ApplicationData foundData)
        {
            DynamicData.Kernel.Optional<ApplicationData> appData = Applications.Lookup(id);
            foundData = appData.HasValue ? appData.Value : null;

            return appData.HasValue;
        }
        
        public bool FindUpdate(ulong id, out TitleUpdateModel foundData)
        {
            Gommon.Optional<TitleUpdateModel> appData = 
                TitleUpdates.Keys.FindFirst(x => x.TitleId == id);
            foundData = appData.HasValue ? appData.Value : null;

            return appData.HasValue;
        }
        
        public TitleUpdateModel[] FindUpdatesFor(ulong id)
            => TitleUpdates.Keys.Where(x => x.TitleIdBase == (id & ~0x1FFFUL)).ToArray();
        
        public (TitleUpdateModel TitleUpdate, bool IsSelected)[] FindUpdateConfigurationFor(ulong id)
            => TitleUpdates.Items.Where(x => x.TitleUpdate.TitleIdBase == (id & ~0x1FFFUL)).ToArray();
        
        public DownloadableContentModel[] FindDlcsFor(ulong id)
            => DownloadableContents.Keys.Where(x => x.TitleIdBase == (id & ~0x1FFFUL)).ToArray();
        
        public (DownloadableContentModel Dlc, bool IsEnabled)[] FindDlcConfigurationFor(ulong id)
            => DownloadableContents.Items.Where(x => x.Dlc.TitleIdBase == (id & ~0x1FFFUL)).ToArray();
        
        public bool HasDlcs(ulong id)
            => DownloadableContents.Keys.Any(x => x.TitleIdBase == (id & ~0x1FFFUL));

        /// <exception cref="LibHac.Common.Keys.MissingKeyException">The configured key set is missing a key.</exception>
        /// <exception cref="InvalidDataException">The NCA header could not be decrypted.</exception>
        /// <exception cref="NotSupportedException">The NCA version is not supported.</exception>
        /// <exception cref="HorizonResultException">An error occured while reading PFS data.</exception>
        /// <exception cref="Ryujinx.HLE.Exceptions.InvalidNpdmException">The npdm file doesn't contain valid data.</exception>
        /// <exception cref="NotImplementedException">The FsAccessHeader.ContentOwnerId section is not implemented.</exception>
        /// <exception cref="ArgumentException">An error occured while reading bytes from the stream.</exception>
        /// <exception cref="EndOfStreamException">The end of the stream is reached.</exception>
        /// <exception cref="IOException">An I/O error occurred.</exception>
        private ApplicationData GetApplicationFromNsp(PartitionFileSystem pfs, string filePath)
        {
            bool isExeFs = false;

            // If the NSP doesn't have a main NCA, decrement the number of applications found and then continue to the next application.
            bool hasMainNca = false;

            foreach (DirectoryEntryEx fileEntry in pfs.EnumerateEntries("/", "*"))
            {
                if (Path.GetExtension(fileEntry.FullPath)?.ToLower() == ".nca")
                {
                    using UniqueRef<IFile> ncaFile = new();

                    try
                    {
                        pfs.OpenFile(ref ncaFile.Ref, fileEntry.FullPath.ToU8Span(), OpenMode.Read).ThrowIfFailure();

                        Nca nca = new(_virtualFileSystem.KeySet, ncaFile.Get.AsStorage());
                        int dataIndex = Nca.GetSectionIndexFromType(NcaSectionType.Data, NcaContentType.Program);

                        // Some main NCAs don't have a data partition, so check if the partition exists before opening it
                        if (nca.Header.ContentType == NcaContentType.Program &&
                            !(nca.SectionExists(NcaSectionType.Data) &&
                              nca.Header.GetFsHeader(dataIndex).IsPatchSection()))
                        {
                            hasMainNca = true;

                            break;
                        }
                    }
                    catch (Exception exception)
                    {
                        Logger.Warning?.Print(LogClass.Application, $"Encountered an error while trying to load applications from file '{filePath}': {exception}");

                        return null;
                    }
                }
                else if (Path.GetFileNameWithoutExtension(fileEntry.FullPath) == "main")
                {
                    isExeFs = true;
                }
            }

            if (hasMainNca)
            {
                List<ApplicationData> applications = GetApplicationsFromPfs(pfs, filePath);

                switch (applications.Count)
                {
                    case 1:
                        return applications[0];
                    case >= 1:
                        Logger.Warning?.Print(LogClass.Application, $"File '{filePath}' contains more applications than expected: {applications.Count}");
                        return applications[0];
                    default:
                        return null;
                }
            }

            return isExeFs 
                ? GetApplicationFromExeFs(pfs, filePath) 
                : null;
        }

        /// <exception cref="LibHac.Common.Keys.MissingKeyException">The configured key set is missing a key.</exception>
        /// <exception cref="InvalidDataException">The NCA header could not be decrypted.</exception>
        /// <exception cref="NotSupportedException">The NCA version is not supported.</exception>
        /// <exception cref="HorizonResultException">An error occured while reading PFS data.</exception>
        private List<ApplicationData> GetApplicationsFromPfs(IFileSystem pfs, string filePath)
        {
            List<ApplicationData> applications = [];
            string extension = Path.GetExtension(filePath).ToLower();

            foreach ((ulong titleId, ContentMetaData content) in pfs.GetContentData(ContentMetaType.Application, _virtualFileSystem, _checkLevel))
            {
                ApplicationData applicationData = new()
                {
                    Id = titleId,
                    Path = filePath,
                };

                Nca mainNca = content.GetNcaByType(_virtualFileSystem.KeySet, ContentType.Program);
                Nca controlNca = content.GetNcaByType(_virtualFileSystem.KeySet, ContentType.Control);

                BlitStruct<ApplicationControlProperty> controlHolder = new(1);

                IFileSystem controlFs = controlNca?.OpenFileSystem(NcaSectionType.Data, _checkLevel);

                // Check if there is an update available.
                if (IsUpdateApplied(mainNca, out IFileSystem updatedControlFs))
                {
                    // Replace the original ControlFs by the updated one.
                    controlFs = updatedControlFs;
                }

                if (controlFs == null)
                {
                    continue;
                }

                ReadControlData(controlFs, controlHolder.ByteSpan);

                GetApplicationInformation(ref controlHolder.Value, ref applicationData);

                // Read the icon from the ControlFS and store it as a byte array
                try
                {
                    using UniqueRef<IFile> icon = new();

                    controlFs.OpenFile(ref icon.Ref, $"/icon_{DesiredLanguage}.dat".ToU8Span(), OpenMode.Read).ThrowIfFailure();

                    using MemoryStream stream = new();

                    icon.Get.AsStream().CopyTo(stream);
                    applicationData.Icon = stream.ToArray();
                }
                catch (HorizonResultException)
                {
                    foreach (DirectoryEntryEx entry in controlFs.EnumerateEntries("/", "*"))
                    {
                        if (entry.Name == "control.nacp")
                        {
                            continue;
                        }

                        using UniqueRef<IFile> icon = new();

                        controlFs.OpenFile(ref icon.Ref, entry.FullPath.ToU8Span(), OpenMode.Read).ThrowIfFailure();

                        using MemoryStream stream = new();

                        icon.Get.AsStream().CopyTo(stream);
                        applicationData.Icon = stream.ToArray();

                        if (applicationData.Icon != null)
                        {
                            break;
                        }
                    }

                    applicationData.Icon ??= extension == ".xci" ? _xciIcon : _nspIcon;
                }

                applicationData.ControlHolder = controlHolder;

                applications.Add(applicationData);
            }

            return applications;
        }

        public bool TryGetApplicationsFromFile(string applicationPath, out List<ApplicationData> applications)
        {
            applications = [];
            long fileSize;

            try
            {
                fileSize = new FileInfo(applicationPath).Length;
            }
            catch (FileNotFoundException)
            {
                Logger.Warning?.Print(LogClass.Application, $"The file was not found: '{applicationPath}'");

                return false;
            }

            BlitStruct<ApplicationControlProperty> controlHolder = new(1);

            try
            {
                string extension = Path.GetExtension(applicationPath).ToLower();

                using FileStream file = new(applicationPath, FileMode.Open, FileAccess.Read);

                switch (extension)
                {
                    case ".xci":
                        {
                            Xci xci = new(_virtualFileSystem.KeySet, file.AsStorage());

                            applications = GetApplicationsFromPfs(xci.OpenPartition(XciPartitionType.Secure), applicationPath);

                            if (applications.Count == 0)
                            {
                                return false;
                            }

                            break;
                        }
                    case ".nsp":
                    case ".pfs0":
                        {
                            PartitionFileSystem pfs = new();
                            pfs.Initialize(file.AsStorage()).ThrowIfFailure();

                            ApplicationData result = GetApplicationFromNsp(pfs, applicationPath);

                            if (result == null)
                            {
                                return false;
                            }

                            applications.Add(result);

                            break;
                        }
                    case ".nro":
                        {
                            BinaryReader reader = new(file);
                            ApplicationData application = new();

                            file.Seek(24, SeekOrigin.Begin);

                            int assetOffset = reader.ReadInt32();

                            if (Encoding.ASCII.GetString(Read(assetOffset, 4)) == "ASET")
                            {
                                byte[] iconSectionInfo = Read(assetOffset + 8, 0x10);

                                long iconOffset = BitConverter.ToInt64(iconSectionInfo, 0);
                                long iconSize = BitConverter.ToInt64(iconSectionInfo, 8);

                                ulong nacpOffset = reader.ReadUInt64();
                                ulong nacpSize = reader.ReadUInt64();

                                // Reads and stores game icon as byte array
                                if (iconSize > 0)
                                {
                                    application.Icon = Read(assetOffset + iconOffset, (int)iconSize);
                                }
                                else
                                {
                                    application.Icon = _nroIcon;
                                }

                                // Read the NACP data
                                Read(assetOffset + (int)nacpOffset, (int)nacpSize).AsSpan().CopyTo(controlHolder.ByteSpan);

                                GetApplicationInformation(ref controlHolder.Value, ref application);
                            }
                            else
                            {
                                application.Icon = _nroIcon;
                                application.Name = Path.GetFileNameWithoutExtension(applicationPath);
                            }

                            application.ControlHolder = controlHolder;
                            applications.Add(application);

                            break;

                            byte[] Read(long position, int size)
                            {
                                file.Seek(position, SeekOrigin.Begin);

                                return reader.ReadBytes(size);
                            }
                        }
                    case ".nca":
                        {
                            ApplicationData application = new();

                            Nca nca = new(_virtualFileSystem.KeySet, new FileStream(applicationPath, FileMode.Open, FileAccess.Read).AsStorage());

                            if (!nca.IsProgram() || nca.IsPatch())
                            {
                                return false;
                            }

                            application.Icon = _ncaIcon;
                            application.Name = Path.GetFileNameWithoutExtension(applicationPath);
                            application.ControlHolder = controlHolder;

                            applications.Add(application);

                            break;
                        }
                    // If its an NSO we just set defaults
                    case ".nso":
                        {
                            ApplicationData application = new()
                            {
                                Icon = _nsoIcon,
                                Name = Path.GetFileNameWithoutExtension(applicationPath),
                            };

                            applications.Add(application);

                            break;
                        }
                }
            }
            catch (MissingKeyException exception)
            {
                Logger.Warning?.Print(LogClass.Application, $"Your key set is missing a key with the name: {exception.Name}");

                return false;
            }
            catch (InvalidDataException)
            {
                Logger.Warning?.Print(LogClass.Application, $"The header key is incorrect or missing and therefore the NCA header content type check has failed. Errored File: {applicationPath}");

                return false;
            }
            catch (IOException exception)
            {
                Logger.Warning?.Print(LogClass.Application, exception.Message);

                return false;
            }
            catch (Exception exception)
            {
                Logger.Warning?.Print(LogClass.Application, $"The file encountered was not of a valid type. File: '{applicationPath}' Error: {exception}");

                return false;
            }

            foreach (ApplicationData data in applications)
            {
                // Only load metadata for applications with an ID
                if (data.Id != 0)
                {
                    ApplicationMetadata appMetadata = LoadAndSaveMetaData(data.IdString, appMetadata =>
                    {                        
                        appMetadata.Title = data.Name;

                        // Only do the migration if time_played has a value and timespan_played hasn't been updated yet.
                        if (appMetadata.TimePlayedOld != default && appMetadata.TimePlayed == TimeSpan.Zero)
                        {
                            appMetadata.TimePlayed = TimeSpan.FromSeconds(appMetadata.TimePlayedOld);
                            appMetadata.TimePlayedOld = default;
                        }

                        // Only do the migration if last_played has a value and last_played_utc doesn't exist yet.
                        if (appMetadata.LastPlayedOld != default && !appMetadata.LastPlayed.HasValue)
                        {
                            // Migrate from string-based last_played to DateTime-based last_played_utc.
                            if (DateTime.TryParse(appMetadata.LastPlayedOld, out DateTime lastPlayedOldParsed))
                            {
                                appMetadata.LastPlayed = lastPlayedOldParsed;

                                // Migration successful: deleting last_played from the metadata file.
                                appMetadata.LastPlayedOld = default;
                            }

                        }
                    });
            
                    data.Favorite = appMetadata.Favorite;
                    data.TimePlayed = appMetadata.TimePlayed;
                    data.LastPlayed = appMetadata.LastPlayed;
                    data.HasIndependentConfiguration = File.Exists(Program.GetDirGameUserConfig(data.IdBaseString, false, false)); // Just check user config
                }

                data.FileExtension = Path.GetExtension(applicationPath).TrimStart('.').ToUpper();
                data.FileSize = fileSize;
                data.Path = applicationPath;
            }

            return true;
        }

        public bool TryGetDownloadableContentFromFile(string filePath, out List<DownloadableContentModel> titleUpdates)
        {
            titleUpdates = [];

            try
            {
                string extension = Path.GetExtension(filePath).ToLower();

                using FileStream file = new(filePath, FileMode.Open, FileAccess.Read);

                switch (extension)
                {
                    case ".xci":
                    case ".nsp":
                        {
                            using IFileSystem pfs = PartitionFileSystemUtils.OpenApplicationFileSystem(filePath, _virtualFileSystem);

                            foreach (DirectoryEntryEx fileEntry in pfs.EnumerateEntries("/", "*.nca"))
                            {
                                using UniqueRef<IFile> ncaFile = new();

                                pfs.OpenFile(ref ncaFile.Ref, fileEntry.FullPath.ToU8Span(), OpenMode.Read).ThrowIfFailure();

                                Nca nca = TryOpenNca(ncaFile.Get.AsStorage());
                                if (nca == null)
                                {
                                    continue;
                                }

                                if (nca.Header.ContentType == NcaContentType.PublicData)
                                {
                                    titleUpdates.Add(new DownloadableContentModel(nca.Header.TitleId, filePath, fileEntry.FullPath));
                                }
                            }

                            return titleUpdates.Count != 0;
                        }
                }
            }
            catch (MissingKeyException exception)
            {
                Logger.Warning?.Print(LogClass.Application, $"Your key set is missing a key with the name: {exception.Name}");
            }
            catch (InvalidDataException)
            {
                Logger.Warning?.Print(LogClass.Application, $"The header key is incorrect or missing and therefore the NCA header content type check has failed. Errored File: {filePath}");
            }
            catch (IOException exception)
            {
                Logger.Warning?.Print(LogClass.Application, exception.Message);
            }
            catch (Exception exception)
            {
                Logger.Warning?.Print(LogClass.Application, $"The file encountered was not of a valid type. File: '{filePath}' Error: {exception}");
            }

            return false;
        }

        public bool TryGetTitleUpdatesFromFile(string filePath, out List<TitleUpdateModel> titleUpdates)
        {
            titleUpdates = [];

            try
            {
                string extension = Path.GetExtension(filePath).ToLower();

                using FileStream file = new(filePath, FileMode.Open, FileAccess.Read);

                switch (extension)
                {
                    case ".xci":
                    case ".nsp":
                        {
                            IntegrityCheckLevel checkLevel = ConfigurationState.Instance.System.EnableFsIntegrityChecks
                                ? IntegrityCheckLevel.ErrorOnInvalid
                                : IntegrityCheckLevel.None;

                            using IFileSystem pfs =
                                PartitionFileSystemUtils.OpenApplicationFileSystem(filePath, _virtualFileSystem);

                            Dictionary<ulong, ContentMetaData> updates =
                                pfs.GetContentData(ContentMetaType.Patch, _virtualFileSystem, checkLevel);

                            if (updates.Count == 0)
                            {
                                return false;
                            }

                            foreach ((_, ContentMetaData content) in updates)
                            {
                                Nca patchNca = content.GetNcaByType(_virtualFileSystem.KeySet, ContentType.Program);
                                Nca controlNca = content.GetNcaByType(_virtualFileSystem.KeySet, ContentType.Control);

                                if (controlNca != null && patchNca != null)
                                {
                                    ApplicationControlProperty controlData = new();

                                    using UniqueRef<IFile> nacpFile = new();

                                    controlNca.OpenFileSystem(NcaSectionType.Data, IntegrityCheckLevel.None)
                                        .OpenFile(ref nacpFile.Ref, "/control.nacp".ToU8Span(), OpenMode.Read)
                                        .ThrowIfFailure();
                                    nacpFile.Get.Read(out _, 0, LibHac.Common.SpanHelpers.AsByteSpan(ref controlData),
                                        ReadOption.None).ThrowIfFailure();

                                    string displayVersion = controlData.DisplayVersionString.ToString();
                                    TitleUpdateModel update = new(content.ApplicationId, content.Version.Version,
                                        displayVersion, filePath);

                                    titleUpdates.Add(update);
                                }
                            }

                            return true;
                        }
                }
            }
            catch (MissingKeyException exception)
            {
                Logger.Warning?.Print(LogClass.Application, $"Your key set is missing a key with the name: {exception.Name}");
            }
            catch (InvalidDataException)
            {
                Logger.Warning?.Print(LogClass.Application, $"The header key is incorrect or missing and therefore the NCA header content type check has failed. Errored File: {filePath}");
            }
            catch (IOException exception)
            {
                Logger.Warning?.Print(LogClass.Application, exception.Message);
            }
            catch (Exception exception)
            {
                Logger.Warning?.Print(LogClass.Application, $"The file encountered was not of a valid type. File: '{filePath}' Error: {exception}");
            }

            return false;
        }

        public void CancelLoading()
        {
            _cancellationToken?.Cancel();
        }

        public static void ReadControlData(IFileSystem controlFs, Span<byte> outProperty)
        {
            using UniqueRef<IFile> controlFile = new();

            controlFs.OpenFile(ref controlFile.Ref, "/control.nacp".ToU8Span(), OpenMode.Read).ThrowIfFailure();
            controlFile.Get.Read(out _, 0, outProperty, ReadOption.None).ThrowIfFailure();
        }

        public void LoadApplications(List<string> appDirs)
        {
            int numApplicationsFound = 0;
            int numApplicationsLoaded = 0;

            _cancellationToken = new CancellationTokenSource();
            _applications.Clear();

            // Builds the applications list with paths to found applications
            List<string> applicationPaths = [];

            try
            {
                foreach (string appDir in appDirs)
                {
                    if (_cancellationToken.Token.IsCancellationRequested)
                    {
                        return;
                    }

                    if (!Directory.Exists(appDir))
                    {
                        Logger.Warning?.Print(LogClass.Application, $"The specified game directory \"{appDir}\" does not exist.");

                        continue;
                    }

                    try
                    {
                        EnumerationOptions options = new()
                        {
                            RecurseSubdirectories = true,
                            IgnoreInaccessible = false
                        };

                        IEnumerable<string> files = Directory.EnumerateFiles(appDir, "*", options)
                            .Where(file =>
                                (Path.GetExtension(file).ToLower() is ".nsp" && ConfigurationState.Instance.UI.ShownFileTypes.NSP) ||
                                (Path.GetExtension(file).ToLower() is ".pfs0" && ConfigurationState.Instance.UI.ShownFileTypes.PFS0) ||
                                (Path.GetExtension(file).ToLower() is ".xci" && ConfigurationState.Instance.UI.ShownFileTypes.XCI) ||
                                (Path.GetExtension(file).ToLower() is ".nca" && ConfigurationState.Instance.UI.ShownFileTypes.NCA) ||
                                (Path.GetExtension(file).ToLower() is ".nro" && ConfigurationState.Instance.UI.ShownFileTypes.NRO) ||
                                (Path.GetExtension(file).ToLower() is ".nso" && ConfigurationState.Instance.UI.ShownFileTypes.NSO)
                            );

                        foreach (string app in files)
                        {
                            if (_cancellationToken.Token.IsCancellationRequested)
                            {
                                return;
                            }

                            FileInfo fileInfo = new(app);

                            try
                            {
                                string fullPath = fileInfo.ResolveLinkTarget(true)?.FullName ?? fileInfo.FullName;

                                applicationPaths.Add(fullPath);
                                numApplicationsFound++;
                            }
                            catch (IOException exception)
                            {
                                Logger.Warning?.Print(LogClass.Application, $"Failed to resolve the full path to file: \"{app}\" Error: {exception}");
                            }
                        }
                    }
                    catch (UnauthorizedAccessException)
                    {
                        Logger.Warning?.Print(LogClass.Application, $"Failed to get access to directory: \"{appDir}\"");
                    }
                }


                // Loops through applications list, creating a struct and then firing an event containing the struct for each application
                foreach (string applicationPath in applicationPaths)
                {
                    if (_cancellationToken.Token.IsCancellationRequested)
                    {
                        return;
                    }

                    if (TryGetApplicationsFromFile(applicationPath, out List<ApplicationData> applications))
                    {
                        _applications.Edit(it =>
                        {
                            foreach (ApplicationData application in applications)
                            {
                                it.AddOrUpdate(application);
                                LoadDlcForApplication(application);
                                if (LoadTitleUpdatesForApplication(application))
                                {
                                    // Trigger a reload of the version data
                                    RefreshApplicationInfo(application.IdBase);
                                }
                            }
                        });

                        if (applications.Count > 1)
                        {
                            numApplicationsFound += applications.Count - 1;
                        }

                        numApplicationsLoaded += applications.Count;
                    }
                    else
                    {
                        numApplicationsFound--;
                    }

                    OnApplicationCountUpdated(new ApplicationCountUpdatedEventArgs
                    {
                        NumAppsFound = numApplicationsFound,
                        NumAppsLoaded = numApplicationsLoaded,
                    });
                }

                OnApplicationCountUpdated(new ApplicationCountUpdatedEventArgs
                {
                    NumAppsFound = numApplicationsFound,
                    NumAppsLoaded = numApplicationsLoaded,
                });
            }
            finally
            {
                _cancellationToken.Dispose();
                _cancellationToken = null;
            }
        }

        public async Task RefreshLdn()
        {
            if (ConfigurationState.Instance.Multiplayer.Mode == MultiplayerMode.LdnRyu)
            {
                try
                {
                    string ldnWebHost = ConfigurationState.Instance.Multiplayer.LdnServer;
                    if (string.IsNullOrEmpty(ldnWebHost))
                    {
                        ldnWebHost = SharedConstants.DefaultLanPlayWebHost;
                    }
                    
                    using HttpClient httpClient = new();
                    string ldnGameDataArrayString = await httpClient.GetStringAsync($"https://{ldnWebHost}/api/public_games");
                    LdnGameData[] ldnGameDataArray = JsonHelper.Deserialize(ldnGameDataArrayString, _ldnDataSerializerContext.IEnumerableLdnGameData).ToArray();
                    LdnGameDataReceived?.Invoke(new LdnGameDataReceivedEventArgs(ldnGameDataArray));
                    return;
                }
                catch (Exception ex)
                {
                    Logger.Warning?.Print(LogClass.Application, $"Failed to fetch the public games JSON from the API. Player and game count in the game list will be unavailable.\n{ex.Message}");
                }
            }
            
            LdnGameDataReceived?.Invoke(LdnGameDataReceivedEventArgs.Empty);
        }

        // Replace the currently stored DLC state for the game with the provided DLC state.
        public void SaveDownloadableContentsForGame(ApplicationData application, List<(DownloadableContentModel, bool IsEnabled)> dlcs)
        {
            _downloadableContents.Edit(it =>
            {
                DownloadableContentsHelper.SaveDownloadableContentsJson(application.IdBase, dlcs);

                it.Remove(it.Items.Where(item => item.Dlc.TitleIdBase == application.IdBase));
                it.AddOrUpdate(dlcs);
            });
        }

        // Replace the currently stored update state for the game with the provided update state.
        public void SaveTitleUpdatesForGame(ApplicationData application, List<(TitleUpdateModel, bool IsSelected)> updates)
        {
            _titleUpdates.Edit(it =>
            {
                TitleUpdatesHelper.SaveTitleUpdatesJson(application.IdBase, updates);

                it.Remove(it.Items.Where(item => item.TitleUpdate.TitleIdBase == application.IdBase));
                it.AddOrUpdate(updates);
                RefreshApplicationInfo(application.IdBase);
            });
        }

        // Searches the provided directories for DLC NSP files that are _valid for the currently detected games in the
        // library_, and then enables those DLC.
        public int AutoLoadDownloadableContents(List<string> appDirs, out int numDlcRemoved)
        {
            _cancellationToken = new CancellationTokenSource();

            List<string> dlcPaths = [];
            int newDlcLoaded = 0;
            numDlcRemoved = 0;

            try
            {
                // Remove any downloadable content which can no longer be located on disk
                List<(DownloadableContentModel Dlc, bool IsEnabled)> dlcToRemove = _downloadableContents.Items
                    .Where(dlc => !File.Exists(dlc.Dlc.ContainerPath))
                    .ToList();
                dlcToRemove.ForEach(dlc =>
                    Logger.Warning?.Print(LogClass.Application, $"Title DLC removed: {dlc.Dlc.ContainerPath}")
                );
                numDlcRemoved += dlcToRemove.Distinct().Count();
                _downloadableContents.RemoveKeys(dlcToRemove.Select(dlc => dlc.Dlc));

                foreach (string appDir in appDirs)
                {
                    if (_cancellationToken.Token.IsCancellationRequested)
                    {
                        return newDlcLoaded;
                    }

                    if (!Directory.Exists(appDir))
                    {
                        Logger.Warning?.Print(LogClass.Application,
                            $"The specified autoload directory \"{appDir}\" does not exist.");

                        continue;
                    }

                    try
                    {
                        EnumerationOptions options = new()
                        {
                            RecurseSubdirectories = true,
                            IgnoreInaccessible = false,
                        };

                        IEnumerable<string> files = Directory.EnumerateFiles(appDir, "*", options).Where(
                            file => Path.GetExtension(file).ToLower() is ".nsp");

                        foreach (string app in files)
                        {
                            if (_cancellationToken.Token.IsCancellationRequested)
                            {
                                return newDlcLoaded;
                            }

                            FileInfo fileInfo = new(app);

                            try
                            {
                                string fullPath = fileInfo.ResolveLinkTarget(true)?.FullName ?? fileInfo.FullName;

                                dlcPaths.Add(fullPath);
                            }
                            catch (IOException exception)
                            {
                                Logger.Warning?.Print(LogClass.Application,
                                    $"Failed to resolve the full path to file: \"{app}\" Error: {exception}");
                            }
                        }
                    }
                    catch (UnauthorizedAccessException)
                    {
                        Logger.Warning?.Print(LogClass.Application,
                            $"Failed to get access to directory: \"{appDir}\"");
                    }
                }

                HashSet<ulong> appIdLookup = Applications.Items.Select(it => it.IdBase).ToHashSet();

                foreach (string dlcPath in dlcPaths)
                {
                    if (_cancellationToken.Token.IsCancellationRequested)
                    {
                        return newDlcLoaded;
                    }

                    if (TryGetDownloadableContentFromFile(dlcPath, out List<DownloadableContentModel> foundDlcs))
                    {
                        foreach (DownloadableContentModel dlc in foundDlcs.Where(it => appIdLookup.Contains(it.TitleIdBase)))
                        {
                            if (!_downloadableContents.Lookup(dlc).HasValue)
                            {
                                _downloadableContents.AddOrUpdate((dlc, true));
                                SaveDownloadableContentsForGame(dlc.TitleIdBase);
                                newDlcLoaded++;
                            }
                        }
                    }
                }
            }
            finally
            {
                _cancellationToken.Dispose();
                _cancellationToken = null;
            }

            return newDlcLoaded;
        }

        // Searches the provided directories for update NSP files that are _valid for the currently detected games in the
        // library_, and then applies those updates. If a newly-detected update is a newer version than the currently
        // selected update (or if no update is currently selected), then that update will be selected.
        public int AutoLoadTitleUpdates(List<string> appDirs, out int numUpdatesRemoved)
        {
            _cancellationToken = new CancellationTokenSource();

            List<string> updatePaths = [];
            int numUpdatesLoaded = 0;
            numUpdatesRemoved = 0;

            try
            {
                HashSet<ulong> titleIdsToSave = [];
                HashSet<ulong> titleIdsToRefresh = [];

                // Remove any updates which can no longer be located on disk
                List<(TitleUpdateModel TitleUpdate, bool IsSelected)> updatesToRemove = _titleUpdates.Items
                    .Where(it => !File.Exists(it.TitleUpdate.Path))
                    .ToList();

                numUpdatesRemoved += updatesToRemove.Select(it => it.TitleUpdate).Distinct().Count();
                updatesToRemove.ForEach(ti =>
                    Logger.Warning?.Print(LogClass.Application, $"Title update removed: {ti.TitleUpdate.Path}")
                );
                _titleUpdates.RemoveKeys(updatesToRemove.Select(it => it.TitleUpdate));
                titleIdsToSave.UnionWith(updatesToRemove.Select(it => it.TitleUpdate.TitleIdBase));
                titleIdsToRefresh.UnionWith(updatesToRemove.Where(it => it.IsSelected).Select(update => update.TitleUpdate.TitleIdBase));

                foreach (string appDir in appDirs)
                {
                    if (_cancellationToken.Token.IsCancellationRequested)
                    {
                        return numUpdatesLoaded;
                    }

                    if (!Directory.Exists(appDir))
                    {
                        Logger.Warning?.Print(LogClass.Application,
                            $"The specified autoload directory \"{appDir}\" does not exist.");

                        continue;
                    }

                    try
                    {
                        EnumerationOptions options = new()
                        {
                            RecurseSubdirectories = true,
                            IgnoreInaccessible = false,
                        };

                        IEnumerable<string> files = Directory.EnumerateFiles(appDir, "*", options).Where(
                            file => Path.GetExtension(file).ToLower() is ".nsp");

                        foreach (string app in files)
                        {
                            if (_cancellationToken.Token.IsCancellationRequested)
                            {
                                return numUpdatesLoaded;
                            }

                            FileInfo fileInfo = new(app);

                            try
                            {
                                string fullPath = fileInfo.ResolveLinkTarget(true)?.FullName ?? fileInfo.FullName;

                                updatePaths.Add(fullPath);
                            }
                            catch (IOException exception)
                            {
                                Logger.Warning?.Print(LogClass.Application,
                                    $"Failed to resolve the full path to file: \"{app}\" Error: {exception}");
                            }
                        }
                    }
                    catch (UnauthorizedAccessException)
                    {
                        Logger.Warning?.Print(LogClass.Application,
                            $"Failed to get access to directory: \"{appDir}\"");
                    }
                }

                HashSet<ulong> appIdLookup = Applications.Items.Select(it => it.IdBase).ToHashSet();

                foreach (string updatePath in updatePaths)
                {
                    if (_cancellationToken.Token.IsCancellationRequested)
                    {
                        return numUpdatesLoaded;
                    }

                    if (TryGetTitleUpdatesFromFile(updatePath, out List<TitleUpdateModel> foundUpdates))
                    {
                        foreach (TitleUpdateModel update in foundUpdates.Where(it => appIdLookup.Contains(it.TitleIdBase)))
                        {
                            if (!_titleUpdates.Lookup(update).HasValue)
                            {
                                bool shouldSelect = AddAndAutoSelectUpdate(update);
                                titleIdsToSave.Add(update.TitleIdBase);
                                numUpdatesLoaded++;

                                if (shouldSelect)
                                {
                                    titleIdsToRefresh.Add(update.TitleIdBase);
                                }
                            }
                        }
                    }
                }

                titleIdsToSave.ForEach(titleId => SaveTitleUpdatesForGame(titleId));
                titleIdsToRefresh.ForEach(titleId => RefreshApplicationInfo(titleId));
            }
            finally
            {
                _cancellationToken.Dispose();
                _cancellationToken = null;
            }

            return numUpdatesLoaded;
        }

        private bool AddAndAutoSelectUpdate(TitleUpdateModel update)
        {
            if (update == null) return false;

            DynamicData.Kernel.Optional<(TitleUpdateModel TitleUpdate, bool IsSelected)> currentlySelected = TitleUpdates.Items.FirstOrOptional(it =>
                it.TitleUpdate.TitleIdBase == update.TitleIdBase && it.IsSelected);

            bool shouldSelect = !currentlySelected.HasValue ||
                                currentlySelected.Value.TitleUpdate.Version < update.Version;

            _titleUpdates.AddOrUpdate((update, shouldSelect));
            
            if (currentlySelected.HasValue && shouldSelect)
            {
                _titleUpdates.AddOrUpdate((currentlySelected.Value.TitleUpdate, false));
            }

            return shouldSelect;
        }

        protected void OnApplicationCountUpdated(ApplicationCountUpdatedEventArgs e)
        {
            ApplicationCountUpdated?.Invoke(null, e);
        }

        public static ApplicationMetadata LoadAndSaveMetaData(string titleId, Action<ApplicationMetadata> modifyFunction = null)
        {
            string metadataFolder = Path.Combine(AppDataManager.GamesDirPath, titleId, "gui");
            string metadataFile = Path.Combine(metadataFolder, "metadata.json");

            ApplicationMetadata appMetadata;

            if (!File.Exists(metadataFile))
            {
                Directory.CreateDirectory(metadataFolder);

                appMetadata = new ApplicationMetadata();

                JsonHelper.SerializeToFile(metadataFile, appMetadata, _serializerContext.ApplicationMetadata);
            }

            try
            {
                appMetadata = JsonHelper.DeserializeFromFile(metadataFile, _serializerContext.ApplicationMetadata);
            }
            catch (JsonException)
            {
                Logger.Warning?.Print(LogClass.Application, $"Failed to parse metadata json for {titleId}. Loading defaults.");

                appMetadata = new ApplicationMetadata();
            }

            if (modifyFunction != null)
            {
                modifyFunction(appMetadata);

                JsonHelper.SerializeToFile(metadataFile, appMetadata, _serializerContext.ApplicationMetadata);
            }

            return appMetadata;
        }

        public byte[] GetApplicationIcon(string applicationPath, Language desiredTitleLanguage, ulong applicationId)
        {
            byte[] applicationIcon = null;

            if (applicationId == 0)
            {
                if (Directory.Exists(applicationPath))
                {
                    return _ncaIcon;
                }

                return Path.GetExtension(applicationPath).ToLower() switch
                {
                    ".nsp" => _nspIcon,
                    ".pfs0" => _nspIcon,
                    ".xci" => _xciIcon,
                    ".nso" => _nsoIcon,
                    ".nro" => _nroIcon,
                    ".nca" => _ncaIcon,
                    _ => _ncaIcon,
                };
            }

            try
            {
                // Look for icon only if applicationPath is not a directory
                if (!Directory.Exists(applicationPath))
                {
                    string extension = Path.GetExtension(applicationPath).ToLower();

                    using FileStream file = new(applicationPath, FileMode.Open, FileAccess.Read);

                    if (extension is ".nsp" or ".pfs0" or ".xci")
                    {
                        try
                        {
                            IFileSystem pfs;

                            bool isExeFs = false;

                            if (extension == ".xci")
                            {
                                Xci xci = new(_virtualFileSystem.KeySet, file.AsStorage());

                                pfs = xci.OpenPartition(XciPartitionType.Secure);
                            }
                            else
                            {
                                PartitionFileSystem pfsTemp = new();
                                pfsTemp.Initialize(file.AsStorage()).ThrowIfFailure();
                                pfs = pfsTemp;

                                foreach (DirectoryEntryEx fileEntry in pfs.EnumerateEntries("/", "*"))
                                {
                                    if (Path.GetFileNameWithoutExtension(fileEntry.FullPath) == "main")
                                    {
                                        isExeFs = true;
                                    }
                                }
                            }

                            if (isExeFs)
                            {
                                applicationIcon = _nspIcon;
                            }
                            else
                            {
                                // Store the ControlFS in variable called controlFs
                                Dictionary<ulong, ContentMetaData> programs = pfs.GetContentData(ContentMetaType.Application, _virtualFileSystem, _checkLevel);
                                IFileSystem controlFs = null;

                                if (programs.TryGetValue(applicationId, out ContentMetaData value))
                                {
                                    if (value.GetNcaByType(_virtualFileSystem.KeySet, ContentType.Control) is { } controlNca)
                                    {
                                        controlFs = controlNca.OpenFileSystem(NcaSectionType.Data, IntegrityCheckLevel.None);
                                    }
                                }

                                // Read the icon from the ControlFS and store it as a byte array
                                try
                                {
                                    using UniqueRef<IFile> icon = new();

                                    controlFs.OpenFile(ref icon.Ref, $"/icon_{desiredTitleLanguage}.dat".ToU8Span(), OpenMode.Read).ThrowIfFailure();

                                    using MemoryStream stream = new();

                                    icon.Get.AsStream().CopyTo(stream);
                                    applicationIcon = stream.ToArray();
                                }
                                catch (HorizonResultException)
                                {
                                    foreach (DirectoryEntryEx entry in controlFs.EnumerateEntries("/", "*"))
                                    {
                                        if (entry.Name == "control.nacp")
                                        {
                                            continue;
                                        }

                                        using UniqueRef<IFile> icon = new();

                                        controlFs.OpenFile(ref icon.Ref, entry.FullPath.ToU8Span(), OpenMode.Read).ThrowIfFailure();

                                        using MemoryStream stream = new();
                                        icon.Get.AsStream().CopyTo(stream);
                                        applicationIcon = stream.ToArray();

                                        break;
                                    }

                                    applicationIcon ??= extension == ".xci" ? _xciIcon : _nspIcon;
                                }
                            }
                        }
                        catch (MissingKeyException)
                        {
                            applicationIcon = extension == ".xci" ? _xciIcon : _nspIcon;
                        }
                        catch (InvalidDataException)
                        {
                            applicationIcon = extension == ".xci" ? _xciIcon : _nspIcon;
                        }
                        catch (Exception exception)
                        {
                            Logger.Warning?.Print(LogClass.Application, $"The file encountered was not of a valid type. File: '{applicationPath}' Error: {exception}");
                        }
                    }
                    else if (extension == ".nro")
                    {
                        BinaryReader reader = new(file);

                        byte[] Read(long position, int size)
                        {
                            file.Seek(position, SeekOrigin.Begin);

                            return reader.ReadBytes(size);
                        }

                        try
                        {
                            file.Seek(24, SeekOrigin.Begin);

                            int assetOffset = reader.ReadInt32();

                            if (Encoding.ASCII.GetString(Read(assetOffset, 4)) == "ASET")
                            {
                                byte[] iconSectionInfo = Read(assetOffset + 8, 0x10);

                                long iconOffset = BitConverter.ToInt64(iconSectionInfo, 0);
                                long iconSize = BitConverter.ToInt64(iconSectionInfo, 8);

                                // Reads and stores game icon as byte array
                                if (iconSize > 0)
                                {
                                    applicationIcon = Read(assetOffset + iconOffset, (int)iconSize);
                                }
                                else
                                {
                                    applicationIcon = _nroIcon;
                                }
                            }
                            else
                            {
                                applicationIcon = _nroIcon;
                            }
                        }
                        catch
                        {
                            Logger.Warning?.Print(LogClass.Application, $"The file encountered was not of a valid type. Errored File: {applicationPath}");
                        }
                    }
                    else if (extension == ".nca")
                    {
                        applicationIcon = _ncaIcon;
                    }
                    // If its an NSO we just set defaults
                    else if (extension == ".nso")
                    {
                        applicationIcon = _nsoIcon;
                    }
                }
            }
            catch (Exception)
            {
                Logger.Warning?.Print(LogClass.Application, $"Could not retrieve a valid icon for the app. Default icon will be used. Errored File: {applicationPath}");
            }

            return applicationIcon ?? _ncaIcon;
        }

        private void GetApplicationInformation(ref ApplicationControlProperty controlData, ref ApplicationData data)
        {
            _ = Enum.TryParse(DesiredLanguage.ToString(), out TitleLanguage desiredTitleLanguage);

            if (controlData.Title.ItemsRo.Length > (int)desiredTitleLanguage)
            {
                data.Name = controlData.Title[(int)desiredTitleLanguage].NameString.ToString();
                data.Developer = controlData.Title[(int)desiredTitleLanguage].PublisherString.ToString();
            }
            else
            {
                data.Name = null;
                data.Developer = null;
            }

            if (string.IsNullOrWhiteSpace(data.Name))
            {
                foreach (ref readonly ApplicationControlProperty.ApplicationTitle controlTitle in controlData.Title.ItemsRo)
                {
                    if (!controlTitle.NameString.IsEmpty())
                    {
                        data.Name = controlTitle.NameString.ToString();

                        break;
                    }
                }
            }

            if (string.IsNullOrWhiteSpace(data.Developer))
            {
                foreach (ref readonly ApplicationControlProperty.ApplicationTitle controlTitle in controlData.Title.ItemsRo)
                {
                    if (!controlTitle.PublisherString.IsEmpty())
                    {
                        data.Developer = controlTitle.PublisherString.ToString();

                        break;
                    }
                }
            }

            if (data.Id == 0)
            {
                if (controlData.SaveDataOwnerId != 0)
                {
                    data.Id = controlData.SaveDataOwnerId;
                }
                else if (controlData.PresenceGroupId != 0)
                {
                    data.Id = controlData.PresenceGroupId;
                }
                else if (controlData.AddOnContentBaseId != 0)
                {
                    data.Id = (controlData.AddOnContentBaseId - 0x1000);
                }
            }

            data.Version = controlData.DisplayVersionString.ToString();
        }

        private bool IsUpdateApplied(Nca mainNca, out IFileSystem updatedControlFs)
        {
            updatedControlFs = null;

            string updatePath = null;

            try
            {
                (Nca patchNca, Nca controlNca) = mainNca.GetUpdateData(_virtualFileSystem, _checkLevel, 0, out updatePath);

                if (patchNca != null && controlNca != null)
                {
                    updatedControlFs = controlNca.OpenFileSystem(NcaSectionType.Data, IntegrityCheckLevel.None);

                    return true;
                }
            }
            catch (InvalidDataException)
            {
                Logger.Warning?.Print(LogClass.Application, $"The header key is incorrect or missing and therefore the NCA header content type check has failed. Errored File: {updatePath}");
            }
            catch (MissingKeyException exception)
            {
                Logger.Warning?.Print(LogClass.Application, $"Your key set is missing a key with the name: {exception.Name}. Errored File: {updatePath}");
            }

            return false;
        }

        private Nca TryOpenNca(IStorage ncaStorage)
        {
            try
            {
                return new Nca(_virtualFileSystem.KeySet, ncaStorage);
            }
            catch (Exception) { }

            return null;
        }

        // Does a two-phase load of DLC. First reading the metadata on disk, then loading anything bundled in the game
        // file itself
        private void LoadDlcForApplication(ApplicationData application)
        {
            _downloadableContents.Edit(it =>
            {
                List<(DownloadableContentModel, bool IsEnabled)> savedDlc =
                    DownloadableContentsHelper.LoadDownloadableContentsJson(_virtualFileSystem, application.IdBase);
                it.AddOrUpdate(savedDlc);

                if (TryGetDownloadableContentFromFile(application.Path, out List<DownloadableContentModel> bundledDlc))
                {
                    HashSet<DownloadableContentModel> savedDlcLookup = savedDlc.Select(dlc => dlc.Item1).ToHashSet();

                    bool addedNewDlc = false;
                    foreach (DownloadableContentModel dlc in bundledDlc)
                    {
                        if (!savedDlcLookup.Contains(dlc))
                        {
                            addedNewDlc = true;
                            it.AddOrUpdate((dlc, true));
                        }
                    }

                    if (addedNewDlc)
                    {
                        List<(DownloadableContentModel Dlc, bool IsEnabled)> gameDlcs = it.Items.Where(dlc => dlc.Dlc.TitleIdBase == application.IdBase).ToList();
                        DownloadableContentsHelper.SaveDownloadableContentsJson(application.IdBase,
                            gameDlcs);
                    }
                }
            });
        }

        // Does a two-phase load of updates. First reading the metadata on disk, then loading anything bundled in the game
        // file itself
        private bool LoadTitleUpdatesForApplication(ApplicationData application)
        {
            bool modifiedVersion = false;

            _titleUpdates.Edit(it =>
            {
                List<(TitleUpdateModel Update, bool IsSelected)> savedUpdates =
                    TitleUpdatesHelper.LoadTitleUpdatesJson(_virtualFileSystem, application.IdBase);
                it.AddOrUpdate(savedUpdates);

                DynamicData.Kernel.Optional<(TitleUpdateModel Update, bool IsSelected)> selectedUpdate = savedUpdates.FirstOrOptional(update => update.IsSelected);

                if (TryGetTitleUpdatesFromFile(application.Path, out List<TitleUpdateModel> bundledUpdates))
                {
                    HashSet<TitleUpdateModel> savedUpdateLookup = savedUpdates.Select(update => update.Update).ToHashSet();
                    bool updatesChanged = false;

                    foreach (TitleUpdateModel update in bundledUpdates.OrderByDescending(bundled => bundled.Version))
                    {
                        if (!savedUpdateLookup.Contains(update))
                        {
                            bool shouldSelect = false;
                            if (!selectedUpdate.HasValue || selectedUpdate.Value.Item1.Version < update.Version)
                            {
                                shouldSelect = true;
                                if (selectedUpdate.HasValue)
                                    _titleUpdates.AddOrUpdate((selectedUpdate.Value.Update, false));
                                selectedUpdate = (update, true);
                            }

                            modifiedVersion = modifiedVersion || shouldSelect;
                            it.AddOrUpdate((update, shouldSelect));

                            updatesChanged = true;
                        }
                    }

                    if (updatesChanged)
                    {
                        List<(TitleUpdateModel TitleUpdate, bool IsSelected)> gameUpdates = it.Items.Where(update => update.TitleUpdate.TitleIdBase == application.IdBase).ToList();
                        TitleUpdatesHelper.SaveTitleUpdatesJson(application.IdBase, gameUpdates);
                    }
                }
            });

            return modifiedVersion;
        }

        // Save the _currently tracked_ DLC state for the game
        private void SaveDownloadableContentsForGame(ulong titleIdBase)
        {
            List<(DownloadableContentModel Dlc, bool IsEnabled)> dlcs = DownloadableContents.Items.Where(dlc => dlc.Dlc.TitleIdBase == titleIdBase).ToList();
            DownloadableContentsHelper.SaveDownloadableContentsJson(titleIdBase, dlcs);
        }

        // Save the _currently tracked_ update state for the game
        private void SaveTitleUpdatesForGame(ulong titleIdBase)
        {
            List<(TitleUpdateModel TitleUpdate, bool IsSelected)> updates = TitleUpdates.Items.Where(update => update.TitleUpdate.TitleIdBase == titleIdBase).ToList();
            TitleUpdatesHelper.SaveTitleUpdatesJson(titleIdBase, updates);
        }

        // ApplicationData isnt live-updating (e.g. when an update gets applied) and so this is meant to trigger a refresh
        // of its state
        private void RefreshApplicationInfo(ulong appIdBase)
        {
            DynamicData.Kernel.Optional<ApplicationData> application = _applications.Lookup(appIdBase);

            if (!application.HasValue)
                return;

            if (!TryGetApplicationsFromFile(application.Value.Path, out List<ApplicationData> newApplications))
                return;

            ApplicationData newApplication = newApplications.First(it => it.IdBase == appIdBase);
            _applications.AddOrUpdate(newApplication);
        }
    }
}
