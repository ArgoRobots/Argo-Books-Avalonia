using System.Diagnostics;
using ArgoBooks.Core.Platform.Linux;

namespace ArgoBooks.Core.Platform;

/// <summary>
/// Linux-specific platform service implementation.
/// Follows XDG Base Directory Specification.
/// </summary>
public class LinuxPlatformService : BasePlatformService
{
    /// <inheritdoc />
    public override PlatformType Platform => PlatformType.Linux;

    /// <inheritdoc />
    public override string GetAppDataPath()
    {
        // $XDG_CONFIG_HOME/ArgoBooks or ~/.config/ArgoBooks
        var xdgConfigHome = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
        if (!string.IsNullOrEmpty(xdgConfigHome))
        {
            return CombinePaths(xdgConfigHome, ApplicationName);
        }

        var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return CombinePaths(homeDir, ".config", ApplicationName);
    }

    /// <inheritdoc />
    public override string GetCachePath()
    {
        // $XDG_CACHE_HOME/ArgoBooks or ~/.cache/ArgoBooks
        var xdgCacheHome = Environment.GetEnvironmentVariable("XDG_CACHE_HOME");
        if (!string.IsNullOrEmpty(xdgCacheHome))
        {
            return CombinePaths(xdgCacheHome, ApplicationName);
        }

        var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return CombinePaths(homeDir, ".cache", ApplicationName);
    }

    /// <inheritdoc />
    public override string GetLogsPath()
    {
        // $XDG_STATE_HOME/ArgoBooks/logs or ~/.local/state/ArgoBooks/logs
        var xdgStateHome = Environment.GetEnvironmentVariable("XDG_STATE_HOME");
        if (!string.IsNullOrEmpty(xdgStateHome))
        {
            return CombinePaths(xdgStateHome, ApplicationName, "logs");
        }

        var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return CombinePaths(homeDir, ".local", "state", ApplicationName, "logs");
    }

    /// <inheritdoc />
    public override string GetDefaultDocumentsPath()
    {
        // $XDG_DOCUMENTS_DIR/ArgoBooks or ~/Documents/ArgoBooks
        var xdgDocumentsDir = Environment.GetEnvironmentVariable("XDG_DOCUMENTS_DIR");
        if (!string.IsNullOrEmpty(xdgDocumentsDir))
        {
            return CombinePaths(xdgDocumentsDir, ApplicationName);
        }

        var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        if (string.IsNullOrEmpty(documentsPath))
        {
            var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            documentsPath = CombinePaths(homeDir, "Documents");
        }
        return CombinePaths(documentsPath, ApplicationName);
    }

    /// <inheritdoc />
    public override bool SupportsBiometrics => true;

    /// <inheritdoc />
    public override async Task<bool> IsBiometricAvailableAsync()
    {
        return await Task.Run(() =>
            LinuxSecretStorage.IsAvailable() && LinuxAuthenticator.IsAvailable());
    }

    /// <inheritdoc />
    public override async Task<string> GetBiometricAvailabilityDetailsAsync()
    {
        if (!LinuxSecretStorage.IsAvailable())
            return "secret-tool is not installed. Install libsecret-tools to enable biometric login.";

        if (!LinuxAuthenticator.IsAvailable())
            return "pkexec (polkit) is not available. Install polkit to enable biometric login.";

        return await Task.FromResult("Available");
    }

    /// <inheritdoc />
    public override async Task<bool> AuthenticateWithBiometricAsync(string reason)
    {
        return await LinuxAuthenticator.AuthenticateAsync();
    }

    /// <inheritdoc />
    public override void StorePasswordForBiometric(string fileId, string password)
    {
        LinuxSecretStorage.Store(fileId, password);
    }

    /// <inheritdoc />
    public override string? GetPasswordForBiometric(string fileId)
    {
        return LinuxSecretStorage.Lookup(fileId);
    }

    /// <inheritdoc />
    public override void ClearPasswordForBiometric(string fileId)
    {
        LinuxSecretStorage.Clear(fileId);
    }

    /// <inheritdoc />
    public override bool SupportsAutoUpdate => true; // AppImage/Flatpak can auto-update

    /// <inheritdoc />
    public override string GetMachineId()
    {
        // Try /etc/machine-id first (systemd standard)
        try
        {
            const string machineIdPath = "/etc/machine-id";
            if (File.Exists(machineIdPath))
            {
                var machineId = File.ReadAllText(machineIdPath).Trim();
                if (!string.IsNullOrEmpty(machineId))
                {
                    return machineId;
                }
            }
        }
        catch
        {
            // File read failed
        }

        // Try /var/lib/dbus/machine-id (older systems)
        try
        {
            const string dbusMachineIdPath = "/var/lib/dbus/machine-id";
            if (File.Exists(dbusMachineIdPath))
            {
                var machineId = File.ReadAllText(dbusMachineIdPath).Trim();
                if (!string.IsNullOrEmpty(machineId))
                {
                    return machineId;
                }
            }
        }
        catch
        {
            // File read failed
        }

        return base.GetMachineId();
    }

    /// <inheritdoc />
    public override void RegisterFileTypeAssociations(string iconPath)
    {
        try
        {
            // Register MIME type for .argo files
            var mimeXmlPath = Path.Combine(AppContext.BaseDirectory, "com.argobooks.ArgoBooks.xml");
            if (File.Exists(mimeXmlPath))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "xdg-mime",
                    Arguments = $"install --novendor \"{mimeXmlPath}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true
                })?.WaitForExit(5000);
            }

            // Register desktop entry
            var desktopPath = Path.Combine(AppContext.BaseDirectory, "com.argobooks.ArgoBooks.desktop");
            if (File.Exists(desktopPath))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "xdg-desktop-menu",
                    Arguments = $"install --novendor \"{desktopPath}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true
                })?.WaitForExit(5000);
            }

            // Set default application for .argo files
            Process.Start(new ProcessStartInfo
            {
                FileName = "xdg-mime",
                Arguments = "default com.argobooks.ArgoBooks.desktop application/x-argo",
                UseShellExecute = false,
                CreateNoWindow = true
            })?.WaitForExit(5000);
        }
        catch
        {
            // File type registration is best-effort on Linux
        }
    }
}
