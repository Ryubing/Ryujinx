using Ryujinx.Common.Configuration;
using Ryujinx.Common.Configuration.Hid;
using Ryujinx.Common.Configuration.Hid.Controller;
using Ryujinx.Common.Configuration.Hid.Keyboard;
using Ryujinx.Common.Configuration.Multiplayer;
using Ryujinx.Common.Logging;
using Ryujinx.HLE;
using Ryujinx.UI.Common.Configuration.System;
using Ryujinx.UI.Common.Configuration.UI;
using System;
using System.Collections.Generic;

namespace Ryujinx.UI.Common.Configuration
{
    public partial class ConfigurationState
    {
        public void Load(ConfigurationFileFormat configurationFileFormat, string configurationFilePath)
        {
            bool configurationFileUpdated = false;

            if (configurationFileFormat.Version is < 0 or > ConfigurationFileFormat.CurrentVersion)
            {
                Ryujinx.Common.Logging.Logger.Warning?.Print(LogClass.Application, $"Unsupported configuration version {configurationFileFormat.Version}, loading default.");

                LoadDefault();
            }

            if (configurationFileFormat.Version < 2)
            {
                Ryujinx.Common.Logging.Logger.Warning?.Print(LogClass.Application, $"Outdated configuration version {configurationFileFormat.Version}, migrating to version 2.");

                configurationFileFormat.SystemRegion = Region.USA;

                configurationFileUpdated = true;
            }

            if (configurationFileFormat.Version < 3)
            {
                Ryujinx.Common.Logging.Logger.Warning?.Print(LogClass.Application, $"Outdated configuration version {configurationFileFormat.Version}, migrating to version 3.");

                configurationFileFormat.SystemTimeZone = "UTC";

                configurationFileUpdated = true;
            }

            if (configurationFileFormat.Version < 4)
            {
                Ryujinx.Common.Logging.Logger.Warning?.Print(LogClass.Application, $"Outdated configuration version {configurationFileFormat.Version}, migrating to version 4.");

                configurationFileFormat.MaxAnisotropy = -1;

                configurationFileUpdated = true;
            }

            if (configurationFileFormat.Version < 5)
            {
                Ryujinx.Common.Logging.Logger.Warning?.Print(LogClass.Application, $"Outdated configuration version {configurationFileFormat.Version}, migrating to version 5.");

                configurationFileFormat.SystemTimeOffset = 0;

                configurationFileUpdated = true;
            }

            if (configurationFileFormat.Version < 8)
            {
                Ryujinx.Common.Logging.Logger.Warning?.Print(LogClass.Application, $"Outdated configuration version {configurationFileFormat.Version}, migrating to version 8.");

                configurationFileFormat.EnablePtc = true;

                configurationFileUpdated = true;
            }

            if (configurationFileFormat.Version < 9)
            {
                Ryujinx.Common.Logging.Logger.Warning?.Print(LogClass.Application, $"Outdated configuration version {configurationFileFormat.Version}, migrating to version 9.");

                configurationFileFormat.ColumnSort = new ColumnSort
                {
                    SortColumnId = 0,
                    SortAscending = false,
                };

                configurationFileFormat.Hotkeys = new KeyboardHotkeys
                {
                    ToggleVSyncMode = Key.F1,
                };

                configurationFileUpdated = true;
            }

            if (configurationFileFormat.Version < 10)
            {
                Ryujinx.Common.Logging.Logger.Warning?.Print(LogClass.Application, $"Outdated configuration version {configurationFileFormat.Version}, migrating to version 10.");

                configurationFileFormat.AudioBackend = AudioBackend.OpenAl;

                configurationFileUpdated = true;
            }

            if (configurationFileFormat.Version < 11)
            {
                Ryujinx.Common.Logging.Logger.Warning?.Print(LogClass.Application, $"Outdated configuration version {configurationFileFormat.Version}, migrating to version 11.");

                configurationFileFormat.ResScale = 1;
                configurationFileFormat.ResScaleCustom = 1.0f;

                configurationFileUpdated = true;
            }

            if (configurationFileFormat.Version < 12)
            {
                Ryujinx.Common.Logging.Logger.Warning?.Print(LogClass.Application, $"Outdated configuration version {configurationFileFormat.Version}, migrating to version 12.");

                configurationFileFormat.LoggingGraphicsDebugLevel = GraphicsDebugLevel.None;

                configurationFileUpdated = true;
            }

            // configurationFileFormat.Version == 13 -> LDN1

            if (configurationFileFormat.Version < 14)
            {
                Ryujinx.Common.Logging.Logger.Warning?.Print(LogClass.Application, $"Outdated configuration version {configurationFileFormat.Version}, migrating to version 14.");

                configurationFileFormat.CheckUpdatesOnStart = true;

                configurationFileUpdated = true;
            }

            if (configurationFileFormat.Version < 16)
            {
                Ryujinx.Common.Logging.Logger.Warning?.Print(LogClass.Application, $"Outdated configuration version {configurationFileFormat.Version}, migrating to version 16.");

                configurationFileFormat.EnableShaderCache = true;

                configurationFileUpdated = true;
            }

            if (configurationFileFormat.Version < 17)
            {
                Ryujinx.Common.Logging.Logger.Warning?.Print(LogClass.Application, $"Outdated configuration version {configurationFileFormat.Version}, migrating to version 17.");

                configurationFileFormat.StartFullscreen = false;

                configurationFileUpdated = true;
            }

            if (configurationFileFormat.Version < 18)
            {
                Ryujinx.Common.Logging.Logger.Warning?.Print(LogClass.Application, $"Outdated configuration version {configurationFileFormat.Version}, migrating to version 18.");

                configurationFileFormat.AspectRatio = AspectRatio.Fixed16x9;

                configurationFileUpdated = true;
            }

            // configurationFileFormat.Version == 19 -> LDN2

            if (configurationFileFormat.Version < 20)
            {
                Ryujinx.Common.Logging.Logger.Warning?.Print(LogClass.Application, $"Outdated configuration version {configurationFileFormat.Version}, migrating to version 20.");

                configurationFileFormat.ShowConfirmExit = true;

                configurationFileUpdated = true;
            }

            if (configurationFileFormat.Version < 21)
            {
                Ryujinx.Common.Logging.Logger.Warning?.Print(LogClass.Application, $"Outdated configuration version {configurationFileFormat.Version}, migrating to version 21.");

                // Initialize network config.

                configurationFileFormat.MultiplayerMode = MultiplayerMode.Disabled;
                configurationFileFormat.MultiplayerLanInterfaceId = "0";

                configurationFileUpdated = true;
            }

            if (configurationFileFormat.Version < 22)
            {
                Ryujinx.Common.Logging.Logger.Warning?.Print(LogClass.Application, $"Outdated configuration version {configurationFileFormat.Version}, migrating to version 22.");

                configurationFileFormat.HideCursor = HideCursorMode.Never;

                configurationFileUpdated = true;
            }

            if (configurationFileFormat.Version < 24)
            {
                Ryujinx.Common.Logging.Logger.Warning?.Print(LogClass.Application, $"Outdated configuration version {configurationFileFormat.Version}, migrating to version 24.");

                configurationFileFormat.InputConfig = new List<InputConfig>
                {
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
                    },
                };

                configurationFileUpdated = true;
            }

