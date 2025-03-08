using Avalonia.Interactivity;
using Ryujinx.Ava.UI.Controls;
using Ryujinx.Ava.UI.ViewModels;
using System;

namespace Ryujinx.Ava.UI.Views.Settings
{
    public partial class SettingsNetworkView : RyujinxControl<SettingsViewModel>
    {
        private readonly Random _random;

        public SettingsNetworkView()
        {
            _random = new Random();
            InitializeComponent();
        }

        private void GenLdnPassButton_OnClick(object sender, RoutedEventArgs e)
        {
            byte[] code = new byte[4];
            _random.NextBytes(code);
            ViewModel.LdnPassphrase = $"Ryujinx-{BitConverter.ToUInt32(code):x8}";
        }

        private void ClearLdnPassButton_OnClick(object sender, RoutedEventArgs e)
        {
            ViewModel.LdnPassphrase = string.Empty;
        }
    }
}
