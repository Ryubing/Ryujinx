using CommandLine;
using Gommon;
using Ryujinx.Ava.Systems.Configuration;
using Ryujinx.Ava.Systems.Configuration.System;
using Ryujinx.Common.Configuration;
using Ryujinx.Common.Configuration.Hid;
using Ryujinx.HLE;
using Ryujinx.HLE.HOS.SystemState;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Ryujinx.Headless
{
    public class Options
    {
        public void InheritMainConfig(string[] originalArgs, ConfigurationState configurationState, out bool needsProfileSet)
        {
            needsProfileSet = NeedsOverride(nameof(UserProfile));

            if (NeedsOverride(nameof(IsFullscreen)))
                IsFullscreen = configurationState.UI.StartFullscreen;

            if (NeedsOverride(nameof(EnableKeyboard)))
                EnableKeyboard = configurationState.Hid.EnableKeyboard;
            
            if (NeedsOverride(nameof(EnableMouse)))
                EnableMouse = configurationState.Hid.EnableMouse;

            if (NeedsOverride(nameof(HideCursorMode)))
                HideCursorMode = configurationState.HideCursor;

            if (NeedsOverride(nameof(DisablePTC)))
                DisablePTC = !configurationState.System.EnablePtc;

            if (NeedsOverride(nameof(EnableInternetAccess)))
                EnableInternetAccess = configurationState.System.EnableInternetAccess;

            if (NeedsOverride(nameof(DisableFsIntegrityChecks)))
                DisableFsIntegrityChecks = !configurationState.System.EnableFsIntegrityChecks;
            
            if (NeedsOverride(nameof(FsGlobalAccessLogMode)))
                FsGlobalAccessLogMode = configurationState.System.FsGlobalAccessLogMode;
            
            if (NeedsOverride(nameof(VSyncMode)))
                VSyncMode = configurationState.Graphics.VSyncMode;
            
            if (NeedsOverride(nameof(CustomVSyncInterval)))
                CustomVSyncInterval = configurationState.Graphics.CustomVSyncInterval;
            
            if (NeedsOverride(nameof(DisableShaderCache)))
                DisableShaderCache = !configurationState.Graphics.EnableShaderCache;

            if (NeedsOverride(nameof(EnableTextureRecompression)))
                EnableTextureRecompression = configurationState.Graphics.EnableTextureRecompression;
            
            if (NeedsOverride(nameof(DisableDockedMode)))
                DisableDockedMode = !configurationState.System.EnableDockedMode;

            if (NeedsOverride(nameof(SystemLanguage)))
                SystemLanguage = configurationState.System.Language.Value.ToHLE();
            
            if (NeedsOverride(nameof(SystemRegion)))
                SystemRegion = configurationState.System.Region.Value.ToHLE();
            
            if (NeedsOverride(nameof(SystemTimeZone)))
                SystemTimeZone = configurationState.System.TimeZone;
            
            if (NeedsOverride(nameof(SystemTimeOffset)))
                SystemTimeOffset = configurationState.System.SystemTimeOffset;
            
            if (NeedsOverride(nameof(MemoryManagerMode)))
                MemoryManagerMode = configurationState.System.MemoryManagerMode;
            
            if (NeedsOverride(nameof(AudioVolume)))
                AudioVolume = configurationState.System.AudioVolume;

            if (NeedsOverride(nameof(UseHypervisor)) && OperatingSystem.IsMacOS())
                UseHypervisor = configurationState.System.UseHypervisor;

            if (NeedsOverride(nameof(MultiplayerLanInterfaceId)))
                MultiplayerLanInterfaceId = configurationState.Multiplayer.LanInterfaceId;
            
            if (NeedsOverride(nameof(DisableFileLog)))
                DisableFileLog = !configurationState.Logger.EnableFileLog;
            
            if (NeedsOverride(nameof(LoggingEnableDebug)))
                LoggingEnableDebug = configurationState.Logger.EnableDebug;
            
            if (NeedsOverride(nameof(LoggingDisableStub)))
                LoggingDisableStub = !configurationState.Logger.EnableStub;
            
            if (NeedsOverride(nameof(LoggingDisableInfo)))
                LoggingDisableInfo = !configurationState.Logger.EnableInfo;
            
            if (NeedsOverride(nameof(LoggingDisableWarning)))
                LoggingDisableWarning = !configurationState.Logger.EnableWarn;
            
            if (NeedsOverride(nameof(LoggingDisableError)))
                LoggingDisableError = !configurationState.Logger.EnableError;
            
            if (NeedsOverride(nameof(LoggingEnableTrace)))
                LoggingEnableTrace = configurationState.Logger.EnableTrace;
            
            if (NeedsOverride(nameof(LoggingDisableGuest)))
                LoggingDisableGuest = !configurationState.Logger.EnableGuest;

            if (NeedsOverride(nameof(LoggingEnableFsAccessLog)))
                LoggingEnableFsAccessLog = configurationState.Logger.EnableFsAccessLog;

            if (NeedsOverride(nameof(LoggingGraphicsDebugLevel)))
                LoggingGraphicsDebugLevel = configurationState.Logger.GraphicsDebugLevel;

            if (NeedsOverride(nameof(ResScale)))
                ResScale = configurationState.Graphics.ResScale;
            
            if (NeedsOverride(nameof(MaxAnisotropy)))
                MaxAnisotropy = configurationState.Graphics.MaxAnisotropy;
            
            if (NeedsOverride(nameof(AspectRatio)))
                AspectRatio = configurationState.Graphics.AspectRatio;
            
            if (NeedsOverride(nameof(BackendThreading)))
                BackendThreading = configurationState.Graphics.BackendThreading;
            
            if (NeedsOverride(nameof(DisableMacroHLE)))
                DisableMacroHLE = !configurationState.Graphics.EnableMacroHLE;
            
            if (NeedsOverride(nameof(GraphicsShadersDumpPath)))
                GraphicsShadersDumpPath = configurationState.Graphics.ShadersDumpPath;
            
            if (NeedsOverride(nameof(GraphicsBackend)))
                GraphicsBackend = configurationState.Graphics.GraphicsBackend;
            
            if (NeedsOverride(nameof(AntiAliasing)))
                AntiAliasing = configurationState.Graphics.AntiAliasing;
            
            if (NeedsOverride(nameof(ScalingFilter)))
                ScalingFilter = configurationState.Graphics.ScalingFilter;
            
            if (NeedsOverride(nameof(ScalingFilterLevel)))
                ScalingFilterLevel = configurationState.Graphics.ScalingFilterLevel;

            if (NeedsOverride(nameof(DramSize)))
                DramSize = configurationState.System.DramSize;
            
            if (NeedsOverride(nameof(IgnoreMissingServices)))
                IgnoreMissingServices = configurationState.System.IgnoreMissingServices;
            
            if (NeedsOverride(nameof(IgnoreControllerApplet)))
                IgnoreControllerApplet = configurationState.System.IgnoreControllerApplet;
            
            return;
            
            bool NeedsOverride(string argKey) => originalArgs.None(arg => arg.TrimStart('-').EqualsIgnoreCase(OptionName(argKey)));
        }

        public void InheritMainConfigInput(string[] originalArgs, ConfigurationState configurationState)
        {
            Dictionary<PlayerIndex, (string InputId, string InputProfileName)> indicesToProperties = new()
            {
                { PlayerIndex.Handheld, (nameof(InputIdHandheld), nameof(InputProfileHandheldName)) },
                { PlayerIndex.Player1, (nameof(InputId1), nameof(InputProfile1Name)) },
                { PlayerIndex.Player2, (nameof(InputId2), nameof(InputProfile2Name)) },
                { PlayerIndex.Player3, (nameof(InputId3), nameof(InputProfile3Name)) },
                { PlayerIndex.Player4, (nameof(InputId4), nameof(InputProfile4Name)) },
                { PlayerIndex.Player5, (nameof(InputId5), nameof(InputProfile5Name)) },
                { PlayerIndex.Player6, (nameof(InputId6), nameof(InputProfile6Name)) },
                { PlayerIndex.Player7, (nameof(InputId7), nameof(InputProfile7Name)) },
                { PlayerIndex.Player8, (nameof(InputId8), nameof(InputProfile8Name)) }
            };

            foreach ((PlayerIndex playerIndex, _) in indicesToProperties
                         .Where(it => NeedsOverride(it.Value.InputId) && NeedsOverride(it.Value.InputProfileName)))
            {
                configurationState.Hid.InputConfig.Value.FindFirst(x => x.PlayerIndex == playerIndex)
                    .IfPresent(ic => InheritedInputConfigs[playerIndex] = ic);
            }

            return;
            
            bool NeedsOverride(string argKey) => originalArgs.None(arg => arg.TrimStart('-').EqualsIgnoreCase(OptionName(argKey)));
        }

        private static string OptionName(string propertyName) =>
            typeof(Options)!.GetProperty(propertyName)!.GetCustomAttribute<OptionAttribute>()!.LongName;
        
        // General
        
        [Option("use-main-config", Required = false, Default = false, HelpText = "Use the settings from what was configured via the UI.")]
        public bool InheritConfig { get; set; }

        [Option("root-data-dir", Required = false, HelpText = "Set the custom folder path for Ryujinx data.")]
        public string BaseDataDir { get; set; }

        [Option("profile", Required = false, HelpText = "Set the user profile to launch the game with.")]
        public string UserProfile { get; set; }

        [Option("display-id", Required = false, Default = 0, HelpText = "Set the display to use - especially helpful for fullscreen mode. [0-n]")]
        public int DisplayId { get; set; }

        [Option("fullscreen", Required = false, Default = false, HelpText = "Launch the game in fullscreen mode.")]
        public bool IsFullscreen { get; set; }

        [Option("exclusive-fullscreen", Required = false, Default = false, HelpText = "Launch the game in exclusive fullscreen mode.")]
        public bool IsExclusiveFullscreen { get; set; }

        [Option("exclusive-fullscreen-width", Required = false, Default = 1920, HelpText = "Set horizontal resolution for exclusive fullscreen mode.")]
        public int ExclusiveFullscreenWidth { get; set; }

        [Option("exclusive-fullscreen-height", Required = false, Default = 1080, HelpText = "Set vertical resolution for exclusive fullscreen mode.")]
        public int ExclusiveFullscreenHeight { get; set; }

        // Input

        [Option("input-profile-1", Required = false, HelpText = "Set the input profile in use for Player 1.")]
        public string InputProfile1Name { get; set; }

        [Option("input-profile-2", Required = false, HelpText = "Set the input profile in use for Player 2.")]
        public string InputProfile2Name { get; set; }

        [Option("input-profile-3", Required = false, HelpText = "Set the input profile in use for Player 3.")]
        public string InputProfile3Name { get; set; }

        [Option("input-profile-4", Required = false, HelpText = "Set the input profile in use for Player 4.")]
        public string InputProfile4Name { get; set; }

        [Option("input-profile-5", Required = false, HelpText = "Set the input profile in use for Player 5.")]
        public string InputProfile5Name { get; set; }

        [Option("input-profile-6", Required = false, HelpText = "Set the input profile in use for Player 6.")]
        public string InputProfile6Name { get; set; }

        [Option("input-profile-7", Required = false, HelpText = "Set the input profile in use for Player 7.")]
        public string InputProfile7Name { get; set; }

        [Option("input-profile-8", Required = false, HelpText = "Set the input profile in use for Player 8.")]
        public string InputProfile8Name { get; set; }

        [Option("input-profile-handheld", Required = false, HelpText = "Set the input profile in use for the Handheld Player.")]
        public string InputProfileHandheldName { get; set; }

        [Option("input-id-1", Required = false, HelpText = "Set the input id in use for Player 1.")]
        public string InputId1 { get; set; }

        [Option("input-id-2", Required = false, HelpText = "Set the input id in use for Player 2.")]
        public string InputId2 { get; set; }

        [Option("input-id-3", Required = false, HelpText = "Set the input id in use for Player 3.")]
        public string InputId3 { get; set; }

        [Option("input-id-4", Required = false, HelpText = "Set the input id in use for Player 4.")]
        public string InputId4 { get; set; }

        [Option("input-id-5", Required = false, HelpText = "Set the input id in use for Player 5.")]
        public string InputId5 { get; set; }

        [Option("input-id-6", Required = false, HelpText = "Set the input id in use for Player 6.")]
        public string InputId6 { get; set; }

        [Option("input-id-7", Required = false, HelpText = "Set the input id in use for Player 7.")]
        public string InputId7 { get; set; }

        [Option("input-id-8", Required = false, HelpText = "Set the input id in use for Player 8.")]
        public string InputId8 { get; set; }

        [Option("input-id-handheld", Required = false, HelpText = "Set the input id in use for the Handheld Player.")]
        public string InputIdHandheld { get; set; }

        [Option("enable-keyboard", Required = false, Default = false, HelpText = "Enable or disable keyboard support (Independent from controllers binding).")]
        public bool EnableKeyboard { get; set; }

        [Option("enable-mouse", Required = false, Default = false, HelpText = "Enable or disable mouse support.")]
        public bool EnableMouse { get; set; }

        [Option("hide-cursor", Required = false, Default = HideCursorMode.OnIdle, HelpText = "Change when the cursor gets hidden.")]
        public HideCursorMode HideCursorMode { get; set; }

        [Option("list-input-profiles", Required = false, HelpText = "List input profiles.")]
        public bool ListInputProfiles { get; set; }

        [Option("list-input-ids", Required = false, HelpText = "List input IDs.")]
        public bool ListInputIds { get; set; }

        // System

        [Option("disable-ptc", Required = false, HelpText = "Disables profiled persistent translation cache.")]
        public bool DisablePTC { get; set; }

        [Option("enable-internet-connection", Required = false, Default = false, HelpText = "Enables guest Internet connection.")]
        public bool EnableInternetAccess { get; set; }

        [Option("disable-fs-integrity-checks", Required = false, HelpText = "Disables integrity checks on Game content files.")]
        public bool DisableFsIntegrityChecks { get; set; }

        [Option("fs-global-access-log-mode", Required = false, Default = 0, HelpText = "Enables FS access log output to the console.")]
        public int FsGlobalAccessLogMode { get; set; }

        [Option("vsync-mode", Required = false, Default = VSyncMode.Switch, HelpText = "Sets the emulated VSync mode (Switch, Unbounded, or Custom).")]
        public VSyncMode VSyncMode { get; set; }

        [Option("custom-refresh-rate", Required = false, Default = 90, HelpText = "Sets the custom refresh rate target value (integer).")]
        public int CustomVSyncInterval { get; set; }

        [Option("disable-shader-cache", Required = false, HelpText = "Disables Shader cache.")]
        public bool DisableShaderCache { get; set; }

        [Option("enable-texture-recompression", Required = false, Default = false, HelpText = "Enables Texture recompression.")]
        public bool EnableTextureRecompression { get; set; }

        [Option("disable-docked-mode", Required = false, HelpText = "Disables Docked Mode.")]
        public bool DisableDockedMode { get; set; }

        [Option("system-language", Required = false, Default = SystemLanguage.AmericanEnglish, HelpText = "Change System Language.")]
        public SystemLanguage SystemLanguage { get; set; }

        [Option("system-region", Required = false, Default = RegionCode.USA, HelpText = "Change System Region.")]
        public RegionCode SystemRegion { get; set; }

        [Option("system-timezone", Required = false, Default = "UTC", HelpText = "Change System TimeZone.")]
        public string SystemTimeZone { get; set; }

        [Option("system-time-offset", Required = false, Default = 0, HelpText = "Change System Time Offset in seconds.")]
        public long SystemTimeOffset { get; set; }

        [Option("memory-manager-mode", Required = false, Default = MemoryManagerMode.HostMappedUnsafe, HelpText = "The selected memory manager mode.")]
        public MemoryManagerMode MemoryManagerMode { get; set; }

        [Option("audio-volume", Required = false, Default = 1.0f, HelpText = "The audio level (0 to 1).")]
        public float AudioVolume { get; set; }

        [Option("use-hypervisor", Required = false, Default = true, HelpText = "Uses Hypervisor over JIT if available.")]
        public bool? UseHypervisor { get; set; }

        [Option("lan-interface-id", Required = false, Default = "0", HelpText = "GUID for the network interface used by LAN.")]
        public string MultiplayerLanInterfaceId { get; set; }

        // Logging

        [Option("disable-file-logging", Required = false, Default = false, HelpText = "Disables logging to a file on disk.")]
        public bool DisableFileLog { get; set; }

        [Option("enable-debug-logs", Required = false, Default = false, HelpText = "Enables printing debug log messages.")]
        public bool LoggingEnableDebug { get; set; }

        [Option("disable-stub-logs", Required = false, HelpText = "Disables printing stub log messages.")]
        public bool LoggingDisableStub { get; set; }

        [Option("disable-info-logs", Required = false, HelpText = "Disables printing info log messages.")]
        public bool LoggingDisableInfo { get; set; }

        [Option("disable-warning-logs", Required = false, HelpText = "Disables printing warning log messages.")]
        public bool LoggingDisableWarning { get; set; }

        [Option("disable-error-logs", Required = false, HelpText = "Disables printing error log messages.")]
        public bool LoggingDisableError { get; set; }

        [Option("enable-trace-logs", Required = false, Default = false, HelpText = "Enables printing trace log messages.")]
        public bool LoggingEnableTrace { get; set; }

        [Option("disable-guest-logs", Required = false, HelpText = "Disables printing guest log messages.")]
        public bool LoggingDisableGuest { get; set; }

        [Option("enable-fs-access-logs", Required = false, Default = false, HelpText = "Enables printing FS access log messages.")]
        public bool LoggingEnableFsAccessLog { get; set; }

        [Option("graphics-debug-level", Required = false, Default = GraphicsDebugLevel.None, HelpText = "Change Graphics API debug log level.")]
        public GraphicsDebugLevel LoggingGraphicsDebugLevel { get; set; }

        // Graphics

        [Option("resolution-scale", Required = false, Default = 1, HelpText = "Resolution Scale. A floating point scale applied to applicable render targets.")]
        public float ResScale { get; set; }

        [Option("max-anisotropy", Required = false, Default = -1, HelpText = "Max Anisotropy. Values range from 0 - 16. Set to -1 to let the game decide.")]
        public float MaxAnisotropy { get; set; }

        [Option("aspect-ratio", Required = false, Default = AspectRatio.Fixed16x9, HelpText = "Aspect Ratio applied to the renderer window.")]
        public AspectRatio AspectRatio { get; set; }

        [Option("backend-threading", Required = false, Default = BackendThreading.Auto, HelpText = "Whether or not backend threading is enabled. The \"Auto\" setting will determine whether threading should be enabled at runtime.")]
        public BackendThreading BackendThreading { get; set; }

        [Option("disable-macro-hle", Required = false, HelpText = "Disables high-level emulation of Macro code. Leaving this enabled improves performance but may cause graphical glitches in some games.")]
        public bool DisableMacroHLE { get; set; }

        [Option("graphics-shaders-dump-path", Required = false, HelpText = "Dumps shaders in this local directory. (Developer only)")]
        public string GraphicsShadersDumpPath { get; set; }

        [Option("graphics-backend", Required = false, Default = GraphicsBackend.Vulkan, HelpText = "Change Graphics Backend to use.")]
        public GraphicsBackend GraphicsBackend { get; set; }

        [Option("preferred-gpu-vendor", Required = false, Default = "", HelpText = "When using the Vulkan backend, prefer using the GPU from the specified vendor.")]
        public string PreferredGPUVendor { get; set; }

        [Option("anti-aliasing", Required = false, Default = AntiAliasing.None, HelpText = "Set the type of anti aliasing being used. [None|Fxaa|SmaaLow|SmaaMedium|SmaaHigh|SmaaUltra]")]
        public AntiAliasing AntiAliasing { get; set; }

        [Option("scaling-filter", Required = false, Default = ScalingFilter.Bilinear, HelpText = "Set the scaling filter. [Bilinear|Nearest|Fsr|Area]")]
        public ScalingFilter ScalingFilter { get; set; }

        [Option("scaling-filter-level", Required = false, Default = 0, HelpText = "Set the scaling filter intensity (currently only applies to FSR). [0-100]")]
        public int ScalingFilterLevel { get; set; }

        // Hacks

        [Option("dram-size", Required = false, Default = MemoryConfiguration.MemoryConfiguration4GiB, HelpText = "Set the RAM amount on the emulated system.")]
        public MemoryConfiguration DramSize { get; set; }

        [Option("ignore-missing-services", Required = false, Default = false, HelpText = "Enable ignoring missing services.")]
        public bool IgnoreMissingServices { get; set; }
        
        [Option("ignore-controller-applet", Required = false, Default = false, HelpText = "Enable ignoring the controller applet when your game loses connection to your controller.")]
        public bool IgnoreControllerApplet { get; set; }

        // Values

        [Value(0, MetaName = "input", HelpText = "Input to load.", Required = true)]
        public string InputPath { get; set; }

        public SafeDictionary<PlayerIndex, InputConfig> InheritedInputConfigs = new();
    }
}
