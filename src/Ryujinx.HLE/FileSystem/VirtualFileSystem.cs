using LibHac;
using LibHac.Common;
using LibHac.Common.Keys;
using LibHac.Fs;
using LibHac.Fs.Fsa;
using LibHac.Fs.Shim;
using LibHac.FsSrv;
using LibHac.FsSystem;
using LibHac.Ncm;
using LibHac.Sdmmc;
using LibHac.Spl;
using LibHac.Tools.Es;
using LibHac.Tools.Fs;
using LibHac.Tools.FsSystem;
using Ryujinx.Common.Configuration;
using Ryujinx.Common.Logging;
using Ryujinx.HLE.HOS;
using System;
using System.Buffers.Text;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using Path = System.IO.Path;

namespace Ryujinx.HLE.FileSystem
{
    public class VirtualFileSystem : IDisposable
    {
        public static readonly string SafeNandPath = Path.Combine(AppDataManager.DefaultNandDir, "safe");
        public static readonly string SystemNandPath = Path.Combine(AppDataManager.DefaultNandDir, "system");
        public static readonly string UserNandPath = Path.Combine(AppDataManager.DefaultNandDir, "user");

        public KeySet KeySet { get; private set; }
        public EmulatedGameCard GameCard { get; private set; }
        public SdmmcApi SdCard { get; private set; }
        public ModLoader ModLoader { get; private set; }

        private readonly ConcurrentDictionary<ulong, Stream> _romFsByPid;

        private static bool _isInitialized = false;

        public static VirtualFileSystem CreateInstance()
        {
            if (_isInitialized)
            {
                throw new InvalidOperationException("VirtualFileSystem can only be instantiated once!");
            }

            _isInitialized = true;

            return new VirtualFileSystem();
        }

        private VirtualFileSystem()
        {
            ReloadKeySet();
            ModLoader = new ModLoader(); // Should only be created once
            _romFsByPid = new ConcurrentDictionary<ulong, Stream>();
        }

        public void LoadRomFs(ulong pid, string fileName)
        {
            FileStream romfsStream = new(fileName, FileMode.Open, FileAccess.Read);

            _romFsByPid.AddOrUpdate(pid, romfsStream, (pid, oldStream) =>
            {
                oldStream.Close();

                return romfsStream;
            });
        }

        public void SetRomFs(ulong pid, Stream romfsStream)
        {
            _romFsByPid.AddOrUpdate(pid, romfsStream, (pid, oldStream) =>
            {
                oldStream.Close();

                return romfsStream;
            });
        }

        public Stream GetRomFs(ulong pid)
        {
            return _romFsByPid[pid];
        }

        public static string GetFullPath(string basePath, string fileName)
        {
            if (fileName.StartsWith("//"))
            {
                fileName = fileName[2..];
            }
            else if (fileName.StartsWith('/'))
            {
                fileName = fileName[1..];
            }
            else
            {
                return null;
            }

            string fullPath = Path.GetFullPath(Path.Combine(basePath, fileName));

            if (!fullPath.StartsWith(AppDataManager.BaseDirPath))
            {
                return null;
            }

            return fullPath;
        }

        internal static string GetSdCardPath() => MakeFullPath(AppDataManager.DefaultSdcardDir);
        public static string GetNandPath() => MakeFullPath(AppDataManager.DefaultNandDir);

        public static string SwitchPathToSystemPath(string switchPath)
        {
            string[] parts = switchPath.Split(":");

            if (parts.Length != 2)
            {
                return null;
            }

            return GetFullPath(MakeFullPath(parts[0]), parts[1]);
        }

        public static string SystemPathToSwitchPath(string systemPath)
        {
            string baseSystemPath = AppDataManager.BaseDirPath + Path.DirectorySeparatorChar;

            if (systemPath.StartsWith(baseSystemPath))
            {
                string rawPath = systemPath.Replace(baseSystemPath, string.Empty);
                int firstSeparatorOffset = rawPath.IndexOf(Path.DirectorySeparatorChar);

                if (firstSeparatorOffset == -1)
                {
                    return $"{rawPath}:/";
                }

                ReadOnlySpan<char> basePath = rawPath.AsSpan(0, firstSeparatorOffset);
                ReadOnlySpan<char> fileName = rawPath.AsSpan(firstSeparatorOffset + 1);

                return $"{basePath}:/{fileName}";
            }

            return null;
        }

