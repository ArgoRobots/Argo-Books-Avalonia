using System.Diagnostics;
using ArgoBooks.Core.Services;
using NetSparkleUpdater;
using NetSparkleUpdater.Enums;
using NetSparkleUpdater.SignatureVerifiers;

namespace ArgoBooks.Desktop.Services;

/// <summary>
/// Desktop auto-update service built on NetSparkleUpdater.
/// Handles checking, downloading, and applying updates across Windows, macOS, and Linux.
/// Uses a custom UI (the existing CheckForUpdateModal) instead of NetSparkle's built-in UI.
/// </summary>
public sealed class NetSparkleUpdateService : IUpdateService, IDisposable
{
    /// <summary>
    /// AppCast URL — separate from the WinForms app since version tracks diverge.
    /// </summary>
    private const string AppCastUrl = "https://argorobots.com/avalonia-update.xml";

    /// <summary>
    /// Download timeout for the update package.
    /// </summary>
    private static readonly TimeSpan DownloadTimeout = TimeSpan.FromMinutes(10);

    private readonly SparkleUpdater _sparkle;
    private readonly IErrorLogger? _errorLogger;
    private string? _installerPath;

    /// <inheritdoc />
    public UpdateState State { get; private set; } = UpdateState.Idle;

    /// <inheritdoc />
    public Core.Services.UpdateInfo? AvailableUpdate { get; private set; }

    /// <inheritdoc />
    public string? LastError { get; private set; }

    /// <inheritdoc />
    public string? InstallerPath => _installerPath;

    /// <inheritdoc />
    public event EventHandler<UpdateState>? StateChanged;

    /// <inheritdoc />
    public event EventHandler<int>? DownloadProgressChanged;

    /// <inheritdoc />
    public event EventHandler? ApplyingUpdate;

    /// <summary>
    /// Creates a new NetSparkleUpdateService.
    /// </summary>
    /// <param name="errorLogger">Optional error logger for diagnostics.</param>
    public NetSparkleUpdateService(IErrorLogger? errorLogger = null)
    {
        _errorLogger = errorLogger;

        // Use Ed25519 with UseIfPossible — will verify signatures when a public key is present,
        // but won't block updates if no key is configured yet.
        _sparkle = new SparkleUpdater(AppCastUrl, new Ed25519Checker(SecurityMode.UseIfPossible))
        {
            UIFactory = null, // We use our own UI
            RelaunchAfterUpdate = false
        };
    }