            if (configurationFileFormat.Version < 25)
            {
                Ryujinx.Common.Logging.Logger.Warning?.Print(LogClass.Application, $"Outdated configuration version {configurationFileFormat.Version}, migrating to version 25.");

                configurationFileUpdated = true;
            }

            if (configurationFileFormat.Version < 26)
            {
                Ryujinx.Common.Logging.Logger.Warning?.Print(LogClass.Application, $"Outdated configuration version {configurationFileFormat.Version}, migrating to version 26.");

                configurationFileFormat.MemoryManagerMode = MemoryManagerMode.HostMappedUnsafe;

                configurationFileUpdated = true;
            }

            if (configurationFileFormat.Version < 27)
            {
                Ryujinx.Common.Logging.Logger.Warning?.Print(LogClass.Application, $"Outdated configuration version {configurationFileFormat.Version}, migrating to version 27.");

                configurationFileFormat.EnableMouse = false;

                configurationFileUpdated = true;
            }

            if (configurationFileFormat.Version < 28)
            {
                Ryujinx.Common.Logging.Logger.Warning?.Print(LogClass.Application, $"Outdated configuration version {configurationFileFormat.Version}, migrating to version 28.");

                configurationFileFormat.Hotkeys = new KeyboardHotkeys
                {
                    ToggleVSyncMode = Key.F1,
                    Screenshot = Key.F8,
                };

                configurationFileUpdated = true;
            }