        private static string MakeFullPath(string path, bool isDirectory = true)
        {
            // Handles Common Switch Content Paths
            switch (path)
            {
                case ContentPath.SdCard:
                    path = AppDataManager.DefaultSdcardDir;
                    break;
                case ContentPath.User:
                    path = UserNandPath;
                    break;
                case ContentPath.System:
                    path = SystemNandPath;
                    break;
                case ContentPath.SdCardContent:
                    path = Path.Combine(AppDataManager.DefaultSdcardDir, "Nintendo", "Contents");
                    break;
                case ContentPath.UserContent:
                    path = Path.Combine(UserNandPath, "Contents");
                    break;
                case ContentPath.SystemContent:
                    path = Path.Combine(SystemNandPath, "Contents");
                    break;
            }

            string fullPath = Path.Combine(AppDataManager.BaseDirPath, path);

            if (isDirectory && !Directory.Exists(fullPath))
            {
                Directory.CreateDirectory(fullPath);
            }

            return fullPath;
        }

        public void InitializeFsServer(LibHac.Horizon horizon, out HorizonClient fsServerClient)
        {
            LocalFileSystem serverBaseFs = new(useUnixTimeStamps: true);
            Result result = serverBaseFs.Initialize(AppDataManager.BaseDirPath, LocalFileSystem.PathMode.DefaultCaseSensitivity, ensurePathExists: true);
            if (result.IsFailure())
            {
                throw new HorizonResultException(result, "Error creating LocalFileSystem.");
            }

            fsServerClient = horizon.CreatePrivilegedHorizonClient();
            FileSystemServer fsServer = new(fsServerClient);

            RandomDataGenerator randomGenerator = Random.Shared.NextBytes;

            DefaultFsServerObjects fsServerObjects = DefaultFsServerObjects.GetDefaultEmulatedCreators(serverBaseFs, KeySet, fsServer, randomGenerator);

            // Use our own encrypted fs creator that doesn't actually do any encryption
            fsServerObjects.FsCreators.EncryptedFileSystemCreator = new EncryptedFileSystemCreator();

            GameCard = fsServerObjects.GameCard;
            SdCard = fsServerObjects.Sdmmc;

            SdCard.SetSdCardInserted(true);

            FileSystemServerConfig fsServerConfig = new()
            {
                ExternalKeySet = KeySet.ExternalKeySet,
                FsCreators = fsServerObjects.FsCreators,
                StorageDeviceManagerFactory = fsServerObjects.StorageDeviceManagerFactory,
                RandomGenerator = randomGenerator,
            };

            FileSystemServerInitializer.InitializeWithConfig(fsServerClient, fsServer, fsServerConfig);
        }

        public void ReloadKeySet()
        {
            KeySet ??= KeySet.CreateDefaultKeySet();

            string prodKeyFile = null;
            string titleKeyFile = null;
            string consoleKeyFile = null;
            string devKeyFile = null;

            if (AppDataManager.Mode == AppDataManager.LaunchMode.UserProfile)
            {
                LoadSetAtPath(AppDataManager.KeysDirPathUser);
            }

            LoadSetAtPath(AppDataManager.KeysDirPath);

            void LoadSetAtPath(string basePath)
            {
                string localProdKeyFile = Path.Combine(basePath, "prod.keys");
                string localTitleKeyFile = Path.Combine(basePath, "title.keys");
                string localConsoleKeyFile = Path.Combine(basePath, "console.keys");
                string localDevKeyFile = Path.Combine(basePath, "dev.keys");

                if (File.Exists(localProdKeyFile))
                {
                    prodKeyFile = localProdKeyFile;
                }

                if (File.Exists(localTitleKeyFile))
                {
                    titleKeyFile = localTitleKeyFile;
                }

                if (File.Exists(localConsoleKeyFile))
                {
                    consoleKeyFile = localConsoleKeyFile;
                }

                if (File.Exists(localDevKeyFile))
                {
                    devKeyFile = localDevKeyFile;
                }
            }

            ExternalKeyReader.ReadKeyFile(KeySet, prodKeyFile, devKeyFile, titleKeyFile, consoleKeyFile, null);
        }

