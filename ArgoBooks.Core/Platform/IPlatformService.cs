namespace ArgoBooks.Core.Platform;

/// <summary>
/// Platform-specific service for handling file system paths and platform capabilities.
/// </summary>
public interface IPlatformService
{
    /// <summary>
    /// Gets the platform type.
    /// </summary>
    PlatformType Platform { get; }

    /// <summary>
    /// Gets the application data directory path.
    /// Windows: %APPDATA%/ArgoBooks
    /// macOS: ~/Library/Application Support/ArgoBooks
    /// Linux: ~/.config/ArgoBooks
    /// Browser: Virtual file system path
    /// </summary>
    string GetAppDataPath();

    /// <summary>
    /// Gets the path for temporary files during file operations.
    /// </summary>
    string GetTempPath();

    /// <summary>
    /// Gets the default directory for saving new company files.
    /// </summary>
    string GetDefaultDocumentsPath();

    /// <summary>
    /// Gets the path for crash logs and diagnostics.
    /// </summary>
    string GetLogsPath();

    /// <summary>
    /// Gets the path for cached data (can be cleared without data loss).
    /// </summary>
    string GetCachePath();

    /// <summary>
    /// Ensures a directory exists, creating it if necessary.
    /// </summary>
    /// <param name="path">Directory path to ensure exists.</param>
    void EnsureDirectoryExists(string path);

    /// <summary>
    /// Gets whether the platform supports file system access.
    /// Browser platform may have limited file system access.
    /// </summary>
    bool SupportsFileSystem { get; }

    /// <summary>
    /// Gets whether the platform supports native file dialogs.
    /// </summary>
    bool SupportsNativeDialogs { get; }

    /// <summary>
    /// Gets whether the platform supports biometric authentication.
    /// </summary>
    bool SupportsBiometrics { get; }

    /// <summary>
    /// Gets whether the platform supports automatic updates.
    /// </summary>
    bool SupportsAutoUpdate { get; }

    /// <summary>
    /// Gets the maximum recent companies to track.
    /// </summary>
    int MaxRecentCompanies { get; }

    /// <summary>
    /// Normalizes a file path for the current platform.
    /// </summary>
    /// <param name="path">Path to normalize.</param>
    /// <returns>Normalized path.</returns>
    string NormalizePath(string path);

    /// <summary>
    /// Combines path segments using the platform's path separator.
    /// </summary>
    /// <param name="paths">Path segments to combine.</param>
    /// <returns>Combined path.</returns>
    string CombinePaths(params string[] paths);

    /// <summary>
    /// Gets a stable, unique identifier for this machine.
    /// Used for machine-binding features like license encryption.
    /// </summary>
    /// <remarks>
    /// Platform implementations:
    /// - Windows: Uses MachineGuid from registry
    /// - Linux: Uses /etc/machine-id
    /// - macOS: Uses IOPlatformUUID
    /// - Browser: Uses a stored random ID
    /// </remarks>
    /// <returns>A stable machine identifier string.</returns>
    string GetMachineId();

    /// <summary>
    /// Registers file type associations for the platform.
    /// On Windows, this sets up the .argo file extension with the app icon.
    /// </summary>
    /// <param name="iconPath">Path to the application icon file.</param>
    void RegisterFileTypeAssociations(string iconPath);

    /// <summary>
    /// Gets the string comparer appropriate for file paths on this platform.
    /// Windows uses case-insensitive comparison, other platforms use case-sensitive.
    /// </summary>
    StringComparer PathComparer { get; }
}

/// <summary>
/// Supported platform types.
/// </summary>
public enum PlatformType
{
    /// <summary>
    /// Microsoft Windows.
    /// </summary>
    Windows,

    /// <summary>
    /// Apple macOS.
    /// </summary>
    MacOS,

    /// <summary>
    /// Linux distributions.
    /// </summary>
    Linux,

    /// <summary>
    /// Web browser (WebAssembly).
    /// </summary>
    Browser
}
