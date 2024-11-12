using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Gommon;
using LibHac.Ncm;
using LibHac.Tools.FsSystem.NcaUtils;
using Ryujinx.Ava.Common.Locale;
using Ryujinx.Ava.UI.Helpers;
using Ryujinx.Ava.UI.ViewModels;
using Ryujinx.Ava.UI.Windows;
using Ryujinx.Common;
using Ryujinx.Common.Utilities;
using Ryujinx.UI.App.Common;
using Ryujinx.UI.Common;
using Ryujinx.UI.Common.Configuration;
using Ryujinx.UI.Common.Helper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Ryujinx.Ava.UI.Views.Main
{
    public partial class MainMenuBarView : UserControl
    {
        public MainWindow Window { get; private set; }
        public MainWindowViewModel ViewModel { get; private set; }

        public MainMenuBarView()
        {
            InitializeComponent();

            RyuLogo.IsVisible = !ConfigurationState.Instance.ShowTitleBar;

            ToggleFileTypesMenuItem.ItemsSource = GenerateToggleFileTypeItems();
            ChangeLanguageMenuItem.ItemsSource = GenerateLanguageMenuItems();
        }

        private CheckBox[] GenerateToggleFileTypeItems() =>
            Enum.GetValues<FileTypes>()
                .Select(it => (FileName: Enum.GetName(it)!, FileType: it))
                .Select(it =>
                    new CheckBox
                    {
                        Content = $".{it.FileName}",
                        IsChecked = it.FileType.GetConfigValue(ConfigurationState.Instance.UI.ShownFileTypes),
                        Command = MiniCommand.Create(() => Window.ToggleFileType(it.FileName))
                    }
                ).ToArray();

        private static MenuItem[] GenerateLanguageMenuItems()
        {
            List<MenuItem> menuItems = new();

            string localePath = "Ryujinx/Assets/Locales";
            string localeExt = ".json";

            string[] localesPath = EmbeddedResources.GetAllAvailableResources(localePath, localeExt);

            Array.Sort(localesPath);

            foreach (string locale in localesPath)
            {
                string languageCode = Path.GetFileNameWithoutExtension(locale).Split('.').Last();
                string languageJson = EmbeddedResources.ReadAllText($"{localePath}/{languageCode}{localeExt}");
                var strings = JsonHelper.Deserialize(languageJson, CommonJsonContext.Default.StringDictionary);

                if (!strings.TryGetValue("Language", out string languageName))
                {
                    languageName = languageCode;
                }

                MenuItem menuItem = new()
                {
                    Padding = new Thickness(10, 0, 0, 0),
                    Header = " " + languageName,
                    Command = MiniCommand.Create(() => MainWindowViewModel.ChangeLanguage(languageCode))
                };

                menuItems.Add(menuItem);
            }

            return menuItems.ToArray();
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);

            if (VisualRoot is MainWindow window)
            {
                Window = window;
                DataContext = ViewModel = window.ViewModel;
            }
        }

        private async void StopEmulation_Click(object sender, RoutedEventArgs e)
        {
            await ViewModel.AppHost?.ShowExitPrompt().OrCompleted()!;
        }

        private void PauseEmulation_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.AppHost?.Pause();
        }

        private void ResumeEmulation_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.AppHost?.Resume();
        }

        public async void OpenSettings(object sender, RoutedEventArgs e)
        {
            Window.SettingsWindow = new(Window.VirtualFileSystem, Window.ContentManager);

            await Window.SettingsWindow.ShowDialog(Window);

            Window.SettingsWindow = null;

            ViewModel.LoadConfigurableHotKeys();
        }

        public async void OpenMiiApplet(object sender, RoutedEventArgs e)
        {
            string contentPath = ViewModel.ContentManager.GetInstalledContentPath(0x0100000000001009, StorageId.BuiltInSystem, NcaContentType.Program);

            if (!string.IsNullOrEmpty(contentPath))
            {
                ApplicationData applicationData = new()
                {
                    Name = "miiEdit",
                    Id = 0x0100000000001009,
                    Path = contentPath,
                };

                await ViewModel.LoadApplication(applicationData, ViewModel.IsFullScreen || ViewModel.StartGamesInFullscreen);
            }
        }

        public async void OpenAmiiboWindow(object sender, RoutedEventArgs e)
            => await ViewModel.OpenAmiiboWindow();

        public async void OpenCheatManagerForCurrentApp(object sender, RoutedEventArgs e)
        {
            if (!ViewModel.IsGameRunning)
                return;

            string name = ViewModel.AppHost.Device.Processes.ActiveApplication.ApplicationControlProperties.Title[(int)ViewModel.AppHost.Device.System.State.DesiredTitleLanguage].NameString.ToString();

            await new CheatWindow(
                Window.VirtualFileSystem,
                ViewModel.AppHost.Device.Processes.ActiveApplication.ProgramIdText,
                name,
                ViewModel.SelectedApplication.Path).ShowDialog(Window);

            ViewModel.AppHost.Device.EnableCheats();
        }

        private void ScanAmiiboMenuItem_AttachedToVisualTree(object sender, VisualTreeAttachmentEventArgs e)
        {
            if (sender is MenuItem)
                ViewModel.IsAmiiboRequested = ViewModel.AppHost.Device.System.SearchingForAmiibo(out _);
        }

        private async void InstallFileTypes_Click(object sender, RoutedEventArgs e)
        {
            if (FileAssociationHelper.Install())
                await ContentDialogHelper.CreateInfoDialog(LocaleManager.Instance[LocaleKeys.DialogInstallFileTypesSuccessMessage], string.Empty, LocaleManager.Instance[LocaleKeys.InputDialogOk], string.Empty, string.Empty);
            else
                await ContentDialogHelper.CreateErrorDialog(LocaleManager.Instance[LocaleKeys.DialogInstallFileTypesErrorMessage]);
        }

        private async void UninstallFileTypes_Click(object sender, RoutedEventArgs e)
        {
            if (FileAssociationHelper.Uninstall())
                await ContentDialogHelper.CreateInfoDialog(LocaleManager.Instance[LocaleKeys.DialogUninstallFileTypesSuccessMessage], string.Empty, LocaleManager.Instance[LocaleKeys.InputDialogOk], string.Empty, string.Empty);
            else
                await ContentDialogHelper.CreateErrorDialog(LocaleManager.Instance[LocaleKeys.DialogUninstallFileTypesErrorMessage]);
        }

        private async void ChangeWindowSize_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not MenuItem { Tag: string resolution })
                return;

            (int width, int height) = resolution.Split(' ', 2)
                .Into(parts => 
                    (int.Parse(parts[0]), int.Parse(parts[1]))
                );

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                ViewModel.WindowState = WindowState.Normal;

                height += (int)Window.StatusBarHeight + (int)Window.MenuBarHeight;

                Window.Arrange(new Rect(Window.Position.X, Window.Position.Y, width, height));
            });
        }

        public async void CheckForUpdates(object sender, RoutedEventArgs e)
        {
            if (Updater.CanUpdate(true))
                await Window.BeginUpdateAsync(true);
        }

        public async void OpenXCITrimmerWindow(object sender, RoutedEventArgs e) => await XCITrimmerWindow.Show(ViewModel);

        public async void OpenAboutWindow(object sender, RoutedEventArgs e) => await AboutWindow.Show();

        public void CloseWindow(object sender, RoutedEventArgs e) => Window.Close();
    }
}