        public void ImportTickets(IFileSystem fs)
        {
            foreach (DirectoryEntryEx ticketEntry in fs.EnumerateEntries("/", "*.tik"))
            {
                using UniqueRef<IFile> ticketFile = new();

                Result result = fs.OpenFile(ref ticketFile.Ref, ticketEntry.FullPath.ToU8Span(), OpenMode.Read);

                if (result.IsSuccess())
                {
                    // When reading a file from a Sha256PartitionFileSystem, you can't start a read in the middle
                    // of the hashed portion (usually the first 0x200 bytes) of the file and end the read after
                    // the end of the hashed portion, so we read the ticket file using a single read.
                    byte[] ticketData = new byte[0x2C0];
                    result = ticketFile.Get.Read(out long bytesRead, 0, ticketData);

                    if (result.IsFailure() || bytesRead != ticketData.Length)
                        continue;

                    Ticket ticket = new(new MemoryStream(ticketData));
                    byte[] titleKey = ticket.GetTitleKey(KeySet);

                    if (titleKey != null)
                    {
                        KeySet.ExternalKeySet.Add(new RightsId(ticket.RightsId), new AccessKey(titleKey));
                    }
                }
            }
        }

        // Save data created before we supported extra data in directory save data will not work properly if
        // given empty extra data. Luckily some of that extra data can be created using the data from the
        // save data indexer, which should be enough to check access permissions for user saves.
        // Every single save data's extra data will be checked and fixed if needed each time the emulator is opened.
        // Consider removing this at some point in the future when we don't need to worry about old saves.
        public static Result FixExtraData(HorizonClient hos)
        {
            Result rc = GetSystemSaveList(hos, out List<ulong> systemSaveIds);
            if (rc.IsFailure())
            {
                return rc;
            }

            rc = FixUnindexedSystemSaves(hos, systemSaveIds);
            if (rc.IsFailure())
            {
                return rc;
            }

            rc = FixExtraDataInSpaceId(hos, SaveDataSpaceId.System);
            if (rc.IsFailure())
            {
                return rc;
            }

            rc = FixExtraDataInSpaceId(hos, SaveDataSpaceId.User);
            if (rc.IsFailure())
            {
                return rc;
            }

            return Result.Success;
        }

        private static Result FixExtraDataInSpaceId(HorizonClient hos, SaveDataSpaceId spaceId)
        {
            Span<SaveDataInfo> info = stackalloc SaveDataInfo[8];

            using UniqueRef<SaveDataIterator> iterator = new();

            Result rc = hos.Fs.OpenSaveDataIterator(ref iterator.Ref, spaceId);
            if (rc.IsFailure())
            {
                return rc;
            }

            while (true)
            {
                rc = iterator.Get.ReadSaveDataInfo(out long count, info);
                if (rc.IsFailure())
                {
                    return rc;
                }

                if (count == 0)
                {
                    return Result.Success;
                }

                for (int i = 0; i < count; i++)
                {
                    rc = FixExtraData(out bool wasFixNeeded, hos, in info[i]);

                    if (ResultFs.TargetNotFound.Includes(rc))
                    {
                        // If the save wasn't found, try to create the directory for its save data ID
                        rc = CreateSaveDataDirectory(hos, in info[i]);

                        if (rc.IsFailure())
                        {
                            Logger.Warning?.Print(LogClass.Application, $"Error {rc.ToStringWithName()} when creating save data 0x{info[i].SaveDataId:x} in the {spaceId} save data space");

                            // Don't bother fixing the extra data if we couldn't create the directory
                            continue;
                        }

                        Logger.Info?.Print(LogClass.Application, $"Recreated directory for save data 0x{info[i].SaveDataId:x} in the {spaceId} save data space");

                        // Try to fix the extra data in the new directory
                        rc = FixExtraData(out wasFixNeeded, hos, in info[i]);
                    }

                    if (rc.IsFailure())
                    {
                        Logger.Warning?.Print(LogClass.Application, $"Error {rc.ToStringWithName()} when fixing extra data for save data 0x{info[i].SaveDataId:x} in the {spaceId} save data space");
                    }
                    else if (wasFixNeeded)
                    {
                        Logger.Info?.Print(LogClass.Application, $"Fixed extra data for save data 0x{info[i].SaveDataId:x} in the {spaceId} save data space");
                    }
                }
            }
        }