            if (configurationFileFormat.Version < 29)
            {
                Ryujinx.Common.Logging.Logger.Warning?.Print(LogClass.Application, $"Outdated configuration version {configurationFileFormat.Version}, migrating to version 29.");

                configurationFileFormat.Hotkeys = new KeyboardHotkeys
                {
                    ToggleVSyncMode = Key.F1,
                    Screenshot = Key.F8,
                    ShowUI = Key.F4,
                };

                configurationFileUpdated = true;
            }

            if (configurationFileFormat.Version < 30)
            {
                Ryujinx.Common.Logging.Logger.Warning?.Print(LogClass.Application, $"Outdated configuration version {configurationFileFormat.Version}, migrating to version 30.");

                foreach (InputConfig config in configurationFileFormat.InputConfig)
                {
                    if (config is StandardControllerInputConfig controllerConfig)
                    {
                        controllerConfig.Rumble = new RumbleConfigController
                        {
                            EnableRumble = false,
                            StrongRumble = 1f,
                            WeakRumble = 1f,
                        };
                    }
                }

                configurationFileUpdated = true;
            }

            if (configurationFileFormat.Version < 31)
            {
                Ryujinx.Common.Logging.Logger.Warning?.Print(LogClass.Application, $"Outdated configuration version {configurationFileFormat.Version}, migrating to version 31.");

                configurationFileFormat.BackendThreading = BackendThreading.Auto;

                configurationFileUpdated = true;
            }

            if (configurationFileFormat.Version < 32)
            {
                Ryujinx.Common.Logging.Logger.Warning?.Print(LogClass.Application, $"Outdated configuration version {configurationFileFormat.Version}, migrating to version 32.");

                configurationFileFormat.Hotkeys = new KeyboardHotkeys
                {
                    ToggleVSyncMode = configurationFileFormat.Hotkeys.ToggleVSyncMode,
                    Screenshot = configurationFileFormat.Hotkeys.Screenshot,
                    ShowUI = configurationFileFormat.Hotkeys.ShowUI,
                    Pause = Key.F5,
                };

                configurationFileUpdated = true;
            }

            if (configurationFileFormat.Version < 33)
            {
                Ryujinx.Common.Logging.Logger.Warning?.Print(LogClass.Application, $"Outdated configuration version {configurationFileFormat.Version}, migrating to version 33.");

                configurationFileFormat.Hotkeys = new KeyboardHotkeys
                {
                    ToggleVSyncMode = configurationFileFormat.Hotkeys.ToggleVSyncMode,
                    Screenshot = configurationFileFormat.Hotkeys.Screenshot,
                    ShowUI = configurationFileFormat.Hotkeys.ShowUI,
                    Pause = configurationFileFormat.Hotkeys.Pause,
                    ToggleMute = Key.F2,
                };

                configurationFileFormat.AudioVolume = 1;

                configurationFileUpdated = true;
            }

            if (configurationFileFormat.Version < 34)
            {
                Ryujinx.Common.Logging.Logger.Warning?.Print(LogClass.Application, $"Outdated configuration version {configurationFileFormat.Version}, migrating to version 34.");

                configurationFileFormat.EnableInternetAccess = false;

                configurationFileUpdated = true;
            }

