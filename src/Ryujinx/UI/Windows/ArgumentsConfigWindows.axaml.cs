using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using FluentAvalonia.Core;
using FluentAvalonia.UI.Controls;
using Projektanker.Icons.Avalonia;
using Ryujinx.Ava.Common.Locale;
using Ryujinx.Ava.UI.Models;
using Ryujinx.Ava.UI.ViewModels;
using Ryujinx.Ava.UI.ViewModels.Input;
using Ryujinx.Ava.Utilities;
using Ryujinx.Ava.Utilities.Configuration;
using Ryujinx.Common.Configuration;
using Ryujinx.HLE.FileSystem;
using Ryujinx.HLE.HOS.SystemState;
using Ryujinx.Input;
using System;
using System.IO;
using System.Linq;
using Key = Avalonia.Input.Key;


namespace Ryujinx.Ava.UI.Windows
{
    public partial class ArgumentsConfigWindows : StyleableAppWindow
    {
        internal readonly SettingsViewModel ViewModel;
        public string GamePath { get; }
        public string GameName { get; }
        public string GameId { get; }
        public byte[] GameIconData { get; }

        public static int OverrideBackendThreading { get; private set; }
        public static int OverrideGraphicsBackend { get; private set; }
        public static int OverrideSystemLanguage { get; private set; }
        public static int OverrideSystemRegion { get; private set; }
        public static bool OverridePPTC { get; private set; }
        public static int OverrideMemoryManagerMode { get; private set; }


        public ArgumentsConfigWindows(MainWindowViewModel viewModel)
        {
            Title = RyujinxApp.FormatTitle(LocaleKeys.Settings);

            DataContext = ViewModel = new SettingsViewModel(
                viewModel.VirtualFileSystem, 
                viewModel.ContentManager,
                viewModel.SelectedApplication.Path,
                viewModel.SelectedApplication.Name,
                viewModel.SelectedApplication.IdString,
                viewModel.SelectedApplication.Icon);

            GamePath = viewModel.SelectedApplication.Path;
            GameName = viewModel.SelectedApplication.Name;
            GameId = viewModel.SelectedApplication.IdString;
            GameIconData = viewModel.SelectedApplication.Icon;

            OverrideBackendThreading = ViewModel.GraphicsBackendMultithreadingIndex;
            OverrideGraphicsBackend = ViewModel.GraphicsBackendIndex;
            OverrideSystemLanguage = ViewModel.Language;
            OverrideSystemRegion = ViewModel.Region;
            OverridePPTC = ViewModel.EnablePptc;
            OverrideMemoryManagerMode = ViewModel.MemoryMode;

            ViewModel.CloseWindow += Close;
            ViewModel.CompareSettingsEvent += CompareConfiguration;

            InitializeComponent();
            Load();

#if DEBUG
            this.AttachDevTools(new KeyGesture(Key.F12, KeyModifiers.Alt));
#endif
        }

        public void CompareConfiguration()
        {
            ShortcutHelper.CreateAppShortcut(
                GamePath,
                GameName,
                GameId,
                GameIconData,
                GetArguments()
                );
        }

        private string GetArguments() 
        {

            string line = "";

            if (OverrideBackendThreading != ViewModel.GraphicsBackendMultithreadingIndex)
            {
                string _result = Enum.GetName(typeof(BackendThreading), ViewModel.GraphicsBackendMultithreadingIndex);
                line += " --backend-threading " + _result;
            }

            if (OverrideGraphicsBackend != ViewModel.GraphicsBackendIndex)
            {
                string _result = Enum.GetName(typeof(GraphicsBackend), ViewModel.GraphicsBackendIndex);
                line += " -g " + _result;
            }

            if (OverridePPTC != ViewModel.EnablePptc)
            {
                string _result =  ViewModel.EnablePptc.ToString();
                line += " --pptc " + _result;
            }

            if (OverrideMemoryManagerMode != ViewModel.MemoryMode)
            {
                string _result = Enum.GetName(typeof(MemoryManagerMode), ViewModel.MemoryMode);
                line += " -m " + _result;
            }

            if (OverrideSystemRegion != ViewModel.Region)
            {
                string _result = Enum.GetName(typeof(RegionCode), ViewModel.Region);
                line += " --system-region " + _result;
            }

            if (OverrideSystemLanguage != ViewModel.Language) 
            {
                string _result = Enum.GetName(typeof(SystemLanguage), ViewModel.Language);
                line += " --system-language " + _result;
            }

            return line;
        }

        private void Load()
        {
            Pages.Children.Clear();
            NavPanel.SelectionChanged += NavPanelOnSelectionChanged;
            NavPanel.SelectedItem = NavPanel.MenuItems.ElementAt(0);
            
        }

        private void NavPanelOnSelectionChanged(object sender, NavigationViewSelectionChangedEventArgs e)
        {
            
            if (e.SelectedItem is NavigationViewItem navItem && navItem.Tag is not null)
            {
                switch (navItem.Tag.ToString())
                {
                    case nameof(AllSettings):
                        NavPanel.Content = AllSettings;
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }        
        }

        protected override void OnClosing(WindowClosingEventArgs e)
        {
            base.OnClosing(e);
        }
    }
}