        private static Result CreateSaveDataDirectory(HorizonClient hos, in SaveDataInfo info)
        {
            if (info.SpaceId != SaveDataSpaceId.User && info.SpaceId != SaveDataSpaceId.System)
            {
                return Result.Success;
            }

            const string MountName = "SaveDir";
            U8Span mountNameU8 = MountName.ToU8Span();

            BisPartitionId partitionId = info.SpaceId switch
            {
                SaveDataSpaceId.System => BisPartitionId.System,
                SaveDataSpaceId.User => BisPartitionId.User,
                _ => throw new ArgumentOutOfRangeException(nameof(info), info.SpaceId, null),
            };

            Result rc = hos.Fs.MountBis(mountNameU8, partitionId);
            if (rc.IsFailure())
            {
                return rc;
            }

            try
            {
                U8Span path = $"{MountName}:/save/{info.SaveDataId:x16}".ToU8Span();

                rc = hos.Fs.GetEntryType(out _, path);

                if (ResultFs.PathNotFound.Includes(rc))
                {
                    rc = hos.Fs.CreateDirectory(path);
                }

                return rc;
            }
            finally
            {
                hos.Fs.Unmount(mountNameU8);
            }
        }

        // Gets a list of all the save data files or directories in the system partition.
        private static Result GetSystemSaveList(HorizonClient hos, out List<ulong> list)
        {
            list = null;

            U8Span mountName = "system".ToU8Span();
            DirectoryHandle handle = default;
            List<ulong> localList = [];

            try
            {
                Result rc = hos.Fs.MountBis(mountName, BisPartitionId.System);
                if (rc.IsFailure())
                {
                    return rc;
                }

                rc = hos.Fs.OpenDirectory(out handle, "system:/save".ToU8Span(), OpenDirectoryMode.All);
                if (rc.IsFailure())
                {
                    return rc;
                }

                DirectoryEntry entry = new();

                while (true)
                {
                    rc = hos.Fs.ReadDirectory(out long readCount, SpanHelpers.AsSpan(ref entry), handle);
                    if (rc.IsFailure())
                    {
                        return rc;
                    }

                    if (readCount == 0)
                    {
                        break;
                    }

                    if (Utf8Parser.TryParse(entry.Name, out ulong saveDataId, out int bytesRead, 'x') && bytesRead == 16 && (long)saveDataId < 0)
                    {
                        localList.Add(saveDataId);
                    }
                }

                list = localList;

                return Result.Success;
            }
            finally
            {
                if (handle.IsValid)
                {
                    hos.Fs.CloseDirectory(handle);
                }

                if (hos.Fs.IsMounted(mountName))
                {
                    hos.Fs.Unmount(mountName);
                }
            }
        }

        // Adds system save data that isn't in the save data indexer to the indexer and creates extra data for it.
        // Only save data IDs added to SystemExtraDataFixInfo will be fixed.
        private static Result FixUnindexedSystemSaves(HorizonClient hos, List<ulong> existingSaveIds)
        {
            foreach (ExtraDataFixInfo fixInfo in _systemExtraDataFixInfo)
            {
                if (!existingSaveIds.Contains(fixInfo.StaticSaveDataId))
                {
                    continue;
                }

                Result rc = FixSystemExtraData(out bool wasFixNeeded, hos, in fixInfo);

                if (rc.IsFailure())
                {
                    Logger.Warning?.Print(LogClass.Application,
                        $"Error {rc.ToStringWithName()} when fixing extra data for system save data 0x{fixInfo.StaticSaveDataId:x}");
                }
                else if (wasFixNeeded)
                {
                    Logger.Info?.Print(LogClass.Application,
                        $"Tried to rebuild extra data for system save data 0x{fixInfo.StaticSaveDataId:x}");
                }
            }

            return Result.Success;
        }

        private static Result FixSystemExtraData(out bool wasFixNeeded, HorizonClient hos, in ExtraDataFixInfo info)
        {
            wasFixNeeded = true;

            Result rc = hos.Fs.Impl.ReadSaveDataFileSystemExtraData(out SaveDataExtraData extraData, info.StaticSaveDataId);
            if (!rc.IsSuccess())
            {
                if (!ResultFs.TargetNotFound.Includes(rc))
                {
                    return rc;
                }

                // We'll reach this point only if the save data directory exists but it's not in the save data indexer.
                // Creating the save will add it to the indexer while leaving its existing contents intact.
                return hos.Fs.CreateSystemSaveData(info.StaticSaveDataId, UserId.InvalidId, info.OwnerId, info.DataSize,
                    info.JournalSize, info.Flags);
            }

            if (extraData.Attribute.StaticSaveDataId != 0 && extraData.OwnerId != 0)
            {
                wasFixNeeded = false;
                return Result.Success;
            }

            extraData = new SaveDataExtraData
            {
                Attribute = { StaticSaveDataId = info.StaticSaveDataId },
                OwnerId = info.OwnerId,
                Flags = info.Flags,
                DataSize = info.DataSize,
                JournalSize = info.JournalSize,
            };

            // Make a mask for writing the entire extra data
            Unsafe.SkipInit(out SaveDataExtraData extraDataMask);
            SpanHelpers.AsByteSpan(ref extraDataMask).Fill(0xFF);

            return hos.Fs.Impl.WriteSaveDataFileSystemExtraData(SaveDataSpaceId.System, info.StaticSaveDataId,
                in extraData, in extraDataMask);
        }

