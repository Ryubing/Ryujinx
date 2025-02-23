using Gommon;
using LibHac.Common;
using LibHac.Fs;
using LibHac.Fs.Fsa;
using LibHac.FsSystem;
using LibHac.Loader;
using LibHac.Ns;
using LibHac.Tools.Fs;
using LibHac.Tools.FsSystem;
using LibHac.Tools.FsSystem.NcaUtils;
using Ryujinx.Ava.Common.Locale;
using Ryujinx.Ava.Utilities.Compat;
using Ryujinx.Ava.Utilities.PlayReport;
using Ryujinx.Common.Logging;
using Ryujinx.HLE.FileSystem;
using Ryujinx.HLE.Loaders.Processes.Extensions;
using System;
using System.IO;
using System.Text.Json.Serialization;

namespace Ryujinx.Ava.Utilities.AppLibrary
{
    public class ApplicationData
    {
        public bool Favorite { get; set; }
        public bool HasIndependentConfiguration { get; set; }
        public byte[] Icon { get; set; }
        public string Name { get; set; } = "Unknown";

        private ulong _id;

        public ulong Id
        {
            get => _id;
            set
            {
                _id = value;

                Compatibility = CompatibilityCsv.Find(value);
                RichPresenceSpec = PlayReports.Analyzer.TryGetSpec(IdString, out GameSpec gameSpec) 
                    ? gameSpec 
                    : default(Optional<GameSpec>);
            }
        }
        public Optional<GameSpec> RichPresenceSpec { get; set; }
        
        public string Developer { get; set; } = "Unknown";
        public string Version { get; set; } = "0";
        public int PlayerCount { get; set; }
        public int GameCount { get; set; }

        public bool HasLdnGames => PlayerCount != 0 && GameCount != 0;

        public bool HasRichPresenceAsset => DiscordIntegrationModule.HasAssetImage(IdString);
        public bool HasDynamicRichPresenceSupport => RichPresenceSpec.HasValue;
        
        public TimeSpan TimePlayed { get; set; }
        public DateTime? LastPlayed { get; set; }
        public string FileExtension { get; set; }
        public long FileSize { get; set; }
        public string Path { get; set; }
        public BlitStruct<ApplicationControlProperty> ControlHolder { get; set; }

        public bool HasControlHolder => ControlHolder.ByteSpan.Length > 0 && !ControlHolder.ByteSpan.IsZeros();

        public string TimePlayedString => ValueFormatUtils.FormatTimeSpan(TimePlayed);

        public bool HasPlayedPreviously => TimePlayed.TotalSeconds > 1;

        public string LastPlayedString => ValueFormatUtils.FormatDateTime(LastPlayed)?.Replace(" ", "\n");

        public string FileSizeString => ValueFormatUtils.FormatFileSize(FileSize);
        
        public Optional<CompatibilityEntry> Compatibility { get; private set; }
        
        public bool HasPlayabilityInfo => Compatibility.HasValue;

        public string LocalizedStatus => Compatibility.Convert(x => x.LocalizedStatus);

        public bool HasCompatibilityLabels => !FormattedCompatibilityLabels.Equals(string.Empty);
        
        public string FormattedCompatibilityLabels
            => Compatibility.Convert(x => x.FormattedIssueLabels).OrElse(string.Empty);
            
        public LocaleKeys? PlayabilityStatus => Compatibility.Convert(x => x.Status).OrElse(null);

        public string LocalizedStatusTooltip =>
            Compatibility.Convert(x => 
#pragma warning disable CS8509 // It is exhaustive for all possible values this can contain.
                LocaleManager.Instance[x.Status switch
#pragma warning restore CS8509
                {
                    LocaleKeys.CompatibilityListPlayable => LocaleKeys.CompatibilityListPlayableTooltip,
                    LocaleKeys.CompatibilityListIngame => LocaleKeys.CompatibilityListIngameTooltip,
                    LocaleKeys.CompatibilityListMenus => LocaleKeys.CompatibilityListMenusTooltip,
                    LocaleKeys.CompatibilityListBoots => LocaleKeys.CompatibilityListBootsTooltip,
                    LocaleKeys.CompatibilityListNothing => LocaleKeys.CompatibilityListNothingTooltip,
                }]
            ).OrElse(string.Empty);
        

