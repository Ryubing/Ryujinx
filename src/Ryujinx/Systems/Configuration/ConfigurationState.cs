using Ryujinx.Ava.Systems.Configuration.System;
using Ryujinx.Ava.Systems.Configuration.UI;
using Ryujinx.Common.Configuration;
using Ryujinx.Common.Configuration.Hid;
using Ryujinx.Common.Configuration.Hid.Keyboard;
using Ryujinx.Common.Configuration.Multiplayer;
using Ryujinx.Graphics.Vulkan;
using Ryujinx.HLE;
using System;
using System.Linq;

namespace Ryujinx.Ava.Systems.Configuration
{
    public partial class ConfigurationState
    {
        public static void Initialize()
        {
            if (Instance != null)
            {
                throw new InvalidOperationException("Configuration is already initialized");
            }

            Instance = new ConfigurationState();
        }

        public ConfigurationFileFormat ToFileFormat()
        {
            ConfigurationFileFormat configurationFile = new()
            {
                Version = ConfigurationFileFormat.CurrentVersion,
                BackendThreading = Graphics.BackendThreading,
                EnableFileLog = Logger.EnableFileLog,
                ResScale = Graphics.ResScale,
                ResScaleCustom = Graphics.ResScaleCustom,
                MaxAnisotropy = Graphics.MaxAnisotropy,
                AspectRatio = Graphics.AspectRatio,
                AntiAliasing = Graphics.AntiAliasing,
                ScalingFilter = Graphics.ScalingFilter,
                ScalingFilterLevel = Graphics.ScalingFilterLevel,
                GraphicsShadersDumpPath = Graphics.ShadersDumpPath,
                LoggingEnableDebug = Logger.EnableDebug,
                LoggingEnableStub = Logger.EnableStub,
                LoggingEnableInfo = Logger.EnableInfo,
                LoggingEnableWarn = Logger.EnableWarn,
                LoggingEnableError = Logger.EnableError,
                LoggingEnableTrace = Logger.EnableTrace,
                LoggingEnableGuest = Logger.EnableGuest,
                LoggingEnableFsAccessLog = Logger.EnableFsAccessLog,
                LoggingEnableAvalonia = Logger.EnableAvaloniaLog,
                LoggingFilteredClasses = Logger.FilteredClasses,
                LoggingGraphicsDebugLevel = Logger.GraphicsDebugLevel,
                SystemLanguage = System.Language,
                SystemRegion = System.Region,
                SystemTimeZone = System.TimeZone,
                SystemTimeOffset = System.SystemTimeOffset,
                MatchSystemTime = System.MatchSystemTime,
                DockedMode = System.EnableDockedMode,
                EnableDiscordIntegration = EnableDiscordIntegration,
                CheckUpdatesOnStart = CheckUpdatesOnStart,
                UpdateCheckerType = UpdateCheckerType,
                FocusLostActionType = FocusLostActionType,
                ShowConfirmExit = ShowConfirmExit,
                RememberWindowState = RememberWindowState,
                ShowTitleBar = ShowTitleBar,
                EnableHardwareAcceleration = EnableHardwareAcceleration,
                HideCursor = HideCursor,
                VSyncMode = Graphics.VSyncMode,
                EnableCustomVSyncInterval = Graphics.EnableCustomVSyncInterval,
                CustomVSyncInterval = Graphics.CustomVSyncInterval,
                EnableShaderCache = Graphics.EnableShaderCache,
                EnableTextureRecompression = Graphics.EnableTextureRecompression,
                EnableMacroHLE = Graphics.EnableMacroHLE,
                EnableColorSpacePassthrough = Graphics.EnableColorSpacePassthrough,
                EnablePtc = System.EnablePtc,
                EnableLowPowerPtc = System.EnableLowPowerPtc,
                EnableInternetAccess = System.EnableInternetAccess,
                EnableFsIntegrityChecks = System.EnableFsIntegrityChecks,
                FsGlobalAccessLogMode = System.FsGlobalAccessLogMode,
                AudioBackend = System.AudioBackend,
                AudioVolume = System.AudioVolume,
                MemoryManagerMode = System.MemoryManagerMode,
                DramSize = System.DramSize,
                IgnoreMissingServices = System.IgnoreMissingServices,
                IgnoreApplet = System.IgnoreControllerApplet,
                UseHypervisor = System.UseHypervisor,
                GuiColumns = new GuiColumns
                {
                    FavColumn = UI.GuiColumns.FavColumn,
                    IconColumn = UI.GuiColumns.IconColumn,
                    AppColumn = UI.GuiColumns.AppColumn,
                    DevColumn = UI.GuiColumns.DevColumn,
                    VersionColumn = UI.GuiColumns.VersionColumn,
                    LdnInfoColumn = UI.GuiColumns.LdnInfoColumn,
                    TimePlayedColumn = UI.GuiColumns.TimePlayedColumn,
                    LastPlayedColumn = UI.GuiColumns.LastPlayedColumn,
                    FileExtColumn = UI.GuiColumns.FileExtColumn,
                    FileSizeColumn = UI.GuiColumns.FileSizeColumn,
                    PathColumn = UI.GuiColumns.PathColumn,
                },
                ColumnSort = new ColumnSort
                {
                    SortColumnId = UI.ColumnSort.SortColumnId,
                    SortAscending = UI.ColumnSort.SortAscending,
                },
                GameDirs = UI.GameDirs,
                AutoloadDirs = UI.AutoloadDirs,
                ShownFileTypes = new ShownFileTypes
                {
                    NSP = UI.ShownFileTypes.NSP,
                    PFS0 = UI.ShownFileTypes.PFS0,
                    XCI = UI.ShownFileTypes.XCI,
                    NCA = UI.ShownFileTypes.NCA,
                    NRO = UI.ShownFileTypes.NRO,
                    NSO = UI.ShownFileTypes.NSO,
                },
                WindowStartup = new WindowStartup
                {
                    WindowSizeWidth = UI.WindowStartup.WindowSizeWidth,
                    WindowSizeHeight = UI.WindowStartup.WindowSizeHeight,
                    WindowPositionX = UI.WindowStartup.WindowPositionX,
                    WindowPositionY = UI.WindowStartup.WindowPositionY,
                    WindowMaximized = UI.WindowStartup.WindowMaximized,
                },
                LanguageCode = UI.LanguageCode,
                BaseStyle = UI.BaseStyle,
                GameListViewMode = UI.GameListViewMode,
                ShowNames = UI.ShowNames,
                GridSize = UI.GridSize,
                ApplicationSort = UI.ApplicationSort,
                IsAscendingOrder = UI.IsAscendingOrder,
                StartFullscreen = UI.StartFullscreen,
                StartNoUI = UI.StartNoUI,
                ShowConsole = UI.ShowConsole,
                EnableKeyboard = Hid.EnableKeyboard,
                EnableMouse = Hid.EnableMouse,
                DisableInputWhenOutOfFocus = Hid.DisableInputWhenOutOfFocus,
                Hotkeys = Hid.Hotkeys,
                InputConfig = Hid.InputConfig,
                RainbowSpeed = Hid.RainbowSpeed,
                GraphicsBackend = Graphics.GraphicsBackend,
                PreferredGpu = Graphics.PreferredGpu,
                MultiplayerLanInterfaceId = Multiplayer.LanInterfaceId,
                MultiplayerMode = Multiplayer.Mode,
                MultiplayerDisableP2p = Multiplayer.DisableP2p,
                MultiplayerLdnPassphrase = Multiplayer.LdnPassphrase,
                LdnServer = Multiplayer.LdnServer,
                ShowDirtyHacks = Hacks.ShowDirtyHacks,
                DirtyHacks = Hacks.EnabledHacks.Select(it => it.Pack()).ToArray(),
            };

            return configurationFile;
        }

