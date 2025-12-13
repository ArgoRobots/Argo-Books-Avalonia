namespace ArgoBooks.Core.Platform;

/// <summary>
/// macOS-specific platform service implementation.
/// </summary>
public class MacPlatformService : BasePlatformService
{
    /// <inheritdoc />
    public override PlatformType Platform => PlatformType.MacOS;

    /// <inheritdoc />
    public override string GetAppDataPath()
    {
        // ~/Library/Application Support/ArgoBooks
        var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return CombinePaths(homeDir, "Library", "Application Support", ApplicationName);
    }

    /// <inheritdoc />
    public override string GetCachePath()
    {
        // ~/Library/Caches/ArgoBooks
        var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return CombinePaths(homeDir, "Library", "Caches", ApplicationName);
    }

    /// <inheritdoc />
    public override string GetLogsPath()
    {
        // ~/Library/Logs/ArgoBooks
        var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return CombinePaths(homeDir, "Library", "Logs", ApplicationName);
    }

    /// <inheritdoc />
    public override string GetDefaultDocumentsPath()
    {
        // ~/Documents/ArgoBooks
        var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        if (string.IsNullOrEmpty(documentsPath))
        {
            var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            documentsPath = CombinePaths(homeDir, "Documents");
        }
        return CombinePaths(documentsPath, ApplicationName);
    }

    /// <inheritdoc />
    public override bool SupportsBiometrics => true; // Touch ID

    /// <inheritdoc />
    public override bool SupportsAutoUpdate => true;
}
