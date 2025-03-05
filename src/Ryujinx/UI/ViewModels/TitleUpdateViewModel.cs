using Avalonia.Collections;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using FluentAvalonia.UI.Controls;
using Ryujinx.Ava.Common.Locale;
using Ryujinx.Ava.Common.Models;
using Ryujinx.Ava.UI.Helpers;
using Ryujinx.Ava.Systems.AppLibrary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Ryujinx.Ava.UI.ViewModels
{
    public record TitleUpdateViewModelNoUpdate;

    public partial class TitleUpdateViewModel : BaseModel
    {
        private ApplicationLibrary ApplicationLibrary { get; }
        private ApplicationData ApplicationData { get; }

        [ObservableProperty] private AvaloniaList<TitleUpdateModel> _titleUpdates = [];
        [ObservableProperty] private AvaloniaList<object> _views = [];
        [ObservableProperty] private object _selectedUpdate = new TitleUpdateViewModelNoUpdate();
        [ObservableProperty] private bool _showBundledContentNotice;

        private readonly IStorageProvider _storageProvider;

        public TitleUpdateViewModel(ApplicationLibrary applicationLibrary, ApplicationData applicationData)
        {
            ApplicationLibrary = applicationLibrary;

            ApplicationData = applicationData;

            _storageProvider = RyujinxApp.MainWindow.StorageProvider;

            LoadUpdates();
        }

        private void LoadUpdates()
        {
            (TitleUpdateModel TitleUpdate, bool IsSelected)[] updates = ApplicationLibrary.FindUpdateConfigurationFor(ApplicationData.Id);

            bool hasBundledContent = false;
            SelectedUpdate = new TitleUpdateViewModelNoUpdate();
            foreach ((TitleUpdateModel update, bool isSelected) in updates)
            {
                TitleUpdates.Add(update);
                hasBundledContent = hasBundledContent || update.IsBundled;

                if (isSelected)
                {
                    SelectedUpdate = update;
                }
            }

            ShowBundledContentNotice = hasBundledContent;

            SortUpdates();
        }

        public void SortUpdates()
        {
            IOrderedEnumerable<TitleUpdateModel> sortedUpdates = TitleUpdates.OrderByDescending(update => update.Version);

            // NOTE(jpr): this works around a bug where calling Views.Clear also clears SelectedUpdate for
            // some reason. so we save the item here and restore it after
            object selected = SelectedUpdate;

            Views.Clear();
            Views.Add(new TitleUpdateViewModelNoUpdate());
            Views.AddRange(sortedUpdates);

            SelectedUpdate = selected;

            if (SelectedUpdate is TitleUpdateViewModelNoUpdate)
            {
                SelectedUpdate = Views[0];
            }
            // this is mainly to handle a scenario where the user removes the selected update
            else if (!TitleUpdates.Contains((TitleUpdateModel)SelectedUpdate))
            {
                SelectedUpdate = Views.Count > 1 ? Views[1] : Views[0];
            }
        }

        private bool AddUpdate(string path, out int numUpdatesAdded)
        {
            numUpdatesAdded = 0;

            if (!File.Exists(path))
            {
                return false;
            }

            if (!ApplicationLibrary.TryGetTitleUpdatesFromFile(path, out List<TitleUpdateModel> updates))
            {
                return false;
            }

            List<TitleUpdateModel> updatesForThisGame = updates.Where(it => it.TitleIdBase == ApplicationData.Id).ToList();
            if (updatesForThisGame.Count == 0)
            {
                return false;
            }

            foreach (TitleUpdateModel update in updatesForThisGame)
            {
                if (!TitleUpdates.Contains(update))
                {
                    TitleUpdates.Add(update);
                    SelectedUpdate = update;

                    numUpdatesAdded++;
                }
            }

            if (numUpdatesAdded > 0)
            {
                SortUpdates();
            }

            return true;
        }

        public void RemoveUpdate(TitleUpdateModel update)
        {
            if (!update.IsBundled)
            {
                TitleUpdates.Remove(update);
            }
            else if (update == SelectedUpdate as TitleUpdateModel)
            {
                SelectedUpdate = new TitleUpdateViewModelNoUpdate();
            }

            SortUpdates();
        }

        public async Task Add()
        {
            IReadOnlyList<IStorageFile> result = await _storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                AllowMultiple = true,
                FileTypeFilter = new List<FilePickerFileType>
                {
                    new(LocaleManager.Instance[LocaleKeys.AllSupportedFormats])
                    {
                        Patterns = ["*.nsp"],
                        AppleUniformTypeIdentifiers = ["com.ryujinx.nsp"],
                        MimeTypes = ["application/x-nx-nsp"],
                    },
                },
            });

            int totalUpdatesAdded = 0;
            foreach (IStorageFile file in result)
            {
                if (!AddUpdate(file.Path.LocalPath, out int newUpdatesAdded))
                {
                    await ContentDialogHelper.CreateErrorDialog(LocaleManager.Instance[LocaleKeys.DialogUpdateAddUpdateErrorMessage]);
                }

                totalUpdatesAdded += newUpdatesAdded;
            }

            if (totalUpdatesAdded > 0)
            {
                await ShowNewUpdatesAddedDialog(totalUpdatesAdded);
            }
        }

        public void Save()
        {
            List<(TitleUpdateModel it, bool)> updates = TitleUpdates.Select(it => (it, it == SelectedUpdate as TitleUpdateModel)).ToList();
            ApplicationLibrary.SaveTitleUpdatesForGame(ApplicationData, updates);
        }

        private Task ShowNewUpdatesAddedDialog(int numAdded)
        {
            string msg = string.Format(LocaleManager.Instance[LocaleKeys.UpdateWindowUpdateAddedMessage], numAdded);
            return Dispatcher.UIThread.InvokeAsync(async () => 
                await ContentDialogHelper.ShowTextDialog(
                    LocaleManager.Instance[LocaleKeys.DialogConfirmationTitle], 
                    msg, 
                    string.Empty, 
                    string.Empty, 
                    string.Empty, 
                    LocaleManager.Instance[LocaleKeys.InputDialogOk], 
                    (int)Symbol.Checkmark
                ));
        }
    }
}
