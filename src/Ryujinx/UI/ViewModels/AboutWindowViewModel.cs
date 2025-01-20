using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using Ryujinx.Ava.Common;
using Ryujinx.Ava.Common.Locale;
using Ryujinx.Ava.Utilities.Configuration;
using System;

namespace Ryujinx.Ava.UI.ViewModels
{
    public partial class AboutWindowViewModel : BaseModel, IDisposable
    {
        [ObservableProperty] private Bitmap _githubLogo;
        [ObservableProperty] private Bitmap _discordLogo;
        [ObservableProperty] private string _version;

        public string Developers => "GreemDev";

        public string FormerDevelopers => LocaleManager.Instance.UpdateAndGetDynamicValue(LocaleKeys.AboutPageDeveloperListMore, "gdkchan, Ac_K, marysaka, rip in peri peri, LDj3SNuD, emmaus, Thealexbarney, GoffyDude, TSRBerry, IsaacMarovitz");

        public AboutWindowViewModel()
        {
            Version = RyujinxApp.FullAppName + "\n" + Program.Version;
            UpdateLogoTheme(ConfigurationState.Instance.UI.BaseStyle.Value);

            ThemeManager.ThemeChanged += ThemeManager_ThemeChanged;
        }

        private void ThemeManager_ThemeChanged()
        {
            Dispatcher.UIThread.Post(() => UpdateLogoTheme(ConfigurationState.Instance.UI.BaseStyle.Value));
        }

        private void UpdateLogoTheme(string theme)
        {
            bool isDarkTheme = theme == "Dark" || (theme == "Auto" && RyujinxApp.DetectSystemTheme() == ThemeVariant.Dark);

            string basePath = "resm:Ryujinx.Assets.UIImages.";
            string themeSuffix = isDarkTheme ? "Dark.png" : "Light.png";

            GithubLogo = LoadBitmap($"{basePath}Logo_GitHub_{themeSuffix}?assembly=Ryujinx");
            DiscordLogo = LoadBitmap($"{basePath}Logo_Discord_{themeSuffix}?assembly=Ryujinx");
        }

        private static Bitmap LoadBitmap(string uri) => new(Avalonia.Platform.AssetLoader.Open(new Uri(uri)));

        public void Dispose()
        {
            ThemeManager.ThemeChanged -= ThemeManager_ThemeChanged;
            GC.SuppressFinalize(this);
        }
    }
}
