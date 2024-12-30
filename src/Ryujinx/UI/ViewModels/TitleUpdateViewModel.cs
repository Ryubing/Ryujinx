using Avalonia.Collections;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using FluentAvalonia.UI.Controls;
using Ryujinx.Ava.Common.Locale;
using Ryujinx.Ava.Common.Models;
using Ryujinx.Ava.UI.Helpers;
using Ryujinx.Ava.Utilities.AppLibrary;
using Ryujinx.HLE.FileSystem;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Application = Avalonia.Application;

namespace Ryujinx.Ava.UI.ViewModels
{
    public record TitleUpdateViewNoUpdateSentinal();

    public class TitleUpdateViewModel : BaseModel
    {
        private ApplicationLibrary ApplicationLibrary { get; }
        private ApplicationData ApplicationData { get; }

        private AvaloniaList<TitleUpdateModel> _titleUpdates = new();
        private AvaloniaList<object> _views = new();
        private object _selectedUpdate = new TitleUpdateViewNoUpdateSentinal();
        private bool _showBundledContentNotice = false;

        public AvaloniaList<TitleUpdateModel> TitleUpdates
        {
            get => _titleUpdates;
            set
            {
                _titleUpdates = value;
                OnPropertyChanged();
            }
        }

        public AvaloniaList<object> Views
        {
            get => _views;
            set
            {
                _views = value;
                OnPropertyChanged();
            }
        }

        public object SelectedUpdate
        {
            get => _selectedUpdate;
            set
            {
                _selectedUpdate = value;
                OnPropertyChanged();
            }
        }

        public bool ShowBundledContentNotice
        {
            get => _showBundledContentNotice;
            set
            {
                _showBundledContentNotice = value;
                OnPropertyChanged();
            }
        }

        public IStorageProvider StorageProvider;

        public TitleUpdateViewModel(ApplicationLibrary applicationLibrary, ApplicationData applicationData)
        {
            ApplicationLibrary = applicationLibrary;

            ApplicationData = applicationData;

            if (Application.Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                StorageProvider = desktop.MainWindow.StorageProvider;
            }

            LoadUpdates();
        }

        private void LoadUpdates()
        {
            var updates = ApplicationLibrary.TitleUpdates.Items
                .Where(it => it.TitleUpdate.TitleIdBase == ApplicationData.IdBase);

            bool hasBundledContent = false;
            SelectedUpdate = new TitleUpdateViewNoUpdateSentinal();
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
            var sortedUpdates = TitleUpdates.OrderByDescending(update => update.Version);

            // NOTE(jpr): this works around a bug where calling Views.Clear also clears SelectedUpdate for
            // some reason. so we save the item here and restore it after
            var selected = SelectedUpdate;

            Views.Clear();
            Views.Add(new TitleUpdateViewNoUpdateSentinal());
            Views.AddRange(sortedUpdates);

            SelectedUpdate = selected;

            if (SelectedUpdate is TitleUpdateViewNoUpdateSentinal)
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

            if (!ApplicationLibrary.TryGetTitleUpdatesFromFile(path, out var updates))
            {
                return false;
            }

            var updatesForThisGame = updates.Where(it => it.TitleIdBase == ApplicationData.Id).ToList();
            if (updatesForThisGame.Count == 0)
            {
                return false;
            }

            foreach (var update in updatesForThisGame)
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
                SelectedUpdate = new TitleUpdateViewNoUpdateSentinal();
            }

            SortUpdates();
        }

        public async Task Add()
        {
            var result = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                AllowMultiple = true,
                FileTypeFilter = new List<FilePickerFileType>
                {
                    new(LocaleManager.Instance[LocaleKeys.AllSupportedFormats])
                    {
                        Patterns = new[] { "*.nsp" },
                        AppleUniformTypeIdentifiers = new[] { "com.ryujinx.nsp" },
                        MimeTypes = new[] { "application/x-nx-nsp" },
                    },
                },
            });

            var totalUpdatesAdded = 0;
            foreach (var file in result)
            {
                if (!AddUpdate(file.Path.LocalPath, out var newUpdatesAdded))
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
            var updates = TitleUpdates.Select(it => (it, it == SelectedUpdate as TitleUpdateModel)).ToList();
            ApplicationLibrary.SaveTitleUpdatesForGame(ApplicationData, updates);
        }

        private Task ShowNewUpdatesAddedDialog(int numAdded)
        {
            var msg = string.Format(LocaleManager.Instance[LocaleKeys.UpdateWindowUpdateAddedMessage], numAdded);
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
