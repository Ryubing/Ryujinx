using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DynamicData;
using DynamicData.Binding;
using FluentAvalonia.UI.Controls;
using Gommon;
using LibHac.Common;
using LibHac.Ns;
using Ryujinx.Ava.Common;
using Ryujinx.Ava.Common.Locale;
using Ryujinx.Ava.Input;
using Ryujinx.Ava.UI.Controls;
using Ryujinx.Ava.UI.Helpers;
using Ryujinx.Ava.UI.Models;
using Ryujinx.Ava.UI.Models.Generic;
using Ryujinx.Ava.UI.Renderer;
using Ryujinx.Ava.UI.Windows;
using Ryujinx.Ava.Utilities.AppLibrary;
using Ryujinx.Ava.Utilities.Configuration;
using Ryujinx.Common;
using Ryujinx.Common.Configuration;
using Ryujinx.Common.Helper;
using Ryujinx.Common.Logging;
using Ryujinx.Common.UI;
using Ryujinx.Common.Utilities;
using Ryujinx.Cpu;
using Ryujinx.HLE;
using Ryujinx.HLE.FileSystem;
using Ryujinx.HLE.HOS;
using Ryujinx.HLE.HOS.Services.Account.Acc;
using Ryujinx.HLE.HOS.Services.Nfc.AmiiboDecryption;
using Ryujinx.HLE.UI;
using Ryujinx.Input.HLE;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Key = Ryujinx.Input.Key;
using MissingKeyException = LibHac.Common.Keys.MissingKeyException;
using ShaderCacheLoadingState = Ryujinx.Graphics.Gpu.Shader.ShaderCacheState;

namespace Ryujinx.Ava.UI.ViewModels
{
    public partial class MainWindowViewModel : BaseModel
    {
        private const int HotKeyPressDelayMs = 500;
        private delegate int LoadContentFromFolderDelegate(List<string> dirs, out int numRemoved);

        [ObservableProperty] private ObservableCollectionExtended<ApplicationData> _applications;
        [ObservableProperty] private string _aspectRatioStatusText;
        [ObservableProperty] private string _loadHeading;
        [ObservableProperty] private string _cacheLoadStatus;
        [ObservableProperty] private string _dockedStatusText;
        [ObservableProperty] private string _fifoStatusText;
        [ObservableProperty] private string _gameStatusText;
        [ObservableProperty] private string _volumeStatusText;
        [ObservableProperty] private string _gpuNameText;
        [ObservableProperty] private string _backendText;
        [ObservableProperty] private string _shaderCountText;
        [ObservableProperty] private bool _showShaderCompilationHint;
        [ObservableProperty] private bool _isFullScreen;
        [ObservableProperty] private int _progressMaximum;
        [ObservableProperty] private int _progressValue;
        [ObservableProperty] private bool _showMenuAndStatusBar = true;
        [ObservableProperty] private bool _showStatusSeparator;
        [ObservableProperty] private Brush _progressBarForegroundColor;
        [ObservableProperty] private Brush _progressBarBackgroundColor;
        [ObservableProperty] private Brush _vSyncModeColor;
        #nullable enable
        [ObservableProperty] private byte[]? _selectedIcon;
        #nullable disable
        [ObservableProperty] private int _statusBarProgressMaximum;
        [ObservableProperty] private int _statusBarProgressValue;
        [ObservableProperty] private string _statusBarProgressStatusText;
        [ObservableProperty] private bool _statusBarProgressStatusVisible;
        [ObservableProperty] private bool _isPaused;
        [ObservableProperty] private bool _isLoadingIndeterminate = true;
        [ObservableProperty] private bool _showAll;
        [ObservableProperty] private string _lastScannedAmiiboId;
        [ObservableProperty] private ReadOnlyObservableCollection<ApplicationData> _appsObservableList;
        [ObservableProperty] private long _lastFullscreenToggle = Environment.TickCount64;
        [ObservableProperty] private bool _showContent = true;
        [ObservableProperty] private float _volumeBeforeMute;
        [ObservableProperty] private bool _areMimeTypesRegistered = FileAssociationHelper.AreMimeTypesRegistered;
        [ObservableProperty] private Cursor _cursor;
        [ObservableProperty] private string _title;
        [ObservableProperty] private WindowState _windowState;
        [ObservableProperty] private double _windowWidth;
        [ObservableProperty] private double _windowHeight;
        [ObservableProperty] private bool _isActive;
        [ObservableProperty] private bool _isSubMenuOpen;
        [ObservableProperty] private ApplicationContextMenu _listAppContextMenu;
        [ObservableProperty] private ApplicationContextMenu _gridAppContextMenu;
        [ObservableProperty] private bool _updateAvailable;

        public static AsyncRelayCommand UpdateCommand { get; } = Commands.Create(async () =>
        {
            if (Updater.CanUpdate(true))
                await Updater.BeginUpdateAsync(true);
        });
        
        private bool _showLoadProgress;
        private bool _isGameRunning;
        private bool _isAmiiboRequested;
        private bool _isAmiiboBinRequested;
        private string _searchText;
        private Timer _searchTimer;
        private string _vSyncModeText;
        private string _showUiKey = "F4";
        private string _pauseKey = "F5";
        private string _screenshotKey = "F8";
        private float _volume;
        private bool _isAppletMenuActive;
        private bool _statusBarVisible;
        private bool _canUpdate = true;
        private ApplicationData _currentApplicationData;
        private readonly AutoResetEvent _rendererWaitEvent;
        private int _customVSyncInterval;
        private int _customVSyncIntervalPercentageProxy;
        private ApplicationData _listSelectedApplication;
        private ApplicationData _gridSelectedApplication;
        
        // Key is Title ID
        public SafeDictionary<string, LdnGameData.Array> LdnData = [];

        // The UI specifically uses a thicker bordered variant of the icon to avoid crunching out the border at lower resolutions.
        // For an example of this, download canary 1.2.95, then open the settings menu, and look at the icon in the top-left.
        // The border gets reduced to colored pixels in the 4 corners.
        public static readonly Bitmap IconBitmap =
            new(Assembly.GetAssembly(typeof(MainWindowViewModel))!
                .GetManifestResourceStream("Ryujinx.Assets.UIImages.Logo_Ryujinx_AntiAlias.png")!);

        public MainWindow Window { get; init; }

        internal AppHost AppHost { get; set; }

        public MainWindowViewModel()
        {
            Applications = [];

            Applications.ToObservableChangeSet()
                .Filter(Filter)
                .Sort(GetComparer())
                .OnItemAdded(_ => OnPropertyChanged(nameof(AppsObservableList)))
                .OnItemRemoved(_ => OnPropertyChanged(nameof(AppsObservableList)))
#pragma warning disable MVVMTK0034 // Event to update is fired below
                .Bind(out _appsObservableList);
#pragma warning restore MVVMTK0034

            _rendererWaitEvent = new AutoResetEvent(false);

            if (Program.PreviewerDetached)
            {
                LoadConfigurableHotKeys();

                Volume = ConfigurationState.Instance.System.AudioVolume;
                CustomVSyncInterval = ConfigurationState.Instance.Graphics.CustomVSyncInterval.Value;
            }
        }

        public void Initialize(
            ContentManager contentManager,
            IStorageProvider storageProvider,
            ApplicationLibrary applicationLibrary,
            VirtualFileSystem virtualFileSystem,
            AccountManager accountManager,
            InputManager inputManager,
            UserChannelPersistence userChannelPersistence,
            LibHacHorizonManager libHacHorizonManager,
            IHostUIHandler uiHandler,
            Action<bool> showLoading,
            Action<bool> switchToGameControl,
            Action<Control> setMainContent,
            TopLevel topLevel)
        {
            ContentManager = contentManager;
            StorageProvider = storageProvider;
            ApplicationLibrary = applicationLibrary;
            VirtualFileSystem = virtualFileSystem;
            AccountManager = accountManager;
            InputManager = inputManager;
            UserChannelPersistence = userChannelPersistence;
            LibHacHorizonManager = libHacHorizonManager;
            UiHandler = uiHandler;

            ShowLoading = showLoading;
            SwitchToGameControl = switchToGameControl;
            SetMainContent = setMainContent;
            TopLevel = topLevel;

#if DEBUG
            topLevel.AttachDevTools(new KeyGesture(Avalonia.Input.Key.F12, KeyModifiers.Control));
#endif
        }

