using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Gommon;
using Ryujinx.Ava.UI.Helpers;
using Ryujinx.Ava.UI.ViewModels;
using Ryujinx.Ava.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Ryujinx.Ava.UI.Views.Settings
{
    public partial class SettingsUiView : UserControl
    {
        public SettingsViewModel ViewModel;

        public SettingsUiView()
        {
            InitializeComponent();
            AddGameDirButton.Command =
                Commands.Create(() => AddDirButton(GameDirPathBox, ViewModel.GameDirectories, true));
            AddAutoloadDirButton.Command =
                Commands.Create(() => AddDirButton(AutoloadDirPathBox, ViewModel.AutoloadDirectories, false));
        }

        private async Task AddDirButton(TextBox addDirBox, AvaloniaList<string> directories, bool isGameList)
        {
            string path = addDirBox.Text;

            if (!string.IsNullOrWhiteSpace(path) && Directory.Exists(path) && !directories.Contains(path))
            {
                directories.Add(path);
                
                addDirBox.Clear();
                
                if (isGameList)
                    ViewModel.GameDirectoryChanged = true;
                else
                    ViewModel.AutoloadDirectoryChanged = true;
            }
            else
            {
                Optional<IStorageFolder> folder = await RyujinxApp.MainWindow.ViewModel.StorageProvider.OpenSingleFolderPickerAsync();

                if (folder.HasValue)
                {
                    directories.Add(folder.Value.Path.LocalPath);
                        
                    if (isGameList)
                        ViewModel.GameDirectoryChanged = true;
                    else
                        ViewModel.AutoloadDirectoryChanged = true;
                }
            }
        }

        private void RemoveGameDirButton_OnClick(object sender, RoutedEventArgs e)
        {
            int oldIndex = GameDirsList.SelectedIndex;

            foreach (string path in new List<string>(GameDirsList.SelectedItems.Cast<string>()))
            {
                ViewModel.GameDirectories.Remove(path);
                ViewModel.GameDirectoryChanged = true;
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
                ViewModel.AutoloadDirectoryChanged = true;
            }

            if (AutoloadDirsList.ItemCount > 0)
            {
                AutoloadDirsList.SelectedIndex = oldIndex < AutoloadDirsList.ItemCount ? oldIndex : 0;
            }
        }
    }
}
