using Avalonia.Threading;
using FluentAvalonia.UI.Controls;
using Gommon;
using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Tar;
using ICSharpCode.SharpZipLib.Zip;
using Ryujinx.Ava.Common.Locale;
using Ryujinx.Ava.Common.Models.Github;
using Ryujinx.Ava.UI.Helpers;
using Ryujinx.Ava.Utilities;
using Ryujinx.Common;
using Ryujinx.Common.Helper;
using Ryujinx.Common.Logging;
using Ryujinx.Common.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ryujinx.Ava
{
    internal static class Updater
    {
        private const string GitHubApiUrl = "https://api.github.com";
        private const string LatestReleaseUrl = 
            $"{GitHubApiUrl}/repos/{ReleaseInformation.ReleaseChannelOwner}/{ReleaseInformation.ReleaseChannelRepo}/releases/latest";
        
        private static readonly GithubReleasesJsonSerializerContext _serializerContext = new(JsonHelper.GetDefaultSerializerOptions());

        private static readonly string _homeDir = AppDomain.CurrentDomain.BaseDirectory;
        private static readonly string _updateDir = Path.Combine(Path.GetTempPath(), "Ryujinx", "update");
        private static readonly string _updatePublishDir = Path.Combine(_updateDir, "publish");
        private const int ConnectionCount = 4;

        private static string _buildVer;
        

        private static readonly string _platformExt = BuildPlatformExtension();
        
        private static string _buildUrl;
        private static long _buildSize;
        private static bool _updateSuccessful;
        private static bool _running;

        private static readonly string[] _windowsDependencyDirs = [];

        public static async Task<Optional<(Version Current, Version Incoming)>> CheckVersionAsync(bool showVersionUpToDate = false)
        {
            if (!Version.TryParse(Program.Version, out Version currentVersion))
            {
                Logger.Error?.Print(LogClass.Application, $"Failed to convert the current {RyujinxApp.FullAppName} version!");

                await ContentDialogHelper.CreateWarningDialog(
                    LocaleManager.Instance[LocaleKeys.DialogUpdaterConvertFailedMessage],
                    LocaleManager.Instance[LocaleKeys.DialogUpdaterCancelUpdateMessage]);

                _running = false;

                return default;
            }
            
            Logger.Info?.Print(LogClass.Application, "Checking for updates.");

            // Get latest version number from GitHub API
            try
            {
                using HttpClient jsonClient = ConstructHttpClient();
                
                string fetchedJson = await jsonClient.GetStringAsync(LatestReleaseUrl);
                GithubReleasesJsonResponse fetched = JsonHelper.Deserialize(fetchedJson, _serializerContext.GithubReleasesJsonResponse);
                _buildVer = fetched.TagName;

                foreach (GithubReleaseAssetJsonResponse asset in fetched.Assets)
                {
                    if (asset.Name.StartsWith("ryujinx") && asset.Name.EndsWith(_platformExt))
                    {
                        _buildUrl = asset.BrowserDownloadUrl;

                        if (asset.State != "uploaded")
                        {
                            if (showVersionUpToDate)
                            {
                                UserResult userResult = await ContentDialogHelper.CreateUpdaterUpToDateInfoDialog(
                                    LocaleManager.Instance[LocaleKeys.DialogUpdaterAlreadyOnLatestVersionMessage],
                                    string.Empty);

                                if (userResult is UserResult.Ok)
                                {
                                    OpenHelper.OpenUrl(ReleaseInformation.GetChangelogForVersion(currentVersion));
                                }
                            }
                            
                            Logger.Info?.Print(LogClass.Application, "Up to date.");

                            _running = false;

                            return default;
                        }

                        break;
                    }
                }

                // If build not done, assume no new update are available.
                if (_buildUrl is null)
                {
                    if (showVersionUpToDate)
                    {
                        UserResult userResult = await ContentDialogHelper.CreateUpdaterUpToDateInfoDialog(
                            LocaleManager.Instance[LocaleKeys.DialogUpdaterAlreadyOnLatestVersionMessage],
                            string.Empty);

                        if (userResult is UserResult.Ok)
                        {
                            OpenHelper.OpenUrl(ReleaseInformation.GetChangelogForVersion(currentVersion));
                        }
                    }
                    
                    Logger.Info?.Print(LogClass.Application, "Up to date.");

                    _running = false;

                    return default;
                }
            }
            catch (Exception exception)
            {
                Logger.Error?.Print(LogClass.Application, exception.Message);

                await ContentDialogHelper.CreateErrorDialog(
                    LocaleManager.Instance[LocaleKeys.DialogUpdaterFailedToGetVersionMessage]);

                _running = false;

                return default;
            }

            if (!Version.TryParse(_buildVer, out Version newVersion))
            {
                Logger.Error?.Print(LogClass.Application, $"Failed to convert the received {RyujinxApp.FullAppName} version from GitHub!");

                await ContentDialogHelper.CreateWarningDialog(
                    LocaleManager.Instance[LocaleKeys.DialogUpdaterConvertFailedGithubMessage],
                    LocaleManager.Instance[LocaleKeys.DialogUpdaterCancelUpdateMessage]);

                _running = false;

                return default;
            }

            return (currentVersion, newVersion);
        }
        
        public static async Task BeginUpdateAsync(bool showVersionUpToDate = false)
        {
            if (_running)
            {
                return;
            }

            _running = true;

            Optional<(Version, Version)> versionTuple = await CheckVersionAsync(showVersionUpToDate);

            if (_running is false || !versionTuple.HasValue) return;

            (Version currentVersion, Version newVersion) = versionTuple.Value;

            if (newVersion <= currentVersion)
            {
                if (showVersionUpToDate)
                {
                    UserResult userResult = await ContentDialogHelper.CreateUpdaterUpToDateInfoDialog(
                        LocaleManager.Instance[LocaleKeys.DialogUpdaterAlreadyOnLatestVersionMessage],
                        string.Empty);

                    if (userResult is UserResult.Ok)
                    {
                        OpenHelper.OpenUrl(ReleaseInformation.GetChangelogForVersion(currentVersion));
                    }
                }
                
                Logger.Info?.Print(LogClass.Application, "Up to date.");

                _running = false;

                return;
            }

            // Fetch build size information to learn chunk sizes.
            using HttpClient buildSizeClient = ConstructHttpClient();
            try
            {
                buildSizeClient.DefaultRequestHeaders.Add("Range", "bytes=0-0");

                HttpResponseMessage message = await buildSizeClient.GetAsync(new Uri(_buildUrl), HttpCompletionOption.ResponseHeadersRead);

                _buildSize = message.Content.Headers.ContentRange.Length.Value;
            }
            catch (Exception ex)
            {
                Logger.Warning?.Print(LogClass.Application, ex.Message);
                Logger.Warning?.Print(LogClass.Application, "Couldn't determine build size for update, using single-threaded updater");

                _buildSize = -1;
            }

            await Dispatcher.UIThread.InvokeAsync(async () =>
            {
                string newVersionString = ReleaseInformation.IsCanaryBuild
                    ? $"Canary {currentVersion} -> Canary {newVersion}"
                    : $"{currentVersion} -> {newVersion}";
                
                Logger.Info?.Print(LogClass.Application, $"Version found: {newVersionString}");
                
            RequestUserToUpdate:
                // Show a message asking the user if they want to update
                UserResult shouldUpdate = await ContentDialogHelper.CreateUpdaterChoiceDialog(
                    LocaleManager.Instance[LocaleKeys.RyujinxUpdater],
                    LocaleManager.Instance[LocaleKeys.RyujinxUpdaterMessage],
                    newVersionString);

                switch (shouldUpdate)
                {
                    case UserResult.Yes:
                        await UpdateRyujinx(_buildUrl);
                        break;
                    // Secondary button maps to no, which in this case is the show changelog button.
                    case UserResult.No:
                        OpenHelper.OpenUrl(ReleaseInformation.GetChangelogUrl(currentVersion, newVersion));
                        goto RequestUserToUpdate;
                    default:
                        _running = false;
                        break;
                }
            });
        }

        private static HttpClient ConstructHttpClient()
        {
            HttpClient result = new();

            // Required by GitHub to interact with APIs.
            result.DefaultRequestHeaders.Add("User-Agent", "Ryujinx-Updater/1.0.0");

            return result;
        }

        private static async Task UpdateRyujinx(string downloadUrl)
        {
            _updateSuccessful = false;

            // Empty update dir, although it shouldn't ever have anything inside it
            if (Directory.Exists(_updateDir))
            {
                Directory.Delete(_updateDir, true);
            }

            Directory.CreateDirectory(_updateDir);

            string updateFile = Path.Combine(_updateDir, "update.bin");

            TaskDialog taskDialog = new()
            {
                Header = LocaleManager.Instance[LocaleKeys.RyujinxUpdater],
                SubHeader = LocaleManager.Instance[LocaleKeys.UpdaterDownloading],
                IconSource = new SymbolIconSource { Symbol = Symbol.Download },
                ShowProgressBar = true,
                XamlRoot = RyujinxApp.MainWindow,
            };

            taskDialog.Opened += (s, e) =>
            {
                if (_buildSize >= 0)
                {
                    DoUpdateWithMultipleThreads(taskDialog, downloadUrl, updateFile);
                }
                else
                {
                    DoUpdateWithSingleThread(taskDialog, downloadUrl, updateFile);
                }
            };

            await taskDialog.ShowAsync(true);

            if (_updateSuccessful)
            {
                bool shouldRestart = true;

                if (!OperatingSystem.IsMacOS())
                {
                    shouldRestart = await ContentDialogHelper.CreateChoiceDialog(LocaleManager.Instance[LocaleKeys.RyujinxUpdater],
                        LocaleManager.Instance[LocaleKeys.DialogUpdaterCompleteMessage],
                        LocaleManager.Instance[LocaleKeys.DialogUpdaterRestartMessage]);
                }

                if (shouldRestart)
                {
                    List<string> arguments = CommandLineState.Arguments.ToList();
                    string executableDirectory = AppDomain.CurrentDomain.BaseDirectory;

                    // On macOS we perform the update at relaunch.
                    if (OperatingSystem.IsMacOS())
                    {
                        string baseBundlePath = Path.GetFullPath(Path.Combine(executableDirectory, "..", ".."));
                        string newBundlePath = Path.Combine(_updateDir, "Ryujinx.app");
                        string updaterScriptPath = Path.Combine(newBundlePath, "Contents", "Resources", "updater.sh");
                        string currentPid = Environment.ProcessId.ToString();

                        arguments.InsertRange(0, new List<string> { updaterScriptPath, baseBundlePath, newBundlePath, currentPid });
                        Process.Start("/bin/bash", arguments);
                    }
                    else
                    {
                        // Find the process name.
                        string ryuName = Path.GetFileName(Environment.ProcessPath) ?? string.Empty;

                        // Some operating systems can see the renamed executable, so strip off the .ryuold if found.
                        if (ryuName.EndsWith(".ryuold"))
                        {
                            ryuName = ryuName[..^7];
                        }

                        // Fallback if the executable could not be found.
                        if (ryuName.Length == 0 || !Path.Exists(Path.Combine(executableDirectory, ryuName)))
                        {
                            ryuName = OperatingSystem.IsWindows() ? "Ryujinx.exe" : "Ryujinx";
                        }

                        ProcessStartInfo processStart = new(ryuName)
                        {
                            UseShellExecute = true,
                            WorkingDirectory = executableDirectory,
                        };

                        foreach (string argument in CommandLineState.Arguments)
                        {
                            processStart.ArgumentList.Add(argument);
                        }

                        Process.Start(processStart);
                    }

                    Environment.Exit(0);
                }
            }
        }

        private static void DoUpdateWithMultipleThreads(TaskDialog taskDialog, string downloadUrl, string updateFile)
        {
            // Multi-Threaded Updater
            long chunkSize = _buildSize / ConnectionCount;
            long remainderChunk = _buildSize % ConnectionCount;

            int completedRequests = 0;
            int totalProgressPercentage = 0;
            int[] progressPercentage = new int[ConnectionCount];

            List<byte[]> list = new(ConnectionCount);
            List<WebClient> webClients = new(ConnectionCount);

            for (int i = 0; i < ConnectionCount; i++)
            {
                list.Add([]);
            }

            for (int i = 0; i < ConnectionCount; i++)
            {
#pragma warning disable SYSLIB0014
                // TODO: WebClient is obsolete and need to be replaced with a more complex logic using HttpClient.
                using WebClient client = new();
#pragma warning restore SYSLIB0014

                webClients.Add(client);

                if (i == ConnectionCount - 1)
                {
                    client.Headers.Add("Range", $"bytes={chunkSize * i}-{(chunkSize * (i + 1) - 1) + remainderChunk}");
                }
                else
                {
                    client.Headers.Add("Range", $"bytes={chunkSize * i}-{chunkSize * (i + 1) - 1}");
                }

                client.DownloadProgressChanged += (_, args) =>
                {
                    int index = (int)args.UserState;

                    Interlocked.Add(ref totalProgressPercentage, -1 * progressPercentage[index]);
                    Interlocked.Exchange(ref progressPercentage[index], args.ProgressPercentage);
                    Interlocked.Add(ref totalProgressPercentage, args.ProgressPercentage);

                    taskDialog.SetProgressBarState(totalProgressPercentage / ConnectionCount, TaskDialogProgressState.Normal);
                };

                client.DownloadDataCompleted += (_, args) =>
                {
                    int index = (int)args.UserState;

                    if (args.Cancelled)
                    {
                        webClients[index].Dispose();

                        taskDialog.Hide();

                        return;
                    }

                    list[index] = args.Result;
                    Interlocked.Increment(ref completedRequests);

                    if (Equals(completedRequests, ConnectionCount))
                    {
                        byte[] mergedFileBytes = new byte[_buildSize];
                        for (int connectionIndex = 0, destinationOffset = 0; connectionIndex < ConnectionCount; connectionIndex++)
                        {
                            Array.Copy(list[connectionIndex], 0, mergedFileBytes, destinationOffset, list[connectionIndex].Length);
                            destinationOffset += list[connectionIndex].Length;
                        }

                        File.WriteAllBytes(updateFile, mergedFileBytes);

                        // On macOS, ensure that we remove the quarantine bit to prevent Gatekeeper from blocking execution.
                        if (OperatingSystem.IsMacOS())
                        {
                            using Process xattrProcess = Process.Start("xattr", 
                                [ "-d", "com.apple.quarantine", updateFile ]);

                            xattrProcess.WaitForExit();
                        }

                        try
                        {
                            InstallUpdate(taskDialog, updateFile);
                        }
                        catch (Exception e)
                        {
                            Logger.Warning?.Print(LogClass.Application, e.Message);
                            Logger.Warning?.Print(LogClass.Application, "Multi-Threaded update failed, falling back to single-threaded updater.");

                            DoUpdateWithSingleThread(taskDialog, downloadUrl, updateFile);
                        }
                    }
                };

                try
                {
                    client.DownloadDataAsync(new Uri(downloadUrl), i);
                }
                catch (WebException ex)
                {
                    Logger.Warning?.Print(LogClass.Application, ex.Message);
                    Logger.Warning?.Print(LogClass.Application, "Multi-Threaded update failed, falling back to single-threaded updater.");

                    foreach (WebClient webClient in webClients)
                    {
                        webClient.CancelAsync();
                    }

                    DoUpdateWithSingleThread(taskDialog, downloadUrl, updateFile);

                    return;
                }
            }
        }

        private static void DoUpdateWithSingleThreadWorker(TaskDialog taskDialog, string downloadUrl, string updateFile)
        {
            using HttpClient client = new();
            // We do not want to timeout while downloading
            client.Timeout = TimeSpan.FromDays(1);

            using HttpResponseMessage response = client.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead).Result;
            using Stream remoteFileStream = response.Content.ReadAsStreamAsync().Result;
            using Stream updateFileStream = File.Open(updateFile, FileMode.Create);

            long totalBytes = response.Content.Headers.ContentLength.Value;
            long bytesWritten = 0;

            byte[] buffer = new byte[32 * 1024];

            while (true)
            {
                int readSize = remoteFileStream.Read(buffer);

                if (readSize == 0)
                {
                    break;
                }

                bytesWritten += readSize;

                taskDialog.SetProgressBarState(GetPercentage(bytesWritten, totalBytes), TaskDialogProgressState.Normal);
                RyujinxApp.SetTaskbarProgressValue(bytesWritten, totalBytes);

                updateFileStream.Write(buffer, 0, readSize);
            }

            InstallUpdate(taskDialog, updateFile);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static double GetPercentage(double value, double max)
        {
            return max == 0 ? 0 : value / max * 100;
        }

        private static void DoUpdateWithSingleThread(TaskDialog taskDialog, string downloadUrl, string updateFile)
        {
            Thread worker = new(() => DoUpdateWithSingleThreadWorker(taskDialog, downloadUrl, updateFile))
            {
                Name = "Updater.SingleThreadWorker",
            };

            worker.Start();
        }

        [SupportedOSPlatform("linux")]
        [SupportedOSPlatform("macos")]
        private static void ExtractTarGzipFile(TaskDialog taskDialog, string archivePath, string outputDirectoryPath)
        {
            using Stream inStream = File.OpenRead(archivePath);
            using GZipInputStream gzipStream = new(inStream);
            using TarInputStream tarStream = new(gzipStream, Encoding.ASCII);

            TarEntry tarEntry;

            while ((tarEntry = tarStream.GetNextEntry()) is not null)
            {
                if (tarEntry.IsDirectory)
                {
                    continue;
                }

                string outPath = Path.Combine(outputDirectoryPath, tarEntry.Name);

                Directory.CreateDirectory(Path.GetDirectoryName(outPath));

                using FileStream outStream = File.OpenWrite(outPath);
                tarStream.CopyEntryContents(outStream);

                File.SetUnixFileMode(outPath, (UnixFileMode)tarEntry.TarHeader.Mode);
                File.SetLastWriteTime(outPath, DateTime.SpecifyKind(tarEntry.ModTime, DateTimeKind.Utc));

                Dispatcher.UIThread.Post(() =>
                {
                    if (tarEntry is null)
                    {
                        return;
                    }

                    taskDialog.SetProgressBarState(GetPercentage(tarEntry.Size, inStream.Length), TaskDialogProgressState.Normal);
                });
            }
        }

        private static void ExtractZipFile(TaskDialog taskDialog, string archivePath, string outputDirectoryPath)
        {
            using Stream inStream = File.OpenRead(archivePath);
            using ZipFile zipFile = new(inStream);

            double count = 0;
            foreach (ZipEntry zipEntry in zipFile)
            {
                count++;
                if (zipEntry.IsDirectory)
                {
                    continue;
                }

                string outPath = Path.Combine(outputDirectoryPath, zipEntry.Name);

                Directory.CreateDirectory(Path.GetDirectoryName(outPath));

                using Stream zipStream = zipFile.GetInputStream(zipEntry);
                using FileStream outStream = File.OpenWrite(outPath);

                zipStream.CopyTo(outStream);

                File.SetLastWriteTime(outPath, DateTime.SpecifyKind(zipEntry.DateTime, DateTimeKind.Utc));

                Dispatcher.UIThread.Post(() =>
                {
                    taskDialog.SetProgressBarState(GetPercentage(count, zipFile.Count), TaskDialogProgressState.Normal);
                });
            }
        }

        private static void InstallUpdate(TaskDialog taskDialog, string updateFile)
        {
            // Extract Update
            taskDialog.SubHeader = LocaleManager.Instance[LocaleKeys.UpdaterExtracting];
            taskDialog.SetProgressBarState(0, TaskDialogProgressState.Normal);

            if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
            {
                ExtractTarGzipFile(taskDialog, updateFile, _updateDir);
            }
            else if (OperatingSystem.IsWindows())
            {
                ExtractZipFile(taskDialog, updateFile, _updateDir);
            }
            else
            {
                throw new NotSupportedException();
            }

            // Delete downloaded zip
            File.Delete(updateFile);

            List<string> allFiles = EnumerateFilesToDelete().ToList();

            taskDialog.SubHeader = LocaleManager.Instance[LocaleKeys.UpdaterRenaming];
            taskDialog.SetProgressBarState(0, TaskDialogProgressState.Normal);

            // NOTE: On macOS, replacement is delayed to the restart phase.
            if (!OperatingSystem.IsMacOS())
            {
                // Replace old files
                double count = 0;
                foreach (string file in allFiles)
                {
                    count++;
                    try
                    {
                        File.Move(file, file + ".ryuold");

                        Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            taskDialog.SetProgressBarState(GetPercentage(count, allFiles.Count), TaskDialogProgressState.Normal);
                        });
                    }
                    catch
                    {
                        Logger.Warning?.Print(LogClass.Application, LocaleManager.Instance.UpdateAndGetDynamicValue(LocaleKeys.UpdaterRenameFailed, file));
                    }
                }

                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    taskDialog.SubHeader = LocaleManager.Instance[LocaleKeys.UpdaterAddingFiles];
                    taskDialog.SetProgressBarState(0, TaskDialogProgressState.Normal);
                });

                MoveAllFilesOver(_updatePublishDir, _homeDir, taskDialog);

                Directory.Delete(_updateDir, true);
            }

            _updateSuccessful = true;

            taskDialog.Hide();
        }

        public static bool CanUpdate(bool showWarnings = false)
        {
#if !DISABLE_UPDATER
            if (!NetworkInterface.GetIsNetworkAvailable())
            {
                if (showWarnings)
                {
                    Dispatcher.UIThread.InvokeAsync(() =>
                        ContentDialogHelper.CreateWarningDialog(
                            LocaleManager.Instance[LocaleKeys.DialogUpdaterNoInternetMessage],
                            LocaleManager.Instance[LocaleKeys.DialogUpdaterNoInternetSubMessage])
                    );
                }

                return false;
            }

            if (Program.Version.Contains("dirty") || !ReleaseInformation.IsValid)
            {
                if (showWarnings)
                {
                    Dispatcher.UIThread.InvokeAsync(() =>
                        ContentDialogHelper.CreateWarningDialog(
                            LocaleManager.Instance[LocaleKeys.DialogUpdaterDirtyBuildMessage],
                            LocaleManager.Instance[LocaleKeys.DialogUpdaterDirtyBuildSubMessage])
                    );
                }

                return false;
            }

            return true;
#else
            if (showWarnings)
            {
                Dispatcher.UIThread.InvokeAsync(() =>
                    ContentDialogHelper.CreateWarningDialog(
                        LocaleManager.Instance[LocaleKeys.UpdaterDisabledWarningTitle],
                        LocaleManager.Instance[LocaleKeys.DialogUpdaterDirtyBuildSubMessage])
                    );
            }

            return false;
#endif
        }

        // NOTE: This method should always reflect the latest build layout.
        private static IEnumerable<string> EnumerateFilesToDelete()
        {
            IEnumerable<string> files = Directory.EnumerateFiles(_homeDir); // All files directly in base dir.

            // Determine and exclude user files only when the updater is running, not when cleaning old files
            if (_running && !OperatingSystem.IsMacOS())
            {
                // Compare the loose files in base directory against the loose files from the incoming update, and store foreign ones in a user list.
                IEnumerable<string> oldFiles = Directory.EnumerateFiles(_homeDir, "*", SearchOption.TopDirectoryOnly).Select(Path.GetFileName);
                IEnumerable<string> newFiles = Directory.EnumerateFiles(_updatePublishDir, "*", SearchOption.TopDirectoryOnly).Select(Path.GetFileName);
                IEnumerable<string> userFiles = oldFiles.Except(newFiles).Select(filename => Path.Combine(_homeDir, filename));

                // Remove user files from the paths in files.
                files = files.Except(userFiles);
            }

            if (OperatingSystem.IsWindows())
            {
                foreach (string dir in _windowsDependencyDirs)
                {
                    string dirPath = Path.Combine(_homeDir, dir);
                    if (Directory.Exists(dirPath))
                    {
                        files = files.Concat(Directory.EnumerateFiles(dirPath, "*", SearchOption.AllDirectories));
                    }
                }
            }

            return files.Where(f => !new FileInfo(f).Attributes.HasFlag(FileAttributes.Hidden | FileAttributes.System));
        }

        private static void MoveAllFilesOver(string root, string dest, TaskDialog taskDialog)
        {
            int total = Directory.GetFiles(root, "*", SearchOption.AllDirectories).Length;
            foreach (string directory in Directory.GetDirectories(root))
            {
                string dirName = Path.GetFileName(directory);

                if (!Directory.Exists(Path.Combine(dest, dirName)))
                {
                    Directory.CreateDirectory(Path.Combine(dest, dirName));
                }

                MoveAllFilesOver(directory, Path.Combine(dest, dirName), taskDialog);
            }

            double count = 0;
            foreach (string file in Directory.GetFiles(root))
            {
                count++;

                File.Move(file, Path.Combine(dest, Path.GetFileName(file)), true);

                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    taskDialog.SetProgressBarState(GetPercentage(count, total), TaskDialogProgressState.Normal);
                });
            }
        }

        public static void CleanupUpdate() =>
            Directory.GetFiles(_homeDir, "*.ryuold", SearchOption.AllDirectories)
                .ForEach(File.Delete);
        
        private static string BuildPlatformExtension()
        {
            if (RunningPlatform.IsMacOS)
                return "macos_universal.app.tar.gz";

#pragma warning disable CS8509 // It is exhaustive for any values this can contain.
            string osPrefix = RunningPlatform.CurrentOS switch
            {
                OperatingSystemType.Linux => "linux",
                OperatingSystemType.Windows => "win"
            };

            string archSuffix = RunningPlatform.Architecture switch
            {
                Architecture.Arm64 => "arm64",
                Architecture.X64 => "x64",
                _ => throw new PlatformNotSupportedException($"Unknown architecture {Enum.GetName(RunningPlatform.Architecture)}."),
            };
            
            string fileExtension = RunningPlatform.CurrentOS switch
#pragma warning restore CS8509
            {
                OperatingSystemType.Linux => "tar.gz",
                OperatingSystemType.Windows => "zip"
            };

            return $"{osPrefix}_{archSuffix}.{fileExtension}";
        }
    }
}