            if (configurationFileFormat.Version < 35)
            {
                Ryujinx.Common.Logging.Logger.Warning?.Print(LogClass.Application, $"Outdated configuration version {configurationFileFormat.Version}, migrating to version 35.");

                foreach (InputConfig config in configurationFileFormat.InputConfig)
                {
                    if (config is StandardControllerInputConfig controllerConfig)
                    {
                        controllerConfig.RangeLeft = 1.0f;
                        controllerConfig.RangeRight = 1.0f;
                    }
                }

                configurationFileUpdated = true;
            }

            if (configurationFileFormat.Version < 36)
            {
                Ryujinx.Common.Logging.Logger.Warning?.Print(LogClass.Application, $"Outdated configuration version {configurationFileFormat.Version}, migrating to version 36.");

                configurationFileFormat.LoggingEnableTrace = false;

                configurationFileUpdated = true;
            }

            if (configurationFileFormat.Version < 37)
            {
                Ryujinx.Common.Logging.Logger.Warning?.Print(LogClass.Application, $"Outdated configuration version {configurationFileFormat.Version}, migrating to version 37.");

                configurationFileFormat.ShowConsole = true;

                configurationFileUpdated = true;
            }

            if (configurationFileFormat.Version < 38)
            {
                Ryujinx.Common.Logging.Logger.Warning?.Print(LogClass.Application, $"Outdated configuration version {configurationFileFormat.Version}, migrating to version 38.");

                configurationFileFormat.BaseStyle = "Dark";
                configurationFileFormat.GameListViewMode = 0;
                configurationFileFormat.ShowNames = true;
                configurationFileFormat.GridSize = 2;
                configurationFileFormat.LanguageCode = "en_US";

                configurationFileUpdated = true;
            }

            if (configurationFileFormat.Version < 39)
            {
                Ryujinx.Common.Logging.Logger.Warning?.Print(LogClass.Application, $"Outdated configuration version {configurationFileFormat.Version}, migrating to version 39.");

                configurationFileFormat.Hotkeys = new KeyboardHotkeys
                {
                    ToggleVSyncMode = configurationFileFormat.Hotkeys.ToggleVSyncMode,
                    Screenshot = configurationFileFormat.Hotkeys.Screenshot,
                    ShowUI = configurationFileFormat.Hotkeys.ShowUI,
                    Pause = configurationFileFormat.Hotkeys.Pause,
                    ToggleMute = configurationFileFormat.Hotkeys.ToggleMute,
                    ResScaleUp = Key.Unbound,
                    ResScaleDown = Key.Unbound,
                };

                configurationFileUpdated = true;
            }

            if (configurationFileFormat.Version < 40)
            {
                Ryujinx.Common.Logging.Logger.Warning?.Print(LogClass.Application, $"Outdated configuration version {configurationFileFormat.Version}, migrating to version 40.");

                configurationFileFormat.GraphicsBackend = GraphicsBackend.OpenGl;

                configurationFileUpdated = true;
            }

            if (configurationFileFormat.Version < 41)
            {
                Ryujinx.Common.Logging.Logger.Warning?.Print(LogClass.Application, $"Outdated configuration version {configurationFileFormat.Version}, migrating to version 41.");

                configurationFileFormat.Hotkeys = new KeyboardHotkeys
                {
                    ToggleVSyncMode = configurationFileFormat.Hotkeys.ToggleVSyncMode,
                    Screenshot = configurationFileFormat.Hotkeys.Screenshot,
                    ShowUI = configurationFileFormat.Hotkeys.ShowUI,
                    Pause = configurationFileFormat.Hotkeys.Pause,
                    ToggleMute = configurationFileFormat.Hotkeys.ToggleMute,
                    ResScaleUp = configurationFileFormat.Hotkeys.ResScaleUp,
                    ResScaleDown = configurationFileFormat.Hotkeys.ResScaleDown,
                    VolumeUp = Key.Unbound,
                    VolumeDown = Key.Unbound,
                };
            }

            if (configurationFileFormat.Version < 42)
            {
                Ryujinx.Common.Logging.Logger.Warning?.Print(LogClass.Application, $"Outdated configuration version {configurationFileFormat.Version}, migrating to version 42.");

                configurationFileFormat.EnableMacroHLE = true;
            }

