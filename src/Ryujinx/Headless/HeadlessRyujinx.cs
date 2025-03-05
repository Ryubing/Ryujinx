using CommandLine;
using Gommon;
using Ryujinx.Ava;
using Ryujinx.Ava.Systems.Configuration;
using Ryujinx.Common;
using Ryujinx.Common.Configuration;
using Ryujinx.Common.Configuration.Hid;
using Ryujinx.Common.Configuration.Hid.Controller;
using Ryujinx.Common.GraphicsDriver;
using Ryujinx.Common.Logging;
using Ryujinx.Common.Logging.Targets;
using Ryujinx.Common.SystemInterop;
using Ryujinx.Common.Utilities;
using Ryujinx.Cpu;
using Ryujinx.Graphics.GAL;
using Ryujinx.Graphics.Gpu;
using Ryujinx.Graphics.Gpu.Shader;
using Ryujinx.Graphics.Vulkan.MoltenVK;
using Ryujinx.HLE;
using Ryujinx.HLE.FileSystem;
using Ryujinx.HLE.HOS;
using Ryujinx.HLE.HOS.Services.Account.Acc;
using Ryujinx.Input;
using Ryujinx.Input.HLE;
using Ryujinx.Input.SDL2;
using Ryujinx.SDL2.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace Ryujinx.Headless
{
    public partial class HeadlessRyujinx
    {
        private static VirtualFileSystem _virtualFileSystem;
        private static ContentManager _contentManager;
        private static AccountManager _accountManager;
        private static LibHacHorizonManager _libHacHorizonManager;
        private static UserChannelPersistence _userChannelPersistence;
        private static InputManager _inputManager;
        private static Switch _emulationContext;
        private static WindowBase _window;
        private static WindowsMultimediaTimerResolution _windowsMultimediaTimerResolution;
        private static List<InputConfig> _inputConfiguration = [];
        private static bool _enableKeyboard;
        private static bool _enableMouse;

        private static readonly InputConfigJsonSerializerContext _serializerContext = new(JsonHelper.GetDefaultSerializerOptions());

        public static void Entrypoint(string[] args)
        {
            // Make process DPI aware for proper window sizing on high-res screens.
            ForceDpiAware.Windows();

            Console.Title = $"HeadlessRyujinx Console {Program.Version}";

            if (OperatingSystem.IsMacOS() || OperatingSystem.IsLinux())
            {
                AutoResetEvent invoked = new(false);

                // MacOS must perform SDL polls from the main thread.
                SDL2Driver.MainThreadDispatcher = action =>
                {
                    invoked.Reset();

                    WindowBase.QueueMainThreadAction(() =>
                    {
                        action();

                        invoked.Set();
                    });

                    invoked.WaitOne();
                };
            }

            if (OperatingSystem.IsMacOS())
            {
                MVKInitialization.InitializeResolver();
            }

            Parser.Default.ParseArguments<Options>(args)
                .WithParsed(options => Load(args, options))
                .WithNotParsed(errors =>
                {
                    Logger.Error?.PrintMsg(LogClass.Application, "Error parsing command-line arguments:");
                    
                    errors.ForEach(err => Logger.Error?.PrintMsg(LogClass.Application, $" - {err.Tag}"));
                });
        }
        
        public static void ReloadConfig(string customConfigPath = null)
        {
            string localConfigurationPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ReleaseInformation.ConfigName);
            string appDataConfigurationPath = Path.Combine(AppDataManager.BaseDirPath, ReleaseInformation.ConfigName);

            string configurationPath = null;
            
            // Now load the configuration as the other subsystems are now registered
            if (customConfigPath != null && File.Exists(customConfigPath))
            {
                configurationPath = customConfigPath;
            } 
            else if (File.Exists(localConfigurationPath))
            {
                configurationPath = localConfigurationPath;
            }
            else if (File.Exists(appDataConfigurationPath))
            {
                configurationPath = appDataConfigurationPath;
            }

            if (configurationPath == null)
            {
                // No configuration, we load the default values and save it to disk
                configurationPath = appDataConfigurationPath;
                Logger.Notice.Print(LogClass.Application, $"No configuration file found. Saving default configuration to: {configurationPath}");

                ConfigurationState.Instance.LoadDefault();
                ConfigurationState.Instance.ToFileFormat().SaveConfig(configurationPath);
            }
            else
            {
                Logger.Notice.Print(LogClass.Application, $"Loading configuration from: {configurationPath}");

                if (ConfigurationFileFormat.TryLoad(configurationPath, out ConfigurationFileFormat configurationFileFormat))
                {
                    ConfigurationState.Instance.Load(configurationFileFormat, configurationPath);
                }
                else
                {
                    Logger.Warning?.PrintMsg(LogClass.Application, $"Failed to load config! Loading the default config instead.\nFailed config location: {configurationPath}");

                    ConfigurationState.Instance.LoadDefault();
                }
            }
        }

        static void Load(string[] originalArgs, Options option)
        {
            Initialize();

            bool useLastUsedProfile = false;

            if (option.InheritConfig)
            {
                option.InheritMainConfig(originalArgs, ConfigurationState.Instance, out useLastUsedProfile);
            }

            AppDataManager.Initialize(option.BaseDataDir);
            
            if (useLastUsedProfile && AccountSaveDataManager.GetLastUsedUser().TryGet(out UserProfile profile))
                option.UserProfile = profile.Name;
            
            // Check if keys exists.
            if (!File.Exists(Path.Combine(AppDataManager.KeysDirPath, "prod.keys")))
            {
                if (!(AppDataManager.Mode == AppDataManager.LaunchMode.UserProfile && File.Exists(Path.Combine(AppDataManager.KeysDirPathUser, "prod.keys"))))
                {
                    Logger.Error?.Print(LogClass.Application, "Keys not found");
                }
            }
            
            ReloadConfig();

            if (option.InheritConfig)
            {
                option.InheritMainConfigInput(originalArgs, ConfigurationState.Instance);
            }
            
            _virtualFileSystem = VirtualFileSystem.CreateInstance();
            _libHacHorizonManager = new LibHacHorizonManager();

            _libHacHorizonManager.InitializeFsServer(_virtualFileSystem);
            _libHacHorizonManager.InitializeArpServer();
            _libHacHorizonManager.InitializeBcatServer();
            _libHacHorizonManager.InitializeSystemClients();

            _contentManager = new ContentManager(_virtualFileSystem);
            _accountManager = new AccountManager(_libHacHorizonManager.RyujinxClient, option.UserProfile);
            _userChannelPersistence = new UserChannelPersistence();

            _inputManager = new InputManager(new SDL2KeyboardDriver(), new SDL2GamepadDriver());

            GraphicsConfig.EnableShaderCache = !option.DisableShaderCache;

            if (OperatingSystem.IsMacOS())
            {
                if (option.GraphicsBackend == GraphicsBackend.OpenGl)
                {
                    option.GraphicsBackend = GraphicsBackend.Vulkan;
                    Logger.Warning?.Print(LogClass.Application, "OpenGL is not supported on macOS, switching to Vulkan!");
                }
            }

            if (option.ListInputIds)
            {
                Logger.Info?.Print(LogClass.Application, "Input Ids:");

                foreach (string id in _inputManager.KeyboardDriver.GamepadsIds)
                {
                    IGamepad gamepad = _inputManager.KeyboardDriver.GetGamepad(id);

                    Logger.Info?.Print(LogClass.Application, $"- {id} (\"{gamepad.Name}\")");

                    gamepad.Dispose();
                }

                foreach (string id in _inputManager.GamepadDriver.GamepadsIds)
                {
                    IGamepad gamepad = _inputManager.GamepadDriver.GetGamepad(id);

                    Logger.Info?.Print(LogClass.Application, $"- {id} (\"{gamepad.Name}\")");

                    gamepad.Dispose();
                }

                return;
            }

            if (option.InputPath == null)
            {
                Logger.Error?.Print(LogClass.Application, "Please provide a file to load");

                return;
            }

            _inputConfiguration ??= [];
            _enableKeyboard = option.EnableKeyboard;
            _enableMouse = option.EnableMouse;
            
            LoadPlayerConfiguration(option.InputProfile1Name, option.InputId1, PlayerIndex.Player1);
            LoadPlayerConfiguration(option.InputProfile2Name, option.InputId2, PlayerIndex.Player2); 
            LoadPlayerConfiguration(option.InputProfile3Name, option.InputId3, PlayerIndex.Player3);
            LoadPlayerConfiguration(option.InputProfile4Name, option.InputId4, PlayerIndex.Player4);
            LoadPlayerConfiguration(option.InputProfile5Name, option.InputId5, PlayerIndex.Player5);
            LoadPlayerConfiguration(option.InputProfile6Name, option.InputId6, PlayerIndex.Player6);
            LoadPlayerConfiguration(option.InputProfile7Name, option.InputId7, PlayerIndex.Player7);
            LoadPlayerConfiguration(option.InputProfile8Name, option.InputId8, PlayerIndex.Player8);
            LoadPlayerConfiguration(option.InputProfileHandheldName, option.InputIdHandheld, PlayerIndex.Handheld);
            
            if (_inputConfiguration.Count == 0)
            {
                return;
            }

            // Setup logging level
            Logger.SetEnable(LogLevel.Debug, option.LoggingEnableDebug);
            Logger.SetEnable(LogLevel.Stub, !option.LoggingDisableStub);
            Logger.SetEnable(LogLevel.Info, !option.LoggingDisableInfo);
            Logger.SetEnable(LogLevel.Warning, !option.LoggingDisableWarning);
            Logger.SetEnable(LogLevel.Error, !option.LoggingDisableError);
            Logger.SetEnable(LogLevel.Trace, option.LoggingEnableTrace);
            Logger.SetEnable(LogLevel.Guest, !option.LoggingDisableGuest);
            Logger.SetEnable(LogLevel.AccessLog, option.LoggingEnableFsAccessLog);

            if (!option.DisableFileLog)
            {
                string logDir = AppDataManager.LogsDirPath;
                FileStream logFile = null;

                if (!string.IsNullOrEmpty(logDir))
                {
                    logFile = FileLogTarget.PrepareLogFile(logDir);
                }

                if (logFile != null)
                {
                    Logger.AddTarget(new AsyncLogTargetWrapper(
                        new FileLogTarget("file", logFile),
                        1000
                    ));
                }
                else
                {
                    Logger.Error?.Print(LogClass.Application, "No writable log directory available. Make sure either the Logs directory, Application Data, or the Ryujinx directory is writable.");
                }
            }

            // Setup graphics configuration
            GraphicsConfig.EnableShaderCache = !option.DisableShaderCache;
            GraphicsConfig.EnableTextureRecompression = option.EnableTextureRecompression;
            GraphicsConfig.ResScale = option.ResScale;
            GraphicsConfig.MaxAnisotropy = option.MaxAnisotropy;
            GraphicsConfig.ShadersDumpPath = option.GraphicsShadersDumpPath;
            GraphicsConfig.EnableMacroHLE = !option.DisableMacroHLE;

            DriverUtilities.InitDriverConfig(option.BackendThreading == BackendThreading.Off);
            
            if (_inputConfiguration.OfType<StandardControllerInputConfig>()
                .Any(ic => ic?.Led?.UseRainbow ?? false))
                Rainbow.Enable();

            while (true)
            {
                LoadApplication(option);

                if (_userChannelPersistence.PreviousIndex == -1 || !_userChannelPersistence.ShouldRestart)
                {
                    break;
                }

                _userChannelPersistence.ShouldRestart = false;
            }

            try
            {
                _inputManager.Dispose();
            } catch {}

            return;
            
            void LoadPlayerConfiguration(string inputProfileName, string inputId, PlayerIndex index)
            {
                if (index == PlayerIndex.Handheld && _inputConfiguration.Count > 0)
                {
                    Logger.Info?.Print(LogClass.Configuration, "Skipping handheld configuration as there are already other players configured.");
                    return;
                }
                
                InputConfig inputConfig = option.InheritedInputConfigs[index] ?? HandlePlayerConfiguration(inputProfileName, inputId, index);

                if (inputConfig != null)
                {
                    _inputConfiguration.Add(inputConfig);
                }
            }
        }

        private static void SetupProgressHandler()
        {
            if (_emulationContext.Processes.ActiveApplication.DiskCacheLoadState != null)
            {
                _emulationContext.Processes.ActiveApplication.DiskCacheLoadState.StateChanged -= ProgressHandler;
                _emulationContext.Processes.ActiveApplication.DiskCacheLoadState.StateChanged += ProgressHandler;
            }

            _emulationContext.Gpu.ShaderCacheStateChanged -= ProgressHandler;
            _emulationContext.Gpu.ShaderCacheStateChanged += ProgressHandler;
        }

        private static void ProgressHandler<T>(T state, int current, int total) where T : Enum
        {
            string label = state switch
            {
                LoadState => "PTC",
                ShaderCacheState => "Shaders",
                _ => throw new ArgumentException($"Unknown Progress Handler type {typeof(T)}")
            };

            Logger.Info?.Print(LogClass.Application, $"{label} : {current}/{total}");
        }

        private static WindowBase CreateWindow(Options options)
        {
            return options.GraphicsBackend switch
            {
                GraphicsBackend.Vulkan => new VulkanWindow(_inputManager, options.LoggingGraphicsDebugLevel, options.AspectRatio, options.EnableMouse, options.HideCursorMode, options.IgnoreControllerApplet),
                _ => new OpenGLWindow(_inputManager, options.LoggingGraphicsDebugLevel, options.AspectRatio, options.EnableMouse, options.HideCursorMode, options.IgnoreControllerApplet)
            };
        }

        private static void ExecutionEntrypoint()
        {
            if (OperatingSystem.IsWindows())
            {
                _windowsMultimediaTimerResolution = new WindowsMultimediaTimerResolution(1);
            }

            DisplaySleep.Prevent();

            _window.Initialize(_emulationContext, _inputConfiguration, _enableKeyboard, _enableMouse);

            _window.Execute();

            _emulationContext.Dispose();
            _window.Dispose();

            if (OperatingSystem.IsWindows())
            {
                _windowsMultimediaTimerResolution?.Dispose();
                _windowsMultimediaTimerResolution = null;
            }
        }

        private static bool LoadApplication(Options options)
        {
            string path = options.InputPath;

            Logger.RestartTime();

            WindowBase window = CreateWindow(options);
            IRenderer renderer = CreateRenderer(options, window);

            _window = window;

            _window.IsFullscreen = options.IsFullscreen;
            _window.DisplayId = options.DisplayId;
            _window.IsExclusiveFullscreen = options.IsExclusiveFullscreen;
            _window.ExclusiveFullscreenWidth = options.ExclusiveFullscreenWidth;
            _window.ExclusiveFullscreenHeight = options.ExclusiveFullscreenHeight;
            _window.AntiAliasing = options.AntiAliasing;
            _window.ScalingFilter = options.ScalingFilter;
            _window.ScalingFilterLevel = options.ScalingFilterLevel;

            _emulationContext = InitializeEmulationContext(window, renderer, options);

            SystemVersion firmwareVersion = _contentManager.GetCurrentFirmwareVersion();

            Logger.Notice.Print(LogClass.Application, $"Using Firmware Version: {firmwareVersion?.VersionString}");

            if (Directory.Exists(path))
            {
                string[] romFsFiles = Directory.GetFiles(path, "*.istorage");

                if (romFsFiles.Length == 0)
                {
                    romFsFiles = Directory.GetFiles(path, "*.romfs");
                }

                if (romFsFiles.Length > 0)
                {
                    Logger.Info?.Print(LogClass.Application, "Loading as cart with RomFS.");

                    if (!_emulationContext.LoadCart(path, romFsFiles[0]))
                    {
                        _emulationContext.Dispose();

                        return false;
                    }
                }
                else
                {
                    Logger.Info?.Print(LogClass.Application, "Loading as cart WITHOUT RomFS.");

                    if (!_emulationContext.LoadCart(path))
                    {
                        _emulationContext.Dispose();

                        return false;
                    }
                }
            }
            else if (File.Exists(path))
            {
                switch (Path.GetExtension(path).ToLowerInvariant())
                {
                    case ".xci":
                        Logger.Info?.Print(LogClass.Application, "Loading as XCI.");

                        if (!_emulationContext.LoadXci(path))
                        {
                            _emulationContext.Dispose();

                            return false;
                        }
                        break;
                    case ".nca":
                        Logger.Info?.Print(LogClass.Application, "Loading as NCA.");

                        if (!_emulationContext.LoadNca(path))
                        {
                            _emulationContext.Dispose();

                            return false;
                        }
                        break;
                    case ".nsp":
                    case ".pfs0":
                        Logger.Info?.Print(LogClass.Application, "Loading as NSP.");

                        if (!_emulationContext.LoadNsp(path))
                        {
                            _emulationContext.Dispose();

                            return false;
                        }
                        break;
                    default:
                        Logger.Info?.Print(LogClass.Application, "Loading as Homebrew.");
                        try
                        {
                            if (!_emulationContext.LoadProgram(path))
                            {
                                _emulationContext.Dispose();

                                return false;
                            }
                        }
                        catch (ArgumentOutOfRangeException)
                        {
                            Logger.Error?.Print(LogClass.Application, "The specified file is not supported by Ryujinx.");

                            _emulationContext.Dispose();

                            return false;
                        }
                        break;
                }
            }
            else
            {
                Logger.Warning?.Print(LogClass.Application, $"Couldn't load '{options.InputPath}'. Please specify a valid XCI/NCA/NSP/PFS0/NRO file.");

                _emulationContext.Dispose();

                return false;
            }

            SetupProgressHandler();
            ExecutionEntrypoint();

            return true;
        }
    }
}