        [JsonIgnore] public string IdString => Id.ToString("x16");

        [JsonIgnore] public ulong IdBase => Id & ~0x1FFFUL;

        [JsonIgnore] public string IdBaseString => IdBase.ToString("x16");

        public static string GetBuildId(VirtualFileSystem virtualFileSystem, IntegrityCheckLevel checkLevel, string titleFilePath)
        {
            if (!System.IO.Path.Exists(titleFilePath))
            {
                Logger.Error?.Print(LogClass.Application, $"File \"{titleFilePath}\" does not exist.");
                return string.Empty;
            }
            
            using FileStream file = new(titleFilePath, FileMode.Open, FileAccess.Read);

            Nca mainNca = null;
            Nca patchNca = null;

            string extension = System.IO.Path.GetExtension(titleFilePath).ToLower();

            if (extension is ".nsp" or ".xci")
            {
                IFileSystem pfs;

                if (extension == ".xci")
                {
                    Xci xci = new(virtualFileSystem.KeySet, file.AsStorage());

                    pfs = xci.OpenPartition(XciPartitionType.Secure);
                }
                else
                {
                    PartitionFileSystem pfsTemp = new();
                    pfsTemp.Initialize(file.AsStorage()).ThrowIfFailure();
                    pfs = pfsTemp;
                }

                foreach (DirectoryEntryEx fileEntry in pfs.EnumerateEntries("/", "*.nca"))
                {
                    using UniqueRef<IFile> ncaFile = new();

                    pfs.OpenFile(ref ncaFile.Ref, fileEntry.FullPath.ToU8Span(), OpenMode.Read).ThrowIfFailure();

                    Nca nca = new(virtualFileSystem.KeySet, ncaFile.Get.AsStorage());

                    if (nca.Header.ContentType != NcaContentType.Program)
                    {
                        continue;
                    }

                    int dataIndex = Nca.GetSectionIndexFromType(NcaSectionType.Data, NcaContentType.Program);

                    if (nca.Header.GetFsHeader(dataIndex).IsPatchSection())
                    {
                        patchNca = nca;
                    }
                    else
                    {
                        mainNca = nca;
                    }
                }
            }
            else if (extension == ".nca")
            {
                mainNca = new Nca(virtualFileSystem.KeySet, file.AsStorage());
            }

            if (mainNca == null)
            {
                Logger.Error?.Print(LogClass.Application, "Extraction failure. The main NCA was not present in the selected file");

                return string.Empty;
            }

            (Nca updatePatchNca, _) = mainNca.GetUpdateData(virtualFileSystem, checkLevel, 0, out string _);

            if (updatePatchNca != null)
            {
                patchNca = updatePatchNca;
            }

            IFileSystem codeFs = null;

            if (patchNca == null)
            {
                if (mainNca.CanOpenSection(NcaSectionType.Code))
                {
                    codeFs = mainNca.OpenFileSystem(NcaSectionType.Code, IntegrityCheckLevel.ErrorOnInvalid);
                }
            }
            else
            {
                if (patchNca.CanOpenSection(NcaSectionType.Code))
                {
                    codeFs = mainNca.OpenFileSystemWithPatch(patchNca, NcaSectionType.Code, IntegrityCheckLevel.ErrorOnInvalid);
                }
            }

            if (codeFs == null)
            {
                Logger.Error?.Print(LogClass.Loader, "No ExeFS found in NCA");

                return string.Empty;
            }

            const string MainExeFs = "main";

            if (!codeFs.FileExists($"/{MainExeFs}"))
            {
                Logger.Error?.Print(LogClass.Loader, "No main binary ExeFS found in ExeFS");

                return string.Empty;
            }

            using UniqueRef<IFile> nsoFile = new();

            codeFs.OpenFile(ref nsoFile.Ref, $"/{MainExeFs}".ToU8Span(), OpenMode.Read).ThrowIfFailure();

            NsoReader reader = new();
            reader.Initialize(nsoFile.Release().AsStorage().AsFile(OpenMode.Read)).ThrowIfFailure();

            return Convert.ToHexString(reader.Header.ModuleId.ItemsRo.ToArray()).Replace("-", string.Empty).ToUpper()[..16];
        }
    }
}