            if (configurationFileFormat.Version < 43)
            {
                Ryujinx.Common.Logging.Logger.Warning?.Print(LogClass.Application, $"Outdated configuration version {configurationFileFormat.Version}, migrating to version 43.");

                configurationFileFormat.UseHypervisor = true;
            }

            if (configurationFileFormat.Version < 44)
            {
                Ryujinx.Common.Logging.Logger.Warning?.Print(LogClass.Application, $"Outdated configuration version {configurationFileFormat.Version}, migrating to version 44.");

                configurationFileFormat.AntiAliasing = AntiAliasing.None;
                configurationFileFormat.ScalingFilter = ScalingFilter.Bilinear;
                configurationFileFormat.ScalingFilterLevel = 80;

                configurationFileUpdated = true;
            }

            if (configurationFileFormat.Version < 45)
            {
                Ryujinx.Common.Logging.Logger.Warning?.Print(LogClass.Application, $"Outdated configuration version {configurationFileFormat.Version}, migrating to version 45.");

                configurationFileFormat.ShownFileTypes = new ShownFileTypes
                {
                    NSP = true,
                    PFS0 = true,
                    XCI = true,
                    NCA = true,
                    NRO = true,
                    NSO = true,
                };

                configurationFileUpdated = true;
            }

            if (configurationFileFormat.Version < 46)
            {
                Ryujinx.Common.Logging.Logger.Warning?.Print(LogClass.Application, $"Outdated configuration version {configurationFileFormat.Version}, migrating to version 46.");

                configurationFileFormat.MultiplayerLanInterfaceId = "0";

                configurationFileUpdated = true;
            }

            if (configurationFileFormat.Version < 47)
            {
                Ryujinx.Common.Logging.Logger.Warning?.Print(LogClass.Application, $"Outdated configuration version {configurationFileFormat.Version}, migrating to version 47.");

                configurationFileFormat.WindowStartup = new WindowStartup
                {
                    WindowPositionX = 0,
                    WindowPositionY = 0,
                    WindowSizeHeight = 760,
                    WindowSizeWidth = 1280,
                    WindowMaximized = false,
                };

                configurationFileUpdated = true;
            }

            if (configurationFileFormat.Version < 48)
            {
                Ryujinx.Common.Logging.Logger.Warning?.Print(LogClass.Application, $"Outdated configuration version {configurationFileFormat.Version}, migrating to version 48.");

                configurationFileFormat.EnableColorSpacePassthrough = false;

                configurationFileUpdated = true;
            }

            if (configurationFileFormat.Version < 49)
            {
                Ryujinx.Common.Logging.Logger.Warning?.Print(LogClass.Application, $"Outdated configuration version {configurationFileFormat.Version}, migrating to version 49.");

                if (OperatingSystem.IsMacOS())
                {
                    AppDataManager.FixMacOSConfigurationFolders();
                }

                configurationFileUpdated = true;
            }

            if (configurationFileFormat.Version < 50)
            {
                Ryujinx.Common.Logging.Logger.Warning?.Print(LogClass.Application, $"Outdated configuration version {configurationFileFormat.Version}, migrating to version 50.");

                configurationFileFormat.EnableHardwareAcceleration = true;

                configurationFileUpdated = true;
            }

            if (configurationFileFormat.Version < 51)
            {
                Ryujinx.Common.Logging.Logger.Warning?.Print(LogClass.Application, $"Outdated configuration version {configurationFileFormat.Version}, migrating to version 51.");

                configurationFileFormat.RememberWindowState = true;

                configurationFileUpdated = true;
            }

            if (configurationFileFormat.Version < 52)
            {
                Ryujinx.Common.Logging.Logger.Warning?.Print(LogClass.Application, $"Outdated configuration version {configurationFileFormat.Version}, migrating to version 52.");

                configurationFileFormat.AutoloadDirs = [];

                configurationFileUpdated = true;
            }