        #region Properties

        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;

                _searchTimer?.Dispose();

                _searchTimer = new Timer(_ =>
                {
                    RefreshView();

                    _searchTimer.Dispose();
                    _searchTimer = null;
                }, null, 1000, 0);
            }
        }

        public bool CanUpdate
        {
            get => _canUpdate && EnableNonGameRunningControls && Updater.CanUpdate();
            set
            {
                _canUpdate = value;
                OnPropertyChanged();
            }
        }

        public bool StatusBarVisible
        {
            get => _statusBarVisible && EnableNonGameRunningControls;
            set
            {
                _statusBarVisible = value;

                OnPropertyChanged();
            }
        }

        public bool EnableNonGameRunningControls => !IsGameRunning;

        public bool ShowFirmwareStatus => !ShowLoadProgress;

        public bool IsGameRunning
        {
            get => _isGameRunning;
            set
            {
                _isGameRunning = value;

                if (!value)
                {
                    ShowMenuAndStatusBar = false;
                }

                OnPropertyChanged();
                OnPropertyChanged(nameof(EnableNonGameRunningControls));
                OnPropertyChanged(nameof(IsAppletMenuActive));
                OnPropertyChanged(nameof(StatusBarVisible));
                OnPropertyChanged(nameof(ShowFirmwareStatus));
            }
        }

        public bool IsAmiiboRequested
        {
            get => _isAmiiboRequested && _isGameRunning;
            set
            {
                _isAmiiboRequested = value;

                OnPropertyChanged();
            }
        }
        public bool IsAmiiboBinRequested
        {
            get => _isAmiiboBinRequested && _isGameRunning;
            set
            {
                _isAmiiboBinRequested = value;

                OnPropertyChanged();
            }
        }

        public bool CanScanAmiiboBinaries => AmiiboBinReader.HasAmiiboKeyFile;

        public bool ShowLoadProgress
        {
            get => _showLoadProgress;
            set
            {
                _showLoadProgress = value;

                OnPropertyChanged();
                OnPropertyChanged(nameof(ShowFirmwareStatus));
            }
        }
        
        public ApplicationData ListSelectedApplication
        {
            get => _listSelectedApplication;
            set
            {
                _listSelectedApplication = value;

#pragma warning disable MVVMTK0034
                if (_listSelectedApplication != null && _listAppContextMenu == null)

                    ListAppContextMenu = new ApplicationContextMenu();
                else if (_listSelectedApplication == null && _listAppContextMenu != null)
                    ListAppContextMenu = null!;
#pragma warning restore MVVMTK0034

                OnPropertyChanged();
            }
        }

        public ApplicationData GridSelectedApplication
        {
            get => _gridSelectedApplication;
            set
            {
                _gridSelectedApplication = value;

#pragma warning disable MVVMTK0034
                if (_gridSelectedApplication != null && _gridAppContextMenu == null)
                    GridAppContextMenu = new ApplicationContextMenu();
                else if (_gridSelectedApplication == null && _gridAppContextMenu != null)
                    GridAppContextMenu = null!;
#pragma warning restore MVVMTK0034
                
                OnPropertyChanged();
            }
        }

        public ApplicationData SelectedApplication
        {
            get
            {
                return Glyph switch
                {
                    Glyph.List => ListSelectedApplication,
                    Glyph.Grid => GridSelectedApplication,
                    _ => null,
                };
            }
            set
            {
                ListSelectedApplication = value;
                GridSelectedApplication = value;
            }        
        }

        public bool HasCompatibilityEntry => SelectedApplication.HasPlayabilityInfo;

        public bool HasDlc => ApplicationLibrary.HasDlcs(SelectedApplication.Id);

        public bool OpenUserSaveDirectoryEnabled => SelectedApplication.HasControlHolder && SelectedApplication.ControlHolder.Value.UserAccountSaveDataSize > 0;

        public bool OpenDeviceSaveDirectoryEnabled => SelectedApplication.HasControlHolder && SelectedApplication.ControlHolder.Value.DeviceSaveDataSize > 0;

        public bool TrimXCIEnabled => XCIFileTrimmer.CanTrim(SelectedApplication.Path, new XCITrimmerLog.MainWindow(this));

        public bool OpenBcatSaveDirectoryEnabled => SelectedApplication.HasControlHolder && SelectedApplication.ControlHolder.Value.BcatDeliveryCacheStorageSize > 0;

        public bool ShowCustomVSyncIntervalPicker 
            => _isGameRunning && AppHost.Device.VSyncMode == VSyncMode.Custom;

        public void UpdateVSyncIntervalPicker()
        {
            OnPropertyChanged(nameof(ShowCustomVSyncIntervalPicker));
        }

        public int CustomVSyncIntervalPercentageProxy
        {
            get => _customVSyncIntervalPercentageProxy;
            set
            {
                int newInterval = (int)((value / 100f) * 60);
                _customVSyncInterval = newInterval;
                _customVSyncIntervalPercentageProxy = value;
                if (_isGameRunning)
                {
                    AppHost.Device.CustomVSyncInterval = newInterval;
                    AppHost.Device.UpdateVSyncInterval();
                }
                OnPropertyChanged((nameof(CustomVSyncInterval)));
                OnPropertyChanged((nameof(CustomVSyncIntervalPercentageText)));
            }
        }

        public string CustomVSyncIntervalPercentageText
        {
            get
            {
                string text = CustomVSyncIntervalPercentageProxy.ToString() + "%";
                return text;
            }
            set
            {

            }
        }

        public int CustomVSyncInterval
        {
            get => _customVSyncInterval;
            set
            {
                _customVSyncInterval = value;
                int newPercent = (int)((value / 60f) * 100);
                _customVSyncIntervalPercentageProxy = newPercent;
                if (_isGameRunning)
                {
                    AppHost.Device.CustomVSyncInterval = value;
                    AppHost.Device.UpdateVSyncInterval();
                }
                OnPropertyChanged(nameof(CustomVSyncIntervalPercentageProxy));
                OnPropertyChanged(nameof(CustomVSyncIntervalPercentageText));
                OnPropertyChanged();
            }
        }

        public string VSyncModeText
        {
            get => _vSyncModeText;
            set
            {
                _vSyncModeText = value;

                OnPropertyChanged();
                OnPropertyChanged(nameof(ShowCustomVSyncIntervalPicker));
            }
        }

        public bool VolumeMuted => _volume == 0;

        public float Volume
        {
            get => _volume;
            set
            {
                _volume = value;

                if (_isGameRunning)
                {
                    AppHost.Device.SetVolume(_volume);
                }

                OnPropertyChanged(nameof(VolumeStatusText));
                OnPropertyChanged(nameof(VolumeMuted));
                OnPropertyChanged();
            }
        }

        public bool IsAppletMenuActive
        {
            get => _isAppletMenuActive && EnableNonGameRunningControls;
            set
            {
                _isAppletMenuActive = value;

                OnPropertyChanged();
            }
        }

        public bool IsGrid => Glyph == Glyph.Grid;
        public bool IsList => Glyph == Glyph.List;

        internal void Sort(bool isAscending)
        {
            IsAscending = isAscending;

            RefreshView();
        }

        internal void Sort(ApplicationSort sort)
        {
            SortMode = sort;

            RefreshView();
        }

        public bool StartGamesInFullscreen
        {
            get => ConfigurationState.Instance.UI.StartFullscreen;
            set
            {
                ConfigurationState.Instance.UI.StartFullscreen.Value = value;

                ConfigurationState.Instance.ToFileFormat().SaveConfig(Program.ConfigurationPath);

                OnPropertyChanged();
            }
        }

        public bool StartGamesWithoutUI
        {
            get => ConfigurationState.Instance.UI.StartNoUI;
            set
            {
                ConfigurationState.Instance.UI.StartNoUI.Value = value;

                ConfigurationState.Instance.ToFileFormat().SaveConfig(Program.ConfigurationPath);

                OnPropertyChanged();
            }
        }

        public bool ShowConsole
        {
            get => ConfigurationState.Instance.UI.ShowConsole;
            set
            {
                ConfigurationState.Instance.UI.ShowConsole.Value = value;

                ConfigurationState.Instance.ToFileFormat().SaveConfig(Program.ConfigurationPath);

                OnPropertyChanged();
            }
        }

        public bool ShowConsoleVisible
        {
            get => ConsoleHelper.SetConsoleWindowStateSupported;
        }

        public bool ManageFileTypesVisible
        {
            get => FileAssociationHelper.IsTypeAssociationSupported;
        }

        public Glyph Glyph
        {
            get => (Glyph)ConfigurationState.Instance.UI.GameListViewMode.Value;
            set
            {
                ConfigurationState.Instance.UI.GameListViewMode.Value = (int)value;

                OnPropertyChanged();
                OnPropertyChanged(nameof(IsGrid));
                OnPropertyChanged(nameof(IsList));

                ConfigurationState.Instance.ToFileFormat().SaveConfig(Program.ConfigurationPath);
            }
        }

        public bool ShowNames
        {
            get => ConfigurationState.Instance.UI.ShowNames && ConfigurationState.Instance.UI.GridSize > 1; 
            set
            {
                ConfigurationState.Instance.UI.ShowNames.Value = value;

                OnPropertyChanged();
                OnPropertyChanged(nameof(GridSizeScale));
                OnPropertyChanged(nameof(GridItemSelectorSize));

                ConfigurationState.Instance.ToFileFormat().SaveConfig(Program.ConfigurationPath);
            }
        }

        internal ApplicationSort SortMode
        {
            get => (ApplicationSort)ConfigurationState.Instance.UI.ApplicationSort.Value;
            private set
            {
                ConfigurationState.Instance.UI.ApplicationSort.Value = (int)value;

                OnPropertyChanged();
                OnPropertyChanged(nameof(SortName));

                ConfigurationState.Instance.ToFileFormat().SaveConfig(Program.ConfigurationPath);
            }
        }

        public int ListItemSelectorSize
        {
            get
            {
                return ConfigurationState.Instance.UI.GridSize.Value switch
                {
                    1 => 78,
                    2 => 100,
                    3 => 120,
                    4 => 140,
                    _ => 16,
                };
            }
        }

        public int GridItemSelectorSize
        {
            get
            {
                return ConfigurationState.Instance.UI.GridSize.Value switch
                {
                    1 => 120,
                    2 => ShowNames ? 210 : 150,
                    3 => ShowNames ? 240 : 180,
                    4 => ShowNames ? 280 : 220,
                    _ => 16,
                };
            }
        }

        public int GridSizeScale
        {
            get => ConfigurationState.Instance.UI.GridSize;
            set
            {
                ConfigurationState.Instance.UI.GridSize.Value = value;

                if (value < 2)
                {
                    ShowNames = false;
                }

                OnPropertyChanged();
                OnPropertyChanged(nameof(IsGridSmall));
                OnPropertyChanged(nameof(IsGridMedium));
                OnPropertyChanged(nameof(IsGridLarge));
                OnPropertyChanged(nameof(IsGridHuge));
                OnPropertyChanged(nameof(ListItemSelectorSize));
                OnPropertyChanged(nameof(GridItemSelectorSize));
                OnPropertyChanged(nameof(ShowNames));

                ConfigurationState.Instance.ToFileFormat().SaveConfig(Program.ConfigurationPath);
            }
        }

        public string SortName
        {
            get
            {
                return SortMode switch
                {
                    ApplicationSort.Favorite => LocaleManager.Instance[LocaleKeys.CommonFavorite],
                    ApplicationSort.TitleId => LocaleManager.Instance[LocaleKeys.DlcManagerTableHeadingTitleIdLabel],
                    ApplicationSort.Title => LocaleManager.Instance[LocaleKeys.GameListHeaderApplication],
                    ApplicationSort.Developer => LocaleManager.Instance[LocaleKeys.GameListSortDeveloper],
                    ApplicationSort.LastPlayed => LocaleManager.Instance[LocaleKeys.GameListSortLastPlayed],
                    ApplicationSort.TotalTimePlayed => LocaleManager.Instance[LocaleKeys.GameListSortTimePlayed],
                    ApplicationSort.FileType => LocaleManager.Instance[LocaleKeys.GameListSortFileExtension],
                    ApplicationSort.FileSize => LocaleManager.Instance[LocaleKeys.GameListSortFileSize],
                    ApplicationSort.Path => LocaleManager.Instance[LocaleKeys.GameListSortPath],
                    _ => string.Empty,
                };
            }
        }

        public bool IsAscending
        {
            get => ConfigurationState.Instance.UI.IsAscendingOrder;
            private set
            {
                ConfigurationState.Instance.UI.IsAscendingOrder.Value = value;

                OnPropertyChanged();
                OnPropertyChanged(nameof(SortMode));
                OnPropertyChanged(nameof(SortName));

                ConfigurationState.Instance.ToFileFormat().SaveConfig(Program.ConfigurationPath);
            }
        }

        public KeyGesture ShowUiKey
        {
            get => KeyGesture.Parse(_showUiKey);
            set
            {
                _showUiKey = value.ToString();

                OnPropertyChanged();
            }
        }

        public KeyGesture ScreenshotKey
        {
            get => KeyGesture.Parse(_screenshotKey);
            set
            {
                _screenshotKey = value.ToString();

                OnPropertyChanged();
            }
        }

        public KeyGesture PauseKey
        {
            get => KeyGesture.Parse(_pauseKey);
            set
            {
                _pauseKey = value.ToString();

                OnPropertyChanged();
            }
        }

        public ContentManager ContentManager { get; private set; }
        public IStorageProvider StorageProvider { get; private set; }
        public ApplicationLibrary ApplicationLibrary { get; private set; }
        public VirtualFileSystem VirtualFileSystem { get; private set; }
        public AccountManager AccountManager { get; private set; }
        public InputManager InputManager { get; private set; }
        public UserChannelPersistence UserChannelPersistence { get; private set; }
        public Action<bool> ShowLoading { get; private set; }
        public Action<bool> SwitchToGameControl { get; private set; }
        public Action<Control> SetMainContent { get; private set; }
        public TopLevel TopLevel { get; private set; }
        public RendererHost RendererHostControl { get; private set; }
        public bool IsClosing { get; set; }
        public LibHacHorizonManager LibHacHorizonManager { get; internal set; }
        public IHostUIHandler UiHandler { get; internal set; }
        public bool IsSortedByFavorite => SortMode == ApplicationSort.Favorite;
        public bool IsSortedByTitle => SortMode == ApplicationSort.Title;
        public bool IsSortedByTitleId => SortMode == ApplicationSort.TitleId;
        public bool IsSortedByDeveloper => SortMode == ApplicationSort.Developer;
        public bool IsSortedByLastPlayed => SortMode == ApplicationSort.LastPlayed;
        public bool IsSortedByTimePlayed => SortMode == ApplicationSort.TotalTimePlayed;
        public bool IsSortedByType => SortMode == ApplicationSort.FileType;
        public bool IsSortedBySize => SortMode == ApplicationSort.FileSize;
        public bool IsSortedByPath => SortMode == ApplicationSort.Path;
        public bool IsGridSmall => ConfigurationState.Instance.UI.GridSize == 1;
        public bool IsGridMedium => ConfigurationState.Instance.UI.GridSize == 2;
        public bool IsGridLarge => ConfigurationState.Instance.UI.GridSize == 3;
        public bool IsGridHuge => ConfigurationState.Instance.UI.GridSize == 4;

        #endregion

        #region PrivateMethods

        private static IComparer<ApplicationData> CreateComparer(bool ascending, Func<ApplicationData, IComparable> selector) =>
            ascending
                ? SortExpressionComparer<ApplicationData>.Ascending(selector)
                : SortExpressionComparer<ApplicationData>.Descending(selector);

        private IComparer<ApplicationData> GetComparer()
            => SortMode switch
            {
#pragma warning disable IDE0055 // Disable formatting
                ApplicationSort.Title           => CreateComparer(IsAscending, app => app.Name),
                ApplicationSort.Developer       => CreateComparer(IsAscending, app => app.Developer),
                ApplicationSort.LastPlayed      => new LastPlayedSortComparer(IsAscending),
                ApplicationSort.TotalTimePlayed => new TimePlayedSortComparer(IsAscending),
                ApplicationSort.FileType        => CreateComparer(IsAscending, app => app.FileExtension),
                ApplicationSort.FileSize        => CreateComparer(IsAscending, app => app.FileSize),
                ApplicationSort.Path            => CreateComparer(IsAscending, app => app.Path),
                ApplicationSort.Favorite        => CreateComparer(IsAscending, app => new AppListFavoriteComparable(app)),
                ApplicationSort.TitleId         => CreateComparer(IsAscending, app => app.Id),
                _ => null,
#pragma warning restore IDE0055
            };

        public void RefreshView()
        {
            RefreshGrid();
        }

        private void RefreshGrid()
        {
            Applications.ToObservableChangeSet()
                .Filter(Filter)
                .Sort(GetComparer())
#pragma warning disable MVVMTK0034
                .Bind(out _appsObservableList)
#pragma warning restore MVVMTK0034
                .AsObservableList();

            OnPropertyChanged(nameof(AppsObservableList));
        }

        private bool Filter(object arg)
        {
            if (arg is ApplicationData app)
            {
                if (string.IsNullOrWhiteSpace(_searchText))
                {
                    return true;
                }

                CompareInfo compareInfo = CultureInfo.CurrentCulture.CompareInfo;

                return compareInfo.IndexOf(app.Name, _searchText, CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace) >= 0;
            }

            return false;
        }

        public async Task HandleFirmwareInstallation(string filename)
        {
            try
            {
                SystemVersion firmwareVersion = ContentManager.VerifyFirmwarePackage(filename);

                if (firmwareVersion == null)
                {
                    await ContentDialogHelper.CreateErrorDialog(LocaleManager.Instance.UpdateAndGetDynamicValue(LocaleKeys.DialogFirmwareInstallerFirmwareNotFoundErrorMessage, filename));

                    return;
                }

                string dialogTitle = LocaleManager.Instance.UpdateAndGetDynamicValue(LocaleKeys.DialogFirmwareInstallerFirmwareInstallTitle, firmwareVersion.VersionString);
                string dialogMessage = LocaleManager.Instance.UpdateAndGetDynamicValue(LocaleKeys.DialogFirmwareInstallerFirmwareInstallMessage, firmwareVersion.VersionString);

                SystemVersion currentVersion = ContentManager.GetCurrentFirmwareVersion();
                if (currentVersion != null)
                {
                    dialogMessage += LocaleManager.Instance.UpdateAndGetDynamicValue(LocaleKeys.DialogFirmwareInstallerFirmwareInstallSubMessage, currentVersion.VersionString);
                }

                dialogMessage += LocaleManager.Instance[LocaleKeys.DialogFirmwareInstallerFirmwareInstallConfirmMessage];

                UserResult result = await ContentDialogHelper.CreateConfirmationDialog(
                    dialogTitle,
                    dialogMessage,
                    LocaleManager.Instance[LocaleKeys.InputDialogYes],
                    LocaleManager.Instance[LocaleKeys.InputDialogNo],
                    LocaleManager.Instance[LocaleKeys.RyujinxConfirm]);

                UpdateWaitWindow waitingDialog = new(dialogTitle, LocaleManager.Instance[LocaleKeys.DialogFirmwareInstallerFirmwareInstallWaitMessage]);

                if (result == UserResult.Yes)
                {
                    Logger.Info?.Print(LogClass.Application, $"Installing firmware {firmwareVersion.VersionString}");

                    Thread thread = new(() =>
                    {
                        Dispatcher.UIThread.InvokeAsync(delegate
                        {
                            waitingDialog.Show();
                        });

                        try
                        {
                            ContentManager.InstallFirmware(filename);

                            Dispatcher.UIThread.InvokeAsync(async delegate
                            {
                                waitingDialog.Close();

                                string message = LocaleManager.Instance.UpdateAndGetDynamicValue(LocaleKeys.DialogFirmwareInstallerFirmwareInstallSuccessMessage, firmwareVersion.VersionString);

                                await ContentDialogHelper.CreateInfoDialog(
                                    dialogTitle, 
                                    message, 
                                    LocaleManager.Instance[LocaleKeys.InputDialogOk], 
                                    string.Empty, 
                                    LocaleManager.Instance[LocaleKeys.RyujinxInfo]);

                                Logger.Info?.Print(LogClass.Application, message);

                                // Purge Applet Cache.

                                DirectoryInfo miiEditorCacheFolder = new(Path.Combine(AppDataManager.GamesDirPath, "0100000000001009", "cache"));

                                if (miiEditorCacheFolder.Exists)
                                {
                                    miiEditorCacheFolder.Delete(true);
                                }
                            });
                        }
                        catch (Exception ex)
                        {
                            Dispatcher.UIThread.InvokeAsync(async () =>
                            {
                                waitingDialog.Close();

                                await ContentDialogHelper.CreateErrorDialog(ex.Message);
                            });
                        }
                        finally
                        {
                            RefreshFirmwareStatus();
                        }
                    })
                    {
                        Name = "GUI.FirmwareInstallerThread",
                    };

                    thread.Start();
                }
            }
            catch (MissingKeyException ex)
            {
                if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime)
                {
                    Logger.Error?.Print(LogClass.Application, ex.ToString());

                    await UserErrorDialog.ShowUserErrorDialog(UserError.NoKeys);
                }
            }
            catch (Exception ex)
            {
                await ContentDialogHelper.CreateErrorDialog(ex.Message);
            }
        }

        private async Task HandleKeysInstallation(string filename)
        {
            try
            {
                string systemDirectory = AppDataManager.KeysDirPath;
                if (AppDataManager.Mode == AppDataManager.LaunchMode.UserProfile && Directory.Exists(AppDataManager.KeysDirPathUser))
                {
                    systemDirectory = AppDataManager.KeysDirPathUser;
                }

                string dialogTitle = LocaleManager.Instance.UpdateAndGetDynamicValue(LocaleKeys.DialogKeysInstallerKeysInstallTitle);
                string dialogMessage = LocaleManager.Instance.UpdateAndGetDynamicValue(LocaleKeys.DialogKeysInstallerKeysInstallMessage);

                bool alreadyKesyInstalled = ContentManager.AreKeysAlredyPresent(systemDirectory);
                if (alreadyKesyInstalled)
                {
                    dialogMessage += LocaleManager.Instance.UpdateAndGetDynamicValue(LocaleKeys.DialogKeysInstallerKeysInstallSubMessage);
                }

                dialogMessage += LocaleManager.Instance[LocaleKeys.DialogKeysInstallerKeysInstallConfirmMessage];

                UserResult result = await ContentDialogHelper.CreateConfirmationDialog(
                    dialogTitle,
                    dialogMessage,
                    LocaleManager.Instance[LocaleKeys.InputDialogYes],
                    LocaleManager.Instance[LocaleKeys.InputDialogNo],
                    LocaleManager.Instance[LocaleKeys.RyujinxConfirm]);

                UpdateWaitWindow waitingDialog = new(dialogTitle, LocaleManager.Instance[LocaleKeys.DialogKeysInstallerKeysInstallWaitMessage]);

                if (result == UserResult.Yes)
                {
                    Logger.Info?.Print(LogClass.Application, $"Installing Keys");

                    Thread thread = new(() =>
                    {
                        Dispatcher.UIThread.InvokeAsync(delegate
                        {
                            waitingDialog.Show();
                        });

                        try
                        {
                            ContentManager.InstallKeys(filename, systemDirectory);

                            Dispatcher.UIThread.InvokeAsync(async delegate
                            {
                                waitingDialog.Close();

                                string message = LocaleManager.Instance.UpdateAndGetDynamicValue(LocaleKeys.DialogKeysInstallerKeysInstallSuccessMessage);

                                await ContentDialogHelper.CreateInfoDialog(
                                    dialogTitle,
                                    message,
                                    LocaleManager.Instance[LocaleKeys.InputDialogOk],
                                    string.Empty,
                                    LocaleManager.Instance[LocaleKeys.RyujinxInfo]);

                                Logger.Info?.Print(LogClass.Application, message);
                            });
                        }
                        catch (Exception ex)
                        {
                            Dispatcher.UIThread.InvokeAsync(async () =>
                            {
                                waitingDialog.Close();

                                string message = ex.Message;
                                if(ex is FormatException)
                                {
                                    message = LocaleManager.Instance.UpdateAndGetDynamicValue(LocaleKeys.DialogKeysInstallerKeysNotFoundErrorMessage, filename);
                                }

                                await ContentDialogHelper.CreateErrorDialog(message);
                            });
                        }
                        finally
                        {
                            VirtualFileSystem.ReloadKeySet();
                        }
                    })
                    {
                        Name = "GUI.KeysInstallerThread",
                    };

                    thread.Start();
                }
            }
            catch (MissingKeyException ex)
            {
                if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime)
                {
                    Logger.Error?.Print(LogClass.Application, ex.ToString());

                    await UserErrorDialog.ShowUserErrorDialog(UserError.NoKeys);
                }
            }
            catch (Exception ex)
            {
                await ContentDialogHelper.CreateErrorDialog(ex.Message);
            }
        }
        private void ProgressHandler<T>(T state, int current, int total) where T : Enum
        {
            Dispatcher.UIThread.Post(() =>
            {
                ProgressMaximum = total;
                ProgressValue = current;

                switch (state)
                {
                    case LoadState ptcState:
                        CacheLoadStatus = $"{current} / {total}";
                        switch (ptcState)
                        {
                            case LoadState.Unloaded:
                            case LoadState.Loading:
                                LoadHeading = LocaleManager.Instance[LocaleKeys.CompilingPPTC];
                                IsLoadingIndeterminate = false;
                                break;
                            case LoadState.Loaded:
                                LoadHeading = LocaleManager.Instance.UpdateAndGetDynamicValue(LocaleKeys.LoadingHeading, _currentApplicationData.Name);
                                IsLoadingIndeterminate = true;
                                CacheLoadStatus = string.Empty;
                                break;
                        }
                        break;
                    case ShaderCacheLoadingState shaderCacheState:
                        CacheLoadStatus = $"{current} / {total}";
                        switch (shaderCacheState)
                        {
                            case ShaderCacheLoadingState.Start:
                            case ShaderCacheLoadingState.Loading:
                                LoadHeading = LocaleManager.Instance[LocaleKeys.CompilingShaders];
                                IsLoadingIndeterminate = false;
                                break;
                            case ShaderCacheLoadingState.Packaging:
                                LoadHeading = LocaleManager.Instance[LocaleKeys.PackagingShaders];
                                IsLoadingIndeterminate = false;
                                break;
                            case ShaderCacheLoadingState.Loaded:
                                LoadHeading = LocaleManager.Instance.UpdateAndGetDynamicValue(LocaleKeys.LoadingHeading, _currentApplicationData.Name);
                                IsLoadingIndeterminate = true;
                                CacheLoadStatus = string.Empty;
                                break;
                        }
                        break;
                    default:
                        throw new ArgumentException($"Unknown Progress Handler type {typeof(T)}");
                }
            });
        }

        private void PrepareLoadScreen()
        {
            using MemoryStream stream = new(SelectedIcon);
            using SKBitmap gameIconBmp = SKBitmap.Decode(stream);

            SKColor dominantColor = IconColorPicker.GetFilteredColor(gameIconBmp);

            const float ColorMultiple = 0.5f;

            Color progressFgColor = Color.FromRgb(dominantColor.Red, dominantColor.Green, dominantColor.Blue);
            Color progressBgColor = Color.FromRgb(
                (byte)(dominantColor.Red * ColorMultiple),
                (byte)(dominantColor.Green * ColorMultiple),
                (byte)(dominantColor.Blue * ColorMultiple));

            ProgressBarForegroundColor = new SolidColorBrush(progressFgColor);
            ProgressBarBackgroundColor = new SolidColorBrush(progressBgColor);
        }

        private void InitializeGame()
        {
            RendererHostControl.WindowCreated += RendererHost_Created;

            AppHost.StatusUpdatedEvent += Update_StatusBar;
            AppHost.AppExit += AppHost_AppExit;

            _rendererWaitEvent.WaitOne();

            AppHost?.Start();
            
            AppHost?.DisposeContext();
        }

        private async Task HandleRelaunch()
        {
            if (UserChannelPersistence.PreviousIndex != -1 && UserChannelPersistence.ShouldRestart)
            {
                UserChannelPersistence.ShouldRestart = false;

                await LoadApplication(_currentApplicationData);
            }
            else
            {
                // Otherwise, clear state.
                UserChannelPersistence = new UserChannelPersistence();
                _currentApplicationData = null;
            }
        }

        private void Update_StatusBar(object sender, StatusUpdatedEventArgs args)
        {
            if (ShowMenuAndStatusBar && !ShowLoadProgress)
            {
                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    Application.Current!.Styles.TryGetResource(args.VSyncMode,
                        Application.Current.ActualThemeVariant,
                        out object color);

                    if (color is Color clr)
                    {
                        VSyncModeColor = new SolidColorBrush(clr);
                    }

                    VSyncModeText = args.VSyncMode == "Custom" ? "Custom" : "VSync";
                    DockedStatusText = args.DockedMode;
                    AspectRatioStatusText = args.AspectRatio;
                    GameStatusText = args.GameStatus;
                    VolumeStatusText = args.VolumeStatus;
                    FifoStatusText = args.FifoStatus;

                    ShaderCountText = (ShowShaderCompilationHint = args.ShaderCount > 0)
                        ? $"{LocaleManager.Instance[LocaleKeys.CompilingShaders]}: {args.ShaderCount}"
                        : string.Empty;

                    ShowStatusSeparator = true;
                });
            }
        }

        private void RendererHost_Created(object sender, EventArgs e)
        {
            ShowLoading(false);

            _rendererWaitEvent.Set();
        }

        private async Task LoadContentFromFolder(LocaleKeys localeMessageAddedKey, LocaleKeys localeMessageRemovedKey, LoadContentFromFolderDelegate onDirsSelected)
        {
            IReadOnlyList<IStorageFolder> result = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = LocaleManager.Instance[LocaleKeys.OpenFolderDialogTitle],
                AllowMultiple = true,
            });

            if (result.Count > 0)
            {
                List<string> dirs = result.Select(it => it.Path.LocalPath).ToList();
                int numAdded = onDirsSelected(dirs, out int numRemoved);

                string msg = string.Join("\n",
                    string.Format(LocaleManager.Instance[localeMessageRemovedKey], numRemoved),
                    string.Format(LocaleManager.Instance[localeMessageAddedKey], numAdded)
                );

                await Dispatcher.UIThread.InvokeAsync(async () =>
                {
                    await ContentDialogHelper.ShowTextDialog(
                        LocaleManager.Instance[numAdded > 0 || numRemoved > 0 ? LocaleKeys.RyujinxConfirm : LocaleKeys.RyujinxInfo],
                        msg, 
                        string.Empty, 
                        string.Empty, 
                        string.Empty, 
                        LocaleManager.Instance[LocaleKeys.InputDialogOk], 
                        (int)Symbol.Checkmark);
                });
            }
        }

        #endregion

        #region PublicMethods

        public void SetUiProgressHandlers(Switch emulationContext)
        {
            if (emulationContext.Processes.ActiveApplication.DiskCacheLoadState != null)
            {
                emulationContext.Processes.ActiveApplication.DiskCacheLoadState.StateChanged -= ProgressHandler;
                emulationContext.Processes.ActiveApplication.DiskCacheLoadState.StateChanged += ProgressHandler;
            }

            emulationContext.Gpu.ShaderCacheStateChanged -= ProgressHandler;
            emulationContext.Gpu.ShaderCacheStateChanged += ProgressHandler;
        }

        public void LoadConfigurableHotKeys()
        {
            if (AvaloniaKeyboardMappingHelper.TryGetAvaKey((Key)ConfigurationState.Instance.Hid.Hotkeys.Value.ShowUI, out Avalonia.Input.Key showUiKey))
            {
                ShowUiKey = new KeyGesture(showUiKey);
            }

            if (AvaloniaKeyboardMappingHelper.TryGetAvaKey((Key)ConfigurationState.Instance.Hid.Hotkeys.Value.Screenshot, out Avalonia.Input.Key screenshotKey))
            {
                ScreenshotKey = new KeyGesture(screenshotKey);
            }

            if (AvaloniaKeyboardMappingHelper.TryGetAvaKey((Key)ConfigurationState.Instance.Hid.Hotkeys.Value.Pause, out Avalonia.Input.Key pauseKey))
            {
                PauseKey = new KeyGesture(pauseKey);
            }
        }

        public void TakeScreenshot()
        {
            AppHost.ScreenshotRequested = true;
        }

        public void HideUi()
        {
            ShowMenuAndStatusBar = false;
        }

        public void ToggleStartGamesInFullscreen()
        {
            StartGamesInFullscreen = !StartGamesInFullscreen;
        }

        public void ToggleStartGamesWithoutUI()
        {
            StartGamesWithoutUI = !StartGamesWithoutUI;
        }

        public void ToggleShowConsole()
        {
            ShowConsole = !ShowConsole;
        }

        public void SetListMode()
        {
            Glyph = Glyph.List;
        }

        public void SetGridMode()
        {
            Glyph = Glyph.Grid;
        }

        public void SetAspectRatio(AspectRatio aspectRatio)
        {
            ConfigurationState.Instance.Graphics.AspectRatio.Value = aspectRatio;
        }

        public async Task InstallFirmwareFromFile()
        {
            IReadOnlyList<IStorageFile> result = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                AllowMultiple = false,
                FileTypeFilter = new List<FilePickerFileType>
                {
                    new(LocaleManager.Instance[LocaleKeys.FileDialogAllTypes])
                    {
                        Patterns = ["*.xci", "*.zip"],
                        AppleUniformTypeIdentifiers = ["com.ryujinx.xci", "public.zip-archive"],
                        MimeTypes = ["application/x-nx-xci", "application/zip"],
                    },
                    new("XCI")
                    {
                        Patterns = ["*.xci"],
                        AppleUniformTypeIdentifiers = ["com.ryujinx.xci"],
                        MimeTypes = ["application/x-nx-xci"],
                    },
                    new("ZIP")
                    {
                        Patterns = ["*.zip"],
                        AppleUniformTypeIdentifiers = ["public.zip-archive"],
                        MimeTypes = ["application/zip"],
                    },
                },
            });

            if (result.Count > 0)
            {
                await HandleFirmwareInstallation(result[0].Path.LocalPath);
            }
        }

        public async Task InstallFirmwareFromFolder()
        {
            IReadOnlyList<IStorageFolder> result = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                AllowMultiple = false,
            });

            if (result.Count > 0)
            {
                await HandleFirmwareInstallation(result[0].Path.LocalPath);
            }
        }

        public async Task InstallKeysFromFile()
        {
            IReadOnlyList<IStorageFile> result = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                AllowMultiple = false,
                FileTypeFilter = new List<FilePickerFileType>
                {
                    new(LocaleManager.Instance[LocaleKeys.FileDialogAllTypes])
                    {
                        Patterns = ["*.keys", "*.zip"],
                        AppleUniformTypeIdentifiers = ["com.ryujinx.xci", "public.zip-archive"],
                        MimeTypes = ["application/keys", "application/zip"],
                    },
                    new("KEYS")
                    {
                        Patterns = ["*.keys"],
                        AppleUniformTypeIdentifiers = ["com.ryujinx.xci"],
                        MimeTypes = ["application/keys"],
                    },
                    new("ZIP")
                    {
                        Patterns = ["*.zip"],
                        AppleUniformTypeIdentifiers = ["public.zip-archive"],
                        MimeTypes = ["application/zip"],
                    },
                },
            });

            if (result.Count > 0)
            {
                await HandleKeysInstallation(result[0].Path.LocalPath);
            }
        }

        public async Task InstallKeysFromFolder()
        {
            IReadOnlyList<IStorageFolder> result = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                AllowMultiple = false,
            });

            if (result.Count > 0)
            {
                await HandleKeysInstallation(result[0].Path.LocalPath);
            }
        }

        public void OpenRyujinxFolder()
        {
            OpenHelper.OpenFolder(AppDataManager.BaseDirPath);
        }

        public void OpenScreenshotsFolder()
        {
            string screenshotsDir = Path.Combine(AppDataManager.BaseDirPath, "screenshots");

            try
            {
                if (!Directory.Exists(screenshotsDir))
                    Directory.CreateDirectory(screenshotsDir);
            }
            catch (Exception ex)
            {
                Logger.Error?.Print(LogClass.Application, $"Failed to create directory at path {screenshotsDir}. Error : {ex.GetType().Name}", "Screenshot");

                return;
            }
            
            OpenHelper.OpenFolder(screenshotsDir);
        }

        public void OpenLogsFolder()
        {
            string logPath = AppDataManager.GetOrCreateLogsDir();
            if (!string.IsNullOrEmpty(logPath))
            {
                OpenHelper.OpenFolder(logPath);
            }
        }

        public void ToggleDockMode()
        {
            if (IsGameRunning)
            {
                ConfigurationState.Instance.System.EnableDockedMode.Toggle();
            }
        }

        public void ToggleVSyncMode()
        {
            AppHost.VSyncModeToggle();
            OnPropertyChanged(nameof(ShowCustomVSyncIntervalPicker));
        }

        public void VSyncModeSettingChanged()
        {
            if (_isGameRunning)
            {
                AppHost.Device.CustomVSyncInterval = ConfigurationState.Instance.Graphics.CustomVSyncInterval.Value;
                AppHost.Device.UpdateVSyncInterval();
            }

            CustomVSyncInterval = ConfigurationState.Instance.Graphics.CustomVSyncInterval.Value;
            OnPropertyChanged(nameof(ShowCustomVSyncIntervalPicker));
            OnPropertyChanged(nameof(CustomVSyncIntervalPercentageProxy));
            OnPropertyChanged(nameof(CustomVSyncIntervalPercentageText));
            OnPropertyChanged(nameof(CustomVSyncInterval));
        }

        public async Task ExitCurrentState()
        {
            if (WindowState is WindowState.FullScreen)
            {
                ToggleFullscreen();
            }
            else if (IsGameRunning)
            {
                await Task.Delay(100);

                AppHost?.ShowExitPrompt();
            }
        }

        public static void ChangeLanguage(object languageCode)
        {
            LocaleManager.Instance.LoadLanguage((string)languageCode);

            if (Program.PreviewerDetached)
            {
                ConfigurationState.Instance.UI.LanguageCode.Value = (string)languageCode;
                ConfigurationState.Instance.ToFileFormat().SaveConfig(Program.ConfigurationPath);
            }
        }

        public async Task ManageProfiles()
        {
            await NavigationDialogHost.Show(AccountManager, ContentManager, VirtualFileSystem, LibHacHorizonManager.RyujinxClient);
        }

        public void SimulateWakeUpMessage()
        {
            AppHost.Device.System.SimulateWakeUpMessage();
        }

        public async Task OpenFile()
        {
            IReadOnlyList<IStorageFile> result = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = LocaleManager.Instance[LocaleKeys.OpenFileDialogTitle],
                AllowMultiple = false,
                FileTypeFilter = new List<FilePickerFileType>
                {
                    new(LocaleManager.Instance[LocaleKeys.AllSupportedFormats])
                    {
                        Patterns = ["*.nsp", "*.xci", "*.nca", "*.nro", "*.nso"],
                        AppleUniformTypeIdentifiers =
                        [
                            "com.ryujinx.nsp",
                            "com.ryujinx.xci",
                            "com.ryujinx.nca",
                            "com.ryujinx.nro",
                            "com.ryujinx.nso"
                        ],
                        MimeTypes =
                        [
                            "application/x-nx-nsp",
                            "application/x-nx-xci",
                            "application/x-nx-nca",
                            "application/x-nx-nro",
                            "application/x-nx-nso"
                        ],
                    },
                    new("NSP")
                    {
                        Patterns = ["*.nsp"],
                        AppleUniformTypeIdentifiers = ["com.ryujinx.nsp"],
                        MimeTypes = ["application/x-nx-nsp"],
                    },
                    new("XCI")
                    {
                        Patterns = ["*.xci"],
                        AppleUniformTypeIdentifiers = ["com.ryujinx.xci"],
                        MimeTypes = ["application/x-nx-xci"],
                    },
                    new("NCA")
                    {
                        Patterns = ["*.nca"],
                        AppleUniformTypeIdentifiers = ["com.ryujinx.nca"],
                        MimeTypes = ["application/x-nx-nca"],
                    },
                    new("NRO")
                    {
                        Patterns = ["*.nro"],
                        AppleUniformTypeIdentifiers = ["com.ryujinx.nro"],
                        MimeTypes = ["application/x-nx-nro"],
                    },
                    new("NSO")
                    {
                        Patterns = ["*.nso"],
                        AppleUniformTypeIdentifiers = ["com.ryujinx.nso"],
                        MimeTypes = ["application/x-nx-nso"],
                    },
                },
            });

            if (result.Count > 0)
            {
                if (ApplicationLibrary.TryGetApplicationsFromFile(result[0].Path.LocalPath,
                        out List<ApplicationData> applications))
                {
                    await LoadApplication(applications[0]);
                }
                else
                {
                    await ContentDialogHelper.CreateErrorDialog(LocaleManager.Instance[LocaleKeys.MenuBarFileOpenFromFileError]);
                }
            }
        }

        public async Task LoadDlcFromFolder()
        {
            await LoadContentFromFolder(
                LocaleKeys.AutoloadDlcAddedMessage,
                LocaleKeys.AutoloadDlcRemovedMessage,
                ApplicationLibrary.AutoLoadDownloadableContents);
        }

        public async Task LoadTitleUpdatesFromFolder()
        {
            await LoadContentFromFolder(
                LocaleKeys.AutoloadUpdateAddedMessage,
                LocaleKeys.AutoloadUpdateRemovedMessage,
                ApplicationLibrary.AutoLoadTitleUpdates);
        }

        public async Task OpenFolder()
        {
            IReadOnlyList<IStorageFolder> result = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = LocaleManager.Instance[LocaleKeys.OpenFolderDialogTitle],
                AllowMultiple = false,
            });

            if (result.Count > 0)
            {
                ApplicationData applicationData = new()
                {
                    Name = Path.GetFileNameWithoutExtension(result[0].Path.LocalPath),
                    Path = result[0].Path.LocalPath,
                };

                await LoadApplication(applicationData);
            }
        }

        public bool InitializeUserConfig(ApplicationData application)
        {
            // Code where conditions will be met before loading the user configuration (Global Config)      
            BackendThreading backendThreadingValue = ConfigurationState.Instance.Graphics.BackendThreading.Value;
            string BackendThreadingInit = Program.BackendThreadingArg;

            if (BackendThreadingInit is null)
            {
                BackendThreadingInit = ConfigurationState.Instance.Graphics.BackendThreading.Value.ToString();
            }
            
            // If a configuration is found in the "/games/xxxxxxxxxxxxxx" folder, the program will load the user setting. 
            string idGame = application.IdBaseString;
            if (ConfigurationFileFormat.TryLoad(Program.GetDirGameUserConfig(idGame), out ConfigurationFileFormat configurationFileFormat))
            {
                // Loads the user configuration, having previously changed the global configuration to the user configuration
                ConfigurationState.Instance.Load(configurationFileFormat, Program.GetDirGameUserConfig(idGame, true, true), idGame);
            }

            // Code where conditions will be executed after loading user configuration
            if (ConfigurationState.Instance.Graphics.BackendThreading.Value.ToString() != BackendThreadingInit)
            {

                List<string> Arguments = new List<string>
                {
                    "--bt", ConfigurationState.Instance.Graphics.BackendThreading.Value.ToString() // BackendThreading
                };

                Rebooter.RebootAppWithGame(application.Path, Arguments);
 
                return true;
            }

            return false;
        }

        public async Task LoadApplication(ApplicationData application, bool startFullscreen = false, BlitStruct<ApplicationControlProperty>? customNacpData = null)
        {

            if (InitializeUserConfig(application))
            {
                return;
            }

            if (AppHost != null)
            {
                await ContentDialogHelper.CreateInfoDialog(
                    LocaleManager.Instance[LocaleKeys.DialogLoadAppGameAlreadyLoadedMessage],
                    LocaleManager.Instance[LocaleKeys.DialogLoadAppGameAlreadyLoadedSubMessage],
                    LocaleManager.Instance[LocaleKeys.InputDialogOk],
                    string.Empty,
                    LocaleManager.Instance[LocaleKeys.RyujinxInfo]);

                return;
            }

#if RELEASE
            await PerformanceCheck();
#endif
         
            Logger.RestartTime();

            SelectedIcon ??= ApplicationLibrary.GetApplicationIcon(application.Path, ConfigurationState.Instance.System.Language, application.Id);

            PrepareLoadScreen();

            RendererHostControl = new RendererHost();

            AppHost = new AppHost(
                RendererHostControl,
                InputManager,
                application.Path,
                application.Id,
                VirtualFileSystem,
                ContentManager,
                AccountManager,
                UserChannelPersistence,
                this,
                TopLevel);

            if (!await AppHost.LoadGuestApplication(customNacpData))
            {
                AppHost.DisposeContext();
                AppHost = null;

                return;
            }

            CanUpdate = false;

            LoadHeading = application.Name;

            if (string.IsNullOrWhiteSpace(application.Name))
            {
                LoadHeading = LocaleManager.Instance.UpdateAndGetDynamicValue(LocaleKeys.LoadingHeading, AppHost.Device.Processes.ActiveApplication.Name);
                application.Name = AppHost.Device.Processes.ActiveApplication.Name;
            }

            SwitchToRenderer(startFullscreen);

            _currentApplicationData = application;

            Thread gameThread = new(InitializeGame) { Name = "GUI.WindowThread" };
            gameThread.Start();
            
        }

        public void SwitchToRenderer(bool startFullscreen) =>
            Dispatcher.UIThread.Post(() =>
            {
                SwitchToGameControl(startFullscreen);

                SetMainContent(RendererHostControl);

                RendererHostControl.Focus();
            });

        public static void UpdateGameMetadata(string titleId)
            => ApplicationLibrary.LoadAndSaveMetaData(titleId, appMetadata => appMetadata.UpdatePostGame());

        public void RefreshFirmwareStatus()
        {
            SystemVersion version = null;
            try
            {
                version = ContentManager.GetCurrentFirmwareVersion();
            }
            catch (Exception)
            {
                // ignored
            }

            bool hasApplet = false;

            if (version != null)
            {
                LocaleManager.Instance.UpdateAndGetDynamicValue(LocaleKeys.StatusBarSystemVersion, version.VersionString);

                hasApplet = version.Major > 3;
            }
            else
            {
                LocaleManager.Instance.UpdateAndGetDynamicValue(LocaleKeys.StatusBarSystemVersion, "NaN");
            }

            IsAppletMenuActive = hasApplet;
        }

        public void AppHost_AppExit(object sender, EventArgs e)
        {
            if (IsClosing)
            {
                return;
            }

            IsGameRunning = false;

            Dispatcher.UIThread.InvokeAsync(async () =>
            {
                ShowMenuAndStatusBar = true;
                ShowContent = true;
                ShowLoadProgress = false;
                IsLoadingIndeterminate = false;
                CanUpdate = true;
                Cursor = Cursor.Default;

                SetMainContent(null);

                AppHost = null;

                await HandleRelaunch();
            });

            RendererHostControl.WindowCreated -= RendererHost_Created;
            RendererHostControl = null;

            SelectedIcon = null;

            Dispatcher.UIThread.InvokeAsync(() =>
            {
                Title = RyujinxApp.FormatTitle();
            });
        }

        public async Task OpenAmiiboWindow()
        {
            if (AppHost.Device.System.SearchingForAmiibo(out int deviceId) && IsGameRunning)
            {
                string titleId = AppHost.Device.Processes.ActiveApplication.ProgramIdText.ToUpper();
                AmiiboWindow window = new(ShowAll, LastScannedAmiiboId, titleId);

                await StyleableAppWindow.ShowAsync(window);

                if (window.IsScanned)
                {
                    ShowAll = window.ViewModel.ShowAllAmiibo;
                    LastScannedAmiiboId = window.ScannedAmiibo.GetId();

                    AppHost.Device.System.ScanAmiibo(deviceId, LastScannedAmiiboId, window.ViewModel.UseRandomUuid);
                }
            }
        }
        public async Task OpenBinFile()
        {
            if (AppHost.Device.System.SearchingForAmiibo(out _) && IsGameRunning)
            {
                IReadOnlyList<IStorageFile> result = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
                {
                    Title = LocaleManager.Instance[LocaleKeys.OpenFileDialogTitle],
                    AllowMultiple = false,
                    FileTypeFilter = new List<FilePickerFileType>
                    {
                        new(LocaleManager.Instance[LocaleKeys.AllSupportedFormats])
                        {
                            Patterns = ["*.bin"],
                        }
                    }
                });
                if (result.Count > 0)
                {
                    AppHost.Device.System.ScanAmiiboFromBin(result[0].Path.LocalPath);
                }
            }
        }


        public void ToggleFullscreen()
        {
            if (Environment.TickCount64 - LastFullscreenToggle < HotKeyPressDelayMs)
            {
                return;
            }

            LastFullscreenToggle = Environment.TickCount64;

            if (WindowState is not WindowState.Normal)
            {
                WindowState = WindowState.Normal;
                Window.TitleBar.ExtendsContentIntoTitleBar = !ConfigurationState.Instance.ShowTitleBar;

                if (IsGameRunning)
                {
                    ShowMenuAndStatusBar = true;
                }
            }
            else
            {
                WindowState = WindowState.FullScreen;
                Window.TitleBar.ExtendsContentIntoTitleBar = true;

                if (IsGameRunning)
                {
                    ShowMenuAndStatusBar = false;
                }
            }

            IsFullScreen = WindowState is WindowState.FullScreen;
        }

        public static void SaveConfig()
        {
            ConfigurationState.Instance.ToFileFormat().SaveConfig(Program.ConfigurationPath);
        }

        public static async Task PerformanceCheck()
        {
            if (ConfigurationState.Instance.Logger.EnableTrace.Value)
            {
                string mainMessage = LocaleManager.Instance[LocaleKeys.DialogPerformanceCheckLoggingEnabledMessage];
                string secondaryMessage = LocaleManager.Instance[LocaleKeys.DialogPerformanceCheckLoggingEnabledConfirmMessage];

                UserResult result = await ContentDialogHelper.CreateLocalizedConfirmationDialog(mainMessage, secondaryMessage);

                if (result == UserResult.Yes)
                {
                    ConfigurationState.Instance.Logger.EnableTrace.Value = false;

                    SaveConfig();
                }
            }

            if (!string.IsNullOrWhiteSpace(ConfigurationState.Instance.Graphics.ShadersDumpPath.Value))
            {
                string mainMessage = LocaleManager.Instance[LocaleKeys.DialogPerformanceCheckShaderDumpEnabledMessage];
                string secondaryMessage = LocaleManager.Instance[LocaleKeys.DialogPerformanceCheckShaderDumpEnabledConfirmMessage];

                UserResult result = await ContentDialogHelper.CreateLocalizedConfirmationDialog(mainMessage, secondaryMessage);

                if (result == UserResult.Yes)
                {
                    ConfigurationState.Instance.Graphics.ShadersDumpPath.Value = string.Empty;

                    SaveConfig();
                }
            }
        }

        public async void ProcessTrimResult(String filename, XCIFileTrimmer.OperationOutcome operationOutcome)
        {
            string notifyUser = operationOutcome.ToLocalisedText();

            if (notifyUser != null)
            {
                await ContentDialogHelper.CreateWarningDialog(
                    LocaleManager.Instance[LocaleKeys.TrimXCIFileFailedPrimaryText],
                    notifyUser
                );
            }
            else
            {
                switch (operationOutcome)
                {
                    case XCIFileTrimmer.OperationOutcome.Successful:
                        RyujinxApp.MainWindow.LoadApplications();
                        break;
                }
            }
        }

        public async Task TrimXCIFile(string filename)
        {
            if (filename == null)
            {
                return;
            }

            XCIFileTrimmer trimmer = new(filename, new XCITrimmerLog.MainWindow(this));

            if (trimmer.CanBeTrimmed)
            {
                double savings = (double)trimmer.DiskSpaceSavingsB / 1024.0 / 1024.0;
                double currentFileSize = (double)trimmer.FileSizeB / 1024.0 / 1024.0;
                double cartDataSize = (double)trimmer.DataSizeB / 1024.0 / 1024.0;
                string secondaryText = LocaleManager.Instance.UpdateAndGetDynamicValue(LocaleKeys.TrimXCIFileDialogSecondaryText, currentFileSize, cartDataSize, savings);

                UserResult result = await ContentDialogHelper.CreateConfirmationDialog(
                    LocaleManager.Instance[LocaleKeys.TrimXCIFileDialogPrimaryText],
                    secondaryText,
                    LocaleManager.Instance[LocaleKeys.Continue],
                    LocaleManager.Instance[LocaleKeys.Cancel],
                    LocaleManager.Instance[LocaleKeys.TrimXCIFileDialogTitle]
                );

                if (result == UserResult.Yes)
                {
                    Thread XCIFileTrimThread = new(() =>
                    {
                        Dispatcher.UIThread.Post(() =>
                        {
                            StatusBarProgressStatusText = LocaleManager.Instance.UpdateAndGetDynamicValue(LocaleKeys.StatusBarXCIFileTrimming, Path.GetFileName(filename));
                            StatusBarProgressStatusVisible = true;
                            StatusBarProgressMaximum = 1;
                            StatusBarProgressValue = 0;
                            StatusBarVisible = true;
                        });

                        try
                        {
                            XCIFileTrimmer.OperationOutcome operationOutcome = trimmer.Trim();

                            Dispatcher.UIThread.Post(() =>
                            {
                                ProcessTrimResult(filename, operationOutcome);
                            });
                        }
                        finally
                        {
                            Dispatcher.UIThread.Post(() =>
                            {
                                StatusBarProgressStatusVisible = false;
                                StatusBarProgressStatusText = string.Empty;
                                StatusBarVisible = false;
                            });
                        }
                    })
                    {
                        Name = "GUI.XCIFileTrimmerThread",
                        IsBackground = true,
                    };
                    XCIFileTrimThread.Start();
                }
            }
        }

        #endregion
    }
}
