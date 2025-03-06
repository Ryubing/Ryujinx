using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Gommon;
using Ryujinx.Ava.UI.Controls;
using Ryujinx.Ava.UI.Helpers;
using Ryujinx.Ava.UI.ViewModels;
using Ryujinx.Ava.Utilities;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Ryujinx.Ava.UI.Views.Settings
{
    public partial class SettingsUiView : RyujinxControl<SettingsViewModel>
    {
        public SettingsUiView()
        {
            InitializeComponent();
            AddGameDirButton.Command =
                Commands.Create(() => AddDirButton(GameDirPathBox, ViewModel.GameDirectories));
            AddAutoloadDirButton.Command =
                Commands.Create(() => AddDirButton(AutoloadDirPathBox, ViewModel.AutoloadDirectories));
        }

        private async Task AddDirButton(TextBox addDirBox, AvaloniaList<string> directories)
        {
            string path = addDirBox.Text;

            if (!string.IsNullOrWhiteSpace(path) && Directory.Exists(path) && !directories.Contains(path))
            {
                directories.Add(path);
                
                addDirBox.Clear();

                ViewModel.GameListNeedsRefresh = true;
            }
            else
            {
                Optional<IStorageFolder> folder = await RyujinxApp.MainWindow.ViewModel.StorageProvider.OpenSingleFolderPickerAsync();

                if (folder.HasValue)
                {
                    directories.Add(folder.Value.Path.LocalPath);
                        
                    ViewModel.GameListNeedsRefresh = true;
                }
            }
        }

        private void RemoveGameDirButton_OnClick(object sender, RoutedEventArgs e)
        {
            int oldIndex = GameDirsList.SelectedIndex;

            foreach (string path in new List<string>(GameDirsList.SelectedItems.Cast<string>()))
            {
                ViewModel.GameDirectories.Remove(path);
                ViewModel.GameListNeedsRefresh = true;
            }

            if (GameDirsList.ItemCount > 0)
            {
                GameDirsList.SelectedIndex = oldIndex < GameDirsList.ItemCount ? oldIndex : 0;
            }
        }

        private void RemoveAutoloadDirButton_OnClick(object sender, RoutedEventArgs e)
        {
            int oldIndex = AutoloadDirsList.SelectedIndex;

            foreach (string path in new List<string>(AutoloadDirsList.SelectedItems.Cast<string>()))
            {
                ViewModel.AutoloadDirectories.Remove(path);
                ViewModel.GameListNeedsRefresh = true;
            }

            if (AutoloadDirsList.ItemCount > 0)
            {
                AutoloadDirsList.SelectedIndex = oldIndex < AutoloadDirsList.ItemCount ? oldIndex : 0;
            }
        }
    }
}
