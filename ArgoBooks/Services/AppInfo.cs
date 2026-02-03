using CoreAppInfo = ArgoBooks.Core.Services.AppInfo;

namespace ArgoBooks.Services;

/// <summary>
/// Provides application information such as version.
/// Delegates to ArgoBooks.Core.Services.AppInfo for consistency.
/// </summary>
public static class AppInfo
{
    private static string? _version;

    /// <summary>
    /// Gets the application version in "V.X.X.X" format for display.
    /// </summary>
    public static string Version
    {
        get
        {
            _version ??= $"V.{VersionNumber}";
            return _version;
        }
    }

    /// <summary>
    /// Gets the application version in "X.X.X" format (without "V." prefix).
    /// </summary>
    public static string VersionNumber => CoreAppInfo.VersionNumber;

    /// <summary>
    /// Gets the raw version object.
    /// </summary>
    public static Version? AssemblyVersion => CoreAppInfo.AssemblyVersion;
}