        public void LoadDefault()
        {
            Logger.EnableFileLog.Value = true;
            Graphics.BackendThreading.Value = BackendThreading.Auto;
            Graphics.ResScale.Value = 1;
            Graphics.ResScaleCustom.Value = 1.0f;
            Graphics.MaxAnisotropy.Value = -1.0f;
            Graphics.AspectRatio.Value = AspectRatio.Fixed16x9;
            Graphics.GraphicsBackend.Value = DefaultGraphicsBackend();
            Graphics.PreferredGpu.Value = string.Empty;
            Graphics.ShadersDumpPath.Value = string.Empty;
            Logger.EnableDebug.Value = false;
            Logger.EnableStub.Value = true;
            Logger.EnableInfo.Value = true;
            Logger.EnableWarn.Value = true;
            Logger.EnableError.Value = true;
            Logger.EnableTrace.Value = false;
            Logger.EnableGuest.Value = true;
            Logger.EnableFsAccessLog.Value = false;
            Logger.EnableAvaloniaLog.Value = false;
            Logger.FilteredClasses.Value = [];
            Logger.GraphicsDebugLevel.Value = GraphicsDebugLevel.None;
            System.Language.Value = Language.AmericanEnglish;
            System.Region.Value = Region.USA;
            System.TimeZone.Value = "UTC";
            System.SystemTimeOffset.Value = 0;
            System.EnableDockedMode.Value = true;
            EnableDiscordIntegration.Value = true;
            UpdateCheckerType.Value = UpdaterType.PromptAtStartup;
            FocusLostActionType.Value = FocusLostType.DoNothing;
            ShowConfirmExit.Value = true;
            RememberWindowState.Value = true;
            ShowTitleBar.Value = !OperatingSystem.IsWindows();
            EnableHardwareAcceleration.Value = true;
            HideCursor.Value = HideCursorMode.OnIdle;
            Graphics.VSyncMode.Value = VSyncMode.Switch;
            Graphics.CustomVSyncInterval.Value = 120;
            Graphics.EnableCustomVSyncInterval.Value = false;
            Graphics.EnableShaderCache.Value = true;
            Graphics.EnableTextureRecompression.Value = false;
            Graphics.EnableMacroHLE.Value = true;
            Graphics.EnableColorSpacePassthrough.Value = false;
            Graphics.AntiAliasing.Value = AntiAliasing.None;
            Graphics.ScalingFilter.Value = ScalingFilter.Bilinear;
            Graphics.ScalingFilterLevel.Value = 80;
            System.EnablePtc.Value = true;
            System.EnableInternetAccess.Value = false;
            System.EnableFsIntegrityChecks.Value = true;
            System.FsGlobalAccessLogMode.Value = 0;
            System.AudioBackend.Value = AudioBackend.SDL2;
            System.AudioVolume.Value = 1;
            System.MemoryManagerMode.Value = MemoryManagerMode.HostMappedUnsafe;
            System.DramSize.Value = MemoryConfiguration.MemoryConfiguration4GiB;
            System.IgnoreMissingServices.Value = false;
            System.IgnoreControllerApplet.Value = false;
            System.UseHypervisor.Value = true;
            Multiplayer.LanInterfaceId.Value = "0";
            Multiplayer.Mode.Value = MultiplayerMode.Disabled;
            Multiplayer.DisableP2p.Value = false;
            Multiplayer.LdnPassphrase.Value = string.Empty;
            Multiplayer.LdnServer.Value = string.Empty;
            UI.GuiColumns.FavColumn.Value = true;
            UI.GuiColumns.IconColumn.Value = true;
            UI.GuiColumns.AppColumn.Value = true;
            UI.GuiColumns.DevColumn.Value = true;
            UI.GuiColumns.VersionColumn.Value = true;
            UI.GuiColumns.TimePlayedColumn.Value = true;
            UI.GuiColumns.LastPlayedColumn.Value = true;
            UI.GuiColumns.FileExtColumn.Value = true;
            UI.GuiColumns.FileSizeColumn.Value = true;
            UI.GuiColumns.PathColumn.Value = true;
            UI.ColumnSort.SortColumnId.Value = 0;
            UI.ColumnSort.SortAscending.Value = false;
            UI.GameDirs.Value = [];
            UI.AutoloadDirs.Value = [];
            UI.ShownFileTypes.NSP.Value = true;
            UI.ShownFileTypes.PFS0.Value = true;
            UI.ShownFileTypes.XCI.Value = true;
            UI.ShownFileTypes.NCA.Value = true;
            UI.ShownFileTypes.NRO.Value = true;
            UI.ShownFileTypes.NSO.Value = true;
            UI.LanguageCode.Value = "en_US";
            UI.BaseStyle.Value = "Dark";
            UI.GameListViewMode.Value = 0;
            UI.ShowNames.Value = true;
            UI.GridSize.Value = 2;
            UI.ApplicationSort.Value = 0;
            UI.IsAscendingOrder.Value = true;
            UI.StartFullscreen.Value = false;
            UI.StartNoUI.Value = false;
            UI.ShowConsole.Value = true;
            UI.WindowStartup.WindowSizeWidth.Value = 1280;
            UI.WindowStartup.WindowSizeHeight.Value = 760;
            UI.WindowStartup.WindowPositionX.Value = 0;
            UI.WindowStartup.WindowPositionY.Value = 0;
            UI.WindowStartup.WindowMaximized.Value = false;
            Hid.EnableKeyboard.Value = false;
            Hid.EnableMouse.Value = false;
            Hid.DisableInputWhenOutOfFocus.Value = false;
            Hid.Hotkeys.Value = new KeyboardHotkeys
            {
                ToggleVSyncMode = Key.F1,
                ToggleMute = Key.F2,
                Screenshot = Key.F8,
                ShowUI = Key.F4,
                Pause = Key.F5,
                ResScaleUp = Key.Unbound,
                ResScaleDown = Key.Unbound,
                VolumeUp = Key.Unbound,
                VolumeDown = Key.Unbound,
            };
            Hid.RainbowSpeed.Value = 1f;
            Hid.InputConfig.Value =
            [
                new StandardKeyboardInputConfig
                {
                    Version = InputConfig.CurrentVersion,
                    Backend = InputBackendType.WindowKeyboard,
                    Id = "0",
                    PlayerIndex = PlayerIndex.Player1,
                    ControllerType = ControllerType.ProController,
                    LeftJoycon = new LeftJoyconCommonConfig<Key>
                    {
                        DpadUp = Key.Up,
                        DpadDown = Key.Down,
                        DpadLeft = Key.Left,
                        DpadRight = Key.Right,
                        ButtonMinus = Key.Minus,
                        ButtonL = Key.E,
                        ButtonZl = Key.Q,
                        ButtonSl = Key.Unbound,
                        ButtonSr = Key.Unbound,
                    },
                    LeftJoyconStick = new JoyconConfigKeyboardStick<Key>
                    {
                        StickUp = Key.W,
                        StickDown = Key.S,
                        StickLeft = Key.A,
                        StickRight = Key.D,
                        StickButton = Key.F,
                    },
                    RightJoycon = new RightJoyconCommonConfig<Key>
                    {
                        ButtonA = Key.Z,
                        ButtonB = Key.X,
                        ButtonX = Key.C,
                        ButtonY = Key.V,
                        ButtonPlus = Key.Plus,
                        ButtonR = Key.U,
                        ButtonZr = Key.O,
                        ButtonSl = Key.Unbound,
                        ButtonSr = Key.Unbound,
                    },
                    RightJoyconStick = new JoyconConfigKeyboardStick<Key>
                    {
                        StickUp = Key.I,
                        StickDown = Key.K,
                        StickLeft = Key.J,
                        StickRight = Key.L,
                        StickButton = Key.H,
                    },
                }
            ];
        }

        private static GraphicsBackend DefaultGraphicsBackend()
        {
            // Any system running macOS or returning any amount of valid Vulkan devices should default to Vulkan.
            // Checks for if the Vulkan version and featureset is compatible should be performed within VulkanRenderer.
            if (OperatingSystem.IsMacOS() || VulkanRenderer.GetPhysicalDevices().Length > 0)
            {
                return GraphicsBackend.Vulkan;
            }

            return GraphicsBackend.OpenGl;
        }
    }
}