    /// <inheritdoc />
    public async Task<Core.Services.UpdateInfo?> CheckForUpdateAsync()
    {
        // Clean up installer files from any previous update session.
        // By this point, any prior update has already been applied (or abandoned).
        CleanupPreviousUpdateFiles();

        SetState(UpdateState.Checking);
        AvailableUpdate = null;
        LastError = null;

        try
        {
            var updateInfo = await _sparkle.CheckForUpdatesQuietly();

            if (updateInfo?.Status == UpdateStatus.UpdateAvailable &&
                updateInfo.Updates is { Count: > 0 })
            {
                // Find the best update for the current OS
                var item = FindBestUpdate(updateInfo.Updates);
                if (item == null)
                {
                    SetState(UpdateState.UpToDate);
                    return null;
                }

                // Verify the available version is actually newer than what we're running
                var currentVersion = ArgoBooks.Core.Services.AppInfo.VersionNumber;
                if (!IsNewerVersion(currentVersion, item.Version))
                {
                    SetState(UpdateState.UpToDate);
                    return null;
                }

                AvailableUpdate = new Core.Services.UpdateInfo
                {
                    Version = item.Version ?? "Unknown",
                    DownloadUrl = item.DownloadLink ?? "",
                    ReleaseNotesUrl = item.ReleaseNotesLink,
                    FileSizeBytes = item.UpdateSize > 0 ? item.UpdateSize : null
                };

                SetState(UpdateState.UpdateAvailable);
                return AvailableUpdate;
            }

            SetState(UpdateState.UpToDate);
            return null;
        }
        catch (Exception ex)
        {
            _errorLogger?.LogWarning($"Update check failed: {ex.Message}", "AutoUpdate");
            LastError = ex.Message;
            SetState(UpdateState.Error);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task DownloadUpdateAsync()
    {
        if (AvailableUpdate == null || string.IsNullOrEmpty(AvailableUpdate.DownloadUrl))
            throw new InvalidOperationException("No update available to download.");

        SetState(UpdateState.Downloading);
        LastError = null;

        try
        {
            using var client = new HttpClient { Timeout = DownloadTimeout };

            var downloadUrl = AvailableUpdate.DownloadUrl;
            var tempDir = Path.Combine(Path.GetTempPath(), UpdateTempDirName);
            Directory.CreateDirectory(tempDir);

            var fileName = GetInstallerFileName(AvailableUpdate.Version);
            var filePath = Path.Combine(tempDir, fileName);

            // Download with progress reporting
            using var response = await client.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            var totalBytes = response.Content.Headers.ContentLength ?? AvailableUpdate.FileSizeBytes ?? -1;
            long receivedBytes = 0;
            int lastReportedProgress = -1;

            await using var contentStream = await response.Content.ReadAsStreamAsync();
            await using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write,
                FileShare.None, bufferSize: 81920, useAsync: true);

            var buffer = new byte[81920];
            int bytesRead;
            while ((bytesRead = await contentStream.ReadAsync(buffer)) > 0)
            {
                await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead));
                receivedBytes += bytesRead;

                if (totalBytes > 0)
                {
                    var progress = (int)(receivedBytes * 100 / totalBytes);
                    if (progress != lastReportedProgress)
                    {
                        lastReportedProgress = progress;
                        DownloadProgressChanged?.Invoke(this, progress);
                    }
                }
            }

            // Ensure 100% is reported
            DownloadProgressChanged?.Invoke(this, 100);

            // On Linux, make the downloaded file executable if it's an AppImage
            if (OperatingSystem.IsLinux())
            {
                try
                {
                    Process.Start("chmod", ["+x", filePath])?.WaitForExit(5000);
                }
                catch
                {
                    // Non-fatal — might not be an AppImage
                }
            }

            _installerPath = filePath;
            SetState(UpdateState.ReadyToInstall);
        }
        catch (Exception ex)
        {
            _errorLogger?.LogWarning($"Update download failed: {ex.Message}", "AutoUpdate");
            LastError = ex.Message;
            SetState(UpdateState.Error);
            throw;
        }
    }

    /// <inheritdoc />
    public void ApplyUpdateAndRestart()
    {
        if (State != UpdateState.ReadyToInstall || string.IsNullOrEmpty(_installerPath))
            throw new InvalidOperationException("No update is ready to install.");

        if (!File.Exists(_installerPath))
            throw new FileNotFoundException("Downloaded installer not found.", _installerPath);

        // Notify subscribers to save their data before we exit
        ApplyingUpdate?.Invoke(this, EventArgs.Empty);

        try
        {
            LaunchInstaller(_installerPath);
        }
        catch (Exception ex)
        {
            _errorLogger?.LogWarning($"Failed to launch installer: {ex.Message}", "AutoUpdate");
            LastError = $"Failed to launch installer automatically. Please run the installer manually from:\n{_installerPath}";
            SetState(UpdateState.Error);
            throw;
        }

        // Exit the application — the installer will handle the rest
        Environment.Exit(0);
    }

    public void Dispose()
    {
        _sparkle.Dispose();
    }

    #region Private Helpers

    /// <summary>
    /// Temp directory name used for downloaded update installers.
    /// </summary>
    private const string UpdateTempDirName = "ArgoBooks-Update";

    /// <summary>
    /// Deletes leftover update installer files from previous sessions.
    /// Safe to call at any time — silently ignores errors since cleanup is non-critical.
    /// </summary>
    private void CleanupPreviousUpdateFiles()
    {
        try
        {
            var tempDir = Path.Combine(Path.GetTempPath(), UpdateTempDirName);
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, recursive: true);
                _errorLogger?.LogInfo("Cleaned up previous update files.");
            }
        }
        catch (Exception ex)
        {
            // Non-critical — files may be locked or already deleted
            _errorLogger?.LogWarning($"Could not clean up previous update files: {ex.Message}", "AutoUpdate");
        }
    }

    /// <summary>
    /// Sets the state and raises the StateChanged event.
    /// </summary>
    private void SetState(UpdateState newState)
    {
        State = newState;
        StateChanged?.Invoke(this, newState);
    }

    /// <summary>
    /// Finds the best update item for the current operating system.
    /// NetSparkle's appcast supports per-OS entries via the sparkle:os attribute.
    /// </summary>
    private static AppCastItem? FindBestUpdate(List<AppCastItem> items)
    {
        // First try to find an OS-specific entry
        foreach (var item in items)
        {
            if (OperatingSystem.IsWindows() && item.IsWindowsUpdate)
                return item;
            if (OperatingSystem.IsMacOS() && item.IsMacOSUpdate)
                return item;
            if (OperatingSystem.IsLinux() && item.IsLinuxUpdate)
                return item;
        }

        // Fallback: return the first item if no OS-specific entry was found
        // (single-platform appcast or generic entry)
        return items.FirstOrDefault();
    }

    /// <summary>
    /// Compares version strings using System.Version.
    /// Returns true if <paramref name="availableVersion"/> is newer than <paramref name="currentVersion"/>.
    /// </summary>
    private static bool IsNewerVersion(string? currentVersion, string? availableVersion)
    {
        if (string.IsNullOrEmpty(currentVersion) || string.IsNullOrEmpty(availableVersion))
            return false;

        try
        {
            var current = new Version(currentVersion);
            var available = new Version(availableVersion);
            return available > current;
        }
        catch
        {
            // Fallback to string comparison
            return string.Compare(availableVersion, currentVersion, StringComparison.OrdinalIgnoreCase) > 0;
        }
    }

    /// <summary>
    /// Generates the appropriate installer filename for the current platform.
    /// </summary>
    private static string GetInstallerFileName(string version)
    {
        if (OperatingSystem.IsWindows())
            return $"ArgoBooks-{version}-win-x64.exe";
        if (OperatingSystem.IsMacOS())
            return $"ArgoBooks-{version}-osx-arm64.zip";
        if (OperatingSystem.IsLinux())
            return $"ArgoBooks-{version}-linux-x64.AppImage";
        return $"ArgoBooks-{version}-update";
    }

    /// <summary>
    /// Launches the downloaded installer using platform-appropriate methods.
    /// </summary>
    private static void LaunchInstaller(string installerPath)
    {
        if (OperatingSystem.IsWindows())
        {
            LaunchWindowsInstaller(installerPath);
        }
        else if (OperatingSystem.IsMacOS())
        {
            LaunchMacInstaller(installerPath);
        }
        else if (OperatingSystem.IsLinux())
        {
            LaunchLinuxInstaller(installerPath);
        }
    }

    /// <summary>
    /// Windows: Launch the downloaded Advanced Installer (MSI) exe silently.
    /// Uses the same flags as the working WinForms version:
    ///   /exenoui  - run the EXE bootstrapper without UI (triggers silent MSI install)
    ///   /norestart - don't restart the computer after install
    /// Verb "runas" requests UAC elevation for writing to Program Files.
    /// </summary>
    private static void LaunchWindowsInstaller(string installerPath)
    {
        var process = Process.Start(new ProcessStartInfo
        {
            FileName = installerPath,
            Arguments = "/exenoui /norestart",
            UseShellExecute = true,
            Verb = "runas"
        });

        if (process == null)
            throw new InvalidOperationException(
                "Failed to start installer process. Please run the installer manually from:\n" + installerPath);
    }

    /// <summary>
    /// macOS: Unzip the .app bundle and replace the current application, then relaunch.
    /// The zip contains the .app bundle at the top level.
    /// </summary>
    private static void LaunchMacInstaller(string installerPath)
    {
        var appBundlePath = GetCurrentAppBundlePath();

        if (installerPath.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
        {
            // Unzip to a staging area next to the current app
            var stagingDir = Path.Combine(Path.GetTempPath(), "ArgoBooks-Staging");
            if (Directory.Exists(stagingDir))
                Directory.Delete(stagingDir, true);

            // Use system unzip command (handles macOS resource forks correctly)
            var unzipProcess = Process.Start(new ProcessStartInfo
            {
                FileName = "unzip",
                Arguments = $"-o \"{installerPath}\" -d \"{stagingDir}\"",
                UseShellExecute = false,
                CreateNoWindow = true
            });
            unzipProcess?.WaitForExit(60_000);

            // Find the .app bundle in the staging directory
            var appBundles = Directory.GetDirectories(stagingDir, "*.app");
            if (appBundles.Length == 0)
                throw new FileNotFoundException("No .app bundle found in update zip.");

            var newAppPath = appBundles[0];

            // Use a shell script to replace the app and relaunch after a short delay
            // (we need the current process to exit first)
            var updateDir = Path.Combine(Path.GetTempPath(), UpdateTempDirName);
            var script = $"""
                          #!/bin/bash
                          sleep 2
                          rm -rf "{appBundlePath}"
                          mv "{newAppPath}" "{appBundlePath}"
                          open -n "{appBundlePath}"
                          rm -rf "{stagingDir}"
                          rm -rf "{updateDir}"
                          """;

            var scriptPath = Path.Combine(Path.GetTempPath(), "argo-update.sh");
            File.WriteAllText(scriptPath, script);
            Process.Start("chmod", ["+x", scriptPath])?.WaitForExit(5000);
            Process.Start(new ProcessStartInfo
            {
                FileName = "/bin/bash",
                Arguments = scriptPath,
                UseShellExecute = false,
                CreateNoWindow = true
            });
        }
        else if (installerPath.EndsWith(".pkg", StringComparison.OrdinalIgnoreCase))
        {
            // .pkg installer — open with the system installer
            Process.Start("open", [installerPath]);
        }
    }

    /// <summary>
    /// Linux: Replace the current AppImage with the new one and relaunch.
    /// </summary>
    private static void LaunchLinuxInstaller(string installerPath)
    {
        // Determine the path of the currently running executable
        var currentExePath = Environment.ProcessPath;
        if (string.IsNullOrEmpty(currentExePath))
            currentExePath = Process.GetCurrentProcess().MainModule?.FileName;

        if (!string.IsNullOrEmpty(currentExePath) &&
            installerPath.EndsWith(".AppImage", StringComparison.OrdinalIgnoreCase))
        {
            // Use a shell script to replace the AppImage and relaunch after a short delay
            var updateDir = Path.Combine(Path.GetTempPath(), UpdateTempDirName);
            var script = $"""
                          #!/bin/bash
                          sleep 2
                          cp "{installerPath}" "{currentExePath}"
                          chmod +x "{currentExePath}"
                          "{currentExePath}" &
                          rm -rf "{updateDir}"
                          """;

            var scriptPath = Path.Combine(Path.GetTempPath(), "argo-update.sh");
            File.WriteAllText(scriptPath, script);
            Process.Start("chmod", ["+x", scriptPath])?.WaitForExit(5000);
            Process.Start(new ProcessStartInfo
            {
                FileName = "/bin/bash",
                Arguments = scriptPath,
                UseShellExecute = false,
                CreateNoWindow = true
            });
        }
        else
        {
            // Non-AppImage Linux (deb/rpm) — just launch the package file with xdg-open
            Process.Start("xdg-open", [installerPath]);
        }
    }

    /// <summary>
    /// Gets the path to the current .app bundle on macOS.
    /// Walks up from the executing binary to find the .app directory.
    /// </summary>
    private static string GetCurrentAppBundlePath()
    {
        var exePath = Environment.ProcessPath
                      ?? Process.GetCurrentProcess().MainModule?.FileName
                      ?? "";

        // macOS app structure: MyApp.app/Contents/MacOS/MyApp
        // Walk up until we find a directory ending in .app
        var dir = Path.GetDirectoryName(exePath);
        while (!string.IsNullOrEmpty(dir))
        {
            if (dir.EndsWith(".app", StringComparison.OrdinalIgnoreCase))
                return dir;
            dir = Path.GetDirectoryName(dir);
        }

        // Fallback — assume standard location
        return Path.Combine("/Applications", "Argo Books.app");
    }

    #endregion
}