            if (configurationFileFormat.Version < 53)
            {
                Ryujinx.Common.Logging.Logger.Warning?.Print(LogClass.Application, $"Outdated configuration version {configurationFileFormat.Version}, migrating to version 53.");

                configurationFileFormat.EnableLowPowerPtc = false;

                configurationFileUpdated = true;
            }

            if (configurationFileFormat.Version < 54)
            {
                Ryujinx.Common.Logging.Logger.Warning?.Print(LogClass.Application, $"Outdated configuration version {configurationFileFormat.Version}, migrating to version 54.");

                configurationFileFormat.DramSize = MemoryConfiguration.MemoryConfiguration4GiB;

                configurationFileUpdated = true;
            }

            if (configurationFileFormat.Version < 55)
            {
                Ryujinx.Common.Logging.Logger.Warning?.Print(LogClass.Application, $"Outdated configuration version {configurationFileFormat.Version}, migrating to version 55.");

                configurationFileFormat.IgnoreApplet = false;

                configurationFileUpdated = true;
            }

            if (configurationFileFormat.Version < 56)
            {
                Ryujinx.Common.Logging.Logger.Warning?.Print(LogClass.Application, $"Outdated configuration version {configurationFileFormat.Version}, migrating to version 56.");

                configurationFileFormat.ShowTitleBar = !OperatingSystem.IsWindows();

                configurationFileUpdated = true;
            }

            if (configurationFileFormat.Version < 57)
            {
                Ryujinx.Common.Logging.Logger.Warning?.Print(LogClass.Application, $"Outdated configuration version {configurationFileFormat.Version}, migrating to version 57.");

                configurationFileFormat.VSyncMode = VSyncMode.Switch;
                configurationFileFormat.EnableCustomVSyncInterval = false;

                configurationFileFormat.Hotkeys = new KeyboardHotkeys
                {
                    ToggleVSyncMode = Key.F1,
                    Screenshot = configurationFileFormat.Hotkeys.Screenshot,
                    ShowUI = configurationFileFormat.Hotkeys.ShowUI,
                    Pause = configurationFileFormat.Hotkeys.Pause,
                    ToggleMute = configurationFileFormat.Hotkeys.ToggleMute,
                    ResScaleUp = configurationFileFormat.Hotkeys.ResScaleUp,
                    ResScaleDown = configurationFileFormat.Hotkeys.ResScaleDown,
                    VolumeUp = configurationFileFormat.Hotkeys.VolumeUp,
                    VolumeDown = configurationFileFormat.Hotkeys.VolumeDown,
                    CustomVSyncIntervalIncrement = Key.Unbound,
                    CustomVSyncIntervalDecrement = Key.Unbound,
                };

                configurationFileFormat.CustomVSyncInterval = 120;

                configurationFileUpdated = true;
            }

