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
    public override bool SupportsBiometrics => false; // Limited Linux support

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
}
