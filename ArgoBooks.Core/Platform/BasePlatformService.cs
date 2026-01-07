namespace ArgoBooks.Core.Platform;

/// <summary>
/// Base implementation of platform service with common functionality.
/// </summary>
public abstract class BasePlatformService : IPlatformService
{
    private const string AppName = "ArgoBooks";

    /// <inheritdoc />
    public abstract PlatformType Platform { get; }

    /// <inheritdoc />
    public abstract string GetAppDataPath();

    /// <inheritdoc />
    public virtual string GetTempPath()
    {
        return CombinePaths(Path.GetTempPath(), AppName);
    }

    /// <inheritdoc />
    public virtual string GetDefaultDocumentsPath()
    {
        var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        return string.IsNullOrEmpty(documentsPath)
            ? GetAppDataPath()
            : CombinePaths(documentsPath, AppName);
    }

    /// <inheritdoc />
    public virtual string GetLogsPath()
    {
        return CombinePaths(GetAppDataPath(), "Logs");
    }

    /// <inheritdoc />
    public virtual string GetCachePath()
    {
        return CombinePaths(GetAppDataPath(), "Cache");
    }

    /// <inheritdoc />
    public void EnsureDirectoryExists(string path)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
    }

    /// <inheritdoc />
    public virtual bool SupportsFileSystem => true;

    /// <inheritdoc />
    public virtual bool SupportsNativeDialogs => true;

    /// <inheritdoc />
    public virtual bool SupportsBiometrics => false;

    /// <inheritdoc />
    public virtual bool SupportsAutoUpdate => true;

    /// <inheritdoc />
    public virtual int MaxRecentCompanies => 10;

    /// <inheritdoc />
    public virtual string NormalizePath(string path)
    {
        if (string.IsNullOrEmpty(path))
            return path;

        return Path.GetFullPath(path);
    }

    /// <inheritdoc />
    public virtual string CombinePaths(params string[] paths)
    {
        return Path.Combine(paths);
    }

    /// <inheritdoc />
    public virtual string GetMachineId()
    {
        // Fallback implementation using machine name
        // Platform-specific implementations should override this with more stable identifiers
        return Environment.MachineName;
    }

    /// <inheritdoc />
    public virtual void RegisterFileTypeAssociations(string iconPath)
    {
        // Default implementation does nothing
        // Platform-specific implementations (Windows) should override this
    }

    /// <inheritdoc />
    public virtual StringComparer PathComparer => StringComparer.Ordinal;

    /// <summary>
    /// Gets the application name used in paths.
    /// </summary>
    protected static string ApplicationName => AppName;
}
