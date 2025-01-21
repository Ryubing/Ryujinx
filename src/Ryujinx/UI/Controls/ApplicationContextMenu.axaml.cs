using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using FluentAvalonia.UI.Controls;
using LibHac;
using LibHac.Fs;
using LibHac.Tools.FsSystem.NcaUtils;
using Ryujinx.Ava.Common;
using Ryujinx.Ava.Common.Locale;
using Ryujinx.Ava.Common.Models;
using Ryujinx.Ava.UI.Helpers;
using Ryujinx.Ava.UI.ViewModels;
using Ryujinx.Ava.UI.Windows;
using Ryujinx.Ava.Utilities;
using Ryujinx.Ava.Utilities.AppLibrary;
using Ryujinx.Common.Configuration;
using Ryujinx.Common.Helper;
using Ryujinx.Common.Logging;
using Ryujinx.HLE.HOS;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Path = System.IO.Path;

namespace Ryujinx.Ava.UI.Controls
{
    public class ApplicationContextMenu : MenuFlyout
    {
        public ApplicationContextMenu()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public void ToggleFavorite_Click(object sender, RoutedEventArgs args)
        {
            if (sender is not MenuItem { DataContext: MainWindowViewModel { SelectedApplication: not null } viewModel })
                return;

            viewModel.SelectedApplication.Favorite = !viewModel.SelectedApplication.Favorite;

            ApplicationLibrary.LoadAndSaveMetaData(viewModel.SelectedApplication.IdString, appMetadata =>
            {
                appMetadata.Favorite = viewModel.SelectedApplication.Favorite;
            });

            viewModel.RefreshView();
        }

        public void OpenUserSaveDirectory_Click(object sender, RoutedEventArgs args)
        {
            if (sender is MenuItem { DataContext: MainWindowViewModel { SelectedApplication: not null } viewModel })
                OpenSaveDirectory(viewModel, SaveDataType.Account, new UserId((ulong)viewModel.AccountManager.LastOpenedUser.UserId.High, (ulong)viewModel.AccountManager.LastOpenedUser.UserId.Low));
        }

        public void OpenDeviceSaveDirectory_Click(object sender, RoutedEventArgs args)
        {
            if (sender is MenuItem { DataContext: MainWindowViewModel { SelectedApplication: not null } viewModel })
                OpenSaveDirectory(viewModel, SaveDataType.Device, default);
        }

        public void OpenBcatSaveDirectory_Click(object sender, RoutedEventArgs args)
        {
            if (sender is MenuItem { DataContext: MainWindowViewModel { SelectedApplication: not null } viewModel })
                OpenSaveDirectory(viewModel, SaveDataType.Bcat, default);
        }

        private static void OpenSaveDirectory(MainWindowViewModel viewModel, SaveDataType saveDataType, UserId userId)
        {
            var saveDataFilter = SaveDataFilter.Make(viewModel.SelectedApplication.Id, saveDataType, userId, saveDataId: default, index: default);

            ApplicationHelper.OpenSaveDir(in saveDataFilter, viewModel.SelectedApplication.Id, viewModel.SelectedApplication.ControlHolder, viewModel.SelectedApplication.Name);
        }

        public async void OpenTitleUpdateManager_Click(object sender, RoutedEventArgs args)
        {
            if (sender is MenuItem { DataContext: MainWindowViewModel { SelectedApplication: not null } viewModel })
                await TitleUpdateWindow.Show(viewModel.ApplicationLibrary, viewModel.SelectedApplication);
        }

        public async void OpenDownloadableContentManager_Click(object sender, RoutedEventArgs args)
        {
            if (sender is MenuItem { DataContext: MainWindowViewModel { SelectedApplication: not null } viewModel })
                await DownloadableContentManagerWindow.Show(viewModel.ApplicationLibrary, viewModel.SelectedApplication);
        }

        public async void OpenCheatManager_Click(object sender, RoutedEventArgs args)
        {
            if (sender is MenuItem { DataContext: MainWindowViewModel { SelectedApplication: not null } viewModel })
                await new CheatWindow(
                    viewModel.VirtualFileSystem,
                    viewModel.SelectedApplication.IdString,
                    viewModel.SelectedApplication.Name,
                    viewModel.SelectedApplication.Path).ShowDialog((Window)viewModel.TopLevel);
        }

        public void OpenModsDirectory_Click(object sender, RoutedEventArgs args)
        {
            if (sender is not MenuItem { DataContext: MainWindowViewModel { SelectedApplication: not null } viewModel })
                return;

            string modsBasePath = ModLoader.GetModsBasePath();
            string titleModsPath = ModLoader.GetApplicationDir(modsBasePath, viewModel.SelectedApplication.IdString);

            OpenHelper.OpenFolder(titleModsPath);
        }

