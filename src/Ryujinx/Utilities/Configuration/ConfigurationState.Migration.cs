using Avalonia.Media;
using Gommon;
using Ryujinx.Ava.Utilities.Configuration.System;
using Ryujinx.Ava.Utilities.Configuration.UI;
using Ryujinx.Common.Configuration;
using Ryujinx.Common.Configuration.Hid;
using Ryujinx.Common.Configuration.Hid.Controller;
using Ryujinx.Common.Configuration.Hid.Keyboard;
using Ryujinx.Common.Configuration.Multiplayer;
using Ryujinx.Common.Logging;
using Ryujinx.HLE;
using System;
using System.Collections.Generic;
using System.Linq;
using RyuLogger = Ryujinx.Common.Logging.Logger;

namespace Ryujinx.Ava.Utilities.Configuration
{
    public partial class ConfigurationState
    {
        public void Load(ConfigurationFileFormat cff, string configurationFilePath, string titleId = "")
        {
            bool configurationFileUpdated = false;
            bool shouldLoadFromFile = string.IsNullOrEmpty(titleId);

            if (cff.Version is < 0 or > ConfigurationFileFormat.CurrentVersion)
            {
                RyuLogger.Warning?.Print(LogClass.Application, $"Unsupported configuration version {cff.Version}, loading default.");

                LoadDefault();
            }

            foreach ((int newVersion, Action<ConfigurationFileFormat> migratorFunction)
                     in _migrations.OrderBy(x => x.Key))
            {
                if (cff.Version >= newVersion) 
                    continue;

                RyuLogger.Warning?.Print(LogClass.Application, 
                    $"Outdated configuration version {cff.Version}, migrating to version {newVersion}.");
                
                migratorFunction(cff);

                configurationFileUpdated = true;
            }
            
          
            EnableDiscordIntegration.Value = cff.EnableDiscordIntegration;
            CheckUpdatesOnStart.Value = shouldLoadFromFile ? cff.CheckUpdatesOnStart : CheckUpdatesOnStart.Value; // Get from global config only
            UpdateCheckerType.Value = shouldLoadFromFile ? cff.UpdateCheckerType : UpdateCheckerType.Value; // Get from global config only
            FocusLostActionType.Value = cff.FocusLostActionType;
            ShowConfirmExit.Value = shouldLoadFromFile ? cff.ShowConfirmExit : ShowConfirmExit.Value; // Get from global config only
            RememberWindowState.Value = shouldLoadFromFile ? cff.RememberWindowState : RememberWindowState.Value; // Get from global config only
            ShowTitleBar.Value = shouldLoadFromFile ? cff.ShowTitleBar : ShowTitleBar.Value; // Get from global config only
            EnableHardwareAcceleration.Value = shouldLoadFromFile ? cff.EnableHardwareAcceleration : EnableHardwareAcceleration.Value; // Get from global config only
            HideCursor.Value = cff.HideCursor;
          
            Logger.EnableFileLog.Value = cff.EnableFileLog;
            Logger.EnableDebug.Value = cff.LoggingEnableDebug;
            Logger.EnableStub.Value = cff.LoggingEnableStub;
            Logger.EnableInfo.Value = cff.LoggingEnableInfo;
            Logger.EnableWarn.Value = cff.LoggingEnableWarn;
            Logger.EnableError.Value = cff.LoggingEnableError;
            Logger.EnableTrace.Value = cff.LoggingEnableTrace;
            Logger.EnableGuest.Value = cff.LoggingEnableGuest;
            Logger.EnableFsAccessLog.Value = cff.LoggingEnableFsAccessLog;
            Logger.FilteredClasses.Value = cff.LoggingFilteredClasses;
            Logger.GraphicsDebugLevel.Value = cff.LoggingGraphicsDebugLevel;
            
            Graphics.ResScale.Value = cff.ResScale;
            Graphics.ResScaleCustom.Value = cff.ResScaleCustom;
            Graphics.MaxAnisotropy.Value = cff.MaxAnisotropy;
            Graphics.AspectRatio.Value = cff.AspectRatio;
            Graphics.ShadersDumpPath.Value = cff.GraphicsShadersDumpPath;
            Graphics.BackendThreading.Value = cff.BackendThreading;
            Graphics.GraphicsBackend.Value = cff.GraphicsBackend;
            Graphics.PreferredGpu.Value = cff.PreferredGpu;
            Graphics.AntiAliasing.Value = cff.AntiAliasing;
            Graphics.ScalingFilter.Value = cff.ScalingFilter;
            Graphics.ScalingFilterLevel.Value = cff.ScalingFilterLevel;
            Graphics.VSyncMode.Value = cff.VSyncMode;
            Graphics.EnableCustomVSyncInterval.Value = cff.EnableCustomVSyncInterval;
            Graphics.CustomVSyncInterval.Value = cff.CustomVSyncInterval;
            Graphics.EnableShaderCache.Value = cff.EnableShaderCache;
            Graphics.EnableTextureRecompression.Value = cff.EnableTextureRecompression;
            Graphics.EnableMacroHLE.Value = cff.EnableMacroHLE;
            Graphics.EnableColorSpacePassthrough.Value = cff.EnableColorSpacePassthrough;
            
            System.Language.Value = cff.SystemLanguage;
            System.Region.Value = cff.SystemRegion;
            System.TimeZone.Value = cff.SystemTimeZone;
            System.SystemTimeOffset.Value = shouldLoadFromFile ? cff.SystemTimeOffset : System.SystemTimeOffset.Value; // Get from global config only
            System.MatchSystemTime.Value = shouldLoadFromFile ? cff.MatchSystemTime : System.MatchSystemTime.Value; // Get from global config only
            System.EnableDockedMode.Value = cff.DockedMode;
            System.EnablePtc.Value = cff.EnablePtc;
            System.EnableLowPowerPtc.Value = cff.EnableLowPowerPtc;
            System.EnableInternetAccess.Value = cff.EnableInternetAccess;
            System.EnableFsIntegrityChecks.Value = cff.EnableFsIntegrityChecks;
            System.FsGlobalAccessLogMode.Value = cff.FsGlobalAccessLogMode;
            System.AudioBackend.Value = cff.AudioBackend;
            System.AudioVolume.Value = cff.AudioVolume;
            System.MemoryManagerMode.Value = cff.MemoryManagerMode;
            System.DramSize.Value = cff.DramSize;
            System.IgnoreMissingServices.Value = cff.IgnoreMissingServices;
            System.IgnoreControllerApplet.Value = cff.IgnoreApplet;
            System.UseHypervisor.Value = cff.UseHypervisor;

            UI.GuiColumns.FavColumn.Value = shouldLoadFromFile ? cff.GuiColumns.FavColumn : UI.GuiColumns.FavColumn.Value;
            UI.GuiColumns.IconColumn.Value = shouldLoadFromFile ? cff.GuiColumns.IconColumn : UI.GuiColumns.IconColumn.Value;
            UI.GuiColumns.AppColumn.Value = shouldLoadFromFile ? cff.GuiColumns.AppColumn : UI.GuiColumns.AppColumn.Value;
            UI.GuiColumns.DevColumn.Value = shouldLoadFromFile ? cff.GuiColumns.DevColumn : UI.GuiColumns.DevColumn.Value;
            UI.GuiColumns.VersionColumn.Value = shouldLoadFromFile ? cff.GuiColumns.VersionColumn : UI.GuiColumns.VersionColumn.Value;
            UI.GuiColumns.TimePlayedColumn.Value = shouldLoadFromFile ? cff.GuiColumns.TimePlayedColumn : UI.GuiColumns.TimePlayedColumn.Value;
            UI.GuiColumns.LastPlayedColumn.Value = shouldLoadFromFile ? cff.GuiColumns.LastPlayedColumn : UI.GuiColumns.LastPlayedColumn.Value;
            UI.GuiColumns.FileExtColumn.Value = shouldLoadFromFile ? cff.GuiColumns.FileExtColumn : UI.GuiColumns.FileExtColumn.Value;
            UI.GuiColumns.FileSizeColumn.Value = shouldLoadFromFile ? cff.GuiColumns.FileSizeColumn : UI.GuiColumns.FileSizeColumn.Value;
            UI.GuiColumns.PathColumn.Value = shouldLoadFromFile ? cff.GuiColumns.PathColumn : UI.GuiColumns.PathColumn.Value;
            UI.ColumnSort.SortColumnId.Value = shouldLoadFromFile ? cff.ColumnSort.SortColumnId : UI.ColumnSort.SortColumnId.Value;
            UI.ColumnSort.SortAscending.Value = shouldLoadFromFile ? cff.ColumnSort.SortAscending : UI.ColumnSort.SortAscending.Value;
            UI.GameDirs.Value = shouldLoadFromFile ? cff.GameDirs : UI.GameDirs.Value;
            UI.AutoloadDirs.Value = shouldLoadFromFile ? (cff.AutoloadDirs ?? []) : UI.AutoloadDirs.Value;
            UI.ShownFileTypes.NSP.Value = shouldLoadFromFile ? cff.ShownFileTypes.NSP : UI.ShownFileTypes.NSP.Value;
            UI.ShownFileTypes.PFS0.Value = shouldLoadFromFile ? cff.ShownFileTypes.PFS0 : UI.ShownFileTypes.PFS0.Value;
            UI.ShownFileTypes.XCI.Value = shouldLoadFromFile ? cff.ShownFileTypes.XCI : UI.ShownFileTypes.XCI.Value;
            UI.ShownFileTypes.NCA.Value = shouldLoadFromFile ? cff.ShownFileTypes.NCA : UI.ShownFileTypes.NCA.Value;
            UI.ShownFileTypes.NRO.Value = shouldLoadFromFile ? cff.ShownFileTypes.NRO : UI.ShownFileTypes.NRO.Value;
            UI.ShownFileTypes.NSO.Value = shouldLoadFromFile ? cff.ShownFileTypes.NSO : UI.ShownFileTypes.NSO.Value;
            UI.LanguageCode.Value = shouldLoadFromFile ? cff.LanguageCode : UI.LanguageCode.Value;
            UI.BaseStyle.Value = shouldLoadFromFile ? cff.BaseStyle : UI.BaseStyle.Value;
            UI.GameListViewMode.Value = shouldLoadFromFile ? cff.GameListViewMode : UI.GameListViewMode.Value;
            UI.ShowNames.Value = shouldLoadFromFile ? cff.ShowNames : UI.ShowNames.Value;
            UI.IsAscendingOrder.Value = shouldLoadFromFile ? cff.IsAscendingOrder : UI.IsAscendingOrder.Value;
            UI.GridSize.Value = shouldLoadFromFile ? cff.GridSize : UI.GridSize.Value;
            UI.ApplicationSort.Value = shouldLoadFromFile ? cff.ApplicationSort : UI.ApplicationSort.Value;
            UI.StartFullscreen.Value = shouldLoadFromFile ? cff.StartFullscreen : UI.StartFullscreen.Value;
            UI.StartNoUI.Value = shouldLoadFromFile ? cff.StartNoUI : UI.StartNoUI.Value;
            UI.ShowConsole.Value = shouldLoadFromFile ? cff.ShowConsole : UI.ShowConsole.Value;
            UI.WindowStartup.WindowSizeWidth.Value = shouldLoadFromFile ? cff.WindowStartup.WindowSizeWidth : UI.WindowStartup.WindowSizeWidth.Value;
            UI.WindowStartup.WindowSizeHeight.Value = shouldLoadFromFile ? cff.WindowStartup.WindowSizeHeight : UI.WindowStartup.WindowSizeHeight.Value;
            UI.WindowStartup.WindowPositionX.Value = shouldLoadFromFile ? cff.WindowStartup.WindowPositionX : UI.WindowStartup.WindowPositionX.Value;
            UI.WindowStartup.WindowPositionY.Value = shouldLoadFromFile ? cff.WindowStartup.WindowPositionY : UI.WindowStartup.WindowPositionY.Value;
            UI.WindowStartup.WindowMaximized.Value = shouldLoadFromFile ? cff.WindowStartup.WindowMaximized : UI.WindowStartup.WindowMaximized.Value;


            Hid.EnableKeyboard.Value = cff.EnableKeyboard;
            Hid.EnableMouse.Value = cff.EnableMouse;
            Hid.DisableInputWhenOutOfFocus.Value = shouldLoadFromFile ? cff.DisableInputWhenOutOfFocus: Hid.DisableInputWhenOutOfFocus.Value; // Get from global config only
            Hid.Hotkeys.Value = shouldLoadFromFile ? cff.Hotkeys : Hid.Hotkeys.Value; // Get from global config only
            Hid.InputConfig.Value = cff.InputConfig ?? [];
            Hid.RainbowSpeed.Value = cff.RainbowSpeed;

            Multiplayer.LanInterfaceId.Value = cff.MultiplayerLanInterfaceId;
            Multiplayer.Mode.Value = cff.MultiplayerMode;
            Multiplayer.DisableP2p.Value = cff.MultiplayerDisableP2p;
            Multiplayer.LdnPassphrase.Value = cff.MultiplayerLdnPassphrase;
            Multiplayer.LdnServer.Value = cff.LdnServer;
            
            {
                Hacks.ShowDirtyHacks.Value = cff.ShowDirtyHacks;
                
                DirtyHacks hacks = new (cff.DirtyHacks ?? []);

                Hacks.Xc2MenuSoftlockFix.Value = hacks.IsEnabled(DirtyHack.Xc2MenuSoftlockFix);

                Hacks.EnableShaderTranslationDelay.Value = hacks.IsEnabled(DirtyHack.ShaderTranslationDelay);
                Hacks.ShaderTranslationDelay.Value = hacks[DirtyHack.ShaderTranslationDelay].CoerceAtLeast(0);
            }

            if (configurationFileUpdated)
            {
                ToFileFormat().SaveConfig(configurationFilePath);

                RyuLogger.Notice.Print(LogClass.Application, $"Configuration file updated to version {ConfigurationFileFormat.CurrentVersion}");
            }
        }
        
