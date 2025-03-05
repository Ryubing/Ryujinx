using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input.Platform;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using Avalonia.Styling;
using Avalonia.Threading;
using FluentAvalonia.UI.Windowing;
using Gommon;
using Ryujinx.Ava.Common;
using Ryujinx.Ava.Common.Locale;
using Ryujinx.Ava.UI.Helpers;
using Ryujinx.Ava.UI.Windows;
using Ryujinx.Ava.Utilities;
using Ryujinx.Ava.Systems.Configuration;
using Ryujinx.Common;
using Ryujinx.Common.Logging;
using System;
using System.Diagnostics;

namespace Ryujinx.Ava
{
    public class RyujinxApp : Application
    {
        public static event Action ThemeChanged;
        
        internal static string FormatTitle(LocaleKeys? windowTitleKey = null, bool includeVersion = true)
            => windowTitleKey is null
                ? $"{FullAppName}{(includeVersion ? $" {Program.Version}" : string.Empty)}"
                : $"{FullAppName}{(includeVersion ? $" {Program.Version}" : string.Empty)} - {LocaleManager.Instance[windowTitleKey.Value]}";

        public static readonly string FullAppName = string.Intern(ReleaseInformation.IsCanaryBuild ? "Ryujinx Canary" : "Ryujinx");

        public static MainWindow MainWindow => Current!
            .ApplicationLifetime.Cast<IClassicDesktopStyleApplicationLifetime>()
            .MainWindow.Cast<MainWindow>();
        
        public static IClassicDesktopStyleApplicationLifetime AppLifetime => Current!
            .ApplicationLifetime.Cast<IClassicDesktopStyleApplicationLifetime>();

        public static bool IsClipboardAvailable(out IClipboard clipboard)
        {
            clipboard = MainWindow.Clipboard;
            return clipboard != null;
        }

        public static void SetTaskbarProgress(TaskBarProgressBarState state) => MainWindow.PlatformFeatures.SetTaskBarProgressBarState(state);
        public static void SetTaskbarProgressValue(ulong current, ulong total) => MainWindow.PlatformFeatures.SetTaskBarProgressBarValue(current, total);
        public static void SetTaskbarProgressValue(long current, long total) => SetTaskbarProgressValue(Convert.ToUInt64(current), Convert.ToUInt64(total));


        public override void Initialize()
        {
            Name = FormatTitle();

            AvaloniaXamlLoader.Load(this);

            if (OperatingSystem.IsMacOS())
            {
                Process.Start("/usr/bin/defaults", "write org.ryujinx.Ryujinx ApplePressAndHoldEnabled -bool false");
            }
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = new MainWindow();
            }

            base.OnFrameworkInitializationCompleted();

            if (Program.PreviewerDetached)
            {
                ApplyConfiguredTheme(ConfigurationState.Instance.UI.BaseStyle);

                ConfigurationState.Instance.UI.BaseStyle.Event += ThemeChanged_Event;
            }
        }

        private void ShowRestartDialog()
        {
            _ = Dispatcher.UIThread.InvokeAsync(async () =>
            {
                if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                {
                    UserResult result = await ContentDialogHelper.CreateConfirmationDialog(
                        LocaleManager.Instance[LocaleKeys.DialogThemeRestartMessage],
                        LocaleManager.Instance[LocaleKeys.DialogThemeRestartSubMessage],
                        LocaleManager.Instance[LocaleKeys.InputDialogYes],
                        LocaleManager.Instance[LocaleKeys.InputDialogNo],
                        LocaleManager.Instance[LocaleKeys.DialogRestartRequiredMessage]);

                    if (result == UserResult.Yes)
                    {
                        _ = Process.Start(Environment.ProcessPath!, CommandLineState.Arguments);
                        desktop.Shutdown();
                        Environment.Exit(0);
                    }
                }
            });
        }

        private void ThemeChanged_Event(object _, ReactiveEventArgs<string> rArgs) => ApplyConfiguredTheme(rArgs.NewValue);

        public void ApplyConfiguredTheme(string baseStyle)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(baseStyle))
                {
                    ConfigurationState.Instance.UI.BaseStyle.Value = "Auto";

                    baseStyle = ConfigurationState.Instance.UI.BaseStyle;
                }

                ThemeChanged?.Invoke();

                RequestedThemeVariant = baseStyle switch
                {
                    "Auto" => DetectSystemTheme(),
                    "Light" => ThemeVariant.Light,
                    "Dark" => ThemeVariant.Dark,
                    _ => ThemeVariant.Default,
                };
            }
            catch (Exception)
            {
                Logger.Warning?.Print(LogClass.Application, "Failed to apply theme. A restart is needed to apply the selected theme.");

                ShowRestartDialog();
            }
        }

        /// <summary>
        /// Converts a PlatformThemeVariant value to the corresponding ThemeVariant value.
        /// </summary>
        public static ThemeVariant ConvertThemeVariant(PlatformThemeVariant platformThemeVariant) =>
            platformThemeVariant switch
            {
                PlatformThemeVariant.Dark => ThemeVariant.Dark,
                PlatformThemeVariant.Light => ThemeVariant.Light,
                _ => ThemeVariant.Default,
            };

        public static ThemeVariant DetectSystemTheme() =>
            Current is RyujinxApp { PlatformSettings: not null } app
                ? ConvertThemeVariant(app.PlatformSettings.GetColorValues().ThemeVariant)
                : ThemeVariant.Default;

        private async void AboutRyujinx_OnClick(object sender, EventArgs e)
        {
            await AboutWindow.Show();
        }
    }
}