        private static Result FixExtraData(out bool wasFixNeeded, HorizonClient hos, in SaveDataInfo info)
        {
            wasFixNeeded = true;

            Result rc = hos.Fs.Impl.ReadSaveDataFileSystemExtraData(out SaveDataExtraData extraData, info.SpaceId, info.SaveDataId);
            if (rc.IsFailure())
            {
                return rc;
            }

            // The extra data should have program ID or static save data ID set if it's valid.
            // We only try to fix the extra data if the info from the save data indexer has a program ID or static save data ID.
            bool canFixByProgramId = extraData.Attribute.ProgramId == ProgramId.InvalidId &&
                                       info.ProgramId != ProgramId.InvalidId;

            bool canFixBySaveDataId = extraData.Attribute.StaticSaveDataId == 0 && info.StaticSaveDataId != 0;

            bool hasEmptyOwnerId = extraData.OwnerId == 0 && info.Type != SaveDataType.System;

            if (!canFixByProgramId && !canFixBySaveDataId && !hasEmptyOwnerId)
            {
                wasFixNeeded = false;
                return Result.Success;
            }

            // The save data attribute struct can be completely created from the save data info.
            extraData.Attribute.ProgramId = info.ProgramId;
            extraData.Attribute.UserId = info.UserId;
            extraData.Attribute.StaticSaveDataId = info.StaticSaveDataId;
            extraData.Attribute.Type = info.Type;
            extraData.Attribute.Rank = info.Rank;
            extraData.Attribute.Index = info.Index;

            // The rest of the extra data can't be created from the save data info.
            // On user saves the owner ID will almost certainly be the same as the program ID.
            if (info.Type != SaveDataType.System)
            {
                extraData.OwnerId = info.ProgramId.Value;
            }
            else
            {
                // Try to match the system save with one of the known saves
                foreach (ExtraDataFixInfo fixInfo in _systemExtraDataFixInfo)
                {
                    if (extraData.Attribute.StaticSaveDataId == fixInfo.StaticSaveDataId)
                    {
                        extraData.OwnerId = fixInfo.OwnerId;
                        extraData.Flags = fixInfo.Flags;
                        extraData.DataSize = fixInfo.DataSize;
                        extraData.JournalSize = fixInfo.JournalSize;

                        break;
                    }
                }
            }

            // Make a mask for writing the entire extra data
            Unsafe.SkipInit(out SaveDataExtraData extraDataMask);
            SpanHelpers.AsByteSpan(ref extraDataMask).Fill(0xFF);

            return hos.Fs.Impl.WriteSaveDataFileSystemExtraData(info.SpaceId, info.SaveDataId, in extraData, in extraDataMask);
        }

        struct ExtraDataFixInfo
        {
            public ulong StaticSaveDataId;
            public ulong OwnerId;
            public SaveDataFlags Flags;
            public long DataSize;
            public long JournalSize;
        }

        private static readonly ExtraDataFixInfo[] _systemExtraDataFixInfo =
        [
            new()
            {
                StaticSaveDataId = 0x8000000000000030,
                OwnerId = 0x010000000000001F,
                Flags = SaveDataFlags.KeepAfterResettingSystemSaveDataWithoutUserSaveData,
                DataSize = 0x10000,
                JournalSize = 0x10000,
            },
            new()
            {
                StaticSaveDataId = 0x8000000000001040,
                OwnerId = 0x0100000000001009,
                Flags = SaveDataFlags.None,
                DataSize = 0xC000,
                JournalSize = 0xC000,
            }
        ];

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                foreach (Stream stream in _romFsByPid.Values)
                {
                    stream.Close();
                }

                _romFsByPid.Clear();
            }
        }
    }
}