            Logger.EnableFileLog.Value = configurationFileFormat.EnableFileLog;
            Graphics.ResScale.Value = configurationFileFormat.ResScale;
            Graphics.ResScaleCustom.Value = configurationFileFormat.ResScaleCustom;
            Graphics.MaxAnisotropy.Value = configurationFileFormat.MaxAnisotropy;
            Graphics.AspectRatio.Value = configurationFileFormat.AspectRatio;
            Graphics.ShadersDumpPath.Value = configurationFileFormat.GraphicsShadersDumpPath;
            Graphics.BackendThreading.Value = configurationFileFormat.BackendThreading;
            Graphics.GraphicsBackend.Value = configurationFileFormat.GraphicsBackend;
            Graphics.PreferredGpu.Value = configurationFileFormat.PreferredGpu;
            Graphics.AntiAliasing.Value = configurationFileFormat.AntiAliasing;
            Graphics.ScalingFilter.Value = configurationFileFormat.ScalingFilter;
            Graphics.ScalingFilterLevel.Value = configurationFileFormat.ScalingFilterLevel;
            Logger.EnableDebug.Value = configurationFileFormat.LoggingEnableDebug;
            Logger.EnableStub.Value = configurationFileFormat.LoggingEnableStub;
            Logger.EnableInfo.Value = configurationFileFormat.LoggingEnableInfo;
            Logger.EnableWarn.Value = configurationFileFormat.LoggingEnableWarn;
            Logger.EnableError.Value = configurationFileFormat.LoggingEnableError;
            Logger.EnableTrace.Value = configurationFileFormat.LoggingEnableTrace;
            Logger.EnableGuest.Value = configurationFileFormat.LoggingEnableGuest;
            Logger.EnableFsAccessLog.Value = configurationFileFormat.LoggingEnableFsAccessLog;
            Logger.FilteredClasses.Value = configurationFileFormat.LoggingFilteredClasses;
            Logger.GraphicsDebugLevel.Value = configurationFileFormat.LoggingGraphicsDebugLevel;
            System.Language.Value = configurationFileFormat.SystemLanguage;
            System.Region.Value = configurationFileFormat.SystemRegion;
            System.TimeZone.Value = configurationFileFormat.SystemTimeZone;
            System.SystemTimeOffset.Value = configurationFileFormat.SystemTimeOffset;
            System.EnableDockedMode.Value = configurationFileFormat.DockedMode;
            EnableDiscordIntegration.Value = configurationFileFormat.EnableDiscordIntegration;
            CheckUpdatesOnStart.Value = configurationFileFormat.CheckUpdatesOnStart;
            ShowConfirmExit.Value = configurationFileFormat.ShowConfirmExit;
            IgnoreApplet.Value = configurationFileFormat.IgnoreApplet;
            RememberWindowState.Value = configurationFileFormat.RememberWindowState;
            ShowTitleBar.Value = configurationFileFormat.ShowTitleBar;
            EnableHardwareAcceleration.Value = configurationFileFormat.EnableHardwareAcceleration;
            HideCursor.Value = configurationFileFormat.HideCursor;
            Graphics.VSyncMode.Value = configurationFileFormat.VSyncMode;
            Graphics.EnableCustomVSyncInterval.Value = configurationFileFormat.EnableCustomVSyncInterval;
            Graphics.CustomVSyncInterval.Value = configurationFileFormat.CustomVSyncInterval;
            Graphics.EnableShaderCache.Value = configurationFileFormat.EnableShaderCache;
            Graphics.EnableTextureRecompression.Value = configurationFileFormat.EnableTextureRecompression;
            Graphics.EnableMacroHLE.Value = configurationFileFormat.EnableMacroHLE;
            Graphics.EnableColorSpacePassthrough.Value = configurationFileFormat.EnableColorSpacePassthrough;
            System.EnablePtc.Value = configurationFileFormat.EnablePtc;
            System.EnableLowPowerPtc.Value = configurationFileFormat.EnableLowPowerPtc;
            System.EnableInternetAccess.Value = configurationFileFormat.EnableInternetAccess;
            System.EnableFsIntegrityChecks.Value = configurationFileFormat.EnableFsIntegrityChecks;
            System.FsGlobalAccessLogMode.Value = configurationFileFormat.FsGlobalAccessLogMode;
            System.AudioBackend.Value = configurationFileFormat.AudioBackend;
            System.AudioVolume.Value = configurationFileFormat.AudioVolume;
            System.MemoryManagerMode.Value = configurationFileFormat.MemoryManagerMode;
            System.DramSize.Value = configurationFileFormat.DramSize;
            System.IgnoreMissingServices.Value = configurationFileFormat.IgnoreMissingServices;
            System.UseHypervisor.Value = configurationFileFormat.UseHypervisor;
            UI.GuiColumns.FavColumn.Value = configurationFileFormat.GuiColumns.FavColumn;
            UI.GuiColumns.IconColumn.Value = configurationFileFormat.GuiColumns.IconColumn;
            UI.GuiColumns.AppColumn.Value = configurationFileFormat.GuiColumns.AppColumn;
            UI.GuiColumns.DevColumn.Value = configurationFileFormat.GuiColumns.DevColumn;
            UI.GuiColumns.VersionColumn.Value = configurationFileFormat.GuiColumns.VersionColumn;
            UI.GuiColumns.TimePlayedColumn.Value = configurationFileFormat.GuiColumns.TimePlayedColumn;
            UI.GuiColumns.LastPlayedColumn.Value = configurationFileFormat.GuiColumns.LastPlayedColumn;
            UI.GuiColumns.FileExtColumn.Value = configurationFileFormat.GuiColumns.FileExtColumn;
            UI.GuiColumns.FileSizeColumn.Value = configurationFileFormat.GuiColumns.FileSizeColumn;
            UI.GuiColumns.PathColumn.Value = configurationFileFormat.GuiColumns.PathColumn;
            UI.ColumnSort.SortColumnId.Value = configurationFileFormat.ColumnSort.SortColumnId;
            UI.ColumnSort.SortAscending.Value = configurationFileFormat.ColumnSort.SortAscending;
            UI.GameDirs.Value = configurationFileFormat.GameDirs;
            UI.AutoloadDirs.Value = configurationFileFormat.AutoloadDirs ?? [];
            UI.ShownFileTypes.NSP.Value = configurationFileFormat.ShownFileTypes.NSP;
            UI.ShownFileTypes.PFS0.Value = configurationFileFormat.ShownFileTypes.PFS0;
            UI.ShownFileTypes.XCI.Value = configurationFileFormat.ShownFileTypes.XCI;
            UI.ShownFileTypes.NCA.Value = configurationFileFormat.ShownFileTypes.NCA;
            UI.ShownFileTypes.NRO.Value = configurationFileFormat.ShownFileTypes.NRO;
            UI.ShownFileTypes.NSO.Value = configurationFileFormat.ShownFileTypes.NSO;
            UI.LanguageCode.Value = configurationFileFormat.LanguageCode;
            UI.BaseStyle.Value = configurationFileFormat.BaseStyle;
            UI.GameListViewMode.Value = configurationFileFormat.GameListViewMode;
            UI.ShowNames.Value = configurationFileFormat.ShowNames;
            UI.IsAscendingOrder.Value = configurationFileFormat.IsAscendingOrder;
            UI.GridSize.Value = configurationFileFormat.GridSize;
            UI.ApplicationSort.Value = configurationFileFormat.ApplicationSort;
            UI.StartFullscreen.Value = configurationFileFormat.StartFullscreen;
            UI.ShowConsole.Value = configurationFileFormat.ShowConsole;
            UI.WindowStartup.WindowSizeWidth.Value = configurationFileFormat.WindowStartup.WindowSizeWidth;
            UI.WindowStartup.WindowSizeHeight.Value = configurationFileFormat.WindowStartup.WindowSizeHeight;
            UI.WindowStartup.WindowPositionX.Value = configurationFileFormat.WindowStartup.WindowPositionX;
            UI.WindowStartup.WindowPositionY.Value = configurationFileFormat.WindowStartup.WindowPositionY;
            UI.WindowStartup.WindowMaximized.Value = configurationFileFormat.WindowStartup.WindowMaximized;
            Hid.EnableKeyboard.Value = configurationFileFormat.EnableKeyboard;
            Hid.EnableMouse.Value = configurationFileFormat.EnableMouse;
            Hid.Hotkeys.Value = configurationFileFormat.Hotkeys;
            Hid.InputConfig.Value = configurationFileFormat.InputConfig ?? [];

            Multiplayer.LanInterfaceId.Value = configurationFileFormat.MultiplayerLanInterfaceId;
            Multiplayer.Mode.Value = configurationFileFormat.MultiplayerMode;
            Multiplayer.DisableP2p.Value = configurationFileFormat.MultiplayerDisableP2p;
            Multiplayer.LdnPassphrase.Value = configurationFileFormat.MultiplayerLdnPassphrase;
            Multiplayer.LdnServer.Value = configurationFileFormat.LdnServer;

            if (configurationFileUpdated)
            {
                ToFileFormat().SaveConfig(configurationFilePath);

                Ryujinx.Common.Logging.Logger.Notice.Print(LogClass.Application, $"Configuration file updated to version {ConfigurationFileFormat.CurrentVersion}");
            }
        }
    }
}