        public void OpenSdModsDirectory_Click(object sender, RoutedEventArgs args)
        {
            if (sender is not MenuItem { DataContext: MainWindowViewModel { SelectedApplication: not null } viewModel })
                return;

            string sdModsBasePath = ModLoader.GetSdModsBasePath();
            string titleModsPath = ModLoader.GetApplicationDir(sdModsBasePath, viewModel.SelectedApplication.IdString);

            OpenHelper.OpenFolder(titleModsPath);
        }

        public async void OpenModManager_Click(object sender, RoutedEventArgs args)
        {
            if (sender is MenuItem { DataContext: MainWindowViewModel { SelectedApplication: not null } viewModel })
                await ModManagerWindow.Show(viewModel.SelectedApplication.Id, viewModel.SelectedApplication.Name);
        }

        public async void PurgePtcCache_Click(object sender, RoutedEventArgs args)
        {
            if (sender is not MenuItem { DataContext: MainWindowViewModel { SelectedApplication: not null } viewModel })
                return;

            UserResult result = await ContentDialogHelper.CreateLocalizedConfirmationDialog(
                LocaleManager.Instance[LocaleKeys.DialogWarning],
                LocaleManager.Instance.UpdateAndGetDynamicValue(LocaleKeys.DialogPPTCDeletionMessage, viewModel.SelectedApplication.Name)
            );

            if (result == UserResult.Yes)
            {
                DirectoryInfo mainDir = new(Path.Combine(AppDataManager.GamesDirPath, viewModel.SelectedApplication.IdString, "cache", "cpu", "0"));
                DirectoryInfo backupDir = new(Path.Combine(AppDataManager.GamesDirPath, viewModel.SelectedApplication.IdString, "cache", "cpu", "1"));

                List<FileInfo> cacheFiles = new();

                if (mainDir.Exists)
                {
                    cacheFiles.AddRange(mainDir.EnumerateFiles("*.cache"));
                }

                if (backupDir.Exists)
                {
                    cacheFiles.AddRange(backupDir.EnumerateFiles("*.cache"));
                }

                if (cacheFiles.Count > 0)
                {
                    foreach (FileInfo file in cacheFiles)
                    {
                        try
                        {
                            file.Delete();
                        }
                        catch (Exception ex)
                        {
                            await ContentDialogHelper.CreateErrorDialog(LocaleManager.Instance.UpdateAndGetDynamicValue(LocaleKeys.DialogPPTCDeletionErrorMessage, file.Name, ex));
                        }
                    }
                }
            }
        }

        public async void PurgeShaderCache_Click(object sender, RoutedEventArgs args)
        {
            if (sender is not MenuItem { DataContext: MainWindowViewModel { SelectedApplication: not null } viewModel })
                return;

            UserResult result = await ContentDialogHelper.CreateLocalizedConfirmationDialog(
                LocaleManager.Instance[LocaleKeys.DialogWarning],
                LocaleManager.Instance.UpdateAndGetDynamicValue(LocaleKeys.DialogShaderDeletionMessage, viewModel.SelectedApplication.Name)
            );

            if (result == UserResult.Yes)
            {
                DirectoryInfo shaderCacheDir = new(Path.Combine(AppDataManager.GamesDirPath, viewModel.SelectedApplication.IdString, "cache", "shader"));

                List<DirectoryInfo> oldCacheDirectories = new();
                List<FileInfo> newCacheFiles = new();

                if (shaderCacheDir.Exists)
                {
                    oldCacheDirectories.AddRange(shaderCacheDir.EnumerateDirectories("*"));
                    newCacheFiles.AddRange(shaderCacheDir.GetFiles("*.toc"));
                    newCacheFiles.AddRange(shaderCacheDir.GetFiles("*.data"));
                }

                if ((oldCacheDirectories.Count > 0 || newCacheFiles.Count > 0))
                {
                    foreach (DirectoryInfo directory in oldCacheDirectories)
                    {
                        try
                        {
                            directory.Delete(true);
                        }
                        catch (Exception ex)
                        {
                            await ContentDialogHelper.CreateErrorDialog(LocaleManager.Instance.UpdateAndGetDynamicValue(LocaleKeys.DialogPPTCDeletionErrorMessage, directory.Name, ex));
                        }
                    }

                    foreach (FileInfo file in newCacheFiles)
                    {
                        try
                        {
                            file.Delete();
                        }
                        catch (Exception ex)
                        {
                            await ContentDialogHelper.CreateErrorDialog(LocaleManager.Instance.UpdateAndGetDynamicValue(LocaleKeys.ShaderCachePurgeError, file.Name, ex));
                        }
                    }
                }
            }
        }

