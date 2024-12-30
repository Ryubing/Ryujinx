using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Gommon;
using Ryujinx.Ava.Common.Locale;
using Ryujinx.Ava.UI.Helpers;
using Ryujinx.Ava.UI.ViewModels;
using Ryujinx.Ava.UI.Windows;
using Ryujinx.Ava.Utilities;
using Ryujinx.Ava.Utilities.Configuration;
using Ryujinx.Common;
using Ryujinx.Common.Helper;
using Ryujinx.Common.Utilities;
using Ryujinx.HLE.HOS.Services.Nfc.AmiiboDecryption;
using System;
using System.Collections.Generic;
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
            RyuLogo.Source = MainWindowViewModel.IconBitmap;

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

            string localePath = "Ryujinx/Assets/locales.json";

            string languageJson = EmbeddedResources.ReadAllText(localePath);

            LocalesJson locales = JsonHelper.Deserialize(languageJson, LocalesJsonContext.Default.LocalesJson);

            foreach (string language in locales.Languages)
            {
                int index = locales.Locales.FindIndex(x => x.ID == "Language");
                string languageName;

                if (index == -1)
                {
                    languageName = language;
                }
                else
                {
                    languageName = locales.Locales[index].Translations[language] == "" ? language : locales.Locales[index].Translations[language];
                }

                MenuItem menuItem = new()
                {
                    Padding = new Thickness(10, 0, 0, 0),
                    Header = " " + languageName,
                    Command = MiniCommand.Create(() => MainWindowViewModel.ChangeLanguage(language))
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

        public static readonly AppletMetadata MiiApplet = new("miiEdit", 0x0100000000001009);

        public async void OpenMiiApplet(object sender, RoutedEventArgs e)
        {
            if (MiiApplet.CanStart(ViewModel.ContentManager, out var appData, out var nacpData))
            {
                await ViewModel.LoadApplication(appData, ViewModel.IsFullScreen || ViewModel.StartGamesInFullscreen, nacpData);
            }
        }

        public async void OpenAmiiboWindow(object sender, RoutedEventArgs e)
            => await ViewModel.OpenAmiiboWindow();

        public async void OpenBinFile(object sender, RoutedEventArgs e)
            => await ViewModel.OpenBinFile();

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

        private void ScanBinAmiiboMenuItem_AttachedToVisualTree(object sender, VisualTreeAttachmentEventArgs e)
        {
            if (sender is MenuItem)
                ViewModel.IsAmiiboBinRequested = ViewModel.IsAmiiboRequested && AmiiboBinReader.HasAmiiboKeyFile;
        }

        private async void InstallFileTypes_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.AreMimeTypesRegistered = FileAssociationHelper.Install();
            if (ViewModel.AreMimeTypesRegistered)
                await ContentDialogHelper.CreateInfoDialog(LocaleManager.Instance[LocaleKeys.DialogInstallFileTypesSuccessMessage], string.Empty, LocaleManager.Instance[LocaleKeys.InputDialogOk], string.Empty, string.Empty);
            else
                await ContentDialogHelper.CreateErrorDialog(LocaleManager.Instance[LocaleKeys.DialogInstallFileTypesErrorMessage]);
        }

        private async void UninstallFileTypes_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.AreMimeTypesRegistered = !FileAssociationHelper.Uninstall();
            if (!ViewModel.AreMimeTypesRegistered)
                await ContentDialogHelper.CreateInfoDialog(LocaleManager.Instance[LocaleKeys.DialogUninstallFileTypesSuccessMessage], string.Empty, LocaleManager.Instance[LocaleKeys.InputDialogOk], string.Empty, string.Empty);
            else
                await ContentDialogHelper.CreateErrorDialog(LocaleManager.Instance[LocaleKeys.DialogUninstallFileTypesErrorMessage]);
        }

        private async void ChangeWindowSize_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not MenuItem { Tag: string resolution })
                return;

            (int resolutionWidth, int resolutionHeight) = resolution.Split(' ', 2)
                .Into(parts => 
                    (int.Parse(parts[0]), int.Parse(parts[1]))
                );

            // Correctly size window when 'TitleBar' is enabled (Nov. 14, 2024)
            double barsHeight = ((Window.StatusBarHeight + Window.MenuBarHeight) +
                (ConfigurationState.Instance.ShowTitleBar ? (int)Window.TitleBar.Height : 0));

            double windowWidthScaled = (resolutionWidth * Program.WindowScaleFactor);
            double windowHeightScaled = ((resolutionHeight + barsHeight) * Program.WindowScaleFactor);

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                ViewModel.WindowState = WindowState.Normal;

                Window.Arrange(new Rect(Window.Position.X, Window.Position.Y, windowWidthScaled, windowHeightScaled));
            });
        }

        public async void CheckForUpdates(object sender, RoutedEventArgs e)
        {
            if (Updater.CanUpdate(true))
                await Updater.BeginUpdateAsync(true);
        }

        private void MenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem { Tag: string url })
                OpenHelper.OpenUrl(url);
        }

        public async void OpenXCITrimmerWindow(object sender, RoutedEventArgs e) => await XCITrimmerWindow.Show(ViewModel);

        public async void OpenAboutWindow(object sender, RoutedEventArgs e) => await AboutWindow.Show();

        public void CloseWindow(object sender, RoutedEventArgs e) => Window.Close();
    }
}
