using Avalonia.Collections;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using DynamicData;
using Gommon;
using Ryujinx.Ava.Common.Locale;
using Ryujinx.Ava.UI.Helpers;
using Ryujinx.Ava.UI.Models;
using Ryujinx.Ava.Systems.AppLibrary;
using Ryujinx.Common.Configuration;
using Ryujinx.Common.Logging;
using Ryujinx.Common.Utilities;
using Ryujinx.HLE.HOS;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace Ryujinx.Ava.UI.ViewModels
{
    public partial class ModManagerViewModel : BaseModel
    {
        private readonly string _modJsonPath;

        private AvaloniaList<ModModel> _mods = [];
        [ObservableProperty] private AvaloniaList<ModModel> _views = [];
        [ObservableProperty] private AvaloniaList<ModModel> _selectedMods = [];

        private string _search;
        private readonly ulong _applicationId;
        private readonly ulong[] _installedDlcIds;
        private readonly IStorageProvider _storageProvider;

        private static readonly ModMetadataJsonSerializerContext _serializerContext = new(JsonHelper.GetDefaultSerializerOptions());

        public AvaloniaList<ModModel> Mods
        {
            get => _mods;
            set
            {
                _mods = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ModCount));
                Sort();
            }
        }

        public string Search
        {
            get => _search;
            set
            {
                _search = value;
                OnPropertyChanged();
                Sort();
            }
        }

        public string ModCount
        {
            get => string.Format(LocaleManager.Instance[LocaleKeys.ModWindowHeading], Mods.Count);
        }

        public ModManagerViewModel(ulong applicationId, ulong applicationIdBase, ApplicationLibrary appLibrary)
        {
            _applicationId = applicationId;

            _installedDlcIds = appLibrary.DownloadableContents.Keys
                .Where(x => x.TitleIdBase == applicationIdBase)
                .Select(x => x.TitleId)
                .ToArray();

            _modJsonPath = Path.Combine(AppDataManager.GamesDirPath, applicationId.ToString("x16"), "mods.json");

            _storageProvider = RyujinxApp.MainWindow.StorageProvider;

            LoadMods(applicationId, _installedDlcIds);
        }

        private void LoadMods(ulong applicationId, ulong[] installedDlcIds)
        {
            Mods.Clear();
            SelectedMods.Clear();

            string[] modsBasePaths = [ModLoader.GetSdModsBasePath(), ModLoader.GetModsBasePath()];

            foreach (string path in modsBasePaths)
            {
                bool inSd = path == ModLoader.GetSdModsBasePath();
                ModLoader.ModCache modCache = new();

                ModLoader.QueryContentsDir(modCache, new DirectoryInfo(Path.Combine(path, "contents")), applicationId, _installedDlcIds);

                foreach (ModLoader.Mod<DirectoryInfo> mod in modCache.RomfsDirs)
                {
                    ModModel modModel = new(mod.Path.Parent.FullName, mod.Name, mod.Enabled, inSd);
                    if (Mods.All(x => x.Path != mod.Path.Parent.FullName))
                    {
                        Mods.Add(modModel);
                    }
                }

                foreach (ModLoader.Mod<FileInfo> mod in modCache.RomfsContainers)
                {
                    Mods.Add(new ModModel(mod.Path.FullName, mod.Name, mod.Enabled, inSd));
                }

                foreach (ModLoader.Mod<DirectoryInfo> mod in modCache.ExefsDirs)
                {
                    ModModel modModel = new(mod.Path.Parent.FullName, mod.Name, mod.Enabled, inSd);
                    if (Mods.All(x => x.Path != mod.Path.Parent.FullName))
                    {
                        Mods.Add(modModel);
                    }
                }

                foreach (ModLoader.Mod<FileInfo> mod in modCache.ExefsContainers)
                {
                    Mods.Add(new ModModel(mod.Path.FullName, mod.Name, mod.Enabled, inSd));
                }
            }

            Sort();
        }

        public void Sort()
        {
            Mods.AsObservableChangeSet()
                .Filter(Filter)
                .Bind(out ReadOnlyObservableCollection<ModModel> view).AsObservableList();

#pragma warning disable MVVMTK0034 // Event to update is fired below
            _views.Clear();
            _views.AddRange(view);
#pragma warning restore MVVMTK0034

            SelectedMods = new(Views.Where(x => x.Enabled));

            OnPropertyChanged(nameof(ModCount));
            OnPropertyChanged(nameof(Views));
            OnPropertyChanged(nameof(SelectedMods));
        }

        private bool Filter(object arg)
        {
            if (arg is ModModel content)
            {
                return string.IsNullOrWhiteSpace(_search) || content.Name.ToLower().Contains(_search.ToLower());
            }

            return false;
        }

        public void Save()
        {
            ModMetadata modData = new();

            foreach (ModModel mod in Mods)
            {
                modData.Mods.Add(new Mod
                {
                    Name = mod.Name,
                    Path = mod.Path,
                    Enabled = SelectedMods.Contains(mod),
                });
            }

            JsonHelper.SerializeToFile(_modJsonPath, modData, _serializerContext.ModMetadata);
        }

        public void Delete(ModModel model, bool removeFromList = true)
        {
            bool isSubdir = true;
            string pathToDelete = model.Path;
            string basePath = model.InSd ? ModLoader.GetSdModsBasePath() : ModLoader.GetModsBasePath();
            string modsDir = ModLoader.GetApplicationDir(basePath, _applicationId.ToString("x16"));

            if (new DirectoryInfo(model.Path).Parent?.FullName == modsDir)
            {
                isSubdir = false;
            }

            if (isSubdir)
            {
                string parentDir = String.Empty;

                foreach (string dir in Directory.GetDirectories(modsDir, "*", SearchOption.TopDirectoryOnly))
                {
                    if (Directory.GetDirectories(dir, "*", SearchOption.AllDirectories).Contains(model.Path))
                    {
                        parentDir = dir;
                        break;
                    }
                }

                if (parentDir == String.Empty)
                {
                    Dispatcher.UIThread.Post(async () =>
                    {
                        await ContentDialogHelper.CreateErrorDialog(LocaleManager.Instance.UpdateAndGetDynamicValue(
                            LocaleKeys.DialogModDeleteNoParentMessage,
                            model.Path));
                    });
                    return;
                }
            }

            Logger.Info?.Print(LogClass.Application, $"Deleting mod at \"{pathToDelete}\"");
            Directory.Delete(pathToDelete, true);

            if (removeFromList)
            {
                Mods.Remove(model);
                OnPropertyChanged(nameof(ModCount));
            }
            Sort();
        }

        private void AddMod(DirectoryInfo directory)
        {
            string[] directories;

            try
            {
                directories = Directory.GetDirectories(directory.ToString(), "*", SearchOption.AllDirectories);
            }
            catch (Exception exception)
            {
                Dispatcher.UIThread.Post(async () =>
                {
                    await ContentDialogHelper.CreateErrorDialog(LocaleManager.Instance.UpdateAndGetDynamicValue(
                        LocaleKeys.DialogLoadFileErrorMessage,
                        exception.ToString(),
                        directory));
                });
                return;
            }

            string destinationDir = ModLoader.GetApplicationDir(ModLoader.GetSdModsBasePath(), _applicationId.ToString("x16"));

            // TODO: More robust checking for valid mod folders
            bool isDirectoryValid = true;

            if (directories.Length == 0)
            {
                isDirectoryValid = false;
            }

            if (!isDirectoryValid)
            {
                Dispatcher.UIThread.Post(async () =>
                {
                    await ContentDialogHelper.CreateErrorDialog(LocaleManager.Instance[LocaleKeys.DialogModInvalidMessage]);
                });
                return;
            }

            foreach (string dir in directories)
            {
                string dirToCreate = dir.Replace(directory.Parent.ToString(), destinationDir);

                // Mod already exists
                if (Directory.Exists(dirToCreate))
                {
                    Dispatcher.UIThread.Post(async () =>
                    {
                        await ContentDialogHelper.CreateErrorDialog(LocaleManager.Instance.UpdateAndGetDynamicValue(
                            LocaleKeys.DialogLoadFileErrorMessage,
                            LocaleManager.Instance[LocaleKeys.DialogModAlreadyExistsMessage],
                            dirToCreate));
                    });

                    return;
                }

                Directory.CreateDirectory(dirToCreate);
            }

            string[] files = Directory.GetFiles(directory.ToString(), "*", SearchOption.AllDirectories);

            foreach (string file in files)
            {
                File.Copy(file, file.Replace(directory.Parent.ToString(), destinationDir), true);
            }

            LoadMods(_applicationId, _installedDlcIds);
        }

        public async void Add()
        {
            IReadOnlyList<IStorageFolder> result = await _storageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = LocaleManager.Instance[LocaleKeys.SelectModDialogTitle],
                AllowMultiple = true,
            });

            foreach (IStorageFolder folder in result)
            {
                AddMod(new DirectoryInfo(folder.Path.LocalPath));
            }
        }

        public void DeleteAll()
        {
            Mods.ForEach(it => Delete(it, false));
            Mods.Clear();
            OnPropertyChanged(nameof(ModCount));
            Sort();
        }

        public void EnableAll()
        {
            SelectedMods = new(Mods);
        }

        public void DisableAll()
        {
            SelectedMods.Clear();
        }
    }
}
