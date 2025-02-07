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
    public partial class UserConfigWindows : StyleableAppWindow
    {
        internal readonly SettingsViewModel ViewModel;

        public UserConfigWindows(MainWindowViewModel viewModel)
        {
            Title = RyujinxApp.FormatTitle(LocaleKeys.Settings);

            DataContext = ViewModel = new SettingsViewModel(
                viewModel.VirtualFileSystem, 
                viewModel.ContentManager,
                viewModel.SelectedApplication.Path,
                viewModel.SelectedApplication.Name,
                viewModel.SelectedApplication.IdString,
                viewModel.SelectedApplication.Icon);

            ViewModel.CloseWindow += Close;
            ViewModel.SaveSettingsEvent += SaveSettings;

            InitializeComponent();
            Load();

#if DEBUG
            this.AttachDevTools(new KeyGesture(Key.F12, KeyModifiers.Alt));
#endif
           
        }

        public void SaveSettings()
        {
            InputPage.InputView?.SaveCurrentProfile();
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
                    case nameof(InputPage):
                        NavPanel.Content = InputPage;
                        break;
                    case nameof(SystemPage):
                        SystemPage.ViewModel = ViewModel;
                        NavPanel.Content = SystemPage;
                        break;
                    case nameof(CpuPage):
                        NavPanel.Content = CpuPage;
                        break;
                    case nameof(GraphicsPage):
                        NavPanel.Content = GraphicsPage;
                        break;
                    case nameof(AudioPage):
                        NavPanel.Content = AudioPage;
                        break;
                    case nameof(NetworkPage):
                        NetworkPage.ViewModel = ViewModel;
                        NavPanel.Content = NetworkPage;
                        break;
                    case nameof(LoggingPage):
                        NavPanel.Content = LoggingPage;
                        break;
                    case nameof(HacksPage):
                        HacksPage.DataContext = ViewModel;
                        NavPanel.Content = HacksPage;
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