        public void OpenPtcDirectory_Click(object sender, RoutedEventArgs args)
        {
            if (sender is not MenuItem { DataContext: MainWindowViewModel { SelectedApplication: not null } viewModel })
                return;

            string ptcDir = Path.Combine(AppDataManager.GamesDirPath, viewModel.SelectedApplication.IdString, "cache", "cpu");
            string mainDir = Path.Combine(ptcDir, "0");
            string backupDir = Path.Combine(ptcDir, "1");

            if (!Directory.Exists(ptcDir))
            {
                Directory.CreateDirectory(ptcDir);
                Directory.CreateDirectory(mainDir);
                Directory.CreateDirectory(backupDir);
            }

            OpenHelper.OpenFolder(ptcDir);
        }

        public void OpenShaderCacheDirectory_Click(object sender, RoutedEventArgs args)
        {
            if (sender is MenuItem { DataContext: MainWindowViewModel { SelectedApplication: not null } viewModel })
            {
                string shaderCacheDir = Path.Combine(AppDataManager.GamesDirPath, viewModel.SelectedApplication.IdString, "cache", "shader");

                if (!Directory.Exists(shaderCacheDir))
                {
                    Directory.CreateDirectory(shaderCacheDir);
                }

                OpenHelper.OpenFolder(shaderCacheDir);
            }
        }

        public async void ExtractApplicationExeFs_Click(object sender, RoutedEventArgs args)
        {
            if (sender is MenuItem { DataContext: MainWindowViewModel { SelectedApplication: not null } viewModel })
            {
                await ApplicationHelper.ExtractSection(
                    viewModel.StorageProvider,
                    NcaSectionType.Code,
                    viewModel.SelectedApplication.Path,
                    viewModel.SelectedApplication.Name);
            }
        }

        public async void ExtractApplicationRomFs_Click(object sender, RoutedEventArgs args)
        {
            if (sender is MenuItem { DataContext: MainWindowViewModel { SelectedApplication: not null } viewModel })
                await ApplicationHelper.ExtractSection(
                    viewModel.StorageProvider,
                    NcaSectionType.Data,
                    viewModel.SelectedApplication.Path,
                    viewModel.SelectedApplication.Name);
        }
        
        public async void ExtractAocRomFs_Click(object sender, RoutedEventArgs args)
        {
            if (sender is not MenuItem { DataContext: MainWindowViewModel { SelectedApplication: not null } viewModel })
                return;

            DownloadableContentModel selectedDlc = await DlcSelectView.Show(viewModel.SelectedApplication.IdBase, viewModel.ApplicationLibrary);
            
            if (selectedDlc is not null)
            {
                await ApplicationHelper.ExtractAoc(
                    viewModel.StorageProvider,
                    selectedDlc.ContainerPath,
                    selectedDlc.FileName);
            }
        }

        public async void ExtractApplicationLogo_Click(object sender, RoutedEventArgs args)
        {
            if (sender is not MenuItem { DataContext: MainWindowViewModel { SelectedApplication: not null } viewModel })
                return;

            var result = await viewModel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = LocaleManager.Instance[LocaleKeys.FolderDialogExtractTitle],
                AllowMultiple = false,
            });

            if (result.Count == 0)
                return;

            ApplicationHelper.ExtractSection(
                result[0].Path.LocalPath,
                NcaSectionType.Logo,
                viewModel.SelectedApplication.Path,
                viewModel.SelectedApplication.Name);

            var iconFile = await result[0].CreateFileAsync($"{viewModel.SelectedApplication.IdString}.png");
            await using var fileStream = await iconFile.OpenWriteAsync();

            using var bitmap = SKBitmap.Decode(viewModel.SelectedApplication.Icon)
                .Resize(new SKSizeI(512, 512), SKFilterQuality.High);

            using var png = bitmap.Encode(SKEncodedImageFormat.Png, 100);

            png.SaveTo(fileStream);
        }

        public void CreateApplicationShortcut_Click(object sender, RoutedEventArgs args)
        {
            if (sender is MenuItem { DataContext: MainWindowViewModel { SelectedApplication: not null } viewModel })
                ShortcutHelper.CreateAppShortcut(
                    viewModel.SelectedApplication.Path,
                    viewModel.SelectedApplication.Name,
                    viewModel.SelectedApplication.IdString,
                    viewModel.SelectedApplication.Icon
                );
        }

        public async void RunApplication_Click(object sender, RoutedEventArgs args)
        {
            if (sender is MenuItem { DataContext: MainWindowViewModel { SelectedApplication: not null } viewModel })
                await viewModel.LoadApplication(viewModel.SelectedApplication);
        }

        public async void TrimXCI_Click(object sender, RoutedEventArgs args)
        {
            var viewModel = (sender as MenuItem)?.DataContext as MainWindowViewModel;

            if (viewModel?.SelectedApplication != null)
            {
                await viewModel.TrimXCIFile(viewModel.SelectedApplication.Path);
            }
        }
    }
}
