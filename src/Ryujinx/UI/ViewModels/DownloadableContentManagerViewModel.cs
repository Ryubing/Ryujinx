using Avalonia.Collections;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using DynamicData;
using FluentAvalonia.UI.Controls;
using Ryujinx.Ava.Common.Locale;
using Ryujinx.Ava.Common.Models;
using Ryujinx.Ava.UI.Helpers;
using Ryujinx.Ava.Systems.AppLibrary;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Ryujinx.Ava.UI.ViewModels
{
    public partial class DownloadableContentManagerViewModel : BaseModel
    {
        private readonly ApplicationLibrary _applicationLibrary;
        private AvaloniaList<DownloadableContentModel> _downloadableContents = [];
        [ObservableProperty] private AvaloniaList<DownloadableContentModel> _selectedDownloadableContents = [];
        [ObservableProperty] private AvaloniaList<DownloadableContentModel> _views = [];
        [ObservableProperty] private bool _showBundledContentNotice = false;

        private string _search;
        private readonly ApplicationData _applicationData;
        private readonly IStorageProvider _storageProvider;

        public AvaloniaList<DownloadableContentModel> DownloadableContents
        {
            get => _downloadableContents;
            set
            {
                _downloadableContents = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(UpdateCount));
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

        public string UpdateCount
        {
            get => string.Format(LocaleManager.Instance[LocaleKeys.DlcWindowHeading], DownloadableContents.Count);
        }

        public DownloadableContentManagerViewModel(ApplicationLibrary applicationLibrary, ApplicationData applicationData)
        {
            _applicationLibrary = applicationLibrary;

            _applicationData = applicationData;

            _storageProvider = RyujinxApp.MainWindow.StorageProvider;

            LoadDownloadableContents();
        }

        private void LoadDownloadableContents()
        {
            (DownloadableContentModel Dlc, bool IsEnabled)[] dlcs = _applicationLibrary.FindDlcConfigurationFor(_applicationData.Id);

            bool hasBundledContent = false;
            foreach ((DownloadableContentModel dlc, bool isEnabled) in dlcs)
            {
                DownloadableContents.Add(dlc);
                hasBundledContent = hasBundledContent || dlc.IsBundled;

                if (isEnabled)
                {
                    SelectedDownloadableContents.Add(dlc);
                }

                OnPropertyChanged(nameof(UpdateCount));
            }

            ShowBundledContentNotice = hasBundledContent;

            Sort();
        }

        public void Sort()
        {
            DownloadableContents
                // Sort bundled last
                .OrderBy(it => it.IsBundled ? 0 : 1)
                .ThenBy(it => it.TitleId)
                .AsObservableChangeSet()
                .Filter(Filter)
                .Bind(out ReadOnlyObservableCollection<DownloadableContentModel> view).AsObservableList();

            // NOTE(jpr): this works around a bug where calling _views.Clear also clears SelectedDownloadableContents for
            // some reason. so we save the items here and add them back after
            DownloadableContentModel[] items = SelectedDownloadableContents.ToArray();
            
            Views.Clear();
            Views.AddRange(view);

            foreach (DownloadableContentModel item in items)
            {
                SelectedDownloadableContents.ReplaceOrAdd(item, item);
            }

            OnPropertyChanged(nameof(Views));
        }

        private bool Filter<T>(T arg)
        {
            if (arg is DownloadableContentModel content)
            {
                return string.IsNullOrWhiteSpace(_search) || content.FileName.ToLower().Contains(_search.ToLower()) || content.TitleIdStr.ToLower().Contains(_search.ToLower());
            }

            return false;
        }

        public async void Add()
        {
            IReadOnlyList<IStorageFile> result = await _storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = LocaleManager.Instance[LocaleKeys.SelectDlcDialogTitle],
                AllowMultiple = true,
                FileTypeFilter = new List<FilePickerFileType>
                {
                    new("NSP")
                    {
                        Patterns = ["*.nsp"],
                        AppleUniformTypeIdentifiers = ["com.ryujinx.nsp"],
                        MimeTypes = ["application/x-nx-nsp"],
                    },
                },
            });

            int totalDlcAdded = 0;
            foreach (IStorageFile file in result)
            {
                if (!AddDownloadableContent(file.Path.LocalPath, out int newDlcAdded))
                {
                    await ContentDialogHelper.CreateErrorDialog(LocaleManager.Instance[LocaleKeys.DialogDlcNoDlcErrorMessage]);
                }

                totalDlcAdded += newDlcAdded;
            }

            if (totalDlcAdded > 0)
            {
                await ShowNewDlcAddedDialog(totalDlcAdded);
            }
        }

        private bool AddDownloadableContent(string path, out int numDlcAdded)
        {
            numDlcAdded = 0;

            if (!File.Exists(path))
            {
                return false;
            }

            if (!_applicationLibrary.TryGetDownloadableContentFromFile(path, out List<DownloadableContentModel> dlcs) || dlcs.Count == 0)
            {
                return false;
            }

            List<DownloadableContentModel> dlcsForThisGame = dlcs.Where(it => it.TitleIdBase == _applicationData.IdBase).ToList();
            if (dlcsForThisGame.Count == 0)
            {
                return false;
            }

            foreach (DownloadableContentModel dlc in dlcsForThisGame)
            {
                if (!DownloadableContents.Contains(dlc))
                {
                    DownloadableContents.Add(dlc);
                    SelectedDownloadableContents.ReplaceOrAdd(dlc, dlc);

                    numDlcAdded++;
                }
            }

            if (numDlcAdded > 0)
            {
                OnPropertyChanged(nameof(UpdateCount));
                Sort();
            }

            return true;
        }

        public void Remove(DownloadableContentModel model)
        {
            SelectedDownloadableContents.Remove(model);

            if (!model.IsBundled)
            {
                DownloadableContents.Remove(model);
                OnPropertyChanged(nameof(UpdateCount));
                Sort();
            }
        }

        public void RemoveAll()
        {
            SelectedDownloadableContents.Clear();
            DownloadableContents.RemoveMany(DownloadableContents.Where(it => !it.IsBundled));

            OnPropertyChanged(nameof(UpdateCount));
            Sort();
        }

        public void EnableAll()
        {
            SelectedDownloadableContents.Clear();
            SelectedDownloadableContents.AddRange(DownloadableContents);
        }

        public void DisableAll()
        {
            SelectedDownloadableContents.Clear();
        }

        public void Enable(DownloadableContentModel model)
        {
            SelectedDownloadableContents.ReplaceOrAdd(model, model);
        }

        public void Disable(DownloadableContentModel model)
        {
            SelectedDownloadableContents.Remove(model);
        }

        public void Save()
        {
            List<(DownloadableContentModel it, bool)> dlcs = DownloadableContents.Select(it => (it, SelectedDownloadableContents.Contains(it))).ToList();
            _applicationLibrary.SaveDownloadableContentsForGame(_applicationData, dlcs);
        }

        private Task ShowNewDlcAddedDialog(int numAdded)
        {
            string msg = string.Format(LocaleManager.Instance[LocaleKeys.DlcWindowDlcAddedMessage], numAdded);
            return Dispatcher.UIThread.InvokeAsync(async () =>
            {
                await ContentDialogHelper.ShowTextDialog(
                    LocaleManager.Instance[LocaleKeys.DialogConfirmationTitle], 
                    msg, 
                    string.Empty, 
                    string.Empty, 
                    string.Empty, 
                    LocaleManager.Instance[LocaleKeys.InputDialogOk], 
                    (int)Symbol.Checkmark);
            });
        }
    }
}