        private static readonly Dictionary<int, Action<ConfigurationFileFormat>> _migrations =
            Collections.NewDictionary<int, Action<ConfigurationFileFormat>>(
                (2, static cff => cff.SystemRegion = Region.USA),
                (3, static cff => cff.SystemTimeZone = "UTC"),
                (4, static cff => cff.MaxAnisotropy = -1),
                (5, static cff => cff.SystemTimeOffset = 0),
                (8, static cff => cff.EnablePtc = true),
                (9, static cff =>
                {
                    cff.ColumnSort = new ColumnSort { SortColumnId = 0, SortAscending = false };
                    cff.Hotkeys = new KeyboardHotkeys { ToggleVSyncMode = Key.F1 };
                }),
                (10, static cff => cff.AudioBackend = AudioBackend.OpenAl),
                (11, static cff =>
                {
                    cff.ResScale = 1;
                    cff.ResScaleCustom = 1.0f;
                }),
                (12, static cff => cff.LoggingGraphicsDebugLevel = GraphicsDebugLevel.None),
                // 13 -> LDN1
                (14, static cff => cff.CheckUpdatesOnStart = true),
                (16, static cff => cff.EnableShaderCache = true),
                (17, static cff => cff.StartFullscreen = false),
                (18, static cff => cff.AspectRatio = AspectRatio.Fixed16x9),
                // 19 -> LDN2
                (20, static cff => cff.ShowConfirmExit = true),
                (21, static cff =>
                {
                    // Initialize network config.

                    cff.MultiplayerMode = MultiplayerMode.Disabled;
                    cff.MultiplayerLanInterfaceId = "0";
                }),
                (22, static cff => cff.HideCursor = HideCursorMode.Never),
                (24, static cff =>
                {
                    cff.InputConfig =
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
                }),
                (26, static cff => cff.MemoryManagerMode = MemoryManagerMode.HostMappedUnsafe),
                (27, static cff => cff.EnableMouse = false),
                (29,
                    static cff =>
                        cff.Hotkeys = new KeyboardHotkeys
                        {
                            ToggleVSyncMode = Key.F1, Screenshot = Key.F8, ShowUI = Key.F4
                        }),
                (30, static cff =>
                {
                    foreach (StandardControllerInputConfig config in cff.InputConfig.OfType<StandardControllerInputConfig>())
                    {
                        config.Rumble = new RumbleConfigController
                        {
                            EnableRumble = false, StrongRumble = 1f, WeakRumble = 1f,
                        };
                    }
                }),
                (31, static cff => cff.BackendThreading = BackendThreading.Auto),
                (32, static cff => cff.Hotkeys = new KeyboardHotkeys
                {
                    ToggleVSyncMode = cff.Hotkeys.ToggleVSyncMode,
                    Screenshot = cff.Hotkeys.Screenshot,
                    ShowUI = cff.Hotkeys.ShowUI,
                    Pause = Key.F5,
                }),
                (33, static cff =>
                {
                    cff.Hotkeys = new KeyboardHotkeys
                    {
                        ToggleVSyncMode = cff.Hotkeys.ToggleVSyncMode,
                        Screenshot = cff.Hotkeys.Screenshot,
                        ShowUI = cff.Hotkeys.ShowUI,
                        Pause = cff.Hotkeys.Pause,
                        ToggleMute = Key.F2,
                    };

                    cff.AudioVolume = 1;
                }),
                (34, static cff => cff.EnableInternetAccess = false),
                (35, static cff =>
                {
                    foreach (StandardControllerInputConfig config in cff.InputConfig
                                 .OfType<StandardControllerInputConfig>())
                    {
                        config.RangeLeft = 1.0f;
                        config.RangeRight = 1.0f;
                    }
                }),

                (36, static cff => cff.LoggingEnableTrace = false),
                (37, static cff => cff.ShowConsole = true),
                (38, static cff =>
                {
                    cff.BaseStyle = "Dark";
                    cff.GameListViewMode = 0;
                    cff.ShowNames = true;
                    cff.GridSize = 2;
                    cff.LanguageCode = "en_US";
                }),
                (39,
                    static cff => cff.Hotkeys = new KeyboardHotkeys
                    {
                        ToggleVSyncMode = cff.Hotkeys.ToggleVSyncMode,
                        Screenshot = cff.Hotkeys.Screenshot,
                        ShowUI = cff.Hotkeys.ShowUI,
                        Pause = cff.Hotkeys.Pause,
                        ToggleMute = cff.Hotkeys.ToggleMute,
                        ResScaleUp = Key.Unbound,
                        ResScaleDown = Key.Unbound
                    }),
                (40, static cff => cff.GraphicsBackend = GraphicsBackend.OpenGl),
                (41,
                    static cff => cff.Hotkeys = new KeyboardHotkeys
                    {
                        ToggleVSyncMode = cff.Hotkeys.ToggleVSyncMode,
                        Screenshot = cff.Hotkeys.Screenshot,
                        ShowUI = cff.Hotkeys.ShowUI,
                        Pause = cff.Hotkeys.Pause,
                        ToggleMute = cff.Hotkeys.ToggleMute,
                        ResScaleUp = cff.Hotkeys.ResScaleUp,
                        ResScaleDown = cff.Hotkeys.ResScaleDown,
                        VolumeUp = Key.Unbound,
                        VolumeDown = Key.Unbound
                    }),
                (42, static cff => cff.EnableMacroHLE = true),
                (43, static cff => cff.UseHypervisor = true),
                (44, static cff =>
                {
                    cff.AntiAliasing = AntiAliasing.None;
                    cff.ScalingFilter = ScalingFilter.Bilinear;
                    cff.ScalingFilterLevel = 80;
                }),
                (45,
                    static cff => cff.ShownFileTypes = new ShownFileTypes
                    {
                        NSP = true,
                        PFS0 = true,
                        XCI = true,
                        NCA = true,
                        NRO = true,
                        NSO = true
                    }),
                (46, static cff => cff.UseHypervisor = OperatingSystem.IsMacOS()),
                (47,
                    static cff => cff.WindowStartup = new WindowStartup
                    {
                        WindowPositionX = 0,
                        WindowPositionY = 0,
                        WindowSizeHeight = 760,
                        WindowSizeWidth = 1280,
                        WindowMaximized = false
                    }),
                (48, static cff => cff.EnableColorSpacePassthrough = false),
                (49, static _ =>
                {
                    if (OperatingSystem.IsMacOS())
                    {
                        AppDataManager.FixMacOSConfigurationFolders();
                    }
                }),
                (50, static cff => cff.EnableHardwareAcceleration = true),
                (51, static cff => cff.RememberWindowState = true),
                (52, static cff => cff.AutoloadDirs = []),
                (53, static cff => cff.EnableLowPowerPtc = false),
                (54, static cff => cff.DramSize = MemoryConfiguration.MemoryConfiguration4GiB),
                (55, static cff => cff.IgnoreApplet = false),
                (56, static cff => cff.ShowTitleBar = !OperatingSystem.IsWindows()),
                (57, static cff =>
                {
                    cff.VSyncMode = VSyncMode.Switch;
                    cff.EnableCustomVSyncInterval = false;

                    cff.Hotkeys = new KeyboardHotkeys
                    {
                        ToggleVSyncMode = Key.F1,
                        Screenshot = cff.Hotkeys.Screenshot,
                        ShowUI = cff.Hotkeys.ShowUI,
                        Pause = cff.Hotkeys.Pause,
                        ToggleMute = cff.Hotkeys.ToggleMute,
                        ResScaleUp = cff.Hotkeys.ResScaleUp,
                        ResScaleDown = cff.Hotkeys.ResScaleDown,
                        VolumeUp = cff.Hotkeys.VolumeUp,
                        VolumeDown = cff.Hotkeys.VolumeDown,
                        CustomVSyncIntervalIncrement = Key.Unbound,
                        CustomVSyncIntervalDecrement = Key.Unbound,
                    };

                    cff.CustomVSyncInterval = 120;
                }),
                // 58 migration accidentally got skipped, but it worked with no issues somehow lol
                (59, static cff =>
                {
                    cff.ShowDirtyHacks = false;
                    cff.DirtyHacks = [];

                    // This was accidentally enabled by default when it was PRed. That is not what we want,
                    // so as a compromise users who want to use it will simply need to re-enable it once after updating.
                    cff.IgnoreApplet = false;
                }),
                (60, static cff => cff.StartNoUI = false),
                (61, static cff =>
                {
                    foreach (StandardControllerInputConfig config in cff.InputConfig.OfType<StandardControllerInputConfig>())
                    {
                        config.Led = new LedConfigController
                        {
                            EnableLed = false,
                            TurnOffLed = false,
                            UseRainbow = false,
                            LedColor = new Color(255, 5, 1, 253).ToUInt32()
                        };
                    }
                }),
                (62, static cff => cff.RainbowSpeed = 1f),
                (63, static cff => cff.MatchSystemTime = false),
                (64, static cff => cff.LoggingEnableAvalonia = false),
                (65, static cff => cff.UpdateCheckerType = cff.CheckUpdatesOnStart ? UpdaterType.PromptAtStartup : UpdaterType.Off),
                (66, static cff => cff.DisableInputWhenOutOfFocus = false),
                (67, static cff => cff.FocusLostActionType = cff.DisableInputWhenOutOfFocus ? FocusLostType.BlockInput : FocusLostType.DoNothing)
            );
    }
}
