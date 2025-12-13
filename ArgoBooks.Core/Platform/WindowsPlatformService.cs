namespace ArgoBooks.Core.Platform;

/// <summary>
/// Windows-specific platform service implementation.
/// </summary>
public class WindowsPlatformService : BasePlatformService
{
    /// <inheritdoc />
    public override PlatformType Platform => PlatformType.Windows;

    /// <inheritdoc />
    public override string GetAppDataPath()
    {
        // %APPDATA%/ArgoBooks (Roaming AppData)
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return CombinePaths(appData, ApplicationName);
    }

    /// <inheritdoc />
    public override string GetCachePath()
    {
        // %LOCALAPPDATA%/ArgoBooks/Cache (Local AppData for machine-specific cache)
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return CombinePaths(localAppData, ApplicationName, "Cache");
    }

    /// <inheritdoc />
    public override bool SupportsBiometrics => true; // Windows Hello

    /// <inheritdoc />
    public override bool SupportsAutoUpdate => true;

    /// <inheritdoc />
    public override string NormalizePath(string path)
    {
        if (string.IsNullOrEmpty(path))
            return path;

        // Handle Windows-specific path normalization
        var normalized = Path.GetFullPath(path);

        // Ensure consistent casing for drive letter
        if (normalized.Length >= 2 && normalized[1] == ':')
        {
            normalized = char.ToUpperInvariant(normalized[0]) + normalized[1..];
        }

        return normalized;
    }
}
